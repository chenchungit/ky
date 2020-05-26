using System;
using System.Collections.Generic;
using ProtoBuf;
using System.IO;

/// <summary>
/// 玩家扮演的角色的数据定义
/// </summary>
[ProtoContract]
public class RoleVO
{
    /// <summary>
    /// 当前的角色ID
    /// </summary>
    [ProtoMember(1)]
    public int RoleID = 0;

    /// <summary>
    /// 当前的角色ID
    /// </summary>
    [ProtoMember(2)]
    public string RoleName = "";

    /// <summary>
    /// 当前角色的性别
    /// </summary>
    [ProtoMember(3)]
    public int RoleSex = 0;

    /// <summary>
    /// 角色职业
    /// </summary>
    [ProtoMember(4)]
    public int Occupation = 0;

    /// <summary>
    /// 角色级别
    /// </summary>
    [ProtoMember(5)]
    public int Level = 1;

    /// <summary>
    /// 角色所属的帮派
    /// </summary>
    [ProtoMember(6)]
    public int Faction = 0;

    /// <summary>
    /// 绑定金币  ===>绑定金币
    /// </summary>
    [ProtoMember(7)]
    public int Money1 = 0;

    /// <summary>
    /// 非绑定金币
    /// </summary>
    [ProtoMember(8)]
    public int Money2 = 0;

    /// <summary>
    /// 当前的经验
    /// </summary>
    [ProtoMember(9)]
    public long Experience = 0;

    /// <summary>
    /// 当前的PK模式
    /// </summary>
    [ProtoMember(10)]
    public int PKMode = 0;

    /// <summary>
    /// 当前的PK值
    /// </summary>
    [ProtoMember(11)]
    public int PKValue = 0;

    /// <summary>
    /// 所在的地图的编号
    /// </summary>
    [ProtoMember(12)]
    public int MapCode = 0;

    /// <summary>
    /// 当前所在的位置X坐标
    /// </summary>
    [ProtoMember(13)]
    public int PosX = 0;

    /// <summary>
    /// 当前所在的位置Y坐标
    /// </summary>
    [ProtoMember(14)]
    public int PosY = 0;

    /// <summary>
    /// 当前的方向
    /// </summary>
    [ProtoMember(15)]
    public int RoleDirection = 0;

    /// <summary>
    /// 当前的生命值
    /// </summary>
    [ProtoMember(16)]
    public int LifeV = 0;

    /// <summary>
    /// 最大的生命值
    /// </summary>
    [ProtoMember(17)]
    public int MaxLifeV = 0;

    /// <summary>
    /// 当前的魔法值
    /// </summary>
    [ProtoMember(18)]
    public int MagicV = 0;

    /// <summary>
    /// 最大的魔法值
    /// </summary>
    [ProtoMember(19)]
    public int MaxMagicV = 0;

    /// <summary>
    /// 当前的头像
    /// </summary>
    [ProtoMember(20)]
    public int RolePic = 0;

    /// <summary>
    /// 当前背包的页数(总个数 - 1)
    /// </summary>
    [ProtoMember(21)]
    public int BagNum = 0;

    /// <summary>
    /// 任务数据
    /// </summary>
    [ProtoMember(22)]
   // public List<TaskData> TaskDataList;

    /// <summary>
    /// 物品数据
    /// </summary>
    //[ProtoMember(23)]
    //public List<GoodsData> GoodsDataList;

    /// <summary>
    /// 衣服代号
    /// </summary>
   // [ProtoMember(24)]
    public int BodyCode;

    /// <summary>
    /// 武器代号
    /// </summary>
    [ProtoMember(25)]
    public int WeaponCode;

    /// <summary>
    /// 技能数据
    /// </summary>
    //[ProtoMember(26)]
    //public List<SkillData> SkillDataList;

    /// <summary>
    /// 称号
    /// </summary>
    [ProtoMember(27)]
    public string OtherName;

    /// <summary>
    /// NPC的任务状态
    /// </summary>
    //[ProtoMember(28)]
    //public List<NPCTaskState> NPCTaskStateList;

    /// <summary>
    /// 主快捷面板的映射
    /// </summary>
    [ProtoMember(29)]
    public string MainQuickBarKeys = "";

    /// <summary>
    /// 辅助快捷面板的映射
    /// </summary>
    [ProtoMember(30)]
    public string OtherQuickBarKeys = "";

    /// <summary>
    /// 登陆的次数
    /// </summary>
    [ProtoMember(31)]
    public int LoginNum = 0;

    /// <summary>
    /// 充值的钱数   ===> 非绑定钻石
    /// </summary>
    [ProtoMember(32)]
    public int UserMoney = 0;

    /// <summary>
    /// 摆摊的名称
    /// </summary>
    [ProtoMember(33)]
    public string StallName;

    /// <summary>
    /// 组队的ID
    /// </summary>
    [ProtoMember(34)]
    public int TeamID;

    /// <summary>
    /// 剩余的自动挂机时间
    /// </summary>
    [ProtoMember(35)]
    public int LeftFightSeconds = 0;

    /// <summary>
    /// 拥有的坐骑的数量
    /// </summary>
    [ProtoMember(36)]
    public int TotalHorseCount = 0;

    /// <summary>
    /// 坐骑数据(当前骑乘)
    /// </summary>
    [ProtoMember(37)]
    public int HorseDbID = -1;

    /// <summary>
    /// 拥有的宠物的数量
    /// </summary>
    [ProtoMember(38)]
    public int TotalPetCount = 0;

    /// <summary>
    /// 宠物数据(当前放出)
    /// </summary>
    [ProtoMember(39)]
    public int PetDbID = -1;

    /// <summary>
    /// 角色的内力值
    /// </summary>
    [ProtoMember(40)]
    public int InterPower = 0;

    /// <summary>
    /// 当前的组队中的队长ID
    /// </summary>        
    [ProtoMember(41)]
    public int TeamLeaderRoleID = 0;

    /// <summary>
    ///  系统绑定的银两
    /// </summary>
    [ProtoMember(42)]
    public int YinLiang = 0;

    /// <summary>
    ///  当前冲脉的重数
    /// </summary>
    [ProtoMember(43)]
    public int JingMaiBodyLevel = 0;

    /// <summary>
    ///  当前冲脉的累加穴位个数
    /// </summary>
    [ProtoMember(44)]
    public int JingMaiXueWeiNum = 0;

    /// <summary>
    /// 上一次的坐骑ID
    /// </summary>
    [ProtoMember(45)]
    public int LastHorseID = 0;

    /// <summary>
    /// 缺省的技能ID
    /// </summary>
    [ProtoMember(46)]
    public int DefaultSkillID = -1;

    /// <summary>
    /// 自动补血喝药的百分比
    /// </summary>
    [ProtoMember(47)]
    public int AutoLifeV = 0;

    /// <summary>
    /// 自动补蓝喝药的百分比
    /// </summary>
    [ProtoMember(48)]
    public int AutoMagicV = 0;

    /// <summary>
    /// Buffer的数据列表
    /// </summary>
    //[ProtoMember(49)]
    //public List<BufferData> BufferDataList = null;

    /// <summary>
    /// 跑环的数据列表
    /// </summary>
    //[ProtoMember(50)]
    //public List<DailyTaskData> MyDailyTaskDataList = null;

    /// <summary>
    /// 已经冲通的经脉的条数
    /// </summary>
    [ProtoMember(51)]
    public int JingMaiOkNum = 0;

    /// <summary>
    /// 每日冲穴的次数数据
    /// </summary>
    //[ProtoMember(52)]
    //public DailyJingMaiData MyDailyJingMaiData = null;

    /// <summary>
    /// 自动增加熟练度的被动技能ID
    /// </summary>
    [ProtoMember(53)]
    public int NumSkillID = 0;

    /// <summary>
    /// 随身仓库数据
    /// </summary>
    //[ProtoMember(54)]
    //public PortableBagData MyPortableBagData = null;

    /// <summary>
    /// 见面有礼领取步骤
    /// </summary>
    [ProtoMember(55)]
    public int NewStep = 0;

    /// <summary>
    /// 领取上一个见面有礼步骤的时间
    /// </summary>
    [ProtoMember(56)]
    public long StepTime = 0;

    /// <summary>
    /// 大奖活动ID
    /// </summary>
    [ProtoMember(57)]
    public int BigAwardID = 0;

    /// <summary>
    /// 送礼活动ID
    /// </summary>
    [ProtoMember(58)]
    public int SongLiID = 0;

    /// <summary>
    /// 副本数据
    /// </summary>
    //[ProtoMember(59)]
    //public List<FuBenData> FuBenDataList = null;

    /// <summary>
    /// 总共学习技能的级别
    /// </summary>
    [ProtoMember(60)]
    public int TotalLearnedSkillLevelCount = 0;

    /// <summary>
    /// 当前已经完成的主线任务ID
    /// </summary>
    [ProtoMember(61)]
    public int CompletedMainTaskID = 0;

    /// <summary>
    /// 当前的PK点
    /// </summary>
    [ProtoMember(62)]
    public int PKPoint = 0;

    /// <summary>
    /// 最高连斩数
    /// </summary>
    [ProtoMember(63)]
    public int LianZhan = 0;

    /// <summary>
    /// 紫名的开始时间
    /// </summary>
    [ProtoMember(64)]
    public long StartPurpleNameTicks = 0;

    /// <summary>
    /// 押镖的数据
    /// </summary>
    //[ProtoMember(65)]
    //public YaBiaoData MyYaBiaoData = null;

    /// <summary>
    /// 角斗场荣誉称号开始时间
    /// </summary>
    [ProtoMember(66)]
    public long BattleNameStart = 0;

    /// <summary>
    /// 角斗场荣誉称号
    /// </summary>
    [ProtoMember(67)]
    public int BattleNameIndex = 0;

    /// <summary>
    /// 充值TaskID
    /// </summary>
    [ProtoMember(68)]
    public int CZTaskID = 0;

    /// <summary>
    /// 英雄逐擂的层数
    /// </summary>
    [ProtoMember(69)]
    public int HeroIndex = 0;

    /// <summary>
    /// 全套品质的级别
    /// </summary>
    [ProtoMember(70)]
    public int AllQualityIndex = 0;

    /// <summary>
    /// 全套锻造级别
    /// </summary>
    [ProtoMember(71)]
    public int AllForgeLevelIndex = 0;

    /// <summary>
    /// 全套宝石级别
    /// </summary>
    [ProtoMember(72)]
    public int AllJewelLevelIndex = 0;

    /// <summary>
    /// 银两折半优惠
    /// </summary>
    [ProtoMember(73)]
    public int HalfYinLiangPeriod = 0;

    /// <summary>
    /// 区ID
    /// </summary>
    [ProtoMember(74)]
    public int ZoneID = 0;

    /// <summary>
    /// 战盟名称
    /// </summary>
    [ProtoMember(75)]
    public string BHName = "";

    /// <summary>
    /// 被邀请加入战盟时是否验证
    /// </summary>
    [ProtoMember(76)]
    public int BHVerify = 0;

    /// <summary>
    /// 战盟职务
    /// </summary>
    [ProtoMember(77)]
    public int BHZhiWu = 0;

    /// <summary>
    /// 战盟帮贡
    /// </summary>
    [ProtoMember(78)]
    public int BangGong = 0;

    /// <summary>
    /// 内存领地战盟分布字典
    /// </summary>
    //[ProtoMember(79)]
    //public Dictionary<int, BangHuiLingDiItemData> BangHuiLingDiItemsDict = null;

    /// <summary>
    /// 当前服的皇帝的ID
    /// </summary>
    [ProtoMember(80)]
    public int HuangDiRoleID = 0;

    /// <summary>
    /// 是否皇后
    /// </summary>
    [ProtoMember(81)]
    public int HuangHou = 0;

    /// <summary>
    /// 自己在排行中的位置字典
    /// </summary>
    [ProtoMember(82)]
    public Dictionary<int, int> PaiHangPosDict = null;

    /// <summary>
    /// 是否进入了挂机保护状态
    /// </summary>
    [ProtoMember(83)]
    public int AutoFightingProtect = 0;

    /// <summary>
    /// 法师的护盾开始的时间
    /// </summary>
    [ProtoMember(84)]
    public long FSHuDunStart = 0;

    /// <summary>
    /// 大乱斗中的阵营ID
    /// </summary>
    [ProtoMember(85)]
    public int BattleWhichSide = -1;

    /// <summary>
    /// 上次的mailID
    /// </summary>
    [ProtoMember(86)]
    public int LastMailID = 0;

    /// <summary>
    /// 上次的mailID
    /// </summary>
    [ProtoMember(87)]
    public int IsVIP = 0;

    /// <summary>
    /// 单次奖励记录标志位
    /// </summary>
    [ProtoMember(88)]
    public long OnceAwardFlag = 0;

    /// <summary>
    ///  系统绑定的金币  ===> 绑定钻石
    /// </summary>
    [ProtoMember(89)]
    public int Gold = 0;

    /// <summary>
    /// 道术隐身的时间
    /// </summary>
    [ProtoMember(90)]
    public long DSHideStart = 0;

    /// <summary>
    /// 角色常用整形参数值列表
    /// </summary>
    [ProtoMember(91)]
    public List<int> RoleCommonUseIntPamams = new List<int>();

    /// <summary>
    /// 法师的护盾持续的秒数
    /// </summary>
    [ProtoMember(92)]
    public int FSHuDunSeconds = 0;

    /// <summary>
    /// 中毒开始的时间
    /// </summary>
    [ProtoMember(93)]
    public long ZhongDuStart = 0;

    /// <summary>
    /// 中毒持续的秒数
    /// </summary>
    [ProtoMember(94)]
    public int ZhongDuSeconds = 0;

    /// <summary>
    /// 开服日期
    /// </summary>
    [ProtoMember(95)]
    public string KaiFuStartDay = "";

    /// <summary>
    /// 注册日期
    /// </summary>
    [ProtoMember(96)]
    public string RegTime = "";

    /// <summary>
    /// 节日活动开始日期
    /// </summary>
    [ProtoMember(97)]
    public string JieriStartDay = "";

    /// <summary>
    /// 节日活动持续天数
    /// </summary>
    [ProtoMember(98)]
    public int JieriDaysNum = 0;

    /// <summary>
    /// 合区活动开始时间
    /// </summary>
    [ProtoMember(99)]
    public string HefuStartDay = "";

    /// <summary>
    /// 节日称号
    /// </summary>
    [ProtoMember(100)]
    public int JieriChengHao = 0;

    /// <summary>
    /// 补偿开始时间
    /// </summary>
    [ProtoMember(101)]
    public string BuChangStartDay = "";

    /// <summary>
    /// 冻结开始的时间
    /// </summary>
    [ProtoMember(102)]
    public long DongJieStart = 0;

    /// <summary>
    /// 冻结持续的秒数
    /// </summary>
    [ProtoMember(103)]
    public int DongJieSeconds = 0;


    /// <summary>
    /// 月度抽奖活动开始日期
    /// </summary>
    [ProtoMember(104)]
    public string YueduDazhunpanStartDay = "";

    /// <summary>
    /// 月度抽奖活动持续天数
    /// </summary>
    [ProtoMember(105)]
    public int YueduDazhunpanStartDayNum = 0;


    // 属性改造 增加一级属性 [8/15/2013 LiaoWei]
    /// <summary>
    /// 力量
    /// </summary>
    [ProtoMember(106)]
    public int RoleStrength = 0;

    /// <summary>
    /// 智力
    /// </summary>
    [ProtoMember(107)]
    public int RoleIntelligence = 0;

    /// <summary>
    /// 敏捷
    /// </summary>
    [ProtoMember(108)]
    public int RoleDexterity = 0;

    /// <summary>
    /// 体力
    /// </summary>
    [ProtoMember(109)]
    public int RoleConstitution = 0;

    // 转生计数 [10/17/2013 LiaoWei]
    [ProtoMember(110)]
    public int ChangeLifeCount = 0;

    // 总属性点 [10/17/2013 LiaoWei]
    [ProtoMember(111)]
    public int TotalPropPoint = 0;

    // 新人标记 [10/17/2013 LiaoWei]
    [ProtoMember(112)]
    public int IsFlashPlayer = 0;

    // 被崇拜计数[12/10/2013 LiaoWei]
    [ProtoMember(113)]
    public int AdmiredCount = 0;

    // 战斗力 [12/17/2013 LiaoWei]
    [ProtoMember(114)]
    public int CombatForce = 0;

    // 崇拜计数[12/10/2013 LiaoWei]
    [ProtoMember(115)]
    public int AdorationCount = 0;

    // 每日在线时长 [1/18/2014 LiaoWei]
    [ProtoMember(116)]
    public int DayOnlineSecond = 0;

    // 连续登陆天数(1-7) [1/18/2014 LiaoWei]
    [ProtoMember(117)]
    public int SeriesLoginNum = 0;

    // 自动分配属性点 [3/3/2014 LiaoWei] 
    [ProtoMember(118)]
    public int AutoAssignPropertyPoint = 0;

    // 总在线时间 [3/3/2014 LiaoWei] 
    [ProtoMember(119)]
    public int OnLineTotalTime = 0;

    /// <summary>
    /// 全套卓越属性装备个数
    /// </summary>
    [ProtoMember(120)]
    public int AllZhuoYueNum = 0;

    /// <summary>
    /// VIP等级 [3/27/2014 LiaoWei]
    /// </summary>
    [ProtoMember(121)]
    public int VIPLevel = 0;

    /// <summary>
    /// 开启背包个格子计时 [4/4/2014 LiaoWei]
    /// </summary>
    [ProtoMember(122)]
    public int OpenGridTime = 0;

    /// <summary>
    /// 开启移动背包格子计时 [4/4/2014 LiaoWei]
    /// </summary>
    [ProtoMember(123)]
    public int OpenPortableGridTime = 0;

    /// <summary>
    /// 翅膀数据列表
    /// </summary>
    //[ProtoMember(124)]
    //public WingData MyWingData = null;

    /// <summary>
    /// 图鉴提交信息 [5/17/2014 LiaoWei]
    /// </summary>
    [ProtoMember(125)]
    public Dictionary<int, int> PictureJudgeReferInfo = null;

    /// <summary>
    /// 星魂值 [8/4/2014 LiaoWei]
    /// </summary>
    [ProtoMember(126)]
    public int StarSoulValue = 0;
    /// <summary>
    /// 仓库金币
    /// </summary>
    [ProtoMember(127)]
    public long StoreYinLiang = 0;

    /// <summary>
    /// 仓库绑定金币
    /// </summary>
    [ProtoMember(128)]
    public long StoreMoney = 0;

    /// <summary>
    /// 节日活动开始日期
    /// </summary>
    [ProtoMember(129)]
    public string PlayerRecallStartDay = "";

    /// <summary>
    /// 节日活动持续天数
    /// </summary>
    [ProtoMember(130)]
    public string PlayerRecallDaysNum = "";

    /// <summary>
    /// 天赋数据
    /// </summary>
    //[ProtoMember(131)]
    //public TalentData MyTalentData;

    /// <summary>
    /// 天梯荣耀值
    /// </summary>
    [ProtoMember(132)]
    public int TianTiRongYao;

    /// <summary>
    /// 荧光宝石数据
    /// </summary>
    //[ProtoMember(133)]
    //public FluorescentGemData FluorescentDiamondData;

    /// <summary>
    /// 是否gm
    /// </summary>
    [ProtoMember(134)]
    public int GMAuth = 0;

    /// <summary>
    /// 魂石石数据
    /// </summary>
    //[ProtoMember(135)]
    //public SoulStoneData soulStoneData;

    /// <summary>
    /// 二态功能设置，参考ESettingBitFlag
    /// </summary>
    [ProtoMember(136)]
    public long SettingBitFlags;

    /// <summary>
    /// 配偶id
    /// </summary>
    [ProtoMember(137)]
    public int SpouseId;

    //public object Clone()
    //{
    //    //BinaryFormatter Formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
    //    //MemoryStream stream = new MemoryStream();
    //    //Formatter.Serialize(stream, this);
    //    //stream.Position = 0;
    //    //object clonedObj = Formatter.Deserialize(stream);
    //    //stream.Close();
    //    //return clonedObj;

    //    RoleData rd = null;
    //    using (MemoryStream ms = new MemoryStream())
    //    {
    //        Serializer.Serialize(ms, this);
    //        ms.Position = 0;
    //        rd = Serializer.Deserialize<RoleData>(ms);
    //    }
    //    return rd;
    //}
}
