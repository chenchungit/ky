using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 荧光宝石数据 [XSea 2015/8/6]
    /// </summary>
    [ProtoContract]
    public class FluorescentGemData
    {
        /// <summary>
        /// 宝石镶嵌列表
        /// [部位id，[宝石类型,宝石GoodsData]]
        /// 
        /// refactor by chenjg.2015-11-30 此字段在服务器内部废弃，仅用于通知客户端(兼容客户端协议)
        /// </summary>
        [ProtoMember(1)]
        public Dictionary<int, Dictionary<int, GoodsData>> GemEquipDict = new Dictionary<int, Dictionary<int, GoodsData>>();

        /// <summary>
        /// 宝石背包
        /// 格子索引,宝石GoodsData
        /// 
        /// refactor by chenjg.2015-11-30 此字段在服务器内部废弃，仅用于通知客户端(兼容客户端协议)
        /// </summary>
        [ProtoMember(2)]
        public Dictionary<int, GoodsData> GemBagDict = new Dictionary<int, GoodsData>();


        /// <summary>
        /// 宝石背包
        /// 
        /// 此字段用于GameServer和GameDBServer之间通信
        /// </summary>
        [ProtoMember(10)]
        public List<GoodsData> GemBagList = new List<GoodsData>();

        /// <summary>
        /// 宝石装备
        /// 
        /// 此字段用于GameServer和GameDBServer之间通信
        /// </summary>
        [ProtoMember(11)]
        public List<GoodsData> GemEquipList = new List<GoodsData>();
    }
}
