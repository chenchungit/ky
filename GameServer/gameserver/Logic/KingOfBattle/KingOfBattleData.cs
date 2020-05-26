using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Server.Data;
using KF.Contract.Data;
using Tmsk.Contract;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 幻影寺院圣杯上下文对象
    /// </summary>
    public class KingOfBattleClientContextData
    {
        public int RoleId;
        public int ServerId;
        public int BattleWhichSide;

        /// <summary>
        /// 总得分,包括采集和击杀获得
        /// </summary>
        public int TotalScore;

        /// <summary>
        /// 连续击杀数
        /// </summary>
        public int KillNum;

        /// <summary>
        /// 伤害Boss未计积分的部分 extensionID vs 伤害
        /// </summary>
        public Dictionary<int, double> InjureBossDeltaDict = new Dictionary<int, double>();
    }

    public class KingOfBattleSceneInfo
    {
        public int Id;
        public int MapCode;

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
        public int TotalSecs { get { return WaitingEnterSecs + PrepareSecs + FightingSecs + ClearRolesSecs + 120; } }

        /// <summary>
        /// 开始报名的提前时间(秒)
        /// </summary>
        public int SignUpStartSecs;

        /// <summary>
        /// 停止报名时间
        /// </summary>
        public int SignUpEndSecs;

        #region 奖励

        public long Exp;
        public int BandJinBi;

        public int AwardMinZhuanSheng = 1;
        public int AwardMinLevel = 1;
        public int AwardMaxZhuanSheng = 1;
        public int AwardMaxLevel = 1;

        public AwardsItemList WinAwardsItemList = new AwardsItemList();
        public AwardsItemList LoseAwardsItemList = new AwardsItemList();

        #endregion 奖励
    }

    public class KingOfBattleBirthPoint
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

    public class KingOfBattleQiZhiConfig
    {
        public int NPCID;

        public int PosX;
        
        public int PosY;

        public int QiZhiMonsterID;

        //运行时数据
        public int BattleWhichSide;

        public bool Alive;

        public long DeadTicks;

        public object Clone()
        {
            KingOfBattleQiZhiConfig obj = new KingOfBattleQiZhiConfig()
            {
                NPCID = NPCID,
                PosX = PosX,
                PosY = PosY,
                QiZhiMonsterID = QiZhiMonsterID,
            };
            return obj;
        }
    }

    public class KingOfBattleStoreConfig
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID;

        /// <summary>
        /// goodsdata
        /// </summary>
        public GoodsData SaleData = null;

        /// <summary>
        /// WangZheJiFen
        /// </summary>
        public int JiFen;

        /// <summary>
        /// SinglePurchase
        /// </summary>
        public int SinglePurchase;

        /// <summary>
        /// 随机数开始
        /// </summary>
        public int BeginNum;

        /// <summary>
        /// 随机数结束
        /// </summary>
        public int EndNum;

        /// <summary>
        /// RandEndNum - RandEndNum + 1 计算随机时的辅助变量
        /// </summary>
        public int RandNumMinus = 0;

        /// <summary>
        /// 随机忽略 计算随机时的辅助变量
        /// </summary>
        public bool RandSkip = false;
    }

    public class KingOfBattleRandomBuff
    {
        // 道具ID
        public int GoodsID;

        // 随机百分比
        public double Pct;
    }

    public enum KingOfBattleMonsterType
    {
        KingOfBattle_None = 0,
        KingOfBattle_BaoLei = 1, // 水晶堡垒
        KingOfBattle_TowerSecond = 2, // 二塔
        KingOfBattle_TowerFirst = 3, // 一塔
        KingOfBattle_Boss = 4, // 野外Boss
    }

    public class KingOfBattleDynamicMonsterItem
    {
        public int Id; // 流水号
        public int MapCode;
        public int MonsterID;
        public int PosX;
        public int PosY;
        public int Num; //刷怪数量
        public int Radius; //刷怪范围(厘米)
        public int DelayBirthMs; // 延迟出生时间(毫秒)
        public int PursuitRadius; //追击范围(厘米)

        // 怪物类型 1=双方水晶堡垒、2=二塔 3=一塔、4=野外BOSS
        public int MonsterType;

        // 是否通过重生生成
        public bool RebornBirth;

        // 重生ID 指向 public int Id; // 流水号
        public int RebornID;

        // 千分之一伤害获得积分
        public int JiFenDamage;

        // 击杀积分
        public int JiFenKill;

        // 随机buff
        public List<KingOfBattleRandomBuff> RandomBuffList = new List<KingOfBattleRandomBuff>();

        // buff时间
        public int BuffTime;
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class KingOfBattleData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();

        #region 活动配置

        /// <summary>
        /// 复活和地图传送位置
        /// </summary>
        public Dictionary<int, YongZheZhanChangBirthPoint> MapBirthPointDict = new Dictionary<int, YongZheZhanChangBirthPoint>();

        /// <summary>
        /// 段位排行奖励配置
        /// </summary>
        public Dictionary<RangeKey, KingOfBattleSceneInfo> LevelRangeSceneIdDict = new Dictionary<RangeKey, KingOfBattleSceneInfo>(RangeKey.Comparer);

        /// <summary>
        /// 活动配置字典
        /// </summary>
        public Dictionary<int, KingOfBattleSceneInfo> SceneDataDict = new Dictionary<int, KingOfBattleSceneInfo>();

        #endregion 活动配置

        #region 奖励配置

        /// <summary>
        /// 王者商店
        /// </summary>
        public Dictionary<int, KingOfBattleStoreConfig> KingOfBattleStoreDict = new Dictionary<int, KingOfBattleStoreConfig>();
        public List<KingOfBattleStoreConfig> KingOfBattleStoreList = new List<KingOfBattleStoreConfig>();

        /// <summary>
        /// 采集得分信息
        /// </summary>
        public Dictionary<int, BattleCrystalMonsterItem> BattleCrystalMonsterDict = new Dictionary<int, BattleCrystalMonsterItem>();

        /// <summary>
        /// 本次跨服活动角色的跨服登录Token缓存集合
        /// </summary>
        public Dictionary<int, KuaFuServerLoginData> RoleIdKuaFuLoginDataDict = new Dictionary<int, KuaFuServerLoginData>();

        /// <summary>
        /// 通知报名的玩家进入活动
        /// </summary>
        public Dictionary<int, KuaFuServerLoginData> NotifyRoleEnterDict = new Dictionary<int, KuaFuServerLoginData>();

        // 原服务器报名的角色到报名分组的映射
        public ConcurrentDictionary<int, int> RoleId2JoinGroup = new ConcurrentDictionary<int, int>();

        // 每个场景需要动态刷的怪
        public Dictionary<int, List<KingOfBattleDynamicMonsterItem>> SceneDynMonsterDict = new Dictionary<int, List<KingOfBattleDynamicMonsterItem>>();
        public Dictionary<int, KingOfBattleDynamicMonsterItem> DynMonsterDict = new Dictionary<int, KingOfBattleDynamicMonsterItem>();

        /// <summary>
        /// 旗座
        /// <summary>
        public Dictionary<int, KingOfBattleQiZhiConfig> NPCID2QiZhiConfigDict = new Dictionary<int, KingOfBattleQiZhiConfig>();

        /// <summary>
        /// 战旗MonsterID 写死
        /// <summary>
        public int BattleQiZhiMonsterID1 = 8800003; // 盟军战旗
        public int BattleQiZhiMonsterID2 = 8800004; // 教团战旗

        /// <summary>
        /// Mu王者战场 固定伤害 军旗、箭塔、主基地
        /// </summary>
        public int KingOfBattleDamageJunQi = 1;
        public int KingOfBattleDamageTower = 1;
        public int KingOfBattleDamageCenter = 1;

        /// <summary>
        /// Mu王者战场被杀获得积分，积分值
        /// </summary>
        public int KingOfBattleDie = 5;

        /// <summary>
        /// 给予奖励的最低积分要求
        /// </summary>
        public int KingOfBattleLowestJiFen = 5;

        /// <summary>
        /// 攻击Boss得分配置,伤害百分比
        /// </summary>
        public double KingBattleBossAttackPercent = 0.001;

        /// <summary>
        /// Mu王者战场连杀积分，格式：基础值,系数,最小值,最大值
        /// </summary>
        public int KingOfBattleUltraKillParam1 = 27;
        public int KingOfBattleUltraKillParam2 = 3;
        public int KingOfBattleUltraKillParam3 = 30;
        public int KingOfBattleUltraKillParam4 = 75;

        /// <summary>
        /// Mu王者战场终结连杀积分，格式：基础值,系数,最小值,最大值
        /// </summary>
        public int KingOfBattleShutDownParam1 = -10;
        public int KingOfBattleShutDownParam2 = 5;
        public int KingOfBattleShutDownParam3 = 0;
        public int KingOfBattleShutDownParam4 = 100;

        /// <summary>
        /// 王者战场商店刷新时间、刷新个数、刷新价格
        /// </summary>
        public int KingOfBattleStoreRefreshTm = 24;
        public int KingOfBattleStoreRefreshNum = 6;
        public int KingOfBattleStoreRefreshCost = 100;

        /// <summary>
        /// 代表已报名状态的数子
        /// </summary>
        public int SighUpStateMagicNum = 100000000;

        //public string RoleParamsAwardsDefaultString = "0,0,0,0,0";
        public string RoleParamsAwardsDefaultString = "";

        /// <summary>
        /// NotifyCenterPrepareGame
        /// </summary>
        public bool PrepareGame;

        #endregion 奖励配置

        #region 运行时数据

        // 副本数据
        public Dictionary<int, YongZheZhanChangFuBenData> FuBenItemData = new Dictionary<int, YongZheZhanChangFuBenData>();

        #endregion 运行时数据
    }
}