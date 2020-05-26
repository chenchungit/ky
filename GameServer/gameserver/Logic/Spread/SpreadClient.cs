using KF.Contract;
using KF.Contract.Data;
using KF.Contract.Data.HuanYingSiYuan;
using KF.Contract.Interface;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Tmsk.Contract;
using Tmsk.Contract.Const;

namespace KF.Client
{
    public class SpreadClient : MarshalByRefObject, IKuaFuClient, IManager2
    {
        #region 标准接口

        private static SpreadClient instance = new SpreadClient();

        public static SpreadClient getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            return true;
        }

        public bool initialize(ICoreInterface coreInterface)
        {
            _CoreInterface = coreInterface;
            _ClientInfo.ServerId = _CoreInterface.GetLocalServerId();
            _ClientInfo.GameType = (int)_gameType;
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

        public const int _gameType = (int)GameTypes.Spread;

        public const int _sceneType = (int)SceneUIClasses.Spread;


        /// <summary>
        /// lock对象
        /// </summary>
        object _Mutex = new object();

        object _RemotingMutex = new object();

        /// <summary>
        /// 游戏管理接口
        /// </summary>
        ICoreInterface _CoreInterface = null;

        /// <summary>
        /// 跨服中心服务对象
        /// </summary>
        ISpreadService _KuaFuService = null;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool _ClientInitialized = false;

        /// <summary>
        /// 本地服务器信息
        /// </summary>
        private KuaFuClientContext _ClientInfo = new KuaFuClientContext();

        /// <summary>
        /// 角色ID到跨服角色信息的字典
        /// </summary>
        ConcurrentDictionary<int, KFSpreadData> _RoleId2KFSpreadDataDict = new ConcurrentDictionary<int, KFSpreadData>();

        /// <summary>
        /// 服务地址
        /// </summary>
        private string _RemoteServiceUri = null;

        private DateTime _checkSpreadDataTime = DateTime.MinValue;

        private const int CHECK_SPREAD_DATA_SECOND = 12*3600;

        #endregion 运行时成员

        #region 内部函数

        public void TimerProc(object sender, EventArgs e)
        {
            try
            {
                string uri = _CoreInterface.GetRuntimeVariable(RuntimeVariableNames.SpreadUri, null);
                if (_RemoteServiceUri != uri)
                {
                    _RemoteServiceUri = uri;
                }

                ISpreadService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    if (_ClientInfo.ClientId > 0)
                    {                   
                        //同步数据
                        AsyncDataItem[] items = kuaFuService.GetClientCacheItems(_ClientInfo.ServerId);
                        if (null != items && items.Length > 0)
                        {
                            ExecuteEventCallBackAsync(items);
                        }
                    }
                }

                CheckSpreadData();
            }
            catch (System.Exception ex)
            {
                ResetKuaFuService();
            }
        }

        private void CheckSpreadData()
        {
            if (_RoleId2KFSpreadDataDict == null || _RoleId2KFSpreadDataDict.Count <= 0) return;
            if (DateTime.Now < _checkSpreadDataTime) return;
            _checkSpreadDataTime = DateTime.Now.AddSeconds(CHECK_SPREAD_DATA_SECOND);

            lock (_RoleId2KFSpreadDataDict)
            {
                List<int> roleList = (from info in _RoleId2KFSpreadDataDict.Values
                                      where info.LogTime <= DateTime.Now.AddSeconds(-CHECK_SPREAD_DATA_SECOND)
                            select info.RoleID).ToList<int>();

                foreach (var t in roleList)
                {
                    KFSpreadData d;
                    _RoleId2KFSpreadDataDict.TryRemove(t, out d);
                }
            }
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

        private ISpreadService GetKuaFuService(bool noWait = false)
        {
            ISpreadService kuaFuService = null;
            int clientId = -1;

            try
            {
                lock (_Mutex)
                {
                    if (string.IsNullOrEmpty(_RemoteServiceUri))
                    {
                        return null;
                    }

                    if (null == _KuaFuService && noWait)
                    {
                        return null;
                    }
                }

                lock (_RemotingMutex)
                {
                    if (_KuaFuService == null)
                    {
                        kuaFuService = (ISpreadService)Activator.GetObject(typeof(ISpreadService), _RemoteServiceUri);
                        if (null == kuaFuService)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        kuaFuService = _KuaFuService;
                    }

                    clientId = kuaFuService.InitializeClient(this, _ClientInfo);
                    if (null != kuaFuService && (clientId != _ClientInfo.ClientId || _KuaFuService != kuaFuService))
                    {
                        lock (_Mutex)
                        {
                            _KuaFuService = kuaFuService;
                            _ClientInfo.ClientId = clientId;
                            return kuaFuService;
                        }
                    }

                    return _KuaFuService;
                }
            }
            catch (System.Exception ex)
            {
                ResetKuaFuService();
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return null;
        }

        private void ResetKuaFuService()
        {
            _RemoteServiceUri = _CoreInterface.GetRuntimeVariable(RuntimeVariableNames.SpreadUri, null);
            lock (_Mutex)
            {
                _KuaFuService = null;
            }
        }

        #endregion 内部函数

        #region Implement IKuaFuClient

        public void EventCallBackHandler(int eventType, params object[] args)
        {
            try
            {
                switch (eventType)
                {
                    case (int)KuaFuEventTypes.SpreadCount:
                        {
                            if (args.Length == 5)
                            {
                                int zoneID = (int)args[0];
                                int roleID = (int)args[1];
                                int countRole = (int)args[2];
                                int countVip = (int)args[3];
                                int countLevle = (int)args[4];

                                KFSpreadData data;
                                //如果取到数据，证明已经注册说了
                                if (!_RoleId2KFSpreadDataDict.TryGetValue(roleID, out data)) return;

                                lock (data)
                                {
                                    data.CountRole = countRole;
                                    data.CountVip = countVip;
                                    data.CountLevel = countLevle;
                                    data.UpdateLogtime();

                                    _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifySpreadCountGameEvent(zoneID, roleID, countRole, countVip, countLevle), (int)_sceneType);
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }
        }

        public object GetDataFromClientServer(int dataType, params object[] args) { return null; }
        public int GetNewFuBenSeqId() { return 0; }
        public int UpdateRoleData(KuaFuRoleData kuaFuRoleData, int roleId = 0) { return 0; }
        public int OnRoleChangeState(int roleId, int state, int age) { return 0; }

        #endregion

        #region ----------功能

        public int SpreadSign(int zoneID, int roleID)
        {
            int result;
            try
            {
                lock (_Mutex)
                {
                    KFSpreadData data;
                    //如果取到数据，证明已经注册说了
                    if (_RoleId2KFSpreadDataDict.TryGetValue(roleID, out data)) return 0;

                    ISpreadService kuaFuService = GetKuaFuService();
                    if (null == kuaFuService)  return StdErrorCode.Error_Server_Not_Registed;

                    try
                    {
                        result = kuaFuService.SpreadSign(_ClientInfo.ServerId, zoneID, roleID);
                        if (result > 0)
                        {
                            KFSpreadData newData = new KFSpreadData();
                            newData.ServerID = _ClientInfo.ServerId;
                            newData.ZoneID = zoneID;
                            newData.RoleID = roleID;

                            _RoleId2KFSpreadDataDict.TryAdd(roleID, newData);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ResetKuaFuService();
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return StdErrorCode.Error_Success;
        }

        public int[] SpreadCount(int zoneID, int roleID)
        {
            int[] counts = { 0, 0, 0 };

            lock (_Mutex)
            {
                KFSpreadData data;
                if (_RoleId2KFSpreadDataDict.TryGetValue(roleID, out data))
                {
                    data.UpdateLogtime();
                    return new int[] { data.CountRole, data.CountVip, data.CountLevel };
                }

                ISpreadService kuaFuService = GetKuaFuService();
                if (null == kuaFuService) return counts;

                try
                {
                    counts = kuaFuService.SpreadCount(_ClientInfo.ServerId, zoneID, roleID);
                    if (counts != null && counts.Length == 3)
                    {
                        KFSpreadData newData = new KFSpreadData();
                        newData.ServerID = _ClientInfo.ServerId;
                        newData.ZoneID = zoneID;
                        newData.RoleID = roleID;
                        newData.CountRole = counts[0];
                        newData.CountVip = counts[1];
                        newData.CountLevel = counts[2];

                        _RoleId2KFSpreadDataDict.TryAdd(roleID, newData);
                    }
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }

            return counts;
        }

        public int CheckVerifyCode(string cuserID, int czoneID, int croleID, int pzoneID, int proleID, int isVip, int isLevel)
        {
            int result = (int)ESpreadState.Fail;
            try
            {
                ISpreadService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    try
                    {
                        result = kuaFuService.CheckVerifyCode(_ClientInfo.ServerId, cuserID, czoneID, croleID, pzoneID, proleID , isVip, isLevel);
                    }
                    catch (System.Exception ex)
                    {
                        ResetKuaFuService();
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return result;
        }

        public int TelCodeGet(int czoneID, int croleID, string tel)
        {
            int result = (int)ESpreadState.Fail;
            try
            {
                ISpreadService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    try
                    {
                        result = kuaFuService.TelCodeGet(_ClientInfo.ServerId, czoneID, croleID, tel);
                    }
                    catch (System.Exception ex)
                    {
                        ResetKuaFuService();
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return result;
        }

        public int TelCodeVerify(int czoneID, int croleID, int telCode)
        {
            int result = (int)ESpreadState.Fail;
            try
            {
                ISpreadService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    try
                    {
                        result = kuaFuService.TelCodeVerify(_ClientInfo.ServerId, czoneID, croleID, telCode);
                    }
                    catch (System.Exception ex)
                    {
                        ResetKuaFuService();
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return result;
        }

        public bool SpreadLevel(int pzoneID, int proleID, int czoneID, int croleID)
        {
            bool result = false;
            try
            {
                ISpreadService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    try
                    {
                      result = kuaFuService.SpreadLevel(pzoneID, proleID, czoneID, croleID);
                    }
                    catch (System.Exception ex)
                    {
                        ResetKuaFuService();
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return result;
        }

        public bool SpreadVip(int pzoneID, int proleID, int czoneID, int croleID)
        {
            bool result = false;
            try
            {
                ISpreadService kuaFuService = GetKuaFuService();
                if (null != kuaFuService)
                {
                    try
                    {
                       result = kuaFuService.SpreadVip(pzoneID, proleID, czoneID, croleID);
                    }
                    catch (System.Exception ex)
                    {
                        ResetKuaFuService();
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return result;
        }


        #endregion


    }
}
