using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 多段伤害时间和比例
    /// </summary>
    public class ManyTimeDmageItem
    {
        /// <summary>
        /// 伤害时间(毫秒)
        /// </summary>
        public long InjuredSeconds = 0;

        /// <summary>
        /// 伤害比例
        /// </summary>
        public double InjuredPercent = 0.0;

        /// <summary>
        /// 
        /// </summary>
        public int manyRangeIndex;
    }

    /// <summary>
    /// 技能分段伤害时间和比例缓存管理
    /// </summary>
    public static class MagicsManyTimeDmageCachingMgr
    {
        /// <summary>
        /// 分段伤害的缓存字典
        /// </summary>
        public static Dictionary<int, List<ManyTimeDmageItem>> ManyTimeDmageCachingDict = new Dictionary<int, List<ManyTimeDmageItem>>();

        /// <summary>
        /// 根据技能编号获取缓存大分段时间和比例
        /// </summary>
        /// <param name="magicCode"></param>
        /// <returns></returns>
        public static List<ManyTimeDmageItem> GetManyTimeDmageItems(int magicCode)
        {
            List<ManyTimeDmageItem> manyTimeDmageItemList = null;
            if (!ManyTimeDmageCachingDict.TryGetValue(magicCode, out manyTimeDmageItemList))
            {
                return null;
            }

            return manyTimeDmageItemList;
        }

        /// <summary>
        /// 预先进行缓存
        /// </summary>
        /// <param name="systemMagicMgr"></param>
        public static void ParseManyTimeDmageItems(SystemXmlItems systemMagicMgr)
        {
            Dictionary<int, List<ManyTimeDmageItem>> manyTimeDmageItemsDict = new Dictionary<int, List<ManyTimeDmageItem>>();
            foreach (var key in systemMagicMgr.SystemXmlItemDict.Keys)
            {
                string manyTimeDmage = (string)systemMagicMgr.SystemXmlItemDict[(int)key].GetStringValue("ManyTimeDmage");
                if (null == manyTimeDmage) continue;
                ParseMagicManyTimeDmage(manyTimeDmageItemsDict, (int)key, manyTimeDmage);
            }

            ManyTimeDmageCachingDict = manyTimeDmageItemsDict;
        }

        /// <summary>
        /// 解析分段时间和伤害
        /// </summary>
        /// <param name="id"></param>
        /// <param name="actions"></param>
        private static void ParseMagicManyTimeDmage(Dictionary<int, List<ManyTimeDmageItem>> dict, int id, string manyTimeDmage)
        {
            manyTimeDmage = manyTimeDmage.Trim();
            if (string.IsNullOrEmpty(manyTimeDmage)) return;

            List<ManyTimeDmageItem> manyTimeDmageItemsList = ParseItems(id, manyTimeDmage);
            dict[id] = manyTimeDmageItemsList;
        }

        /// <summary>
        /// 解析分段的字符串
        /// </summary>
        /// <param name="manyTimeDmage"></param>
        /// <returns></returns>
        private static List<ManyTimeDmageItem> ParseItems(int id, string manyTimeDmage)
        {
            List<ManyTimeDmageItem> manyTimeDmageItemsList = new List<ManyTimeDmageItem>();

            string[] fields = manyTimeDmage.Split('|');
            for (int i = 0; i < fields.Length; i++)
            {
                string[] fields2 = fields[i].Split(',');
                if (fields2.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("解析技能项的多段伤害配置时，个数配置错误, ID={0}", id));
                    continue;
                }

                ManyTimeDmageItem manyTimeDmageItem = new ManyTimeDmageItem()
                {
                    InjuredSeconds = Global.SafeConvertToInt32(fields2[0]),
                    InjuredPercent = Global.SafeConvertToDouble(fields2[1]),
                    manyRangeIndex = i,
                };

                manyTimeDmageItemsList.Add(manyTimeDmageItem);
            }

            return manyTimeDmageItemsList;
        }
    }
}
