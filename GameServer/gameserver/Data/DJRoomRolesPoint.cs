using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 点将台房间积分数据
    /// </summary>
    [ProtoContract] 
    public class DJRoomRolesPoint
    {
        /// <summary>
        /// 房间号
        /// </summary>
        [ProtoMember(1)]
        public int RoomID = 0;

        /// <summary>
        /// 房间名称
        /// </summary>
        [ProtoMember(2)]
        public string RoomName = "";

        /// <summary>
        /// 队伍2
        /// </summary>
        [ProtoMember(3)]
        public List<DJRoomRolePoint> RolePoints;
    }
}
