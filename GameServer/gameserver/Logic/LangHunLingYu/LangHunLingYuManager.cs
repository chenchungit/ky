#define ___CC___FUCK___YOU___BB___

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
using GameServer.Logic;

namespace GameServer.Logic
{
    /// <summary>
    /// 圣域争霸管理器
    /// </summary>
    public partial class LangHunLingYuManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 标准接口

        public const SceneUIClasses ManagerType = SceneUIClasses.LangHunLingYu;

        private static LangHunLingYuManager instance = new LangHunLingYuManager();

        public static LangHunLingYuManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public LangHunLingYuData RuntimeData = new LangHunLingYuData();

        /// <summary>
        /// 定时的等级经验奖励管理器
        /// </summary>
        public LevelAwardsMgr _LevelAwardsMgr = new LevelAwardsMgr();

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
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_JOIN, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_WORLD_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_CITY_DATA, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_ENTER, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_GET_DAY_AWARD, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_ADMIRE_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_ADMIRE_HIST, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_ADMIRE, 2, 2, getInstance());

            //向事件源注册监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerLogout, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreBangHuiAddMember, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreBangHuiRemoveMember, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreBangHuiChangeZhiWu, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PostBangHuiChange, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.NotifyLhlyBangHuiData, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.NotifyLhlyCityData, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.NotifyLhlyOtherCityList, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.NotifyLhlyCityOwnerHist, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.NotifyLhlyCityOwnerAdmire, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.OnCreateMonster, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreMonsterInjure, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.OnClientChangeMap, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.ProcessClickOnNpc, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, getInstance());
            //GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerDead, getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.StartPlayGame, getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerInitGame, getInstance());
            return true;
        }

        public bool startup()
        {
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("LangHunLingYuManager.TimerProc", TimerProc), 15000, 1428);
            return true;
        }

        public bool showdown()
        {
            //向事件源删除监听器
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerLogout, getInstance());
            //GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreInstallJunQi, SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreBangHuiAddMember, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreBangHuiRemoveMember, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreBangHuiChangeZhiWu, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PostBangHuiChange, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.NotifyLhlyBangHuiData, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.NotifyLhlyCityData, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.NotifyLhlyOtherCityList, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.NotifyLhlyCityOwnerHist, (int)SceneUIClasses.LangHunLingYu, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.NotifyLhlyCityOwnerAdmire, (int)SceneUIClasses.LangHunLingYu, getInstance());
            //GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerDead, getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.StartPlayGame, getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerInitGame, getInstance());

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
				case (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_JOIN:
                    return ProcessLangHunLingYuJoinCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_ENTER:
                    return ProcessLangHunLingYuEnterCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_DATA:
                    return ProcessLangHunLingYuRoleDataCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_CITY_DATA:
                    return ProcessLangHunLingYuCityDataCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_WORLD_DATA:
                    return ProcessLangHunLingYuWorldDataCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_GET_DAY_AWARD:
                    return ProcessGetDailyAwardsCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_ADMIRE_DATA:
                    return ProcessGetAdmireDataCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_ADMIRE_HIST:
                    return ProcessGetAdmireHistoryCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_ADMIRE:
                    return ProcessAdmireCmd(client, nID, bytes, cmdParams);
            }

            return true;
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventObject"></param>
        public void processEvent(EventObject eventObject)
        {
            int nID = eventObject.getEventType();
            switch (nID)
            {
                case (int)EventTypes.MonsterDead:
                    MonsterDeadEventObject e = eventObject as MonsterDeadEventObject;
                    OnProcessJunQiDead(e.getAttacker(), e.getMonster());
                    break;
                case (int)EventTypes.PlayerInitGame:
                    PlayerInitGameEventObject playerInitGameEventObject = eventObject as PlayerInitGameEventObject;
                    if (null != playerInitGameEventObject)
                    {
                        OnInitGame(playerInitGameEventObject.getPlayer());
                    }
                    break;
                case (int)EventTypes.StartPlayGame:
                    OnStartPlayGameEventObject onStartPlayGameEventObject = eventObject as OnStartPlayGameEventObject;
                    if (onStartPlayGameEventObject.Client.SceneType == (int)SceneUIClasses.LangHunLingYu)
                    {
                        YongZheZhanChangClient.getInstance().ChangeRoleState(onStartPlayGameEventObject.Client.ClientData.RoleID, KuaFuRoleStates.None);
                        OnStartPlayGame(onStartPlayGameEventObject.Client);
                    }
                    break;
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
                case (int)EventTypes.PreBangHuiAddMember:
                    {
                        PreBangHuiAddMemberEventObject e = eventObject as PreBangHuiAddMemberEventObject;
                        if (null != e)
                        {
                            eventObject.Handled = OnPreBangHuiAddMember(e);
                        }
                    }
                    break;
                case (int)EventTypes.PreBangHuiRemoveMember:
                    {
                        PreBangHuiRemoveMemberEventObject e = eventObject as PreBangHuiRemoveMemberEventObject;
                        if (null != e)
                        {
                            eventObject.Handled = OnPreBangHuiRemoveMember(e);
                        }
                    }
                    break;
                case (int)EventTypes.PreBangHuiChangeZhiWu:
                    {
                        PreBangHuiChangeZhiWuEventObject e = eventObject as PreBangHuiChangeZhiWuEventObject;
                        if (null != e)
                        {
                            //如果是圣域城主，则禁止改变帮会职务
                            if (e.Player.ClientData.Faction == RuntimeData.ChengHaoBHid && e.TargetZhiWu == (int)ZhanMengZhiWus.ShouLing)
                            {
                                LogManager.WriteLog(LogTypes.Warning, string.Format("圣域城主禁止委任首领职务"));
                                eventObject.Handled = true;
                                eventObject.Result = false;
                            }
                        }
                    }
                    break;
                case (int)EventTypes.PostBangHuiChange:
                    {
                        PostBangHuiChangeEventObject e = eventObject as PostBangHuiChangeEventObject;
                        if (null != e && null != e.Player)
                        {
                            UpdateChengHaoBuffer(e.Player, 0, RuntimeData.ChengHaoBHid);
                        }
                    }
                    break;
                case (int)GlobalEventTypes.KuaFuNotifyEnterGame:
                    {
                        KuaFuNotifyEnterGameEvent e = eventObject as KuaFuNotifyEnterGameEvent;
                        if (null != e)
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("通知角色ID={0}拥有进入勇者战场资格,跨服GameID={1}", 0, 0));
                        }
                    }
                    break;
                case (int)EventTypes.ProcessClickOnNpc:
                    {
                        ProcessClickOnNpcEventObject e = eventObject as ProcessClickOnNpcEventObject;
                        if (null != e)
                        {
                            int npcId = 0;
                            if (null != e.Npc)
                            {
                                npcId = e.Npc.NpcID;
                            }
                            if (OnSpriteClickOnNpc(e.Client, e.NpcId, e.ExtensionID))
                            {
                                e.Result = false;
                                e.Handled = true;
                            }
                        }
                    }
                    break;
                case (int)EventTypes.OnCreateMonster:
                    {
                        OnCreateMonsterEventObject e = eventObject as OnCreateMonsterEventObject;
                        if (null != e)
                        {
                            QiZhiConfig qiZhiConfig = e.Monster.Tag as QiZhiConfig;
                            if (null != qiZhiConfig)
                            {
                                e.Monster.MonsterName = qiZhiConfig.InstallBhName;
                                e.Monster.Camp = qiZhiConfig.BattleWhichSide;
                                e.Result = true;
                                e.Handled = true;
                            }
                        }
                    }
                    break;
                case (int)EventTypes.PreMonsterInjure:
                    {
                        PreMonsterInjureEventObject e = eventObject as PreMonsterInjureEventObject;
                        if (null != e)
                        {
                            lock (RuntimeData.Mutex)
                            {
#if ___CC___FUCK___YOU___BB___
                                if (RuntimeData.JunQiMonsterHashSet.Contains(e.Monster.XMonsterInfo.MonsterId))
                                {
                                    e.Injure = RuntimeData.CutLifeV;
                                    e.Result = true;
                                    e.Handled = true;
                                }
#else
                                 if (RuntimeData.JunQiMonsterHashSet.Contains(e.Monster.MonsterInfo.ExtensionID))
                                {
                                    e.Injure = RuntimeData.CutLifeV;
                                    e.Result = true;
                                    e.Handled = true;
                                }
#endif
                            }
                        }
                    }
                    break;
                case (int)EventTypes.OnClientChangeMap:
                    {
                        OnClientChangeMapEventObject e = eventObject as OnClientChangeMapEventObject;
                        if (null != e)
                        {
                            e.Handled = e.Result = ClientChangeMap(e.Client, ref e.ToMapCode, ref e.ToPosX, ref e.ToPosY);
                        }
                    }
                    break;
                case (int)GlobalEventTypes.NotifyLhlyBangHuiData:
                    {
                        NotifyLhlyBangHuiDataGameEvent e = eventObject as NotifyLhlyBangHuiDataGameEvent;
                        if (null != e)
                        {
                            UpdateBangHuiDataEx(e.Arg as LangHunLingYuBangHuiDataEx);
                            e.Handled = e.Result = true;
                        }
                    }
                    break;
                case (int)GlobalEventTypes.NotifyLhlyCityData:
                    {
                        NotifyLhlyCityDataGameEvent e = eventObject as NotifyLhlyCityDataGameEvent;
                        if (null != e)
                        {
                            UpdateCityDataEx(e.Arg as LangHunLingYuCityDataEx);
                            e.Handled = e.Result = true;
                        }
                    }
                    break;
                case (int)GlobalEventTypes.NotifyLhlyOtherCityList:
                    {
                        NotifyLhlyOtherCityListGameEvent e = eventObject as NotifyLhlyOtherCityListGameEvent;
                        if (null != e)
                        {
                            UpdateOtherCityList(e.Arg);
                            e.Handled = e.Result = true;
                        }
                    }
                    break;
                case (int)GlobalEventTypes.NotifyLhlyCityOwnerHist:
                    {
                        NotifyLhlyCityOwnerHistGameEvent e = eventObject as NotifyLhlyCityOwnerHistGameEvent;
                        if (null != e)
                        {
                            UpdateCityOwnerHist(e.Arg);
                            e.Handled = e.Result = true;
                        }
                    }
                    break;
                case (int)GlobalEventTypes.NotifyLhlyCityOwnerAdmire:
                    {
                        NotifyLhlyCityOwnerAdmireGameEvent e = eventObject as NotifyLhlyCityOwnerAdmireGameEvent;
                        if (null != e)
                        {
                            UpdateCityOwnerAdmire(e.RoleID, e.AdmireCount);
                            e.Handled = e.Result = true;
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
                    RuntimeData.CutLifeV = (int)GameManager.systemParamsList.GetParamValueIntByName("CutLifeV", 10);

                    RuntimeData.CityLevelInfoDict.Clear();

                    fileName = "Config/City.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    int cityId = 0;
                    foreach (var node in nodes)
                    {
                        CityLevelInfo item = new CityLevelInfo();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.CityLevel = (int)Global.GetSafeAttributeLong(node, "CityLevel");
                        item.CityNum = (int)Global.GetSafeAttributeLong(node, "CityNum");
                        item.MaxNum = (int)Global.GetSafeAttributeLong(node, "MaxNum");
                        item.ZhanMengZiJin = (int)Global.GetSafeAttributeLong(node, "ZhanMengZiJin");
                        item.AttackWeekDay = Global.StringToIntList(Global.GetSafeAttributeStr(node, "AttackWeekDay"), ',');
                        ConfigParser.ParseAwardsItemList(Global.GetSafeAttributeStr(node, "Award"), ref item.Award);
                        ConfigParser.ParseAwardsItemList(Global.GetSafeAttributeStr(node, "DayAward"), ref item.DayAward);
                        if (!ConfigParser.ParserTimeRangeListWithDay(item.BaoMingTime, Global.GetSafeAttributeStr(node, "BaoMingTime").Replace(';', '|')))
                        {
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("解析文件{0}的BaoMingTime出错", fileName));
                            return false;
                        }

                        if (!ConfigParser.ParserTimeRangeList(item.AttackTime, Global.GetSafeAttributeStr(node, "AttackTime")))
                        {
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("解析文件{0}的BaoMingTime出错", fileName));
                            return false;
                        }

                        RuntimeData.CityLevelInfoDict[item.CityLevel] = item;
                        for (int i = 0; i < item.CityNum; i++)
                        {
                            cityId++;
                            RuntimeData.CityDataExDict.Add(cityId, new LangHunLingYuCityDataEx() { CityId = cityId, CityLevel = item.CityLevel });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    RuntimeData.MapBirthPointListDict.Clear();

                    fileName = "Config/SiegeWarfareBirthPoint.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        MapBirthPoint item = new MapBirthPoint();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.Type = (int)Global.GetSafeAttributeLong(node, "Type");
                        item.MapCode = (int)Global.GetSafeAttributeLong(node, "MapCode");
                        item.BirthPosX = (int)Global.GetSafeAttributeLong(node, "BirthPosX");
                        item.BirthPosY = (int)Global.GetSafeAttributeLong(node, "BirthPosY");
                        item.BirthRangeX = (int)Global.GetSafeAttributeLong(node, "BirthRangeX");
                        item.BirthRangeY = (int)Global.GetSafeAttributeLong(node, "BirthRangeY");

                        List<MapBirthPoint> list;
                        if (!RuntimeData.MapBirthPointListDict.TryGetValue(item.Type, out list))
                        {
                            list = new List<MapBirthPoint>();
                            RuntimeData.MapBirthPointListDict.Add(item.Type, list);
                        }
                        list.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    RuntimeData.NPCID2QiZhiConfigDict.Clear();
                    RuntimeData.QiZhiBuffOwnerDataList.Clear();
                    RuntimeData.QiZhiBuffDisableParamsDict.Clear();
                    RuntimeData.QiZhiBuffEnableParamsDict.Clear();

                    fileName = "Config/CityWarQiZuo.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        QiZhiConfig item = new QiZhiConfig();
                        item.NPCID = (int)Global.GetSafeAttributeLong(node, "NPCID");
                        item.BufferID = (int)Global.GetSafeAttributeLong(node, "BufferID");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "PosX");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "PosY");
                        item.MonsterId = (int)Global.GetSafeAttributeLong(node, "JuQiID");

                        RuntimeData.JunQiMonsterHashSet.Add(item.MonsterId);
                        RuntimeData.NPCID2QiZhiConfigDict[item.NPCID] = item;
                        RuntimeData.QiZhiBuffOwnerDataList.Add(new LangHunLingYuQiZhiBuffOwnerData() { NPCID = item.NPCID, OwnerBHName = ""});
                        RuntimeData.QiZhiBuffDisableParamsDict[item.BufferID] = new double[] { 0, item.BufferID };
                        RuntimeData.QiZhiBuffEnableParamsDict[item.BufferID] = new double[] { 0, item.BufferID };
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    //活动配置
                    QiZhiConfig qiZhiConfig;
                    if (RuntimeData.NPCID2QiZhiConfigDict.TryGetValue(RuntimeData.SuperQiZhiNpcId, out qiZhiConfig))
                    {
                        RuntimeData.SuperQiZhiOwnerBirthPosX = qiZhiConfig.PosX;
                        RuntimeData.SuperQiZhiOwnerBirthPosY = qiZhiConfig.PosY;
                    }

                    RuntimeData.SceneDataDict.Clear();
                    RuntimeData.LevelRangeSceneIdDict.Clear();

                    fileName = "Config/CityWar.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        LangHunLingYuSceneInfo sceneItem = new LangHunLingYuSceneInfo();
                        int id = (int)Global.GetSafeAttributeLong(node, "ID");
                        sceneItem.Id = id;
                        sceneItem.MapCode = (int)Global.GetSafeAttributeLong(node, "MapCode1");
                        sceneItem.MapCode_LongTa = (int)Global.GetSafeAttributeLong(node, "MapCode2");
                        sceneItem.MinLevel = (int)Global.GetSafeAttributeLong(node, "MinLevel");
                        sceneItem.MaxLevel = 10000;
                        sceneItem.MinZhuanSheng = (int)Global.GetSafeAttributeLong(node, "MinZhuanSheng");
                        sceneItem.MaxZhuanSheng = 10000;
                        sceneItem.PrepareSecs = (int)Global.GetSafeAttributeLong(node, "PrepareSecs");
                        sceneItem.WaitingEnterSecs = (int)Global.GetSafeAttributeLong(node, "WaitingEnterSecs");
                        sceneItem.FightingSecs = (int)Global.GetSafeAttributeLong(node, "FightingSecs");
                        sceneItem.ClearRolesSecs = (int)Global.GetSafeAttributeLong(node, "ClearRolesSecs");

                        GameMap gameMap = null;
                        if (!GameManager.MapMgr.DictMaps.TryGetValue(sceneItem.MapCode, out gameMap))
                        {
                            success = false;
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("33地图配置中缺少{0}所需的地图:{1}", fileName, sceneItem.MapCode));
                        }

                        if (!GameManager.MapMgr.DictMaps.TryGetValue(sceneItem.MapCode_LongTa, out gameMap))
                        {
                            success = false;
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("44地图配置中缺少{0}所需的地图:{1}", fileName, sceneItem.MapCode_LongTa));
                        }

                        RangeKey range = new RangeKey(Global.GetUnionLevel(sceneItem.MinZhuanSheng, sceneItem.MinLevel), Global.GetUnionLevel(sceneItem.MaxZhuanSheng, sceneItem.MaxLevel));
                        RuntimeData.LevelRangeSceneIdDict[range] = sceneItem;
                        RuntimeData.SceneDataDict[id] = sceneItem;
                        RuntimeData.SceneInfoId = id;
                        RuntimeData.SceneDataList.Add(sceneItem);
                    }
                }
                catch (System.Exception ex)
                {
                    success = false;
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("加载xml配置文件:{0}, 失败。", fileName), ex);
                }

                try
                {
                    fileName = "Config/SiegeWarfareExp.xml";
                    _LevelAwardsMgr.LoadFromXMlFile(fileName, "", "ID");
                }
                catch (System.Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }
            }

            // 石头雕像走起
            RestoreLangHunLingYuNpc();

            return success ;
        }

        public int GetCityLevelById(int cityId)
        {
            lock (RuntimeData.Mutex)
            {
                LangHunLingYuCityDataEx cityDataEx;
                if (RuntimeData.CityDataExDict.TryGetValue(cityId, out cityDataEx))
                {
                    return cityDataEx.CityLevel;
                }
            }

            return 0;
        }

        /// <summary>
        /// 获取帮会占领的最高等级城池的等级
        /// </summary>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public int GetBangHuiCityLevel(int bhid)
        {
            lock (RuntimeData.Mutex)
            {
                LangHunLingYuBangHuiDataEx bangHuiData;
                if (RuntimeData.BangHuiDataExDict.TryGetValue(bhid, out bangHuiData))
                {
                    return bangHuiData.Level;
                }
            }

            return 0;
        }

#endregion 初始化配置

#region 内存中的王族帮会ID

        /// <summary>
        /// 查询帮会名
        /// </summary>
        public string GetBangHuiName(int bhid, out int zoneId)
        {
            zoneId = 0;
            LangHunLingYuBangHuiDataEx bangHuiDataEx;
            if (!RuntimeData.BangHuiDataExDict.TryGetValue(bhid, out bangHuiDataEx))
            {
                return Global.GetLang("无");
            }

            zoneId = bangHuiDataEx.ZoneId;
            return bangHuiDataEx.BhName;
        }

#endregion 内存中的王族帮会ID

#region 指令处理

        private void UpdateCityDataEx(LangHunLingYuCityDataEx cityDataEx)
        {
            if (null == cityDataEx)
            {
                return;
            }

            lock (RuntimeData.Mutex)
            {
                HashSet<long> AddBangHuiIdHashSet = new HashSet<long>();
                HashSet<long> removedBangHuiIdHashSet = new HashSet<long>();
                HashSet<long> oldBangHuiAttackerIdHashSet = new HashSet<long>();
                HashSet<long> newBangHuiAttackerIdHashSet = new HashSet<long>();
                //所有受影响帮会ID集合，有多余的0值
                HashSet<long> allBangHuiIdHashSet = new HashSet<long>();

                //需要重新计算最高城池等级的帮派
                LangHunLingYuCityDataEx oldCityDataEx;
                AddBangHuiIdHashSet = new HashSet<long>(cityDataEx.Site);
                allBangHuiIdHashSet.UnionWith(cityDataEx.Site);
                if (RuntimeData.CityDataExDict.TryGetValue(cityDataEx.CityId, out oldCityDataEx))
                {
                    AddBangHuiIdHashSet.ExceptWith(oldCityDataEx.Site);
                    removedBangHuiIdHashSet = new HashSet<long>(oldCityDataEx.Site);
                    removedBangHuiIdHashSet.ExceptWith(cityDataEx.Site);
                    for (int i = Consts.LangHunLingYuCityAttackerSite; i < oldCityDataEx.Site.Length; i++ )
                    {
                        long bhid = oldCityDataEx.Site[i];
                        if (bhid > 0 && !oldBangHuiAttackerIdHashSet.Contains(bhid)) oldBangHuiAttackerIdHashSet.Add(bhid);
                    }
                    allBangHuiIdHashSet.UnionWith(oldCityDataEx.Site);
                }

                for (int i = Consts.LangHunLingYuCityAttackerSite; i < cityDataEx.Site.Length; i++)
                {
                    long bhid = cityDataEx.Site[i];
                    if (bhid > 0 && !newBangHuiAttackerIdHashSet.Contains(bhid)) newBangHuiAttackerIdHashSet.Add(bhid);
                }

                RuntimeData.CityDataExDict[cityDataEx.CityId] = cityDataEx;

                //更新LangHunLingYuCityData
                LangHunLingYuCityData cityData;
                if (!RuntimeData.CityDataDict.TryGetValue(cityDataEx.CityId, out cityData))
                {
                    cityData = new LangHunLingYuCityData();
                    cityData.CityId = cityDataEx.CityId;
                    cityData.CityLevel = cityDataEx.CityLevel;
                    RuntimeData.CityDataDict[cityDataEx.CityId] = cityData;
                }

                LangHunLingYuBangHuiDataEx bangHuiDataEx;
                if (RuntimeData.BangHuiDataExDict.TryGetValue(cityDataEx.Site[0], out bangHuiDataEx))
                {
                    cityData.Owner = new BangHuiMiniData()
                    {
                        BHID = bangHuiDataEx.Bhid,
                        BHName = bangHuiDataEx.BhName,
                        ZoneID = bangHuiDataEx.ZoneId,
                    };
                }
                else
                {
                    cityData.Owner = null;
                }

                cityData.AttackerList.Clear();
                for (int i = Consts.LangHunLingYuCityAttackerSite; i < cityDataEx.Site.Length; i++ )
                {
                    long id = cityDataEx.Site[i];
                    if (id > 0 && RuntimeData.BangHuiDataExDict.TryGetValue(id, out bangHuiDataEx))
                    {
                        cityData.AttackerList.Add(new BangHuiMiniData()
                            {
                                BHID = bangHuiDataEx.Bhid,
                                BHName = bangHuiDataEx.BhName,
                                ZoneID = bangHuiDataEx.ZoneId,
                            });
                    }
                }

                foreach (var id in AddBangHuiIdHashSet)
                {
                    LangHunLingYuBangHuiData bangHuiData;
                    if (!RuntimeData.BangHuiDataDict.TryGetValue(id, out bangHuiData))
                    {
                        bangHuiData = new LangHunLingYuBangHuiData();
                        RuntimeData.BangHuiDataDict[id] = bangHuiData;
                    }

                    bangHuiData.SelfCityList.Add(cityDataEx.CityId);
                    bangHuiData.SelfCityList.Sort();
                }

                foreach (var id in removedBangHuiIdHashSet)
                {
                    LangHunLingYuBangHuiData bangHuiData;
                    if (!RuntimeData.BangHuiDataDict.TryGetValue(id, out bangHuiData))
                    {
                        bangHuiData = new LangHunLingYuBangHuiData();
                        RuntimeData.BangHuiDataDict[id] = bangHuiData;
                    }

                    bangHuiData.SelfCityList.Remove(cityDataEx.CityId);
                }

                foreach (var id in newBangHuiAttackerIdHashSet.Except(oldBangHuiAttackerIdHashSet))
                {
                    LangHunLingYuBangHuiData bangHuiData;
                    if (RuntimeData.BangHuiDataDict.TryGetValue(id, out bangHuiData))
                    {
                        bangHuiData.SignUpState = 1;
                    }
                }
                foreach (var id in oldBangHuiAttackerIdHashSet.Except(newBangHuiAttackerIdHashSet))
                {
                    LangHunLingYuBangHuiData bangHuiData;
                    if (RuntimeData.BangHuiDataDict.TryGetValue(id, out bangHuiData))
                    {
                        bangHuiData.SignUpState = 0;
                    }
                }

                long chengHaoBhid = 0;
                if (RuntimeData.CityDataExDict.TryGetValue(Consts.LangHunLingYuMinCityID, out cityDataEx))
                {
                    chengHaoBhid = cityDataEx.Site[0];
                    ReplaceLangHunLingYuNpc();
                }

                foreach (var id in allBangHuiIdHashSet)
                {
                    LangHunLingYuBangHuiData bangHuiData;
                    if (RuntimeData.BangHuiDataDict.TryGetValue(id, out bangHuiData))
                    {
                        bangHuiData.DayAwardFlags = 0;
                        foreach (var cityId in bangHuiData.SelfCityList)
                        {
                            if (RuntimeData.CityDataExDict.TryGetValue(cityId, out cityDataEx))
                            {
                                if (cityDataEx.Site[0] == id)
                                {
                                    bangHuiData.DayAwardFlags = Global.SetIntSomeBit(cityDataEx.CityLevel, bangHuiData.DayAwardFlags, true);
                                }
                            }
                        }
                    }
                }

                //广播所有帮会玩家最新数据
                BroadcastBangHuiCityData(allBangHuiIdHashSet, RuntimeData.ChengHaoBHid, chengHaoBhid);
                RuntimeData.ChengHaoBHid = chengHaoBhid;
            }
        }

        private void UpdateBangHuiDataEx(LangHunLingYuBangHuiDataEx bangHuiDataEx)
        {
            if (null != bangHuiDataEx)
            {
                lock (RuntimeData.Mutex)
                {
                    RuntimeData.BangHuiDataExDict[bangHuiDataEx.Bhid] = bangHuiDataEx;
                    LangHunLingYuBangHuiData bangHuiData;
                    if (!RuntimeData.BangHuiDataDict.TryGetValue(bangHuiDataEx.Bhid, out bangHuiData))
                    {
                        bangHuiData = new LangHunLingYuBangHuiData();
                        RuntimeData.BangHuiDataDict[bangHuiDataEx.Bhid] = bangHuiData;
                    }
                }
            }
        }

        private void UpdateOtherCityList(Dictionary<int, List<int>> list)
        {
            lock (RuntimeData.Mutex)
            {
                RuntimeData.OtherCityList = list;
            }
        }

        private void UpdateCityOwnerAdmire(int rid, int admirecount)
        {
            lock (RuntimeData.Mutex)
            {
                if (null == RuntimeData.OwnerHistList)
                    return;

                foreach(var data in RuntimeData.OwnerHistList)
                {
                    if(data.rid == rid)
                    {
                        data.AdmireCount = admirecount;
                    }
                }
            }
        }

        private void UpdateCityOwnerHist(List<LangHunLingYuKingHist> list)
        {
            lock (RuntimeData.Mutex)
            {
                RuntimeData.OwnerHistList = list;
                ReplaceLangHunLingYuNpc();           
            }
        }

        private void BroadcastBangHuiCityData(HashSet<long> newBangHuiIdHashSet, long oldBhid, long newBhid)
        {
            int count = GameManager.ClientMgr.GetMaxClientCount();
            for (int i = 0; i < count; i++)
            {
                GameClient client = GameManager.ClientMgr.FindClientByNid(i);
                if (null != client)
                {
                    if (client.ClientData.Faction > 0 && newBangHuiIdHashSet.Contains(client.ClientData.Faction))
                    {
                        ProcessLangHunLingYuRoleDataCmd(client, (int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_DATA, null, null);
                    }

                    if (oldBhid != newBhid)
                    {
                        UpdateChengHaoBuffer(client, oldBhid, newBhid);
                    }
                }
            }
        }

        private void OnInitGame(GameClient client)
        {
            UpdateChengHaoBuffer(client, 0, RuntimeData.ChengHaoBHid);
        }

        private void UpdateChengHaoBuffer(GameClient client, long oldBhid, long newBhid)
        {
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.LangHunLingYu_ChengHao);
            if (newBhid > 0 && client.ClientData.Faction == newBhid)
            {
                if (null == bufferData || Global.IsBufferDataOver(bufferData))
                {
                    double[] bufferParams = new double[1] { 1 };
                    Global.UpdateBufferData(client, BufferItemTypes.LangHunLingYu_ChengHao, bufferParams, 1, true);
                }
            }
            else
            {
                if (bufferData != null && !Global.IsBufferDataOver(bufferData))
                {
                    double[] bufferParams = new double[1] { 0 };
                    Global.UpdateBufferData(client, BufferItemTypes.LangHunLingYu_ChengHao, bufferParams, 1, true);
                }
            }
        }

        public bool CanGetAwardsByEnterTime(GameClient client)
        {
            int secs = DataHelper.UnixSecondsNow() - Global.GetRoleParamsInt32FromDB(client, RoleParamName.EnterBangHuiUnixSecs);
            if (secs >= GameManager.systemParamsList.GetParamValueIntByName("JiaRuTime", 0) * 60 * 60)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void CheckTipsIconState(GameClient client)
        {
            int awardFlags = 0;
            bool canGetAwards = false;
            bool canFight = false;
            lock (RuntimeData.Mutex)
            {
                canGetAwards = CanGetAwardsByEnterTime(client);
                if (canGetAwards)
                {
                    LangHunLingYuBangHuiData langHunLingYuBangHuiData;
                    if (RuntimeData.BangHuiDataDict.TryGetValue(client.ClientData.Faction, out langHunLingYuBangHuiData))
                    {
                        awardFlags = langHunLingYuBangHuiData.DayAwardFlags;
                    }

                    int nowDayId = Global.GetOffsetDayNow();
                    int lastDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LangHunLingYuDayAwardsDay);
                    int flags = 0;
                    if (lastDayID == nowDayId)
                    {
                        flags = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LangHunLingYuDayAwardsFlags);
                    }

                    int flags2 = (awardFlags ^ flags) & awardFlags;
                    if (flags2 == 0)
                    {
                        canGetAwards = false;
                    }
                }

                LangHunLingYuBangHuiData bangHuiData;
                if (RuntimeData.BangHuiDataDict.TryGetValue(client.ClientData.Faction, out bangHuiData))
                {
                    if (bangHuiData.SelfCityList.Count > 0)
                    {
                        canFight = true;
                    }
                }
            }

            client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.LangHunLingYuIcon, canGetAwards);
            client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.LangHunLingYuFightIcon, canFight);
            client._IconStateMgr.SendIconStateToClient(client);
        }

        /// <summary>
        /// 圣域争霸攻防竞价申请指令处理
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessLangHunLingYuJoinCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success_No_Info;

                do
                {
                    int bhid = client.ClientData.Faction;
                    if (bhid <= 0)
                    {
                        result = StdErrorCode.Error_ZhanMeng_Not_In_ZhanMeng; //非报名时间
                        break;
                    }

                    if (client.ClientData.BHZhiWu != (int)ZhanMengZhiWus.ShouLing)
                    {
                        result = StdErrorCode.Error_ZhanMeng_ShouLing_Only; 
                        break;
                    }

                    if (!IsGongNengOpened(client, true))
                    {
                        result = StdErrorCode.Error_Type_Not_Match;
                        break;
                    }

                    BangHuiMiniData bangHuiMiniData = Global.GetBangHuiMiniData(bhid);
                    if (null == bangHuiMiniData)
                    {
                        result = StdErrorCode.Error_ZhanMeng_Not_Exist; 
                        break;
                    }

                    int bhZoneID = 0;
                    CityLevelInfo sceneItem = null;
                    LangHunLingYuGameStates state = LangHunLingYuGameStates.None;
                    int cityLevel = GetBangHuiCityLevel(bhid) + 1;
                    if (cityLevel > Consts.MaxLangHunCityLevel)
                    {
                        result = StdErrorCode.Error_Reach_Max_Level; //已达最高等级
                        break;
                    }

                    result = CheckSignUpCondition(cityLevel, ref sceneItem, ref state);
                    if (state != LangHunLingYuGameStates.SignUp)
                    {
                        result = StdErrorCode.Error_Not_In_valid_Time; //非报名时间
                        break;
                    }

                    LangHunLingYuBangHuiData bangHuiData = null;
                    lock (RuntimeData.Mutex)
                    {
                        if (RuntimeData.BangHuiDataDict.TryGetValue(bhid, out bangHuiData))
                        {
                            if (null != bangHuiData.SelfCityList)
                            {
                                foreach (var cityID in bangHuiData.SelfCityList)
                                {
                                    LangHunLingYuCityDataEx cityDataEx;
                                    if (RuntimeData.CityDataExDict.TryGetValue(cityID, out cityDataEx) && cityDataEx.CityLevel >= cityLevel)
                                    {
                                        result = StdErrorCode.Error_ZhanMeng_Has_Bid_OtherSite;
                                        break;
                                    }
                                }
                            }
                            if (bangHuiData.SignUpTime > TimeUtil.NOW() - 10 * 1000)
                            {
                                result = StdErrorCode.Error_Operate_Too_Fast;
                                break;
                            }
                        }
                        else
                        {
                            bangHuiData = new LangHunLingYuBangHuiData();
                            RuntimeData.BangHuiDataDict.Add(bhid, bangHuiData);
                        }

                        //扣除所需的竞标资金
                        if (!GameManager.ClientMgr.SubBangHuiTongQian(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                            Global._TCPManager.TcpOutPacketPool, client, sceneItem.ZhanMengZiJin, out bhZoneID))
                        {
                            result = StdErrorCode.Error_JinBi_Not_Enough;
                            break;
                        }

                        bangHuiData.SignUpTime = TimeUtil.NOW();
                        result = YongZheZhanChangClient.getInstance().LangHunLingYuSignUp(bangHuiMiniData.BHName, bangHuiMiniData.BHID, bangHuiMiniData.ZoneID, (int)GameTypes.LangHunLingYu, 1, 0);
                        if (result >= 0)
                        {
                            EventLogManager.AddRoleEvent(client, OpTypes.Join, OpTags.LangHunLingYu, LogRecordType.IntValue2, bangHuiMiniData.BHID, sceneItem.ZhanMengZiJin);
                            //string broadCastMsg;
                            //全服广播消息
                            //broadCastMsg = StringUtil.substitute(Global.GetLang("【{0}】通过竞标获得了圣域争霸活动的进攻名额！"), GetBHName(bhid));
                            //Global.BroadcastRoleActionMsg(null, RoleActionsMsgTypes.Bulletin, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                        }
                        else
                        {
                            //返还所需的资金
                            GameManager.ClientMgr.AddBangHuiTongQian(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                                Global._TCPManager.TcpOutPacketPool, client, bhid, sceneItem.ZhanMengZiJin);
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

        public bool ProcessLangHunLingYuEnterCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                CityLevelInfo sceneItem = null;
                LangHunLingYuGameStates state = LangHunLingYuGameStates.None;
                int result = StdErrorCode.Error_Success_No_Info;
                int roleId = client.ClientData.RoleID;
                int cityId = Global.SafeConvertToInt32(cmdParams[1]);
                int bhid = client.ClientData.Faction;

                do
                {
                    if (cityId < 1 || cityId > Consts.LangHunLingYuMaxCityID)
                    {
                        result = StdErrorCode.Error_Invalid_Params;
                        break;
                    }

                    int uniolLevel = Global.GetUnionLevel(client);
                    if (uniolLevel < Global.GetUnionLevel(RuntimeData.MinZhuanSheng, RuntimeData.MinLevel))
                    {
                        result = StdErrorCode.Error_Level_Limit;
                        break;
                    }

                    if (!IsGongNengOpened(client, true))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }

                    if (bhid <= 0)
                    {
                        result = StdErrorCode.Error_ZhanMeng_Not_In_ZhanMeng;
                        break;
                    }

                    bool canEnter = false;
                    bool gameOver = true;
                    LangHunLingYuBangHuiData langHunLingYuBangHuiData = null;
                    LangHunLingYuCityDataEx cityData;
                    lock (RuntimeData.Mutex)
                    {
                        if (RuntimeData.CityDataExDict.TryGetValue(cityId, out cityData))
                        {
                            for (int i = 0; i < cityData.Site.Length; i++ )
                            {
                                long id = cityData.Site[i];
                                if (id == bhid)
                                {
                                    canEnter = true;
                                }
                                if (i > 0 && id > 0)
                                {
                                    gameOver = false;
                                }
                            }
                        }
                    }

                    if (!canEnter)
                    {
                        result = StdErrorCode.Error_ZhanMeng_Is_Unqualified;
                        break;
                    }

                    if (gameOver)
                    {
                        result = StdErrorCode.Error_Game_Over;
                        break;
                    }

                    if (!CheckMap(client))
                    {
                        result = StdErrorCode.Error_Denied_In_Current_Map;
                        break;
                    }
                    else
                    {
                        result = CheckFightCondition(GetCityLevelById(cityId), ref sceneItem, ref state);
                    }

                    if (result >= 0 && state == LangHunLingYuGameStates.Start)
                    {
                        KuaFuServerLoginData kuaFuServerLoginData = null;
                        lock (RuntimeData.Mutex)
                        {
                            KuaFuServerLoginData clientKuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);
                            if (null != clientKuaFuServerLoginData)
                            {
                                if (YongZheZhanChangClient.getInstance().LangHunLingYuKuaFuLoginData(roleId, cityId, cityData.GameId, clientKuaFuServerLoginData))
                                {
                                    result = StdErrorCode.Error_Success_No_Info;
                                }
                                else
                                {
                                    result = StdErrorCode.Error_Server_Busy;
                                }
                            }
                        }

                        if (result >= 0)
                        {
                            if (result >= 0)
                            {
                                GlobalNew.RecordSwitchKuaFuServerLog(client);
                                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client));
                                EventLogManager.AddRoleEvent(client, OpTypes.Enter, OpTags.LangHunLingYu, LogRecordType.IntValue, cityId);
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
                } while (false);

                client.sendCmd(nID, result);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return true;
        }

        public bool ProcessLangHunLingYuRoleDataCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                LangHunLingYuGameStates state = LangHunLingYuGameStates.None;
                int result = StdErrorCode.Error_Success_No_Info;
                int roleId = client.ClientData.RoleID;
                int bhid = client.ClientData.Faction;
                LangHunLingYuRoleData roleData = new LangHunLingYuRoleData();

                do
                {
                    if (bhid <= 0)
                    {
                        result = StdErrorCode.Error_ZhanMeng_Not_In_ZhanMeng;
                        break;
                    }

                    LangHunLingYuCityData cityData;
                    LangHunLingYuBangHuiData bangHuiData = null;
                    lock (RuntimeData.Mutex)
                    {
                        if (RuntimeData.BangHuiDataDict.TryGetValue(bhid, out bangHuiData))
                        {
                            int maxLevel = 0;
                            bool findTopLevelCityId = false;
                            roleData.SignUpState = bangHuiData.SignUpState;
                            foreach (var id in bangHuiData.SelfCityList)
                            {
                                if (id > 0 && RuntimeData.CityDataDict.TryGetValue(id, out cityData))
                                {
                                    if (id == Consts.LangHunLingYuMinCityID) findTopLevelCityId = true;
                                    roleData.SelfCityList.Add(cityData);
                                    if (cityData.Owner != null && cityData.Owner.BHID == bhid && cityData.CityLevel > maxLevel)
                                    {
                                        maxLevel = cityData.CityLevel;
                                    }
                                }
                            }
                            //foreach (var kv in RuntimeData.OtherCityList)
                            //{
                            //    if (maxLevel >= kv.Key)
                            //    {
                            //        foreach (var cityId in kv.Value)
                            //        {
                            //            if (RuntimeData.CityDataDict.TryGetValue(cityId, out cityData) && (cityData.Owner == null || cityData.Owner.BHID != bhid))
                            //            {
                            //                roleData.OtherCityList.Add(cityData);
                            //                break;
                            //            }
                            //        }
                            //    }
                            //}
                            if (!findTopLevelCityId)
                            {
                                if (RuntimeData.CityDataDict.TryGetValue(Consts.LangHunLingYuMinCityID, out cityData))
                                {
                                    roleData.SelfCityList.Insert(0, cityData);
                                }
                            }
                        }
                        else
                        {
                            if (RuntimeData.CityDataDict.TryGetValue(Consts.LangHunLingYuMinCityID, out cityData))
                            {
                                roleData.SelfCityList.Insert(0, cityData);
                            }
                            break;
                        }
                    }

                    int lastDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LangHunLingYuDayAwardsDay);
                    int flags = 0;
                    int nowDayId = Global.GetOffsetDayNow();
                    if (lastDayID == nowDayId)
                    {
                        flags = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LangHunLingYuDayAwardsFlags);
                    }

                    List<int> levelList = new List<int>();
                    lock (RuntimeData.Mutex)
                    {
                        if (null != bangHuiData.SelfCityList)
                        {
                            LangHunLingYuCityDataEx cityDataEx;
                            foreach (var id in bangHuiData.SelfCityList)
                            {
                                if (RuntimeData.CityDataExDict.TryGetValue(id, out cityDataEx))
                                {
                                    if (cityDataEx.Site[0] == bhid)
                                    {
                                        if (0 == Global.GetIntSomeBit(flags, cityDataEx.CityLevel))
                                        {
                                            levelList.Add(cityDataEx.CityLevel);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    roleData.GetDayAwardsState = new List<int>(levelList);
                } while (false);

                client.sendCmd(nID, roleData);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return true;
        }

        public bool ProcessLangHunLingYuCityDataCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                LangHunLingYuSceneInfo sceneItem = null;
                LangHunLingYuGameStates state = LangHunLingYuGameStates.None;
                int roleId = client.ClientData.RoleID;
                int bhid = client.ClientData.Faction;
                int cityId = Global.SafeConvertToInt32(cmdParams[1]);
                LangHunLingYuCityData cityData = null;

                if (bhid > 0 && cityId >= 1 || cityId <= Consts.LangHunLingYuMaxCityID)
                {
                    lock (RuntimeData.Mutex)
                    {
                        if (RuntimeData.CityDataDict.TryGetValue(bhid, out cityData))
                        {

                        }
                    }
                }

                client.sendCmd(nID, cityData);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return true;
        }

        /// <summary>
        /// 获取当前圣域城主膜拜数据
        /// </summary>
        public bool ProcessGetAdmireDataCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int roleID = Convert.ToInt32(cmdParams[0]);

                // 构造一个
                LangHunLingYuKingShowData showData = new LangHunLingYuKingShowData();
                showData.AdmireCount = Global.GetLHLYAdmireCount(client);
                showData.RoleData4Selector = Global.RoleDataEx2RoleData4Selector(OwnerRoleData);
                client.sendCmd(nID, showData);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
            return true;
        }

        /// <summary>
        /// 获取历届圣域城主膜拜信息
        /// </summary>
        public bool ProcessGetAdmireHistoryCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int roleID = Convert.ToInt32(cmdParams[0]);

                // 构造一个
                List<LangHunLingYuKingShowDataHist> showDataList = new List<LangHunLingYuKingShowDataHist>();
                lock (RuntimeData.Mutex)
                {
                    // 历届城主数据
                    if(RuntimeData.OwnerHistList != null && RuntimeData.OwnerHistList.Count > 1)
                    {
                        // index == RuntimeData.OwnerHistList.Count - 1 可能是当前城主
                        for (int index = RuntimeData.OwnerHistList.Count - 1; index >= 0; --index)
                        {
                            LangHunLingYuKingHist histData = RuntimeData.OwnerHistList[index];
                            if (null == histData.CityOwnerRoleData)
                                continue;

                            RoleDataEx rd = DataHelper.BytesToObject<RoleDataEx>(histData.CityOwnerRoleData, 0, histData.CityOwnerRoleData.Length);
                            if (null == rd)
                                continue;

                            // 当前圣域城主不显示在历届里
                            if (index == RuntimeData.OwnerHistList.Count - 1 && RuntimeData.ChengHaoBHid != 0)
                                continue;

                            LangHunLingYuKingShowDataHist data = new LangHunLingYuKingShowDataHist();
                            data.AdmireCount = histData.AdmireCount;
                            data.CompleteTime = histData.CompleteTime;
                            data.RoleData4Selector = Global.RoleDataEx2RoleData4Selector(rd);
                            data.BHName = rd.BHName;

                            showDataList.Add(data);

                            // 达到显示上限
                            if (showDataList.Count == Consts.LangHunLingYuAdmireHistCount - 1)
                                break;
                        }
                    }

                    // 返回
                    client.sendCmd(nID, showDataList);
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return true;
        }

        /// <summary>
        /// 膜拜
        /// </summary>
        public bool ProcessAdmireCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int roleID = Convert.ToInt32(cmdParams[0]);
                int type = Convert.ToInt32(cmdParams[1]);

                string strcmd = "";
                MoBaiData MoBaiConfig = null;
                if(!Data.MoBaiDataInfoList.TryGetValue((int)MoBaiTypes.LangHunLingYun, out MoBaiConfig))
                {
                    strcmd = string.Format("{0}", -2);                                  // 配置出错
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                if(client.ClientData.ChangeLifeCount < MoBaiConfig.MinZhuanSheng ||
                    (client.ClientData.ChangeLifeCount == MoBaiConfig.MinZhuanSheng && client.ClientData.Level < MoBaiConfig.MinLevel))
                {
                    strcmd = string.Format("{0}", -2);                                  // 配置出错
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                int nRealyNum = MoBaiConfig.AdrationMaxLimit;
                int AdmireCount = Global.GetLHLYAdmireCount(client);
                if(null != OwnerRoleData && client.ClientData.RoleID == OwnerRoleData.RoleID)
                {
                    // 玩家是圣域城主有额外次数
                    nRealyNum += MoBaiConfig.ExtraNumber;
                }

                // 玩家是VIP 有额外的次数
                int nVIPLev = client.ClientData.VipLevel;

                int[] nArrayVIPAdded = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPMoBaiNum");

                if (nVIPLev > (int)VIPEumValue.VIPENUMVALUE_MAXLEVEL || (nArrayVIPAdded.Length > 13 || nArrayVIPAdded.Length < 1))
                {
                    strcmd = string.Format("{0}", -2);                                  // 配置出错
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                nRealyNum += nArrayVIPAdded[nVIPLev];

                // 节日活动多倍
                JieRiMultAwardActivity activity = HuodongCachingMgr.GetJieRiMultAwardActivity();
                if (null != activity)
                {
                    JieRiMultConfig config = activity.GetConfig((int)MultActivityType.DiaoXiangCount);
                    if (null != config)
                    {
                        nRealyNum = nRealyNum * ((int)config.GetMult() + 1)/*做倍数处理的时候减了1*/;
                    }
                }

                // 膜拜次数达到上限
                if(AdmireCount >= nRealyNum)
                {
                    strcmd = string.Format("{0}", -3);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // double nRate = Math.Min(400.0d, (400.4d * client.ClientData.ChangeLifeCount + client.ClientData.Level));
                double nRate = 0;
                if (client.ClientData.ChangeLifeCount == 0)
                    nRate = 1;
                else
                    nRate = Data.ChangeLifeEverydayExpRate[client.ClientData.ChangeLifeCount];
                
                // 金币膜拜
                if (type == 1)
                {
                    // 金币不够
                    if (!Global.SubBindTongQianAndTongQian(client, MoBaiConfig.NeedJinBi, "膜拜圣域城主"))
                    {
                        strcmd = string.Format("{0}", -4);
                        client.sendCmd(nID, strcmd);
                        return true;
                    }

                    // 配置值*转生倍率
                    int nExp = (int)(nRate * MoBaiConfig.JinBiExpAward);
                    if (nExp > 0)
                        GameManager.ClientMgr.ProcessRoleExperience(client, nExp, true);

                    // 战功
                    if (MoBaiConfig.JinBiZhanGongAward > 0)
                    {
                        GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                            client, ref MoBaiConfig.JinBiZhanGongAward, AddBangGongTypes.LHLYMoBai);
                    }

                    if (MoBaiConfig.LingJingAwardByJinBi > 0)
                    {
                        GameManager.ClientMgr.ModifyMUMoHeValue(client, MoBaiConfig.LingJingAwardByJinBi, "膜拜圣域城主", true, true);
                    }
                }
                // 钻石膜拜
                else if (type == 2)
                {
                    // 钻石不够
                    if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, MoBaiConfig.NeedZuanShi, "膜拜圣域城主"))
                        {
                            strcmd = string.Format("{0}", -5);
                            client.sendCmd(nID, strcmd);
                            return true;
                        }

                    // 配置值*转生倍率
                    int nExp = (int)(nRate * MoBaiConfig.ZuanShiExpAward);
                    if (nExp > 0)
                        GameManager.ClientMgr.ProcessRoleExperience(client, nExp, true);

                    // 战功
                    if (MoBaiConfig.ZuanShiZhanGongAward > 0)
                    {
                        GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                            client, ref MoBaiConfig.ZuanShiZhanGongAward, AddBangGongTypes.LHLYMoBai);
                    }
                    if (MoBaiConfig.LingJingAwardByZuanShi > 0)
                    {
                        GameManager.ClientMgr.ModifyMUMoHeValue(client, MoBaiConfig.LingJingAwardByZuanShi, "膜拜圣域城主", true, true);
                    }
                }

                // 膜拜
                if (null != OwnerRoleData)
                    YongZheZhanChangClient.getInstance().LangHunLingYunAdmire(OwnerRoleData.RoleID);
                
                Global.ProcessIncreaseLHLYAdmireCount(client);

                // 1.成功
                strcmd = string.Format("{0}", 1);
                client.sendCmd(nID, strcmd);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return true;
        }

        public bool ProcessLangHunLingYuWorldDataCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                LangHunLingYuGameStates state = LangHunLingYuGameStates.None;
                int roleId = client.ClientData.RoleID;
                int bhid = client.ClientData.Faction;
                LangHunLingYuWorldData worldData = new LangHunLingYuWorldData();

                if (bhid > 0)
                {
                    LangHunLingYuCityData cityData;
                    LangHunLingYuBangHuiData bangHuiData = null;
                    lock (RuntimeData.Mutex)
                    {
                        for (int i = 1; i <= Consts.LangHunLingYuMaxWorldCityNum; i++)
                        {
                            if (RuntimeData.CityDataDict.TryGetValue(i, out cityData))
                            {
                                worldData.CityList.Add(cityData);
                            }
                        }
                    }
                }

                client.sendCmd(nID, worldData);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return true;
        }

        /// <summary>
        /// 领取每日奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessGetDailyAwardsCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success;

                do 
                {
                    int roleID = Convert.ToInt32(cmdParams[0]);
                    int bhid = client.ClientData.Faction;

                    if (bhid <= 0 || client.ClientData.Faction != bhid)
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }

                    if (!CanGetAwardsByEnterTime(client))
                    {
                        result = StdErrorCode.Error_In_ZhanMeng_Time_Not_Enough;
                        break;
                    }

                    int lastDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LangHunLingYuDayAwardsDay);
                    int flags = 0;
                    int nowDayId = Global.GetOffsetDayNow();
                    if (lastDayID == nowDayId)
                    {
                        flags = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LangHunLingYuDayAwardsFlags);
                    }

                    List<int> levelList = new List<int>();
                    lock (RuntimeData.Mutex)
                    {
                        LangHunLingYuBangHuiData bangHuiData;
                        if (!RuntimeData.BangHuiDataDict.TryGetValue(bhid, out bangHuiData))
                        {
                            result = StdErrorCode.Error_Not_Exist;
                            break;
                        }

                        if (null != bangHuiData.SelfCityList)
                        {
                            LangHunLingYuCityDataEx cityDataEx;
                            foreach (var id in bangHuiData.SelfCityList)
                            {
                                if (RuntimeData.CityDataExDict.TryGetValue(id, out cityDataEx) && cityDataEx.Site[0] == bhid)
                                {
                                    if (0 == Global.GetIntSomeBit(flags, cityDataEx.CityLevel))
                                    {
                                        levelList.Add(cityDataEx.CityLevel);
                                    }
                                }
                            }
                        }
                    }

                    bool getSomeAwards = false;
                    foreach (var level in levelList)
                    {
                        CityLevelInfo awardsItem;
                        if (!RuntimeData.CityLevelInfoDict.TryGetValue(level, out awardsItem))
                        {
                            LogManager.WriteLog(LogTypes.Error, "城池等级每日奖励未配置：Level=" + level);
                            continue;
                        }

                        List<GoodsData> goodsDataList = Global.ConvertToGoodsDataList(awardsItem.DayAward.Items);
                        if (Global.CanAddGoodsDataList(client, goodsDataList))
                        {
                            for (int i = 0; i < goodsDataList.Count; i++)
                            {
                                //向DBServer请求加入某个新的物品到背包中
                                Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsDataList[i].GoodsID, goodsDataList[i].GCount, goodsDataList[i].Quality, "",
                                                            goodsDataList[i].Forge_level, goodsDataList[i].Binding, 0, "", true, 1, /**/"圣域争霸胜利战盟每日奖励", Global.ConstGoodsEndTime,
                                                            0, goodsDataList[i].BornIndex, goodsDataList[i].Lucky, 0, goodsDataList[i].ExcellenceInfo, goodsDataList[i].AppendPropLev);

                                GoodsData goodsData = goodsDataList[i];
                                GameManager.logDBCmdMgr.AddDBLogInfo(goodsData.Id, Global.ModifyGoodsLogName(goodsData), "圣域争霸胜利战盟每日奖励", Global.GetMapName(client.ClientData.MapCode), client.ClientData.RoleName, "增加", goodsData.GCount, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId);
                            }

                            //设置领取标识
                            flags = Global.SetIntSomeBit(level, flags, true);
                            getSomeAwards = true; //标记至少为获得了部分奖励
                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LangHunLingYuDayAwardsDay, Global.GetOffsetDayNow(), true);
                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LangHunLingYuDayAwardsFlags, flags, true);
                            EventLogManager.AddRoleEvent(client, OpTypes.GiveAwards, OpTags.LangHunLingYuDailyAwards, LogRecordType.IntValue, level);
                        }
                        else
                        {
                            result = StdErrorCode.Error_BagNum_Not_Enough;
                        }
                    }

                    if (getSomeAwards)
                    {
                        CheckTipsIconState(client);
                    }
                } while (false);

                client.sendCmd(nID, string.Format("{0}", result));
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
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
        private int CheckSignUpCondition(int cityLevel, ref CityLevelInfo sceneItem, ref LangHunLingYuGameStates state)
        {
            int result = 0;

            do
            {
                cityLevel = Math.Max(cityLevel, 1);
                lock (RuntimeData.Mutex)
                {
                    if (!RuntimeData.CityLevelInfoDict.TryGetValue(cityLevel, out sceneItem))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }
                }

                result = StdErrorCode.Error_Not_In_valid_Time;
                DateTime now = TimeUtil.NowDateTime();
                lock (RuntimeData.Mutex)
                {
                    for (int i = 0; i < sceneItem.BaoMingTime.Count - 1; i += 2)
                    {
                        TimeSpan ts = now.TimeOfDay.Add(TimeSpan.FromDays((int)now.DayOfWeek));
                        if (ts >= sceneItem.BaoMingTime[i] && ts <= sceneItem.BaoMingTime[i + 1])
                        {
                            state = LangHunLingYuGameStates.SignUp;
                            result = StdErrorCode.Error_Success;
                            break;
                        }
                    }
                }
            } while (false);

            return result;
        }

        // 检查勇者战场当前处于什么时间状态
        private int CheckFightCondition(int cityLevel, ref CityLevelInfo sceneItem, ref LangHunLingYuGameStates state)
        {
            int result = 0;

            do
            {
                cityLevel = Math.Max(cityLevel, 1);
                lock (RuntimeData.Mutex)
                {
                    LangHunLingYuSceneInfo sceneInfo;
                    if (!RuntimeData.CityLevelInfoDict.TryGetValue(cityLevel, out sceneItem))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }
                }

                result = StdErrorCode.Error_Not_In_valid_Time;
                DateTime now = TimeUtil.NowDateTime();
                lock (RuntimeData.Mutex)
                {
                    if (sceneItem.AttackWeekDay.Contains((int)now.DayOfWeek))
                    {
                        for (int i = 0; i < sceneItem.AttackTime.Count - 1; i += 2)
                        {
                            if (now.TimeOfDay >= sceneItem.AttackTime[i] &&
                                now.TimeOfDay <= sceneItem.AttackTime[i + 1])
                            {
                                state = LangHunLingYuGameStates.Start;
                                result = StdErrorCode.Error_Success_No_Info;
                                break;
                            }
                        }
                    }
                }
            } while (false);

            return result;
        }

        private TimeSpan GetStartTime(int cityLevel)
        {
            CityLevelInfo sceneItem = null;
            TimeSpan startTime = TimeSpan.MinValue;
            DateTime now = TimeUtil.NowDateTime();

            do
            {
                lock (RuntimeData.Mutex)
                {
                    if (!RuntimeData.CityLevelInfoDict.TryGetValue(cityLevel, out sceneItem))
                    {
                        break;
                    }
                }

                lock (RuntimeData.Mutex)
                {
                    for (int i = 0; i < sceneItem.AttackTime.Count - 1; i += 2)
                    {
                        if (now.TimeOfDay >= sceneItem.AttackTime[i] &&
                            now.TimeOfDay <= sceneItem.AttackTime[i + 1])
                        {
                            startTime = sceneItem.AttackTime[i];
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

#endregion 指令处理

#region 辅助函数

        /// <summary>
        /// 获取帮会名称
        /// </summary>
        /// <param name="warDay"></param>
        /// <param name="bangHuiID"></param>
        /// <param name="dayTime"></param>
        /// <param name="bangHuiName"></param>
        /// <returns></returns>
        private string GetBHName(int bangHuiID)
        {
            BangHuiMiniData bhData = Global.GetBangHuiMiniData(bangHuiID);
            if (null != bhData)
            {
                return bhData.BHName;//Global.FormatBangHuiName(bhData.ZoneID, bhData.BHName);
            }

            return Global.GetLang("无");
        }

#endregion 辅助函数

#region 定时给在场的玩家家经验

        /// <summary>
        /// 定时给在场的玩家增加经验
        /// </summary>
        private void ProcessTimeAddRoleExp(LangHunLingYuScene scene)
        {
            long ticks = TimeUtil.NOW();
            if (ticks - scene.LastAddBangZhanAwardsTicks < (10 * 1000))
            {
                return;
            }

            scene.LastAddBangZhanAwardsTicks = ticks;

            //刷新旗帜Buff拥有者信息
            NotifyQiZhiBuffOwnerDataList(scene);
            NotifyLongTaRoleDataList(scene);
            NotifyLongTaOwnerData(scene);

            foreach (var copyMap in scene.CopyMapDict.Values)
            {
                List<GameClient> list = copyMap.GetClientsList();
                foreach (var client in list)
                {
                    if (null != client)
                    {
                        // 处理用户的经验奖励
                        _LevelAwardsMgr.ProcessBangZhanAwards(client);
                    }
                }
            }
        }

#endregion 定时给在场的玩家家经验

#region 复活与地图传送

        /// <summary>
        /// 获取出生点或复活点位置
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toLongTa"></param>
        /// <param name="mapCode"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <returns></returns>
        public bool GetZhanMengBirthPoint(LangHunLingYuSceneInfo sceneInfo, GameClient client, int toMapCode, out int mapCode, out int posX, out int posY)
        {
            mapCode = sceneInfo.MapCode;
            posX = -1;
            posY = -1;
            int bhid = client.ClientData.Faction;
            lock (RuntimeData.Mutex)
            {
                int site = client.ClientData.BattleWhichSide - 1;
                int round = 0;
                if (sceneInfo.MapCode_LongTa == toMapCode)
                {
                    do 
                    {
                        Point pt = Global.GetRandomPoint(ObjectTypes.OT_CLIENT, sceneInfo.MapCode_LongTa);
                        if (!Global.InObs(ObjectTypes.OT_CLIENT, sceneInfo.MapCode_LongTa, (int)pt.X, (int)pt.Y))
                        {
                            mapCode = sceneInfo.MapCode_LongTa;
                            posX = (int)pt.X;
                            posY = (int)pt.Y;

                            return true;
                        }
                    } while (round++ < 1000);
                }

                //特殊复活点,拥有指定旗帜后有效
                round = 0;
                LangHunLingYuScene scene = client.SceneObject as LangHunLingYuScene;
                if (scene != null && client.ClientData.Faction == scene.SuperQiZhiOwnerBhid && toMapCode == sceneInfo.MapCode)
                {
                    do 
                    {
                        mapCode = toMapCode;
                        posX = Global.GetRandomNumber(RuntimeData.SuperQiZhiOwnerBirthPosX - 400, RuntimeData.SuperQiZhiOwnerBirthPosX + 400);
                        posY = Global.GetRandomNumber(RuntimeData.SuperQiZhiOwnerBirthPosY - 400, RuntimeData.SuperQiZhiOwnerBirthPosY + 400);
                        if (!Global.InObs(ObjectTypes.OT_CLIENT, toMapCode, (int)posX, (int)posY))
                        {
                            return true;
                        }
                    } while (round++ < 100);
                }

                List<MapBirthPoint> list;
                if (!RuntimeData.MapBirthPointListDict.TryGetValue(site, out list) || list.Count == 0)
                {
                    return true;
                }

                round = 0;
                do 
                {
                    int rnd = Global.GetRandomNumber(0, list.Count);
                    MapBirthPoint mapBirthPoint = list[rnd];
                    posX = mapBirthPoint.BirthPosX + Global.GetRandomNumber(-mapBirthPoint.BirthRangeX, mapBirthPoint.BirthRangeX);
                    posY = mapBirthPoint.BirthPosY + Global.GetRandomNumber(-mapBirthPoint.BirthRangeY, mapBirthPoint.BirthRangeY);
                    if (!Global.InObs(ObjectTypes.OT_CLIENT, mapCode, posX, posY))
                    {
                        return true;
                    }
                } while (round++ < 1000);
            }

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
            int mapCode = client.ClientData.MapCode;
            int toMapCode, toPosX, toPosY;
            LangHunLingYuSceneInfo sceneInfo = client.SceneInfoObject as LangHunLingYuSceneInfo;
            if (null != sceneInfo)
            {
                toMapCode = mapCode = sceneInfo.MapCode;
                if (GetZhanMengBirthPoint(sceneInfo, client, mapCode, out toMapCode, out toPosX, out toPosY))
                {
                    client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                    client.ClientData.CurrentMagicV = client.ClientData.MagicV;

                    client.ClientData.MoveAndActionNum = 0;

                    //通知队友自己要复活
                    GameManager.ClientMgr.NotifyTeamRealive(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.ClientData.RoleID, toPosX, toPosY, -1);

                    //马上通知切换地图---->这个函数每次调用前，如果地图未发生发变化，则直接通知其他人自己位置变动
                    //比如在扬州城死 回 扬州城复活，就是位置变化
                    if (toMapCode != client.ClientData.MapCode)
                    {
                        //通知自己要复活
                        GameManager.ClientMgr.NotifyMySelfRealive(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, client.ClientData.RoleID, client.ClientData.PosX, client.ClientData.PosY, -1);
                        client.ClientData.KuaFuChangeMapCode = toMapCode;
                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, toMapCode, toPosX, toPosY, -1, 1);
                    }
                    else
                    {
                        Global.ClientRealive(client, toPosX, toPosY, -1);
                    }

                    return true;
                }
            }

            return false;
        }

        public bool ClientChangeMap(GameClient client, ref int toNewMapCode, ref int toNewPosX, ref int toNewPosY)
        {
            LangHunLingYuSceneInfo sceneInfo = client.SceneInfoObject as LangHunLingYuSceneInfo;
            if (null != sceneInfo)
            {
                if (toNewMapCode == sceneInfo.MapCode || toNewMapCode == sceneInfo.MapCode_LongTa)
                {
                    int toMapCode, toPosX, toPosY;
                    if (client.ClientData.MapCode == sceneInfo.MapCode_LongTa)
                    {
                        //从龙塔通过传送点传到罗兰峡谷地图,不作特殊处理,按传送点配置传送
                    }
                    else if (GetZhanMengBirthPoint(sceneInfo, client, toNewMapCode, out toMapCode, out toPosX, out toPosY))
                    {
                        toNewMapCode = toMapCode;
                        toNewPosX = toPosX;
                        toNewPosY = toPosY;
                    }
                }
            }

            return true;
        }

#endregion 复活与地图传送

#region 军旗和BUFF

        /// <summary>
        /// 处理玩家点击NPC事件
        /// 如果是旗座,尝试安装帮旗
        /// </summary>
        /// <param name="client"></param>
        /// <param name="npcID"></param>
        /// <returns>是否是旗座NPC</returns>
        public bool OnSpriteClickOnNpc(GameClient client, int npcID, int npcExtentionID)
        {
            bool isQiZuo = false;
            LangHunLingYuScene scene = client.SceneObject as LangHunLingYuScene;
            CopyMap copyMap;
            if (null != scene && scene.CopyMapDict.TryGetValue(client.ClientData.MapCode, out copyMap))
            {
                do 
                {
                    lock (RuntimeData.Mutex)
                    {
                        QiZhiConfig item;
                        if (scene.NPCID2QiZhiConfigDict.TryGetValue(npcExtentionID, out item))
                        {
                            isQiZuo = true;
                            if (item.Alive) return isQiZuo;
                            if (item.KillerBhid > 0 && client.ClientData.Faction != item.KillerBhid && Math.Abs(TimeUtil.NOW() - item.DeadTicks) < 10 * 1000)
                            {
                                break;
                            }

                            //判断是否在帮会中，否则不允许安插帮旗
                            if (client.ClientData.Faction <= 0)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, StringUtil.substitute(Global.GetLang("只有战盟成员才能安插帮旗!")),
                                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                                break;
                            }

                            //在1000*1000的距离内才可以
                            if (Math.Abs(client.ClientData.PosX - item.PosX) <= 1000 && Math.Abs(client.ClientData.PosY - item.PosY) <= 1000)
                            {
                                item.Alive = true;
                                int zoneId;
                                item.InstallBhName = GetBangHuiName(client.ClientData.Faction, out zoneId);
                                item.BattleWhichSide = client.ClientData.BattleWhichSide;

                                CreateMonster(copyMap, item, item.MonsterId);

                                //通知地图变动信息
                                UpdateQiZhiBangHui(scene, npcExtentionID, client.ClientData.Faction, client.ClientData.BHName, zoneId);

                                Global.BroadcastBangHuiMsg(-1, client.ClientData.Faction,
                                    StringUtil.substitute(Global.GetLang("本战盟成员【{0}】成功在{1}『{2}』安插了本战盟旗帜，可喜可贺"),
                                    Global.FormatRoleName(client, client.ClientData.RoleName),
                                    Global.GetServerLineName2(),
                                    Global.GetMapName(client.ClientData.MapCode)),
                                    true, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlySysHint);
                            }
                        }
                    }
                } while (false);
            }
            DateTime dt = DateTime.Now;
            dt.AddDays(1);
            return isQiZuo;
        }

        /// <summary>
        /// 军旗死亡时,通知并更新buff
        /// </summary>
        /// <param name="npcID"></param>
        /// <param name="bhid"></param>
        public void OnProcessJunQiDead(GameClient client, Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            if (null != client && RuntimeData.JunQiMonsterHashSet.Contains(monster.XMonsterInfo.MonsterId))
            {
                int zoneId;
                string bhName = GetBangHuiName(client.ClientData.Faction, out zoneId);
                LangHunLingYuScene scene = client.SceneObject as LangHunLingYuScene;
                QiZhiConfig qizhiConfig = monster.Tag as QiZhiConfig;
                if (null != scene && null != qizhiConfig)
                {
                    lock (RuntimeData.Mutex)
                    {
                        qizhiConfig.KillerBhid = client.ClientData.Faction;
                        qizhiConfig.InstallBhName = "";
                        qizhiConfig.InstallBhid = 0;
                        qizhiConfig.DeadTicks = TimeUtil.NOW();
                        qizhiConfig.Alive = false;

                        UpdateQiZhiBangHui(scene, qizhiConfig.NPCID, 0, bhName, 0);
                    }
                }
            }
#else
             if (null != client && RuntimeData.JunQiMonsterHashSet.Contains(monster.MonsterInfo.ExtensionID))
            {
                int zoneId;
                string bhName = GetBangHuiName(client.ClientData.Faction, out zoneId);
                LangHunLingYuScene scene = client.SceneObject as LangHunLingYuScene;
                QiZhiConfig qizhiConfig = monster.Tag as QiZhiConfig;
                if (null != scene && null != qizhiConfig)
                {
                    lock (RuntimeData.Mutex)
                    {
                        qizhiConfig.KillerBhid = client.ClientData.Faction;
                        qizhiConfig.InstallBhName = "";
                        qizhiConfig.InstallBhid = 0;
                        qizhiConfig.DeadTicks = TimeUtil.NOW();
                        qizhiConfig.Alive = false;

                        UpdateQiZhiBangHui(scene, qizhiConfig.NPCID, 0, bhName, 0);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// 设置玩家的旗帜buff
        /// </summary>
        /// <param name="client"></param>
        private void ResetQiZhiBuff(GameClient client)
        {
            LangHunLingYuScene scene = client.SceneObject as LangHunLingYuScene;

            int toMapCode = client.ClientData.MapCode;
            List<int> bufferIDList = new List<int>();
            lock (RuntimeData.Mutex)
            {
                EquipPropItem item = null;
                int bufferID = 0;
                if (scene != null && client.SceneType == (int)ManagerType)
                {
                    for (int i = 0; i < scene.QiZhiBuffOwnerDataList.Count; i++)
                    {
                        bool add = false;
                        QiZhiConfig qiZhiConfig;
                        LangHunLingYuQiZhiBuffOwnerData ownerData = scene.QiZhiBuffOwnerDataList[i];
                        if (RuntimeData.NPCID2QiZhiConfigDict.TryGetValue(ownerData.NPCID, out qiZhiConfig))
                        {
                            bufferID = qiZhiConfig.BufferID;
                            item = GameManager.EquipPropsMgr.FindEquipPropItem(bufferID);
                            if (null != item)
                            {
                                if (ownerData.OwnerBHID == client.ClientData.Faction)
                                {
                                    add = true;
                                }
                            }
                        }

                        UpdateQiZhiBuff4GameClient(client, item, bufferID, add);
                    }
                }
                else
                {
                    foreach (var qiZhiConfig in RuntimeData.NPCID2QiZhiConfigDict.Values)
                    {
                        UpdateQiZhiBuff4GameClient(client, item, qiZhiConfig.BufferID, false);
                    }
                }
            }
        }

        /// <summary>
        /// 地图加载完成并准备开始游戏时
        /// </summary>
        /// <param name="client"></param>
        public void OnStartPlayGame(GameClient client)
        {
            ResetQiZhiBuff(client);
            BroadcastLuoLanChengZhuLoginHint(client);
            LangHunLingYuScene scene = client.SceneObject as LangHunLingYuScene;
            if (null != scene)
            {
                NotifyTimeStateInfoAndScoreInfo(client);
            }
        }

        /// <summary>
        /// 皇帝上线的提示
        /// </summary>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        public void BroadcastLuoLanChengZhuLoginHint(GameClient client)
        {

        }

        private void CreateMonster(CopyMap copyMap, QiZhiConfig qiZhiConfig, int monsterId)
        {
            GameManager.MonsterZoneMgr.AddDynamicMonsters(copyMap.MapCode, monsterId, copyMap.CopyMapID, 1,
                qiZhiConfig.PosX / RuntimeData.MapGridWidth, qiZhiConfig.PosY / RuntimeData.MapGridHeight, 0, 0, SceneUIClasses.LangHunLingYu, qiZhiConfig);
        }

#endregion 军旗和BUFF

#region 战盟事件钩子

        private bool RefuseChangeBangHui(int bhid)
        {
            CityLevelInfo sceneItem = null;
            LangHunLingYuGameStates state = LangHunLingYuGameStates.None;
            CheckFightCondition(1, ref sceneItem, ref state);
            if (state == LangHunLingYuGameStates.Start)
            {
                lock (RuntimeData.Mutex)
                {
                    LangHunLingYuBangHuiData bangHuiData;
                    if (RuntimeData.BangHuiDataDict.TryGetValue(bhid, out bangHuiData))
                    {
                        if (bangHuiData.SelfCityList.Count > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 战盟添加成员事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public bool OnPreBangHuiAddMember(PreBangHuiAddMemberEventObject e)
        {
            if (RefuseChangeBangHui(e.BHID))
            {
                e.Result = false;
                GameManager.ClientMgr.NotifyImportantMsg(e.Player, Global.GetLang("圣域争霸活动中的战盟不能接收新成员!"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 战盟添加删除事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public bool OnPreBangHuiRemoveMember(PreBangHuiRemoveMemberEventObject e)
        {
            if (RefuseChangeBangHui(e.BHID))
            {
                e.Result = false;
                GameManager.ClientMgr.NotifyImportantMsg(e.Player, Global.GetLang("圣域争霸活动中的战盟不能有成员退出战盟!"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }

            return false;
        }

#endregion 战盟事件钩子

#region 圣域城主雕像

        private object OwnerRoleDataMutex = new object();
        private RoleDataEx _OwnerRoleData = null;
        private RoleDataEx OwnerRoleData
        {
            get { lock (OwnerRoleDataMutex) { return _OwnerRoleData; } }
            set { lock (OwnerRoleDataMutex) { _OwnerRoleData = value; } }
        }

        /// <summary>
        /// 替换圣域城主的npc显示
        /// </summary>
        public void ReplaceLangHunLingYuNpc()
        {
            if (RuntimeData.OwnerHistList == null || RuntimeData.OwnerHistList.Count == 0 || RuntimeData.ChengHaoBHid == 0)
            {
                RestoreLangHunLingYuNpc(); // 石头雕像走起
                return;
            }

            LangHunLingYuKingHist OwnerData = RuntimeData.OwnerHistList[RuntimeData.OwnerHistList.Count - 1];
            RoleDataEx rd = DataHelper.BytesToObject<RoleDataEx>(OwnerData.CityOwnerRoleData, 0, OwnerData.CityOwnerRoleData.Length);
            if (rd == null || rd.RoleID <= 0)
            {
                RestoreLangHunLingYuNpc(); // 石头雕像走起
                return;
            }

            OwnerRoleData = rd;

            NPC npc = NPCGeneralManager.FindNPC(GameManager.MainMapCode, 134);
            if (null != npc)
            {
                npc.ShowNpc = false;
                GameManager.ClientMgr.NotifyMySelfDelNPCBy9Grid(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, npc);
                FakeRoleManager.ProcessDelFakeRoleByType(FakeRoleTypes.DiaoXiang3);
                SafeClientData clientData = new SafeClientData();
                clientData.RoleData = rd;
                FakeRoleManager.ProcessNewFakeRole(clientData, npc.MapCode, FakeRoleTypes.DiaoXiang3, 4, (int)npc.CurrentPos.X, (int)npc.CurrentPos.Y, 134);
            }
        }

        /// <summary>
        /// 恢复圣域城主的雕像
        /// </summary>
        public void RestoreLangHunLingYuNpc()
        {
            // 清空
            OwnerRoleData = null;

            NPC npc = NPCGeneralManager.FindNPC(GameManager.MainMapCode, 134);
            if (null != npc)
            {
                npc.ShowNpc = true;
                GameManager.ClientMgr.NotifyMySelfNewNPCBy9Grid(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, npc);
                FakeRoleManager.ProcessDelFakeRoleByType(FakeRoleTypes.DiaoXiang3);
            }
        }

#endregion

#region 其他

        /// <summary>
        /// 填充圣域城主详细信息
        /// </summary>
        public void LangHunLingYuBuildMaxCityOwnerInfo(LangHunLingYuStatisticalData statisticalData, int ServerID)
        {
            if (statisticalData.CityId != Consts.LangHunLingYuMinCityID) // 顶级城
                return;

            BangHuiDetailData bangHuiDetailData = GetBangHuiDetailDataAuto(statisticalData.SiteBhids[0], -1, ServerID);
            if (null == bangHuiDetailData)
                return;

            statisticalData.rid = bangHuiDetailData.BZRoleID;
            RoleDataEx dbRd = Global.sendToDB<RoleDataEx, string>(
                        (int)TCPGameServerCmds.CMD_SPR_GETOTHERATTRIB2,
                        string.Format("{0}:{1}", -1, statisticalData.rid), ServerID);
            if (dbRd == null || dbRd.RoleID <= 0) return;
            statisticalData.CityOwnerRoleData = DataHelper.ObjectToBytes(dbRd);
        }

        /// 获得帮派详细信息
        public BangHuiDetailData GetBangHuiDetailDataAuto(int bhid, int roleID = -1, int ServerID = GameManager.LocalServerId)
        {
            BangHuiDetailData bangHuiDetailData = Global.GetBangHuiDetailData(roleID, bhid, ServerID);
            if (null != bangHuiDetailData)
            {
                if (roleID <= 0 && bangHuiDetailData.BZRoleID > 0)
                {
                    bangHuiDetailData = Global.GetBangHuiDetailData(bangHuiDetailData.BZRoleID, bhid, ServerID);
                }
            }

            return bangHuiDetailData;
        }

        public bool CanEnterKuaFuMap(KuaFuServerLoginData kuaFuServerLoginData)
        {
            int rid = kuaFuServerLoginData.RoleId;
            LangHunLingYuFuBenData fuBenData;
            lock (RuntimeData.Mutex)
            {
                if (!RuntimeData.FuBenDataDict.TryGetValue((int)kuaFuServerLoginData.GameId, out fuBenData))
                {
                    fuBenData = null;
                }
                else if (fuBenData.State >= GameFuBenState.End)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("圣域争霸副本已结束,禁止角色{0}进入", rid));
                    return false;
                }
            }

            if (null == fuBenData)
            {
                //从中心查询副本信息
                LangHunLingYuFuBenData newFuBenData = YongZheZhanChangClient.getInstance().GetLangHunLingYuGameFuBenData((int)kuaFuServerLoginData.GameId);
                if (newFuBenData == null || newFuBenData.State == GameFuBenState.End)
                {
                    LogManager.WriteLog(LogTypes.Error, "获取不到有效的副本数据," + newFuBenData == null ? "fuBenData == null" : "fuBenData.State == GameFuBenState.End");
                    return false;
                }

                if (newFuBenData.ServerId != GameManager.ServerId)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("玩家请求进入的圣域争霸活动GameId={0}，不在本服务器{1}", fuBenData.GameId, GameManager.ServerId));
                    return false;
                }

                lock (RuntimeData.Mutex)
                {
                    if (!RuntimeData.FuBenDataDict.TryGetValue((int)kuaFuServerLoginData.GameId, out fuBenData))
                    {
                        fuBenData = newFuBenData;
                        fuBenData.SequenceId = GameCoreInterface.getinstance().GetNewFuBenSeqId();
                        RuntimeData.FuBenDataDict[fuBenData.GameId] = fuBenData;
                    }
                }
            }

            return true;
        }

        public bool OnInitGameKuaFu(GameClient client)
        {
            int bhid = client.ClientData.Faction;
            long rid = client.ClientData.RoleID;
            int posX;
            int posY;
            int side = 0;

            KuaFuServerLoginData kuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);
            LangHunLingYuFuBenData fuBenData;
            lock (RuntimeData.Mutex)
            {
                if (!RuntimeData.FuBenDataDict.TryGetValue((int)kuaFuServerLoginData.GameId, out fuBenData))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("玩家请求进入的圣域争霸活动GameId={0}，不在本服务器{1},角色{2}({3})", fuBenData.GameId, GameManager.ServerId, rid, Global.FormatRoleName4(client)));
                    return false;
                }
                else if (fuBenData.State >= GameFuBenState.End)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("圣域争霸副本已结束,禁止角色{0}({1})进入", rid, Global.FormatRoleName4(client)));
                    return false;
                }
            }

            //判断帮会是否是战斗参与者
            if (fuBenData.CityDataEx == null)
            {
                return false;
            }
            if (null != fuBenData.CityDataEx.Site)
            {
                for (int i = 0; i < fuBenData.CityDataEx.Site.Length; i++ )
                {
                    if (fuBenData.CityDataEx.Site[i] == bhid)
                    {
                        side = i + 1;
                    }
                }
            }
            if (side <= 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("角色{0}({1})所在帮会({2})不在指定的圣域争霸活动GameId={3}", rid, Global.FormatRoleName4(client), bhid, fuBenData.GameId));
                return false;
            }

            LangHunLingYuSceneInfo sceneInfo;
            lock (RuntimeData.Mutex)
            {
                int sceneIndex = fuBenData.GameId % RuntimeData.SceneDataList.Count;
                sceneInfo = RuntimeData.SceneDataList[sceneIndex];
                client.SceneInfoObject = sceneInfo;
                client.ClientData.MapCode = sceneInfo.MapCode;
            }

            client.ClientData.BattleWhichSide = side;
            kuaFuServerLoginData.FuBenSeqId = fuBenData.SequenceId;

            int toMapCode, toPosX, toPosY;
            if (!GetZhanMengBirthPoint(sceneInfo, client, client.ClientData.MapCode, out toMapCode, out toPosX, out toPosY))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("角色{0}({1})无法获取有效的阵营和出生点,进入跨服失败,side={2}", rid, Global.FormatRoleName4(client), side));
                return false;
            }

            client.ClientData.PosX = toPosX;
            client.ClientData.PosY = toPosY;
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
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.LangHunLingYu))
            {
                return false;
            }

            // 如果1.9的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot9))
            {
                return false;
            }

            return GlobalNew.IsGongNengOpened(client, GongNengIDs.LangHunLingYu, hint);
        }

#endregion 其他
    }
}
