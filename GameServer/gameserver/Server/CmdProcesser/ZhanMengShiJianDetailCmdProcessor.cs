using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using GameServer.Logic.BangHui.ZhanMengShiJian;
using GameServer.Logic.JingJiChang;
using Server.Tools;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 战盟事件详情
    /// </summary>
    public class ZhanMengShiJianDetailCmdProcessor :ICmdProcessor
    {

        private static ZhanMengShiJianDetailCmdProcessor instance = new ZhanMengShiJianDetailCmdProcessor();

        private ZhanMengShiJianDetailCmdProcessor() { }

        public static ZhanMengShiJianDetailCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            int bhId = client.ClientData.Faction;
            int pageIndex = Convert.ToInt32(cmdParams[1]);

            byte[] cmd = DataHelper.ObjectToBytes<int[]>(new int[] { bhId, pageIndex });

            //向DB请求查询数据
            List<ZhanMengShiJianData> dataList = Global.sendToDB<List<ZhanMengShiJianData>, byte[]>((int)TCPGameServerCmds.CMD_DB_ZHANMENGSHIJIAN_DETAIL, cmd, client.ServerId);

            client.sendCmd<List<ZhanMengShiJianData>>((int)TCPGameServerCmds.CMD_SPR_ZHANMENGSHIJIAN_DETAIL, dataList);
            return true;
        }
    }
}
