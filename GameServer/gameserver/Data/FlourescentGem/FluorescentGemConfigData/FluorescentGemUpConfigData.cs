using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 荧光宝石升级表 [XSea 2015/8/11]
    /// </summary>
    public class FluorescentGemUpConfigData
    {
        /// <summary>
        /// 元素类型
        /// </summary>
        public int _ElementsType;

        /// <summary>
        /// 宝石类型
        /// </summary>
        public int _GemType;

        /// <summary>
        /// 宝石等级
        /// </summary>
        public int _Level;

        /// <summary>
        /// 上一级宝石id
        /// </summary>
        public int _OldGoodsID;

        /// <summary>
        /// 下一级宝石id
        /// </summary>
        public int _NewGoodsID;

        /// <summary>
        /// 所需上一级宝石数量
        /// </summary>
        public int _NeedOldGoodsCount;

        /// <summary>
        /// 所需1级宝石数量
        /// </summary>
        public int _NeedLevelOneGoodsCount;

        /// <summary>
        /// 消耗金币
        /// </summary>
        public int _NeedGold;
    }
}
