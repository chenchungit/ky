using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 神器套装信息
    /// </summary>
    [ProtoContract]
    public class ArtifactSuitData
    {
        /// <summary>
        /// 套装id
        /// </summary>
        [ProtoMember(1)]
        public int SuitID = 0;

        /// <summary>
        /// 套装名称
        /// </summary>
        [ProtoMember(2)]
        public string SuitName = "";

        /// <summary>
        /// 套装装备id
        /// </summary>
        [ProtoMember(3)]
        public List<int> EquipIDList = null;

        /// <summary>
        /// 相同的多件是否计数
        /// </summary>
        [ProtoMember(4)]
        public bool IsMulti = false;

        /// <summary>
        /// 套装属性
        /// </summary>
        [ProtoMember(5)]
        public Dictionary<int,Dictionary<string,string>> SuitAttr = null;
    }
}
