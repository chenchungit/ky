using ProtoBuf;

namespace GameServer.Logic
{
    [ProtoContract]
    public class HuanYingSiYuanScoreInfoData
    {
        /// <summary>
        /// 1队得分
        /// </summary>
        [ProtoMember(1)]
        public int Score1;

        /// <summary>
        /// 2队得分
        /// </summary>
        [ProtoMember(2)]
        public int Score2;

        /// <summary>
        /// 1队人数
        /// </summary>
        [ProtoMember(3)]
        public long Count1;

        /// <summary>
        /// 2队人数
        /// </summary>
        [ProtoMember(4)]
        public int Count2;
    }

    [ProtoContract]
    public class HuanYingSiYuanAwardsData
    {
        /// <summary>
        /// 胜方
        /// </summary>
        [ProtoMember(1)]
        public int SuccessSide;

        /// <summary>
        /// 经验
        /// </summary>
        [ProtoMember(2)]
        public long Exp;

        /// <summary>
        /// 声望
        /// </summary>
        [ProtoMember(3)]
        public int ShengWang;

        /// <summary>
        /// 奖励成就点
        /// </summary>
        [ProtoMember(4)]
        public int ChengJiuAward;

        /// <summary>
        /// 奖励倍数
        /// </summary>
        [ProtoMember(5)]
        public int AwardsRate;
    }

    /// <summary>
    /// 分数增加
    /// </summary>
    [ProtoContract]
    public class HuanYingSiYuanAddScore
    {
        /// <summary>
        /// 分数
        /// </summary>
        [ProtoMember(1)]
        public int Score;

        /// <summary>
        /// 区号
        /// </summary>
        [ProtoMember(2)]
        public int ZoneID;

        /// <summary>
        /// 名字
        /// </summary>
        [ProtoMember(3)]
        public string Name = "";

        /// <summary>
        /// 阵营方
        /// </summary>
        [ProtoMember(4)]
        public int Side;

        /// <summary>
        /// 得分的角色ID
        /// </summary>
        [ProtoMember(5)]
        public int RoleId;

        /// <summary>
        /// 如果是连杀得分,这个值表示连杀数,否则为0
        /// </summary>
        [ProtoMember(6)]
        public int ByLianShaNum;

        /// <summary>
        /// 职业
        /// </summary>
        [ProtoMember(7)]
        public int Occupation;
    }

    /// <summary>
    /// 连杀结构
    /// </summary>
    [ProtoContract]
    public class HuanYingSiYuanLianSha
    {
        /// <summary>
        /// 区号
        /// </summary>
        [ProtoMember(1)]
        public int ZoneID;

        /// <summary>
        /// 名字
        /// </summary>
        [ProtoMember(2)]
        public string Name = "";

        /// <summary>
        /// 连杀类型
        /// </summary>
        [ProtoMember(3)]
        public int LianShaType;

        /// <summary>
        /// 职业
        /// </summary>
        [ProtoMember(4)]
        public int Occupation;

        /// <summary>
        /// 额外的分数
        /// </summary>
        [ProtoMember(5)]
        public int ExtScore;

        /// <summary>
        /// 阵营方
        /// </summary>
        [ProtoMember(6)]
        public int Side;
    }

    /// <summary>
    /// 终结连杀结构
    /// </summary>
    [ProtoContract]
    public class HuanYingSiYuanLianshaOver
    {
        /// <summary>
        /// 击杀者区号
        /// </summary>
        [ProtoMember(1)]
        public int KillerZoneID;

        /// <summary> 
        /// 击杀者名字 
        /// </summary>
        [ProtoMember(2)]
        public string KillerName = "";

        /// <summary>
        /// 击杀者职业
        /// </summary>
        [ProtoMember(3)]
        public int KillerOccupation;

        /// <summary>
        /// 击杀者阵营方
        /// </summary>
        [ProtoMember(4)]
        public int KillerSide;

        /// <summary>
        /// 被杀者区号
        /// </summary>
        [ProtoMember(5)]
        public int KilledZoneID;

        /// <summary>
        /// 被杀者名字
        /// </summary>
        [ProtoMember(6)]
        public string KilledName = "";

        /// <summary>
        /// 被杀者职业
        /// </summary>
        [ProtoMember(7)]
        public int KilledOccupation;

        /// <summary>
        /// 被杀者阵营方
        /// </summary>
        [ProtoMember(8)]
        public int KilledSide;

        /// <summary>
        /// 额外的分数
        /// </summary>
        [ProtoMember(9)]
        public int ExtScore;
    }
}
