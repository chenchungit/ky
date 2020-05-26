using GameServer.Core.GameEvent;
using GameServer.Server;
using GameServer.Tools;
using KF.Client;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;
using Tmsk.Contract.KuaFuData;

namespace GameServer.Logic.UnionAlly
{
    public class AllyManager : IManager, ICmdProcessorEx, IEventListenerEx
    {
        #region ----------接口
        private const int ALLY_LOG_MAX = 20;
        public object _mutex = new object();

        public const int _sceneType = (int)SceneUIClasses.Ally;

        private static AllyManager instance = new AllyManager();
        public static AllyManager getInstance()
        {
            return instance;
        }

        public bool initialize() { return true; }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_REQUEST, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_CANCEL, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_REMOVE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_AGREE, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_LOG, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_NUM, 1, 1, getInstance());

            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.Ally, (int)_sceneType, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.AllyLog, (int)_sceneType, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.AllyTip, (int)_sceneType, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFAllyStart, (int)_sceneType, getInstance());

            return true;
        }

        public bool showdown() { return true; }
        public bool destroy() { return true; }

        public bool processCmd(GameClient client, string[] cmdParams) { return true; }
        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_REQUEST:
                    return ProcessAllyRequestCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_CANCEL:
                    return ProcessAllyCancelCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_REMOVE:
                    return ProcessAllyRemoveCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_AGREE:
                    return ProcessAllyAgreeCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_DATA:
                    return ProcessAllyDataCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_LOG:
                    return ProcessAllyLogDataCmd(client, nID, bytes, cmdParams);
            }
            return true;
        }

        public void processEvent(EventObjectEx eventObject)
        {
            int eventType = eventObject.EventType;
            switch (eventType)
            {
                case (int)GlobalEventTypes.KFAllyStart:
                    {
                        int index = 0;
                        GameClient client = null;
                        while ((client = GameManager.ClientMgr.GetNextClient(ref index)) != null)
                        {
                            lock (AllyClient.getInstance()._Mutex)
                            {
                                client.ClientData.AllyList = null;
                                UnionAllyInit(client);
                            }
                        }
                    }
                    break;
                case (int)GlobalEventTypes.Ally:
                    {
                        KFNotifyAllyGameEvent e = eventObject as KFNotifyAllyGameEvent;
                        int unionID = (int)e.UnionID;
                        List<AllyData> list = AllyClient.getInstance().HAllyDataList(unionID, EAllyDataType.Ally);

                        int index = 0;
                        GameClient client = null;
                        while ((client = GameManager.ClientMgr.GetNextClient(ref index)) != null)
                        {
                            lock (AllyClient.getInstance()._Mutex)
                            {
                                if (client.ClientData.Faction != unionID)
                                    continue;

                                client.ClientData.AllyList = list;
                            }
                        }
                    }
                    break;
                case (int)GlobalEventTypes.AllyLog:
                    {
                        KFNotifyAllyLogGameEvent e = eventObject as KFNotifyAllyLogGameEvent;
                        if (null != e)
                        {
                            List<AllyLogData> list = (List<AllyLogData>)e.LogList;
                            if (list != null && list.Count > 0)
                            {
                                foreach (var log in list)
                                    DBAllyLogAdd(log, GameManager.LocalServerId);
                            }

                            eventObject.Handled = true;
                        }
                    }
                    break;
                case (int)GlobalEventTypes.AllyTip:
                    {
                        KFNotifyAllyTipGameEvent e = eventObject as KFNotifyAllyTipGameEvent;
                        if (null != e)
                        {
                            int unionID = (int)e.UnionID;
                            int tipID = (int)e.TipID;

                            BangHuiDetailData unionData = Global.GetBangHuiDetailData(-1, unionID, GameManager.ServerId);
                            if (unionData != null && IsAllyOpen(unionData.QiLevel))
                            {
                                GameClient client = GameManager.ClientMgr.FindClient(unionData.BZRoleID);
                                if (client == null) return;

                                lock (AllyClient.getInstance()._Mutex)
                                {
                                    if (tipID == (int)ActivityTipTypes.AllyAccept)
                                    {
                                        int countAlly = AllyClient.getInstance().AllyCount(unionID);
                                        if (countAlly > 0 && IsAllyMax(countAlly)) return ;
                                    }

                                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.Ally, false);
                                    client._IconStateMgr.AddFlushIconState((ushort)tipID, false);
                                    client._IconStateMgr.SendIconStateToClient(client);

                                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.Ally, true);
                                    client._IconStateMgr.AddFlushIconState((ushort)tipID, true);
                                    client._IconStateMgr.SendIconStateToClient(client);

                                    switch (tipID)
                                    {
                                        case (int)ActivityTipTypes.AllyAccept:
                                            client.AllyTip[0] = 1;
                                            break;
                                        case (int)ActivityTipTypes.AllyMsg:
                                            client.AllyTip[1] = 1;
                                            break;
                                    }
                                }
                            }

                            eventObject.Handled = true;
                        }
                    }
                    break;
            }
        }

        public bool ProcessAllyDataCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                EAllyDataType dataType = (EAllyDataType)Convert.ToInt32(cmdParams[0]);
                List<AllyData> data = GetAllyData(client, dataType);
                client.sendCmd(nID, data);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessAllyLogDataCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                List<AllyLogData> data = GetAllyLogData(client);
                client.sendCmd(nID, data);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessAllyRequestCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 2);
                if (!isCheck) return false;

                int zoneID = Convert.ToInt32(cmdParams[0]);
                string unionName = cmdParams[1];

                EAlly state = AllyRequest(client, zoneID, unionName);
                client.sendCmd(nID, ((int)state).ToString());

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessAllyCancelCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                int unionID = Convert.ToInt32(cmdParams[0]);
                EAlly state = AllyOperate(client, unionID, EAllyOperate.Cancel);
                client.sendCmd(nID, ((int)state).ToString());

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessAllyRemoveCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                int unionID = Convert.ToInt32(cmdParams[0]);
                EAlly state = AllyOperate(client, unionID, EAllyOperate.Remove);
                client.sendCmd(nID, ((int)state).ToString());

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessAllyAgreeCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 2);
                if (!isCheck) return false;

                int unionID = Convert.ToInt32(cmdParams[0]);
                EAllyOperate operateType = (EAllyOperate)Convert.ToInt32(cmdParams[1]);

                EAlly state = AllyOperate(client, unionID, operateType);
                client.sendCmd(nID, ((int)state).ToString());

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        #endregion

        #region ----------功能

        private List<AllyData> GetAllyData(GameClient client, EAllyDataType dataType)
        {
            List<AllyData> resultList = new List<AllyData>();
            List<AllyData> list = null;

            int unionID = client.ClientData.Faction;
            if (unionID <= 0) return resultList;

            BangHuiDetailData unionData = Global.GetBangHuiDetailData(-1, unionID, client.ServerId);
            if (unionData == null || !IsAllyOpen(unionData.QiLevel)) return resultList;

            switch (dataType)
            {
                case EAllyDataType.Ally:
                    {
                        list = AllyClient.getInstance().HAllyDataList(unionID, EAllyDataType.Ally);
                        if (list != null && list.Count > 0) resultList.AddRange(list);

                        list = AllyClient.getInstance().HAllyDataList(unionID, EAllyDataType.Request);
                        if (list != null && list.Count > 0) resultList.AddRange(list);
                    }
                    break;
                case EAllyDataType.Accept:
                    {
                        list = AllyClient.getInstance().HAllyDataList(unionID, EAllyDataType.Accept);
                        if (list != null && list.Count > 0) resultList.AddRange(list);

                        client.AllyTip[0] = 0;
                        if (client.AllyTip[1] <= 0) client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.Ally, false);
                        client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.AllyAccept, false);
                        client._IconStateMgr.SendIconStateToClient(client);
                    }
                    break;
            }

            return resultList;
        }

        private EAlly AllyRequest(GameClient client, int zoneID, string unionName)
        {
            if (zoneID <= 0) return EAlly.EZoneID;
            if (string.IsNullOrEmpty(unionName)) return EAlly.EName;

            int unionID = client.ClientData.Faction;
            if (unionID <= 0) return EAlly.EUnionJoin;

            BangHuiDetailData myUnion = Global.GetBangHuiDetailData(-1, unionID, client.ServerId);
            if (myUnion == null) return EAlly.EUnionJoin;
            if (!IsAllyOpen(myUnion.QiLevel)) return EAlly.EUnionLevel;
            if (myUnion.ZoneID == zoneID && myUnion.BHName == unionName) return EAlly.EIsSelf;
            if (myUnion.BZRoleID != client.ClientData.RoleID) return EAlly.ENotLeader;
            if (!UnionMoneyIsMore(myUnion.TotalMoney)) return EAlly.EMoney;

            if (AllyClient.getInstance().UnionIsAlly(unionID, zoneID, unionName)) return EAlly.EIsAlly;
            if (AllyClient.getInstance().UnionIsRequest(unionID, zoneID, unionName)) return EAlly.EMore;
            if (AllyClient.getInstance().UnionIsAccept(unionID, zoneID, unionName)) return EAlly.EMore;

            int countAlly = AllyClient.getInstance().AllyCount(unionID);
            int countRequest = AllyClient.getInstance().AllyRequestCount(unionID);
            if (countAlly > 0 && IsAllyMax(countAlly)) return EAlly.EAllyMax;

            int countSum = countAlly + countRequest;
            if (countSum > 0 && IsAllyMax(countSum)) return EAlly.EAllyRequestMax;

            EAlly result = AllyClient.getInstance().HAllyRequest(unionID, zoneID, unionName);
            if (result == EAlly.AllyRequestSucc)
            {
                int bhZoneID = 0;
                if (!GameManager.ClientMgr.SubBangHuiTongQian(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    client, this.AllyCostMoney, out bhZoneID))
                {
                    LogManager.WriteLog(LogTypes.Error, "战盟结盟 申请 扣除战盟资金失败");
                }
            }

            return result;
        }

        private EAlly AllyOperate(GameClient client, int targetID, EAllyOperate operateType)
        {
            if (targetID <= 0) return EAlly.ENoTargetUnion;

            int unionID = client.ClientData.Faction;
            if (unionID <= 0) return EAlly.EUnionJoin;

            BangHuiDetailData myUnion = Global.GetBangHuiDetailData(-1, unionID, client.ServerId);
            if (myUnion == null) return EAlly.EUnionJoin;
            if (!IsAllyOpen(myUnion.QiLevel)) return EAlly.EUnionLevel;

            if (myUnion.BZRoleID != client.ClientData.RoleID) return EAlly.ENotLeader;

            int countSum = 0;
            if (operateType == EAllyOperate.Agree)
            {
                int countAlly = AllyClient.getInstance().AllyCount(unionID);
                int countRequest = AllyClient.getInstance().AllyRequestCount(unionID);
                if (countAlly > 0 && IsAllyMax(countAlly)) return EAlly.EAllyMax;

                countSum = countAlly + countRequest;
                if (countSum > 0 && IsAllyMax(countSum)) return EAlly.EAllyMax;
            }

            EAlly result = (EAlly)AllyClient.getInstance().HAllyOperate(unionID, targetID, operateType);
            if (result == EAlly.AllyAgree)
            {
                countSum++;
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_UNION_ALLY_NUM, countSum.ToString());
            }

            return result;
        }

        #endregion

        #region ----------结盟日志

        private List<AllyLogData> GetAllyLogData(GameClient client)
        {
            List<AllyLogData> resultList = new List<AllyLogData>();

            int unionID = client.ClientData.Faction;
            if (unionID <= 0) return resultList;

            BangHuiDetailData unionData = Global.GetBangHuiDetailData(-1, unionID, client.ServerId);
            if (unionData == null || !IsAllyOpen(unionData.QiLevel)) return resultList;

            client.AllyTip[1] = 0;
            if (client.AllyTip[0] <= 0) client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.Ally, false);
            client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.AllyMsg, false);
            client._IconStateMgr.SendIconStateToClient(client);

            return DBAllyLogData(unionID, client.ServerId);
        }

        public List<AllyLogData> DBAllyLogData(int unionID, int serverID)
        {
            int result = 0;
            List<AllyLogData> items = Global.sendToDB<List<AllyLogData>, int>((int)TCPGameServerCmds.CMD_DB_UNION_ALLY_LOG, unionID, serverID);
            if (items == null) items = new List<AllyLogData>();

            return items;
        }

        public bool DBAllyLogAdd(AllyLogData logData, int serverID)
        {
            return Global.sendToDB<bool, AllyLogData>((int)TCPGameServerCmds.CMD_DB_UNION_ALLY_LOG_ADD, logData, serverID);
        }

        #endregion

        #region ----------战盟数据

        public void UnionAllyInit(GameClient client)
        {
            lock (AllyClient.getInstance()._Mutex)
            {
                int unionID = client.ClientData.Faction;
                int serverID = client.ServerId;
                bool isKF = client.ClientSocket.IsKuaFuLogin;

                if (unionID <= 0) return;

                BangHuiDetailData unionData = Global.GetBangHuiDetailData(-1, unionID, serverID);
                if (unionData == null || !IsAllyOpen(unionData.QiLevel)) return;

                EAlly result = AllyClient.getInstance().HUnionAllyInit(unionID, isKF);
                if (result == EAlly.EAddUnion)
                    UnionDataChange(unionID, serverID);
                else if (result != EAlly.Succ)
                    LogManager.WriteLog(LogTypes.Error, string.Format("战盟结盟：数据初始化失败 id={0}", result));

                List<AllyData> list = AllyClient.getInstance().HAllyDataList(unionID, EAllyDataType.Ally);
                if (list != null && list.Count > 0) client.ClientData.AllyList = list;
            }
        }

        public bool UnionIsAlly(GameClient client, int targetID)
        {
            lock (AllyClient.getInstance()._Mutex)
            {
                if (client.ClientData.AllyList == null || client.ClientData.AllyList.Count <= 0) return false;

                AllyData resultData = client.ClientData.AllyList.Find(
                      delegate(AllyData data) { return data.UnionID == targetID; });

                bool isAllyMap = IsAllyMap(client.ClientData.MapCode);

                if (resultData != null && isAllyMap) return true;
                return false;
            }
        }

        public void UnionDataChange(int unionID, int serverID, bool isDel = false, int unionLevel = 0)
        {
            if (unionID <= 0) return;
 
            if (isDel)
            {
                if (!IsAllyOpen(unionLevel)) return;
                EAlly result = AllyClient.getInstance().HUnionDel(unionID);
                if (result != EAlly.Succ)
                    LogManager.WriteLog(LogTypes.Error, string.Format("战盟结盟：战盟{0}解散失败 id={1}", unionID, result));
            }
            else
            {
                BangHuiDetailData unionData = Global.GetBangHuiDetailData(-1, unionID, serverID);
                if (unionData == null || !IsAllyOpen(unionData.QiLevel)) return;

                AllyData data = new AllyData();
                data.UnionID = unionData.BHID;
                data.UnionZoneID = unionData.ZoneID;
                data.UnionName = unionData.BHName;
                data.UnionLevel = unionData.QiLevel;
                data.UnionNum = unionData.TotalNum;
                data.LeaderID = unionData.BZRoleID;
                data.LeaderName = unionData.BZRoleName;

                var clientData = Global.GetSafeClientDataFromLocalOrDB(data.LeaderID);
                data.LeaderZoneID = clientData.ZoneID;

                EAlly result = AllyClient.getInstance().HUnionDataChange(data);
                if (result != EAlly.Succ)
                    LogManager.WriteLog(LogTypes.Error, string.Format("战盟结盟：战盟数据变更失败 id={0}", result));
            }
        }

        public void UnionLeaderChangName(int roleId, string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                return;

            SafeClientData clientData = Global.GetSafeClientDataFromLocalOrDB(roleId);
            if (clientData == null || clientData.Faction <= 0)return;

            BangHuiDetailData unionData = Global.GetBangHuiDetailData(-1, clientData.Faction, GameManager.ServerId);
            if (roleId != unionData.BZRoleID) return;

            UnionDataChange(clientData.Faction, GameManager.ServerId);
        }

        #endregion

        #region 配置相关

        public bool IsAllyOpen(int unionLevel)
        {
            int allyOpenLevel = (int)GameManager.systemParamsList.GetParamValueIntByName("AlignZhanMengLevel");
            if (unionLevel >= allyOpenLevel) return true;

            return false;
        }

        private bool IsAllyMax(int numNow)
        {
            int allyMaxNum = (int)GameManager.systemParamsList.GetParamValueIntByName("AlignNum");
            if (numNow >= allyMaxNum) return true;

            return false;
        }

        private int AllyCostMoney
        {
            get { return (int)GameManager.systemParamsList.GetParamValueIntByName("AlignCostMoney"); }
        }

        private bool UnionMoneyIsMore(int myMoney)
        {
            int[] moneyArr = GameManager.systemParamsList.GetParamValueIntArrayByName("ZhanMengZiJin");
            if (moneyArr == null) return false;

            if (myMoney - this.AllyCostMoney > moneyArr[0]) return true;

            return false;
        }

        private int[] AllyMapArr
        {
            get { return GameManager.systemParamsList.GetParamValueIntArrayByName("AlignMap"); }
        }

        public bool IsAllyMap(int mapID)
        {
            return this.AllyMapArr.Contains(mapID);
        }

        #endregion
    }
}
