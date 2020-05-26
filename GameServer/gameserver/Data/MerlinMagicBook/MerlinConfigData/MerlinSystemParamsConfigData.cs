using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// SystemParams中梅林魔法书的配置 [XSea 2015/6/18]
    /// </summary>
    public class MerlinSystemParamsConfigData
    {
        /// <summary>
        /// 重生内置cd 单位：秒
        /// </summary>
        public static int _ReviveCDTime;

        /// <summary>
        /// 秘语属性每项最大值
        /// </summary>
        public static int _MaxSecretAttrNum;

        /// <summary>
        /// 秘语持续时间 单位：分钟
        /// </summary>
        public static int _MaxSecretTime;

        /// <summary>
        /// 最大阶数
        /// </summary>
        public static int _MaxLevelNum;

        /// <summary>
        /// 最大星数
        /// </summary>
        public static int _MaxStarNum;
    }
}
