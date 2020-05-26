using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Spread
{
    public class SpreadAwardInfo
    {
        /// <summary>
        /// 通用奖励
        /// </summary>
        public List<GoodsData> DefaultGoodsList = null;

        /// <summary>
        /// 根据职业奖励
        /// </summary>
        public List<GoodsData> ProGoodsList = null;
    }
}
