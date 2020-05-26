using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 点将台房间管理数据
    /// </summary>
    [ProtoContract] 
    public class DJRoomRolesData
    {
        /// <summary>
        /// 房间号
        /// </summary>
        [ProtoMember(1)]
        public int RoomID = 0;

        /// <summary>
        /// 队伍1
        /// </summary>
        [ProtoMember(2)]
        public List<DJRoomRoleData> Team1;

        /// <summary>
        /// 队伍2
        /// </summary>
        [ProtoMember(3)]
        public List<DJRoomRoleData> Team2;

        /// <summary>
        /// 队伍成员的准备状态
        /// </summary>
        [ProtoMember(4)]        
        public Dictionary<int, int> TeamStates;

        /// <summary>
        /// 是否已经全部锁定准备开始
        /// </summary>
        [ProtoMember(5)]         
        public int Locked;

        /// <summary>
        /// 是否已经被删除
        /// </summary>
        [ProtoMember(6)]   
        public int Removed;

        /// <summary>
        /// 观众
        /// </summary>
        [ProtoMember(7)]
        public List<DJRoomRoleData> ViewRoles;

        /// <summary>
        /// 进入和退出，或者和死亡状态词典
        /// </summary>
        [ProtoMember(8)]
        public Dictionary<int, int> RoleStates;
    }
}
