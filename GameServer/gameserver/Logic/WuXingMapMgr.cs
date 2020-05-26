using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System.Windows;
//using System.Windows.Documents;
using GameServer.Server;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 五行奇阵地图项
    /// </summary>
    public class WuXingMapItem
    {
        /// <summary>
        /// 流水ID
        /// </summary>
        public int GlobalID = 0;

        /// <summary>
        /// NPCID列表
        /// </summary>
        public List<int> OtherNPCIDList = new List<int>();

        /// <summary>
        /// 要传送的地图编号列表
        /// </summary>
        public List<int> GoToMapCodeList = new List<int>();

        /// <summary>
        /// 排序的日期
        /// </summary>
        public int DayID = -1;
    };

    /// <summary>
    /// 五行奇阵NPC项
    /// </summary>
    public class WuXingNPCItem
    {
        /// <summary>
        /// NPCID
        /// </summary>
        public int NPCID = 0;

        /// <summary>
        /// 地图编号
        /// </summary>
        public int MapCode = 0;

        /// <summary>
        /// 需要的物品道具ID
        /// </summary>
        public int NeedGoodsID = 0;

        /// <summary>
        /// 五行奇阵地图项
        /// </summary>
        public WuXingMapItem MapItem = null;
    };

    /// <summary>
    /// 五行奇阵地图奖励项
    /// </summary>
    public class WuXingMapAwardItem
    {
        /// <summary>
        /// 地图编号
        /// </summary>
        public int MapCode = 0;

        /// <summary>
        /// 铜钱奖励
        /// </summary>
        public int Money1 = 0;

        /// <summary>
        /// 经验奖励
        /// </summary>
        public double ExpXiShu = 0;

        /// <summary>
        /// 经验奖励
        /// </summary>
        public List<GoodsData> GoodsDataList = null;

        /// <summary>
        /// 最小的幸运点比例
        /// </summary>
        public int MinBlessPoint = 0;

        /// <summary>
        /// 最大的幸运点比例
        /// </summary>
        public int MaxBlessPoint = 0;
    };

    /// <summary>
    /// 随机五行奇阵地图管理
    /// </summary>
    public class WuXingMapMgr
    {
        #region 五行奇阵的配置管理

        /// <summary>
        /// 根据流水号的五行奇阵地图管理字典
        /// </summary>
        private static Dictionary<int, WuXingMapItem> WuXingMapDict = new Dictionary<int, WuXingMapItem>();

        /// <summary>
        /// 根据流水号的五行奇阵NPC管理字典
        /// </summary>
        private static Dictionary<string, WuXingNPCItem> WuXingNPCDict = new Dictionary<string, WuXingNPCItem>();

        /// <summary>
        /// 对于整数的列表进行随机排序后，返回一个新的列表
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static List<int> RandomIntList(List<int> list)
        {
            if (null == list) return null;

            List<int> newList = new List<int>();
            foreach (int item in list)
            {
                int index = Global.GetRandomNumber(0, newList.Count);
                newList.Insert(index, item);
            }

            return newList;
        }

        /// <summary>
        /// 获取要传送的地图编号根据NPCID和当前地图的ID
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="npcID"></param>
        /// <returns></returns>
        public static int GetNextMapCodeByNPCID(int mapCode, int npcID)
        {
            WuXingNPCItem wuXingNPCItem = null;
            string key = string.Format("{0}_{1}", mapCode, npcID);
            int dayID = TimeUtil.NowDateTime().DayOfYear;

            //先锁定
            lock (WuXingNPCDict)
            {
                if (!WuXingNPCDict.TryGetValue(key, out wuXingNPCItem))
                {
                    return -1;
                }

                if (null == wuXingNPCItem.MapItem)
                {
                    return -1;
                }

                if (dayID != wuXingNPCItem.MapItem.DayID)
                {
                    wuXingNPCItem.MapItem.DayID = dayID;
                    wuXingNPCItem.MapItem.GoToMapCodeList = RandomIntList(wuXingNPCItem.MapItem.GoToMapCodeList);
                }

                if (null == wuXingNPCItem.MapItem.GoToMapCodeList ||
                        null == wuXingNPCItem.MapItem.OtherNPCIDList ||
                        wuXingNPCItem.MapItem.GoToMapCodeList.Count != wuXingNPCItem.MapItem.OtherNPCIDList.Count)
                {
                    return -1;
                }

                for (int i = 0; i < wuXingNPCItem.MapItem.OtherNPCIDList.Count; i++)
                {
                    if (npcID == wuXingNPCItem.MapItem.OtherNPCIDList[i])
                    {
                        return wuXingNPCItem.MapItem.GoToMapCodeList[i];
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 获取要传送的地图编号根据NPCID和当前地图的ID
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="npcID"></param>
        /// <returns></returns>
        public static int GetNeedGoodsIDByNPCID(int mapCode, int npcID)
        {
            WuXingNPCItem wuXingNPCItem = null;
            string key = string.Format("{0}_{1}", mapCode, npcID);

            //先锁定
            lock (WuXingNPCDict)
            {
                if (!WuXingNPCDict.TryGetValue(key, out wuXingNPCItem))
                {
                    return -1;
                }

                return wuXingNPCItem.NeedGoodsID;
            }
        }

        /// <summary>
        /// 将配置项的字符串转成整数列表
        /// </summary>
        /// <param name="extProps"></param>
        /// <returns></returns>
        private static List<int> Str2IntArray(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            List<int> intList = new List<int>();
            string[] fields = str.Split('|');
            for (int i = 0; i < fields.Length; i++)
            {
                try
                {
                    intList.Add(Convert.ToInt32(fields[i]));
                }
                catch (Exception)
                {
                }
            }

            return intList;
        }

        /// <summary>
        /// 解析全局地图分配配置项
        /// </summary>
        /// <param name="globalID"></param>
        /// <param name="otherNPCIDs"></param>
        /// <param name="goToMaps"></param>
        private static WuXingMapItem ParseGlobalConfigItem(int globalID, string otherNPCIDs, string goToMaps)
        {
            WuXingMapItem wuXingMapItem = null;
            if (WuXingMapDict.TryGetValue(globalID, out wuXingMapItem))
            {
                return wuXingMapItem;
            }

            wuXingMapItem = new WuXingMapItem()
            {
                GlobalID = globalID,
                OtherNPCIDList = Str2IntArray(otherNPCIDs),
                GoToMapCodeList = Str2IntArray(goToMaps),
            };

            //判断是否异常
            if (null == wuXingMapItem.OtherNPCIDList || null == wuXingMapItem.GoToMapCodeList ||
                wuXingMapItem.OtherNPCIDList.Count != wuXingMapItem.GoToMapCodeList.Count)
            {
                throw new Exception(string.Format("解析五行奇阵配置文件时，解析NPC列表或者地图列表失败, GlobalID={0}", globalID));
            }

            WuXingMapDict[globalID] = wuXingMapItem;
            return wuXingMapItem;
        }

        /// <summary>
        /// 解析五行奇阵的配置Xml项
        /// </summary>
        /// <param name="systemXmlItem"></param>
        private static void ParseWuXingXmlItem(SystemXmlItem systemXmlItem)
        {
            int npcID = systemXmlItem.GetIntValue("NPCID");
            int mapCode = systemXmlItem.GetIntValue("MapCode");
            int needGoodsID = systemXmlItem.GetIntValue("NeedGoodsID");
            int globalID = systemXmlItem.GetIntValue("GlobalID");
            string otherNPCIDs = systemXmlItem.GetStringValue("OtherNPCIDs");
            string goToMaps = systemXmlItem.GetStringValue("GoToMaps");

            //解析全局地图分配配置项
            WuXingMapItem wuXingMapItem = ParseGlobalConfigItem(globalID, otherNPCIDs, goToMaps);
            WuXingNPCItem wuXingNPCItem = new WuXingNPCItem()
            {
                NPCID = npcID,
                MapCode = mapCode,
                NeedGoodsID = needGoodsID,
                MapItem = wuXingMapItem,
            };

            string key = string.Format("{0}_{1}", mapCode, npcID);
            WuXingNPCDict[key] = wuXingNPCItem;
        }

        /// <summary>
        /// 加载五行奇阵的配置文件
        /// </summary>
        public static void LoadXuXingConfig()
        {
            XElement xml = null;
            string fileName = "Config/WuXing.xml";

            try
            {
                xml = XElement.Load(Global.GameResPath(fileName));
                if (null == xml)
                {
                    throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            SystemXmlItem systemXmlItem = null;
            IEnumerable<XElement> nodes = xml.Elements();
            foreach (var node in nodes)
            {
                systemXmlItem = new SystemXmlItem()
                {
                    XMLNode = node,
                };

                //解析五行奇阵的配置Xml项
                ParseWuXingXmlItem(systemXmlItem);
            }
        }

        #endregion 五行奇阵的配置管理

        #region 五行奇阵的最终奖励

        /// <summary>
        /// 五行奇阵的领取奖励情况
        /// </summary>
        private static Dictionary<int, int> ClientsAwardsDict = new Dictionary<int, int>();

        /// <summary>
        /// 五行奇阵奖励项
        /// </summary>
        private static WuXingMapAwardItem TheWuXingMapAwardItem = null;

        /// <summary>
        /// 将物品字符串列表解析成物品数据列表
        /// </summary>
        /// <param name="goodsStr"></param>
        /// <returns></returns>
        private static List<GoodsData> ParseGoodsDataList(string[] fields)
        {
            List<GoodsData> goodsDataList = new List<GoodsData>();
            for (int i = 0; i < fields.Length; i++)
            {
                string[] sa = fields[i].Split(',');
                if (sa.Length != 6)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("解析WuXingAwards.xml文件中的奖励项时失败, 物品配置项个数错误"));
                    continue;
                }

                int[] goodsFields = Global.StringArray2IntArray(sa);

                //获取物品数据
                GoodsData goodsData = Global.GetNewGoodsData(goodsFields[0], goodsFields[1], goodsFields[2], goodsFields[3], goodsFields[4], goodsFields[5], 0, 0);
                goodsDataList.Add(goodsData);
            }

            return goodsDataList;
        }

        /// <summary>
        /// 解析五行奇阵的奖励项
        /// </summary>
        /// <param name="systemXmlItem"></param>
        public static void ParseWuXingAwardItem(SystemXmlItem systemXmlItem)
        {
            List<GoodsData> goodsDataList = null;
            string goodsIDs = systemXmlItem.GetStringValue("GoodsIDs");
            if (!string.IsNullOrEmpty(goodsIDs))
            {
                string[] fields = goodsIDs.Split('|');
                if (fields.Length > 0)
                {
                    goodsDataList = ParseGoodsDataList(fields);
                }
                else
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("解析WuXingAwards.xml配置项中的物品奖励失败, MapCode={0}", systemXmlItem.GetIntValue("MapCode")));
                }
            }
            else
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析WuXingAwards.xml配置项中的物品奖励失败, MapCode={0}", systemXmlItem.GetIntValue("MapCode")));
            }

            TheWuXingMapAwardItem = new WuXingMapAwardItem()
            {
                MapCode = systemXmlItem.GetIntValue("MapCode"),
                Money1 = systemXmlItem.GetIntValue("Moneyaward"),
                ExpXiShu = systemXmlItem.GetDoubleValue("ExpXiShu"),
                GoodsDataList = goodsDataList,
                MinBlessPoint = systemXmlItem.GetIntValue("MinBlessPoint"),
                MaxBlessPoint = systemXmlItem.GetIntValue("MaxBlessPoint"),
            };
        }

        /// <summary>
        /// 加载五行奇阵奖励项
        /// </summary>
        public static void LoadWuXingAward()
        {
            XElement xml = null;
            string fileName = "Config/WuXingAwards.xml";

            try
            {
                xml = XElement.Load(Global.GameResPath(fileName));
                if (null == xml)
                {
                    throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            SystemXmlItem systemXmlItem = null;
            IEnumerable<XElement> nodes = xml.Elements();
            foreach (var node in nodes)
            {
                systemXmlItem = new SystemXmlItem()
                {
                    XMLNode = node,
                };

                //解析五行奇阵的奖励Xml项
                ParseWuXingAwardItem(systemXmlItem);
                break;
            }

            if (null == TheWuXingMapAwardItem)
            {
                throw new Exception(string.Format("加载五行奇阵的最顶层奖励项失败!"));
            }
        }

        /// <summary>
        /// 是否能获取五行奇阵的奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool CanGetWuXingAward(GameClient client)
        {
            int currentDayID = TimeUtil.NowDateTime().DayOfYear;
            int wuXingDayID = -1;
            int wuXingNum = 0;
            if (null != client.ClientData.MyRoleDailyData)
            {
                wuXingDayID = client.ClientData.MyRoleDailyData.WuXingDayID;
                wuXingNum = client.ClientData.MyRoleDailyData.WuXingNum;
            }

            if (currentDayID != wuXingDayID)
            {
                return true;
            }

            return (wuXingNum <= 0);
        }

        /// <summary>
        /// 获取五行奇阵的奖励，一天只能获取一次
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessWuXingAward(GameClient client)
        {
            //已经领取过了，就不能再领取
            if (!CanGetWuXingAward(client))
            {
                return;
            }

            if (null == TheWuXingMapAwardItem)
            {
                return;
            }

            if (null != TheWuXingMapAwardItem.GoodsDataList)
            {
                //判断背包是否空间足够
                if (!Global.CanAddGoodsDataList(client, TheWuXingMapAwardItem.GoodsDataList))
                {
                    GameManager.ClientMgr.NotifyImportantMsg(
                        Global._TCPManager.MySocketListener,
                        Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("背包中空格不足，请清理出足够的空格后，再获取五行奇阵的奖励")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoBagGrid);
                    return;
                }
            }

            int blessPoint = 0;

            //判断如果有坐骑的祝福点奖励，则要判断是否在骑乘状态
            if (TheWuXingMapAwardItem.MinBlessPoint >= 0 && TheWuXingMapAwardItem.MaxBlessPoint >= 0)
            {
                blessPoint = Global.GetRandomNumber(TheWuXingMapAwardItem.MinBlessPoint, TheWuXingMapAwardItem.MaxBlessPoint);
                if (blessPoint > 0)
                {
                    if (client.ClientData.HorseDbID <= 0)
                    {
                        GameManager.ClientMgr.NotifyImportantMsg(
                            Global._TCPManager.MySocketListener,
                            Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("获取五行奇阵的奖励的坐骑临时养成点，必须处于骑乘状态")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);

                        return;
                    }
                }
            }

            if (Global.FilterFallGoods(client)) //是否奖励物品
            {
                //奖励用户物品
                if (null != TheWuXingMapAwardItem.GoodsDataList)
                {
                    for (int i = 0; i < TheWuXingMapAwardItem.GoodsDataList.Count; i++)
                    {
                        //想DBServer请求加入某个新的物品到背包中
                        //添加物品
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool,
                            client, TheWuXingMapAwardItem.GoodsDataList[i].GoodsID, TheWuXingMapAwardItem.GoodsDataList[i].GCount, TheWuXingMapAwardItem.GoodsDataList[i].Quality, "", TheWuXingMapAwardItem.GoodsDataList[i].Forge_level, TheWuXingMapAwardItem.GoodsDataList[i].Binding, 0, "", true, 1, /**/"五行奇阵奖励物品", Global.ConstGoodsEndTime, TheWuXingMapAwardItem.GoodsDataList[i].AddPropIndex, TheWuXingMapAwardItem.GoodsDataList[i].BornIndex, TheWuXingMapAwardItem.GoodsDataList[i].Lucky, TheWuXingMapAwardItem.GoodsDataList[i].Strong);
                    }
                }
            }

            //添加角色的ID+日期的ID的奖励领取状态
            //更新角色的日常数据_五行奇阵领取奖励数量
            GameManager.ClientMgr.UpdateRoleDailyData_WuXingNum(client, 1);

            //奖励用户经验
            //异步写数据库，写入经验和级别
            double expXiShu = TheWuXingMapAwardItem.ExpXiShu;
            int experience = (int)Math.Pow(client.ClientData.Level, expXiShu);

            //处理VIP月卡
            if (DBRoleBufferManager.ProcessMonthVIP(client) > 0.0)
            {
                experience = (int)(experience * 1.50);
            }

            //处理角色经验
            GameManager.ClientMgr.ProcessRoleExperience(client, experience, true, false);

            //五行奇阵通关获取经验通知
            Global.BroadcastWuXingExperience(client, experience);

            //奖励用户金钱
            //异步写数据库，写入金钱
            int money = TheWuXingMapAwardItem.Money1;
            if (-1 != money)
            {
                //过滤金币奖励
                money = Global.FilterValue(client, money);

                //更新用户的铜钱
                GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, money, "五行奇阵", false);

                GameManager.SystemServerEvents.AddEvent(string.Format("角色获取金钱, roleID={0}({1}), Money={2}, newMoney={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Money1, money), EventLevels.Record);
            }

            //获取当前正在骑乘的坐骑的进阶养成点
            int currentHorseBlessPoint = ProcessHorse.GetCurrentHorseBlessPoint(client);
            if (currentHorseBlessPoint > 0 && blessPoint > 0)
            {
                double blessPointPercent = blessPoint / 100.0;
                blessPoint = (int)(blessPointPercent * currentHorseBlessPoint);

                //过滤养成点奖励
                blessPoint = Global.FilterValue(client, blessPoint);

                //为指定的坐骑增加养成点(临时或者永久)
                ProcessHorse.ProcessAddHorseAwardLucky(client, blessPoint, true, "五行奇阵奖励");
            }

            Global.AddWuXingAwardEvent(client, experience, blessPoint);
        }

        #endregion 五行奇阵的最终奖励
    }
}
