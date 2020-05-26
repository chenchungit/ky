using GameServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 配置解析器
    /// </summary>
    public static class ConfigParser
    {
        /// <summary>
        /// 属性名和属性类型对应字典
        /// </summary>
        static Dictionary<string, ExtPropIndexes> ExtPropName2ExtPropIndexDict = new Dictionary<string, ExtPropIndexes>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static ConfigParser()
        {
            for (ExtPropIndexes i = (ExtPropIndexes)0; i < ExtPropIndexes.Max; i++)
            {
                ExtPropName2ExtPropIndexDict[i.ToString()] = i;
            }
        }

        /// <summary>
        /// 根据属性名查找对应的属性类型(索引)
        /// </summary>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static ExtPropIndexes GetPropIndexByPropName(string propName)
        {
            ExtPropIndexes propIndex;
            if (ExtPropName2ExtPropIndexDict.TryGetValue(propName.Trim(), out propIndex))
            {
                return propIndex;
            }

            return ExtPropIndexes.Max;
        }

        /// <summary>
        /// 解析两个整数
        /// </summary>
        /// <param name="str"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="splitChar"></param>
        /// <returns></returns>
        public static bool ParseStrInt2(string str, ref int v1, ref int v2, char splitChar = ',')
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            string[] strArray = str.Split(splitChar);
            int t1, t2;
            if (strArray.Length < 2 || !int.TryParse(strArray[0], out t1) || !int.TryParse(strArray[1], out t2))
            {
                return false;
            }

            v1 = t1;
            v2 = t2;

            return true;
        }

        /// <summary>
        /// 解析三个整数
        /// </summary>
        /// <param name="str"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="splitChar"></param>
        /// <returns></returns>
        public static bool ParseStrInt3(string str, ref int v1, ref int v2, ref int v3, char splitChar = ',')
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            string[] strArray = str.Split(splitChar);
            int t1, t2, t3;
            if (strArray.Length < 3 || !int.TryParse(strArray[0], out t1) || !int.TryParse(strArray[1], out t2) || !int.TryParse(strArray[2], out t3))
            {
                return false;
            }

            v1 = t1;
            v2 = t2;
            v3 = t3;

            return true;
        }

        /// <summary>
        /// 解析时间区间配置
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool ParserTimeRangeList(List<TimeSpan> list, string str, bool clear = true, char splitChar1 = '|', char splitChar2 = '-')
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            if (clear)
            {
                list.Clear();
            }

            string[] rangeStr = str.Split(splitChar1);

            foreach (var rangeItem in rangeStr)
            {
                string[] timeStr = rangeItem.Split(splitChar2);
                if (timeStr.Length != 2)
                {
                    return false;
                }

                TimeSpan time1, time2;
                if (!TimeSpan.TryParse(timeStr[0], out time1) || !TimeSpan.TryParse(timeStr[1], out time2))
                {
                    return false;
                }

                list.Add(time1);
                list.Add(time2);
            }

            return list.Count > 0;
        }

        /// <summary>
        /// 解析时间区间配置
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool ParserTimeRangeListWithDay(List<TimeSpan> list, string str, bool clear = true, char splitChar1 = '|', char splitChar2 = '-', char splitChar3 = ',')
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            if (clear)
            {
                list.Clear();
            }

            string[] rangeStr = str.Split(splitChar1);

            foreach (var rangeItem in rangeStr)
            {
                string[] dayAndTimeStr = rangeItem.Split(splitChar3);
                if (dayAndTimeStr.Length != 2)
                {
                    return false;
                }

                int day;
                if (!int.TryParse(dayAndTimeStr[0], out day))
                {
                    return false;
                }

                string[] timeStr = dayAndTimeStr[1].Split(splitChar2);
                if (timeStr.Length != 2)
                {
                    return false;
                }

                TimeSpan time1, time2;
                if (!TimeSpan.TryParse(timeStr[0], out time1) || !TimeSpan.TryParse(timeStr[1], out time2))
                {
                    return false;
                }

                TimeSpan dayPart = new TimeSpan(day, 0, 0, 0);
                time1 = time1.Add(dayPart);
                time2 = time2.Add(dayPart);
                list.Add(time1);
                list.Add(time2);
            }

            return list.Count > 0;
        }

        /// <summary>
        /// 解析时间区间配置
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<List<int>> ParserIntArrayList(string str, bool verifyColumn = true, char splitChar1 = '|', char splitChar2 = ',')
        {
            List<List<int>> list = new List<List<int>>();
            if (string.IsNullOrEmpty(str))
            {
                return list;
            }

            string[] rangeStr = str.Split(splitChar1);

            int maxColumnCount = -1;
            foreach (var rangeItem in rangeStr)
            {
                List<int> ls = new List<int>();
                if (!string.IsNullOrEmpty(rangeItem))
                {
                    string[] arr = rangeItem.Split(splitChar2);
                    foreach (var s in arr)
                    {
                        int v;
                        if (int.TryParse(s, out v))
                        {
                            ls.Add(v);
                        }
                    }
                }

                list.Add(ls);

                //验证列数是否一致
                if (verifyColumn)
                {
                    if (maxColumnCount < 0)
                    {
                        maxColumnCount = ls.Count;
                        if (maxColumnCount == 0)
                        {
                            break;
                        }
                    }
                    else if (maxColumnCount != ls.Count)
                    {
                        list.Clear();
                        break;
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 解析通用的扩展属性加成配置,目前需要扩展属性
        /// </summary>
        /// <param name="str"></param>
        /// <param name="verifyColumn"></param>
        /// <param name="splitChar1"></param>
        /// <param name="splitChar2"></param>
        public static EquipPropItem ParseEquipPropItem(string str, bool verifyColumn = true, char splitChar1 = '|', char splitChar2 = ',', char splitChar3 = '-')
        {
            EquipPropItem equipPropItem = new EquipPropItem();

            if (!string.IsNullOrEmpty(str))
            {
                string[] propertyConfigArray = str.Split(splitChar1);
                foreach (var propertyConfigItem in propertyConfigArray)
                {
                    string[] nameValueArray = propertyConfigItem.Split(splitChar2);
                    if (nameValueArray.Length == 2)
                    {
                        ExtPropIndexes propIndex = ConfigParser.GetPropIndexByPropName(nameValueArray[0]);
                        if (propIndex < ExtPropIndexes.Max)
                        {
                            double propValue;
                            if (double.TryParse(nameValueArray[1], out propValue))
                            {
                                equipPropItem.ExtProps[(int)propIndex] = propValue;
                            }
                        }
                        else
                        {
                            //其他特殊配置类型
                            int propIndex0 = -1;
                            int propIndex1 = -1;
                            switch (nameValueArray[0])
                            {
                                case "Attack":
                                    {
                                        propIndex0 = (int)ExtPropIndexes.MinAttack;
                                        propIndex1 = (int)ExtPropIndexes.MaxAttack;
                                    }
                                    break;
                                case "Mattack":
                                    {
                                        propIndex0 = (int)ExtPropIndexes.MinMAttack;
                                        propIndex1 = (int)ExtPropIndexes.MaxMAttack;
                                    }
                                    break;
                                case "Defense":
                                    {
                                        propIndex0 = (int)ExtPropIndexes.MinDefense;
                                        propIndex1 = (int)ExtPropIndexes.MaxDefense;
                                    }
                                    break;
                                case "Mdefense":
                                    {
                                        propIndex0 = (int)ExtPropIndexes.MinMDefense;
                                        propIndex1 = (int)ExtPropIndexes.MaxMDefense;
                                    }
                                    break;
                            }

                            string[] valueArray = nameValueArray[1].Split(splitChar3);
                            double propValue;
                            if (propIndex0 >= 0 && double.TryParse(valueArray[0], out propValue))
                            {
                                equipPropItem.ExtProps[propIndex0] = propValue;
                            }

                            if (propIndex1 >= 0 && double.TryParse(valueArray[1], out propValue))
                            {
                                equipPropItem.ExtProps[propIndex1] = propValue;
                            }
                        }
                    }
                }
            }

            return equipPropItem;
        }

        /// <summary>
        /// 解析奖励物品列表
        /// </summary>
        /// <param name="str"></param>
        /// <param name="awardsItemList"></param>
        /// <param name="splitChar1"></param>
        /// <param name="splitChar2"></param>
        /// <returns></returns>
        public static void ParseAwardsItemList(string str, ref AwardsItemList awardsItemList, char splitChar1 = '|', char splitChar2 = ',')
        {
            awardsItemList.Add(str);
        }
    }
}
