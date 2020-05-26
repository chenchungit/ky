using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 奇珍阁数据项
    /// </summary>
    [ProtoContract]  
    public class QiZhenGeItemData
    {
        /// <summary>
        /// 项流水号ID
        /// </summary>
        [ProtoMember(1)]
        public int ItemID = 0;

        /// <summary>
        /// 物品ID
        /// </summary>
        [ProtoMember(2)]
        public int GoodsID = 0;

        /// <summary>
        /// 物品原价
        /// </summary>
        [ProtoMember(3)]
        public int OrigPrice = 0;

        /// <summary>
        /// 物品现在价格
        /// </summary>
        [ProtoMember(4)]
        public int Price = 0;

        /// <summary>
        /// 物品现在价格
        /// </summary>
        [ProtoMember(5)]
        public string Description = "";

        /// <summary>
        /// 物品的单个概率控制
        /// </summary>
        [ProtoMember(6)]
        public int BaseProbability = 0;

        /// <summary>
        /// 物品的单个概率控制
        /// </summary>
        [ProtoMember(7)]
        public int SelfProbability = 0;
    }
}
