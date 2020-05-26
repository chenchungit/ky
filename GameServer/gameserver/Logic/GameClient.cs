using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Server.Data;
using GameServer.Interface;
//using System.Windows.Threading;
using Server.Tools;
using Server.Protocol;
using GameServer.Server;

using GameServer.Logic.RefreshIconState;
using GameServer.Logic.NewBufferExt;
using Server.TCP;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 游戏客户端类
    /// </summary>
    public class GameClient : IObject
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public GameClient()
        {
        }

        /// <summary>
        /// 客户端的TMSKSocket连接
        /// </summary>
        public TMSKSocket ClientSocket
        {
            get;
            set;
        }

        /// <summary>
        /// 客户端的UserID(帐号，避免频繁查询)
        /// </summary>
        public string strUserID
        {
            get;
            set;
        }

        public string deviceID { get;set; }

        public bool IsYueYu = false;

        /// <summary>
        /// 客户端的UserName(用户名，避免频繁查询)
        /// </summary>
        public string strUserName;

        private SafeClientData _ClientData = null;

        /// <summary>
        /// 线程安全的客户端数据
        /// </summary>
        public SafeClientData ClientData
        {
            get { return _ClientData; }
            set
            {
                _ClientData = value;
                if (null != _ClientData)
                {
                    bufferPropsManager.Init(ClientData.PropsCacheManager);
                    _ClientData.ChangePosHandler += ChangeGrid;
                }
            }
        }

        /// <summary>
        /// 装备佩戴管理
        /// </summary>
        private UsingEquipManager _UsingEquipMgr = new UsingEquipManager();

        /// <summary>
        /// 图标上感叹号显示管理
        /// </summary>
        public IconStateManager _IconStateMgr = new IconStateManager();

        /// <summary>
        /// 装备佩戴管理
        /// </summary>
        public UsingEquipManager UsingEquipMgr
        {
            get { return _UsingEquipMgr; }
        }

        /// <summary>
        /// 是否已经退出了
        /// </summary>
        public bool LogoutState = false;

        /// <summary>
        /// 是否已调用过登出函数LogOut
        /// </summary>
        private bool _ClientLogOut = false;

        /// <summary>
        /// 是否第一次调用登出函数
        /// </summary>
        /// <returns></returns>
        public bool ClientLogOutOnce()
        {
            lock (this)
            {
                if (!_ClientLogOut)
                {
                    _ClientLogOut = true;
                    return true;
                }

                return false;
            }
        }
        /// <summary>
        /// 服务器ID
        /// </summary>
        public int ServerId
        {
            get { return ClientSocket.ServerId; }
        }

        public int[] AllyTip = new int[] { 0, 0 };

        #region 版本号

        /// <summary>
        /// 代码修订版本号
        /// </summary>
        public int CodeRevision = 0;

        /// <summary>
        /// App版本号
        /// </summary>
        public int MainExeVer = 0;

        /// <summary>
        /// 资源版本号
        /// </summary>
        public int ResVer = 0;

        #endregion 版本号

        #region 活动上下文数据

        /// <summary>
        /// 跨服上下文数据
        /// </summary>
        public object KuaFuContextData = null;

        /// <summary>
        /// 活动场景上下文数据
        /// </summary>
        public object SceneContextData = null;

        /// <summary>
        /// 活动场景上下文数据2
        /// </summary>
        public object SceneContextData2 = null;

        /// <summary>
        /// 场景类型
        /// </summary>
        public int SceneType;

        /// <summary>
        /// 场景对象
        /// </summary>
        public object SceneObject;

        /// <summary>
        /// 场景配置对象
        /// </summary>
        public object SceneInfoObject;

        /// <summary>
        /// 唯一场次ID
        /// </summary>
        public long SceneGameId;

        #endregion 活动上下文数据

        #region 防外挂校验
        public CheckCheat CheckCheatData = new CheckCheat();
        public InterestingData InterestingData = new InterestingData();
        #endregion 防外挂校验

        #region 修订版本号日志

        /*
         *  1.军衔1级时给予的军衔Buff1等级为2级的问题
         *  2.开格子消耗钻石数,按客户端显示数字,服务器验证,如果数值不符(客户端显示错误或外挂操作),禁止操作
         */

        #endregion

        #region 实现IObject接口方法

        /// <summary>
        /// 对象的类型
        /// </summary>
        public ObjectTypes ObjectType
        {
            get { return ObjectTypes.OT_CLIENT; }
        }

        /// <summary>
        /// 精灵Buffer
        /// </summary>
        private SpriteBuffer _RoleBuffer = new SpriteBuffer();

        /// <summary>
        /// 精灵Buffer
        /// </summary>
        public SpriteBuffer RoleBuffer
        {
            get { return _RoleBuffer; }
            set { _RoleBuffer = value; }
        }

        /// <summary>
        /// 精灵一次性Buffer
        /// </summary>
        private SpriteOnceBuffer _RoleOnceBuffer = new SpriteOnceBuffer();

        /// <summary>
        /// 精灵一次性Buffer
        /// </summary>
        public SpriteOnceBuffer RoleOnceBuffer
        {
            get { return _RoleOnceBuffer; }
            set { _RoleOnceBuffer = value; }
        }

        /// <summary>
        /// 技能辅助选项
        /// </summary>
        private SpriteMagicHelper _RoleMagicHelper = new SpriteMagicHelper();

        /// <summary>
        /// 技能辅助选项
        /// </summary>
        public SpriteMagicHelper RoleMagicHelper
        {
            get { return _RoleMagicHelper; }
            set { _RoleMagicHelper = value; }
        }

        /// <summary>
        /// Buffer乘以系数属性项
        /// </summary>
        private SpriteMultipliedBuffer _RoleMultipliedBuffer = new SpriteMultipliedBuffer();

        /// <summary>
        /// Buffer乘以系数属性项
        /// </summary>
        public SpriteMultipliedBuffer RoleMultipliedBuffer
        {
            get { return _RoleMultipliedBuffer; }
            set { _RoleMultipliedBuffer = value; }
        }

        /// <summary>
        /// Buffer乘以系数属性项(为全套装备品质、锻造级别、宝石级别所用)
        /// </summary>
        private SpriteMultipliedBuffer _AllThingsMultipliedBuffer = new SpriteMultipliedBuffer();

        /// <summary>
        /// Buffer乘以系数属性项(为全套装备品质、锻造级别、宝石级别所用)
        /// </summary>
        public SpriteMultipliedBuffer AllThingsMultipliedBuffer
        {
            get { return _AllThingsMultipliedBuffer; }
            set { _AllThingsMultipliedBuffer = value; }
        }

        /// <summary>
        /// 获取ID
        /// </summary>
        /// <returns></returns>
        public int GetObjectID()
        {
            return ClientData.RoleID;
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
        /// 最后一次检查gmail的时间
        /// </summary>
        private long _LastCheckGMailTick = TimeUtil.NOW();

        /// <summary>
        /// 最后一次检查gmail的时间
        /// </summary>
        public long LastCheckGMailTick
        {
            get { return _LastCheckGMailTick; }
            set { _LastCheckGMailTick = value; }
        }

        /// <summary>
        /// 当前所在的格子的X坐标
        /// </summary>
        public Point CurrentGrid
        {
            get
            {
                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(this.ClientData.MapCode, out gameMap))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("GameClient CurrentGrid Error MapCode={0}", this.ClientData.MapCode));
                    return new Point(0, 0);
                }

                return new Point((int)(this.ClientData.PosX / gameMap.MapGridWidth), (int)(this.ClientData.PosY / gameMap.MapGridHeight));
            }

            set
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.ClientData.MapCode];
                this.ClientData.PosX = (int)(value.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2);
                this.ClientData.PosY = (int)(value.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
            }
        }

        /// <summary>
        /// 当前所在的像素坐标
        /// </summary>
        public Point CurrentPos
        {
            get
            {
                return new Point(this.ClientData.PosX, this.ClientData.PosY);
            }

            set
            {
                this.ClientData.PosX = (int)value.X;
                this.ClientData.PosY = (int)value.Y;
            }
        }

        /// <summary>
        /// 当前所在的地图的编号
        /// </summary>
        public int CurrentMapCode
        {
            get
            {
                return this.ClientData.MapCode;
            }
        }

        /// <summary>
        /// 当前所在的副本地图的ID
        /// </summary>
        public int CurrentCopyMapID
        {
            get
            {
                return this.ClientData.CopyMapID;
            }
        }

        /// <summary>
        /// 当前的方向
        /// </summary>
        public Dircetions CurrentDir
        {
            get
            {
                return (Dircetions)this.ClientData.RoleDirection;
            }

            set
            {
                this.ClientData.RoleDirection = (int)value;
            }
        }

        #endregion 实现IObject接口方法

        #region 移动处理

        /// <summary>
        /// 旧的格子坐标
        /// </summary>
        private Point _OldGridPoint = new Point(-1, -1);

        /// <summary>
        /// 旧的区域ID
        /// </summary>
        private int _OldAreaLuaID = -1;

        /// <summary>
        /// 清空区域变化
        /// </summary>
        public void ClearChangeGrid()
        {
            _OldAreaLuaID = -1;
        }

        /// <summary>
        /// 格子发生了变化
        /// </summary>
        public void ChangeGrid()
        {
            //角色死亡，并且登出的时候，会被配置成 -1
            if (_ClientData.MapCode < 0)
            {
                return;
            }

            GameMap gameMap = GameManager.MapMgr.DictMaps[_ClientData.MapCode];
            int newGridX = _ClientData.PosX / gameMap.MapGridWidth;
            int newGridY = _ClientData.PosY / gameMap.MapGridHeight;
            if (_OldGridPoint.X != newGridX || _OldGridPoint.Y != newGridY)
            {
                _OldGridPoint = new Point(newGridX, newGridY);

                //触发动作
                int areaLuaID = gameMap.GetAreaLuaID(_OldGridPoint);
                if (areaLuaID != _OldAreaLuaID)
                {
                    if (_OldAreaLuaID > 0)
                    {
                        //执行区域脚本
                        RunAreaLuaFile(gameMap, _OldAreaLuaID, "leaveArea");
                    }

                    _OldAreaLuaID = areaLuaID;
                    if (areaLuaID > 0)
                    {
                        //执行区域脚本
                        RunAreaLuaFile(gameMap, areaLuaID, "enterArea");
                    }
                }

                //在这里移动中拾取物品?
                //安全区不进行物品拾取检测 ChenXiaojun
                if (!gameMap.InSafeRegionList(CurrentGrid))
                {
                    GameManager.GoodsPackMgr.ProcessClickGoodsPackWhenMovingToOtherGrid(this);
                }
            }
        }

        /// <summary>
        /// 上次自动拾取的时间
        /// </summary>
        private long lastAutoGetthingsTicks = 0;

        /// <summary>
        /// 每隔20秒自动失去一次周围物品,范围为7个格子
        /// </summary>
        /// <param name="ticks"></param>
        public void AutoGetThingsOnAutoFight(long ticks)
        {
            if (ticks - lastAutoGetthingsTicks < 20000)
            {
                return;
            }
            lastAutoGetthingsTicks = ticks;
            if (ClientData.AutoFighting && ClientData.AutoFightGetThings != 0)
            {
                //安全区不进行物品拾取检测 ChenXiaojun
                GameMap gameMap = GameManager.MapMgr.DictMaps[_ClientData.MapCode];
                if (!gameMap.InSafeRegionList(CurrentGrid))
                {
                    GameManager.GoodsPackMgr.ProcessClickGoodsPackWhenMovingToOtherGrid(this, 5);
                }
            }
        }

        /// <summary>
        /// 执行区域脚本
        /// </summary>
        /// <param name="areaLuaID"></param>
        /// <param name="functionName"></param>
        private void RunAreaLuaFile(GameMap gameMap, int areaLuaID, string functionName)
        {
            GAreaLua areaLua = gameMap.GetAreaLuaByID(areaLuaID);
            if (null == areaLua)
            {
                return;
            }

            string fileName = areaLua.LuaScriptFileName;
            if (!string.IsNullOrEmpty(fileName))
            {
                /*string luaFileName = Global.GetAreaLuaScriptFile(fileName);
                if (!string.IsNullOrEmpty(luaFileName))
                {
                    ////执行对话脚本
                    Global.ExcuteLuaFunction(this, luaFileName, functionName, null, null);
                }*/

                ProcessAreaScripts.ProcessScripts(this, fileName, functionName, areaLuaID);
            }
        }

        #endregion 移动处理

        #region 清空可见对象列表

        /// <summary>
        /// 清空角色的可见列表
        /// </summary>
        /// <param name="recalcMonsterVisibleNum"></param>
        public void ClearVisibleObjects(bool recalcMonsterVisibleNum)
        {
            lock (ClientData.VisibleGrid9Objects)
            {
                if (recalcMonsterVisibleNum)
                {
                    List<Object> keysList = ClientData.VisibleGrid9Objects.Keys.ToList<Object>();
                    for (int i = 0; i < keysList.Count; i++)
                    {
                        Object key = keysList[i];
                        if (key is Monster)
                        {
                            if ((key as Monster).CurrentCopyMapID == ClientData.CopyMapID)
                            {
                                (key as Monster).VisibleClientsNum--;
                            }
                        }
                    }
                }

                ClientData.VisibleGrid9Objects.Clear();
                ClientData.VisibleMeGrid9GameClients.Clear();
            }
        }


        #endregion 清空可见对象列表

        #region 后台处理管理

        /// <summary>
        /// 后台是否正在处理中
        /// </summary>
        public int BackgroundHandling = 0;

        #endregion 后台处理管理

        #region 九宫格刷新管理

        /// <summary>
        /// 后台是否正在处理中
        /// </summary>
        public object Current9GridMutex = new object();

        /// <summary>
        /// 上次更新9宫格的时间
        /// </summary>
        public long LastRefresh9GridObjectsTicks = 0;

        /// <summary>
        /// 客户端的效果隐藏选项
        /// </summary>
        public int ClientEffectHideFlag1 = 0;

        #endregion 九宫格刷新管理

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
                    if (ticks - _LastCachingBytesDataTicks < GameManager.MaxCachingClientToClientBytesDataTicks)
                    {
                        //System.Diagnostics.Debug.WriteLine(string.Format("从角色缓存中读取数据"));
                        return _CachingBytesData;
                    }
                }

                _LastCachingBytesDataTicks = ticks;
                RoleData roleData = Global.ClientDataToRoleData2(this.ClientData);
                _CachingBytesData = DataHelper.ObjectToBytes<RoleData>(roleData);
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
                    if (bForce || ticks - _LastCachingBytesDataTicks >= GameManager.MaxCachingClientToClientBytesDataTicks)
                    {
                        //System.Diagnostics.Debug.WriteLine(string.Format("将角色缓存释放"));
                        _CachingBytesData = null;
                    }
                }
            }
        }

        /// <summary>
        /// 从缓存中获取角色对象
        /// </summary>
        /// <returns></returns>
        public TCPOutPacket GetTCPOutPacketFromCaching(int cmdID)
        {
            byte[] bytesCmd = this.GetBytesDataFromCaching();
            return TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, bytesCmd, 0, bytesCmd.Length, cmdID);
        }
        */
        #endregion 

        #region 指令发送

        /// <summary>
        /// 向“自己”发送指令
        /// </summary>
        public void sendCmd(int cmdId, string cmdData) 
        {
            TCPManager.getInstance().MySocketListener.SendData(ClientSocket, TCPOutPacket.MakeTCPOutPacket(TCPOutPacketPool.getInstance(), cmdData, cmdId));
        }

        /// <summary>
        /// 附近的角色发送指令(包括自己)
        /// </summary>
        public void sendOthersCmd(int cmdId, string cmdData)
        {
            List<Object> objsList = Global.GetAll9Clients(this);
            if (null == objsList) return;

            //群发消息
            GameManager.ClientMgr.SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, cmdData, cmdId);
        }

        /// <summary>
        /// 向“自己”发送指令
        /// </summary>
        public void sendCmd<T>(int cmdId, T cmdData)
        {
            TCPManager.getInstance().MySocketListener.SendData(ClientSocket, DataHelper.ObjectToTCPOutPacket<T>(cmdData, TCPOutPacketPool.getInstance(), cmdId));
        }
        public void sendProtocolCmd<T>(int cmdId, T cmdData)
        {
            TCPManager.getInstance().MySocketListener.SendData(ClientSocket, DataHelper.ProtocolToTCPOutPacket<T>(cmdData, TCPOutPacketPool.getInstance(), cmdId));
        }

        public void sendCmd(TCPOutPacket cmdData, bool pushBack = true)
        {
            TCPManager.getInstance().MySocketListener.SendData(ClientSocket, cmdData, pushBack);
        }

        public void PushVersion(string MainExeVer = "", string ResVer = "")
        {
            sendCmd((int)TCPGameServerCmds.CMD_SPR_PUSH_VERSION, string.Format("{0}:{1}:{2}", CodeRevision, MainExeVer, ResVer));
        }

        #endregion 指令发送

        #region 伤害累计计算

        /// <summary>
        /// 累积输出伤害
        /// </summary>
        public long SumDamageForCopyTeam = 0;
        
        #endregion 伤害累计计算

        #region 执行地图初始化lua脚本

        /// <summary>
        /// 执行进入地图的lua脚本
        /// </summary>
        /// <param name="mapCode"></param>
        public void ExecuteEnterMap(int mapCode)
        {
            //角色死亡，并且登出的时候，会被配置成 -1
            if (_ClientData.MapCode < 0 || _ClientData.CopyMapID < 0)
            {
                return;
            }

            MapTypes mapType = Global.GetMapType(mapCode);
            if (mapType >= MapTypes.NormalCopy && mapType <= MapTypes.MarriageCopy)
            {
                /// 获取副本地图对象
                CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(_ClientData.CopyMapID);
                if (null == copyMap) return;

                //重发AI相关的指令队列
                lock (copyMap.EventQueue)
                {
                    foreach (var e in copyMap.EventQueue)
                    {
                        int guangMuID = e.GuangMuID;
                        int show = e.Show;
                        sendCmd((int)TCPGameServerCmds.CMD_SPR_MAPAIEVENT, string.Format("{0}:{1}", guangMuID, show));
                    }
                }

                if (copyMap.ExecEnterMapLuaFile)
                {
                    return;
                }

                copyMap.ExecEnterMapLuaFile = true;

                GameMap gameMap = GameManager.MapMgr.DictMaps[_ClientData.MapCode];
                if (!string.IsNullOrEmpty(gameMap.EnterMapLuaFile))
                {
                    //执行对话脚本
                    Global.ExcuteLuaFunction(this, gameMap.EnterMapLuaFile, "comeOn", null, null);
                }

            }
        }

        #endregion 执行地图初始化lua脚本

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

        /// <summary>
        /// buff属性
        /// </summary>
        public BufferPropsModule bufferPropsManager = new BufferPropsModule();

        /// <summary>
        /// 被动技能
        /// </summary>
        public PassiveSkillModule passiveSkillModule = new PassiveSkillModule();

        /// <summary>
        /// 执行分段攻击的技能执行队列
        /// </summary>
        public MagicsManyTimeDmageQueue MyMagicsManyTimeDmageQueue = new MagicsManyTimeDmageQueue();

        /// <summary>
        /// 属性计算缓存
        /// </summary>
        public PropsCacheModule propsCacheModule = new PropsCacheModule();

        /// <summary>
        /// 需要延迟执行的过程
        /// </summary>
        public DelayExecModule delayExecModule = new DelayExecModule();

        /// <summary>
        /// 最近一次是否掉落在障碍中的判断时间
        /// </summary>
        public long LastInObsJugeTicks
        {
            get;
            set;
        }

        #endregion 扩展接口

        #region 新的扩展buffer实现

        /// <summary>
        /// 新的buffer扩展管理
        /// </summary>
        public BufferExtManager MyBufferExtManager = new BufferExtManager();

        #endregion 新的扩展buffer实现
    }
}
