#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System.Windows;
using System.Threading;
//using System.Windows.Documents;
using GameServer.Server;
using GameServer.Interface;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic.LiXianBaiTan;
using GameServer.Logic.LiXianGuaJi;
using System.Xml.Linq;
using GameServer.Core.Executor;

using GameServer.Logic.WanMota;
using GameServer.Logic.JingJiChang;
using GameServer.Logic.YueKa;
using GameServer.Logic.UserReturn;
using GameServer.Logic.ActivityNew;
using Tmsk.Contract;
using GameServer.Logic.FluorescentGem;
using System.Net;
using GameServer.Logic.ActivityNew.SevenDay;
using GameServer.Logic.Spread;
using GameServer.Logic.SecondPassword;
using GameServer.Logic.OnePiece;
using GameServer.Logic.CheatGuard;
using GameServer.Logic.UnionPalace;
using GameServer.Logic.Goods;
using GameServer.Logic.Tarot;
using GameServer.Logic.Marriage.CoupleArena;
using GameServer.Logic.UnionAlly;
using CC;
using System.IO;
using ProtoBuf;

namespace GameServer.Logic
{
    /// <summary>
    /// 游戏客户端管理
    /// </summary>
    public class ClientManager
    {
        #region 基本属性和方法

        private const int MAX_CLIENT_COUNT = 2000;

        public int GetMaxClientCount()
        {
            return MAX_CLIENT_COUNT;
        }

        /// <summary>
        /// 客户端队列
        /// </summary>
        //private List<GameClient> _ListClients = new List<GameClient>(1000);
        private GameClient[] _ArrayClients = new GameClient[MAX_CLIENT_COUNT];

        /// <summary>
        /// 客户端映射对象
        /// </summary>
        //private Dictionary<TMSKSocket, GameClient> _DictClients = new Dictionary<TMSKSocket, GameClient>(1000);
        // roleid->nid的字典
        private Dictionary<int, int> _DictClientNids = new Dictionary<int, int>(MAX_CLIENT_COUNT);

        /// <summary>
        /// 空闲列表
        /// </summary>
        private List<int> _FreeClientList = new List<int>(MAX_CLIENT_COUNT);

        /// <summary>
        /// 客户端容器对象
        /// </summary>
        private SpriteContainer Container = new SpriteContainer();

        public void initialize(IEnumerable<XElement> mapItems)
        {
            Container.initialize(mapItems);

            for (int i = 0; i < MAX_CLIENT_COUNT; i++)
            {
                _ArrayClients[i] = null;
                // 初始化时增加到空闲列表
                _FreeClientList.Add(i);
            }
        }

        /// <summary>
        /// 添加一个新的客户端
        /// </summary>
        /// <param name="client"></param>
        public bool AddClient(GameClient client)
        {
            try
            {
                /*lock (_ListClients)
                {
                    if (_ListClients.FindIndex((x) => { return x.ClientData.RoleID == client.ClientData.RoleID && x.ClientData.ClosingClientStep == 0; }) >= 0)
                    {
                        return false;
                    }
                    _ListClients.Add(client);
                }*/

                GameClient gc = FindClient(client.ClientData.RoleID);
                if (null != gc)
                {
                    // 要把无心跳的客户端先删掉
                    if (gc.ClientData.ClosingClientStep > 0)
                    {
                        RemoveClient(gc);
                    }
                    else
                    {
                        return false;
                    }
                }

                int index = -1;
                lock (_FreeClientList)
                {
                    if (null == _FreeClientList || _FreeClientList.Count <= 0)
                    {
                        LogManager.WriteLog(LogTypes.Error,
                            string.Format("ClientManager::AddClient _FreeClientList.Count <= 0"));
                        return false;
                    }
                    index = _FreeClientList[0];
                    _FreeClientList.RemoveAt(0);
                }

                _ArrayClients[index] = client;
                client.ClientSocket.Nid = index;

                lock (_DictClientNids)
                {
                    _DictClientNids[client.ClientData.RoleID] = index;
                }

                AddClientToContainer(client);
            }
            catch (Exception e)
            {
                LogManager.WriteLog(LogTypes.Error,
                        string.Format("ClientManager::AddClient ==>{0}", e.ToString()));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 添加一个新的客户端到容器
        /// </summary>
        /// <param name="client"></param>
        public void AddClientToContainer(GameClient client)
        {
            // 也添加到客户端容器对象中
            Container.AddObject(client.ClientData.RoleID, client.ClientData.MapCode, client);
        }

        /// <summary>
        /// 删除一个客户端
        /// </summary>
        /// <param name="client"></param>
        public void RemoveClient(GameClient client)
        {
            /*lock (_ListClients)
            {
                try
                {
                    _ListClients.Remove(client);
                    _ListClients.RemoveAll((x) => { return x.ClientData.RoleID == client.ClientData.RoleID && x.ClientData.ClosingClientStep > 0; });
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(ex.ToString());
                }
            }*/

            try
            {
                // 如果字典的nid和客户端的不同就记录一个log
                int nNid = FindClientNid(client.ClientData.RoleID);
                if (nNid != client.ClientSocket.Nid)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("ClientManager::RemoveClient nNid={0}, client.ClientSocket.Nid={1]", nNid, client.ClientSocket.Nid));
                }
            }
            catch (Exception e)
            {
            }

            lock (_DictClientNids)
            {
                try
                {
                    _DictClientNids.Remove(client.ClientData.RoleID);
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(ex.ToString());
                    try
                    {
                        _DictClientNids.Remove(client.ClientData.RoleID);
                    }
                    catch (Exception ex2)
                    {
                        LogManager.WriteException(string.Format("try agin:{0}", ex2.ToString()));
                    }
                }
            }

            if (client.ClientSocket.Nid >= 0 && client.ClientSocket.Nid < MAX_CLIENT_COUNT)
            {
                _ArrayClients[client.ClientSocket.Nid] = null;
                lock (_FreeClientList)
                {
                    _FreeClientList.Add(client.ClientSocket.Nid);
                }
            }
            //if (client.ClientSocket.Nid < 0 || client.ClientSocket.Nid >= MAX_CLIENT_COUNT)
            else
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("ClientManager::RemoveClient nid={0} out range", client.ClientSocket.Nid));
                //return;
            }

            client.ClientSocket.Nid = -1;
            RemoveClientFromContainer(client);
        }

        /// <summary>
        /// 从容器删除一个客户端
        /// </summary>
        /// <param name="client"></param>
        public void RemoveClientFromContainer(GameClient client)
        {
            GameMap gameMap = null;
            //从格子的定位队列中删除
            if (!GameManager.MapMgr.DictMaps.TryGetValue(client.ClientData.MapCode, out gameMap) || null == gameMap)
            {
                LogManager.WriteLog(LogTypes.Error, "RemoveClientFromContainer 错误的地图编号：" + client.ClientData.MapCode);
                return;
            }

            bool removed = GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode].RemoveObject(client);

            //也从客户端容器对象中删除
            removed = Container.RemoveObject(client.ClientData.RoleID, client.ClientData.MapCode, client) && removed;

            if (!removed)
            {
                foreach (var mc in GameManager.MapMgr.DictMaps.Keys)
                {
                    GameManager.MapGridMgr.DictGrids[mc].RemoveObject(client);
                    Container.RemoveObject(client.ClientData.RoleID, mc, client);
                }
            }
        }

        /// <summary>
        /// 根据玩家的roleid返回流水号 便于快速查找到GameClient
        /// </summary>
        public int FindClientNid(int RoleID)
        {
            int nNid = -1;
            lock (_DictClientNids)
            {
                if (!_DictClientNids.TryGetValue(RoleID, out nNid))
                {
                    return -1;
                }
            }
            return nNid;
        }

        /// <summary>
        /// 通过TMSKSocket查找一个客户端
        /// </summary>
        /// <param name="client"></param>
        public GameClient FindClientByNid(int nNid)
        {
            if (nNid < 0 || nNid >= MAX_CLIENT_COUNT) return null;

            return _ArrayClients[nNid];
        }

        /// <summary>
        /// 通过TMSKSocket查找一个客户端
        /// </summary>
        /// <param name="client"></param>
        public GameClient FindClient(TMSKSocket socket)
        {
            if (null == socket) return null;

            return FindClientByNid(socket.Nid);
        }

        /// <summary>
        /// 通过ID查找一个客户端
        /// </summary>
        /// <param name="client"></param>
        public GameClient FindClient(int roleID)
        {
            int nNid = FindClientNid(roleID);

            return FindClientByNid(nNid);
        }

        /// <summary>
        /// 判断客户端是否存在
        /// </summary>
        /// <param name="client"></param>
        public bool ClientExists(GameClient client)
        {
            object obj = null;
            lock (Container.ObjectDict)
            {
                Container.ObjectDict.TryGetValue(client.ClientData.RoleID, out obj);
            }

            return (null != obj);
        }

        /// <summary>
        /// 获取下一个客户端
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GameClient GetNextClient(ref int nNid)
        {
            if (nNid < 0 || nNid >= MAX_CLIENT_COUNT) return null;

            GameClient client = null;
            for (; nNid < MAX_CLIENT_COUNT; nNid++)
            {
                if (null != _ArrayClients[nNid])
                {
                    client = _ArrayClients[nNid];
                    // 便于循环取得下一个client
                    nNid++;
                    break;
                }
            }

            return client;
        }

        /// <summary>
        /// 获取地图中的所有的角色
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public List<Object> GetMapClients(int mapCode)
        {
            return Container.GetObjectsByMap(mapCode); //发送给所有地图的用户
        }

        public List<GameClient> GetMapGameClients(int mapCode)
        {
            List<object> objsList = Container.GetObjectsByMap(mapCode); //发送给所有地图的用户
            List<GameClient> clientList = new List<GameClient>();
            if (null != objsList)
            {
                foreach (var obj in objsList)
                {
                    GameClient client = obj as GameClient;
                    if (null != client)
                    {
                        clientList.Add(client);
                    }
                }
            }

            return clientList;
        }

        /// <summary>
        /// 返回地图中活着的玩家列表
        /// </summary>
        /// <returns></returns>
        public List<GameClient> GetMapAliveClients(int mapCode)
        {
            List<GameClient> lsAliveClient = new List<GameClient>();

            GameClient client = null;

            List<Object> lsObjects = GetMapClients(mapCode);
            if (null == lsObjects)
            {
                return lsAliveClient;
            }

            for (int n = 0; n < lsObjects.Count; n++)
            {
                client = lsObjects[n] as GameClient;
                if (null != client && client.ClientData.CurrentLifeV > 0)
                {
                    lsAliveClient.Add(client);
                }
            }

            return lsAliveClient;
        }

        public List<GameClient> GetMapAliveClientsEx(int mapCode, bool writeLog = true)
        {
            List<GameClient> lsAliveClient = new List<GameClient>();

            GameClient client = null;
            List<Object> lsObjects = Container.GetObjectsByMap(mapCode);
            if (null == lsObjects)
            {
                return lsAliveClient;
            }

            for (int n = 0; n < lsObjects.Count; n++)
            {
                client = lsObjects[n] as GameClient;
                if (null != client && client.ClientData.CurrentLifeV > 0)
                {
                    bool valid = false;
                    if (!client.ClientData.WaitingNotifyChangeMap && !client.ClientData.WaitingForChangeMap)
                    {
                        if (client.ClientData.MapCode == mapCode && Global.IsPosReachable(mapCode, client.ClientData.PosX, client.ClientData.PosY))
                        {
                            valid = true;
                            lsAliveClient.Add(client);
                        }

                        if (writeLog && !valid)
                        {
                            /**/
                            string reason = string.Format("存活玩家坐标非法:{6}({7}) mapCode:{0},clientMapCode{1}:,WaitingNotifyChangeMap:{2},WaitingForChangeMap:{3},PosX:{4},PosY{5}",
                            mapCode, client.ClientData.MapCode, client.ClientData.WaitingNotifyChangeMap, client.ClientData.WaitingForChangeMap, client.ClientData.PosX,
                            client.ClientData.PosY, client.ClientData.RoleID, client.ClientData.RoleName);
                            LogManager.WriteLog(LogTypes.Error, reason);
                        }
                    }
                }
            }

            return lsAliveClient;
        }

        public int GetMapAliveClientCountEx(int mapCode)
        {
            int aliveClientCount = 0;
            GameClient client = null;
            List<Object> lsObjects = Container.GetObjectsByMap(mapCode);
            if (null == lsObjects)
            {
                return aliveClientCount;
            }

            for (int n = 0; n < lsObjects.Count; n++)
            {
                client = lsObjects[n] as GameClient;
                if (null != client && client.ClientData.CurrentLifeV > 0)
                {
                    if (!client.ClientData.WaitingNotifyChangeMap && !client.ClientData.WaitingForChangeMap)
                    {
                        if (client.ClientData.MapCode == mapCode && !Global.InOnlyObsByXY(ObjectTypes.OT_CLIENT, mapCode, client.ClientData.PosX, client.ClientData.PosY))
                        {
                            aliveClientCount++;
                        }
                    }
                }
            }

            return aliveClientCount;
        }

        /// <summary>
        /// 获取地图上的用户的个数
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public int GetMapClientsCount(int mapCode)
        {
            return Container.GetObjectsCountByMap(mapCode);
        }

        /// <summary>
        /// 获取在线客户端的个数
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetClientCount()
        {
            int count = 0;
            lock (_FreeClientList)
            {
                count = _FreeClientList.Count;
            }

            return MAX_CLIENT_COUNT - count;
        }

        public int GetClientCountFromDict()
        {
            int count = 0;
            lock (_DictClientNids)
            {
                count = _DictClientNids.Count;
            }

            return count;
        }

        public string GetAllMapRoleNumStr()
        {
            return Container.GetAllMapRoleNumStr();
        }

        /// <summary>
        /// 获取第一个用户
        /// </summary>
        /// <returns></returns>
        public GameClient GetFirstClient()
        {
            GameClient client = null;

            lock (_DictClientNids)
            {
                if (_DictClientNids.Count > 0)
                {
                    foreach (var item in _DictClientNids)
                    {
                        return FindClientByNid(item.Value);
                    }
                }
            }

            return client;
        }

        /// <summary>
        /// 获取一个随机用户
        /// </summary>
        /// <returns></returns>
        public GameClient GetRandomClient()
        {
            lock (_DictClientNids)
            {
                if (_DictClientNids.Count > 0)
                {
                    int[] array = new int[MAX_CLIENT_COUNT];
                    _DictClientNids.Values.CopyTo(array, 0);
                    int index = Global.GetRandomNumber(0, _DictClientNids.Count);
                    return FindClientByNid(array[index]);
                }
            }

            return null;
        }

        #endregion 基本属性和方法

        #region 扩展属性和方法

        #region 公用的发送方法

        public void PushBackTcpOutPacket(TCPOutPacket tcpOutPacket)
        {
            if (null != tcpOutPacket)
            {
                //还回tcpoutpacket
                Global._TCPManager.TcpOutPacketPool.Push(tcpOutPacket);
            }
        }

        /// <summary>
        /// 将消息包发送到其他用户
        /// </summary>
        /// <param name="clientList"></param>
        /// <param name="tcpOutPacket"></param>
        public void SendToClients(SocketListener sl, TCPOutPacketPool pool, object self, List<object> objsList, byte[] bytesData, int cmdID)
        {




            // Console.WriteLine("广播移动数据 = " + objsList.Count.ToString());
            if (null == objsList || cmdID <30000) return;

            TCPOutPacket tcpOutPacket = null;
            try
            {
                for (int i = 0; i < objsList.Count; i++)
                {
                  if(objsList[i] is GameClient)
                  {
                        //    GameClient szToClient = objsList[i] as GameClient;
                        //    SysConOut.WriteLine(string.Format("广播移动数据给   roleID = {0}", szToClient.ClientData.RoleID));
                        //是否跳过自己
                        //if (null != self && self == objsList[i])
                        //{
                        //    continue;
                        //}


                        //SysConOut.WriteLine("UID =》" + (objsList[i] as GameClient).strUserID + "");


                        GameClient c = objsList[i] as GameClient;
                        if (null == c)
                        {
                            continue;
                        }

                        if (c.LogoutState) //如果已经退出了
                        {
                            continue;
                        }

                        if (null == tcpOutPacket)
                        {
                            tcpOutPacket = pool.Pop();
                            tcpOutPacket.PacketCmdID = (UInt16)cmdID;
                            tcpOutPacket.FinalWriteData(bytesData, 0, (int)bytesData.Length);
                        }

                        if (!sl.SendData((objsList[i] as GameClient).ClientSocket, tcpOutPacket, false))
                        {

                            //Console.WriteLine("广播移动数据失败 = " + (objsList[i] as GameClient).ClientData.RoleID.ToString());
                            //
                            LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                tcpOutPacket.PacketCmdID,
                                tcpOutPacket.PacketDataSize,
                                (objsList[i] as GameClient).ClientData.RoleID,
                                (objsList[i] as GameClient).ClientData.RoleName));
                            
                        }
                        else
                        {
                            //SysConOut.WriteLine(string.Format("2====>向用户发送tcp数据成功: ID={0}, Size={1}, RoleID={2}, CMD={3}",
                            //    tcpOutPacket.PacketCmdID,
                            //    tcpOutPacket.PacketDataSize,
                            //    (objsList[i] as GameClient).ClientData.RoleID,
                            //    cmdID));
                            
                        }
                    }
                }
                    
            }
            catch(Exception ex)
            {
                SysConOut.WriteLine(string.Format("向用户发送tcp数据异常: CMD={0}   {1}",cmdID,
                            ex.Message));
                //PushBackTcpOutPacket(tcpOutPacket);
                if (tcpOutPacket != null)
                    PushBackTcpOutPacket(tcpOutPacket);
            }
           
        }

        /// <summary>
        /// 将消息包发送到其他用户
        /// </summary>
        /// <param name="clientList"></param>
        /// <param name="tcpOutPacket"></param>
        public void SendToClients(SocketListener sl, TCPOutPacketPool pool, object self, List<object> objsList, string strCmd, int cmdID)
        {
            if (null == objsList) return;

            TCPOutPacket tcpOutPacket = null;
            try
            {
                for (int i = 0; i < objsList.Count; i++)
                {
                    //是否跳过自己
                    if (null != self && self == objsList[i])
                    {
                        continue;
                    }

                    GameClient c = objsList[i] as GameClient;
                    if (null == c)
                    {
                        continue;
                    }

                    if (c.LogoutState) //如果已经退出了
                    {
                        continue;
                    }

                    if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, cmdID);
                    if (!sl.SendData((objsList[i] as GameClient).ClientSocket, tcpOutPacket, false))
                    {
                        //
                        /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                            tcpOutPacket.PacketCmdID,
                            tcpOutPacket.PacketDataSize,
                            (objsList[i] as GameClient).ClientData.RoleID,
                            (objsList[i] as GameClient).ClientData.RoleName));*/
                    }
                }
            }
            finally
            {
                PushBackTcpOutPacket(tcpOutPacket);
            }
        }

        /// <summary>
        /// 将消息包发送到其他用户
        /// </summary>
        /// <param name="clientList"></param>
        /// <param name="tcpOutPacket"></param>
        public void SendToClients<T>(SocketListener sl, TCPOutPacketPool pool, object self, List<object> objsList, T scData, int cmdID)
        {
            if (null == objsList) return;

            TCPOutPacket tcpOutPacket = null;
            try
            {
                for (int i = 0; i < objsList.Count; i++)
                {
                    //是否跳过自己
                    if (null != self && self == objsList[i])
                    {
                        continue;
                    }

                    if (!(objsList[i] is GameClient))
                    {
                        continue;
                    }

                    if ((objsList[i] as GameClient).LogoutState) //如果已经退出了
                    {
                        continue;
                    }

                    (objsList[i] as GameClient).sendCmd(cmdID, scData);

                    //if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, cmdID);
                    //if (!sl.SendData((objsList[i] as GameClient).ClientSocket, tcpOutPacket, false))
                    //{
                    //    //
                    //    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    //        tcpOutPacket.PacketCmdID,
                    //        tcpOutPacket.PacketDataSize,
                    //        (objsList[i] as GameClient).ClientData.RoleID,
                    //        (objsList[i] as GameClient).ClientData.RoleName));*/
                    //}
                }
            }
            finally
            {
                PushBackTcpOutPacket(tcpOutPacket);
            }
        }
        public void SendProtocolToClients<T>(SocketListener sl, TCPOutPacketPool pool, object self, List<object> objsList, T scData, int cmdID)
        {
            if (null == objsList) return;

            TCPOutPacket tcpOutPacket = null;
            try
            {
                for (int i = 0; i < objsList.Count; i++)
                {
                    //是否跳过自己
                    if (null != self && self == objsList[i])
                    {
                        continue;
                    }

                    if (!(objsList[i] is GameClient))
                    {
                        continue;
                    }

                    if ((objsList[i] as GameClient).LogoutState) //如果已经退出了
                    {
                        continue;
                    }
                    //if (cmdID == 30006)
                    //    SysConOut.WriteLine(string.Format("广播移动消息,移动人{0} 发送给{1}", (scData as SpriteMove).RoleID, (objsList[i] as GameClient).ClientData.RoleID));

                    (objsList[i] as GameClient).sendProtocolCmd(cmdID, scData);
                }
            }
            finally
            {
                PushBackTcpOutPacket(tcpOutPacket);
            }
        }

        /// <summary>
        /// 将消息包发送到其他用户
        /// </summary>
        /// <param name="clientList"></param>
        /// <param name="tcpOutPacket"></param>
        public void SendToClients<T1, T2>(SocketListener sl, TCPOutPacketPool pool, object self, List<T1> objsList, T2 data, int cmdID, int hideFlag, int includeRoleId)
        {
            if (null == objsList) return;

            TCPOutPacket tcpOutPacket = null;
            try
            {
                for (int i = 0; i < objsList.Count; i++)
                {
                    //是否跳过自己
                    if (null != self && self == (object)objsList[i])
                    {
                        continue;
                    }

                    GameClient c = objsList[i] as GameClient;
                    if (null == c)
                    {
                        continue;
                    }

                    if (c.ClientData.RoleID != includeRoleId && (c.ClientEffectHideFlag1 & hideFlag) > 0)
                    {
                        continue;
                    }

                    if (c.LogoutState) //如果已经退出了
                    {
                        continue;
                    }

                    if (null == tcpOutPacket)
                    {
                        if (data is string)
                        {
                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, data as string, cmdID);
                        }
                        else if (data is byte[])
                        {
                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, data as byte[], cmdID);
                        }
                        else
                        {
                            return; //尚未实现
                        }
                    }
                    if (!sl.SendData((objsList[i] as GameClient).ClientSocket, tcpOutPacket, false))
                    {
                        //
                        /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                            tcpOutPacket.PacketCmdID,
                            tcpOutPacket.PacketDataSize,
                            (objsList[i] as GameClient).ClientData.RoleID,
                            (objsList[i] as GameClient).ClientData.RoleName));*/
                    }
                }
            }
            finally
            {
                PushBackTcpOutPacket(tcpOutPacket);
            }
        }

        /// <summary>
        /// 将消息包发送到某用户
        /// </summary>
        /// <param name="clientList"></param>
        /// <param name="tcpOutPacket"></param>
        public void SendToClient(SocketListener sl, TCPOutPacketPool pool, GameClient client, string strCmd, int cmdID)
        {
            if (null == client) return;

            if (client.LogoutState) //如果已经退出了
            {
                return;
            }

            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, cmdID);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 将消息包发送到某用户
        /// </summary>
        /// <param name="clientList"></param>
        /// <param name="tcpOutPacket"></param>
        public void SendToClient(GameClient client, string strCmd, int cmdID)
        {
            if (null == client) return;

            if (client.LogoutState) //如果已经退出了
            {
                return;
            }

            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strCmd, cmdID);

            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 将消息包发送到某用户
        /// </summary>
        /// <param name="clientList"></param>
        /// <param name="tcpOutPacket"></param>
        public void SendToClient(GameClient client, byte[] buffer, int cmdID)
        {
            if (null == client) return;

            if (client.LogoutState) //如果已经退出了
            {
                return;
            }

            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, buffer, 0, buffer.Length, cmdID);

            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 公用的发送方法

        #region 通知角色打开窗口

        /// <summary>
        /// 通知客户端打开窗口
        /// </summary>
        /// <param name="client"></param>
        /// <param name="windowType"></param>
        /// <param name="strParams"></param>
        public void NotifyClientOpenWindow(GameClient client, int windowType, String strParams)
        {
            String cmd = String.Format("{0}:{1}:{2}", client.ClientData.RoleID, windowType, strParams);

            SendToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, cmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYOPENWINDOW);
        }

        #endregion

        #region 角色数据通知

        /// <summary>
        /// 通知其他人自己上线(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersIamComing(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList, int cmd)
        {
            if (null == objsList) return;

            RoleData roleData = Global.ClientToRoleData2(client);

            RoleInfoDataMini szRoleInfoDataMini = new RoleInfoDataMini();
            szRoleInfoDataMini.RoleID = roleData.RoleID;
            szRoleInfoDataMini.RoleName = roleData.RoleName;
            szRoleInfoDataMini.RoleSex = roleData.RoleSex;
            szRoleInfoDataMini.Occupation = roleData.Occupation;
            szRoleInfoDataMini.Level = roleData.Level;
            szRoleInfoDataMini.MapCode = roleData.MapCode;
            szRoleInfoDataMini.PosX = roleData.PosX;
            szRoleInfoDataMini.PosY = roleData.PosY;
            szRoleInfoDataMini.RoleDirection = roleData.RoleDirection;
            szRoleInfoDataMini.LifeV = roleData.LifeV;
            szRoleInfoDataMini.MaxLifeV = roleData.MaxLifeV;
            szRoleInfoDataMini.MagicV = roleData.MagicV;
            szRoleInfoDataMini.MaxMagicV = roleData.MaxMagicV;
            szRoleInfoDataMini.ZoneID = roleData.ZoneID;
            foreach(var s in client.ClientData.SkillDataList)
            {
                szRoleInfoDataMini.SkillList.Add(s.SkillID);
            }

           

            SendProtocolToClients<RoleInfoDataMini>(sl, pool, client, objsList, szRoleInfoDataMini, cmd);

           // byte[] bytesData = DataHelper.ObjectToBytes<RoleData>(roleData);

            //群发消息
           // SendToClients(sl, pool, client, objsList, bytesData, cmd);
        }

        /// <summary>
        /// 将其他所有在线的人通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public int NotifySelfOnlineOthers(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList, int cmd)
        {
            if (null == objsList) return 0;

            int totalCount = 0;
            for (int i = 0; i < objsList.Count && i < 30; i++)
            {
                if(objsList[i] is GameClient)
                {
                    TCPOutPacket tcpOutPacket = null;
                    if (1 == GameManager.RoleDataMiniMode)
                    {
                        
                        // objsList[i].GetType
                        RoleDataMini roleDataMini = Global.ClientToRoleDataMini((objsList[i] as GameClient));
                        roleDataMini.BufferMiniInfo = Global.GetBufferMiniList((objsList[i] as GameClient));
                        GameClient szOtherClient = GameManager.ClientMgr.FindClient(roleDataMini.RoleID);

                        RoleInfoDataMini szRoleInfoDataMini = new RoleInfoDataMini();
                        szRoleInfoDataMini.RoleID = roleDataMini.RoleID;
                        szRoleInfoDataMini.RoleName = roleDataMini.RoleName;
                        szRoleInfoDataMini.RoleSex = roleDataMini.RoleSex;
                        szRoleInfoDataMini.Occupation = roleDataMini.Occupation;
                        szRoleInfoDataMini.Level = roleDataMini.Level;
                        szRoleInfoDataMini.MapCode = roleDataMini.MapCode;
                        szRoleInfoDataMini.PosX = roleDataMini.PosX;
                        szRoleInfoDataMini.PosY = roleDataMini.PosY;
                        szRoleInfoDataMini.RoleDirection = roleDataMini.RoleDirection;
                        szRoleInfoDataMini.LifeV = roleDataMini.LifeV;
                        szRoleInfoDataMini.MaxLifeV = roleDataMini.MaxLifeV;
                        szRoleInfoDataMini.MagicV = roleDataMini.MagicV;
                        szRoleInfoDataMini.MaxMagicV = roleDataMini.MaxMagicV;
                        szRoleInfoDataMini.ZoneID = roleDataMini.ZoneID;
                        if(null != szOtherClient)
                        {
                            foreach (var s in szOtherClient.ClientData.SkillDataList)
                            {
                                szRoleInfoDataMini.SkillList.Add(s.SkillID);
                            }
                        }
                        

                        tcpOutPacket = DataHelper.ProtocolToTCPOutPacket<RoleInfoDataMini>(szRoleInfoDataMini, pool, cmd);
                        //tcpOutPacket = DataHelper.ObjectToTCPOutPacket<RoleDataMini>(roleDataMini, pool, cmd);
                    }
                    else
                    {
                        RoleData roleData = Global.ClientToRoleData2((objsList[i] as GameClient));
                        GameClient szOtherClient = GameManager.ClientMgr.FindClient(roleData.RoleID);

                        RoleInfoDataMini szRoleInfoDataMini = new RoleInfoDataMini();
                        szRoleInfoDataMini.RoleID = roleData.RoleID;
                        szRoleInfoDataMini.RoleName = roleData.RoleName;
                        szRoleInfoDataMini.RoleSex = roleData.RoleSex;
                        szRoleInfoDataMini.Occupation = roleData.Occupation;
                        szRoleInfoDataMini.Level = roleData.Level;
                        szRoleInfoDataMini.MapCode = roleData.MapCode;
                        szRoleInfoDataMini.PosX = roleData.PosX;
                        szRoleInfoDataMini.PosY = roleData.PosY;
                        szRoleInfoDataMini.RoleDirection = roleData.RoleDirection;
                        szRoleInfoDataMini.LifeV = roleData.LifeV;
                        szRoleInfoDataMini.MaxLifeV = roleData.MaxLifeV;
                        szRoleInfoDataMini.MagicV = roleData.MagicV;
                        szRoleInfoDataMini.MaxMagicV = roleData.MaxMagicV;
                        szRoleInfoDataMini.ZoneID = roleData.ZoneID;
                        if(null != szOtherClient)
                        {
                            foreach (var s in client.ClientData.SkillDataList)
                            {
                                szRoleInfoDataMini.SkillList.Add(s.SkillID);
                            }
                        }
                        
                        tcpOutPacket = DataHelper.ProtocolToTCPOutPacket<RoleInfoDataMini>(szRoleInfoDataMini, pool, cmd);
                        // tcpOutPacket = DataHelper.ObjectToTCPOutPacket<RoleData>(roleData, pool, cmd);

                    }

                    if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                    {

                        break;
                    }
                    totalCount++;
                }


               
            }

            return totalCount;
        }

        /// <summary>
        /// 将其他在线的某人通知自己(未必是同一个地图, 所以客户端需要特出处理)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfOnline(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient otherClient, int cmd)
        {
            if (null == otherClient)
            {
                return;
            }

            RoleData roleData = Global.ClientToRoleData2(otherClient);
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<RoleData>(roleData, pool, cmd);

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 将其他在线的某人数据通知自己(未必是同一个地图, 所以客户端需要特出处理)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfOnlineData(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient otherClient, int cmd)
        {
            if (null == otherClient)
            {
                return;
            }

            RoleData roleData = Global.ClientToRoleData2(otherClient);
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<RoleData>(roleData, pool, cmd);

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 将其他在线的某人数据通知自己(未必是同一个线路, 所以客户端需要特出处理)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfOtherData(SocketListener sl, TCPOutPacketPool pool, GameClient client, RoleDataEx roleDataEx, int cmd)
        {
            RoleData roleData = null;
            if (null != roleDataEx)
            {
                roleData = Global.RoleDataExToRoleData(roleDataEx);
            }

            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<RoleData>(roleData, pool, cmd);

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 角色数据通知

        #region 加载和移动通知

        /// <summary>
        /// 通知自己其他人的当前动作状态
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="otherRoleID"></param>
        /// <param name="mapCode"></param>
        /// <param name="action"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cmd"></param>
        /// <param name="moveCost"></param>
        /// <param name="extAction"></param>
        public void NotifyMyselfOtherLoadAlready(SocketListener sl, TCPOutPacketPool pool, GameClient client, int otherRoleID, int mapCode, long startMoveTicks, int currentX, int currentY, int currentDirection, int action, int toX, int toY, double moveCost = 1.0, int extAction = 0, int currentPathIndex = 0)
        {
            GameClient otherClient = FindClient(otherRoleID);

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}", otherRoleID, mapCode, currentX, currentY, currentDirection, action, toX, toY, moveCost, extAction, startMoveTicks, null != otherClient ? otherClient.ClientData.RolePathString : "", currentPathIndex);
            LoadAlreadyData loadAlreadyData = new LoadAlreadyData()
            {
                RoleID = otherRoleID,
                MapCode = mapCode,
                StartMoveTicks = startMoveTicks,
                CurrentX = currentX,
                CurrentY = currentY,
                CurrentDirection = currentDirection,
                Action = action,
                ToX = toX,
                ToY = toY,
                MoveCost = moveCost,
                ExtAction = extAction,
                PathString = null != otherClient ? otherClient.ClientData.RolePathString : "",
                CurrentPathIndex = currentPathIndex,
            };

            TCPOutPacket tcpOutPacket = null;
            //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_LOADALREADY);
            byte[] bytes = DataHelper.ObjectToBytes<LoadAlreadyData>(loadAlreadyData);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_LOADALREADY);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知自己其他人的移动
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="otherRoleID"></param>
        /// <param name="mapCode"></param>
        /// <param name="action"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cmd"></param>
        /// <param name="moveCost"></param>
        /// <param name="extAction"></param>
        public void NotifyMyselfOtherMoving(SocketListener sl, TCPOutPacketPool pool, GameClient client, int otherRoleID, int mapCode, int action, long startMoveTicks, int fromX, int fromY, int toX, int toY, int cmd, double moveCost = 1.0, int extAction = 0)
        {
            GameClient otherClient = FindClient(otherRoleID);

            List<object> objsList = new List<object>();
            objsList.Add(client);

           SpriteMove szSpriteMove = new SpriteMove();
            szSpriteMove.RoleID = otherRoleID;
            szSpriteMove.MapCode = mapCode;
            szSpriteMove.action = action;
            szSpriteMove.toX = toX;
            szSpriteMove.toY = toY;
            szSpriteMove.extAction = extAction;
            szSpriteMove.fromX = fromX;
            szSpriteMove.fromY = fromY;
            szSpriteMove.startMoveTicks = startMoveTicks;
            szSpriteMove.pathString = (null != otherClient ? otherClient.ClientData.RolePathString : "") ;
            szSpriteMove.targetRoleID = 0;
            if (toX <= 0 || toY<=0 || fromX <= 0 || fromY <=0 )
                LogManager.WriteLog(LogTypes.Error, string.Format("移动消息ID = {0}   px={1} py={2} fx={3} fy={4}", otherRoleID, toX, toY, fromX, fromY));
            // SysConOut.WriteLine("=================================>Role ID = " + otherRoleID.ToString());
            SendProtocolToClients<SpriteMove>(sl, pool, null, objsList, szSpriteMove, cmd);
            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", otherRoleID, mapCode, action, toX, toY, moveCost, extAction, fromX, fromY, startMoveTicks, null != otherClient? otherClient.ClientData.RolePathString : "");
        //    SpriteNotifyOtherMoveData moveData = new SpriteNotifyOtherMoveData();
        //    moveData.roleID = otherRoleID;
        //    moveData.mapCode = mapCode;
        //    moveData.action = action;
        //    moveData.toX = toX;
        //    moveData.toY = toY;
        //    moveData.moveCost = moveCost;
        //    moveData.extAction = extAction;
        //    moveData.fromX = fromX;
        //    moveData.fromY = fromY;
        //    moveData.startMoveTicks = startMoveTicks;
        //    moveData.pathString = null != otherClient ? otherClient.ClientData.RolePathString : "";
        //    TCPOutPacket tcpOutPacket = null;
        //    //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, cmd);
        //    byte[] bytes = DataHelper.ObjectToBytes<SpriteNotifyOtherMoveData>(moveData);
        //    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytes, 0, bytes.Length, cmd);
        //    if (!sl.SendData(client.ClientSocket, tcpOutPacket))
        //    {
        //        //
        //        /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
        //            tcpOutPacket.PacketCmdID,
        //            tcpOutPacket.PacketDataSize,
        //            client.ClientData.RoleID,
        //            client.ClientData.RoleName));*/
        //    }
        }

        /// <summary>
        /// 通知自己其他人的移动
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="objsList"></param>
        public void NotifyMyselfOthersMoving(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                if (client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID) //跳过自己
                {
                    continue;
                }

                //判断如果是在移动中
                if ((objsList[i] as GameClient).ClientData.CurrentAction == (int)GActions.Walk || (objsList[i] as GameClient).ClientData.CurrentAction == (int)GActions.Run)
                {
                    //通知自己其他人的移动
                    GameManager.ClientMgr.NotifyMyselfOtherMoving(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                        (objsList[i] as GameClient).ClientData.RoleID,
                        (objsList[i] as GameClient).ClientData.MapCode,
                        (objsList[i] as GameClient).ClientData.CurrentAction,
                        Global.GetClientStartMoveTicks((objsList[i] as GameClient)),
                        (int)(objsList[i] as GameClient).ClientData.PosX,
                        (int)(objsList[i] as GameClient).ClientData.PosY,
                        (int)(objsList[i] as GameClient).ClientData.DestPoint.X,
                        (int)(objsList[i] as GameClient).ClientData.DestPoint.Y,
                        (int)CommandID.CMD_GAME_MOVE, (objsList[i] as GameClient).ClientData.MoveSpeed, 0);
                }
            }
        }

        /// <summary>
        /// 通知其他人自己开始移动(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersMyMoving(SocketListener sl, TCPOutPacketPool pool, SpriteNotifyOtherMoveData moveData, GameClient client, int cmd, List<Object> objsList = null)
        {
            if (null == objsList)
            {
                objsList = Global.GetAll9Clients(client);
            }

            if (null == objsList) return;
            //SysConOut.WriteLine("\r\n\r\n");
            //foreach (var s in objsList)
            //{
            //    if(s is GameClient)
            //    {
            //        GameClient szToClient = s as GameClient;
            //        //SysConOut.WriteLine(string.Format("1===>广播Role = {0}移动数据给   roleID = {1} , 发送的数据 {2} 命令 = {3}",
            //        //    client.ClientData.RoleID, szToClient.ClientData.RoleID, moveData.roleID, cmd));
            //    }
                
            //}

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", client.ClientData.RoleID, mapCode, action, toX, toY, moveCost, extAction, fromX, fromY, startMoveTicks, client.ClientData.RolePathString);
            //             SpriteNotifyOtherMoveData moveData = new SpriteNotifyOtherMoveData();
            //             moveData.roleID = client.ClientData.RoleID;
            //             moveData.mapCode = mapCode;
            //             moveData.action = action;
            //             moveData.toX = toX;
            //             moveData.toY = toY;
            //             moveData.moveCost = moveCost;
            //             moveData.extAction = extAction;
            //             moveData.fromX = fromX;
            //             moveData.fromY = fromY;
            //             moveData.startMoveTicks = startMoveTicks;
            //             moveData.pathString = client.ClientData.RolePathString;
            //群发消息
            //  SendToClients(sl, pool, client, objsList, DataHelper.ObjectToBytes<SpriteNotifyOtherMoveData>(moveData), cmd);
            try
            {
                SpriteMove szSpriteMove = new SpriteMove();
                szSpriteMove.RoleID = moveData.roleID;
                szSpriteMove.MapCode = moveData.mapCode;
                szSpriteMove.action = moveData.action;
                szSpriteMove.toX = moveData.toX;
                szSpriteMove.toY = moveData.toY;
                //szSpriteMove.Movec = moveData.moveCost;
                szSpriteMove.extAction = moveData.extAction;
                szSpriteMove.fromX = moveData.fromX;
                szSpriteMove.fromY = moveData.fromY;
                szSpriteMove.startMoveTicks = moveData.startMoveTicks;
                szSpriteMove.pathString = moveData.pathString;

                if (moveData.toX <= 0 || moveData.toY <= 0 || moveData.fromX <= 0 || moveData.fromY <= 0)
                    LogManager.WriteLog(LogTypes.Error, string.Format("移动消息ID = {0}   px={1} py={2} fx={3} fy={4}", moveData.roleID, moveData.toX, moveData.toY, moveData.fromX, moveData.fromY));

                MemoryStream msResult = new MemoryStream();
                Serializer.Serialize<SpriteMove>(msResult, szSpriteMove);
                byte[] msSendData = msResult.ToArray();

                SendToClients(sl, pool, client, objsList, msSendData, cmd);
            }
            catch(Exception ex)
            {
                SysConOut.WriteLine("NotifyOthersMyMoving 异常 " + ex.Message);
            }
        }

        /// <summary>
        /// 通知其他人自己移动结束(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersMyMovingEnd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int mapCode, int action, int toX, int toY, int direction, int tryRun, bool sendToSelf, List<Object> objsList = null)
        {
            if (null == objsList)
            {
                objsList = Global.GetAll9Clients(client);
            }

            if (null == objsList) return;

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", client.ClientData.RoleID, mapCode, action, toX, toY, direction, tryRun);

            SCMoveEnd scData = new SCMoveEnd(client.ClientData.RoleID, mapCode, action, toX, toY, direction, tryRun);
            ////群发消息
            SendToClients<SCMoveEnd>(sl, pool, sendToSelf ? null : client, objsList, scData, (int)TCPGameServerCmds.CMD_SPR_MOVEEND);
            //SendToClients(sl, pool, sendToSelf ? null : client, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_MOVEEND);
        }
        public void NotifyOthersMyMovingEnd<T>(SocketListener sl, TCPOutPacketPool pool, GameClient client,T Protocol , bool sendToSelf, List<Object> objsList = null)
        {
            if (null == objsList)
            {
                objsList = Global.GetAll9Clients(client);
            }

            if (null == objsList) return;
            ////群发消息
            SendProtocolToClients<T>(sl, pool, sendToSelf ? null : client, objsList, Protocol, (int)CommandID.CMD_STOP_MOVE);
          
        }

        /// <summary>
        /// 通知其他人自己终止自动寻路(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersStopMyMoving(SocketListener sl, TCPOutPacketPool pool, GameClient client, int stopIndex, List<Object> objsList = null)
        {
            if (null == objsList)
            {
                objsList = Global.GetAll9Clients(client);
            }

            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, stopIndex);

            //群发消息
            SendToClients(sl, pool, client, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_STOPMOVE);
        }

        /// <summary>
        /// 通知其他人怪物(宠物)开始移动(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public bool NotifyOthersToMoving(SocketListener sl, TCPOutPacketPool pool, IObject obj, int mapCode, int copyMapID, int roleID, long startMoveTicks, int currentX,
            int currentY, int action, int toX, int toY, int cmd, double moveCost = 1.0, String pathString = "", List<Object> objsList = null)
        {
            if (null == objsList)
            {
                if (null == obj)
                {
                    objsList = Global.GetAll9Clients2(mapCode, currentX, currentY, copyMapID);
                }
                else
                {
                    objsList = Global.GetAll9Clients(obj);
                }
            }

            if (null == objsList) return true;

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", roleID, mapCode, action, toX, toY, moveCost, 0, currentX, currentY, startMoveTicks, pathString);//镖车，怪物和宠物的移动历史路径为空
            SpriteNotifyOtherMoveData moveData = new SpriteNotifyOtherMoveData();
            moveData.roleID = roleID;
            moveData.mapCode = mapCode;
            moveData.action = action;
            moveData.toX = toX;
            moveData.toY = toY;
            moveData.moveCost = moveCost;
            moveData.extAction = 0;
            moveData.fromX = currentX;
            moveData.fromY = currentY;
            moveData.startMoveTicks = startMoveTicks;
            moveData.pathString = pathString;
           
            //群发消息
            SendToClients(sl, pool, null, objsList, DataHelper.ObjectToBytes<SpriteNotifyOtherMoveData>(moveData), cmd);
            //for (int i = 0; i < objsList.Count; i++)
            //{
            //        GameClient c = objsList[i] as GameClient;
            //        if (null == c)
            //        {
            //            continue;
            //        }
            //        System.Console.WriteLine(String.Format("Send to {0} Monster {1} Move From ({2},{3}) To ({4},{5})",
            //            c.ClientData.RoleName, roleID, currentX, currentY, toX, toY));
            //}
            //* tmp
            // 测试 拦截
            //PerformanceTest.NotifyOthersToMovingForMonsterTest(sl, pool, obj, mapCode, copyMapID, roleID, startMoveTicks, currentX, currentY, action, toX, toY, cmd, moveCost, pathString, objsList);

            return true;
        }
        public bool NotifyOthersToMoving(SocketListener sl, TCPOutPacketPool pool, IObject obj, int mapCode, int copyMapID, int roleID, int targetID,long startMoveTicks, int currentX,
           int currentY, int action, int toX, int toY, int cmd, double moveCost = 1.0, String pathString = "", List<Object> objsList = null)
        {
            if (null == objsList)
            {
                if (null == obj)
                {
                    objsList = Global.GetAll9Clients2(mapCode, currentX, currentY, copyMapID);
                }
                else
                {
                    objsList = Global.GetAll9Clients(obj);
                }
            }
            //if(mapCode == 1)
            //{
            //    SysConOut.WriteLine(string.Format("怪物移动寻路{0}   X={1}  Y={2}", roleID, currentX, currentY));
            //}
            if (null == objsList) return true;
            SpriteMove szSpriteMove = new SpriteMove();
            szSpriteMove.RoleID = roleID;
            szSpriteMove.MapCode = mapCode;
            szSpriteMove.action = action;
            szSpriteMove.toX = toX;
            szSpriteMove.toY = toY;
            szSpriteMove.extAction = 0;
            szSpriteMove.fromX = currentX;
            szSpriteMove.fromY = currentY;
            szSpriteMove.startMoveTicks = startMoveTicks;
            szSpriteMove.pathString = pathString;
            szSpriteMove.targetRoleID = targetID;
            //if (toX <= 0 || toY <= 0 || currentX <= 0 || currentX <= 0)
            LogManager.WriteLog(LogTypes.Robot, string.Format("移动消息ID = {0}   px={1} py={2} fx={3} fy={4}", roleID, toX, toY, currentX, currentX));
           // LogManager.WriteLog(LogTypes.Error, string.Format("移动消息ID = {0}   px={1} py={2} fx={3} fy={4}", roleID, toX, toY, currentX, currentX));
            //SysConOut.WriteLine(string.Format("通知其他人怪物移动 ID = {0} x={1} y={2}", roleID, toX, toY));
            SendProtocolToClients<SpriteMove>(sl, pool, null, objsList, szSpriteMove, cmd);

            return true;
        }

        /// <summary>
        /// 通知自己怪物的当前加载准备状态
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="otherRoleID"></param>
        /// <param name="mapCode"></param>
        /// <param name="action"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cmd"></param>
        /// <param name="moveCost"></param>
        /// <param name="extAction"></param>
        public void NotifyMyselfMonsterLoadAlready(SocketListener sl, TCPOutPacketPool pool, GameClient client, int monsterID, int mapCode, long startMoveTicks, int currentX, int currentY, int currentDirection, int action, int toX, int toY, double moveCost = 1.0, int extAction = 0, String pathString = "", int currentPathIndex = 0)
        {
            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}", monsterID, mapCode, currentX, currentY, currentDirection, action, toX, toY, moveCost, extAction, startMoveTicks, pathString, currentPathIndex);//怪物路径列表默认是空

            LoadAlreadyData loadAlreadyData = new LoadAlreadyData()
            {
                RoleID = monsterID,
                MapCode = mapCode,
                StartMoveTicks = startMoveTicks,
                CurrentX = currentX,
                CurrentY = currentY,
                CurrentDirection = currentDirection,
                Action = action,
                ToX = toX,
                ToY = toY,
                MoveCost = moveCost,
                ExtAction = extAction,
                PathString = pathString,
                CurrentPathIndex = currentPathIndex,
            };

            TCPOutPacket tcpOutPacket = null;
            //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_LOADALREADY);
            byte[] bytes = DataHelper.ObjectToBytes<LoadAlreadyData>(loadAlreadyData);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_LOADALREADY);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知自己怪物的移动
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="otherRoleID"></param>
        /// <param name="mapCode"></param>
        /// <param name="action"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cmd"></param>
        /// <param name="moveCost"></param>
        /// <param name="extAction"></param>
        public void NotifyMyselfMonsterMoving(SocketListener sl, TCPOutPacketPool pool, GameClient client, int monsterID, int mapCode, int action, long startMoveTicks, int fromX, int fromY, int toX, int toY, int cmd, double moveCost = 1.0, int extAction = 0, String pathString = "")
        {

            List<object> objsList = new List<object>();
            objsList.Add(client);

            SpriteMove szSpriteMove = new SpriteMove();
            szSpriteMove.RoleID = monsterID;
            szSpriteMove.MapCode = mapCode;
            szSpriteMove.action = action;
            szSpriteMove.toX = toX;
            szSpriteMove.toY = toY;
            szSpriteMove.extAction = extAction;
            szSpriteMove.fromX = fromX;
            szSpriteMove.fromY = fromY;
            szSpriteMove.startMoveTicks = startMoveTicks;
            szSpriteMove.pathString = pathString;
            szSpriteMove.targetRoleID = 0;
            if (toX <= 0 || toY <= 0 || fromX <= 0 || fromY <= 0)
                LogManager.WriteLog(LogTypes.Error,string.Format("移动消息ID = {0}   px={1} py={2} fx={3} fy={4}", monsterID, toX, toY, fromX, fromY));
          //  SysConOut.WriteLine("Role ID = " + monsterID.ToString());
            SendProtocolToClients<SpriteMove>(sl, pool, null, objsList, szSpriteMove, cmd);
            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", monsterID, mapCode, action, toX, toY, moveCost, extAction, fromX, fromY, startMoveTicks, pathString);//怪物宠物镖车等的移动历史路径为空
            //SpriteNotifyOtherMoveData moveData = new SpriteNotifyOtherMoveData();
            //moveData.roleID = monsterID;
            //moveData.mapCode = mapCode;
            //moveData.action = action;
            //moveData.toX = toX;
            //moveData.toY = toY;
            //moveData.moveCost = moveCost;
            //moveData.extAction = extAction;
            //moveData.fromX = fromX;
            //moveData.fromY = fromY;
            //moveData.startMoveTicks = startMoveTicks;
            //moveData.pathString = pathString;
            //TCPOutPacket tcpOutPacket = null;
            ////tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, cmd);
            //byte[] bytes = DataHelper.ObjectToBytes<SpriteNotifyOtherMoveData>(moveData);
            //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytes, 0, bytes.Length, cmd);
            //if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            //{
            //    //
            //    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
            //        tcpOutPacket.PacketCmdID,
            //        tcpOutPacket.PacketDataSize,
            //        client.ClientData.RoleID,
            //        client.ClientData.RoleName));*/
            //}
        }

        /// <summary>
        /// 通知自己怪物们的的移动
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="objsList"></param>
        public void NotifyMyselfMonstersMoving(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is Monster))
                {
                    continue;
                }

                //判断如果是在移动中
                if ((objsList[i] as Monster).SafeAction == GActions.Walk || (objsList[i] as Monster).SafeAction == GActions.Run)
                {
                    Monster monster = objsList[i] as Monster;

                    //通知自己其他人的移动
                    GameManager.ClientMgr.NotifyMyselfMonsterMoving(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                        (objsList[i] as Monster).RoleID,
                        (objsList[i] as Monster).MonsterZoneNode.MapCode,
                        (int)(objsList[i] as Monster).SafeAction,
                        Global.GetMonsterStartMoveTicks((objsList[i] as Monster)),
                        (int)(objsList[i] as Monster).SafeCoordinate.X,
                        (int)(objsList[i] as Monster).SafeCoordinate.Y,
                        (int)(objsList[i] as Monster).DestPoint.X,
                        (int)(objsList[i] as Monster).DestPoint.Y,
                        (int)CommandID.CMD_GAME_MOVE, (objsList[i] as Monster).MoveSpeed, 0, ""/*null != monster? monster.PathString : ""*/);
                       SysConOut.WriteLine("---->怪物移动 " + (objsList[i] as Monster).RoleID.ToString());
                }
            }
        }

        #endregion 加载和移动通知

        #region 动作通知

        /// <summary>
        /// 通知其他人自己开始做动作(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersMyAction(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, int mapCode, int direction, int action, int x, int y, int targetX, int targetY, int yAngle, int moveToX, int moveToY, int cmd)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", roleID, mapCode, direction, action, x, y, targetX, targetY, yAngle, moveToX, moveToY);

            SpriteActionData cmdData = new SpriteActionData();
            cmdData.roleID = roleID;
            cmdData.mapCode = mapCode;
            cmdData.direction = direction;
            cmdData.action = action;
            cmdData.toX = x;
            cmdData.toY = y;
            cmdData.targetX = targetX;
            cmdData.targetY = targetY;
            cmdData.yAngle = yAngle;
            cmdData.moveToX = moveToX;
            cmdData.moveToY = moveToY;

            //群发消息
            SendToClients(sl, pool, null, objsList, /*strcmd*/  DataHelper.ObjectToBytes<SpriteActionData>(cmdData), cmd);
        }

        /// <summary>
        /// 通知其他人怪(宠物/卫兵)开始做动作(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersDoAction(SocketListener sl, TCPOutPacketPool pool, IObject obj, int mapCode, int copyMapID, int roleID, int direction, int action, int x, int y, int targetX, int targetY, int cmd, List<Object> objsList)
        {
            if (null == objsList)
            {
                if (null == obj)
                {
                    objsList = Global.GetAll9Clients2(mapCode, x, y, copyMapID);
                }
                else
                {
                    objsList = Global.GetAll9Clients(obj);
                }
            }

            if (null == objsList) return;

            //             string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", roleID, mapCode, direction, action, x, y, targetX, targetY, -1, 0, 0);
            // 
            //             //群发消息
            //             SendToClients(sl, pool, null, objsList, strcmd, cmd);


            SpriteActionData cmdData = new SpriteActionData();
            cmdData.roleID = roleID;
            cmdData.mapCode = mapCode;
            cmdData.direction = direction;
            cmdData.action = action;
            cmdData.toX = x;
            cmdData.toY = y;
            cmdData.targetX = targetX;
            cmdData.targetY = targetY;
            cmdData.yAngle = -1;
            cmdData.moveToX = 0;
            cmdData.moveToY = 0;

            //群发消息
            SendToClients(sl, pool, null, objsList, /*strcmd*/  DataHelper.ObjectToBytes<SpriteActionData>(cmdData), cmd);
        }

        #endregion 动作通知

        #region 旋转角度通知

        /// <summary>
        /// 通知其他人自己开始旋转的角度(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersChangeAngle(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, int direction, int yAngle, int cmd)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}", roleID, direction, yAngle);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, cmd);
        }

        #endregion 旋转角度通知

        #region 技能使用

        /// <summary>
        /// 通知其他人自己开始做动作的准备工作(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersMagicCode(SocketListener sl, TCPOutPacketPool pool, IObject attacker, int roleID, int mapCode, int magicCode, int cmd)
        {
            List<Object> objsList = Global.GetAll9Clients(attacker);
            if (null == objsList) return;

            //string strcmd = string.Format("{0}:{1}:{2}", roleID, mapCode, magicCode);
            SpriteMagicCodeData cmdData = new SpriteMagicCodeData();

            cmdData.roleID = roleID;
            cmdData.mapCode = mapCode;
            cmdData.magicCode = magicCode;

            //群发消息
            SendToClients(sl, pool, attacker, objsList, /*strcmd*/DataHelper.ObjectToBytes(cmdData), cmd);
        }

        #endregion 技能使用

        #region 伤害命中通知

        /// <summary>
        /// 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySpriteHited(SocketListener sl, TCPOutPacketPool pool, IObject attacker, int enemy, int enemyX, int enemyY, int magicCode)
        {
            List<Object> objsList = Global.GetAll9Clients(attacker);
            if (null == objsList) return;

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", attacker.GetObjectID(), enemy, enemyX, enemyY, magicCode);
            SpriteHitedData hitedData = new SpriteHitedData();

            hitedData.roleId = attacker.GetObjectID();
            hitedData.enemy = enemy;
            hitedData.magicCode = magicCode;
            if (enemy < 0)
            {
                hitedData.enemyX = enemyX;
                hitedData.enemyY = enemyY;
            }
            System.Console.WriteLine(String.Format("{0} 使用技能——NotifySpriteHited", hitedData.roleId));
            //2015-9-16消息流量优化
            if (!GameManager.FlagEnableHideFlags || !GameManager.HideFlagsMapDict.ContainsKey(attacker.CurrentMapCode))
            {
                SendToClients(sl, pool, null, objsList, DataHelper.ObjectToBytes<SpriteHitedData>(hitedData), (int)TCPGameServerCmds.CMD_SPR_HITED);
            }
            else
            {
                //2015-9-16消息流量优化,根据客户端的当前显示需要,客户端忽略其他人击中怪物的消息
                GSpriteTypes spriteType = Global.GetSpriteType((uint)enemy);
                GameClient client = attacker as GameClient;
                if (null != client)
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_HITED, hitedData);
                }

                if (spriteType == GSpriteTypes.Other && (null != client || GameManager.FlagHideFlagsType == 0))
                {
                    SendToClients(sl, pool, attacker, objsList, DataHelper.ObjectToBytes<SpriteHitedData>(hitedData), (int)TCPGameServerCmds.CMD_SPR_HITED, ClientHideFlags.HideOtherMagicAndInjured, enemy);
                }
            }

            //加入到地图上去的持久特效
            AddDelayDecoToMap(attacker, magicCode, attacker.CurrentMapCode, attacker.CurrentCopyMapID, enemyX, enemyY);
        }

        /// <summary>
        /// 加入到地图上去的持久特效
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        public void AddDelayDecoToMap(IObject attacker, int magicCode, int mapCode, int copyMapID, int posX, int posY)
        {
            //首先判断技能是群攻还是单攻
            SystemXmlItem systemMagic = null;
            if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(magicCode, out systemMagic))
            {
                return;
            }

            if (systemMagic.GetIntValue("DelayDecoToMap") <= 0)
            {
                return;
            }

            string magicTimes = systemMagic.GetStringValue("MagicTime");
            if (string.IsNullOrEmpty(magicTimes))
            {
                return;
            }
            string[] magicTimeFields = magicTimes.Split(',');
            if (magicTimeFields.Length <= 0)
            {
                return;
            }

            SkillData skillData = null;
            if (attacker is GameClient)
            {
                skillData = Global.GetSkillDataByID(attacker as GameClient, magicCode);
            }

            int magicTimeIndex = (null == skillData) ? 0 : skillData.SkillLevel - 1;
            magicTimeIndex = Math.Min(magicTimeIndex, magicTimeFields.Length - 1);
            int magicTime = Global.SafeConvertToInt32(magicTimeFields[magicTimeIndex]);
            if (magicTime <= 0)
            {
                return;
            }

            int delayDeco = (int)systemMagic.GetIntValue("DelayDecoration");
            if (delayDeco <= 0)
            {
                return;
            }

            if (1 == systemMagic.GetIntValue("DelayDecoToMap"))
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[mapCode];

                Point centerGridXY = new Point((int)(posX / gameMap.MapGridWidth), (int)(posY / gameMap.MapGridHeight));

                List<Point> pts = new List<Point>();
                pts.Add(centerGridXY);
                pts.Add(new Point(centerGridXY.X, centerGridXY.Y - 1));
                pts.Add(new Point(centerGridXY.X + 1, centerGridXY.Y));
                pts.Add(new Point(centerGridXY.X, centerGridXY.Y + 1));
                pts.Add(new Point(centerGridXY.X - 1, centerGridXY.Y));

                for (int i = 0; i < pts.Count; i++)
                {
                    ///障碍上边，不能放火墙
                    if (Global.InOnlyObs(ObjectTypes.OT_CLIENT, mapCode, (int)pts[i].X, (int)pts[i].Y))
                    {
                        continue;
                    }

                    //如果已经有人放了火墙，则不能再放
                    if (GameManager.GridMagicHelperMgr.ExistsMagicHelper(MagicActionIDs.FIRE_WALL, (int)pts[i].X, (int)pts[i].Y))
                    {
                        continue;
                    }

                    Point pos = new Point(pts[i].X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, pts[i].Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                    DecorationManager.AddDecoToMap(mapCode, copyMapID, pos, delayDeco, magicTime * 1000, 2000, true);
                }
            }
        }

        #endregion 伤害命中通知

        #region 复活相关

        /// <summary>
        /// 通知其他人主角要复活(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersRealive(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, int posX, int posY, int direction)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            //string strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, posX, posY, direction);
            MonsterRealiveData monsterRealiveData = new MonsterRealiveData()
            {
                RoleID = roleID,
                PosX = posX,
                PosY = posY,
                Direction = direction,
            };
            byte[] bytes = DataHelper.ObjectToBytes<MonsterRealiveData>(monsterRealiveData);

            //群发消息
            SendToClients(sl, pool, client, objsList, /*strcmd*/bytes, (int)TCPGameServerCmds.CMD_SPR_REALIVE);

            //通知队友自己要复活
            NotifyTeamRealive(sl, pool, roleID, posX, posY, direction);
        }

        /// <summary>
        /// 通知队友自己要复活
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamRealive(SocketListener sl, TCPOutPacketPool pool, int roleID, int posX, int posY, int direction)
        {
            //string strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, posX, posY, direction);

            //判断精灵是否组队中，如果是，则也通知九宫格之外的队友
            GameClient otherClient = FindClient(roleID);
            if (null != otherClient)
            {
                if (otherClient.ClientData.TeamID > 0)
                {
                    //查找组队数据
                    TeamData td = GameManager.TeamMgr.FindData(otherClient.ClientData.TeamID);
                    if (null != td)
                    {
                        List<int> roleIDsList = new List<int>();

                        //锁定组队数据
                        lock (td)
                        {
                            for (int i = 0; i < td.TeamRoles.Count; i++)
                            {
                                if (roleID == td.TeamRoles[i].RoleID)
                                {
                                    continue;
                                }

                                roleIDsList.Add(td.TeamRoles[i].RoleID);
                            }
                        }
                        TCPOutPacket tcpOutPacket = null;
                        try
                        {
                            for (int i = 0; i < roleIDsList.Count; i++)
                            {
                                GameClient gc = FindClient(roleIDsList[i]);
                                if (null == gc) continue;

                                if (null == tcpOutPacket)
                                {
                                    MonsterRealiveData monsterRealiveData = new MonsterRealiveData()
                                    {
                                        RoleID = roleID,
                                        PosX = posX,
                                        PosY = posY,
                                        Direction = direction,
                                    };
                                    byte[] bytes = DataHelper.ObjectToBytes<MonsterRealiveData>(monsterRealiveData);
                                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, /*strcmd*/bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_REALIVE);
                                }
                                if (!sl.SendData(gc.ClientSocket, tcpOutPacket, false))
                                {
                                    //
                                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                        tcpOutPacket.PacketCmdID,
                                        tcpOutPacket.PacketDataSize,
                                        gc.ClientData.RoleID,
                                        gc.ClientData.RoleName));*/
                                }
                            }
                        }
                        finally
                        {
                            PushBackTcpOutPacket(tcpOutPacket);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 通知自己要复活
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="roleID"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="direction"></param>
        /// <param name="cmd"></param>
        public void NotifyMySelfRealive(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, int posX, int posY, int direction)
        {
            //string strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, posX, posY, direction);
            MonsterRealiveData monsterRealiveData = new MonsterRealiveData()
            {
                RoleID = roleID,
                PosX = posX,
                PosY = posY,
                Direction = direction,
            };
            byte[] bytes = DataHelper.ObjectToBytes<MonsterRealiveData>(monsterRealiveData);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, /*strcmd*/bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_REALIVE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知所有在线用户某个怪物复活(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMonsterRealive(SocketListener sl, TCPOutPacketPool pool, IObject obj, int mapCode, int copyMapID, int roleID, int posX, int posY, int direction, List<Object> objsList)
        {
            if (null == objsList)
            {
                if (null == obj)
                {
                    objsList = Global.GetAll9Clients2(mapCode, posX, posY, copyMapID);
                }
                else
                {
                    objsList = Global.GetAll9Clients(obj);
                }
            }

            if (null == objsList) return;

            //string strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, posX, posY, direction);
            MonsterRealiveData monsterRealiveData = new MonsterRealiveData()
            {
                RoleID = roleID,
                PosX = posX,
                PosY = posY,
                Direction = direction,
            };

            //群发消息
            SendToClients(sl, pool, null, objsList, /*strcmd*/DataHelper.ObjectToBytes<MonsterRealiveData>(monsterRealiveData), (int)TCPGameServerCmds.CMD_SPR_REALIVE);
        }

        #endregion 复活相关

        #region 离线/离开地图相关

        /// <summary>
        /// 角色离线(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersLeave(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;

            // string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, (int)GSpriteTypes.Other);
            OthersLeaveMap szOthersLeaveMap = new OthersLeaveMap();
            szOthersLeaveMap.RoleID = client.ClientData.RoleID;
            szOthersLeaveMap.SpriteTypes = (int)GSpriteTypes.Other;
           // SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_LEAVE);
            //群发消息
            SendProtocolToClients<OthersLeaveMap>(sl, pool, null, objsList, szOthersLeaveMap, (int)CommandID.CMD_MAP_LEAVE);
        }

        /// <summary>
        /// 怪物离开(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersMonsterLeave(SocketListener sl, TCPOutPacketPool pool, Monster monster, List<Object> objsList)
        {
            if (null == objsList) return;
            OthersLeaveMap szOthersLeaveMap = new OthersLeaveMap();
            szOthersLeaveMap.RoleID = monster.RoleID;
            szOthersLeaveMap.SpriteTypes = (int)GSpriteTypes.Monster;
            // SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_LEAVE);
            //群发消息
            SendProtocolToClients<OthersLeaveMap>(sl, pool, null, objsList, szOthersLeaveMap, (int)CommandID.CMD_MAP_LEAVE);

            //string strcmd = string.Format("{0}:{1}", monster.RoleID, (int)GSpriteTypes.Monster);

            ////群发消息
            //SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_LEAVE);
        }

        /// <summary>
        /// 通知自己其他角色离线(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMyselfLeaveOthers(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                if (client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID) //跳过自己
                {
                    continue;
                }

                OthersLeaveMap szOthersLeaveMap = new OthersLeaveMap();
                szOthersLeaveMap.RoleID = (objsList[i] as GameClient).ClientData.RoleID;
                szOthersLeaveMap.SpriteTypes = (int)GSpriteTypes.Other;
                // SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_LEAVE);
                //群发消息
                SendProtocolToClients<OthersLeaveMap>(sl, pool, null, objsList, szOthersLeaveMap, (int)CommandID.CMD_MAP_LEAVE);

                //string strcmd = string.Format("{0}:{1}", (objsList[i] as GameClient).ClientData.RoleID, (int)GSpriteTypes.Other);

                //TCPOutPacket tcpOutPacket = null;
                //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_LEAVE);
                //if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                //{
                //    //
                //    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                //        tcpOutPacket.PacketCmdID,
                //        tcpOutPacket.PacketDataSize,
                //        client.ClientData.RoleID,
                //        client.ClientData.RoleName));*/
                //    break;
                //}
            }
        }

        /// <summary>
        /// 通知自己怪物离开自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMyselfLeaveMonsters(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is Monster))
                {
                    continue;
                }

                //通知自己怪物离开自己(同一个地图才需要通知)
                if (!NotifyMyselfLeaveMonsterByID(sl, pool, client, (objsList[i] as Monster).RoleID))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 通知自己怪物离开自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public bool NotifyMyselfLeaveMonsterByID(SocketListener sl, TCPOutPacketPool pool, GameClient client, int monsterID)
        {
            //System.Diagnostics.Debug.WriteLine(string.Format("九宫格: 发送删除怪物给客户端: {0}, {1}", (objsList[i] as Monster).VSName, (objsList[i] as Monster).Name));
            OthersLeaveMap szOthersLeaveMap = new OthersLeaveMap();
            szOthersLeaveMap.RoleID = monsterID;
            szOthersLeaveMap.SpriteTypes = (int)GSpriteTypes.Monster;
            // SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_LEAVE);
            List<Object> objsList = new List<object>();
            objsList.Add(client);
            //群发消息
            SendProtocolToClients<OthersLeaveMap>(sl, pool, null, objsList, szOthersLeaveMap, (int)CommandID.CMD_MAP_LEAVE);

            //string strcmd = string.Format("{0}:{1}", monsterID, (int)GSpriteTypes.Monster);

            //TCPOutPacket tcpOutPacket = null;
            //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_LEAVE);
            //if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            //{
            //    //
            //    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
            //        tcpOutPacket.PacketCmdID,
            //        tcpOutPacket.PacketDataSize,
            //        client.ClientData.RoleID,
            //        client.ClientData.RoleName));*/
            //    return false;
            //}

            return true;
        }

        #endregion 离线/离开地图相关

        #region 角色死亡

        /// <summary>
        /// 判断如果对方已经无血，但是还存活着，则立刻发送死亡消息
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void JugeSpriteDead(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            if (client.ClientData.CurrentLifeV > 0)
            {
                return;
            }

            GameManager.SystemServerEvents.AddEvent(string.Format("角色强制死亡, roleID={0}({1}), Life={2}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.CurrentLifeV), EventLevels.Debug);
            NotifySpriteInjured(sl, pool, client, client.ClientData.MapCode, -1, client.ClientData.RoleID, 0, 0, 0, client.ClientData.Level, new Point(-1, -1));
        }

        #endregion 角色死亡

        #region 角色生命值变化

        /// <summary>
        /// 仅通知自己HP、MP变化 [LiaoWei 08/02/2013]        
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfLifeChanged(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            //             string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, client.ClientData.LifeV, client.ClientData.MagicV,
            //                                             client.ClientData.CurrentLifeV, client.ClientData.CurrentMagicV);
            SpriteLifeChangeData lifeChangeData = new SpriteLifeChangeData();

            lifeChangeData.roleID = client.ClientData.RoleID;
            lifeChangeData.lifeV = client.ClientData.LifeV;
            lifeChangeData.magicV = client.ClientData.MagicV;
            lifeChangeData.currentLifeV = client.ClientData.CurrentLifeV;
            lifeChangeData.currentMagicV = client.ClientData.CurrentMagicV;

            byte[] cmdData = DataHelper.ObjectToBytes<SpriteLifeChangeData>(lifeChangeData);

            SendToClient(client, /*strcmd*/cmdData, (int)TCPGameServerCmds.CMD_SPR_UPDATE_ROLEDATA);
        }

        /// <summary>
        /// 通知其他人怪物开始回血回魔(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public bool NotifyOthersRelife(SocketListener sl, TCPOutPacketPool pool, IObject obj, int mapCode, int copyMapID, int roleID, int x, int y, int direction, double lifeV, double magicV, int cmd, List<Object> objsList, int force = 0)
        {
            if (null == objsList)
            {
                if (null == obj)
                {
                    objsList = Global.GetAll9Clients2(mapCode, x, y, copyMapID);
                }
                else
                {
                    objsList = Global.GetAll9Clients(obj);
                }
            }

            if (null == objsList) return true;

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", roleID, x, y, direction, lifeV, magicV, force);
            SpriteRelifeData relifeData = new SpriteRelifeData();
            relifeData.roleID = roleID;
            relifeData.direction = direction;
            relifeData.lifeV = lifeV;
            relifeData.magicV = magicV;
            relifeData.force = force;

            //2015-9-16消息流量优化
            if (!GameManager.FlagEnableHideFlags)
            {
                relifeData.x = x;
                relifeData.y = y;
            }

            //群发消息
            SendToClients(sl, pool, null, objsList, /*strcmd*/DataHelper.ObjectToBytes<SpriteRelifeData>(relifeData), cmd);

            return true;
        }

        /// <summary>
        /// 玩家满血满蓝
        /// </summary>
        /// <param name="client"></param>
        public void UserFullLife(GameClient client, string reason, bool allSend = true)
        {
            RoleRelifeLog relifeLog = new RoleRelifeLog(client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.MapCode, reason);
            relifeLog.hpModify = true;
            relifeLog.mpModify = true;
            relifeLog.oldHp = client.ClientData.CurrentLifeV;
            relifeLog.oldMp = client.ClientData.CurrentMagicV;
            client.ClientData.CurrentLifeV = client.ClientData.LifeV;
            client.ClientData.CurrentMagicV = client.ClientData.MagicV;
            relifeLog.newHp = client.ClientData.CurrentLifeV;
            relifeLog.newMp = client.ClientData.CurrentMagicV;
            MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, allSend);
        }

        /// <summary>
        /// 总生命值和魔法值变化通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersLifeChanged(SocketListener sl, TCPOutPacketPool pool, GameClient client, bool allSend = true, bool resetMax = false)
        {
            //计算用户的生命值和魔法值
            client.ClientData.LifeV = (int)RoleAlgorithm.GetMaxLifeV(client);
            client.ClientData.MagicV = (int)RoleAlgorithm.GetMaxMagicV(client);

            if (!resetMax)
            {
                client.ClientData.CurrentLifeV = Global.GMin(client.ClientData.CurrentLifeV, client.ClientData.LifeV);
                client.ClientData.CurrentMagicV = Global.GMin(client.ClientData.CurrentMagicV, client.ClientData.MagicV);
            }
            else
            {
                client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                client.ClientData.CurrentMagicV = client.ClientData.MagicV;
            }

            //             string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, client.ClientData.LifeV, client.ClientData.MagicV,
            //                 client.ClientData.CurrentLifeV, client.ClientData.CurrentMagicV);
            SpriteLifeChangeData lifeChangeData = new SpriteLifeChangeData();

            lifeChangeData.roleID = client.ClientData.RoleID;
            lifeChangeData.lifeV = client.ClientData.LifeV;
            lifeChangeData.magicV = client.ClientData.MagicV;
            lifeChangeData.currentLifeV = client.ClientData.CurrentLifeV;
            lifeChangeData.currentMagicV = client.ClientData.CurrentMagicV;

            byte[] cmdData = DataHelper.ObjectToBytes<SpriteLifeChangeData>(lifeChangeData);

            if (!allSend)
            {
                if (null != client)
                {
                    TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, /*strcmd*/cmdData, 0, cmdData.Length, (int)TCPGameServerCmds.CMD_SPR_UPDATE_ROLEDATA);
                    if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                    {
                        //
                        /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                            tcpOutPacket.PacketCmdID,
                            tcpOutPacket.PacketDataSize,
                            client.ClientData.RoleID,
                            client.ClientData.RoleName));*/
                    }
                }

                return;
            }

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            //群发消息
            SendToClients(sl, pool, null, objsList, /*strcmd*/cmdData, (int)TCPGameServerCmds.CMD_SPR_UPDATE_ROLEDATA);
        }

        #endregion 角色生命值变化

        #region 回城通知

        /// <summary>
        /// 通知其他人(包括自己)回城(同一个地图才需要通知)
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="roleID"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="direction"></param>
        /// <param name="cmd"></param>
        public void NotifyOthersGoBack(SocketListener sl, TCPOutPacketPool pool, GameClient client, int toPosX = -1, int toPosY = -1, int direction = -1)
        {
            if ("1" == GameManager.GameConfigMgr.GetGameConfigItemStr("log-changmap", "0"))
            {
                if (client.ClientData.LastChangeMapTicks >= TimeUtil.NOW() - 12000)
                {
                    try
                    {
                        DataHelper.WriteStackTraceLog(string.Format(Global.GetLang("地图传送频繁,记录堆栈信息备查 role={3}({4}) toMapCode={0} pt=({1},{2})"),
                            client.ClientData.MapCode, toPosX, toPosY, client.ClientData.RoleName, client.ClientData.RoleID));
                    }
                    catch (Exception) { }
                }
            }
            client.ClientData.LastChangeMapTicks = TimeUtil.NOW();

            int defaultBirthPosX = GameManager.MapMgr.DictMaps[client.ClientData.MapCode].DefaultBirthPosX;
            int defaultBirthPosY = GameManager.MapMgr.DictMaps[client.ClientData.MapCode].DefaultBirthPosY;
            int defaultBirthRadius = GameManager.MapMgr.DictMaps[client.ClientData.MapCode].BirthRadius;

            int posX = toPosX;
            int posY = toPosY;

            //如果外部不配置坐标，则回到复活点
            if (-1 == posX || -1 == posY)
            {
                //从配置根据地图取默认位置
                Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, client.ClientData.MapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
                posX = (int)newPos.X;
                posY = (int)newPos.Y;
            }

            if (direction >= 0)
            {
                client.ClientData.RoleDirection = direction;
            }

            GameManager.ClientMgr.ChangePosition(sl, pool,
                client, (int)posX, (int)posY, direction, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
        }

        #endregion 回城通知

        #region 角色换装

        /// <summary>
        /// 更换衣服和武器(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        //public void NotifyOthersChangeCode(SocketListener sl, TCPOutPacketPool pool, GameClient client, int bodyCode, int weaponCode, int refreshNow)
        //{
        //    List<Object> objsList = Global.GetAll9Clients(client);
        //    if (null == objsList) return;

        //    string strcmd = string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, bodyCode, weaponCode, refreshNow);

        //    //群发消息
        //    SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGCODE);
        //}

        public void NotifyOthersChangeEquip(SocketListener sl, TCPOutPacketPool pool, GameClient client, GoodsData goodsData, int refreshNow, WingData usingWinData = null)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            ChangeEquipData changeEquipData = new ChangeEquipData()
            {
                RoleID = client.ClientData.RoleID,
                EquipGoodsData = goodsData,
                UsingWinData = usingWinData,
            };

            byte[] bytesData = DataHelper.ObjectToBytes<ChangeEquipData>(changeEquipData);

            //群发消息
            SendToClients(sl, pool, null, objsList, bytesData, (int)TCPGameServerCmds.CMD_SPR_CHGCODE);
        }

        #endregion 角色换装

        #region 角色PK模式

        /// <summary>
        /// PK模式变化通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersPKModeChanged(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.PKMode);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGPKMODE);
        }

        #endregion 角色PK模式

        #region 角色退出

        /// <summary>
        /// 客户端离线
        /// </summary>
        public void Logout(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            try
            {
             
                //触发玩家退出事件
                GlobalEventSource.getInstance().fireEvent(new PlayerLogoutEventObject(client));

              

                //执行指定的数据库命令
                Global.ProcessDBCmdByTicks(client, true);

                //执行指定的技能数据库命令
                Global.ProcessDBSkillCmdByTicks(client, true);

                //执行指定的角色参数数据库命令
                Global.ProcessDBRoleParamCmdByTicks(client, true);

              

                SceneUIClasses sceneType = Global.GetMapSceneType(client.ClientData.MapCode);
                KuaFuManager.getInstance().OnLeaveScene(client, sceneType);
                KuaFuManager.getInstance().OnLogout(client);

              

                SysConOut.WriteLine(string.Format("{0}/{1} [MapCode = {2},PosX = {3} ,PosY = {4}]离线",
                    client.ClientData.RoleID, client.ClientData.RoleName,
                    client.ClientData.MapCode, client.ClientData.PosX, client.ClientData.PosY));

                //发送在线时长计算
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEONLINETIME,
                    string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.TotalOnlineSecs, client.ClientData.AntiAddictionSecs),
                    null, client.ServerId);

              
                //从队列中删除
                RemoveClient(client);
                
                List<Object> objsList = Global.GetAll9GridObjects(client);

                //玩家进行了移动, 处理旧对象
                Global.GameClientHandleOldObjs(client, objsList);

                /// 清空角色的可见列表
                client.ClearVisibleObjects(true);

                //HX_SERVER 从可见队列中删除
                Global.RemoveFromAddObjsList(client);

                /// 切换地图或者退出时清空副本
                Global.ClearCopyMap(client, true);

                /// 解锁掉落物品的包裹
                GameManager.GoodsPackMgr.UnLockGoodsPackItem(client);

            

                // 角色名称到ID的映射
              //  RoleName2IDs.RemoveRoleName(Global.FormatRoleName(client, client.ClientData.RoleName));

                //断开时处理交易数据
                ProcessExchangeData(sl, pool, client);
                client.ClientData.CurrentLifeV = 10000;
                //判断如果是死亡状态则，强迫设置IP为错误，强迫回城
                if (client.ClientData.CurrentLifeV <= 0)
                {
                    client.ClientData.MapCode = -1;
                    client.ClientData.PosX = -1;
                    client.ClientData.PosY = -1;
                    client.ClientData.ReportPosTicks = 0;
                }

                // 从水晶地图下线回到勇者大陆
                if (sceneType == SceneUIClasses.ShuiJingHuanJing)
                {
                    client.ClientData.MapCode = -1;
                    client.ClientData.PosX = -1;
                    client.ClientData.PosY = -1;
                    client.ClientData.ReportPosTicks = 0;
                }

                //是否记录位置信息
                if (Global.CanRecordPos(client))
                {
                   
                    //异步写数据库，写入当前的位置
                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_POS,
                        string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, client.ClientData.MapCode,
                        client.ClientData.RoleDirection, client.ClientData.PosX, client.ClientData.PosY),
                        null, client.ServerId);
                }

                //更新离线状态
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ROLE_OFFLINE,
                    string.Format("{0}:{1}:{2}:{3}:{4}",
                    client.ClientData.RoleID,
                    GameManager.ServerLineID,
                    Global.GetSocketRemoteIP(client),
                    client.ClientData.OnlineActiveVal,
                    TimeUtil.NOW()),
                    null, client.ServerId);

                //写入角色登出的行为日志
                Global.AddRoleLogoutEvent(client);
                // 重置玩家外挂数据
                RobotTaskValidator.getInstance().RobotDataReset(client);

                // 给每个登出的玩家留x秒马上进入 如果玩家掉线了 不在x秒之内init_game 就对不起了 排队去吧
                GameManager.loginWaitLogic.AddToAllow(client.strUserID,
                    GameManager.loginWaitLogic.GetConfig(LoginWaiting.LoginWaitLogic.UserType.Normal, LoginWaiting.LoginWaitLogic.ConfigType.LogouAllowMSeconds));

                // 下线增加几分钟的免验证二级密码时间
                SecondPasswordManager.OnUsrLogout(client.strUserID);
                SpeedUpTickCheck.Instance().OnLogout(client);
                CoupleArenaManager.Instance().OnClientLogout(client);

                try
                {
                    string ip = RobotTaskValidator.getInstance().GetIp(client);

                    string analysisLog = string.Format("logout server={0} account={1} player={2} dev_id={3} exp={4}", GameManager.ServerId, client.strUserID,
                        client.ClientData.RoleID, string.IsNullOrEmpty(client.deviceID) ? "" : client.deviceID, ip);
                    LogManager.WriteLog(LogTypes.Analysis, analysisLog);
                }
                catch { }

                //是否已经退出了
                client.LogoutState = true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }
        }

        private void SaleGoodsToOfflineSale(GameClient client)
        {
            //如果将要去往跨服活动,为防止财务丢失,挂售物品下架,不上离线挂售
            if (client.ClientSocket.ClientKuaFuServerLoginData.RoleId <= 0 && !client.CheckCheatData.IsKickedRole)
            {
                //判断如果允许离线挂机，则要填充离线挂机的数据
                if (Global.Flag_MUSale)
                {
                    LiXianBaiTanManager.AddLiXianSaleGoodsItems(client, -1);
                }
                else
                {
                    if (client.ClientData.OfflineMarketState > 0 && GameManager.ClientMgr.GetLiXianBaiTanTicksValue(client) > 0)
                    {
                        int fakeRoleID = FakeRoleManager.ProcessNewFakeRole(client.ClientData, client.ClientData.MapCode, FakeRoleTypes.LiXiaBaiTan, 4, (int)client.ClientData.PosX, (int)client.ClientData.PosY, 0);
                        if (fakeRoleID > 0)
                        {
                            LiXianBaiTanManager.AddLiXianSaleGoodsItems(client, fakeRoleID);
                        }
                    }
                }
            }
        }

        private void ProcessFakeRoleForLiXianGuaJi(GameClient client)
        {
            //如果将要去往跨服活动,禁用离线挂机(冥想)
            if (client.ClientSocket.ClientKuaFuServerLoginData.RoleId <= 0 && !client.CheckCheatData.IsKickedRole)
            {
                int fakeRoleID = 0;
                if (GameManager.FlagLiXianGuaJi > 0)
                {
                    fakeRoleID = FakeRoleManager.ProcessNewFakeRole(client.ClientData, client.ClientData.MapCode, FakeRoleTypes.LiXianGuaJi, -1, (int)client.ClientData.PosX, (int)client.ClientData.PosY, 0);
                }

                LiXianGuaJiManager.AddLiXianGuaJiRole(client, fakeRoleID);
            }
        }

        #endregion 角色退出

        #region 任务相关

        /// <summary>
        /// 任务更新通知
        /// </summary>
        public void NotifyUpdateTask(SocketListener sl, TCPOutPacketPool pool, GameClient client, int dbID, int taskID, int taskVal1, int taskVal2, int taskFocus)
        {
            //
            string strcmd = "";
            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", dbID, taskID, taskVal1, taskVal2, taskFocus);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_MODTASK);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// NPC的任务状态更新通知
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="npcID"></param>
        /// <param name="state"></param>
        public void NotifyUpdateNPCTaskSate(SocketListener sl, TCPOutPacketPool pool, GameClient client, int npcID, int state)
        {
            //
            string strcmd = "";
            strcmd = string.Format("{0}:{1}", npcID, state);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_UPDATENPCSTATE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 将NPC的状态列表通知客户端
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="npcTaskStatList"></param>
        public void NotifyNPCTaskStateList(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<NPCTaskState> npcTaskStatList)
        {
            //
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<NPCTaskState>>(npcTaskStatList, pool, (int)TCPGameServerCmds.CMD_SPR_NPCSTATELIST);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 给与新手任务 [XSea 2015/4/14]
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="client">角色</param>
        /// <param name="nNeedTakeStartTask">是否需要起始任务</param>
        /// <returns>true=成功，false=失败</returns>
        public bool GiveFirstTask(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, TCPRandKey tcpRandKey, GameClient client, bool bNeedTakeStartTask)
        {
            // 判空
            if (null == client)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("client不存在，无法给与新手任务"));
                return false;
            }

            // 角色id
            int nRoleID = client.ClientData.RoleID;

            try
            {
                // 给与魔剑士新手任务 [XSea 2015/4/9]
                if (null == Global.GetTaskData(client, MagicSwordData.InitTaskID)
                    && GameManager.MagicSwordMgr.IsFirstLoginMagicSword(client, MagicSwordData.InitChangeLifeCount))
                {
                    // 循环将魔剑士初始任务以前的任务标记为已完成
                    int tmpRes = GameManager.ClientMgr.AutoCompletionTaskByTaskID(tcpMgr, tcpClientPool, pool, tcpRandKey, client, MagicSwordData.InitPrevTaskID);

                    // 失败
                    if (tmpRes != 0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士任务初始化失败，无法创建魔剑士, RoleID={0}", nRoleID));
                        return false;
                    }

                    // 这里的 需要等以前任务标记初始化完成才可以执行
                    client.ClientData.MainTaskID = MagicSwordData.InitPrevTaskID; // 上一个完成的任务

                    // 将新手魔剑士放到专属场景
                    client.ClientData.MapCode = MagicSwordData.InitMapID;

                    TCPOutPacket tcpOutPacketTemp = null;
                    // 给与新手任务
                    Global.TakeNewTask(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, (int)TCPGameServerCmds.CMD_SPR_NEWTASK, client, nRoleID,
                        MagicSwordData.InitTaskID, MagicSwordData.InitTaskNpcID, out tcpOutPacketTemp);
                }
                // 给战士、法师、弓箭手新手任务
                else if (bNeedTakeStartTask && null == Global.GetTaskData(client, 1000) && !GameManager.MagicSwordMgr.IsMagicSword(client))
                {
                    // 新手场景id
                    client.ClientData.MainTaskID = 106;
                    TCPOutPacket tcpOutPacketTemp = null;
                    Global.AddOldTask(client, 106);
                    Global.TakeNewTask(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, (int)TCPGameServerCmds.CMD_SPR_NEWTASK, client, nRoleID, 1000, 60900, out tcpOutPacketTemp);
                }

                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }
            return false;
        }

        #endregion 任务相关

        #region 通知客户端角色的数值属性

        /// <summary>
        /// 获取属性字符串
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        //private string GetEquipPropsStr(GameClient client)
        private EquipPropsData GetEquipPropsStr(GameClient client)
        {
            // 属性改造 给客户端显示的内容 重新组织下 [8/15/2013 LiaoWei]
            // 0. 人物ROLEID 1.力量 2.智力 3.体力 4.敏捷 5.最小物理攻击力 6.最大物理攻击力 7.最小物理防御 8.最大物理防御 9.魔法技能增幅 10.最小魔法攻击力 
            // 11.最大魔法攻击力 12.最小魔法防御力 13.最大魔法防御力 14. 物理技能增幅 15.生命上限 16.魔法上限 17.攻击速度 18.命中 19.闪避 20.总属性点 21.转生计数, 22.战斗力

            client.propsCacheModule.ResetAllProps();
            AdvanceBufferPropsMgr.DoSpriteBuffers(client);

            double nMinAttack = RoleAlgorithm.GetMinAttackV(client);

            double nMaxAttack = RoleAlgorithm.GetMaxAttackV(client);
            //LogManager.WriteLog(LogTypes.Error, string.Format("--------------nMaxAttack={0}", nMaxAttack));
            double nMinDefense = RoleAlgorithm.GetMinADefenseV(client);

            double nMaxDefense = RoleAlgorithm.GetMaxADefenseV(client);

            double nMinMAttack = RoleAlgorithm.GetMinMagicAttackV(client);

            double nMaxMAttack = RoleAlgorithm.GetMaxMagicAttackV(client);

            double nMinMDefense = RoleAlgorithm.GetMinMDefenseV(client);

            double nMaxMDefense = RoleAlgorithm.GetMaxMDefenseV(client);

            double nHit = RoleAlgorithm.GetHitV(client);

            double nDodge = RoleAlgorithm.GetDodgeV(client);

            double addAttackInjure = RoleAlgorithm.GetAddAttackInjureValue(client);

            double decreaseInjure = RoleAlgorithm.GetDecreaseInjureValue(client);

            double nMaxHP = RoleAlgorithm.GetMaxLifeV(client);

            double nMaxMP = RoleAlgorithm.GetMaxMagicV(client);

            double nLifeSteal = RoleAlgorithm.GetLifeStealV(client);

            // add元素属性战斗力 [XSea 2015/8/24]
            double dFireAttack = GameManager.ElementsAttackMgr.GetElementAttack(client, EElementDamageType.EEDT_Fire);
            double dWaterAttack = GameManager.ElementsAttackMgr.GetElementAttack(client, EElementDamageType.EEDT_Water);
            double dLightningAttack = GameManager.ElementsAttackMgr.GetElementAttack(client, EElementDamageType.EEDT_Lightning);
            double dSoilAttack = GameManager.ElementsAttackMgr.GetElementAttack(client, EElementDamageType.EEDT_Soil);
            double dIceAttack = GameManager.ElementsAttackMgr.GetElementAttack(client, EElementDamageType.EEDT_Ice);
            double dWindAttack = GameManager.ElementsAttackMgr.GetElementAttack(client, EElementDamageType.EEDT_Wind);

            // 战斗力 [12/17/2013 LiaoWei]  改成一项了 [3/5/2014 LiaoWei]
            //int nOccup = Global.CalcOriginalOccupationID(client);

            CombatForceInfo CombatForce = null;
            CombatForce = Data.CombatForceDataInfo[1];

            if (CombatForce != null)
            {
                double nValue = 0.0;

                nValue = (nMinAttack / CombatForce.MinPhysicsAttackModulus + nMaxAttack / CombatForce.MaxPhysicsAttackModulus) / 2 +
                         (nMinDefense / CombatForce.MinPhysicsDefenseModulus + nMaxDefense / CombatForce.MaxPhysicsDefenseModulus) / 2 +
                         (nMinMAttack / CombatForce.MinMagicAttackModulus + nMaxMAttack / CombatForce.MaxMagicAttackModulus) / 2 +
                         (nMinMDefense / CombatForce.MinMagicDefenseModulus + nMaxMDefense / CombatForce.MaxMagicDefenseModulus) / 2 +
                         addAttackInjure / CombatForce.AddAttackInjureModulus + decreaseInjure / CombatForce.DecreaseInjureModulus +
                         nHit / CombatForce.HitValueModulus + nDodge / CombatForce.DodgeModulus + nMaxHP / CombatForce.MaxHPModulus + nMaxMP / CombatForce.MaxMPModulus +
                         nLifeSteal / CombatForce.LifeStealModulus;

                // 元素属性 [XSea 2015/8/24]
                nValue += dFireAttack / CombatForce.FireAttack + dWaterAttack / CombatForce.WaterAttack +
                         dLightningAttack / CombatForce.LightningAttack + dSoilAttack / CombatForce.SoilAttack + dIceAttack / CombatForce.IceAttack +
                         dWindAttack / CombatForce.WindAttack;

                client.ClientData.CombatForce = (int)nValue;
                if (nValue >= Data.CombatForceLogMinValue && nValue >= client.ClientData.MaxCombatForce * (1 + Data.CombatForceLogPercent))
                {
                    client.ClientData.MaxCombatForce = (long)nValue;

                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        Global.PrintSomeProps(client, ref sb);
                        LogManager.WriteLog(LogTypes.Alert, sb.ToString());
                    }
                    catch (System.Exception ex)
                    {
                        LogManager.WriteException(ex.ToString());
                    }
                }
            }

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}:{13}:{14}:{15}:{16}:{17}:{18}:{19}:{20}:{21}:{22}:{23}:{24}:{25}:{26}",
            //    client.ClientData.RoleID,
            //    RoleAlgorithm.GetStrength(client),          // 1
            //    RoleAlgorithm.GetIntelligence(client),      // 2
            //    RoleAlgorithm.GetDexterity(client),         // 3
            //    RoleAlgorithm.GetConstitution(client),      // 4
            //    nMinAttack,                                 // 5
            //    nMaxAttack,                                 // 6
            //    nMinDefense,                                // 7
            //    nMaxDefense,                                // 8
            //    RoleAlgorithm.GetMagicSkillIncrease(client),// 9
            //    nMinMAttack,                                // 10
            //    nMaxMAttack,                                // 11
            //    nMinMDefense,                               // 12
            //    nMaxMDefense,                               // 13
            //    RoleAlgorithm.GetPhySkillIncrease(client),  // 14
            //    nMaxHP,                                     // 15
            //    nMaxMP,                                     // 16
            //    RoleAlgorithm.GetAttackSpeed(client),       // 17
            //    nHit,                                       // 18
            //    nDodge,                                     // 19
            //    Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint),// 20
            //    client.ClientData.ChangeLifeCount,          // 21
            //    client.ClientData.CombatForce, //Global.GetRoleParamsInt32FromDB(client, RoleParamName.sChangeLifeCount)// 22
            //    Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropStrengthChangeless) - DBRoleBufferManager.GetTimeAddProp(client, BufferItemTypes.ADDTEMPStrength),              // 23
            //    Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropIntelligenceChangeless) - DBRoleBufferManager.GetTimeAddProp(client, BufferItemTypes.ADDTEMPIntelligsence),     // 24
            //    Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropDexterityChangeless) - DBRoleBufferManager.GetTimeAddProp(client, BufferItemTypes.ADDTEMPDexterity),            // 25
            //    Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropConstitutionChangeless) - DBRoleBufferManager.GetTimeAddProp(client, BufferItemTypes.ADDTEMPConstitution)       // 26
            //    );

            int nStrength = DBRoleBufferManager.GetTimeAddProp(client, BufferItemTypes.ADDTEMPStrength);
            int nIntelligence = DBRoleBufferManager.GetTimeAddProp(client, BufferItemTypes.ADDTEMPIntelligsence);
            int nDexterity = DBRoleBufferManager.GetTimeAddProp(client, BufferItemTypes.ADDTEMPDexterity);
            int nConstitution = DBRoleBufferManager.GetTimeAddProp(client, BufferItemTypes.ADDTEMPConstitution);

            int addStrength = (int)RoleAlgorithm.GetStrength(client, false);
            int addIntelligence = (int)RoleAlgorithm.GetIntelligence(client, false);
            int addDexterity = (int)RoleAlgorithm.GetDexterity(client, false);
            int addConstitution = (int)RoleAlgorithm.GetConstitution(client, false);

            int addAll = addStrength + addIntelligence + addDexterity + addConstitution;

            EquipPropsData equipPropsData = new EquipPropsData()
            {
                RoleID = client.ClientData.RoleID,
                Strength = addStrength + nStrength,          // 1
                Intelligence = addIntelligence + nIntelligence,      // 2
                Dexterity = addDexterity + nDexterity,         // 3
                Constitution = addConstitution + nConstitution,      // 4
                MinAttack = nMinAttack,                                 // 5
                MaxAttack = nMaxAttack,                                 // 6
                MinDefense = nMinDefense,                                // 7
                MaxDefense = nMaxDefense,                                // 8
                MagicSkillIncrease = RoleAlgorithm.GetMagicSkillIncrease(client),// 9
                MinMAttack = nMinMAttack,                                // 10
                MaxMAttack = nMaxMAttack,                                // 11
                MinMDefense = nMinMDefense,                               // 12
                MaxMDefense = nMaxMDefense,                               // 13
                PhySkillIncrease = RoleAlgorithm.GetPhySkillIncrease(client),  // 14
                MaxHP = nMaxHP,                                     // 15
                MaxMP = nMaxMP,                                     // 16
                AttackSpeed = RoleAlgorithm.GetAttackSpeed(client),       // 17
                Hit = nHit,                                       // 18
                Dodge = nDodge,                                     // 19
                TotalPropPoint = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint),// 20
                ChangeLifeCount = client.ClientData.ChangeLifeCount,          // 21
                CombatForce = client.ClientData.CombatForce, //Global.GetRoleParamsInt32FromDB(client, RoleParamName.sChangeLifeCount)// 22
                TEMPStrength = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropStrengthChangeless),              // 23
                TEMPIntelligsence = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropIntelligenceChangeless),     // 24
                TEMPDexterity = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropDexterityChangeless),            // 25
                TEMPConstitution = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropConstitutionChangeless),       // 26
            };

            equipPropsData.TotalPropPoint += nStrength + nIntelligence + nDexterity + nConstitution;
            equipPropsData.TotalPropPoint += (int)client.ClientData.PropsCacheManager.GetBaseProp((int)UnitPropIndexes.Strength)
                + (int)client.ClientData.PropsCacheManager.GetBaseProp((int)UnitPropIndexes.Intelligence)
                + (int)client.ClientData.PropsCacheManager.GetBaseProp((int)UnitPropIndexes.Dexterity)
                + (int)client.ClientData.PropsCacheManager.GetBaseProp((int)UnitPropIndexes.Constitution);

            //return strcmd;
            return equipPropsData;
        }

        /// <summary>
        /// 装备属性更新通知
        /// </summary>
        public void NotifyUpdateEquipProps(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            //string strcmd = GetEquipPropsStr(client);
            //TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GETATTRIB2);

            EquipPropsData equipPropsData = GetEquipPropsStr(client);
            byte[] bytes = DataHelper.ObjectToBytes<EquipPropsData>(equipPropsData);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_GETATTRIB2);

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }

            //LogManager.WriteLog(LogTypes.Error, string.Format("-----------old={0},new={1},sub={2}",
            //       client.ClientData.LastNotifyCombatForce,
            //       client.ClientData.CombatForce,
            //       client.ClientData.LastNotifyCombatForce - client.ClientData.CombatForce));

            if (client.ClientData.CombatForce != client.ClientData.LastNotifyCombatForce)
            {
                //如果战力变化了,则通知给需要的人
                client.ClientData.LastNotifyCombatForce = client.ClientData.CombatForce;
                NotifyTeamCHGZhanLi(sl, pool, client);

                // 七日活动
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.CombatChange));
            }
        }

        /// <summary>
        /// 重量属性更新通知
        /// </summary>
        public void NotifyUpdateWeights(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            // 属性改造 去掉 重量属性[8/15/2013 LiaoWei]
            /* string strcmd = StringUtil.substitute("{0}:{1}:{2}:{3}", 
                 client.ClientData.RoleID,
                 client.ClientData.WeighItems.Weights[(int)WeightIndexes.HandWeight],
                 client.ClientData.WeighItems.Weights[(int)WeightIndexes.BagWeight],
                 client.ClientData.WeighItems.Weights[(int)WeightIndexes.DressWeight]
                 );

             TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_UPDATEWEIGHTS);
             if (!sl.SendData(client.ClientSocket, tcpOutPacket))
             {
                 //
                 /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                     tcpOutPacket.PacketCmdID,
                     tcpOutPacket.PacketDataSize,
                     client.ClientData.RoleID,
                     client.ClientData.RoleName));*/
            //}*/
        }

        /// <summary>
        /// 装备属性更新通知
        /// </summary>
        public void NotifyUpdateEquipProps(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient otherClient)
        {
            //
            //string strcmd = GetEquipPropsStr(otherClient);
            //TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GETATTRIB2);

            EquipPropsData equipPropsData = GetEquipPropsStr(otherClient);
            byte[] bytes = DataHelper.ObjectToBytes<EquipPropsData>(equipPropsData);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_GETATTRIB2);

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 通知客户端角色的数值属性

        #region 角色加减血

        /// <summary>
        /// 给某个客户端加血
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="addedVal"></param>
        public void AddSpriteLifeV(SocketListener sl, TCPOutPacketPool pool, GameClient c, double lifeV, string reason)
        {
            //如果已经死亡，则不再调度
            if (c.ClientData.CurrentLifeV <= 0)
            {
                return;
            }

            //判断如果血量少于最大血量
            if (c.ClientData.CurrentLifeV < c.ClientData.LifeV)
            {
                RoleRelifeLog relifeLog = new RoleRelifeLog(c.ClientData.RoleID, c.ClientData.RoleName, c.ClientData.MapCode, reason);
                relifeLog.hpModify = true;
                relifeLog.oldHp = c.ClientData.CurrentLifeV;
                c.ClientData.CurrentLifeV = (int)Global.GMin(c.ClientData.LifeV, c.ClientData.CurrentLifeV + lifeV);
                relifeLog.newHp = c.ClientData.CurrentLifeV;
                MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);
                //GameManager.SystemServerEvents.AddEvent(string.Format("角色加血, roleID={0}({1}), Add={2}, Life={3}", c.ClientData.RoleID, c.ClientData.RoleName, lifeV, c.ClientData.CurrentLifeV), EventLevels.Debug);

                //通知客户端怪已经加血加魔                                    
                List<Object> listObjs = Global.GetAll9Clients(c);
                GameManager.ClientMgr.NotifyOthersRelife(sl, pool, c, c.ClientData.MapCode, c.ClientData.CopyMapID, c.ClientData.RoleID, (int)c.ClientData.PosX, (int)c.ClientData.PosY, (int)c.ClientData.RoleDirection, c.ClientData.CurrentLifeV, c.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
            }
        }

        /// <summary>
        /// 给某个客户端减血
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="addedVal"></param>
        public void SubSpriteLifeV(SocketListener sl, TCPOutPacketPool pool, GameClient c, double lifeV)
        {
            //如果已经死亡，则不再调度
            if (c.ClientData.CurrentLifeV <= 0)
            {
                return;
            }

            c.ClientData.CurrentLifeV = (int)Global.GMax(0.0, c.ClientData.CurrentLifeV - lifeV);
            //GameManager.SystemServerEvents.AddEvent(string.Format("角色减血, roleID={0}({1}), Sub={2}, Life={3}", c.ClientData.RoleID, c.ClientData.RoleName, lifeV, c.ClientData.CurrentLifeV), EventLevels.Debug);

            //通知客户端怪已经加血加魔   
            List<Object> listObjs = Global.GetAll9Clients(c);
            GameManager.ClientMgr.NotifyOthersRelife(sl, pool, c, c.ClientData.MapCode, c.ClientData.CopyMapID, c.ClientData.RoleID, (int)c.ClientData.PosX, (int)c.ClientData.PosY, (int)c.ClientData.RoleDirection, c.ClientData.CurrentLifeV, c.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
        }

        #endregion 角色加减血

        #region 角色加减魔

        /// <summary>
        /// 给某个客户端加魔
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="addedVal"></param>
        public void AddSpriteMagicV(SocketListener sl, TCPOutPacketPool pool, GameClient c, double magicV, string reason)
        {
            //如果已经死亡，则不再调度
            if (c.ClientData.CurrentLifeV <= 0)
            {
                return;
            }

            //判断如果魔量少于最大魔量
            if (c.ClientData.CurrentMagicV < c.ClientData.MagicV)
            {
                RoleRelifeLog relifeLog = new RoleRelifeLog(c.ClientData.RoleID, c.ClientData.RoleName, c.ClientData.MapCode, reason);
                relifeLog.mpModify = true;
                relifeLog.oldMp = c.ClientData.CurrentMagicV;
                c.ClientData.CurrentMagicV = (int)Global.GMin(c.ClientData.MagicV, c.ClientData.CurrentMagicV + magicV);
                relifeLog.newMp = c.ClientData.CurrentMagicV;

                MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);
                //GameManager.SystemServerEvents.AddEvent(string.Format("角色加魔, roleID={0}({1}), Add={2}, Magic={3}", c.ClientData.RoleID, c.ClientData.RoleName, magicV, c.ClientData.CurrentMagicV), EventLevels.Debug);

                //通知客户端怪已经加血加魔  
                List<Object> listObjs = Global.GetAll9Clients(c);
                GameManager.ClientMgr.NotifyOthersRelife(sl, pool, c, c.ClientData.MapCode, c.ClientData.CopyMapID, c.ClientData.RoleID, (int)c.ClientData.PosX, (int)c.ClientData.PosY, (int)c.ClientData.RoleDirection, c.ClientData.CurrentLifeV, c.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
            }
        }

        /// <summary>
        /// 给某个客户端减魔
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="addedVal"></param>
        public void SubSpriteMagicV(SocketListener sl, TCPOutPacketPool pool, GameClient c, double magicV)
        {
            if (c.ClientData.IsFlashPlayer == 1 && c.ClientData.MapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)
                return;

            //如果已经死亡，则不再调度
            if (c.ClientData.CurrentLifeV <= 0)
            {
                return;
            }

            c.ClientData.CurrentMagicV = (int)Global.GMax(0.0, c.ClientData.CurrentMagicV - magicV);

            //GameManager.SystemServerEvents.AddEvent(string.Format("角色减魔, roleID={0}({1}), Sub={2}, Magic={3}", c.ClientData.RoleID, c.ClientData.RoleName, magicV, c.ClientData.CurrentMagicV), EventLevels.Debug);

            //通知客户端怪已经加血加魔     
            List<Object> listObjs = Global.GetAll9Clients(c);
            GameManager.ClientMgr.NotifyOthersRelife(sl, pool, c, c.ClientData.MapCode, c.ClientData.CopyMapID, c.ClientData.RoleID, (int)c.ClientData.PosX, (int)c.ClientData.PosY, (int)c.ClientData.RoleDirection, c.ClientData.CurrentLifeV, c.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
        }

        #endregion 角色加减魔

        #region 宠物相关

        /// <summary>
        /// 通知角色宠物的指令信息(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyPetCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int status, int petType, int extTag1, string extTag2, List<Object> objsList)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", status, client.ClientData.RoleID, petType, extTag1, extTag2);

            if (null == objsList)
            {
                TCPOutPacket tcpOutPacket = null;
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_PET);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
            else
            {
                //群发消息
                SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_PET);
            }
        }

        /// <summary>
        /// 退出时删除角色放出的宠物
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void RemoveRolePet(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList, bool notifySelf)
        {
            //数据库中记录宠物状态
            //和DBServer通讯，清空当前的骑乘状态, 如果失败，则不处理, 成功则, 清空buffer属性值
            if (client.ClientData.PetDbID <= 0 || client.ClientData.PetRoleID <= 0)
            {
                return;
            }

            PetData petData = Global.GetPetDataByDbID(client, client.ClientData.PetDbID);
            if (null == petData)
            {
                return;
            }

            //List<object> obsList2 = new List<object>();
            //if (null != objsList)
            //{
            //    for (int i = 0; i < objsList.Count; i++)
            //    {
            //        if (!notifySelf)
            //        {
            //            if (objsList[i] is GameClient && 
            //                ((objsList[i] as GameClient).ClientData.RoleID == client.ClientData.RoleID))
            //            {
            //                continue;
            //            }
            //        }

            //        obsList2.Add(objsList[i]);
            //    }
            //}

            //通知角色宠物的指令信息(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyPetCmd(sl, pool, client, 0, (int)PetCmds.Hide, client.ClientData.PetRoleID, "", objsList);
        }

        /// <summary>
        /// 通知自己的宠物状态(新登录，新地图, 复活需要)
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void NotifySelfPetShow(GameClient client)
        {
            //通知宠物的指令信息
            if (client.ClientData.PetDbID > 0)
            {
                PetData petData = Global.GetPetDataByDbID(client, client.ClientData.PetDbID);
                if (null != petData)
                {
                    if (client.ClientData.PetRoleID <= 0)
                    {
                        /// 判断宠物是否死掉了
                        if (!Global.IsPetDead(petData))
                        {
                            client.ClientData.PetRoleID = (int)GameManager.PetIDMgr.GetNewID();

                            //通知角色宠物的指令信息(同一个地图才需要通知)
                            Point pos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, client.ClientData.MapCode, client.ClientData.PosX, client.ClientData.PosY, 150);

                            client.ClientData.PetPosX = (int)pos.X;
                            client.ClientData.PetPosY = (int)pos.Y;
                            client.ClientData.ReportPetPosTicks = 0;
                        }
                    }

                    if (client.ClientData.PetRoleID > 0)
                    {
                        double direction = Global.GetDirectionByTan(client.ClientData.PosX, client.ClientData.PosY, client.ClientData.PetPosX, client.ClientData.PetPosY);
                        string petInfo = string.Format("{0}${1}${2}${3}${4}${5}${6}", client.ClientData.PetRoleID, petData.PetName, petData.Level, petData.PetID, (int)client.ClientData.PetPosX, (int)client.ClientData.PetPosY, (int)direction);
                        GameManager.ClientMgr.NotifyPetCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, 0, (int)PetCmds.Show, petData.DbID, petInfo, null); //只发送给自己
                    }
                }
            }
        }

        /// <summary>
        /// 通知他人自己的宠物状态隐藏(新登录，新地图, 复活需要)
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void NotifyOthersMyPetHide(GameClient client)
        {
            //数据库中记录宠物状态
            //处理先前的宠物
            if (client.ClientData.PetRoleID > 0)
            {
                List<Object> objsList = Global.GetAll9Clients(client);

                //通知角色宠物的指令信息(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyPetCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, 0, (int)PetCmds.Hide, client.ClientData.PetRoleID, "", objsList);
            }
        }

        /// <summary>
        /// 将其他人的宠物数据通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfOnlineOtherPet(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient otherClient)
        {
            if (null == otherClient) return;
            if (client.ClientData.RoleID == otherClient.ClientData.RoleID) //跳过自己
            {
                return;
            }

            if (otherClient.ClientData.PetDbID <= 0 || otherClient.ClientData.PetRoleID <= 0)
            {
                return;
            }

            //将来设置为安全的SafePetData 数据以便于访问, 其他所有的地方也可以利用这个思路
            PetData petData = Global.GetPetDataByDbID(otherClient, otherClient.ClientData.PetDbID);
            if (null == petData)
            {
                return;
            }

            Point pos = new Point(otherClient.ClientData.PetPosX, otherClient.ClientData.PetPosY);
            double direction = Global.GetDirectionByTan(otherClient.ClientData.PosX, otherClient.ClientData.PosY, pos.X, pos.Y);
            string petInfo = string.Format("{0}${1}${2}${3}${4}${5}${6}", otherClient.ClientData.PetRoleID, petData.PetName, petData.Level, petData.PetID, (int)pos.X, (int)pos.Y, (int)direction);
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 0, otherClient.ClientData.RoleID, (int)PetCmds.Show, otherClient.ClientData.PetRoleID, petInfo);

            TCPOutPacket tcpOutPacket = null;
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_PET);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 将其他所有在线的人的宠物数据通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfOnlineOtherPets(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                if (client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID) //跳过自己
                {
                    continue;
                }

                if ((objsList[i] as GameClient).ClientData.PetDbID <= 0 || (objsList[i] as GameClient).ClientData.PetRoleID <= 0)
                {
                    continue;
                }

                //将来设置为安全的SafePetData 数据以便于访问, 其他所有的地方也可以利用这个思路
                PetData petData = Global.GetPetDataByDbID((objsList[i] as GameClient), (objsList[i] as GameClient).ClientData.PetDbID);
                if (null == petData)
                {
                    continue;
                }

                Point pos = new Point((objsList[i] as GameClient).ClientData.PetPosX, (objsList[i] as GameClient).ClientData.PetPosY);
                double direction = Global.GetDirectionByTan((objsList[i] as GameClient).ClientData.PosX, (objsList[i] as GameClient).ClientData.PosY, pos.X, pos.Y);
                string petInfo = string.Format("{0}${1}${2}${3}${4}${5}${6}", (objsList[i] as GameClient).ClientData.PetRoleID, petData.PetName, petData.Level, petData.PetID, (int)pos.X, (int)pos.Y, (int)direction);
                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 0, (objsList[i] as GameClient).ClientData.RoleID, (int)PetCmds.Show, (objsList[i] as GameClient).ClientData.PetRoleID, petInfo);

                TCPOutPacket tcpOutPacket = null;
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_PET);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                    break;
                }
            }
        }

        /// <summary>
        /// 将其他所有在线的人的宠物数据离开通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfPetsOfflines(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                if (client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID) //跳过自己
                {
                    continue;
                }

                if ((objsList[i] as GameClient).ClientData.PetDbID <= 0 || (objsList[i] as GameClient).ClientData.PetRoleID <= 0)
                {
                    continue;
                }

                //将来设置为安全的SafePetData 数据以便于访问, 其他所有的地方也可以利用这个思路
                PetData petData = Global.GetPetDataByDbID((objsList[i] as GameClient), (objsList[i] as GameClient).ClientData.PetDbID);
                if (null == petData)
                {
                    continue;
                }

                Point pos = new Point((objsList[i] as GameClient).ClientData.PetPosX, (objsList[i] as GameClient).ClientData.PetPosY);
                double direction = Global.GetDirectionByTan((objsList[i] as GameClient).ClientData.PosX, (objsList[i] as GameClient).ClientData.PosY, pos.X, pos.Y);
                string petInfo = string.Format("{0}${1}${2}${3}${4}${5}${6}", (objsList[i] as GameClient).ClientData.PetRoleID, petData.PetName, petData.Level, petData.PetID, (int)pos.X, (int)pos.Y, (int)direction);

                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 0, (objsList[i] as GameClient).ClientData.RoleID, (int)PetCmds.Hide, (objsList[i] as GameClient).ClientData.PetRoleID, petInfo);

                TCPOutPacket tcpOutPacket = null;
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_PET);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                    break;
                }
            }
        }

        #endregion 宠物相关

        #region 坐骑相关

        /// <summary>
        /// 通知角色骑乘的指令信息(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyHorseCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int status, int horseType, int horseDbID, int horseID, int horseBodyID, List<Object> objsList)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", status, client.ClientData.RoleID, horseType, horseDbID, horseID, horseBodyID);

            if (null == objsList)
            {
                TCPOutPacket tcpOutPacket = null;
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_HORSE);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
            else
            {
                //群发消息
                SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_HORSE);
            }
        }

        /// <summary>
        /// 通知自己的坐骑骑乘状态(新登录，新地图, 复活需要)
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void NotifySelfOnHorse(GameClient client)
        {
            //通知骑乘的的指令信息
            if (client.ClientData.HorseDbID > 0)
            {
                HorseData horseData = Global.GetHorseDataByDbID(client, client.ClientData.HorseDbID);
                if (null != horseData)
                {
                    //计算坐骑的积分值
                    client.ClientData.RoleHorseJiFen = Global.CalcHorsePropsJiFen(horseData);

                    GameManager.ClientMgr.NotifyHorseCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, 0,
                        (int)HorseCmds.On, horseData.DbID, horseData.HorseID, horseData.BodyID, null);
                }
            }
        }

        /// <summary>
        /// 将其他人的骑乘状态通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfOtherHorse(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient otherClient)
        {
            if (null == otherClient) return;
            if (client.ClientData.RoleID == otherClient.ClientData.RoleID) //跳过自己
            {
                return;
            }

            if (otherClient.ClientData.HorseDbID <= 0)
            {
                return;
            }

            //将来设置为安全的SafeHorseData 数据以便于访问, 其他所有的地方也可以利用这个思路
            HorseData horseData = Global.GetHorseDataByDbID(otherClient, otherClient.ClientData.HorseDbID);
            if (null == horseData)
            {
                return;
            }

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", 0, otherClient.ClientData.RoleID, (int)HorseCmds.On,
                horseData.DbID, horseData.HorseID, horseData.BodyID);

            TCPOutPacket tcpOutPacket = null;
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_HORSE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 将其他所有在线的人的骑乘状态通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfOthersHorse(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                if (client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID) //跳过自己
                {
                    continue;
                }

                if ((objsList[i] as GameClient).ClientData.HorseDbID <= 0)
                {
                    continue;
                }

                //将来设置为安全的SafeHorseData 数据以便于访问, 其他所有的地方也可以利用这个思路
                HorseData horseData = Global.GetHorseDataByDbID((objsList[i] as GameClient), (objsList[i] as GameClient).ClientData.HorseDbID);
                if (null == horseData)
                {
                    continue;
                }

                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", 0, (objsList[i] as GameClient).ClientData.RoleID, (int)HorseCmds.On,
                    horseData.DbID, horseData.HorseID, horseData.BodyID);

                TCPOutPacket tcpOutPacket = null;
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_HORSE);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                    break;
                }
            }
        }

        /// <summary>
        /// 广播改变的临时坐骑的ID
        /// </summary>
        /// <param name="client"></param>
        public void JugeTempHorseID(GameClient client)
        {
            if (client.ClientData.StartTempHorseIDTicks <= 0)
            {
                return;
            }

            if (client.ClientData.TempHorseID <= 0)
            {
                return;
            }

            long ticks = TimeUtil.NOW();
            if (ticks - client.ClientData.StartTempHorseIDTicks < (3 * 60 * 1000))
            {
                return;
            }

            int tempHorseID = client.ClientData.TempHorseID;
            client.ClientData.StartTempHorseIDTicks = 0;
            client.ClientData.TempHorseID = 0;

            //处理先前的坐骑
            if (client.ClientData.HorseDbID <= 0)
            {
                return;
            }

            HorseData horseData = Global.GetHorseDataByDbID(client, client.ClientData.HorseDbID);
            if (null == horseData)
            {
                return;
            }

            string horseName = Global.GetHorseNameByID(tempHorseID);
            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, StringUtil.substitute(Global.GetLang("临时体验的【{0}】形象，超过最长体验时间，被系统收回"), horseName),
                GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            //通知骑乘的的指令信息
            GameManager.ClientMgr.NotifyHorseCmd(Global._TCPManager.MySocketListener,
                Global._TCPManager.TcpOutPacketPool, client, 0, (int)HorseCmds.On, horseData.DbID, horseData.HorseID, horseData.BodyID, objsList);
        }

        #endregion 坐骑相关

        #region 经脉相关

        /// <summary>
        /// 通知角色经脉列表数据的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyJingMaiListCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            TCPOutPacket tcpOutPacket = null;
            List<JingMaiData> jingMaiDataList = client.ClientData.JingMaiDataList;
            if (null != jingMaiDataList)
            {
                lock (jingMaiDataList)
                {
                    tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<JingMaiData>>(jingMaiDataList, pool, (int)TCPGameServerCmds.CMD_GETJINGMAILIST);
                }
            }
            else
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<JingMaiData>>(jingMaiDataList, pool, (int)TCPGameServerCmds.CMD_GETJINGMAILIST);
            }

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知角色经脉综合信息的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyJingMaiInfoCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            TCPOutPacket tcpOutPacket = null;
            Dictionary<string, int> jingMaiDataDict = client.ClientData.JingMaiPropsDict;
            if (null != jingMaiDataDict)
            {
                lock (jingMaiDataDict)
                {
                    tcpOutPacket = DataHelper.ObjectToTCPOutPacket<Dictionary<string, int>>(jingMaiDataDict, pool, (int)TCPGameServerCmds.CMD_SPR_JINGMAI_INFO);
                }
            }
            else
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<Dictionary<string, int>>(jingMaiDataDict, pool, (int)TCPGameServerCmds.CMD_SPR_JINGMAI_INFO);
            }

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知其他角色经脉列表数据的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOtherJingMaiListCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int otherRoleID)
        {
            TCPOutPacket tcpOutPacket = null;
            List<JingMaiData> jingMaiDataList = null;
            GameClient otherClient = FindClient(otherRoleID);
            if (null != otherClient)
            {
                jingMaiDataList = otherClient.ClientData.JingMaiDataList;
                if (null != jingMaiDataList)
                {
                    lock (jingMaiDataList)
                    {
                        tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<JingMaiData>>(jingMaiDataList, pool, (int)TCPGameServerCmds.CMD_GETOTHERJINGMAILIST);
                    }
                }
                else
                {
                    tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<JingMaiData>>(jingMaiDataList, pool, (int)TCPGameServerCmds.CMD_GETOTHERJINGMAILIST);
                }
            }

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知角色结束冲穴状态的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyEndChongXueCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            string strcmd = string.Format("{0}", client.ClientData.RoleID);
            TCPOutPacket tcpOutPacket = null;
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYENDCHONGXUE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知客户端冲脉的结果
        /// </summary>
        /// <param name="client"></param>
        public void NotifyJingMaiResult(GameClient client, int retCode, int jingMaiID, int jingMaiLevel)
        {
            //应该返回重新计算过的经脉重数
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, retCode, client.ClientData.JingMaiBodyLevel, jingMaiID, jingMaiLevel);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_UP_JINGMAI_LEVEL);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 经脉相关

        #region 好友和敌人相关

        /// <summary>
        /// 删除时间最早的敌人
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="killedRoleID"></param>
        public bool RemoveOldestEnemy(TCPManager tcpMgr, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client)
        {
            int totalCount = Global.GetFriendCountByType(client, 2); //获取敌人的数量
            if (totalCount < (int)FriendsConsts.MaxEnemiesNum)
            {
                return true;
            }

            //查找第一符合指定类型的队列
            FriendData friendData = Global.FindFirstFriendDataByType(client, 2);
            if (null == friendData)
            {
                return true;
            }

            //删除好友、黑名单、仇人列表
            return GameManager.ClientMgr.RemoveFriend(tcpMgr, tcpClientPool, pool, client, friendData.DbID);
        }

        /// <summary>
        /// 加入到敌人列表中
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="friendType"></param>
        public void AddToEnemyList(TCPManager tcpMgr, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int killedRoleID)
        {
            //竞技场 和 炎黄战场中，不记忆仇人
            if (client.ClientData.MapCode == GameManager.BattleMgr.BattleMapCode
                || client.ClientData.MapCode == GameManager.ArenaBattleMgr.BattleMapCode)
            {
                return;
            }

            GameClient findClient = FindClient(killedRoleID);
            if (null == findClient)
            {
                return;
            }

            //先判断是否先删除
            if (!RemoveOldestEnemy(tcpMgr, tcpClientPool, pool, findClient))
            {
                return;
            }

            int friendDbID = -1;
            FriendData friendData = Global.FindFriendData(findClient, client.ClientData.RoleID);
            if (null != friendData)
            {
                friendDbID = friendData.DbID;
            }

            int enemyCount = Global.GetFriendCountByType(findClient, 2);
            if (enemyCount >= (int)FriendsConsts.MaxEnemiesNum)
            {
                GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, findClient,
                    StringUtil.substitute(Global.GetLang("您的仇人列表已经满, 最多不能超过{0}个"),
                    (int)FriendsConsts.MaxEnemiesNum), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

                return;
            }

            //shizhu added: 被好友击杀不加入到仇人列表
            if (friendData == null || (friendData.FriendType != 0 && friendData.FriendType != 2))
            {
                AddFriend(tcpMgr, tcpClientPool, pool, findClient, friendDbID, client.ClientData.RoleID, Global.FormatRoleName(client, client.ClientData.RoleName), 2);
            }
        }

        /// <summary>
        /// 删除好友
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="dbID"></param>
        public bool RemoveFriend(TCPManager tcpMgr, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int dbID)
        {
            bool ret = false;

            try
            {
                string strcmd = string.Format("{0}:{1}", dbID, client.ClientData.RoleID);
                byte[] bytesCmd = new UTF8Encoding().GetBytes(strcmd);

                TCPOutPacket tcpOutPacket = null;
                TCPProcessCmdResults result = Global.TransferRequestToDBServer(tcpMgr, client.ClientSocket, tcpClientPool, null, pool, (int)TCPGameServerCmds.CMD_SPR_REMOVEFRIEND, bytesCmd, bytesCmd.Length, out tcpOutPacket, client.ServerId);
                if (TCPProcessCmdResults.RESULT_FAILED != result)
                {
                    //处理本地精简的好友数据
                    string strData = new UTF8Encoding().GetString(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);

                    //解析客户端的指令
                    string[] fields = strData.Split(':');
                    if (fields.Length == 3 && Convert.ToInt32(fields[2]) >= 0)
                    {
                        Global.RemoveFriendData(client, dbID);
                    }

                    ret = true;
                }

                //发送消息给客户端
                if (!tcpMgr.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }

                return ret;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return ret;
        }

        /// <summary>
        /// 加入朋友列表中
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="friendType"></param>
        public bool AddFriend(TCPManager tcpMgr, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int dbID, int otherRoleID, string otherRoleName, int friendType)
        {
            bool ret = false;

            if (client.ClientSocket.IsKuaFuLogin)
            {
                return false;
            }

            //禁止将自己加入仇人列表
            if (friendType == 2 && otherRoleID == client.ClientData.RoleID)
            {
                return false;
            }

            try
            {
                FriendData friendData = null;
                if (otherRoleID > 0)
                {
                    friendData = Global.FindFriendData(client, otherRoleID);
                    if (null != friendData)
                    {
                        if (friendData.FriendType == friendType) //已经存在
                        {
                            return ret;
                        }
                    }
                }

                //判断是否数量已经满了
                int friendTypeCount = Global.GetFriendCountByType(client, friendType);
                if (0 == friendType) //好友
                {
                    if (friendTypeCount >= (int)FriendsConsts.MaxFriendsNum)
                    {
                        GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, client, StringUtil.substitute(Global.GetLang("您的好友列表已经满, 最多不能超过{0}个"), (int)FriendsConsts.MaxFriendsNum), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                        return ret;
                    }
                }
                else if (1 == friendType) //黑名单
                {
                    if (friendTypeCount >= (int)FriendsConsts.MaxBlackListNum)
                    {
                        GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, client, StringUtil.substitute(Global.GetLang("您的黑名单列表已经满, 最多不能超过{0}个"), (int)FriendsConsts.MaxBlackListNum), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                        return ret;
                    }
                }
                else if (2 == friendType) //仇人
                {
                    if (friendTypeCount >= (int)FriendsConsts.MaxEnemiesNum)
                    {
                        GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, client, StringUtil.substitute(Global.GetLang("您的仇人列表已经满, 最多不能超过{0}个"), (int)FriendsConsts.MaxEnemiesNum), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                        return ret;
                    }
                }

                string strcmd = string.Format("{0}:{1}:{2}:{3}", dbID, client.ClientData.RoleID, otherRoleName, friendType);
                byte[] bytesCmd = new UTF8Encoding().GetBytes(strcmd);

                TCPOutPacket tcpOutPacket = null;
                TCPProcessCmdResults result = Global.TransferRequestToDBServer(tcpMgr, client.ClientSocket, tcpClientPool, null, pool, (int)TCPGameServerCmds.CMD_SPR_ADDFRIEND, bytesCmd, bytesCmd.Length, out tcpOutPacket, client.ServerId);

                if (null == tcpOutPacket)
                {
                    return ret;
                }

                //处理本地精简的好友列表数据
                friendData = DataHelper.BytesToObject<FriendData>(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);
                if (null != friendData && friendData.DbID >= 0)
                {
                    ret = true;

                    Global.RemoveFriendData(client, friendData.DbID); //防止加入重复数据
                    Global.AddFriendData(client, friendData);

                    if (0 == friendType) //好友
                    {
                        friendTypeCount = Global.GetFriendCountByType(client, friendType);
                        if (1 == friendTypeCount)
                        {
                            //成就相关---第一次拥有了一个好友
                            ChengJiuManager.OnFirstAddFriend(client);
                        }
                    }

                    //通知对方自己将他加为了好友
                    //查看用户是否在本服务器上，如果没有，则查询从其他服务器查询，并且转发给自己的用户(只针对当前服务器，不转发)
                    GameClient otherClient = GameManager.ClientMgr.FindClient(friendData.OtherRoleID);
                    if (null != otherClient)
                    {
                        if (friendData.FriendType == 0)
                        {
                            string typeName = Global.GetLang("好友");
                            /*if (friendData.FriendType == 1)
                            {
                                typeName = Global.GetLang("黑名单");
                            }
                            else if (friendData.FriendType == 2)
                            {
                                typeName = Global.GetLang("仇人");
                            }*/

                            /// 通知在线的对方(不限制地图)个人紧要消息
                            GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, otherClient, StringUtil.substitute(Global.GetLang("【{0}】将您加入了{1}列表"), Global.FormatRoleName(client, client.ClientData.RoleName), typeName), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.ErrAndBox);
                        }
                    }
                }

                //发送消息给客户端
                if (!tcpMgr.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }

                return ret;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }
            return ret;
        }

        #endregion 好友和敌人相关

        #region 点将台相关

        /// <summary>
        /// 通知点将台房间数据的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDianJiangData(SocketListener sl, TCPOutPacketPool pool, DJRoomData roomData)
        {
            if (null != roomData)
            {
                byte[] bytesData = null;
                lock (roomData)
                {
                    bytesData = DataHelper.ObjectToBytes<DJRoomData>(roomData);
                }

                if (null != bytesData && bytesData.Length > 0)
                {
                    TCPOutPacket tcpOutPacket = null;
                    int index = 0;
                    GameClient client = null;
                    while ((client = GetNextClient(ref index)) != null)
                    {
                        if (!client.ClientData.ViewDJRoomDlg)
                        {
                            continue;
                        }

                        tcpOutPacket = pool.Pop();
                        tcpOutPacket.PacketCmdID = (UInt16)TCPGameServerCmds.CMD_SPR_DIANJIANGDATA;
                        tcpOutPacket.FinalWriteData(bytesData, 0, (int)bytesData.Length);

                        if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                        {
                            //
                            /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                tcpOutPacket.PacketCmdID,
                                tcpOutPacket.PacketDataSize,
                                client.ClientData.RoleID,
                                client.ClientData.RoleName));*/
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 通知点将台房间成员数据的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDJRoomRolesData(SocketListener sl, TCPOutPacketPool pool, DJRoomRolesData djRoomRolesData)
        {
            if (null != djRoomRolesData)
            {
                lock (djRoomRolesData)
                {
                    byte[] bytesData = DataHelper.ObjectToBytes<DJRoomRolesData>(djRoomRolesData);

                    TCPOutPacket tcpOutPacket = null;
                    for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
                    {
                        GameClient client = FindClient(djRoomRolesData.Team1[i].RoleID);
                        if (null == client) continue;

                        tcpOutPacket = pool.Pop();
                        tcpOutPacket.PacketCmdID = (UInt16)TCPGameServerCmds.CMD_SPR_DJROOMROLESDATA;
                        tcpOutPacket.FinalWriteData(bytesData, 0, (int)bytesData.Length);

                        if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                        {
                            //
                            /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                tcpOutPacket.PacketCmdID,
                                tcpOutPacket.PacketDataSize,
                                client.ClientData.RoleID,
                                client.ClientData.RoleName));*/
                        }
                    }

                    for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
                    {
                        GameClient client = FindClient(djRoomRolesData.Team2[i].RoleID);
                        if (null == client) continue;

                        tcpOutPacket = pool.Pop();
                        tcpOutPacket.PacketCmdID = (UInt16)TCPGameServerCmds.CMD_SPR_DJROOMROLESDATA;
                        tcpOutPacket.FinalWriteData(bytesData, 0, (int)bytesData.Length);

                        if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                        {
                            //
                            /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                tcpOutPacket.PacketCmdID,
                                tcpOutPacket.PacketDataSize,
                                client.ClientData.RoleID,
                                client.ClientData.RoleName));*/
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 通知角色点将台的指令信息(所有打开了点将台窗口的都通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDianJiangCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int status, int djCmdType, int extTag1, string extTag2, bool allSend = false)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", status, client.ClientData.RoleID, djCmdType, extTag1, extTag2);

            if (!allSend)
            {
                TCPOutPacket tcpOutPacket = null;
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DIANJIANG);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
            else
            {
                int index = 0;
                client = null;
                TCPOutPacket tcpOutPacket = null;
                try
                {
                    while ((client = GetNextClient(ref index)) != null)
                    {
                        if (!client.ClientData.ViewDJRoomDlg)
                        {
                            continue;
                        }

                        if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DIANJIANG);
                        if (!sl.SendData(client.ClientSocket, tcpOutPacket, false))
                        {
                            //
                            /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                tcpOutPacket.PacketCmdID,
                                tcpOutPacket.PacketDataSize,
                                client.ClientData.RoleID,
                                client.ClientData.RoleName));*/
                        }
                    }
                }
                finally
                {
                    PushBackTcpOutPacket(tcpOutPacket);
                }
            }
        }

        /// <summary>
        /// 销毁点将台房间
        /// </summary>
        public int DestroyDianJiangRoom(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            if (client.ClientData.DJRoomID <= 0)
            {
                return -1;
            }

            //查找房间数据
            DJRoomData djRoomData = GameManager.DJRoomMgr.FindRoomData(client.ClientData.DJRoomID);
            if (null == djRoomData)
            {
                return -2;
            }

            //判断自己是否是房间的创建者
            if (djRoomData.CreateRoleID != client.ClientData.RoleID)
            {
                return -3;
            }

            //判断房间的是否已经开始了战斗，开始了则无法直接删除了
            lock (djRoomData)
            {
                if (djRoomData.PKState > 0)
                {
                    return -4;
                }
            }

            //查找房间角色数据
            DJRoomRolesData djRoomRolesData = GameManager.DJRoomMgr.FindRoomRolesData(client.ClientData.DJRoomID);
            if (null == djRoomRolesData)
            {
                return -5;
            }

            int roomID = client.ClientData.DJRoomID;

            //从内存中清空
            GameManager.DJRoomMgr.RemoveRoomData(roomID);
            GameManager.DJRoomMgr.RemoveRoomRolesData(roomID);

            lock (djRoomRolesData)
            {
                djRoomRolesData.Removed = 1;

                for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
                {
                    GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team1[i].RoleID);
                    if (null != gc)
                    {
                        gc.ClientData.DJRoomID = -1;
                        gc.ClientData.DJRoomTeamID = -1;
                        gc.ClientData.HideSelf = 0;
                    }
                }

                for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
                {
                    GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team2[i].RoleID);
                    if (null != gc)
                    {
                        gc.ClientData.DJRoomID = -1;
                        gc.ClientData.DJRoomTeamID = -1;
                        gc.ClientData.HideSelf = 0;
                    }
                }
            }

            //发送错误信息
            GameManager.ClientMgr.NotifyDianJiangCmd(sl, pool, client, 0, (int)DianJiangCmds.RemoveRoom, roomID, "", true);
            return 0;
        }

        /// <summary>
        /// 离开点将台房间
        /// </summary>
        public int LeaveDianJiangRoom(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            if (client.ClientData.DJRoomID <= 0)
            {
                return -1;
            }

            //查找房间数据
            DJRoomData djRoomData = GameManager.DJRoomMgr.FindRoomData(client.ClientData.DJRoomID);
            if (null == djRoomData)
            {
                return -2;
            }

            //判断自己是否是房间的创建者
            if (djRoomData.CreateRoleID == client.ClientData.RoleID)
            {
                return -3;
            }

            //判断房间的是否已经开始了战斗，开始了则无法直接删除了
            lock (djRoomData)
            {
                if (djRoomData.PKState > 0)
                {
                    return -4;
                }
            }

            //查找房间角色数据
            DJRoomRolesData djRoomRolesData = GameManager.DJRoomMgr.FindRoomRolesData(client.ClientData.DJRoomID);
            if (null == djRoomRolesData)
            {
                return -5;
            }

            int roomID = client.ClientData.DJRoomID;

            bool found = false;
            lock (djRoomRolesData)
            {
                if (djRoomRolesData.Removed > 0)
                {
                    return -6;
                }

                if (djRoomRolesData.Locked > 0)
                {
                    return -7;
                }

                for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
                {
                    if (client.ClientData.RoleID == djRoomRolesData.Team1[i].RoleID)
                    {
                        found = true;
                        djRoomRolesData.Team1.RemoveAt(i);
                        break;
                    }
                }

                if (!found)
                {
                    for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
                    {
                        if (client.ClientData.RoleID == djRoomRolesData.Team2[i].RoleID)
                        {
                            found = true;
                            djRoomRolesData.Team2.RemoveAt(i);
                            break;
                        }
                    }
                }

                djRoomRolesData.TeamStates.Remove(client.ClientData.RoleID);
                djRoomRolesData.RoleStates.Remove(client.ClientData.RoleID);
            }

            if (found)
            {
                lock (djRoomData)
                {
                    djRoomData.PKRoleNum--;
                }
            }

            client.ClientData.DJRoomID = -1;
            client.ClientData.DJRoomTeamID = -1;
            client.ClientData.HideSelf = 0;

            //发送房间数据
            GameManager.ClientMgr.NotifyDianJiangData(sl, pool, djRoomData);

            //通知点将台房间成员数据的指令信息
            GameManager.ClientMgr.NotifyDJRoomRolesData(sl, pool, djRoomRolesData);

            //发送信息
            GameManager.ClientMgr.NotifyDianJiangCmd(sl, pool, client, 0, (int)DianJiangCmds.LeaveRoom, roomID, Global.FormatRoleName(client, client.ClientData.RoleName), true);
            return 0;
        }

        /// <summary>
        /// 观众离开点将台房间
        /// </summary>
        public int ViewerLeaveDianJiangRoom(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            if (client.ClientData.DJRoomID <= 0)
            {
                return -1;
            }

            if (client.ClientData.DJRoomTeamID > 0) //参战者
            {
                return -100;
            }

            //查找房间数据
            DJRoomData djRoomData = GameManager.DJRoomMgr.FindRoomData(client.ClientData.DJRoomID);
            if (null == djRoomData)
            {
                return -2;
            }

            //查找房间角色数据
            DJRoomRolesData djRoomRolesData = GameManager.DJRoomMgr.FindRoomRolesData(client.ClientData.DJRoomID);
            if (null == djRoomRolesData)
            {
                return -3;
            }

            int roomID = client.ClientData.DJRoomID;

            bool found = false;
            lock (djRoomRolesData)
            {
                if (null != djRoomRolesData.ViewRoles)
                {
                    for (int i = 0; i < djRoomRolesData.ViewRoles.Count; i++)
                    {
                        if (client.ClientData.RoleID == djRoomRolesData.ViewRoles[i].RoleID)
                        {
                            found = true;
                            djRoomRolesData.ViewRoles.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            if (found)
            {
                lock (djRoomData)
                {
                    djRoomData.ViewRoleNum--;
                }
            }

            client.ClientData.DJRoomID = -1;
            client.ClientData.DJRoomTeamID = -1;
            client.ClientData.HideSelf = 0;

            //发送房间数据
            GameManager.ClientMgr.NotifyDianJiangData(sl, pool, djRoomData);
            return 0;
        }

        /// <summary>
        /// 通知点将台房间内的玩家传动到点将台地图
        /// </summary>
        public int TransportDianJiangRoom(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            if (client.ClientData.DJRoomID <= 0)
            {
                return -1;
            }

            //查找房间数据
            DJRoomData djRoomData = GameManager.DJRoomMgr.FindRoomData(client.ClientData.DJRoomID);
            if (null == djRoomData)
            {
                return -2;
            }

            //判断自己是否是房间的创建者
            if (djRoomData.CreateRoleID != client.ClientData.RoleID)
            {
                return -3;
            }

            //判断房间的是否已经开始了战斗，开始了则无法直接删除了
            lock (djRoomData)
            {
                if (djRoomData.PKState <= 0)
                {
                    return -4;
                }
            }

            //查找房间角色数据
            DJRoomRolesData djRoomRolesData = GameManager.DJRoomMgr.FindRoomRolesData(client.ClientData.DJRoomID);
            if (null == djRoomRolesData)
            {
                return -5;
            }

            lock (djRoomRolesData)
            {
                if (djRoomRolesData.Locked <= 0)
                {
                    return -6;
                }

                for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
                {
                    GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team1[i].RoleID);
                    if (null != gc)
                    {
                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            gc, Global.DianJiangTaiMapCode, -1, -1, -1);
                    }
                }

                for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
                {
                    GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team2[i].RoleID);
                    if (null != gc)
                    {
                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            gc, Global.DianJiangTaiMapCode, -1, -1, -1);
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// 通知角色点将台房间内战斗的指令信息(参战者，观众)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDianJiangFightCmd(SocketListener sl, TCPOutPacketPool pool, DJRoomData djRoomData, int djCmdType, string extTag2, GameClient toClient = null)
        {
            if (null == djRoomData) return;

            string strcmd = string.Format("{0}:{1}:{2}:{3}", djRoomData.RoomID, djCmdType, 0, extTag2);

            //查找房间角色数据
            DJRoomRolesData djRoomRolesData = GameManager.DJRoomMgr.FindRoomRolesData(djRoomData.RoomID);
            if (null == djRoomRolesData)
            {
                return;
            }

            TCPOutPacket tcpOutPacket = null;

            if (null != toClient)
            {
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DIANJIANGFIGHT);
                if (!sl.SendData(toClient.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        toClient.ClientData.RoleID,
                        toClient.ClientData.RoleName));*/
                }

                return;
            }

            lock (djRoomRolesData)
            {
                tcpOutPacket = null;
                try
                {
                    for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
                    {
                        GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team1[i].RoleID);
                        if (null != gc)
                        {
                            if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DIANJIANGFIGHT);
                            if (!sl.SendData(gc.ClientSocket, tcpOutPacket, false))
                            {
                                //
                                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                    tcpOutPacket.PacketCmdID,
                                    tcpOutPacket.PacketDataSize,
                                    gc.ClientData.RoleID,
                                    gc.ClientData.RoleName));*/
                            }
                        }
                    }

                    for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
                    {
                        GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team2[i].RoleID);
                        if (null != gc)
                        {
                            if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DIANJIANGFIGHT);
                            if (!sl.SendData(gc.ClientSocket, tcpOutPacket, false))
                            {
                                //
                                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                    tcpOutPacket.PacketCmdID,
                                    tcpOutPacket.PacketDataSize,
                                    gc.ClientData.RoleID,
                                    gc.ClientData.RoleName));*/
                            }
                        }
                    }
                }
                finally
                {
                    PushBackTcpOutPacket(tcpOutPacket);
                }

                tcpOutPacket = null;
                try
                {
                    if (null != djRoomRolesData.ViewRoles)
                    {
                        strcmd = string.Format("{0}:{1}:{2}:{3}", djRoomData.RoomID, djCmdType, 1, extTag2);
                        for (int i = 0; i < djRoomRolesData.ViewRoles.Count; i++)
                        {
                            GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.ViewRoles[i].RoleID);
                            if (null != gc)
                            {
                                if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DIANJIANGFIGHT);
                                if (!sl.SendData(gc.ClientSocket, tcpOutPacket, false))
                                {
                                    //
                                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                        tcpOutPacket.PacketCmdID,
                                        tcpOutPacket.PacketDataSize,
                                        gc.ClientData.RoleID,
                                        gc.ClientData.RoleName));*/
                                }
                            }
                        }
                    }
                }
                finally
                {
                    PushBackTcpOutPacket(tcpOutPacket);
                }
            }
        }

        /// <summary>
        /// 通知角色点将台房间内战斗的指令信息(参战者，观众)离开离开场景消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDJFightRoomLeaveMsg(SocketListener sl, TCPOutPacketPool pool, DJRoomData djRoomData)
        {
            if (null == djRoomData) return;

            //查找房间角色数据
            DJRoomRolesData djRoomRolesData = GameManager.DJRoomMgr.FindRoomRolesData(djRoomData.RoomID);
            if (null == djRoomRolesData)
            {
                return;
            }

            lock (djRoomRolesData)
            {
                for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
                {
                    int state = 0;
                    djRoomRolesData.RoleStates.TryGetValue(djRoomRolesData.Team1[i].RoleID, out state);
                    if (1 != state) //不再当前地图
                    {
                        continue;
                    }

                    GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team1[i].RoleID);
                    if (null != gc)
                    {
                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            gc, gc.ClientData.LastMapCode, gc.ClientData.LastPosX, gc.ClientData.LastPosY, -1);
                    }
                }

                for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
                {
                    int state = 0;
                    djRoomRolesData.RoleStates.TryGetValue(djRoomRolesData.Team2[i].RoleID, out state);
                    if (1 != state) //不再当前地图
                    {
                        continue;
                    }

                    GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team2[i].RoleID);
                    if (null != gc)
                    {
                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            gc, gc.ClientData.LastMapCode, gc.ClientData.LastPosX, gc.ClientData.LastPosY, -1);
                    }
                }

                if (null != djRoomRolesData.ViewRoles)
                {
                    for (int i = 0; i < djRoomRolesData.ViewRoles.Count; i++)
                    {
                        GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.ViewRoles[i].RoleID);
                        if (null != gc)
                        {
                            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                gc, gc.ClientData.LastMapCode, gc.ClientData.LastPosX, gc.ClientData.LastPosY, -1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 发送点将台房间的战斗结果
        /// </summary>
        public void NotifyDianJiangRoomRolesPoint(SocketListener sl, TCPOutPacketPool pool, DJRoomRolesPoint djRoomRolesPoint)
        {
            if (null == djRoomRolesPoint) return;

            //查找房间角色数据
            DJRoomRolesData djRoomRolesData = GameManager.DJRoomMgr.FindRoomRolesData(djRoomRolesPoint.RoomID);
            if (null == djRoomRolesData)
            {
                return;
            }

            byte[] bytesData = DataHelper.ObjectToBytes<DJRoomRolesPoint>(djRoomRolesPoint);
            if (null == bytesData)
            {
                return;
            }

            TCPOutPacket tcpOutPacket = null;
            lock (djRoomRolesData)
            {
                try
                {
                    for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
                    {
                        GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team1[i].RoleID);
                        if (null != gc)
                        {
                            if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytesData, 0, bytesData.Length, (int)TCPGameServerCmds.CMD_SPR_DIANJIANGPOINT);
                            if (!sl.SendData(gc.ClientSocket, tcpOutPacket, false))
                            {
                                //
                                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                    tcpOutPacket.PacketCmdID,
                                    tcpOutPacket.PacketDataSize,
                                    gc.ClientData.RoleID,
                                    gc.ClientData.RoleName));*/
                            }
                        }
                    }

                    for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
                    {
                        GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team2[i].RoleID);
                        if (null != gc)
                        {
                            if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytesData, 0, bytesData.Length, (int)TCPGameServerCmds.CMD_SPR_DIANJIANGPOINT);
                            if (!sl.SendData(gc.ClientSocket, tcpOutPacket, false))
                            {
                                //
                                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                    tcpOutPacket.PacketCmdID,
                                    tcpOutPacket.PacketDataSize,
                                    gc.ClientData.RoleID,
                                    gc.ClientData.RoleName));*/
                            }
                        }
                    }

                    if (null != djRoomRolesData.ViewRoles)
                    {
                        for (int i = 0; i < djRoomRolesData.ViewRoles.Count; i++)
                        {
                            GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.ViewRoles[i].RoleID);
                            if (null != gc)
                            {
                                if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytesData, 0, bytesData.Length, (int)TCPGameServerCmds.CMD_SPR_DIANJIANGPOINT);
                                if (!sl.SendData(gc.ClientSocket, tcpOutPacket, false))
                                {
                                    //
                                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                        tcpOutPacket.PacketCmdID,
                                        tcpOutPacket.PacketDataSize,
                                        gc.ClientData.RoleID,
                                        gc.ClientData.RoleName));*/
                                }
                            }
                        }
                    }
                }
                finally
                {
                    PushBackTcpOutPacket(tcpOutPacket);
                }
            }
        }

        /// <summary>
        /// 删除点将台房间
        /// </summary>
        public void RemoveDianJiangRoom(SocketListener sl, TCPOutPacketPool pool, DJRoomData djRoomData)
        {
            if (null == djRoomData) return;

            //查找房间角色数据
            DJRoomRolesData djRoomRolesData = GameManager.DJRoomMgr.FindRoomRolesData(djRoomData.RoomID);
            if (null == djRoomRolesData)
            {
                return;
            }

            int roomID = djRoomData.RoomID;

            //从内存中清空
            GameManager.DJRoomMgr.RemoveRoomData(roomID);
            GameManager.DJRoomMgr.RemoveRoomRolesData(roomID);

            lock (djRoomRolesData)
            {
                djRoomRolesData.Removed = 1;

                for (int i = 0; i < djRoomRolesData.Team1.Count; i++)
                {
                    int state = 0;
                    djRoomRolesData.RoleStates.TryGetValue(djRoomRolesData.Team1[i].RoleID, out state);
                    if (1 != state) //不再当前地图
                    {
                        continue;
                    }

                    GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team1[i].RoleID);
                    if (null != gc)
                    {
                        gc.ClientData.DJRoomID = -1;
                        gc.ClientData.DJRoomTeamID = -1;
                        gc.ClientData.HideSelf = 0;
                    }
                }

                for (int i = 0; i < djRoomRolesData.Team2.Count; i++)
                {
                    int state = 0;
                    djRoomRolesData.RoleStates.TryGetValue(djRoomRolesData.Team2[i].RoleID, out state);
                    if (1 != state) //不再当前地图
                    {
                        continue;
                    }

                    GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.Team2[i].RoleID);
                    if (null != gc)
                    {
                        gc.ClientData.DJRoomID = -1;
                        gc.ClientData.DJRoomTeamID = -1;
                        gc.ClientData.HideSelf = 0;
                    }
                }

                if (null != djRoomRolesData.ViewRoles)
                {
                    for (int i = 0; i < djRoomRolesData.ViewRoles.Count; i++)
                    {
                        GameClient gc = GameManager.ClientMgr.FindClient(djRoomRolesData.ViewRoles[i].RoleID);
                        if (null != gc)
                        {
                            gc.ClientData.DJRoomID = -1;
                            gc.ClientData.DJRoomTeamID = -1;
                            gc.ClientData.HideSelf = 0;
                        }
                    }
                }
            }

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 0, -1, (int)DianJiangCmds.RemoveRoom, roomID, "noHint");

            int index = 0;
            GameClient client = null;
            TCPOutPacket tcpOutPacket = null;
            try
            {
                while ((client = GetNextClient(ref index)) != null)
                {
                    if (!client.ClientData.ViewDJRoomDlg)
                    {
                        continue;
                    }

                    if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DIANJIANG);
                    if (!sl.SendData(client.ClientSocket, tcpOutPacket, false))
                    {
                        //
                        /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                            tcpOutPacket.PacketCmdID,
                            tcpOutPacket.PacketDataSize,
                            client.ClientData.RoleID,
                            client.ClientData.RoleName));*/
                    }
                }
            }
            finally
            {
                PushBackTcpOutPacket(tcpOutPacket);
            }
        }

        #endregion 点将台相关

        #region 竞技场决斗赛相关

        /// <summary>
        /// 通知角色大乱斗的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyArenaBattleCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int status, int battleType, int extTag1, int leftSecs)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", status, client.ClientData.RoleID, battleType, extTag1, leftSecs);
            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_ARENABATTLE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知在线的所有人(不限制地图)大乱斗邀请消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllArenaBattleInviteMsg(SocketListener sl, TCPOutPacketPool pool, int minLevel, int battleType, int extTag1, int leftSecs)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientData.Level < minLevel) //最低级别要求
                {
                    continue;
                }

                if (client.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 0, client.ClientData.RoleID, battleType, extTag1, leftSecs);
                TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_ARENABATTLE);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
        }

        /// <summary>
        /// 通知在线的所有人(仅限在大乱斗地图上)大乱斗邀请消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyArenaBattleInviteMsg(SocketListener sl, TCPOutPacketPool pool, int mapCode, int battleType, int extTag1, int leftSecs)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode); //发送给所有地图的用户
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 0, -1, battleType, extTag1, leftSecs);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_ARENABATTLE);
        }

        /// <summary>
        /// 通知角色大乱斗中杀人个数的指令,系统统一通知，每隔30秒一次，通知竞技场地图的玩家
        /// </summary>
        /// <param name="client"></param>
        public void NotifyArenaBattleKilledNumCmd(SocketListener sl, TCPOutPacketPool pool, int roleNumKilled, int roleNumOnStart, int rowNumNow)
        {
            List<Object> objsList = Container.GetObjectsByMap(GameManager.ArenaBattleMgr.BattleMapCode);
            if (null == objsList) return;

            string strcmd = "";

            GameClient client = null;

            for (int n = 0; n < objsList.Count; n++)
            {
                client = objsList[n] as GameClient;
                if (null != client)
                {
                    // MU 改造 只发个人积分、剩余人数
                    //strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, client.ClientData.ArenaBattleKilledNum, roleNumKilled, roleNumOnStart, rowNumNow);

                    strcmd = string.Format("{0}:{1}", client.ClientData.KingOfPkCurrentPoint, rowNumNow);
                    SendToClient(sl, pool, client, strcmd, (int)TCPGameServerCmds.CMD_SPR_ARENABATTLEKILLEDNUM);
                }
            }
        }

        #endregion 竞技场决斗赛相关

        #region 炎黄战场相关

        /// <summary>
        /// 通知角色大乱斗的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBattleCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int status, int battleType, int extTag1, int leftSecs)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", status, client.ClientData.RoleID, battleType, extTag1, leftSecs);
            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_BATTLE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知在线的所有人(不限制地图)大乱斗邀请消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllBattleInviteMsg(SocketListener sl, TCPOutPacketPool pool, int minLevel, int battleType, int extTag1, int leftSecs)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientData.Level < minLevel) //最低级别要求
                {
                    continue;
                }

                if (client.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 0, client.ClientData.RoleID, battleType, extTag1, leftSecs);
                TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_BATTLE);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
        }

        /// <summary>
        /// 通知在线的所有人(仅限在大乱斗地图上)大乱斗邀请消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBattleInviteMsg(SocketListener sl, TCPOutPacketPool pool, int mapCode, int battleType, int extTag1, int leftSecs)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode); //发送给所有地图的用户
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 0, -1, battleType, extTag1, leftSecs);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_BATTLE);
        }

        /// <summary>
        /// 开始时强制清场(仅限在大乱斗地图上)离开大乱斗场景消息
        /// </summary>
        /// <param name="client"></param>
        public void BattleBeginForceLeaveg(SocketListener sl, TCPOutPacketPool pool, int mapCode)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode);
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient client = objsList[i] as GameClient;
                if (null == client) continue;

                Container.RemoveObject(client.ClientData.RoleID, mapCode, client);
            }
        }

        /// <summary>
        /// 通知在线的所有人(仅限在大乱斗地图上)离开大乱斗场景消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBattleLeaveMsg(SocketListener sl, TCPOutPacketPool pool, int mapCode)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode);
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient client = objsList[i] as GameClient;
                if (null == client) continue;

                int toMapCode = GameManager.MainMapCode;
                int toPosX = -1;
                int toPosY = -1;

                //判断下，如果上一次的地图为空，或则不是普通地图，则强制回主城
                if (client.ClientData.LastMapCode != -1 && client.ClientData.LastPosX != -1 && client.ClientData.LastPosY != -1)
                {
                    if (MapTypes.Normal == Global.GetMapType(client.ClientData.LastMapCode))
                    {
                        toMapCode = client.ClientData.LastMapCode;
                        toPosX = client.ClientData.LastPosX;
                        toPosY = client.ClientData.LastPosY;
                    }
                }

                GameMap gameMap = null;
                if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                {
                    GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, toMapCode, toPosX, toPosY, -1);
                }
            }
        }

        /// <summary>
        /// 通知角色大乱斗中杀人个数的指令[旧的通知函数，通知所有人]
        /// </summary>
        /// <param name="client"></param>
        //public void NotifyBattleKilledNumCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int suiJiFen, int tangJiFen)
        //{
        //    List<Object> objsList = Container.GetObjectsByMap(client.ClientData.MapCode);
        //    if (null == objsList) return;

        //    string strcmd = string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, client.ClientData.BattleKilledNum, suiJiFen, tangJiFen);

        //    //群发消息
        //    SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_BATTLEKILLEDNUM);
        //}

        /// <summary>
        /// 通知角色大乱斗中杀人个数的指令[新的通知函数，只通知自己]
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBattleKilledNumCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int tangJiFen, int suiJiFen)
        {
            // 阵营战场改造 1.角色roleid 2.个人积分 3.本场最高分 4.教团得分 5.联盟得分 [12/23/2013 LiaoWei]
            int nTotal = BattleManager.BattleMaxPointNow;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, client.ClientData.BattleKilledNum, nTotal, tangJiFen, suiJiFen);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_BATTLEKILLEDNUM);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知角色大乱斗中杀人个数的指令,系统统一通知，每隔30秒一次，通知隋唐战场地图的玩家
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBattleKilledNumCmd(SocketListener sl, TCPOutPacketPool pool, int suiJiFen, int tangJiFen)
        {
            List<Object> objsList = Container.GetObjectsByMap(GameManager.BattleMgr.BattleMapCode);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -1, -1, -1, tangJiFen, suiJiFen);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_BATTLEKILLEDNUM);
        }

        /// <summary>
        /// 通知角斗场称号的信息
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void NotifyRoleBattleNameInfo(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.BattleNameStart, client.ClientData.BattleNameIndex);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGBATTLENAMEINFO);
        }

        /// <summary>
        /// 处理通知角斗场称号的超时
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void ProcessRoleBattleNameInfoTimeOut(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            //无称号
            if (client.ClientData.BattleNameIndex <= 0)
            {
                return;
            }

            long ticks = TimeUtil.NOW();
            if (ticks - client.ClientData.BattleNameStart < Global.MaxBattleNameTicks) //有称号，并且在有效时间中
            {
                return;
            }

            //有称号，已经超过了指定的时间
            client.ClientData.BattleNameIndex = 0;

            //异步写数据库，写入当前的角斗场称号信息
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEBATTLENAME,
                string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.BattleNameStart, client.ClientData.BattleNameIndex),
                null, client.ServerId);

            //通知角斗场称号的信息
            GameManager.ClientMgr.NotifyRoleBattleNameInfo(sl, pool, client);
        }

        /// <summary>
        /// 通知角斗场开始的人数和已经死亡的人数
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void NotifyRoleBattleRoleInfo(SocketListener sl, TCPOutPacketPool pool, int mapCode, int startTotalRoleNum, int allKilledRoleNum)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode); //发送给地图的用户
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", startTotalRoleNum, allKilledRoleNum);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYBATTLEROLEINFO);
        }

        /// <summary>
        /// 通知角斗场结束时的信息
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void NotifyRoleBattleEndInfo(SocketListener sl, TCPOutPacketPool pool, int mapCode, List<BattleEndRoleItem> endRoleItemList)
        {
            if (endRoleItemList.Count <= 0) return;

            List<Object> objsList = Container.GetObjectsByMap(mapCode); //发送给地图的用户
            if (null == objsList) return;

            byte[] bytesData = DataHelper.ObjectToBytes<List<BattleEndRoleItem>>(endRoleItemList);

            //群发消息
            SendToClients(sl, pool, null, objsList, bytesData, (int)TCPGameServerCmds.CMD_SPR_NOTIFYBATTLEENDINFO);
        }

        /// <summary>
        /// 通知角斗场阵营的信息变更
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void NotifyRoleBattleSideInfo(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.BattleWhichSide);
            TCPOutPacket tcpOutPacket = null;
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYBATTLESIDE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }


        /// <summary>
        /// 通知角斗场双方人员数量 [1/20/2014 LiaoWei]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void NotifyRoleBattlePlayerSideNumberEndInfo(SocketListener sl, TCPOutPacketPool pool, int mapCode, int nNum1, int nNum2)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode); //发送给地图的用户
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", nNum1, nNum2);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_BATTLEPLAYERNUMNOTIFY);
        }

        #endregion 大乱斗相关

        #region 自动战斗相关

        /// <summary>
        /// 通知角色自动战斗的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAutoFightCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int status, int fightType, int extTag1)
        {
            //string strcmd = string.Format("{0}:{1}:{2}:{3}", status, client.ClientData.RoleID, fightType, extTag1);
            //TCPOutPacket tcpOutPacket = null;

            //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_AUTOFIGHT);
            //if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            //{
            //    //
            //    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
            //        tcpOutPacket.PacketCmdID,
            //        tcpOutPacket.PacketDataSize,
            //        client.ClientData.RoleID,
            //        client.ClientData.RoleName));*/
            //}

            SCAutoFight scData = new SCAutoFight(status, client.ClientData.RoleID, fightType, extTag1);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_AUTOFIGHT, scData);
        }

        #endregion 自动战斗相关

        #region 组队相关

        /// <summary>
        /// 通知角色组队的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int status, int teamType, int extTag1, string extTag2, int nOccu = -1, int nLev = -1, int nChangeLife = -1)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", status, client.ClientData.RoleID, teamType, extTag1, extTag2, nOccu, nLev, nChangeLife);
            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_TEAM);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知组队数据的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamData(SocketListener sl, TCPOutPacketPool pool, TeamData td)
        {
            if (null != td)
            {
                lock (td)
                {
                    byte[] bytesData = DataHelper.ObjectToBytes<TeamData>(td);
                    TCPOutPacket tcpOutPacket = null;
                    for (int i = 0; i < td.TeamRoles.Count; i++)
                    {
                        GameClient client = FindClient(td.TeamRoles[i].RoleID);
                        if (null == client) continue;

                        tcpOutPacket = pool.Pop();
                        tcpOutPacket.PacketCmdID = (UInt16)TCPGameServerCmds.CMD_SPR_TEAMDATA;
                        tcpOutPacket.FinalWriteData(bytesData, 0, (int)bytesData.Length);

                        if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                        {
                            //
                            /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                tcpOutPacket.PacketCmdID,
                                tcpOutPacket.PacketDataSize,
                                client.ClientData.RoleID,
                                client.ClientData.RoleName));*/
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 组队状态变化通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersTeamIDChanged(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.TeamID, Global.GetGameClientTeamLeaderID(client.ClientData));

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_TEAMID);
        }

        /// <summary>
        /// 组队解散通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersTeamDestroy(SocketListener sl, TCPOutPacketPool pool, GameClient client, TeamData td)
        {
            if (null != td)
            {
                lock (td)
                {
                    for (int i = 0; i < td.TeamRoles.Count; i++)
                    {
                        GameClient gameClient = FindClient(td.TeamRoles[i].RoleID);
                        if (null == gameClient) continue;
                        if (client == gameClient) continue;

                        gameClient.ClientData.TeamID = 0;
                        GameManager.TeamMgr.RemoveRoleID2TeamID(gameClient.ClientData.RoleID);

                        NotifyOthersTeamIDChanged(sl, pool, gameClient);
                    }
                }
            }
        }

        /// <summary>
        /// 通知组队中的其他队员自己的级别发生了变化
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamUpLevel(SocketListener sl, TCPOutPacketPool pool, GameClient client, bool zhuanShengChanged = false)
        {
            if (client.ClientData.TeamID <= 0) //如果没有队伍
            {
                return;
            }

            //查找组队的数据
            TeamData td = GameManager.TeamMgr.FindData(client.ClientData.TeamID);
            if (null == td) //没有找到组队数据
            {
                return;
            }

            lock (td)
            {
                TCPOutPacket tcpOutPacket = null;
                for (int i = 0; i < td.TeamRoles.Count; i++)
                {
                    GameClient gc = FindClient(td.TeamRoles[i].RoleID);
                    if (null == gc) continue;

                    if (td.TeamRoles[i].RoleID == client.ClientData.RoleID)
                    {
                        td.TeamRoles[i].Level = client.ClientData.Level; //更新级别
                        td.TeamRoles[i].ChangeLifeLev = client.ClientData.ChangeLifeCount; //更新级别
                    }

                    string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.Level, client.ClientData.ChangeLifeCount);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYTEAMCHGLEVEL);
                    if (!sl.SendData(gc.ClientSocket, tcpOutPacket))
                    {
                        //
                        /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                            tcpOutPacket.PacketCmdID,
                            tcpOutPacket.PacketDataSize,
                            client.ClientData.RoleID,
                            client.ClientData.RoleName));*/
                    }
                }
            }
        }

        /// <summary>
        /// 通知自己战力改变
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ZhanLi"></param>
        public void NotifySelfChgZhanLi(GameClient client, int ZhanLi)
        {
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_NOTIFYSELFCHGZHANLI, ZhanLi);
        }

        /// <summary>
        /// 通知组队中的其他队员自己的战力发生了变化
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamCHGZhanLi(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            if (client.ClientData.TeamID <= 0) //如果没有队伍
            {
                return;
            }

            //查找组队的数据
            TeamData td = GameManager.TeamMgr.FindData(client.ClientData.TeamID);
            if (null == td) //没有找到组队数据
            {
                return;
            }

            lock (td)
            {
                TCPOutPacket tcpOutPacket = null;
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.CombatForce);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYTEAMCHGZHANLI);
                for (int i = 0; i < td.TeamRoles.Count; i++)
                {
                    GameClient gc = FindClient(td.TeamRoles[i].RoleID);
                    if (null == gc) continue;

                    if (td.TeamRoles[i].RoleID == client.ClientData.RoleID)
                    {
                        td.TeamRoles[i].CombatForce = client.ClientData.CombatForce; //更新战力
                    }

                    //若客户端修订版本小于1,不发送此消息
                    if (gc.CodeRevision < 1)
                    {
                        continue;
                    }

                    if (!sl.SendData(gc.ClientSocket, tcpOutPacket, false))
                    {
                        //
                        /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                            tcpOutPacket.PacketCmdID,
                            tcpOutPacket.PacketDataSize,
                            client.ClientData.RoleID,
                            client.ClientData.RoleName));*/
                    }
                }
                PushBackTcpOutPacket(tcpOutPacket);
            }
        }

        #endregion 组队相关

        #region 物品交易

        /// <summary>
        /// 通知请求物品交易的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyGoodsExchangeCmd(SocketListener sl, TCPOutPacketPool pool, int roleID, int otherRoleID, GameClient client, GameClient otherClient, int status, int exchangeType)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}", status, roleID, otherRoleID, exchangeType);
            TCPOutPacket tcpOutPacket = null;

            if (null != client)
            {
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GOODSEXCHANGE);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }

            if (null != otherClient)
            {
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GOODSEXCHANGE);
                if (!sl.SendData(otherClient.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        otherClient.ClientData.RoleID,
                        otherClient.ClientData.RoleName));*/
                }
            }
        }

        /// <summary>
        /// 通知请求物品交易数据的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyGoodsExchangeData(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient otherClient, ExchangeData ed)
        {
            byte[] bytesData = null;

            lock (ed)
            {
                bytesData = DataHelper.ObjectToBytes<ExchangeData>(ed);
            }

            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = pool.Pop();
            tcpOutPacket.PacketCmdID = (UInt16)TCPGameServerCmds.CMD_SPR_EXCHANGEDATA;
            tcpOutPacket.FinalWriteData(bytesData, 0, (int)bytesData.Length);

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }

            tcpOutPacket = pool.Pop();
            tcpOutPacket.PacketCmdID = (UInt16)TCPGameServerCmds.CMD_SPR_EXCHANGEDATA;
            tcpOutPacket.FinalWriteData(bytesData, 0, (int)bytesData.Length);

            if (!sl.SendData(otherClient.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    otherClient.ClientData.RoleID,
                    otherClient.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 断开时处理交易数据
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        private void ProcessExchangeData(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            // 判断如果正在交易中，则立刻通知对方取消交易
            if (client.ClientData.ExchangeID > 0)
            {
                ExchangeData ed = GameManager.GoodsExchangeMgr.FindData(client.ClientData.ExchangeID);
                if (null != ed)
                {
                    int otherRoleID = (ed.RequestRoleID == client.ClientData.RoleID) ? ed.AgreeRoleID : ed.RequestRoleID;

                    //首先检查对方有没有正在进行中的交易，如果有则拒绝
                    GameClient otherClient = GameManager.ClientMgr.FindClient(otherRoleID);
                    if (null != otherClient) //对方不在线，直接返回, 不再处理
                    {
                        if (otherClient.ClientData.ExchangeID > 0 && otherClient.ClientData.ExchangeID == client.ClientData.ExchangeID)
                        {
                            //删除交易数据
                            GameManager.GoodsExchangeMgr.RemoveData(client.ClientData.ExchangeID);

                            // 从交易数据中恢复自己的数据
                            Global.RestoreExchangeData(otherClient, ed);

                            otherClient.ClientData.ExchangeID = 0; //重置交易ID
                            otherClient.ClientData.ExchangeTicks = 0;

                            //通知请求物品交易的指令信息
                            GameManager.ClientMgr.NotifyGoodsExchangeCmd(sl, pool, client.ClientData.RoleID, otherRoleID, null, otherClient, client.ClientData.ExchangeID, (int)GoodsExchangeCmds.Cancel);
                        }
                    }
                }
            }
        }

        #endregion 物品交易

        #region 掉落包裹相关

        /// <summary>
        /// 物品掉落通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        //public void NotifyOthersNewGoodsPack(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, int ownerRoleID, string ownerRoleName, int autoID, int goodsPackID, int mapCode, int toX, int toY, int goodsID, int goodsNum, long productTicks, int teamID, string teamRoleIDs)
        //{
        //    if (null == objsList) return;

        //    string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", ownerRoleID, ownerRoleName, autoID, goodsPackID, toX, toY, goodsID, goodsNum, productTicks, teamID, teamRoleIDs);

        //    //群发消息
        //    SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_NEWGOODSPACK);
        //}

        /// <summary>
        /// 物品掉落通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfNewGoodsPack(SocketListener sl, TCPOutPacketPool pool, GameClient client, int ownerRoleID, string ownerRoleName, int autoID, int goodsPackID, int mapCode, int toX, int toY, int goodsID, int goodsNum, long productTicks, int teamID, string teamRoleIDs, int lucky, int excellenceInfo, int appendPropLev, int forge_Level)
        {
            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}:{13}:{14}", ownerRoleID, ownerRoleName, autoID, goodsPackID, toX, toY, goodsID, goodsNum, productTicks, teamID, teamRoleIDs, lucky, excellenceInfo, appendPropLev, forge_Level);

            NewGoodsPackData newGoodsPackData = new NewGoodsPackData()
            {
                ownerRoleID = ownerRoleID,
                ownerRoleName = ownerRoleName,
                autoID = autoID,
                goodsPackID = goodsPackID,
                mapCode = mapCode,
                toX = toX,
                toY = toY,
                goodsID = goodsID,
                goodsNum = goodsNum,
                productTicks = productTicks,
                teamID = teamID,
                teamRoleIDs = teamRoleIDs,
                lucky = lucky,
                excellenceInfo = excellenceInfo,
                appendPropLev = appendPropLev,
                forge_Level = forge_Level,
            };

            //TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NEWGOODSPACK);

            byte[] bytes = DataHelper.ObjectToBytes<NewGoodsPackData>(newGoodsPackData);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_NEWGOODSPACK);

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 物品拾取通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfGetThing(SocketListener sl, TCPOutPacketPool pool, GameClient client, int goodsDbID)
        {
            string strcmd = "";
            strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, goodsDbID);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GETTHING);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket)) //告诉客户端已经获取的物品
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 物品掉落消失通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersDelGoodsPack(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, int mapCode, int autoID, int toRoleID)
        {
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", autoID, toRoleID);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELGOODSPACK);
        }

        /// <summary>
        /// 物品掉落消失通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfDelGoodsPack(SocketListener sl, TCPOutPacketPool pool, GameClient client, int autoID)
        {
            //string strcmd = string.Format("{0}:{1}", autoID, client.ClientData.RoleID);
            string strcmd = string.Format("{0}:{1}", autoID, -1);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELGOODSPACK);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 掉落包裹相关

        #region 通知客户端的伤害消息

        /// <summary>
        /// 通知自己攻击数据
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="roleID"></param>
        /// <param name="_AtkObj"></param>
        /// <param name="_SkillHarm"></param>
        /// <param name="_SkillID"></param>
        /// <param name="eMerlinType"></param>
        public static void NotifySelfEnemyInjured(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, List<Object> _AtkObj, List<int> _SkillHarm, int _SkillID, Point _targetPos,
            EMerlinSecretAttrType eMerlinType = EMerlinSecretAttrType.EMSAT_None)
        {
            PointAttack szPointAttack = new PointAttack
            {
                PosX = (int)_targetPos.X,
                PosY = (int)_targetPos.Y,
            };
            LogManager.WriteLog(LogTypes.Robot, string.Format("通知自己攻击伤害信息 坐标X={0} Y={1} ", szPointAttack.PosX, szPointAttack.PosY));
            SysConOut.WriteLine(string.Format("通知自己攻击伤害信息 坐标X={0} Y={1} ", szPointAttack.PosX, szPointAttack.PosY));

           

            ProAttackDataReponse proAttackDataReponse = new ProAttackDataReponse() {
                attackerLevel = client.ClientData.Level,
                attackerRoleID = client.ClientData.RoleID,
                SkillID = _SkillID,
                attackerPosX = (int)client.CurrentPos.X,
                attackerPosY = (int)client.CurrentPos.Y,
                LockPoint = szPointAttack
            };
            for (int i = 0; i < _AtkObj.Count; i++)
            {
                int szMaxinjure, szMininjure;
                szMaxinjure = szMininjure = 0;
                if (_SkillHarm.Count > 0)
                {
                    szMininjure = _SkillHarm.ToArray()[0];
                    szMaxinjure = _SkillHarm.ToArray()[1];
                }
                
                int szinjure = Global.GetRandomNumber(0, szMaxinjure - szMininjure) + szMininjure;

                AttackDataInfo attackDataInfo = new AttackDataInfo();
                attackDataInfo.injuredRoleMagic = 0;
                attackDataInfo.injuredRoleMaxMagicV = 0;
                attackDataInfo.burst = 0;
                attackDataInfo.injure = szinjure;
                attackDataInfo.currentExperience = client.ClientData.Experience;


                if (_AtkObj[i] is Monster)
                {
                    Monster tmpMonster = _AtkObj[i] as Monster;
                    attackDataInfo.injuredRoleID = tmpMonster.RoleID;
                    attackDataInfo.injuredRoleLife = (int)tmpMonster.VLife - szinjure;
                    attackDataInfo.injuredRoleMaxLifeV = (int)tmpMonster.XMonsterInfo.MaxHP;
                    tmpMonster.VLife = tmpMonster.VLife - szinjure;
                    if (tmpMonster.VLife <= 0.0)
                    {
                        GameManager.MonsterMgr.ProcessMonsterDead(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, tmpMonster, 100, 100, szinjure);
                        Global.ProcessMonsterDieForRoleAttack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, tmpMonster, szinjure);
                    }
                    else
                    {
                        //锁定攻击自己的敌人
                        if (tmpMonster.LockObject < 0)
                        {
                            tmpMonster.LockObject = client.ClientData.RoleID;
                            tmpMonster.LockFocusTime = TimeUtil.NOW();
                        }
                    }
                    attackDataInfo.injuredType = (int)ObjectTypes.OT_MONSTER;
                    proAttackDataReponse.AttackObjList.Add(attackDataInfo);

                }
                else
                    if(_AtkObj[i] is GameClient)
                {
                    GameClient tmpGameClient = _AtkObj[i] as GameClient;
                    if (tmpGameClient.ClientData.RoleID == client.ClientData.RoleID)
                        continue;
                    attackDataInfo.injuredRoleID = tmpGameClient.ClientData.RoleID;
                    attackDataInfo.injuredRoleLife = (int)tmpGameClient.ClientData.CurrentLifeV - szinjure;
                    attackDataInfo.injuredRoleMaxLifeV = (int)tmpGameClient.ClientData.LifeV;
                    tmpGameClient.ClientData.CurrentLifeV = tmpGameClient.ClientData.CurrentLifeV - szinjure;
                    attackDataInfo.injuredType = (int)ObjectTypes.OT_CLIENT;
                    proAttackDataReponse.AttackObjList.Add(attackDataInfo);
                }
                // Monster tmpMonster = _AtkObj[i] as Monster;
         /*       AttackDataInfo attackDataInfo = new AttackDataInfo()
                {
                   
                    injuredRoleID = tmpMonster.RoleID,
                   
                    injuredRoleLife = (int)tmpMonster.VLife - szinjure,
#if ___CC___FUCK___YOU___BB___
                    injuredRoleMaxLifeV = (int)tmpMonster.XMonsterInfo.MaxHP,
#else
             injuredRoleMaxLifeV = (int)tmpMonster.MonsterInfo.VLifeMax,
#endif

                    injuredRoleMagic = 0,
                    injuredRoleMaxMagicV = 0,
                    burst = 0,
                    injure = szinjure,
                    currentExperience = client.ClientData.Experience

                };
                
                tmpMonster.VLife = tmpMonster.VLife - szinjure;
                proAttackDataReponse.AttackObjList.Add(attackDataInfo);

                if (tmpMonster.VLife <= 0.0)
                {
                   
                    GameManager.MonsterMgr.ProcessMonsterDead(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, tmpMonster, 100, 100, szinjure);
                    Global.ProcessMonsterDieForRoleAttack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, tmpMonster, szinjure);
                   
                }
                else
                {
                    //锁定攻击自己的敌人
                    if (tmpMonster.LockObject < 0)
                    {
                        tmpMonster.LockObject = client.ClientData.RoleID;
                        tmpMonster.LockFocusTime = TimeUtil.NOW();
                    }
                }*/


            }
            TCPOutPacket tcpOutPacket = DataHelper.ProtocolToTCPOutPacket<ProAttackDataReponse>(proAttackDataReponse, Global._TCPManager.TcpOutPacketPool, (int)CommandID.CMD_PLAY_ATTACK);

            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
            }
            List<Object> szClientList;
            Global.GetMapAllGameClient(client.ClientData.MapCode, out szClientList);
            for (int i = 0; i < _AtkObj.Count; i++)
            {
                if (_AtkObj[i] is Monster)
                {
                    Monster tmpMonster = _AtkObj[i] as Monster;
                    if (tmpMonster.VLife <= 0.0)
                    {
                        GameManager.MonsterMgr.NotifyMonsterDead(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, szClientList, tmpMonster.RoleID);
                    }
                }
                else
                {
                    GameClient tmpGameClient = _AtkObj[i] as GameClient;
                    if (tmpGameClient.ClientData.CurrentLifeV <= 0.0)
                    {
                        GameManager.MonsterMgr.NotifyMonsterDead(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, szClientList, tmpGameClient.ClientData.RoleID);
                    }
                }
               
            }
           
        }
        /// <summary>
        /// 向自己发送敌人受伤的信息
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="roleID"></param>
        /// <param name="enemy"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        /// <param name="cmd"></param>
        /// <param name="enemyLife"></param>
        public static void NotifySelfEnemyInjured(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, int enemy, int burst, int injure, double enemyLife, long newExperience, int nMerlinInjure = 0, EMerlinSecretAttrType eMerlinType = EMerlinSecretAttrType.EMSAT_None)
        {
            ProAttackDataReponse szProAttackDataReponse = new ProAttackDataReponse();
            //szProAttackDataReponse.enemy = enemy;
            //szProAttackDataReponse.burst = burst;
            //szProAttackDataReponse.injure = injure;
            //szProAttackDataReponse.enemyLife = enemyLife;
            //szProAttackDataReponse.newExperience = newExperience;
            //szProAttackDataReponse.currentExperience = client.ClientData.Experience;
            //szProAttackDataReponse.newLevel = client.ClientData.Level;

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", enemy, burst, injure, enemyLife, newExperience, client.ClientData.Experience, client.ClientData.Level);
            //SpriteAttackResultData attackResultData = new SpriteAttackResultData();
            //attackResultData.enemy = enemy;
            //attackResultData.burst = burst;
            //attackResultData.injure = injure;
            //attackResultData.enemyLife = enemyLife;
            //attackResultData.newExperience = newExperience;
            //attackResultData.currentExperience = client.ClientData.Experience;
            //attackResultData.newLevel = client.ClientData.Level;

            //2015-9-16消息流量优化
            //if (nMerlinInjure > 0)
            //{
            //    szProAttackDataReponse.MerlinInjuer = nMerlinInjure; // 梅林伤害 [XSea 2015/6/26]
            //    szProAttackDataReponse.MerlinType = (int)eMerlinType; // 梅林类型 [XSea 2015/6/26]
            //}
            TCPOutPacket tcpOutPacket = DataHelper.ProtocolToTCPOutPacket<ProAttackDataReponse>(szProAttackDataReponse, pool, (int)CommandID.CMD_PLAY_ATTACK);
            //byte[] cmdData = DataHelper.ObjectToBytes<SpriteAttackResultData>(attackResultData);
            //System.Console.WriteLine(String.Format("{0} 使用技能——NotifySelfEnemyInjured", roleID));
            //TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, /*strcmd*/cmdData, 0, cmdData.Length, (int)TCPGameServerCmds.CMD_SPR_ATTACK);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
               
            }
        }

        /// <summary>
        /// 获取受伤的对象
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="injuredRoleID"></param>
        /// <returns></returns>
        private IObject GetInjuredObject(int mapCode, int injuredRoleID)
        {
            IObject injuredObj = null;

            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            GSpriteTypes st = Global.GetSpriteType((UInt32)injuredRoleID);
            if (st == GSpriteTypes.Monster)
            {
                //通知敌人自己开始攻击他，并造成了伤害
                Monster monster = GameManager.MonsterMgr.FindMonster(mapCode, injuredRoleID);
                if (null != monster)
                {
                    injuredObj = monster;
                }
            }
            else if (st == GSpriteTypes.BiaoChe) //如果是镖车
            {
                //暂时系统不支持，也不增加了
                BiaoCheManager.FindBiaoCheByRoleID(injuredRoleID);
            }
            else if (st == GSpriteTypes.JunQi) //如果是帮旗
            {
                return JunQiManager.FindJunQiByID(injuredRoleID);
            }
            else
            {
                //通知敌人自己开始攻击他，并造成了伤害
                GameClient obj = GameManager.ClientMgr.FindClient(injuredRoleID);
                if (null != obj)
                {
                    injuredObj = obj;
                }
            }

            return injuredObj;
        }
        /// <summary>
        /// 通知所有在线用户攻击了别人或者怪物
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="roleID"></param>
        /// <param name="_AtkObj"></param>
        /// <param name="_SkillHarm"></param>
        /// <param name="_SkillID"></param>
        /// <param name="eMerlinType"></param>
        public void NotifySpriteInjured(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, List<Object> _AtkObj, List<int> _SkillHarm, int _SkillID, Point _targetPos,
           EMerlinSecretAttrType eMerlinType = EMerlinSecretAttrType.EMSAT_None)
        {
            PointAttack szPointAttack = new PointAttack {
                PosX = (int)_targetPos.X,
                PosY = (int)_targetPos.Y,
            };
            LogManager.WriteLog(LogTypes.Robot, string.Format("通知自己攻击伤害信息 坐标X={0} Y={1} ", szPointAttack.PosX, szPointAttack.PosY));
            SysConOut.WriteLine(string.Format("通知自己攻击伤害信息 坐标X={0} Y={1} ", szPointAttack.PosX, szPointAttack.PosY));
            ProAttackDataReponse proAttackDataReponse = new ProAttackDataReponse() {
                attackerLevel = client.ClientData.Level,
                attackerRoleID = client.ClientData.RoleID,
                SkillID = _SkillID,
                attackerPosX = (int)client.CurrentPos.X,
                attackerPosY = (int)client.CurrentPos.Y,
                LockPoint = szPointAttack
            };
            for (int i = 0; i < _AtkObj.Count; i++)
            {
                //非对手就可以
                //if (!Global.IsOpposition(client, (_AtkObj[i] as Monster)))
                //{
                //    continue;
                //}
                int szMaxinjure, szMininjure;
                szMaxinjure = szMininjure = 0;
                if (_SkillHarm.Count > 0)
                {
                    szMininjure = _SkillHarm.ToArray()[0];
                    szMaxinjure = _SkillHarm.ToArray()[1];
                }
                
                int szinjure = Global.GetRandomNumber(0, szMaxinjure - szMininjure) + szMininjure;
                AttackDataInfo attackDataInfo = new AttackDataInfo();
                attackDataInfo.injuredRoleMagic = 0;
                attackDataInfo.injuredRoleMaxMagicV = 0;
                attackDataInfo.burst = 0;
                attackDataInfo.injure = szinjure;
                attackDataInfo.currentExperience = client.ClientData.Experience;

                if (_AtkObj[i] is Monster)
                {
                    Monster tmpMonster = _AtkObj[i] as Monster;
                    attackDataInfo.injuredRoleID = tmpMonster.RoleID;
                    attackDataInfo.injuredRoleLife = (int)tmpMonster.VLife - szinjure;
                    attackDataInfo.injuredRoleMaxLifeV = (int)tmpMonster.XMonsterInfo.MaxHP;
                    tmpMonster.VLife = tmpMonster.VLife - szinjure;
                    attackDataInfo.injuredType = (int)ObjectTypes.OT_MONSTER;
                    proAttackDataReponse.AttackObjList.Add(attackDataInfo);

                }
                else
                    if (_AtkObj[i] is GameClient)
                {
                    GameClient tmpGameClient = _AtkObj[i] as GameClient;
                    if (tmpGameClient.ClientData.RoleID == client.ClientData.RoleID)
                        continue;
                    attackDataInfo.injuredRoleID = tmpGameClient.ClientData.RoleID;
                    attackDataInfo.injuredRoleLife = (int)tmpGameClient.ClientData.CurrentLifeV - szinjure;
                    attackDataInfo.injuredRoleMaxLifeV = (int)tmpGameClient.ClientData.LifeV;
                    tmpGameClient.ClientData.CurrentLifeV = tmpGameClient.ClientData.CurrentLifeV - szinjure;
                    attackDataInfo.injuredType = (int)ObjectTypes.OT_CLIENT;
                    proAttackDataReponse.AttackObjList.Add(attackDataInfo);
                }

                






             /*   Monster tmpMonster = _AtkObj[i] as Monster;
                AttackDataInfo attackDataInfo = new AttackDataInfo()
                {
                   
                    injuredRoleID = tmpMonster.RoleID,
                    
                    injuredRoleLife = (int)tmpMonster.VLife - szinjure,
#if ___CC___FUCK___YOU___BB___
                    injuredRoleMaxLifeV = (int)tmpMonster.XMonsterInfo.MaxHP,
#else
             injuredRoleMaxLifeV = (int)tmpMonster.MonsterInfo.VLifeMax,
#endif

                    injuredRoleMagic = 0,
                    injuredRoleMaxMagicV = 0,
                    burst = 0,
                    injure = szinjure,
                    currentExperience = client.ClientData.Experience

                };
                proAttackDataReponse.AttackObjList.Add(attackDataInfo);*/
            }
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList)
            {
                objsList = new List<object>();
            }
            foreach(var s in objsList)
            {
                if(s is GameClient)
                {
                    SysConOut.WriteLine(string.Format("==========>广播战斗数据给 用户 = {0} 受击个数{1}",(s as GameClient).ClientData.RoleID,proAttackDataReponse.AttackObjList.Count));
                }
            }

            SendProtocolToClients<ProAttackDataReponse>(sl, pool, client, objsList, proAttackDataReponse, (int)CommandID.CMD_PLAY_ATTACK);
        }

        /// <summary>
        /// 通知所有在线用户某个精灵的被伤害(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySpriteInjured(SocketListener sl, TCPOutPacketPool pool, IObject attacker, int mapCode, int attackerRoleID, int injuredRoleID, int burst, int injure, double injuredRoleLife,
            int attackerLevel, Point hitToGrid, int nMerlinInjure = 0, EMerlinSecretAttrType eMerlinType = EMerlinSecretAttrType.EMSAT_None)
        {
            if (hitToGrid.X < 0 || hitToGrid.Y < 0)
            {
                hitToGrid.X = 0;
                hitToGrid.Y = 0;
            }
            System.Console.WriteLine(String.Format("{0} 使用技能——NotifySpriteInjured", attackerRoleID));
            //获取受伤的对象
            IObject injuredObj = GetInjuredObject(mapCode, injuredRoleID);
            if (null == injuredObj)
            {
                return;
            }

            List<Object> objsList = Global.GetAll9Clients(attacker);
            if (null == objsList)
            {
                objsList = new List<object>();
            }

            //2015-9-16消息流量优化
            if (GameManager.FlagHideFlagsType == -1 || !GameManager.HideFlagsMapDict.ContainsKey(mapCode))
            {
                if (objsList.IndexOf(injuredObj) < 0)
                {
                    objsList.Add(injuredObj);
                }

                int injuredRoleMagic = 0;
                int injuredRoleMaxMagicV = 0;
                int injuredRoleMaxLifeV = 0;
                GameClient injuredClient = FindClient(injuredRoleID);
                if (null != injuredClient)
                {
                    injuredRoleMagic = injuredClient.ClientData.CurrentMagicV;
                    injuredRoleMaxMagicV = injuredClient.ClientData.MagicV;
                    injuredRoleMaxLifeV = injuredClient.ClientData.LifeV;
                }

                SpriteInjuredData injuredData = new SpriteInjuredData();
                injuredData.attackerRoleID = attackerRoleID;
                injuredData.injuredRoleID = injuredRoleID;
                injuredData.burst = burst;
                injuredData.injure = injure;
                injuredData.injuredRoleLife = injuredRoleLife;
                injuredData.attackerLevel = attackerLevel;
                injuredData.injuredRoleMaxLifeV = injuredRoleMaxLifeV;
                injuredData.injuredRoleMagic = injuredRoleMagic;
                injuredData.injuredRoleMaxMagicV = injuredRoleMaxMagicV;
                injuredData.hitToGridX = (int)hitToGrid.X;
                injuredData.hitToGridY = (int)hitToGrid.Y;
                injuredData.MerlinInjuer = nMerlinInjure; // 梅林伤害值 [XSea 2015/6/26]
                injuredData.MerlinType = (sbyte)eMerlinType; // 梅林伤害类型 [XSea 2015/6/26]

                byte[] bytesCmd = DataHelper.ObjectToBytes<SpriteInjuredData>(injuredData);
                //群发消息
                SendToClients(sl, pool, null, objsList, /*strcmd*/bytesCmd, (int)TCPGameServerCmds.CMD_SPR_INJURE);

                //判断精灵是否组队中，如果是，则也通知九宫格之外的队友
                //通知被伤害的用户的队友伤害的数据
                NotifySpriteTeamInjured(sl, pool, injuredRoleID, /*strcmd*/bytesCmd, mapCode);
            }
            else
            {
                int injuredRoleMagic = 0;
                int injuredRoleMaxMagicV = 0;
                int injuredRoleMaxLifeV = 0;
                SpriteInjuredData injuredData = new SpriteInjuredData();

                //先准备发给别人的数据
                injuredData.injuredRoleID = injuredRoleID;
                injuredData.injuredRoleLife = injuredRoleLife;
                injuredData.burst = burst;
                injuredData.injure = injure;
                if (hitToGrid.X > 0 || hitToGrid.Y > 0)
                {
                    injuredData.hitToGridX = (int)hitToGrid.X;
                    injuredData.hitToGridY = (int)hitToGrid.Y;
                    injuredData.attackerRoleID = attackerRoleID; //击退时,需要知道攻击者,以便计算
                }

                if (nMerlinInjure > 0)
                {
                    injuredData.MerlinInjuer = nMerlinInjure; // 梅林伤害值 [XSea 2015/6/26]
                    injuredData.MerlinType = (sbyte)eMerlinType; // 梅林伤害类型 [XSea 2015/6/26]
                }

                //准备发给攻击者的数据
                //injuredData.injuredRoleMaxLifeV = injuredRoleMaxLifeV;
                //injuredData.injuredRoleMagic = injuredRoleMagic;
                //injuredData.injuredRoleMaxMagicV = injuredRoleMaxMagicV;

                //目前不需要显示的如魔法值等的信息暂不发送
                if (null != injuredObj && injuredObj.ObjectType == ObjectTypes.OT_CLIENT)
                {
                    if (objsList.IndexOf(injuredObj) < 0)
                    {
                        objsList.Add(injuredObj);
                    }

                    //准备发给被攻击者的数据
                    //injuredData.attackerLevel = attackerLevel;
                    //GameClient injuredClient = FindClient(injuredRoleID);
                    //if (null != injuredClient)
                    //{
                    //  injuredRoleMagic = injuredClient.ClientData.CurrentMagicV;
                    //  injuredRoleMaxMagicV = injuredClient.ClientData.MagicV;
                    //  injuredRoleMaxLifeV = injuredClient.ClientData.LifeV;
                    //}
                }

                bool dead = (injuredRoleLife <= 0);
                if (dead) injuredData.attackerRoleID = attackerRoleID;

                byte[] bytesCmd = DataHelper.ObjectToBytes<SpriteInjuredData>(injuredData);
                if (dead)
                {
                    SendToClients(sl, pool, null, objsList, /*strcmd*/bytesCmd, (int)TCPGameServerCmds.CMD_SPR_INJURE, ClientHideFlags.HideOtherMagicAndInjured, injuredRoleID);
                }
                else
                {
                    SendToClients(sl, pool, null, objsList, /*strcmd*/bytesCmd, (int)TCPGameServerCmds.CMD_SPR_INJURE, ClientHideFlags.HideOtherMagicAndInjured, injuredRoleID);
                }

                NotifySpriteTeamInjured(sl, pool, injuredRoleID, /*strcmd*/bytesCmd, mapCode);
            }
        }

        //         / <summary>
        /// 通知被伤害的用户的队友伤害的数据
        /// </summary>
        /// <param name="client"></param>
        public void NotifySpriteTeamInjured(SocketListener sl, TCPOutPacketPool pool, int injuredRoleID, /*string*/byte[] bytesCmd, int mapCode)
        {
            //判断精灵是否组队中，如果是，则也通知九宫格之外的队友
            GameClient otherClient = FindClient(injuredRoleID);
            if (null != otherClient)
            {
                if (otherClient.ClientData.TeamID > 0)
                {
                    //查找组队数据
                    TeamData td = GameManager.TeamMgr.FindData(otherClient.ClientData.TeamID);
                    if (null != td)
                    {
                        List<int> roleIDsList = new List<int>();

                        //锁定组队数据
                        lock (td)
                        {
                            for (int i = 0; i < td.TeamRoles.Count; i++)
                            {
                                if (injuredRoleID == td.TeamRoles[i].RoleID)
                                {
                                    continue;
                                }

                                roleIDsList.Add(td.TeamRoles[i].RoleID);
                            }
                        }
                        TCPOutPacket tcpOutPacket = null;
                        try
                        {
                            for (int i = 0; i < roleIDsList.Count; i++)
                            {
                                GameClient gc = FindClient(roleIDsList[i]);
                                if (null == gc) continue;
                                if (gc.ClientData.MapCode != mapCode)
                                {
                                    continue;
                                }

                                if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, /*strcmd*/bytesCmd, 0, bytesCmd.Length, (int)TCPGameServerCmds.CMD_SPR_INJURE);
                                if (!sl.SendData(gc.ClientSocket, tcpOutPacket, false))
                                {
                                    //
                                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                        tcpOutPacket.PacketCmdID,
                                        tcpOutPacket.PacketDataSize,
                                        gc.ClientData.RoleID,
                                        gc.ClientData.RoleName));*/
                                }
                            }
                        }
                        finally
                        {
                            PushBackTcpOutPacket(tcpOutPacket);
                        }
                    }
                }
            }
        }

        #endregion 通知客户端的伤害消息

        #region 个人紧要消息通知

        /// <summary>
        /// 通知在线的所有人(不限制地图)个人紧要消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllImportantMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string msgText, GameInfoTypeIndexes typeIndex, ShowGameInfoTypes showGameInfoType, int errCode = 0, int minZhuanSheng = 0, int minLevel = 0)
        {
            int index = 0;
            GameClient gc = null;
            while ((gc = GetNextClient(ref index)) != null)
            {
                if (null != client && gc == client)
                {
                    continue;
                }
                if (null != gc && Global.GetUnionLevel(gc) < Global.GetUnionLevel(minZhuanSheng, minLevel))
                {
                    continue;
                }

                //通知在线的对方(不限制地图)个人紧要消息
                NotifyImportantMsg(sl, pool, gc, msgText, typeIndex, showGameInfoType, errCode);
            }
        }

        /// <summary>
        /// 通知在线的所有帮会的人(不限制地图)个人紧要消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBangHuiImportantMsg(SocketListener sl, TCPOutPacketPool pool, int faction, string msgText, GameInfoTypeIndexes typeIndex, ShowGameInfoTypes showGameInfoType, int errCode = 0)
        {
            int index = 0;
            GameClient gc = null;
            while ((gc = GetNextClient(ref index)) != null)
            {
                if (faction != gc.ClientData.Faction)
                {
                    continue;
                }

                //通知在线的对方(不限制地图)个人紧要消息
                NotifyImportantMsg(sl, pool, gc, msgText, typeIndex, showGameInfoType, errCode);
            }
        }

        /// <summary>
        /// 通知在线的对方(不限制地图)个人紧要消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyImportantMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string msgText, GameInfoTypeIndexes typeIndex, ShowGameInfoTypes showGameInfoType, int errCode = 0)
        {
            //替换非法字符
            msgText = msgText.Replace(":", "``");

            TCPOutPacket tcpOutPacket = null;
            string strcmd = string.Format("{0}:{1}:{2}:{3}", (int)showGameInfoType, (int)typeIndex, msgText, errCode);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYMSG);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        public void NotifyImportantMsg(GameClient client, string msgText, GameInfoTypeIndexes typeIndex, ShowGameInfoTypes showGameInfoType, int errCode = 0)
        {
            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, msgText, typeIndex, showGameInfoType, errCode);
        }

        public void NotifyAddExpMsg(GameClient client, long addExp)
        {
            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, string.Format(Global.GetLang("您获得了：经验+{0}"), addExp),
                GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoTongQian);
        }

        public void NotifyAddJinBiMsg(GameClient client, int addJinBi)
        {
            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, StringUtil.substitute(Global.GetLang("您获得了：金币 + {0}"), addJinBi),
                GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoTongQian);
        }

        /// <summary>
        /// 通知客户端显示提示信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public void NotifyHintMsg(GameClient client, string msg)
        {
            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, msg,
                GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
        }

        /// <summary>
        /// 通知客户端显示提示信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public void NotifyCopyMapHintMsg(GameClient client, string msg)
        {
            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, msg,
                GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
        }

        #endregion 个人紧要消息通知

        #region 公告发布

        /// <summary>
        /// 通知GM授权消息
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="auth"></param>
        public void NotifyGMAuthCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int auth)
        {
            TCPOutPacket tcpOutPacket = null;
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, auth);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GMAUTH);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知在线的所有人(不限制地图)公告消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllBulletinMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, BulletinMsgData bulletinMsgData, int minZhuanSheng = 0, int minLevel = 0)
        {
            int index = 0;
            GameClient gc = null;
            while ((gc = GetNextClient(ref index)) != null)
            {
                if (null != client && gc == client)
                {
                    continue;
                }
                if (Global.GetUnionLevel(gc) < Global.GetUnionLevel(minZhuanSheng, minLevel))
                {
                    continue;
                }

                if (gc.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                //通知在线的对方(不限制地图)公告消息
                NotifyBulletinMsg(sl, pool, gc, bulletinMsgData);
            }
        }

        /// <summary>
        /// 通知在线的对方(不限制地图)公告消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBulletinMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, BulletinMsgData bulletinMsgData)
        {
            TCPOutPacket tcpOutPacket = null;
            tcpOutPacket = DataHelper.ObjectToTCPOutPacket<BulletinMsgData>(bulletinMsgData, pool, (int)TCPGameServerCmds.CMD_SPR_BULLETINMSG);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 公告发布

        #region PK值/PK点通知

        /// <summary>
        /// 通知PK值和PK点更新(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void ChangeRolePKValueAndPKPoint(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient enemy)
        {
            ///野蛮冲撞时，自己会受伤
            if (client == enemy)
            {
                return;
            }

            //角斗场 和 炎黄战场 中，红名功能失效
            if (client.ClientData.MapCode == GameManager.BattleMgr.BattleMapCode
                || client.ClientData.MapCode == GameManager.ArenaBattleMgr.BattleMapCode)
            {
                return;
            }

            //如果在王城争霸赛其间，失效
            if (WangChengManager.IsInCityWarBattling(client))
            {
                return;
            }

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(client.ClientData.MapCode, out gameMap))
            {
                return;
            }

            //判断地图的PK模式
            //PKMode, 0普通地图表示由用户的pk模式决定(加PK值), 1战斗地图表示强制PK模式(不加PK值), 2安全地图表示不允许PK;
            if ((int)MapPKModes.Normal != gameMap.PKMode)
            {
                return;
            }

            client.ClientData.PKValue = client.ClientData.PKValue + 1;

            //根据PK点计算出颜色索引值(0: 白色, 1:黄色, 2:红色)
            int enemyNameColorIndex = Global.GetNameColorIndexByPKPoints(enemy.ClientData.PKPoint);
            if (enemyNameColorIndex < 2) //杀红名不记PK点
            {
                //是否是紫名
                if (!Global.IsPurpleName(enemy))
                {
                    client.ClientData.PKPoint = Global.GMin(Global.MaxPKPointValue, client.ClientData.PKPoint + Global.PKValueEqPKPoints);
                }
            }
            else if (Global.IsRedName(client))
            {
                if (Global.AddToTodayRoleKillRoleSet(client.ClientData.RoleID, enemy.ClientData.RoleID))
                {
                    client.ClientData.PKPoint = Global.GMax(0, client.ClientData.PKPoint - Global.PKValueEqPKPoints / 2);
                }
            }

            // 给玩家更新红名处罚BUFFER [4/21/2014 LiaoWei]
            Global.ProcessRedNamePunishForDebuff(client);

            //更新PKValue
            //GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEPKVAL_CMD,
            //    string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.PKValue, client.ClientData.PKPoint),
            //    null);

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.PKValue, client.ClientData.PKPoint);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGPKVAL);
        }

        /// <summary>
        /// 通知PK值和PK点更新(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        //public void SubRedNameRolePKValueAndPKPoint(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient enemy)
        //{
        //    //角斗场 和 炎黄战场 中，红名功能失效
        //    if (client.ClientData.MapCode == GameManager.BattleMgr.BattleMapCode
        //        || client.ClientData.MapCode == GameManager.ArenaBattleMgr.BattleMapCode)
        //    {
        //        return;
        //    }

        //    GameMap gameMap = null;
        //    if (!GameManager.MapMgr.DictMaps.TryGetValue(client.ClientData.MapCode, out gameMap))
        //    {
        //        return;
        //    }

        //    //判断地图的PK模式
        //    //PKMode, 0普通地图表示由用户的pk模式决定(加PK值), 1战斗地图表示强制PK模式(不加PK值), 2安全地图表示不允许PK;
        //    if (1 == gameMap.PKMode)
        //    {
        //        return;
        //    }

        //    //自己是红名被杀，则减少自己的PK值
        //    if (client.ClientData.PKPoint < Global.MinRedNamePKPoints)
        //    {
        //        return;
        //    }

        //    //client.ClientData.PKValue = Global.GMax(0, client.ClientData.PKValue - 1);
        //    client.ClientData.PKPoint = Global.GMax(0, client.ClientData.PKPoint - Global.RedNameBeKilledSubPKPoints);


        //    List<Object> objsList = Global.GetAll9Clients(client);
        //    if (null == objsList) return;

        //    string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.PKValue, client.ClientData.PKPoint);

        //    //群发消息
        //    SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGPKVAL);
        //}

        /// <summary>
        /// 设置PK值(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void SetRolePKValuePoint(SocketListener sl, TCPOutPacketPool pool, GameClient client, int pkValue, int pkPoint, bool writeToDB = true)
        {
            client.ClientData.PKValue = pkValue;
            client.ClientData.PKPoint = pkPoint;

            //更新红名BUFF
            Global.ProcessRedNamePunishForDebuff(client);

            if (writeToDB)
            {
                //更新PKValue
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEPKVAL_CMD,
                    string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.PKValue, client.ClientData.PKPoint),
                    null, client.ServerId);

                long nowTicks = TimeUtil.NOW();
                Global.SetLastDBCmdTicks(client, (int)TCPGameServerCmds.CMD_DB_UPDATEPKVAL_CMD, nowTicks);
            }

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.PKValue, client.ClientData.PKPoint);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGPKVAL);
        }

        #endregion PK值/PK点通知

        #region 紫名管理

        /// <summary>
        /// 通知紫名信息(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void ChangeRolePurpleName(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient enemy)
        {
            ///野蛮冲撞自己对自己伤害，不记灰名
            if (client == enemy)
            {
                return;
            }

            //角斗场  和 炎黄战场 中，紫名功能失效
            if (client.ClientData.MapCode == GameManager.BattleMgr.BattleMapCode
                || client.ClientData.MapCode == GameManager.ArenaBattleMgr.BattleMapCode)
            {
                return;
            }

            //杀红名不会紫名
            if (enemy.ClientData.PKPoint >= Global.MinRedNamePKPoints)
            {
                return;
            }

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(client.ClientData.MapCode, out gameMap))
            {
                return;
            }

            //判断地图的PK模式
            //PKMode, 0普通地图表示由用户的pk模式决定(加PK值), 1战斗地图表示强制PK模式(不加PK值), 2安全地图表示不允许PK;
            if ((int)MapPKModes.Normal != gameMap.PKMode)
            {
                return;
            }

            //攻击紫名不会紫名
            //if (Global.IsPurpleName(enemy))
            //{
            //    return;
            //}

            //bool oldPurpleName = Global.IsPurpleName(client);

            //设置紫名的时间
            client.ClientData.StartPurpleNameTicks = TimeUtil.NOW();

            //是否是紫名, (重复更新紫名信息，会导致频繁的广播通讯)
            //if (oldPurpleName)
            //{
            //    return;
            //}

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.StartPurpleNameTicks);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGPURPLENAME);
        }

        /// <summary>
        /// 通知紫名信息(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void ForceChangeRolePurpleName2(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            //是否是紫名, (重复更新紫名信息，会导致频繁的广播通讯)
            if (Global.IsPurpleName(client))
            {
                return;
            }

            //设置紫名的时间
            client.ClientData.StartPurpleNameTicks = TimeUtil.NOW();

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.StartPurpleNameTicks);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGPURPLENAME);
        }

        /// <summary>
        /// 播报紫名的消失事件
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void BroadcastRolePurpleName(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            if (client.ClientData.StartPurpleNameTicks <= 0)
            {
                return;
            }

            if (Global.IsPurpleName(client))
            {
                return;
            }

            client.ClientData.StartPurpleNameTicks = 0;

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.StartPurpleNameTicks);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGPURPLENAME);
        }

        #endregion 紫名管理

        #region 聊天消息处理

        /// <summary>
        /// 通知在线的所有人(不限制地图)聊天消息(世界)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllChatMsg(SocketListener sl, TCPOutPacketPool pool, string cmdText, GameClient sender = null)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (sender != null && client.ClientSocket.IsKuaFuLogin)
                {
                    if (sender.ClientData.MapCode == client.ClientData.MapCode && sender.ClientData.CopyMapID == client.ClientData.CopyMapID)
                    {
                        SendChatMessage(sl, pool, client, cmdText);
                    }
                }
                else
                {
                    SendChatMessage(sl, pool, client, cmdText);
                }
            }
        }

        /// <summary>
        /// 通知在线的本帮会的人(不限制地图)聊天消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyFactionChatMsg(SocketListener sl, TCPOutPacketPool pool, int faction, string cmdText, GameClient sender = null)
        {
            if (faction <= 0) //没有加入任何帮会
            {
                return;
            }

            int index = 0;
            GameClient gc = null;
            while ((gc = GetNextClient(ref index)) != null)
            {
                if (gc.ClientData.Faction == faction)
                {
                    if (sender != null && gc.ClientSocket.IsKuaFuLogin)
                    {
                        if (sender.SceneGameId == gc.SceneGameId)
                        {
                            SendChatMessage(sl, pool, gc, cmdText);
                        }
                    }
                    else
                    {
                        SendChatMessage(sl, pool, gc, cmdText);
                    }
                }
            }
        }

        /// <summary>
        /// 通知在线的组队的人(不限制地图)聊天消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamChatMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string cmdText)
        {
            if (client.ClientData.TeamID <= 0)
            {
                return;
            }

            //查找组队数据
            TeamData td = GameManager.TeamMgr.FindData(client.ClientData.TeamID);
            if (null == td) return;

            List<int> roleIDsList = new List<int>();

            //锁定组队数据
            lock (td)
            {
                for (int i = 0; i < td.TeamRoles.Count; i++)
                {
                    roleIDsList.Add(td.TeamRoles[i].RoleID);
                }
            }

            if (roleIDsList.Count <= 0) return;
            for (int i = 0; i < roleIDsList.Count; i++)
            {
                GameClient gc = FindClient(roleIDsList[i]);
                if (null == gc) continue;
                SendChatMessage(sl, pool, gc, cmdText);
            }
        }

        /// <summary>
        /// 通知在线的所有附近的人(限制地图, 限制附近)聊天消息(附近)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMapChatMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string cmdText)
        {
            // 查找指定圆周范围内的敌人
            List<Object> clientList = new List<Object>();
            LookupRolesInCircle(null, client.ClientData.MapCode, client.ClientData.PosX, client.ClientData.PosY, 1000, clientList);
            if (clientList.Count <= 0) return;

            //群发消息
            SendToClients(sl, pool, null, clientList, cmdText, (int)TCPGameServerCmds.CMD_SPR_CHAT);
        }

        /// <summary>
        /// 相同副本地图的人聊天消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyCopyMapChatMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string cmdText)
        {
            if (client.ClientData.CopyMapID > 0 && client.ClientData.FuBenSeqID > 0)
            {
                CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(client.ClientData.MapCode, client.ClientData.FuBenSeqID);
                if (null != copyMap)
                {
                    List<object> clientList = copyMap.GetClientsList2();
                    if (null != clientList && clientList.Count > 0)
                    {
                        //群发消息
                        SendToClients(sl, pool, null, clientList, cmdText, (int)TCPGameServerCmds.CMD_SPR_CHAT);
                    }
                }
            }
        }

        /// <summary>
        /// 相同副本里相同阵营的人聊天消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBattleSideChatMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string cmdText)
        {
            //必须拥有阵营才能用阵营聊天
            if (client.ClientData.BattleWhichSide <= 0)
            {
                return;
            }

            //分副本地图和公共地图两种情况
            if (client.ClientData.CopyMapID > 0 && client.ClientData.FuBenSeqID > 0)
            {
                CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(client.ClientData.MapCode, client.ClientData.FuBenSeqID);
                if (null != copyMap)
                {
                    List<object> clientList = copyMap.GetClientsList2();
                    if (null != clientList && clientList.Count > 0)
                    {
                        //群发消息
                        foreach (var obj in clientList)
                        {
                            GameClient c = obj as GameClient;
                            if (null != c)
                            {
                                if (client.ClientData.BattleWhichSide == c.ClientData.BattleWhichSide)
                                {
                                    c.sendCmd((int)TCPGameServerCmds.CMD_SPR_CHAT, cmdText);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                List<GameClient> clientList = GameManager.ClientMgr.GetMapGameClients(client.ClientData.MapCode);
                foreach (var c in clientList)
                {
                    if (null != c)
                    {
                        if (client.ClientData.BattleWhichSide == c.ClientData.BattleWhichSide)
                        {
                            c.sendCmd((int)TCPGameServerCmds.CMD_SPR_CHAT, cmdText);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 通知(指定角色)聊天消息(全世界)
        /// </summary>
        /// <param name="client"></param>
        public bool NotifyClientChatMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, int fromRoleID, string fromRoleName, string toRoleName, int index, string textMsg, int chatType)
        {
            bool result = true;
            GameClient toClient = null;

            do
            {
                //根据ID查找敌人
                int roleID = RoleName2IDs.FindRoleIDByName(toRoleName);
                if (-1 == roleID)
                {
                    result = false;
                    break;
                }

                toClient = FindClient(roleID);
                if (null == toClient)
                {
                    result = false;
                    break;
                }

                //判断如果自己在对方的黑名单中，则无法查看其信息
                if (Global.InFriendsBlackList(toClient, fromRoleID))
                {
                    toClient = null;
                    break;
                }
            } while (false);

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", fromRoleID, fromRoleName, 0, toRoleName, index, textMsg, chatType);
            if (null != client)
            {
                SendChatMessage(sl, pool, client, strcmd);
            }

            if (null != toClient)
            {
                //给某个在线的角色发送消息
                SendChatMessage(sl, pool, toClient, strcmd);
            }
            else
            {
                string offlineTip = string.Format(Global.GetLang("{0} 不存在或者不在线！"), toRoleName); ;
                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, offlineTip);
            }

            return result;
        }

        /// <summary>
        /// 给某个在线的角色发送系统消息
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="roleName"></param>
        /// <param name="cmdText"></param>
        public void SendSystemChatMessageToClient(SocketListener sl, TCPOutPacketPool pool, GameClient client, string textMsg)
        {
            if (null != client)
            {
                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", -1, "", 0, "", (int)ChatTypeIndexes.System, textMsg, 0);
                SendChatMessage(sl, pool, client, strcmd);
            }
        }

        /// <summary>
        /// 给所有在线的角色发送系统消息
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="roleName"></param>
        /// <param name="cmdText"></param>
        public void SendSystemChatMessageToClients(SocketListener sl, TCPOutPacketPool pool, string textMsg)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", -1, "", 0, "", (int)ChatTypeIndexes.System, textMsg);
            NotifyAllChatMsg(sl, pool, strcmd);
        }

        /// <summary>
        /// 给某个在线的角色发送消息
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="roleName"></param>
        /// <param name="cmdText"></param>
        public void SendChatMessage(SocketListener sl, TCPOutPacketPool pool, GameClient client, string cmdText)
        {
            TCPOutPacket tcpOutPacket = null;
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, cmdText, (int)TCPGameServerCmds.CMD_SPR_CHAT);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 上次转发消息的时间
        /// </summary>
        private long LastTransferTicks = 0;

        /// <summary>
        /// 处理聊天消息转发的操作
        /// </summary>
        public void HandleTransferChatMsg()
        {
            long ticks = TimeUtil.NOW();
            if (ticks - LastTransferTicks < (5 * 1000))
            {
                return;
            }

            LastTransferTicks = ticks; //记录时间

            string strcmd = "";

            //向DBServer请求修改物品
            TCPOutPacket tcpOutPacket = null;
            //strcmd = string.Format("{0}", GameManager.ServerLineID); 修改，精简指令，提供服务器心跳的工作
            strcmd = string.Format("{0}:{1}:{2}:{3}", GameManager.ServerLineID, GameManager.ClientMgr.GetClientCount(), Global.SendServerHeartCount, GetMapcodeOnlineNumManager.CountMapIDOnlineNum());
            Global.SendServerHeartCount++; //为了标识是否是第一次

            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer2(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_GET_CHATMSGLIST, strcmd, out tcpOutPacket, GameManager.LocalServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("处理转发消息时，连接DBServer获取消息列表失败"));
                return;
            }

            if (null == tcpOutPacket)
            {
                return;
            }

            //转发的聊天消息
            List<string> chatMsgList = DataHelper.BytesToObject<List<string>>(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);

            //还回tcpOutPacket
            Global._TCPManager.TcpOutPacketPool.Push(tcpOutPacket);

            if (null == chatMsgList || chatMsgList.Count <= 0)
            {
                return;
            }

            //此处转发处理消息
            for (int i = 0; i < chatMsgList.Count; i++)
            {
                TransferChatMsg(chatMsgList[i]);
            }

            chatMsgList = null;
        }

        /// <summary>
        /// 转发消息
        /// </summary>
        /// <param name="chatMsg"></param>
        public void TransferChatMsg(string chatMsg)
        {
            try
            {
                //解析用户名称和用户密码
                string[] fields = chatMsg.Split(':');
                if (fields.Length != 9)
                {
                    return;
                }

                int roleID = Convert.ToInt32(fields[0]);
                string roleName = fields[1];
                int status = Convert.ToInt32(fields[2]);
                string toRoleName = fields[3];
                int index = Convert.ToInt32(fields[4]);
                string textMsg = fields[5];
                int chatType = Convert.ToInt32(fields[6]);
                int extTag1 = Convert.ToInt32(fields[7]);
                int serverLineID = Convert.ToInt32(fields[8]);
                if (serverLineID == GameManager.ServerLineID)
                {
                    return;
                }

                if (GameManager.systemGMCommands.ProcessChatMessage(null, null, textMsg, true)) //先判断是否GM消息
                {
                    ;//不处理
                }
                else
                {
                    if (index == (int)ChatTypeIndexes.Map) //附近喊话
                    {
                        ;//不处理
                    }
                    if (index == (int)ChatTypeIndexes.World) //向世界发送需要扣除小喇叭道具，所以检测如果不足，不发送，需要提前扣除
                    {
                        string cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", roleID, roleName, 0, toRoleName, index, textMsg, chatType);
                        GameManager.ClientMgr.NotifyAllChatMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, cmdData);
                    }
                    else if (index == (int)ChatTypeIndexes.Faction) //帮会聊天
                    {
                        string cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", roleID, roleName, 0, toRoleName, index, textMsg, chatType);
                        GameManager.ClientMgr.NotifyFactionChatMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, extTag1, cmdData);
                    }
                    else if (index == (int)ChatTypeIndexes.Team) //组队聊天
                    {
                        ;//不处理
                    }
                    else if (index == (int)ChatTypeIndexes.Private) //私人聊天
                    {
                        GameManager.ClientMgr.NotifyClientChatMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, roleID, roleName, toRoleName, index, textMsg, chatType);
                    }
                }
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, "TransferChatMsg", false);
                //throw ex;
                //});
            }
        }

        #endregion 聊天消息处理

        #region 角色攻击角色

        /// <summary>
        /// 记录战斗中的敌人
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="victim"></param>
        public void LogBatterEnemy(GameClient attacker, GameClient victim)
        {
            attacker.ClientData.RoleIDAttackebByMyself = victim.ClientData.RoleID;
            victim.ClientData.RoleIDAttackMe = attacker.ClientData.RoleID;
        }
        public void NotifyOtherInjured(SocketListener sl, TCPOutPacketPool pool, IObject attacker, List<Object> _defens, List<int> _SkillHarm, int _SkillID)
        {
            if (!(attacker is Monster))
                return;
            Monster szMonster = attacker as Monster;
            int szinjure = 0;


            ProAttackDataReponse proAttackDataReponse = new ProAttackDataReponse()
            {
                attackerLevel = szMonster.XMonsterInfo.Level,
                attackerRoleID = szMonster.RoleID,
                SkillID = _SkillID,
                attackerPosX = (int)szMonster.CurrentPos.X,
                attackerPosY = (int)szMonster.CurrentPos.Y
        };
            for (int i = 0; i < _defens.Count; i++)
            {
                //非对手就可以
                //if (!Global.IsOpposition(client, (_AtkObj[i] as Monster)))
                //{
                //    continue;
                //}
                int szMaxinjure, szMininjure;
                szMaxinjure = szMininjure = 0;
                if (_SkillHarm.Count > 0 && _SkillHarm.ToArray()[0] > 0)
                {
                    szMininjure = _SkillHarm.ToArray()[0];
                    szMaxinjure = _SkillHarm.ToArray()[1];
                }

                szinjure = Global.GetRandomNumber(0, szMaxinjure - szMininjure) + szMininjure + szMonster.XMonsterInfo.Ad;
                GameClient tmpGameClient = _defens[i] as GameClient;
                AttackDataInfo attackDataInfo = new AttackDataInfo()
                {

                    injuredRoleID = tmpGameClient.ClientData.RoleID,

                    injuredRoleLife = ((int)tmpGameClient.ClientData.LifeV - szinjure) > 0 ? ((int)tmpGameClient.ClientData.LifeV - szinjure) : 0,
#if ___CC___FUCK___YOU___BB___
                    injuredRoleMaxLifeV = 0,
#else
             injuredRoleMaxLifeV = (int)tmpMonster.MonsterInfo.VLifeMax,
#endif

                    injuredRoleMagic = 0,
                    injuredRoleMaxMagicV = 0,
                    burst = 0,
                    injure = szinjure,
                    currentExperience = 0

                };
                proAttackDataReponse.AttackObjList.Add(attackDataInfo);
                tmpGameClient.ClientData.LifeV = ((int)tmpGameClient.ClientData.LifeV - szinjure) > 0 ? ((int)tmpGameClient.ClientData.LifeV - szinjure) : 0;
                
            }
            List<Object> objsList = Global.GetAll9Clients(attacker);
            if (null == objsList)
            {
                objsList = new List<object>();
            }
            foreach (var s in objsList)
            {
                if (s is GameClient)
                {
                    SysConOut.WriteLine(string.Format("==========>广播战斗数据给 用户 = {0} 攻击者{1}  受击者{2}",
                        (s as GameClient).ClientData.RoleID, szMonster.RoleID,
                         (s as GameClient).ClientData.RoleID));
                }
            }

            SendProtocolToClients<ProAttackDataReponse>(sl, pool, attacker, objsList, proAttackDataReponse, (int)CommandID.CMD_PLAY_ATTACK);

            for (int i = 0; i < _defens.Count; i++)
            {
                GameClient tmpGameClient = _defens[i] as GameClient;
                if(tmpGameClient.ClientData.LifeV <= 0)
                {
                    //List<Object> szClientTempList = Global.GetAll9GridObjectsForClient(tmpGameClient);
                    //List<Object> szClientList = new List<object>();
                    //if (null != szClientList)
                    //    foreach (var s in szClientTempList)
                    //    {
                    //        if (s is GameClient)
                    //            szClientList.Add(s);
                    //    }
                    List<Object> szClientList;
                    Global.GetMapAllGameClient(tmpGameClient.ClientData.MapCode, out szClientList);
                    GameManager.MonsterMgr.NotifyMonsterDead(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, szClientList, tmpGameClient.ClientData.RoleID);
                }
            }
        }

        /// <summary>
        /// 通知其他人被攻击(单攻)，并且被伤害(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public int NotifyOtherInjured(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient enemy, int burst, 
            int injure, double injurePercnet, int attackType, bool forceBurst, int addInjure, double attackPercent, int addAttackMin, 
            int addAttackMax, int skillLevel, double skillBaseAddPercent, double skillUpAddPercent, bool ignoreDefenseAndDodge = false, 
            bool dontEffectDSHide = false, double baseRate = 1.0, int addVlue = 0, int nHitFlyDistance = 0)
        {
            int ret = 0;
            object obj = enemy;
            if ((obj as GameClient).ClientData.CurrentLifeV > 0)
            {
                //记录战斗中的敌人
                LogBatterEnemy(client, enemy);

                // 角色攻击角色的计算公式
                if (injure <= 0)
                {
                    if ((int)AttackType.PHYSICAL_ATTACK == attackType) //物理攻击
                    {
                        RoleAlgorithm.AttackEnemy(client, (obj as GameClient), forceBurst, injurePercnet, addInjure, attackPercent, addAttackMin, addAttackMax, out burst, out injure, ignoreDefenseAndDodge, baseRate, addVlue);
                    }
                    else if ((int)AttackType.MAGIC_ATTACK == attackType) //魔法攻击
                    {
                        RoleAlgorithm.MAttackEnemy(client, (obj as GameClient), forceBurst, injurePercnet, addInjure, attackPercent, addAttackMin, addAttackMax, out burst, out injure, ignoreDefenseAndDodge, baseRate, addVlue);
                    }
                    else //道术攻击
                    {
                        // 属性改造 去掉 道术攻击[8/15/2013 LiaoWei]
                        //RoleAlgorithm.DSAttackEnemy(client, (obj as GameClient), forceBurst, injurePercnet, addInjure, attackPercent, addAttack, out burst, out injure, ignoreDefenseAndDodge);
                    }
                }

                /*if (!Global.InCircle(new Point(enemy.ClientData.PosX, enemy.ClientData.PosY), new Point(client.ClientData.PosX, client.ClientData.PosY), Data.MaxAttackDistance)) //如果敌人已经离开了攻击点半径则视为闪避
                {
                    System.Diagnostics.Debug.WriteLine("GetRealHitRate{0}, out of circle, radius", 0);
                    injure = 0;
                }*/

                bool selfLifeChanged = false;
                if (injure > 0)
                {
                    RoleRelifeLog relifeLog = new RoleRelifeLog(
                        client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.MapCode,
                        /**/string.Format("打人rid={0},rname={1}击中恢复", enemy.ClientData.RoleID, enemy.ClientData.RoleName));
                    int lifeSteal = (int)RoleAlgorithm.GetLifeStealV(client);
                    if (lifeSteal > 0 && client.ClientData.CurrentLifeV < client.ClientData.LifeV)
                    {
                        relifeLog.hpModify = true;
                        relifeLog.oldHp = client.ClientData.CurrentLifeV;
                        selfLifeChanged = true;
                        client.ClientData.CurrentLifeV += lifeSteal;
                    }
                    if (client.ClientData.CurrentLifeV > client.ClientData.LifeV)
                    {
                        client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                    }
                    relifeLog.newHp = client.ClientData.CurrentLifeV;
                    MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);
                }

                //将对敌人的伤害进行处理
                injure = InjureToEnemy(sl, pool, enemy, injure, attackType, ignoreDefenseAndDodge, skillLevel);

                /// 被攻击时吸收一部分伤害(护身戒指)
                //injure -= SpecialEquipMgr.DoSubInJure((obj as GameClient), (int)ItemCategories.FashionWeapon, injure);

                //处理角色克星
                injure = DBRoleBufferManager.ProcessAntiRole(client, (obj as GameClient), injure);

                // PK的伤害为50% ChenXiaojun
                injure = injure / 2;

                #region 计算梅林伤害
                EMerlinSecretAttrType eMerlinType = EMerlinSecretAttrType.EMSAT_None; // 梅林伤害类型
                // 计算梅林伤害
                int nMerlinInjure = GameManager.MerlinInjureMgr.CalcMerlinInjure(client, enemy, injure, ref eMerlinType);
                #endregion

                // 扣血
                if (!GameManager.TestGamePerformanceMode || !GameManager.TestGamePerformanceLockLifeV)
                {
                    (obj as GameClient).ClientData.CurrentLifeV -= (int)Global.GMax(0, injure + nMerlinInjure); //是否需要锁定///////加上梅林伤害 [XSea 2015/6/26]
                }

                // 校正
                (obj as GameClient).ClientData.CurrentLifeV = Global.GMax((obj as GameClient).ClientData.CurrentLifeV, 0);

                // 卓越属性 有几率完全恢复血和蓝 [12/27/2013 LiaoWei]
                if (client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP15] > 0.0)
                {
                    int nRan = Global.GetRandomNumber(0, 101);
                    if (nRan <= client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP15] * 100)
                    {
                        client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                        selfLifeChanged = true; // 校正 血蓝改变 需要通知客户端 [XSea 2015/8/10]
                    }
                }

                if (client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP16] > 0.0)
                {
                    int nRan = Global.GetRandomNumber(0, 101);
                    if (nRan <= client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP16] * 100)
                    {
                        client.ClientData.CurrentMagicV = client.ClientData.MagicV;
                        selfLifeChanged = true; // 校正 血蓝改变 需要通知客户端 [XSea 2015/8/10]
                    }
                }

                //生命值为0时,立即回复100%生命值
                //SpecialEquipMgr.DoEquipRestoreBlood((obj as GameClient), (int)ItemCategories.FashionWeapon);

                //攻击时附加的效果(麻痹戒指附加一定几率的麻痹效果)
                //SpecialEquipMgr.DoEquipExtAttack(client, (int)ItemCategories.FashionWeapon, (obj as GameClient).ClientData.RoleID);

                int enemyLife = (obj as GameClient).ClientData.CurrentLifeV;

                //被攻击时减少装备耐久度
                (obj as GameClient).UsingEquipMgr.InjuredSomebody((obj as GameClient));

                //判断是否将给敌人的伤害转化成自己的血量增长
                SpriteInjure2Blood(sl, pool, client, injure);

                //GameManager.SystemServerEvents.AddEvent(string.Format("角色减血, roleID={0}({1}), Injure={2}, Life={3}", (obj as GameClient).ClientData.RoleID, (obj as GameClient).ClientData.RoleName, injure, enemyLife), EventLevels.Debug);

                //bool hitFly = (injure > ((obj as GameClient).ClientData.LifeV / 3));
                Point hitToGrid = new Point(-1, -1);
                //if (hitFly)
                //{
                //    hitToGrid = ChuanQiUtils.HitFly(client, (obj as GameClient), (obj as GameClient).ClientData.LifeV <= 0 ? 2 : 1);
                //}

                // 处理击飞 [3/15/2014 LiaoWei]
                if (nHitFlyDistance > 0)
                {
                    MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode];

                    int nGridNum = nHitFlyDistance * 100 / mapGrid.MapGridWidth;

                    if (nGridNum > 0)
                        hitToGrid = ChuanQiUtils.HitFly(client, enemy, nGridNum);
                }

                NotifySpriteInjured(sl, pool, client, client.ClientData.MapCode, client.ClientData.RoleID, (obj as GameClient).ClientData.RoleID, burst, injure, enemyLife, client.ClientData.Level, hitToGrid, nMerlinInjure, eMerlinType); // 加上梅林伤害与类型 [XSea 2015/6/26]

                //向自己发送敌人受伤的信息
                {
                    NotifySelfEnemyInjured(sl, pool, client, client.ClientData.RoleID, enemy.ClientData.RoleID, burst, injure, enemyLife, 0, nMerlinInjure, eMerlinType); // 加上梅林伤害与类型 [XSea 2015/6/26]
                }

                //攻击就取消隐身
                if (!dontEffectDSHide)
                {
                    if (client.ClientData.DSHideStart > 0)
                    {
                        Global.RemoveBufferData(client, (int)BufferItemTypes.DSTimeHideNoShow);
                        client.ClientData.DSHideStart = 0;
                        GameManager.ClientMgr.NotifyDSHideCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                    }
                }

                //受到其他角色攻击就取消隐身
                if (enemy.ClientData.DSHideStart > 0)
                {
                    Global.RemoveBufferData(enemy, (int)BufferItemTypes.DSTimeHideNoShow);
                    enemy.ClientData.DSHideStart = 0;
                    GameManager.ClientMgr.NotifyDSHideCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, enemy);
                }

                //受到其他角色攻击就取消采集状态
                CaiJiLogic.CancelCaiJiState(enemy);

                //判断是否将自己加入对方的仇人黑名单
                if (enemyLife <= 0) //自己杀死了对方
                {
                    // 注释掉 利用接口 [12/27/2013 LiaoWei]
                    /*GameManager.ClientMgr.AddToEnemyList(Global._TCPManager, Global._TCPManager.tcpClientPool, pool, client, (obj as GameClient).ClientData.RoleID);

                    //处理掉落
                    GameManager.GoodsPackMgr.ProcessRole(sl, pool, client, (obj as GameClient), client.ClientData.RoleName);

                    /// 通知PK值更新(限制当前地图)
                    GameManager.ClientMgr.ChangeRolePKValueAndPKPoint(sl, pool, client, (obj as GameClient));

                    /// 增加大乱斗中杀死的敌人的数量
                    Global.AddBattleKilledNum(client, obj as GameClient, (obj as GameClient).ClientData.Level, (obj as GameClient).ClientData.Level);

                    /// 增加竞技场中杀死的敌人的数量
                    Global.AddArenaBattleKilledNum(client, obj as GameClient);

                    //谁Kill了谁
                    Global.BroadcastXKilledY(client, (obj as GameClient));

                    //写入角色死亡的行为日志
                    Global.AddRoleDeathEvent((obj as GameClient), string.Format("被角色{0}({1})杀死", client.ClientData.RoleID, client.ClientData.RoleName));

                    //记录角色死亡时间
                    (obj as GameClient).ClientData.LastRoleDeadTicks = TimeUtil.NOW();*/

                    Global.ProcessRoleDieForRoleAttack(sl, pool, client, (obj as GameClient));
                }

                //通知紫名信息(限制当前地图)
                GameManager.ClientMgr.ChangeRolePurpleName(sl, pool, client, enemy);

                // 反射伤害处理 [12/27/2013 LiaoWei]
                Global.ProcessDamageThorn(sl, pool, client, enemy, injure);

                if (injure > 0)
                {
                    enemy.passiveSkillModule.OnInjured(enemy);
                }

                if (selfLifeChanged)
                {
                    GameManager.ClientMgr.NotifyOthersLifeChanged(sl, pool, client);
                }

                GameManager.damageMonitor.Out(client);
            }

            return ret;
        }

        /// <summary>
        /// 通知其他人被攻击(群攻)，并且被伤害(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        /*public void NotifyOtherInjuredEx(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, int burst, int injure, List<GameClient> toClients)
        {
            if (null == toClients) return;

            int enemyLife = 0;
            for (int i = 0; i < toClients.Count; i++)
            {
                if (client == toClients[i]) //跳过自己
                {
                    continue;
                }

                if (toClients[i].ClientData.CurrentLifeV <= 0)
                {
                    continue;
                }

                //记录战斗中的敌人
                LogBatterEnemy(client, toClients[i]);

                //将对敌人的伤害进行处理
                injure = InjureToEnemy(sl, pool, toClients[i], injure);

                //处理角色克星
                injure = DBRoleBufferManager.ProcessAntiRole(client, toClients[i], injure);

                toClients[i].ClientData.CurrentLifeV -= (int)injure; //是否需要锁定
                toClients[i].ClientData.CurrentLifeV = Global.GMax(toClients[i].ClientData.CurrentLifeV, 0);
                enemyLife = toClients[i].ClientData.CurrentLifeV;

                //被攻击时减少装备耐久度
                toClients[i].UsingEquipMgr.InjuredSomebody(toClients[i]);

                //判断是否将给敌人的伤害转化成自己的血量增长
                SpriteInjure2Blood(sl, pool, client, injure);

                //GameManager.SystemServerEvents.AddEvent(string.Format("角色减血, roleID={0}({1}), Injure={2}, Life={3}", toClients[i].ClientData.RoleID, toClients[i].ClientData.RoleName, injure, enemyLife), EventLevels.Debug);

                NotifySpriteInjured(sl, pool, client, client.ClientData.MapCode, roleID, toClients[i].ClientData.RoleID, burst, injure, enemyLife, client.ClientData.Level);

                //向自己发送敌人受伤的信息
                if (null != client)
                {
                    NotifySelfEnemyInjured(sl, pool, client, roleID, toClients[i].ClientData.RoleID, burst, injure, enemyLife, 0);
                }

                //攻击就取消隐身
                if (client.ClientData.DSHideStart > 0)
                {
                    Global.RemoveBufferData(client, (int)BufferItemTypes.DSTimeHideNoShow);
                    client.ClientData.DSHideStart = 0;
                    GameManager.ClientMgr.NotifyDSHideCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }

                //受到其他角色攻击就取消隐身
                if (toClients[i].ClientData.DSHideStart > 0)
                {
                    Global.RemoveBufferData(toClients[i], (int)BufferItemTypes.DSTimeHideNoShow);
                    toClients[i].ClientData.DSHideStart = 0;
                    GameManager.ClientMgr.NotifyDSHideCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, toClients[i]);
                }

                //判断是否将自己加入对方的仇人黑名单
                if (enemyLife <= 0) //自己杀死了对方
                {
                    GameManager.ClientMgr.AddToEnemyList(Global._TCPManager, Global._TCPManager.tcpClientPool, pool, client, toClients[i].ClientData.RoleID);

                    //处理掉落
                    GameManager.GoodsPackMgr.ProcessRole(sl, pool, client, toClients[i], client.ClientData.RoleName);

                    /// 通知PK值更新(限制当前地图)
                    GameManager.ClientMgr.ChangeRolePKValueAndPKPoint(sl, pool, client, toClients[i]);

                    /// 增加大乱斗中杀死的敌人的数量
                    Global.AddBattleKilledNum(client, toClients[i] as GameClient, toClients[i].ClientData.Level, toClients[i].ClientData.Level);

                    /// 增加竞技场中杀死的敌人的数量
                    Global.AddArenaBattleKilledNum(client, toClients[i] as GameClient);

                    //谁Kill了谁
                    Global.BroadcastXKilledY(client, toClients[i]);

                    //写入角色死亡的行为日志
                    Global.AddRoleDeathEvent(toClients[i], string.Format("被角色{0}({1})杀死", client.ClientData.RoleID, client.ClientData.RoleName));

                    //记录角色死亡时间
                    toClients[i].ClientData.LastRoleDeadTicks = TimeUtil.NOW();
                }

                //通知紫名信息(限制当前地图)
                GameManager.ClientMgr.ChangeRolePurpleName(sl, pool, client, toClients[i]);
            }
        }*/

        #endregion 角色攻击角色

        #region 怪物攻击角色

        /// <summary>
        /// 通知其他人被攻击(单攻)，并且被伤害(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOtherInjured(SocketListener sl, TCPOutPacketPool pool, Monster monster, GameClient enemy, int burst, int injure, double injurePercnet, int attackType, bool forceBurst, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, int skillLevel, double skillBaseAddPercent, double skillUpAddPercent, bool ignoreDefenseAndDodge = false, double baseRate = 1.0, int addVlue = 0, int nHitFlyDistance = 0)
        {
            Object obj = enemy;
            if (null != obj)
            {
                if (enemy.ClientData.CurrentLifeV > 0) //做次判断，否则会杀死多次
                {
                    //记录战斗中的敌人
                    enemy.ClientData.RoleIDAttackMe = monster.RoleID;

                    // 怪物攻击角色的计算公式
                    if (injure <= 0)
                    {
                        if ((int)AttackType.PHYSICAL_ATTACK == attackType) //物理攻击
                        {
                            RoleAlgorithm.AttackEnemy(monster, (obj as GameClient), false, 1.0, 0, attackPercent, addAttackMin, addAttackMax, out burst, out injure, ignoreDefenseAndDodge, baseRate, addVlue);
                        }
                        else if ((int)AttackType.MAGIC_ATTACK == attackType) //魔法攻击
                        {
                            RoleAlgorithm.MAttackEnemy(monster, (obj as GameClient), false, 1.0, 0, attackPercent, addAttackMin, addAttackMax, out burst, out injure, ignoreDefenseAndDodge, baseRate, addVlue);
                        }
                        else //道术攻击
                        {
                            // 属性改造 去掉 道术攻击[8/15/2013 LiaoWei]
                            //RoleAlgorithm.DSAttackEnemy(monster, (obj as GameClient), false, 1.0, 0, attackPercent, addAttack, out burst, out injure, ignoreDefenseAndDodge);
                        }
                    }

                    //将对敌人的伤害进行处理
                    injure = InjureToEnemy(sl, pool, (obj as GameClient), injure, attackType, ignoreDefenseAndDodge, skillLevel);

                    // 技能中可配置伤害百分比
                    injure = (int)(injure * injurePercnet);

                    /// 被攻击时吸收一部分伤害(护身戒指)
                    //injure -= SpecialEquipMgr.DoSubInJure((obj as GameClient), (int)ItemCategories.FashionWeapon, injure);

                    #region 计算梅林伤害
                    EMerlinSecretAttrType eMerlinType = EMerlinSecretAttrType.EMSAT_None; // 梅林伤害类型
                    // 计算梅林伤害
                    int nElementInjure = GameManager.MerlinInjureMgr.CalcMerlinInjure(monster, enemy, injure, ref eMerlinType);
                    #endregion

                    // 扣血
                    if (!GameManager.TestGamePerformanceMode || !GameManager.TestGamePerformanceLockLifeV)
                    {
                        (obj as GameClient).ClientData.CurrentLifeV -= (int)Global.GMax(0, injure + nElementInjure); //是否需要锁定///////加上梅林伤害 [XSea 2015/6/26]
                    }

                    // 校正
                    (obj as GameClient).ClientData.CurrentLifeV = Global.GMax((obj as GameClient).ClientData.CurrentLifeV, 0);

                    //生命值为0时,立即回复100%生命值
                    //SpecialEquipMgr.DoEquipRestoreBlood((obj as GameClient), (int)ItemCategories.FashionWeapon);

                    int enemyLife = (obj as GameClient).ClientData.CurrentLifeV;

                    //被攻击时减少装备耐久度
                    (obj as GameClient).UsingEquipMgr.InjuredSomebody((obj as GameClient));

                    //为什么不取消隐身状态？

                    //取消被攻击玩家的采集状态
                    CaiJiLogic.CancelCaiJiState(obj as GameClient);

                    //判断是否将给敌人的伤害转化成自己的血量增长

                    if (enemyLife <= 0) //怪物杀死了角色
                    {
                        Global.ProcessRoleDieForMonsterAttack(sl, pool, monster, enemy);
                    }

                    //GameManager.SystemServerEvents.AddEvent(string.Format("角色减血, roleID={0}({1}), Injure={2}, Life={3}", (obj as GameClient).ClientData.RoleID, (obj as GameClient).ClientData.RoleName, injure, enemyLife), EventLevels.Debug);

                    // 处理击飞 怪物击飞玩家 todo...[3/15/2014 LiaoWei]
                    /*Point hitToGrid = new Point(-1, -1);
                    if (nHitFlyDistance > 0)
                    {
                        MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode];

                        int nGridNum = nHitFlyDistance / mapGrid.MapGridWidth;

                        if (nGridNum > 0)
                            hitToGrid = ChuanQiUtils.HitFly(client, enemy, nGridNum);
                    }*/

                    Point hitToGrid = new Point(-1, -1);

                    // 处理击飞 [3/15/2014 LiaoWei]
                    if (nHitFlyDistance > 0)
                    {
                        MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[(obj as GameClient).ClientData.MapCode];

                        int nGridNum = nHitFlyDistance * 100 / mapGrid.MapGridWidth;

                        if (nGridNum > 0)
                            hitToGrid = ChuanQiUtils.HitFly((obj as GameClient), enemy, nGridNum);
                    }

                    if (injure > 0)
                    {
                        enemy.passiveSkillModule.OnInjured(enemy);
                    }
#if ___CC___FUCK___YOU___BB___
                    NotifySpriteInjured(sl, pool, (obj as GameClient), monster.MonsterZoneNode.MapCode, monster.RoleID,
                        (obj as GameClient).ClientData.RoleID, burst, injure, enemyLife, monster.XMonsterInfo.Level,
                        hitToGrid, nElementInjure, eMerlinType); // 加上梅林伤害与类型 [XSea 2015/6/26]
#else
             NotifySpriteInjured(sl, pool, (obj as GameClient), monster.MonsterZoneNode.MapCode, monster.RoleID,
                        (obj as GameClient).ClientData.RoleID, burst, injure, enemyLife, monster.MonsterInfo.VLevel, 
                        hitToGrid, nElementInjure, eMerlinType); // 加上梅林伤害与类型 [XSea 2015/6/26]
#endif


                    // 反射伤害处理 [6/9/2014 LiaoWei]
                    Global.ProcessDamageThorn(sl, pool, monster, (obj as GameClient), injure);
                }
            }
        }

        #endregion 怪物攻击角色

        #region 怪物锁定精灵

        /// <summary>
        /// 供怪使用，自动搜索并锁定
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public void SeekSpriteToLock(Monster monster)
        {
            //if (monster.MonsterInfo.ExtensionID == 5)
            //{
            //    System.Diagnostics.Debug.WriteLine("abc");
            //}

            //HX_SERVER FORTEST
            /* if (monster.MonsterInfo.SeekRange <= 0)
             {
                 monster.VisibleItemList = null;
                 return;
             }

             if (monster.VLife <= 0)
             {
                 monster.VisibleItemList = null;
                 return;
             }

             ////判断是否能够寻敌
             if (!MonsterManager.CanMonsterSeekRange(monster))
             {
                 monster.VisibleItemList = null;
                 return;
             }*/
            //HX_SERVER FORTEST END
            // int viewRange = (monster.MonsterInfo.SeekRange + 1) * (monster.MonsterInfo.SeekRange + 1);
            int viewRange = (2 * (Global.MaxCache9XGridNum - 1) + 1) * (2 * (Global.MaxCache9YGridNum - 1) + 1);
            //int viewRange = (Global.MaxCache9XGridNum *Global.MaxCache9YGridNum);

            Point grid = monster.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            List<Point> searchList = SearchTable.GetSearchTableList();
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[monster.MonsterZoneNode.MapCode];

            monster.VisibleItemList = new List<VisibleItem>();
            for (int i = 0; i < viewRange && i < searchList.Count; i++)
           // for (int i = 0; i < searchList.Count; i++)//HX_SERVER FOR TEST
            {
                int nX = nCurrX + (int)searchList[i].X;
                int nY = nCurrY + (int)searchList[i].Y;

                List<Object> objsList = mapGrid.FindObjects(nX, nY);
                if (null == objsList)
                {
                    continue;
                }

                for (int j = 0; j < objsList.Count; j++)
                {
                    if (null == objsList[j] as IObject)
                    {
                        continue;
                    }

                    if (monster.GetObjectID() == (objsList[j] as IObject).GetObjectID())
                    {
                        continue;
                    }

                    //如果是在副本地图中
                    if (monster.CopyMapID > 0)
                    {
                        if (monster.CopyMapID != (objsList[j] as IObject).CurrentCopyMapID) //副本地图的ID必须相等，否则忽略
                        {
                            continue;
                        }
                    }

                    //判断角色对于怪物是否可见
                    if (objsList[j] is GameClient)
                    {
                        if (!Global.RoleIsVisible((objsList[j] as GameClient)))
                        {
                            continue;
                        }
                    }
                    

                    monster.VisibleItemList.Add(new VisibleItem()
                    {
                        ItemType = (objsList[j] as IObject).ObjectType,
                        ItemID = (objsList[j] as IObject).GetObjectID(),
                    });
                }
            }
        }

        #endregion 怪物锁定精灵

        #region 精灵搜索怪物

        /// <summary>
        /// 供精灵使用，自动搜索怪物
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public Point SeekMonsterPosition(GameClient client, int centerX, int centerY, int radiusGridNum, out int totalMonsterNum)
        {
            totalMonsterNum = 0;
            Point pt = new Point(centerX, centerY);
            List<Object> objsList = GameManager.MonsterMgr.GetObjectsByMap(client.ClientData.MapCode);
            if (null == objsList) return pt;

            int radiusXY = radiusGridNum * 64;

            //totalMonsterNum = objsList.Count;
            int lastDistance = 2147483647;
            Monster monster = null, findMonster = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                if (objsList[i] is Monster)
                {
                    monster = objsList[i] as Monster;

                    //如果怪物已经死亡
                    if (monster.VLife <= 0 || !monster.Alive)
                    {
                        continue;
                    }

                    

                    //如果怪物不是自己可以攻击的，则不返回
                    if (!Global.IsOpposition(client, monster))
                    {
                        continue;
                    }

                    //如果是在副本地图中
                    if (monster.CopyMapID > 0)
                    {
                        if (monster.CopyMapID != client.ClientData.CopyMapID) //副本地图的ID必须相等，否则忽略
                        {
                            continue;
                        }
                    }

                    if (!Global.InCircle(monster.SafeCoordinate, pt, radiusXY))
                    {
                        continue;
                    }

                    totalMonsterNum++;

                    int distance = (int)Global.GetTwoPointDistance(pt, monster.SafeCoordinate);
                    if (distance < lastDistance) //使用此算法查找离自己最近的怪物
                    {
                        lastDistance = distance;
                        findMonster = monster;
                    }
                }
            }

            if (null != findMonster)
            {
                return new Point(findMonster.SafeCoordinate.X, findMonster.SafeCoordinate.Y);
            }

            //return new Point(centerX, centerY);
            return new Point(client.ClientData.PosX, client.ClientData.PosY);
        }

        #endregion 精灵搜索怪物

        #region 查找指定范围内的角色

        /// <summary>
        /// 查找指定圆周范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupEnemiesInCircle(GameClient client, int mapCode, int toX, int toY, int radius, List<int> enemiesList)
        {
            List<Object> objList = new List<object>();
            LookupEnemiesInCircle(client, mapCode, toX, toY, radius, objList);
            for (int i = 0; i < objList.Count; i++)
            {
                enemiesList.Add((objList[i] as GameClient).ClientData.RoleID);
            }
        }

        /// <summary>
        /// 查找指定圆周范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupEnemiesInCircle(GameClient client, int mapCode, int toX, int toY, int radius, List<Object> enemiesList, int nTargetType = -1)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objsList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objsList) return;

            Point center = new Point(toX, toY);
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                if (null != client && client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID) continue;

                //非敌对对象
                if ((null != client && !Global.IsOpposition(client, (objsList[i] as GameClient))) && nTargetType != 2)
                {
                    continue;
                }

                //不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objsList[i] as GameClient).ClientData.CopyMapID)
                {
                    continue;
                }

                Point target = new Point((objsList[i] as GameClient).ClientData.PosX, (objsList[i] as GameClient).ClientData.PosY);
                if (Global.InCircle(target, center, (double)radius))
                {
                    enemiesList.Add(objsList[i]);
                }
            }
        }

        /// <summary>
        /// 查找指定圆周范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupEnemiesInCircle(int mapCode, int copyMapCode, int toX, int toY, int radius, List<Object> enemiesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objsList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objsList) return;

            Point center = new Point(toX, toY);
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                //不在同一个副本
                if (copyMapCode != (objsList[i] as GameClient).ClientData.CopyMapID)
                {
                    continue;
                }

                Point target = new Point((objsList[i] as GameClient).ClientData.PosX, (objsList[i] as GameClient).ClientData.PosY);
                if (Global.InCircle(target, center, (double)radius))
                {
                    enemiesList.Add(objsList[i]);
                }
            }
        }

        /// <summary>
        /// 查找指定半圆范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupEnemiesInCircleByAngle(GameClient client, int direction, int mapCode, int toX, int toY, int radius, List<int> enemiesList, double angle, bool near180)
        {
            List<Object> objList = new List<Object>();

            LookupEnemiesInCircleByAngle(client, direction, mapCode, toX, toY, radius, objList, angle, near180);
            for (int i = 0; i < objList.Count; i++)
            {
                enemiesList.Add((objList[i] as GameClient).ClientData.RoleID);
            }
        }

        /// <summary>
        /// 查找指定半圆范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupEnemiesInCircleByAngle(GameClient client, int direction, int mapCode, int toX, int toY, int radius, List<Object> enemiesList, double angle, bool near180)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objsList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objsList) return;

            double loAngle = 0.0, hiAngle = 0.0;
            Global.GetAngleRangeByDirection(direction, angle, out loAngle, out hiAngle);

            double loAngleNear = 0.0, hiAngleNear = 0.0;
            Global.GetAngleRangeByDirection(direction, 360, out loAngleNear, out hiAngleNear);

            Point center = new Point(toX, toY);
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                if (client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID) continue;

                //非敌对对象
                if (null != client && !Global.IsOpposition(client, (objsList[i] as GameClient)))
                {
                    continue;
                }

                //不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objsList[i] as GameClient).ClientData.CopyMapID)
                {
                    continue;
                }

                Point target = new Point((objsList[i] as GameClient).ClientData.PosX, (objsList[i] as GameClient).ClientData.PosY);
                if (Global.InCircleByAngle(target, center, (double)radius, loAngle, hiAngle))
                {
                    enemiesList.Add((objsList[i]));
                }
                //else if (Global.InCircleByAngle(target, center, (double)200, loAngleNear, hiAngleNear))
                else if (Global.InCircle(target, center, (double)100))
                {
                    enemiesList.Add((objsList[i]));
                }
            }
        }

        /// <summary>
        /// 查找指定半圆范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupEnemiesInCircleByAngle(int direction, int mapCode, int copyMapCode, int toX, int toY, int radius, List<int> enemiesList, double angle, bool near180)
        {
            List<Object> objList = new List<Object>();

            LookupEnemiesInCircleByAngle(direction, mapCode, copyMapCode, toX, toY, radius, objList, angle, near180);
            for (int i = 0; i < objList.Count; i++)
            {
                enemiesList.Add((objList[i] as GameClient).ClientData.RoleID);
            }
        }

        /// <summary>
        /// 查找指定半圆范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupEnemiesInCircleByAngle(int direction, int mapCode, int copyMapCode, int toX, int toY, int radius, List<Object> enemiesList, double angle, bool near180)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objsList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objsList) return;

            double loAngle = 0.0, hiAngle = 0.0;
            Global.GetAngleRangeByDirection(direction, angle, out loAngle, out hiAngle);

            double loAngleNear = 0.0, hiAngleNear = 0.0;
            Global.GetAngleRangeByDirection(direction, 360, out loAngleNear, out hiAngleNear);

            int nAddRadius = 100;
            if (JingJiChangManager.getInstance().IsJingJiChangMap(mapCode))
            {
                nAddRadius = 200;
            }

            Point center = new Point(toX, toY);
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                //不在同一个副本
                if (copyMapCode != (objsList[i] as GameClient).ClientData.CopyMapID)
                {
                    continue;
                }

                Point target = new Point((objsList[i] as GameClient).ClientData.PosX, (objsList[i] as GameClient).ClientData.PosY);
                if (Global.InCircleByAngle(target, center, (double)radius, loAngle, hiAngle))
                {
                    enemiesList.Add((objsList[i]));
                }
                //else if (Global.InCircleByAngle(target, center, (double)200, loAngleNear, hiAngleNear))
                else if (Global.InCircle(target, center, (double)nAddRadius))
                {
                    enemiesList.Add((objsList[i]));
                }
            }
        }

        /// <summary>
        /// 查找指定圆周范围内的玩家
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupRolesInCircle(GameClient client, int mapCode, int toX, int toY, int radius, List<Object> rolesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objsList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objsList) return;

            Point center = new Point(toX, toY);
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                if (null != client && client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID) continue;

                //不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objsList[i] as GameClient).ClientData.CopyMapID)
                {
                    continue;
                }

                Point target = new Point((objsList[i] as GameClient).ClientData.PosX, (objsList[i] as GameClient).ClientData.PosY);
                if (Global.InCircle(target, center, (double)radius))
                {
                    rolesList.Add(objsList[i]);
                }
            }
        }

        // 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
        /// <summary>
        /// 查找指定矩形范围内的玩家
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        //public void LookupRolesInSquare(GameClient client, int mapCode, int toX, int toY, int radius, int nWidth, List<Object> rolesList)
        public void LookupRolesInSquare(GameClient client, int mapCode, int radius, int nWidth, List<Object> rolesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objsList = mapGrid.FindObjects((int)client.ClientData.PosX, (int)client.ClientData.PosY, radius);
            if (null == objsList) return;

            // 源点
            Point source = new Point(client.ClientData.PosX, client.ClientData.PosY);

            Point toPos = Global.GetAPointInCircle(source, radius, client.ClientData.RoleYAngle);

            int toX = (int)toPos.X;
            int toY = (int)toPos.Y;

            // 矩形的中心点
            Point center = new Point();
            center.X = (client.ClientData.PosX + toX) / 2;
            center.Y = (client.ClientData.PosY + toY) / 2;

            // 矩形方向向量
            int fDirectionX = toX - client.ClientData.PosX;
            int fDirectionY = toY - client.ClientData.PosY;
            //Point center = new Point(toX, toY);

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                    continue;

                if ((objsList[i] as GameClient).ClientData.LifeV <= 0)
                    continue;

                if (null != client && client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID)
                    continue;

                // 不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objsList[i] as GameClient).ClientData.CopyMapID)
                    continue;

                Point target = new Point((objsList[i] as GameClient).ClientData.PosX, (objsList[i] as GameClient).ClientData.PosY);

                if (Global.InSquare(center, target, radius, nWidth, fDirectionX, fDirectionY))
                    rolesList.Add(objsList[i]);
                else if (Global.InCircle(target, source, (double)100))  // 补充扫描
                    rolesList.Add((objsList[i]));
            }
        }



        /// <summary>
        /// 查找指定矩形范围内的玩家(矩形扫描)
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupRolesInSquare(int mapCode, int copyMapId, int srcX, int srcY, int toX, int toY, int radius, int nWidth, List<Object> rolesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objsList = mapGrid.FindObjects(srcX, srcY, radius);
            if (null == objsList) return;

            // 源点
            Point source = new Point(srcX, srcY);

            // 矩形的中心点
            Point center = new Point();
            center.X = (srcX + toX) / 2;
            center.Y = (srcY + toY) / 2;

            // 矩形方向向量
            int fDirectionX = toX - srcX;
            int fDirectionY = toY - srcY;
            //Point center = new Point(toX, toY);

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                    continue;

                if ((objsList[i] as GameClient).ClientData.LifeV <= 0)
                    continue;

                // 不在同一个副本
                if (copyMapId != (objsList[i] as GameClient).ClientData.CopyMapID)
                    continue;

                Point target = new Point((objsList[i] as GameClient).ClientData.PosX, (objsList[i] as GameClient).ClientData.PosY);

                if (Global.InSquare(center, target, radius, nWidth, fDirectionX, fDirectionY))
                    rolesList.Add(objsList[i]);
                else if (Global.InCircle(target, source, (double)100))  // 补充扫描
                    rolesList.Add((objsList[i]));
            }
        }

        #endregion 查找指定范围内的角色

        #region 查找指定格子内的角色

        /// <summary>
        /// 查找指定格子内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupEnemiesAtGridXY(IObject attacker, int gridX, int gridY, List<Object> enemiesList)
        {
            int mapCode = attacker.CurrentMapCode;
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];

            List<Object> objsList = mapGrid.FindObjects((int)gridX, (int)gridY);
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                //if (null != client && client.ClientData.RoleID == (objsList[i] as GameClient).ClientData.RoleID)
                //{
                //    continue;
                //}

                //非敌对对象
                //if (null != client && !Global.IsOpposition(client, (objsList[i] as GameClient)))
                //{
                //    continue;
                //}

                //不在同一个副本
                if (null != attacker && attacker.CurrentCopyMapID != (objsList[i] as GameClient).ClientData.CopyMapID)
                {
                    continue;
                }

                enemiesList.Add(objsList[i]);
            }
        }

        /// <summary>
        /// 查找指定格子内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupAttackEnemies(IObject attacker, int direction, List<Object> enemiesList)
        {
            int mapCode = attacker.CurrentMapCode;
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];

            Point grid = attacker.CurrentGrid;
            int gridX = (int)grid.X;
            int gridY = (int)grid.Y;

            Point p = Global.GetGridPointByDirection(direction, gridX, gridY);

            //查找指定格子内的敌人
            LookupEnemiesAtGridXY(attacker, (int)p.X, (int)p.Y, enemiesList);
        }

        /// <summary>
        /// 查找指定格子内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupAttackEnemyIDs(IObject attacker, int direction, List<int> enemiesList)
        {
            List<Object> objList = new List<Object>();
            LookupAttackEnemies(attacker, direction, objList);
            for (int i = 0; i < objList.Count; i++)
            {
                enemiesList.Add((objList[i] as GameClient).ClientData.RoleID);
            }
        }

        /// <summary>
        /// 查找指定给子范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public void LookupRangeAttackEnemies(IObject obj, int toX, int toY, int direction, string rangeMode, List<Object> enemiesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[obj.CurrentMapCode];

            int gridX = toX / mapGrid.MapGridWidth;
            int gridY = toY / mapGrid.MapGridHeight;

            //根据传入的格子坐标和方向返回指定方向的格子列表
            List<Point> gridList = Global.GetGridPointByDirection(direction, gridX, gridY, rangeMode);
            if (gridList.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < gridList.Count; i++)
            {
                //查找指定格子内的敌人
                LookupEnemiesAtGridXY(obj, (int)gridList[i].X, (int)gridList[i].Y, enemiesList);
            }
        }

        #endregion 查找指定格子内的角色

        #region 伤害转化

        /// <summary>
        /// 忽视防御的物理攻击，魔法盾的最高吸收伤害比例
        /// </summary>
        private static double[] IgnoreDefenseAndDogeSubPercent = { 0.05, 0.10, 0.20 };

        /// <summary>
        /// 将对敌人的伤害进行处理
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="injured"></param>
        public int InjureToEnemy(SocketListener sl, TCPOutPacketPool pool, GameClient enemy, int injured, int attackType, bool ignoreDefenseAndDodge, int skillLevel)
        {
            double totalSubValue = 0;

            //是否有防护罩，减伤
            totalSubValue += enemy.RoleMagicHelper.GetSubInjure();

            // MU项目 防护罩
            totalSubValue += enemy.RoleMagicHelper.MU_GetSubInjure2();

            //结算百分比减伤和固定值减伤
            injured = (int)(injured - totalSubValue); //这里要求先固定值后百分比
            injured = (int)(injured * (1 - enemy.RoleMagicHelper.GetSubInjure1()) * (1 - enemy.RoleMagicHelper.MU_GetSubInjure1()));
            if (injured <= 0)
            {
                return 0;
            }

            //是否可以用魔法消耗来代替伤害
            double percent = enemy.RoleMagicHelper.GetInjure2Magic();
            if (percent > 0.0)
            {
                double magicV = percent * injured;
                magicV = Global.GMin(magicV, injured);
                magicV = Global.GMin(enemy.ClientData.CurrentMagicV, magicV);

                injured -= (int)magicV;
                //injured = Global.GMax(1, injured);

                //通知减少魔量
                SubSpriteMagicV(sl, pool, enemy, magicV);
            }

            //是否可以用魔法消耗来代替伤害
            double injured2Magic = enemy.RoleMagicHelper.GetNewInjure2Magic();
            if (injured2Magic > 0.0)
            {
                injured2Magic = Global.GMin(injured2Magic, injured);
                injured2Magic = Global.GMin(enemy.ClientData.CurrentMagicV, injured2Magic);

                injured -= (int)injured2Magic;
                //injured = Global.GMax(1, injured);

                //通知减少魔量
                SubSpriteMagicV(sl, pool, enemy, injured2Magic);
            }

            //是否可以用魔法消耗来代替伤害3
            percent = enemy.RoleMagicHelper.GetNewInjure2Magic3();
            if (percent > 0.0)
            {
                double magicV = percent * injured;
                magicV = Global.GMin(magicV, injured);
                magicV = Global.GMin(enemy.ClientData.CurrentMagicV, magicV);

                injured -= (int)magicV;
                //injured = Global.GMax(1, injured);

                //通知减少魔量
                SubSpriteMagicV(sl, pool, enemy, magicV);
            }


            //是否可以用魔法消耗来代替伤害
            percent = enemy.RoleMagicHelper.GetNewMagicSubInjure();
            if (percent > 0.0)
            {
                if (0 == attackType) //物理攻击
                {
                    if (ignoreDefenseAndDodge) //忽视防御
                    {
                        skillLevel = Math.Min(skillLevel, IgnoreDefenseAndDogeSubPercent.Length - 1);
                        skillLevel = Math.Max(0, skillLevel);
                        percent = Math.Min(percent, IgnoreDefenseAndDogeSubPercent[skillLevel]);
                    }
                }

                double magicV = percent * injured;
                magicV = Global.GMin(magicV, injured);
                magicV = Global.GMin(enemy.ClientData.CurrentMagicV, magicV);

                injured -= (int)magicV;
                //injured = Global.GMax(1, injured);

                //通知减少魔量
                //SubSpriteMagicV(sl, pool, enemy, magicV);
            }

            injured = DBRoleBufferManager.ProcessHuZhaoSubLifeV(enemy, Math.Max(0, injured));
            injured = DBRoleBufferManager.ProcessWuDiHuZhaoNoInjured(enemy, Math.Max(0, injured));

            return Math.Max(0, injured);
        }

        /// <summary>
        /// 将对敌人的伤害转化成为自己的血量
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="injured"></param>
        public void SpriteInjure2Blood(SocketListener sl, TCPOutPacketPool pool, GameClient client, int injured)
        {
            double percent = client.RoleMagicHelper.GetInjure2Life();
            if (0.0 >= percent) return;

            injured = (int)(injured * percent);
            AddSpriteLifeV(sl, pool, client, injured, "击中恢复");
        }

        #endregion 伤害转化

        #region 地图和位置切换

        /// <summary>
        /// 通知切换地图
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="toMapCode"></param>
        /// <param name="toMapX"></param>
        /// <param name="toMapY"></param>
        /// <param name="toMapDirection"></param>
        /// <param name="nID"></param>
        /// <returns></returns>
        public bool NotifyChangeMap(SocketListener sl, TCPOutPacketPool pool, GameClient client, int toMapCode, int maxX = -1, int mapY = -1, int direction = -1, int relife = 0)
        {
            if (client.CheckCheatData.GmGotoShadowMapCode != toMapCode)
            {
                if (client.ClientSocket.IsKuaFuLogin && client.ClientData.KuaFuChangeMapCode != toMapCode)
                {
                    //目前,跨服状态切换地图,强制切换回原服务器
                    KuaFuManager.getInstance().GotoLastMap(client);
                    return true;
                }

                if (client.ClientSocket.IsKuaFuLogin != KuaFuManager.getInstance().IsKuaFuMap(toMapCode))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("GotoMap denied, mapCode={0},IsKuaFuLogin={1}", toMapCode, client.ClientSocket.IsKuaFuLogin));
                    return false;
                }
            }

            client.ClientData.WaitingNotifyChangeMap = true;
            client.ClientData.WaitingChangeMapToMapCode = toMapCode;
            client.ClientData.WaitingChangeMapToPosX = maxX;
            client.ClientData.WaitingChangeMapToPosY = mapY;

            if ("1" == GameManager.GameConfigMgr.GetGameConfigItemStr("log-changmap", "0"))
            {
                if (client.ClientData.LastNotifyChangeMapTicks >= TimeUtil.NOW() - 12000)
                {
                    try
                    {
                        DataHelper.WriteStackTraceLog(string.Format(Global.GetLang("地图传送频繁,记录堆栈信息备查 role={3}({4}) toMapCode={0} pt=({1},{2})"),
                            toMapCode, maxX, mapY, client.ClientData.RoleName, client.ClientData.RoleID));
                    }
                    catch (Exception) { }
                }
            }
            client.ClientData.LastNotifyChangeMapTicks = TimeUtil.NOW();

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", client.ClientData.RoleID, toMapCode, maxX, mapY, direction, relife);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYCHGMAP);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }

            return true;
        }

        /// <summary>
        /// 切换地图
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="toMapCode"></param>
        /// <param name="toMapX"></param>
        /// <param name="toMapY"></param>
        /// <param name="toMapDirection"></param>
        /// <param name="nID"></param>
        /// <returns></returns>
        public bool ChangeMap(SocketListener sl, TCPOutPacketPool pool, GameClient client, int teleport, int toMapCode, int toMapX, int toMapY, int toMapDirection, int nID)
        {
            if ("1" == GameManager.GameConfigMgr.GetGameConfigItemStr("log-changmap", "0"))
            {
                if (client.ClientData.LastChangeMapTicks >= TimeUtil.NOW() - 12000)
                {
                    try
                    {
                        DataHelper.WriteStackTraceLog(string.Format(Global.GetLang("地图传送频繁,记录堆栈信息备查 role={3}({4}) toMapCode={0} pt=({1},{2})"),
                            toMapCode, toMapX, toMapY, client.ClientData.RoleName, client.ClientData.RoleID));
                    }
                    catch (Exception) { }
                }
            }
            client.ClientData.LastChangeMapTicks = TimeUtil.NOW();

           

            //验证地图编号有效性和目标坐标是否可行走
            if (toMapCode > 0)
            {
                GameMap gameMap = GameManager.MapMgr.GetGameMap(toMapCode);
                if (null != gameMap)
                {
                    //如果不可行走,置为-1,后续处理中会设置为出生点坐标
                    if (!gameMap.CanMove(toMapX / gameMap.MapGridWidth, toMapY / gameMap.MapGridHeight))
                    {
                        toMapX = -1;
                        toMapY = -1;
                    }
                }
                else
                {
                    //地图编号无效,置为-1
                    toMapCode = -1;
                }
            }

            //如果传送点大于等于0
            if (teleport >= 0)
            {
                //镖车切换地图
                Global.HandleBiaoCheChangMap(client, toMapCode, toMapX, toMapY, toMapDirection);
            }

            //通知自己所在的地图，其他的所有用户，自己离开了
            List<Object> objsList = Global.GetAll9Clients(client);
            GameManager.ClientMgr.NotifyOthersLeave(sl, pool, client, objsList);

            
            //先删除客户端(此处顺带删除了在地图方块格子中的存在)
            //GameManager.ClientMgr.RemoveClient(client);

            //从格子的定位队列中删除
            //GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode].RemoveObject(client);

            // 只移除在格子中的存在 ChenXiaojun
            GameManager.ClientMgr.RemoveClientFromContainer(client);

            if (toMapX <= 0 || toMapY <= 0)
            {
                int defaultBirthPosX = GameManager.MapMgr.DictMaps[toMapCode].DefaultBirthPosX;
                int defaultBirthPosY = GameManager.MapMgr.DictMaps[toMapCode].DefaultBirthPosY;
                int defaultBirthRadius = GameManager.MapMgr.DictMaps[toMapCode].BirthRadius;

                //是否是皇城
                if (Global.IsHuangChengMapCode(toMapCode))
                {
                    //获取皇城的皇城传送点和复活点
                    Global.GetHuangChengMapPos(client, ref defaultBirthPosX, ref defaultBirthPosY, ref defaultBirthRadius);
                }
                else if (toMapCode == GameManager.BattleMgr.BattleMapCode) //如果是进入炎黄战场场，则选择复活点--->如果是角斗场，采用默认复活点
                {
                    //获取大乱斗的复活点
                    Global.GetLastBattleSideInfo(client); //先设置阵营信息
                    Global.GetBattleMapPos(client, ref defaultBirthPosX, ref defaultBirthPosY, ref defaultBirthRadius);
                }

                //从配置根据地图取默认位置
                Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, toMapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
                toMapX = (int)newPos.X;
                toMapY = (int)newPos.Y;
            }
           

            //记忆旧的地图编号和坐标位置
            //万魔塔不作记忆
            if (!WanMotaCopySceneManager.IsWanMoTaMapCode(client.ClientData.MapCode))
            {
                client.ClientData.LastMapCode = client.ClientData.MapCode;
                client.ClientData.LastPosX = client.ClientData.PosX;
                client.ClientData.LastPosY = client.ClientData.PosY;
            }

            client.ClientData.WaitingForChangeMap = true;

            int oldMapCode = client.ClientData.MapCode;
            //此处不用做互斥，因为已经将客户端从队列中拿出了, 客户端切换地图时一定要启用阻塞的操作，防止用户再操作
            client.ClientData.MapCode = toMapCode;
            client.ClientData.PosX = toMapX;
            client.ClientData.PosY = toMapY;
            client.ClientData.ReportPosTicks = 0;

            client.ClientData.CurrentAction = (int)GActions.Stand;
            //client.ClientData.CurrentGridX = -1;
            //client.ClientData.CurrentGridY = -1;
            //client.ClientData.CurrentObjsDict = null;

            /// 清空角色的可见列表
            client.ClearVisibleObjects(true);

            client.ClientData.DestPoint = new Point(-1, -1);

            if (toMapDirection > 0)
            {
                client.ClientData.RoleDirection = toMapDirection;
            }
            else
            {
                toMapDirection = client.ClientData.RoleDirection;
            }
            

            //添加到队列中
            //GameManager.ClientMgr.AddClient(client);

            // 只添加到地图容器中 ChenXiaojun
            GameManager.ClientMgr.AddClientToContainer(client);

            //处理限时副本的通知信息
            Global.ProcessLimitFuBenMapNotifyMsg(client);

            //清空区域变化
            client.ClearChangeGrid();

            Global.AddMapEvent(client);

            Global.RecordClientPosition(client);
            client.CheckCheatData.LastNotifyLeaveGuMuTick = 0;

            SCMapChange scData = new SCMapChange(client.ClientData.RoleID, teleport, toMapCode, toMapX, toMapY, toMapDirection, 0);
            client.sendProtocolCmd((int)TCPGameServerCmds.CMD_SPR_MAPCHANGE, scData);

            SpriteExchangeMap szSpriteExchangeMap = new SpriteExchangeMap();
            szSpriteExchangeMap.RoleID = client.ClientData.RoleID;
            szSpriteExchangeMap.TeleportID = teleport;
            szSpriteExchangeMap.NewMapCode = toMapCode;
            szSpriteExchangeMap.ToNewMapX = toMapX;
            szSpriteExchangeMap.ToNewMapY = toMapY;
            szSpriteExchangeMap.ToNewDiection = toMapDirection;
            szSpriteExchangeMap.State = 0;
            // SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_LEAVE);
            //群发消息
            SendProtocolToClients<SpriteExchangeMap>(sl, pool, null, objsList, szSpriteExchangeMap, (int)CommandID.CMD_MAP_CHANGE);

            return true;
        }

        /// <summary>
        /// 切换在地图上的位置
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="toMapX"></param>
        /// <param name="toMapY"></param>
        /// <param name="toMapDirection"></param>
        /// <param name="nID"></param>
        /// <returns></returns>
        public bool ChangePosition(SocketListener sl, TCPOutPacketPool pool, GameClient client, int toMapX, int toMapY, int toMapDirection, int nID, int animation = 0)
        {
            if (2 != animation) //如果不是跑步方式才在服务器端改变
            {
                //停止正在移动的故事版
                GameManager.ClientMgr.StopClientStoryboard(client);

                if (toMapX <= 0 || toMapY <= 0)
                {
                    int defaultBirthPosX = GameManager.MapMgr.DictMaps[client.ClientData.MapCode].DefaultBirthPosX;
                    int defaultBirthPosY = GameManager.MapMgr.DictMaps[client.ClientData.MapCode].DefaultBirthPosY;
                    int defaultBirthRadius = GameManager.MapMgr.DictMaps[client.ClientData.MapCode].BirthRadius;

                    //从配置根据地图取默认位置
                    Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, client.ClientData.MapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
                    toMapX = (int)newPos.X;
                    toMapY = (int)newPos.Y;
                }

                //此处不用做互斥，因为已经将客户端从队列中拿出了, 客户端切换地图时一定要启用阻塞的操作，防止用户再操作
                int oldX = client.ClientData.PosX;
                int oldY = client.ClientData.PosY;

                client.ClientData.PosX = toMapX;
                client.ClientData.PosY = toMapY;
                client.ClientData.ReportPosTicks = 0;

                if (toMapDirection > 0)
                {
                    client.ClientData.RoleDirection = toMapDirection;
                }

                //将精灵放入格子
                if (!GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode].MoveObject(-1, -1, client.ClientData.PosX, client.ClientData.PosY, client))
                {
                    client.ClientData.PosX = oldX; //还原
                    client.ClientData.PosY = oldY; //还原
                    client.ClientData.ReportPosTicks = 0;

                    //LogManager.WriteLog(LogTypes.Warning, string.Format("精灵移动超出了地图边界: Cmd={0}, RoleID={1}, 关闭连接", (TCPGameServerCmds)nID, client.ClientData.RoleID));
                    //return false;
                }

                /// 玩家进行了移动
                if (GameManager.Update9GridUsingNewMode <= 0)
                {
                    ClientManager.DoSpriteMapGridMove(client);
                }
                else
                {
                    Global.GameClientMoveGrid(client);
                }

                //Thread.Sleep((int)Global.GetRandomNumber(100, 201)); ///模拟npc对话框窗口不出来的操作

                Global.RecordClientPosition(client);
            }

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return true;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, toMapX, toMapY, toMapDirection, animation);
            SendToClients(sl, pool, null, objsList, strcmd, nID);
            return true;
        }

        /// <summary>
        /// 切换在地图上的位置2
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="toMapX"></param>
        /// <param name="toMapY"></param>
        /// <param name="toMapDirection"></param>
        /// <param name="nID"></param>
        /// <returns></returns>
        public bool ChangePosition2(SocketListener sl, TCPOutPacketPool pool, IObject obj, int roleID, int mapCode, int copyMapID, int toMapX, int toMapY, int toMapDirection, List<Object> objsList)
        {
            int nID = (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS;

            if (null == objsList)
            {
                if (null == obj)
                {
                    objsList = Global.GetAll9Clients2(mapCode, toMapX, toMapY, copyMapID);
                }
                else
                {
                    objsList = Global.GetAll9Clients(obj);
                }
            }

            if (objsList == null) return true;

            string strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, toMapX, toMapY, toMapDirection);
            SendToClients(sl, pool, null, objsList, strcmd, nID);
            return true;
        }

        #endregion 地图和位置切换

        #region 物品管理

        /// <summary>
        /// 添加了新物品通知(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfAddGoods(SocketListener sl, TCPOutPacketPool pool, GameClient client, int id, int goodsID, int forgeLevel, int quality, int goodsNum, int binding, int site, string jewellist, int newHint, string newEndTime,
            int addPropIndex, int bornIndex, int lucky, int strong, int ExcellenceProperty, int nAppendPropLev, int ChangeLifeLevForEquip = 0, int bagIndex = 0, List<int> washProps = null, List<int> elementhrtsProps = null)
        {
            newEndTime = newEndTime.Replace(":", "$");
            //string washPropsStr = Global.ListToString(washProps, '$');

            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}:{13}:{14}:{15}:{16}:{17}:{18}:{19}", client.ClientData.RoleID, id, goodsID, forgeLevel, quality, goodsNum, binding, site, jewellist, newHint, newEndTime, addPropIndex, bornIndex, lucky, strong, ExcellenceProperty, nAppendPropLev, ChangeLifeLevForEquip, bagIndex, washPropsStr);
            //TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_ADD_GOODS);

            AddGoodsData addGoodsData = new AddGoodsData()
            {
                roleID = client.ClientData.RoleID,
                id = id,
                goodsID = goodsID,
                forgeLevel = forgeLevel,
                quality = quality,
                goodsNum = goodsNum,
                binding = binding,
                site = site,
                jewellist = jewellist,
                newHint = newHint,
                newEndTime = newEndTime,
                addPropIndex = addPropIndex,
                bornIndex = bornIndex,
                lucky = lucky,
                strong = strong,
                ExcellenceProperty = ExcellenceProperty,
                nAppendPropLev = nAppendPropLev,
                ChangeLifeLevForEquip = ChangeLifeLevForEquip,
                bagIndex = bagIndex,
                washProps = washProps,
                ElementhrtsProps = elementhrtsProps,
            };

            byte[] bytes = DataHelper.ObjectToBytes<AddGoodsData>(addGoodsData);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_ADD_GOODS);

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 物品修改通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyModGoods(SocketListener sl, TCPOutPacketPool pool, GameClient client, int modType, int id, int isusing, int site, int gcount, int bagIndex, int newHint)
        {
            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", 0, modType, id, isusing, site, gcount, bagIndex, newHint);
            //TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_MOD_GOODS);
            //if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            //{
            //    //
            //    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
            //        tcpOutPacket.PacketCmdID,
            //        tcpOutPacket.PacketDataSize,
            //        client.ClientData.RoleID,
            //        client.ClientData.RoleName));*/
            //}

            SCModGoods scData = new SCModGoods(0, modType, id, isusing, site, gcount, bagIndex, newHint);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
        }

        /// <summary>
        /// 物品移动通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMoveGoods(SocketListener sl, TCPOutPacketPool pool, GameClient client, GoodsData gd, int moveType)
        {
            if (0 == moveType)
            {
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, gd.Id);
                TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_MOVEGOODSDATA);
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
            else
            {
                GameManager.ClientMgr.NotifySelfAddGoods(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    gd.Id, gd.GoodsID, gd.Forge_level, gd.Quality, gd.GCount, gd.Binding, gd.Site, gd.Jewellist, 0, gd.Endtime, gd.AddPropIndex, gd.BornIndex, gd.Lucky, gd.Strong, gd.ExcellenceInfo, gd.AppendPropLev, gd.ChangeLifeLevForEquip, gd.BagIndex, gd.WashProps);
            }
        }

        /// <summary>
        /// 物品信息通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyGoodsInfo(SocketListener sl, TCPOutPacketPool pool, GameClient client, GoodsData gd)
        {
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, gd.Id, gd.Lucky);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYGOODSINFO);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        public TCPProcessCmdResults DestroyGoodsByDBID(TCPManager tcpMgr, TMSKSocket socket, TCPOutPacketPool pool, byte[] data, int nCount)
        {
            GameClient client = GameManager.ClientMgr.FindClient(socket);
            string cmdData = null;
            cmdData = new UTF8Encoding().GetString(data, 0, nCount);
            if (null == cmdData)
            {
                return TCPProcessCmdResults.RESULT_FAILED;
            }
            GoodsData goodsData = Global.GetGoodsByDbID(client, Convert.ToInt32(cmdData));
            Global.DestroyGoods(client, goodsData);
            GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, client, StringUtil.substitute(Global.GetLang("已经摧毁{0}"), Global.GetGoodsNameByID(goodsData.GoodsID)), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
            return TCPProcessCmdResults.RESULT_DATA;
        }

        #endregion 物品管理

        #region 物品消耗

        /// <summary>
        /// 从用户物品中扣除消耗的数量【存在多个数量，仅仅扣除一个】
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        public bool NotifyUseGoods(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int dbID, bool usingGoods, bool dontCalcLimitNum = false)
        {
            //修改内存中物品记录
            GoodsData goodsData = null;
            goodsData = Global.GetGoodsByDbID(client, dbID);
            return NotifyUseGoods(sl, tcpClientPool, pool, client, goodsData, 1, usingGoods, dontCalcLimitNum);
        }

        /// <summary>
        /// 从用户物品中扣除消耗的数量[将dbID对应的物品全部扣除,单个dbid对应的数量为多个也一起扣除]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        public bool NotifyUseGoodsByDbId(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int dbID, int useCount, bool usingGoods, bool dontCalcLimitNum = false)
        {
            //修改内存中物品记录
            GoodsData goodsData = null;
            goodsData = Global.GetGoodsByDbID(client, dbID);

            return NotifyUseGoods(sl, tcpClientPool, pool, client, goodsData, useCount, usingGoods, dontCalcLimitNum);
        }

        /// <summary>
        /// 重载物品消耗接口 从用户物品中扣除消耗的数量[将dbID对应的物品全部扣除,单个dbid对应的数量为多个也一起扣除] [6/9/2014 LiaoWei]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        public bool NotifyUseGoodsByDbId(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int dbID, int useCount, bool usingGoods, out bool usedBinding, out bool usedTimeLimited, bool dontCalcLimitNum = false)
        {
            //修改内存中物品记录
            GoodsData goodsData = null;
            goodsData = Global.GetGoodsByDbID(client, dbID);

            return NotifyUseGoods(sl, tcpClientPool, pool, client, goodsData.GoodsID, useCount, usingGoods, out usedBinding, out usedTimeLimited, dontCalcLimitNum);
        }

        /// <summary>
        /// 重载物品消耗接口 [6/9/2014 LiaoWei]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        public bool NotifyUseGoods(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, GoodsData goodsData, int useCount, bool usingGoods, out bool usedBinding, out bool usedTimeLimited, bool dontCalcLimitNum = false)
        {
            usedBinding = false;
            usedTimeLimited = false;
            bool ret = false;

            lock (client.ClientData.GoodsDataList)
            {
                if (Global.IsGoodsTimeOver(goodsData) || Global.IsGoodsNotReachStartTime(goodsData))
                {
                    return ret; //已经超时无法再使用
                }

                if (!usedBinding)
                {
                    usedBinding = (goodsData.Binding > 0); //判断是否使用了绑定的物品
                }

                if (!usedTimeLimited)
                {
                    usedTimeLimited = Global.IsTimeLimitGoods(goodsData);
                }

                ret = NotifyUseGoods(sl, tcpClientPool, pool, client, goodsData, useCount, usingGoods, dontCalcLimitNum);
            }

            return ret;
        }

        /// <summary>
        /// 从用户物品中扣除消耗的数量,usingGoods参数用于激活使用物品时的相关脚本,如果是日程普通扣除使用，需要设置false
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        public bool NotifyUseGoods(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, GoodsData goodsData, int subNum, bool usingGoods, bool dontCalcLimitNum = false)
        {
            //修改内存中物品记录
            if (null == goodsData)
            {
                //不做处理
                return false;
            }

            //判断物品使用次数限制
            if (!dontCalcLimitNum)
            {
                if (!Global.HasEnoughGoodsDayUseNum(client, goodsData.GoodsID, subNum))
                {
                    return false;
                }
            }

            if (Global.IsGoodsTimeOver(goodsData) || Global.IsGoodsNotReachStartTime(goodsData))
            {
                return false; //已经超时无法再使用
            }

            if (goodsData.GCount <= 0)
            {
                return false; //个数已经为0，无法使用， 防止0个数刷物品后，使用。
            }

            if (goodsData.GCount < subNum)
            {
                return false; //如果要使用的已经小于已经有的，则返回失败
            }

            if (subNum <= 0) //无意义，防止外挂
            {
                return false;
            }

            List<MagicActionItem> magicActionItemList = null;
            int categoriy = 0;
            if (usingGoods)
            {
                // 处理物品的使用功能
                int verifyResult = UsingGoods.ProcessUsingGoodsVerify(client, goodsData.GoodsID, goodsData.Binding, out magicActionItemList, out categoriy);
                if (verifyResult < 0)
                {
                    return false;
                }
                else if (verifyResult == 0)
                {
                    // 升级物品 特殊判断 [8/16/2014 LiaoWei]
                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        if (magicActionItemList[j].MagicActionID == MagicActionIDs.UP_LEVEL)
                        {
                            int nLev = 0;
                            int nAddValue = (int)magicActionItemList[j].MagicActionParams[0];

                            bool bCanUp = true;
                            if (nAddValue > 0)
                            {
                                if (client.ClientData.ChangeLifeCount > GameManager.ChangeLifeMgr.m_MaxChangeLifeCount)
                                {
                                    bCanUp = false;
                                }
                                else if (client.ClientData.ChangeLifeCount == GameManager.ChangeLifeMgr.m_MaxChangeLifeCount)
                                {
                                    ChangeLifeDataInfo infoTmp = null;

                                    infoTmp = GameManager.ChangeLifeMgr.GetChangeLifeDataInfo(client);

                                    if (infoTmp == null)
                                        bCanUp = false;
                                    else
                                    {
                                        nLev = infoTmp.NeedLevel;

                                        if (client.ClientData.Level >= nLev)
                                            bCanUp = false;
                                    }

                                }
                                else
                                {
                                    ChangeLifeDataInfo infoTmp = null;

                                    infoTmp = GameManager.ChangeLifeMgr.GetChangeLifeDataInfo(client, client.ClientData.ChangeLifeCount + 1);

                                    if (infoTmp == null)
                                        bCanUp = false;
                                    else
                                    {
                                        nLev = infoTmp.NeedLevel;

                                        if (client.ClientData.Level >= nLev)
                                            bCanUp = false;
                                    }
                                }

                                if (!bCanUp)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                                    StringUtil.substitute(Global.GetLang("您的等级已达上限")),
                                                                                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelOutOfRange);
                                    return false;
                                }

                                if ((client.ClientData.Level + nAddValue) > nLev)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                                    StringUtil.substitute(Global.GetLang("无法使用该物品")),
                                                                                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelOutOfRange);
                                    return false;
                                }

                                if (client.ClientData.CurrentLifeV <= 0)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                                    StringUtil.substitute(Global.GetLang("死亡状态下无法使用该物品")),
                                                                                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelOutOfRange);
                                    return false;
                                }
                            }
                        }
                        else if (magicActionItemList[j].MagicActionID == MagicActionIDs.ADD_GOODWILL)
                        {
                            if (!MarriageOtherLogic.getInstance().CanAddMarriageGoodWill(client))
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("您无需使用该物品")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                return false;
                            }
                        }
                        else if (magicActionItemList[j].MagicActionID == MagicActionIDs.MU_GETSHIZHUANG)
                        {
                            int fashionID = (int)magicActionItemList[j].MagicActionParams[0];
                            if (!FashionManager.getInstance().FashionCanAdd(client, fashionID))
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                    StringUtil.substitute(Global.GetLang("已拥有该称号")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                return false;
                            }
                        }
                    }
                }
                else if (verifyResult == 1)
                {
                    usingGoods = false;
                }
            }

            int gcount = goodsData.GCount;
            string strcmd = "";

            gcount = goodsData.GCount - subNum;
            TCPOutPacket tcpOutPacket = null;

            //向DBServer请求修改物品
            string[] dbFields = null;
            strcmd = Global.FormatUpdateDBGoodsStr(client.ClientData.RoleID, goodsData.Id, "*", "*", "*", "*", "*", "*", "*", gcount, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*"); // 卓越信息 [12/13/2013 LiaoWei] 装备转生
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strcmd, out dbFields, client.ServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket(new SC_SprUseGoods(-1, goodsData.Id, gcount), pool, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);
                /*
                strcmd = string.Format("{0}:{1}:{2}", -1, goodsData.Id, gcount);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);
                 */
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }

                return false;
            }

            if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket(new SC_SprUseGoods(-2, goodsData.Id, gcount), pool, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);
                /*
                strcmd = string.Format("{0}:{1}:{2}", -2, goodsData.Id, gcount);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);
                 */
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }

                return false;
            }

            //修改内存中物品记录
            if (gcount > 0)
            {
                goodsData.GCount = gcount;
            }
            else if ((int)SaleGoodsConsts.ElementhrtsGoodsID == goodsData.Site || (int)SaleGoodsConsts.UsingElementhrtsGoodsID == goodsData.Site)
            {
                goodsData.GCount = 0;
                ElementhrtsManager.RemoveElementhrtsData(client, goodsData);
            }
            else if ((int)SaleGoodsConsts.FluorescentGemBag == goodsData.Site)
            {
                goodsData.GCount = 0;
                GameManager.FluorescentGemMgr.RemoveFluorescentGemData(client, goodsData);
            }
            else if ((int)SaleGoodsConsts.SoulStoneBag == goodsData.Site)
            {
                goodsData.GCount = 0;
                SoulStoneManager.Instance().RemoveSoulStoneGoods(client, goodsData, goodsData.Site);
            }
            else
            {
                goodsData.GCount = 0;
                Global.RemoveGoodsData(client, goodsData);
            }

            if (usingGoods)
            {
                // 处理物品的使用功能
                UsingGoods.ProcessUsingGoods(client, goodsData.GoodsID, goodsData.Binding, magicActionItemList, categoriy);
            }

            //更新物品使用次数限制
            if (!dontCalcLimitNum)
            {
                Global.AddGoodsLimitNum(client, goodsData.GoodsID, subNum);
            }

            //写入角色物品的得失行为日志(扩展)
            Global.ModRoleGoodsEvent(client, goodsData, -subNum, "物品使用");
            EventLogManager.AddGoodsEvent(client, OpTypes.AddOrSub, OpTags.None, goodsData.GoodsID, goodsData.Id, -subNum, goodsData.GCount, "物品使用");

            // 七日活动
            SevenDayGoalEventObject evObj = SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.UseGoodsCount);
            evObj.Arg1 = goodsData.GoodsID;
            evObj.Arg2 = subNum;
            GlobalEventSource.getInstance().fireEvent(evObj);

            tcpOutPacket = DataHelper.ObjectToTCPOutPacket(new SC_SprUseGoods(0, goodsData.Id, gcount), pool, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);
            /*
            strcmd = string.Format("{0}:{1}:{2}", 0, goodsData.Id, gcount);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);
             */
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }

            // 属性改造 去掉 负重[8/15/2013 LiaoWei]
            //重新计算角色负重(单个物品)
            /*if (Global.UpdateGoodsWeight(client, goodsData, subNum, false))
            {
                //重量属性更新通知
                GameManager.ClientMgr.NotifyUpdateWeights(sl, pool, client);
            }*/

            return true;
        }

        /// <summary>
        /// 从用户物品中扣除消耗的数量
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        /// <param name="totalNum"></param>
        /// <returns></returns>
        public bool NotifyUseGoods(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int goodsID, int totalNum, bool usingGoods, out bool usedBinding, out bool usedTimeLimited, bool dontCalcLimitNum = false)
        {
            usedBinding = false;
            usedTimeLimited = false;
            bool ret = false;
            int count = 0;

            lock (client.ClientData.GoodsDataList)
            {
                for (int i = 0; i < client.ClientData.GoodsDataList.Count; i++)
                {
                    if (client.ClientData.GoodsDataList[i].GoodsID == goodsID)
                    {
                        if (Global.IsGoodsTimeOver(client.ClientData.GoodsDataList[i]) || Global.IsGoodsNotReachStartTime(client.ClientData.GoodsDataList[i]))
                        {
                            continue; //已经超时无法再使用
                        }

                        if (!usedBinding)
                        {
                            usedBinding = (client.ClientData.GoodsDataList[i].Binding > 0); //判断是否使用了绑定的物品
                        }

                        if (!usedTimeLimited)
                        {
                            usedTimeLimited = Global.IsTimeLimitGoods(client.ClientData.GoodsDataList[i]);
                        }

                        int gcount = client.ClientData.GoodsDataList[i].GCount;
                        int subNum = Global.GMin(gcount, totalNum - count);
                        ret = NotifyUseGoods(sl, tcpClientPool, pool, client, client.ClientData.GoodsDataList[i], subNum, usingGoods, dontCalcLimitNum);
                        if (!ret)
                        {
                            break;
                        }

                        count += subNum;
                        if (count >= totalNum)
                        {
                            break;
                        }

                        if (subNum >= gcount)
                        {
                            i--;
                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 从用户绑定物品中扣除消耗的数量 [4/30/2014 LiaoWei]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        /// <param name="totalNum"></param>
        /// <returns></returns>
        public bool NotifyUseBindGoods(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int goodsID, int totalNum, bool usingGoods, out bool usedBinding, out bool usedTimeLimited, bool dontCalcLimitNum = false)
        {
            usedBinding = false;
            usedTimeLimited = false;
            bool ret = false;
            int count = 0;

            lock (client.ClientData.GoodsDataList)
            {
                for (int i = 0; i < client.ClientData.GoodsDataList.Count; i++)
                {
                    if (client.ClientData.GoodsDataList[i].GoodsID == goodsID)
                    {
                        if (Global.IsGoodsTimeOver(client.ClientData.GoodsDataList[i]) || Global.IsGoodsNotReachStartTime(client.ClientData.GoodsDataList[i]))
                        {
                            continue; //已经超时无法再使用
                        }

                        if (client.ClientData.GoodsDataList[i].Binding < 1)
                        {
                            continue;
                        }

                        if (!usedBinding)
                        {
                            usedBinding = (client.ClientData.GoodsDataList[i].Binding > 0); //判断是否使用了绑定的物品
                        }

                        if (!usedTimeLimited)
                        {
                            usedTimeLimited = Global.IsTimeLimitGoods(client.ClientData.GoodsDataList[i]);
                        }

                        int gcount = client.ClientData.GoodsDataList[i].GCount;
                        int subNum = Global.GMin(gcount, totalNum - count);
                        ret = NotifyUseGoods(sl, tcpClientPool, pool, client, client.ClientData.GoodsDataList[i], subNum, usingGoods, dontCalcLimitNum);
                        if (!ret)
                        {
                            break;
                        }

                        count += subNum;
                        if (count >= totalNum)
                        {
                            break;
                        }

                        if (subNum >= gcount)
                        {
                            i--;
                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 从用户非绑定物品中扣除消耗的数量 [4/30/2014 LiaoWei]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        /// <param name="totalNum"></param>
        /// <returns></returns>
        public bool NotifyUseNotBindGoods(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int goodsID, int totalNum, bool usingGoods, out bool usedBinding, out bool usedTimeLimited, bool dontCalcLimitNum = false)
        {
            usedBinding = false;
            usedTimeLimited = false;
            bool ret = false;
            int count = 0;

            lock (client.ClientData.GoodsDataList)
            {
                for (int i = 0; i < client.ClientData.GoodsDataList.Count; i++)
                {
                    if (client.ClientData.GoodsDataList[i].GoodsID == goodsID)
                    {
                        if (Global.IsGoodsTimeOver(client.ClientData.GoodsDataList[i]) || Global.IsGoodsNotReachStartTime(client.ClientData.GoodsDataList[i]))
                        {
                            continue; //已经超时无法再使用
                        }

                        if (client.ClientData.GoodsDataList[i].Binding > 0)
                        {
                            continue;
                        }

                        if (!usedBinding)
                        {
                            usedBinding = (client.ClientData.GoodsDataList[i].Binding > 0); //判断是否使用了绑定的物品
                        }

                        if (!usedTimeLimited)
                        {
                            usedTimeLimited = Global.IsTimeLimitGoods(client.ClientData.GoodsDataList[i]);
                        }

                        int gcount = client.ClientData.GoodsDataList[i].GCount;
                        int subNum = Global.GMin(gcount, totalNum - count);
                        ret = NotifyUseGoods(sl, tcpClientPool, pool, client, client.ClientData.GoodsDataList[i], subNum, usingGoods, dontCalcLimitNum);
                        if (!ret)
                        {
                            break;
                        }

                        count += subNum;
                        if (count >= totalNum)
                        {
                            break;
                        }

                        if (subNum >= gcount)
                        {
                            i--;
                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 物品掉落
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsData"></param>
        /// <returns></returns>
        public bool FallRoleGoods(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, GoodsData goodsData)
        {
            //修改内存中物品记录
            if (null == goodsData)
            {
                //不做处理
                return false;
            }

            if (Global.IsGoodsTimeOver(goodsData) || Global.IsGoodsNotReachStartTime(goodsData))
            {
                return false; //已经超时无法再使用
            }

            if (goodsData.GCount <= 0)
            {
                return false; //个数已经为0，无法使用， 防止0个数刷物品后，使用。
            }

            int gcount = goodsData.GCount;
            string strcmd = "";

            int subNum = 1;
            if (Global.GetGoodsDefaultCount(goodsData.GoodsID) > 1)
            {
                subNum = goodsData.GCount;
            }

            gcount = goodsData.GCount - subNum;
            TCPOutPacket tcpOutPacket = null;

            //向DBServer请求修改物品
            string[] dbFields = null;
            strcmd = Global.FormatUpdateDBGoodsStr(client.ClientData.RoleID, goodsData.Id, "*", "*", "*", "*", "*", "*", "*", gcount, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*"); // 卓越信息 [12/13/2013 LiaoWei] 装备转生
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strcmd, out dbFields, client.ServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket(new SC_SprUseGoods(-1, goodsData.Id, gcount), pool, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);
                /*
                strcmd = string.Format("{0}:{1}:{2}", -1, goodsData.Id, gcount);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);*/
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }

                return false;
            }

            if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket(new SC_SprUseGoods(-2, goodsData.Id, gcount), pool, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);
                /*
                strcmd = string.Format("{0}:{1}:{2}", -2, goodsData.Id, gcount);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);*/
                if (!sl.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }

                return false;
            }

            //修改内存中物品记录
            if (gcount > 0)
            {
                goodsData.GCount = gcount;
            }
            else
            {
                goodsData.GCount = 0;
                Global.RemoveGoodsData(client, goodsData);
            }

            //写入角色物品的得失行为日志(扩展)
            Global.ModRoleGoodsEvent(client, goodsData, -subNum, "物品掉落");
            EventLogManager.AddGoodsEvent(client, OpTypes.AddOrSub, OpTags.None, goodsData.GoodsID, goodsData.Id, -subNum, goodsData.GCount, "物品掉落");

            tcpOutPacket = DataHelper.ObjectToTCPOutPacket(new SC_SprUseGoods(0, goodsData.Id, gcount), pool, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);
            /*
            strcmd = string.Format("{0}:{1}:{2}", 0, goodsData.Id, gcount);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_USEGOODS);*/
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }

            // 属性改造 去掉 负重[8/15/2013 LiaoWei]
            //重新计算角色负重(单个物品)
            /*if (Global.UpdateGoodsWeight(client, goodsData, subNum, false, goodsData.Using > 0))
            {
                //重量属性更新通知
                GameManager.ClientMgr.NotifyUpdateWeights(sl, pool, client);
            }*/

            return true;
        }

        #endregion 物品消耗

        #region 金币处理

        /// <summary>
        /// 钱更新通知(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfMoneyChange(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.Money1, client.ClientData.Money2);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_MONEYCHANGE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 添加游戏金币1
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddMoney1(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int addMoney, string strFrom, bool writeToDB = true)
        {

            int oldMoney = client.ClientData.Money1;


            // 已经超过上限就直接存入仓库里
            if (oldMoney >= Global.Max_Role_Money)
            {
                return AddUserStoreMoney(sl, tcpClientPool, pool, client, addMoney, strFrom);
            }

            if (oldMoney + addMoney > Global.Max_Role_YinLiang)
            {
                long newValue = Global.GMax(0, oldMoney + addMoney - Global.Max_Role_YinLiang);
                // 超过上限的部分就直接存入仓库里
                addMoney = Global.GMax(0, Global.Max_Role_Money - oldMoney);
                AddUserStoreMoney(sl, tcpClientPool, pool, client, newValue, strFrom);
            }

            if (0 == addMoney)
            {
                return true;
            }

            if (writeToDB)
            {
                //先DBServer请求扣费
                //string[] dbFields = null;
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.Money1 + addMoney);
                //TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEMONEY1_CMD, strcmd, out dbFields);
                //if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                //{
                //    return false;
                //}

                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEMONEY1_CMD,
                    strcmd,
                    null, client.ServerId);

                long nowTicks = TimeUtil.NOW();
                Global.SetLastDBCmdTicks(client, (int)TCPGameServerCmds.CMD_DB_UPDATEMONEY1_CMD, nowTicks);
            }

            client.ClientData.Money1 = client.ClientData.Money1 + addMoney; //加钱

            // 钱更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfMoneyChange(sl, pool, client);
            if (0 != addMoney)
            {
                GameManager.logDBCmdMgr.AddDBLogInfo(-1, "绑金", strFrom, "系统", client.ClientData.RoleName, "增加", addMoney, client.ClientData.ZoneID, client.strUserID, client.ClientData.Money1, client.ServerId);
                EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.Use, MoneyTypes.TongQian, addMoney, client.ClientData.Money1, strFrom);
            }

            GameManager.SystemServerEvents.AddEvent(string.Format("角色添加金钱, roleID={0}({1}), Money={2}, addMoney={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Money1, addMoney), EventLevels.Record);
            return true;
        }

        /// <summary>
        /// 添加游戏金币1
        /// </summary>
        /// <param name="client"></param>
        /// <param name="addMoney"></param>
        /// <param name="strFrom"></param>
        /// <param name="writeToDB"></param>
        /// <returns></returns>
        public bool AddMoney1(GameClient client, int addMoney, string strFrom, bool writeToDB = true)
        {
            return AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, addMoney, strFrom, writeToDB);
        }

        /// <summary>
        /// 扣除游戏金币1
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool SubMoney1(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int subMoney, string strFrom)
        {
            if (client.ClientData.Money1 - subMoney < 0)
            {
                subMoney = client.ClientData.Money1;
            }

            //先DBServer请求扣费
            //string[] dbFields = null;
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.Money1 - subMoney);
            //TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEMONEY1_CMD, strcmd, out dbFields);
            //if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            //{
            //    return false;
            //}
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEMONEY1_CMD,
                strcmd,
                null, client.ServerId);

            long nowTicks = TimeUtil.NOW();
            Global.SetLastDBCmdTicks(client, (int)TCPGameServerCmds.CMD_DB_UPDATEMONEY1_CMD, nowTicks);

            // 先锁定
            client.ClientData.Money1 = client.ClientData.Money1 - subMoney; //扣费

            // 钱更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfMoneyChange(sl, pool, client);
            if (0 != subMoney)
            {
                GameManager.logDBCmdMgr.AddDBLogInfo(-1, "绑金", strFrom, client.ClientData.RoleName, "系统", "减少", subMoney, client.ClientData.ZoneID, client.strUserID, client.ClientData.Money1, client.ServerId);
            }
            GameManager.SystemServerEvents.AddEvent(string.Format("角色扣除金钱, roleID={0}({1}), Money={2}, subMoney={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Money1, subMoney), EventLevels.Record);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.Use, MoneyTypes.TongQian, -subMoney, client.ClientData.Money1, strFrom);

            return true;
        }

        #endregion 金币处理

        #region 元宝处理

        /// <summary>
        /// 点卷更新通知(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfUserMoneyChange(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.UserMoney);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_USERMONEYCHANGE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知用户更新钻石
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfUserMoneyChange(GameClient client)
        {
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_USERMONEYCHANGE, string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.UserMoney));
        }

        /// <summary>
        /// 添加用户点卷
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddUserMoney(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int addMoney, string msg, ActivityTypes result = ActivityTypes.None, string param = "")
        {
            //先锁定
            lock (client.ClientData.UserMoneyMutex)
            {
                //先DBServer请求扣费
                //只发增量
                string strcmd = string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, addMoney, (int)result, param);// 发放钻石的活动类型
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERMONEY_CMD, strcmd, client.ServerId);
                if (null == dbFields) return false;
                if (dbFields.Length != 3)
                {
                    return false;
                }

                // 先锁定
                if (Convert.ToInt32(dbFields[1]) < 0)
                {
                    return false; //元宝添加失败
                }

                // 先锁定
                client.ClientData.UserMoney = Convert.ToInt32(dbFields[1]);
                int nTotalMoney = Convert.ToInt32(dbFields[2]);

                //尝试更新钻皇等级,登录的时候也要判断，防止用户离线充值，钻皇等级得不到触发更新
                //Global.TryToActivateSpecialZuanHuangLevel(client);

                // 增加Vip经验 Begin[2/20/2014 LiaoWei]
                if (nTotalMoney > 0)
                {
                    Global.ProcessVipLevelUp(client);
                }

                // 增加Vip经验 End[2/20/2014 LiaoWei]

                //添加日志
                Global.AddRoleUserMoneyEvent(client, "+", addMoney, msg);

                // 添加日志到日志数据库
                if (0 != addMoney)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "钻石", msg, "系统", client.ClientData.RoleName, "增加", addMoney, client.ClientData.ZoneID, client.strUserID, client.ClientData.UserMoney, client.ServerId);
                    EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.YuanBao, addMoney, client.ClientData.UserMoney, msg);
                }
            }

            // 钱更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfUserMoneyChange(sl, pool, client);

            return true;
        }

        /// <summary>
        /// 添加离线用户点卷
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddOfflineUserMoney(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int otherRoleID, string roleName, int addMoney, string msg, int zoneid, string userid)
        {
            //先锁定
            {
                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", otherRoleID, addMoney); //只发增量
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERMONEY_CMD, strcmd, GameManager.LocalServerId);
                if (null == dbFields) return false;
                if (dbFields.Length != 3)
                {
                    return false;
                }

                // 先锁定
                if (Convert.ToInt32(dbFields[1]) < 0)
                {
                    return false; //元宝添加失败
                }

                //添加日志
                Global.AddRoleUserMoneyEvent(otherRoleID, "+", addMoney, msg);

                // 添加日志到日志数据库
                if (0 != addMoney)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "钻石", msg, "系统", roleName, "增加", addMoney, zoneid, userid, Convert.ToInt32(dbFields[1]), GameManager.LocalServerId);
                    EventLogManager.AddMoneyEvent(GameManager.ServerId, zoneid, userid, otherRoleID, OpTypes.AddOrSub, OpTags.None, MoneyTypes.YuanBao, addMoney, -1, msg);
                }
            }

            return true;
        }

        /// <summary>
        /// 扣除用户点卷
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool SubUserMoney(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int subMoney, string msg, bool bIsAddVipExp = true, int type = 1, bool isAddFund = true)
        {
            //先锁定
            lock (client.ClientData.UserMoneyMutex)
            {
                subMoney = Math.Abs(subMoney);
                if (client.ClientData.UserMoney < subMoney)
                {
                    return false; //元宝余额不足
                }

                // 记录原始值
                int oldValue = client.ClientData.UserMoney;
                // 优先把GameServer缓存更新掉
                client.ClientData.UserMoney -= subMoney;

                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, -subMoney); //只发减少的量
                string[] dbFields = null;

                try
                {
                    dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERMONEY_CMD, strcmd, client.ServerId);
                }
                catch (Exception ex)
                {
                    DataHelper.WriteExceptionLogEx(ex, string.Format("CMD_DB_UPDATEUSERMONEY_CMD Faild"));

                    // 如果扣钱的时候出现异常，这时不能判断db是否执行了扣费操作
                    // 为了保证不出问题，不恢复原始值，使gs的缓存小于db的缓存
                    // 避免db扣费后，gs的缓存大于db缓存
                    // client.ClientData.UserMoney = oldValue;
                    return false;
                }

                if (null == dbFields) return false;
                if (dbFields.Length != 3)
                {
                    // 扣钱失败恢复原始值
                    client.ClientData.UserMoney = oldValue;
                    return false;
                }

                // 先锁定
                if (Convert.ToInt32(dbFields[1]) < 0)
                {
                    // 扣钱失败恢复原始值
                    client.ClientData.UserMoney = oldValue;
                    return false; //元宝扣除失败，余额不足
                }

                client.ClientData.UserMoney = Convert.ToInt32(dbFields[1]);

                // 钻石消费后，刷新相应的图标状态
                client._IconStateMgr.FlushUsedMoneyconState(client);
                client._IconStateMgr.CheckJieRiActivity(client, false);
                client._IconStateMgr.SendIconStateToClient(client);

                //每笔消费都存盘
                if (bIsAddVipExp)
                {
                    Global.SaveConsumeLog(client, subMoney, type);

                    if (isAddFund)
                        FundManager.FundMoneyCost(client, subMoney);

                    SpecialActivity act = HuodongCachingMgr.GetSpecialActivity();
                    if (act != null)
                    {
                        act.MoneyConst(client, subMoney);
                    }
                }
                // 添加日志
                Global.AddRoleUserMoneyEvent(client, "-", subMoney, msg);

                // 添加日志到日志数据库
                if (0 != subMoney)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "钻石", msg, client.ClientData.RoleName, "系统", "减少", subMoney, client.ClientData.ZoneID, client.strUserID, client.ClientData.UserMoney, client.ServerId);
                    EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.YuanBao, -subMoney, client.ClientData.UserMoney, msg);
                }
            }

            // 钱更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfUserMoneyChange(sl, pool, client);

            return true;
        }

        /// <summary>
        /// 扣除钻石
        /// </summary>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <param name="bIsAddVipExp"></param>
        ///  <param name="type">消费钻石的类型</param>
        /// <returns></returns>
        public bool SubUserMoney(GameClient client, int subMoney, string msg, bool savedb = true, bool bIsAddVipExp = true, int type = 1, bool isAddFund = true)
        {
            //先锁定
            lock (client.ClientData.UserMoneyMutex)
            {
                subMoney = Math.Abs(subMoney);
                if (client.ClientData.UserMoney < subMoney)
                {
                    return false; //元宝余额不足
                }

                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, -subMoney); //只发减少的量
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERMONEY_CMD, strcmd, client.ServerId);
                if (null == dbFields) return false;
                if (dbFields.Length != 3)
                {
                    return false;
                }

                // 先锁定
                if (Convert.ToInt32(dbFields[1]) < 0)
                {
                    return false; //元宝扣除失败，余额不足
                }

                client.ClientData.UserMoney = Convert.ToInt32(dbFields[1]);

                // 钻石消费后，刷新相应的图标状态
                client._IconStateMgr.FlushUsedMoneyconState(client);
                client._IconStateMgr.SendIconStateToClient(client);
                //每笔消费都存盘
                if (savedb)
                {
                    Global.SaveConsumeLog(client, subMoney, type);

                    if (isAddFund)
                        FundManager.FundMoneyCost(client, subMoney);

                    SpecialActivity act = HuodongCachingMgr.GetSpecialActivity();
                    if (act != null)
                    {
                        act.MoneyConst(client, subMoney);
                    }
                }

                //添加日志
                Global.AddRoleUserMoneyEvent(client, "-", subMoney, msg);

                // 添加日志到日志数据库
                if (0 != subMoney)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "钻石", msg, client.ClientData.RoleName, "系统", "减少", subMoney, client.ClientData.ZoneID, client.strUserID, client.ClientData.UserMoney, client.ServerId);
                    EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.YuanBao, -subMoney, client.ClientData.UserMoney, msg);
                }
            }

            // 钱更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfUserMoneyChange(client);

            return true;
        }

        /// <summary>
        /// 扣除用户点卷[有金子，扣金子，没有金子或者金子不足，再扣元宝]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        //public bool SubUserMoney2(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int subMoney, out int subYuanBao, out int subGold)
        //{
        //    //优先扣除金币
        //    //需要扣除的金币
        //    subGold = 0;

        //    //需要扣除的元宝
        //    subYuanBao = 0;

        //    //允许扣除金子，则扣除金子，默认是允许扣除金子
        //    if ("1" == GameManager.GameConfigMgr.GetGameConfigItemStr("allowsubgold", "1"))
        //    {
        //        if (client.ClientData.Gold > 0)
        //        {
        //            subGold = Global.GMin(client.ClientData.Gold, subMoney);
        //        }
        //    }

        //    subYuanBao = subMoney - subGold;

        //    //扣除金币
        //    if (subGold > 0)
        //    {
        //        //先DBServer请求扣费
        //        //扣除用户点卷
        //        if (!GameManager.ClientMgr.SubUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, subGold))
        //        {
        //            return false;
        //        }
        //    }

        //    //扣除元宝
        //    if (subYuanBao > 0)
        //    {
        //        //先DBServer请求扣费
        //        //扣除用户点卷
        //        if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, subYuanBao))
        //        {
        //            return false;
        //        }
        //    }

        //    return true;
        //}

        /// <summary>
        /// 判断总的元宝和金币个数
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        //public int GetCanUseUserMoneyAndGold(GameClient client)
        //{
        //    int currentGold = 0;

        //    //允许扣除金子，则扣除金子，默认是允许扣除金子
        //    if ("1" == GameManager.GameConfigMgr.GetGameConfigItemStr("allowsubgold", "1"))
        //    {
        //        if (client.ClientData.Gold > 0)
        //        {
        //            currentGold = client.ClientData.Gold;
        //        }
        //    }

        //    return client.ClientData.UserMoney + currentGold;
        //}

        /// <summary>
        /// 查询历史充值记录
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public int QueryTotaoChongZhiMoney(GameClient client)
        {
            string userID = GameManager.OnlineUserSession.FindUserID(client.ClientSocket);
            int zoneID = client.ClientData.ZoneID;

            return QueryTotaoChongZhiMoney(userID, zoneID, client.ServerId);
        }

        /// <summary>
        /// 查询历史充值记录
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public int QueryTotaoChongZhiMoney(string userID, int zoneID, int ServerId)
        {
            //先DBServer请求扣费
            string strcmd = string.Format("{0}:{1}", userID, zoneID);
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_QUERYCHONGZHIMONEY, strcmd, ServerId);
            if (null == dbFields) return 0;
            if (dbFields.Length != 1)
            {
                return 0;
            }

            return Global.SafeConvertToInt32(dbFields[0]);
        }

        /// <summary>
        /// 查询今天的充值额
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public int QueryTotaoChongZhiMoneyToday(GameClient client)
        {
            string userID = GameManager.OnlineUserSession.FindUserID(client.ClientSocket);
            int zoneID = client.ClientData.ZoneID;

            //先DBServer请求扣费
            string strcmd = string.Format("{0}:{1}", userID, zoneID);
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_QUERYTODAYCHONGZHIMONEY, strcmd, client.ServerId);
            if (null == dbFields) return 0;
            if (dbFields.Length != 1)
            {
                return 0;
            }

            return Global.SafeConvertToInt32(dbFields[0]);
        }

        /// <summary>
        /// 添加用户点卷(不在线的情况下)
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddUserMoneyOffLine(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int roleID, int addMoney, string msg, int zoneid, string userid)
        {
            //先DBServer请求扣费
            string strcmd = string.Format("{0}:{1}", roleID, addMoney); //只发增量
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERMONEY_CMD, strcmd, GameManager.LocalServerId);
            if (null == dbFields) return false;
            if (dbFields.Length != 3)
            {
                return false;
            }

            // 先锁定
            if (Convert.ToInt32(dbFields[1]) < 0)
            {
                return false; //元宝添加失败
            }

            //添加日志
            Global.AddRoleUserMoneyEvent(roleID, "+", addMoney, msg);

            // 添加日志到日志数据库
            if (0 != addMoney)
            {
                GameManager.logDBCmdMgr.AddDBLogInfo(-1, "钻石", msg, "系统", "" + roleID, "增加", addMoney, zoneid, userid, Convert.ToInt32(dbFields[1]), GameManager.LocalServerId);
                EventLogManager.AddMoneyEvent(GameManager.ServerId, zoneid, userid, roleID, OpTypes.AddOrSub, OpTags.None, MoneyTypes.YuanBao, addMoney, -1, msg);
            }

            return true;
        }

        #endregion 元宝处理

        #region  绑定元宝处理

        /// <summary>
        /// 金币通知(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfUserGoldChange(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.Gold);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_USERGOLDCHANGE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 用户添加绑钻
        /// </summary>
        /// <param name="client"></param>
        /// <param name="addGold"></param>
        /// <returns></returns>
        public bool AddUserGold(GameClient client, int addGold, string strFrom)
        {
            return AddUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, addGold, strFrom);
        }
        /// <summary>
        /// 添加用户绑钻
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddUserGold(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int addGold, string strFrom = "")
        {
            int oldGold = client.ClientData.Gold;

            //先锁定
            lock (client.ClientData.GoldMutex)
            {
                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, addGold); //只发增量
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERGOLD_CMD, strcmd, client.ServerId);
                if (null == dbFields) return false;
                if (dbFields.Length != 2)
                {
                    return false;
                }

                // 先锁定
                if (Convert.ToInt32(dbFields[1]) < 0)
                {
                    return false; //金币添加失败
                }

                // 先锁定
                client.ClientData.Gold = Convert.ToInt32(dbFields[1]);

                // 添加日志到日志数据库
                if (0 != addGold)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "绑定钻石", strFrom, client.ClientData.RoleName, "系统", "增加", addGold, client.ClientData.ZoneID, client.strUserID, client.ClientData.Gold, client.ServerId);
                    EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.BindYuanBao, addGold, client.ClientData.Gold, strFrom);
                }
            }

            // 金币更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfUserGoldChange(sl, pool, client);

            //写入角色金币增加/减少日志
            Global.AddRoleGoldEvent(client, oldGold);

            return true;
        }

        /// <summary>
        /// 添加用户金币[离线添加--->如果突然在线，走在线添加路线]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddUserGoldOffLine(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int roleID, int addGold, string strFrom = "", String strUserID = "")
        {
            GameClient client = GameManager.ClientMgr.FindClient(roleID);
            if (null != client)
            {
                return AddUserGold(sl, tcpClientPool, pool, client, addGold, strFrom);
            }
            else
            {
                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", roleID, addGold); //只发增量
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERGOLD_CMD, strcmd, GameManager.LocalServerId);
                if (null == dbFields) return false;
                if (dbFields.Length != 2)
                {
                    return false;
                }

                // 先锁定
                if (Convert.ToInt32(dbFields[1]) < 0)
                {
                    return false; //金币添加失败
                }

                if (0 != addGold)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "绑定钻石", strFrom, "" + roleID, "系统", "增加", addGold, 0, strUserID, Convert.ToInt32(dbFields[1]), GameManager.LocalServerId);
                    EventLogManager.AddMoneyEvent(GameManager.ServerId, 0, strUserID, roleID, OpTypes.AddOrSub, OpTags.None, MoneyTypes.BindYuanBao, addGold, -1, strFrom);
                }
            }

            return true;
        }

        /// <summary>
        /// 扣除用户绑钻
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool SubUserGold(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int subGold, string msg = "无")
        {
            int oldGold = client.ClientData.Gold;

            //先锁定
            lock (client.ClientData.GoldMutex)
            {
                if (client.ClientData.Gold < subGold)
                {
                    return false; //金币余额不足
                }

                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, -subGold); //只发减少的量
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERGOLD_CMD, strcmd, client.ServerId);
                if (null == dbFields) return false;
                if (dbFields.Length != 2)
                {
                    return false;
                }

                // 先锁定
                if (Convert.ToInt32(dbFields[1]) < 0)
                {
                    return false; //银两扣除失败，余额不足
                }

                client.ClientData.Gold = Convert.ToInt32(dbFields[1]);

                // 添加日志到日志数据库
                if (0 != subGold)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "绑定钻石", msg, client.ClientData.RoleName, "系统", "减少", subGold, client.ClientData.ZoneID, client.strUserID, client.ClientData.Gold, client.ServerId);
                    EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.BindYuanBao, -subGold, client.ClientData.Gold, msg);
                }
            }

            // 金币更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfUserGoldChange(sl, pool, client);

            //写入角色银两增加/减少日志
            Global.AddRoleGoldEvent(client, oldGold);

            return true;
        }

        /// <summary>
        /// 扣除用户绑钻
        /// </summary>
        /// <param name="client"></param>
        /// <param name="subGold"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool SubUserGold(GameClient client, int subGold, string msg = "无")
        {
            return SubUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, subGold, msg);
        }

        #endregion 绑定元宝处理

        #region 银两处理

        /// <summary>
        /// 银两通知(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfUserYinLiangChange(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.YinLiang);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_USERYINLIANGCHANGE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 添加用户银两
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddUserYinLiang(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int addYinLiang, string strFrom)
        {
            int oldYinLiang = client.ClientData.YinLiang;

            //先锁定
            lock (client.ClientData.YinLiangMutex)
            {
                // 已经超过上限就直接存入仓库里
                if (oldYinLiang >= Global.Max_Role_YinLiang)
                {
                    return AddUserStoreYinLiang(sl, tcpClientPool, pool, client, addYinLiang, strFrom);
                }

                if (oldYinLiang + addYinLiang > Global.Max_Role_YinLiang)
                {
                    long newValue = Global.GMax(0, oldYinLiang + addYinLiang - Global.Max_Role_YinLiang);
                    // 超过上限的部分就直接存入仓库里
                    addYinLiang = Global.GMax(0, Global.Max_Role_YinLiang - oldYinLiang);
                    AddUserStoreYinLiang(sl, tcpClientPool, pool, client, newValue, strFrom);
                }

                if (0 == addYinLiang)
                {
                    return true;
                }

                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, addYinLiang); //只发增量
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERYINLIANG_CMD, strcmd, client.ServerId);
                if (null == dbFields) return false;
                if (dbFields.Length != 2)
                {
                    return false;
                }

                // 先锁定
                if (Convert.ToInt32(dbFields[1]) < 0)
                {
                    return false; //银两添加失败
                }

                // 先锁定
                client.ClientData.YinLiang = Convert.ToInt32(dbFields[1]);

                if (0 != addYinLiang)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "金币", strFrom, "系统", client.ClientData.RoleName, "增加", addYinLiang, client.ClientData.ZoneID, client.strUserID, client.ClientData.YinLiang, client.ServerId);
                    EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.YinLiang, addYinLiang, client.ClientData.YinLiang, strFrom);
                }
            }

            //银两增加的时候通知成就系统
            if (addYinLiang > 0)
            {
                ChengJiuManager.OnTongQianIncrease(client);
            }

            // 银两更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfUserYinLiangChange(sl, pool, client);

            //写入角色银两增加/减少日志
            Global.AddRoleYinLiangEvent(client, oldYinLiang);

            return true;
        }

        /// <summary>
        /// 添加用户银两
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="addYinLiang"></param>
        /// <param name="strFrom"></param>
        /// <returns></returns>
        public bool AddUserYinLiang(GameClient client, int addYinLiang, string strFrom)
        {
            return AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, addYinLiang, strFrom);
        }

        /// <summary>
        /// 添加离线用户银两
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddOfflineUserYinLiang(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, string userID, int roleID, string roleName, int addYinLiang, string strFrom, int zoneid)
        {
            //先DBServer请求扣费
            string strcmd = string.Format("{0}:{1}", roleID, addYinLiang); //只发增量
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERYINLIANG_CMD, strcmd, GameManager.LocalServerId);
            if (null == dbFields) return false;
            if (dbFields.Length != 2)
            {
                return false;
            }

            // 先锁定
            if (Convert.ToInt32(dbFields[1]) < 0)
            {
                return false; //银两添加失败
            }

            if (0 != addYinLiang)
            {
                GameManager.logDBCmdMgr.AddDBLogInfo(-1, "金币", strFrom, "系统", "" + roleID, "增加", addYinLiang, zoneid, userID, Convert.ToInt32(dbFields[1]), GameManager.LocalServerId);
            }
            //写入角色银两增加/减少日志
            Global.AddRoleYinLiangEvent2(userID, roleID, roleName, addYinLiang);

            return true;
        }

        /// <summary>
        /// 扣除用户银两
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool SubUserYinLiang(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int subYinLiang, string strFrom)
        {
            int oldYinLiang = client.ClientData.YinLiang;

            //先锁定
            lock (client.ClientData.YinLiangMutex)
            {
                if (client.ClientData.YinLiang < subYinLiang)
                {
                    return false; //银两余额不足
                }

                // 记录旧值
                int oldValue = client.ClientData.YinLiang;
                // 优先修改gs的缓存值
                client.ClientData.YinLiang -= subYinLiang;

                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, -subYinLiang); //只发减少的量
                string[] dbFields = null;

                try
                {
                    dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEUSERYINLIANG_CMD, strcmd, client.ServerId);
                }
                catch (Exception ex)
                {
                    DataHelper.WriteExceptionLogEx(ex, string.Format("CMD_DB_UPDATEUSERYINLIANG_CMD Faild"));
                    // 如果扣钱的时候出现异常，这时不能判断db是否执行了扣费操作
                    // 为了保证不出问题，不恢复原始值，使gs的缓存小于db的缓存
                    // 避免db扣费后，gs的缓存大于db缓存
                    // client.ClientData.YinLiang = oldValue;
                    return false;
                }

                if (null == dbFields) return false;
                if (dbFields.Length != 2)
                {
                    // 失败后回滚原值
                    client.ClientData.YinLiang = oldValue;
                    return false;
                }

                // 先锁定
                if (Convert.ToInt32(dbFields[1]) < 0)
                {
                    // 失败后回滚原值
                    client.ClientData.YinLiang = oldValue;
                    return false; //银两扣除失败，余额不足
                }

                client.ClientData.YinLiang = Convert.ToInt32(dbFields[1]);

                if (0 != subYinLiang)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "金币", strFrom, client.ClientData.RoleName, "系统", "减少", subYinLiang, client.ClientData.ZoneID, client.strUserID, client.ClientData.YinLiang, client.ServerId);
                }
            }

            // 银两更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfUserYinLiangChange(sl, pool, client);

            //写入角色银两增加/减少日志
            Global.AddRoleYinLiangEvent(client, oldYinLiang);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.Use, MoneyTypes.YinLiang, -subYinLiang, client.ClientData.YinLiang, strFrom);

            return true;
        }

        #endregion 银两处理

        #region 角色间物品交换处理

        /// <summary>
        /// 将某指定的物品转移给某个角色
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="gd"></param>
        /// <param name="toClient"></param>
        /// <param name="bAddToTarget">是否添加到目标角色 ChenXiaojun</param>
        /// <returns></returns>
        public bool MoveGoodsDataToOtherRole(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GoodsData gd,
                                             GameClient fromClient, GameClient toClient, bool bAddToTarget = true)
        {
            //先DBServer请求扣费
            string[] dbFields = null;
            string strcmd = string.Format("{0}:{1}:{2}", toClient.ClientData.RoleID, fromClient.ClientData.RoleID, gd.Id);
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_MOVEGOODS_CMD, strcmd, out dbFields, GameManager.LocalServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                return false;
            }

            if (dbFields.Length < 4 || Convert.ToInt32(dbFields[3]) < 0)
            {
                return false;
            }

            //处理获取到商城道具的物品
            //Global.ProcessMallGoods(toClient, gd.GoodsID, gd.GCount);

            //写入角色物品的得失行为日志(扩展)
            Global.AddRoleGoodsEvent(fromClient, gd.Id, gd.GoodsID, gd.GCount, gd.Binding, gd.Quality, gd.Forge_level, gd.Jewellist, gd.Site, gd.Endtime, -gd.GCount, "物品转给别人", gd.AddPropIndex, gd.BornIndex, gd.Lucky, gd.Strong, gd.ExcellenceInfo, gd.AppendPropLev, gd.ChangeLifeLevForEquip);
            EventLogManager.AddGoodsEvent(fromClient, OpTypes.AddOrSub, OpTags.None, gd.GoodsID, gd.Id, -gd.GCount, 0, "物品转给别人");
            GameManager.logDBCmdMgr.AddDBLogInfo(gd.Id, Global.ModifyGoodsLogName(gd), "物品转给别人(在线)", fromClient.ClientData.RoleName, toClient.ClientData.RoleName, "移动", -gd.GCount, fromClient.ClientData.ZoneID, toClient.strUserID, -1, GameManager.LocalServerId, gd);
            if (bAddToTarget)
            {
                string[] dbFields2 = null;
                gd.BagIndex = Global.GetIdleSlotOfBagGoods(toClient); //找到空闲的包裹格子
                strcmd = Global.FormatUpdateDBGoodsStr(toClient.ClientData.RoleID, gd.Id, "*", "*", "*", "*", "*", "*", "*", "*", "*", gd.BagIndex, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*"); // 卓越一击 [12/13/2013 LiaoWei] 装备转生
                Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strcmd, out dbFields2, GameManager.LocalServerId);

                // 先锁定
                Global.AddGoodsData(toClient, gd);

                //写入角色物品的得失行为日志(扩展)
                Global.AddRoleGoodsEvent(toClient, gd.Id, gd.GoodsID, gd.GCount, gd.Binding, gd.Quality, gd.Forge_level, gd.Jewellist, gd.Site, gd.Endtime, gd.GCount, "得到他人物品", gd.AddPropIndex, gd.BornIndex, gd.Lucky, gd.Strong, gd.ExcellenceInfo, gd.AppendPropLev, gd.ChangeLifeLevForEquip);
                EventLogManager.AddGoodsEvent(toClient, OpTypes.AddOrSub, OpTags.None, gd.GoodsID, gd.Id, gd.GCount, gd.GCount, "得到他人物品");
                GameManager.logDBCmdMgr.AddDBLogInfo(gd.Id, Global.ModifyGoodsLogName(gd), "得到他人物品(在线)", fromClient.ClientData.RoleName, toClient.ClientData.RoleName, "移动", gd.GCount, toClient.ClientData.ZoneID, toClient.strUserID, -1, GameManager.LocalServerId, gd);
                // 处理任务
                ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, toClient, -1, -1, gd.GoodsID, TaskTypes.BuySomething);

                GameManager.ClientMgr.NotifySelfAddGoods(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, toClient,
                    gd.Id, gd.GoodsID, gd.Forge_level, gd.Quality, gd.GCount, gd.Binding, gd.Site, gd.Jewellist, 1, gd.Endtime, gd.AddPropIndex, gd.BornIndex, gd.Lucky, gd.Strong, gd.ExcellenceInfo, gd.AppendPropLev, gd.ChangeLifeLevForEquip, gd.BagIndex, gd.WashProps);
            }

            return true;
        }

        /// <summary>
        /// 将某指定的物品转移给某个角色
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="gd"></param>
        /// <param name="toClient"></param>
        /// <param name="bAddToTarget">是否添加到目标角色 ChenXiaojun</param>
        /// <returns></returns>
        public bool MoveGoodsDataToOfflineRole(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GoodsData gd,
                                               string fromUserID, int fromRoleID, string fromRoleName, int fromRoleLevel, string toUserID,
                                               int toRoleID, string toRoleName, int toRoleLevel, bool bAddToTarget, int zoneid)
        {
            //先DBServer请求扣费
            string[] dbFields = null;
            string strcmd = string.Format("{0}:{1}:{2}", toRoleID, fromRoleID, gd.Id);
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_MOVEGOODS_CMD, strcmd, out dbFields, GameManager.LocalServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                LogManager.WriteLog(LogTypes.SQL, string.Format("向DB请求转移物品时失败{0}->{1}", fromRoleName, toRoleName));
                return false;
            }

            if (dbFields.Length < 4 || Convert.ToInt32(dbFields[3]) < 0)
            {
                LogManager.WriteLog(LogTypes.SQL, string.Format("向DB请求转移物品时失败{0}->{1},错误码{2}", fromRoleName, toRoleName, dbFields[3]));
                return false;
            }

            //写入角色物品的得失行为日志(扩展)
            Global.AddRoleGoodsEvent(fromUserID, fromRoleID, fromRoleName, fromRoleLevel, gd.Id, gd.GoodsID, gd.GCount, gd.Binding, gd.Quality, gd.Forge_level, gd.Jewellist, gd.Site, gd.Endtime, -gd.GCount, "物品转给别人", gd.AddPropIndex, gd.BornIndex, gd.Lucky, gd.Strong, gd.ExcellenceInfo, gd.AppendPropLev, gd.ChangeLifeLevForEquip);
            GameManager.logDBCmdMgr.AddDBLogInfo(gd.Id, Global.ModifyGoodsLogName(gd), "物品转给别人(离线)", fromRoleName/*fromUserID*/, toRoleName/*toUserID*/, "移动", -gd.GCount, zoneid, fromUserID, -1, GameManager.LocalServerId, gd);
            if (bAddToTarget)
            {
                //写入角色物品的得失行为日志(扩展)
                Global.AddRoleGoodsEvent(toUserID, toRoleID, toRoleName, toRoleLevel, gd.Id, gd.GoodsID, gd.GCount, gd.Binding, gd.Quality, gd.Forge_level, gd.Jewellist, gd.Site, gd.Endtime, gd.GCount, "得到他人物品", gd.AddPropIndex, gd.BornIndex, gd.Lucky, gd.Strong, gd.ExcellenceInfo, gd.AppendPropLev, gd.ChangeLifeLevForEquip);
                GameManager.logDBCmdMgr.AddDBLogInfo(gd.Id, Global.ModifyGoodsLogName(gd), "得到他人物品(离线)", fromRoleName/*fromUserID*/, toRoleName/*toUserID*/, "移动", gd.GCount, zoneid, toUserID, -1, GameManager.LocalServerId, gd);

                GameClient toClient = GameManager.ClientMgr.FindClient(toRoleID);
                if (null != toClient)
                {
                    string[] dbFields2 = null;
                    gd.BagIndex = Global.GetIdleSlotOfBagGoods(toClient); //找到空闲的包裹格子
                    strcmd = Global.FormatUpdateDBGoodsStr(toRoleID, gd.Id, "*", "*", "*", "*", "*", "*", "*", "*", "*", gd.BagIndex, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*"); // 卓越一击 [12/13/2013 LiaoWei] 装备转生
                    Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strcmd, out dbFields2, GameManager.LocalServerId);

                    Global.AddGoodsData(toClient, gd);

                    // 处理任务
                    ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, toClient, -1, -1, gd.GoodsID, TaskTypes.BuySomething);

                    GameManager.ClientMgr.NotifySelfAddGoods(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, toClient,
                        gd.Id, gd.GoodsID, gd.Forge_level, gd.Quality, gd.GCount, gd.Binding, gd.Site, gd.Jewellist, 1, gd.Endtime, gd.AddPropIndex, gd.BornIndex, gd.Lucky, gd.Strong, gd.ExcellenceInfo, gd.AppendPropLev, gd.ChangeLifeLevForEquip, gd.BagIndex, gd.WashProps);

                }
            }

            return true;
        }

        #endregion 角色间物品交换处理

        #region 摆摊处理

        /// <summary>
        /// 通知请求角色摆摊的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyGoodsStallCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int status, int stallType)
        {
            string strcmd = string.Format("{0}:{1}:{2}", status, client.ClientData.RoleID, stallType);
            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GOODSSTALL);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知请求物品摆摊数据的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyGoodsStallData(SocketListener sl, TCPOutPacketPool pool, GameClient client, StallData sd)
        {
            byte[] bytesData = null;

            lock (sd)
            {
                bytesData = DataHelper.ObjectToBytes<StallData>(sd);
            }

            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = pool.Pop();
            tcpOutPacket.PacketCmdID = (UInt16)TCPGameServerCmds.CMD_SPR_STALLDATA;
            tcpOutPacket.FinalWriteData(bytesData, 0, (int)bytesData.Length);

            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知所有在线用户某个精灵的开始摆摊(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySpriteStartStall(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            if (null == client.ClientData.StallDataItem) return;

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.StallDataItem.StallName);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_STALLNAME);
        }

        #endregion 摆摊处理

        #region 交易市场购买

        /// <summary>
        /// 通知用户某个精灵的购买了某个物品
        /// </summary>
        /// <param name="client"></param>
        public void NotifySpriteMarketBuy(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient otherClient, int result, int buyType, int goodsDbID, int goodsID, int nID = (int)TCPGameServerCmds.CMD_SPR_MARKETBUYGOODS)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", result, buyType, client.ClientData.RoleID, null != otherClient ? otherClient.ClientData.RoleID : -1, null != otherClient ? Global.FormatRoleName(otherClient, otherClient.ClientData.RoleName) : "", goodsDbID, goodsID);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知用户某个精灵的购买了某个物品
        /// </summary>
        /// <param name="client"></param>
        public void NotifySpriteMarketBuy2(SocketListener sl, TCPOutPacketPool pool, GameClient client, int otherRoleID, int result, int buyType, int goodsDbID, int goodsID, int otherRoleZoneID, string otherRoleName, int nID = (int)TCPGameServerCmds.CMD_SPR_MARKETBUYGOODS)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", result, buyType, client.ClientData.RoleID, otherRoleID, Global.FormatRoleName3(otherRoleID, otherRoleName), goodsDbID, goodsID);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知用户某个精灵的交易市场的名称（开放状态）
        /// </summary>
        /// <param name="client"></param>
        public void NotifySpriteMarketName(SocketListener sl, TCPOutPacketPool pool, GameClient client, string marketName, int offlineMarket)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, marketName, offlineMarket);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_OPENMARKET);
        }

        #endregion 交易市场购买

        #region 冷却时间处理

        /// <summary>
        /// 消除冷却时间处理
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public void RemoveCoolDown(SocketListener sl, TCPOutPacketPool pool, GameClient client, int type, int code)
        {
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, type, code);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_REMOVE_COOLDOWN);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 冷却时间处理

        #region 角色内力系统

        /// <summary>
        /// 通知角色自动增长内力的指令信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyUpdateInterPowerCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int hintUser = 1)
        {
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.InterPower, hintUser);
            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_UPDATEINTERPOWER);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 添加角色的内力
        /// </summary>
        /// <param name="client"></param>
        /// <param name="subInterPower"></param>
        /// <returns></returns>
        public bool AddInterPower(GameClient client, int addInterPower, bool enableFilter = false, bool writeToDB = true)
        {
            if (client.ClientData.InterPower >= (int)LingLiConsts.MaxLingLiVal)
            {
                return false;
            }

            if (enableFilter)
            {
                addInterPower = Global.FilterValue(client, addInterPower);
            }

            if (addInterPower <= 0) return false;

            int oldInterPower = client.ClientData.InterPower;
            client.ClientData.InterPower = client.ClientData.InterPower + addInterPower;
            client.ClientData.InterPower = Global.GMin(client.ClientData.InterPower, (int)LingLiConsts.MaxLingLiVal);

            if (client.ClientData.InterPower > oldInterPower) //如果灵力已经满，则不再增加
            {
                if (writeToDB)
                {
                    //异步写数据库，写入当前的剩余战斗时间
                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_INTERPOWER,
                        string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.InterPower),
                        null, client.ServerId);

                    long nowTicks = TimeUtil.NOW();
                    Global.SetLastDBCmdTicks(client, (int)TCPGameServerCmds.CMD_DB_UPDATE_INTERPOWER, nowTicks);
                }

                //通知角色自动增长内力的指令信息
                NotifyUpdateInterPowerCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                //更新角色的日常数据_灵力
                GameManager.ClientMgr.UpdateRoleDailyData_LingLi(client, client.ClientData.InterPower - oldInterPower);
            }

            return true;
        }

        /// <summary>
        /// 扣除角色的内力
        /// </summary>
        /// <param name="client"></param>
        /// <param name="subInterPower"></param>
        /// <returns></returns>
        public bool SubInterPower(GameClient client, int subInterPower)
        {
            if (subInterPower > 0)
            {
                client.ClientData.InterPower = Global.GMax(client.ClientData.InterPower - subInterPower, 0);
                client.ClientData.InterPower = Global.GMin(client.ClientData.InterPower, (int)LingLiConsts.MaxLingLiVal);

                //异步写数据库，写入当前的剩余战斗时间
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_INTERPOWER,
                    string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.InterPower),
                    null, client.ServerId);

                long nowTicks = TimeUtil.NOW();
                Global.SetLastDBCmdTicks(client, (int)TCPGameServerCmds.CMD_DB_UPDATE_INTERPOWER, nowTicks);

                //通知角色自动增长内力的指令信息
                NotifyUpdateInterPowerCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                {
                    //处理灵力储备
                    DBRoleBufferManager.ProcessLingLiVReserve(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }
            }

            return true;
        }

        #endregion 角色内力系统

        #region 角色在线时长

        /// <summary>
        /// 更新角色的在线时长
        /// </summary>
        /// <param name="client"></param>
        /// <param name="addTicks"></param>
        private void UpdateRoleOnlineTimes(GameClient client, long addTicks)
        {
            //对韩国版本做每小时提示一次的处理
            UpdateRoleOnlineTimesForKorea(client, addTicks);

            if (client.ClientData.FirstPlayStart) //如果还未登陆
            {
                return;
            }

            //是否强制断开网络
            if (client.ClientData.ForceShenFenZheng)
            {
                client.ClientData.ForceShenFenZheng = false;
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, StringUtil.substitute(Global.GetLang("您未完善身份信息，已经进入不健康游戏时间，系统将强制您离线休息5个小时，请完善身份信息，并重新登录。")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.ForceShenFenZheng);
                return;
            }

            int oldTotalOnlineHours = (int)(client.ClientData.TotalOnlineSecs / 3600);

            client.ClientData.TotalOnlineSecs += Math.Max(0, (int)(addTicks / 1000));

            // 针对背包开启存住时长 [4/4/2014 LiaoWei]
            if (client.ClientData.BagNum < Global.MaxBagGridNum)
            {
                client.ClientData.OpenGridTime += Math.Max(0, (int)(addTicks / 1000));
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.OpenGridTick, client.ClientData.OpenGridTime, false);
            }

            if (client.ClientData.MyPortableBagData.ExtGridNum < Global.MaxPortableGridNum)
            {
                client.ClientData.OpenPortableGridTime += Math.Max(0, (int)(addTicks / 1000));
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.OpenPortableGridTick, client.ClientData.OpenPortableGridTime, false);
            }

            int newTotalOnlineHours = (int)(client.ClientData.TotalOnlineSecs / 3600);

            if (oldTotalOnlineHours != newTotalOnlineHours)
            {
                //处理角色的在线累计
                HuodongCachingMgr.ProcessKaiFuGiftAward(client);
            }

            int oldAntiAddictionHours = (int)(client.ClientData.AntiAddictionSecs / 3600);

            client.ClientData.AntiAddictionSecs += Math.Max(0, (int)(addTicks / 1000));

            int newAntiAddictionHours = (int)(client.ClientData.AntiAddictionSecs / 3600);


            int monthID = TimeUtil.NowDateTime().Month;
            if (client.ClientData.MyHuodongData.CurMID == monthID.ToString())
            {
                client.ClientData.MyHuodongData.CurMTime += Math.Max(0, (int)(addTicks / 1000));
            }
            else
            {
                client.ClientData.MyHuodongData.OnlineGiftState = 0;
                client.ClientData.MyHuodongData.CurMID = monthID.ToString();
                client.ClientData.MyHuodongData.LastMTime = client.ClientData.MyHuodongData.CurMTime;
                client.ClientData.MyHuodongData.CurMTime = 0;
                client.ClientData.MyHuodongData.CurMTime += Math.Max(0, (int)(addTicks / 1000));
            }

            DailyActiveManager.ProcessOnlineForDailyActive(client);

            // 刷新“每日在线”、“竞技场奖励”图标感叹号状态
            client._IconStateMgr.CheckJingJiChangJiangLi(client);
            client._IconStateMgr.CheckFuMeiRiZaiXian(client);
            client._IconStateMgr.SendIconStateToClient(client);

            //2011-05-31 精简通讯指令，放到logout中处理
            //发送在线时长计算
            //GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEONLINETIME,
            //    string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.TotalOnlineSecs, client.ClientData.AntiAddictionSecs),
            //    null);

            //是否启用了防止沉迷系统设计, 如果没有则退出
            if ("1" != GameManager.GameConfigMgr.GetGameConfigItemStr("anti-addiction", "1"))
            {
                return;
            }

            //腾讯的特殊防止沉迷的逻辑兼容函数
            if (UpdateRoleOnlineTimesForTengXun(client))
            {
                return; //如果是腾讯版本，则立刻返回
            }

            //计算是否是达到了防止沉迷的时间
            int isAdult = GameManager.OnlineUserSession.FindUserAdult(client.ClientSocket);
            if (isAdult > 0) //如果是成人，则退出不处理
            {
                return;
            }

            BulletinMsgData bulletinMsgData = null;

            //1小时提示
            if (oldAntiAddictionHours < 1 && newAntiAddictionHours >= 1)
            {
                bulletinMsgData = new BulletinMsgData()
                {
                    MsgID = "one-hour-hint-addiction",
                    PlayMinutes = -1,
                    ToPlayNum = -1,
                    BulletinText = Global.GetLang("您累计在线时间已满1小时，请您下线休息，做适当身体活动。"),
                    BulletinTicks = TimeUtil.NOW(),
                    playingNum = 0,
                };

                //发出公告(此公告针对个人，服务器端不留存)
                NotifyBulletinMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    bulletinMsgData);
            }

            //2小时提示
            if (oldAntiAddictionHours < 2 && newAntiAddictionHours >= 2)
            {
                bulletinMsgData = new BulletinMsgData()
                {
                    MsgID = "two-hour-hint-addiction",
                    PlayMinutes = -1,
                    ToPlayNum = -1,
                    BulletinText = Global.GetLang("您累计在线时间已满2小时，请您下线休息，做适当身体活动。"),
                    BulletinTicks = TimeUtil.NOW(),
                    playingNum = 0,
                };

                //发出公告(此公告针对个人，服务器端不留存)
                NotifyBulletinMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    bulletinMsgData);
            }

            //判断如果是未成年人，则起用防止沉迷的操作
            //获取防止沉迷时间的类型
            int antiAddictionType = Global.GetAntiAddictionTimeType(client);
            if (antiAddictionType == client.ClientData.AntiAddictionTimeType)
            {
                return;
            }

            //记录防止沉迷的提示类型
            client.ClientData.AntiAddictionTimeType = antiAddictionType;

            if ((int)AntiAddictionTimeTypes.ThreeHours == client.ClientData.AntiAddictionTimeType)
            {
                if ("0" == GameManager.GameConfigMgr.GetGameConfigItemStr("force-add-shenfenzheng", "1"))
                {
                    bulletinMsgData = new BulletinMsgData()
                    {
                        MsgID = "anti-addiction",
                        PlayMinutes = -1,
                        ToPlayNum = -1,
                        BulletinText = Global.GetLang("您已经进入不健康游戏时间，您的游戏收益将降为正常值的50%"),
                        BulletinTicks = TimeUtil.NOW(),
                        playingNum = 0,
                    };

                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, StringUtil.substitute(Global.GetLang("您未完善身份信息，已经进入不健康游戏时间，您的游戏收益将降为正常值的50%, 请完善身份信息，并重新登录。")),
                        GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.AddShenFenZheng);
                }
                else
                {
                    bulletinMsgData = new BulletinMsgData()
                    {
                        MsgID = "anti-addiction",
                        PlayMinutes = -1,
                        ToPlayNum = -1,
                        BulletinText = Global.GetLang("您累计在线时间已满3小时，您已进入不健康游戏时间，为了您的健康，对您已做立即下线操作。直到您的累计下线时间满5小时后，才能恢复正常。"),
                        BulletinTicks = TimeUtil.NOW(),
                        playingNum = 0,
                    };

                    client.ClientData.ForceShenFenZheng = true;
                }

                //发出公告(此公告针对个人，服务器端不留存)
                NotifyBulletinMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    bulletinMsgData);
            }
            else if ((int)AntiAddictionTimeTypes.FiveHoures == client.ClientData.AntiAddictionTimeType)
            {
                bulletinMsgData = new BulletinMsgData()
                {
                    MsgID = "anti-addiction",
                    PlayMinutes = -1,
                    ToPlayNum = -1,
                    BulletinText = Global.GetLang("您已进入不健康游戏时间，为了您的健康，请您立即下线休息。如不下线，您的身体将受到损害，您的收益已降为零，直到您的累计下线时间满5小时后，才能恢复正常"),
                    BulletinTicks = TimeUtil.NOW(),
                    playingNum = 0,
                };

                //发出公告(此公告针对个人，服务器端不留存)
                NotifyBulletinMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    bulletinMsgData);

                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, StringUtil.substitute(Global.GetLang("您已经进入不健康游戏时间，您的收益已降为零, 请完善身份信息，并重新登录")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.AddShenFenZheng);
            }
        }

        /// <summary>
        /// 更新角色的在线时长,针对韩国,每个小时提示一次
        /// </summary>
        /// <param name="client"></param>
        /// <param name="addTicks"></param>
        private void UpdateRoleOnlineTimesForKorea(GameClient client, long addTicks)
        {
            if (client.ClientData.FirstPlayStart) //如果还未登陆
            {
                return;
            }

            //如果不是韩国版本，则返回
            if ("korea" != GameManager.GameConfigMgr.GetGameConfigItemStr("country", ""))
            {
                return;
            }

            int oldThisTimeAntiAddictionHours = (int)(client.ClientData.ThisTimeOnlineSecs / 3600);

            client.ClientData.ThisTimeOnlineSecs += Math.Max(0, (int)(addTicks / 1000));

            int newThisTimeAntiAddictionHours = (int)(client.ClientData.ThisTimeOnlineSecs / 3600);

            BulletinMsgData bulletinMsgData = null;

            //本次在线每一小时都给提示,本次在线的新旧在线小时数不一样，就表示跨小时出现，进行提示
            if (oldThisTimeAntiAddictionHours != newThisTimeAntiAddictionHours)
            {
                bulletinMsgData = new BulletinMsgData()
                {
                    MsgID = "this-time-every-one-hour-hint-addiction",
                    PlayMinutes = -1,
                    ToPlayNum = -1,
                    BulletinText = string.Format(Global.GetLang("玩家玩游戏的时间超过了{0}小时，过度的玩游戏，会影响您正常生活！"), newThisTimeAntiAddictionHours),
                    BulletinTicks = TimeUtil.NOW(),
                    playingNum = 0,
                };

                //发出公告(此公告针对个人，服务器端不留存)
                NotifyBulletinMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    bulletinMsgData);
            }
        }

        /// <summary>
        /// 腾讯的特殊防止沉迷的逻辑兼容函数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="addTicks"></param>
        private bool UpdateRoleOnlineTimesForTengXun(GameClient client)
        {
            //如果不是腾讯版本，则返回
            if ("TengXun" != GameManager.GameConfigMgr.GetGameConfigItemStr("pingtainame", ""))
            {
                return false;
            }

            //还未进入防止沉迷状态
            if (client.ClientData.TengXunFCMRate >= 1.0)
            {
                return true;
            }

            //判断如果是未成年人，则起用防止沉迷的操作
            //获取防止沉迷时间的类型
            int antiAddictionType = Global.GetAntiAddictionTimeType_TengXun(client);
            if (antiAddictionType == client.ClientData.AntiAddictionTimeType)
            {
                return true;
            }

            //记录防止沉迷的提示类型
            client.ClientData.AntiAddictionTimeType = antiAddictionType;

            return true;
        }

        #endregion 角色在线时长

        #region 特效播放

        /// <summary>
        /// 通知其自己，开始播放特效
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfDeco(SocketListener sl, TCPOutPacketPool pool, GameClient client, int decoID, int decoType, int toBody, int toX, int toY, int shakeMap, int toX1, int toY1, int moveTicks, int alphaTicks)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", client.ClientData.RoleID, decoID, decoType, toBody, toX, toY, shakeMap, toX1, toY1, moveTicks, alphaTicks);
            TCPOutPacket tcpOutPacket = null;

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_PLAYDECO);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知其自己和其他人，自己开始播放特效(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersMyDeco(SocketListener sl, TCPOutPacketPool pool, GameClient client, int decoID, int decoType, int toBody, int toX, int toY, int shakeMap, int toX1, int toY1, int moveTicks, int alphaTicks, List<Object> objsList = null)
        {
            if (null == objsList)
            {
                objsList = Global.GetAll9Clients(client);
            }

            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", client.ClientData.RoleID, decoID, decoType, toBody, toX, toY, shakeMap, toX1, toY1, moveTicks, alphaTicks);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_PLAYDECO);
        }

        /// <summary>
        /// 通知其自己和其他人，自己开始播放特效(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersMyDeco(SocketListener sl, TCPOutPacketPool pool, IObject obj, int mapCode, int copyMapID, int decoID, int decoType, int toBody, int toX, int toY, int shakeMap, int toX1, int toY1, int moveTicks, int alphaTicks, List<Object> objsList = null)
        {
            if (null == objsList)
            {
                if (null == obj)
                {
                    objsList = Global.GetAll9Clients2(mapCode, toX, toY, copyMapID);
                }
                else
                {
                    objsList = Global.GetAll9Clients(obj);
                }
            }

            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", -1, decoID, decoType, toBody, toX, toY, shakeMap, toX1, toY1, moveTicks, alphaTicks);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_PLAYDECO);
        }

        #endregion 特效播放

        #region Buffer数据处理

        /// <summary>
        /// 将新的Buffer数据通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBufferData(GameClient client, BufferData bufferData)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<BufferData>(bufferData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_BUFFERDATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 将新的Buffer数据通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOtherBufferData(IObject self, BufferData bufferData)
        {
            OtherBufferData otherBufferData = new OtherBufferData()
            {
                BufferID = bufferData.BufferID,
                BufferVal = bufferData.BufferVal,
                BufferType = bufferData.BufferType,
                BufferSecs = bufferData.BufferSecs,
                StartTime = bufferData.StartTime,
            };

            switch (self.ObjectType)
            {
                case ObjectTypes.OT_MONSTER:
                    otherBufferData.RoleID = (self as Monster).RoleID;
                    break;
                case ObjectTypes.OT_CLIENT:
                    otherBufferData.RoleID = (self as GameClient).ClientData.RoleID;
                    break;
                case ObjectTypes.OT_NPC:
                    otherBufferData.RoleID = (self as NPC).NpcID;
                    break;
                case ObjectTypes.OT_FAKEROLE:
                    otherBufferData.RoleID = (self as FakeRoleItem).FakeRoleID;
                    break;
                default:
                    return;//暂不支持其它类型
            }

            byte[] bytes = DataHelper.ObjectToBytes<OtherBufferData>(otherBufferData);

            List<Object> objsList = Global.GetAll9Clients(self);
            if (null == objsList)
            {
                objsList = new List<object>();
            }

            if (objsList.IndexOf(self) < 0)
            {
                objsList.Add(self);
            }

            foreach (var obj in objsList)
            {
                GameClient c = obj as GameClient;
                if (null != c && c.CodeRevision >= 2)
                {
                    SendToClient(c, bytes, (int)TCPGameServerCmds.CMD_SPR_NOTIFYOTHERBUFFERDATA);
                }
            }
        }

        #endregion Buffer数据处理

        #region 角色收获经验

        /// <summary>
        /// 添加了新经验(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfExperience(SocketListener sl, TCPOutPacketPool pool, GameClient client, long newExperience)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, client.ClientData.Experience, client.ClientData.Level, newExperience, client.ClientData.ChangeLifeCount);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_EXPCHANGE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 处理角色收获经验
        /// </summary>
        /// <param name="client"></param>
        /// <param name="experience"></param>
        public void ProcessRoleExperience(GameClient client, long experience, bool enableFilter = true, bool writeToDB = true, bool checkDead = false, string strFrom = "none")
        {
            // 增加死亡判断 [5/12/2014 LiaoWei]
            if (checkDead && client.ClientData.CurrentLifeV <= 0)
                return;

            if (experience <= 0) return;

            //过滤经验奖励
            if (enableFilter)
            {
                experience = Global.FilterValue(client, experience);
            }

            if (experience > 0)
            {
                EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.Awards, MoneyTypes.Exp, experience, -1, strFrom);

                int oldLevel = client.ClientData.Level;

                //获取敌人的经验值
                Global.EarnExperience(client, experience);

                if (writeToDB || (oldLevel != client.ClientData.Level)) //如果升级了也必须写入数据库
                {
                    // 升级后 根据职业给予玩家属性点 [9/29/2013 LiaoWei]
                    int nOccupation = Global.CalcOriginalOccupationID(client.ClientData.Occupation);

                    // 改成转生时给属性点 [3/6/2014 LiaoWei]
                    //OccupationAddPointInfo tmpOccAddPointInfo = new OccupationAddPointInfo();
                    //tmpOccAddPointInfo = Data.OccupationAddPointInfoList[nOccupation];

                    ChangeLifeAddPointInfo tmpChangeAddPointInfo = null;
                    if (!Data.ChangeLifeAddPointInfoList.TryGetValue(client.ClientData.ChangeLifeCount, out tmpChangeAddPointInfo) || tmpChangeAddPointInfo == null)
                        return;

                    //tmpChangeAddPointInfo = Data.ChangeLifeAddPointInfoList[client.ClientData.ChangeLifeCount];

                    // 奖励属性点
                    lock (client.ClientData.PropPointMutex)
                    {
                        int nOldPoint = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint);
                        int nNewPoint = 0;
                        int nAddLev = client.ClientData.Level - oldLevel;
                        //nNewPoint = nAddLev * tmpOccAddPointInfo.AddPoint + nOldPoint;
                        nNewPoint = nAddLev * tmpChangeAddPointInfo.AddPoint + nOldPoint;
                        client.ClientData.TotalPropPoint = nNewPoint;

                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TotalPropPoint, nNewPoint, true);
                    }

                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_EXPLEVEL,
                        string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.Level, client.ClientData.Experience),
                        null, client.ServerId);

                    long nowTicks = TimeUtil.NOW();
                    Global.SetLastDBCmdTicks(client, (int)TCPGameServerCmds.CMD_DB_UPDATE_EXPLEVEL, nowTicks);
                }

                if (oldLevel != client.ClientData.Level)
                {
                    //如果开启了自动加点选项(新手默认开启)
                    //Global.AutoAddRolePoint(client);

                    //通知客户端属性变化
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                    // 总生命值和魔法值变化通知(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, true, true);

                    //通知组队中的其他队员自己的级别发生了变化
                    GameManager.ClientMgr.NotifyTeamUpLevel(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                    // 新手场景 特殊处理 [2/28/2014 LiaoWei]
                    if (client.ClientData.IsFlashPlayer != 1 && client.ClientData.MapCode != (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)
                    {
                        //自动学习技能
                        Global.AutoLearnSkills(client);

                        //判断技能是否能够自动升级
                        //Global.AutoUpLevelSkills(client);
                    }

                    //跨10的升级  // 注释掉 升级不给提示了 转生给提示 [12/13/2013 LiaoWei]
                    //Global.BroadcastUpLevel(client, oldLevel);

                    //写入角色升级的行为日志
                    Global.AddRoleUpgradeEvent(client, oldLevel);

                    //成就相关处理
                    if (client.ClientData.IsFlashPlayer != 1 && client.ClientData.MapCode != (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)
                        ChengJiuManager.OnRoleLevelUp(client);

                    //处理角色的在线累计
                    HuodongCachingMgr.ProcessKaiFuGiftAward(client);

                    /// 处理角色的升级
                    HuodongCachingMgr.ProcessUpLevelAward4_60Level_100Level(client, oldLevel, client.ClientData.Level);

                    // 处理世界等级
                    WorldLevelManager.getInstance().UpddateWorldLevelBuff(client);

                    // 触发七日活动
                    GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.RoleLevelUp));

                    SpreadManager.getInstance().SpreadIsLevel(client);

                    TradeBlackManager.Instance().UpdateObjectExtData(client);
                }

                //更新角色的日常数据_经验
                GameManager.ClientMgr.UpdateRoleDailyData_Exp(client, experience);

                // 添加了新经验(只通知自己)
                GameManager.ClientMgr.NotifySelfExperience(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, experience);

                //GameManager.SystemServerEvents.AddEvent(string.Format("角色获取经验和级别, roleID={0}({1}), Level={2}, Experience={3}, newExperience={4}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Level, client.ClientData.Experience, experience), EventLevels.Hint);
            }
        }

        /// <summary>
        /// 为在线角色添加经验
        /// </summary>
        /// <param name="addPercent"></param>
        public void AddOnlieRoleExperience(GameClient client, int addPercent)
        {
            long needExperience = 0;
            if (client.ClientData.Level < (Data.LevelUpExperienceList.Length - 1))
            {
                needExperience = Data.LevelUpExperienceList[client.ClientData.Level + 1];
            }

            if (needExperience <= 0)
            {
                return;
            }

            int addExperience = (int)(needExperience * ((double)addPercent / 100.0));

            //处理角色收获经验
            ProcessRoleExperience(client, addExperience, false, false);
        }

        /// <summary>
        /// 为所有在线角色添加经验
        /// </summary>
        /// <param name="addPercent"></param>
        public void AddAllOnlieRoleExperience(int addPercent)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientData.ClosingClientStep > 0)
                {
                    continue;
                }

                //为在线角色添加经验
                AddOnlieRoleExperience(client, addPercent);
            }
        }

        /// <summary>
        /// 获取当前等级升级所需经验 [XSea 2015/4/8]
        /// </summary>
        /// <param name="client">角色</param>
        public long GetCurRoleLvUpNeedExp(GameClient client)
        {
            if (client == null)
                return 0;

            if (client.ClientData.Level >= Data.LevelUpExperienceList.Length - 1)
                return 0;

            long lNeedExp = 0; // 经验

            // 从经验表取该等级需要多少经验升级
            lNeedExp = Data.LevelUpExperienceList[client.ClientData.Level];

            // 如果转生次数大于0
            if (client.ClientData.ChangeLifeCount > 0)
            {
                // 转生信息
                ChangeLifeDataInfo infoTmp = GameManager.ChangeLifeMgr.GetChangeLifeDataInfo(client);

                // 根据转生次数 乘以经验系数
                if (infoTmp != null && infoTmp.ExpProportion > 0)
                    lNeedExp = lNeedExp * infoTmp.ExpProportion;
            }

            return lNeedExp;
        }

        #endregion 角色收获经验

        #region 完成某任务与之前所有任务(仅限对db操作完成，不走任务流程)
        /// <summary>
        /// 完成某任务与之前所有任务(仅限对db操作完成，不走任务流程) [XSea 2015/6/5]
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nDestTaskID">目标任务id</param>
        /// <returns>[结果=0成功,结果!=0失败]</returns>
        public int AutoCompletionTaskByTaskID(TCPManager tcpMgr, TCPClientPool tcpClientPool, TCPOutPacketPool pool, TCPRandKey tcpRandKey, GameClient client, int nDestTaskID)
        {
            if (null == client)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("client不存在，服务器无法完成某任务与之前所有任务"));
                return -1;
            }

            try
            {
                int roleID = client.ClientData.RoleID; // 角色id
                List<int> list = new List<int>(); // 存放任务id(第一位放角色id)

                // 循环将魔剑士初始任务以前的任务标记未已完成
                foreach (var kv in GameManager.SystemTasksMgr.SystemXmlItemDict)
                {
                    SystemXmlItem systemTask = kv.Value;
                    int nTaskID = kv.Key; // 任务id

                    // 任务id超过目标任务id就停止
                    if (nTaskID > nDestTaskID)
                        break;

                    if (nTaskID > 0)
                    {
                        Global.AddOldTask(client, nTaskID); // 依次将已完成的任务加入列表
                        Global.AddRoleTaskEvent(client, nTaskID); // 写入角色完成任务的行为日志
                        ChengJiuManager.ProcessCompleteMainTaskForChengJiu(client, nTaskID);// 完成主线任务成就
                        Global.UpdateTaskZhangJieProp(client, nTaskID); // 更新任务章节完成度 给buff
                        list.Add(nTaskID); // 加入任务id列表
                    }
                }
                list.Sort(); // 排序
                list.Insert(0, roleID); // 第一位放角色id

                byte[] bytesCmd = DataHelper.ObjectToBytes(list);
                TCPOutPacket tcpOutPacket = null;

                // 发送至db
                //return Global.sendToDB<int>((int)TCPGameServerCmds.CMD_DB_ALL_COMPLETION_OF_TASK_BY_TASKID, bytesCmd);

                // 发送至db
                TCPProcessCmdResults result = Global.TransferRequestToDBServer(tcpMgr, client.ClientSocket, tcpClientPool, tcpRandKey, pool,
                    (int)TCPGameServerCmds.CMD_DB_ALL_COMPLETION_OF_TASK_BY_TASKID, bytesCmd, bytesCmd.Length, out tcpOutPacket, client.ServerId);

                if (result == TCPProcessCmdResults.RESULT_DATA && null != tcpOutPacket)
                {
                    string strData = new UTF8Encoding().GetString(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);

                    // 注意--还回缓冲池 不然会造成内存泄漏 内存池将会耗尽！！
                    Global.PushBackTcpOutPacket(tcpOutPacket);

                    // 解析指令
                    string[] fields = strData.Split(':');
                    if (fields.Length != 1)
                        return -1;
                    return int.Parse(fields[0]);
                }

                return -1;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }
            return -1;
        }
        #endregion

        #region 搜索当前地图的用户并返回列表

        /// <summary>
        /// 搜索符合角色名符合字符串的用户并返回列表
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="startIndex"></param>
        public void SearchRolesByStr(GameClient client, string roleName, int startIndex)
        {
            int index = startIndex, addCount = 0;
            GameClient otherClient = null;
            List<SearchRoleData> roleDataList = new List<SearchRoleData>();
            while ((otherClient = GetNextClient(ref index)) != null)
            {
                if (-1 == otherClient.ClientData.RoleName.IndexOf(roleName))
                {
                    continue;
                }

                roleDataList.Add(new SearchRoleData()
                {
                    RoleID = otherClient.ClientData.RoleID,
                    RoleName = Global.FormatRoleName(otherClient, otherClient.ClientData.RoleName),
                    RoleSex = otherClient.ClientData.RoleSex,
                    Level = otherClient.ClientData.Level,
                    Occupation = otherClient.ClientData.Occupation,
                    MapCode = otherClient.ClientData.MapCode,
                    PosX = otherClient.ClientData.PosX,
                    PosY = otherClient.ClientData.PosY,
                    ChangeLifeLev = otherClient.ClientData.ChangeLifeCount,
                });

                addCount++;
                if (addCount >= (int)SearchResultConsts.MaxSearchRolesNum)
                {
                    break;
                }
            }

            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<SearchRoleData>>(roleDataList, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_SEARCHROLES);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 列举用户并返回列表
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="startIndex"></param>
        public void ListMapRoles(GameClient client, int startIndex)
        {
            ListRolesData listRolesData = new ListRolesData()
            {
                StartIndex = startIndex,
                TotalRolesCount = 0,
                PageRolesCount = (int)SearchResultConsts.MaxSearchRolesNum,
                SearchRoleDataList = new List<SearchRoleData>(),
            };

            List<SearchRoleData> roleDataList = listRolesData.SearchRoleDataList;

            List<Object> objsList = GetMapClients(client.ClientData.MapCode);
            objsList = Global.FilterHideObjsList(objsList);
            if (null == objsList || objsList.Count <= 0)
            {
                SendListRolesDataResult(client, listRolesData);
                return;
            }

            List<GameClient> clients = new List<GameClient>();
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                if ((objsList[i] as GameClient).ClientData.TeamID > 0) //已经组队的不再显示
                {
                    continue;
                }

                clients.Add(objsList[i] as GameClient);
            }

            listRolesData.TotalRolesCount = clients.Count;
            if (listRolesData.TotalRolesCount <= 0)
            {
                SendListRolesDataResult(client, listRolesData);
                return;
            }

            if (startIndex >= clients.Count)
            {
                startIndex = 0; //从0开始
            }

            int index = startIndex, addCount = 0;
            GameClient otherClient = null;
            for (int i = 0; i < clients.Count; i++)
            {
                if (i < startIndex)
                {
                    continue;
                }

                otherClient = clients[i];
                roleDataList.Add(new SearchRoleData()
                {
                    RoleID = otherClient.ClientData.RoleID,
                    RoleName = Global.FormatRoleName(otherClient, otherClient.ClientData.RoleName),
                    RoleSex = otherClient.ClientData.RoleSex,
                    Level = otherClient.ClientData.Level,
                    Occupation = otherClient.ClientData.Occupation,
                    MapCode = otherClient.ClientData.MapCode,
                    PosX = otherClient.ClientData.PosX,
                    PosY = otherClient.ClientData.PosY,
                    CombatForce = otherClient.ClientData.CombatForce,
                    ChangeLifeLev = otherClient.ClientData.ChangeLifeCount
                });

                addCount++;
                if (addCount >= (int)SearchResultConsts.MaxSearchRolesNum)
                {
                    break;
                }
            }

            SendListRolesDataResult(client, listRolesData);
        }

        /// <summary>
        /// 发送列列举地图上的角色的数据给客户端
        /// </summary>
        /// <param name="listRolesData"></param>
        private void SendListRolesDataResult(GameClient client, ListRolesData listRolesData)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<ListRolesData>(listRolesData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_LISTROLES);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 搜索当前地图的用户并返回列表

        #region 队伍查询

        /// <summary>
        /// 列举组队的队伍并返回列表
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="startIndex"></param>
        public void ListAllTeams(GameClient client, int startIndex)
        {
            SearchTeamData searchTeamData = new SearchTeamData()
            {
                StartIndex = startIndex,
                TotalTeamsCount = 0,
                PageTeamsCount = (int)SearchResultConsts.MaxSearchTeamsNum,
                TeamDataList = null,
            };

            searchTeamData.TotalTeamsCount = GameManager.TeamMgr.GetTotalDataCount();
            if (searchTeamData.TotalTeamsCount <= 0)
            {
                SendListTeamsDataResult(client, searchTeamData);
                return;
            }

            if (startIndex >= searchTeamData.TotalTeamsCount)
            {
                startIndex = 0; //从0开始
            }

            searchTeamData.TeamDataList = GameManager.TeamMgr.GetTeamDataList(startIndex, searchTeamData.PageTeamsCount);
            SendListTeamsDataResult(client, searchTeamData);
        }

        /// <summary>
        /// 发送队伍列表的数据给客户端
        /// </summary>
        /// <param name="listRolesData"></param>
        private void SendListTeamsDataResult(GameClient client, SearchTeamData searchTeamData)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<SearchTeamData>(searchTeamData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_LISTTEAMS);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 队伍查询

        #region 日常任务

        /// <summary>
        /// 将新的日常任务数据通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDailyTaskData(GameClient client)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<DailyTaskData>>(client.ClientData.MyDailyTaskDataList, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_DAILYTASKDATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 日常任务

        #region 副本系统

        /// <summary>
        /// 将新的副本的数据通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyFuBenData(GameClient client, FuBenData fuBenData)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<FuBenData>(fuBenData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_FUBENDATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知副本的开始信息(每一层图怪物清空时也会调用)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyFuBenBeginInfo(GameClient client)
        {
            string strcmd = "";
            TCPOutPacket tcpOutPacket = null;

            // 新手场景 模拟信息 [12/13/2013 LiaoWei]
            if (client.ClientData.IsFlashPlayer == 1 && client.ClientData.MapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", client.ClientData.RoleID, -1, TimeUtil.NOW(), 0, 1, 0, 1, 1, 1);

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GETFUBENBEGININFO);
                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                    return;
            }

            int fuBenSeqID = FuBenManager.FindFuBenSeqIDByRoleID(client.ClientData.RoleID);
            if (fuBenSeqID <= 0) //如果副本不存在
            {
                return;
            }

            int copyMapID = client.ClientData.CopyMapID;
            if (copyMapID <= 0) //如果不是在副本地图中
            {
                return;
            }

            int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
            if (fuBenID <= 0)
            {
                return;
            }

            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(fuBenSeqID);
            if (null == fuBenInfoItem)
            {
                return;
            }

            CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(copyMapID);
            if (null == copyMap)
            {
                return;
            }

            // 剧情副本 [7/25/2014 LiaoWei]
            if (Global.IsStoryCopyMapScene(client.ClientData.MapCode))
            {
                SystemXmlItem systemFuBenItem = null;
                if (GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(copyMap.FubenMapID, out systemFuBenItem) && systemFuBenItem != null)
                {
                    int nBossID = -1;
                    nBossID = systemFuBenItem.GetIntValue("BossID");

                    int nNum = 0;
                    nNum = GameManager.MonsterZoneMgr.GetMapMonsterNum(client.ClientData.MapCode, nBossID);

                    if (nNum == 0)
                        Global.NotifyClientStoryCopyMapInfo(copyMap.CopyMapID, 1);
                    else
                        Global.NotifyClientStoryCopyMapInfo(copyMap.CopyMapID, 2);
                }

            }

            long startTicks = fuBenInfoItem.StartTicks;
            long endTicks = fuBenInfoItem.EndTicks;
            int killedNormalNum = copyMap.KilledNormalNum;
            int totalNormalNum = copyMap.TotalNormalNum;
            int killedBossNum = copyMap.KilledBossNum;
            int totalBossNum = copyMap.TotalBossNum;

            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}",
                client.ClientData.RoleID,
                fuBenID,
                startTicks,
                endTicks,
                killedNormalNum,
                totalNormalNum,
                killedBossNum,
                totalBossNum);

            tcpOutPacket = null;
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GETFUBENBEGININFO);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知副本地图上的所有人副本信息(同一个副本地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllFuBenBeginInfo(GameClient client, bool allKilled)
        {
            int fuBenSeqID = FuBenManager.FindFuBenSeqIDByRoleID(client.ClientData.RoleID);
            if (fuBenSeqID <= 0) //如果副本不存在
            {
                return;
            }

            int copyMapID = client.ClientData.CopyMapID;
            if (copyMapID <= 0) //如果不是在副本地图中
            {
                return;
            }

            int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
            if (fuBenID <= 0)
            {
                return;
            }

            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(fuBenSeqID);
            if (null == fuBenInfoItem)
            {
                return;
            }

            CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(copyMapID);
            if (null == copyMap)
            {
                return;
            }

            long startTicks = fuBenInfoItem.StartTicks;
            long endTicks = fuBenInfoItem.EndTicks;
            int killedNormalNum = copyMap.KilledNormalNum;
            int totalNormalNum = copyMap.TotalNormalNum;
            if (allKilled)
            {
                killedNormalNum = totalNormalNum;
            }

            int killedBossNum = copyMap.KilledBossNum;
            int totalBossNum = copyMap.TotalBossNum;
            if (allKilled)
            {
                killedBossNum = totalBossNum;
            }

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}",
                client.ClientData.RoleID,
                fuBenID,
                startTicks,
                endTicks,
                killedNormalNum,
                totalNormalNum,
                killedBossNum,
                totalBossNum);

            List<Object> objsList = GetMapClients(client.ClientData.MapCode);
            if (null == objsList)
            {
                return;
            }

            objsList = Global.ConvertObjsList(client.ClientData.MapCode, client.ClientData.CopyMapID, objsList);

            //群发消息
            SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_GETFUBENBEGININFO);
        }

        /// <summary>
        /// 通知副本所有子地图上的所有人副本结束或开始信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllMapFuBenBeginInfo(GameClient client, bool allKilled)
        {
            if (client.ClientData.FuBenSeqID <= 0 || client.ClientData.FuBenID <= 0)
                return;

            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(client.ClientData.FuBenSeqID);
            if (null == fuBenInfoItem)
            {
                return;
            }

            CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(client.ClientData.CopyMapID);
            if (null == copyMap)
            {
                return;
            }

            long startTicks = fuBenInfoItem.StartTicks;
            long endTicks = fuBenInfoItem.EndTicks;
            int killedNormalNum = copyMap.KilledNormalNum;
            int totalNormalNum = copyMap.TotalNormalNum;
            if (allKilled)
            {
                killedNormalNum = totalNormalNum;
            }

            int killedBossNum = copyMap.KilledBossNum;
            int totalBossNum = copyMap.TotalBossNum;
            if (allKilled)
            {
                killedBossNum = totalBossNum;
            }

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}",
                client.ClientData.RoleID,
                client.ClientData.FuBenID,
                startTicks,
                endTicks,
                killedNormalNum,
                totalNormalNum,
                killedBossNum,
                totalBossNum);

            List<Object> objsList = new List<Object>();

            //根据副本编号获取副本地图编号列表
            List<int> mapCodeList = FuBenManager.FindMapCodeListByFuBenID(copyMap.FubenMapID);
            if (null != mapCodeList)
            {
                //多地图副本需要处理各个地图内所有玩家
                foreach (int mapcode in mapCodeList)
                {
                    int copyMapID = GameManager.CopyMapMgr.FindCopyID(copyMap.FuBenSeqID, mapcode);
                    if (copyMapID >= 0)
                    {
                        CopyMap child_map = GameManager.CopyMapMgr.FindCopyMap(copyMapID);
                        if (null != child_map)
                        {
                            objsList.AddRange(child_map.GetClientsList());
                        }
                    }
                }
            }

            if (0 == objsList.Count)
            {
                return;
            }

            //群发消息
            SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_GETFUBENBEGININFO);
        }

        /// <summary>
        /// 通知副本地图上的所有人副本通关奖励信息(同一个副本地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllFuBenTongGuanJiangLi(GameClient client, byte[] bytesData)
        {
            int fuBenSeqID = FuBenManager.FindFuBenSeqIDByRoleID(client.ClientData.RoleID);
            if (fuBenSeqID <= 0) //如果副本不存在
            {
                return;
            }

            int copyMapID = client.ClientData.CopyMapID;
            if (copyMapID <= 0) //如果不是在副本地图中
            {
                return;
            }

            int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
            if (fuBenID <= 0)
            {
                return;
            }

            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(fuBenSeqID);
            if (null == fuBenInfoItem)
            {
                return;
            }

            CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(copyMapID);
            if (null == copyMap)
            {
                return;
            }

            List<Object> objsList = GetMapClients(client.ClientData.MapCode);
            if (null == objsList)
            {
                return;
            }

            objsList = Global.ConvertObjsList(client.ClientData.MapCode, client.ClientData.CopyMapID, objsList);

            //群发消息
            SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, bytesData, (int)TCPGameServerCmds.CMD_SPR_FUBENPASSNOTIFY);
        }

        /// <summary>
        /// 通知副本地图上的所有人怪物数量(同一个副本地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllFuBenMonstersNum(GameClient client, bool allKilled)
        {
            // 血色城堡副本 不发该消息 [7/8/2014 LiaoWei]
            if (GameManager.BloodCastleCopySceneMgr.IsBloodCastleCopyScene(client.ClientData.FuBenID))
            {
                return;
            }

            int fuBenSeqID = FuBenManager.FindFuBenSeqIDByRoleID(client.ClientData.RoleID);
            if (fuBenSeqID <= 0) //如果副本不存在
            {
                return;
            }

            int copyMapID = client.ClientData.CopyMapID;
            if (copyMapID <= 0) //如果不是在副本地图中
            {
                return;
            }

            CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(copyMapID);
            if (null == copyMap)
            {
                return;
            }

            int killedNormalNum = copyMap.KilledNormalNum;
            int totalNormalNum = copyMap.TotalNormalNum;
            if (allKilled)
            {
                killedNormalNum = totalNormalNum;
            }

            int killedBossNum = copyMap.KilledBossNum;
            int totalBossNum = copyMap.TotalBossNum;
            if (allKilled)
            {
                killedBossNum = totalBossNum;
            }

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}",
                copyMap.GetGameClientCount(), //client.ClientData.RoleID, //不发RoleID,改发副本内人数
                killedNormalNum,
                totalNormalNum,
                killedBossNum,
                totalBossNum);

            List<Object> objsList = GetMapClients(client.ClientData.MapCode);
            if (null == objsList)
            {
                return;
            }

            objsList = Global.ConvertObjsList(client.ClientData.MapCode, client.ClientData.CopyMapID, objsList);

            //群发消息
            SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_COPYMAPMONSTERSNUM);
        }

        #endregion 副本系统

        #region 每日冲穴次数

        /// <summary>
        /// 将新的每日冲穴次数数据通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDailyJingMaiData(GameClient client)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<DailyJingMaiData>(client.ClientData.MyDailyJingMaiData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_DAILYJINGMAIDATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知其他角色，获取的冲脉经验和剩余的获取次数
        /// </summary>
        /// <param name="clien"></param>
        public void NotifyOtherJingMaiExp(GameClient client)
        {
            int canGetExpNum = Global.GetLeftAddJingMaiExpNum(client);
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.TotalJingMaiExp, canGetExpNum);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_OHTERJINGMAIEXP);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 每日冲穴次数

        #region 技能消息/技能升级和熟练度

        /// <summary>
        /// 添加了新技能通知(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfAddSkill(SocketListener sl, TCPOutPacketPool pool, GameClient client, int skillDbID, int skillID, int skillLevel)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, skillDbID, skillID, skillLevel);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_ADD_SKILL);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 增加技能熟练度
        /// </summary>
        /// <param name="client"></param>
        /// <param name="addNum"></param>
        public void AddNumSkill(GameClient client, SkillData skillData, int addNum, bool writeToDB = true)
        {
            if (addNum != 0) //因为可以是负数
            {
                int oldUsedNum = 0;

                if (skillData.DbID < 0)
                {
                    oldUsedNum = client.ClientData.DefaultSkillUseNum;
                }
                else
                {
                    oldUsedNum = skillData.UsedNum;
                }

                // 完善熟练度逻辑 -- 到达该级别的熟练度最大值后 就不增加了[4/30/2014 LiaoWei]
                SystemXmlItem skillXml = null;
                skillXml = MagicsCacheManager.GetMagicCacheItem(client.ClientData.Occupation, skillData.SkillID, skillData.SkillLevel);

                if (addNum > 0)
                {
                    if (skillXml == null)
                        return; // 表配置错误 一般不会出现 最好能写个log

                    if (skillXml.GetIntValue("ShuLianDu") <= oldUsedNum)
                    {
                        if (skillData.DbID < 0)
                        {
                            client.ClientData.DefaultSkillUseNum = skillXml.GetIntValue("ShuLianDu");
                        }
                        else
                        {
                            skillData.UsedNum = skillXml.GetIntValue("ShuLianDu");
                        }

                        return;
                    }
                }

                int nUseNum = 0;
                if (skillData.DbID < 0)
                {
                    client.ClientData.DefaultSkillUseNum += addNum;

                    if (client.ClientData.DefaultSkillUseNum < 0)
                        client.ClientData.DefaultSkillUseNum = 0;

                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DefaultSkillUseNum, client.ClientData.DefaultSkillUseNum, false);

                    nUseNum = client.ClientData.DefaultSkillUseNum;
                }
                else
                {
                    skillData.UsedNum += addNum;

                    if (skillData.UsedNum < 0)
                        skillData.UsedNum = 0;

                    //更新技能信息
                    UpdateSkillInfo(client, skillData, writeToDB);

                    nUseNum = skillData.UsedNum;
                }

                if (skillXml != null && nUseNum >= skillXml.GetIntValue("ShuLianDu"))
                {
                    //通知技能熟练度满
                    GameManager.ClientMgr.NotifySkillUsedNumFull(client, skillData);
                }

                //改变技能熟练度满的通知状态
                /*if (skillData.UsedNum > oldUsedNum)
                {
                    //判断技能是否能够自动升级
                    //if (!Global.AutoUpLevelSkill(client, skillData))
                    {
                        Global.ChangeSkillUsedNumNotifyState(client, skillData, oldUsedNum, skillData.UsedNum);
                    }
                }*/
            }
        }

        /// <summary>
        /// 更新技能信息
        /// </summary>
        /// <param name="skillData"></param>
        public void UpdateSkillInfo(GameClient client, SkillData skillData, bool writeToDB = true)
        {
            //是否立刻写入数据库
            if (writeToDB)
            {
                Global.SetLastDBSkillCmdTicks(client, skillData.SkillID, 0);

                //异步写数据库，写入当前的重新开始闭关的的时间
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPSKILLINFO,
                    string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, skillData.DbID, skillData.SkillLevel, skillData.UsedNum),
                    null, client.ServerId);
            }
            else
            {
                long nowTicks = TimeUtil.NOW();
                Global.SetLastDBSkillCmdTicks(client, skillData.SkillID, nowTicks);
            }
        }

        /// <summary>
        /// 通知技能熟练度满
        /// </summary>
        /// <param name="client"></param>
        public void NotifySkillUsedNumFull(GameClient client, SkillData skillData)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, skillData.DbID, skillData.SkillID, skillData.UsedNum, skillData.SkillLevel);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SKILLUSEDNUMFULL);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 技能消息/技能升级和熟练度

        #region 随身仓库

        /// <summary>
        /// 将新的随身仓库数据通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyPortableBagData(GameClient client)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<PortableBagData>(client.ClientData.MyPortableBagData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_PORTABLEBAGDATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 随身仓库

        #region 系统送礼

        /// <summary>
        /// 向客户端发送活动数据
        /// </summary>
        /// <param name="client"></param>
        public void NotifyHuodongData(GameClient client)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<HuodongData>(client.ClientData.MyHuodongData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_GETHUODONGDATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 向客户端发送领取升级有礼完毕
        /// </summary>
        /// <param name="client"></param>
        public void NotifyGetLevelUpGiftData(GameClient client, int newLevel)
        {
            GameManager.ClientMgr.SendToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, String.Format("{0}:{1}", client.ClientData.RoleID, newLevel), (int)TCPGameServerCmds.CMD_SPR_GETUPLEVELGIFTOK);
        }

        /// <summary>
        /// 通知所有在线用户活动ID发生了改变
        /// </summary>
        /// <param name="bigAwardID"></param>
        /// <param name="songLiID"></param>
        public void NotifyAllChangeHuoDongID(int bigAwardID, int songLiID)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, bigAwardID, songLiID);
                TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGHUODONGID);
                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
        }

        #endregion 系统送礼

        #region 组队副本通知消息

        /// <summary>
        /// 通知组队副本进入的消息
        /// </summary>
        /// <param name="roleIDsList"></param>
        /// <param name="minLevel"></param>
        /// <param name="maxLevel"></param>
        /// <param name="mapCode"></param>
        public void NotifyTeamMemberFuBenEnterMsg(GameClient client, int leaderRoleID, int fuBenID, int fuBenSeqID)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, leaderRoleID, fuBenID, fuBenSeqID);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYENTERFUBEN);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知组队副本进入的消息
        /// </summary>
        /// <param name="roleIDsList"></param>
        /// <param name="minLevel"></param>
        /// <param name="maxLevel"></param>
        /// <param name="mapCode"></param>
        public void NotifyTeamFuBenEnterMsg(List<int> roleIDsList, int minLevel, int maxLevel, int leaderMapCode, int leaderRoleID, int fuBenID, int fuBenSeqID, int enterNumber, int maxFinishNum, bool igoreNumLimit = false)
        {
            if (null == roleIDsList || roleIDsList.Count <= 0) return;
            for (int i = 0; i < roleIDsList.Count; i++)
            {
                GameClient otherClient = FindClient(roleIDsList[i]);
                if (null == otherClient) continue; //不在线，则不通知

                //和队长不在同一个地图则不通知
                if (otherClient.ClientData.MapCode != leaderMapCode)
                {
                    continue;
                }

                //级别不匹配，则不通知
                int unionLevel = Global.GetUnionLevel(otherClient.ClientData.ChangeLifeCount, otherClient.ClientData.Level);
                if (unionLevel < minLevel || unionLevel > maxLevel)
                {
                    continue;
                }

                if (!igoreNumLimit)
                {
                    FuBenData fuBenData = Global.GetFuBenData(otherClient, fuBenID);
                    int nFinishNum;
                    int haveEnterNum = Global.GetFuBenEnterNum(fuBenData, out nFinishNum);
                    if ((enterNumber >= 0 && haveEnterNum >= enterNumber) || (maxFinishNum >= 0 && nFinishNum >= maxFinishNum))
                    {
                        continue;
                    }
                }

                //通知组队副本进入的消息
                NotifyTeamMemberFuBenEnterMsg(otherClient, leaderRoleID, fuBenID, fuBenSeqID);
            }
        }

        #endregion 组队副本通知消息

        #region 连斩管理

        /// <summary>
        /// 通知连斩值更新(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void ChangeRoleLianZhan(SocketListener sl, TCPOutPacketPool pool, GameClient client, Monster monster)
        {
            //传奇版本禁止掉连斩

            //怪物等级不得小于人物等级10级以上，否则不计连斩
            //if (monster.MonsterInfo.VLevel <= (client.ClientData.Level - Global.MaxLianZhanSubLevel))
            //{
            //    return;
            //}

            //通知连斩值更新(限制当前地图)
            //ChangeRoleLianZhan2(sl, pool, client, 1, false);
        }

        /// <summary>
        /// 通知连斩值更新(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void ChangeRoleLianZhan2(SocketListener sl, TCPOutPacketPool pool, GameClient client, int addNum, bool force)
        {
            int oldLianZhanNum = client.ClientData.TempLianZhan;

            //是否能继续计算连斩
            if (force || (Global.CanContinueLianZhan(client) && oldLianZhanNum < 999))
            {
                //获取旧的连斩的Buffer值
                int oldLianZhanBufferVal = Global.GetLianZhanBufferVal(oldLianZhanNum);

                //累计计数
                client.ClientData.TempLianZhan = client.ClientData.TempLianZhan + addNum;

                //记录连斩的最大值
                client.ClientData.LianZhan = Global.GMax(client.ClientData.LianZhan, client.ClientData.TempLianZhan);

                //获取新的连斩的Buffer值
                int newLianZhanBufferVal = Global.GetLianZhanBufferVal(client.ClientData.TempLianZhan);
                if (oldLianZhanBufferVal != newLianZhanBufferVal && newLianZhanBufferVal > 0)
                {
                    //更新BufferData
                    double[] actionParams = new double[2];
                    actionParams[0] = 30.0;
                    actionParams[1] = (double)newLianZhanBufferVal;
                    Global.UpdateBufferData(client, BufferItemTypes.AntiBoss, actionParams);
                }

                //连斩提示
                Global.BroadcastLianZhanNum(client, oldLianZhanNum, client.ClientData.TempLianZhan);
            }
            else
            {
                //重新开始计数
                client.ClientData.TempLianZhan = 1;
            }

            client.ClientData.StartLianZhanTicks = TimeUtil.NOW();

            //获取连斩的时间间隔
            double secs = Global.GetLianZhanSecs(client.ClientData.TempLianZhan);
            client.ClientData.WaitingLianZhanTicks = (int)(secs * 1000);

            //给角色添加Buffer

            //只有一个连斩时不通知
            if (client.ClientData.TempLianZhan <= 1)
            {
                return;
            }

            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.TempLianZhan, secs);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGLIANZHAN);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 连斩管理

        #region 更新角色的日常数据

        /// <summary>
        /// 更新角色的日常数据_经验
        /// </summary>
        /// <param name="client"></param>
        /// <param name="newExperience"></param>
        public void UpdateRoleDailyData_Exp(GameClient client, long newExperience)
        {
            if (null == client.ClientData.MyRoleDailyData)
            {
                client.ClientData.MyRoleDailyData = new RoleDailyData();
            }

            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (dayID == client.ClientData.MyRoleDailyData.ExpDayID)
            {
                client.ClientData.MyRoleDailyData.TodayExp += (int)newExperience;
            }
            else
            {
                client.ClientData.MyRoleDailyData.ExpDayID = dayID;
                client.ClientData.MyRoleDailyData.TodayExp = (int)newExperience;
            }
        }

        /// <summary>
        /// 更新角色的日常数据_灵力
        /// </summary>
        /// <param name="client"></param>
        /// <param name="newExperience"></param>
        public void UpdateRoleDailyData_LingLi(GameClient client, int newLingLi)
        {
            if (null == client.ClientData.MyRoleDailyData)
            {
                client.ClientData.MyRoleDailyData = new RoleDailyData();
            }

            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (dayID == client.ClientData.MyRoleDailyData.LingLiDayID)
            {
                client.ClientData.MyRoleDailyData.TodayLingLi += newLingLi;
            }
            else
            {
                client.ClientData.MyRoleDailyData.LingLiDayID = dayID;
                client.ClientData.MyRoleDailyData.TodayLingLi = newLingLi;
            }
        }

        /// <summary>
        /// 更新角色的日常数据_杀BOSS数量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="newExperience"></param>
        public void UpdateRoleDailyData_KillBoss(GameClient client, int newKillBoss)
        {
            if (null == client.ClientData.MyRoleDailyData)
            {
                client.ClientData.MyRoleDailyData = new RoleDailyData();
            }

            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (dayID == client.ClientData.MyRoleDailyData.KillBossDayID)
            {
                client.ClientData.MyRoleDailyData.TodayKillBoss += newKillBoss;
            }
            else
            {
                client.ClientData.MyRoleDailyData.KillBossDayID = dayID;
                client.ClientData.MyRoleDailyData.TodayKillBoss = newKillBoss;
            }
        }

        /// <summary>
        /// 更新角色的日常数据_通关副本数量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="newExperience"></param>
        public void UpdateRoleDailyData_FuBenNum(GameClient client, int newFuBenNum, int nLev, bool bActiveChenJiu = true)
        {
            if (null == client.ClientData.MyRoleDailyData)
            {
                client.ClientData.MyRoleDailyData = new RoleDailyData();
            }

            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (dayID == client.ClientData.MyRoleDailyData.FuBenDayID)
            {
                client.ClientData.MyRoleDailyData.TodayFuBenNum += newFuBenNum;
            }
            else
            {
                client.ClientData.MyRoleDailyData.FuBenDayID = dayID;
                client.ClientData.MyRoleDailyData.TodayFuBenNum = newFuBenNum;
            }

            DailyActiveManager.ProcessCompleteCopyMapForDailyActive(client, nLev);

            // 副本完成成就 [3/12/2014 LiaoWei]
            if (bActiveChenJiu)
                ChengJiuManager.ProcessCompleteCopyMapForChengJiu(client, nLev);
        }

        /// <summary>
        /// 更新角色的日常数据_五行奇阵领取奖励数量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="newExperience"></param>
        public void UpdateRoleDailyData_WuXingNum(GameClient client, int newWuXingNum)
        {
            if (null == client.ClientData.MyRoleDailyData)
            {
                client.ClientData.MyRoleDailyData = new RoleDailyData();
            }

            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (dayID == client.ClientData.MyRoleDailyData.WuXingDayID)
            {
                client.ClientData.MyRoleDailyData.WuXingNum += newWuXingNum;
            }
            else
            {
                client.ClientData.MyRoleDailyData.WuXingDayID = dayID;
                client.ClientData.MyRoleDailyData.WuXingNum = newWuXingNum;
            }
        }

        /// <summary>
        /// 更新角色的日常数据_扫荡次数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="newExperience"></param>
        public void UpdateRoleDailyData_SweepNum(GameClient client, int newWuXingNum)
        {
            if (null == client.ClientData.MyRoleDailyData)
            {
                client.ClientData.MyRoleDailyData = new RoleDailyData();
            }

            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (dayID == client.ClientData.MyRoleDailyData.WuXingDayID)
            {
                client.ClientData.MyRoleDailyData.WuXingNum += newWuXingNum;
            }
            else
            {
                client.ClientData.MyRoleDailyData.WuXingDayID = dayID;
                client.ClientData.MyRoleDailyData.WuXingNum = newWuXingNum;
            }
        }

        /// <summary>
        /// 将新角色每日数据通知客户端
        /// </summary>
        /// <param name="client"></param>
        public void NotifyRoleDailyData(GameClient client)
        {
            RoleDailyData roleDailyData = client.ClientData.MyRoleDailyData;
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<RoleDailyData>(roleDailyData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_GETROLEDAILYDATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 更新角色的日常数据

        #region 杀BOSS数量更新

        /// <summary>
        /// 更新杀BOSS的数量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="killBossNum"></param>
        public void UpdateKillBoss(GameClient client, int killBossNum, Monster monster, bool writeToDB = false)
        {
            //如果不是BOSS，则不处理
            if ((int)MonsterTypes.BOSS != monster.MonsterType)
            {
                return;
            }

            int[] ids = GameManager.systemParamsList.GetParamValueIntArrayByName("NotTuMo");
            if (null != ids && ids.Length > 0)
            {
                for (int i = 0; i < ids.Length; i++)
                {
#if ___CC___FUCK___YOU___BB___
                    if (monster.XMonsterInfo.MonsterId == ids[i])
                    {
                        return;
                    }
#else
              if (monster.MonsterInfo.ExtensionID == ids[i])
                    {
                        return;
                    }
#endif


                }
            }

            client.ClientData.KillBoss += killBossNum;

            //更新每日的杀BOSS的数据
            UpdateRoleDailyData_KillBoss(client, killBossNum);

            if (writeToDB)
            {
                //更新PKValue
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEKILLBOSS,
                    string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.KillBoss),
                    null, client.ServerId);

                long nowTicks = TimeUtil.NOW();
                Global.SetLastDBCmdTicks(client, (int)TCPGameServerCmds.CMD_DB_UPDATEKILLBOSS, nowTicks);
            }
        }

        #endregion 杀BOSS数量更新

        #region 角斗场称号次数更新

        /// <summary>
        /// 角斗场称号次数更新
        /// </summary>
        /// <param name="client"></param>
        /// <param name="killBossNum"></param>
        public void UpdateBattleNum(GameClient client, int addNum, bool writeToDB = false)
        {
            client.ClientData.BattleNum += addNum;

            if (writeToDB)
            {
                //更新角斗场称号次数
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEBATTLENUM,
                    string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.BattleNum),
                    null, client.ServerId);

                long nowTicks = TimeUtil.NOW();
                Global.SetLastDBCmdTicks(client, (int)TCPGameServerCmds.CMD_DB_UPDATEBATTLENUM, nowTicks);
            }
        }

        #endregion 角斗场称号次数更新



        #region 英雄逐擂的到达层数更新

        /// <summary>
        /// 通知英雄逐擂到达层数更新(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void ChangeRoleHeroIndex(SocketListener sl, TCPOutPacketPool pool, GameClient client, int heroIndex, bool force = false)
        {
            if (!force)
            {
                if (heroIndex <= 0) return;
                int oldHeroIndex = client.ClientData.HeroIndex;
                if (heroIndex <= oldHeroIndex)
                {
                    //英雄逐擂过关通知
                    Global.BroadcastHeroMapOk(client, heroIndex, false);

                    return; //如果新的到达层数小于等于旧的到达层数，则不修改，不通知
                }
            }

            client.ClientData.HeroIndex = Math.Min(13, heroIndex); //新的记录, 会被自动提交

            //英雄逐擂过关通知
            Global.BroadcastHeroMapOk(client, heroIndex, true);

            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.HeroIndex);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGHEROINDEX);
        }

        #endregion 英雄逐擂的到达层数更新

        #region BOSS刷新数据

        /// <summary>
        /// 将BOSS刷新数据通知客户端
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBossInfoDictData(GameClient client)
        {
            //BOSS刷新数据字典
            Dictionary<int, BossData> dict = MonsterBossManager.GetBossDictData();
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<Dictionary<int, BossData>>(dict, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_GETBOSSINFODICT);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion BOSS刷新数据

        #region 镖车相关

        /// <summary>
        /// 将押镖数据通知客户端
        /// </summary>
        /// <param name="client"></param>
        public void NotifyYaBiaoData(GameClient client)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<YaBiaoData>(client.ClientData.MyYaBiaoData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_YABIAODATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 镖车血变化(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOtherBiaoCheLifeV(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, int biaoCheID, int currentLifeV)
        {
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", biaoCheID, currentLifeV);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGBIAOCHELIFEV);
        }

        /// <summary>
        /// 镖车通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        //public void NotifyOthersNewBiaoChe(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, BiaoCheItem biaoCheItem)
        //{
        //    if (null == objsList) return;

        //    //镖车项到镖车数据的转换
        //    BiaoCheData biaoCheData = Global.BiaoCheItem2BiaoCheData(biaoCheItem);

        //    byte[] bytesData = DataHelper.ObjectToBytes<BiaoCheData>(biaoCheData);

        //    //群发消息
        //    SendToClients(sl, pool, null, objsList, bytesData, (int)TCPGameServerCmds.CMD_SPR_NEWBIAOCHE);
        //}

        /// <summary>
        /// 镖车通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfNewBiaoChe(SocketListener sl, TCPOutPacketPool pool, GameClient client, BiaoCheItem biaoCheItem)
        {
            //镖车项到镖车数据的转换
            BiaoCheData biaoCheData = Global.BiaoCheItem2BiaoCheData(biaoCheItem);

            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<BiaoCheData>(biaoCheData, pool, (int)TCPGameServerCmds.CMD_SPR_NEWBIAOCHE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 镖车消失通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        //public void NotifyOthersDelBiaoChe(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, int biaoCheID)
        //{
        //    if (null == objsList) return;

        //    string strcmd = string.Format("{0}", biaoCheID);

        //    //群发消息
        //    SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELBIAOCHE);
        //}

        /// <summary>
        /// 镖车消失通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfDelBiaoChe(SocketListener sl, TCPOutPacketPool pool, GameClient client, int biaoCheID)
        {
            string strcmd = string.Format("{0}", biaoCheID);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELBIAOCHE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 镖车相关

        #region 弹窗管理

        /// <summary>
        /// 通知在线的所有人(不限制地图)弹窗消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllPopupWinMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string strcmd)
        {
            int index = 0;
            GameClient gc = null;
            while ((gc = GetNextClient(ref index)) != null)
            {
                if (null != client && gc == client)
                {
                    continue;
                }

                if (gc.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                //通知在线的对方(不限制地图)公告消息
                NotifyPopupWinMsg(sl, pool, gc, strcmd);
            }
        }

        /// <summary>
        /// 通知在线的对方(不限制地图)弹窗消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyPopupWinMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string strcmd)
        {
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOFITYPOPUPWIN);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 弹窗管理

        #region 登录次数自动增加

        /// <summary>
        /// 重新计算按照日来判断的登录次数
        /// </summary>
        /// <param name="client"></param>
        private void ChangeDayLoginNum(GameClient client)
        {
            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (dayID == client.ClientData.LoginDayID)
            {
                return;
            }

            client.ClientData.LoginDayID = dayID;

            /// 当角色在节日期间登录游戏的时候--->每天只会被调用一次
            HuodongCachingMgr.OnJieriRoleLogin(client, Global.SafeConvertToInt32(client.ClientData.MyHuodongData.LastDayID));

            //更新成就相关登录次数--->一定要在UpdateWeekLoginNum 前面调用，保证 MyHuodongData中LastDayID未被更改
            ChengJiuManager.OnRoleLogin(client, Global.SafeConvertToInt32(client.ClientData.MyHuodongData.LastDayID));

            //更新前七天的每天在线累计时长--->一定要在UpdateWeekLoginNum 前面调用，保证 MyHuodongData中LastDayID未被更改
            HuodongCachingMgr.ProcessDayOnlineSecs(client, Global.SafeConvertToInt32(client.ClientData.MyHuodongData.LastDayID));

            //更新周连续登录的次数
            bool notifyHuodDongData = Global.UpdateWeekLoginNum(client);

            //更新限时累计登录次数
            notifyHuodDongData |= Global.UpdateLimitTimeLoginNum(client);
            if (notifyHuodDongData) //是否通知客户端
            {
                GameManager.ClientMgr.NotifyHuodongData(client);
            }

            Global.UpdateHuoDongDBCommand(Global._TCPManager.TcpOutPacketPool, client);

            //跨天的时候
            Global.GiveGuMuTimeLimitAward(client);

            //在线跨天的时候处理每日任务
            Global.InitRoleDailyTaskData(client, true);

            //跨天时清除采集数据，记录资源找回数据，必须在InitRoleOldResourceInfo之前调用
            CaiJiLogic.InitRoleDailyCaiJiData(client, false, true);

            HuanYingSiYuanManager.getInstance().InitRoleDailyHYSYData(client);

            Global.ProcessUpdateFuBenData(client);

            //初始化资源找回数据
            CGetOldResourceManager.InitRoleOldResourceInfo(client, true);

            // 更新合服登陆活动
            Global.UpdateHeFuLoginFlag(client);

            // 更新合服累计登陆活动的计数
            Global.UpdateHeFuTotalLoginFlag(client);

            // 向客户端推送图标变化
            //if (client._IconStateMgr.CheckHeFuLogin(client) 
            //    || client._IconStateMgr.CheckHeFuTotalLogin(client)
            //    || client._IconStateMgr.CheckHeFuPKKing(client))
            if (client._IconStateMgr.CheckHeFuActivity(client)
                || client._IconStateMgr.CheckSpecialActivity(client))
                client._IconStateMgr.SendIconStateToClient(client);

            // 更新在线用户的月卡信息
            YueKaManager.UpdateNewDay(client);

            //玩家召回
            UserReturnManager.getInstance().initUserReturnData(client);

            FundManager.initFundData(client);

            // [bing] 在线玩家跨天清理结婚送花次数
            MarriageOtherLogic.getInstance().ChangeDayUpdate(client);
            MarryPartyLogic.getInstance().MarryPartyJoinListClear(client, true);

            // 跨天更新角色登陆记录
            Global.UpdateRoleLoginRecord(client);

            JieriGiveActivity giveAct = HuodongCachingMgr.GetJieriGiveActivity();
            if (giveAct != null)
            {
                giveAct.UpdateNewDay(client);
            }

            JieriRecvActivity recvAct = HuodongCachingMgr.GetJieriRecvActivity();
            if (recvAct != null)
            {
                recvAct.UpdateNewDay(client);
            }

            // 对于跨天的同步处理
            client.sendCmd((int)TCPGameServerCmds.CMD_SYNC_CHANGE_DAY_SERVER, string.Format("{0}", TimeUtil.NOW() * 10000));

            // 七日活动
            SevenDayActivityMgr.Instance().OnNewDay(client);

            ZhengBaManager.Instance().OnNewDay(client);
        }

        /*/// <summary>
        //  新增加一个接口 处理连续登陆[1/19/2014 LiaoWei]
        /// </summary>
        /// <param name="client"></param>
        private void ProcessSeriesLogin(GameClient client)
        {
            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (dayID == client.ClientData.RoleLoginDayID)
                return;

            client.ClientData.MyHuodongData.EveryDayOnLineAwardStep = 0;    // 设置下每日在线奖励的领取到了第几步

            client.ClientData.MyHuodongData.SeriesLoginGetAwardStep = 0;    // 设置下连续登陆奖励的领取到了第几步

            client.ClientData.DayOnlineSecond = 0;      // 每日在线时长重置

            Global.UpdateSeriesLoginInfo(client);

            //client.ClientData.RoleLoginDayID = dayID;

            Global.UpdateHuoDongDBCommand(Global._TCPManager.TcpOutPacketPool, client);

        }*/

        #endregion 登录次数自动增加

        #region 加成属性索引管理

        /// <summary>
        /// 通知全套加成属性值更新(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void ChangeAllThingAddPropIndexs(GameClient client)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, client.ClientData.AllQualityIndex, client.ClientData.AllForgeLevelIndex, client.ClientData.AllJewelLevelIndex, client.ClientData.AllZhuoYueNum);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_UPDATEALLTHINGINDEXS);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 加成属性索引管理

        #region 银两折半优惠通知

        /// <summary>
        /// 通知所有在线用户银两折半优惠发生了改变
        /// </summary>
        /// <param name="bigAwardID"></param>
        /// <param name="songLiID"></param>
        public void NotifyAllChangeHalfYinLiangPeriod(int halfYinLiangPeriod)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, halfYinLiangPeriod);
                TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGHALFYINLIANGPERIOD);
                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
        }

        #endregion 银两折半优惠通知

        #region 帮派管理

        /// <summary>
        /// 通知帮派信息(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void ChangeBangHuiName(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, client.ClientData.Faction, client.ClientData.BHName, client.ClientData.BHZhiWu);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGBANGHUIINFO);
        }

        /// <summary>
        /// 通知帮会职务变更(限制当前地图)
        /// </summary>
        /// <param name="client"></param>
        public void ChangeBangHuiZhiWu(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.Faction, client.ClientData.BHZhiWu);

            // 公告委任通知 [1/16/2014 LiaoWei]
            if (client.ClientData.BHZhiWu > 0)
            {
                string sBusiness = "";

                if (client.ClientData.BHZhiWu == 1)
                    sBusiness = Global.GetLang("首领");
                else if (client.ClientData.BHZhiWu == 2)
                    sBusiness = Global.GetLang("副首领");
                else if (client.ClientData.BHZhiWu == 3)
                    sBusiness = Global.GetLang("左将军");
                else if (client.ClientData.BHZhiWu == 4)
                    sBusiness = Global.GetLang("右将军");

                //string sMsg = client.ClientData.RoleName + "被委任为" + sBusiness;

                //GameManager.ClientMgr.NotifyFactionChatMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.ClientData.Faction, sMsg);

                Global.BroadcastBangHuiMsg(client.ClientData.RoleID, client.ClientData.Faction,
                    StringUtil.substitute(Global.GetLang("【{0}】被委任为『{1}』"), client.ClientData.RoleName, sBusiness),
                    true, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox);
            }

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYBHZHIWU);
        }

        /// <summary>
        /// 通知所有在线的帮会管理用户，某个用户申请了加入帮派
        /// </summary>
        /// <param name="bigAwardID"></param>
        /// <param name="songLiID"></param>
        public void NotifyOnlineBangHuiMgrRoleApplyMsg(int roleID, string roleName, int bhid, string bhName, string roleList)
        {
            if (string.IsNullOrEmpty(roleList))
            {
                return;
            }

            string[] fields = roleList.Split(',');
            if (null == fields || fields.Length <= 0) return;

            // 增加申请者的职业和等级信息 [12/31/2013 LiaoWei]
            GameClient clientApply = null;
            clientApply = GameManager.ClientMgr.FindClient(roleID);

            if (clientApply == null)
                return;

            GameClient client = null;
            for (int i = 0; i < fields.Length; i++)
            {
                int bhMgrRoleID = Global.SafeConvertToInt32(fields[i]);
                if (bhMgrRoleID <= 0) continue;

                client = GameManager.ClientMgr.FindClient(bhMgrRoleID);
                if (null == client)
                {
                    continue;
                }

                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", roleID, roleName, clientApply.ClientData.Occupation, clientApply.ClientData.Level, clientApply.ClientData.ChangeLifeCount, bhid, bhName);
                TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_APPLYTOBHMEMBER);
                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
        }

        /// <summary>
        /// 加入帮派邀请通知通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyInviteToBangHui(SocketListener sl, TCPOutPacketPool pool, GameClient otherClient, int inviteRoleID, string inviteRoleName, int bhid, string bhName, int nChangelifeLev)
        {
            //如果等级不到，则不发送邀请通知
            if (Global.GetUnionLevel(otherClient) < Global.JoinBangHuiNeedLevel)
            {
                return;
            }

            string strcmd = string.Format("{0}:{1}:{2}:{3}", inviteRoleID, inviteRoleName, bhid, bhName, nChangelifeLev);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_INVITETOBANGHUI);
            if (!sl.SendData(otherClient.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    otherClient.ClientData.RoleID,
                    otherClient.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知某个角色被加入了某个帮派
        /// </summary>
        /// <param name="client"></param>
        public void NotifyJoinBangHui(SocketListener sl, TCPOutPacketPool pool, GameClient otherClient, int bhid, string bhName)
        {
            //防止重复设置帮会
            if (otherClient.ClientData.Faction > 0)
            {
                return;
            }

            //修改加入帮派的角色的信息
            otherClient.ClientData.Faction = bhid;
            otherClient.ClientData.BHName = bhName;
            otherClient.ClientData.BHZhiWu = 0;

            //通知附近用户，某用户的帮派信息进行了修改
            //通知帮派信息(限制当前地图)
            GameManager.ClientMgr.ChangeBangHuiName(sl, pool, otherClient);
            GlobalEventSource4Scene.getInstance().fireEvent(new PostBangHuiChangeEventObject(otherClient, bhid), (int)SceneUIClasses.All);
            Global.SaveRoleParamsInt32ValueToDB(otherClient, RoleParamName.EnterBangHuiUnixSecs, DataHelper.UnixSecondsNow(), true);

            int junQiLevel = JunQiManager.GetJunQiLevelByBHID(otherClient.ClientData.Faction);

            //更新BufferData
            double[] actionParams = new double[1];
            actionParams[0] = (double)junQiLevel - 1;
            Global.UpdateBufferData(otherClient, BufferItemTypes.JunQi, actionParams, 1);

            //通知本帮派的所有在线的人，某人加入了本帮派
            Global.BroadcastBangHuiMsg(otherClient.ClientData.RoleID, bhid,
                StringUtil.substitute(Global.GetLang("『{0}』加入了『{1}』战盟"), otherClient.ClientData.RoleName, otherClient.ClientData.BHName),
                true, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox);

            //成就处理第一次加入帮会
            ChengJiuManager.OnFirstInFaction(otherClient);
            UnionPalaceManager.initSetUnionPalaceProps(otherClient, true);

            otherClient._IconStateMgr.CheckGuildIcon(otherClient, false);
        }

        /// <summary>
        /// 通知某个角色离开了某个帮派
        /// </summary>
        /// <param name="client"></param>
        public void NotifyLeaveBangHui(SocketListener sl, TCPOutPacketPool pool, GameClient otherClient, int bhid, string bhName, int leaveType)
        {
            //防止重复设置
            if (otherClient.ClientData.Faction <= 0)
            {
                return;
            }

            //通知本帮派的所有在线的人，某人加入了本帮派
            Global.BroadcastBangHuiMsg(otherClient.ClientData.RoleID, bhid,
                StringUtil.substitute(Global.GetLang("『{0}』{1}『{2}』战盟"), otherClient.ClientData.RoleName, leaveType <= 0 ? Global.GetLang("被开除出了") : Global.GetLang("脱离了"), bhName),
                true, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox);

            //修改加入帮派的角色的信息
            otherClient.ClientData.Faction = 0;
            otherClient.ClientData.BHName = "";
            otherClient.ClientData.BHZhiWu = 0;
            //otherClient.ClientData.BangGong = 0; // 离开帮会 帮贡不清空 [5/11/2014 LiaoWei]

            //通知附近用户，某用户的帮派信息进行了修改
            //通知帮派信息(限制当前地图)
            GameManager.ClientMgr.ChangeBangHuiName(sl, pool, otherClient);
            GlobalEventSource4Scene.getInstance().fireEvent(new PostBangHuiChangeEventObject(otherClient, bhid), (int)SceneUIClasses.All);

            //帮贡变化通知(只通知自己)
            //GameManager.ClientMgr.NotifySelfBangGongChange(sl, pool, otherClient);

            //从buffer数据到列表删除指定的临时Buffer
            Global.RemoveBufferData(otherClient, (int)BufferItemTypes.JunQi);

            UnionPalaceManager.initSetUnionPalaceProps(otherClient, true);

            //通知用户数值发生了变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, otherClient);

            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, otherClient);
        }

        /// <summary>
        /// 通知所有指定帮会的在线用户帮会已经解散
        /// </summary>
        /// <param name="bigAwardID"></param>
        /// <param name="songLiID"></param>
        public void NotifyBangHuiDestroy(int retCode, int roleID, int bhid)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientData.Faction != bhid)
                {
                    continue;
                }

                if (client.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                //修改加入帮派的角色的信息
                client.ClientData.Faction = 0;
                client.ClientData.BHName = "";
                client.ClientData.BHZhiWu = 0;
                // 帮贡不清
                //client.ClientData.BangGong = 0;

                string strcmd = string.Format("{0}:{1}:{2}", retCode, roleID, bhid);
                TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DESTROYBANGHUI);
                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }

                //从buffer数据到列表删除指定的临时Buffer
                Global.RemoveBufferData(client, (int)BufferItemTypes.JunQi);

                UnionPalaceManager.initSetUnionPalaceProps(client, true);

                client.ClientData.AllyList = null;

                //通知用户数值发生了变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
        }

        public void NotifyBangHuiUpLevel(int bhid, int serverID, int level, bool isKF)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientData.Faction != bhid || client.ClientSocket.IsKuaFuLogin)
                    continue;

                UnionPalaceManager.initSetUnionPalaceProps(client, true);
            }

            if (AllyManager.getInstance().IsAllyOpen(level))
                AllyManager.getInstance().UnionDataChange(bhid, serverID);
        }


        public void NotifyBangHuiChangeName(int bhid, string newName)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientData.Faction != bhid)
                {
                    continue;
                }

                if (client.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                //修改加入帮派的角色的信息
                client.ClientData.BHName = newName;

                string strcmd = string.Format("{0}:{1}", bhid, newName);
                TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_NTF_BANGHUI_CHANGE_NAME);
                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //
                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                        tcpOutPacket.PacketCmdID,
                        tcpOutPacket.PacketDataSize,
                        client.ClientData.RoleID,
                        client.ClientData.RoleName));*/
                }
            }
        }

        /// <summary>
        /// 拒绝申请加入帮派的操作
        /// </summary>
        /// <param name="client"></param>
        public void NotifyRefuseApplyToBHMember(GameClient otherClient, string bhRoleName, string bhName)
        {
            if (otherClient.ClientData.Faction > 0) //已经加入帮派
            {
                return;
            }

            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, otherClient,
                StringUtil.substitute(Global.GetLang("『{0}』拒绝了你加入『{1}』战盟的申请"), bhRoleName, bhName),
                GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
        }

        /// <summary>
        /// 拒绝邀请加入帮派的操作
        /// </summary>
        /// <param name="client"></param>
        public void NotifyRefuseInviteToBHMember(GameClient otherClient, string bhRoleName, string bhName)
        {
            if (otherClient.ClientData.Faction <= 0) //已经无帮派
            {
                return;
            }

            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, otherClient,
                StringUtil.substitute(Global.GetLang("『{0}』拒绝了你加入『{1}』战盟的邀请"), bhRoleName, bhName),
                GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
        }

        #endregion 帮派管理

        #region 帮贡处理

        /// <summary>
        /// 帮贡变化通知(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfBangGongChange(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.BangGong, client.ClientData.BGMoney);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_BANGGONGCHANGE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 添加用户帮贡
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddBangGong(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, ref int addBangGong, AddBangGongTypes addBangGongType, int nBangGongLimit = 0)
        {
            int oldBangGong = client.ClientData.BangGong;
            int dayID = TimeUtil.NowDateTime().DayOfYear;

            if (client.ClientData.BGDayID1 != dayID)
            {
                client.ClientData.BGMoney = 0;
                client.ClientData.BGDayID1 = dayID;
            }

            if (client.ClientData.BGDayID2 != dayID)
            {
                client.ClientData.BGGoods = 0;
                client.ClientData.BGDayID2 = dayID;
            }

            if (AddBangGongTypes.BGGold == addBangGongType) // 用金币  //如果是贡献铜钱帮贡
            {
                int oldBGMoney = client.ClientData.BGMoney;
                client.ClientData.BGMoney = Global.GMin(client.ClientData.BGMoney + addBangGong, nBangGongLimit);   //[bing] 如果oldBGMoney 大于 Limit 则会造成addBangGong为负值 可能会有这种情况么? 先mark一下
                addBangGong = client.ClientData.BGMoney - oldBGMoney;
            }
            else if (AddBangGongTypes.BGGoods == addBangGongType) // 用钻石 //如果是贡献道具帮贡
            {
                int oldBGGoods = client.ClientData.BGGoods;
                client.ClientData.BGGoods = Global.GMin(client.ClientData.BGGoods + addBangGong, nBangGongLimit);
                addBangGong = client.ClientData.BGGoods - oldBGGoods;
            }

            if (0 == addBangGong)
            {
                return true;
            }

            //先DBServer请求扣费
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                client.ClientData.RoleID,
                client.ClientData.BGDayID1,
                client.ClientData.BGMoney,
                client.ClientData.BGDayID2,
                client.ClientData.BGGoods,
                addBangGong); //只发增量

            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEBANGGONG_CMD, strcmd, client.ServerId);
            if (null == dbFields) return false;
            if (dbFields.Length != 2)
            {
                return false;
            }

            // 先锁定
            if (Convert.ToInt32(dbFields[1]) < 0)
            {
                return false; //帮贡添加失败
            }

            // 先锁定
            client.ClientData.BangGong = Convert.ToInt32(dbFields[1]);

            // MU成就处理 战盟成就 [3/30/2014 LiaoWei]
            ChengJiuManager.OnRoleGuildChengJiu(client);

            // 帮贡变化通知(只通知自己)
            GameManager.ClientMgr.NotifySelfBangGongChange(sl, pool, client);

            //写入角色帮贡增加/减少日志
            Global.AddRoleBangGongEvent(client, oldBangGong);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.BangGong, addBangGong, client.ClientData.BangGong, addBangGongType.ToString());

            return true;
        }

        public bool AddBangGong(GameClient client, ref int addBangGong, AddBangGongTypes addBangGongType, int nBangGongLimit = 0)
        {
            return AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ref addBangGong, addBangGongType, nBangGongLimit);
        }

        /// <summary>
        /// 扣除用户帮贡
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool SubUserBangGong(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int subBangGong)
        {
            int oldBangGong = client.ClientData.BangGong;

            if (client.ClientData.BangGong < subBangGong)
            {
                return false; //帮贡余额不足
            }

            //先DBServer请求扣费
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                client.ClientData.RoleID,
                client.ClientData.BGDayID1,
                client.ClientData.BGMoney,
                client.ClientData.BGDayID2,
                client.ClientData.BGGoods,
                -subBangGong); //只发增量

            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEBANGGONG_CMD, strcmd, client.ServerId);
            if (null == dbFields) return false;
            if (dbFields.Length != 2)
            {
                return false;
            }

            // 先锁定
            if (Convert.ToInt32(dbFields[1]) < 0)
            {
                return false; //帮贡扣除失败，余额不足
            }

            client.ClientData.BangGong = Convert.ToInt32(dbFields[1]);

            // 帮贡变化通知(只通知自己)
            GameManager.ClientMgr.NotifySelfBangGongChange(sl, pool, client);

            //写入角色帮贡增加/减少日志
            Global.AddRoleBangGongEvent(client, oldBangGong);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.BangGong, -subBangGong, client.ClientData.BangGong, "none");

            return true;
        }

        #endregion 帮贡处理

        #region 帮会库存铜钱处理

        /// <summary>
        /// 添加帮会库存铜钱
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddBangHuiTongQian(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int bhid, int addMoney)
        {
            //先DBServer请求扣费
            string strcmd = string.Format("{0}:{1}:{2}",
                client.ClientData.RoleID,
                bhid,
                addMoney);

            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_ADDBHTONGQIAN_CMD, strcmd, client.ServerId);
            if (null == dbFields) return false;
            if (dbFields.Length != 2)
            {
                return false;
            }

            // 先锁定
            if (Convert.ToInt32(dbFields[0]) < 0)
            {
                return false;
            }

            GameManager.ClientMgr.NotifyBangHuiZiJinChanged(client, bhid);

            return true;
        }

        /// <summary>
        /// 扣除帮会库存铜钱
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool SubBangHuiTongQian(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int subMoney, out int bhZoneID)
        {
            bhZoneID = 0;
            if (client.ClientData.Faction <= 0)
            {
                return false;
            }

            //先DBServer请求扣费
            string strcmd = string.Format("{0}:{1}:{2}",
                client.ClientData.RoleID,
                client.ClientData.Faction,
                subMoney);

            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEBHTONGQIAN_CMD, strcmd, client.ServerId);
            if (null == dbFields) return false;
            if (dbFields.Length != 2)
            {
                return false;
            }

            // 先锁定
            if (Convert.ToInt32(dbFields[0]) < 0)
            {
                return false; //帮会库存扣除失败，余额不足
            }

            bhZoneID = Global.SafeConvertToInt32(dbFields[1]);
            GameManager.ClientMgr.NotifyBangHuiZiJinChanged(client, client.ClientData.Faction);
            return true;
        }

        /// <summary>
        /// 通知帮会自己变化
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bhid"></param>
        public void NotifyBangHuiZiJinChanged(GameClient client, int bhid)
        {
            int roleID = client.ClientData.RoleID;

            BangHuiDetailData bangHuiDetailData = Global.GetBangHuiDetailData(roleID, bhid);
            if (null != bangHuiDetailData)
            {
                GameClient clientBZ = client;
                if (roleID != bangHuiDetailData.BZRoleID)
                {
                    clientBZ = GameManager.ClientMgr.FindClient(bangHuiDetailData.BZRoleID);
                    if (null != clientBZ)
                    {
                        clientBZ.sendCmd((int)TCPGameServerCmds.CMD_SPR_SERVERUPDATE_ZHANMENGZIJIN, string.Format("{0}:{1}", bhid, bangHuiDetailData.TotalMoney));
                    }
                }

                if (client.ClientData.Faction == bhid)
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SERVERUPDATE_ZHANMENGZIJIN, string.Format("{0}:{1}", bhid, bangHuiDetailData.TotalMoney));
                }
            }
        }

        #endregion 帮会库存铜钱处理

        #region 插帮旗相关

        /// <summary>
        /// 帮旗血变化(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOtherJunQiLifeV(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, int junQiID, int currentLifeV)
        {
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", junQiID, currentLifeV);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGJUNQILIFEV);
        }

        /// <summary>
        /// 帮旗通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        //public void NotifyOthersNewJunQi(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, JunQiItem junQiItem)
        //{
        //    if (null == objsList) return;

        //    //帮旗项到帮旗数据的转换
        //    JunQiData junQiData = Global.JunQiItem2JunQiData(junQiItem);

        //    byte[] bytesData = DataHelper.ObjectToBytes<JunQiData>(junQiData);

        //    //群发消息
        //    SendToClients(sl, pool, null, objsList, bytesData, (int)TCPGameServerCmds.CMD_SPR_NEWJUNQI);
        //}

        /// <summary>
        /// 帮旗通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfNewJunQi(SocketListener sl, TCPOutPacketPool pool, GameClient client, JunQiItem junQiItem)
        {
            //帮旗项到帮旗数据的转换
            JunQiData junQiData = Global.JunQiItem2JunQiData(junQiItem);

            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<JunQiData>(junQiData, pool, (int)TCPGameServerCmds.CMD_SPR_NEWJUNQI);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 帮旗消失通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        //public void NotifyOthersDelJunQi(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, int junQiID)
        //{
        //    if (null == objsList) return;

        //    string strcmd = string.Format("{0}", junQiID);

        //    //群发消息
        //    SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELJUNQI);
        //}

        /// <summary>
        /// 镖车消失通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfDelJunQi(SocketListener sl, TCPOutPacketPool pool, GameClient client, int junQiID)
        {
            string strcmd = string.Format("{0}", junQiID);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELJUNQI);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 领地帮会和税收变更消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyLingDiForBHMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string strcmd)
        {
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_LINGDIFORBH);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知在线的所有人(不限制地图)领地帮会和税收变更消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllLingDiForBHMsg(SocketListener sl, TCPOutPacketPool pool, int lingDiID, int bhid, int zoneID, string bhName, int tax)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", lingDiID, bhid, zoneID, bhName, tax);

            int index = 0;
            GameClient gc = null;
            while ((gc = GetNextClient(ref index)) != null)
            {
                if (gc.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                //领地帮会和税收变更消息
                NotifyLingDiForBHMsg(sl, pool, gc, strcmd);
            }
        }

        /// <summary>
        /// 广播单个帮会领地信息
        /// </summary>
        /// <param name="bangHuiLingDiItemData"></param>
        public void NotifyAllLuoLanChengZhanRequestInfoList(List<LuoLanChengZhanRequestInfoEx> list)
        {
            int index = 0;
            GameClient gc = null;
            while ((gc = GetNextClient(ref index)) != null)
            {
                if (gc.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                NotifyLuoLanChengZhanRequestInfoList(gc, list);
            }
        }

        /// <summary>
        /// 向玩家发送单个领地信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bangHuiLingDiItemData"></param>
        public void NotifyLuoLanChengZhanRequestInfoList(GameClient client, List<LuoLanChengZhanRequestInfoEx> list)
        {
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_GET_LUOLANCHENGZHAN_REQUEST_INFO_LIST, list);
        }

        /// <summary>
        /// 通知在线的指定帮会的所有人帮旗升级了，更新buffer
        /// </summary>
        /// <param name="client"></param>
        public void HandleBHJunQiUpLevel(int bhid, int junQiLevel)
        {
            //更新BufferData
            double[] actionParams = new double[1];
            actionParams[0] = (double)junQiLevel - 1;

            int index = 0;
            GameClient gc = null;
            while ((gc = GetNextClient(ref index)) != null)
            {
                if (gc.ClientData.Faction != bhid)
                {
                    continue;
                }

                Global.UpdateBufferData(gc, BufferItemTypes.JunQi, actionParams, 1);
            }
        }

        #endregion 插帮旗相关

        #region 处理假人相关消息

        /// <summary>
        /// 假人血变化(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOtherFakeRoleLifeV(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, int FakeRoleID, int currentLifeV)
        {
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", FakeRoleID, currentLifeV);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGFAKEROLELIFEV);
        }

        /// <summary>
        /// 假人通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfNewFakeRole(SocketListener sl, TCPOutPacketPool pool, GameClient client, FakeRoleItem FakeRoleItem)
        {
            //假人项到假人数据的转换
            FakeRoleData FakeRoleData = Global.FakeRoleItem2FakeRoleData(FakeRoleItem);

            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<FakeRoleData>(FakeRoleData, pool, (int)TCPGameServerCmds.CMD_SPR_NEWFAKEROLE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 假人消失通知自己(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfDelFakeRole(SocketListener sl, TCPOutPacketPool pool, GameClient client, int FakeRoleID)
        {
            string strcmd = string.Format("{0}", FakeRoleID);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELFAKEROLE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 假人血变化(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllDelFakeRole(SocketListener sl, TCPOutPacketPool pool, FakeRoleItem fakeRoleItem)
        {
            List<Object> objsList = Global.GetAll9Clients(fakeRoleItem);

            string strcmd = string.Format("{0}", fakeRoleItem.FakeRoleID);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELFAKEROLE);
        }

        #endregion 处理假人相关消息

        #region 皇城战相关

        /// <summary>
        /// 皇帝角色ID变更消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyChgHuangDiRoleIDMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string strcmd)
        {
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGHUANGDIROLEID);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知在线的所有人(不限制地图)皇帝角色ID变更消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllChgHuangDiRoleIDMsg(SocketListener sl, TCPOutPacketPool pool, int oldHuangDiRoleID, int huangDiRoleID)
        {
            string strcmd = string.Format("{0}:{1}", oldHuangDiRoleID, huangDiRoleID);

            int index = 0;
            GameClient gc = null;
            while ((gc = GetNextClient(ref index)) != null)
            {
                if (gc.ClientSocket.IsKuaFuLogin)
                {
                    continue;
                }

                //皇帝角色ID变更消息
                NotifyChgHuangDiRoleIDMsg(sl, pool, gc, strcmd);
            }
        }

        #endregion 皇城战相关

        #region 册封和废黜皇后

        /// <summary>
        /// 通知选为皇妃的命令
        /// </summary>
        /// <param name="client"></param>
        public void NotifyInviteAddHuangFei(GameClient client, int otherRoleID, string otherRoleName, int randNum)
        {
            string strcmd = string.Format("{0}:{1}:{2}", otherRoleID, otherRoleName, randNum);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_INVITEADDHUANGFEI);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 皇后状态变更消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyChgHuangHou(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.HuangHou);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGHUANGHOU);
        }

        #endregion 册封和废黜皇后

        #region 领地地图信息数据

        /// <summary>
        /// 领地信息数据通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyLingDiMapInfoData(GameClient client, LingDiMapInfoData lingDiMapInfoData)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<LingDiMapInfoData>(lingDiMapInfoData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_GETLINGDIMAPINFO);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知在线的所有人(不限制地图)领地信息数据通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllLingDiMapInfoData(int mapCode, LingDiMapInfoData lingDiMapInfoData)
        {
            List<Object> objsList = GetMapClients(mapCode);
            if (null == objsList)
            {
                return;
            }

            objsList = Global.ConvertObjsList(mapCode, -1, objsList);

            byte[] bytesData = DataHelper.ObjectToBytes<LingDiMapInfoData>(lingDiMapInfoData);

            //群发消息
            SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, bytesData, (int)TCPGameServerCmds.CMD_SPR_GETLINGDIMAPINFO);
        }

        #endregion 领地地图信息数据

        #region 皇城地图信息数据

        /// <summary>
        /// 皇城信息数据通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyHuangChengMapInfoData(GameClient client, HuangChengMapInfoData huangChengMapInfoData)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<HuangChengMapInfoData>(huangChengMapInfoData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_GETHUANGCHENGMAPINFO);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知在线的所有人(不限制地图)皇城信息数据通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllHuangChengMapInfoData(int mapCode, HuangChengMapInfoData huangChengMapInfoData)
        {
            List<Object> objsList = GetMapClients(mapCode);
            if (null == objsList)
            {
                return;
            }

            objsList = Global.ConvertObjsList(mapCode, -1, objsList);

            byte[] bytesData = DataHelper.ObjectToBytes<HuangChengMapInfoData>(huangChengMapInfoData);

            //群发消息
            SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, bytesData, (int)TCPGameServerCmds.CMD_SPR_GETHUANGCHENGMAPINFO);
        }

        #endregion 皇城地图信息数据

        #region 王城地图信息数据

        /// <summary>
        /// 皇城信息数据通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyWangChengMapInfoData(GameClient client, WangChengMapInfoData wangChengMapInfoData)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<WangChengMapInfoData>(wangChengMapInfoData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_GETWANGCHENGMAPINFO);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知在线的所有人(不限制地图)王城信息数据通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllWangChengMapInfoData(WangChengMapInfoData wangChengMapInfoData)
        {
            /*List<Object> objsList = GetMapClients(mapCode);
            if (null == objsList)
            {
                return;
            }

            objsList = Global.ConvertObjsList(mapCode, -1, objsList);

            byte[] bytesData = DataHelper.ObjectToBytes<WangChengMapInfoData>(wangChengMapInfoData);

            //群发消息
            SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, bytesData, (int)TCPGameServerCmds.CMD_SPR_GETWANGCHENGMAPINFO);*/

            GameClient client = null;
            int index = 0;
            while ((client = GetNextClient(ref index)) != null)
            {
                NotifyWangChengMapInfoData(client, wangChengMapInfoData);
            }
        }

        #endregion 王城地图信息数据

        #region 领地税收

        /// <summary>
        /// 添加帮会领地税收
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddLingDiTaxMoney(int bhid, int lingDiID, int addMoney)
        {
            //先DBServer请求扣费
            string strcmd = string.Format("{0}:{1}:{2}",
                bhid,
                lingDiID,
                addMoney);

            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_SPR_ADDLINGDITAXMONEY, strcmd, GameManager.LocalServerId);
            if (null == dbFields) return false;
            if (dbFields.Length != 4)
            {
                return false;
            }

            // 先锁定
            if (Convert.ToInt32(dbFields[0]) < 0)
            {
                return false;
            }

            return true;
        }

        #endregion 领地税收

        #region 隋唐争霸赛（角斗场）最后经验奖励提示

        /// <summary>
        /// 隋唐争霸赛（角斗场）最后经验奖励提示信息 (只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfSuiTangBattleAward(SocketListener sl, TCPOutPacketPool pool, GameClient client, int nPoint1, int nPoint2, long experience, int bindYuanBao, int chengJiu, bool bIsSuccess, int paiMing, string awardsGoods)
        {
            // 增加提示内容 -- 0.角色roleid 1.是否胜利 2.教团得分 3.联盟等分 4.个人得分 5.个人经验奖励 6.个人成就点奖励 7.魔晶奖励 8.排名 9.物品奖励 [7/23/2014 lt]

            int nSelfPoint = client.ClientData.BattleKilledNum;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}", client.ClientData.RoleID, bIsSuccess, nPoint1, nPoint2, nSelfPoint, experience, chengJiu, bindYuanBao, paiMing, awardsGoods);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYBATTLEAWARD);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion

        #region 邮件相关

        /// <summary>
        /// 通知新邮件
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool NotifyLastUserMail(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, int mailID)
        {
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, mailID);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_RECEIVELASTMAIL);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }

            return true;
        }

        /// <summary>
        /// 一键完成任务时 背包满了--发邮件 [6/24/2014 LiaoWei] 
        /// </summary>
        public void SendMailWhenPacketFull(GameClient client, List<GoodsData> awardsItemList, string sContent, string sSubject)
        {
            int nTotalGroup = 0;
            nTotalGroup = awardsItemList.Count / 5;

            int nRemain = 0;
            nRemain = awardsItemList.Count % 5;

            int nCount = 0;
            if (nTotalGroup > 0)
            {
                for (int i = 0; i < nTotalGroup; ++i)
                {
                    List<GoodsData> goods = new List<GoodsData>();

                    for (int n = 0; n < 5; ++n)
                    {
                        goods.Add(awardsItemList[nCount]);
                        ++nCount;
                    }

                    Global.UseMailGivePlayerAward2(client, goods, sContent, sSubject);
                }
            }

            if (nRemain > 0)
            {
                List<GoodsData> goods1 = new List<GoodsData>();
                for (int i = 0; i < nRemain; ++i)
                {
                    goods1.Add(awardsItemList[nCount]);
                    ++nCount;
                }

                Global.UseMailGivePlayerAward2(client, goods1, sContent, sSubject);
            }
        }

        #endregion 邮件相关

        #region Vip相关
        /// <summary>
        /// 将新的VIP日常数据通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyVipDailyData(GameClient client)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<VipDailyData>>(client.ClientData.VipDailyDataList, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_VIPDAILYDATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }
        #endregion Vip相关

        #region 杨公宝库积分奖励相关
        /// <summary>
        /// 将新的VIP日常数据通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyYangGongBKAwardDailyData(GameClient client)
        {
            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<YangGongBKDailyJiFenData>(client.ClientData.YangGongBKDailyJiFen, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_YANGGONGBKDAILYDATA);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }
        #endregion 杨公宝库积分奖励相关

        #region 生肖运程竞猜相关
        /// <summary>
        /// 通知在线的所有人(限制生肖地图)生肖运程竞猜状态信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllShengXiaoGuessStateMsg(SocketListener sl, TCPOutPacketPool pool, int shengXiaoGuessState, int extraParams, int minLevel, int preGuessResult)
        {
            string strcmd = string.Format("{0}:{1}:{2}", shengXiaoGuessState, extraParams, preGuessResult);

            //先找出当前生肖地图中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(GameManager.ShengXiaoGuessMgr.GuessMapCode);
            if (null == objsList) return;
            TCPOutPacket tcpOutPacket = null;
            try
            {
                for (int i = 0; i < objsList.Count; i++)
                {
                    GameClient client = objsList[i] as GameClient;
                    if (client == null) continue;

                    if (client.ClientData.Level < minLevel) //最低级别要求
                    {
                        continue;
                    }

                    if (null == tcpOutPacket) tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYSHENGXIAOGUESSSTAT);
                    if (!sl.SendData(client.ClientSocket, tcpOutPacket, false))
                    {
                        //
                        /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                            tcpOutPacket.PacketCmdID,
                            tcpOutPacket.PacketDataSize,
                            client.ClientData.RoleID,
                            client.ClientData.RoleName));*/
                    }
                }
            }
            finally
            {
                PushBackTcpOutPacket(tcpOutPacket);
            }
        }

        /// <summary>
        /// 通知玩家生肖运程竞猜结果
        /// </summary>
        /// <param name="client"></param>
        public void NotifyShengXiaoGuessResultMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, string sResult)
        {
            if (null == client)
            {
                return;
            }

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, sResult);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYSHENGXIAOGUESSRESULT);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 通知某个刚刚上线的角色生肖运程竞猜状态信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyClientShengXiaoGuessStateMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, int shengXiaoGuessState, int extraParams, int minLevel, int preGuessResult)
        {
            if (null == client || client.ClientData.Level < minLevel) //最低级别要求
            {
                return;
            }

            string strcmd = string.Format("{0}:{1}:{2}", shengXiaoGuessState, extraParams, preGuessResult);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYSHENGXIAOGUESSSTAT);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 生肖运程竞猜

        #region NPC 相关

        /// <summary>
        /// NPC创建通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfNewNPC(SocketListener sl, TCPOutPacketPool pool, GameClient client, NPC npc)
        {
            if (null == npc || null == npc.RoleBufferData)
            {
                return;
            }

            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, npc.RoleBufferData, 0, npc.RoleBufferData.Length, (int)TCPGameServerCmds.CMD_SPR_NEWNPC);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// NPC创建通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfNewNPCBy9Grid(SocketListener sl, TCPOutPacketPool pool, NPC npc)
        {
            if (null == npc || null == npc.RoleBufferData)
            {
                return;
            }
#if TestGrid2
            List<Object> objsList = Global.GetAll9GridGameClient(npc);
#else
            List<Object> objsList = Global.GetAll9GridObjects(npc);
#endif
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                NotifyMySelfNewNPC(sl, pool, objsList[i] as GameClient, npc);
            }
        }

        /// <summary>
        /// NPC移除通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfDelNPC(SocketListener sl, TCPOutPacketPool pool, GameClient client, int mapCode, int npcID)
        {
            string strcmd = string.Format("{0}:{1}", npcID, mapCode);

            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELNPC);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// NPC移除通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfDelNPC(SocketListener sl, TCPOutPacketPool pool, GameClient client, NPC npc)
        {
            NotifyMySelfDelNPC(sl, pool, client, npc.MapCode, npc.NpcID);
        }

        /// <summary>
        /// 通知指定npc附近的角色npc删除
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfDelNPCBy9Grid(SocketListener sl, TCPOutPacketPool pool, NPC npc)
        {
#if TestGrid2
            List<Object> objsList = Global.GetAll9GridGameClient(npc);
#else
            List<Object> objsList = Global.GetAll9GridObjects(npc);
#endif
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GameClient))
                {
                    continue;
                }

                NotifyMySelfDelNPC(sl, pool, objsList[i] as GameClient, npc.MapCode, npc.NpcID);
            }
        }

        #endregion NPC相关

        #region 角色故事版移动处理

        //Unix秒的起始计算毫秒时间(相对系统时间)
        public const long Before1970Ticks = 62135625600000;

        /// <summary>
        /// 尝试模仿怪物直接移动
        /// </summary>
        /// <param name="client"></param>
        /// <param name="startMoveTicks"></param>
        private bool TryDirectMove(GameClient client, long startMoveTicks, List<Point> path)
        {
            int endGridX = (int)path[path.Count - 1].X;
            int endGridY = (int)path[path.Count - 1].Y;

            if (Global.GetTwoPointDistance(client.CurrentGrid, new Point(endGridX, endGridY)) >= 3.0)
            {
                return false;
            }

            if (path.Count > 2)
            {
                return false;
            }

            for (int i = 0; i < path.Count; i++)
            {
                Point clientGrid = client.CurrentGrid;

                MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode];
                int gridX = (int)path[i].X;
                int gridY = (int)path[i].Y;

                //已经在位置上了，则跳过
                if (gridX == (int)clientGrid.X && gridY == (int)clientGrid.Y)
                {
                    continue;
                }

                //服务器端不判断障碍(根据俊武的建议，应该加入判断，否则客户端会使用外挂抛入障碍物中)
                if (Global.InObsByGridXY(ObjectTypes.OT_CLIENT, client.ClientData.MapCode, (int)gridX, gridY, 0))
                {
                    int direction = client.ClientData.RoleDirection;
                    int tryRun = 0;

                    //通知其他人自己开始移动
                    GameManager.ClientMgr.NotifyOthersMyMovingEnd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, client.ClientData.MapCode, (int)GActions.Stand, client.ClientData.PosX, client.ClientData.PosY, direction, tryRun, true);

                    //LogManager.WriteLog(LogTypes.Error, string.Format("TryDirectMove, send moveend, roleName={0}", client.ClientData.RoleName));

                    break;
                }

                int toX = gridX * mapGrid.MapGridWidth + mapGrid.MapGridWidth / 2;
                int toY = gridY * mapGrid.MapGridHeight + mapGrid.MapGridHeight / 2;

                //确保在中心位置
                client.ClientData.PosX = toX;
                client.ClientData.PosY = toY;

                //立即处理格子的移动
                mapGrid.MoveObject(-1, -1, toX, toY, client);

                /// 当前正在做的动作
                client.ClientData.CurrentAction = (int)GActions.Stand;

                //System.Diagnostics.Debug.WriteLine(string.Format("TryDirectMove, toX={0}, toY={1}", toX, toY));
            }

            return true;
        }

        /// <summary>
        /// 角色开始故事版的移动
        /// </summary>
        /// <param name="client"></param>
        /// <param name="startMoveTicks"></param>
        /// <param name="pathString"></param>
        public void StartClientStoryboard(GameClient client, long startMoveTicks)
        {
            StoryBoard4Client.RemoveStoryBoard(client.ClientData.RoleID);

            string unZipPathString = DataHelper.UnZipStringToBase64(client.ClientData.RolePathString);
            //System.Diagnostics.Debug.WriteLine(string.Format("解开压缩后，压缩比原始小: {0}, 压缩比例: {1}%", unZipPathString.Length - client.ClientData.RolePathString.Length, client.ClientData.RolePathString.Length * 100 / unZipPathString.Length));

            List<Point> path = Global.TransStringToPathArr(unZipPathString);
            if (path.Count <= 1) //后边会删掉一个
            {
                return;
            }

            path.RemoveAt(0); //删除第一个格子，因为无必要

            //尝试模仿怪物直接移动
            if (TryDirectMove(client, startMoveTicks, path))
            {
                /// 玩家进行了移动
                if (GameManager.Update9GridUsingNewMode <= 0)
                {
                    ClientManager.DoSpriteMapGridMove(client);
                }

                return;
            }

            //只有自动寻路才使用服务器端故事版算法

            StoryBoard4Client sb = new StoryBoard4Client(client.ClientData.RoleID);
            sb.Completed = Move_Completed;

            GameMap gameMap = GameManager.MapMgr.DictMaps[client.ClientData.MapCode];

            long ticks = TimeUtil.NOW() * 10000 - (Before1970Ticks * 10000);
            ticks /= 10000;

            startMoveTicks -= Before1970Ticks;

            //long elapsedTicks = ticks - startMoveTicks; //就是这里导致了有些客户端会自动寻路时导致周围角色和怪物隐身
            long elapsedTicks = 0;
            sb.Start(client, path, gameMap.MapGridWidth, gameMap.MapGridHeight, elapsedTicks);

            sb.Binding(); //先开始，后绑定
        }

        /// <summary>
        /// 移动结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Move_Completed(object sender, EventArgs e)
        {
            StoryBoard4Client sb = sender as StoryBoard4Client;
            StoryBoard4Client.RemoveStoryBoard(sb.RoleID);

            //如果是遇到障碍停止，才通知客户端
            if (sb.IsStopped())
            {
                GameClient client = GameManager.ClientMgr.FindClient(sb.RoleID);
                if (null != client)
                {
                    GameMap gameMap = GameManager.MapMgr.DictMaps[client.ClientData.MapCode];
                    int toX = gameMap.CorrectWidthPointToGridPoint(client.ClientData.PosX);
                    int toY = gameMap.CorrectHeightPointToGridPoint(client.ClientData.PosY);

                    //确保在中心位置
                    client.ClientData.PosX = toX;
                    client.ClientData.PosY = toY;

                    /// 当前正在做的动作
                    client.ClientData.CurrentAction = (int)GActions.Stand;

                    int direction = client.ClientData.RoleDirection;
                    int tryRun = 1;

                    //通知其他人自己开始移动
                    GameManager.ClientMgr.NotifyOthersMyMovingEnd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, client.ClientData.MapCode, (int)GActions.Stand, toX, toY, direction, tryRun, true);

                    /// 玩家进行了移动
                    if (GameManager.Update9GridUsingNewMode <= 0)
                    {
                        ClientManager.DoSpriteMapGridMove(client);
                    }
                }
            }
            else
            {
                GameClient client = GameManager.ClientMgr.FindClient(sb.RoleID);
                if (null != client)
                {
                    /// 当前正在做的动作
                    client.ClientData.CurrentAction = (int)GActions.Stand;

                    /// 移动的速度
                    client.ClientData.MoveSpeed = 1.0;

                    /// 移动的目的地坐标点
                    client.ClientData.DestPoint = new Point(client.ClientData.PosX, client.ClientData.PosY);
                    //System.Diagnostics.Debug.WriteLine(string.Format("EndStoryboard, PosX={0}, PosY={1}", client.ClientData.PosX, client.ClientData.PosY));

                    /// 玩家进行了移动
                    if (GameManager.Update9GridUsingNewMode <= 0)
                    {
                        ClientManager.DoSpriteMapGridMove(client);
                    }
                }
            }
        }

        /// <summary>
        /// 角色停止故事版的移动
        /// </summary>
        /// <param name="client"></param>
        /// <param name="startMoveTicks"></param>
        /// <param name="pathString"></param>
        public void StopClientStoryboard(GameClient client, int stopIndex = -1)
        {
            if (stopIndex > 0)
            {
                StoryBoard4Client.StopStoryBoard(client.ClientData.RoleID, stopIndex);
            }
            else
            {
                StoryBoard4Client.RemoveStoryBoard(client.ClientData.RoleID);
            }
        }

        /// <summary>
        /// 获取角色故事版的最终位置
        /// </summary>
        /// <param name="client"></param>
        /// <param name="startMoveTicks"></param>
        /// <param name="pathString"></param>
        public bool GetClientStoryboardLastPoint(GameClient client, out Point lastPoint)
        {
            lastPoint = new Point(0, 0);
            StoryBoard4Client sb = StoryBoard4Client.FindStoryBoard(client.ClientData.RoleID);
            if (null != sb)
            {
                lastPoint = sb.LastPoint;
                return true;
            }

            return false;
        }

        #endregion 角色故事板移动处理

        #region 装备耐久度管理

        /// <summary>
        /// 增加指定装备的耐久度
        /// </summary>
        /// <param name="goodsData"></param>
        /// <param name="subStrong"></param>
        public bool AddEquipStrong(GameClient client, GoodsData goodsData, int subStrong)
        {
            //获取指定物品的最大耐久度
            int maxStrong = Global.GetEquipGoodsMaxStrong(goodsData.GoodsID);
            if (goodsData.Strong >= maxStrong)
            {
                return false;
            }

            int oldStrong = goodsData.Strong;
            int modValue1 = goodsData.Strong / Global.MaxNotifyEquipStrongValue;

            //此处的多线程操作可以忽略不计
            goodsData.Strong = Math.Min(goodsData.Strong + subStrong, maxStrong); //不允许超过上限

            int modValue2 = goodsData.Strong / Global.MaxNotifyEquipStrongValue;

            bool hasNotifyClient = false;

            //这样计算，防止频繁通知客户端
            if (modValue1 != modValue2)
            {
                //攻击线程不直接写数据库,因为短时间可能会处理很多个更新耐久的需求,但写数据库的延时不可预知
                if (GameManager.FlagOptimizeAlgorithm_Props)
                {
                    if (goodsData.Strong < maxStrong)
                    {
                        //设置2个小时后自动提交
                        Global.SetLastDBEquipStrongCmdTicks(client, goodsData.Id, TimeUtil.NOW(), false);
                    }
                    else
                    {
                        Global.SetLastDBEquipStrongCmdTicks(client, goodsData.Id, TimeUtil.NOW() - Global.MaxDBEquipStrongCmdSlot, false);
                    }
                }
                else
                {
                    //写入到数据库中
                    Global.UpdateEquipStrong(client, goodsData);
                }

                //通知客户端
                //装备耐久度变化通知
                NotifyMySelfEquipStrong(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, goodsData);

                hasNotifyClient = true;
            }
            else
            {
                //设置2个小时后自动提交
                Global.SetLastDBEquipStrongCmdTicks(client, goodsData.Id, TimeUtil.NOW(), false);
            }

            //物品因为耐久要失效
            if (oldStrong < maxStrong && goodsData.Strong >= maxStrong) //防止重复的操作
            {
                if (!hasNotifyClient)
                {
                    //写入到数据库中
                    Global.UpdateEquipStrong(client, goodsData);

                    //通知客户端
                    //装备耐久度变化通知
                    NotifyMySelfEquipStrong(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, goodsData);
                }

                Global.RefreshEquipPropAndNotify(client);
            }

            return true;
        }

        /// <summary>
        /// 减少指定装备的耐久度
        /// </summary>
        /// <param name="goodsData"></param>
        /// <param name="subStrong"></param>
        public int SubEquipStrong(GameClient client, GoodsData goodsData, int subStrong)
        {
            int modValue1 = goodsData.Strong / Global.MaxNotifyEquipStrongValue;

            //此处的多线程操作可以忽略不计
            goodsData.Strong = Math.Max(0, goodsData.Strong - subStrong);

            int modValue2 = goodsData.Strong / Global.MaxNotifyEquipStrongValue;

            //这样计算，防止频繁通知客户端
            if (modValue1 != modValue2)
            {
                //写入到数据库中
                Global.UpdateEquipStrong(client, goodsData);

                //通知客户端
                //装备耐久度变化通知
                NotifyMySelfEquipStrong(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, goodsData);
            }
            else
            {
                //设置2个小时后自动提交
                Global.SetLastDBEquipStrongCmdTicks(client, goodsData.Id, TimeUtil.NOW(), false);
            }

            return modValue2;
        }

        /// <summary>
        /// 装备耐久度变化通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfEquipStrong(SocketListener sl, TCPOutPacketPool pool, GameClient client, GoodsData goodsData)
        {
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, goodsData.Id, goodsData.Strong);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_NOTIFYEQUIPSTRONG);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 装备耐久度管理

        #region 道术隐身命令

        /// <summary>
        /// 发送道术隐身命令
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDSHideCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.DSHideStart);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_DSHIDECMD);
        }

        /// <summary>
        /// 定时检查道术隐身装备
        /// </summary>
        /// <param name="client"></param>
        public void CheckDSHideState(GameClient client)
        {
            if (client.ClientData.DSHideStart <= 0)
            {
                return;
            }

            long nowTicks = TimeUtil.NOW();
            if (nowTicks < client.ClientData.DSHideStart)
            {
                return;
            }

            Global.RemoveBufferData(client, (int)BufferItemTypes.DSTimeHideNoShow);
            client.ClientData.DSHideStart = 0;
            GameManager.ClientMgr.NotifyDSHideCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
        }

        #endregion 道术隐身命令

        #region 法师的护盾和被道士下毒的相关命令

        /// <summary>
        /// 发送角色状态相关的命令
        /// </summary>
        /// <param name="client"></param>
        public void NotifyRoleStatusCmd(SocketListener sl, TCPOutPacketPool pool, GameClient client, int statusID, long startTicks, int slotSeconds, double tag = 0.0)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, statusID, startTicks, slotSeconds, tag);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_ROLESTATUSCMD);
        }

        /// <summary>
        /// 发送怪物状态相关的命令
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMonsterStatusCmd(SocketListener sl, TCPOutPacketPool pool, Monster monster, int statusID, long startTicks, int slotSeconds, double tag = 0.0)
        {
            List<Object> objsList = Global.GetAll9Clients(monster);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", monster.RoleID, statusID, startTicks, slotSeconds, tag);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_ROLESTATUSCMD);
        }

        #endregion 法师的护盾和被道士下毒的相关命令

        #region 地图特效 相关

        /// <summary>
        /// Deco创建通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfNewDeco(SocketListener sl, TCPOutPacketPool pool, GameClient client, Decoration deco)
        {
            if (null == deco)
            {
                return;
            }

            DecorationData decoData = new DecorationData()
            {
                AutoID = deco.AutoID,
                DecoID = deco.DecoID,
                MapCode = deco.MapCode,
                PosX = (int)deco.Pos.X,
                PosY = (int)deco.Pos.Y,
                StartTicks = deco.StartTicks,
                MaxLiveTicks = deco.MaxLiveTicks,
                AlphaTicks = deco.AlphaTicks,
            };

            TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<DecorationData>(decoData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_NEWDECO);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// Decoration移除通知
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMySelfDelDeco(SocketListener sl, TCPOutPacketPool pool, GameClient client, Decoration deco)
        {
            if (null == deco)
            {
                return;
            }

            string strcmd = string.Format("{0}", deco.AutoID);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELDECO);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 特效消失通知(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersDelDeco(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, int mapCode, int autoID)
        {
            if (null == objsList) return;

            string strcmd = string.Format("{0}", autoID);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_DELDECO);
        }

        #endregion 地图特效 相关

        #region 角色基础参数读写 装备积分 猎杀值 悟性值 真气值 天地精元值 试炼令[===>通天令值]值 经脉等级 武学等级 军功值, +角色在线奖励天ID

        /*
         * 悟性值的修改，会触发武学等级的变化，武学等级的变化，会触发武学buffer的变化
         * 成就值的修改，【会触发成就隐含等级的变化，这个等级的变化，表现为成就buffer的变化】
         * 角色每次登录时，也会判断这几个buffer是否应该切换，此外，每个buffer是24小时消失，
         * buffer消失的时候，也需要判断一下，buffer是否需要再次激活
         */
        /// <summary>
        /// 修改成就点 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyChengJiuPointsValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            client.ClientData.ChengJiuPoints += addValue;
            client.ClientData.ChengJiuPoints = Math.Max(client.ClientData.ChengJiuPoints, 0);
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "成就", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, client.ClientData.ChengJiuPoints, client.ServerId);

            // 更新到数据库
            // 成就变动时，强制写到数据库 ChenXiaojun
            ChengJiuManager.ModifyChengJiuExtraData(client, (uint)client.ClientData.ChengJiuPoints, ChengJiuExtraDataField.ChengJiuPoints, true);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.ChengJiu, addValue, client.ClientData.ChengJiuPoints, strFrom);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ChengJiu, client.ClientData.ChengJiuPoints);
            }

            client._IconStateMgr.CheckChengJiuUpLevelState(client);
        }

        /// <summary>
        /// 读取成就点
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetChengJiuPointsValue(GameClient client)
        {
            client.ClientData.ChengJiuPoints = (int)ChengJiuManager.GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.ChengJiuPoints);

            return client.ClientData.ChengJiuPoints;
        }

        /// <summary>
        /// 修改成就等级
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int SetChengJiuLevelValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            client.ClientData.ChengJiuLevel = ChengJiuManager.GetChengJiuLevel(client);
            client.ClientData.ChengJiuLevel += addValue;

            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "成就等级", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, client.ClientData.ChengJiuLevel, client.ServerId);
            ChengJiuManager.SetChengJiuLevel(client, client.ClientData.ChengJiuLevel, true);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ChengJiuLevel, client.ClientData.ChengJiuLevel);
            }

            return client.ClientData.ChengJiuLevel;
        }

        /// <summary>
        /// 修改装备积分 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyZhuangBeiJiFenValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetZhuangBeiJiFenValue(client) + addValue;

            //更新到数据库
            SaveZhuangBeiJiFenValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ZhuangBeiJiFen, newValue);
            }
        }

        /// <summary>
        /// 保存装备积分
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveZhuangBeiJiFenValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ZhuangBeiJiFen, nValue, writeToDB);
        }

        /// <summary>
        /// 读取装备积分
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetZhuangBeiJiFenValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.ZhuangBeiJiFen);
        }

        /// <summary>
        /// 修改猎杀值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyLieShaValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetLieShaValue(client) + addValue;

            //更新到数据库
            SaveLieShaValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.LieShaZhi, newValue);
            }
        }

        /// <summary>
        /// 保存猎杀值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveLieShaValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.LieShaZhi, nValue, writeToDB);
        }

        /// <summary>
        /// 读取猎杀值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetLieShaValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.LieShaZhi);
        }

        /// <summary>
        /// 修改悟性值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyWuXingValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true, bool doChangeWuXueLevel = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetWuXingValue(client) + addValue;

            //更新到数据库
            SaveWuXingValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.WuXingZhi, newValue);
            }

            //如果悟性值增加，则进行下一武学等级激活的判断，有的武学等级是不需要消耗任何物品直接激活的,当悟性值不够的时候，武学等级会下降
            if (doChangeWuXueLevel)
            {
                if (addValue > 0)
                {
                    Global.TryToActivateSpecialWuXueLevel(client);
                }
                else
                {
                    Global.TryToDeActivateSpecialWuXueLevel(client);
                }
            }
        }

        /// <summary>
        /// 保存悟性值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveWuXingValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.WuXingZhi, nValue, writeToDB);
        }

        /// <summary>
        /// 读取悟性值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetWuXingValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.WuXingZhi);
        }

        /// <summary>
        /// 修改真气值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyZhenQiValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetZhenQiValue(client) + addValue;

            //更新到数据库
            SaveZhenQiValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ZhenQiZhi, newValue);
            }
        }

        /// <summary>
        /// 保存真气值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveZhenQiValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ZhenQiZhi, nValue, writeToDB);
        }

        /// <summary>
        /// 读取真气值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetZhenQiValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.ZhenQiZhi);
        }

        /// <summary>
        /// 修改星魂值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyStarSoulValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            client.ClientData.StarSoul += addValue;
            if (client.ClientData.StarSoul < 0)
                client.ClientData.StarSoul = 0;

            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "星魂", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, client.ClientData.StarSoul, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.XingHun, addValue, client.ClientData.StarSoul, strFrom);

            //更新到数据库
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.StarSoul, client.ClientData.StarSoul, true);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.StarSoulValue, client.ClientData.StarSoul);
            }
        }

        /// <summary>
        /// 修改精灵积分 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyPetJiFenValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int nPetJiFen = Convert.ToInt32(Global.GetRoleParamByName(client, RoleParamName.PetJiFen)) + addValue;

            //更新到数据库
            Global.UpdateRoleParamByName(client, RoleParamName.PetJiFen, nPetJiFen.ToString(), true);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.PetJiFen, nPetJiFen);
            }
            // 日志
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "精灵积分", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, nPetJiFen, client.ServerId);
        }

        /// <summary>
        /// 修改元素粉末值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyYuanSuFenMoValue(GameClient client, int addValue, string strFrom, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int currPowder = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementPowderCount);
            int newPowder = currPowder + addValue;
            // if (newPowder < 0)
            //    newPowder = 0;

            if (newPowder == currPowder)
                return;

            addValue = newPowder - currPowder;

            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "元素粉末", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, newPowder, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.YuanSuFenMo, addValue, newPowder, strFrom);

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ElementPowderCount, newPowder, true);

            if (notifyClient)
            {
                GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.YuansuFenmo, newPowder);
            }
        }

        /// <summary>
        /// 修改灵晶值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyMUMoHeValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetMUMoHeValue(client) + addValue;
            // newValue = Math.Max(0, newValue);
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "魔核", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, newValue, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.MUMoHe, addValue, newValue, strFrom);

            //更新到数据库
            SaveMUMoHeValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.MUMoHe, newValue);
            }
        }

        /// <summary>
        /// 保存天地精元值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveMUMoHeValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.MUMoHe, nValue, writeToDB);
        }

        /// <summary>
        /// 读取魔核值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetMUMoHeValue(GameClient client)
        {
            return Global.GetRoleParamsInt32FromDB(client, RoleParamName.MUMoHe);
        }

        /// <summary>
        /// 修改天地精元值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyTianDiJingYuanValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            long oldValue = GetTianDiJingYuanValue(client);
            long targetValue = oldValue + addValue;
            int newValue = targetValue > int.MaxValue ? int.MaxValue : (int)targetValue;

            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "魔晶", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, newValue, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.JingYuanZhi, addValue, newValue, strFrom);

            //更新到数据库
            SaveTianDiJingYuanValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.TianDiJingYuan, newValue);
            }

            // 七日活动
            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.MoJingCntInBag));
        }

        /// <summary>
        /// 保存天地精元值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveTianDiJingYuanValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.TianDiJingYuan, nValue, writeToDB);
        }

        /// <summary>
        /// 读取天地精元值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetTianDiJingYuanValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.TianDiJingYuan);
        }


        #region 再造点

        /// <summary>
        /// 再造点——修改 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyZaiZaoValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
                return;

            int newValue = GetZaiZaoValue(client) + addValue;
            // newValue = Math.Max(newValue, 0);
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "再造点", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.ZaiZao, addValue, newValue, strFrom);

            //更新到数据库
            SaveZaiZaoValue(client, newValue, writeToDB);

            if (notifyClient)
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ZaiZaoPoint, newValue);
        }

        /// <summary>
        /// 再造点——保存
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveZaiZaoValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ZaiZaoPoint, nValue, writeToDB);
        }

        /// <summary>
        /// 再造点——读取
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetZaiZaoValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.ZaiZaoPoint);
        }

        #endregion

        /// <summary>
        /// 修改试炼令值 addValue > 0,增加，小于0，减少 ===>通天令值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyShiLianLingValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetShiLianLingValue(client) + addValue;

            //更新到数据库
            SaveShiLianLingValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ShiLianLing, newValue);
            }
        }

        /// <summary>
        /// 保存试炼令值===>通天令值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveShiLianLingValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ShiLianLing, nValue, writeToDB);
        }

        /// <summary>
        /// 读取试炼令值===>通天令值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetShiLianLingValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.ShiLianLing);
        }

        /// <summary>
        /// 修改经脉等级值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        /*public void ModifyJingMaiLevelValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetJingMaiLevelValue(client) + addValue;

            //更新到数据库
            SaveJingMaiLevelValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.JingMaiLevel, newValue);
            }

            //经脉等级变化 激活新的经脉buffer 这个是永久buffer
            Global.ActiveJinMaiBuffer(client, true);

            //处理经脉成就 如果gm命令，可能会减少经脉等级，减少就不取消成就了
            if (addValue > 0)
            {
                ChengJiuManager.OnJingMaiLevelUp(client, newValue);
            }
        }*/

        /// <summary>
        /// 保存经脉等级值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveJingMaiLevelValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.JingMaiLevel, nValue, writeToDB);
        }

        /// <summary>
        /// 读取经脉等级值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetJingMaiLevelValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.JingMaiLevel);
        }

        /// <summary>
        /// 修改武学等级值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        /*public void ModifyWuXueLevelValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetWuXueLevelValue(client) + addValue;

            //更新到数据库
            SaveWuXueLevelValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.WuXueLevel, newValue);
            }

            //如果武学等级变化，武学等级可能下降，尝试激活新的武学buffer
            Global.TryToActiveNewWuXueBuffer(client, true);

            //处理武学成就 如果gm命令，可能会减少武学等级，减少就不取消成就了
            if (addValue > 0)
            {
                ChengJiuManager.OnWuXueLevelUp(client, newValue);
            }
        }*/

        /// <summary>
        /// 保存武学等级值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveWuXueLevelValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.WuXueLevel, nValue, writeToDB);
        }

        /// <summary>
        /// 读取武学等级值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetWuXueLevelValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.WuXueLevel);
        }

        /// <summary>
        /// 修改钻皇等级值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyZuanHuangLevelValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetZuanHuangLevelValue(client) + addValue;

            //更新到数据库
            SaveZuanHuangLevelValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ZuanHuangLevel, newValue);
            }
        }

        /// <summary>
        /// 保存钻皇等级值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveZuanHuangLevelValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ZuanHuangLevel, nValue, writeToDB);
        }

        /// <summary>
        /// 读取钻皇等级值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetZuanHuangLevelValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.ZuanHuangLevel);
        }

        /// <summary>
        /// addValue 是激活项索引
        /// 修改系统激活项值 addValue 必须大于等于0，且小于等于31
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifySystemOpenValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 非法参数不进行任何处理
            if (addValue < 0 || addValue > 31)
            {
                return;
            }

            int newValue = GetSystemOpenValue(client) | (int)(1 << addValue);

            //更新到数据库
            SaveSystemOpenValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.SystemOpenValue, newValue);
            }
        }

        /// <summary>
        /// 保存系统激活项值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveSystemOpenValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.SystemOpenValue, nValue, writeToDB);
        }

        /// <summary>
        /// 读取系统激活项值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetSystemOpenValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.SystemOpenValue);
        }

        /// <summary>
        /// 修改军功值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyJunGongValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetJunGongValue(client) + addValue;

            //更新到数据库
            SaveJunGongValue(client, newValue, writeToDB);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.JunGongZhi, addValue, newValue, "none");

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.JunGong, newValue);
            }
        }

        /// <summary>
        /// 保存军功值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveJunGongValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.JunGong, nValue, writeToDB);
        }

        /// <summary>
        /// 读取军功值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetJunGongValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.JunGong);
        }

        /// <summary>
        /// dayID 是激活项索引
        /// 修改DayID 必须大于等于1，且小于等于7
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyKaiFuOnlineDayID(GameClient client, int dayID, bool writeToDB = false, bool notifyClient = true)
        {
            //对 非法参数不进行任何处理
            if (dayID < 1 || dayID > 7)
            {
                return;
            }

            //更新到数据库
            SaveKaiFuOnlineDayID(client, dayID, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.KaiFuOnlineDayID, dayID);
            }
        }

        /// <summary>
        /// 保存开服在线奖励DayID
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveKaiFuOnlineDayID(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.KaiFuOnlineDayID, nValue, writeToDB);
        }

        /// <summary>
        /// 读取开服在线奖励DayID
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetKaiFuOnlineDayID(GameClient client)
        {
            return Global.GetRoleParamsInt32FromDB(client, RoleParamName.KaiFuOnlineDayID);
        }

        /// <summary>
        /// ID 是记忆索引
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyTo60or100ID(GameClient client, int nID, bool writeToDB = false, bool notifyClient = true)
        {
            //更新到数据库
            SaveTo60or100ID(client, nID, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.To60or100, nID);
            }
        }

        /// <summary>
        /// 保存开服在线奖励DayID
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveTo60or100ID(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.To60or100, nValue, writeToDB);
        }

        /// <summary>
        /// 读取开服在线奖励DayID
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetTo60or100ID(GameClient client)
        {
            return Global.GetRoleParamsInt32FromDB(client, RoleParamName.To60or100);
        }

        #region 藏宝秘境
        /// <summary>
        /// 修改藏宝积分 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyTreasureJiFenValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
                return;

            int newValue = GetTreasureJiFen(client) + addValue;

            //更新到数据库
            SaveTreasureJiFenValue(client, newValue, writeToDB);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.BaoZangJiFen, addValue, newValue, "none");

            if (notifyClient)                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.TreasureJiFen, newValue);
        }

        public void ModifyTreasureXueZuanValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
                return;

            int newValue = GetTreasureXueZuan(client) + addValue;

            //更新到数据库
            SaveTreasureXueZuanValue(client, newValue, writeToDB);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.BaoZangXueZuan, addValue, newValue, "none");

            if (notifyClient)                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.TreasureXueZuan, newValue);
        }

        /// <summary>
        /// 获取一个角色的藏宝积分
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetTreasureJiFen(GameClient client)
        {
            return Global.GetRoleParamsInt32FromDB(client, RoleParamName.TreasureJiFen);
        }

        /// <summary>
        /// 保存一个角色的藏宝积分
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public void SaveTreasureJiFenValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TreasureJiFen, nValue, writeToDB);
        }

        /// <summary>
        /// 获取一个角色的藏宝血钻
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetTreasureXueZuan(GameClient client)
        {
            return Global.GetRoleParamsInt32FromDB(client, RoleParamName.TreasureXueZuan);
        }

        /// <summary>
        /// 保存一个角色的藏宝血钻
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public void SaveTreasureXueZuanValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TreasureXueZuan, nValue, writeToDB);
        }

        #endregion

        /// <summary>
        /// 修改战魂 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyZhanHunValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetZhanHunValue(client) + addValue;

            //更新到数据库
            SaveZhanHunValue(client, newValue, writeToDB);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.ZhanHun, addValue, newValue, "none");

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ZhanHun, newValue);
            }
        }

        /// <summary>
        /// 修改天梯荣耀 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public bool ModifyTianTiRongYaoValue(GameClient client, int addValue, string strFrom, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 != addValue)
            {
                client.ClientData.TianTiData.RongYao += addValue;
                RoleAttributeValueData roleAttributeValueData = new RoleAttributeValueData()
                        {
                            RoleAttribyteType = (int)RoleAttribyteTypes.RongYao,
                            Targetvalue = client.ClientData.TianTiData.RongYao,
                            AddVAlue = addValue,
                        };

                Global.sendToDB<int, int[]>((int)TCPGameServerCmds.CMD_DB_TIANTI_UPDATE_RONGYAO, new int[] { client.ClientData.RoleID, client.ClientData.TianTiData.RongYao }, client.ServerId);

                // 日志
                GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荣耀", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, client.ClientData.TianTiData.RongYao, client.ServerId);
                EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.TianTiRongYao, addValue, client.ClientData.TianTiData.RongYao, strFrom);

                if (notifyClient)
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ROLE_ATTRIBUTE_VALUE, roleAttributeValueData);
                }
            }

            return true;
        }

        /// <summary>
        /// 保存战魂
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveZhanHunValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ZhanHun, nValue, writeToDB);
        }

        /// <summary>
        /// 读取战魂
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetZhanHunValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.ZhanHun);
        }

        /// <summary>
        /// 修改荣誉 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyRongYuValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetRongYuValue(client) + addValue;

            //更新到数据库
            SaveRongYuValue(client, newValue, writeToDB);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.RongYu, addValue, newValue, "none");

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.RongYu, newValue);
            }
        }

        /// <summary>
        /// 保存荣誉
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveRongYuValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.RongYu, nValue, writeToDB);
        }

        /// <summary>
        /// 读取荣誉
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetRongYuValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.RongYu);
        }

        /// <summary>
        /// 修改战魂等级值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyZhanHunLevelValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetZhanHunLevelValue(client) + addValue;

            //更新到数据库
            SaveZhanHunLevelValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ZhanHunLevel, newValue);
            }

            //战魂等级变化 激活新的战魂buffer 这个是永久buffer
            Global.ActiveZhanHunBuffer(client, true);
        }

        /// <summary>
        /// 保存战魂等级值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveZhanHunLevelValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ZhanHunLevel, nValue, writeToDB);
        }

        /// <summary>
        /// 读取战魂等级值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetZhanHunLevelValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.ZhanHunLevel);
        }

        /// <summary>
        /// 修改荣誉等级值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyRongYuLevelValue(GameClient client, int addValue, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetRongYuLevelValue(client) + addValue;

            //更新到数据库
            SaveRongYuLevelValue(client, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.RongYuLevel, newValue);
            }
        }

        /// <summary>
        /// 保存荣誉等级值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveRongYuLevelValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.RongYuLevel, nValue, writeToDB);
        }

        /// <summary>
        /// 读取荣誉等级值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetRongYuLevelValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.RongYuLevel);
        }

        /// <summary>
        /// 修改声望 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyShengWangValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetShengWangValue(client) + addValue;
            //newValue = Math.Max(newValue, 0);
            // 更新到数据库
            // 声望改变时，强制写到数据 ChenXiaojun
            SaveShengWangValue(client, newValue, true);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ShengWang, newValue);
            }

            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "声望", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, newValue, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.ShengWang, addValue, newValue, strFrom);

            // 增加竞技场声望时，刷新图标状态
            if (addValue > 0)
            {
                client._IconStateMgr.CheckJingJiChangJunXian(client);
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        /// <summary>
        /// 修改狼魂粉末 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyLangHunFenMoValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            if (client == null) return;

            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            long lNewValue = (long)GetLangHunFenMoValue(client) + addValue;
            lNewValue = Math.Min(lNewValue, int.MaxValue);

            int newValue = (int)lNewValue;
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LangHunFenMo, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.LangHunFenMo, newValue);
            }

            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "狼魂粉末", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, newValue, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.LangHunFenMo, addValue, newValue, strFrom);
        }

        /// <summary>
        /// 获取狼魂粉末值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetLangHunFenMoValue(GameClient client)
        {
            return Global.GetRoleParamsInt32FromDB(client, RoleParamName.LangHunFenMo);
        }

        /// <summary>
        /// 修改王者点数 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyKingOfBattlePointValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            if (client == null) return;

            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            long lNewValue = (long)GetKingOfBattlePointValue(client) + addValue;
            lNewValue = Math.Min(lNewValue, int.MaxValue);

            int newValue = (int)lNewValue;
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.KingOfBattlePoint, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.KingOfBattlePoint, newValue);
            }

            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "王者争霸点数", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, newValue, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.KingOfBattlePoint, addValue, newValue, strFrom);
        }

        /// <summary>
        /// 获取王者争霸点数
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetKingOfBattlePointValue(GameClient client)
        {
            return Global.GetRoleParamsInt32FromDB(client, RoleParamName.KingOfBattlePoint);
        }

        /// <summary>
        /// 修改狼魂粉末 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyZhengBaPointValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            if (client == null) return;

            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            long lNewValue = (long)GetZhengBaPointValue(client) + addValue;
            lNewValue = Math.Min(lNewValue, int.MaxValue);

            int newValue = (int)lNewValue;
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ZhengBaPoint, newValue, writeToDB);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ZhengBaPoint, newValue);
            }

            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "争霸点", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, newValue, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.ZhengBaPoint, addValue, newValue, strFrom);
        }

        /// <summary>
        /// 获取狼魂粉末值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetZhengBaPointValue(GameClient client)
        {
            return Global.GetRoleParamsInt32FromDB(client, RoleParamName.ZhengBaPoint);
        }

        #region 万魔塔通关层数
        /// <summary>
        /// 保存万魔塔通关层数
        /// </summary>
        public void SaveWanMoTaPassLayerValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.WanMoTaCurrLayerOrder, nValue, true);
        }

        /// <summary>
        /// 读取万魔塔通关层数
        /// </summary>
        public int GetWanMoTaPassLayerValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.WanMoTaCurrLayerOrder);
        }

        #endregion 万魔塔通关层数

        /// <summary>
        /// 保存声望
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveShengWangValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ShengWang, nValue, writeToDB);
        }

        /// <summary>
        /// 读取声望
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetShengWangValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.ShengWang);
        }

        /// <summary>
        /// 修改声望等级值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyShengWangLevelValue(GameClient client, int addValue, string strFrom, bool writeToDB = false, bool notifyClient = true)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetShengWangLevelValue(client) + addValue;

            //更新到数据库
            SaveShengWangLevelValue(client, newValue, writeToDB);
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "声望等级", strFrom, "系统", client.ClientData.RoleName, "修改", addValue, client.ClientData.ZoneID, client.strUserID, newValue, client.ServerId);

            // MU成就处理 -- 军衔成就 [3/30/2014 LiaoWei]
            ChengJiuManager.OnRoleJunXianChengJiu(client);

            if (notifyClient)
            {
                //通知自己
                NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ShengWangLevel, newValue);
            }

            // 七日活动
            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.JunXianLevel));
        }

        /// <summary>
        /// 保存声望等级值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveShengWangLevelValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ShengWangLevel, nValue, writeToDB);

            //[bing] 刷新客户端活动叹号
            if (client._IconStateMgr.CheckJieRiFanLi(client, ActivityTypes.JieriMilitaryRank)
                || client._IconStateMgr.CheckSpecialActivity(client))
            {
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        /// <summary>
        /// 读取声望等级值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetShengWangLevelValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.ShengWangLevel);
        }

        /// <summary>
        /// 通知客户端角色参数发生变化
        /// </summary>
        /// <param name="client"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void NotifySelfParamsValueChange(GameClient client, RoleCommonUseIntParamsIndexs index, int value)
        {
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, (int)index, value);

            SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_ROLEPARAMSCHANGE);
        }

        /// <summary>
        /// 修改离线摆摊时长值 addValue > 0,增加，小于0，减少
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void ModifyLiXianBaiTanTicksValue(GameClient client, int addValue, bool writeToDB = false)
        {
            //对 0 参数不进行任何处理
            if (0 == addValue)
            {
                return;
            }

            int newValue = GetLiXianBaiTanTicksValue(client) + addValue;

            //更新到数据库
            SaveLiXianBaiTanTicksValue(client, newValue, writeToDB);
        }

        /// <summary>
        /// 保存离线摆摊时长值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="jiFen"></param>
        /// <param name="writeToDB"></param>
        public void SaveLiXianBaiTanTicksValue(GameClient client, int nValue, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.LiXianBaiTanTicks, nValue, writeToDB);
        }

        /// <summary>
        /// 读取离线摆摊时长值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetLiXianBaiTanTicksValue(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.LiXianBaiTanTicks);
        }

        #endregion 角色基础参数

        #region 游戏特殊效果播放(下雨，下雪，落花，烟花等)

        /// <summary>
        /// 播放游戏特殊效果(只给指定的角色)
        /// </summary>
        /// <param name="effectName"></param>
        /// <param name="lifeTicks"></param>
        public void SendGameEffect(GameClient client, string effectName, int lifeTicks, GameEffectAlignModes alignMode = GameEffectAlignModes.None, string mp3Name = "")
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}",
                effectName,
                lifeTicks,
                (int)alignMode,
                mp3Name);

            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_PLAYGAMEEFFECT);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                //
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        /// <summary>
        /// 播放游戏特殊效果
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMapID"></param>
        /// <param name="effectName"></param>
        /// <param name="lifeTicks"></param>
        /// <param name="alignMode"></param>
        public void BroadCastGameEffect(int mapCode, int copyMapID, string effectName, int lifeTicks, GameEffectAlignModes alignMode = GameEffectAlignModes.None, string mp3Name = "")
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}",
                effectName,
                lifeTicks,
                (int)alignMode,
                mp3Name);

            List<Object> objsList = GetMapClients(mapCode);
            if (null == objsList)
            {
                return;
            }

            objsList = Global.ConvertObjsList(mapCode, copyMapID, objsList);

            //群发消息
            SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_PLAYGAMEEFFECT);
        }

        #endregion 游戏特殊效果播放(下雨，下雪，落花，烟花等)

        #region 节日称号管理

        /// <summary>
        /// 播报节日称号
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        public void BroadcastJieriChengHao(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.JieriChengHao);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_CHGJIERICHENGHAO);
        }

        #endregion 节日称号管理

        #region 砸金蛋积分奖励相关

        /// <summary>
        /// 将砸金蛋积分奖励相关通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyZaJinDanKAwardDailyData(GameClient client)
        {
            int jiFen = Global.GetZaJinDanJifen(client);
            int jiFenBits = Global.GetZaJinDanJiFenBits(client);

            string strcmd = string.Format("{0}:{1}", jiFen, jiFenBits);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_ZJDJIFEN);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 砸金蛋积分奖励相关

        #region 冥想

        /// <summary>
        /// 通知用户某个精灵的冥想状态改变
        /// </summary>
        /// <param name="client"></param>
        public void NotifySpriteMeditate(SocketListener sl, TCPOutPacketPool pool, GameClient client, int meditate)
        {
            List<Object> objsList = Global.GetAll9Clients(client);
            if (null == objsList) return;

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, meditate);

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_STARTMEDITATE);
        }

        /// <summary>
        /// 将冥想的时间累计通知自己
        /// </summary>
        /// <param name="client"></param>
        public void NotifyMeditateTime(GameClient client)
        {
            int msecs1 = Global.GetRoleParamsInt32FromDB(client, RoleParamName.MeditateTime) / 1000;
            int msecs2 = Global.GetRoleParamsInt32FromDB(client, RoleParamName.NotSafeMeditateTime) / 1000;

            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, msecs1, msecs2);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GETMEDITATETIMEINFO);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 冥想

        #region 通知boss动画

        /// <summary>
        /// 将boss要刷新的动画消息通知客户端（因为一个地图可能存在多个角色，因此，只有单人副本才能配置boss动画，只发送给第一个角色）
        /// </summary>
        /// <param name="client"></param>
        public void NotifyPlayBossAnimation(GameClient client, int monsterID, int mapCode, int toX, int toY, int effectX, int effectY)
        {
            long ticks = TimeUtil.NOW();
            int checkCode = Global.GetBossAnimationCheckCode(monsterID, mapCode, toX, toY, effectX, effectY, ticks);

            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, monsterID, mapCode, toX, toY, effectX, effectY, ticks, checkCode);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_PLAYBOSSANIMATION);
            if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
            {
                /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                    tcpOutPacket.PacketCmdID,
                    tcpOutPacket.PacketDataSize,
                    client.ClientData.RoleID,
                    client.ClientData.RoleName));*/
            }
        }

        #endregion 通知boss动画

        #region 通知扩展属性命中

        /// <summary>
        /// 通知所有在线用户某个精灵的扩展属性被命中(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySpriteExtensionPropsHited(SocketListener sl, TCPOutPacketPool pool, IObject attacker, int enemy, int enemyX, int enemyY, int extensionPropID)
        {
            List<Object> objsList = Global.GetAll9Clients(attacker);
            if (null == objsList) return;

            SpriteExtensionPropsHitedData hitedData = new SpriteExtensionPropsHitedData();

            hitedData.roleId = attacker.GetObjectID();
            hitedData.enemy = enemy;
            hitedData.enemyX = enemyX;
            hitedData.enemyY = enemyY;
            hitedData.ExtensionPropID = extensionPropID;

            SendToClients(sl, pool, null, objsList, DataHelper.ObjectToBytes<SpriteExtensionPropsHitedData>(hitedData), (int)TCPGameServerCmds.CMD_SPR_EXTENSIONPROPSHITED);
        }

        #endregion 通知扩展属性命中

        #region 向地图内的角色广播消息

        /// <summary>
        /// 向地图中的用户广播特殊提示信息
        /// </summary>
        /// <param name="text"></param>
        public void BroadSpecialHintText(int mapCode, int copyMapID, string text)
        {
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(mapCode);
            if (null == objsList || objsList.Count <= 0) return;

            List<Object> objsList2 = new List<Object>();
            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;
                if (c.ClientData.CopyMapID != copyMapID) continue;

                objsList2.Add(c);
            }

            text = text.Replace(":", " ");
            string strcmd = string.Format("{0}", text);

            //群发消息
            SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList2, strcmd, (int)TCPGameServerCmds.CMD_SPR_BROADSPECIALHINTTEXT);
        }

        /// <summary>
        /// 向地图中的用户广播地图事件(清除光幕等)
        /// </summary>
        /// <param name="text"></param>
        public void BroadSpecialMapAIEvent(int mapCode, int copyMapID, int guangMuID, int show)
        {
            string strcmd = string.Format("{0}:{1}", guangMuID, show);

            List<Object> objsList = GameManager.ClientMgr.GetMapClients(mapCode);
            if (null == objsList || objsList.Count <= 0) return;
            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;
                if (c.ClientData.CopyMapID != copyMapID) continue;
                c.sendCmd((int)TCPGameServerCmds.CMD_SPR_MAPAIEVENT, strcmd);
            }
        }

        /// <summary>
        /// 向地图中的用户广播消息(Boss动画等)
        /// </summary>
        /// <param name="text"></param>
        public void BroadSpecialMapMessage(int cmdID, string strcmd, int mapCode, int copyMapID)
        {
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(mapCode);
            if (null == objsList || objsList.Count <= 0) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                if (c.ClientData.CopyMapID != copyMapID) continue;

                c.sendCmd(cmdID, strcmd);
            }
        }

        /// <summary>
        /// 向地图中的用户广播消息
        /// </summary>
        /// <param name="text"></param>
        public void BroadSpecialMapMessage(TCPOutPacket tcpOutPacket, int mapCode, int copyMapID, bool pushBack = true)
        {
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(mapCode);
            if (null == objsList || objsList.Count <= 0) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                if (c.ClientData.CopyMapID != copyMapID) continue;

                c.sendCmd(tcpOutPacket, false);
            }

            if (pushBack)
            {
                Global.PushBackTcpOutPacket(tcpOutPacket);
            }
        }

        /// <summary>
        /// 向副本地图中的用户广播消息
        /// </summary>
        /// <param name="cmdID"></param>
        /// <param name="strcmd"></param>
        /// <param name="copyMap"></param>
        /// <param name="insertRoleID"></param>
        public void BroadSpecialCopyMapMessageStr(int cmdID, string strcmd, CopyMap copyMap, bool insertRoleID = false)
        {
            List<GameClient> objsList = copyMap.GetClientsList();
            if (null == objsList || objsList.Count <= 0) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i];
                if (c == null) continue;

                if (c.ClientData.CopyMapID != copyMap.CopyMapID) continue;

                if (insertRoleID)
                {
                    c.sendCmd(cmdID, strcmd.Insert(0, string.Format("{0}:", c.ClientData.RoleID)));
                }
                else
                {
                    c.sendCmd(cmdID, strcmd);
                }
            }
        }

        /// <summary>
        /// 向地图中的用户广播消息(Boss动画等)
        /// </summary>
        /// <param name="text"></param>
        public void BroadSpecialCopyMapMessage<T>(int cmdID, T data, CopyMap copyMap)
        {
            List<GameClient> objsList = copyMap.GetClientsList();
            if (null == objsList || objsList.Count <= 0) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i];
                if (c != null && c.ClientData.CopyMapID == copyMap.CopyMapID)
                {
                    c.sendCmd(cmdID, data);
                }
            }
        }

        /// <summary>
        /// 向地图中的用户广播消息(Boss动画等)
        /// </summary>
        /// <param name="text"></param>
        public void BroadSpecialCopyMapMessage(int cmdID, string strcmd, List<GameClient> objsList, bool insertRoleID = false)
        {
            if (null == objsList || objsList.Count <= 0) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i];
                if (c == null) continue;

                if (insertRoleID)
                {
                    c.sendCmd(cmdID, strcmd.Insert(0, string.Format("{0}:", c.ClientData.RoleID)));
                }
                else
                {
                    c.sendCmd(cmdID, strcmd);
                }
            }
        }

        /// <summary>
        /// 副本地图群发提示信息
        /// </summary>
        /// <param name="copymap"></param>
        /// <param name="msg"></param>
        public void BroadSpecialCopyMapHintMsg(CopyMap copymap, string msg)
        {
            try
            {
                msg = msg.Replace(":", "``");
                string strcmd = string.Format("{0}:{1}:{2}:{3}", (int)ShowGameInfoTypes.ErrAndBox, (int)GameInfoTypeIndexes.Error, msg, (int)HintErrCodeTypes.None);
                BroadSpecialCopyMapMessageStr((int)TCPGameServerCmds.CMD_SPR_NOTIFYMSG, strcmd, copymap);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 副本地图群发提示信息
        /// </summary>
        /// <param name="copymap"></param>
        /// <param name="msg"></param>
        public void BroadSpecialCopyMapMsg(CopyMap copymap, string msg, ShowGameInfoTypes showGameInfoType = ShowGameInfoTypes.OnlySysHint, GameInfoTypeIndexes infoType = GameInfoTypeIndexes.Hot, int error = (int)HintErrCodeTypes.None)
        {
            try
            {
                msg = msg.Replace(":", "``");
                string strcmd = string.Format("{0}:{1}:{2}:{3}", (int)showGameInfoType, (int)infoType, msg, error);
                BroadSpecialCopyMapMessageStr((int)TCPGameServerCmds.CMD_SPR_NOTIFYMSG, strcmd, copymap);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 通知客户端切换到常规地图
        /// </summary>
        /// <param name="client"></param>
        public void NotifyChangMap2NormalMap(GameClient client)
        {
            if (Global.CanChangeMap(client, client.ClientData.LastMapCode, client.ClientData.LastPosX, client.ClientData.LastPosY, true))
            {
                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, client.ClientData.LastMapCode, client.ClientData.LastPosX, client.ClientData.LastPosY, -1);
            }
            else
            {
                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, GameManager.MainMapCode, -1, -1, -1);
            }
        }

        #endregion 向地图内的角色广播消息

        #endregion 扩展属性和方法

        #region 后台工作线程调用方法

        /// <summary>
        /// 补血补魔
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteLifeMagic(SocketListener sl, TCPOutPacketPool pool, GameClient c)
        {
            long subTicks = 0;
            long ticks = TimeUtil.NOW();
            subTicks = ticks - c.LastLifeMagicTick;

            //如果还没到时间，则跳过
            if (subTicks < (10 * 1000))
            {
                return;
            }

            c.LastLifeMagicTick = ticks;
            RoleRelifeLog relifeLog = new RoleRelifeLog(c.ClientData.RoleID, c.ClientData.RoleName, c.ClientData.MapCode, "自然恢复补血补蓝");

            //如果已经死亡，则不再调度
            if (c.ClientData.CurrentLifeV > 0)
            {
                bool doRelife = false;

                //判断如果血量少于最大血量
                if (c.ClientData.CurrentLifeV < c.ClientData.LifeV)
                {
                    doRelife = true;
                    relifeLog.hpModify = true;
                    relifeLog.oldHp = c.ClientData.CurrentLifeV;

                    double percent = RoleAlgorithm.GetLifeRecoverValPercentV(c);
                    double lifeMax = percent * c.ClientData.LifeV;
                    lifeMax = lifeMax * (1.0 + RoleAlgorithm.GetLifeRecoverAddPercentV(c) + DBRoleBufferManager.ProcessHuZhaoRecoverPercent(c) + RoleAlgorithm.GetLifeRecoverAddPercentOnlySandR(c));

                    //if (c.ClientData.CurrentAction == (int)GActions.Sit) //如果是在打坐中，则快
                    //{
                    //    lifeMax *= 2;
                    //}

                    lifeMax += c.ClientData.CurrentLifeV;
                    c.ClientData.CurrentLifeV = (int)Global.GMin(c.ClientData.LifeV, lifeMax);
                    relifeLog.newHp = c.ClientData.CurrentLifeV;
                    //GameManager.SystemServerEvents.AddEvent(string.Format("角色加血, roleID={0}({1}), Add={2}, Life={3}", c.ClientData.RoleID, c.ClientData.RoleName, percent * c.ClientData.LifeV, c.ClientData.CurrentLifeV), EventLevels.Debug);
                }

                //判断如果魔量少于最大魔量
                if (c.ClientData.CurrentMagicV < c.ClientData.MagicV)
                {
                    doRelife = true;
                    relifeLog.mpModify = true;
                    relifeLog.oldMp = c.ClientData.CurrentMagicV;

                    double percent = RoleAlgorithm.GetMagicRecoverValPercentV(c);
                    double magicMax = percent * c.ClientData.MagicV;
                    magicMax = magicMax * (1.0 + RoleAlgorithm.GetMagicRecoverAddPercentV(c) + RoleAlgorithm.GetMagicRecoverAddPercentOnlySandR(c));

                    //if (c.ClientData.CurrentAction == (int)GActions.Sit) //如果是在打坐中，则快
                    //{
                    //    magicMax *= 2;
                    //}

                    magicMax += c.ClientData.CurrentMagicV;
                    c.ClientData.CurrentMagicV = (int)Global.GMin(c.ClientData.MagicV, magicMax);
                    relifeLog.newMp = c.ClientData.CurrentMagicV;
                    //GameManager.SystemServerEvents.AddEvent(string.Format("角色加魔, roleID={0}({1}), Add={2}, Magic={3}", c.ClientData.RoleID, c.ClientData.RoleName, percent * c.ClientData.MagicV, c.ClientData.CurrentMagicV), EventLevels.Debug);
                }

                if (doRelife)
                {
                    //通知客户端怪已经加血加魔    
                    List<Object> listObjs = Global.GetAll9Clients(c);
                    GameManager.ClientMgr.NotifyOthersRelife(sl, pool, c, c.ClientData.MapCode, c.ClientData.CopyMapID, c.ClientData.RoleID, (int)c.ClientData.PosX, (int)c.ClientData.PosY, (int)c.ClientData.RoleDirection, c.ClientData.CurrentLifeV, c.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
                }

                MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);
            }
        }

        //2011-05-31 精简指令
        /// <summary>
        /// 与DBServer保持心跳连接
        /// </summary>
        /// <param name="client"></param>
        //private void KeepHeartWithDBServer(GameClient client)
        //{
        //    string strcmd = string.Format("{0}", client.ClientData.RoleID);
        //    string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_ROLE_HEART, strcmd);
        //    if (null == fields) return;
        //    if (fields.Length != 2)
        //    {
        //        return;
        //    }

        //    //判断是否更新了游戏元宝
        //    int addUserMoney = Convert.ToInt32(fields[1]);
        //    if (addUserMoney > 0)
        //    {
        //        client.ClientData.UserMoney += addUserMoney; //更新用户的元宝

        //        // 钱更新通知(只通知自己)
        //        GameManager.ClientMgr.NotifySelfUserMoneyChange(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
        //    }
        //}

        /// <summary>
        /// 玩家心跳
        /// </summary>
        private void DoSpriteHeart(SocketListener sl, TCPOutPacketPool pool, GameClient c)
        {
            // 玩家每日在线时长[1/16/2014 LiaoWei]
            // client.ClientData.DayOnlineSecond += Math.Max(0, (int)(addTicks / 1000));
            // 玩家每日在线时长[05/25/2014 ChenXiaojun]
            // 移到同一个线程处理
            c.ClientData.DayOnlineSecond = c.ClientData.BakDayOnlineSecond + (int)((TimeUtil.NOW() - c.ClientData.DayOnlineRecSecond) / 1000);
        }
        /// <summary>
        /// 和DBserver保持数据更新
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteDBHeart(SocketListener sl, TCPOutPacketPool pool, GameClient c)
        {
            long subTicks = 0;
            long ticks = TimeUtil.NOW();
            subTicks = ticks - c.ClientData.LastDBHeartTicks;

            // 玩家每日在线时长[1/16/2014 LiaoWei]
            // client.ClientData.DayOnlineSecond += Math.Max(0, (int)(addTicks / 1000));
            // 玩家每日在线时长[05/25/2014 ChenXiaojun]
            // 移到同一个线程处理
            // c.ClientData.DayOnlineSecond = c.ClientData.BakDayOnlineSecond + (int)((TimeUtil.NOW() - c.ClientData.DayOnlineRecSecond) / 1000);

            //如果还没到时间，则跳过
            if (subTicks < (10 * 1000))
            {
                return;
            }

            long remainder = 0;
            Math.DivRem(subTicks, 1000, out remainder);
            subTicks -= remainder;
            ticks -= remainder;
            c.ClientData.LastDBHeartTicks = ticks;

            //与DBServer保持心跳连接
            //2011-05-31 不再使用，仅靠GameServer的健壮性，来实现dbserver判断是否在线
            //KeepHeartWithDBServer(c);

            //更新角色的在线时长
            UpdateRoleOnlineTimes(c, subTicks);

            //2011-05-31 精简DBServer指令
            //是否记录位置信息
            //if (Global.CanRecordPos(c))
            //{
            //    //异步写数据库，写入当前的位置
            //    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_POS,
            //        string.Format("{0}:{1}:{2}:{3}:{4}", c.ClientData.RoleID, c.ClientData.MapCode, c.ClientData.RoleDirection, c.ClientData.PosX, c.ClientData.PosY),
            //        null);
            //}
        }

        /// <summary>
        /// 自动挂机的调度
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteAutoFight(SocketListener sl, TCPOutPacketPool pool, GameClient c)
        {
            long nowTicks = TimeUtil.NOW();

            //自动挂机自动大范围拾取
            c.AutoGetThingsOnAutoFight(nowTicks);

            //判断如果是在自动战斗中，则消减自动挂机时间
            //if (c.ClientData.AutoFighting)
            //{
            //    if (c.ClientData.AutoFightingProctect <= 0)
            //    {
            //        long ticks = TimeUtil.NOW();
            //        if (ticks - c.ClientData.LastAutoFightTicks >= (5 * 60 * 1000)) //超过5分钟，才进入被保护状态
            //        {
            //            //处理挂机保护卡
            //            if (DBRoleBufferManager.ProcessAutoFightingProtect(c))
            //            {
            //                c.ClientData.AutoFightingProctect = 1;

            //                GameManager.ClientMgr.NotifyImportantMsg(sl, pool, c,
            //                    StringUtil.substitute(Global.GetLang("自动战斗进入了【战斗保护】状态")),
            //                    GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.EnterAFProtect);
            //            }
            //        }
            //    }
            //}
        }

        /// <summary>
        /// 处理打坐收益
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteSitExp(SocketListener sl, TCPOutPacketPool pool, GameClient c)
        {
            long subTicks = 0;
            long ticks = TimeUtil.NOW();
            subTicks = ticks - c.ClientData.LastSiteExpTicks;

            //如果还没到时间，则跳过
            if (subTicks < (60 * 1000))
            {
                return;
            }

            c.ClientData.LastSiteExpTicks = ticks;

            //判断是否在主城的安全区中，否则不增加经验(后期加入)
            bool safeRegion = false;
            if (c.ClientData.MapCode == GameManager.MainMapCode)
            {
                GameMap gameMap = null;
                if (GameManager.MapMgr.DictMaps.TryGetValue(c.ClientData.MapCode, out gameMap))
                {
                    safeRegion = gameMap.InSafeRegionList(c.CurrentGrid);
                }
            }

            if (!safeRegion)
            {
                return;
            }

            double multiExpNum = 0.0;
            long zhuFuSecs = DBRoleBufferManager.ProcessErGuoTouGiveExperience(c, subTicks, out multiExpNum);

            //处理祝福经验buffer，定时给经验
            if (zhuFuSecs <= 0)
            {
                return;
            }

            //判断如果是否在打坐，则自动增加经验和内力值
            RoleSitExpItem roleSitExpItem = null;
            if (c.ClientData.Level < Data.RoleSitExpList.Length)
            {
                roleSitExpItem = Data.RoleSitExpList[c.ClientData.Level];
            }

            //经验的收益
            if (null != roleSitExpItem)
            {
                int experience = roleSitExpItem.Experience;
                double dblExperience = 1.0;

                //这儿应该是双倍烤火时间(后期加入)
                if (SpecailTimeManager.JugeIsDoulbeKaoHuo())
                {
                    dblExperience += 1.0;
                }

                //如果是处于组队状态，则有经验加成
                //处理组队状态下的祝福经验加成
                dblExperience += Global.ProcessTeamZhuFuExperience(c);

                //增加额外的倍数
                dblExperience += multiExpNum;

                //处理双倍经验的buffer
                experience = (int)(experience * dblExperience);

                //处理角色经验
                GameManager.ClientMgr.ProcessRoleExperience(c, experience, true, false, true); //不写数据库, 否则太频繁

                //通知客户端学习了新技能
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    c, StringUtil.substitute(Global.GetLang("恭喜您获得了烤火经验 +{0}, 您的烤火时间还剩余{1}分{2}秒"), experience, zhuFuSecs / 60, zhuFuSecs % 60), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
            }
        }

        /// <summary>
        /// 处理消减PK点
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteSubPKPoint(SocketListener sl, TCPOutPacketPool pool, GameClient c)
        {
            //如果不是在打坐，并且也不在牢房, 则不消减PK值
            //if (c.ClientData.CurrentAction != (int)GActions.Sit &&
            //        Global.GetLaoFangMapCode() != c.ClientData.MapCode)
            //{
            //    return;
            //}

            long subTicks = 0;
            long ticks = TimeUtil.NOW();
            subTicks = ticks - c.ClientData.LastSiteSubPKPointTicks;

            //如果还没到时间，则跳过
            if (subTicks < (60 * 1000)) // 原来是36 现在改成 60秒   [4/21/2014 LiaoWei]
            {
                return;
            }

            c.ClientData.LastSiteSubPKPointTicks = ticks;
            if (c.ClientData.PKPoint <= 0) //已经为0，不需要再消减
            {
                return;
            }

            //判断如果是否在打坐，则自动增加经验和内力值
            //RoleSitExpItem roleSitExpItem = null;
            //if (c.ClientData.Level < Data.RoleSitExpList.Length)
            //{
            //    roleSitExpItem = Data.RoleSitExpList[c.ClientData.Level];
            //}

            ////消减PK值
            //if (null != roleSitExpItem)
            //{
            //    int oldPKPoint = c.ClientData.PKPoint;

            //    //消减PK值
            //    c.ClientData.PKPoint = Global.GMax(c.ClientData.PKPoint - roleSitExpItem.PKPoint, 0);
            //    if (oldPKPoint != c.ClientData.PKPoint)
            //    {
            //        //设置PK值(限制当前地图)
            //        SetRolePKValuePoint(sl, pool, c, c.ClientData.PKValue, c.ClientData.PKPoint, false);
            //    }
            //}

            int oldPKPoint = c.ClientData.PKPoint;

            //消减PK值
            c.ClientData.PKPoint = Global.GMax(c.ClientData.PKPoint - Data.ConstSubPKPointPerMin, 0);    // 原来是-1 现在改成-2 [4/21/2014 LiaoWei]

            c.ClientData.TmpPKPoint += Data.ConstSubPKPointPerMin;

            if (oldPKPoint != c.ClientData.PKPoint)
            {
                // 设置PK值(限制当前地图)
                if (c.ClientData.TmpPKPoint >= 60)
                {
                    SetRolePKValuePoint(sl, pool, c, c.ClientData.PKValue, c.ClientData.PKPoint, true);
                    c.ClientData.TmpPKPoint = 0;
                }
                else
                    SetRolePKValuePoint(sl, pool, c, c.ClientData.PKValue, c.ClientData.PKPoint, false);
            }
        }

        /// <summary>
        /// 处理DBBuffer中的项
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteBuffers(SocketListener sl, TCPOutPacketPool pool, GameClient c)
        {
            //处理经验buffer，定时给经验
            DBRoleBufferManager.ProcessAutoGiveExperience(c);

            //去除生命符咒
            DBRoleBufferManager.RemoveUpLifeLimitStatus(c);

            //去除攻击的buffer
            DBRoleBufferManager.RemoveAttackBuffer(c);

            //去除防御的buffer
            DBRoleBufferManager.RemoveDefenseBuffer(c);

            //刷新战斗属性
            {
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.TimeAddDefense);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.TimeAddMDefense);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.TimeAddAttack);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.TimeAddMAttack);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.TimeAddDSAttack);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.PKKingBuffer);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.DSTimeShiDuNoShow);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.DSTimeAddLifeNoShow);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.DSTimeAddDefenseNoShow);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.DSTimeAddMDefenseNoShow);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.MU_LUOLANCHENGZHAN_QIZHI1);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.MU_LUOLANCHENGZHAN_QIZHI2);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.MU_LUOLANCHENGZHAN_QIZHI3);
            }

            // 属性改造 一级属性 [8/15/2013 LiaoWei]
            {
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.ADDTEMPStrength);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.ADDTEMPIntelligsence);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.ADDTEMPDexterity);
                DBRoleBufferManager.RefreshTimePropBuffer(c, BufferItemTypes.ADDTEMPConstitution);
            }

            //处理药品buffer，定时不计生命和蓝
            DBRoleBufferManager.ProcessTimeAddLifeMagic(c);

            //处理药品buffer，定时不计生命
            DBRoleBufferManager.ProcessTimeAddLifeNoShow(c);

            //处理药品buffer，定时不计蓝
            DBRoleBufferManager.ProcessTimeAddMagicNoShow(c);

            //处理道士加血buffer，定时不计生命
            DBRoleBufferManager.ProcessDSTimeAddLifeNoShow(c);

            //处理道士释放毒的buffer, 定时伤害
            DBRoleBufferManager.ProcessDSTimeSubLifeNoShow(c);

            //处理持续伤害的新的扩展buffer, 定时伤害
            DBRoleBufferManager.ProcessAllTimeSubLifeNoShow(c);

            AdvanceBufferPropsMgr.DoSpriteBuffers(c);

            // 刷新塔罗牌国王特权加成
            //判断功能是否开启
            if (GlobalNew.IsGongNengOpened(c, GongNengIDs.TarotCard))
                TarotManager.getInstance().RemoveTarotKingData(c);

            long subTicks = 0;
            long ticks = TimeUtil.NOW();
            subTicks = ticks - c.ClientData.LastProcessBufferTicks;

            //如果还没到时间，则跳过
            if (subTicks < (60 * 1000)) //生命和魔法储备不再使用， 武学和成就的激活1分钟的间隔足够了
            {
                return;
            }

            c.ClientData.LastProcessBufferTicks = ticks;

            //处理生命和魔法储备
            //DBRoleBufferManager.ProcessLifeVAndMagicVReserve(sl, pool, c);

            //判断是否需要激活新的成就 武学 经脉buffer
            //Global.TryToActiveNewWuXueBuffer(c, true);    注释掉 [5/7/2014 LiaoWei]

            // 不需要再自动更新 ChengXiaojun
            //ChengJiuManager.TryToActiveNewChengJiuBuffer(c, true);

            /// 刷新初始化节日称号
            Global.RefreshJieriChengHao(c);
        }

        /// <summary>
        /// 处理角色的地图限制字段
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteMapLimitTimes(SocketListener sl, TCPOutPacketPool pool, GameClient c)
        {
            long subTicks = 0;
            DateTime dateTime = TimeUtil.NowDateTime();
            long ticks = dateTime.Ticks / 10000;
            subTicks = ticks - c.ClientData.LastProcessMapLimitTimesTicks;

            //如果还没到时间，则跳过
            if (subTicks < (60 * 1000))
            {
                return;
            }

            int elapsedSecs = (int)((ticks - c.ClientData.LastProcessMapLimitTimesTicks) / 1000);
            c.ClientData.LastProcessMapLimitTimesTicks = ticks;

            //判断是否超出了地图的时间限制
            if (!Global.CanMapInLimitTimes(c.ClientData.MapCode, dateTime))
            {
                GameManager.ClientMgr.NotifyImportantMsg(sl, pool, c,
                    StringUtil.substitute(Global.GetLang("你在『{0}』地图中停留的时间超过了限制，被系统自动传回主城"), Global.GetMapName(c.ClientData.MapCode)),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    c, GameManager.MainMapCode, -1, -1, -1);
            }

            //处理每日停留时间限制
            Global.ProcessDayLimitSecsByClient(c, elapsedSecs);
        }

        /// <summary>
        /// 处理角色的地图限时，角色使用某些道具到某个地图只能停留有限时间
        /// </summary>
        /// <param name="client"></param>
        private static void DoSpriteMapTimeLimit(GameClient client)
        {
            long nowTicks = TimeUtil.NOW();
            long elapseTicks = nowTicks - client.ClientData.LastMapLimitUpdateTicks;

            //判断上次限时地图更新判断的时间
            if (elapseTicks < 3000) //每3秒更新判断一次 
            {
                return;
            }

            //冥界地图
            Global.ProcessMingJieMapTimeLimit(client, elapseTicks);

            //古墓地图
            Global.ProcessGuMuMapTimeLimit(client, elapseTicks);

            //记录本次的时间
            client.ClientData.LastMapLimitUpdateTicks = nowTicks;
        }

        /// <summary>
        /// 处理角色登录的客户端的修订版本号低于服务器端版本号时, 每隔1分钟推送一次给用户,提示用户更新客户端
        /// </summary>
        /// <param name="client"></param>
        private static void DoSpriteHintToUpdateClient(GameClient client)
        {
            long nowTicks = TimeUtil.NOW();
            long elapseTicks = nowTicks - client.ClientData.LastHintToUpdateClientTicks;

            //判断上次限时地图更新判断的时间
            if (elapseTicks < (60 * 1000)) //每1分钟判断一次 
            {
                return;
            }

            //记录本次的时间
            client.ClientData.LastHintToUpdateClientTicks = nowTicks;

            int forceHintAppVer = GameManager.GameConfigMgr.GetGameConfigItemInt("hint-appver", 0);
            if (client.MainExeVer > 0 && client.MainExeVer < forceHintAppVer)
            {
                string msgID = "1";
                int minutes = 1;
                int playNum = 1;
                //string bulletinText = "服务器端检测到你的客户端版本过低, 请完全退出游戏后再启动自动更新, 如果没有自动更新, 请重新下载安装游戏!";
                string bulletinText = Global.GetLang("尊敬的用户，您当前的客户端版本过低可能会导致各种异常，建议您重新下载最新的客户端！");

                BulletinMsgData bulletinMsgData = new BulletinMsgData()
                {
                    MsgID = msgID,
                    PlayMinutes = minutes,
                    ToPlayNum = playNum,
                    BulletinText = bulletinText,
                    BulletinTicks = TimeUtil.NOW(),
                    MsgType = 0,
                };

                //将本条消息广播给所有在线的客户端
                GameManager.ClientMgr.NotifyBulletinMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, bulletinMsgData);
            }
            else if (client.ResVer > 0) //判断资源是否需要更新
            {
                int forceHintResVer = GameManager.GameConfigMgr.GetGameConfigItemInt("hint-resver", 0);
                if (client.ResVer < forceHintResVer)
                {
                    string msgID = "1";
                    int minutes = 1;
                    int playNum = 1;
                    //string bulletinText = "服务器端检测到你的客户端版本过低, 请完全退出游戏后再启动自动更新, 如果没有自动更新, 请重新下载安装游戏!";
                    string bulletinText = Global.GetLang("尊敬的用户，您当前的客户端游戏资源版本过低可能会导致无法游戏，建议您退出游戏后重新启动会自动更新到最新版本！");

                    BulletinMsgData bulletinMsgData = new BulletinMsgData()
                    {
                        MsgID = msgID,
                        PlayMinutes = minutes,
                        ToPlayNum = playNum,
                        BulletinText = bulletinText,
                        BulletinTicks = TimeUtil.NOW(),
                        MsgType = 0,
                    };

                    //将本条消息广播给所有在线的客户端
                    GameManager.ClientMgr.NotifyBulletinMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, bulletinMsgData);
                }
            }
        }

        /// <summary>
        /// 处理角色的物品限时
        /// </summary>
        /// <param name="client"></param>
        private static void DoSpriteGoodsTimeLimit(GameClient client)
        {
            bool isFashion = false;
            bool isGoods = false;

            long nowTicks = TimeUtil.NOW();
            long elapseTicks = nowTicks - client.ClientData.LastGoodsLimitUpdateTicks;
            long fashionTicks = nowTicks - client.ClientData.LastFashionLimitUpdateTicks;

            List<GoodsData> expiredList = null;
            if (fashionTicks >= 3 * 1000)
            {
                expiredList = Global.GetFashionTimeExpired(client);
                isFashion = true;
            }

            //判断上次限时地图更新判断的时间
            if (elapseTicks >= 30 * 1000) //每30秒更新判断一次 
            {
                List<GoodsData> goodsList = Global.GetGoodsTimeExpired(client);
                if (goodsList != null)
                {
                    if (expiredList == null)
                        expiredList = goodsList;
                    else
                        expiredList.AddRange(Global.GetGoodsTimeExpired(client));
                }

                isGoods = true;
            }

            if (null != expiredList && expiredList.Count > 0)
            {
                //这儿进行物品摧毁操作
                for (int n = 0; n < expiredList.Count; n++)
                {
                    GoodsData goods = expiredList[n];
                    if (Global.DestroyGoods(client, goods))
                    {
                        Global.SendMail(client, Global.GetLang("物品过期通知"), string.Format(
                            Global.GetLang("限时物品【{0}】已过期，自动销毁"), Global.GetGoodsNameByID(goods.GoodsID)));
                    }
                }
            }

            if (isGoods) client.ClientData.LastGoodsLimitUpdateTicks = nowTicks;

            if (isFashion) client.ClientData.LastFashionLimitUpdateTicks = nowTicks;
        }

        /// <summary>
        /// 处理角色的地图移动延迟刷新
        /// </summary>
        /// <param name="client"></param>
        public static void DoSpriteMapGridMove(GameClient client, long extTicks = 1000)
        {
            long ticks = TimeUtil.NOW();

            //后台是否正在处理中
            lock (client.Current9GridMutex)
            {
                long slotTicks = Math.Max(1000, GameManager.MaxSlotOnUpdate9GridsTicks);
                slotTicks = Math.Max(slotTicks, extTicks);

                if (ticks - client.LastRefresh9GridObjectsTicks >= slotTicks)
                {
                    client.LastRefresh9GridObjectsTicks = ticks;
                    Global.GameClientMoveGrid(client);

                    //System.Diagnostics.Debug.WriteLine(string.Format("Global.GameClientMoveGrid {0}", TimeUtil.NowDateTime()));
                }
            }

            //System.Diagnostics.Debug.WriteLine(string.Format("DoSpriteMapGridMove 消耗时间:{0}", TimeUtil.NOW() - ticks));
        }

        /// <summary>
        // 处理冥想计时 [3/18/2014 LiaoWei]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteMeditateTime(GameClient c)
        {
            long lTicks = 0;
            long lCurrticks = TimeUtil.NOW();

            lTicks = lCurrticks - c.ClientData.MeditateTicks;

            if (lTicks < 10 * 1000) //每10秒判断是否可进入冥想状态
            {
                return;
            }

            //判断是否可自动进入冥想状态
            //if (c.ClientData.StartMeditate <= 0 && c.ClientData.MeditateTime + c.ClientData.NotSafeMeditateTime < Global.ConstMaxMeditateTime)
            if (c.ClientData.StartMeditate <= 0)
            {
                if (c.ClientData.LastMovePosTicks == 0 || c.ClientData.Last10sPosX != c.ClientData.PosX || c.ClientData.Last10sPosY != c.ClientData.PosY)
                {
                    c.ClientData.Last10sPosX = c.ClientData.PosX;
                    c.ClientData.Last10sPosY = c.ClientData.PosY;
                    c.ClientData.LastMovePosTicks = lCurrticks;
                }
                //else if (c.ClientData.StallDataItem != null)
                //{
                //    //摆摊状态不进入冥想
                //}
                else if (!GlobalNew.IsGongNengOpened(c, GongNengIDs.MingXiang))
                {
                    //未达到冥想功能开启等级
                }
                else if (lCurrticks - c.ClientData.LastMovePosTicks > 60 * 1000)
                {
                    Global.StartMeditate(c);
                    lTicks = 60 * 1000; //强制下面的代码执行,以便同步刷新
                }
            }

            // 每分钟计时一次
            if (lTicks < (60 * 1000))
            {
                return;
            }

            c.ClientData.MeditateTicks = lCurrticks;

            //是否进入了冥想状态
            if (c.ClientData.StartMeditate <= 0)
            {
                return;
            }

            // 判断是否在安全区中
            bool bIsInsafeArea = true;

            //总是算做安全区时间,这样避免导致每次下线后总时间回到11:59分
            //GameMap gameMap = null;
            //if (GameManager.MapMgr.DictMaps.TryGetValue(c.ClientData.MapCode, out gameMap))
            //    bIsInsafeArea = gameMap.InSafeRegionList(c.CurrentGrid);

            if (bIsInsafeArea)
            {
                int nTime = Global.GetRoleParamsInt32FromDB(c, RoleParamName.MeditateTime);
                int nTime2 = Global.GetRoleParamsInt32FromDB(c, RoleParamName.NotSafeMeditateTime);
                if ((nTime + nTime2) < Global.ConstMaxMeditateTime)
                {
                    long msecs = Math.Max(lCurrticks - c.ClientData.BiGuanTime, 0);
                    msecs = Math.Min(msecs + nTime, Global.ConstMaxMeditateTime - nTime2);   // 12个小时

                    c.ClientData.MeditateTime = (int)msecs;
                    Global.SaveRoleParamsInt32ValueToDB(c, RoleParamName.MeditateTime, (int)msecs, false);
                }
            }
            else
            {
                int nTime = Global.GetRoleParamsInt32FromDB(c, RoleParamName.MeditateTime);
                int nTime2 = Global.GetRoleParamsInt32FromDB(c, RoleParamName.NotSafeMeditateTime);

                if ((nTime + nTime2) < Global.ConstMaxMeditateTime)
                {
                    long msecs = Math.Max(lCurrticks - c.ClientData.BiGuanTime, 0);
                    msecs = Math.Min(msecs + nTime2, Global.ConstMaxMeditateTime - nTime);   // 12个小时

                    c.ClientData.NotSafeMeditateTime = (int)msecs;
                    Global.SaveRoleParamsInt32ValueToDB(c, RoleParamName.NotSafeMeditateTime, (int)msecs, false);
                }
            }

            // 重置时间
            c.ClientData.BiGuanTime = lCurrticks;

            GameManager.ClientMgr.NotifyMeditateTime(c);

            return;
        }

        /// <summary>
        // 处理死亡计时 [9/23/2014 lt]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteDeadTime(GameClient c)
        {
            long lTicks = 0;
            long lCurrticks = TimeUtil.NOW();
            lTicks = lCurrticks - c.ClientData.LastProcessDeadTicks;
            if (c.ClientData.CurrentLifeV > 0 || lTicks < 3 * 1000) //每3秒判断是否应处理死亡状态
            {
                return;
            }

            c.ClientData.LastProcessDeadTicks = lCurrticks;
            //if (c.ClientData.LastRoleDeadTicks + 12000 < lCurrticks) //死亡12秒内,不触发死亡复活容错自动复活机制
            //{
            //    return;
            //}
            ProcessSpriteDead(c, lCurrticks);
        }

        private void ProcessSpriteDead(GameClient client, long nowTicks)
        {
            int posX = -1, posY = -1;

            //如果是超时机制复活，需要判断死亡时间是否超过特定时间
            if ((int)RoleReliveTypes.TimeWaiting == Global.GetRoleReliveType(client) || (int)RoleReliveTypes.TimeWaitingRandomAlive == Global.GetRoleReliveType(client))
            {
                long elapseTicks = nowTicks - client.ClientData.LastRoleDeadTicks;
                if (elapseTicks / 1000 < Global.GetRoleReliveWaitingSecs(client) + 3000)
                {
                    return;
                }
            }
            else if ((int)RoleReliveTypes.TimeWaitingOrRelifeNow == Global.GetRoleReliveType(client))
            {
                long elapseTicks = TimeUtil.NOW() - client.ClientData.LastRoleDeadTicks;
                if (elapseTicks / 1000 < Global.GetRoleReliveWaitingSecs(client) + 3000)
                {
                    return;
                }
                posX = -1;
                posY = -1;
            }
            else if ((int)RoleReliveTypes.HomeOrHere == Global.GetRoleReliveType(client))
            {
                if (nowTicks - client.ClientData.LastRoleDeadTicks < 35000)
                {
                    return;
                }
            }
            else if ((int)RoleReliveTypes.Home == Global.GetRoleReliveType(client))
            {
                if (nowTicks - client.ClientData.LastRoleDeadTicks < 5000)
                {
                    return;
                }
            }
            else
            {
                return; //地图编号为-1或未来新加的复活方式,以后再加
            }

            //如果是在皇城地图上
            if (Global.IsHuangChengMapCode(client.ClientData.MapCode) || Global.IsHuangGongMapCode(client.ClientData.MapCode))
            {
                posX = -1;
                posY = -1;
            }

            //如果玩家在炎黄战场内，则强行传送回本阵营复活点复活
            if (Global.IsBattleMap(client))
            {
                int toMapCode = GameManager.BattleMgr.BattleMapCode;
                GameMap gameMap = null;
                if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                {
                    client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                    client.ClientData.CurrentMagicV = client.ClientData.MagicV;

                    int defaultBirthPosX = gameMap.DefaultBirthPosX;
                    int defaultBirthPosY = gameMap.DefaultBirthPosY;
                    int defaultBirthRadius = gameMap.BirthRadius;

                    Global.GetBattleMapPos(client, ref defaultBirthPosX, ref defaultBirthPosY, ref defaultBirthRadius);

                    //从配置根据地图取默认位置
                    Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, toMapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
                    posX = (int)newPos.X;
                    posY = (int)newPos.Y;

                    //角色复活
                    Global.ClientRealive(client, posX, posY, client.ClientData.RoleDirection);
                }

                //只要进入这个分支，强行返回 ok
                return;
            }

            /// 是否是领地战地图
            if (Global.IsLingDiZhanMapCode(client))
            {
                int toMapCode = client.ClientData.MapCode;
                GameMap gameMap = null;
                if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                {
                    client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                    client.ClientData.CurrentMagicV = client.ClientData.MagicV;

                    //随机点
                    Point newPos = Global.GetRandomPoint(ObjectTypes.OT_CLIENT, toMapCode);

                    posX = (int)newPos.X;
                    posY = (int)newPos.Y;

                    //角色复活
                    Global.ClientRealive(client, posX, posY, client.ClientData.RoleDirection);
                }

                //只要进入这个分支，强行返回 ok
                return;
            }

            //竞技场决斗赛死亡后，强制回城复活
            if (GameManager.ArenaBattleMgr.IsInArenaBattle(client))
            {
                posX = -1;
                posY = -1;
            }

            //如果是回城复活
            if (posX == -1 || posY == -1)
            {
                // 复活改造 [3/19/2014 LiaoWei]
                /*int toMapCode = GameManager.MainMapCode;
                //if (client.ClientData.MapCode == GameManager.DefaultMapCode) //新手村死亡后，回城复活，是回新手村得出生点，而不是扬州城
                //某些地图回城复活不回主城，回本地图复活点
                if (GameManager.systemParamsList.GetParamValueIntArrayByName("MainReliveCity").ToList<int>().IndexOf(client.ClientData.MapCode) >= 0)
                {
                    toMapCode = client.ClientData.MapCode;
                }*/
                int toMapCode = -1;
                toMapCode = Global.GetMapRealiveInfoByCode(client.ClientData.MapCode);

                // 保证能回到主城
                if (toMapCode <= -1)
                {
                    toMapCode = GameManager.MainMapCode;
                }
                else
                {
                    if (toMapCode == 0 || GameManager.ArenaBattleMgr.IsInArenaBattle(client))
                        toMapCode = GameManager.MainMapCode;
                    else if (toMapCode == 1)
                        toMapCode = client.ClientData.MapCode;
                }

                //现在没有坐牢机制
                /*if (client.ClientData.MapCode == Global.GetLaoFangMapCode()) //牢房中(死亡同时被传入牢房)死亡后，回城复活，是回牢房的得出生点，而不是扬州城
                {
                    toMapCode = Global.GetLaoFangMapCode();
                }*/

                if (toMapCode >= 0)
                {
                    GameMap gameMap = null;
                    if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                    {
                        int defaultBirthPosX = GameManager.MapMgr.DictMaps[toMapCode].DefaultBirthPosX;
                        int defaultBirthPosY = GameManager.MapMgr.DictMaps[toMapCode].DefaultBirthPosY;
                        int defaultBirthRadius = GameManager.MapMgr.DictMaps[toMapCode].BirthRadius;

                        //从配置根据地图取默认位置
                        Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, toMapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);
                        posX = (int)newPos.X;
                        posY = (int)newPos.Y;

                        client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                        client.ClientData.CurrentMagicV = client.ClientData.MagicV;

                        client.ClientData.MoveAndActionNum = 0;

                        //通知队友自己要复活
                        GameManager.ClientMgr.NotifyTeamRealive(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.ClientData.RoleID, posX, posY, client.ClientData.RoleDirection);

                        //马上通知切换地图---->这个函数每次调用前，如果地图未发生发变化，则直接通知其他人自己位置变动
                        //比如在扬州城死 回 扬州城复活，就是位置变化
                        if (toMapCode != client.ClientData.MapCode)
                        {
                            //通知自己要复活
                            GameManager.ClientMgr.NotifyMySelfRealive(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, client.ClientData.RoleID, client.ClientData.PosX, client.ClientData.PosY, client.ClientData.RoleDirection);

                            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, toMapCode, posX, posY, -1, 1);
                        }
                        else
                        {
                            Global.ClientRealive(client, posX, posY, client.ClientData.RoleDirection);
                            //NotifyMySelfRealive
                        }

                        //LogManager.WriteLog(LogTypes.Error, string.Format("成功处理复活通知1, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                        return;
                    }
                }

                //LogManager.WriteLog(LogTypes.Error, string.Format("成功处理复活通知2, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                return;
            }
        }

        /// <summary>
        /// 处理角色的后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoSpriteWorks(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            //long startTicks = TimeUtil.NOW();

            // 更新在线时长
            DoSpriteHeart(sl, pool, client);

            //自动挂机的调度
            DoSpriteAutoFight(sl, pool, client);

            // 处理打坐收益
            DoSpriteSitExp(sl, pool, client);

            //处理消减PK点
            DoSpriteSubPKPoint(sl, pool, client);

            //播报紫名的消失事件
            BroadcastRolePurpleName(sl, pool, client);

            //执行角色排队的命令
            Global.ProcessQueueCmds(client);

            //处理通知角斗场称号的超时
            ProcessRoleBattleNameInfoTimeOut(sl, pool, client);

            //广播改变的临时坐骑的ID
            JugeTempHorseID(client);

            //重新计算按照日来判断的登录次数
            ChangeDayLoginNum(client);

            //处理角色的心跳时间, 如果超时，则执行清除工作
            Global.ProcessClientHeart(client);

            //处理角色的地图限制字段
            DoSpriteMapLimitTimes(sl, pool, client);

            //处理地图限时
            DoSpriteMapTimeLimit(client);

            //提示客户端更新版本
            DoSpriteHintToUpdateClient(client);

            //处理物品使用限时
           // DoSpriteGoodsTimeLimit(client);

            // 冥想计时处理 [3/18/2014 LiaoWei]
            DoSpriteMeditateTime(client);

            //处理超时的死亡状态,强制他复活
            DoSpriteDeadTime(client);

            // 处理叹号的定时
            client._IconStateMgr.DoSpriteIconTicks(client);

            // 处理群邮件的定时
            GroupMailManager.CheckRoleGroupMail(client);

            // 处理梅林魔法书秘语 [XSea 2015/6/25]
            GameManager.MerlinMagicBookMgr.DoMerlinSecretTime(client);

            GetInterestingDataMgr.Instance().Update(client);

            //updateEveryDayData(client);
            //long endTicks = TimeUtil.NOW();
            //System.Diagnostics.Debug.WriteLine(string.Format("DoSpriteWorks 消耗: {0} 毫秒", endTicks - startTicks));
        }

        //更新用户的每日数据//是否需要锁？
        //public void updateEveryDayData(GameClient client)
        //{
        //    int today = TimeUtil.NowDateTime().DayOfYear;
        //    if (today == client.ClientData.EveryDayUpDate) return;
        //    client.ClientData.EveryDayUpDate = today;

        //    UnionPalaceManager.getInstance().initTodayData(client);
        //    PetSkillManager.getInstance().initTodayData(client);
        //}

        /// <summary>
        /// 角色后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoSpriteBackgourndWork(SocketListener sl, TCPOutPacketPool pool)
        {
            GameClient client = null;
            int index = 0;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientData.ClosingClientStep > 0)
                {
                    continue;
                }

                /// 处理角色的后台工作
                DoSpriteWorks(sl, pool, client);
            }
        }

        /// <summary>
        /// 处理角色buffers的后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoBuffersWorks(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            // 补血补魔
            DoSpriteLifeMagic(sl, pool, client);

            //处理DBBuffer中的项
            DoSpriteBuffers(sl, pool, client);

            //定时检查道术隐身装备
            CheckDSHideState(client);
        }

        /// <summary>
        /// 处理角色Extension的后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoBuffersExtension(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            long nowTicks = TimeUtil.NOW();

            //执行多段攻击的操作
            if (GameManager.FlagManyAttackOp)
                SpriteAttack.ExecMagicsManyTimeDmageQueueEx(client);
            else
                SpriteAttack.ExecMagicsManyTimeDmageQueue(client);

            //Buff类
            client.bufferPropsManager.TimerUpdateProps(nowTicks);

            //延时执行的代码
            client.delayExecModule.ExecDelayProcs(client);

            // 执行所有杂项项
            SpriteMagicHelper.ExecuteAllItems(client);
        }

        /// <summary>
        /// 处理角色DB的后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoDBWorks(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            // 和DBserver保持数据更新
            DoSpriteDBHeart(sl, pool, client);

            //执行指定的数据库命令
            Global.ProcessDBCmdByTicks(client, false);

            //执行指定的技能数据库命令
            Global.ProcessDBSkillCmdByTicks(client, false);

            //执行指定的角色参数数据库命令
            Global.ProcessDBRoleParamCmdByTicks(client, false);

            //执行指定的装备耐久度数据库命令
            Global.ProcessDBEquipStrongCmdByTicks(client, false);
        }

        /// <summary>
        /// 角色Buffers后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoSpriteBuffersWork(SocketListener sl, TCPOutPacketPool pool)
        {
            GameClient client = null;
            int index = 0;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientData.ClosingClientStep > 0)
                {
                    continue;
                }

                /// 处理角色的后台工作
                DoBuffersWorks(sl, pool, client);
            }
        }

        /// <summary>
        /// 角色Extension后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoSpriteExtensionWork(SocketListener sl, TCPOutPacketPool pool, int nThead, int nMaxThread)
        {
            GameClient client = null;
            int index = 0;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (nMaxThread > 0)
                {
                    //判断如果角色的角色ID的2的模是否等于线程编号
                    if (client.ClientData.RoleID % nMaxThread != nThead)
                    {
                        continue;
                    }
                }
                if (client.ClientData.ClosingClientStep > 0)
                {
                    continue;
                }

                /// 处理角色的扩展工作
                DoBuffersExtension(sl, pool, client);
            }
        }

        /// <summary>
        /// 角色Extension后台工作，分地图
        /// </summary>
        /// <param name="client"></param>
        public void DoSpriteExtensionWorkByPerMap(int mapCode = -1, int subMapCode = -1)
        {
            SocketListener sl = Global._TCPManager.MySocketListener;
            TCPOutPacketPool tp = Global._TCPManager.TcpOutPacketPool;

            List<Object> mapClients = GameManager.ClientMgr.GetMapClients(mapCode);
            if (null == mapClients || mapClients.Count == 0)
            {
                return;
            }

            foreach (Object obj in mapClients)
            {
                if (null == obj)
                {
                    continue;
                }

                GameClient client = obj as GameClient;
                if (null == client)
                {
                    continue;
                }

                // 只处理当前副本地图的
                if (subMapCode >= 0 && client.ClientData.CopyMapID != subMapCode)
                {
                    continue;
                }

                if (client.ClientData.ClosingClientStep > 0)
                {
                    continue;
                }

                /// 处理角色的扩展工作
                DoBuffersExtension(sl, tp, client);
            }
        }

        /// <summary>
        /// 角色DB指令后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoSpriteDBWork(SocketListener sl, TCPOutPacketPool pool)
        {
            GameClient client = null;
            int index = 0;
            while ((client = GetNextClient(ref index)) != null)
            {
                if (client.ClientData.ClosingClientStep > 0)
                {
                    continue;
                }

                /// 处理角色DB的后台工作
                DoDBWorks(sl, pool, client);
            }
        }

        /// <summary>
        /// 角色的定时舞台对象刷新后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoSpritesMapGridMove(int nThead)
        {
            if (GameManager.Update9GridUsingPosition > 0)
            {
                return;
            }

            GameClient client = null;
            int index = 0;
            while ((client = GetNextClient(ref index)) != null)
            {
                //判断如果角色的角色ID的2的模是否等于线程编号
                if (client.ClientData.RoleID % Program.MaxGird9UpdateWorkersNum != nThead)
                {
                    continue;
                }

                //处理角色的地图移动延迟刷新
                DoSpriteMapGridMove(client);

                //故意降低cpu消耗
                if (GameManager.MaxSleepOnDoMapGridMoveTicks > 0)
                {
                    Thread.Sleep(GameManager.MaxSleepOnDoMapGridMoveTicks);
                }
            }
        }

        /// <summary>
        /// 角色的定时舞台对象刷新后台工作
        /// </summary>
        /// <param name="client"></param>
        public void DoSpritesMapGridMoveNewMode(int nThead)
        {
            GameClient client = null;
            int index = 0;
            while ((client = GetNextClient(ref index)) != null)
            {
                //判断如果角色的角色ID的2的模是否等于线程编号
                if (client.ClientData.RoleID % Program.MaxGird9UpdateWorkersNum != nThead)
                {
                    continue;
                }

                //处理角色的地图移动延迟刷新
                Global.GameClientMoveGrid(client);
            }
        }

        #endregion 后台工作线程调用方法

        #region 血色堡垒

        /// <summary>
        /// 通知在线的所有人(仅限在血色堡垒地图上)血色堡垒邀请消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBloodCastleMsg(SocketListener sl, TCPOutPacketPool pool, int mapCode, int nCmdID, int nTimer = 0, int nValue = 0, int nType = 0, int nPlayerNum = 0)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode); //发送给所有地图的用户
            if (null == objsList)
                return;

            string strcmd = "";

            if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEBEGINFIGHT)
            {
                strcmd = string.Format("{0}:{1}", mapCode, nTimer);  // 1.mapID 2.时间(秒)
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS)
            {
                strcmd = string.Format("{0}:{1}", nValue, nType);
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPLAYERNUMNOTIFY)
            {
                strcmd = string.Format("{0}", nPlayerNum);
            }


            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, nCmdID);
        }

        /// <summary>
        /// 通知在线的所有人(仅限在血色堡垒地图上)血色堡垒邀请消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBloodCastleCopySceneMsg(SocketListener sl, TCPOutPacketPool pool, CopyMap mapInfo, int nCmdID, int nTimer = 0, int nValue = 0, int nType = 0, int nPlayerNum = 0, GameClient client = null)
        {
            List<GameClient> objsList = mapInfo.GetClientsList(); //发送给所有地图的用户
            if (null == objsList)
                return;

            string strcmd = "";

            if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEBEGINFIGHT)
            {
                strcmd = string.Format("{0}:{1}", mapInfo.FubenMapID, nTimer);  // 1.mapID 2.时间(秒)
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS)
            {
                strcmd = string.Format("{0}:{1}", nValue, nType);
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPLAYERNUMNOTIFY)
            {
                strcmd = string.Format("{0}", nPlayerNum);
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPREPAREFIGHT)
            {
                BloodCastleDataInfo bcDataTmp = null;

                if (!Data.BloodCastleDataInfoList.TryGetValue(mapInfo.FubenMapID, out bcDataTmp) || bcDataTmp == null)
                    return;

                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", mapInfo.FubenMapID, nTimer, bcDataTmp.NeedKillMonster1Num, 1, bcDataTmp.NeedKillMonster2Num, 1, 1, 1);
            }

            //群发消息
            for (int i = 0; i < objsList.Count; i++)
            {
                if (objsList[i] != client)
                {
                    SendToClient(sl, pool, objsList[i], strcmd, nCmdID);
                }
            }

            if (null != client)
            {
                SendToClient(sl, pool, client, strcmd, nCmdID);
            }
        }

        /// <summary>
        /// 通知在线的所有人(仅限在血色堡垒地图上)血色堡垒结束战斗
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBloodCastleCopySceneMsgEndFight(SocketListener sl, TCPOutPacketPool pool, CopyMap mapInfo, BloodCastleScene bcTmp, int nCmdID, int nTimer, int nTimeAward)
        {
            string strcmd = "";

            BloodCastleDataInfo bcDataTmp = null;

            //bcDataTmp = Data.BloodCastleDataInfoList[mapCode];
            if (!Data.BloodCastleDataInfoList.TryGetValue(mapInfo.FubenMapID, out bcDataTmp))
                return;

            if (bcTmp == null || bcDataTmp == null)
                return;

            bcTmp.m_bEndFlag = true;

            List<GameClient> objsList = mapInfo.GetClientsList(); //发送给所有地图的用户
            if (null == objsList)
                return;

            for (int i = 0; i < objsList.Count; ++i)
            {
                GameClient client = objsList[i];

                if (client.ClientData.FuBenID > 0 && !GameManager.BloodCastleCopySceneMgr.IsBloodCastleCopyScene(client.ClientData.FuBenID))
                    continue;

                string AwardItem1 = null;
                string AwardItem2 = null;

                client.ClientData.BloodCastleAwardPoint += nTimeAward;
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastlePlayerPoint, client.ClientData.BloodCastleAwardPoint, true);

                if (client.ClientData.RoleID == bcTmp.m_nRoleID)
                {
                    for (int j = 0; j < bcDataTmp.AwardItem1.Length; ++j)
                    {
                        AwardItem1 += bcDataTmp.AwardItem1[j];
                        if (j != bcDataTmp.AwardItem1.Length - 1)
                            AwardItem1 += "|";
                    }
                }

                for (int n = 0; n < bcDataTmp.AwardItem2.Length; ++n)
                {
                    AwardItem2 += bcDataTmp.AwardItem2[n];
                    if (n != bcDataTmp.AwardItem2.Length - 1)
                        AwardItem2 += "|";
                }

                int nFlag = 0;
                if (bcTmp.m_bIsFinishTask)
                    nFlag = 1;

                // 保存完成状态
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastleSceneFinishFlag, nFlag, true);

                // 1.离场倒计时开始 2.是否成功完成 3.玩家的积分 4.玩家经验奖励 5.玩家的金钱奖励 6.玩家物品奖励1(只有提交大天使武器的玩家才有 其他人为null) 7.玩家物品奖励2(通用奖励 大家都有的)
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", nTimer, nFlag, client.ClientData.BloodCastleAwardPoint, Global.CalcExpForRoleScore(client.ClientData.BloodCastleAwardPoint, bcDataTmp.ExpModulus),
                                        client.ClientData.BloodCastleAwardPoint * bcDataTmp.MoneyModulus, AwardItem1, AwardItem2);

                GameManager.ClientMgr.SendToClient(client, strcmd, nCmdID);
            }

        }

        #endregion 血色堡垒

        #region 恶魔广场

        /// <summary>
        /// 恶魔广场广播信息(仅限在恶魔广场地图上)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDaimonSquareMsg(SocketListener sl, TCPOutPacketPool pool, int mapCode, int nCmdID, int nSection, int nTimer, int nWave, int nNum, int nPlayerNum)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode); //发送给所有地图的用户
            if (null == objsList)
                return;

            string strcmd = "";

            if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUAREMONSTERWAVEANDPOINTRINFO)
            {
                strcmd = string.Format("{0}:{1}", nWave, nNum);
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUARETIMERINFO)
            {
                strcmd = string.Format("{0}:{1}", nSection, nTimer);
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_DAIMONSQUAREPLAYERNUMNOTIFY)
            {
                strcmd = string.Format("{0}", nPlayerNum);
            }

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, nCmdID);
        }

        /// <summary>
        /// 通知在线的所有人(仅限在血色堡垒地图上)血色堡垒邀请消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDaimonSquareCopySceneMsg(SocketListener sl, TCPOutPacketPool pool, CopyMap mapInfo, int nCmdID, int nTimer = 0, int nValue = 0, int nType = 0, int nPlayerNum = 0)
        {
            List<GameClient> objsList = mapInfo.GetClientsList(); //发送给所有地图的用户
            if (null == objsList)
                return;

            string strcmd = "";

            if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_DAIMONSQUAREPLAYERNUMNOTIFY)
            {
                strcmd = string.Format("{0}", nPlayerNum);
            }

            //群发消息
            for (int i = 0; i < objsList.Count; i++)
                SendToClient(sl, pool, objsList[i], strcmd, nCmdID);
        }

        /// <summary>
        /// 恶魔广场副本广播信息(仅限在恶魔广场地图上)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDaimonSquareCopySceneMsg(SocketListener sl, TCPOutPacketPool pool, CopyMap mapInfo, int nCmdID, int nSection, int nTimer, int nWave, int nNum, int nPlayerNum)
        {
            List<GameClient> objsList = mapInfo.GetClientsList();
            if (null == objsList)
                return;

            string strcmd = "";

            if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUAREMONSTERWAVEANDPOINTRINFO)
            {
                strcmd = string.Format("{0}:{1}", nWave, nNum);
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUARETIMERINFO)
            {
                strcmd = string.Format("{0}:{1}", nSection, nTimer);
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_DAIMONSQUAREPLAYERNUMNOTIFY)
            {
                strcmd = string.Format("{0}", nPlayerNum);
            }

            //群发消息
            for (int i = 0; i < objsList.Count; i++)
                SendToClient(sl, pool, objsList[i], strcmd, nCmdID);
        }

        /// <summary>
        /// 通知在线的所有人(仅限在血色堡垒地图上)血色堡垒结束战斗
        /// </summary>
        /// <param name="client"></param>
        public void NotifyDaimonSquareCopySceneMsgEndFight(SocketListener sl, TCPOutPacketPool pool, CopyMap mapInfo, DaimonSquareScene dsInfo, int nCmdID, int nTimeAward)
        {
            string strcmd = "";

            DaimonSquareDataInfo bcDataTmp = null;

            if (!Data.DaimonSquareDataInfoList.TryGetValue(mapInfo.FubenMapID, out bcDataTmp))
                return;

            if (dsInfo == null || bcDataTmp == null)
                return;

            dsInfo.m_bEndFlag = true;

            List<GameClient> objsList = mapInfo.GetClientsList(); //发送给所有地图的用户
            if (null == objsList)
                return;


            for (int i = 0; i < objsList.Count; ++i)
            {
                if (!(objsList[i] is GameClient))
                    continue;

                GameClient client = (objsList[i] as GameClient);

                if (client.ClientData.FuBenID > 0 && !GameManager.DaimonSquareCopySceneMgr.IsDaimonSquareCopyScene(client.ClientData.FuBenID))
                    continue;

                string sAwardItem = null;

                client.ClientData.DaimonSquarePoint += nTimeAward;
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquarePlayerPoint, client.ClientData.DaimonSquarePoint, true);

                for (int n = 0; n < bcDataTmp.AwardItem.Length; ++n)
                {
                    sAwardItem += bcDataTmp.AwardItem[n];
                    if (n != bcDataTmp.AwardItem.Length - 1)
                        sAwardItem += "|";
                }

                int nFlag = 0;
                if (dsInfo.m_bIsFinishTask)
                    nFlag = 1;

                // 1.是否成功完成 2.玩家的积分 3.玩家经验奖励 4.玩家的金钱奖励 5.玩家物品奖励
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", nFlag, client.ClientData.DaimonSquarePoint, Global.CalcExpForRoleScore(client.ClientData.DaimonSquarePoint, bcDataTmp.ExpModulus),
                                        client.ClientData.DaimonSquarePoint * bcDataTmp.MoneyModulus, sAwardItem);

                GameManager.ClientMgr.SendToClient(client, strcmd, nCmdID);
            }
        }

        #endregion 恶魔广场

        #region 天使神殿

        /// <summary>
        /// 通知在线的所有人(仅限在天使神殿地图上)一些信息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAngelTempleMsg(SocketListener sl, TCPOutPacketPool pool, int mapCode, int nCmdID, AngelTemplePointInfo[] array, int nSection, int nTimer = 0, int nValue = 0, int nType = 0, int nPlayerNum = 0, double nBossHP = 0)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode); //发送给所有地图的用户
            if (null == objsList)
                return;

            string strcmd = "";

            if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_ANGELTEMPLETIMERINFO)
            {
                strcmd = string.Format("{0}:{1}", nSection, nTimer);  // 1.哪个时间段 2.时间(秒)
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS)
            {
                strcmd = string.Format("{0}:{1}", nValue, nType);
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPLAYERNUMNOTIFY)
            {
                strcmd = string.Format("{0}", nPlayerNum);
            }
            else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_ANGELTEMPLEFIGHTINFOALL)
            {
                string sName1 = "";
                string sName2 = "";
                string sName3 = "";
                string sName4 = "";
                string sName5 = "";

                double dValue1 = 0.0;
                double dValue2 = 0.0;
                double dValue3 = 0.0;
                double dValue4 = 0.0;
                double dValue5 = 0.0;

                dValue1 = Math.Round(((double)array[0].m_DamagePoint / (double)GameManager.AngelTempleMgr.m_BossHP), 2);
                sName1 = array[0].m_RoleName;
                dValue2 = Math.Round(((double)array[1].m_DamagePoint / (double)GameManager.AngelTempleMgr.m_BossHP), 2);
                sName2 = array[1].m_RoleName;
                dValue3 = Math.Round(((double)array[2].m_DamagePoint / (double)GameManager.AngelTempleMgr.m_BossHP), 2);
                sName3 = array[2].m_RoleName;
                dValue4 = Math.Round(((double)array[3].m_DamagePoint / (double)GameManager.AngelTempleMgr.m_BossHP), 2);
                sName4 = array[3].m_RoleName;
                dValue5 = Math.Round(((double)array[4].m_DamagePoint / (double)GameManager.AngelTempleMgr.m_BossHP), 2);
                sName5 = array[4].m_RoleName;

                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}", Math.Round(nBossHP / GameManager.AngelTempleMgr.m_BossHP, 2), sName1, dValue1, sName2, dValue2, sName3, dValue3, sName4, dValue4, sName5, dValue5);
            }

            //群发消息
            SendToClients(sl, pool, null, objsList, strcmd, nCmdID);
        }

        /// <summary>
        /// 通知在线的所有人(仅限在天使神殿地图上)天使神殿Boss消失
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAngelTempleMsgBossDisappear(SocketListener sl, TCPOutPacketPool pool, int mapCode)
        {
            List<Object> objsList = Container.GetObjectsByMap(mapCode); //发送给所有地图的用户
            if (null == objsList)
                return;

            for (int i = 0; i < objsList.Count; ++i)
            {
                if (!(objsList[i] is GameClient))
                    continue;

                GameClient client = (objsList[i] as GameClient);

                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                        StringUtil.substitute(Global.GetLang("Boss未被击杀已自动消失")),
                                                                        GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
            }
        }

        #endregion 天使神殿

        #region 队伍广播
        /// <summary>
        /// 通知队伍有人加入、离开
        /// </summary>
        /// <param name="client"></param>
        public void NotifyTeamMemberMsg(SocketListener sl, TCPOutPacketPool pool, GameClient client, TeamData td, TeamCmds nCmd)
        {
            if (null != td)
            {
                lock (td)
                {
                    for (int i = 0; i < td.TeamRoles.Count; i++)
                    {
                        GameClient gameClient = FindClient(td.TeamRoles[i].RoleID);
                        if (null == gameClient)
                            continue;

                        if (nCmd == TeamCmds.Quit)
                        {
                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, gameClient,
                                                                        StringUtil.substitute(Global.GetLang("『{0}』离开了队伍"), client.ClientData.RoleName),
                                                                        GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, (int)HintErrCodeTypes.None);
                        }
                        else if (nCmd == TeamCmds.AgreeApply)
                        {
                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, gameClient,
                                                                        StringUtil.substitute(Global.GetLang("『{0}』加入了队伍"), client.ClientData.RoleName),
                                                                        GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, (int)HintErrCodeTypes.None);
                        }


                    }
                }
            }

        }

        #endregion 队伍广播

        #region 取得地图当中的玩家列表

        /// <summary>
        /// 根据玩家所在地图 取得所有玩家
        /// </summary>
        /// <param name="client"></param>
        public List<Object> GetPlayerByMap(GameClient client)
        {
            List<Object> newObjList = null;

            newObjList = Container.GetObjectsByMap(client.ClientData.MapCode);

            return newObjList;
        }
        #endregion 取得地图当中的玩家列表

        #region 仓库货币处理

        /// <summary>
        /// 银两通知(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfUserStoreYinLiangChange(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.StoreYinLiang);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_STORE_YINLIANG_CHANGE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
            }
        }

        /// <summary>
        /// 银两通知(只通知自己)
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfUserStoreMoneyChange(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.StoreMoney);
            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_STORE_MONEY_CHANGE);
            if (!sl.SendData(client.ClientSocket, tcpOutPacket))
            {
            }
        }

        /// <summary>
        /// 添加用户仓库金币
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddUserStoreYinLiang(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, long addYinLiang, string strFrom)
        {
            if (0 == addYinLiang)
            {
                return true;
            }

            long oldYinLiang = client.ClientData.StoreYinLiang;

            //先锁定
            lock (client.ClientData.StoreYinLiangMutex)
            {
                if (addYinLiang < 0 && oldYinLiang < Math.Abs(addYinLiang))
                {
                    return false;
                }

                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, addYinLiang); //只发增量
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_ADD_STORE_YINLIANG, strcmd, client.ServerId);
                if (null == dbFields) return false;
                if (dbFields.Length != 2)
                {
                    return false;
                }

                // 先锁定
                if (Convert.ToInt64(dbFields[1]) < 0)
                {
                    return false; //银两添加失败
                }

                // 先锁定
                client.ClientData.StoreYinLiang = Convert.ToInt64(dbFields[1]);

                // 更新通知(只通知自己)
                GameManager.ClientMgr.NotifySelfUserStoreYinLiangChange(sl, pool, client);
                if (0 != addYinLiang)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "仓库金币", strFrom, "系统", client.ClientData.RoleName, "增加", (int)addYinLiang, client.ClientData.ZoneID, client.strUserID, (int)client.ClientData.StoreYinLiang, client.ServerId);
                    EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.StoreYinLiang, addYinLiang, client.ClientData.StoreYinLiang, strFrom);
                }
            }

            //写入角色银两增加/减少日志
            Global.AddRoleStoreYinLiangEvent(client, oldYinLiang);

            return true;
        }

        /// <summary>
        /// 添加用户仓库绑定金币
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="subMoney"></param>
        /// <returns></returns>
        public bool AddUserStoreMoney(SocketListener sl, TCPClientPool tcpClientPool, TCPOutPacketPool pool, GameClient client, long addMoney, string strFrom)
        {
            if (0 == addMoney)
            {
                return true;
            }

            long oldMoney = client.ClientData.StoreMoney;

            //先锁定
            lock (client.ClientData.StoreMoneyMutex)
            {
                if (addMoney < 0 && oldMoney < Math.Abs(addMoney))
                {
                    return false;
                }

                //先DBServer请求扣费
                string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, addMoney); //只发增量
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_ADD_STORE_MONEY, strcmd, client.ServerId);
                if (null == dbFields) return false;
                if (dbFields.Length != 2)
                {
                    return false;
                }

                // 先锁定
                if (Convert.ToInt64(dbFields[1]) < 0)
                {
                    return false; //银两添加失败
                }

                // 先锁定
                client.ClientData.StoreMoney = Convert.ToInt64(dbFields[1]);

                if (0 != addMoney)
                {
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "仓库绑定金币", strFrom, "系统", client.ClientData.RoleName, "增加", (int)addMoney, client.ClientData.ZoneID, client.strUserID, (int)client.ClientData.StoreMoney, client.ServerId);
                }
            }

            // 更新通知(只通知自己)
            GameManager.ClientMgr.NotifySelfUserStoreMoneyChange(sl, pool, client);

            //写入角色银两增加/减少日志
            Global.AddRoleStoreMoneyEvent(client, oldMoney);

            return true;
        }

        #endregion

        /// <summary>
        /// 通知所有人节日/合服开启关闭状态
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllActivityState(int type, int state, string activityTimeBegin = "", string activityTimeEnd = "", int activityID = 0)
        {
            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", type, state, activityTimeBegin, activityTimeEnd, activityID);
                TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_JIERIACT_STATE);
                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                {
                }
            }
        }

        /// <summary>
        /// 在线所有角色重新生成专属活动数据
        /// </summary>
        public void ReGenerateSpecActGroup()
        {
            SpecialActivity act = HuodongCachingMgr.GetSpecialActivity();
            if (null == act)
                return;

            int index = 0;
            GameClient client = null;
            while ((client = GetNextClient(ref index)) != null)
            {
                act.OnRoleLogin(client, false);

                // 感叹号
                if (client._IconStateMgr.CheckSpecialActivity(client))
                    client._IconStateMgr.SendIconStateToClient(client);
            }
        }
    }
}
