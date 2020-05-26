using CC;
using GameServer.Core.Executor;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.cc
{
    class CCTCPCmdHandler
    {
        /// <summary>
        /// 总共处理的指令个数
        /// </summary>
        public static long TotalHandledCmdsNum = 0;

        /// <summary>
        /// 消耗时间最长的指令ID
        /// </summary>
        public static int MaxUsedTicksCmdID = 0;

        /// <summary>
        /// 消耗时间最长的指令ID消耗的时间
        /// </summary>
        public static long MaxUsedTicksByCmdID = 0;
        /// <summary>
        /// 正在处理指令的完成端口线程统计
        /// </summary>
        private static Dictionary<TMSKSocket, int> HandlingCmdDict = new Dictionary<TMSKSocket, int>();

       
        public static void CCProcessCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count)
        {

            //接收到了完整的命令包
            //TCPOutPacket tcpOutPacket = null;
            //TCPProcessCmdResults result = TCPProcessCmdResults.RESULT_FAILED;

            //result = TCPCmdHandler.ProcessCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);

            //if (result == TCPProcessCmdResults.RESULT_DATA && null != tcpOutPacket)
            //{
            //    //向登陆客户端返回数据
            //    tcpMgr.MySocketListener.SendData(socket, tcpOutPacket);
            //}
            //else if (result == TCPProcessCmdResults.RESULT_FAILED)//解析失败, 直接关闭连接
            //{
            //    if (nID != (int)TCPGameServerCmds.CMD_LOG_OUT)
            //    {
            //        LogManager.WriteLog(LogTypes.Error, string.Format("解析并执行命令失败: {0},{1}, 关闭连接", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
            //    }

            //    //这儿需要关闭链接--->这样关闭对吗?
            //    tcpMgr.MySocketListener.CloseSocket(socket);
            //}
        }

        private static string ConvertEnumToString<T>(int itemValue)
        {
            return Enum.Parse(typeof(T), itemValue.ToString()).ToString();
        }

        public static TCPProcessCmdResults CCProcessCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {


            //if (nID > 30000)
            //    SysConOut.WriteLine("接收：" + nID + "|" + ConvertEnumToString<CC.CommandID>(nID));

            //测试用
            //System.Diagnostics.Debug.WriteLine("ProcessCmd: {0}", (TCPGameServerCmds)nID);
            long startTicks = TimeUtil.NOW();

            //加入统计
            lock (HandlingCmdDict)
            {
                HandlingCmdDict[socket] = 1;
            }
            TCPProcessCmdResults result = TCPProcessCmdResults.RESULT_FAILED;
            tcpOutPacket = null;

            //记录最后一次消息id，事件，总消息数量
            socket.session.CmdID = nID;
            socket.session.CmdTime = startTicks;

            #region 指令处理

            result = TCPCmdDispatcher.getInstance().dispathProcessor(socket, nID, data, count);

            if (result == TCPProcessCmdResults.RESULT_UNREGISTERED)
            {
                result = CMDProcess.GetInstance.AttchFun(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
            }
            #endregion
            /// 总共处理的指令个数
            TotalHandledCmdsNum++;

            //测试用
            long nowTicks = TimeUtil.NOW();
            long usedTicks = nowTicks - startTicks;
           // SysConOut.WriteLine(string.Format("ProcessCmd: {0}, ticks: {1}", (TCPGameServerCmds)nID, usedTicks));
            if (usedTicks > 0)
            {
                //LogManager.WriteLog(LogTypes.Error, string.Format("指令处理时间, CMD={0}, Client={1}, Ticks={2}",
                //(TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nowTicks - startTicks));

                if (usedTicks > MaxUsedTicksByCmdID)
                {
                    MaxUsedTicksCmdID = nID;
                    MaxUsedTicksByCmdID = usedTicks;
                }
            }

            //删除统计
            lock (HandlingCmdDict)
            {
                HandlingCmdDict.Remove(socket);
            }

            //Thread.Sleep((int)Global.GetRandomNumber(100, 250)); ///模拟卡顿的操作

            return result;
        }
    }
}