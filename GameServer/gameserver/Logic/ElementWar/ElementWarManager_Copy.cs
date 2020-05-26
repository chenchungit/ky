using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Server;
using KF.Client;
using KF.Contract.Data;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;

namespace GameServer.Logic
{
    public partial class ElementWarManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 运行时变量

        /// <summary>
        /// 线程锁对象
        /// </summary>
        public static object _mutex = new object();

        /// <summary>
        /// 副本场景数据
        /// KEY-副本顺序ID VALUE-Scene信息
        /// </summary>
        public ConcurrentDictionary<int, ElementWarScene> _sceneDict = new ConcurrentDictionary<int, ElementWarScene>(); 

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long _nextHeartBeatTicks = 0L;

        #endregion 运行时变量

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
                    ElementWarScene newScene = null;
                    if (!_sceneDict.TryGetValue(fuBenSeqId, out newScene))
                    {
                        newScene = new ElementWarScene();
                        newScene.CopyMapInfo = copyMap;
                        newScene.CleanAllInfo();
                        newScene.GameId = Global.GetClientKuaFuServerLoginData(client).GameId;
                        newScene.MapID = mapCode;
                        newScene.CopyID = copyMap.CopyMapID;
                        newScene.FuBenSeqId = fuBenSeqId;
                        newScene.PlayerCount = 1;

                        _sceneDict[fuBenSeqId] = newScene;
                    }
                    else
                    {
                        newScene.PlayerCount++;
                    }

                    copyMap.IsKuaFuCopy = true;
                    copyMap.SetRemoveTicks(TimeUtil.NOW() + _runtimeData.TotalSecs * TimeUtil.SECOND);
                    GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_SCORE_INFO, newScene.ScoreData, newScene.CopyMapInfo);
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
                    ElementWarScene scene;
                    _sceneDict.TryRemove(copyMap.FuBenSeqID, out scene);
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
            foreach (ElementWarScene scene in _sceneDict.Values)
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
                        scene.StateTimeData.EndTicks = nowTicks + _runtimeData.PrepareSecs * 1000;//scene.BeginTime;
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
                            scene.StateTimeData.EndTicks = nowTicks + _runtimeData.FightingSecs * 1000;//scene.EndTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMapInfo);

                            //放开光幕
                            //copyMap.AddGuangMuEvent(1, 0);
                            //GameManager.ClientMgr.BroadSpecialMapAIEvent(copyMap.MapCode, copyMap.CopyMapID, 1, 0);
                        }
                    }
                    else if (scene.SceneStatus == GameSceneStatuses.STATUS_BEGIN)       // 战斗开始
                    {
                        if (nowSecond >= scene.EndTime)
                        {
                            scene.SceneStatus = GameSceneStatuses.STATUS_END;
                            continue;
                        }

                        //检查怪物
                        bool bNeedCreateMonster = false;
                        int upWave = 0;
                        lock (scene)
                        {
                            ElementWarMonsterConfigInfo configInfo = _runtimeData.GetOrderConfig(scene.MonsterWave);
                            if (configInfo == null)
                            {
                                scene.MonsterWaveOld = 1;
                                scene.MonsterWave = 0;
                                scene.SceneStatus = GameSceneStatuses.STATUS_END;
                                continue;
                            }
                            //检查超时
                            if (scene.CreateMonsterTime > 0 && nowSecond - scene.CreateMonsterTime >= configInfo.Up1)
                            {
                                scene.MonsterWave = 0;
                                scene.SceneStatus = GameSceneStatuses.STATUS_END;
                                continue;
                            }

                            if (scene.CreateMonsterTime > 0 && scene.IsMonsterFlag == 0 && scene.ScoreData.MonsterCount <= 0)
                            {
                                if (scene.MonsterWave < scene.MonsterWaveTotal)
                                {
                                    bNeedCreateMonster = true;
                                    if (nowSecond - scene.CreateMonsterTime <= configInfo.Up4)
                                        upWave = 4;
                                    else if (nowSecond - scene.CreateMonsterTime <= configInfo.Up3)
                                        upWave = 3;
                                    else if (nowSecond - scene.CreateMonsterTime <= configInfo.Up2)
                                        upWave = 2;
                                    else if (nowSecond - scene.CreateMonsterTime < configInfo.Up1)
                                        upWave = 1;
                                }
                                else
                                {
                                    scene.MonsterWaveOld = scene.MonsterWave;
                                    scene.MonsterWave = 0;
                                    scene.SceneStatus = GameSceneStatuses.STATUS_END;
                                    continue;
                                }
                            }

                            if (scene.CreateMonsterTime <= 0)
                                bNeedCreateMonster = true;

                            if (bNeedCreateMonster)
                            {
                                CreateMonster(scene, upWave);
                            }
                        }
                    }
                    else if (scene.SceneStatus == GameSceneStatuses.STATUS_END)
                    {
                        GiveAwards(scene);

                        //结算奖励
                        scene.SceneStatus = GameSceneStatuses.STATUS_AWARD;
                        scene.EndTime = nowSecond;
                        scene.LeaveTime = scene.EndTime + _runtimeData.ClearRolesSecs;

                      //  ElementWarClient.getInstance().GameFuBenChangeState(scene.GameId, GameFuBenState.End, DateTime.Now);

                        scene.StateTimeData.GameType = (int)_gameType;
                        scene.StateTimeData.State = (int)GameSceneStatuses.STATUS_END;
                        scene.StateTimeData.EndTicks = nowTicks + _runtimeData.ClearRolesSecs * 1000;//scene.LeaveTime;
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
                                DataHelper.WriteExceptionLogEx(ex, "【元素试炼】清场调度异常");
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
        public void CreateMonster(ElementWarScene scene,int upWave)
        {
            CopyMap copyMap = scene.CopyMapInfo;
            ElementWarMonsterConfigInfo waveConfig = null;

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(scene.MapID, out gameMap))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("元素试炼报错 地图配置 ID = {0}", scene.MapID));
                return;
            }

            long nowTicket = TimeUtil.NOW();
            long nowSecond = nowTicket / 1000;

            lock (scene)
            {
                if (scene.MonsterWave >= scene.MonsterWaveTotal)
                {
                    scene.SceneStatus = GameSceneStatuses.STATUS_END;
                    return;
                }

                //置刷怪标记
                scene.IsMonsterFlag = 1;

                int wave = scene.MonsterWave + upWave;
                if (wave > scene.MonsterWaveTotal)
                    wave = scene.MonsterWaveTotal;

                waveConfig = _runtimeData.GetOrderConfig(wave);
                if (waveConfig == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("元素试炼报错 刷怪波次 = {0}", wave));
                    return;
                }

                scene.MonsterCountCreate = waveConfig.MonsterCount;
                scene.MonsterWave = wave; // 递增刷怪波数
                scene.CreateMonsterTime = nowSecond;

                int monsterID = 0;
                int gridX = 0;
                int gridY = 0;
                int gridNum = 0;

                for (int i = 0; i < waveConfig.MonsterCount; i++)
                {
                    monsterID = waveConfig.MonsterIDs[i % waveConfig.MonsterIDs.Count];
                    gridX = gameMap.CorrectWidthPointToGridPoint(waveConfig.X + Global.GetRandomNumber(-waveConfig.Radius, waveConfig.Radius)) / gameMap.MapGridWidth;
                    gridY = gameMap.CorrectHeightPointToGridPoint(waveConfig.Y + Global.GetRandomNumber(-waveConfig.Radius, waveConfig.Radius)) / gameMap.MapGridHeight;
                    gridNum = 0;

                    GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.MapID, monsterID, scene.CopyMapInfo.CopyMapID, 1, gridX, gridY, gridNum);
                }

                scene.ScoreData.Wave = waveConfig.OrderID;
                scene.ScoreData.EndTime = nowTicket + waveConfig.Up1 * 1000;
                scene.ScoreData.MonsterCount = waveConfig.MonsterCount;

                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_SCORE_INFO, scene.ScoreData, scene.CopyMapInfo);
            }
        }

        //是否副本
        public bool IsElementWarCopy(int FubenID)
        {
            return FubenID == _runtimeData.CopyID;
        }

        /// <summary>
        // 杀怪接口
        /// </summary>
        public void KillMonster(GameClient client, Monster monster)
        {
            if (client.ClientData.FuBenSeqID < 0 || client.ClientData.CopyMapID < 0 || !IsElementWarCopy(client.ClientData.FuBenID))
                return;

            ElementWarScene scene = null;
            if (!_sceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene) || scene == null)
                return;

            //如果是重复记录击杀同一只怪,则直接返回
            if (!scene.AddKilledMonster(monster))
                return;

            if (scene.SceneStatus >= GameSceneStatuses.STATUS_END)
                return;

            lock (scene)
            {
                scene.MonsterCountKill++;

                scene.ScoreData.MonsterCount -= 1;
                scene.ScoreData.MonsterCount = scene.ScoreData.MonsterCount < 0 ? 0 : scene.ScoreData.MonsterCount;
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_SCORE_INFO, scene.ScoreData, scene.CopyMapInfo);

                if (scene.IsMonsterFlag == 1 && scene.MonsterCountKill >= scene.MonsterCountCreate && scene.ScoreData.MonsterCount <= 0)
                {
                    scene.MonsterWaveOld = scene.MonsterWave;
                    if (scene.MonsterWave >= scene.MonsterWaveTotal)
                    {
                        scene.SceneStatus = GameSceneStatuses.STATUS_END;
                    }
                    else
                    {
                        scene.IsMonsterFlag = 0;

                        scene.MonsterCountKill = 0;
                        scene.MonsterCountCreate = 0;
                    }
                }
            }//lock
        }

        /// <summary>
        /// 给奖励
        /// </summary>
        public void GiveAwards(ElementWarScene scene)
        {
            try
            {
               FuBenMapItem fuBenMapItem = FuBenManager.FindMapCodeByFuBenID(scene.CopyMapInfo.FubenMapID, scene.MapID);
                if(fuBenMapItem==null)  return;

                //FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(scene.FuBenSeqId);
                //if (null == fuBenInfoItem) return;

                //fuBenInfoItem.EndTicks = TimeUtil.NOW();
                //int addFuBenNum = 1;
                //if (fuBenInfoItem.nDayOfYear != TimeUtil.NowDateTime().DayOfYear)
                //    addFuBenNum = 0;

                //int usedSecs = (int)(scene.EndTime - scene.BeginTime);

                int zhanLi = 0;
                List<GameClient> objsList = scene.CopyMapInfo.GetClientsList();
                if (objsList != null && objsList.Count > 0)
                {                 
                    for (int n = 0; n < objsList.Count; ++n)
                    {
                        GameClient client = objsList[n];
                        if (client != null && client == GameManager.ClientMgr.FindClient(client.ClientData.RoleID)) //确认角色仍然在线
                        {
                            // 公式
                            long nExp = fuBenMapItem.Experience;
                            int money = fuBenMapItem.Money1;

                            int wave = scene.MonsterWaveOld;
                            if (wave > 0) wave -= 1;
                            int light = fuBenMapItem.LightAward + _runtimeData.AwardLight[wave];

                            if (nExp > 0)
                                GameManager.ClientMgr.ProcessRoleExperience(client, nExp, false);

                            if (money > 0)
                                GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, money, string.Format(/**/"副本{0}通关奖励", scene.CopyID), false);

                            if (light > 0)
                                GameManager.FluorescentGemMgr.AddFluorescentPoint(client, light, "元素试炼");


                            ElementWarAwardsData awardsData = new ElementWarAwardsData()
                            {
                                Wave = scene.MonsterWaveOld,
                                Exp = nExp,
                                Money = money,
                                Light = light
                            };

                            AddElementWarCount(client);
                            GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 0, 1, 0, (int)_gameType);

                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_AWARD, awardsData);


                            zhanLi += client.ClientData.CombatForce;

                            Global.UpdateFuBenDataForQuickPassTimer(client, scene.CopyMapInfo.FubenMapID, 0, 1);
                        }
                    }
                }

                int roleCount = 0;
                if (objsList != null && objsList.Count>0)
                {
                    roleCount = objsList.Count;
                    zhanLi = zhanLi / roleCount;
                }

               // ElementWarClient.getInstance().UpdateCopyPassEvent(scene.FuBenSeqId, roleCount, scene.MonsterWaveOld, zhanLi);
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteExceptionLogEx(ex, "【元素试炼】清场调度异常");
            }

        }

        public void NotifyTimeStateInfoAndScoreInfo(GameClient client, bool timeState = true, bool scoreInfo = true)
        {
            lock (_mutex)
            {
                ElementWarScene scene;
                if (_sceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    if (timeState)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData);
                    }

                    if (scoreInfo)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_SCORE_INFO, scene.ScoreData);
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

            ElementWarScene scene = null;
            lock (_sceneDict)
            {
                if (!_sceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene))
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

        #endregion 活动逻辑
    }
}
