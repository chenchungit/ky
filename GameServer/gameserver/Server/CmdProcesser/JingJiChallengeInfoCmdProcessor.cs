using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Logic;
using Server.Tools;
using GameServer.Logic.JingJiChang;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 竞技场战报
    /// </summary>
    public class JingJiChallengeInfoCmdProcessor : ICmdProcessor
    {
        private static JingJiChallengeInfoCmdProcessor instance = new JingJiChallengeInfoCmdProcessor();

        private JingJiChallengeInfoCmdProcessor() { }

        public static JingJiChallengeInfoCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            //战斗时不允许请求
            //if (!JingJiChangManager.getInstance().checkAction(client))
            //{
            //    return true;
            //}

            //死亡时不允许请求
            if (client.ClientData.CurrentLifeV <= 0 || client.ClientData.CurrentAction == (int)GActions.Death)
            {
                return true;
            }

            int pageIndex = Convert.ToInt32(cmdParams[1]);

            int roleId = client.ClientData.RoleID;

            List<JingJiChallengeInfoData> dataList = Global.sendToDB<List<JingJiChallengeInfoData>, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_ZHANBAO_DATA, DataHelper.ObjectToBytes<int[]>(new int[] { roleId, pageIndex }), client.ServerId);

            client.sendCmd<List<JingJiChallengeInfoData>>((int)TCPGameServerCmds.CMD_SPR_JINGJI_CHALLENGEINFO, dataList);

            return true;
        }
    }
}
