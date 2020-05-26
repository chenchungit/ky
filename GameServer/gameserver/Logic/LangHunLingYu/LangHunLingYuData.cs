using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Server.Data;
using KF.Contract.Data;
using Tmsk.Contract;

namespace GameServer.Logic
{
    public enum LangHunLingYuBirthPointTypes
    {
        ShouChengFang = 0,
        GongChengFang1 = 1,
        GongChengFang2 = 2,
        GongChengFang3 = 3,
    }

    /// <summary>
    /// 每日奖励配置项
    /// </summary>
    public class LangHunLingYuEveryDayAwardsItem
    {
        public int ID;
        public int ZhiWu;
        public int DayZhanGong;
        public long DayExp;
        public AwardsItemList DayGoods = new AwardsItemList();
    }

    public class CityLevelInfo
    {
        public int ID;
        public int CityLevel;
        public int CityNum;
        public int MaxNum;
        public List<TimeSpan> BaoMingTime = new List<TimeSpan>();
        public List<int> AttackWeekDay;
        public List<TimeSpan> AttackTime = new List<TimeSpan>();
        public AwardsItemList Award = new AwardsItemList();
        public AwardsItemList DayAward = new AwardsItemList();
        public int ZhanMengZiJin; //报名消耗自己
    }

    public class LangHunLingYuSceneInfo
    {
        public int Id;
        public int MapCode;
        public int MapCode_LongTa;

        public int MinZhuanSheng = 1;
        public int MinLevel = 1;
        public int MaxZhuanSheng = 1;
        public int MaxLevel = 1;

        /// <summary>
        /// 活动时间区间列表,依次为:开始时间1,结束时间1,开始时间2,结束时间2...
        /// </summary>
        public List<TimeSpan> TimePoints = new List<TimeSpan>();

        // 每个时间相对于一天经过的秒数
        public List<double> SecondsOfDay = new List<double>();

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
        /// 总时间(额外60秒作为副本删除时间)
        /// </summary>
        public int TotalSecs { get { return WaitingEnterSecs + PrepareSecs + FightingSecs + ClearRolesSecs + 1200; } }

        /// <summary>
        /// 开始报名的提前时间(秒)
        /// </summary>
        public int SignUpStartSecs;

        /// <summary>
        /// 停止报名时间
        /// </summary>
        public int SignUpEndSecs;

        /// <summary>
        /// 奖励
        /// </summary>
        public AwardsItemList WinAwardsItemList = new AwardsItemList();
    }

    public class LangHunLingYuBirthPoint
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

    public class LangHunLingYuRankAward
    {
        public int ID;
        public int StarRank;
        public int EndRank;
        public AwardsItemList Award = new AwardsItemList();
    }

    public class LangHunLingYuRoleMiniData
    {
        public int RoleId;
        public int ZoneId;
        public string RoleName;
        public int BattleWitchSide;
    }

    /// <summary>
    /// 幻影寺院圣杯上下文对象
    /// </summary>
    public class LangHunLingYuClientContextData
    {
        public int RoleId;
        public int ServerId;
        public int BattleWhichSide;

        /// <summary>
        /// 当前战斗的场景对象
        /// </summary>
        public int CityLevel;
    }

    /// <summary>
    /// 勇者战场副本上下文对象
    /// </summary>
    public class LangHunLingYuContextData
    {
        public int TotalSignUpCount;
        public int SuccessRoleCount;
        public int FaildRoleCount;
        public int ScoreFromCaiJi;
        public int ScoreFromKill;
        public int ScoreFromContinueKill;
        public int ScoreFromBreakContinueKill;
        public int ScoreFromBoss;
    }

    public class LangHunLingYuMonsterContextData
    {
        public QiZhiConfig QiZhiConfigData;
        public LangHunLingYuScene SceneObject;
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class LangHunLingYuData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();

        #region 活动配置

        /// <summary>
        /// 复活和地图传送位置
        /// </summary>
        public Dictionary<int, List<MapBirthPoint>> MapBirthPointListDict = new Dictionary<int, List<MapBirthPoint>>();

        /// <summary>
        /// NPCID到旗帜配置字典
        /// </summary>
        public Dictionary<int, QiZhiConfig> NPCID2QiZhiConfigDict = new Dictionary<int, QiZhiConfig>();

        /// <summary>
        /// 罗兰城战旗帜Buff拥有者信息列表
        /// </summary>
        public List<LangHunLingYuQiZhiBuffOwnerData> QiZhiBuffOwnerDataList = new List<LangHunLingYuQiZhiBuffOwnerData>();

        public List<int> MapCodeList = new List<int>();
        public List<int> MapCodeLongTaList = new List<int>();
        public int[] BattleWitchSideJunQi = null;

        public int CutLifeV = 10;
        public HashSet<int> JunQiMonsterHashSet = new HashSet<int>();

        /// <summary>
        /// 最小的转生等级
        /// </summary>
        public int MinZhuanSheng = 0;

        /// <summary>
        /// 最低等级
        /// </summary>
        public int MinLevel = 0;

        /// <summary>
        /// 活动前的预留截止报名时间（秒）
        /// </summary>
        public long EnrollTime = 1800;


        /// <summary>
        /// 拥有后,享有特殊复活点的旗帜NPC的ID
        /// </summary>
        public int SuperQiZhiNpcId = 80000;

        /// <summary>
        /// 旗帜Buff禁用的参数
        /// </summary>
        public Dictionary<int, double[]> QiZhiBuffDisableParamsDict = new Dictionary<int, double[]>();

        /// <summary>
        /// 旗帜Buff启用的参数
        /// </summary>
        public Dictionary<int, double[]> QiZhiBuffEnableParamsDict = new Dictionary<int, double[]>();

        public int SuperQiZhiOwnerBirthPosX;
        public int SuperQiZhiOwnerBirthPosY;

        /// <summary>
        /// 段位排行奖励配置
        /// </summary>
        public Dictionary<RangeKey, LangHunLingYuSceneInfo> LevelRangeSceneIdDict = new Dictionary<RangeKey, LangHunLingYuSceneInfo>(RangeKey.Comparer);

        /// <summary>
        /// 活动配置字典
        /// </summary>
        public Dictionary<int, LangHunLingYuSceneInfo> SceneDataDict = new Dictionary<int, LangHunLingYuSceneInfo>();

        /// <summary>
        /// 活动配置列表（区分在于地图编号）
        /// </summary>
        public List<LangHunLingYuSceneInfo> SceneDataList = new List<LangHunLingYuSceneInfo>();

        public int SceneInfoId = 1;

        /// <summary>
        /// 每日奖励配置字典
        /// </summary>
        public Dictionary<int, CityLevelInfo> CityLevelInfoDict = new Dictionary<int, CityLevelInfo>();

        /// <summary>
        /// 活动结果数据，须保证全部发送给跨服中心
        /// </summary>
        public Queue<LangHunLingYuStatisticalData> StatisticalDataQueue = new Queue<LangHunLingYuStatisticalData>();

        #endregion 活动配置

        #region 活动时间

        /// <summary>
        /// 开服几天后这个功能开启
        /// </summary>
        public int GongNengOpenDaysFromKaiFu = 5;

        /// <summary>
        /// 申请时间开始
        /// </summary>
        public TimeSpan NoRequestTimeStart;

        /// <summary>
        /// 申请时间结束
        /// </summary>
        public TimeSpan NoRequestTimeEnd;

        /// <summary>
        /// 活动的DayOfWeek
        /// </summary>
        public int[] WeekPoints = new int[0];

        /// <summary>
        /// 活动开始时间
        /// </summary>
        public DateTime TimePoints;

        /// <summary>
        /// 下次王城战的举行的时间
        /// </summary>
        public DateTime WangChengZhanFightingDateTime;

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
        /// 播放竞价结果公告的状态(是否已播报)
        /// </summary>
        public bool CanRequestState = false;

        #endregion 活动时间

        #region 运行时数据

        public int MapGridWidth = 100;

        public int MapGridHeight = 100;

        /// <summary>
        /// 王族所在的帮会ID
        /// </summary>
        public long ChengHaoBHid = 0;

        /// <summary>
        /// 占领皇宫决定胜负的最长时间
        /// </summary>
        public int MaxTakingHuangGongSecs = (5 * 1000);

        /// <summary>
        /// 副本场景数据
        /// </summary>
        public ConcurrentDictionary<int, LangHunLingYuScene> SceneDict = new ConcurrentDictionary<int, LangHunLingYuScene>();

        public Dictionary<long, LangHunLingYuCityData> BangHui2CityDict = new Dictionary<long, LangHunLingYuCityData>();

        public Dictionary<int, LangHunLingYuCityData> CityDataDict = new Dictionary<int, LangHunLingYuCityData>();

        public Dictionary<long, LangHunLingYuBangHuiData> BangHuiDataDict = new Dictionary<long, LangHunLingYuBangHuiData>();

        /// <summary>
        /// 4个其他城池的数组
        /// </summary>
        public Dictionary<int, List<int>> OtherCityList = null;

        /// <summary>
        /// 圣域城主历史记录
        /// </summary>
        public List<LangHunLingYuKingHist> OwnerHistList = null;

        /// <summary>
        /// 全服的参与圣域争霸的帮会的信息
        /// </summary>
        public Dictionary<long, LangHunLingYuBangHuiDataEx> BangHuiDataExDict = new Dictionary<long, LangHunLingYuBangHuiDataEx>();

        /// <summary>
        /// 圣域争霸的所有城池的信息
        /// </summary>
        public Dictionary<int, LangHunLingYuCityDataEx> CityDataExDict = new Dictionary<int, LangHunLingYuCityDataEx>();

        /// <summary>
        /// 所有副本字典
        /// </summary>
        public Dictionary<int, LangHunLingYuFuBenData> FuBenDataDict = new Dictionary<int, LangHunLingYuFuBenData>();

        /// <summary>
        /// 缓存本场战斗的帮会ID对应的帮会信息
        /// </summary>
        public Dictionary<int, BangHuiMiniData> BangHuiMiniDataCacheDict = new Dictionary<int, BangHuiMiniData>();

        #endregion 运行时数据
    }
}
