using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Logic.JingJiChang;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 竞技场详情指令处理器
    /// </summary>
    public class JingJiDetailCmdProcessor : ICmdProcessor
    {
        private static JingJiDetailCmdProcessor instance = new JingJiDetailCmdProcessor();

        private JingJiDetailCmdProcessor() { }

        public static JingJiDetailCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            int type = Convert.ToInt32(cmdParams[1]);

            JingJiDetailData data = JingJiChangManager.getInstance().getDetailData(client, type);

            client.sendCmd<JingJiDetailData>((int)TCPGameServerCmds.CMD_SPR_JINGJI_DETAIL, data);
            return true;
        }
    }
}
