using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Server.Tools;

namespace GameServer.Logic
{
    public class SingleEquipProps
    {
        #region 单件装备的属性

        /// <summary>
        /// 单件装备的属性项缓存
        /// </summary>
        private Dictionary<string, List<double[]>> _SingleEquipItemsDict = new Dictionary<string, List<double[]>>();

        /// <summary>
        /// 从缓存中读取单件装备的属性项
        /// </summary>
        /// <returns></returns>
        public List<double[]> GetSingleEquipPropsList(int occupation, int categoriy, int suitID)
        {
            if (null == _SingleEquipItemsDict) return null;
            string key = string.Format("{0}_{1}_{2}", occupation, categoriy, suitID);
            List<double[]> propsList = null;
            if (!_SingleEquipItemsDict.TryGetValue(key, out propsList))
            {
                return null;
            }

            return propsList;
        }

        /// <summary>
        /// 解析Xml项中的属性
        /// </summary>
        /// <param name="xmlItem"></param>
        /// <returns></returns>
        private List<double[]> ParseSystemXmlItem(SystemXmlItem xmlItem)
        {
            string equipProps = xmlItem.GetStringValue("EquipProps");
            if (string.IsNullOrEmpty(equipProps))
            {
                return null;
            }

            string[] fields = equipProps.Split('|');
            if (null == fields || fields.Length <= 0)
            {
                return null;
            }

            List<double[]> propsList = new List<double[]>();
            for (int i = 0; i < fields.Length; i++)
            {
                propsList.Add(ParseStringProps(fields[i]));
            }

            return propsList;
        }

        /// <summary>
        /// 解析字符串中的属性
        /// </summary>
        /// <param name="xmlItem"></param>
        /// <returns></returns>
        private double[] ParseStringProps(string props)
        {
            if (string.IsNullOrEmpty(props))
            {
                return null;
            }

            double [] doubleProps = Global.String2DoubleArray(props);
            if (null == doubleProps || doubleProps.Length != 10)
            {
                return null;
            }

            for (int i = 0; i < doubleProps.Length; i++)
            {
                doubleProps[i] = Global.GMax(0, doubleProps[i]);
            }

            return doubleProps;
        }

        /// <summary>
        /// 根据加载单件装备的属性项，并进行缓存
        /// </summary>
        /// <param name="occupation"></param>
        private void LoadEquipPropItemsByOccupation(string pathName, int occupation)
        {
            XElement xml = null;
            string fileName = "";

            try
            {
                fileName = string.Format("{0}{1}.xml", pathName, occupation);
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

            IEnumerable<XElement> xmlItems = xml.Elements("Equip");
            foreach (var xmltem in xmlItems)
            {
                IEnumerable<XElement> items = xmltem.Elements("Item");
                foreach (var item in items)
                {
                    SystemXmlItem systemXmlItem = new SystemXmlItem()
                    {
                        XMLNode = item,
                    };

                    string key = string.Format("{0}_{1}_{2}",
                        occupation,
                        (int)Global.GetSafeAttributeLong(xmltem, "Categoriy"),
                        (int)Global.GetSafeAttributeLong(item, "SuitID"));

                    _SingleEquipItemsDict[key] = ParseSystemXmlItem(systemXmlItem);
                }
            }
        }

        /// <summary>
        /// 根据加载单件装备的属性项，并进行缓存
        /// </summary>
        /// <param name="occupation"></param>
        public void LoadEquipPropItems(string pathName)
        {
            LoadEquipPropItemsByOccupation(pathName, 0);
            LoadEquipPropItemsByOccupation(pathName, 1);
            LoadEquipPropItemsByOccupation(pathName, 2);
        }

        #endregion 单件装备的属性
    }
}
