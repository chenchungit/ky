using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Data;
using Server.Tools.Pattern;
using GameServer.Server;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using Server.Protocol;
using Server.Tools;
using GameServer.Core.Executor;

namespace GameServer.Logic.ActivityNew.SevenDay
{
    /// <summary>
    /// 七日活动 管理器
    /// </summary>
    public class SevenDayActivityMgr : SingletonTemplate<SevenDayActivityMgr>, IManager, ICmdProcessorEx, IEventListener
    {
        private SevenDayActivityMgr() { }

        private SevenDayLoginAct LoginAct = new SevenDayLoginAct();
        private SevenDayChargeAct ChargeAct = new SevenDayChargeAct();
        private SevenDayBuyAct BuyAct = new SevenDayBuyAct();
        private SevenDayGoalAct GoalAct = new SevenDayGoalAct();

        #region Implement Interface IManager
        public bool initialize()
        {
            this.LoadConfig();
            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SEVEN_DAY_ACT_QUERY, 2, 2, SevenDayActivityMgr.Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SEVEN_DAY_ACT_GET_AWARD, 3, 3, SevenDayActivityMgr.Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SEVEN_DAY_ACT_QIANG_GOU, 3, 3, SevenDayActivityMgr.Instance());

            GlobalEventSource.getInstance().registerListener((int)EventTypes.SevenDayGoal, SevenDayActivityMgr.Instance());

            return true;
        }

        public bool showdown()
        {
            GlobalEventSource.getInstance().removeListener((int)EventTypes.SevenDayGoal, SevenDayActivityMgr.Instance());

            return true;
        }

        public bool destroy()
        {
            return true;
        }
        #endregion

        #region Implement Interface ICmdProcessorEx
        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_SEVEN_DAY_ACT_QUERY:
                    return HandleClientQuery(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SEVEN_DAY_ACT_GET_AWARD:
                    return HandleGetAward(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SEVEN_DAY_ACT_QIANG_GOU:
                    return HandleClientBuy(client, nID, bytes, cmdParams);
            }

            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return true;
        }
        #endregion

        #region Implement Interface IEventListener
        public void processEvent(EventObject eventObject)
        {
           /* if (eventObject.getEventType() == (int)EventTypes.SevenDayGoal)
            {
                SevenDayGoalEventObject evObj = eventObject as SevenDayGoalEventObject;
                try
                {
                    if (evObj != null && IsInActivityTime(evObj.Client))
                    {
                        GoalAct.HandleEvent(evObj);

                        // cmd_init_game 会触发很多目标项，从而触发感叹号提示发给客户端，但是这时客户端不能正确接受。所以必须等到cmd_play_game之后才能发送感叹号
                        if (evObj.Client.ClientSocket.session.SocketTime[4] > 0)
                        {
                            CheckSendIconState(evObj.Client);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Error, "SevenDayActivityMgr.processEvent [SevenDayGoal]", ex);
                }
                finally
                {
                    SevenDayGoalEvPool.Free(evObj);
                }
            }*/
        }
        #endregion

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public void LoadConfig()
        {
            LoginAct.LoadConfig();
            ChargeAct.LoadConfig();
            BuyAct.LoadConfig();
            GoalAct.LoadConfig();
        }

        /// <summary>
        /// 玩家登录，触发七日活动更新
        /// </summary>
        /// <param name="client"></param>
        public void OnLogin(GameClient client)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return ;

            if (IsInActivityTime(client))
            {
                LoginAct.Update(client);
                ChargeAct.Update(client);
                GoalAct.Update(client);

                CheckSendIconState(client);
            }
            else if (TimeUtil.NowDateTime() > Global.GetRegTime(client.ClientData).AddDays(SevenDayConsts.DayCount + 7))
            {
                // 不在活动期间，检测清空数据
                lock (client.ClientData.SevenDayActDict)
                {
                    if (client.ClientData.SevenDayActDict.Count > 0)
                    {
                        if (!Global.sendToDB<bool, int>((int)TCPGameServerCmds.CMD_DB_CLEAR_SEVEN_DAY_DATA, client.ClientData.RoleID, client.ServerId))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("玩家超过七日活动结束后7天了，删除数据失败,roleid={0}", client.ClientData.RoleID));
                        }
                        else
                        {
                            client.ClientData.SevenDayActDict.Clear();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 玩家跨天登录，触发七日活动更新
        /// </summary>
        /// <param name="client"></param>
        public void OnNewDay(GameClient client)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return;

            if (IsInActivityTime(client))
            {
                LoginAct.Update(client);
                ChargeAct.Update(client);
                CheckSendIconState(client);
            }
        }

        /// <summary>
        ///  玩家充值
        /// </summary>
        /// <param name="client"></param>
        public void OnCharge(GameClient client)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return;

            if (IsInActivityTime(client))
            {
                ChargeAct.Update(client);
                CheckSendIconState(client);
            }
        }

        /// <summary>
        /// 检测发送图标状态
        /// </summary>
        /// <param name="client"></param>
        private void CheckSendIconState(GameClient client)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return;

            if (client == null) return;

            bool bAnyChildActived = false;
            bool bChanged = false;
            bool bTmpFlag = false;

            // 七日登录
            bTmpFlag = LoginAct.HasAnyAwardCanGet(client); bAnyChildActived |= bTmpFlag;
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayLogin, bTmpFlag);

            // 七日充值
            bTmpFlag = ChargeAct.HasAnyAwardCanGet(client); bAnyChildActived |= bTmpFlag;
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayCharge, bTmpFlag);

            // 七日抢购
            bTmpFlag = BuyAct.HasAnyCanBuy(client); bAnyChildActived |= bTmpFlag;
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayBuy, bTmpFlag);

            // 七日目标
            bool[] bGoalDay = null;
            bTmpFlag = GoalAct.HasAnyAwardCanGet(client, out bGoalDay);
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayGoal_1, bGoalDay[0]);
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayGoal_2, bGoalDay[1]);
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayGoal_3, bGoalDay[2]);
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayGoal_4, bGoalDay[3]);
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayGoal_5, bGoalDay[4]);
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayGoal_6, bGoalDay[5]);
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayGoal_7, bGoalDay[6]);

            bAnyChildActived |= bTmpFlag;
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayGoal, bTmpFlag);

            // 总图标
            bChanged |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.SevenDayActivity, bAnyChildActived);

            if (bChanged)
            {
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        /// <summary>
        /// 处理客户端查询七日活动
        /// </summary>
        private bool HandleClientQuery(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return false;

            int act = Convert.ToInt32(cmdParams[1]);

            SevenDayActQueryData resultData = new SevenDayActQueryData();
            resultData.ActivityType = act;
            resultData.ItemDict = null;

            TCPOutPacket packet = null;
            Dictionary<int, SevenDayItemData> itemData = GetActivityData(client, (ESevenDayActType)act);
            if (itemData == null)
            {
                packet = DataHelper.ObjectToTCPOutPacket(resultData, TCPOutPacketPool.getInstance(), nID);
            }
            else
            {
                // 外部会修改，锁住，保证原子性
                lock (itemData)
                {
                    resultData.ItemDict = itemData;
                    if (act == (int)ESevenDayActType.Charge)
                    {
                        //为兼容老数据，七日充值数据，数据库保存的是人民币值，给客户端时，转换为钻石数
                        resultData.ItemDict = new Dictionary<int, SevenDayItemData>();
                        foreach (var kv in itemData)
                        {
                            resultData.ItemDict.Add(kv.Key, new SevenDayItemData() { AwardFlag = kv.Value.AwardFlag, Params1 = Global.TransMoneyToYuanBao(kv.Value.Params1), Params2 = kv.Value.Params2, });
                        }
                    }

                    packet = DataHelper.ObjectToTCPOutPacket(resultData, TCPOutPacketPool.getInstance(), nID);
                }
            }

            if (packet != null)
                client.sendCmd(packet);

            return true;
        }

        /// <summary>
        /// 处理客户端领取七日活动奖励
        /// 七日登录、七日充值、七日目标
        /// </summary>
        private bool HandleGetAward(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return false;

            // roleid:act:arg1
            int act = Convert.ToInt32(cmdParams[1]);
            int arg1 = Convert.ToInt32(cmdParams[2]);

            ESevenDayActErrorCode ec = ESevenDayActErrorCode.NotInActivityTime;
            if (!IsInActivityTime(client)) ec = ESevenDayActErrorCode.NotInActivityTime;
            else if (act == (int)ESevenDayActType.Login) ec = LoginAct.HandleGetAward(client, arg1);
            else if (act == (int)ESevenDayActType.Charge) ec = ChargeAct.HandleGetAward(client, arg1);
            else if (act == (int)ESevenDayActType.Goal) ec = GoalAct.HandleGetAward(client, arg1);

            client.sendCmd(nID, string.Format("{0}:{1}:{2}", (int)ec, act, arg1));

            if (ec == ESevenDayActErrorCode.Success)
                CheckSendIconState(client);

            return true;
        }

        /// <summary>
        /// 处理七日抢购活动
        /// </summary>
        private bool HandleClientBuy(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return false;

            // roleid:id:cnt
            int id = Convert.ToInt32(cmdParams[1]);
            int cnt = Convert.ToInt32(cmdParams[2]);

            ESevenDayActErrorCode ec = ESevenDayActErrorCode.Success;

            if (!IsInActivityTime(client)) ec = ESevenDayActErrorCode.NotInActivityTime;
            else ec = BuyAct.HandleClientBuy(client, id, cnt);

            if (ec == ESevenDayActErrorCode.Success)
                CheckSendIconState(client);

            client.sendCmd(nID, string.Format("{0}:{1}:{2}", (int)ec, id, cnt));
            return true;
        }

        /// <summary>
        /// 判断是否在七日活动时间内
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool IsInActivityTime(GameClient client)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return false;

            int currDay;
            return IsInActivityTime(client, out currDay);
        }

        /// <summary>
        /// 判断是否在七日活动时间内
        /// </summary>
        /// <param name="client"></param>
        /// <param name="currDay">返回当前处于活动的第几天 1---7</param>
        /// <returns></returns>
        public bool IsInActivityTime(GameClient client, out int currDay)
        {
            currDay = 0;

            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return false;

            if (client == null) return false;

            DateTime startDate = Global.GetRegTime(client.ClientData);
            startDate -= startDate.TimeOfDay;

            DateTime todayDate = TimeUtil.NowDateTime();
            todayDate -= todayDate.TimeOfDay;

            currDay = (todayDate - startDate).Days + 1;
            if (currDay < 1 || currDay > SevenDayConsts.DayCount)
                return false;

            return true;
        }

        /// <summary>
        /// 获取某项七日活动数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="actType"></param>
        /// <returns></returns>
        public Dictionary<int, SevenDayItemData> GetActivityData(GameClient client, ESevenDayActType actType)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return null;

            if (client == null) return null;

            Dictionary<int, SevenDayItemData> resultDict = null;
            lock (client.ClientData.SevenDayActDict)
            {
                if (!client.ClientData.SevenDayActDict.TryGetValue((int)actType, out resultDict))
                {
                    resultDict = new Dictionary<int, SevenDayItemData>();
                    client.ClientData.SevenDayActDict[(int)actType] = resultDict;
                }
            }

            return resultDict;
        }

        /// <summary>
        /// 更新db
        /// </summary>
        public bool UpdateDb(int roleid, ESevenDayActType actType, int id, SevenDayItemData itemData, int serverId)
        {
            SevenDayUpdateDbData updateData = new SevenDayUpdateDbData();
            updateData.RoleId = roleid;
            updateData.ActivityType = (int)actType;
            updateData.Id = id;
            updateData.Data = itemData;

            if (!Global.sendToDB<bool, SevenDayUpdateDbData>((int)TCPGameServerCmds.CMD_DB_UPDATE_SEVEN_DAY_ITEM_DATA, updateData, serverId))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("七日活动更新玩家数据失败, roleid={0}, act={1}, id={2}", roleid, actType, id));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 发奖
        /// </summary>
        /// <param name="client"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool GiveAward(GameClient client, AwardItem item, ESevenDayActType type)
        {
            if (null == client || null == item) return false;

            if (item.GoodsDataList != null)
            {
                for (int i = 0; i < item.GoodsDataList.Count; i++)
                {
                    int nGoodsID = item.GoodsDataList[i].GoodsID; // 物品id
                    if (Global.IsCanGiveRewardByOccupation(client, nGoodsID))
                    {
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                            item.GoodsDataList[i].GoodsID, item.GoodsDataList[i].GCount,
                            item.GoodsDataList[i].Quality, "", item.GoodsDataList[i].Forge_level,
                            item.GoodsDataList[i].Binding, 0, "", true, 1,
                            GetActivityChineseName((ESevenDayActType)type), Global.ConstGoodsEndTime,
                            item.GoodsDataList[i].AddPropIndex, item.GoodsDataList[i].BornIndex,
                            item.GoodsDataList[i].Lucky, item.GoodsDataList[i].Strong,
                            item.GoodsDataList[i].ExcellenceInfo, item.GoodsDataList[i].AppendPropLev, item.GoodsDataList[i].ChangeLifeLevForEquip);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 获取七日活动的中文名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetActivityChineseName(ESevenDayActType type)
        {
            string name = type.ToString();
            if (type == ESevenDayActType.Login) name = "七日登录";
            else if (type == ESevenDayActType.Charge) name = "七日充值";
            else if (type == ESevenDayActType.Goal) name = "七日目标";
            else if (type == ESevenDayActType.Buy) name = "七日抢购";

            return name;
        }

        /// <summary>
        /// 发放时限性物品
        /// </summary>
        /// <param name="client"></param>
        /// <param name="item"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool GiveEffectiveTimeAward(GameClient client, AwardItem item, ESevenDayActType type)
        {
            if (null == client || null == item) return false;

            if (item.GoodsDataList != null)
            {
                for (int i = 0; i < item.GoodsDataList.Count; i++)
                {
                    int nGoodsID = item.GoodsDataList[i].GoodsID; // 物品id

                    if (Global.IsCanGiveRewardByOccupation(client, nGoodsID))
                    {
                        //添加限时物品
                        Global.AddEffectiveTimeGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                            item.GoodsDataList[i].GoodsID, item.GoodsDataList[i].GCount,
                            item.GoodsDataList[i].Quality, "", item.GoodsDataList[i].Forge_level,
                            item.GoodsDataList[i].Binding, 0, "", false, 1,
                            GetActivityChineseName((ESevenDayActType)type), item.GoodsDataList[i].Starttime, item.GoodsDataList[i].Endtime,
                            item.GoodsDataList[i].AddPropIndex, item.GoodsDataList[i].BornIndex,
                            item.GoodsDataList[i].Lucky, item.GoodsDataList[i].Strong,
                            item.GoodsDataList[i].ExcellenceInfo, item.GoodsDataList[i].AppendPropLev, item.GoodsDataList[i].ChangeLifeLevForEquip);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// GM命令测试
        /// -sevenday reload
        /// -sevenday get act id
        /// -sevenday buy id cnt
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cmdFields"></param>
        public void On_GM(GameClient client, string[] cmdFields)
        {
            if (cmdFields == null || cmdFields.Length < 2)
                return;

            if (cmdFields[1] == "reload")
            {
                SevenDayActivityMgr.Instance().LoadConfig();
            }
            else if (cmdFields[1] == "get" && client != null)
            {
                if (cmdFields.Length >= 4)
                {
                    HandleGetAward(client, (int)TCPGameServerCmds.CMD_SPR_SEVEN_DAY_ACT_GET_AWARD, null, new string[] { client.ClientData.RoleID.ToString(), cmdFields[2], cmdFields[3]});
                }
            }
            else if (cmdFields[1] == "buy" && client != null)
            {
                if (cmdFields.Length >= 4)
                {
                    HandleClientBuy(client, (int)TCPGameServerCmds.CMD_SPR_SEVEN_DAY_ACT_QIANG_GOU, null, new string[] { client.ClientData.RoleID.ToString(), cmdFields[2], cmdFields[3] });
                }
            }
        }
    }
}
