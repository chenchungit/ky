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
    /// 镖车的数据
    /// </summary>
    public class BiaoCheItem : IObject
    {
        #region 线程安全

        /// <summary>
        /// 线程锁
        /// </summary>
        private Object Mutex = new object();

        #endregion 线程安全

        #region 基础值

        /// <summary>
        /// 镖车数据
        /// </summary>
        private BiaoCheData _MyBiaoCheData = new BiaoCheData();

        /// <summary>
        /// 获取镖车数据(禁止直接修改数据)
        /// </summary>
        public BiaoCheData GetBiaoCheData()
        {
            return _MyBiaoCheData;
        }

        /// <summary>
        /// 拥有者的RoleID
        /// </summary>
        public int OwnerRoleID
        {
            get { lock (Mutex) { return _MyBiaoCheData.OwnerRoleID; } }
            set { lock (Mutex) { _MyBiaoCheData.OwnerRoleID = value; } }
        }

        /// <summary>
        /// 镖车的ID
        /// </summary>
        public int BiaoCheID
        {
            get { lock (Mutex) { return _MyBiaoCheData.BiaoCheID; } }
            set { lock (Mutex) { _MyBiaoCheData.BiaoCheID = value; } }
        }

        /// <summary>
        /// 镖车的名称
        /// </summary>
        public string BiaoCheName
        {
            get { lock (Mutex) { return _MyBiaoCheData.BiaoCheName; } }
            set { lock (Mutex) { _MyBiaoCheData.BiaoCheName = value; } }
        }

        /// <summary>
        /// 押镖的类型
        /// </summary>
        public int YaBiaoID
        {
            get { lock (Mutex) { return _MyBiaoCheData.YaBiaoID; } }
            set { lock (Mutex) { _MyBiaoCheData.YaBiaoID = value; } }
        }

        /// <summary>
        /// 所在的地图的编号
        /// </summary>
        public int MapCode
        {
            get { lock (Mutex) { return _MyBiaoCheData.MapCode; } }
            set { lock (Mutex) { _MyBiaoCheData.MapCode = value; } }
        }

        /// <summary>
        /// 当前所在的位置X坐标
        /// </summary>
        public int PosX
        {
            get { lock (Mutex) { return _MyBiaoCheData.PosX; } }
            set { lock (Mutex) { _MyBiaoCheData.PosX = value; } }
        }

        /// <summary>
        /// 当前所在的位置Y坐标
        /// </summary>
        public int PosY
        {
            get { lock (Mutex) { return _MyBiaoCheData.PosY; } }
            set { lock (Mutex) { _MyBiaoCheData.PosY = value; } }
        }

        /// <summary>
        /// 当前的方向
        /// </summary>
        public int Direction
        {
            get { lock (Mutex) { return _MyBiaoCheData.Direction; } }
            set { lock (Mutex) { _MyBiaoCheData.Direction = value; } }
        }

        /// <summary>
        /// 当前的生命值
        /// </summary>
        public int LifeV
        {
            get { lock (Mutex) return _MyBiaoCheData.LifeV; }
            set { lock (Mutex) _MyBiaoCheData.LifeV = value; }
        }

        /// <summary>
        /// 每一刀伤害的生命值
        /// </summary>
        public int CutLifeV
        {
            get { lock (Mutex) return _MyBiaoCheData.CutLifeV; }
            set { lock (Mutex) _MyBiaoCheData.CutLifeV = value; }
        }

        /// <summary>
        /// 开始的时间
        /// </summary>
        public long StartTime
        {
            get { lock (Mutex) return _MyBiaoCheData.StartTime; }
            set { lock (Mutex) _MyBiaoCheData.StartTime = value; }
        }

        /// <summary>
        /// 形象编号
        /// </summary>
        public int BodyCode
        {
            get { lock (Mutex) return _MyBiaoCheData.BodyCode; }
            set { lock (Mutex) _MyBiaoCheData.BodyCode = value; }
        }

        /// <summary>
        /// 头像编号
        /// </summary>
        public int PicCode
        {
            get { lock (Mutex) return _MyBiaoCheData.PicCode; }
            set { lock (Mutex) _MyBiaoCheData.PicCode = value; }
        }

        /// <summary>
        /// 拥有者的RoleName
        /// </summary>
        public string OwnerRoleName
        {
            get { lock (Mutex) { return _MyBiaoCheData.OwnerRoleName; } }
            set { lock (Mutex) { _MyBiaoCheData.OwnerRoleName = value; } }
        }

        #endregion 基础值

        #region 额外添加值

        /// <summary>
        /// 汇报镖车的坐标的客户端时间
        /// </summary>
        private long _ReportPosTicks = 0;

        /// <summary>
        /// 汇报角色的坐标的客户端时间
        /// </summary>
        public long ReportPosTicks
        {
            get { lock (Mutex) return _ReportPosTicks; }
            set { lock (Mutex) _ReportPosTicks = value; }
        }

        /// <summary>
        /// 当前正在做的动作
        /// </summary>
        private int _CurrentAction = 0;

        /// <summary>
        /// 当前正在做的动作
        /// </summary>
        public int CurrentAction
        {
            get { lock (Mutex) return _CurrentAction; }
            set
            {
                lock (Mutex)
                {
                    if (_CurrentAction != value)
                    {
                        _CurrentAction = value;
                    }
                }
            }
        }

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
        /// 镖车的死亡时间
        /// </summary>
        private long _BiaoCheDeadTicks = 0;

        /// <summary>
        /// 镖车的死亡时间
        /// </summary>
        public long BiaoCheDeadTicks
        {
            get { lock (Mutex) return _BiaoCheDeadTicks; }
            set { lock (Mutex) _BiaoCheDeadTicks = value; }
        }

        /// <summary>
        /// 接镖的NPCID
        /// </summary>
        private int _DestNPC = 0;

        /// <summary>
        /// 接镖的NPCID
        /// </summary>
        public int DestNPC
        {
            get { lock (Mutex) return _DestNPC; }
            set { lock (Mutex) _DestNPC = value; }
        }

        /// <summary>
        /// 镖车的最低级别
        /// </summary>
        private int _MinLevel = 0;

        /// <summary>
        /// 镖车的最低级别
        /// </summary>
        public int MinLevel
        {
            get { lock (Mutex) return _MinLevel; }
            set { lock (Mutex) _MinLevel = value; }
        }

        /// <summary>
        /// 镖车的最高级别
        /// </summary>
        private int _MaxLevel = 0;

        /// <summary>
        /// 镖车的最高级别
        /// </summary>
        public int MaxLevel
        {
            get { lock (Mutex) return _MaxLevel; }
            set { lock (Mutex) _MaxLevel = value; }
        }

        #endregion 额外添加值

        #region 实现IObject接口方法

        /// <summary>
        /// 对象的类型
        /// </summary>
        public ObjectTypes ObjectType
        {
            get { return ObjectTypes.OT_BIAOCHE; }
        }

        /// <summary>
        /// 获取ID
        /// </summary>
        /// <returns></returns>
        public int GetObjectID()
        {
            return BiaoCheID;
        }

        /// <summary>
        /// 最后一次补血补魔的时间
        /// </summary>
        private long _LastLifeMagicTick = TimeUtil.NOW();

        /// <summary>
        /// 最后一次补血补魔的时间
        /// </summary>
        public long LastLifeMagicTick
        {
            get { return _LastLifeMagicTick; }
            set { _LastLifeMagicTick = value; }
        }

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

        #region 扩展接口

        public T GetExtComponent<T>(ExtComponentTypes type) where T : class
        {
            return default(T);
        }

        #endregion 扩展接口

        #endregion 实现IObject接口方法
    }
}
