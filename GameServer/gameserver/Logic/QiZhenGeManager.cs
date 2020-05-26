using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 奇珍阁管理
    /// </summary>
    public class QiZhenGeManager
    {
        /// <summary>
        /// 奇珍阁线程锁
        /// </summary>
        public static Object QiZhenMutex = new object();

        /// <summary>
        /// 奇珍阁缓存项
        /// </summary>
        private static List<QiZhenGeItemData> QiZhenGeItemDataList = new List<QiZhenGeItemData>();

        /// <summary>
        /// 初始化奇珍阁缓存项
        /// </summary>
        private static void InitQiZhenGeCachingItems()
        {
            if (QiZhenGeItemDataList.Count > 0) return;

            int basePercent = 0;
            foreach (var val in GameManager.systemQiZhenGeGoodsMgr.SystemXmlItemDict.Values)
            {
                int percent = (int)(val.GetDoubleValue("Probability") * 10000);
                QiZhenGeItemDataList.Add(new QiZhenGeItemData()
                {
                    ItemID = val.GetIntValue("ID"),
                    GoodsID = val.GetIntValue("GoodsID"),
                    OrigPrice = val.GetIntValue("OrigPrice"),
                    Price = val.GetIntValue("Price"),
                    Description = val.GetStringValue("Description"),
                    BaseProbability = basePercent,
                    SelfProbability = percent,
                });

                basePercent += percent;
            }

            if (basePercent > 10000)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析奇珍阁配置项时发生概率溢出10000错误"));
            }
        }

        /// <summary>
        /// 清空初始化奇珍阁缓存项
        /// </summary>
        public static void ClearQiZhenGeCachingItems()
        {
            lock (QiZhenMutex)
            {
                QiZhenGeItemDataList.Clear();
            }
        }

        private static QiZhenGeItemData PickUpQiZhenGeItemDataByPercent(List<QiZhenGeItemData> qiZhenGeItemDataList, int randPercent)
        {
            QiZhenGeItemData qiZhenGeItemData = null;
            for (int i = 0; i < qiZhenGeItemDataList.Count; i++)
            {
                if (randPercent > qiZhenGeItemDataList[i].BaseProbability && randPercent <= (qiZhenGeItemDataList[i].BaseProbability + qiZhenGeItemDataList[i].SelfProbability))
                {
                    qiZhenGeItemData = qiZhenGeItemDataList[i];
                    break;
                }
            }

            return qiZhenGeItemData;
        }

        /// <summary>
        /// 获取随机的前4个项的字典
        /// </summary>
        public static List<QiZhenGeItemData> GetRandomQiZhenGeCachingItems(int maxNum)
        {
            List<QiZhenGeItemData> qiZhenGeItemDataList1 = null;
            lock (QiZhenMutex)
            {
                InitQiZhenGeCachingItems();
                qiZhenGeItemDataList1 = Global.RandomSortList<QiZhenGeItemData>(QiZhenGeItemDataList);

                //将随机过的重新赋值，确保第一个可以出现
                QiZhenGeItemDataList = qiZhenGeItemDataList1;
            }
            
            if (null == qiZhenGeItemDataList1) return null;

            List<QiZhenGeItemData> list = new List<QiZhenGeItemData>();
            for (int i = 0; i < maxNum; i++)
            {
                int randNum = Global.GetRandomNumber(1, 10001);
                QiZhenGeItemData qiZhenGeItemData = PickUpQiZhenGeItemDataByPercent(qiZhenGeItemDataList1, randNum);
                //if (dict.ContainsKey(qiZhenGeItemData.ItemID))
                //{
                //    continue;
                //}

                list.Add(qiZhenGeItemData);
            }

            return list;
        }

        /// <summary>
        /// 获取奇珍阁中的物品
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static List<QiZhenGeItemData> GetQiZhenGeGoodsList(GameClient client)
        {
            return GetRandomQiZhenGeCachingItems(Global.MaxNumPerRefreshQiZhenGe);
        }
    }
}
