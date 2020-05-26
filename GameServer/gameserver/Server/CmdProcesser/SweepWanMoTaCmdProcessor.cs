using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.MUWings;
using GameServer.Logic;
using GameServer.Logic.WanMota;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 翅膀进阶指令
    /// </summary>
    public class SweepWanMoTaCmdProcessor : ICmdProcessor
    {
        private static SweepWanMoTaCmdProcessor instance = new SweepWanMoTaCmdProcessor();

        private SweepWanMoTaCmdProcessor() 
        {
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_SWEEP_WANMOTA, 2, this);
        }

        public static SweepWanMoTaCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            int nID = (int)TCPGameServerCmds.CMD_SPR_SWEEP_WANMOTA;
            int nRoleID = Global.SafeConvertToInt32(cmdParams[0]);
            int nSweepBeginOrder = Global.SafeConvertToInt32(cmdParams[1]);

            string strCmd = "";

            // 扫荡需要的最小挑战通关层数
            if (client.ClientData.WanMoTaProp.nPassLayerCount < SweepWanMotaManager.nSweepReqMinLayerOrder)
            {
                strCmd = string.Format("{0}:{1}", -2, nRoleID);
                client.sendCmd(nID, strCmd);
                return true;
            }            

            // 扫荡已完成，需要领取奖励
            if (0 == client.ClientData.WanMoTaProp.nSweepLayer)
            {
                strCmd = string.Format("{0}:{1}", -4, nRoleID);
                client.sendCmd(nID, strCmd);
                return true;
            }

            //// 检测要扫荡的层是否与服务器一致
            //if (client.ClientData.WanMoTaProp.nSweepLayer != nSweepBeginOrder)
            //{
            //    strCmd = string.Format("{0}:{1}", -3, nRoleID);
            //    client.sendCmd(nID, strCmd);
            //    return true;
            //}

            //// 检测是否还有扫荡次数
            //if (client.ClientData.WanMoTaProp.nSweepLayer != nSweepBeginOrder)
            //{
            //    strCmd = string.Format("{0}:{1}", -3, nRoleID);
            //    client.sendCmd(nID, strCmd);
            //    return true;
            //}

            // 继续扫荡
            if (client.ClientData.WanMoTaProp.nSweepLayer > 0)
            {
                SweepWanMotaManager.SweepContinue(client);
            }
            // 重新开始扫荡
            else
            {
                // 扫荡次数是否用完
                if (SweepWanMotaManager.GetSweepCount(client) >= SweepWanMotaManager.nWanMoTaMaxSweepNum)
                {
                    strCmd = string.Format("{0}:{1}", -1, nRoleID);
                    client.sendCmd(nID, strCmd);
                    return true;
                }

                SweepWanMotaManager.SweepBegin(client);
            }

            strCmd = string.Format("{0}:{1}", 0, nRoleID);
            client.sendCmd(nID, strCmd);
            return true;
        }
    }
}
