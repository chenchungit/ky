using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 神器信息
    /// </summary>
    [ProtoContract]
    public class ArtifactData
    {
        /// <summary>
        /// 神器id
        /// </summary>
        [ProtoMember(1)]
        public int ArtifactID = 0;

        /// <summary>
        /// 神器名称
        /// </summary>
        [ProtoMember(2)]
        public string ArtifactName = "";

        /// <summary>
        /// 再造后装备id
        /// </summary>
        [ProtoMember(3)]
        public int NewEquitID = 0;

        /// <summary>
        /// 需要装备id
        /// </summary>
        [ProtoMember(4)]
        public int NeedEquitID = 0;

        /// <summary>
        /// 需要道具<道具id，数量>
        /// </summary>
        [ProtoMember(5)]
        public Dictionary<int, int> NeedMaterial = null;

        /// <summary>
        /// 需要绑定金币
        /// </summary>
        [ProtoMember(6)]
        public int NeedGoldBind = 0;

        /// <summary>
        /// 需要再造点数
        /// </summary>
        [ProtoMember(7)]
        public int NeedZaiZao = 0;

        /// <summary>
        /// 失败销毁道具<道具id，数量>
        /// </summary>
        [ProtoMember(8)]
        public Dictionary<int, int> FailMaterial = null;

        /// <summary>
        /// 成功率
        /// </summary>
        [ProtoMember(9)]
        public int SuccessRate= 0;
    }
}
