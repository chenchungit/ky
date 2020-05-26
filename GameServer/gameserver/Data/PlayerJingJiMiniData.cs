using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 用于展示的竞技场机器人数据
    /// </summary>
    [ProtoContract]
    public class PlayerJingJiMiniData
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        [ProtoMember(1)]
        public int roleId;

        /// <summary>
        /// 玩家名称
        /// </summary>
        [ProtoMember(2)]
        public string roleName;

        /// <summary>
        /// 玩家职业
        /// </summary>
        [ProtoMember(3)]
        public int occupationId;

        /// <summary>
        /// 玩家战力
        /// </summary>
        [ProtoMember(4)]
        public int combatForce;

        /// <summary>
        /// 排名
        /// </summary>
        [ProtoMember(5)]
        public int ranking;

        /// <summary>
        /// 性别
        /// </summary>
        [ProtoMember(6)]
        public int sex;

    }
}
