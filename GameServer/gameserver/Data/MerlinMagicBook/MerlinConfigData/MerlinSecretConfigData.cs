using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 梅林秘语配置 [XSea 2015/6/18]
    /// </summary>
    public class MerlinSecretConfigData
    {
        /// <summary>
        /// 阶数
        /// </summary>
        public int _Level;

        /// <summary>
        /// 所需物品
        /// </summary>
        public int _NeedGoodsID;

        /// <summary>
        /// 所需物品数量
        /// </summary>
        public int _NeedGoodsCount;

        /// <summary>
        /// 可随机到的总值库 格式：x,x,x,x,x,x,x
        /// </summary>
        public int[] _Num;
    }
}
