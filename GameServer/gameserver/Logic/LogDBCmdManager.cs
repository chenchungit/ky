using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Server.TCP;
using Server.Tools;
using Server.Protocol;
using GameServer.Server;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 数据库命令队列管理
    /// </summary>
    public class LogDBCmdManager
    {
        /// <summary>
        /// 数据库命令池
        /// </summary>
        private DBCmdPool _DBCmdPool = new DBCmdPool(2000);

        /// <summary>
        /// 等待处理的数据库命令队列
        /// </summary>
        private Queue<DBCommand> _DBCmdQueue = new Queue<DBCommand>(2000);

        /// <summary>
        /// 添加一个新的数据库命令到队列中
        /// </summary>
        /// <param name="cmdID"></param>
        /// <param name="cmdText"></param>
        /// [bing] 2015.3.19 参数改造 log 增加一个属性操作后剩余值记录
        public void AddDBLogInfo(int nGoodDBID, string strObjName, string strFrom, string strCurrEnvName, string strTarEnvName, string strOptType, int nAmount, int nZoneID, string userid, int nSurplus, int serverId, GoodsData goodsData = null)
        {
            if ("" == strObjName)
            {
                return;
            }

            AddGameDBLogInfo(nGoodDBID, strObjName, strFrom, strCurrEnvName, strTarEnvName, strOptType, nAmount, nZoneID, userid, nSurplus, serverId);

            //是否禁用交易市场购买功能
            int disableDBLog = GameManager.GameConfigMgr.GetGameConfigItemInt("disable-dblog", 0);
            if (disableDBLog > 0)
            {
                return;
            }

            string extData = "";
            if (null != goodsData)
            {
                extData = string.Format("{0}|{1}|{2}", goodsData.ExcellenceInfo, goodsData.Forge_level, goodsData.AppendPropLev);
            }

            strFrom = strFrom.Replace(':', '-');
            String strLogInfo = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}", nGoodDBID, strObjName, strFrom, strCurrEnvName, strTarEnvName, strOptType, nAmount, nZoneID, nSurplus, extData);
            AddDBCmd((int)TCPGameServerCmds.CMD_LOGDB_ADD_ITEM_LOG, strLogInfo, null, serverId);
        }

        /// <summary>
        /// 游戏服务器记录的消费日志
        /// </summary>
        /// <param name="nGoodDBID"></param>
        /// <param name="strObjName"></param>
        /// <param name="strFrom"></param>
        /// <param name="strCurrEnvName"></param>
        /// <param name="strTarEnvName"></param>
        /// <param name="strOptType"></param>
        /// <param name="nAmount"></param>
        /// <param name="nZoneID"></param>
        /// <param name="userid"></param>
        public void AddGameDBLogInfo(int nGoodDBID, string strObjName, string strFrom, string strCurrEnvName, string strTarEnvName, string strOptType, int nAmount, int nZoneID, string userid, int nSurplus, int serverId)
        {
            /**/if ("钻石" != strObjName)
            {
                return;
            }

            strFrom = strFrom.Replace(':', '-');
            string strLogInfo = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}", nGoodDBID, strObjName, strFrom, strCurrEnvName, strTarEnvName, strOptType, nAmount, nZoneID, userid, nSurplus);
            Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_LOGDB_ADD_ITEM_LOG, strLogInfo, serverId);
        }

        /// <summary>
        /// 记录玩家的大笔交易的日志
        /// </summary>
        public void AddTradeNumberInfo(int type, int money, int roleid1, int roleid2, int serverId = GameManager.LocalServerIdForNotImplement)
        {
            string today = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");
            string strLogInfo = String.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)type, money, DataHelper.ConvertToTicks(today), GameManager.ServerLineID, CombClientInfo(roleid1, serverId), CombClientInfo(roleid2, serverId));
            AddDBCmd((int)TCPGameServerCmds.CMD_LOGDB_ADD_TRADEMONEY_NUM_LOG, strLogInfo, null, serverId);
        }

        /// <summary>
        /// 记录玩家交易频繁的日志
        /// </summary>
        public void AddTradeFreqInfo(int type, int count, int roleid, int serverId = GameManager.LocalServerIdForNotImplement)
        {
            string today = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");
            string strLogInfo = String.Format("{0}:{1}:{2}:{3}:{4}", type, count, DataHelper.ConvertToTicks(today), GameManager.ServerLineID, CombClientInfo(roleid, serverId));
            AddDBCmd((int)TCPGameServerCmds.CMD_LOGDB_ADD_TRADEMONEY_FREQ_LOG, strLogInfo, null, serverId);
        }

        public string CombClientInfo(int roleid, int serverId)
        {
            // "userid:roleid:rname:inputmoney:usedmoney:currmoney:online:level:regtime:ip
            string result = "";

            // 去db查询相关信息
            string dbcmd = string.Format("{0}", roleid);
            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_QUERYROLEMONEYINFO, dbcmd, serverId);

            if (null == fields || fields.Length != 9)
            {
                GameClient client = GameManager.ClientMgr.FindClient(roleid);
                if (null == client)
                {
                    result = "-1:-1:-1:-1:-1:-1:-1:-1:-1:-1";
                }
                else
                {
                    int TotalOnlineSecs = client.ClientData.TotalOnlineSecs;
                    result = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}", client.strUserID, roleid, client.ClientData.RoleName, -1, -1, client.ClientData.UserMoney, TotalOnlineSecs, client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level, client.ClientData.RegTime, Global.GetSocketRemoteIP(client));
                }
            }
            else
            {
                GameClient client = GameManager.ClientMgr.FindClient(roleid);

                int TotalOnlineSecs = (null != client) ? client.ClientData.TotalOnlineSecs : Convert.ToInt32(fields[6]);
                result = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}", fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], TotalOnlineSecs, fields[7], fields[8], (null != client) ? Global.GetSocketRemoteIP(client) : "");
            }
            return result;
        }

        /// <summary>
        /// 添加一个新的数据库命令到队列中
        /// </summary>
        /// <param name="cmdID"></param>
        /// <param name="cmdText"></param>
        private void AddDBCmd(int cmdID, string cmdText, DBCommandEventHandler dbCommandEvent, int serverId)
        {
            DBCommand dbCmd = _DBCmdPool.Pop();
            if (null == dbCmd)
            {
                dbCmd = new DBCommand();
            }

            dbCmd.DBCommandID = cmdID;
            dbCmd.DBCommandText = cmdText;
            dbCmd.ServerId = serverId;
            if (null != dbCommandEvent)
            {
                dbCmd.DBCommandEvent += dbCommandEvent;
            }

            lock (_DBCmdQueue)
            {
                _DBCmdQueue.Enqueue(dbCmd);
            }
        }

        /// <summary>
        /// 获取等待处理的DBCmd数量个数
        /// </summary>
        /// <returns></returns>
        public int GetDBCmdCount()
        {
            lock (_DBCmdQueue)
            {
                return _DBCmdQueue.Count;
            }
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="dbCmd"></param>
        /// <returns></returns>
        private TCPProcessCmdResults DoDBCmd(TCPClientPool tcpClientPool, TCPOutPacketPool pool, DBCommand dbCmd, out byte[] bytesData)
        {
            bytesData = Global.SendAndRecvData(dbCmd.DBCommandID, dbCmd.DBCommandText, dbCmd.ServerId, 1);
            if (null == bytesData || bytesData.Length <= 0)
            {
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            return TCPProcessCmdResults.RESULT_OK;
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        public void ExecuteDBCmd(TCPClientPool tcpClientPool, TCPOutPacketPool pool)
        {
            //int nTestCount = 2000;
            //for (int i = 0; i < nTestCount; i++)
            //{
            //    AddDBLogInfo(123, "0000000", "111111111", "22222222222222", "增加", 1, 1);
            //}

            lock (_DBCmdQueue)
            {
                if (_DBCmdQueue.Count <= 0) return;
            }

            List<DBCommand> dbCmdList = new List<DBCommand>();
            lock (_DBCmdQueue)
            {
                while (_DBCmdQueue.Count > 0)
                {
                    dbCmdList.Add(_DBCmdQueue.Dequeue());
                }
            }
           
            byte[] bytesData = null;
            TCPProcessCmdResults result;
            //long ticks = TimeUtil.NOW();
            for (int i = 0; i < dbCmdList.Count; i++)
            {
                result = DoDBCmd(tcpClientPool, pool, dbCmdList[i], out bytesData);
                if (result == TCPProcessCmdResults.RESULT_FAILED)
                {
                    //写日志
                    LogManager.WriteLog(LogTypes.Error, string.Format("向LogDBServer请求执行命令失败, CMD={0}", (TCPGameServerCmds)dbCmdList[i].DBCommandID));
                }               

                //还回队列
                _DBCmdPool.Push(dbCmdList[i]);
            }

            //SysConOut.WriteLine(string.Format("发送{0}条日志到数据库耗时 {1}", nTestCount, TimeUtil.NOW() - ticks));
        }
    }
}
