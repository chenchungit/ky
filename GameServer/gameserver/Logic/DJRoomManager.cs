using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
//using System.Windows.Resources;
using System.Windows;
//using System.Windows.Media.Animation;
using System.Threading;
using GameServer.Interface;
using Server.Data;
using GameServer.Server;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using System.Net;
using System.Net.Sockets;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 点将台房间管理
    /// </summary>
    public class DJRoomManager
    {
        #region 点将台房间管理线程锁对象

        /// <summary>
        /// 锁对象
        /// </summary>
        private Object mutex = new Object();

        /// <summary>
        /// 线程锁对象
        /// </summary>
        public Object Mutex
        {
            get { return mutex; }
        }

        #endregion 点将台房间管理线程锁对象

        #region 点将台房间号管理

        /// <summary>
        /// 基础的点将台房间号
        /// </summary>
        int BaseRoomID = 1;

        /// <summary>
        /// 获取一个新的房间的ID
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public int GetNewRoomID()
        {
            int id = 1;
            lock (mutex)
            {
                id = BaseRoomID++;
            }

            return id;
        }

        #endregion 点将台房间号管理

        #region 点将台房间对象管理

        /// <summary>
        /// 点将台房间字典
        /// </summary>
        private Dictionary<int, DJRoomData> DJRoomDict = new Dictionary<int, DJRoomData>(100);

        /// <summary>
        /// 点将台房间列表
        /// </summary>
        private List<DJRoomData> DJRoomDataList = new List<DJRoomData>(100);

        /// <summary>
        /// 复制房间数据列表
        /// </summary>
        /// <returns></returns>
        public List<DJRoomData> CloneRoomDataList()
        {
            List<DJRoomData> roomDataList = null;
            lock (mutex)
            {
                roomDataList = DJRoomDataList.GetRange(0, DJRoomDataList.Count);
            }

            return roomDataList;
        }

        /// <summary>
        /// 根据房间ID查找房间数据对象
        /// </summary>
        /// <param name="roomID"></param>
        /// <returns></returns>
        public DJRoomData FindRoomData(int roomID)
        {
            DJRoomData djRoomData = null;
            lock (mutex)
            {
                DJRoomDict.TryGetValue(roomID, out djRoomData);
            }

            return djRoomData;
        }

        /// <summary>
        /// 加入房间数据
        /// </summary>
        /// <param name="roomData"></param>
        public void AddRoomData(DJRoomData roomData)
        {
            lock (mutex)
            {
                DJRoomDict[roomData.RoomID] = roomData;
                DJRoomDataList.Add(roomData);
            }
        }

        /// <summary>
        /// 删除房间数据
        /// </summary>
        /// <param name="roomData"></param>
        public void RemoveRoomData(int roomID)
        {
            lock (mutex)
            {
                DJRoomData roomData = null;
                if (DJRoomDict.TryGetValue(roomID, out roomData))
                {
                    DJRoomDict.Remove(roomID);
                    DJRoomDataList.Remove(roomData);
                }
            }
        }

        /// <summary>
        /// 获取下一个房间对象
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DJRoomData GetNextDJRoomData(int index)
        {
            DJRoomData djRoomData = null;
            lock (mutex)
            {
                if (index < DJRoomDataList.Count)
                {
                    djRoomData = DJRoomDataList[index];
                }
            }

            return djRoomData;
        }

        #endregion 点将台房间对象管理

        #region 点将台房间角色对象管理

        /// <summary>
        /// 点将台房间角色对象字典
        /// </summary>
        private Dictionary<int, DJRoomRolesData> DJRoomRolesDict = new Dictionary<int, DJRoomRolesData>(100);

        /// <summary>
        /// 根据房间ID查找房间角色对象
        /// </summary>
        /// <param name="roomID"></param>
        /// <returns></returns>
        public DJRoomRolesData FindRoomRolesData(int roomID)
        {
            DJRoomRolesData djRoomRolesData = null;
            lock (mutex)
            {
                DJRoomRolesDict.TryGetValue(roomID, out djRoomRolesData);
            }

            return djRoomRolesData;
        }

        /// <summary>
        /// 加入房间角色对象数据
        /// </summary>
        /// <param name="roomData"></param>
        public void AddRoomRolesData(DJRoomRolesData djRoomRolesData)
        {
            lock (mutex)
            {
                DJRoomRolesDict[djRoomRolesData.RoomID] = djRoomRolesData;
            }
        }

        /// <summary>
        /// 删除房间角色对象数据
        /// </summary>
        /// <param name="roomData"></param>
        public void RemoveRoomRolesData(int roomID)
        {
            lock (mutex)
            {
                if (DJRoomRolesDict.ContainsKey(roomID))
                {
                    DJRoomRolesDict.Remove(roomID);
                }
            }
        }

        /// <summary>
        /// 设置角色的状态
        /// </summary>
        /// <param name="roomID"></param>
        /// <param name="state"></param>
        public void SetRoomRolesDataRoleState(int roomID, int roleID, int state)
        {
            DJRoomRolesData djRoomRolesData = FindRoomRolesData(roomID);
            if (null == djRoomRolesData) return;

            lock (mutex)
            {
                int oldState = 0;
                djRoomRolesData.RoleStates.TryGetValue(roleID, out oldState);
                if (state > oldState)
                {
                    djRoomRolesData.RoleStates[roleID] = state;
                }
            }
        }

        #endregion 点将台房间角色对象管理

        #region 点将台战斗调度

        /// <summary>
        /// 处理正在战斗的过程
        /// </summary>
        public void ProcessFighting()
        {
            int index = 0;
            DJRoomData djRoomData = GetNextDJRoomData(index);
            while (null != djRoomData)
            {
                ProcessRoomFighting(djRoomData);

                index++;
                djRoomData = GetNextDJRoomData(index);
            }
        }

        /// <summary>
        /// 是否战斗已经结束?
        /// </summary>
        /// <param name="djRoomRolesData"></param>
        /// <returns></returns>
        private bool CanGameOver(DJRoomRolesData djRoomRolesData)
        {
            bool team1Over = true;
            for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
            {
                int state = 0;
                djRoomRolesData.RoleStates.TryGetValue(djRoomRolesData.Team1[i].RoleID, out state);
                if (state == 1)
                {
                    team1Over = false;
                    break;
                }
            }

            bool team2Over = true;
            for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
            {
                int state = 0;
                djRoomRolesData.RoleStates.TryGetValue(djRoomRolesData.Team2[i].RoleID, out state);
                if (state == 1)
                {
                    team2Over = false;
                    break;
                }
            }

            return (team1Over || team2Over);
        }

        /// <summary>
        /// 是否战斗已经结束?
        /// </summary>
        /// <param name="djRoomRolesData"></param>
        /// <returns></returns>
        private int GetLoseTeam(DJRoomRolesData djRoomRolesData)
        {
            bool team1Over = true;
            for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
            {
                int state = 0;
                djRoomRolesData.RoleStates.TryGetValue(djRoomRolesData.Team1[i].RoleID, out state);
                if (state == 1)
                {
                    team1Over = false;
                    break;
                }
            }

            bool team2Over = true;
            for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
            {
                int state = 0;
                djRoomRolesData.RoleStates.TryGetValue(djRoomRolesData.Team2[i].RoleID, out state);
                if (state == 1)
                {
                    team2Over = false;
                    break;
                }
            }

            if (team1Over)
            {
                return 1;
            }

            if (team2Over)
            {
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// 处理房间中站在战斗的过程
        /// </summary>
        /// <param name="djRoomData"></param>
        private void ProcessRoomFighting(DJRoomData djRoomData)
        {
            lock (djRoomData)
            {
                if (djRoomData.PKState <= 0) //战斗还没有开始
                {
                    return;
                }
            }

            int djFightState = 0;
            lock (djRoomData)
            {
                djFightState = djRoomData.DJFightState;
            }

            long startFightTicks = 0;
            lock (djRoomData)
            {
                startFightTicks = djRoomData.StartFightTicks;
            }

            //如果超过了最大倒计时时间，允许角色进入战斗伤害
            long ticks = TimeUtil.NOW();

            if (djFightState == (int)DJFightStates.NoFight) //如果还没开始战斗
            {
                //发送广播消息
                /// 通知角色点将台房间内战斗的指令信息(参战者，观众)
                GameManager.ClientMgr.NotifyDianJiangFightCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    djRoomData, (int)DJFightStates.WaitingFight, ticks.ToString());

                lock (djRoomData)
                {
                    djRoomData.DJFightState = (int)DJFightStates.WaitingFight;
                    djRoomData.StartFightTicks = ticks;
                }
            }
            else if (djFightState == (int)DJFightStates.WaitingFight) //等待战斗倒计时(此时伤害无效)
            {
                if (ticks >= (startFightTicks + (30 * 1000)))
                {
                    //发送广播消息
                    /// 通知角色点将台房间内战斗的指令信息(参战者，观众)
                    GameManager.ClientMgr.NotifyDianJiangFightCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        djRoomData, (int)DJFightStates.StartFight, ticks.ToString());

                    lock (djRoomData)
                    {
                        djRoomData.PKState = 2;
                        djRoomData.DJFightState = (int)DJFightStates.StartFight;
                        djRoomData.StartFightTicks = ticks;
                    }
                }
            }
            else if (djFightState == (int)DJFightStates.StartFight) //开始战斗(倒计时中)
            {
                bool gameOver = false;
                DJRoomRolesData djRoomRolesData = FindRoomRolesData(djRoomData.RoomID);
                if (null != djRoomRolesData)
                {
                    // 是否战斗已经结束?
                    gameOver = CanGameOver(djRoomRolesData);
                }

                if (gameOver || ticks >= (startFightTicks + (3 * 30 * 1000)))
                {
                    //发送广播消息
                    /// 通知角色点将台房间内战斗的指令信息(参战者，观众)
                    GameManager.ClientMgr.NotifyDianJiangFightCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        djRoomData, (int)DJFightStates.EndFight, ticks.ToString());

                    lock (djRoomData)
                    {
                        djRoomData.PKState = 3;
                        djRoomData.DJFightState = (int)DJFightStates.EndFight;
                        djRoomData.StartFightTicks = ticks;
                    }

                    //发送房间数据
                    GameManager.ClientMgr.NotifyDianJiangData(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, djRoomData);
                }
            }
            else if (djFightState == (int)DJFightStates.EndFight) //结束战斗(此时伤害无效)
            {
                //开始计算给予的奖励
                /// 处理点将台结束时的奖励
                ProcessDJFightAwards(djRoomData);

                //发送广播消息
                /// 通知角色点将台房间内战斗的指令信息(参战者，观众)
                GameManager.ClientMgr.NotifyDianJiangFightCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    djRoomData, (int)DJFightStates.ClearRoom, ticks.ToString());

                lock (djRoomData)
                {
                    djRoomData.DJFightState = (int)DJFightStates.ClearRoom;
                    djRoomData.StartFightTicks = ticks;
                }
            }
            else if (djFightState == (int)DJFightStates.ClearRoom) //清空房间
            {
                if (ticks >= (startFightTicks + (60 * 1000)))
                {
                    //通知角色点将台房间内战斗的指令信息(参战者，观众)离开离开场景消息
                    GameManager.ClientMgr.NotifyDJFightRoomLeaveMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, djRoomData);

                    //将点将台房间数据从内存中清空, 并重置所有用户的变量状态(包括观众)
                    //删除点将台房间
                    GameManager.ClientMgr.RemoveDianJiangRoom(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, djRoomData);
                }
            }
        }

        #endregion 点将台战斗调度

        #region 奖励处理

        /// <summary>
        /// 获取队伍的积分平均值
        /// </summary>
        /// <param name="team1"></param>
        /// <returns></returns>
        private int GetTeamAvgDJPoint(List<DJRoomRoleData> team)
        {
            if (team.Count <= 0) return 0;

            int totalDJPoint = 0;
            for (int i = 0; i < team.Count; i++)
            {
                totalDJPoint += team[i].DJPoint;
            }

            return (totalDJPoint / team.Count);
        }

        /// <summary>
        /// 获取积分的级别
        /// </summary>
        /// <param name="djPoint"></param>
        /// <returns></returns>
        private int GetDJPointClass(int djPoint)
        {
            if (djPoint <= 100)
            {
                return 0;
            }
            else if (djPoint <= 200)
            {
                return 1;
            }
            else if (djPoint <= 300)
            {
                return 2;
            }

            return 3;
        }

        /// <summary>
        /// 获取不同级别对应的加减积分
        /// </summary>
        /// <param name="pointClass"></param>
        /// <param name="isWinner"></param>
        /// <returns></returns>
        private int GetRetPoint(int pointClass, bool isWinner)
        {
            int retPoint = 0;
            if (0 == pointClass) //A级别
            {
                if (isWinner)
                {
                    retPoint = 10;
                }
                else
                {
                    retPoint = -4;
                }
            }
            else if (1 == pointClass) //B级别
            {
                if (isWinner)
                {
                    retPoint = 9;
                }
                else
                {
                    retPoint = -5;
                }
            }
            else if (2 == pointClass) //C级别
            {
                if (isWinner)
                {
                    retPoint = 8;
                }
                else
                {
                    retPoint = -6;
                }
            }
            else //D级别
            {
                if (isWinner)
                {
                    retPoint = 7;
                }
                else
                {
                    retPoint = -7;
                }
            }

            return retPoint;
        }

        /// <summary>
        /// 获取点将积分
        /// </summary>
        /// <returns></returns>
        private int GetTeamRolePoint(DJRoomRoleData djRoomRoleData, int otherTeamAvgDJPoint, bool isWinner)
        {
            int retPoint = 0;
            int selfPointClass = GetDJPointClass(djRoomRoleData.DJPoint);
            int otherPointClass = GetDJPointClass(otherTeamAvgDJPoint);
            int absDJPoint = Math.Abs(selfPointClass - otherPointClass);
            
            retPoint = GetRetPoint(selfPointClass, isWinner);
            if (0 == absDJPoint) //同级别
            {
                //不处理
            }
            else if (1 == absDJPoint)
            {
                if (selfPointClass > otherPointClass)
                {
                    if (isWinner)
                    {
                        //不变
                    }
                    else
                    {
                        retPoint -= 10; //多扣除10分
                    }
                }
                else
                {
                    if (isWinner)
                    {
                        retPoint += 10; //多加10分
                    }
                    else
                    {
                        //不变
                    }
                }
            }
            else if (2 == absDJPoint)
            {
                if (selfPointClass > otherPointClass)
                {
                    if (isWinner)
                    {
                        //不变
                    }
                    else
                    {
                        retPoint -= 15; //多扣除15分
                    }
                }
                else
                {
                    if (isWinner)
                    {
                        retPoint += 15; //多加15分
                    }
                    else
                    {
                        //不变
                    }
                }
            }
            else if (3 == absDJPoint)
            {
                if (selfPointClass > otherPointClass)
                {
                    if (isWinner)
                    {
                        //不变
                    }
                    else
                    {
                        retPoint -= 20; //多扣除20分
                    }
                }
                else
                {
                    if (isWinner)
                    {
                        retPoint += 20; //多加20分
                    }
                    else
                    {
                        //不变
                    }
                }
            }

            return retPoint;
        }

        /// <summary>
        /// 处理点将台结束时的奖励
        /// </summary>
        private void ProcessDJFightAwards(DJRoomData djRoomData)
        {
            DJRoomRolesData djRoomRolesData = FindRoomRolesData(djRoomData.RoomID);
            if (null == djRoomRolesData)
            {
                return;
            }

            DJRoomRolesPoint djRoomRolesPoint = new DJRoomRolesPoint()
            {
                RoomID = djRoomData.RoomID,
                RoomName = djRoomData.RoomName,
                RolePoints = new List<DJRoomRolePoint>(),
            };

            lock (djRoomRolesData)
            {
                int loseTeam = GetLoseTeam(djRoomRolesData);
                //if (0 == loseTeam) return;

                //获取队伍1的积分平均值
                int team1AvgDJPoint = GetTeamAvgDJPoint(djRoomRolesData.Team1);

                //获取队伍2的积分平均值
                int team2AvgDJPoint = GetTeamAvgDJPoint(djRoomRolesData.Team2);

                for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
                {
                    djRoomRolesPoint.RolePoints.Add(new DJRoomRolePoint()
                    {
                        RoleID = djRoomRolesData.Team1[i].RoleID,
                        RoleName = djRoomRolesData.Team1[i].RoleName,
                        FightPoint = loseTeam > 0 ? GetTeamRolePoint(djRoomRolesData.Team1[i], team2AvgDJPoint, (loseTeam != 1)) : 0,
                    });
                }

                for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
                {
                    djRoomRolesPoint.RolePoints.Add(new DJRoomRolePoint()
                    {
                        RoleID = djRoomRolesData.Team2[i].RoleID,
                        RoleName = djRoomRolesData.Team2[i].RoleName,
                        FightPoint = loseTeam > 0 ? GetTeamRolePoint(djRoomRolesData.Team1[i], team1AvgDJPoint, (loseTeam != 2)) : 0,
                    });
                }
            }

            //写入数据库中...
            for (int i = 0; i < djRoomRolesPoint.RolePoints.Count; i++)
            {
                if (djRoomRolesPoint.RolePoints[i].FightPoint != 0)
                {
                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDDJPOINT,
                        string.Format("{0}:{1}", djRoomRolesPoint.RolePoints[i].RoleID, djRoomRolesPoint.RolePoints[i].FightPoint),
                        null, GameManager.LocalServerIdForNotImplement);
                }
            }

            //发送给客户端(参战者，以及所有的观众)
            // 发送点将台房间的战斗结果
            GameManager.ClientMgr.NotifyDianJiangRoomRolesPoint(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, djRoomRolesPoint);
        }

        #endregion 奖励处理
    }
}
