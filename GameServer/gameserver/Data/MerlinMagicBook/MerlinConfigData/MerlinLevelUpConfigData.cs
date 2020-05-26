using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 梅林魔法书升阶配置 [XSea 2015/6/19]
    /// </summary>
    public class MerlinLevelUpConfigData
    {
        /// <summary>
        /// 阶数
        /// </summary>
        public int _Level;

        /// <summary>
        /// 幸运点
        /// </summary>
        public int _LuckyOne;

        /// <summary>
        /// 进入进行概率计算的幸运点
        /// </summary>
        public int _LuckyTwo;

        /// <summary>
        /// 升阶概率
        /// </summary>
        public double _Rate;

        /// <summary>
        /// 升阶所需物品id
        /// </summary>
        public int _NeedGoodsID;

        /// <summary>
        /// 升阶所需物品数量
        /// </summary>
        public int _NeedGoodsCount;

        /// <summary>
        /// 升阶所需顶替物品的钻石数量
        /// </summary>
        public int _NeedDiamond;
    }
}
