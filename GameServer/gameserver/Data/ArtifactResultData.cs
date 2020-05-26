using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    public class ArtifactResultData
    {
        /// <summary>
        /// 神器再造状态
        /// </summary>
        [ProtoMember(1)]
        public int State = 0;

        /// <summary>
        /// 装备id
        /// </summary>
        [ProtoMember(2)]
        public int EquipDbID = 0;

        /// <summary>
        /// 是否绑定
        /// </summary>
        [ProtoMember(3)]
        public int Bind = 0;
    }
}
