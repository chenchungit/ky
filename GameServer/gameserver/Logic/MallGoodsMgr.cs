using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 商城缓存项
    /// </summary>
    public class MallGoodsCacheItem
    {
        /// <summary>
        /// 价格
        /// </summary>
        public int Price = 0;

        /// <summary>
        /// 锻造级别
        /// </summary>
        public int Forge_level;

        /// <summary>
        /// 装备的幸运值
        /// </summary>
        public int Lucky;

        // 新增物品属性 [12/13/2013 LiaoWei]
        /// <summary>
        /// 卓越信息 -- 一个32位int 每位代表一个卓越属性
        /// </summary>
        public int ExcellenceInfo;

        // 新增物品属性 [12/18/2013 LiaoWei]
        /// <summary>
        /// 追加等级
        /// </summary>
        public int AppendPropLev;
    }


    /// <summary>
    /// 商城物品管理
    /// </summary>
    public class MallGoodsMgr
    {
        /// <summary>
        /// 商城物品缓存
        /// </summary>
        private static Dictionary<int, MallGoodsCacheItem> MallGoodsCacheDict = new Dictionary<int, MallGoodsCacheItem>();

        /// <summary>
        /// 初始化商城物品价格
        /// </summary>
        public static void InitMallGoodsPriceDict()
        {
            foreach (var systemXmlItem in GameManager.systemMallMgr.SystemXmlItemDict.Values)
            {
                int goodsID = systemXmlItem.GetIntValue("GoodsID");
                int price = systemXmlItem.GetIntValue("Price");
                
                string property = systemXmlItem.GetStringValue("Property");
                if (string.IsNullOrEmpty(property))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载商城出售列表时, 物品配置属性错误，忽略。{0}", property));
                    continue;
                }

                string[] fields2 = property.Split(',');
                if (4 != fields2.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载加载商城出售列表时出售列表时, 物品配置项个数错误，忽略。{0}", systemXmlItem.GetIntValue("ID")));
                    continue;
                }

                MallGoodsCacheItem mallGoodsCacheItem = new MallGoodsCacheItem()
                {
                    Price = price,
                    Forge_level = Math.Max(0, Global.SafeConvertToInt32(fields2[0])),
                    AppendPropLev = Math.Max(0, Global.SafeConvertToInt32(fields2[1])),
                    Lucky = Math.Max(0, Global.SafeConvertToInt32(fields2[2])),
                    ExcellenceInfo = Math.Max(0, Global.SafeConvertToInt32(fields2[3])),
                };

                MallGoodsCacheDict[goodsID] = mallGoodsCacheItem;
            }

            foreach (var systemXmlItem in GameManager.systemQiZhenGeGoodsMgr.SystemXmlItemDict.Values)
            {
                int goodsID = systemXmlItem.GetIntValue("GoodsID");
                int price = systemXmlItem.GetIntValue("Price");

                string property = systemXmlItem.GetStringValue("Property");
                if (string.IsNullOrEmpty(property))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载商城出售列表时, 物品配置属性错误，忽略。{0}", property));
                    continue;
                }

                string[] fields2 = property.Split(',');
                if (4 != fields2.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载加载商城出售列表时出售列表时, 物品配置项个数错误，忽略。{0}", systemXmlItem.GetIntValue("ID")));
                    continue;
                }

                MallGoodsCacheItem mallGoodsCacheItem = new MallGoodsCacheItem()
                {
                    Price = price,
                    Forge_level = Math.Max(0, Global.SafeConvertToInt32(fields2[0])),
                    AppendPropLev = Math.Max(0, Global.SafeConvertToInt32(fields2[1])),
                    Lucky = Math.Max(0, Global.SafeConvertToInt32(fields2[2])),
                    ExcellenceInfo = Math.Max(0, Global.SafeConvertToInt32(fields2[3])),
                };

                MallGoodsCacheDict[goodsID] = mallGoodsCacheItem;
            }
        }

        /// <summary>
        /// 获取商城的配置信息
        /// </summary>
        public static MallGoodsCacheItem GetMallGoodsCacheItem(int goodsID)
        {
            MallGoodsCacheItem mallGoodsCacheItem = null;
            if (!MallGoodsCacheDict.TryGetValue(goodsID, out mallGoodsCacheItem))
            {
                return null;
            }

            return mallGoodsCacheItem;
        }
    }
}
