using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace GameServer.Logic
{
    /// <summary>
    /// 成绩信息
    /// </summary>
    [ProtoContract]
    public class CopyWolfScoreData
    {
        /// <summary>
        /// 波次
        /// </summary>
        [ProtoMember(1)]
        public int Wave = 0;

        /// <summary>
        ///  结束时间
        /// </summary>
        [ProtoMember(2)]
        public long EndTime = 0;

        /// <summary>
        /// 要塞生命
        /// </summary>
        [ProtoMember(3)]
        public int FortLifeNow = 0;

        /// <summary>
        /// 要塞生命（最大）
        /// </summary>
        [ProtoMember(4)]
        public int FortLifeMax = 0;

        /// <summary>
        /// 积分（角色id，分数）
        /// </summary>
        [ProtoMember(5)]
        public Dictionary<int, int> RoleMonsterScore = new Dictionary<int, int>();

        /// <summary>
        /// 剩余怪物数量
        /// </summary>
        [ProtoMember(6)]
        public int MonsterCount = 0;
    }

    /// <summary>
    /// 领奖信息
    /// </summary>
    [ProtoContract]
    public class CopyWolfAwardsData
    {
        /// <summary>
        /// 经验
        /// </summary>
        [ProtoMember(1)]
        public long Exp;

        /// <summary>
        /// 金钱
        /// </summary>
        [ProtoMember(2)]
        public int Money;

        /// <summary>
        /// 狼魂粉末
        /// </summary>
        [ProtoMember(3)]
        public int WolfMoney;

        /// <summary>
        /// 波数
        /// </summary>
        [ProtoMember(4)]
        public int Wave;

        /// <summary>
        /// 积分（角色id，分数）
        /// </summary>
        [ProtoMember(5)]
        public int RoleScore = 0;
    }
}
