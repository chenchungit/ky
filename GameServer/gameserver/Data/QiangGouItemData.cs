using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 抢购购买历史记录项数据
    /// </summary>
    [ProtoContract]
    public class QiangGouItemData
    {
        /// <summary>
        /// 抢购ID[不管任何区 全局唯一]
        /// </summary>
        [ProtoMember(1)]
        public int QiangGouID = 0;

        /// <summary>
        /// 分组ID
        /// </summary>
        [ProtoMember(2)]
        public int Group = 0;

        /// <summary>
        /// 抢购项ID
        /// </summary>
        [ProtoMember(3)]
        public int ItemID = 0;

        /// <summary>
        /// 物品ID
        /// </summary>
        [ProtoMember(4)]
        public int GoodsID = 0;

        /// 开始时间
        /// </summary>
        [ProtoMember(5)]
        public String StartTime = "";

        /// 结束时间
        /// </summary>
        [ProtoMember(6)]
        public String EndTime = "";

        /// <summary>
        /// 是否过时【已经结束】
        /// </summary>
        [ProtoMember(7)]
        public int IsTimeOver = 0;

        /// <summary>
        /// 单个角色最大购买
        /// </summary>
        [ProtoMember(8)]
        public int SinglePurchase = 0;

        /// <summary>
        /// 所有角色最大购买
        /// </summary>
        [ProtoMember(9)]
        public int FullPurchase = 0;

        /// <summary>
        /// 所有角色已经购买
        /// </summary>
        [ProtoMember(10)]
        public int FullHasPurchase = 0;

        /// <summary>
        /// 当前角色已经购买
        /// </summary>
        [ProtoMember(11)]
        public int SingleHasPurchase = 0;

        /// <summary>
        /// 当前角色ID
        /// </summary>
        [ProtoMember(12)]
        public int CurrentRoleID = 0;

        /// <summary>
        /// 持续天数
        /// </summary>
        [ProtoMember(13)]
        public int DaysTime = 0;

        /// <summary>
        /// 抢购价格
        /// </summary>
        [ProtoMember(14)]
        public int Price = 0;

        /// <summary>
        /// 是否随机
        /// </summary>
        [ProtoMember(15)]
        public int Random = 0;

        /// <summary>
        /// 原始价格
        /// </summary>
        [ProtoMember(16)]
        public int OrigPrice = 0;

        /// <summary>
        /// 抢购类型
        /// 0 商店的抢购
        /// 1 合服抢购
        /// 2 节日抢购
        /// </summary>
        [ProtoMember(17)]
        public int Type = 0;
    }
}
