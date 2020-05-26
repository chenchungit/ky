using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 特殊时间段管理
    /// </summary>
    public class SpecailTimeManager
    {
        #region 基础变量

        /// <summary>
        /// 时间限制缓存
        /// </summary>
        private static Dictionary<int, DateTimeRange[]> _TimeLimitsDict = new Dictionary<int, DateTimeRange[]>();

        /// <summary>
        /// 判断翻倍时间
        /// </summary>
        private static long JugeDoulbeExperienceTicks = 0;

        /// <summary>
        /// 是否是打坐时经验和灵力翻倍时间
        /// </summary>
        private static bool IsDoulbeExperienceAndLingli = false;

        /// <summary>
        /// 是否是烤火翻倍时间
        /// </summary>
        private static bool IsDoulbeKaoHuo = false;

        #endregion 基础变量

        #region 辅助函数

        /// <summary>
        /// 根据ID获取时间限制字段
        /// </summary>
        /// <param name="systemScriptItem"></param>
        /// <returns></returns>
        private static DateTimeRange[] GetTimeLimitsByID(int timeLimitsID)
        {
            DateTimeRange[] dateTimeRangeArray = null;
            lock (_TimeLimitsDict)
            {
                if (_TimeLimitsDict.TryGetValue(timeLimitsID, out dateTimeRangeArray))
                {
                    return dateTimeRangeArray;
                }
            }

            SystemXmlItem systemSpecialTimeItem = null;
            if (!GameManager.systemSpecialTimeMgr.SystemXmlItemDict.TryGetValue(timeLimitsID, out systemSpecialTimeItem))
            {
                return null;
            }

            string timeLimits = systemSpecialTimeItem.GetStringValue("TimeLimits");
            if (string.IsNullOrEmpty(timeLimits))
            {
                return null;
            }

            dateTimeRangeArray = Global.ParseDateTimeRangeStr(timeLimits);

            lock (_TimeLimitsDict)
            {
                _TimeLimitsDict[timeLimitsID] = dateTimeRangeArray;
            }

            return dateTimeRangeArray;
        }

        /// <summary>
        /// 重置特殊时间段限制缓存
        /// </summary>
        /// <returns></returns>
        public static int ResetSpecialTimeLimits()
        {
            int ret = GameManager.systemSpecialTimeMgr.ReloadLoadFromXMlFile();

            lock (_TimeLimitsDict)
            {
                _TimeLimitsDict.Clear();
            }

            return ret;
        }

        /// <summary>
        /// 处理是否是翻倍的时间
        /// </summary>
        public static void ProcessDoulbeExperience()
        {
            DateTime dateTime = TimeUtil.NowDateTime();
            if (dateTime.Ticks - SpecailTimeManager.JugeDoulbeExperienceTicks < (5L * 1000L * 10000L))
            {
                return;
            }

            //限制5秒钟判断一次
            SpecailTimeManager.JugeDoulbeExperienceTicks = dateTime.Ticks;

            //当前是否在双倍经验和双倍灵力时间
            SpecailTimeManager.IsDoulbeExperienceAndLingli = SpecailTimeManager.InDoubleExperienceAndLingLiTimeRange(dateTime);

            //是否是烤火翻倍时间
            SpecailTimeManager.IsDoulbeKaoHuo = SpecailTimeManager.InDoubleKaoHuoTimeRange(dateTime);
        }

        #endregion 辅助函数

        #region 打坐时经验和灵力翻倍

        /// <summary>
        /// 当前是否在双倍经验和双倍灵力时间
        /// </summary>
        /// <returns></returns>
        private static bool InDoubleExperienceAndLingLiTimeRange(DateTime dateTime)
        {
            //根据ID获取时间限制字段
            DateTimeRange[] dateTimeRangeArray = GetTimeLimitsByID((int)SpecialTimeIDs.DoubleExpAndLingLi);
            if (null == dateTimeRangeArray)
            {
                return false;
            }

            int endMinute = 0;
            if (!Global.JugeDateTimeInTimeRange(dateTime, dateTimeRangeArray, out endMinute))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断是否是打坐经验和灵力翻倍的时间
        /// </summary>
        /// <returns></returns>
        public static bool JugeIsDoulbeExperienceAndLingli()
        {
            //当前是否在双倍经验和双倍灵力时间
            return SpecailTimeManager.IsDoulbeExperienceAndLingli;
        }

        #endregion 打坐时经验和灵力翻倍

        #region 烤火双倍时间

        /// <summary>
        /// 当前是否在双倍烤火时间
        /// </summary>
        /// <returns></returns>
        private static bool InDoubleKaoHuoTimeRange(DateTime dateTime)
        {
            //根据ID获取时间限制字段
            DateTimeRange[] dateTimeRangeArray = GetTimeLimitsByID((int)SpecialTimeIDs.KaoHuo);
            if (null == dateTimeRangeArray)
            {
                return false;
            }

            int endMinute = 0;
            if (!Global.JugeDateTimeInTimeRange(dateTime, dateTimeRangeArray, out endMinute))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断是否是烤火翻倍时间
        /// </summary>
        /// <returns></returns>
        public static bool JugeIsDoulbeKaoHuo()
        {
            //当前是否在双倍烤火时间
            return SpecailTimeManager.IsDoulbeKaoHuo;
        }

        #endregion 烤火双倍时间

        public static bool InSpercailTime(int spercailid)
        {
            DateTime dateTime = TimeUtil.NowDateTime();
            DateTimeRange[] dateTimeRangeArray = SpecailTimeManager.GetTimeLimitsByID(spercailid);
            if (null == dateTimeRangeArray)
                return false;

            int endMinute = 0;
            if (!Global.JugeDateTimeInTimeRange(dateTime, dateTimeRangeArray, out endMinute))
            {
                return false;
            }

            return true;
        }
    }
}
