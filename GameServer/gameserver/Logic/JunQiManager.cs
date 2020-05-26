using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;
using GameServer.Interface;
using GameServer.Core.Executor;
using Tmsk.Contract;

namespace GameServer.Logic
{
    /// <summary>
    /// 砍倒帮旗的项
    /// </summary>
    public class KillJunQiItem
    {
        /// <summary>
        /// 砍倒帮旗的帮会ID
        /// </summary>
        public int BHID = 0;

        /// <summary>
        /// 砍倒帮旗的时间
        /// </summary>
        public long KillJunQiTicks = 0L;
    };

    /// <summary>
    /// 帮旗管理类
    /// </summary>
    public class JunQiManager
    {
        #region 安插帮旗时的互斥对象

        /// <summary>
        /// 安插帮旗时的互斥对象
        /// </summary>
        public static Object JunQiMutex = new object();

        #endregion 安插帮旗时的互斥对象

        #region 记录砍倒帮旗的帮会和时间

        /// <summary>
        /// 记录砍倒帮旗的字典
        /// </summary>
        private static Dictionary<string, KillJunQiItem> KillJunQiDict = new Dictionary<string, KillJunQiItem>();

        /// <summary>
        /// 记录砍倒帮旗的帮会的信息
        /// </summary>
        /// <param name="bhid"></param>
        public static void AddKillJunQiItem(int mapCode, int npcID, int bhid)
        {
            string key = string.Format("{0}_{1}", mapCode, npcID);
            lock (KillJunQiDict)
            {
                KillJunQiDict[key] = new KillJunQiItem()
                {
                    BHID = bhid,
                    KillJunQiTicks = TimeUtil.NOW(),
                };
            }
        }

        /// <summary>
        /// 是否能够安插帮旗
        /// </summary>
        /// <param name="bhid"></param>
        public static bool CanInstallJunQiNow(int mapCode, int npcExtentionID, int bhid)
        {
            KillJunQiItem killJunQiItem = null;
            long ticks = TimeUtil.NOW();
            string key = string.Format("{0}_{1}", mapCode, npcExtentionID);
            lock (KillJunQiDict)
            {
                if (!KillJunQiDict.TryGetValue(key, out killJunQiItem))
                {
                    return true;
                }

                if (killJunQiItem.BHID == bhid)
                {
                    return true;
                }

                if (ticks - killJunQiItem.KillJunQiTicks >= (10 * 1000))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion 记录砍倒帮旗的帮会和时间

        #region 内存中的所有帮会的帮旗列表

        /// <summary>
        /// 内存帮旗字典
        /// </summary>
        private static Dictionary<int, BangHuiJunQiItemData> _BangHuiJunQiItemsDict = null;

        /// <summary>
        /// 从DBServer加载帮旗字典数据
        /// </summary>
        public static void LoadBangHuiJunQiItemsDictFromDBServer()
        {
            byte[] bytesData = null;
            if (TCPProcessCmdResults.RESULT_FAILED == Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                (int)TCPGameServerCmds.CMD_DB_GETBHJUNQILIST, string.Format("{0}", GameManager.ServerLineID), out bytesData, GameManager.LocalServerId))
            {
                return; //如果查询失败，就当做时不在线了
            }

            if (null == bytesData || bytesData.Length <= 6)
            {
                return;
            }

            Int32 length = BitConverter.ToInt32(bytesData, 0);

            Dictionary<int, BangHuiJunQiItemData> oldBangHuiJunQiItemsDict = _BangHuiJunQiItemsDict;

            //获取公告消息字典
            Dictionary<int, BangHuiJunQiItemData> newBangHuiJunQiItemsDict = DataHelper.BytesToObject<Dictionary<int, BangHuiJunQiItemData>>(bytesData, 6, length - 2);

            //查找变化
            if (null != newBangHuiJunQiItemsDict)
            {
                BangHuiJunQiItemData bangHuiJunQiItemData = null;
                foreach (var key in newBangHuiJunQiItemsDict.Keys)
                {
                    if (null == oldBangHuiJunQiItemsDict || !oldBangHuiJunQiItemsDict.ContainsKey(key)) //更添加，肯定要通知
                    {
                        bangHuiJunQiItemData = newBangHuiJunQiItemsDict[key];
                        //GameManager.ClientMgr.HandleBHJunQiUpLevel(bangHuiJunQiItemData.BHID, bangHuiJunQiItemData.QiLevel);
                    }
                    else
                    {
                        bangHuiJunQiItemData = newBangHuiJunQiItemsDict[key];
                        BangHuiJunQiItemData odlBangHuiLingDiItemData = oldBangHuiJunQiItemsDict[key];
                        if (bangHuiJunQiItemData.QiLevel != odlBangHuiLingDiItemData.QiLevel)
                        {
                            //GameManager.ClientMgr.HandleBHJunQiUpLevel(bangHuiJunQiItemData.BHID, bangHuiJunQiItemData.QiLevel);
                        }
                    }
                }
            }

            _BangHuiJunQiItemsDict = newBangHuiJunQiItemsDict;
        }

        /// <summary>
        /// 通知GameServer同步帮会的所属和范围
        /// </summary>
        public static void NotifySyncBangHuiJunQiItemsDict(GameClient client)
        {
            //通知其他线路
            string gmCmdData = string.Format("-syncjunqi");

            //转发GM消息到DBServer
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", null != client ? client.ClientData.RoleID : -1, "", 0, "", 0, gmCmdData, 0, 0, -1),
                null, GameManager.LocalServerIdForNotImplement);
        }

        /// <summary>
        /// 根据帮会ID来获取帮旗级别
        /// </summary>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public static int GetJunQiLevelByBHID(int bhid)
        {
            if (null == _BangHuiJunQiItemsDict) return 0;

            BangHuiJunQiItemData bangHuiJunQiItemData = null;
            if (!_BangHuiJunQiItemsDict.TryGetValue(bhid, out bangHuiJunQiItemData))
            {
                return 0;
            }

            return bangHuiJunQiItemData.QiLevel;
        }

        /// <summary>
        /// 根据帮会ID来获取帮旗名称
        /// </summary>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public static string GetJunQiNameByBHID(int bhid)
        {
            if (null == _BangHuiJunQiItemsDict) return Global.GetLang("未知");

            BangHuiJunQiItemData bangHuiJunQiItemData = null;
            if (!_BangHuiJunQiItemsDict.TryGetValue(bhid, out bangHuiJunQiItemData))
            {
                return Global.GetLang("未知");
            }

            return bangHuiJunQiItemData.QiName;
        }

        #endregion 内存中的所有帮会的帮旗列表

        #region 内存中的所有领地的帮会分布

        /// <summary>
        /// 内存领地帮会分布字典
        /// </summary>
        private static Dictionary<int, BangHuiLingDiItemData> _BangHuiLingDiItemsDict = null;

        /// <summary>
        /// 从DBServer加载领地帮会字典数据
        /// </summary>
        /// <param name="serverId"></param>
        /// <returns></returns>
        public static Dictionary<int, BangHuiLingDiItemData> LoadBangHuiLingDiItemsDictFromDBServer(int serverId)
        {
            byte[] bytesData = null;
            if (TCPProcessCmdResults.RESULT_FAILED == Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                (int)TCPGameServerCmds.CMD_DB_GETBHLINGDIDICT, string.Format("{0}", GameManager.ServerLineID), out bytesData, serverId))
            {
                return null; //如果查询失败，就当做时不在线了
            }

            if (null == bytesData || bytesData.Length <= 6)
            {
                return null;
            }

            Int32 length = BitConverter.ToInt32(bytesData, 0);
            Dictionary<int, BangHuiLingDiItemData> newBangHuiLingDiItemsDict = DataHelper.BytesToObject<Dictionary<int, BangHuiLingDiItemData>>(bytesData, 6, length - 2);
            return newBangHuiLingDiItemsDict;
        }

        /// <summary>
        /// 从DBServer加载领地帮会字典数据
        /// </summary>
        public static void LoadBangHuiLingDiItemsDictFromDBServer()
        {
            Dictionary<int, BangHuiLingDiItemData> oldBangHuiLingDiItemsDict = _BangHuiLingDiItemsDict;
            //内存领地帮会分布字典
            Dictionary<int, BangHuiLingDiItemData> newBangHuiLingDiItemsDict = LoadBangHuiLingDiItemsDictFromDBServer(GameManager.LocalServerId);

            //查找变化
            bool luoLanChengZhuBHIDChanged = false;
            if (null != newBangHuiLingDiItemsDict)
            {
                BangHuiLingDiItemData bangHuiLingDiItemData = null;
                foreach (var key in newBangHuiLingDiItemsDict.Keys)
                {
                    if (null == oldBangHuiLingDiItemsDict || !oldBangHuiLingDiItemsDict.ContainsKey(key)) //更添加，肯定要通知
                    {
                        bangHuiLingDiItemData = newBangHuiLingDiItemsDict[key];
                        GameManager.ClientMgr.NotifyAllLingDiForBHMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            bangHuiLingDiItemData.LingDiID, bangHuiLingDiItemData.BHID, bangHuiLingDiItemData.ZoneID, bangHuiLingDiItemData.BHName,
                            bangHuiLingDiItemData.LingDiTax);

                        if (key == (int)LingDiIDs.LuoLanChengZhan)
                        {
                            luoLanChengZhuBHIDChanged = true;
                        }
                    }
                    else
                    {
                        bangHuiLingDiItemData = newBangHuiLingDiItemsDict[key];
                        BangHuiLingDiItemData odlBangHuiLingDiItemData = oldBangHuiLingDiItemsDict[key];
                        if (bangHuiLingDiItemData.BHID != odlBangHuiLingDiItemData.BHID ||
                            bangHuiLingDiItemData.LingDiTax != odlBangHuiLingDiItemData.LingDiTax)
                        {
                            GameManager.ClientMgr.NotifyAllLingDiForBHMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                bangHuiLingDiItemData.LingDiID, bangHuiLingDiItemData.BHID, bangHuiLingDiItemData.ZoneID, bangHuiLingDiItemData.BHName,
                                bangHuiLingDiItemData.LingDiTax);

                            if (key == (int)LingDiIDs.LuoLanChengZhan)
                            {
                                luoLanChengZhuBHIDChanged = true;
                            }
                        }
                    }
                }
            }

            _BangHuiLingDiItemsDict = newBangHuiLingDiItemsDict;
            if (luoLanChengZhuBHIDChanged)
            {
                LuoLanChengZhanManager.getInstance().BangHuiLingDiItemsDictFromDBServer();
            }

            //每一次重新加载都更新一次，保证最新的修改都得到应用
            Global.UpdateWangChengZhanWeekDays(true);
        }

        /// <summary>
        /// 获取内存领地帮会分布字典
        /// </summary>
        public static Dictionary<int, BangHuiLingDiItemData> GetBangHuiLingDiItemsDict()
        {
            return _BangHuiLingDiItemsDict;
        }

        /// <summary>
        /// 通知GameServer同步领地帮会分布
        /// </summary>
        public static void NotifySyncBangHuiLingDiItemsDict()
        {
            //从DBServer加载领地帮会字典数据
            JunQiManager.LoadBangHuiLingDiItemsDictFromDBServer();

            //通知其他线路
            string gmCmdData = string.Format("-synclingdi");

            //转发GM消息到DBServer
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", 0, "", 0, "", 0, gmCmdData, 0, 0, -1),
                null, GameManager.LocalServerId);
        }

        /// <summary>
        /// 根据领地ID来获取项
        /// </summary>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public static BangHuiLingDiItemData GetItemByLingDiID(int lingDiID)
        {
            if (null == _BangHuiLingDiItemsDict) return null;

            BangHuiLingDiItemData bangHuiLingDiItemData = null;
            if (!_BangHuiLingDiItemsDict.TryGetValue(lingDiID, out bangHuiLingDiItemData))
            {
                return null;
            }

            return bangHuiLingDiItemData;
        }

        /// <summary>
        /// 根据领地ID来获取占领的帮会的ID
        /// </summary>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public static int GetBHIDByLingDiID(int lingDiID)
        {
            if (null == _BangHuiLingDiItemsDict) return 0;

            BangHuiLingDiItemData bangHuiLingDiItemData = null;
            if (!_BangHuiLingDiItemsDict.TryGetValue(lingDiID, out bangHuiLingDiItemData))
            {
                return 0;
            }

            return bangHuiLingDiItemData.BHID;
        }

        /// <summary>
        /// 根据领地ID来获取领地的税率
        /// </summary>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public static int GetTaxByLingDiID(int lingDiID)
        {
            if (null == _BangHuiLingDiItemsDict) return 0;

            BangHuiLingDiItemData bangHuiLingDiItemData = null;
            if (!_BangHuiLingDiItemsDict.TryGetValue(lingDiID, out bangHuiLingDiItemData))
            {
                return 0;
            }

            return bangHuiLingDiItemData.LingDiTax;
        }

        /// <summary>
        /// 获取指定帮会占领的ID最小的领地信息
        /// </summary>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public static BangHuiLingDiItemData GetFirstLingDiItemDataByBHID(int bhid)
        {
            if (null == _BangHuiLingDiItemsDict) return null;

            int lingDiID = 10000;
            BangHuiLingDiItemData bangHuiLingDiItemData = null;
            foreach (var val in _BangHuiLingDiItemsDict.Values)
            {
                if (val.LingDiID == (int)LingDiIDs.YanZhou) continue;
                if (val.BHID == bhid)
                {
                    if (val.LingDiID < lingDiID)
                    {
                        lingDiID = val.LingDiID;
                        bangHuiLingDiItemData = val;
                    }
                }
            }

            return bangHuiLingDiItemData;
        }

        /// <summary>
        /// 获取指定帮会占领的领地信息
        /// </summary>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public static BangHuiLingDiItemData GetAnyLingDiItemDataByBHID(int bhid)
        {
            if (null == _BangHuiLingDiItemsDict) return null;

            BangHuiLingDiItemData bangHuiLingDiItemData = null;
            foreach (var val in _BangHuiLingDiItemsDict.Values)
            {
                if (val.LingDiID == (int)LingDiIDs.YanZhou) continue;
                if (val.BHID == bhid)
                {
                    bangHuiLingDiItemData = val;
                    break;
                }
            }

            return bangHuiLingDiItemData;
        }

        #endregion 内存中的所有领地的帮会分布

        #region 基础数据

        /// <summary>
        /// 根据角NPCID索引的帮旗数据字典
        /// </summary>
        private static Dictionary<int, JunQiItem> _NPCID2JunQiDict = new Dictionary<int, JunQiItem>();

        /// <summary>
        /// 根据帮旗ID索引的帮旗数据字典
        /// </summary>
        private static Dictionary<int, JunQiItem> _ID2JunQiDict = new Dictionary<int, JunQiItem>();

        #endregion 基础数据

        #region 管理函数

        /// <summary>
        /// 获取旗座NPC的位置
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="npcID"></param>
        /// <returns></returns>
        private static Point GetQiZuoNPCPosition(int mapCode, int npcID)
        {
            SystemXmlItem systemQiZuoItem = null;
            if (!GameManager.systemQiZuoMgr.SystemXmlItemDict.TryGetValue(mapCode, out systemQiZuoItem))
            {
                return new Point(0, 0);
            }

            for (int i = 1; i <= MaxInstallQiNum; i++)
            {
                if (npcID == systemQiZuoItem.GetIntValue(string.Format("NPC{0}", i)))
                {
                    return new Point(systemQiZuoItem.GetIntValue(string.Format("NPC{0}PosX", i)), systemQiZuoItem.GetIntValue(string.Format("NPC{0}PosY", i)));
                }
            }

            return new Point(0, 0);
        }

        /// <summary>
        /// 获取普通地图上安插帮旗的位置
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        private static Point GetNormalMapJunQiPosition(int mapCode)
        {
            SystemXmlItem systemQiZiItem = null;
            if (!GameManager.systemLingQiMapQiZhiMgr.SystemXmlItemDict.TryGetValue(mapCode, out systemQiZiItem))
            {
                return new Point(0, 0);
            }

            return new Point(systemQiZiItem.GetIntValue("PosX"), systemQiZiItem.GetIntValue("PosY"));
        }

        /// <summary>
        /// 添加一个新的帮旗数据
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="bhid"></param>
        /// <param name="bhName"></param>
        /// <param name="npcID"></param>
        /// <param name="junQiName"></param>
        /// <param name="junQiLevel"></param>
        /// <returns></returns>
        private static JunQiItem AddJunQi(int mapCode, int bhid, int zoneID, string bhName, int npcID, string junQiName, int junQiLevel, SceneUIClasses sceneType = SceneUIClasses.Normal)
        {
            SystemXmlItem systemJunQiItem = null;
            if (!GameManager.systemJunQiMgr.SystemXmlItemDict.TryGetValue(junQiLevel, out systemJunQiItem))
            {
                return null;
            }

            Point junQiPos = new Point(0, 0);
            if (sceneType == SceneUIClasses.LuoLanChengZhan)
            {
                if (-1 != npcID)
                {
                    junQiPos = GetQiZuoNPCPosition(mapCode, npcID);
                }
            }
            else
            {
                junQiPos = GetNormalMapJunQiPosition(mapCode);
            }

            if (0.0 == junQiPos.X && 0.0 == junQiPos.Y)
            {
                return null;
            }

            JunQiItem JunQiItem = new JunQiItem()
            {
                JunQiID = (int)GameManager.JunQiIDMgr.GetNewID(),
                QiName = junQiName,
                ZoneID = zoneID,
                BHID = bhid,
                BHName = bhName,
                JunQiLevel = junQiLevel,
                QiZuoNPC = npcID,
                MapCode = mapCode,
                PosX = (int)junQiPos.X,
                PosY = (int)junQiPos.Y,
                Direction = 0,
                LifeV = systemJunQiItem.GetIntValue("Lifev"),
                StartTime = TimeUtil.NOW(),
                CurrentLifeV = systemJunQiItem.GetIntValue("Lifev"),
                CutLifeV = systemJunQiItem.GetIntValue("CutLifeV"),
                BodyCode = systemJunQiItem.GetIntValue("BodyCode"),
                PicCode = systemJunQiItem.GetIntValue("PicCode"),
                ManagerType = sceneType,
            };

            if (-1 != npcID)
            {
                lock (_NPCID2JunQiDict)
                {
                    _NPCID2JunQiDict[npcID] = JunQiItem;
                }
            }

            lock (_ID2JunQiDict)
            {
                _ID2JunQiDict[JunQiItem.JunQiID] = JunQiItem;
            }

            return JunQiItem;
        }

        /// <summary>
        /// 通过NPCID查找一个帮旗数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="JunQiID"></param>
        /// <returns></returns>
        public static JunQiItem FindJunQiByNpcID(int npcID)
        {
            JunQiItem JunQiItem = null;
            lock (_NPCID2JunQiDict)
            {
                _NPCID2JunQiDict.TryGetValue(npcID, out JunQiItem);
            }

            return JunQiItem;
        }

        /// <summary>
        /// 通过帮旗ID查找一个帮旗数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="JunQiID"></param>
        /// <returns></returns>
        public static JunQiItem FindJunQiByID(int JunQiID)
        {
            JunQiItem JunQiItem = null;
            lock (_ID2JunQiDict)
            {
                _ID2JunQiDict.TryGetValue(JunQiID, out JunQiItem);
            }

            return JunQiItem;
        }

        /// <summary>
        /// 通过帮会查找一个帮旗数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="JunQiID"></param>
        /// <returns></returns>
        public static JunQiItem FindJunQiByBHID(int bhid)
        {
            JunQiItem JunQiItem = null;
            lock (_ID2JunQiDict)
            {
                foreach (var val in _ID2JunQiDict.Values)
                {
                    if (val.BHID == bhid)
                    {
                        JunQiItem = val;
                        break;
                    }
                }
            }

            return JunQiItem;
        }

        /// <summary>
        /// 删除一个帮旗数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="JunQiID"></param>
        /// <returns></returns>
        private static void RemoveJunQi(int JunQiID)
        {
            JunQiItem JunQiItem = null;
            lock (_ID2JunQiDict)
            {
                _ID2JunQiDict.TryGetValue(JunQiID, out JunQiItem);
                if (null != JunQiItem)
                {
                    _ID2JunQiDict.Remove(JunQiItem.JunQiID);
                }
            }

            if (null != JunQiItem)
            {
                if (-1 != JunQiItem.QiZuoNPC)
                {
                    lock (_NPCID2JunQiDict)
                    {
                        _NPCID2JunQiDict.Remove(JunQiItem.QiZuoNPC);
                    }
                }
            }
        }

        /// <summary>
        /// 根据地图编号计算帮旗安插的数量
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        private static int CalcJunQiNumByMapCode(int mapCode, out int bhid)
        {
            bhid = 0;
            Dictionary<int, int> calcDict = new Dictionary<int, int>();
            lock (_ID2JunQiDict)
            {
                foreach (var val in _ID2JunQiDict.Values)
                {
                    if (val.MapCode == mapCode)
                    {
                        if (calcDict.ContainsKey(val.BHID))
                        {
                            calcDict[val.BHID] = calcDict[val.BHID] + 1;
                        }
                        else
                        {
                            calcDict[val.BHID] = 1;
                        }
                    }
                }
            }

            int maxNum = 0;
            foreach (var key in calcDict.Keys)
            {
                if (calcDict[key] > maxNum)
                {
                    maxNum = calcDict[key];
                    bhid = key;
                }                
            }

            return maxNum;
        }

        /// <summary>
        /// 根据地图编号计算帮旗安插的帮会名称字典
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        private static string GetBHNameByNPCID(int npcID)
        {
            JunQiItem junQiItem = FindJunQiByNpcID(npcID);
            if (null == junQiItem) return "";
            return Global.FormatBangHuiName(junQiItem.ZoneID, junQiItem.BHName);
        }

        #endregion 管理函数

        #region 处理新帮旗的生成和删除

        /// <summary>
        /// 处理添加帮旗
        /// </summary>
        public static bool ProcessNewJunQi(SocketListener sl, TCPOutPacketPool pool, int mapCode, int bhid, int zoneID, string bhName, int npcID, string junQiName, int junQiLevel, SceneUIClasses sceneType = SceneUIClasses.Normal)
        {
            //添加一个新的帮旗数据
            JunQiItem JunQiItem = AddJunQi(mapCode, bhid, zoneID, bhName, npcID, junQiName, junQiLevel, sceneType);
            if (null == JunQiItem)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("为RoleID生成帮旗对象时失败, MapCode={0}, BHID={1}, BHName={2}, NPCID={3}, QiName={4}, QiLevel={5}", mapCode, bhid, bhName, npcID, junQiName, junQiLevel));
                return false;
            }

            GameManager.MapGridMgr.DictGrids[JunQiItem.MapCode].MoveObject(-1, -1, (int)JunQiItem.PosX, (int)JunQiItem.PosY, JunQiItem);

            //List<Object> objList = Global.GetAll9Clients(JunQiItem);
            //GameManager.ClientMgr.NotifyOthersNewJunQi(sl, pool, objList, JunQiItem);
            return true;
        }

        /// <summary>
        /// 处理删除帮旗
        /// </summary>
        public static void ProcessDelJunQi(SocketListener sl, TCPOutPacketPool pool, int JunQiID)
        {
            JunQiItem JunQiItem = FindJunQiByID(JunQiID);
            if (null == JunQiItem)
            {
                return;
            }

            RemoveJunQi(JunQiID);

            GameManager.MapGridMgr.DictGrids[JunQiItem.MapCode].RemoveObject(JunQiItem);
            //List<Object> objList = Global.GetAll9Clients(JunQiItem);
            //GameManager.ClientMgr.NotifyOthersDelJunQi(sl, pool, objList, JunQiID);
        }

        /// <summary>
        /// 处理删除全部帮旗
        /// </summary>
        public static void ProcessDelAllJunQiByMapCode(SocketListener sl, TCPOutPacketPool pool, int mapCode)
        {
            List<JunQiItem> junQiItemList = new List<JunQiItem>();
            lock (_ID2JunQiDict)
            {
                foreach (var val in _ID2JunQiDict.Values)
                {
                    if (val.MapCode == mapCode)
                    {
                        junQiItemList.Add(val);
                    }
                }
            }

            for (int i = 0; i < junQiItemList.Count; i++)
            {
                RemoveJunQi(junQiItemList[i].JunQiID);

                GameManager.MapGridMgr.DictGrids[junQiItemList[i].MapCode].RemoveObject(junQiItemList[i]);
                //List<Object> objList = Global.GetAll9Clients(junQiItemList[i]);
                //GameManager.ClientMgr.NotifyOthersDelJunQi(sl, pool, objList, junQiItemList[i].JunQiID);
            }
        }

        /// <summary>
        /// 处理删除全部帮旗
        /// </summary>
        public static void ProcessDelAllJunQiByBHID(SocketListener sl, TCPOutPacketPool pool, int bhid)
        {
            List<JunQiItem> junQiItemList = new List<JunQiItem>();
            lock (_ID2JunQiDict)
            {
                foreach (var val in _ID2JunQiDict.Values)
                {
                    if (val.BHID == bhid)
                    {
                        junQiItemList.Add(val);
                    }
                }
            }

            for (int i = 0; i < junQiItemList.Count; i++)
            {
                RemoveJunQi(junQiItemList[i].JunQiID);

                GameManager.MapGridMgr.DictGrids[junQiItemList[i].MapCode].RemoveObject(junQiItemList[i]);
                //List<Object> objList = Global.GetAll9Clients(junQiItemList[i]);
                //GameManager.ClientMgr.NotifyOthersDelJunQi(sl, pool, objList, junQiItemList[i].JunQiID);
            }
        }

        /// <summary>
        /// 获取指定地图上所有旗座NPC的ID
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="npcID"></param>
        /// <returns></returns>
        private static List<int> GetQiZuoNPCIDList(int mapCode)
        {
            SystemXmlItem systemQiZuoItem = null;
            if (!GameManager.systemQiZuoMgr.SystemXmlItemDict.TryGetValue(mapCode, out systemQiZuoItem))
            {
                return null;
            }

            List<int> list = new List<int>();
            for (int i = 1; i <= MaxInstallQiNum; i++)
            {
                list.Add(systemQiZuoItem.GetIntValue(string.Format("NPC{0}", i)));
            }

            return list;
        }

        /// <summary>
        /// 处理添加帮旗
        /// </summary>
        public static void ProcessAllNewJunQiByMapCode(int mapCode, int bhid, int zoneID, string bhName)
        {
            //获取指定地图上所有旗座NPC的ID
            List<int> list = GetQiZuoNPCIDList(mapCode);
            if (null == list) return;

            //处理获取帮旗名称的操作
            string junQiName = JunQiManager.GetJunQiNameByBHID(bhid);

            //处理获取帮旗级别的操作
            int junQiLevel = JunQiManager.GetJunQiLevelByBHID(bhid);

            for (int i = 0; i < list.Count; i++)
            {
                ProcessNewJunQi(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, mapCode, bhid, zoneID, bhName, list[i], junQiName, junQiLevel);
            }
        }

        #endregion 处理新帮旗的生成和删除

        #region 处理帮旗的显示和隐藏

        /// <summary>
        /// 通知所有人显示帮旗
        /// </summary>
        public static void NotifyOthersShowJunQi(SocketListener sl, TCPOutPacketPool pool, JunQiItem JunQiItem)
        {
            if (null == JunQiItem) return;

            GameManager.MapGridMgr.DictGrids[JunQiItem.MapCode].MoveObject(-1, -1, (int)JunQiItem.PosX, (int)JunQiItem.PosY, JunQiItem);
            //List<Object> objList = Global.GetAll9Clients(JunQiItem);
            //GameManager.ClientMgr.NotifyOthersNewJunQi(sl, pool, objList, JunQiItem);
        }

        /// <summary>
        /// 通知所有人隐藏帮旗
        /// </summary>
        public static void NotifyOthersHideJunQi(SocketListener sl, TCPOutPacketPool pool, JunQiItem JunQiItem)
        {
            if (null == JunQiItem) return;

            GameManager.MapGridMgr.DictGrids[JunQiItem.MapCode].RemoveObject(JunQiItem);
            //List<Object> objList = Global.GetAll9Clients(JunQiItem);
            //GameManager.ClientMgr.NotifyOthersDelJunQi(sl, pool, objList, JunQiItem.JunQiID);
        }

        #endregion 处理帮旗的显示和隐藏

        #region 处理已经帮旗加血/死亡/超时

        /// <summary>
        /// 处理帮旗的死亡
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="JunQiItem"></param>
        /// <returns></returns>
        private static bool ProcessJunQiDead(SocketListener sl, TCPOutPacketPool pool, long nowTicks, JunQiItem JunQiItem)
        {
            if (JunQiItem.CurrentLifeV > 0)
            {
                return false;
            }

            long subTicks = nowTicks - JunQiItem.JunQiDeadTicks;

            //如果还没到时间，则跳过
            if (subTicks < (2 * 1000))
            {
                return false;
            }

            ProcessDelJunQi(sl, pool, JunQiItem.JunQiID);

            //通知地图变动信息
            NotifyAllLingDiMapInfoData(JunQiItem.MapCode);

            return true;
        }

        /// <summary>
        /// 处理已经帮旗的超时
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessAllJunQiItems(SocketListener sl, TCPOutPacketPool pool)
        {
            if (Global.GetBangHuiFightingLineID() != GameManager.ServerLineID)
            {
                return;
            }

            List<JunQiItem> JunQiItemList = new List<JunQiItem>();
            lock (_ID2JunQiDict)
            {
                foreach (var val in _ID2JunQiDict.Values)
                {
                    JunQiItemList.Add(val);
                }
            }

            long nowTicks = TimeUtil.NOW();

            JunQiItem JunQiItem = null;
            for (int i = 0; i < JunQiItemList.Count; i++)
            {
                JunQiItem = JunQiItemList[i];

                //处理帮旗的死亡
                if (ProcessJunQiDead(sl, pool, nowTicks, JunQiItem))
                {
                    continue;
                }
            }
        }

        #endregion 处理已经帮旗加血/死亡/超时

        #region 处理角色移动时的帮旗发送

        /// <summary>
        /// 发送帮旗到给自己
        /// </summary>
        /// <param name="client"></param>
        public static void SendMySelfJunQiItems(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            JunQiItem JunQiItem = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is JunQiItem))
                {
                    continue;
                }

                if ((objsList[i] as JunQiItem).CurrentLifeV <= 0)
                {
                    continue;
                }

                JunQiItem = objsList[i] as JunQiItem;
                GameManager.ClientMgr.NotifyMySelfNewJunQi(sl, pool, client, JunQiItem);
            }
        }

        /// <summary>
        /// 删除自己哪儿的帮旗
        /// </summary>
        /// <param name="client"></param>
        public static void DelMySelfJunQiItems(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            JunQiItem JunQiItem = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is JunQiItem))
                {
                    continue;
                }

                JunQiItem = objsList[i] as JunQiItem;
                GameManager.ClientMgr.NotifyMySelfDelJunQi(sl, pool, client, JunQiItem.JunQiID);
            }
        }

        #endregion 处理角色移动时的帮旗发送

        #region 查找指定范围内的帮旗

        /// <summary>
        /// 查找指定圆周范围内的帮旗
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupEnemiesInCircle(GameClient client, int mapCode, int toX, int toY, int radius, List<int> enemiesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objList) return;

            Point center = new Point(toX, toY);
            for (int i = 0; i < objList.Count; i++)
            {
                if (!(objList[i] is JunQiItem))
                {
                    continue;
                }

                //非敌对对象
                if (null != client && !Global.IsOpposition(client, (objList[i] as JunQiItem)))
                {
                    continue;
                }

                //不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objList[i] as JunQiItem).CopyMapID)
                {
                    continue;
                }

                Point pt = new Point((objList[i] as JunQiItem).PosX, (objList[i] as JunQiItem).PosY);
                if (Global.InCircle(pt, center, (double)radius))
                {
                    enemiesList.Add((objList[i] as JunQiItem).JunQiID);
                }
            }
        }

        /// <summary>
        /// 查找指定半圆周范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupEnemiesInCircleByAngle(GameClient client, int direction, int mapCode, int toX, int toY, int radius, List<JunQiItem> enemiesList, double angle)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objList) return;

            double loAngle = 0.0, hiAngle = 0.0;
            Global.GetAngleRangeByDirection(direction, angle, out loAngle, out hiAngle);
            Point center = new Point(toX, toY);
            for (int i = 0; i < objList.Count; i++)
            {
                if (!(objList[i] is JunQiItem))
                {
                    continue;
                }

                //非敌对对象
                if (null != client && !Global.IsOpposition(client, (objList[i] as JunQiItem)))
                {
                    continue;
                }

                //不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objList[i] as JunQiItem).CopyMapID)
                {
                    continue;
                }

                Point pt = new Point((objList[i] as JunQiItem).PosX, (objList[i] as JunQiItem).PosY);
                if (Global.InCircleByAngle(pt, center, (double)radius, loAngle, hiAngle))
                {
                    enemiesList.Add((objList[i] as JunQiItem));
                }
                else
                {
                    ;
                }
            }
        }

        #endregion 查找指定范围内的帮旗

        #region 查找指定格子内的帮旗

        /// <summary>
        /// 查找指定格子内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupEnemiesAtGridXY(IObject attacker, int gridX, int gridY, List<Object> enemiesList)
        {
            int mapCode = attacker.CurrentMapCode;
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];

            List<Object> objList = mapGrid.FindObjects((int)gridX, (int)gridY);
            if (null == objList) return;

            for (int i = 0; i < objList.Count; i++)
            {
                if (!(objList[i] is JunQiItem))
                {
                    continue;
                }

                //不在同一个副本
                if (null != attacker && attacker.CurrentCopyMapID != (objList[i] as JunQiItem).CopyMapID)
                {
                    continue;
                }

                enemiesList.Add(objList[i]);
            }
        }

        /// <summary>
        /// 查找指定格子内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupAttackEnemies(IObject attacker, int direction, List<Object> enemiesList)
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
        public static void LookupAttackEnemyIDs(IObject attacker, int direction, List<int> enemiesList)
        {
            List<Object> objList = new List<Object>();
            LookupAttackEnemies(attacker, direction, objList);
            for (int i = 0; i < objList.Count; i++)
            {
                enemiesList.Add((objList[i] as JunQiItem).JunQiID);
            }
        }

        /// <summary>
        /// 查找指定给子范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupRangeAttackEnemies(IObject obj, int toX, int toY, int direction, string rangeMode, List<Object> enemiesList)
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

        #endregion 查找指定格子内的帮旗

        #region 战斗相关

        /// <summary>
        /// 是否能被攻击
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public static bool CanAttack(JunQiItem enemy)
        {
            if (null == enemy) return false;
            if (JugeLingDiZhanEndByMapCode(enemy.MapCode))
            {
                return false;
            }

            //防止普通地图上的旗帜被攻击
            int lingDiID = GetLingDiIDBy2MapCode(enemy.MapCode);
            //if (lingDiID < (int)LingDiIDs.YouZhou || lingDiID > (int)LingDiIDs.XingYang)
            if (lingDiID != (int)LingDiIDs.YouZhou)
            {
                return false;
            }

            return (LingDiZhanStates.Fighting == LingDiZhanState);
        }

        /// <summary>
        /// 通知其他人被攻击(单攻)，并且被伤害(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public static int NotifyInjured(SocketListener sl, TCPOutPacketPool pool, GameClient client, JunQiItem enemy, int burst, int injure, double injurePercent, int attackType, bool forceBurst, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, double baseRate = 1.0, int addVlue = 0, int nHitFlyDistance = 0)
        {
            int ret = 0;
            object obj = enemy;
            {
                //怪物必须或者才操作
                if ((obj as JunQiItem).CurrentLifeV > 0)
                {
                    injure = (obj as JunQiItem).CutLifeV;

                    // 技能改造[3/13/2014 LiaoWei]
                    injure = (int)(injure * baseRate + addVlue);

                    // 技能中可配置伤害百分比
                    injure = (int)(injure * injurePercent);

                    ret = injure;

                    (obj as JunQiItem).CurrentLifeV -= (int)injure; //是否需要锁定
                    (obj as JunQiItem).CurrentLifeV = Global.GMax((obj as JunQiItem).CurrentLifeV, 0);
                    int enemyLife = (int)(obj as JunQiItem).CurrentLifeV;
                    (obj as JunQiItem).AttackedRoleID = client.ClientData.RoleID;

                    //判断是否将给敌人的伤害转化成自己的血量增长
                    GameManager.ClientMgr.SpriteInjure2Blood(sl, pool, client, injure);

                    //将攻击者加入历史列表
                    (obj as JunQiItem).AddAttacker(client.ClientData.RoleID, Global.GMax(0, injure));

                    GameManager.SystemServerEvents.AddEvent(string.Format("帮旗减血, Injure={0}, Life={1}", injure, enemyLife), EventLevels.Debug);

                    //判断怪物是否死亡
                    if ((int)(obj as JunQiItem).CurrentLifeV <= 0)
                    {
                        GameManager.SystemServerEvents.AddEvent(string.Format("帮旗死亡, roleID={0}", (obj as JunQiItem).JunQiID), EventLevels.Debug);

                        /// 处理怪物死亡
                        ProcessJunQiDead(sl, pool, client, (obj as JunQiItem));
                    }

                    if ((obj as JunQiItem).AttackedRoleID >= 0 && (obj as JunQiItem).AttackedRoleID != client.ClientData.RoleID)
                    {
                        GameClient findClient = GameManager.ClientMgr.FindClient((obj as JunQiItem).AttackedRoleID);
                        if (null != findClient)
                        {
                            //通知其他在线客户端
                            GameManager.ClientMgr.NotifySpriteInjured(sl, pool, findClient, findClient.ClientData.MapCode, findClient.ClientData.RoleID, (obj as JunQiItem).JunQiID, 0, 0, enemyLife, findClient.ClientData.Level, new Point(-1, -1));

                            //向自己发送敌人受伤的信息
                            ClientManager.NotifySelfEnemyInjured(sl, pool, findClient, findClient.ClientData.RoleID, enemy.JunQiID, 0, 0, enemyLife, 0);
                        }
                    }

                    //通知其他在线客户端
                    GameManager.ClientMgr.NotifySpriteInjured(sl, pool, client, client.ClientData.MapCode, client.ClientData.RoleID, (obj as JunQiItem).JunQiID, burst, injure, enemyLife, client.ClientData.Level, new Point(-1, -1));

                    //向自己发送敌人受伤的信息
                    ClientManager.NotifySelfEnemyInjured(sl, pool, client, client.ClientData.RoleID, enemy.JunQiID, burst, injure, enemyLife, 0);

                    //通知紫名信息(限制当前地图)
                    if (!client.ClientData.DisableChangeRolePurpleName)
                    {
                        GameManager.ClientMgr.ForceChangeRolePurpleName2(sl, pool, client);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 通知其他人被攻击(单攻)，并且被伤害(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public static void NotifyInjured(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, int enemy, int enemyX, int enemyY, int burst, int injure, double attackPercent, int addAttack, double baseRate = 1.0, int addVlue = 0, int nHitFlyDistance = 0)
        {
            object obj = FindJunQiByID(enemy);
            if (null != obj)
            {
                //怪物必须或者才操作
                if ((obj as JunQiItem).CurrentLifeV > 0)
                {
                    //处理BOSS克星
                    injure = (obj as JunQiItem).CutLifeV;

                    (obj as JunQiItem).CurrentLifeV -= (int)injure; //是否需要锁定
                    (obj as JunQiItem).CurrentLifeV = Global.GMax((obj as JunQiItem).CurrentLifeV, 0);
                    int enemyLife = (int)(obj as JunQiItem).CurrentLifeV;
                    (obj as JunQiItem).AttackedRoleID = client.ClientData.RoleID;

                    //判断是否将给敌人的伤害转化成自己的血量增长
                    GameManager.ClientMgr.SpriteInjure2Blood(sl, pool, client, injure);

                    GameManager.SystemServerEvents.AddEvent(string.Format("帮旗减血, Injure={0}, Life={1}", injure, enemyLife), EventLevels.Debug);

                    //判断怪物是否死亡
                    if ((int)(obj as JunQiItem).CurrentLifeV <= 0)
                    {
                        GameManager.SystemServerEvents.AddEvent(string.Format("帮旗死亡, roleID={0}", (obj as JunQiItem).JunQiID), EventLevels.Debug);

                        /// 处理帮旗死亡
                        ProcessJunQiDead(sl, pool, client, (obj as JunQiItem));
                    }

                    int ownerRoleID = (obj as JunQiItem).GetAttackerFromList();
                    if (ownerRoleID >= 0 && ownerRoleID != client.ClientData.RoleID)
                    {
                        GameClient findClient = GameManager.ClientMgr.FindClient(ownerRoleID);
                        if (null != findClient)
                        {
                            //通知其他在线客户端
                            GameManager.ClientMgr.NotifySpriteInjured(sl, pool, findClient, findClient.ClientData.MapCode, findClient.ClientData.RoleID, (obj as JunQiItem).JunQiID, 0, 0, enemyLife, findClient.ClientData.Level, new Point(-1, -1));

                            //向自己发送敌人受伤的信息
                            ClientManager.NotifySelfEnemyInjured(sl, pool, findClient, findClient.ClientData.RoleID, (obj as JunQiItem).JunQiID, 0, 0, enemyLife, 0);
                        }
                    }

                    //通知其他在线客户端
                    GameManager.ClientMgr.NotifySpriteInjured(sl, pool, client, client.ClientData.MapCode, client.ClientData.RoleID, (obj as JunQiItem).JunQiID, burst, injure, enemyLife, client.ClientData.Level, new Point(-1, -1));

                    //向自己发送敌人受伤的信息
                    ClientManager.NotifySelfEnemyInjured(sl, pool, client, client.ClientData.RoleID, (obj as JunQiItem).JunQiID, burst, injure, enemyLife, 0);

                    //通知紫名信息(限制当前地图)
                    if (!client.ClientData.DisableChangeRolePurpleName)
                    {
                        GameManager.ClientMgr.ForceChangeRolePurpleName2(sl, pool, client);
                    }
                }
            }
        }

        /// <summary>
        /// 处理帮旗死亡
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        private static void ProcessJunQiDead(SocketListener sl, TCPOutPacketPool pool, GameClient client, JunQiItem junQiItem)
        {
            if (junQiItem.HandledDead)
            {
                return;
            }

            junQiItem.HandledDead = true;
            junQiItem.JunQiDeadTicks = TimeUtil.NOW();

            int ownerRoleID = junQiItem.GetAttackerFromList(); //根据血量计算
            if (ownerRoleID >= 0 && ownerRoleID != client.ClientData.RoleID)
            {
                GameClient findClient = GameManager.ClientMgr.FindClient(ownerRoleID);
                if (null != findClient)
                {
                    client = findClient;
                }
            }

            //记录帮旗时被那个帮会的人砍倒的
            //记录砍倒帮旗的帮会的信息
            AddKillJunQiItem(client.ClientData.MapCode, junQiItem.QiZuoNPC, client.ClientData.Faction);

            if (junQiItem.ManagerType == SceneUIClasses.LuoLanChengZhan)
            {
                LuoLanChengZhanManager.getInstance().OnProcessJunQiDead(junQiItem.QiZuoNPC, junQiItem.BHID);
            }

            if (client.ClientData.Faction > 0)
            {
                Global.BroadcastBangHuiMsg(-1, client.ClientData.Faction,
                    StringUtil.substitute(Global.GetLang("本战盟成员成【{0}】在{1}『{2}』成功将【{3}】战盟的旗帜砍倒，值得敬佩"),
                    Global.FormatRoleName(client, client.ClientData.RoleName),
                    Global.GetServerLineName2(),
                    Global.GetMapName(client.ClientData.MapCode),
                    junQiItem.BHName),
                    true, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlySysHint);
            }

            Global.BroadcastBangHuiMsg(-1, junQiItem.BHID,
                StringUtil.substitute(Global.GetLang("很不幸，本战盟旗帜在{0}『{1}』惨被{2}【{3}】砍倒，请火速前往支援"),
                Global.GetServerLineName2(),
                Global.GetMapName(client.ClientData.MapCode),
                string.IsNullOrEmpty(client.ClientData.BHName) ? "" : StringUtil.substitute(Global.GetLang("『{0}』战盟成员"), client.ClientData.BHName),
                Global.FormatRoleName(client, client.ClientData.RoleName)),
                true, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlySysHint);
        }

        #endregion 战斗相关

        #region 处理插旗的胜负结果

        /// <summary>
        /// 最大的插旗数量
        /// </summary>
        private static int MaxInstallQiNum = 3;

        /// <summary>
        /// 插旗战的举行周日期
        /// </summary>
        private static int[] LingDiZhanWeekDays = null;

        /// <summary>
        /// 插旗战举行的时间段
        /// </summary>
        private static DateTimeRange[] LingDiZhanFightingDayTimes = null;

        /// <summary>
        /// 领地战开始多长时间后，可以判断结束的时间段
        /// </summary>
        private static DateTimeRange[] LingDiZhanEndDayTimes = null;

        /// <summary>
        /// 领地ID对应的地图编号
        /// </summary>
        private static int[] LingDiIDs2MapCodes = null;

        /// <summary>
        /// 解析插旗战的日期和时间
        /// </summary>
        public static void ParseWeekDaysTimes()
        {
            string lingDiZhanWeekDays_str = GameManager.systemParamsList.GetParamValueByName("LingDiZhanWeekDays");
            if (!string.IsNullOrEmpty(lingDiZhanWeekDays_str))
            {
                string[] lingDiZhanWeekDays_fields = lingDiZhanWeekDays_str.Split(',');
                int[] weekDays  = new int[lingDiZhanWeekDays_fields.Length];
                for (int i = 0; i < lingDiZhanWeekDays_fields.Length; i++)
                {
                    weekDays[i] = Global.SafeConvertToInt32(lingDiZhanWeekDays_fields[i]);
                }

                LingDiZhanWeekDays = weekDays;
            }

            string lingDiIDs2MapCodes_str = GameManager.systemParamsList.GetParamValueByName("LingDiIDs2MapCodes");
            if (!string.IsNullOrEmpty(lingDiIDs2MapCodes_str))
            {
                string[] lingDiIDs2MapCodes_fields = lingDiIDs2MapCodes_str.Split(',');
                int[] mapCodes = new int[lingDiIDs2MapCodes_fields.Length];
                for (int i = 0; i < lingDiIDs2MapCodes_fields.Length; i++)
                {
                    mapCodes[i] = Global.SafeConvertToInt32(lingDiIDs2MapCodes_fields[i]);
                }

                LingDiIDs2MapCodes = mapCodes;
            }

            string lingDiZhanFightingDayTimes_str = GameManager.systemParamsList.GetParamValueByName("LingDiZhanFightingDayTimes");
            LingDiZhanFightingDayTimes = Global.ParseDateTimeRangeStr(lingDiZhanFightingDayTimes_str);

            string lingDiZhanEndDayTimes_str = GameManager.systemParamsList.GetParamValueByName("LingDiZhanEndDayTimes");
            LingDiZhanEndDayTimes = Global.ParseDateTimeRangeStr(lingDiZhanEndDayTimes_str);
        }

        /// <summary>
        /// 判断周日期是否相符
        /// </summary>
        /// <param name="weekDayID"></param>
        /// <returns></returns>
        private static bool IsDayOfWeek(int weekDayID)
        {
            if (null == LingDiZhanWeekDays) return false;
            for (int i = 0; i < LingDiZhanWeekDays.Length; i++)
            {
                if (LingDiZhanWeekDays[i] == weekDayID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 是否在领地战时间段内
        /// </summary>
        /// <returns></returns>
        public static bool IsInLingDiZhanFightingTime()
        {
            DateTime now = TimeUtil.NowDateTime();
            int weekDayID = (int)now.DayOfWeek;
            if (!IsDayOfWeek(weekDayID))
            {
                return false;
            }

            int endMinute = 0;
            return Global.JugeDateTimeInTimeRange(now, LingDiZhanFightingDayTimes, out endMinute, false);
        }

        /// <summary>
        /// 是否在领地战结束时间段内
        /// </summary>
        /// <returns></returns>
        private static bool IsInLingDiZhanEndTime()
        {
            DateTime now = TimeUtil.NowDateTime();
            int weekDayID = (int)now.DayOfWeek;
            if (!IsDayOfWeek(weekDayID))
            {
                return false;
            }

            int endMinute = 0;
            return Global.JugeDateTimeInTimeRange(now, LingDiZhanEndDayTimes, out endMinute, false);
        }

        /// <summary>
        /// 根据地图编号获取领地ID
        /// </summary>
        public static int GetLingDiIDBy2MapCode(int mapCode)
        {
            if (null == LingDiIDs2MapCodes) return 0;
            for (int i = 0; i < LingDiIDs2MapCodes.Length; i++)
            {
                if (LingDiIDs2MapCodes[i] == mapCode)
                {
                    return (i + 1);
                }
            }

            //王城也属于皇宫的领地
            if (Global.GetWangChengMapCode() == mapCode)
            {
                return (int)LingDiIDs.HuangCheng;
            }

            return 0;
        }

        /// <summary>
        /// 根据领地ID获取地图编号
        /// </summary>
        public static int GetMapCodeByLingDiID(int lingDiID)
        {
            if (null == LingDiIDs2MapCodes) return 0;
            return LingDiIDs2MapCodes[lingDiID];
        }

        /// <summary>
        /// 领地战的状态类型
        /// </summary>
        public static LingDiZhanStates LingDiZhanState = LingDiZhanStates.None;

        /// <summary>
        /// 是否能插旗
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public static bool CanInstallJunQi(GameClient client)
        {
            if (JugeLingDiZhanEndByMapCode(client.ClientData.MapCode))
            {
                return false;
            }

            return (LingDiZhanStates.Fighting == LingDiZhanState);
        }

        /// <summary>
        /// 领地战结果
        /// </summary>
        private static Dictionary<int, bool> LingDiZhanResultsDict = new Dictionary<int, bool>();

        /// <summary>
        /// 根据地图编号判断领地战是否已经结束
        /// </summary>
        /// <param name="mapCode"></param>
        private static bool JugeLingDiZhanEndByMapCode(int mapCode)
        {
            bool result = false;
            lock (LingDiZhanResultsDict)
            {
                if (!LingDiZhanResultsDict.TryGetValue(mapCode, out result))
                {
                    return false;
                }
            }

            return result;
        }

        /// <summary>
        /// 根据地图编号添加领地战的结果
        /// </summary>
        /// <param name="mapCode"></param>
        private static void AddLingDiZhanEndResultByMapCode(int mapCode, bool result)
        {
            lock (LingDiZhanResultsDict)
            {
                LingDiZhanResultsDict[mapCode] = result;
            }
        }

        /// <summary>
        /// 处理领地战的战斗结果
        /// </summary>
        public static void ProcessLingDiZhanResult()
        {
            if (Global.GetBangHuiFightingLineID() != GameManager.ServerLineID)
            {
                return;
            }

            //配置错误则不进行领地战的处理
            if (null == LingDiIDs2MapCodes || LingDiIDs2MapCodes.Length != (int)LingDiIDs.MaxVal - 1)
            {
                return;
            }

            if (LingDiZhanStates.None == LingDiZhanState) //非战斗状态
            {
                //是否在领地战时间段内
                if (IsInLingDiZhanFightingTime())
                {
                    LingDiZhanResultsDict.Clear();
                    LingDiZhanState = LingDiZhanStates.Fighting;

                    //判断如果某个帮会的帮旗全部插满了，则就是获胜
                    for (int i = (int)LingDiIDs.YouZhou; i <= (int)LingDiIDs.YouZhou; i++)
                    {
                        //通知地图变动信息
                        NotifyAllLingDiMapInfoData(LingDiIDs2MapCodes[i - 1]);
                    }
                }
            }
            else //战斗状态
            {
                //是否在领地战时间段内
                if (IsInLingDiZhanFightingTime())
                {
                    //是否在领地战结束时间段内
                    if (IsInLingDiZhanEndTime())
                    {
                        //判断如果某个帮会的帮旗全部插满了，则就是获胜
                        for (int i = (int)LingDiIDs.YouZhou; i <= (int)LingDiIDs.YouZhou; i++)
                        {
                            if (JugeLingDiZhanEndByMapCode(LingDiIDs2MapCodes[i - 1]))
                            {
                                continue; //已经处理过了
                            }

                            int bhid = 0;
                            int totalJunQiNum = CalcJunQiNumByMapCode(LingDiIDs2MapCodes[i - 1], out bhid);
                            if (totalJunQiNum >= MaxInstallQiNum)
                            {
                                //已经胜利，交给胜利处理的流程
                                AddLingDiZhanEndResultByMapCode(LingDiIDs2MapCodes[i - 1], true);

                                //处理领地战的结果
                                HandleLingDiZhanResultByMapCode(i, LingDiIDs2MapCodes[i - 1], bhid, true);

                                /// 处理领地战结束时的奖励
                                ProcessHuangChengFightingEndAwards();
                            }
                        }
                    }
                    else
                    {
                        /// 定时给在场的玩家增加经验
                        ProcessTimeAddRoleExp();
                    }
                }
                else
                {
                    LingDiZhanState = LingDiZhanStates.None; //结束战斗

                    //判断那个帮会的帮旗最多，就是那个帮会获胜
                    for (int i = (int)LingDiIDs.YouZhou; i <= (int)LingDiIDs.YouZhou; i++)
                    {
                        if (JugeLingDiZhanEndByMapCode(LingDiIDs2MapCodes[i - 1]))
                        {
                            continue; //已经处理过了
                        }

                        int bhid = 0;
                        int totalJunQiNum = CalcJunQiNumByMapCode(LingDiIDs2MapCodes[i - 1], out bhid);

                        //已经胜利，交给胜利处理的流程
                        AddLingDiZhanEndResultByMapCode(LingDiIDs2MapCodes[i - 1], true);

                        //处理领地战的结果
                        HandleLingDiZhanResultByMapCode(i, LingDiIDs2MapCodes[i - 1], bhid, true);
                    }

                    /// 处理领地战结束时的奖励
                    ProcessHuangChengFightingEndAwards();
                }
            }
        }

        /// <summary>
        /// 通知GameServer同步领地战结果
        /// </summary>
        public static void NotifySyncBangHuiLingDiZhanResult(int lingDiID, int mapCode, int bhid, int zoneID, string bhName)
        {
            //通知其他线路
            string gmCmdData = string.Format("-syncldzresult {0} {1} {2} {3} {4}", lingDiID, mapCode, bhid, zoneID, bhName);

            //转发GM消息到DBServer
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", 0, "", 0, "", 0, gmCmdData, 0, 0, GameManager.ServerLineID),
                null, GameManager.LocalServerId);
        }

        /// <summary>
        /// 处理领地战的结果
        /// </summary>
        /// <param name="lingDiID"></param>
        /// <param name="mapCode"></param>
        /// <param name="bhid"></param>
        public static void HandleLingDiZhanResultByMapCode(int lingDiID, int mapCode, int bhid, bool sendToOtherLine, bool lingDiOkHint = true)
        {
            //通过帮会查找一个帮旗数据
            JunQiItem junQiItem = null;
            if (bhid > 0)
            {
                junQiItem = FindJunQiByBHID(bhid);
            }

            if (sendToOtherLine)
            {
                //通知数据库记录领地的归属
                //发送内部GM命令通知其他的GameServer更新内不能中的领地的归属
                //为领地更新帮会ID信息
                Global.UpdateLingDiForBH(lingDiID, bhid);

                //清空指定地图上的所有旗帜
                ProcessDelAllJunQiByMapCode(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, mapCode);

                //将指定地图上插满指定帮会的所有旗帜
                if (null != junQiItem)
                {
                    ProcessAllNewJunQiByMapCode(mapCode, bhid, junQiItem.ZoneID, junQiItem.BHName);
                }
            }

            //为领地的下属地图安插帮旗
            //根据领地ID来获取项
            if (null != junQiItem)
            {
                BangHuiLingDiItemData bangHuiLingDiItemData = GetItemByLingDiID(lingDiID);
                if (null != bangHuiLingDiItemData)
                {
                    InstallJunQiOnNormalMap(lingDiID, bhid, junQiItem.ZoneID, junQiItem.BHName, true);
                }
            }
            else
            {
                //为领地的下属地图清空帮旗
                ClearJunQiOnNormalMap(lingDiID);
            }

            //通知GameServer同步领地战结果
            if (sendToOtherLine)
            {
                if (null != junQiItem)
                {
                    NotifySyncBangHuiLingDiZhanResult(lingDiID, mapCode, bhid, junQiItem.ZoneID, junQiItem.BHName);
                }
            }

            //通知地图变动信息
            NotifyAllLingDiMapInfoData(mapCode);

            //夺取领地的提示
            if (lingDiOkHint)
            {
                if (null != junQiItem)
                {
                    Global.BroadcastLingDiOkHint(junQiItem.BHName, mapCode);
                }
            }
        }

        /// <summary>
        /// 处理领地战的结果
        /// </summary>
        /// <param name="lingDiID"></param>
        /// <param name="mapCode"></param>
        /// <param name="bhid"></param>
        public static void HandleLuoLanChengZhanResult(int lingDiID, int mapCode, int bhid, string bhName, bool sendToOtherLine, bool lingDiOkHint = true)
        {
            //为领地更新帮会ID信息
            Global.UpdateLingDiForBH(lingDiID, bhid);

            //通知地图变动信息
            NotifyAllLingDiMapInfoData(mapCode);

            //夺取领地的提示
            if (lingDiOkHint)
            {
                Global.BroadcastLingDiOkHint(bhName, mapCode);
            }
        }

        /// <summary>
        /// 处理领地战的结果
        /// </summary>
        /// <param name="lingDiID"></param>
        /// <param name="mapCode"></param>
        /// <param name="bhid"></param>
        public static void HandleLingDiZhanResultByMapCode2(int lingDiID, int mapCode, int bhid, int zoneID, string bhName)
        {
            //为领地的下属地图安插帮旗
            //根据领地ID来获取项
            if (bhid > 0)
            {
                BangHuiLingDiItemData bangHuiLingDiItemData = GetItemByLingDiID(lingDiID);
                if (null != bangHuiLingDiItemData)
                {
                    InstallJunQiOnNormalMap(lingDiID, bhid, zoneID, bhName, true);
                }
            }
            else
            {
                //为领地的下属地图清空帮旗
                ClearJunQiOnNormalMap(lingDiID);
            }

            //通知地图变动信息
            NotifyAllLingDiMapInfoData(mapCode);
        }

        /// <summary>
        /// 根据领地的帮会分布初始化插入的帮旗
        /// </summary>
        public static void InitLingDiJunQi()
        {
            //配置错误则不进行领地战的处理
            if (null == LingDiIDs2MapCodes || LingDiIDs2MapCodes.Length != (int)LingDiIDs.MaxVal - 1)
            {
                return;
            }

            if (Global.GetBangHuiFightingLineID() == GameManager.ServerLineID)
            {
                //判断那个帮会的帮旗最多，就是那个帮会获胜
                for (int i = (int)LingDiIDs.YouZhou; i <= (int)LingDiIDs.YouZhou; i++)
                {
                    //根据领地ID来获取项
                    BangHuiLingDiItemData bangHuiLingDiItemData = GetItemByLingDiID(i);
                    if (null == bangHuiLingDiItemData) continue;
                    if (bangHuiLingDiItemData.BHID <= 0) continue;

                    //将指定地图上插满指定帮会的所有旗帜
                    ProcessAllNewJunQiByMapCode(LingDiIDs2MapCodes[i - 1], bangHuiLingDiItemData.BHID, bangHuiLingDiItemData.ZoneID, bangHuiLingDiItemData.BHName);
                }
            }

            //为领地的下属地图安插帮旗
            /*for (int i = (int)LingDiIDs.HuangCheng; i < (int)LingDiIDs.MaxVal; i++)
            {
                //根据领地ID来获取项
                BangHuiLingDiItemData bangHuiLingDiItemData = GetItemByLingDiID(i);
                if (null == bangHuiLingDiItemData) continue;
                if (bangHuiLingDiItemData.BHID <= 0) continue;

                InstallJunQiOnNormalMap(i, bangHuiLingDiItemData.BHID, bangHuiLingDiItemData.ZoneID, bangHuiLingDiItemData.BHName, false);
            }*/
        }

        /// <summary>
        /// 为领地的下属地图安插帮旗
        /// </summary>
        /// <param name="lingDiID"></param>
        /// <param name="zoneID"></param>
        /// <param name="bhName"></param>
        public static void InstallJunQiOnNormalMap(int lingDiID, int bhid, int zoneID, string bhName, bool forceClean = true)
        {
            List<int> mapCodesList = new List<int>();
            SystemXmlItem systemQiZiItem = null;
            foreach (var key in GameManager.systemLingQiMapQiZhiMgr.SystemXmlItemDict.Keys)
            {
                systemQiZiItem = GameManager.systemLingQiMapQiZhiMgr.SystemXmlItemDict[key];
                if (lingDiID == (int)systemQiZiItem.GetIntValue("LingDiID"))
                {
                    mapCodesList.Add(key);
                }
            }

            //处理获取帮旗名称的操作
            string junQiName = JunQiManager.GetJunQiNameByBHID(bhid);

            //处理获取帮旗级别的操作
            int junQiLevel = JunQiManager.GetJunQiLevelByBHID(bhid);

            //处理普通地图的安插帮旗的操作
            for (int i = 0; i < mapCodesList.Count; i++)
            {
                //清空指定地图上的所有旗帜
                if (forceClean)
                {
                    ProcessDelAllJunQiByMapCode(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, mapCodesList[i]);
                }

                ProcessNewJunQi(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, mapCodesList[i], bhid, zoneID, bhName, -1, junQiName, junQiLevel);
            }
        }

        /// <summary>
        /// 为领地的下属地图清空帮旗
        /// </summary>
        /// <param name="lingDiID"></param>
        /// <param name="zoneID"></param>
        /// <param name="bhName"></param>
        public static void ClearJunQiOnNormalMap(int lingDiID)
        {
            List<int> mapCodesList = new List<int>();
            SystemXmlItem systemQiZiItem = null;
            foreach (var key in GameManager.systemLingQiMapQiZhiMgr.SystemXmlItemDict.Keys)
            {
                systemQiZiItem = GameManager.systemLingQiMapQiZhiMgr.SystemXmlItemDict[key];
                if (lingDiID == (int)systemQiZiItem.GetIntValue("LingDiID"))
                {
                    mapCodesList.Add(key);
                }
            }

            //处理普通地图的安插帮旗的操作
            for (int i = 0; i < mapCodesList.Count; i++)
            {
                //清空指定地图上的所有旗帜
                ProcessDelAllJunQiByMapCode(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, mapCodesList[i]);
            }
        }

        /// <summary>
        /// 帮会解散时发出取消领地所属的帮旗的操作
        /// </summary>
        public static void SendClearJunQiCmd(int bhid)
        {
            //配置错误则不进行领地战的处理
            if (null == LingDiIDs2MapCodes || LingDiIDs2MapCodes.Length != (int)LingDiIDs.MaxVal - 1)
            {
                return;
            }

            //通知其他线路
            string gmCmdData = string.Format("-clearmap {0}", bhid);

            //转发GM消息到DBServer
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", -1, "", 0, "", 0, gmCmdData, 0, 0, -1),
                null, GameManager.LocalServerId);
        }

        #endregion 处理插旗的胜负结果

        #region 地图战斗状态数据

        /// <summary>
        /// 获取地图战斗状态数据
        /// </summary>
        /// <returns></returns>
        public static LingDiMapInfoData GetLingDiMapData(GameClient client)
        {
            //防止普通地图上的旗帜被攻击
            int lingDiID = GetLingDiIDBy2MapCode(client.ClientData.MapCode);
            //if (lingDiID < (int)LingDiIDs.YouZhou || lingDiID > (int)LingDiIDs.XingYang)
            if (lingDiID != (int)LingDiIDs.YouZhou)
            {
                return null;
            }

            return FormatLingDiMapData(client.ClientData.MapCode);
        }

        /// <summary>
        /// 获取地图战斗状态数据
        /// </summary>
        /// <returns></returns>
        private static LingDiMapInfoData FormatLingDiMapData(int mapCode)
        {
            DateTime now = TimeUtil.NowDateTime();
            long fightingEndTime = 0;
            long fightingStartTime = 0;
            if (null != LingDiZhanFightingDayTimes && LingDiZhanFightingDayTimes.Length > 0)
            {
                if (!JugeLingDiZhanEndByMapCode(mapCode))
                {
                    if (LingDiZhanStates.Fighting == LingDiZhanState)
                    {
                        DateTime endDateTime = new DateTime(now.Year, now.Month, now.Day, LingDiZhanFightingDayTimes[0].EndHour, LingDiZhanFightingDayTimes[0].EndMinute, 0);
                        fightingEndTime = endDateTime.Ticks / 10000;

                        DateTime startDateTime = new DateTime(now.Year, now.Month, now.Day, LingDiZhanFightingDayTimes[0].FromHour, LingDiZhanFightingDayTimes[0].FromMinute, 0);
                        fightingStartTime = startDateTime.Ticks / 10000;
                    }
                }
            }

            LingDiMapInfoData lingDiMapInfoData = new LingDiMapInfoData()
            {
                FightingEndTime = fightingEndTime,
                FightingStartTime = fightingStartTime,
                BHNameDict = new Dictionary<int,string>(),
            };

            List<int> npcList = GetQiZuoNPCIDList(mapCode);
            if (null != npcList)
            {
                for (int i = 0; i < npcList.Count; i++)
                {
                    lingDiMapInfoData.BHNameDict[npcList[i]] = GetBHNameByNPCID(npcList[i]);
                }
            }

            return lingDiMapInfoData;
        }

        /// <summary>
        /// 通知地图变动信息
        /// </summary>
        /// <param name="mapCode"></param>
        public static void NotifyAllLingDiMapInfoData(int mapCode)
        {
            LingDiMapInfoData lingDiMapInfoData = FormatLingDiMapData(mapCode);

            //通知在线的所有人(不限制地图)领地信息数据通知
            GameManager.ClientMgr.NotifyAllLingDiMapInfoData(mapCode, lingDiMapInfoData);
        }

        #endregion 地图战斗状态数据

        #region 定时给在场的玩家家经验

        /// <summary>
        /// 定时给予收益
        /// </summary>
        private static long LastAddBangZhanAwardsTicks = 0;

        /// <summary>
        /// 定时给在场的玩家增加经验
        /// </summary>
        private static void ProcessTimeAddRoleExp()
        {
            long ticks = TimeUtil.NOW();
            if (ticks - LastAddBangZhanAwardsTicks < (10 * 1000))
            {
                return;
            }

            LastAddBangZhanAwardsTicks = ticks;

            //先找出当前大乱斗中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(LingDiIDs2MapCodes[(int)LingDiIDs.YouZhou - 1]);
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                //if (c.ClientData.CurrentLifeV <= 0) continue;

                /// 处理用户的经验奖励
                BangZhanAwardsMgr.ProcessBangZhanAwards(c);
            }
        }

        #endregion 定时给在场的玩家家经验

        #region 战斗结束奖励处理

        /// <summary>
        ///  给予奖励的时间
        /// </summary>
        private static int MaxHavingAwardsSecs = (20 * 60 * 1000);

        /// <summary>
        /// 获取角色的奖励经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static int GetExperienceAwards(GameClient client, bool success)
        {
            if (success)
            {
                return (500 * 10000);
            }

            return (250 * 10000);
        }

        /// <summary>
        /// 获取角色的荣耀奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static int GetRongYuAwards(GameClient client, bool success)
        {
            if (success)
            {
                return (5000);
            }

            return (2500);
        }

        /// <summary>
        /// 处理用户的经验奖励
        /// </summary>
        /// <param name="client"></param>
        private static void ProcessRoleExperienceAwards(GameClient client, bool success)
        {
            //奖励用户经验
            //异步写数据库，写入经验和级别
            int experience = GetExperienceAwards(client, success);

            //处理角色经验
            GameManager.ClientMgr.ProcessRoleExperience(client, experience, true, false);
        }

        /// <summary>
        /// 处理用户的军贡奖励
        /// </summary>
        /// <param name="client"></param>
        private static void ProcessRoleBangGongAwards(GameClient client, bool success)
        {
            //奖励用户军贡
            //异步写数据库，写入军贡
            int rongYu = GetRongYuAwards(client, success);
            if (rongYu > 0)
            {
                //更新用户帮贡
                GameManager.ClientMgr.ModifyRongYuValue(client, rongYu, true, true);

                GameManager.SystemServerEvents.AddEvent(string.Format("角色获取荣誉, roleID={0}({1}), BangGong={2}, newBangGong={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.BangGong, rongYu), EventLevels.Record);
            }
        }

        /// <summary>
        /// 是否能够获取奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static bool CanGetAWards(GameClient client, long nowTicks)
        {
            if (nowTicks - client.ClientData.EnterMapTicks < MaxHavingAwardsSecs)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 处理领地战结束时的奖励
        /// </summary>
        private static void ProcessHuangChengFightingEndAwards()
        {
            //先找出当前大乱斗中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(LingDiIDs2MapCodes[(int)LingDiIDs.YouZhou - 1]);
            if (null == objsList) return;

            int successBHID = -1;
            int lingDiID = (int)LingDiIDs.YouZhou;
            if (lingDiID > 0)
            {
                BangHuiLingDiItemData bangHuiLingDiItemData = JunQiManager.GetItemByLingDiID(lingDiID);
                if (null != bangHuiLingDiItemData && bangHuiLingDiItemData.BHID > 0)
                {
                    successBHID = bangHuiLingDiItemData.BHID;
                }
            }

            long nowTicks = TimeUtil.NOW();
            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;
                if (c.ClientData.CurrentLifeV <= 0) continue;

                //是否能够获取奖励
                if (!CanGetAWards(c, nowTicks))
                {
                    continue;
                }

                /// 处理用户的经验奖励
                ProcessRoleExperienceAwards(c, (successBHID == c.ClientData.Faction));

                /// 处理用户的金钱奖励
                ProcessRoleBangGongAwards(c, (successBHID == c.ClientData.Faction));
            }
        }

        #endregion 战斗结束奖励处理
    }
}
