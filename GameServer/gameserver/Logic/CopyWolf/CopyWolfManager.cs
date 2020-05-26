using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Server;
using GameServer.Tools;
using KF.Contract.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using Tmsk.Contract;

namespace GameServer.Logic
{
    /// <summary>
    /// 狼魂要塞
    /// </summary>
    public class CopyWolfManager : IManager, IEventListener, IEventListenerEx
    {
        #region 标准接口

        //public const SceneUIClasses _sceneType = SceneUIClasses.CopyWolf;
        public const GameTypes _gameType = GameTypes.CopyWolf;

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long _nextHeartBeatTicks = 0L;

        /// <summary>
        /// 线程锁对象
        /// </summary>
        public static object _mutex = new object();

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public CopyWolfInfo _runtimeData = new CopyWolfInfo();

        private static CopyWolfManager instance = new CopyWolfManager();
        public static CopyWolfManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            if (!InitConfig())
                return false;

            return true;
        }

        public bool startup()
        {
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterToMonsterDead, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreMonsterInjure, (int)SceneUIClasses.CopyWolf, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.OnCreateMonster, (int)SceneUIClasses.CopyWolf, getInstance());

            return true;
        }

        public bool showdown()
        {
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterToMonsterDead, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreMonsterInjure, (int)SceneUIClasses.CopyWolf, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.OnCreateMonster, (int)SceneUIClasses.CopyWolf, getInstance());
            return true;
        }

        public bool destroy()
        {
            return true;
        }

        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() == (int)EventTypes.MonsterDead)
            {
                MonsterDeadEventObject obj = eventObject as MonsterDeadEventObject;
                Monster monster = obj.getMonster();
                GameClient client = obj.getAttacker();
                MonsterDead(client, monster);
            }

           if (eventObject.getEventType() == (int)EventTypes.MonsterToMonsterDead)
           {
               MonsterToMonsterDeadEventObject obj = eventObject as MonsterToMonsterDeadEventObject;
               Monster attack = obj.getMonsterAttack();
               Monster monster = obj.getMonster();

               CreateMonsterTagInfo tagInfo = monster.Tag as CreateMonsterTagInfo;
               if (monster != null && attack != null && monster.UniqueID != attack.UniqueID &&
                   tagInfo.ManagerType == (int)SceneUIClasses.CopyWolf)
               {
                   FortDead(monster);
               }
               else if (monster != null && attack != null && monster.UniqueID == attack.UniqueID)
               {
                   MonsterDead(monster);
               }
           }
        }

        public void processEvent(EventObjectEx eventObject)
        {
            if (eventObject.EventType == (int)EventTypes.OnCreateMonster)
            {
                OnCreateMonsterEventObject e = eventObject as OnCreateMonsterEventObject;
                if (null != e)
                {
                    CreateMonsterTagInfo tagInfo = e.Monster.Tag as CreateMonsterTagInfo;
                    if (null != tagInfo)
                    {
                        e.Monster.AllwaySearchEnemy = true;
                        if (tagInfo.IsFort) e.Monster.Camp = _runtimeData.CampID;

                        e.Result = true;
                        e.Handled = true;
                    }
                }
            }

            if (eventObject.EventType == (int)EventTypes.PreMonsterInjure)
            {
                PreMonsterInjureEventObject obj = eventObject as PreMonsterInjureEventObject;
                if (obj != null && obj.SceneType == (int)SceneUIClasses.CopyWolf)
                {
                    Monster attacker = obj.Attacker as Monster;
                    Monster fortMonster = obj.Monster;

                    if (attacker == null || fortMonster == null) return;

                    CreateMonsterTagInfo tagInfo = fortMonster.Tag as CreateMonsterTagInfo;
                    if (tagInfo != null)
                    {
                        int fubebSeqID = tagInfo.FuBenSeqId;
                        //int.TryParse(fortMonster.Tag.ToString(), out fubebSeqID);
                        if (fubebSeqID <= 0) return;

                        CopyWolfSceneInfo scene = null;
                        if (!_runtimeData.SceneDict.TryGetValue(fubebSeqID, out scene) || scene == null)
                            return;
                        //====Monsters===
                        //int injure = _runtimeData.GetMonsterHurt(attacker.MonsterInfo.ExtensionID);
                        ////injure = 1;
                        //int fortLife = (int)Math.Max(0, fortMonster.VLife - injure);
                        //scene.ScoreData.FortLifeNow = fortLife;
                        //scene.ScoreData.FortLifeMax = (int)fortMonster.MonsterInfo.VLifeMax;
                        //GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_COPY_WOLF_SCORE_INFO, scene.ScoreData, scene.CopyMapInfo);

                       // obj.Injure = injure;
                        eventObject.Handled = true;
                        eventObject.Result = true;
                    }
                }
            }
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        public bool InitConfig()
        {
            bool success = true;
            string fileName = "";
            XElement xml = null;
            IEnumerable<XElement> nodes;

            lock (_mutex)
            {
                try
                {
                    //怪物批次配置--------------------------------------------------------------------
                    _runtimeData.CopyWolfWaveDic.Clear();

                    fileName = Global.GameResPath("Config/LangHunYaoSai.xml");
                    xml = CheckHelper.LoadXml(fileName);
                    if (null == xml) return false;
                    nodes = xml.Elements();
                    foreach (var xmlItem in nodes)
                    {
                        if (xmlItem == null) continue;

                        CopyWolfWaveInfo config = new CopyWolfWaveInfo();
                        config.WaveID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));

                        config.MonsterList.Clear();
                        string[] monsterArr = Global.GetDefAttributeStr(xmlItem, "MonstersID", "0,0").Split('|');
                        foreach (string monster in monsterArr)
                        {
                            string[] m = monster.Split(',');
                            config.MonsterList.Add(new int[] { int.Parse(m[0]), int.Parse(m[1]) });
                        }

                        config.NextTime = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NextTime", "60"));

                        config.MonsterSiteDic.Clear();
                        string[] siteArr = Global.GetDefAttributeStr(xmlItem, "Site", "0,0,0").Split('|');
                        foreach (string site in siteArr)
                        {
                            string[] s = site.Split(',');

                            CopyWolfSiteInfo siteInfo = new CopyWolfSiteInfo();
                            siteInfo.X = int.Parse(s[0]);
                            siteInfo.Y = int.Parse(s[1]);
                            siteInfo.Radius = int.Parse(s[2]);

                            config.MonsterSiteDic.Add(siteInfo);
                        }

                        _runtimeData.CopyWolfWaveDic.Add(config.WaveID, config);
                    }

                    //怪物对要塞伤害--------------------------------------------------------------------
                    string[] monsterHurtArr = GameManager.systemParamsList.GetParamValueByName("LangHunYaoSaiMonstersHurt").Split('|');
                    foreach (string monsterHurt in monsterHurtArr)
                    {
                        string[] h = monsterHurt.Split(',');
                        _runtimeData.MonsterHurtDic.Add(int.Parse(h[0]), int.Parse(h[1]));
                    }

                    //积分系数--------------------------------------------------------------------
                    _runtimeData.ScoreRateTime = (int)GameManager.systemParamsList.GetParamValueIntByName("LangHunYaoSaiTimeNum");
                    _runtimeData.ScoreRateLife = (int)GameManager.systemParamsList.GetParamValueIntByName("LangHunYaoSaiLifeNum");

                    //要塞--------------------------------------------------------------------------------
                    int[] forts = GameManager.systemParamsList.GetParamValueIntArrayByName("LangHunYaoSaiMonsters");
                    _runtimeData.FortMonsterID = forts[0];
                    _runtimeData.FortSite.X = forts[1];
                    _runtimeData.FortSite.Y = forts[2];
                }
                catch (System.Exception ex)
                {
                    success = false;
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("加载xml配置文件:{0}, 失败。", fileName), ex);
                }
            }

            return success;
        }

        #endregion

        #region 副本

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public bool AddCopyScene(GameClient client, CopyMap copyMap)
        {
            if (copyMap.MapCode == _runtimeData.MapID)
            {
                int fuBenSeqId = copyMap.FuBenSeqID;
                int mapCode = copyMap.MapCode;
                lock (_mutex)
                {
                    CopyWolfSceneInfo newScene = null;
                    if (!_runtimeData.SceneDict.TryGetValue(fuBenSeqId, out newScene))
                    {
                        newScene = new CopyWolfSceneInfo();
                        newScene.CopyMapInfo = copyMap;
                        newScene.CleanAllInfo();
                        newScene.GameId = Global.GetClientKuaFuServerLoginData(client).GameId;
                        newScene.MapID = mapCode;
                        newScene.CopyID = copyMap.CopyMapID;
                        newScene.FuBenSeqId = fuBenSeqId;
                        newScene.PlayerCount = 1;

                        _runtimeData.SceneDict[fuBenSeqId] = newScene;
                    }
                    else
                    {
                        newScene.PlayerCount++;
                    }

                    client.ClientData.BattleWhichSide = _runtimeData.CampID;
                    copyMap.IsKuaFuCopy = true;
                    copyMap.SetRemoveTicks(TimeUtil.NOW() + _runtimeData.TotalSecs * TimeUtil.SECOND);
                    GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_COPY_WOLF_SCORE_INFO, newScene.ScoreData, newScene.CopyMapInfo);
                }

                //更新状态
                // ElementWarClient.getInstance().GameFuBenRoleChangeState(client.ClientData.RoleID, (int)KuaFuRoleStates.StartGame);
                // 开始游戏统计
                GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 1, 0, 0, (int)_gameType);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public bool RemoveCopyScene(CopyMap copyMap)
        {
            if (copyMap.MapCode == _runtimeData.MapID)
            {
                lock (_mutex)
                {
                    CopyWolfSceneInfo scene;
                    _runtimeData.SceneDict.TryRemove(copyMap.FuBenSeqID, out scene);
                }

                return true;
            }

            return false;
        }

        #endregion 副本

        #region 活动逻辑

        /// <summary>
        /// 心跳处理
        /// </summary>
        public void TimerProc()
        {
            long nowTicks = TimeUtil.NOW();
            if (nowTicks < _nextHeartBeatTicks)
                return;

            _nextHeartBeatTicks = nowTicks + 1020; //1020毫秒执行一次

            long nowSecond = nowTicks / 1000;
            foreach (CopyWolfSceneInfo scene in _runtimeData.SceneDict.Values)
            {
                lock (_mutex)
                {
                    int nID = scene.FuBenSeqId;
                    int nCopyID = scene.CopyID;
                    int nMapID = scene.MapID;

                    if (nID < 0 || nCopyID < 0 || nMapID < 0)
                        continue;

                    CopyMap copyMap = scene.CopyMapInfo;
                    if (scene.SceneStatus == GameSceneStatuses.STATUS_NULL)             // 如果处于空状态 -- 是否要切换到准备状态
                    {
                        scene.PrepareTime = nowSecond;
                        scene.BeginTime = nowSecond + _runtimeData.PrepareSecs;
                        scene.SceneStatus = GameSceneStatuses.STATUS_PREPARE;

                        scene.StateTimeData.GameType = (int)_gameType;
                        scene.StateTimeData.State = (int)scene.SceneStatus;
                        scene.StateTimeData.EndTicks = nowTicks + _runtimeData.PrepareSecs * 1000;
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMapInfo);
                    }
                    else if (scene.SceneStatus == GameSceneStatuses.STATUS_PREPARE)     // 场景战斗状态切换
                    {
                        if (nowSecond >= (scene.BeginTime))
                        {
                            scene.SceneStatus = GameSceneStatuses.STATUS_BEGIN;
                            scene.EndTime = nowSecond + _runtimeData.FightingSecs;

                            scene.StateTimeData.GameType = (int)_gameType;
                            scene.StateTimeData.State = (int)scene.SceneStatus;
                            scene.StateTimeData.EndTicks = nowTicks + _runtimeData.FightingSecs * 1000;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMapInfo);
                        }
                    }
                    else if (scene.SceneStatus == GameSceneStatuses.STATUS_BEGIN)       // 战斗开始
                    {
                        if (nowSecond >= scene.EndTime)
                        {
                            scene.SceneStatus = GameSceneStatuses.STATUS_END;
                            continue;
                        }

                        //要塞
                        if (scene.IsFortFlag <= 0) CreateFort(scene);

                        //检查怪物
                        bool bNeedCreateMonster = false;
                        lock (scene)
                        {
                            CopyWolfWaveInfo configInfo = _runtimeData.GetWaveConfig(scene.MonsterWave);
                            if (configInfo == null)
                            {
                                scene.MonsterWaveOld = 0;
                                scene.MonsterWave = 0;
                                scene.SceneStatus = GameSceneStatuses.STATUS_END;
                                continue;
                            }

                            //if (scene.MonsterWave >= scene.MonsterWaveTotal)
                            //{
                            //    scene.MonsterWaveOld = scene.MonsterWave;
                            //    scene.MonsterWave = 0;
                            //    scene.SceneStatus = GameSceneStatuses.STATUS_END;
                            //    continue;
                            //}

                            //刷新下一波
                            if (scene.CreateMonsterTime > 0 && nowSecond - scene.CreateMonsterTime >= configInfo.NextTime && configInfo.NextTime>0)
                                bNeedCreateMonster = true;

                            //怪物清除
                            if (scene.CreateMonsterTime > 0 && scene.IsMonsterFlag == 0 && scene.KilledMonsterHashSet.Count == scene.MonsterCountCreate)
                                bNeedCreateMonster = true;

                            if (scene.CreateMonsterTime <= 0)
                            {
                                bNeedCreateMonster = true;
                                scene.MonsterWave = 0;
                            }

                            if (bNeedCreateMonster)
                                CreateMonster(scene);
                        }
                    }
                    else if (scene.SceneStatus == GameSceneStatuses.STATUS_END)
                    {
                        int leftSecond = 0;
                        if (scene.MonsterWave >= scene.MonsterWaveTotal)
                            leftSecond = (int)Math.Max(0, nowSecond - scene.EndTime);
                        GiveAwards(scene, leftSecond);

                        //结算奖励
                        scene.SceneStatus = GameSceneStatuses.STATUS_AWARD;
                        scene.EndTime = nowSecond;
                        scene.LeaveTime = scene.EndTime + _runtimeData.ClearRolesSecs;

                        scene.StateTimeData.GameType = (int)_gameType;
                        scene.StateTimeData.State = (int)GameSceneStatuses.STATUS_END;
                        scene.StateTimeData.EndTicks = nowTicks + _runtimeData.ClearRolesSecs * 1000;
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMapInfo);
                    }
                    else if (scene.SceneStatus == GameSceneStatuses.STATUS_AWARD)         // 战斗结束
                    {
                        if (nowSecond >= scene.LeaveTime)
                        {
                            copyMap.SetRemoveTicks(scene.LeaveTime);
                            scene.SceneStatus = GameSceneStatuses.STATUS_CLEAR;

                            try
                            {
                                List<GameClient> objsList = copyMap.GetClientsList();
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
                                DataHelper.WriteExceptionLogEx(ex, "【狼魂要塞】清场调度异常");
                            }
                        }
                    }
                }
            }

            return;
        }

        /// <summary>
        /// 刷怪
        /// </summary>
        public void CreateMonster(CopyWolfSceneInfo scene, int upWave = 1)
        {
            CopyMap copyMap = scene.CopyMapInfo;
            CopyWolfWaveInfo waveConfig = null;

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(scene.MapID, out gameMap))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("【狼魂要塞】报错 地图配置 ID = {0}", scene.MapID));
                return;
            }

            //------------------------------------临时测试
            //if (scene.MonsterWave > 0)
            //{
            //    //置刷怪标记
            //    scene.IsMonsterFlag = 1;
            //    return;
            //}

            long nowTicket = TimeUtil.NOW();
            long nowSecond = nowTicket / 1000;

            lock (scene)
            {
                if (scene.MonsterWave >= scene.MonsterWaveTotal)
                {
                    scene.MonsterWaveOld = scene.MonsterWave;
                    scene.MonsterWave = 0;
                    scene.SceneStatus = GameSceneStatuses.STATUS_END;
                    return;
                }

                //置刷怪标记
                scene.IsMonsterFlag = 1;

                int wave = scene.MonsterWave + upWave;
                if (wave > scene.MonsterWaveTotal)
                    wave = scene.MonsterWaveTotal;

                waveConfig = _runtimeData.GetWaveConfig(wave);
                if (waveConfig == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("【狼魂要塞】报错 刷怪波次 = {0}", wave));
                    return;
                }

                scene.MonsterWave = wave; // 递增刷怪波数
                scene.CreateMonsterTime = nowSecond;

                int totalCount = 0;
                int monsterID = 0;
                int monsterCount = 0;
                int gridX = 0;
                int gridY = 0;
                int gridNum = 0;

                CreateMonsterTagInfo tagInfo = new CreateMonsterTagInfo();
                tagInfo.FuBenSeqId = scene.FuBenSeqId;
                tagInfo.IsFort = false;
                tagInfo.ManagerType = (int)SceneUIClasses.CopyWolf;

                foreach (CopyWolfSiteInfo siteInfo in waveConfig.MonsterSiteDic)
                {
                    gridX = gameMap.CorrectWidthPointToGridPoint(siteInfo.X + Global.GetRandomNumber(-siteInfo.Radius, siteInfo.Radius)) / gameMap.MapGridWidth;
                    gridY = gameMap.CorrectHeightPointToGridPoint(siteInfo.Y + Global.GetRandomNumber(-siteInfo.Radius, siteInfo.Radius)) / gameMap.MapGridHeight;

                    foreach (var monster in waveConfig.MonsterList)
                    {
                        monsterID = monster[0];
                        monsterCount = monster[1];
                        totalCount += monsterCount;
                        GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.MapID, monsterID, scene.CopyMapInfo.CopyMapID, monsterCount, gridX, gridY, gridNum, 0, SceneUIClasses.CopyWolf, tagInfo);
                        //break;
                    }
                    //break;
                }

                scene.MonsterCountCreate += totalCount;
                scene.ScoreData.Wave = waveConfig.WaveID;
                scene.ScoreData.EndTime = nowTicket + waveConfig.NextTime * 1000;
                //scene.ScoreData.MonsterCount += scene.MonsterCountCreate;

                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_COPY_WOLF_SCORE_INFO, scene.ScoreData, scene.CopyMapInfo);
            }
        }

        /// <summary>
        /// 刷要塞
        /// </summary>
        public void CreateFort(CopyWolfSceneInfo scene)
        {
            CopyMap copyMap = scene.CopyMapInfo;
            CopyWolfWaveInfo waveConfig = null;

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(scene.MapID, out gameMap))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("【狼魂要塞】报错 地图配置 ID = {0}", scene.MapID));
                return;
            }

            lock (scene)
            {
                if (scene.IsFortFlag > 0) return;

                //置刷怪标记
                scene.IsFortFlag = 1;

                int gridX = gameMap.CorrectWidthPointToGridPoint((int)_runtimeData.FortSite.X) / gameMap.MapGridWidth;
                int gridY = gameMap.CorrectHeightPointToGridPoint((int)_runtimeData.FortSite.Y) / gameMap.MapGridHeight;

                CreateMonsterTagInfo tagInfo = new CreateMonsterTagInfo();
                tagInfo.FuBenSeqId = scene.FuBenSeqId;
                tagInfo.IsFort = true;
                tagInfo.ManagerType = (int)SceneUIClasses.CopyWolf;

                GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.MapID, _runtimeData.FortMonsterID, scene.CopyMapInfo.CopyMapID, 1, gridX, gridY, 0, 0, SceneUIClasses.CopyWolf, tagInfo);

                XElement xml = GameManager.MonsterZoneMgr.AllMonstersXml;
                if (xml == null) return;

                XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", _runtimeData.FortMonsterID.ToString());
                if (monsterXml == null) return;

                int life = (int)Global.GetSafeAttributeLong(monsterXml, "MaxLife");
                scene.ScoreData.FortLifeNow = life;
                scene.ScoreData.FortLifeMax = life;
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_COPY_WOLF_SCORE_INFO, scene.ScoreData, scene.CopyMapInfo);
            }
        }

        //是否副本
        public bool IsCopyWolf(int fubenID)
        {
            return fubenID == _runtimeData.CopyID;
        }

        public void MonsterDead(Monster monster)
        {
             CreateMonsterTagInfo tagInfo = monster.Tag as CreateMonsterTagInfo;
             if (tagInfo == null) return;

             int fubebSeqID = tagInfo.FuBenSeqId;
            if (fubebSeqID < 0 || monster.CopyMapID < 0 || !IsCopyWolf(monster.CurrentMapCode))
                return;

            CopyWolfSceneInfo scene = null;
            if (!_runtimeData.SceneDict.TryGetValue(fubebSeqID, out scene) || scene == null) return;
            if (scene.SceneStatus >= GameSceneStatuses.STATUS_END) return;
           
            //如果是重复记录击杀同一只怪,则直接返回
            if (!scene.AddKilledMonster(monster))
                return;

            if (scene.SceneStatus >= GameSceneStatuses.STATUS_END)
                return;

            lock (scene)
            {
                if (scene.IsMonsterFlag == 1 && scene.KilledMonsterHashSet.Count == scene.MonsterCountCreate)
                {
                    scene.MonsterWaveOld = scene.MonsterWave;
                    if (scene.MonsterWave >= scene.MonsterWaveTotal)
                        scene.SceneStatus = GameSceneStatuses.STATUS_END;
                    else
                        scene.IsMonsterFlag = 0;
                }
            }//lock
        }

        /// <summary>
        // 杀怪接口
        /// </summary>
        public void MonsterDead(GameClient client, Monster monster)
        {
                //if(client == null && monster.SceneType == (int)SceneUIClasses.CopyWolf)
                //    scene.AddKilledMonster(monster))

            if (client.ClientData.FuBenSeqID < 0 || client.ClientData.CopyMapID < 0 || !IsCopyWolf(client.ClientData.FuBenID))
                return;

            CopyWolfSceneInfo scene = null;
            if (!_runtimeData.SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene) || scene == null)
                return;

            //如果是重复记录击杀同一只怪,则直接返回
            if (!scene.AddKilledMonster(monster))
                return;

            if (scene.SceneStatus >= GameSceneStatuses.STATUS_END)
                return;

            lock (scene)
            {

                //scene.MonsterCountKill++;
                //====Monsters===
                //int score = scene.AddMonsterScore(client.ClientData.RoleID, monster.MonsterInfo.WolfScore);
                //scene.ScoreData.RoleMonsterScore = scene.RoleMonsterScore;

                ////scene.ScoreData.MonsterCount -= 1;
                ////scene.ScoreData.MonsterCount = scene.ScoreData.MonsterCount < 0 ? 0 : scene.ScoreData.MonsterCount;
                //GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_COPY_WOLF_SCORE_INFO, scene.ScoreData, scene.CopyMapInfo);

                //if (scene.IsMonsterFlag == 1 && scene.KilledMonsterHashSet.Count == scene.MonsterCountCreate)
                //{
                //    scene.MonsterWaveOld = scene.MonsterWave;
                //    if (scene.MonsterWave >= scene.MonsterWaveTotal)
                //        scene.SceneStatus = GameSceneStatuses.STATUS_END;
                //    else
                //        scene.IsMonsterFlag = 0;
                //}
            }//lock
        }

        public void FortDead(Monster fortMonster)
        {
            CreateMonsterTagInfo tagInfo = fortMonster.Tag as CreateMonsterTagInfo;
            if (tagInfo == null) return;

            int fubebSeqID = tagInfo.FuBenSeqId;
            if (fubebSeqID < 0 || fortMonster.CopyMapID < 0 || !IsCopyWolf(fortMonster.CurrentMapCode))
                return;

            CopyWolfSceneInfo scene = null;
            if (!_runtimeData.SceneDict.TryGetValue(fubebSeqID, out scene) || scene == null) return;
            if (scene.SceneStatus >= GameSceneStatuses.STATUS_END) return;

            lock (scene)
            {
                scene.SceneStatus = GameSceneStatuses.STATUS_END;
                scene.ScoreData.FortLifeNow = 0;
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_COPY_WOLF_SCORE_INFO, scene.ScoreData, scene.CopyMapInfo);
            }//lock
        }

        /// <summary>
        /// 给奖励
        /// </summary>
        public void GiveAwards(CopyWolfSceneInfo scene, int leftSecond)
        {
            try
            {
                FuBenMapItem fuBenMapItem = FuBenManager.FindMapCodeByFuBenID(scene.CopyMapInfo.FubenMapID, scene.MapID);
                if (fuBenMapItem == null) return;

                int zhanLi = 0;
                List<GameClient> objsList = scene.CopyMapInfo.GetClientsList();
                if (objsList != null && objsList.Count > 0)
                {
                    for (int n = 0; n < objsList.Count; ++n)
                    {
                        GameClient client = objsList[n];
                        if (client != null && client == GameManager.ClientMgr.FindClient(client.ClientData.RoleID)) //确认角色仍然在线
                        {
                            int wave = scene.MonsterWaveOld;
                            if (wave > scene.MonsterWaveTotal) wave = scene.MonsterWaveTotal;

                            int scoreMonster = scene.GetMonsterScore(client.ClientData.RoleID);
                            int life = scene.ScoreData.FortLifeNow;
                            int scoreAll = GetScore(scoreMonster, leftSecond, life);

                            // 公式
                            long nExp = AwardExp(fuBenMapItem.Experience, scoreAll);
                            int money = AwardGoldBind(fuBenMapItem.Money1, scoreAll);
                            int wolfMoney = AwardWolfMoney(fuBenMapItem.WolfMoney, scoreAll);

                            if (nExp > 0)
                                GameManager.ClientMgr.ProcessRoleExperience(client, nExp, false);

                            if (money > 0)
                                GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, money, string.Format("副本{0}通关奖励", scene.CopyID), false);

                            if (wolfMoney > 0)
                                GameManager.ClientMgr.ModifyLangHunFenMoValue(client, wolfMoney, "狼魂要塞", true);

                            CopyWolfAwardsData awardsData = new CopyWolfAwardsData()
                            {
                                Wave = scene.MonsterWaveOld,
                                Exp = nExp,
                                Money = money,
                                WolfMoney = wolfMoney,
                                RoleScore = scene.GetMonsterScore(client.ClientData.RoleID)
                            };

                            //AddElementWarCount(client);
                            GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 0, 1, 0, (int)_gameType);

                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_COPY_WOLF_AWARD, awardsData);

                            zhanLi += client.ClientData.CombatForce;
                            Global.UpdateFuBenDataForQuickPassTimer(client, scene.CopyMapInfo.FubenMapID, 0, 1);
                        }
                    }
                }

                int roleCount = 0;
                if (objsList != null && objsList.Count > 0)
                {
                    roleCount = objsList.Count;
                    zhanLi = zhanLi / roleCount;
                }

                // ElementWarClient.getInstance().UpdateCopyPassEvent(scene.FuBenSeqId, roleCount, scene.MonsterWaveOld, zhanLi);
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteExceptionLogEx(ex, "【狼魂要塞】清场调度异常");
            }

        }

        public void NotifyTimeStateInfoAndScoreInfo(GameClient client, bool timeState = true, bool scoreInfo = true)
        {
            lock (_mutex)
            {
                CopyWolfSceneInfo scene;
                if (_runtimeData.SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    if (timeState)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData);
                    }

                    if (scoreInfo)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_COPY_WOLF_SCORE_INFO, scene.ScoreData);
                    }
                }
            }
        }

        /// <summary>
        /// 玩家离开
        /// </summary>
        public void LeaveFuBen(GameClient client)
        {
            //ElementWarClient.getInstance().GameFuBenRoleChangeState(client.ClientData.RoleID, (int)KuaFuRoleStates.None);

            CopyWolfSceneInfo scene = null;
            lock (_runtimeData.SceneDict)
            {
                if (!_runtimeData.SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    return;
                }
            }

            lock (scene)
            {
                scene.PlayerCount--;
                if (scene.SceneStatus != GameSceneStatuses.STATUS_END
                    && scene.SceneStatus != GameSceneStatuses.STATUS_AWARD
                    && scene.SceneStatus != GameSceneStatuses.STATUS_CLEAR)
                {
                    KuaFuManager.getInstance().SetCannotJoinKuaFu_UseAutoEndTicks(client);
                }
            }
        }

        /// <summary>
        /// 玩家离开
        /// </summary>
        public void OnLogout(GameClient client)
        {
            LeaveFuBen(client);
        }

        /// <summary>
        /// 角色复活
        /// </summary>
        public bool ClientRelive(GameClient client)
        {
            GameMap gameMap = null;
            if (GameManager.MapMgr.DictMaps.TryGetValue(_runtimeData.MapID, out gameMap)) //确认地图编号是否有效
            {
                int defaultBirthPosX = GameManager.MapMgr.DictMaps[_runtimeData.MapID].DefaultBirthPosX;
                int defaultBirthPosY = GameManager.MapMgr.DictMaps[_runtimeData.MapID].DefaultBirthPosY;
                int defaultBirthRadius = GameManager.MapMgr.DictMaps[_runtimeData.MapID].BirthRadius;

                //从配置根据地图取默认位置
                Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, _runtimeData.MapID, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);

                client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                client.ClientData.CurrentMagicV = client.ClientData.MagicV;

                client.ClientData.MoveAndActionNum = 0;

                //通知队友自己要复活
                GameManager.ClientMgr.NotifyTeamRealive(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.ClientData.RoleID, (int)newPos.X, (int)newPos.Y, -1);
                Global.ClientRealive(client, (int)newPos.X, (int)newPos.Y, -1);

                return true;
            }

            return false;
        }

        #endregion 活动逻辑


        #region 奖励相关

        /// <summary>
        /// 副本结束——统计积分
        /// </summary>
        public int GetScore(int monsterScore, int second, int life)
        {
            int timeScore = _runtimeData.ScoreRateTime * second;
            int lifeScore = _runtimeData.ScoreRateLife * life;
            return Math.Max(0, monsterScore + timeScore + lifeScore);
        }

        /// <summary>
        /// 奖励——经验 = 基础经验 * ( 100% + Min ( 积分 , 1000000 ) ^ 0.34 / 100 )
        /// </summary>
        public int AwardExp(int baseValue, int score)
        {
            return (int)(baseValue * (1 + Math.Pow(Math.Min(score, 1000000), 0.34) / 100));
        }

        /// <summary>
        /// 奖励——金币 = 基础金币 * ( 100% + Min ( 积分 , 500000 ) ^ 0.34 / 100 )
        /// </summary>
        public int AwardGoldBind(int baseValue, int score)
        {
            return (int)(baseValue * (1 + Math.Pow(Math.Min(score, 500000), 0.34) / 100));
        }

        /// <summary>
        /// 奖励——狼魂粉末= 200+int(Min( 积分,100000)/100)
        /// </summary>
        public int AwardWolfMoney(int baseValue, int score)
        {
            return (int)(200 + Math.Min(score, 100000) / 100);
        }

        #endregion

    }
}
