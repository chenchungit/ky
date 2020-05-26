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
    public class ElementWarScoreData
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
        /// 怪物数量
        /// </summary>
        [ProtoMember(3)]
        public long MonsterCount = 0;
    }

    /// <summary>
    /// 领奖信息
    /// </summary>
    [ProtoContract]
    public class ElementWarAwardsData
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
        /// 荧光粉末
        /// </summary>
        [ProtoMember(3)]
        public int Light;

        /// <summary>
        /// 波数
        /// </summary>
        [ProtoMember(4)]
        public int Wave;
    }
}
