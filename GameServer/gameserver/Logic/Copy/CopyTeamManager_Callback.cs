using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KF.Contract.Data;
using KF.Client;
using Server.Tools;
using Tmsk.Contract;

namespace GameServer.Logic.Copy
{
    public partial class CopyTeamManager
    {
        /// <summary>
        /// 队伍创建回调
        /// </summary>
        /// <param name="data"></param>
        private void OnTeamCreate(CopyTeamCreateData data)
        {
            if (data == null) return;

            bool isKuaFuCopy = IsKuaFuCopy(data.CopyId);

            CopyTeamData td = new CopyTeamData();
            td.TeamID = data.TeamId;
            td.LeaderRoleID = data.Member.RoleID;
            td.FuBenId = data.CopyId;
            td.MinZhanLi = data.MinCombat;
            td.AutoStart = data.AutoStart > 0;
            td.TeamRoles.Add(data.Member);
            td.TeamRoles[0].IsReady = true;
            td.TeamName = td.TeamRoles[0].RoleName; //KF
            td.MemberCount = td.TeamRoles.Count;

            lock (Mutex)
            {
                TeamDict[td.TeamID] = td;

                HashSet<long> teams = null;
                if (FuBenId2Teams.TryGetValue(td.FuBenId, out teams) && !teams.Contains(td.TeamID))
                {
                    teams.Add(td.TeamID);
                }

                if (data.Member.ServerId == ThisServerId)
                {
                    RoleId2JoinedTeam[data.Member.RoleID] = td.TeamID;

                    GameClient client = GameManager.ClientMgr.FindClient(data.Member.RoleID);
                    if (client != null)
                    {
                        NotifyTeamCmd(client, CopyTeamErrorCodes.Success, (int)TeamCmds.Create, td.TeamID, td.TeamName);
                    }
                }

                NotifyTeamData(td);
                NotifyTeamListChange(td);
            }

            /*
            int roleID = client.ClientData.RoleID;
            long teamID = FindRoleID2TeamID(roleID);
            if (teamID > 0)
            {
                QuitFromTeam(client);
            }

            teamID = GetNextAutoID();
            AddRoleID2TeamID(roleID, teamID);

            CopyTeamData td = new CopyTeamData()
            {
                TeamID = teamID,
                LeaderRoleID = roleID,
                SceneIndex = copyId,
                MinZhanLi = minCombat,
                AutoStart = autoStart > 0,
            };

            if (null == td.TeamRoles)
            {
                td.TeamRoles = new List<CopyTeamMemberData>();
            }

            td.TeamRoles.Add(ClientDataToTeamMemberData(client.ClientData));
            td.TeamRoles[0].IsReady = true;
            td.TeamName = td.TeamRoles[0].RoleName;
            td.MemberCount = td.TeamRoles.Count;

            //存入组队管理队列
            AddData(teamID, td);

            //通知组队数据的指令信息
            NotifyTeamCmd(client, CopyTeamErrorCodes.Success, (int)TeamCmds.Create, teamID, td.TeamName);
            NotifyTeamData(td);

            //添加队伍需要向注册通知的用户发送变化列表
            NotifyTeamListChange(td);*/
        }

        /// <summary>
        /// 加入队伍回调
        /// </summary>
        /// <param name="data"></param>
        private void OnTeamJoin(CopyTeamJoinData data)
        {
            if (data == null) return;

            lock (Mutex)
            {
                CopyTeamData td;
                if (!TeamDict.TryGetValue(data.TeamId, out td))
                    return;

                if (td.TeamRoles.Count >= ConstData.CopyRoleMax(td.FuBenId))
                    return;

                td.TeamRoles.Add(data.Member);
                td.MemberCount = td.TeamRoles.Count();

                if (data.Member.ServerId == ThisServerId)
                {
                    RoleId2JoinedTeam[data.Member.RoleID] = td.TeamID;
                    GameClient client = GameManager.ClientMgr.FindClient(data.Member.RoleID);
                    if (client != null)
                    {
                        NotifyTeamCmd(client, CopyTeamErrorCodes.Success, (int)TeamCmds.Apply, td.TeamID, td.TeamName);
                    }
                }

                NotifyTeamData(td);
                NotifyTeamListChange(td);
            }

            /*
            lock (td)
            {

                //是否能加入队伍
                //if (!CopyTeamManager.getInstance().CanAddToTeam(roleID, otherClient.ClientData.TeamID, 0))


                int index = td.TeamRoles.FindIndex((x) => x.RoleID == roleID);
                if (index >= 0)
                {
                    td.TeamRoles[index] = ClientDataToTeamMemberData(client.ClientData);
                }
                else
                {
                    td.TeamRoles.Add(ClientDataToTeamMemberData(client.ClientData));
                }
                td.MemberCount = td.TeamRoles.Count;
            }

            AddRoleID2TeamID(roleID, teamID);

            //通知角色组队的指令信息
            NotifyTeamCmd(client, CopyTeamErrorCodes.Success, (int)TeamCmds.Apply, teamID, td.TeamName);
            NotifyTeamData(td);
            NotifyTeamListChange(td);
             * 
             * 
             */
        }

        /// <summary>
        /// 踢出队伍回调
        /// </summary>
        /// <param name="data"></param>
        private void OnTeamKickout(CopyTeamKickoutData data)
        {
            if (data == null) return;

            lock (Mutex)
            {
                CopyTeamData td = null;
                if (!TeamDict.TryGetValue(data.TeamId, out td))
                {
                    return;
                }

                CopyTeamMemberData member = td.TeamRoles.Find(_role => _role.RoleID == data.ToRoleId);
                if (member == null) return;

                td.TeamRoles.Remove(member);
                td.MemberCount = td.TeamRoles.Count;

                if (member.ServerId == ThisServerId)
                {
                    RoleId2JoinedTeam.Remove(member.RoleID);
                    GameClient client = GameManager.ClientMgr.FindClient(member.RoleID);
                    if (client != null)
                    {
                        NotifyTeamStateChanged(client, (int)CopyTeamErrorCodes.BeRemovedFromTeam, member.RoleID, 0);
                    }
                }

                NotifyTeamData(td);
                NotifyTeamListChange(td);
            }

            /*
            bool destroy = false;
            lock (td)
            {
                //判断是否是队长
                if (td.LeaderRoleID != client.ClientData.RoleID)
                {
                    //通知角色组队的指令信息

                }

                if (td.TeamRoles.Count > 1) //转交队长
                {
                    for (int i = 0; i < td.TeamRoles.Count; i++)
                    {
                        if (td.TeamRoles[i].RoleID == otherRoleID)
                        {
                            td.TeamRoles.RemoveAt(i);
                            break;
                        }
                    }

                    //判断是否是队长
                    if (td.LeaderRoleID == client.ClientData.RoleID)
                    {
                        td.LeaderRoleID = td.TeamRoles[0].RoleID; //转交队长
                        td.TeamRoles[0].IsReady = true;
                        td.TeamName = td.TeamRoles[0].RoleName;
                    }
                }
                else
                {
                    destroy = true;
                    td.TeamRoles.Clear();
                    td.LeaderRoleID = -1; //强迫解散
                }
                td.MemberCount = td.TeamRoles.Count;
            }

            if (destroy)
            {
                //删除组队数据
                RemoveData(teamID);
            }

            //通知组队数据的指令信息
            NotifyTeamData(td); //发送null数据，强迫组队解散

            //清空组队ID
            RemoveRoleID2TeamID(otherRoleID);

            GameClient otherClient = GameManager.ClientMgr.FindClient(otherRoleID);
            if (null != otherClient)
            {
                NotifyTeamStateChanged(otherClient, CopyTeamErrorCodes.BeRemovedFromTeam, otherRoleID, 0);
            }

            NotifyTeamListChange(td);
             */
        }

        /// <summary>
        /// 离开队伍回调
        /// </summary>
        /// <param name="data"></param>
        private void OnTeamLeave(CopyTeamLeaveData data)
        {
            if (data == null) return;

            lock (Mutex)
            {
                CopyTeamData td = null;
                if (!TeamDict.TryGetValue(data.TeamId, out td))
                {
                    return;
                }

                CopyTeamMemberData member = td.TeamRoles.Find(_role => _role.RoleID == data.RoleId);
                if (member == null) return;

                td.TeamRoles.Remove(member);
                td.MemberCount = td.TeamRoles.Count;

                if (td.MemberCount <= 0)
                {
                    td.LeaderRoleID = -1;
                    OnTeamDestroy(new CopyTeamDestroyData() { TeamId = td.TeamID });
                }
                else
                {
                    if (td.LeaderRoleID == member.RoleID)
                    {
                        td.LeaderRoleID = td.TeamRoles[0].RoleID;
                        td.TeamRoles[0].IsReady = true;
                        td.TeamName = td.TeamRoles[0].RoleName;
                    }
                }

                if (member.ServerId == ThisServerId)
                {
                    RoleId2JoinedTeam.Remove(member.RoleID);
                    GameClient client = GameManager.ClientMgr.FindClient(member.RoleID);
                    if (client != null)
                    {
                        NotifyTeamStateChanged(client, (int)CopyTeamErrorCodes.LeaveTeam, member.RoleID, 0);
                    }
                }

                NotifyTeamData(td);
                NotifyTeamListChange(td);
            }

            /*
            int roleID = client.ClientData.RoleID;
            long teamID = FindRoleID2TeamID(client.ClientData.RoleID);
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

            bool updateTeamList = false;
            bool destroy = false;
            lock (td)
            {
                if (td.TeamRoles.Count > 1) //转交队长
                {
                    for (int i = 0; i < td.TeamRoles.Count; i++)
                    {
                        if (td.TeamRoles[i].RoleID == client.ClientData.RoleID)
                        {
                            td.TeamRoles.RemoveAt(i);
                            break;
                        }
                    }

                    //判断是否是队长
                    if (td.LeaderRoleID == client.ClientData.RoleID)
                    {
                        td.LeaderRoleID = td.TeamRoles[0].RoleID; //转交队长
                        td.TeamRoles[0].IsReady = true;
                        td.TeamName = td.TeamRoles[0].RoleName;
                    }
                    td.MemberCount = td.TeamRoles.Count;
                }
                else
                {
                    destroy = true;
                    td.LeaderRoleID = -1; //强迫解散
                    td.TeamRoles.Clear();
                    updateTeamList = true;
                }
                td.MemberCount = td.TeamRoles.Count;

                if (td.StartTime == 0)
                {
                    updateTeamList = true;
                }
            }

            if (destroy)
            {
                //删除组队数据
                RemoveData(teamID);
            }

            //清空组队ID
            RemoveRoleID2TeamID(roleID);

            //发送队伍状态
            NotifyTeamStateChanged(client, CopyTeamErrorCodes.LeaveTeam, roleID, 0);

            if (notifyOther)
            {
                //通知组队数据的指令信息
                NotifyTeamData(td); //发送null数据，强迫组队解散
            }

            if (updateTeamList)
            {
                //组队状态变化通知
                NotifyTeamListChange(td);
            }
             * */
        }

        /// <summary>
        /// 设置准备状态 回调
        /// </summary>
        /// <param name="data"></param>
        private void OnTeamSetReady(CopyTeamReadyData data)
        {
            if (data == null) return;

            lock (Mutex)
            {
                CopyTeamData td = null;
                if (!TeamDict.TryGetValue(data.TeamId, out td))
                {
                    return;
                }

                CopyTeamMemberData member = td.TeamRoles.Find(_role => _role.RoleID == data.RoleId);
                if (member == null) return;

                member.IsReady = data.Ready > 0;
                if (member.ServerId == ThisServerId)
                {
                    GameClient client = GameManager.ClientMgr.FindClient(member.RoleID);
                    if (client != null)
                    {
                        NotifyTeamStateChanged(client, td.TeamID, member.RoleID, data.Ready);
                    }
                }

                NotifyTeamData(td);

                // 满员了，所有人都已经准备，那么就开吧
                if (member.IsReady && td.AutoStart && td.MemberCount >= ConstData.CopyRoleMax(td.FuBenId) && td.TeamRoles.All(_role => _role.IsReady))
                {
                    CopyTeamMemberData leader = td.TeamRoles.Find(_role => _role.RoleID == td.LeaderRoleID);
                    if (leader != null && leader.ServerId == ThisServerId)
                    {
                        GameClient client = GameManager.ClientMgr.FindClient(leader.RoleID);
                        if (client != null)
                        {
                            NotifyTeamCmd(client, CopyTeamErrorCodes.Success, (int)TeamCmds.Start, 0, "");
                        }
                    }
                }
            }

            /*
            //int teamType = (int)TeamCmds.Ready;
            int roleID = client.ClientData.RoleID;
            long teamID = FindRoleID2TeamID(client.ClientData.RoleID);
            if (teamID <= 0)
            {

            }

            CopyTeamData td = FindData(teamID);
            if (null != td)
            {
                int readyCount = 0;
                bool someoneOffline = false;
                lock (td)
                {
                    for (int i = 0; i < td.TeamRoles.Count; i++)
                    {
                        GameClient gc;
                        if (td.TeamRoles[i].RoleID == roleID)
                        {
                            td.TeamRoles[i].IsReady = ready > 0; //更新状态
                            gc = client;
                        }
                        else
                        {
                            gc = GameManager.ClientMgr.FindClient(td.TeamRoles[i].RoleID);
                        }

                        if (null == gc)
                        {
                            td.TeamRoles[i].IsReady = false;
                            someoneOffline = true;
                            continue;
                        }

                        //状态变化通知
                        NotifyTeamStateChanged(gc, teamID, roleID, ready);

                        if (!someoneOffline && td.TeamRoles[i].IsReady && td.AutoStart)
                        {
                            readyCount++;
                            if (readyCount == MaxTeamMemberCount)
                            {
                                GameClient leader = GameManager.ClientMgr.FindClient(td.LeaderRoleID);
                                NotifyTeamCmd(leader, CopyTeamErrorCodes.Success, (int)TeamCmds.Start, 0, "");
                            }
                        }
                    }
                }

                if (someoneOffline)
                {
                    NotifyTeamData(td);
                }
            }*/
        }

        /// <summary>
        /// 队伍销毁 回调
        /// </summary>
        /// <param name="data"></param>
        private void OnTeamDestroy(CopyTeamDestroyData data)
        {
            if (data == null) return;

            lock (Mutex)
            {
                CopyTeamData td = null;
                if (!TeamDict.TryGetValue(data.TeamId, out td))
                {
                    return;
                }

                TeamDict.Remove(data.TeamId);
                FuBenSeq2TeamId.Remove(td.FuBenSeqID);
                HashSet<long> teamList = null;
                if (FuBenId2Teams.TryGetValue(td.FuBenId, out teamList))
                {
                    teamList.Remove(td.TeamID);
                }

                foreach (var member in td.TeamRoles)
                {
                    RoleId2JoinedTeam.Remove(member.RoleID);

                    if (member.ServerId != ThisServerId) 
                        continue;

                    GameClient client = GameManager.ClientMgr.FindClient(member.RoleID);
                    if (client != null)
                    {
                        NotifyTeamStateChanged(client, (int)CopyTeamErrorCodes.LeaveTeam, member.RoleID, 0);
                    }
                }

                // 队长id设为-1， 通知整个队伍
                td.LeaderRoleID = -1;
                NotifyTeamData(td);

                // 清空队伍，然后通知给所有关注列表的人
                td.TeamRoles.Clear();
                td.MemberCount = td.TeamRoles.Count;
                NotifyTeamListChange(td);
            }
        }

        /// <summary>
        /// 开始游戏 回调
        /// </summary>
        private void OnTeamStart(CopyTeamStartData data)
        {
            if (data == null) return;

            lock (Mutex)
            {
                CopyTeamData td = null;
                if (!this.TeamDict.TryGetValue(data.TeamId, out td))
                {
                    return;
                }

                td.StartTime = data.StartMs;
                td.KFServerId = data.ToServerId;
                td.FuBenSeqID = data.FuBenSeqId;

                bool isKuaFuCopy = IsKuaFuCopy(td.FuBenId);
                string toServerIp = string.Empty;
                int toServerPort = 0;
                if (isKuaFuCopy)
                {
                    if (!KFCopyRpcClient.getInstance().GetKuaFuGSInfo(data.ToServerId, out toServerIp, out toServerPort))
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("跨服副本CopyType={0}, RoomId={1}被分配到服务器ServerId={2}, 但是找不到该跨服活动服务器", td.FuBenId, data.TeamId, data.ToServerId));
                        return;
                    }
                }
                else
                {
                    FuBenSeq2TeamId[td.FuBenSeqID] = td.TeamID;
                }

                foreach (var member in td.TeamRoles)
                {
                    if (member.ServerId == ThisServerId)
                    {
                        GameClient client = GameManager.ClientMgr.FindClient(member.RoleID);
                        if (client == null) continue;

                        if (isKuaFuCopy)
                        {
                            client.ClientSocket.ClientKuaFuServerLoginData.RoleId = member.RoleID;
                            client.ClientSocket.ClientKuaFuServerLoginData.GameId = td.TeamID;
                            client.ClientSocket.ClientKuaFuServerLoginData.GameType = (int)GameTypes.KuaFuCopy;
                            client.ClientSocket.ClientKuaFuServerLoginData.EndTicks = 0;
                            client.ClientSocket.ClientKuaFuServerLoginData.ServerId = ThisServerId;
                            client.ClientSocket.ClientKuaFuServerLoginData.ServerIp = toServerIp;
                            client.ClientSocket.ClientKuaFuServerLoginData.ServerPort = toServerPort;
                            client.ClientSocket.ClientKuaFuServerLoginData.FuBenSeqId = data.FuBenSeqId;
                        }

                        // 通知倒计时
                        GameManager.ClientMgr.NotifyTeamMemberFuBenEnterMsg(client, td.LeaderRoleID, td.FuBenId, td.FuBenSeqID);
                    }
                }

                NotifyTeamListChange(td);
            }
        }
    }
}
