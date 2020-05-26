using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using GameServer.Logic.JingJiChang;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 竞技场领取Buff
    /// </summary>
    public class JingJiGetBuffCmdProcessor : ICmdProcessor
    {
        private static JingJiGetBuffCmdProcessor instance = new JingJiGetBuffCmdProcessor();

        private JingJiGetBuffCmdProcessor() { }

        public static JingJiGetBuffCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            int _replace = Convert.ToInt32(cmdParams[1]);

            int result;

            if (_replace != 0 && _replace != 1)
            {
                result = ResultCode.Illegal;
            }
            else
            {
                result = JingJiChangManager.getInstance().activeJunXianBuff(client, _replace == 1 ? true : false);
            }

            client.sendCmd<int>((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_GET_BUFF, result);
            return true;
            
        }
    }
}
