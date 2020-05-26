using System;
using System.Net;
using System.Collections.Generic;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 帐号的少量数据的数据定义
    /// </summary>
    [ProtoContract]
    public class UserMiniData
    {
        /// <summary>
        /// 当前的帐号ID
        /// </summary>
        [ProtoMember(1)]
        public string UserId;

        /// <summary>
        /// 最近登陆的角色ID
        /// </summary>
        [ProtoMember(2)]
        public int LastRoleId;

        /// <summary>
        /// 充值钱数
        /// </summary>
        [ProtoMember(3)]
        public int RealMoney;

        [ProtoMember(4)]
        public DateTime MinCreateRoleTime;

        [ProtoMember(5)]
        public DateTime LastLoginTime;

        [ProtoMember(6)]
        public DateTime LastLogoutTime;

        [ProtoMember(7)]
        public DateTime RoleCreateTime;

        [ProtoMember(8)]
        public DateTime RoleLastLoginTime;

        [ProtoMember(9)]
        public DateTime RoleLastLogoutTime;

        [ProtoMember(10)]
        public int MaxLevel;

        [ProtoMember(11)]
        public int MaxChangeLifeCount;
    }
}
