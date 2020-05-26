using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.MUWings;
using GameServer.Logic;
using GameServer.Logic.WanMota;

using Server.Data;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 领取扫荡奖励指令
    /// </summary>
    public class GetSweepWanMoTaAwardCmdProcessor : ICmdProcessor
    {
        private static GetSweepWanMoTaAwardCmdProcessor instance = new GetSweepWanMoTaAwardCmdProcessor();

        private GetSweepWanMoTaAwardCmdProcessor() 
        {
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_GET_SWEEP_REWARD, 1, this);
        }

        public static GetSweepWanMoTaAwardCmdProcessor getInstance()
        {
            return instance;
        }        

        /// <summary>
        /// 给扫荡奖励
        /// </summary>
        private int GiveSweepReward(GameClient client)
        {
            if (null == client.ClientData.LayerRewardData || client.ClientData.LayerRewardData.WanMoTaLayerRewardList.Count != 1)
            {
                return -1;
            }

            int nExp = 0;
            int nMoney = 0;
            int nXinHun = 0;
            List<GoodsData> rewardList = null;
            lock (client.ClientData.LayerRewardData)
            {
                nExp = client.ClientData.LayerRewardData.WanMoTaLayerRewardList[0].nExp;
                nMoney = client.ClientData.LayerRewardData.WanMoTaLayerRewardList[0].nMoney;
                nXinHun = client.ClientData.LayerRewardData.WanMoTaLayerRewardList[0].nXinHun;
                rewardList = client.ClientData.LayerRewardData.WanMoTaLayerRewardList[0].sweepAwardGoodsList;

                client.ClientData.LayerRewardData.WanMoTaLayerRewardList.Clear();
            }

            WanMotaCopySceneManager.AddRewardToClient(client, rewardList, nExp, nMoney, nXinHun, "万魔塔扫荡奖励");
            return 0;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            int nID = (int)TCPGameServerCmds.CMD_SPR_GET_SWEEP_REWARD;
            int nRoleID = Global.SafeConvertToInt32(cmdParams[0]);

            string strCmd = "";
            if (0 != client.ClientData.WanMoTaProp.nSweepLayer)
            {
                strCmd = string.Format("{0}:{1}", -1, nRoleID);
                client.sendCmd(nID, strCmd);
                return true;
            }
            else
            {
                if (-1 == WanMoTaDBCommandManager.UpdateSweepAwardDBCommand(client, -1))
                {
                    strCmd = string.Format("{0}:{1}", -1, nRoleID);
                    client.sendCmd(nID, strCmd);
                    return true;
                }
                else
                {
                    client.ClientData.WanMoTaProp.nSweepLayer = -1;
                    GiveSweepReward(client);

                    strCmd = string.Format("{0}:{1}", 0, nRoleID);
                    client.sendCmd(nID, strCmd);
                    return true;
                }
            }
        }
    }
}
