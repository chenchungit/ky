using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 排行榜数据封装类
    /// </summary>
    [ProtoContract]
    public class PlayerJingJiRankingData
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
        /// 玩家战力
        /// </summary>
        [ProtoMember(3)]
        public int combatForce;

        /// <summary>
        /// 玩家战力
        /// </summary>
        [ProtoMember(4)]
        public int ranking;
    }
}
