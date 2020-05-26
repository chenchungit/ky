using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 装备进阶项缓存管理
    /// </summary>
    public class EquipUpgradeCacheMgr
    {
        /// <summary>
        /// 装备进阶项项缓存
        /// </summary>
        private static Dictionary<string, SystemXmlItem> _EquipUpgradeItemsDict = new Dictionary<string, SystemXmlItem>();

        /// <summary>
        /// 从缓存中读取装备进阶配置项
        /// </summary>
        /// <param name="occupation"></param>
        /// <param name="jingMaiID"></param>
        /// <param name="jingMaiLevel"></param>
        /// <returns></returns>
        public static SystemXmlItem GetEquipUpgradeCacheItem(int categoriy, int suitID)
        {
            if (null == _EquipUpgradeItemsDict) return null;
            string key = string.Format("{0}_{1}", categoriy, suitID);
            SystemXmlItem systemEquipUpgradeItem = null;
            if (!_EquipUpgradeItemsDict.TryGetValue(key, out systemEquipUpgradeItem))
            {
                return null;
            }

            return systemEquipUpgradeItem;
        }

        /// <summary>
        /// 根据物品ID获取装备进阶项
        /// </summary>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        public static SystemXmlItem GetEquipUpgradeItemByGoodsID(int goodsID, int maxSuiItID)
        {
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsID, out systemGoods))
            {
                return null;
            }

            int categoriy = systemGoods.GetIntValue("Categoriy");
            if (categoriy < 0 || categoriy >= (int)ItemCategories.EquipMax)
            {
                return null;
            }

            int suitID = systemGoods.GetIntValue("SuitID");
            if (suitID < 1 || suitID > maxSuiItID)
            {
                suitID = 1;
            }

            return GetEquipUpgradeCacheItem(categoriy, suitID);
        }

        /// <summary>
        /// 根据加载装备进阶能项，并进行缓存
        /// </summary>
        /// <param name="occupation"></param>
        public static void LoadEquipUpgradeItems()
        {
            string fileName = "";
            XElement xml = null;

            try
            {
                fileName = string.Format("Config/EquipUpgrade.xml");
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

            Dictionary<string, SystemXmlItem> equipUpgradeItemsDict = new Dictionary<string, SystemXmlItem>();

            IEnumerable<XElement> jiNengXmlItems = xml.Elements("Equip");
            foreach (var jiNengItem in jiNengXmlItems)
            {
                IEnumerable<XElement> items = jiNengItem.Elements("Item");
                foreach (var item in items)
                {
                    SystemXmlItem systemXmlItem = new SystemXmlItem()
                    {
                        XMLNode = item,
                    };

                    string key = string.Format("{0}_{1}",
                        (int)Global.GetSafeAttributeLong(jiNengItem, "Categoriy"),
                        (int)Global.GetSafeAttributeLong(item, "SuitID"));

                    equipUpgradeItemsDict[key] = systemXmlItem;
                }
            }

            _EquipUpgradeItemsDict = equipUpgradeItemsDict;
        }
    }
}
