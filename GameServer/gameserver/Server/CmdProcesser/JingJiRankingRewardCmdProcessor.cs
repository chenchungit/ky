using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.JingJiChang;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 领取竞技场排名奖励
    /// </summary>
    public class JingJiRankingRewardCmdProcessor:ICmdProcessor
    {
        private static JingJiRankingRewardCmdProcessor instance = new JingJiRankingRewardCmdProcessor();

        private JingJiRankingRewardCmdProcessor() { }

        public static JingJiRankingRewardCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            int result;
            long nextRewardTime;

            JingJiChangManager.getInstance().rankingReward(client, out result, out nextRewardTime);

            client.sendCmd<long[]>((int)TCPGameServerCmds.CMD_SPR_JINGJI_RANKING_REWARD, new long[]{result, nextRewardTime});
            return true;
        }
    }
}
