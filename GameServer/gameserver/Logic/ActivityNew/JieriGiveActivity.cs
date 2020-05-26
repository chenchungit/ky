using GameServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.ActivityNew
{
    public class JieriGiveActivity : JieriGiveRecv_Base
    {
        public override string GetConfigFile()
        {
            return "Config/JieRiGifts/JieRiZengSong.xml";
        }

        public override string QueryActInfo(GameClient client)
        {
            if ((!InActivityTime() && !InAwardTime())
                || client == null)
            {
                return "0:0:0";
            }

            RoleGiveRecvInfo info = GetRoleGiveRecvInfo(client.ClientData.RoleID);
            lock (info)
            {
                return string.Format("{0}:{1}:{2}", info.TotalGive, info.TotalRecv, info.AwardFlag);
            }
        }

        public override void FlushIcon(GameClient client)
        {
            if (client != null && client._IconStateMgr.CheckJieriGive(client))
            {
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        public override bool IsReachConition(RoleGiveRecvInfo info, int condValue)
        {
            if (info == null) return false;

            return info.TotalGive >= condValue;
        }

        // 客户端请求赠送物品给对方 return `ec:totalgive:totalrecv:awardflag`
        public string ProcRoleGiveToOther(GameClient client, string receiverRolename, int goodsID, int goodsCnt)
        {
            int receiverRoleid = -1;
            JieriGiveErrorCode ec = JieriGiveErrorCode.Success;
            do
            {
                if (!InActivityTime())
                {
                    ec = JieriGiveErrorCode.ActivityNotOpen;
                    break;
                }

                if (string.IsNullOrEmpty(receiverRolename) || receiverRolename == client.ClientData.RoleName)
                {
                    ec = JieriGiveErrorCode.ReceiverCannotSelf;
                    break;
                }

                if (!IsGiveGoodsID(goodsID))
                {
                    ec = JieriGiveErrorCode.GoodsIDError;
                    break;
                }

                if (goodsCnt <= 0 || Global.GetTotalGoodsCountByID(client, goodsID) < goodsCnt)
                {
                    ec = JieriGiveErrorCode.GoodsNotEnough;
                    break;
                }

                string dbReq = string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, receiverRolename, goodsID, goodsCnt);
                string[] dbRsp = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_ROLE_JIERI_GIVE_TO_OTHER, dbReq, client.ServerId);
                if (dbRsp == null || dbRsp.Length != 1)
                {
                    ec = JieriGiveErrorCode.DBFailed;
                    break;
                }

                receiverRoleid = Convert.ToInt32(dbRsp[0]);
                if (receiverRoleid == -1)
                {
                    ec = JieriGiveErrorCode.ReceiverNotExist;
                    break;
                }
                else if (receiverRoleid <= 0)
                {
                    ec = JieriGiveErrorCode.DBFailed;
                    break;
                }

                bool bUsedBinding_just_placeholder = false, bUsedTimeLimited_just_placeholder = false;
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                     Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, goodsID, goodsCnt, false, out bUsedBinding_just_placeholder, out bUsedTimeLimited_just_placeholder))
                {
                    ec = JieriGiveErrorCode.GoodsNotEnough;
                    break;
                }

                ec = JieriGiveErrorCode.Success;
            } while (false);

            RoleGiveRecvInfo info = GetRoleGiveRecvInfo(client.ClientData.RoleID);
            if (ec == JieriGiveErrorCode.Success)
            {
                // 增加自己的赠送数量
                lock (info)
                {
                    info.TotalGive += goodsCnt;
                }

                // 检测自己是否需要刷新节日赠送图标
                if (client._IconStateMgr.CheckJieriGive(client))
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                    client._IconStateMgr.SendIconStateToClient(client);
                }

                // 触发节日赠送王活动
                JieRiGiveKingActivity gkActivity = HuodongCachingMgr.GetJieriGiveKingActivity();
                if (gkActivity != null)
                {
                    gkActivity.OnGive(client, goodsID, goodsCnt);
                }

                /*
                // 增加对方的接收数量
                RoleGiveRecvInfo otherInfo = GetRoleGiveRecvInfo(receiverRoleid);
                lock (otherInfo)
                {
                    otherInfo.TotalRecv += goodsCnt;
                }*/

                // 触发节日收取活动
                JieriRecvActivity recvAct = HuodongCachingMgr.GetJieriRecvActivity();
                if (recvAct != null)
                {
                    recvAct.OnRecv(receiverRoleid, goodsCnt);
                }

                // 触发节日收取王活动
                JieRiRecvKingActivity rkActivity = HuodongCachingMgr.GetJieriRecvKingActivity();
                if (rkActivity != null)
                {
                    rkActivity.OnRecv(receiverRoleid, goodsID, goodsCnt, client.ServerId);
                }

                //根据瑞祥需求，赠送后，直接扣除，并不把物品给对方
            }

            lock (info)
            {
                return string.Format("{0}:{1}:{2}:{3}", (int)ec, info.TotalGive, info.TotalRecv, info.AwardFlag);
            }
        }
    }
}
