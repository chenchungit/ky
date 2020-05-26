using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    public class QiangHuaFuJiaItem
    {
        public int Id;
        public int Level;
        public int Num;
        public double MaxLifePercent;
        public double AddAttackInjurePercent;
    }

    /// <summary>
    /// 全套属性加成管理项
    /// </summary>
    public class AllThingsCalcItem
    {
        /// <summary>
        /// 紫色的装备的个数
        /// </summary>
        public int TotalPurpleQualityNum = 0;

        /// <summary>
        /// 金色的装备的个数
        /// </summary>
        public int TotalGoldQualityNum = 0;

        /// <summary>
        /// 锻造到+5的装备的个数
        /// </summary>
        public int TotalForge5LevelNum = 0;

        /// <summary>
        /// 锻造到+7的装备的个数
        /// </summary>
        public int TotalForge7LevelNum = 0;

        /// <summary>
        /// 锻造到+9的装备的个数
        /// </summary>
        public int TotalForge9LevelNum = 0;

        /// <summary>
        /// 锻造到+11的装备的个数
        /// </summary>
        public int TotalForge11LevelNum = 0;

        /// <summary>
        /// 锻造到+13的装备的个数
        /// </summary>
        public int TotalForge13LevelNum = 0;

        /// <summary>
        /// 锻造到+15的装备的个数
        /// </summary>
        public int TotalForge15LevelNum = 0;

        /// <summary>
        /// 4级宝石的个数
        /// </summary>
        public int TotalJewel4LevelNum = 0;

        /// <summary>
        /// 5级宝石的个数
        /// </summary>
        public int TotalJewel5LevelNum = 0;

        /// <summary>
        /// 6级宝石的个数
        /// </summary>
        public int TotalJewel6LevelNum = 0;

        /// <summary>
        /// 7级宝石的个数
        /// </summary>
        public int TotalJewel7LevelNum = 0;

        /// <summary>
        /// 8级宝石的个数
        /// </summary>
        public int TotalJewel8LevelNum = 0;

        /// <summary>
        /// 绿色卓越装备个数
        /// </summary>
        public int TotalGreenZhuoYueNum = 0;

        /// <summary>
        /// 蓝色卓越装备个数
        /// </summary>
        public int TotalBlueZhuoYueNum = 0;
       
        /// <summary>
        /// 紫色卓越装备个数
        /// </summary>
        public int TotalPurpleZhuoYueNum = 0;

        /// <summary>
        /// 强化等级累加数量字典
        /// </summary>
        public Dictionary<int, int> TotalForgeLevelAccDict = new Dictionary<int, int>();

        public static List<QiangHuaFuJiaItem> QiangHuaFuJiaItemList = new List<QiangHuaFuJiaItem>();

        /// <summary>
        /// 初始化套装强化加成信息
        /// </summary>
        public static void InitAllForgeLevelInfo()
        {
            lock (QiangHuaFuJiaItemList)
            {
                SystemXmlItems xmlitems = new SystemXmlItems();
                xmlitems.LoadFromXMlFile("Config/QiangHuaFuJia.xml", "", "ID");
                SystemXmlItem item = null;
                QiangHuaFuJiaItemList.Clear();
                foreach (var kv in xmlitems.SystemXmlItemDict)
                {
                    item = kv.Value;

                    QiangHuaFuJiaItem qiangHuaFuJiaItem = new QiangHuaFuJiaItem();
                    qiangHuaFuJiaItem.Id = item.GetIntValue("ID");
                    qiangHuaFuJiaItem.Level = item.GetIntValue("QiangHuaLevel");
                    qiangHuaFuJiaItem.Num = item.GetIntValue("Num");
                    qiangHuaFuJiaItem.AddAttackInjurePercent = item.GetDoubleValue("AddAttackInjurePercent");
                    qiangHuaFuJiaItem.MaxLifePercent = item.GetDoubleValue("MaxLifePercent");
                    QiangHuaFuJiaItemList.Add(qiangHuaFuJiaItem);
                }

                QiangHuaFuJiaItemList.Sort((x, y) => { return x.Id - y.Id; });
                for (int i = 0; i < QiangHuaFuJiaItemList.Count; i++ )
                {
                    QiangHuaFuJiaItemList[i].Id = i + 1;
                }
            }
        }

        public void ChangeTotalForgeLevel(int level, bool toAdd)
        {
            lock (TotalForgeLevelAccDict)
            {
                int num = 0;
                foreach (var item in QiangHuaFuJiaItemList)
                {
                    if (item.Level <= level)
                    {
                        if (toAdd)
                        {
                            if (TotalForgeLevelAccDict.TryGetValue(item.Level, out num))
                            {
                                TotalForgeLevelAccDict[item.Level] = num + 1;
                            }
                            else
                            {
                                TotalForgeLevelAccDict[item.Level] = 1;
                            }
                        }
                        else
                        {
                            if (TotalForgeLevelAccDict.TryGetValue(item.Level, out num))
                            {
                                TotalForgeLevelAccDict[item.Level] = num - 1;
                            }
                        }
                    }
                }
            }
        }

        public int GetTotalForgeLevelValidIndex()
        {
            lock (TotalForgeLevelAccDict)
            {
                foreach (var item in QiangHuaFuJiaItemList)
                {
                    int num;
                    if (TotalForgeLevelAccDict.TryGetValue(item.Level, out num) && item.Num <= num)
                    {
                        return item.Id;
                    }
                }
            }

            return 0;
        }

        public static QiangHuaFuJiaItem GetQiangHuaFuJiaItem(int index)
        {
            if (index >= 0)
            {
                lock (QiangHuaFuJiaItemList)
                {
                    if (index < QiangHuaFuJiaItemList.Count)
                    {
                        return QiangHuaFuJiaItemList[index];
                    }
                }
            }

            return null;
        }
    }
}
