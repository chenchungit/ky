using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using ProtoBuf;

namespace Server.Data
{
    // 一键征友数据类 [2/17/2014 LiaoWei]
    [ProtoContract]
    class OneKeyFindFriendData
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(1)]
        public int m_nRoleID = 0;

        /// <summary>
        /// 角色名字
        /// </summary>
        [ProtoMember(2)]
        public string m_nRoleName = "";

        /// <summary>
        /// 角色等级
        /// </summary>
        [ProtoMember(3)]
        public int m_nLevel = 0;

        /// <summary>
        /// 角色转生等级
        /// </summary>
        [ProtoMember(4)]
        public int m_nChangeLifeLev = 0;

        /// <summary>
        /// 角色职业
        /// </summary>
        [ProtoMember(5)]
        public int m_nOccupation = 0;

    }
}
