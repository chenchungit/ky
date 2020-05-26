using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Tmsk.Contract
{
    /// <summary>
    /// 线路数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class KuaFuLineData
    {
        /// <summary>
        /// 线路ID
        /// </summary>
        [ProtoMember(1)]
        public int Line;

        /// <summary>
        /// 服务器状态
        /// </summary>
        [ProtoMember(2)]
        public int State;

        /// <summary>
        /// 在线人数
        /// </summary>
        [ProtoMember(3)]
        public int OnlineCount;
        
        /// <summary>
        /// 最大在线人数
        /// </summary>
        [ProtoMember(4)]
        public int MaxOnlineCount;

        /// <summary>
        /// 服务器编号
        /// </summary>
        [ProtoMember(5)]
        public int ServerId;

        /// <summary>
        /// 地图编号
        /// </summary>
        [ProtoMember(6)]
        public int MapCode;
    }
}
