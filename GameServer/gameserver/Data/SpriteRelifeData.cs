using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 精灵回血
    /// </summary>
    [ProtoContract]
    public class SpriteRelifeData
    {
        [ProtoMember(1)]
        public int roleID;

        [ProtoMember(2)]
        public int x;

        [ProtoMember(3)]
        public int y;

        [ProtoMember(4)]
        public int direction;

        [ProtoMember(5)]
        public double lifeV;

        [ProtoMember(6)]
        public double magicV;

        [ProtoMember(7)]
        public int force;
    }
}
