using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;
using GameServer.Logic;
using System.Threading;
using Server.Tools;
using System.Net.Sockets;

namespace GameServer.Server
{
    /// <summary>
    /// 发送指令管理
    /// </summary>
    public class SendCmdManager : IManager
    {
        private static SendCmdManager instance = new SendCmdManager();

        private SendCmdManager() { }

        public static SendCmdManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 指令异步执行处理器
        /// </summary>
        //public ScheduleExecutor taskExecutor = null;

        /// <summary>
        /// 加入到发送指令队列
        /// </summary>
        /// <param name="wrapper"></param>
        public void addSendCmdWrapper(SendCmdWrapper wrapper)
        {
            //TCPSession session = null;
            //if (!TCPManager.getInstance().GetTCPSessions().TryGetValue(wrapper.socket, out session))
            //{
            //    return;
            //}

            //session.addSendCmdWrapper(wrapper);
            //taskExecutor.execute(new ProcessSendCmdTask(session));
        }

        public bool initialize()
        {
            //taskExecutor = new ScheduleExecutor(20);
            return true;
        }

        public bool startup()
        {
            //taskExecutor.start();
            return true;
        }

        public bool showdown()
        {
            //taskExecutor.stop();
            return true;
        }

        public bool destroy()
        {
            //taskExecutor = null;
            return true;
        }
    }

    /// <summary>
    /// 发送指令任务
    /// </summary>
    class ProcessSendCmdTask : ScheduleTask
    {
        private TaskInternalLock _InternalLock = new TaskInternalLock();
        public TaskInternalLock InternalLock { get { return _InternalLock; } }

        private TCPSession session = null;

        public ProcessSendCmdTask(TCPSession session)
        {
            this.session = session;
        }

        public void run()
        {
            //SendCmdManager sendCmdManager = SendCmdManager.getInstance();

            ////因为业务层处理时未做同步，暂时锁会话，保证每个玩家的指令处理时线性的
            //if (Monitor.TryEnter(session.SendCmdLock))
            //{
            //    TMSKSocket socket = null;

            //    try
            //    {
            //        SendCmdWrapper wrapper = session.getNextSendCmdWrapper();
            //        if (null != wrapper)
            //        {
            //            try
            //            {
            //                socket = wrapper.socket;

            //                //缓冲数据包
            //                Global._SendBufferManager.AddOutPacket(wrapper.socket, wrapper.tcpOutPacket);

            //                //还回tcpoutpacket
            //                Global._TCPManager.TcpOutPacketPool.Push(wrapper.tcpOutPacket);
            //            }
            //            finally
            //            {
            //                wrapper.Release();
            //                wrapper = null;
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        // 格式化异常错误信息
            //        DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            //    }
            //    finally
            //    {
            //        Monitor.Exit(session.SendCmdLock);
            //    }
            //}
            //else
            //{
            //    //如果当session有指令正在处理，把当前指令重新丢进队列，延迟5毫秒处理，防止同一session占用过多线程，保证资源合理利用
            //    sendCmdManager.taskExecutor.scheduleExecute(this, 5);
            //}
        }
    }
}
