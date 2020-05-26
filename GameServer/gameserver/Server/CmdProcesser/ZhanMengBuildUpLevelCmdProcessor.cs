using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using Server.Tools;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 战盟建筑升级事件处理
    /// </summary>
    public class ZhanMengBuildUpLevelCmdProcessor : ICmdProcessor
    {
        private static ZhanMengBuildUpLevelCmdProcessor instance = new ZhanMengBuildUpLevelCmdProcessor();

        private ZhanMengBuildUpLevelCmdProcessor()  //此函数默认不会出发，必须在CmdReigsterTriggerManager中注册下
        {
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_ZHANMENGBUILDUPLEVEL, 4, this);
        }

        public static ZhanMengBuildUpLevelCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            int roleID = Global.SafeConvertToInt32(cmdParams[0]);
            int bhid = Global.SafeConvertToInt32(cmdParams[1]);
            int buildType = Global.SafeConvertToInt32(cmdParams[2]);
            int toLevel = Math.Max(2, Global.SafeConvertToInt32(cmdParams[3]));

            int nID = (int)TCPGameServerCmds.CMD_SPR_ZHANMENGBUILDUPLEVEL;

            string strcmd = "";
            if (client.ClientData.Faction != bhid)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -1, roleID, bhid, buildType, 0);
                client.sendCmd(nID, strcmd);
                return true;
            }

            if (toLevel > Global.MaxBangHuiFlagLevel)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -2, roleID, bhid, buildType, 0);
                client.sendCmd(nID, strcmd);
                return true;
            }

            SystemXmlItem systemZhanMengBuildItem = Global.GetZhanMengBuildItem(buildType, toLevel);
            if (null == systemZhanMengBuildItem)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -3, roleID, bhid, buildType, 0);
                client.sendCmd(nID, strcmd);
                return true;
            }

            int levelupCost = systemZhanMengBuildItem.GetIntValue("LevelupCost");
            String strReqGoods = systemZhanMengBuildItem.GetStringValue("NeedGoods");
            string dbcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", roleID, bhid, buildType, levelupCost, toLevel, Global.GetZhanMengInitCoin(), strReqGoods);
            string[] fields = Global.ExecuteDBCmd(nID, dbcmd, client.ServerId);
            if (null == fields || fields.Length != 1)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("升级帮旗等级时失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(client.ClientSocket), roleID));
                return false;
            }

            int retCode = Global.SafeConvertToInt32(fields[0]);
            if (retCode >= 0)
            {
                //帮旗升级的提示
                Global.BroadcastZhanMengBuildUpLevelHint(client, buildType, toLevel);
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", toLevel, roleID, bhid, buildType, levelupCost);

                //通知GameServer同步帮会的所属和范围
                JunQiManager.NotifySyncBangHuiJunQiItemsDict(client);

                GameManager.ClientMgr.NotifyBangHuiUpLevel(bhid,client.ServerId,toLevel,client.ClientSocket.IsKuaFuLogin);
            }
            else
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", retCode, roleID, bhid, buildType, 0);
            }



            client.sendCmd(nID, strcmd);
            return true;
        }
    }
}
