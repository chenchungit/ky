using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 经脉项缓存管理
    /// </summary>
    public class JingMaiCacheManager
    {
        /// <summary>
        /// 经脉xml项缓存
        /// </summary>
        private static Dictionary<string, SystemXmlItem> JingMaiItemsDict = new Dictionary<string, SystemXmlItem>();

        /// <summary>
        /// 从缓存中读取经脉配置项
        /// </summary>
        /// <param name="occupation"></param>
        /// <param name="jingMaiID"></param>
        /// <param name="jingMaiLevel"></param>
        /// <returns></returns>
        public static SystemXmlItem GetJingMaiItem(int occupation, int jingMaiID, int jingMaiBodyLevel)
        {
            string key = string.Format("{0}_{1}_{2}", occupation,
                jingMaiID,
                jingMaiBodyLevel);

            SystemXmlItem systemJingMaiItem = null;
            if (!JingMaiItemsDict.TryGetValue(key, out systemJingMaiItem))
            {
                return null;
            }

            return systemJingMaiItem;
        }

        /// <summary>
        /// 根据职业加载经脉项，并进行缓存
        /// </summary>
        /// <param name="occupation"></param>
        public static void LoadJingMaiItemsByOccupation(int occupation)
        {
            string fileName = "";
            XElement xml = null;

            try
            {
                fileName = string.Format("Config/JingMais/{0}.xml", occupation);
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

            IEnumerable<XElement> JingMaiXmlItems = xml.Elements("JingMai");
            foreach (var JingMaiItem in JingMaiXmlItems)
            {
                IEnumerable<XElement> ChongItems = JingMaiItem.Elements("Chong");
                foreach (var ChongItem in ChongItems)
                {
                    SystemXmlItem systemXmlItem = new SystemXmlItem()
                    {
                        XMLNode = ChongItem,
                    };

                    string key = string.Format("{0}_{1}_{2}", occupation,
                        (int)Global.GetSafeAttributeLong(JingMaiItem, "ID"),
                        (int)Global.GetSafeAttributeLong(ChongItem, "ID"));

                    JingMaiItemsDict[key] = systemXmlItem;
                }
            }
        }

        /// <summary>
        /// 加载经脉项到缓存中
        /// </summary>
        public static void LoadJingMaiItems()
        {
            for (int i = 0; i < 3; i++) // 标记一下，策划说这个现在没有用到，不知道以后会不会因新职业魔剑而改变 [XSea 2015/4/16]
            {
                LoadJingMaiItemsByOccupation(i);
            }
        }
    }
}
