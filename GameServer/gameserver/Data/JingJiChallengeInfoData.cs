using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 竞技场战报
    /// </summary>
    [ProtoContract]
    public class JingJiChallengeInfoData
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [ProtoMember(1)]
        public int pkId;

        /// <summary>
        /// 玩家ID
        /// </summary>
        [ProtoMember(2)]
        public int roleId;

        /// <summary>
        /// 战报类型
        /// 
        /// 挑战成功：0
        /// 挑战失败：1
        /// 被挑战，胜利：2
        /// 被挑战，战败：3
        /// 连胜：4
        /// </summary>
        [ProtoMember(3)]
        public int zhanbaoType;

        /// <summary>
        /// 挑战者或被挑战者名字
        /// </summary>
        [ProtoMember(4)]
        public string challengeName;

        /// <summary>
        /// 挑战成功：排名值
        /// 挑战失败：无用
        /// 被挑战，胜利：无用
        /// 被挑战，战败：排名值
        /// 连胜：连胜次数
        /// </summary>
        [ProtoMember(5)]
        public int value;

    }
}
