using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 天赋状态类型
    /// </summary>
    public class TalentResultType
    {
        /// <summary>
        /// 功能未开放
        /// </summary>
        public static int EnoOpen = -11;

        // <summary>
        /// 最高等级
        /// </summary>
        public static int EisMaxLevel = -10;

        // <summary>
        /// 前置开启点数不足
        /// </summary>
        public static int EnoOpenPreCount = -9;

        // <summary>
        /// 前置效果为未开启
        /// </summary>
        public static int EnoOpenPreEffect  = -8;

        /// <summary>
        /// 天赋不存在
        /// </summary>
        public static int EnoEffect = -7;

        /// <summary>
        /// 失败
        /// </summary>
        public static int EFail = -6;

        /// <summary>
        /// 天赋点数不足
        /// </summary>
        public static int EnoTalentCount = -5;

        /// <summary>
        /// 钻石不足
        /// </summary>
        public static int EnoDiamond = -4;

        /// <summary>
        /// 洗点券不足
        /// </summary>
        public static int EnoWash = -3;

        /// <summary>
        /// 天赋点数未开启（角色等级）
        /// </summary>
        public static int EnoOpenPoint = -2;

        /// <summary>
        /// 无可注入经验
        /// </summary>
        public static int EnoExp = -1;

        /// <summary>
        /// 默认
        /// </summary>
        public static int Defalut = 0;

        /// <summary>
        /// 成功,（经验注入未注满）
        /// </summary>
        public static int SuccessHalf = 1;

        /// <summary>
        /// 成功，（经验注入已注满，洗点成功，技能加点成功）
        /// </summary>
        public static int Success = 2;
    }
}
