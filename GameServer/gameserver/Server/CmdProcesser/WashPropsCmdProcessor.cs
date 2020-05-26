using System;
using GameServer.Logic;
using GameServer.Logic.MUWings;
using Server.Data;
using System.Collections.Generic;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 装备洗练
    /// </summary>
    public class WashPropsCmdProcessor : ICmdProcessor
    {
        private TCPGameServerCmds CmdID = TCPGameServerCmds.CMD_SPR_EXEC_WASHPROPS;

        public WashPropsCmdProcessor(TCPGameServerCmds cmdID)
        {
            CmdID = cmdID;
        }

        public static WashPropsCmdProcessor getInstance(TCPGameServerCmds cmdID)
        {
            return new WashPropsCmdProcessor(cmdID);
        }

        /// <summary>
        /// 翅膀升星处理
        /// </summary>
        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            int nID = (int)CmdID;

            if (CmdID == TCPGameServerCmds.CMD_SPR_EXEC_WASHPROPS)
            {
                int dbid = Global.SafeConvertToInt32(cmdParams[1]);
                int washIndex = Global.SafeConvertToInt32(cmdParams[2]); //-1: 洗练数值  0及以上:洗练第几条属性
                bool bUseBinding = Global.SafeConvertToInt32(cmdParams[3]) > 0;
                int moneyType = Global.SafeConvertToInt32(cmdParams[4]); 
                return WashPropsManager.WashProps(client, dbid, washIndex, bUseBinding, moneyType);
            }
            else if(CmdID == TCPGameServerCmds.CMD_SPR_EXEC_WASHPROPSINHERIT)
            {
                // 洗练传承 就是 培养传承
                int leftGoodsDBID = Global.SafeConvertToInt32(cmdParams[1]); // 源装备
                int rightGoodsDBID = Global.SafeConvertToInt32(cmdParams[2]); // 目标装备
                int moneyType = Global.SafeConvertToInt32(cmdParams[3]); //消耗钱类型
                return WashPropsManager.WashPropsInherit(client, leftGoodsDBID, rightGoodsDBID, moneyType);
            }

            return true;
        }
    }
}
