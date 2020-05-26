#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Threading;
using System.Xml.Linq;
using GameServer.Interface;
using Server.Data;
using GameServer.Logic;
using Server.Tools;
using Server.Protocol;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic.BossAI;
using GameServer.Logic.ExtensionProps;
using GameServer.Logic.NewBufferExt;
using System.Threading;
using GameServer.Core.Executor;
using Tmsk.Contract;

namespace GameServer.Logic
{
    public delegate void MoveToEventHandler(Monster monster);

    public delegate void CoordinateEventHandler(Monster monster);

    //定义精灵内部改动作通知事件
    public delegate void SpriteChangeActionEventHandler(object sender, SpriteChangeActionEventArgs e);

    /// <summary>
    /// 怪物精灵类
    /// </summary>
    public class Monster : IObject
    {
        public Monster()
        {
            //Heart = new DispatcherTimer();
            //Heart.Interval = TimeSpan.FromMilliseconds(400);
            //Heart.Tick += new EventHandler(Timer_Tick);
        }

        #region 自拷贝函数

        /// <summary>
        /// 克隆自己，克隆基础数据
        /// </summary>
        /// <returns></returns>
        public Monster Clone()
        {
            Monster monster = new Monster();
            //monster.SpriteSpeedTickList = this.SpriteSpeedTickList;

            monster.Name = this.Name;
            monster.MonsterZoneNode = this.MonsterZoneNode;
#if ___CC___FUCK___YOU___BB___
            monster.XMonsterInfo = this.XMonsterInfo;
            if (null == monster.XMonsterInfo)
            {
                monster.XMonsterInfo = this.MonsterZoneNode.GetMonsterInfo();
                monster.Camp = 0;
            }
#else
             monster.MonsterInfo = this.MonsterInfo;
            if (null == monster.MonsterInfo)
            {
                monster.MonsterInfo = this.MonsterZoneNode.GetMonsterInfo();
                monster.Camp = monster.MonsterInfo.Camp;
            }
#endif

            monster.RoleID = this.RoleID;
            //monster.RoleSex = this.RoleSex;
            //monster.VSName = this.VSName;
            //monster.MonsterInfo.ExtensionID = this.MonsterInfo.ExtensionID;
            monster.VLife = this.VLife;
            monster.VMana = this.VMana;

            //monster.MonsterInfo.VLifeMax = this.MonsterInfo.VLifeMax;
            //monster.VManaMax = this.VManaMax;

            //monster.MonsterInfo.VLevel = this.MonsterInfo.VLevel;
            //monster.MonsterZoneNode.VExperience = this.MonsterZoneNode.VExperience;
            //monster.MonsterZoneNode.VMoney = this.MonsterZoneNode.VMoney;

            //monster.Buff = this.Buff;
            //monster.FaceSign = this.FaceSign;

            //monster.EachActionFrameRange = this.EachActionFrameRange;
            //monster.EffectiveFrame = this.EffectiveFrame;

            monster.AttackRange = this.AttackRange;

            //monster.SeekRange = this.SeekRange;
            //monster.EquipmentBody = this.EquipmentBody;
            //monster.EquipmentWeapon = this.EquipmentWeapon;

            //monster.CenterX = this.CenterX;
            //monster.CenterY = this.CenterY;
            //monster.FirstCoordinate = oldMonster.FirstCoordinate;
            //monster.Coordinate = oldMonster.Coordinate;
            monster.Direction = this.Direction;
            //monster.HoldWidth = this.HoldWidth;
            //monster.HoldHeight = this.HoldHeight;
            monster.MoveSpeed = this.MoveSpeed;
            //monster.ToOccupation = this.ToOccupation;
            //monster.ToRoleLevel = this.ToRoleLevel;
            //monster.MinAttack = this.MinAttack;
            //monster.MaxAttack = this.MaxAttack;
            //monster.Defense = this.Defense;
            //monster.MDefense = this.MDefense;
            //monster.HitV = this.HitV;
            //monster.Dodge = this.Dodge;
            //monster.RecoverLifeV = this.RecoverLifeV;
            //monster.RecoverMagicV = this.RecoverMagicV;
            //monster.FallGoodsPackID = this.FallGoodsPackID;
            monster.MonsterType = this.MonsterType;
            //monster.BattlePersonalJiFen = this.BattlePersonalJiFen;
            //monster.BattleZhenYingJiFen = this.BattleZhenYingJiFen;
            //monster.FallBelongTo = this.FallBelongTo;
            //monster.SkillIDs = this.SkillIDs;
            //monster.AttackType = this.AttackType;
            monster.Camp = this.Camp;
            //monster.PetAiControlType = this.PetAiControlType;
            //monster.ZhenQiMinValue = this.ZhenQiMinValue;
            //monster.ZhenQiMaxValue = this.ZhenQiMaxValue;

            //monster.CoordinateChanged = this.CoordinateChanged;

            //添加移动结束事件
            //monster.MoveToComplete = this.MoveToComplete;

            //初始化移动的目标点
            //monster.MoveToPos = this.MoveToPos;

            monster.NextSeekEnemyTicks = this.NextSeekEnemyTicks;
            monster.OwnerClient = this.OwnerClient;

            Monster.IncMonsterCount();

            return monster;
        }

#endregion

#region 怪物操作

        /// <summary>
        /// 获取怪物数据
        /// </summary>
        /// <returns></returns>
        public MonsterData GetMonsterData()
        {
            MonsterData _MonsterData = new MonsterData();

            _MonsterData.RoleID = this.RoleID;

            //如果怪物有主人，则需要加上主人的名字后缀
            if (null != OwnerClient)
            {
#if ___CC___FUCK___YOU___BB___
                //中文点字符在越南文或者英文里面没有，需要翻译一下
                _MonsterData.RoleName = String.Format(Global.GetLang("{0}•{1}"), this.XMonsterInfo.Name, OwnerClient.ClientData.RoleName);
#else
                //中文点字符在越南文或者英文里面没有，需要翻译一下
                _MonsterData.RoleName = String.Format(Global.GetLang("{0}•{1}"), this.MonsterInfo.VSName, OwnerClient.ClientData.RoleName);
#endif
            }
            else if (!string.IsNullOrEmpty(MonsterName))
            {
                _MonsterData.RoleName = MonsterName;
            }
            else
            {
#if ___CC___FUCK___YOU___BB___
                _MonsterData.RoleName = this.XMonsterInfo.Name;
#else
                 _MonsterData.RoleName = this.MonsterInfo.VSName;
#endif
            }

            //_MonsterData.RoleSex = this.RoleSex;
#if ___CC___FUCK___YOU___BB___
            _MonsterData.ExtensionID = this.XMonsterInfo.MonsterId;
            _MonsterData.Level = this.XMonsterInfo.Level;
            _MonsterData.Experience = this.XMonsterInfo.Exp;
            _MonsterData.MaxLifeV = this.XMonsterInfo.MaxHP;
            _MonsterData.MaxMagicV = 1;
            _MonsterData.EquipmentBody = 1;
#else
            _MonsterData.ExtensionID = this.MonsterInfo.ExtensionID;
            _MonsterData.Level = this.MonsterInfo.VLevel;
            _MonsterData.Experience = this.MonsterInfo.VExperience;
            _MonsterData.MaxLifeV = this.MonsterInfo.VLifeMax;
            _MonsterData.MaxMagicV = this.MonsterInfo.VManaMax;
            _MonsterData.EquipmentBody = this.MonsterInfo.EquipmentBody;
#endif
            _MonsterData.MonsterType = this.MonsterType;
            _MonsterData.BattleWitchSide = this.Camp;

            //怪物主人的角色ID
            if (null != OwnerClient)
            {
                _MonsterData.MasterRoleID = OwnerClient.ClientData.RoleID;
            }

            _MonsterData.PosX = (int)this.SafeCoordinate.X;
            _MonsterData.PosY = (int)this.SafeCoordinate.Y;
            _MonsterData.RoleDirection = (int)this.SafeDirection;
            _MonsterData.LifeV = this.VLife;
            _MonsterData.MagicV = this.VMana;
            _MonsterData.AiControlType = (ushort)this.PetAiControlType;
#if ___CC___FUCK___YOU___BB___
            _MonsterData.MonsterLevel = this.XMonsterInfo.Level;
#else
             _MonsterData.MonsterLevel = this.MonsterInfo.VLevel;
#endif

            BufferData bufferData = Global.GetMonsterBufferDataByID(this, (int)BufferItemTypes.DSTimeShiDuNoShow);
            if (null != bufferData)
            {
                _MonsterData.ZhongDuStart = bufferData.StartTime;
                _MonsterData.ZhongDuSeconds = bufferData.BufferSecs;
            }
            else
            {
                _MonsterData.ZhongDuStart = 0;
                _MonsterData.ZhongDuSeconds = 0;
            }

            // MU增加昏迷 [5/7/2014 LiaoWei]
            if (IsMonsterDongJie())
            {
                _MonsterData.FaintStart = DongJieStart;
                _MonsterData.FaintSeconds = DongJieSeconds;
            }
            
            return _MonsterData;
        }

        /// <summary>
        /// 怪物复活(重用)
        /// </summary>
        public Point Realive()
        {
            this.UniqueID = Global.GetUniqueID();
            //强迫重新通知客户端
            //this.CurrentObjsDict = null;
            //this.CurrentGridsDict = null;
            //this._CurrentGridX = -1;
            //this._CurrentGridY = -1;

            this._LastDeadTicks = 0;
            this.HandledDead = false;
#if ___CC___FUCK___YOU___BB___
            this.VLife = (int)this.XMonsterInfo.MaxHP;
            this.VMana = 1;
#else
           this.VLife = (int)this.MonsterInfo.VLifeMax;
            this.VMana = (int)this.MonsterInfo.VManaMax;
#endif

            this.Action = GActions.Stand;
            this.DongJieStart = 0;
            this.DongJieSeconds = 0;
            this.TempPropsBuffer.Init();

            this.WhoKillMeID = 0;
            this.WhoKillMeName = "";
            this.IsCollected = false;

            isDeath = false;
            deathDelay = 0;

            lock (_AttackerLogDict)
            {
              //  _AttackerDict.Clear();
              //  _AttackerTicksDict.Clear();
                _AttackerLogDict.Clear();
            }

            this.Start();

            Point toPoint;
           
            toPoint = Global.GetMapPointByGridXY(ObjectTypes.OT_MONSTER, MonsterZoneNode.MapCode, MonsterZoneNode.ToX, MonsterZoneNode.ToY, MonsterZoneNode.Radius, 0, true); //人的位置X/Y坐标
           

            this.Coordinate = toPoint;
            this.Direction = (double)Global.GetRandomNumber(0, 8); //朝向;

            GameManager.SystemServerEvents.AddEvent(string.Format("怪物复活, roleID={0}", RoleID), EventLevels.Debug);

            return toPoint;
        }

        /// <summary>
        /// 主循环线程调用
        /// </summary>
        public void OnDead()
        {
            MyMagicsManyTimeDmageQueue.Clear();

            _CurrentMagic = -1;
            _MagicFinish = 0;

            //清空动态技能
            ClearDynSkill();

            ///删除，防止遗漏，占用内存资源
            Global.RemoveMonsterBufferData(this, (int)BufferItemTypes.DSTimeShiDuNoShow);

            //MoveToPos = new Point(-1, -1); //防止重入
            DestPoint = new Point(-1, -1);
            Global.RemoveStoryboard(Name);

            //将精灵从格子中删除
            GameManager.MapGridMgr.DictGrids[MonsterZoneNode.MapCode].RemoveObject(this);

            Action = GActions.Death;
            _LastDeadTicks = TimeUtil.NOW() * 10000;

            //清空bossAI的记录
            ClearBossAI();

            //精灵生命--->怪物一次生命周期结束
            Alive = false;
            OnReallyDied();

            //释放缓存
            //ReleaseBytesDataFromCaching(true);
        }

#endregion //怪物操作i

#region 管理状态

        /// <summary>
        /// 所属的区域管理对象
        /// </summary>
        public MonsterZone MonsterZoneNode
        {
            get;
            set;
        }

        /// <summary>
        /// 静态数据对象
        /// </summary>
#if ___CC___FUCK___YOU___BB___
        public XMonsterStaticInfo XMonsterInfo
        {
            get;
            set;
        }
#else
         public MonsterStaticInfo MonsterInfo
        {
            get;
            set;
        }
#endif


        /// <summary>
        /// 是否仍然活着
        /// </summary>
        public bool Alive = false;

#endregion //管理状态

#region 实现IObject接口方法

        /// <summary>
        /// 对象的类型
        /// </summary>
        public ObjectTypes ObjectType
        {
            get { return ObjectTypes.OT_MONSTER; }
        }

        /// <summary>
        /// 获取ID
        /// </summary>
        /// <returns></returns>
        public int GetObjectID()
        {
            return RoleID;
        }

        /// <summary>
        /// 最后一次补血补魔的时间
        /// </summary>
        public long LastLifeMagicTick
        {
            get;
            set;
        }

        /// <summary>
        /// 当前所在的格子的X坐标
        /// </summary>
        public Point CurrentGrid
        { 
            get
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.MonsterZoneNode.MapCode];
                return new Point((int)(this.Coordinate.X / gameMap.MapGridWidth), (int)(this.Coordinate.Y / gameMap.MapGridHeight));
            }

            set
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.MonsterZoneNode.MapCode];
                this.Coordinate = new Point(value.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, value.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
            }
        }

        /// <summary>
        /// 当前所在的像素坐标
        /// </summary>
        public Point CurrentPos
        {
            get
            {
                return this.Coordinate;
            }

            set
            {
                this.Coordinate = value;
            }
        }

        /// <summary>
        /// 当前所在的地图的编号
        /// </summary>
        public int CurrentMapCode
        {
            get
            {
                return this.MonsterZoneNode.MapCode;
            }
        }

        /// <summary>
        /// 当前所在的副本地图的ID
        /// </summary>
        public int CurrentCopyMapID
        {
            get
            {
                return this.CopyMapID;
            }
            set
            {
                CopyMapID = value;
            }
        }

        /// <summary>
        /// 当前的方向
        /// </summary>
        public Dircetions CurrentDir
        {
            get
            {
                return (Dircetions)this.Direction;
            }

            set
            {
                this.Direction = (int)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool AllwaySearchEnemy = true;//HX_SERVER FOR TEST,DEFAULT VALUE: FALSE

#region 扩展接口

        public T GetExtComponent<T>(ExtComponentTypes type) where T : class
        {
            //GetType().GetField("").GetValue(this);
            switch (type)
            {
                case ExtComponentTypes.ManyTimeDamageQueue:
                    return MyMagicsManyTimeDmageQueue as T;
                default:
                    return default(T);
            }
        }

#endregion 扩展接口

#endregion //实现IObject接口方法

#region 其他扩展方法和属性

        /// <summary>
        /// 当前所在的地图九宫中可见对象
        /// </summary>
        private List<VisibleItem> _VisibleItemList = null;

        /// <summary>
        /// 当前所在的地图九宫中可见对象
        /// </summary>
        public List<VisibleItem> VisibleItemList
        {
            get { lock (this) return _VisibleItemList; }
            set { lock (this) _VisibleItemList = value; }
        }

        /// <summary>
        /// 上次记录别人攻击自己的时间
        /// </summary>
        private long _LastLogAttackerTicks = 0;

        /// <summary>
        /// 检查判断攻击者列表是否有效，如果大于30秒没人攻击，则无效
        /// 无效则清空列表
        /// </summary>
        /// <returns></returns>
        private Boolean CheckAttackerListEfficiency()
        {
            //30秒 无人攻击 后 无效
            if (TimeUtil.NOW() - _LastLogAttackerTicks > 30000)
            {
                lock (_AttackerLogDict)
                {
                    //_AttackerDict.Clear();
                   // _AttackerTicksDict.Clear();
                    _AttackerLogDict.Clear();
                }

                return false;
            }

            return true;
        }

        //private Dictionary<int, int> _AttackerDict = new Dictionary<int, int>();
        // private Dictionary<int, long> _AttackerTicksDict = new Dictionary<int, long>();
        private Dictionary<int, MonsterAttackerLog> _AttackerLogDict = new Dictionary<int, MonsterAttackerLog>();

        /// <summary>
        /// 增加攻击者ID
        /// </summary>
        /// <param name="roleID"></param>
        public void AddAttacker(GameClient client, int injured)
        {
            if (client == null) return;

            int roleID = client.ClientData.RoleID;
            lock (_AttackerLogDict)
            {
                _LastLogAttackerTicks = TimeUtil.NOW();

                /*
                int oldInjured = 0;
                _AttackerDict.TryGetValue(roleID, out oldInjured);
                _AttackerDict[roleID] = oldInjured + injured;
                _AttackerTicksDict[roleID] = _LastLogAttackerTicks; //为每个角色也记忆下攻击时间
                */

                MonsterAttackerLog attacker = null;
                if (!_AttackerLogDict.TryGetValue(roleID, out attacker))
                {
                    attacker = new MonsterAttackerLog();
                    attacker.RoleId = roleID;
                    attacker.RoleName = client.ClientData.RoleName;
                    attacker.Occupation = Global.CalcOriginalOccupationID(client);
                    attacker.FirstAttackMs = _LastLogAttackerTicks;
                    attacker.FirstAttack_MaxAttckV = RoleAlgorithm.GetMaxAttackV(client);
                    attacker.FirstAttack_MaxMAttackV = RoleAlgorithm.GetMaxMagicAttackV(client);

                    _AttackerLogDict[roleID] = attacker;
                }


                attacker.MaxAttackV = Math.Max(attacker.MaxAttackV, RoleAlgorithm.GetMaxAttackV(client));
                attacker.MaxMAttackV = Math.Max(attacker.MaxMAttackV, RoleAlgorithm.GetMaxMagicAttackV(client));
                attacker.TotalInjured += injured;
                attacker.InjureTimes++;
                attacker.LastAttackMs = _LastLogAttackerTicks;
            }
        }

        /// <summary>
        /// 删除攻击者ID(谨慎调用，目前只为弓箭卫士，专用。其他boss，不要做类似操作，否则，记忆的血量就被清空了)
        /// </summary>
        /// <param name="roleID"></param>
        public void RemoveAttacker(int roleID)
        {
            lock (_AttackerLogDict)
            {
               // _AttackerDict.Remove(roleID);
              //  _AttackerTicksDict.Remove(roleID);

                _AttackerLogDict.Remove(roleID);
            }
        }

        /// <summary>
        /// 判断roleid 对应的玩家是否攻击者
        /// </summary>
        /// <param name="roleID"></param>
        public bool IsAttackedBy(int roleID)
        {
            CheckAttackerListEfficiency();

            lock (_AttackerLogDict)
            {
                if (_AttackerLogDict.ContainsKey(roleID))
                    return true;

                /*
                if (_AttackerDict.ContainsKey(roleID))
                {
                    return true;
                }*/
            }

            return false;
        }

        /// <summary>
        /// 获取拥有者列表
        /// </summary>
        /// <returns></returns>
        public int GetAttackerFromList()
        {
            CheckAttackerListEfficiency();

            int attackerRid = -1;
            long maxInjured = 0;
            long nowTicks = TimeUtil.NOW();
            lock (_AttackerLogDict)
            {
                /*
                foreach (var key in _AttackerDict.Keys)
                {
                    long lastAttackTicks = 0;
                    _AttackerTicksDict.TryGetValue(key, out lastAttackTicks);
                    if (nowTicks - lastAttackTicks >= (30 * 1000))
                    {
                        continue; //如果距离上次攻击已经超过30秒，则累计的血量不再计算
                    }

                    int injured = _AttackerDict[key];
                    if (injured > maxInjured)
                    {
                        maxInjured = injured;
                        attackerRid = key;
                    }
                }*/

                foreach (var attacker in _AttackerLogDict.Values)
                {
                    if (nowTicks - attacker.LastAttackMs >= 30000)
                    {
                        continue;
                    }

                    if (attacker.TotalInjured > maxInjured)
                    {
                        maxInjured = attacker.TotalInjured;
                        attackerRid = attacker.RoleId;
                    }
                }
            }

            return attackerRid;
        }

        /// <summary>
        /// 获取拥有者列表
        /// </summary>
        /// <returns></returns>
        public List<int> GetAttackerList()
        {
            List<int> attackerList = new List<int>();
            long nowTicks = TimeUtil.NOW();
            lock (_AttackerLogDict)
            {
                foreach (var attacker in _AttackerLogDict.Values)
                {
                    if (nowTicks - attacker.LastAttackMs >= 15000)
                    {
                        continue;
                    }

                    attackerList.Add(attacker.RoleId);
                }

                /*
                foreach (var key in _AttackerDict.Keys)
                {
                    long lastAttackTicks = 0;
                    _AttackerTicksDict.TryGetValue(key, out lastAttackTicks);
                    if (nowTicks - lastAttackTicks >= (15 * 1000))
                    {
                        continue; //如果距离上次攻击已经超过15秒，则不再计算
                    }

                    attackerList.Add(key);
                }*/
            }

            return attackerList;
        }

        /// <summary>
        /// 生成伤害记录日志
        /// </summary>
        /// <returns></returns>
        public string BuildAttackerLog()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("怪物伤害日志: MonsterId={0}, MonsterName={1},", XMonsterInfo.MonsterId, Global.GetMonsterNameByID(XMonsterInfo.MonsterId));
            lock (_AttackerLogDict)
            {
                sb.AppendFormat("共有{0}个攻击者: ", _AttackerLogDict.Count());
                sb.AppendLine();
                foreach (var attacker in _AttackerLogDict.Values)
                {
                    sb.Append("     ");
                    sb.AppendFormat("roleid={0}, ", attacker.RoleId);
                    sb.AppendFormat("rolename={0}, ",attacker.RoleName);
                    sb.AppendFormat("职业={0}, ", Global.GetOccupationStr(attacker.Occupation));
                    sb.AppendFormat("总计伤害={0}, ", attacker.TotalInjured);
                    long totalAttackSec = (attacker.LastAttackMs- attacker.FirstAttackMs) / 1000;
                    sb.AppendFormat("共用时={0}秒, ", totalAttackSec);
                    sb.AppendFormat("伤害次数={0}, ", attacker.InjureTimes);
                    sb.AppendFormat("首次伤害物攻={0}, ", attacker.FirstAttack_MaxAttckV);
                    sb.AppendFormat("首次伤害魔攻={0}, ", attacker.FirstAttack_MaxMAttackV);
                    sb.AppendFormat("最大物攻={0}, ", attacker.MaxAttackV);
                    sb.AppendFormat("最大魔攻={0}, ", attacker.MaxMAttackV);

                    double maxAttack = Math.Max(attacker.MaxMAttackV, attacker.MaxAttackV);
                    if (maxAttack > 0)
                        sb.AppendFormat("攻击系数={0}, ", (attacker.TotalInjured * 1.0 / attacker.InjureTimes) / maxAttack);
                    else
                        sb.AppendFormat("攻击系数={0}[最大伤害无效]", "无效");

                    if (totalAttackSec > 0)
                        sb.AppendFormat("攻速系数={0}", attacker.InjureTimes * 1.0 / totalAttackSec);
                    else
                        sb.AppendFormat("攻速系数={0}[总攻击时间无效]", "无效");

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 最近一次是否掉落在障碍中的判断时间
        /// </summary>
        public long LastInObsJugeTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 最近一次搜寻敌人的时间
        /// </summary>
        public long LastSeekEnemyTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 下一次搜寻敌人的时间
        /// </summary>
        public long NextSeekEnemyTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 最近一次锁定敌人的时间
        /// </summary>
        public long LastLockEnemyTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 锁定敌人的时间
        /// </summary>
        public long LockFocusTime
        {
            get;
            set;
        }

        /// <summary>
        /// 最近一执行Timer的时间
        /// </summary>
        public long LastExecTimerTicks = 0;

        /// <summary>
        /// 当前所在的地图九宫格子位置
        /// </summary>
        //private int _CurrentGridX = -1;

        /// <summary>
        /// 当前所在的地图九宫格子位置
        /// </summary>
        //public int CurrentGridX
        //{
        //    get { lock (this) return _CurrentGridX; }
        //    set { lock (this) _CurrentGridX = value; }
        //}

        /// <summary>
        /// 当前所在的地图九宫格子位置
        /// </summary>
        //private int _CurrentGridY = -1;

        /// <summary>
        /// 当前所在的地图九宫格子位置
        /// </summary>
        //public int CurrentGridY
        //{
        //    get { lock (this) return _CurrentGridY; }
        //    set { lock (this) _CurrentGridY = value; }
        //}

        /// <summary>
        /// 上次检测失效对象的格子X坐标
        /// </summary>
        //public int _LastCheckGridX = -1;

        /// <summary>
        /// 上次检测失效对象的格子X坐标
        /// </summary>
        //public int LastCheckGridX
        //{
        //    get { lock (this) return _LastCheckGridX; }
        //    set { lock (this) _LastCheckGridX = value; }
        //}

        /// <summary>
        /// 上次检测失效对象的格子Y坐标
        /// </summary>
        //public int _LastCheckGridY = -1;

        /// <summary>
        /// 上次检测失效对象的格子Y坐标
        /// </summary>
        //public int LastCheckGridY
        //{
        //    get { lock (this) return _LastCheckGridY; }
        //    set { lock (this) _LastCheckGridY = value; }
        //}

        /// <summary>
        /// 上次检测失效对象的时间
        /// </summary>
        //public long _LastCheckGridTicks = 0;

        /// <summary>
        /// 上次检测失效对象的时间
        /// </summary>
        //public long LastCheckGridTicks
        //{
        //    get { lock (this) return _LastCheckGridTicks; }
        //    set { lock (this) _LastCheckGridTicks = value; }
        //}

        /*
        /// <summary>
        /// 当前所在的地图九宫中的其他玩家和怪物的ID
        /// </summary>
        private Dictionary<string, bool> _CurrentObjsDict = null;

        /// <summary>
        /// 当前所在的地图九宫中的其他玩家和怪物的ID
        /// </summary>
        public Dictionary<string, bool> CurrentObjsDict
        {
            get { lock (this) return _CurrentObjsDict; }
            set { lock (this) _CurrentObjsDict = value; }
        }

        /// <summary>
        /// 当前所在的地图九宫中的格子字典
        /// </summary>
        private Dictionary<string, bool> _CurrentGridsDict = null;

        /// <summary>
        /// 当前所在的地图九宫中的格子字典
        /// </summary>
        public Dictionary<string, bool> CurrentGridsDict
        {
            get { lock (this) return _CurrentGridsDict; }
            set { lock (this) _CurrentGridsDict = value; }
        }
         * */

        /// <summary>
        /// 移动的速度
        /// </summary>
        private double _MoveSpeed = 1.0;

        /// <summary>
        /// 移动的速度
        /// </summary>
        public double MoveSpeed
        {
            get
            {      
                return _MoveSpeed + TempPropsBuffer.GetExtProp((int)ExtPropIndexes.MoveSpeed);
            }

            set 
            {
                _MoveSpeed = value;
            }

            //get { lock (this) return _MoveSpeed + TempPropsBuffer.GetExtProp((int)ExtPropIndexes.MoveSpeed); }
            ////set { lock (this) _MoveSpeed = value <= 0 ? 1.0 : value; }
            //set { lock (this) _MoveSpeed = value; }
        }

        /// <summary>
        /// 移动的目的地坐标点
        /// </summary>
        private Point _DestPoint = new Point(-1, -1);

        /// <summary>
        /// 移动的目的地坐标点
        /// </summary>
        public Point DestPoint
        {
            get { lock (this) return _DestPoint; }
            set { lock (this) _DestPoint = value; }
        }

        private int _CopyMapID = -1;

        /// <summary>
        /// 副本地图编号
        /// </summary>
        public int CopyMapID
        {
            get
            {
                return _CopyMapID;
            }

            set 
            {
                _CopyMapID = value;
            }

            //get { lock (this) { return _CopyMapID; } }
            //set { lock (this) _CopyMapID = value; }
        }

        public bool _HandledDead = false;

        /// <summary>
        /// 是否已经处理过死亡了
        /// </summary>
        public bool HandledDead
        {
            get
            {
                return _HandledDead;
            }

            set 
            {
                _HandledDead = value;
            }

            //get { lock (this) { return _HandledDead; } }
            //set { lock (this) _HandledDead = value; }
        }

        /// <summary>
        /// 可见的角色的数量
        /// </summary>
        public int VisibleClientsNum = 0;

#endregion 其他扩展方法和属性

#region 身份属性

        /// <summary>
        /// 对象名称
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// 角色的ID(玩家扮演的角色ID和怪物的角色ID是分开的)
        /// </summary>
        public int RoleID { get; set; }

        /// <summary>
        /// 全局唯一ID
        /// </summary>
        public long UniqueID;

        /// <summary>
        /// 逻辑模块自定义的标记对象,用于动态刷怪时添加自定义的信息,供模块内部使用
        /// </summary>
        public object Tag;

        /// <summary>
        /// 模块管理器的类型,用于动态刷怪时设置类型信息,分辨由那个管理器管理
        /// </summary>
        public SceneUIClasses ManagerType = SceneUIClasses.Normal;

        public string MonsterName;

        /// <summary>
        /// 角色的性别, 0:男性, 1: 女性
        /// </summary>
        //public int RoleSex { get; set; }

        /// <summary>
        /// 获取或设置头像代号(选中后在控制界面显示的大头像/派别头像)
        /// 设置为 -1 表示不显示头像和血条, 角色/怪/NPC通用属性
        /// </summary>
        //public int FaceSign { get; set; }

        /// <summary>
        /// 获取或设置姓名
        /// </summary>
        //public string VSName
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// 扩展ID
        ///// </summary>
        //public int ExtensionID
        //{
        //    get;
        //    set;
        //}

#endregion

#region 基本值属性

        public bool IsAttackRole = true;

        public bool IsAutoSearchRoad = false;

        /// <summary>
        /// 获取或设置等级
        /// </summary>
        //public int VLevel { get; set; }

        /// 经验值是当前的级别修炼值，如果升级，扣除升级的经验值
        /// <summary>
        /// 获取或设置自身的经验值(如果为怪物等NPC,则为杀它的玩家可以得到的经验值)
        /// </summary>
        //public int VExperience { get; set; }

        /// <summary>
        /// 获取或设置自身的金币(如果为怪物等NPC,则为杀它的玩家可以得到的金币)
        /// </summary>
        //public int VMoney { get; set; }

        private double _VLife = 0.0;

        /// <summary>
        /// 获取或设置当前生命值
        /// </summary>
        public double VLife 
        {
            get
            {
                //lock (this)
                //{
                //    return _VLife;
                //}

                return Thread.VolatileRead(ref _VLife);
            }

            set
            {
                //lock (this)
                //{
                //    _VLife = value;
                //}
                Thread.VolatileWrite(ref _VLife, value);
            }
        }

        /// <summary>
        /// 获取最大生命值
        /// </summary>
        //public double VLifeMax
        //{
        //    get;
        //    set;
        //}

        private double _VMana = 0.0;

        /// <summary>
        /// 获取或设置当前魔法值
        /// </summary>
        public double VMana 
        {
            get
            {
                //lock (this)
                //{
                //    return _VMana;
                //}

                return Thread.VolatileRead(ref _VMana);
            }

            set
            {
                //lock (this)
                //{
                //    _VMana = value;
                //}

                Thread.VolatileWrite(ref _VMana, value);
            }
        }

        /// <summary>
        /// 获取最大魔法值
        /// </summary>
        //public double VManaMax
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 获取或设置加持/减持值总和
        /// 0-15对应基础属性值
        /// 16-20对应5大属性
        /// </summary>
        //public double[] Buff { get; set; }

        /// <summary>
        /// 对应的职业
        /// </summary>
        //public int ToOccupation
        //{
        //    get;
        //    set;
        //}
        
        /// <summary>
        /// 对应的角色的级别
        /// </summary>
        //public int ToRoleLevel
        //{
        //    get;
        //    set;
        //}
        
        ///// <summary>
        ///// 对应角色最小攻击力
        ///// </summary>
        //public int MinAttack
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// 对应角色最大攻击力
        ///// </summary>
        //public int MaxAttack
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// 对应角色的防御力
        ///// </summary>
        //public int Defense
        //{
        //    get;
        //    set;
        //}
        
        ///// <summary>
        ///// 魔防
        ///// </summary>
        //public int MDefense
        //{
        //    get;
        //    set;
        //}
        
        ///// <summary>
        ///// 命中率
        ///// </summary>
        //public double HitV
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// 闪避率
        ///// </summary>
        //public double Dodge
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// 生命恢复速度(每隔5秒百分之多少)
        ///// </summary>
        //public double RecoverLifeV
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// 魔法恢复速度(每隔5秒百分之多少)
        ///// </summary>
        //public double RecoverMagicV
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 怪物掉落ID
        /// </summary>
        //public int FallGoodsPackID
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 攻击方式(0: 物理攻击, 1: 魔法攻击, 2: 道术攻击)
        /// </summary>
        //public int AttackType
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 怪物类型
        /// </summary>
        public int MonsterType
        {
            get;
            set;
        }

        /// <summary>
        /// 谁杀死了我
        /// </summary>
        public int WhoKillMeID
        {
            get;
            set;
        }

        /// <summary>
        /// 谁杀死了我
        /// </summary>
        public string WhoKillMeName
        {
            get;
            set;
        }

        ///// <summary>
        ///// 大乱斗个人积分
        ///// </summary>
        //public int BattlePersonalJiFen
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// 大乱斗阵营积分
        ///// </summary>
        //public int BattleZhenYingJiFen
        //{
        //    get;
        //    set;
        //}

        //// 恶魔广场 血色堡垒 begin [11/14/2013 LiaoWei]
        ///// <summary>
        ///// 恶魔广场积分
        ///// </summary>
        //public int DaimonSquareJiFen
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// 血色堡垒积分
        ///// </summary>
        //public int BloodCastJiFen
        //{
        //    get;
        //    set;
        //}
        // 恶魔广场 血色堡垒 end [11/14/2013 LiaoWei]

        /// <summary>
        /// 随机的真气值最小值
        /// </summary>
        //public int ZhenQiMinValue
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 随机的真气值最大值
        /// </summary>
        //public int ZhenQiMaxValue
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 掉落是否属于拥有者
        /// </summary>
        //public int FallBelongTo
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// 怪物挂的技能ID列表
        ///// </summary>
        //private int[] _SkillIDs = null;

        ///// <summary>
        ///// 怪物挂的技能ID列表
        ///// </summary>
        //public int[] SkillIDs
        //{
        //    get { return _SkillIDs; }
        //    set { _SkillIDs = value; }
        //}

        /// <summary>
        /// 怪物所属阵营
        /// </summary>
        public int Camp;

#endregion

#region 角色自己主动召唤的属于自己的怪物的相关属性

        /// <summary>
        /// 怪物所有者 owner role 
        /// </summary>
        public GameClient OwnerClient = null;

        /// <summary>
        /// 宠物怪Ai类型【当怪物有主人的时候，它的攻击类型,默认自由攻击】
        /// </summary>
        public int _PetAiControlType = (int)PetMonsterControlType.FreeAttack;

        /// <summary>
        /// 宠物怪Ai类型【当怪物有主人的时候，它的攻击类型,默认自由攻击】
        /// </summary>
        public int PetAiControlType
        {
            get { return _PetAiControlType; }
            set 
            {
                int oldType = _PetAiControlType;

                _PetAiControlType = value;

                //控制类型变化后需要暂时取消目标
                if (oldType != _PetAiControlType)
                {
                    _LockObject = -1;
                }
            }
        }

#endregion 角色自己主动召唤的属于自己的怪物的相关属性

#region Buffer数据项管理

        /// <summary>
        /// 怪物的BufferData字典
        /// </summary>
        public Dictionary<int, BufferData> BufferDataDict = null;

        /// <summary>
        /// 上次道士加血的补给时间
        /// </summary>
        public long DSStartDSAddLifeNoShowTicks = 0;

        /// <summary>
        /// 根据DSTimeShiDuNoShow buffer上次伤害时间
        /// </summary>
        public long DSStartDSSubLifeNoShowTicks = 0;

        /// <summary>
        /// 放毒的角色的ID
        /// </summary>
        public int FangDuRoleID = 0;

        /// <summary>
        /// 冻结开始的时间
        /// </summary>
        public long DongJieStart = 0;

        /// <summary>
        /// 冻结的持续时间
        /// </summary>
        public int DongJieSeconds = 0;

        /// <summary>
        /// 是否怪物被冻结了
        /// </summary>
        /// <returns></returns>
        public bool IsMonsterDongJie()
        {
            if (DongJieStart <= 0)
            {
                return false;
            }

            long ticks = TimeUtil.NOW();
            if (ticks >= (DongJieStart + (DongJieSeconds * 1000)))
            {
                return false;
            }

            return true;
        }

#endregion Buffer数据项管理

#region 界面属性

        /// <summary>
        /// 获取或设置精灵单位图片左上角点与精灵图片中角色脚底的X距离
        /// </summary>
        //public double CenterX { get; set; }

        /// <summary>
        /// 获取或设置精灵单位图片左上角点与精灵图片中角色脚底的Y距离
        /// </summary>
        //public double CenterY { get; set; }

        public event MoveToEventHandler MoveToComplete;

        /// <summary>
        /// 上次的死亡时间
        /// </summary>
        private long _LastDeadTicks = 0;

        /// <summary>
        /// 上次的死亡时间
        /// </summary>
        public long LastDeadTicks
        {
            get { return _LastDeadTicks; }
        }

        /// <summary>
        /// 移动的目标点
        /// </summary>
        /*public Point MoveToPos
        {
            get;
            set;
        }*/

        /// <summary>
        /// 是否在移动的中
        /// </summary>
        public bool IsMoving
        {
            get
            {
                if (GActions.Walk != _Action)
                {
                    return false;
                }

                long nowTicks = TimeUtil.NOW();
                return (nowTicks - _LastActionTick) < Global.MovingNeedTicksPerGrid;
            }
        }

        public event CoordinateEventHandler CoordinateChanged;

        private Point _SafeCoordinate;
        public Point SafeCoordinate
        {
            get
            {
                lock (this)
                {
                    return _SafeCoordinate;
                }
            }
        }

        private Point _SafeOldCoordinate = new Point(0, 0);
        public Point SafeOldCoordinate
        {
            get
            {
                lock (this)
                {
                    return _SafeOldCoordinate;
                }
            }
        }

        /// <summary>
        /// 是否是故事板得首次移动
        /// </summary>
        private bool _FirstStoryMove = false;

        /// <summary>
        /// 临时补救措施，不知道为什么，每次故事版总是通知自己第一次初始化时设置的坐标，导致
        /// 九宫格判断错误，通知客户端就行了隐藏，显示的操作，无法找到原因，只能临时采用这种
        /// 办法，记录原始值，如果是原始值，则不通知九宫格。
        /// </summary>
        public bool FirstStoryMove
        {
            get { return _FirstStoryMove; }
            set { _FirstStoryMove = value; }
        }

        /// <summary>
        /// 临时补救措施，不知道为什么，每次故事版总是通知自己第一次初始化时设置的坐标，导致
        /// 九宫格判断错误，通知客户端就行了隐藏，显示的操作，无法找到原因，只能临时采用这种
        /// 办法，记录原始值，如果是原始值，则不通知九宫格。
        /// </summary>
        public Point FirstCoordinate
        {
            get;
            set;
        }

        /// <summary>
        /// 平行场景ID
        /// </summary>
        public int SubMapCode = -1;

        /// <summary>
        /// 出生点格子坐标
        /// </summary>
        private Point firstGrid = default(Point);

        public bool isReturn = false;

        public Point getFirstGrid()
        {
            if (default(Point).Equals(firstGrid))
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.MonsterZoneNode.MapCode];
                firstGrid = new Point((int)(this.FirstCoordinate.X / gameMap.MapGridWidth), (int)(this.FirstCoordinate.Y / gameMap.MapGridHeight));
            }

            return firstGrid;
        }

        private Point _Coordinate = new Point(0, 0);

        /// <summary>
        /// 获取或设置精灵坐标(关联属性)
        /// </summary>
        public Point Coordinate
        {
            get 
            {
                //return (Point)GetValue(CoordinateProperty);
                return _Coordinate;
            }

            set 
            {
                lock (this)
                {
                    _SafeOldCoordinate = value;
                }

                //SetValue(CoordinateProperty, value);
                
                Point oldCoordinate = _Coordinate;
                _Coordinate = new Point(value.X, value.Y);

                ChangeCoordinateProperty2(this, oldCoordinate, _Coordinate);
            }
        }

        /*public static readonly DependencyProperty CoordinateProperty = DependencyProperty.Register(
            "Coordinate",
            typeof(Point),
            typeof(Monster),
            new PropertyMetadata(ChangeCoordinateProperty)
        );

        private static void ChangeCoordinateProperty(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Monster obj = (Monster)d;
            ChangeCoordinateProperty2(obj, (Point)e.OldValue, (Point)e.NewValue);
        }&*/

        private static void ChangeCoordinateProperty2(Monster obj, Point oldValue, Point newValue)
        {
            {
                Point p = (Point)newValue;
                lock (obj)
                {
                    obj._SafeOldCoordinate = (Point)oldValue;
                    obj._SafeCoordinate = p;
                }

                //obj.SetValue(Canvas.LeftProperty, p.X - obj.CenterX);
                //obj.SetValue(Canvas.TopProperty, p.Y - obj.CenterY);
                //obj.SetValue(Canvas.ZIndexProperty, (int)p.Y);

                if (obj.CoordinateChanged != null)
                {
                    obj.CoordinateChanged(obj);
                }
            }
        }

        private double _SafeDirection = 0.0;
        public double SafeDirection
        {
            get
            {
                return Thread.VolatileRead(ref _SafeDirection);
            }

            //get
            //{
            //    lock (this)
            //    {
            //        return _SafeDirection;
            //    }
            //}
        }

        private double _Direction = 0.0;

        /// <summary>
        /// 获取或设置精灵当前朝向:0朝上4朝下,顺时针依次为0,1,2,3,4,5,6,7(关联属性)
        /// </summary>
        public double Direction
        {
            get 
            {
                //return (double)GetValue(DirectionProperty);
                return _Direction;
            }

            set 
            {
                //SetValue(DirectionProperty, value);
                lock (this)
                {
                    _SafeDirection = value;
                }

                double oldDirection = _Direction;
                _Direction = value;
                if (Action == GActions.Attack || Action == GActions.Magic || Action == GActions.Bow)
                {
                    if (FrameCounter < EndFrame)
                    {
                        return;
                    }
                }
                ChangeDirectionProperty2(this, oldDirection, _Direction);
            }
        }

        /*public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(
            "Direction",
            typeof(double),
            typeof(Monster),
            new PropertyMetadata(ChangeDirectionProperty)
        );

        private static void ChangeDirectionProperty(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Monster obj = (Monster)d;
            ChangeDirectionProperty2(obj, (double)e.OldValue, (double)e.NewValue);
        }*/

        private static void ChangeDirectionProperty2(Monster obj, double oldValue, double newValue)
        {
            lock (obj)
            {
                obj._SafeDirection = (double)newValue;
            }
            obj.ChangeAction(false);
        }

#endregion

#region 控件属性

        /// <summary>
        /// 获取或设置精灵当前衣服代码
        /// </summary>
        //public int EquipmentBody { get; set; }

        /// <summary>
        /// 获取或设置精灵当前武器代码
        /// </summary>
        //public int EquipmentWeapon { get; set; }

        /// <summary>
        /// 获取或设置锁定的目标精灵对象
        /// </summary>
        private int _LockObject = -1;

        /// <summary>
        /// 获取或设置锁定的目标精灵对象
        /// </summary>
        public int LockObject 
        { 
            get 
            {
                lock (this)
                {
                    return _LockObject;
                }
            } 
            
            set 
            {
                lock (this)
                {
                    _LockObject = value;
                }
            } 
        }

        /// <summary>
        /// 获取或设置目标精灵
        /// </summary>
        public int TargetSrpite { get; set; }

        /// <summary>
        /// 获取或设置脚底示为障碍物区域扩展宽度
        /// </summary>
        //public int HoldWidth { get; set; }

        /// <summary>
        /// 获取或设置脚底示为障碍物区域扩展高度
        /// </summary>
        //public int HoldHeight { get; set; }

        /// <summary>
        /// 获取或设置物理攻击范围(距离)
        /// </summary>
        public int AttackRange { get; set; }

        /// <summary>
        /// 获取或设置索敌范围(距离)
        /// </summary>
        //public int SeekRange { get; set; }

        /// <summary>
        /// 上次执行动作的时间
        /// </summary>
        long _LastActionTick = 0;

        /// <summary>
        /// 上次执行动作的时间
        /// </summary>
        public long LastActionTick
        {
            get { return _LastActionTick; }
        }

        /// <summary>
        /// 上次执行攻击动作的时间
        /// </summary>
        long _LastAttackActionTick = 0;

        /// <summary>
        /// 上次执行动作的时间
        /// </summary>
        public long LastAttackActionTick
        {
            get { return _LastAttackActionTick; }
        }

        /// <summary>
        /// 记录战斗待机前的动作状态
        /// </summary>
        private GActions OldAction = GActions.Attack;

        public GActions _Action;

        /// <summary>
        /// 获取或设置精灵当前动作
        /// </summary>
        public GActions Action
        {
            get { return _Action; }
            set
            {
                if (_Action != value)
                {
                    if (value == GActions.Attack || value == GActions.Magic || value == GActions.Bow)
                    {
                        if (_Action == GActions.PreAttack) //防止跳过待机进行攻击
                        {
                            return;
                        }
                    }

                    _Action = value;
                    _LastAttackActionTick = TimeUtil.NOW();

                    //禁止怪物出现奔跑动作
                    if (_Action == GActions.Run)
                    {
                        _Action = GActions.Walk;
                    }

                    lock (this)
                    {
                        _SafeAction = _Action;
                    }

                    _LastActionTick = TimeUtil.NOW();
                    if (value == GActions.Attack || value == GActions.Magic || value == GActions.Bow)
                    {
                        if (FrameCounter < EndFrame)
                        {
                            return;
                        }
                    }

                    // 放技能的时候，不需要改变帧
                    if (CurrentMagic > 0)
                    {
                        return;
                    }

                    ChangeAction(true);
                }
            }
        }

        GActions _SafeAction;

        /// <summary>
        /// 获取或设置精灵当前动作
        /// </summary>
        public GActions SafeAction
        {
            get { lock (this) { return _SafeAction; } }
        }

        /// <summary>
        /// 各个动作的帧的速度
        /// </summary>
        //public int[] SpriteSpeedTickList { get; set; }

        /// <summary>
        /// 获取或设置精灵各动作对应的帧列个数
        /// </summary>
        //public int[] EachActionFrameRange { get; set; }

        /// <summary>
        /// 获取或设置精灵移动目的地
        /// </summary>
        public Point Destination { get; set; }

        /// <summary>
        /// 获取或设置精灵的魔法攻击目标坐标
        /// </summary>
        public Point EnemyTarget { get; set; }

        /// <summary>
        /// 是否启用了A*移动
        /// </summary>
        //public bool UseAStarMove { get; set; }

        /// <summary>
        /// 获取或设置帧推进器
        /// </summary>
        public int FrameCounter { get; set; }

        /// <summary>
        /// 获取或设置精灵当前动作开始图片列号
        /// </summary>
        public int StartFrame { get; set; }

        /// <summary>
        /// 获取或设置精灵当前动作结束图片列号
        /// </summary>
        public int EndFrame { get; set; }

        /// <summary>
        /// 获取或设置各动作产生实际效果的针序号
        /// </summary>
        //public int[] EffectiveFrame { get; set; }

        /// <summary>
        /// 心跳间隔
        /// </summary>
        public long _HeartInterval = 400;

#endregion

#region 方法与事件

        //定义精灵内部改动作通知事件
        public event SpriteChangeActionEventHandler SpriteChangeAction;

        /// <summary>
        /// 开始心跳
        /// </summary>
        private void BeginHeart()
        {
            //开始特效计时器;
            //Heart.Start();

            LastMonsterLivingSlotTicks = TimeUtil.NOW();
            LastMonsterLivingTicks = LastMonsterLivingSlotTicks;
            GlobalEventSource.getInstance().fireEvent(new MonsterBirthOnEventObject(this));
        }

        /// <summary>
        /// 开始特效
        /// </summary>
        public void Start()
        {
            //精灵生命
            Alive = true;

            //开始心跳
            BeginHeart();
        }

        bool isDeath = false; int deathDelay = 0;

        //long LastAttackTicks = 0;

        /// <summary>
        /// 计时器事件
        /// </summary>
        public virtual void Timer_Tick(object sender, EventArgs e)
        {
            //处理怪物死亡
            if (isDeath)
            {
                deathDelay += 1;
                if (deathDelay >= 10)
                {
                    //精灵生命--->怪物一次生命周期结束
                    //Alive = false;
                    //OnReallyDied();
                }
                return;
            }

            // 昏迷(冻结！) [5/7/2014 LiaoWei]
            if (IsMonsterDongJie())
                Action = GActions.Stand;

            long nowTicks = TimeUtil.NOW();
            if (nowTicks - LastMonsterLivingSlotTicks >= (60 * 1000))
            {
                LastMonsterLivingSlotTicks = nowTicks;
                GlobalEventSource.getInstance().fireEvent(new MonsterLivingTimeEventObject(this));
            }

            //如果是在移动中
            if (GActions.Walk == Action || GActions.Run == Action)
            {                
                if ((nowTicks - _LastActionTick) >= Global.MovingNeedTicksPerGrid * Global.MovingNeedStepPerGrid)
                {
                    if (null != MoveToComplete)
                    {
                        MoveToComplete(this);
                        return;
                    }
                }

                return; //走路和跑步没必须要走到下边去吧
            }

            double newDirection = ChangeDirectionValue();

#if ___CC___FUCK___YOU___BB___
            //如果触动起效帧
            //int action = Global.GetActionIndex(Action);
            //int frameNumber = MonsterInfo.EachActionFrameRange[action];
            //if (GActions.PreAttack == Action)
            //{
            //    action = Global.GetActionIndex(GActions.Stand);
            //    frameNumber = MonsterInfo.EachActionFrameRange[action];
            //}
#else
             //如果触动起效帧
            int action = Global.GetActionIndex(Action);
            int frameNumber = MonsterInfo.EachActionFrameRange[action];
            if (GActions.PreAttack == Action)
            {
                action = Global.GetActionIndex(GActions.Stand);
                frameNumber = MonsterInfo.EachActionFrameRange[action];
            }
            int EffectiveFrameCounter = -1;
            if (Action == GActions.Death)
            {
                //EffectiveFrameCounter = EffectiveFrame[action];
                EffectiveFrameCounter = (int)(newDirection * frameNumber) + (frameNumber - 1);
            }
            else
            {
                EffectiveFrameCounter = (int)(newDirection * frameNumber) + MonsterInfo.EffectiveFrame[action];
            }
             if (FrameCounter == EffectiveFrameCounter)
            {
                // SysConOut.WriteLine(string.Format("执行DoAction{0}：{1}:{2}", CurrentMagic, TimeUtil.NOW() * 10000, Action));
                DoAction();
            }
#endif




            //如果是一帧播放完毕，则判断是否是战斗动作，如果是则执行战斗待机(怪用普通待机替代, 角色有专门的待机动作)
            if (FrameCounter >= EndFrame && (_Action == GActions.Attack || _Action == GActions.Magic || _Action == GActions.Bow))
            {
                //首先判断技能是群攻还是单攻
                SystemXmlItem systemMagic = null;

                lock (CurrentSkillIDIndexLock)
                {
                    _ToExecSkillID = -1;
                    if (this._CurrentMagic > 0)
                    {
                        if (GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(this._CurrentMagic, out systemMagic))
                        {
                            int nextMagicID = systemMagic.GetIntValue("NextMagicID");
                            if (nextMagicID > 0)
                            {
                                _ToExecSkillID = nextMagicID;
                            }
                        }
                    }

                    _CurrentSkillIDIndex++;
                }

                //不再进入待机动作, 2014-06-23, 刘惠城

                OldAction = _Action;
                //_Action = GActions.PreAttack; //进入战斗待机状态
                _Action = GActions.Stand;

                _MaxAttackTimeSlot = 2000;

                //首先判断技能是群攻还是单攻
                if (null == systemMagic)
                {
                    GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(this._CurrentMagic, out systemMagic);
                }

                if (null != systemMagic)
                {
                    int attackInterval = systemMagic.GetIntValue("AttackInterval");
                    if (attackInterval > 0)
                    {
                        _MaxAttackTimeSlot = attackInterval;
                    }
                }

                //if (LastAttackTicks > 0)
                //{
                //    System.Diagnostics.Debug.WriteLine(string.Format("PreAttack {0}", TimeUtil.NOW() - LastAttackTicks));
                //}

                //LastAttackTicks = TimeUtil.NOW();

                //if (value == GActions.Attack || value == GActions.Magic || value == GActions.Bow)
                {
                    _LastAttackActionTick = TimeUtil.NOW();
                }

                lock (this)
                {
                    _SafeAction = _Action;
                }

                ChangeAction(false);
                FrameCounter = StartFrame;
            }
            else if (FrameCounter >= EndFrame && _Action == GActions.PreAttack) //如果是战斗待机状态，则转入战斗状态
            {
                _Action = OldAction; //重新进入战斗状态

                //if (LastAttackTicks > 0)
                //{
                //    System.Diagnostics.Debug.WriteLine(string.Format("Attack {0}", TimeUtil.NOW() - LastAttackTicks));
                //}

                //LastAttackTicks = TimeUtil.NOW();

                lock (this)
                {
                    _SafeAction = _Action;
                }

                ChangeAction(false);
                FrameCounter = StartFrame;
            }
            else
            {
                FrameCounter = FrameCounter >= EndFrame ? StartFrame : FrameCounter + 1;
            }

        }

        /// <summary>
        /// 根据设置的方向代号获取新的方向
        /// </summary>
        /// <returns></returns>
        private double ChangeDirectionValue()
        {
            //强制方向为0
            if ((int)MonsterTypes.CaiJi == this.MonsterType)
            {
                return 0;
            }

            return Direction;
        }

        /// <summary>
        /// 改变精灵动作状态后激发的属性
        /// </summary>
        private void ChangeAction(bool resetCounter)
        {
            int n = Global.GetActionIndex(Action);
#if ___CC___FUCK___YOU___BB___
            int frameNumber = 0;// MonsterInfo.EachActionFrameRange[n];
#else
             int frameNumber = MonsterInfo.EachActionFrameRange[n];
#endif
            if (frameNumber <= 0)
            {
                //System.Diagnostics.Debug.WriteLine("frameNumber=" + frameNumber.ToString());
            }

            int newDirection = (int)ChangeDirectionValue();

            //如果是死亡的动作, 则只会有一个方向0, [实际图片未必合方向吻合，不同的怪物和角色，有随机生成]
            if (Action == GActions.Death)
            {
                //newDirection = 0;
            }

            if ((int)MonsterTypes.CaiJi == this.MonsterType)
            {
                newDirection = 0;
            }
#if ___CC___FUCK___YOU___BB___
            int actionTick = 0;
            if (actionTick <= 0)
            {
                actionTick = 100;
            }
#else
                   int actionTick = Global.GetActionTick((GActions)Action, MonsterInfo.SpriteSpeedTickList);
            if (actionTick <= 0)
			{
				actionTick = 100;
			}
#endif
            

            if (GActions.Death == Action)
            {
                actionTick = actionTick * 2; //估计增加两倍，好让客户端看到尸体
            }

            //对于PreAttack动作特殊处理，为了外部设置方便，会设置一个总的值例如2000毫秒，但是对于一帧待机的对，对于多帧的就错了，所以，这里做一下运算处理
            if (GActions.PreAttack == Action)
            {
                int preAttackIndex = Global.GetActionIndex(GActions.Stand);
#if ___CC___FUCK___YOU___BB___
                frameNumber = 0;//MonsterInfo.EachActionFrameRange[preAttackIndex];

                ////如果是攻击待机，则另外计算获取
                //actionTick = Global.GetPreAttackTicksByMonsterType((MonsterTypes)this.MonsterType);

                actionTick = 0;// Global.GetActionTick(GActions.Stand, MonsterInfo.SpriteSpeedTickList);
#else
                frameNumber = MonsterInfo.EachActionFrameRange[preAttackIndex];
                actionTick = Global.GetActionTick(GActions.Stand, MonsterInfo.SpriteSpeedTickList);
#endif

                //if (frameNumber > 1)
                //{
                //    actionTick = actionTick / frameNumber;
                //}
            }

            switch (Action)
            {
                case GActions.Stand:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.Walk:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.Run:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.Attack:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.Magic:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.Bow:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.Death:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.HorseStand:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.HorseRun:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.HorseDead:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.Sit:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.PreAttack:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;
                case GActions.Injured:
                    RefreshThread(actionTick, newDirection * frameNumber, (newDirection + 1) * frameNumber - 1);
                    break;	
            }
            if (resetCounter)
            {
                FrameCounter = StartFrame;
            }
            else
            {
                if (FrameCounter < StartFrame || StartFrame >= EndFrame)
                {
                    FrameCounter = StartFrame;
                }
            }
        }

        /// <summary>
        /// 刷新精灵
        /// </summary>
        /// <param name="timeSpan">动作图片切换间隔</param>
        /// <param name="startFrame">动作在合成大图中的开始列</param>
        /// <param name="endFrame">动作在合成大图中的结束列</param>
        private void RefreshThread(double timeSpan, int startFrame, int endFrame)
        {
            //RefreshRate = TimeSpan.FromMilliseconds(timeSpan);
            _HeartInterval = (long)timeSpan;
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

#endregion

#region 执行动作

        /// <summary>
        /// 执行精灵动作
        /// </summary>
        private void DoAction()
        {
            switch (Action)
            {
                case GActions.Attack: //如果是物理攻击
                    {
                        if (this.LockObject == -1) //如果已经没有目标对象, 改成原地站立
                        {
                            //this.Action = GActions.Stand;
                            if (null != SpriteChangeAction)
                            {
                                SpriteChangeAction(this, new SpriteChangeActionEventArgs() { Action = (int)GActions.Stand });
                            }
                        }
                        else
                        {
                            SystemXmlItem systemMagic = null;
                            if (CurrentMagic > 0)
                            {
                                if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(CurrentMagic, out systemMagic))
                                {
                                    return;
                                }

                                if (systemMagic.GetIntValue("InjureType") == 1)
                                {
                                    return;
                                }

                                if (MagicFinish == -2)
                                {
                                    return;
                                }

                                if (GameManager.FlagManyAttackOp)
                                {
                                    if (this.MyMagicsManyTimeDmageQueue.GetManyTimeDmageQueueItemNumEx() > 0)
                                    {
                                        return;
                                    }
                                }
                                else 
                                {
                                    if (this.MyMagicsManyTimeDmageQueue.GetManyTimeDmageQueueItemNum() > 0)
                                    {
                                        return;
                                    }
                                }
                            }

                            //攻击敌人
                            Global.DoInjure(this, _LockObject, EnemyTarget);
                        }
                    }

                    break;

                case GActions.Death: //死亡
                    isDeath = true;
                    break;
            }
        }

#endregion 执行动作

#region 移动路径信息
        /// <summary>
        /// 怪物当前路径信息字符串
        /// </summary>
        //private string _PathString = "";

        /// <summary>
        /// 怪物当前路径信息字符串
        /// </summary>
        /*public string PathString
        {
            get { lock (this) return _PathString; }
            set { lock (this) _PathString = value; }
        }*/
#endregion

#region 相关动作相应

        /// <summary>
        /// 怪物真正死亡时调用 Alive
        ///  为false时调用
        /// </summary>
        protected void OnReallyDied()
        {
            //如果是临时召唤的怪物，则将其移除
            MonsterZoneNode.OnReallyDied(this);
        }

#endregion

#region 技能相关

        /// <summary>
        /// 技能编号
        /// </summary>
        private int _CurrentMagic = -1;

        /// <summary>
        /// 技能编号
        /// </summary>
        public int CurrentMagic
        {
            get { return _CurrentMagic; }
            set { _CurrentMagic = value; }
        }

        /// <summary>
        /// 技能攻击完成标记
        /// </summary>
        private int _MagicFinish = 0;

        /// <summary>
        /// 技能攻击是否完成
        /// </summary>
        public int MagicFinish
        {
            get { return _MagicFinish; }
            set
            {
                _MagicFinish = value;
            }
        }

        //技能CD控制
        private MagicCoolDownMgr _MagicCoolDownMgr = new MagicCoolDownMgr();

        /// <summary>
        /// 技能CD控制
        /// </summary>
        /// 与此相关的都要改
        public MagicCoolDownMgr MyMagicCoolDownMgr
        {
            get {return _MagicCoolDownMgr;}
            // get { lock (this) return _MagicCoolDownMgr; }
        }

        /// <summary>
        /// 攻击间隔
        /// </summary>
        private long _MaxAttackTimeSlot = 1000;

        /// <summary>
        /// 攻击间隔
        /// </summary>
        public long MaxAttackTimeSlot
        {
            get { return _MaxAttackTimeSlot; }
            set { _MaxAttackTimeSlot = value; }
        }

#endregion 技能相关

#region 动态技能队列

        /// <summary>
        /// 轮询使用的怪物技能ID的数组索引的锁定对象
        /// </summary>
        public Object CurrentSkillIDIndexLock = new Object();

        /// <summary>
        /// 轮询使用的怪物技能ID的数组索引
        /// </summary>
        public int _CurrentSkillIDIndex = 0;

        /// <summary>
        /// 上次使用的技能ID
        /// </summary>
        public int _ToExecSkillID = -1;

        /// <summary>
        /// 动态的技能ID列表
        /// </summary>
        private List<DynSkillItem> DynSkillIDsList = new List<DynSkillItem>();

        /// <summary>
        /// 添加一个动态的技能
        /// </summary>
        /// <param name="sillID"></param>
        /// <param name="priority"></param>
        public void AddDynSkillID(int skillID, int priority)
        {
            lock (CurrentSkillIDIndexLock)
            {
                bool found = false;
                for (int i = 0; i < DynSkillIDsList.Count; i++)
                {
                    if (DynSkillIDsList[i].SkillID == skillID)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    DynSkillIDsList.Add(new DynSkillItem()
                        {
                            SkillID = skillID,
                            Priority = priority,
                        });

                    DynSkillIDsList.Sort((left, right) =>
                        {
                            if (left.Priority < right.Priority) //倒排序
                                return 1;
                            else if (left.Priority == right.Priority)
                                return 0;
                            else
                                return -1;
                        });
                }
            }
        }

        /// <summary>
        /// 删除一个动态技能
        /// </summary>
        /// <param name="skillID"></param>
        public void RemoveDynSkill(int skillID)
        {
            lock (CurrentSkillIDIndexLock)
            {
                for (int i = 0; i < DynSkillIDsList.Count; i++)
                {
                    if (DynSkillIDsList[i].SkillID == skillID)
                    {
                        DynSkillIDsList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 清空动态技能
        /// </summary>
        private void ClearDynSkill()
        {
            lock (CurrentSkillIDIndexLock)
            {
                //上次使用的技能ID
                _ToExecSkillID = -1;

                DynSkillIDsList.Clear();
                _CurrentSkillIDIndex = 0;
            }
        }

        /// <summary>
        /// 获取自动使用的技能
        /// </summary>
        /// <returns></returns>
        public int GetAutoUseSkillID()
		{
            int skillID = -1;

            lock (CurrentSkillIDIndexLock)
            {
                if (_ToExecSkillID > 0)
                {
                    skillID = _ToExecSkillID;
                    return skillID;
                }

                if (DynSkillIDsList.Count > 0)
                {
                    if (_CurrentSkillIDIndex >= DynSkillIDsList.Count)
                    {
                        _CurrentSkillIDIndex = 0;
                    }

                    for (int i = _CurrentSkillIDIndex; i < DynSkillIDsList.Count; i++)
                    {
                        //判断是否处在
                        if (!MyMagicCoolDownMgr.SkillCoolDown(DynSkillIDsList[i].SkillID))
                        {
                            continue;
                        }

                        if (!SkillNeedMagicVOk(this, DynSkillIDsList[i].SkillID))
                        {
                            continue;
                        }

                        skillID = DynSkillIDsList[i].SkillID;
                        break;
                    }
                }

                if (skillID <= 0)
                {
#if ___CC___FUCK___YOU___BB___
                    if (null != XMonsterInfo.Skills)
                    {
                        if (_CurrentSkillIDIndex >= XMonsterInfo.Skills.Count)
                        {
                            _CurrentSkillIDIndex = 0;
                        }

                        for (int i = _CurrentSkillIDIndex; i < XMonsterInfo.Skills.Count; i++)
                        {
                            //判断是否处在
                            if (!MyMagicCoolDownMgr.SkillCoolDown(XMonsterInfo.Skills[i]))
                            {
                                continue;
                            }

                            if (!SkillNeedMagicVOk(this, XMonsterInfo.Skills[i]))
                            {
                                continue;
                            }

                            skillID = XMonsterInfo.Skills[i];
                            break;
                        }
                    }
#else
                     if (null != MonsterInfo.SkillIDs)
                    {
                        if (_CurrentSkillIDIndex >= MonsterInfo.SkillIDs.Length)
                        {
                            _CurrentSkillIDIndex = 0;
                        }

                        for (int i = _CurrentSkillIDIndex; i < MonsterInfo.SkillIDs.Length; i++)
                        {
                            //判断是否处在
                            if (!MyMagicCoolDownMgr.SkillCoolDown(MonsterInfo.SkillIDs[i]))
                            {
                                continue;
                            }

                            if (!SkillNeedMagicVOk(this, MonsterInfo.SkillIDs[i]))
                            {
                                continue;
                            }

                            skillID = MonsterInfo.SkillIDs[i];
                            break;
                        }
                    }
#endif

                }
            }

            //测试动态的技能
            //if (this.MonsterInfo.ExtensionID == 100)
            //{
            //    lock (CurrentSkillIDIndexLock)
            //    {
            //        if (DynSkillIDsList.Count <= 0)
            //        {
            //            AddDynSkillID(5004, 100);
            //            AddDynSkillID(1027, 200);
            //        }
            //    }
            //}

			return skillID;
		}

        /// <summary>
        /// 判断使用技能需要的魔法值是否足够
        /// </summary>
        /// <param name="skillID"></param>
        /// <returns></returns>
        protected bool SkillNeedMagicVOk(Monster monster, int skillID)
        {
            // 改造 [11/13/2013 LiaoWei]
            //获取法术攻击需要消耗的魔法值
            int usedMagicV = Global.GetNeedMagicV(monster, skillID, 1);

            if (usedMagicV > 0)
            {
#if ___CC___FUCK___YOU___BB___
                int nMax = 0;
#else
                int nMax = (int)monster.MonsterInfo.VManaMax;
#endif
                int nNeed = (int)(nMax * (usedMagicV / 100.0)); //非浮点数运算，有问题

                nNeed = Global.GMax(0, nNeed);
                if (monster.VMana - nNeed < 0)
                    return false;
            }

            return true;
        }

#endregion 动态技能队列

#region 发送到客户端数据缓存
        /*
        /// <summary>
        /// 缓存锁
        /// </summary>
        private object _CachingBytesDataMutex = new object();

        /// <summary>
        /// 上次缓存的时间
        /// </summary>
        private long _LastCachingBytesDataTicks = 0;

        /// <summary>
        /// 缓存的数据
        /// </summary>
        private byte[] _CachingBytesData = null;

        /// <summary>
        /// 从缓存中获取怪物对象
        /// </summary>
        /// <returns></returns>
        private byte[] GetBytesDataFromCaching()
        {
            long ticks = TimeUtil.NOW();
            lock (_CachingBytesDataMutex)
            {
                if (null != _CachingBytesData)
                {
                    if (ticks - _LastCachingBytesDataTicks < GameManager.MaxCachingMonsterToClientBytesDataTicks)
                    {
                        //System.Diagnostics.Debug.WriteLine(string.Format("从怪物缓存中读取数据"));
                        return _CachingBytesData;
                    }
                }

                _LastCachingBytesDataTicks = ticks;
                MonsterData md = this.GetMonsterData();
                _CachingBytesData = DataHelper.ObjectToBytes<MonsterData>(md);
                return _CachingBytesData;
            }
        }

        /// <summary>
        /// 释放缓存
        /// </summary>
        public void ReleaseBytesDataFromCaching(bool bForce = false)
        {
            lock (_CachingBytesDataMutex)
            {
                if (null != _CachingBytesData)
                {
                    long ticks = TimeUtil.NOW();
                    if (bForce || ticks - _LastCachingBytesDataTicks >= GameManager.MaxCachingMonsterToClientBytesDataTicks)
                    {
                        //System.Diagnostics.Debug.WriteLine(string.Format("将怪物缓存释放"));
                        _CachingBytesData = null;
                    }
                }
            }
        }

        /// <summary>
        /// 从缓存中获取怪物对象
        /// </summary>
        /// <returns></returns>
        public TCPOutPacket GetTCPOutPacketFromCaching(int cmdID)
        {
            byte[] bytesCmd = this.GetBytesDataFromCaching();
            return TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, bytesCmd, 0, bytesCmd.Length, cmdID);
        }
        */
#endregion 发送到客户端数据缓存

#region 致命受伤时间

        /// <summary>
        /// 最后一次致命受伤时间
        /// </summary>
        //public long LastFatalInjuredTicks
        //{
        //    get;
        //    set;
        //}

#endregion 致命受伤时间

#region 是否是动态刷怪

        /// <summary>
        /// 是否是动态刷出的怪物
        /// </summary>
        public bool DynamicMonster = false;

        /// <summary>
        /// 动态刷怪追击范围
        /// </summary>
        public int DynamicPursuitRadius = 0;

#endregion 是否是动态刷怪

#region 加入死亡队列的时间

        /// <summary>
        /// 加入死亡队列的时间
        /// </summary>
        public long AddToDeadQueueTicks
        {
            get;
            set;
        }

#endregion 加入死亡队列的时间

#region 怪物计数器

        /// <summary>
        /// 静态计数锁
        /// </summary>
        private static Object CountLock = new Object();

        /// <summary>
        /// 总的怪物计数
        /// </summary>
        private static int TotalMonsterCount = 0;

        /// <summary>
        /// 增加计数
        /// </summary>
        public static void IncMonsterCount()
        {
            lock (CountLock)
            {
                TotalMonsterCount++;
            }
        }

        /// <summary>
        /// 减少计数
        /// </summary>
        public static void DecMonsterCount()
        {
            lock (CountLock)
            {
                TotalMonsterCount--;
            }
        }

        /// <summary>
        /// 获取计数
        /// </summary>
        public static int GetMonsterCount()
        {
            int count = 0;
            lock (CountLock)
            {
                count = TotalMonsterCount;
            }

            return count;
        }

#endregion 怪物计数器

#region 怪物的临时属性Buffer

        /// <summary>
        /// 怪物的临时属性Buffer
        /// </summary>
        public MonsterBuffer TempPropsBuffer = new MonsterBuffer();

#endregion 怪物的临时属性Buffer

#region 金币副本怪数据

        // 金币副本中的怪的特殊数据 [6/11/2014 LiaoWei]
        // 第几步
        public int Step;

        /// <summary>
        /// 移动的时间
        /// </summary>
        public long MoveTime;

        /// <summary>
        /// 路径点
        /// </summary>
        public List<int[]> PatrolPath;

#endregion 金币副本怪数据

#region BossAI相关

        private long LastMonsterLivingSlotTicks = TimeUtil.NOW();
        private long LastMonsterLivingTicks = TimeUtil.NOW();

        /// <summary>
        /// 获取怪物的存活时间
        /// </summary>
        /// <returns></returns>
        public long GetMonsterLivingTicks()
        {
            return (TimeUtil.NOW() - LastMonsterLivingTicks);
        }

        /// <summary>
        /// 触发锁
        /// </summary>
        public Object TriggerMutex = new Object();

        /// <summary>
        /// 记录BossAI 触发的次数
        /// </summary>
        private Dictionary<int, int> TriggerNumDict = new Dictionary<int, int>();

        /// <summary>
        /// 记录BossAI 触发的时间
        /// </summary>
        private Dictionary<int, long> TriggerCDDict = new Dictionary<int, long>();

        /// <summary>
        /// 是否能够执行指定的BossAI项
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool CanExecBossAI(BossAIItem bossAIItem)
        {
            if (bossAIItem.TriggerNum <= 0 && bossAIItem.TriggerCD <= 0) //这种情况最多，不再走下边流程，加快执行速度
            {
                return true;
            }

            int num = 0;
            long lastTicks = 0;

            lock (TriggerMutex)
            {
                if (bossAIItem.TriggerNum > 0)
                {
                    TriggerNumDict.TryGetValue(bossAIItem.ID, out num);

                    if (num >= bossAIItem.TriggerNum)
                    {
                        return false;
                    }
                }                

                if (bossAIItem.TriggerCD > 0)
                {
                    TriggerCDDict.TryGetValue(bossAIItem.ID, out lastTicks);

                    if ((TimeUtil.NOW()) - lastTicks < (bossAIItem.TriggerCD * 1000))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 是否能够执行指定的BossAI项
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool RecBossAI(BossAIItem bossAIItem)
        {
            int num = 0;
            long lastTicks = 0;

            lock (TriggerMutex)
            {
                if (bossAIItem.TriggerNum > 0)
                {
                    if (!TriggerNumDict.TryGetValue(bossAIItem.ID, out num))
                    {
                        num = 0;
                    }

                    num += 1;
                    TriggerNumDict[bossAIItem.ID] = num;
                }

                if (bossAIItem.TriggerCD > 0)
                {
                    TriggerCDDict[bossAIItem.ID] = (TimeUtil.NOW());
                }
            }

            return true;
        }

        /// <summary>
        /// 清空bossAI的记录
        /// </summary>
        public void ClearBossAI()
        {
            lock (TriggerMutex)
            {
                TriggerNumDict.Clear();
                TriggerCDDict.Clear();
            }
        }

#endregion BossAI相关

#region 扩展属性ID

        /// <summary>
        /// 扩展属性ID管理
        /// </summary>
        public SpriteExtensionProps ExtensionProps = new SpriteExtensionProps();

#endregion 扩展属性ID

#region 技能执行队列

        /// <summary>
        /// 执行分段攻击的技能执行队列
        /// </summary>
        public MagicsManyTimeDmageQueue MyMagicsManyTimeDmageQueue = new MagicsManyTimeDmageQueue();

#endregion 技能执行队列

#region 新的扩展buffer实现

        /// <summary>
        /// 新的buffer扩展管理
        /// </summary>
        public BufferExtManager MyBufferExtManager = new BufferExtManager();

#endregion 新的扩展buffer实现

#region 采集物专用

        public Object CaiJiStateLock = new Object();
        /// <summary>
        /// 是否已经被采集
        /// </summary>
        private bool _IsCollected = false;
        public bool IsCollected
        {
            get { return _IsCollected; }
            set { _IsCollected = value; }
        }
#endregion 采集物专用
    }
}
