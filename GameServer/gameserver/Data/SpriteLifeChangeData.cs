using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 精灵生命变化
    /// </summary>
    [ProtoContract]
    public class SpriteLifeChangeData
    {
        [ProtoMember(1)]
        public int roleID;

        [ProtoMember(2)]
        public int lifeV;

        [ProtoMember(3)]
        public int magicV;

        [ProtoMember(4)]
        public int currentLifeV;

        [ProtoMember(5)]
        public int currentMagicV;
    }
}
