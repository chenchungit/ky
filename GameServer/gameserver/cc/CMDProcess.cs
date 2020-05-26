using System;
using System.Collections.Generic;
using GameServer.Core.Executor;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;

namespace GameServer.cc
{
    class CMDProcess
    {
        public delegate TCPProcessCmdResults FunIns(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, 
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket);
        private Dictionary<int, FunIns> m_FunInsinuate = new Dictionary<int, FunIns>();

        static CMDProcess m_CMDProcess = null;
        public static CMDProcess GetInstance
        {
            get
            {
                if (m_CMDProcess == null)
                    m_CMDProcess = new CMDProcess();
                return m_CMDProcess;
            }
        }
        public void RegisterFun(int _Key, FunIns _Fun)
        {
            m_FunInsinuate[_Key] = _Fun;
        }

        public TCPProcessCmdResults AttchFun(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            TCPProcessCmdResults result = TCPProcessCmdResults.RESULT_FAILED;
            tcpOutPacket = null;
            try
            {
                if (m_FunInsinuate.ContainsKey(nID))
                    result = m_FunInsinuate[nID](tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                else
                {
                    SysConOut.WriteLine("收到APP数据，但没找到隐射函数");
                }
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null != client )
                {
                    client.ClientData.LastClientHeartTicks = TimeUtil.NOW();
                }
                
            }
            catch(Exception ex)
            {
                SysConOut.WriteLine("AttchFun****************" + ex.ToString());
            }
            
            return result;
        }
    }
}
