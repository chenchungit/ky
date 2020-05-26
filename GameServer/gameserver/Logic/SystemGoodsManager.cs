using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 物品名字索引管理
    /// </summary>
    public class SystemGoodsManager
    {
        #region 物品名字索引管理

        private Dictionary<string, SystemXmlItem> _GoodsItemsDict = null;

        /// <summary>
        /// 快速索引词典对象
        /// </summary>
        public Dictionary<string, SystemXmlItem> GoodsItemsDict
        {
            get { return _GoodsItemsDict; }
        }

        /// <summary>
        /// 物品名字索引管理
        /// </summary>
        /// <param name="systemMagicMgr"></param>
        public void LoadGoodsItemsDict(SystemXmlItems systemGoodsMgr)
        {
            Dictionary<string, SystemXmlItem> goodsItemsDict = new Dictionary<string, SystemXmlItem>();
            foreach (var key in systemGoodsMgr.SystemXmlItemDict.Keys)
            {
                SystemXmlItem systemGoods = systemGoodsMgr.SystemXmlItemDict[key];

                string strKey = systemGoods.GetStringValue("Title");
                goodsItemsDict[strKey] = systemGoods;
            }

            _GoodsItemsDict = goodsItemsDict;
        }

        #endregion 物品名字索引管理
    }
}
