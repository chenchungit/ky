using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 特效数据
    /// </summary>
    [ProtoContract]
    public class DecorationData
    {
        /// <summary>
        /// 流水ID
        /// </summary>
        [ProtoMember(1)]
        public int AutoID = 0;

        /// <summary>
        /// Deco的ID
        /// </summary>
        [ProtoMember(2)]
        public int DecoID = 0;

        /// <summary>
        /// 格子X坐标
        /// </summary>
        [ProtoMember(3)]
        public int PosX = 0;

        /// <summary>
        /// 格子Y坐标
        /// </summary>
        [ProtoMember(4)]
        public int PosY = 0;

        /// <summary>
        /// 地图编码
        /// </summary>
        [ProtoMember(5)]
        public int MapCode = -1;

        /// <summary>
        /// 开始时间
        /// </summary>
        [ProtoMember(6)]
        public long StartTicks = 0;

        /// <summary>
        /// 最大生存时间
        /// </summary>
        [ProtoMember(7)]
        public int MaxLiveTicks = 0;

        /// <summary>
        /// 开始变透明的时间
        /// </summary>
        [ProtoMember(8)]
        public int AlphaTicks = 0;
    }
}
