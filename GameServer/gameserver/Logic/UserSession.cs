using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Server.Data;
using Server.TCP;
using GameServer.Logic.SecondPassword;

namespace GameServer.Logic
{
    /// <summary>
    /// 用户在线管理类
    /// </summary>
    public class UserSession
    {
        /// <summary>
        /// TMSKSocket连接到用户ID的映射
        /// </summary>
        private Dictionary<TMSKSocket, string> _S2UDict = new Dictionary<TMSKSocket, string>(1000);

        /// <summary>
        /// 户ID到TMSKSocket连接的映射
        /// </summary>
        private Dictionary<string, TMSKSocket> _U2SDict = new Dictionary<string, TMSKSocket>(1000);

        /// <summary>
        /// TMSKSocket连接到用户名称的映射
        /// </summary>
        private Dictionary<TMSKSocket, string> _S2UNameDict = new Dictionary<TMSKSocket, string>(1000);

        /// <summary>
        /// 用户名称到TMSKSocket连接的映射
        /// </summary>
        private Dictionary<string, TMSKSocket> _UName2SDict = new Dictionary<string, TMSKSocket>(1000);

        /// <summary>
        /// TMSKSocket连接到是否成人判断的映射
        /// </summary>
        private Dictionary<TMSKSocket, int> _S2UAdultDict = new Dictionary<TMSKSocket, int>(1000);

        /// <summary>
        /// 获取所有socket连接
        /// </summary>
        /// <returns></returns>
        public List<TMSKSocket> GetSocketList()
        {
            lock (this)
            {
                return _S2UDict.Keys.ToList<TMSKSocket>();
            }
        }
        /// <summary>
        /// 添加一个在线的回话
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="userID"></param>
        public bool AddSession(TMSKSocket clientSocket, string userID)
        {
            lock (this)
            {
                string oldUserID = "";
                if (_S2UDict.TryGetValue(clientSocket, out oldUserID)) //已经存在
                {
                    return false;
                }

                TMSKSocket oldClientSocket = null;
                if (_U2SDict.TryGetValue(userID, out oldClientSocket)) //已经存在
                {
                    return false;
                }

                _S2UDict[clientSocket] = userID;
                _U2SDict[userID] = clientSocket;
            }

            return true;
        }

        /// <summary>
        /// 删除一个在线的回话
        /// </summary>
        /// <param name="clientSocket"></param>
        public void RemoveSession(TMSKSocket clientSocket)
        {
            if (null == clientSocket) return;
            string userID = "";
            lock (this)
            {
                if (_S2UDict.TryGetValue(clientSocket, out userID))
                {
                    _S2UDict.Remove(clientSocket);
                    _U2SDict.Remove(userID);
                }
            }
        }

        /// <summary>
        /// 根据TMSKSocket查找在线UserID
        /// </summary>
        /// <param name="clientSocket"></param>
        public string FindUserID(TMSKSocket clientSocket)
        {
            string userID = "";
            lock (this)
            {
                _S2UDict.TryGetValue(clientSocket, out userID);
            }

            return userID;
        }

        /// <summary>
        /// 根据UserID查找在线TMSKSocket
        /// </summary>
        /// <param name="clientSocket"></param>
        public TMSKSocket FindSocketByUserID(string userID)
        {
            TMSKSocket clientSocket = null;
            lock (this)
            {
                _U2SDict.TryGetValue(userID, out clientSocket);
            }

            return clientSocket;
        }

        /// <summary>
        /// 添加一个在线的用户名称
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="userID"></param>
        public void AddUserName(TMSKSocket clientSocket, string userName)
        {
            lock (this)
            {
                _S2UNameDict[clientSocket] = userName;
                _UName2SDict[userName] = clientSocket;
            }
        }

        /// <summary>
        /// 删除一个在线的用户名称
        /// </summary>
        /// <param name="clientSocket"></param>
        public void RemoveUserName(TMSKSocket clientSocket)
        {
            if (null == clientSocket) return;
            lock (this)
            {
                string userName = null;
                if (_S2UNameDict.TryGetValue(clientSocket, out userName))
                {
                    _S2UNameDict.Remove(clientSocket);
                    _UName2SDict.Remove(userName);
                }
            }
        }

        /// <summary>
        /// 根据TMSKSocket查找在线用户名称
        /// </summary>
        /// <param name="clientSocket"></param>
        public string FindUserName(TMSKSocket clientSocket)
        {
            string userName = null;
            lock (this)
            {
                _S2UNameDict.TryGetValue(clientSocket, out userName);
            }

            return userName;
        }

        /// <summary>
        /// 根据UserName查找在线TMSKSocket
        /// </summary>
        /// <param name="clientSocket"></param>
        public TMSKSocket FindSocketByUserName(string userName)
        {
            TMSKSocket clientSocket = null;
            lock (this)
            {
                _UName2SDict.TryGetValue(userName, out clientSocket);
            }

            return clientSocket;
        }

        /// <summary>
        /// 添加一个在线的用户是否成人标志
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="userID"></param>
        public void AddUserAdult(TMSKSocket clientSocket, int isAdult)
        {
            lock (this)
            {
                _S2UAdultDict[clientSocket] = isAdult;
            }
        }

        /// <summary>
        /// 删除一个在线的用户是否成人标志
        /// </summary>
        /// <param name="clientSocket"></param>
        public void RemoveUserAdult(TMSKSocket clientSocket)
        {
            if (null == clientSocket) return;
            lock (this)
            {
                _S2UAdultDict.Remove(clientSocket);
            }
        }

        /// <summary>
        /// 根据TMSKSocket查找在线用户成人标志
        /// </summary>
        /// <param name="clientSocket"></param>
        public int FindUserAdult(TMSKSocket clientSocket)
        {
            int isAdult = 0;
            lock (this)
            {
                _S2UAdultDict.TryGetValue(clientSocket, out isAdult);
            }

            return isAdult;
        }
    }
}
