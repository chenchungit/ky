using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using System.Xml.Linq;
using Server.Tools;
using GameServer.Server;

namespace GameServer.Logic.ActivityNew
{
    public enum JieriLianXuChargeErrorCode
    {
        Success,                      // 成功
	    InvalidParam,			     // 客户端传来参数错误
        ActivityNotOpen,              // 活动未开启
        NotAwardTime,                 // 不是领奖时间
        DBFailed,                           // 数据库服务器出错
        ConfigError,                       // 服务器配置错误
        NoBagSpace,                     // 背包不足
        NotMeetAwardCond,         // 不满足领奖条件(已领奖或未达成)
    }

    /// <summary>
    /// 节日连续充值活动
    /// </summary>
    public class JieriLianXuChargeActivity : Activity
    {
        // 领奖信息
        class _AwardInfo
        {
            public int AwardId;         // 档次Id
            public int LianXuDay;      // 连续达成充值额度的天数
            public int AwardFlag;      // 领奖标记
        }

        // 连续达成充值额度的天数奖励
        class _DayAward
        {
            public int LianXuDay;                   // 连续登陆的天数
            public AwardItem AwardGoods = new AwardItem();
        }

        // 充值档次
        class _ChargeLvl
        {
            public int Id;   // 档次id
            public int NeedCharge; // 最少充值
            public List<_DayAward> AwardList = new List<_DayAward>(); // 连续充值天数对应的奖励
        }

        private readonly string CfgFile = "Config/JieRiGifts/JieRiLianXu.xml";
        // 保存多个档次的奖励信息
        private List<_ChargeLvl> chargeLvlList = new List<_ChargeLvl>();

        // 初始化配置文件信息
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

                Dictionary<int, _ChargeLvl> awardId2ChargeLvl = new Dictionary<int, _ChargeLvl>();

                if (null != args)
                {
                    IEnumerable<XElement> xmlItems = args.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null == xmlItem)
                        {
                            continue;
                        }

                        int awardId = (int)Global.GetSafeAttributeLong(xmlItem, "Group");
                        int needCharge = (int)Global.GetSafeAttributeLong(xmlItem, "NeedZuanShi");
                        _ChargeLvl chargeLvl = null;
                        if (!awardId2ChargeLvl.TryGetValue(awardId, out chargeLvl))
                        {
                            chargeLvl = new _ChargeLvl();
                            chargeLvl.Id = awardId;
                            chargeLvl.NeedCharge = needCharge;

                            awardId2ChargeLvl[awardId] = chargeLvl;
                        }

                        _DayAward dayAward = new _DayAward();
                        dayAward.LianXuDay = (int)Global.GetSafeAttributeLong(xmlItem, "Day");
                        string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsOne");
                        if (string.IsNullOrEmpty(goodsIDs))
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项为空", CfgFile));
                        }
                        else
                        {
                            string[] fields = goodsIDs.Split('|');
                            if (fields.Length <= 0)
                            {
                                LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项失败", CfgFile));
                            }
                            else
                            {
                                dayAward.AwardGoods.GoodsDataList.AddRange(HuodongCachingMgr.ParseGoodsDataList(fields, "连续充值活动goods1配置"));
                            }
                        }

                        goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsTwo");
                        if (string.IsNullOrEmpty(goodsIDs))
                        {
                        }
                        else
                        {
                            string[] fields = goodsIDs.Split('|');
                            if (fields.Length <= 0)
                            {
                                LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项失败", CfgFile));
                            }
                            else
                            {
                                //将物品字符串列表解析成物品数据列表
                                dayAward.AwardGoods.GoodsDataList.AddRange(HuodongCachingMgr.ParseGoodsDataList(fields, "连续充值活动goods2配置"));
                            }
                        }

                        chargeLvl.AwardList.Add(dayAward);
                    }

                    chargeLvlList.AddRange(awardId2ChargeLvl.Values.ToList());

                    PredealDateTime();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", CfgFile, ex.Message));
                return false;
            }

            return true;
        }

        // 查询我的活动信息
        // 返回: errcode : awardId,dayCnt,awardFlag : awardId,dayCnt,awardFlag : awardId,dayCnt,awardFlag......(多个)
        // awardId：类型int，档次id，对应JieRiLianXu.xml里面的id属性。
        // dayCnt: 类型int，表示对应awardId档次连续充值的天数。取值范围[0 , 配置文件中的最大天数]
        // awardFlag: 类型int，表示awardId每天的领奖信息。
        // 注意：只有day∈[1,dayCnt]时，awardFlag&(1<<day) == 1表示已领取，==0表示可领取但是未领取。
        // day不属于[1, dayCnt]时，说明尚未达成。
        public string QueryMyActInfo(GameClient client)
        {
            if ((!InActivityTime() && !InAwardTime()) || client == null)
            {
                return string.Format("{0}", (int)JieriLianXuChargeErrorCode.ActivityNotOpen);
            }

            List<_AwardInfo> myDataLst = _GetMyActInfoFromDB(client);
            if (myDataLst == null)
            {
                return string.Format("{0}", (int)JieriLianXuChargeErrorCode.DBFailed);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append((int)JieriLianXuChargeErrorCode.Success);
            foreach (var info in myDataLst)
            {
                sb.Append(":").Append(info.AwardId);  // 档次id
                sb.Append(",").Append(info.LianXuDay); // 达成的天数
                sb.Append(",").Append(info.AwardFlag); // 达成天数内的领奖标记，其余天数默认为未达成
            }

            return sb.ToString();
        }

        // 领奖
        public JieriLianXuChargeErrorCode HandleGetAward(GameClient client, int awardId, int day)
        {
            if ( !InAwardTime() || client == null)
            {
                return JieriLianXuChargeErrorCode.NotAwardTime;
            }

            _ChargeLvl cl = chargeLvlList.Find(_cl=> _cl.Id == awardId);
            if (cl == null)
            {
                // 找不到该档次
                return JieriLianXuChargeErrorCode.ConfigError;
            }

            _DayAward da = cl.AwardList.Find(_da => _da.LianXuDay == day);
            if (da == null)
            {
                // 找不到该档次的day信息
                return JieriLianXuChargeErrorCode.ConfigError;
            }

            List<_AwardInfo> myDataLst = _GetMyActInfoFromDB(client);
            if (myDataLst == null)
            {
                // 找不到我的领奖信息
                return JieriLianXuChargeErrorCode.DBFailed;
            }

            var info = myDataLst.Find(_info => _info.AwardId == awardId);
            if (info == null)
            {
                // 找不到该档次的领奖信息
                return JieriLianXuChargeErrorCode.ConfigError;
            }

            if (info.LianXuDay < day || Global.GetIntSomeBit(info.AwardFlag, day) == 1)
            {
                // 尚未达成或者已经领取
                return JieriLianXuChargeErrorCode.NotMeetAwardCond;
            }

            if (da.AwardGoods != null && da.AwardGoods.GoodsDataList != null && da.AwardGoods.GoodsDataList.Count > 0)
            {
                // 发放的是礼包，只有1个
                int AwardGoodsCnt = da.AwardGoods.GoodsDataList.Count(goods => Global.IsRoleOccupationMatchGoods(client, goods.GoodsID));
                if (!Global.CanAddGoodsNum(client, AwardGoodsCnt))
                {
                    // 背包不足
                    return JieriLianXuChargeErrorCode.NoBagSpace;
                }
            }

            int newAwardFlag = Global.SetIntSomeBit(day, info.AwardFlag, true);
            if (!_UpdateAwardFlag2DB(client, awardId, newAwardFlag))
            {
                // 更新领奖信息失败啊啊啊啊
                return JieriLianXuChargeErrorCode.DBFailed;
            }

            info.AwardFlag = newAwardFlag;
            GiveAward(client, da.AwardGoods);
            //_GiveAward2Client(client, da.AwardGoods1);

            // 检查图标
            if (client._IconStateMgr.CheckJieriLianXuCharge(client))
            {
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                client._IconStateMgr.SendIconStateToClient(client);
            }

            return JieriLianXuChargeErrorCode.Success;
        }

        private bool _UpdateAwardFlag2DB(GameClient client, int awardId, int awardFlag)
        {
            if (client == null) return false;

            string cmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, awardId, FromDate.Replace(':', '$'), ToDate.Replace(':', '$'), awardFlag);
            string[] dbRet = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_JIERI_LIANXU_CHARGE_AWARD, cmd, client.ServerId);
            if (dbRet == null || dbRet.Length != 1 || Convert.ToInt32(dbRet[0]) <= 0)
            {
                return false;
            }

            return true;
        }

        // 从db获取我的活动信息, 包括每个充值档次达成的天数
        // 以及每个档次达成的天数的每一天的领奖情况
        private List<_AwardInfo> _GetMyActInfoFromDB(GameClient client)
        {
            if (client == null) return null;
            if (!InActivityTime() && !InAwardTime()) return null;

            StringBuilder sb = new StringBuilder();
            sb.Append(client.ClientData.RoleID);
            sb.Append(':').Append(client.ClientData.ZoneID);
            sb.Append(':').Append(FromDate.Replace(':', '$'));
            sb.Append(':').Append(ToDate.Replace(':', '$'));
            sb.Append(':');
            foreach (var cl in chargeLvlList)
            {
                sb.Append(cl.Id).Append('_');
            }

            string[] dbRet = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_QUERY_JIERI_LIANXU_CHARGE, sb.ToString(), client.ServerId);
            if (dbRet == null || dbRet.Length != 2)
            {
                return null;
            }

            int[] eachDayChargeArr = _ParseEachDayCharge(dbRet[0]);
            Dictionary<int,int> awardFlagDic = _ParseAwardFlagOfEachLvl(dbRet[1]);
    
            List<_AwardInfo> result = new List<_AwardInfo>();
            foreach (var cl in chargeLvlList)
            {
                // 计算出来每个充值档次连续充值达成的天数以及领奖信息
                _AwardInfo ai = new _AwardInfo();
                ai.LianXuDay = _CalcLianXuChargeDay(eachDayChargeArr,  cl.NeedCharge);
                ai.AwardId = cl.Id;
                ai.AwardFlag = 0;
                if (awardFlagDic.ContainsKey(cl.Id))
                {
                    ai.AwardFlag = awardFlagDic[cl.Id];
                }

                result.Add(ai);
            }

            return result;
        }

        // 计算连续每天充值atLeastCharge的天数
        private int _CalcLianXuChargeDay(int[] eachDayChargeArray, int atLeastCharge)
        {
            if (eachDayChargeArray == null || atLeastCharge <= 0)
                return 0;

            int dayCnt = 0;

            // 动态规划计算
            //int _globalDayCnt = 0;
            //int _localDayCnt = 0;

            for (int i = 0; i < eachDayChargeArray.Length; ++i)
            {
                if (eachDayChargeArray[i] >= atLeastCharge)
                {
                    dayCnt++;
                    //_localDayCnt++;
                }
                else
                {
                   // _localDayCnt = 0;
                }

               // _globalDayCnt = Math.Max(_globalDayCnt, _localDayCnt);
            }

            return dayCnt;
            //return _globalDayCnt;
        }

        // 根据`awardId,awardFlag$awardId,awardFlag`的格式得到字典
        // Key: awardId  Value: awardFlag
        private Dictionary<int, int> _ParseAwardFlagOfEachLvl(string strAwardIdAndFlag)
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            if (!string.IsNullOrEmpty(strAwardIdAndFlag))
            {
                string[] szIdFlag = strAwardIdAndFlag.Split('$');
                foreach (var str in szIdFlag)
                {
                    if (string.IsNullOrEmpty(str)) continue;

                    string[] fields = str.Split(',');
                    int awardId = Convert.ToInt32(fields[0]);
                    int awardFlag = Convert.ToInt32(fields[1]);

                    result[awardId] = awardFlag;
                }
            }

            return result;
        }

        // 根据`天,钱$天,钱`的格式得到活动期间每天充值了多少钱
        // 天的格式是 2015-06-19 这样的
        private int[] _ParseEachDayCharge(string strMoneyOfDays)
        {
            if (string.IsNullOrEmpty(strMoneyOfDays))
            {
                return null;
            }

            Dictionary<string, int> chargeOfDay = new Dictionary<string, int>();
            string[] szDayCharge = strMoneyOfDays.Split('$');
            foreach (var str in szDayCharge)
            {
                if (string.IsNullOrEmpty(str)) continue;

                string[] fields = str.Split(',');
                string day = fields[0]; //"yyyy-MM-dd"
                int money = Convert.ToInt32(fields[1]);

                if (chargeOfDay.ContainsKey(day))
                {
                    chargeOfDay[day] += money;
                }
                else
                {
                    chargeOfDay.Add(day, money);
                }
            }

            DateTime _startReal = DateTime.Parse(FromDate);
            DateTime _endReal = DateTime.Parse(ToDate);

            // 通过活动的第一天的凌晨和最后一天的凌晨，判断活动一共有多少天
            DateTime _startMorning = new DateTime(_startReal.Year, _startReal.Month, _startReal.Day);
            DateTime _endMorning = new DateTime(_endReal.Year, _endReal.Month, _endReal.Day);

            // 得到活动的持续天数
            int actTotalDay = (int)(_endMorning - _startMorning).TotalDays + 1;
            if (actTotalDay <= 0) return null;

            // 保存活动的每一天充值了多少
            int[] eachDayChargeArray = new int[actTotalDay];
            for (int i = 0; i < actTotalDay; ++i)
            {
                string szDay = _startMorning.AddDays(i).ToString("yyyy-MM-dd");
                if (chargeOfDay.ContainsKey(szDay))
                {
                    eachDayChargeArray[i] = chargeOfDay[szDay];
                }
                else
                {
                    eachDayChargeArray[i] = 0;
                }
            }

            return eachDayChargeArray;
        }

        //  是否有任一档次中的任一项可以领取
        public bool CanGetAnyAward(GameClient client)
        {
            if (client == null || !InAwardTime()) return false;

            List<_AwardInfo> myDataLst = _GetMyActInfoFromDB(client);
            if (myDataLst == null) return false;

            foreach (var info in myDataLst)
            {
                // 找到对应的充值档次
                _ChargeLvl cl = chargeLvlList.Find(_cl => _cl.Id == info.AwardId);
                if (cl == null) return false;

                foreach (var award in cl.AwardList)
                {
                    // 判断本档次中达成的天数中，是否有任意天数未领取
                    if (award.LianXuDay <= info.LianXuDay && Global.GetIntSomeBit(info.AwardFlag, award.LianXuDay) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}