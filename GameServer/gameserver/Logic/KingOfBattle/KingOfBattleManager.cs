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
    /// 王者战场管理
    /// </summary>
    public partial class KingOfBattleManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 标准接口

        public const SceneUIClasses ManagerType = SceneUIClasses.KingOfBattle;

        private static KingOfBattleManager instance = new KingOfBattleManager();

        public static KingOfBattleManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public KingOfBattleData RuntimeData = new KingOfBattleData();

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
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("KingOfBattleManager.TimerProc", TimerProc), 15000, 5000);
            return true;
        }

        public bool startup()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_JOIN, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_ENTER, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_STATE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_AWARD_GET, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_AWARD, 1, 1, getInstance());

            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_MALL_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_MALL_BUY, 3, 3, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_MALL_REFRESH, 1, 1, getInstance());

            //向事件源注册监听器
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.KingOfBattle, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.PlayerCaiJi, (int)SceneUIClasses.KingOfBattle, getInstance());

            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreMonsterInjure, (int)SceneUIClasses.KingOfBattle, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.ProcessClickOnNpc, (int)SceneUIClasses.KingOfBattle, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.OnCreateMonster, (int)SceneUIClasses.KingOfBattle, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.OnClientChangeMap, (int)SceneUIClasses.KingOfBattle, getInstance());

            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerDead, getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.ClientRegionEvent, getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, getInstance());
            return true;
        }

        public bool showdown()
        {
            //向事件源删除监听器
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.KingOfBattle, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.PlayerCaiJi, (int)SceneUIClasses.KingOfBattle, getInstance());

            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreMonsterInjure, (int)SceneUIClasses.KingOfBattle, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.ProcessClickOnNpc, (int)SceneUIClasses.KingOfBattle, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.OnCreateMonster, (int)SceneUIClasses.KingOfBattle, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.OnClientChangeMap, (int)SceneUIClasses.KingOfBattle, getInstance());

            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerDead, getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.ClientRegionEvent, getInstance());
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
                case (int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_JOIN:
                    return ProcessKingOfBattleJoinCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_ENTER:
                    return ProcessKingOfBattleEnterCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_STATE:
                    return ProcessGetKingOfBattleStateCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_AWARD_GET:
                    return ProcessGetKingOfBattleAwardCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_AWARD:
                    return ProcessGetKingOfBattleAwardInfoCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_MALL_DATA:
                    return ProcessGetKingOfBattleMallDataCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_MALL_BUY:
                    return ProcessKingOfBattleMallBuyCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_MALL_REFRESH:
                    return ProcessKingOfBattleMallRefreshCmd(client, nID, bytes, cmdParams);
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
                        SubmitCrystalBuff(e.Client, e.AreaLuaID);
                    }
                }
            }
            if (eventType == (int)EventTypes.PlayerDead)
            {
                PlayerDeadEventObject playerDeadEvent = eventObject as PlayerDeadEventObject;
                if (null != playerDeadEvent)
                {
                    if (playerDeadEvent.Type == PlayerDeadEventTypes.ByRole)
                    {
                        OnKillRole(playerDeadEvent.getAttackerRole(), playerDeadEvent.getPlayer());
                    }

                    // 卸载王者战场内Buff
                    GameClient clientDead = playerDeadEvent.getPlayer();
                    if (null != clientDead)
                    {
                        KingOfBattleScene scene;
                        if (SceneDict.TryGetValue(clientDead.ClientData.FuBenSeqID, out scene))
                        {
                            RemoveBattleSceneBuffForRole(scene, clientDead);
                        }
                    }
                }
            }
            if (eventType == (int)EventTypes.MonsterDead)
            {
                MonsterDeadEventObject e = eventObject as MonsterDeadEventObject;
                OnProcessMonsterDead(e.getAttacker(), e.getMonster());
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
                                    LogManager.WriteLog(LogTypes.Error, string.Format("通知角色ID={0}拥有进入王者战场资格,跨服GameID={1}", kuaFuServerLoginData.RoleId, kuaFuServerLoginData.GameId));
                                }
                            }
                            eventObject.Handled = true;
                        }
                    }
                    break;
                case (int)EventTypes.PreMonsterInjure:
                    {
                        PreMonsterInjureEventObject obj = eventObject as PreMonsterInjureEventObject;
                        if (obj != null && obj.SceneType == (int)SceneUIClasses.KingOfBattle)
                        {
                            Monster injureMonster = obj.Monster;

                            if (injureMonster == null) return;

                            // 军旗
                            //====Monsters===
                            //if (injureMonster.MonsterInfo.ExtensionID == RuntimeData.BattleQiZhiMonsterID1
                            //    || injureMonster.MonsterInfo.ExtensionID == RuntimeData.BattleQiZhiMonsterID2)
                            //{
                            //    obj.Injure = RuntimeData.KingOfBattleDamageJunQi;
                            //    eventObject.Handled = true;
                            //    eventObject.Result = true;
                            //}

                            KingOfBattleDynamicMonsterItem tagInfo = injureMonster.Tag as KingOfBattleDynamicMonsterItem;
                            if (tagInfo == null) return;

                            // 主基地
                            if (tagInfo.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_BaoLei)
                            {
                                obj.Injure = RuntimeData.KingOfBattleDamageCenter;
                                eventObject.Handled = true;
                                eventObject.Result = true;
                            }
                            // 箭塔
                            else if(tagInfo.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_TowerSecond
                                || tagInfo.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_TowerFirst)
                            {
                                obj.Injure = RuntimeData.KingOfBattleDamageTower;
                                eventObject.Handled = true;
                                eventObject.Result = true;
                            }
                        }
                    }
                    break;
                case (int)EventTypes.OnCreateMonster:
                    {
                        OnCreateMonsterEventObject e = eventObject as OnCreateMonsterEventObject;
                        if (null != e)
                        {
                            KingOfBattleQiZhiConfig qiZhiConfig = e.Monster.Tag as KingOfBattleQiZhiConfig;
                            if (null != qiZhiConfig)
                            {
                                e.Monster.Camp = qiZhiConfig.BattleWhichSide;
                                e.Result = true;
                                e.Handled = true;
                            }

                            // 水晶堡垒、箭塔阵营处理
                            KingOfBattleDynamicMonsterItem tagInfo = e.Monster.Tag as KingOfBattleDynamicMonsterItem;
                            if (null != tagInfo)
                            {
                                //====Monsters===
                                //if (tagInfo.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_BaoLei)
                                //{
                                //    e.Monster.Camp = e.Monster.MonsterInfo.Camp;
                                //    e.Result = true;
                                //    e.Handled = true;
                                //}
                                //else if(tagInfo.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_TowerSecond ||
                                //    tagInfo.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_TowerFirst)
                                //{
                                //    if ((int)MonsterTypes.XianFactionGuard == e.Monster.MonsterType)
                                //        e.Monster.Camp = 1;
                                //    else if ((int)MonsterTypes.MoFactionGuard == e.Monster.MonsterType)
                                //        e.Monster.Camp = 2;

                                //    e.Result = true;
                                //    e.Handled = true;
                                //}
                            }
                        }
                    }
                    break;
                case (int)GlobalEventTypes.PlayerCaiJi:
                    {
                        CaiJiEventObject e = eventObject as CaiJiEventObject;
                        if (null != e)
                        {
                            GameClient client = e.Source as GameClient;
                            Monster monster = e.Target as Monster;
                            OnCaiJiFinish(client, monster);
                            eventObject.Handled = true;
                            eventObject.Result = true;
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
                case (int)EventTypes.OnClientChangeMap:
                    {
                        OnClientChangeMapEventObject e = eventObject as OnClientChangeMapEventObject;
                        if (null != e)
                        {
                            e.Result = ClientChangeMap(e.Client, e.TeleportID, ref e.ToMapCode, ref e.ToPosX, ref e.ToPosY);
                            e.Handled = true;
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
                    //采集怪配置
                    RuntimeData.BattleCrystalMonsterDict.Clear();

                    fileName = "Config/KingOfBattleCrystalMonster.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        BattleCrystalMonsterItem item = new BattleCrystalMonsterItem();
                        item.Id = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.MonsterID = (int)Global.GetSafeAttributeLong(node, "MonsterID");
                        item.GatherTime = (int)Global.GetSafeAttributeLong(node, "GatherTime");
                        item.BattleJiFen = (int)Global.GetSafeAttributeLong(node, "BattleJiFen");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "X");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "Y");
                        item.FuHuoTime = (int)Global.GetSafeAttributeLong(node, "FuHuoTime") * 1000;
                        item.BuffGoodsID = (int)Global.GetSafeAttributeLong(node, "GoodsID");
                        item.BuffTime = (int)Global.GetSafeAttributeLong(node, "Time");
                        RuntimeData.BattleCrystalMonsterDict[item.Id] = item;
                    }

                    //出生点配置
                    RuntimeData.MapBirthPointDict.Clear();

                    fileName = "Config/KingOfBattleRebirth.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        YongZheZhanChangBirthPoint item = new YongZheZhanChangBirthPoint();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "PosX");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "PosY");
                        item.BirthRadius = (int)Global.GetSafeAttributeLong(node, "BirthRadius");

                        RuntimeData.MapBirthPointDict[item.ID] = item;
                    }

                    //活动配置
                    RuntimeData.SceneDataDict.Clear();
                    RuntimeData.LevelRangeSceneIdDict.Clear();
                    fileName = "Config/KingOfBattle.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        KingOfBattleSceneInfo sceneItem = new KingOfBattleSceneInfo();
                        int id = (int)Global.GetSafeAttributeLong(node, "Group");
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
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("11地图配置中缺少{0}所需的地图:{1}", fileName, mapCode));
                        }

                        RangeKey range = new RangeKey(Global.GetUnionLevel(sceneItem.MinZhuanSheng, sceneItem.MinLevel), Global.GetUnionLevel(sceneItem.MaxZhuanSheng, sceneItem.MaxLevel));
                        RuntimeData.LevelRangeSceneIdDict[range] = sceneItem;
                        RuntimeData.SceneDataDict[id] = sceneItem;
                    }

                    //活动奖励配置
                    fileName = "Config/KingOfBattleAward.xml";
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        int id = (int)Global.GetSafeAttributeLong(node, "MapCode");
                        KingOfBattleSceneInfo sceneItem;
                        if (RuntimeData.SceneDataDict.TryGetValue(id, out sceneItem))
                        {
                            sceneItem.Exp = (int)Global.GetSafeAttributeLong(node, "Exp");
                            sceneItem.BandJinBi = (int)Global.GetSafeAttributeLong(node, "BandJinBi");

                            sceneItem.AwardMinLevel = (int)Global.GetSafeAttributeLong(node, "MinLevel");
                            sceneItem.AwardMaxLevel = (int)Global.GetSafeAttributeLong(node, "MaxLevel");
                            sceneItem.AwardMinZhuanSheng = (int)Global.GetSafeAttributeLong(node, "MinZhuanSheng");
                            sceneItem.AwardMaxZhuanSheng = (int)Global.GetSafeAttributeLong(node, "MaxZhuanSheng");

                            ConfigParser.ParseAwardsItemList(Global.GetSafeAttributeStr(node, "WinGoods"), ref sceneItem.WinAwardsItemList);
                            ConfigParser.ParseAwardsItemList(Global.GetSafeAttributeStr(node, "LoseGoods"), ref sceneItem.LoseAwardsItemList);
                        }
                    }

                    // 怪
                    fileName = "Config/KingOfBattleMonster.xml";
                    fullPathFileName = Global.GameResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    HashSet<int> RebornBirthMonsterSet = new HashSet<int>();
                    foreach (var node in nodes)
                    {
                        KingOfBattleDynamicMonsterItem item = new KingOfBattleDynamicMonsterItem();
                        item.Id = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.MapCode = (int)Global.GetSafeAttributeLong(node, "CodeID");
                        item.MonsterID = (int)Global.GetSafeAttributeLong(node, "MonsterID");
                        item.MonsterType = (int)Global.GetSafeAttributeLong(node, "MonsterType");
                        item.RebornID = (int)Global.GetSafeAttributeLong(node, "RebornID");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "X");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "Y");
                        item.DelayBirthMs = (int)Global.GetSafeAttributeLong(node, "Time") * TimeUtil.SECOND;
                        item.PursuitRadius = (int)Global.GetSafeAttributeLong(node, "PursuitRadius"); 
                        item.BuffTime = (int)Global.GetSafeAttributeLong(node, "BuffTime");

                        // 积分
                        string[] JiFenListFileds = Global.GetSafeAttributeStr(node, "JiFen").Split('|');
                        if(JiFenListFileds.Length == 2)
                        {
                            item.JiFenDamage = Global.SafeConvertToInt32(JiFenListFileds[0]);
                            item.JiFenKill = Global.SafeConvertToInt32(JiFenListFileds[1]);
                        }

                        // Buff
                        string[] BuffListFileds = Global.GetSafeAttributeStr(node, "Buff").Split('|');
                        for(int n=0; n<BuffListFileds.Length; ++n)
                        {
                            string[] BuffFiled = BuffListFileds[n].Split(',');
                            if(BuffFiled.Length == 2)
                            {
                                KingOfBattleRandomBuff buff = new KingOfBattleRandomBuff();
                                buff.GoodsID = Global.SafeConvertToInt32(BuffFiled[0]);
                                buff.Pct = Global.SafeConvertToDouble(BuffFiled[1]);
                                item.RandomBuffList.Add(buff);
                            }
                        }

                        List<KingOfBattleDynamicMonsterItem> itemList = null;
                        if (!RuntimeData.SceneDynMonsterDict.TryGetValue(item.MapCode, out itemList))
                        {
                            itemList = new List<KingOfBattleDynamicMonsterItem>();
                            RuntimeData.SceneDynMonsterDict[item.MapCode] = itemList;
                        }
                        RuntimeData.DynMonsterDict[item.Id] = item;

                        if (item.RebornID != -1) // 重生生成怪物列表                     
                            RebornBirthMonsterSet.Add(item.RebornID);
                        
                        itemList.Add(item);
                    }

                    // 处理怪物重生方式
                    foreach (var kvp in RuntimeData.DynMonsterDict)
                    {
                        if (RebornBirthMonsterSet.Contains(kvp.Value.Id))
                            kvp.Value.RebornBirth = true;                        
                    }

                    // 旗座
                    RuntimeData.NPCID2QiZhiConfigDict.Clear();
                    fileName = "Config/KingOfBattleQiZuo.xml";
                    fullPathFileName = Global.GameResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        KingOfBattleQiZhiConfig item = new KingOfBattleQiZhiConfig();
                        item.NPCID = (int)Global.GetSafeAttributeLong(node, "NPCID");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "PosX");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "PosY");
                        item.QiZhiMonsterID = (int)Global.GetSafeAttributeLong(node, "Monster");
                        RuntimeData.NPCID2QiZhiConfigDict[item.NPCID] = item;
                    }

                    // 商店
                    RuntimeData.KingOfBattleStoreDict.Clear();
                    RuntimeData.KingOfBattleStoreList.Clear();
                    fileName = "Config/KingOfBattleStore.xml";
                    fullPathFileName = Global.GameResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        KingOfBattleStoreConfig item = new KingOfBattleStoreConfig();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.SaleData = Global.ParseGoodsFromStr_7(Global.GetSafeAttributeStr(node, "GoodsID").Split(','), 0);
                        item.JiFen = (int)Global.GetSafeAttributeLong(node, "WangZheJiFen");
                        item.SinglePurchase = (int)Global.GetSafeAttributeLong(node, "SinglePurchase");
                        item.BeginNum = (int)Global.GetSafeAttributeLong(node, "BeginNum");
                        item.EndNum = (int)Global.GetSafeAttributeLong(node, "EndNum");
                        item.RandNumMinus = item.EndNum - item.BeginNum + 1;
                        RuntimeData.KingOfBattleStoreDict[item.ID] = item;
                        RuntimeData.KingOfBattleStoreList.Add(item);
                    }

                    // SystemParams
                    // Mu王者战场 固定伤害 军旗、箭塔、主基地 KingOfBattleAttackBuild
                    int[] intArray = GameManager.systemParamsList.GetParamValueIntArrayByName("KingOfBattleAttackBuild");
                    if(intArray.Length == 3)
                    {
                        RuntimeData.KingOfBattleDamageJunQi = intArray[0];
                        RuntimeData.KingOfBattleDamageTower = intArray[1];
                        RuntimeData.KingOfBattleDamageCenter = intArray[2];
                    }

                    // Mu王者战场被杀获得积分，积分值 KingOfBattleDie
                    RuntimeData.KingOfBattleDie = (int)GameManager.systemParamsList.GetParamValueIntByName("KingOfBattleDie");

                    // Mu王者战场连杀积分 KingOfBattleUltraKill
                    intArray = GameManager.systemParamsList.GetParamValueIntArrayByName("KingOfBattleUltraKill");
                    if (intArray.Length == 4)
                    {
                        RuntimeData.KingOfBattleUltraKillParam1 = intArray[0];
                        RuntimeData.KingOfBattleUltraKillParam2 = intArray[1];
                        RuntimeData.KingOfBattleUltraKillParam3 = intArray[2];
                        RuntimeData.KingOfBattleUltraKillParam4 = intArray[3];
                    }

                    // Mu王者战场终结连杀积分 KingOfBattleShutDown
                    intArray = GameManager.systemParamsList.GetParamValueIntArrayByName("KingOfBattleShutDown");
                    if (intArray.Length == 4)
                    {
                        RuntimeData.KingOfBattleShutDownParam1 = intArray[0];
                        RuntimeData.KingOfBattleShutDownParam2 = intArray[1];
                        RuntimeData.KingOfBattleShutDownParam3 = intArray[2];
                        RuntimeData.KingOfBattleShutDownParam4 = intArray[3];
                    }

                    // 奖励最低积分要求 KingOfBattleLowestJiFen
                    RuntimeData.KingOfBattleLowestJiFen = (int)GameManager.systemParamsList.GetParamValueIntByName("KingOfBattleLowestJiFen");

                    // 王者战场商店刷新时间、刷新个数、刷新价格 KingOfBattleStore
                    intArray = GameManager.systemParamsList.GetParamValueIntArrayByName("KingOfBattleStore");
                    if (intArray.Length == 3)
                    {
                        RuntimeData.KingOfBattleStoreRefreshTm = intArray[0];
                        RuntimeData.KingOfBattleStoreRefreshNum = intArray[1];
                        RuntimeData.KingOfBattleStoreRefreshCost = intArray[2];
                    }
                }

                catch (System.Exception ex)
                {
                    success = false;
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("加载xml配置文件:{0}, 失败。", fileName), ex);
                }
            }

            return success;
        }
       
        private void TimerProc(object sender, EventArgs e)
        {
            bool notifyPrepareGame = false;
            bool notifyEnterGame = false;
            DateTime now = TimeUtil.NowDateTime();
            lock (RuntimeData.Mutex)
            {
                bool bInActiveTime = false;

                KingOfBattleSceneInfo sceneItem = RuntimeData.SceneDataDict.Values.FirstOrDefault();
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
                string cmd = string.Format("{0} {1} {2}", GameStates.CommandName, GameStates.PrepareGame, (int)GameTypes.KingOfBattle);
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
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_ENTER, 1);
                    }
                }
            }
        }

        #endregion 初始化配置

        #region 指令处理

        /// <summary>
        /// 王者战场申请指令处理
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessKingOfBattleJoinCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success_No_Info;

                do
                {
                    if (!IsGongNengOpened(client))
                    {
                        break;
                    }

                    KingOfBattleSceneInfo sceneItem = null;
                    KingOfBattleGameStates state = KingOfBattleGameStates.None;
                    if (!CheckMap(client))
                    {
                        result = StdErrorCode.Error_Denied_In_Current_Map;
                    }
                    else
                    {
                        result = CheckCondition(client, ref sceneItem, ref state);
                    }

                    if (state != KingOfBattleGameStates.SignUp)
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
                            (int)GameTypes.KingOfBattle, gropuId, client.ClientData.CombatForce);
                        if (result > 0)
                        {
                            RuntimeData.RoleId2JoinGroup[client.ClientData.RoleID] = gropuId;
                            client.ClientData.SignUpGameType = (int)GameTypes.KingOfBattle;
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

        // 检查王者战场当前处于什么时间状态
        private int CheckCondition(GameClient client, ref KingOfBattleSceneInfo sceneItem, ref KingOfBattleGameStates state)
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
                                state = KingOfBattleGameStates.None;
                                result = StdErrorCode.Error_Not_In_valid_Time;
                            }
                            else if (now.TimeOfDay.TotalSeconds < sceneItem.SecondsOfDay[i] - sceneItem.SignUpEndSecs)
                            {
                                state = KingOfBattleGameStates.SignUp;
                                result = StdErrorCode.Error_Success;
                            }
                            else if (now.TimeOfDay.TotalSeconds < sceneItem.SecondsOfDay[i])
                            {
                                state = KingOfBattleGameStates.Wait;
                                result = StdErrorCode.Error_Success;
                            }
                            else if (now.TimeOfDay.TotalSeconds < sceneItem.SecondsOfDay[i + 1])
                            {
                                state = KingOfBattleGameStates.Start;
                                result = StdErrorCode.Error_Success;
                            }
                            else
                            {
                                state = KingOfBattleGameStates.None;
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
            KingOfBattleSceneInfo sceneItem = null;
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

        public bool ProcessGetKingOfBattleAwardCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int err = StdErrorCode.Error_Success;
                if (!IsGongNengOpened(client))
                {
                    return false;
                }

                string awardsInfo = Global.GetRoleParamByName(client, RoleParamName.KingOfBattleAwards);
                if (!string.IsNullOrEmpty(awardsInfo))
                {
                    int lastGroupId = 0;
                    int score = 0;
                    int success = 0;
                    int sideScore1 = 0;
                    int sideScore2 = 0;
                    ConfigParser.ParseStrInt3(awardsInfo, ref lastGroupId, ref success, ref score);
                    List<int> awardsParamList = Global.StringToIntList(awardsInfo, ',');
                    lastGroupId = awardsParamList[0];
                    bool clear = true;
                    if (awardsParamList.Count >= 5 && lastGroupId > 0)
                    {
                        success = awardsParamList[1];
                        score = awardsParamList[2];
                        sideScore1 = awardsParamList[3];
                        sideScore2 = awardsParamList[4];

                        KingOfBattleSceneInfo lastSceneItem = null;
                        if (RuntimeData.SceneDataDict.TryGetValue(lastGroupId, out lastSceneItem))
                        {
                            err = GiveRoleAwards(client, success, score, lastSceneItem);
                            if (err < StdErrorCode.Error_Success_No_Info)
                            {
                                clear = false;
                            }
                        }
                    }

                    if (clear)
                    {
                        Global.SaveRoleParamsStringToDB(client, RoleParamName.KingOfBattleAwards, RuntimeData.RoleParamsAwardsDefaultString, true);
                    }

                    client.sendCmd(nID, err);
                }

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            client.sendCmd(nID, StdErrorCode.Error_Success_No_Info);

            return false;
        }

        public bool ProcessGetKingOfBattleAwardInfoCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    return false;
                }

                string awardsInfo = Global.GetRoleParamByName(client, RoleParamName.KingOfBattleAwards);
                if (!string.IsNullOrEmpty(awardsInfo))
                {
                    int lastGroupId = 0;
                    int score = 0;
                    int success = 0;
                    int sideScore1 = 0;
                    int sideScore2 = 0;
                    ConfigParser.ParseStrInt3(awardsInfo, ref lastGroupId, ref success, ref score);
                    List<int> awardsParamList = Global.StringToIntList(awardsInfo, ',');
                    lastGroupId = awardsParamList[0];
                    bool clear = true;
                    if (awardsParamList.Count >= 5 && lastGroupId > 0)
                    {
                        success = awardsParamList[1];
                        score = awardsParamList[2];
                        sideScore1 = awardsParamList[3];
                        sideScore2 = awardsParamList[4];

                        KingOfBattleSceneInfo lastSceneItem = null;
                        if (RuntimeData.SceneDataDict.TryGetValue(lastGroupId, out lastSceneItem))
                        {
                            //只给一次,马上清掉记录
                            if (score >= RuntimeData.KingOfBattleLowestJiFen)
                            {
                                clear = false;
                            }

                            NtfCanGetAward(client, success, score, lastSceneItem, sideScore1, sideScore2);
                        }
                    }

                    if (clear)
                    {
                        Global.SaveRoleParamsStringToDB(client, RoleParamName.KingOfBattleAwards, RuntimeData.RoleParamsAwardsDefaultString, true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            client.sendCmd(nID, StdErrorCode.Error_Success_No_Info);

            return false;
        }

        public bool ProcessGetKingOfBattleStateCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                // 根据策划需求，任何时候来查询状态，领奖状态具有最高优先级
                string awardsInfo = Global.GetRoleParamByName(client, RoleParamName.KingOfBattleAwards);
                if (!string.IsNullOrEmpty(awardsInfo))
                {
                    int lastGroupId = 0;
                    int score = 0;
                    int success = 0;
                    ConfigParser.ParseStrInt3(awardsInfo, ref lastGroupId, ref success, ref score);
                    if (lastGroupId > 0)
                    {
                        KingOfBattleSceneInfo lastSceneItem = null;
                        if (RuntimeData.SceneDataDict.TryGetValue(lastGroupId, out lastSceneItem))
                        {
                            // 通知有奖励可以领取
                            client.sendCmd(nID, (int)KingOfBattleGameStates.Awards);
                            return true;
                        }
                    }
                }

                KingOfBattleSceneInfo sceneItem = null;
                KingOfBattleGameStates timeState = KingOfBattleGameStates.None;
                int result = (int)KingOfBattleGameStates.None;
                int groupId = 0;
                RuntimeData.RoleId2JoinGroup.TryGetValue(client.ClientData.RoleID, out groupId);

                CheckCondition(client, ref sceneItem, ref timeState);
                if (groupId > 0)
                {
                    if (timeState >= KingOfBattleGameStates.SignUp && timeState <= KingOfBattleGameStates.Wait)
                    {
                        int state = YongZheZhanChangClient.getInstance().GetKuaFuRoleState(client.ClientData.RoleID);
                        if (state >= (int)KuaFuRoleStates.SignUp)
                        {
                            result = (int)KingOfBattleGameStates.Wait;
                        }
                        else
                        {
                            result = (int)KuaFuBossGameStates.NotJoin;
                        }
                    }
                    else if (timeState == KingOfBattleGameStates.Start)
                    {
                        if (RuntimeData.RoleIdKuaFuLoginDataDict.ContainsKey(client.ClientData.RoleID))
                        {
                            result = (int)KingOfBattleGameStates.Start;
                        }
                    }
                }
                else
                {
                    if (timeState == KingOfBattleGameStates.SignUp)
                    {
                        result = (int)KingOfBattleGameStates.SignUp;
                    }
                    else if (timeState == KingOfBattleGameStates.Wait || timeState == KingOfBattleGameStates.Start)
                    {
                        // 未参加本次活动
                        result = (int)KingOfBattleGameStates.NotJoin;
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

        public bool ProcessKingOfBattleEnterCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success_No_Info;
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, result);
                    return true;
                }

                KingOfBattleSceneInfo sceneItem = null;
                KingOfBattleGameStates state = KingOfBattleGameStates.None;

                if (!CheckMap(client))
                {
                    result = StdErrorCode.Error_Denied_In_Current_Map;
                }
                else
                {
                    result = CheckCondition(client, ref sceneItem, ref state);
                }

                if (state == KingOfBattleGameStates.Start)
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

        // 王者战场商店刷新
        public void RefreshKingOfBattleStoreData(KingOfBattleStoreData KOBattleStoreData, bool SetRefreshTm = true)
        {
            lock (RuntimeData.Mutex)
            {
                if (true == SetRefreshTm) // 设置刷新时间
                    KOBattleStoreData.LastRefTime = TimeUtil.NowDateTime();

                KOBattleStoreData.SaleList.Clear();

                // 计算100%概率总值
                List<KingOfBattleStoreConfig> KOBStoreList = RuntimeData.KingOfBattleStoreList;
                int PercentZero = KOBStoreList[0].BeginNum;
                int PercentOne = KOBStoreList[KOBStoreList.Count - 1].EndNum;

                // 随机指定数量的商品出来
                for (int Num = 0; Num < RuntimeData.KingOfBattleStoreRefreshNum; ++Num)
                {
                    // 随机一个出来
                    int rate = Global.GetRandomNumber(PercentZero, PercentOne);
                    for (int i = 0; i < KOBStoreList.Count; ++i)
                    {
                        if (true == KOBStoreList[i].RandSkip)
                        {
                            rate += KOBStoreList[i].RandNumMinus;
                        }
                        if (false == KOBStoreList[i].RandSkip &&
                            rate >= KOBStoreList[i].BeginNum && rate <= KOBStoreList[i].EndNum)
                        {
                            // 命中
                            KOBStoreList[i].RandSkip = true;
                            PercentOne -= KOBStoreList[i].RandNumMinus; // 用最新的随机数上限随机

                            KingOfBattleStoreSaleData SaleData = new KingOfBattleStoreSaleData();
                            SaleData.ID = KOBStoreList[i].ID;
                            KOBattleStoreData.SaleList.Add(SaleData);
                        }
                    }
                }

                // refresh KingOfBattleStoreList
                for (int i = 0; i < KOBStoreList.Count; ++i)
                    KOBStoreList[i].RandSkip = false;           
            }
        }

        // 获取王者战场商店数据
        public KingOfBattleStoreData GetClientKingOfBattleStoreData(GameClient client)
        {
            // 缓存的数据
            if (null != client.ClientData.KOBattleStoreData)
                return client.ClientData.KOBattleStoreData;

            lock (RuntimeData.Mutex)
            {
                client.ClientData.KOBattleStoreData = new KingOfBattleStoreData();
                client.ClientData.KOBattleStoreData.LastRefTime = Global.GetRoleParamsDateTimeFromDB(client, RoleParamName.KingOfBattleStoreTm);
                client.ClientData.KOBattleStoreData.SaleList = new List<KingOfBattleStoreSaleData>();

                // 商品数据
                List<ushort> StoreSaleDataList = Global.GetRoleParamsUshortListFromDB(client, RoleParamName.KingOfBattleStore);
                for(int index = 0; index<StoreSaleDataList.Count; index += 2)
                {
                    KingOfBattleStoreSaleData SaleData = new KingOfBattleStoreSaleData();
                    SaleData.ID = StoreSaleDataList[index];
                    SaleData.Purchase = StoreSaleDataList[index + 1];
                    client.ClientData.KOBattleStoreData.SaleList.Add(SaleData);
                }
            }
            return client.ClientData.KOBattleStoreData;
        }

        // Save王者战场商店数据
        public void SaveKingOfBattleStoreData(GameClient client)
        {
            if (null == client.ClientData.KOBattleStoreData)
                return;

            lock (RuntimeData.Mutex)
            {
                KingOfBattleStoreData KOBattleStoreData = client.ClientData.KOBattleStoreData;
                Global.SaveRoleParamsDateTimeToDB(client, RoleParamName.KingOfBattleStoreTm, KOBattleStoreData.LastRefTime, true);

                // 商品数据
                List<ushort> StoreSaleDataList = new List<ushort>();
                foreach (var item in KOBattleStoreData.SaleList)
                {
                    StoreSaleDataList.Add((ushort)item.ID);
                    StoreSaleDataList.Add((ushort)item.Purchase);
                }
                Global.SaveRoleParamsUshortListToDB(client, StoreSaleDataList, RoleParamName.KingOfBattleStore, true);
            }
        }

        public bool ProcessGetKingOfBattleMallDataCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    return false;
                }

                // 获得角色王者商店数据
                KingOfBattleStoreData KOBattleStoreData = GetClientKingOfBattleStoreData(client);

                TimeSpan tmSpan = TimeUtil.NowDateTime() - KOBattleStoreData.LastRefTime;
                if (tmSpan.TotalSeconds >= RuntimeData.KingOfBattleStoreRefreshTm * 3600)
                {
                    // 尝试刷新数据
                    RefreshKingOfBattleStoreData(KOBattleStoreData);

                    // save to db
                    SaveKingOfBattleStoreData(client);
                }

                // 返回给客户端结果
                client.sendCmd<KingOfBattleStoreData>(nID, KOBattleStoreData);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessKingOfBattleMallBuyCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    return false;
                }

                String strcmd = "";
                int result = (int)KingOfBattleErrorCode.KingOfBattle_Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);
                int storeID = Global.SafeConvertToInt32(cmdParams[1]);
                int countNum = Global.SafeConvertToInt32(cmdParams[2]);

                // 找配置数据
                KingOfBattleStoreConfig KOBattleStoreConfig = null;
                lock (RuntimeData.Mutex)
                {
                    if(!RuntimeData.KingOfBattleStoreDict.TryGetValue(storeID, out KOBattleStoreConfig))
                    {
                        result = (int)KingOfBattleErrorCode.KingOfBattle_ErrorParams;
                        strcmd = string.Format("{0}:{1}:{2}", result, storeID, 0);
                        client.sendCmd(nID, strcmd);
                        return true;
                    }
                }

                // 获得角色王者商店数据，查看是否有对应的商品。
                KingOfBattleStoreData KOBattleStoreData = GetClientKingOfBattleStoreData(client);
                KingOfBattleStoreSaleData SaleData = null;
                foreach (var item in KOBattleStoreData.SaleList)
                {
                    if (item.ID == storeID)
                    {
                        SaleData = item;
                        break;
                    }
                }
                if(null == SaleData)
                {
                    result = (int)KingOfBattleErrorCode.KingOfBattle_ErrorNotSaleGoods;
                    strcmd = string.Format("{0}:{1}:{2}", result, storeID, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查限购个数
                if (KOBattleStoreConfig.SinglePurchase - SaleData.Purchase < countNum)
                {
                    result = (int)KingOfBattleErrorCode.KingOfBattle_ErrorPurchaseLimit;
                    strcmd = string.Format("{0}:{1}:{2}", result, storeID, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查背包
                if (!Global.CanAddGoods(client, KOBattleStoreConfig.SaleData.GoodsID, KOBattleStoreConfig.SaleData.GCount * countNum
                    , KOBattleStoreConfig.SaleData.Binding))
                {
                    result = (int)KingOfBattleErrorCode.KingOfBattle_ErrorPurchaseLimit;
                    strcmd = string.Format("{0}:{1}:{2}", result, storeID, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查王者积分是否够
                int curKingOfBattlePoint = GameManager.ClientMgr.GetKingOfBattlePointValue(client);
                if (curKingOfBattlePoint < KOBattleStoreConfig.JiFen * countNum)
                {
                    result = (int)KingOfBattleErrorCode.KingOfBattle_ErrorJiFenNotEnough;
                    strcmd = string.Format("{0}:{1}:{2}", result, storeID, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 扣王者积分
                GameManager.ClientMgr.ModifyKingOfBattlePointValue(client, -KOBattleStoreConfig.JiFen * countNum, "王者战场商店", true);

                // 给物品
                GoodsData goodsData = KOBattleStoreConfig.SaleData;
                Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount * countNum, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, string.Format("王者战场商店"), false,
                                                                        goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, true);

                SaleData.Purchase += countNum;
                SaveKingOfBattleStoreData(client); // save to db

                strcmd = string.Format("{0}:{1}:{2}", result, storeID, SaleData.Purchase);
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessKingOfBattleMallRefreshCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    return false;
                }

                String strcmd = "";
                int result = (int)KingOfBattleErrorCode.KingOfBattle_Success;

                // 检查钻石够不够
                if (client.ClientData.UserMoney < RuntimeData.KingOfBattleStoreRefreshCost)
                {
                    result = (int)KingOfBattleErrorCode.KingOfBattle_ErrorZuanShiNotEnough;
                    strcmd = string.Format("{0}", result);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 扣钻石
                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                        Global._TCPManager.TcpOutPacketPool, client, RuntimeData.KingOfBattleStoreRefreshCost, "王者战场商店刷新"))
                {
                    result = (int)KingOfBattleErrorCode.KingOfBattle_ErrorZuanShiNotEnough;
                    strcmd = string.Format("{0}", result);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 获得角色身上的王者战场商店数据
                KingOfBattleStoreData KOBattleStoreData = GetClientKingOfBattleStoreData(client);

                // 强制刷新
                RefreshKingOfBattleStoreData(KOBattleStoreData, false);

                // save to db
                SaveKingOfBattleStoreData(client);

                // 同步新的数据给客户端
                client.sendCmd<KingOfBattleStoreData>((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_MALL_DATA, KOBattleStoreData);

                // 返回结果
                strcmd = string.Format("{0}", result);
                client.sendCmd(nID, strcmd);
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

        public void OnStartPlayGame(GameClient client)
        {
            lock (RuntimeData.Mutex)
            {
                KingOfBattleScene scene = null;
                if (!SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                    return;

                // 非首次进来的人同步传送门开启状态  
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_TELEPORT, scene.SceneOpenTeleportList);
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

            client.ClientData.BattleWhichSide = kuaFuFuBenRoleData.Side;
            side = GetBirthPoint(client, out posX, out posY);
            if (side <= 0)
            {
                LogManager.WriteLog(LogTypes.Error, "无法获取有效的阵营和出生点,进入跨服失败,side=" + side);
                return false;
            }

            KingOfBattleSceneInfo sceneInfo;
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

        /// <summary>
        /// 出生点
        /// </summary>
        public int GetBirthPoint(GameClient client, out int posX, out int posY)
        {
            int side = client.ClientData.BattleWhichSide;
            lock (RuntimeData.Mutex)
            {
                YongZheZhanChangBirthPoint birthPoint = null;
                if (RuntimeData.MapBirthPointDict.TryGetValue(side, out birthPoint))
                {
                    posX = birthPoint.PosX;
                    posY = birthPoint.PosY;
                    return side;
                }
            }

            posX = 0;
            posY = 0;
            return -1;
        }

        /// <summary>
        /// 判断功能是否开启
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool IsGongNengOpened(GameClient client, bool hint = false)
        {
            // 如果1.7 或 1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7)
                || GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
            {
                return false;
            }

            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.KingOfBattle))
            {
                return false;
            }

            return GlobalNew.IsGongNengOpened(client, GongNengIDs.KingOfBattle, hint);
        }

        public int GetCaiJiMonsterTime(GameClient client, Monster monster)
        {
            BattleCrystalMonsterItem tag = monster != null ? monster.Tag as BattleCrystalMonsterItem : null;
            if (tag == null) return StdErrorCode.Error_Has_Get;

            return tag.GatherTime;
        }

        #endregion 其他

        #region 军旗和BUFF

        /// <summary>
        /// 构造一个场景Buff key
        /// </summary>
        private string BuildSceneBuffKey(GameClient client, int bufferGoodsID)
        {
            return string.Format("{0}_{1}", client.ClientData.RoleID, bufferGoodsID);
        }

        /// <summary>
        /// 更新玩家的Buff
        /// </summary>
        private void UpdateBuff4GameClient(GameClient client, int bufferGoodsID, object tagInfo, bool add)
        {
            try
            {
                // 附加信息
                BattleCrystalMonsterItem CrystalItem = tagInfo as BattleCrystalMonsterItem;
                KingOfBattleDynamicMonsterItem MonsterItem = tagInfo as KingOfBattleDynamicMonsterItem;
                if (null == CrystalItem && null == MonsterItem)
                    return;

                int BuffTime = 0; // buff 持续时间 秒
                BufferItemTypes buffItemType = BufferItemTypes.None;
                if(null != CrystalItem)
                {
                    BuffTime = CrystalItem.BuffTime;
                    buffItemType = BufferItemTypes.KingOfBattleCrystal;
                }
                if(null != MonsterItem)
                {
                    BuffTime = MonsterItem.BuffTime;
                    buffItemType = (BufferItemTypes)bufferGoodsID;
                }

                KingOfBattleScene scene;
                if (!SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                    return;

                EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(bufferGoodsID);
                if (null == item)
                    return;

                if (add)
                {
                    double[] actionParams = new double[] { BuffTime, bufferGoodsID };
                    Global.UpdateBufferData(client, buffItemType, actionParams, 1, true);
                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.BufferByGoodsProps, bufferGoodsID, item.ExtProps);

                    // 添加场景Buff
                    string Key = BuildSceneBuffKey(client, bufferGoodsID);
                    scene.SceneBuffDict[Key] = new KingOfBattleSceneBuff()
                    {
                        RoleID = client.ClientData.RoleID,
                        BuffID = bufferGoodsID,
                        EndTicks = TimeUtil.NOW() + BuffTime * TimeUtil.SECOND,
                        tagInfo = tagInfo
                    };

                    // 采集数据标记
                    if (buffItemType == BufferItemTypes.KingOfBattleCrystal)
                        client.SceneContextData = tagInfo;
                }
                else
                {
                    Global.RemoveBufferData(client, (int)buffItemType);
                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.BufferByGoodsProps, bufferGoodsID, PropsCacheManager.ConstExtProps);
                    
                    // 删除场景Buff
                    string Key = BuildSceneBuffKey(client, bufferGoodsID);
                    scene.SceneBuffDict.Remove(Key);

                    // 清除角色身上的水晶数据
                    if (buffItemType == BufferItemTypes.KingOfBattleCrystal)
                        client.SceneContextData = null;
                }
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// 获得Boss击杀随机Buff
        /// </summary>
        public void TryAddBossKillRandomBuff(GameClient client, KingOfBattleDynamicMonsterItem tagInfo)
        {
            int GoodsID = -1;
            if (tagInfo.RandomBuffList.Count == 0)
                return;

            // 随机累计数上限
            double rateEnd = 0.0d;

            // 随机一个Buff出来
            double rate = (double)Global.GetRandomNumber(1, 101) / 100;
            for (int i = 0; i < tagInfo.RandomBuffList.Count; ++i)
            {
                rateEnd += tagInfo.RandomBuffList[i].Pct;
                if (rate <= rateEnd)
                {
                    GoodsID = tagInfo.RandomBuffList[i].GoodsID;
                    break;
                }
            }

            UpdateBuff4GameClient(client, GoodsID, tagInfo, true);
        }

        /// <summary>
        /// 安插军旗
        /// </summary>
        public void InstallJunQi(KingOfBattleScene scene, GameClient client, KingOfBattleQiZhiConfig item)
        {
            CopyMap copyMap = scene.CopyMap;
            GameMap gameMap = GameManager.MapMgr.GetGameMap(scene.m_nMapCode);
            if (null == copyMap || null == gameMap)
                return;

            item.Alive = true;
            item.BattleWhichSide = client.ClientData.BattleWhichSide;

            int BattleQiZhiMonsterID = 0;
            if (client.ClientData.BattleWhichSide == 1)
            {
                BattleQiZhiMonsterID = RuntimeData.BattleQiZhiMonsterID1;
            }
            else if (client.ClientData.BattleWhichSide == 2)
            {
                BattleQiZhiMonsterID = RuntimeData.BattleQiZhiMonsterID2;
            }
            GameManager.MonsterZoneMgr.AddDynamicMonsters(copyMap.MapCode, BattleQiZhiMonsterID, copyMap.CopyMapID, 1,
                    item.PosX / gameMap.MapGridWidth, item.PosY / gameMap.MapGridHeight, 0, 0, SceneUIClasses.KingOfBattle, item);
        }

        /// <summary>
        /// 根据军旗状态计算传送门开启关闭
        /// </summary>
        public void CalculateTeleportGateState(KingOfBattleScene scene)
        {
            // 检查所有军旗是否都属于一个阵营
            int OpenGateSide = -1;
            foreach (var qizhi in scene.NPCID2QiZhiConfigDict.Values)
            {
                // 获取首个有效旗帜的阵营
                if (OpenGateSide == -1 && qizhi.Alive)
                    OpenGateSide = qizhi.BattleWhichSide;

                // 存在空旗座 或 存在不同阵营插的旗
                if (!qizhi.Alive || qizhi.BattleWhichSide != OpenGateSide)
                {
                    OpenGateSide = -1;
                    break;
                }
            }

            scene.SceneOpenTeleportList.Clear();

            // 开启传送门
            if (-1 != OpenGateSide)
                scene.SceneOpenTeleportList.Add(OpenGateSide + 10);

            // 同步给客户端
            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_TELEPORT, scene.SceneOpenTeleportList, scene.CopyMap);       
        }

        /// <summary>
        /// 处理玩家点击NPC事件
        /// 如果是旗座,尝试安装帮旗
        /// </summary>
        /// <param name="client"></param>
        /// <param name="npcID"></param>
        /// <returns>是否是旗座NPC</returns>
        public bool OnSpriteClickOnNpc(GameClient client, int npcID, int npcExtentionID)
        {
            KingOfBattleQiZhiConfig item = null;
            bool isQiZuo = false;
            bool installJunQi = false;
         
            KingOfBattleScene scene = client.SceneObject as KingOfBattleScene;            
            if (null == scene)
                return isQiZuo;

            lock (RuntimeData.Mutex)
            {
                do 
                {
                    if (!scene.NPCID2QiZhiConfigDict.TryGetValue(npcExtentionID, out item))
                        break;

                    isQiZuo = true; // 是否旗座
                    if (item.Alive) return isQiZuo;
                    if (client.ClientData.BattleWhichSide != item.BattleWhichSide && Math.Abs(TimeUtil.NOW() - item.DeadTicks) < 3 * 1000)
                    {
                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                client, StringUtil.substitute(Global.GetLang("非砍倒战旗的阵营，在原有盟旗被砍倒3秒后，才能安插盟旗！")),
                                GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                        break;
                    }

                    //在1000*1000的距离内才可以
                    if (Math.Abs(client.ClientData.PosX - item.PosX) <= 1000 && Math.Abs(client.ClientData.PosY - item.PosY) <= 1000)
                    {
                        installJunQi = true;
                    }

                } while (false);

                // 安插
                if (installJunQi)
                {
                    InstallJunQi(scene, client, item);
                    CalculateTeleportGateState(scene);
                }
            }
            return isQiZuo;
        }

        #endregion
    }
}