using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 组队的请求项
    /// </summary>
    public class TeamRequestItem
    {
        /// <summary>
        /// 请求加入或者被邀请的角色的ID
        /// </summary>
        public int ToRoleID = 0;

        /// <summary>
        /// 请求的时间
        /// </summary>
        public long RequestTicks = 0;
    }

    /// <summary>
    /// 组队管理
    /// </summary>
    public class TeamManager
    {
        #region 组队流水ID

        /// <summary>
        /// 组队流水ID
        /// </summary>
        private long BaseAutoID = 0;

        /// <summary>
        /// 获取下一个组队流水ID
        /// </summary>
        /// <returns></returns>
        public int GetNextAutoID()
        {
            return (int)(Interlocked.Increment(ref BaseAutoID) & 0x7fffffff);
        }

        #endregion 组队流水ID

        #region 角色ID和组队ID的映射管理

        /// <summary>
        /// 角色ID到组队ID的映射字典
        /// </summary>
        private Dictionary<int, int> _RoleID2TeamIDDict = new Dictionary<int, int>();

        /// <summary>
        /// 添加项
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public void AddRoleID2TeamID(int roleID, int teamID)
        {
            lock (_RoleID2TeamIDDict)
            {
                _RoleID2TeamIDDict[roleID] = teamID;
            }
        }

        /// <summary>
        /// 删除项
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public void RemoveRoleID2TeamID(int roleID)
        {
            lock (_RoleID2TeamIDDict)
            {
                if (_RoleID2TeamIDDict.ContainsKey(roleID))
                {
                    _RoleID2TeamIDDict.Remove(roleID);
                }
            }
        }

        /// <summary>
        /// 查找项
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public int FindRoleID2TeamID(int roleID)
        {
            int teamID = -1;
            lock (_RoleID2TeamIDDict)
            {
                if (_RoleID2TeamIDDict.TryGetValue(roleID, out teamID))
                {
                    return teamID;
                }
            }

            return teamID;
        }

        #endregion 角色ID和组队ID的映射管理

        #region 组队项管理

        /// <summary>
        /// 组队项字典
        /// </summary>
        private Dictionary<int, TeamData> _TeamDataDict = new Dictionary<int, TeamData>();

        /// <summary>
        /// 添加项
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public void AddData(int teamID, TeamData td)
        {
            lock (_TeamDataDict)
            {
                _TeamDataDict[teamID] = td;
            }
        }

        /// <summary>
        /// 删除项
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public void RemoveData(int teamID)
        {
            lock (_TeamDataDict)
            {
                if (_TeamDataDict.ContainsKey(teamID))
                {
                    _TeamDataDict.Remove(teamID);
                }
            }
        }

        /// <summary>
        /// 查找项
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public TeamData FindData(int teamID)
        {
            TeamData td = null;
            lock (_TeamDataDict)
            {
                _TeamDataDict.TryGetValue(teamID, out td);
            }

            return td;
        }

        /// <summary>
        /// 获取总的个数
        /// </summary>
        /// <returns></returns>
        public int GetTotalDataCount()
        {
            int count = 0;
            lock (_TeamDataDict)
            {
                count = _TeamDataDict.Count;
            }

            return count;
        }

        /// <summary>
        /// 返回从指定位置开始的指定的个数
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public List<TeamData> GetTeamDataList(int startIndex, int count)
        {
            int index = 0;
            List<TeamData> teamDataList = new List<TeamData>();
            lock (_TeamDataDict)
            {
                foreach (var teamData in _TeamDataDict.Values)
                {
                    if (index < startIndex)
                    {
                        index++;
                        continue;
                    }

                    teamDataList.Add(teamData);
                    if (teamDataList.Count >= count)
                    {
                        break;
                    }

                    index++;
                }
            }

            return teamDataList;
        }

        #endregion 组队项管理

        #region 组队请求和邀请的管理

        /// <summary>
        /// 组队请求的字典
        /// </summary>
        private Dictionary<string, TeamRequestItem> _TeamRequestDict = new Dictionary<string, TeamRequestItem>();

        /// <summary>
        /// 是否能加入队伍
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="teamID"></param>
        /// <returns></returns>
        public bool CanAddToTeam(int roleID, int teamID, int requestType)
        {
            TeamRequestItem teamRequestItem = null;
            string key = string.Format("{0}_{1}_{2}", roleID, teamID, requestType);

            lock (_TeamRequestDict)
            {
                if (!_TeamRequestDict.TryGetValue(key, out teamRequestItem))
                {
                    return true;
                }
            }

            long ticks = TimeUtil.NOW();
            if ((ticks - teamRequestItem.RequestTicks) >= (35 * 1000))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 将请求项缓存
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="teamID"></param>
        /// <returns></returns>
        public void AddTeamRequestItem(int roleID, int teamID, int requestType)
        {
            string key = string.Format("{0}_{1}_{2}", roleID, teamID, requestType);
            TeamRequestItem teamRequestItem = new TeamRequestItem()
            {
                ToRoleID = roleID,
                RequestTicks = (TimeUtil.NOW()),
            };

            lock (_TeamRequestDict)
            {
                _TeamRequestDict[key] = teamRequestItem;
            }
        }

        /// <summary>
        /// 删除请求项缓存
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="teamID"></param>
        /// <returns></returns>
        public void RemoveTeamRequestItem(int roleID, int teamID, int requestType)
        {
            string key = string.Format("{0}_{1}_{2}", roleID, teamID, requestType);
            lock (_TeamRequestDict)
            {
                _TeamRequestDict.Remove(key);
            }
        }

        #endregion 组队请求和要求的管理
    }
}
