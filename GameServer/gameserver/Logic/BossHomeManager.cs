using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Windows;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    // Boss之家管理器 [4/7/2014 LiaoWei]
    public class BossHomeManager
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
        public void HeartBeatBossHomeScene()
        {
            int nRoleNum = 0;
            nRoleNum = GameManager.ClientMgr.GetMapClientsCount(Data.BosshomeData.MapID);
            if (nRoleNum <= 0)
            {
                return;
            }

            List<Object> objsList = GameManager.ClientMgr.GetMapClients(Data.BosshomeData.MapID);

            if (objsList != null)
            {
                for (int n = 0; n < objsList.Count; ++n)
                {
                    GameClient client = objsList[n] as GameClient;
                    if (client == null)
                        continue;

                    if (client.ClientData.MapCode != Data.BosshomeData.MapID)
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
            if (0 == Data.BosshomeData.OneMinuteNeedDiamond)
            {
                return;
            }

            long lTicks = TimeUtil.NOW();
            if (lTicks >= (m_SubMoneyTick + m_SubMoneyInterval))
            {
                m_SubMoneyTick = lTicks;

                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, Data.BosshomeData.OneMinuteNeedDiamond, "BOSS之家扣除"))
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
