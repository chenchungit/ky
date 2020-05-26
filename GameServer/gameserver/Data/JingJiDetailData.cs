using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 竞技场详情数据
    /// </summary>
    [ProtoContract]
    public class JingJiDetailData
    {

        /// <summary>
        /// 状态码
        /// ResultCode
        /// </summary>
        [ProtoMember(1)]
        public int state;

        /// <summary>
        /// 免费挑战次数
        /// </summary>
        [ProtoMember(2)]
        public int freeChallengeNum;

        /// <summary>
        /// 已用免费挑战次数
        /// </summary>
        [ProtoMember(3)]
        public int useFreeChallengeNum;

        /// <summary>
        /// vip挑战次数
        /// </summary>
        [ProtoMember(4)]
        public int vipChallengeNum;

        /// <summary>
        /// 已用vip挑战次数
        /// </summary>
        [ProtoMember(5)]
        public int useVipChallengeNum;
        
        /// <summary>
        /// 连胜次数
        /// </summary>
        [ProtoMember(6)]
        public int winCount = 0;

        /// <summary>
        /// 排名
        /// </summary>
        [ProtoMember(7)]
        public int ranking = -1;

        /// <summary>
        /// 下次领取奖励时间戳
        /// </summary>
        [ProtoMember(8)]
        public long nextRewardTime = 0;

        /// <summary>
        /// 上次挑战时间戳
        /// </summary>
        [ProtoMember(9)]
        public long nextChallengeTime = 0;

        /// <summary>
        /// 被挑战者数据
        /// </summary>
        [ProtoMember(10)]
        public List<PlayerJingJiMiniData> beChallengerData;

        /// <summary>
        /// 最大连胜
        /// </summary>
        [ProtoMember(11)]
        public int maxwincount;

    }
}
