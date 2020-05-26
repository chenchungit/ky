using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 组队数据
    /// </summary>
    [ProtoContract]
    public class TeamData
    {
        /// <summary>
        /// 队伍流水ID
        /// </summary>
        [ProtoMember(1)]
        public int TeamID = 0;

        /// <summary>
        /// 队伍队长
        /// </summary>
        [ProtoMember(2)]
        public int LeaderRoleID = 0;

        /// <summary>
        /// 组队的成员列表
        /// </summary>
        [ProtoMember(3)]
        public List<TeamMemberData> TeamRoles;

        /// <summary>
        /// 组队建立的时间(单位秒)
        /// </summary>
        [ProtoMember(4)]
        public long AddDateTime = 0;

        /// <summary>
        /// 自由拾取选项
        /// </summary>
        [ProtoMember(5)]
        public int GetThingOpt = 0;

        /// <summary>
        /// 队长的当前X坐标
        /// </summary>
        [ProtoMember(6)]
        public int PosX = 0;

        /// <summary>
        /// 队长的当前Y坐标
        /// </summary>
        [ProtoMember(7)]
        public int PosY = 0;
    }
}
