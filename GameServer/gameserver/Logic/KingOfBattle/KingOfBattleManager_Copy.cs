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
using System.Collections.Concurrent;
using Tmsk.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 王者战场管理器
    /// </summary>
    public partial class KingOfBattleManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 运行时变量

        /// <summary>
        /// 王者战场副本场景数据
        /// </summary>
        public ConcurrentDictionary<int, KingOfBattleScene> SceneDict = new ConcurrentDictionary<int, KingOfBattleScene>(); // KEY-副本ID VALUE- KEY-副本顺序ID VALUE-KingOfBattleScene信息

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long NextHeartBeatTicks = 0L;

        #endregion 运行时变量

        #region 副本

        private void InitScene(KingOfBattleScene scene, GameClient client)
        {
            foreach (var item in RuntimeData.NPCID2QiZhiConfigDict.Values)
            {
                scene.NPCID2QiZhiConfigDict.Add(item.NPCID, item.Clone() as KingOfBattleQiZhiConfig);
            }
        }

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public bool AddCopyScenes(GameClient client, CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (sceneType == SceneUIClasses.KingOfBattle)
            {
                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(client.ClientData.MapCode, out gameMap))
                {
                    return false;
                }

                int fuBenSeqId = copyMap.FuBenSeqID;
                int mapCode = copyMap.MapCode;
                int roleId = client.ClientData.RoleID;
                int gameId = (int)Global.GetClientKuaFuServerLoginData(client).GameId;
                DateTime now = TimeUtil.NowDateTime();
                lock (RuntimeData.Mutex)
                {
                    KingOfBattleScene scene = null;
                    if (!SceneDict.TryGetValue(fuBenSeqId, out scene))
                    {
                        KingOfBattleSceneInfo sceneInfo = null;
                        YongZheZhanChangFuBenData fuBenData;
                        if (!RuntimeData.FuBenItemData.TryGetValue(gameId, out fuBenData))
                        {
                            LogManager.WriteLog(LogTypes.Error, "王者战场没有为副本找到对应的跨服副本数据,GameID:" + gameId);
                        }

                        if (!RuntimeData.SceneDataDict.TryGetValue(fuBenData.GroupIndex, out sceneInfo))
                        {
                            LogManager.WriteLog(LogTypes.Error, "王者战场没有为副本找到对应的档位数据,ID:" + fuBenData.GroupIndex);
                        }

                        scene = new KingOfBattleScene();
                        scene.CopyMap = copyMap;
                        scene.CleanAllInfo();
                        scene.GameId = gameId;
                        scene.m_nMapCode = mapCode;
                        scene.CopyMapId = copyMap.CopyMapID;
                        scene.FuBenSeqId = fuBenSeqId;
                        scene.m_nPlarerCount = 1;
                        scene.SceneInfo = sceneInfo;
                        scene.MapGridWidth = gameMap.MapGridWidth;
                        scene.MapGridHeight = gameMap.MapGridHeight;
                        DateTime startTime = now.Date.Add(GetStartTime(sceneInfo.Id));
                        scene.StartTimeTicks = startTime.Ticks / 10000;
                        InitScene(scene, client);
                        scene.GameStatisticalData.GameId = gameId;

                        SceneDict[fuBenSeqId] = scene;
                    }
                    else
                    {
                        scene.m_nPlarerCount++;
                    }

                    KingOfBattleClientContextData clientContextData;
                    if (!scene.ClientContextDataDict.TryGetValue(roleId, out clientContextData))
                    {
                        clientContextData = new KingOfBattleClientContextData() { RoleId = roleId, ServerId = client.ServerId, BattleWhichSide = client.ClientData.BattleWhichSide };
                        scene.ClientContextDataDict[roleId] = clientContextData;
                    }
                    else
                    {
                        clientContextData.KillNum = 0;
                    }

                    client.SceneObject = scene;
                    client.SceneGameId = scene.GameId;
                    client.SceneContextData2 = clientContextData;

                    copyMap.IsKuaFuCopy = true;
                    copyMap.SetRemoveTicks(TimeUtil.NOW() + scene.SceneInfo.TotalSecs * TimeUtil.SECOND);
                }

                //更新状态
                YongZheZhanChangClient.getInstance().GameFuBenRoleChangeState(roleId, (int)KuaFuRoleStates.StartGame);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public bool RemoveCopyScene(CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (sceneType == SceneUIClasses.KingOfBattle)
            {
                lock (RuntimeData.Mutex)
                {
                    KingOfBattleScene KingOfBattleScene;
                    SceneDict.TryRemove(copyMap.FuBenSeqID, out KingOfBattleScene);
                }

                return true;
            }

            return false;
        }

        #endregion 副本

        #region 活动逻辑

        public void OnCaiJiFinish(GameClient client, Monster monster)
        {
            KingOfBattleScene scene;
            BattleCrystalMonsterItem monsterItem;
            int addScore = 0;
            lock (RuntimeData.Mutex)
            {
                if (!SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    return;
                }

                if (scene.m_eStatus != GameSceneStatuses.STATUS_BEGIN)
                {
                    return;
                }

                monsterItem = monster.Tag as BattleCrystalMonsterItem;
                if (monsterItem == null) return;

                // 水晶复活
                BattleCrystalMonsterItem crystalItem = client.SceneContextData as BattleCrystalMonsterItem;
                if (null != crystalItem)
                {
                    AddDelayCreateMonster(scene, TimeUtil.NOW() + crystalItem.FuHuoTime, crystalItem);
                }

                // 采集BUFF
                UpdateBuff4GameClient(client, monsterItem.BuffGoodsID, monsterItem, true);
            }
        }

        /// </summary>
        /// 传送门
        /// </summary>
        public bool ClientChangeMap(GameClient client, int teleportID, ref int toNewMapCode, ref int toNewPosX, ref int toNewPosY)
        {
            KingOfBattleScene scene = client.SceneObject as KingOfBattleScene;
            if (null == scene)
                return false;

            int OpenGateSide = teleportID % 10;
            if (client.ClientData.BattleWhichSide != OpenGateSide)
                return false;

            if (!scene.SceneOpenTeleportList.Contains(teleportID))
                return false;

            return true;
        }

        /// <summary>
        /// 怪物死亡时
        /// </summary>
        /// <param name="npcID"></param>
        /// <param name="bhid"></param>
        public void OnProcessMonsterDead(GameClient client, Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            // 战旗
            if (null != client && (monster.XMonsterInfo.MonsterId == RuntimeData.BattleQiZhiMonsterID1 
                || monster.XMonsterInfo.MonsterId == RuntimeData.BattleQiZhiMonsterID2))
            {
                KingOfBattleScene scene = client.SceneObject as KingOfBattleScene;
                KingOfBattleQiZhiConfig qizhiConfig = monster.Tag as KingOfBattleQiZhiConfig;
                if (null != scene && null != qizhiConfig)
                {
                    lock (RuntimeData.Mutex)
                    {
                        qizhiConfig.DeadTicks = TimeUtil.NOW();
                        qizhiConfig.Alive = false;
                        qizhiConfig.BattleWhichSide = client.ClientData.BattleWhichSide;
                        CalculateTeleportGateState(scene);
                    }
                }
            }
#else
             // 战旗
            if (null != client && (monster.MonsterInfo.ExtensionID == RuntimeData.BattleQiZhiMonsterID1 
                || monster.MonsterInfo.ExtensionID == RuntimeData.BattleQiZhiMonsterID2))
            {
                KingOfBattleScene scene = client.SceneObject as KingOfBattleScene;
                KingOfBattleQiZhiConfig qizhiConfig = monster.Tag as KingOfBattleQiZhiConfig;
                if (null != scene && null != qizhiConfig)
                {
                    lock (RuntimeData.Mutex)
                    {
                        qizhiConfig.DeadTicks = TimeUtil.NOW();
                        qizhiConfig.Alive = false;
                        qizhiConfig.BattleWhichSide = client.ClientData.BattleWhichSide;
                        CalculateTeleportGateState(scene);
                    }
                }
            }
#endif

            // 杀塔
            KingOfBattleDynamicMonsterItem monsterConfig = monster.Tag as KingOfBattleDynamicMonsterItem;
            if (null != monsterConfig && (monsterConfig.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_TowerFirst
                || monsterConfig.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_TowerSecond))
            {
                KingOfBattleScene scene = null; // 尝试开本方光幕
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    CopyMap copyMap = scene.CopyMap;
                    string msgText = string.Format(Global.GetLang("【{0}】摧毁了敌方箭塔，激活了本方密道！"), Global.FormatRoleName4(client));
                    if(client.ClientData.BattleWhichSide == 1 && scene.GuangMuNotify1 == false)
                    {
                        scene.GuangMuNotify1 = true;
                        GameManager.CopyMapMgr.AddGuangMuEvent(copyMap, client.ClientData.BattleWhichSide, 0);
                        GameManager.ClientMgr.BroadSpecialCopyMapMsg(copyMap, msgText);
                    }
                    else if(client.ClientData.BattleWhichSide == 2 && scene.GuangMuNotify2 == false)
                    {
                        scene.GuangMuNotify2 = true;
                        GameManager.CopyMapMgr.AddGuangMuEvent(copyMap, client.ClientData.BattleWhichSide, 0);
                        GameManager.ClientMgr.BroadSpecialCopyMapMsg(copyMap, msgText);
                    }
                    msgText = string.Format(Global.GetLang("【{0}】摧毁了敌方箭塔！"), Global.FormatRoleName4(client));
                    GameManager.ClientMgr.BroadSpecialCopyMapMsg(copyMap, msgText);
                }
            }

            // 杀水晶堡垒
            if (null != monsterConfig && monsterConfig.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_BaoLei)
            {
                KingOfBattleScene scene = null;
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    ProcessEnd(scene, client.ClientData.BattleWhichSide, TimeUtil.NOW());
                }
            }

            // 杀Boss
            if (null != monsterConfig && monsterConfig.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_Boss)
            {
                KingOfBattleScene scene = null;
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    string msgText = string.Format(Global.GetLang("【{0}】击杀了BOSS！"), Global.FormatRoleName4(client));
                    GameManager.ClientMgr.BroadSpecialCopyMapMsg(scene.CopyMap, msgText);
                }
            }
        }

        /// <summary>
        /// 伤害或击杀Boss的处理
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="injure"></param>
        public void OnInjureMonster(GameClient client, Monster monster, long injure)
        {
            if (monster.MonsterType != (int)MonsterTypes.BOSS /*&& monster.MonsterType != (int)MonsterTypes.XianFactionGuard
                && monster.MonsterType != (int)MonsterTypes.MoFactionGuard && monster.MonsterType != (int)MonsterTypes.CampNoAttack*/)
                return;

            KingOfBattleClientContextData contextData = client.SceneContextData2 as KingOfBattleClientContextData;
            if (null == contextData)
                return;

            KingOfBattleDynamicMonsterItem tagInfo = monster.Tag as KingOfBattleDynamicMonsterItem;
            if (null == tagInfo)
                return;

            KingOfBattleScene scene = null;
            int addScore = 0;
            if (monster.HandledDead && monster.WhoKillMeID == client.ClientData.RoleID)
            {
                addScore += tagInfo.JiFenKill;           
            }

#if ___CC___FUCK___YOU___BB___
            double jiFenInjure = RuntimeData.KingBattleBossAttackPercent * monster.XMonsterInfo.MaxHP;

#else
         double jiFenInjure = RuntimeData.KingBattleBossAttackPercent * monster.MonsterInfo.VLifeMax;
#endif

            lock (RuntimeData.Mutex)
            {
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    if (scene.m_eStatus != GameSceneStatuses.STATUS_BEGIN)
                        return;

                    double InjureBossDelta = 0.0d;
#if ___CC___FUCK___YOU___BB___
                    contextData.InjureBossDeltaDict.TryGetValue(monster.XMonsterInfo.MonsterId, out InjureBossDelta);
#else
                    contextData.InjureBossDeltaDict.TryGetValue(monster.MonsterInfo.ExtensionID, out InjureBossDelta);
#endif

                    InjureBossDelta += injure;
                    if (InjureBossDelta >= jiFenInjure && jiFenInjure > 0.0)
                    {
                        // 求出达成的伤害倍数
                        int calcRate = (int)(InjureBossDelta / jiFenInjure);
                        InjureBossDelta -= jiFenInjure * calcRate;
                        addScore += tagInfo.JiFenDamage * calcRate;
                    }
#if ___CC___FUCK___YOU___BB___
                    contextData.InjureBossDeltaDict[monster.XMonsterInfo.MonsterId] = InjureBossDelta;
#else
                     contextData.InjureBossDeltaDict[monster.MonsterInfo.ExtensionID] = InjureBossDelta;
#endif
                    contextData.TotalScore += addScore;

                    // 处理Boss死亡重生
                    if (monster.HandledDead)
                    {
                        KingOfBattleDynamicMonsterItem RebornItem = null;
                        if (tagInfo.RebornID != -1 && RuntimeData.DynMonsterDict.TryGetValue(tagInfo.RebornID, out RebornItem))
                        {
                            long ticks = TimeUtil.NOW();
                            AddDelayCreateMonster(scene, ticks + RebornItem.DelayBirthMs, RebornItem);
                        }

                        // Buff
                        TryAddBossKillRandomBuff(client, tagInfo);
                    }

                    // 积分
                    if (client.ClientData.BattleWhichSide == 1)
                    {
                        scene.ScoreData.Score1 += addScore;
                    }
                    else if (client.ClientData.BattleWhichSide == 2)
                    {
                        scene.ScoreData.Score2 += addScore;
                    }

                    scene.GameStatisticalData.BossScore += addScore;
                }
            }

            if (addScore > 0 && scene != null)
            {
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_SIDE_SCORE, scene.ScoreData, scene.CopyMap);
                NotifyTimeStateInfoAndScoreInfo(client, false, false, true);
            }
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        private void ProcessEnd(KingOfBattleScene scene, int successSide, long nowTicks)
        {
            CompleteScene(scene, successSide);
            scene.m_eStatus = GameSceneStatuses.STATUS_END;
            scene.m_lLeaveTime = nowTicks + scene.SceneInfo.ClearRolesSecs * TimeUtil.SECOND;

            scene.StateTimeData.GameType = (int)GameTypes.KingOfBattle;
            scene.StateTimeData.State = (int)GameSceneStatuses.STATUS_CLEAR;
            scene.StateTimeData.EndTicks = scene.m_lLeaveTime;
            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);
        }

        /// <summary>
        /// 心跳处理
        /// </summary>
        public void TimerProc()
        {
            long nowTicks = TimeUtil.NOW();
            if (nowTicks < NextHeartBeatTicks)
            {
                return;
            }

            NextHeartBeatTicks = nowTicks + 1020; //1020毫秒执行一次

            foreach (var scene in SceneDict.Values)
            {
                lock (RuntimeData.Mutex)
                {
                    int nID = -1;
                    nID = scene.FuBenSeqId;

                    int nCopyID = -1;
                    nCopyID = scene.CopyMapId;

                    int nMapCodeID = -1;
                    nMapCodeID = scene.m_nMapCode;

                    if (nID < 0 || nCopyID < 0 || nMapCodeID < 0)
                        continue;

                    CopyMap copyMap = scene.CopyMap;

                    // 当前tick
                    DateTime now = TimeUtil.NowDateTime();
                    long ticks = TimeUtil.NOW();

                    // 战斗状态-准备 || 战斗状态-开始 更新
                    if (scene.m_eStatus == GameSceneStatuses.STATUS_PREPARE 
                        || scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                    {
                        CheckCreateDynamicMonster(scene, ticks);
                    }

                    if (scene.m_eStatus == GameSceneStatuses.STATUS_NULL)             // 如果处于空状态 -- 是否要切换到准备状态
                    {
                        if (ticks >= scene.StartTimeTicks)
                        {
                            scene.m_lPrepareTime = scene.StartTimeTicks;
                            scene.m_lBeginTime = scene.m_lPrepareTime + scene.SceneInfo.PrepareSecs * TimeUtil.SECOND;
                            scene.m_eStatus = GameSceneStatuses.STATUS_PREPARE;

                            scene.StateTimeData.GameType = (int)GameTypes.KingOfBattle;
                            scene.StateTimeData.State = (int)scene.m_eStatus;
                            scene.StateTimeData.EndTicks = scene.m_lBeginTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);

                            InitCreateDynamicMonster(scene);
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_PREPARE)     // 场景战斗状态切换
                    {
                        if (ticks >= scene.m_lBeginTime)
                        {
                            scene.m_eStatus = GameSceneStatuses.STATUS_BEGIN;
                            scene.m_lEndTime = scene.m_lBeginTime + scene.SceneInfo.FightingSecs * TimeUtil.SECOND;

                            scene.StateTimeData.GameType = (int)GameTypes.KingOfBattle;
                            scene.StateTimeData.State = (int)scene.m_eStatus;
                            scene.StateTimeData.EndTicks = scene.m_lEndTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);

                            // 打开光幕限制
                            for (int guangMuId = 3; guangMuId <= 8; ++guangMuId)
                                GameManager.CopyMapMgr.AddGuangMuEvent(copyMap, guangMuId, 0);
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)       // 战斗开始
                    {
                        if (ticks >= scene.m_lEndTime)
                        {
                            int successSide = 0;
                            if (scene.ScoreData.Score1 > scene.ScoreData.Score2)
                            {
                                successSide = 1;
                            }
                            else if (scene.ScoreData.Score2 > scene.ScoreData.Score1)
                            {
                                successSide = 2;
                            }
                            ProcessEnd(scene, successSide, ticks);
                        }
                        else
                        {
                            CheckSceneBufferTime(scene, ticks);
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_END)         // 战斗结束
                    {
                        GameManager.CopyMapMgr.KillAllMonster(scene.CopyMap);

                        //结算奖励
                        scene.m_eStatus = GameSceneStatuses.STATUS_AWARD;
                        YongZheZhanChangClient.getInstance().GameFuBenChangeState(scene.GameId, GameFuBenState.End, now);
                        GiveAwards(scene);

                        YongZheZhanChangFuBenData fuBenData;
                        if (RuntimeData.FuBenItemData.TryGetValue(scene.GameId, out fuBenData))
                        {
                            fuBenData.State = GameFuBenState.End;
                            LogManager.WriteLog(LogTypes.Error, string.Format("王者战场跨服副本GameID={0},战斗结束", fuBenData.GameId));
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_AWARD)
                    {
                        if (ticks >= scene.m_lLeaveTime)
                        {
                            copyMap.SetRemoveTicks(scene.m_lLeaveTime);
                            scene.m_eStatus = GameSceneStatuses.STATUS_CLEAR;

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
                                DataHelper.WriteExceptionLogEx(ex, "王者战场系统清场调度异常");
                            }
                        }
                    }
                }
            }

            return;
        }

        private void AddDelayCreateMonster(KingOfBattleScene scene, long ticks, object monster)
        {
            lock (RuntimeData.Mutex)
            {
                List<object> list = null;
                if (!scene.CreateMonsterQueue.TryGetValue(ticks, out list))
                {
                    list = new List<object>();
                    scene.CreateMonsterQueue.Add(ticks, list);
                }

                list.Add(monster);
            }
        }

        private void InitCreateDynamicMonster(KingOfBattleScene scene)
        {
            lock (RuntimeData.Mutex)
            {
                // 刷军旗
                foreach (var item in scene.NPCID2QiZhiConfigDict.Values)
                {
                    item.Alive = true;
                    if (item.QiZhiMonsterID == RuntimeData.BattleQiZhiMonsterID1)
                    {
                        item.BattleWhichSide = 1;
                    }
                    else if (item.QiZhiMonsterID == RuntimeData.BattleQiZhiMonsterID2)
                    {
                        item.BattleWhichSide = 2;
                    }
                    GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.m_nMapCode, item.QiZhiMonsterID, scene.CopyMapId, 1,
                        item.PosX / scene.MapGridWidth, item.PosY / scene.MapGridHeight, 0, 0, SceneUIClasses.KingOfBattle, item);
                }

                // 刷水晶怪
                foreach (var crystal in RuntimeData.BattleCrystalMonsterDict.Values)
                {
                    AddDelayCreateMonster(scene, scene.m_lPrepareTime, crystal);
                }

                List<KingOfBattleDynamicMonsterItem> dynMonsterList = null;
                if (RuntimeData.SceneDynMonsterDict.TryGetValue(scene.m_nMapCode, out dynMonsterList))
                {
                    foreach (var item in dynMonsterList)
                    {
                        if (item.RebornBirth == false) // 初始生成
                            AddDelayCreateMonster(scene, scene.m_lPrepareTime + item.DelayBirthMs, item);
                    }
                }
            }
        }

        public void CheckCreateDynamicMonster(KingOfBattleScene scene, long nowMs)
        {
            lock (RuntimeData.Mutex)
            {
                while (scene.CreateMonsterQueue.Count > 0)
                {
                    KeyValuePair<long, List<object>> pair = scene.CreateMonsterQueue.First();
                    if (nowMs < pair.Key)
                    {
                        break;
                    }

                    try
                    {
                        foreach (var obj in pair.Value)
                        {
                            if (obj is KingOfBattleDynamicMonsterItem)
                            {
                                KingOfBattleDynamicMonsterItem item = obj as KingOfBattleDynamicMonsterItem;
                                GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.m_nMapCode, item.MonsterID, scene.CopyMapId, 1,
                                    item.PosX / scene.MapGridWidth, item.PosY / scene.MapGridHeight, 0, item.PursuitRadius, SceneUIClasses.KingOfBattle, item);

                                if(item.MonsterType == (int)KingOfBattleMonsterType.KingOfBattle_Boss)
                                {
                                    string msgText = string.Format(Global.GetLang("【{0}】已刷新"), Global.GetMonsterNameByID(item.MonsterID));
                                    GameManager.ClientMgr.BroadSpecialCopyMapMsg(scene.CopyMap, msgText);
                                }
                            }
                            else if (obj is BattleCrystalMonsterItem)
                            {
                                BattleCrystalMonsterItem crystal = obj as BattleCrystalMonsterItem;
                                GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.m_nMapCode, crystal.MonsterID, scene.CopyMap.CopyMapID, 1,
                                    crystal.PosX / scene.MapGridWidth, crystal.PosY / scene.MapGridHeight, 0, 0, SceneUIClasses.KingOfBattle, crystal);
                            }
                        }
                    }
                    finally
                    {
                        scene.CreateMonsterQueue.RemoveAt(0);
                    }
                }
            }
        }

        public void NotifyTimeStateInfoAndScoreInfo(GameClient client, bool timeState = true, bool sideScore = true, bool selfScore = true)
        {
            lock (RuntimeData.Mutex)
            {
                KingOfBattleScene scene;
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    if (timeState)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData);
                    }

                    if (sideScore)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_SIDE_SCORE, scene.ScoreData);
                    }

                    if (selfScore)
                    {
                        KingOfBattleClientContextData clientContextData = client.SceneContextData2 as KingOfBattleClientContextData;
                        if (null != clientContextData)
                        {
                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_SELF_SCORE, clientContextData.TotalScore);
                        }
                    }
                }
            }
        }

        public void CompleteScene(KingOfBattleScene scene, int successSide)
        {
            scene.SuccessSide = successSide;
        }

        /// <summary>
        /// 删除角色身上的王者战场内Buff
        /// </summary>
        public void RemoveBattleSceneBuffForRole(KingOfBattleScene scene, GameClient client)
        {
            List<KingOfBattleSceneBuff> sceneBuffDeleteList = new List<KingOfBattleSceneBuff>();
            lock (RuntimeData.Mutex)
            {
                if (scene.SceneBuffDict.Count == 0)
                    return;

                foreach (var contextData in scene.SceneBuffDict.Values)
                {
                    if (contextData.RoleID == client.ClientData.RoleID)
                        sceneBuffDeleteList.Add(contextData);
                }

                if (sceneBuffDeleteList.Count == 0)
                    return;

                foreach (var contextData in sceneBuffDeleteList)
                {
                    if (contextData.RoleID != 0)
                    {
                        // 卸载Buff
                        UpdateBuff4GameClient(client, contextData.BuffID, contextData.tagInfo, false);               
                    }

                    // 水晶复活
                    BattleCrystalMonsterItem CrystalItem = contextData.tagInfo as BattleCrystalMonsterItem;
                    if (null != CrystalItem)
                    {
                        AddDelayCreateMonster(scene, TimeUtil.NOW() + CrystalItem.FuHuoTime, contextData.tagInfo);
                    }
                }
            }
        }

        public void OnKillRole(GameClient client, GameClient other)
        {
            lock (RuntimeData.Mutex)
            {
                KingOfBattleScene scene;
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    if (scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                    {
                        int addScore = 0;
                        int addScoreDie = RuntimeData.KingOfBattleDie;
                        KingOfBattleClientContextData clientLianShaContextData = client.SceneContextData2 as KingOfBattleClientContextData;
                        KingOfBattleClientContextData otherLianShaContextData = other.SceneContextData2 as KingOfBattleClientContextData;
                        HuanYingSiYuanLianSha huanYingSiYuanLianSha = null;
                        HuanYingSiYuanLianshaOver huanYingSiYuanLianshaOver = null;
                        HuanYingSiYuanAddScore huanYingSiYuanAddScore = new HuanYingSiYuanAddScore();

                        huanYingSiYuanAddScore.Name = Global.FormatRoleName4(client);
                        huanYingSiYuanAddScore.ZoneID = client.ClientData.ZoneID;
                        huanYingSiYuanAddScore.Side = client.ClientData.BattleWhichSide;
                        huanYingSiYuanAddScore.ByLianShaNum = 1;
                        huanYingSiYuanAddScore.RoleId = client.ClientData.RoleID;
                        huanYingSiYuanAddScore.Occupation = client.ClientData.Occupation;

                        //addScore += RuntimeData.WarriorBattlePk; //
                        scene.GameStatisticalData.KillScore += RuntimeData.KingOfBattleUltraKillParam1;
                        if (null != clientLianShaContextData)
                        {
                            clientLianShaContextData.KillNum++;
                            int lianShaScore = RuntimeData.KingOfBattleUltraKillParam1 + clientLianShaContextData.KillNum * RuntimeData.KingOfBattleUltraKillParam2;
                            lianShaScore = Math.Min(RuntimeData.KingOfBattleUltraKillParam4, Math.Max(RuntimeData.KingOfBattleUltraKillParam3, lianShaScore));

                            huanYingSiYuanAddScore.ByLianShaNum = 1;
                            huanYingSiYuanLianSha = new HuanYingSiYuanLianSha();
                            huanYingSiYuanLianSha.Name = huanYingSiYuanAddScore.Name;
                            huanYingSiYuanLianSha.ZoneID = huanYingSiYuanAddScore.ZoneID;
                            huanYingSiYuanLianSha.Occupation = huanYingSiYuanAddScore.Occupation;
                            // 每杀5人，连杀公告就更暴力，所以除以5，计算属于哪个连杀类型,最大为30 / 5
                            huanYingSiYuanLianSha.LianShaType = Math.Min(clientLianShaContextData.KillNum, 30) / 5;
                            huanYingSiYuanLianSha.ExtScore = lianShaScore;
                            huanYingSiYuanLianSha.Side = huanYingSiYuanAddScore.Side;
                            addScore += lianShaScore;
                            scene.GameStatisticalData.LianShaScore += lianShaScore;

                            //只在连杀数为5的倍数时推送消息
                            if ((clientLianShaContextData.KillNum % 5) != 0)
                            {
                                huanYingSiYuanLianSha = null;
                            }
                        }

                        if (null != otherLianShaContextData)
                        {
                            int overScore = RuntimeData.KingOfBattleShutDownParam1 + otherLianShaContextData.KillNum * RuntimeData.KingOfBattleShutDownParam2;
                            overScore = Math.Min(RuntimeData.KingOfBattleShutDownParam4, Math.Max(RuntimeData.KingOfBattleShutDownParam3, overScore));
                            addScore += overScore;
                            scene.GameStatisticalData.ZhongJieScore += overScore;
                            if (otherLianShaContextData.KillNum >= 10)
                            {
                                huanYingSiYuanLianshaOver = new HuanYingSiYuanLianshaOver();
                                huanYingSiYuanLianshaOver.KillerName = huanYingSiYuanAddScore.Name;
                                huanYingSiYuanLianshaOver.KillerZoneID = huanYingSiYuanAddScore.ZoneID;
                                huanYingSiYuanLianshaOver.KillerOccupation = client.ClientData.Occupation;
                                huanYingSiYuanLianshaOver.KillerSide = huanYingSiYuanAddScore.Side;
                                huanYingSiYuanLianshaOver.KilledName = Global.FormatRoleName4(other);
                                huanYingSiYuanLianshaOver.KilledZoneID = other.ClientData.ZoneID;
                                huanYingSiYuanLianshaOver.KilledOccupation = other.ClientData.Occupation;
                                huanYingSiYuanLianshaOver.KilledSide = other.ClientData.BattleWhichSide;
                                huanYingSiYuanLianshaOver.ExtScore = overScore;
                            }

                            otherLianShaContextData.KillNum = 0;
                            otherLianShaContextData.TotalScore += addScoreDie;
                            scene.GameStatisticalData.KillScore += addScoreDie;
                        }

                        huanYingSiYuanAddScore.Score = addScore;
                        if (client.ClientData.BattleWhichSide == 1)
                        {
                            scene.ScoreData.Score1 += addScore;
                            scene.ScoreData.Score2 += addScoreDie;
                        }
                        else
                        {
                            scene.ScoreData.Score2 += addScore;
                            scene.ScoreData.Score1 += addScoreDie;
                        }

                        if (null != clientLianShaContextData)
                        {
                            clientLianShaContextData.TotalScore += addScore;
                        }

                        //GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_HYSY_ADD_SCORE, huanYingSiYuanAddScore, huanYingSiYuanScene.CopyMap);
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_SIDE_SCORE, scene.ScoreData, scene.CopyMap);
                        if (null != huanYingSiYuanLianSha)
                        {
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_LIANSHA, huanYingSiYuanLianSha, scene.CopyMap);
                        }

                        if (null != huanYingSiYuanLianshaOver)
                        {
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_STOP_LIANSHA, huanYingSiYuanLianshaOver, scene.CopyMap);
                        }

                        NotifyTimeStateInfoAndScoreInfo(client, false, false, true);
                        NotifyTimeStateInfoAndScoreInfo(other, false, false, true);
                    }
                }
            }
        }

        /// <summary>
        /// 提交采集BUFF
        /// </summary>
        public void SubmitCrystalBuff(GameClient client, int areaLuaID)
        {
            if (areaLuaID != client.ClientData.BattleWhichSide)
                return;

            BattleCrystalMonsterItem crystalItem = client.SceneContextData as BattleCrystalMonsterItem;
            if (null == crystalItem)
                return;

            lock (RuntimeData.Mutex)
            {
                KingOfBattleScene scene;
                if (!SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                    return;

                // 加分
                KingOfBattleClientContextData contextData = client.SceneContextData2 as KingOfBattleClientContextData;
                if (null != contextData && scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                {
                    int addScore = 0;
                    addScore = crystalItem.BattleJiFen;
                    contextData.TotalScore += addScore;
                    scene.GameStatisticalData.CaiJiScore += addScore;
                    if (client.ClientData.BattleWhichSide == 1)
                    {
                        scene.ScoreData.Score1 += addScore;
                    }
                    else if (client.ClientData.BattleWhichSide == 2)
                    {
                        scene.ScoreData.Score2 += addScore;
                    }

                    if (addScore > 0)
                    {
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_SIDE_SCORE, scene.ScoreData, scene.CopyMap);
                        NotifyTimeStateInfoAndScoreInfo(client, false, false, true);
                    }
                }

                // 清除Buff
                UpdateBuff4GameClient(client, crystalItem.BuffGoodsID, crystalItem, false);

                // 水晶复活
                AddDelayCreateMonster(scene, TimeUtil.NOW() + crystalItem.FuHuoTime, crystalItem);
            }
        }

        /// <summary>
        /// 给奖励
        /// </summary>
        public void GiveAwards(KingOfBattleScene scene)
        {
            try
            {
                KingOfBattleStatisticalData gameResultData = scene.GameStatisticalData;
                foreach (var contextData in scene.ClientContextDataDict.Values)
                {
                    gameResultData.AllRoleCount++;
                    int success;
                    if (contextData.BattleWhichSide == scene.SuccessSide)
                    {
                        success = 1;
                        gameResultData.WinRoleCount++;
                    }
                    else
                    {
                        success = 0;
                        gameResultData.LoseRoleCount++;
                    }

                    GameClient client = GameManager.ClientMgr.FindClient(contextData.RoleId);
                    string awardsInfo = string.Format("{0},{1},{2},{3},{4}", scene.SceneInfo.Id, success, contextData.TotalScore, scene.ScoreData.Score1, scene.ScoreData.Score2);
                    if (client != null) //确认角色仍然在线
                    {
                        int score = contextData.TotalScore;
                        contextData.TotalScore = 0;
                        if (score >= RuntimeData.KingOfBattleLowestJiFen)
                        {
                            Global.SaveRoleParamsStringToDB(client, RoleParamName.KingOfBattleAwards, awardsInfo, true);
                        }
                        else
                        {
                            Global.SaveRoleParamsStringToDB(client, RoleParamName.KingOfBattleAwards, RuntimeData.RoleParamsAwardsDefaultString, true);
                        }

                        NtfCanGetAward(client, success, score, scene.SceneInfo, scene.ScoreData.Score1, scene.ScoreData.Score2);
                    }
                    else if (contextData.TotalScore >= RuntimeData.KingOfBattleLowestJiFen)
                    {
                        Global.UpdateRoleParamByNameOffline(contextData.RoleId, RoleParamName.KingOfBattleAwards, awardsInfo, contextData.ServerId);
                    }
                    else
                    {
                        Global.UpdateRoleParamByNameOffline(contextData.RoleId, RoleParamName.KingOfBattleAwards, RuntimeData.RoleParamsAwardsDefaultString, contextData.ServerId);
                    }
                }

                YongZheZhanChangClient.getInstance().PushGameResultData(gameResultData);
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteExceptionLogEx(ex, "王者战场系统清场调度异常");
            }
        }

        // 通知客户端有奖励可以领取
        private void NtfCanGetAward(GameClient client, int success, int score, KingOfBattleSceneInfo sceneInfo, int sideScore1, int sideScore2)
        {
            long addExp = 0;
            int addBindJinBi = 0;
            List<AwardsItemData> awardsItemDataList = null;
            if (score >= RuntimeData.KingOfBattleLowestJiFen)
            {
                //最终经验(取整万舍去尾数) = 经验系数 * (0.2 + Min(0.8, (积分 ^ 0.5) / 100)
                //最终金币(取整万舍去尾数) = 金币系数 * Min(100, (积分 ^ 0.5))
                addExp = (long)(sceneInfo.Exp * (0.2 + Math.Min(0.8, Math.Pow(score, 0.5) / 100)));
                addBindJinBi = (int)(sceneInfo.BandJinBi * Math.Min(100, Math.Pow(score, 0.5)));
                if (success > 0)
                {
                    awardsItemDataList = sceneInfo.WinAwardsItemList.Items;
                }
                else
                {
                    addExp = (long)(addExp * 0.8);
                    addBindJinBi = (int)(addBindJinBi * 0.8);
                    awardsItemDataList = sceneInfo.LoseAwardsItemList.Items;
                }

                addExp = addExp - (addExp % 10000);
                addBindJinBi = addBindJinBi - (addBindJinBi % 10000);
            }

            KingOfBattleAwardsData awardsData = new KingOfBattleAwardsData();
            awardsData.Exp = addExp;
            awardsData.BindJinBi = addBindJinBi;
            awardsData.Success = success;
            awardsData.AwardsItemDataList = awardsItemDataList;
            awardsData.SideScore1 = sideScore1;
            awardsData.SideScore2 = sideScore2;
            awardsData.SelfScore = score;

            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KINGOFBATTLE_AWARD, awardsData);
        }

        private int GiveRoleAwards(GameClient client, int success, int score, KingOfBattleSceneInfo sceneInfo)
        {
            long addExp = 0;
            int addBindJinBi = 0;
            List<AwardsItemData> awardsItemDataList = null;
            if (score >= RuntimeData.KingOfBattleLowestJiFen)
            {
                //最终经验(取整万舍去尾数) = 经验系数 * (0.2 + Min(0.8, (积分 ^ 0.5) / 100)
                //最终金币(取整万舍去尾数) = 金币系数 * Min(100, (积分 ^ 0.5))
                addExp = (long)(sceneInfo.Exp * (0.2 + Math.Min(0.8, Math.Pow(score, 0.5) / 100)));
                addBindJinBi = (int)(sceneInfo.BandJinBi * Math.Min(100, Math.Pow(score, 0.5)));
                if (success > 0)
                {
                    awardsItemDataList = sceneInfo.WinAwardsItemList.Items;
                }
                else
                {
                    addExp = (long)(addExp * 0.8);
                    addBindJinBi = (int)(addBindJinBi * 0.8);
                    awardsItemDataList = sceneInfo.LoseAwardsItemList.Items;
                }

                addExp = addExp - (addExp % 10000);
                addBindJinBi = addBindJinBi - (addBindJinBi % 10000);
            }

            if (awardsItemDataList != null && !Global.CanAddGoodsNum(client, awardsItemDataList.Count))
            {
                // 背包不足
                return StdErrorCode.Error_BagNum_Not_Enough;
            }

            if (addExp > 0)
            {
                GameManager.ClientMgr.ProcessRoleExperience(client, addExp);
            }

            if (addBindJinBi > 0)
            {
                GameManager.ClientMgr.AddMoney1(client, addBindJinBi, "王者战场奖励");
            }

            if (awardsItemDataList != null)
            {
                foreach (var item in awardsItemDataList)
                {
                    Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, item.GoodsID, item.GoodsNum, 0, "", item.Level, item.Binding, 0, "", true, 1, "王者战场奖励", Global.ConstGoodsEndTime, 0, 0, item.IsHaveLuckyProp, 0, item.ExcellencePorpValue, item.AppendLev);
                }
            }

            return StdErrorCode.Error_Success;
        }

        /// <summary>
        /// 玩家离开王者战场
        /// </summary>
        public void LeaveFuBen(GameClient client)
        {
            lock (RuntimeData.Mutex)
            {
                KingOfBattleScene scene = null;
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    scene.m_nPlarerCount--;

                    // 卸载王者战场内Buff
                    RemoveBattleSceneBuffForRole(scene, client);
                }
            }
        }

        /// <summary>
        /// 玩家离开王者战场
        /// </summary>
        public void OnLogout(GameClient client)
        {
            LeaveFuBen(client);
        }

        /// <summary>
        /// 检查场景BUFF
        /// </summary>
        private void CheckSceneBufferTime(KingOfBattleScene kingOfBattleScene, long nowTicks)
        {
            List<KingOfBattleSceneBuff> sceneBuffDeleteList = new List<KingOfBattleSceneBuff>();
            lock (RuntimeData.Mutex)
            {
                if (kingOfBattleScene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                {
                    if (kingOfBattleScene.SceneBuffDict.Count == 0)
                        return;

                    foreach (var contextData in kingOfBattleScene.SceneBuffDict.Values)
                    {
                        //处理超时情况
                        if (contextData.EndTicks < nowTicks)
                            sceneBuffDeleteList.Add(contextData);
                    }

                    if (sceneBuffDeleteList.Count == 0)
                        return;

                    foreach (var contextData in sceneBuffDeleteList)
                    {
                        if (contextData.RoleID != 0)
                        {
                            // 卸载Buff
                            GameClient client = GameManager.ClientMgr.FindClient(contextData.RoleID);
                            if (null != client)
                            {
                                UpdateBuff4GameClient(client, contextData.BuffID, contextData.tagInfo, false);
                            }
                        }

                        // 水晶复活
                        BattleCrystalMonsterItem CrystalItem = contextData.tagInfo as BattleCrystalMonsterItem;
                        if (null != CrystalItem)
                        {
                            AddDelayCreateMonster(kingOfBattleScene, TimeUtil.NOW() + CrystalItem.FuHuoTime, contextData.tagInfo);
                        }
                    }
                }
            }
        }

#endregion 活动逻辑
    }

}