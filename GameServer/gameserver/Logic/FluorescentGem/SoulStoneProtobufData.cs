using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// "魂石获取"额外功能
    /// </summary>
    [ProtoContract]
    public class SoulStoneExtFuncItem
    {
        // 功能类型
        [ProtoMember(1)]
        public int FuncType;

        // 消耗类型
        [ProtoMember(2)]
        public int CostType;
    }

    /// <summary>
    /// "魂石获取" 随机信息查询
    /// </summary>
    [ProtoContract]
    public class SoulStoneQueryGetData
    {
        [ProtoMember(1)]
        public int CurrRandId;

        [ProtoMember(2)]
        public List<SoulStoneExtFuncItem> ExtFuncList;
    }

    /// <summary>
    /// 魂石数据
    /// </summary>
    [ProtoContract]
    public class SoulStoneData
    {
        // 魂石背包栏
        [ProtoMember(1)]
        public List<GoodsData> StonesInBag;

        // 魂石装备栏
        [ProtoMember(2)]
        public List<GoodsData> StonesInUsing;
    }

    /// <summary>
    /// 获取魂石
    /// </summary>
    [ProtoContract]
    public class SoulStoneGetData
    {
        [ProtoMember(1)]
        public int Error;

        // 客户端请求进行的次数
        [ProtoMember(2)]
        public int RequestTimes;

        // 实际进行的次数
        [ProtoMember(3)]
        public int RealDoTimes;

        // 最终的随机组ID
        [ProtoMember(4)]
        public int NewRandId;

        // 魂石GoodsID列表
        [ProtoMember(5)]
        public List<int> Stones;

        // 额外获得物品
        [ProtoMember(6)]
        public List<int> ExtGoods;
    }
}
