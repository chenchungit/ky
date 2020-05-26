using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
//using System.Windows.Media.Animation;
using System.Threading;
using Server.Data;
using GameServer.Server;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using System.Net.Sockets;
using HSGameEngine.Tools.AStar;
//using System.Windows.Forms;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 坐骑属性项缓存管理
    /// </summary>
    public class HorseCachingManager
    {
        /// <summary>
        /// Xml项的名称
        /// </summary>
        private static string[] XmlItemNames = { "WuGong", "WuFang", "MoGong", "MoFang", "BaoJi", "MingZong", "ShanBi", "ShengL", "MoFaL", "KangBao" };

        /// <summary>
        /// 坐骑强化xml项缓存
        /// </summary>
        private static Dictionary<string, SystemXmlItem> HorseItemsDict = new Dictionary<string, SystemXmlItem>();

        /// <summary>
        /// 从缓存中读取坐骑强化项
        /// </summary>
        /// <param name="level"></param>
        /// <param name="extIndex"></param>
        /// <returns></returns>
        public static SystemXmlItem GetHorseEnchanceItem(int level, HorseExtIndexes extIndex)
        {
            string key = string.Format("{0}_{1}", level, XmlItemNames[(int)extIndex]);
            SystemXmlItem systemHorseEnchanceItem = null;
            if (!HorseItemsDict.TryGetValue(key, out systemHorseEnchanceItem))
            {
                return null;
            }

            return systemHorseEnchanceItem;
        }

        /// <summary>
        /// 根据坐骑强化表加载, 并进行缓存
        /// </summary>
        /// <param name="occupation"></param>
        public static void LoadHorseEnchanceItems()
        {
            string fileName = "";
            XElement xml = null;

            try
            {
                fileName = string.Format("Config/Horses/HorseEnchance.xml");
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

            IEnumerable<XElement> levelXmlItems = xml.Elements("Levels");
            foreach (var levelXmlItem in levelXmlItems)
            {
                IEnumerable<XElement> propItems = levelXmlItem.Elements();
                foreach (var propItem in propItems)
                {
                    SystemXmlItem systemXmlItem = new SystemXmlItem()
                    {
                        XMLNode = propItem,
                    };

                    string key = string.Format("{0}_{1}", Global.GetSafeAttributeStr(levelXmlItem, "level"), propItem.Name);
                    HorseItemsDict[key] = systemXmlItem;
                }
            }
        }
    }
}
