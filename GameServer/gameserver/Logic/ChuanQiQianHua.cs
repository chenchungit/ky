using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using System.Xml.Linq;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 单件装备的属性加成项
    /// </summary>
    public class ChuanQiQianHuaItem
    {
        /// <summary>
        /// 强化等级
        /// </summary>
        public int QianHuaLevel = 0;

        /// <summary>
        /// 属性索引
        /// </summary>
        public int PropIndex = -1;

        /// <summary>
        /// 值
        /// </summary>
        public double ItemValue = 0.0;
    }

    /// <summary>
    /// 单件装备的属性加成(传奇版本)
    /// </summary>
    public class ChuanQiQianHua
    {
        #region 缓存的属性列表

        /// <summary>
        /// 缓存字典
        /// </summary>
        public static Dictionary<int, List<ChuanQiQianHuaItem>> QianHuaItemDict = null;

        /// <summary>
        /// 根据强化ID定位强化附加属性列表
        /// </summary>
        /// <param name="qianHuaID"></param>
        /// <returns></returns>
        public static List<ChuanQiQianHuaItem> GetListChuanQiQianHuaItem(int qianHuaID)
        {
            List<ChuanQiQianHuaItem> list = null;
            Dictionary<int, List<ChuanQiQianHuaItem>> dict = QianHuaItemDict;
            lock (dict)
            {
                dict.TryGetValue(qianHuaID, out list);
            }

            return list;
        }

        #endregion 缓存的属性列表

        #region 加载缓存的属性列表

        /// <summary>
        /// 加载缓存的属性列表
        /// </summary>
        public static void LoadEquipQianHuaProps()
        {
            XElement xml = null;
            string fileName = "";

            try
            {
                fileName = string.Format("Config/QiangHua.xml");
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

            Dictionary<int, List<ChuanQiQianHuaItem>> dict = new Dictionary<int, List<ChuanQiQianHuaItem>>();
            IEnumerable<XElement> xmlItems = xml.Elements("QiangHua");
            foreach (var xmltem in xmlItems)
            {
                SystemXmlItem systemXmlItem = new SystemXmlItem()
                {
                    XMLNode = xmltem,
                };

                int id = systemXmlItem.GetIntValue("ID");
                dict[id] = ParseSystemXmlItem(systemXmlItem);
            }

            QianHuaItemDict = dict;
        }

        /// <summary>
        /// 解析xml配置项
        /// </summary>
        /// <param name="systemXmlItem"></param>
        private static List<ChuanQiQianHuaItem> ParseSystemXmlItem(SystemXmlItem systemXmlItem)
        {
            List<ChuanQiQianHuaItem> list = new List<ChuanQiQianHuaItem>();
            string qianHua = systemXmlItem.GetStringValue("QiangHua");
            if (string.IsNullOrEmpty(qianHua))
            {
                return list;
            }

            string[] qianHuaFields = qianHua.Split('|');
            for (int i = 0; i < qianHuaFields.Length; i++)
            {
                list.AddRange(ParseChuanQiQianHuaItem((int)systemXmlItem.GetIntValue("ID"), qianHuaFields[i]));
            }

            return list;
        }

        /// <summary>
        /// 属性名称到属性索引的映射字典
        /// </summary>
        public static Dictionary<string, int> StrToExtPropIndexDict = new Dictionary<string, int>();

        /// <summary>
        /// 根据文本文件名称来转成装备的属性索引名称
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetExtPropIndexeFromStr(string str)
        {
            int index = -1;
            lock (StrToExtPropIndexDict)
            {
                if (StrToExtPropIndexDict.TryGetValue(str, out index))
                {
                    return index;
                }
            }

            for (int i = 0; i < (int)ExtPropIndexes.Max; i++)
            {
                string name = string.Format("{0}", (ExtPropIndexes)i);
                if (name == str)
                {
                    index = i;
                    break;
                }
            }

            lock (StrToExtPropIndexDict)
            {
                StrToExtPropIndexDict[str] = index;
            }

            return index;
        }

        /// <summary>
        /// 解析强化配置项
        /// </summary>
        /// <param name="systemXmlItem"></param>
        private static List<ChuanQiQianHuaItem> ParseChuanQiQianHuaItem(int qianHuaID, string strValue)
        {
            List<ChuanQiQianHuaItem> list = new List<ChuanQiQianHuaItem>();
            //if (string.IsNullOrEmpty(strValue))
            {
                return list;
            }

            string[] fields = strValue.Split(',');
            if (fields.Length != 3)
            {
                return list;
            }

            string[] fields2 = fields[2].Split('-');
            if (1 == fields2.Length)
            {
                int propIndex = GetExtPropIndexeFromStr(fields[1]);
                if (propIndex < 0)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("解析附加项出错, QianHuaID={0}, ItemStr={1}", qianHuaID, strValue));
                    throw new Exception(string.Format("解析附加项出错, QianHuaID={0}, ItemStr={1}", qianHuaID, strValue));
                }

                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = propIndex,
                    ItemValue = Global.SafeConvertToDouble(fields2[0]),
                });

                return list;
            }

            if ("Attack" == fields[1])
            {
                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MinAttack,
                    ItemValue = Global.SafeConvertToInt32(fields2[0]),
                });

                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MaxAttack,
                    ItemValue = Global.SafeConvertToInt32(fields2[1]),
                });
            }
            else if ("Mattack" == fields[1])
            {
                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MinMAttack,
                    ItemValue = Global.SafeConvertToInt32(fields2[0]),
                });

                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MaxMAttack,
                    ItemValue = Global.SafeConvertToInt32(fields2[1]),
                });
            }
            /*else if ("DSAttack" == fields[1]) // 属性改造[8/15/2013 LiaoWei]
            {
                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MinDSAttack,
                    ItemValue = Global.SafeConvertToInt32(fields2[0]),
                });

                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MaxDSAttack,
                    ItemValue = Global.SafeConvertToInt32(fields2[1]),
                });
            }*/
            else if ("Defense" == fields[1])
            {
                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MinDefense,
                    ItemValue = Global.SafeConvertToInt32(fields2[0]),
                });

                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MaxDefense,
                    ItemValue = Global.SafeConvertToInt32(fields2[1]),
                });
            }
            else if ("Mdefense" == fields[1])
            {
                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MinMDefense,
                    ItemValue = Global.SafeConvertToInt32(fields2[0]),
                });

                list.Add(new ChuanQiQianHuaItem()
                {
                    QianHuaLevel = Global.SafeConvertToInt32(fields[0]),
                    PropIndex = (int)ExtPropIndexes.MaxMDefense,
                    ItemValue = Global.SafeConvertToInt32(fields2[1]),
                });
            }
            else
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析附加项出错, QianHuaID={0}, ItemStr={1}, 未知属性项", qianHuaID, strValue));
                throw new Exception(string.Format("解析附加项出错, QianHuaID={0}, ItemStr={1}, 未知属性项", qianHuaID, strValue));
            }

            return list;
        }

        #endregion 加载缓存的属性列表

        #region 应用属性到装备上

        /// <summary>
        /// 将装备的强化属性应用到装备上
        /// </summary>
        /// <param name="equipProps"></param>
        /// <param name="occupation"></param>
        /// <param name="goodsData"></param>
        /// <param name="systemGoods"></param>
        /// <param name="toAdd"></param>
        public static void ApplayEquipQianHuaProps(double[] equipProps, int occupation, GoodsData goodsData, SystemXmlItem systemGoods, bool toAdd)
        {
            List<MagicActionItem> magicActionItemList = null;
            if (!GameManager.SystemMagicActionMgr.GoodsActionsDict.TryGetValue(goodsData.GoodsID, out magicActionItemList) || null == magicActionItemList)
            {
                //物品没有配置脚本
                return;
            }

            if (magicActionItemList.Count <= 0)
            {
                return; //如果公式为空
            }

            if (magicActionItemList[0].MagicActionID != MagicActionIDs.DB_ADD_YINYONG)
            {
                return;
            }

            if (magicActionItemList[0].MagicActionParams.Length != 2)
            {
                return;
            }

            int qianHuaID = (int)magicActionItemList[0].MagicActionParams[0];
            List<ChuanQiQianHuaItem> list = GetListChuanQiQianHuaItem(qianHuaID);
            if (null == list || list.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].QianHuaLevel > goodsData.Forge_level)
                {
                    continue;
                }

                if (toAdd)
                {
                    equipProps[list[i].PropIndex] += list[i].ItemValue;
                }
                else
                {
                    equipProps[list[i].PropIndex] -= list[i].ItemValue;
                }
            }
        }

        #endregion 应用属性到装备上
    }
}
