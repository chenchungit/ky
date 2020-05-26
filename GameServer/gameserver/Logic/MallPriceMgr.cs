using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 商城价格管理,缓存商城价格
    /// </summary>
    public class MallPriceMgr
    {
        //商城价格缓存类
        private static Dictionary<int, int> PriceDict = new Dictionary<int, int>();

        /// <summary>
        /// 清空缓存，商城配置信息重新加载的时候
        /// </summary>
        public static void ClearCache()
        {
            //清空时锁
            lock (PriceDict)
            {
                PriceDict.Clear();
            }
        }

        /// <summary>
        /// 通过物品ID返回商城价格，失败返回 -1 
        /// </summary>
        /// <returns></returns>
        public static int GetPriceByGoodsID(int goodsID)
        {
            int goodsPrice = -1;

            //尝试从缓存取
            if (!PriceDict.TryGetValue(goodsID, out goodsPrice))
            {
                goodsPrice = GetPriceByGoodsIDFromCfg(goodsID);
                if (goodsPrice > 0)
                {
                    //缓存时锁
                    lock (PriceDict)
                    {
                        PriceDict.Add(goodsID, goodsPrice);
                    }
                }
            }

            return goodsPrice;
        }

        /// <summary>
        /// 从配置信息获取物品价格
        /// </summary>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        private static int GetPriceByGoodsIDFromCfg(int goodsID)
        {
            //遍历商城价格列表
            foreach (var systemMallItem in GameManager.systemMallMgr.SystemXmlItemDict.Values)
            {
                int myGoodsID = systemMallItem.GetIntValue("GoodsID");
                if (myGoodsID != goodsID)
                {
                    continue;
                }

                int price = systemMallItem.GetIntValue("Price");
                if (price <= 0)
                {
                    //相关物品价格非法
                    return -1;
                }

                //是否有时间段限制????
                string pubStartTime = systemMallItem.GetStringValue("PubStartTime");
                string pubEndTime = systemMallItem.GetStringValue("PubEndTime");
                if (!string.IsNullOrEmpty(pubStartTime) && !string.IsNullOrEmpty(pubEndTime))
                {
                    long startTime = Global.SafeConvertToTicks(pubStartTime);
                    long endTime = Global.SafeConvertToTicks(pubEndTime);
                    long nowTicks = TimeUtil.NOW();
                    if (nowTicks < startTime || nowTicks > endTime)
                    {
                        //相关物品在商城已过期
                        return -1;
                    }
                }

                return price;
            }

            return -1;
        }

        /// <summary>
        /// 从配置信息获取物品价格类型 0 表示元宝 1表示金币==>用于自动扣除元宝或者金币时，即自动购买物品
        /// </summary>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        public static int GetPriceTypeByGoodsIDFromCfg(int goodsID)
        {
            //遍历商城价格列表
            foreach (var systemMallItem in GameManager.systemMallMgr.SystemXmlItemDict.Values)
            {
                int myGoodsID = systemMallItem.GetIntValue("GoodsID");
                if (myGoodsID != goodsID)
                {
                    continue;
                }

                int tabID = systemMallItem.GetIntValue("TabID");
                if (10000 == tabID)
                {
                    return 1;//金币
                }
            }

            return 0;
        }
    }
}
