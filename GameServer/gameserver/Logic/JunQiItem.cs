using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Logic;
using System.Windows;
//using System.Windows.Threading;
using GameServer.Interface;
using GameServer.Core.Executor;
using Tmsk.Contract;

namespace GameServer.Logic
{
    /// <summary>
    /// 帮旗的数据项
    /// </summary>
    public class JunQiItem : IObject
    {
        #region 线程安全

        /// <summary>
        /// 线程锁
        /// </summary>
        private Object Mutex = new object();

        #endregion 线程安全

        #region 基础值

        /// <summary>
        /// 帮旗数据
        /// </summary>
        private JunQiData _MyJunQiData = new JunQiData();

        /// <summary>
        /// 获取帮旗数据(禁止直接修改数据)
        /// </summary>
        public JunQiData GetJunQiData()
        {
            return _MyJunQiData;
        }

        /// <summary>
        /// 帮旗的ID
        /// </summary>
        public int JunQiID
        {
            get { lock (Mutex) { return _MyJunQiData.JunQiID; } }
            set { lock (Mutex) { _MyJunQiData.JunQiID = value; } }
        }

        /// <summary>
        /// 帮旗的名称
        /// </summary>
        public string QiName
        {
            get { lock (Mutex) { return _MyJunQiData.QiName; } }
            set { lock (Mutex) { _MyJunQiData.QiName = value; } }
        }

        /// <summary>
        /// 帮旗的级别
        /// </summary>
        public int JunQiLevel
        {
            get { lock (Mutex) { return _MyJunQiData.JunQiLevel; } }
            set { lock (Mutex) { _MyJunQiData.JunQiLevel = value; } }
        }

        /// <summary>
        /// 区ID
        /// </summary>
        public int ZoneID
        {
            get { lock (Mutex) { return _MyJunQiData.ZoneID; } }
            set { lock (Mutex) { _MyJunQiData.ZoneID = value; } }
        }

        /// <summary>
        /// 帮会的ID
        /// </summary>
        public int BHID
        {
            get { lock (Mutex) { return _MyJunQiData.BHID; } }
            set { lock (Mutex) { _MyJunQiData.BHID = value; } }
        }

        /// <summary>
        /// 帮会名称
        /// </summary>
        public string BHName
        {
            get { lock (Mutex) { return _MyJunQiData.BHName; } }
            set { lock (Mutex) { _MyJunQiData.BHName = value; } }
        }

        /// <summary>
        /// 旗座NPC的ID
        /// </summary>
        public int QiZuoNPC
        {
            get { lock (Mutex) { return _MyJunQiData.QiZuoNPC; } }
            set { lock (Mutex) { _MyJunQiData.QiZuoNPC = value; } }
        }

        /// <summary>
        /// 所在的地图的编号
        /// </summary>
        public int MapCode
        {
            get { lock (Mutex) { return _MyJunQiData.MapCode; } }
            set { lock (Mutex) { _MyJunQiData.MapCode = value; } }
        }

        /// <summary>
        /// 当前所在的位置X坐标
        /// </summary>
        public int PosX
        {
            get { lock (Mutex) { return _MyJunQiData.PosX; } }
            set { lock (Mutex) { _MyJunQiData.PosX = value; } }
        }

        /// <summary>
        /// 当前所在的位置Y坐标
        /// </summary>
        public int PosY
        {
            get { lock (Mutex) { return _MyJunQiData.PosY; } }
            set { lock (Mutex) { _MyJunQiData.PosY = value; } }
        }

        /// <summary>
        /// 当前的方向
        /// </summary>
        public int Direction
        {
            get { lock (Mutex) { return _MyJunQiData.Direction; } }
            set { lock (Mutex) { _MyJunQiData.Direction = value; } }
        }

        /// <summary>
        /// 最大的生命值
        /// </summary>
        public int LifeV
        {
            get { lock (Mutex) { return _MyJunQiData.LifeV; } }
            set { lock (Mutex) { _MyJunQiData.LifeV = value; } }
        }

        /// <summary>
        /// 每一刀伤害的生命值
        /// </summary>
        public int CutLifeV
        {
            get { lock (Mutex) { return _MyJunQiData.CutLifeV; } }
            set { lock (Mutex) { _MyJunQiData.CutLifeV = value; } }
        }

        /// <summary>
        /// 开始的时间
        /// </summary>
        public long StartTime
        {
            get { lock (Mutex) { return _MyJunQiData.StartTime; } }
            set { lock (Mutex) { _MyJunQiData.StartTime = value; } }
        }

        /// <summary>
        /// 形象编号
        /// </summary>
        public int BodyCode
        {
            get { lock (Mutex) { return _MyJunQiData.BodyCode; } }
            set { lock (Mutex) { _MyJunQiData.BodyCode = value; } }
        }

        /// <summary>
        /// 头像编号
        /// </summary>
        public int PicCode
        {
            get { lock (Mutex) { return _MyJunQiData.PicCode; } }
            set { lock (Mutex) { _MyJunQiData.PicCode = value; } }
        }

        /// <summary>
        /// 所属管理器
        /// </summary>
        public SceneUIClasses ManagerType = SceneUIClasses.Normal;

        #endregion 基础值

        #region 额外添加值

        private int _CopyMapID = -1;

        /// <summary>
        /// 副本地图编号
        /// </summary>
        public int CopyMapID
        {
            get { lock (this) { return _CopyMapID; } }
            set { lock (this) _CopyMapID = value; }
        }

        private int _CurrentLifeV;

        /// <summary>
        /// 当前的生命值
        /// </summary>
        public int CurrentLifeV
        {
            get { lock (Mutex) return _CurrentLifeV; }
            set { lock (Mutex) _CurrentLifeV = value; }
        }

        /// <summary>
        /// 一直攻击的角色ID
        /// </summary>
        private int _AttackedRoleID;

        /// <summary>
        /// 最后一次判断拥有者的时间
        /// </summary>
        private long _LastAttackedTick = 0;

        /// <summary>
        /// 一直攻击的角色ID
        /// </summary>
        public int AttackedRoleID
        {
            get
            {
                lock (this)
                {
                    return _AttackedRoleID;
                }
            }

            set
            {
                lock (this)
                {
                    long ticks = TimeUtil.NOW();
                    if (_AttackedRoleID == value)
                    {
                        _LastAttackedTick = ticks;
                    }
                    else
                    {
                        if (ticks - _LastAttackedTick >= (10 * 1000))
                        {
                            _LastAttackedTick = ticks;
                            _AttackedRoleID = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 当前所在的地图九宫格子位置
        /// </summary>
        private int _CurrentGridX = -1;

        /// <summary>
        /// 当前所在的地图九宫格子位置
        /// </summary>
        public int CurrentGridX
        {
            get { lock (this) return _CurrentGridX; }
            set { lock (this) _CurrentGridX = value; }
        }

        /// <summary>
        /// 当前所在的地图九宫格子位置
        /// </summary>
        private int _CurrentGridY = -1;

        /// <summary>
        /// 当前所在的地图九宫格子位置
        /// </summary>
        public int CurrentGridY
        {
            get { lock (this) return _CurrentGridY; }
            set { lock (this) _CurrentGridY = value; }
        }

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

        public bool _HandledDead = false;

        /// <summary>
        /// 是否已经处理过死亡了
        /// </summary>
        public bool HandledDead
        {
            get { lock (this) { return _HandledDead; } }
            set { lock (this) _HandledDead = value; }
        }

        /// <summary>
        /// 帮旗的死亡时间
        /// </summary>
        private long _JunQiDeadTicks = 0;

        /// <summary>
        /// 帮旗的死亡时间
        /// </summary>
        public long JunQiDeadTicks
        {
            get { lock (Mutex) return _JunQiDeadTicks; }
            set { lock (Mutex) _JunQiDeadTicks = value; }
        }

        #endregion 额外添加值

        #region 实现IObject接口方法

        /// <summary>
        /// 对象的类型
        /// </summary>
        public ObjectTypes ObjectType
        {
            get { return ObjectTypes.OT_JUNQI; }
        }

        /// <summary>
        /// 获取ID
        /// </summary>
        /// <returns></returns>
        public int GetObjectID()
        {
            return JunQiID;
        }

        /// <summary>
        /// 最后一次补血补魔的时间
        /// </summary>
        public long LastLifeMagicTick { get; set; }

        /// <summary>
        /// 当前所在的格子的X坐标
        /// </summary>
        public Point CurrentGrid
        {
            get
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.MapCode];
                return new Point((int)(this.PosX / gameMap.MapGridWidth), (int)(this.PosY / gameMap.MapGridHeight));
            }

            set
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.MapCode];
                this.PosX = (int)(value.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2);
                this.PosY = (int)(value.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
            }
        }

        /// <summary>
        /// 当前所在的像素坐标
        /// </summary>
        public Point CurrentPos
        {
            get
            {
                return new Point(this.PosX, this.PosY);
            }

            set
            {
                this.PosX = (int)value.X;
                this.PosY = (int)value.Y;
            }
        }

        /// <summary>
        /// 当前所在的地图的编号
        /// </summary>
        public int CurrentMapCode
        {
            get
            {
                return this.MapCode;
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

        #endregion 实现IObject接口方法

        #region 记忆攻击军旗的血量

        /// <summary>
        /// 上次记录别人攻击自己的时间
        /// </summary>
        private long _LastLogAttackerTicks = 0;

        /// <summary>
        /// 检查判断攻击者列表是否有效，如果大于60秒没人攻击，则无效
        /// 无效则清空列表
        /// </summary>
        /// <returns></returns>
        private Boolean CheckAttackerListEfficiency()
        {
            //30秒 无人攻击 后 无效
            if (TimeUtil.NOW() - _LastLogAttackerTicks > 30000)
            {
                lock (_AttackerDict)
                {
                    _AttackerDict.Clear();
                    _AttackerTicksDict.Clear();
                }

                return false;
            }

            return true;
        }

        private Dictionary<int, int> _AttackerDict = new Dictionary<int, int>();
        private Dictionary<int, long> _AttackerTicksDict = new Dictionary<int, long>();

        /// <summary>
        /// 增加攻击者ID
        /// </summary>
        /// <param name="roleID"></param>
        public void AddAttacker(int roleID, int injured)
        {
            lock (_AttackerDict)
            {
                _LastLogAttackerTicks = TimeUtil.NOW();

                int oldInjured = 0;
                _AttackerDict.TryGetValue(roleID, out oldInjured);
                _AttackerDict[roleID] = oldInjured + injured;
                _AttackerTicksDict[roleID] = _LastLogAttackerTicks; //为每个角色也记忆下攻击时间
            }
        }

        /// <summary>
        /// 删除攻击者ID(谨慎调用，目前只为弓箭卫士，专用。其他boss，不要做类似操作，否则，记忆的血量就被清空了)
        /// </summary>
        /// <param name="roleID"></param>
        public void RemoveAttacker(int roleID)
        {
            lock (_AttackerDict)
            {
                _AttackerDict.Remove(roleID);
                _AttackerTicksDict.Remove(roleID);
            }
        }

        /// <summary>
        /// 判断roleid 对应的玩家是否攻击者
        /// </summary>
        /// <param name="roleID"></param>
        public bool IsAttackedBy(int roleID)
        {
            CheckAttackerListEfficiency();

            lock (_AttackerDict)
            {
                if (_AttackerDict.ContainsKey(roleID))
                {
                    return true;
                }
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

            int attacker = -1;
            int maxInjured = 0;
            long nowTicks = TimeUtil.NOW();
            lock (_AttackerDict)
            {
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
                        attacker = key;
                    }
                }
            }

            return attacker;
        }

        #endregion 记忆攻击军旗的血量

        #region 扩展接口

        public T GetExtComponent<T>(ExtComponentTypes type) where T : class
        {
            return default(T);
        }

        #endregion 扩展接口
    }
}
