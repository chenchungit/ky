using System;
using System.Net;
using System.Collections.Generic;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 地图传送点信息
    /// </summary>
    [ProtoContract]
    public class MapTeleportInfo
    {
        /// <summary>
        /// 当前地图编号
        /// </summary>
        [ProtoMember(1)]
        public int MapCode = 0;

        /// <summary>
        /// 本地图的传送点状态列表
        /// </summary>
        [ProtoMember(2)]
        public List<TeleportState> TeleportStateList;
    }
    
    /// <summary>
    /// 传送点状态信息
    /// </summary>
    [ProtoContract]
    public class TeleportState
    {
        /// <summary>
        /// 目标地图编号
        /// </summary>
        [ProtoMember(1)]
        public int ToMapCode;

        /// <summary>
        /// 状态
        /// </summary>
        [ProtoMember(2)]
        public int State;
    }
}
