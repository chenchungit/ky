using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KF.Contract.Interface;
using KF.Contract.Data;
using KF.Contract.Data.HuanYingSiYuan;
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
    public class HuanYingSiYuanClient : MarshalByRefObject, IKuaFuClient, IManager2
    {
        #region 标准接口

        private static HuanYingSiYuanClient instance = new HuanYingSiYuanClient();

        public static HuanYingSiYuanClient getInstance()
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
            ClientInfo.GameType = (int)GameTypes.HuanYingSiYuan;
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
        IKuaFuService KuaFuService = null;

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
        public int SceneType = (int)SceneUIClasses.HuanYingSiYuan;

        public GameTypes GameType = GameTypes.HuanYingSiYuan;

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
        /// 帐号ID到跨服角色信息的字典
        /// </summary>
        Dictionary<string, KuaFuRoleData> UserId2RoleDataDict = new Dictionary<string, KuaFuRoleData>();

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
        /// 服务器标志:0 仅区服务器,1 跨服服务器,2 区服务器,3 前两者均可
        /// </summary>
        public int LocalServerFlags = 0; //默认0

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
                string huanYingSiYuanUri = CoreInterface.GetRuntimeVariable(RuntimeVariableNames.HuanYingSiYuanUri, null);
                if (RemoteServiceUri != huanYingSiYuanUri)
                {
                    RemoteServiceUri = huanYingSiYuanUri;
                }

                IKuaFuService kuaFuService = GetKuaFuService();
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
            RemoteServiceUri = CoreInterface.GetRuntimeVariable(RuntimeVariableNames.HuanYingSiYuanUri, null);
            lock (Mutex)
            {
                KuaFuService = null;
            }
        }

        private IKuaFuService GetKuaFuService(bool noWait = false)
        {
            IKuaFuService kuaFuService = null;
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
                        kuaFuService = (IKuaFuService)Activator.GetObject(typeof(IKuaFuService), RemoteServiceUri);
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
            KuaFuRoleData oldKuaFuRoleData, newKuaFuRoleData;
            int oldRoleId = 0, gameType = 0, groupIndex = 0;
            int oldServerId = 0;
            lock (Mutex)
            {
                if (kuaFuRoleData.State == KuaFuRoleStates.None)
                {
                    RemoveRoleData(kuaFuRoleData.RoleId);
                    return (int)KuaFuRoleStates.None;
                }

                string userId = kuaFuRoleData.UserId;

                //如果此帐号下其他角色在跨服游戏中,记下相关信息,稍后处理
                if (UserId2RoleDataDict.TryGetValue(userId, out oldKuaFuRoleData) && oldKuaFuRoleData.RoleId != roleId)
                {
                    oldRoleId = oldKuaFuRoleData.RoleId;
                    gameType = oldKuaFuRoleData.GameType;
                    groupIndex = oldKuaFuRoleData.GroupIndex;
                    oldServerId = oldKuaFuRoleData.ServerId;
                }

                if (oldKuaFuRoleData != kuaFuRoleData)
                {
                    newKuaFuRoleData = (KuaFuRoleData)kuaFuRoleData;
                    UserId2RoleDataDict[userId] = newKuaFuRoleData;
                    RoleId2RoleDataDict[roleId] = newKuaFuRoleData;

                    //更新状态缓存
                    //UserId2KuaFuStateDict[userId] = (int)kuaFuRoleData.State;
                    RoleId2KuaFuStateDict[roleId] = (int)kuaFuRoleData.State;
                }
            }

            if (oldRoleId > 0)
            {
                //如果此帐号下其他角色在跨服游戏中,修改其状态
                RoleChangeState(oldServerId, oldRoleId, (int)KuaFuRoleStates.None);
                RemoveRoleData(oldRoleId);
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

            IKuaFuService kuaFuService = GetKuaFuService();
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
            IKuaFuService kuaFuService = GetKuaFuService();
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

                                    HuanYingSiYuanFuBenData huanYingSiYuanFuBenData = GetKuaFuFuBenData(kuaFuRoleData.GameId);
                                    if (null != huanYingSiYuanFuBenData && huanYingSiYuanFuBenData.State == GameFuBenState.Start)
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
                                            if (ServerIdServerInfoDict.TryGetValue(huanYingSiYuanFuBenData.ServerId, out kuaFuServerInfo))
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

        #region 幻影寺院

        /// <summary>
        /// 匹配报名
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleId"></param>
        /// <param name="zoneId"></param>
        /// <param name="gameType"></param>
        /// <param name="groupIndex"></param>
        /// <returns></returns>
        public int HuanYingSiYuanSignUp(string userId, int roleId, int zoneId, int gameType, int groupIndex, int zhanDouLi)
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

                    IKuaFuService kuaFuService = GetKuaFuService();
                    if (null != kuaFuService)
                    {
                        try
                        {
                            HuanYingSiYuanGameData huanYingSiYuanGameData = new HuanYingSiYuanGameData(){ZhanDouLi = zhanDouLi};
                            result = kuaFuService.RoleSignUp(ClientInfo.ServerId, userId, zoneId, roleId, gameType, groupIndex, huanYingSiYuanGameData);
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

            IKuaFuService kuaFuService = null;
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

        private HuanYingSiYuanFuBenData GetKuaFuFuBenData(int gameId)
        {
            HuanYingSiYuanFuBenData huanYingSiYuanFuBenData = null;

            if (huanYingSiYuanFuBenData == null)
            {
                IKuaFuService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    try
                    {
                        huanYingSiYuanFuBenData = (HuanYingSiYuanFuBenData)kuaFuService.GetFuBenData(gameId);
                    }
                    catch (System.Exception ex)
                    {
                        huanYingSiYuanFuBenData = null;
                    }
                }
            }

            return huanYingSiYuanFuBenData;
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
                IKuaFuService kuaFuService = GetKuaFuService();
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
                IKuaFuService kuaFuService = GetKuaFuService();
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
                KuaFuRoleData kuaFuRoleData;
                if (RoleId2RoleDataDict.TryGetValue(roleId, out kuaFuRoleData))
                {
                    UserId2RoleDataDict.Remove(kuaFuRoleData.UserId);
                }

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
            IKuaFuService kuaFuService = GetKuaFuService();
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
            HuanYingSiYuanFuBenData huanYingSiYuanFuBenData = GetKuaFuFuBenData((int)kuaFuServerLoginData.GameId);
            if (null != huanYingSiYuanFuBenData && huanYingSiYuanFuBenData.State < GameFuBenState.End)
            {
                if (huanYingSiYuanFuBenData.ServerId == ClientInfo.ServerId)
                {
                    if (huanYingSiYuanFuBenData.RoleDict.ContainsKey(kuaFuServerLoginData.RoleId))
                    {
                        KuaFuRoleData kuaFuRoleData = GetKuaFuRoleDataFromServer(kuaFuServerLoginData.ServerId, kuaFuServerLoginData.RoleId);
                        if (kuaFuRoleData.GameId == huanYingSiYuanFuBenData.GameId)
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
        /// 获取角色所属阵营
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public int GetRoleBattleWhichSide(int gameId, int roleId)
        {
            HuanYingSiYuanFuBenData huanYingSiYuanFuBenData = GetKuaFuFuBenData(gameId);
            if (null != huanYingSiYuanFuBenData && huanYingSiYuanFuBenData.State < GameFuBenState.End)
            {
                if (huanYingSiYuanFuBenData.ServerId == ClientInfo.ServerId)
                {
                    KuaFuFuBenRoleData kuaFuFuBenRoleData;
                    if (huanYingSiYuanFuBenData.RoleDict.TryGetValue(roleId, out kuaFuFuBenRoleData))
                    {
                        return kuaFuFuBenRoleData.Side;
                    }
                }
            }

            return 0;
        }

        #endregion 幻影寺院
    }
}
