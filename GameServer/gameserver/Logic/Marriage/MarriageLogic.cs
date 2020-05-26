using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using GameServer.Server;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using Server.Data;
using GameServer.Core.Executor;
using Tmsk.Contract;
using GameServer.Logic.Marriage.CoupleArena;
using GameServer.Logic.Marriage.CoupleWish;

namespace GameServer.Logic
{
    public enum MarryResult
    {
        Success,              //成功

        SelfMarried,          //已结婚
        SelfLevelNotEnough,   //转生等级不足
        SelfBusy,             //繁忙
        TargetMarried,        //求婚对象已结婚
        TargetBusy,           //对方忙
        TargetLevelNotEnough, //对方转生等级不足
        TargetOffline,        //离线
        InvalidSex,           //性別不符
        ApplyCD,              //求婚cd
        ApplyTimeout,         //求婚超时
        MoneyNotEnough,       //缺少金钱
        NotMarried,           //没有结婚
        AutoReject,           //自动拒绝

        NotOpen,              //结婚功能没有开启
        TargetNotOpen,        //对方的结婚功能没有开启
        DeniedByCoupleAreanTime, // 情侣竞技活动时间不能离婚
    }

    public enum MarryNotifyType
    {
        NotifyInit,          //通知求婚
        NotifyRejectInit,    //通知拒绝结婚
        //NotifyRejectDivorce, //通知拒绝离婚
    }

    public enum MarryApplyType
    {
        ApplyNull,
        ApplyInit,    //申请结婚
        ApplyDivorce, //申请离婚
    }

    public enum MarryDivorceType
    {
        DivorceForce,      //申请强制离婚
        DivorceFree,       //申请自由离婚
        DivorceFreeAccept, //回复同意离婚
        DivorceFreeReject, //回复拒绝离婚      
    }

    public class MarryApplyData
    {
        public long ApplyExpireTime = 0; // 0代表已返还
        public long ApplyCDEndTime = 0;
        public int ApplySpouseRoleID = -1; // 申请对像的角色id
        public MarryApplyType ApplyType = MarryApplyType.ApplyNull;
    }

    class MarryLogic
    {
        #region 成员变量
        public static Dictionary<int, MarryApplyData> MarryApplyList = new Dictionary<int, MarryApplyData>();
        public static long NextPeriodicCheckTime = 0;

        /// <summary>
        /// 配置数据
        /// </summary>
        private static int MarryCost;
        private static int MarryCD;
        private static int MarryReplyTime;
        private static int DivorceCost;
        private static int DivorceForceCost;
        #endregion

        #region 配置文件
        public static void LoadMarryBaseConfig()
        {
            MarryCost = Convert.ToInt32(GameManager.systemParamsList.GetParamValueByName("JieHunCost"));
            MarryCD = Convert.ToInt32(GameManager.systemParamsList.GetParamValueByName("QiuHuiCD"));
            MarryReplyTime = Convert.ToInt32(GameManager.systemParamsList.GetParamValueByName("MarriageTipsTime"));
            DivorceCost = Convert.ToInt32(GameManager.systemParamsList.GetParamValueByName("DivorceJinBiCost"));
            DivorceForceCost = Convert.ToInt32(GameManager.systemParamsList.GetParamValueByName("DivorceZuanShiCost"));
        }
        #endregion

        #region 是否开放结婚系统
        /// <summary>
        /// 是否开放结婚系统 [bing] 2015.6.16
        /// </summary>
        public static bool IsVersionSystemOpenOfMarriage()
        {
            // 如果1.5的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot5))
            {
                return false;
            }
            return GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.Marriage);
        }
        #endregion

        #region MarryApplyList逻辑
        public static MarryApplyData AddMarryApply(int roleID, MarryApplyType type, int spouseID)
        {
            MarryApplyData data = null;

            lock (MarryApplyList)
            {
                if (MarryApplyList.ContainsKey(roleID) == false)
                {
                    data = new MarryApplyData()
                    {
                        ApplyExpireTime = TimeUtil.NOW() + MarryReplyTime * 1000,
                        ApplyCDEndTime = 0,
                        ApplySpouseRoleID = spouseID,
                        ApplyType = type,
                        //IsMoneyReturn = 0,
                    };
                    if (type == MarryApplyType.ApplyInit)
                    {
                        data.ApplyCDEndTime = TimeUtil.NOW() + MarryCD * 1000;
                    }
                    else
                    {
                        data.ApplyCDEndTime = data.ApplyExpireTime;
                    }
                    MarryApplyList.Add(roleID, data);
                }
            }

            return data;
        }

        public static bool RemoveMarryApply(int roleID, MarryApplyType type = MarryApplyType.ApplyNull)
        {
            lock (MarryApplyList)
            {
                if (type == MarryApplyType.ApplyNull)
                {
                    return MarryApplyList.Remove(roleID);
                }
                else
                {
                    MarryApplyData applyData;
                    bool ret = MarryApplyList.TryGetValue(roleID, out applyData);
                    if (ret == true)
                    {
                        if (applyData.ApplyType != type)
                        {
                            ret = false;
                        }
                        else if (applyData.ApplyExpireTime == 0)
                        {
                            ret = false;
                        }
                        else
                        {
                            if (applyData.ApplyExpireTime <= TimeUtil.NOW())
                            {
                                ret = false;
                            }
                            else
                            {
                                // 符合条件才清0，外面逻辑注意是否要返还货币
                                applyData.ApplyExpireTime = 0;
                            }
                        }
                    }
                    return ret;
                }
            }
        }

        public static void ApplyPeriodicClear(long ticks)
        {
            if (ticks < NextPeriodicCheckTime)// || -1 != ticks)
            {
                return;
            }
            NextPeriodicCheckTime = ticks + 1000 * 10;

            lock (MarryApplyList)
            {
                foreach (var it in MarryApplyList.ToList())
                {
                    MarryApplyData applyData = it.Value;
                    if (applyData.ApplyExpireTime > 0 && applyData.ApplyExpireTime <= ticks)
                    {
                        ApplyReturnMoney(it.Key, applyData);
                        applyData.ApplyExpireTime = 0;
                    }

                    if (applyData.ApplyCDEndTime <= ticks)
                    {
                        MarryApplyList.Remove(it.Key);
                    }
                }
            }
        }

        public static void ApplyShutdownClear()
        {
            lock (MarryApplyList)
            {
                foreach (var it in MarryApplyList)
                {
                    MarryApplyData applyData = it.Value;
                    if (applyData.ApplyExpireTime > 0)
                    {
                        ApplyReturnMoney(it.Key, applyData);
                    }
                }
                MarryApplyList.Clear();
            }
        }

        public static void ApplyLogoutClear(GameClient client)
        {
            MarryApplyData applyData;
            lock (MarryApplyList)
            {
                if (MarryApplyList.TryGetValue(client.ClientData.RoleID, out applyData) == true)
                {
                    if (applyData.ApplyExpireTime > 0)
                    {
                        ApplyReturnMoney(0, applyData, client);
                        applyData.ApplyExpireTime = 0;
                    }
                }
            }
        }

        public static void ApplyReturnMoney(int roleID, MarryApplyData applyData, GameClient client = null)
        {
            if (client == null)
            {
                client = GameManager.ClientMgr.FindClient(roleID);
            }
            if (client != null)
            {
                if (applyData.ApplyType == MarryApplyType.ApplyInit)
                {
                    GameManager.ClientMgr.AddUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, MarryCost, "求婚超时返回钻石");
                }
                else if (applyData.ApplyType == MarryApplyType.ApplyDivorce)
                {
                    GameManager.ClientMgr.AddMoney1(client, DivorceCost, "离婚超时返还绑金", true);
                }
            }
        }

        // 是否申请或被申请求婚和离婚
        public static bool ApplyExist(int roleID)
        {
            foreach (KeyValuePair<int, MarryApplyData> kv in MarryApplyList)
            {
                if (roleID == kv.Value.ApplySpouseRoleID || roleID == kv.Key)
                {
                    if (kv.Value.ApplyExpireTime > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region 协议处理
        public static TCPProcessCmdResults ProcessMarryInit(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "", nID);
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "", nID);
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int spouseID = Convert.ToInt32(fields[1]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "", nID);
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.ClientSocket.IsKuaFuLogin)
                {
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}", (int)StdErrorCode.Error_Operation_Denied, roleID, spouseID));
                    tcpOutPacket = null;
                    return TCPProcessCmdResults.RESULT_OK;
                }

                MarryResult result = MarryInit(client, spouseID);

                string strcmd = string.Format("{0}:{1}:{2}", (int)result, roleID, spouseID);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "", nID);
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ProcessMarryReply(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int sourceID = Convert.ToInt32(fields[1]);
                int accept = Convert.ToInt32(fields[2]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                MarryResult result = MarryReply(client, sourceID, accept);

                string strcmd = string.Format("{0}:{1}:{2}", (int)result, roleID, sourceID);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ProcessMarryDivorce(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int divorceType = Convert.ToInt32(fields[1]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                MarryResult result = MarryDivorce(client, (MarryDivorceType)divorceType);

                string strcmd = string.Format("{0}", (int)result);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessMarryPartyCancel", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ProcessMarryAutoReject(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int autoReject = Convert.ToInt32(fields[1]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                MarryResult result = MarryAutoReject(client, autoReject);

                string strcmd = string.Format("{0}:{1}", (int)result, client.ClientData.MyMarriageData.byAutoReject);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessMarryPartyCancel", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion

        #region 主要逻辑
        /// <summary>
        /// 求婚 
        /// </summary>
        public static MarryResult MarryInit(GameClient client, int spouseID)
        {
            if (GlobalNew.IsGongNengOpened(client, GongNengIDs.Marriage, true) == false
                || !MarryLogic.IsVersionSystemOpenOfMarriage())
            {
                return MarryResult.NotOpen;
            }
            if (client.ClientData.MyMarriageData.byMarrytype > 0)
            {
                return MarryResult.SelfMarried;
            }
            if (client.ClientData.ChangeLifeCount < 3)
            {
                return MarryResult.SelfLevelNotEnough;
            }
            if (client.ClientData.ExchangeID > 0 || true == client.ClientSocket.IsKuaFuLogin || client.ClientData.CopyMapID > 0)
            {
                return MarryResult.SelfBusy;
            }

            GameClient spouseClient = GameManager.ClientMgr.FindClient(spouseID);
            if (spouseClient == null)
            {
                return MarryResult.TargetOffline;
            }
            if (GlobalNew.IsGongNengOpened(spouseClient, GongNengIDs.Marriage) == false)
            {
                return MarryResult.TargetNotOpen;
            }
            if (client.ClientData.RoleSex == spouseClient.ClientData.RoleSex)
            {
                return MarryResult.InvalidSex;
            }
            if (spouseClient.ClientData.MyMarriageData.byMarrytype > 0)
            {
                return MarryResult.TargetMarried;
            }
            if (spouseClient.ClientData.ChangeLifeCount < 3)
            {
                return MarryResult.TargetLevelNotEnough;
            }
            if (spouseClient.ClientData.ExchangeID > 0 || true == spouseClient.ClientSocket.IsKuaFuLogin || spouseClient.ClientData.CopyMapID > 0)
            {
                return MarryResult.TargetBusy;
            }
            if (ApplyExist(spouseID) == true)
            {
                return MarryResult.TargetBusy;
            }

            //如果自动拒绝结婚
            if (spouseClient.ClientData.MyMarriageData.byAutoReject == (sbyte)1)
            {
                return MarryResult.AutoReject;
            }
            if (AddMarryApply(client.ClientData.RoleID, MarryApplyType.ApplyInit, spouseID) == null)
            {
                return MarryResult.ApplyCD;
            }

            //能求婚才会扣除钻石
            if (GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, MarryCost, "结婚", false) == false)
            {
                RemoveMarryApply(client.ClientData.RoleID);
                return MarryResult.MoneyNotEnough;
            }

            string notifyData = string.Format("{0}:{1}:{2}", (int)MarryNotifyType.NotifyInit, client.ClientData.RoleID, client.ClientData.RoleName);
            spouseClient.sendCmd((int)TCPGameServerCmds.CMD_SPR_MARRY_NOTIFY, notifyData);

            return MarryResult.Success;
        }

        /// <summary>
        /// 对方回复求婚
        /// </summary>
        public static MarryResult MarryReply(GameClient client, int sourceID, int accept)
        {
            if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                return MarryResult.NotOpen;

            if (client.ClientData.MyMarriageData.byMarrytype > 0)
            {
                return MarryResult.SelfMarried;
            }

            GameClient sourceClient = GameManager.ClientMgr.FindClient(sourceID);
            if (sourceClient == null)
            {
                return MarryResult.ApplyTimeout;
            }
            if (sourceClient.ClientData.MyMarriageData.byMarrytype > 0)
            {
                return MarryResult.TargetMarried;
            }

            if (RemoveMarryApply(sourceID, MarryApplyType.ApplyInit) == false)
            {
                return MarryResult.ApplyTimeout;
            }

            if (accept == 0 || client.ClientData.MyMarriageData.byAutoReject == 1)
            {
                // 拒绝返钱
                string notifyData = string.Format("{0}:{1}:{2}", (int)MarryNotifyType.NotifyRejectInit, client.ClientData.RoleID, client.ClientData.RoleName);
                sourceClient.sendCmd((int)TCPGameServerCmds.CMD_SPR_MARRY_NOTIFY, notifyData);

                GameManager.ClientMgr.AddUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, sourceClient, MarryCost, "求婚被拒绝返还钻石");
            }
            else
            {
                // 同意
                RemoveMarryApply(sourceID);

                // 如果自己在申请狀态，需取消返钱
                MarryLogic.ApplyLogoutClear(client);
                RemoveMarryApply(client.ClientData.RoleID);

                // 初始婚戒
                int initRingID = 0;
                if (null != MarriageOtherLogic.getInstance().WeddingRingDic.SystemXmlItemDict)
                {
                    initRingID = MarriageOtherLogic.getInstance().WeddingRingDic.SystemXmlItemDict.Keys.First();
                }
                if (sourceClient.ClientData.MyMarriageData.nRingID <= 0)
                {
                    sourceClient.ClientData.MyMarriageData.nRingID = initRingID;
                }
                if (client.ClientData.MyMarriageData.nRingID <= 0)
                {
                    client.ClientData.MyMarriageData.nRingID = initRingID;
                }

                // 更新婚姻狀态
                // marry type 1:丈夫 2:妻子
                sbyte sourceType = (sourceClient.ClientData.RoleSex != 1 || client.ClientData.RoleSex == sourceClient.ClientData.RoleSex)? (sbyte)1 : (sbyte)2;
                sourceClient.ClientData.MyMarriageData.byMarrytype = sourceType;
                client.ClientData.MyMarriageData.byMarrytype = (sourceType == 1)? (sbyte)2 : (sbyte)1;

                // 更新伴侶role id
                sourceClient.ClientData.MyMarriageData.nSpouseID = client.ClientData.RoleID;
                client.ClientData.MyMarriageData.nSpouseID = sourceID;

                // 初始化0星1阶 [bing] 因为再结婚还会走这个函数就会被初始化为1阶 应该不初始化它 是个bug 这里FIX下
                if (sourceClient.ClientData.MyMarriageData.byGoodwilllevel == 0)
                {
                    //[bing] 更新时间
                    sourceClient.ClientData.MyMarriageData.ChangTime = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");
                    sourceClient.ClientData.MyMarriageData.byGoodwilllevel = 1;
                }
                if (client.ClientData.MyMarriageData.byGoodwilllevel == 0)
                {
                    client.ClientData.MyMarriageData.ChangTime = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");
                    client.ClientData.MyMarriageData.byGoodwilllevel = 1;
                }

                MarryFuBenMgr.UpdateMarriageData2DB(sourceClient);
                MarryFuBenMgr.UpdateMarriageData2DB(client);

                MarriageOtherLogic.getInstance().SendMarriageDataToClient(sourceClient);
                MarriageOtherLogic.getInstance().SendMarriageDataToClient(client);

                //更新婚戒属性
                MarriageOtherLogic.getInstance().UpdateRingAttr(sourceClient, true);

                //[bing] 刷新客户端活动叹号
                if (client._IconStateMgr.CheckJieRiFanLi(client, ActivityTypes.JieriMarriage) == true)
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                    client._IconStateMgr.SendIconStateToClient(client);
                }
                //[bing] 刷新客户端活动叹号
                if (sourceClient._IconStateMgr.CheckJieRiFanLi(sourceClient, ActivityTypes.JieriMarriage) == true)
                {
                    sourceClient._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, sourceClient._IconStateMgr.IsAnyJieRiTipActived());
                    sourceClient._IconStateMgr.SendIconStateToClient(sourceClient);
                }

                // 好友逻辑
                FriendData friendData = Global.FindFriendData(client, sourceID);
                if (friendData != null && friendData.FriendType != 0)
                {
                    GameManager.ClientMgr.RemoveFriend(Global._TCPManager, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                        client, friendData.DbID);
                    friendData = null;
                }
                if (friendData == null)
                {
                    GameManager.ClientMgr.AddFriend(Global._TCPManager, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                        client, -1, sourceID, Global.FormatRoleName(sourceClient, sourceClient.ClientData.RoleName), 0);
                }

                friendData = Global.FindFriendData(sourceClient, client.ClientData.RoleID);
                if (friendData != null && friendData.FriendType != 0)
                {
                    GameManager.ClientMgr.RemoveFriend(Global._TCPManager, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                        sourceClient, friendData.DbID);
                    friendData = null;
                }
                if (friendData == null)
                {
                    GameManager.ClientMgr.AddFriend(Global._TCPManager, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                        sourceClient, -1, client.ClientData.RoleID, Global.FormatRoleName(client, client.ClientData.RoleName), 0);
                }

                //扩播消息
                string broadCastMsg = string.Format(Global.GetLang("恭喜 {0} 和 {1} 喜结连理！"), sourceClient.ClientData.RoleName, client.ClientData.RoleName);
                Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.Bulletin, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);

                CoupleArenaManager.Instance().OnMarry(sourceClient, client);
            }

            return MarryResult.Success;
        }

        /// <summary>
        /// 离婚 
        /// </summary>
        public static MarryResult MarryDivorce(GameClient client, MarryDivorceType divorceType)
        {
            if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                return MarryResult.NotOpen;

            if (0 >= client.ClientData.MyMarriageData.byMarrytype)
            {
                return MarryResult.NotMarried;
            }

            if (!CoupleArenaManager.Instance().IsNowCanDivorce(TimeUtil.NowDateTime()))
            {
                return MarryResult.DeniedByCoupleAreanTime;
            }

            int spouseID = client.ClientData.MyMarriageData.nSpouseID;
            GameClient spouseClient = GameManager.ClientMgr.FindClient(spouseID);

            if (divorceType == MarryDivorceType.DivorceForce || divorceType == MarryDivorceType.DivorceFree || divorceType == MarryDivorceType.DivorceFreeAccept)
            {
                if (client.ClientData.ExchangeID > 0 || true == client.ClientSocket.IsKuaFuLogin || client.ClientData.CopyMapID > 0)
                {
                    return MarryResult.SelfBusy;
                }
                if (-1 != client.ClientData.FuBenID && MapTypes.MarriageCopy == Global.GetMapType(client.ClientData.MapCode))
                {
                    return MarryResult.SelfBusy;
                }
                if (null != spouseClient)
                {
                    if (-1 != spouseClient.ClientData.FuBenID && MapTypes.MarriageCopy == Global.GetMapType(spouseClient.ClientData.MapCode))
                    {
                        return MarryResult.TargetBusy;
                    }
                }

                if (divorceType == MarryDivorceType.DivorceForce || divorceType == MarryDivorceType.DivorceFree)
                {
                    if (true == ApplyExist(client.ClientData.RoleID))
                    {
                        return MarryResult.SelfBusy;
                    }
                }
            }

            int _man = client.ClientData.RoleID, _wife = spouseID;
            if (client.ClientData.MyMarriageData.byMarrytype == 2)
            {
                DataHelper2.Swap(ref _man, ref _wife); ;
            }

            if (divorceType == MarryDivorceType.DivorceForce)
            {
                if (client.ClientData.UserMoney < DivorceForceCost)
                {
                    return MarryResult.MoneyNotEnough;
                }

                // 情侣竞技和情侣祝福都需要离婚清除数据，所以，可能会导致部分清除失败，由于竞技更要求数据的准确性，所以先清除情侣祝福，再清楚情侣竞技

                // 情侣祝福排行版通知清除排行数据
                // 必须保证先清除成功，才能强制离婚离婚
                if (!CoupleWishManager.Instance().PreClearDivorceData(_man, _wife))
                {
                    return MarryResult.NotOpen;
                }

                // 情侣竞技通知中心竞技、排行数据，dbserver清除战报数据
                // 必须保证先清除成功，才能强制离婚离婚
                if (!CoupleArenaManager.Instance().PreClearDivorceData(_man, _wife))
                {
                    return MarryResult.NotOpen;
                }

                //强制离婚
                if (false == GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, DivorceForceCost, "强制离婚", false))
                {
                   // return MarryResult.MoneyNotEnough;
                }

                client.ClientData.MyMarriageData.byMarrytype = -1;
                client.ClientData.MyMarriageData.nSpouseID = -1;
                MarryFuBenMgr.UpdateMarriageData2DB(client);
                MarriageOtherLogic.getInstance().ResetRingAttr(client);

                if (null != spouseClient)
                {
                    spouseClient.ClientData.MyMarriageData.nSpouseID = -1;
                    spouseClient.ClientData.MyMarriageData.byMarrytype = -1;
                    MarryFuBenMgr.UpdateMarriageData2DB(spouseClient);
                    MarriageOtherLogic.getInstance().ResetRingAttr(spouseClient);

                    //[bing] 给配偶发送CMD_SPR_MARRY_UPDATE
                    MarriageOtherLogic.getInstance().SendMarriageDataToClient(spouseClient);

                    //[bing] 刷新客户端活动叹号
                    if (spouseClient._IconStateMgr.CheckJieRiFanLi(spouseClient, ActivityTypes.JieriMarriage) == true)
                    {
                        spouseClient._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, spouseClient._IconStateMgr.IsAnyJieRiTipActived());
                        spouseClient._IconStateMgr.SendIconStateToClient(spouseClient);
                    }
                }
                else
                {
                    string tcpstring = string.Format("{0}", spouseID);
                    MarriageData spouseMarriageData = Global.sendToDB<MarriageData, string>((int)TCPGameServerCmds.CMD_DB_GET_MARRY_DATA, tcpstring, client.ServerId);

                    if (null != spouseMarriageData && 0 < spouseMarriageData.byMarrytype)
                    {
                        spouseMarriageData.byMarrytype = -1;
                        spouseMarriageData.nSpouseID = -1;
                        MarryFuBenMgr.UpdateMarriageData2DB(spouseID, spouseMarriageData, client);
                    }
                }

                // 取消婚宴
                MarryPartyLogic.getInstance().MarryPartyRemove(client.ClientData.RoleID, true, client);
                MarryPartyLogic.getInstance().MarryPartyRemove(spouseID, true, client);

                //[bing] 给自己发送CMD_SPR_MARRY_UPDATE
                MarriageOtherLogic.getInstance().SendMarriageDataToClient(client);

                //[bing] 刷新客户端活动叹号
                if (client._IconStateMgr.CheckJieRiFanLi(client, ActivityTypes.JieriMarriage) == true)
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                    client._IconStateMgr.SendIconStateToClient(client);
                }

                // send mail
                string msg = string.Format(Global.GetLang("在您收到这封邮件的时候，您的伴侣{0}已经申请与您强制离婚，现在您已经恢复单身。您的定情信物将被封存，所有属性暂时失效，直至下次结婚时生效。最后，预祝您再次觅得真心人，相伴共度美好的游戏时光。感谢您阅读这封邮件。"), client.ClientData.RoleName);
                SendDivorceMail(spouseID, Global.GetLang("离婚"), msg, spouseClient, client.ServerId);

                CoupleArenaManager.Instance().OnDivorce(client.ClientData.RoleID, spouseID);
            }
            else if (divorceType == MarryDivorceType.DivorceFree)
            {
                //申请离婚
                if (null == spouseClient)
                {
                    return MarryResult.TargetOffline;
                }
                if (spouseClient.ClientData.ExchangeID > 0 || true == spouseClient.ClientSocket.IsKuaFuLogin || spouseClient.ClientData.CopyMapID > 0)
                {
                    return MarryResult.TargetBusy;
                }

                if (Global.GetTotalBindTongQianAndTongQianVal(client) < DivorceCost)
                {
                    return MarryResult.MoneyNotEnough;
                }
                if (Global.SubBindTongQianAndTongQian(client, DivorceCost, "申请离婚") == false)
                {
                    return MarryResult.MoneyNotEnough;
                }

                AddMarryApply(client.ClientData.RoleID, MarryApplyType.ApplyDivorce, spouseID);

                //发送离婚申请
                string notifyData = string.Format("{0}:{1}", client.ClientData.RoleID, (int)MarryDivorceType.DivorceFree);
                spouseClient.sendCmd((int)TCPGameServerCmds.CMD_SPR_MARRY_DIVORCE, notifyData);
                CoupleArenaManager.Instance().OnSpouseRequestDivorce(spouseClient, client);
            }
            else
            {
                if (null == spouseClient)
                {
                    return MarryResult.TargetOffline;
                }

                if (RemoveMarryApply(spouseID, MarryApplyType.ApplyDivorce) == false)
                {
                    return MarryResult.ApplyTimeout;
                }
                RemoveMarryApply(spouseID);

                if (divorceType == MarryDivorceType.DivorceFreeAccept)
                {
                    // 情侣竞技通知中心竞技、排行数据，dbserver清除战报数据
                    // 必须保证先清除成功，才能自由离婚
                    if (CoupleWishManager.Instance().PreClearDivorceData(_man, _wife)
                        && CoupleArenaManager.Instance().PreClearDivorceData(_man, _wife))
                    {
                        client.ClientData.MyMarriageData.byMarrytype = -1;
                        client.ClientData.MyMarriageData.nSpouseID = -1;
                        spouseClient.ClientData.MyMarriageData.byMarrytype = -1;
                        spouseClient.ClientData.MyMarriageData.nSpouseID = -1;

                        MarryFuBenMgr.UpdateMarriageData2DB(client);
                        MarryFuBenMgr.UpdateMarriageData2DB(spouseClient);

                        MarriageOtherLogic.getInstance().SendMarriageDataToClient(client);
                        MarriageOtherLogic.getInstance().SendMarriageDataToClient(spouseClient);

                        //更新婚戒属性
                        MarriageOtherLogic.getInstance().ResetRingAttr(client);
                        MarriageOtherLogic.getInstance().ResetRingAttr(spouseClient);

                        //取消婚宴
                        MarryPartyLogic.getInstance().MarryPartyRemove(client.ClientData.RoleID, true, client);
                        MarryPartyLogic.getInstance().MarryPartyRemove(spouseID, true, client);

                        //[bing] 刷新客户端活动叹号
                        if (client._IconStateMgr.CheckJieRiFanLi(client, ActivityTypes.JieriMarriage) == true)
                        {
                            client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                            client._IconStateMgr.SendIconStateToClient(client);
                        }
                        //[bing] 刷新客户端活动叹号
                        if (spouseClient._IconStateMgr.CheckJieRiFanLi(spouseClient, ActivityTypes.JieriMarriage) == true)
                        {
                            spouseClient._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, spouseClient._IconStateMgr.IsAnyJieRiTipActived());
                            spouseClient._IconStateMgr.SendIconStateToClient(spouseClient);
                        }

                        CoupleArenaManager.Instance().OnDivorce(client.ClientData.RoleID, spouseID);
                    }
                    else
                    {
                        GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, spouseClient,
                            DivorceCost, "自由离婚拒绝返还绑金", false);
                        //GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, spouseClient, DivorceCost, "自由离婚拒绝返还绑金");

                        Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.HintMsg, "自由离婚操作失败", true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                        Global.BroadcastRoleActionMsg(spouseClient, RoleActionsMsgTypes.HintMsg, "自由离婚操作失败", true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                    }                 
                }
                else if (divorceType == MarryDivorceType.DivorceFreeReject)
                {
                    GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, spouseClient,
                        DivorceCost, "自由离婚拒绝返还绑金", false);
                    //GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, spouseClient, DivorceCost, "自由离婚拒绝返还绑金");

                    //发送离婚拒绝讯息
                    string notifyData = string.Format("{0}:{1}", client.ClientData.RoleID, (int)MarryDivorceType.DivorceFreeReject);
                    spouseClient.sendCmd((int)TCPGameServerCmds.CMD_SPR_MARRY_DIVORCE, notifyData);
                }
            }

            return MarryResult.Success;
        }

        /// <summary>
        /// 离婚邮件
        /// </summary>
        public static bool SendDivorceMail(int roleID, string subject, string content, GameClient client, int serverId)
        {
            string mailGoodsString = "";
            string strDbCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}", -1, Global.GetLang("系统"), roleID, "", subject, content, 0, 0, 0, mailGoodsString);
            string[] fieldsData = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_SENDUSERMAIL, strDbCmd, serverId);

            if (client != null)
            {
                client._IconStateMgr.CheckEmailCount(client);
            }

            return (fieldsData == null)? true : false;
        }

        /// <summary>
        /// 自动拒绝离婚
        /// </summary>
        public static MarryResult MarryAutoReject(GameClient client, int autoReject)
        {
            if (client.ClientData.MyMarriageData.byAutoReject != autoReject)
            {
                client.ClientData.MyMarriageData.byAutoReject = (sbyte)autoReject;
            }
            MarryFuBenMgr.UpdateMarriageData2DB(client);
            return MarryResult.Success;
        }
        #endregion

        #region 外部使用逻辑
        public static bool IsMarried(int roleID)
        {
            RoleDataEx roleDataEx = GetOfflineRoleData(roleID);
            if (roleDataEx != null && roleDataEx.MyMarriageData != null)
            {
                if (roleDataEx.MyMarriageData.byMarrytype != -1)
                {
                    return true;
                }
            }
            return false;
        }

        public static int GetSpouseID(int roleID)
        {
            RoleDataEx roleDataEx = GetOfflineRoleData(roleID);
            return (roleDataEx != null && roleDataEx.MyMarriageData != null)? roleDataEx.MyMarriageData.nSpouseID : -1;
        }

        public static string GetRoleName(int roleID)
        {
            // 角色不在线读数据库
            RoleDataEx roleDataEx = GetOfflineRoleData(roleID);
            return (roleDataEx != null)? roleDataEx.RoleName : "";
        }

        public static RoleDataEx GetOfflineRoleData(int roleID)
        {
            GameClient client = GameManager.ClientMgr.FindClient(roleID);
            if (null != client)
            {
                return client.ClientData.GetRoleData();
            }

            var clientData = Global.GetSafeClientDataFromLocalOrDB(roleID);
            return clientData != null ? clientData.GetRoleData() : null;
        }
        #endregion

        #region 角色改名，通知配偶，检查更新婚宴
        public static void OnChangeName(int roleId, string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                return;
            }

            SafeClientData clientData = Global.GetSafeClientDataFromLocalOrDB(roleId);
            // 未婚不处理
            if (clientData == null
                || clientData.MyMarriageData == null
                || clientData.MyMarriageData.nSpouseID == -1)
            {
                return;
            }

            // 把我的信息重新发给我的配偶
            GameClient spouseClient = GameManager.ClientMgr.FindClient(clientData.MyMarriageData.nSpouseID);
            if (spouseClient != null)
            {
                // 我改名了，要通知我的配偶
                MarriageOtherLogic.getInstance().SendSpouseDataToClient(spouseClient);
            }

            MarryPartyLogic.getInstance().OnChangeName(roleId, oldName, newName);
        }
        #endregion
    }
}
