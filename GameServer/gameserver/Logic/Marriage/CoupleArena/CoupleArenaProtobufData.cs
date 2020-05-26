using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProtoBuf;
using Server.Data;

namespace GameServer.Logic.Marriage.CoupleArena
{
    /// <summary>
    /// 夫妻竞技场角色匹配状态
    /// </summary>
    [ProtoContract]
    public class CoupleArenaRoleStateData
    {
        /// <summary>
        /// 角色id
        /// </summary>
        [ProtoMember(1)]
        public int RoleId;

        /// <summary>
        /// 匹配状态, 参考枚举ECoupleArenaRoleMatchState
        /// </summary>
        [ProtoMember(2)]
        public int MatchState;
    }

    /// <summary>
    /// 夫妻竞技场主界面信息
    /// </summary>
    [ProtoContract]
    public class CoupleArenaMainData
    {
        /// <summary>
        /// 夫妻数据
        /// </summary>
        [ProtoMember(1)]
        public CoupleArenaCoupleJingJiData JingJiData;

        /// <summary>
        /// 本周获得荣耀次数, 丈夫和妻子的值单独计算
        /// </summary>
        [ProtoMember(2)]
        public int WeekGetRongYaoTimes;

        /// <summary>
        /// 通知可以领取的奖励id
        /// CoupleWarAward.xml
        /// </summary>
        [ProtoMember(3)]
        public int CanGetAwardId;
    }

    /// <summary>
    /// 夫妻竞技场夫妻数据
    /// </summary>
    [ProtoContract]
    public class CoupleArenaCoupleJingJiData
    {
        /// <summary>
        /// 丈夫角色id
        /// </summary>
        [ProtoMember(1)]
        public int ManRoleId;

        /// <summary>
        /// 丈夫zoneid
        /// </summary>
        [ProtoMember(2)]
        public int ManZoneId;

        /// <summary>
        /// 丈夫形象
        /// </summary>
        [ProtoMember(3)]
        public RoleData4Selector ManSelector;

        /// <summary>
        /// 妻子角色id
        /// </summary>
        [ProtoMember(4)]
        public int WifeRoleId;

        /// <summary>
        /// 妻子zoneid
        /// </summary>
        [ProtoMember(5)]
        public int WifeZoneId;

        /// <summary>
        /// 妻子形象
        /// </summary>
        [ProtoMember(6)]
        public RoleData4Selector WifeSelector;

        /// <summary>
        /// 总战斗次数
        /// </summary>
        [ProtoMember(7)]
        public int TotalFightTimes;

        /// <summary>
        /// 总胜利次数
        /// </summary>
        [ProtoMember(8)]
        public int WinFightTimes;

        /// <summary>
        /// 连胜次数
        /// </summary>
        [ProtoMember(9)]
        public int LianShengTimes;

        /// <summary>
        /// 段位type
        /// </summary>
        [ProtoMember(10)]
        public int DuanWeiType;

        /// <summary>
        /// 段位等级
        /// </summary>
        [ProtoMember(11)]
        public int DuanWeiLevel;

        /// <summary>
        /// 积分
        /// </summary>
        [ProtoMember(12)]
        public int JiFen;

        /// <summary>
        /// 排名
        /// </summary>
        [ProtoMember(13)]
        public int Rank;

        /// <summary>
        /// 是否离婚了
        /// </summary>
        public int IsDivorced;
    }

    /// <summary>
    /// 夫妻竞技场 战报项
    /// </summary>
    [ProtoContract]
    public class CoupleArenaZhanBaoItemData
    {
        /// <summary>
        /// 被挑战夫妻的丈夫区号
        /// </summary>
        [ProtoMember(1)]
        public int TargetManZoneId;

        /// <summary>
        /// 被挑战夫妻的丈夫角色名
        /// </summary>
        [ProtoMember(2)]
        public string TargetManRoleName;

        /// <summary>
        /// 被挑战夫妻的妻子区号
        /// </summary>
        [ProtoMember(3)]
        public int TargetWifeZoneId;

        /// <summary>
        /// 被挑战夫妻的其妻子角色名
        /// </summary>
        [ProtoMember(4)]
        public string TargetWifeRoleName;

        /// <summary>
        /// 是否胜利
        /// </summary>
        [ProtoMember(5)]
        public bool IsWin;

        /// <summary>
        /// 获得积分
        /// </summary>
        [ProtoMember(6)]
        public int GetJiFen;
    }

    [ProtoContract]
    public class CoupleArenaZhanBaoSaveDbData
    {
        [ProtoMember(1)]
        public CoupleArenaZhanBaoItemData ZhanBao;

        [ProtoMember(2)]
        public int FirstWeekday;

        [ProtoMember(3)]
        public int FromMan;

        [ProtoMember(4)]
        public int FromWife;

        [ProtoMember(5)]
        public int ToMan;

        [ProtoMember(6)]
        public int ToWife;
    }

    /// <summary>
    /// 夫妻竞技场排行榜
    /// </summary>
    [ProtoContract]
    public class CoupleArenaPaiHangData
    {
        /// <summary>
        /// 排行榜
        /// </summary>
        [ProtoMember(1)]
        public List<CoupleArenaCoupleJingJiData> PaiHang;
    }

    /// <summary>
    /// 夫妻竞技场战斗结果
    /// </summary>
    [ProtoContract]
    public class CoupleArenaPkResultData
    {
        /// <summary>
        /// 参考 ECoupleArenaPkResult
        /// 无论是无效、胜利、还是失败，其余字段均有效
        /// </summary>
        [ProtoMember(1)]
        public int PKResult;

        /// <summary>
        /// 获得荣耀
        /// </summary>
        [ProtoMember(2)]
        public int GetRongYao;

        /// <summary>
        /// 获得积分
        /// </summary>
        [ProtoMember(3)]
        public int GetJiFen;

        /// <summary>
        /// 段位类型
        /// </summary>
        [ProtoMember(4)]
        public int DuanWeiType;

        /// <summary>
        /// 段位等级
        /// </summary>
        [ProtoMember(5)]
        public int DuanWeiLevel;
    }

    /// <summary>
    /// 夫妻竞技场buff持有信息
    /// </summary>
    [ProtoContract]
    public class CoupleArenaBuffHoldData
    {
        /// <summary>
        /// 是否有人持有真爱buff
        /// </summary>
        [ProtoMember(1, IsRequired=true)]
        public bool IsZhenAiBuffValid;

        [ProtoMember(2)]
        public int ZhenAiHolderZoneId;

        [ProtoMember(3)]
        public string ZhenAiHolderRname;

        /// <summary>
        /// 是否有人持有勇气buff
        /// </summary>
        [ProtoMember(4, IsRequired=true)]
        public bool IsYongQiBuffValid;

        [ProtoMember(5)]
        public int YongQiHolderZoneId;

        [ProtoMember(6)]
        public string YongQiHolderRname;
    }
}
