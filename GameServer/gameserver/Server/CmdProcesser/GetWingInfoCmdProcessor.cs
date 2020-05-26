using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using GameServer.Logic.MUWings;
using Server.Protocol;
using Server.Data;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 翅膀佩戴/下载指令
    /// </summary>
    public class GetWingInfoCmdProcessor : ICmdProcessor
    {
        private static GetWingInfoCmdProcessor instance = new GetWingInfoCmdProcessor();

        private GetWingInfoCmdProcessor()
        {
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_GETWINGINFO, 1, this);
        }

        public static GetWingInfoCmdProcessor getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 处理翅膀佩戴/下载指令
        /// </summary>
        public bool processCmd(GameClient client, string[] cmdParams)
        {
            int nID = (int)TCPGameServerCmds.CMD_SPR_GETWINGINFO;
            int roleID = Global.SafeConvertToInt32(cmdParams[0]);

            string strcmd = "";
            if (null == client.ClientData.MyWingData)
            {
                WingData wingData = new WingData();
                client.sendCmd<WingData>(nID, wingData);
                return true;
            }
            else
            {
                client.sendCmd<WingData>(nID, client.ClientData.MyWingData);
                return true;
            }            
            
            return true;
        }
    }
}
