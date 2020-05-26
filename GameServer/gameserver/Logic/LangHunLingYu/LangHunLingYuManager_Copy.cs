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
    public partial class LangHunLingYuManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 副本

        private void InitScene(LangHunLingYuScene scene, GameClient client)
        {
            foreach (var item in RuntimeData.QiZhiBuffOwnerDataList)
            {
                scene.QiZhiBuffOwnerDataList.Add(new LangHunLingYuQiZhiBuffOwnerData() { NPCID = item.NPCID });
            }
            foreach (var item in RuntimeData.NPCID2QiZhiConfigDict.Values)
            {
                scene.NPCID2QiZhiConfigDict.Add(item.NPCID, item.Clone() as QiZhiConfig);
            }
        }

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public bool AddCopyScenes(GameClient client, CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (sceneType == SceneUIClasses.LangHunLingYu)
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
                    LangHunLingYuScene scene = null;
                    if (!RuntimeData.SceneDict.TryGetValue(fuBenSeqId, out scene))
                    {
                        LangHunLingYuFuBenData fuBenData;
                        if (!RuntimeData.FuBenDataDict.TryGetValue(gameId, out fuBenData))
                        {
                            LogManager.WriteLog(LogTypes.Error, "圣域争霸没有为副本找到对应的跨服副本数据,GameID:" + gameId);
                        }

                        scene = new LangHunLingYuScene();
                        scene.CleanAllInfo();
                        scene.GameId = gameId;
                        RuntimeData.MapGridWidth = gameMap.MapGridWidth;
                        RuntimeData.MapGridHeight = gameMap.MapGridHeight;
                        int cityLevel = GetCityLevelById(fuBenData.CityId);
                        if(!RuntimeData.CityLevelInfoDict.TryGetValue(cityLevel, out scene.LevelInfo))
                        {
                            LogManager.WriteLog(LogTypes.Error, "圣域争霸没有为副本找到对应的城池等级配置:CityId=" + fuBenData.CityId);
                        }

                        scene.SceneInfo = client.SceneInfoObject as LangHunLingYuSceneInfo;

                        DateTime startTime = now.Date.Add(GetStartTime(scene.LevelInfo.ID));
                        scene.StartTimeTicks = startTime.Ticks / 10000;
                        scene.m_lEndTime = scene.StartTimeTicks + (scene.SceneInfo.PrepareSecs + scene.SceneInfo.FightingSecs) * TimeUtil.SECOND;
                        InitScene(scene, client);

                        RuntimeData.SceneDict[fuBenSeqId] = scene;
                        scene.CityData.CityId = fuBenData.CityDataEx.CityId;
                        scene.CityData.CityLevel = fuBenData.CityDataEx.CityLevel;
                        LangHunLingYuBangHuiDataEx bangHuiDataEx;
                        if (RuntimeData.BangHuiDataExDict.TryGetValue(fuBenData.CityDataEx.Site[0], out bangHuiDataEx))
                        {
                            scene.LongTaOwnerData.OwnerBHid = bangHuiDataEx.Bhid;
                            scene.LongTaOwnerData.OwnerBHName = bangHuiDataEx.BhName;
                            scene.LongTaOwnerData.OwnerBHZoneId = bangHuiDataEx.ZoneId;
                        }
                    }

                    scene.CopyMapDict[mapCode] = copyMap;

                    int bhid = client.ClientData.Faction;
                    if (!RuntimeData.BangHuiMiniDataCacheDict.ContainsKey(bhid))
                    {
                        RuntimeData.BangHuiMiniDataCacheDict[bhid] = Global.GetBangHuiMiniData(bhid, client.ServerId);
                    }

                    LangHunLingYuClientContextData clientContextData;
                    if (!scene.ClientContextDataDict.TryGetValue(roleId, out clientContextData))
                    {
                        clientContextData = new LangHunLingYuClientContextData() { RoleId = roleId, ServerId = client.ServerId, BattleWhichSide = client.ClientData.BattleWhichSide };
                        scene.ClientContextDataDict[roleId] = clientContextData;
                    }

                    client.SceneObject = scene;
                    client.SceneGameId = scene.GameId;
                    client.SceneContextData2 = clientContextData;

                    copyMap.SetRemoveTicks(scene.StartTimeTicks + scene.SceneInfo.TotalSecs * TimeUtil.SECOND);
                    copyMap.IsKuaFuCopy = true;
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
            if (sceneType == SceneUIClasses.LangHunLingYu)
            {
                lock (RuntimeData.Mutex)
                {
                    LangHunLingYuScene LangHunLingYuScene;
                    RuntimeData.SceneDict.TryRemove(copyMap.FuBenSeqID, out LangHunLingYuScene);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 玩家离开血色堡垒
        /// </summary>
        public void OnLogout(GameClient client)
        {
            YongZheZhanChangClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.StartGame);
        }

        #endregion 副本

        #region 活动逻辑

        /// <summary>
        /// 心跳处理
        /// </summary>
        public void TimerProc(object sender, EventArgs e)
        {
            lock (RuntimeData.Mutex)
            {
                if (RuntimeData.StatisticalDataQueue.Count > 0)
                {
                    LangHunLingYuStatisticalData data = RuntimeData.StatisticalDataQueue.Peek();
                    int result = YongZheZhanChangClient.getInstance().GameFuBenComplete(data);
                    if (result >= 0)
                    {
                        RuntimeData.StatisticalDataQueue.Dequeue();
                    }
                }
            }

            foreach (var scene in RuntimeData.SceneDict.Values)
            {
                lock (RuntimeData.Mutex)
                {
                    // 当前tick
                    DateTime now = TimeUtil.NowDateTime();
                    long ticks = TimeUtil.NOW();

                    if (scene.m_eStatus == GameSceneStatuses.STATUS_NULL)             // 如果处于空状态 -- 是否要切换到准备状态
                    {
                        if (ticks >= scene.StartTimeTicks)
                        {
                            LangHunLingYuFuBenData fuBenData;
                            if (RuntimeData.FuBenDataDict.TryGetValue(scene.GameId, out fuBenData) && fuBenData.State == GameFuBenState.End)
                            {
                                scene.m_eStatus = GameSceneStatuses.STATUS_AWARD;
                                scene.m_lLeaveTime = TimeUtil.NOW();
                            }

                            scene.m_lPrepareTime = scene.StartTimeTicks;
                            scene.m_lBeginTime = scene.m_lPrepareTime + scene.SceneInfo.PrepareSecs * TimeUtil.SECOND;
                            scene.m_eStatus = GameSceneStatuses.STATUS_PREPARE;

                            scene.StateTimeData.GameType = (int)GameTypes.LangHunLingYu;
                            scene.StateTimeData.State = (int)scene.m_eStatus;
                            scene.StateTimeData.EndTicks = scene.m_lBeginTime;
                            foreach (var copy in scene.CopyMapDict.Values)
                            {
                                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, copy);
                            }
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_PREPARE)     // 场景战斗状态切换
                    {
                        if (ticks >= scene.m_lBeginTime)
                        {
                            scene.m_eStatus = GameSceneStatuses.STATUS_BEGIN;
                            scene.m_lEndTime = scene.m_lBeginTime + scene.SceneInfo.FightingSecs * TimeUtil.SECOND;

                            scene.StateTimeData.GameType = (int)GameTypes.LangHunLingYu;
                            scene.StateTimeData.State = (int)scene.m_eStatus;
                            scene.StateTimeData.EndTicks = scene.m_lEndTime;
                            foreach (var copy in scene.CopyMapDict.Values)
                            {
                                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, copy);
                            }
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_BEGIN)       // 战斗开始
                    {
                        if (ticks >= scene.m_lEndTime)
                        {
                            scene.m_eStatus = GameSceneStatuses.STATUS_END;
                            scene.m_lLeaveTime = scene.m_lEndTime + scene.SceneInfo.ClearRolesSecs * TimeUtil.SECOND;

                            scene.StateTimeData.GameType = (int)GameTypes.LangHunLingYu;
                            scene.StateTimeData.State = (int)GameSceneStatuses.STATUS_CLEAR;
                            scene.StateTimeData.EndTicks = scene.m_lLeaveTime;
                            foreach (var copy in scene.CopyMapDict.Values)
                            {
                                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData, copy);
                            }

                            ProcessWangChengZhanResult(scene, true);
                        }
                        else
                        {
                            ProcessWangChengZhanResult(scene, false);
                        }
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_END)         // 战斗结束
                    {
                        //结算奖励
                        scene.m_eStatus = GameSceneStatuses.STATUS_AWARD;

                        LangHunLingYuStatisticalData statisticalData = new LangHunLingYuStatisticalData();
                        statisticalData.CompliteTime = TimeUtil.NowDateTime();
                        statisticalData.CityId = scene.CityData.CityId;
                        statisticalData.GameId = scene.GameId;
                        statisticalData.SiteBhids[0] = scene.LongTaOwnerData.OwnerBHid;
                        LangHunLingYuBuildMaxCityOwnerInfo(statisticalData, scene.LongTaOwnerData.OwnerBHServerId);
                        RuntimeData.StatisticalDataQueue.Enqueue(statisticalData);
                        LangHunLingYuFuBenData fuBenData;
                        if (RuntimeData.FuBenDataDict.TryGetValue(scene.GameId, out fuBenData))
                        {
                            fuBenData.State = GameFuBenState.End;
                        }

                        EventLogManager.AddGameEvent(LogRecordType.LangHunLingYuResult, statisticalData.GameId, statisticalData.CityId, statisticalData.SiteBhids[0]);
                    }
                    else if (scene.m_eStatus == GameSceneStatuses.STATUS_AWARD)
                    {
                        if (ticks >= scene.m_lLeaveTime)
                        {
                            foreach (var copy in scene.CopyMapDict.Values)
                            {
                                copy.SetRemoveTicks(scene.m_lLeaveTime);
                                try
                                {
                                    List<GameClient> objsList = copy.GetClientsList();
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
                                    DataHelper.WriteExceptionLogEx(ex, "圣域争霸系统清场调度异常");
                                }
                            }

                            scene.m_eStatus = GameSceneStatuses.STATUS_CLEAR;
                        }
                    }
                }
            }

            return;
        }

        public void NotifyTimeStateInfoAndScoreInfo(GameClient client, bool timeState = true, bool otherInfo = true)
        {
            lock (RuntimeData.Mutex)
            {
                LangHunLingYuScene scene = client.SceneObject as LangHunLingYuScene;;
                if (scene != null)
                {
                    if (timeState)
                    {
                        //client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, scene.StateTimeData);
                    }
                    if (otherInfo)
                    {
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_QIZHI_OWNERINFO, scene.QiZhiBuffOwnerDataList);
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_LONGTA_ROLEINFO, scene.LongTaBHRoleCountList);
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_LONGTA_OWNERINFO, scene.LongTaOwnerData);
                    }
                }
            }
        }

        #region 处理王城战的胜负结果

        /// <summary>
        /// 处理王城战的战斗结果
        /// </summary>
        public void ProcessWangChengZhanResult(LangHunLingYuScene scene, bool finish)
        {
            try
            {
                DateTime now = TimeUtil.NowDateTime();
                int remailSecs = (int)((scene.m_lEndTime - TimeUtil.NOW()) / 1000);
                if (!finish) //还在战斗期间
                {
                    if (remailSecs < 0) remailSecs = 0;
                    UpdateQiZhiBuffParams(remailSecs);

                    //这儿其实是在模拟拥有舍利之源的操作，如此就走就代码的逻辑，不用修改太多代码
                    bool ret = TryGenerateNewHuangChengBangHui(scene);

                    //生成了新的占有王城的帮会
                    if (ret)
                    {
                        //处理王城的归属
                        HandleHuangChengResultEx(scene, false);
                    }
                    else
                    {
                        /// 定时给在场的玩家增加经验
                        ProcessTimeAddRoleExp(scene);
                    }
                }
                else
                {
                    //这儿其实是在模拟拥有舍利之源的操作，如此就走就代码的逻辑，不用修改太多代码
                    TryGenerateNewHuangChengBangHui(scene);

                    //处理王城的归属
                    HandleHuangChengResultEx(scene, true);

                    //发放奖励
                    GiveLangHunLingYuAwards(scene);
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// 更新旗帜Buff的时间参数信息
        /// </summary>
        /// <param name="now"></param>
        private void UpdateQiZhiBuffParams(int secs)
        {
            lock (RuntimeData.Mutex)
            {
                foreach (var key in RuntimeData.QiZhiBuffEnableParamsDict.Keys)
                {
                    RuntimeData.QiZhiBuffEnableParamsDict[key][0] = secs;
                }
            }
        }

        /// <summary>
        /// 发放活动奖励
        /// </summary>
        private void GiveLangHunLingYuAwards(LangHunLingYuScene scene)
        {
            LangHunLingYuAwardsData successAwardsData = new LangHunLingYuAwardsData();
            LangHunLingYuAwardsData faildAwardsData = new LangHunLingYuAwardsData();
            successAwardsData.Success = 1;
            successAwardsData.AwardsItemDataList = scene.LevelInfo.Award.Items;
            foreach (var copyMap in scene.CopyMapDict.Values)
            {
                List<GameClient> objList = copyMap.GetClientsList();
                foreach (var client in objList)
                {
                    LangHunLingYuAwardsData awardsData = client.ClientData.Faction == scene.LongTaOwnerData.OwnerBHid ? successAwardsData : faildAwardsData;
                    if (awardsData.AwardsItemDataList != null)
                    {
                        // 判断背包空闲格子是否足够
                        if (Global.CanAddGoodsNum(client, awardsData.AwardsItemDataList.Count))
                        {
                            foreach (var item in awardsData.AwardsItemDataList)
                            {
                                Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, item.GoodsID, item.GoodsNum, 0, "", item.Level, item.Binding, 0,
                                                                "", true, 1, /**/"圣域争霸胜利奖励", Global.ConstGoodsEndTime, 0, 0, item.IsHaveLuckyProp, 0, item.ExcellencePorpValue, item.AppendLev);
                            }
                        }
                        else
                        {
                            Global.UseMailGivePlayerAward2(client, awardsData.AwardsItemDataList, Global.GetLang("圣域争霸胜利奖励"), Global.GetLang("圣域争霸胜利奖励"));
                        }

                        EventLogManager.AddRoleEvent(client, OpTypes.GiveAwards, OpTags.LangHunLingYu, LogRecordType.IntValue2, client.ClientData.Faction, awardsData.Success);
                    }

                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_AWARD, awardsData);
                }
            }
        }

        /// <summary>
        /// <summary>
        /// 新的王城帮会
        /// 2.1若在结束前，王城已有归属，且当前皇宫内成为拥有多个行会的成员或者无人在皇宫中，则王城胜利方属于王城原有归属
        /// 2.2若在结束前，王城无归属，且皇宫内所有成员均为一个行会的成员，则该行会将成为本次王城战的胜利方
        /// 2.3王城战结束时间到后，若之前王城为无归属状态，且皇宫内成员非同一个行会或者无人在皇宫中，则本次王城战流产
        /// </summary>
        /// 尝试产生新帮会[拥有王城所有权的帮会]
        /// </summary>
        /// <returns></returns>
        public bool TryGenerateNewHuangChengBangHui(LangHunLingYuScene scene)
        {
            int newBHid = 0;
            int newBHServerID = 0;
            GetTheOnlyOneBangHui(scene, out newBHid, out newBHServerID);
            lock (RuntimeData.Mutex)
            {
                //剩下的帮会是王城帮会，没有产生新帮会
                if (newBHid <= 0 || newBHid == scene.LongTaOwnerData.OwnerBHid)
                {
                    scene.LastTheOnlyOneBangHui = 0;
                    return false;
                }

                //这次的新帮会和上次不一样，替换,并记录时间
                if (scene.LastTheOnlyOneBangHui != newBHid)
                {
                    scene.LastTheOnlyOneBangHui = newBHid;
                    scene.BangHuiTakeHuangGongTicks = TimeUtil.NOW();

                    //还是没产生
                    return false;
                }

                if (scene.LastTheOnlyOneBangHui > 0)
                {
                    //超过最小时间之后，产生了新帮会，接下来外面的代码需要进行数据库修改
                    long ticks = TimeUtil.NOW();
                    EventLogManager.AddGameEvent(LogRecordType.LangHunLingYuLongTaOnlyBangHuiLog, scene.CityData.CityId, newBHid, ticks - scene.BangHuiTakeHuangGongTicks, "狼魂领域龙塔占领持续时间");
                    if (ticks - scene.BangHuiTakeHuangGongTicks > RuntimeData.MaxTakingHuangGongSecs)
                    {
                        scene.LongTaOwnerData.OwnerBHid = scene.LastTheOnlyOneBangHui;
                        scene.LongTaOwnerData.OwnerBHName = GetBangHuiName(newBHid, out scene.LongTaOwnerData.OwnerBHZoneId); //加载帮会名称等细节信息
                        scene.LongTaOwnerData.OwnerBHServerId = newBHServerID;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 返回剩下的唯一帮会,-1表示有没有唯一帮会
        /// </summary>
        /// <returns></returns>
        public void GetTheOnlyOneBangHui(LangHunLingYuScene scene, out int newBHid, out int newBHServerID)
        {
            newBHid = 0;
            newBHServerID = 0;

            CopyMap copyMap;
            if (!scene.CopyMapDict.TryGetValue(scene.SceneInfo.MapCode_LongTa, out copyMap))
                return;
            
            //龙塔地图中活着的玩家列表
            List<GameClient> lsClients = copyMap.GetClientsList();
            lsClients = Global.GetMapAliveClientsEx(lsClients, scene.SceneInfo.MapCode_LongTa, true);

            lock (RuntimeData.Mutex)
            {
                Dictionary<int, BangHuiRoleCountData> dict = new Dictionary<int, BangHuiRoleCountData>();

                //根据活着的玩家列表，判断王族是否应该产生 保留 还说流产
                for (int n = 0; n < lsClients.Count; n++)
                {
                    GameClient client = lsClients[n];
                    int bhid = client.ClientData.Faction;
                    if (bhid > 0)
                    {
                        BangHuiRoleCountData data;
                        if (!dict.TryGetValue(bhid, out data))
                        {
                            data = new BangHuiRoleCountData() { BHID = bhid, RoleCount = 0, ServerID = client.ServerId };
                            dict.Add(bhid, data);
                        }

                        data.RoleCount++;
                    }
                }

                scene.LongTaBHRoleCountList = dict.Values.ToList();

                if (scene.LongTaBHRoleCountList.Count == 1)
                {
                    newBHid = scene.LongTaBHRoleCountList[0].BHID;
                    newBHServerID = scene.LongTaBHRoleCountList[0].ServerID;
                    EventLogManager.AddGameEvent(LogRecordType.LangHunLingYuLongTaOnlyBangHuiLog, scene.CityData.CityId, newBHid, -1, "狼魂领域龙塔唯一帮会");
                }
            }
        }

        /// <summary>
        /// 通知地图数据变更信息
        /// </summary>
        public void NotifyLongTaRoleDataList(LangHunLingYuScene scene)
        {
            foreach (var copyMap in scene.CopyMapDict.Values)
            {
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_LONGTA_ROLEINFO, scene.LongTaBHRoleCountList, copyMap);
            }
        }

        /// <summary>
        /// 通知地图数据变更信息
        /// </summary>
        public void NotifyLongTaOwnerData(LangHunLingYuScene scene)
        {
            foreach (var copyMap in scene.CopyMapDict.Values)
            {
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_LONGTA_OWNERINFO, scene.LongTaOwnerData, copyMap);
            }
        }

        public void UpdateQiZhiBangHui(LangHunLingYuScene scene, int npcExtentionID, int bhid, string bhName, int zoneId)
        {
            int oldBHID = 0;
            int bufferID = 0;
            lock (RuntimeData.Mutex)
            {
                for (int i = 0; i < scene.QiZhiBuffOwnerDataList.Count; i++)
                {
                    if (scene.QiZhiBuffOwnerDataList[i].NPCID == npcExtentionID)
                    {
                        oldBHID = scene.QiZhiBuffOwnerDataList[i].OwnerBHID;
                        scene.QiZhiBuffOwnerDataList[i].OwnerBHID = bhid;
                        scene.QiZhiBuffOwnerDataList[i].OwnerBHName = bhName;
                        scene.QiZhiBuffOwnerDataList[i].OwnerBHZoneId = zoneId;
                        break;
                    }
                }

                QiZhiConfig qiZhiConfig;
                if (RuntimeData.NPCID2QiZhiConfigDict.TryGetValue(npcExtentionID, out qiZhiConfig))
                {
                    bufferID = qiZhiConfig.BufferID;
                }
            }

            if (bhid == oldBHID)
            {
                return;
            }

            if (npcExtentionID == RuntimeData.SuperQiZhiNpcId)
            {
                scene.SuperQiZhiOwnerBhid = bhid;
            }

            try
            {
                EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(bufferID);
                if (null != item)
                {
                    foreach (var copyMap in scene.CopyMapDict.Values)
                    {
                        List<GameClient> clientList = copyMap.GetClientsList();
                        for (int i = 0; i < clientList.Count; i++)
                        {
                            GameClient c = clientList[i] as GameClient;
                            if (c == null) continue;

                            bool add = false;
                            if (c.ClientData.Faction == oldBHID)
                            {
                                add = false;
                            }
                            else if (c.ClientData.Faction == bhid)
                            {
                                add = true;
                            }

                            UpdateQiZhiBuff4GameClient(c, item, bufferID, add);
                        }
                    }
                }

                NotifyQiZhiBuffOwnerDataList(scene);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException("旗帜状态变化,设置旗帜Buff时发生异常:" + ex.ToString());
            }
        }

        /// <summary>
        /// 更新玩家的军旗Buff
        /// </summary>
        /// <param name="c"></param>
        /// <param name="item"></param>
        /// <param name="bufferID"></param>
        private void UpdateQiZhiBuff4GameClient(GameClient client, EquipPropItem item, int bufferID, bool add)
        {
            try
            {
                if (add && null != item)
                {
                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.BufferByGoodsProps, bufferID, item.ExtProps);
                    Global.UpdateBufferData(client, (BufferItemTypes)bufferID, RuntimeData.QiZhiBuffEnableParamsDict[bufferID], 1, true);
                }
                else
                {
                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.BufferByGoodsProps, bufferID, PropsCacheManager.ConstExtProps);//BufferItemTypes.MU_LangHunLingYu_QIZHI1
                    Global.UpdateBufferData(client, (BufferItemTypes)bufferID, RuntimeData.QiZhiBuffDisableParamsDict[bufferID], 1, true);
                }

                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// 通知地图数据变更信息
        /// </summary>
        public void NotifyQiZhiBuffOwnerDataList(LangHunLingYuScene scene)
        {
            byte[] bytes;
            lock (RuntimeData.Mutex)
            {
                bytes = DataHelper.ObjectToBytes(scene.QiZhiBuffOwnerDataList);
            }

            //通知在线的所有人(不限制地图)领地信息数据通知
            foreach (var copyMap in scene.CopyMapDict.Values)
            {
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_LANGHUNLINGYU_QIZHI_OWNERINFO, scene.QiZhiBuffOwnerDataList, copyMap);
            }
        }

        /// <summary>
        /// 处理王城的归属--->只考虑帮会ID，不考虑具体角色
        /// </summary>
        private void HandleHuangChengResultEx(LangHunLingYuScene scene, bool isBattleOver = false)
        {
            int bhid = scene.LongTaOwnerData.OwnerBHid;
            string bhName = scene.LongTaOwnerData.OwnerBHName;
            if (isBattleOver)
            {
                if (bhid <= 0)
                {
                    //流产的提示
                    string broadCastMsg = StringUtil.substitute(Global.GetLang("很遗憾，本次圣域争霸没有战盟能够占领，请各位勇士再接再厉！"));
                    foreach (var copyMap in scene.CopyMapDict.Values)
                    {
                        GameManager.ClientMgr.BroadSpecialCopyMapMsg(copyMap, broadCastMsg);
                    }
                    return;
                }
            }

            if (scene.LastTheOnlyOneBangHui > 0)
            {
                //夺取龙塔的提示
                string broadCastMsg = "";
                if (!isBattleOver)
                {
                    long nSecond = (scene.m_lEndTime - TimeUtil.NOW()) / 1000;
                    broadCastMsg = StringUtil.substitute(Global.GetLang("『{0}』战盟暂时占领了领域核心，距离圣域争霸结束还有{1}分{2}秒！"), bhName, nSecond / 60, nSecond % 60);
                }
                else
                {
                    broadCastMsg = StringUtil.substitute(Global.GetLang("恭喜战盟【{0}】成功占领领域核心！"), bhName); //
                }

                foreach (var copyMap in scene.CopyMapDict.Values)
                {
                    GameManager.ClientMgr.BroadSpecialCopyMapMsg(copyMap, broadCastMsg);
                }
            }
        }

        #endregion 处理王城战的胜负结果

        #endregion 活动逻辑
     }
}
