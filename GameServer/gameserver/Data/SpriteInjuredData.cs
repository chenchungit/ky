using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 精灵伤害数据
    /// </summary>
    [ProtoContract]
    public class SpriteInjuredData
    {
        [ProtoMember(1)]
        public int attackerRoleID = 0;

        [ProtoMember(2)]
        public int injuredRoleID = 0;

        [ProtoMember(3)]
        public int burst = 0;

        [ProtoMember(4)]
        public int injure = 0;

        [ProtoMember(5)]
        public double injuredRoleLife = 0;

        [ProtoMember(6)]
        public int attackerLevel = 0;

        [ProtoMember(7)]
        public int injuredRoleMaxLifeV = 0;

        [ProtoMember(8)]
        public int injuredRoleMagic = 0;

        [ProtoMember(9)]
        public int injuredRoleMaxMagicV = 0;

        [ProtoMember(10)]
        public int hitToGridX = 0;

        [ProtoMember(11)]
        public int hitToGridY = 0;

        /// <summary>
        /// 梅林伤害值
        /// </summary>
        [ProtoMember(12)]
        public int MerlinInjuer = 0;

        /// <summary>
        /// 梅林伤害类型
        /// </summary>
        [ProtoMember(13)]
        public sbyte MerlinType = 0;
    }
}
