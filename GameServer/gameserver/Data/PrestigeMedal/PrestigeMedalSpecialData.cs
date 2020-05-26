using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    public class PrestigeMedalSpecialData
    {
        /// <summary>
        /// 额外属性id
        /// </summary>
        [ProtoMember(2)]
        public int SpecialID = 0;

        /// <summary>
        /// 勋章id
        /// </summary>
        [ProtoMember(1)]
        public int MedalID = 0;

        /// <summary>
        /// 双倍一击
        /// </summary>
        [ProtoMember(3)]
        public double DoubleAttack = 0;

        /// <summary>
        /// 抵抗双倍一击
        /// </summary>
        [ProtoMember(4)]
        public double DiDouble = 0;
    }
}
