using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    public class AchievementRuneBasicData
    {
        /// <summary>
        /// 符文id
        /// </summary>
        [ProtoMember(1)]
        public int RuneID = 0;

        /// <summary>
        /// 符文名称
        /// </summary>
        [ProtoMember(2)]
        public string RuneName = "";

        /// <summary>
        /// 生命上限
        /// </summary>
        [ProtoMember(3)]
        public int LifeMax = 0;

        /// <summary>
        /// 攻击力上限
        /// </summary>
        [ProtoMember(4)]
        public int AttackMax = 0;

        /// <summary>
        /// 防御力上限
        /// </summary>
        [ProtoMember(5)]
        public int DefenseMax = 0;

        /// <summary>
        /// 闪避上限
        /// </summary>
        [ProtoMember(6)]
        public int DodgeMax = 0;

        /// <summary>
        /// 成就消耗
        /// </summary>
        [ProtoMember(7)]
        public int AchievementCost = 0;

        /// <summary>
        /// 加成几率
        /// </summary>
        [ProtoMember(8)]
        public List<int> RateList ;

        /// <summary>
        /// 加成属性值
        /// </summary>
        [ProtoMember(9)]
        public List<int[]> AddNumList;
    }
}
