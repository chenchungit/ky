using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 荧光宝石矿坑类型表 100=初级矿坑，200=中级矿坑，300=高级矿坑
    /// </summary>
    public class FluorescentGemLevelTypeConfigData
    {
        /// <summary>
        /// 消耗荧光宝石粉末
        /// </summary>
        public int _NeedFluorescentPoint;

        /// <summary>
        /// 消耗钻石
        /// </summary>
        public int _NeedDiamond;
    }
}
