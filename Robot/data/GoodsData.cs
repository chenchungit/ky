using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 物品数据
    /// </summary>
    [ProtoContract]
    public class GoodsData
    {
        public GoodsData(GoodsData item)
        {
            if (null != item)
            {
                Id = -1;
                GoodsID = item.GoodsID;
                Using = 0;
                Forge_level = item.Forge_level;
                Starttime = "1900-01-01 12:00:00";
                Endtime = GameServer.Logic.Global.ConstGoodsEndTime;
                Site = 0;
                Quality = item.Quality;
                Props = item.Props;
                GCount = item.GCount;
                Binding = item.Binding;
                Jewellist = item.Jewellist;
                BagIndex = 0;
                AddPropIndex = item.AddPropIndex;
                BornIndex = item.BornIndex;
                Lucky = item.Lucky;
                Strong = item.Strong;
                ExcellenceInfo = item.ExcellenceInfo;
                AppendPropLev = item.AppendPropLev;
                ChangeLifeLevForEquip = item.ChangeLifeLevForEquip;
                WashProps = item.WashProps;
                ElementhrtsProps = item.ElementhrtsProps;
                SaleMoney1 = item.SaleMoney1;
                SaleYuanBao = item.SaleYuanBao;
                SaleYinPiao = item.SaleYinPiao;
            }
        }

        public GoodsData()
        {
        }

        /// <summary>
        /// 数据库流水ID
        /// </summary>
        [ProtoMember(1)]
        public int Id;

        /// <summary>
        /// 物品ID
        /// </summary>
        [ProtoMember(2)]
        public int GoodsID;

        /// <summary>
        /// 是否正在使用
        /// </summary>
        [ProtoMember(3)]
        public int Using;

        /// <summary>
        /// 锻造级别
        /// </summary>
        [ProtoMember(4)]
        public int Forge_level;

        /// <summary>
        /// 开始使用的时间
        /// </summary>
        [ProtoMember(5)]
        public string Starttime;

        /// <summary>
        /// 物品使用截止时间
        /// </summary>
        [ProtoMember(6)]
        public string Endtime;

        /// <summary>
        /// 所在的位置(0: 包裹, 1:仓库)
        /// </summary>
        [ProtoMember(7)]
        public int Site;

        /// <summary>
        /// 物品的品质(某些装备会分品质，不同的品质属性不同，用户改变属性后要记录下来)
        /// </summary>
        [ProtoMember(8)]
        public int Quality;

        /// <summary>
        /// 根据品质随机抽取的扩展属性的索引列表
        /// </summary>
        [ProtoMember(9)]
        public string Props;

        /// <summary>
        /// 物品数量
        /// </summary>
        [ProtoMember(10)]
        public int GCount;

        /// <summary>
        /// 是否绑定的物品(绑定的物品不可交易, 不可摆摊)
        /// </summary>
        [ProtoMember(11)]
        public int Binding;

        /// <summary>
        /// 根据品质随机抽取的扩展属性的索引列表
        /// </summary>
        [ProtoMember(12)]
        public string Jewellist;

        /// <summary>
        /// 装备时用于确定在装备栏中格子的索引 0=右手（主手），1=左右（副手）
        /// </summary>
        [ProtoMember(13)]
        public int BagIndex;

        /// <summary>
        /// 出售的金币价格
        /// </summary>
        [ProtoMember(14)]
        public int SaleMoney1;

        /// <summary>
        /// 出售的元宝价格
        /// </summary>
        [ProtoMember(15)]
        public int SaleYuanBao;

        /// <summary>
        /// 出售的银两价格
        /// </summary>
        [ProtoMember(16)]
        public int SaleYinPiao;

        /// <summary>
        /// 出售的银两价格
        /// </summary>
        [ProtoMember(17)]
        public int AddPropIndex;

        /// <summary>
        /// 增加一个天生属性的百分比
        /// </summary>
        [ProtoMember(18)]
        public int BornIndex;

        /// <summary>
        /// 装备的幸运值
        /// </summary>
        [ProtoMember(19)]
        public int Lucky;

        /// <summary>
        /// 装备的耐久度
        /// </summary>
        [ProtoMember(20)]
        public int Strong;

        // 新增物品属性 [12/13/2013 LiaoWei]
        /// <summary>
        /// 卓越信息 -- 一个32位int 每位代表一个卓越属性
        /// </summary>
        [ProtoMember(21)]
        public int ExcellenceInfo;

        // 新增物品属性 [12/18/2013 LiaoWei]
        /// <summary>
        /// 追加等级
        /// </summary>
        [ProtoMember(22)]
        public int AppendPropLev;

        // 新增物品属性 [2/17/2014 LiaoWei]
        /// <summary>
        /// 装备的转生级别
        /// </summary>
        [ProtoMember(23)]
        public int ChangeLifeLevForEquip;

        /// <summary>
        /// 装备洗炼属性
        /// 结构: 属性ID|属性值|属性ID|属性值|属性ID|属性值...
        /// </summary>
        [ProtoMember(24)]
        public List<int> WashProps;

        /// <summary>
        /// 元素之心的属性
        /// </summary>
        [ProtoMember(25)]
        public List<int> ElementhrtsProps;
    }
}
