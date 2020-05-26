using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;

namespace GameServer.Logic
{
    /// <summary>
    /// 与服务器端通信连接参数
    /// </summary>
    public class SpriteChangeActionEventArgs : EventArgs
    {
        /// <summary>
        /// 动作类型
        /// </summary>
        public int Action
        {
            get;
            set;
        }
    }
    
    
    /// <summary>
    /// 角色基础属性表项
    /// </summary>
    public class RoleBasePropItem
    {
        public double[] arrRoleExtProp = null;
        /// <summary>
        /// 最大生命值
        /// </summary>
        public double LifeV
        {
            get;
            set;
        }

        /// <summary>
        /// 最大生命值
        /// </summary>
        public double MagicV
        {
            get;
            set;
        }

        /// <summary>
        /// 最小物理防御力
        /// </summary>
        public double MinDefenseV
        {
            get;
            set;
        }

        /// <summary>
        /// 最大物理防御力
        /// </summary>
        public double MaxDefenseV
        {
            get;
            set;
        }

        /// <summary>
        /// 最小魔法防御力
        /// </summary>
        public double MinMDefenseV
        {
            get;
            set;
        }

        /// <summary>
        /// 最大魔法防御力
        /// </summary>
        public double MaxMDefenseV
        {
            get;
            set;
        }

        /// <summary>
        /// 最小物理攻击力
        /// </summary>
        public double MinAttackV
        {
            get;
            set;
        }

        /// <summary>
        /// 最大物理攻击力
        /// </summary>
        public double MaxAttackV
        {
            get;
            set;
        }

        /// <summary>
        /// 最小魔法攻击力
        /// </summary>
        public double MinMAttackV
        {
            get;
            set;
        }

        /// <summary>
        /// 最大魔法攻击力
        /// </summary>
        public double MaxMAttackV
        {
            get;
            set;
        }

        /// <summary>
        /// 生命值回复速度
        /// </summary>
        public double RecoverLifeV
        {
            get;
            set;
        }

        /// <summary>
        /// 魔法值回复速度
        /// </summary>
        public double RecoverMagicV
        {
            get;
            set;
        }

        /// <summary>
        /// 闪避值
        /// </summary>
        public double Dodge
        {
            get;
            set;
        }

        /// <summary>
        /// 命中值
        /// </summary>
        public double HitV
        {
            get;
            set;
        }

        /// <summary>
        /// 物理技能增幅(百分比)
        /// </summary>
        public double PhySkillIncreasePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 魔法技能增幅(百分比)
        /// </summary>
        public double MagicSkillIncreasePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 攻击速度
        /// </summary>
        public double AttackSpeed
        {
            get;
            set;
        }
        
    }

    /// <summary>
    /// 摆摊配置项
    /// </summary>
    public class MapStallItem
    {
        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapID
        {
            get;
            set;
        }

        /// <summary>
        /// 地点
        /// </summary>
        public Point ToPos
        {
            get;
            set;
        }

        /// <summary>
        /// 半径
        /// </summary>
        public int Radius
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 打坐收益配置项
    /// </summary>
    public class RoleSitExpItem
    {
        /// <summary>
        /// 级别
        /// </summary>
        public int Level
        {
            get;
            set;
        }

        /// <summary>
        /// 经验收益(每10秒钟)
        /// </summary>
        public int Experience
        {
            get;
            set;
        }

        /// <summary>
        /// 灵力收益(每10秒钟)
        /// </summary>
        public int InterPower
        {
            get;
            set;
        }

        /// <summary>
        /// 被动技能熟练度收益(每10秒钟)
        /// </summary>
        public int SkilledDegrees
        {
            get;
            set;
        }

        /// <summary>
        /// 打坐时消减PK点(每10秒钟)
        /// </summary>
        public int PKPoint
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 排队的命令队列项
    /// </summary>
    public class QueueCmdItem
    {
        /// <summary>
        /// 命令ID
        /// </summary>
        public int CmdID
        {
            get;
            set;
        }

        /// <summary>
        /// 执行时间
        /// </summary>
        public long ExecTicks
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 传送点配置项
    /// </summary>
    public class MapTeleport
    {
        /// <summary>
        /// 地图编号
        /// </summary>
        public int Code
        {
            get;
            set;
        }

        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapID
        {
            get;
            set;
        }

        /// <summary>
        /// X
        /// </summary>
        public int X
        {
            get;
            set;
        }

        /// <summary>
        /// Y
        /// </summary>
        public int Y
        {
            get;
            set;
        }

        /// <summary>
        /// Radius
        /// </summary>
        public int Radius
        {
            get;
            set;
        }

        /// <summary>
        /// 要到地图ID
        /// </summary>
        public int ToMapID
        {
            get;
            set;
        }

        /// <summary>
        /// 地点X
        /// </summary>
        public int ToX
        {
            get;
            set;
        }

        /// <summary>
        /// 地点Y
        /// </summary>
        public int ToY
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 角色速度记录项
    /// </summary>
    public class RoleSpeedItem
    {
        /// <summary>
        /// 地图编号
        /// </summary>
        public int MapCode
        {
            get;
            set;
        }

        /// <summary>
        /// X
        /// </summary>
        public int X
        {
            get;
            set;
        }

        /// <summary>
        /// Y
        /// </summary>
        public int Y
        {
            get;
            set;
        }

        /// <summary>
        /// 超出的比例
        /// </summary>
        public double OverflowSpeed
        {
            get;
            set;
        }
    }

    /// <summary>
    /// CoolDown项
    /// </summary>
    public class CoolDownItem
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// 使用时间
        /// </summary>
        public long StartTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 冷却时间
        /// </summary>
        public long CDTicks
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 怪物减伤项
    /// </summary>
    public class MonsterSubInjureItem
    {
        /// <summary>
        /// ID
        /// </summary>
        public int MonsterTypeID
        {
            get;
            set;
        }

        /// <summary>
        /// 减伤比例
        /// </summary>
        public double SubInjureRate
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 视线内的对象
    /// </summary>
    public class VisibleItem
    {
        /// <summary>
        /// 对象类型
        /// </summary>
        public ObjectTypes ItemType { get; set; }

        /// <summary>
        /// 对象ID
        /// </summary>
        public int ItemID { get; set; }
    }

    /// <summary>
    /// 地图内的对象个数
    /// </summary>
    public class MapGridSpriteNum
    {
        /// <summary>
        /// 总的个数
        /// </summary>
        public int TotalNum { get; set; }

        /// <summary>
        /// 角色对象的个数
        /// </summary>
        public int RoleNum { get; set; }

        /// <summary>
        /// 怪物对象的个数
        /// </summary>
        public int MonsterNum { get; set; }

        /// <summary>
        /// NPC对象的个数
        /// </summary>
        public int NPCNum { get; set; }

        /// <summary>
        /// 镖车对象个数
        /// </summary>
        public int BiaoCheNum { get; set; }

        /// <summary>
        /// 帮旗对象个数
        /// </summary>
        public int JunQiNum { get; set; }

        /// <summary>
        /// 包裹对象个数
        /// </summary>
        public int GoodsPackNum { get; set; }

        /// <summary>
        /// 特效对象个数
        /// </summary>
        public int DecoNum { get; set; }
    }

    /// <summary>
    /// 地图格子内的对象管理(使用struct来一次性连续分配内存)
    /// </summary>
    public struct MapGridSpriteItem
    {
        /// <summary>
        /// 格子锁
        /// </summary>
        public object GridLock;

        /// <summary>
        /// 对象列表
        /// </summary>
        public List<object> ObjsList;

        /// <summary>
        /// 角色对象的个数
        /// </summary>
        public short RoleNum;

        /// <summary>
        /// 怪物对象的个数
        /// </summary>
        public short MonsterNum;

        /// <summary>
        /// NPC对象的个数
        /// </summary>
        public short NPCNum;

        /// <summary>
        /// 镖车对象个数
        /// </summary>
        public short BiaoCheNum;

        /// <summary>
        /// 帮旗对象个数
        /// </summary>
        public short JunQiNum;

        /// <summary>
        /// 包裹对象个数
        /// </summary>
        public short GoodsPackNum;

        /// <summary>
        /// 特效对象个数
        /// </summary>
        public short DecoNum;
    }
    
    /// <summary>
    /// 转职信息 [9/28/2013 LiaoWei] 
    /// </summary>
    public class ChangeOccupInfo
    {
        /// <summary>
        /// 职业ID
        /// </summary>
        public int OccupationID { get; set; }

        /// <summary>
        /// 需要的等级
        /// </summary>
        public int NeedLevel { get; set; }

        /// <summary>
        /// 需要的金币
        /// </summary>
        public int NeedMoney { get; set; }

        /// <summary>
        /// 需要的物品
        /// </summary>
        public List<GoodsData> NeedGoodsDataList { get; set; }

        /// <summary>
        /// 奖励物品
        /// </summary>
        public List<GoodsData> AwardGoodsDataList { get; set; }

        /// <summary>
        /// 奖励属性点
        /// </summary>
        public int AwardPropPoint { get; set; }

    }

    /// <summary>
    /// 转生信息 [9/28/2013 LiaoWei] 
    /// </summary>
    /*public class ChangeLifeInfo
    {
        /// <summary>
        /// 职业ID
        /// </summary>
        public int ChangeLifeID { get; set; }

        /// <summary>
        /// 需要的等级
        /// </summary>
        public int NeedLevel { get; set; }

        /// <summary>
        /// 需要的金币
        /// </summary>
        public int NeedMoney { get; set; }

        /// <summary>
        /// 需要的魔晶
        /// </summary>
        public int NeedMoJing { get; set; }

        /// <summary>
        /// 需要的物品
        /// </summary>
        public List<GoodsData> NeedGoodsDataList { get; set; }


        /// <summary>
        /// 需要的物品
        /// </summary>
        public List<GoodsData> AwardGoodsDataList { get; set; }

        /// <summary>
        /// 奖励属性点
        /// </summary>
        public int AwardPropPoint { get; set; }

        /// <summary>
        /// 升级经验系数
        /// </summary>
        public double ExpProportion { get; set; }

    }*/

    /// <summary>
    /// 职业加点信息 [9/28/2013 LiaoWei] 
    /// </summary>
    public class OccupationAddPointInfo
    {
        /// <summary>
        /// 职业ID
        /// </summary>
        public int OccupationID { get; set; }

        /// <summary>
        /// 加点数量
        /// </summary>
        public int AddPoint { get; set; }

    }

    /// <summary>
    // 转生加点信息 [3/6/2014 LiaoWei]
    /// </summary>
    public class ChangeLifeAddPointInfo
    {
        /// <summary>
        /// 转生等级
        /// </summary>
        public int ChangeLevel { get; set; }

        /// <summary>
        /// 加点数量
        /// </summary>
        public int AddPoint { get; set; }

        /// <summary>
        /// 力量上限
        /// </summary>
        public int nStrLimit { get; set; }

        /// <summary>
        /// 敏捷上限
        /// </summary>
        public int nDexLimit { get; set; }

        /// <summary>
        /// 智力上限
        /// </summary>
        public int nIntLimit { get; set; }

        /// <summary>
        /// 体力上限
        /// </summary>
        public int nConLimit { get; set; }

    }

    /// <summary>
    /// 血色城堡信息类 [11/5/2013 LiaoWei]
    /// </summary>
    public class BloodCastleDataInfo
    {
        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapCode { get; set; }

        /// <summary>
        /// 最小转生次数
        /// </summary>
        public int MinChangeLifeNum { get; set; }

        /// <summary>
        /// 最大转生次数
        /// </summary>
        public int MaxChangeLifeNum { get; set; }

        /// <summary>
        /// 最小等级限制
        /// </summary>
        public int MinLevel { get; set; }

        /// <summary>
        /// 最大等级限制
        /// </summary>
        public int MaxLevel { get; set; }

        /// <summary>
        /// 每日进入次数
        /// </summary>
        public int MaxEnterNum { get; set; }

        /// <summary>
        /// 消耗道具ID
        /// </summary>
        public int NeedGoodsID { get; set; }

        /// <summary>
        /// 消耗道具数量
        /// </summary>
        public int NeedGoodsNum { get; set; }

        /// <summary>
        /// 最大人数
        /// </summary>
        public int MaxPlayerNum { get; set; }

        /// <summary>
        /// 要杀死的怪物1等级
        /// </summary>
        public int NeedKillMonster1Level { get; set; }

        /// <summary>
        /// 要杀死的怪物1数量
        /// </summary>
        public int NeedKillMonster1Num { get; set; }

        /// <summary>
        /// 要杀死的怪物2ID
        /// </summary>
        public int NeedKillMonster2ID { get; set; }

        /// <summary>
        /// 要杀死的怪物2数量
        /// </summary>
        public int NeedKillMonster2Num { get; set; }

        /// <summary>
        /// 要刷出怪物2的数量
        /// </summary>
        public int NeedCreateMonster2Num { get; set; }


        /// <summary>
        /// 要刷出怪物2的坐标
        /// </summary>
        public string NeedCreateMonster2Pos { get; set; }

        /// <summary>
        /// 要刷出怪物2的半径
        /// </summary>
        public int NeedCreateMonster2Radius { get; set; }

        /// <summary>
        /// 要刷出怪物2的半径
        /// </summary>
        public int NeedCreateMonster2PursuitRadius { get; set; }

        /// <summary>
        /// 城门(怪物)ID
        /// </summary>
        public int GateID { get; set; }

        /// <summary>
        /// 城门(怪物)坐标
        /// </summary>
        public string GatePos { get; set; }

        /// <summary>
        /// 水晶棺(怪物)ID
        /// </summary>
        public int CrystalID { get; set; }

        /// <summary>
        /// 水晶棺(怪物)坐标
        /// </summary>
        public string CrystalPos { get; set; }

        /// <summary>
        /// 时间系数
        /// </summary>
        public int TimeModulus { get; set; }
        
        /// <summary>
        /// 经验系数
        /// </summary>
        public int ExpModulus { get; set; }

        /// <summary>
        /// 金币系数
        /// </summary>
        public int MoneyModulus { get; set; }

        /// <summary>
        /// 奖励道具1
        /// </summary>
        public string[] AwardItem1 { get; set; }
        
        /// <summary>
        /// 奖励道具2
        /// </summary>
        public string[] AwardItem2 { get; set; }
        
        /// <summary>
        /// 开启时间
        /// </summary>
        public List<string> BeginTime { get; set; }

        /// <summary>
        /// 准备时间
        /// </summary>
        public int PrepareTime { get; set; }

        /// <summary>
        /// 持续时间
        /// </summary>
        public int DurationTime { get; set; }

        /// <summary>
        /// 离场等待时间
        /// </summary>
        public int LeaveTime { get; set; }

        /// <summary>
        /// 雕像(怪物)ID
        /// </summary>
        public int DiaoXiangID { get; set; }

        /// <summary>
        /// 雕像(怪物)坐标
        /// </summary>
        public string DiaoXiangPos { get; set; }

    }

    /// <summary>
    /// 膜拜数据
    /// </summary>
    public class MoBaiData
    {
        // 编号
        public int ID;

        // 每日膜拜次数
        public int AdrationMaxLimit;

        // 金币膜拜消耗
        public int NeedJinBi;
        // 金币膜拜基础经验奖励
        public int JinBiExpAward;
        // 金币膜拜战功奖励
        public int JinBiZhanGongAward;

        // 钻石膜拜消耗
        public int NeedZuanShi;
        // 钻石膜拜基础经验奖励
        public int ZuanShiExpAward;
        // 钻石膜拜战功奖励
        public int ZuanShiZhanGongAward;

        // 城主本人额外膜拜次数
        public int ExtraNumber;
        // 膜拜最小等级要求
        public int MinZhuanSheng;
        public int MinLevel;
        /// <summary>
        /// 金币膜拜奖励的灵晶
        /// </summary>
        public int LingJingAwardByJinBi;
        /// <summary>
        /// 钻石膜拜奖励的灵晶
        /// </summary>
        public int LingJingAwardByZuanShi;
    }

    /// <summary>
    /// 副本评分信息类 [11/15/2013 LiaoWei]
    /// </summary>
    public class CopyScoreDataInfo
    {
        /// <summary>
        /// 副本地图ID
        /// </summary>
        public int CopyMapID { get; set; }

        /// <summary>
        /// 评分名称
        /// </summary>
        public string ScoreName { get; set; }

        /// <summary>
        /// 最小分值
        /// </summary>
        public int MinScore { get; set; }

        /// <summary>
        /// 最大分值
        /// </summary>
        public int MaxScore { get; set; }

        /// <summary>
        /// 经验系数
        /// </summary>
        public double ExpModulus { get; set; }

        /// <summary>
        /// 金币系数
        /// </summary>
        public double MoneyModulus { get; set; }

        /// <summary>
        /// 掉落包ID
        /// </summary>
        public int FallPacketID { get; set; }
		
        /// <summary>
        /// 翻牌奖励类型，1为翻牌随机魔晶，2为翻牌随机掉落，-1为不翻牌
        /// </summary>
        public int AwardType;

        /// <summary>
        /// 随机魔晶下限
        /// </summary>
        public int MinMoJing;

        /// <summary>
        /// 随机魔晶上限
        /// </summary>
        public int MaxMoJing;
    }

    /// <summary>
    /// 新手场景信息类 [12/1/2013 LiaoWei]
    /// </summary>
    public class FreshPlayerCopySceneInfo
    {
        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapCode { get; set; }

        /// <summary>
        /// 要杀死的怪物1等级
        /// </summary>
        public int NeedKillMonster1Level { get; set; }

        /// <summary>
        /// 要杀死的怪物1数量
        /// </summary>
        public int NeedKillMonster1Num { get; set; }

        /// <summary>
        /// 要杀死的怪物2ID
        /// </summary>
        public int NeedKillMonster2ID { get; set; }

        /// <summary>
        /// 要杀死的怪物2数量
        /// </summary>
        public int NeedKillMonster2Num { get; set; }

        /// <summary>
        /// 要刷出怪物2的数量
        /// </summary>
        public int NeedCreateMonster2Num { get; set; }


        /// <summary>
        /// 要刷出怪物2的坐标
        /// </summary>
        public string NeedCreateMonster2Pos { get; set; }

        /// <summary>
        /// 要刷出怪物2的半径
        /// </summary>
        public int NeedCreateMonster2Radius { get; set; }

        /// <summary>
        /// 要刷出怪物2的追击半径
        /// </summary>
        public int NeedCreateMonster2PursuitRadius { get; set; }

        /// <summary>
        /// 城门(怪物)ID
        /// </summary>
        public int GateID { get; set; }

        /// <summary>
        /// 城门(怪物)坐标
        /// </summary>
        public string GatePos { get; set; }

        /// <summary>
        /// 水晶棺(怪物)ID
        /// </summary>
        public int CrystalID { get; set; }

        /// <summary>
        /// 水晶棺(怪物)坐标
        /// </summary>
        public string CrystalPos { get; set; }

        /// <summary>
        /// 雕像(怪物)ID
        /// </summary>
        public int DiaoXiangID { get; set; }

        /// <summary>
        /// 雕像(怪物)坐标
        /// </summary>
        public string DiaoXiangPos { get; set; }

    }

    /// <summary>
    /// 任务星级信息 [12/3/2013 LiaoWei]
    /// </summary>
    public class TaskStarDataInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// 经验系数
        /// </summary>
        public double ExpModulus { get; set; }

        /// <summary>
        /// 星魂系数
        /// </summary>
        public double StarSoulModulus { get; set; }

        /// <summary>
        /// 绑定元宝系数
        /// </summary>
        public double BindYuanBaoModulus { get; set; }

        /// <summary>
        /// 概率
        /// </summary>
        public int Probability { get; set; }

    }


    /// <summary>
    /// 日常跑环任务奖励信息 [12/3/2013 LiaoWei]
    /// </summary>
    public class DailyCircleTaskAwardInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// 最小转生等级
        /// </summary>
        public int MinChangeLifeLev { get; set; }

        /// <summary>
        /// 最大转生等级
        /// </summary>
        public int MaxChangeLifeLev { get; set; }

        /// <summary>
        /// 最小等级
        /// </summary>
        public int MinLev { get; set; }

        /// <summary>
        /// 最大等级
        /// </summary>
        public int MaxLev { get; set; }

        /// <summary>
        /// 完成所有环额外经验奖励
        /// </summary>
        public int Experience { get; set; }

        /// <summary>
        /// 完成所有环额外元宝奖励
        /// </summary>
        public int MoJing { get; set; }

        /// <summary>
        /// 完成所有环额外物品奖励
        /// </summary>
        public int GoodsID { get; set; }

        /// <summary>
        /// 奖励数量
        /// </summary>
        public int GoodsNum { get; set; }

        /// <summary>
        /// 是否绑定
        /// </summary>
        public int Binding { get; set; }

        /// <summary>
        /// 星魂奖励
        /// </summary>
        public int XingHun { get; set; }
    }

    /// <summary>
    /// 讨伐任务奖励信息
    /// </summary>
    public class TaofaTaskAwardInfo
    {
        /// <summary>
        /// 绑钻奖励
        /// </summary>
        public int BangZuan { get; set; }
    }

    /// <summary>
    /// 战斗力信息 [12/17/2013 LiaoWei]
    /// </summary>
    public class CombatForceInfo
    {
        /// <summary>
        /// ID--职业ID
        /// </summary>
        public int ID;

        /// <summary>
        /// 生命上限
        /// </summary>
        public double MaxHPModulus;

        /// <summary>
        /// 魔法上限
        /// </summary>
        public double MaxMPModulus;

        /// <summary>
        /// 物理防御下限
        /// </summary>
        public double MinPhysicsDefenseModulus;

        /// <summary>
        /// 物理防御上限
        /// </summary>
        public double MaxPhysicsDefenseModulus;

        /// <summary>
        /// 魔法防御下限
        /// </summary>
        public double MinMagicDefenseModulus;

        /// <summary>
        /// 魔法防御上限
        /// </summary>
        public double MaxMagicDefenseModulus;

        /// <summary>
        /// 物理攻击下限
        /// </summary>
        public double MinPhysicsAttackModulus;

        /// <summary>
        /// 物理攻击上限
        /// </summary>
        public double MaxPhysicsAttackModulus;

        /// <summary>
        /// 魔法攻击下限
        /// </summary>
        public double MinMagicAttackModulus;

        /// <summary>
        /// 魔法攻击上限
        /// </summary>
        public double MaxMagicAttackModulus;

        /// <summary>
        /// 命中
        /// </summary>
        public double HitValueModulus;

        /// <summary>
        /// 闪避
        /// </summary>
        public double DodgeModulus;

        /// <summary>
        /// 伤害加成
        /// </summary>
        public double AddAttackInjureModulus;

        /// <summary>
        /// 伤害减少
        /// </summary>
        public double DecreaseInjureModulus;

        /// <summary>
        /// 附加攻击力
        /// </summary>
        public double AddAttackModulus;

        /// <summary>
        /// 附加防御力
        /// </summary>
        public double AddDefenseModulus;

        /// <summary>
        /// 击中生命恢复
        /// </summary>
        public double LifeStealModulus;

        /// <summary>
        /// 火系伤害
        /// </summary>
        public double FireAttack;

        /// <summary>
        /// 水系伤害
        /// </summary>
        public double WaterAttack;

        /// <summary>
        /// 雷系伤害
        /// </summary>
        public double LightningAttack;

        /// <summary>
        /// 土系伤害
        /// </summary>
        public double SoilAttack;

        /// <summary>
        /// 冰系伤害
        /// </summary>
        public double IceAttack;

        /// <summary>
        /// 风系伤害
        /// </summary>
        public double WindAttack;
    }

    /// <summary>
    /// 恶魔广场场景信息 [12/24/2013 LiaoWei]
    /// </summary>
    public class DaimonSquareDataInfo
    {
        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapCode { get; set; }

        /// <summary>
        /// 最小转生次数
        /// </summary>
        public int MinChangeLifeNum { get; set; }

        /// <summary>
        /// 最大转生次数
        /// </summary>
        public int MaxChangeLifeNum { get; set; }

        /// <summary>
        /// 最小等级限制
        /// </summary>
        public int MinLevel { get; set; }

        /// <summary>
        /// 最大等级限制
        /// </summary>
        public int MaxLevel { get; set; }

        /// <summary>
        /// 每日进入次数
        /// </summary>
        public int MaxEnterNum { get; set; }

        /// <summary>
        /// 消耗道具ID
        /// </summary>
        public int NeedGoodsID { get; set; }

        /// <summary>
        /// 消耗道具数量
        /// </summary>
        public int NeedGoodsNum { get; set; }

        /// <summary>
        /// 最大人数
        /// </summary>
        public int MaxPlayerNum { get; set; }

        /// <summary>
        /// 要刷出的怪物的ID列表 竖线分隔 列表有几个元素就有几波
        /// </summary>
        public string[] MonsterID { get; set; }

        /// <summary>
        /// 要刷出的怪物的个数列表 竖线分隔 和MonsterID一一对应
        /// </summary>
        public string[] MonsterNum { get; set; }

        /// <summary>
        /// 要刷出的怪物的X坐标
        /// </summary>
        public int posX { get; set; }

        /// <summary>
        /// 要刷出的怪物的Z坐标
        /// </summary>
        public int posZ { get; set; }

        /// <summary>
        /// 要刷出的怪物的半径
        /// </summary>
        public int Radius { get; set; }

        /// <summary>
        /// 要刷出的怪物的总数
        /// </summary>
        public int MonsterSum { get; set; }

        /// <summary>
        /// 刷一波的条件 杀死当前波怪物的百分比数量
        /// </summary>
        public string[] CreateNextWaveMonsterCondition { get; set; }

        /// <summary>
        /// 时间系数
        /// </summary>
        public int TimeModulus { get; set; }

        /// <summary>
        /// 经验系数
        /// </summary>
        public int ExpModulus { get; set; }

        /// <summary>
        /// 金币系数
        /// </summary>
        public int MoneyModulus { get; set; }

        /// <summary>
        /// 奖励道具
        /// </summary>
        public string[] AwardItem { get; set; }

        /// <summary>
        /// 开启时间
        /// </summary>
        public List<string> BeginTime { get; set; }

        /// <summary>
        /// 准备时间
        /// </summary>
        public int PrepareTime { get; set; }

        /// <summary>
        /// 持续时间
        /// </summary>
        public int DurationTime { get; set; }

        /// <summary>
        /// 离场等待时间
        /// </summary>
        public int LeaveTime { get; set; }

    }
    
    /// <summary>
    /// 累计登陆奖励信息数据  [2/11/2014 LiaoWei]
    /// </summary>
    public class TotalLoginDataInfo
    {
        /// <summary>
        /// 累计登陆天数
        /// </summary>
        public int TotalLoginDays { get; set; }

        /// <summary>
        /// 通用奖励
        /// </summary>
        public List<GoodsData> NormalAward { get; set; }

        /// <summary>
        /// 战士奖励物品
        /// </summary>
        public List<GoodsData> Award0 { get; set; }

        /// <summary>
        /// 法师奖励物品
        /// </summary>
        public List<GoodsData> Award1 { get; set; }

        /// <summary>
        /// 弓箭手奖励物品
        /// </summary>
        public List<GoodsData> Award2 { get; set; }

        /// <summary>
        /// 魔剑士奖励物品
        /// </summary>
        public List<GoodsData> Award3 { get; set; }

    }

    /// <summary>
    /// VIP奖励信息数据 [2/19/2014 LiaoWei]
    /// </summary>
    public class VIPDataInfo
    {
        /// <summary>
        /// 奖励ID
        /// </summary>
        public int AwardID { get; set; }

        /// <summary>
        /// 等级限制
        /// </summary>
        public int VIPlev { get; set; }

        /// <summary>
        /// 每天领取次数
        /// </summary>
        public int DailyMaxUseTimes { get; set; }

        /// <summary>
        /// 奖励物品
        /// </summary>
        public List<GoodsData> AwardGoods { get; set; }

        /// <summary>
        /// 奖励钻石
        /// </summary>
        public int ZuanShi { get; set; }

        /// <summary>
        /// 奖励绑定钻石
        /// </summary>
        public int BindZuanShi { get; set; }

        /// <summary>
        /// 奖励金币
        /// </summary>
        public int JinBi { get; set; }

        /// <summary>
        /// 奖励绑定金币
        /// </summary>
        public int BindJinBi { get; set; }

        /// <summary>
        /// 奖励BUFF
        /// </summary>
        public int[] BufferGoods { get; set; }

        /// <summary>
        /// 洗红名
        /// </summary>
        public int XiHongMing { get; set; }

        /// <summary>
        /// 奖励活跃值
        /// </summary>
        public int XiuLi { get; set; }
        
    }

    /// <summary>
    /// VIP等级奖励和经验信息数据 [2/19/2014 LiaoWei]
    /// </summary>
    public class VIPLevAwardAndExpInfo
    {
        /// <summary>
        /// VIP等级
        /// </summary>
        public int VipLev { get; set; }

        /// <summary>
        /// VIP等级奖励
        /// </summary>
        public List<GoodsData> AwardList{ get; set; }

        /// <summary>
        /// 需求经验
        /// </summary>
        public int NeedExp { get; set; }

    }

    /// <summary>
    // 冥想数据 [3/5/2014 LiaoWei]
    /// </summary>
    public class MeditateData
    {
        /// <summary>
        /// ID
        /// </summary>
        public int MeditateID { get; set; }

        /// <summary>
        /// 最小转生等级
        /// </summary>
        public int MinZhuanSheng { get; set; }

        /// <summary>
        /// 最小等级
        /// </summary>
        public int MinLevel { get; set; }

        /// <summary>
        /// 最大转生等级
        /// </summary>
        public int MaxZhuanSheng { get; set; }

        /// <summary>
        /// 最大等级
        /// </summary>
        public int MaxLevel { get; set; }

        /// <summary>
        /// 经验收益
        /// </summary>
        public int Experience { get; set; }

        /// <summary>
        ///星魂收益
        /// </summary>
        public int StarSoul { get; set; }

    }

    /// <summary>
    /// 经验副本静态数据 [3/18/2014 LiaoWei]
    /// </summary>
    public class ExperienceCopyMapDataInfo
    {
        /// <summary>
        /// 副本ID
        /// </summary>
        public int CopyMapID { get; set; }

        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapCodeID { get; set; }

        /// <summary>
        /// 要刷出的怪物的ID列表 竖线分隔 列表有几个元素就有几波
        /// </summary>
        //public int[] MonsterID { get; set; }
        public Dictionary<int, List<int>> MonsterIDList { get; set; }

        /// <summary>
        /// 要刷出的怪物的个数列表 竖线分隔 和MonsterID一一对应
        /// </summary>
        //public int[] MonsterNum { get; set; }
        public Dictionary<int, List<int>> MonsterNumList { get; set; }

        /// <summary>
        /// 要刷出的怪物的X坐标
        /// </summary>
        public int posX { get; set; }

        /// <summary>
        /// 要刷出的怪物的Z坐标
        /// </summary>
        public int posZ { get; set; }

        /// <summary>
        /// 要刷出的怪物的半径
        /// </summary>
        public int Radius { get; set; }

        /// <summary>
        /// 要刷出的怪物的总数
        /// </summary>
        public int MonsterSum { get; set; }

        /// <summary>
        /// 刷一波的条件 杀死当前波怪物的百分比数量
        /// </summary>
        public int[] CreateNextWaveMonsterCondition { get; set; }

    }

    /// <summary>
    /// 天使神殿静态数据 [3/23/2014 LiaoWei]
    /// <summary>
    public class AngelTempleData
    {
        //MapCode="9000" TimePoints="15:00" WaitingEnterSecs="180" FightingSecs="900" ClearRolesSecs="30" MinZhuangSheng="0" MinLevel="50" MinRequestNum="1" MaxEnterNum="1000" 

        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapCode { get; set; }

        /// <summary>
        /// 最小转生次数限制
        /// </summary>
        public int MinChangeLifeNum { get; set; }

        /// <summary>
        /// 最小等级限制
        /// </summary>
        public int MinLevel { get; set; }

        /// <summary>
        /// 开启时间
        /// </summary>
        public List<string> BeginTime { get; set; }

        /// <summary>
        /// 准备时间
        /// </summary>
        public int PrepareTime { get; set; }

        /// <summary>
        /// 持续时间
        /// </summary>
        public int DurationTime { get; set; }

        /// <summary>
        /// 离场等待时间
        /// </summary>
        public int LeaveTime { get; set; }

        /// <summary>
        /// 最少人数
        /// </summary>
        public int MinPlayerNum { get; set; }
        
        /// <summary>
        /// 最大人数
        /// </summary>
        public int MaxPlayerNum { get; set; }

        /// <summary>
        /// BossID
        /// </summary>
        public int BossID { get; set; }

        /// <summary>
        /// Boss x 坐标
        /// </summary>
        public int BossPosX { get; set; }

        /// <summary>
        /// Boss y 坐标
        /// </summary>
        public int BossPosY { get; set; }
    }

    /// <summary>
    /// PK之王崇拜静态数据[3/25/2014 LiaoWei]
    /// <summary>
    public class PKKingAdrationData
    {
        /// <summary>
        /// 每日崇拜次数上限
        /// </summary>
        public int AdrationMaxLimit { get; set; }

        /// <summary>
        /// 金币崇拜消耗
        /// </summary>
        public int GoldAdrationSpend { get; set; }

        /// <summary>
        /// 金币崇拜经验奖励系数
        /// </summary>
        public int GoldAdrationExpModulus { get; set; }

        /// <summary>
        /// 金币崇拜声望奖励系数
        /// </summary>
        public int GoldAdrationShengWangModulus { get; set; }

        /// <summary>
        /// 钻石崇拜消耗
        /// </summary>
        public int DiamondAdrationSpend { get; set; }

        /// <summary>
        /// 钻石崇拜经验奖励系数
        /// </summary>
        public int DiamondAdrationExpModulus { get; set; }

        /// <summary>
        /// 钻石崇拜声望奖励系数
        /// </summary>
        public int DiamondAdrationShengWangModulus { get; set; }
    }

    /// <summary>
    /// Boss之家静态数据 [4/7/2014 LiaoWei]
    /// <summary>
    public class BossHomeData
    {
        /// <summary>
        /// MapID
        /// </summary>
        public int MapID { get; set; }

        /// <summary>
        /// VIP等级限制
        /// </summary>
        public int VIPLevLimit { get; set; }

        /// <summary>
        /// 最小转生等级
        /// </summary>
        public int MinChangeLifeLimit { get; set; }

        /// <summary>
        /// 最大转生等级
        /// </summary>
        public int MaxChangeLifeLimit { get; set; }

        /// <summary>
        /// 最小等级
        /// </summary>
        public int MinLevel { get; set; }

        /// <summary>
        /// 最大等级
        /// </summary>
        public int MaxLevel { get; set; }

        /// <summary>
        /// 进入需求钻石
        /// </summary>
        public int EnterNeedDiamond { get; set; }

        /// <summary>
        /// 每分钟需要的钻石
        /// </summary>
        public int OneMinuteNeedDiamond { get; set; }

    }

    /// <summary>
    /// 黄金神庙静态数据 [4/7/2014 LiaoWei]
    /// <summary>
    public class GoldTempleData
    {
        /// <summary>
        /// MapID
        /// </summary>
        public int MapID { get; set; }

        /// <summary>
        /// VIP等级限制
        /// </summary>
        public int VIPLevLimit { get; set; }

        /// <summary>
        /// 最小转生等级
        /// </summary>
        public int MinChangeLifeLimit { get; set; }

        /// <summary>
        /// 最大转生等级
        /// </summary>
        public int MaxChangeLifeLimit { get; set; }

        /// <summary>
        /// 最小等级
        /// </summary>
        public int MinLevel { get; set; }

        /// <summary>
        /// 最大等级
        /// </summary>
        public int MaxLevel { get; set; }

        /// <summary>
        /// 进入需求钻石
        /// </summary>
        public int EnterNeedDiamond { get; set; }

        /// <summary>
        /// 每分钟需要的钻石
        /// </summary>
        public int OneMinuteNeedDiamond { get; set; }

    }

    /// <summary>
    /// 图鉴系统静态数据 [5/3/2014 LiaoWei]
    /// <summary>
    public class PictureJudgeData
    {
        /// <summary>
        /// 图鉴ID
        /// </summary>
        public int PictureJudgeID { get; set; }

        /// <summary>
        /// 图鉴type
        /// </summary>
        public int PictureJudgeType { get; set; }

        /// <summary>
        /// 属性ID1
        /// </summary>
        public int PropertyID1 { get; set; }

        /// <summary>
        /// 属性ID2
        /// </summary>
        public int PropertyID2 { get; set; }

        /// <summary>
        /// 属性ID3
        /// </summary>
        public int PropertyID3 { get; set; }

        /// <summary>
        /// 属性ID4
        /// </summary>
        public int PropertyID4 { get; set; }

        /// <summary>
        /// 属性ID5
        /// </summary>
        public int PropertyID5 { get; set; }

        /// <summary>
        /// 属性ID6
        /// </summary>
        public int PropertyID6 { get; set; }

        /// <summary>
        /// 属性ID7
        /// </summary>
        public int PropertyID7 { get; set; }

        /// <summary>
        /// 属性ID8
        /// </summary>
        public int PropertyID8 { get; set; }

        /// <summary>
        /// 属性ID9
        /// </summary>
        public int PropertyID9 { get; set; }

        /// <summary>
        /// 属性ID10
        /// </summary>
        public int PropertyID10 { get; set; }

        /// <summary>
        /// 属性ID11
        /// </summary>
        public int PropertyID11 { get; set; }


        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue1 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue1 { get; set; }

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue2 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue2 { get; set; }

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue3 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue3 { get; set; }

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue4 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue4 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue5 { get; set; }
        
        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue6 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue7 { get; set; }

        /// <summary>
        /// 需求物品ID
        /// </summary>
        public int NeedGoodsID { get; set; }

        /// <summary>
        /// 需求物品数量
        /// </summary>
        public int NeedGoodsNum { get; set; }
    }

    /// <summary>
    /// 图鉴系统图鉴类型静态数据 [7/12/2014 LiaoWei]
    /// <summary>
    public class PictureJudgeTypeData
    {
        /// <summary>
        /// 图鉴ID
        /// </summary>
        public int PictureJudgeTypeID { get; set; }

        /// <summary>
        /// 开启转生等级
        /// </summary>
        public int OpenChangeLifeLevel { get; set; }

        /// <summary>
        /// 开启等级
        /// </summary>
        public int OpenLevel { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalNum { get; set; }

        /// <summary>
        /// 属性ID1
        /// </summary>
        public int PropertyID1 { get; set; }

        /// <summary>
        /// 属性ID2
        /// </summary>
        public int PropertyID2 { get; set; }

        /// <summary>
        /// 属性ID3
        /// </summary>
        public int PropertyID3 { get; set; }

        /// <summary>
        /// 属性ID4
        /// </summary>
        public int PropertyID4 { get; set; }

        /// <summary>
        /// 属性ID5
        /// </summary>
        public int PropertyID5 { get; set; }

        /// <summary>
        /// 属性ID6
        /// </summary>
        public int PropertyID6 { get; set; }

        /// <summary>
        /// 属性ID7
        /// </summary>
        public int PropertyID7 { get; set; }

        /// <summary>
        /// 属性ID8
        /// </summary>
        public int PropertyID8 { get; set; }

        /// <summary>
        /// 属性ID9
        /// </summary>
        public int PropertyID9 { get; set; }

        /// <summary>
        /// 属性ID10
        /// </summary>
        public int PropertyID10 { get; set; }

        /// <summary>
        /// 属性ID11
        /// </summary>
        public int PropertyID11 { get; set; }

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue1 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue1 { get; set; }

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue2 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue2 { get; set; }

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue3 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue3 { get; set; }

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue4 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue4 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue5 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue6 { get; set; }

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue7 { get; set; }

    }

    /// <summary>
    /// 对象ID的类型分配的区间范围
    /// 32位整数最大值为2,147,483,648
    /// 7D2B 0000 = 2,099,970,048
    /// 7F00 0000 = 2,130,706,432
    /// 7F01 0000 = 2,130,771,968
    /// 7F40 0000 = 2,134,900,736
    /// 7F41 0000 = 2,134,966,272
    /// 7F42 0000 = 2,135,031,808
    /// 7F43 0000 = 2,135,097,344
    /// 7F50 0000 = 2,135,949,312
    /// </summary>
    public static class SpriteBaseIds
    {
        public const int RoleBaseId = 0;
        public const int NpcBaseId = 0x7F000000;
        public const int MonsterBaseId = 0x7F010000;
        public const int PetBaseId = 0x7F400000;
        public const int BiaoCheBaseId = 0x7F410000;
        public const int JunQiBaseId = 0x7F420000;
        public const int FakeRoleBaseId = 0x7F430000;
        public const int MaxId = 0x7F500000;
    }

    /// <summary>
    /// 地图相关的AI事件
    /// </summary>
    public struct MapAIEvent 
    {
        public int GuangMuID;
        public int Show;
    }

    /// <summary>
    /// 装备进阶静态数据 [4/30/2014 LiaoWei]
    /// <summary>
    public class MuEquipUpgradeData
    {
        /// <summary>
        /// 类型ID
        /// </summary>
        public int CategoriyID { get; set; }

        /// <summary>
        /// 分类ID
        /// </summary>
        public int SuitID { get; set; }

        /// <summary>
        /// 需要魔晶
        /// </summary>
        public int NeedMoJing { get; set; }
    }

    /// <summary>
    /// 金币副本静态数据 [6/10/2014 LiaoWei]
    /// </summary>
    public class GoldCopySceneData
    {
        /// <summary>
        /// 怪数据
        /// </summary>
        public Dictionary<int, GoldCopySceneMonster> GoldCopySceneMonsterData = new Dictionary<int, GoldCopySceneMonster>();

        /// <summary>
        /// 怪的巡逻路径
        /// </summary>
        public List<int[]> m_MonsterPatorlPathList { get; set; }

    }

    /// <summary>
    /// 恶魔来袭副本静态数据
    /// </summary>
    public class EMoLaiXiCopySenceData
    {
        /// <summary>
        /// 怪数据
        /// </summary>
        public List<EMoLaiXiCopySenceMonster> EMoLaiXiCopySenceMonsterData = new List<EMoLaiXiCopySenceMonster>();

        /// <summary>
        /// 怪的巡逻路径列表
        /// </summary>
        public Dictionary<int, List<int[]>> m_MonsterPatorlPathLists = new Dictionary<int, List<int[]>>();

        /// <summary>
        /// 总波数
        /// </summary>
        public int TotalWave;

        /// <summary>
        /// 失败需要的逃跑怪物数
        /// </summary>
        public int FaildEscapeMonsterNum;
    }

    /// <summary>
    /// 怪物动态的技能项
    /// </summary>
    public class DynSkillItem
    {
        /// <summary>
        /// 技能ID
        /// </summary>
        public int SkillID;

        /// <summary>
        /// 优先级别
        /// </summary>
        public int Priority;
    }
}
