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

namespace GameServer.Logic
{
    /// <summary>
    /// 假人数据项
    /// </summary>
    public class FakeRoleItem : IObject
    {
        #region 线程安全

        /// <summary>
        /// 线程锁
        /// </summary>
        private Object Mutex = new object();

        #endregion 线程安全

        #region 基础值

        /// <summary>
        /// 假人数据
        /// </summary>
        private FakeRoleData _MyFakeRoleData = new FakeRoleData();

        /// <summary>
        /// 获取假人数据(禁止直接修改数据)
        /// </summary>
        public FakeRoleData GetFakeRoleData()
        {
            return _MyFakeRoleData;
        }

        /// <summary>
        /// 假人的ID
        /// </summary>
        public int FakeRoleID
        {
            get { lock (Mutex) { return _MyFakeRoleData.FakeRoleID; } }
            set { lock (Mutex) { _MyFakeRoleData.FakeRoleID = value; } }
        }

        /// <summary>
        /// 假人的类型
        /// </summary>
        public int FakeRoleType
        {
            get { lock (Mutex) { return _MyFakeRoleData.FakeRoleType; } }
            set { lock (Mutex) { _MyFakeRoleData.FakeRoleType = value; } }
        }

        /// <summary>
        /// 对应的扩展ID
        /// </summary>
        public int ToExtensionID
        {
            get { lock (Mutex) { return _MyFakeRoleData.ToExtensionID; } }
            set { lock (Mutex) { _MyFakeRoleData.ToExtensionID = value; } }
        }

        /// <summary>
        /// 假人对应的mini角色数据
        /// </summary>
        public RoleDataMini MyRoleDataMini
        {
            get { lock (Mutex) { return _MyFakeRoleData.MyRoleDataMini; } }
            set { lock (Mutex) { _MyFakeRoleData.MyRoleDataMini = value; } }
        }

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

        /// <summary>
        /// 当前的生命值
        /// </summary>
        public int CurrentLifeV
        {
            get { lock (this) return _MyFakeRoleData.MyRoleDataMini.LifeV; }
            set { lock (this) _MyFakeRoleData.MyRoleDataMini.LifeV = value; }
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
        /// 假人的死亡时间
        /// </summary>
        private long _FakeRoleDeadTicks = 0;

        /// <summary>
        /// 假人的死亡时间
        /// </summary>
        public long FakeRoleDeadTicks
        {
            get { lock (Mutex) return _FakeRoleDeadTicks; }
            set { lock (Mutex) _FakeRoleDeadTicks = value; }
        }

        #endregion 额外添加值

        #region 实现IObject接口方法

        /// <summary>
        /// 对象的类型
        /// </summary>
        public ObjectTypes ObjectType
        {
            get { return ObjectTypes.OT_FAKEROLE; }
        }

        /// <summary>
        /// 获取ID
        /// </summary>
        /// <returns></returns>
        public int GetObjectID()
        {
            return FakeRoleID;
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
                GameMap gameMap = GameManager.MapMgr.DictMaps[MyRoleDataMini.MapCode];
                return new Point((int)(MyRoleDataMini.PosX / gameMap.MapGridWidth), (int)(MyRoleDataMini.PosY / gameMap.MapGridHeight));
            }

            set
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[MyRoleDataMini.MapCode];
                MyRoleDataMini.PosX = (int)(value.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2);
                MyRoleDataMini.PosY = (int)(value.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
            }
        }

        /// <summary>
        /// 当前所在的像素坐标
        /// </summary>
        public Point CurrentPos
        {
            get
            {
                return new Point(MyRoleDataMini.PosX, MyRoleDataMini.PosY);
            }

            set
            {
                MyRoleDataMini.PosX = (int)value.X;
                MyRoleDataMini.PosY = (int)value.Y;
            }
        }

        /// <summary>
        /// 当前所在的地图的编号
        /// </summary>
        public int CurrentMapCode
        {
            get
            {
                return MyRoleDataMini.MapCode;
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
                return (Dircetions)MyRoleDataMini.RoleDirection;
            }

            set
            {
                MyRoleDataMini.RoleDirection = (int)value;
            }
        }

        #region 扩展接口

        public T GetExtComponent<T>(ExtComponentTypes type) where T : class
        {
            return default(T);
        }

        #endregion 扩展接口

        #endregion 实现IObject接口方法

        #region 记忆攻击假人的血量

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

        #endregion 记忆攻击假人的血量
    }
}
