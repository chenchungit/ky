using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 天赋附加属性
    /// </summary>
    public class TalentSpecialInfo
    {
        /// <summary>
        /// 天赋类型
        /// </summary>
        public int SpecialType = 0;

        /// <summary>
        /// 天赋点数
        /// </summary>
        public int SingleCount = 0;

        /// <summary>
        /// 效果列表
        /// </summary>
        public Dictionary<int, double> EffectList = null;
    }
}
