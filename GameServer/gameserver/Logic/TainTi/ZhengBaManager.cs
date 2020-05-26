#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Windows;
using System.Diagnostics;

using Server.Tools.Pattern;
using GameServer.Server;
using GameServer.Core.GameEvent;
using Tmsk.Contract;
using Server.Data;
using GameServer.Core.Executor;
using KF.Contract.Data;
using KF.Client;
using Server.Tools;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic.JingJiChang;
using GameServer.Tools;

namespace GameServer.Logic
{
    class ZhengBaMatchAward
    {
        public int AwardId;
        public string Name;
        public int FinalPassDay;
        public List<GoodsData> GoodsList;
    }

    /// <summary>
    /// 众神争霸
    /// </summary>
    public class ZhengBaManager : SingletonTemplate<ZhengBaManager>, IManager, ICmdProcessorEx, IEventListenerEx, IEventListener
    {
        private ZhengBaManager() { }

        #region copy
        class ZhengBaCopyScene
        {
            public int FuBenSeq;
            public int GameId;
            public int MapCode;
            public CopyMap CopyMap;

            public int RoleId1;
            public bool IsMirror1;
            public PlayerJingJiData JingJiData1;
            public Robot Robot1;

            public int RoleId2;
            public bool IsMirror2;
            public PlayerJingJiData JingJiData2;
            public Robot Robot2;

            public long m_lPrepareTime = 0;
            public long m_lBeginTime = 0;
            public long m_lEndTime = 0;
            public long m_lLeaveTime = 0;
            public GameSceneStatuses m_eStatus = GameSceneStatuses.STATUS_NULL;
            public GameSceneStateTimeData StateTimeData = new GameSceneStateTimeData();

            public int Winner = 0;
            public int FirstLeaveRoleId = 0;
        }

        class GameSideInfo
        {
            public int FuBenSeq;
            public int CurrSide;
        }

        private Dictionary<int, ZhengBaCopyScene> FuBenSeq2CopyScenes = new Dictionary<int, ZhengBaCopyScene>();
        private Dictionary<int, GameSideInfo> GameId2FuBenSeq = new Dictionary<int, GameSideInfo>();
        private long NextHeartBeatMs = TimeUtil.NOW();
        #endregion

        #region Config Data
        private ZhengBaConfig _Config = new ZhengBaConfig();
        private List<ZhengBaMatchAward> _MatchAwardList = new List<ZhengBaMatchAward>();
        #endregion

        #region Runtime Data
        private object Mutex = new object();

        private ZhengBaSyncData SyncData = new ZhengBaSyncData();
        private TimeSpan DiffKfCenter = TimeSpan.Zero;
        
        private Dictionary<int, TianTiPaiHangRoleData> RoleDataDict = new Dictionary<int, TianTiPaiHangRoleData>();
        private List<TianTiPaiHangRoleData> RoleDataList = new List<TianTiPaiHangRoleData>();
        private List<TianTiPaiHangRoleData> Top16RoleList = new List<TianTiPaiHangRoleData>();
        private List<ZhengBaSupportAnalysisData> SupportDatas = new List<ZhengBaSupportAnalysisData>();
        private Dictionary<int, PlayerJingJiData> MirrorDatas = new Dictionary<int, PlayerJingJiData>();
        private int MaxSupportGroup = 0;
        private int MaxOpposeGroup = 0;

        private Queue<ZhengBaPkLogData> PkLogQ = new Queue<ZhengBaPkLogData>();
        private Dictionary<int, Queue<ZhengBaSupportLogData>> SupportLogs = new Dictionary<int, Queue<ZhengBaSupportLogData>>();
        private List<ZhengBaWaitYaZhuAwardData> WaitAwardOfYaZhuList = new List<ZhengBaWaitYaZhuAwardData>();
        #endregion

        #region Implement Interface `IManager`
        public bool initialize()
        {
            if (!_Config.Load(
                Global.GameResPath(ZhengBaConsts.MatchConfigFile),
                Global.GameResPath(ZhengBaConsts.SupportConfigFile),
                Global.GameResPath(ZhengBaConsts.BirthPointConfigFile)))
            {
                return false;
            }

            XElement xml = XElement.Load(Global.GameResPath(ZhengBaConsts.MatchAwardConfigFile));
            foreach (var xmlItem in xml.Elements())
            {
                ZhengBaMatchAward award = new ZhengBaMatchAward();
                award.AwardId = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                award.Name = Global.GetSafeAttributeStr(xmlItem, "Name");
                award.FinalPassDay = (int)Global.GetSafeAttributeLong(xmlItem, "FinalPassDay");
                award.GoodsList = GoodsHelper.ParseGoodsDataList(Global.GetSafeAttributeStr(xmlItem, "Award").Split('|'), ZhengBaConsts.MatchAwardConfigFile);
                Debug.Assert(award.FinalPassDay >= 0 && award.FinalPassDay <= ZhengBaConsts.ContinueDays);
                Debug.Assert(award.GoodsList != null);
                _MatchAwardList.Add(award);
            }

            foreach (var support in _Config.SupportConfigList)
            {
                string winAwardTag = (string)support.WinAwardTag;
                string failAwardTag = (string)support.FailAwardTag;
                List<GoodsData> winAwardGoodsList = GoodsHelper.ParseGoodsDataList(winAwardTag.Split('|'), ZhengBaConsts.SupportConfigFile);
                List<GoodsData> failAwardGoodsList = GoodsHelper.ParseGoodsDataList(failAwardTag.Split('|'), ZhengBaConsts.SupportConfigFile);
                support.FailAwardTag = failAwardGoodsList;
                support.WinAwardTag = winAwardGoodsList;

                int totalPoint = 0;
                foreach (var goods in winAwardGoodsList)
                {
                    SystemXmlItem xmlItem = null;
                    List<MagicActionItem> magicActionItemList = null;
                    if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goods.GoodsID, out xmlItem)
                        || !GameManager.SystemMagicActionMgr.GoodsActionsDict.TryGetValue(goods.GoodsID, out magicActionItemList))
                    {
                        LogManager.WriteLog(LogTypes.Fatal, string.Format("众神争霸goods={0}找不到对应的action"));
                        continue;
                    }

                    foreach (var action in magicActionItemList)
                        if (action.MagicActionID == MagicActionIDs.ADD_ZHENGBADIANSHU)
                            totalPoint += (int)action.MagicActionParams[0] * goods.GCount;
                }
                support.WinPoint = totalPoint;

                totalPoint = 0;
                foreach (var goods in failAwardGoodsList)
                {
                    SystemXmlItem xmlItem = null;
                    List<MagicActionItem> magicActionItemList = null;
                    if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goods.GoodsID, out xmlItem)
                        || !GameManager.SystemMagicActionMgr.GoodsActionsDict.TryGetValue(goods.GoodsID, out magicActionItemList))
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("众神争霸goods={0}找不到对应的action", goods.GoodsID));
                        continue;
                    }

                    foreach (var action in magicActionItemList)
                        if (action.MagicActionID == MagicActionIDs.ADD_ZHENGBADIANSHU)
                            totalPoint += (int)action.MagicActionParams[0] * goods.GCount;
                }
                support.FailPoint = totalPoint;
            }

            DateTime now = TimeUtil.NowDateTime();
            int month = ZhengBaUtils.MakeMonth(now);
            // protobuf不能正确处理queue，所以用list
            List<ZhengBaPkLogData> pkLogList = Global.sendToDB<List<ZhengBaPkLogData>, string>(
                (int)TCPGameServerCmds.CMD_DB_ZHENGBA_LOAD_PK_LOG, string.Format("{0}:{1}", month, (int)ZhengBaConsts.MaxPkLogNum), GameManager.LocalServerId);
            Dictionary<int, List<ZhengBaSupportLogData>> supportLogs = Global.sendToDB<Dictionary<int, List<ZhengBaSupportLogData>>, string>(
                (int)TCPGameServerCmds.CMD_DB_ZHENGBA_LOAD_SUPPORT_LOG, string.Format("{0}:{1}", month, (int)ZhengBaConsts.MaxSupportLogNum), GameManager.LocalServerId);
            List<ZhengBaWaitYaZhuAwardData> waitAwardOfYaZhuList = Global.sendToDB<List<ZhengBaWaitYaZhuAwardData>, string>(
                (int)TCPGameServerCmds.CMD_DB_ZHENGBA_LOAD_WAIT_AWARD_YAZHU, string.Format("{0}", month), GameManager.LocalServerId);

            if (pkLogList != null)
            {
                pkLogList.RemoveAll(_log => _log.UpGrade == false);
                foreach (var log in pkLogList) this.PkLogQ.Enqueue(log);
            }
            if (supportLogs != null)
            {
                foreach (var kvp in supportLogs)
                {
                    Queue<ZhengBaSupportLogData> logQ = new Queue<ZhengBaSupportLogData>();
                    this.SupportLogs[kvp.Key] = logQ;
                    foreach (var log in kvp.Value) logQ.Enqueue(log);
                }
            }
            if (waitAwardOfYaZhuList != null) this.WaitAwardOfYaZhuList = waitAwardOfYaZhuList;
            SyncData.Month = month;
            SyncData.RoleModTime = DateTime.MinValue;
            SyncData.SupportModTime = DateTime.MinValue;
            SyncData.RealActDay = -1; // 设个-1的标记，表示未同步中心时的状态

            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("ZhengBaManager.TimerProc", SyncCenterData), 20000, 10000);
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("ZhengBaManager.TimerProc", CheckYaZhuAward), 20000, 120000);
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("ZhengBaManager.UpdateCopyScene", UpdateCopyScene), 10000, 100);

            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_MAIN_INFO, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_ALL_PK_LOG, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_ALL_PK_STATE, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_16_PK_STATE, 2, 2, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_SUPPORT, 4, 4, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_YA_ZHU, 2, 2, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_ENTER, 2, 2, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_MINI_STATE, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_QUERY_JOIN_HINT, 1, 1, Instance());

            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFZhengBaSupportLog, (int)SceneUIClasses.KFZhengBa, Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFZhengBaPkLog, (int)SceneUIClasses.KFZhengBa, Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFZhengBaNtfEnter, (int)SceneUIClasses.KFZhengBa, Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFZhengBaMirrorFight, (int)SceneUIClasses.KFZhengBa, Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFZhengBaBulletinJoin, (int)SceneUIClasses.KFZhengBa, Instance());

            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerDead, Instance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, Instance());
            return true;
        }

        public bool showdown()
        {
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KFZhengBaSupportLog, (int)SceneUIClasses.KFZhengBa, Instance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KFZhengBaPkLog, (int)SceneUIClasses.KFZhengBa, Instance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KFZhengBaNtfEnter, (int)SceneUIClasses.KFZhengBa, Instance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KFZhengBaMirrorFight, (int)SceneUIClasses.KFZhengBa, Instance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KFZhengBaBulletinJoin, (int)SceneUIClasses.KFZhengBa, Instance());

            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerDead, Instance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, Instance());
            return true;
        }

        public bool destroy()
        {
            return true;
        }
        #endregion

        #region Implement Interface `ICmdProcessorEx`
        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (client.ClientSocket.IsKuaFuLogin)
                return true;

            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_MAIN_INFO:
                    return HandleGetMainInfo(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_ALL_PK_LOG:
                    return HandleGetAllPkLog(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_ALL_PK_STATE:
                    return HandleGetAllPkState(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_16_PK_STATE:
                    return HandleGet16PkState(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ZHENGBA_SUPPORT:
                    return HandleSupport(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ZHENGBA_YA_ZHU:
                    return false; // 消息废弃
                case (int)TCPGameServerCmds.CMD_SPR_ZHENGBA_ENTER:
                    return HandleEnter(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_MINI_STATE:
                    return HandleGetMiniState(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ZHENGBA_QUERY_JOIN_HINT:
                    return HandleQueryJoinHint(client, nID, bytes, cmdParams);

            }

            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }
        #endregion

        #region HandleClientRequest
        private bool HandleQueryJoinHint(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            int hint = 0;
            DateTime now = TimeUtil.NowDateTime();
            int nowMonth = ZhengBaUtils.MakeMonth(TimeUtil.NowDateTime());
            int oldMonth = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ZhengBaHintFlag);
            lock (Mutex)
            {
                if (SyncData.Month == nowMonth
                    && oldMonth != nowMonth
                    && SyncData.IsThisMonthInActivity && IsGongNengOpened()
                    && this.RoleDataDict.ContainsKey(client.ClientData.RoleID))
                {
                    if (SyncData.RealActDay <= 0)
                        hint = 1;
                    else if (SyncData.RealActDay == 1 && now.TimeOfDay.Ticks < _Config.MatchConfigList.Find(_m => _m.Day == 1).DayBeginTick)
                        hint = 1;
                    else
                        hint = 0;
                }
            }

            client.sendCmd(nID, hint.ToString());
            if (hint == 1)
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ZhengBaHintFlag, nowMonth, true);

            return true;
        }

        private bool HandleGetMiniState(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            // 校正时间信息
            int realDay = 0;
            DateTime nowTime = DateTime.MinValue;
            bool todayIsPking = false, thisMonthInActivity = false;
            lock (Mutex)
            {
                nowTime = TimeUtil.NowDateTime().Add(DiffKfCenter);
                realDay = SyncData.RealActDay;
                todayIsPking = SyncData.TodayIsPking;
                thisMonthInActivity = SyncData.IsThisMonthInActivity;
            }

            ZhengBaMiniStateData data = new ZhengBaMiniStateData();
            data.IsZhengBaOpened = IsGongNengOpened();
            data.IsThisMonthInActivity = thisMonthInActivity;
            if (!data.IsZhengBaOpened || realDay < 0)
            {
                client.sendCmd(nID, data);
                return true;
            }

            // 本月不举行活动
            if (!thisMonthInActivity)
            {
                DateTime nextMonth = nowTime.AddMonths(1);
                nextMonth = new DateTime(nextMonth.Year, nextMonth.Month, ZhengBaConsts.StartMonthDay, 0, 0, 0);
                nextMonth = nextMonth.AddTicks(_Config.MatchConfigList.Find(_m => _m.Day == 1).DayBeginTick);

                data.PkStartWaitSec = (long)(nextMonth - nowTime).TotalSeconds;
            }
            else
            {
                // 本月举行活动

                if (realDay == 0)
                {
                    // 已经是10号之后了，但是还没开始，说明活动推移了，那么距离活动开始的时间就比较明天
                    DateTime end = DateTime.MinValue;

                    if (nowTime.AddDays(1).Month == nowTime.Month)
                    {
                        // 加1天之后还是本月
                        end = new DateTime(nowTime.Year, nowTime.Month, Math.Max(nowTime.Day + 1, ZhengBaConsts.StartMonthDay), 0, 0, 0);
                    }
                    else if (nowTime.AddMonths(1).Year == nowTime.Year)
                    {
                        // 加1天后到达下个月，下个月还是本年
                        end = new DateTime(nowTime.Year, nowTime.Month+1, ZhengBaConsts.StartMonthDay, 0, 0, 0);
                    }
                    else
                    {
                        // 加1天后不是本月，也不是本年
                        end = new DateTime(nowTime.Year+1, 1, ZhengBaConsts.StartMonthDay, 0, 0, 0);
                    }

                    end = end.AddTicks(_Config.MatchConfigList.Find(_m => _m.Day == 1).DayBeginTick);

                    data.PkStartWaitSec = (long)(end - nowTime).TotalSeconds;
                }
                else if (realDay >= 1 && realDay <= 7)
                {
                    ZhengBaMatchConfig todayMatchConfig = _Config.MatchConfigList.Find(_m => _m.Day == realDay);
                    if (nowTime.TimeOfDay.Ticks <= todayMatchConfig.DayBeginTick)                    // 今日未开始
                    {
                        data.PkStartWaitSec = (todayMatchConfig.DayBeginTick - nowTime.TimeOfDay.Ticks) / TimeSpan.TicksPerSecond;
                    }
                    else if (nowTime.TimeOfDay.Ticks >= todayMatchConfig.DayEndTick // 今日已结束
                        || (nowTime.TimeOfDay.Ticks - todayMatchConfig.DayBeginTick > 60 * TimeSpan.TicksPerSecond && !todayIsPking))               
                    {
                        // 开始时间60S之后，再判断是否提前结束，因为刚进入开始时间的时候, todayIsPking = false(还未从中心同步过来)
                        bool bCrossMonth = (realDay == 7 || nowTime.AddDays(1).Month != nowTime.Month);
                        DateTime nextDay = bCrossMonth
                            ? new DateTime(nowTime.AddMonths(1).Year, nowTime.AddMonths(1).Month, ZhengBaConsts.StartMonthDay, 0, 0, 0)
                            : new DateTime(nowTime.Year, nowTime.Month, nowTime.Day + 1, 0, 0, 0);
                        nextDay = nextDay.AddTicks(bCrossMonth
                            ? _Config.MatchConfigList.Find(_m => _m.Day == 1).DayBeginTick
                            : _Config.MatchConfigList.Find(_m => _m.Day == realDay + 1).DayBeginTick);

                        data.PkStartWaitSec = (long)(nextDay - nowTime).TotalSeconds;
                    }
                    else
                    {
                        // 每轮用时
                        int loopUseSec = todayMatchConfig.WaitSeconds + todayMatchConfig.FightSeconds + todayMatchConfig.ClearSeconds + todayMatchConfig.IntervalSeconds;
                        long todayContinueSec = (nowTime.TimeOfDay.Ticks - todayMatchConfig.DayBeginTick) / TimeSpan.TicksPerSecond;
                        long loopCurSec = todayContinueSec % loopUseSec;
                        if (loopCurSec < todayMatchConfig.WaitSeconds + todayMatchConfig.FightSeconds + todayMatchConfig.ClearSeconds)
                        {
                            // 当前处于等待和战斗时间内，那么计算为 本轮剩余结束时间 =xx
                            data.LoopEndWaitSec = todayMatchConfig.WaitSeconds + todayMatchConfig.FightSeconds + todayMatchConfig.ClearSeconds - loopCurSec;
                        }
                        else
                        {
                            // 计算为离下轮时间
                            data.NextLoopWaitSec = loopUseSec - loopCurSec;
                        }
                    }
                }
                else
                {
                    DateTime nextMonth = nowTime.AddMonths(1);
                    nextMonth = new DateTime(nextMonth.Year, nextMonth.Month, ZhengBaConsts.StartMonthDay, 0, 0, 0);
                    nextMonth = nextMonth.AddTicks(_Config.MatchConfigList.Find(_m => _m.Day == 1).DayBeginTick);

                    data.PkStartWaitSec = (long)(nextMonth - nowTime).TotalSeconds;
                }
            }

            data.PkStartWaitSec = Math.Max(data.PkStartWaitSec, 0);
            data.NextLoopWaitSec = Math.Max(data.NextLoopWaitSec, 0);
            data.LoopEndWaitSec = Math.Max(data.LoopEndWaitSec, 0);

            client.sendCmd(nID, data);
            return true;
        }

        private bool HandleGetMainInfo(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (!IsGongNengOpened()) return true;
            DateTime now = TimeUtil.NowDateTime();

            ZhengBaMainInfoData mainInfo = new ZhengBaMainInfoData();
            lock (Mutex)
            {
                mainInfo.RealActDay = SyncData.RealActDay;
                mainInfo.RankResultOfDay = SyncData.RankResultOfDay;
                mainInfo.Top16List = this.Top16RoleList;
                mainInfo.MaxOpposeGroup = this.MaxOpposeGroup;
                mainInfo.MaxSupportGroup = this.MaxSupportGroup;

                mainInfo.CanGetAwardId = 0;            
                bool bInAwardTime = false;
                if (SyncData.RealActDay >= ZhengBaConsts.ContinueDays
                    && this.Top16RoleList.Exists(_r => _r.ZhengBaGrade == (int)EZhengBaGrade.Grade1))
                {
                    // 第一名产生之后，就可以参与领奖了
                    bInAwardTime = true;
                }
                else if (now.AddDays(1).Month != now.Month)
                {
                    // 月底最后一天，并且本天的pk已结束，那么不论有没有冠军，直接发奖
                    // 当前比赛结束的5分钟后，加5分钟是为了防止同步延迟
                    ZhengBaMatchConfig _config = _Config.MatchConfigList.Find(_m => _m.Day == SyncData.RealActDay);
                    if (_config != null 
                        && now.TimeOfDay.Ticks > _config.DayEndTick + 5 * TimeSpan.TicksPerMinute
                        && !SyncData.TodayIsPking)
                    {
                        bInAwardTime = true;
                    }
                }
           
                if (bInAwardTime)
                {
                    // 我在参赛名单中
                    TianTiPaiHangRoleData roleData = null;
                    if (this.RoleDataDict.TryGetValue(client.ClientData.RoleID, out roleData))
                    {
                        int awardFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ZhengBaAwardFlag);
                        if (awardFlag <= 0 || awardFlag/100 != SyncData.Month)
                        {
                            int day = ZhengBaUtils.WhichDayResultByGrade((EZhengBaGrade)roleData.ZhengBaGrade);
                            ZhengBaMatchAward award = _MatchAwardList.Find(_m => _m.FinalPassDay == day);
                            if (award != null)
                            {
                                if (award.GoodsList.Count > 0)
                                {
                                    if (Global.CanAddGoodsDataList(client, award.GoodsList))
                                    {
                                        foreach (var gd in award.GoodsList)
                                        {
                                            Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, gd.GoodsID, gd.GCount, gd.Quality, gd.Props, gd.Forge_level, gd.Binding, 0, gd.Jewellist, true, 1, "众神争霸", false,
                                                                       gd.Endtime, gd.AddPropIndex, gd.BornIndex, gd.Lucky, gd.Strong, gd.ExcellenceInfo, gd.AppendPropLev, gd.ChangeLifeLevForEquip, true);
                                        }
                                    }
                                    else
                                    {
                                        Global.UseMailGivePlayerAward3(client.ClientData.RoleID, award.GoodsList, Global.GetLang("众神争霸"),
                                            string.Format(Global.GetLang("众神争霸【{0}】奖励，请查收！"), award.Name), 0);
                                    }
                                }

                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ZhengBaAwardFlag, SyncData.Month * 100 + award.AwardId, true);
                                mainInfo.CanGetAwardId = award.AwardId;
                            }
                        }
                    }
                }

                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ZhengBaJoinIconFlag, ZhengBaUtils.MakeMonth(now), true);
                CheckTipsIconState(client);
            }

            client.sendCmd(nID, mainInfo);
            return true;
        }

        private bool HandleGetAllPkLog(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (!IsGongNengOpened()) return true;

            List<ZhengBaPkLogData> retList = new List<ZhengBaPkLogData>();

            lock (Mutex)
            {
                retList.AddRange(PkLogQ);
            }

            retList.Reverse();
            client.sendCmd(nID, retList);
            return true;
        }

        private bool HandleGetAllPkState(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (!IsGongNengOpened()) return true;

            List<TianTiPaiHangRoleData> retList = new List<TianTiPaiHangRoleData>();

            lock (Mutex)
            {
                retList.AddRange(RoleDataList);
            }

            client.sendCmd(nID, retList);
            return true;
        }

        private bool HandleGet16PkState(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (!IsGongNengOpened()) return true;

            int unionGroup = Convert.ToInt32(cmdParams[1]);
            client.sendCmd(nID, Get16PkState(client, unionGroup));
            return true;
        }

        private ZhengBaUnionGroupData Get16PkState(GameClient client, int unionGroup)
        {
            int group1 = 0, group2 = 0;
            ZhengBaUtils.SplitUnionGroup(unionGroup, out group1, out group2);

            List<ZhengBaSupportAnalysisData> supportDatas = null;
            List<ZhengBaSupportLogData> supportLogs = null;
            lock (Mutex)
            {
                supportDatas = this.SupportDatas;
                Queue<ZhengBaSupportLogData> supportLogQ = null;
                if (this.SupportLogs.TryGetValue(unionGroup, out supportLogQ))
                {
                    supportLogs = new List<ZhengBaSupportLogData>(supportLogQ);
                    supportLogs.Reverse();
                }
            }

            ZhengBaUnionGroupData result = new ZhengBaUnionGroupData();
            result.UnionGroup = unionGroup;
            result.SupportLogs = supportLogs;

            result.SupportDatas = new List<ZhengBaSupportAnalysisData>();
            var data1 = supportDatas.Find(_s => _s.UnionGroup == unionGroup && _s.Group == group1);
            var data2 = supportDatas.Find(_s => _s.UnionGroup == unionGroup && _s.Group == group2);
            if (data1 != null) result.SupportDatas.Add(data1);
            if (data2 != null) result.SupportDatas.Add(data2);

            result.SupportFlags = new List<ZhengBaSupportFlagData>();
            List<ZhengBaSupportFlagData> mySupports = client.ClientData.ZhengBaSupportFlags;
            var flag1 = mySupports.Find(_s => _s.UnionGroup == unionGroup && _s.Group == group1);
            var flag2 = mySupports.Find(_s => _s.UnionGroup == unionGroup && _s.Group == group2);
            if (flag1 != null) result.SupportFlags.Add(flag1);
            if (flag2 != null) result.SupportFlags.Add(flag2);

            int supportDay = 0;
            for (int day = ZhengBaConsts.ContinueDays; day >= 1; day--)
            {
                if (ZhengBaUtils.IsValidPkGroup(group1, group2, day))
                {
                    supportDay = day - 1;
                    break;
                }
            }

            ZhengBaSupportConfig supportConfig = _Config.SupportConfigList.Find(_s => _s.RankOfDay == supportDay);
            if (supportConfig != null) result.WinZhengBaPoint = supportConfig.WinPoint;

                return result;
        }

        private bool HandleSupport(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (!IsGongNengOpened()) return true;

            int unionGroup = Convert.ToInt32(cmdParams[1]);
            int group = Convert.ToInt32(cmdParams[2]);
            int supportType = Convert.ToInt32(cmdParams[3]);
            if (supportType != (int)EZhengBaSupport.Support && supportType != (int)EZhengBaSupport.Oppose && supportType != (int)EZhengBaSupport.YaZhu)
                return true;

            // 检查上传的group是否有效
            int group1 = 0, group2 = 0;
            ZhengBaUtils.SplitUnionGroup(unionGroup, out group1, out group2);
            if (group < 1 || group > ZhengBaConsts.MaxGroupNum) return true;
            if (group1 < 1 || group1 > ZhengBaConsts.MaxGroupNum) return true;
            if (group2 < 1 || group2 > ZhengBaConsts.MaxGroupNum) return true;
            if (group1 >= group2) return true;
            if (group != group1 && group != group2) return true;
            if (SyncData.RealActDay < 3 || SyncData.RealActDay > ZhengBaConsts.ContinueDays) return true;

            // 检测是否是操作时间
            DateTime now = TimeUtil.NowDateTime();
            ZhengBaSupportConfig supportConfig = _Config.SupportConfigList.Find(
                _s => _s.TimeList.Exists(
                    _t => _t.RealDay == SyncData.RealActDay && _t.DayBeginTicks < now.TimeOfDay.Ticks && _t.DayEndTicks > now.TimeOfDay.Ticks)
                    );
            if (supportConfig == null)
            {
                client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", StdErrorCode.Error_Not_In_valid_Time, unionGroup, group, supportType));
                return true;
            }

            // 检查等级限制
            if (Global.GetUnionLevel(client) < Global.GetUnionLevel(supportConfig.MinChangeLife, supportConfig.MinLevel))
            {
                client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", StdErrorCode.Error_Level_Limit, unionGroup, group, supportType));
                return true;
            }

            // 检查这两个group当前是否是一个pk组合
            if (!ZhengBaUtils.IsValidPkGroup(group1, group2, supportConfig.RankOfDay + 1))
                return true;

            // 检查这两个玩家是否存在并且晋级
            lock (Mutex)
            {
                if (!Top16RoleList.Exists(_r => _r.ZhengBaGroup == group1 && _r.ZhengBaState == (int)EZhengBaState.UpGrade)
                    || !Top16RoleList.Exists(_r => _r.ZhengBaGroup == group2 && _r.ZhengBaState == (int)EZhengBaState.UpGrade))
                {
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", StdErrorCode.Error_Operation_Denied, unionGroup, group, supportType));
                    return true;
                }
            }

            // 检查押注总次数上限
            if (supportType == (int)EZhengBaSupport.YaZhu)
            {
                int hadYaZhuCnt = client.ClientData.ZhengBaSupportFlags.Count(_s => _s.RankOfDay == supportConfig.RankOfDay && _s.IsYaZhu);
                if (hadYaZhuCnt >= supportConfig.MaxTimes)
                {
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", StdErrorCode.Error_No_Residue_Degree, unionGroup, group, supportType));
                    return true;
                }
            }

            // 检查我的赞、贬、押注限制
            ZhengBaSupportFlagData flagData = client.ClientData.ZhengBaSupportFlags.Find(_f => _f.UnionGroup == unionGroup && _f.Group == group);
            if (flagData != null)
            {
                // 赞和贬 二选一
                if ((flagData.IsOppose || flagData.IsSupport)
                    && (supportType == (int)EZhengBaSupport.Oppose || supportType == (int)EZhengBaSupport.Support))
                {
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", StdErrorCode.Error_Invalid_Operation, unionGroup, group, supportType));
                    return true;
                }

                if (supportType == (int)EZhengBaSupport.YaZhu)
                {
                    // 检查是否已押注 或者 是否已对同组的另一个人押注
                    if (flagData.IsYaZhu
                        || client.ClientData.ZhengBaSupportFlags.Count(_f => _f.UnionGroup == unionGroup && _f.IsYaZhu) >= 1)
                    {
                        client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", StdErrorCode.Error_Invalid_Operation, unionGroup, group, supportType));
                        return true;
                    }
                }
            }

            // 押注扣钱
            if (supportType == (int)EZhengBaSupport.YaZhu && !Global.SubBindTongQianAndTongQian(client, supportConfig.CostJinBi, "众神争霸押注"))
            {
                client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", StdErrorCode.Error_JinBi_Not_Enough, unionGroup, group, supportType));
                return true;
            }

            ZhengBaSupportLogData log = new ZhengBaSupportLogData();
            log.FromRoleId = client.ClientData.RoleID;
            log.FromZoneId = client.ClientData.ZoneID;
            log.FromRolename = client.ClientData.RoleName;
            log.SupportType = supportType;
            log.ToUnionGroup = unionGroup;
            log.ToGroup = group;
            log.Time = now;
            log.FromServerId = GameCoreInterface.getinstance().GetLocalServerId();
            log.Month = ZhengBaUtils.MakeMonth(now);
            log.RankOfDay = supportConfig.RankOfDay;

            int ec = TianTiClient.getInstance().ZhengBaSupport(log);
            if (ec < 0)
            {
                client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", ec, unionGroup, group, supportType));
                return true;
            }

            if (!Global.sendToDB<bool, ZhengBaSupportLogData>(
                (int)TCPGameServerCmds.CMD_DB_ZHENGBA_SAVE_SUPPORT_LOG, log, GameManager.LocalServerId))
            {
                client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", StdErrorCode.Error_DB_Faild, unionGroup, group, supportType));
                return true;
            }

            if (flagData == null)
            {
                flagData = new ZhengBaSupportFlagData();
                flagData.UnionGroup = unionGroup;
                flagData.Group = group;
                flagData.RankOfDay = supportConfig.RankOfDay;
                client.ClientData.ZhengBaSupportFlags.Add(flagData);
            }

            if (supportType == (int)EZhengBaSupport.Support) flagData.IsSupport = true;
            else if (supportType == (int)EZhengBaSupport.Oppose) flagData.IsOppose = true;
            else if (supportType == (int)EZhengBaSupport.YaZhu) flagData.IsYaZhu = true;

            lock (Mutex)
            {
                Queue<ZhengBaSupportLogData> supportLogQ = null;
                if (!SupportLogs.TryGetValue(unionGroup, out supportLogQ))
                    SupportLogs[unionGroup] = supportLogQ = new Queue<ZhengBaSupportLogData>();
                supportLogQ.Enqueue(log);
                while (supportLogQ.Count > ZhengBaConsts.MaxSupportLogNum)
                    supportLogQ.Dequeue();

                if (supportType == (int)EZhengBaSupport.YaZhu)
                {
                    ZhengBaWaitYaZhuAwardData waitYaZhuAward = new ZhengBaWaitYaZhuAwardData();
                    waitYaZhuAward.Month = log.Month;
                    waitYaZhuAward.FromRoleId = client.ClientData.RoleID;
                    waitYaZhuAward.UnionGroup = unionGroup;
                    waitYaZhuAward.Group = group;
                    waitYaZhuAward.RankOfDay = log.RankOfDay;
                    WaitAwardOfYaZhuList.Add(waitYaZhuAward);
                }

                // 统计的支持数据，这里不修改，会有延迟， 统一的定期从跨服中心获取
                // 2.0 本来打算统一定期从中心获取的，但是发现延迟太明显，所以本服的操作，先直接修改到本GameServer
                ZhengBaSupportAnalysisData analysisData = SupportDatas.Find(_s => _s.UnionGroup == unionGroup && _s.Group == group);
                if (analysisData == null)
                {
                    analysisData = new ZhengBaSupportAnalysisData() { UnionGroup = unionGroup, Group = group };
                    SupportDatas.Add(analysisData);
                }
                if (supportType == (int)EZhengBaSupport.Support) analysisData.TotalSupport++;
                else if (supportType == (int)EZhengBaSupport.Oppose) analysisData.TotalOppose++;
                else if (supportType == (int)EZhengBaSupport.YaZhu) analysisData.TotalYaZhu++;
            }           

            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ZHENGBA_GET_16_PK_STATE, Get16PkState(client, unionGroup));
            return true;
        }

        private bool HandleEnter(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (!IsGongNengOpened()) return true;

            int gameId = Convert.ToInt32(cmdParams[0]);
            int enter = Convert.ToInt32(cmdParams[1]);
            if (gameId != client.ClientSocket.ClientKuaFuServerLoginData.GameId
                || client.ClientSocket.ClientKuaFuServerLoginData.GameType != (int)GameTypes.ZhengBa)
            {
                client.sendCmd(nID, string.Format("{0}", StdErrorCode.Error_Not_In_valid_Time));
                return true;
            }

            if (enter != (int)EZhengBaEnterType.Player && enter != (int)EZhengBaEnterType.Mirror)
            {
                client.sendCmd(nID, string.Format("{0}", StdErrorCode.Error_Invalid_Params));
                return true;
            }

            int ec = TianTiClient.getInstance().ZhengBaRequestEnter(client.ClientData.RoleID, gameId, (EZhengBaEnterType)enter);
            if (ec < 0)
            {
                client.sendCmd(nID, string.Format("{0}", ec));
                return true;
            }

            if (enter == (int)EZhengBaEnterType.Player)
            {
                GlobalNew.RecordSwitchKuaFuServerLog(client);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client));
            }
            else if (enter == (int)EZhengBaEnterType.Mirror)
            {
                client.ClientSocket.ClientKuaFuServerLoginData.RoleId = 0;
                client.ClientSocket.ClientKuaFuServerLoginData.GameId = 0;
                client.ClientSocket.ClientKuaFuServerLoginData.GameType = 0;
                client.ClientSocket.ClientKuaFuServerLoginData.ServerId = 0;
            }

            return true;
        }
        #endregion

        #region `Timer` RPC ---> KF.Hosting
        public void SyncCenterData(object sender, EventArgs e)
        {
            ZhengBaSyncData syncData = TianTiClient.getInstance().GetZhengBaRankData(SyncData);
            if (syncData == null) return;

            if (syncData.RoleList != null)
            {
                List<TianTiPaiHangRoleData> roleList = new List<TianTiPaiHangRoleData>();
                Dictionary<int, TianTiPaiHangRoleData> roleDict = new Dictionary<int, TianTiPaiHangRoleData>();
                Dictionary<int, PlayerJingJiData> mirrorData = new Dictionary<int, PlayerJingJiData>();
                foreach (var info in syncData.RoleList)
                {
                    if (null != info.TianTiPaiHangRoleData)
                    {
                        TianTiPaiHangRoleData role = DataHelper.BytesToObject<TianTiPaiHangRoleData>(info.TianTiPaiHangRoleData, 0, info.TianTiPaiHangRoleData.Length);
                        if (role == null) continue;

                        role.RoleId = info.RoleId;
                        role.DuanWeiRank = info.DuanWeiRank;
                        role.ZhengBaGrade = info.Grade;
                        role.ZhengBaGroup = info.Group;
                        role.ZhengBaState = info.State;
                        if (role.RoleData4Selector != null)
                        {
                            // 客户端不需要用到这里面的数据，强制清空
                            role.RoleData4Selector.GoodsDataList = null;
                            role.RoleData4Selector.MyWingData = null;
                        }
                        roleList.Add(role);
                        roleDict.Add(role.RoleId, role);

                        if (null != info.PlayerJingJiMirrorData)
                        {
                            PlayerJingJiData jingJiData = DataHelper.BytesToObject<PlayerJingJiData>(info.PlayerJingJiMirrorData, 0, info.PlayerJingJiMirrorData.Length);
                            if (jingJiData != null)
                            {
                                mirrorData[role.RoleId] = jingJiData;
                            }
                        }
                    }
                }

                roleList.Sort((_l, _r) =>
                {
                    // 先按成绩排名
                    if (_l.ZhengBaGrade < _r.ZhengBaGrade) return -1;
                    else if (_l.ZhengBaGrade > _r.ZhengBaGrade) return 1;
                    else
                    {
                        // 成绩相同，按晋级、淘汰排序 
                        if (_l.ZhengBaState < _r.ZhengBaState) return -1;
                        else if (_l.ZhengBaState > _r.ZhengBaState) return 1;
                        else
                        {
                            // 都处于晋级或者淘汰状态, 按段位排名
                            return _l.DuanWeiRank - _r.DuanWeiRank;
                        }
                    }
                });

                List<TianTiPaiHangRoleData> top16List = roleList.FindAll(_r => _r.ZhengBaGrade <= (int)EZhengBaGrade.Grade16);
                TianTiPaiHangRoleData KingRole = top16List.Find(_r => _r.ZhengBaGrade == (int)EZhengBaGrade.Grade1);
                if (KingRole != null) this.SetZhongShengRole(KingRole.RoleId);

                lock (Mutex)
                {
                    this.RoleDataList = roleList;
                    this.RoleDataDict = roleDict;
                    this.Top16RoleList = top16List;
                    this.MirrorDatas = mirrorData;
                }
            }

            if (syncData.SupportList != null)
            {
                List<ZhengBaSupportAnalysisData> supportDatas = syncData.SupportList;

                lock (Mutex)
                {
                    this.SupportDatas = supportDatas;

                    // 找出赞、贬最多的玩家(group)
                    List<KeyValuePair<int, int>> _supportList = new List<KeyValuePair<int, int>>();
                    List<KeyValuePair<int, int>> _opposeList = new List<KeyValuePair<int, int>>();

                    foreach (var data in this.SupportDatas)
                    {
                        if (data.RankOfDay != syncData.RankResultOfDay) 
                            continue;

                        TianTiPaiHangRoleData roleData = null;
                        if ( (roleData = Top16RoleList.Find(_r => _r.ZhengBaGroup == data.Group)) == null
                            || roleData.ZhengBaState != (int)EZhengBaState.UpGrade)
                            continue;

                        _supportList.RemoveAll(_kvp => _kvp.Key == data.Group);
                        _opposeList.RemoveAll(_kvp => _kvp.Key == data.Group);
                        _supportList.Add(new KeyValuePair<int, int>(data.Group, data.TotalSupport));
                        _opposeList.Add(new KeyValuePair<int, int>(data.Group, data.TotalOppose));
                    }

                    _supportList.RemoveAll(_kvp => _kvp.Value <= 0);
                    _opposeList.RemoveAll(_kvp => _kvp.Value <= 0);
                    _supportList.Sort((_l, _r) => _r.Value - _l.Value);
                    _opposeList.Sort((_l, _r) => _r.Value - _l.Value);

                    int _maxSupportGroup = 0, _maxOpposeGroup = 0;
                    if (_supportList.Count > 0) 
                        _maxSupportGroup = _supportList[0].Key;
                    if (_opposeList.Count > 0)
                    {
                        _maxOpposeGroup = _opposeList[0].Key;
                        if (_maxOpposeGroup == _maxSupportGroup)
                        {
                            _maxOpposeGroup = 0;
                            if (_opposeList.Count > 1)
                            {
                                _maxOpposeGroup = _opposeList[1].Key;
                            }
                        }
                    }

                    this.MaxSupportGroup = _maxSupportGroup;
                    this.MaxOpposeGroup = _maxOpposeGroup;
                }       
            }

            lock (Mutex)
            {
                if (SyncData.Month != syncData.Month)
                {
                    this.SupportLogs.Clear();
                    this.PkLogQ.Clear();
                    this.WaitAwardOfYaZhuList.Clear();
                }

                syncData.RoleList = null;
                syncData.SupportList = null;

                SyncData = syncData;
                DiffKfCenter = syncData.CenterTime - TimeUtil.NowDateTime();
            }
        }

        public void SetZhongShengRole(int roleid)
        {
            int oldRoleId = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.ZhongShenZhiShenRole, 0);
            if (oldRoleId != roleid)
            {
                // 记录标记
                Global.UpdateDBGameConfigg(GameConfigNames.ZhongShenZhiShenRole, roleid.ToString());
                GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.ZhongShenZhiShenRole, roleid.ToString());

                GameClient oldClient = GameManager.ClientMgr.FindClient(oldRoleId);
                if (oldClient != null) CheckZhongShenChengHao(oldClient);

                GameClient newClient = GameManager.ClientMgr.FindClient(roleid);
                if (newClient != null) CheckZhongShenChengHao(newClient);
            }              
        }

        private void CheckZhongShenChengHao(GameClient client)
        {
            if (client == null) return;

            int kingRole = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.ZhongShenZhiShenRole, 0);
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.ZhongShenZhiShen_ChengHao);
            if (client.ClientData.RoleID != kingRole)
            {
                // 检查是否有称号并删除
                if (bufferData != null && bufferData.BufferVal != 0)
                {
                    double[] bufferParams = new double[1] { 0 };
                    Global.UpdateBufferData(client, BufferItemTypes.ZhongShenZhiShen_ChengHao, bufferParams);
                }
            }
            else
            {
                // 检查是否没有称号并添加
                if (bufferData == null || bufferData.BufferVal == 0)
                {
                    double[] bufferParams = new double[1] { 1 };
                    Global.UpdateBufferData(client, BufferItemTypes.ZhongShenZhiShen_ChengHao, bufferParams);
                }
            }
        }

        /// <summary>
        /// 押注发奖采用gameserver定期检测，不使用中心通知发奖的方式，防止中心--->game的消息丢失
        /// </summary>
        private void CheckYaZhuAward(object sender, EventArgs e)
        {
            lock (Mutex)
            {
                if (this.WaitAwardOfYaZhuList.Count <= 0)
                    return;

                foreach (var waitAward in this.WaitAwardOfYaZhuList)
                {
                    TianTiPaiHangRoleData roleData = this.Top16RoleList.Find(_r => _r.ZhengBaGroup == waitAward.Group);
                    if (roleData == null) continue;

                    ZhengBaSupportConfig supportConfig = _Config.SupportConfigList.Find(_m => _m.RankOfDay == waitAward.RankOfDay);
                    if (supportConfig == null) continue;

                    List<GoodsData> awardGoodsList = null;
                    string mailMsg = string.Empty;
                    if (roleData.ZhengBaGrade <= (int)ZhengBaUtils.GetDayUpGrade(waitAward.RankOfDay + 1)) // 支持的选手已晋级
                    {
                        mailMsg = roleData.ZhengBaGrade == (int)EZhengBaGrade.Grade1 ?
                            Global.GetLang("您支持的选手【{0}】获得本次众神争霸冠军，您获得争霸点数{1}！") :
                            Global.GetLang("您支持的选手【{0}】成功晋级，您获得争霸点数{1}！");
                        mailMsg = string.Format(mailMsg, roleData.RoleName, supportConfig.WinPoint);

                        awardGoodsList = supportConfig.WinAwardTag as List<GoodsData>;
                    }
                    else if (roleData.ZhengBaState == (int)EZhengBaState.Failed) // 支持的选手被淘汰
                    {
                        mailMsg = Global.GetLang("您支持的选手【{0}】不幸淘汰！您获得参与奖争霸点数{1}！");
                        mailMsg = string.Format(mailMsg, roleData.RoleName, supportConfig.FailPoint);

                        awardGoodsList = supportConfig.FailAwardTag as List<GoodsData>;
                    }
                    else continue;

                    if (Global.UseMailGivePlayerAward3(waitAward.FromRoleId, awardGoodsList, Global.GetLang("众神争霸"), mailMsg, 0))
                    {
                        Global.sendToDB<bool, string>((int)TCPGameServerCmds.CMD_DB_ZHENGBA_SET_YAZHU_AWARD_FLAG,
                            string.Format("{0}:{1}:{2}:{3}", waitAward.Month, waitAward.FromRoleId, waitAward.UnionGroup, waitAward.Group), 
                            GameManager.LocalServerId);

                        waitAward.FromRoleId = -1;
                    }
                }

                this.WaitAwardOfYaZhuList.RemoveAll(_w => _w.FromRoleId == -1);
            }
        }

        #endregion

        #region
        public void CheckTipsIconState(GameClient client)
        {
            if (client == null) return;

            DateTime now = TimeUtil.NowDateTime();
            int nowMonth = ZhengBaUtils.MakeMonth(TimeUtil.NowDateTime());
            int oldMonth = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ZhengBaJoinIconFlag);

            bool bIconActive = false;

            lock (Mutex)
            {
                if (SyncData.Month == nowMonth
                    && oldMonth != nowMonth
                    && SyncData.IsThisMonthInActivity && IsGongNengOpened()
                    && this.RoleDataDict.ContainsKey(client.ClientData.RoleID))
                {
                    bIconActive = true;
                }
            }

            if (client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.ZhengBaCanJoinIcon, bIconActive))
            {
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        public void OnLogin(GameClient client)
        {
            if (client == null) return;
            CheckZhongShenChengHao(client);
            CheckGongNengCanOpen(client);
            CheckTipsIconState(client);
            if (client.ClientSocket.IsKuaFuLogin) return;

            DateTime now = TimeUtil.NowDateTime();
            // 活动只要开始了，就去数据库加载我的支持信息，虽然16强之后才会产生赞、贬、支持信息
            if (now.Day > ZhengBaConsts.StartMonthDay)
            {
                int month = ZhengBaUtils.MakeMonth(now);
                List<ZhengBaSupportFlagData> mySupports = Global.sendToDB<List<ZhengBaSupportFlagData>, string>(
                    (int)TCPGameServerCmds.CMD_DB_ZHENGBA_LOAD_SUPPORT_FLAG, string.Format("{0}:{1}", client.ClientData.RoleID, month), client.ServerId);
                client.ClientData.ZhengBaSupportFlags.Clear();
                if (mySupports != null)
                {
                    client.ClientData.ZhengBaSupportFlags.AddRange(mySupports);
                }
            }
        }

        public bool IsGongNengOpened()
        {
            // 如果2.0的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System2Dot0))
                return false;

            return GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.ZhengBaOpenedFlag, 0) == 1;
        }

        public void CheckGongNengCanOpen(GameClient client)
        {
            if (client == null) return;

            int openFlag = 1;
            if (GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.ZhengBaOpenedFlag, 0) != openFlag
                && TianTiManager.getInstance().IsGongNengOpened(client, false))
            {
                Global.UpdateDBGameConfigg(GameConfigNames.ZhengBaOpenedFlag, openFlag.ToString());
                GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.ZhengBaOpenedFlag, openFlag.ToString());

                string broadcastMsg = Global.GetLang("玩家【{0}】率先开启了跨服天梯，服务器开启跨服众神争霸功能！");
                broadcastMsg = string.Format(broadcastMsg, client.ClientData.RoleName);
                Global.BroadcastRoleActionMsg(null, RoleActionsMsgTypes.Bulletin, broadcastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
            }
        }

        public void OnNewDay(GameClient client)
        {
            if (client == null || client.ClientSocket.IsKuaFuLogin) return;

            if (TimeUtil.NowDateTime().Day == 1)
            {
                client.ClientData.ZhengBaSupportFlags.Clear();
            }
        }

        #endregion

        #region IEventListenerEx
        public void processEvent(EventObjectEx eventObject)
        {
            if (eventObject.EventType == (int)GlobalEventTypes.KFZhengBaSupportLog)
                this.HandleSupportLog((eventObject as KFZhengBaSupportEvent).Data);
            else if (eventObject.EventType == (int)GlobalEventTypes.KFZhengBaPkLog)
                this.HandlePkLog((eventObject as KFZhengBaPkLogEvent).Log);
            else if (eventObject.EventType == (int)GlobalEventTypes.KFZhengBaNtfEnter)
                this.HandleNtfEnter((eventObject as KFZhengBaNtfEnterEvent).Data);
            else if (eventObject.EventType == (int)GlobalEventTypes.KFZhengBaMirrorFight)
                this.HandleMirrirFight((eventObject as KFZhengBaMirrorFightEvent).Data);
            else if (eventObject.EventType == (int)GlobalEventTypes.KFZhengBaBulletinJoin)
                this.HandleBulletinJoin((eventObject as KFZhengBaBulletinJoinEvent).Data);
            eventObject.Handled = true;
        }

        private void HandleBulletinJoin(ZhengBaBulletinJoinData data)
        {
            // 强制尝试从中心同步一下排行榜
            this.SyncCenterData(null, null);

            if (data.NtfType == ZhengBaBulletinJoinData.ENtfType.BulletinServer)
            {
                ZhengBaMatchConfig matchConfig = _Config.MatchConfigList.Find(_m => _m.Day == 1);
                DateTime dtBegin = new DateTime(matchConfig.DayBeginTick);
                string broadcastMsg = Global.GetLang("本月跨服众神争霸将于{0}日{1}点{2}分准时开始，上月跨服竞技排名前{3}的角色获得参与资格，邀请函已发送至邮箱，请查阅！");
                broadcastMsg = string.Format(broadcastMsg, ZhengBaConsts.StartMonthDay, dtBegin.Hour, dtBegin.Minute, data.Args1);
                Global.BroadcastRoleActionMsg(null, RoleActionsMsgTypes.Bulletin, broadcastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
            }
            else if (data.NtfType == ZhengBaBulletinJoinData.ENtfType.MailJoinRole)
            {
                ZhengBaMatchConfig matchConfig = _Config.MatchConfigList.Find(_m => _m.Day == 1);
                DateTime dtBegin = new DateTime(matchConfig.DayBeginTick), dtEnd = new DateTime(matchConfig.DayEndTick);
                string mailMsg = Global.GetLang("本月跨服众神争霸将于{0}日开始，您已获得参与资格。每日{1}点{2}分至{3}点{4}分进行比赛，比赛为期7天。为了确保能够参与活动，请在活动开始期间保持在线，并且将角色停留在主线地图（不包括跨服主线地图），活动开始后您会收到系统发出的战斗提示，最后祝您武运昌隆！");
                mailMsg = string.Format(mailMsg, ZhengBaConsts.StartMonthDay, dtBegin.Hour, dtBegin.Minute, dtEnd.Hour, dtEnd.Minute);
                List<int> roleIdList = new List<int>();
                lock (Mutex)
                {
                    var result = this.RoleDataDict.Keys.ToList();
                    if (result != null) roleIdList.AddRange(result);
                }
                roleIdList.ForEach(_rid => {
                    Global.UseMailGivePlayerAward3(_rid, null, Global.GetLang("众神争霸"), mailMsg, 0, 1);
                });
            }
            else if (data.NtfType == ZhengBaBulletinJoinData.ENtfType.MailUpgradeRole)
            {
                string mailMsg = Global.GetLang("恭喜您成功晋级，请在下轮比赛开始期间保持在线，并且将角色停留在主线地图（不包括跨服主线地图），活动开始后您会收到系统发出的战斗提示，最后祝您武运昌隆！");
                Global.UseMailGivePlayerAward3(data.Args1, null, Global.GetLang("众神争霸"), mailMsg, 0, 1);
            }
            else if (data.NtfType == ZhengBaBulletinJoinData.ENtfType.DayLoopEnd)
            {
                if (data.Args1 >= 1 && data.Args1 < ZhengBaConsts.ContinueDays)
                {
                    string mailMsg = string.Format(Global.GetLang("本月跨服众神争霸第{0}轮比赛已结束，晋级选手请于明日准时参加后续比赛！"), data.Args1); ;
                    Global.BroadcastRoleActionMsg(null, RoleActionsMsgTypes.Bulletin, mailMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                }
            }
        }

        private void HandleMirrirFight(ZhengBaMirrorFightData data)
        {
            if (data.ToServerId != GameCoreInterface.getinstance().GetLocalServerId())
                return;

            lock (Mutex)
            {
                ZhengBaMatchConfig matchConfig = _Config.MatchConfigList.Find(_m => _m.Day == SyncData.RealActDay);
                if (matchConfig == null) return;

                PlayerJingJiData jingJiData = null;
                if (!MirrorDatas.TryGetValue(data.RoleId, out jingJiData))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("镜像出战，找不到镜像, server={0}, rid={1}, gameid={2}", data.ToServerId, data.RoleId, data.GameId));
                    return;
                }

                GameSideInfo sideInfo = null;
                if (!GameId2FuBenSeq.TryGetValue(data.GameId, out sideInfo))
                {
                    sideInfo = new GameSideInfo();
                    sideInfo.FuBenSeq = GameCoreInterface.getinstance().GetNewFuBenSeqId();
                    GameId2FuBenSeq[data.GameId] = sideInfo;
                }

                // 镜像出战创建ZhengBaCopyScene的时候，不创建CopyMap，等待真人玩家进入的时候创建
                // 如果是两个镜像对战，那么CopyMap就省去了
                ZhengBaCopyScene scene = null;
                if (!FuBenSeq2CopyScenes.TryGetValue(sideInfo.FuBenSeq, out scene))
                {
                    scene = new ZhengBaCopyScene();
                    scene.FuBenSeq = sideInfo.FuBenSeq;
                    scene.GameId = data.GameId;
                    scene.MapCode = matchConfig.MapCode;

                    FuBenSeq2CopyScenes[sideInfo.FuBenSeq] = scene;
                }

                // 由于竞技场的机器人创建的时候需要绑定一个敌对目标，为了不修改原有代码
                // 机器人出战的时候先不创建robot对象，等副本进入开战状态时，检测如果对方是玩家，那么则创建自己的机器人，
                // 如果两方都是机器人，那么robot对象根本无须创建
                if (scene.RoleId1 <= 0)
                {
                    scene.RoleId1 = data.RoleId;
                    scene.IsMirror1 = true;
                    scene.JingJiData1 = jingJiData;
                    scene.Robot1 = null;
                }
                else if (scene.RoleId2 <= 0)
                {
                    scene.RoleId2 = data.RoleId;
                    scene.IsMirror2 = true;
                    scene.JingJiData2 = jingJiData;
                    scene.Robot2 = null;
                }
            }
        }

        /// <summary>
        /// 同步过来的其他服务器的支持日志
        /// </summary>
        /// <param name="data"></param>
        private void HandleSupportLog(ZhengBaSupportLogData data)
        {
            if (!Global.sendToDB<bool, ZhengBaSupportLogData>(
                (int)TCPGameServerCmds.CMD_DB_ZHENGBA_SAVE_SUPPORT_LOG, data, GameManager.LocalServerId))
                return;

            lock (Mutex)
            {
                Queue<ZhengBaSupportLogData> supportLogQ = null;
                if (!SupportLogs.TryGetValue(data.ToUnionGroup, out supportLogQ))
                {
                    SupportLogs[data.ToUnionGroup] = supportLogQ = new Queue<ZhengBaSupportLogData>();
                }

                supportLogQ.Enqueue(data);
                while (supportLogQ.Count > ZhengBaConsts.MaxSupportLogNum)
                    supportLogQ.Dequeue();
            }
        }

        /// <summary>
        /// 同步过来的PK日志
        /// </summary>
        /// <param name="data"></param>
        private void HandlePkLog(ZhengBaPkLogData data)
        {
            // 游戏服务器只记录胜利日志
            if (data.PkResult == (int)EZhengBaPKResult.Invalid)
                return;

            // 需求：游戏服务器只记录胜利并且晋级的日志
            if (!data.UpGrade)
                return;

            if (!Global.sendToDB<bool, ZhengBaPkLogData>(
                (int)TCPGameServerCmds.CMD_DB_ZHENGBA_SAVE_PK_LOG, data, GameManager.LocalServerId))
                return;

            lock (Mutex)
            {
                PkLogQ.Enqueue(data);
                while (PkLogQ.Count > ZhengBaConsts.MaxPkLogNum)
                    PkLogQ.Dequeue();
            }
        }

        /// <summary>
        /// 中心通知GameServer匹配成功，GameServer邀请玩家进入
        /// </summary>
        /// <param name="data"></param>
        private void HandleNtfEnter(ZhengBaNtfEnterData data)
        {
            GameClient client1 = GameManager.ClientMgr.FindClient(data.RoleId1);
            if (client1 != null && !client1.ClientSocket.IsKuaFuLogin)
            {
                bool bHasMirror = false;
                lock (Mutex) { bHasMirror = this.MirrorDatas.ContainsKey(data.RoleId1); }

                client1.ClientSocket.ClientKuaFuServerLoginData.RoleId = data.RoleId1;
                client1.ClientSocket.ClientKuaFuServerLoginData.GameId = data.GameId;
                client1.ClientSocket.ClientKuaFuServerLoginData.GameType = (int)GameTypes.ZhengBa;
                client1.ClientSocket.ClientKuaFuServerLoginData.EndTicks = 0;
                client1.ClientSocket.ClientKuaFuServerLoginData.ServerId = GameCoreInterface.getinstance().GetLocalServerId();
                client1.ClientSocket.ClientKuaFuServerLoginData.ServerIp = data.ToServerIp;
                client1.ClientSocket.ClientKuaFuServerLoginData.ServerPort = data.ToServerPort;
                client1.ClientSocket.ClientKuaFuServerLoginData.FuBenSeqId = 0;
                
                client1.sendCmd((int)TCPGameServerCmds.CMD_NTF_ZHENGBA_CAN_ENTER, 
                    string.Format("{0}:{1}:{2}:{3}", data.GameId, data.Day, data.Loop, bHasMirror ? 1 : 0));
// 
//                 int ec = TianTiClient.getInstance().ZhengBaRequestEnter(client1.ClientData.RoleID, data.GameId, EZhengBaEnterType.Player);
//                 if (ec >= 0)
//                 {
//                     GlobalNew.RecordSwitchKuaFuServerLog(client1);
//                     client1.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client1));
//                 }
            }

            GameClient client2 = GameManager.ClientMgr.FindClient(data.RoleId2);
            if (client2 != null && !client2.ClientSocket.IsKuaFuLogin)
            {
                bool bHasMirror = false;
                lock (Mutex) { bHasMirror = this.MirrorDatas.ContainsKey(data.RoleId2); }

                client2.ClientSocket.ClientKuaFuServerLoginData.RoleId = data.RoleId2;
                client2.ClientSocket.ClientKuaFuServerLoginData.GameId = data.GameId;
                client2.ClientSocket.ClientKuaFuServerLoginData.GameType = (int)GameTypes.ZhengBa;
                client2.ClientSocket.ClientKuaFuServerLoginData.EndTicks = 0;
                client2.ClientSocket.ClientKuaFuServerLoginData.ServerId = GameCoreInterface.getinstance().GetLocalServerId();
                client2.ClientSocket.ClientKuaFuServerLoginData.ServerIp = data.ToServerIp;
                client2.ClientSocket.ClientKuaFuServerLoginData.ServerPort = data.ToServerPort;
                client2.ClientSocket.ClientKuaFuServerLoginData.FuBenSeqId = 0;

                client2.sendCmd((int)TCPGameServerCmds.CMD_NTF_ZHENGBA_CAN_ENTER,
                    string.Format("{0}:{1}:{2}:{3}", data.GameId, data.Day, data.Loop, bHasMirror ? 1 : 0));

//                 int ec = TianTiClient.getInstance().ZhengBaRequestEnter(client2.ClientData.RoleID, data.GameId, EZhengBaEnterType.Player);
//                 if (ec >= 0)
//                 {
//                     GlobalNew.RecordSwitchKuaFuServerLog(client2);
//                     client2.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client2));
//                 }
            }
        }

        #endregion

        #region IEventListener
        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() == (int)EventTypes.PlayerDead)
            {
                HandleClientDead(((PlayerDeadEventObject)eventObject).getPlayer());
            }
            if (eventObject.getEventType() == (int)EventTypes.MonsterDead)
            {
                HandleMonsterDead(((MonsterDeadEventObject)eventObject).getAttacker(), ((MonsterDeadEventObject)eventObject).getMonster());
            }
        }

        private void HandleClientDead(GameClient player)
        {
            OnLogout(player);
        }

        private void HandleMonsterDead(GameClient player, Monster monster)
        {
            if (player == null) return;
            if (monster == null) return;
            Robot robot = monster as Robot;
            if (robot == null) return;

            if (player.ClientData.CopyMapID > 0 && player.ClientData.FuBenSeqID > 0)
            {

                lock (Mutex)
                {
                    ZhengBaCopyScene scene = null;
                    if (!FuBenSeq2CopyScenes.TryGetValue(player.ClientData.FuBenSeqID, out scene))
                        return;
                    if (player.ClientData.MapCode != scene.MapCode)
                        return;
                    if (monster.CurrentMapCode != scene.MapCode)
                        return;

                    // 只有开战状态的死亡才处理
                    if (scene.m_eStatus != GameSceneStatuses.STATUS_BEGIN)
                        return;

                    // 停下吧，机器人
                    if (scene.Robot1 != null) scene.Robot1.stopAttack();
                    if (scene.Robot2 != null) scene.Robot2.stopAttack();

                    // 玩家胜利
                    scene.Winner = player.ClientData.RoleID;

                    scene.m_eStatus = GameSceneStatuses.STATUS_END;
                }
            }
        }

        #endregion

        #region KuaFu
        public bool CanKuaFuLogin(KuaFuServerLoginData kuaFuServerLoginData)
        {
            if (TianTiClient.getInstance().ZhengBaKuaFuLogin(kuaFuServerLoginData.RoleId, (int)kuaFuServerLoginData.GameId) < 0)
                return false;

            return true;
        }

        public bool KuaFuInitGame(GameClient client)
        {
            lock (Mutex)
            {
                ZhengBaMatchConfig matchConfig = _Config.MatchConfigList.Find(_m => _m.Day == SyncData.RealActDay);
                if (matchConfig == null) return false;

                int gameId = (int)Global.GetClientKuaFuServerLoginData(client).GameId;
                if (gameId < 0) return false;

                GameSideInfo sideInfo = null;
                if (!GameId2FuBenSeq.TryGetValue(gameId, out sideInfo))
                {
                    sideInfo = new GameSideInfo();
                    sideInfo.FuBenSeq = GameCoreInterface.getinstance().GetNewFuBenSeqId();
                    GameId2FuBenSeq[gameId] = sideInfo;
                }

                int toX = 0, toY = 0;
                if (!GetBirthPoint(matchConfig.MapCode, ++sideInfo.CurrSide, out toX, out toY))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("roleid={0},mapcode={1},side={2} 未找到出生点",client.ClientData.RoleID,matchConfig.MapCode,sideInfo.CurrSide));
                    return false;
                }

                Global.GetClientKuaFuServerLoginData(client).FuBenSeqId = sideInfo.FuBenSeq;
                client.ClientData.MapCode = matchConfig.MapCode;
                client.ClientData.PosX = toX;
                client.ClientData.PosY = toY;
                client.ClientData.FuBenSeqID = sideInfo.FuBenSeq;
                client.ClientData.BattleWhichSide = sideInfo.CurrSide;

                return true;
            }
        }
        public void OnLogout(GameClient player)
        {
            if (player == null || !player.ClientSocket.IsKuaFuLogin)
                return;

            if (player.ClientData.CopyMapID > 0 && player.ClientData.FuBenSeqID > 0)
            {
                lock (Mutex)
                {
                    ZhengBaCopyScene scene = null;
                    if (!FuBenSeq2CopyScenes.TryGetValue(player.ClientData.FuBenSeqID, out scene))
                        return;
                    if (player.ClientData.MapCode != scene.MapCode)
                        return;

                    // 未结束，提前退出才处理
                    if (scene.m_eStatus < GameSceneStatuses.STATUS_BEGIN)
                    {
                        // 提前退出的第一个人判输
                        if (scene.FirstLeaveRoleId <= 0
                            && (scene.RoleId1 == player.ClientData.RoleID || scene.RoleId2 == player.ClientData.RoleID))
                        {
                            scene.FirstLeaveRoleId = player.ClientData.RoleID;
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                    {
                        // 对方已进来

                        // 停下吧，机器人
                        if (scene.Robot1 != null) scene.Robot1.stopAttack();
                        if (scene.Robot2 != null) scene.Robot2.stopAttack();

                        // 对方胜利
                        scene.Winner = 0;
                        if (player.ClientData.RoleID == scene.RoleId1 && scene.RoleId2 > 0) scene.Winner = scene.RoleId2;
                        else if (player.ClientData.RoleID == scene.RoleId2 && scene.RoleId1 > 0) scene.Winner = scene.RoleId1;

                        scene.m_eStatus = GameSceneStatuses.STATUS_END;
                    }
                }
            }
        }
        #endregion

        #region Copy Scene
        private bool GetBirthPoint(int mapCode, int side, out int toPosX, out int toPosY)
        {
            toPosX = -1;
            toPosY = -1;

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out gameMap))
            {
                return false;
            }

            int defaultBirthPosX = _Config.BirthPointList[side % _Config.BirthPointList.Count].X;
            int defaultBirthPosY = _Config.BirthPointList[side % _Config.BirthPointList.Count].Y;
            int defaultBirthRadius = _Config.BirthPointList[side % _Config.BirthPointList.Count].Radius;

            //从配置根据地图取默认位置
            Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, mapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
            toPosX = (int)newPos.X;
            toPosY = (int)newPos.Y;

            return true;
        }

        public void AddCopyScenes(GameClient client, CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (sceneType != SceneUIClasses.KFZhengBa) return;
            ZhengBaMatchConfig matchConfig = _Config.MatchConfigList.Find(_m => _m.Day == SyncData.RealActDay);
            if (matchConfig == null) return;

            int fuBenSeqId = copyMap.FuBenSeqID;
            int mapCode = copyMap.MapCode;
            lock (Mutex)
            {
                ZhengBaCopyScene scene = null;
                if (!this.FuBenSeq2CopyScenes.TryGetValue(fuBenSeqId, out scene))
                {
                    scene = new ZhengBaCopyScene();
                    scene.GameId = (int)Global.GetClientKuaFuServerLoginData(client).GameId;
                    scene.FuBenSeq = fuBenSeqId;
                    scene.MapCode = mapCode;

                    FuBenSeq2CopyScenes[fuBenSeqId] = scene;
                }

                // 走到这里说明，先进来了一个机器人
                if (scene.CopyMap == null)
                {
                    copyMap.IsKuaFuCopy = true;
                    copyMap.SetRemoveTicks(TimeUtil.NOW() + (matchConfig.WaitSeconds + matchConfig.FightSeconds + matchConfig.ClearSeconds) * TimeUtil.SECOND);
                    scene.CopyMap = copyMap;
                }

                if (scene.RoleId1 <= 0)
                {
                    scene.RoleId1 = client.ClientData.RoleID;
                    scene.IsMirror1 = false;
                }
                else if (scene.RoleId2 <= 0)
                {
                    scene.RoleId2 = client.ClientData.RoleID;
                    scene.IsMirror2 = false;
                }
            }
        }

        public void RemoveCopyScene(CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (copyMap == null || sceneType != SceneUIClasses.KFZhengBa)
                return;

            lock (Mutex)
            {
                ZhengBaCopyScene scene = null;
                if (FuBenSeq2CopyScenes.TryGetValue(copyMap.FuBenSeqID, out scene))
                {
                    FuBenSeq2CopyScenes.Remove(copyMap.FuBenSeqID);
                    GameId2FuBenSeq.Remove(scene.GameId);
                }
            }
        }

        private void ProcessEnd(ZhengBaCopyScene scene, DateTime now, long nowTicks, int clearSec)
        {
            //结算奖励
            scene.m_eStatus = GameSceneStatuses.STATUS_AWARD;
            scene.m_lEndTime = nowTicks;
            scene.m_lLeaveTime = scene.m_lEndTime + clearSec * TimeUtil.SECOND;

            scene.StateTimeData.GameType = (int)GameTypes.ZhengBa;
            scene.StateTimeData.State = (int)GameSceneStatuses.STATUS_END;
            scene.StateTimeData.EndTicks = scene.m_lLeaveTime;
            if (scene.CopyMap != null)
            {
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);
            }

            if (scene.Robot1 != null) scene.Robot1.stopAttack();
            if (scene.Robot2 != null) scene.Robot2.stopAttack();

            List<ZhengBaNtfPkResultData> pkResult = TianTiClient.getInstance().ZhengBaPkResult(scene.GameId, scene.Winner, scene.FirstLeaveRoleId);
            if (pkResult == null) return;
            foreach (var result in pkResult)
            {
                GameClient client = GameManager.ClientMgr.FindClient(result.RoleID);
                if (client != null && client.ClientData.MapCode == scene.MapCode)
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_NTF_ZHENGBA_PK_RESULT, result);
                }
            }
        }

        public void UpdateCopyScene(object sender, EventArgs e)
        {
            long nowTicks = TimeUtil.NOW();
            if (nowTicks < NextHeartBeatMs) return;
            NextHeartBeatMs = nowTicks + 100; //100毫秒执行一次, 因为有机器人

            ZhengBaMatchConfig matchConfig = _Config.MatchConfigList.Find(_m => _m.Day == SyncData.RealActDay);
            if (matchConfig == null) return;

            lock (Mutex)
            {
                foreach (var scene in this.FuBenSeq2CopyScenes.Values.ToList())
                {
                    DateTime now = TimeUtil.NowDateTime();
                    long ticks = TimeUtil.NOW();

                    if (scene.m_eStatus == GameSceneStatuses.STATUS_NULL)             // 如果处于空状态 -- 是否要切换到准备状态
                    {
                        scene.m_lPrepareTime = ticks;
                        // 策划说CopyPrepareTimeOutSecs的准备时间属于战斗时间的范围内
                        scene.m_lBeginTime = ticks + ZhengBaConsts.CopyPrepareTimeOutSecs * TimeUtil.SECOND;
                        scene.m_lEndTime = ticks + matchConfig.FightSeconds * TimeUtil.SECOND;
                        scene.m_eStatus = GameSceneStatuses.STATUS_PREPARE;

                        scene.StateTimeData.GameType = (int)GameTypes.ZhengBa;
                        scene.StateTimeData.State = (int)scene.m_eStatus;
                        scene.StateTimeData.EndTicks = scene.m_lBeginTime;
                        if (scene.CopyMap != null)
                        {
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_PREPARE)     // 场景战斗状态切换
                    {
                        //检查双方是否都进入了
                        if (scene.RoleId1 > 0 && scene.RoleId2 > 0)
                        {
                            scene.m_eStatus = GameSceneStatuses.STATUS_BEGIN;
                            //scene.m_lEndTime = ticks + matchConfig.FightSeconds * TimeUtil.SECOND;

                            scene.StateTimeData.GameType = (int)GameTypes.ZhengBa;
                            scene.StateTimeData.State = (int)scene.m_eStatus;
                            scene.StateTimeData.EndTicks = scene.m_lEndTime;
                            if (scene.CopyMap != null)
                            {
                                // 都是机器人出战的话，不创建CopyMap
                                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);

                                //放开光幕
                                scene.CopyMap.AddGuangMuEvent(1, 0);
                                GameManager.ClientMgr.BroadSpecialMapAIEvent(scene.CopyMap.MapCode, scene.CopyMap.CopyMapID, 1, 0);
                                scene.CopyMap.AddGuangMuEvent(2, 0);
                                GameManager.ClientMgr.BroadSpecialMapAIEvent(scene.CopyMap.MapCode, scene.CopyMap.CopyMapID, 2, 0);
                            }

                            if (scene.IsMirror1 && scene.IsMirror2) { }
                            else if (scene.IsMirror1)
                            {
                                GameClient player2 = GameManager.ClientMgr.FindClient(scene.RoleId2);
                                if (player2 != null && player2.ClientData.MapCode == scene.MapCode)
                                {
                                    scene.Robot1 = JingJiChangManager.getInstance().createRobot(player2, scene.JingJiData1);
                                    GameMap gameMap = GameManager.MapMgr.DictMaps[scene.MapCode];
                                    int RobotBothX, RobotBothY;
                                    int side = 0;
                                    GameSideInfo sideInfo = null;
                                    if (GameId2FuBenSeq.TryGetValue(scene.FuBenSeq, out sideInfo))
                                        side = ++sideInfo.CurrSide;

                                    GetBirthPoint(scene.MapCode, side, out RobotBothX, out RobotBothY);
                                    int gridX = gameMap.CorrectWidthPointToGridPoint(RobotBothX) / gameMap.MapGridWidth;
                                    int gridY = gameMap.CorrectHeightPointToGridPoint(RobotBothY) / gameMap.MapGridHeight;

                                    GameManager.MonsterZoneMgr.AddDynamicRobot(scene.MapCode, scene.Robot1, scene.CopyMap.CopyMapID, 1, gridX, gridY, 1);
                                    GameClient player = GameManager.ClientMgr.FindClient(scene.RoleId2);
                                    if (player != null)
                                        JingJiChangManager.getInstance().SendMySelfJingJiFakeRoleItem(player, scene.Robot1);
                                    scene.Robot1.startAttack();
                                }
                            }
                            else if (scene.IsMirror2)
                            {
                                GameClient player1 = GameManager.ClientMgr.FindClient(scene.RoleId1);
                                if (player1 != null && player1.ClientData.MapCode == scene.MapCode)
                                {
                                    scene.Robot2 = JingJiChangManager.getInstance().createRobot(player1, scene.JingJiData2);
                                    GameMap gameMap = GameManager.MapMgr.DictMaps[scene.MapCode];
                                    int RobotBothX, RobotBothY;
                                    int side = 0;
                                    GameSideInfo sideInfo = null;
                                    if (GameId2FuBenSeq.TryGetValue(scene.FuBenSeq, out sideInfo))
                                        side = ++sideInfo.CurrSide;

                                    GetBirthPoint(scene.MapCode, side, out RobotBothX, out RobotBothY);
                                    int gridX = gameMap.CorrectWidthPointToGridPoint(RobotBothX) / gameMap.MapGridWidth;
                                    int gridY = gameMap.CorrectHeightPointToGridPoint(RobotBothY) / gameMap.MapGridHeight;

                                    GameManager.MonsterZoneMgr.AddDynamicRobot(scene.MapCode, scene.Robot2, scene.CopyMap.CopyMapID, 1, gridX, gridY, 1);
                                    GameClient player = GameManager.ClientMgr.FindClient(scene.RoleId1);
                                    if (player != null)
                                        JingJiChangManager.getInstance().SendMySelfJingJiFakeRoleItem(player, scene.Robot2);
                                    scene.Robot2.startAttack();
                                }
                            }
                        }
                        else if (ticks >= scene.m_lBeginTime)
                        {
                            scene.Winner = 0; // wait time out
                            if (scene.RoleId1 > 0 && scene.FirstLeaveRoleId != scene.RoleId1) scene.Winner = scene.RoleId1;
                            else if (scene.RoleId2 > 0 && scene.FirstLeaveRoleId != scene.RoleId2) scene.Winner = scene.RoleId2;

                            scene.m_eStatus = GameSceneStatuses.STATUS_END;
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)       // 战斗开始
                    {
                        if (scene.FirstLeaveRoleId > 0)
                        {
                            // 两个人都进来了，并且有一个人提前退出了
                            scene.Winner = 0;
                            scene.m_eStatus = GameSceneStatuses.STATUS_END;
                        }
                        else if (ticks >= scene.m_lEndTime)
                        {
                            scene.Winner = 0;

                            if (scene.IsMirror1 && scene.IsMirror2)
                            {
                                // 都是机器人按段位排名决出胜负
                                TianTiPaiHangRoleData data1 = null, data2 = null;
                                if (this.RoleDataDict.TryGetValue(scene.RoleId1, out data1)
                                    && this.RoleDataDict.TryGetValue(scene.RoleId2, out data2))
                                {
                                    if (data1.DuanWeiRank < data2.DuanWeiRank) scene.Winner = data1.RoleId;
                                    else scene.Winner = data2.RoleId;
                                }
                            }
                            else if (scene.IsMirror1 || scene.IsMirror2)
                            {
                                // 1个机器人和1个玩家，比较血量
                                Robot robot = scene.IsMirror1 ? scene.Robot1 : scene.Robot2;
                                GameClient client = GameManager.ClientMgr.FindClient(scene.IsMirror1 ? scene.RoleId2 : scene.RoleId1);
                                
                                if (client != null && robot != null)
                                {
#if ___CC___FUCK___YOU___BB___
                                    int clientMaxLifeV = (int)RoleAlgorithm.GetMaxLifeV(client);
                                    if (clientMaxLifeV > 0 && robot.XMonsterInfo.MaxHP > 0)
                                    {
                                        if (client.ClientData.CurrentLifeV * 1.0 / clientMaxLifeV
                                            >= robot.VLife * 1.0 / robot.XMonsterInfo.MaxHP)
                                        {
                                            scene.Winner = client.ClientData.RoleID;
                                        }
                                        else
                                        {
                                            scene.Winner = robot.getRoleDataMini().RoleID;
                                        }
                                    }
                                    else scene.Winner = client.ClientData.RoleID;
#else
                                    int clientMaxLifeV = (int)RoleAlgorithm.GetMaxLifeV(client);
                                    if (clientMaxLifeV > 0 && robot.MonsterInfo.VLifeMax > 0)
                                    {
                                        if (client.ClientData.CurrentLifeV * 1.0 / clientMaxLifeV
                                            >= robot.VLife * 1.0 / robot.MonsterInfo.VLifeMax)
                                        {
                                            scene.Winner = client.ClientData.RoleID;
                                        }
                                        else
                                        {
                                            scene.Winner = robot.getRoleDataMini().RoleID;
                                        }
                                    }
                                    else scene.Winner = client.ClientData.RoleID;
#endif
                                }
                                else
                                {
                                    // 要考虑异常情况，导致机器人没有被创建出来，那么玩家胜利吧
                                    scene.Winner = scene.IsMirror1 ? scene.RoleId2 : scene.RoleId1;
                                }
                            }
                            else
                            {
                                // 两个玩家,比较血量
                                GameClient client1 = GameManager.ClientMgr.FindClient(scene.RoleId1);
                                GameClient client2 = GameManager.ClientMgr.FindClient(scene.RoleId2);
                                if (client1 != null && client2 != null)
                                {
                                    int clientMaxLifeV1 = (int)RoleAlgorithm.GetMaxLifeV(client1);
                                    int clientMaxLifeV2 = (int)RoleAlgorithm.GetMaxLifeV(client2);
                                    if (clientMaxLifeV1 > 0 && clientMaxLifeV2 > 0)
                                    {
                                        if (client1.ClientData.CurrentLifeV * 1.0 / clientMaxLifeV1
                                            >= client2.ClientData.CurrentLifeV * 1.0 / clientMaxLifeV2)
                                        {
                                            scene.Winner = client1.ClientData.RoleID;
                                        }
                                        else
                                        {
                                            scene.Winner = client2.ClientData.RoleID;
                                        }
                                    }
                                }
                            }

                            scene.m_eStatus = GameSceneStatuses.STATUS_END;
                        }
                        else if (scene.IsMirror1 && scene.IsMirror2) {
                            // 双方都是机器人，就不打了
                        }
                        else if (scene.Robot1 != null)
                        {
                            scene.Robot1.onUpdate();
                        }
                        else if (scene.Robot2 != null)
                        {
                            scene.Robot2.onUpdate();
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_END)         // 战斗结束
                    {
                        ProcessEnd(scene, now, nowTicks, matchConfig.ClearSeconds);
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_AWARD)
                    {
                        if (ticks >= scene.m_lLeaveTime)
                        {
                            scene.m_eStatus = GameSceneStatuses.STATUS_CLEAR;
                            if (scene.CopyMap != null)
                            {
                                scene.CopyMap.SetRemoveTicks(scene.m_lLeaveTime);
                                try
                                {
                                    List<GameClient> objsList = scene.CopyMap.GetClientsList();
                                    if (objsList != null && objsList.Count > 0)
                                    {
                                        for (int n = 0; n < objsList.Count; ++n)
                                        {
                                            GameClient c = objsList[n];
                                            if (c != null)
                                            {
                                                KuaFuManager.getInstance().GotoLastMap(c);
                                            }
                                        }
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    DataHelper.WriteExceptionLogEx(ex, "众神争霸系统清场调度异常");
                                }
                            }
                            else
                            {
                                // 经调试发现，有内存泄露的情况，因为没有玩家进入的话，那么copymap没有创建，就没法触发副本的删除事件
                                // 所有这里手动强制删除
                                this.FuBenSeq2CopyScenes.Remove(scene.FuBenSeq);
                            }
                        }
                    }
                }
            }
        }
#endregion
    }
}
