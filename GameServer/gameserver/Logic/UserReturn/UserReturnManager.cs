using GameServer.Core.GameEvent;
using GameServer.Logic.RefreshIconState;
using GameServer.Server;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Tmsk.Contract;
using GameServer.Core.Executor;
using GameServer.Tools;

namespace GameServer.Logic.UserReturn
{
    /// <summary>
    /// 玩家召回管理器
    /// </summary>
    public class UserReturnManager : ICmdProcessorEx
    {
        #region ----------接口相关

        private const int AWARD_DAY_COUNT = 6;
        private const int CHECK_WAIT_HOUR = 1;
        private static UserReturnManager instance = new UserReturnManager();
        public static UserReturnManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            if (!initConfigInfo())
                return false;

            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_RETURN_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_RETURN_CHECK, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_RETURN_AWARD, 3, 3, getInstance());

            return true;
        }

        public bool showdown() { return true; }
        public bool destroy() { return true; }
        public bool processCmd(GameClient client, string[] cmdParams) { return true; }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_RETURN_DATA:
                    return ProCmdReturnData(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_RETURN_CHECK:
                    return ProCmdReturnCheck(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_RETURN_AWARD:
                    return ProCmdReturnAward(client, nID, bytes, cmdParams);
            }

            return true;
        }

        public bool ProCmdReturnData(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                UserReturnData result = GetUserReturnData(client);
                client.sendCmd(nID, result);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProCmdReturnCheck(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 2);
                if (!isCheck) return false;

                string code = cmdParams[1];
                int result = (int)ReturnCheck(client, code);
                client.sendCmd(nID, result);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProCmdReturnAward(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 3);
                if (!isCheck) return false;

                int awardType = Convert.ToInt32(cmdParams[0]);
                int awardID = Convert.ToInt32(cmdParams[1]);
                int awardCount = Convert.ToInt32(cmdParams[2]);

                string result = "{0}:{1}:{2}";//操作状态，奖励类型，奖励数据(*间隔)
                string awardData = "0";
                EReturnAwardState state = ReturnAward(client, awardType, awardID, awardCount);
                if (state == EReturnAwardState.Succ)
                {
                    UserReturnData myData = GetUserReturnData(client);
                    if (myData.AwardDic.ContainsKey(awardType))
                        awardData = string.Join("*", myData.AwardDic[awardType]);
                }

                result = string.Format(result, (int)state, awardType, awardData);
                client.sendCmd(nID, result);
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

        private UserReturnData GetUserReturnData(GameClient client)
        {
            lock (client.ClientData.LockReturnData)
            {
                UserReturnData myData = client.ClientData.ReturnData;
                if (myData != null && myData.ActivityIsOpen != _returnActivityInfo.IsOpen)
                {
                    initUserReturnData(client);
                    myData = client.ClientData.ReturnData;
                }

                if (!_returnActivityInfo.IsOpen) return myData;

                if (myData.ReturnState == (int)EReturnState.Default || myData.ReturnState == (int)EReturnState.Check)
                {
                    string returnCode = Global.GetRoleParamByName(client, RoleParamName.ReturnCode);
                    if (!string.IsNullOrEmpty(returnCode))
                    {
                        string[] arr = returnCode.Split('|');
                        string day = arr[0];
                        string code = arr[1];
                        DateTime logTime = new DateTime(long.Parse(arr[2]));

                        if (_returnActivityInfo.ActivityDay == day)
                        {
                            if (myData.ReturnState == (int)EReturnState.Check && code == "0")
                            {
                                Global.UpdateRoleParamByName(client, RoleParamName.ReturnCode, "", true);
                                myData.RecallCode = "0";
                                myData.TimeWait = DateTime.MinValue;
                            }
                            else
                            {
                                myData.RecallCode = code;
                                myData.ReturnState = (code == "0" ? (int)EReturnState.WaitCheck : (int)EReturnState.WaitSign);
                                myData.TimeWait = logTime;
                            }
                        }
                        else
                        {
                            Global.UpdateRoleParamByName(client, RoleParamName.ReturnCode, "", true);
                        }
                    }
                }

                if ((myData.ReturnState == (int)EReturnState.WaitCheck || myData.ReturnState == (int)EReturnState.WaitSign)
                    && DateTime.Now > myData.TimeWait.AddHours(1))
                {
                    myData.RecallCode = "";
                    myData.ReturnState = 0;
                    myData.TimeWait = DateTime.MinValue;

                    Global.UpdateRoleParamByName(client, RoleParamName.ReturnCode, "", true);
                }

                if (myData.ReturnState == (int)EReturnState.ShowReturn) myData.ReturnState = (int)EReturnState.Default;
                if (myData.ReturnState == (int)EReturnState.ShowNoCheck) myData.ReturnState = (int)EReturnState.Default;
                if (myData.ReturnState == (int)EReturnState.ShowNoSign) myData.ReturnState = (int)EReturnState.Check;

                if (myData.ReturnState == (int)EReturnState.ECheck)
                {
                    ReturnData data = ReturnDataGet(client.ClientData.RoleID);
                    bool b = DBUserReturnDataDel(client, data);
                    if (b)
                    {
                        ReturnDataRemove(client.ClientData.RoleID);
                        myData.ReturnState = (int)EReturnState.ShowNoCheck;
                        Global.UpdateRoleParamByName(client, RoleParamName.ReturnCode, "", true);

                        client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnResult, false);
                        client._IconStateMgr.SendIconStateToClient(client);
                    }
                }

                if (myData.ReturnState == (int)EReturnState.EIsReturn)
                {
                    ReturnData data = ReturnDataGet(client.ClientData.RoleID);
                    bool b = DBUserReturnDataDel(client, data);
                    if (b)
                    {
                        ReturnDataRemove(client.ClientData.RoleID);
                        myData.ReturnState = (int)EReturnState.ShowReturn;
                        Global.UpdateRoleParamByName(client, RoleParamName.ReturnCode, "", true);

                        client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnResult, false);
                        client._IconStateMgr.SendIconStateToClient(client);
                    }
                }

                if (myData.ReturnState == (int)EReturnState.ESign)
                {
                    ReturnData data = ReturnDataGet(client.ClientData.RoleID);
                    data.StateCheck = (int)EReturnState.Check;

                    bool b = DBUserReturnDataUpdate(client, data);
                    if (b)
                    {
                        myData.ReturnState = (int)EReturnState.ShowNoSign;
                        Global.UpdateRoleParamByName(client, RoleParamName.ReturnCode, "", true);

                        client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnResult, false);
                        client._IconStateMgr.SendIconStateToClient(client);
                    }
                }

                //LogManager.WriteLog(LogTypes.Error,string.Format("--------------------召回状态：{0} 召回角色等级：{1}/{2} 召回角色vip：{3} ", myData.ReturnState,myData.Level,myData.DengJi,myData.Vip));

                return myData;
            }
        }

        private EReturnState ReturnCheck(GameClient client, string code)
        {
            lock (client.ClientData.LockReturnData)
            {
                if (!_returnActivityInfo.IsOpen || TimeUtil.NowDateTime() >= _returnActivityInfo.TimeEnd) return EReturnState.ENoOpen;

                int level = client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level;
                if (level < _returnActivityInfo.Level) return EReturnState.ELevel;
                if (client.ClientData.VipLevel < _returnActivityInfo.Vip) return EReturnState.EVip;

                UserReturnData myData = GetUserReturnData(client);
                if (code == "0")
                {
                    if (myData.ReturnState == (int)EReturnState.WaitCheck) return EReturnState.EWait;
                    if (myData.ReturnState == (int)EReturnState.Check) return EReturnState.EIsReturn;
                }
                else
                {
                    if (myData.ReturnState == (int)EReturnState.WaitSign) return EReturnState.EWait;
                    if (myData.ReturnState == (int)EReturnState.CheckAndSign) return EReturnState.EIsReturn;
                    if (myData.ReturnState != (int)EReturnState.Check) return EReturnState.EFail;

                    string[] recallIDs = code.Split('#');
                    if (recallIDs == null || recallIDs.Length != 3)
                        return EReturnState.EFail;

                    int playform = StringUtil.CodeToID(recallIDs[0]);
                    int pZoneID = StringUtil.CodeToID(recallIDs[1]);
                    int pRoleID = StringUtil.CodeToID(recallIDs[2]);
                    if (pZoneID <= 0 || pRoleID <= 0)
                        return EReturnState.EFail;

                    int myPlatform = (int)GameCoreInterface.getinstance().GetPlatformType();
                    if (playform != myPlatform) return EReturnState.EPlatform;

                    if (pRoleID > 0 && pRoleID == client.ClientData.RoleID) return EReturnState.EIsSelf;
                    if (myData.ReturnState == (int)EReturnState.WaitSign) return EReturnState.EWait;
                    if (myData.ReturnState == (int)EReturnState.CheckAndSign) return EReturnState.EIsReturn;
                }

                long time = DateTime.Now.Ticks;
                Global.UpdateRoleParamByName(client, RoleParamName.ReturnCode, string.Format("{0}|{1}|{2}", _returnActivityInfo.ActivityDay, code, time), true);

                myData.ReturnState = (code == "0") ? (int)EReturnState.WaitCheck : (int)EReturnState.WaitSign;
                myData.TimeWait = DateTime.Now;

                return (EReturnState)myData.ReturnState;
            }
        }

        private EReturnAwardState ReturnAward(GameClient client, int awardType, int awardID, int awardCount)
        {
            lock (client.ClientData.LockReturnData)
            {
                if (!_returnActivityInfo.IsOpen || TimeUtil.NowDateTime() >= _returnActivityInfo.TimeAward) return EReturnAwardState.ENoOpen;

                UserReturnData myData = client.ClientData.ReturnData;
                if (awardType != (int)EReturnAwardType.Recall && myData.ReturnState <= (int)EReturnState.WaitCheck) return EReturnAwardState.ENoReturn;

                switch ((EReturnAwardType)awardType)
                {
                    case EReturnAwardType.Return:
                        return AwardReturn(client, myData, awardID);
                    case EReturnAwardType.Check:
                        return AwardCheck(client, myData, awardID);
                    case EReturnAwardType.Shop:
                        return AwardShop(client, myData, awardID, awardCount);
                    case EReturnAwardType.Recall:
                        return AwardRecall(client, myData, awardID);
                }

                return EReturnAwardState.EFail;
            }
        }

        //回归：vip，奖励
        private EReturnAwardState AwardReturn(GameClient client, UserReturnData myData, int awardID)
        {
            lock (client.ClientData.LockReturnData)
            {
                bool isNew = false;
                int awardType = (int)EReturnAwardType.Return;
                ReturnAwardInfo awardInfo = null;
                if (!myData.AwardDic.ContainsKey(awardType))
                {
                    isNew = true;
                    var tReturn = from info in _returnAwardDic.Values
                                  orderby info.Vip
                                  select info;

                    if (tReturn.Any())
                        awardInfo = tReturn.First<ReturnAwardInfo>();
                }
                else
                {
                    int oldID = myData.AwardDic[awardType][0];
                    var tReturn = from info in _returnAwardDic.Values
                                  where info.ID > oldID
                                  orderby info.Vip
                                  select info;

                    if (tReturn.Any())
                        awardInfo = tReturn.First<ReturnAwardInfo>();
                }

                if (awardInfo == null || awardInfo.ID != awardID) return EReturnAwardState.EFail;
                if (client.ClientData.VipLevel < awardInfo.Vip) return EReturnAwardState.EVip;

                //奖励
                List<GoodsData> awardList = new List<GoodsData>();
                if (awardInfo.DefaultGoodsList != null) awardList.AddRange(awardInfo.DefaultGoodsList);

                List<GoodsData> proGoods = GoodsHelper.GetAwardPro(client, awardInfo.ProGoodsList);
                if (proGoods != null) awardList.AddRange(proGoods);

                //背包
                if (!Global.CanAddGoodsDataList(client, awardList)) return EReturnAwardState.ENoBag;

                //记录
                string award = awardInfo.ID.ToString();
                bool result = DBUserReturnAwardUpdate(client, awardType, award);
                if (result)
                {
                    if (isNew) myData.AwardDic.Add(awardType, new int[] { awardInfo.ID });
                    else myData.AwardDic[awardType] = new int[] { awardInfo.ID };

                    //发奖
                    for (int i = 0; i < awardList.Count; i++)
                    {
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                            awardList[i].GoodsID, awardList[i].GCount, awardList[i].Quality, "", awardList[i].Forge_level,
                            awardList[i].Binding, 0, "", true, 1,
                            /**/"回归奖励", Global.ConstGoodsEndTime, awardList[i].AddPropIndex, awardList[i].BornIndex,
                            awardList[i].Lucky, awardList[i].Strong, awardList[i].ExcellenceInfo, awardList[i].AppendPropLev);
                    }

                    CheckActivityTip(client);
                    return EReturnAwardState.Succ;
                }

                return EReturnAwardState.EFail;
            }
        }

        //回归签到：角色等级范围，天数
        private EReturnAwardState AwardCheck(GameClient client, UserReturnData myData, int awardID)
        {
            lock (client.ClientData.LockReturnData)
            {
                bool isNew = false;
                int awardType = (int)EReturnAwardType.Check;
                ReturnCheckAwardInfo awardInfo = null;
                if (!myData.AwardDic.ContainsKey(awardType))
                {
                    isNew = true;
                    var tReturn = from info in _returnCheckAwardDic.Values
                                  where myData.Level >= info.LevelMin && myData.Level <= info.LevelMax
                                  orderby info.Day
                                  select info;

                    if (tReturn.Any())
                        awardInfo = tReturn.First<ReturnCheckAwardInfo>();
                }
                else
                {
                    int oldID = myData.AwardDic[awardType][0];
                    var tReturn = from info in _returnCheckAwardDic.Values
                                  where myData.Level >= info.LevelMin && myData.Level <= info.LevelMax && info.ID > oldID
                                  orderby info.Day
                                  select info;

                    if (tReturn.Any())
                        awardInfo = tReturn.First<ReturnCheckAwardInfo>();
                }

                if (awardInfo == null || awardInfo.ID != awardID) return EReturnAwardState.EFail;

                int spanDay = Global.GetOffsetDay(DateTime.Now) - Global.GetOffsetDay(myData.TimeReturn) + 1;
                if (awardInfo.Day > spanDay) return EReturnAwardState.EFail;

                //奖励
                List<GoodsData> awardList = new List<GoodsData>();
                if (awardInfo.DefaultGoodsList != null) awardList.AddRange(awardInfo.DefaultGoodsList);

                List<GoodsData> proGoods = GoodsHelper.GetAwardPro(client, awardInfo.ProGoodsList);
                if (proGoods != null) awardList.AddRange(proGoods);

                //背包
                if (!Global.CanAddGoodsDataList(client, awardList)) return EReturnAwardState.ENoBag;

                //记录
                string award = awardInfo.ID.ToString();
                bool result = DBUserReturnAwardUpdate(client, awardType, award);
                if (result)
                {
                    if (isNew) myData.AwardDic.Add(awardType, new int[] { awardInfo.ID });
                    else myData.AwardDic[awardType] = new int[] { awardInfo.ID };

                    //发奖
                    for (int i = 0; i < awardList.Count; i++)
                    {
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                            awardList[i].GoodsID, awardList[i].GCount, awardList[i].Quality, "", awardList[i].Forge_level,
                            awardList[i].Binding, 0, "", true, 1,
                            /**/"召回签到", Global.ConstGoodsEndTime, awardList[i].AddPropIndex, awardList[i].BornIndex,
                            awardList[i].Lucky, awardList[i].Strong, awardList[i].ExcellenceInfo, awardList[i].AppendPropLev);
                    }

                    CheckActivityTip(client);
                    return EReturnAwardState.Succ;
                }

                return EReturnAwardState.EFail;
            }
        }

        private EReturnAwardState AwardShop(GameClient client, UserReturnData myData, int awardID, int awardCount)
        {
            lock (client.ClientData.LockReturnData)
            {
                int awardType = (int)EReturnAwardType.Shop;
                bool isNew = true;
                int oldCount = 0;
                int priceNeed = 0;
                int[] oldArr = null;

                if (myData.AwardDic.ContainsKey(awardType))
                {
                    isNew = false;
                    oldArr = myData.AwardDic[awardType];
                    for (int i = 0; i < oldArr.Length; i++)
                    {
                        if (oldArr[i++] == awardID)
                        {
                            oldCount = oldArr[i];
                            break;
                        }
                    }
                }

                ReturnShopAwardInfo awardInfo = GetReturnShopAwardInfo(awardID);
                if (awardInfo == null) return EReturnAwardState.EFail;
                if (oldCount + awardCount > awardInfo.LimitCount) return EReturnAwardState.EShopMax;

                priceNeed = awardInfo.NewPrice * awardCount;
                if (priceNeed > client.ClientData.UserMoney) return EReturnAwardState.ENoMoney;

                if (!Global.CanAddGoodsNum(client, awardCount)) return EReturnAwardState.ENoBag;

                //扣钱
                bool result = GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    client, priceNeed, "召回商店", true, 1, false);
                if (!result) return EReturnAwardState.ENoMoney;

                //记录          
                List<int> list = new List<int>();
                if (isNew)
                {
                    list.Add(awardID);
                    list.Add(awardCount);
                }
                else
                {
                    bool isAdd = false;
                    for (int i = 0; i < oldArr.Length; i++)
                    {
                        int id = oldArr[i++];
                        int count = oldArr[i];

                        if (id == awardID)
                        {
                            isAdd = true;
                            count += awardCount;
                        }

                        list.Add(id);
                        list.Add(count);
                    }

                    if (!isAdd)
                    {
                        list.Add(awardID);
                        list.Add(awardCount);
                    }
                }

                string award = string.Join("*", list.ToArray<int>());
                result = DBUserReturnAwardUpdate(client, awardType, award);
                if (result)
                {
                    if (isNew) myData.AwardDic.Add(awardType, list.ToArray<int>());
                    else myData.AwardDic[awardType] = list.ToArray<int>();

                    //发奖
                    Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                        awardInfo.Goods.GoodsID, awardInfo.Goods.GCount, awardInfo.Goods.Quality, "", awardInfo.Goods.Forge_level,
                        awardInfo.Goods.Binding, 0, "", true, awardCount,
                        /**/"召回商店", Global.ConstGoodsEndTime, awardInfo.Goods.AddPropIndex, awardInfo.Goods.BornIndex,
                        awardInfo.Goods.Lucky, awardInfo.Goods.Strong, awardInfo.Goods.ExcellenceInfo, awardInfo.Goods.AppendPropLev);

                    return EReturnAwardState.Succ;
                }

                return EReturnAwardState.EFail;
            }
        }

        private EReturnAwardState AwardRecall(GameClient client, UserReturnData myData, int awardID)
        {
            lock (client.ClientData.LockReturnData)
            {
                int awardType = (int)EReturnAwardType.Recall;

                int oldState = 0;
                int[] oldArr = null;
                if (myData.AwardDic.ContainsKey(awardType))
                {
                    oldArr = myData.AwardDic[awardType];
                    for (int i = 0; i < oldArr.Length; i++)
                    {
                        if (oldArr[i++] == awardID)
                        {
                            oldState = oldArr[i];
                            break;
                        }

                        i++;
                    }
                }

                if (oldState == (int)EReturnAwardOperateState.Old) return EReturnAwardState.EIsHave;
                if (oldState == (int)EReturnAwardOperateState.CanNot) return EReturnAwardState.EFail;

                RecallAwardInfo awardInfo = GetRecallAwardInfo(awardID);
                if (awardInfo == null) return EReturnAwardState.EFail;

                var temp = from data in _returnDic.Values
                           where data.PRoleID == client.ClientData.RoleID && data.PZoneID == client.ClientData.ZoneID
                                && data.Vip >= awardInfo.Vip && data.Level >= awardInfo.Level && data.StateLog == 0
                           select data;

                if (!temp.Any()) return EReturnAwardState.EFail;
                int count = temp.Count();
                if (count < awardInfo.Count) return EReturnAwardState.EFail;

                //奖励
                List<GoodsData> awardList = new List<GoodsData>();
                if (awardInfo.DefaultGoodsList != null) awardList.AddRange(awardInfo.DefaultGoodsList);

                List<GoodsData> proGoods = GoodsHelper.GetAwardPro(client, awardInfo.ProGoodsList);
                if (proGoods != null) awardList.AddRange(proGoods);

                //背包
                if (!Global.CanAddGoodsDataList(client, awardList)) return EReturnAwardState.ENoBag;

                //记录          
                List<int> list = new List<int>();
                for (int i = 0; i < oldArr.Length; i++)
                {
                    int id = oldArr[i++];
                    int state = oldArr[i++];
                    int num = oldArr[i];

                    if (id == awardID) state = (int)EReturnAwardOperateState.Old;

                    list.Add(id);
                    list.Add(state);
                    list.Add(num);
                }


                string award = string.Join("*", list.ToArray<int>());
                bool result = DBUserReturnAwardUpdate(client, awardType, award);
                if (result)
                {
                    myData.AwardDic[awardType] = list.ToArray<int>();

                    //发奖
                    for (int i = 0; i < awardList.Count; i++)
                    {
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                            awardList[i].GoodsID, awardList[i].GCount, awardList[i].Quality, "", awardList[i].Forge_level,
                            awardList[i].Binding, 0, "", true, 1,
                            /**/"召回奖励", Global.ConstGoodsEndTime, awardList[i].AddPropIndex, awardList[i].BornIndex,
                            awardList[i].Lucky, awardList[i].Strong, awardList[i].ExcellenceInfo, awardList[i].AppendPropLev);
                    }

                    CheckActivityTip(client);
                    return EReturnAwardState.Succ;
                }

                return EReturnAwardState.EFail;
            }
        }

        #endregion

        #region ----------配置

        public bool initConfigInfo()
        {
            LoadReturnActivityInfo();
            LoadReturnAwardInfo();
            LoadReturnCheckAwardInfo();
            LoadReturnShopAwardInfo();
            LoadRecallAwardInfo();

            return true;
        }

        #region 回归活动基本信息

        public ReturnActivityInfo _returnActivityInfo = new ReturnActivityInfo();
        private void LoadReturnActivityInfo()
        {
            string fileName = Global.IsolateResPath("Config/PlayerRecall/HuoDongZhaoHui.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _returnActivityInfo = new ReturnActivityInfo();
                _returnActivityInfo.TimeSet = getUserReturnBeginTime();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    _returnActivityInfo.ID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    _returnActivityInfo.ActivityID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "HuoDongID", "0"));

                    _returnActivityInfo.TimeBegin = DateTime.Parse(Global.GetDefAttributeStr(xmlItem, "BeginTime", "1970-01-01 00:00:00"));
                    _returnActivityInfo.TimeEnd = DateTime.Parse(Global.GetDefAttributeStr(xmlItem, "FinishTime", "1970-01-01 00:00:00"));
                    _returnActivityInfo.TimeAward = _returnActivityInfo.TimeEnd.AddDays(AWARD_DAY_COUNT);

                    _returnActivityInfo.TimeBeginNoLogin = DateTime.Parse(Global.GetDefAttributeStr(xmlItem, "NotLoggedInBegin", "1970-01-01 00:00:00"));
                    _returnActivityInfo.TimeEndNoLogin = DateTime.Parse(Global.GetDefAttributeStr(xmlItem, "NotLoggedInFinish", "1970-01-01 00:00:00"));

                    string levelStr = Global.GetDefAttributeStr(xmlItem, "Level", "0,0");
                    string[] levelArr = levelStr.Split(',');
                    _returnActivityInfo.Level = int.Parse(levelArr[0]) * 100 + int.Parse(levelArr[1]);

                    _returnActivityInfo.Vip = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "VIP", "4"));

                    if (TimeUtil.NowDateTime() >= _returnActivityInfo.TimeSet
                        && TimeUtil.NowDateTime() < _returnActivityInfo.TimeAward 
                        && _returnActivityInfo.TimeSet >= _returnActivityInfo.TimeBegin 
                        && _returnActivityInfo.TimeSet < _returnActivityInfo.TimeAward)
                    {
                        _returnActivityInfo.IsOpen = true;
                        _returnActivityInfo.ActivityDay = _returnActivityInfo.TimeSet.ToString("yyyy-MM-dd");
                        DBReturnIsOpen(1);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载IsolateRes/Config/PlayerRecall/HuoDongZhaoHui.xml时出现异常!!!", ex);
            }
        }

        public bool IsUserReturnOpen()
        {
            return _returnActivityInfo.IsOpen;
        }

        #endregion

        //回归奖励
        public Dictionary<int, ReturnAwardInfo> _returnAwardDic = new Dictionary<int, ReturnAwardInfo>();
        private void LoadReturnAwardInfo()
        {
            string fileName = Global.IsolateResPath("Config/PlayerRecall/OldLogin.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _returnAwardDic = new Dictionary<int, ReturnAwardInfo>();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    ReturnAwardInfo info = new ReturnAwardInfo();
                    info.ID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    info.Vip = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MinVip", "0"));

                    string[] fields;
                    string goods = Global.GetSafeAttributeStr(xmlItem, "GoodsID1");
                    if (!string.IsNullOrEmpty(goods))
                    {
                        fields = goods.Split('|');
                        if (fields.Length > 0) info.DefaultGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                    }

                    goods = Global.GetSafeAttributeStr(xmlItem, "GoodsID2");
                    if (!string.IsNullOrEmpty(goods))
                    {
                        fields = goods.Split('|');
                        if (fields.Length > 0) info.ProGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                    }

                    _returnAwardDic.Add(info.ID, info);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载IsolateRes/Config/PlayerRecall/OldLogin.xml时出现异常!!!", ex);
            }
        }

        //回归签到
        public Dictionary<int, ReturnCheckAwardInfo> _returnCheckAwardDic = new Dictionary<int, ReturnCheckAwardInfo>();
        private void LoadReturnCheckAwardInfo()
        {
            string fileName = Global.IsolateResPath("Config/PlayerRecall/OldHuoDongLoginNumGift.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _returnCheckAwardDic = new Dictionary<int, ReturnCheckAwardInfo>();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    ReturnCheckAwardInfo info = new ReturnCheckAwardInfo();

                    info.ID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));

                    string levelStr = Global.GetDefAttributeStr(xmlItem, "Level", "");
                    if (string.IsNullOrEmpty(levelStr)) continue;
                    string[] levelArr1 = levelStr.Split('|');

                    string[] levelArr2 = levelArr1[0].Split(',');
                    info.LevelMin = Convert.ToInt32(levelArr2[0]) * 100 + Convert.ToInt32(levelArr2[1]);

                    levelArr2 = levelArr1[1].Split(',');
                    info.LevelMax = Convert.ToInt32(levelArr2[0]) * 100 + Convert.ToInt32(levelArr2[1]);

                    info.Day = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "TimeOl", "0"));

                    string[] fields;
                    string goods = Global.GetSafeAttributeStr(xmlItem, "GoodsID1");
                    if (!string.IsNullOrEmpty(goods))
                    {
                        fields = goods.Split('|');
                        if (fields.Length > 0) info.DefaultGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                    }

                    goods = Global.GetSafeAttributeStr(xmlItem, "GoodsID2");
                    if (!string.IsNullOrEmpty(goods))
                    {
                        fields = goods.Split('|');
                        if (fields.Length > 0) info.ProGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                    }

                    _returnCheckAwardDic.Add(info.ID, info);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载IsolateRes/Config/PlayerRecall/OldHuoDongLoginNumGift.xml时出错!!!文件不存在");
            }
        }

        //回归商店
        public Dictionary<int, ReturnShopAwardInfo> _returnShopAwardDic = new Dictionary<int, ReturnShopAwardInfo>();
        private void LoadReturnShopAwardInfo()
        {
            string fileName = Global.IsolateResPath("Config/PlayerRecall/OldStore.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _returnShopAwardDic = new Dictionary<int, ReturnShopAwardInfo>();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    ReturnShopAwardInfo info = new ReturnShopAwardInfo();

                    info.ID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    info.OldPrice = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "OrigPrice", "0"));
                    info.NewPrice = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Price", "0"));
                    info.LimitCount = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "SinglePurchase", "0"));

                    string goods = Global.GetSafeAttributeStr(xmlItem, "GoodsID");
                    if (!string.IsNullOrEmpty(goods)) info.Goods = GoodsHelper.ParseGoodsData(goods, fileName);

                    _returnShopAwardDic.Add(info.ID, info);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载IsolateRes/Config/PlayerRecall/OldHuoDongLoginNumGift.xml时出错!!!文件不存在");
            }
        }

        public ReturnShopAwardInfo GetReturnShopAwardInfo(int id)
        {
            if (_returnShopAwardDic.ContainsKey(id))
                return _returnShopAwardDic[id];

            return null;
        }

        //召回奖励
        public Dictionary<int, RecallAwardInfo> _recallAwardList = new Dictionary<int, RecallAwardInfo>();
        private void LoadRecallAwardInfo()
        {
            string fileName = Global.IsolateResPath("Config/PlayerRecall/RecruitOld.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _recallAwardList = new Dictionary<int, RecallAwardInfo>();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    RecallAwardInfo info = new RecallAwardInfo();
                    info.ID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    string levelStr = Global.GetDefAttributeStr(xmlItem, "MinLevel", "0,0");
                    if (string.IsNullOrEmpty(levelStr)) levelStr = "0,0";
                    string[] levelArr = levelStr.Split(',');
                    info.Level = Convert.ToInt32(levelArr[0]) * 100 + Convert.ToInt32(levelArr[1]);

                    string vipStr = Global.GetDefAttributeStr(xmlItem, "MinVip", "0");
                    if (string.IsNullOrEmpty(vipStr)) vipStr = "0";
                    info.Vip = Convert.ToInt32(vipStr);
                    info.Count = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "RecruitNum", "0"));

                    string[] fields;
                    string goods = Global.GetSafeAttributeStr(xmlItem, "GoodsID1");
                    if (!string.IsNullOrEmpty(goods))
                    {
                        fields = goods.Split('|');
                        if (fields.Length > 0) info.DefaultGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                    }

                    goods = Global.GetSafeAttributeStr(xmlItem, "GoodsID2");
                    if (!string.IsNullOrEmpty(goods))
                    {
                        fields = goods.Split('|');
                        if (fields.Length > 0) info.ProGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                    }

                    _recallAwardList.Add(info.ID, info);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载IsolateRes/Config/PlayerRecall/RecruitOld.xml时出错!!!文件不存在", ex);
            }
        }

        public RecallAwardInfo GetRecallAwardInfo(int id)
        {
            if (_recallAwardList.ContainsKey(id))
                return _recallAwardList[id];

            return null;
        }

        #endregion

        #region ----------init

        public void initUserReturnData(GameClient client)
        {
            lock (client.ClientData.LockReturnData)
            {
                UserReturnData oldData = client.ClientData.ReturnData;
                UserReturnData newData = new UserReturnData();
                if (_returnActivityInfo.IsOpen)
                {
                    newData.ActivityIsOpen = _returnActivityInfo.IsOpen;
                    newData.ActivityID = _returnActivityInfo.ActivityID;
                    newData.ActivityDay = _returnActivityInfo.ActivityDay;
                    newData.TimeBegin = _returnActivityInfo.TimeBegin;
                    newData.TimeEnd = _returnActivityInfo.TimeEnd;
                    newData.TimeAward = _returnActivityInfo.TimeAward;

                    int myPlatform = (int)GameCoreInterface.getinstance().GetPlatformType();
                    newData.MyCode = String.Format("{0}#{1}#{2}", StringUtil.IDToCode(myPlatform), StringUtil.IDToCode(client.ClientData.ZoneID), StringUtil.IDToCode(client.ClientData.RoleID));

                    initReturnData(client, newData, oldData);
                    DBUserReturnDataList(client);
                    newData.AwardDic = DBUserReturnAwardList(client);
                    CheckRecallAward(client, newData);
                }

                client.ClientData.ReturnData = newData;
                CheckActivityTip(client);
            }
        }

        private void initReturnData(GameClient client, UserReturnData newData, UserReturnData oldData)
        {
            lock (client.ClientData.LockReturnData)
            {
                ReturnData data = DBUserReturnDataGet(client);
                if (data == null) return;

                int platform = (int)GameCoreInterface.getinstance().GetPlatformType();
                newData.RecallCode = String.Format("{0}#{1}#{2}", StringUtil.IDToCode(platform), StringUtil.IDToCode(data.PZoneID), StringUtil.IDToCode(data.PRoleID));
                newData.RecallZoneID = data.PZoneID;
                newData.RecallRoleID = data.PRoleID;
                newData.Level = data.Level;
                newData.Vip = data.Vip;
                newData.TimeReturn = data.LogTime;
                newData.ZhuanSheng = data.Level / 100;
                newData.DengJi = data.Level % 100;
                switch (data.StateCheck)
                {
                    case -1:
                    case -2:
                        newData.ReturnState = data.StateCheck;
                        break;
                    case -11:
                    case -22:
                        newData.ReturnState = (int)EReturnState.EIsReturn;
                        break;
                    case 1:
                        newData.ReturnState = (int)EReturnState.Check;
                        break;
                    case 2:
                        newData.ReturnState = (int)EReturnState.CheckAndSign;
                        break;
                }
                
                if (oldData != null && oldData.ReturnState == (int)EReturnState.WaitCheck && newData.ReturnState == (int)EReturnState.Check)
                {
                    string broadcastMsg = StringUtil.substitute(Global.GetLang("欢迎【{0}】重新回到勇者大陆！！再次驰骋沙场！！"), client.ClientData.RoleName);
                    Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.HintMsg, broadcastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                }
            }
        }

        private void CheckRecallAward(GameClient client, UserReturnData newData)
        {
            lock (client.ClientData.LockReturnData)
            {
                List<int> list = null;
                if (!newData.AwardDic.ContainsKey((int)EReturnAwardType.Recall))
                {
                    list = new List<int>();
                    foreach (var info in _recallAwardList.Values)
                    {
                        list.Add(info.ID);
                        list.Add((int)EReturnAwardOperateState.CanNot);
                        list.Add(0);
                    }

                    newData.AwardDic.Add((int)EReturnAwardType.Recall, list.ToArray());
                }
                else
                {
                    list = newData.AwardDic[(int)EReturnAwardType.Recall].ToList<int>();
                }

                for (int i = 0; i < list.Count; i++)
                {
                    int id = list[i++];
                    int state = list[i];
                    if (state == (int)EReturnAwardOperateState.Old) 
                    {
                        i++;
                        continue;
                    }

                    RecallAwardInfo awardInfo = GetRecallAwardInfo(id);
                    if (awardInfo == null)
                    {
                        i++;
                        continue;
                    }

                    var temp = from data in _returnDic.Values
                               where data.PRoleID == client.ClientData.RoleID && data.PZoneID == client.ClientData.ZoneID
                                    && data.Vip >= awardInfo.Vip && data.Level >= awardInfo.Level && data.StateLog == 0
                               select data;

                    if (!temp.Any())
                    {
                        i++;
                        continue;
                    }

                    int count = temp.Count();
                    if (count >= awardInfo.Count) list[i] = (int)EReturnAwardOperateState.Can;

                    list[++i] = count;
                }

                newData.AwardDic[(int)EReturnAwardType.Recall] = list.ToArray<int>();
            }
        }

        private Dictionary<int, ReturnData> _returnDic = new Dictionary<int, ReturnData>();
        private void ReturnDicAdd(List<ReturnData> list)
        {
            foreach (ReturnData data in list)
            {
                if (_returnDic.ContainsKey(data.CRoleID))
                {
                    ReturnData oldData = _returnDic[data.CRoleID];
                    if (oldData.StateCheck == 1 || oldData.StateCheck == -2)//1=check通过，-2=sign失败
                        data.LogTime = oldData.LogTime;

                    if (oldData.StateCheck == 2 && data.StateCheck == 2)
                    {
                        data.StateLog = 0;

                        if (oldData.LogTime < data.LogTime)
                            data.LogTime = oldData.LogTime;
                    }

                    _returnDic[data.CRoleID] = data;
                }
                else
                    _returnDic.Add(data.CRoleID, data);
            }
        }

        private void ReturnDataRemove(int roleID)
        {
            if (_returnDic.ContainsKey(roleID))
                _returnDic.Remove(roleID);
        }

        private ReturnData ReturnDataGet(int roleID)
        {
            if (_returnDic.ContainsKey(roleID))
                return _returnDic[roleID];

            return null;
        }

        #endregion

        #region ----------数据库相关

        public void DBReturnIsOpen(int isOpen)
        {
            Global.sendToDB<int, int>((int)TCPGameServerCmds.CMD_DB_RETURN_IS_OPEN, isOpen, GameManager.LocalServerId);
        }

        public ReturnData DBUserReturnDataGet(GameClient client)
        {
            string cmd2db = string.Format("{0}:{1}:{2}:{3}", _returnActivityInfo.ActivityDay, _returnActivityInfo.ActivityID, client.ClientData.ZoneID, client.ClientData.RoleID);
            ReturnData result = Global.sendToDB<ReturnData, string>((int)TCPGameServerCmds.CMD_DB_RETURN_DATA, cmd2db, client.ServerId);
            ReturnDicAdd(new List<ReturnData> { result });
            return result;
        }

        public List<ReturnData> DBUserReturnDataList(GameClient client)
        {
            string cmd2db = string.Format("{0}:{1}:{2}:{3}", _returnActivityInfo.ActivityDay, _returnActivityInfo.ActivityID, client.ClientData.ZoneID, client.ClientData.RoleID);
            List<ReturnData> result = Global.sendToDB<List<ReturnData>, string>((int)TCPGameServerCmds.CMD_DB_RETURN_DATA_LIST, cmd2db, client.ServerId);
            ReturnDicAdd(result);
            return result;
        }

        public bool DBUserReturnDataUpdate(GameClient client, ReturnData data)
        {
            return Global.sendToDB<bool, ReturnData>((int)TCPGameServerCmds.CMD_DB_RETURN_DATA_UPDATE, data, client.ServerId);
        }

        public bool DBUserReturnDataDel(GameClient client, ReturnData data)
        {
            return Global.sendToDB<bool, ReturnData>((int)TCPGameServerCmds.CMD_DB_RETURN_DATA_DEL, data, client.ServerId);
        }

        public Dictionary<int, int[]> DBUserReturnAwardList(GameClient client)
        {
            string cmd2db = string.Format("{0}:{1}:{2}", _returnActivityInfo.ActivityDay, _returnActivityInfo.ActivityID, client.ClientData.RoleID);
            Dictionary<int, int[]> result = Global.sendToDB<Dictionary<int, int[]>, string>((int)TCPGameServerCmds.CMD_DB_RETURN_AWARD_LIST, cmd2db, client.ServerId);
            return result;
        }

        public bool DBUserReturnAwardUpdate(GameClient client, int awardType, string award)
        {
            string cmd2db = string.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                _returnActivityInfo.ActivityDay, _returnActivityInfo.ActivityID, client.ClientData.ZoneID, client.ClientData.RoleID, awardType, award);
            return Global.sendToDB<bool, string>((int)TCPGameServerCmds.CMD_DB_RETURN_AWARD_UPDATE, cmd2db, client.ServerId);
        }

        #endregion

        #region ----------检查活动开启

        private long _lastTicks = 0;
        public void CheckUserReturnOpenState(long ticks)
        {
            // 10秒检测一次
            if (ticks - _lastTicks < 1000 * 10)
                return;

            _lastTicks = ticks;
            UpdateUserReturnState();
        }

        public void UpdateUserReturnState()
        {
            DateTime actTime = getUserReturnBeginTime();
            DateTime nowTime = TimeUtil.NowDateTime();

            //开始
            if (!_returnActivityInfo.IsOpen
                && actTime >= _returnActivityInfo.TimeBegin
                && nowTime >= actTime
                && nowTime < _returnActivityInfo.TimeAward)
            {
                _returnActivityInfo.IsOpen = true;
                _returnActivityInfo.TimeSet = actTime;
                _returnActivityInfo.ActivityDay = actTime.ToString("yyyy-MM-dd");

                GameManager.ClientMgr.NotifyAllActivityState(3, 1, _returnActivityInfo.TimeBegin.ToString("yyyyMMddHHmmss"), _returnActivityInfo.TimeEnd.ToString("yyyyMMddHHmmss"), _returnActivityInfo.ActivityID);
                DBReturnIsOpen(1);
            }

            //结束
            if (_returnActivityInfo.IsOpen &&
                (nowTime > _returnActivityInfo.TimeAward
                || actTime > _returnActivityInfo.TimeAward
                || actTime < _returnActivityInfo.TimeBegin))
            {
                DBReturnIsOpen(0);

                _returnActivityInfo.IsOpen = false;
                _returnActivityInfo.TimeSet = DateTime.MinValue;
                _returnActivityInfo.ActivityDay = "";

                GameManager.ClientMgr.NotifyAllActivityState(3, 0);
            }
        }

        private DateTime getUserReturnBeginTime()
        {
            string dayBeginStr = GameManager.GameConfigMgr.GetGameConfigItemStr("userbegintime", "");
            if (dayBeginStr == "") return DateTime.MinValue;

            DateTime dateTime;
            DateTime.TryParse(dayBeginStr, out dateTime);

            return dateTime;
        }

        #endregion

        #region ----------感叹号提示

        public void VipChange(GameClient client)
        {
            CheckActivityTip(client);
        }

        private void CheckActivityTip(GameClient client)
        {
            lock (client.ClientData.LockReturnData)
            {
                UserReturnData myData = client.ClientData.ReturnData;
                bool isTipReturn = false;
                bool isTipRecall = false;
                int awardType = (int)EReturnAwardType.Recall;

                if (myData == null) return;

                //召回奖励          
                if (myData.AwardDic.ContainsKey(awardType))
                {
                    int[] oldArr = myData.AwardDic[awardType];
                    for (int i = 0; i < oldArr.Length; i++)
                    {
                        int id = oldArr[i++];
                        int state = oldArr[i++];
                        if (state == (int)EReturnAwardOperateState.Can)
                        {
                            isTipRecall = true;
                            break;
                        }
                    }
                }

                if (isTipRecall)
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnRecall, true);
                    //LogManager.WriteLog(LogTypes.Error, string.Format("---------------玩家召回tip：{0}={1}",ActivityTipTypes.UserReturnRecall, 1));
                }
                else
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnRecall, false);
                    //LogManager.WriteLog(LogTypes.Error, string.Format("---------------玩家召回tip：{0}={1}", ActivityTipTypes.UserReturnRecall, 0));
                }

                //被召回------------------------------------------------------------------------------------
                int oldID = 0;
                if (myData.ReturnState > (int)EReturnState.WaitCheck || myData.ReturnState == (int)EReturnState.ShowNoSign)
                {
                    //回归vip
                    awardType = (int)EReturnAwardType.Return;
                    ReturnAwardInfo returnInfo = null;
                    if (myData.AwardDic.ContainsKey(awardType) && myData.AwardDic[awardType].Length > 0) oldID = myData.AwardDic[awardType][0];

                    var tReturn = from info in _returnAwardDic.Values
                                  where info.ID > oldID
                                  orderby info.Vip
                                  select info;

                    if (tReturn.Any()) returnInfo = tReturn.First<ReturnAwardInfo>();

                    if (returnInfo != null && client.ClientData.VipLevel >= returnInfo.Vip)
                    {
                        isTipReturn = true;
                        client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnAward, true);
                        //LogManager.WriteLog(LogTypes.Error, string.Format("---------------玩家召回tip：{0}={1}", ActivityTipTypes.UserReturnAward, 1));
                    }
                    else
                    {
                        client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnAward, false);
                       // LogManager.WriteLog(LogTypes.Error, string.Format("---------------玩家召回tip：{0}={1}", ActivityTipTypes.UserReturnAward, 0));
                    }

                    //签到level
                    oldID = 0;
                    awardType = (int)EReturnAwardType.Check;
                    ReturnCheckAwardInfo checkInfo = null;
                    if (myData.AwardDic.ContainsKey(awardType) && myData.AwardDic[awardType].Length > 0) oldID = myData.AwardDic[awardType][0];

                    var tCheck = from info in _returnCheckAwardDic.Values
                                 where myData.Level >= info.LevelMin && myData.Level <= info.LevelMax && info.ID > oldID
                                 orderby info.Day
                                 select info;

                    if (tCheck.Any()) checkInfo = tCheck.First<ReturnCheckAwardInfo>();

                    int spanDay = Global.GetOffsetDay(DateTime.Now) - Global.GetOffsetDay(myData.TimeReturn) + 1;
                    if (checkInfo != null && checkInfo.Day <= spanDay)
                    {
                        isTipReturn = true;
                        client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnCheck, true);
                        //LogManager.WriteLog(LogTypes.Error, string.Format("---------------玩家召回tip：{0}={1}", ActivityTipTypes.UserReturnCheck, 1));
                    }
                    else
                    {
                        client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnCheck, false);
                        //LogManager.WriteLog(LogTypes.Error, string.Format("---------------玩家召回tip：{0}={1}", ActivityTipTypes.UserReturnCheck, 0));
                    }
                }

                if (isTipReturn || isTipRecall)
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnAll, true);
                    //LogManager.WriteLog(LogTypes.Error, string.Format("---------------玩家召回tip：{0}={1}", ActivityTipTypes.UserReturnAll, 1));
                }
                else
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.UserReturnAll, false);
                    //LogManager.WriteLog(LogTypes.Error, string.Format("---------------玩家召回tip：{0}={1}", ActivityTipTypes.UserReturnAll, 0));
                }

                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        #endregion
    }
}
