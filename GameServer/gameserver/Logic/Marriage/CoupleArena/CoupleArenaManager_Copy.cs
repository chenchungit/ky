#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Tmsk.Contract;
using KF.Contract.Data;
using KF.Client;
using Server.Tools;
using GameServer.Core.Executor;
using GameServer.Server;
using Server.Data;

namespace GameServer.Logic.Marriage.CoupleArena
{
    public partial class CoupleArenaManager
    {
        #region member 副本相关
        /// <summary>
        /// key: game id 
        /// value: fuben data
        /// </summary>
        private Dictionary<long, CoupleArenaFuBenData> GameId2FuBenData = new Dictionary<long, CoupleArenaFuBenData>();

        /// <summary>
        /// key: fuben seq
        /// value: copy scene
        /// </summary>
        private Dictionary<int, CoupleArenaCopyScene> FuBenSeq2CopyScenes = new Dictionary<int, CoupleArenaCopyScene>();

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long NextHeartBeatTicks = 0L;
        #endregion

        #region 跨服登录
        /// <summary>
        /// 跨服登录检测
        /// </summary>
        /// <param name="kuaFuServerLoginData"></param>
        /// <returns></returns>
        public bool CanKuaFuLogin(KuaFuServerLoginData kuaFuServerLoginData)
        {
            return true;
        }

        /// <summary>
        /// 跨服初始化游戏
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool KuaFuInitGame(GameClient client)
        {
            long gameId = Global.GetClientKuaFuServerLoginData(client).GameId;
            lock (Mutex)
            {
                CoupleArenaFuBenData fubenData = null;
                if (!GameId2FuBenData.TryGetValue(gameId, out fubenData))
                {
                    fubenData = TianTiClient.getInstance().GetFuBenData(gameId);
                    if (fubenData != null)
                    {
                        if (fubenData.FuBenSeq == 0)
                            fubenData.FuBenSeq = GameCoreInterface.getinstance().GetNewFuBenSeqId();
                        GameId2FuBenData.Add(gameId, fubenData);
                    }
                }

                if (fubenData == null) return false;
                if (fubenData.KfServerId != GameCoreInterface.getinstance().GetLocalServerId())
                    return false;

                KuaFuFuBenRoleData roleData = null;
                if (fubenData.RoleList == null ||
                    (roleData = fubenData.RoleList.Find(_r => _r.RoleId == client.ClientData.RoleID)) == null)
                    return false;

                client.ClientData.MapCode = WarCfg.MapCode;
                client.ClientData.BattleWhichSide = roleData.Side;
                int _posx = 0, _posy = 0;
                if (!GetBirthPoint(client.ClientData.MapCode, client.ClientData.BattleWhichSide,
                    out _posx, out _posy))
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("找不到出生点mapcode={0},side={1}", client.ClientData.MapCode, client.ClientData.BattleWhichSide));
                    return false;
                }

                client.ClientData.PosX = _posx;
                client.ClientData.PosY = _posy;
                Global.GetClientKuaFuServerLoginData(client).FuBenSeqId = fubenData.FuBenSeq;
                client.ClientData.FuBenSeqID = fubenData.FuBenSeq;
            }

            return true;
        }
        #endregion

        #region 复活、出生点
        /// <summary>
        /// 玩家复活
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool ClientRelive(GameClient client)
        {
            int toPosX, toPosY;
            if (client.ClientData.MapCode == WarCfg.MapCode)
            {
                if (!GetBirthPoint(WarCfg.MapCode, client.ClientData.BattleWhichSide, out toPosX, out toPosY))
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

            return false;
        }

        /// <summary>
        /// 获取出生点
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="side"></param>
        /// <param name="toPosX"></param>
        /// <param name="toPosY"></param>
        /// <returns></returns>
        private bool GetBirthPoint(int mapCode, int side, out int toPosX, out int toPosY)
        {
            toPosX = -1;
            toPosY = -1;

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out gameMap))
            {
                return false;
            }

            int defaultBirthPosX = BirthPointList[side % BirthPointList.Count].PosX;
            int defaultBirthPosY = BirthPointList[side % BirthPointList.Count].PosY;
            int defaultBirthRadius = BirthPointList[side % BirthPointList.Count].BirthRadius;

             //从配置根据地图取默认位置
             Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, mapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
            toPosX = (int)newPos.X;
            toPosY = (int)newPos.Y;

            toPosX = defaultBirthPosX;
            toPosY = defaultBirthPosY;

            return true;
        }
        #endregion

        #region 副本

        /// <summary>
        /// 客户端查询情侣竞技副本时间
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTimeStateInfoAndScoreInfo(GameClient client)
        {
            lock (Mutex)
            {
                CoupleArenaCopyScene copyScene;
                if (FuBenSeq2CopyScenes.TryGetValue(client.ClientData.FuBenSeqID, out copyScene))
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, copyScene.StateTimeData);
                }
            }
        }

        /// <summary>
        /// 玩家离开副本
        /// </summary>
        /// <param name="client"></param>
        public void OnLeaveFuBen(GameClient client)
        {
            lock (Mutex)
            {
                CoupleArenaCopyScene scene = null;
                if (FuBenSeq2CopyScenes.TryGetValue(client.ClientData.FuBenSeqID, out scene))
                {
                    if (scene.m_eStatus < GameSceneStatuses.STATUS_BEGIN)
                    {
                        // 尚未开始，退出也没事
                        scene.EnterRoleSide.Remove(client.ClientData.RoleID);
                    }
                    else if (scene.m_eStatus < GameSceneStatuses.STATUS_END)
                    {
                        CoupleArenaBuffCfg zhenAiBuffCfg = BuffCfgList.Find(_b => _b.Type == CoupleAreanConsts.ZhenAiBuffCfgType);
                        CoupleArenaBuffCfg yongQiBuffCfg = BuffCfgList.Find(_b => _b.Type == CoupleAreanConsts.YongQiBuffCfgType);
                        ModifyBuff(scene, client, BufferItemTypes.CoupleArena_YongQi_Buff, yongQiBuffCfg, false);
                        ModifyBuff(scene, client, BufferItemTypes.CoupleArena_ZhenAi_Buff, zhenAiBuffCfg, false);

                        scene.EnterRoleSide.Remove(client.ClientData.RoleID);
                        var leftSide = scene.EnterRoleSide.Values.ToList();
                        if (leftSide.Count(_s => _s == client.ClientData.BattleWhichSide) <= 0)
                        {
                            // 本方没人了, 剩下的对方胜利, 如果没有剩余的一方，那么本次pk无效
                            scene.WinSide = leftSide.Count > 0 ? leftSide[0] : 0;
                            ProcessEnd(scene, TimeUtil.NowDateTime(), TimeUtil.NOW());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 添加副本
        /// </summary>
        /// <param name="client"></param>
        /// <param name="copyMap"></param>
        /// <param name="sceneType"></param>
        public void AddCopyScenes(GameClient client, CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (sceneType != SceneUIClasses.CoupleArena) return;

            int fuBenSeqId = copyMap.FuBenSeqID;
            int mapCode = copyMap.MapCode;
            lock (Mutex)
            {
                CoupleArenaCopyScene scene = null;
                if (!this.FuBenSeq2CopyScenes.TryGetValue(fuBenSeqId, out scene))
                {
                    scene = new CoupleArenaCopyScene();
                    scene.GameId = (int)Global.GetClientKuaFuServerLoginData(client).GameId;
                    scene.FuBenSeq = fuBenSeqId;
                    scene.MapCode = mapCode;
                    scene.CopyMap = copyMap;

                    FuBenSeq2CopyScenes[fuBenSeqId] = scene;
                }

                scene.EnterRoleSide[client.ClientData.RoleID] = client.ClientData.BattleWhichSide;
                copyMap.IsKuaFuCopy = true;
                copyMap.SetRemoveTicks(TimeUtil.NOW() + (WarCfg.WaitSec + WarCfg.FightSec + WarCfg.ClearSec + 120) * TimeUtil.SECOND);
                GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 1, 0, 0, (int)GameTypes.CoupleArena);
            }
        }

        /// <summary>
        /// 移除副本
        /// </summary>
        /// <param name="copyMap"></param>
        /// <param name="sceneType"></param>
        public void RemoveCopyScene(CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (copyMap == null || sceneType != SceneUIClasses.CoupleArena)
                return;

            lock (Mutex)
            {
                CoupleArenaCopyScene scene = null;
                if (FuBenSeq2CopyScenes.TryGetValue(copyMap.FuBenSeqID, out scene))
                {
                    FuBenSeq2CopyScenes.Remove(copyMap.FuBenSeqID);
                    GameId2FuBenData.Remove(scene.GameId);
                }
            }
        }

        /// <summary>
        /// timer 更新副本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateCopyScene()
        {
            DateTime now = TimeUtil.NowDateTime();
            long nowTicks = now.Ticks / TimeSpan.TicksPerMillisecond;
            if (nowTicks < NextHeartBeatTicks)
            {
                return;
            }

            NextHeartBeatTicks = nowTicks + 1020; //1020毫秒执行一次

            lock (Mutex)
            {
                foreach (var scene in this.FuBenSeq2CopyScenes.Values.ToList())
                {
                    scene.m_lPrevUpdateTime = scene.m_lCurrUpdateTime;
                    scene.m_lCurrUpdateTime = nowTicks;

                    if (scene.m_eStatus == GameSceneStatuses.STATUS_NULL)             // 如果处于空状态 -- 是否要切换到准备状态
                    {
                        NtfBuffHoldData(scene);
                        scene.m_lPrepareTime = nowTicks;
                        scene.m_lBeginTime = nowTicks + WarCfg.WaitSec * TimeUtil.SECOND;
                        scene.m_eStatus = GameSceneStatuses.STATUS_PREPARE;

                        scene.StateTimeData.GameType = (int)GameTypes.CoupleArena;
                        scene.StateTimeData.State = (int)scene.m_eStatus;
                        scene.StateTimeData.EndTicks = scene.m_lBeginTime;
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_PREPARE)     // 场景战斗状态切换
                    {
                        if (nowTicks >= scene.m_lBeginTime)
                        {
                            // 至少有一方无人进入
                            if (scene.EnterRoleSide.Values.ToList().Distinct().Count() <= 1)
                            {
                                scene.WinSide = 0;
                                scene.m_eStatus = GameSceneStatuses.STATUS_END;
                            }
                            else
                            {
                                NtfBuffHoldData(scene);
                                scene.m_eStatus = GameSceneStatuses.STATUS_BEGIN;
                                scene.m_lEndTime = nowTicks + WarCfg.FightSec * TimeUtil.SECOND;

                                scene.StateTimeData.GameType = (int)GameTypes.CoupleArena;
                                scene.StateTimeData.State = (int)scene.m_eStatus;
                                scene.StateTimeData.EndTicks = scene.m_lEndTime;

                                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);
                                //放开光幕
                                scene.CopyMap.AddGuangMuEvent(1, 0);
                                GameManager.ClientMgr.BroadSpecialMapAIEvent(scene.CopyMap.MapCode, scene.CopyMap.CopyMapID, 1, 0);
                                scene.CopyMap.AddGuangMuEvent(2, 0);
                                GameManager.ClientMgr.BroadSpecialMapAIEvent(scene.CopyMap.MapCode, scene.CopyMap.CopyMapID, 2, 0);
                            }
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)       // 战斗开始
                    {
                        if (nowTicks >= scene.m_lEndTime)
                        {
                            // 超时结束，拥有真爱buff的一方胜利
                            scene.m_eStatus = GameSceneStatuses.STATUS_END;

                            if (!scene.EnterRoleSide.TryGetValue(scene.ZhenAiBuff_Role, out scene.WinSide))
                                scene.WinSide = 0;
                        }
                        else if (scene.EnterRoleSide.ContainsKey(scene.ZhenAiBuff_Role)
                            && (nowTicks - scene.ZhenAiBuff_StartMs) >= ZhenAiBuffHoldWinSec * 1000)
                        {
                            // 拥有真爱buff超过一段时间的一方胜利
                            scene.m_eStatus = GameSceneStatuses.STATUS_END;
                            scene.WinSide = scene.EnterRoleSide[scene.ZhenAiBuff_Role];
                        }
                        else
                        {
                            CheckFlushZhenAiMonster(scene);
                            CheckFlushYongQiMonster(scene);
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_END)         // 战斗结束
                    {
                        ProcessEnd(scene, now, nowTicks);
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_AWARD)
                    {
                        if (nowTicks >= scene.m_lLeaveTime)
                        {
                            scene.m_eStatus = GameSceneStatuses.STATUS_CLEAR;
                            scene.CopyMap.SetRemoveTicks(scene.m_lLeaveTime);
                            try
                            {
                                List<GameClient> objsList = scene.CopyMap.GetClientsList();
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
                                DataHelper.WriteExceptionLogEx(ex, "情侣竞技系统清场调度异常");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检测刷新真爱buff怪物
        /// </summary>
        /// <param name="scene"></param>
        private void CheckFlushZhenAiMonster(CoupleArenaCopyScene scene)
        {
            if (!scene.IsZhenAiMonsterExist && !scene.EnterRoleSide.ContainsKey(scene.ZhenAiBuff_Role))
            {
                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(scene.MapCode, out gameMap))
                {
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("缺少情侣竞技地图 {0}", scene.MapCode));
                    return;
                }

                CoupleArenaBuffCfg buffCfg = BuffCfgList.Find(_b => _b.Type == CoupleAreanConsts.ZhenAiBuffCfgType);
                if (buffCfg == null) return;

                var pos = buffCfg.RandPosList[Global.GetRandomNumber(0, buffCfg.RandPosList.Count)];

                GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.MapCode, buffCfg.MonsterId,
                    scene.CopyMap.CopyMapID, 1,
                    pos.X / gameMap.MapGridWidth, pos.Y / gameMap.MapGridHeight, pos.R, 0,
                    SceneUIClasses.CoupleArena, null);

                scene.IsZhenAiMonsterExist = true;
            }
        }

        /// <summary>
        /// 检测刷新勇气buff怪物
        /// </summary>
        /// <param name="scene"></param>
        private void CheckFlushYongQiMonster(CoupleArenaCopyScene scene)
        {
            CoupleArenaBuffCfg buffCfg = BuffCfgList.Find(_b => _b.Type == CoupleAreanConsts.YongQiBuffCfgType);
            if (buffCfg == null) return;
            bool isInFlusTime = false;
            foreach (int sec in buffCfg.FlushSecList)
            {
                if (scene.m_lPrevUpdateTime - scene.m_lBeginTime <= sec * 1000
                    && scene.m_lCurrUpdateTime - scene.m_lBeginTime >= sec * 1000)
                {
                    isInFlusTime = true;
                    break;
                }
            }

            if (isInFlusTime && !scene.IsYongQiMonsterExist && !scene.EnterRoleSide.ContainsKey(scene.YongQiBuff_Role))
            {
                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(scene.MapCode, out gameMap))
                {
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("缺少情侣竞技地图 {0}", scene.MapCode));
                    return;
                }

                var pos = buffCfg.RandPosList[Global.GetRandomNumber(0, buffCfg.RandPosList.Count)];

                GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.MapCode, buffCfg.MonsterId,
                    scene.CopyMap.CopyMapID, 1,
                    pos.X / gameMap.MapGridWidth, pos.Y / gameMap.MapGridHeight, pos.R, 0,
                    SceneUIClasses.CoupleArena, null);

                scene.IsYongQiMonsterExist = true;
            }
        }

        /// <summary>
        /// 副本结算
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="now"></param>
        /// <param name="nowTicks"></param>
        private void ProcessEnd(CoupleArenaCopyScene scene, DateTime now, long nowTicks)
        {
            GameManager.CopyMapMgr.KillAllMonster(scene.CopyMap);

            //结算奖励
            scene.m_eStatus = GameSceneStatuses.STATUS_AWARD;
            scene.m_lEndTime = nowTicks;
            scene.m_lLeaveTime = scene.m_lEndTime + WarCfg.ClearSec * TimeUtil.SECOND;

            scene.StateTimeData.GameType = (int)GameTypes.CoupleArena;
            scene.StateTimeData.State = (int)GameSceneStatuses.STATUS_END;
            scene.StateTimeData.EndTicks = scene.m_lLeaveTime;
            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, scene.CopyMap);

            CoupleArenaFuBenData fubenData = null;
            if (!GameId2FuBenData.TryGetValue(scene.GameId, out fubenData))
                return;

            List<RoleData4Selector> selectorList = new List<RoleData4Selector>();
            foreach (var roledata in fubenData.RoleList)
            {
                RoleData4Selector _roleInfo = Global.sendToDB<RoleData4Selector,
                    string>((int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", roledata.RoleId), roledata.ServerId);
                if (_roleInfo == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载RoleData4Selector失败, serverid={0}, roleid={1}", roledata.ServerId, roledata.RoleId));
                    return;
                }
                selectorList.Add(_roleInfo);
            }

            if (selectorList[0].RoleSex == (int)ERoleSex.Girl)
            {
                var tmp = selectorList[0];
                selectorList[0] = selectorList[1];
                selectorList[1] = tmp;
            }

            if (selectorList[2].RoleSex == (int)ERoleSex.Girl)
            {
                var tmp = selectorList[2];
                selectorList[2] = selectorList[3];
                selectorList[3] = tmp;
            }

            // selectorList 存储的分别为男1，女1，男2，女2
            CoupleArenaPkResultReq req = new CoupleArenaPkResultReq();
            req.GameId = scene.GameId;
            req.winSide = scene.WinSide;

            req.ManRole1 = selectorList[0].RoleID;
            req.ManZoneId1 = selectorList[0].ZoneId;
            req.ManSelector1 = DataHelper.ObjectToBytes<RoleData4Selector>(selectorList[0]);
            req.WifeRole1 = selectorList[1].RoleID;
            req.WifeZoneId1 = selectorList[1].ZoneId;
            req.WifeSelector1 = DataHelper.ObjectToBytes<RoleData4Selector>(selectorList[1]);
            req.ManRole2 = selectorList[2].RoleID;
            req.ManZoneId2 = selectorList[2].ZoneId;
            req.ManSelector2 = DataHelper.ObjectToBytes<RoleData4Selector>(selectorList[2]);
            req.WifeRole2 = selectorList[3].RoleID;
            req.WifeZoneId2 = selectorList[3].ZoneId;
            req.WifeSelector2 = DataHelper.ObjectToBytes<RoleData4Selector>(selectorList[3]);
            CoupleArenaPkResultRsp rsp = TianTiClient.getInstance().CoupleArenaPkResult(req);
            if (rsp == null) return;

            if (rsp.Couple1RetData != null)
            {
                if (rsp.Couple1RetData.Result != (int)ECoupleArenaPkResult.Invalid)
                {
                    CoupleArenaZhanBaoSaveDbData saveData = new CoupleArenaZhanBaoSaveDbData();
                    saveData.FromMan = req.ManRole1;
                    saveData.FromWife = req.WifeRole1;
                    saveData.FirstWeekday = TimeUtil.MakeFirstWeekday(now);
                    saveData.ZhanBao = new CoupleArenaZhanBaoItemData()
                    {
                        TargetManZoneId = req.ManZoneId2,
                        TargetManRoleName = selectorList[2].RoleName,
                        TargetWifeZoneId = req.WifeZoneId2,
                        TargetWifeRoleName = selectorList[3].RoleName,
                        IsWin = rsp.Couple1RetData.Result == (int)ECoupleArenaPkResult.Win,
                        GetJiFen = rsp.Couple1RetData.GetJiFen
                    };
                    Global.sendToDB<bool, CoupleArenaZhanBaoSaveDbData>((int)TCPGameServerCmds.CMD_COUPLE_ARENA_DB_SAVE_ZHAN_BAO, saveData, fubenData.RoleList[0].ServerId);
                }
                NtfAwardData(req.ManRole1, rsp.Couple1RetData);
                NtfAwardData(req.WifeRole1, rsp.Couple1RetData);
            }

            if (rsp.Couple2RetData != null)
            {
                if (rsp.Couple2RetData.Result != (int)ECoupleArenaPkResult.Invalid)
                {
                    CoupleArenaZhanBaoSaveDbData saveData = new CoupleArenaZhanBaoSaveDbData();
                    saveData.FirstWeekday = TimeUtil.MakeFirstWeekday(now);
                    saveData.FromMan = req.ManRole2;
                    saveData.FromWife = req.WifeRole2;
                    saveData.ZhanBao = new CoupleArenaZhanBaoItemData()
                    {
                        TargetManZoneId = req.ManZoneId1,
                        TargetManRoleName = selectorList[0].RoleName,
                        TargetWifeZoneId = req.WifeZoneId1,
                        TargetWifeRoleName = selectorList[1].RoleName,
                        IsWin = rsp.Couple2RetData.Result == (int)ECoupleArenaPkResult.Win,
                        GetJiFen = rsp.Couple2RetData.GetJiFen
                    };
                    Global.sendToDB<bool, CoupleArenaZhanBaoSaveDbData>((int)TCPGameServerCmds.CMD_COUPLE_ARENA_DB_SAVE_ZHAN_BAO, saveData, fubenData.RoleList[2].ServerId);
                }
                NtfAwardData(req.ManRole2, rsp.Couple2RetData);
                NtfAwardData(req.WifeRole2, rsp.Couple2RetData);
            }
        }

        /// <summary>
        /// 通知客户端奖励信息
        /// </summary>
        /// <param name="roleid"></param>
        /// <param name="retItem"></param>
        private void NtfAwardData(int roleid, CoupleArenaPkResultItem retItem)
        {
            GameClient client = GameManager.ClientMgr.FindClient(roleid);
            if (client == null || client.ClientData.MapCode != WarCfg.MapCode)
                return;

            var cfg = DuanWeiCfgList.Find(_d => _d.Type == retItem.OldDuanWeiType && _d.Level == retItem.OldDuanWeiLevel);
            if (cfg == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("NtfAwardData 段位配置找不到 type={0},level={1}", retItem.OldDuanWeiType, retItem.OldDuanWeiLevel));
                return;
            }

            DateTime now = TimeUtil.NowDateTime();
            int getRongYaoTimes = 0;
            bool canGetRongYao = false;
            string szWeekRongYao = Global.GetRoleParamByName(client, RoleParamName.CoupleArenaWeekRongYao);
            if (!string.IsNullOrEmpty(szWeekRongYao))
            {
                string[] fields = szWeekRongYao.Split(',');
                if (fields != null && fields.Length == 2 && Convert.ToInt32(fields[0]) == TimeUtil.MakeFirstWeekday(now))
                {
                    getRongYaoTimes = Convert.ToInt32(fields[1]);
                }
            }

            CoupleArenaPkResultData data = new CoupleArenaPkResultData();
            data.PKResult = retItem.Result;
            data.DuanWeiType = retItem.NewDuanWeiType;
            data.DuanWeiLevel = retItem.NewDuanWeiLevel;
            data.GetJiFen = retItem.GetJiFen;
            if (retItem.Result != (int)ECoupleArenaPkResult.Invalid && getRongYaoTimes < cfg.WeekGetRongYaoTimes)
            {
                data.GetRongYao = retItem.Result == (int)ECoupleArenaPkResult.Win ? cfg.WinRongYao : cfg.LoseRongYao;
                canGetRongYao = true;
            }

            if (canGetRongYao)
            {
                GameManager.ClientMgr.ModifyTianTiRongYaoValue(client, data.GetRongYao, "情侣竞技系统获得荣耀", true);
                Global.SaveRoleParamsStringToDB(client, RoleParamName.CoupleArenaWeekRongYao, string.Format("{0},{1}", TimeUtil.MakeFirstWeekday(now), getRongYaoTimes + 1), true);
            }

            client.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_ARENA_NTF_PK_RESULT, data);
        }
        #endregion

        #region buff
        /// <summary>
        /// 怪物死亡，检测给予玩家buff
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        private void OnMonsterDead(CoupleArenaCopyScene scene, GameClient client, Monster monster)
        {
            lock (Mutex)
            {
                if (scene.m_eStatus < GameSceneStatuses.STATUS_BEGIN || scene.m_eStatus >= GameSceneStatuses.STATUS_END)
                    return;

                if (!scene.EnterRoleSide.ContainsKey(client.ClientData.RoleID))
                    return;

                CoupleArenaBuffCfg zhenAiBuffCfg = BuffCfgList.Find(_b => _b.Type == CoupleAreanConsts.ZhenAiBuffCfgType);
                CoupleArenaBuffCfg yongQiBuffCfg = BuffCfgList.Find(_b => _b.Type == CoupleAreanConsts.YongQiBuffCfgType);
#if ___CC___FUCK___YOU___BB___
                // 击杀真爱怪物，将获得真爱Buff
                if (scene.IsZhenAiMonsterExist
                    && monster.XMonsterInfo.MonsterId == zhenAiBuffCfg.MonsterId)
                {
                    scene.IsZhenAiMonsterExist = false;
                    // 真爱buff 会 顶掉勇气buff， 先移除勇气buff，再添加真爱buff
                    ModifyBuff(scene, client, BufferItemTypes.CoupleArena_YongQi_Buff, yongQiBuffCfg, false);
                    ModifyBuff(scene, client, BufferItemTypes.CoupleArena_ZhenAi_Buff, zhenAiBuffCfg, true);
                }

                // 击杀勇气怪物，将获得勇气Buff
                if (scene.IsYongQiMonsterExist
                    && monster.XMonsterInfo.MonsterId == yongQiBuffCfg.MonsterId)
                {
                    scene.IsYongQiMonsterExist = false;
                    ModifyBuff(scene, client, BufferItemTypes.CoupleArena_YongQi_Buff, yongQiBuffCfg, true);
                }
#else
                if (scene.IsZhenAiMonsterExist
                    && monster.MonsterInfo.ExtensionID == zhenAiBuffCfg.MonsterId)
                {
                    scene.IsZhenAiMonsterExist = false;
                    // 真爱buff 会 顶掉勇气buff， 先移除勇气buff，再添加真爱buff
                    ModifyBuff(scene, client, BufferItemTypes.CoupleArena_YongQi_Buff, yongQiBuffCfg, false);
                    ModifyBuff(scene, client, BufferItemTypes.CoupleArena_ZhenAi_Buff, zhenAiBuffCfg, true);
                }
                // 击杀勇气怪物，将获得勇气Buff
                if (scene.IsYongQiMonsterExist
                    && monster.MonsterInfo.ExtensionID == yongQiBuffCfg.MonsterId)
                {
                    scene.IsYongQiMonsterExist = false;
                    ModifyBuff(scene, client, BufferItemTypes.CoupleArena_YongQi_Buff, yongQiBuffCfg, true);
                }
#endif
            }
        }

        /// <summary>
        /// 玩家死亡，检测玩家buff变更
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="deader"></param>
        /// <param name="killer"></param>
        private void OnPlayerDead(CoupleArenaCopyScene scene, GameClient deader, GameClient killer)
        {
            lock (Mutex)
            {
                if (scene.m_eStatus < GameSceneStatuses.STATUS_BEGIN || scene.m_eStatus >= GameSceneStatuses.STATUS_END)
                    return;

                if (!scene.EnterRoleSide.ContainsKey(deader.ClientData.RoleID))
                    return;

                // 有可能是被怪杀死的，killer为null
                if (killer != null && !scene.EnterRoleSide.ContainsKey(killer.ClientData.RoleID))
                    return;

                CoupleArenaBuffCfg zhenAiBuffCfg = BuffCfgList.Find(_b => _b.Type == CoupleAreanConsts.ZhenAiBuffCfgType);
                CoupleArenaBuffCfg yongQiBuffCfg = BuffCfgList.Find(_b => _b.Type == CoupleAreanConsts.YongQiBuffCfgType);

                // 拥有真爱buff的玩家死亡，转移真爱buff
                if (scene.ZhenAiBuff_Role == deader.ClientData.RoleID)
                {
                    ModifyBuff(scene, deader, BufferItemTypes.CoupleArena_ZhenAi_Buff, zhenAiBuffCfg, false);
                    // 真爱buff 会 顶掉勇气buff， 先移除勇气buff，再添加真爱buff
                    ModifyBuff(scene, killer, BufferItemTypes.CoupleArena_YongQi_Buff, yongQiBuffCfg, false);
                    ModifyBuff(scene, killer, BufferItemTypes.CoupleArena_ZhenAi_Buff, zhenAiBuffCfg, true);
                }

                // 拥有勇气buff的玩家死亡，移除勇气buff
                if (scene.YongQiBuff_Role == deader.ClientData.RoleID)
                {
                    ModifyBuff(scene, deader, BufferItemTypes.CoupleArena_YongQi_Buff, yongQiBuffCfg, false);
                }
            }         
        }

        /// <summary>
        /// 添加、移除buff
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="client"></param>
        /// <param name="buffType"></param>
        /// <param name="buffCfg"></param>
        /// <param name="bAdd"></param>
        private void ModifyBuff(CoupleArenaCopyScene scene, GameClient client, BufferItemTypes buffType, CoupleArenaBuffCfg buffCfg, bool bAdd)
        {
            if (scene == null || client == null || buffCfg == null) return;

            lock (Mutex)
            {
                bool bChanged = false;
                BufferData buffData = Global.GetBufferDataByID(client, (int)buffType);

                int noSaveDbBuffType = 1;

                if (bAdd && (buffData == null || Global.IsBufferDataOver(buffData)))
                {
                    // 持有真爱buff的玩家无法获得勇气buff
                    if (buffType != BufferItemTypes.CoupleArena_YongQi_Buff
                        || scene.ZhenAiBuff_Role != client.ClientData.RoleID)
                    {
                        double[] bufferParams = new double[1] { 1 };
                        Global.UpdateBufferData(client, buffType, bufferParams, noSaveDbBuffType);

                        foreach (var prop in buffCfg.ExtProps)
                        {
                            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.CoupleArena, (int)prop.Key, prop.Value);
                        }

                        bChanged = true;
                    }
                }

                if (!bAdd && buffData != null && !Global.IsBufferDataOver(buffData))
                {
                    double[] bufferParams = new double[1] { 0 };
                    Global.UpdateBufferData(client, buffType, bufferParams, noSaveDbBuffType);

                    foreach (var prop in buffCfg.ExtProps)
                    {
                        client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.CoupleArena, (int)prop.Key, 0);
                    }

                    bChanged = true;
                }

                if (bChanged)
                {
                    if (buffType == BufferItemTypes.CoupleArena_ZhenAi_Buff)
                    {
                        if (bAdd)
                        {
                            scene.ZhenAiBuff_Role = client.ClientData.RoleID;
                            scene.ZhenAiBuff_StartMs = TimeUtil.NOW();
                        }
                        else
                            scene.ZhenAiBuff_Role = 0;
                    }
                    else if (buffType == BufferItemTypes.CoupleArena_YongQi_Buff)
                    {
                        if (bAdd)
                            scene.YongQiBuff_Role = client.ClientData.RoleID;
                        else
                            scene.YongQiBuff_Role = 0;
                    }

                    NtfBuffHoldData(scene);
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                    // 总生命值和魔法值变化通知(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }
            }          
        }      

        /// <summary>
        /// 通知客户端buff信息
        /// </summary>
        /// <param name="scene"></param>
        private void NtfBuffHoldData(CoupleArenaCopyScene scene)
        {
            lock (Mutex)
            {
                CoupleArenaFuBenData fuben;
                if (!GameId2FuBenData.TryGetValue(scene.GameId, out fuben))
                    return;
                GameClient client = null;

                CoupleArenaBuffHoldData holdData = new CoupleArenaBuffHoldData();
                client = GameManager.ClientMgr.FindClient(scene.ZhenAiBuff_Role);
                if (client != null && scene.EnterRoleSide.ContainsKey(client.ClientData.RoleID))
                {
                    holdData.IsZhenAiBuffValid = true;
                    holdData.ZhenAiHolderZoneId = client.ClientData.ZoneID;
                    holdData.ZhenAiHolderRname = client.ClientData.RoleName;
                }
                else holdData.IsZhenAiBuffValid = false;

                client = GameManager.ClientMgr.FindClient(scene.YongQiBuff_Role);
                if (client != null && scene.EnterRoleSide.ContainsKey(scene.YongQiBuff_Role))
                {
                    holdData.IsYongQiBuffValid = true;
                    holdData.YongQiHolderZoneId = client.ClientData.ZoneID;
                    holdData.YongQiHolderRname = client.ClientData.RoleName;
                }
                else holdData.IsYongQiBuffValid = false;             

                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_COUPLE_ARENA_NTF_BUFF_HOLDER, holdData, scene.CopyMap);
            }
        }
#endregion
    }
}
