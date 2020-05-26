using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 角色的经脉数据
    /// </summary>
    [ProtoContract]
    public class JingMaiData
    {
        /// <summary>
        /// 经脉的数据库ID
        /// </summary>
        [ProtoMember(1)]
        public int DbID = 0;

        /// <summary>
        /// 经脉ID
        /// </summary>
        [ProtoMember(2)]
        public int JingMaiID = 0;

        /// <summary>
        /// 经脉的的级别(对应穴位)
        /// </summary>
        [ProtoMember(3)]
        public int JingMaiLevel = 0;

        /// <summary>
        /// 经脉的的重数
        /// </summary>
        [ProtoMember(4)]
        public int JingMaiBodyLevel = 0;
    }
}
