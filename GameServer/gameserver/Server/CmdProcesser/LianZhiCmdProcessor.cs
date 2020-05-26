using System;
using GameServer.Logic;
using GameServer.Logic.MUWings;
using Server.Data;
using System.Collections.Generic;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 炼制系统
    /// </summary>
    public class LianZhiCmdProcessor : ICmdProcessor
    {
        #region 通用接口

        private TCPGameServerCmds CmdID = TCPGameServerCmds.CMD_SPR_EXEC_LIANZHI;

        public LianZhiCmdProcessor(TCPGameServerCmds cmdID)
        {
            CmdID = cmdID;
        }

        public static LianZhiCmdProcessor getInstance(TCPGameServerCmds cmdID)
        {
            return new LianZhiCmdProcessor(cmdID);
        }

        /// <summary>
        /// 炼制系统命令处理
        /// </summary>
        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            int nID = (int)CmdID;

            if (CmdID == TCPGameServerCmds.CMD_SPR_EXEC_LIANZHI)
            {
                int type = Global.SafeConvertToInt32(cmdParams[1]); //炼制类型: 0 金币,1 绑钻,2钻石
                int count = Global.SafeConvertToInt32(cmdParams[2]); //炼制次数: 非正数表示全部剩余次数
                return LianZhiManager.GetInstance().ExecLianZhi(client, type, count);
            }
            else if (CmdID == TCPGameServerCmds.CMD_SPR_QUERY_LIANZHICOUNT)
            {
                return LianZhiManager.GetInstance().QueryLianZhiCount(client);
            }

            return false;
        }

        #endregion 通用接口
    }
}
