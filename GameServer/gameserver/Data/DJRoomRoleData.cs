using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 点将台房间成员数据
    /// </summary>
    [ProtoContract] 
    public class DJRoomRoleData
    {
        /// <summary>
        /// 成员角色ID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID = 0;

        /// <summary>
        /// 成员角色名称
        /// </summary>
        [ProtoMember(2)]
        public string RoleName;

        /// <summary>
        /// 成员的等级
        /// </summary>
        [ProtoMember(3)]
        public int Level = 0;

        /// <summary>
        /// 积分
        /// </summary>
        [ProtoMember(4)]
        public int DJPoint = 0;

        /// <summary>
        /// 总的比赛场次
        /// </summary>
        [ProtoMember(5)]
        public int DJTotal = 0;

        /// <summary>
        /// 获胜的场次
        /// </summary>
        [ProtoMember(6)]
        public int DJWincnt = 0;
    }
}
