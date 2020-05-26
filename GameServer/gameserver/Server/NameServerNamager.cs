using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Server.Protocol;
using GameServer.Logic;
using Server.Tools;
using System.Xml.Linq;
using System.Collections.Specialized;
using Server.Data;
using System.Net;
using System.IO;

namespace GameServer.Server
{
    public class NameServerData
    {
        public int ID;
        public string Host;
        public int Port;

        public string ResolvedHost;
        public string ResolvedIP;
    }

    /// <summary>
    /// 要发送的指令的队列项封装
    /// </summary>
    public static class NameServerNamager
    {
        #region 成员函数

        public static class NameErrorCodes
        {
            public const int ErrorSuccess = 1; //成功,这个值不会直接返回给客户端,而是根据具体逻辑返回特定的值
            public const int ErrorServerDisabled = -2; //服务器禁止创建
            public const int ErrorInvalidCharacter = -3; //名字包含特殊字符
            public const int ErrorNameHasBeUsed = -4; //名字已经被占用
        }

        public static string NameServerIP;
        public static int NameServerPort;
        public static int NameServerConfig;
        public static string ServerPingTaiID;
        public static string PingTaiID;
        private static char[] InvalidCharacters = { '<', '>', '\\', '\'', '"', '=', '%', '\t', '\b', '\r', '\n', '○', '●', '|', '$', '{', '}',};
        private static string[] InvalidSqlStrings = { "--", };

        private static NameServerData DefaultServerData = new NameServerData();
        private static Dictionary<RangeKey, NameServerData> ZoneID2NameServerDict = new Dictionary<RangeKey, NameServerData>(RangeKey.Comparer);

        public static void Init(XElement xml)
        {
            if (GameManager.FlagDisableNameServer)
            {
                Global.Flag_NameServer = false;
                return;
            }
            NameServerIP = Global.GetSafeAttributeStr(xml, "NameServer", "IP");
            NameServerPort = (int)Global.GetSafeAttributeLong(xml, "NameServer", "Port");
            ServerPingTaiID = Global.GetSafeAttributeStr(xml, "NameServer", "PingTaiID");
            if (!int.TryParse(ServerPingTaiID, out NameServerConfig) || NameServerConfig > 1)
            {
                NameServerConfig = 1;
            }

            DefaultServerData.Host = NameServerIP;
            DefaultServerData.Port = NameServerPort;
            GetIPV4IP(DefaultServerData);

            if (!File.Exists("NameServer.xml"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("警告:没有名字服务器列表文件(NameServer.xml)");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            XElement xmlNameServer = XElement.Load("NameServer.xml");
            foreach (var item in xmlNameServer.DescendantsAndSelf("Server"))
            {
                NameServerData serverData = new NameServerData();
                serverData.ID = (int)Global.GetSafeAttributeLong(item, "ID");
                serverData.Host = Global.GetSafeAttributeStr(item, "host");
                serverData.Port = (int)Global.GetSafeAttributeLong(item, "port");
                int start = (int)Global.GetSafeAttributeLong(item, "start");
                int end = (int)Global.GetSafeAttributeLong(item, "end");
                RangeKey range = new RangeKey(start, end);
                ZoneID2NameServerDict.Add(range, serverData);
            }
        }

        public static string GetIPV4IP(NameServerData serverData)
        {
            try
            {
                IPAddress ip;
                if (IPAddress.TryParse(serverData.Host, out ip))
                {
                    return ip.ToString();
                }
                //否则解析域名
                IPHostEntry hostEntry = Dns.GetHostEntry(serverData.Host);
                if (hostEntry.AddressList.Length >= 0)
                {
                    for (int i = 0; i < hostEntry.AddressList.Length; i++)
                    {
                        //必须是IPV4的地址
                        if (hostEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                        {
                            serverData.ResolvedHost = serverData.Host;
                            serverData.ResolvedIP = hostEntry.AddressList[i].ToString();
                            return serverData.ResolvedIP;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException("解析名字服务器域名异常: " + ex.ToString());
            }

            if (serverData.ResolvedHost == serverData.Host)
            {
                return serverData.ResolvedIP;
            }

            return null;
        }

        public static int CheckInvalidCharacters(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                return NameErrorCodes.ErrorInvalidCharacter;
            }
            if (param.IndexOfAny(InvalidCharacters) >= 0)
            {
                return NameErrorCodes.ErrorInvalidCharacter;
            }

            for (int i = 0; i < InvalidSqlStrings.Length; i++)
            {
                if (param.IndexOf(InvalidSqlStrings[i]) >= 0)
                    return NameErrorCodes.ErrorInvalidCharacter;
            }

            return NameErrorCodes.ErrorSuccess;
        }

        public static int RegisterNameToNameServer(int zoneID, string userID, string[] nameAndPingTaiID, int type, int roleID = 0)
        {
            if (GameManager.FlagDisableNameServer || !Global.Flag_NameServer || NameServerConfig == -1)
            {
                return NameErrorCodes.ErrorSuccess; //未配置名字服务器则允许注册且不到名字服务器验证
            }

            string name = nameAndPingTaiID[0];
            string pingTai;
            if(NameServerConfig == 1)
            {
                pingTai = ServerPingTaiID;
            }
            else if (nameAndPingTaiID.Length == 2)
            {
                pingTai = nameAndPingTaiID[1];
            }
            else
            {
                pingTai = "Global";
            }

            // 原来的检测放到函数外面
            /*int ret = CheckInvalidCharacters(name);
            if (ret <= 0)
            {
                return NameErrorCodes.ErrorInvalidCharacter;
            }*/

            if (NameServerConfig < 0)
            {
                if (NameServerConfig == -1)
                {
                    return NameErrorCodes.ErrorSuccess; //否则允许注册且不到名字服务器验证
                }
                else if (NameServerConfig == -2) //如果配置了-2则不允许注册,否则允许注册且不到名字服务器验证
                {
                    return NameErrorCodes.ErrorServerDisabled; //如果配置了-2则不允许注册
                }
            }
            if (zoneID < 0)
            {
                DataHelper.WriteStackTraceLog(string.Format("注册名字到名字服务器时区号不合法 zoneID={0} userID={1} name={2}", zoneID, userID, name));
                return NameErrorCodes.ErrorServerDisabled;
            }

            int ret = 0;
            TCPClient connection = null;
            try
            {
                connection = new TCPClient() { RootWindow = Program.ServerConsole, ListIndex = 100};
                NameRegisterData nameRegisterData = new NameRegisterData()
                {
                    Name = name,
                    PingTaiID = pingTai,
                    ZoneID = zoneID,
                    UserID = userID,
                    NameType = type,
                };

                string ip;
                NameServerData nameServerData;
                lock (ZoneID2NameServerDict)
                {
                    if (!ZoneID2NameServerDict.TryGetValue(zoneID, out nameServerData))
                    {
                        nameServerData = DefaultServerData;
                    }
                }
                ip = GetIPV4IP(nameServerData);
                if (null == ip)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("解析名字服务器IP失败 zoneID={0} host={1} NameServerID={2}", zoneID, nameServerData.Host, nameServerData.ID));
                    return NameErrorCodes.ErrorServerDisabled;
                }

                connection.Connect(ip, nameServerData.Port, "NameServer" + nameServerData.ID + 1000);
                ret = Global.SendToNameServer<NameRegisterData, int>(connection, (int)TCPGameServerCmds.CMD_NAME_REGISTERNAME, nameRegisterData);
                if (ret == 0)
                {
                    ret = NameErrorCodes.ErrorNameHasBeUsed;
                }
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, string.Format("注册名字到名字服务器时发生错误 zoneID={0} userID={1} name={2}", zoneID, userID, name), false);
                ret = NameErrorCodes.ErrorServerDisabled;
            }

            if (null != connection)
            {
                connection.Disconnect();
            }
            return ret;
        }

        #endregion 成员函数
    }
}
