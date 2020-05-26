using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;

namespace GameServer.Logic
{
    /// <summary>
    /// 杨公宝库项
    /// </summary>
    public class YangGongBKItem
    {
        /// <summary>
        /// 物品掉路项列表
        /// </summary>
        public List<FallGoodsItem> FallGoodsItemList = null;

        /// <summary>
        /// 物品项列表
        /// </summary>
        public List<GoodsData> GoodsDataList = null;

        /// <summary>
        /// 物品项列表
        /// </summary>
        public List<GoodsData> TempGoodsDataList = null;

        /// <summary>
        /// 已经免费刷新的次数
        /// </summary>
        public int FreeRefreshNum = 0;

        /// <summary>
        /// 已经开启的次数
        /// </summary>
        public int ClickBKNum = 0;

        /// <summary>
        /// 已经挑选的物品记录字典
        /// </summary>
        public Dictionary<int, bool> PickUpDict = new Dictionary<int, bool>();

        /// <summary>
        /// 是否绑定宝物
        /// </summary>
        public bool IsBaoWuBinding = false;
    }

    /// <summary>
    /// 杨公宝库管理
    /// </summary>
    public class YangGongBKManager
    {
        /// <summary>
        /// 打开杨公宝库的处理
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static YangGongBKItem OpenYangGongBK(GameClient client, bool isBaoWuBinding)
        {
            YangGongBKItem yangGongBKItem = null;

            int yangGongFallID1 = (int)GameManager.systemParamsList.GetParamValueIntByName("YangGongFallID1");
            int yangGongFallID2 = (int)GameManager.systemParamsList.GetParamValueIntByName("YangGongFallID2");
            int yangGongFallID3 = (int)GameManager.systemParamsList.GetParamValueIntByName("YangGongFallID3");
            int yangGongFallID4 = (int)GameManager.systemParamsList.GetParamValueIntByName("YangGongFallID4");
            int yangGongFallID5 = (int)GameManager.systemParamsList.GetParamValueIntByName("YangGongFallID5");
            int yangGongFallID6 = (int)GameManager.systemParamsList.GetParamValueIntByName("YangGongFallID6");
            int yangGongFallID7 = (int)GameManager.systemParamsList.GetParamValueIntByName("YangGongFallID7");
            int yangGongFallID8 = (int)GameManager.systemParamsList.GetParamValueIntByName("YangGongFallID8");

            if (yangGongFallID1 <= 0 || yangGongFallID2 <= 0 || yangGongFallID3 <= 0 || yangGongFallID4 <= 0) return yangGongBKItem;
            if (yangGongFallID5 <= 0 || yangGongFallID6 <= 0 || yangGongFallID7 <= 0 || yangGongFallID8 <= 0) return yangGongBKItem;

            List<FallGoodsItem> gallGoodsItemList1 = GameManager.GoodsPackMgr.GetRandomFallGoodsItemList(yangGongFallID1, 1, false);
            List<FallGoodsItem> gallGoodsItemList2 = GameManager.GoodsPackMgr.GetRandomFallGoodsItemList(yangGongFallID2, 1, false);
            List<FallGoodsItem> gallGoodsItemList3 = GameManager.GoodsPackMgr.GetRandomFallGoodsItemList(yangGongFallID3, 1, false);
            List<FallGoodsItem> gallGoodsItemList4 = GameManager.GoodsPackMgr.GetRandomFallGoodsItemList(yangGongFallID4, 1, false);
            List<FallGoodsItem> gallGoodsItemList5 = GameManager.GoodsPackMgr.GetRandomFallGoodsItemList(yangGongFallID5, 1, true);
            List<FallGoodsItem> gallGoodsItemList6 = GameManager.GoodsPackMgr.GetRandomFallGoodsItemList(yangGongFallID6, 1, true);
            List<FallGoodsItem> gallGoodsItemList7 = GameManager.GoodsPackMgr.GetRandomFallGoodsItemList(yangGongFallID7, 1, true);
            List<FallGoodsItem> gallGoodsItemList8 = GameManager.GoodsPackMgr.GetRandomFallGoodsItemList(yangGongFallID8, 1, true);

            if (null == gallGoodsItemList1 || null == gallGoodsItemList2 || null == gallGoodsItemList3 || null == gallGoodsItemList4) return yangGongBKItem;
            if (null == gallGoodsItemList5 || null == gallGoodsItemList6 || null == gallGoodsItemList7 || null == gallGoodsItemList8) return yangGongBKItem;

            if (1 != gallGoodsItemList1.Count || 1 != gallGoodsItemList2.Count || 1 != gallGoodsItemList3.Count || 1 != gallGoodsItemList4.Count) return yangGongBKItem;
            if (1 != gallGoodsItemList5.Count || 1 != gallGoodsItemList6.Count || 1 != gallGoodsItemList7.Count || 1 != gallGoodsItemList8.Count) return yangGongBKItem;

            gallGoodsItemList1.AddRange(gallGoodsItemList2);
            gallGoodsItemList1.AddRange(gallGoodsItemList3);
            gallGoodsItemList1.AddRange(gallGoodsItemList4);
            gallGoodsItemList1.AddRange(gallGoodsItemList5);
            gallGoodsItemList1.AddRange(gallGoodsItemList6);
            gallGoodsItemList1.AddRange(gallGoodsItemList7);
            gallGoodsItemList1.AddRange(gallGoodsItemList8);

            gallGoodsItemList1 = Global.RandomSortList<FallGoodsItem>(gallGoodsItemList1); //随机打乱
            List<GoodsData> goodsDataList = GameManager.GoodsPackMgr.GetGoodsDataListFromFallGoodsItemList(gallGoodsItemList1);

            List<GoodsData> tempGoodsDataList = new List<GoodsData>();
            for (int i = 0; i < goodsDataList.Count; i++)
            {
                tempGoodsDataList.Add(goodsDataList[i]);
            }

            yangGongBKItem = new YangGongBKItem()
            {
                FallGoodsItemList = gallGoodsItemList1,
                GoodsDataList = goodsDataList,
                IsBaoWuBinding = isBaoWuBinding,
                TempGoodsDataList = tempGoodsDataList,
            };

            return yangGongBKItem;
        }

        /// <summary>
        /// 渐进的概率系数
        /// </summary>
        private static double[] YangGongBKNumPercents = null;

        /// <summary>
        /// 从杨公宝库的处理中选宝的操作
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int ClickYangGongBK(GameClient client, YangGongBKItem yangGongBKItem, out GoodsData goodsData)
        {
            goodsData = null;
            if (null == YangGongBKNumPercents)
            {
                YangGongBKNumPercents = GameManager.systemParamsList.GetParamValueDoubleArrayByName("YangGongBKNumPercents");
            }

            if (null == YangGongBKNumPercents || YangGongBKNumPercents.Length != 4)
            {
                return -1000;
            }

            List<FallGoodsItem> fallGoodsItemList = yangGongBKItem.FallGoodsItemList;
            List<GoodsData> goodsDataList = yangGongBKItem.GoodsDataList;
            //List<GoodsData> tempGoodsDataList = yangGongBKItem.TempGoodsDataList;
            if (null == fallGoodsItemList || null == goodsDataList)
            {
                return -200;
            }

            if (fallGoodsItemList.Count != goodsDataList.Count)
            {
                return -200;
            }

            /*int findIndex = Global.GetRandomNumber(0, tempGoodsDataList.Count);
            if (findIndex < 0)
            {
                return -300;
            }

            goodsData = tempGoodsDataList[findIndex];
            tempGoodsDataList.RemoveAt(findIndex);
            return findIndex;*/

            double numPercent = YangGongBKNumPercents[yangGongBKItem.ClickBKNum];

            int findIndex = -1;
            for (int i = 0; i < fallGoodsItemList.Count; i++)
            {
                int randNum = Global.GetRandomNumber(1, 10001);
                int percent = fallGoodsItemList[i].SelfPercent;
                if (fallGoodsItemList[i].IsGood)
                {
                    percent = (int)(percent * numPercent);
                }

                if (randNum <= percent)
                {
                    if (yangGongBKItem.PickUpDict.ContainsKey(goodsDataList[i].Id))
                    {
                        continue;
                    }

                    findIndex = i;
                    break;
                }
            }

            if (-1 == findIndex) //如果通过随机概率没有找到了物品
            {
                int maxIndex = -1;
                int maxPercent = 0;
                for (int i = 0; i < fallGoodsItemList.Count; i++)
                {
                    if (yangGongBKItem.PickUpDict.ContainsKey(goodsDataList[i].Id))
                    {
                        continue;
                    }

                    if (fallGoodsItemList[i].SelfPercent > maxPercent)
                    {
                        maxIndex = i;
                        maxPercent = fallGoodsItemList[i].SelfPercent;
                    }
                }

                findIndex = maxIndex;
            }

            if (findIndex < 0)
            {
                return -300;
            }

            goodsData = goodsDataList[findIndex];
            return findIndex;
        }
    }
}
