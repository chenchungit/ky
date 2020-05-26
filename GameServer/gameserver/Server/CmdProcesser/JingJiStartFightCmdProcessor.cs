using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.JingJiChang;

using Server.Data;
using GameServer.Logic;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 竞技场请求挑战指令处理器
    /// </summary>
    public class JingJiStartFightCmdProcessor : ICmdProcessor
    {
        private static JingJiStartFightCmdProcessor instance = new JingJiStartFightCmdProcessor();

        private JingJiStartFightCmdProcessor() { }

        public static JingJiStartFightCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            int nID = (int)TCPGameServerCmds.CMD_SPR_JINGJI_START_FIGHT;
            int nRoleID = Global.SafeConvertToInt32(cmdParams[0]);

            string strCmd = "";
            if (-1 == JingJiChangManager.getInstance().JingJiChangStartFight(client))
            {
                strCmd = string.Format("{0}:{1}", -1, nRoleID);
                client.sendCmd(nID, strCmd);
                return true;
            }
            else
            {
                strCmd = string.Format("{0}:{1}", 0, nRoleID);
                client.sendCmd(nID, strCmd);
                return true;
            }
        }
    }
}
