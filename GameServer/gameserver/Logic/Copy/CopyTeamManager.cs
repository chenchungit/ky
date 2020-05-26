using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.GameEvent;
using GameServer.Server;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Server.CmdProcesser;
using Server.Data;
using System.Threading;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using Server.Tools.Pattern;
using KF.Contract.Data;
using KF.Client;
using GameServer.Core.Executor;
using Tmsk.Contract;

namespace GameServer.Logic.Copy
{
    /// <summary>
    /// 组队副本事件管理器
    /// </summary>
    public partial class CopyTeamManager : SingletonTemplate<CopyTeamManager>, IManager, ICmdProcessorEx, IEventListener, IEventListenerEx
    {
        private CopyTeamManager() { }

        /// <summary>
        /// 需要记录伤害排名的副本ID集合
        /// </summary>
        private HashSet<int> RecordDamagesFuBenIDHashSet = new HashSet<int>();

        /// <summary>
        /// 关注副本队伍的角色列表
        /// </summary>
        private Dictionary<int, HashSet<int>> FuBenId2Watchers = new Dictionary<int, HashSet<int>>();

        /// <summary>
        /// 每个副本创建的队伍列表
        /// </summary>
        private Dictionary<int, HashSet<long>> FuBenId2Teams = new Dictionary<int,HashSet<long>>();

        /// <summary>
        /// 地图编号--->副本ID
        /// </summary>
        private Dictionary<int, int> MapCode2ToFubenId = new Dictionary<int, int>();

        /// <summary>
        /// 副本ID--->地图编号列表
        /// </summary>
        private Dictionary<int, List<int>> FuBenId2MapCodes = new Dictionary<int,List<int>>();

        /// <summary>
        /// 组队项字典
        /// </summary>
        private Dictionary<long, CopyTeamData> TeamDict = new Dictionary<long, CopyTeamData>();

        /// <summary>
        /// 本服务器区号，跨服副本使用
        /// </summary>
        private int ThisServerId;

        /// <summary>
        /// 角色ID到组队ID的映射字典
        /// </summary>
        private Dictionary<int, long> RoleId2JoinedTeam = new Dictionary<int, long>();

        /// <summary>
        /// 副本流水号 ---> 队伍ID
        /// </summary>
        private Dictionary<int, long> FuBenSeq2TeamId = new Dictionary<int, long>();

        /// <summary>
        /// 组队副本常量定义
        /// </summary>
        //public const int MaxTeamMemberCount = 5;
        public const int ConstCopyType = 1;

        /// <summary>
        /// 多线程锁
        /// </summary>
        private object Mutex = new object();


        #region Implement IManager
        public bool initialize()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_COPYTEAM, 5, CopyTeamManager.Instance());
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_REGEVENTNOTIFY, 4, CopyTeamManager.Instance());
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_LISTCOPYTEAMS, 4, CopyTeamManager.Instance());

            //向事件源注册监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerLeaveFuBen, CopyTeamManager.Instance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerLogout, CopyTeamManager.Instance());

            // 跨服副本
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamCreate, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamJoin, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamReady, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamKickout, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamLeave, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamDestroy, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamStart, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());

            ThisServerId = GameCoreInterface.getinstance().GetLocalServerId();

            //每个组队副本ID都映射一个角色ID列表，这些角色关注这个组队副本的变化
            foreach (var systemFuBenItem in GameManager.systemFuBenMgr.SystemXmlItemDict.Values)
            {
                int copyType = systemFuBenItem.GetIntValue("CopyType");
                if (Global.ConstTeamCopyType == copyType)
                {
                    int FubenID = systemFuBenItem.GetIntValue("ID");
                    FuBenId2Watchers.Add(FubenID, new HashSet<int>());
                    FuBenId2Teams.Add(FubenID, new HashSet<long>());
                }
            }

            //组队地图编号与副本编号的映射，主要是记录有哪些地图是组队副本的地图（一个副本有可能有多张地图） 
            List<FuBenMapItem> fubenMapItemList = FuBenManager.GetAllFubenMapItem();
            foreach (var fubenMapItem in fubenMapItemList)
            {
                int copyType = Global.GetFuBenCopyType(fubenMapItem.FuBenID);
                if (Global.ConstTeamCopyType == copyType)
                {
                    MapCode2ToFubenId.Add(fubenMapItem.MapCode, fubenMapItem.FuBenID);

                    if (!FuBenId2MapCodes.ContainsKey(fubenMapItem.FuBenID))
                    {
                        FuBenId2MapCodes.Add(fubenMapItem.FuBenID, new List<int>());
                    }
                    FuBenId2MapCodes[fubenMapItem.FuBenID].Add(fubenMapItem.MapCode);
                }
            }

            //需要记录伤害排名的副本ID集合
            RecordDamagesFuBenIDHashSet.Add(4000);

            // 唯一队伍ID
            UniqueTeamId.Instance().Init();

            return true;
        }

        public bool startup()
        {
            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            //向事件源删除监听器
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerLeaveFuBen, CopyTeamManager.Instance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerLogout, CopyTeamManager.Instance());

            // 跨服副本
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamCreate, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamJoin, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamReady, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamKickout, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamLeave, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamDestroy, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFCopyTeamStart, (int)SceneUIClasses.KuaFuCopy, CopyTeamManager.Instance());
            return true;
        }

        #endregion

        #region Implement ICmdProcessorEx
        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_COPYTEAM:
                    return HandleNetCmd_CopyTeam(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_REGEVENTNOTIFY:
                    return HandleNetCmd_RegRoomNotify(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LISTCOPYTEAMS: // NotImplement
                    return HandleNetCmd_GetRoomList(client, nID, bytes, cmdParams);
            }
            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return true;
        }
        #endregion

        #region Implement IEventListener
        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() == (int)EventTypes.PlayerLeaveFuBen)
            {
                PlayerLeaveFuBenEventObject eventObj = (PlayerLeaveFuBenEventObject)eventObject;
                this.RoleLeaveFuBen(eventObj.getPlayer());
            }
            else if (eventObject.getEventType() == (int)EventTypes.PlayerInitGame)
            {
                //PlayerInitGameEventObject eventObj = (PlayerInitGameEventObject)eventObject;
                //CopyTeamManager.getInstance().OnPlayerInitGame(eventObj.getPlayer());
            }
            else if (eventObject.getEventType() == (int)EventTypes.PlayerLogout)
            {
                PlayerLogoutEventObject eventObj = (PlayerLogoutEventObject)eventObject;
                this.OnPlayerLogout(eventObj.getPlayer());
            }
        }
        #endregion

        #region Implement interface IEventListenerEx
        public void processEvent(EventObjectEx eventObject)
        {
            switch (eventObject.EventType)
            {
                case (int)GlobalEventTypes.KFCopyTeamCreate: OnTeamCreate((eventObject as KFCopyRoomCreateEvent).Data); break;
                case (int)GlobalEventTypes.KFCopyTeamJoin: OnTeamJoin((eventObject as KFCopyRoomJoinEvent).Data); break;
                case (int)GlobalEventTypes.KFCopyTeamKickout: OnTeamKickout((eventObject as KFCopyRoomKickoutEvent).Data); break;
                case (int)GlobalEventTypes.KFCopyTeamLeave: OnTeamLeave((eventObject as KFCopyRoomLeaveEvent).Data); break;
                case (int)GlobalEventTypes.KFCopyTeamReady: OnTeamSetReady((eventObject as KFCopyRoomReadyEvent).Data); break;
                case (int)GlobalEventTypes.KFCopyTeamStart: OnTeamStart((eventObject as KFCopyRoomStartEvent).Data); break;
                case (int)GlobalEventTypes.KFCopyTeamDestroy: OnTeamDestroy((eventObject as KFCopyTeamDestroyEvent).Data); break;
                default: break;
            }

            eventObject.Handled = true;
        }
        #endregion

        #region 处理客户端请求
        private bool HandleNetCmd_CopyTeam(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            int teamType = Convert.ToInt32(cmdParams[1]);
            if (teamType == (int)TeamCmds.Create) //创建队伍
            {
                int copyId = Convert.ToInt32(cmdParams[2]);
                int minCombat = Convert.ToInt32(cmdParams[3]);
                int autoStart = Convert.ToInt32(cmdParams[4]);
                this.HandleCreateCopyTeam(client, copyId, minCombat, autoStart);
            }
            else if (teamType == (int)TeamCmds.Apply) //申请组队
            {
                long teamId = Convert.ToInt64(cmdParams[2]);
                this.HandleApplyCopyTeam(client, teamId);
            }
            else if (teamType == (int)TeamCmds.Remove) //踢出队伍
            {
                int otherRoleId = Convert.ToInt32(cmdParams[2]);
                this.HandleKickoutCopyTeam(client, otherRoleId);
            }
            else if (teamType == (int)TeamCmds.Quit) //离开组队
            {
                this.HandleQuitFromTeam(client);
            }
            else if (teamType == (int)TeamCmds.Ready) //准备状态变化
            {
                int ready = Convert.ToInt32(cmdParams[2]);
                this.HandleSetReady(client, ready);
            }
            else if (teamType == (int)TeamCmds.QuickJoinTeam) //快速加入
            {
                int copyId = Convert.ToInt32(cmdParams[2]);
                this.HandleQuickJoinTeam(client, copyId);
            }

            return true;
        }

        private bool HandleNetCmd_RegRoomNotify(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            int copyId = Convert.ToInt32(cmdParams[1]);
            int ready = Convert.ToInt32(cmdParams[2]);

            if (ready > 0)
                this.RegisterCopyTeamListNotify(client, copyId);
            else
                this.UnRegisterCopyTeamListNotify(client);

            return true;
        }

        private bool HandleNetCmd_GetRoomList(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            // 根据客户端代码，这个消息已经废弃不用了
            throw new NotImplementedException();
        }


        /// <summary>
        /// 客户端请求创建副本队伍
        /// </summary>
        /// <param name="client">玩家</param>
        /// <param name="copyId">副本ID</param>
        /// <param name="minCombat">最小战斗力</param>
        /// <param name="autoStart">人满是否自动开始</param>
        private void HandleCreateCopyTeam(GameClient client, int copyId, int minCombat, int autoStart)
        {
            if (client.ClientSocket.IsKuaFuLogin)
                return;

            SystemXmlItem copyItem = null;
            if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(copyId, out copyItem)
                || !FuBenChecker.HasFinishedPreTask(client, copyItem)
                || !FuBenChecker.HasPassedPreCopy(client, copyItem)
                || !FuBenChecker.IsInCopyLevelLimit(client, copyItem)
                || !FuBenChecker.IsInCopyTimesLimit(client, copyItem)
                )
            {
                return;
            }

            lock (Mutex)
            {
                long oldTeamId;
                if (RoleId2JoinedTeam.TryGetValue(client.ClientData.RoleID, out oldTeamId))
                {
                    NotifyTeamCmd(client, CopyTeamErrorCodes.AllreadyHasTeam, (int)TeamCmds.Create, 0, "");
                    return;
                }
            }

            // 不存在此副本
            if (!FuBenId2Watchers.ContainsKey(copyId))
                return;

            if (!IsKuaFuCopy(copyId))
            {
                CopyTeamCreateData data = new CopyTeamCreateData();
                data.Member = ClientDataToTeamMemberData(client.ClientData);
                data.MinCombat = minCombat;
                data.TeamId = UniqueTeamId.Instance().Create();
                data.CopyId = copyId;
                data.AutoStart = autoStart;
                OnTeamCreate(data);

                HandleSetReady(client, 1);
            }
            else
            {
                KFCopyTeamCreateReq req = new KFCopyTeamCreateReq();
                req.Member = ClientDataToTeamMemberData(client.ClientData);
                req.Member.RoleName = string.Format(Global.GetLang("[{0}区]{1}"), req.Member.ServerId, req.Member.RoleName);
                req.CopyId = copyId;
                req.MinCombat = minCombat;
                req.AutoStart = autoStart;
                req.TeamId = UniqueTeamId.Instance().Create();
                KFCopyTeamCreateRsp rsp = KFCopyRpcClient.getInstance().CreateTeam(req);
                if (rsp == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("KF 创建队伍RPC调用失败 roleid={0}, rolename={1}, copyid={2}", client.ClientData.RoleID, client.ClientData.RoleName, copyId));
                    NotifyTeamCmd(client, CopyTeamErrorCodes.ServerException, (int)TeamCmds.Create, 0, "");
                }
                else if (rsp.ErrorCode == CopyTeamErrorCodes.Success)
                {
                    OnTeamCreate(rsp.Data);
                }
                else
                {
                    NotifyTeamCmd(client, rsp.ErrorCode, (int)TeamCmds.Create, 0, "");
                    LogManager.WriteLog(LogTypes.Error, string.Format("KF 创建队伍失败 roleid={0}, rolename={1}, copyid={2}, errorcode={3}", client.ClientData.RoleID, client.ClientData.RoleName, copyId, rsp.ErrorCode));
                }
            }            
        }

        /// <summary>
        /// 客户端请求加入队伍
        /// </summary>
        /// <param name="client"></param>
        /// <param name="teamId"></param>
        /// <param name="autoQuitOldTeam"></param>
        public void HandleApplyCopyTeam(GameClient client, long teamId)
        {
            if (client.ClientSocket.IsKuaFuLogin)
                return;

            lock (Mutex)
            {
                long oldTeamId;
                if (RoleId2JoinedTeam.TryGetValue(client.ClientData.RoleID, out oldTeamId))
                {
                    NotifyTeamCmd(client, CopyTeamErrorCodes.AllreadyHasTeam, (int)TeamCmds.Apply, 0, "");
                    return;
                }                             

                CopyTeamData td = null;
                if (!TeamDict.TryGetValue(teamId, out td))
                {
                    NotifyListTeamRemove(client, teamId);
                    return;
                }

                SystemXmlItem copyItem = null;
                if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(td.FuBenId, out copyItem)
                    || !FuBenChecker.HasFinishedPreTask(client, copyItem)
                    || !FuBenChecker.HasPassedPreCopy(client, copyItem)
                    || !FuBenChecker.IsInCopyLevelLimit(client, copyItem)
                    || !FuBenChecker.IsInCopyTimesLimit(client, copyItem)
                    )
                {
                    return;
                }

                if (td.TeamRoles.Count >= ConstData.CopyRoleMax(td.FuBenId))
                {
                    NotifyTeamCmd(client, CopyTeamErrorCodes.TeamIsFull, (int)TeamCmds.Apply, 0, "");
                    return;
                }

                if (client.ClientData.CombatForce < td.MinZhanLi)
                {
                    NotifyTeamCmd(client, CopyTeamErrorCodes.ZhanLiLow, (int)TeamCmds.Apply, 0, "");
                    return;
                }

                if (!IsKuaFuCopy(td.FuBenId))
                {
                    CopyTeamJoinData data = new CopyTeamJoinData();
                    data.Member = ClientDataToTeamMemberData(client.ClientData);
                    data.TeamId = teamId;
                    OnTeamJoin(data);
                }
                else
                {
                    KFCopyTeamJoinReq req = new KFCopyTeamJoinReq();
                    req.Member = ClientDataToTeamMemberData(client.ClientData);
                    req.Member.RoleName = string.Format(Global.GetLang("[{0}区]{1}"), req.Member.ServerId, req.Member.RoleName);
                    req.CopyId = td.FuBenId;
                    req.TeamId = td.TeamID;
                    KFCopyTeamJoinRsp rsp = KFCopyRpcClient.getInstance().JoinTeam(req);
                    if (rsp == null)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 加入队伍RPC调用失败 roleid={0}, rolename={1}, teamid={2}", client.ClientData.RoleID, client.ClientData.RoleName, teamId));
                        NotifyTeamCmd(client, CopyTeamErrorCodes.ServerException, (int)TeamCmds.Apply, 0, "");
                    }
                    else if (rsp.ErrorCode == CopyTeamErrorCodes.Success)
                    {
                        OnTeamJoin(rsp.Data);
                    }
                    else
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 加入队伍失败 roleid={0}, rolename={1}, teamid={2}, errorcode={3}", client.ClientData.RoleID, client.ClientData.RoleName, teamId, rsp.ErrorCode));
                        NotifyTeamCmd(client, rsp.ErrorCode, (int)TeamCmds.Apply, 0, "");

                        if (rsp.ErrorCode == CopyTeamErrorCodes.TeamIsDestoryed)
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("KF 加入队伍, 队伍在中心已销毁 roleid={0}, rolename={1}, teamid={2}", client.ClientData.RoleID, client.ClientData.RoleName, teamId));
                            // 这种情况要特别注意，防止中心重启，要清除掉该房间
                            OnTeamDestroy(new CopyTeamDestroyData() { TeamId = req.TeamId });
                        }
                    }          
                }

            }
        }

        /// <summary>
        /// 踢出队伍
        /// </summary>
        /// <param name="client"></param>
        /// <param name="otherRoleId"></param>
        public void HandleKickoutCopyTeam(GameClient client, int otherRoleId)
        {
            if (client.ClientSocket.IsKuaFuLogin)
                return;

            lock (Mutex)
            {
                long teamId;
                if (!RoleId2JoinedTeam.TryGetValue(client.ClientData.RoleID, out teamId))
                {
                    NotifyTeamCmd(client, CopyTeamErrorCodes.NoTeam, (int)TeamCmds.Remove, 0, "");
                    return;
                }
                
                CopyTeamData td;
                if (!TeamDict.TryGetValue(teamId, out td))
                {
                    RoleId2JoinedTeam.Remove(client.ClientData.RoleID);
                    NotifyTeamCmd(client, CopyTeamErrorCodes.TeamIsDestoryed, (int)TeamCmds.Remove, 0, "");
                    return;
                }

                if (td.LeaderRoleID != client.ClientData.RoleID || client.ClientData.RoleID == otherRoleId)
                {
                    NotifyTeamCmd(client, CopyTeamErrorCodes.NotTeamLeader, (int)TeamCmds.Remove, 0, "");
                    return;
                }

                if (!IsKuaFuCopy(td.FuBenId))
                {
                    CopyTeamKickoutData data = new CopyTeamKickoutData();
                    data.FromRoleId = client.ClientData.RoleID;
                    data.ToRoleId = otherRoleId;
                    data.TeamId = td.TeamID;
                    OnTeamKickout(data);
                }
                else
                {
                    KFCopyTeamKickoutReq req = new KFCopyTeamKickoutReq();
                    req.FromRoleId = client.ClientData.RoleID;
                    req.ToRoleId = otherRoleId;
                    req.TeamId = td.TeamID;

                    KFCopyTeamKickoutRsp rsp = KFCopyRpcClient.getInstance().KickoutTeam(req);
                    if (rsp == null)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 队伍踢人RPC调用失败 roleid={0}, rolename={1}, otherrid={2}", client.ClientData.RoleID, client.ClientData.RoleName, otherRoleId));
                        NotifyTeamCmd(client, CopyTeamErrorCodes.ServerException, (int)TeamCmds.Remove, 0, "");
                    }
                    else if (rsp.ErrorCode == CopyTeamErrorCodes.Success)
                    {
                        OnTeamKickout(rsp.Data);
                    }
                    else
                    {
                        NotifyTeamCmd(client, rsp.ErrorCode, (int)TeamCmds.Remove, 0, "");
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 队伍踢人失败 roleid={0}, rolename={1}, teamid={2}, errorcode={3}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId, rsp.ErrorCode));
                        if (rsp.ErrorCode == CopyTeamErrorCodes.TeamIsDestoryed)
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("KF 队伍踢人, 队伍在中心已销毁 roleid={0}, rolename={1}, otherrid={2}", client.ClientData.RoleID, client.ClientData.RoleName, otherRoleId));
                            // 这种情况要特别注意，防止中心重启，要清除掉该房间
                            OnTeamDestroy(new CopyTeamDestroyData() { TeamId = req.TeamId });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 退出队伍
        /// </summary>
        /// <param name="client"></param>
        public void HandleQuitFromTeam(GameClient client, bool notifyOther = true)
        {
            lock (Mutex)
            {
                long teamId;
                if (!RoleId2JoinedTeam.TryGetValue(client.ClientData.RoleID, out teamId))
                    return;

                CopyTeamData td;
                if (!TeamDict.TryGetValue(teamId, out td))
                {
                    RoleId2JoinedTeam.Remove(client.ClientData.RoleID);
                    return;
                }

                CopyTeamMemberData member = td.TeamRoles.Find(_role => _role.RoleID == client.ClientData.RoleID);
                if (member == null)
                    return;

                if (!IsKuaFuCopy(td.FuBenId))
                {
                    CopyTeamLeaveData data = new CopyTeamLeaveData();
                    data.TeamId = td.TeamID;
                    data.RoleId = client.ClientData.RoleID;
                    OnTeamLeave(data);
                }
                else
                {
                    KFCopyTeamLeaveReq req = new KFCopyTeamLeaveReq();
                    req.ReqServerId = ThisServerId;
                    req.RoleId = client.ClientData.RoleID;
                    req.TeamId = td.TeamID;
                    KFCopyTeamLeaveRsp rsp = KFCopyRpcClient.getInstance().LeaveTeam(req);
                    if (rsp == null)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 离开队伍RPC调用失败 roleid={0}, rolename={1}, teamid={2}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId));
                        NotifyTeamCmd(client, CopyTeamErrorCodes.ServerException, (int)TeamCmds.Quit, 0, "");
                    }
                    else if (rsp.ErrorCode == CopyTeamErrorCodes.Success)
                    {
                        OnTeamLeave(rsp.Data);
                    }
                    else
                    {
                        NotifyTeamCmd(client, rsp.ErrorCode, (int)TeamCmds.Quit, 0, "");
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 离开队伍失败 roleid={0}, rolename={1}, teamid={2}, errorcode={3}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId, rsp.ErrorCode));
                        if (rsp.ErrorCode == CopyTeamErrorCodes.TeamIsDestoryed)
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("KF 离开队伍, 队伍在中心已销毁 roleid={0}, rolename={1}, teamid={2}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId));
                            // 这种情况要特别注意，防止中心重启，要清除掉该房间
                            OnTeamDestroy(new CopyTeamDestroyData() { TeamId = req.TeamId });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 设置准备状态
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ready"></param>
        public void HandleSetReady(GameClient client, int ready)
        {
            if (client.ClientSocket.IsKuaFuLogin)
                return;

            lock (Mutex)
            {
                long teamId;
                if (!RoleId2JoinedTeam.TryGetValue(client.ClientData.RoleID, out teamId))
                {
                    NotifyTeamStateChanged(client, (int)CopyTeamErrorCodes.NoTeam, client.ClientData.RoleID, 0);
                    return;
                }

                CopyTeamData td;
                if (!TeamDict.TryGetValue(teamId, out td))
                {
                    return;
                }

                if (!IsKuaFuCopy(td.FuBenId))
                {
                    CopyTeamReadyData data = new CopyTeamReadyData();
                    data.RoleId = client.ClientData.RoleID;
                    data.TeamId = td.TeamID;
                    data.Ready = ready;
                    OnTeamSetReady(data);
                }
                else
                {
                    KFCopyTeamSetReadyReq req = new KFCopyTeamSetReadyReq();
                    req.RoleId = client.ClientData.RoleID;
                    req.TeamId = td.TeamID;
                    req.Ready = ready;
                    KFCopyTeamSetReadyRsp rsp = KFCopyRpcClient.getInstance().SetReady(req);
                    if (rsp == null)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 设置准备状态RPC调用失败 roleid={0}, rolename={1}, teamid={2}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId));
                        NotifyTeamStateChanged(client, (int)CopyTeamErrorCodes.ServerException, client.ClientData.RoleID, 0);
                    }
                    else if (rsp.ErrorCode == CopyTeamErrorCodes.Success)
                    {
                        OnTeamSetReady(rsp.Data);
                    }
                    else
                    {
                        // ???
                        NotifyTeamStateChanged(client, (int)rsp.ErrorCode, client.ClientData.RoleID, 0);
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 设置准备状态失败 roleid={0}, rolename={1}, teamid={2}, errorcode={3}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId, rsp.ErrorCode));
                        if (rsp.ErrorCode == CopyTeamErrorCodes.TeamIsDestoryed)
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("KF 设置准备状态, 队伍在中心已销毁 roleid={0}, rolename={1}, teamid={2}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId));
                            // 这种情况要特别注意，防止中心重启，要清除掉该房间
                            OnTeamDestroy(new CopyTeamDestroyData() { TeamId = req.TeamId });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 快速加入队伍
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public void HandleQuickJoinTeam(GameClient client, int copyId)
        {
            if (client.ClientSocket.IsKuaFuLogin)
                return;

            SystemXmlItem copyItem = null;
            if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(copyId, out copyItem)
                || !FuBenChecker.HasFinishedPreTask(client, copyItem)
                || !FuBenChecker.HasPassedPreCopy(client, copyItem)
                || !FuBenChecker.IsInCopyLevelLimit(client, copyItem)
                || !FuBenChecker.IsInCopyTimesLimit(client, copyItem)
                )
            {
                return;
            }

            lock (Mutex)
            {
                if (RoleId2JoinedTeam.ContainsKey(client.ClientData.RoleID))
                {
                    NotifyTeamCmd(client, CopyTeamErrorCodes.AllreadyHasTeam, (int)TeamCmds.QuickJoinTeam, 0, "");
                    return;
                }

                int zhanLi = client.ClientData.CombatForce;

                HashSet<long> teamIdList = null;
                if (!FuBenId2Teams.TryGetValue(copyId, out teamIdList)
                    || teamIdList.Count <= 0)
                {
                    NotifyTeamCmd(client, CopyTeamErrorCodes.NoAcceptableTeam, (int)TeamCmds.QuickJoinTeam, -1, "");
                    return;
                }

                CopyTeamData selectTd = null;
                foreach (var teamId in teamIdList.ToList())
                {
                    CopyTeamData tmpTd = null;
                    if (!TeamDict.TryGetValue(teamId, out tmpTd))
                    {
                        teamIdList.Remove(teamId);
                        continue;
                    }

                    if (tmpTd.StartTime <= 0
                        && zhanLi >= tmpTd.MinZhanLi
                        && tmpTd.MemberCount < ConstData.CopyRoleMax(tmpTd.FuBenId))
                    {
                        selectTd = tmpTd;
                        break;
                    }
                }

                if (selectTd == null)
                {
                    NotifyTeamCmd(client, CopyTeamErrorCodes.NoAcceptableTeam, (int)TeamCmds.QuickJoinTeam, -1, "");
                    return;
                }

                HandleApplyCopyTeam(client, selectTd.TeamID);
            }

            /*
            long oldTeamID = FindRoleID2TeamID(client.ClientData.RoleID);
            if (oldTeamID > 0) //如果有队伍
            {
         
            }

            

            if (null != td)
            {
                AddRoleID2TeamID(client.ClientData.RoleID, td.TeamID);

                //通知角色组队的指令信息
                NotifyTeamCmd(client, CopyTeamErrorCodes.Success, (int)TeamCmds.QuickJoinTeam, td.TeamID, td.TeamName);
                NotifyTeamData(td);
                NotifyTeamListChange(td);
            }
            else
            {
            }*/
        }

        /// <summary>
        /// 房主点击开始游戏
        /// </summary>
        /// <param name="client"></param>
        /// <param name="fubenSeqId"></param>
        public void HandleClickStart(GameClient client, int fubenSeqId)
        {
            lock (Mutex)
            {
                CopyTeamData td = null;
                if (!CanEnterScene(client, out td))
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ENTERFUBEN, string.Format("{0}:{1}", client.ClientData.RoleID, -100));
                    return;
                }

                if (!IsKuaFuCopy(td.FuBenId))
                {
                    CopyTeamStartData data = new CopyTeamStartData();
                    data.TeamId = td.TeamID;
                    data.StartMs = TimeUtil.NOW();
                    data.ToServerId = 0; //非跨服没有目标服务器
                    data.FuBenSeqId = fubenSeqId;

                    OnTeamStart(data);
                }
                else
                {
                     SystemXmlItem copyItem = null;
                     FuBenMapItem mapItem = null;
                     if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(td.FuBenId, out copyItem)
                         || (mapItem = FuBenManager.FindMapCodeByFuBenID(td.FuBenId, copyItem.GetIntValue("MapCode"))) == null)
                     {
                         return;
                     }
       
                    KFCopyTeamStartReq req = new KFCopyTeamStartReq();
                    req.RoleId = client.ClientData.RoleID;
                    req.TeamId = td.TeamID;
                    req.LastMs = mapItem.MaxTime * 60 * 1000; // 持续时间，中心超时检测

                    KFCopyTeamStartRsp rsp = KFCopyRpcClient.getInstance().StartGame(req);
                    if (rsp == null)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 开始游戏RPC调用失败 roleid={0}, rolename={1}, teamid={2}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId));
                    }
                    else if (rsp.ErrorCode == CopyTeamErrorCodes.Success)
                    {
                        OnTeamStart(rsp.Data);
                    }
                    else
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("KF 开始游戏 roleid={0}, rolename={1}, teamid={2}, errorcode={3}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId, rsp.ErrorCode));
                        if (rsp.ErrorCode == CopyTeamErrorCodes.TeamIsDestoryed)
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("KF 开始游戏, 队伍在中心已销毁 roleid={0}, rolename={1}, teamid={2}", client.ClientData.RoleID, client.ClientData.RoleName, req.TeamId));
                            // 这种情况要特别注意，防止中心重启，要清除掉该房间
                            OnTeamDestroy(new CopyTeamDestroyData() { TeamId = req.TeamId });
                        }
                    }
                }

                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ENTERFUBEN, string.Format("{0}:{1}", client.ClientData.RoleID, 1000));
            }
        }
        #endregion

        #region 功能接口



        /// <summary>
        /// 注册对某一个副本队伍的关注
        /// </summary>
        /// <param name="client"></param>
        /// <param name="copyId"></param>
        private void RegisterCopyTeamListNotify(GameClient client, int copyId)
        {
            // 尝试退出旧的队伍
            HandleQuitFromTeam(client);

            lock (Mutex)
            {
                int roleId = client.ClientData.RoleID;

                foreach (var kvp in FuBenId2Watchers)
                {
                    int _copyId = kvp.Key;
                    HashSet<int> _watchers = kvp.Value;
                    if (_copyId == copyId)
                    {
                        if (!_watchers.Contains(roleId))
                        {
                            _watchers.Add(roleId);
                        }
                    }
                    else
                    {
                        _watchers.Remove(roleId);
                    }
                }
            }

            SendTeamList(client, 0, copyId);
        }

        /// <summary>
        /// 取消注册对某一个副本队伍的关注
        /// </summary>
        /// <param name="client"></param>
        public void UnRegisterCopyTeamListNotify(GameClient client)
        {
            lock (Mutex)
            {
                foreach (var kvp in FuBenId2Watchers)
                {
                    HashSet<int> watchers = kvp.Value;
                    watchers.Remove(client.ClientData.RoleID);
                }
            }
        }

        public bool CanEnterScene(GameClient client, out CopyTeamData td)
        {
            td = null;

            MarriageInstance FubenInstance = MarryFuBenMgr.getInstance().GetMarriageInstanceEX(client);
            if (FubenInstance != null)
            {
                if (MapTypes.MarriageCopy == Global.GetMapType(FubenInstance.nHusband_FuBenID))
                {
                    if (MarryFuBenMgr.getInstance().CanEnterSceneEX(client) == true)
                    {
                        td = null;
                        return true;
                    }
                    td = null;
                    return false;
                }
            }

            lock (Mutex)
            {
                long teamId;
                if (!RoleId2JoinedTeam.TryGetValue(client.ClientData.RoleID, out teamId))
                {
                    NotifyTeamStateChanged(client, -1, client.ClientData.RoleID, 0);
                    return false;
                }

                if (TeamDict.TryGetValue(teamId, out td) && td.LeaderRoleID == client.ClientData.RoleID)
                {
                    if (IsKuaFuCopy(td.FuBenId))
                    {
                        return true;
                    }
                    else
                    {
                        foreach (var member in td.TeamRoles)
                        {
                            if (!member.IsReady) 
                                return false;

                            GameClient gc = GameManager.ClientMgr.FindClient(member.RoleID);
                            if (gc == null)
                            {
                                member.IsReady = false;
                                NotifyTeamData(td);
                                return false;
                            }
                        }

                        return true;
                    }
                }

                td = null;
                return false;

                /*
                td = FindData(teamId);
                if (null != td && roleID == td.LeaderRoleID)
                {
                    int readyCount = 0;
                    bool someoneOffline = false;
                    lock (td)
                    {
                        for (int i = 0; i < td.TeamRoles.Count; i++)
                        {
                            GameClient gc = GameManager.ClientMgr.FindClient(td.TeamRoles[i].RoleID);
                            if (gc == null)
                            {
                                td.TeamRoles[i].IsReady = false;
                                someoneOffline = true;
                                break;
                            }
                            if (td.TeamRoles[i].IsReady)
                            {
                                readyCount++;
                            }
                        }
                    }

                    if (someoneOffline)
                    {
                        NotifyTeamData(td);
                    }

                    if (readyCount == td.MemberCount)
                    {
                        return true;
                    }
                }

                td = null;
                return false;*/

            }            
        }

        #endregion 功能接口

        #region 组队项管理
        /// <summary>
        /// 返回从指定位置开始的指定的个数  已废弃，可删除
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public List<CopyTeamData> GetTeamDataList(int startIndex, int count, int sceneIndex, int zhanLi)
        {
            int index = 0;
            List<CopyTeamData> teamDataList = new List<CopyTeamData>();
            lock (TeamDict)
            {
                foreach (var teamData in TeamDict.Values)
                {
                    if (index >= startIndex &&
                        sceneIndex == teamData.FuBenId &&
                        teamData.StartTime == 0 &&
                        zhanLi >= teamData.MinZhanLi &&
                        teamData.MemberCount < ConstData.CopyRoleMax(sceneIndex))
                    {
                        teamDataList.Add(teamData.SimpleClone());
                        if (teamDataList.Count >= count)
                        {
                            break;
                        }
                    }
                    index++;
                }
            }

            return teamDataList;
        }

        #endregion 组队项管理

        #region 其他

        /// <summary>
        /// 将ClientData 类型转换为 TeamMemberData类型(组队时使用)
        /// </summary>
        /// <param name="clientData"></param>
        /// <returns></returns>
        public CopyTeamMemberData ClientDataToTeamMemberData(SafeClientData clientData)
        {
            CopyTeamMemberData teamMemberData = new CopyTeamMemberData()
            {
                RoleID = clientData.RoleID,
                RoleName = Global.FormatRoleName2(clientData, clientData.RoleName),
                RoleSex = clientData.RoleSex,
                Level = clientData.Level,
                Occupation = clientData.Occupation,
                RolePic = clientData.RolePic,
                MapCode = clientData.MapCode,
                OnlineState = 1,
                MaxLifeV = clientData.LifeV,
                CurrentLifeV = clientData.CurrentLifeV,
                MaxMagicV = clientData.MagicV,
                CurrentMagicV = clientData.CurrentMagicV,
                PosX = clientData.PosX,
                PosY = clientData.PosY,
                CombatForce = clientData.CombatForce,
                ChangeLifeLev = clientData.ChangeLifeCount,
                ServerId = ThisServerId,
                ZoneId = clientData.ZoneID,
            };

            return teamMemberData;
        }

        public void RoleLeaveFuBen(GameClient client)
        {
#if true
            HandleQuitFromTeam(client);
#else
            int roleID = client.ClientData.RoleID;
            int teamID = FindRoleID2TeamID(client.ClientData.RoleID);
            if (teamID <= 0) //如果没有队伍
            {
                return;
            }

            //查找组队的数据
            CopyTeamData td = FindData(teamID);
            if (null == td) //没有找到组队数据
            {
                //清空组队ID
                RemoveRoleID2TeamID(roleID);
                return;
            }

            bool destroy = false;
            lock (td)
            {
                if (td.MemberCount > 1) //转交队长
                {
                    for (int i = 0; i < td.TeamRoles.Count; i++)
                    {
                        if (td.TeamRoles[i].RoleID == client.ClientData.RoleID)
                        {
                            td.TeamRoles[i].OnlineState = 2;
                            td.MemberCount--;
                            break;
                        }
                    }
                }
                else
                {
                    destroy = true;
                }
            }

            if (destroy)
            {
                //删除组队数据
                RemoveData(teamID);
            }

            //清空组队ID
            RemoveRoleID2TeamID(roleID);

            //NotifyTeamStateChanged(client, CopyTeamErrorCodes.LeaveTeam, roleID, 0);//通知组队数据的指令信息
            NotifyTeamData(td);
#endif
        }

        /// <summary>
        /// 玩家退出游戏
        /// 这里比较特殊: 
        /// 1: 非跨服登录, 退出队伍(td.KFServerId == 0)
        /// 2: 跨服登录，在源服务器队伍未开始时(td.KFServerId == 0)需退出队伍，在跨服服务器(td.KFServerId == ThisServerId)退出时，需要退出队伍
        /// </summary>
        /// <param name="client"></param>
        public void OnPlayerLogout(GameClient client)
        {
            if (client == null) return;

            UnRegisterCopyTeamListNotify(client);

            lock (Mutex)
            {
                long teamId;
                if (!RoleId2JoinedTeam.TryGetValue(client.ClientData.RoleID, out teamId))
                {
                    return;
                }

                CopyTeamData td;
                if (!TeamDict.TryGetValue(teamId, out td))
                {
                    return;
                }

                if (td.KFServerId == 0 || td.KFServerId == ThisServerId)
                {
                    HandleQuitFromTeam(client);
                }
            }
        }

        /// <summary>
        /// 是否是组队副本地图
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public bool IsTeamCopyMapCode(int mapCode)
        {
            return MapCode2ToFubenId.ContainsKey(mapCode);
        }

        /// <summary>
        /// 获取副本对应的地图列表
        /// </summary>
        /// <param name="fubenId"></param>
        /// <returns></returns>
        public List<int> GetTeamCopyMapCodes(int fubenId)
        {
            List<int> mapCodes = null;
            if (!FuBenId2MapCodes.TryGetValue(fubenId, out mapCodes))
            {
                return null;
            }

            return mapCodes;
        }


        /// <summary>
        /// 是否需要记录角色伤害信息
        /// </summary>
        /// <param name="fuBenID"></param>
        /// <returns></returns>
        public bool NeedRecordDamageInfoFuBenID(int fuBenID)
        {
            return RecordDamagesFuBenIDHashSet.Contains(fuBenID) || GameManager.GuildCopyMapMgr.IsGuildCopyMap(fuBenID);
        }

        #endregion

        #region 组队相关

        public void NotifyTeamListChange(CopyTeamData td)
        {
            if (td == null) return;

            lock (Mutex)
            {
                HashSet<int> watchers = null;
                if (!FuBenId2Watchers.TryGetValue(td.FuBenId, out watchers))
                    return;

                List<int> watcherList = watchers.ToList();
                if (watcherList == null || watcherList.Count() <= 0)
                    return;

                foreach (var rid in watcherList)
                {
                    GameClient client = GameManager.ClientMgr.FindClient(rid);
                    if (client == null)
                    {
                        watchers.Remove(rid);
                        continue;
                    }

                    if (td.MemberCount <= 0 || td.MinZhanLi <= client.ClientData.CombatForce || td.StartTime > 0)
                    {
                        NotifyListTeamData(client, td);
                    }
                }
            }

            /*
            List<GameClient> roleList = new List<GameClient>();
            List<int> removeList = null;
            lock(FuBenId2Watchers)
            {
                HashSet<int> list;
                if(FuBenId2Watchers.TryGetValue(td.SceneIndex, out list))
                {
                    foreach (var id in list)
                    {
                        GameClient client = GameManager.ClientMgr.FindClient(id);
                        if (null == client)
                        {
                            if (null == removeList) removeList = new List<int>();
                            removeList.Add(id);
                        }
                        else if (td.MemberCount == 0 || (td.MinZhanLi <= client.ClientData.CombatForce))
                        {
                            roleList.Add(client);
                        }
                    }
                }
            }

            for (int i = 0; i < roleList.Count; i++)
            {
                NotifyListTeamData(roleList[i], td);
            }

            UnRegisterCopyTeamListNotifyForOfflineClient(removeList, td.SceneIndex);*/
        }

        public void NotifyListTeamData(GameClient client, CopyTeamData ctd)
        {
            int memberCount = ctd.StartTime > 0 ? 0 : ctd.MemberCount; //如果开始了
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", ctd.FuBenId, ctd.TeamID, ctd.TeamName, memberCount, ctd.MinZhanLi);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_LISTCOPYTEAMDATA, strcmd);
        }

        public void NotifyListTeamRemove(GameClient client, long teamID, int sceneIndex = -1)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", sceneIndex, teamID, "", 0, 0);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_LISTCOPYTEAMDATA, strcmd);
        }

        /// <summary>
        /// 通知角色组队的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamCmd(GameClient client, CopyTeamErrorCodes status, int teamType, long extTag1, string extTag2, int nOccu = -1, int nLev = -1, int nChangeLife = -1)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", (int)status, client.ClientData.RoleID, teamType, extTag1, extTag2, nOccu, nLev, nChangeLife);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_COPYTEAM, strcmd);
        }

        /// <summary>
        /// 通知组队数据
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamData(CopyTeamData td)
        {
            if (td == null) return;

            lock (Mutex)
            {
                foreach (var member in td.TeamRoles)
                {
                    if (member.ServerId == ThisServerId)
                    {
                        GameClient client = GameManager.ClientMgr.FindClient(member.RoleID);
                        if (null == client) continue;
                        client.sendCmd<CopyTeamData>((int)TCPGameServerCmds.CMD_SPR_COPYTEAMDATA, td);
                    }
                }
            }
        }

        /// <summary>
        /// 组队队员状态变化通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamStateChanged(GameClient client, long teamID, int roleID, int isReady)
        {
            string strcmd = string.Format("{0}:{1}:{2}", roleID, teamID, isReady);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_COPYTEAMSTATE, strcmd);
        }

        /// <summary>
        /// 通知组队副本进入的消息
        /// </summary>
        /// <param name="roleIDsList"></param>
        /// <param name="minLevel"></param>
        /// <param name="maxLevel"></param>
        /// <param name="mapCode"></param>
        public void NotifyTeamFuBenEnterMsg(List<int> roleIDsList, int minLevel, int maxLevel, int leaderMapCode, int leaderRoleID, int fuBenID, int fuBenSeqID, int enterNumber, int maxFinishNum, bool igoreNumLimit = false)
        {
            if (null == roleIDsList || roleIDsList.Count <= 0) return;
            for (int i = 0; i < roleIDsList.Count; i++)
            {
                GameClient otherClient = GameManager.ClientMgr.FindClient(roleIDsList[i]);
                if (null == otherClient) continue; //不在线，则不通知

                //级别不匹配，则不通知
                int unionLevel = Global.GetUnionLevel(otherClient.ClientData.ChangeLifeCount, otherClient.ClientData.Level);
                if (unionLevel < minLevel || unionLevel > maxLevel)
                {
                    continue;
                }

                if (!igoreNumLimit)
                {
                    FuBenData fuBenData = Global.GetFuBenData(otherClient, fuBenID);
                    int nFinishNum;
                    int haveEnterNum = Global.GetFuBenEnterNum(fuBenData, out nFinishNum);
                    if ((enterNumber >= 0 && haveEnterNum >= enterNumber) || (maxFinishNum >= 0 && nFinishNum >= maxFinishNum))
                    {
                        continue;
                    }
                }

                //通知组队副本进入的消息
                GameManager.ClientMgr.NotifyTeamMemberFuBenEnterMsg(otherClient, leaderRoleID, fuBenID, fuBenSeqID);
            }
        }

        #endregion 组队相关

        #region 队伍查询
        /// <summary>
        /// 列举组队的队伍并返回列表
        /// </summary>
        /// <param name="client"></param>
        /// <param name="startIndex"></param>
        /// <param name="copyId"></param>
        public void SendTeamList(GameClient client, int startIndex, int copyId)
        {
            CopySearchTeamData searchData = new CopySearchTeamData()
            {
                StartIndex = startIndex,
                TotalTeamsCount = 0,
                PageTeamsCount = (int)SearchResultConsts.MaxSearchTeamsNum * 10,
                TeamDataList = null,
            };

            lock (Mutex)
            {
                searchData.TotalTeamsCount = TeamDict.Count();
                startIndex = startIndex >= TeamDict.Count ? 0 : startIndex;

                if (TeamDict.Count > 0)
                {
                    searchData.TeamDataList = new List<CopyTeamData>();
                    int _index = 0;
                    foreach (var td in TeamDict.Values)
                    {
                        if (_index >= startIndex
                            && copyId == td.FuBenId
                            && td.StartTime == 0
                            && client.ClientData.CombatForce >= td.MinZhanLi
                            && td.MemberCount < ConstData.CopyRoleMax(copyId))
                        {
                            searchData.TeamDataList.Add(td.SimpleClone());
                            if (searchData.TeamDataList.Count() >= searchData.PageTeamsCount)
                            {
                                break;
                            }
                        }

                        ++_index;
                    }
                }
            }

            client.sendCmd<CopySearchTeamData>((int)TCPGameServerCmds.CMD_SPR_LISTCOPYTEAMS, searchData);
        }
        #endregion 队伍查询
    }
}
