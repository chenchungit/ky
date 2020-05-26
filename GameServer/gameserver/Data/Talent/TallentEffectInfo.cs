using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 天赋效果类型
    /// </summary>
    public enum TalentEffectType
    {
        /// <summary>
        /// 基本属性
        /// </summary>
        PropBasic = 1,

        /// <summary>
        /// 扩展属性
        /// </summary>
        PropExt = 2,

        /// <summary>
        /// 单个技能等级
        /// </summary>
        SkillOne = 3,

        /// <summary>
        /// 分职业全部技能等级
        /// </summary>
        SkillAll = 4,
    }

    /// <summary>
    /// 天赋——效果状态
    /// </summary>
    public enum TalentEffectState
    {
        /// <summary>
        /// 未激活
        /// </summary>
        NoOpen = 0,

        /// <summary>
        /// 已激活，未点天赋
        /// </summary>
        Open = 1,

        /// <summary>
        /// 已激活，未点满
        /// </summary>
        Half = 2,

        /// <summary>
        /// 已激活，已点满
        /// </summary>
        All = 3,

    }

    
}
