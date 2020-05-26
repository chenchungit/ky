using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using GameServer.Logic.JingJiChang;
using Server.Tools;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 竞技场消除冷却时间指令处理器
    /// </summary>
    public class JingJiRemoveCDCmdProcessor : ICmdProcessor
    {
        private static JingJiRemoveCDCmdProcessor instance = new JingJiRemoveCDCmdProcessor();

        private JingJiRemoveCDCmdProcessor() { }

        public static JingJiRemoveCDCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {

            int result = JingJiChangManager.getInstance().removeCD(client);

            client.sendCmd<int>((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_REMOVE_CD, result);
            return true;
        }
    }
}
