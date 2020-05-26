using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 荧光宝石存储数据 [XSea 2015/8/6]
    /// </summary>
    [ProtoContract]
    public class FluorescentGemSaveDBData
    {
        /// <summary>
        /// 角色id
        /// </summary>
        [ProtoMember(1)]
        public int _RoleID;

        /// <summary>
        /// 物品id
        /// </summary>
        [ProtoMember(2)]
        public int _GoodsID;

        /// <summary>
        /// 装备部位索引
        /// </summary>
        [ProtoMember(3)]
        public int _Position;

        /// <summary>
        /// 宝石类型
        /// </summary>
        [ProtoMember(4)]
        public int _GemType;

        /// <summary>
        /// 绑定状态
        /// </summary>
        [ProtoMember(5)]
        public int _Bind;
    }
}
