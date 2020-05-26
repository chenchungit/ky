using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using GameServer.Logic.MUWings;
using Server.Protocol;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 翅膀佩戴/下载指令
    /// </summary>
    public class WingOffOnCmdProcessor : ICmdProcessor
    {
        private static WingOffOnCmdProcessor instance = new WingOffOnCmdProcessor();

        private WingOffOnCmdProcessor() 
        {
            WingStarCacheManager.LoadWingStarItems();
            WingPropsCacheManager.LoadWingPropsItems();
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_WINGOFFON, 1, this);
        }

        public static WingOffOnCmdProcessor getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 处理翅膀佩戴/下载指令
        /// </summary>
        public bool processCmd(GameClient client, string[] cmdParams)
        {
            int nID = (int)TCPGameServerCmds.CMD_SPR_WINGOFFON;
            int roleID = Global.SafeConvertToInt32(cmdParams[0]);

            string strcmd = "";
            if (null == client.ClientData.MyWingData)
            {
                strcmd = string.Format("{0}:{1}", -3, roleID);
                client.sendCmd(nID, strcmd);
                return true;
            }

            // 佩戴状态自动取反
            int iRet = MUWingsManager.WingOnOffDBCommand(client, client.ClientData.MyWingData.DbID, client.ClientData.MyWingData.Using == 0 ? 1 : 0);
            if (iRet < 0)
            {
                strcmd = string.Format("{0}:{1}", -3, roleID);
                client.sendCmd(nID, strcmd);
            }
            else
            {
                // 佩戴/卸下成功
                strcmd = string.Format("{0}:{1}", 0, roleID);
                client.sendCmd(nID, strcmd);

                client.ClientData.MyWingData.Using = client.ClientData.MyWingData.Using == 0 ? 1 : 0;

                // 佩戴/卸下后，更新翅膀对玩家数据的影响
                MUWingsManager.UpdateWingDataProps(client, client.ClientData.MyWingData.Using == 1);
                LingYuManager.UpdateLingYuProps(client);
                ZhuLingZhuHunManager.UpdateZhuLingZhuHunProps(client);

                // 通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                //通知其他人翅膀信息
                GameManager.ClientMgr.NotifyOthersChangeEquip(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, null, 1, client.ClientData.MyWingData);

                //[bing] 刷新客户端活动叹号
                if (client._IconStateMgr.CheckJieRiFanLi(client, ActivityTypes.JieriWing))
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                    client._IconStateMgr.SendIconStateToClient(client);
                }
            }
            
            return true;
        }
    }
}
