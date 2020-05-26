using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Server.Data;

namespace Server.Data
{
    /// <summary>
    /// 领地建筑数据
    /// </summary>
    [ProtoContract]
    public class BuildingData
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [ProtoMember(1)] 
        public int BuildId = 0;

        /// <summary>
        /// 建筑物等级
        /// </summary>
        [ProtoMember(2)] 
        public int BuildLev = 1;

        /// <summary>
        /// 经验
        /// </summary>
        [ProtoMember(3)] 
        public int BuildExp = 0;

        /// <summary>
        /// 开发时间
        /// </summary>
        [ProtoMember(4)] 
        public string BuildTime = null;

        /// <summary>
        /// 开发方式 1
        /// </summary>
        [ProtoMember(5)] 
        public int TaskID_1 = 0;

        /// <summary>
        /// 开发方式 2
        /// </summary>
        [ProtoMember(6)]
        public int TaskID_2 = 0;

        /// <summary>
        /// 开发方式 3
        /// </summary>
        [ProtoMember(7)]
        public int TaskID_3 = 0;
    }
}

namespace GameServer.Logic.Building
{
    /// <summary>
    /// 领地日志数据更新枚举
    /// </summary>
    public enum BuildingLogType
    {
        BuildLog_TaskRole = 0,  // 每日开发资源人数（1人多次计1人)
        BuildLog_Task,          // 每日开发资源次数
        BuildLog_RefreshRole,   // 每日刷新人数（1人多次计1人)
        BuildLog_Refresh,       // 每日刷新使用次数
        BuildLog_OpenRole,      // 每日购买队列人数（1人多次计1人)
        BuildLog_Open,          // 每日购买队列人数
        BuildLog_Push,          // 每日推送次数
        BuildLog_PushUse,       // 每日推送被使用次数
    }

    /// <summary>
    /// 领地建筑开发队列ID
    /// </summary>
    public enum BuildTeamType
    {
        /// <summary>
        /// 空
        /// </summary>
        NullTeam = -1,
        /// <summary>
        /// 免费队列
        /// </summary>
        FreeTeam = 0,
        /// <summary>
        /// 付费队列
        /// </summary>
        PayTeam = 1,
    }

    /// <summary>
    /// 建筑物状态
    /// </summary>
    public enum BuildingState
    {
        EBS_Null = 0,

        EBS_InBuilding, // 建造中

        EBS_Finish // 建造完成
    }

    public enum BuildingErrorCode
    {
        Success = 0,          			// 成功
        ErrorAllLevelAwarded,			// 总等级奖励已经领过了
        ErrorAllLevel,					// 不满足总等级要求
        ErrorPayTeamMaxOver,			// 收费建造队列达到上限
        ErrorNoTaskFinish,				// 为找到已完成任务
        ErrorBQueueNotEnough,		    // 建造队列不足
        ErrorInBuilding,				// 建造中
        ErrorConfig,        			// 配置错误
        ErrorParams,        			// 传来的参数错误
        ZuanShiNotEnough,    		    // 钻石不足
        DBFailed,                       // 数据库出错
        ErrorIsNotOpen,                 // 功能未开放
        ErrorBuildNotFind,              // 找不到对应的建筑物
        ErrorTaskNotFind,               // 找不到对应的建造任务
        ErrorBagNotEnough,              // 背包空间不够
    }

    public enum BuildingQuality
    {
        Null   = 0,
        White  = 1, // 白
        Green,      // 绿
        Blue,       // 蓝
        Purple,     // 紫
    }

    /// <summary>
    /// 领地建筑开发队列
    /// </summary>
    public class BuildTeam  
    {
        /// <summary>
        /// 队列类型
        /// </summary>
        public BuildTeamType _TeamType = BuildTeamType.FreeTeam;

        /// <summary>
        /// 建筑物ID
        /// </summary>
        public int BuildID = 0;

        /// <summary>
        /// 开发方式ID
        /// </summary>
        public int TaskID = 0;
    }

    /// <summary>
    /// 随机数据
    /// </summary>
    public class BuildingRandomData
    {
        /// <summary>
        /// 品级
        /// </summary>
        public BuildingQuality quality = 0;

        /// <summary>
        /// 概率
        /// </summary>
        public double rate = 0.0d;
    }

    /// <summary>
    /// 建筑物配置信息 Build.xml
    /// </summary>
    public class BuildingConfigData
    {
        /// <summary>
        /// 建筑物ID
        /// </summary>
        public int BuildID = 0;

        /// <summary>
        /// 最大等级
        /// </summary>
        public int MaxLevel = 0;

        /// <summary>
        /// 免费队列随机数据
        /// </summary>
        public List<BuildingRandomData> FreeRandomList = new List<BuildingRandomData>();

        /// <summary>
        /// 收费随机数据
        /// </summary>
        public List<BuildingRandomData> RandomList = new List<BuildingRandomData>();
    }

    /// <summary>
    /// 建筑开发方式配置信息 BuildTask.xml
    /// </summary>
    public class BuildingTaskConfigData
    {
        /// <summary>
        /// 开发方式ID
        /// </summary>
        public int TaskID = 0;

        /// <summary>
        /// 建筑物ID
        /// </summary>
        public int BuildID = 0;

        /// <summary>
        /// 品级
        /// </summary>
        public BuildingQuality quality = 0;

        /// <summary>
        /// 产出倍率和
        /// </summary>
        public double SumNum = 0.0;

        /// <summary>
        /// 建筑经验倍率
        /// </summary>
        public double ExpNum = 0.0;

        /// <summary>
        /// 开发时间 单位分钟
        /// </summary>
        public int Time = 0;

        /// <summary>
        /// 随机忽略 计算随机时的辅助变量
        /// </summary>
        public bool RandSkip = false;
    }

    /// <summary>
    /// 建筑升级产出表 BuildLevel.xml
    /// </summary>
    public class BuildingLevelConfigData
    {
        /// <summary>
        /// 流水号ID
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// 建筑物ID
        /// </summary>
        public int BuildID = 0;

        /// <summary>
        /// 等级
        /// </summary>
        public int Level = 0;

        /// <summary>
        /// 升级所需经验
        /// </summary>
        public int UpNeedExp = 0;

#region 基础产出
        /// <summary>
        /// 经验
        /// </summary>
        public double Exp = 0.0d;

        /// <summary>
        /// 金币
        /// </summary>
        public double Money = 0.0d;

        /// <summary>
        /// 魔晶
        /// </summary>
        public double MoJing = 0.0d;

        /// <summary>
        /// 星魂
        /// </summary>
        public double XingHun = 0.0d;

        /// <summary>
        /// 成就
        /// </summary>
        public double ChengJiu = 0.0d;

        /// <summary>
        /// 声望
        /// </summary>
        public double ShengWang = 0.0d;

        /// <summary>
        /// 元素粉末
        /// </summary>
        public double YuanSu = 0.0d;

        /// <summary>
        /// 荧光粉末
        /// </summary>
        public double YingGuang = 0.0d;
#endregion
    }

    /// <summary>
    /// 总等级奖励产出表 BuildLevelAward.xml
    /// </summary>
    public class BuildingLevelAwardConfigData
    {
        /// <summary>
        /// 开发等级ID
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// 总等级
        /// </summary>
        public int AllLevel = 0;

        /// <summary>
        /// 奖励物品列表
        /// </summary>
        public AwardsItemList GoodsList = new AwardsItemList();
    }
}

