using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Web.Script.Serialization;

using Tmsk.Tools;
using Server.Data;
using Server.Tools;
using GameServer.Server;
using Tmsk.Contract.Data;
using KF.Contract.Interface;
using KF.Client;
using GameServer.Core.Executor;

namespace GameServer.Logic.ActivityNew
{
    /// <summary>
    /// 节日平台充值王
    /// </summary>
    public class JieriPlatChargeKing : Activity
    {
        // 每个充值档的信息
        class ChargeItem
        {
            // 第几名
            public int Rank;
            // 至少充值多少YB
            public int NeedChargeYB;
            // 奖励的物品就不处理了。因为是客服手工发放
        }

        private readonly string CfgFile = "Config/JieRiGifts/PingTaiChongZhiKing.xml";
        private List<ChargeItem> chargeItemList = new List<ChargeItem>();

        private object Mutex = new object();
        private List<InputKingPaiHangData> _realRankList = null;
        public List<InputKingPaiHangData> RealRankList
        {
            get { lock (Mutex) { return _realRankList; } }
            private set { lock (Mutex) { _realRankList = value; } }
        }

        private DateTime lastUpdateTime = TimeUtil.NowDateTime().AddSeconds(-updateIntervalSec * 2);
        private const int updateIntervalSec = 15; //30 seconds

        public bool Init()
        {
            try
            {
                GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(CfgFile));
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(CfgFile));
                if (null == xml) return false;

                XElement args = xml.Element("Activities");
                if (null != args)
                {
                    FromDate = Global.GetSafeAttributeStr(args, "FromDate");
                    ToDate = Global.GetSafeAttributeStr(args, "ToDate");
                    ActivityType = (int)Global.GetSafeAttributeLong(args, "ActivityType");

                    AwardStartDate = Global.GetSafeAttributeStr(args, "AwardStartDate");
                    AwardEndDate = Global.GetSafeAttributeStr(args, "AwardEndDate");
                }

                args = xml.Element("GiftList");
                if (null != args)
                {
                    IEnumerable<XElement> xmlItems = args.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null == xmlItem)
                        {
                            continue;
                        }
                        ChargeItem ci = new ChargeItem();
                        ci.Rank = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        ci.NeedChargeYB = (int)Global.GetSafeAttributeLong(xmlItem, "MinYuanBao");

                        chargeItemList.Add(ci);
                    }

                    // 多个档次，按照档次Id从小到大排序
                    chargeItemList.Sort((left, right) =>
                    {
                        if (left.Rank < right.Rank) return -1;
                        else if (left.Rank > right.Rank) return 1;
                        else return 0;
                    });
                }

                PredealDateTime();
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", CfgFile, ex.Message));
                return false;
            }

            return true;
        }

        public void Update()
        {
            if (InActivityTime() || InAwardTime())
            {
                DateTime now = TimeUtil.NowDateTime();
                if (now < lastUpdateTime.AddSeconds(updateIntervalSec))
                {
                    return;
                }
                lastUpdateTime = now;

                InputKingPaiHangDataEx tmpRankEx = KFCopyRpcClient.getInstance().GetPlatChargeKing();
                if (tmpRankEx == null)
                    return;

                List<InputKingPaiHangData> tmpRankList = tmpRankEx.ListData;
                if (tmpRankEx.StartTime != FromDate || tmpRankEx.EndTime != ToDate)
                {
                  //  tmpRankList = null;
                }

                if (tmpRankList != null)
                {
                    // 排下序，防止意外
                    // 排序这里要注意：老陶那边会排序，充值金额相同的按照时间排序
                    // 所以这里做一个检查，如果顺序ok就不排序了，防止打乱时间顺序
                    bool bNeedSort = false;
                    for (int i = 1; i < tmpRankList.Count(); ++i)
                    {
                        if (tmpRankList[i].PaiHangValue > tmpRankList[i - 1].PaiHangValue)
                        {
                            bNeedSort = true;
                            break;
                        }
                    }

                    if (bNeedSort)
                    {
                        tmpRankList.Sort((_left, _right) => { return _right.PaiHangValue - _left.PaiHangValue; });
                    }

                    tmpRankList.ForEach(_item => _item.PaiHangValue = Global.TransMoneyToYuanBao(_item.PaiHangValue));

                    int procListIdx = 0;
                    for (int i = 0; i < chargeItemList.Count && procListIdx < tmpRankList.Count; ++i)
                    {
                        if (tmpRankList[procListIdx].PaiHangValue >= chargeItemList[i].NeedChargeYB)
                        {
                            tmpRankList[procListIdx].PaiHang = chargeItemList[i].Rank;
                            ++procListIdx;
                        }
                    }

                    if (procListIdx < tmpRankList.Count)
                    {
                        tmpRankList.RemoveRange(procListIdx, tmpRankList.Count - procListIdx);
                    }
                }

                RealRankList = tmpRankList;
            }
            else
            {
                RealRankList = null;
            }
        }
    }
}
