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

namespace GameServer.Logic
{
    /// <summary>
    /// 幻影寺院管理器
    /// </summary>
    public partial class HuanYingSiYuanManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 运行时变量

        /// <summary>
        /// 线程锁对象 -- 血色城堡场景
        /// </summary>
        public static object Mutex = new object();

        private int InternalShengBeiId = 0;

        /// <summary>
        /// 血色城堡副本场景数据
        /// </summary>
        public ConcurrentDictionary<int, HuanYingSiYuanScene> HuanYingSiYuanSceneDict = new ConcurrentDictionary<int, HuanYingSiYuanScene>(); // KEY-副本ID VALUE- KEY-副本顺序ID VALUE-HuanYingSiYuanScene信息

        /// <summary>
        /// key: game id
        /// value: fuben seq
        /// </summary>
        public Dictionary<int, int> GameId2FuBenSeq = new Dictionary<int, int>();

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long NextHeartBeatTicks = 0L;

        #endregion 运行时变量

        #region 副本

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public bool AddHuanYingSiYuanCopyScenes(GameClient client, CopyMap copyMap)
        {
            if (copyMap.MapCode == RuntimeData.MapCode)
            {
                int fuBenSeqId = copyMap.FuBenSeqID;
                int mapCode = copyMap.MapCode;
                lock (Mutex)
                {
                    HuanYingSiYuanScene huanYingSiYuanScene = null;
                    if (!HuanYingSiYuanSceneDict.TryGetValue(fuBenSeqId, out huanYingSiYuanScene))
                    {
                        huanYingSiYuanScene = new HuanYingSiYuanScene();
                        huanYingSiYuanScene.CopyMap = copyMap;
                        huanYingSiYuanScene.CleanAllInfo();
                        huanYingSiYuanScene.GameId = (int)Global.GetClientKuaFuServerLoginData(client).GameId;
                        huanYingSiYuanScene.m_nMapCode = mapCode;
                        huanYingSiYuanScene.CopyMapId = copyMap.CopyMapID;
                        huanYingSiYuanScene.FuBenSeqId = fuBenSeqId;
                        huanYingSiYuanScene.m_nPlarerCount = 1;

                        HuanYingSiYuanSceneDict[fuBenSeqId] = huanYingSiYuanScene;
                    }
                    else
                    {
                        huanYingSiYuanScene.m_nPlarerCount++;
                    }

                    if (client.ClientData.BattleWhichSide == 1)
                    {
                        huanYingSiYuanScene.ScoreInfoData.Count1++;
                    }
                    else
                    {
                        huanYingSiYuanScene.ScoreInfoData.Count2++;
                    }

                    copyMap.IsKuaFuCopy = true;
                    copyMap.SetRemoveTicks(TimeUtil.NOW() + RuntimeData.TotalSecs * TimeUtil.SECOND);
                    GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_HYSY_SCORE_INFO, huanYingSiYuanScene.ScoreInfoData, huanYingSiYuanScene.CopyMap);
                }

                client.SceneContextData2 = new HuanYingSiYuanLianShaContextData();

                //更新状态
                HuanYingSiYuanClient.getInstance().GameFuBenRoleChangeState(client.ClientData.RoleID, (int)KuaFuRoleStates.StartGame);
                GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 1, 0, 0, (int)GameTypes.HuanYingSiYuan);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public bool RemoveHuanYingSiYuanListCopyScenes(CopyMap copyMap)
        {
            if (copyMap.MapCode == RuntimeData.MapCode)
            {
                lock (Mutex)
                {
                    HuanYingSiYuanScene huanYingSiYuanScene;
                    if (HuanYingSiYuanSceneDict.TryRemove(copyMap.FuBenSeqID, out huanYingSiYuanScene))
                    {
                        GameId2FuBenSeq.Remove(huanYingSiYuanScene.GameId);
                    }
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
            if (nowTicks < NextHeartBeatTicks)
            {
                return;
            }

            NextHeartBeatTicks = nowTicks + 1020; //1020毫秒执行一次

            foreach (var huanYingSiYuanScene in HuanYingSiYuanSceneDict.Values)
            {
                lock (Mutex)
                {
                    int nID = -1;
                    nID = huanYingSiYuanScene.FuBenSeqId;

                    int nCopyID = -1;
                    nCopyID = huanYingSiYuanScene.CopyMapId;

                    int nMapCodeID = -1;
                    nMapCodeID = huanYingSiYuanScene.m_nMapCode;

                    if (nID < 0 || nCopyID < 0 || nMapCodeID < 0)
                        continue;

                    CopyMap copyMap = huanYingSiYuanScene.CopyMap;

                    // 当前tick
                    DateTime now = TimeUtil.NowDateTime();
                    long ticks = TimeUtil.NOW();

                    if (huanYingSiYuanScene.m_eStatus == GameSceneStatuses.STATUS_NULL)             // 如果处于空状态 -- 是否要切换到准备状态
                    {
                        huanYingSiYuanScene.m_lPrepareTime = ticks;
                        huanYingSiYuanScene.m_lBeginTime = ticks + RuntimeData.PrepareSecs * TimeUtil.SECOND;
                        huanYingSiYuanScene.m_eStatus = GameSceneStatuses.STATUS_PREPARE;

                        huanYingSiYuanScene.StateTimeData.GameType = (int)GameTypes.HuanYingSiYuan;
                        huanYingSiYuanScene.StateTimeData.State = (int)huanYingSiYuanScene.m_eStatus;
                        huanYingSiYuanScene.StateTimeData.EndTicks = huanYingSiYuanScene.m_lBeginTime;
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, huanYingSiYuanScene.StateTimeData, huanYingSiYuanScene.CopyMap);
                    }
                    else if (huanYingSiYuanScene.m_eStatus == GameSceneStatuses.STATUS_PREPARE)     // 场景战斗状态切换
                    {
                        if (ticks >= (huanYingSiYuanScene.m_lBeginTime))
                        {
                            huanYingSiYuanScene.m_eStatus = GameSceneStatuses.STATUS_BEGIN;
                            huanYingSiYuanScene.m_lEndTime = ticks + RuntimeData.FightingSecs * TimeUtil.SECOND;

                            huanYingSiYuanScene.StateTimeData.GameType = (int)GameTypes.HuanYingSiYuan;
                            huanYingSiYuanScene.StateTimeData.State = (int)huanYingSiYuanScene.m_eStatus;
                            huanYingSiYuanScene.StateTimeData.EndTicks = huanYingSiYuanScene.m_lEndTime;
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, huanYingSiYuanScene.StateTimeData, huanYingSiYuanScene.CopyMap);

                            //刷圣杯
                            foreach (var shengBeiData in RuntimeData.ShengBeiDataDict.Values)
                            {
                                HuanYingSiYuanShengBeiContextData contextData = new HuanYingSiYuanShengBeiContextData()
                                {
                                    UniqueId = GetInternalId(),
                                    FuBenSeqId = huanYingSiYuanScene.FuBenSeqId,
                                    ShengBeiId = shengBeiData.ID,
                                    BufferGoodsId = shengBeiData.GoodsID,
                                    MonsterId = shengBeiData.MonsterID,
                                    PosX = shengBeiData.PosX,
                                    PosY = shengBeiData.PosY,
                                    CopyMapID = huanYingSiYuanScene.CopyMapId,
                                    Score = shengBeiData.Score,
                                    Time = shengBeiData.Time,
                                    BufferProps = shengBeiData.BufferProps,
                                };

                                CreateMonster(huanYingSiYuanScene, contextData);
                            }

                            //放开光幕
                            copyMap.AddGuangMuEvent(1, 0);
                            GameManager.ClientMgr.BroadSpecialMapAIEvent(copyMap.MapCode, copyMap.CopyMapID, 1, 0);
                        }
                    }
                    else if (huanYingSiYuanScene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)       // 战斗开始
                    {
                        if (ticks >= huanYingSiYuanScene.m_lEndTime)
                        {
                            int successSide = 0;
                            if (huanYingSiYuanScene.ScoreInfoData.Score1 > huanYingSiYuanScene.ScoreInfoData.Score2)
                            {
                                successSide = 1;
                            }
                            else if (huanYingSiYuanScene.ScoreInfoData.Score2 > huanYingSiYuanScene.ScoreInfoData.Score1)
                            {
                                successSide = 2;
                            }

                            CompleteHuanYingSiYuanScene(huanYingSiYuanScene, successSide);
                        }
                        else
                        {
                            CheckShengBeiBufferTime(huanYingSiYuanScene, nowTicks);
                        }
                    }
                    else if (huanYingSiYuanScene.m_eStatus == GameSceneStatuses.STATUS_END)
                    {
                        //结算奖励
                        huanYingSiYuanScene.m_eStatus = GameSceneStatuses.STATUS_AWARD;
                        huanYingSiYuanScene.m_lEndTime = nowTicks;
                        huanYingSiYuanScene.m_lLeaveTime = huanYingSiYuanScene.m_lEndTime + RuntimeData.ClearRolesSecs * TimeUtil.SECOND;

                        HuanYingSiYuanClient.getInstance().GameFuBenChangeState(huanYingSiYuanScene.GameId, GameFuBenState.End, now);

                        huanYingSiYuanScene.StateTimeData.GameType = (int)GameTypes.HuanYingSiYuan;
                        huanYingSiYuanScene.StateTimeData.State = (int)GameSceneStatuses.STATUS_END;
                        huanYingSiYuanScene.StateTimeData.EndTicks = huanYingSiYuanScene.m_lLeaveTime;
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, huanYingSiYuanScene.StateTimeData, huanYingSiYuanScene.CopyMap);

                        GiveAwards(huanYingSiYuanScene);
                    }
                    else if (huanYingSiYuanScene.m_eStatus == GameSceneStatuses.STATUS_AWARD)         // 战斗结束
                    {
                        if (ticks >= huanYingSiYuanScene.m_lLeaveTime)
                        {
                            copyMap.SetRemoveTicks(huanYingSiYuanScene.m_lLeaveTime);
                            huanYingSiYuanScene.m_eStatus = GameSceneStatuses.STATUS_CLEAR;

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
                                DataHelper.WriteExceptionLogEx(ex, "幻影寺院清场调度异常");
                            }
                        }
                    }
                }
            }

            return;
        }

        public int GetInternalId()
        {
            int id = Interlocked.Increment(ref InternalShengBeiId);
            if (id < 0)
            {
                id = InternalShengBeiId = 1;
            }

            return id;
        }

        public void NotifyTimeStateInfoAndScoreInfo(GameClient client, bool timeState = true, bool scoreInfo = true)
        {
            lock (Mutex)
            {
                HuanYingSiYuanScene huanYingSiYuanScene;
                if (HuanYingSiYuanSceneDict.TryGetValue(client.ClientData.FuBenSeqID, out huanYingSiYuanScene))
                {
                    if (timeState)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, huanYingSiYuanScene.StateTimeData);
                    }

                    if (scoreInfo)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_HYSY_SCORE_INFO, huanYingSiYuanScene.ScoreInfoData);
                    }
                }
            }
        }

        /// <summary>
        /// 刷怪
        /// </summary>
        public void CreateMonster(HuanYingSiYuanScene scene, HuanYingSiYuanShengBeiContextData contextData = null)
        {
            int gridX = contextData.PosX / RuntimeData.MapGridWidth;
            int gridY = contextData.PosY / RuntimeData.MapGridHeight;
            GameManager.MonsterZoneMgr.AddDynamicMonsters(RuntimeData.MapCode, contextData.MonsterId, contextData.CopyMapID, 1, gridX, gridY, 0, 0, SceneUIClasses.HuanYingSiYuan, contextData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <returns></returns>
        public int GetCaiJiMonsterTime(GameClient client, Monster monster)
        {
            HuanYingSiYuanShengBeiContextData contextData = client.SceneContextData as HuanYingSiYuanShengBeiContextData;
            if (null != contextData)
            {
                lock (Mutex)
                {
                    HuanYingSiYuanScene huanYingSiYuanScene;
                    if (HuanYingSiYuanSceneDict.TryGetValue(contextData.FuBenSeqId, out huanYingSiYuanScene))
                    {
                        if (huanYingSiYuanScene.ShengBeiContextDict.ContainsKey(contextData.UniqueId))
                        {
                            return StdErrorCode.Error_Has_Ownen_ShengBei;
                        }
                    }
                }
            }

            contextData = monster.Tag as HuanYingSiYuanShengBeiContextData;
            if (contextData != null)
            {
                return contextData.Time;
            }

            return StdErrorCode.Error_Other_Has_Get;
        }

        public void OnCaiJiFinish(GameClient client, Monster monster)
        {
            HuanYingSiYuanShengBeiContextData contextData = monster.Tag as HuanYingSiYuanShengBeiContextData;
            HuanYingSiYuanScene huanYingSiYuanScene = null;
            CopyMap copyMap = null;
            if (null != contextData)
            {
                long endTicks = TimeUtil.NOW() + RuntimeData.HoldShengBeiSecs * TimeUtil.SECOND;
                lock (Mutex)
                {
                    contextData.OwnerRoleId = client.ClientData.RoleID;
                    contextData.EndTicks = endTicks;
                    if (HuanYingSiYuanSceneDict.TryGetValue(contextData.FuBenSeqId, out huanYingSiYuanScene))
                    {
                        if (huanYingSiYuanScene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                        {
		                    GetShengBei(huanYingSiYuanScene, client, contextData);
                        }
                    }
                }
            }
        }

        public void CompleteHuanYingSiYuanScene(HuanYingSiYuanScene huanYingSiYuanScene, int successSide)
        {
            huanYingSiYuanScene.m_eStatus = GameSceneStatuses.STATUS_END;
            huanYingSiYuanScene.SuccessSide = successSide;
        }

        public void OnKillRole(GameClient client, GameClient other)
        {
            TryLostShengBei(other);

            lock (Mutex)
            {
                HuanYingSiYuanScene huanYingSiYuanScene;
                if (HuanYingSiYuanSceneDict.TryGetValue(client.ClientData.FuBenSeqID, out huanYingSiYuanScene))
                {
                    if (huanYingSiYuanScene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                    {
                        int addScore = 0;
                        HuanYingSiYuanLianShaContextData clientLianShaContextData = client.SceneContextData2 as HuanYingSiYuanLianShaContextData;
                        HuanYingSiYuanLianShaContextData otherLianShaContextData = other.SceneContextData2 as HuanYingSiYuanLianShaContextData;
                        ContinuityKillAward continuityKillAward;
                        HuanYingSiYuanLianSha huanYingSiYuanLianSha = null;
                        HuanYingSiYuanLianshaOver huanYingSiYuanLianshaOver = null;
                        HuanYingSiYuanAddScore huanYingSiYuanAddScore = new HuanYingSiYuanAddScore();

                        huanYingSiYuanAddScore.Name = Global.FormatRoleName4(client);
                        huanYingSiYuanAddScore.ZoneID = client.ClientData.ZoneID;
                        huanYingSiYuanAddScore.Side = client.ClientData.BattleWhichSide;
                        huanYingSiYuanAddScore.ByLianShaNum = 1;
                        huanYingSiYuanAddScore.RoleId = client.ClientData.RoleID;
                        huanYingSiYuanAddScore.Occupation = client.ClientData.Occupation;

                        if (null != clientLianShaContextData)
                        {
                            clientLianShaContextData.KillNum++;
                            if (RuntimeData.ContinuityKillAwardDict.TryGetValue(clientLianShaContextData.KillNum, out continuityKillAward))
                            {
                                huanYingSiYuanAddScore.ByLianShaNum = 1;
                                huanYingSiYuanLianSha = new HuanYingSiYuanLianSha();
                                huanYingSiYuanLianSha.Name = huanYingSiYuanAddScore.Name;
                                huanYingSiYuanLianSha.ZoneID = huanYingSiYuanAddScore.ZoneID;
                                huanYingSiYuanLianSha.Occupation = huanYingSiYuanAddScore.Occupation;
                                huanYingSiYuanLianSha.LianShaType = continuityKillAward.ID;
                                huanYingSiYuanLianSha.ExtScore = continuityKillAward.Score;
                                huanYingSiYuanLianSha.Side = huanYingSiYuanAddScore.Side;
                                addScore += huanYingSiYuanLianSha.ExtScore;
                            }
                        }

                        if (null != otherLianShaContextData)
                        {
                            if (otherLianShaContextData.KillNum >= 2)
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
                                huanYingSiYuanLianshaOver.ExtScore = otherLianShaContextData.KillNum * 5;

                                addScore += huanYingSiYuanLianshaOver.ExtScore;
                            }

                            otherLianShaContextData.KillNum = 0;
                        }

                        addScore += RuntimeData.TempleMiragePK;
                        huanYingSiYuanAddScore.Score = addScore;
                        if (client.ClientData.BattleWhichSide == 1)
                        {
                            huanYingSiYuanScene.ScoreInfoData.Score1 += addScore;
                            if (huanYingSiYuanScene.ScoreInfoData.Score1 >= RuntimeData.TempleMirageWin)
                            {
                                CompleteHuanYingSiYuanScene(huanYingSiYuanScene, 1);
                            }
                        }
                        else
                        {
                            huanYingSiYuanScene.ScoreInfoData.Score2 += addScore;
                            if (huanYingSiYuanScene.ScoreInfoData.Score2 >= RuntimeData.TempleMirageWin)
                            {
                                CompleteHuanYingSiYuanScene(huanYingSiYuanScene, 2);
                            }
                        }

                        if (null != clientLianShaContextData)
                        {
                            clientLianShaContextData.TotalScore += addScore;
                        }

                        //GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_HYSY_ADD_SCORE, huanYingSiYuanAddScore, huanYingSiYuanScene.CopyMap);
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_HYSY_SCORE_INFO, huanYingSiYuanScene.ScoreInfoData, huanYingSiYuanScene.CopyMap);
                        if (null != huanYingSiYuanLianSha)
                        {
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_HYSY_LIANSHA, huanYingSiYuanLianSha, huanYingSiYuanScene.CopyMap);
                        }

                        if (null != huanYingSiYuanLianshaOver)
                        {
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_HYSY_STOP_LIANSHA, huanYingSiYuanLianshaOver, huanYingSiYuanScene.CopyMap);
                        }
                    }
                }
            }
        }

        private void GetShengBei(HuanYingSiYuanScene huanYingSiYuanScene, GameClient client, HuanYingSiYuanShengBeiContextData contextData)
        {
            if (null != contextData)
            {
                lock (Mutex)
                {
                    client.SceneContextData = contextData;
                    huanYingSiYuanScene.ShengBeiContextDict[contextData.UniqueId] = contextData;

                    //设置拥有者的buffer
                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.HysyShengBei, contextData.BufferProps);
                    double[] actionParams = new double[2] { contextData.BufferGoodsId, RuntimeData.HoldShengBeiSecs };
                    Global.UpdateBufferData(client, BufferItemTypes.HysyShengBei, actionParams);
                }
            }
        }

        private HuanYingSiYuanShengBeiContextData LostShengBei(GameClient client)
        {
            ShengBeiData shengBeiData = null;
            HuanYingSiYuanShengBeiContextData contextData = null;
            if (null != client.SceneContextData)
            {
                contextData = client.SceneContextData as HuanYingSiYuanShengBeiContextData;
                if (null != contextData)
                {
                    lock (Mutex)
                    {
	                    //清除拥有者的buffer
	                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.HysyShengBei, null);
	                    double[] actionParams = new double[2]{ 0, 0 };
	                    Global.UpdateBufferData(client, BufferItemTypes.HysyShengBei, actionParams);
                        client.SceneContextData = null;
                    }
                }
            }

            return contextData;
        }

        private void SubmitShengBei(GameClient client)
        {
            if (null == client.SceneContextData)
            {
                return;
            }

            HuanYingSiYuanShengBeiContextData contextData = client.SceneContextData as HuanYingSiYuanShengBeiContextData;
            if (null == contextData)
            {
                return;
            }

            //为了放止外挂加速移动导致游戏不平衡并影响产出量,这里限制提交时间不能少于13秒(正常采集后提交大约需要15秒)
            long nowTicks = TimeUtil.NOW();
            if (contextData.EndTicks - nowTicks > (RuntimeData.HoldShengBeiSecs - RuntimeData.MinSubmitShengBeiSecs) * TimeUtil.SECOND)
            {
                return;
            }

            Point clientPoint = new Point(client.ClientData.PosX, client.ClientData.PosY);
            lock (RuntimeData.Mutex)
            {
                HuanYingSiYuanBirthPoint huanYingSiYuanBirthPoint;
                if (!RuntimeData.MapBirthPointDict.TryGetValue(client.ClientData.BattleWhichSide, out huanYingSiYuanBirthPoint))
                {
                    return;
                }

                Point targetPoint = new Point(huanYingSiYuanBirthPoint.PosX, huanYingSiYuanBirthPoint.PosY);
                if (Global.GetTwoPointDistance(clientPoint, targetPoint) > 1000)
                {
                    return;
                }
            }

            CopyMap copyMap = null;
            lock (Mutex)
            {
                HuanYingSiYuanScene huanYingSiYuanScene;
                if (HuanYingSiYuanSceneDict.TryGetValue(client.ClientData.FuBenSeqID, out huanYingSiYuanScene) && huanYingSiYuanScene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                {
                    if (huanYingSiYuanScene.ShengBeiContextDict.Remove(contextData.UniqueId))
                    {
                        LostShengBei(client);
                        CreateMonster(huanYingSiYuanScene, contextData);
                        contextData.OwnerRoleId = 0;

                        if (client.ClientData.BattleWhichSide == 1)
                        {
                            huanYingSiYuanScene.ScoreInfoData.Score1 += contextData.Score;
                            if (huanYingSiYuanScene.ScoreInfoData.Score1 >= RuntimeData.TempleMirageWin)
                            {
                                CompleteHuanYingSiYuanScene(huanYingSiYuanScene, 1);
                            }
                        }
                        else
                        {
                            huanYingSiYuanScene.ScoreInfoData.Score2 += contextData.Score;
                            if (huanYingSiYuanScene.ScoreInfoData.Score2 >= RuntimeData.TempleMirageWin)
                            {
                                CompleteHuanYingSiYuanScene(huanYingSiYuanScene, 2);
                            }
                        }

                        HuanYingSiYuanLianShaContextData clientLianShaContextData = client.SceneContextData2 as HuanYingSiYuanLianShaContextData;
                        if (null != clientLianShaContextData)
                        {
                            clientLianShaContextData.TotalScore += contextData.Score;
                        }

                        HuanYingSiYuanAddScore huanYingSiYuanAddScore = new HuanYingSiYuanAddScore();
                        huanYingSiYuanAddScore.Name = Global.FormatRoleName4(client);
                        huanYingSiYuanAddScore.ZoneID = client.ClientData.ZoneID;
                        huanYingSiYuanAddScore.Side = client.ClientData.BattleWhichSide;
                        huanYingSiYuanAddScore.Score = contextData.Score;
                        huanYingSiYuanAddScore.RoleId = client.ClientData.RoleID;
                        huanYingSiYuanAddScore.Occupation = client.ClientData.Occupation;
                        copyMap = huanYingSiYuanScene.CopyMap;
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_HYSY_ADD_SCORE, huanYingSiYuanAddScore, copyMap);
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_HYSY_SCORE_INFO, huanYingSiYuanScene.ScoreInfoData, copyMap);
                    }
                }
            }
        }

        private void CheckShengBeiBufferTime(HuanYingSiYuanScene huanYingSiYuanScene, long nowTicks)
        {
            List<HuanYingSiYuanShengBeiContextData> shengBeiList = new List<HuanYingSiYuanShengBeiContextData>();
            lock (Mutex)
            {
                if (huanYingSiYuanScene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                {
                    if (huanYingSiYuanScene.ShengBeiContextDict.Count > 0)
                    {
                        foreach (var contextData in huanYingSiYuanScene.ShengBeiContextDict.Values)
                        {
                            //处理超时情况
                            if (contextData.EndTicks < nowTicks)
                            {
                                shengBeiList.Add(contextData);
                                if (contextData.OwnerRoleId != 0)
                                {
                                    GameClient client = GameManager.ClientMgr.FindClient(contextData.OwnerRoleId);
                                    if (null != client)
                                    {
                                        LostShengBei(client);
                                    }
                                }

                                contextData.OwnerRoleId = 0;
                                CreateMonster(huanYingSiYuanScene, contextData);
                            }
                        }
                    }

                    if (shengBeiList.Count > 0)
                    {
                        foreach (var contextData in shengBeiList)
                        {
                            huanYingSiYuanScene.ShengBeiContextDict.Remove(contextData.UniqueId);
                        }
                    }
                }
            }
        }

        private void TryLostShengBei(GameClient client)
        {
            lock (Mutex)
            {
                HuanYingSiYuanScene huanYingSiYuanScene = null;
                if (HuanYingSiYuanSceneDict.TryGetValue(client.ClientData.FuBenSeqID, out huanYingSiYuanScene))
                {
                    HuanYingSiYuanShengBeiContextData contextData = LostShengBei(client);
                    if (null != contextData)
                    {
                        contextData.OwnerRoleId = 0;
                        huanYingSiYuanScene.ShengBeiContextDict.Remove(contextData.UniqueId);
                        CreateMonster(huanYingSiYuanScene, contextData);
                    }
                }
            }

        }

        /// <summary>
        /// 给奖励
        /// </summary>
        public void GiveAwards(HuanYingSiYuanScene huanYingSiYuanScene)
        {
            try
            {
                List<GameClient> objsList = huanYingSiYuanScene.CopyMap.GetClientsList();
                if (objsList != null && objsList.Count > 0)
                {
                    int nowDayId = Global.GetOffsetDayNow();
                    for (int n = 0; n < objsList.Count; ++n)
                    {
                        GameClient client = objsList[n];
                        if (client != null && client == GameManager.ClientMgr.FindClient(client.ClientData.RoleID)) //确认角色仍然在线
                        {
                            bool success = false;
                            double nMultiple = 0.5;
                            int awardsRate = 1;
                            int count = 0;

                            HuanYingSiYuanLianShaContextData clientLianShaContextData = client.SceneContextData2 as HuanYingSiYuanLianShaContextData;
                            if (null != clientLianShaContextData && clientLianShaContextData.TotalScore >= RuntimeData.TempleMirageMinJiFen)
                            {
                                if (client.ClientData.BattleWhichSide == huanYingSiYuanScene.SuccessSide)
                                {
                                    success = true;
                                    nMultiple = 1;

                                    //每日前3次享受10倍奖励
                                    int dayid = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysySuccessDayId);
                                    if (dayid == nowDayId)
                                    {
                                        count = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysySuccessCount);
                                        if (count < RuntimeData.TempleMirageWinExtraNum)
                                        {
                                            awardsRate = RuntimeData.TempleMirageWinExtraRate;
                                        }
                                    }
                                    else
                                    {
                                        awardsRate = RuntimeData.TempleMirageWinExtraRate;
                                    }
                                }
                            }
                            else
                            {
                                //达不到最低分数,无奖励,不计次
                                nMultiple = 0;
                                awardsRate = 0;
                            }

                            // 公式
                            long nExp = (long)(RuntimeData.TempleMirageEXPAward * nMultiple * client.ClientData.ChangeLifeCount);
                            int chengJiuaward = (int)(RuntimeData.TempleMirageAwardChengJiu * nMultiple);
                            int shengWangaward = (int)(RuntimeData.TempleMirageAwardShengWang * nMultiple);

                            if (nExp > 0)
                            {
                                GameManager.ClientMgr.ProcessRoleExperience(client, nExp * awardsRate, false);
                                //GameManager.ClientMgr.NotifyAddExpMsg(client, nExp); //客户端自己提示,有显示"X10"的需求
                            }

                            if (chengJiuaward > 0)
                            {
                                ChengJiuManager.AddChengJiuPoints(client, "幻影寺院获得成就", chengJiuaward * awardsRate, true, true);
                            }

                            if (shengWangaward > 0)
                            {
                                GameManager.ClientMgr.ModifyShengWangValue(client, shengWangaward * awardsRate, "幻影寺院获得声望");
                            }

                            HuanYingSiYuanAwardsData awardsData = new HuanYingSiYuanAwardsData()
                            {
                                SuccessSide = huanYingSiYuanScene.SuccessSide,
                                Exp = nExp,
                                ShengWang = shengWangaward,
                                ChengJiuAward = chengJiuaward,
                                AwardsRate = awardsRate,
                            };

                            if (success)
                            {
                                if (nMultiple > 0)
                                {
                                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HysySuccessDayId, nowDayId, true);
                                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HysySuccessCount, count + 1, true);

                                    GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 0, 1, 0, (int)GameTypes.HuanYingSiYuan);
                                    if (huanYingSiYuanScene.ScoreInfoData.Score1 >= 1000 || huanYingSiYuanScene.ScoreInfoData.Score2 >= 1000)
                                    {
                                        //FaildCount记录为获得够1000分而获胜的人次
                                        GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 0, 0, 1, (int)GameTypes.HuanYingSiYuan);
                                    }
                                }
                            }
                            else
                            {
                                //FaildCount记录为失败人次
                                //GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 0, 0, 1);
                            }
                            
                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_HYSY_AWARD, awardsData);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteExceptionLogEx(ex, "幻影寺院清场调度异常");
            }

        }

        /// <summary>
        /// 玩家离开血色堡垒
        /// </summary>
        public void LeaveFuBen(GameClient client)
        {
            lock (Mutex)
            {
                TryLostShengBei(client);

                HuanYingSiYuanScene huanYingSiYuanScene = null;
                if (HuanYingSiYuanSceneDict.TryGetValue(client.ClientData.FuBenSeqID, out huanYingSiYuanScene))
                {
                    huanYingSiYuanScene.m_nPlarerCount--;

                    if (client.ClientData.BattleWhichSide == 1)
                    {
                        huanYingSiYuanScene.ScoreInfoData.Count1--;
                    }
                    else// if (client.ClientData.BattleWhichSide == 2)
                    {
                        huanYingSiYuanScene.ScoreInfoData.Count2--;
                    }

                    GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_HYSY_SCORE_INFO, huanYingSiYuanScene.ScoreInfoData, huanYingSiYuanScene.CopyMap);
                }
            }

            HuanYingSiYuanClient.getInstance().GameFuBenRoleChangeState(client.ClientData.RoleID, (int)KuaFuRoleStates.None);
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
