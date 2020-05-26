using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Server;
using GameServer.Core.Executor;
using GameServer.Logic.ActivityNew.SevenDay;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Core.GameEvent;

namespace GameServer.Logic
{
    /// <summary>
    /// 缓存合成项
    /// </summary>
    public class CacheMergeItem
    {
        /// <summary>
        /// 新物品ID
        /// </summary>
        public List<int> NewGoodsID
        {
            get;
            set;
        }

        /// <summary>
        /// 旧的物品ID列表
        /// </summary>
        public List<int> OrigGoodsIDList
        {
            get;
            set;
        }

        /// <summary>
        /// 旧的物品个数
        /// </summary>
        public List<int> OrigGoodsNumList
        {
            get;
            set;
        }

        /// <summary>
        /// 消耗的点卷
        /// </summary>
        public int DianJuan
        {
            get;
            set;
        }

        /// <summary>
        /// 消耗的游戏金币
        /// </summary>
        public int Money
        {
            get;
            set;
        }

        /// <summary>
        /// 消耗的真气
        /// </summary>
        public int ZhenQi
        {
            get;
            set;
        }

        /// <summary>
        /// 消耗的积分(神器之魂)
        /// </summary>
        public int JiFen
        {
            get;
            set;
        }

        /// <summary>
        /// 消耗的天地精元
        /// </summary>
        public int JingYuan
        {
            get;
            set;
        }

        /// <summary>
        /// 成功概率
        /// </summary>
        public int SuccessRate
        {
            get;
            set;
        }

        /// <summary>
        /// 要丢弃的物品ID
        /// </summary>
        public Dictionary<string, int> DestroyGoodsIDs
        {
            get;
            set;
        }

        /// <summary>
        /// 发布开始的时间
        /// </summary>
        public string PubStartTime
        {
            get;
            set;
        }

        /// <summary>
        /// 发布结束的时间
        /// </summary>
        public string PubEndTime
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 合成新物品
    /// </summary>
    public class MergeNewGoods
    {
        #region 数据缓存

        /// <summary>
        /// 合成项数据缓存
        /// </summary>
        private static Dictionary<int, CacheMergeItem> MergeItemsDict = new Dictionary<int, CacheMergeItem>();

        /// <summary>
        /// 获取缓存项
        /// </summary>
        /// <param name="mergeItemID"></param>
        /// <returns></returns>
        private static CacheMergeItem GetCacheMergeItem(int mergeItemID)
        {
            CacheMergeItem cacheMergeItem = null;
            lock (MergeItemsDict)
            {
                if (MergeItemsDict.TryGetValue(mergeItemID, out cacheMergeItem))
                {
                    return cacheMergeItem;
                }
            }

            SystemXmlItem systemMergeItem = null;
            if (!GameManager.systemGoodsMergeItems.SystemXmlItemDict.TryGetValue(mergeItemID, out systemMergeItem))
            {
                return null;
            }

            List<int> origGoodsIDList = new List<int>();
            List<int> origGoodsNumList = new List<int>();

            string origGoodsIDs = systemMergeItem.GetStringValue("OrigGoodsIDs").Trim();
            if (!string.IsNullOrEmpty(origGoodsIDs))
            {
                string[] fields = origGoodsIDs.Split('|');
                if (null != fields)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        string[] fields2 = fields[i].Trim().Split(',');
                        if (fields2.Length != 2) continue;

                        origGoodsIDList.Add(Convert.ToInt32(fields2[0]));
                        origGoodsNumList.Add(Convert.ToInt32(fields2[1]));
                    }
                }
            }

            Dictionary<string, int> dictDestroyGoodsIDs = new Dictionary<string, int>();
            string destroyGoodsIDs = systemMergeItem.GetStringValue("destroy").Trim();
            if (!string.IsNullOrEmpty(destroyGoodsIDs))
            {
                string[] fields = destroyGoodsIDs.Split('|');
                if (null != fields)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        string[] fields2 = fields[i].Trim().Split(',');
                        if (fields2.Length != 2) continue;

                        dictDestroyGoodsIDs[fields2[0]] = Convert.ToInt32(fields2[1]);
                    }
                }
            }

            // 合成改造 增加字段 [12/12/2013 LiaoWei]
            List<int> tmpList = new List<int>();
            string newGoodID = systemMergeItem.GetStringValue("NewGoodsID");
            if (!string.IsNullOrEmpty(newGoodID))
            {
                string[] fields = newGoodID.Split('|');
                if (null != fields)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        int nID = 0;
                        nID = Convert.ToInt32(fields[i]);
                        if (nID > 0)
                            tmpList.Add(nID);
                    }
                }
            }

            cacheMergeItem = new CacheMergeItem()
            {
                NewGoodsID = tmpList,                   // 合成改造 [12/12/2013 LiaoWei] //systemMergeItem.GetIntValue("NewGoodsID"),
                OrigGoodsIDList = origGoodsIDList,
                OrigGoodsNumList = origGoodsNumList,
                DianJuan = systemMergeItem.GetIntValue("DianJuan"),
                Money = systemMergeItem.GetIntValue("Money"),
                //ZhenQi = systemMergeItem.GetIntValue("ZhenQi"),   // 合成改造 注释掉 [12/12/2013 LiaoWei]
                //JiFen = systemMergeItem.GetIntValue("JiFen"),     // 合成改造 注释掉 [12/12/2013 LiaoWei]
                JingYuan = systemMergeItem.GetIntValue("JingYuan"),
                SuccessRate = Global.GMin(systemMergeItem.GetIntValue("SuccessRate"), 100),
                DestroyGoodsIDs = dictDestroyGoodsIDs,
                PubStartTime = systemMergeItem.GetStringValue("PubStartTime"),
                PubEndTime = systemMergeItem.GetStringValue("PubEndTime"),
            };

            lock (MergeItemsDict)
            {
                MergeItemsDict[mergeItemID] = cacheMergeItem;
            }

            return cacheMergeItem;
        }

        /// <summary>
        /// 重新加载合成的缓存
        /// </summary>
        /// <returns></returns>
        public static int ReloadCacheMergeItems()
        {
            int ret = GameManager.systemGoodsMergeItems.ReloadLoadFromXMlFile();

            lock (MergeItemsDict)
            {
                MergeItemsDict.Clear();
            }

            return ret;
        }

        #endregion 数据缓存

        #region 合成处理

        /// <summary>
        /// 是否材料足够合成新物品
        /// </summary>
        /// <param name="cacheMergeItem"></param>
        /// <returns></returns>
        private static int CanMergeNewGoods(GameClient client, CacheMergeItem cacheMergeItem, int nMergeTargetItemID, bool bLeftGrid = false)
        {
            //是否有时间段限制????
            if (!string.IsNullOrEmpty(cacheMergeItem.PubStartTime) && !string.IsNullOrEmpty(cacheMergeItem.PubEndTime))
            {
                long startTime = Global.SafeConvertToTicks(cacheMergeItem.PubStartTime);
                long endTime = Global.SafeConvertToTicks(cacheMergeItem.PubEndTime);
                long nowTicks = TimeUtil.NOW();
                if (nowTicks < startTime || nowTicks > endTime)
                {
                    return -50;
                }
            }

            //判断背包是否够用
            if (!Global.CanAddGoods(client, nMergeTargetItemID, 1, 0, Global.ConstGoodsEndTime, true, bLeftGrid))
            {
                return -1;
            }

            //检查物品
            for (int i = 0; i < cacheMergeItem.OrigGoodsIDList.Count; i++)
            {
                int goodsNum1 = Global.GetTotalBindGoodsCountByID(client, cacheMergeItem.OrigGoodsIDList[i]);
                int goodsNum2 = Global.GetTotalNotBindGoodsCountByID(client, cacheMergeItem.OrigGoodsIDList[i]);

                if (goodsNum1 + goodsNum2 < cacheMergeItem.OrigGoodsNumList[i])
                {
                    return -2;
                }
            }

            //检查点卷
            if (cacheMergeItem.DianJuan > 0)
            {
                if (client.ClientData.UserMoney < cacheMergeItem.DianJuan)
                {
                    return -3;
                }
            }

            //检查银两
            if (cacheMergeItem.Money > 0)
            {
                if (Global.GetTotalBindTongQianAndTongQianVal(client) < cacheMergeItem.Money)
                {
                    return -4;
                }
            }

            /*//检查真气
            if (cacheMergeItem.ZhenQi > 0)
            {

                if (GameManager.ClientMgr.GetZhenQiValue(client) < cacheMergeItem.ZhenQi)
                {
                    return -5;
                }
            }*/

            /*//检查装备积分(神器之魂)
            if (cacheMergeItem.JiFen > 0)
            {

                if (GameManager.ClientMgr.GetZhuangBeiJiFenValue(client) < cacheMergeItem.JiFen)
                {
                    return -6;
                }
            }*/

            //检查天地精元
            if (cacheMergeItem.JingYuan > 0)
            {

                if (GameManager.ClientMgr.GetTianDiJingYuanValue(client) < cacheMergeItem.JingYuan)
                {
                    return -7;
                }
            }

            return 0;
        }

        /// <summary>
        /// 判断是否成功
        /// </summary>
        /// <param name="cacheMergeItem"></param>
        /// <returns></returns>
        private static bool JugeSucess(int mergeItemID, CacheMergeItem cacheMergeItem, int addSuccessPercent)
        {
            int randNum = Global.GetRandomNumber(0, 101);

            double awardmuti = 1.0;
            if (50 == mergeItemID)
            {
                // 合服果实
                JieRiMultAwardActivity activity = HuodongCachingMgr.GetJieRiMultAwardActivity();
                if (null != activity)
                {
                    JieRiMultConfig config = activity.GetConfig((int)MultActivityType.MergeFruitCoe);
                    if (null != config)
                    {
                        awardmuti += config.GetMult();
                    }
                }
            }

            int successRate = (int)(cacheMergeItem.SuccessRate * awardmuti);

            if (randNum <= (successRate + addSuccessPercent))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断是否是要消耗的物品ID
        /// </summary>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        private static int GetUsingGoodsNum(bool sucesss, CacheMergeItem cacheMergeItem, int goodsID, int goodsNum)
        {
            if (sucesss)
            {
                return goodsNum;
            }

            if (!cacheMergeItem.DestroyGoodsIDs.ContainsKey(goodsID.ToString()))
            {
                return goodsNum;
            }

            cacheMergeItem.DestroyGoodsIDs.TryGetValue(goodsID.ToString(), out goodsNum);
            return goodsNum;
        }

        /// <summary>
        /// 用材料合成新物品
        /// </summary>
        /// <param name="cacheMergeItem"></param>
        /// <returns></returns>
        private static int ProcessMergeNewGoods(GameClient client, int mergeItemID, CacheMergeItem cacheMergeItem, int luckyGoodsID, int nUseBindItemFirst)
        {
            //新合成的物品是否绑定
            int newGoodsBinding = 0;
            int addSuccessPercent = 0;

            bool bLeftGrid = false;
            int nNewGoodsID = cacheMergeItem.NewGoodsID[0];
            if (cacheMergeItem.NewGoodsID.Count > 1)
            {
                if (!Global.CanAddGoodsNum(client, 1))
                {
                    return -1; //包裹已满,因为合成物品未知,所以必须有一个空格子,
                }

                nNewGoodsID = cacheMergeItem.NewGoodsID[Global.GetRandomNumber(0, cacheMergeItem.NewGoodsID.Count)];
                bLeftGrid = true;
            }

            //检查是否满足合成的条件
            int ret = CanMergeNewGoods(client, cacheMergeItem, nNewGoodsID, bLeftGrid);
            if (ret < 0)
            {
                return ret;
            }

            if (luckyGoodsID > 0)
            {
                int luckyPercent = Global.GetLuckyValue(luckyGoodsID);
                if (luckyPercent > 0)
                {
                    bool usedBinding = false;
                    bool usedTimeLimited = false;

                    //从用户物品中扣除消耗的数量
                    if (GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                        Global._TCPManager.TcpOutPacketPool, client, luckyGoodsID, 1, false, out usedBinding, out usedTimeLimited))
                    {
                        if (newGoodsBinding <= 0)
                        {
                            newGoodsBinding = usedBinding ? 1 : 0;
                        }

                        addSuccessPercent = luckyPercent;
                    }
                }
            }

            //检查概率
            bool success = JugeSucess(mergeItemID, cacheMergeItem, addSuccessPercent);

            //检查物品
            for (int i = 0; i < cacheMergeItem.OrigGoodsIDList.Count; i++)
            {
                int usingGoodsNum = GetUsingGoodsNum(success, cacheMergeItem, cacheMergeItem.OrigGoodsIDList[i], cacheMergeItem.OrigGoodsNumList[i]);

                int nBindGoodNum    = Global.GetTotalBindGoodsCountByID(client, cacheMergeItem.OrigGoodsIDList[i]);
                int nNotBindGoodNum = Global.GetTotalNotBindGoodsCountByID(client, cacheMergeItem.OrigGoodsIDList[i]);

                if (usingGoodsNum > nBindGoodNum + nNotBindGoodNum)
                {
                    return -10;
                }

                bool usedBinding = false;
                bool usedTimeLimited = false;

                int nSubNum = usingGoodsNum;
                int nSum = 0;

                if (nUseBindItemFirst > 0 && nBindGoodNum >0)
                {
                    if (usingGoodsNum > nBindGoodNum)
                    {
                        nSum = nBindGoodNum;
                        nSubNum = usingGoodsNum - nBindGoodNum;
                    }
                    else
                    {
                        nSum = usingGoodsNum;
                        nSubNum = 0;
                    }

                    if (nSum > 0)
                    {
                        if (!GameManager.ClientMgr.NotifyUseBindGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                            Global._TCPManager.TcpOutPacketPool, client, cacheMergeItem.OrigGoodsIDList[i], nSum, false, out usedBinding, out usedTimeLimited, true))
                        {
                            return -10;
                        }
                    }

                    if (nSubNum > 0)
                    {
                        if (!GameManager.ClientMgr.NotifyUseNotBindGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                            Global._TCPManager.TcpOutPacketPool, client, cacheMergeItem.OrigGoodsIDList[i], nSubNum, false, out usedBinding, out usedTimeLimited, true))
                        {
                            return -10;
                        }
                    }

                    newGoodsBinding = 1;
                }
                else
                {
                    if (usingGoodsNum > nNotBindGoodNum)
                    {
                        nSum = nNotBindGoodNum;
                        nSubNum = usingGoodsNum - nNotBindGoodNum;
                    }
                    else
                    {
                        nSum = usingGoodsNum;
                        nSubNum = 0;
                    }

                    if (nSum > 0)
                    {
                        if (!GameManager.ClientMgr.NotifyUseNotBindGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                            Global._TCPManager.TcpOutPacketPool, client, cacheMergeItem.OrigGoodsIDList[i], nSum, false, out usedBinding, out usedTimeLimited, true))
                        {
                            return -10;
                        }
                    }

                    if (nSubNum > 0)
                    {
                        if (!GameManager.ClientMgr.NotifyUseBindGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                            Global._TCPManager.TcpOutPacketPool, client, cacheMergeItem.OrigGoodsIDList[i], nSubNum, false, out usedBinding, out usedTimeLimited, true))
                        {
                            return -10;
                        }

                        newGoodsBinding = 1;
                    }
                }

//                 if (newGoodsBinding <= 0)
//                 {
//                     newGoodsBinding = usedBinding ? 1 : 0;
//                 }
            }

            //检查点卷
            if (cacheMergeItem.DianJuan > 0)
            {
                //优先扣除金币
                //扣除的金币
                //int hasSubGold = 0;

                //扣除的元宝
                //int hasSubYuanBao = 0;

                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, cacheMergeItem.DianJuan, "合成新物品"))
                {
                    return -11;
                }

                //if (hasSubGold > 0)
                //{
                //    newGoodsBinding = 1;
                //}
            }

            //检查银两
            if (cacheMergeItem.Money > 0)
            {
                //if (!GameManager.ClientMgr.SubUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                //    Global._TCPManager.TcpOutPacketPool, client, cacheMergeItem.Money))
                if (!Global.SubBindTongQianAndTongQian(client, cacheMergeItem.Money, "材料合成"))
                {
                    return -12;
                }
            }

            // 合成改造 -- 注释掉真气和积分 [12/14/2013 LiaoWei]
            //检查真气
            /*if (cacheMergeItem.ZhenQi > 0)
            {
                //扣除角色的真气
                GameManager.ClientMgr.ModifyZhenQiValue(client, -cacheMergeItem.ZhenQi, true, true);
            }*/

            //检查积分
            /*if (cacheMergeItem.JiFen > 0)
            {
                //扣除角色的积分
                GameManager.ClientMgr.ModifyZhuangBeiJiFenValue(client, -cacheMergeItem.JiFen, true, true);
            }*/

            //检查天地精元
            if (cacheMergeItem.JingYuan > 0)
            {
                //扣除角色的天地精元
                GameManager.ClientMgr.ModifyTianDiJingYuanValue(client, -cacheMergeItem.JingYuan, "材料合成", true, true);
            }

            if (!success)
            {
                return -1000;
            }            

            //添加合成的新物品到角色的背包中
            //想DBServer请求加入某个新的物品到背包中
            int dbRet = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, nNewGoodsID, 1, 0, "", 0, newGoodsBinding, 0, "", true, 1, "材料合成新物品");
            if (dbRet < 0)
            {
                return -20;
            }

            //判断如果是宝石，并且超过了4级，则提示
            if ((int)ItemCategories.ItemJewel == Global.GetGoodsCatetoriy(nNewGoodsID))
            {
                //如果是宝石，处理成就
                //ChengJiuManager.OnRoleGoodsHeCheng(client, cacheMergeItem.NewGoodsID[0]);

                if (Global.GetJewelLevel(nNewGoodsID) >= 6) //6级以上
                {
                    //宝石合成
                    Global.BroadcastMergeJewelOk(client, nNewGoodsID);
                }
            }

            //如果是合成强化石，则处理成就
            if ((int)ItemCategories.ItemMaterial == Global.GetGoodsCatetoriy(nNewGoodsID))
            {
                //如果是宝石，处理成就
                //ChengJiuManager.OnRoleGoodsHeCheng(client, cacheMergeItem.NewGoodsID[0]);
            }

            //成就处理 第一次合成
            ChengJiuManager.OnFirstHeCheng(client);
            ChengJiuManager.OnRoleGoodsHeCheng(client, nNewGoodsID);

            // 七日活动
           SevenDayGoalEventObject evObj = SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.HeChengTimes);
           evObj.Arg1 = nNewGoodsID;
           GlobalEventSource.getInstance().fireEvent(evObj);

            return 0;
        }

        /// <summary>
        /// 处理合成的新物品
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mergeItemID"></param>
        public static int Process(GameClient client, int mergeItemID, int luckyGoodsID, int WingDBID, int CrystalDBID, int nUseBindItemFirst)
        {
            //获取缓存项
            CacheMergeItem cacheMergeItem = GetCacheMergeItem(mergeItemID);
            if (null == cacheMergeItem)
            {
                return -1000;
            }

            int ret = 0;
            // 翅膀合成 特殊处理！ [12/12/2013 LiaoWei]
            if (mergeItemID >= (int)WINGMERGEINFO.WINGMERGE_FIRST_LEVEL_ID && mergeItemID <= (int)WINGMERGEINFO.WINGMERGE_THIRD_LEVEL_ID)
            {
                //检查是否满足合成的条件
                ret = CanMergeNewGoods(client, cacheMergeItem, cacheMergeItem.NewGoodsID[0]);
                if (ret < 0)
                {
                    return ret;
                }

                ret = ProcessWingMerge(client, mergeItemID, luckyGoodsID, WingDBID, CrystalDBID, cacheMergeItem);
                if (ret < 0)
                    return ret;

                //成就处理 第一次合成
                ChengJiuManager.OnFirstHeCheng(client);
                ChengJiuManager.OnRoleGoodsHeCheng(client, cacheMergeItem.NewGoodsID[0]);
            }
            else
            {
                //用材料合成新物品
                ret = ProcessMergeNewGoods(client, mergeItemID, cacheMergeItem, luckyGoodsID, nUseBindItemFirst);
                if (ret < 0)
                {
                    return ret;
                }
            }

            

            return 0;
        }

        // 翅膀合成 Begin [12/12/2013 LiaoWei]
        /// <summary>
        /// 处理翅膀合成
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mergeItemID"></param>
        public static int ProcessWingMerge(GameClient client, int mergeItemID, int luckyGoodsID, int WingDBID, int CrystalDBID, CacheMergeItem cacheMergeItem)
        {
            /*  1.合成翅膀时，如果提供了晶石 那么一定生成晶石相对应的翅膀
                2.合成二阶翅膀时 必须提供一个强化等级为9的一阶翅膀
                3.合成三阶翅膀时 必须提供一个强化等级为9的二阶翅膀
            */

            GoodsData goodData = null;
            if (mergeItemID == (int)WINGMERGEINFO.WINGMERGE_SECOND_LEVEL_ID || mergeItemID == (int)WINGMERGEINFO.WINGMERGE_THIRD_LEVEL_ID)
            {   
                if (WingDBID < 0)
                    return -304;        // 没有放入一阶翅膀

                goodData = Global.GetGoodsByDbID(client, WingDBID);

                if (goodData == null)
                    return -305;    // 背包中没有一阶翅膀

                //if (goodData.Forge_level < 9)
                //    return -306;    // 翅膀强化等级未到9级

                /*List<int> lNeedWingID = null;
                lNeedWingID = GetWingIDForWingMerge(client, mergeItemID);
                if (lNeedWingID == null)
                    return -303;     // 配置错误

                for (int i = 0; i < lNeedWingID.Count; ++i)
                {
                    if (lNeedWingID[i] == goodData.GoodsID)
                        break;
                }*/
            }

            bool usedBinding = false;
            bool usedTimeLimited = false;

            // 扣除物品
            if (cacheMergeItem.OrigGoodsIDList != null)
            {
                // 先检测一遍有没有 够不够 然后再扣除
                for (int i = 0; i < cacheMergeItem.OrigGoodsIDList.Count; i++)
                {
                    GoodsData goodsData = Global.GetGoodsByID(client, cacheMergeItem.OrigGoodsIDList[i]);
                    if (null == goodsData)      // 没有找到物品
                        return -301;

                    if (goodsData.GCount < cacheMergeItem.OrigGoodsNumList[i])
                        return -301;            // 物品数量不够

                }

                for (int i = 0; i < cacheMergeItem.OrigGoodsIDList.Count; i++)
                {
                    //从用户物品中扣除消耗的数量
                    if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                                                client, cacheMergeItem.OrigGoodsIDList[i], cacheMergeItem.OrigGoodsNumList[i], false, out usedBinding, out usedTimeLimited, true))
                        return -301;        // 扣除物品失败

                }
            }

            int newGoodsBinding = 0;

            // 检测晶石
            List<int> nNeedCrystalID = null;
            nNeedCrystalID = GetCrystalIDForWingMerge(client, mergeItemID);

            if (nNeedCrystalID == null)
                return -302;            // 晶石信息错误 配置错误

            int nGoodsID = -1;
            //bool nRet = false;
            bool usedBinding1 = false;
            bool usedTimeLimited1 = false;

            if (CrystalDBID > 0)
            {
                GoodsData goodsinfo = null;
                goodsinfo = Global.GetGoodsByDbID(client, CrystalDBID);
                if (goodsinfo != null)
                {
                    nGoodsID = goodsinfo.GoodsID;
                    if (nNeedCrystalID.Count > 0 && !nNeedCrystalID.Contains(nGoodsID))
                    {
                        return -302;
                    }
                    if (GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                                            client, goodsinfo.GoodsID, 1, false, out usedBinding1, out usedTimeLimited1))
                    {
                        if (newGoodsBinding <= 0)
                            newGoodsBinding = usedBinding1 ? 1 : 0;

                        //nRet = true;
                    }
                }

            }

            // roll一下
            if (!RollWingMergeSuccess(client, cacheMergeItem, luckyGoodsID))
            {
                // 概率没到啊..

                /*if (goodData != null)
                {
                    // 要把翅膀的強化级别设为1
                    goodData.Forge_level = 1;

                    string[] dbFields = null;
                    string strDbCmd = Global.FormatUpdateDBGoodsStr(client.ClientData.RoleID, goodData.Id, "*", goodData.Forge_level, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*"); // 卓越一击 [12/13/2013 LiaoWei] 装备转生
                    TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, 
                                                                                        (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strDbCmd, out dbFields);
                    if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                        return -305;    // DB失敗

                    if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
                        return -305;    // DB失敗
                }*/

                return -300;
            }   

            int nWingGoods = -1;

            nWingGoods = GetFianlWingGoodsID(client, mergeItemID, nGoodsID, nNeedCrystalID);

            int ExcellenceProperty = RollWingGoodsExcellenceProperty(mergeItemID);

            int nForge = 0;
            if (goodData != null)
                nForge = goodData.Forge_level;

            int dbRet = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, nWingGoods, 1, 0, "", nForge, newGoodsBinding, 0, "", true, 1, "材料合成新物品--翅膀合成",
                                                    Global.ConstGoodsEndTime, 0, 0, 0, 0, ExcellenceProperty);
            if (dbRet < 0)
                return -20;
            else
            {
                // 消耗一阶翅膀
                if (goodData != null)
                {
                    bool usedBinding2 = false;
                    bool usedTimeLimited2 = false;

                    GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                                            client, goodData.GoodsID, 1, false, out usedBinding2, out usedTimeLimited2);
                }
                
            }

            return 0;
        }

        /// <summary>
        /// 取得翅膀合成需要的翅膀的ID列表 -- 二阶以上才需要
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mergeItemID"></param>
        public static List<int> GetWingIDForWingMerge(GameClient client, int mergeItemID)
        {
            List<int> lRet = null;

            string StrWingID = GameManager.systemParamsList.GetParamValueByName("HeChengChiBang");
            string[] arrID = StrWingID.Split('|');
            if (arrID.Length < 0 || arrID.Length > 3)
                return null;

            List<List<int>> WingIDList = new List<List<int>>();

            for (int i = 0; i < arrID.Length; ++i)
            {
                string[] sData = arrID[i].Split(',');
                if (sData.Length != 3)
                    return null;

                List<int> id = new List<int>();
                int nValue1 = -1;
                if (!int.TryParse(sData[0], out nValue1))
                    return null;

                id.Add(nValue1);

                int nValue2 = -1;
                if (!int.TryParse(sData[1], out nValue2))
                    return null;

                id.Add(nValue2);

                int nValue3 = -1;
                if (!int.TryParse(sData[2], out nValue3))
                    return null;

                id.Add(nValue3);

                WingIDList.Add(id);
            }

            if (mergeItemID == (int)WINGMERGEINFO.WINGMERGE_SECOND_LEVEL_ID)
                return WingIDList[0];
            else if (mergeItemID == (int)WINGMERGEINFO.WINGMERGE_THIRD_LEVEL_ID)
                return WingIDList[1];

            return lRet;
        }

        /// <summary>
        /// 翅膀的卓越属性roll一下
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mergeItemID"></param>
        public static int RollWingGoodsExcellenceProperty(int MergeID)
        {
            int nRet = 0;
            double[] ExcellenceArr = null;
            ExcellenceArr = GameManager.systemParamsList.GetParamValueDoubleArrayByName("WingMergeExcellencePropertyRandomID");

            if (ExcellenceArr == null || ExcellenceArr.Length != 3)
                return nRet;

            int nIndex = -1;
            if (MergeID == (int)WINGMERGEINFO.WINGMERGE_FIRST_LEVEL_ID)
                nIndex = 0;
            else if (MergeID == (int)WINGMERGEINFO.WINGMERGE_SECOND_LEVEL_ID)
                nIndex = 1;
            else if (MergeID == (int)WINGMERGEINFO.WINGMERGE_THIRD_LEVEL_ID)
                nIndex = 2;

            if (nIndex == -1)
                return nRet;

            int nIndex1 = -1;
            nIndex1 = (int)ExcellenceArr[nIndex];

            if (nIndex1 == -1)
                return nRet;

            //根据物品掉落卓越ID获取缓存项
            ExcellencePropertyGroupItem excellencePropertyGroupItem = GameManager.GoodsPackMgr.GetExcellencePropertyGroupItem(nIndex1);

            if (excellencePropertyGroupItem == null || null == excellencePropertyGroupItem.ExcellencePropertyItems ||
                    null == excellencePropertyGroupItem.Max || excellencePropertyGroupItem.Max.Length <= 0)
                return nRet;

            //int nCur = 0;
            //int nMax = excellencePropertyGroupItem.Max;

            int nNum = 0;
            int rndPercent = Global.GetRandomNumber(1, 100001);
            for (int i = 0; i < excellencePropertyGroupItem.ExcellencePropertyItems.Length; i++)
            {
                /*int rndPercent = Global.GetRandomNumber(1, 10001);
                int percent = (int)(excellencePropertyGroupItem.ExcellencePropertyItems[i].Percent * 10000);
                if (rndPercent <= percent)
                {
                    nRet |= Global.GetBitValue(excellencePropertyGroupItem.ExcellencePropertyItems[i].ID);
                    
                    ++nCur;

                    if (nCur >= nMax)
                        break;
                }*/
                if (rndPercent > excellencePropertyGroupItem.ExcellencePropertyItems[i].BasePercent &&
                        rndPercent <= (excellencePropertyGroupItem.ExcellencePropertyItems[i].BasePercent + excellencePropertyGroupItem.ExcellencePropertyItems[i].SelfPercent))
                {
                    nNum = excellencePropertyGroupItem.ExcellencePropertyItems[i].Num;
                    break;
                }
            }

            List<int> idList = new List<int>();

            if (nNum > 0 && nNum <= excellencePropertyGroupItem.Max.Length)
            {   
                int nCount = 0;
                while (true)
                {
                    int nProp = 0;
                    nProp = Global.GetRandomNumber(0, excellencePropertyGroupItem.Max.Length);
                    if (idList.IndexOf(nProp) < 0)
                    {
                        idList.Add(nProp);
                        ++nCount;
                    }

                    if (nCount == nNum)
                        break;
                }
            }

            for (int i = 0; i < idList.Count && i < excellencePropertyGroupItem.Max.Length; i++)
            {
                nRet |= Global.GetBitValue(excellencePropertyGroupItem.Max[idList[i]]);
            }

            return nRet;
        }

        /// <summary>
        /// roll一下成功与否
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mergeItemID"></param>
        public static bool RollWingMergeSuccess(GameClient client, CacheMergeItem cacheMergeItem, int luckyGoodsID)
        {
            int newGoodsBinding = 0;
            int addSuccessPercent = 0;

            if (luckyGoodsID > 0)
            {
                int luckyPercent = Global.GetLuckyValue(luckyGoodsID);
                if (luckyPercent > 0)
                {
                    bool usedBinding = false;
                    bool usedTimeLimited = false;

                    //从用户物品中扣除消耗的数量
                    if (GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                                                client, luckyGoodsID, 1, false, out usedBinding, out usedTimeLimited))
                    {
                        if (newGoodsBinding <= 0)
                            newGoodsBinding = usedBinding ? 1 : 0;

                        addSuccessPercent = luckyPercent;
                    }
                }
            }

            int randNum = Global.GetRandomNumber(0, 101);
            if (randNum <= (cacheMergeItem.SuccessRate + addSuccessPercent))
                return true;

            return false;
        }

        /// <summary>
        /// 取得翅膀合成需要的晶石列表
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mergeItemID"></param>
        public static List<int> GetCrystalIDForWingMerge(GameClient client, int mergeItemID)
        {
            string nStrCrystal = GameManager.systemParamsList.GetParamValueByName("ZhiYeHeChengJingShi");
            string[] arr = nStrCrystal.Split('|');
            if (arr.Length < 0 || arr.Length > 3)
                return null;

            List<List<int>> CrystalIDList = new List<List<int>>();
            
            for (int i = 0; i < arr.Length; ++i)
            {
                string[] sData = arr[i].Split(',');
                if (sData.Length != 3)
                    return null;

                List<int> id = new List<int>();
                int nValue1 = -1;
                if (!int.TryParse(sData[0], out nValue1))
                    return null;

                id.Add(nValue1);

                int nValue2 = -1;
                if (!int.TryParse(sData[1], out nValue2))
                    return null;

                id.Add(nValue2);

                int nValue3 = -1;
                if (!int.TryParse(sData[2], out nValue3))
                    return null;

                id.Add(nValue3);

                CrystalIDList.Add(id);
            }

            if (mergeItemID == (int)WINGMERGEINFO.WINGMERGE_FIRST_LEVEL_ID)
                return CrystalIDList[0];
            else if (mergeItemID == (int)WINGMERGEINFO.WINGMERGE_SECOND_LEVEL_ID)
                return CrystalIDList[1];
            else if (mergeItemID == (int)WINGMERGEINFO.WINGMERGE_THIRD_LEVEL_ID)
                return CrystalIDList[2];

            return null;
        }

        /// <summary>
        /// 得到生成的物品列表
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mergeItemID"></param>
        public static List<int> GetWingMergeCreateGoodsID(GameClient client, int mergeItemID)
        {
            string StrWing = GameManager.systemParamsList.GetParamValueByName("WingMergeCreatedID");
            string[] arr = StrWing.Split('|');
            if (arr.Length < 0 || arr.Length > 3)
                return null;

            List<List<int>> WingIDList = new List<List<int>>();
            
            for (int i = 0; i < arr.Length; ++i)
            {
                string[] sData = arr[i].Split(',');
                if (sData.Length != 3)
                    return null;

                List<int> id = new List<int>();
                int nValue1 = -1;
                if (!int.TryParse(sData[0], out nValue1))
                    return null;

                id.Add(nValue1);

                int nValue2 = -1;
                if (!int.TryParse(sData[1], out nValue2))
                    return null;

                id.Add(nValue2);

                int nValue3 = -1;
                if (!int.TryParse(sData[2], out nValue3))
                    return null;

                id.Add(nValue3);

                WingIDList.Add(id);
            }

            if (mergeItemID == (int)WINGMERGEINFO.WINGMERGE_FIRST_LEVEL_ID)
                return WingIDList[0];
            else if (mergeItemID == (int)WINGMERGEINFO.WINGMERGE_SECOND_LEVEL_ID)
                return WingIDList[1];
            else if (mergeItemID == (int)WINGMERGEINFO.WINGMERGE_THIRD_LEVEL_ID)
                return WingIDList[2];

            return null;
        }

        /// <summary>
        /// 得到最终的翅膀ID
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mergeItemID"></param>
        public static int GetFianlWingGoodsID(GameClient client, int mergeItemID, int nGoodsID, List<int> nNeedCrystalID)
        {
            
            List<int> nGoods = null;
            nGoods = GetWingMergeCreateGoodsID(client, mergeItemID);
            if (nGoods == null)
                return -303;    // 配置文件错了

            int nWingGoods = -1;
            int nIndex = -1;

            if (nGoodsID != -1)
            {
                for (int i = 0; i < nNeedCrystalID.Count; ++i)
                {
                    if (nNeedCrystalID[i] == nGoodsID)
                    {
                        nIndex = i;
                        break;
                    }
                }

                if (nIndex == -1)
                    return -303;    // 配置文件错误
            }
            else
                nIndex = Global.GetRandomNumber(0, 3);

            if (nIndex < 0 || nIndex > 3)
                nIndex = 0;

            nWingGoods = nGoods[nIndex];

            return nWingGoods;
        }

        // 翅膀合成 End [12/12/2013 LiaoWei]

        #endregion 合成处理 
    }
}
