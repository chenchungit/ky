using CC;
using ClientTools;
using HSGameEngine.GameEngine.Network;
using HSGameEngine.GameEngine.Network.Protocol;
using ProtoBuf;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Robot
{
    class NetManager
    {

        /// <summary>
        /// 用户命令
        /// </summary>
        enum TCPServerCmds
        {
            CMD_LOGIN_ON1 = 1,
            CMD_LOGIN_ON2 = 20,

            CMD_ROLE_LIST = 101,
            CMD_CREATE_ROLE = 102,
            CMD_INIT_GAME = 104,
            CMD_PLAY_GAME = 106,
            CMD_SPR_MOVE = 107,
            CMD_NTF_EACH_ROLE_ALLOW_CHANGE_NAME = 14002
        };

        //  public AutoResetEvent autoEvent = new AutoResetEvent(false);
        public int m_Key = 0;
        public int m_ReConnectCount = 0;
        private ClientTools.TCPClient m_LNetClient = null;
        private ClientTools.TCPClient m_GSNetClient = null;
        
        private static TCPOutPacketPool m_tcpOutPacketPool = new TCPOutPacketPool(20000);

        private Thread g_Thread = null;
        private Thread g_Thread1 = null;
        private Thread g_Thread2 = null;
        private string g_Acc = null;//ClientManager.m_Account + string.Format("{0:D3}", ClientManager.m_MinNumber);
        private string g_psw = null;//ClientManager.m_Account + string.Format("{0:D3}", ClientManager.m_MinNumber);;
        private string g_RoleID = null;
        public NetManager g_NetManager = null;
        private static Semaphore  mutex= new Semaphore(1, 1);

        readonly static object m_locker = new object();
        public static Queue<TCPOutPacket> m_MoveDataList = new Queue<TCPOutPacket>();

        public bool ProcessServerCmdHandler(TCPClient client, int nID, byte[] data, int count)
        {
            try
            {

                mutex.WaitOne();


                TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_tcpOutPacketPool, data,
               0, count, nID);

                m_MoveDataList.Enqueue(szTCPOutPacket);

                mutex.Release();

            }
            catch (Exception ex)
            {
                SysConOut.WriteLine(ex.Message);
            }

            return true;
        }


        private void SocketConnectState(object sender, SocketConnectEventArgs e)
        {
            SysConOut.WriteLine("SocketReceived " + e.Error + " ErrorMsg = " + e.ErrorMsg + " ErrorStr = " + e.ErrorStr
              + " NetSocketType = " + e.NetSocketType.ToString());


        }
        public void SendLoginData()
        {
            TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_GSNetClient.OutPacketPool,
                StringUtil.substitute("{0}:{1}:{2}:{3}:{4}:{5}", "20191113", g_Acc, g_psw, 1, 1, 1),
                (int)TCPServerCmds.CMD_LOGIN_ON1);
            if (m_LNetClient.SendData(szTCPOutPacket))
                SysConOut.WriteLine("发送登陆数据成功");
            else
                SysConOut.WriteLine("发送登陆数据失败");
            m_GSNetClient.OutPacketPool.Push(szTCPOutPacket);
        }

        public void SendRoleData(string _Acc)
        {
            GetRolesList szGetRolesList = new GetRolesList();
            szGetRolesList.Acc = _Acc;
            szGetRolesList.ZoneID = 0;

            MemoryStream msResult = new MemoryStream();
            Serializer.Serialize<GetRolesList>(msResult, szGetRolesList);
            byte[] msSendData = msResult.ToArray();
            TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_GSNetClient.OutPacketPool, msSendData,0, msSendData.Length, (int)CommandID.CMD_GET_ROLE_LIST);
            if (m_GSNetClient.SendData(szTCPOutPacket))
                SysConOut.WriteLine("发送获取角色列表数据成功");
            else
                SysConOut.WriteLine("发送获取角色列表数据失败");

            //TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_tcpOutPacketPool,
            //    StringUtil.substitute("{0}:{1}", _Acc, "0"),
            //    (int)TCPServerCmds.CMD_ROLE_LIST);
            //if (m_GSNetClient.SendData(szTCPOutPacket))
            //    SysConOut.WriteLine("发送获取角色列表数据成功");
            //else
            //    SysConOut.WriteLine("发送获取角色列表数据失败");
            m_GSNetClient.OutPacketPool.Push(szTCPOutPacket);
        }
        public bool ConnectSer(string _LoginIP = "127.0.0.1", string _GameIP = "127.0.0.1", int _LPort = 4402, int _GSPort = 9929, int _key = 0)
        {

            if (!m_LNetClient.Connect(_LoginIP, _LPort))
                return false;
            SysConOut.WriteLine("连接登陆服务器成功 IP=" + _LoginIP + "   Port = " + _LPort.ToString());

            if (m_GSNetClient != null && !m_GSNetClient.Connect(_GameIP, _GSPort))
            {
                m_ReConnectCount = 0;
                m_LNetClient.Destroy();
                return false;
            }

            SysConOut.WriteLine("连接游戏服务器成功 IP=" + _GameIP + "   Port = " + _GSPort.ToString());

            m_Key = _key;
            return true;
        }

        public int getPress()
        {
            int szCount = 0;
            Process[] ps = Process.GetProcesses();

            foreach (Process p in ps)
            {
                if (p.ProcessName.Equals("Robot") || p.ProcessName.Equals("Robot.exe"))
                    szCount++;
            }
            return szCount;
        }
        public void InitClient(NetManager _NetManager)
        {
            int szCount = getPress();
            //  SysConOut.Title = "机器人--" + (ClientManager.m_MinNumber + szCount).ToString();
            g_Acc = ClientManager.m_Account + string.Format("{0:D4}", ClientManager.m_MinNumber + szCount);
            g_psw =ClientManager.m_Account + string.Format("{0:D4}", ClientManager.m_MinNumber + szCount); ;
            g_NetManager = _NetManager;
            m_LNetClient = new ClientTools.TCPClient(0);
            m_LNetClient.SocketConnect += SocketConnectState;
            TCPClient.ProcessServerCmd = this.ProcessServerCmdHandler;

            m_GSNetClient = new ClientTools.TCPClient(10);
            m_GSNetClient.SocketConnect += SocketConnectState;
            g_Thread = new Thread(new ThreadStart(ThreadFuncation));//开始一个玩游戏的线程
            g_Thread1 = new Thread(new ThreadStart(ThreadFuncation1));//开始一个玩游戏的线程
            g_Thread2 = new Thread(new ThreadStart(ThreadFuncation2));//开始一个玩游戏的线程
            g_Thread2.Start();

        }
        public void SendMove()
        {
            try
            {
                long tickTime = DateTime.Now.Ticks;
                SpriteMove szSpriteMove = new SpriteMove();
                szSpriteMove.RoleID = Convert.ToInt32(g_RoleID);
                szSpriteMove.MapCode = 1;
                szSpriteMove.action = 2;
                szSpriteMove.toX = 22500;
                szSpriteMove.toY = 32300;
                //szSpriteMove.Movec = moveData.moveCost;
                szSpriteMove.extAction = 2;
                szSpriteMove.fromX = 2840;
                szSpriteMove.fromY = 4840;
                szSpriteMove.startMoveTicks = tickTime;
                szSpriteMove.pathString = "1001(1,283,427):(1,283,427)";

                MemoryStream msResult = new MemoryStream();
                Serializer.Serialize<SpriteMove>(msResult, szSpriteMove);
                byte[] msSendData = msResult.ToArray();
                TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_GSNetClient.OutPacketPool, msSendData, 0, msSendData.Length,
                   (int)CommandID.CMD_GAME_MOVE);
                if (null == szTCPOutPacket || !m_GSNetClient.SendData(szTCPOutPacket))
                {
                    SysConOut.WriteLine("发送移动数据失败");
                    g_Thread.Abort();
                    g_Thread = null;

                }
                m_GSNetClient.OutPacketPool.Push(szTCPOutPacket);
                //m_GSNetClient.NotifyRecvData()
                msResult.Dispose();
                


            }
            catch (Exception ex)
            {
                SysConOut.WriteLine(ex.Message);
            }

        }
        public void SendMovePosition()
        {
            try
            {
                long tickTime = DateTime.Now.Ticks;
                SpriteMovePosition szSpriteMovePosition = new SpriteMovePosition();
                szSpriteMovePosition.RoleID = Convert.ToInt32(g_RoleID);
                szSpriteMovePosition.MapCode = 1001;
                szSpriteMovePosition.action = 2;
                szSpriteMovePosition.ToMapX = 22500;
                szSpriteMovePosition.ToMapY = 24300;
                //szSpriteMove.Movec = moveData.moveCost;
                szSpriteMovePosition.ToDiection = 2;
                szSpriteMovePosition.clientTicks = 1;

                MemoryStream msResult = new MemoryStream();
                Serializer.Serialize<SpriteMovePosition>(msResult, szSpriteMovePosition);
                byte[] msSendData = msResult.ToArray();
                TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_GSNetClient.OutPacketPool, msSendData, 0, msSendData.Length,
                   (int)CommandID.CMD_POSITION_MOVE);

                if (!m_GSNetClient.SendData(szTCPOutPacket))
                {
                    SysConOut.WriteLine("发送移位置数据失败");
                    g_Thread.Abort();
                    g_Thread = null;

                }
                m_GSNetClient.OutPacketPool.Push(szTCPOutPacket);

                msResult.Dispose();

                //long tickTime = DateTime.Now.Ticks;
                //var szMove = new SpriteMoveData(Convert.ToInt32(g_RoleID), 1, 2, 2520, 4600, 2, 2840, 4840, tickTime, "1(1,35,60):(1,31,57)");

                //TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_tcpOutPacketPool, szMove.toBytes(), 0, szMove.toBytes().Length,
                //    (int)TCPServerCmds.CMD_SPR_MOVE);
                //if (!m_GSNetClient.SendData(szTCPOutPacket))
                //{
                //    SysConOut.WriteLine("发送移动数据失败");
                //    g_Thread.Abort();
                //    g_Thread = null;

                //}
                //m_tcpOutPacketPool.Push(szTCPOutPacket);
            }
            catch (Exception ex)
            {
                SysConOut.WriteLine(ex.Message);
            }

        }
        public void SendPlayGame(string _RoleID)
        {
            PlayGame szPlayGame = new PlayGame();
            szPlayGame.RoleID = Convert.ToInt32(_RoleID);

            MemoryStream msResult = new MemoryStream();
            Serializer.Serialize<PlayGame>(msResult, szPlayGame);
            byte[] msSendData = msResult.ToArray();
            TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_GSNetClient.OutPacketPool, msSendData, 0, msSendData.Length, (int)CommandID.CMD_PLAY_GAME);
            if (m_GSNetClient.SendData(szTCPOutPacket))
                SysConOut.WriteLine("发送进入游戏成功");
            else
                SysConOut.WriteLine("发送进入游戏失败");
            //TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_tcpOutPacketPool, StringUtil.substitute("{0}", _RoleID),
            //    (int)TCPServerCmds.CMD_PLAY_GAME);
            //if (m_GSNetClient.SendData(szTCPOutPacket))
            //    SysConOut.WriteLine("发送进入游戏成功");
            //else
            //    SysConOut.WriteLine("发送进入游戏失败");
            m_GSNetClient.OutPacketPool.Push(szTCPOutPacket);
        }
        //public static bool SendNetObject<T>(this T obj, CommandID cmd)
        //where T : global::ProtoBuf.IExtensible
        //{
        //    int cmdID = (int)cmd;
        //    try
        //    {
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            Serializer.Serialize<T>(ms, obj);
        //            byte[] datas = ms.ToArray();//序列化结果
                    
        //            TCPOutPacket packet = TCPOutPacket.MakeTCPOutPacket(
        //                Sender.tcpOutPacketPool,
        //                datas, 0, datas.Length,
        //                cmdID
        //                );
        //            return Sender.SendData(packet);


        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.LogError(e.Message);
        //        return false;
        //    }

        //}
        public void SendInitGame(string _Acc, string _RoleID)
        {
            InitialGame szInitialGame = new InitialGame();
            szInitialGame.Acc = _Acc;
            szInitialGame.RoleID = Convert.ToInt32(_RoleID);

            MemoryStream msResult = new MemoryStream();
            Serializer.Serialize<InitialGame>(msResult, szInitialGame);
            byte[] msSendData = msResult.ToArray();
            TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_GSNetClient.OutPacketPool, msSendData, 0, msSendData.Length, (int)CommandID.CMD_INIT_GAME);
            if (m_GSNetClient.SendData(szTCPOutPacket))
                SysConOut.WriteLine("发送初始化游戏成功");
            else
                SysConOut.WriteLine("发送初始化游戏失败");
            //TCPOutPacket szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_tcpOutPacketPool,
            //    StringUtil.substitute("{0}:{1}:{2}", _Acc, _RoleID, "0"), (int)TCPServerCmds.CMD_INIT_GAME);
            //if (m_GSNetClient.SendData(szTCPOutPacket))
            //    SysConOut.WriteLine("发送初始化游戏成功");
            //else
            //{
            //    SysConOut.WriteLine("发送初始化游戏失败 连接状态 = " + m_GSNetClient.Connected.ToString());
            //}
            m_GSNetClient.OutPacketPool.Push(szTCPOutPacket);
        }
        public void ThreadFuncation1()
        {
            while (true)
            {
                if (!m_GSNetClient.Connected)
                {
                    //m_GSNetClient.Destroy();
                    //m_GSNetClient = new TCPClient(10);
                    //m_GSNetClient.SocketConnect += SocketConnectState;
                    //if (m_GSNetClient.Connect(ClientManager.m_GameIP, (int)ClientManager.m_GamePort))
                    //{
                    //    m_ReConnectCount = 0;
                    //    SysConOut.WriteLine("重连游戏服务器成功");
                    //    if (g_Thread != null)
                    //    {
                    //        g_Thread.Abort();
                    //        g_Thread = null;
                    //    }

                    //    SendRoleData(g_Acc);
                    //}
                    //else
                    //{
                    //    m_ReConnectCount++;
                    //    SysConOut.WriteLine("重连游戏服务器失败");
                    //}
                }
                Thread.Sleep(2000);
            }
        }
        public void ThreadFuncation()
        {
            //autoEvent.WaitOne();
            while (true)
            {

                g_NetManager.SendMove();

               //Thread.Sleep(2000);
                  g_NetManager.SendMovePosition();
            }
        }


        public void ThreadFuncation2()
        {
            int szOfest = 11;

            //autoEvent.WaitOne();
            while (true)
            {
              

                    if (m_MoveDataList.Count > 0)
                    {
                    mutex.WaitOne();
                    try
                        {
                        TCPOutPacket szTCPOutPacket = null;
                       
                        szTCPOutPacket = m_MoveDataList.Dequeue();
                       
                        string[] fields = null;
                        if(szTCPOutPacket.PacketCmdID <30000)
                        fields = Encoding.Default.GetString(szTCPOutPacket.GetPacketBytes(), szOfest, szTCPOutPacket.PacketDataSize - szOfest).Trim().Split(':');
                        switch (szTCPOutPacket.PacketCmdID)
                            {
                                case (int)TCPServerCmds.CMD_LOGIN_ON1:
                                    {
                                        m_LNetClient.Disconnect();
                                        m_LNetClient.Destroy();

                                        if (!fields[0].Equals("0"))
                                        {
                                            g_Thread1.Start();
                                            g_Acc = fields[0];
                                            SendRoleData(fields[0]);
                                        }
                                        else
                                            SysConOut.WriteLine("登陆失败");
                                        break;
                                    }
                                case (int)TCPServerCmds.CMD_NTF_EACH_ROLE_ALLOW_CHANGE_NAME:
                                    {
                                        SysConOut.WriteLine("==============CHANGE_NAME===============");
                                        foreach (var s in fields)
                                            SysConOut.WriteLine(s);
                                        break;
                                    }

                            case (int)CommandID.CMD_GET_ROLE_LIST:
                                {
                                    SysConOut.WriteLine("==============CMD_ROLE_LIST 30003===============");
                                    //反序列化
                                    MemoryStream msRecvData = new MemoryStream(szTCPOutPacket.GetPacketBytes(), szOfest, szTCPOutPacket.PacketDataSize - szOfest);
                                    GetRolesListReponse szGetRolesListReponse = Serializer.Deserialize<GetRolesListReponse>(msRecvData);
                                    SysConOut.WriteLine("get Role state = "+ szGetRolesListReponse.State.ToString());
                                    foreach(var i in szGetRolesListReponse.SubRoleinfo)
                                        SysConOut.WriteLine("get Role Role = " + i.RoleID.ToString());
                                    if(szGetRolesListReponse.State == (int)ErrorCode.ERROR_INVALID_ROLE)
                                    {
                                        var szRandom = new Random().Next(100);
                                        CreateRoles szCreateRoles = new CreateRoles();
                                        szCreateRoles.Acc = g_Acc;
                                        szCreateRoles.UserName = g_Acc + szRandom.ToString();
                                        szCreateRoles.Sex = 0;
                                        szCreateRoles.occup = 0;
                                        szCreateRoles.ZoneID = 0;
                                        m_tcpOutPacketPool.Push(szTCPOutPacket);

                                        MemoryStream msResult = new MemoryStream();
                                        Serializer.Serialize<CreateRoles>(msResult, szCreateRoles);
                                        byte[] msSendData = msResult.ToArray();
                                        szTCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_GSNetClient.OutPacketPool, msSendData, 0, msSendData.Length, 30002);
                                        if (m_GSNetClient.SendData(szTCPOutPacket))
                                            SysConOut.WriteLine("发送创建角色列表数据成功");
                                        else
                                            SysConOut.WriteLine("发送创建角色列表数据失败");

                                        m_GSNetClient.OutPacketPool.Push(szTCPOutPacket);


                                    }
                                    else
                                    {
                                        foreach(var Roleinfo in szGetRolesListReponse.SubRoleinfo)
                                        {
                                            g_RoleID = Roleinfo.RoleID.ToString();
                                            break;
                                        }
                                        SendInitGame(g_Acc, g_RoleID);
                                        Console.Title = "机器人---" + g_RoleID;
                                    }
                                    break;
                                }
                          
                            case (int)TCPServerCmds.CMD_ROLE_LIST:
                                {
                                    SysConOut.WriteLine("==============CMD_ROLE_LIST===============");
                                    if (fields[0].Equals("0"))
                                    {
                                        var szRandom = new Random().Next(100);
                                        var result = StringUtil.substitute("{0}:{1}:{2}:{3}:{4}:{5}:{6}", g_Acc, g_Acc + szRandom.ToString(), 0, 0,
                                            String.Format("{0}${1}", g_Acc + szRandom.ToString(), 0), 0, 1);

                                        TCPOutPacket _TCPOutPacket = TCPOutPacket.MakeTCPOutPacket(m_GSNetClient.OutPacketPool, result,
                                               (int)TCPServerCmds.CMD_CREATE_ROLE);
                                        //创建角色
                                        if (m_GSNetClient.SendData(_TCPOutPacket))
                                            SysConOut.WriteLine("发送创建角色数据成功");
                                        else
                                            SysConOut.WriteLine("发送创建角色数据失败");
                                        m_GSNetClient.OutPacketPool.Push(_TCPOutPacket);
                                    }
                                    else
                                    {
                                        string[] fieldsData = fields[1].Split('$');
                                        g_RoleID = fieldsData[0];
                                        SendInitGame(g_Acc, g_RoleID);
                                        Console.Title = "机器人---" + g_RoleID;
                                    }
                                    break;
                                }

                            case (int)CommandID.CMD_CREATE_ROLE:
                                {
                                    SysConOut.WriteLine("====CMD_CREATE_ROLE  30002=====");
                                    MemoryStream msRecvData = new MemoryStream(szTCPOutPacket.GetPacketBytes(), szOfest, szTCPOutPacket.PacketDataSize - szOfest);
                                    CreateRolesReponse szCreateRolesReponse = Serializer.Deserialize<CreateRolesReponse>(msRecvData);
                                    if(szCreateRolesReponse.State == (int)ErrorCode.ERROR_OK)
                                        SendRoleData(g_Acc);
                                    m_tcpOutPacketPool.Push(szTCPOutPacket);
                                    break;
                                }
                            case (int)TCPServerCmds.CMD_CREATE_ROLE:
                                    {
                                        SysConOut.WriteLine("====CMD_CREATE_ROLE=====");

                                        if (!fields[0].Equals("0"))
                                            SendRoleData(g_Acc);
                                        break;
                                    }
                            case (int)CommandID.CMD_INIT_GAME:
                                {

                                    SysConOut.WriteLine("==============CMD_INIT_GAME 30004===============");

                                    MemoryStream msRecvData = new MemoryStream(szTCPOutPacket.GetPacketBytes(), szOfest, szTCPOutPacket.PacketDataSize - szOfest);
                                    InitialGameReponse szInitialGameReponse = Serializer.Deserialize<InitialGameReponse>(msRecvData);
                                    
                                    SysConOut.WriteLine("CMD_INIT_GAME RoleID = " + szInitialGameReponse.SubRoleinfo.RoleID.ToString() + "  Role = " + g_RoleID.ToString());
                                    SendPlayGame(g_RoleID);
                                    m_tcpOutPacketPool.Push(szTCPOutPacket);
                                    break;
                                }
                            case (int)TCPServerCmds.CMD_INIT_GAME:
                                    {

                                        SysConOut.WriteLine("==============CMD_INIT_GAME===============");

                                        RoleData szRoleData = DataHelper.BytesToObject<RoleData>(szTCPOutPacket.GetPacketBytes(), szOfest, szTCPOutPacket.PacketDataSize - szOfest);
                                        SysConOut.WriteLine("CMD_INIT_GAME RoleID = " + szRoleData.RoleID.ToString());
                                        SendPlayGame(g_RoleID);
                                        break;
                                    }
                            case (int)CommandID.CMD_PLAY_GAME:
                                {
                                    SysConOut.WriteLine("==============CMD_PLAY_GAME 30005===============");
                                    MemoryStream msRecvData = new MemoryStream(szTCPOutPacket.GetPacketBytes(), szOfest, szTCPOutPacket.PacketDataSize - szOfest);
                                    PlayGameReponse szPlayGameReponse = Serializer.Deserialize<PlayGameReponse>(msRecvData);

                                    SysConOut.WriteLine("CMD_PLAY_GAME RoleID = " + szPlayGameReponse.RoleID.ToString());

                                    if (g_Thread == null)
                                        g_Thread = new Thread(new ThreadStart(ThreadFuncation));
                                    g_Thread.Start();
                                    m_tcpOutPacketPool.Push(szTCPOutPacket);
                                    break;
                                }
                            case (int)TCPServerCmds.CMD_PLAY_GAME:
                                    {
                                        SysConOut.WriteLine("==============CMD_PLAY_GAME===============");

                                        if (g_Thread == null)
                                            g_Thread = new Thread(new ThreadStart(ThreadFuncation));
                                        g_Thread.Start();
                                        break;
                                    }
                            case (int)CommandID.CMD_GAME_MOVE:
                                {
                                    SysConOut.WriteLine("==============CMD_GAME_MOVE 30006===============");
                                    MemoryStream msRecvData = new MemoryStream(szTCPOutPacket.GetPacketBytes(), szOfest, szTCPOutPacket.PacketDataSize - szOfest);
                                    SpriteMove szSpriteMove = Serializer.Deserialize<SpriteMove>(msRecvData);
                                   

                                    SysConOut.WriteLine("CMD_SPR_MOVE Role = " + szSpriteMove.RoleID.ToString()
                                        + " Mapcode = " + szSpriteMove.MapCode.ToString()
                                        + " action = " + szSpriteMove.action.ToString()
                                        + " pathString = " + szSpriteMove.pathString);
                                    m_tcpOutPacketPool.Push(szTCPOutPacket);
                                    break;
                                }

                            case (int)TCPServerCmds.CMD_SPR_MOVE:
                                    {
                                        SpriteNotifyOtherMoveData moveData = DataHelper.BytesToObject<SpriteNotifyOtherMoveData>(szTCPOutPacket.GetPacketBytes(), 11, szTCPOutPacket.PacketDataSize - 11);

                                        SysConOut.WriteLine("CMD_SPR_MOVE Role = " + moveData.roleID.ToString()
                                            + " Mapcode = " + moveData.mapCode.ToString()
                                            + " action = " + moveData.action.ToString()
                                            + " pathString = " + moveData.pathString);
                                    m_tcpOutPacketPool.Push(szTCPOutPacket);
                                    break;
                                    }
                                   
                            }
                        }
                        catch (Exception ex)
                        {
                            SysConOut.WriteLine(ex.Message);

                        }


                    mutex.Release();
                }
                Thread.Sleep(100);
            }
        }

        public void Add(TCPOutPacket p)
        {
            m_MoveDataList.Enqueue(p);
        }

        public TCPOutPacket Get()
        {
            return m_MoveDataList.Dequeue();
        }
    }
}
