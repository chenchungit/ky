using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 点将台房间数据
    /// </summary>
    [ProtoContract]    
    public class DJRoomData
    {
        /// <summary>
        /// 房间号
        /// </summary>
        [ProtoMember(1)]
        public int RoomID = 0;

        /// <summary>
        /// 创建房间的角色ID
        /// </summary>
        [ProtoMember(2)]
        public int CreateRoleID = 0;

        /// <summary>
        /// 创建房间的角色名称
        /// </summary>
        [ProtoMember(3)]
        public string CreateRoleName = "";

        /// <summary>
        /// 房间名称
        /// </summary>
        [ProtoMember(4)]
        public string RoomName = "";

        /// <summary>
        /// VS模式, 0: 1V1, 1: 2V2, 2: 3V3
        /// </summary>
        [ProtoMember(5)]
        public int VSMode = 0;

        /// <summary>
        /// 房间状态, 0: 准备, 1:开始战斗
        /// </summary>
        [ProtoMember(6)]
        public int PKState = 0;

        /// <summary>
        /// 当前人数
        /// </summary>
        [ProtoMember(7)]
        public int PKRoleNum = 0;

        /// <summary>
        /// 当前观众人数
        /// </summary>
        [ProtoMember(8)]
        public int ViewRoleNum = 0;

        /// <summary>
        /// 开始战斗时间
        /// </summary>
        [ProtoMember(9)]
        public long StartFightTicks = 0;

        /// <summary>
        /// 战斗状态
        /// </summary>
        [ProtoMember(10)]
        public int DJFightState = 0;
    }
}
