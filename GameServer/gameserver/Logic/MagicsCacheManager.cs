using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Server.Tools;

namespace GameServer.Logic
{
    // 技能改造 1.静态数据增加数据项 2.增加逻辑 [3/8/2014 LiaoWei]

    /// <summary>
    /// 魔法项缓存管理
    /// </summary>
    public class MagicsCacheManager
    {
        /// <summary>
        /// 经脉xml项缓存
        /// </summary>
        private static Dictionary<string, SystemXmlItem> MagicItemsDict = new Dictionary<string, SystemXmlItem>();

        /// <summary>
        /// 从缓存中读取Magic配置项
        /// </summary>
        /// <param name="occupation"></param>
        /// <param name="jingMaiID"></param>
        /// <param name="jingMaiLevel"></param>
        /// <returns></returns>
        public static SystemXmlItem GetMagicCacheItem(int occupation, int skillID, int skillLevel)
        {
            string key = string.Format("{0}_{1}_{2}", occupation, skillID,
                skillLevel);

            SystemXmlItem systemMagicCacheItem = null;
            if (!MagicItemsDict.TryGetValue(key, out systemMagicCacheItem))
            {
                return null;
            }

            return systemMagicCacheItem;
        }

        /// <summary>
        /// 根据职业加载技能项，并进行缓存
        /// </summary>
        /// <param name="occupation"></param>
        public static void LoadMagicItemsByOccupation(int occupation)
        {
            string fileName = "";
            XElement xml = null;

            try
            {
                fileName = string.Format("Config/Magics/Magics_{0}.xml", occupation);
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

            IEnumerable<XElement> jiNengXmlItems = xml.Elements("Magic");
            foreach (var jiNengItem in jiNengXmlItems)
            {
                IEnumerable<XElement> items = jiNengItem.Elements("JiNeng");
                foreach (var item in items)
                {
                    SystemXmlItem systemXmlItem = new SystemXmlItem()
                    {
                        XMLNode = item,
                    };

                    string key = string.Format("{0}_{1}_{2}", occupation,
                        (int)Global.GetSafeAttributeLong(jiNengItem, "ID"),
                        (int)Global.GetSafeAttributeLong(item, "Level"));

                    MagicItemsDict[key] = systemXmlItem;
                }
            }
        }

        /// <summary>
        /// 加载经技能项配置到缓存中
        /// </summary>
        public static void LoadMagicItems()
        {
            for (int i = (int)EOccupationType.EOT_Warrior; i < (int)EOccupationType.EOT_MAX; i++) // 新增魔剑士技能升级配置 [XSea 2015/4/16]
            {
                LoadMagicItemsByOccupation(i);
            }
        }
    }
}
