using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using Server.Data;
using Server.Tools;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 战盟建筑buffer领取
    /// </summary>
    public class ZhanMengBuildGetBufferCmdProcessor : ICmdProcessor
    {
        private static ZhanMengBuildGetBufferCmdProcessor instance = new ZhanMengBuildGetBufferCmdProcessor();

        private ZhanMengBuildGetBufferCmdProcessor() //此函数默认不会出发，必须在CmdReigsterTriggerManager中注册下
        {
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_ZHANMENGBUILDGETBUFFER, 4, this);
        }

        public static ZhanMengBuildGetBufferCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            int roleID = Global.SafeConvertToInt32(cmdParams[0]);
            int bhid = Global.SafeConvertToInt32(cmdParams[1]);
            int buildType = Global.SafeConvertToInt32(cmdParams[2]);
            int level = Global.SafeConvertToInt32(cmdParams[3]);

            int nID = (int)TCPGameServerCmds.CMD_SPR_ZHANMENGBUILDGETBUFFER;

            string strcmd = "";
            if (client.ClientData.Faction != bhid)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -1, roleID, bhid, buildType, 0);
                client.sendCmd(nID, strcmd);
                return true;
            }

            SystemXmlItem systemZhanMengBuildItem = Global.GetZhanMengBuildItem(buildType, level);
            if (null == systemZhanMengBuildItem)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -3, roleID, bhid, buildType, 0);
                client.sendCmd(nID, strcmd);
                return true;
            }

            int buffTime = systemZhanMengBuildItem.GetIntValue("BuffTime");
            int convertCost = systemZhanMengBuildItem.GetIntValue("ConvertCost");
            if (client.ClientData.BangGong < convertCost)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -1110, roleID, bhid, buildType, 0);
                client.sendCmd(nID, strcmd);
                return true;
            }

            string dbcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, bhid, buildType, convertCost, level);
            string[] fields = Global.ExecuteDBCmd(nID, dbcmd, client.ServerId);
            if (null == fields || fields.Length != 1)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("升级帮旗等级时失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(client.ClientSocket), roleID));
                return false;
            }

            int retCode = Global.SafeConvertToInt32(fields[0]);
            if (retCode > 0)
            {
                client.ClientData.BangGong -= Math.Abs(convertCost);
                installBuff(client, level - 1, buildType, buffTime);
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 1, roleID, bhid, buildType, convertCost);
            }
            else
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", retCode, roleID, bhid, buildType, 0);
            }

            client.sendCmd(nID, strcmd);
            return true;
        }

        /// <summary>
        /// 安装Buff
        /// </summary>
        /// <param name="player"></param>
        private void installBuff(GameClient client, int nNewBufferGoodsIndexID, int buildType, int secs)
        {
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.MU_ZHANMENGBUILD_ZHANQI + buildType - 1);

            //更新BufferData
            double[] actionParams = new double[2];
            actionParams[0] = (double)(secs);//持续时间
            actionParams[1] = (double)nNewBufferGoodsIndexID;
            Global.UpdateBufferData(client, (BufferItemTypes)((int)BufferItemTypes.MU_ZHANMENGBUILD_ZHANQI + buildType - 1), actionParams, 0, true);

            // 通知客户端帮贡信息变化
            GameManager.ClientMgr.NotifySelfBangGongChange(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
   
            //通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

        }
    }
}
