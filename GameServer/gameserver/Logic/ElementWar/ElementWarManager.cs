using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Server;
using GameServer.Tools;
using KF.Client;
using KF.Contract.Data;
using Server.Data;
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
    /// 元素试炼管理器
    /// </summary>
    public partial class ElementWarManager : IManager, ICmdProcessorEx, IEventListenerEx, IManager2
    {
        #region 标准接口

        public const SceneUIClasses _sceneType = SceneUIClasses.ElementWar;
        public const GameTypes _gameType = GameTypes.ElementWar;

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public ElementWarData _runtimeData = new ElementWarData();

        private static ElementWarManager instance = new ElementWarManager();
        public static ElementWarManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            if (!InitConfig())
                return false;

            return true;
        }

        public bool initialize(ICoreInterface coreInterface)
        {
            return true;
        }

        public bool startup()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_JOIN, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_QUIT, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_ENTER, 2, 2, getInstance());

            //向事件源注册监听器
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)_sceneType, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuRoleCountChange, (int)_sceneType, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuCopyCanceled, (int)_sceneType, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuNotifyRealEnterGame, (int)_sceneType, getInstance());
            //GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, getInstance());

            return true;
        }

        public bool showdown()
        {
            //向事件源删除监听器
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)_sceneType, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuRoleCountChange, (int)_sceneType, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuCopyCanceled, (int)_sceneType, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuNotifyRealEnterGame, (int)_sceneType, getInstance());
            //GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, getInstance());

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
                case (int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_JOIN:
                    return ProcessJoinCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_QUIT:
                    return ProcessQuitCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_ENTER:
                    return ProcessEnterCmd(client, nID, bytes, cmdParams);
            }
            return true;
        }

        public void processEvent(EventObject eventObject)
        {
            //if (eventObject.getEventType() != (int)EventTypes.MonsterDead)
            //    return;

            //MonsterDeadEventObject deadEv = eventObject as MonsterDeadEventObject;
            //if (deadEv.getAttacker().ClientData.CopyMapID > 0 && deadEv.getAttacker().ClientData.FuBenSeqID > 0
            //    && deadEv.getAttacker().ClientData.MapCode == _runtimeData.MapID
            //    && deadEv.getMonster().CurrentMapCode == _runtimeData.MapID
            //    )
            //{

            //    Monster monster = deadEv.getMonster();
            //    GameClient client = deadEv.getAttacker();

            //    ElementWarScene scene = null;
            //    if (!_sceneDict.TryGetValue(client.ClientData.FuBenSeqID, out scene) || scene == null)
            //        return;

            //    //如果是重复记录击杀同一只怪,则直接返回
            //    if (!scene.AddKilledMonster(monster))
            //        return;

            //    if (scene.SceneStatus >= GameSceneStatuses.STATUS_END)
            //        return;

            //    lock (scene)
            //    {
            //        scene.MonsterCountKill++;

            //        if (scene.IsMonsterFlag == 1 && scene.MonsterCountKill >= scene.MonsterCountCreate)
            //        {
            //            scene.MonsterWaveOld = scene.MonsterWave;
            //            if (scene.MonsterWave >= scene.MonsterWaveTotal)
            //            {
            //                scene.SceneStatus = GameSceneStatuses.STATUS_END;
            //            }
            //            else
            //            {
            //                scene.IsMonsterFlag = 0;

            //                scene.MonsterCountKill = 0;
            //                scene.MonsterCountCreate = 0;
            //            }
            //        }

            //        scene.ScoreData.MonsterCount -= 1;
            //        scene.ScoreData.MonsterCount = scene.ScoreData.MonsterCount < 0 ? 0 : scene.ScoreData.MonsterCount;

            //        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_SCORE_INFO, scene.ScoreData, scene.CopyMapInfo);
            //    }
            //}
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        public void processEvent(EventObjectEx eventObject)
        {
            int eventType = eventObject.EventType;
            switch (eventType)
            {
                case (int)GlobalEventTypes.KuaFuRoleCountChange:
                    {
                        KuaFuFuBenRoleCountEvent e = eventObject as KuaFuFuBenRoleCountEvent;
                        if (null != e)
                        {
                            GameClient client = GameManager.ClientMgr.FindClient(e.RoleId);
                            if (null != client)
                                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_PLAYER_NUM, e.RoleCount);

                            eventObject.Handled = true;
                        }
                    }
                    break;
                case (int)GlobalEventTypes.KuaFuNotifyEnterGame:
                    {
                        KuaFuNotifyEnterGameEvent e = eventObject as KuaFuNotifyEnterGameEvent;
                        if (null != e)
                        {
                            KuaFuServerLoginData kuaFuServerLoginData = e.Arg as KuaFuServerLoginData;
                            if (null != kuaFuServerLoginData)
                            {
                                GameClient client = GameManager.ClientMgr.FindClient(kuaFuServerLoginData.RoleId);
                                if (null != client)
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
                                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_ENTER, string.Format("{0}:{1}", kuaFuServerLoginData.GameId, e.TeamCombatAvg));
                                        //ProcessEnterCmd(client, (int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_ENTER, null, new string[] { "", "1" });
                                    }
                                }
                            }

                            eventObject.Handled = true;
                        }
                    }
                    break;
                case (int)GlobalEventTypes.KuaFuCopyCanceled:
                    {
                        KuaFuNotifyCopyCancelEvent e = eventObject as KuaFuNotifyCopyCancelEvent;
                        GameClient client = GameManager.ClientMgr.FindClient(e.RoleId);
                        if (client != null)
                        {
                            client.ClientData.SignUpGameType = (int)GameTypes.None;
                          //  ElementWarClient.getInstance().RoleChangeState(e.RoleId, KuaFuRoleStates.None);

                            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_CANCEL, string.Format("{0}:{1}", e.GameId, e.Reason)); 
                        }

                        eventObject.Handled = true;
                    }
                    break;
                case (int)GlobalEventTypes.KuaFuNotifyRealEnterGame:
                    {
                        KuaFuNotifyRealEnterGameEvent e = eventObject as KuaFuNotifyRealEnterGameEvent;
                        if (e != null)
                        {
                            GameClient client = GameManager.ClientMgr.FindClient(e.RoleId);
                            if (client != null)
                            {
                                client.ClientData.SignUpGameType = (int)GameTypes.None;
                                GlobalNew.RecordSwitchKuaFuServerLog(client);
                                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client));                               
                            }
                        }

                        eventObject.Handled = true;
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
            string fileName = "";
            XElement xml = null;
            IEnumerable<XElement> nodes;

            lock (_runtimeData.Mutex)
            {
                try
                {
                    //怪物批次配置--------------------------------------------------------------------
                    _runtimeData.MonsterOrderConfigList.Clear();

                    fileName = Global.GameResPath("Config/YuanSuShiLian.xml");
                    xml = CheckHelper.LoadXml(fileName);
                    if (null == xml) return false;
                    nodes = xml.Elements();
                    foreach (var xmlItem in nodes)
                    {
                        if (xmlItem == null) continue;

                        ElementWarMonsterConfigInfo config = new ElementWarMonsterConfigInfo();
                        config.OrderID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                        config.MonsterCount = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Num", "0"));

                        string[] ids = Global.GetDefAttributeStr(xmlItem, "MonstersID", "0,0,0").Split('|');
                        config.MonsterIDs = new List<int>();
                        foreach (string id in ids)
                            config.MonsterIDs.Add(int.Parse(id));

                        config.Up1 = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "UpOne", "0"));
                        config.Up2 = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "UpTwo", "0"));
                        config.Up3 = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "UpThree", "0"));
                        config.Up4 = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "UpFour", "0"));

                        config.X = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "X", "0"));
                        config.Y = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Y", "0"));
                        config.Radius = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Radius", "0"));

                        _runtimeData.MonsterOrderConfigList.Add(config.OrderID, config);
                    }

                    //奖励配置--------------------------------------------------------------------
                    _runtimeData.AwardLight = GameManager.systemParamsList.GetParamValueIntArrayByName("YuanSuShiLianAward");
                }
                catch (System.Exception ex)
                {
                    success = false;
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("加载xml配置文件:{0}, 失败。", fileName), ex);
                }
            }

            return success;
        }

        #endregion 初始化配置

        #region 指令处理

        /// <summary>
        /// 开始匹配
        /// </summary>
        public bool ProcessJoinCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                SceneUIClasses sceneType = Global.GetMapSceneType(client.ClientData.MapCode);
                if (sceneType != SceneUIClasses.Normal)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Denied_In_Current_Map.ToString());
                    return true;
                }

                if (!IsGongNengOpened(client, true))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Not_In_valid_Time.ToString());
                    return true;
                }

                if (client.ClientData.SignUpGameType != (int)GameTypes.None)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Denied_In_Activity_Time.ToString());
                    return true;
                }

                if (KuaFuManager.getInstance().IsInCannotJoinKuaFuCopyTime(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Time_Punish.ToString());
                    return true;
                }

                SystemXmlItem systemFuBenItem = null;
                if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(_runtimeData.CopyID, out systemFuBenItem))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Config_Fault.ToString());
                    return true;
                }

                int minLevel = systemFuBenItem.GetIntValue("MinLevel");
                int minZhuanSheng = systemFuBenItem.GetIntValue("MinZhuanSheng");
                int levelLimit = minZhuanSheng * 100 + minLevel;

                // 先判断等级
                if (client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level < levelLimit)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Level_Limit.ToString());
                    return true;
                }

                // 判断剩余次数
                int oldCount = GetElementWarCount(client);
                if (oldCount >= systemFuBenItem.GetIntValue("FinishNumber"))
                {
                    client.sendCmd(nID, StdErrorCode.Error_No_Residue_Degree.ToString());
                    return true;
                }

                int result = 0;// ElementWarClient.getInstance().SignUp(client.strUserID, client.ClientData.RoleID, client.ClientData.ZoneID, (int)_gameType, client.ClientData.CombatForce);
                if (result > 0)
                {
                    client.ClientData.SignUpGameType = (int)_gameType;
                    // 报名统计
                    GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 1, 0, 0, 0, (int)_gameType);
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

        /// <summary>
        /// 退出匹配
        /// </summary>
        public bool ProcessQuitCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Success_No_Info.ToString());
                    return true;
                }
  
                client.ClientData.SignUpGameType = (int)GameTypes.None;
                int result = 0;// ElementWarClient.getInstance().RoleChangeState(client.ClientData.RoleID, KuaFuRoleStates.None);

                client.sendCmd(nID, result);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        /// <summary>
        /// 匹配成功，立即开始或暂不进入
        /// </summary>
        public bool ProcessEnterCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Success_No_Info.ToString());
                    return true;
                }
    
                int result = StdErrorCode.Error_Success;
                client.ClientData.SignUpGameType = (int)GameTypes.None;

                int flag = Global.SafeConvertToInt32(cmdParams[1]);
                if (flag > 0)
                {
                    result = 0;// ElementWarClient.getInstance().RoleChangeState(client.ClientData.RoleID, KuaFuRoleStates.EnterGame);
                    if (result < 0) flag = 0;
                }
                else
                {
                   // ElementWarClient.getInstance().RoleChangeState(client.ClientData.RoleID, KuaFuRoleStates.None);
                    
                    //惩罚
                    KuaFuManager.getInstance().SetCannotJoinKuaFu_UseAutoEndTicks(client);
                }

                if (flag <= 0)
                {
                    Global.GetClientKuaFuServerLoginData(client).RoleId = 0;
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ELEMENT_WAR_QUIT, StdErrorCode.Error_Success_No_Info.ToString());
                }

                //client.sendCmd(nID, result);
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

        public bool OnInitGame(GameClient client)
        {
            GameMap gameMap = null;
            if (GameManager.MapMgr.DictMaps.TryGetValue(_runtimeData.MapID, out gameMap)) //确认地图编号是否有效
            {
                int defaultBirthPosX = GameManager.MapMgr.DictMaps[_runtimeData.MapID].DefaultBirthPosX;
                int defaultBirthPosY = GameManager.MapMgr.DictMaps[_runtimeData.MapID].DefaultBirthPosY;
                int defaultBirthRadius = GameManager.MapMgr.DictMaps[_runtimeData.MapID].BirthRadius;

                //从配置根据地图取默认位置
                Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, _runtimeData.MapID, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
                client.ClientData.MapCode = _runtimeData.MapID;
                client.ClientData.PosX = (int)newPos.X;
                client.ClientData.PosY = (int)newPos.Y;
                client.ClientData.FuBenSeqID = Global.GetClientKuaFuServerLoginData(client).FuBenSeqId;

                return true;
            }

            return false;
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

        /// <summary>
        /// 判断功能是否开启
        /// </summary>
        public bool IsGongNengOpened(GameClient client, bool hint = false)
        {
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.ElementWar))
            {
                return false;
            }

            return GlobalNew.IsGongNengOpened(client, GongNengIDs.ElementWar, hint);
        }

        public int GetElementWarCount(GameClient client)
        {
            int day = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementWarDayId);
            int count = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementWarCount);

            int today = Global.GetOffsetDayNow();
            if (today == day)
                return count;
            else
            {
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ElementWarDayId, today, true);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ElementWarCount, 0, true);
            }

            return 0;
        }

        public void AddElementWarCount(GameClient client)
        {
            int count = GetElementWarCount(client);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ElementWarCount, count + 1, true);
        }

        #endregion 其他

    }
}
