using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Tmsk.Contract;
using KF.Client;
using KF.Contract.Data;
using Server.Tools;
using System.Windows;

namespace GameServer.Logic.Copy
{
    public partial class CopyTeamManager
    {
        /// <summary>
        /// 是否是跨服副本
        /// 重要：每次增加新的跨服副本，这都要修改
        /// </summary>
        /// <param name="copyId"></param>
        /// <returns></returns>
        public bool IsKuaFuCopy(int copyId)
        {
            // 假的，暂时认为卡利玛神庙是跨服副本
           // if (copyId == 4000)
           //     return true;

            if (copyId == 70000 || copyId == 70100 || copyId == 70200)
                return true;

            return false;
        }

        public bool HandleKuaFuLogin(KuaFuServerLoginData data)
        {
            if (data == null) return false;

            lock (Mutex)
            {
                CopyTeamData td = null;
                if (!this.TeamDict.TryGetValue(data.GameId, out td)
                    || td.StartTime <= 0)
                {
                    // 防止本跨服服务器保存的队伍信息不是最新的，从中心取一下
                    td = KFCopyRpcClient.getInstance().GetTeamData(data.GameId);
                    if (td == null)
                    {
                        return false;
                    }

                    this.TeamDict[td.TeamID] = td;

                    HashSet<long> teamList = null;
                    if (this.FuBenId2Teams.TryGetValue(td.FuBenId, out teamList) && !teamList.Contains(td.TeamID))
                    {
                        teamList.Add(td.TeamID);
                    }
                }

                if (td == null) return false;

                if (td.KFServerId != ThisServerId)
                {
                    return false;
                }

                if (td.StartTime <= 0)
                {
                    return false;
                }

                if (!td.TeamRoles.Exists(_role => _role.RoleID == data.RoleId))
                {
                    return false;
                }

                if (td.FuBenSeqID <= 0)
                {
                    td.FuBenSeqID = GameCoreInterface.getinstance().GetNewFuBenSeqId();
                }

                data.FuBenSeqId = td.FuBenSeqID;
                FuBenSeq2TeamId[td.FuBenSeqID] = td.TeamID;

                return true;
            }
        }

        public bool HandleKuaFuInitGame(GameClient client)
        {
            if (client == null)
                return false;

            lock (Mutex)
            {
                CopyTeamData td = null;
                if (!this.TeamDict.TryGetValue(client.ClientSocket.ClientKuaFuServerLoginData.GameId, out td))
                {
                    return false;
                }

                SystemXmlItem systemFuBenItem = null;
                if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(td.FuBenId, out systemFuBenItem))
                {
                    return false;
                }
                int mapCode = systemFuBenItem.GetIntValue("MapCode");
                int destX, destY;
                if (!GetBirthPoint(mapCode, out destX, out destY))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("rolename={0} 跨服登录副本copyid={1}, 找不到出生点", client.ClientData.RoleName, td.FuBenId));
                    return false;
                }

                client.ClientData.MapCode = mapCode;
                client.ClientData.PosX = (int)destX;
                client.ClientData.PosY = (int)destY;
                client.ClientData.FuBenSeqID = client.ClientSocket.ClientKuaFuServerLoginData.FuBenSeqId;

                // 记录跨服玩家参加的队伍ID，玩家下线的时候用
                RoleId2JoinedTeam[client.ClientData.RoleID] = td.TeamID;

                return true;
            }
        }

        private bool GetBirthPoint(int mapCode, out int toPosX, out int toPosY)
        {
            toPosX = -1;
            toPosY = -1;

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out gameMap))
            {
                return false;
            }

            int defaultBirthPosX = gameMap.DefaultBirthPosX;
            int defaultBirthPosY = gameMap.DefaultBirthPosY;
            int defaultBirthRadius = gameMap.BirthRadius;

            //从配置根据地图取默认位置
            Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, mapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
            toPosX = (int)newPos.X;
            toPosY = (int)newPos.Y;

            return true;
        }


        /// <summary>
        /// 删除副本, 检测是否是跨服，是否需要上报中心
        /// </summary>
        /// <param name="FuBenSeqId"></param>
        public void OnCopyRemove(int FuBenSeqId)
        {
            long teamId = -1;
            lock (Mutex)
            {
                if (!this.FuBenSeq2TeamId.TryGetValue(FuBenSeqId, out teamId))
                {
                    return;                 
                }

                FuBenSeq2TeamId.Remove(FuBenSeqId);

                CopyTeamData td;
                if (!TeamDict.TryGetValue(teamId, out td))
                {
                    return;
                }

                OnTeamDestroy(new CopyTeamDestroyData() { TeamId = teamId });
                if (IsKuaFuCopy(td.FuBenId))
                {
                    KFCopyRpcClient.getInstance().KFCopyTeamRemove(teamId);
                }
            }
        }
    }
}
