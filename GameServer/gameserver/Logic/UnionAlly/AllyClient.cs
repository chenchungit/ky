using KF.Contract;
using KF.Contract.Data;
using KF.Contract.Interface;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;
using Tmsk.Contract.Interface;
using Tmsk.Contract.KuaFuData;

namespace KF.Client
{
    public class AllyClient : MarshalByRefObject, IKuaFuClient, IManager2
    {
        #region 运行时成员

        private static AllyClient instance = new AllyClient();
        public static AllyClient getInstance()
        {
            return instance;
        }

        public const int _gameType = (int)GameTypes.Ally;
        public const int _sceneType = (int)SceneUIClasses.Ally;

        public object _Mutex = new object();
        object _RemotingMutex = new object();

        /// <summary>
        /// 游戏管理接口
        /// </summary>
        ICoreInterface _CoreInterface = null;

        /// <summary>
        /// 跨服中心服务对象
        /// </summary>
        IAllyService _KuaFuService = null;

        /// <summary>
        /// 本地服务器信息
        /// </summary>
        private KuaFuClientContext _ClientInfo = new KuaFuClientContext();

        /// <summary>
        /// 服务地址
        /// </summary>
        private string _RemoteServiceUri = null;

        private DateTime _versionTime = DateTime.MinValue;
        private const int ALLY_VERSION_SPAN_SECOND = 30;

        private long _unionAllyVersion = 0;
        private ConcurrentDictionary<int, DateTime> _unionDic = new ConcurrentDictionary<int, DateTime>();
        private ConcurrentDictionary<int, List<AllyData>> _allyDic = new ConcurrentDictionary<int, List<AllyData>>();
        private ConcurrentDictionary<int, List<AllyData>> _requestDic = new ConcurrentDictionary<int, List<AllyData>>();
        private ConcurrentDictionary<int, List<AllyData>> _acceptDic = new ConcurrentDictionary<int, List<AllyData>>();

        #endregion 

        #region ----------标准接口

        public bool initialize(ICoreInterface coreInterface)
        {
            _CoreInterface = coreInterface;
            _ClientInfo.ServerId = _CoreInterface.GetLocalServerId();
            _ClientInfo.GameType = _gameType;
            return true;
        }

        public bool startup() { return true; }
        public bool showdown() { return true; }
        public bool destroy() { return true; }

        public void EventCallBackHandler(int eventType, params object[] args)
        {
            try
            {
                lock (_Mutex)
                {
                    List<AllyData> list = null;
                    AllyData data = null;
                    switch (eventType)
                    {
                        case (int)KuaFuEventTypes.AllyLog:
                            {
                                if (args.Length == 2)
                                {
                                    List<AllyLogData> logList = (List<AllyLogData>)args[1];
                                    _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyLogGameEvent(logList), (int)_sceneType);
                                }
                            }
                            break;
                        case (int)KuaFuEventTypes.Ally:
                            {
                                if (args.Length == 3)
                                {
                                    int unionID = (int)args[0];
                                    AllyData target = (AllyData)args[1];
                                    bool isTipMsg = (bool)args[2];

                                    if (!_allyDic.TryGetValue(unionID, out list))
                                    {
                                        list = new List<AllyData>() { };
                                        _allyDic.TryAdd(unionID, list);
                                    }

                                    AllyData oldData = GetAllyData(unionID, target.UnionID);
                                    if (oldData == null)
                                    {
                                        list.Add(target);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyGameEvent(unionID), (int)_sceneType);
                                        if (isTipMsg) _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyTipGameEvent(unionID, (int)ActivityTipTypes.AllyMsg), (int)_sceneType);
                                    }
                                }
                            }
                            break;
                        case (int)KuaFuEventTypes.KFAlly:
                            {
                                if (args.Length == 2)
                                {
                                    AllyData unionData = (AllyData)args[0];
                                    AllyData targetData = (AllyData)args[1];

                                    if (!_allyDic.TryGetValue(unionData.UnionID, out list))
                                    {
                                        list = new List<AllyData>() { };
                                        _allyDic.TryAdd(unionData.UnionID, list);
                                    }

                                    AllyData oldData = GetAllyData(unionData.UnionID, targetData.UnionID);
                                    if (oldData == null)
                                    {
                                        list.Add(targetData);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyGameEvent(unionData.UnionID), (int)_sceneType);
                                    }

                                    //
                                    List<AllyData> list2 = null;
                                    if (!_allyDic.TryGetValue(targetData.UnionID, out list2))
                                    {
                                        list2 = new List<AllyData>() { };
                                        _allyDic.TryAdd(targetData.UnionID, list2);
                                    }

                                    oldData = GetAllyData(targetData.UnionID, unionData.UnionID);
                                    if (oldData == null)
                                    {
                                        list2.Add(unionData);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyGameEvent(targetData.UnionID), (int)_sceneType);
                                    }
                                }
                            }
                            break;
                        case (int)KuaFuEventTypes.AllyUnionUpdate:
                            {
                                if (args.Length == 2)
                                {
                                    int unionID = (int)args[0];
                                    AllyData targetData = (AllyData)args[1];

                                    data = GetAllyData(unionID, targetData.UnionID);
                                    if (data != null && _allyDic.TryGetValue(unionID, out list))
                                    {
                                        list.Remove(data);
                                        list.Add(targetData);
                                    }

                                    data = GetRequestData(unionID, targetData.UnionID);
                                    if (data != null && _requestDic.TryGetValue(unionID, out list))
                                    {
                                        list.Remove(data);
                                        list.Add(data);
                                    }

                                    data = GetAcceptData(unionID, targetData.UnionID);
                                    if (data != null && _acceptDic.TryGetValue(unionID, out list))
                                    {
                                        list.Remove(data);
                                        list.Add(data);
                                    }
                                }
                            }
                            break;
                        case (int)KuaFuEventTypes.AllyRemove:
                            {
                                if (args.Length == 2)
                                {
                                    int unionID = (int)args[0];
                                    int targetID = (int)args[1];

                                    data = GetAllyData(unionID, targetID);
                                    if (data != null && _allyDic.TryGetValue(unionID, out list))
                                    {
                                        list.Remove(data);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyTipGameEvent(unionID, (int)ActivityTipTypes.AllyMsg), (int)_sceneType);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyGameEvent(unionID), (int)_sceneType);
                                    }
                                }
                            }
                            break;
                        case (int)KuaFuEventTypes.KFAllyRemove:
                            {
                                if (args.Length == 2)
                                {
                                    int unionID = (int)args[0];
                                    int targetID = (int)args[1];

                                    data = GetAllyData(unionID, targetID);
                                    if (data != null && _allyDic.TryGetValue(unionID, out list))
                                    {
                                        list.Remove(data);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyGameEvent(unionID), (int)_sceneType);
                                    }

                                    AllyData data2 = GetAllyData(targetID, unionID);
                                    List<AllyData> list2 = null;
                                    if (data2 != null && _allyDic.TryGetValue(targetID, out list2))
                                    {
                                        list2.Remove(data2);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyGameEvent(targetID), (int)_sceneType);
                                    }
                                }
                            }
                            break;
                        case (int)KuaFuEventTypes.AllyAccept:
                            {
                                if (args.Length == 2)
                                {
                                    int unionID = (int)args[0];
                                    AllyData target = (AllyData)args[1];

                                    if (!_acceptDic.TryGetValue(unionID, out list))
                                    {
                                        list = new List<AllyData>() { };
                                        _acceptDic.TryAdd(unionID, list);
                                    }

                                    AllyData oldData = GetAcceptData(unionID, target.UnionID);
                                    if (oldData == null)
                                    {
                                        list.Add(target);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyTipGameEvent(unionID, (int)ActivityTipTypes.AllyAccept), (int)_sceneType);
                                    }
                                }
                            }
                            break;
                        case (int)KuaFuEventTypes.AllyAcceptRemove:
                            {
                                if (args.Length == 2)
                                {
                                    int unionID = (int)args[0];
                                    int targetID = (int)args[1];

                                    data = GetAcceptData(unionID, targetID);
                                    if (data != null && _acceptDic.TryGetValue(unionID, out list))
                                    {
                                        list.Remove(data);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyTipGameEvent(unionID, (int)ActivityTipTypes.AllyAccept), (int)_sceneType);
                                    }
                                }
                            }
                            break;
                        case (int)KuaFuEventTypes.AllyRequest:
                            {
                                if (args.Length == 2)
                                {
                                    int unionID = (int)args[0];
                                    AllyData target = (AllyData)args[1];

                                    if (!_requestDic.TryGetValue(unionID, out list))
                                    {
                                        list = new List<AllyData>() { };
                                        _requestDic.TryAdd(unionID, list);
                                    }

                                    AllyData oldData = GetRequestData(unionID, target.UnionID);
                                    if (oldData == null)
                                    {
                                        list.Add(target);
                                    }
                                }
                            }
                            break;
                        case (int)KuaFuEventTypes.AllyRequestRemove:
                            {
                                if (args.Length == 2)
                                {
                                    int unionID = (int)args[0];
                                    int targetID = (int)args[1];

                                    data = GetRequestData(unionID, targetID);
                                    if (data != null && _requestDic.TryGetValue(unionID, out list))
                                    {
                                        list.Remove(data);
                                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyTipGameEvent(unionID, (int)ActivityTipTypes.AllyMsg), (int)_sceneType);
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }
        }

        public object GetDataFromClientServer(int dataType, params object[] args) { return null; }
        public int GetNewFuBenSeqId() { return 0; }
        public int UpdateRoleData(Contract.Data.KuaFuRoleData kuaFuRoleData, int roleId = 0) { return 0; }
        public int OnRoleChangeState(int roleId, int state, int age) { return 0; }

        #endregion 

        #region ----------内部函数

        public void TimerProc(object sender, EventArgs e)
        {
            try
            {
                string uri = _CoreInterface.GetRuntimeVariable(RuntimeVariableNames.AllyUri, null);
                if (_RemoteServiceUri != uri)
                {
                    _RemoteServiceUri = uri;
                }

                IAllyService kuaFuService = GetKuaFuService();
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

                DateTime now = DateTime.Now;
                if (now > _versionTime)
                {
                    _versionTime = now.AddSeconds(ALLY_VERSION_SPAN_SECOND);
                    if (!VersionIsEqual())
                    {
                        _unionDic.Clear();
                        _allyDic.Clear();
                        _requestDic.Clear();
                        _acceptDic.Clear();

                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyStartGameEvent(), (int)_sceneType);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ResetKuaFuService();
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

        private IAllyService GetKuaFuService(bool noWait = false)
        {
            IAllyService kuaFuService = null;
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
                        kuaFuService = (IAllyService)Activator.GetObject(typeof(IAllyService), _RemoteServiceUri);
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
            _RemoteServiceUri = _CoreInterface.GetRuntimeVariable(RuntimeVariableNames.AllyUri, null);
            lock (_Mutex)
            {
                _KuaFuService = null;
            }
        }

        #endregion

        #region ----------功能

        private bool VersionIsEqual()
        {
            lock (_Mutex)
            {
                IAllyService kuaFuService = GetKuaFuService(true);
                if (null == kuaFuService) return false;

                long oldVersion = _unionAllyVersion;
                _unionAllyVersion = kuaFuService.UnionAllyVersion(_ClientInfo.ServerId);
                return _unionAllyVersion == oldVersion && _unionAllyVersion > 0;
            }
        }

        public EAlly HUnionAllyInit(int unionID, bool isKF)
        {
            EAlly result = EAlly.EFail;
            try
            {
                lock (_Mutex)
                {
                    IAllyService kuaFuService = GetKuaFuService(true);
                    if (null == kuaFuService) return result;

                    DateTime oldTime;
                    if (_unionDic.TryGetValue(unionID, out oldTime))
                    {
                        _unionDic[unionID] = DateTime.Now;
                        return EAlly.Succ;
                    }
                    
                    try
                    {
                        result = (EAlly)kuaFuService.UnionAllyInit(_ClientInfo.ServerId, unionID, isKF);
                        if (result == EAlly.Succ)
                        {
                            _unionDic.TryAdd(unionID, DateTime.Now);

                            HAllyDataList(unionID,EAllyDataType.Ally);
                            HAllyDataList(unionID, EAllyDataType.Request);
                            HAllyDataList(unionID,EAllyDataType.Accept);
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

            return result;
        }

        public EAlly HUnionDel(int unionID)
        {
            EAlly result = EAlly.EFail;
            try
            {
                lock (_Mutex)
                {
                    IAllyService kuaFuService = GetKuaFuService();
                    if (null == kuaFuService) return result;

                    try
                    {
                        result = (EAlly)kuaFuService.UnionDel(_ClientInfo.ServerId, unionID);
                        if (result == EAlly.Succ)
                        {
                            DateTime oldTime;
                            List<AllyData> list;
                            _unionDic.TryRemove(unionID, out oldTime);
                            _allyDic.TryRemove(unionID, out list);
                            _requestDic.TryRemove(unionID, out list);
                            _acceptDic.TryRemove(unionID, out list);
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

            return result;
        }

        public EAlly HUnionDataChange(AllyData unionData)
        {
            EAlly result = EAlly.EFail;
            try
            {
                lock (_Mutex)
                {
                    IAllyService kuaFuService = GetKuaFuService();
                    if (null == kuaFuService) return result;

                    try
                    {
                        result = (EAlly)kuaFuService.UnionDataChange(_ClientInfo.ServerId, unionData);
                        if (result == EAlly.Succ)
                        {
                            int unionID = unionData.UnionID;

                             DateTime oldTime;
                            if (_unionDic.TryGetValue(unionID, out oldTime))
                            {
                                _unionDic[unionID] = DateTime.Now;
                                return EAlly.Succ;
                            }
                            else
                            {
                                _unionDic.TryAdd(unionID, DateTime.Now);

                                HAllyDataList(unionID,EAllyDataType.Ally);
                                HAllyDataList(unionID, EAllyDataType.Request);
                                HAllyDataList(unionID,EAllyDataType.Accept);
                            }
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

            return result;
        }

        public List<AllyData> HAllyDataList(int unionID, EAllyDataType type)
        {
            List<AllyData> list = new List<AllyData>();

            lock (_Mutex)
            {
                ConcurrentDictionary<int, List<AllyData>> dic = null;
                switch (type)
                {
                    case EAllyDataType.Ally:
                        dic = _allyDic;
                        break;
                    case EAllyDataType.Request:
                        dic = _requestDic;
                        break;
                    case EAllyDataType.Accept:
                         dic = _acceptDic;
                        break;
                }

                if (dic.TryGetValue(unionID, out list)) return list;

                IAllyService kuaFuService = GetKuaFuService();
                if (null == kuaFuService) return list;

                try
                {
                    list = kuaFuService.AllyDataList(_ClientInfo.ServerId, unionID, (int)type);
                    if (list != null) dic.TryAdd(unionID, list);
                }
                catch (System.Exception ex)
                {
                    ResetKuaFuService();
                }
            }

            return list;
        }

        public EAlly HAllyOperate(int unionID, int targetID, EAllyOperate operateType)
        {
            EAlly result = EAlly.EFail;
            try
            {
                lock (_Mutex)
                {
                    if (!VersionIsEqual())
                    {
                        _unionDic.Clear();
                        _allyDic.Clear();
                        _requestDic.Clear();
                        _acceptDic.Clear();

                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyStartGameEvent(), (int)_sceneType);
                        return result;
                    }

                    ConcurrentDictionary<int, List<AllyData>> dic = null;
                    switch (operateType)
                    {
                        case EAllyOperate.Agree:
                        case EAllyOperate.Refuse:
                            dic = _acceptDic;
                            break;
                        case EAllyOperate.Cancel:
                            dic = _requestDic;
                            break;
                        case EAllyOperate.Remove:
                            dic = _allyDic;
                            break;
                    }

                    List<AllyData> list = null;
                    if (!dic.TryGetValue(unionID, out list)) return EAlly.ENoTargetUnion;

                    AllyData targetData = dic[unionID].Find(
                                    delegate(AllyData data) { return data.UnionID == targetID; });
                    if (targetData == null) return EAlly.ENoTargetUnion;

                    IAllyService kuaFuService = GetKuaFuService();
                    if (null == kuaFuService) return EAlly.EServer;

                    try
                    {
                        result = (EAlly)kuaFuService.AllyOperate(_ClientInfo.ServerId, unionID, targetID, (int)operateType);
                        if (result == EAlly.AllyAgree || result == EAlly.AllyRefuse
                            || result == EAlly.AllyCancelSucc || result == EAlly.AllyRemoveSucc)
                        {
                            dic[unionID].Remove(targetData);
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

            return result;
        }

        public EAlly HAllyRequest(int unionID, int zoneID, string unionName)
        {
            AllyData result = null;
            try
            {
                lock (_Mutex)
                {
                    if (!VersionIsEqual())
                    {
                        _unionDic.Clear();
                        _allyDic.Clear();
                        _requestDic.Clear();
                        _acceptDic.Clear();

                        _CoreInterface.GetEventSourceInterface().fireEvent(new KFNotifyAllyStartGameEvent(), (int)_sceneType);
                        return EAlly.EFail;
                    }

                    IAllyService kuaFuService = GetKuaFuService();
                    if (null == kuaFuService) return EAlly.EServer;

                    try
                    {
                        AllyData d = kuaFuService.AllyRequest(_ClientInfo.ServerId, unionID, zoneID, unionName);
                        if (d.LogState == (int)EAlly.AllyRequestSucc)
                        {
                            List<AllyData> list = null;
                            if (!_requestDic.TryGetValue(unionID, out list))
                            {
                                list = new List<AllyData>() { };
                                _requestDic.TryAdd(unionID, list);
                            }

                            list.Add(d);
                            return (EAlly)d.LogState;
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

            return EAlly.EAllyRequest;
        }

        public bool UnionIsAlly(int unionID, int zoneID, string unionName)
        {
            lock (_Mutex)
            {
                List<AllyData> list = null;
                if (_allyDic.TryGetValue(unionID, out list))
                {
                    AllyData resultData = list.Find(
                                      delegate(AllyData data) { return data.UnionZoneID == zoneID && data.UnionName == unionName; });

                    if (resultData != null) return true;
                }

                return false;
            }
        }

        public bool UnionIsRequest(int unionID, int zoneID,string unionName)
        {
            lock (_Mutex)
            {
                List<AllyData> list = null;
                if (_requestDic.TryGetValue(unionID, out list))
                {
                    AllyData resultData = list.Find(
                                      delegate(AllyData data) { return data.UnionZoneID == zoneID && data.UnionName == unionName; });

                    if (resultData != null) return true;
                }

                return false;
            }
        }

        public bool UnionIsAccept(int unionID, int zoneID, string unionName)
        {
            lock (_Mutex)
            {
                List<AllyData> list = null;
                if (_acceptDic.TryGetValue(unionID, out list))
                {
                    AllyData resultData = list.Find(
                                      delegate(AllyData data) { return data.UnionZoneID == zoneID && data.UnionName == unionName; });

                    if (resultData != null) return true;
                }

                return false;
            }
        }

        public int AllyCount(int unionID)
        {
            lock (_Mutex)
            {
                if (_allyDic.ContainsKey(unionID))
                    return _allyDic[unionID].Count;

                return 0;
            }
        }

        public int AllyRequestCount(int unionID)
        {
            lock (_Mutex)
            {
                if (_requestDic.ContainsKey(unionID))
                    return _requestDic[unionID].Count;

                return 0;
            }
        }

        private AllyData GetAllyData(int unionID, int targetID)
        {
            lock (_Mutex)
            {
                List<AllyData> list = null;
                if (_allyDic.TryGetValue(unionID, out list))
                    return list.Find(delegate(AllyData data) { return data.UnionID == targetID; });

                return null;
            }
        }

        private AllyData GetRequestData(int unionID, int targetID)
        {
            lock (_Mutex)
            {
                List<AllyData> list = null;
                if (_requestDic.TryGetValue(unionID, out list))
                    return list.Find(delegate(AllyData data) { return data.UnionID == targetID; });

                return null;
            }
        }

        private AllyData GetAcceptData(int unionID, int targetID)
        {
            lock (_Mutex)
            {
                List<AllyData> list = null;
                if (_acceptDic.TryGetValue(unionID, out list))
                    return list.Find(delegate(AllyData data) { return data.UnionID == targetID; });

                return null;
            }
        }

        #endregion
    }
}
