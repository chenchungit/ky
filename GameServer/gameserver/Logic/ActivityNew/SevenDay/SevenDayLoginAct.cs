using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Server.Tools;
using GameServer.Core.Executor;
using Server.Data;

namespace GameServer.Logic.ActivityNew.SevenDay
{
    /// <summary>
    /// 七日登录活动
    /// </summary>
    public class SevenDayLoginAct
    {
        /// <summary>
        /// 每天登录的奖励项
        /// </summary>
        class _DayAward
        {
            public AwardItem AllAward = new AwardItem();  // 通用奖励
            public AwardItem OccAward = new AwardItem(); // 职业奖励
            public AwardEffectTimeItem TimeAward = new AwardEffectTimeItem(); // 限时奖励
        }

        private Dictionary<int, _DayAward> DayAwardDict = null;
        private object ConfigMutex = new object();

        /// <summary>
        /// 加载配置文件，支持热加载
        /// </summary>
        public void LoadConfig()
        {
            Dictionary<int, _DayAward> tmpDict = new Dictionary<int, _DayAward>();
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(SevenDayConsts.LoginConfig)).Element("GiftList");
                foreach (var xmlItem in xml.Elements())
                {
                    int day = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    _DayAward award = new _DayAward();

                    // 通用奖励
                    string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsOne");
                    if (!string.IsNullOrEmpty(goodsIDs))
                    {
                        string[] fields = goodsIDs.Split('|');
                        if (fields.Length <= 0)
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}活动配置文件中的物品配置项失败", SevenDayConsts.LoginConfig));
                        }
                        else
                        {
                            award.AllAward.GoodsDataList =HuodongCachingMgr.ParseGoodsDataList(fields, SevenDayConsts.LoginConfig);
                        }
                    }

                    // 职业奖励
                    goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsTwo");
                    if (!string.IsNullOrEmpty(goodsIDs))
                    {
                        string[] fields = goodsIDs.Split('|');
                        if (fields.Length <= 0)
                        {
                            LogManager.WriteLog(LogTypes.Warning, SevenDayConsts.LoginConfig);
                        }
                        else
                        {
                            award.OccAward.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, SevenDayConsts.LoginConfig);
                        }
                    }

                    // 限时奖励
                    string timeGoods = Global.GetSafeAttributeStr(xmlItem, "GoodsThr");
                    string timeList = Global.GetSafeAttributeStr(xmlItem, "EffectiveTime");
                    award.TimeAward.Init(timeGoods, timeList, SevenDayConsts.LoginConfig + " 时效性物品");

                    tmpDict[day] = award;
                }

                lock (ConfigMutex)
                {
                    DayAwardDict = tmpDict;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("七日登录活动加载配置失败{0}", SevenDayConsts.LoginConfig), ex);
            }
        }

        /// <summary>
        /// 领取奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public ESevenDayActErrorCode HandleGetAward(GameClient client, int day)
        {
            int currDay;
            if (!SevenDayActivityMgr.Instance().IsInActivityTime(client, out currDay))
                return ESevenDayActErrorCode.NotInActivityTime;

            Dictionary<int, SevenDayItemData> actData = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Login);
            if (actData == null)
                return ESevenDayActErrorCode.NotInActivityTime;

            if (day <= 0 || day > currDay)
                return ESevenDayActErrorCode.NotReachCondition;

            Dictionary<int, _DayAward> tmpDayAwardDict = null;
            lock (ConfigMutex) { tmpDayAwardDict = this.DayAwardDict; }
            _DayAward dayAward = null;
            if (tmpDayAwardDict == null || !tmpDayAwardDict.TryGetValue(day, out dayAward))
                return ESevenDayActErrorCode.ServerConfigError;

            lock (actData)
            {
                SevenDayItemData data = null;
                if (!actData.TryGetValue(day, out data))
                    return ESevenDayActErrorCode.NotReachCondition;

                if (data.Params1 != SevenDayConsts.HadLoginFlag || data.AwardFlag == SevenDayConsts.HadGetAward)
                    return ESevenDayActErrorCode.NotReachCondition;

                // 检查背包
                int awardGoodsCnt = 0;
                if (dayAward.AllAward != null && dayAward.AllAward.GoodsDataList != null)
                    awardGoodsCnt += dayAward.AllAward.GoodsDataList.Count;
                if (dayAward.OccAward != null && dayAward.OccAward.GoodsDataList != null)
                    awardGoodsCnt += dayAward.OccAward.GoodsDataList.Count(_goods =>Global.IsRoleOccupationMatchGoods(client, _goods.GoodsID));
                if (dayAward.TimeAward != null)
                    awardGoodsCnt += dayAward.TimeAward.GoodsCnt();
                if (awardGoodsCnt <= 0 || !Global.CanAddGoodsNum(client, awardGoodsCnt))
                    return ESevenDayActErrorCode.NoBagSpace;

                data.AwardFlag = SevenDayConsts.HadGetAward;
                if (!SevenDayActivityMgr.Instance().UpdateDb(client.ClientData.RoleID, ESevenDayActType.Login, day, data, client.ServerId))
                {
                    data.AwardFlag = SevenDayConsts.NotGetAward;
                    return ESevenDayActErrorCode.DBFailed;
                }

                // 发奖
                if (!SevenDayActivityMgr.Instance().GiveAward(client, dayAward.AllAward, ESevenDayActType.Login)
                    || !SevenDayActivityMgr.Instance().GiveAward(client, dayAward.OccAward, ESevenDayActType.Login)
                    || !SevenDayActivityMgr.Instance().GiveEffectiveTimeAward(client, dayAward.TimeAward.ToAwardItem(), ESevenDayActType.Login)
                    )
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("玩家领取七日活动奖励，设置领奖了但是发奖失败 roleid={0}, day={1}", client.ClientData.RoleID, day));
                }

                return ESevenDayActErrorCode.Success;
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

            Dictionary<int, SevenDayItemData> actData = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Login);
            if (actData == null) return false;

            lock (actData)
            {
                foreach (var kvp in actData)
                {
                    var data = kvp.Value;
                    if (data.Params1 == SevenDayConsts.HadLoginFlag && data.AwardFlag != SevenDayConsts.HadGetAward)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 角色上线，跨天登录时检测
        /// </summary>
        /// <param name="client"></param>
        public void Update(GameClient client)
        {
            if (client == null) return;

            int currDay;
            if (!SevenDayActivityMgr.Instance().IsInActivityTime(client, out currDay)) return;

            Dictionary<int, SevenDayItemData> actData = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Login);
            lock (actData)
            {
                if (!actData.ContainsKey(currDay))
                {
                    // 今日首次登录，设置登录并且未领奖
                    SevenDayItemData itemData = new SevenDayItemData();
                    itemData.AwardFlag = SevenDayConsts.NotGetAward;
                    itemData.Params1 = SevenDayConsts.HadLoginFlag;
                    if (SevenDayActivityMgr.Instance().UpdateDb(client.ClientData.RoleID, ESevenDayActType.Login, currDay, itemData, client.ServerId))
                    {
                        actData[currDay] = itemData;
                    }
                }
            }
        }
    }
}
