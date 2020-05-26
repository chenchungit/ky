using GameServer.Core.Executor;
using GameServer.Server;
using GameServer.Tools;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic
{
    public class WarnManager : IManager
    {
        #region ----------接口

        private static WarnManager instance = new WarnManager();
        public static WarnManager getInstance() { return instance; }

        public bool initialize() 
        {
            initWarnInfo();

            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("WarnManager.WarnCloseClient()", WarnCloseClient), 5 * 1000, 5 * 1000);
            return true;
        }

        public bool startup() { return true; }
        public bool showdown() { return true; }
        public bool destroy() { return true; }

        #endregion

        #region 配置信息

        /// <summary>
        ///警告信息
        /// </summary>
        private static Dictionary<int, WarnInfo> _warnInfoList = new Dictionary<int, WarnInfo>();

        /// <summary>
        /// 加载警告配置
        /// </summary>
        public static void initWarnInfo()
        {
            string fileName = Global.IsolateResPath("Config/JingGao.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _warnInfoList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    WarnInfo config = new WarnInfo();
                    config.ID = Convert.ToInt32(Global.GetSafeAttributeLong(xmlItem, "ID"));
                    config.Desc = Global.GetSafeAttributeStr(xmlItem, "Description");
                    config.TimeSec = Convert.ToInt32(Global.GetSafeAttributeLong(xmlItem, "Time"));
                    config.Operate = Convert.ToInt32(Global.GetSafeAttributeLong(xmlItem, "Operate"));

                    _warnInfoList.Add(config.Operate, config);
                }
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }
        }

        private static WarnInfo GetWarnInfo(int warnType)
        {
            if (_warnInfoList.ContainsKey(warnType))
                return _warnInfoList[warnType];

            return null;
        }

        #endregion

        /// <summary>
        /// 警告处理
        /// </summary>
        /// <param name="roleName">角色name</param>
        /// <param name="warnType">警告类型</param>
        public static void WarnProcess(string userID, int warnType)
        {
            WarnInfo info = GetWarnInfo(warnType);
            if (info == null) return;

            TMSKSocket socket = GameManager.OnlineUserSession.FindSocketByUserID(userID);
            if (socket == null) return;

            GameClient client = GameManager.ClientMgr.FindClient(socket);
            if (null != client)
            {
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WARN_INFO, info);
                if (info.Operate != (int)WarnOperateType.Hand)
                {
                    AddTaskToHashSet(client, info.TimeSec);
                }
            }
            /*/根据ID查找敌人
            int roleID = RoleName2IDs.FindRoleIDByName(roleName, true);
            if (roleID <= 0) return;

            GameClient client = GameManager.ClientMgr.FindClient(roleID);*/     
        }

        private static object _lock = new object();
        private static Dictionary<GameClient, DateTime> _clientList = new Dictionary<GameClient, DateTime>();
        private static void AddTaskToHashSet(GameClient client,int time)
        {
            lock (_lock)
            {
                if (_clientList.ContainsKey(client))
                    return;

                _clientList.Add(client, DateTime.Now.AddSeconds(time));
                if (_clientList.Count >= 3000)
                    WarnCloseClient(null, null);
            }
        }

        public static void WarnCloseClient(object sender, EventArgs e)
        {
            try
            {
                Dictionary<GameClient, DateTime> dic = new Dictionary<GameClient, DateTime>();
                List<GameClient> list = new List<GameClient>();

                lock (_lock)
                {
                    foreach (KeyValuePair<GameClient, DateTime> c in _clientList)
                    {
                        DateTime endTime = c.Value;
                        if (DateTime.Now >= endTime)
                            list.Add(c.Key);
                        else
                            dic.Add(c.Key, c.Value);
                    }

                    _clientList.Clear();
                    _clientList = dic;
                }

                foreach (GameClient client in list)
                {
                    Global.ForceCloseClient(client, "warn踢人");
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, ex.Message);
            }
        }

        //
    }
}
