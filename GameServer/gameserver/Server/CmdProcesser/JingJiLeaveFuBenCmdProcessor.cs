using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.JingJiChang;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 离开竞技场副本指令处理器
    /// </summary>
    public class JingJiLeaveFuBenCmdProcessor : ICmdProcessor
    {
        private static JingJiLeaveFuBenCmdProcessor instance = new JingJiLeaveFuBenCmdProcessor();

        private JingJiLeaveFuBenCmdProcessor() { }

        public static JingJiLeaveFuBenCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            JingJiChangManager.getInstance().onLeaveFuBenForStopCD(client);
            return true;
        }
    }
}
