using GameServer.Core.Executor;
using GameServer.Logic;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Linq;

namespace GameServer.cc.monster
{

    /// <summary>
    /// 爆怪物的时间点
    /// </summary>
    public class CBirthTimePoint
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
    public class CBirthTimeForDayOfWeek
    {
        /// <summary>
        /// 爆怪的时间
        /// </summary>
        public CBirthTimePoint BirthTime;

        /// <summary>
        /// 每周哪天爆怪
        /// </summary>
        public int BirthDayOfWeek = 0;
    };
    /// <summary>
    /// 怪物的静态数据
    /// </summary>
    public class CMonsterStaticInfo
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

    }
    /// <summary>
    /// 区域爆怪管理类
    /// </summary>
    public class CMonsterZone
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

        #region 初始化怪物数据
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
        private void LoadMonster(Monster monster, CMonsterZone monsterZone, CMonsterStaticInfo monsterInfo, int monsterType, 
            int roleID, string name, double life, double mana, Point coordinate, double direction, double moveSpeed, int attackRange)
        {

            monster.Name = name;
            monster.MonsterZoneNode = monsterZone;
            monster.MonsterInfo = monsterInfo;
           
            monster.RoleID = roleID;
            monster.VLife = life * 10000;//HX_SERVER FOR TEST
            monster.VMana = mana;
            monster.AttackRange = attackRange;
            monster.Coordinate = coordinate;
            monster.Direction = direction;
            monster.MoveSpeed = moveSpeed;
            monster.MonsterType = monsterType;
           
            monster.CoordinateChanged += UpdateMonsterEvent;

            SysConOut.WriteLine(string.Format("初始化怪物 {0}----{1}",monster.RoleID, monster.VLife));
            //人为导致搜索的时间间隔错开
            monster.NextSeekEnemyTicks = Global.MonsterSearchTimer + Global.GetRandomNumber(0, Global.MonsterSearchRandomTimer);
        }

        private Monster InitMonster(XElement monsterXml, double maxLifeV, double maxMagicV, XElement xmlFrameConfig, 
            /*XElement xmlPictureConfig, */double moveSpeed, /*int[] speedTickList, */bool attachEvent = true)
        {
            GameMap gameMap = GameManager.MapMgr.DictMaps[MapCode];

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
        /// <summary>
        /// 加载静态的怪物信息
        /// </summary>
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
        /// <summary>
        /// 初始化怪
        /// </summary>
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
         
            fileName = string.Format("GuaiWu/{0}.xml",
                Global.GetSafeAttributeStr(monsterXml, "ResName"));

            string defaultFileName = string.Format("GuaiWu/ceshi_guaiwu.unity3d.xml");
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
            XElement xmlSpeedConfig = Global.GetSafeXElement(xml, "SpeedConfig");
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
                    GameManager.MonsterMgr.AddNewMonster(monster);
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

               // SeedMonster = monster;
            }
        }

        /// <summary>
        /// 由主角坐标变化触发游戏画面中对象位置刷新
        /// </summary>
        /// <param name="sprite"></param>
        private void UpdateMonsterEvent(Monster monster)
        {
           // SysConOut.WriteLine("由主角坐标变化触发游戏画面中对象位置刷新");
            //如果是原始坐标，则不通知九宫格，导致的其他问题比较少，移动中如果遇到此坐标，也会被连续的移动补救。
            ////if (!((int)monster.FirstCoordinate.X == (int)monster.Coordinate.X && (int)monster.FirstCoordinate.Y == (int)monster.Coordinate.Y))
            //if (!monster.FirstStoryMove)
            //{
            //    //将精灵放入格子
            GameManager.MapGridMgr.DictGrids[MapCode].MoveObject((int)monster.SafeOldCoordinate.X, (int)monster.SafeOldCoordinate.Y,
                (int)monster.SafeCoordinate.X, (int)monster.SafeCoordinate.Y, monster);
            //}
        }

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
        #endregion



        #region 怪物列表

        /// <summary>
        /// 当前区域的怪列表
        /// </summary>
        private List<Monster> MonsterList = new List<Monster>(100);

        #endregion 怪物列表

        #region 静态数据
        /// <summary>
        /// 静态引用的怪物数据
        /// </summary>
        private CMonsterStaticInfo MonsterInfo = new CMonsterStaticInfo();

        #endregion

        #region 区域怪物复活
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
               
            }
            else if ((int)MonsterBirthTypes.CreateDayOfWeek == BirthType) // 每周几刷怪 [1/10/2014 LiaoWei]
            {
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
                   // Global.SystemKillMonster(MonsterList[n]);
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
                        if ((int)MonsterTypes.Boss == MonsterList[i].MonsterType)
                        {
                            if ((int)MonsterBirthTypes.TimeSpan == BirthType && Timeslot >= (30 * 60)) //大于30分钟的才报告
                            {
                                //通知客户端boss刷新了
                                GameManager.ClientMgr.NotifyAllImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    null, StringUtil.substitute(Global.GetLang("[{0}]出现在[{1}],请各位勇士速速前往击杀。"), MonsterList[i].MonsterInfo.VSName, Global.GetMapName(MonsterList[i].CurrentMapCode)), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
                            }
                        }

                        if (Global.IsGongGaoReliveMonster(MonsterList[i].MonsterInfo.ExtensionID))
                        {
                            GameManager.ClientMgr.NotifyAllImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    null, StringUtil.substitute(Global.GetLang("【{0}】出现了【{1}】的身影，战胜它就有机会获得珍贵宝藏哦！"), Global.GetMapName(MonsterList[i].CurrentMapCode), MonsterList[i].MonsterInfo.VSName), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
                        }

                        // 刷黄金部队或世界BOSS
                        //if (BirthType == (int)MonsterBirthTypes.TimePoint || BirthType == (int)MonsterBirthTypes.CreateDayOfWeek)
                        //{
                        //    /// 处理BOSS复活时图标状态刷新
                        //    TimerBossManager.getInstance().AddBoss(BirthType, MonsterList[i].MonsterInfo.ExtensionID);
                        //}
                    }
                }
            }
        }
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
        /// 判断是否动态刷怪区域 动态刷怪区域刷出的怪，都是属于玩家的怪，玩家可以带走并穿越地图 每个地图都有一个动态
        /// 刷怪区域，便于玩家带怪穿越地图
        /// </summary>
        /// <returns></returns>
        public Boolean IsDynamicZone()
        {
            return (int)MonsterBirthTypes.CrossMap == BirthType;
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
        /// 销毁怪物 这个函数是真正销毁怪物的地方，怪物一旦被销毁，就意味着所有的引用都会消除，
        /// 相应的内存空间也会被释放
        /// </summary>
        /// <param name="monster"></param>
        private void DestroyMonster(Monster monster)
        {
            

            monster.CoordinateChanged -= UpdateMonsterEvent;//这儿是 + 还是 -
            monster.MoveToComplete -= MoveToComplete;

            //将精灵从地图格子中删除
            GameManager.MapGridMgr.DictGrids[MapCode].RemoveObject(monster);

            //从当前区域队列中删除
            bool ret = MonsterList.Remove(monster);

            //将一个怪物从管理队列中删除(副本动态刷怪会用到)
            GameManager.MonsterMgr.NewRemoveMonster(monster);

            //将怪物ID 还回管理器，以便重用
            GameManager.MonsterIDMgr.PushBack(monster.RoleID);

            //减少计数
            if (ret)
            {
                Monster.DecMonsterCount(); //防止某些情况下重复的调用，导致负数
            }
        }
        #endregion



        #region 初始化怪物静态信息

        /// <summary>
        /// 初始化怪物静态数据
        /// </summary>
        /// <param name="monsterXml"></param>
        /// <returns></returns>
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
        private void SetStaticInfo4Monster(string sname, int extensionID, double life, double mana, int level, int experience, int money, int[] frameRange,
                                            int[] effectiveFrame, int attackRange, int seekRange, int equipmentBody, int equipmentWeapon, int[] speedTickList,
                                                int toOccupation, int toRoleLevel, int minAttack, int maxAttack, int defense, int magicDefense, double hitV, double dodge,
                                                    double recoverLifeV, double recoverMagicV, double DamageThornPercent, double DamageThorn, double SubAttackInjurePercent, double SubAttackInjure,
                                                        double IgnoreDefensePercent, double IgnoreDefenseRate, double Lucky, double FatalAttack, double DoubleAttack, int fallGoodsPackID,
                                                            int monsterType, int battlePersonalJiFen, int battleZhenYingJiFen, int nDaimonSquareJiFen, int nBloodCastJiFen, int fallBelongTo,
                                                                int[] skillIDs, int attackType, int camp, int AIID, int nChangeLifeCount, int nWolfScore)
        {
            // this.MonsterInfo = new MonsterStaticInfo();

           // this.MonsterInfo.SpriteSpeedTickList = speedTickList;

            this.MonsterInfo.VSName = sname;
            this.MonsterInfo.ExtensionID = extensionID;
            this.MonsterInfo.VLifeMax = life;
            this.MonsterInfo.VManaMax = mana;
            this.MonsterInfo.VLevel = level;
            this.MonsterInfo.VExperience = experience;
            this.MonsterInfo.VMoney = money;
           // this.MonsterInfo.EachActionFrameRange = frameRange;
           // this.MonsterInfo.EffectiveFrame = effectiveFrame;
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
           // this.MonsterInfo.MonsterLucky = Lucky;
           // this.MonsterInfo.MonsterFatalAttack = FatalAttack;
           // this.MonsterInfo.MonsterDoubleAttack = DoubleAttack;
           // this.MonsterInfo.FallGoodsPackID = fallGoodsPackID;
           // this.MonsterInfo.BattlePersonalJiFen = Global.GMax(0, battlePersonalJiFen);
          //  this.MonsterInfo.BattleZhenYingJiFen = Global.GMax(0, battleZhenYingJiFen);
           // this.MonsterInfo.DaimonSquareJiFen = Global.GMax(0, nDaimonSquareJiFen); // 恶魔广场积分 add by liaowei
          //  this.MonsterInfo.BloodCastJiFen = Global.GMax(0, nBloodCastJiFen);       // 血色堡垒积分 add by liaowei 
           // this.MonsterInfo.WolfScore = Global.GMax(0, nWolfScore);       //狼魂积分
           // this.MonsterInfo.FallBelongTo = Global.GMax(0, fallBelongTo);
            this.MonsterInfo.SkillIDs = skillIDs;
            this.MonsterInfo.AttackType = attackType;
            this.MonsterInfo.Camp = camp; //* tmp
            this.MonsterInfo.AIID = AIID;
          //  this.MonsterInfo.ChangeLifeCount = nChangeLifeCount < 0 ? 0 : nChangeLifeCount;
        }

        #endregion 初始化怪物静态信息
    }
}
