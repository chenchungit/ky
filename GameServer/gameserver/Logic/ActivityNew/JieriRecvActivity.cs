using GameServer.Server;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic.ActivityNew
{
    public class JieriRecvActivity : JieriGiveRecv_Base
    {
        public override string GetConfigFile()
        {
            return  "Config/JieRiGifts/JieRiShouQu.xml";
        }

        public override string QueryActInfo(GameClient client)
        {
            if ((!InActivityTime() && !InAwardTime())
              || client == null)
            {
                return "0:0";
            }

            RoleGiveRecvInfo info = GetRoleGiveRecvInfo(client.ClientData.RoleID);
            lock (info)
            {
                return string.Format("{0}:{1}",  info.TotalRecv, info.AwardFlag);
            }
        }

        public override void FlushIcon(GameClient client)
        {
            if (client != null && client._IconStateMgr.CheckJieriRecv(client))
            {
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        public override bool IsReachConition(RoleGiveRecvInfo info, int condValue)
        {
            if (info == null) return false;

            return info.TotalRecv >= condValue;
        }

        public void OnRecv(int roleid, int goodsCnt)
        {
            if (!InActivityTime()) return;

            bool bLoadFromDb;
            RoleGiveRecvInfo info = GetRoleGiveRecvInfo(roleid, out bLoadFromDb);
            if (info == null) return;

            // 从数据库加载的时候，已经把刚才赠送的信息给加上了
            if (!bLoadFromDb)
            {
                lock (info)
                {
                    info.TotalRecv += goodsCnt;
                }
            }

            GameClient client = GameManager.ClientMgr.FindClient(roleid);
            if (client != null)
            {
                if (client._IconStateMgr.CheckJieriRecv(client))
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                    client._IconStateMgr.SendIconStateToClient(client);
                }
            }
        }
    }
}
