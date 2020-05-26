using System;
using System.Text;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Security.Cryptography;
using Server.TCP;
using Server.Protocol;
using GameServer.Server;
using ComponentAce.Compression.Libs.zlib;
using Tmsk.Contract;
using Server.Tools;
using System.IO;
using GameServer.Core.Executor;
using Server.Data;
using System.Collections.Concurrent;
using Tmsk.Tools.Tools;
using GameServer.Tools;

namespace GameServer.Logic
{

    class RobotTaskValidator
    {
        private RobotTaskValidator() { }
        private static RobotTaskValidator instance = new RobotTaskValidator();
        public static RobotTaskValidator getInstance() { return instance; }
        private object m_Mutex = new object();

        #region 加密初始，记录进程，配置

        public bool Initialize(bool client, int seed, int randomCount, string pubKey)
        {
            return true;
        }

        public bool LoadRobotTaskData()
        {
            return true;
        }

        #endregion



        public void RobotDataReset(GameClient client)
        {
            if (client == null) return;

            client.CheckCheatData.RobotTaskListData = "";

            client.CheckCheatData.BanCheckMaxCount = 0;
            client.CheckCheatData.KickWarnMaxCount = 0;

            client.CheckCheatData.DropRateDown = false;
            client.CheckCheatData.KickState = false;

            client.CheckCheatData.RobotDetectedKickTime = 0;
            client.CheckCheatData.RobotDetectedReason = "";

            client.CheckCheatData.NextTaskListTimeout = 0;

            client.CheckCheatData.LogCountDic = new Dictionary<int, int>();
        }


        public string GetIp(GameClient client)
        {
            int _canLogIp = 1;
            string ip = "0";
            switch (_canLogIp)
            {
                case 1:
                    ip = Global.GetIPAddress(client.ClientSocket);
                    break;
                case 2:
                    string ipStr = Global.GetIPAddress(client.ClientSocket);
                    ip = IpHelper.IpToInt(ipStr).ToString();
                    break;
            }

            return ip;
        }

    }
}