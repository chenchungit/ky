using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;

namespace GameServer.Logic.MUWings
{
    /// <summary>
    /// 翅膀基础属性配置缓存（因为分直接，所以在这里管理下)
    /// </summary>
    public class WingPropsCacheManager
    {
        /// <summary>
        /// 翅膀缓存项缓存
        /// </summary>
        private static Dictionary<string, SystemXmlItem> WingPropsItemsDict = new Dictionary<string, SystemXmlItem>();

        /// <summary>
        /// 从缓存中读取配置项
        /// </summary>
        /// <param name="occupation"></param>
        /// <param name="jingMaiID"></param>
        /// <param name="jingMaiLevel"></param>
        /// <returns></returns>
        public static SystemXmlItem GetWingPropsCacheItem(int occupation, int level)
        {
            string key = string.Format("{0}_{1}", occupation, level);

            SystemXmlItem systemWingPropsCacheItem = null;
            if (!WingPropsItemsDict.TryGetValue(key, out systemWingPropsCacheItem))
            {
                return null;
            }

            return systemWingPropsCacheItem;
        }

        /// <summary>
        /// 根据职业加载技能项，并进行缓存
        /// </summary>
        /// <param name="occupation"></param>
        public static void LoadWingPropsItemsByOccupation(int occupation)
        {
            string fileName = "";
            XElement xml = null;

            try
            {
                fileName = string.Format("Config/Wing/Wing_{0}.xml", occupation);
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

            IEnumerable<XElement> WingPropsXmlItems = xml.Elements("Level");
            foreach (var WingPropsItem in WingPropsXmlItems)
            {
                SystemXmlItem systemXmlItem = new SystemXmlItem()
                {
                    XMLNode = WingPropsItem,
                };

                string key = string.Format("{0}_{1}", occupation,
                    (int)Global.GetSafeAttributeLong(WingPropsItem, "ID"));

                WingPropsItemsDict[key] = systemXmlItem;
            }
        }

        /// <summary>
        /// 加载翅膀缓存项配置到缓存中
        /// </summary>
        public static void LoadWingPropsItems()
        {
            for (int i = (int)EOccupationType.EOT_Warrior; i < (int)EOccupationType.EOT_MAX; i++) // 新增魔剑士翅膀升阶配置 [XSea 2015/4/16]
            {
                LoadWingPropsItemsByOccupation(i);
            }
        }
    }
}
