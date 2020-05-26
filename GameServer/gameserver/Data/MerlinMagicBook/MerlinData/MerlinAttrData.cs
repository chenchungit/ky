using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 梅林魔法书属性 [XSea 2015/6/18]
    /// </summary>
    [ProtoContract]
    public class MerlinAttrData
    {
        /// <summary>
        /// 最小物攻
        /// </summary>
        [ProtoMember(1)]
        public int _MinAttackV = 0;

        /// <summary>
        /// 最大物攻
        /// </summary>
        [ProtoMember(2)]
        public int _MaxAttackV = 0;

        /// <summary>
        /// 最小魔攻
        /// </summary>
        [ProtoMember(3)]
        public int _MinMAttackV = 0;

        /// <summary>
        /// 最大魔攻
        /// </summary>
        [ProtoMember(4)]
        public int _MaxMAttackV = 0;

        /// <summary>
        /// 最小物防
        /// </summary>
        [ProtoMember(5)]
        public int _MinDefenseV = 0;

        /// <summary>
        /// 最大物防
        /// </summary>
        [ProtoMember(6)]
        public int _MaxDefenseV = 0;

        /// <summary>
        /// 最小魔防
        /// </summary>
        [ProtoMember(7)]
        public int _MinMDefenseV = 0;

        /// <summary>
        /// 最大魔防
        /// </summary>
        [ProtoMember(8)]
        public int _MaxMDefenseV = 0;

        /// <summary>
        /// 命中
        /// </summary>
        [ProtoMember(9)]
        public int _HitV = 0;

        /// <summary>
        /// 闪避
        /// </summary>
        [ProtoMember(10)]
        public int _DodgeV = 0;

        /// <summary>
        /// 生命上限
        /// </summary>
        [ProtoMember(11)]
        public int _MaxHpV = 0;

        /// <summary>
        /// 重生几率
        /// </summary>
        [ProtoMember(12)]
        public double _ReviveP = 0;

        /// <summary>
        /// 魔法完全恢复几率
        /// </summary>
        [ProtoMember(13)]
        public double _MpRecoverP = 0;
    }
}
