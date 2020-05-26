using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 天赋类型
    /// </summary>
    public enum TalentType
    {
        /// <summary>
        /// 野蛮
        /// </summary>
        Savage = 1,

        /// <summary>
        /// 坚韧
        /// </summary>
        Tough = 2,

        /// <summary>
        /// 机敏
        /// </summary>
        Quick = 3,
    }

    /// <summary>
    /// 洗点类型
    /// </summary>
    public enum TalentWashType
    {
        /// <summary>
        /// 钻石
        /// </summary>
        Diamond = 0,

        /// <summary>
        /// 点券
        /// </summary>
        Goods = 1,
    }

    /// <summary>
    /// 天赋信息
    /// </summary>
    public class TalentInfo
    {
        /// <summary>
        /// 天赋id
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// 天赋类型
        /// </summary>
        public int Type = 0;

        /// <summary>
        /// 天赋名称
        /// </summary>
        public string Name = "";

        /// <summary>
        /// 需要天赋点数
        /// </summary>
        public int NeedTalentCount = 0;

        /// <summary>
        /// 前置开启天赋
        /// </summary>
        public int NeedTalentID = 0;

        /// <summary>
        /// 前置开启天赋等级
        /// </summary>
        public int NeedTalentLevel = 0;

        /// <summary>
        /// 最高等级
        /// </summary>
        public int LevelMax = 0;

        /// <summary>
        /// 效果类型（TalentEffectType）
        /// </summary>
        public int EffectType = 0;

        /// <summary>
        /// 效果（等级，效果）
        /// </summary>
        public Dictionary<int, List<TalentEffectInfo>> EffectList = null;
    }
}
