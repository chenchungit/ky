using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 组队成员数据
    /// </summary>
    [ProtoContract]
    public class TeamMemberData
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
        /// 角色的性别
        /// </summary>
        [ProtoMember(3)]
        public int RoleSex = 0;

        /// <summary>
        /// 成员的等级
        /// </summary>
        [ProtoMember(4)]
        public int Level = 0;

        /// <summary>
        /// 成员的职业
        /// </summary>
        [ProtoMember(5)]
        public int Occupation = 0;

        /// <summary>
        /// 当前的头像
        /// </summary>
        [ProtoMember(6)]
        public int RolePic = 0;

        /// <summary>
        /// 所在的地图的编号
        /// </summary>
        [ProtoMember(7)]
        public int MapCode = 0;

        /// <summary>
        /// 成员的在线状态
        /// </summary>
        [ProtoMember(8)]
        public int OnlineState = 0;

        /// <summary>
        /// 成员的最大血量
        /// </summary>
        [ProtoMember(9)]
        public int MaxLifeV = 0;

        /// <summary>
        /// 成员的当前血量
        /// </summary>
        [ProtoMember(10)]
        public int CurrentLifeV = 0;

        /// <summary>
        /// 成员的最大魔量
        /// </summary>
        [ProtoMember(11)]
        public int MaxMagicV = 0;

        /// <summary>
        /// 成员的当前魔量
        /// </summary>
        [ProtoMember(12)]
        public int CurrentMagicV = 0;

        /// <summary>
        /// 成员的当前X坐标
        /// </summary>
        [ProtoMember(13)]
        public int PosX = 0;

        /// <summary>
        /// 成员的当前Y坐标
        /// </summary>
        [ProtoMember(14)]
        public int PosY = 0;

        /// <summary>
        /// 成员战力
        /// </summary>
        [ProtoMember(15)]
        public int CombatForce = 0;

        /// <summary>
        /// 成员转生级别  MU新增 [1/10/2014 LiaoWei]
        /// </summary>
        [ProtoMember(16)]
        public int ChangeLifeLev = 0;
    }
}
