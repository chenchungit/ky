using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 翎羽信息
    /// </summary>
    [ProtoContract]
    public class LingYuData
    {
        public LingYuData()
        {
            this.Type = -1;
            this.Level = 0;
            this.Suit = 0;
        }

        /// <summary>
        /// 翎羽类型
        /// </summary>
        [ProtoMember(1)]
        public int Type;

        /// <summary>
        /// 等级
        /// </summary>
        [ProtoMember(2)]
        public int Level;

        /// <summary>
        /// 品阶
        /// </summary>
        [ProtoMember(3)]
        public int Suit;
    }

}
