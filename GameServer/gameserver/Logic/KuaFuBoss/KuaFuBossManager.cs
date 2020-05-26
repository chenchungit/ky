using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using System.Xml.Linq;
using GameServer.Core.GameEvent.EventOjectImpl;
using Tmsk.Contract;
using KF.Client;
using KF.Contract.Data;
using Tmsk.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 跨服天梯管理
    /// </summary>
    public partial class KuaFuBossManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 标准接口

        public const SceneUIClasses ManagerType = SceneUIClasses.KuaFuBoss;

        private static KuaFuBossManager instance = new KuaFuBossManager();

        public static KuaFuBossManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public KuaFuBossData RuntimeData = new KuaFuBossData();

        public bool initialize()
        {
            if (!InitConfig())
            {
                return false;
            }

            return true;
        }

        public bool initialize(ICoreInterface coreInterface)
        {
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("KuaFuBossManager.TimerProc", TimerProc), 15000, 5000);
            return true;
        }

        public bool startup()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KUAFU_BOSS_JOIN, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KUAFU_BOSS_ENTER, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KUAFU_BOSS_STATE, 1, 1, getInstance());

            //向事件源注册监听器
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.KuaFuBoss, getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, getInstance());

            return true;
        }

        public bool showdown()
        {
            //向事件源删除监听器
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.KuaFuBoss, getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, getInstance());

            return true;
        }

        public bool destroy()
        {
            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_KUAFU_BOSS_JOIN:
                    return ProcessKuaFuBossJoinCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KUAFU_BOSS_ENTER:
                    return ProcessKuaFuBossEnterCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KUAFU_BOSS_STATE:
                    return ProcessGetKuaFuBossStateCmd(client, nID, bytes, cmdParams);
            }

            return true;
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventObject"></param>
        public void processEvent(EventObject eventObject)
        {
            int eventType = eventObject.getEventType();
            if (eventType == (int)EventTypes.MonsterDead)
            {
                MonsterDeadEventObject monsterDeadEvent = eventObject as MonsterDeadEventObject;
                if (null != monsterDeadEvent)
                {
                    OnKillMonster(monsterDeadEvent.getAttacker(), monsterDeadEvent.getMonster());
                }
            }
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventObject"></param>
        public void processEvent(EventObjectEx eventObject)
        {
            int eventType = eventObject.EventType;
            switch (eventType)
            {
                case (int)GlobalEventTypes.KuaFuNotifyEnterGame:
                    {
                        KuaFuNotifyEnterGameEvent e = eventObject as KuaFuNotifyEnterGameEvent;
                        if (null != e)
                        {
                            KuaFuServerLoginData kuaFuServerLoginData = e.Arg as KuaFuServerLoginData;
                            if (null != kuaFuServerLoginData)
                            {
                                lock (RuntimeData.Mutex)
                                {
                                    RuntimeData.RoleIdKuaFuLoginDataDict[kuaFuServerLoginData.RoleId] = kuaFuServerLoginData;
                                    LogManager.WriteLog(LogTypes.Error, string.Format("通知角色ID={0}拥有进入跨服Boss资格,跨服GameID={1}", kuaFuServerLoginData.RoleId, kuaFuServerLoginData.GameId));
                                }
                            }

                            eventObject.Handled = true;
                        }
                    }
                    break;
            }
        }

        #endregion 标准接口

        #region 初始化配置

        /// <summary>
        /// 初始化配置
        /// </summary>
        public bool InitConfig()
        {
            bool success = true;
            XElement xml = null;
            string fileName = "";
            string fullPathFileName = "";
            IEnumerable<XElement> nodes;

            lock (RuntimeData.Mutex)
            {
                try
                {
                    //出生点配置
                    RuntimeData.MapBirthPointDict.Clear();

                    fileName = "Config/ThroughServiceBossRebirth.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        KuaFuBossBirthPoint item = new KuaFuBossBirthPoint();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "PosX");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "PosY");
                        item.BirthRadius = (int)Global.GetSafeAttributeLong(node, "BirthRadius");

                        RuntimeData.MapBirthPointDict[item.ID] = item;
                    }

                    //活动配置
                    RuntimeData.SceneDataDict.Clear();
                    RuntimeData.LevelRangeSceneIdDict.Clear();
                    RuntimeData.SceneDynMonsterDict.Clear();

                    fileName = "Config/ThroughServiceBoss.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        KuaFuBossSceneInfo sceneItem = new KuaFuBossSceneInfo();
                        int id = (int)Global.GetSafeAttributeLong(node, "MapCode");
                        int mapCode = (int)Global.GetSafeAttributeLong(node, "MapCode");

                        sceneItem.Id = id;
                        sceneItem.MapCode = mapCode;
                        sceneItem.MinLevel = (int)Global.GetSafeAttributeLong(node, "MinLevel");
                        sceneItem.MaxLevel = (int)Global.GetSafeAttributeLong(node, "MaxLevel");
                        sceneItem.MinZhuanSheng = (int)Global.GetSafeAttributeLong(node, "MinZhuanSheng");
                        sceneItem.MaxZhuanSheng = (int)Global.GetSafeAttributeLong(node, "MaxZhuanSheng");
                        sceneItem.PrepareSecs = (int)Global.GetSafeAttributeLong(node, "PrepareSecs");
                        sceneItem.WaitingEnterSecs = (int)Global.GetSafeAttributeLong(node, "WaitingEnterSecs");
                        sceneItem.FightingSecs = (int)Global.GetSafeAttributeLong(node, "FightingSecs");
                        sceneItem.ClearRolesSecs = (int)Global.GetSafeAttributeLong(node, "ClearRolesSecs");

                        ConfigParser.ParseStrInt2(Global.GetSafeAttributeStr(node, "ApplyTime"), ref sceneItem.SignUpStartSecs, ref sceneItem.SignUpEndSecs);
                        sceneItem.SignUpStartSecs += sceneItem.SignUpEndSecs;

                        if (!ConfigParser.ParserTimeRangeListWithDay(sceneItem.TimePoints, Global.GetSafeAttributeStr(node, "TimePoints")))
                        {
                            success = false;
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("读取{0}时间配置(TimePoints)出错", fileName));
                        }

                        for (int i = 0; i < sceneItem.TimePoints.Count; ++i)
                        {
                            TimeSpan ts = new TimeSpan(sceneItem.TimePoints[i].Hours, sceneItem.TimePoints[i].Minutes, sceneItem.TimePoints[i].Seconds);
                            sceneItem.SecondsOfDay.Add(ts.TotalSeconds);
                        }

                        GameMap gameMap = null;
                        if (!GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out gameMap))
                        {
                            success = false;
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("22地图配置中缺少{0}所需的地图:{1}", fileName, mapCode));
                        }

                        RangeKey range = new RangeKey(Global.GetUnionLevel(sceneItem.MinZhuanSheng, sceneItem.MinLevel), Global.GetUnionLevel(sceneItem.MaxZhuanSheng, sceneItem.MaxLevel));
                        RuntimeData.LevelRangeSceneIdDict[range] = sceneItem;
                        RuntimeData.SceneDataDict[id] = sceneItem;
                    }

                    fileName = "Config/ThroughServiceBossMonster.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        BattleDynamicMonsterItem item = new BattleDynamicMonsterItem();
                        item.Id = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.MapCode = (int)Global.GetSafeAttributeLong(node, "CodeID");
                        item.MonsterID = (int)Global.GetSafeAttributeLong(node, "MonsterID");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "X");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "Y");
                        item.DelayBirthMs = (int)Global.GetSafeAttributeLong(node, "Time");
                        item.PursuitRadius = (int)Global.GetSafeAttributeLong(node, "PursuitRadius");
                        item.Num = (int)Global.GetSafeAttributeLong(node, "Num");
                        item.Radius = (int)Global.GetSafeAttributeLong(node, "Radius");

                        List<BattleDynamicMonsterItem> itemList = null;
                        if (!RuntimeData.SceneDynMonsterDict.TryGetValue(item.MapCode, out itemList))
                        {
                            itemList = new List<BattleDynamicMonsterItem>();
                            RuntimeData.SceneDynMonsterDict[item.MapCode] = itemList;
                        }

                        itemList.Add(item);
                    }
                }
                catch (System.Exception ex)
                {
                    success = false;
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("加载xml配置文件:{0}, 失败。", fileName), ex);
                }
            }

            return success ;
        }

        private void TimerProc(object sender, EventArgs e)
        {
            bool notifyPrepareGame = false;
            bool notifyEnterGame = false;
            DateTime now = TimeUtil.NowDateTime();
            lock(RuntimeData.Mutex)
            {
                bool bInActiveTime = false;

                KuaFuBossSceneInfo sceneItem = RuntimeData.SceneDataDict.Values.FirstOrDefault();
                for (int i = 0; i < sceneItem.TimePoints.Count - 1; i += 2)
                {
                    if ((int)now.DayOfWeek == sceneItem.TimePoints[i].Days
                        && now.TimeOfDay.TotalSeconds >= sceneItem.SecondsOfDay[i] - sceneItem.SignUpStartSecs
                        && now.TimeOfDay.TotalSeconds <= sceneItem.SecondsOfDay[i + 1])
                    {
                        double secs = sceneItem.SecondsOfDay[i] - now.TimeOfDay.TotalSeconds;
                        bInActiveTime = true;
                        if (!RuntimeData.PrepareGame)
                        {
                            if (secs > 0 && secs < sceneItem.SignUpEndSecs / 2)
                            {
                                LogManager.WriteLog(LogTypes.Error, "报名截止5分钟时间过半,通知跨服中心开始分配所有报名玩家的活动场次");

                                // 通知跨服中心开始准备副本
                                RuntimeData.PrepareGame = true;
                                notifyPrepareGame = true;
                                break;
                            }
                        }
                        else
                        {
                            if (secs < 0)
                            {
                                LogManager.WriteLog(LogTypes.Error, "报名截止状态结束,可以通知已分配到场次的玩家进入游戏了");

                                // 首次到达进入时间，通知进入，并重置PrepareGame状态，然后以后的循环走上面的if
                                // 但是上面的if在本次活动期间就相当于空转
                                notifyEnterGame = true;
                                RuntimeData.PrepareGame = false;
                                break;
                            }
                        }
                    }
                }

                if (!bInActiveTime)
                {
                    if (RuntimeData.RoleIdKuaFuLoginDataDict.Count > 0)
                    {
                        RuntimeData.RoleIdKuaFuLoginDataDict.Clear();
                    }

                    if (RuntimeData.RoleId2JoinGroup.Count > 0)
                    {
                        RuntimeData.RoleId2JoinGroup.Clear();
                    }
                }
            }
            
            if (notifyPrepareGame)
            {
                LogManager.WriteLog(LogTypes.Error, "通知跨服中心开始分配所有报名玩家的活动场次");

                // GameServer和KF-GameServer都会通知准备游戏，所以中心要防止状态回滚
                string cmd = string.Format("{0} {1} {2}", GameStates.CommandName, GameStates.PrepareGame, (int)GameTypes.KuaFuBoss);
                YongZheZhanChangClient.getInstance().ExecuteCommand(cmd);
            }

            if (notifyEnterGame)
            {
                lock (RuntimeData.Mutex)
                {
                    foreach (var kuaFuServerLoginData in RuntimeData.RoleIdKuaFuLoginDataDict.Values)
                    {
                        RuntimeData.NotifyRoleEnterDict.Add(kuaFuServerLoginData.RoleId, kuaFuServerLoginData);
                    }
                }
            }

            //通知报名的玩家进入活动,每次只通知一部分(按RoleID除以15的余数),防止所有玩家一起进入给服务器造成压力.
            List<KuaFuServerLoginData> list = null;
            lock (RuntimeData.Mutex)
            {
                int count = RuntimeData.NotifyRoleEnterDict.Count;
                if (count > 0)
                {
                    list = new List<KuaFuServerLoginData>();
                    KuaFuServerLoginData kuaFuServerLoginData = RuntimeData.NotifyRoleEnterDict.First().Value;
                    foreach (var kv in RuntimeData.NotifyRoleEnterDict)
                    {
                        if ((kv.Key % 15) == (kuaFuServerLoginData.RoleId % 15))
                        {
                            list.Add(kv.Value);
                        }
                    }

                    foreach (var data in list)
                    {
                        RuntimeData.NotifyRoleEnterDict.Remove(data.RoleId);
                    }
                }
            }

            if (null != list)
            {
                foreach (var kuaFuServerLoginData in list)
                {
                    GameClient client = GameManager.ClientMgr.FindClient(kuaFuServerLoginData.RoleId);
                    if (null != client)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KUAFU_BOSS_ENTER, 1);
                    }
                }
            }
        }

        #endregion 初始化配置

        #region 指令处理

        /// <summary>
        /// 罗兰城战攻防竞价申请指令处理
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessKuaFuBossJoinCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success_No_Info;

                do 
                {
                    KuaFuBossSceneInfo sceneItem = null;
                    KuaFuBossGameStates state = KuaFuBossGameStates.None;
                    if (!CheckMap(client))
                    {
                        result = StdErrorCode.Error_Denied_In_Current_Map;
                    }
                    else
                    {
                        result = CheckCondition(client, ref sceneItem, ref state);
                    }
                    
                    if (state != KuaFuBossGameStates.SignUp)
                    {
                        result = StdErrorCode.Error_Not_In_valid_Time; //非报名时间
                    }
                    else if (RuntimeData.RoleId2JoinGroup.ContainsKey(client.ClientData.RoleID))
                    {
                        result = StdErrorCode.Error_Operation_Denied; // 已经报名了
                    }

                    if (result >= 0)
                    {
                        int gropuId = sceneItem.Id;
                        result = YongZheZhanChangClient.getInstance().YongZheZhanChangSignUp(client.strUserID, client.ClientData.RoleID, client.ClientData.ZoneID,
                            (int)GameTypes.KuaFuBoss, gropuId, client.ClientData.CombatForce);
                        if (result > 0)
                        {
                            RuntimeData.RoleId2JoinGroup[client.ClientData.RoleID] = gropuId;
                            client.ClientData.SignUpGameType = (int)GameTypes.KuaFuBoss;
                        }
                    }
                } while (false);

                //发送结果给客户端
                client.sendCmd(nID, result);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        // 检查该地图是否允许操作
        private bool CheckMap(GameClient client)
        {
            SceneUIClasses sceneType = Global.GetMapSceneType(client.ClientData.MapCode);
            if (sceneType != SceneUIClasses.Normal)
            {
                return false;
            }

            return true;
        }

        // 检查勇者战场当前处于什么时间状态
        private int CheckCondition(GameClient client, ref KuaFuBossSceneInfo sceneItem, ref KuaFuBossGameStates state)
        {
            int result = 0;
            sceneItem = null;

            do 
            {
                if (!IsGongNengOpened(client, true))
                {
                    result = StdErrorCode.Error_Type_Not_Match;
                    break;
                }

                lock (RuntimeData.Mutex)
                {
                    if (!RuntimeData.LevelRangeSceneIdDict.TryGetValue(new RangeKey(Global.GetUnionLevel(client)), out sceneItem))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }
                }

                result = StdErrorCode.Error_Not_In_valid_Time;
                DateTime now = TimeUtil.NowDateTime();
                lock (RuntimeData.Mutex)
                {
                    for (int i = 0; i < sceneItem.TimePoints.Count - 1; i += 2)
                    {
                        if ((int)now.DayOfWeek == sceneItem.TimePoints[i].Days
                            && now.TimeOfDay.TotalSeconds >= sceneItem.SecondsOfDay[i] - sceneItem.SignUpStartSecs
                            && now.TimeOfDay.TotalSeconds <= sceneItem.SecondsOfDay[i + 1])
                        {
                            if (now.TimeOfDay.TotalSeconds < sceneItem.SecondsOfDay[i] - sceneItem.SignUpStartSecs)
                            {
                                state = KuaFuBossGameStates.None;
                                result = StdErrorCode.Error_Not_In_valid_Time;
                            }
                            else if (now.TimeOfDay.TotalSeconds < sceneItem.SecondsOfDay[i] - sceneItem.SignUpEndSecs)
                            {
                                state = KuaFuBossGameStates.SignUp;
                                result = StdErrorCode.Error_Success;
                            }
                            else if (now.TimeOfDay.TotalSeconds < sceneItem.SecondsOfDay[i])
                            {
                                state = KuaFuBossGameStates.Wait;
                                result = StdErrorCode.Error_Success;
                            }
                            else if (now.TimeOfDay.TotalSeconds < sceneItem.SecondsOfDay[i + 1])
                            {
                                state = KuaFuBossGameStates.Start;
                                result = StdErrorCode.Error_Success;
                            }
                            else
                            {
                                state = KuaFuBossGameStates.None;
                                result = StdErrorCode.Error_Not_In_valid_Time;
                            }
                            break;
                        }
                    }
                }
            } while (false);

            return result;
        }

        private TimeSpan GetStartTime(int sceneId)
        {
            int result = 0;
            KuaFuBossSceneInfo sceneItem = null;
            TimeSpan startTime = TimeSpan.MinValue;
            DateTime now = TimeUtil.NowDateTime();

            do
            {
                lock (RuntimeData.Mutex)
                {
                    if (!RuntimeData.SceneDataDict.TryGetValue(sceneId, out sceneItem))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }
                }

                result = StdErrorCode.Error_Not_In_valid_Time;
                lock (RuntimeData.Mutex)
                {
                    for (int i = 0; i < sceneItem.TimePoints.Count - 1; i += 2)
                    {
                        if ((int)now.DayOfWeek == sceneItem.TimePoints[i].Days
                            && now.TimeOfDay.TotalSeconds >= sceneItem.SecondsOfDay[i] - sceneItem.SignUpStartSecs
                            && now.TimeOfDay.TotalSeconds <= sceneItem.SecondsOfDay[i + 1])
                        {
                            startTime = TimeSpan.FromSeconds(sceneItem.SecondsOfDay[i]);
                            break;
                        }
                    }
                }
            } while (false);

            if (startTime < TimeSpan.Zero)
            {
                startTime = now.TimeOfDay;
            }

            return startTime;
        }

        public bool ProcessGetKuaFuBossStateCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                KuaFuBossSceneInfo sceneItem = null;
                KuaFuBossGameStates timeState = KuaFuBossGameStates.None;
                int result = (int)KuaFuBossGameStates.None;
                int groupId = 0;
                RuntimeData.RoleId2JoinGroup.TryGetValue(client.ClientData.RoleID, out groupId);

                CheckCondition(client, ref sceneItem, ref timeState);
                if (groupId > 0)
                {
                    if (timeState >= KuaFuBossGameStates.SignUp && timeState <= KuaFuBossGameStates.Wait)
                    {
                        int state = YongZheZhanChangClient.getInstance().GetKuaFuRoleState(client.ClientData.RoleID);
                        if (state >= (int)KuaFuRoleStates.SignUp)
                        {
                            result = (int)KuaFuBossGameStates.Wait;
                        }
                        else
                        {
                            result = (int)KuaFuBossGameStates.NotJoin;
                        }
                    }
                    else if (timeState == KuaFuBossGameStates.Start)
                    {
                        if (RuntimeData.RoleIdKuaFuLoginDataDict.ContainsKey(client.ClientData.RoleID))
                        {
                            result = (int)KuaFuBossGameStates.Start;
                        }
                    }
                }
                else
                {
                    if (timeState == KuaFuBossGameStates.SignUp)
                    {
                        result = (int)KuaFuBossGameStates.SignUp;
                    }
                    else if (timeState == KuaFuBossGameStates.Wait || timeState == KuaFuBossGameStates.Start)
                    {
                        // 未参加本次活动
                        result = (int)KuaFuBossGameStates.NotJoin;
                    }
                }

                client.sendCmd(nID, result);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessKuaFuBossEnterCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                KuaFuBossSceneInfo sceneItem = null;
                KuaFuBossGameStates state = KuaFuBossGameStates.None;
                int result = StdErrorCode.Error_Success_No_Info;

                if (!CheckMap(client))
                {
                    result = StdErrorCode.Error_Denied_In_Current_Map;
                }
                else
                {
                    result = CheckCondition(client, ref sceneItem, ref state);
                }

                if (state == KuaFuBossGameStates.Start)
                {
                    KuaFuServerLoginData kuaFuServerLoginData = null;
                    lock (RuntimeData.Mutex)
                    {
                        if (RuntimeData.RoleIdKuaFuLoginDataDict.TryGetValue(client.ClientData.RoleID, out kuaFuServerLoginData))
                        {
                            KuaFuServerLoginData clientKuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);
                            if (null != clientKuaFuServerLoginData)
                            {
                                clientKuaFuServerLoginData.RoleId = kuaFuServerLoginData.RoleId;
                                clientKuaFuServerLoginData.GameId = kuaFuServerLoginData.GameId;
                                clientKuaFuServerLoginData.GameType = kuaFuServerLoginData.GameType;
                                clientKuaFuServerLoginData.EndTicks = kuaFuServerLoginData.EndTicks;
                                clientKuaFuServerLoginData.ServerId = kuaFuServerLoginData.ServerId;
                                clientKuaFuServerLoginData.ServerIp = kuaFuServerLoginData.ServerIp;
                                clientKuaFuServerLoginData.ServerPort = kuaFuServerLoginData.ServerPort;
                                clientKuaFuServerLoginData.FuBenSeqId = kuaFuServerLoginData.FuBenSeqId;
                            }
                        }
                        else
                        {
                            result = StdErrorCode.Error_Server_Busy;
                        }
                    }

                    if (result >= 0)
                    {
                        result = YongZheZhanChangClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.EnterGame);
                        if (result >= 0)
                        {
                            GlobalNew.RecordSwitchKuaFuServerLog(client);
                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client));
                        }
                        else
                        {
                            Global.GetClientKuaFuServerLoginData(client).RoleId = 0;
                        }
                    }
                }
                else
                {
                    result = StdErrorCode.Error_Not_In_valid_Time;
                }

                client.sendCmd(nID, result);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        #endregion 指令处理

        #region 其他

        public int GetBirthPoint(GameClient client, out int posX, out int posY)
        {
            int side = 1; //client.ClientData.BattleWhichSide
            lock (RuntimeData.Mutex)
            {
                KuaFuBossBirthPoint birthPoint = null;
                side = Global.GetRandomNumber(1, RuntimeData.MapBirthPointDict.Count);
                if (!RuntimeData.MapBirthPointDict.TryGetValue(side, out birthPoint))
                {
                    birthPoint = RuntimeData.MapBirthPointDict.First().Value;
                }

                posX = birthPoint.PosX;
                posY = birthPoint.PosY;
                return side;
            }
        }

        public bool OnInitGame(GameClient client)
        {
            int posX;
            int posY;
            int side;

            KuaFuServerLoginData kuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);
            YongZheZhanChangFuBenData fuBenData;
            lock (RuntimeData.Mutex)
            {
                if (!RuntimeData.FuBenItemData.TryGetValue((int)kuaFuServerLoginData.GameId, out fuBenData))
                {
                    fuBenData = null;
                }
                else if (fuBenData.State >= GameFuBenState.End)
                {
                    return false;
                }
            }

            if (null == fuBenData)
            {
                //从中心查询副本信息
                YongZheZhanChangFuBenData newFuBenData = YongZheZhanChangClient.getInstance().GetKuaFuFuBenData((int)kuaFuServerLoginData.GameId);
                if (newFuBenData == null || newFuBenData.State == GameFuBenState.End)
                {
                    LogManager.WriteLog(LogTypes.Error, "获取不到有效的副本数据," + newFuBenData == null ? "fuBenData == null" : "fuBenData.State == GameFuBenState.End");
                    return false;
                }

                lock (RuntimeData.Mutex)
                {
                    if (!RuntimeData.FuBenItemData.TryGetValue((int)kuaFuServerLoginData.GameId, out fuBenData))
                    {
                        fuBenData = newFuBenData;
                        fuBenData.SequenceId = GameCoreInterface.getinstance().GetNewFuBenSeqId();
                        RuntimeData.FuBenItemData[fuBenData.GameId] = fuBenData;
                    }
                }
            }

            KuaFuFuBenRoleData kuaFuFuBenRoleData;
            if (!fuBenData.RoleDict.TryGetValue(client.ClientData.RoleID, out kuaFuFuBenRoleData))
            {
                return false;
            }

            GetBirthPoint(client, out posX, out posY);

            KuaFuBossSceneInfo sceneInfo;
            lock (RuntimeData.Mutex)
            {
                kuaFuServerLoginData.FuBenSeqId = fuBenData.SequenceId;
                if (!RuntimeData.SceneDataDict.TryGetValue(fuBenData.GroupIndex, out sceneInfo))
                {
                    return false;
                }

                client.ClientData.MapCode = sceneInfo.MapCode;
            }

            client.ClientData.PosX = posX;
            client.ClientData.PosY = posY;
            client.ClientData.FuBenSeqID = kuaFuServerLoginData.FuBenSeqId;

            return true;
        }

        /// <summary>
        /// 角色复活
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="direction"></param>
        public bool ClientRelive(GameClient client)
        {
            int toPosX, toPosY;
            GetBirthPoint(client, out toPosX, out toPosY);

            client.ClientData.CurrentLifeV = client.ClientData.LifeV;
            client.ClientData.CurrentMagicV = client.ClientData.MagicV;

            client.ClientData.MoveAndActionNum = 0;

            //通知队友自己要复活
            GameManager.ClientMgr.NotifyTeamRealive(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.ClientData.RoleID, toPosX, toPosY, -1);

            Global.ClientRealive(client, toPosX, toPosY, -1);
            return true;
        }

        /// <summary>
        /// 判断功能是否开启
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool IsGongNengOpened(GameClient client, bool hint = false)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return false;

            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.KuaFuBoss))
            {
                return false;
            }

            return GlobalNew.IsGongNengOpened(client, GongNengIDs.KuaFuBoss, hint);
        }

        #endregion 其他
    }
}
