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
    public partial class KuaFuBossManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 运行时变量

        /// <summary>
        /// 血色城堡副本场景数据
        /// </summary>
        public ConcurrentDictionary<int, KuaFuBossScene> SceneDict = new ConcurrentDictionary<int, KuaFuBossScene>(); // KEY-副本ID VALUE- KEY-副本顺序ID VALUE-KuaFuBossScene信息

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
            if (sceneType == SceneUIClasses.KuaFuBoss)
            {
                int fuBenSeqId = copyMap.FuBenSeqID;
                int mapCode = copyMap.MapCode;
                int roleId = client.ClientData.RoleID;
                int gameId = (int)Global.GetClientKuaFuServerLoginData(client).GameId;
                DateTime now = TimeUtil.NowDateTime();
                KuaFuBossScene scene = null;
                List<BattleDynamicMonsterItem> dynMonsterList;
                lock (RuntimeData.Mutex)
                {
                    if (!SceneDict.TryGetValue(fuBenSeqId, out scene))
                    {
                        KuaFuBossSceneInfo sceneInfo = null;
                        YongZheZhanChangFuBenData fuBenData;
                        if (!RuntimeData.FuBenItemData.TryGetValue(gameId, out fuBenData))
                        {
                            LogManager.WriteLog(LogTypes.Error, "跨服Boss没有为副本找到对应的跨服副本数据,GameID:" + gameId);
                        }

                        if (!RuntimeData.SceneDataDict.TryGetValue(fuBenData.GroupIndex, out sceneInfo))
                        {
                            LogManager.WriteLog(LogTypes.Error, "跨服Boss没有为副本找到对应的档位数据,ID:" + fuBenData.GroupIndex);
                        }

                        scene = new KuaFuBossScene();
                        scene.CopyMap = copyMap;
                        scene.CleanAllInfo();
                        scene.GameId = gameId;
                        scene.m_nMapCode = mapCode;
                        scene.CopyMapId = copyMap.CopyMapID;
                        scene.FuBenSeqId = fuBenSeqId;
                        scene.m_nPlarerCount = 1;
                        scene.SceneInfo = sceneInfo;
                        DateTime startTime = now.Date.Add(GetStartTime(sceneInfo.Id));
                        scene.StartTimeTicks = startTime.Ticks / 10000;

                        scene.GameStatisticalData.GameId = gameId;

                        SceneDict[fuBenSeqId] = scene;
                        if (RuntimeData.SceneDynMonsterDict.TryGetValue(mapCode, out dynMonsterList))
                        {
                            scene.DynMonsterList = dynMonsterList;
                        }
                    }
                    else
                    {
                        scene.m_nPlarerCount++;
                    }

                    copyMap.IsKuaFuCopy = true;
                    copyMap.SetRemoveTicks(TimeUtil.NOW() + scene.SceneInfo.TotalSecs * TimeUtil.SECOND);

                    // 非首次进来的人会有旧的积分信息需要通知
					// 改为进入的时候，让客户端来主动查询
                   //NotifyTimeStateInfoAndScoreInfo(client, false, true, true);
                }

                GameMap gameMap = null;
                if (GameManager.MapMgr.DictMaps.TryGetValue(copyMap.MapCode, out gameMap))
                {
                    scene.MapGridWidth = gameMap.MapGridWidth;
                    scene.MapGridHeight = gameMap.MapGridHeight;
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
            if (sceneType == SceneUIClasses.KuaFuBoss)
            {
                lock (RuntimeData.Mutex)
                {
                    KuaFuBossScene KuaFuBossScene;
                    SceneDict.TryRemove(copyMap.FuBenSeqID, out KuaFuBossScene);
                }

                return true;
            }

            return false;
        }

        #endregion 副本

        #region 活动逻辑

        private void NotifySceneData(KuaFuBossScene scene)
        {
            if (null != scene)
            {
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_KUAFU_BOSS_DATA, scene.SceneStateData, scene.CopyMap);
            }
        }

        public void NotifyTimeStateInfoAndScoreInfo(GameClient client, bool timeState = true, bool sideScore = true)
        {
            lock (RuntimeData.Mutex)
            {
                KuaFuBossScene scene;
                if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    if (timeState)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData);
                    }

                    if (sideScore)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KUAFU_BOSS_DATA, scene.SceneStateData);
                    }
                }
            }
        }

        public void CheckCreateDynamicMonster(KuaFuBossScene scene, long nowMs)
        {
            lock (RuntimeData.Mutex)
            {
                List<BattleDynamicMonsterItem> dynMonsterList = scene.DynMonsterList;
                if (dynMonsterList == null)
                {
                    return;
                }

                foreach (var item in dynMonsterList)
                {
                    if (scene.DynMonsterSet.Contains(item.Id))
                    {
                        // 已经刷了
                        continue;
                    }

                    if (nowMs - scene.m_lBeginTime < item.DelayBirthMs)
                    {
                        // 未到时间
                        continue;
                    }

                    Monster seedMonster = GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.m_nMapCode, item.MonsterID, scene.CopyMap.CopyMapID, item.Num, 
                        item.PosX / scene.MapGridWidth, item.PosY / scene.MapGridHeight, item.Radius, item.PursuitRadius, ManagerType, null);
                    scene.DynMonsterSet.Add(item.Id);
                    if (null != seedMonster)
                    {
                        if (seedMonster.MonsterType == (int)MonsterTypes.BOSS)
                        {
                            scene.SceneStateData.TotalBossNum += item.Num;
                            scene.SceneStateData.BossNum += item.Num;
                        }
                        else
                        {
                            scene.SceneStateData.TotalNormalNum += item.Num;
                            scene.SceneStateData.MonsterNum += item.Num;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 伤害或击杀Boss的处理
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="injure"></param>
        public void OnKillMonster(GameClient client, Monster monster)
        {
            if (monster.ManagerType == ManagerType)
            {
                KuaFuBossScene scene;
                lock (RuntimeData.Mutex)
                {
                    if (SceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                    {
                        if (scene.m_eStatus != GameSceneStatuses.STATUS_BEGIN)
                        {
                            return;
                        }
#if ___CC___FUCK___YOU___BB___
                        scene.GameStatisticalData.MonsterDieTimeList.Add(monster.XMonsterInfo.MonsterId);
#else
                        scene.GameStatisticalData.MonsterDieTimeList.Add(monster.MonsterInfo.ExtensionID);
#endif
                        scene.GameStatisticalData.MonsterDieTimeList.Add(scene.ElapsedSeconds);

                        if (monster.MonsterType == (int)MonsterTypes.BOSS)
                        {
                            scene.SceneStateData.BossNum = Math.Max(0, scene.SceneStateData.BossNum - 1);
                            if (null != client)
                            {
#if ___CC___FUCK___YOU___BB___
                                string msgText = string.Format(Global.GetLang("【{0}】击杀了BOSS【{1}】！"), Global.FormatRoleName4(client), monster.XMonsterInfo.Name);
                                GameManager.ClientMgr.BroadSpecialCopyMapMsg(scene.CopyMap, msgText);
#else
                                 string msgText = string.Format(Global.GetLang("【{0}】击杀了BOSS【{1}】！"), Global.FormatRoleName4(client), monster.MonsterInfo.VSName);
                                GameManager.ClientMgr.BroadSpecialCopyMapMsg(scene.CopyMap, msgText);
#endif
                            }
                        }
                        else
                        {
                            scene.SceneStateData.MonsterNum = Math.Max(0, scene.SceneStateData.MonsterNum - 1);
                        }
                    }
                }

                NotifySceneData(scene);
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

                            scene.StateTimeData.GameType = (int)GameTypes.KuaFuBoss;
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

                            scene.StateTimeData.GameType = (int)GameTypes.KuaFuBoss;
                            scene.StateTimeData.State = (int)scene.m_eStatus;
                            scene.StateTimeData.EndTicks = scene.m_lEndTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)       // 战斗开始
                    {
                        if (ticks >= scene.m_lEndTime)
                        {
                            scene.m_eStatus = GameSceneStatuses.STATUS_END;
                            scene.m_lLeaveTime = scene.m_lEndTime + scene.SceneInfo.ClearRolesSecs * TimeUtil.SECOND;

                            scene.StateTimeData.GameType = (int)GameTypes.KuaFuBoss;
                            scene.StateTimeData.State = (int)GameSceneStatuses.STATUS_CLEAR;
                            scene.StateTimeData.EndTicks = scene.m_lLeaveTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);
                            NotifySceneData(scene);
                        }
                        else
                        {
                            scene.ElapsedSeconds = (int)Math.Min((nowTicks - scene.m_lBeginTime) / 1000, scene.SceneInfo.TotalSecs);
                            CheckCreateDynamicMonster(scene, ticks);
                            if (nowTicks > scene.NextNotifySceneStateDataTicks)
                            {
                                scene.NextNotifySceneStateDataTicks = nowTicks + 3000;
                                NotifySceneData(scene);
                            }
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_END)         // 战斗结束
                    {
                        GameManager.CopyMapMgr.KillAllMonster(scene.CopyMap);

                        //结算奖励
                        scene.m_eStatus = GameSceneStatuses.STATUS_AWARD;
                        YongZheZhanChangClient.getInstance().PushGameResultData(scene.GameStatisticalData);
                        YongZheZhanChangClient.getInstance().GameFuBenChangeState(scene.GameId, GameFuBenState.End, now);

                        YongZheZhanChangFuBenData fuBenData;
                        if (RuntimeData.FuBenItemData.TryGetValue(scene.GameId, out fuBenData))
                        {
                            fuBenData.State = GameFuBenState.End;
                            LogManager.WriteLog(LogTypes.Error, string.Format("跨服Boss跨服副本GameID={0},战斗结束", fuBenData.GameId));
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
                                DataHelper.WriteExceptionLogEx(ex, "跨服Boss系统清场调度异常");
                            }
                        }
                    }
                }
            }

            return;
        }

        /// <summary>
        /// 玩家离开血色堡垒
        /// </summary>
        public void LeaveFuBen(GameClient client)
        {
            lock (RuntimeData.Mutex)
            {
                KuaFuBossScene scene = null;
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
