using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 交易数据
    /// </summary>
    [ProtoContract]
    public class ExchangeData
    {
        /// <summary>
        /// 交易ID
        /// </summary>
        [ProtoMember(1)]
        public int ExchangeID;

        /// <summary>
        /// 请求交易的ID
        /// </summary>
        [ProtoMember(2)]
        public int RequestRoleID;

        /// <summary>
        /// 同意交易的ID
        /// </summary>
        [ProtoMember(3)]
        public int AgreeRoleID;

        /// <summary>
        /// 双方的物品列表
        /// </summary>
        [ProtoMember(4)]
        public Dictionary<int, List<GoodsData>> GoodsDict;

        /// <summary>
        /// 双方的金币列表
        /// </summary>
        [ProtoMember(5)]
        public Dictionary<int, int> MoneyDict;

        /// <summary>
        /// 双方的锁定状态
        /// </summary>
        [ProtoMember(6)]
        public Dictionary<int, int> LockDict;

        /// <summary>
        /// 双方的完成状态
        /// </summary>
        [ProtoMember(7)]
        public Dictionary<int, int> DoneDict;

        /// <summary>
        /// 添加的时间(单位秒)
        /// </summary>
        [ProtoMember(8)]
        public long AddDateTime;

        /// <summary>
        /// 完成状态
        /// </summary>
        [ProtoMember(9)]
        public int Done;

        /// <summary>
        /// 双方的元宝列表
        /// </summary>
        [ProtoMember(10)]
        public Dictionary<int, int> YuanBaoDict;
    }
}
