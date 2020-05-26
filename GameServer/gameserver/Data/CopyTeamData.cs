using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data.OldCopyTeam
{
    /// <summary>
    /// 组队数据
    /// </summary>
    [ProtoContract]
    public class CopyTeamData
    {
        public CopyTeamData SimpleClone()
        {
            CopyTeamData simple = new CopyTeamData();
            simple.TeamID =TeamID;
            simple.LeaderRoleID = LeaderRoleID;
            simple.StartTime = StartTime;
            simple.GetThingOpt = GetThingOpt;
            simple.SceneIndex = SceneIndex;
            simple.FuBenSeqID = FuBenSeqID;
            simple.MinZhanLi = MinZhanLi;
            simple.AutoStart = AutoStart;
            simple.TeamRoles = null;
            simple.MemberCount = MemberCount;
            simple.TeamName = TeamName;
            return simple;
        }

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
        public List<CopyTeamMemberData> TeamRoles;

        /// <summary>
        /// 在组队副本中表示开始时间,0表示未开始
        /// </summary>
        [ProtoMember(4)]
        public long StartTime = 0;

        /// <summary>
        /// 自由拾取选项
        /// </summary>
        [ProtoMember(5)]
        public int GetThingOpt = 0;

        /// <summary>
        /// 副本ID
        /// </summary>
        [ProtoMember(6)]
        public int SceneIndex = 0;

        /// <summary>
        /// 运行时场景ID, 对于副本,是FuBenSeqID
        /// </summary>
        [ProtoMember(7)]
        public int FuBenSeqID = 0;

        /// <summary>
        /// 队长设定的最小战力要求
        /// </summary>
        [ProtoMember(8)]
        public int MinZhanLi = 0;

        /// <summary>
        /// 是否自动开始
        /// </summary>
        [ProtoMember(9)]
        public bool AutoStart = false;

        /// <summary>
        /// 成员个数
        /// </summary>
        [ProtoMember(10)]
        public int MemberCount = 0;

        /// <summary>
        /// 队伍名称(队长名称)
        /// </summary>
        [ProtoMember(11)]
        public string TeamName = null;
    }
}
