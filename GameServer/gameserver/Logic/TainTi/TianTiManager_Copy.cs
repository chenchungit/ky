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
using GameServer.Logic.JingJiChang;

namespace GameServer.Logic
{
    /// <summary>
    /// 天梯系统管理器
    /// </summary>
    public partial class TianTiManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 运行时变量

        /// <summary>
        /// 血色城堡副本场景数据
        /// </summary>
        public ConcurrentDictionary<int, TianTiScene> TianTiSceneDict = new ConcurrentDictionary<int, TianTiScene>(); // KEY-副本ID VALUE- KEY-副本顺序ID VALUE-TianTiScene信息

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long NextHeartBeatTicks = 0L;

        #endregion 运行时变量

        #region 副本

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public bool AddTianTiCopyScenes(GameClient client, CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (sceneType == SceneUIClasses.TianTi)
            {
                int fuBenSeqId = copyMap.FuBenSeqID;
                int mapCode = copyMap.MapCode;
                lock (RuntimeData.Mutex)
                {
                    TianTiScene tianTiScene = null;
                    if (!TianTiSceneDict.TryGetValue(fuBenSeqId, out tianTiScene))
                    {
                        tianTiScene = new TianTiScene();
                        tianTiScene.CopyMap = copyMap;
                        tianTiScene.CleanAllInfo();
                        tianTiScene.GameId = (int)Global.GetClientKuaFuServerLoginData(client).GameId;
                        tianTiScene.m_nMapCode = mapCode;
                        tianTiScene.CopyMapId = copyMap.CopyMapID;
                        tianTiScene.FuBenSeqId = fuBenSeqId;
                        tianTiScene.m_nPlarerCount = 1;

                        TianTiSceneDict[fuBenSeqId] = tianTiScene;
                    }
                    else
                    {
                        tianTiScene.m_nPlarerCount++;
                    }

                    copyMap.IsKuaFuCopy = true;
                    SaveClientBattleSide(tianTiScene, client);
                    copyMap.SetRemoveTicks(TimeUtil.NOW() + RuntimeData.TotalSecs * TimeUtil.SECOND);

                    if (tianTiScene.SuccessSide == -1)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TIANTI_AWARD, new TianTiAwardsData() { Success = -1 });
                    }
                }

                //更新状态
                TianTiClient.getInstance().GameFuBenRoleChangeState(client.ClientData.RoleID, (int)KuaFuRoleStates.StartGame);
                GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 1, 0, 0, (int)GameTypes.TianTi);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public bool RemoveTianTiCopyScene(CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (sceneType == SceneUIClasses.TianTi)
            {
                lock (RuntimeData.Mutex)
                {
                    TianTiScene tianTiScene;
                    if (TianTiSceneDict.TryRemove(copyMap.FuBenSeqID, out tianTiScene))
                    {
                        RuntimeData.GameId2FuBenSeq.Remove(tianTiScene.GameId);
                    }
                }

                return true;
            }

            return false;
        }

        private void SaveClientBattleSide(TianTiScene tianTiScene, GameClient client)
        {
            TianTiRoleMiniData tianTiRoleMiniData;
            if (!tianTiScene.RoleIdDuanWeiIdDict.TryGetValue(client.ClientData.RoleID, out tianTiRoleMiniData))
            {
                tianTiRoleMiniData = new TianTiRoleMiniData();
                tianTiScene.RoleIdDuanWeiIdDict[client.ClientData.RoleID] = tianTiRoleMiniData;
            }

            tianTiRoleMiniData.RoleId = client.ClientData.RoleID;
            tianTiRoleMiniData.RoleName = client.ClientData.RoleName;
            tianTiRoleMiniData.BattleWitchSide = client.ClientData.BattleWhichSide;
            tianTiRoleMiniData.ZoneId = client.ClientData.ZoneID;
            tianTiRoleMiniData.DuanWeiId = client.ClientData.TianTiData.DuanWeiId;
        }

        private TianTiRoleMiniData GetEnemyBattleSide(TianTiScene tianTiScene, GameClient client)
        {
            foreach (var kv in tianTiScene.RoleIdDuanWeiIdDict)
            {
                if (client.ClientData.RoleID != kv.Key)
                {
                    return kv.Value;
                }
            }

            return null;
        }

        #endregion 副本

        #region 活动逻辑

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

            foreach (var tianTiScene in TianTiSceneDict.Values)
            {
                lock (RuntimeData.Mutex)
                {
                    int nID = -1;
                    nID = tianTiScene.FuBenSeqId;

                    int nCopyID = -1;
                    nCopyID = tianTiScene.CopyMapId;

                    int nMapCodeID = -1;
                    nMapCodeID = tianTiScene.m_nMapCode;

                    if (nID < 0 || nCopyID < 0 || nMapCodeID < 0)
                        continue;

                    CopyMap copyMap = tianTiScene.CopyMap;

                    // 当前tick
                    DateTime now = TimeUtil.NowDateTime();
                    long ticks = TimeUtil.NOW();

                    if (tianTiScene.m_eStatus == GameSceneStatuses.STATUS_NULL)             // 如果处于空状态 -- 是否要切换到准备状态
                    {
                        tianTiScene.m_lPrepareTime = ticks;
                        tianTiScene.m_lBeginTime = ticks + RuntimeData.WaitingEnterSecs * TimeUtil.SECOND;
                        tianTiScene.m_eStatus = GameSceneStatuses.STATUS_PREPARE;

                        tianTiScene.StateTimeData.GameType = (int)GameTypes.TianTi;
                        tianTiScene.StateTimeData.State = (int)tianTiScene.m_eStatus;
                        tianTiScene.StateTimeData.EndTicks = tianTiScene.m_lBeginTime;
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, tianTiScene.StateTimeData, tianTiScene.CopyMap);
                    }
                    else if (tianTiScene.m_eStatus == GameSceneStatuses.STATUS_PREPARE)     // 场景战斗状态切换
                    {
                        //检查双方是否都进入了
                        if (copyMap.GetGameClientCount() >= 2)
                        {
                            tianTiScene.m_eStatus = GameSceneStatuses.STATUS_BEGIN;
                            tianTiScene.m_lEndTime = ticks + RuntimeData.FightingSecs * TimeUtil.SECOND;

                            tianTiScene.StateTimeData.GameType = (int)GameTypes.TianTi;
                            tianTiScene.StateTimeData.State = (int)tianTiScene.m_eStatus;
                            tianTiScene.StateTimeData.EndTicks = tianTiScene.m_lEndTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, tianTiScene.StateTimeData, tianTiScene.CopyMap);

                            //放开光幕
                            copyMap.AddGuangMuEvent(1, 0);
                            GameManager.ClientMgr.BroadSpecialMapAIEvent(copyMap.MapCode, copyMap.CopyMapID, 1, 0);
                            copyMap.AddGuangMuEvent(2, 0);
                            GameManager.ClientMgr.BroadSpecialMapAIEvent(copyMap.MapCode, copyMap.CopyMapID, 2, 0);
                        }
                        else if (ticks >= (tianTiScene.m_lBeginTime))
                        {
                            CompleteTianTiScene(tianTiScene, -1);
                        }
                    }
                    else if (tianTiScene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)       // 战斗开始
                    {
                        if (ticks >= tianTiScene.m_lEndTime)
                        {
                            CompleteTianTiScene(tianTiScene, 0);
                        }
                    }
                    else if (tianTiScene.m_eStatus == GameSceneStatuses.STATUS_END)         // 战斗结束
                    {
                        ProcessEnd(tianTiScene, now, nowTicks);
                    }
                    else if (tianTiScene.m_eStatus == GameSceneStatuses.STATUS_AWARD)
                    {
                        if (ticks >= tianTiScene.m_lLeaveTime)
                        {
                            copyMap.SetRemoveTicks(tianTiScene.m_lLeaveTime);
                            tianTiScene.m_eStatus = GameSceneStatuses.STATUS_CLEAR;

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
                                DataHelper.WriteExceptionLogEx(ex, "跨服天梯系统清场调度异常");
                            }
                        }
                    }
                }
            }

            return;
        }

        public void NotifyTimeStateInfoAndScoreInfo(GameClient client)
        {
            lock (RuntimeData.Mutex)
            {
                TianTiScene tianTiScene;
                if (TianTiSceneDict.TryGetValue(client.ClientData.FuBenSeqID, out tianTiScene))
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, tianTiScene.StateTimeData);
                }
            }
        }

        public void CompleteTianTiScene(TianTiScene tianTiScene, int successSide)
        {
            tianTiScene.m_eStatus = GameSceneStatuses.STATUS_END;
            tianTiScene.SuccessSide = successSide;
        }

        public void OnKillRole(GameClient client, GameClient other)
        {
            lock (RuntimeData.Mutex)
            {
                TianTiScene tianTiScene;
                if (TianTiSceneDict.TryGetValue(client.ClientData.FuBenSeqID, out tianTiScene))
                {
                    if (tianTiScene.m_eStatus < GameSceneStatuses.STATUS_END)
                    {
                        CompleteTianTiScene(tianTiScene, client.ClientData.BattleWhichSide);
                    }
                }
            }
        }

        private void ProcessEnd(TianTiScene tianTiScene, DateTime now, long nowTicks)
        {
            //结算奖励
            tianTiScene.m_eStatus = GameSceneStatuses.STATUS_AWARD;
            tianTiScene.m_lEndTime = nowTicks;
            tianTiScene.m_lLeaveTime = tianTiScene.m_lEndTime + RuntimeData.ClearRolesSecs * TimeUtil.SECOND;

            TianTiClient.getInstance().GameFuBenChangeState(tianTiScene.GameId, GameFuBenState.End, now);

            tianTiScene.StateTimeData.GameType = (int)GameTypes.TianTi;
            tianTiScene.StateTimeData.State = (int)GameSceneStatuses.STATUS_END;
            tianTiScene.StateTimeData.EndTicks = tianTiScene.m_lLeaveTime;
            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, tianTiScene.StateTimeData, tianTiScene.CopyMap);

            if (tianTiScene.SuccessSide == -1)
            {
                GameCanceled(tianTiScene);
            }
            else
            {
                GiveAwards(tianTiScene);
            }
        }

        /// <summary>
        /// 给奖励
        /// </summary>
        public void GiveAwards(TianTiScene tianTiScene)
        {
            try
            {
                DateTime now = TimeUtil.NowDateTime();
                DateTime startTime = now.Subtract(RuntimeData.RefreshTime); //后退刷新时间,这样来保证不跨天计次
                List<GameClient> objsList = tianTiScene.CopyMap.GetClientsList();
                if (objsList != null && objsList.Count > 0)
                {
                    int nowDayId = Global.GetOffsetDayNow();
                    for (int n = 0; n < objsList.Count; ++n)
                    {
                        GameClient client = objsList[n];
                        if (client != null && client == GameManager.ClientMgr.FindClient(client.ClientData.RoleID)) //确认角色仍然在线
                        {
                            RoleTianTiData roleTianTiData = client.ClientData.TianTiData;
                            bool success = client.ClientData.BattleWhichSide == tianTiScene.SuccessSide;
                            int selfDuanWeiId = roleTianTiData.DuanWeiId;
                            TianTiRoleMiniData enemyMiniData = GetEnemyBattleSide(tianTiScene, client);
                            int addDuanWeiJiFen = 0;
                            int addLianShengJiFen = 0;
                            int addRongYao = 0;

                            int dayId = Global.GetOffsetDay(startTime);
                            if (dayId != roleTianTiData.LastFightDayId)
                            {
                                roleTianTiData.LastFightDayId = dayId;
                                roleTianTiData.TodayFightCount = 1;
                            }
                            else
                            {
                                roleTianTiData.TodayFightCount++;
                            }

                            //设置每日天梯积分获得上限为60万
                            if (roleTianTiData.DayDuanWeiJiFen < RuntimeData.MaxTianTiJiFen)
                            {
                                TianTiDuanWei tianTiDuanWei;
                                if (success)
                                {
                                    roleTianTiData.LianSheng++;
                                    roleTianTiData.SuccessCount++;
                                    if (RuntimeData.TianTiDuanWeiDict.TryGetValue(enemyMiniData.DuanWeiId, out tianTiDuanWei))
                                    {
                                        //连胜后积分=基础积分*(1+Min(2,((连续胜利次数-1)* 0.2)))
                                        addDuanWeiJiFen = tianTiDuanWei.WinJiFen;
                                        addLianShengJiFen = (int)(tianTiDuanWei.WinJiFen * Math.Min(2, (roleTianTiData.LianSheng - 1) * 0.2));
                                        if (roleTianTiData.TodayFightCount <= tianTiDuanWei.RongYaoNum)
                                        {
                                            addRongYao = tianTiDuanWei.WinRongYu;
                                        }
                                    }
                                }
                                else
                                {
                                    roleTianTiData.LianSheng = 0;
                                    if (RuntimeData.TianTiDuanWeiDict.TryGetValue(roleTianTiData.DuanWeiId, out tianTiDuanWei))
                                    {
                                        addDuanWeiJiFen = tianTiDuanWei.LoseJiFen;
                                        if (roleTianTiData.TodayFightCount <= tianTiDuanWei.RongYaoNum)
                                        {
                                            addRongYao = tianTiDuanWei.LoseRongYu;
                                        }
                                    }
                                }

                                if (addDuanWeiJiFen != 0)
                                {
                                    roleTianTiData.DuanWeiJiFen += addDuanWeiJiFen + addLianShengJiFen;
                                    roleTianTiData.DuanWeiJiFen = Math.Max(0, roleTianTiData.DuanWeiJiFen);

                                    roleTianTiData.DayDuanWeiJiFen += addDuanWeiJiFen + addLianShengJiFen;
                                    roleTianTiData.DayDuanWeiJiFen = Math.Max(0, roleTianTiData.DayDuanWeiJiFen);
                                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TianTiDayScore, roleTianTiData.DayDuanWeiJiFen, true);
                                }
                            }
                            else
                            {
                                GameManager.ClientMgr.NotifyHintMsg(client, Global.GetLang("今日获得段位积分已达上限！"));
                            }

                            if (addRongYao != 0)
                            {
                                GameManager.ClientMgr.ModifyTianTiRongYaoValue(client, addRongYao, "天梯系统获得荣耀", true);
                            }

                            roleTianTiData.FightCount++;
                            if (RuntimeData.DuanWeiJiFenRangeDuanWeiIdDict.TryGetValue(roleTianTiData.DuanWeiJiFen, out selfDuanWeiId))
                            {
                                roleTianTiData.DuanWeiId = selfDuanWeiId;
                            }

                            TianTiAwardsData awardsData = new TianTiAwardsData();
                            awardsData.DuanWeiJiFen = addDuanWeiJiFen;
                            awardsData.LianShengJiFen = addLianShengJiFen;
                            awardsData.RongYao = addRongYao;
                            awardsData.DuanWeiId = roleTianTiData.DuanWeiId;
                            if (success)
                            {
                                awardsData.Success = 1;
                                GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 0, 1, 0, (int)GameTypes.TianTi);
                            }
                            else
                            {
                                GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 0, 0, 1, (int)GameTypes.TianTi);
                            }
                            
                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TIANTI_AWARD, awardsData);
                            Global.sendToDB<int, RoleTianTiData>((int)TCPGameServerCmds.CMD_DB_TIANTI_UPDATE_ROLE_DATA, roleTianTiData, client.ServerId);

                            TianTiLogItemData tianTiLogItemData = new TianTiLogItemData()
                                {
                                    Success = awardsData.Success,
                                    ZoneId1 = client.ClientData.ZoneID,
                                    RoleName1 = client.ClientData.RoleName,
                                    ZoneId2 = enemyMiniData.ZoneId,
                                    RoleName2 = enemyMiniData.RoleName,
                                    DuanWeiJiFenAward = addDuanWeiJiFen + addLianShengJiFen,
                                    RongYaoAward = addRongYao,
                                    RoleId = client.ClientData.RoleID,
                                    EndTime = now,
                                };
                            Global.sendToDB<int, TianTiLogItemData>((int)TCPGameServerCmds.CMD_DB_TIANTI_ADD_ZHANBAO_LOG, tianTiLogItemData, client.ServerId);

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
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteExceptionLogEx(ex, "天梯系统清场调度异常");
            }

        }

        /// <summary>
        /// 本场游戏取消
        /// </summary>
        public void GameCanceled(TianTiScene tianTiScene)
        {
            try
            {
                List<GameClient> objsList = tianTiScene.CopyMap.GetClientsList();
                if (objsList != null && objsList.Count > 0)
                {
                    for (int n = 0; n < objsList.Count; ++n)
                    {
                        GameClient client = objsList[n];
                        if (client != null && client == GameManager.ClientMgr.FindClient(client.ClientData.RoleID)) //确认角色仍然在线
                        {
                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TIANTI_AWARD, new TianTiAwardsData() { Success = -1 });
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteExceptionLogEx(ex, "天梯系统清场调度异常");
            }

        }

        /// <summary>
        /// 玩家离开血色堡垒
        /// </summary>
        public void LeaveFuBen(GameClient client)
        {
            lock (RuntimeData.Mutex)
            {
                TianTiScene tianTiScene = null;
                if (TianTiSceneDict.TryGetValue(client.ClientData.FuBenSeqID, out tianTiScene))
                {
                    if (tianTiScene.m_eStatus < GameSceneStatuses.STATUS_END)
                    {
                        if (tianTiScene.CopyMap.GetGameClientCount() >= 2)
                        {
                            if (client.ClientData.BattleWhichSide == 1)
                            {
                                CompleteTianTiScene(tianTiScene, 2);
                            }
                            else// if (client.ClientData.BattleWhichSide == 2)
                            {
                                CompleteTianTiScene(tianTiScene, 1);
                            }
                        }
                        else
                        {
                            CompleteTianTiScene(tianTiScene, -1);
                        }

                        ProcessEnd(tianTiScene, TimeUtil.NowDateTime(), TimeUtil.NOW());
                    }
                }
            }

            TianTiClient.getInstance().GameFuBenRoleChangeState(client.ClientData.RoleID, (int)KuaFuRoleStates.None);
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
