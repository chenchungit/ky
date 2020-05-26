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

namespace GameServer.Logic
{
    /// <summary>
    /// 王城战管理
    /// </summary>
    public partial class HuanYingSiYuanManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 标准接口

        public const SceneUIClasses ManagerType = SceneUIClasses.HuanYingSiYuan;

        private static HuanYingSiYuanManager instance = new HuanYingSiYuanManager();

        public static HuanYingSiYuanManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public HuanYingSiYuanData RuntimeData = new HuanYingSiYuanData();

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
            return true;
        }

        public bool startup()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_HYSY_ENQUEUE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_HYSY_DEQUEUE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_HYSY_ENTER_RESPOND, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_HYSY_QUEUE_PLAYER_NUM, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_HYSY_SUCCESS_COUNT, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_HYSY_SCORE_INFO, 1, 1, getInstance());

            //向事件源注册监听器
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.HuanYingSiYuan, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuRoleCountChange, (int)SceneUIClasses.HuanYingSiYuan, getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.ClientRegionEvent, getInstance());

            //玩家死亡事件监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerDead, getInstance());

            return true;
        }

        public bool showdown()
        {
            //向事件源删除监听器
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.HuanYingSiYuan, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuRoleCountChange, (int)SceneUIClasses.HuanYingSiYuan, getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.ClientRegionEvent, getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerDead, getInstance());

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
                case (int)TCPGameServerCmds.CMD_SPR_HYSY_ENQUEUE:
                    return ProcessHuanYingSiYuanEnqueueCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_HYSY_DEQUEUE:
                    return ProcessHuanYingSiYuanDequeueCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_HYSY_ENTER_RESPOND:
                    return ProcessHuanYingSiYuanEnterRespondCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_HYSY_QUEUE_PLAYER_NUM:
                    return ProcessHuanYingSiYuanQueueRoleCountCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_HYSY_SUCCESS_COUNT:
                    return ProcessHuanYingSiYuanSuccessCountCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_HYSY_SCORE_INFO:
                    return ProcessHuanYingSiYuanScoreInfoCmd(client, nID, bytes, cmdParams);
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
            if (eventType == (int)EventTypes.ClientRegionEvent)
            {
                ClientRegionEventObject e = eventObject as ClientRegionEventObject;
                if (null != e)
                {
                    if (e.EventType == (int)RegionEventTypes.JiaoFu && e.Flag == 1)
                    {
                        SubmitShengBei(e.Client);
                    }
                }
            }
            else if (eventType == (int)EventTypes.PlayerDead)
            {
                PlayerDeadEventObject playerDeadEvent = eventObject as PlayerDeadEventObject;
                if (null != playerDeadEvent)
                {
                    if (playerDeadEvent.Type == PlayerDeadEventTypes.ByRole)
                    {
                        OnKillRole(playerDeadEvent.getAttackerRole(), playerDeadEvent.getPlayer());
                    }
                    else
                    {
                        TryLostShengBei(playerDeadEvent.getPlayer());
                    }
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
                case (int)GlobalEventTypes.KuaFuRoleCountChange:
                    {
                        KuaFuFuBenRoleCountEvent e = eventObject as KuaFuFuBenRoleCountEvent;
                        if (null != e)
                        {
                            GameClient client = GameManager.ClientMgr.FindClient(e.RoleId);
                            if (null != client)
                            {
                                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_HYSY_QUEUE_PLAYER_NUM, e.RoleCount);
                            }

                            eventObject.Handled = true;
                        }
                    }
                    break;
                case (int)GlobalEventTypes.KuaFuNotifyEnterGame:
                    {
                        KuaFuNotifyEnterGameEvent e = eventObject as KuaFuNotifyEnterGameEvent;
                        if (null != e)
                        {
                            KuaFuServerLoginData kuaFuServerLoginData = e.Arg as KuaFuServerLoginData;
                            if (null != kuaFuServerLoginData)
                            {
                                GameClient client = GameManager.ClientMgr.FindClient(kuaFuServerLoginData.RoleId);
                                if (null != client)
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
                                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_HYSY_ENTER_NOTIFY, clientKuaFuServerLoginData);
                                    }
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
                    //圣杯配置
                    RuntimeData.ShengBeiDataDict.Clear();

                    fileName = "Config/HolyGrail.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        ShengBeiData item = new ShengBeiData();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.MonsterID = (int)Global.GetSafeAttributeLong(node, "MonsterID");
                        item.Time = (int)Global.GetSafeAttributeLong(node, "Time");
                        item.GoodsID = (int)Global.GetSafeAttributeLong(node, "GoodsID");
                        item.Score = (int)Global.GetSafeAttributeLong(node, "Score");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "PosX");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "PosY");

                        EquipPropItem propItem = GameManager.EquipPropsMgr.FindEquipPropItem(item.GoodsID);
                        if (null != propItem)
                        {
                            item.BufferProps = propItem.ExtProps;
                        }
                        else
                        {
                            success = false;
                            LogManager.WriteLog(LogTypes.Fatal, "幻影寺院的圣杯Buffer的GoodsID在物品表中找不到");
                        }

                        RuntimeData.ShengBeiDataDict[item.ID] = item;
                    }

                    //出生点配置
                    RuntimeData.MapBirthPointDict.Clear();

                    fileName = "Config/TempleMirageRebirth.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        HuanYingSiYuanBirthPoint item = new HuanYingSiYuanBirthPoint();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "PosX");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "PosY");
                        item.BirthRadius = (int)Global.GetSafeAttributeLong(node, "BirthRadius");

                        RuntimeData.MapBirthPointDict[item.ID] = item;
                    }

                    //连杀配置
                    RuntimeData.ContinuityKillAwardDict.Clear();

                    fileName = "Config/ContinuityKillAward.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        ContinuityKillAward item = new ContinuityKillAward();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.Num = (int)Global.GetSafeAttributeLong(node, "Num");
                        item.Score = (int)Global.GetSafeAttributeLong(node, "Score");

                        RuntimeData.ContinuityKillAwardDict[item.Num] = item;
                    }

                    //活动配置
                    RuntimeData.MapCode = 0;

                    fileName = "Config/TempleMirage.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        RuntimeData.MapCode = (int)Global.GetSafeAttributeLong(node, "MapCode");
                        RuntimeData.MinZhuanSheng = (int)Global.GetSafeAttributeLong(node, "MinZhuanSheng");
                        RuntimeData.MinLevel = (int)Global.GetSafeAttributeLong(node, "MinLevel");
                        RuntimeData.MinRequestNum = (int)Global.GetSafeAttributeLong(node, "MinRequestNum");
                        RuntimeData.MaxEnterNum = (int)Global.GetSafeAttributeLong(node, "MaxEnterNum");

                        RuntimeData.WaitingEnterSecs = (int)Global.GetSafeAttributeLong(node, "WaitingEnterSecs");
                        RuntimeData.PrepareSecs = (int)Global.GetSafeAttributeLong(node, "PrepareSecs");
                        RuntimeData.FightingSecs = (int)Global.GetSafeAttributeLong(node, "FightingSecs");
                        RuntimeData.ClearRolesSecs = (int)Global.GetSafeAttributeLong(node, "ClearRolesSecs");

                        if (!ConfigParser.ParserTimeRangeList(RuntimeData.TimePoints, Global.GetSafeAttributeStr(node, "TimePoints")))
                        {
                            success = false;
                            LogManager.WriteLog(LogTypes.Fatal, "读取幻影寺院时间配置(TimePoints)出错");
                        }

                        GameMap gameMap = null;
                        if (!GameManager.MapMgr.DictMaps.TryGetValue(RuntimeData.MapCode, out gameMap))
                        {
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("缺少幻影寺院地图 {0}", RuntimeData.MapCode));
                        }

                        RuntimeData.MapGridWidth = gameMap.MapGridWidth;
                        RuntimeData.MapGridHeight = gameMap.MapGridHeight;

                        break;
                    }

                    //奖励配置
                    RuntimeData.TempleMirageEXPAward = GameManager.systemParamsList.GetParamValueIntByName("TempleMirageEXPAward");
                    RuntimeData.TempleMirageWin = (int)GameManager.systemParamsList.GetParamValueIntByName("TempleMirageWin");
                    RuntimeData.TempleMiragePK = (int)GameManager.systemParamsList.GetParamValueIntByName("TempleMiragePK");
                    RuntimeData.TempleMirageMinJiFen = (int)GameManager.systemParamsList.GetParamValueIntByName("TempleMirageMinJiFen");

                    if (!ConfigParser.ParseStrInt2(GameManager.systemParamsList.GetParamValueByName("TempleMirageWinNum"), ref RuntimeData.TempleMirageWinExtraNum, ref RuntimeData.TempleMirageWinExtraRate))
                    {
                        success = false;
                        LogManager.WriteLog(LogTypes.Fatal, "读取幻影寺院多倍奖励配置(TempleMirageWin)出错");
                    }

                    if (!ConfigParser.ParseStrInt2(GameManager.systemParamsList.GetParamValueByName("TempleMirageAward"), ref RuntimeData.TempleMirageAwardChengJiu, ref RuntimeData.TempleMirageAwardShengWang))
                    {
                        success = false;
                        LogManager.WriteLog(LogTypes.Fatal, "读取幻影寺院多倍奖励配置(TempleMirageWin)出错");
                    }

                    List<List<int>> levelRanges = ConfigParser.ParserIntArrayList(GameManager.systemParamsList.GetParamValueByName("TempleMirageLevel"));
                    if (levelRanges.Count == 0)
                    {
                        success = false;
                        LogManager.WriteLog(LogTypes.Fatal, "读取幻影寺院等级分组配置(TempleMirageLevel)出错");
                    }
                    else
                    {
                        for (int i = 0; i < levelRanges.Count; i++ )
                        {
                            List<int> range = levelRanges[i];
                            RuntimeData.Range2GroupIndexDict.Add(new RangeKey(Global.GetUnionLevel(range[0], range[1]), Global.GetUnionLevel(range[2], range[3])), i + 1);
                        }
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

        #endregion 初始化配置

        #region GM指令

        public void GMStartHuoDongNow()
        {
            try
            {
                lock (RuntimeData.Mutex)
                {
                    ConfigParser.ParserTimeRangeList(RuntimeData.TimePoints, "00:00-23:59:59");
                }
            }
            catch (System.Exception ex)
            {
            	
            }
        }

        #endregion GM指令

        #region 指令处理

        /// <summary>
        /// 罗兰城战攻防竞价申请指令处理
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessHuanYingSiYuanEnqueueCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                SceneUIClasses sceneType = Global.GetMapSceneType(client.ClientData.MapCode);
                if (sceneType != SceneUIClasses.Normal)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Denied_In_Current_Map);
                    return true;
                }

                if (!IsGongNengOpened(client, true))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Not_In_valid_Time);
                    return true;
                }

                int result = StdErrorCode.Error_Not_In_valid_Time;
                int gropuIndex = 1;
                TimeSpan time = TimeUtil.NowDateTime().TimeOfDay;
                lock(RuntimeData.Mutex)
                {
                    for (int i = 0; i < RuntimeData.TimePoints.Count - 1; i += 2 )
                    {
                        if (time >= RuntimeData.TimePoints[i] && time < RuntimeData.TimePoints[i + 1])
                        {
                            result = StdErrorCode.Error_Success;
                            break;
                        }
                    }

                    if (result >= StdErrorCode.Error_Success)
                    {
                        if (!RuntimeData.Range2GroupIndexDict.TryGetValue(new RangeKey(Global.GetUnionLevel(client)), out gropuIndex))
                        {
                            result = StdErrorCode.Error_Operation_Denied; //Error_Level_Limit
                        }
                    }
                }

                if (result >= 0)
                {
                    result = HuanYingSiYuanClient.getInstance().HuanYingSiYuanSignUp(client.strUserID, client.ClientData.RoleID, client.ClientData.ZoneID,
                        (int)GameTypes.HuanYingSiYuan, gropuIndex, client.ClientData.CombatForce);

                    if (result == (int)KuaFuRoleStates.SignUp)
                    {
                        client.ClientData.SignUpGameType = (int)GameTypes.HuanYingSiYuan;
                        GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 1, 0, 0, 0, (int)GameTypes.HuanYingSiYuan);
                    }
                }

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

        public bool ProcessHuanYingSiYuanQueueRoleCountCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, 0);
                    return true;
                }

                int result = StdErrorCode.Error_Not_In_valid_Time;
                TimeSpan time = TimeUtil.NowDateTime().TimeOfDay;
                lock (RuntimeData.Mutex)
                {
                    for (int i = 0; i < RuntimeData.TimePoints.Count - 1; i += 2)
                    {
                        if (time >= RuntimeData.TimePoints[i] && time < RuntimeData.TimePoints[i + 1])
                        {
                            result = StdErrorCode.Error_Success;
                            break;
                        }
                    }
                }

                if (result >= 0)
                {
                    result = HuanYingSiYuanClient.getInstance().GetRoleKuaFuFuBenRoleCount(client.ClientData.RoleID);
                }

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

        public bool ProcessHuanYingSiYuanSuccessCountCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int count = 0;
                int nowDayId = Global.GetOffsetDayNow();
                int dayid = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysySuccessDayId);
                if (dayid == nowDayId)
                {
                    count = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysySuccessCount);
                }

                //发送结果给客户端
                client.sendCmd(nID, count);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessHuanYingSiYuanScoreInfoCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (client.ClientSocket.IsKuaFuLogin)
                {
                    NotifyTimeStateInfoAndScoreInfo(client);
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return true;
        }

        public bool ProcessHuanYingSiYuanEnterRespondCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Success_No_Info);
                    return true;
                }

                int result = StdErrorCode.Error_Success;
                int gropuIndex;
                int flag = Global.SafeConvertToInt32(cmdParams[1]);
                lock (RuntimeData.Mutex)
                {
                    if (!RuntimeData.Range2GroupIndexDict.TryGetValue(new RangeKey(Global.GetUnionLevel(client)), out gropuIndex))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                    }
                }

                client.ClientData.SignUpGameType = (int)GameTypes.None;
                if (result >= 0)
                {
                    if (flag > 0)
                    {
                        result = HuanYingSiYuanClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.EnterGame);
                        if (result >= 0)
                        {
                            GlobalNew.RecordSwitchKuaFuServerLog(client);
                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client));
                        }
                        else
                        {
                            Global.GetClientKuaFuServerLoginData(client).RoleId = 0;
                            client.sendCmd(nID, result);
                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_HYSY_DEQUEUE, StdErrorCode.Error_Success_No_Info);
                        }
                    }
                    else
                    {
                        HuanYingSiYuanClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.None);
                        Global.GetClientKuaFuServerLoginData(client).RoleId = 0;
                        client.sendCmd(nID, StdErrorCode.Error_Success_No_Info);
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_HYSY_DEQUEUE, StdErrorCode.Error_Success_No_Info);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        /// <summary>
        /// 领取每日奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessHuanYingSiYuanDequeueCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Success_No_Info);
                    return true;
                }

                int result = StdErrorCode.Error_Success;

                int gropuIndex;
                lock (RuntimeData.Mutex)
                {
                    if (!RuntimeData.Range2GroupIndexDict.TryGetValue(new RangeKey(Global.GetUnionLevel(client)), out gropuIndex))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                    }
                }

                client.ClientData.SignUpGameType = (int)GameTypes.None;
                if (result >= 0)
                {
                    result = HuanYingSiYuanClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.None);
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
            int side = client.ClientData.BattleWhichSide;
            if (side <= 0)
            {
                KuaFuServerLoginData clientKuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);
                side = HuanYingSiYuanClient.getInstance().GetRoleBattleWhichSide((int)clientKuaFuServerLoginData.GameId, clientKuaFuServerLoginData.RoleId);
                if (side > 0)
                {
                    client.ClientData.BattleWhichSide = side;
                }
            }

            lock (RuntimeData.Mutex)
            {
                HuanYingSiYuanBirthPoint huanYingSiYuanBirthPoint = null;
                if (RuntimeData.MapBirthPointDict.TryGetValue(side, out huanYingSiYuanBirthPoint))
                {
                    posX = huanYingSiYuanBirthPoint.PosX;
                    posY = huanYingSiYuanBirthPoint.PosY;
                    return side;
                }
            }

            posX = 0;
            posY = 0;
            return -1;
        }

        public void InitRoleDailyHYSYData(GameClient client)
        {
            //是否开启幻影寺院
            if (!IsGongNengOpened(client))
                return;

            int dayid = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysySuccessDayId);
            int ytdid = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysyYTDSuccessDayId);
            int nowcount = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysySuccessCount);

            int currdayid = Global.GetOffsetDayNow();

            // 已经把昨天的记录记录下来了
            if (ytdid + 1 == currdayid)
            {
                return;
            }

            // 当前是昨天的记录
            if (dayid + 1 == currdayid)
            {
                ytdid = dayid;
                int ytdcount = nowcount;

                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HysyYTDSuccessDayId, ytdid, true);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HysyYTDSuccessCount, ytdcount, true);
                return;
            }

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HysyYTDSuccessDayId, currdayid - 1, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HysyYTDSuccessCount, 0, true);

        }

        public int GetLeftCount(GameClient client)
        {
            int dayid = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysySuccessDayId);
            int nowcount = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysySuccessCount);

            int currdayid = Global.GetOffsetDayNow();

            int leftnum = 3;
            int[] nParams = GameManager.systemParamsList.GetParamValueIntArrayByName("TempleMirageWinNum");

            if (null != nParams && nParams.Length == 2)
            {
                leftnum = nParams[0];
            }

            if (dayid == currdayid)
            {
                return Global.GMax(0, leftnum - nowcount);
            }

            return leftnum;
        }

        public bool OnInitGame(GameClient client)
        {
            int posX;
            int posY;

            int side = GetBirthPoint(client, out posX, out posY);
            if (side <= 0)
            {
                return false;
            }

            client.ClientData.MapCode = RuntimeData.MapCode;
            client.ClientData.PosX = posX;
            client.ClientData.PosY = posY;
            client.ClientData.BattleWhichSide = side;

            int fubenSeq = 0;
            lock (Mutex)
            {
                if (!GameId2FuBenSeq.TryGetValue((int)Global.GetClientKuaFuServerLoginData(client).GameId, out fubenSeq))
                {
                    fubenSeq = GameCoreInterface.getinstance().GetNewFuBenSeqId();
                    GameId2FuBenSeq[(int)Global.GetClientKuaFuServerLoginData(client).GameId] = fubenSeq;
                }
            }
            Global.GetClientKuaFuServerLoginData(client).FuBenSeqId = fubenSeq;

            client.ClientData.FuBenSeqID = Global.GetClientKuaFuServerLoginData(client).FuBenSeqId;

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
            if (client.ClientData.MapCode == RuntimeData.MapCode)
            {
                int side = GetBirthPoint(client, out toPosX, out toPosY);
                if (side <= 0)
                {
                    return false;
                }

                client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                client.ClientData.CurrentMagicV = client.ClientData.MagicV;

                client.ClientData.MoveAndActionNum = 0;

                //通知队友自己要复活
                GameManager.ClientMgr.NotifyTeamRealive(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.ClientData.RoleID, toPosX, toPosY, -1);

                Global.ClientRealive(client, toPosX, toPosY, -1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断功能是否开启
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool IsGongNengOpened(GameClient client, bool hint = false)
        {
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.HuanYingSiYuan))
            {
                return false;
            }

            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
            {
                return false;
            }

            return GlobalNew.IsGongNengOpened(client, GongNengIDs.HuanYingSiYuan, hint);
        }

        #endregion 其他
    }
}
