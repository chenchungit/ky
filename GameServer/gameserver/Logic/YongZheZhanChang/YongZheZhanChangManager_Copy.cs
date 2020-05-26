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
    /// 天梯系统管理器
    /// </summary>
    public partial class YongZheZhanChangManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 运行时变量

        /// <summary>
        /// 血色城堡副本场景数据
        /// </summary>
        public ConcurrentDictionary<int, YongZheZhanChangScene> SceneDict = new ConcurrentDictionary<int, YongZheZhanChangScene>(); // KEY-副本ID VALUE- KEY-副本顺序ID VALUE-YongZheZhanChangScene信息

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long NextHeartBeatTicks = 0L;

        #endregion 运行时变量

        #region 副本

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public bool AddCopyScenes(GameClient client, CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (sceneType == SceneUIClasses.YongZheZhanChang)
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
                    YongZheZhanChangScene scene = null;
                    if (!SceneDict.TryGetValue(fuBenSeqId, out scene))
                    {
                        YongZheZhanChangSceneInfo sceneInfo = null;
                        YongZheZhanChangFuBenData fuBenData;
                        if (!RuntimeData.FuBenItemData.TryGetValue(gameId, out fuBenData))
                        {
                            LogManager.WriteLog(LogTypes.Error, "勇者战场没有为副本找到对应的跨服副本数据,GameID:" + gameId);
                        }

                        if (!RuntimeData.SceneDataDict.TryGetValue(fuBenData.GroupIndex, out sceneInfo))
                        {
                            LogManager.WriteLog(LogTypes.Error, "勇者战场没有为副本找到对应的档位数据,ID:" + fuBenData.GroupIndex);
                        }

                        scene = new YongZheZhanChangScene();
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

                        scene.GameStatisticalData.GameId = gameId;

                        SceneDict[fuBenSeqId] = scene;
                    }
                    else
                    {
                        scene.m_nPlarerCount++;
                    }

                    YongZheZhanChangClientContextData clientContextData;
                    if (!scene.ClientContextDataDict.TryGetValue(roleId, out clientContextData))
                    {
                        clientContextData = new YongZheZhanChangClientContextData() { RoleId = roleId, ServerId = client.ServerId, BattleWhichSide = client.ClientData.BattleWhichSide };
                        scene.ClientContextDataDict[roleId] = clientContextData;
                    }
                    else
                    {
                        clientContextData.KillNum = 0;
                    }

                    client.SceneContextData2 = clientContextData;

                    copyMap.IsKuaFuCopy = true;
                    copyMap.SetRemoveTicks(TimeUtil.NOW() + scene.SceneInfo.TotalSecs * TimeUtil.SECOND);

                    // 非首次进来的人会有旧的积分信息需要通知
					// 改为进入的时候，让客户端来主动查询
                   //NotifyTimeStateInfoAndScoreInfo(client, false, true, true);
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
            if (sceneType == SceneUIClasses.YongZheZhanChang)
            {
                lock (RuntimeData.Mutex)
                {
                    YongZheZhanChangScene YongZheZhanChangScene;
                    SceneDict.TryRemove(copyMap.FuBenSeqID, out YongZheZhanChangScene);
                }

                return true;
            }

            return false;
        }

        #endregion 副本

        #region 活动逻辑

        public void OnCaiJiFinish(GameClient client, Monster monster)
        {
            YongZheZhanChangScene scene;
            BattleCrystalMonsterItem monsterItem;
            int addScore = 0;
            lock(RuntimeData.Mutex)
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
                /*
                if (!RuntimeData.BattleCrystalMonsterDict.TryGetValue(monster.MonsterInfo.ExtensionID, out monsterItem))
                {
                    return;
                }*/

                YongZheZhanChangClientContextData contextData = client.SceneContextData2 as YongZheZhanChangClientContextData;
                if (null != contextData)
                {
                    addScore = monsterItem.BattleJiFen;
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
                }

                AddDelayCreateMonster(scene, TimeUtil.NOW() + monsterItem.FuHuoTime, monsterItem);
            }

            if (addScore > 0)
            {
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_YONGZHEZHANCHANG_SIDE_SCORE, scene.ScoreData, scene.CopyMap);
                NotifyTimeStateInfoAndScoreInfo(client, false, false, true);
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
            if (monster.MonsterType == (int)MonsterTypes.BOSS)
            {
                YongZheZhanChangClientContextData contextData = client.SceneContextData2 as YongZheZhanChangClientContextData;
                if (null != contextData)
                {
                    YongZheZhanChangScene scene = null;
                    int addScore = 0;
                    if (monster.HandledDead && monster.WhoKillMeID == client.ClientData.RoleID)
                    {
                        addScore += RuntimeData.WarriorBattleBOssLastAttack;
                    }
#if ___CC___FUCK___YOU___BB___
                    double jiFenInjure = RuntimeData.WarriorBattleBossAttackPercent * monster.XMonsterInfo.MaxHP;
#else
                    double jiFenInjure = RuntimeData.WarriorBattleBossAttackPercent * monster.MonsterInfo.VLifeMax;
#endif
                    contextData.InjureBossDelta += injure;
                    if (contextData.InjureBossDelta >= jiFenInjure && jiFenInjure > 0.0)
                    {
                        // 求出达成的伤害倍数
                        int calcRate = (int)(contextData.InjureBossDelta / jiFenInjure);
                        contextData.InjureBossDelta -=  jiFenInjure * calcRate;
                        addScore += RuntimeData.WarriorBattleBossAttackScore * calcRate;
                    }

                    lock (RuntimeData.Mutex)
                    {
                        contextData.TotalScore += addScore;
                        if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                        {
                            if (scene.m_eStatus != GameSceneStatuses.STATUS_BEGIN)
                            {
                                return;
                            }

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
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_YONGZHEZHANCHANG_SIDE_SCORE, scene.ScoreData, scene.CopyMap);
                        NotifyTimeStateInfoAndScoreInfo(client, false, false, true);
                    }
                }
            }
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

                    if (scene.m_eStatus == GameSceneStatuses.STATUS_NULL)             // 如果处于空状态 -- 是否要切换到准备状态
                    {
                        if (ticks >= scene.StartTimeTicks)
                        {
                            scene.m_lPrepareTime = scene.StartTimeTicks;
                            scene.m_lBeginTime = scene.m_lPrepareTime + scene.SceneInfo.PrepareSecs * TimeUtil.SECOND;
                            scene.m_eStatus = GameSceneStatuses.STATUS_PREPARE;

                            scene.StateTimeData.GameType = (int)GameTypes.YongZheZhanChang;
                            scene.StateTimeData.State = (int)scene.m_eStatus;
                            scene.StateTimeData.EndTicks = scene.m_lBeginTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_PREPARE)     // 场景战斗状态切换
                    {
                        if (ticks >= scene.m_lBeginTime)
                        {
                            scene.m_eStatus = GameSceneStatuses.STATUS_BEGIN;
                            scene.m_lEndTime = scene.m_lBeginTime + scene.SceneInfo.FightingSecs * TimeUtil.SECOND;

                            scene.StateTimeData.GameType = (int)GameTypes.YongZheZhanChang;
                            scene.StateTimeData.State = (int)scene.m_eStatus;
                            scene.StateTimeData.EndTicks = scene.m_lEndTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);

                            InitCreateDynamicMonster(scene);

                            copyMap.AddGuangMuEvent(1, 0);
                            GameManager.ClientMgr.BroadSpecialMapAIEvent(copyMap.MapCode, copyMap.CopyMapID, 1, 0);
                            copyMap.AddGuangMuEvent(2, 0);
                            GameManager.ClientMgr.BroadSpecialMapAIEvent(copyMap.MapCode, copyMap.CopyMapID, 2, 0);
                            copyMap.AddGuangMuEvent(3, 0);
                            GameManager.ClientMgr.BroadSpecialMapAIEvent(copyMap.MapCode, copyMap.CopyMapID, 3, 0);
                            copyMap.AddGuangMuEvent(4, 0);
                            GameManager.ClientMgr.BroadSpecialMapAIEvent(copyMap.MapCode, copyMap.CopyMapID, 4, 0);
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

                            CompleteScene(scene, successSide);
                            scene.m_eStatus = GameSceneStatuses.STATUS_END;
                            scene.m_lLeaveTime = scene.m_lEndTime + scene.SceneInfo.ClearRolesSecs * TimeUtil.SECOND;

                            scene.StateTimeData.GameType = (int)GameTypes.YongZheZhanChang;
                            scene.StateTimeData.State = (int)GameSceneStatuses.STATUS_CLEAR;
                            scene.StateTimeData.EndTicks = scene.m_lLeaveTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);
                        }
                        else
                        {
                            CheckCreateDynamicMonster(scene, ticks);
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
                            LogManager.WriteLog(LogTypes.Error, string.Format("勇者战场跨服副本GameID={0},战斗结束", fuBenData.GameId));
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
                                DataHelper.WriteExceptionLogEx(ex, "勇者战场系统清场调度异常");
                            }
                        }
                    }
                }
            }

            return;
        }

        // 刷勇者战场水晶采集物
        private void CreateCrystalMonster(YongZheZhanChangScene scene, BattleCrystalMonsterItem crystal)
        {
            GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.m_nMapCode, crystal.MonsterID, scene.CopyMapId, 1, 
                crystal.PosX / scene.MapGridWidth, crystal.PosY / scene.MapGridHeight, 0, 0, SceneUIClasses.YongZheZhanChang, crystal);
        }

        private void AddDelayCreateMonster(YongZheZhanChangScene scene, long ticks, object monster)
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

        private void InitCreateDynamicMonster(YongZheZhanChangScene scene)
        {
            lock (RuntimeData.Mutex)
            {
                // 刷水晶怪
                foreach (var crystal in RuntimeData.BattleCrystalMonsterDict.Values)
                {
                    AddDelayCreateMonster(scene, scene.m_lBeginTime, crystal);
                }

                List<BattleDynamicMonsterItem> dynMonsterList = null;
                if (RuntimeData.SceneDynMonsterDict.TryGetValue(scene.m_nMapCode, out dynMonsterList))
                {
                    foreach (var item in dynMonsterList)
                    {
                        AddDelayCreateMonster(scene, scene.m_lBeginTime + item.DelayBirthMs, item);
                    }
                }
            }
        }

        public void CheckCreateDynamicMonster(YongZheZhanChangScene scene, long nowMs)
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
                            if (obj is BattleDynamicMonsterItem)
                            {
                                BattleDynamicMonsterItem crystal = obj as BattleDynamicMonsterItem;
                                GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.m_nMapCode, crystal.MonsterID, scene.CopyMapId, 1,
                                    crystal.PosX / scene.MapGridWidth, crystal.PosY / scene.MapGridHeight, 0, 0, SceneUIClasses.YongZheZhanChang, crystal);
                            }
                            else if (obj is BattleCrystalMonsterItem)
                            {
                                BattleCrystalMonsterItem item = obj as BattleCrystalMonsterItem;
                                GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.m_nMapCode, item.MonsterID, scene.CopyMap.CopyMapID, 1, item.PosX / scene.MapGridWidth, item.PosY / scene.MapGridHeight, 0, 0, SceneUIClasses.YongZheZhanChang, item);
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
                YongZheZhanChangScene scene;
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    if (timeState)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData);
                    }

                    if (sideScore)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_YONGZHEZHANCHANG_SIDE_SCORE, scene.ScoreData);
                    }

                    if (selfScore)
                    {
                        YongZheZhanChangClientContextData clientContextData = client.SceneContextData2 as YongZheZhanChangClientContextData;
                        if (null != clientContextData)
                        {
                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_YONGZHEZHANCHANG_SELF_SCORE, clientContextData.TotalScore);
                        }
                    }
                }
            }
        }

        public void CompleteScene(YongZheZhanChangScene scene, int successSide)
        {
            scene.SuccessSide = successSide;
        }

        public void OnKillRole(GameClient client, GameClient other)
        {
            lock (RuntimeData.Mutex)
            {
                YongZheZhanChangScene scene;
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    if (scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                    {
                        int addScore = 0;
                        int addScoreDie = RuntimeData.WarriorBattleDie;
                        YongZheZhanChangClientContextData clientLianShaContextData = client.SceneContextData2 as YongZheZhanChangClientContextData;
                        YongZheZhanChangClientContextData otherLianShaContextData = other.SceneContextData2 as YongZheZhanChangClientContextData;
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
                        scene.GameStatisticalData.KillScore += RuntimeData.WarriorBattleUltraKillParam1;
                        if (null != clientLianShaContextData)
                        {
                            clientLianShaContextData.KillNum++;
                            int lianShaScore = RuntimeData.WarriorBattleUltraKillParam1 + clientLianShaContextData.KillNum * RuntimeData.WarriorBattleUltraKillParam2;
                            lianShaScore = Math.Min(RuntimeData.WarriorBattleUltraKillParam4, Math.Max(RuntimeData.WarriorBattleUltraKillParam3, lianShaScore));

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
                            int overScore = RuntimeData.WarriorBattleShutDownParam1 + otherLianShaContextData.KillNum * RuntimeData.WarriorBattleShutDownParam2;
                            overScore = Math.Min(RuntimeData.WarriorBattleShutDownParam4, Math.Max(RuntimeData.WarriorBattleShutDownParam3, overScore));
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
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_YONGZHEZHANCHANG_SIDE_SCORE, scene.ScoreData, scene.CopyMap);
                        if (null != huanYingSiYuanLianSha)
                        {
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_YONGZHEZHANCHANG_LIANSHA, huanYingSiYuanLianSha, scene.CopyMap);
                        }

                        if (null != huanYingSiYuanLianshaOver)
                        {
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_YONGZHEZHANCHANG_STOP_LIANSHA, huanYingSiYuanLianshaOver, scene.CopyMap);
                        }

                        NotifyTimeStateInfoAndScoreInfo(client, false, false, true);
                        NotifyTimeStateInfoAndScoreInfo(other, false, false, true);
                    }
                }
            }
        }

        /// <summary>
        /// 给奖励
        /// </summary>
        public void GiveAwards(YongZheZhanChangScene scene)
        {
            try
            {
                YongZheZhanChangStatisticalData gameResultData = scene.GameStatisticalData;
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
                        if (score >= RuntimeData.WarriorBattleLowestJiFen)
                        {
                            Global.SaveRoleParamsStringToDB(client, RoleParamName.YongZheZhanChangAwards, awardsInfo, true);
                        }
                        else
                        {
                            Global.SaveRoleParamsStringToDB(client, RoleParamName.YongZheZhanChangAwards, RuntimeData.RoleParamsAwardsDefaultString, true);
                        }

                        NtfCanGetAward(client, success, score, scene.SceneInfo, scene.ScoreData.Score1, scene.ScoreData.Score2);
                    }
                    else if (contextData.TotalScore >= RuntimeData.WarriorBattleLowestJiFen)
                    {
                        Global.UpdateRoleParamByNameOffline(contextData.RoleId, RoleParamName.YongZheZhanChangAwards, awardsInfo, contextData.ServerId);
                    }
                    else
                    {
                        Global.UpdateRoleParamByNameOffline(contextData.RoleId, RoleParamName.YongZheZhanChangAwards, RuntimeData.RoleParamsAwardsDefaultString, contextData.ServerId);
                    }
                }

                YongZheZhanChangClient.getInstance().PushGameResultData(gameResultData);
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteExceptionLogEx(ex, "天梯系统清场调度异常");
            }
        }

        // 通知客户端有奖励可以领取
        private void NtfCanGetAward(GameClient client, int success, int score, YongZheZhanChangSceneInfo sceneInfo, int sideScore1, int sideScore2)
        {
            long addExp = 0;
            int addBindJinBi = 0;
            List<AwardsItemData> awardsItemDataList = null;
            if (score >= RuntimeData.WarriorBattleLowestJiFen)
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

            YongZheZhanChangAwardsData awardsData = new YongZheZhanChangAwardsData();
            awardsData.Exp = addExp;
            awardsData.BindJinBi = addBindJinBi;
            awardsData.Success = success;
            awardsData.AwardsItemDataList = awardsItemDataList;
            awardsData.SideScore1 = sideScore1;
            awardsData.SideScore2 = sideScore2;
            awardsData.SelfScore = score;

            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_YONGZHEZHANCHANG_AWARD, awardsData);
        }

        private int GiveRoleAwards(GameClient client, int success, int score, YongZheZhanChangSceneInfo sceneInfo)
        {
            long addExp = 0;
            int addBindJinBi = 0;
            List<AwardsItemData> awardsItemDataList = null;
            if (score >= RuntimeData.WarriorBattleLowestJiFen)
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
                GameManager.ClientMgr.AddMoney1(client, addBindJinBi, "勇者战场奖励");
            }

            if (awardsItemDataList != null)
            {
                foreach (var item in awardsItemDataList)
                {
                    Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, item.GoodsID, item.GoodsNum, 0, "", item.Level, item.Binding, 0, "", true, 1, "勇者战场奖励", Global.ConstGoodsEndTime, 0, 0, item.IsHaveLuckyProp, 0, item.ExcellencePorpValue, item.AppendLev);
                }
            }

            return StdErrorCode.Error_Success;
        }

        /// <summary>
        /// 玩家离开血色堡垒
        /// </summary>
        public void LeaveFuBen(GameClient client)
        {
            lock (RuntimeData.Mutex)
            {
                YongZheZhanChangScene scene = null;
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    scene.m_nPlarerCount--;
                }
            }
        }

        /// <summary>
        /// 玩家离开血色堡垒
        /// </summary>
        public void OnLogout(GameClient client)
        {
            LeaveFuBen(client);
        }

#endregion 活动逻辑
     }
}
