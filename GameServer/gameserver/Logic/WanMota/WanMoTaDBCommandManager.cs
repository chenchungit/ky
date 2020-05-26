using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GameServer.Core.Executor;
using Server.Protocol;
using GameServer.Server;
using Server.Tools;
using Server.Data;

namespace GameServer.Logic.WanMota
{
    /// <summary>
    /// 万魔塔数据库操作
    /// </summary>
    public class WanMoTaDBCommandManager
    {
        /// <summary>
        /// 保存万魔塔通关层数到万魔塔数据库表
        /// </summary>
        public static int LayerChangeDBCommand(GameClient client, int nLayerCount)
        {
            long lFlushTime = TimeUtil.NOW();
            
            //roleID, flushTime, passLayerCount, sweepLayer, sweepReward, sweepBeginTime,
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", client.ClientData.RoleID, lFlushTime, nLayerCount, "*", "*", "*");
            ModifyWanMotaData modifyData = new ModifyWanMotaData()
            {
                strParams = strcmd,
                strSweepReward = "*",
            };

            string[] fields = Global.SendToDB<ModifyWanMotaData>((int)TCPGameServerCmds.CMD_DB_MODIFY_WANMOTA, modifyData, client.ServerId);
            if (null == fields || fields.Length != 2)
            {
                return -1;
            }

            return Convert.ToInt32(fields[1]);
        }

        /// <summary>
        /// 扫荡开始时操作万魔塔数据库表
        /// </summary>
        public static int SweepBeginDBCommand(GameClient client, int nLayerCount)
        {
            long lBeginTime = TimeUtil.NOW();

            //roleID, flushTime, passLayerCount, sweepLayer, sweepReward, sweepBeginTime,
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", client.ClientData.RoleID, "*", "*", "1", "", lBeginTime);
            ModifyWanMotaData modifyData = new ModifyWanMotaData()
            {
                strParams = strcmd,
                strSweepReward = "*",
            };

            string[] fields = Global.SendToDB<ModifyWanMotaData>((int)TCPGameServerCmds.CMD_DB_MODIFY_WANMOTA, modifyData, client.ServerId);
            if (null == fields || fields.Length != 2)
            {
                return -1;
            }

            return Convert.ToInt32(fields[1]);
        }

        /// <summary>
        /// 更新扫荡层号与相应层的扫荡奖励
        /// </summary>
        public static int UpdateSweepAwardDBCommand(GameClient client, int nSweepLayerCount)
        {
            if (null == client.ClientData.LayerRewardData)
            {
                return -1;
            }

            string strLayerReward = "";
            lock (client.ClientData.LayerRewardData)
            {
                if (-1 != nSweepLayerCount)
                {
                    byte[] bytes = DataHelper.ObjectToBytes<LayerRewardData>(client.ClientData.LayerRewardData);
                    strLayerReward = Convert.ToBase64String(bytes);
                }
            }

            //roleID, flushTime, passLayerCount, sweepLayer, sweepReward, sweepBeginTime,
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", client.ClientData.RoleID, "*", "*", nSweepLayerCount, "*", "*");
            ModifyWanMotaData modifyData = new ModifyWanMotaData()
            {
                strParams = strcmd,
                strSweepReward = strLayerReward,
            };

            string[] fields = Global.SendToDB<ModifyWanMotaData>((int)TCPGameServerCmds.CMD_DB_MODIFY_WANMOTA, modifyData, client.ServerId);
            if (null == fields || fields.Length != 2)
            {
                return -1;
            }

            // 保存本地万魔塔扫荡层数
            client.ClientData.WanMoTaProp.nSweepLayer = nSweepLayerCount;
            client.ClientData.WanMoTaProp.strSweepReward = strLayerReward;
            return Convert.ToInt32(fields[1]);
        }
    }
}
