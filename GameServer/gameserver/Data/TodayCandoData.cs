using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    class TodayCandoData
    {
        /// <summary>
        /// JinRiKeZuo.xml中的ID.
        /// </summary>
        [ProtoMember(1)]
        public int ID;	

        /// <summary>
        /// 剩余次数
        /// </summary>
        [ProtoMember(2)]
        public int LeftCount = 0; 
    }
}
