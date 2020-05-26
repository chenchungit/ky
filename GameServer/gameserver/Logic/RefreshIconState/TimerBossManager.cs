using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;

using Server.Data;
using GameServer.Server;
using GameServer.Logic.JingJiChang;

namespace GameServer.Logic.RefreshIconState
{
    public class TimerBossData
    {
        /// <summary>
        /// 怪号编号
        /// </summary>
        public int nRoleID;

        /// <summary>
        /// 刷新所需玩家等级
        /// </summary>
        public int nReqLevel;

        /// <summary>
        ///刷新所需玩家转生等级
        /// </summary>
        public int nReqChangeLiveCount;
    }

    /// summary
    /// 世界BOSS，黄金部队管理类
	/// summary
    public class TimerBossManager
    {
        /// <summary>
        /// 世界BOSS字典
        /// </summary>
        private Dictionary<int, TimerBossData> m_WorldBossDict = new Dictionary<int, TimerBossData>();

        /// <summary>
        /// 黄金部队字典
        /// </summary>
        private Dictionary<int, TimerBossData> m_HuangJinBossDict = new Dictionary<int, TimerBossData>();

        /// <summary>
        /// 已经刷新的BOSS字典
        /// </summary>
        private Dictionary<int, int> m_LivedInMapBoss = new Dictionary<int, int>();

        /// summary
        /// 世界BOSS、黄金部队管理类单件
        /// summary
        private static TimerBossManager instance = null; 
        private TimerBossManager() 
        {
        }

        /// summary
        /// 装入世界BOSS信息
        /// summary
        private void LoadWorldBossInfo()
        {
            // 世界BOSS信息表
            try
            {
                XElement xmlFile = null;
                xmlFile = Global.GetGameResXml(string.Format("Config/Activity/BossInfo.xml"));
                if (null == xmlFile)
                    return;

                IEnumerable<XElement> WorldBossXEle = xmlFile.Elements("Boss");
                foreach (var xmlItem in WorldBossXEle)
                {
                    if (null != xmlItem)
                    {
                        SystemXmlItem systemXmlItem = new SystemXmlItem()
                        {
                            XMLNode = xmlItem,
                        };

                        TimerBossData tmpInfo = new TimerBossData();

                        tmpInfo.nRoleID = systemXmlItem.GetIntValue("ID");
                        int [] arrLevel = systemXmlItem.GetIntArrayValue("Level");
                        if (null == arrLevel || arrLevel.Length != 2)
                        {
                            // 填写错误则抛异常
                            systemXmlItem = null;
                            throw new Exception(string.Format("启动时加载xml文件: {0} 失败 Level格式错误", string.Format("Config/Activity/BossInfo.xml")));
                        }
                        else
                        {
                            tmpInfo.nReqLevel = arrLevel[1];
                            tmpInfo.nReqChangeLiveCount = arrLevel[0];
                        }

                        systemXmlItem = null;
                        m_WorldBossDict.Add(tmpInfo.nRoleID, tmpInfo);
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", string.Format("Config/Activity/BossInfo.xml")));
            }
        }

        /// summary
        /// 装入黄金部队信息
        /// summary
        private void LoadHuangJinBossInfo()
        {
            // 世界BOSS信息表
            try
            {
                XElement xmlFile = null;
                xmlFile = Global.GetGameResXml(string.Format("Config/HuangJin.xml"));
                if (null == xmlFile)
                    return;

                IEnumerable<XElement> WorldBossXEle = xmlFile.Elements("Boss");
                foreach (var xmlItem in WorldBossXEle)
                {
                    if (null != xmlItem)
                    {
                        SystemXmlItem systemXmlItem = new SystemXmlItem()
                        {
                            XMLNode = xmlItem,
                        };

                        TimerBossData tmpInfo = new TimerBossData();

                        tmpInfo.nRoleID = systemXmlItem.GetIntValue("ID");
                        int[] arrLevel = systemXmlItem.GetIntArrayValue("Level");
                        if (null == arrLevel || arrLevel.Length != 2)
                        {
                            // 填写错误则抛异常
                            systemXmlItem = null;
                            throw new Exception(string.Format("启动时加载xml文件: {0} 失败 Level格式错误", string.Format("Config/HuangJin.xml")));
                        }
                        else
                        {
                            tmpInfo.nReqLevel = arrLevel[1];
                            tmpInfo.nReqChangeLiveCount = arrLevel[0];
                        }

                        systemXmlItem = null;
                        m_HuangJinBossDict.Add(tmpInfo.nRoleID, tmpInfo);
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", string.Format("Config/HuangJin.xml")));
            }
        }

        /// <summary>
        /// 仅供初始化instance静态实例使用
        /// </summary>
        private static object Mutex = new object();

        /// summary
        /// 返回世界BOSS、黄金部队管理类单件对象
        /// summary
        public static TimerBossManager getInstance()
        {
            if (null == instance)
            {
                lock (Mutex)
                {
                    if (null == instance)
                    {
                        TimerBossManager inst = new TimerBossManager();
                        inst.LoadWorldBossInfo();
                        inst.LoadHuangJinBossInfo();
                        instance = inst;
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// 增加BOSS到管理列表
        /// </summary>
        public void AddBoss(int nBirthType, int nRoleID)
        {
            lock (m_LivedInMapBoss)
            {
                m_LivedInMapBoss[nRoleID] = nBirthType;
            }

            if (nBirthType == (int)MonsterBirthTypes.TimePoint)
            {
                // 刷新所有玩家“世界BOSS”图标状态
                RefreshWorldBoss();
            }
            else if (nBirthType == (int)MonsterBirthTypes.CreateDayOfWeek)
            {
                // 刷新所有玩家“黄金部队”图标状态
                RefreshHuangJinBoss();
            }
        }

        /// <summary>
        /// 从管理列表移除BOSS
        /// </summary>
        public void RemoveBoss(int nRoleID)
        {
            int nBirthType = 0;
            lock (m_LivedInMapBoss)
            {
                // 是否活在地图中
                if (!m_LivedInMapBoss.TryGetValue(nRoleID, out nBirthType))
                {
                    return;
                }

                m_LivedInMapBoss.Remove(nRoleID);                
            }
            
            if (nBirthType == (int)MonsterBirthTypes.TimePoint)
            {
                // 刷新所有玩家“世界BOSS”图标状态
                RefreshWorldBoss();
            }
            else if (nBirthType == (int)MonsterBirthTypes.CreateDayOfWeek)
            {
                // 刷新所有玩家“黄金部队”图标状态
                RefreshHuangJinBoss();
            }
        }

        /// <summary>
        /// 是否有世界BOSS
        /// </summary>
        public bool HaveWorldBoss(GameClient client)
        {
            lock (m_LivedInMapBoss)
            {
                foreach (KeyValuePair<int, int> kvp in m_LivedInMapBoss)
                {
                    if (kvp.Value == (int)MonsterBirthTypes.TimePoint)
                    {
                        TimerBossData bossData = null;

                        // 是否存在于世界BOSS列表中
                        if (m_WorldBossDict.TryGetValue(kvp.Key, out bossData))
                        {
                            if (null != bossData)
                            {
                                // 达到要求的转生数与级别
                                if ((client.ClientData.ChangeLifeCount == bossData.nReqChangeLiveCount && client.ClientData.Level >= bossData.nReqLevel)
                                    || client.ClientData.ChangeLifeCount > bossData.nReqChangeLiveCount)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 是否有黄金部队
        /// </summary>
        public bool HaveHuangJinBoss(GameClient client)
        {
            lock (m_LivedInMapBoss)
            {
                foreach (KeyValuePair<int, int> kvp in m_LivedInMapBoss)
                {
                    if (kvp.Value == (int)MonsterBirthTypes.CreateDayOfWeek)
                    {
                        TimerBossData bossData = null;

                        // 是否存在于世界BOSS列表中
                        if (m_HuangJinBossDict.TryGetValue(kvp.Key, out bossData))
                        {
                            if (null != bossData)
                            {
                                // 达到要求的转生数与级别
                                if ((client.ClientData.ChangeLifeCount == bossData.nReqChangeLiveCount && client.ClientData.Level >= bossData.nReqLevel)
                                    || client.ClientData.ChangeLifeCount > bossData.nReqChangeLiveCount)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 刷新所有玩家“黄金部队”图标状态
        /// </summary>
        public void RefreshHuangJinBoss()
        {
            int count = GameManager.ClientMgr.GetMaxClientCount();
            for( int i = 0; i < count; i++ )
            {
                GameClient client = GameManager.ClientMgr.FindClientByNid(i);
                if (null != client)
                {
                    client._IconStateMgr.CheckHuangJinBoss(client);
                    client._IconStateMgr.SendIconStateToClient(client);
                }
            }
        }

        /// <summary>
        /// 刷新所有玩家“世界BOSS”图标状态
        /// </summary>
        public void RefreshWorldBoss()
        {
            int count = GameManager.ClientMgr.GetMaxClientCount();
            for (int i = 0; i < count; i++)
            {
                GameClient client = GameManager.ClientMgr.FindClientByNid(i);
                if (null != client)
                {
                    client._IconStateMgr.CheckShiJieBoss(client);
                    client._IconStateMgr.SendIconStateToClient(client);
                }
            }
        }
    }
}
