using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 竞技场机器人装备数据
    /// </summary>
    [ProtoContract]
    public class PlayerJingJiEquipData
    {

        /// <summary>
        /// 装备ID
        /// </summary>
        [ProtoMember(1)]
        public int EquipId;

        /// <summary>
        /// 强化等级
        /// </summary>
        [ProtoMember(2)]
        public int Forge_level;

        /// <summary>
        /// 卓越属性
        /// </summary>
        [ProtoMember(3)]
        public int ExcellenceInfo;
    }
}
