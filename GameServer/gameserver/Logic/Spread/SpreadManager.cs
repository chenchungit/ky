using GameServer.Core.GameEvent;
using GameServer.Server;
using GameServer.Tools;
using KF.Client;
using KF.Contract.Data;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tmsk.Contract;

namespace GameServer.Logic.Spread
{
    public class SpreadManager : IManager, ICmdProcessorEx, IEventListenerEx
    {

        #region ----------接口
        private static int TEL_CODE_VERIFY_SECOND = 70;
        private static int AWARD_COUNT_MAX = 10;

        public const int _sceneType = (int)SceneUIClasses.Spread;

        private static SpreadManager instance = new SpreadManager();
        public static SpreadManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            InitConfig();
            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SPREAD_SIGN, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SPREAD_AWARD, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SPREAD_VERIFY_CODE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SPREAD_TEL_CODE_GET, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SPREAD_TEL_CODE_VERIFY, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SPREAD_INFO, 1, 1, getInstance());

            GlobalEventSource4Scene.getInstance().registerListener((int)GlobalEventTypes.KFSpreadCount, (int)_sceneType, getInstance());

            return true;
        }

        public bool showdown()
        {
            GlobalEventSource4Scene.getInstance().removeListener((int)GlobalEventTypes.KFSpreadCount, (int)_sceneType, getInstance());

            return true;
        }

        public bool destroy()
        {
            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return true;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_SPREAD_INFO:
                    return ProcessSpreadInfoCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SPREAD_SIGN:
                    return ProcessSpreadSignCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SPREAD_AWARD:
                    return ProcessSpreadAwardCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SPREAD_VERIFY_CODE:
                    return ProcessSpreadVerifyCodeCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SPREAD_TEL_CODE_GET:
                    return ProcessSpreadTelCodeGetCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SPREAD_TEL_CODE_VERIFY:
                    return ProcessSpreadTelCodeVerifyCmd(client, nID, bytes, cmdParams);
            }
            return true;
        }

        public bool ProcessSpreadInfoCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                SpreadData data = GetSpreadInfo(client);
                client.sendCmd(nID, data);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessSpreadSignCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                string result = SpreadSign(client);
                client.sendCmd(nID, result);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessSpreadAwardCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                int awardType = Convert.ToInt32(cmdParams[0]);
                SpreadData data = SpreadAward(client, awardType);
                client.sendCmd(nID, data);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessSpreadVerifyCodeCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                string verifyCode = cmdParams[0];
                int result = (int)SpreadVerifyCode(client, verifyCode);
                client.sendCmd(nID, result.ToString());

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessSpreadTelCodeGetCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                string tel = cmdParams[0];
                int result = (int)SpreadTelCodeGet(client, tel);
                client.sendCmd(nID, result.ToString());

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessSpreadTelCodeVerifyCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                string telCode = cmdParams[0];
                int result = (int)SpreadTelCodeVerify(client, telCode);
                client.sendCmd(nID, result.ToString());

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public void processEvent(EventObjectEx eventObject)
        {
            int eventType = eventObject.EventType;
            switch (eventType)
            {
                case (int)GlobalEventTypes.KFSpreadCount:
                    {
                        KFNotifySpreadCountGameEvent e = eventObject as KFNotifySpreadCountGameEvent;
                        if (null != e)
                        {
                            GameClient client = GameManager.ClientMgr.FindClient(e.PRoleID);
                            if (null != client) initRoleSpreadData(client);

                            eventObject.Handled = true;
                        }
                    }
                    break;
            }
        }

        #endregion

        #region ----------功能

        private SpreadData GetSpreadInfo(GameClient client)
        {
            lock (client.ClientData.LockSpread)
            {
                SpreadData data = client.ClientData.MySpreadData;
                if (data != null) data.State = 0;

                if (IsSpreadOpen() != data.IsOpen)
                    initRoleSpreadData(client);

                return client.ClientData.MySpreadData;
            }
        }

        private string SpreadSign(GameClient client)
        {
            lock (client.ClientData.LockSpread)
            {
                string result = "{0}:{1}";

                SpreadData data = GetSpreadInfo(client);
                if (!data.IsOpen) return string.Format(result, (int)ESpreadState.ENoOpen, "");

                if (!string.IsNullOrEmpty(data.SpreadCode)) return string.Format(result, (int)ESpreadState.ESpreadIsSign, "");

                string spreadCode = GetSpreadCode(client);
                bool isCheck = HSpreadSign(client);
                if (!isCheck)
                    return string.Format(result, (int)ESpreadState.ESpreadIsSign, "");
                else
                {
                    data.SpreadCode = spreadCode;
                    Global.UpdateRoleParamByName(client, RoleParamName.SpreadCode, spreadCode, true);
                }

                return string.Format(result, (int)ESpreadState.Success, spreadCode);
            }
        }

        private SpreadData SpreadAward(GameClient client, int awardType)
        {
            lock (client.ClientData.LockSpread)
            {
                SpreadData data = GetSpreadInfo(client);
                if (!data.IsOpen)
                {
                    data.State = (int)ESpreadState.ENoOpen;
                    return data;
                }

                ESpreadState resultType = ESpreadState.Default;

                switch (awardType)
                {
                    case (int)ESpreadAward.Vip:
                        resultType = AwardOne(client, data, awardType, _awardVipInfo, data.CountVip);
                        break;
                    case (int)ESpreadAward.Level:
                        resultType = AwardOne(client, data, awardType, _awardLevelInfo, data.CountLevel);
                        break;
                    case (int)ESpreadAward.Count:
                        resultType = AwardCount(client, data);
                        break;
                    case (int)ESpreadAward.Verify:
                        resultType = AwardOne(client, data, awardType, _awardVerifyInfo, 1);
                        break;
                }

                if (resultType != ESpreadState.Success) { data.State = (int)resultType; }

                return client.ClientData.MySpreadData;
            }
        }

        private ESpreadState SpreadVerifyCode(GameClient client, string verifyCode)
        {
            lock (client.ClientData.LockSpread)
            {
                //空
                if (string.IsNullOrEmpty(verifyCode)) return ESpreadState.EVerifyCodeNull;
                //开放
                SpreadData data = GetSpreadInfo(client);
                if (!data.IsOpen) return ESpreadState.ENoOpen;
                //已验证推荐
                if (!string.IsNullOrEmpty(data.VerifyCode)) return ESpreadState.EVerifyCodeHave;
                //超出推荐时间
                DateTime regTime = Global.GetRegTime(client.ClientData);
                if (regTime < _createDate) return ESpreadState.EVerifyOutTime;
                //推荐码错误
                string[] codes = verifyCode.Split('#');
                if (codes.Length < 2) return ESpreadState.EVerifyCodeWrong;

                int pzoneID = StringUtil.SpreadCodeToID(codes[0]);
                int proleID = StringUtil.SpreadCodeToID(codes[1]);
                //自己
                if (pzoneID == client.ClientData.ZoneID && proleID == client.ClientData.RoleID) return ESpreadState.EVerifySelf;

                ESpreadState checkState = HCheckVerifyCode(client, pzoneID, proleID);
                if (checkState == ESpreadState.EVerifyCodeHave)
                {
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                      StringUtil.substitute(Global.GetLang("同一账号下只能有1个角色被推广")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);

                    return ESpreadState.EVerifyCodeHave;
                }
                else if (checkState != ESpreadState.Success) return checkState;

                SpreadVerifyData verifyData = new SpreadVerifyData();
                verifyData.VerifyCode = verifyCode;

                client.ClientData.MySpreadVerifyData = verifyData;

                return ESpreadState.Success;
            }
        }

        private ESpreadState SpreadTelCodeGet(GameClient client, string tel)
        {
            lock (client.ClientData.LockSpread)
            {
                //开放
                SpreadData spreadData = GetSpreadInfo(client);
                if (!spreadData.IsOpen) return ESpreadState.ENoOpen;
                //填写验证码
                SpreadVerifyData verifyData = client.ClientData.MySpreadVerifyData;
                if (verifyData == null || string.IsNullOrEmpty(verifyData.VerifyCode)) return ESpreadState.EVerifyCodeNull;
                //手机号空
                if (string.IsNullOrEmpty(tel)) return ESpreadState.ETelNull;
                //手机号错误
                if (!IsTel(tel)) return ESpreadState.ETelWrong;

                //手机超时&& verifyData.Tel.Equals(tel)
                if (!string.IsNullOrEmpty(verifyData.Tel)
                    && DateTime.Now < verifyData.TelTime.AddSeconds(TEL_CODE_VERIFY_SECOND))
                    return ESpreadState.Success;

                //验证手机并发送验证码
                ESpreadState result = HTelCodeGet(client, tel);
                if (result != ESpreadState.Success) return result;
                //获取失败
                if (string.IsNullOrEmpty(tel)) return ESpreadState.ETelCodeGet;

                verifyData.Tel = tel;
                verifyData.TelTime = DateTime.Now;

                return ESpreadState.Success;
            }
        }

        private ESpreadState SpreadTelCodeVerify(GameClient client, string telCode)
        {
            lock (client.ClientData.LockSpread)
            {
                //开放
                SpreadData spreadData = GetSpreadInfo(client);
                if (!spreadData.IsOpen) return ESpreadState.ENoOpen;
                //填写验证码
                SpreadVerifyData verifyData = client.ClientData.MySpreadVerifyData;
                if (verifyData == null || string.IsNullOrEmpty(verifyData.VerifyCode)) return ESpreadState.EVerifyCodeNull;
                //手机号空
                if (string.IsNullOrEmpty(verifyData.Tel)) return ESpreadState.ETelNull;
                //手机超时
                if (DateTime.Now > verifyData.TelTime.AddSeconds(TEL_CODE_VERIFY_SECOND)) return ESpreadState.ETelCodeOutTime;
                //手机code错误
                if (!IsTelCode(telCode)) return ESpreadState.ETelCodeWrong;
                //if (verifyData.TelCode<=0 || telCode != verifyData.TelCode) return ESpreadState.ETelCodeWrong;

                int code = int.Parse(telCode);
                ESpreadState result = HTelCodeVerify(client, code);
                if (result != ESpreadState.Success) return result;

                int isVip = client.ClientData.VipLevel >= _vipLimit ? 1 : 0;
                int isLevel = client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level >= _levelLimit ? 1 : 0;
                if (isVip > 0) Global.UpdateRoleParamByName(client, RoleParamName.SpreadIsVip, "1", true);
                if (isLevel > 0) Global.UpdateRoleParamByName(client, RoleParamName.SpreadIsLevel, "1", true);

                spreadData.VerifyCode = verifyData.VerifyCode;
                client.ClientData.MySpreadVerifyData = null;

                Global.UpdateRoleParamByName(client, RoleParamName.VerifyCode, verifyData.VerifyCode, true);

                return ESpreadState.Success;
            }
        }

        #endregion

        #region ----------发奖

        private ESpreadState AwardOne(GameClient client, SpreadData data, int awardType, SpreadAwardInfo awardInfo, int countSum)
        {
            lock (client.ClientData.LockSpread)
            {
                bool isAward = false;
                ESpreadState resultState = ESpreadState.Fail;
                //推广员
                switch (awardType)
                {
                    case (int)ESpreadAward.Vip:
                    case (int)ESpreadAward.Level:
                        if (string.IsNullOrEmpty(data.SpreadCode)) return ESpreadState.ESpreadNo;
                        break;
                    case (int)ESpreadAward.Verify:
                        if (string.IsNullOrEmpty(data.VerifyCode)) return ESpreadState.EVerifyNo;
                        break;
                }

                string countGetStr = "";
                int countGet = 0;
                data.AwardDic.TryGetValue(awardType, out countGetStr);
                if (!string.IsNullOrEmpty(countGetStr)) countGet = Math.Max(int.Parse(countGetStr), 0);

                //奖励数量
                int countTotal = countSum - countGet;
                if (countTotal <= 0) return ESpreadState.ENoAward;

                int num = (countTotal + 9) / 10;
                for (int n = 0; n < num; n++)
                {
                    int left = countTotal - (n * 10);
                    int count = Math.Min(left, 10);

                    //奖励
                    List<GoodsData> awardList = new List<GoodsData>();
                    if (awardInfo != null && awardInfo.DefaultGoodsList != null) awardList.AddRange(awardInfo.DefaultGoodsList);

                    List<GoodsData> proGoods = GoodsHelper.GetAwardPro(client, awardInfo.ProGoodsList);
                    if (proGoods != null) awardList.AddRange(proGoods);

                    //背包
                    if (!Global.CanAddGoodsDataList(client, awardList))
                    {
                        resultState = ESpreadState.ENoBag;
                        break;
                    }

                    //记录
                    countGet += count;
                    bool result = DBAwardUpdate(client.ClientData.ZoneID, client.ClientData.RoleID, awardType, countGet.ToString(), client.ServerId);
                    if (result)
                    {
                        if (data.AwardDic.ContainsKey(awardType))
                            data.AwardDic[awardType] = countGet.ToString();
                        else
                            data.AwardDic.Add(awardType, countGet.ToString());

                        //发奖
                        for (int i = 0; i < awardList.Count; i++)
                        {
                            Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                                awardList[i].GoodsID, awardList[i].GCount * count, awardList[i].Quality, "", awardList[i].Forge_level,
                                awardList[i].Binding, 0, "", true, 1,
                                /**/"推广", Global.ConstGoodsEndTime, awardList[i].AddPropIndex, awardList[i].BornIndex,
                                awardList[i].Lucky, awardList[i].Strong, awardList[i].ExcellenceInfo, awardList[i].AppendPropLev);
                        }

                        isAward = true;
                    }
                    else
                    {
                        resultState = ESpreadState.Fail;
                        break;
                    }
                }

                if (isAward)
                {
                    CheckActivityTip(client);
                    return ESpreadState.Success;
                }

                return resultState;
            }
        }

        private ESpreadState AwardCount(GameClient client, SpreadData data)
        {
            lock (client.ClientData.LockSpread)
            {
                bool isAward = false;
                ESpreadState resultState = ESpreadState.Fail;

                int awardType = (int)ESpreadAward.Count;
                if (string.IsNullOrEmpty(data.SpreadCode)) return ESpreadState.ESpreadNo;

                var tempDic = from dic in data.AwardCountDic
                              where dic.Value == 0 && dic.Key <= data.CountRole
                              select dic;

                if (!tempDic.Any()) return ESpreadState.ENoAward;

                Dictionary<int, int> temp = new Dictionary<int, int>();
                foreach (var d in tempDic)
                {
                    temp.Add(d.Key, d.Value);
                }

                foreach (var d in temp)
                {
                    //奖励
                    if (!_awardCountDic.ContainsKey(d.Key))
                    {
                        resultState = ESpreadState.ENoAward;
                        break;
                    }

                    SpreadCountAwardInfo awardInfo = _awardCountDic[d.Key];
                    if (awardInfo == null)
                    {
                        resultState = ESpreadState.ENoAward;
                        break;
                    }

                    List<GoodsData> awardList = new List<GoodsData>();
                    if (awardInfo != null && awardInfo.DefaultGoodsList != null) awardList.AddRange(awardInfo.DefaultGoodsList);

                    List<GoodsData> proGoods = GoodsHelper.GetAwardPro(client, awardInfo.ProGoodsList);
                    if (proGoods != null) awardList.AddRange(proGoods);

                    //背包
                    if (!Global.CanAddGoodsDataList(client, awardList))
                    {
                        resultState = ESpreadState.ENoBag;
                        break;
                    }

                    //记录
                    data.AwardCountDic[d.Key] = 1;
                    string awardString = AwardCountToStr(data.AwardCountDic);
                    bool result = DBAwardUpdate(client.ClientData.ZoneID, client.ClientData.RoleID, (int)ESpreadAward.Count, awardString, client.ServerId);
                    if (result)
                    {
                        if (data.AwardDic.ContainsKey(awardType))
                            data.AwardDic[awardType] = awardString;
                        else
                            data.AwardDic.Add(awardType, awardString);

                        //发奖
                        for (int i = 0; i < awardList.Count; i++)
                        {
                            Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                                awardList[i].GoodsID, awardList[i].GCount, awardList[i].Quality, "", awardList[i].Forge_level,
                                awardList[i].Binding, 0, "", true, 1,
                                /**/"推广", Global.ConstGoodsEndTime, awardList[i].AddPropIndex, awardList[i].BornIndex,
                                awardList[i].Lucky, awardList[i].Strong, awardList[i].ExcellenceInfo, awardList[i].AppendPropLev);
                        }

                        isAward = true;
                    }
                    else
                    {
                        resultState = ESpreadState.Fail;
                        data.AwardCountDic[d.Key] = 0;
                        break;
                    }
                }

                if (isAward)
                {
                    CheckActivityTip(client);
                    return ESpreadState.Success;
                }

                return resultState;
            }
        }

        #endregion

        #region ----------其他

        /// <summary>
        /// 角色登陆时，初始角色推广信息
        /// </summary>
        public static void initRoleSpreadData(GameClient client)
        {
            lock (client.ClientData.LockSpread)
            {
                SpreadData data = new SpreadData();
                data.IsOpen = IsSpreadOpen();

                if (!data.IsOpen)
                {
                    client.ClientData.MySpreadData = data;
                    return;
                }

                data.SpreadCode = Global.GetRoleParamByName(client, RoleParamName.SpreadCode);
                data.VerifyCode = Global.GetRoleParamByName(client, RoleParamName.VerifyCode);

                if (string.IsNullOrEmpty(data.SpreadCode) && string.IsNullOrEmpty(data.VerifyCode))
                {
                    client.ClientData.MySpreadData = data;
                    return;
                }

                if (!string.IsNullOrEmpty(data.SpreadCode))
                {
                    int[] counts = HSpreadCount(client);
                    data.CountRole = counts[0];
                    data.CountVip = Math.Min(counts[1], _vipCountMax);
                    data.CountLevel = Math.Min(counts[2], _levelCountMax);
                }

                data.AwardDic = DBAwardGet(client.ClientData.ZoneID, client.ClientData.RoleID, client.ServerId);

                string countStr = "";
                data.AwardDic.TryGetValue((int)ESpreadAward.Count, out countStr);
                data.AwardCountDic = initAwardCountDic(countStr);

                client.ClientData.MySpreadData = data;
                CheckActivityTip(client);
            }
        }

        private static Dictionary<int, int> initAwardCountDic(string awardStr)
        {
            Dictionary<int, int> dic = new Dictionary<int, int>();
            if (string.IsNullOrEmpty(awardStr))
            {
                foreach (int c in _awardCountDic.Keys)
                {
                    dic.Add(c, 0);
                }
            }
            else
            {
                string[] arr = awardStr.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    dic.Add(int.Parse(arr[i]), int.Parse(arr[++i]));
                }
            }

            return dic;
        }

        private static string AwardCountToStr(Dictionary<int, int> dic)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var d in dic)
            {
                sb.Append(string.Format("{0},{1},", d.Key, d.Value));
            }

            string str = sb.ToString();
            return str.Substring(0, str.Length - 1);
        }

        //感叹号提示
        private static void CheckActivityTip(GameClient client)
        {
            SpreadData data = client.ClientData.MySpreadData;

            bool isTip = false;
            int count = 0;
            if (!string.IsNullOrEmpty(data.SpreadCode))
            {
                if (data.CountLevel > 0)
                {
                    if (!data.AwardDic.ContainsKey((int)ESpreadAward.Level))
                        isTip = true;
                    else
                    {
                        count = int.Parse(data.AwardDic[(int)ESpreadAward.Level]);
                        if (data.CountLevel - count > 0) isTip = true;
                    }
                }

                if (data.CountVip > 0)
                {
                    if (!data.AwardDic.ContainsKey((int)ESpreadAward.Vip))
                        isTip = true;
                    else
                    {
                        var temp = from info in data.AwardDic
                                   where info.Key == (int)ESpreadAward.Vip && data.CountVip - int.Parse(info.Value) > 0
                                   select info;

                        if (temp.Any()) isTip = true;
                    }
                }

                if (data.CountRole > 0)
                {
                    var temp = from info in data.AwardCountDic
                               where info.Key <= data.CountRole && info.Value == 0
                               select info;

                    if (temp.Any()) isTip = true;
                }
            }

            //被推荐
            if (!string.IsNullOrEmpty(data.VerifyCode))
            {
                var temp = from info in data.AwardDic
                           where info.Key == (int)ESpreadAward.Verify && int.Parse(info.Value) <= 0
                           select info;

                if (temp.Any()) isTip = true;
            }

            if (isTip)
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.TipSpread, true);
            else
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.TipSpread, false);

            client._IconStateMgr.SendIconStateToClient(client);
        }

        private string GetSpreadCode(GameClient client)
        {
            int zoneID = client.ClientData.ZoneID;
            int roleID = client.ClientData.RoleID;

            return String.Format("{0}#{1}", StringUtil.SpreadIDToCode(zoneID), StringUtil.SpreadIDToCode(roleID));
        }

        /// <summary>
        /// 判断功能是否开启
        /// </summary>
        public static bool IsSpreadOpen()
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return false;

            int state = 0;

            PlatformTypes platformType = GameCoreInterface.getinstance().GetPlatformType();
            switch (platformType)
            {
                case PlatformTypes.Android:
                    state = (int)GameManager.systemParamsList.GetParamValueIntByName("TuiGuang_Android");
                    break;
                case PlatformTypes.APP:
                    state = (int)GameManager.systemParamsList.GetParamValueIntByName("TuiGuang_APP");
                    break;
                case PlatformTypes.YueYu:
                    state = (int)GameManager.systemParamsList.GetParamValueIntByName("TuiGuang_YueYu");
                    break;
            }

            return state > 0;
        }

        public void SpreadIsLevel(GameClient client)
        {
            SpreadData spreadData = GetSpreadInfo(client);
            if (spreadData == null || !spreadData.IsOpen || string.IsNullOrEmpty(spreadData.VerifyCode)) return;

            int level = client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level;
            bool isLevel = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SpreadIsLevel) > 0;

            if (level >= _levelLimit && !isLevel)
            {
                //推荐码错误
                string[] codes = spreadData.VerifyCode.Split('#');
                if (codes.Length < 2) return;

                int pzoneID = StringUtil.SpreadCodeToID(codes[0]);
                int proleID = StringUtil.SpreadCodeToID(codes[1]);

                bool result = SpreadClient.getInstance().SpreadLevel(pzoneID, proleID, client.ClientData.ZoneID, client.ClientData.RoleID);
                if (result) Global.UpdateRoleParamByName(client, RoleParamName.SpreadIsLevel, "1", true);
            }
        }

        public void SpreadIsVip(GameClient client)
        {
            SpreadData spreadData = GetSpreadInfo(client);
            if (spreadData == null || !spreadData.IsOpen || string.IsNullOrEmpty(spreadData.VerifyCode)) return;

            int vip = client.ClientData.VipLevel;
            bool isVip = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SpreadIsVip) > 0;

            if (vip >= _vipLimit && !isVip)
            {
                //推荐码错误
                string[] codes = spreadData.VerifyCode.Split('#');
                if (codes.Length < 2) return;

                int pzoneID = StringUtil.SpreadCodeToID(codes[0]);
                int proleID = StringUtil.SpreadCodeToID(codes[1]);

                bool result = SpreadClient.getInstance().SpreadVip(pzoneID, proleID, client.ClientData.ZoneID, client.ClientData.RoleID);
                if (result) Global.UpdateRoleParamByName(client, RoleParamName.SpreadIsVip, "1", true);
            }
        }

        #endregion

        #region ----------数据库相关

        //奖励
        public static Dictionary<int, string> DBAwardGet(int zoneID, int roleID, int serverID)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();

            string awardStr = "";
            string cmd2db = string.Format("{0}:{1}", zoneID, roleID);
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_SPREAD_AWARD_GET, cmd2db, serverID);
            if (null != dbFields && dbFields.Length == 1)
                awardStr = dbFields[0];

            if (!string.IsNullOrEmpty(awardStr))
            {
                string[] awardArr = awardStr.Split('$');
                foreach (string s in awardArr)
                {
                    string[] award = s.Split('#');
                    result.Add(int.Parse(award[0]), award[1]);
                }
            }

            return result;
        }

        public static bool DBAwardUpdate(int zoneID, int roleID, int awardType, string award, int serverID)
        {
            bool result = false;

            string cmd2db = string.Format("{0}:{1}:{2}:{3}", zoneID, roleID, awardType, award);
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_SPREAD_AWARD_UPDATE, cmd2db, serverID);
            if (null != dbFields && dbFields.Length == 1)
                result = (dbFields[0] == "1");

            return result;
        }

        #endregion

        #region ----------跨服中心

        public static bool HSpreadSign(GameClient client)
        {
            //return true;

            int result = SpreadClient.getInstance().SpreadSign(client.ClientData.ZoneID, client.ClientData.RoleID);
            return result > 0;
        }

        public static int[] HSpreadCount(GameClient client)
        {
            //int[] result = {10,21,5};
            int[] result = SpreadClient.getInstance().SpreadCount(client.ClientData.ZoneID, client.ClientData.RoleID);
            return result;
        }

        private ESpreadState HCheckVerifyCode(GameClient client, int pzoneID, int proleID)
        {
            //return ESpreadState.Success;

            int isVip = client.ClientData.VipLevel >= _vipLimit ? 1 : 0;
            int isLevel = client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level >= _levelLimit ? 1 : 0;
            int result = SpreadClient.getInstance().CheckVerifyCode(client.strUserID, client.ClientData.ZoneID, client.ClientData.RoleID, pzoneID, proleID, isVip, isLevel);
            return (ESpreadState)result;
        }

        private ESpreadState HTelCodeGet(GameClient client, string tel)
        {
            //return ESpreadState.Success;

            int result = SpreadClient.getInstance().TelCodeGet(client.ClientData.ZoneID, client.ClientData.RoleID, tel);
            return (ESpreadState)result;
        }

        private ESpreadState HTelCodeVerify(GameClient client, int telCode)
        {
            //return ESpreadState.Success;

            int result = SpreadClient.getInstance().TelCodeVerify(client.ClientData.ZoneID, client.ClientData.RoleID, telCode);
            return (ESpreadState)result;
        }

        #endregion

        #region ----------辅助

        private bool IsTel(string tel)
        {
            //return Regex.IsMatch(tel.ToString(), "^\\d{11}$");
            return Regex.IsMatch(tel.ToString(), @"^(0|86|17951)?(1[0-9])[0-9]{9}$");
        }

        private bool IsTelCode(string tel)
        {
            return Regex.IsMatch(tel.ToString(), "^\\d{6}$");
        }

        #endregion

        #region ----------配置

        private static Dictionary<int, SpreadCountAwardInfo> _awardCountDic = new Dictionary<int, SpreadCountAwardInfo>();

        private static int _levelLimit = 0;
        private static SpreadAwardInfo _awardLevelInfo = new SpreadAwardInfo();

        private static int _vipLimit = 0;
        private static SpreadAwardInfo _awardVipInfo = new SpreadAwardInfo();

        private static SpreadAwardInfo _awardVerifyInfo = new SpreadAwardInfo();

        private static DateTime _createDate = DateTime.MinValue;

        private static int _vipCountMax = 0;
        private static int _levelCountMax = 0;

        private static int VIP_LEVEL_COUNT_MAX_DEFAULT = 50;

        private static bool InitConfig()
        {
            string fileName = "";
            string[] fields;
            string goods = "";

            try
            {
                #region ----------count

                _awardCountDic.Clear();
                fileName = Global.IsolateResPath("Config/TuiGuang/TuiGuangYuanLeiJi.xml");

                XElement xml = CheckHelper.LoadXml(fileName);
                if (null == xml) return false;

                XElement args = xml.Element("GiftList");
                if (null == args) return false;

                IEnumerable<XElement> xmlItems = args.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    SpreadCountAwardInfo info = new SpreadCountAwardInfo();
                    info.Count = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MinNum", "0"));


                    goods = Global.GetSafeAttributeStr(xmlItem, "GoodsOne");
                    if (!string.IsNullOrEmpty(goods))
                    {
                        fields = goods.Split('|');
                        if (fields.Length > 0)
                            info.DefaultGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                    }

                    goods = Global.GetSafeAttributeStr(xmlItem, "GoodsTwo");
                    if (!string.IsNullOrEmpty(goods))
                    {
                        fields = goods.Split('|');
                        if (fields.Length > 0)
                            info.ProGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                    }

                    _awardCountDic.Add(info.Count, info);
                }

                #endregion

                #region ----------level

                _levelLimit = 0;
                _awardLevelInfo = new SpreadAwardInfo();

                fileName = Global.IsolateResPath("Config/TuiGuang/TuiGuangYuanLevel.xml");

                xml = CheckHelper.LoadXml(fileName);
                if (null == xml) return false;

                args = xml.Element("TuiGuangYuanLevel");
                if (null == args) return false;

                int zhuanSheng = Convert.ToInt32(Global.GetDefAttributeStr(args, "MinZhuanSheng", "0"));
                int level = Convert.ToInt32(Global.GetDefAttributeStr(args, "MinLevel", "0"));
                _levelLimit = zhuanSheng * 100 + level;

                args = xml.Element("GiftList").Element("Award");
                if (null == args) return false;

                goods = Global.GetSafeAttributeStr(args, "GoodsOne");
                if (!string.IsNullOrEmpty(goods))
                {
                    fields = goods.Split('|');
                    if (fields.Length > 0)
                        _awardLevelInfo.DefaultGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                }

                goods = Global.GetSafeAttributeStr(args, "GoodsTwo");
                if (!string.IsNullOrEmpty(goods))
                {
                    fields = goods.Split('|');
                    if (fields.Length > 0)
                        _awardLevelInfo.ProGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                }

                #endregion

                #region ----------vip

                _vipLimit = 0;
                _awardVipInfo = new SpreadAwardInfo();

                fileName = Global.IsolateResPath("Config/TuiGuang/TuiGuangYuanVip.xml");

                xml = CheckHelper.LoadXml(fileName);
                if (null == xml) return false;

                args = xml.Element("TuiGuangYuanVip");
                if (null == args) return false;

                _vipLimit = Convert.ToInt32(Global.GetDefAttributeStr(args, "VipLevel", "0"));

                args = xml.Element("GiftList").Element("Award");
                if (null == args) return false;

                goods = Global.GetSafeAttributeStr(args, "GoodsOne");
                if (!string.IsNullOrEmpty(goods))
                {
                    fields = goods.Split('|');
                    if (fields.Length > 0)
                        _awardVipInfo.DefaultGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                }

                goods = Global.GetSafeAttributeStr(args, "GoodsTwo");
                if (!string.IsNullOrEmpty(goods))
                {
                    fields = goods.Split('|');
                    if (fields.Length > 0)
                        _awardVipInfo.ProGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                }

                #endregion

                #region ----------verify

                _awardVerifyInfo = new SpreadAwardInfo();

                fileName = Global.IsolateResPath("Config/TuiGuang/TuiGuangXinYongHu.xml");

                xml = CheckHelper.LoadXml(fileName);
                if (null == xml) return false;

                args = xml.Element("GiftList").Element("Award");
                if (null == args) return false;

                goods = Global.GetSafeAttributeStr(args, "GoodsOne");
                if (!string.IsNullOrEmpty(goods))
                {
                    fields = goods.Split('|');
                    if (fields.Length > 0)
                        _awardVerifyInfo.DefaultGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                }

                goods = Global.GetSafeAttributeStr(args, "GoodsTwo");
                if (!string.IsNullOrEmpty(goods))
                {
                    fields = goods.Split('|');
                    if (fields.Length > 0)
                        _awardVerifyInfo.ProGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                }

                #endregion

                string createDate = GameManager.systemParamsList.GetParamValueByName("TuiGuangCreatData");
                _createDate = DateTime.Parse(createDate);

                _vipCountMax = (int)GameManager.systemParamsList.GetParamValueIntByName("TuiGuangVIPRewardNum", VIP_LEVEL_COUNT_MAX_DEFAULT);
                _levelCountMax = (int)GameManager.systemParamsList.GetParamValueIntByName("TuiGuangLevelRewardNum", VIP_LEVEL_COUNT_MAX_DEFAULT);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载xml配置文件:{0}, 失败。", fileName), ex);
                return false;
            }

            return true;
        }

        #endregion

        #region ----------GM

        public void SpreadSetCount(GameClient client, int[] counts)
        {
            SpreadData data = GetSpreadInfo(client);

            data.CountRole = counts[0];
            data.CountVip = Math.Min(counts[1], _vipCountMax);
            data.CountLevel = Math.Min(counts[2], _levelCountMax);

            client.ClientData.MySpreadData = data;
        }

        #endregion
    }
}
