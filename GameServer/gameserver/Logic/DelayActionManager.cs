using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using GameServer.Core.Executor;
using GameServer.Server;

namespace GameServer.Logic
{
    // 延迟动作管理类 [10/24/2013 LiaoWei]

    class DelayAction
    {
        public GameClient m_Client = null;
        public long m_StartTime = 0;                  // 开始时间
        public long m_DelayTime = 0;                  // 延迟时间
        public int[] m_Params = new int[2];                    // 参数
        public DelayActionType m_DelayActionType = DelayActionType.DB_NULL; // 执行何种动作 想法是 在global里面写个接口 根据枚举值来确定调用何种逻辑
    }

    class DelayActionManager
    {
        private static List<DelayAction> m_Actions = new List<DelayAction>();

        /// <summary>
        // 添加
        /// </summary>
        public static void AddDelayAction(DelayAction action)
        {
            lock (m_Actions)
            {
                m_Actions.Add(action);
            }
        }

        /// <summary>
        // 移除
        /// </summary>
        public static void RemoveDelayAction(DelayAction action)
        {
            lock (m_Actions)
            {
                m_Actions.Remove(action);
            }
        }

        /// <summary>
        // 开始动作
        /// </summary>
        public static void StartAction(DelayAction action)
        {
            DelayActionType nActionID = action.m_DelayActionType;
            switch(nActionID)
            {
                case DelayActionType.DA_BLINK:
                    {
                        // 闪现
                        int nParams = action.m_Params[0];   // 距离
                        GameClient client = action.m_Client;

                        int nRadius     = nParams * 100;                                            // 半径
                        GameMap gameMap = GameManager.MapMgr.DictMaps[client.ClientData.MapCode];   // 取得地图信息
                        int nDirection = client.ClientData.RoleDirection;   // 玩家朝向
                        Point pClientGrid = client.CurrentGrid;             // 玩家所在的格子
                        int nGridNum = nRadius / gameMap.MapGridWidth;      // 取得半径长度包含的格子数量
                        int nTmp = nGridNum;

                        // 根据当前所在的格子列表、半径  取得范围内的格子数量
                        //int nGridWidthNum = nRadius / gameMap.MapGridWidth;
                        //int nGridHeightNum = nRadius / gameMap.MapGridHeight;

                        //List<Point> lPointsList = Global.GetGridPointByRadius((int)pClientGrid.X, (int)pClientGrid.Y, nGridWidthNum, nGridHeightNum);   // 格子列表
                        //int nGridNum = lPointsList.Count;

                        // 根据朝向、当前所在格子、将要行进的格子数量  取得前方的格子列表
                        List<Point> lMovePointsList = Global.GetGridPointByDirection(nDirection, (int)pClientGrid.X, (int)pClientGrid.Y, nGridNum);

                        // 玩家、怪的阻挡设置 将影响能不能到达有人物或者怪的格子
                        byte holdBitSet = 0;
                        holdBitSet |= (byte)ForceHoldBitSets.HoldRole;
                        holdBitSet |= (byte)ForceHoldBitSets.HoldMonster;

                        for (int i = 0; i < lMovePointsList.Count; i++)
                        {
                            if (Global.InObsByGridXY(client.ObjectType, client.ClientData.MapCode, (int)lMovePointsList[i].X, (int)lMovePointsList[i].Y, 0, holdBitSet))
                                break;
                            else
                                --nGridNum;
                        }

                        if (nGridNum < nTmp)
                            pClientGrid = lMovePointsList[nTmp - nGridNum - 1];

                        Point canMovePoint = pClientGrid;
                        if (!Global.CanQueueMoveObject(client, nDirection, (int)pClientGrid.X, (int)pClientGrid.Y, nGridNum, nGridNum, holdBitSet, out canMovePoint, false))
                        {
                            Point clientMoveTo = new Point(canMovePoint.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, canMovePoint.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);

                            GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,client, (int)clientMoveTo.X, (int)clientMoveTo.Y,
                                                                    client.ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS, 3);
                            
                        }
                        else
                        {
                            Point clientMoveTo = new Point(lMovePointsList[lMovePointsList.Count - 1].X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, lMovePointsList[lMovePointsList.Count - 1].Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);

                            GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, (int)clientMoveTo.X, (int)clientMoveTo.Y, 
                                                                    client.ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS, 3);
                           
                        }

                        List<Object> objsList = Global.GetAll9Clients(client);
                        string strcmd = string.Format("{0}", client.ClientData.RoleID);
                        GameManager.ClientMgr.SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_ENDBLINK);

                        RemoveDelayAction(action);
                    }
                    break;
                default:
                    break;
            }

        }


        /// <summary>
        // 心跳处理
        /// </summary>
        public static void HeartBeatDelayAction()
        {
            for (int i = 0; i < m_Actions.Count; ++i)
            {
                // 当前时间
                long ticks = TimeUtil.NOW();

                DelayAction tmpInfo = m_Actions[i];
                long lStart = tmpInfo.m_StartTime;
                long lDelay = tmpInfo.m_DelayTime;

                // 时间计算 以及 动作触发
                if (ticks - lStart > lDelay)
                    StartAction(tmpInfo);

            }

        }

    }

}
