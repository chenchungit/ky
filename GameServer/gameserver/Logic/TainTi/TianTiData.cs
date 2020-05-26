using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using System.Xml.Linq;
using GameServer.Core.GameEvent.EventOjectImpl;
using KF.Contract.Data;

namespace GameServer.Logic
{
    public class TianTiBirthPoint
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID;

        /// <summary>
        /// X坐标
        /// </summary>
        public int PosX;

        /// <summary>
        /// Y坐标
        /// </summary>
        public int PosY;

        /// <summary>
        /// 随机半径（米）
        /// </summary>
        public int BirthRadius; 
    }

    public class TianTiDuanWei
    {
        public int ID;
        public int NeedDuanWeiJiFen;
        public int WinJiFen;
        public int LoseJiFen;
        public int RongYaoNum;
        public int WinRongYu;
        public int LoseRongYu;
    }

    public class DuanWeiRankAward
    {
        public int ID;
        public int StarRank;
        public int EndRank;
        public AwardsItemList Award = new AwardsItemList();
    }

    public class RongYaoRankAward
    {
        public int ID;
        public int StarRank;
        public int EndRank;
        public AwardsItemList Award = new AwardsItemList();
    }

    public class TianTiRoleMiniData
    {
        public int RoleId;
        public int ZoneId;
        public string RoleName;
        public int BattleWitchSide;
        public int DuanWeiId;
    }

    public class TianTiFuBenItem
    {
        public int GameId;
        public int FuBenSeqId;
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class TianTiData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();

        #region 活动配置

        /// <summary>
        /// 等级区间对应的组ID字典
        /// </summary>
        public Dictionary<RangeKey, int> Range2GroupIndexDict = new Dictionary<RangeKey, int>(RangeKey.Comparer);

        /// <summary>
        /// 复活和地图传送位置
        /// </summary>
        public Dictionary<int, TianTiBirthPoint> MapBirthPointDict = new Dictionary<int, TianTiBirthPoint>();

        /// <summary>
        /// 段位排行奖励配置
        /// </summary>
        public Dictionary<RangeKey, DuanWeiRankAward> DuanWeiRankAwardDict = new Dictionary<RangeKey, DuanWeiRankAward>(RangeKey.Comparer);

        /// <summary>
        /// 荣耀排行奖励配置
        /// </summary>
        public Dictionary<RangeKey, RongYaoRankAward> RongYaoRankAwardDict = new Dictionary<RangeKey, RongYaoRankAward>(RangeKey.Comparer);

        /// <summary>
        /// key: gameId
        /// value: fuben SEQ
        /// </summary>
        public Dictionary<int, int> GameId2FuBenSeq = new Dictionary<int, int>();

        /// <summary>
        /// 幻影寺院地图编号
        /// </summary>
        public Dictionary<int, int> MapCodeDict = new Dictionary<int, int>();
        public List<int> MapCodeList = new List<int>();

        public string TimePointsStr;

        /// <summary>
        /// 活动时间列表（开始|结束|开始|结束...)
        /// </summary>
        public List<TimeSpan> TimePoints = new List<TimeSpan>();

        /// <summary>
        /// 最大参与战盟数(包括守城方)
        /// </summary>
        public int MaxZhanMengNum = 4;

        /// <summary>
        /// 竞标城战所需增加的战盟资金
        /// </summary>
        public TimeSpan RefreshTime = new TimeSpan(3, 0, 0);

        /// <summary>
        /// 最小的转生等级
        /// </summary>
        public int MinZhuanSheng = 1;

        /// <summary>
        /// 最低等级
        /// </summary>
        public int MinLevel = 1;

        /// <summary>
        /// 最小参战战盟数
        /// </summary>
        public int MinRequestNum = 1;

        /// <summary>
        /// 最大进入人数
        /// </summary>
        public int MaxEnterNum = 10;

        /// <summary>
        /// 副本ID
        /// </summary>
        public int FuBenId = 13000;

        /// <summary>
        /// 圣杯最大持有不交付的时间(秒)
        /// </summary>
        public int HoldShengBeiSecs = 60;

        /// <summary>
        /// 提交圣杯的最小消耗时间(防外挂)
        /// </summary>
        public int MinSubmitShengBeiSecs = 13;

        #endregion 活动配置

        #region 活动时间

        /// <summary>
        /// 准备进入时间
        /// </summary>
        public int WaitingEnterSecs;

        /// <summary>
        /// 准备时间
        /// </summary>
        public int PrepareSecs;

        /// <summary>
        /// 战斗时间
        /// </summary>
        public int FightingSecs;

        /// <summary>
        /// 清场时间
        /// </summary>
        public int ClearRolesSecs;

        /// <summary>
        /// 总时间
        /// </summary>
        public int TotalSecs { get { return WaitingEnterSecs + PrepareSecs + FightingSecs + ClearRolesSecs; } }

        #endregion 活动时间

        #region 奖励配置

        /// <summary>
        /// 圣杯信息字典
        /// </summary>
        public Dictionary<int, TianTiDuanWei> TianTiDuanWeiDict = new Dictionary<int, TianTiDuanWei>();

        public Dictionary<RangeKey, int> DuanWeiJiFenRangeDuanWeiIdDict = new Dictionary<RangeKey, int>(RangeKey.Comparer);

        /// <summary>
        /// 奖励成就
        /// </summary>
        public int TempleMirageAwardChengJiu;

        /// <summary>
        /// 奖励声望
        /// </summary>
        public int TempleMirageAwardShengWang;

        /// <summary>
        /// 获胜所需积分
        /// </summary>
        public int WinDuanWeiJiFen = 1000;

        /// <summary>
        /// 杀对付的人得分
        /// </summary>
        public int LoseDuanWeiJiFen = 8;

        /// <summary>
        /// 每日获得段位积分的次数
        /// </summary>
        public int DuanWeiJiFenNum = 5;

        /// <summary>
        /// 每日多倍奖励倍数
        /// </summary>
        public int TempleMirageWinExtraRate = 10;

        /// <summary>
        /// 副本获得物品是否绑定
        /// </summary>
        public int FuBenGoodsBinding = 1;

        /// <summary>
        /// 每日获取段位积分上限值
        /// </summary>
        public int MaxTianTiJiFen = 600000;

        #endregion 奖励配置

        #region 运行时数据

        //public Dictionary<int, TianTiFuBenItem> TianTiFuBenItemDict = new Dictionary<int, TianTiFuBenItem>();

        /// <summary>
        /// 排行更新时间
        /// </summary>
        public DateTime ModifyTime;

        /// <summary>
        /// 最大排行数
        /// </summary>
        public int MaxPaiMingRank = 100;

        public int MaxDayPaiMingListCount = 10;
        public int MaxMonthPaiMingListCount = 10;
        public Dictionary<int, TianTiPaiHangRoleData> TianTiPaiHangRoleDataDict = new Dictionary<int, TianTiPaiHangRoleData>();
        public List<TianTiPaiHangRoleData> TianTiPaiHangRoleDataList = new List<TianTiPaiHangRoleData>();
        public Dictionary<int, TianTiPaiHangRoleData> TianTiMonthPaiHangRoleDataDict = new Dictionary<int, TianTiPaiHangRoleData>();
        public List<TianTiPaiHangRoleData> TianTiMonthPaiHangRoleDataList = new List<TianTiPaiHangRoleData>();

        #endregion 运行时数据
    }
}
