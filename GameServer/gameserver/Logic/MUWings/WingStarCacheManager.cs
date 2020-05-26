using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;

namespace GameServer.Logic.MUWings
{
    /// <summary>
    /// 翅膀升星的配置缓存
    /// </summary>
    public class WingStarCacheManager
    {
        /// <summary>
        /// 翅膀升星项缓存
        /// </summary>
        private static Dictionary<string, SystemXmlItem> WingStarItemsDict = new Dictionary<string, SystemXmlItem>();

        /// <summary>
        /// 从缓存中读取配置项
        /// </summary>
        /// <param name="occupation"></param>
        /// <param name="jingMaiID"></param>
        /// <param name="jingMaiLevel"></param>
        /// <returns></returns>
        public static SystemXmlItem GetWingStarCacheItem(int occupation, int level, int starNum)
        {
            string key = string.Format("{0}_{1}_{2}", occupation, level,
                starNum);

            SystemXmlItem systemWingStarCacheItem = null;
            if (!WingStarItemsDict.TryGetValue(key, out systemWingStarCacheItem))
            {
                return null;
            }

            return systemWingStarCacheItem;
        }

        /// <summary>
        /// 根据职业加载技能项，并进行缓存
        /// </summary>
        /// <param name="occupation"></param>
        public static void LoadWingStarItemsByOccupation(int occupation)
        {
            string fileName = "";
            XElement xml = null;

            try
            {
                fileName = string.Format("Config/Wing/WingStar_{0}.xml", occupation);
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

            IEnumerable<XElement> WingStarXmlItems = xml.Elements("Wing");
            foreach (var WingStarItem in WingStarXmlItems)
            {
                IEnumerable<XElement> items = WingStarItem.Elements("Item");
                foreach (var item in items)
                {
                    SystemXmlItem systemXmlItem = new SystemXmlItem()
                    {
                        XMLNode = item,
                    };

                    string key = string.Format("{0}_{1}_{2}", occupation,
                        (int)Global.GetSafeAttributeLong(WingStarItem, "ID"),
                        (int)Global.GetSafeAttributeLong(item, "Star"));

                    WingStarItemsDict[key] = systemXmlItem;
                }
            }
        }

        /// <summary>
        /// 加载翅膀升星项配置到缓存中
        /// </summary>
        public static void LoadWingStarItems()
        {
            for (int i = (int)EOccupationType.EOT_Warrior; i < (int)EOccupationType.EOT_MAX; i++) // 新增魔剑士翅膀升星配置 [XSea 2015/4/16]
            {
                LoadWingStarItemsByOccupation(i);
            }
        }
    }
}
