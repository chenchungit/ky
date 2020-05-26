#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Threading;
//using System.Windows.Threading;
using Server.Data;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using System.IO;
using ProtoBuf;
using Server.TCP;
using Server.Tools;
//using System.Windows.Forms;

using GameServer.Logic.RefreshIconState;
using GameServer.Core.Executor;
using Tmsk.Contract;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic.JingJiChang;

namespace GameServer.Logic
{
    /// <summary>
    /// 爆怪物的时间点
    /// </summary>
    public class BirthTimePoint
    {
        /// <summary>
        /// 爆怪的小时
        /// </summary>
        public int BirthHour = 0;

        /// <summary>
        /// 爆怪的分钟
        /// </summary>
        public int BirthMinute = 0;
    };

    /// <summary>
    /// 每周几爆怪物信息 MU新增 [1/10/2014 LiaoWei]
    /// </summary>
    public class BirthTimeForDayOfWeek
    {
        /// <summary>
        /// 爆怪的时间
        /// </summary>
        public BirthTimePoint BirthTime;

        /// <summary>
        /// 每周哪天爆怪
        /// </summary>
        public int BirthDayOfWeek = 0;
    };

    /// <summary>
    /// 怪物的静态数据
    /// </summary>
    public class MonsterStaticInfo
    {
        /// <summary>
        /// 获取或设置姓名
        /// </summary>
        public string VSName
        {
            get;
            set;
        }

        /// <summary>
        /// 扩展ID
        /// </summary>
        public int ExtensionID
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置等级
        /// </summary>
        public int VLevel { get; set; }

        /// 经验值是当前的级别修炼值，如果升级，扣除升级的经验值
        /// <summary>
        /// 获取或设置自身的经验值(如果为怪物等NPC,则为杀它的玩家可以得到的经验值)
        /// </summary>
        public int VExperience { get; set; }

        /// <summary>
        /// 获取或设置自身的金币(如果为怪物等NPC,则为杀它的玩家可以得到的金币)
        /// </summary>
        public int VMoney { get; set; }

        /// <summary>
        /// 获取最大生命值
        /// </summary>
        public double VLifeMax
        {
            get;
            set;
        }

        /// <summary>
        /// 获取最大魔法值
        /// </summary>
        public double VManaMax
        {
            get;
            set;
        }

        /// <summary>
        /// 对应的职业
        /// </summary>
        public int ToOccupation
        {
            get;
            set;
        }

        /// <summary>
        /// 各个动作的帧的速度
        /// </summary>
        public int[] SpriteSpeedTickList { get; set; }

        /// <summary>
        /// 获取或设置精灵各动作对应的帧列个数
        /// </summary>
        public int[] EachActionFrameRange { get; set; }

        /// <summary>
        /// 获取或设置各动作产生实际效果的针序号
        /// </summary>
        public int[] EffectiveFrame { get; set; }

        /// <summary>
        /// 获取或设置索敌范围(距离)
        /// </summary>
        public int SeekRange { get; set; }

        /// <summary>
        /// 获取或设置精灵当前衣服代码
        /// </summary>
        public int EquipmentBody { get; set; }

        /// <summary>
        /// 获取或设置精灵当前武器代码
        /// </summary>
        public int EquipmentWeapon { get; set; }

        /// <summary>
        /// 对应角色最小攻击力
        /// </summary>
        public int MinAttack
        {
            get;
            set;
        }

        /// <summary>
        /// 对应角色最大攻击力
        /// </summary>
        public int MaxAttack
        {
            get;
            set;
        }

        /// <summary>
        /// 对应角色的防御力
        /// </summary>
        public int Defense
        {
            get;
            set;
        }

        /// <summary>
        /// 魔防
        /// </summary>
        public int MDefense
        {
            get;
            set;
        }

        /// <summary>
        /// 命中率
        /// </summary>
        public double HitV
        {
            get;
            set;
        }

        /// <summary>
        /// 闪避率
        /// </summary>
        public double Dodge
        {
            get;
            set;
        }

        /// <summary>
        /// 生命恢复速度(每隔5秒百分之多少)
        /// </summary>
        public double RecoverLifeV
        {
            get;
            set;
        }

        /// <summary>
        /// 魔法恢复速度(每隔5秒百分之多少)
        /// </summary>
        public double RecoverMagicV
        {
            get;
            set;
        }

        /// <summary>
        /// 伤害反弹(百分比)
        /// </summary>
        public double MonsterDamageThornPercent
        {
            get;
            set;
        }

        /// <summary>
        /// 伤害反弹(固定值)
        /// </summary>
        public double MonsterDamageThorn
        {
            get;
            set;
        }

        /// <summary>
        /// 伤害吸收(百分比)
        /// </summary>
        public double MonsterSubAttackInjurePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 伤害吸收(固定值)
        /// </summary>
        public double MonsterSubAttackInjure
        {
            get;
            set;
        }

        /// <summary>
        /// 无视防御概率
        /// </summary>
        public double MonsterIgnoreDefensePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 无视防御比例
        /// </summary>
        public double MonsterIgnoreDefenseRate
        {
            get;
            set;
        }

        /// <summary>
        /// 幸运一击概率	
        /// </summary>
        public double MonsterLucky
        {
            get;
            set;
        }

        /// <summary>
        /// 卓越一击概率
        /// </summary>
        public double MonsterFatalAttack
        {
            get;
            set;
        }

        /// <summary>
        /// 双倍一击概率
        /// </summary>
        public double MonsterDoubleAttack
        {
            get;
            set;
        }

        /// <summary>
        /// 怪物掉落ID
        /// </summary>
        public int FallGoodsPackID
        {
            get;
            set;
        }

        /// <summary>
        /// 攻击方式(0: 物理攻击, 1: 魔法攻击, 2: 道术攻击)
        /// </summary>
        public int AttackType
        {
            get;
            set;
        }

        /// <summary>
        /// 大乱斗个人积分
        /// </summary>
        public int BattlePersonalJiFen
        {
            get;
            set;
        }

        /// <summary>
        /// 大乱斗阵营积分
        /// </summary>
        public int BattleZhenYingJiFen
        {
            get;
            set;
        }

        // 恶魔广场 血色堡垒 begin [11/14/2013 LiaoWei]
        /// <summary>
        /// 恶魔广场积分
        /// </summary>
        public int DaimonSquareJiFen
        {
            get;
            set;
        }

        /// <summary>
        /// 血色堡垒积分
        /// </summary>
        public int BloodCastJiFen
        {
            get;
            set;
        }

        /// <summary>
        /// 狼魂积分
        /// </summary>
        public int WolfScore { get; set; }

        /// <summary>
        /// 掉落是否属于拥有者
        /// </summary>
        public int FallBelongTo
        {
            get;
            set;
        }

        /// <summary>
        /// 怪物挂的技能ID列表
        /// </summary>
        public int[] SkillIDs
        {
            get;
            set;
        }

        /// <summary>
        /// 怪物所属阵营
        /// </summary>
        public int Camp
        {
            get;
            set;
        }

        /// <summary>
        /// 怪物的AIID
        /// </summary>
        public int AIID
        {
            get;
            set;
        }

        /// <summary>
        /// 怪物的转生次数
        /// </summary>
        public int ChangeLifeCount
        {
            get;
            set;
        }
    }

    public class MonsterTaskObject
    {
        public int MissionID1
        {
            set; get;
        }
        /// <summary>
        /// 任务道具1
        /// </summary>
        public int MissionPropsID1
        {
            set; get;
        }
        /// <summary>
        /// 任务掉率1
        /// </summary>
        public int MissionRate1
        {
            set; get;
        }

        /// <summary>
        /// 任务ID2
        /// </summary>
        public int MissionID2
        {
            set; get;
        }
        /// <summary>
        /// 任务道具2
        /// </summary>
        public int MissionPropsID2
        {
            set; get;
        }
        /// <summary>
        /// 任务掉率2
        /// </summary>
        public int MissionRate2
        {
            set; get;
        }

        /// <summary>
        /// 任务ID3
        /// </summary>
        public int MissionID3
        {
            set; get;
        }
        /// <summary>
        /// 任务道具3
        /// </summary>
        public int MissionPropsID3
        {
            set; get;
        }
        /// <summary>
        /// 任务掉率3
        /// </summary>
        public int MissionRate3
        {
            set; get;
        }
    }

    public class XMonsterStaticInfo
    {
        /// <summary>
        /// 怪物ID
        /// </summary>
        public int MonsterId
        {
            set; get;
        }
        /// <summary>
        /// 怪物名称
        /// </summary>
        public string Name
        {
            set; get;
        }
        /// <summary>
        /// 怪物类型
        /// </summary>
        public int MonsterType
        {
            set; get;
        }
        /// <summary>
        /// 怪物等级
        /// </summary>
        public int Level
        {
            set; get;
        }
        /// <summary>
        /// 基础经验
        /// </summary>
        public int Exp
        {
            set; get;
        }
        /// <summary>
        /// 五行基础经验
        /// </summary>
        public int FiveExp
        {
            set; get;
        }
        /// <summary>
        /// 御灵魂魄基础经验
        /// </summary>
        public int OrenExp
        {
            set; get;
        }
        /// <summary>
        /// 宠物基础经验
        /// </summary>
        public int PetsExp
        {
            set; get;
        }

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHP
        {
            set; get;
        }

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int CurHP
        {
            set; get;
        }
        /// <summary>
        /// 物理攻击值
        /// </summary>
        public int Ad
        {
            set; get;
        }
        /// <summary>
        /// 物理防御
        /// </summary>
        public int Pd
        {
            set; get;
        }
        /// <summary>
        /// 火焰伤害
        /// </summary>
        public int FireDamage
        {
            set; get;
        }

        /// <summary>
        /// 冰霜伤害
        /// </summary>
        public int FrostDamage
        {
            set; get;
        }
        /// <summary>
        /// 闪电伤害
        /// </summary>
        public int LightDamage
        {
            set; get;
        }
        /// <summary>
        /// 毒素伤害
        /// </summary>
        public int ToxicInjury
        {
            set; get;
        }
        /// <summary>
        /// 火焰抗性
        /// </summary>
        public int FireResist
        {
            set; get;
        }
        /// <summary>
        /// 冰霜抗性
        /// </summary>
        public int FrozenResist
        {
            set; get;
        }
        /// <summary>
        /// 闪电抗性
        /// </summary>
        public int LightResist
        {
            set; get;
        }
        /// <summary>
        /// 毒素抗性
        /// </summary>
        public int PoisonResist
        {
            set; get;
        }
        /// <summary>
        /// 闪避几率
        /// </summary>
        public int DodgeChance
        {
            set; get;
        }
        /// <summary>
        /// 命中几率
        /// </summary>
        public int DodgeResis
        {
            set; get;
        }
        /// <summary>
        /// 暴击几率
        /// </summary>
        public int CritChance
        {
            set; get;
        }
        /// <summary>
        /// 韧性几率
        /// </summary>
        public int CritResist
        {
            set; get;
        }
        /// <summary>
        /// 移动速度
        /// </summary>
        public int MoveSpeed
        {
            set; get;
        }
        /// <summary>
        /// 警戒范围
        /// </summary>
        public int AlertRange
        {
            set; get;
        }
        /// <summary>
        /// 追击范围
        /// </summary>
        public int PursuitRange
        {
            set; get;
        }
        /// <summary>
        /// 脱战范围
        /// </summary>
        public int DisengageRange
        {
            set; get;
        }
        /// <summary>
        /// 技能ID1
        /// </summary>
        public List<int> Skills;
        //public int SkillsID1
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 技能ID2
        ///// </summary>
        //public int SkillsID2
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 技能ID3
        ///// </summary>
        //public int SkillsID3
        //{
        //    set; get;
        //}
        /// <summary>
        /// 掉落铜钱
        /// </summary>
        public int DroppedCoin
        {
            set; get;
        }
        /// <summary>
        /// 铜钱掉率
        /// </summary>
        public int CoinRate
        {
            set; get;
        }
        /// <summary>
        /// 掉落ID1
        /// </summary>
        public List<int> Dropped;
        //public int DroppedID1
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 掉落ID2
        ///// </summary>
        //public int DroppedID2
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 掉落ID3
        ///// </summary>
        //public int DroppedID3
        //{
        //    set; get;
        //}
        /// <summary>
        /// 掉率1
        /// </summary>
        public List<int> DroppedRate;
        //public int DroppedRateID1
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 掉率2
        ///// </summary>
        //public int DroppedRateID2
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 掉率3
        ///// </summary>
        //public int DroppedRateID3
        //{
        //    set; get;
        //}
        /// <summary>
        /// 任务ID1
        /// </summary>
        public List<MonsterTaskObject> MonsterTask;

        /// <summary>
        /// 宠物表
        /// </summary>
        public List<int> PetsTable = null;
        /// <summary>
        /// 魂魄表
        /// </summary>
        public int SpiritsTable
        {
            set; get;
        }
        /// <summary>
        /// 御灵表
        /// </summary>
        public int OrenTable
        {
            set; get;
        }
        /// <summary>
        /// 喊话ID
        /// </summary>
        public List<int> MonsterCall = null;
        /// <summary>
        /// 存活时间
        /// </summary>
        public int SurvivalTime
        {
            set; get;
        }
        /// <summary>
        /// 怪物头像
        /// </summary>
        public string Ico
        {
            set; get;
        }
        /// <summary>
        ///怪物所在的区
        /// </summary>
        public MonsterZone monsterZone
        {
            set; get;
        }
    }

    /// <summary>
    /// 区域爆怪管理类
    /// </summary>
    public class MonsterZone
    {
        #region 基本属性

        /// <summary>
        /// 地图编号
        /// </summary>
        public int MapCode
        {
            get;
            set;
        }

        /// <summary>
        /// 爆怪的区域ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// 要爆怪的ID
        /// </summary>
        public int Code
        {
            get;
            set;
        }
        
        /// <summary>
        /// 爆怪的X坐标
        /// </summary>
        public int ToX
        {
            get;
            set;
        }
        
        /// <summary>
        /// 爆怪的Y坐标
        /// </summary>
        public int ToY
        {
            get;
            set;
        }
        
        /// <summary>
        /// 要爆怪的半径
        /// </summary>
        public int Radius
        {
            get;
            set;
        }
        
        /// <summary>
        /// 要爆怪的总个数
        /// </summary>
        public int TotalNum
        {
            get;
            set;
        }

        /// <summary>
        /// 多长时间曝一次怪
        /// </summary>
        public int Timeslot
        {
            get;
            set;
        }

        /// <summary>
        /// 最大的追踪距离
        /// </summary>
        public int PursuitRadius
        {
            get;
            set;
        }

        /// <summary>
        /// 爆怪的类型, 0: 按照时间间隔爆怪, 1: 按照时间点爆怪, 2: 系统控制，用户主动召唤, 3 用户主动召唤区域，和2的功能一致，但比
        /// 2的功能丰富，2主要用于实现之前的生肖宝典刷怪，而3召唤出来的怪都是一次性的，可由玩家控制或者不控制的系统零时怪
        /// </summary>
        public int BirthType
        {
            get;
            set;
        }

        /// <summary>
        /// 配置的刷怪方式，主要用于记录配置文件中的刷怪方式，当刷怪方式是4的时候，会动态转换为 0 或 1
        /// </summary>
        public int ConfigBirthType
        {
            get;
            set;
        }

        /// <summary>
        /// 开服多少天之后开始刷怪 小于等于0表示 开服当天开始刷，大于0就是相应天数
        /// </summary>
        public int SpawnMonstersAfterKaiFuDays
        {
            get;
            set;
        }

        /// <summary>
        /// 持续刷怪天数 小于等于0表示一直刷，大于0就是相应天数
        /// </summary>
        public int SpawnMonstersDays
        {
            get;
            set;
        }

        /// <summary>
        /// 每周的哪天刷怪 为空就代表一直刷  MU 新增[1/10/2014 LiaoWei]
        /// </summary>
        public List<BirthTimeForDayOfWeek> SpawnMonstersDayOfWeek
        {
            get;
            set;
        }

        /// <summary>
        /// 爆怪物的时间点列表
        /// </summary>
        public List<BirthTimePoint> BirthTimePointList
        {
            get;
            set;
        }

        /// <summary>
        /// 爆怪的概率(最大万分)
        /// </summary>
        public int BirthRate
        {
            get;
            set;
        }

        /// <summary>
        /// 判断系统是否已经全部杀死本区域怪物,刷怪类型为4的时候用到
        /// </summary>
        private Boolean HasSystemKilledAllOfThisZone = false;

        /// <summary>
        /// 是否是副本地图中的区域
        /// </summary>
        public bool IsFuBenMap = false;

        /// <summary>
        /// 爆怪的类型
        /// </summary>
        public MonsterTypes MonsterType = MonsterTypes.None;

        /// <summary>
        /// 最后一次区域复活调度的时间
        /// </summary>
        private long LastReloadTicks = 0;

        /// <summary>
        /// 最后一次区域销毁死亡的副本怪物的时间
        /// </summary>
        private long LastDestroyTicks = 0;

        /// <summary>
        /// 上次爆怪物的天ID
        /// </summary>
        private int LastBirthDayID = -1;

        /// <summary>
        /// 上次爆怪物的时间点
        /// </summary>
        private BirthTimePoint LastBirthTimePoint = null;

        /// <summary>
        /// 上次爆怪物的时间点的索引
        /// </summary>
        private int LastBirthTimePointIndex = -1;

        #endregion 基本属性

        #region 怪物静态数据

        /// <summary>
        /// 静态引用的怪物数据
        /// </summary>
#if ___CC___FUCK___YOU___BB___
        private XMonsterStaticInfo XMonsterInfo = new XMonsterStaticInfo();
        /// <summary>
        /// 静态引用的怪物数据
        /// </summary>
        public XMonsterStaticInfo GetMonsterInfo()
        {
            return XMonsterInfo;
        }

#else
        private MonsterStaticInfo MonsterInfo = new MonsterStaticInfo();
        /// <summary>
        /// 静态引用的怪物数据
        /// </summary>
        public MonsterStaticInfo GetMonsterInfo()
        {
            return MonsterInfo;
        }
#endif


        ///// <summary>
        ///// 获取或设置姓名
        ///// </summary>
        //public string VSName
        //{
        //    get { return MonsterInfo.VSName;  }
        //}

        ///// <summary>
        ///// 扩展ID
        ///// </summary>
        //public int ExtensionID
        //{
        //    get { return MonsterInfo.ExtensionID; }
        //}

        ///// <summary>
        ///// 获取或设置等级
        ///// </summary>
        //public int VLevel
        //{
        //    get { return MonsterInfo.VLevel; }
        //}

        ///// 经验值是当前的级别修炼值，如果升级，扣除升级的经验值
        ///// <summary>
        ///// 获取或设置自身的经验值(如果为怪物等NPC,则为杀它的玩家可以得到的经验值)
        ///// </summary>
        //public int VExperience
        //{
        //    get { return MonsterInfo.VExperience; }
        //}

        ///// <summary>
        ///// 获取或设置自身的金币(如果为怪物等NPC,则为杀它的玩家可以得到的金币)
        ///// </summary>
        //public int VMoney
        //{
        //    get { return MonsterInfo.VMoney; }
        //}

        ///// <summary>
        ///// 获取最大生命值
        ///// </summary>
        //public double VLifeMax
        //{
        //    get { return MonsterInfo.VLifeMax; }
        //}

        ///// <summary>
        ///// 获取最大魔法值
        ///// </summary>
        //public double VManaMax
        //{
        //    get { return MonsterInfo.VManaMax; }
        //}

        ///// <summary>
        ///// 对应的职业
        ///// </summary>
        //public int ToOccupation
        //{
        //    get { return MonsterInfo.ToOccupation; }
        //}

        ///// <summary>
        ///// 各个动作的帧的速度
        ///// </summary>
        //public int[] SpriteSpeedTickList
        //{
        //    get { return MonsterInfo.SpriteSpeedTickList; }
        //}

        ///// <summary>
        ///// 获取或设置精灵各动作对应的帧列个数
        ///// </summary>
        //public int[] EachActionFrameRange
        //{
        //    get { return MonsterInfo.EachActionFrameRange; }
        //}

        ///// <summary>
        ///// 获取或设置各动作产生实际效果的针序号
        ///// </summary>
        //public int[] EffectiveFrame
        //{
        //    get { return MonsterInfo.EffectiveFrame; }
        //}

        ///// <summary>
        ///// 获取或设置索敌范围(距离)
        ///// </summary>
        //public int SeekRange
        //{
        //    get { return MonsterInfo.SeekRange; }
        //}

        ///// <summary>
        ///// 获取或设置精灵当前衣服代码
        ///// </summary>
        //public int EquipmentBody
        //{
        //    get { return MonsterInfo.EquipmentBody; }
        //}

        ///// <summary>
        ///// 获取或设置精灵当前武器代码
        ///// </summary>
        //public int EquipmentWeapon
        //{
        //    get { return MonsterInfo.EquipmentWeapon; }
        //}

        ///// <summary>
        ///// 对应角色最小攻击力
        ///// </summary>
        //public int MinAttack
        //{
        //    get { return MonsterInfo.MinAttack; }
        //}

        ///// <summary>
        ///// 对应角色最大攻击力
        ///// </summary>
        //public int MaxAttack
        //{
        //    get { return MonsterInfo.MaxAttack; }
        //}

        ///// <summary>
        ///// 对应角色的防御力
        ///// </summary>
        //public int Defense
        //{
        //    get { return MonsterInfo.Defense; }
        //}

        ///// <summary>
        ///// 魔防
        ///// </summary>
        //public int MDefense
        //{
        //    get { return MonsterInfo.MDefense; }
        //}

        ///// <summary>
        ///// 命中率
        ///// </summary>
        //public double HitV
        //{
        //    get { return MonsterInfo.HitV; }
        //}

        ///// <summary>
        ///// 闪避率
        ///// </summary>
        //public double Dodge
        //{
        //    get { return MonsterInfo.Dodge; }
        //}

        ///// <summary>
        ///// 生命恢复速度(每隔5秒百分之多少)
        ///// </summary>
        //public double RecoverLifeV
        //{
        //    get { return MonsterInfo.RecoverLifeV; }
        //}

        ///// <summary>
        ///// 魔法恢复速度(每隔5秒百分之多少)
        ///// </summary>
        //public double RecoverMagicV
        //{
        //    get { return MonsterInfo.RecoverMagicV; }
        //}

        ///// <summary>
        ///// 怪物掉落ID
        ///// </summary>
        //public int FallGoodsPackID
        //{
        //    get { return MonsterInfo.FallGoodsPackID; }
        //}

        ///// <summary>
        ///// 攻击方式(0: 物理攻击, 1: 魔法攻击, 2: 道术攻击)
        ///// </summary>
        //public int AttackType
        //{
        //    get { return MonsterInfo.AttackType; }
        //}

        ///// <summary>
        ///// 大乱斗个人积分
        ///// </summary>
        //public int BattlePersonalJiFen
        //{
        //    get { return MonsterInfo.BattlePersonalJiFen; }
        //}

        ///// <summary>
        ///// 大乱斗阵营积分
        ///// </summary>
        //public int BattleZhenYingJiFen
        //{
        //    get { return MonsterInfo.BattleZhenYingJiFen; }
        //}

        //// 恶魔广场 血色堡垒 begin [11/14/2013 LiaoWei]
        ///// <summary>
        ///// 恶魔广场积分
        ///// </summary>
        //public int DaimonSquareJiFen
        //{
        //    get { return MonsterInfo.DaimonSquareJiFen; }
        //}

        ///// <summary>
        ///// 血色堡垒积分
        ///// </summary>
        //public int BloodCastJiFen
        //{
        //    get { return MonsterInfo.BloodCastJiFen; }
        //}

        ///// <summary>
        ///// 掉落是否属于拥有者
        ///// </summary>
        //public int FallBelongTo
        //{
        //    get { return MonsterInfo.FallBelongTo; }
        //}

        ///// <summary>
        ///// 怪物挂的技能ID列表
        ///// </summary>
        //public int[] SkillIDs
        //{
        //    get { return MonsterInfo.SkillIDs; }
        //}

        ///// <summary>
        ///// 怪物所属阵营
        ///// </summary>
        //public int Camp
        //{
        //    get { return MonsterInfo.Camp; }
        //}

        #endregion 怪物静态数据

        #region 怪物列表

        /// <summary>
        /// 当前区域的怪列表
        /// </summary>
        private List<Monster> MonsterList = new List<Monster>(100);

#endregion 怪物列表

#region 怪物样本(副本地图才起作用)

        /// <summary>
        /// 副本地图中的怪物样本
        /// </summary>
        private Monster SeedMonster = null;

        #endregion 怪物样本(副本地图才起作用)

        #region 副本地图相关属性

        #endregion 副本地图相关属性

        #region 初始化怪

        /// <summary>
        /// 加载精灵类型控件
        /// </summary>
        /// <param name="sprite">引参:对象精灵</param>
        /// <param name="roleID">角色ID</param>
        /// <param name="roleSex">性别</param>
        /// <param name="name">识别名</param>
        /// <param name="sname">角色名</param>
        /// <param name="life">当前生命值</param>
        /// <param name="mana">当前魔法值</param>
        /// <param name="level">等级</param>
        /// <param name="experience">经验值</param>
        /// <param name="buff">属性BUFF加/减持</param>
        /// <param name="facesign">头像</param>
        /// <param name="frameRange">各动作帧数</param>
        /// <param name="effectiveFrame">各动作起效帧</param>
        /// <param name="attackRange">物理攻击距离</param>
        /// <param name="seekRange">索敌距离</param>
        /// <param name="equipmentBody">衣服代号</param>
        /// <param name="equipmentWeapon">武器代号</param>
        /// <param name="coordinate">XY坐标</param>
        /// <param name="direction">朝向</param>
        /// <param name="holdWidth">朝向</param>
        /// <param name="holdHeight">朝向</param>
#if ___CC___FUCK___YOU___BB___
        private void LoadMonster(Monster monster, MonsterZone monsterZone, XMonsterStaticInfo xmonsterInfo, int monsterType, int roleID,
            string name, double life, double mana, Point coordinate, double direction, double moveSpeed, int attackRange)
        {
            //monster.SpriteSpeedTickList = speedTickList;
            
                //LogManager.WriteLog(LogTypes.Robot, string.Format("加载怪物数据  mapcode {3} ID{0} 坐X={1} Y={2} ", roleID, coordinate.X, coordinate.Y, MapCode));
                //SysConOut.WriteLine(string.Format("加载怪物数据  mapcode {3} ID{0} 坐X={1} Y={2} ", roleID, coordinate.X, coordinate.Y, MapCode));

            monster.Name = name;
            monster.MonsterZoneNode = monsterZone;
            monster.XMonsterInfo = xmonsterInfo;
            monster.RoleID = roleID;
            //monster.RoleSex = roleSex;
            //monster.VSName = sname;
            //monster.ExtensionID = extensionID;
            monster.VLife = life*10000;//HX_SERVER FOR TEST
            monster.VMana = mana;
            //monster.VLifeMax = life;
            //monster.VManaMax = mana;
            //monster.MonsterInfo.VLevel = level;
            //monster.MonsterZoneNode.VExperience = experience;
            //monster.MonsterZoneNode.VMoney = money;
            //monster.Buff = buff;
            //monster.FaceSign = facesign;
            //monster.EachActionFrameRange = frameRange;
            //monster.EffectiveFrame = effectiveFrame;
            monster.AttackRange = attackRange;
            //monster.SeekRange = seekRange;//* tmp
            //monster.EquipmentBody = equipmentBody;
            //monster.EquipmentWeapon = equipmentWeapon;
            //monster.CenterX = centerX;
            //monster.CenterY = centerY;
            monster.FirstCoordinate = coordinate;
            monster.Coordinate = coordinate;
            monster.Direction = direction;
            monster.CurrentPos = coordinate;
            //monster.HoldWidth = holdWidth;
            //monster.HoldHeight = holdHeight;
            monster.MoveSpeed = moveSpeed;
            //monster.ToOccupation = toOccupation;
            //monster.ToRoleLevel = toRoleLevel;
            //monster.MinAttack = minAttack;
            //monster.MaxAttack = maxAttack;
            //monster.Defense = defense;
            //monster.MDefense = magicDefense;
            //monster.HitV = hitV;
            //monster.Dodge = dodge;
            //monster.RecoverLifeV = recoverLifeV;
            //monster.RecoverMagicV = recoverMagicV;
            //monster.FallGoodsPackID = fallGoodsPackID;
            monster.MonsterType = monsterType;
            //monster.BattlePersonalJiFen = Global.GMax(0, battlePersonalJiFen);
            //monster.BattleZhenYingJiFen = Global.GMax(0, battleZhenYingJiFen);
            //monster.DaimonSquareJiFen = Global.GMax(0, nDaimonSquareJiFen); // 恶魔广场积分 add by liaowei
            //monster.BloodCastJiFen = Global.GMax(0, nBloodCastJiFen);       // 血色堡垒积分 add by liaowei 
            //monster.FallBelongTo = Global.GMax(0, fallBelongTo);
            //monster.SkillIDs = skillIDs;
            //monster.AttackType = attackType;
            //monster.Camp = camp; //* tmp
            //monster.ZhenQiMinValue = Global.GMax(0, zhenQiMinVal);
            //monster.ZhenQiMaxValue = Global.GMax(0, zhenQiMaxVal);

            monster.CoordinateChanged += UpdateMonsterEvent;

            //人为导致搜索的时间间隔错开
            monster.NextSeekEnemyTicks = Global.MonsterSearchTimer + Global.GetRandomNumber(0, Global.MonsterSearchRandomTimer);
        }
#else
         private void LoadMonster(Monster monster, MonsterZone monsterZone, MonsterStaticInfo monsterInfo, int monsterType, int roleID,
            string name, double life, double mana, Point coordinate, double direction, double moveSpeed, int attackRange)
        {
            //monster.SpriteSpeedTickList = speedTickList;

            monster.Name = name;
            monster.MonsterZoneNode = monsterZone;
            monster.MonsterInfo = monsterInfo;
            monster.RoleID = roleID;
            //monster.RoleSex = roleSex;
            //monster.VSName = sname;
            //monster.ExtensionID = extensionID;
            monster.VLife = life*10000;//HX_SERVER FOR TEST
            monster.VMana = mana;
            //monster.VLifeMax = life;
            //monster.VManaMax = mana;
            //monster.MonsterInfo.VLevel = level;
            //monster.MonsterZoneNode.VExperience = experience;
            //monster.MonsterZoneNode.VMoney = money;
            //monster.Buff = buff;
            //monster.FaceSign = facesign;
            //monster.EachActionFrameRange = frameRange;
            //monster.EffectiveFrame = effectiveFrame;
            monster.AttackRange = attackRange;
            //monster.SeekRange = seekRange;//* tmp
            //monster.EquipmentBody = equipmentBody;
            //monster.EquipmentWeapon = equipmentWeapon;
            //monster.CenterX = centerX;
            //monster.CenterY = centerY;
            monster.FirstCoordinate = coordinate;
            monster.Coordinate = coordinate;
            monster.Direction = direction;
            //monster.HoldWidth = holdWidth;
            //monster.HoldHeight = holdHeight;
            monster.MoveSpeed = moveSpeed;
            //monster.ToOccupation = toOccupation;
            //monster.ToRoleLevel = toRoleLevel;
            //monster.MinAttack = minAttack;
            //monster.MaxAttack = maxAttack;
            //monster.Defense = defense;
            //monster.MDefense = magicDefense;
            //monster.HitV = hitV;
            //monster.Dodge = dodge;
            //monster.RecoverLifeV = recoverLifeV;
            //monster.RecoverMagicV = recoverMagicV;
            //monster.FallGoodsPackID = fallGoodsPackID;
            monster.MonsterType = monsterType;
            //monster.BattlePersonalJiFen = Global.GMax(0, battlePersonalJiFen);
            //monster.BattleZhenYingJiFen = Global.GMax(0, battleZhenYingJiFen);
            //monster.DaimonSquareJiFen = Global.GMax(0, nDaimonSquareJiFen); // 恶魔广场积分 add by liaowei
            //monster.BloodCastJiFen = Global.GMax(0, nBloodCastJiFen);       // 血色堡垒积分 add by liaowei 
            //monster.FallBelongTo = Global.GMax(0, fallBelongTo);
            //monster.SkillIDs = skillIDs;
            //monster.AttackType = attackType;
            //monster.Camp = camp; //* tmp
            //monster.ZhenQiMinValue = Global.GMax(0, zhenQiMinVal);
            //monster.ZhenQiMaxValue = Global.GMax(0, zhenQiMaxVal);

            monster.CoordinateChanged += UpdateMonsterEvent;

            //人为导致搜索的时间间隔错开
            monster.NextSeekEnemyTicks = Global.MonsterSearchTimer + Global.GetRandomNumber(0, Global.MonsterSearchRandomTimer);
        }
#endif
        /// <summary>
        /// 复制一份Monster数据
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        private Monster CopyMonster(Monster oldMonster)
        {
            /*
             * Monster monster = new Monster();
            monster.SpriteSpeedTickList = oldMonster.SpriteSpeedTickList;
            monster.Name = oldMonster.Name;
            monster.MonsterZoneNode = oldMonster.MonsterZoneNode;
            monster.RoleID = oldMonster.RoleID;
            monster.RoleSex = oldMonster.RoleSex;
            monster.VSName = oldMonster.VSName;
            monster.MonsterInfo.ExtensionID = oldMonster.MonsterInfo.ExtensionID;
            monster.VLife = oldMonster.VLife;
            monster.VMana = oldMonster.VMana;
            monster.MonsterInfo.VLifeMax = oldMonster.MonsterInfo.VLifeMax;
            monster.VManaMax = oldMonster.VManaMax;
            monster.MonsterInfo.VLevel = oldMonster.MonsterInfo.VLevel;
            monster.MonsterZoneNode.VExperience = oldMonster.MonsterZoneNode.VExperience;
            monster.MonsterZoneNode.VMoney = oldMonster.MonsterZoneNode.VMoney;
            monster.Buff = oldMonster.Buff;
            monster.FaceSign = oldMonster.FaceSign;
            monster.EachActionFrameRange = oldMonster.EachActionFrameRange;
            monster.EffectiveFrame = oldMonster.EffectiveFrame;
            monster.AttackRange = oldMonster.AttackRange;
            monster.SeekRange = oldMonster.SeekRange;
            monster.EquipmentBody = oldMonster.EquipmentBody;
            monster.EquipmentWeapon = oldMonster.EquipmentWeapon;
            monster.CenterX = oldMonster.CenterX;
            monster.CenterY = oldMonster.CenterY;
            //monster.FirstCoordinate = oldMonster.FirstCoordinate;
            //monster.Coordinate = oldMonster.Coordinate;
            monster.Direction = oldMonster.Direction;
            monster.HoldWidth = oldMonster.HoldWidth;
            monster.HoldHeight = oldMonster.HoldHeight;
            monster.MoveSpeed = oldMonster.MoveSpeed;
            monster.ToOccupation = oldMonster.ToOccupation;
            monster.ToRoleLevel = oldMonster.ToRoleLevel;
            monster.MinAttack = oldMonster.MinAttack;
            monster.MaxAttack = oldMonster.MaxAttack;
            monster.Defense = oldMonster.Defense;
            monster.MDefense = oldMonster.MDefense;
            monster.HitV = oldMonster.HitV;
            monster.Dodge = oldMonster.Dodge;
            monster.RecoverLifeV = oldMonster.RecoverLifeV;
            monster.RecoverMagicV = oldMonster.RecoverMagicV;
            monster.FallGoodsPackID = oldMonster.FallGoodsPackID;
            monster.MonsterType = oldMonster.MonsterType;
            monster.BattlePersonalJiFen = oldMonster.BattlePersonalJiFen;
            monster.BattleZhenYingJiFen = oldMonster.BattleZhenYingJiFen;
            monster.FallBelongTo = oldMonster.FallBelongTo;
            */

            Monster monster = oldMonster.Clone();

            monster.CoordinateChanged += UpdateMonsterEvent;

            //添加移动结束事件
            monster.MoveToComplete += MoveToComplete;

            //初始化移动的目标点
            //monster.MoveToPos = new Point(-1, -1);

            return monster;
        }

        /// <summary>
        /// 销毁怪物 这个函数是真正销毁怪物的地方，怪物一旦被销毁，就意味着所有的引用都会消除，
        /// 相应的内存空间也会被释放
        /// </summary>
        /// <param name="monster"></param>
        private void DestroyMonster(Monster monster)
        {
            //如果怪物有主人，则从主人的队列移除
            if (monster.OwnerClient != null)
            {
                monster.OwnerClient.ClientData.SummonMonstersList.Remove(monster);
                monster.OwnerClient = null;
            }

            monster.CoordinateChanged -= UpdateMonsterEvent;//这儿是 + 还是 -
            monster.MoveToComplete -= MoveToComplete;

            //将精灵从地图格子中删除
            GameManager.MapGridMgr.DictGrids[MapCode].RemoveObject(monster);

            //从当前区域队列中删除
            bool ret = MonsterList.Remove(monster);

            //将一个怪物从管理队列中删除(副本动态刷怪会用到)
            GameManager.MonsterMgr.RemoveMonster(monster);

            //将怪物ID 还回管理器，以便重用
            GameManager.MonsterIDMgr.PushBack(monster.RoleID);

            //减少计数
            if (ret)
            {
                Monster.DecMonsterCount(); //防止某些情况下重复的调用，导致负数
            }
       }

        /// <summary>
        /// 初始化怪物数据
        /// </summary>
        /// <param name="monsterXml"></param>
        /// <returns></returns>
        /// 
#if ___CC___FUCK___YOU___BB___
        private Monster InitMonster(XElement monsterXml, double maxLifeV, double moveSpeed,bool attachEvent = true)
        {
            GameMap gameMap = GameManager.MapMgr.DictMaps[MapCode];
            

            Monster monster = new Monster();
            int roleID = (int)GameManager.MonsterIDMgr.GetNewID(MapCode);
            monster.UniqueID = Global.GetUniqueID();

            LoadMonster(
                    monster, //引参:对象精灵
                    this,
                    this.XMonsterInfo,
                    (int)Global.GetSafeAttributeLong(monsterXml, "Type"), //怪物的类型
                    roleID,
                    string.Format("Role_{0}", roleID), //识别名
                    maxLifeV,
                    0,
                    Global.GetMapPointByGridXY(ObjectTypes.OT_MONSTER, MapCode, ToX, ToY, Radius, 0, true), //人的位置X/Y坐标
                    Global.GetRandomNumber(0, 8), //朝向
                    moveSpeed,
                    (int)Global.GetSafeAttributeLong(monsterXml, "AlertRange") //物理攻击距离, 70个格子？？
                );


            //这个字段保证动态生成种子 monster的时候，不会绑定这个事件，默认值为true 同时兼容旧代码
            if (attachEvent)
            {
                //添加移动结束事件
                monster.MoveToComplete += MoveToComplete;
            }

            //初始化移动的目标点
            //monster.MoveToPos = new Point(-1, -1);

            return monster;
        }

#else
        private Monster InitMonster(XElement monsterXml, double maxLifeV, double maxMagicV, XElement xmlFrameConfig, /*XElement xmlPictureConfig, */double moveSpeed, /*int[] speedTickList, */bool attachEvent = true)
        {
            GameMap gameMap = GameManager.MapMgr.DictMaps[MapCode];

            //if ((int)Global.GetSafeAttributeLong(monsterXml, "ID") == 5)
            //{
            //    System.Diagnostics.Debug.WriteLine("abc");
            //}

            Monster monster = new Monster();
            int roleID = (int)GameManager.MonsterIDMgr.GetNewID(MapCode);
            monster.UniqueID = Global.GetUniqueID();

             LoadMonster(
                    monster, //引参:对象精灵
                    this,
                    this.MonsterInfo,
                    (int)Global.GetSafeAttributeLong(monsterXml, "MonsterType"), //怪物的类型
                    roleID,
                    string.Format("Role_{0}", roleID), //识别名
                    maxLifeV,
                    maxMagicV,
                    Global.GetMapPointByGridXY(ObjectTypes.OT_MONSTER, MapCode, ToX, ToY, Radius, 0, true), //人的位置X/Y坐标
                    Global.GetRandomNumber(0, 8), //朝向
                    moveSpeed,
                    (int)Global.GetSafeAttributeLong(monsterXml, "AttackRange") //物理攻击距离, 70个格子？？
                );
            //这个字段保证动态生成种子 monster的时候，不会绑定这个事件，默认值为true 同时兼容旧代码
            if (attachEvent)
            {
                //添加移动结束事件
                monster.MoveToComplete += MoveToComplete;
            }

            //初始化移动的目标点
            //monster.MoveToPos = new Point(-1, -1);

            return monster;
        }

#endif


        /// <summary>
        /// 根据爆怪的概率是否能爆怪
        /// </summary>
        private bool CanRealiveByRate()
        {
            if (BirthRate >= 10000)
            {
                return true;
            }

            int randNum = Global.GetRandomNumber(1, 10001);
            if (randNum <= BirthRate)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 加载静态的怪物信息
        /// </summary>
#if ___CC___FUCK___YOU___BB___
        public void LoadStaticMonsterInfo()
        {
            string fileName = string.Format("Config/Monsters.xml");

            XElement xml = GameManager.MonsterZoneMgr.AllMonstersXml;
            if (xml == null)
            {
                throw new Exception(string.Format("加载系统怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", Code.ToString());
            
            //先取子节点，便于获取
          //  XElement xmlFrameConfig = Global.GetSafeXElement(xml, "FrameConfig");
          //  XElement xmlSpeedConfig = Global.GetSafeXElement(xml, "MoveSpeed");
            //int[] speedTickList = Global.StringArray2IntArray(Global.GetSafeAttributeStr(xmlSpeedConfig, "Tick").Split(','));
           // double moveSpeed = Global.GetSafeAttributeDouble(xml, "MoveSpeed");

            double moveSpeed = Global.GetSafeAttributeDouble(monsterXml, "MoveSpeed"); //怪物移动的速度
           

            int maxLifeV = (int)Global.GetSafeAttributeLong(monsterXml, "MaxHP"); //当前生命值
            if (maxLifeV <= 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的时，怪物的数据配置错误，生命值不能小于等于0: MonsterID={0}, MonsterName={1}",
                    (int)Global.GetSafeAttributeLong(monsterXml, "ID"), Global.GetSafeAttributeStr(monsterXml, "SName")));

                return;
            }

            //初始化怪物数据
            InitMonsterStaticInfo(monsterXml, maxLifeV,moveSpeed);
        }
#else
            public void LoadStaticMonsterInfo()
        {
            string fileName = string.Format("Config/Monsters.xml");

            XElement xml = GameManager.MonsterZoneMgr.AllMonstersXml;
            if (xml == null)
            {
                throw new Exception(string.Format("加载系统怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", Code.ToString());

            //首先根据地图编号定位地图文件
            //fileName = string.Format("Role/{0:000}/0/{1:000}/{2:000}.xml",
            //    Global.GetSpriteBodyCode(GSpriteTypes.Monster),
            //    (int)Global.GetSafeAttributeLong(monsterXml, "Sex"), (int)Global.GetSafeAttributeLong(monsterXml, "Code"));
            fileName = string.Format("GuaiWu/{0}.xml",
                Global.GetSafeAttributeStr(monsterXml, "ResName"));

            string defaultFileName = string.Format("GuaiWu/ceshi_guaiwu.unity3d.xml");
            //if (!File.Exists(Global.GameResPath(fileName)))
            //{
            //    LogManager.WriteLog(LogTypes.Error, string.Format("加载指定怪物的衣服文件:{0}, 失败。启用默认XML配置文件!", fileName));
            //    fileName = defaultFileName;                
            //}

            try
            {
                xml = null;
                string fileFullName = Global.ResPath(fileName);
                if (File.Exists(fileFullName))
                {
                    xml = XElement.Load(fileFullName);
                }
            }
            catch (Exception)
            {
                xml = null;
            }

            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Info, string.Format("加载指定怪物的衣服文件:{0}, {1}, 失败。启用默认XML配置文件!", Global.GetSafeAttributeStr(monsterXml, "SName"), fileName));
                fileName = defaultFileName;

                xml = null;
                string fileFullName = Global.ResPath(fileName);
                if (File.Exists(fileFullName))
                {
                    xml = XElement.Load(fileFullName);
                }
                if (null == xml)
                {
                    throw new Exception(string.Format("加载指定怪物的衣服代号:{0}, 失败。没有找到相关XML配置文件!", fileName));
                }
            }

            //先取子节点，便于获取
            XElement xmlFrameConfig = Global.GetSafeXElement(xml, "FrameConfig");
            //XElement xmlPictureConfig = Global.GetSafeXElement(xml, "PictureConfig");
            //XElement xmlSpriteConfig = Global.GetSafeXElement(xml, "SpriteConfig");
            XElement xmlSpeedConfig = Global.GetSafeXElement(xml, "SpeedConfig");
            int[] speedTickList = Global.StringArray2IntArray(Global.GetSafeAttributeStr(xmlSpeedConfig, "Tick").Split(','));
            double moveSpeed = Global.GetSafeAttributeDouble(xmlSpeedConfig, "UnitSpeed") / 100.0;

            double monsterSpeed = Global.GetSafeAttributeDouble(monsterXml, "MonsterSpeed"); //怪物移动的速度
            moveSpeed *= monsterSpeed;

            int maxLifeV = (int)Global.GetSafeAttributeLong(monsterXml, "MaxLife"); //当前生命值
            int maxMagicV = (int)Global.GetSafeAttributeLong(monsterXml, "MaxMagic"); //当前魔法值
            if (maxLifeV <= 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的时，怪物的数据配置错误，生命值不能小于等于0: MonsterID={0}, MonsterName={1}",
                    (int)Global.GetSafeAttributeLong(monsterXml, "ID"), Global.GetSafeAttributeStr(monsterXml, "SName")));

                return;
            }

            //初始化怪物数据
            InitMonsterStaticInfo(monsterXml, maxLifeV, maxMagicV, xmlFrameConfig, /*xmlPictureConfig, */moveSpeed, speedTickList);
        }
#endif


        /// <summary>
        /// 初始化怪
        /// </summary>
#if ___CC___FUCK___YOU___BB___
        public void LoadMonsters()
        {

            string fileName = string.Format("Config/Monsters.xml");

            XElement xml = GameManager.MonsterZoneMgr.AllMonstersXml;
            if (xml == null)
            {
                throw new Exception(string.Format("加载系统怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", Code.ToString());

            //添加到怪物名称管理
            MonsterNameManager.AddMonsterName(Code, Global.GetSafeAttributeStr(monsterXml, "Name"));

            double moveSpeed = Global.GetSafeAttributeDouble(monsterXml, "MoveSpeed"); //怪物移动的速度

            double maxLifeV = (int)Global.GetSafeAttributeLong(monsterXml, "MaxHP"); //当前生命值
            if (maxLifeV <= 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的时，怪物的数据配置错误，生命值不能小于等于0: MonsterID={0}, MonsterName={1}",
                    (int)Global.GetSafeAttributeLong(monsterXml, "ID"), Global.GetSafeAttributeStr(monsterXml, "SName")));
                return;
            }
            Monster monster = null;
            if (!IsFuBenMap) //如果不是副本地图
            {
                for (int i = 0; i < TotalNum; i++)
                {
                    //初始化怪物数据
                    monster = InitMonster(monsterXml, maxLifeV, moveSpeed);
                    if (MonsterTypes.None == MonsterType)
                    {
                        MonsterType = (MonsterTypes)monster.MonsterType;
                    }

                    //加入当前区域队列
                    MonsterList.Add(monster);

                    //添加到全局的队列
                    GameManager.MonsterMgr.AddMonster(monster);

                    //怪物野外Boss管理
                    if ((int)MonsterTypes.BOSS == monster.MonsterType)
                    {
                        MonsterBossManager.AddBoss(monster);
                    }
                }
            }
            else //如果是副本地图，则只生成一个怪物的样本
            {
                //初始化怪物数据
                monster = InitMonster(monsterXml, maxLifeV,moveSpeed);
                if (MonsterTypes.None == MonsterType)
                {
                    MonsterType = (MonsterTypes)monster.MonsterType;
                }

                SeedMonster = monster;
            }
        }
#else
              public void LoadMonsters()
        {
            
            string fileName = string.Format("Config/Monsters.xml");

            XElement xml = GameManager.MonsterZoneMgr.AllMonstersXml;
            if (xml == null)
            {
                throw new Exception(string.Format("加载系统怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", Code.ToString());

            //添加到怪物名称管理
            MonsterNameManager.AddMonsterName(Code, Global.GetSafeAttributeStr(monsterXml, "SName"));

            //首先根据地图编号定位地图文件
            //fileName = string.Format("Role/{0:000}/0/{1:000}/{2:000}.xml",
            //    Global.GetSpriteBodyCode(GSpriteTypes.Monster),
            //    0/*(int)Global.GetSafeAttributeLong(monsterXml, "Sex")*/, (int)Global.GetSafeAttributeLong(monsterXml, "Code"));
            fileName = string.Format("GuaiWu/{0}.xml",
                Global.GetSafeAttributeStr(monsterXml, "ResName"));

            string defaultFileName = string.Format("GuaiWu/ceshi_guaiwu.unity3d.xml");
            //if (!File.Exists(Global.GameResPath(fileName)))
            //{
            //    LogManager.WriteLog(LogTypes.Error, string.Format("加载指定怪物的衣服文件:{0}, 失败。启用默认XML配置文件!", fileName));
            //    fileName = defaultFileName;                
            //}

            try
            {
                xml = XElement.Load(Global.ResPath(fileName));
            }
            catch (Exception)
            {
                xml = null;
            }

            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Info, string.Format("加载指定怪物的衣服文件:{0}, {1}, 失败。启用默认XML配置文件!", Global.GetSafeAttributeStr(monsterXml, "SName"), fileName));
                fileName = defaultFileName;

                xml = XElement.Load(Global.ResPath(fileName));
                if (null == xml)
                {
                    throw new Exception(string.Format("加载指定怪物的衣服代号:{0}, 失败。没有找到相关XML配置文件!", fileName));
                }
            }

            //先取子节点，便于获取
            XElement xmlFrameConfig = Global.GetSafeXElement(xml, "FrameConfig");
            //XElement xmlPictureConfig = Global.GetSafeXElement(xml, "PictureConfig");
            //XElement xmlSpriteConfig = Global.GetSafeXElement(xml, "SpriteConfig");
            XElement xmlSpeedConfig = Global.GetSafeXElement(xml, "SpeedConfig");
            //int[] speedTickList = Global.StringArray2IntArray(Global.GetSafeAttributeStr(xmlSpeedConfig, "Tick").Split(','));
            double moveSpeed = Global.GetSafeAttributeDouble(xmlSpeedConfig, "UnitSpeed") / 100.0;

            double monsterSpeed = Global.GetSafeAttributeDouble(monsterXml, "MonsterSpeed"); //怪物移动的速度
            moveSpeed *= monsterSpeed;

            double maxLifeV = (int)Global.GetSafeAttributeLong(monsterXml, "MaxLife"); //当前生命值
            double maxMagicV = (int)Global.GetSafeAttributeLong(monsterXml, "MaxMagic"); //当前魔法值
            if (maxLifeV <= 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的时，怪物的数据配置错误，生命值不能小于等于0: MonsterID={0}, MonsterName={1}",
                    (int)Global.GetSafeAttributeLong(monsterXml, "ID"), Global.GetSafeAttributeStr(monsterXml, "SName")));

                return;
            }

            Monster monster = null;
            if (!IsFuBenMap) //如果不是副本地图
            {
                for (int i = 0; i < TotalNum; i++)
                {
                    //初始化怪物数据
                    monster = InitMonster(monsterXml, maxLifeV, maxMagicV, xmlFrameConfig, /*xmlPictureConfig, */moveSpeed/*, speedTickList*/);
                    if (MonsterTypes.None == MonsterType)
                    {
                        MonsterType = (MonsterTypes)monster.MonsterType;
                    }

                    //加入当前区域队列
                    MonsterList.Add(monster);

                    //添加到全局的队列
                    GameManager.MonsterMgr.AddMonster(monster);

                    //怪物野外Boss管理
                    if ((int)MonsterTypes.BOSS == monster.MonsterType)
                    {
                        MonsterBossManager.AddBoss(monster);
                    }

                    //是否是根据时间段来爆怪, 根据时间段爆怪的后边处理===>这儿先不处理，统一在刷怪处处理，便于处理开服时间限制，刷怪日期限制
                    /*
                    if ((int)MonsterBirthTypes.TimeSpan == BirthType)
                    {
                        //根据爆怪的概率是否能爆怪
                        if (CanRealiveByRate())
                        {
                            //将精灵放入格子
                            if (!GameManager.MapGridMgr.DictGrids[MapCode].MoveObject(-1, -1, (int)monster.Coordinate.X, (int)monster.Coordinate.Y, monster))
                            {
                                LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的范围错误，超出了地图的最大限制: MonsterID={0}, MonsterName={1}",
                                    monster.MonsterInfo.ExtensionID, monster.VSName));
                            }

                            //怪物生命计时器开始
                            monster.Start();

                            /// 怪物进行了移动
                            Global.MonsterMoveGrid(monster);
                        }
                    }
                    */
                }
            }
            else //如果是副本地图，则只生成一个怪物的样本
            {
                //初始化怪物数据
                monster = InitMonster(monsterXml, maxLifeV, maxMagicV, xmlFrameConfig, /*xmlPictureConfig, */moveSpeed/*, speedTickList*/);
                if (MonsterTypes.None == MonsterType)
                {
                    MonsterType = (MonsterTypes)monster.MonsterType;
                }

                SeedMonster = monster;
            }
        }
#endif


        /// <summary>
        /// 加载 动态怪物种子，这个函数会被调用多次，每一次被调用之前，其相关参数已经被更改
        /// 返回的结果将用于 怪物的动态创建
        /// 这样写可以用之前 LoadMonsters()代码
        /// 程序初始化的时候会调用这个函数，执行过程中找不到怪物种子的时候可能会重新初始化调用
        /// </summary>
        /// <returns></returns>
        /// 
#if ___CC___FUCK___YOU___BB___
        public Monster LoadDynamicMonsterSeed()
        {

            string fileName = string.Format("Config/Monsters.xml");
            XElement xml = null;

            try
            {
                xml = XElement.Load(Global.GameResPath(fileName));
                if (null == xml)
                {
                    throw new Exception(string.Format("加载系统怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载系统怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }


            XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", Code.ToString());

            //添加到怪物名称管理
            MonsterNameManager.AddMonsterName(Code, Global.GetSafeAttributeStr(monsterXml, "Name"));


            double moveSpeed = Global.GetSafeAttributeDouble(monsterXml, "MoveSpeed"); //怪物移动的速度

            double maxLifeV = (int)Global.GetSafeAttributeLong(monsterXml, "MaxHP"); //当前生命值
            if (maxLifeV <= 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的时，怪物的数据配置错误，生命值不能小于等于0: MonsterID={0}, MonsterName={1}",
                    (int)Global.GetSafeAttributeLong(monsterXml, "ID"), Global.GetSafeAttributeStr(monsterXml, "SName")));

                return null;
            }

            //初始化怪物数据
            return InitMonster(monsterXml, maxLifeV,moveSpeed, false);
        }
#else
             public Monster LoadDynamicMonsterSeed()
        {
            
            string fileName = string.Format("Config/Monsters.xml");
            XElement xml = null;

            try
            {
                xml = XElement.Load(Global.GameResPath(fileName));
                if (null == xml)
                {
                    throw new Exception(string.Format("加载系统怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载系统怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }
            

            XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", Code.ToString());

            //添加到怪物名称管理
            MonsterNameManager.AddMonsterName(Code, Global.GetSafeAttributeStr(monsterXml, "SName"));

            //首先根据地图编号定位地图文件
            //fileName = string.Format("Role/{0:000}/0/{1:000}/{2:000}.xml",
            //    Global.GetSpriteBodyCode(GSpriteTypes.Monster),
            //    (int)Global.GetSafeAttributeLong(monsterXml, "Sex"), (int)Global.GetSafeAttributeLong(monsterXml, "Code"));
            fileName = string.Format("GuaiWu/{0}.xml",
                Global.GetSafeAttributeStr(monsterXml, "ResName"));

            string defaultFileName = string.Format("GuaiWu/ceshi_guaiwu.unity3d.xml");
            //if (!File.Exists(Global.GameResPath(fileName)))
            //{
            //    LogManager.WriteLog(LogTypes.Error, string.Format("加载指定怪物的衣服文件:{0}, 失败。启用默认XML配置文件!", fileName));
            //    fileName = defaultFileName;                
            //}

            try
            {
                xml = null;
                string fileFullName = Global.ResPath(fileName);
                if (File.Exists(fileFullName))
                {
                    xml = XElement.Load(fileFullName);
                }
            }
            catch (Exception)
            {
                xml = null;
            }

            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Info, string.Format("加载指定怪物的衣服文件:{0}, {1}, 失败。启用默认XML配置文件!", Global.GetSafeAttributeStr(monsterXml, "SName"), fileName));
                fileName = defaultFileName;

                xml = null;
                string fileFullName = Global.ResPath(fileName);
                if (File.Exists(fileFullName))
                {
                    xml = XElement.Load(fileFullName);
                }
                if (null == xml)
                {
                    throw new Exception(string.Format("加载指定怪物的衣服代号:{0}, 失败。没有找到相关XML配置文件!", fileName));
                }
            }

            //先取子节点，便于获取
            XElement xmlFrameConfig = Global.GetSafeXElement(xml, "FrameConfig");
            //XElement xmlPictureConfig = Global.GetSafeXElement(xml, "PictureConfig");
            //XElement xmlSpriteConfig = Global.GetSafeXElement(xml, "SpriteConfig");
            XElement xmlSpeedConfig = Global.GetSafeXElement(xml, "SpeedConfig");
            //int[] speedTickList = Global.StringArray2IntArray(Global.GetSafeAttributeStr(xmlSpeedConfig, "Tick").Split(','));
            double moveSpeed = Global.GetSafeAttributeDouble(xmlSpeedConfig, "UnitSpeed") / 100.0;

            double monsterSpeed = Global.GetSafeAttributeDouble(monsterXml, "MonsterSpeed"); //怪物移动的速度
            moveSpeed *= monsterSpeed;

            double maxLifeV = (int)Global.GetSafeAttributeLong(monsterXml, "MaxLife"); //当前生命值
            double maxMagicV = (int)Global.GetSafeAttributeLong(monsterXml, "MaxMagic"); //当前魔法值
            if (maxLifeV <= 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的时，怪物的数据配置错误，生命值不能小于等于0: MonsterID={0}, MonsterName={1}",
                    (int)Global.GetSafeAttributeLong(monsterXml, "ID"), Global.GetSafeAttributeStr(monsterXml, "SName")));

                return null;
            }

            //初始化怪物数据
            return InitMonster(monsterXml, maxLifeV, maxMagicV, xmlFrameConfig, /*xmlPictureConfig, */moveSpeed/*, speedTickList*/, false);
        }
#endif


        /// <summary>
        /// 移动结束事件
        /// </summary>
        /// <param name="sender"></param>
        private void MoveToComplete(object sender)
        {
            //(sender as Monster).MoveToPos = new Point(-1, -1); //防止重入
            (sender as Monster).DestPoint = new Point(-1, -1);
            (sender as Monster).Action = GActions.Stand; //不需要通知，同一会执行动作
            Global.RemoveStoryboard((sender as Monster).Name);
        }

        /// <summary>
        /// 由主角坐标变化触发游戏画面中对象位置刷新
        /// </summary>
        /// <param name="sprite"></param>
        private void UpdateMonsterEvent(Monster monster)
        {
            //如果是原始坐标，则不通知九宫格，导致的其他问题比较少，移动中如果遇到此坐标，也会被连续的移动补救。
            //if (!((int)monster.FirstCoordinate.X == (int)monster.Coordinate.X && (int)monster.FirstCoordinate.Y == (int)monster.Coordinate.Y))
            if (!monster.FirstStoryMove)
            {
                //if (monster.MonsterInfo.ExtensionID == 38)
                //{
                //    System.Diagnostics.Debug.WriteLine(string.Format("UpdateMonsterEvent, monster={0}, X={1}, Y={2}, SafeX={3}, SafeY={4}",
                //        monster.MonsterInfo.ExtensionID, (int)monster.Coordinate.X, (int)monster.Coordinate.Y, (int)monster.SafeCoordinate.X, (int)monster.SafeCoordinate.Y));
                //}

                //将精灵放入格子
                GameManager.MapGridMgr.DictGrids[MapCode].MoveObject((int)monster.SafeOldCoordinate.X, (int)monster.SafeOldCoordinate.Y, (int)monster.SafeCoordinate.X, (int)monster.SafeCoordinate.Y, monster);

                /// 玩家进行了移动
                //Global.MonsterMoveGrid(monster);
            }
            else
            {
                monster.FirstStoryMove = false;
            }
        }

#endregion //初始化怪

#region 区域怪物复活

        /// <summary>
        /// 获取下一个爆怪的时间点字符串
        /// </summary>
        /// <returns></returns>
        public string GetNextBirthTimePoint()
        {
            if (BirthType == (int)MonsterBirthTypes.CreateDayOfWeek && SpawnMonstersDayOfWeek != null)
            {
                DateTime nowTime = TimeUtil.NowDateTime();
                int dayOfWeek = 0;
                int birthHour = 0;
                int birthMinite = 0;

                int nextIndex = -1;
                if (LastBirthTimePointIndex >= 0)
                {
                    nextIndex = (LastBirthTimePointIndex + 1) % SpawnMonstersDayOfWeek.Count;
                }
                else
                {
                    DateTime now = TimeUtil.NowDateTime();
                    int time1 = (int)now.DayOfWeek * 1440 + now.Hour * 60 + now.Minute;

                    for (int i = 0; i < SpawnMonstersDayOfWeek.Count; i++)
                    {
                        int time2 = SpawnMonstersDayOfWeek[i].BirthDayOfWeek * 1440 + SpawnMonstersDayOfWeek[i].BirthTime.BirthHour * 60 + SpawnMonstersDayOfWeek[i].BirthTime.BirthMinute;
                        if (time1 <= time2)
                        {
                            nextIndex = i;
                            break;
                        }
                    }
                    if (nextIndex < 0)
                    {
                        nextIndex = 0;
                    }
                }

                dayOfWeek = (int)SpawnMonstersDayOfWeek[nextIndex].BirthDayOfWeek;
                birthHour = SpawnMonstersDayOfWeek[nextIndex].BirthTime.BirthHour;
                birthMinite = SpawnMonstersDayOfWeek[nextIndex].BirthTime.BirthMinute;

                return string.Format("{0}${1}${2}", birthHour, birthMinite, dayOfWeek);
            }
            else
            {
                if (null == BirthTimePointList)
                {
                    return "";
                }

                //原子操作
                int lastBirthTimePointIndex = LastBirthTimePointIndex;

                //上次爆怪物的时间点的索引
                int nextIndex = 0;
                if (lastBirthTimePointIndex >= 0)
                {
                    nextIndex = (lastBirthTimePointIndex + 1) % BirthTimePointList.Count;
                }
                else
                {
                    DateTime now = TimeUtil.NowDateTime();
                    int time2 = now.Hour * 60 + now.Minute;
                    for (int i = 0; i < BirthTimePointList.Count; i++)
                    {
                        int time1 = BirthTimePointList[i].BirthHour * 60 + BirthTimePointList[i].BirthMinute;
                        if (time2 <= time1)
                        {
                            nextIndex = i;
                            break;
                        }
                    }
                }

                return string.Format("{0}${1}", BirthTimePointList[nextIndex].BirthHour, BirthTimePointList[nextIndex].BirthMinute);
            }
        }

        /// <summary>
        /// 是否能爆怪
        /// </summary>
        /// <param name="birthTimePoint"></param>
        /// <returns></returns>
        private bool CanBirthOnTimePoint(DateTime now, BirthTimePoint birthTimePoint)
        {
            if (now.DayOfYear == LastBirthDayID) //如果天数相同
            {
                if (null != LastBirthTimePoint)
                {
                    if (LastBirthTimePoint.BirthHour == birthTimePoint.BirthHour &&
                            LastBirthTimePoint.BirthMinute == birthTimePoint.BirthMinute)
                    {
                        return false; //说明今天的这个时间点已经爆过怪物了
                    }
                }
            }

            if (now.Hour != birthTimePoint.BirthHour)
            {
                return false;
            }

            int minMinute = birthTimePoint.BirthMinute;
            int maxMinute = birthTimePoint.BirthMinute + 1;
            return (now.Minute >= minMinute && now.Minute <= maxMinute);
        }

        /// <summary>
        /// 是否能爆怪 每周哪天刷怪 MU 新增[1/10/2014 LiaoWei]
        /// </summary>
        /// <param name="birthTimePoint"></param>
        /// <returns></returns>
        private bool CanBirthOnTimePointForWeekOfDay(DateTime now, BirthTimePoint birthTimePoint)
        {
            if (now.DayOfYear == LastBirthDayID) //如果天数相同
            {
                if (null != LastBirthTimePoint)
                {
                    if (LastBirthTimePoint.BirthHour == birthTimePoint.BirthHour &&
                            LastBirthTimePoint.BirthMinute == birthTimePoint.BirthMinute)
                    {
                        return false; //说明今天的这个时间点已经爆过怪物了
                    }
                }
            }

            if (now.Hour != birthTimePoint.BirthHour)
            {
                return false;
            }

            int minMinute = birthTimePoint.BirthMinute;
            int maxMinute = birthTimePoint.BirthMinute + 1;
            return (now.Minute >= minMinute && now.Minute <= maxMinute);
        }

        /// <summary>
        /// 根据当前的怪剩余个数，重新爆怪 1.普通地图，不能是副本地图 2.怪物复活
        /// 怪物复活机制 a.非副本地图，只有birth 类型为 0 和 为1 的区域 才会被循环线程定期判断并复活
        /// b 副本地图，基础规则是副本地图不管什么怪物一旦加载，不再复活,如果调用相应reload函数，则意味着
        /// 相关区域不管什么birth类型，怪物死掉的全部复活
        /// c 存在 birth类型为2的怪物，如果在副本地图中，和其它类型处理上没区别，如果在非副本地图中，则不会
        /// 被定期复活，除非明显调用相应的reload函数
        /// 因此，总共有三个reload函数，针对非副本地图2个，针对副本地图一个
        /// 
        /// 针对上述原则，为了实现动态刷怪，进入如下处理
        /// birth为2的区域，强行不管是否副本地图，都不允许用旧的函数进行reload操作，必须采用新的函数进行reload，
        /// 目前出去非副本地图有一个对birth为2的区域的支持函数外，不需要其它函数，暂时不用写。
        /// </summary>
        public void ReloadMonsters(SocketListener sl, TCPOutPacketPool pool)
        {
            //副本地图无复活机制
            if (IsFuBenMap)
            {
                return;
            }

            DateTime now = TimeUtil.NowDateTime();

            //判断在指定的地图上是否在许可的时间段内
            if (!Global.CanMapInLimitTimes(MapCode, now)) //没有在刷怪的时间内
            {
                return;
            }

            //今天是否能刷怪,今天不能刷怪，直接返回，活着的全杀死
            if (!CanTodayReloadMonsters() || !CanTodayReloadMonstersForDayOfWeek())
            {
                if (!HasSystemKilledAllOfThisZone)
                {
                    SystemKillAllMonstersOfThisZone();
                    HasSystemKilledAllOfThisZone = true;
                }
                return;
            }

            //重置系统杀死怪物标志，这样保证在未刷怪时间点怪物全死
            HasSystemKilledAllOfThisZone = false;

            // code == -1的动态区域，有异常
            if (Code > 0 && ConfigBirthType == (int)MonsterBirthTypes.AfterJieRiDays)
            {
                try
                {
                    // 如果是节日boss复活，那么重新加载一下MonsterInfo
                    // 前提是策划已经热更了Config/Monsters.xml
                    LoadStaticMonsterInfo();
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Error, "reload jieri boss monster failed", ex);
                }
            }

            if ((int)MonsterBirthTypes.TimeSpan == BirthType) //按照时间段的爆怪机制
            {
                long ticks = now.Ticks;                
                if (LastReloadTicks <= 0 || ticks - LastReloadTicks >= (1 * 1000 * 10000))
                {
                    LastReloadTicks = ticks;

                    //重新刷新怪
                    MonsterRealive(sl, pool);
                }
            }
            else if ((int)MonsterBirthTypes.TimePoint == BirthType) //按照时间点的爆怪机制
            {
                //时间有爆怪的时间点
                if (null != BirthTimePointList)
                {
                    //上次爆怪物的时间点的索引
                    int nextIndex = 0;
                    if (LastBirthTimePointIndex >= 0)
                    {
                        nextIndex = (LastBirthTimePointIndex + 1) % BirthTimePointList.Count;
                    }
                    else
                    {
                        for (int i = 0; i < BirthTimePointList.Count; i++)
                        {
                            if (CanBirthOnTimePoint(now, BirthTimePointList[i]))
                            {
                                nextIndex = i;
                                break;
                            }
                        }
                    }

                    //查找下一个爆怪的时间点
                    BirthTimePoint birthTimePoint = BirthTimePointList[nextIndex];

                    //判断是否能够爆怪
                    if (CanBirthOnTimePoint(now, birthTimePoint))
                    {
                        //记住这次爆怪的索引
                        LastBirthTimePointIndex = nextIndex;

                        //记住这次爆怪的时间点
                        LastBirthTimePoint = birthTimePoint;

                        //上次爆怪物的天ID
                        LastBirthDayID = TimeUtil.NowDateTime().DayOfYear;

                        //重新刷新怪
                        MonsterRealive(sl, pool);
                    }
                }
            }
            else if ((int)MonsterBirthTypes.CreateDayOfWeek == BirthType) // 每周几刷怪 [1/10/2014 LiaoWei]
            {
                if (SpawnMonstersDayOfWeek == null)
                    return;

                DateTime nowTime = TimeUtil.NowDateTime();
                DayOfWeek nDayOfWeek = nowTime.DayOfWeek;

                for (int i = 0; i < SpawnMonstersDayOfWeek.Count; ++i)
                {
                    int nDay = SpawnMonstersDayOfWeek[i].BirthDayOfWeek;

                    if (nDay == (int)nDayOfWeek)
                    {
                        BirthTimePoint time = SpawnMonstersDayOfWeek[i].BirthTime;

                        //判断是否能够爆怪
                        if (CanBirthOnTimePoint(now, time))
                        {
                            //记住这次爆怪的索引
                            LastBirthTimePointIndex = i;

                            //记住这次爆怪的时间点
                            LastBirthTimePoint = time;

                            //上次爆怪物的天ID
                            LastBirthDayID = TimeUtil.NowDateTime().DayOfYear;

                            //重新刷新怪
                            MonsterRealive(sl, pool);
                        }

                    }
                }


            }
        }

        /// <summary>
        /// 判断现在是否能刷怪，主要针对原始刷怪类型为4的配置进行开服后多少天的刷怪控制
        /// 如果不能刷怪，则外部需要系统强行杀死所有本区域的怪
        /// </summary>
        /// <returns></returns>
        public Boolean CanTodayReloadMonsters()
        {
            if (SpawnMonstersAfterKaiFuDays <= 0 && SpawnMonstersDays <= 0)
            {
                return true;
            }

            DateTime kaifuTime = Global.GetKaiFuTime();
            if (ConfigBirthType == (int)MonsterBirthTypes.AfterHeFuDays)
            {
                // 检查是否开启了该活动
                HeFuActivityConfig config = HuodongCachingMgr.GetHeFuActivityConfing();
                if (null == config)
                    return false;
                if (!config.InList((int)ActivityTypes.HeFuBossAttack))
                    return false;

                kaifuTime = Global.GetHefuStartDay();
            }
            else if (ConfigBirthType == (int)MonsterBirthTypes.AfterJieRiDays)
            {
                // 检查是否开启了该活动
                JieriActivityConfig config = HuodongCachingMgr.GetJieriActivityConfig();
                if (null == config)
                    return false;
                if (!config.InList((int)ActivityTypes.JieriBossAttack))
                    return false;

                kaifuTime = Global.GetJieriStartDay();
            }

            DateTime now = TimeUtil.NowDateTime();
            int days2Kaifu = Global.GetDaysSpanNum(now, kaifuTime) + 1;

            if (SpawnMonstersAfterKaiFuDays <= 0 || days2Kaifu >= SpawnMonstersAfterKaiFuDays)
            {
                if (SpawnMonstersDays <= 0 || days2Kaifu < SpawnMonstersDays + SpawnMonstersAfterKaiFuDays)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断现在是否能刷怪 针对MU新增的一周哪天能刷怪 [1/10/2014 LiaoWei]
        /// </summary>
        /// <returns></returns>
        public Boolean CanTodayReloadMonstersForDayOfWeek()
        {
            if (SpawnMonstersDayOfWeek == null)
                return true;

            if (ConfigBirthType != (int)MonsterBirthTypes.CreateDayOfWeek) // 周几刷怪[1/10/2014 LiaoWei]
                return true;

            DateTime now = TimeUtil.NowDateTime();
            DayOfWeek nDayOfWeek = now.DayOfWeek;

            for (int i = 0; i < SpawnMonstersDayOfWeek.Count; ++i)
            {
                int nDay = SpawnMonstersDayOfWeek[i].BirthDayOfWeek;

                if (nDay == (int)nDayOfWeek)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 杀死本区域的所有怪物
        /// </summary>
        /// <returns></returns>
        public void SystemKillAllMonstersOfThisZone()
        {
            for (int n = 0; n < MonsterList.Count; n++)
            {
                if (null == MonsterList[n])
                {
                    continue;
                }
                if (MonsterList[n].Alive)
                {
                    Global.SystemKillMonster(MonsterList[n]);
                }
            }
        }

        /// <summary>
        /// 未来避免怪物复活时奇怪的隐身操作(可能坐标点不对，导致无法在事件中处理)
        /// </summary>
        /// <param name="monster"></param>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        private void RepositionMonster(Monster monster, int toX, int toY)
        {
            //将精灵放入格子
            GameManager.MapGridMgr.DictGrids[MapCode].MoveObject(-1, -1, toX, toY, monster);

            /// 玩家进行了移动
            //Global.MonsterMoveGrid(monster);
        }

        /// <summary>
        /// 遍历判断怪是否需要复活(主线程调用)
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void MonsterRealive(SocketListener sl, TCPOutPacketPool pool, int copyMapID = -1, int birthCount = 65535)
        {
            int haveBirthCount = 0;
            for (int i = 0; i < MonsterList.Count; i++)
            {
                if (null == MonsterList[i])
                {
                    continue;
                }
                //超过了最大的召唤个数
                if (haveBirthCount >= birthCount)
                {
                    break;
                }

                if (-1 != copyMapID)
                {
                    if (MonsterList[i].CopyMapID != copyMapID)
                    {
                        continue;
                    }
                }

                if (!MonsterList[i].Alive) //如果怪物已经死亡
                {
                    if (((int)MonsterBirthTypes.TimeSpan == BirthType || (int)MonsterBirthTypes.CopyMapLike == BirthType) && Timeslot > 0) //如果是按照时间段来刷怪的这里要进行判断 // 增加副本复活时间 [3/3/2014 LiaoWei]
                    {
                        long monsterRealiveTimeslot = ((long)Timeslot * 1000L * 10000L);
                        if (TimeUtil.NOW() * 10000 - MonsterList[i].LastDeadTicks < monsterRealiveTimeslot)
                        {
                            continue;
                        }
                    }

                    //根据爆怪的概率是否能爆怪
                    if (CanRealiveByRate())
                    {
                        // 转入界面线程
                        Point pt = MonsterList[i].Realive();

                        //未来避免怪物复活时奇怪的隐身操作(可能坐标点不对，导致无法在事件中处理)
                        RepositionMonster(MonsterList[i], (int)pt.X, (int)pt.Y);

                        List<Object> listObjs = Global.GetAll9Clients(MonsterList[i]);
                        GameManager.ClientMgr.NotifyMonsterRealive(sl, pool, MonsterList[i], MapCode, MonsterList[i].CopyMapID, MonsterList[i].RoleID, (int)MonsterList[i].Coordinate.X, (int)MonsterList[i].Coordinate.Y, (int)MonsterList[i].Direction, listObjs);

                        haveBirthCount++;

                        //System.Diagnostics.Debug.WriteLine(string.Format("刷出{0}个怪", haveBirthCount));
                        if ((int)MonsterTypes.BOSS == MonsterList[i].MonsterType)
                        {
                            if ((int)MonsterBirthTypes.TimeSpan == BirthType && Timeslot >= (30 * 60)) //大于30分钟的才报告
                            {
                                //通知客户端boss刷新了
                                GameManager.ClientMgr.NotifyAllImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    null, StringUtil.substitute(Global.GetLang("[{0}]出现在[{1}],请各位勇士速速前往击杀。"), 
                                    MonsterList[i].XMonsterInfo.Name, Global.GetMapName(MonsterList[i].CurrentMapCode)), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
                            }
                        }
#if ___CC___FUCK___YOU___BB___
                        if (Global.IsGongGaoReliveMonster(MonsterList[i].XMonsterInfo.MonsterId))
                        {
                            GameManager.ClientMgr.NotifyAllImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    null, StringUtil.substitute(Global.GetLang("【{0}】出现了【{1}】的身影，战胜它就有机会获得珍贵宝藏哦！"),
                                    Global.GetMapName(MonsterList[i].CurrentMapCode), MonsterList[i].XMonsterInfo.Name), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
                        }
#else
                           if (Global.IsGongGaoReliveMonster(MonsterList[i].MonsterInfo.ExtensionID))
                        {
                            GameManager.ClientMgr.NotifyAllImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    null, StringUtil.substitute(Global.GetLang("【{0}】出现了【{1}】的身影，战胜它就有机会获得珍贵宝藏哦！"),  Global.GetMapName(MonsterList[i].CurrentMapCode), MonsterList[i].MonsterInfo.VSName), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
                        }
#endif


                        // 刷黄金部队或世界BOSS
                        if (BirthType == (int)MonsterBirthTypes.TimePoint || BirthType == (int)MonsterBirthTypes.CreateDayOfWeek)
                        {
#if ___CC___FUCK___YOU___BB___
                            /// 处理BOSS复活时图标状态刷新
                            TimerBossManager.getInstance().AddBoss(BirthType, MonsterList[i].XMonsterInfo.MonsterId);
#else
                            /// 处理BOSS复活时图标状态刷新
                            TimerBossManager.getInstance().AddBoss(BirthType, MonsterList[i].MonsterInfo.ExtensionID);
#endif
                        }
                    }
                }
            }
        }

#endregion 区域怪物复活

#region 副本地图区域怪物动态生成和回收

        /// <summary>
        /// 初始化副本地图怪
        /// </summary>
        public void LoadCopyMapMonsters(int copyMapID)
        {
            if (!IsFuBenMap)
            {
                return; //不是副本不处理
            }

            if (null == SeedMonster)
            {
                return; //没有种子怪物, 不处理
            }

            Monster monster = null;
            for (int i = 0; i < TotalNum; i++)
            {
                monster = CopyMonster(SeedMonster);

                int roleID = (int)GameManager.MonsterIDMgr.GetNewID(MapCode);
                monster.RoleID = roleID;
                monster.UniqueID = Global.GetUniqueID();
                monster.Name = string.Format("Role_{0}", roleID);
                monster.CopyMapID = copyMapID;
                monster.FirstCoordinate = Global.GetMapPointByGridXY(ObjectTypes.OT_MONSTER, MapCode, ToX, ToY, Radius, 0, true); //人的位置X/Y坐标

                if (Global.InOnlyObsByXY(ObjectTypes.OT_MONSTER, monster.CurrentMapCode, (int)monster.FirstCoordinate.X, (int)monster.FirstCoordinate.Y))
                {
                    System.Diagnostics.Debug.WriteLine("abc");
                }

                //monster.Coordinate = monster.FirstCoordinate;
                monster.Direction = Global.GetRandomNumber(0, 8);

                //加入当前区域队列
                MonsterList.Add(monster);

                //添加到全局的队列
                GameManager.MonsterMgr.AddMonster(monster);

                //将精灵放入格子
                //if (!GameManager.MapGridMgr.DictGrids[MapCode].MoveObject(-1, -1, (int)monster.Coordinate.X, (int)monster.Coordinate.Y, monster))
                //{
                //    LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的范围错误，超出了地图的最大限制: MonsterID={0}, MonsterName={1}",
                //        monster.MonsterInfo.ExtensionID, monster.VSName));
                //}

                //怪物生命计时器开始
                monster.Start(); //放在MoveGrid前边，否则不会发送, 因为Alive还未激活

                //当Coordinate竖线改变的时候，会激活UpdateMonsterEvent函数，将monster放入格子，并通知九宫格内的玩家
                monster.Coordinate = monster.FirstCoordinate;

                /// 怪物进行了移动
                //Global.MonsterMoveGrid(monster);
            }
        }

        /// <summary>
        /// 立刻重新刷新副本怪物(特殊使用)---->复活副本怪物【1.必须是副本， 2.怪物必须死亡才会复活】
        /// </summary>
        public void ReloadCopyMapMonsters(SocketListener sl, TCPOutPacketPool pool, int copyMapID)
        {
            //非副本地图不执行
            if (!IsFuBenMap)
            {
                return;
            }

            
            if ((int)MonsterBirthTypes.CopyMapLike != BirthType) //是否是用户召唤的爆怪机制
            {
                return;
            }
            
            //重新刷新怪
            MonsterRealive(sl, pool, copyMapID);
        }

        /// <summary>
        /// 清除副本地图怪
        /// </summary>
        public void ClearCopyMapMonsters(int copyMapID)
        {
            //不是副本地图无销毁机制
            if (!IsFuBenMap)
            {
                return;
            }

            List<Monster> monsterList = new List<Monster>();
            bool bExistNull = false;
            for (int i = 0; i < MonsterList.Count; i++)
            {
                if (null == MonsterList[i])
                {
                    bExistNull = true;
                    continue;
                }
                if (MonsterList[i].CopyMapID == copyMapID)
                {
                    monsterList.Add(MonsterList[i]);
                }
            }

            for (int i = 0; i < monsterList.Count; i++)
            {
                DestroyMonster(monsterList[i]);
            }

            if (bExistNull)
            {
                MonsterList.RemoveAll((x) => { return null == x; });
            }
        }

        /// <summary>
        /// 遍历判断副本地图怪是否需要销毁(主线程调用)
        /// 如果仅仅是副本才销毁，而当前地图不是副本地图，则无销毁机制
        /// onlyFuBen 默认值true 兼容老的执行方式
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        public void DestroyDeadMonsters(bool onlyFuBen = true)
        {
            //如果仅仅是副本才销毁，而当前地图不是副本地图，则无销毁机制
            if (!IsFuBenMap && onlyFuBen)
            {
                return;
            }

            if (BirthType == (int)MonsterBirthTypes.CopyMapLike)
            {
                return;
            }

            long ticks = TimeUtil.NOW() * 10000;
            long monsterDestroyTimeslot = (30 * 1000 * 10000);
            if (ticks - LastDestroyTicks < monsterDestroyTimeslot)
            {
                return;
            }

            LastDestroyTicks = ticks;

            List<Monster> monsterList = new List<Monster>();
            bool bExistNull = false;
            for (int i = 0; i < MonsterList.Count; i++)
            {
                if (null == MonsterList[i])
                {
                    bExistNull = true;
                    continue;
                }
                if (!MonsterList[i].Alive) //如果怪物已经死亡
                {
                    monsterList.Add(MonsterList[i]);
                }
            }

            for (int i = 0; i < monsterList.Count; i++)
            {
                DestroyMonster(monsterList[i]);
            }

            if (bExistNull)
            {
                MonsterList.RemoveAll((x) => { return null == x; });
                LogManager.WriteLog(LogTypes.Error, string.Format("DestroyDeadMonsters MonsterList Exist Null!!!"));
            }
        }

#region 动态召唤怪物相关
        /// <summary>
        /// 销毁死掉的动态生成的怪物,不管副本不副本，只要 birthtype == 3的，都销毁
        /// </summary>
        public void DestroyDeadDynamicMonsters()
        {
            if (IsDynamicZone())
            {
                DestroyDeadMonsters(false);
            }
        }

        /// <summary>
        /// 判断是否动态刷怪区域 动态刷怪区域刷出的怪，都是属于玩家的怪，玩家可以带走并穿越地图 每个地图都有一个动态
        /// 刷怪区域，便于玩家带怪穿越地图
        /// </summary>
        /// <returns></returns>
        public Boolean IsDynamicZone()
        {
            return (int)MonsterBirthTypes.CrossMap == BirthType;
        }

        /// <summary>
        /// 初始化动态生成的地图怪物
        /// </summary>
        public Monster LoadDynamicRobot(MonsterZoneQueueItem monsterZoneQueueItem)
        {
            Monster monster = monsterZoneQueueItem.seedMonster;
            //int roleID = (int)GameManager.MonsterIDMgr.GetNewID(MapCode);
            //monster.RoleID = roleID;
            //monster.UniqueID = Global.GetUniqueID();
            monster.Tag = monsterZoneQueueItem.Tag;
            monster.ManagerType = monsterZoneQueueItem.ManagerType;
            //monster.Name = string.Format("Role_{0}", monster.RoleID);
            monster.CopyMapID = monsterZoneQueueItem.CopyMapID;

            //这儿一定要配置区域
            monster.MonsterZoneNode = this;
            monster.CoordinateChanged += UpdateMonsterEvent;
            monster.MoveToComplete += MoveToComplete;

            monster.DynamicMonster = true;
            monster.DynamicPursuitRadius = monsterZoneQueueItem.PursuitRadius;

            monster.FirstCoordinate = Global.GetMapPointByGridXY(ObjectTypes.OT_MONSTER, MapCode, monsterZoneQueueItem.ToX, monsterZoneQueueItem.ToY, monsterZoneQueueItem.Radius, 0, true); //人的位置X/Y坐标

            monster.Direction = Global.GetRandomNumber(0, 8);

            //加入当前区域队列
            MonsterList.Add(monster);

            //添加到全局的队列
            GameManager.MonsterMgr.AddMonster(monster);

            //怪物生命计时器开始
            monster.Start(); //放在MoveGrid前边，否则不会发送, 因为Alive还未激活

            //当Coordinate竖线改变的时候，会激活UpdateMonsterEvent函数，将monster放入格子，并通知九宫格内的玩家
            monster.Coordinate = monster.FirstCoordinate;

            JingJiChangManager.getInstance().onRobotBron(monster as Robot);

            return monster;
        }

        /// <summary>
        /// 初始化动态生成的地图怪物
        /// </summary>
        public void LoadDynamicMonsters(MonsterZoneQueueItem monsterZoneQueueItem)
        {
            if (!IsDynamicZone())
            {
                return; //不是动态刷怪区域则返回，调用已经保证，这儿简单再次验证
            }

            if (null == monsterZoneQueueItem || null == monsterZoneQueueItem.seedMonster)
            {
                return; //没有种子怪物, 不处理
            }

            Monster monster = null;
            for (int i = 0; i < monsterZoneQueueItem.BirthCount; i++)
            {
                monster = CopyMonster(monsterZoneQueueItem.seedMonster);

                //如果怪物拥有角色主人
                if (monster.OwnerClient != null)
                {
                    //将怪物加入角色管理列表
                    monster.OwnerClient.ClientData.SummonMonstersList.Add(monster);
                }

                int roleID = (int)GameManager.MonsterIDMgr.GetNewID(MapCode);
                monster.RoleID = roleID;
                monster.UniqueID = Global.GetUniqueID();
                monster.Tag = monsterZoneQueueItem.Tag;
                monster.ManagerType = monsterZoneQueueItem.ManagerType;
                monster.Name = string.Format("Role_{0}", roleID);
                monster.CopyMapID = monsterZoneQueueItem.CopyMapID;

                //这儿一定要配置区域
                monster.MonsterZoneNode = this;
                monster.CoordinateChanged += UpdateMonsterEvent;
                monster.MoveToComplete += MoveToComplete;

                monster.DynamicMonster = true;
                monster.DynamicPursuitRadius = monsterZoneQueueItem.PursuitRadius;

                //如果怪物拥有角色主人
                if (monster.OwnerClient != null)
                {
                    monster.FirstCoordinate = new Point(monster.OwnerClient.ClientData.PosX, monster.OwnerClient.ClientData.PosY);
                }
                else
                {
                    // 万兽谷特殊处理 -- 如果是万兽谷 且召唤的怪是JUSTMOVE 则在配置文件的位置刷怪 [10/17/2013 LiaoWei]
                    if (monster.ManagerType == SceneUIClasses.EMoLaiXiCopy)
                    {
                        monster.FirstCoordinate = new Point(monsterZoneQueueItem.ToX, monsterZoneQueueItem.ToY);
                        monster.Step = 0;
                        monster.PatrolPath = monster.Tag as List<int[]>;
                    }
                    
                    else
                        monster.FirstCoordinate = Global.GetMapPointByGridXY(ObjectTypes.OT_MONSTER, MapCode, monsterZoneQueueItem.ToX, monsterZoneQueueItem.ToY, monsterZoneQueueItem.Radius, 0, true); //人的位置X/Y坐标
                }

                //monster.Coordinate = monster.FirstCoordinate;
                monster.Direction = Global.GetRandomNumber(0, 8);

                // 天使神殿 [3/25/2014 LiaoWei]
                if (monsterZoneQueueItem.MyMonsterZone.MapCode == GameManager.AngelTempleMgr.m_AngelTempleData.MapCode)
                {
                    GameManager.AngelTempleMgr.OnLoadDynamicMonsters(monster);
                }
                else  if (monsterZoneQueueItem.MyMonsterZone.MapCode == MoRi.MoRiJudgeManager.Instance().MapCode)
                {
                    MoRi.MoRiJudgeManager.Instance().OnLoadDynamicMonsters(monster);
                }

                // 剧情副本 [7/25/2014 LiaoWei]
                if (Global.IsStoryCopyMapScene(monsterZoneQueueItem.MyMonsterZone.MapCode))
                {
                    CopyMap mapInfo = null;
                    mapInfo = GameManager.CopyMapMgr.FindCopyMap(monster.CopyMapID);

                    if (mapInfo == null)
                        return;

                    SystemXmlItem systemFuBenItem = null;
                    if (GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(mapInfo.FubenMapID, out systemFuBenItem) && systemFuBenItem != null)
                    {
                        int nBossID = -1;
                        nBossID = systemFuBenItem.GetIntValue("BossID");
#if ___CC___FUCK___YOU___BB___
                        if (nBossID == monster.XMonsterInfo.MonsterId)
                            Global.NotifyClientStoryCopyMapInfo(monster.CopyMapID, 2);
#else
                        if (nBossID == monster.MonsterInfo.ExtensionID)
                            Global.NotifyClientStoryCopyMapInfo(monster.CopyMapID, 2);
#endif

                    }
                }

                //加入当前区域队列
                MonsterList.Add(monster);

                //添加到全局的队列
                GameManager.MonsterMgr.AddMonster(monster);

                GlobalEventSource4Scene.getInstance().fireEvent(new OnCreateMonsterEventObject(monster), (int)monster.ManagerType);

                //将精灵放入格子
                //if (!GameManager.MapGridMgr.DictGrids[MapCode].MoveObject(-1, -1, (int)monster.Coordinate.X, (int)monster.Coordinate.Y, monster))
                //{
                //    LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的范围错误，超出了地图的最大限制: MonsterID={0}, MonsterName={1}",
                //        monster.MonsterInfo.ExtensionID, monster.VSName));
                //}

                //怪物生命计时器开始
                monster.Start(); //放在MoveGrid前边，否则不会发送, 因为Alive还未激活

                //当Coordinate竖线改变的时候，会激活UpdateMonsterEvent函数，将monster放入格子，并通知九宫格内的玩家
                monster.Coordinate = monster.FirstCoordinate;

                /// 怪物进行了移动
                //Global.MonsterMoveGrid(monster);

                //判断如果是特殊的怪物类型，则播放出生特效
                //if ((int)MonsterTypes.DSPetMonster == monster.MonsterType)
                //{
                //    GameManager.ClientMgr.NotifyOthersMyDeco(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                //        monster, monster.MonsterZoneNode.MapCode, monster.CopyMapID, 160, (int)GDecorationTypes.AutoRemove, -1, (int)monster.Coordinate.X, (int)monster.Coordinate.Y, 0, -1, -1, 0, 0, null);
                //}
            }
        }

        // 以下俩函数在多线程环境下调用
        // 可能会导致MonsterList出错
        // 风险很大 先注释掉

        /// <summary>
        /// 将怪物从我的区域移除
        /// </summary>
        /// <param name="monster"></param>
//         public void RemoveMonsterFromMyZone(Monster monster)
//         {
//             //从全局队列移除
//             GameManager.MonsterMgr.RemoveMonster(monster);
// 
//             //列表中移除
//             MonsterList.Remove(monster);
// 
//             //移除坐标响应
//             monster.CoordinateChanged -= UpdateMonsterEvent;
// 
//             //移除移动结束事件
//             monster.MoveToComplete -= MoveToComplete;
// 
//             //区域设置 暂时保留
//             //monster.MonsterZoneNode = null;
//         }

        //将怪物添加到我的区域
//         public void AddMonsterToMyZone(Monster monster)
//         {
//             monster.MonsterZoneNode = this;
// 
//             //添加坐标响应
//             monster.CoordinateChanged += UpdateMonsterEvent;
// 
//             //添加移动结束事件
//             monster.MoveToComplete += MoveToComplete;
// 
//             MonsterList.Add(monster);
// 
//             //添加到全局的队列
//             GameManager.MonsterMgr.AddMonster(monster);
//         }

        /// <summary>
        /// 返回怪物信息字符串
        /// </summary>
        /// <returns></returns>
        public String GetMonstersInfoString()
        {
            int aliveCount = 0;
            int deadCount = 0;

            foreach (var monster in MonsterList)
            {
                if (null == monster)
                {
                    continue;
                }
                if (monster.Alive)
                {
                    aliveCount++;
                }
                else
                {
                    deadCount++;
                }
            }

            if (aliveCount + deadCount > 0)
            {
                return String.Format("地图{0}, {1}副本, aliveCount={2}, deadCount={3}, total={4}",
                    MapCode, IsFuBenMap ? "是" : "不是", aliveCount, deadCount, aliveCount + deadCount);
            }

            return "";
        }
#endregion 动态召唤怪物相关

#endregion 副本地图区域怪物动态生成和回收

#region 普通地图怪物召唤

        /// <summary>
        /// 立刻重新刷新普通地图怪物(特殊使用)  BirthType 必须等于2
        /// </summary>
        public void ReloadNormalMapMonsters(SocketListener sl, TCPOutPacketPool pool, int birthCount)
        {
            //副本地图不执行
            if (IsFuBenMap)
            {
                return;
            }

            if ((int)MonsterBirthTypes.CopyMapLike != BirthType) //是否是用户召唤的爆怪机制
            {
                return;
            }

            //重新刷新怪
            MonsterRealive(sl, pool, -1, birthCount);
        }

#endregion 普通地图怪物召唤

#region 怪物真正死亡 Alive == false
         /// <summary>
        /// 怪物真正死亡时调用 Alive
        ///  为false时调用
        /// </summary>
        public void OnReallyDied(Monster deadMonster)
        {
            //如果是临时召唤的怪物，则应该在管理列表里面移除
        }

#endregion 怪物真正死亡

#region 初始化怪物静态信息

        /// <summary>
        /// 初始化怪物静态数据
        /// </summary>
        /// <param name="monsterXml"></param>
        /// <returns></returns>
#if ___CC___FUCK___YOU___BB___
        private void InitMonsterStaticInfo(XElement monsterXml, int maxLifeV,  double moveSpeed)
        {

            this.XMonsterInfo.MonsterId = (int)Global.GetSafeAttributeLong(monsterXml, "ID");
            this.XMonsterInfo.Name = Global.GetSafeAttributeStr(monsterXml, "Name");
            this.XMonsterInfo.MonsterType = (int)Global.GetSafeAttributeLong(monsterXml, "Type");
            this.XMonsterInfo.Level = (int)Global.GetSafeAttributeLong(monsterXml, "Lvevl"); 
            this.XMonsterInfo.Exp = (int)Global.GetSafeAttributeLong(monsterXml, "Exp");
            this.XMonsterInfo.FiveExp = (int)Global.GetSafeAttributeLong(monsterXml, "FiveExp");
            this.XMonsterInfo.OrenExp = (int)Global.GetSafeAttributeLong(monsterXml, "OrenExp");
            this.XMonsterInfo.PetsExp = (int)Global.GetSafeAttributeLong(monsterXml, "PetsExp");
            this.XMonsterInfo.MaxHP = (int)Global.GetSafeAttributeLong(monsterXml, "MaxHP");
            this.XMonsterInfo.CurHP = this.XMonsterInfo.MaxHP;
            this.XMonsterInfo.Ad = (int)Global.GetSafeAttributeLong(monsterXml, "Ad");
            this.XMonsterInfo.Pd = (int)Global.GetSafeAttributeLong(monsterXml, "Pd");
            this.XMonsterInfo.FireDamage = (int)Global.GetSafeAttributeLong(monsterXml, "FireDamage");
            this.XMonsterInfo.FrostDamage = (int)Global.GetSafeAttributeLong(monsterXml, "FrostDamage");
            this.XMonsterInfo.LightDamage = (int)Global.GetSafeAttributeLong(monsterXml, "LightDamage");
            this.XMonsterInfo.ToxicInjury = (int)Global.GetSafeAttributeLong(monsterXml, "ToxicInjury");
            this.XMonsterInfo.FireResist = (int)Global.GetSafeAttributeLong(monsterXml, "FireResist");
            this.XMonsterInfo.FrozenResist = (int)Global.GetSafeAttributeLong(monsterXml, "FrozenResist");
            this.XMonsterInfo.LightResist = (int)Global.GetSafeAttributeLong(monsterXml, "LightResist");
            this.XMonsterInfo.PoisonResist = (int)Global.GetSafeAttributeLong(monsterXml, "PoisonResist");
            this.XMonsterInfo.DodgeChance = (int)Global.GetSafeAttributeLong(monsterXml, "DodgeChance");
            this.XMonsterInfo.DodgeResis = (int)Global.GetSafeAttributeLong(monsterXml, "DodgeResist");
            this.XMonsterInfo.MoveSpeed = (int)Global.GetSafeAttributeLong(monsterXml, "MoveSpeed");
            this.XMonsterInfo.AlertRange = (int)Global.GetSafeAttributeLong(monsterXml, "AlertRange");
            this.XMonsterInfo.PursuitRange = (int)Global.GetSafeAttributeLong(monsterXml, "PursuitRange");
            this.XMonsterInfo.DisengageRange = (int)Global.GetSafeAttributeLong(monsterXml, "DisengageRange");

            this.XMonsterInfo.Skills = new List<int>();
            if((int)Global.GetSafeAttributeLong(monsterXml, "SkillsID1") > 0)
                this.XMonsterInfo.Skills.Add((int)Global.GetSafeAttributeLong(monsterXml, "SkillsID1"));
            if ((int)Global.GetSafeAttributeLong(monsterXml, "SkillsID2") > 0)
                this.XMonsterInfo.Skills.Add((int)Global.GetSafeAttributeLong(monsterXml, "SkillsID2"));
            if ((int)Global.GetSafeAttributeLong(monsterXml, "SkillsID3") > 0)
                this.XMonsterInfo.Skills.Add((int)Global.GetSafeAttributeLong(monsterXml, "SkillsID3"));
            this.XMonsterInfo.DroppedCoin = (int)Global.GetSafeAttributeLong(monsterXml, "DroppedCoin");
            this.XMonsterInfo.CoinRate = (int)Global.GetSafeAttributeLong(monsterXml, "CoinRate");

            this.XMonsterInfo.Dropped = new List<int>();
            if ((int)Global.GetSafeAttributeLong(monsterXml, "DroppedID1") > 0)
                this.XMonsterInfo.Dropped.Add((int)Global.GetSafeAttributeLong(monsterXml, "DroppedID1"));
            if ((int)Global.GetSafeAttributeLong(monsterXml, "DroppedID2") > 0)
                this.XMonsterInfo.Dropped.Add((int)Global.GetSafeAttributeLong(monsterXml, "DroppedID2"));
            if ((int)Global.GetSafeAttributeLong(monsterXml, "DroppedID3") > 0)
                this.XMonsterInfo.Dropped.Add((int)Global.GetSafeAttributeLong(monsterXml, "DroppedID3"));

            this.XMonsterInfo.DroppedRate = new List<int>();
            if ((int)Global.GetSafeAttributeLong(monsterXml, "DroppedRateID1") > 0)
                this.XMonsterInfo.DroppedRate.Add((int)Global.GetSafeAttributeLong(monsterXml, "DroppedRateID1"));
            if ((int)Global.GetSafeAttributeLong(monsterXml, "DroppedRateID2") > 0)
                this.XMonsterInfo.DroppedRate.Add((int)Global.GetSafeAttributeLong(monsterXml, "DroppedRateID2"));
            if ((int)Global.GetSafeAttributeLong(monsterXml, "DroppedRateID3") > 0)
                this.XMonsterInfo.DroppedRate.Add((int)Global.GetSafeAttributeLong(monsterXml, "DroppedRateID3"));

            this.XMonsterInfo.MonsterTask = new List<MonsterTaskObject>();

            MonsterTaskObject szMonsterTaskObject = new MonsterTaskObject();
            szMonsterTaskObject.MissionID1 = (int)Global.GetSafeAttributeLong(monsterXml, "MissionID1");
            szMonsterTaskObject.MissionPropsID1 = (int)Global.GetSafeAttributeLong(monsterXml, "MissionPropsID1");
            szMonsterTaskObject.MissionRate1 = (int)Global.GetSafeAttributeLong(monsterXml, "MissionRate1");
            this.XMonsterInfo.MonsterTask.Add(szMonsterTaskObject);

            szMonsterTaskObject = new MonsterTaskObject();
            szMonsterTaskObject.MissionID2 = (int)Global.GetSafeAttributeLong(monsterXml, "MissionID2");
            szMonsterTaskObject.MissionPropsID2 = (int)Global.GetSafeAttributeLong(monsterXml, "MissionPropsID2");
            szMonsterTaskObject.MissionRate2 = (int)Global.GetSafeAttributeLong(monsterXml, "MissionRate2");
            this.XMonsterInfo.MonsterTask.Add(szMonsterTaskObject);

            szMonsterTaskObject = new MonsterTaskObject();
            szMonsterTaskObject.MissionID3 = (int)Global.GetSafeAttributeLong(monsterXml, "MissionID3");
            szMonsterTaskObject.MissionPropsID3 = (int)Global.GetSafeAttributeLong(monsterXml, "MissionPropsID3");
            szMonsterTaskObject.MissionRate3 = (int)Global.GetSafeAttributeLong(monsterXml, "MissionRate3");
            this.XMonsterInfo.MonsterTask.Add(szMonsterTaskObject);


            

            this.XMonsterInfo.PetsTable = null;
            this.XMonsterInfo.SpiritsTable = (int)Global.GetSafeAttributeLong(monsterXml, "SpiritsTable");
            this.XMonsterInfo.OrenTable = (int)Global.GetSafeAttributeLong(monsterXml, "OrenTable");
            this.XMonsterInfo.MonsterCall = null;
            this.XMonsterInfo.SurvivalTime = (int)Global.GetSafeAttributeLong(monsterXml, "SurvivalTime");
            this.XMonsterInfo.Ico = Global.GetSafeAttributeStr(monsterXml, "Ico");


            
        }
#else
             private void InitMonsterStaticInfo(XElement monsterXml, int maxLifeV, int maxMagicV, XElement xmlFrameConfig, /*XElement xmlPictureConfig, */double moveSpeed, int[] speedTickList)
        {
            SetStaticInfo4Monster(
                    Global.GetSafeAttributeStr(monsterXml, "SName"), //角色名
                    (int)Global.GetSafeAttributeLong(monsterXml, "ID"), //扩展ID
                    maxLifeV,
                    maxMagicV,
                    (int)Global.GetSafeAttributeLong(monsterXml, "Level"), //等级
                    (int)Global.GetSafeAttributeLong(monsterXml, "Experience"), //经验值
                    0, //金币
                    Global.StringArray2IntArray(Global.GetSafeAttributeStr(xmlFrameConfig, "EachActionFrameRange").Split(',')), //各个动作每个方向的帧数
                    Global.StringArray2IntArray(Global.GetSafeAttributeStr(xmlFrameConfig, "EachActionEffectiveFrame").Split(',')), //各个动作的起效帧，无则写-1
                    (int)Global.GetSafeAttributeLong(monsterXml, "AttackRange"), //物理攻击距离, 70个格子？？
                    (int)Global.GetSafeAttributeLong(monsterXml, "SeedRange"), // gameMap.MapGridWidth, //索敌距离, 0表示不主动索敌?
                    (int)Global.GetSafeAttributeLong(monsterXml, "Code"), //衣服代号
                    -1, //武器代号
                    speedTickList,
                    0, //对应的职业
                    0, //对应的角色级别
                    (int)Global.GetSafeAttributeLong(monsterXml, "MinAttackPercent"), //最小角色攻击力
                    (int)Global.GetSafeAttributeLong(monsterXml, "MaxAttackPercent"), //最大角色攻击力
                    (int)Global.GetSafeAttributeLong(monsterXml, "DefensePercent"), //角色防御力
                    (int)Global.GetSafeAttributeLong(monsterXml, "MDefensePercent"), //魔防
                    (double)Global.GetSafeAttributeDouble(monsterXml, "HitV"), //命中率
                    (double)Global.GetSafeAttributeDouble(monsterXml, "Dodge"), //闪避率
                    (double)Global.GetSafeAttributeDouble(monsterXml, "RecoverLifeV"), // 生命恢复 [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "RecoverMagicV"), // 魔法恢复 [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "DamageThornPercent"), // 伤害反弹(百分比) [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "DamageThorn"), // 伤害反弹(固定值) [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "SubAttackInjurePercent"), // 伤害吸收(百分比) [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "SubAttackInjure"), // 伤害吸收(固定值) [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "IgnoreDefensePercent"), // 无视防御概率 [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "IgnoreDefenseRate"), // 无视防御比例 [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "Lucky"), // 幸运一击概率 [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "FatalAttack"), // 卓越一击概率 [7/2/2014 LiaoWei]
                    (double)Global.GetSafeAttributeDouble(monsterXml, "DoubleAttack"), // 双倍一击概率 [7/2/2014 LiaoWei]
                    (int)Global.GetSafeAttributeLong(monsterXml, "FallID"), //物品掉落包ID
                    (int)Global.GetSafeAttributeLong(monsterXml, "MonsterType"), //怪物的类型
                    (int)Global.GetSafeAttributeLong(monsterXml, "PersonalJiFen"), //大乱斗个人积分
                    (int)Global.GetSafeAttributeLong(monsterXml, "CampJiFen"), //大乱斗阵营积分
                    (int)Global.GetSafeAttributeLong(monsterXml, "EMoJiFen"), // 恶魔广场积分 [11/14/2013 LiaoWei]
                    (int)Global.GetSafeAttributeLong(monsterXml, "XueSeJiFen"), // 血色堡垒积分 [11/14/2013 LiaoWei]
                    (int)Global.GetSafeAttributeLong(monsterXml, "Belong"), //掉落属于
                    Global.String2IntArray(Global.GetSafeAttributeStr(monsterXml, "SkillIDs")), //默认技能ID列表
                    (int)Global.GetSafeAttributeLong(monsterXml, "AttackType"), //攻击类型
                    (int)Global.GetSafeAttributeLong(monsterXml, "Camp"), //怪物阵营
                    (int)Global.GetSafeAttributeLong(monsterXml, "AIID"),   //怪物AIID
                    (int)Global.GetSafeAttributeLong(monsterXml, "ZhuanSheng"),    //怪物的转生次数
                    (int)Global.GetSafeAttributeLong(monsterXml, "LangHunJiFen")    //狼魂积分
                );
        }
#endif


        /// <summary>
        /// 初始化每个怪物的静态数据
        /// </summary>
        /// <param name="sname"></param>
        /// <param name="extensionID"></param>
        /// <param name="life"></param>
        /// <param name="mana"></param>
        /// <param name="level"></param>
        /// <param name="experience"></param>
        /// <param name="money"></param>
        /// <param name="frameRange"></param>
        /// <param name="effectiveFrame"></param>
        /// <param name="attackRange"></param>
        /// <param name="seekRange"></param>
        /// <param name="equipmentBody"></param>
        /// <param name="equipmentWeapon"></param>
        /// <param name="speedTickList"></param>
        /// <param name="toOccupation"></param>
        /// <param name="toRoleLevel"></param>
        /// <param name="minAttack"></param>
        /// <param name="maxAttack"></param>
        /// <param name="defense"></param>
        /// <param name="magicDefense"></param>
        /// <param name="hitV"></param>
        /// <param name="dodge"></param>
        /// <param name="recoverLifeV"></param>
        /// <param name="recoverMagicV"></param>
        /// <param name="fallGoodsPackID"></param>
        /// <param name="monsterType"></param>
        /// <param name="battlePersonalJiFen"></param>
        /// <param name="battleZhenYingJiFen"></param>
        /// <param name="nDaimonSquareJiFen"></param>
        /// <param name="nBloodCastJiFen"></param>
        /// <param name="fallBelongTo"></param>
        /// <param name="skillIDs"></param>
        /// <param name="attackType"></param>
        /// <param name="camp"></param>
#if ___CC___FUCK___YOU___BB___
#else
        private void SetStaticInfo4Monster(string sname, int extensionID, double life, double mana, int level, int experience, int money, int[] frameRange, 
                                            int[] effectiveFrame, int attackRange, int seekRange, int equipmentBody, int equipmentWeapon, int[] speedTickList, 
                                                int toOccupation, int toRoleLevel, int minAttack, int maxAttack, int defense, int magicDefense, double hitV, double dodge,
                                                    double recoverLifeV, double recoverMagicV, double DamageThornPercent, double DamageThorn, double SubAttackInjurePercent, double SubAttackInjure,
                                                        double IgnoreDefensePercent, double IgnoreDefenseRate, double Lucky, double FatalAttack, double DoubleAttack, int fallGoodsPackID, 
                                                            int monsterType, int battlePersonalJiFen, int battleZhenYingJiFen, int nDaimonSquareJiFen, int nBloodCastJiFen, int fallBelongTo,
                                                                int[] skillIDs, int attackType, int camp, int AIID, int nChangeLifeCount, int nWolfScore)
        {
           // this.MonsterInfo = new MonsterStaticInfo();

            this.MonsterInfo.SpriteSpeedTickList = speedTickList;

            this.MonsterInfo.VSName = sname;
            this.MonsterInfo.ExtensionID = extensionID;
            this.MonsterInfo.VLifeMax = life;
            this.MonsterInfo.VManaMax = mana;
            this.MonsterInfo.VLevel = level;
            this.MonsterInfo.VExperience = experience;
            this.MonsterInfo.VMoney = money;
            this.MonsterInfo.EachActionFrameRange = frameRange;
            this.MonsterInfo.EffectiveFrame = effectiveFrame;
            this.MonsterInfo.SeekRange = seekRange;//* tmp
            this.MonsterInfo.EquipmentBody = equipmentBody;
            this.MonsterInfo.EquipmentWeapon = equipmentWeapon;
            this.MonsterInfo.ToOccupation = toOccupation;
            this.MonsterInfo.MinAttack = minAttack;
            this.MonsterInfo.MaxAttack = maxAttack;
            this.MonsterInfo.Defense = defense;
            this.MonsterInfo.MDefense = magicDefense;
            this.MonsterInfo.HitV = hitV;
            this.MonsterInfo.Dodge = dodge;
            this.MonsterInfo.RecoverLifeV = recoverLifeV;
            this.MonsterInfo.RecoverMagicV = recoverMagicV;
            this.MonsterInfo.MonsterDamageThornPercent = DamageThornPercent;
            this.MonsterInfo.MonsterDamageThorn = DamageThorn;
            this.MonsterInfo.MonsterSubAttackInjurePercent = SubAttackInjurePercent;
            this.MonsterInfo.MonsterSubAttackInjure = SubAttackInjure;
            this.MonsterInfo.MonsterIgnoreDefensePercent = IgnoreDefensePercent;
            this.MonsterInfo.MonsterIgnoreDefenseRate = IgnoreDefenseRate;
            this.MonsterInfo.MonsterLucky = Lucky;
            this.MonsterInfo.MonsterFatalAttack = FatalAttack;
            this.MonsterInfo.MonsterDoubleAttack = DoubleAttack;
            this.MonsterInfo.FallGoodsPackID = fallGoodsPackID;
            this.MonsterInfo.BattlePersonalJiFen = Global.GMax(0, battlePersonalJiFen);
            this.MonsterInfo.BattleZhenYingJiFen = Global.GMax(0, battleZhenYingJiFen);
            this.MonsterInfo.DaimonSquareJiFen = Global.GMax(0, nDaimonSquareJiFen); // 恶魔广场积分 add by liaowei
            this.MonsterInfo.BloodCastJiFen = Global.GMax(0, nBloodCastJiFen);       // 血色堡垒积分 add by liaowei 
            this.MonsterInfo.WolfScore = Global.GMax(0, nWolfScore);       //狼魂积分
            this.MonsterInfo.FallBelongTo = Global.GMax(0, fallBelongTo);
            this.MonsterInfo.SkillIDs = skillIDs;
            this.MonsterInfo.AttackType = attackType;
            this.MonsterInfo.Camp = camp; //* tmp
            this.MonsterInfo.AIID = AIID;
            this.MonsterInfo.ChangeLifeCount = nChangeLifeCount < 0 ? 0 : nChangeLifeCount;
        }
#endif


#endregion 初始化怪物静态信息
    }


}
