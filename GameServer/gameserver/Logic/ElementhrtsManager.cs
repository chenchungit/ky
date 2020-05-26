using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using Server.Data;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using GameServer.Server;
using GameServer.Logic;

namespace GameServer.Logic
{
    enum ElementhrtsError
    {
        Success = 0,        // 成功
        ItemNotExist,       // 道具不存在
        AlreadyOut,         // 已经卸下
        AlreadyUse,         // 已经穿上
        BagIsFull,          // 背包已满
        EquipIsFull,        // 装备栏已满
        ErrorTimes,         // 猎取时错误的次数
        BagNotEnough,       // 背包格子不足
        ErrorConfig,        // 配置错误
        ErrorParams,        // 传来的参数错误
        ErrorLevel,         // 等级或转生不满足条件
        PowderNotEnough,    // 粉末不足
        MoneyNotEnough,     // 钻石不足
        CantEquip,          // 无法装备
        SameCategoriy,      // 已装备相同类型的
        GoodsNotExist,      // Goods.xml里不存在GoodsID
        DBSERVERERROR,      // 与dbserver通信失败
    }

    class ElementhrtsManager
    {

        /// <summary>
        /// 元素背包的最大格子数
        /// </summary>
        public static int MaxElementhrtsGridNum = 100;

        /// <summary>
        /// 元素装备栏最大格子数
        /// </summary>
        public static int MaxUsingElementhrtsGridNum = 8;

        /// <summary>
        /// 钻石档
        /// </summary>
        public static int ZhuanShiGrade = 6; 


        #region 元素之心相关配置文件

        /// <summary>
        /// 获取元素之心的档次相关配置
        /// </summary>
        public class RefineType
        {
            public int Grade;          // 档次编号
            public int MinZhuanSheng;  // 最小转生
            public int MinLevel;       // 最小等级
            public int MaxZhuanSheng;  // 最大转生
            public int MaxLevel;       // 最大等级
            public int RefineCost;     // 提炼所需元素粉末
            public int ZuanShiCost;    // 提炼所需钻石
            public double SuccessRate;    // 成功升级几率
            public int RefineLevel;    // 成功后档次提升到~~~
        }

        private static Dictionary<int, RefineType> RefineTypeDict = new Dictionary<int, RefineType>();

        /// <summary>
        /// 根据档次取得档次的配置
        /// </summary>
        public static RefineType GetRefineType(int Grade)
        {
            RefineType config = null;

            lock (RefineTypeDict)
            {
                if (RefineTypeDict.ContainsKey(Grade))
                    config = RefineTypeDict[Grade];
            }

            return config;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void LoadRefineType()
        {
            string fileName = "Config/RefineType.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/RefineType.xml时出错!!!文件不存在");
                return;
            }

            try
            {
                lock (RefineTypeDict)
                {
                    RefineTypeDict.Clear();

                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null == xmlItem)
                            continue;

                        RefineType config = new RefineType();
                        config.Grade = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                        config.MinZhuanSheng = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MinZhuanSheng", "0"));
                        config.MinLevel = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MinLevel", "0"));
                        config.MaxZhuanSheng = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MaxZhuanSheng", "0"));
                        config.MaxLevel = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MaxLevel", "0"));
                        config.RefineCost = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "RefineCost", "0"));
                        config.ZuanShiCost = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ZuanShiCost", "0"));
                        config.SuccessRate = Global.GetSafeAttributeDouble(xmlItem, "SuccessRate");
                        config.RefineLevel = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "RefineLevel", "0"));
                        RefineTypeDict[config.Grade] = config;
                    }
                }
            }

            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/RefineType.xml时文件出错", ex);
            }
        }

        /// <summary>
        /// 获取元素的基本信息
        /// </summary>
        public class ElementHrtsBase
        {
            public int ID;                 // 流水号
            public int GoodsID;            // 物品ID
            // 对元素之心没啥用
            /*public int Num;                // 数量
            public int QiangHuaFallID;     // 强化掉落ID
            public int ZhuiJiaFallID;      // 追加掉落ID
            public int LckyProbability;    // 幸运掉落概率
            public int ZhuoYueFallID;      // 卓越掉落ID
            public int MinMoney;           // 最小金币
            public int MaxMoney;           // 最大金币
            public int MinBindYuanBao;     // 最少绑定元宝
            public int MaxBindYuanBao;     // 最大绑定元宝
            public int MinExp;             // 最少经验
            public int MaxExp;             // 最大经验*/
            public int StartValues;        // 起始值
            public int EndValues;          // 结束值
        }

        private static Dictionary<int, List<ElementHrtsBase> > ElementHrtsBaseDict = new Dictionary<int, List<ElementHrtsBase> >();

        /// <summary>
        /// 根据档次取得能够获得的元素之心的列表
        /// </summary>
        public static List<ElementHrtsBase> GetElementHrtsBase(int Grade)
        {
            List<ElementHrtsBase> cfgList = null;

            lock (ElementHrtsBaseDict)
            {
                if (ElementHrtsBaseDict.ContainsKey(Grade))
                    cfgList = ElementHrtsBaseDict[Grade];
            }

            return cfgList;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void LoadElementHrtsBase()
        {
            string fileName = "Config/Refine.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/RefineType.xml时出错!!!文件不存在");
                return;
            }

            try
            { 
                lock (ElementHrtsBaseDict)
                {
                    ElementHrtsBaseDict.Clear();

                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null == xmlItem)
                            continue;

                        int Grade = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "TypeID", "0"));
                        List<ElementHrtsBase> baseList = new List<ElementHrtsBase>();

                        IEnumerable<XElement> args = xmlItem.Elements();
                        foreach (var arg in args)
                        { 
                            ElementHrtsBase config = new ElementHrtsBase();
                            config.ID = Convert.ToInt32(Global.GetDefAttributeStr(arg, "ID", "0"));
                            config.GoodsID = Convert.ToInt32(Global.GetDefAttributeStr(arg, "GoodsID", "0"));
                            config.StartValues = Convert.ToInt32(Global.GetDefAttributeStr(arg, "StartValues", "0"));
                            config.EndValues = Convert.ToInt32(Global.GetDefAttributeStr(arg, "EndValues", "0"));
                            baseList.Add(config);
                        }

                        ElementHrtsBaseDict[Grade] = baseList;
                    }
                }
            }

            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/RefineType.xml时出现异常!!!");
            }
        }

        public class ElementHrtsLevelInfo
        {
            public int Level;      // 等级
            public int NeedExp;    // 升级所需经验
            public int TotalExp;   // 所含基础经验
        }

        // key = 档次,等级
        private static Dictionary<string, ElementHrtsLevelInfo> ElementHrtsLevelDict = new Dictionary<string, ElementHrtsLevelInfo>();

        /// <summary>
        /// 根据阶段和等级取得元素之心的信息
        /// </summary>
        public static ElementHrtsLevelInfo GetElementHrtsLevelInfo(int grade, int level)
        {
            string key = grade.ToString() + "|" + level.ToString();

            ElementHrtsLevelInfo config = null;

            lock (ElementHrtsLevelDict)
            {
                if (ElementHrtsLevelDict.ContainsKey(key))
                    config = ElementHrtsLevelDict[key];
            }

            return config;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void LoadElementHrtsLevelInfo()
        {
            string fileName = "Config/ElementsHeart.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ElementsHeart.xml时出错!!!文件不存在");
                return;
            }

            try
            { 
                lock (ElementHrtsLevelDict)
                {
                    ElementHrtsLevelDict.Clear();

                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null == xmlItem)
                            continue;

                        int ID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                        int Level = 0;
                        int TotalExp = 0;

                        IEnumerable<XElement> args = xmlItem.Elements();
                        foreach (var arg in args)
                        {
                            ElementHrtsLevelInfo config = new ElementHrtsLevelInfo();
                            config.Level = Convert.ToInt32(Global.GetDefAttributeStr(arg, "ID", "0"));

                            if (Level + 1 != config.Level)
                            {
                                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载Config/ElementsHeart.xml时出错!!!，{0}, {1}", ID, Level));
                                return;
                            }

                            Level = config.Level;

                            config.NeedExp = Convert.ToInt32(Global.GetDefAttributeStr(arg, "NeedExp", "0"));
                            TotalExp += config.NeedExp;
                            config.TotalExp = TotalExp;

                            string key = ID.ToString() + "|" + Level.ToString();
                            ElementHrtsLevelDict[key] = config;
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ElementsHeart.xml时出现异常!!!", ex);
            }
        }

        private static Dictionary<int, int> SpecialExpDict = new Dictionary<int, int>();

        public static int GetSpecialElementHrtsExp(int GoodsID)
        {
            int Exp = 0;
            lock (SpecialExpDict)
            {
                if (SpecialExpDict.ContainsKey(GoodsID))
                    Exp = SpecialExpDict[GoodsID];
            }
            return Exp;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void LoadSpecialElementHrtsExp()
        {
            // Value="100001,300|100002,100|100003,500|100004,1000"
            lock (SpecialExpDict)
            {
                SpecialExpDict.Clear();
                string strParam = GameManager.systemParamsList.GetParamValueByName("SpecialElementsHeart");
                if (null == strParam)
                {
                    SysConOut.WriteLine("SpecialElementsHeart 不存在，加载失败");
                    return ;
                }
                string[] fields = strParam.Split('|');
                for (int i = 0; i < fields.Length; i++)
                {
                    string[] str = fields[i].Split(',');
                    if (2 != str.Length)
                    {
                        SysConOut.WriteLine("加载SpecialElementsHeart时出现异常!!!");
                    }
                    int GoodsID = Convert.ToInt32(str[0]);
                    int Exp = Convert.ToInt32(str[1]);
                    SpecialExpDict[GoodsID] = Exp;
                }
            }
        }

        public static bool IsElementHrt(int categoriy)
        {
            return categoriy >= (int)ItemCategories.ElementHrtBegin && categoriy < (int)ItemCategories.ElementHrtEnd;
        }

        #endregion

        #region 元素之心管理

        /// <summary>
        /// 根据物品的Site和DbID获取元素之心物品的信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GoodsData GetElementhrtsByDbID(GameClient client, int Site, int id)
        {
            List<GoodsData> goodsList = null;
            if ((int)SaleGoodsConsts.ElementhrtsGoodsID == Site)
            {
                goodsList = client.ClientData.ElementhrtsList;
            }
            else if ((int)SaleGoodsConsts.UsingElementhrtsGoodsID == Site)
            {
                goodsList = client.ClientData.UsingElementhrtsList;
            }

            if (null == goodsList)
            {
                return null;
            }

            for (int i = 0; i < goodsList.Count; i++)
            {
                if (goodsList[i].Id == id)
                {
                    return goodsList[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 根据物品的DbID获取元素之心物品的信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GoodsData GetElementhrtsByDbID(GameClient client, int id)
        {
            if (null != client.ClientData.ElementhrtsList)
            {
                for (int i = 0; i < client.ClientData.ElementhrtsList.Count; i++)
                {
                    if (client.ClientData.ElementhrtsList[i].Id == id)
                    {
                        return client.ClientData.ElementhrtsList[i];
                    }
                }
            }

            if (null != client.ClientData.UsingElementhrtsList)
            {
                for (int i = 0; i < client.ClientData.UsingElementhrtsList.Count; i++)
                {
                    if (client.ClientData.UsingElementhrtsList[i].Id == id)
                    {
                        return client.ClientData.UsingElementhrtsList[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 添加元素之心到背包
        /// </summary>
        /// <param name="goodsData"></param>
        public static void AddElementhrtsData(GameClient client, GoodsData goodsData)
        {
            if (goodsData.Site != (int)SaleGoodsConsts.ElementhrtsGoodsID) return;

            if (null == client.ClientData.ElementhrtsList)
            {
                client.ClientData.ElementhrtsList = new List<GoodsData>();
            }

            lock (client.ClientData.ElementhrtsList)
            {
                client.ClientData.ElementhrtsList.Add(goodsData);
            }
        }

        /// <summary>
        /// 添加元素之心到装备栏
        /// </summary>
        /// <param name="goodsData"></param>
        public static void AddUsingElementhrtsData(GameClient client, GoodsData goodsData)
        {
            if (goodsData.Site != (int)SaleGoodsConsts.UsingElementhrtsGoodsID) return;

            if (null == client.ClientData.UsingElementhrtsList)
            {
                client.ClientData.UsingElementhrtsList = new List<GoodsData>();
            }

            lock (client.ClientData.UsingElementhrtsList)
            {
                client.ClientData.UsingElementhrtsList.Add(goodsData);
            }
        }

        /// <summary>
        /// 添加物品到元素之心队列中
        /// </summary>
        /// <param name="client"></param>
        public static GoodsData AddElementhrtsData(GameClient client, int id, int goodsID, int forgeLevel, int quality, int goodsNum, int binding, int site, string jewelList, int idelBagIndex, string endTime, int addPropIndex, int bornIndex, int lucky, int strong, int ExcellenceProperty, int nAppendPropLev, int nEquipChangeLife)
        {
            GoodsData gd = new GoodsData()
            {
                Id = id,
                GoodsID = goodsID,
                Using = 0,
                Forge_level = forgeLevel,
                Starttime = "1900-01-01 12:00:00",
                Endtime = endTime,
                Site = site,
                Quality = quality,
                Props = "",
                GCount = goodsNum,
                Binding = binding,
                Jewellist = jewelList,
                BagIndex = idelBagIndex,
                AddPropIndex = addPropIndex,
                BornIndex = bornIndex,
                Lucky = lucky,
                Strong = strong,
                ExcellenceInfo = ExcellenceProperty,
                AppendPropLev = nAppendPropLev,
                ChangeLifeLevForEquip = nEquipChangeLife,

            };

            //Global.AddJinDanGoodsData(client, gd);
            if ((int)SaleGoodsConsts.ElementhrtsGoodsID == gd.Site)
                AddElementhrtsData(client, gd);
            if ((int)SaleGoodsConsts.UsingElementhrtsGoodsID == gd.Site)
                AddUsingElementhrtsData(client, gd);
            return gd;
        }

        /// <summary>
        /// 删除元素物品
        /// </summary>
        public static void RemoveElementhrtsData(GameClient client, GoodsData goodsData)
        {
            if ((int)SaleGoodsConsts.ElementhrtsGoodsID == goodsData.Site)
            {
                lock (client.ClientData.ElementhrtsList)
                { 
                    if (null != client.ClientData.ElementhrtsList)
                    {
                        client.ClientData.ElementhrtsList.Remove(goodsData);
                    }               
                }

            }

            if ((int)SaleGoodsConsts.UsingElementhrtsGoodsID == goodsData.Site)
            {
                lock (client.ClientData.UsingElementhrtsList)
                {
                    if (null != client.ClientData.UsingElementhrtsList)
                    {
                        client.ClientData.UsingElementhrtsList.Remove(goodsData);
                    }
                }
            }
        }

        /// <summary>
        /// 返回包裹中的空闲位置 找不到返回-1
        /// </summary>
        public static int GetIdleSlotOfBag(GameClient client)
        {
            int idelPos = -1;

            if (null == client.ClientData.ElementhrtsList) 
                return 0;

            List<int> usedBagIndex = new List<int>();

            for (int i = 0; i < client.ClientData.ElementhrtsList.Count; i++)
            {
                //if (client.ClientData.ElementhrtsList[i].Site == (int)SaleGoodsConsts.ElementhrtsGoodsID && client.ClientData.ElementhrtsList[i].Using <= 0)
                {
                    if (usedBagIndex.IndexOf(client.ClientData.ElementhrtsList[i].BagIndex) < 0)
                    {
                        usedBagIndex.Add(client.ClientData.ElementhrtsList[i].BagIndex);
                    }
                }
            }

            for (int n = 0; n < GetMaxElementhrtsCount(); n++)
            {
                if (usedBagIndex.IndexOf(n) < 0)
                {
                    //idelPos = n;
                    //break;
                    return n;
                }
            }

            return idelPos;
        }

        /// <summary>
        /// 返回装备栏中的空闲位置 找不到返回-1
        /// </summary>
        public static int GetIdleSlotOfUsing(GameClient client)
        {
            int idelPos = -1;

            if (null == client.ClientData.UsingElementhrtsList)
                return 0;

            List<int> usedBagIndex = new List<int>();

            for (int i = 0; i < client.ClientData.UsingElementhrtsList.Count; i++)
            {
                //if (client.ClientData.UsingElementhrtsList[i].Site == (int)SaleGoodsConsts.UsingElementhrtsGoodsID && client.ClientData.UsingElementhrtsList[i].Using <= 0)
                {
                    if (usedBagIndex.IndexOf(client.ClientData.UsingElementhrtsList[i].BagIndex) < 0)
                    {
                        usedBagIndex.Add(client.ClientData.UsingElementhrtsList[i].BagIndex);
                    }
                }
            }

            for (int n = 0; n < GetMaxUsingElementhrtsCount(); n++)
            {
                if (usedBagIndex.IndexOf(n) < 0)
                {
                    //idelPos = n;
                    //break;
                    return n;
                }
            }

            return idelPos;
        }


        /// <summary>
        /// 取得元素背包道具数量 用来判断元素之心背包是否已经满
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetElementhrtsListCount(GameClient client)
        {
            if (null == client.ClientData.ElementhrtsList)
            {
                return 0;
            }

            return client.ClientData.ElementhrtsList.Count;
        }

        /// <summary>
        /// 取得元素装备栏道具数量 用来判断元素之心装备栏是否已经满
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetUsingElementhrtsListCount(GameClient client)
        {
            if (null == client.ClientData.UsingElementhrtsList)
            {
                return 0;
            }

            return client.ClientData.UsingElementhrtsList.Count;
        }

        /// <summary>
        ///  获取元素背包的最大容量
        /// </summary>
        /// <returns></returns>
        public static int GetMaxElementhrtsCount()
        {
            return MaxElementhrtsGridNum;
        }

        /// <summary>
        ///  获取元素容器的最大容量
        /// </summary>
        /// <returns></returns>
        public static int GetMaxUsingElementhrtsCount()
        {
            return MaxUsingElementhrtsGridNum;
        }

        /// <summary>
        /// 整理背包
        /// </summary>
        public static void SortElementhrtsList(GameClient client)
        {
            if (null == client.ClientData.ElementhrtsList)
                return;
            // 先排序
            client.ClientData.ElementhrtsList.Sort(delegate(GoodsData x, GoodsData y)
            {
                //return (Global.GetGoodsCatetoriy(y.GoodsID) - Global.GetGoodsCatetoriy(x.GoodsID));

                /*if (x.GoodsID - y.GoodsID == 0)
                    return (x.Id - y.Id);
                return (x.GoodsID - y.GoodsID);*/

                if (Global.GetEquipGoodsSuitID(y.GoodsID) - Global.GetEquipGoodsSuitID(x.GoodsID) == 0)
                {
                    if (x.GoodsID - y.GoodsID == 0)
                        return (x.Id - y.Id);
                    // 从小到大
                    return (x.GoodsID - y.GoodsID);
                }

                // suitid 从大到小
                return (Global.GetEquipGoodsSuitID(y.GoodsID) - Global.GetEquipGoodsSuitID(x.GoodsID));
                
            });

            bool bModify = false;
            int bagindex = 0;
            foreach (var item in client.ClientData.ElementhrtsList)
            {
                if (item.BagIndex != bagindex)
                {
                    item.BagIndex = bagindex;
                    if (false && !GameManager.Flag_OptimizationBagReset)
                    {
                        Global.ResetBagGoodsData(client, item);
                    }
                    bModify = true;
                }
                bagindex++;
            }

            TCPOutPacket tcpOutPacket = null;
            // 没有修改
            if (!bModify)
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<GoodsData>>(null, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_RESET_EHRTSBAG);
                Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket);
                return;
            }

            tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<GoodsData>>(client.ClientData.ElementhrtsList, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_RESET_EHRTSBAG);
            Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket);
        }

        #endregion 元素之心物品管理

        #region 元素之心客户端请求操作

        private static void RequestElementHrtList(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, GameClient client, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string strDBcmd = "";
            strDBcmd = StringUtil.substitute("{0}:{1}", client.ClientData.RoleID, (int)SaleGoodsConsts.ElementhrtsGoodsID);

            byte[] bytesCmd = new UTF8Encoding().GetBytes(strDBcmd);

            TCPProcessCmdResults result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, (int)TCPGameServerCmds.CMD_GETGOODSLISTBYSITE, bytesCmd, bytesCmd.Length, out tcpOutPacket, client.ServerId);
            if (TCPProcessCmdResults.RESULT_FAILED != result && null != tcpOutPacket)
            {
                //处理物品列表数据
                List<GoodsData> goodsDataList = DataHelper.BytesToObject<List<GoodsData>>(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);

                client.ClientData.ElementhrtsList = goodsDataList;
                Global.PushBackTcpOutPacket(tcpOutPacket);
            }

            if (null == client.ClientData.ElementhrtsList)
            {
                client.ClientData.ElementhrtsList = new List<GoodsData>();
            }
        }

        /// <summary>
        ///  申请元素数据
        /// </summary>
        public static TCPProcessCmdResults ProcessGetElementHrtList(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                if (2 != fields.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int site = Convert.ToInt32(fields[1]);
                if (site == (int)SaleGoodsConsts.ElementhrtsGoodsID)
                {
                    byte[] bytesData = DataHelper.ObjectToBytes<List<GoodsData>>(client.ClientData.ElementhrtsList);
                    GameManager.ClientMgr.SendToClient(client, bytesData, nID);
                }
                else if (site == (int)SaleGoodsConsts.UsingElementhrtsGoodsID)
                {
                    byte[] bytesData = DataHelper.ObjectToBytes<List<GoodsData>>(client.ClientData.UsingElementhrtsList);
                    GameManager.ClientMgr.SendToClient(client, bytesData, nID);
                }

                return TCPProcessCmdResults.RESULT_OK;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessGetElementHrtList", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }

        /// <summary>
        ///  申请获取猎取元素相关信息  
        /// </summary>
        public static TCPProcessCmdResults ProcessGetElementHrtsInfo(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                if (1 != fields.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 当前档次
                int currGrade = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementGrade);
                if (currGrade <= 0)
                {
                    currGrade = 1;
                }
                // 当前粉末
                int currPowder = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementPowderCount);

                // 角色id:元素粉末数量:当前档次
                string strcmd = string.Format("{0}:{1}:{2}", roleID, currPowder, currGrade);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessGetElementHrtsInfo", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }

        /// <summary>
        ///  佩戴/卸下元素之心
        /// </summary>
        public static TCPProcessCmdResults ProcessUseElementHrt(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                if (3 != fields.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int dbid = Convert.ToInt32(fields[1]);
                int state = Convert.ToInt32(fields[2]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                /*int dbid = 1;
                int state = 1;*/

                bool bEquip = state > 0;
                string strCmd = "";
                GoodsData goodsData = null;
                int newsite = 0;
                int newbagindex = 0;
                // 佩戴 
                if (bEquip)
                {
                    // 从元素背包中查找元素之心
                    goodsData = GetElementhrtsByDbID(client, (int)SaleGoodsConsts.ElementhrtsGoodsID, dbid);
                    if (null == goodsData)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.ItemNotExist, roleID, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    // 已经佩戴了
                    if ((int)SaleGoodsConsts.UsingElementhrtsGoodsID == goodsData.Site)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.AlreadyUse, roleID, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    // 检查元素装备栏是否有空位
                    if (GetUsingElementhrtsListCount(client) >= GetMaxUsingElementhrtsCount())
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.EquipIsFull, roleID, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    // 检查是否有同类型的元素之心
                    ElementhrtsError result = CheckCanEquipElementHrt(client, goodsData.GoodsID);
                    if (ElementhrtsError.Success != result)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)result, roleID, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    int bagindex = GetIdleSlotOfUsing(client);
                    if (bagindex < 0)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.EquipIsFull, roleID, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    newsite = (int)SaleGoodsConsts.UsingElementhrtsGoodsID;
                    newbagindex = bagindex;
                }
                // 	卸下 
                else
                {
                    // 从元素装备栏中查找元素之心
                    goodsData = GetElementhrtsByDbID(client, (int)SaleGoodsConsts.UsingElementhrtsGoodsID, dbid);
                    if (null == goodsData)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.ItemNotExist, roleID, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    if ((int)SaleGoodsConsts.UsingElementhrtsGoodsID != goodsData.Site)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.AlreadyOut, roleID, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    // 检查元素背包是否有空位
                    if (GetElementhrtsListCount(client) >= GetMaxElementhrtsCount())
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.BagIsFull, roleID, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    int bagindex = GetIdleSlotOfBag(client);
                    if (bagindex < 0)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.BagIsFull, roleID, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    newsite = (int)SaleGoodsConsts.ElementhrtsGoodsID;
                    newbagindex = bagindex;
                }

                // 更新道具信息
                string[] dbFields = null;
                strCmd = Global.FormatUpdateDBGoodsStr(roleID, dbid, "*"/*isusing*/, "*", "*", "*", newsite, "*", "*", 1, "*", newbagindex, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*"); // 卓越信息 [12/13/2013 LiaoWei] 装备转生
                TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strCmd, out dbFields, client.ServerId);
                if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.DBSERVERERROR, dbid, goodsData.Site, goodsData.BagIndex, 0);
                    GameManager.ClientMgr.SendToClient(client, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.DBSERVERERROR, dbid, goodsData.Site, goodsData.BagIndex, 0);
                    GameManager.ClientMgr.SendToClient(client, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_OK;
                }

                GoodsData DamonData = null;
                if (null != client.ClientData.DamonGoodsDataList)
                {
                    lock (client.ClientData.DamonGoodsDataList)
                    {
                        for (int i = 0; i < client.ClientData.DamonGoodsDataList.Count; i++)
                        {
                            GoodsData gd = client.ClientData.DamonGoodsDataList[i];
                            if (gd.Using <= 0)
                            {
                                continue;
                            }

                            DamonData = gd;
                            break;
                        }
                    }
                }

                // 从装备栏里删除
                RemoveElementhrtsData(client, goodsData);
                goodsData.Site = newsite;
                goodsData.BagIndex = newbagindex;
                if (bEquip)
                {
                    AddUsingElementhrtsData(client, goodsData);
                }
                else
                {
                    AddElementhrtsData(client, goodsData);
                }

                // 如果已经装备了精灵
                if (null != DamonData)
                {
                    // 再把精灵的属性加上
                    if (Global.RefreshEquipProp(client, goodsData))
                    {
                        //通知客户端属性变化
                        GameManager.ClientMgr.NotifyUpdateEquipProps(tcpMgr.MySocketListener, pool, client);

                        // 总生命值和魔法值变化通知(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifyOthersLifeChanged(tcpMgr.MySocketListener, pool, client);
                    }
                }

                // 更新玩家属性

                //向客户端发指令
                GameManager.ClientMgr.NotifyModGoods(Global._TCPManager.MySocketListener, pool, client, (int)ModGoodsTypes.ModValue, 
                    goodsData.Id, goodsData.Using, goodsData.Site, goodsData.GCount, goodsData.BagIndex, 1/*???*/);

                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.Success, roleID, dbid, state, newbagindex);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessUseElementHrt", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }

        public static ElementhrtsError CheckCanEquipElementHrt(GameClient client, int GoodsID)
        {
            //首先判断要进阶的物品是否是元素之心，如果不是，则返回失败代码
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(GoodsID, out systemGoods))
            {
                return ElementhrtsError.ErrorConfig;
            }

            //判断是否能装备
            int categoriy = systemGoods.GetIntValue("Categoriy");
            if (!IsElementHrt(categoriy))
            {
                return ElementhrtsError.CantEquip;
            }
            if (categoriy == (int)ItemCategories.SpecialElementHrt)
            {
                return ElementhrtsError.CantEquip;
            }

            if (null == client.ClientData.UsingElementhrtsList)
            {
                return ElementhrtsError.Success;
            }

            foreach (var item in client.ClientData.UsingElementhrtsList)
            {
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(item.GoodsID, out systemGoods))
                {
                    continue;
                }

                if (categoriy == systemGoods.GetIntValue("Categoriy"))
                {
                    return ElementhrtsError.SameCategoriy;
                }
            }

            return ElementhrtsError.Success;
        }

        /// <summary>
        ///  执行猎取操作
        /// </summary>CMD_SPR_GET_SOMEELEMENTHRTS
        public static TCPProcessCmdResults ProcessGetSomeElementHrts(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                if (3 != fields.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int times = Convert.ToInt32(fields[1]);
                bool bUseMoney = Convert.ToInt32(fields[2]) > 0;
                string strCmd = "";
                // 次数只能是1或者10
                if (1 != times && 10 != times)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.ErrorTimes, roleID, 0, 0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                // 检查背包剩余数量
                if (GetElementhrtsListCount(client) + times > GetMaxElementhrtsCount())
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.BagNotEnough, roleID, 0, 0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                // 如果是1次 并且 使用钻石抽取
                if (times > 1 && bUseMoney)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.ErrorParams, roleID, 0, 0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                string strResult = "";
                int nCount = 0;
                // 执行获取操作
                for (int i = 0; i < times; i++)
                {
                    int GoodsID = 0;
                    int EhtLevel = 0;
                    ElementhrtsError result = GetSomeElementHrts(client, bUseMoney, out GoodsID, out EhtLevel);
                    if (ElementhrtsError.Success != result)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)result, roleID, 0, 0, strResult);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    strResult += GoodsID;
                    strResult += ",";
                    //strResult += Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementGrade);
                    strResult += EhtLevel;
                    strResult += "|";
                    nCount++;
                    // 给客户端发送结果
                }

                LogManager.WriteLog(LogTypes.Info, string.Format("玩家抽取获取元素之心 times = {0}, count = {1}", times, nCount));

                // 当前档次
                int currGrade = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementGrade);
                if (currGrade <= 0)
                {
                    currGrade = 1;
                }
                // 当前粉末
                int currPowder = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementPowderCount);

                strCmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ElementhrtsError.Success, roleID, currPowder, currGrade, strResult);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessGetSomeElementHrts", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }

        public static ElementhrtsError GetSomeElementHrts(GameClient client, bool bUseMoney, out int GoodsID, out int EhtLevel)
        {
            GoodsID = 0;
            EhtLevel = 0;

            try
            {
                // 当前档次
                int currGrade = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementGrade);
                if (currGrade <= 0)
                {
                    currGrade = 1;
                }
                // 当前粉末
                int currPowder = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ElementPowderCount);

                // 如果是1次 并且 使用钻石抽取
                if (bUseMoney)
                {
                    currGrade = ZhuanShiGrade;
                }

                RefineType refinecfg = GetRefineType(currGrade);
                if (null == refinecfg)
                {
                    return ElementhrtsError.ErrorConfig;
                }

                // 判断条件
                if (client.ClientData.Level < refinecfg.MinLevel)
                {
                    return ElementhrtsError.ErrorLevel;
                }

                if (client.ClientData.Level > refinecfg.MaxLevel)
                {
                    return ElementhrtsError.ErrorLevel;
                }

                if (client.ClientData.ChangeLifeCount < refinecfg.MinZhuanSheng)
                {
                    return ElementhrtsError.ErrorLevel;
                }

                if (client.ClientData.ChangeLifeCount > refinecfg.MaxZhuanSheng)
                {
                    return ElementhrtsError.ErrorLevel;
                }

                // 检查背包剩余数量
                if (GetElementhrtsListCount(client) >= GetMaxElementhrtsCount())
                {
                    return ElementhrtsError.BagNotEnough;
                }

                // 检查粉末数量
                if (refinecfg.RefineCost > 0)
                {
                    if (currPowder < refinecfg.RefineCost)
                    {
                        return ElementhrtsError.PowderNotEnough;
                    }
                }

                // 检查钻石数量
                if (refinecfg.ZuanShiCost > 0)
                {
                    if (client.ClientData.UserMoney < refinecfg.ZuanShiCost)
                    {
                        return ElementhrtsError.MoneyNotEnough;
                    }
                }

                // 扣除钻石
                if (refinecfg.ZuanShiCost > 0)
                {
                    if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, refinecfg.ZuanShiCost, "获取元素之心"))
                    {
                        return ElementhrtsError.MoneyNotEnough;
                    }
                }

                // 扣除粉末
                if (refinecfg.RefineCost > 0)
                {
                    GameManager.ClientMgr.ModifyYuanSuFenMoValue(client, -refinecfg.RefineCost, "获取元素");
                }

                // 根据档位给物品
                List<ElementHrtsBase> baseList = GetElementHrtsBase(currGrade);
                if (null == baseList || baseList.Count <= 0)
                {
                    return ElementhrtsError.ErrorConfig;
                }

                int random = Global.GetRandomNumber(1, 100001);
                foreach (var item in baseList)
                {
                    if (random >= item.StartValues && random <= item.EndValues)
                    {
                        GoodsID = item.GoodsID;
                        break;
                    }
                }

                LogManager.WriteLog(LogTypes.Info, string.Format("获取元素之心随机数: grade = {0}, random = {1}, GoodsID = {2}", currGrade, random, GoodsID));

                // 如果没找到合适的区间，应该是策划配错了，把第一个东西给他并且log it…
                if (0 == GoodsID)
                {
                    GoodsID = baseList[0].GoodsID;
                    LogManager.WriteLog(LogTypes.Error, string.Format("获取元素之心获得配置异常: grade = {0}, random = {1}", currGrade, random));
                }

                SystemXmlItem systemGoods = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(GoodsID, out systemGoods))
                {
                    return ElementhrtsError.ErrorConfig;
                }

                if (null == systemGoods)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("GetSomeElementHrts: (null == systemGoods) GoodsID={0}", GoodsID));
                    return ElementhrtsError.ErrorConfig;
                }

                //判断是否能装备
                string props = systemGoods.GetStringValue("EquipProps");
                int suitid = systemGoods.GetIntValue("SuitID");
                int level = 1;
                int exp = 0;
                int categoriy = systemGoods.GetIntValue("Categoriy");

                /*if (categoriy == (int)ItemCategories.SpecialElementHrt)
                {
                    exp = GetSpecialElementHrtsExp(GoodsID);
                }
                else
                {
                    ElementHrtsLevelInfo info = GetElementHrtsLevelInfo(suitid, 1);
                    exp = info.TotalExp;
                }*/

                List<int> elementhrtsProps = new List<int>();
                elementhrtsProps.Add(level);
                elementhrtsProps.Add(0);

                Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                            GoodsID, 1/*GCount*/,
                            0, ""/*props*/, 0/*forgelevel*/,
                            1/*binding*/, (int)SaleGoodsConsts.ElementhrtsGoodsID, ""/*jewelList*/, false, 1,
                            /**/"获取元素之心", Global.ConstGoodsEndTime, 0, 0, 0, 0/*Strong*/, 0, 0, 0, null, elementhrtsProps);
                // 客户端需要这个等级
                EhtLevel = level;

                // 成功检测
                int randIndex = Global.GetRandomNumber(0, 100);
                // 提升档次
                if (randIndex <= refinecfg.SuccessRate * 100)
                {
                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ElementGrade, refinecfg.RefineLevel, true);
                }
                else
                {
                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ElementGrade, 1, true);
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "GetSomeElementHrts", false);
            }
            
            return ElementhrtsError.Success;
        }

        /// <summary>
        ///  强化元素之心
        /// </summary>
        public static TCPProcessCmdResults ProcessPowerElementHrt(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // roleid:srcid:site:dbid1|dbid2……
                fields = cmdData.Split(':');
                if (fields.Length < 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                string strCmd = "";
                // 被升级的道具
                int srcid = Convert.ToInt32(fields[1]);
                int srcsite = Convert.ToInt32(fields[2]);
                List<int> materialList = new List<int>(); 
                string[] strMaterials = fields[3].Split('|');
                for (int i = 0; i < strMaterials.Length; i++)
                {
                    if (strMaterials[i] != "")
                        materialList.Add(Convert.ToInt32(strMaterials[i]));
                }
                /*int srcid = 1;
                List<int> materialList = new List<int>();
                int temp = 2;
                materialList.Add(temp);
                temp++;
                materialList.Add(temp);
                temp++;
                materialList.Add(temp);
                temp++;
                materialList.Add(temp);
                temp++;*/

                if (materialList.Count <= 0)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.ErrorParams, roleID, 0, 0 ,0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                // 被升级的东西不能在原料列表里
                if (materialList.IndexOf(srcid) >= 0)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.ErrorParams, roleID, 0, 0, 0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                GoodsData goodsData = GetElementhrtsByDbID(client, srcsite, srcid);
                if (null == goodsData)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.ErrorParams, roleID, 0, 0, 0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                SystemXmlItem systemGoods = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out systemGoods))
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.GoodsNotExist, roleID, 0, 0, 0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                int categoriy = systemGoods.GetIntValue("Categoriy");
                if (!IsElementHrt(categoriy))
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.ErrorParams, roleID, 0, 0, 0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (categoriy == (int)ItemCategories.SpecialElementHrt)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.CantEquip, roleID, 0, 0, 0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                int addExp = 0;
                // 检查这些原料是否存在 并且计算这些东西的总经验
                foreach (var item in materialList)
                {
                    GoodsData material = GetElementhrtsByDbID(client, item);
                    if (null == material)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.GoodsNotExist, roleID, 0, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    int exp = 0;

                    SystemXmlItem systemMaterial = null;
                    if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(material.GoodsID, out systemMaterial))
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.GoodsNotExist, roleID, 0, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    int suitid = systemMaterial.GetIntValue("SuitID");
                    if (systemMaterial.GetIntValue("Categoriy") == (int)ItemCategories.SpecialElementHrt)
                    {
                        exp = GetSpecialElementHrtsExp(material.GoodsID);
                    }
                    else
                    {
                        if (null != material.ElementhrtsProps && material.ElementhrtsProps.Count >= 2)
                        {
                            ElementHrtsLevelInfo materialInfo = GetElementHrtsLevelInfo(systemMaterial.GetIntValue("SuitID"), material.ElementhrtsProps[0]/*level*/);
                            if (null != materialInfo)
                            {
                                exp = materialInfo.TotalExp + material.ElementhrtsProps[1];
                            }
                        }
                    }

                    addExp += exp;
                }

                // 删除道具
                foreach (var item in materialList)
                {
                    GoodsData material = GetElementhrtsByDbID(client, item);
                    if (null == material)
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.GoodsNotExist, roleID, 0, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    //RemoveElementhrtsData(client, material);
                    if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, tcpClientPool, pool, client, material, 1, false))
                    {
                        strCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.GoodsNotExist, roleID, 0, 0, 0, 0);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                }


                int SuitID = systemGoods.GetIntValue("SuitID");
                int currLevel = 1;
                int currExp = 0;
                if (null != goodsData.ElementhrtsProps && goodsData.ElementhrtsProps.Count >= 2)
                {
                    currLevel = goodsData.ElementhrtsProps[0];
                    currExp = goodsData.ElementhrtsProps[1];
                }

                while (addExp > 0)
                {
                    ElementHrtsLevelInfo info = GetElementHrtsLevelInfo(SuitID, currLevel + 1);
                    if (null == info)
                    {
                        break;
                    }

                    int NeedExp = Global.GMax(0, info.NeedExp - currExp);
                    if (NeedExp < 0)
                    {
                        break;
                    }

                    if (NeedExp > addExp)
                    {
                        currExp += addExp;
                        addExp = 0;
                    }
                    else
                    {
                        currLevel++;
                        currExp = 0;
                        addExp -= NeedExp;
                    }
                }

                UpdateGoodsArgs updateGoodsArgs = new UpdateGoodsArgs() { RoleID = client.ClientData.RoleID, DbID = srcid, WashProps = null };
                updateGoodsArgs.ElementhrtsProps = new List<int>();
                updateGoodsArgs.ElementhrtsProps.Add(currLevel);
                updateGoodsArgs.ElementhrtsProps.Add(currExp);

                GoodsData DamonData = null;
                if (null != client.ClientData.DamonGoodsDataList)
                {
                    lock (client.ClientData.DamonGoodsDataList)
                    {
                        for (int i = 0; i < client.ClientData.DamonGoodsDataList.Count; i++)
                        {
                            GoodsData gd = client.ClientData.DamonGoodsDataList[i];
                            if (gd.Using <= 0)
                            {
                                continue;
                            }

                            DamonData = gd;
                            break;
                        }
                    }
                }

                bool bEquip = false;
                int oldsuit = goodsData.Site;
                // 如果已经装备了精灵
                if (null != DamonData)
                { 
                    // 先把精灵脱下来
                    if ((int)SaleGoodsConsts.UsingElementhrtsGoodsID == goodsData.Site)
                    {
                        goodsData.Site = (int)SaleGoodsConsts.ElementhrtsGoodsID;
                        bEquip = Global.RefreshEquipProp(client, goodsData);// 此处不通知客户端
                        goodsData.Site = oldsuit;
                    }
                }

                //存盘并通知用户结果
                Global.UpdateGoodsProp(client, goodsData, updateGoodsArgs);

                //再把精灵穿上计算装备的合成属性
                //goodsData.Site = oldsuit;

                // 如果已经装备了精灵
                if (null != DamonData)
                {
                    if (bEquip && Global.RefreshEquipProp(client, goodsData))
                    {
                        //通知客户端属性变化
                        GameManager.ClientMgr.NotifyUpdateEquipProps(tcpMgr.MySocketListener, pool, client);

                        // 总生命值和魔法值变化通知(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifyOthersLifeChanged(tcpMgr.MySocketListener, pool, client);
                    }
                }
                
                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", (int)ElementhrtsError.Success, roleID, srcid, srcsite, currLevel, currExp);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessPowerElementHrt", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }

        /// <summary>
        ///  整理元素之心背包
        /// </summary>
        public static TCPProcessCmdResults ProcessResetElementHrtBag(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // roleid:srcid:site:dbid1|dbid2……
                fields = cmdData.Split(':');
                if (fields.Length < 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                SortElementhrtsList(client);
                
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessPowerElementHrt", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }
        #endregion 元素之心客户端请求操作

    }
}
