using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using GameServer.Logic.JingJiChang;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 升级军衔
    /// </summary>
    public class JingJiJunxianLevelupCmdProcessor : ICmdProcessor
    {
        private static JingJiJunxianLevelupCmdProcessor instance = new JingJiJunxianLevelupCmdProcessor();

        private JingJiJunxianLevelupCmdProcessor() { }

        public static JingJiJunxianLevelupCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            int result = JingJiChangManager.getInstance().upGradeJunXian(client);

            client.sendCmd<int>((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_JUNXIAN_LEVELUP, result);
            return true;
        }
    }
}
