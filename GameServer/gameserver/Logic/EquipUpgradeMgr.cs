using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using GameServer.Server;

namespace GameServer.Logic
{
    /// <summary>
    /// 装备进阶管理
    /// </summary>
    public class EquipUpgradeMgr
    {
        /// <summary>
        /// 处理装备进阶
        /// </summary>
        /// <param name="goodsDbID"></param>
        /// <param name="ironNum"></param>
        /// <param name="goldRock"></param>
        /// <param name="luckyNum"></param>
        /// <returns></returns>
        public static int ProcessUpgrade(GameClient client, int goodsDbID)
        {
            //判断背包中是否有此物品
            GoodsData goodsData = Global.GetGoodsByDbID(client, goodsDbID);
            if (null == goodsData)
            {
                return -1;
            }

            if (goodsData.Site != 0) //如果物品不在背包中，拒绝操作
            {
                return -9998;
            }

            if (goodsData.Using > 0) //如果物品被佩戴在身上, 拒绝操作
            {
                return -9999;
            }

            //首先判断要进阶的物品是否是装备，如果不是，则返回失败代码
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out systemGoods))
            {
                return -2;
            }

            //判断是否是武器装备
            int categoriy = systemGoods.GetIntValue("Categoriy");
            if (categoriy < (int)ItemCategories.TouKui || categoriy >= (int)ItemCategories.EquipMax)
            {
                return -3;
            }

            //判断是否已经是最高阶的装备
            int suitID = systemGoods.GetIntValue("SuitID");
            
            //不用判断，便于将来自由扩展，也不用维护MaxSuitID的值
            /*
            if (suitID >= Global.MaxSuitID)
            {
                return -4;
            }
            */

            int toOccupation = Global.GetMainOccupationByGoodsID(goodsData.GoodsID);
            int toSex = systemGoods.GetIntValue("ToSex");

            int newGoodsID = -1;
            SystemXmlItem newGoodsXmlItem = null;
            Dictionary<int, SystemXmlItem> dict = GameManager.SystemGoods.SystemXmlItemDict;
            foreach (var goodsItem in dict.Values)
            {
                int nCmpCategoriy = goodsItem.GetIntValue("Categoriy"); // 类别
                int nCmpSuitID = goodsItem.GetIntValue("SuitID"); // 套装编号
                int nCmpToSex = goodsItem.GetIntValue("ToSex"); // 性别
                int nCmpOccu = goodsItem.GetIntValue("MainOccupation"); // 主职业

                if (nCmpCategoriy == categoriy && nCmpSuitID == (suitID + 1) && nCmpOccu == toOccupation && nCmpToSex == toSex)
                {
                    newGoodsXmlItem = goodsItem;
                    newGoodsID = goodsItem.GetIntValue("ID");
                    break;
                }
            }

            //没有找到下一阶的装备
            if (newGoodsID < 0)
            {
                return -5;
            }

            //下一阶装备ID
            int nextSuitID = suitID + 1;

            //返回装备进阶配置项
            SystemXmlItem systemEquipUpgradeItem = EquipUpgradeCacheMgr.GetEquipUpgradeCacheItem(categoriy, nextSuitID);

            //相当于前面的最高阶判断
            if (null == systemEquipUpgradeItem)
            {
                return -4;
            }

            //需要物品 女娲石
            int needGoodsID = systemEquipUpgradeItem.GetIntValue("NeedGoodsID");
            if (needGoodsID < 0)
            {
                return -6;
            }

            //需要物品数量
            int needGoodsNum = systemEquipUpgradeItem.GetIntValue("GoodsNum");
            if (needGoodsNum <= 0)
            {
                return -7;
            }

            //需要积分
            int needJiFen = systemEquipUpgradeItem.GetIntValue("JiFen");
            if (needJiFen <= 0)
            {
                return -8;
            }

            //成功率
            int succeed = systemEquipUpgradeItem.GetIntValue("Succeed") * 100;//为了以10000为标准进行概率判定，乘100
            if (succeed < 0)
            {
                return -9;
            }

            int newGoodsBinding = goodsData.Binding;
            bool usedBinding = false;
            bool usedTimeLimited = false;

            //女娲石数量不够
            if (Global.GetTotalGoodsCountByID(client, needGoodsID) < needGoodsNum)
            {
                return -10;
            }

            //积分是否足够
            int jiFen = GameManager.ClientMgr.GetZhuangBeiJiFenValue(client);
            if (jiFen < needJiFen)
            {
                return -11;//装备积分先屏蔽，主要方便测试
            }

            //扣除进阶道具
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                Global._TCPManager.TcpOutPacketPool, client, needGoodsID, needGoodsNum, false, out usedBinding, out usedTimeLimited))
            {
                return -12;
            }

            //成功与否判断
            if (Global.GetRandomNumber(0, 10001) > succeed)
            {
                return -1000; //进阶没有成功
            }

            //进阶成功，开始装备扣除
            //扣除积分
            GameManager.ClientMgr.ModifyZhuangBeiJiFenValue(client, -needJiFen, true);

            //扣除原有的装备
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                Global._TCPManager.TcpOutPacketPool, client, goodsDbID, false))
            {
                return -14;
            }

            int currentStrong = Math.Max(goodsData.Strong, 0);
            //int currentLucky = Math.Max(0, goodsData.Lucky);
            int currentLucky = 0; //幸运值不带过去

            //给予新的装备
            int dbRet = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, newGoodsID, 1, goodsData.Quality, "", goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, false, 1, "装备进阶", Global.ConstGoodsEndTime, goodsData.AddPropIndex, goodsData.BornIndex, currentLucky, currentStrong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, goodsData.WashProps);
            if (dbRet < 0)
            {
                return -2000;
            }

            //进阶成功(紫色 + 6级以上提示)
            Global.BroadcastEquipUpgradeOk(client, goodsData.GoodsID, newGoodsID, goodsData.Quality, goodsData.Forge_level);

            //处理成就，第一次炼化，炼化就是神装进阶，的确不好理解
            //ChengJiuManager.OnFirstLianHua(client);

            return dbRet;
        }
        /*
        /// <summary>
        /// 处理装备进阶
        /// </summary>
        /// <param name="goodsDbID"></param>
        /// <param name="ironNum"></param>
        /// <param name="goldRock"></param>
        /// <param name="luckyNum"></param>
        /// <returns></returns>
        public static int ProcessUpgrade(GameClient client, int goodsDbID, int ironNum, int goldRock, int luckyNum)
        {
            //判断背包中是否有此物品
            GoodsData goodsData = Global.GetGoodsByDbID(client, goodsDbID);
            if (null == goodsData)
            {
                return -1;
            }

            if (goodsData.Site != 0) //如果物品不在背包中，拒绝操作
            {
                return -9998;
            }

            if (goodsData.Using > 0) //如果物品被佩戴在身上, 拒绝操作
            {
                return -9999;
            }

            //首先判断要进阶的物品是否是装备，如果不是，则返回失败代码
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out systemGoods))
            {
                return -2;
            }

            //判断是否是武器装备
            int categoriy = systemGoods.GetIntValue("Categoriy");
            if (categoriy < (int)ItemCategories.Weapon || categoriy >= (int)ItemCategories.EquipMax)
            {
                return -3;
            }

            //判断是否已经是最高阶的装备
            int suitID = systemGoods.GetIntValue("SuitID");
            if (suitID >= Global.MaxSuitID)
            {
                return -4;
            }

            int toOccupation = systemGoods.GetIntValue("ToOccupation");

            int newGoodsID = -1;
            foreach (var goodsItem in GameManager.SystemGoods.SystemXmlItemDict.Values)
            {
                if (categoriy == goodsItem.GetIntValue("Categoriy") && 
                    goodsItem.GetIntValue("SuitID") == (suitID + 1) &&
                    goodsItem.GetIntValue("ToOccupation") == toOccupation)
                {
                    newGoodsID = goodsItem.GetIntValue("ID");
                    break;
                }
            }

            //没有找到下一阶的装备
            if (newGoodsID < 0)
            {
                return -5;
            }

            int jinjieRockGoodsID = Global.GetJinjieNextRocksGoodsID(newGoodsID);
            if (jinjieRockGoodsID < 0)
            {
                return -6;
            }

            int jinjieIronGoodsID = -1;
            if (ironNum > 0)
            {
                jinjieIronGoodsID = (int)GameManager.systemParamsList.GetParamValueIntByName("JinjieIronGoodsID");
                if (jinjieIronGoodsID < 0)
                {
                    return -7;
                }
            }

            int jinjieGoldGoodsID = -1;
            if (goldRock > 0)
            {
                jinjieGoldGoodsID = (int)GameManager.systemParamsList.GetParamValueIntByName("JinjieGoldGoodsID");
                if (jinjieGoldGoodsID < 0)
                {
                    return -8;
                }
            }

            int jinjieLuckyGoodsID = -1;
            if (luckyNum > 0)
            {
                jinjieLuckyGoodsID = (int)GameManager.systemParamsList.GetParamValueIntByName("JinjieLuckyGoodsID");
                if (jinjieLuckyGoodsID < 0)
                {
                    return -9;
                }
            }

            int needYinLiang = Global.GetJinjieNextLevelYinLiang(newGoodsID);
            needYinLiang = Global.RecalcNeedYinLiang(needYinLiang); //判断银两是否折半
            if (needYinLiang > 0)
            {
                if (needYinLiang > client.ClientData.YinLiang)
                {
                    return -59;
                }
            }

            int newGoodsBinding = goodsData.Binding;
            bool usedBinding = false;
            bool usedTimeLimited = false;
            int needNextRocksNum = Global.GetJinjieNextRocks(newGoodsID);

            if (Global.GetTotalGoodsCountByID(client, jinjieRockGoodsID) < needNextRocksNum)
            {
                return -10;
            }
     
            //扣除进阶道具
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                Global._TCPManager.TcpOutPacketPool, client, jinjieRockGoodsID, needNextRocksNum, false, out usedBinding, out usedTimeLimited))
            {
                return -10;
            }

            if (newGoodsBinding <= 0)
            {
                newGoodsBinding = usedBinding ? 1 : 0;
            }

            if (jinjieIronGoodsID > 0)
            {
                if (Global.GetTotalGoodsCountByID(client, jinjieIronGoodsID) < 1)
                {
                    return -11;
                }

                usedBinding = false;
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                    Global._TCPManager.TcpOutPacketPool, client, jinjieIronGoodsID, 1, false, out usedBinding, out usedTimeLimited))
                {
                    return -11;
                }

                if (newGoodsBinding <= 0)
                {
                    newGoodsBinding = usedBinding ? 1 : 0;
                }
            }

            if (jinjieGoldGoodsID > 0)
            {
                if (Global.GetTotalGoodsCountByID(client, jinjieGoldGoodsID) < 1)
                {
                    return -12;
                }

                usedBinding = false;
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                    Global._TCPManager.TcpOutPacketPool, client, jinjieGoldGoodsID, 1, false, out usedBinding, out usedTimeLimited))
                {
                    return -12;
                }

                if (newGoodsBinding <= 0)
                {
                    newGoodsBinding = usedBinding ? 1 : 0;
                }
            }

            //扣除银两
            if (needYinLiang > 0)
            {
                if (client.ClientData.YinLiang < needYinLiang)
                {
                    return -69;
                }

                if (!GameManager.ClientMgr.SubUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, 
                    Global._TCPManager.TcpOutPacketPool, client, needYinLiang))
                {
                    return -69;
                }
            }

            //获取进阶的概率
            int percent = Global.GetJinjieNextPercent(client, newGoodsID, luckyNum);
            int randNum = Global.GetRandomNumber(0, 101);

            if (jinjieLuckyGoodsID > 0)
            {
                if (Global.GetTotalGoodsCountByID(client, jinjieLuckyGoodsID) < 1)
                {
                    return -13;
                }

                usedBinding = false;
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                    Global._TCPManager.TcpOutPacketPool, client, jinjieLuckyGoodsID, 1, false, out usedBinding, out usedTimeLimited))
                {
                    return -13;
                }

                if (newGoodsBinding <= 0)
                {
                    newGoodsBinding = usedBinding ? 1 : 0;
                }
            }

            if (randNum > percent)
            {
                return -1000; //进阶没有成功
            }

            //扣除原有的装备
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                Global._TCPManager.TcpOutPacketPool, client, goodsDbID, false))
            {
                return -14;
            }

            int newLevel = (ironNum > 0) ? goodsData.Forge_level : 0;
            int newQuality = (goldRock > 0) ? goodsData.Quality : 0;

            //给予新的装备
            int dbRet = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, newGoodsID, 1, newQuality, "", newLevel, newGoodsBinding, 0, goodsData.Jewellist, false, 1, "装备进阶", Global.ConstGoodsEndTime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong);
            if (dbRet < 0)
            {
                return -2000;
            }

            //进阶成功(紫色 + 6级以上提示)
            Global.BroadcastEquipUpgradeOk(client, goodsData.GoodsID, newGoodsID, newQuality, newLevel);

            return dbRet;
        }
        */
    }
}
