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
using GameServer.Core.Executor;
using GameServer.Logic.ActivityNew.SevenDay;
using GameServer.Core.GameEvent;
using GameServer.Logic.ActivityNew;

namespace GameServer.Logic
{
    /// <summary>
    /// 精灵召唤的结果枚举
    /// </summary>
    public enum CallSpriteResult
    {
        Success = 0,			// 成功
        ErrorParams,            // 客户端传来的参数错误
        ErrorConfig,            // 配置错误
        ErrorLevel,			// 等级不满足
        ZuanShiNotEnough,	// 钻石不足
        BagIsFull,				// 背包已满
        SpriteBagIsFull,		// 精灵背包已满
        GoodsNotExist,          // 道具不存在
        DBSERVERERROR,          // 存储时出现问题
    }

    /// <summary>
    /// 召唤精灵的类型
    /// </summary>
    public class CallPetType
    {
        public int ID;              // 流水号
        public int MinZhuanSheng;   // 最小转生等级
        public int MinLevel;        // 最小等级
        public int MaxZhuanSheng;   // 最小转生等级
        public int MaxLevel;        // 最小等级
    }

    /// <summary>
    /// 召唤精灵
    /// </summary>
    public class CallPetConfig
    { 
        public int ID;	            // 流水号
        public int GoodsID;	        // 物品ID
        public int Num;	            // 数量
        public int QiangHuaFallID;	// 强化掉落ID
        public int ZhuiJiaFallID;	// 追加掉落ID
        public int LckyProbability;	// 幸运掉落概率
        public int ZhuoYueFallID;	// 卓越掉落ID
        public int MinMoney;	    // 最小金币
        public int MaxMoney;	    // 最大金币
        public int MinBindYuanBao;	// 最少绑定元宝
        public int MaxBindYuanBao;	// 最大绑定元宝
        public int MinExp;	        // 最少经验
        public int MaxExp;	        // 最大经验
        public int StartValues;     // 起始值
        public int EndValues;       // 结束值
    }

    class CallPetManager
    {
        #region 配置相关

        /// <summary>
        /// 精灵召唤配置字典线程锁
        /// </summary>
        private static object _CallPetMutex = new object();

        /// <summary>
        /// CallPetType.xml
        /// </summary>
        private static Dictionary<int, CallPetType> CallPetTypeDict = new Dictionary<int, CallPetType>();

        /// <summary>
        /// CallPet.xml
        /// </summary>
        private static List<CallPetConfig> CallPetConfigList = new List<CallPetConfig>();

        /// <summary>
        /// FreeCallPet.xml
        /// </summary>
        private static List<CallPetConfig> FreeCallPetConfigList = new List<CallPetConfig>();

        /// <summary>
        /// HuoDongCallPet.xml
        /// </summary>
        private static List<CallPetConfig> HuoDongCallPetConfigList = new List<CallPetConfig>();

        /// <summary>
        /// 免费的周期
        /// </summary>
        private static double CallPetFreeHour = 60;

        /// <summary>
        /// SystemParams.xml  CallPet
        /// </summary>
        private static Dictionary<int, int> CallPetPriceDict = new Dictionary<int, int>();

        /// <summary>
        /// SystemParams.xml  精灵积分转换比例 ConsumeCallPetJiFen
        /// </summary>
        private static double ConsumeCallPetJiFen = 0.1;

        /// <summary>
        /// SystemParams.xml  免费道具
        /// </summary>
        private static int CallPetGoodsID = 0;

        /// <summary>
        /// 加载CallPetType.xml
        /// </summary>
        public static void LoadCallPetType()
        {
            try
            { 
                lock (_CallPetMutex)
                {
                    CallPetTypeDict.Clear();
                    string fileName = "Config/CallPetType.xml";
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
                    XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Fatal, "加载Config/CallPetType.xml时出错!!!文件不存在");
                        return;
                    }

                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        CallPetType CfgData = new CallPetType();
                        CfgData.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");                          // 流水号
                        CfgData.MinZhuanSheng = (int)Global.GetSafeAttributeLong(xmlItem, "MinZhuanSheng");    // 最小转生等级
                        CfgData.MinLevel = (int)Global.GetSafeAttributeLong(xmlItem, "MinLevel");              // 最小等级
                        CfgData.MaxZhuanSheng = (int)Global.GetSafeAttributeLong(xmlItem, "MaxZhuanSheng");    // 最小转生等级
                        CfgData.MaxLevel = (int)Global.GetSafeAttributeLong(xmlItem, "MaxLevel");              // 最小等级
                        CallPetTypeDict[CfgData.ID] = CfgData;
                    }
                }            
            }

            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/CallPetType.xml时文件出错", ex);
            }
        }

        /// <summary>
        /// 加载CallPet.xml
        /// </summary>
        public static void LoadCallPetConfig()
        {
            try
            { 
                lock (_CallPetMutex)
                {
                    CallPetConfigList.Clear();
                    FreeCallPetConfigList.Clear();
                    HuoDongCallPetConfigList.Clear();

                    string fileName = "Config/CallPet.xml";
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
                    XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Fatal, "加载Config/CallPet.xml时出错!!!文件不存在");
                        return;
                    }

                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        CallPetConfig CfgData = new CallPetConfig();
                        CfgData.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");               // 流水号
                        CfgData.GoodsID = (int)Global.GetSafeAttributeLong(xmlItem, "GoodsID");     // 物品ID
                        CfgData.Num = (int)Global.GetSafeAttributeLong(xmlItem, "Num");             // 数量
                        CfgData.QiangHuaFallID = (int)Global.GetSafeAttributeLong(xmlItem, "QiangHuaFallID");	// 强化掉落ID
                        CfgData.ZhuiJiaFallID = (int)Global.GetSafeAttributeLong(xmlItem, "ZhuiJiaFallID");	// 追加掉落ID
                        CfgData.LckyProbability = (int)Global.GetSafeAttributeLong(xmlItem, "LckyProbability");	// 幸运掉落概率
                        CfgData.ZhuoYueFallID = (int)Global.GetSafeAttributeLong(xmlItem, "ZhuoYueFallID");	// 卓越掉落ID
                        CfgData.MinMoney = (int)Global.GetSafeAttributeLong(xmlItem, "MinMoney");	    // 最小金币
                        CfgData.MaxMoney = (int)Global.GetSafeAttributeLong(xmlItem, "MaxMoney");	    // 最大金币
                        CfgData.MinBindYuanBao = (int)Global.GetSafeAttributeLong(xmlItem, "MinBindYuanBao");	// 最少绑定元宝
                        CfgData.MaxBindYuanBao = (int)Global.GetSafeAttributeLong(xmlItem, "MaxBindYuanBao");	// 最大绑定元宝
                        CfgData.MinExp = (int)Global.GetSafeAttributeLong(xmlItem, "MinExp");	        // 最少经验
                        CfgData.MaxExp = (int)Global.GetSafeAttributeLong(xmlItem, "MaxExp");	        // 最大经验
                        CfgData.StartValues = (int)Global.GetSafeAttributeLong(xmlItem, "StartValues");     // 起始值
                        CfgData.EndValues = (int)Global.GetSafeAttributeLong(xmlItem, "EndValues");       // 结束值
                        CallPetConfigList.Add(CfgData);
                    }

                    fileName = "Config/FreeCallPet.xml";
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
                    xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Fatal, "加载Config/FreeCallPet.xml时出错!!!文件不存在");
                        return;
                    }

                    xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        CallPetConfig CfgData = new CallPetConfig();
                        CfgData.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");               // 流水号
                        CfgData.GoodsID = (int)Global.GetSafeAttributeLong(xmlItem, "GoodsID");     // 物品ID
                        CfgData.Num = (int)Global.GetSafeAttributeLong(xmlItem, "Num");             // 数量
                        CfgData.QiangHuaFallID = (int)Global.GetSafeAttributeLong(xmlItem, "QiangHuaFallID");	// 强化掉落ID
                        CfgData.ZhuiJiaFallID = (int)Global.GetSafeAttributeLong(xmlItem, "ZhuiJiaFallID");	// 追加掉落ID
                        CfgData.LckyProbability = (int)Global.GetSafeAttributeLong(xmlItem, "LckyProbability");	// 幸运掉落概率
                        CfgData.ZhuoYueFallID = (int)Global.GetSafeAttributeLong(xmlItem, "ZhuoYueFallID");	// 卓越掉落ID
                        CfgData.MinMoney = (int)Global.GetSafeAttributeLong(xmlItem, "MinMoney");	    // 最小金币
                        CfgData.MaxMoney = (int)Global.GetSafeAttributeLong(xmlItem, "MaxMoney");	    // 最大金币
                        CfgData.MinBindYuanBao = (int)Global.GetSafeAttributeLong(xmlItem, "MinBindYuanBao");	// 最少绑定元宝
                        CfgData.MaxBindYuanBao = (int)Global.GetSafeAttributeLong(xmlItem, "MaxBindYuanBao");	// 最大绑定元宝
                        CfgData.MinExp = (int)Global.GetSafeAttributeLong(xmlItem, "MinExp");	        // 最少经验
                        CfgData.MaxExp = (int)Global.GetSafeAttributeLong(xmlItem, "MaxExp");	        // 最大经验
                        CfgData.StartValues = (int)Global.GetSafeAttributeLong(xmlItem, "StartValues");     // 起始值
                        CfgData.EndValues = (int)Global.GetSafeAttributeLong(xmlItem, "EndValues");       // 结束值
                        FreeCallPetConfigList.Add(CfgData);
                    }

                    fileName = "Config/HuoDongCallPet.xml";
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
                    xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Fatal, "加载Config/HuoDongCallPet.xml时出错!!!文件不存在");
                        return;
                    }

                    xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        CallPetConfig CfgData = new CallPetConfig();
                        CfgData.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");               // 流水号
                        CfgData.GoodsID = (int)Global.GetSafeAttributeLong(xmlItem, "GoodsID");     // 物品ID
                        CfgData.Num = (int)Global.GetSafeAttributeLong(xmlItem, "Num");             // 数量
                        CfgData.QiangHuaFallID = (int)Global.GetSafeAttributeLong(xmlItem, "QiangHuaFallID");	// 强化掉落ID
                        CfgData.ZhuiJiaFallID = (int)Global.GetSafeAttributeLong(xmlItem, "ZhuiJiaFallID");	// 追加掉落ID
                        CfgData.LckyProbability = (int)Global.GetSafeAttributeLong(xmlItem, "LckyProbability");	// 幸运掉落概率
                        CfgData.ZhuoYueFallID = (int)Global.GetSafeAttributeLong(xmlItem, "ZhuoYueFallID");	// 卓越掉落ID
                        CfgData.MinMoney = (int)Global.GetSafeAttributeLong(xmlItem, "MinMoney");	    // 最小金币
                        CfgData.MaxMoney = (int)Global.GetSafeAttributeLong(xmlItem, "MaxMoney");	    // 最大金币
                        CfgData.MinBindYuanBao = (int)Global.GetSafeAttributeLong(xmlItem, "MinBindYuanBao");	// 最少绑定元宝
                        CfgData.MaxBindYuanBao = (int)Global.GetSafeAttributeLong(xmlItem, "MaxBindYuanBao");	// 最大绑定元宝
                        CfgData.MinExp = (int)Global.GetSafeAttributeLong(xmlItem, "MinExp");	        // 最少经验
                        CfgData.MaxExp = (int)Global.GetSafeAttributeLong(xmlItem, "MaxExp");	        // 最大经验
                        CfgData.StartValues = (int)Global.GetSafeAttributeLong(xmlItem, "StartValues");     // 起始值
                        CfgData.EndValues = (int)Global.GetSafeAttributeLong(xmlItem, "EndValues");       // 结束值
                        HuoDongCallPetConfigList.Add(CfgData);
                    }
                }                
            }

            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/CallPet.xml或FreeCallPet.xml时文件出错", ex);
            }
        }

        /// <summary>
        /// 加载SystemParams.xml里的独立配置
        /// </summary>
        public static void LoadCallPetSystem()
        {
            lock (_CallPetMutex)
            {
                CallPetPriceDict.Clear();
                string[] strPrice = GameManager.systemParamsList.GetParamValueByName("CallPet").Split(',');
                if (null == strPrice || strPrice.Length != 2)
                {
                    SysConOut.WriteLine("        加载SystemParams.xml时出错!!!CallPet不存在");
                    return;
                }
                CallPetPriceDict[1] = Convert.ToInt32(strPrice[0]);
                CallPetPriceDict[10] = Convert.ToInt32(strPrice[1]);
                double nHour = GameManager.systemParamsList.GetParamValueDoubleByName("FreeCallPet");
                if (nHour <= 0)
                {
                    SysConOut.WriteLine("        加载SystemParams.xml时出错!!!FreeCallPet不存在");
                    return;
                }
                CallPetFreeHour = nHour;

                double nTemp = GameManager.systemParamsList.GetParamValueDoubleByName("ConsumeCallPetJiFen");
                if (nTemp < 0)
                {
                    SysConOut.WriteLine("        加载SystemParams.xml时出错!!!ConsumeCallPetJiFen小于0");
                    return;
                }
                ConsumeCallPetJiFen = nTemp;

                nTemp = GameManager.systemParamsList.GetParamValueDoubleByName("ZhaoHuan");
                if (nTemp < 0)
                {
                    //SysConOut.WriteLine("        加载SystemParams.xml时出错!!!CallPetGoodsID小于0");
                    //return;
                }
                CallPetGoodsID = (int)nTemp;
            }
        }

        /// <summary>
        /// 加载CallPetType.xml里的配置
        /// </summary>
        public static CallPetType GetCallPetType(int type = 1)
        {
            CallPetType config = null;
            lock (_CallPetMutex)
            {
                if (CallPetTypeDict.ContainsKey(type))
                    config = CallPetTypeDict[type];
            }
            return config;
        }

        /// <summary>
        /// 取得CallPet.xml里的配置
        /// </summary>
        public static List<CallPetConfig> GetCallPetConfigList(bool freeCall)
        {
            lock (_CallPetMutex)
            {
                if (freeCall)
                {
                    return FreeCallPetConfigList;
                }
                else
                {
                    // 增加节日福利 召唤宠物
                    JieRiFuLiActivity act = HuodongCachingMgr.GetJieriFuLiActivity();
                    object o_placeholder = null;
                    if (act != null && act.IsOpened(EJieRiFuLiType.CallPetReplace, out o_placeholder))
                    {
                        return HuoDongCallPetConfigList;
                    }

                    return CallPetConfigList;
                }
            }
        }

        /// <summary>
        /// 取得价格 返回负值说明没有对应的配置
        /// </summary>
        public static int GetCallPetPrice(int times)
        {
            int price = -1;
            lock (_CallPetMutex)
            {
                if (CallPetPriceDict.ContainsKey(times))
                    price = CallPetPriceDict[times];
            }
            return price;
        }

        #endregion

        #region 道具管理

        /// <summary>
        /// 元素背包的最大格子数
        /// </summary>
        public static int MaxPetGridNum = 240;

        /// <summary>
        /// 根据物品的DbID获取精灵物品的信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GoodsData GetPetByDbID(GameClient client, int id)
        {
            if (null != client.ClientData.PetList)
            {
                for (int i = 0; i < client.ClientData.PetList.Count; i++)
                {
                    if (client.ClientData.PetList[i].Id == id)
                    {
                        return client.ClientData.PetList[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 添加精灵到背包
        /// </summary>
        /// <param name="goodsData"></param>
        public static void AddPetData(GameClient client, GoodsData goodsData)
        {
            if (goodsData.Site != (int)SaleGoodsConsts.PetBagGoodsID) return;

            if (null == client.ClientData.PetList)
            {
                client.ClientData.PetList = new List<GoodsData>();
            }

            lock (client.ClientData.PetList)
            {
                client.ClientData.PetList.Add(goodsData);
            }
        }

        /// <summary>
        /// 添加物品到精灵队列中
        /// </summary>
        /// <param name="client"></param>
        public static GoodsData AddPetData(GameClient client, int id, int goodsID, int forgeLevel, int quality, int goodsNum, int binding, int site, string jewelList, int idelBagIndex, string endTime,
            int addPropIndex, int bornIndex, int lucky, int strong, int ExcellenceProperty, int nAppendPropLev, int nEquipChangeLife)
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

            AddPetData(client, gd);
            return gd;
        }

        /// <summary>
        /// 删除精灵物品
        /// </summary>
        public static void RemovePetGoodsData(GameClient client, GoodsData goodsData)
        {
            lock (client.ClientData.PetList)
            {
                if (null != client.ClientData.PetList)
                {
                    client.ClientData.PetList.Remove(goodsData);
                }
            }
        }

        /// <summary>
        /// 返回包裹中的空闲位置 找不到返回-1
        /// </summary>
        public static int GetIdleSlotOfBag(GameClient client)
        {
            int idelPos = -1;

            if (null == client.ClientData.PetList)
                return 0;

            List<int> usedBagIndex = new List<int>();

            for (int i = 0; i < client.ClientData.PetList.Count; i++)
            {
                if (usedBagIndex.IndexOf(client.ClientData.PetList[i].BagIndex) < 0)
                {
                    usedBagIndex.Add(client.ClientData.PetList[i].BagIndex);
                }
            }

            for (int n = 0; n < GetMaxPetCount(); n++)
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
        public static int GetPetListCount(GameClient client)
        {
            if (null == client.ClientData.PetList)
            {
                return 0;
            }

            return client.ClientData.PetList.Count;
        }

        /// <summary>
        ///  获取元素背包的最大容量
        /// </summary>
        /// <returns></returns>
        public static int GetMaxPetCount()
        {
            return MaxPetGridNum;
        }

        #endregion 元素之心物品管理

        #region 逻辑相关

        /// <summary>
        /// 取得精灵召唤免费倒计时秒
        /// </summary>
        public static long getFreeSec(GameClient client)
        {
            double currSec = Global.GetOffsetSecond(TimeUtil.NowDateTime());
            double lastSec = Convert.ToDouble(Global.GetRoleParamByName(client, RoleParamName.CallPetFreeTime));
            double nIntSec = CallPetFreeHour * 60 * 60;
            // 距离最后一次免费召唤是否超过一定时间
            return (long)Global.GMax(0, lastSec + nIntSec - currSec);
        }

        /// <summary>
        /// 整理用户的金蛋仓库
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ResetPetBagAllGoods(GameClient client)
        {
            if (null != client.ClientData.PetList)
            {
                lock (client.ClientData.PetList)
                {
                    Dictionary<string, GoodsData> oldGoodsDict = new Dictionary<string, GoodsData>();
                    List<GoodsData> toRemovedGoodsDataList = new List<GoodsData>();
                    for (int i = 0; i < client.ClientData.PetList.Count; i++)
                    {
                        if (client.ClientData.PetList[i].Using > 0)
                        {
                            continue;
                        }

                        client.ClientData.PetList[i].BagIndex = 1;
                        int gridNum = Global.GetGoodsGridNumByID(client.ClientData.PetList[i].GoodsID);
                        if (gridNum <= 1)
                        {
                            continue;
                        }

                        GoodsData oldGoodsData = null;
                        string key = string.Format("{0}_{1}_{2}", client.ClientData.PetList[i].GoodsID,
                            client.ClientData.PetList[i].Binding, Global.DateTimeTicks(client.ClientData.PetList[i].Endtime));
                        if (oldGoodsDict.TryGetValue(key, out oldGoodsData))
                        {
                            int toAddNum = Global.GMin((gridNum - oldGoodsData.GCount), client.ClientData.PetList[i].GCount);

                            oldGoodsData.GCount += toAddNum;

                            client.ClientData.PetList[i].GCount -= toAddNum;
                            client.ClientData.PetList[i].BagIndex = 1;
                            oldGoodsData.BagIndex = 1;
                            if (!Global.ResetBagGoodsData(client, client.ClientData.PetList[i]))
                            {
                                //出错, 停止整理
                                break;
                            }

                            if (oldGoodsData.GCount >= gridNum) //旧的物品已经加满
                            {
                                if (client.ClientData.PetList[i].GCount > 0)
                                {
                                    oldGoodsDict[key] = client.ClientData.PetList[i];
                                }
                                else
                                {
                                    oldGoodsDict.Remove(key);
                                    toRemovedGoodsDataList.Add(client.ClientData.PetList[i]);
                                }
                            }
                            else
                            {
                                if (client.ClientData.PetList[i].GCount <= 0)
                                {
                                    toRemovedGoodsDataList.Add(client.ClientData.PetList[i]);
                                }
                            }
                        }
                        else
                        {
                            oldGoodsDict[key] = client.ClientData.PetList[i];
                        }
                    }

                    for (int i = 0; i < toRemovedGoodsDataList.Count; i++)
                    {
                        client.ClientData.PetList.Remove(toRemovedGoodsDataList[i]);
                    }

                    //按照物品分类排序
                    client.ClientData.PetList.Sort(delegate(GoodsData x, GoodsData y)
                    {
                        //return (Global.GetGoodsCatetoriy(y.GoodsID) - Global.GetGoodsCatetoriy(x.GoodsID));
                        return (y.GoodsID - x.GoodsID);
                    });

                    int index = 0;
                    for (int i = 0; i < client.ClientData.PetList.Count; i++)
                    {
                        if (client.ClientData.PetList[i].Using > 0)
                        {
                            continue;
                        }

                        if (false && GameManager.Flag_OptimizationBagReset)
                        {
                            bool godosCountChanged = client.ClientData.PetList[i].BagIndex > 0;
                            client.ClientData.PetList[i].BagIndex = index++;
                            if (godosCountChanged)
                            {
                                if (!Global.ResetBagGoodsData(client, client.ClientData.PetList[i]))
                                {
                                    //出错, 停止整理
                                    break;
                                }
                            }
                        }
                        else
                        {
                            client.ClientData.PetList[i].BagIndex = index++;
                            if (!Global.ResetBagGoodsData(client, client.ClientData.PetList[i]))
                            {
                                //出错, 停止整理
                                break;
                            }
                        }
                    }
                }
            }

            TCPOutPacket tcpOutPacket = null;

            if (null != client.ClientData.PetList)
            {
                //先锁定
                lock (client.ClientData.PetList)
                {
                    tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<GoodsData>>(client.ClientData.PetList, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_RESET_PETBAG);
                }
            }
            else
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<GoodsData>>(client.ClientData.PetList, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_RESET_PETBAG);
            }

            Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket);
        }

        /// <summary>
        /// 召唤精灵
        /// </summary>
        public static CallSpriteResult CallPet(GameClient client, int times, out string strGetGoods)
        {
            strGetGoods = "";

            if (times != 1 && times != 10)
            {
                return CallSpriteResult.ErrorParams;
            }

            // 检查等级
            CallPetType TypeData = GetCallPetType();
            if (null == TypeData)
            {
                return CallSpriteResult.ErrorConfig;
            }

            // 判断条件
            if (client.ClientData.Level < TypeData.MinLevel)
            {
                return CallSpriteResult.ErrorLevel;
            }

            if (client.ClientData.Level > TypeData.MaxLevel)
            {
                return CallSpriteResult.ErrorLevel;
            }

            if (client.ClientData.ChangeLifeCount < TypeData.MinZhuanSheng)
            {
                return CallSpriteResult.ErrorLevel;
            }

            if (client.ClientData.ChangeLifeCount > TypeData.MaxZhuanSheng)
            {
                return CallSpriteResult.ErrorLevel;
            }

            // 抽取一次 是不是到达免费次数了
            // 免费抽取的精灵为绑定状态，钻石抽取的精灵为不绑定状态
            bool bFreeCall = false;
            bool bUseGoods = false;
            int bind = 0;
            if (1 == times)
            {
                if (getFreeSec(client) <= 0)
                {
                    bFreeCall = true;
                    bind = 1;
                }
            }

            // 不是免费 看看有没有道具
            if (bFreeCall == false && CallPetGoodsID > 0)
            {
                if (1 == times)
                {
                    if (null != Global.GetGoodsByID(client, CallPetGoodsID))
                    {
                        bUseGoods = true;
                        bind = 1;
                    }
                }
            }

            // 检查金钱是否足够
            int nNeedZuanShi = GetCallPetPrice(times);
            if (nNeedZuanShi < 0)
            {
                return CallSpriteResult.ErrorConfig;
            }

            if (false == bFreeCall &&  false == bUseGoods)
            { 
                if (client.ClientData.UserMoney < nNeedZuanShi)
                {
                    return CallSpriteResult.ZuanShiNotEnough;
                }
            }

            // 检查精灵背包是否足够
            if (GetMaxPetCount() - GetPetListCount(client) < times)
            {
                return CallSpriteResult.SpriteBagIsFull;
            }

            if (bFreeCall)
            { 
                //do nothing;
            }
            // 扣道具
            else if (bUseGoods)
            {
                bool usedBinding = false;
                bool usedTimeLimited = false;
                // 消耗物品
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                        Global._TCPManager.TcpOutPacketPool, client, CallPetGoodsID, 1, false,  out usedBinding, out usedTimeLimited))
                {
                    bUseGoods = false;
                }
            }

            // 再扣钱
            if (false == bFreeCall && false == bUseGoods)
            {
                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nNeedZuanShi, "精灵召唤"))
                {
                    return CallSpriteResult.ZuanShiNotEnough;
                }
                bind = 0;
            }

            // 生成道具
            for (int i = 0; i < times; ++i)
            {
                CallPetConfig CfgData = null;
                List<CallPetConfig> CfgList = GetCallPetConfigList(bFreeCall || bUseGoods);
                if (null == CfgList || CfgList.Count <= 0)
                {
                    return CallSpriteResult.ErrorConfig;
                }

                int random = Global.GetRandomNumber(1, 100001);
                foreach (var item in CfgList)
                {
                    if (random >= item.StartValues && random <= item.EndValues)
                    {
                        CfgData = item;
                        break;
                    }
                }

                LogManager.WriteLog(LogTypes.Info, string.Format("获取精灵随机数: random = {0}, GoodsID = {1}", random, CfgData.GoodsID));

                if (null == CfgData)
                {
                    continue;
                }

                // 生成卓越属性
                int nExcellenceProp = 0;

                if (CfgData.ZhuoYueFallID != -1)
                {
                    nExcellenceProp = GameManager.GoodsPackMgr.GetExcellencePropertysID(CfgData.ZhuoYueFallID);
                }

                // 加道具
                Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                            CfgData.GoodsID, CfgData.Num/*GCount*/,
                            0, ""/*props*/, 0/*forgelevel*/,
                            bind/*binding*/, (int)SaleGoodsConsts.PetBagGoodsID, ""/*jewelList*/, false, 1,
                            /**/"精灵召唤", Global.ConstGoodsEndTime, 0, 0, 0, 0/*Strong*/, nExcellenceProp/*ExcellenceProperty*/, 0, 0);
               
                // 返回4个参数 1.返回值 2.抽奖类型 3.抽中的物品列表(物品ID, 物品ID|物品数量|是否绑定|强化等级|追加等级|是否有幸运|卓越属性,...) 4.免费祈福的剩余时间
                strGetGoods += String.Format("{0},{1},{2},{3},{4},{5},{6}|", CfgData.GoodsID, CfgData.Num, bind, 0, 0, 0, nExcellenceProp);
            }

            // 如果是免费的 更新下上次召唤的时间
            if (true == bFreeCall)
            {
                double currSec = Global.GetOffsetSecond(TimeUtil.NowDateTime());
                Global.UpdateRoleParamByName(client, RoleParamName.CallPetFreeTime, currSec.ToString(), true);

                if (client._IconStateMgr.CheckPetIcon(client))
                    client._IconStateMgr.SendIconStateToClient(client);
            }
            else if (true == bUseGoods)
            { 
                // do nothing
            }
            else
            {
                int nPetJiFen = (int)(nNeedZuanShi * ConsumeCallPetJiFen);
                GameManager.ClientMgr.ModifyPetJiFenValue(client, nPetJiFen, "精灵召唤");
            }

            return CallSpriteResult.Success;
        }

        /// <summary>
        /// 把精灵从精灵背包移动到普通背包
        /// </summary>
        public static CallSpriteResult MovePet(GameClient client, int dbid)
        {
            GoodsData goodsData = GetPetByDbID(client, dbid);
            if (null == goodsData)
            {
                return CallSpriteResult.GoodsNotExist;
            }

            // 先判断是否能取到背包中(客户端也有判断) 然后检索到背包中剩余的格子 [7/24/2014 LiaoWei]
            if (!Global.CanAddGoods(client, goodsData.GoodsID, goodsData.GCount, goodsData.Binding))
            {
                return CallSpriteResult.BagIsFull;
            }

            // 更新道具信息
            string[] dbFields = null;
            string strCmd = Global.FormatUpdateDBGoodsStr(client.ClientData.RoleID, dbid, "*"/*isusing*/, "*", "*", "*", 0, "*", "*", 1, "*", 0, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*");
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strCmd, out dbFields, client.ServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                //strCmd = string.Format("{0}:{1}:{2}:{3}", (int)ElementhrtsError.DBSERVERERROR, dbid, goodsData.Site, goodsData.BagIndex);
                //GameManager.ClientMgr.SendToClient(client, strCmd, nID);
                return CallSpriteResult.DBSERVERERROR;
            }

            if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
            {
                //strCmd = string.Format("{0}:{1}:{2}:{3}", (int)ElementhrtsError.DBSERVERERROR, dbid, goodsData.Site, goodsData.BagIndex);
                //GameManager.ClientMgr.SendToClient(client, strCmd, nID);
                return CallSpriteResult.DBSERVERERROR;
            }

            RemovePetGoodsData(client, goodsData);
            goodsData.Site = 0;
            Global.AddGoodsData(client, goodsData);
           
            return CallSpriteResult.Success;
        }
        
        #endregion

        #region 协议相关

        /// <summary>
        ///  申请精灵数据
        /// </summary>
        public static TCPProcessCmdResults ProcessGetPetList(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                byte[] bytesData = DataHelper.ObjectToBytes<List<GoodsData>>(client.ClientData.PetList);
                GameManager.ClientMgr.SendToClient(client, bytesData, nID);

                return TCPProcessCmdResults.RESULT_OK;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessGetPetList", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }

        /// <summary>
        ///  申请精灵界面所需数据
        /// </summary>
        public static TCPProcessCmdResults ProcessGetPetUIInfo(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                string strcmd = string.Format("{0}:{1}", roleID, getFreeSec(client));
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessGetPetUIInfo", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }

        /// <summary>
        /// 精灵召唤
        /// </summary>
        public static TCPProcessCmdResults ProcessCallPetCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int times = Convert.ToInt32(fields[1]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                
                string strGetGoods = "";
                CallSpriteResult result = CallPet(client, times, out strGetGoods);
                string strcmd = "";

                if (result != CallSpriteResult.Success)
                {
                    strcmd = string.Format("{0}:{1}:{2}:{3}", (int)result, roleID, 0, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                strcmd = string.Format("{0}:{1}:{2}:{3}", (int)CallSpriteResult.Success, times, strGetGoods, getFreeSec(client));
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessCallPetCMD", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// 精灵提取
        /// </summary>
        public static TCPProcessCmdResults ProcessMovePetCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            return TCPProcessCmdResults.RESULT_OK;

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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int dbid = Convert.ToInt32(fields[1]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                CallSpriteResult result = MovePet(client, dbid);
                string strcmd = "";

                if (result != CallSpriteResult.Success)
                {
                    strcmd = string.Format("{0}:{1}:{2}", (int)result, roleID, dbid);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                strcmd = string.Format("{0}:{1}:{2}", (int)CallSpriteResult.Success, roleID, dbid);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessMovePetCMD", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// 整理精灵背包
        /// </summary>
        public static TCPProcessCmdResults ProcessResetPetBagCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
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

                ResetPetBagAllGoods(client);
                return TCPProcessCmdResults.RESULT_OK;

                /*CallSpriteResult result = ResetPetBagAllGoods(client);
                string strcmd = "";

                if (result != CallSpriteResult.Success)
                {
                    strcmd = string.Format("{0}:{1}:{2}", (int)result, roleID, dbid);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }*/

                //strcmd = string.Format("{0}:{1}:{2}", (int)CallSpriteResult.Success, roleID, dbid);
                //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                //return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessResetPetBagCMD", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        #endregion

    }
}
