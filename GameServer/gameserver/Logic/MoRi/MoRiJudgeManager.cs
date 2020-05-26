using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Server.Tools.Pattern;
using GameServer.Server;
using Tmsk.Contract;
using GameServer.Core.Executor;
using KF.Client;
using KF.Contract.Data;
using Server.Tools;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using System.Windows;
using Server.Data;

namespace GameServer.Logic.MoRi
{
    public class MoRiJudgeManager : SingletonTemplate<MoRiJudgeManager>, IManager, ICmdProcessorEx, IManager2, IEventListenerEx, IEventListener
    {
        private MoRiJudgeManager() { }

        private List<MoRiMonster> BossConfigList = new List<MoRiMonster>();

        private Dictionary<int, MoRiJudgeCopy> copyDict = new Dictionary<int, MoRiJudgeCopy>();
        private long NextHeartBeatMs = 0L;
        private int copyMapGirdWidth;
        private int copyMapGirdHeight;
        private int CopyMaxAliveMinutes = 15;
        public double[] AwardFactor { get; set; }
        public int MapCode { get; set; }

        #region Implement IManager
        public bool initialize()
        {
            if (!InitConfig())
            {
                return false;
            }

            return true;
        }

        private bool InitConfig()
        {
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(MoRiJudgeConsts.MonsterConfigFile));
                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var item in xmlItems)
                {
                    MoRiMonster monster = new MoRiMonster();
                    monster.Id = (int)Global.GetSafeAttributeLong(item, "ID");
                    monster.Name = Global.GetSafeAttributeStr(item, "Name");
                    monster.MonsterId = (int)Global.GetSafeAttributeLong(item, "MonstersID");
                    monster.BirthX = (int)Global.GetSafeAttributeLong(item, "X");
                    monster.BirthY = (int)Global.GetSafeAttributeLong(item, "Y");
                    monster.KillLimitSecond = (int)Global.GetSafeAttributeLong(item, "Time");

                    string addBossProps = Global.GetSafeAttributeStr(item, "Props");
                    if (!string.IsNullOrEmpty(addBossProps) && addBossProps != "-1")
                    {
                        foreach (var prop in addBossProps.Split('|'))
                        {
                            string[] prop_kv = prop.Split(',');
                            if (prop_kv != null && prop_kv.Length == 2)
                            {
                                monster.ExtPropDict.Add(
                                    (int)Enum.Parse(typeof(ExtPropIndexes), prop_kv[0]),
                                    (float)Convert.ToDouble(prop_kv[1])
                                    );
                            }
                        }
                    }

                    // parse prop
                    BossConfigList.Add(monster);
                }

                BossConfigList.Sort((left, right) =>
                {
                    if (left.Id < right.Id) return -1;
                    else if (left.Id > right.Id) return 1;
                    else return 0;
                });

              

                SystemXmlItem systemFuBenItem = null;
                if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(MoRiJudgeConsts.CopyId, out systemFuBenItem))
                {
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("缺少末日审判副本配置 CopyID={0}", MoRiJudgeConsts.CopyId));
                    return false;
                }

                this.MapCode = systemFuBenItem.GetIntValue("MapCode");

                var fubenItem = FuBenManager.FindMapCodeByFuBenID(MoRiJudgeConsts.CopyId, this.MapCode);
                if (fubenItem == null)
                {
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("末日审判地图 {0} 配置错误", this.MapCode));
                    return false;
                }
                this.CopyMaxAliveMinutes = fubenItem.MaxTime;

                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(this.MapCode, out gameMap))
                {
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("缺少末日审判地图 {0}", this.MapCode));
                    return false;
                }

                copyMapGirdWidth = gameMap.MapGridWidth;
                copyMapGirdHeight = gameMap.MapGridHeight;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载xml配置文件:{0}, 失败。", MoRiJudgeConsts.MonsterConfigFile), ex);
                return false;
            }

            return true;
        }

        public bool initialize(ICoreInterface coreInterface)
        {
            return true;
        }

        public bool startup()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_MORI_JOIN, 1, 1, MoRiJudgeManager.Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_MORI_QUIT, 1, 1, MoRiJudgeManager.Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_MORI_NTF_ENTER, 2, 2, MoRiJudgeManager.Instance());

            //向事件源注册监听器
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuRoleCountChange, (int)SceneUIClasses.MoRiJudge, MoRiJudgeManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.MoRiJudge, MoRiJudgeManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuCopyCanceled, (int)SceneUIClasses.MoRiJudge, MoRiJudgeManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KuaFuNotifyRealEnterGame, (int)SceneUIClasses.MoRiJudge, MoRiJudgeManager.Instance());
            
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, MoRiJudgeManager.Instance());
            return true;
        }

        public bool showdown()
        {
            //向事件源删除监听器
            
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuRoleCountChange, (int)SceneUIClasses.MoRiJudge, MoRiJudgeManager.Instance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuNotifyEnterGame, (int)SceneUIClasses.MoRiJudge, MoRiJudgeManager.Instance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuCopyCanceled, (int)SceneUIClasses.MoRiJudge, MoRiJudgeManager.Instance());
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KuaFuNotifyRealEnterGame, (int)SceneUIClasses.MoRiJudge, MoRiJudgeManager.Instance());
            
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, MoRiJudgeManager.Instance());

            return true;
        }

        public bool destroy()
        {
            return true;
        }
        #endregion

        #region Implement IEventListener
        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() == (int)EventTypes.MonsterDead)
            {
                MonsterDeadEventObject deadEv = eventObject as MonsterDeadEventObject;
                if (deadEv.getAttacker().ClientData.CopyMapID > 0 && deadEv.getAttacker().ClientData.FuBenSeqID > 0
                    && deadEv.getAttacker().ClientData.MapCode == this.MapCode
                    && deadEv.getMonster().CurrentMapCode == this.MapCode
                    )
                {
                    MoRiMonsterTag tag = deadEv.getMonster().Tag as MoRiMonsterTag;
                    if (tag == null) return;

                    MoRiJudgeCopy judgeCopy = null;
                    lock (copyDict)
                    {
                        if (!copyDict.TryGetValue(tag.CopySeqId, out judgeCopy))
                        {
                            return;
                        }
                    }

                    long killMs = 0;
                    lock (judgeCopy)
                    {
                        // 只有在战斗阶段的杀怪才有效
                        if (judgeCopy.m_eStatus != GameSceneStatuses.STATUS_BEGIN) return;

                        // 只有一条命
                        if (judgeCopy.MonsterList[tag.MonsterIdx].DeathMs > 0) return;

                        judgeCopy.MonsterList[tag.MonsterIdx].DeathMs = TimeUtil.NOW();


                        // 通知怪物死亡事件
                        GameManager.ClientMgr.BroadSpecialCopyMapMessageStr(
                            (int)TCPGameServerCmds.CMD_NTF_MORI_MONSTER_EVENT,
                            string.Format("{0}:{1}:{2}:{3}", (int)MoRiMonsterEvent.Death, BossConfigList[tag.MonsterIdx].Id, judgeCopy.MonsterList[tag.MonsterIdx].BirthMs, judgeCopy.MonsterList[tag.MonsterIdx].DeathMs),
                            judgeCopy.MyCopyMap);

                        CalcAwardRate(judgeCopy);

                        /*
                        FuBenInfoItem fbItem = FuBenManager.FindFuBenInfoBySeqID(judgeCopy.MyCopyMap.FuBenSeqID);
                        if (fbItem != null)
                        {
                            fbItem.AwardRate += CalcAwardRate(judgeCopy);
                        }*/

                        if (judgeCopy.MonsterList.Count == BossConfigList.Count)
                        {
                            judgeCopy.Passed = true;
                            judgeCopy.m_eStatus = GameSceneStatuses.STATUS_END;
                        }
                    }
                }
            }
        }
        #endregion

        #region Implement  IEventListenerEx
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
                            {
                                client.sendCmd((int)TCPGameServerCmds.CMD_MORI_NTF_ROLE_COUNT, e.RoleCount.ToString());
                            }

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

                                        client.sendCmd((int)TCPGameServerCmds.CMD_MORI_NTF_ENTER, string.Format("{0}:{1}", kuaFuServerLoginData.GameId, e.TeamCombatAvg));
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
                            client.sendCmd((int)TCPGameServerCmds.CMD_NTF_MORI_COPY_CANCEL, string.Format("{0}:{1}", e.GameId, e.Reason));
                            client.ClientData.SignUpGameType = (int)GameTypes.None;
                        }
                     //   MoRiJudgeClient.getInstance().ChangeRoleState(e.RoleId, KuaFuRoleStates.None);

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
                                GlobalNew.RecordSwitchKuaFuServerLog(client);
                                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client));
                                client.ClientData.SignUpGameType = (int)GameTypes.None;
                            }
                        }

                        eventObject.Handled = true;
                    }
                    break;
            }
        }
        #endregion

        #region Implement ICmdProcessorEx
        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }
        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_MORI_JOIN:
                    return ProcessMoRiJudgeJoin(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_MORI_QUIT:
                    return ProcessMoRiJudgeQuit(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_MORI_NTF_ENTER:
                    return ProcessMoRiJudgeEnter(client, nID, bytes, cmdParams);
            }

            return true;
        }
        #endregion

        #region Process Client Request
        // 报名末日审判
        private bool ProcessMoRiJudgeJoin(GameClient client, int nID, byte[] bytes, string[] cmdParams)
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
                if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(MoRiJudgeConsts.CopyId, out systemFuBenItem))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Config_Fault.ToString());
                    return true;
                }

                int minLevel = systemFuBenItem.GetIntValue("MinLevel");
                int maxLevel = systemFuBenItem.GetIntValue("MaxLevel");
                int nMinZhuanSheng  = systemFuBenItem.GetIntValue("MinZhuanSheng");
                int nMaxZhuanSheng  = systemFuBenItem.GetIntValue("MaxZhuanSheng");

                // 先判断等级
                if (client.ClientData.ChangeLifeCount < nMinZhuanSheng ||
                    (client.ClientData.ChangeLifeCount == nMinZhuanSheng && client.ClientData.Level < minLevel))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Level_Limit.ToString());
                    return true;
                }

                if (client.ClientData.ChangeLifeCount > nMaxZhuanSheng ||
                     (client.ClientData.ChangeLifeCount == nMaxZhuanSheng && client.ClientData.Level > maxLevel))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Level_Limit.ToString());
                    return true;
                }

                // 判断剩余次数
                FuBenData fuBenData = Global.GetFuBenData(client, MoRiJudgeConsts.CopyId);
                if (fuBenData != null && fuBenData.FinishNum >= systemFuBenItem.GetIntValue("FinishNumber"))
                {
                    client.sendCmd(nID, StdErrorCode.Error_No_Residue_Degree.ToString());
                    return true;
                }

                int result = 0;// MoRiJudgeClient.getInstance().MoRiJudgeSignUp(client.strUserID, client.ClientData.RoleID, client.ClientData.ZoneID, client.ClientData.CombatForce);
                if (result == (int)KuaFuRoleStates.SignUp)
                {
                    // 报名成功
                    client.ClientData.SignUpGameType = (int)GameTypes.MoRiJudge;

                    // 报名统计
                    GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 1, 0, 0, 0, (int)GameTypes.MoRiJudge);
                }

                //发送结果给客户端
                client.sendCmd(nID, result.ToString());
                return true;
            }
           
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }
        // 取消报名
        private bool ProcessMoRiJudgeQuit(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Success_No_Info.ToString());
                    return true;
                }

                int result = StdErrorCode.Error_Success;

                if (result >= 0)
                {
                    result = 0;// MoRiJudgeClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.None);
                    client.ClientData.SignUpGameType = (int)GameTypes.None;
                }

                client.sendCmd(nID, result.ToString());
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }
        // 客户端发来是否确定进入
        private bool ProcessMoRiJudgeEnter(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                {
                    client.sendCmd(nID, StdErrorCode.Error_Not_In_valid_Time.ToString() + ":0");
                    return true;
                }

                int result = StdErrorCode.Error_Success;
                int flag = Global.SafeConvertToInt32(cmdParams[1]);
                if (flag > 0)
                {
                  //  result = MoRiJudgeClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.EnterGame);
                    if (result < 0)
                    {
                        flag = 0;
                    }
                }
                else
                {
                    result = 0;// MoRiJudgeClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.None);
                    client.ClientData.SignUpGameType = (int)GameTypes.None;
                    // 增加惩罚时间
                    KuaFuManager.getInstance().SetCannotJoinKuaFu_UseAutoEndTicks(client);
                }

                if (flag <= 0)
                {
                    Global.GetClientKuaFuServerLoginData(client).RoleId = 0;
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_MORI_QUIT, StdErrorCode.Error_Success_No_Info);
                }

                // 进入就不发送消息了
                //client.sendCmd(nID, result.ToString() + ":0");
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }
        #endregion

        #region Util Function

        private double CalcAwardRate(MoRiJudgeCopy judgeCopy)
        {
            double result = 1.0;

            judgeCopy.LimitKillCount = 0;
            for (int i = 0; i < judgeCopy.MonsterList.Count; ++i)
            {
                if (judgeCopy.MonsterList[i].DeathMs > 0
                    && BossConfigList[i].KillLimitSecond > 0
                    && judgeCopy.MonsterList[i].DeathMs - judgeCopy.MonsterList[i].BirthMs <= BossConfigList[i].KillLimitSecond * 1000)
                {
                    ++judgeCopy.LimitKillCount;
                }
            }

            if (AwardFactor != null && judgeCopy.LimitKillCount - 1 >= 0 && judgeCopy.LimitKillCount - 1 < AwardFactor.Length)
            {
                result = AwardFactor[judgeCopy.LimitKillCount - 1];
            }

            return result;
        }

        private bool IsGongNengOpened(GameClient client, bool bHint = true)
        {
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.MoRiJudge))
            {
                return false;
            }

            return GlobalNew.IsGongNengOpened(client, GongNengIDs.MoRiJudge, bHint);
        }
        #endregion

        public bool OnInitGame(GameClient client)
        {
            int destX, destY;
            if (!GetBirthPoint(out destX, out destY))
            {
                return false;
            }

            client.ClientData.MapCode = this.MapCode;
            client.ClientData.PosX = (int)destX;
            client.ClientData.PosY = (int)destY;
            client.ClientData.FuBenSeqID = Global.GetClientKuaFuServerLoginData(client).FuBenSeqId;

            return true;
        }

        public void OnLogOut(GameClient client)
        {
           // MoRiJudgeClient.getInstance().GameFuBenRoleChangeState(client.ClientData.RoleID, (int)KuaFuRoleStates.None);

            MoRiJudgeCopy judgeCopy = null;
            lock (copyDict)
            {
                if (!copyDict.TryGetValue(client.ClientData.FuBenSeqID, out judgeCopy))
                {
                    return;
                }
            }

            lock (judgeCopy)
            {
                judgeCopy.RoleCount--;
                if (judgeCopy.m_eStatus != GameSceneStatuses.STATUS_END
                    && judgeCopy.m_eStatus != GameSceneStatuses.STATUS_AWARD
                    && judgeCopy.m_eStatus != GameSceneStatuses.STATUS_CLEAR)
                {
                    KuaFuManager.getInstance().SetCannotJoinKuaFu_UseAutoEndTicks(client);
                }
            }
        }

        // 添加一个末日审判副本
        public void AddCopyScene(GameClient client, CopyMap copyMap, SceneUIClasses sceneType)
        {
            if (copyMap.MapCode == this.MapCode)
            {
                int fuBenSeqId = copyMap.FuBenSeqID;
                int mapCode = copyMap.MapCode;

                lock (copyDict)
                {
                    MoRiJudgeCopy copy = null;
                    if (!copyDict.TryGetValue(fuBenSeqId, out copy))
                    {
                        copy = new MoRiJudgeCopy();
                        copy.MyCopyMap = copyMap;
                        copy.GameId = Global.GetClientKuaFuServerLoginData(client).GameId;
                        copy.StateTimeData.GameType = (int)GameTypes.MoRiJudge;

                        // 副本的统计信息
                        copy.StartTime = TimeUtil.NowDateTime();
                        copy.EndTime = copy.StartTime.AddMinutes(this.CopyMaxAliveMinutes);
                        copy.LimitKillCount = 0;
                        copy.RoleCount = 1;
                        copy.Passed = false;

                        copyDict[fuBenSeqId] = copy;
                    }
                    else
                    {
                        copy.RoleCount++;
                    }
                }

                FuBenManager.AddFuBenSeqID(client.ClientData.RoleID, copyMap.FuBenSeqID, 0, copyMap.FubenMapID);

                copyMap.IsKuaFuCopy = true;
                // 增加清除时间
                copyMap.SetRemoveTicks(TimeUtil.NOW() + (this.CopyMaxAliveMinutes + 3) * TimeUtil.MINITE);

                //更新状态
              //  MoRiJudgeClient.getInstance().GameFuBenRoleChangeState(client.ClientData.RoleID, (int)KuaFuRoleStates.StartGame);
                // 开始游戏统计
                GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 1, 0, 0, (int)GameTypes.MoRiJudge);
            }
        }

        // 清理副本的线程删除该副本，直接设为结束，由timer定时处理
        // 可能是超时了，也可能是没人超过了一定时间
        public void DelCopyScene(CopyMap copyMap)
        {
            if (copyMap != null && copyMap.MapCode == this.MapCode)
            {
                MoRiJudgeCopy judgeCopy = null;
                lock (copyDict)
                {
                    if (!copyDict.TryGetValue(copyMap.FuBenSeqID, out judgeCopy))
                    {
                        return;
                    }
                    //copyDict.Remove(copyMap.FuBenSeqID);
                }

                lock (judgeCopy)
                {
                    // 结束吧, 防止award状态回滚
                    if (judgeCopy.m_eStatus < GameSceneStatuses.STATUS_END)
                    {
                        judgeCopy.m_eStatus = GameSceneStatuses.STATUS_END;
                    }
                }
            }
        }

        // 定时处理副本逻辑
        public void TimerProc()
        {
            long nowMs = TimeUtil.NOW();
            if (nowMs < NextHeartBeatMs)
            {
                return;
            }

            NextHeartBeatMs = nowMs + 1020; //1020毫秒执行一次

            List<MoRiJudgeCopy> copyList = null;
            lock (copyDict)
            {
                copyList = copyDict.Values.ToList();
            }
            if (copyList == null || copyList.Count <= 0)
            {
                return;
            }

            foreach (var judgeCopy in copyList)
            {
                lock (judgeCopy)
                {
                    if (judgeCopy.m_eStatus == GameSceneStatuses.STATUS_NULL)
                    {
                        judgeCopy.m_eStatus = GameSceneStatuses.STATUS_PREPARE;
                        judgeCopy.CurrStateBeginMs = nowMs;
                        judgeCopy.DeadlineMs = nowMs + this.CopyMaxAliveMinutes * 60 * 1000;

                        // 末日审判副本进入之后就是等待结束
                        judgeCopy.StateTimeData.State = (int)GameSceneStatuses.STATUS_BEGIN;
                        judgeCopy.StateTimeData.EndTicks = judgeCopy.DeadlineMs;
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, judgeCopy.StateTimeData, judgeCopy.MyCopyMap);
                    }
                    else if (judgeCopy.m_eStatus == GameSceneStatuses.STATUS_PREPARE)
                    {
                        if (nowMs >= judgeCopy.CurrStateBeginMs + 1500)
                        {
                            judgeCopy.m_eStatus = GameSceneStatuses.STATUS_BEGIN;
                            judgeCopy.CurrStateBeginMs = nowMs;
                        }
                    }
                    else if (judgeCopy.m_eStatus == GameSceneStatuses.STATUS_BEGIN)
                    {
                        // 如果超时了
                        if (nowMs >= judgeCopy.DeadlineMs
                            || (nowMs >= judgeCopy.CurrStateBeginMs + 90 * 1000 && judgeCopy.RoleCount <= 0)
                            )
                        {
                            judgeCopy.m_eStatus = GameSceneStatuses.STATUS_END;
                            judgeCopy.CurrStateBeginMs = nowMs;
                            return;
                        }

                        int nextMonsterIdx = -1;
                        if (judgeCopy.CurrMonsterIdx == -1)
                        {
                            nextMonsterIdx = 0;
                        }
                        else// if (judgeCopy.CurrMonsterIndex < )
                        {
                            if (judgeCopy.MonsterList[judgeCopy.CurrMonsterIdx].DeathMs > 0
                                && nowMs >= judgeCopy.MonsterList[judgeCopy.CurrMonsterIdx].DeathMs + MoRiJudgeConsts.MonsterFlushIntervalMs)
                            {
                                nextMonsterIdx = judgeCopy.CurrMonsterIdx + 1;
                            }
                        }

                        if (nextMonsterIdx != -1)
                        {
                            if (nextMonsterIdx >= BossConfigList.Count)
                            {
                                judgeCopy.m_eStatus = GameSceneStatuses.STATUS_END;
                                judgeCopy.CurrStateBeginMs = nowMs;
                            }
                            else
                            {
                                FlushMonster(judgeCopy, nextMonsterIdx);
                            }
                        }
                    }
                    else if (judgeCopy.m_eStatus == GameSceneStatuses.STATUS_END)
                    {
                        // 把所有怪杀死，主要是副本到时间的情况
                        GameManager.CopyMapMgr.KillAllMonster(judgeCopy.MyCopyMap);

                      //  MoRiJudgeClient.getInstance().GameFuBenChangeState(judgeCopy.GameId, GameFuBenState.End, DateTime.Now);
                        // 记录副本通关信息
                        judgeCopy.EndTime = TimeUtil.NowDateTime();
                        int combatAvg = 0;
                        int roleCount = 0;

                        List<GameClient> clientList = judgeCopy.MyCopyMap.GetClientsList();
                        if (clientList != null && clientList.Count > 0)
                        {
                            int combatSum = 0;

                            foreach (var client in clientList)
                            {
                                ++roleCount;
                                combatSum += client.ClientData.CombatForce;

                                if (judgeCopy.Passed)
                                {
                                    // 成功统计
                                    GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 0, 1, 0, (int)GameTypes.MoRiJudge);
                                }
                                else
                                {
                                    // 失败统计
                                    GlobalNew.UpdateKuaFuRoleDayLogData(client.ServerId, client.ClientData.RoleID, TimeUtil.NowDateTime(), client.ClientData.ZoneID, 0, 0, 0, 1, (int)GameTypes.MoRiJudge);
                                }
                            }
                            if (roleCount > 0)
                            {
                                combatAvg = combatSum / roleCount;
                            }

                            if (judgeCopy.Passed)
                            {
                                GameManager.CopyMapMgr.CopyMapPassAwardForAll(clientList[0], judgeCopy.MyCopyMap, false);
                            }                      
                        }

                      //  MoRiJudgeClient.getInstance().UpdateCopyPassEvent(judgeCopy.GameId, judgeCopy.Passed, judgeCopy.StartTime, judgeCopy.EndTime, judgeCopy.LimitKillCount, roleCount, combatAvg);

                        judgeCopy.m_eStatus = GameSceneStatuses.STATUS_AWARD;
                        judgeCopy.CurrStateBeginMs = nowMs;

                        judgeCopy.StateTimeData.State = (int)GameSceneStatuses.STATUS_END;
                        judgeCopy.StateTimeData.EndTicks = nowMs + 30 * 1000;
                        GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, judgeCopy.StateTimeData, judgeCopy.MyCopyMap);
                    }
                    else if (judgeCopy.m_eStatus == GameSceneStatuses.STATUS_AWARD)
                    {
                        // 暂时设置为30S的清理时间
                        // 副本到时间的话，客户端会主动关闭
                        if (nowMs >= judgeCopy.CurrStateBeginMs + 30 * 1000)
                        {
                            lock (copyDict)
                            {
                                copyDict.Remove(judgeCopy.MyCopyMap.FuBenSeqID);
                            }

                            try
                            {
                                List<GameClient> clientList = judgeCopy.MyCopyMap.GetClientsList();
                                if (clientList != null)
                                {
                                    foreach (var client in clientList)
                                    {
                                        KuaFuManager.getInstance().GotoLastMap(client);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                DataHelper.WriteExceptionLogEx(ex, "末日审判清场调度异常");
                            }
                        }
                    }
                }
            }
        }

        private void FlushMonster(MoRiJudgeCopy judgeCopy, int nextMonsterIdx)
        {
            MoRiMonsterTag tag = new MoRiMonsterTag();
            tag.CopySeqId = judgeCopy.MyCopyMap.FuBenSeqID;
            tag.MonsterIdx = nextMonsterIdx;
            tag.ExtPropDict = null;

            // 刷怪
            if (nextMonsterIdx == BossConfigList.Count - 1)
            {
                tag.ExtPropDict = new Dictionary<int, float>();
                // 刷boss
                for (int i = 0; i < judgeCopy.MonsterList.Count && i < judgeCopy.CurrMonsterIdx; ++i)
                {
                    if (BossConfigList[i].KillLimitSecond != -1
                        && (judgeCopy.MonsterList[i].DeathMs - judgeCopy.MonsterList[i].BirthMs) <= BossConfigList[i].KillLimitSecond * 1000L)
                    {
                        // 完成了限时击杀，需要给boss加成
                        foreach (var kvp in BossConfigList[i].ExtPropDict)
                        {
                            if (tag.ExtPropDict.ContainsKey(kvp.Key)) tag.ExtPropDict[kvp.Key] += kvp.Value;
                            else tag.ExtPropDict.Add(kvp.Key, kvp.Value);
                        }
                    }
                }
            }

            GameManager.MonsterZoneMgr.AddDynamicMonsters(this.MapCode, BossConfigList[nextMonsterIdx].MonsterId,
                judgeCopy.MyCopyMap.CopyMapID, 1,
                BossConfigList[nextMonsterIdx].BirthX / copyMapGirdWidth, BossConfigList[nextMonsterIdx].BirthY / copyMapGirdHeight, 0, 0,
                SceneUIClasses.MoRiJudge, tag);

            judgeCopy.MonsterList.Add(new MoRiMonsterData()
            {
                Id = BossConfigList[nextMonsterIdx].Id,
                BirthMs = TimeUtil.NOW(),
                DeathMs = -1
            });

            judgeCopy.CurrMonsterIdx = nextMonsterIdx;
        }

        public void OnLoadDynamicMonsters(Monster monster)
        {
            MoRiMonsterTag tag  = null;
            if (monster == null || (tag = monster.Tag as MoRiMonsterTag) == null)
            {
                return;
            }

            MoRiJudgeCopy judgeCopy = null;
            lock (copyDict)
            {
                if (!copyDict.TryGetValue(tag.CopySeqId, out judgeCopy))
                {
                    return;
                }
            }

            // 通知怪物出生
            GameManager.ClientMgr.BroadSpecialCopyMapMessageStr((int)TCPGameServerCmds.CMD_NTF_MORI_MONSTER_EVENT,
                string.Format("{0}:{1}:{2}:{3}", (int)MoRiMonsterEvent.Birth, BossConfigList[tag.MonsterIdx].Id, judgeCopy.MonsterList[tag.MonsterIdx].BirthMs, judgeCopy.MonsterList[tag.MonsterIdx].DeathMs), judgeCopy.MyCopyMap);


            // 增强临时属性，副本时间只有15分钟，只有就暂时加1个小时吧，因为是永久加成的
            long toTick = TimeUtil.NowDateTime().Ticks + 3600L * 1000 * 10000;
            if (tag.ExtPropDict != null)
            {
                foreach (var kvp in tag.ExtPropDict)
                {
                    monster.TempPropsBuffer.AddTempExtProp(kvp.Key, kvp.Value, toTick);
                }
            }
        }

        public bool ClientRelive(GameClient client)
        {
            int toPosX, toPosY;
            if (client.ClientData.MapCode == this.MapCode)
            {
                if (!GetBirthPoint(out toPosX, out toPosY))
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

        private bool GetBirthPoint(out int toPosX, out int toPosY)
        {
            toPosX = -1;
            toPosY = -1;

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(this.MapCode, out gameMap))
            {
                return false;
            }

            int defaultBirthPosX = GameManager.MapMgr.DictMaps[this.MapCode].DefaultBirthPosX;
            int defaultBirthPosY = GameManager.MapMgr.DictMaps[this.MapCode].DefaultBirthPosY;
            int defaultBirthRadius = GameManager.MapMgr.DictMaps[this.MapCode].BirthRadius;

            //从配置根据地图取默认位置
            Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, this.MapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
            toPosX = (int)newPos.X;
            toPosY = (int)newPos.Y;

            return true;
        }

        #region 自测，GM之灵
        public void OnGMCommand(GameClient client, string[] cmdFields)
        {
            if (cmdFields[1] == "join")
            {
                ProcessMoRiJudgeJoin(client, (int)TCPGameServerCmds.CMD_SPR_MORI_JOIN, null, null);
            }
            else if (cmdFields[1] == "quit")
            {
                ProcessMoRiJudgeQuit(client, (int)TCPGameServerCmds.CMD_SPR_MORI_QUIT, null, null);
            }
        }
        #endregion

        public double GetCopyAwardRate(int copySeqId)
        {
            MoRiJudgeCopy judgeCopy = null;
            lock (copyDict)
            {
                if (!copyDict.TryGetValue(copySeqId, out judgeCopy))
                {
                    return 1.0;
                }
            }


            return CalcAwardRate(judgeCopy);
        }

        // 客户端进入地图
        public void NotifyTimeStateAndBossEvent(GameClient client)
        {
            MoRiJudgeCopy judgeCopy = null;
            lock (copyDict)
            {
                if (!copyDict.TryGetValue(client.ClientData.FuBenSeqID, out judgeCopy))
                {
                    return;           
                }
            }

            lock (judgeCopy)
            {
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFY_TIME_STATE, judgeCopy.StateTimeData);

                foreach (var boss in judgeCopy.MonsterList)
                {
                    if (boss.BirthMs > 0)
                    {
                        GameManager.ClientMgr.BroadSpecialCopyMapMessageStr(
                            (int)TCPGameServerCmds.CMD_NTF_MORI_MONSTER_EVENT,
                            string.Format("{0}:{1}:{2}:{3}", (int)MoRiMonsterEvent.Birth, boss.Id, boss.BirthMs, boss.DeathMs),
                            judgeCopy.MyCopyMap);
                    }

                    if (boss.DeathMs > 0)
                    {
                        GameManager.ClientMgr.BroadSpecialCopyMapMessageStr(
                            (int)TCPGameServerCmds.CMD_NTF_MORI_MONSTER_EVENT,
                            string.Format("{0}:{1}:{2}:{3}", (int)MoRiMonsterEvent.Death, boss.Id, boss.BirthMs, boss.DeathMs),
                            judgeCopy.MyCopyMap);
                    }
                }
            }
        }
    }
}
