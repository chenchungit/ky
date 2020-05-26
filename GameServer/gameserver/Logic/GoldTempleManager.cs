using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    // 黄金神庙管理器 [4/7/2014 LiaoWei]
    public class GoldTempleManager
    {
        /// <summary>
        /// sub money interval -- 1 minute
        /// </summary>
        public int m_SubMoneyInterval = 60000;

        /// <summary>
        /// last sub money tick
        /// </summary>
        public long m_SubMoneyTick = 0;

        /// <summary>
        // 心跳处理
        /// </summary>
        public void HeartBeatGoldtempleScene()
        {
            int nRoleNum = 0;
            nRoleNum = GameManager.ClientMgr.GetMapClientsCount(Data.GoldtempleData.MapID);
            if (nRoleNum <= 0)
            {
                return;
            }

            List<Object> objsList = GameManager.ClientMgr.GetMapClients(Data.GoldtempleData.MapID);

            if (objsList != null)
            {
                for (int n = 0; n < objsList.Count; ++n)
                {
                    GameClient client = objsList[n] as GameClient;
                    if (client == null)
                        continue;

                    if (client.ClientData.MapCode != Data.GoldtempleData.MapID)
                        continue;

                    SubDiamond(client);
                }
            }
        }

        /// <summary>
        /// 减钻石
        /// </summary>
        public void SubDiamond(GameClient client)
        {
            long lTicks = TimeUtil.NOW();
            if (lTicks >= (m_SubMoneyTick + m_SubMoneyInterval))
            {
                m_SubMoneyTick = lTicks;

                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, Data.GoldtempleData.OneMinuteNeedDiamond, "黄金神庙扣除"))
                {
                    KickOutScene(client);
                }

            }
        }

        /// <summary>
        /// 踢出场景
        /// </summary>
        public void KickOutScene(GameClient client)
        {
            int toMapCode = GameManager.MainMapCode;
            int toPosX = -1;
            int toPosY = -1;
            if (MapTypes.Normal == Global.GetMapType(client.ClientData.LastMapCode))
            {
                if (GameManager.BattleMgr.BattleMapCode != client.ClientData.LastMapCode || GameManager.ArenaBattleMgr.BattleMapCode != client.ClientData.LastMapCode)
                {
                    toMapCode = client.ClientData.LastMapCode;
                    toPosX = client.ClientData.LastPosX;
                    toPosY = client.ClientData.LastPosY;
                }
            }

            GameMap gameMap = null;
            if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap))
            {
                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, toMapCode, toPosX, toPosY, -1);
            }
        }

    }
}
