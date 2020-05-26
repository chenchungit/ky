using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    public class PrestigeMedalBasicData
    {
        /// <summary>
        /// 勋章id
        /// </summary>
        [ProtoMember(1)]
        public int MedalID = 0;

        /// <summary>
        /// 勋章名称
        /// </summary>
        [ProtoMember(2)]
        public string MedalName = "";

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
        /// 命中上限
        /// </summary>
        [ProtoMember(6)]
        public int HitMax = 0;

        /// <summary>
        /// 声望消耗
        /// </summary>
        [ProtoMember(7)]
        public int PrestigeCost = 0;

        /// <summary>
        /// 加成几率
        /// </summary>
        [ProtoMember(8)]
        public List<int> RateList;

        /// <summary>
        /// 加成属性值
        /// </summary>
        [ProtoMember(9)]
        public List<int[]> AddNumList;
    }
}
