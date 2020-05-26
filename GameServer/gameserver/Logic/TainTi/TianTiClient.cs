using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KF.Contract.Interface;
using KF.Contract.Data;
using Tmsk.Contract;
using System.Globalization;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using KF.Contract;
using System.Diagnostics;
using System.Configuration;
using Tmsk.Tools;
using Tmsk.Contract.Const;
using Server.Tools;

namespace KF.Client
{
    public class TianTiClient : MarshalByRefObject, IKuaFuClient, IManager2
    {
        #region 标准接口

        private static TianTiClient instance = new TianTiClient();

        public static TianTiClient getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            return true;
        }

        public bool initialize(ICoreInterface coreInterface)
        {
            CoreInterface = coreInterface;
            ClientInfo.ServerId = CoreInterface.GetLocalServerId();
            ClientInfo.GameType = (int)GameTypes.TianTi;
            return true;
        }

        public bool startup()
        {
            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            return true;
        }

        #endregion 标准接口

        #region 运行时成员

        /// <summary>
        /// lock对象
        /// </summary>
        object Mutex = new object();

        object RemotingMutex = new object();

        /// <summary>
        /// 游戏管理接口
        /// </summary>
        ICoreInterface CoreInterface = null;

        /// <summary>
        /// 跨服中心服务对象
        /// </summary>
        ITianTiService KuaFuService = null;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool ClientInitialized = false;

        /// <summary>
        /// 本地服务器信息
        /// </summary>
        private KuaFuClientContext ClientInfo = new KuaFuClientContext();

        /// <summary>
        /// 特殊场景类型
        /// </summary>
        public int SceneType = (int)SceneUIClasses.TianTi;

        /// <summary>
        /// 当前并发数
        /// </summary>
        private int CurrentRequestCount = 0;

        /// <summary>
        /// 最大并发数
        /// </summary>
        private int MaxRequestCount = 50;

        /// <summary>
        /// 角色ID到跨服角色信息的字典
        /// </summary>
        Dictionary<int, KuaFuRoleData> RoleId2RoleDataDict = new Dictionary<int, KuaFuRoleData>();

        /// <summary>
        /// 角色ID到跨服状态的缓存字典
        /// </summary>
        Dictionary<int, int> RoleId2KuaFuStateDict = new Dictionary<int, int>();

        /// <summary>
        /// 获取的服务器信息的年龄
        /// </summary>
        private int ServerInfoAsyncAge = 0;

        /// <summary>
        /// 服务器信息
        /// </summary>
        Dictionary<int, KuaFuServerInfo> ServerIdServerInfoDict = new Dictionary<int, KuaFuServerInfo>();

        /// <summary>
        /// 排名信息
        /// </summary>
        private TianTiRankData RankData = new TianTiRankData();

        /// <summary>
        /// 服务器标志:0 仅区服务器,1 跨服服务器,2 区服务器,3 前两者均可
        /// </summary>
        private int LocalServerFlags = 0; //默认0

        /// <summary>
        /// 服务地址
        /// </summary>
        private string RemoteServiceUri = null;

        #endregion 运行时成员

        #region 内部函数

        public bool LocalLogin(string userId)
        {
            if (LocalServerFlags == ServerFlags.NormalServerOnly)
            {
                return true;
            }

            if ((ServerFlags.NormalServer & LocalServerFlags) != 0)
            {
                return true;
            }

            return true;
        }

        public bool CanKuaFuLogin()
        {
            if (LocalServerFlags == ServerFlags.NormalServerOnly)
            {
                return false;
            }

            if ((ServerFlags.KuaFuServer & LocalServerFlags) != 0)
            {
                return true;
            }

            return false;
        }

        public void ExecuteEventCallBackAsync(object state)
        {
            AsyncDataItem[] items = state as AsyncDataItem[];
            if (null != items && items.Length > 0)
            {
                foreach (var item in items)
                {
                    EventCallBackHandler((int)item.EventType, item.Args);
                }
            }
        }

        public void TimerProc(object sender, EventArgs e)
        {
            try
            {
                string tianTiUri = CoreInterface.GetRuntimeVariable(RuntimeVariableNames.TianTiUri, null);
                if (RemoteServiceUri != tianTiUri)
                {
                    RemoteServiceUri = tianTiUri;
                }

                ITianTiService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    if (ClientInfo.ClientId > 0)
                    {
                        List<KuaFuServerInfo> dict = kuaFuService.GetKuaFuServerInfoData(ServerInfoAsyncAge);
                        if (null != dict && dict.Count > 0)
                        {
                            lock (Mutex)
                            {
                                ServerIdServerInfoDict.Clear();
                                bool first = true;
                                foreach (var item in dict)
                                {
                                    ServerIdServerInfoDict[item.ServerId] = item;
                                    if (first)
                                    {
                                        first = false;
                                        ServerInfoAsyncAge = item.Age;
                                    }
                                    if (ClientInfo.ServerId == item.ServerId)
                                    {
                                        LocalServerFlags = item.Flags;
                                    }
                                }
                            }
                        }

                        //同步数据
                        AsyncDataItem[] items = kuaFuService.GetClientCacheItems(ClientInfo.ServerId);
                        if (null != items && items.Length > 0)
                        {
                            //ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteEventCallBackAsync), items);
                            ExecuteEventCallBackAsync(items);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                ResetKuaFuService();
            }
        }

        private void ResetKuaFuService()
        {
            RemoteServiceUri = CoreInterface.GetRuntimeVariable("TianTiUri", null);
            lock (Mutex)
            {
                KuaFuService = null;
            }
        }

        private ITianTiService GetKuaFuService(bool noWait = false)
        {
            ITianTiService kuaFuService = null;
            int clientId = -1;

            try
            {
                lock (Mutex)
                {
                    if (string.IsNullOrEmpty(RemoteServiceUri))
                    {
                        return null;
                    }

                    if (null == KuaFuService && noWait)
                    {
                        return null;
                    }
                }

                lock (RemotingMutex)
                {
                    if (KuaFuService == null)
                    {
                        kuaFuService = (ITianTiService)Activator.GetObject(typeof(ITianTiService), RemoteServiceUri);
                        if (null == kuaFuService)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        kuaFuService = KuaFuService;
                    }

                    //KuaFuClientContext clientContext = CallContext.GetData("KuaFuClientContext") as KuaFuClientContext;
                    //if (null == clientContext)
                    //{
                    //    CallContext.SetData("KuaFuClientContext", new KuaFuClientContext() { ServerId = ClientInfo.ServerId, ClientId = ClientInfo.ClientId });
                    //}

                    clientId = kuaFuService.InitializeClient(this, ClientInfo);

                    if (null != kuaFuService && (clientId != ClientInfo.ClientId || KuaFuService != kuaFuService))
                    {
                        lock (Mutex)
                        {
                            KuaFuService = kuaFuService;
                            ClientInfo.ClientId = clientId;
                            return kuaFuService;
                        }
                    }

                    return KuaFuService;
                }
            }
            catch (System.Exception ex)
            {
                ResetKuaFuService();
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// 服务器通知添加一个角色信息
        /// </summary>
        /// <param name="kuaFuRoleData"></param>
        /// <returns></returns>
        public int UpdateRoleData(KuaFuRoleData kuaFuRoleData, int roleId = 0)
        {
            int result = (int)KuaFuRoleStates.None;
            if (kuaFuRoleData == null)
            {
                return result;
            }

            roleId = kuaFuRoleData.RoleId;
            lock (Mutex)
            {
                if (kuaFuRoleData.State == KuaFuRoleStates.None)
                {
                    RemoveRoleData(kuaFuRoleData.RoleId);
                    return (int)KuaFuRoleStates.None;
                }

                RoleId2RoleDataDict[roleId] = kuaFuRoleData;
                RoleId2KuaFuStateDict[roleId] = (int)kuaFuRoleData.State;
            }

            return result;
        }

        /// <summary>
        /// 角色状态修改
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="gameType"></param>
        /// <param name="groupIndex"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public int RoleChangeState(int serverId, int rid, int state)
        {
            int result = StdErrorCode.Error_Operation_Faild;

            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    result = kuaFuService.RoleChangeState(serverId, rid, state);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }

            return result;
        }

        /// <summary>
        /// 游戏副本状态变更
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="state"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public int GameFuBenChangeState(int gameId, GameFuBenState state, DateTime time)
        {
            int result = StdErrorCode.Error_Server_Busy;
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    result = kuaFuService.GameFuBenChangeState(gameId, state, time);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    result = StdErrorCode.Error_Server_Internal_Error;
                }
            }

            return result;
        }

        #endregion 内部函数

        #region 回调函数

        public int GetNewFuBenSeqId()
        {
            if (null != CoreInterface)
            {
                return CoreInterface.GetNewFuBenSeqId();
            }

            return StdErrorCode.Error_Operation_Faild;
        }

        public object GetDataFromClientServer(int dataType, params object[] args)
        {
            return null;
        }

        public void EventCallBackHandler(int eventType, params object[] args)
        {
            try
            {
                switch (eventType)
                {
                    case (int)KuaFuEventTypes.NotifyWaitingRoleCount:
                        {
                            if (args.Length == 2)
                            {
                                int rid = (int)args[0];
                                int count = (int)args[1];
                                CoreInterface.GetEventSourceInterface().fireEvent(new KuaFuFuBenRoleCountEvent(rid, count), SceneType);
                            }
                        }
                        break;
                    case (int)KuaFuEventTypes.RoleSignUp:
                    case (int)KuaFuEventTypes.RoleStateChange:
                        {
                            if (args.Length == 1)
                            {
                                KuaFuRoleData kuaFuRoleData = args[0] as KuaFuRoleData;
                                if (null != kuaFuRoleData)
                                {
                                    UpdateRoleData(kuaFuRoleData, kuaFuRoleData.RoleId);
                                }
                            }
                        }
                        break;
                    case (int)KuaFuEventTypes.UpdateAndNotifyEnterGame:
                        {
                            if (args.Length == 1)
                            {
                                KuaFuRoleData kuaFuRoleData = args[0] as KuaFuRoleData;
                                if (null != kuaFuRoleData)
                                {
                                    UpdateRoleData(kuaFuRoleData, kuaFuRoleData.RoleId);

                                    TianTiFuBenData TianTiFuBenData = GetKuaFuFuBenData(kuaFuRoleData.GameId);
                                    if (null != TianTiFuBenData && TianTiFuBenData.State == GameFuBenState.Start)
                                    {
                                        KuaFuServerLoginData kuaFuServerLoginData = new KuaFuServerLoginData()
                                        {
                                            RoleId = kuaFuRoleData.RoleId,
                                            GameType = kuaFuRoleData.GameType,
                                            GameId = kuaFuRoleData.GameId,
                                            EndTicks = kuaFuRoleData.StateEndTicks,
                                        };

                                        kuaFuServerLoginData.ServerId = ClientInfo.ServerId;
                                        lock (Mutex)
                                        {
                                            KuaFuServerInfo kuaFuServerInfo;
                                            if (ServerIdServerInfoDict.TryGetValue(TianTiFuBenData.ServerId, out kuaFuServerInfo))
                                            {
                                                kuaFuServerLoginData.ServerIp = kuaFuServerInfo.Ip;
                                                kuaFuServerLoginData.ServerPort = kuaFuServerInfo.Port;
                                            }
                                        }

                                        CoreInterface.GetEventSourceInterface().fireEvent(new KuaFuNotifyEnterGameEvent(kuaFuServerLoginData), SceneType);
                                    }
                                }
                            }
                        }
                        break;
                    case (int)KuaFuEventTypes.ZhengBaSupport:
                        {
                            ZhengBaSupportLogData data = args[0] as ZhengBaSupportLogData;
                            if (null != data && data.FromServerId != ClientInfo.ServerId)
                            {
                                CoreInterface.GetEventSourceInterface().fireEvent(new KFZhengBaSupportEvent(data), (int)SceneUIClasses.KFZhengBa);
                            }
                        }
                        break;
                    case (int)KuaFuEventTypes.ZhengBaPkLog:
                        {
                            if (args.Length == 1)
                            {
                                ZhengBaPkLogData log = args[0] as ZhengBaPkLogData;
                                if (log != null)
                                {
                                    CoreInterface.GetEventSourceInterface().fireEvent(new KFZhengBaPkLogEvent(log), (int)SceneUIClasses.KFZhengBa);
                                }
                            }
                        }
                        break;
                    case (int)KuaFuEventTypes.ZhengBaNtfEnter:
                        {
                            ZhengBaNtfEnterData data = args[0] as ZhengBaNtfEnterData;
                            lock (Mutex)
                            {
                                KuaFuServerInfo kuaFuServerInfo;
                                if (ServerIdServerInfoDict.TryGetValue(data.ToServerId, out kuaFuServerInfo))
                                {
                                    data.ToServerIp = kuaFuServerInfo.Ip;
                                    data.ToServerPort = kuaFuServerInfo.Port;
                                }
                                else
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("KuaFuEventTypes.ZhengBaNtfEnter not find kfserver={0}", data.ToServerId));
                                }
                            }
                            CoreInterface.GetEventSourceInterface().fireEvent(new KFZhengBaNtfEnterEvent(data), (int)SceneUIClasses.KFZhengBa);
                        }
                        break;
                    case (int)KuaFuEventTypes.ZhengBaMirrorFight:
                        {
                            ZhengBaMirrorFightData data = args[0] as ZhengBaMirrorFightData;
                            CoreInterface.GetEventSourceInterface().fireEvent(new KFZhengBaMirrorFightEvent(data), (int)SceneUIClasses.KFZhengBa);
                        }
                        break;
                    case (int)KuaFuEventTypes.ZhengBaButtetinJoin:
                        {
                            ZhengBaBulletinJoinData data = args[0] as ZhengBaBulletinJoinData;
                            CoreInterface.GetEventSourceInterface().fireEvent(new KFZhengBaBulletinJoinEvent(data), (int)SceneUIClasses.KFZhengBa);
                        }
                        break;
                    case (int)KuaFuEventTypes.CoupleArenaCanEnter:
                        {
                            CoreInterface.GetEventSourceInterface().fireEvent(
                                new CoupleArenaCanEnterEvent(args[0] as CoupleArenaCanEnterData), (int)SceneUIClasses.CoupleArena);
                        }
                        break;
                }
            }
            catch(Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }
        }

        public int OnRoleChangeState(int roleId, int state, int age)
        {
            lock (Mutex)
            {
                KuaFuRoleData kuaFuRoleData;
                if (!RoleId2RoleDataDict.TryGetValue(roleId, out kuaFuRoleData))
                {
                    return -1;
                }

                if (age > kuaFuRoleData.Age)
                {
                    kuaFuRoleData.State = (KuaFuRoleStates)state;
                }
            }

            return 0;
        }

        #endregion 回调函数

        #region 天梯系统

        /// <summary>
        /// 匹配报名
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleId"></param>
        /// <param name="zoneId"></param>
        /// <param name="gameType"></param>
        /// <param name="groupIndex"></param>
        /// <returns></returns>
        public int TianTiSignUp(string userId, int roleId, int zoneId, int gameType, int groupIndex, int zhanDouLi)
        {
            int result;
            if (string.IsNullOrEmpty(userId) || roleId <= 0)
            {
                return StdErrorCode.Error_Not_Exist;
            }

            userId = userId.ToUpper();
            int count = Interlocked.Increment(ref CurrentRequestCount);
            try
            {
                if (count < MaxRequestCount)
                {
                    lock (Mutex)
                    {
                        KuaFuRoleData kuaFuRoleData;
                        if (RoleId2RoleDataDict.TryGetValue(roleId, out kuaFuRoleData))
                        {
                            //如果服务器ID不同,表明是跨服登录角色,不应该在此报名
                            if (kuaFuRoleData.ServerId != ClientInfo.ServerId)
                            {
                                return StdErrorCode.Error_Operation_Faild;
                            }
                        }
                    }

                    ITianTiService kuaFuService = GetKuaFuService();
                    if (null != kuaFuService)
                    {
                        try
                        {
                            TianTiGameData TianTiGameData = new TianTiGameData(){ZhanDouLi = zhanDouLi};
                            result = kuaFuService.RoleSignUp(ClientInfo.ServerId, userId, zoneId, roleId, gameType, groupIndex, TianTiGameData);
                        }
                        catch (System.Exception ex)
                        {
                            ResetKuaFuService();
                        }
                    }
                    else
                    {
                        return StdErrorCode.Error_Server_Not_Registed;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }
            finally
            {
                Interlocked.Decrement(ref CurrentRequestCount);
            }

            return StdErrorCode.Error_Success;
        }

        public int ChangeRoleState(int roleId, KuaFuRoleStates state, bool noWait = false)
        {
            int result = StdErrorCode.Error_Operation_Faild;

            ITianTiService kuaFuService = null;
            KuaFuRoleData kuaFuRoleData = null;
            int serverId = ClientInfo.ServerId;
            lock (Mutex)
            {
                if (RoleId2RoleDataDict.TryGetValue(roleId, out kuaFuRoleData))
                {
                    serverId = kuaFuRoleData.ServerId;
                }
            }

            kuaFuService = GetKuaFuService(noWait);
            if (null != kuaFuService)
            {
                try
                {
                    result = kuaFuService.RoleChangeState(serverId, roleId, (int)state);
                    if (result >= 0)
                    {
                        lock (Mutex)
                        {
                            if (RoleId2RoleDataDict.TryGetValue(roleId, out kuaFuRoleData))
                            {
                                kuaFuRoleData.State = (KuaFuRoleStates)result;
                            }
                        }

                        if (null != kuaFuRoleData)
                        {
                            UpdateRoleData(kuaFuRoleData);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    result = StdErrorCode.Error_Server_Internal_Error;
                }
            }

            return result;
        }

        private TianTiFuBenData GetKuaFuFuBenData(int gameId)
        {
            TianTiFuBenData TianTiFuBenData = null;

            if (TianTiFuBenData == null)
            {
                ITianTiService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    try
                    {
                        TianTiFuBenData = (TianTiFuBenData)kuaFuService.GetFuBenData(gameId);
                    }
                    catch (System.Exception ex)
                    {
                        TianTiFuBenData = null;
                    }
                }
            }

            return TianTiFuBenData;
        }

        /// <summary>
        /// 从服务器获取
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public int GetRoleKuaFuFuBenRoleCount(int roleId)
        {
            int roleCount = 0;

            try
            {
                ITianTiService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    object result =kuaFuService.GetRoleExtendData(ClientInfo.ServerId, roleId, (int)KuaFuRoleExtendDataTypes.GameFuBenRoleCount);
                    if (null != result)
                    {
                        roleCount = (int)result;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return roleCount;
        }

        /// <summary>
        /// 改变角色的在某个游戏副本中的状态
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="state"></param>
        /// <param name="serverId">如果不知道,传0</param>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public int GameFuBenRoleChangeState(int roleId, int state, int serverId = 0, int gameId = 0)
        {
            try
            {
                ITianTiService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    if (serverId <= 0 || gameId <= 0)
                    {
                        KuaFuRoleData kuaFuRoleData;
                        if (!RoleId2RoleDataDict.TryGetValue(roleId, out kuaFuRoleData))
                        {
                            return (int)KuaFuRoleStates.None;
                        }

                        serverId = kuaFuRoleData.ServerId;
                        gameId = kuaFuRoleData.GameId;
                    }

                    return KuaFuService.GameFuBenRoleChangeState(serverId, roleId, gameId, state);
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return 0;
        }

        /// <summary>
        /// 移除指定ID的角色的缓存
        /// </summary>
        /// <param name="roleId"></param>
        public void RemoveRoleData(int roleId)
        {
            lock (Mutex)
            {
                RoleId2RoleDataDict.Remove(roleId);
                RoleId2KuaFuStateDict.Remove(roleId);
            }
        }

        /// <summary>
        /// 从服务器获取角色数据
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public KuaFuRoleData GetKuaFuRoleDataFromServer(int serverId, int roleId)
        {
            KuaFuRoleData kuaFuRoleData = null;
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    kuaFuRoleData = (KuaFuRoleData)kuaFuService.GetKuaFuRoleData(serverId, roleId);
                    UpdateRoleData(kuaFuRoleData); //更新
                }
                catch (System.Exception ex)
                {
                    kuaFuRoleData = null;
                }
            }

            return kuaFuRoleData;

        }

        /// <summary>
        /// 验证角色是否有本服务器的跨服活动
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="gameType"></param>
        /// <param name="gameId"></param>
        /// <returns>是否有效</returns>
        public bool KuaFuLogin(KuaFuServerLoginData kuaFuServerLoginData)
        {
            TianTiFuBenData TianTiFuBenData = GetKuaFuFuBenData((int)kuaFuServerLoginData.GameId);
            if (null != TianTiFuBenData && TianTiFuBenData.State < GameFuBenState.End)
            {
                if (TianTiFuBenData.ServerId == ClientInfo.ServerId)
                {
                    if (TianTiFuBenData.RoleDict.ContainsKey(kuaFuServerLoginData.RoleId))
                    {
                        KuaFuRoleData kuaFuRoleData = GetKuaFuRoleDataFromServer(kuaFuServerLoginData.ServerId, kuaFuServerLoginData.RoleId);
                        if (kuaFuRoleData.GameId == TianTiFuBenData.GameId)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 获取跨服登录的角色的原服务器的IP和服务端口
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="dbIp"></param>
        /// <param name="dbPort"></param>
        /// <param name="logIp"></param>
        /// <param name="logPort"></param>
        /// <returns></returns>
        public bool GetKuaFuDbServerInfo(int serverId, out string dbIp, out int dbPort, out string logIp, out int logPort)
        {
            KuaFuServerInfo kuaFuServerInfo;
            lock (Mutex)
            {
                if (ServerIdServerInfoDict.TryGetValue(serverId, out kuaFuServerInfo))
                {
                    dbIp = kuaFuServerInfo.DbIp;
                    dbPort = kuaFuServerInfo.DbPort;
                    logIp = kuaFuServerInfo.LogDbIp;
                    logPort = kuaFuServerInfo.LogDbPort;
                    return true;
                }
            }

            dbIp = null;
            dbPort = 0;
            logIp = null;
            logPort = 0;
            return false;
        }

        /// <summary>
        /// 获取跨服登录的角色的原服务器的IP和服务端口
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="dbIp"></param>
        /// <param name="dbPort"></param>
        /// <param name="logIp"></param>
        /// <param name="logPort"></param>
        /// <returns></returns>
        public bool GetKuaFuDbServerInfo(int serverId, out string dbIp, out int dbPort, out string logIp, out int logPort, out string gsIp, out int gsPort)
        {
            KuaFuServerInfo kuaFuServerInfo;
            lock (Mutex)
            {
                if (ServerIdServerInfoDict.TryGetValue(serverId, out kuaFuServerInfo))
                {
                    dbIp = kuaFuServerInfo.DbIp;
                    dbPort = kuaFuServerInfo.DbPort;
                    logIp = kuaFuServerInfo.LogDbIp;
                    logPort = kuaFuServerInfo.LogDbPort;
                    gsIp = kuaFuServerInfo.Ip;
                    gsPort = kuaFuServerInfo.Port;
                    return true;
                }
            }

            dbIp = null;
            dbPort = 0;
            logIp = null;
            logPort = 0;
            gsIp = null;
            gsPort = 0;
            return false;
        }

        /// <summary>
        /// 获取角色所属阵营
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public int GetRoleBattleWhichSide(int gameId, int roleId)
        {
            TianTiFuBenData TianTiFuBenData = GetKuaFuFuBenData(gameId);
            if (null != TianTiFuBenData && TianTiFuBenData.State < GameFuBenState.End)
            {
                if (TianTiFuBenData.ServerId == ClientInfo.ServerId)
                {
                    KuaFuFuBenRoleData kuaFuFuBenRoleData;
                    if (TianTiFuBenData.RoleDict.TryGetValue(roleId, out kuaFuFuBenRoleData))
                    {
                        return kuaFuFuBenRoleData.Side;
                    }
                }
            }

            return 0;
        }

        public TianTiRankData GetRankingData()
        {
            TianTiRankData tianTiRankData;
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    DateTime modifyTime;
                    lock (Mutex)
                    {
                        modifyTime = RankData.ModifyTime;
                    }

                    tianTiRankData = kuaFuService.GetRankingData(modifyTime);
                    lock (Mutex)
                    {
                        if (tianTiRankData != null && tianTiRankData.ModifyTime > RankData.ModifyTime)
                        {
                            RankData = tianTiRankData;
                        }

                        tianTiRankData = new TianTiRankData();
                        tianTiRankData.ModifyTime = RankData.ModifyTime;
                        tianTiRankData.MaxPaiMingRank = RankData.MaxPaiMingRank;
                        if (RankData.TianTiRoleInfoDataList != null && RankData.TianTiRoleInfoDataList.Count > 0)
                        {
                            tianTiRankData.TianTiRoleInfoDataList = new List<TianTiRoleInfoData>(RankData.TianTiRoleInfoDataList);
                        }
                        if (RankData.TianTiMonthRoleInfoDataList != null && RankData.TianTiMonthRoleInfoDataList.Count > 0)
                        {
                            tianTiRankData.TianTiMonthRoleInfoDataList = new List<TianTiRoleInfoData>(RankData.TianTiMonthRoleInfoDataList);
                        }

                        return tianTiRankData;
                    }
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }

            return null;
        }

        public void UpdateRoleInfoData(TianTiRoleInfoData data)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    kuaFuService.UpdateRoleInfoData(data);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }
        }

        #endregion 天梯系统

        #region 众神争霸
        public ZhengBaSyncData GetZhengBaRankData(ZhengBaSyncData lastSyncData)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.SyncZhengBaData(lastSyncData);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }

            return null;
        }

        public int ZhengBaSupport(ZhengBaSupportLogData data)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.ZhengBaSupport(data);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }

            return StdErrorCode.Error_Server_Internal_Error;
        }

        public int ZhengBaRequestEnter(int roleId, int gameId, EZhengBaEnterType enter)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.ZhengBaRequestEnter(roleId, gameId, enter);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }

            return StdErrorCode.Error_Server_Internal_Error;
        }

        public int ZhengBaKuaFuLogin(int roleId, int gameId)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.ZhengBaKuaFuLogin(roleId, gameId);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }

            return StdErrorCode.Error_Server_Internal_Error;
        }

        public List<ZhengBaNtfPkResultData> ZhengBaPkResult(int gameId, int winner, int FirstLeaveRoleId)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.ZhengBaPkResult(gameId, winner, FirstLeaveRoleId);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }

            return null;
        }

        #endregion

        #region 情侣竞技
        public int CoupleArenaJoin(int roleId1, int roleId2, int serverId)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleArenaJoin(roleId1, roleId2, serverId);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return StdErrorCode.Error_Server_Internal_Error;
                }
            }
            else
            {
                return StdErrorCode.Error_Server_Not_Registed;
            }
        }

        public int CoupleArenaQuit(int roleId1, int roleId2)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleArenaQuit(roleId1, roleId2);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return StdErrorCode.Error_Server_Internal_Error;
                }
            }
            else
            {
                return StdErrorCode.Error_Server_Not_Registed;
            }
        }

        public CoupleArenaSyncData CoupleArenaSync(DateTime lastSyncTime)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleArenaSync(lastSyncTime);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public int CoupleArenaPreDivorce(int roleId1, int roleId2)
        {
            ITianTiService kuaFuService = GetKuaFuService(true);
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleArenaPreDivorce(roleId1, roleId2);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return StdErrorCode.Error_Server_Internal_Error;
                }
            }
            else
            {
                return StdErrorCode.Error_Server_Not_Registed;
            }
        }

        public CoupleArenaFuBenData GetFuBenData(long gameId)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.GetFuBenData(gameId);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public CoupleArenaPkResultRsp CoupleArenaPkResult(CoupleArenaPkResultReq req)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleArenaPkResult(req);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region 情侣祝福
        public int CoupleWishWishRole(CoupleWishWishRoleReq req)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleWishWishRole(req);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return StdErrorCode.Error_Server_Internal_Error;
                }
            }
            else
            {
                return StdErrorCode.Error_Server_Not_Registed;
            }
        }

        public List<CoupleWishWishRecordData> CoupleWishGetWishRecord(int roleId)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleWishGetWishRecord(roleId);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public CoupleWishSyncData CoupleWishSyncCenterData(DateTime oldThisWeek, DateTime oldLastWeek, DateTime oldStatue)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleWishSyncCenterData(oldThisWeek, oldLastWeek, oldStatue);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public int CoupleWishPreDivorce(int man, int wife)
        {
            ITianTiService kuaFuService = GetKuaFuService(true);
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleWishPreDivorce(man, wife);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return StdErrorCode.Error_Server_Internal_Error;
                }
            }
            else
            {
                return StdErrorCode.Error_Server_Not_Registed;
            }
        }

        public void CoupleWishReportCoupleStatue(CoupleWishReportStatueData req)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    kuaFuService.CoupleWishReportCoupleStatue(req);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }
            else
            {

            }
        }

        public int CoupleWishAdmire(int fromRole, int fromZone, int admireType, int toCoupleId)
        {
            ITianTiService kuaFuService = GetKuaFuService(true);
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleWishAdmire(fromRole, fromZone, admireType, toCoupleId);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return StdErrorCode.Error_Server_Internal_Error;
                }
            }
            else
            {
                return StdErrorCode.Error_Server_Not_Registed;
            }
        }

        public int CoupleWishJoinParty(int fromRole, int fromZone, int toCoupleId)
        {
            ITianTiService kuaFuService = GetKuaFuService();
            if (null != kuaFuService)
            {
                try
                {
                    return kuaFuService.CoupleWishJoinParty(fromRole, fromZone, toCoupleId);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                    return StdErrorCode.Error_Server_Internal_Error;
                }
            }
            else
            {
                return StdErrorCode.Error_Server_Not_Registed;
            }
        }
        #endregion     
    }
}
