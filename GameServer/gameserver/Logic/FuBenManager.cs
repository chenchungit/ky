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
    /// 副本地图映射项
    /// </summary>
    public class FuBenMapItem
    {
        /// <summary>
        /// 副本ID
        /// </summary>
        public int FuBenID = 0;

        /// <summary>
        /// 地图编号
        /// </summary>
        public int MapCode = 0;

        /// <summary>
        /// 副本存在的最长时间
        /// </summary>
        public int MaxTime = -1;

        // 副本改造 [11/15/2013 LiaoWei]
        /// <summary>
        /// 可执行扫荡最低满足时间
        /// </summary>
        public int MinSaoDangTimer = -1;

        /// <summary>
        /// 铜钱奖励
        /// </summary>
        public int Money1 = 0;

        /// <summary>
        /// 经验奖励
        /// </summary>
        public int Experience = 0;

        /// <summary>
        /// 首次通关金币奖励
        /// </summary>
        public int nFirstGold = 0;

        /// <summary>
        /// 首次通关经验奖励
        /// </summary>
        public int nFirstExp = 0;

        /// <summary>
        /// 正常通关物品奖励
        /// </summary>
        public List<GoodsData> GoodsDataList = null;

        /// <summary>
        /// 首次通关物品奖励 ChenXiaojun
        /// </summary>
        public List<GoodsData> FirstGoodsDataList = null;

        /// <summary>
        /// 星魂奖励
        /// </summary>
        public int nXingHunAward = 0;

        /// <summary>
        /// 首次通关星魂奖励
        /// </summary>
        public int nFirstXingHunAward = 0;

        /// <summary>
        /// 战功奖励
        /// </summary>
        public int nZhanGongaward = 0;

        /// <summary>
        /// 粉末奖励
        /// </summary>
        public int YuanSuFenMoaward = 0;

        /// <summary>
        /// 荧光粉末奖励
        /// </summary>
        public int LightAward = 0;

        /// <summary>
        /// 狼魂粉末
        /// </summary>
        public int WolfMoney = 0;
    };

    /// <summary>
    /// 副本信息项
    /// </summary>
    public class FuBenInfoItem
    {
        /// <summary>
        /// 副本顺序ID
        /// </summary>
        public int FuBenSeqID = 0;

        /// <summary>
        /// 副本开始时间
        /// </summary>
        private long _StartTicks = 0;

        /// <summary>
        /// 副本开始时间
        /// </summary>
        public long StartTicks
        {
            get { lock (this) { return _StartTicks; } }
            set { lock (this) { _StartTicks = value; } }
        }

        /// <summary>
        /// 副本结束时间
        /// </summary>
        public long _EndTicks = 0;

        /// <summary>
        /// 副本结束时间
        /// </summary>
        public long EndTicks
        {
            get { lock (this) { return _EndTicks; } }
            set { lock (this) { _EndTicks = value; } }
        }

        /// <summary>
        /// 副本中的物品掉落绑定状态
        /// </summary>
        public int GoodsBinding = 0;

        /// <summary>
        /// 进入副本时对应的副本ID
        /// </summary>
        public int FuBenID = 0;

        // 副本改造 [11/15/2013 LiaoWei]
        /// <summary>
        /// 副本死亡次数
        /// </summary>
        public int _nDieCount = 0;

        /// <summary>
        /// 副本死亡次数
        /// </summary>
        public int nDieCount
        {
            get { lock (this) { return _nDieCount; } }
            set { lock (this) { _nDieCount = value; } }
        }

        /// <summary>
        /// 开始时的DayOfYear
        /// </summary>
        public int nDayOfYear = TimeUtil.NowDateTime().DayOfYear;

        //奖励倍数，在副本过程中有可能发生变化
        public double AwardRate = 1.0;
    };

    /// <summary>
    /// 副本管理
    /// </summary>
    public class FuBenManager
    {
        #region 副本顺序ID

        /// <summary>
        /// 角色到副本顺序ID的映射字典
        /// </summary>
        private static Dictionary<int, int> _FuBenSeqIDDict = new Dictionary<int, int>();

        /// <summary>
        /// 副本顺序ID到信息项映射字典
        /// </summary>
        private static Dictionary<int, FuBenInfoItem> _FuBenSeqID2InfoDict = new Dictionary<int, FuBenInfoItem>();

        /// <summary>
        /// 查找一个角色的副本顺序ID
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static int FindFuBenSeqIDByRoleID(int roleID)
        {
            int fuBenSeqID = 0;
            lock (_FuBenSeqIDDict)
            {
                if (_FuBenSeqIDDict.TryGetValue(roleID, out fuBenSeqID))
                {
                    return fuBenSeqID;
                }

                return 0;
            }
        }

        /// <summary>
        /// 查找一个副本顺序ID的信息项
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static FuBenInfoItem FindFuBenInfoBySeqID(int fuBenSeqID)
        {
            FuBenInfoItem fuBenInfoItem = null;
            lock (_FuBenSeqID2InfoDict)
            {
                if (!_FuBenSeqID2InfoDict.TryGetValue(fuBenSeqID, out fuBenInfoItem))
                {
                    return null;
                }

                return fuBenInfoItem;
            }
        }

        /// <summary>
        /// 添加一个角色到副本顺序ID的映射
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static void AddFuBenSeqID(int roleID, int fuBenSeqID, int goodsBinding, int fuBenID)
        {
            lock (_FuBenSeqIDDict)
            {
                _FuBenSeqIDDict[roleID] = fuBenSeqID;
            }

            lock (_FuBenSeqID2InfoDict)
            {
                if (!_FuBenSeqID2InfoDict.ContainsKey(fuBenSeqID))
                {
                    _FuBenSeqID2InfoDict[fuBenSeqID] = new FuBenInfoItem()
                    {
                        FuBenSeqID = fuBenSeqID,
                        StartTicks = TimeUtil.NOW(),
                        EndTicks = 0,
                        GoodsBinding = goodsBinding,
                        FuBenID = fuBenID,
                    };
                }
            }
        }

        /// <summary>
        /// 删除一个角色到副本顺序ID的映射
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static void RemoveFuBenSeqID(int roleID)
        {
            int fuBenSeqID = -1;
            lock (_FuBenSeqIDDict)
            {
                if (_FuBenSeqIDDict.TryGetValue(roleID, out fuBenSeqID))
                {
                    _FuBenSeqIDDict.Remove(roleID);
                }
                else
                {
                    fuBenSeqID = -1;
                }
            }
/*
            if (fuBenSeqID != -1)
            {
                lock (_FuBenSeqID2InfoDict)
                {
                    _FuBenSeqID2InfoDict.Remove(fuBenSeqID);
                }
            }
 * */
        }

        /// <summary>
        /// 根据副本序列ID移除缓存的副本信息
        /// </summary>
        /// <param name="fuBenSeqID"></param>
        public static void RemoveFuBenInfoBySeqID(int fuBenSeqID)
        {
            if (fuBenSeqID != -1)
            {
                lock (_FuBenSeqID2InfoDict)
                {
                    _FuBenSeqID2InfoDict.Remove(fuBenSeqID);
                }
            }
        }

        /// <summary>
        /// 获取新的副本序列ID
        /// </summary>
        /// <returns></returns>
        public static int GetFuBenSeqId(int tag = 0)
        {
            int nSeqID;
            //从DBServer获取副本顺序ID
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GETFUBENSEQID, string.Format("{0}", tag), GameManager.LocalServerId);
            if (null != dbFields && dbFields.Length >= 2)
            {
                nSeqID = Global.SafeConvertToInt32(dbFields[1]);
                if (nSeqID > 0)
                {
                    return nSeqID;
                }
            }

            return 0;
        }

        #endregion 副本顺序ID

        #region 副本和地图映射

        /// <summary>
        /// 副本ID到副本地图映射项的字典
        /// </summary>
        private static Dictionary<string, FuBenMapItem> _FuBenMapCode2MapItemDict = new Dictionary<string, FuBenMapItem>();

        /// <summary>
        /// 副本ID到副本地图编号列表的字典
        /// </summary>
        private static Dictionary<int, List<int>> _FuBen2MapCodeListDict = new Dictionary<int, List<int>>();

        /// <summary>
        /// 地图编号到副本ID的字典
        /// </summary>
        private static Dictionary<int, int> _MapCode2FuBenDict = new Dictionary<int, int>();

        /// <summary>
        /// 根据副本ID获取副本地图映射项
        /// </summary>
        /// <param name="fuBenID"></param>
        /// <returns></returns>
        public static FuBenMapItem FindMapCodeByFuBenID(int fuBenID, int mapCode)
        {
            FuBenMapItem fuBenMapItem = null;
            string key = string.Format("{0}_{1}", fuBenID, mapCode);
            if (!_FuBenMapCode2MapItemDict.TryGetValue(key, out fuBenMapItem))
            {
                return null;
            }

            return fuBenMapItem;
        }

        public static List<FuBenMapItem> GetAllFubenMapItem()
        {
            List<FuBenMapItem> list = new List<FuBenMapItem>();
            lock (_FuBenMapCode2MapItemDict)
            {
                list.AddRange(_FuBenMapCode2MapItemDict.Values);
            }
            return list;
        }

        /// <summary>
        /// 根据副本ID获取副本地图列表
        /// </summary>
        /// <param name="fuBenID"></param>
        /// <returns></returns>
        public static List<int> FindMapCodeListByFuBenID(int fuBenID)
        {
            List<int> mapCodeList = null;
            if (!_FuBen2MapCodeListDict.TryGetValue(fuBenID, out mapCodeList))
            {
                return null;
            }

            return mapCodeList;
        }

        /// <summary>
        /// 根据地图编号获取副本ID
        /// </summary>
        /// <param name="fuBenID"></param>
        /// <returns></returns>
        public static int FindFuBenIDByMapCode(int mapCode)
        {
            int fuBenID = -1;
            if (!_MapCode2FuBenDict.TryGetValue(mapCode, out fuBenID))
            {
                return -1;
            }

            return fuBenID;
        }

        public static bool IsFuBenMap(int mapCode)
        {
            bool isFuBenMap = FuBenManager.FindFuBenIDByMapCode(mapCode) > 0 ? true : false; ;
            if (Global.GetMapType(mapCode) == MapTypes.HuanYingSiYuan)
            {
                isFuBenMap = true;
            }

            return isFuBenMap;
        }

        /// <summary>
        /// 根据副本ID和地图编号, 获取下一层地图编号
        /// </summary>
        /// <param name="fuBenID"></param>
        /// <returns></returns>
        public static int FindNextMapCodeByFuBenID(int mapCode)
        {
            int fuBenID = FuBenManager.FindFuBenIDByMapCode(mapCode);
            if (fuBenID <= 0)
            {
                return -1;
            }

            //根据副本ID获取副本地图列表
            List<int> mapCodeList = FindMapCodeListByFuBenID(fuBenID);
            if (null == mapCodeList)
            {
                return -1;
            }

            int findIndex = mapCodeList.IndexOf(mapCode);
            if (-1 == findIndex)
            {
                return -1;
            }

            if (findIndex >= mapCodeList.Count - 1)
            {
                return -1;
            }

            return mapCodeList[findIndex + 1];
        }

        /// <summary>
        /// 根据副本ID和地图编号, 当前所在的层数
        /// </summary>
        /// <param name="fuBenID"></param>
        /// <returns></returns>
        public static int FindMapCodeIndexByFuBenID(int mapCode)
        {
            int fuBenID = FuBenManager.FindFuBenIDByMapCode(mapCode);
            if (fuBenID <= 0)
            {
                return 0;
            }

            //根据副本ID获取副本地图列表
            List<int> mapCodeList = FindMapCodeListByFuBenID(fuBenID);
            if (null == mapCodeList)
            {
                return 0;
            }

            int findIndex = mapCodeList.IndexOf(mapCode);
            if (-1 == findIndex)
            {
                return 0;
            }

            return (findIndex + 1);
        }

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
                if (fields[i] == "1" || fields[i] == "")
                {
                    continue;
                }
                else if (sa.Length != 7)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("解析FuBenMap.xml文件中的奖励项时失败, 物品配置项个数错误"));
                    continue;
                }
                
                int[] goodsFields = Global.StringArray2IntArray(sa);
                                                    
                //获取物品数据            // 物品通用奖励改造 [1/8/2014 LiaoWei]
                GoodsData goodsData = Global.GetNewGoodsData(goodsFields[0], goodsFields[1], 0, goodsFields[3], goodsFields[2], 0, goodsFields[5], 0, goodsFields[6], goodsFields[4]);
                goodsDataList.Add(goodsData);
            }

            return goodsDataList;
        }

        /// <summary>
        /// 解析Xml项
        /// </summary>
        /// <param name="systemXmlItem"></param>
        private static void ParseXmlItem(SystemXmlItem systemXmlItem)
        {
            int mapCode = systemXmlItem.GetIntValue("MapCode");
            int fuBenID = systemXmlItem.GetIntValue("CopyID");
            int maxTime = systemXmlItem.GetIntValue("MaxTime");
            int money1 = systemXmlItem.GetIntValue("Moneyaward");
            int experience = systemXmlItem.GetIntValue("Experienceaward");
            int nTmpFirstGold = systemXmlItem.GetIntValue("FirstGold");
            int nTmpFirstExp = systemXmlItem.GetIntValue("FirstExp");
            int nMinSaoDangTimer = systemXmlItem.GetIntValue("MinSaoDangTime");
            int nTmpXingHunAward = systemXmlItem.GetIntValue("XingHunaward");
            int nTmpFirstXingHunAward = systemXmlItem.GetIntValue("FirstXingHun");
            int nTmpZhanGongaward = systemXmlItem.GetIntValue("ZhanGongaward");
            int YuanSuFenMoaward = systemXmlItem.GetIntValue("YuanSuFenMoaward");
            int lightAward = systemXmlItem.GetIntValue("YingGuangaward");
            List<GoodsData> goodsDataList = null;
            string goodsIDs = systemXmlItem.GetStringValue("GoodsIDs");
            if (!string.IsNullOrEmpty(goodsIDs))
            {
                string[] fields = goodsIDs.Split('|');
                if (fields.Length > 0)
                {
                    goodsDataList = ParseGoodsDataList(fields);
                }
                //else
                //{
                //    LogManager.WriteLog(LogTypes.Error, string.Format("解析副本地图映射配置项中的物品奖励失败, FuBenID={0}, MapCode={1}", fuBenID, mapCode));
                //}
            }
            //else
            //{
            //    LogManager.WriteLog(LogTypes.Error, string.Format("解析副本地图映射配置项中的物品奖励失败, FuBenID={0}, MapCode={1}", fuBenID, mapCode));
            //}

            // 首次通关奖励解析 ChenXiaojun
            List<GoodsData> goodsFirstDataList = null;
            string goodsFirstIDs = systemXmlItem.GetStringValue("FirstGoodsID");
            if (!string.IsNullOrEmpty(goodsFirstIDs))
            {
                string[] fields = goodsFirstIDs.Split('|');
                if (fields.Length > 0)
                {
                    goodsFirstDataList = ParseGoodsDataList(fields);
                }
            }
            

            FuBenMapItem fuBenMapItem = new FuBenMapItem()
            {
                FuBenID = fuBenID,
                MapCode = mapCode,
                MaxTime = maxTime,
                Money1 = money1,
                Experience = experience,
                GoodsDataList = goodsDataList,
                FirstGoodsDataList = goodsFirstDataList,
                MinSaoDangTimer = nMinSaoDangTimer,
                nFirstExp = nTmpFirstExp,
                nFirstGold = nTmpFirstGold,
                nXingHunAward = nTmpXingHunAward,
                nFirstXingHunAward = nTmpFirstXingHunAward,
                nZhanGongaward = nTmpZhanGongaward,
                YuanSuFenMoaward = YuanSuFenMoaward,
                LightAward = lightAward,
            };

            string key = string.Format("{0}_{1}", fuBenID, mapCode);
            lock (_FuBenMapCode2MapItemDict)
            {
                _FuBenMapCode2MapItemDict[key] = fuBenMapItem;
            }

            List<int> mapCodeList = null;
            if (!_FuBen2MapCodeListDict.TryGetValue(fuBenID, out mapCodeList))
            {
                mapCodeList = new List<int>();
                _FuBen2MapCodeListDict[fuBenID] = mapCodeList;
            }

            mapCodeList.Add(mapCode);

            _MapCode2FuBenDict[mapCode] = fuBenID;
        }

        /// <summary>
        /// 从文件中加载副本到地图编号的映射
        /// </summary>
        public static void LoadFuBenMap()
        {
            XElement xml = null;
            string fileName = "Config/FuBenMap.xml";

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

                //解析Xml项
                ParseXmlItem(systemXmlItem);
            }
        }

        #endregion 副本和地图映射

        #region 副本奖励

        /// <summary>
        /// 判断副本地图中掉落的物品是否自动飞入背包
        /// </summary>
        /// <returns></returns>
        public static bool CanFuBenMapFallGoodsAutoGet(GameClient client)
        {
            int fuBenSeqID = client.ClientData.FuBenSeqID;

            //查找一个副本顺序ID的信息项
            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(fuBenSeqID);
            if (null == fuBenInfoItem)
            {
                return false;
            }
            
            //获取副本的数据
            SystemXmlItem systemFuBenItem = null;
            if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(fuBenInfoItem.FuBenID, out systemFuBenItem))
            {
                return false;
            }

            int copyType = systemFuBenItem.GetIntValue("CopyType");
            return (0 == copyType);
        }

        /// <summary>
        /// 判断副本地图中奖励物品的绑定状态
        /// </summary>
        /// <returns></returns>
        public static int GetFuBenMapAwardsGoodsBinding(GameClient client)
        {
            int fuBenSeqID = client.ClientData.FuBenSeqID;
            return GetFuBenMapAwardsGoodsBinding(fuBenSeqID);
        }

        /// <summary>
        /// 判断副本地图中奖励物品的绑定状态
        /// </summary>
        /// <returns></returns>
        public static int GetFuBenMapAwardsGoodsBinding(int fuBenSeqID)
        {
            //查找一个副本顺序ID的信息项
            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(fuBenSeqID);
            if (null == fuBenInfoItem)
            {
                return 0;
            }

            return fuBenInfoItem.GoodsBinding;
        }

        /// <summary>
        /// 判断副本地图中是否有奖励
        /// </summary>
        /// <returns></returns>
        public static bool CanGetFuBenMapAwards(GameClient client)
        {
            int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
            if (fuBenID <= 0)
            {
                return false;
            }

            if (client.ClientData.FuBenSeqID <= 0)
            {
                return false;
            }

            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(client.ClientData.FuBenSeqID);
            if (null == fuBenInfoItem)
            {
                return false;
            }

            if (fuBenID != fuBenInfoItem.FuBenID)
            {
                return false;
            }

            FuBenMapItem fuBenMapItem = FindMapCodeByFuBenID(fuBenID, client.ClientData.MapCode);
            if (null == fuBenMapItem)
            {
                return false;
            }

            if (null == fuBenMapItem.GoodsDataList || fuBenMapItem.GoodsDataList.Count <= 0)
            {
                if (fuBenMapItem.Experience <= 0)
                {
                    if (fuBenMapItem.Money1 <= 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 判断是否能自动领取副本地图中中的奖励
        /// </summary>
        /// <returns></returns>
        public static bool CanAutoGetFuBenMapAwards(GameClient client)
        {
            int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
            if (fuBenID <= 0)
            {
                return false;
            }

            if (client.ClientData.FuBenSeqID <= 0)
            {
                return false;
            }

            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(client.ClientData.FuBenSeqID);
            if (null == fuBenInfoItem)
            {
                return false;
            }

            if (fuBenID != fuBenInfoItem.FuBenID)
            {
                return false;
            }

            FuBenMapItem fuBenMapItem = FindMapCodeByFuBenID(fuBenID, client.ClientData.MapCode);
            if (null == fuBenMapItem)
            {
                return false;
            }

            if (null == fuBenMapItem.GoodsDataList || fuBenMapItem.GoodsDataList.Count <= 0)
            {
                if (fuBenMapItem.Experience <= 0)
                {
                    if (fuBenMapItem.Money1 <= 0)
                    {
                        return false;
                    }
                }
            }

            if (null == fuBenMapItem.GoodsDataList || fuBenMapItem.GoodsDataList.Count <= 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 处理用户的获取副本奖励
        /// </summary>
        /// <param name="client"></param>
        public static bool ProcessFuBenMapAwards(GameClient client, bool notifyClient = false)
        {
            if (client.ClientData.FuBenSeqID < 0)
            {
                GameManager.ClientMgr.NotifyImportantMsg(
                    Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("您当前的副本顺序ID错误")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return false;
            }

            //如果已经获取过一次奖励，则不再提示奖励
            //查找角色的ID+副本顺序ID对应地图编号的奖励领取状态
            int awardState = GameManager.CopyMapMgr.FindAwardState(client.ClientData.RoleID, client.ClientData.FuBenSeqID, client.ClientData.MapCode);
            if (awardState > 0)
            {
                GameManager.ClientMgr.NotifyImportantMsg(
                    Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("当前副本地图的奖励只能领取一次")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return false;
            }

            int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
            if (fuBenID <= 0)
            {
                GameManager.ClientMgr.NotifyImportantMsg(
                    Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("没有找到当前地图对应的副本配置")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return false;
            }

            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(client.ClientData.FuBenSeqID);
            if (null == fuBenInfoItem)
            {
                GameManager.ClientMgr.NotifyImportantMsg(
                    Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("没有找到对应的内存副本信息")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return false;
            }

            if (fuBenID != fuBenInfoItem.FuBenID)
            {
                GameManager.ClientMgr.NotifyImportantMsg(
                    Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("副本ID错误，无法领取其他副本的奖励")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return false;
            }

            FuBenMapItem fuBenMapItem = FindMapCodeByFuBenID(fuBenID, client.ClientData.MapCode);
            if (null == fuBenMapItem)
            {
                GameManager.ClientMgr.NotifyImportantMsg(
                    Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("没有找到当前副本地图的奖励")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return false;
            }

            CopyMap copyMap = null;
            copyMap = GameManager.CopyMapMgr.FindCopyMap(client.ClientData.MapCode);
            if (copyMap == null)
            {
                GameManager.ClientMgr.NotifyImportantMsg(
                    Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("领取副本奖励出错")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return false;
            }

            GameManager.CopyMapMgr.AddAwardState(client.ClientData.RoleID, client.ClientData.FuBenSeqID, client.ClientData.MapCode, 1);

            FuBenTongGuanData fubenTongGuanData = null;

            int nMaxTime = fuBenMapItem.MaxTime * 60; //分->秒
            long startTicks = fuBenInfoItem.StartTicks;
            long endTicks = fuBenInfoItem.EndTicks;
            int nFinishTimer = (int)(endTicks - startTicks) / 1000;//毫秒->秒
            int killedNum = 0;// copyMap.KilledNormalNum + copyMap.KilledBossNum;
            int nDieCount = fuBenInfoItem.nDieCount;

            //向客户的发放通关奖励
            fubenTongGuanData = Global.GiveCopyMapGiftForScore(client, fuBenID, client.ClientData.MapCode, nMaxTime, nFinishTimer, killedNum, nDieCount, (int)(fuBenMapItem.Experience * fuBenInfoItem.AwardRate), (int)(fuBenMapItem.Money1 * fuBenInfoItem.AwardRate), fuBenMapItem);

            if (fubenTongGuanData != null)
            {
                //发送奖励到客户端
                TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<FuBenTongGuanData>(fubenTongGuanData, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_SPR_FUBENPASSNOTIFY);

                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                {
                    //如果发送失败
                }
            }

            /*if (null != fuBenMapItem.GoodsDataList)
            {
                //判断背包是否空间足够
                if (!Global.CanAddGoodsDataList(client, fuBenMapItem.GoodsDataList))
                {
                    GameManager.ClientMgr.NotifyImportantMsg(
                        Global._TCPManager.MySocketListener,
                        Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("背包中空格不足，请清理出足够的空格后，再获取副本地图的奖励")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoBagGrid);
                    return false;
                }
            }

            //添加角色的ID+副本顺序ID对应地图编号的奖励领取状态
            GameManager.CopyMapMgr.AddAwardState(client.ClientData.RoleID, client.ClientData.FuBenSeqID, client.ClientData.MapCode, 1);

            if (Global.FilterFallGoods(client)) //是否奖励物品
            {
                //奖励用户物品
                if (null != fuBenMapItem.GoodsDataList)
                {
                    for (int i = 0; i < fuBenMapItem.GoodsDataList.Count; i++)
                    {
                        //想DBServer请求加入某个新的物品到背包中
                        //添加物品
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool,
                            client, fuBenMapItem.GoodsDataList[i].GoodsID, fuBenMapItem.GoodsDataList[i].GCount, fuBenMapItem.GoodsDataList[i].Quality, "", fuBenMapItem.GoodsDataList[i].Forge_level, fuBenMapItem.GoodsDataList[i].Binding, 0, "", true, 1, "副本奖励物品", Global.ConstGoodsEndTime, fuBenMapItem.GoodsDataList[i].AddPropIndex, fuBenMapItem.GoodsDataList[i].BornIndex, fuBenMapItem.GoodsDataList[i].Lucky, fuBenMapItem.GoodsDataList[i].Strong);
                    }
                }
            }

            //奖励用户经验
            //异步写数据库，写入经验和级别
            int experience = fuBenMapItem.Experience;

            //处理角色经验
            GameManager.ClientMgr.ProcessRoleExperience(client, experience, true, false);

            //通知客户端
            if (notifyClient)
            {
                string msgText = string.Format(Global.GetLang("恭喜您通关本副本后，获得了{0}点经验奖励"), experience);
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
            }

            //判断如果是最后一层，则不显示
            int toNextMapCode = FuBenManager.FindNextMapCodeByFuBenID(client.ClientData.MapCode);
            if (-1 == toNextMapCode) //最后一层？
            {
                //副本通关获取经验通知
                Global.BroadcastFuBenExperience(client, fuBenID, experience);
            }

            //奖励用户金钱
            //异步写数据库，写入金钱
            int money = fuBenMapItem.Money1;
            if (-1 != money)
            {
                //过滤金币奖励
                money = Global.FilterValue(client, money);

                //更新用户的铜钱
                GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, money, false);

                //GameManager.SystemServerEvents.AddEvent(string.Format("角色获取金钱, roleID={0}({1}), Money={2}, newMoney={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Money1, money), EventLevels.Record);
            }*/

            Global.AddFuBenAwardEvent(client, fuBenID);

            return true;
        }

        #endregion 副本奖励
    }
}
