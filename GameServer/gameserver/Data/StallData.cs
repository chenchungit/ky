using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 摆摊数据
    /// </summary>
    [ProtoContract]
    public class StallData
    {
        /// <summary>
        /// 摆摊ID
        /// </summary>
        [ProtoMember(1)]
        public int StallID;

        /// <summary>
        /// 摆摊的角色的ID
        /// </summary>
        [ProtoMember(2)]
        public int RoleID;

        /// <summary>
        /// 摊位名称
        /// </summary>
        [ProtoMember(3)]
        public string StallName;

        /// <summary>
        /// 摊位留言
        /// </summary>
        [ProtoMember(4)]
        public string StallMessage;

        /// <summary>
        /// 物品列表
        /// </summary>
        [ProtoMember(5)]
        public List<GoodsData> GoodsList;

        /// <summary>
        /// 物品的价格词典
        /// </summary>
        [ProtoMember(6)]
        public Dictionary<int, int> GoodsPriceDict;

        /// <summary>
        /// 添加的时间(单位秒)
        /// </summary>
        [ProtoMember(7)]
        public long AddDateTime;

        /// <summary>
        /// 开始摆摊
        /// </summary>
        [ProtoMember(8)]
        public int Start;
    }
}
