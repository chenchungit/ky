using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Tools.Pattern;
using GameServer.Server;
using Tmsk.Contract;
using GameServer.Core.GameEvent;
using KF.Contract.Data;
using GameServer.Core.Executor;
using Server.Data;
using GameServer.Tools;
using Server.Tools;
using KF.Client;

namespace GameServer.Logic.Marriage.CoupleWish
{
    /// <summary>
    /// 情侣祝福排行榜
    /// </summary>
    public class CoupleWishManager : SingletonTemplate<CoupleWishManager>, IManager, ICmdProcessorEx
    {
        #region Member
        /// <summary>
        /// lock
        /// </summary>
        private object Mutex = new object();

        /// <summary>
        /// 中心同步数据
        /// </summary>
        private CoupleWishSyncData SyncData = new CoupleWishSyncData();

        /// <summary>
        /// 本周排行前N名
        /// </summary>
        private List<CoupleWishCoupleData> ThisWeekTopNList = new List<CoupleWishCoupleData>();

        /// <summary>
        /// 配置管理
        /// </summary>
        public readonly CoupleWishConfig _Config = new CoupleWishConfig();

        /// <summary>
        /// 雕像、宴会管理
        /// </summary>
        private CoupleWishStatueManager StatueMgr = new CoupleWishStatueManager();

        /// <summary>
        /// 祝福特效奖励类型
        /// </summary>
        enum EWishEffectAwardType {BangJin = 0,BangZuan = 1,Exp=2, Max=3, None=99 }

        /// <summary>
        /// 祝福特效每日最大奖励
        /// </summary>
        private Dictionary<EWishEffectAwardType, int> WishEffectDayMaxAward = new Dictionary<EWishEffectAwardType, int>()
        {
            { EWishEffectAwardType.BangJin,60000},
            {EWishEffectAwardType.BangZuan,10000},
            {EWishEffectAwardType.Exp,1000000}
        };

        /// <summary>
        /// 可以获得祝福特效奖励的地图
        /// </summary>
        private HashSet<int> CanEffectAwardMap = new HashSet<int>();
        #endregion

        #region Interface `IManager`
        public bool initialize()
        {
            if (!_Config.Load(Global.GameResPath(CoupleWishConsts.RankAwardCfgFile),
                Global.GameResPath(CoupleWishConsts.WishTypeCfgFile),
                Global.GameResPath(CoupleWishConsts.YanHuiCfgFile)))
                return false;

            StatueMgr.SetWishConfig(_Config);
            if (!StatueMgr.LoadConfig())
                return false;

            foreach (var awardItem in _Config.RankAwardCfgList)
            {
                List<GoodsData> goods1List = GoodsHelper.ParseGoodsDataList(
                    ((string)awardItem.GoodsOneTag).Split('|'), CoupleWishConsts.RankAwardCfgFile);
                List<GoodsData> goods2List = GoodsHelper.ParseGoodsDataList(
                    ((string)awardItem.GoodsTwoTag).Split('|'), CoupleWishConsts.RankAwardCfgFile);

                awardItem.GoodsOneTag = goods1List;
                awardItem.GoodsTwoTag = goods2List;
            }

            int[] nDayMax = GameManager.systemParamsList.GetParamValueIntArrayByName("WishEffectAwardMax");
            WishEffectDayMaxAward[EWishEffectAwardType.BangJin] = nDayMax[0];
            WishEffectDayMaxAward[EWishEffectAwardType.BangZuan] = nDayMax[1];
            WishEffectDayMaxAward[EWishEffectAwardType.Exp] = nDayMax[2];

            int[] nCanGetAwardMap = GameManager.systemParamsList.GetParamValueIntArrayByName("WishEffectAwardMap");
            if (nCanGetAwardMap != null)
            {
                foreach (var m in nCanGetAwardMap)
                    if (!this.CanEffectAwardMap.Contains(m))
                        CanEffectAwardMap.Add(m);
            }

            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("CoupleWishManager.TimerProc", TimerProc), 20000, 10000);

            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_WISH_GET_MAIN_DATA, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_WISH_GET_WISH_RECORD, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerStreamProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_WISH_WISH_OTHER_ROLE, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_WISH_GET_ADMIRE_DATA, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_WISH_ADMIRE_STATUE, 3, 3, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_WISH_GET_PARTY_DATA, 1, 1, Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_COUPLE_WISH_JOIN_PARTY, 2, 2, Instance());

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
        #endregion

        #region Interface `ICmdProcessorEx`
        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (nID == (int)TCPGameServerCmds.CMD_COUPLE_WISH_GET_MAIN_DATA)
                this.HandleGetMainDataCommand(client, nID, bytes, cmdParams);
            else if (nID == (int)TCPGameServerCmds.CMD_COUPLE_WISH_GET_WISH_RECORD)
                this.HandleGetWishRecordCommand(client, nID, bytes, cmdParams);
            else if (nID == (int)TCPGameServerCmds.CMD_COUPLE_WISH_WISH_OTHER_ROLE)
                this.HandleWishOtherRoleCommand(client, nID, bytes, cmdParams);
            else if (nID == (int)TCPGameServerCmds.CMD_COUPLE_WISH_ADMIRE_STATUE)
                this.HandleAdmireStatueCommand(client, nID, bytes, cmdParams);
            else if (nID == (int)TCPGameServerCmds.CMD_COUPLE_WISH_GET_ADMIRE_DATA)
                this.HandleGetAdmireDataCommand(client, nID, bytes, cmdParams);
            else if (nID == (int)TCPGameServerCmds.CMD_COUPLE_WISH_GET_PARTY_DATA)
                this.HandleGetPartyDataCommand(client, nID, bytes, cmdParams);
            else if (nID == (int)TCPGameServerCmds.CMD_COUPLE_WISH_JOIN_PARTY)
                this.HandleJoinPartyCommand(client, nID, bytes, cmdParams);

            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return true;
        }
        #endregion

        #region Handle Client Command
        /// <summary>
        /// 参加宴会
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        private void HandleJoinPartyCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            int toCoupleId = Convert.ToInt32(cmdParams[1]);
            lock (Mutex)
            {
                int ec = StatueMgr.HandleJoinParty(client, toCoupleId);
                client.sendCmd(nID, ec.ToString());
            }
        }

        /// <summary>
        /// 查看宴会
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        private void HandleGetPartyDataCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            lock (Mutex)
            {
                CoupleWishYanHuiData data = StatueMgr.HandleQueryParty(client);
                client.sendCmd(nID, data);
            }
        }
        /// <summary>
        /// 获取膜拜数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        private void HandleGetAdmireDataCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            lock (Mutex)
            {
                CoupleWishTop1AdmireData data = StatueMgr.HandleQueryAdmireData(client);
                client.sendCmd(nID, data);
            }
        }

        /// <summary>
        /// 膜拜雕像
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        private void HandleAdmireStatueCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            int toCoupleId = Convert.ToInt32(cmdParams[1]);
            int admireType = Convert.ToInt32(cmdParams[2]);
            if (admireType != 1 && admireType != 2) return;

            lock (Mutex)
            {
                int ec = StatueMgr.HandleAdmireStatue(client, toCoupleId, admireType);
                client.sendCmd(nID, ec.ToString());
            }
        }


        /// <summary>
        /// 玩家请求祝福他人
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        private void HandleWishOtherRoleCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            var cliReq = DataHelper.BytesToObject<CoupleWishWishReqData>(bytes, 0, bytes.Length);
            DateTime now = TimeUtil.NowDateTime();

            if (client.ClientSocket.IsKuaFuLogin)
            {
                client.sendCmd(nID, StdErrorCode.Error_Operation_Denied.ToString());
                return;
            }

            if (cliReq.CostType != (int)CoupleWishWishReqData.ECostType.Goods
                && cliReq.CostType != (int)CoupleWishWishReqData.ECostType.ZuanShi)
            {
                client.sendCmd(nID, StdErrorCode.Error_Invalid_Params.ToString());
                return;
            }

            // 是否是活动时间
            int wishWeek;
            if (!_Config.IsInWishTime(now, out wishWeek))
            {
                client.sendCmd(nID, StdErrorCode.Error_Wish_In_Balance_Time.ToString());
                return;
            }          

            // 祝福类型检查
            CoupleWishTypeConfig wishCfg = _Config.WishTypeCfgList.Find(_w => _w.WishType == cliReq.WishType);
            if (wishCfg == null)
            {
                client.sendCmd(nID, StdErrorCode.Error_Config_Fault.ToString());
                return;
            }

            // 道具检查
            if (cliReq.CostType == (int)CoupleWishWishReqData.ECostType.Goods
                && wishCfg.CostGoodsId > 0 && wishCfg.CostGoodsNum > 0
                && Global.GetTotalGoodsCountByID(client, wishCfg.CostGoodsId) < wishCfg.CostGoodsNum)
            {
                client.sendCmd(nID, StdErrorCode.Error_Goods_Not_Enough.ToString());
                return;
            }

            // 钻石检查
            if (cliReq.CostType == (int)CoupleWishWishReqData.ECostType.ZuanShi
                && wishCfg.CostZuanShi > 0 && client.ClientData.UserMoney < wishCfg.CostZuanShi)
            {
                client.sendCmd(nID, StdErrorCode.Error_ZuanShi_Not_Enough.ToString());
                return;
            }

            // 祝福寄语检查
            if (!string.IsNullOrEmpty(cliReq.WishTxt))
            {
                if (wishCfg.CanHaveWishTxt != 1)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Cannot_Have_Wish_Txt.ToString());
                    return;
                }
                else if (cliReq.WishTxt.Length > CoupleWishConsts.MaxWishTxtLen)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Wish_Txt_Length_Limit.ToString());
                    return;
                }
            }

            CoupleWishWishRoleReq centerReq = new CoupleWishWishRoleReq();
            centerReq.From.RoleId = client.ClientData.RoleID;
            centerReq.From.ZoneId = client.ClientData.ZoneID;
            centerReq.From.RoleName = client.ClientData.RoleName;
            centerReq.WishType = cliReq.WishType;
            centerReq.WishTxt = cliReq.WishTxt;

            RoleData4Selector toManSelector = null;
            RoleData4Selector toWifeSelector = null;
            CoupleWishCoupleDataK rankCoupleData = null;

            if (cliReq.IsWishRankRole)
            {
                centerReq.IsWishRank = true;
                // 跨服排行榜祝福
                lock (Mutex)
                {
                    int coupleIdx;
                    if (!this.SyncData.ThisWeek.CoupleIdex.TryGetValue(cliReq.ToRankCoupleId, out coupleIdx))
                    {
                        client.sendCmd(nID, StdErrorCode.Error_Operation_Denied.ToString());
                        return;
                    }

                    rankCoupleData = this.SyncData.ThisWeek.RankList[coupleIdx];
                    if (rankCoupleData == null || rankCoupleData.DbCoupleId != cliReq.ToRankCoupleId || rankCoupleData.Rank > CoupleWishConsts.MaxRankNum * 2)
                    {
                        // 因为客户端看到的不是实时的数据，客户端看到的时候某对情侣可能处于前20名，但是当祝福的时候，可能已经不是前20名了，优化下体验，如果是前40名就允许
                        client.sendCmd(nID, StdErrorCode.Error_Operation_Denied.ToString());
                        return;
                    }

                    centerReq.ToCoupleId = cliReq.ToRankCoupleId;

                    // 赠送排行榜情侣，检测是否是本服的情侣，尝试更新角色形象
                    toManSelector = Global.sendToDB<RoleData4Selector,
                        string>((int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", rankCoupleData.Man.RoleId), client.ServerId);
                    toWifeSelector = Global.sendToDB<RoleData4Selector,
                        string>((int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", rankCoupleData.Wife.RoleId), client.ServerId);
                    if (toManSelector == null || toWifeSelector == null || toManSelector.RoleID <= 0 || toWifeSelector.RoleID <= 0)
                        toManSelector = toWifeSelector = null;
                }
            }
            else
            {
                // 本服祝福
                int toRoleId = -1;
                if (!string.IsNullOrEmpty(cliReq.ToLocalRoleName))
                    toRoleId = RoleName2IDs.FindRoleIDByName(cliReq.ToLocalRoleName, true);
                if (toRoleId <= 0)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Wish_Player_Not_Exist.ToString());
                    return;
                }
                if (toRoleId == client.ClientData.RoleID)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Cannot_Wish_Self.ToString());
                    return;
                }

                int nSpouseId = MarryLogic.GetSpouseID(toRoleId);
                if (nSpouseId <= 0)
                {
                    client.sendCmd(nID, StdErrorCode.Error_Wish_Player_Not_Marry.ToString());
                    return;
                }

                toManSelector = Global.sendToDB<RoleData4Selector,
                    string>((int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", toRoleId), client.ServerId);
                toWifeSelector = Global.sendToDB<RoleData4Selector,
                    string>((int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", nSpouseId), client.ServerId);

                if (toManSelector == null || toWifeSelector == null || toManSelector.RoleSex == toWifeSelector.RoleSex)
                {
                    client.sendCmd(nID, StdErrorCode.Error_DB_Faild.ToString());
                    return;
                }

                if (toManSelector.RoleSex == (int)ERoleSex.Girl)
                {
                    DataHelper2.Swap(ref toManSelector, ref toWifeSelector);
                }        
            }

            if (toManSelector != null && toWifeSelector != null)
            {
                // 不管是排行榜赠送还是选中好友赠送，都尝试更新被赠送者形象数据
                // 排行榜赠送时，toManSelector和toWifeSelector可能都为null，或者都不为null
                // 选中好友赠送，toManSelector和toWifeSelector一定都不为null
                centerReq.ToMan.RoleId = toManSelector.RoleID;
                centerReq.ToMan.ZoneId = toManSelector.ZoneId;
                centerReq.ToMan.RoleName = toManSelector.RoleName;
                centerReq.ToManSelector = DataHelper.ObjectToBytes<RoleData4Selector>(toManSelector);

                centerReq.ToWife.RoleId = toWifeSelector.RoleID;
                centerReq.ToWife.ZoneId = toWifeSelector.ZoneId;
                centerReq.ToWife.RoleName = toWifeSelector.RoleName;
                centerReq.ToWifeSelector = DataHelper.ObjectToBytes<RoleData4Selector>(toWifeSelector);
            }

            int ec = TianTiClient.getInstance().CoupleWishWishRole(centerReq);
            if (ec < 0)
            {
                client.sendCmd(nID, ec.ToString());
                return;
            }

            // 扣除物品
            if (cliReq.CostType == (int)CoupleWishWishReqData.ECostType.Goods
                && wishCfg.CostGoodsId > 0 && wishCfg.CostGoodsNum > 0)
            {
                bool oneUseBind = false;
                bool oneUseTimeLimit = false;
                Global.UseGoodsBindOrNot(client, wishCfg.CostGoodsId, wishCfg.CostGoodsNum, true, out oneUseBind, out oneUseTimeLimit);
            }

            // 扣除钻石
            if (cliReq.CostType == (int)CoupleWishWishReqData.ECostType.ZuanShi
                && wishCfg.CostZuanShi > 0)
            {
                GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    client, wishCfg.CostZuanShi, "情侣祝福");
            }

            // 增加本服祝福特效
            if (wishCfg.IsHaveEffect == 1)
            {
                CoupleWishNtfWishEffectData effectData = new CoupleWishNtfWishEffectData();
                effectData.From = centerReq.From;
                effectData.WishType = cliReq.WishType;
                effectData.WishTxt = cliReq.WishTxt;
                effectData.To = new List<KuaFuRoleMiniData>();

                if (cliReq.IsWishRankRole)
                {
                    effectData.To.Add(rankCoupleData.Man);
                    effectData.To.Add(rankCoupleData.Wife);
                }
                else
                {
                    if (centerReq.ToMan.RoleName == cliReq.ToLocalRoleName) effectData.To.Add(centerReq.ToMan);
                    else effectData.To.Add(centerReq.ToWife);
                }

                lock (Mutex)
                {
                    // 这里必须锁住，不然多个人同时祝福，都有可能修改所有在线玩家的奖励数据
                    HandleWishEffect(effectData);
                }
            }

            client.sendCmd(nID, StdErrorCode.Error_Success.ToString());
        }

        /// <summary>
        /// 玩家查看祝福记录
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        private void HandleGetWishRecordCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            List<CoupleWishWishRecordData> records = TianTiClient.getInstance().CoupleWishGetWishRecord(client.ClientData.RoleID);
            if (records != null)
                records.Reverse();
            client.sendCmd(nID, records);
        }

        /// <summary>
        /// 玩家查看情侣祝福主界面
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        private void HandleGetMainDataCommand(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            if (!IsGongNengOpened(client)) return;
            DateTime now = TimeUtil.NowDateTime();

            CoupleWishMainData mainData = new CoupleWishMainData();
            lock (Mutex)
            {
                mainData.RankList = new List<CoupleWishCoupleData>(ThisWeekTopNList);
                mainData.CanGetAwardId = CheckGiveAward(client);

                int idx;
                if (SyncData.ThisWeek.RoleIndex.TryGetValue(client.ClientData.RoleID, out idx))
                {
                    CoupleWishCoupleDataK coupleDataK = SyncData.ThisWeek.RankList[idx];
                    if (coupleDataK.Man.RoleId == client.ClientData.RoleID || coupleDataK.Wife.RoleId == client.ClientData.RoleID)
                    {
                        mainData.MyCoupleRank = coupleDataK.Rank;
                        mainData.MyCoupleBeWishNum = coupleDataK.BeWishedNum;
                    }
                }
            }

            mainData.MyCoupleManSelector = Global.sendToDB<RoleData4Selector,
                string>((int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", client.ClientData.RoleID), client.ServerId);
            if (MarryLogic.IsMarried(client.ClientData.RoleID))
                mainData.MyCoupleWifeSelector = Global.sendToDB<RoleData4Selector,
                                   string>((int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", client.ClientData.MyMarriageData.nSpouseID), client.ServerId);

            if (client.ClientData.RoleSex == (int)ERoleSex.Girl)
            {
                DataHelper2.Swap(ref mainData.MyCoupleManSelector, ref mainData.MyCoupleWifeSelector);
            }

            client.sendCmd(nID, mainData);
        }
        #endregion

        #region Util
        /// <summary>
        /// 检测发奖
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private int CheckGiveAward(GameClient client)
        {
            if (client == null) return 0;
            DateTime now = TimeUtil.NowDateTime();
            int awardWeek;
            if (!_Config.IsInAwardTime(now, out awardWeek))
                return 0;

            lock (Mutex)
            {
                string szAwardFlag = Global.GetRoleParamByName(client, RoleParamName.CoupleWishWeekAward);
                string[] fields = string.IsNullOrEmpty(szAwardFlag) ? null : szAwardFlag.Split(',');
                if (fields != null && fields.Length == 2 && Convert.ToInt32(fields[0]) == awardWeek)
                    return 0;

                if (awardWeek != SyncData.LastWeek.Week)
                    return 0;
               
                int idx;
                if (!SyncData.LastWeek.RoleIndex.TryGetValue(client.ClientData.RoleID, out idx))
                    return 0;

                CoupleWishCoupleDataK coupleData = SyncData.LastWeek.RankList[idx];
                if (coupleData == null) return 0;
                if (coupleData.Man.RoleId != client.ClientData.RoleID && coupleData.Wife.RoleId != client.ClientData.RoleID)
                    return 0;
             
                var wishAward = _Config.RankAwardCfgList.Find(_r => coupleData.Rank >= _r.StartRank && (_r.EndRank <= 0 || coupleData.Rank <= _r.EndRank));
                if (wishAward == null) 
                    return 0;

                List<GoodsData> goodsList = new List<GoodsData>();
                goodsList.AddRange(wishAward.GoodsOneTag as List<GoodsData>);
                goodsList.AddRange((wishAward.GoodsTwoTag as List<GoodsData>).FindAll(_g => Global.IsCanGiveRewardByOccupation(client, _g.GoodsID)));
                if (Global.CanAddGoodsDataList(client, goodsList))
                {
                    foreach (var goodsData in goodsList)
                    {
                        Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, "情侣排行榜", false,
                                                           goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, true);
                    }
                }
                else
                {
                    Global.UseMailGivePlayerAward3(client.ClientData.RoleID, goodsList, "情侣祝福榜", string.Format("情侣祝福榜第{0}名奖励，请查收！", coupleData.Rank), 0);
                }

                Global.SaveRoleParamsStringToDB(client, RoleParamName.CoupleWishWeekAward, string.Format("{0},{1}", awardWeek, wishAward.Id), true);
                CheckTipsIconState(client);
                return wishAward.Id;
            }

            return 0;
        }

        /// <summary>
        /// 检查图标
        /// </summary>
        /// <param name="client"></param>
        public void CheckTipsIconState(GameClient client)
        {
            if (client == null || !IsGongNengOpened(client)) 
                return;
            bool bCanGetAward = false;
            lock (Mutex)
            {
                int awardWeek = 0;
                if (_Config.IsInAwardTime(TimeUtil.NowDateTime(), out awardWeek))
                {
                    string szAwardFlag = Global.GetRoleParamByName(client, RoleParamName.CoupleWishWeekAward);
                    string[] fields = string.IsNullOrEmpty(szAwardFlag) ? null : szAwardFlag.Split(',');
                    if (fields == null || fields.Length != 2 || Convert.ToInt32(fields[0]) != awardWeek && SyncData.LastWeek.Week == awardWeek)
                    {
                        int idx;
                        if (SyncData.LastWeek.Week == awardWeek
                            && SyncData.LastWeek.RoleIndex.TryGetValue(client.ClientData.RoleID, out idx))
                        {
                            // 只有部分名次有奖励
                            int rank = SyncData.LastWeek.RankList[idx].Rank;
                            bCanGetAward = _Config.RankAwardCfgList.Exists(_a => rank >= _a.StartRank && (_a.EndRank <= 0 || rank <= _a.EndRank));
                        }
                    }
                }
            }

            if (client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.CoupleWishCanAward, bCanGetAward))
            {
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        /// <summary>
        /// 功能是否开启
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool IsGongNengOpened(GameClient client)
        {
            if (client == null) return false;

            return true;
        }

        /// <summary>
        /// 存储特效奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="day"></param>
        /// <param name="awardType"></param>
        /// <param name="get"></param>
        private void SaveGetNextEffectAward(GameClient client, int day, EWishEffectAwardType awardType, int get)
        {
            string szEffectAward = Global.GetRoleParamByName(client, RoleParamName.CoupleWishEffectAward);
            string[] awardFields = !string.IsNullOrEmpty(szEffectAward) ? szEffectAward.Split(',') : null;

            int[] newFlag = new int[ 2 + (int)EWishEffectAwardType.Max];
            newFlag[0] = (int)awardType;
            newFlag[1] = day;
            if (awardFields != null && awardFields.Length == 2 + (int)EWishEffectAwardType.Max && Convert.ToInt32(awardFields[1]) == day)
            {
                for (int i = 0; i < (int)EWishEffectAwardType.Max; i++)
                {
                    newFlag[2 + i] = Convert.ToInt32(awardFields[2 + i]);
                }
            }

            newFlag[2 + (int)awardType] = (int)Math.Min((long)int.MaxValue, newFlag[2 + (int)awardType] + get);
            Global.SaveRoleParamsStringToDB(client, RoleParamName.CoupleWishEffectAward,
                string.Format("{0},{1},{2},{3},{4}", newFlag[0], newFlag[1], newFlag[2], newFlag[3], newFlag[4]), true);
        }

        /// <summary>
        /// 检查可以获得的下一个随机特效奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="day"></param>
        /// <param name="awardType"></param>
        /// <param name="canGetMax"></param>
        /// <returns></returns>
        private bool GetNextCanEffectAward(GameClient client, int day, out EWishEffectAwardType awardType, out int canGetMax)
        {
            awardType = EWishEffectAwardType.None;
            canGetMax = 0;

            string szEffectAward = Global.GetRoleParamByName(client, RoleParamName.CoupleWishEffectAward);
            string[] awardFields = !string.IsNullOrEmpty(szEffectAward) ? szEffectAward.Split(',') : null;

            if (awardFields == null || awardFields.Length != 2 + (int)EWishEffectAwardType.Max)
            {
                // 奖励记录数据不正确，重新从第一个开始奖励
                awardType = EWishEffectAwardType.BangJin;
                canGetMax = WishEffectDayMaxAward[awardType];
                return true;
            }

            if (Convert.ToInt32(awardFields[1]) != day)
            {
                // 上次奖励日期不是今天，从下一个开始奖励，已获得数值清空
                awardType = (EWishEffectAwardType)((Convert.ToInt32(awardFields[0]) + 1) % (int)EWishEffectAwardType.Max);
                canGetMax = WishEffectDayMaxAward[awardType];
                return true;
            }

            // 循环找到下一个可奖励的项
            for (int i = 0; i < (int)EWishEffectAwardType.Max; i++)
            {
                int nextType = (Convert.ToInt32(awardFields[0]) + i + 1) % (int)EWishEffectAwardType.Max;
                int nextHadGet = Convert.ToInt32(awardFields[2 + nextType]);

                if (WishEffectDayMaxAward[(EWishEffectAwardType)nextType] > nextHadGet)
                {
                    awardType = (EWishEffectAwardType)nextType;
                    canGetMax = WishEffectDayMaxAward[awardType] - nextHadGet;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 本服增加祝福特效
        /// </summary>
        /// <param name="from"></param>
        /// <param name="toMan"></param>
        /// <param name="toWife"></param>
        private void HandleWishEffect(CoupleWishNtfWishEffectData effectData)
        {
            if (effectData == null) return;

            int dayFlag = TimeUtil.MakeYearMonthDay(TimeUtil.NowDateTime());

            int index = 0;
            GameClient client = null;
            while ((client = GameManager.ClientMgr.GetNextClient(ref index)) != null)
            {
//                 if (Global.GetMapSceneType(client.ClientData.MapCode) != SceneUIClasses.Normal)
//                     continue;

                // 还未登录成功的，不发了，不然特效把客户端搞死了。
                if (client.ClientData.FirstPlayStart)
                    continue;

                if (!CanEffectAwardMap.Contains(client.ClientData.MapCode))
                    continue;

                // 只有在安全区的才能收到祝福特效
                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(client.ClientData.MapCode, out gameMap))
                    continue;

                if (!gameMap.InSafeRegionList(client.CurrentGrid))
                    continue;

                // TODO: 祝福特效公式尚未给出
                effectData.GetBinJinBi = 0;
                effectData.GetBindZuanShi = 0;
                effectData.GetExp = 0;

                EWishEffectAwardType awardType;
                int canGetMax;
                if (!GetNextCanEffectAward(client, dayFlag, out awardType, out canGetMax))
                {
                    // 仅播放祝福特效，不获得实际奖励
                    client.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_WISH_NTF_WISH_EFFECT, effectData);
                    continue;
                }

                int calcKey = client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level;
                int realGet = 0;
                if (awardType == EWishEffectAwardType.BangJin)
                {
                    // 绑金：400*(玩家转*100+玩家等级)
                    effectData.GetBinJinBi = Math.Max(0, Math.Min(400 * calcKey, canGetMax));
                    realGet = effectData.GetBinJinBi;
                }
                else if (awardType == EWishEffectAwardType.BangZuan)
                {
                    // 绑钻：int(0.08*(玩家转*100+玩家等级)
                    effectData.GetBindZuanShi = Math.Max(0, Math.Min((int)(0.08* calcKey), canGetMax));
                    realGet = effectData.GetBindZuanShi;
                }
                else if (awardType == EWishEffectAwardType.Exp)
                {
                    // 经验：4000*(玩家转*100+玩家等级)
                    effectData.GetExp = Math.Max(0, Math.Min(4000 * calcKey, canGetMax));
                    realGet = effectData.GetExp;
                }
                else continue;

                if (effectData.GetBinJinBi > 0)
                {
                    GameManager.ClientMgr.AddMoney1(client, effectData.GetBinJinBi, "情侣祝福特效", true);
                    string tip = string.Format(Global.GetLang("【{0}】送出一生一世祝福，你获得{1}绑定金币"), effectData.From.RoleName, realGet);
                    GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, tip);
                }

                if (effectData.GetBindZuanShi > 0)
                {
                    GameManager.ClientMgr.AddUserGold(client, effectData.GetBindZuanShi, "情侣祝福特效");
                    string tip = string.Format(Global.GetLang("【{0}】送出一生一世祝福，你获得{1}绑定钻石"), effectData.From.RoleName, realGet);
                    GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, tip);
                }

                if (effectData.GetExp > 0)
                {
                    GameManager.ClientMgr.ProcessRoleExperience(client, effectData.GetExp, false);
                    GameManager.ClientMgr.NotifyAddExpMsg(client, effectData.GetExp);
                    string tip = string.Format(Global.GetLang("【{0}】送出一生一世祝福，你获得{1}经验"), effectData.From.RoleName, realGet);
                    GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, tip);
                }

                client.sendCmd((int)TCPGameServerCmds.CMD_COUPLE_WISH_NTF_WISH_EFFECT, effectData);
                SaveGetNextEffectAward(client, dayFlag, awardType, realGet);
            }
        }

        /// <summary>
        /// 即将离婚，通知中心清除排行数据
        /// 清除成功返回true
        /// </summary>
        /// <param name="roleId1"></param>
        /// <param name="roleId2"></param>
        /// <returns></returns>
        public bool PreClearDivorceData(int man, int wife)
        {
            if (TianTiClient.getInstance().CoupleWishPreDivorce(man, wife) >= 0)
                return true;

            return false;
        }
        #endregion

        #region 定期同步中心
        private void TimerProc(object sender, EventArgs e)
        {
            CoupleWishSyncData _syncData = TianTiClient.getInstance().CoupleWishSyncCenterData(
                this.SyncData.ThisWeek.ModifyTime, this.SyncData.LastWeek.ModifyTime, this.SyncData.Statue.ModifyTime);

            if (_syncData == null)
                return;

            lock (Mutex)
            {
                if (_syncData.ThisWeek.ModifyTime != this.SyncData.ThisWeek.ModifyTime)
                {
                    this.SyncData.ThisWeek = _syncData.ThisWeek;
                    this.ThisWeekTopNList.Clear();
                    foreach (var syncCouple in this.SyncData.ThisWeek.RankList)
                    {
                        if (syncCouple.Rank > CoupleWishConsts.MaxRankNum)
                            break;

                        CoupleWishCoupleData couple = new CoupleWishCoupleData();
                        couple.DbCoupleId = syncCouple.DbCoupleId;
                        couple.Man = syncCouple.Man;
                        if (syncCouple.ManSelector != null)
                            couple.ManSelector = DataHelper.BytesToObject<RoleData4Selector>(syncCouple.ManSelector, 0, syncCouple.ManSelector.Length);
                        couple.Wife = syncCouple.Wife;
                        if (syncCouple.WifeSelector != null)
                            couple.WifeSelector = DataHelper.BytesToObject<RoleData4Selector>(syncCouple.WifeSelector, 0, syncCouple.WifeSelector.Length);
                        couple.BeWishedNum = syncCouple.BeWishedNum;
                        couple.Rank = syncCouple.Rank;
                        this.ThisWeekTopNList.Add(couple);
                    }
                }

                if (_syncData.LastWeek.ModifyTime != this.SyncData.LastWeek.ModifyTime)
                {
                    this.SyncData.LastWeek = _syncData.LastWeek;
                }

                if (_syncData.Statue.ModifyTime != this.SyncData.Statue.ModifyTime)
                {
                    this.SyncData.Statue = _syncData.Statue;
                    StatueMgr.SetDiaoXiang(this.SyncData.Statue);
                }
            }
        }
        #endregion
    }
}
