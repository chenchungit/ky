using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 梅林魔法书升星配置 [XSea 2015/6/18]
    /// </summary>
    public class MerlinStarUpConfigData
    {
        /// <summary>
        /// 阶数
        /// </summary>
        public int _Level;

        /// <summary>
        /// 星数
        /// </summary>
        public int _StarNum;

        /// <summary>
        /// 最小物攻
        /// </summary>
        public int _MinAttackV;

        /// <summary>
        /// 最大物攻
        /// </summary>
        public int _MaxAttackV;

        /// <summary>
        /// 最小魔攻
        /// </summary>
        public int _MinMAttackV;

        /// <summary>
        /// 最大魔攻
        /// </summary>
        public int _MaxMAttackV;

        /// <summary>
        /// 最小物防
        /// </summary>
        public int _MinDefenseV;

        /// <summary>
        /// 最大物防
        /// </summary>
        public int _MaxDefenseV;

        /// <summary>
        /// 最小魔防
        /// </summary>
        public int _MinMDefenseV;

        /// <summary>
        /// 最大魔防
        /// </summary>
        public int _MaxMDefenseV;

        /// <summary>
        /// 命中
        /// </summary>
        public int _HitV;

        /// <summary>
        /// 闪避
        /// </summary>
        public int _DodgeV;

        /// <summary>
        /// 生命上限
        /// </summary>
        public int _MaxHpV;

        /// <summary>
        /// 重生几率
        /// </summary>
        public double _ReviveP;

        /// <summary>
        /// 魔法完全恢复几率
        /// </summary>
        public double _MpRecoverP;

        /// <summary>
        /// 消耗物品
        /// </summary>
        public int _NeedGoodsID;

        /// <summary>
        /// 消耗物品个数
        /// </summary>
        public int _NeedGoodsCount;

        /// <summary>
        /// 消耗钻石(顶替消耗物品)
        /// </summary>
        public int _NeedDiamond;

        /// <summary>
        /// 升星所需经验
        /// </summary>
        public int _NeedExp;

        /// <summary>
        /// 增加经验 下标0=普通，1=暴击
        /// </summary>
        public int[] _AddExp;

        /// <summary>
        /// 暴击几率 0=普通几率，1=暴击几率
        /// </summary>
        public double[] _CritPercent;
    }
}
