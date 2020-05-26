using System.Collections.Generic;
using ProtoBuf;
using System;

namespace Server.Data
{
    /// <summary>
    /// 角色的天梯数据
    /// </summary>
    [ProtoContract]
    public class RoleTianTiData
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(1)]
        public int RoleId;

        /// <summary> 
        /// 段位ID
        /// </summary>
        [ProtoMember(2)]
        public int DuanWeiId;

        /// <summary>
        /// 段位积分
        /// </summary>
        [ProtoMember(3)]
        public int DuanWeiJiFen;

        /// <summary>
        /// 段位排名
        /// </summary>
        [ProtoMember(4)]
        public int DuanWeiRank;

        /// <summary>
        /// 连胜数
        /// </summary>
        [ProtoMember(5)]
        public int LianSheng;

        /// <summary>
        /// 胜利总次数
        /// </summary>
        [ProtoMember(6)]
        public int SuccessCount;

        /// <summary>
        /// 战斗总次数
        /// </summary>
        [ProtoMember(7)]
        public int FightCount;

        /// <summary>
        /// 今日战斗次数(前几次可获得荣耀)
        /// </summary>
        [ProtoMember(8)]
        public int TodayFightCount;

        /// <summary>
        /// 月度段位排行
        /// </summary>
        [ProtoMember(9)]
        public int MonthDuanWeiRank;

        /// <summary>
        /// 月排名奖励领取时间
        /// </summary>
        [ProtoMember(10)]
        public DateTime FetchMonthDuanWeiRankAwardsTime;

        /// <summary>
        /// 荣耀值
        /// </summary>
        [ProtoMember(11)]
        public int RongYao;

        /// <summary>
        /// 上次战斗的日期
        /// </summary>
        [ProtoMember(12)]
        public int LastFightDayId;

        /// <summary>
        /// 排名更新时间
        /// </summary>
        [ProtoMember(13)]
        public DateTime RankUpdateTime;

        /// <summary>
        /// 今日得到段位积分
        /// </summary>
        [ProtoMember(14)]
        public int DayDuanWeiJiFen;
    }

    /// <summary>
    /// 战斗结束的结果和奖励
    /// </summary>
    [ProtoContract]
    public class TianTiAwardsData
    {
        /// <summary>
        /// 战斗结果(0失败,1胜利,-1进入超时)
        /// </summary>
        [ProtoMember(1)]
        public int Success;

        /// <summary>
        /// 获得段位积分(可能为负值)
        /// </summary>
        [ProtoMember(2)]
        public int DuanWeiJiFen;

        /// <summary>
        /// 获得荣耀
        /// </summary>
        [ProtoMember(3)]
        public int RongYao;

        /// <summary>
        /// 获得段位积分(连胜加成部分)
        /// </summary>
        [ProtoMember(4)]
        public int LianShengJiFen;

        /// <summary>
        /// 新的段位Id
        /// </summary>
        [ProtoMember(5)]
        public int DuanWeiId;
    }

    /// <summary>
    /// 角色的天梯排行信息
    /// </summary>
    [ProtoContract]
    public class TianTiPaiHangRoleData
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(1)]
        public int RoleId;

        /// <summary>
        /// 名字
        /// </summary>
        [ProtoMember(2)]
        public string RoleName;

        /// <summary>
        /// 职业
        /// </summary>
        [ProtoMember(3)]
        public int Occupation;

        /// <summary>
        /// 区号
        /// </summary>
        [ProtoMember(4)]
        public int ZoneId;

        /// <summary>
        /// 战力
        /// </summary>
        [ProtoMember(5)]
        public int ZhanLi;

        /// <summary>
        /// 段位ID
        /// </summary>
        [ProtoMember(6)]
        public int DuanWeiId;

        /// <summary>
        /// 段位积分
        /// </summary>
        [ProtoMember(7)]
        public int DuanWeiJiFen;

        /// <summary>
        /// 段位排名
        /// </summary>
        [ProtoMember(8)]
        public int DuanWeiRank;

        /// <summary>
        /// 形象数据(类型为RoleData4Selector)
        /// </summary>
        [ProtoMember(9)]
        public RoleData4Selector RoleData4Selector;

        /// <summary>
        /// 众神争霸成绩，参考EZhengBaGrade
        /// </summary>
        [ProtoMember(10)]
        public int ZhengBaGrade;
        
        /// <summary>
        /// 众神争霸16强分组, 范围1-16，只有前16强选手才有意义
        /// </summary>
        [ProtoMember(11)]
        public int ZhengBaGroup;

        /// <summary>
        /// 众神争霸当前状态, 无/晋级/淘汰，参考EZhengBaState
        /// </summary>
        [ProtoMember(12)]
        public int ZhengBaState;
    }

    /// <summary>
    /// 天梯排行列表数据
    /// </summary>
    [ProtoContract]
    public class TianTiMonthPaiHangData
    {
        /// <summary>
        /// 请求者自己的排行数据
        /// </summary>
        [ProtoMember(1)]
        public TianTiPaiHangRoleData SelfPaiHangRoleData;

        /// <summary>
        /// 月排行10名
        /// </summary>
        [ProtoMember(2)]
        public List<TianTiPaiHangRoleData> PaiHangRoleDataList;
    }

    /// <summary>
    /// 天梯排行列表数据
    /// </summary>
    [ProtoContract]
    public class TianTiDataAndDayPaiHang
    {
        /// <summary>
        /// 请求者自己的排行数据
        /// </summary>
        [ProtoMember(1)]
        public RoleTianTiData TianTiData;

        /// <summary>
        /// 日排行3名
        /// </summary>
        [ProtoMember(2)]
        public List<TianTiPaiHangRoleData> PaiHangRoleDataList;
    }

    /// <summary>
    /// 天梯战报记录
    /// </summary>
    [ProtoContract]
    public class TianTiLogItemData
    {
        /// <summary>
        /// 区号
        /// </summary>
        [ProtoMember(1)]
        public int ZoneId1;

        /// <summary> 
        /// 角色名 
        /// </summary>
        [ProtoMember(2)]
        public string RoleName1;

        /// <summary>
        /// 对方区号
        /// </summary>
        [ProtoMember(3)]
        public int ZoneId2;

        /// <summary>
        /// 对方角色名
        /// </summary>
        [ProtoMember(4)]
        public string RoleName2;

        /// <summary>
        /// 战斗结果(0失败,1胜利)
        /// </summary>
        [ProtoMember(5)]
        public int Success;

        /// <summary>
        /// 奖励段位积分
        /// </summary>
        [ProtoMember(6)]
        public int DuanWeiJiFenAward;

        /// <summary>
        /// 奖励荣耀值
        /// </summary>
        [ProtoMember(7)]
        public int RongYaoAward;

        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(8)]
        public int RoleId;

        /// <summary>
        /// 结束时间
        /// </summary>
        [ProtoMember(9)]
        public DateTime EndTime;
    }
}
