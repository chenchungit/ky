using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System.Windows;
//using System.Windows.Documents;
using GameServer.Server;
using System.Xml.Linq;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Core.GameEvent;
using GameServer.Logic.ActivityNew.SevenDay;

namespace GameServer.Logic
{
    /// <summary>
    /// 乾坤袋挖宝
    /// </summary>
    public class QianKunManager
    {
        /// <summary>
        /// 祈福静态数据 NewDig.xml   key-typeid value- key-id value-SystemXmlItem
        /// </summary>
        public static Dictionary<int, Dictionary<int, SystemXmlItem>> m_ImpetrateDataInfo = null;
        public static Dictionary<int, Dictionary<int, SystemXmlItem>> m_ImpetrateDataInfoFree = null;
        // 节日活动期间，付费祈福读取这个配置
        public static Dictionary<int, Dictionary<int, SystemXmlItem>> m_ImpetrateDataHuoDong = null;

        /// <summary>   
        /// 线程锁 -- 资源重现加载时 必须锁住
        /// </summary>
        public static object m_mutex = new object();

        /// <summary>
        /// 付费祈福
        /// </summary>
        /// <param name="occupation"></param>
        public static void LoadImpetrateItemsInfo()
        {
            lock (m_mutex)
            {
                m_ImpetrateDataInfo = null;
                m_ImpetrateDataInfo = new Dictionary<int, Dictionary<int, SystemXmlItem>>();

                string fileName = "";
                XElement xml = null;

                try
                {
                    fileName = string.Format("Config/NewDig.xml");
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

                IEnumerable<XElement> jiNengXmlItems = xml.Elements("Type");

                foreach (var jiNengItem in jiNengXmlItems)
                {
                    int nType = (int)Global.GetSafeAttributeLong(jiNengItem, "TypeID");

                    Dictionary<int, SystemXmlItem> dicTmp = new Dictionary<int, SystemXmlItem>();

                    SystemXmlItem systemXmlItem;
                    IEnumerable<XElement> items = jiNengItem.Elements("Item");
                    foreach (var item in items)
                    {
                        systemXmlItem = new SystemXmlItem()
                        {
                            XMLNode = item,
                        };

                        int nKey = (int)Global.GetSafeAttributeLong(item, "ID");

                        dicTmp[nKey] = systemXmlItem;
                    }

                    m_ImpetrateDataInfo.Add(nType, dicTmp);
                }
            }
            
        }

        /// <summary>
        /// 免费祈福
        /// </summary>
        public static void LoadImpetrateItemsInfoFree()
        {
            lock (m_mutex)
            {
                m_ImpetrateDataInfoFree = null;
                m_ImpetrateDataInfoFree = new Dictionary<int, Dictionary<int, SystemXmlItem>>();

                string fileName = "";
                XElement xml = null;

                try
                {
                    fileName = string.Format("Config/FreeNewDig.xml");
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

                IEnumerable<XElement> jiNengXmlItems = xml.Elements("Type");

                foreach (var jiNengItem in jiNengXmlItems)
                {
                    int nType = (int)Global.GetSafeAttributeLong(jiNengItem, "TypeID");

                    Dictionary<int, SystemXmlItem> dicTmp = new Dictionary<int, SystemXmlItem>();

                    SystemXmlItem systemXmlItem;
                    IEnumerable<XElement> items = jiNengItem.Elements("Item");
                    foreach (var item in items)
                    {
                        systemXmlItem = new SystemXmlItem()
                        {
                            XMLNode = item,
                        };

                        int nKey = (int)Global.GetSafeAttributeLong(item, "ID");

                        dicTmp[nKey] = systemXmlItem;
                    }

                    m_ImpetrateDataInfoFree.Add(nType, dicTmp);
                }
            }

        }

        /// <summary>
        /// 节日活动期间--付费祈福
        /// </summary>
        public static void LoadImpetrateItemsInfoHuodong()
        {
            lock (m_mutex)
            {
                m_ImpetrateDataHuoDong = null;
                m_ImpetrateDataHuoDong = new Dictionary<int, Dictionary<int, SystemXmlItem>>();

                string fileName = "";
                XElement xml = null;

                try
                {
                    fileName = string.Format("Config/HuoDongNewDig.xml");
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

                IEnumerable<XElement> jiNengXmlItems = xml.Elements("Type");

                foreach (var jiNengItem in jiNengXmlItems)
                {
                    int nType = (int)Global.GetSafeAttributeLong(jiNengItem, "TypeID");

                    Dictionary<int, SystemXmlItem> dicTmp = new Dictionary<int, SystemXmlItem>();

                    SystemXmlItem systemXmlItem;
                    IEnumerable<XElement> items = jiNengItem.Elements("Item");
                    foreach (var item in items)
                    {
                        systemXmlItem = new SystemXmlItem()
                        {
                            XMLNode = item,
                        };

                        int nKey = (int)Global.GetSafeAttributeLong(item, "ID");

                        dicTmp[nKey] = systemXmlItem;
                    }

                    m_ImpetrateDataHuoDong.Add(nType, dicTmp);
                }
            }

        }

        /// <summary>
        /// 处理挖宝的操作
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessRandomWaBao(GameClient client, int binding, Dictionary<int, SystemXmlItem> SystemXmlItemDict, int nType)
        {
            //算一个万以内的随机数
            int randomNum = Global.GetRandomNumber(1, 10001);
            Dictionary<int, SystemXmlItem> dict = SystemXmlItemDict;

            List<SystemXmlItem> itemList = new List<SystemXmlItem>();
            foreach (var systemWaBaoItem in dict.Values)
            {
                if (randomNum >= systemWaBaoItem.GetIntValue("StartValues") && randomNum <= systemWaBaoItem.GetIntValue("EndValues"))
                {
                    itemList.Add(systemWaBaoItem);
                }
            }

            //没有挖到物品
            if (itemList.Count <= 0)
            {
                return; //没有挖到任何东西
            }

            List<string> mstTextList = new List<string>();

            int index = Global.GetRandomNumber(0, itemList.Count);
            SystemXmlItem waBaoItem = itemList[index];


            //先判断是否给予物品(背包的判断在使用物品哪儿做截获处理)
            int goodsID = (int)waBaoItem.GetIntValue("GoodsID");
            if (goodsID > 0)
            {
                //判断背包是否已经满了?
                if (Global.CanAddGoods(client, goodsID,
                    1,
                    binding,
                    Global.ConstGoodsEndTime,
                    true))
                {
                    //想DBServer请求加入某个新的物品到背包中
                    int dbRet = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                        goodsID,
                        1,
                        0,
                        "",
                        0,
                        binding,
                        0,
                        "", true, 1, /**/"乾坤袋挖宝获取道具", Global.ConstGoodsEndTime, 0, 0, 0, 0);

                    if (dbRet < 0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("使用乾坤袋挖宝时背包满，放入物品时错误, RoleID={0}, GoodsID={1}, Binding={2}, Ret={3}", client.ClientData.RoleID, goodsID, binding, dbRet));
                    }
                    else
                    {
                        //开启乾坤袋成功的提示
                        Global.BroadcastQianKunDaiGoodsHint(client, goodsID, nType);

                        string msgText = string.Format(Global.GetLang("恭喜您开启开启乾坤袋得到: {0}"), Global.GetGoodsNameByID(goodsID));
                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                    }
                }
                else
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("使用乾坤袋挖宝时背包满，无法放入物品, RoleID={0}, GoodsID={1}, Binding={2}", client.ClientData.RoleID, goodsID, binding));
                }
            }

            //给予非绑定金币
            int minMoney = (int)waBaoItem.GetIntValue("MinMoney");
            int maxMoney = (int)waBaoItem.GetIntValue("MaxMoney");
            if (minMoney >= 0 && maxMoney > minMoney)
            {
                int giveMoney = Global.GetRandomNumber(minMoney, maxMoney);
                if (giveMoney > 0)
                {
                    GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, giveMoney, "开启乾坤袋一");

                    string msgText = string.Format(Global.GetLang("开启乾坤袋得到金币: +{0}"), giveMoney);
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                }
            }

            //给予绑定元宝
            int minBindYuanBao = (int)waBaoItem.GetIntValue("MinBindYuanBao");
            int maxBindYuanBao = (int)waBaoItem.GetIntValue("MaxBindYuanBao");
            if (minBindYuanBao >= 0 && maxBindYuanBao > minBindYuanBao)
            {
                int giveBingYuanBao = Global.GetRandomNumber(minBindYuanBao, maxBindYuanBao);
                if (giveBingYuanBao > 0)
                {
                    GameManager.ClientMgr.AddUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, giveBingYuanBao, "开启乾坤袋");

                    string msgText = string.Format(Global.GetLang("开启乾坤袋得到绑定元宝: +{0}"), giveBingYuanBao);
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                }
            }

            //给予经验奖励
            int minExp = (int)waBaoItem.GetIntValue("MinExp");
            int maxExp = (int)waBaoItem.GetIntValue("MaxExp");
            if (minExp >= 0 && maxExp > minExp)
            {
                int giveExp = Global.GetRandomNumber(minExp, maxExp);
                if (giveExp > 0)
                {
                    GameManager.ClientMgr.ProcessRoleExperience(client, giveExp, false, true);

                    string msgText = string.Format(Global.GetLang("开启乾坤袋得到高额经验: +{0}"), giveExp);
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                }
            }
        }

        /// <summary>
        /// 处理砸金蛋挖宝的操作
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static String ProcessRandomWaBaoByZaDan(GameClient client, Dictionary<int, SystemXmlItem> SystemXmlItemDic, int nType, out String strRecord, int binding = 0, bool bMuProject = false)
        {
            //默认非绑定
            //int binding = 0;

            strRecord = null;

            //砸金蛋获取的各种奖励值
            int gainGoodsID = 0;
            int gainGoodsNum = 0;
            int gainGold = 0;
            int gainYinLiang = 0;
            int gainExp = 0;

            //算一个万以内的随机数
            int randomNum = Global.GetRandomNumber(1, 100001);
            Dictionary<int, SystemXmlItem> dict = SystemXmlItemDic;

            List<SystemXmlItem> itemList = new List<SystemXmlItem>();
            foreach (var systemWaBaoItem in dict.Values)
            {
                if (randomNum >= systemWaBaoItem.GetIntValue("StartValues") && randomNum <= systemWaBaoItem.GetIntValue("EndValues"))
                {
                    itemList.Add(systemWaBaoItem);
                    break;  // 直接往下执行 [8/27/2014 LiaoWei]
                }
            }

            //没有挖到物品
            if (itemList.Count <= 0)
            {
                return ""; //没有挖到任何东西
            }

            List<string> mstTextList = new List<string>();

            int index = Global.GetRandomNumber(0, itemList.Count);
            SystemXmlItem waBaoItem = itemList[index];
            
            // 把属性带进DB记录中 [4/1/2014 LiaoWei]
            int nGoodsLevel = 0;
            int nAppendProp = 0;
            int nLuckyProp = 0;
            int nExcellenceProp = 0;
            int nGoodCount = 1;

            nGoodCount = (int)waBaoItem.GetIntValue("Num");

            //先判断是否给予物品(背包的判断在使用物品哪儿做截获处理)
            int goodsID = (int)waBaoItem.GetIntValue("GoodsID");
            if (goodsID > 0)
            {
                //判断金蛋仓库是否已经满了?
                if (Global.CanAddGoodsToJinDanCangKu(client, goodsID,
                    1,
                    binding,
                    Global.ConstGoodsEndTime,
                    true))
                {
                    // 注意  MU 新增加--物品1.强化 2.追加 3.幸运 4.卓越 的随机掉落 begin[1/22/2014 LiaoWei]
                    {
                        // 1.强化
                        int nForgeFallId = -1;
                        nForgeFallId = (int)waBaoItem.GetIntValue("QiangHuaFallID");

                        if (nForgeFallId != -1)
                        {
                            nGoodsLevel = GameManager.GoodsPackMgr.GetFallGoodsLevel(nForgeFallId);
                        }
                        
                        // 2.追加
                        int nAppendPropFallId = -1;
                        nAppendPropFallId = (int)waBaoItem.GetIntValue("ZhuiJiaFallID");

                        if (nAppendPropFallId != -1)
                        {
                            nAppendProp = GameManager.GoodsPackMgr.GetZhuiJiaGoodsLevelID(nAppendPropFallId);
                        }

                        // 3.幸运
                        int nLuckyPropFallId = -1;
                        nLuckyPropFallId = (int)waBaoItem.GetIntValue("LckyProbability");

                        if (nLuckyPropFallId != -1)
                        {
                            int nValue = 0;
                            nValue = GameManager.GoodsPackMgr.GetLuckyGoodsID(nLuckyPropFallId);
                            if (nValue >= 1)
                            {
                                nLuckyProp = 1;
                            }
                        }

                        // 4.卓越
                        int nExcellencePropFallId = -1;
                        nExcellencePropFallId = (int)waBaoItem.GetIntValue("ZhuoYueFallID");

                        if (nExcellencePropFallId != -1)
                        {
                            nExcellenceProp = GameManager.GoodsPackMgr.GetExcellencePropertysID(nExcellencePropFallId);
                        }
                    }
                    // 注意  MU 新增加--物品1.强化 2.追加 3.幸运 4.卓越 的随机掉落 end[1/22/2014 LiaoWei]

                    //想DBServer请求加入某个新的物品到金蛋仓库中
                    int dbRet = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsID, nGoodCount, 0, "", nGoodsLevel, binding, (int)SaleGoodsConsts.JinDanGoodsID, "", true, 1,
                        /**/"乾坤袋挖宝获取道具", Global.ConstGoodsEndTime, 0, 0, nLuckyProp, 0, nExcellenceProp, nAppendProp);

                    if (dbRet < 0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("使用乾坤袋挖宝时背包满，放入物品时错误, RoleID={0}, GoodsID={1}, Binding={2}, Ret={3}", client.ClientData.RoleID, goodsID, binding, dbRet));
                    }
                    else
                    {
                        //开启乾坤袋成功的提示
                        Global.BroadcastQianKunDaiGoodsHint(client, goodsID, nType);

                        /*
                        string msgText = string.Format(Global.GetLang("恭喜您开启开启乾坤袋得到: {0}"), Global.GetGoodsNameByID(goodsID));
                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                        */

                        gainGoodsID = goodsID;
                        gainGoodsNum = 1;

                        // 七日活动
                        SevenDayGoalEventObject evObj = SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.GetEquipCountByQiFu);
                        evObj.Arg1 = goodsID;
                        evObj.Arg2 = gainGoodsNum;
                        GlobalEventSource.getInstance().fireEvent(evObj);
     
                    }
                }
                else
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("使用乾坤袋挖宝时背包满，无法放入物品, RoleID={0}, GoodsID={1}, Binding={2}", client.ClientData.RoleID, goodsID, binding));
                }
            }

            //给予非绑定金币
            int minMoney = (int)waBaoItem.GetIntValue("MinMoney");
            int maxMoney = (int)waBaoItem.GetIntValue("MaxMoney");
            if (minMoney >= 0 && maxMoney > minMoney)
            {
                int giveMoney = Global.GetRandomNumber(minMoney, maxMoney);
                if (giveMoney > 0)
                {
                    GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, giveMoney, "开启乾坤袋二");

                    /*
                    string msgText = string.Format(Global.GetLang("开启乾坤袋得到铜钱: +{0}"), giveMoney);
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                    */

                    gainYinLiang = giveMoney;
                }
            }

            //给予绑定元宝
            int minBindYuanBao = (int)waBaoItem.GetIntValue("MinBindYuanBao");
            int maxBindYuanBao = (int)waBaoItem.GetIntValue("MaxBindYuanBao");
            if (minBindYuanBao >= 0 && maxBindYuanBao > minBindYuanBao)
            {
                int giveBingYuanBao = Global.GetRandomNumber(minBindYuanBao, maxBindYuanBao);
                if (giveBingYuanBao > 0)
                {
                    GameManager.ClientMgr.AddUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, giveBingYuanBao, "开启乾坤袋二");

                    /*
                    string msgText = string.Format(Global.GetLang("开启乾坤袋得到绑定元宝: +{0}"), giveBingYuanBao);
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                    */

                    gainGold = giveBingYuanBao;
                }
            }

            //给予经验奖励
            int minExp = (int)waBaoItem.GetIntValue("MinExp");
            int maxExp = (int)waBaoItem.GetIntValue("MaxExp");
            if (minExp >= 0 && maxExp > minExp)
            {
                int giveExp = Global.GetRandomNumber(minExp, maxExp);
                if (giveExp > 0)
                {
                    GameManager.ClientMgr.ProcessRoleExperience(client, giveExp, false, true);

                    /*
                    string msgText = string.Format(Global.GetLang("开启乾坤袋得到高额经验: +{0}"), giveExp);
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                    */

                    gainExp = giveExp;
                }
            }

            string strProp = "";
            strProp = String.Format("{0}|{1}|{2}|{3}", nGoodsLevel, nAppendProp, nLuckyProp, nExcellenceProp);

            String sResult = null;
            
            if (bMuProject)
            {
                sResult = String.Format("{0},{1},{2},{3},{4},{5},{6}", gainGoodsID, nGoodCount, binding, nGoodsLevel, nAppendProp, nLuckyProp, nExcellenceProp);
            }
            else
                sResult = String.Format("{0}_{1}_{2}_{3}_{4}_{5}", gainGoodsID, gainGoodsNum, gainGold, gainYinLiang, gainExp, strProp);

            strRecord = String.Format("{0}_{1}_{2}_{3}_{4}_{5}", gainGoodsID, gainGoodsNum, gainGold, gainYinLiang, gainExp, strProp);

            return sResult;
        }

        /// <summary>
        /// 处理砸金蛋挖宝的操作,10连抽的特殊随机池
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static String ProcessRandomWaBaoByZaDanSP(GameClient client, Dictionary<int, SystemXmlItem> SystemXmlItemDic, int nType, out String strRecord, int binding = 0, bool bMuProject = false)
        {
            strRecord = null;

            //砸金蛋获取的各种奖励值
            int gainGoodsID = 0;
            int gainGoodsNum = 0;
            int gainGold = 0;
            int gainYinLiang = 0;
            int gainExp = 0;

            // 把属性带进DB记录中 [4/1/2014 LiaoWei]
            int nGoodsLevel = 0;
            int nAppendProp = 0;
            int nLuckyProp = 0;
            int nExcellenceProp = 0;
            int nGoodCount = 1;

            int[] goodsInfo = Global.GetRandomGoods(GameManager.systemParamsList.GetParamValueByName("QiFuTen"));

            int goodsID = goodsInfo[0];
            nGoodCount = goodsInfo[1];

            //判断金蛋仓库是否已经满了?
            if (Global.CanAddGoodsToJinDanCangKu(client, goodsID,
                1,
                binding,
                Global.ConstGoodsEndTime,
                true))
            {
                // 注意  MU 新增加--物品1.强化 2.追加 3.幸运 4.卓越 的随机掉落 begin[1/22/2014 LiaoWei]
                {
                    // 1.强化
                    int nForgeFallId = goodsInfo[3];
                    nGoodsLevel = GameManager.GoodsPackMgr.GetFallGoodsLevel(nForgeFallId);

                    // 2.追加
                    int nAppendPropFallId = goodsInfo[4];
                    nAppendProp = GameManager.GoodsPackMgr.GetZhuiJiaGoodsLevelID(nAppendPropFallId);

                    // 3.幸运
                    int nLuckyPropFallId = goodsInfo[5];
                    nLuckyProp = GameManager.GoodsPackMgr.GetLuckyGoodsID(nLuckyPropFallId);

                    // 4.卓越
                    int nExcellencePropFallId = goodsInfo[6];
                    nExcellenceProp = GameManager.GoodsPackMgr.GetExcellencePropertysID(nExcellencePropFallId);
                }

                //想DBServer请求加入某个新的物品到金蛋仓库中
                int dbRet = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsID, nGoodCount, 0, "", nGoodsLevel, binding, (int)SaleGoodsConsts.JinDanGoodsID, "", true, 1,
                    /**/"乾坤袋挖宝获取道具", Global.ConstGoodsEndTime, 0, 0, nLuckyProp, 0, nExcellenceProp, nAppendProp);

                if (dbRet < 0)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("使用乾坤袋挖宝时背包满，放入物品时错误, RoleID={0}, GoodsID={1}, Binding={2}, Ret={3}", client.ClientData.RoleID, goodsID, binding, dbRet));
                }
                else
                {
                    //开启乾坤袋成功的提示
                    Global.BroadcastQianKunDaiGoodsHint(client, goodsID, nType);

                    /*
                    string msgText = string.Format(Global.GetLang("恭喜您开启开启乾坤袋得到: {0}"), Global.GetGoodsNameByID(goodsID));
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                    */

                    gainGoodsID = goodsID;
                    gainGoodsNum = 1;

                    // 七日活动
                    SevenDayGoalEventObject evObj = SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.GetEquipCountByQiFu);
                    evObj.Arg1 = goodsID;
                    evObj.Arg2 = gainGoodsNum;
                    GlobalEventSource.getInstance().fireEvent(evObj);
                }
            }
            else
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("使用乾坤袋挖宝时背包满，无法放入物品, RoleID={0}, GoodsID={1}, Binding={2}", client.ClientData.RoleID, goodsID, binding));
            }

            string strProp = "";
            strProp = String.Format("{0}|{1}|{2}|{3}", nGoodsLevel, nAppendProp, nLuckyProp, nExcellenceProp);

            String sResult = null;
            
            if (bMuProject)
            {
                sResult = String.Format("{0},{1},{2},{3},{4},{5},{6}", gainGoodsID, nGoodCount, binding, nGoodsLevel, nAppendProp, nLuckyProp, nExcellenceProp);
            }
            else
                sResult = String.Format("{0}_{1}_{2}_{3}_{4}_{5}", gainGoodsID, gainGoodsNum, gainGold, gainYinLiang, gainExp, strProp);

            strRecord = String.Format("{0}_{1}_{2}_{3}_{4}_{5}", gainGoodsID, gainGoodsNum, gainGold, gainYinLiang, gainExp, strProp);

            return sResult;
        }
    }
}
