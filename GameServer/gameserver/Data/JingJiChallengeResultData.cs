using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 竞技场挑战结果
    /// </summary>
    [ProtoContract]
    public class JingJiChallengeResultData
    {
        /// <summary>
        /// 挑战者
        /// </summary>
        [ProtoMember(1)]
        public int playerId;

        /// <summary>
        /// 被挑战者
        /// </summary>
        [ProtoMember(2)]
        public int robotId;

        /// <summary>
        /// 是否胜利
        /// </summary>
        [ProtoMember(3)]
        public bool isWin;

    }
}
