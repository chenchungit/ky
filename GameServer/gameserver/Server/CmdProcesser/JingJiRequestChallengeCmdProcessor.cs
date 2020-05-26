using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.JingJiChang;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 竞技场请求挑战指令处理器
    /// </summary>
    public class JingJiRequestChallengeCmdProcessor : ICmdProcessor
    {
        private static JingJiRequestChallengeCmdProcessor instance = new JingJiRequestChallengeCmdProcessor();

        private JingJiRequestChallengeCmdProcessor() { }

        public static JingJiRequestChallengeCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            int roleId = Convert.ToInt32(cmdParams[0]);

            //被挑战者ID
            int beChallengerId = Convert.ToInt32(cmdParams[1]);

            //被挑战者排名
            int beChallengeRanking = Convert.ToInt32(cmdParams[2]);

            //挑战方式： 0：免费， 1：vip
            int enterType = Convert.ToInt32(cmdParams[3]);

            //非法参数
            int result = 0;

            if (beChallengerId < 0 || beChallengeRanking < 1 || beChallengeRanking > JingJiChangConstants.RankingListMaxNum 
            || (enterType != JingJiChangConstants.Enter_Type_Free && enterType != JingJiChangConstants.Enter_Type_Vip))
            {
                client.sendCmd<int>((int)TCPGameServerCmds.CMD_SPR_JINGJI_REQUEST_CHALLENGE, result);
                return true;
            }

            result = JingJiChangManager.getInstance().requestChallenge(client, beChallengerId, beChallengeRanking, enterType);
            
            client.sendCmd<int>((int)TCPGameServerCmds.CMD_SPR_JINGJI_REQUEST_CHALLENGE, result);
            return true;
        }
    }
}
