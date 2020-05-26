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
using GameServer.Logic.JingJiChang;

namespace GameServer.Logic
{
    /// <summary>
    /// 跨服天梯管理
    /// </summary>
    public partial class TianTiManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 标准接口

        public const SceneUIClasses ManagerType = SceneUIClasses.TianTi;

        private static TianTiManager instance = new TianTiManager();

        public static TianTiManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public TianTiData RuntimeData = new TianTiData();

        public bool initialize()
        {
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("TianTiManager.TimerProc", TimerProc), 20000, 10000);
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
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TIANTI_JOIN, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TIANTI_QUIT, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TIANTI_ENTER, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TIANTI_DAY_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TIANTI_MONTH_PAIHANG, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TIANTI_GET_PAIMING_AWARDS, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TIANTI_GET_LOG, 1, 1, getInstance());

            //向事件源注册监听器
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.TianTi, getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerDead, getInstance());

            return true;
        }

        public bool showdown()
        {
            //向事件源删除监听器
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.TianTi, getInstance());
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
                case (int)TCPGameServerCmds.CMD_SPR_TIANTI_JOIN:
                    return ProcessTianTiJoinCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TIANTI_QUIT:
                    return ProcessTianTiQuitCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TIANTI_ENTER:
                    return ProcessTianTiEnterCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TIANTI_DAY_DATA:
                    return ProcessGetTianTiDataAndDayPaiHangCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TIANTI_MONTH_PAIHANG:
                    return ProcessGetTianTiMonthPaiHangDataCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TIANTI_GET_PAIMING_AWARDS:
                    return ProcessTianTiGetPaiHangAwardsCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TIANTI_GET_LOG:
                    return ProcessTianTiGeLogCmd(client, nID, bytes, cmdParams);
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
            if (eventType == (int)EventTypes.PlayerDead)
            {
                PlayerDeadEventObject playerDeadEvent = eventObject as PlayerDeadEventObject;
                if (null != playerDeadEvent)
                {
                    if (playerDeadEvent.Type == PlayerDeadEventTypes.ByRole)
                    {
                        OnKillRole(playerDeadEvent.getAttackerRole(), playerDeadEvent.getPlayer());
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
                                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TIANTI_ENTER, kuaFuServerLoginData.GameId);
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
                    //段位配置
                    RuntimeData.TianTiDuanWeiDict.Clear();
                    RuntimeData.DuanWeiJiFenRangeDuanWeiIdDict.Clear();

                    int preJiFen = 0;
                    int perDuanWeiId = 0;
                    int maxDuanWeiId = 0;
                    fileName = "Config/DuanWei.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        TianTiDuanWei item = new TianTiDuanWei();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.NeedDuanWeiJiFen = (int)Global.GetSafeAttributeLong(node, "NeedDuanWeiJiFen");
                        item.WinJiFen = (int)Global.GetSafeAttributeLong(node, "WinJiFen");
                        item.LoseJiFen = (int)Global.GetSafeAttributeLong(node, "LoseJiFen");
                        item.RongYaoNum = (int)Global.GetSafeAttributeLong(node, "RongYaoNum");
                        item.WinRongYu = (int)Global.GetSafeAttributeLong(node, "WinRongYu");
                        item.LoseRongYu = (int)Global.GetSafeAttributeLong(node, "LoseRongYu");

                        if (perDuanWeiId > 0)
                        {
                            RuntimeData.DuanWeiJiFenRangeDuanWeiIdDict[new RangeKey(preJiFen, item.NeedDuanWeiJiFen - 1)] = perDuanWeiId;
                        }

                        preJiFen = item.NeedDuanWeiJiFen;
                        perDuanWeiId = item.ID;
                        maxDuanWeiId = item.ID;
                        RuntimeData.TianTiDuanWeiDict[item.ID] = item;
                    }

                    if (maxDuanWeiId > 0 && preJiFen > 0)
                    {
                        RuntimeData.DuanWeiJiFenRangeDuanWeiIdDict[new RangeKey(preJiFen, int.MaxValue)] = maxDuanWeiId;
                    }

                    //出生点配置
                    RuntimeData.MapBirthPointDict.Clear();

                    fileName = "Config/TianTiBirthPoint.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        TianTiBirthPoint item = new TianTiBirthPoint();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "PosX");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "PosY");
                        item.BirthRadius = (int)Global.GetSafeAttributeLong(node, "BirthRadius");

                        RuntimeData.MapBirthPointDict[item.ID] = item;
                    }

                    //段位排行奖励
                    RuntimeData.DuanWeiRankAwardDict.Clear();

                    fileName = "Config/DuanWeiRankAward.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        DuanWeiRankAward item = new DuanWeiRankAward();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.StarRank = (int)Global.GetSafeAttributeLong(node, "StarRank");
                        item.EndRank = (int)Global.GetSafeAttributeLong(node, "EndRank");
                        ConfigParser.ParseAwardsItemList(Global.GetSafeAttributeStr(node, "Award"), ref item.Award);
                        if (item.EndRank < 0)
                        {
                            item.EndRank = int.MaxValue;
                        }

                        RuntimeData.DuanWeiRankAwardDict[new RangeKey(item.StarRank, item.EndRank)] = item;
                    }

                    //活动配置
                    RuntimeData.MapCodeDict.Clear();

                    fileName = "Config/TianTi.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        int mapCode = (int)Global.GetSafeAttributeLong(node, "MapCode");
                        if (!RuntimeData.MapCodeDict.ContainsKey(mapCode))
                        {
                            RuntimeData.MapCodeDict[mapCode] = 1;
                            RuntimeData.MapCodeList.Add(mapCode);
                        }

                        RuntimeData.WaitingEnterSecs = (int)Global.GetSafeAttributeLong(node, "WaitingEnterSecs");
                        RuntimeData.FightingSecs = (int)Global.GetSafeAttributeLong(node, "FightingSecs");
                        RuntimeData.ClearRolesSecs = (int)Global.GetSafeAttributeLong(node, "ClearRolesSecs");

                        if (!ConfigParser.ParserTimeRangeList(RuntimeData.TimePoints, Global.GetSafeAttributeStr(node, "TimePoints")))
                        {
                            success = false;
                            LogManager.WriteLog(LogTypes.Fatal, "读取跨服天梯系统时间配置(TimePoints)出错");
                        }

                        GameMap gameMap = null;
                        if (!GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out gameMap))
                        {
                            success = false;
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("缺少跨服天梯系统地图 {0}", mapCode));
                        }
                    }

                    //奖励配置
                    RuntimeData.DuanWeiJiFenNum = (int)GameManager.systemParamsList.GetParamValueIntByName("DuanWeiJiFenNum");
                    RuntimeData.WinDuanWeiJiFen = (int)GameManager.systemParamsList.GetParamValueIntByName("WinDuanWeiJiFen");
                    RuntimeData.LoseDuanWeiJiFen = (int)GameManager.systemParamsList.GetParamValueIntByName("LoseDuanWeiJiFen");
                    RuntimeData.MaxTianTiJiFen = (int)GameManager.systemParamsList.GetParamValueIntByName("MaxTianTiJiFen");
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

        public void GMStartHuoDongNow(int v)
        {
            try
            {
                lock (RuntimeData.Mutex)
                {
                    if (v == 0)
                    {
                        ConfigParser.ParserTimeRangeList(RuntimeData.TimePoints, RuntimeData.TimePointsStr);
                    }
                    else
                    {
                        ConfigParser.ParserTimeRangeList(RuntimeData.TimePoints, "00:00-23:59:59");
                    }
                }
            }
            catch (System.Exception ex)
            {
            	
            }
        }

        public void GMSetRoleData(GameClient client, int duanWeiId, int duanWeiJiFen, int rongYao, int monthDuanWeiRank, int lianSheng, int successCount, int fightCount)
        {
            RoleTianTiData roleTianTiData = client.ClientData.TianTiData;
            roleTianTiData.DuanWeiId = duanWeiId;
            roleTianTiData.DuanWeiJiFen = duanWeiJiFen;
            roleTianTiData.RongYao = rongYao;
            roleTianTiData.MonthDuanWeiRank = monthDuanWeiRank;
            roleTianTiData.LianSheng = lianSheng;
            roleTianTiData.SuccessCount = successCount;
            roleTianTiData.FightCount = fightCount;

            int selfDuanWeiId;
            if (RuntimeData.DuanWeiJiFenRangeDuanWeiIdDict.TryGetValue(roleTianTiData.DuanWeiJiFen, out selfDuanWeiId))
            {
                roleTianTiData.DuanWeiId = selfDuanWeiId;
            }

            Global.sendToDB<int, RoleTianTiData>((int)TCPGameServerCmds.CMD_DB_TIANTI_UPDATE_ROLE_DATA, roleTianTiData, client.ServerId);

            TianTiPaiHangRoleData tianTiPaiHangRoleData = new TianTiPaiHangRoleData();
            tianTiPaiHangRoleData.DuanWeiId = roleTianTiData.DuanWeiId;
            tianTiPaiHangRoleData.RoleId = roleTianTiData.RoleId;
            tianTiPaiHangRoleData.RoleName = client.ClientData.RoleName;
            tianTiPaiHangRoleData.Occupation = client.ClientData.Occupation;
            tianTiPaiHangRoleData.ZhanLi = client.ClientData.CombatForce;
            tianTiPaiHangRoleData.ZoneId = client.ClientData.ZoneID;
            tianTiPaiHangRoleData.DuanWeiJiFen = roleTianTiData.DuanWeiJiFen;
            RoleData4Selector roleInfo = Global.sendToDB<RoleData4Selector, string>((int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", client.ClientData.RoleID), client.ServerId);
            if (null != roleInfo || roleInfo.RoleID < 0)
            {
                tianTiPaiHangRoleData.RoleData4Selector = roleInfo;
            }
            PlayerJingJiData jingJiData = JingJiChangManager.getInstance().createJingJiData(client);

            TianTiRoleInfoData tianTiRoleInfoData = new TianTiRoleInfoData();
            tianTiRoleInfoData.RoleId = tianTiPaiHangRoleData.RoleId;
            tianTiRoleInfoData.ZoneId = tianTiPaiHangRoleData.ZoneId;
            tianTiRoleInfoData.ZhanLi = tianTiPaiHangRoleData.ZhanLi;
            tianTiRoleInfoData.RoleName = tianTiPaiHangRoleData.RoleName;
            tianTiRoleInfoData.DuanWeiId = tianTiPaiHangRoleData.DuanWeiId;
            tianTiRoleInfoData.DuanWeiJiFen = tianTiPaiHangRoleData.DuanWeiJiFen;
            tianTiRoleInfoData.DuanWeiRank = tianTiPaiHangRoleData.DuanWeiRank;
            tianTiRoleInfoData.TianTiPaiHangRoleData = DataHelper.ObjectToBytes(tianTiPaiHangRoleData);
            tianTiRoleInfoData.PlayerJingJiMirrorData = DataHelper.ObjectToBytes(jingJiData);
            TianTiClient.getInstance().UpdateRoleInfoData(tianTiRoleInfoData);
            GameManager.ClientMgr.ModifyTianTiRongYaoValue(client, rongYao, "GM添加");
        }

        #endregion GM指令

        #region 指令处理

        public void TimerProc(object sender, EventArgs e)
        {
            bool modify = false;
            TianTiRankData tianTiRankData = TianTiClient.getInstance().GetRankingData();
            lock (RuntimeData.Mutex)
            {
                if (tianTiRankData != null && tianTiRankData.ModifyTime > RuntimeData.ModifyTime)
                {
                    modify = true;
                }
            }

            if (modify)
            {
                Dictionary<int, TianTiPaiHangRoleData> tianTiPaiHangRoleDataDict = new Dictionary<int, TianTiPaiHangRoleData>();
                List<TianTiPaiHangRoleData> tianTiPaiHangRoleDataList = new List<TianTiPaiHangRoleData>();
                Dictionary<int, TianTiPaiHangRoleData> tianTiMonthPaiHangRoleDataDict = new Dictionary<int, TianTiPaiHangRoleData>();
                List<TianTiPaiHangRoleData> tianTiMonthPaiHangRoleDataList = new List<TianTiPaiHangRoleData>();
                if (null != tianTiRankData.TianTiRoleInfoDataList)
                {
                    foreach (var data in tianTiRankData.TianTiRoleInfoDataList)
                    {
                        TianTiPaiHangRoleData tianTiPaiHangRoleData;
                        if (null != data.TianTiPaiHangRoleData)
                        {
                            tianTiPaiHangRoleData = DataHelper.BytesToObject<TianTiPaiHangRoleData>(data.TianTiPaiHangRoleData, 0, data.TianTiPaiHangRoleData.Length);
                        }
                        else
                        {
                            tianTiPaiHangRoleData = new TianTiPaiHangRoleData() { RoleId = data.RoleId };
                        }

                        if (null != tianTiPaiHangRoleData)
                        {
                            tianTiPaiHangRoleData.RoleId = data.RoleId;
                            tianTiPaiHangRoleData.DuanWeiRank = data.DuanWeiRank;
                            tianTiPaiHangRoleDataDict[tianTiPaiHangRoleData.RoleId] = tianTiPaiHangRoleData;
                            if (tianTiPaiHangRoleDataList.Count < RuntimeData.MaxDayPaiMingListCount)
                            {
                                tianTiPaiHangRoleDataList.Add(tianTiPaiHangRoleData);
                            }
                        }
                    }
                }

                if (null != tianTiRankData.TianTiMonthRoleInfoDataList)
                {
                    foreach (var data in tianTiRankData.TianTiMonthRoleInfoDataList)
                    {
                        TianTiPaiHangRoleData tianTiPaiHangRoleData;
                        if (null != data.TianTiPaiHangRoleData)
                        {
                            tianTiPaiHangRoleData = DataHelper.BytesToObject<TianTiPaiHangRoleData>(data.TianTiPaiHangRoleData, 0, data.TianTiPaiHangRoleData.Length);
                        }
                        else
                        {
                            tianTiPaiHangRoleData = new TianTiPaiHangRoleData() { RoleId = data.RoleId };
                        }

                        if (null != tianTiPaiHangRoleData)
                        {
                            tianTiPaiHangRoleData.RoleId = data.RoleId;
                            tianTiPaiHangRoleData.DuanWeiRank = data.DuanWeiRank;
                            tianTiMonthPaiHangRoleDataDict[tianTiPaiHangRoleData.RoleId] = tianTiPaiHangRoleData;
                            if (tianTiMonthPaiHangRoleDataList.Count < RuntimeData.MaxMonthPaiMingListCount)
                            {
                                tianTiMonthPaiHangRoleDataList.Add(tianTiPaiHangRoleData);
                            }
                        }
                    }
                }

                lock (RuntimeData.Mutex)
                {
                    RuntimeData.ModifyTime = tianTiRankData.ModifyTime;
                    RuntimeData.MaxPaiMingRank = tianTiRankData.MaxPaiMingRank;
                    RuntimeData.TianTiPaiHangRoleDataDict = tianTiPaiHangRoleDataDict;
                    RuntimeData.TianTiPaiHangRoleDataList = tianTiPaiHangRoleDataList;
                    RuntimeData.TianTiMonthPaiHangRoleDataDict = tianTiMonthPaiHangRoleDataDict;
                    RuntimeData.TianTiMonthPaiHangRoleDataList = tianTiMonthPaiHangRoleDataList;
                }
            }
        }

        /// <summary>
        /// 罗兰城战攻防竞价申请指令处理
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessTianTiJoinCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
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
                int gropuIndex = client.ClientData.TianTiData.DuanWeiId;
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
                }

                if (result >= 0)
                {
                    result = TianTiClient.getInstance().TianTiSignUp(client.strUserID, client.ClientData.RoleID, client.ClientData.ZoneID,
                        (int)GameTypes.TianTi, gropuIndex, client.ClientData.CombatForce);

                    if (result > 0)
                    {
                        client.ClientData.SignUpGameType = (int)GameTypes.TianTi;
                        GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 1, 0, 0, 0, (int)GameTypes.TianTi);
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

        public bool ProcessGetTianTiDataAndDayPaiHangCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                TianTiDataAndDayPaiHang tianTiDataAndDayPaiHang = new TianTiDataAndDayPaiHang();
                if (IsGongNengOpened(client))
                {
                    //发送结果给客户端
                    InitRoleTianTiData(client);
                    tianTiDataAndDayPaiHang.TianTiData = client.ClientData.TianTiData;
                    lock (RuntimeData.Mutex)
                    {
                        int count = RuntimeData.TianTiPaiHangRoleDataList.Count;
                        if (count > 0)
                        {
                            tianTiDataAndDayPaiHang.PaiHangRoleDataList = RuntimeData.TianTiPaiHangRoleDataList.GetRange(0, count);
                        }
                    }
                }

                client.sendCmd(nID, tianTiDataAndDayPaiHang);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessGetTianTiMonthPaiHangDataCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                TianTiMonthPaiHangData tianTiMonthPaiHangData = new TianTiMonthPaiHangData();
                if (IsGongNengOpened(client))
                {
                    //发送结果给客户端
                    tianTiMonthPaiHangData.SelfPaiHangRoleData = new TianTiPaiHangRoleData()
                        {
                            RoleId = client.ClientData.RoleID,
                            RoleName = client.ClientData.RoleName,
                            DuanWeiId = client.ClientData.TianTiData.DuanWeiId,
                            DuanWeiJiFen = client.ClientData.TianTiData.DuanWeiJiFen,
                            DuanWeiRank = client.ClientData.TianTiData.DuanWeiRank,
                        };

                    lock (RuntimeData.Mutex)
                    {
                        if (null != RuntimeData.TianTiMonthPaiHangRoleDataList)
                        {
                            tianTiMonthPaiHangData.PaiHangRoleDataList = new List<TianTiPaiHangRoleData>(RuntimeData.TianTiMonthPaiHangRoleDataList);
                        }
                    }
                }

                client.sendCmd(nID, tianTiMonthPaiHangData);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessTianTiGetPaiHangAwardsCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Not_Exist;
                DuanWeiRankAward duanWeiRankAward = null;
                if (CanGetMonthRankAwards(client, out duanWeiRankAward))
                {
                    List<GoodsData> goodsDataList = Global.ConvertToGoodsDataList(duanWeiRankAward.Award.Items);
                    if (!Global.CanAddGoodsDataList(client, goodsDataList))
                    {
                        result = StdErrorCode.Error_BagNum_Not_Enough;
                    }
                    else
                    {
                        result = StdErrorCode.Error_Success_No_Info;
                        client.ClientData.TianTiData.FetchMonthDuanWeiRankAwardsTime = TimeUtil.NowDateTime();
                        Global.sendToDB<int, RoleTianTiData>((int)TCPGameServerCmds.CMD_DB_TIANTI_UPDATE_ROLE_DATA, client.ClientData.TianTiData, client.ServerId);
                        for (int i = 0; i < goodsDataList.Count; i++)
                        {
                            //向DBServer请求加入某个新的物品到背包中
                            Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsDataList[i].GoodsID, goodsDataList[i].GCount, goodsDataList[i].Quality, "", goodsDataList[i].Forge_level, goodsDataList[i].Binding, 0, "", true, 1, "天梯月段位排名奖励", Global.ConstGoodsEndTime, 0, goodsDataList[i].BornIndex, goodsDataList[i].Lucky, 0, goodsDataList[i].ExcellenceInfo, goodsDataList[i].AppendPropLev);
                        }
                    }
                }
                else if(duanWeiRankAward != null)
                {
                    if (client.CodeRevision <= 2)
                    {
                        result = StdErrorCode.Error_Success;
                        GameManager.ClientMgr.NotifyHintMsg(client, Global.GetLang("请等待排名更新后再领取排名奖励"));
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

        public bool ProcessTianTiGeLogCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                //发送结果给客户端
                List<TianTiLogItemData> logList = new List<TianTiLogItemData>();
                logList = Global.sendToDB<List<TianTiLogItemData>, int>((int)TCPGameServerCmds.CMD_SPR_TIANTI_GET_LOG, client.ClientData.RoleID, client.ServerId);
                client.sendCmd(nID, logList);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessTianTiEnterCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Success_No_Info);
                    return true;
                }

                int result = StdErrorCode.Error_Success;
                int flag = Global.SafeConvertToInt32(cmdParams[1]);
                if (flag > 0)
                {
                    result = TianTiClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.EnterGame);
                    if (result >= 0)
                    {
                        GlobalNew.RecordSwitchKuaFuServerLog(client);
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client));
                    }
                    else
                    {
                        flag = 0;
                    }
                }
                else
                {
                    //TianTiClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.None);
                }

                client.ClientData.SignUpGameType = (int)GameTypes.None;
                if (flag <= 0)
                {
                    Global.GetClientKuaFuServerLoginData(client).RoleId = 0;
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TIANTI_QUIT, StdErrorCode.Error_Success_No_Info);
                }

                //client.sendCmd(nID, result);
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
        public bool ProcessTianTiQuitCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Success_No_Info);
                    return true;
                }

                int result = StdErrorCode.Error_Success;

                if (result >= 0)
                {
                    result = TianTiClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.None);
                    client.ClientData.SignUpGameType = (int)GameTypes.None;
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

        public bool InitRoleTianTiData(GameClient client)
        {
            bool rankChanged = false;
            DateTime now = TimeUtil.NowDateTime();
            DateTime lastMonth = now.AddMonths(-1);
            lastMonth = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            lock (RuntimeData.Mutex)
            {
                if (RuntimeData.ModifyTime > lastMonth)
                {
                    TianTiPaiHangRoleData tianTiPaiHangRoleData;
                    int newRank = RuntimeData.MaxPaiMingRank + 1;
                    if (RuntimeData.TianTiPaiHangRoleDataDict.TryGetValue(client.ClientData.RoleID, out tianTiPaiHangRoleData))
                    {
                        newRank = tianTiPaiHangRoleData.DuanWeiRank;
                    }
                    if (client.ClientData.TianTiData.DuanWeiRank != newRank)
                    {
                        rankChanged = true;
                        client.ClientData.TianTiData.DuanWeiRank = newRank;
                    }

                    newRank = RuntimeData.MaxPaiMingRank + 1;
                    if (RuntimeData.TianTiMonthPaiHangRoleDataDict.TryGetValue(client.ClientData.RoleID, out tianTiPaiHangRoleData))
                    {
                        newRank = tianTiPaiHangRoleData.DuanWeiRank;
                    }

                    if (client.ClientData.TianTiData.MonthDuanWeiRank != newRank)
                    {
                        rankChanged = true;
                        client.ClientData.TianTiData.MonthDuanWeiRank = newRank;
                    }
                }

                DateTime lastFightDay = Global.GetRealDate(client.ClientData.TianTiData.LastFightDayId);
                if (RuntimeData.ModifyTime > lastFightDay && lastFightDay.Month != RuntimeData.ModifyTime.Month)
                {
                    client.ClientData.TianTiData.LianSheng = 0;
                    client.ClientData.TianTiData.SuccessCount = 0;
                    client.ClientData.TianTiData.FightCount = 0;
                    client.ClientData.TianTiData.DuanWeiJiFen = 0;
                }

                int selfDuanWeiId;
                if (RuntimeData.DuanWeiJiFenRangeDuanWeiIdDict.TryGetValue(client.ClientData.TianTiData.DuanWeiJiFen, out selfDuanWeiId))
                {
                    client.ClientData.TianTiData.DuanWeiId = selfDuanWeiId;
                }

                if (!client.ClientSocket.IsKuaFuLogin && lastFightDay.Date != now.Subtract(RuntimeData.RefreshTime).Date)
                {
                    client.ClientData.TianTiData.TodayFightCount = 0;
                }

                //判断上个月是否参加过天体赛
                if (lastFightDay < lastMonth)
                {
                    client.ClientData.TianTiData.FetchMonthDuanWeiRankAwardsTime = lastMonth.AddMonths(1);
                }

                client.ClientData.TianTiData.RankUpdateTime = RuntimeData.ModifyTime;
            }

            if (client.ClientData.TianTiData.TodayFightCount == 0)
            {
                client.ClientData.TianTiData.DayDuanWeiJiFen = 0;
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TianTiDayScore, 0, true);
            }
            else
            {
                client.ClientData.TianTiData.DayDuanWeiJiFen = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TianTiDayScore);
            }

            //if (rankChanged)
            //{
            //    Global.sendToDB<int, RoleTianTiData>((int)TCPGameServerCmds.CMD_DB_TIANTI_UPDATE_ROLE_DATA, client.ClientData.TianTiData, client.ServerId);
            //}

            return rankChanged;
        }

        public int GetBirthPoint(GameClient client, out int posX, out int posY)
        {
            int side = client.ClientData.BattleWhichSide;
            if (side <= 0)
            {
                KuaFuServerLoginData clientKuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);
                side = TianTiClient.getInstance().GetRoleBattleWhichSide((int)clientKuaFuServerLoginData.GameId, clientKuaFuServerLoginData.RoleId);
                if (side > 0)
                {
                    client.ClientData.BattleWhichSide = side;
                }
            }

            lock (RuntimeData.Mutex)
            {
                TianTiBirthPoint TianTiBirthPoint = null;
                if (RuntimeData.MapBirthPointDict.TryGetValue(side, out TianTiBirthPoint))
                {
                    posX = TianTiBirthPoint.PosX;
                    posY = TianTiBirthPoint.PosY;
                    return side;
                }
            }

            posX = 0;
            posY = 0;
            return -1;
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

            KuaFuServerLoginData kuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);
            //TianTiFuBenItem tianTiFuBenItem;
            //lock(RuntimeData.Mutex)
            //{
            //    if (!RuntimeData.TianTiFuBenItemDict.TryGetValue(kuaFuServerLoginData.GameId, out tianTiFuBenItem))
            //    {
            //        tianTiFuBenItem = new TianTiFuBenItem() { GameId = kuaFuServerLoginData.GameId };
            //        tianTiFuBenItem.FuBenSeqId = GameCoreInterface.getinstance().GetNewFuBenSeqId();
            //        RuntimeData.TianTiFuBenItemDict[tianTiFuBenItem.GameId] = tianTiFuBenItem;
            //    }

            //    kuaFuServerLoginData.FuBenSeqId = tianTiFuBenItem.FuBenSeqId;
            //}

            int index = ((int)kuaFuServerLoginData.GameId) % RuntimeData.MapCodeList.Count;
            client.ClientData.MapCode = RuntimeData.MapCodeList[index];
            client.ClientData.PosX = posX;
            client.ClientData.PosY = posY;
            client.ClientData.BattleWhichSide = side;

            int fuBenSeq = 0;
            lock (RuntimeData.Mutex)
            {
                if (!RuntimeData.GameId2FuBenSeq.TryGetValue((int)kuaFuServerLoginData.GameId, out fuBenSeq))
                {
                    fuBenSeq = GameCoreInterface.getinstance().GetNewFuBenSeqId();
                    RuntimeData.GameId2FuBenSeq[(int)kuaFuServerLoginData.GameId] = fuBenSeq;
                }
            }
            kuaFuServerLoginData.FuBenSeqId = fuBenSeq;
            client.ClientData.FuBenSeqID = kuaFuServerLoginData.FuBenSeqId;

            return true;
        }

        /// <summary>
        /// 判断功能是否开启
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool IsGongNengOpened(GameClient client, bool hint = false)
        {
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.TianTi))
            {
                return false;
            }

            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return false;
            }

            return GlobalNew.IsGongNengOpened(client, GongNengIDs.TianTi, hint);
        }

        public bool CanGetMonthRankAwards(GameClient client, out DuanWeiRankAward duanWeiRankAward)
        {
            duanWeiRankAward = null;
            lock (RuntimeData.Mutex)
            {
                if (client.ClientData.TianTiData.MonthDuanWeiRank > 0)
                {
                    if (RuntimeData.DuanWeiRankAwardDict.TryGetValue(client.ClientData.TianTiData.MonthDuanWeiRank, out duanWeiRankAward))
                    {
                        DateTime fetchTime = client.ClientData.TianTiData.FetchMonthDuanWeiRankAwardsTime;
                        DateTime now = TimeUtil.NowDateTime();
                        if ((fetchTime.Month != now.Month || fetchTime.Year != now.Year))
                        {
                            if (new DateTime(fetchTime.Year, fetchTime.Month, 1) < new DateTime(RuntimeData.ModifyTime.Year, RuntimeData.ModifyTime.Month, 1))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        #endregion 其他
    }
}
