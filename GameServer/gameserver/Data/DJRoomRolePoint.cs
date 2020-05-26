using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 点将台结果成员积分
    /// </summary>
    [ProtoContract]     
    public class DJRoomRolePoint
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
        public int FightPoint = 0;
    }
}
