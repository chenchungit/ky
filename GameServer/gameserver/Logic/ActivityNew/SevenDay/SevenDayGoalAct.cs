using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Data;
using GameServer.Core.GameEvent.EventOjectImpl;
using System.Xml.Linq;
using Server.Tools;
using GameServer.Core.GameEvent;

namespace GameServer.Logic.ActivityNew.SevenDay
{
    /// <summary>
    /// 七日目标
    /// </summary>
    class SevenDayGoalAct
    {
        class _GoalItemConfig
        {
            public int Id;
            public int Day;
            public int FuncType;
            public List<GoodsData> GoodsList;

            public int ExtCond1;
            public int ExtCond2;
            public int ExtCond3;
        }


        private Dictionary<ESevenDayGoalFuncType, Action<SevenDayGoalEventObject, List<int>, Dictionary<int, _GoalItemConfig>>> evHandlerDict = null;

        private object ConfigMutex = new object();
        private Dictionary<ESevenDayGoalFuncType, List<int>> Func2GoalId = null;
        private Dictionary<int, _GoalItemConfig> ItemConfigDict = null;

        public SevenDayGoalAct()
        {
            evHandlerDict = new Dictionary<ESevenDayGoalFuncType, Action<SevenDayGoalEventObject, List<int>, Dictionary<int, _GoalItemConfig>>>();

            //evHandlerDict[ESevenDayGoalFuncType.RoleLevelUp] = _Handle_RoleLevelUp;
            //evHandlerDict[ESevenDayGoalFuncType.SkillLevelUp] = _Handle_SkillLevelUp;
            //evHandlerDict[ESevenDayGoalFuncType.MoJingCntInBag] = _Handle_MoJingCntInBag;
            //evHandlerDict[ESevenDayGoalFuncType.RecoverMoJing] = _Handle_RecoverMoJing;
            //evHandlerDict[ESevenDayGoalFuncType.ExchangeJinHuaJingShiByMoJing] = _Handle_ExchangeJinHuaJingShiByMoJing;
            //evHandlerDict[ESevenDayGoalFuncType.JoinJingJiChangTimes] = _Handle_JoinJingJiChangTimes;
            //evHandlerDict[ESevenDayGoalFuncType.WinJingJiChangTimes] = _Handle_WinJingJiChangTimes;
            //evHandlerDict[ESevenDayGoalFuncType.JingJiChangRank] = _Handle_JingJiChangRank;
            //evHandlerDict[ESevenDayGoalFuncType.PeiDaiBlueUp] = _Handle_PeiDaiBlueUp;
            //evHandlerDict[ESevenDayGoalFuncType.PeiDaiPurpleUp] = _Handle_PeiDaiPurpleUp;
            //evHandlerDict[ESevenDayGoalFuncType.RecoverEquipBlueUp] = _Handle_RecoverEquipBlueUp;
            //evHandlerDict[ESevenDayGoalFuncType.MallInSaleCount] = _Handle_MallInSaleCount;
            //evHandlerDict[ESevenDayGoalFuncType.GetEquipCountByQiFu] = _Handle_GetEquipCountByQiFu;
            //evHandlerDict[ESevenDayGoalFuncType.PickUpEquipCount] = _Handle_PickUpEquipCount;
            //evHandlerDict[ESevenDayGoalFuncType.EquipChuanChengTimes] = _Handle_EquipChuanChengTimes;
            //evHandlerDict[ESevenDayGoalFuncType.EnterFuBenTimes] = _Handle_EnterFuBenTimes;
            //evHandlerDict[ESevenDayGoalFuncType.KillMonsterInMap] = _Handle_KillMonsterInMap;
            //evHandlerDict[ESevenDayGoalFuncType.JoinActivityTimes] = _Handle_JoinActivityTimes;
            //evHandlerDict[ESevenDayGoalFuncType.HeChengTimes] = _Handle_HeChengTimes;
            //evHandlerDict[ESevenDayGoalFuncType.UseGoodsCount] = _Handle_UseGoodsCount;
            //evHandlerDict[ESevenDayGoalFuncType.JinBiZhuanHuanTimes] = _Handle_JinBiZhuanHuanTimes;
            //evHandlerDict[ESevenDayGoalFuncType.BangZuanZhuanHuanTimes] = _Handle_BangZuanZhuanHuanTimes;
            //evHandlerDict[ESevenDayGoalFuncType.ZuanShiZhuanHuanTimes] = _Handle_ZuanShiZhuanHuanTimes;
            //evHandlerDict[ESevenDayGoalFuncType.ExchangeJinHuaJingShiByQiFuScore] = _Handle_ExchangeJinHuaJingShiByQiFuScore;
            //evHandlerDict[ESevenDayGoalFuncType.CombatChange] = _Handle_CombatChange;
            //evHandlerDict[ESevenDayGoalFuncType.PeiDaiForgeEquip] = _Handle_PeiDaiForgeEquip;
            //evHandlerDict[ESevenDayGoalFuncType.ForgeEquipLevel] = _Handle_ForgeEquipLevel;
            //evHandlerDict[ESevenDayGoalFuncType.ForgeEquipTimes] = _Handle_ForgeEquipTimes;
            //evHandlerDict[ESevenDayGoalFuncType.CompleteChengJiu] = _Handle_CompleteChengJiu;
            //evHandlerDict[ESevenDayGoalFuncType.ChengJiuLevel] = _Handle_ChengJiuLevel;
            //evHandlerDict[ESevenDayGoalFuncType.JunXianLevel] = _Handle_JunXianLevel;
            //evHandlerDict[ESevenDayGoalFuncType.PeiDaiAppendEquip] = _Handle_PeiDaiAppendEquip;
            //evHandlerDict[ESevenDayGoalFuncType.AppendEquipLevel] = _Handle_AppendEquipLevel;
            //evHandlerDict[ESevenDayGoalFuncType.AppendEquipTimes] = _Handle_AppendEquipTimes;
            //evHandlerDict[ESevenDayGoalFuncType.ActiveXingZuo] = _Handle_ActiveXingZuo;
            //evHandlerDict[ESevenDayGoalFuncType.GetSpriteCountBuleUp] = _Handle_GetSpriteCountBuleUp;
            //evHandlerDict[ESevenDayGoalFuncType.GetSpriteCountPurpleUp] = _Handle_GetSpriteCountPurpleUp;
            //evHandlerDict[ESevenDayGoalFuncType.WingLevel] = _Handle_WingLevel;
            //evHandlerDict[ESevenDayGoalFuncType.WingSuitStarTimes] = _Handle_WingSuitStarTimes;
            //evHandlerDict[ESevenDayGoalFuncType.CompleteTuJian] = _Handle_CompleteTuJian;
            //evHandlerDict[ESevenDayGoalFuncType.PeiDaiSuitEquipCount] = _Handle_PeiDaiSuitEquipCount;
            //evHandlerDict[ESevenDayGoalFuncType.PeiDaiSuitEquipLevel] = _Handle_PeiDaiSuitEquipLevel;
            //evHandlerDict[ESevenDayGoalFuncType.EquipSuitUpTimes] = _Handle_EquipSuitUpTimes;
        }

        /// <summary>
        /// 加载配置文件，线程安全
        /// </summary>
        public void LoadConfig()
        {
            Dictionary<ESevenDayGoalFuncType, List<int>> tmpFunc2GoalId = new Dictionary<ESevenDayGoalFuncType, List<int>>();
            Dictionary<int, _GoalItemConfig> tmpItemConfigDict = new Dictionary<int, _GoalItemConfig>();
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(SevenDayConsts.GoalConfig));
                foreach (var xmlItem in xml.Elements())
                {
                    _GoalItemConfig itemConfig = new _GoalItemConfig();
                    itemConfig.Id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    itemConfig.Day = (int)Global.GetSafeAttributeLong(xmlItem, "Day");
                    itemConfig.FuncType = (int)Global.GetSafeAttributeLong(xmlItem, "FunctionType");

                    string[] szCond = Global.GetSafeAttributeStr(xmlItem, "TypeGoal").Split(',');
                    itemConfig.ExtCond1 = szCond.Length >= 1 ? Convert.ToInt32(szCond[0]) : 0;
                    itemConfig.ExtCond2 = szCond.Length >= 2 ? Convert.ToInt32(szCond[1]) : 0;
                    itemConfig.ExtCond3 = szCond.Length >= 3 ? Convert.ToInt32(szCond[2]) : 0;

                    string[] szGoods = Global.GetSafeAttributeStr(xmlItem, "Award").Split('|');
                    itemConfig.GoodsList = HuodongCachingMgr.ParseGoodsDataList(szGoods, "七日目标");

                    if (!tmpFunc2GoalId.ContainsKey((ESevenDayGoalFuncType)itemConfig.FuncType))
                        tmpFunc2GoalId[(ESevenDayGoalFuncType)itemConfig.FuncType] = new List<int>();

                    tmpFunc2GoalId[(ESevenDayGoalFuncType)itemConfig.FuncType].Add(itemConfig.Id);
                    tmpItemConfigDict.Add(itemConfig.Id, itemConfig);
                }

                lock (ConfigMutex)
                {
                    Func2GoalId = tmpFunc2GoalId;
                    ItemConfigDict = tmpItemConfigDict;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("七日登录活动加载配置失败{0}", SevenDayConsts.GoalConfig), ex);
            }
        }

        /// <summary>
        /// 获取物品颜色，太蛋疼了，
        /// 白、绿、蓝、紫、紫闪
        /// 0、1、2、3、4
        /// </summary>
        /// <param name="ExcellencePropNum"></param>
        /// <returns></returns>
        private int GetColor__(int ExcellencePropNum)
        {
            int color = 0;
            if (ExcellencePropNum == 0)
                color = 0;
            else if (ExcellencePropNum >= 1 && ExcellencePropNum <= 2)
                color = 1;
            else if (ExcellencePropNum >= 3 && ExcellencePropNum <= 4)
                color = 2;
            else if (ExcellencePropNum >= 5 && ExcellencePropNum <= 6)
                color = 3;
            else
                color = 4;

            return color;
        }

        /// <summary>
        /// 是否有可以领取的奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool HasAnyAwardCanGet(GameClient client, out bool[] bGoalDay)
        {
            bGoalDay = new bool[SevenDayConsts.DayCount];
            for (int i = 0; i < bGoalDay.Length; ++i)
                bGoalDay[i] = false;

            if (client == null) return false;

            bool bResult = false;
            int currDay;
            if (!SevenDayActivityMgr.Instance().IsInActivityTime(client, out currDay)) return false;
            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Goal);
            if (itemDict == null || itemDict.Count <= 0) return false;

            Dictionary<ESevenDayGoalFuncType, List<int>> tmpFunc2GoalId = null;
            Dictionary<int, _GoalItemConfig> tmpItemConfigDict = null;

            lock (ConfigMutex)
            {
                tmpFunc2GoalId = this.Func2GoalId;
                tmpItemConfigDict = this.ItemConfigDict;
            }

            if (tmpFunc2GoalId == null || tmpItemConfigDict == null)
                return false;

            lock (itemDict)
            {
                foreach (var kvp in itemDict)
                {
                    _GoalItemConfig itemConfig = null;
                    if (!tmpItemConfigDict.TryGetValue(kvp.Key, out itemConfig))
                        continue;

                    if (itemConfig.Day > currDay)
                        continue;

                    if (itemConfig.Day <= 0 && itemConfig.Day > bGoalDay.Length)
                        continue;

                    if (bGoalDay[itemConfig.Day - 1] == true)
                        continue;

                    if (!CheckCanGetAward(client, kvp.Value, itemConfig))
                        continue;

                    bGoalDay[itemConfig.Day - 1] = true;
                    bResult = true;
                }
            }

            return bResult;
        }

        /// <summary>
        /// 领取七日活动的奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="id"></param>
        /// <returns>领取的七日目标的ID</returns>
        public ESevenDayActErrorCode HandleGetAward(GameClient client, int id)
        {
            int dayIdx;
            if (!SevenDayActivityMgr.Instance().IsInActivityTime(client, out dayIdx))
                return ESevenDayActErrorCode.NotInActivityTime;

            Dictionary<ESevenDayGoalFuncType, List<int>> tmpFunc2GoalId = null;
            Dictionary<int, _GoalItemConfig> tmpItemConfigDict = null;

            lock (ConfigMutex)
            {
                tmpFunc2GoalId = this.Func2GoalId;
                tmpItemConfigDict = this.ItemConfigDict;
            }

            if (tmpFunc2GoalId == null || tmpItemConfigDict == null)
                return ESevenDayActErrorCode.ServerConfigError;

            _GoalItemConfig itemConfig = null;
            if (!tmpItemConfigDict.TryGetValue(id, out itemConfig) || itemConfig.GoodsList == null)
                return ESevenDayActErrorCode.ServerConfigError;

            if (itemConfig.Day > dayIdx)
                return ESevenDayActErrorCode.NotReachCondition;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Goal);
            if (itemDict == null)
                return ESevenDayActErrorCode.NotReachCondition;

            lock (itemDict)
            {
                SevenDayItemData itemData = null;
                if (!itemDict.TryGetValue(id, out itemData))
                    return ESevenDayActErrorCode.NotReachCondition;

                if (!CheckCanGetAward(client, itemData, itemConfig))
                    return ESevenDayActErrorCode.NotReachCondition;

                if (!Global.CanAddGoodsNum(client, itemConfig.GoodsList.Count))
                    return ESevenDayActErrorCode.NoBagSpace;
                
                itemData.AwardFlag = SevenDayConsts.HadGetAward;
                if (!SevenDayActivityMgr.Instance().UpdateDb(client.ClientData.RoleID, ESevenDayActType.Goal, id, itemData, client.ServerId))
                {
                    itemData.AwardFlag = SevenDayConsts.NotGetAward;
                    return ESevenDayActErrorCode.DBFailed;
                }

                if (!SevenDayActivityMgr.Instance().GiveAward(client, new AwardItem() {GoodsDataList = itemConfig.GoodsList }, ESevenDayActType.Goal))
                {

                }
            }

            return ESevenDayActErrorCode.Success;
        }

        /// <summary>
        /// 检查是否能够领取奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <param name="itemConfig"></param>
        /// <returns></returns>
        private bool CheckCanGetAward(GameClient client, SevenDayItemData data, _GoalItemConfig itemConfig)
        {
            if (data == null || itemConfig == null || client == null)
                return false;

            if (data.AwardFlag != SevenDayConsts.NotGetAward)
                return false;

            switch (itemConfig.FuncType)
            {
                // 需要记录数据库
                case (int)ESevenDayGoalFuncType.RecoverMoJing:
                                case (int)ESevenDayGoalFuncType.JoinJingJiChangTimes:
                case (int)ESevenDayGoalFuncType.WinJingJiChangTimes:
                case (int)ESevenDayGoalFuncType.RecoverEquipBlueUp:
                case (int)ESevenDayGoalFuncType.MallInSaleCount:
                case (int)ESevenDayGoalFuncType.GetEquipCountByQiFu:
                case (int)ESevenDayGoalFuncType.PickUpEquipCount:
                case (int)ESevenDayGoalFuncType.EquipChuanChengTimes:
                case (int)ESevenDayGoalFuncType.BangZuanZhuanHuanTimes:
                case (int)ESevenDayGoalFuncType.JinBiZhuanHuanTimes:
                case (int)ESevenDayGoalFuncType.ZuanShiZhuanHuanTimes:
                case (int)ESevenDayGoalFuncType.ForgeEquipLevel:
                case (int)ESevenDayGoalFuncType.ForgeEquipTimes:
                case (int)ESevenDayGoalFuncType.WingSuitStarTimes:
                case (int)ESevenDayGoalFuncType.EquipSuitUpTimes:
                case (int)ESevenDayGoalFuncType.AppendEquipLevel:
                case (int)ESevenDayGoalFuncType.AppendEquipTimes:
                    {
                        return data.Params1 >= itemConfig.ExtCond1;
                    }

                // 需要记录数据库
                case (int)ESevenDayGoalFuncType.EnterFuBenTimes:
                case (int)ESevenDayGoalFuncType.JoinActivityTimes:
                case (int)ESevenDayGoalFuncType.HeChengTimes:
                case (int)ESevenDayGoalFuncType.UseGoodsCount:
                case (int)ESevenDayGoalFuncType.ExchangeJinHuaJingShiByQiFuScore:
                case (int)ESevenDayGoalFuncType.ExchangeJinHuaJingShiByMoJing:
                    {
                        return data.Params1 >= itemConfig.ExtCond2;
                    }

                // 需要记录数据库
                case (int)ESevenDayGoalFuncType.KillMonsterInMap:
                    {
                        return data.Params1 >= itemConfig.ExtCond3;
                    }

                // 需要记录数据库
                case (int)ESevenDayGoalFuncType.JingJiChangRank:
                    {
                        return data.Params1 >= 1 && data.Params1 <= itemConfig.ExtCond1;
                    }

                // 不需要计入数据库
                case (int)ESevenDayGoalFuncType.SkillLevelUp:
                case (int)ESevenDayGoalFuncType.MoJingCntInBag:
                case (int)ESevenDayGoalFuncType.PeiDaiBlueUp:
                case (int)ESevenDayGoalFuncType.PeiDaiPurpleUp:
                case (int)ESevenDayGoalFuncType.CombatChange:
                case (int)ESevenDayGoalFuncType.ChengJiuLevel:
                case (int)ESevenDayGoalFuncType.JunXianLevel:
                case (int)ESevenDayGoalFuncType.GetSpriteCountBuleUp:
                case (int)ESevenDayGoalFuncType.GetSpriteCountPurpleUp:
                case (int)ESevenDayGoalFuncType.PeiDaiSuitEquipLevel:
                    {
                        return data.Params1 >= itemConfig.ExtCond1;
                    }

                // 不需要计入数据库
                case (int)ESevenDayGoalFuncType.PeiDaiForgeEquip:
                case (int)ESevenDayGoalFuncType.PeiDaiAppendEquip:
                case (int)ESevenDayGoalFuncType.PeiDaiSuitEquipCount:
                case (int)ESevenDayGoalFuncType.ActiveXingZuo:
                    {
                        return data.Params1 >= itemConfig.ExtCond2;
                    }

                // 不需要计入数据库
                case (int)ESevenDayGoalFuncType.CompleteChengJiu:
                case (int)ESevenDayGoalFuncType.CompleteTuJian:
                    {
                        return data.Params1 == SevenDayConsts.HadFinishFlag;
                    }

                // 不需要计入数据库
                case (int)ESevenDayGoalFuncType.RoleLevelUp:
                case (int)ESevenDayGoalFuncType.WingLevel:
                    {
                        return (data.Params1 > itemConfig.ExtCond1) || (data.Params1 == itemConfig.ExtCond1 && data.Params2 >= itemConfig.ExtCond2);
                    }
                default:
                    return false;
            }

            return false;
        }

        public void Update(GameClient client)
        {
            // 不存储数据库的，上线时候强制计算，(领奖记录会存数据库，计算的时候切莫把领奖记录重置！！！)
            if (SevenDayActivityMgr.Instance().IsInActivityTime(client))
            {
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.RoleLevelUp));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.SkillLevelUp));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.CombatChange));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.CompleteChengJiu));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.ChengJiuLevel));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.JunXianLevel));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.CompleteTuJian));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.WingLevel));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.ActiveXingZuo));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.GetSpriteCountBuleUp));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.GetSpriteCountPurpleUp));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.PeiDaiAppendEquip));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.PeiDaiForgeEquip));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.PeiDaiBlueUp));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.PeiDaiPurpleUp));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.PeiDaiSuitEquipLevel));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.PeiDaiSuitEquipCount));
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.MallInSaleCount));
            }
        }

        public void HandleEvent(SevenDayGoalEventObject evObj)
        {
            if (evObj == null)
            {
                return;
            }

            Action<SevenDayGoalEventObject, List<int>, Dictionary<int, _GoalItemConfig>> handler = null;
            if (!evHandlerDict.TryGetValue(evObj.FuncType, out handler))
            {
                return;
            }

            List<int> tmpGoalIdList = null;
            Dictionary<int, _GoalItemConfig> tmpGoalConfigDict = null;

            // 为了支持热加载，配置从统一的入口读出来，传给各个处理函数
            lock (ConfigMutex)
            {
                if (!this.Func2GoalId.TryGetValue(evObj.FuncType, out tmpGoalIdList)
                    || tmpGoalIdList.Count <= 0)
                {
                    return;
                }

                if ((tmpGoalConfigDict = this.ItemConfigDict) == null
                    || tmpGoalConfigDict.Count <= 0)
                {
                    return;
                }
            }

            handler(evObj, tmpGoalIdList, tmpGoalConfigDict);
        }

        #region 处理七日目标事件
        /// <summary>
        /// 玩家升级，不需要存数据库，上线时需要计算
        /// </summary>
        /// <param name="evObj"></param>
        private void _Handle_RoleLevelUp(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    itemData.Params1 = evObj.Client.ClientData.ChangeLifeCount;
                    itemData.Params2 = evObj.Client.ClientData.Level;
                }
            }
        }

        /// <summary>
        /// 技能升级，不需要存数据库，上线时需要计算
        /// </summary>
        /// <param name="evObj"></param>
        private void _Handle_SkillLevelUp(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;
            if (evObj.Client.ClientData.SkillDataList == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                // 先把到达各个等级的技能个数置为0
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }
                    itemData.Params1 = 0;
                }

                bool bHadAddDefault = false;

                for (int i = 0; i < evObj.Client.ClientData.SkillDataList.Count; ++i)
                {
                    if (Global.GetPrevSkilID(evObj.Client.ClientData.SkillDataList[i].SkillID) > 0)
                    {
                        continue;
                    }

                    if (evObj.Client.ClientData.SkillDataList[i].DbID == -1)
                    {
                        if (bHadAddDefault)
                            continue;

                        bHadAddDefault = true;
                    }

                    int skillLevel = evObj.Client.ClientData.SkillDataList[i].SkillLevel;

                    foreach (var goalId in goalIdList)
                    {
                        _GoalItemConfig itemConfig = null;
                        if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                            continue;

                        if (skillLevel >= itemConfig.ExtCond2)
                        {
                            SevenDayItemData itemData = null;
                            if (!itemDict.TryGetValue(goalId, out itemData))
                            {
                                itemData = new SevenDayItemData();
                                itemData.Params1 = 0;
                                itemData.AwardFlag = SevenDayConsts.NotGetAward;
                                itemDict[goalId] = itemData;
                            }
                            itemData.Params1++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 最大魔晶值，曾经，需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_MoJingCntInBag(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            int value = GameManager.ClientMgr.GetTianDiJingYuanValue(evObj.Client);

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    if (value > itemData.Params1)
                    {
                        int oldValue = itemData.Params1;
                        itemData.Params1 = value;
                        if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                        {
                            itemData.Params1 = oldValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 通过回收获得魔晶[0] 需要存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_RecoverMoJing(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1 += evObj.Arg1;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1 -= evObj.Arg1;
                    }
                }
            }
        }

        /// <summary>
        /// 魔晶兑换进化晶石的次数，需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_ExchangeJinHuaJingShiByMoJing(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    // 是否是关注的物品(进化晶石)
                    if (itemConfig.ExtCond1 != evObj.Arg1)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 进入竞技场次数，需要保存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_JoinJingJiChangTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 竞技场次数胜利次数，需要保存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_WinJingJiChangTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 在竞技场中排名达到X或X以上, 需要记录，曾经最大值
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_JingJiChangRank(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;
            if (evObj.Arg1 < 1) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = -1;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    if (itemData.Params1 < 1 || itemData.Params1 > evObj.Arg1)
                    {
                        int oldRank = itemData.Params1;
                        itemData.Params1 = evObj.Arg1;

                        if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                        {
                            itemData.Params1 = oldRank;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 佩戴xxx件蓝色或更高品质的装备，不需要记数据库，上线加载
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_PeiDaiBlueUp(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            List<int> exceList = evObj.Client.UsingEquipMgr.GetUsingEquipExcellencePropNum();
            int equipCnt = exceList != null ? exceList.Count(_e => this.GetColor__(_e) >= 2) : 0;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    itemData.Params1 = equipCnt;
                }
            }
        }

        /// <summary>
        /// 佩戴[0]紫色或更高品质的装备，不需要记数据库，上线加载
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_PeiDaiPurpleUp(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            List<int> exceList = evObj.Client.UsingEquipMgr.GetUsingEquipExcellencePropNum();
            int equipCnt = exceList != null ? exceList.Count(_e => this.GetColor__(_e) >= 3) : 0;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    itemData.Params1 = equipCnt;
                }
            }
        }

        /// <summary>
        /// 分解[0]件蓝色或更高品质装备, 需要记录数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_RecoverEquipBlueUp(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            int categoriy = Global.GetGoodsCatetoriy(evObj.Arg1);
            bool isEquip = false;
            if (categoriy >= (int)ItemCategories.TouKui && categoriy <= (int)ItemCategories.JieZhi) { isEquip = true; }
            else if (categoriy >= (int)ItemCategories.WuQi_Jian && categoriy < (int)ItemCategories.EquipMax) { isEquip = true; }
            if (!isEquip) return;

            if (this.GetColor__(evObj.Arg3) < 2) return; // 低于蓝色

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1 += evObj.Arg2;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1 -= evObj.Arg2;
                    }
                }
            }
        }

        /// <summary>
        /// 交易所上交物品 实时 ，不需要记数据库，上线加载
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_MallInSaleCount(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;
            if (evObj.Client.ClientData.SaleGoodsDataList == null) return;

            int equipCount = 0;
            lock (evObj.Client.ClientData.SaleGoodsDataList)
            {
                foreach (var gd in evObj.Client.ClientData.SaleGoodsDataList)
                {
                    if (gd == null) continue;
                    int categoriy = Global.GetGoodsCatetoriy(gd.GoodsID);
                    if (categoriy >= (int)ItemCategories.TouKui && categoriy <= (int)ItemCategories.JieZhi) { equipCount++; }
                    else if (categoriy >= (int)ItemCategories.WuQi_Jian && categoriy < (int)ItemCategories.EquipMax) { equipCount++; }
                }
            }

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    itemData.Params1 = equipCount;
                }
            }
        }

        /// <summary>
        /// 通过祈福获得XXX件装备, 需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_GetEquipCountByQiFu(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            // 判断是否是装备
            int categoriy = Global.GetGoodsCatetoriy(evObj.Arg1);
            if (categoriy >= (int)ItemCategories.TouKui && categoriy <= (int)ItemCategories.JieZhi) { }
            else if (categoriy >= (int)ItemCategories.WuQi_Jian && categoriy < (int)ItemCategories.EquipMax) { }
            else return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1 += evObj.Arg2;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1 -= evObj.Arg2;
                    }
                }
            }
        }

        /// <summary>
        /// 拾取X件装备, 累加，需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_PickUpEquipCount(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            // 判断是否是装备
            int categoriy = Global.GetGoodsCatetoriy(evObj.Arg1);
            if (categoriy >= (int)ItemCategories.TouKui && categoriy <= (int)ItemCategories.JieZhi) { }
            else if (categoriy >= (int)ItemCategories.WuQi_Jian && categoriy < (int)ItemCategories.EquipMax) { }
            else return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1 += evObj.Arg2;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1 -= evObj.Arg2;
                    }
                }
            }
        }

        /// <summary>
        /// 装备传承，需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_EquipChuanChengTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 已完成并领奖的就不更新了
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 进入副本次数，需要存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_EnterFuBenTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;
            if (evObj.Arg2 <= 0) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 已完成并领奖的就不更新了
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    // 不关注该副本
                    if (itemConfig.ExtCond1 != evObj.Arg1)
                        continue;

                    itemData.Params1 += evObj.Arg2;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1 -= evObj.Arg2;
                    }
                }
            }
        }

        /// <summary>
        /// 杀死指定地图的指定怪物X个，累加，需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_KillMonsterInMap(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 已完成并领奖的就不更新了
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    // 不关注该地图或者怪物
                    if (itemConfig.ExtCond1 != evObj.Arg1
                        || itemConfig.ExtCond2 != evObj.Arg2)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 参与血色、恶魔 X 次，需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_JoinActivityTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 已完成并领奖的就不更新了
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    // 不关注该活动
                    if (itemConfig.ExtCond1 != evObj.Arg1)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 合成指定道具X个 需要存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_HeChengTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 已完成并领奖的就不更新了
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    // 不是关注的物品
                    if (itemConfig.ExtCond1 != evObj.Arg1)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 使用指定道具X个  需要存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_UseGoodsCount(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;
            if (evObj.Arg2 <= 0) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 已完成并领奖的就不更新了
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    // 不是关注的物品
                    if (itemConfig.ExtCond1 != evObj.Arg1)
                        continue;

                    itemData.Params1 += evObj.Arg2;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1 -= evObj.Arg2;
                    }
                }
            }
        }

        /// <summary>
        /// 金币转换次数，需要存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_JinBiZhuanHuanTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已领奖的不再更新
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 绑钻转换次数，需要存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_BangZuanZhuanHuanTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已领奖的不再更新
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 钻石转换次数，需要存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_ZuanShiZhuanHuanTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已领奖的不再更新
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 使用祈福积分兑换进化晶石次数，需要记录数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_ExchangeJinHuaJingShiByQiFuScore(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不在更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    // 是否是关注的物品(进化晶石)
                    if (itemConfig.ExtCond1 != evObj.Arg1)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        ///  战斗力变化，不存数据库，上线计算
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_CombatChange(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }
                    itemData.Params1 = evObj.Client.ClientData.CombatForce;
                }
            }
        }

        /// <summary>
        /// 佩戴强化等级X或以上的装备X个, 不需要记数据库，上线加载
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_PeiDaiForgeEquip(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            List<int> forgeList = evObj.Client.UsingEquipMgr.GetUsingEquipForge();
            //    if (appendList == null || appendList.Count() <= 0) return; 不能加这句话，防止最后一件装备脱掉后，无法往下走

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    itemData.Params1 = forgeList != null
                        ? forgeList.Count(_forge => _forge >= itemConfig.ExtCond1)
                        : 0;
                }
            }
        }

        /// <summary>
        /// 强化等级最高的装备达到+[0] 需要存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_ForgeEquipLevel(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已完成的就不更新了
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    if (evObj.Arg1 > itemData.Params1)
                    {
                        int oldValue = itemData.Params1;
                        itemData.Params1 = evObj.Arg1;
                        if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                        {
                            itemData.Params1 = oldValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 进行了多少次强化，需要存数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_ForgeEquipTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已完成的就不更新了
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 完成成就，不存数据库，角色上线时初始化
        /// </summary>
        /// <param name="evObj"></param>
        private void _Handle_CompleteChengJiu(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = SevenDayConsts.NotFinishFlag;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    if (ChengJiuManager.IsChengJiuCompleted(evObj.Client, itemConfig.ExtCond1))
                    {
                        itemData.Params1 = SevenDayConsts.HadFinishFlag;
                    }
                }
            }
        }

        /// <summary>
        /// 成就等级，不存数据库，角色上线时初始化
        /// </summary>
        /// <param name="evObj"></param>
        private void _Handle_ChengJiuLevel(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }
                    itemData.Params1 = ChengJiuManager.GetChengJiuLevel(evObj.Client);
                }
            }
        }

        /// <summary>
        /// 成就等级，不存数据库，角色上线时初始化
        /// </summary>
        /// <param name="evObj"></param>
        private void _Handle_JunXianLevel(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }
                    itemData.Params1 = GameManager.ClientMgr.GetShengWangLevelValue(evObj.Client);
                }
            }
        }

        /// <summary>
        /// 佩戴追加等级X或以上的装备X个,  不需要记数据库，上线计算
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_PeiDaiAppendEquip(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            List<int> appendList = evObj.Client.UsingEquipMgr.GetUsingEquipAppend();
            //if (appendList == null || appendList.Count() <= 0) return; 不能加这句话，防止脱最后一件装备的时候，无法往下走

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    itemData.Params1 = appendList != null
                         ? appendList.Count(_append => _append >= itemConfig.ExtCond1)
                         : 0;
                }
            }
        }

        /// <summary>
        /// 最大追加等级达到[0],  需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_AppendEquipLevel(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;
            if (evObj.Arg1 <= 0) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已领奖的不再更新
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    if (evObj.Arg1 > itemData.Params1)
                    {
                        int oldLvl = itemData.Params1;
                        itemData.Params1 = evObj.Arg1;
                        if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                        {
                            itemData.Params1 = oldLvl;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 进行[0]次追加,  需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_AppendEquipTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已领奖的不再更新
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 激活星座，不需要存数据库，上线计算
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_ActiveXingZuo(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;
            if (evObj.Client.ClientData.RoleStarConstellationInfo == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    int star = 0;
                    if (evObj.Client.ClientData.RoleStarConstellationInfo.TryGetValue(itemConfig.ExtCond1, out star))
                    {
                        itemData.Params1 = star;
                    }
                }
            }
        }

        /// <summary>
        /// 入库[0]个蓝色或更高品质精灵，不需要存数据库，上线计算
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_GetSpriteCountBuleUp(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            int spriteCount = 0;
            if (evObj.Client.ClientData.DamonGoodsDataList != null)
            {
                evObj.Client.ClientData.DamonGoodsDataList.ForEach(_sprite =>
                {
                    if (_sprite.Site == (int)SaleGoodsConsts.UsingDemonGoodsID &&  Global.GetGoodsColorEx(_sprite) >= 2)
                        ++spriteCount;
                }); 
            }

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已领取的奖励，不再更新
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1 = spriteCount;
                }
            }
        }

        /// <summary>
        /// 入库[0]个紫色或更高品质精灵，不需要存数据库，上线计算
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_GetSpriteCountPurpleUp(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            int spriteCount = 0;
            if (evObj.Client.ClientData.DamonGoodsDataList != null)
            {
                evObj.Client.ClientData.DamonGoodsDataList.ForEach(_sprite =>
                {
                    if (_sprite.Site == (int)SaleGoodsConsts.UsingDemonGoodsID && Global.GetGoodsColorEx(_sprite) >= 3)
                        ++spriteCount;
                });
            }

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已领取的奖励，不再更新
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1 = spriteCount;
                }
            }
        }

        /// <summary>
        /// 翅膀等级星阶，不需要存数据库，上线计算
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_WingLevel(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;
            if (evObj.Client.ClientData.MyWingData == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    itemData.Params1 = evObj.Client.ClientData.MyWingData.WingID;
                    itemData.Params2 = evObj.Client.ClientData.MyWingData.ForgeLevel;
                }
            }
        }

        /// <summary>
        /// 翅膀升级和升阶次数，需要保存数据库，上线不计算
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_WingSuitStarTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;
            if (evObj.Client.ClientData.MyWingData == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }
                    else if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                    {
                        // 优化处理，已经领奖了，没有更新数据了，只需要知道领奖了就够了
                        //  continue;
                    }

                    itemData.Params1++;

                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }

        /// <summary>
        /// 激活图鉴，不需要存数据库，上线计算
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_CompleteTuJian(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = SevenDayConsts.NotFinishFlag;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    if (evObj.Client.ClientData.ActivedTuJianItem != null && evObj.Client.ClientData.ActivedTuJianItem.Contains(itemConfig.ExtCond1))
                        itemData.Params1 = SevenDayConsts.HadFinishFlag;
                    else
                        itemData.Params1 = SevenDayConsts.NotFinishFlag;
                }
            }
        }

        /// <summary>
        /// 佩戴[1]个[0]阶装备，实时，不需要记数据库，上线加载
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_PeiDaiSuitEquipCount(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            List<int> suitList = evObj.Client.UsingEquipMgr.GetUsingEquipSuit();
            //if (appendList == null || appendList.Count() <= 0) return; 不能加这句话，防止脱最后一件装备的时候，无法往下走

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    itemData.Params1 = suitList != null
                         ? suitList.Count(_suit => _suit >= itemConfig.ExtCond1)
                         : 0;
                }
            }
        }

        /// <summary>
        /// 佩戴装备最高达到[0]阶，实时，不需要记数据库，上线加载
        /// </summary>
        /// <param name="evObj"></param>
        /// <param name="goalIdList"></param>
        /// <param name="goalConfigDict"></param>
        private void _Handle_PeiDaiSuitEquipLevel(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            List<int> suitList = evObj.Client.UsingEquipMgr.GetUsingEquipSuit();
            //if (appendList == null || appendList.Count() <= 0) return; 不能加这句话，防止脱最后一件装备的时候，无法往下走

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    _GoalItemConfig itemConfig = null;
                    if (!goalConfigDict.TryGetValue(goalId, out itemConfig))
                        continue;

                    itemData.Params1 = suitList != null && suitList.Count > 0 ? suitList.Max() : 0;
                }
            }
        }

        /// <summary>
        /// 进行[0]次装备进阶，需要记数据库
        /// </summary>
        /// <param name="evObj"></param>
        private void _Handle_EquipSuitUpTimes(SevenDayGoalEventObject evObj, List<int> goalIdList, Dictionary<int, _GoalItemConfig> goalConfigDict)
        {
            if (evObj == null || evObj.Client == null) return;

            List<int> suitList = evObj.Client.UsingEquipMgr.GetUsingEquipSuit();
            //if (appendList == null || appendList.Count() <= 0) return; 不能加这句话，防止脱最后一件装备的时候，无法往下走

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(evObj.Client, ESevenDayActType.Goal);
            lock (itemDict)
            {
                foreach (var goalId in goalIdList)
                {
                    SevenDayItemData itemData = null;
                    if (!itemDict.TryGetValue(goalId, out itemData))
                    {
                        itemData = new SevenDayItemData();
                        itemData.Params1 = 0;
                        itemData.AwardFlag = SevenDayConsts.NotGetAward;
                        itemDict[goalId] = itemData;
                    }

                    // 优化，已经领奖的不再更新db
                    if (itemData.AwardFlag == SevenDayConsts.HadGetAward)
                        continue;

                    itemData.Params1++;
                    if (!SevenDayActivityMgr.Instance().UpdateDb(evObj.Client.ClientData.RoleID, ESevenDayActType.Goal, goalId, itemData, evObj.Client.ServerId))
                    {
                        itemData.Params1--;
                    }
                }
            }
        }
        #endregion
    }
}
