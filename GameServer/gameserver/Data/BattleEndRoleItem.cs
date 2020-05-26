using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 角斗场结束角色数据
    /// </summary>
    [ProtoContract]
    public class BattleEndRoleItem
    {
        /// <summary>
        /// 角色的ID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID = 0;

        /// <summary>
        /// 角色的名称
        /// </summary>
        [ProtoMember(2)]
        public string RoleName = "";

        /// <summary>
        /// 杀死的人数
        /// </summary>
        [ProtoMember(3)]
        public int KilledNum = 0;
    }
}
