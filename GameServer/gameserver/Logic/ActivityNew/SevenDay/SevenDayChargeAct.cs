using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Data;
using Server.Tools;
using System.Xml.Linq;
using GameServer.Server;

namespace GameServer.Logic.ActivityNew.SevenDay
{
    /// <summary>
    /// 七日充值活动
    /// </summary>
    public class SevenDayChargeAct
    {
        /// <summary>
        /// 每天的奖励
        /// </summary>
        class _DayAward
        {
            public int NeedCharge;
            public AwardItem AllAward = new AwardItem();
            public AwardItem OccAward = new AwardItem();
        }

        private Dictionary<int, _DayAward> DayAwardDict = null;
        private object ConfigMutex = new object();

        /// <summary>
        /// 读取配置文件
        /// </summary>
        public void LoadConfig()
        {
            Dictionary<int, _DayAward> tmpDict = new Dictionary<int, _DayAward>();
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(SevenDayConsts.ChargeConfig)).Element("GiftList");
                foreach (var xmlItem in xml.Elements())
                {
                    int day = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    _DayAward award = new _DayAward();
                    award.NeedCharge = (int)Global.GetSafeAttributeLong(xmlItem, "MinZhuanShi");

                    // 通用奖励
                    string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsOne");
                    if (!string.IsNullOrEmpty(goodsIDs))
                    {
                        string[] fields = goodsIDs.Split('|');
                        if (fields.Length <= 0)
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}活动配置文件中的物品配置项失败", SevenDayConsts.ChargeConfig));
                        }
                        else
                        {
                            award.AllAward.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, SevenDayConsts.ChargeConfig);
                        }
                    }

                    // 职业奖励
                    goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsTwo");
                    if (!string.IsNullOrEmpty(goodsIDs))
                    {
                        string[] fields = goodsIDs.Split('|');
                        if (fields.Length <= 0)
                        {
                            LogManager.WriteLog(LogTypes.Warning, SevenDayConsts.ChargeConfig);
                        }
                        else
                        {
                            award.OccAward.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, SevenDayConsts.ChargeConfig);
                        }
                    }

                    tmpDict[day] = award;
                }

                lock (ConfigMutex)
                {
                    DayAwardDict = tmpDict;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("七日登录活动加载配置失败{0}", SevenDayConsts.ChargeConfig), ex);
            }
        }

        /// <summary>
        /// 领取奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="innerId"></param>
        /// <returns></returns>
        public ESevenDayActErrorCode HandleGetAward(GameClient client, int day)
        {
            int currDay;
            if (!SevenDayActivityMgr.Instance().IsInActivityTime(client, out currDay))
                return ESevenDayActErrorCode.NotInActivityTime;

            if (day < 0 || day > currDay) 
                return ESevenDayActErrorCode.VisitParamsWrong;

            _DayAward dayAward = null;
            lock (ConfigMutex)
            {
                if (DayAwardDict == null || !DayAwardDict.TryGetValue(day, out dayAward))
                    return ESevenDayActErrorCode.ServerConfigError;
            }

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Charge);
            if (itemDict == null)
                return ESevenDayActErrorCode.NotReachCondition;

            lock (itemDict)
            {
                SevenDayItemData itemData = null;
                if (!itemDict.TryGetValue(day, out itemData))
                {
                    return ESevenDayActErrorCode.ServerConfigError;
                }

                if (itemData.Params1 * GameManager.GameConfigMgr.GetGameConfigItemInt("money-to-yuanbao", 10) < dayAward.NeedCharge || itemData.AwardFlag != SevenDayConsts.NotGetAward)
                {
                    return ESevenDayActErrorCode.NotReachCondition;
                }

                // 检查背包
                int awardGoodsCnt = 0;
                if (dayAward.AllAward != null && dayAward.AllAward.GoodsDataList != null)
                    awardGoodsCnt += dayAward.AllAward.GoodsDataList.Count;
                if (dayAward.OccAward != null && dayAward.OccAward.GoodsDataList != null)
                    awardGoodsCnt += dayAward.OccAward.GoodsDataList.Count(_goods => Global.IsRoleOccupationMatchGoods(client, _goods.GoodsID));
                if (awardGoodsCnt <= 0 || !Global.CanAddGoodsNum(client, awardGoodsCnt))
                    return ESevenDayActErrorCode.NoBagSpace;

                itemData.AwardFlag = SevenDayConsts.HadGetAward;
                if (!SevenDayActivityMgr.Instance().UpdateDb(client.ClientData.RoleID, ESevenDayActType.Charge, day, itemData, client.ServerId))
                {
                    itemData.AwardFlag = SevenDayConsts.NotGetAward;
                    return ESevenDayActErrorCode.DBFailed;
                }

                if (!SevenDayActivityMgr.Instance().GiveAward(client, dayAward.AllAward, ESevenDayActType.Charge)
                    || !SevenDayActivityMgr.Instance().GiveAward(client, dayAward.OccAward, ESevenDayActType.Charge)
                    )
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("玩家领取七日充值奖励，设置领奖了但是发奖失败 roleid={0}, day={1}", client.ClientData.RoleID, day));
                }

                return ESevenDayActErrorCode.Success;
            }
        }

        /// <summary>
        /// 角色上线，跨天登录, 充值时检测
        /// </summary>
        /// <param name="client"></param>
        public void Update(GameClient client)
        {
            if (client == null) return;

            int currDay;
            if (!SevenDayActivityMgr.Instance().IsInActivityTime(client, out currDay)) return;

            // 取出第一天的凌晨 和今天晚上，这段时间内每天充值的金额
            DateTime startDate = Global.GetRegTime(client.ClientData);
            startDate -= startDate.TimeOfDay;
            DateTime endDate = startDate.AddDays(currDay - 1).AddHours(23).AddMinutes(59).AddSeconds(59);

            StringBuilder sb = new StringBuilder();
            sb.Append(client.ClientData.RoleID);
            sb.Append(':').Append(startDate.ToString("yyyy-MM-dd HH:mm:ss").Replace(':', '$'));
            sb.Append(':').Append(endDate.ToString("yyyy-MM-dd HH:mm:ss").Replace(':', '$'));

            Dictionary<string, int> eachDayChargeDict = Global.sendToDB<Dictionary<string, int>, string>((int)TCPGameServerCmds.CMD_DB_GET_EACH_DAY_CHARGE, sb.ToString(), client.ServerId);
            if (eachDayChargeDict == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Charge);
            lock (itemDict)
            {
                // i+1 对应 SevenDayChongZhi.xml  的id
                for (int i = 0; i < SevenDayConsts.DayCount; ++i)
                {
                    string szKey = startDate.AddDays(i).ToString("yyyy-MM-dd");
                    int charge;
                    if (!eachDayChargeDict.TryGetValue(szKey, out charge))
                    {
                        continue;
                    }

                    // 检测更新充值金额
                    SevenDayItemData itemData;
                    if (!itemDict.TryGetValue(i + 1, out itemData) || itemData.Params1 != charge)
                    {
                        SevenDayItemData tmpData = new SevenDayItemData();
                        tmpData.AwardFlag = itemData != null ? itemData.AwardFlag : SevenDayConsts.NotGetAward;
                        tmpData.Params1 = charge;

                        if (SevenDayActivityMgr.Instance().UpdateDb(client.ClientData.RoleID, ESevenDayActType.Charge, i + 1, tmpData, client.ServerId))
                        {
                            itemDict[i + 1] = tmpData;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 是否有任何可以领取的奖励
        /// 用于检测图标更新
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool HasAnyAwardCanGet(GameClient client)
        {
            if (client == null) return false;

            if (!SevenDayActivityMgr.Instance().IsInActivityTime(client)) return false;

            Dictionary<int, _DayAward> tmpDayAwardDict = null;
            lock (ConfigMutex) { tmpDayAwardDict = this.DayAwardDict; }
            if (tmpDayAwardDict == null) return false;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Charge);
            if (itemDict == null) return false;

            lock (itemDict)
            {
                foreach (var kvp in itemDict)
                {
                    int day = kvp.Key;
                    SevenDayItemData itemData = kvp.Value;
                    _DayAward award = null;
                    if (tmpDayAwardDict.TryGetValue(day, out award) && itemData.Params1 * GameManager.GameConfigMgr.GetGameConfigItemInt("money-to-yuanbao", 10) >= award.NeedCharge && itemData.AwardFlag != SevenDayConsts.HadGetAward)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
