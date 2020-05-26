using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System.Windows;
using Server.Data;
using GameServer.Logic;
using GameServer.Server;

namespace GameServer.Logic
{
    /// <summary>
    /// 解释执行NPC功能脚本
    /// </summary>
    class RunNPCScripts
    {
        /// <summary>
        /// 处理NPC的的功能脚本
        /// </summary>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        public static int ProcessNPCScript(GameClient client, int scriptID, int npcID)
        {
            int errorCode = 0;
            if (Global.FilterNPCScriptByID(client, scriptID, out errorCode))
            {
                //GameManager.LuaMgr.Error(client, Global.GetLang("条件不满足"));
                return errorCode;
            }

            List<MagicActionItem> magicActionItemList = null;
            if (!GameManager.SystemMagicActionMgr.NPCScriptActionsDict.TryGetValue(scriptID, out magicActionItemList) || null == magicActionItemList)
            {
                //物品没有配置脚本
                return -3;
            }

            if (magicActionItemList.Count <= 0)
            {
                return -1;
            }

            for (int j = 0; j < magicActionItemList.Count; j++)
            {
                MagicAction.ProcessAction(client, client, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, -1, -1, 0, 1, -1, npcID);
            }

            return 0;
        }
    }
}
