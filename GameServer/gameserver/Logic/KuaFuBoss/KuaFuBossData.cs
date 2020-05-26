using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Server.Data;
using KF.Contract.Data;
using Tmsk.Contract;

namespace GameServer.Logic
{
    public class KuaFuBossBirthPoint
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

    public class BattleDynamicMonsterItem
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
    }

    public class KuaFuBossSceneInfo
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
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class KuaFuBossData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();

        #region 活动配置

        /// <summary>
        /// 复活和地图传送位置
        /// </summary>
        public Dictionary<int, KuaFuBossBirthPoint> MapBirthPointDict = new Dictionary<int, KuaFuBossBirthPoint>();

        /// <summary>
        /// 段位排行奖励配置
        /// </summary>
        public Dictionary<RangeKey, KuaFuBossSceneInfo> LevelRangeSceneIdDict = new Dictionary<RangeKey, KuaFuBossSceneInfo>(RangeKey.Comparer);

        /// <summary>
        /// 活动配置字典
        /// </summary>
        public Dictionary<int, KuaFuBossSceneInfo> SceneDataDict = new Dictionary<int, KuaFuBossSceneInfo>();

        #endregion 活动配置

        #region 奖励配置

        // 每个场景需要动态刷的怪
        public Dictionary<int, List<BattleDynamicMonsterItem>> SceneDynMonsterDict = new Dictionary<int, List<BattleDynamicMonsterItem>>();

        /// <summary>
        /// 本次跨服活动角色的跨服登录Token缓存集合
        /// </summary>
        public Dictionary<int, KuaFuServerLoginData> RoleIdKuaFuLoginDataDict = new Dictionary<int, KuaFuServerLoginData>();

        public Dictionary<int, KuaFuServerLoginData> NotifyRoleEnterDict = new Dictionary<int, KuaFuServerLoginData>();

        // 原服务器报名的角色到报名分组的映射
        public ConcurrentDictionary<int, int> RoleId2JoinGroup = new ConcurrentDictionary<int, int>();

        /// <summary>
        /// NotifyCenerPrepareGame
        /// </summary>
        public bool PrepareGame;

        #endregion 奖励配置

        #region 运行时数据

        public Dictionary<int, YongZheZhanChangFuBenData> FuBenItemData = new Dictionary<int, YongZheZhanChangFuBenData>();

        #endregion 运行时数据
    }
}
