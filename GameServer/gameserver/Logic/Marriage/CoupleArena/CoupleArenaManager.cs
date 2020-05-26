using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Server.Tools.Pattern;
using GameServer.Server;
using Tmsk.Contract;
using GameServer.Core.GameEvent;
using Server.Tools;
using GameServer.Tools;
using GameServer.Core.Executor;
using Server.Data;
using KF.Client;
using KF.Contract.Data;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Interface;

namespace GameServer.Logic.Marriage.CoupleArena
{
    /// <summary>
    /// 夫妻竞技场
    /// </summary>
    public partial class CoupleArenaManager : SingletonTemplate<CoupleArenaManager>, IManager, ICmdProcessorEx, IEventListenerEx, IEventListener
    {
        #region `Member`
        /// <summary>
        /// lock
        /// </summary>
        private object Mutex = new object();

        /// <summary>
        /// 上次从从中心同步的数据
        /// </summary>
        private DateTime SyncDateTime = DateTime.MinValue;

        /// <summary>
        /// 从中心同步过来的排行榜
        /// </summary>
        private List<CoupleArenaCoupleJingJiData> SyncRankList = new List<CoupleArenaCoupleJingJiData>();

        /// <summary>
        /// 从中心同步过来的角色数据
        /// </summary>
        private Dictionary<int, CoupleArenaCoupleJingJiData> SyncRoleDict = new Dictionary<int, CoupleArenaCoupleJingJiData>();

        /// <summary>
        /// key: role id
        /// </summary>
        private Dictionary<int, ECoupleArenaMatchState> RoleMatchStateDict = new Dictionary<int, ECoupleArenaMatchState>();

        /// <summary>
        /// 角色开始准备的毫秒时间
        /// </summary>
        private Dictionary<int, long> RoleStartReadyMs = new Dictionary<int, long>();

        /// <summary>
        /// roleid --- match key
        /// </summary>
        private Dictionary<int, int> RoleMatchKeyDict = new Dictionary<int, int>();

        /// <summary>
        /// 关注配偶状态变更的玩家, 以便服务器推送状态变更通知
        /// </summary>
        private HashSet<int> coupleStateWatchers = new HashSet<int>();

        /// <summary>
        /// 战斗配置
        /// </summary>
        private CoupleAreanWarCfg WarCfg = new CoupleAreanWarCfg();

        /// <summary>
        /// 段位配置
        /// </summary>
        private List<CoupleAreanDuanWeiCfg> DuanWeiCfgList = new List<CoupleAreanDuanWeiCfg>();

        /// <summary>
        /// 周奖励配置
        /// </summary>
        private List<CoupleAreanWeekRankAwardCfg> WeekAwardCfgList = new List<CoupleAreanWeekRankAwardCfg>();

        /// <summary>
        /// buff配置
        /// </summary>
        private List<CoupleArenaBuffCfg> BuffCfgList = new List<CoupleArenaBuffCfg>();

        /// <summary>
        /// 出生点配置
        /// </summary>
        private List<TianTiBirthPoint> BirthPointList = new List<TianTiBirthPoint>();

        /// <summary>
        /// 持有真爱buff超过一段时间即胜利
        /// </summary>
        private int ZhenAiBuffHoldWinSec = 60;
        /// <summary>
        /// 夫妻竞技场勇气祝福对真爱祝福的额外伤害加成
        /// </summary>
        private double YongQiBuff2ZhenAiBuffHurt = 0.2;
        #endregion

        #region `Function` 加载配置

        public void InitSystenParams()
        {
            try
            {
                ZhenAiBuffHoldWinSec = (int)GameManager.systemParamsList.GetParamValueIntByName("CoupleVictoryNeedTime");
                YongQiBuff2ZhenAiBuffHurt = GameManager.systemParamsList.GetParamValueDoubleByName("CoupleBuffSpecificHurt");
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex.Message);
                ZhenAiBuffHoldWinSec = 60;
                YongQiBuff2ZhenAiBuffHurt = 0.2;
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private bool LoadConfig()
        {
            try
            {
                XElement xml;
                // 加载战斗配置文件
                xml = XElement.Load(Global.GameResPath(CoupleAreanConsts.WarCfgFile));
                if (xml.Elements().Count() < 1) throw new Exception(CoupleAreanConsts.WarCfgFile + " need at least 1 elements");
                foreach (var xmlItem in xml.Elements())
                {
                    WarCfg.Id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    WarCfg.MapCode = (int)Global.GetSafeAttributeLong(xmlItem, "MapCode");
                    WarCfg.WaitSec = (int)Global.GetSafeAttributeLong(xmlItem, "WaitingEnterSecs");
                    WarCfg.FightSec = (int)Global.GetSafeAttributeLong(xmlItem, "FightingSecs");
                    WarCfg.ClearSec = (int)Global.GetSafeAttributeLong(xmlItem, "ClearRolesSecs");
                    WarCfg.TimePoints = new List<CoupleAreanWarCfg.TimePoint>();
                    string[] weekPoints = Global.GetSafeAttributeStr(xmlItem, "TimePoints").Split(new char[] { ',', '-', '|' });
                    for (int i = 0; i < weekPoints.Length; i += 3)
                    {
                        var tp = new CoupleAreanWarCfg.TimePoint();
                        tp.Weekday = Convert.ToInt32(weekPoints[i]);
                        if (tp.Weekday < 1 || tp.Weekday > 7) throw new Exception("weekday error!");
                        tp.DayStartTicks = DateTime.Parse(weekPoints[i + 1]).TimeOfDay.Ticks;
                        tp.DayEndTicks = DateTime.Parse(weekPoints[i + 2]).TimeOfDay.Ticks;

                        WarCfg.TimePoints.Add(tp);
                    }

                    WarCfg.TimePoints.Sort((_l, _r) => { return _l.Weekday - _r.Weekday; });

                    // 只需要第一个子项，故意的break
                    break;
                }

                // 加载段位配置文件
                xml = XElement.Load(Global.GameResPath(CoupleAreanConsts.DuanWeiCfgFile));
                foreach (var xmlItem in xml.Elements())
                {
                    CoupleAreanDuanWeiCfg duanweiCfg = new CoupleAreanDuanWeiCfg();
                    duanweiCfg.Id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    duanweiCfg.Type = (int)Global.GetSafeAttributeLong(xmlItem, "Type");
                    duanweiCfg.Level = (int)Global.GetSafeAttributeLong(xmlItem, "Level");
                    duanweiCfg.NeedJiFen = (int)Global.GetSafeAttributeLong(xmlItem, "NeedCoupleDuanWeiJiFen");
                    duanweiCfg.WinJiFen = (int)Global.GetSafeAttributeLong(xmlItem, "WinJiFen");
                    duanweiCfg.LoseJiFen = (int)Global.GetSafeAttributeLong(xmlItem, "LoseJiFen");
                    duanweiCfg.WeekGetRongYaoTimes = (int)Global.GetSafeAttributeLong(xmlItem, "WeekRongYaoNum");
                    duanweiCfg.WinRongYao = (int)Global.GetSafeAttributeLong(xmlItem, "WinRongYu");
                    duanweiCfg.LoseRongYao = (int)Global.GetSafeAttributeLong(xmlItem, "LoseRongYu");

                    DuanWeiCfgList.Add(duanweiCfg);
                }
                // 段位升序排序
                DuanWeiCfgList.Sort((_l, _r) => {
                    if (_l.Type < _r.Type) return -1;
                    else if (_l.Type > _r.Type) return 1;
                    else
                    {
                        if (_l.Level > _r.Level) return -1;
                        else if (_l.Level < _r.Level) return 1;
                        else return 0;
                    }
                });
                // 检查下需求积分是否严格升序
                for (int i = 1; i < DuanWeiCfgList.Count; i++)
                {
                    var curr = DuanWeiCfgList[i];
                    var left = DuanWeiCfgList[i - 1];
                    if (curr.NeedJiFen <= left.NeedJiFen)
                    {
                        throw new Exception(string.Format("段位积分配置有问题{0}", curr.Id));
                    }
                }

                if (DuanWeiCfgList[0].NeedJiFen != 0)
                    throw new Exception(string.Format("段位积分配置有问题{0}", DuanWeiCfgList[0].Id));

                // 加载周排行奖励
                xml = XElement.Load(Global.GameResPath(CoupleAreanConsts.WeekRankAwardCfgFile));
                foreach (var xmlItem in xml.Elements())
                {
                    CoupleAreanWeekRankAwardCfg weekAwardCfg = new CoupleAreanWeekRankAwardCfg();
                    weekAwardCfg.Id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    weekAwardCfg.Name = Global.GetSafeAttributeStr(xmlItem, "Name");
                    weekAwardCfg.StartRank = (int)Global.GetSafeAttributeLong(xmlItem, "StarRank");
                    weekAwardCfg.EndRank = (int)Global.GetSafeAttributeLong(xmlItem, "EndRank");
                    weekAwardCfg.AwardGoods = GoodsHelper.ParseGoodsDataList(
                        Global.GetSafeAttributeStr(xmlItem, "Award").Split('|'), CoupleAreanConsts.WeekRankAwardCfgFile);

                    WeekAwardCfgList.Add(weekAwardCfg);
                }

                // 加载战斗buff配置
                xml = XElement.Load(Global.GameResPath(CoupleAreanConsts.BuffCfgFile));
                foreach (var xmlItem in xml.Elements())
                {
                    CoupleArenaBuffCfg buffCfg = new CoupleArenaBuffCfg();
                    buffCfg.Type = (int)Global.GetSafeAttributeLong(xmlItem, "TypeID");
                    buffCfg.Name = Global.GetSafeAttributeStr(xmlItem, "Name");
                    buffCfg.MonsterId = (int)Global.GetSafeAttributeLong(xmlItem, "MonstersID");

                    buffCfg.RandPosList = new List<CoupleArenaBuffCfg.RandPos>();
                    string[] szBuffPos = Global.GetSafeAttributeStr(xmlItem, "Site").Split(new char[] { '|', ',' });
                    for (int i = 0; i < szBuffPos.Length - 2; i += 3)
                    {
                        var randPos = new CoupleArenaBuffCfg.RandPos();
                        randPos.X = Convert.ToInt32(szBuffPos[i]);
                        randPos.Y = Convert.ToInt32(szBuffPos[i + 1]);
                        randPos.R = Convert.ToInt32(szBuffPos[i + 2]);

                        buffCfg.RandPosList.Add(randPos);
                    }

                    buffCfg.FlushSecList = new List<int>();
                    string[] szFlushSec = Global.GetSafeAttributeStr(xmlItem, "Time").Split('|');
                    for (int i = 0; i < szFlushSec.Length; i++)
                    {
                        buffCfg.FlushSecList.Add(Convert.ToInt32(szFlushSec[i]));
                    }

                    buffCfg.ExtProps = new Dictionary<ExtPropIndexes, double>();
                    string[] szExtProps = Global.GetSafeAttributeStr(xmlItem, "Property").Split(new char[] { '|', ',' });
                    for (int i = 0; i < szExtProps.Length - 1; i += 2)
                    {
                        buffCfg.ExtProps.Add(
                            (ExtPropIndexes)Enum.Parse(typeof(ExtPropIndexes), szExtProps[i]),
                            Convert.ToDouble(szExtProps[i + 1]));
                    }

                    BuffCfgList.Add(buffCfg);
                }

                xml = XElement.Load(Global.GameResPath(CoupleAreanConsts.BirthPointCfgFile));
                foreach (var xmlItem in xml.Elements())
                {
                    TianTiBirthPoint bp = new TianTiBirthPoint();
                    bp.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    bp.PosX = (int)Global.GetSafeAttributeLong(xmlItem, "PosX");
                    bp.PosY = (int)Global.GetSafeAttributeLong(xmlItem, "PosY");
                    bp.BirthRadius = (int)Global.GetSafeAttributeLong(xmlItem, "BirthRadius");
                    BirthPointList.Add(bp);
                }
                if (BirthPointList.Count != 2) throw new Exception(CoupleAreanConsts.BirthPointCfgFile);

                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteException("CoupleArenaManager loadconfig. " + ex.Message);
                return false;
            }
        }
        #endregion

        #region Implement Interface `IManager`
        public bool initialize()
        {
            if (!LoadConfig())
                return false;

            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("CoupleArenaManager.TimerProc", TimerProc), 20000, 10000);

            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_ARENA_GET_MAIN_DATA, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_ARENA_GET_PAI_HANG, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_ARENA_GET_ZHAN_BAO, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_ARENA_SET_READY, 2, 2, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_ARENA_SINGLE_JOIN, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_ARENA_ENTER, 2, 2, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_ARENA_QUIT, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_ARENA_REG_STATE_WATCHER, 2, 2, Instance());

            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.CoupleArenaCanEnter, (int)SceneUIClasses.CoupleArena, Instance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerDead, Instance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, Instance());

            return true;
        }

        public bool showdown()
        {
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.CoupleArenaCanEnter, (int)SceneUIClasses.CoupleArena, Instance());
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
                case (int)TCPGameServerCmds.CMD_COUPLE_ARENA_GET_MAIN_DATA:
                    return HandleGetMainDataCommand(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_COUPLE_ARENA_GET_PAI_HANG:
                    return HandleGetPaiHangCommand(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_COUPLE_ARENA_GET_ZHAN_BAO:
                    return HandleGetZhanBaoCommand(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_COUPLE_ARENA_SET_READY:
                    return HandleSetReadyCommand(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_COUPLE_ARENA_SINGLE_JOIN:
                    return HandleSingleJoinCommand(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_COUPLE_ARENA_QUIT:
                    return HandleQuitCommand(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_COUPLE_ARENA_ENTER:
                    return HandleEnterCommand(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_COUPLE_ARENA_REG_STATE_WATCHER:
                    return HandleRegStateWatcherCommand(client, nID, bytes, cmdParams);
            }

            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }
        #endregion

        #region Implement Interface `IEventListenerEx`
        public void processEvent(EventObjectEx eventObject)
        {
            if (eventObject.EventType == (int)GlobalEventTypes.CoupleArenaCanEnter)
            {
                this.HandleCanEnterEvent((eventObject as CoupleArenaCanEnterEvent).Data);
            }

            eventObject.Handled = true;
        }

        private void HandleCanEnterEvent(CoupleArenaCanEnterData data)
        {
            string gsIp, dbIp, logIp;
            int gsPort, dbPort, logPort;
            if (!TianTiClient.getInstance().GetKuaFuDbServerInfo(data.KfServerId, out dbIp, out dbPort, out logIp, out logPort, out gsIp, out gsPort))
            {
                LogManager.WriteLog(LogTypes.Error,
                    string.Format("夫妻竞技被分配到服务器ServerId={0}, 但是找不到该跨服活动服务器", data.KfServerId));
                return;
            }

            lock (Mutex)
            {
                GameClient client1 = GameManager.ClientMgr.FindClient(data.RoleId1);
                if (client1 != null && GetMatchState(data.RoleId1) == ECoupleArenaMatchState.Ready)
                {
                    client1.ClientSocket.ClientKuaFuServerLoginData.RoleId = data.RoleId1;
                    client1.ClientSocket.ClientKuaFuServerLoginData.GameId = data.GameId;
                    client1.ClientSocket.ClientKuaFuServerLoginData.GameType = (int)GameTypes.CoupleArena;
                    client1.ClientSocket.ClientKuaFuServerLoginData.EndTicks = 0;
                    client1.ClientSocket.ClientKuaFuServerLoginData.ServerId = GameCoreInterface.getinstance().GetLocalServerId();
                    client1.ClientSocket.ClientKuaFuServerLoginData.ServerIp = gsIp;
                    client1.ClientSocket.ClientKuaFuServerLoginData.ServerPort = gsPort;
                    client1.ClientSocket.ClientKuaFuServerLoginData.FuBenSeqId = 0;
                     
                    // 可进入, 随便发个东西过期
                    client1.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_NTF_CAN_ENTER, "1");
                    SetMatchState(data.RoleId1, ECoupleArenaMatchState.OnLine);
                    NtfCoupleMatchState(client1.ClientData.RoleID);
                    if (MarryLogic.IsMarried(client1.ClientData.RoleID))
                        NtfCoupleMatchState(client1.ClientData.MyMarriageData.nSpouseID);
                }

                GameClient client2 = GameManager.ClientMgr.FindClient(data.RoleId2);
                if (client2 != null && GetMatchState(data.RoleId2) == ECoupleArenaMatchState.Ready)
                {
                    client2.ClientSocket.ClientKuaFuServerLoginData.RoleId = data.RoleId2;
                    client2.ClientSocket.ClientKuaFuServerLoginData.GameId = data.GameId;
                    client2.ClientSocket.ClientKuaFuServerLoginData.GameType = (int)GameTypes.CoupleArena;
                    client2.ClientSocket.ClientKuaFuServerLoginData.EndTicks = 0;
                    client2.ClientSocket.ClientKuaFuServerLoginData.ServerId = GameCoreInterface.getinstance().GetLocalServerId();
                    client2.ClientSocket.ClientKuaFuServerLoginData.ServerIp = gsIp;
                    client2.ClientSocket.ClientKuaFuServerLoginData.ServerPort = gsPort;
                    client2.ClientSocket.ClientKuaFuServerLoginData.FuBenSeqId = 0;

                    // 可进入, 随便发个东西过期
                    client2.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_NTF_CAN_ENTER, "1");
                    SetMatchState(data.RoleId2, ECoupleArenaMatchState.OnLine);
                    NtfCoupleMatchState(client2.ClientData.RoleID);
                    if (MarryLogic.IsMarried(client2.ClientData.RoleID))
                        NtfCoupleMatchState(client2.ClientData.MyMarriageData.nSpouseID);
                }
            }
        }
        #endregion

        #region Implement Interface `IEventListener`
        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() == (int)EventTypes.MonsterDead)
            {
                MonsterDeadEventObject deadEv = eventObject as MonsterDeadEventObject;
                if (deadEv.getAttacker().ClientData.CopyMapID > 0 && deadEv.getAttacker().ClientData.FuBenSeqID > 0
                    && deadEv.getAttacker().ClientData.MapCode == WarCfg.MapCode
                    && deadEv.getMonster().CurrentMapCode == WarCfg.MapCode
                    )
                {
                    lock (Mutex)
                    {
                        CoupleArenaCopyScene scene;
                        if (FuBenSeq2CopyScenes.TryGetValue(deadEv.getAttacker().ClientData.FuBenSeqID, out scene))
                            OnMonsterDead(scene, deadEv.getAttacker(), deadEv.getMonster());                        
                    }
                }
            }
            else if (eventObject.getEventType() == (int)EventTypes.PlayerDead)
            {
                PlayerDeadEventObject deadEv = eventObject as PlayerDeadEventObject;
                if (deadEv.getPlayer().ClientData.CopyMapID > 0 && deadEv.getPlayer().ClientData.FuBenSeqID > 0
                    && deadEv.getPlayer().ClientData.MapCode == WarCfg.MapCode)
                {
                    lock (Mutex)
                    {
                        CoupleArenaCopyScene scene;
                        if (FuBenSeq2CopyScenes.TryGetValue(deadEv.getPlayer().ClientData.FuBenSeqID, out scene))
                            OnPlayerDead(scene, deadEv.getPlayer(), deadEv.getAttackerRole());
                    }
                }
            }
        }
        #endregion

        #region Handle Client Net Command
        /// <summary>
        /// 注册关注夫妻状态变更的事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool HandleRegStateWatcherCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            int roleid = Convert.ToInt32(cmdParams[0]);
            int watch = Convert.ToInt32(cmdParams[1]);
            RegStateWatcher(client.ClientData.RoleID, watch > 0 ? true : false);
            NtfCoupleMatchState(client.ClientData.RoleID);
            return true;
        }

        /// <summary>
        /// 玩家请求进入
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool HandleEnterCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            int roleid = Convert.ToInt32(cmdParams[0]);
            int enter = Convert.ToInt32(cmdParams[1]);

            if (enter <= 0)
            {
                Global.GetClientKuaFuServerLoginData(client).RoleId = 0;
                client.sendCmd(nID, StdErrorCode.Error_Success.ToString());
            }
            else
            {
                GlobalNew.RecordSwitchKuaFuServerLog(client);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client));
            }

            return true;
        }

        /// <summary>
        /// 客户端请求取消匹配
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool HandleQuitCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (!IsGongNengOpened(client) || !MarryLogic.IsMarried(client.ClientData.RoleID))
                return true;

            lock (Mutex)
            {
                if (GetMatchState(client.ClientData.RoleID) != ECoupleArenaMatchState.Ready)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Success.ToString());
                    return true;
                }

                TianTiClient.getInstance().CoupleArenaQuit(client.ClientData.RoleID, client.ClientData.MyMarriageData.nSpouseID);
                SetMatchState(client.ClientData.RoleID, ECoupleArenaMatchState.OnLine);
                if (GetMatchState(client.ClientData.MyMarriageData.nSpouseID) == ECoupleArenaMatchState.Ready)
                {
                    SetMatchState(client.ClientData.MyMarriageData.nSpouseID, ECoupleArenaMatchState.OnLine);
                    GameClient spouseClient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);
                    if (spouseClient != null)
                    {
                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, spouseClient,
                            "您的伴侣取消了匹配", GameInfoTypeIndexes.Normal, ShowGameInfoTypes.ErrAndBox);
                        spouseClient.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_QUIT, StdErrorCode.Error_Success.ToString());
                    }
                }

                NtfCoupleMatchState(client.ClientData.RoleID);
                NtfCoupleMatchState(client.ClientData.MyMarriageData.nSpouseID);

                client.sendCmd(nID, StdErrorCode.Error_Success.ToString());
                return true;
            }
        }

        /// <summary>
        /// 客户端进行单人匹配
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool HandleSingleJoinCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            lock (Mutex)
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Operation_Denied.ToString());
                    return true;
                }

                if (!IsInWeekOnceActTimes(TimeUtil.NowDateTime()))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Not_In_valid_Time.ToString());
                    return true;
                }

                // 配偶在线且开启了此系统，不允许单人匹配
                if (MarryLogic.IsMarried(client.ClientData.RoleID)
                    && GetMatchState(client.ClientData.MyMarriageData.nSpouseID) != ECoupleArenaMatchState.Offline
                    && GetMatchState(client.ClientData.MyMarriageData.nSpouseID) != ECoupleArenaMatchState.NotOpen)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Operation_Denied.ToString());
                    return true;
                }

                // 我必须处于在线未匹配状态, 可过滤掉NotOpen状态
                if (GetMatchState(client.ClientData.RoleID) != ECoupleArenaMatchState.OnLine)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Operation_Denied.ToString());
                    return true;
                }

                // 通知中心单人匹配
                int ret = TianTiClient.getInstance().CoupleArenaJoin(
                    client.ClientData.RoleID, 
                    client.ClientData.MyMarriageData.nSpouseID,
                    GameCoreInterface.getinstance().GetLocalServerId());
                if (ret >= 0)
                {
                    SetMatchState(client.ClientData.RoleID, ECoupleArenaMatchState.Ready);
                    NtfCoupleMatchState(client.ClientData.RoleID);
                    NtfCoupleMatchState(client.ClientData.MyMarriageData.nSpouseID);
                    GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 1, 0, 0, 0, (int)GameTypes.CoupleArena);
                }

                client.sendCmd(nID, ret.ToString());
                return true;
            }
        }

        /// <summary>
        /// 客户端设置、取消准备状态
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool HandleSetReadyCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            int roleid = Convert.ToInt32(cmdParams[0]);
            bool bReady = Convert.ToInt32(cmdParams[1]) > 0 ? true : false;

            if (!IsGongNengOpened(client))
            {
                client.sendCmd(nID, StdErrorCode.Error_Operation_Denied.ToString());
                return true;
            }

            if (!IsInWeekOnceActTimes(TimeUtil.NowDateTime()))
            {
                client.sendCmd(nID, StdErrorCode.Error_Not_In_valid_Time.ToString());
                return true;
            }

            lock (Mutex)
            {
                var oldState = GetMatchState(client.ClientData.RoleID);
                var newState = bReady ? ECoupleArenaMatchState.Ready : ECoupleArenaMatchState.OnLine;
                if (oldState == newState) return true;

                SetMatchState(client.ClientData.RoleID, newState);
                NtfCoupleMatchState(client.ClientData.RoleID);
                NtfCoupleMatchState(client.ClientData.MyMarriageData.nSpouseID);

                if (oldState != ECoupleArenaMatchState.Ready && newState == ECoupleArenaMatchState.Ready
                    && GetMatchState(client.ClientData.MyMarriageData.nSpouseID) == ECoupleArenaMatchState.Ready)
                {
                    // 首次进入双方准备状态, 自动开始匹配
                    CoupleArenaJoinData req = new CoupleArenaJoinData();
                    req.RoleId1 = client.ClientData.RoleID;
                    req.RoleId2 = client.ClientData.MyMarriageData.nSpouseID;
                    int ret = TianTiClient.getInstance().CoupleArenaJoin(
                        client.ClientData.RoleID, 
                        client.ClientData.MyMarriageData.nSpouseID,
                        GameCoreInterface.getinstance().GetLocalServerId());
                    if (ret >= 0)
                    {
                        // 通知双方进入匹配倒计时界面
                        client.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_SINGLE_JOIN, ret.ToString());
                        GameClient spouseClient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);
                        if (spouseClient != null)
                            spouseClient.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_SINGLE_JOIN, ret.ToString());
                    }
                }

                if (newState == ECoupleArenaMatchState.Ready)
                {
                    GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 1, 0, 0, 0, (int)GameTypes.CoupleArena);
                }

                return true;
            }
        }

        /// <summary>
        /// 客户端查询战报
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool HandleGetZhanBaoCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            List<CoupleArenaZhanBaoItemData> items = null;
            if (IsGongNengOpened(client))
            {
                // 转到db，db做缓存，跨服服务器产生的战报直接给dbserver
                items = Global.sendToDB<List<CoupleArenaZhanBaoItemData>, string>(
                    nID, string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.MyMarriageData.nSpouseID), client.ServerId);
            }
            client.sendCmd(nID, items);
            return true;
        }

        /// <summary>
        /// 客户端查询排行榜
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool HandleGetPaiHangCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            CoupleArenaPaiHangData data = new CoupleArenaPaiHangData();

            lock (Mutex)
            {
                data.PaiHang = SyncRankList.GetRange(0, Math.Min(10, SyncRankList.Count));
            }

            client.sendCmd(nID, data);
            return true;
        }

        /// <summary>
        /// 客户端查询主界面信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool HandleGetMainDataCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            DateTime now = TimeUtil.NowDateTime();
            CoupleArenaMainData data = new CoupleArenaMainData();
            data.JingJiData = null;
            data.WeekGetRongYaoTimes = 0;
            data.CanGetAwardId = 0;

            CoupleArenaCoupleJingJiData jingJiData = new CoupleArenaCoupleJingJiData();
            jingJiData.ManRoleId = client.ClientData.RoleID;
            jingJiData.ManZoneId = client.ClientData.ZoneID;
            jingJiData.ManSelector = Global.sendToDB<RoleData4Selector, string>(
                (int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", client.ClientData.RoleID), client.ServerId);

            if (MarryLogic.IsMarried(client.ClientData.RoleID))
            {
                jingJiData.WifeSelector = Global.sendToDB<RoleData4Selector, string>(
                    (int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", client.ClientData.MyMarriageData.nSpouseID), client.ServerId);
                if (jingJiData.WifeSelector != null)
                {
                    jingJiData.WifeRoleId = jingJiData.WifeSelector.RoleID;
                    jingJiData.WifeZoneId = jingJiData.WifeSelector.ZoneId;
                }
            }

            if ((!MarryLogic.IsMarried(client.ClientData.RoleID) && client.ClientData.RoleSex == (int)ERoleSex.Girl)
                || client.ClientData.MyMarriageData.byMarrytype == 2)
            {
                int tmpRoleId = jingJiData.ManRoleId;
                int tmpZoneId = jingJiData.ManZoneId;
                RoleData4Selector tmpSelector = jingJiData.ManSelector;
                jingJiData.ManRoleId = jingJiData.WifeRoleId;
                jingJiData.ManZoneId = jingJiData.WifeZoneId;
                jingJiData.ManSelector = jingJiData.WifeSelector;
                jingJiData.WifeRoleId = tmpRoleId;
                jingJiData.WifeZoneId = tmpZoneId;
                jingJiData.WifeSelector = tmpSelector;
            }

            jingJiData.DuanWeiType = DuanWeiCfgList[0].Type;
            jingJiData.DuanWeiLevel = DuanWeiCfgList[0].Level;
            jingJiData.JiFen = 0;

            if (MarryLogic.IsMarried(client.ClientData.RoleID))
            {
                CoupleArenaCoupleJingJiData coupleData = GetCachedCoupleData(client.ClientData.RoleID);
                if (coupleData != null)
                {
                    jingJiData.TotalFightTimes = coupleData.TotalFightTimes;
                    jingJiData.WinFightTimes = coupleData.WinFightTimes;
                    jingJiData.LianShengTimes = coupleData.LianShengTimes;
                    jingJiData.DuanWeiType = coupleData.DuanWeiType;
                    jingJiData.DuanWeiLevel = coupleData.DuanWeiLevel;
                    jingJiData.JiFen = coupleData.JiFen;
                    jingJiData.Rank = coupleData.Rank;
                }
            }

            data.JingJiData = jingJiData;

            string szWeekRongYao = Global.GetRoleParamByName(client, RoleParamName.CoupleArenaWeekRongYao);
            if (!string.IsNullOrEmpty(szWeekRongYao))
            {
                string[] fields = szWeekRongYao.Split(',');
                if (fields != null && fields.Length == 2 && Convert.ToInt32(fields[0]) == CurrRankWeek(now))
                {
                    data.WeekGetRongYaoTimes = Convert.ToInt32(fields[1]);
                }
            }

            int willAwardWeek;
            if (IsInWeekAwardTime(now, out willAwardWeek))
            {
                CoupleArenaCoupleJingJiData coupleData = GetCachedCoupleData(client.ClientData.RoleID);
                if (coupleData != null)
                {
                    string szWeekAward = Global.GetRoleParamByName(client, RoleParamName.CoupleArenaWeekAward);
                    string[] fields = szWeekAward == null ? null : szWeekAward.Split(',');
                    if (fields == null || fields.Length != 2 || Convert.ToInt32(fields[0]) != willAwardWeek)
                    {
                        int awardId = 0;
                        foreach (var award in WeekAwardCfgList)
                        {
                            if (coupleData.Rank >= award.StartRank && (award.EndRank == -1 || coupleData.Rank <= award.EndRank))
                            {
                                Global.SaveRoleParamsStringToDB(client, RoleParamName.CoupleArenaWeekAward, string.Format("{0},{1}", willAwardWeek, award.Id), true);
                                data.CanGetAwardId = award.Id;
                                if (Global.CanAddGoodsDataList(client, award.AwardGoods))
                                {
                                    foreach (var goodsData in award.AwardGoods)
                                    {
                                        Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, "情侣竞技场", false,
                                                                           goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, true);
                                    }
                                }
                                else
                                {
                                    Global.UseMailGivePlayerAward3(client.ClientData.RoleID, award.AwardGoods, "情侣竞技场", string.Format("情侣竞技场第{0}名奖励，请查收！", coupleData.Rank), 0);
                                }

                                break;
                            }
                        }           
                    }
                    CheckTipsIconState(client);
                }
            }

            client.sendCmd(nID, data);
            return true;
        }
        #endregion

        #region Client Log-in-out、Marry、Divoce Conditions
        /// <summary>
        /// 玩家上线
        /// </summary>
        /// <param name="client"></param>
        public void OnClientLogin(GameClient client)
        {
            if (client == null) return;
            CheckFengHuoJiaRenChengHao(client);
            if (!client.ClientSocket.IsKuaFuLogin)
            {
                lock (Mutex)
                {
                    RegStateWatcher(client.ClientData.RoleID, false);
                    SetMatchState(client.ClientData.RoleID, 
                        IsGongNengOpened(client) && MarryLogic.IsMarried(client.ClientData.RoleID) ? ECoupleArenaMatchState.OnLine : ECoupleArenaMatchState.NotOpen);
                    if (GetMatchState(client.ClientData.RoleID) == ECoupleArenaMatchState.OnLine)
                    {
                        if (GetMatchState(client.ClientData.MyMarriageData.nSpouseID) == ECoupleArenaMatchState.Ready)
                        {
                            TianTiClient.getInstance().CoupleArenaQuit(client.ClientData.RoleID, client.ClientData.MyMarriageData.nSpouseID);
                            SetMatchState(client.ClientData.MyMarriageData.nSpouseID, ECoupleArenaMatchState.OnLine);
                            GameClient spouseClient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);
                            if (spouseClient != null)
                            {
                                if (RoleStartReadyMs.ContainsKey(spouseClient.ClientData.RoleID)
                                    && RoleStartReadyMs[spouseClient.ClientData.RoleID] + 60 * 1000 > TimeUtil.NOW())
                                {
                                    // 只在开始准备的60s内才能提示
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, spouseClient,
                                        "情侣双方均在线，无法单人匹配", GameInfoTypeIndexes.Normal, ShowGameInfoTypes.ErrAndBox);
                                }                              
                                spouseClient.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_QUIT, StdErrorCode.Error_Success.ToString());
                            }                         
                        }

                        NtfCoupleMatchState(client.ClientData.MyMarriageData.nSpouseID);
                    }

                    CheckTipsIconState(client);
                }
            }
        }

        /// <summary>
        /// 玩家下线
        /// </summary>
        /// <param name="client"></param>
        public void OnClientLogout(GameClient client)
        {
            try
            {
                if (client == null) return;
                if (!client.ClientSocket.IsKuaFuLogin)
                {
                    lock (Mutex)
                    {
                        RegStateWatcher(client.ClientData.RoleID, false);
                        if (GetMatchState(client.ClientData.RoleID) == ECoupleArenaMatchState.Ready
                            || GetMatchState(client.ClientData.RoleID) == ECoupleArenaMatchState.OnLine)
                        {
                            TianTiClient.getInstance().CoupleArenaQuit(client.ClientData.RoleID, client.ClientData.MyMarriageData.nSpouseID);

                            // 双人匹配中下线，取消匹配
                            if (GetMatchState(client.ClientData.MyMarriageData.nSpouseID) == ECoupleArenaMatchState.Ready)
                            {
                                SetMatchState(client.ClientData.MyMarriageData.nSpouseID, ECoupleArenaMatchState.OnLine);
                                GameClient spouseClient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);
                                if (spouseClient != null)
                                    spouseClient.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_QUIT, StdErrorCode.Error_Success.ToString());
                            }
                        }
                        SetMatchState(client.ClientData.RoleID, ECoupleArenaMatchState.Offline);
                        NtfCoupleMatchState(client.ClientData.MyMarriageData.nSpouseID);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex.Message);
            }
        }

        /// <summary>
        /// 检查功能是否开启
        /// </summary>
        /// <param name="client"></param>
        public void CheckGongNengOpen(GameClient client)
        {
            lock (Mutex)
            {
                if (GetMatchState(client.ClientData.RoleID) == ECoupleArenaMatchState.NotOpen && IsGongNengOpened(client))
                {
                    SetMatchState(client.ClientData.RoleID, ECoupleArenaMatchState.OnLine);
                    NtfCoupleMatchState(client.ClientData.RoleID);

                    // 玩家首次开启了功能，当配偶正在单人匹配时，取消配偶的匹配状态
                    if (GetMatchState(client.ClientData.MyMarriageData.nSpouseID) == ECoupleArenaMatchState.Ready)
                    {
                        GameClient spouseClient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);
                        if (spouseClient != null)
                        {
                            Global.BroadcastRoleActionMsg(spouseClient, RoleActionsMsgTypes.HintMsg, "情侣双方均在线，无法单人匹配", true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                        }

                        // cancel
                        TianTiClient.getInstance().CoupleArenaQuit(client.ClientData.RoleID, client.ClientData.MyMarriageData.nSpouseID);
                        SetMatchState(client.ClientData.MyMarriageData.nSpouseID, ECoupleArenaMatchState.OnLine);
                    }

                    NtfCoupleMatchState(client.ClientData.MyMarriageData.nSpouseID);
                }
            }
        }

        /// <summary>
        /// 结婚了
        /// </summary>
        /// <param name="client1"></param>
        /// <param name="client2"></param>
        public void OnMarry(GameClient client1, GameClient client2)
        {
            lock (Mutex)
            {
                SetMatchState(client1.ClientData.RoleID, IsGongNengOpened(client1) ? ECoupleArenaMatchState.OnLine : ECoupleArenaMatchState.NotOpen);
                SetMatchState(client2.ClientData.RoleID, IsGongNengOpened(client2) ? ECoupleArenaMatchState.OnLine : ECoupleArenaMatchState.NotOpen);
                NtfCoupleMatchState(client1.ClientData.RoleID);
                NtfCoupleMatchState(client2.ClientData.RoleID);
            }
        }

        /// <summary>
        /// 离婚了
        /// </summary>
        /// <param name="roleId1"></param>
        /// <param name="roleId2"></param>
        public void OnDivorce(int roleId1, int roleId2)
        {
            lock (Mutex)
            {
                if (GetMatchState(roleId1) == ECoupleArenaMatchState.Ready
                    || GetMatchState(roleId2) == ECoupleArenaMatchState.Ready)
                {
                    TianTiClient.getInstance().CoupleArenaQuit(roleId1, roleId2);
                }

                var oldState1 = GetMatchState(roleId1);
                var oldState2 = GetMatchState(roleId2);

                SetMatchState(roleId1, (oldState1 == ECoupleArenaMatchState.OnLine || oldState1 == ECoupleArenaMatchState.Ready) ? ECoupleArenaMatchState.NotOpen : oldState1);
                SetMatchState(roleId2, (oldState2 == ECoupleArenaMatchState.OnLine || oldState2 == ECoupleArenaMatchState.Ready) ? ECoupleArenaMatchState.NotOpen : oldState2);
                NtfCoupleMatchState(roleId1);
                NtfCoupleMatchState(roleId2);

                if (oldState1 == ECoupleArenaMatchState.Ready)
                {
                    GameClient client1 = GameManager.ClientMgr.FindClient(roleId1);
                    if (client1 != null)
                        client1.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_QUIT, StdErrorCode.Error_Success.ToString());
                }

                if (oldState2 == ECoupleArenaMatchState.Ready)
                {
                    GameClient client2 = GameManager.ClientMgr.FindClient(roleId2);
                    if (client2 != null)
                        client2.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_QUIT, StdErrorCode.Error_Success.ToString());
                }

                CheckFengHuoJiaRenChengHao(GameManager.ClientMgr.FindClient(roleId1));
                CheckFengHuoJiaRenChengHao(GameManager.ClientMgr.FindClient(roleId2));

                Global.sendToDB<bool, string>((int)TCPGameServerCmds.CMD_COUPLE_ARENA_DB_CLR_ZHAN_BAO, string.Format("{0}:{1}", roleId1, roleId2), GameManager.LocalServerId);
            }
        }

        /// <summary>
        /// 即将离婚，通知中心清除竞技、排行数据，通知dbserver清除战报数据
        /// 清除成功返回true
        /// </summary>
        /// <param name="roleId1"></param>
        /// <param name="roleId2"></param>
        /// <returns></returns>
        public bool PreClearDivorceData(int roleId1, int roleId2)
        {
            if (TianTiClient.getInstance().CoupleArenaPreDivorce(roleId1, roleId2) >= 0)
             return true;

            return false;
        }

        /// <summary>
        /// 配偶申请了离婚, 取消掉匹配界面
        /// </summary>
        /// <param name="client"></param>
        public void OnSpouseRequestDivorce(GameClient client, GameClient spouseClient)
        {
            if (client == null) return;
            if (spouseClient == null) return;

            lock (Mutex)
            {
                if (GetMatchState(client.ClientData.RoleID) == ECoupleArenaMatchState.Ready)
                {
                    TianTiClient.getInstance().CoupleArenaQuit(client.ClientData.RoleID, spouseClient.ClientData.RoleID);
                    SetMatchState(client.ClientData.RoleID, ECoupleArenaMatchState.OnLine);
                    NtfCoupleMatchState(client.ClientData.RoleID);
                    client.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_QUIT, StdErrorCode.Error_Success.ToString());
                }
            }
        }
        #endregion

        #region Util Function
        /// <summary>
        /// 情侣竞技，祝福buff对真爱buff的伤害加成
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="objTarget"></param>
        /// <returns></returns>
        public double CalcBuffHurt(IObject obj, IObject objTarget)
        {
            try
            {
                if (obj == null || objTarget == null) return 0.0;

                GameClient fromClient = obj as GameClient;
                GameClient toClient = objTarget as GameClient;
                if (fromClient == null || toClient == null) return 0.0;

                BufferData yongqiData = Global.GetBufferDataByID(fromClient, (int)BufferItemTypes.CoupleArena_YongQi_Buff);
                BufferData zhenaiData = Global.GetBufferDataByID(toClient, (int)BufferItemTypes.CoupleArena_ZhenAi_Buff);
                if (yongqiData != null && !Global.IsBufferDataOver(yongqiData)
                    && zhenaiData != null && !Global.IsBufferDataOver(zhenaiData))
                    return YongQiBuff2ZhenAiBuffHurt;

                return 0.0;
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// 设置烽火佳人夫妻
        /// </summary>
        /// <param name="roleid1"></param>
        /// <param name="roleid2"></param>
        public void SetFengHuoJiaRenCouple(int roleid1, int roleid2)
        {
            int min = Math.Min(roleid1, roleid2), max = Math.Max(roleid1, roleid2);

            string[] roles = GameManager.GameConfigMgr.GetGameConfigItemStr(GameConfigNames.CoupleArenaFengHuo, "0,0").Split(',');
            if (roles == null || roles.Length != 2 || Convert.ToInt32(roles[0]) != min || Convert.ToInt32(roles[1]) != max)
            {
                int oldRole1 = Convert.ToInt32(roles[0]);
                int oldRole2 = Convert.ToInt32(roles[1]);
                 // 记录标记
                Global.UpdateDBGameConfigg(GameConfigNames.CoupleArenaFengHuo, string.Format("{0},{1}", min, max));
                GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.CoupleArenaFengHuo, string.Format("{0},{1}", min, max));

                CheckFengHuoJiaRenChengHao(GameManager.ClientMgr.FindClient(oldRole1));
                CheckFengHuoJiaRenChengHao(GameManager.ClientMgr.FindClient(oldRole2));
                CheckFengHuoJiaRenChengHao(GameManager.ClientMgr.FindClient(roleid1));
                CheckFengHuoJiaRenChengHao(GameManager.ClientMgr.FindClient(roleid2));
            }             
        }

        /// <summary>
        /// 检查烽火佳人称号
        /// </summary>
        /// <param name="client"></param>
        public void CheckFengHuoJiaRenChengHao(GameClient client)
        {
            if (client == null) return;
            lock (Mutex)
            {
                DateTime now = TimeUtil.NowDateTime();

                string[] roles = GameManager.GameConfigMgr.GetGameConfigItemStr(GameConfigNames.CoupleArenaFengHuo, "0,0").Split(',');
                bool isMe = false;
                // 先检查是否有我
                for (int i = 0; roles != null && i < roles.Length && !isMe; i++)
                    isMe = Convert.ToInt32(roles[i]) == client.ClientData.RoleID;
                // 离婚的话，就没有了
                isMe &= MarryLogic.IsMarried(client.ClientData.RoleID);
                // 必须在烽火佳人称号的允许时间内
                isMe &= IsInFengHuoJiaRenChengHaoTime(now);

                if (isMe)
                {
                    CoupleAreanWarCfg.TimePoint weekFirst = WarCfg.TimePoints.First();

                    int todayWeek = TimeUtil.GetWeekDay1To7(now);
                    DateTime endTime = now.AddTicks(-now.TimeOfDay.Ticks);
                    endTime = endTime.AddDays(-TimeUtil.GetWeekDay1To7(endTime));
                    endTime = endTime.AddDays(weekFirst.Weekday);
                    endTime = endTime.AddTicks(weekFirst.DayStartTicks);
                    if (todayWeek > weekFirst.Weekday || (todayWeek == weekFirst.Weekday && now.TimeOfDay.Ticks > weekFirst.DayStartTicks))
                    {
                        endTime = endTime.AddDays(7);
                    }

                    // 没有的话，就给1个
                    FashionManager.getInstance().GetFashionByMagic(client, FashionIdConsts.CoupleArenaFengHuoJiaRen,
                        endTime.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    // 有的话，就删掉
                    FashionManager.getInstance().DelFashionByMagic(client, FashionIdConsts.CoupleArenaFengHuoJiaRen);
                }
            }
        }

        /// <summary>
        /// 检查图标
        /// </summary>
        /// <param name="client"></param>
        public void CheckTipsIconState(GameClient client)
        {
            if (client == null) return;
            bool bCanGetAward = false;
            lock (Mutex)
            {
                int willAwardWeek = 0;
                if (IsInWeekAwardTime(TimeUtil.NowDateTime(), out willAwardWeek))
                {
                    CoupleArenaCoupleJingJiData coupleData = GetCachedCoupleData(client.ClientData.RoleID);
                    if (coupleData != null)
                    {
                        string szWeekAward = Global.GetRoleParamByName(client, RoleParamName.CoupleArenaWeekAward);
                        string[] fields = szWeekAward == null ? null : szWeekAward.Split(',');
                        if (fields == null || fields.Length != 2 || Convert.ToInt32(fields[0]) != willAwardWeek)
                        {
                            bCanGetAward = true;
                        }
                    }
                }
            }

            if (client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.CoupleArenaCanAward, bCanGetAward))
            {
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        /// <summary>
        /// 功能是否开启
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private bool IsGongNengOpened(GameClient client)
        {
            if (client == null) return false;
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.Marriage)) return false;
            if (!MarryLogic.IsMarried(client.ClientData.RoleID)) return false;
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.CoupleArena)) return false;
            return true;
        }

        /// <summary>
        /// 是否在烽火佳人时间内
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        private bool IsInFengHuoJiaRenChengHaoTime(DateTime now)
        {
            int week;
            if (IsInWeekAwardTime(now, out week))
                return true;

            return false;
        }

        /// <summary>
        /// 是否处于1次活动时间内
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private bool IsInWeekOnceActTimes(DateTime time)
        {
            int wd = TimeUtil.GetWeekDay1To7(time);
            foreach (var tp in WarCfg.TimePoints)
            {
                if (tp.Weekday == wd
                    && time.TimeOfDay.Ticks >= tp.DayStartTicks
                    && time.TimeOfDay.Ticks <= tp.DayEndTicks)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 是否在可以离婚的时间内
        /// 每次活动的前后5分钟时间内都不能离婚
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool IsNowCanDivorce(DateTime time)
        {
            int wd = TimeUtil.GetWeekDay1To7(time);
            foreach (var tp in WarCfg.TimePoints)
            {
                if (tp.Weekday == wd
                    && time.TimeOfDay.Ticks >= tp.DayStartTicks - 5 * TimeSpan.TicksPerMinute
                    && time.TimeOfDay.Ticks <= tp.DayEndTicks + 5 * TimeSpan.TicksPerMinute)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 是否在领奖时间
        /// </summary>
        /// <param name="time"></param>
        /// <param name="week"></param>
        /// <returns></returns>
        private bool IsInWeekAwardTime(DateTime time, out int week)
        {
            week = 0;
            CoupleAreanWarCfg.TimePoint weekFirst = WarCfg.TimePoints.First();
            CoupleAreanWarCfg.TimePoint weekLast = WarCfg.TimePoints.Last();

            int todayWeek = TimeUtil.GetWeekDay1To7(time);

            // 本周活动首次开始前，领取上周奖励
            if (todayWeek < weekFirst.Weekday || (todayWeek == weekFirst.Weekday && time.TimeOfDay.Ticks < weekFirst.DayStartTicks))
            {
                week = TimeUtil.MakeFirstWeekday(time.AddDays(-7));
                return true;
            }
            // 处于本周首次活动开始和最后一次活动的时间内，不可领取奖励
            else if (todayWeek < weekLast.Weekday || (todayWeek == weekLast.Weekday && time.TimeOfDay.Ticks < weekLast.DayEndTicks + (10 * TimeSpan.TicksPerMinute)))
            {
                week = 0;
                return false;
            }
            // 处于本周最后一次活动之后，可以领取本周奖励
            else
            {
                week = TimeUtil.MakeFirstWeekday(time);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 查看当前应该是第几周的排行榜
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private int CurrRankWeek(DateTime time)
        {
            int currWeekDay = TimeUtil.GetWeekDay1To7(time);
            // 本次首次活动尚未开始，查看上周排行榜
            var first = this.WarCfg.TimePoints.First();
            if (currWeekDay < first.Weekday
                || (currWeekDay == first.Weekday && time.TimeOfDay.Ticks < first.DayStartTicks))
            {
                return TimeUtil.MakeFirstWeekday(time.AddDays(-7));
            }
            else
            {
                return TimeUtil.MakeFirstWeekday(time);
            }
        }

        /// <summary>
        /// 是否关注状态变更
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        private bool IsStateWatcher(int roleId)
        {
            lock (Mutex)
            {
                return coupleStateWatchers.Contains(roleId);
            }
        }

        /// <summary>
        /// 注册、取消状态关注
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="bWatch"></param>
        private void RegStateWatcher(int roleId, bool bWatch)
        {
            lock (Mutex)
            {
                if (bWatch && !coupleStateWatchers.Contains(roleId))
                    coupleStateWatchers.Add(roleId);

                if (!bWatch)
                    coupleStateWatchers.Remove(roleId);
            }
        }

        /// <summary>
        /// 获取匹配状态
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        private ECoupleArenaMatchState GetMatchState(int roleId)
        {
            lock (Mutex)
            {
                ECoupleArenaMatchState state;
                if (!RoleMatchStateDict.TryGetValue(roleId, out state))
                    state = ECoupleArenaMatchState.Offline;
                return state;
            }
        }

        /// <summary>
        /// 设置匹配状态
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="state"></param>
        private void SetMatchState(int roleId, ECoupleArenaMatchState state)
        {
            lock (Mutex)
            {
                if (state == ECoupleArenaMatchState.Offline)
                    RoleMatchStateDict.Remove(roleId);
                else
                    RoleMatchStateDict[roleId] = state;

                if (state == ECoupleArenaMatchState.Ready)
                    RoleStartReadyMs[roleId] = TimeUtil.NOW();
                else
                    RoleStartReadyMs.Remove(roleId);
            }
        }

        /// <summary>
        /// 通知roleid配偶双方的匹配状态
        /// </summary>
        /// <param name="roleId"></param>
        private void NtfCoupleMatchState(int roleId)
        {
            if (!IsStateWatcher(roleId)) return;
            GameClient client = GameManager.ClientMgr.FindClient(roleId);
            if (client == null) return;

            CoupleArenaRoleStateData myState = new CoupleArenaRoleStateData() { RoleId = client.ClientData.RoleID };
            CoupleArenaRoleStateData spouseState = null;
            if (MarryLogic.IsMarried(client.ClientData.RoleID))
            {
                spouseState = new CoupleArenaRoleStateData() { RoleId = client.ClientData.MyMarriageData.nSpouseID };
            }

            lock (Mutex)
            {
                myState.MatchState = (int)GetMatchState(myState.RoleId);
                if (spouseState != null)
                    spouseState.MatchState = (int)GetMatchState(spouseState.RoleId);
            }

            List<CoupleArenaRoleStateData> stateList = new List<CoupleArenaRoleStateData>();
            stateList.Add(myState);
            if (spouseState != null)
                stateList.Add(spouseState);
            client.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_NTF_COUPLE_STATE, stateList);
        }

        /// <summary>
        /// 获取角色的情侣竞技数据，从中心查询得到的缓存
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        private CoupleArenaCoupleJingJiData GetCachedCoupleData(int roleId)
        {
            lock (Mutex)
            {
                CoupleArenaCoupleJingJiData coupleData = null;
                if (!SyncRoleDict.TryGetValue(roleId, out coupleData))
                {
                    coupleData = null;
                }

                return coupleData;
            }
        }

        private CoupleArenaCoupleJingJiData ConvertToJiJiData(CoupleArenaCoupleDataK kData)
        {
            CoupleArenaCoupleJingJiData jingJiData = new CoupleArenaCoupleJingJiData();
            jingJiData.ManRoleId = kData.ManRoleId;
            jingJiData.ManZoneId = kData.ManZoneId;
            jingJiData.ManSelector = kData.ManSelectorData != null ? DataHelper.BytesToObject<RoleData4Selector>(kData.ManSelectorData, 0, kData.ManSelectorData.Length) : null;
            jingJiData.WifeRoleId = kData.WifeRoleId;
            jingJiData.WifeZoneId = kData.WifeZoneId;
            jingJiData.WifeSelector = kData.WifeSelectorData != null ? DataHelper.BytesToObject<RoleData4Selector>(kData.WifeSelectorData, 0, kData.WifeSelectorData.Length) : null;
            jingJiData.TotalFightTimes = kData.TotalFightTimes;
            jingJiData.WinFightTimes = kData.WinFightTimes;
            jingJiData.LianShengTimes = kData.LianShengTimes;
            jingJiData.DuanWeiType = kData.DuanWeiType;
            jingJiData.DuanWeiLevel = kData.DuanWeiLevel;
            jingJiData.JiFen = kData.JiFen;
            jingJiData.Rank = kData.Rank;
            jingJiData.IsDivorced = kData.IsDivorced;

            return jingJiData;
        }
        #endregion

        #region 定时器
        /// <summary>
        /// 从中心同步数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerProc(object sender, EventArgs e)
        {
            CoupleArenaSyncData data = TianTiClient.getInstance().CoupleArenaSync(SyncDateTime);
            if (data == null) return;

            lock (Mutex)
            {
                this.SyncRankList.Clear();
                this.SyncRoleDict.Clear();
                if (data.RankList != null)
                {
                    this.SyncRankList.AddRange(data.RankList.Select(_r => ConvertToJiJiData(_r)));
                    foreach (var r in this.SyncRankList)
                    {
                        this.SyncRoleDict[r.ManRoleId] = r;
                        this.SyncRoleDict[r.WifeRoleId] = r;
                    }
                }

                if (this.SyncRankList.Count > 0 && this.SyncRankList[0].Rank == 1 && this.SyncRankList[0].IsDivorced == 0)
                {
                    this.SetFengHuoJiaRenCouple(this.SyncRankList[0].ManRoleId, this.SyncRankList[0].WifeRoleId);
                }
                else
                {
                    this.SetFengHuoJiaRenCouple(0, 0);
                }

                this.SyncDateTime = data.ModifyTime;
            }
        }
        #endregion
    }
}
