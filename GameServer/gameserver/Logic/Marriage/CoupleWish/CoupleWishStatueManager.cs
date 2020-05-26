using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KF.Contract.Data;
using Server.Data;
using KF.Client;
using Server.Tools;
using GameServer.Server;
using Tmsk.Contract;
using GameServer.Core.Executor;

namespace GameServer.Logic.Marriage.CoupleWish
{
    /// <summary>
    /// 情侣排行榜雕像、宴会管理
    /// </summary>
    class CoupleWishStatueManager
    {
        private int YanHuiMapCode;
        private int YanHuiNpcId;
        private int YanHuiNpcX;
        private int YanHuiNpcY;
        private int YanHuiNpcDir;
        private CoupleWishSyncStatueData _Statue = null;
        private CoupleWishConfig _Config = null;

        public void SetWishConfig(CoupleWishConfig config)
        {
            _Config = config;
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <returns></returns>
        public bool LoadConfig()
        {
            try
            {
                string[] fields = GameManager.systemParamsList.GetParamValueByName("WishHunYanNPC").Split(',');
                YanHuiMapCode = Convert.ToInt32(fields[0]);
                YanHuiNpcId = Convert.ToInt32(fields[1]);
                YanHuiNpcX = Convert.ToInt32(fields[2]);
                YanHuiNpcY = Convert.ToInt32(fields[3]);
                YanHuiNpcDir = Convert.ToInt32(fields[4]);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex.Message);

                return false;
            }

        }

        /// <summary>
        /// 设置雕像
        /// </summary>
        /// <param name="newStatue"></param>
        public void SetDiaoXiang(CoupleWishSyncStatueData newStatue)
        {
            if (newStatue.DbCoupleId > 0
                  && (newStatue.ManRoleDataEx == null || newStatue.WifeRoleDataEx == null))
            {
                // 把第一名情侣的雕像形象数据上传到中心
                // 雕像数据非常大，有100K+，所以在祝福的时候不传到中心，然后每个服务器自行检测上周的第一名是否有雕像数据，如果没有，那么则尝试上报雕像数据
                // 第一名所在的服务器上报之后，其余服务器就能同步到雕像数据了
                SafeClientData manClientData = Global.GetSafeClientDataFromLocalOrDB(newStatue.Man.RoleId);
                SafeClientData wifeClientData = Global.GetSafeClientDataFromLocalOrDB(newStatue.Wife.RoleId);
                RoleDataEx manRoleDataEx = manClientData != null ? manClientData.GetRoleDataEx() : null;
                RoleDataEx wifeRoleDataEx = wifeClientData != null ? wifeClientData.GetRoleDataEx() : null;
                if (manRoleDataEx != null && wifeRoleDataEx != null)
                {
                    CoupleWishReportStatueData statueReq = new CoupleWishReportStatueData();
                    statueReq.DbCoupleId = newStatue.DbCoupleId;
                    statueReq.ManStatue = DataHelper.ObjectToBytes<RoleDataEx>(manRoleDataEx);
                    statueReq.WifeStatue = DataHelper.ObjectToBytes<RoleDataEx>(wifeRoleDataEx);
                    TianTiClient.getInstance().CoupleWishReportCoupleStatue(statueReq);
                }
            }

            if (newStatue.DbCoupleId > 0
                && newStatue.ManRoleDataEx != null && newStatue.WifeRoleDataEx != null)
            {
                if (newStatue.IsDivorced == 1) // 强制不可显示雕像。离婚了呗
                {
                    ReshowCoupleStatue(null, null);
                }
                else
                {
                    if (_Statue == null
                        || _Statue.ManRoleDataEx == null || _Statue.WifeRoleDataEx == null
                        || _Statue.DbCoupleId != newStatue.DbCoupleId)
                    {
                        ReshowCoupleStatue(DataHelper.BytesToObject<RoleDataEx>(newStatue.ManRoleDataEx, 0, newStatue.ManRoleDataEx.Length),
                            DataHelper.BytesToObject<RoleDataEx>(newStatue.WifeRoleDataEx, 0, newStatue.WifeRoleDataEx.Length));
                    }
                }
            }
            else
            {
                ReshowCoupleStatue(null, null);
            }

            NPC npc = NPCGeneralManager.GetNPCFromConfig(YanHuiMapCode, YanHuiNpcId, YanHuiNpcX, YanHuiNpcY, YanHuiNpcDir);
            if (newStatue.DbCoupleId > 0
                && null != npc && (_Statue == null || _Statue.DbCoupleId != newStatue.DbCoupleId)
                && newStatue.YanHuiJoinNum < _Config.YanHuiCfg.TotalMaxJoinNum)
            {
                NPCGeneralManager.AddNpcToMap(npc);
            }

            if (newStatue.DbCoupleId <= 0 || newStatue.YanHuiJoinNum >= _Config.YanHuiCfg.TotalMaxJoinNum)
            {
                NPCGeneralManager.RemoveMapNpc(YanHuiMapCode, YanHuiNpcId);
            }

            _Statue = newStatue;
        }

        #region 宴会
        /// <summary>
        /// 查看宴会
        /// </summary>
        /// <param name="client"></param>
        public CoupleWishYanHuiData HandleQueryParty(GameClient client)
        {
            CoupleWishYanHuiData data = new CoupleWishYanHuiData();
            if (_Statue !=null && _Statue.Man != null && _Statue.Wife != null)
            {
                data.Man = _Statue.Man;
                data.Wife = _Statue.Wife;
                data.TotalJoinNum = _Statue.YanHuiJoinNum;
                data.DbCoupleId = _Statue.DbCoupleId;
                data.MyJoinNum = GetJoinPartyNum(client, _Statue.DbCoupleId);
            }

            return data;
        }

        /// <summary>
        /// 参加宴会
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toCouleId"></param>
        public int HandleJoinParty(GameClient client, int toCouleId)
        {
            if (_Statue == null || _Statue.DbCoupleId <= 0 || _Statue.DbCoupleId != toCouleId)
                return StdErrorCode.Error_Operation_Denied;

            if (GetJoinPartyNum(client, toCouleId) >= _Config.YanHuiCfg.EachRoleMaxJoinNum
                || _Statue.YanHuiJoinNum >= _Config.YanHuiCfg.TotalMaxJoinNum)
                return StdErrorCode.Error_No_Residue_Degree;

            if (Global.GetTotalBindTongQianAndTongQianVal(client) < _Config.YanHuiCfg.CostBindJinBi)
                return StdErrorCode.Error_JinBi_Not_Enough;

            int ec = TianTiClient.getInstance().CoupleWishJoinParty(client.ClientData.RoleID, client.ClientData.ZoneID, toCouleId);
            if (ec < 0) return ec;

            Global.SubBindTongQianAndTongQian(client, _Config.YanHuiCfg.CostBindJinBi, "情侣祝福宴会");

            AddJoinPartyNum(client, toCouleId);
            _Statue.YanHuiJoinNum++; // 先临时加1，让本服的人立马看到，gameserver定期会从中心同步过来

            if (_Config.YanHuiCfg.GetExp > 0)
            {
                GameManager.ClientMgr.ProcessRoleExperience(client, _Config.YanHuiCfg.GetExp, false);
                GameManager.ClientMgr.NotifyAddExpMsg(client, _Config.YanHuiCfg.GetExp);
            }

            if (_Config.YanHuiCfg.GetXingHun > 0)
            {
                GameManager.ClientMgr.ModifyStarSoulValue(client, _Config.YanHuiCfg.GetXingHun, "情侣祝福榜宴会", true);
            }

            if (_Config.YanHuiCfg.GetShengWang > 0)
            {
                GameManager.ClientMgr.ModifyShengWangValue(client, _Config.YanHuiCfg.GetShengWang, "情侣祝福榜宴会", true);
            }

            return StdErrorCode.Error_Success;
        }

        /// <summary>
        /// 获取宴会参加次数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toCoupleId"></param>
        /// <returns></returns>
        private int GetJoinPartyNum(GameClient client, int toCoupleId)
        {
            if (client == null) return 0;

            string szTxt = Global.GetRoleParamByName(client, RoleParamName.CoupleWishYanHuiFlag);
            string[] fields = !string.IsNullOrEmpty(szTxt) ? szTxt.Split(',') : null;
            if (fields != null && fields.Length == 2 && Convert.ToInt32(fields[0]) == toCoupleId)
            {
                return Convert.ToInt32(fields[1]);
            }

            return 0;
        }

        /// <summary>
        /// 增加宴会参加次数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toCoupleId"></param>
        /// <param name="addNum"></param>
        private void AddJoinPartyNum(GameClient client, int toCoupleId, int addNum = 1)
        {
            int totalNum = addNum + GetJoinPartyNum(client, toCoupleId);
            Global.SaveRoleParamsStringToDB(client, RoleParamName.CoupleWishYanHuiFlag, string.Format("{0},{1}", toCoupleId, totalNum), true);
        }
        #endregion 宴会

        #region 雕像
        /// <summary>
        /// 获取雕像数据
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public CoupleWishTop1AdmireData HandleQueryAdmireData(GameClient client)
        {
            CoupleWishTop1AdmireData data = new CoupleWishTop1AdmireData();
            if (_Statue != null  && _Statue.IsDivorced != 1 && _Statue.DbCoupleId > 0
                    && _Statue.ManRoleDataEx != null && _Statue.WifeRoleDataEx != null)
            {
                data.DbCoupleId = _Statue.DbCoupleId;
                data.ManSelector = Global.RoleDataEx2RoleData4Selector(
                    DataHelper.BytesToObject<RoleDataEx>(_Statue.ManRoleDataEx, 0, _Statue.ManRoleDataEx.Length));
                data.WifeSelector = Global.RoleDataEx2RoleData4Selector(
                    DataHelper.BytesToObject<RoleDataEx>(_Statue.WifeRoleDataEx, 0, _Statue.WifeRoleDataEx.Length));
                data.BeAdmireCount = _Statue.BeAdmireCount;       
            }
            data.MyAdmireCount = GetAdmireCount(client, TimeUtil.MakeYearMonthDay(TimeUtil.NowDateTime()));
            return data;
        }

        /// <summary>
        /// 膜拜雕像
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toCoupleId"></param>
        /// <param name="admireType"></param>
        /// <returns></returns>
        public int HandleAdmireStatue(GameClient client, int toCoupleId, int admireType)
        {
            int toDay = TimeUtil.MakeYearMonthDay(TimeUtil.NowDateTime());
            string strcmd = "";
            MoBaiData MoBaiConfig = null;
            if (!Data.MoBaiDataInfoList.TryGetValue((int)MoBaiTypes.CoupleWish, out MoBaiConfig))
                return StdErrorCode.Error_Config_Fault;

            if (client.ClientData.ChangeLifeCount < MoBaiConfig.MinZhuanSheng ||
                (client.ClientData.ChangeLifeCount == MoBaiConfig.MinZhuanSheng && client.ClientData.Level < MoBaiConfig.MinLevel))
                return StdErrorCode.Error_Level_Limit;

//             不管有没有雕像都可以膜拜
//             if (this._Statue == null || this._Statue.DbCoupleId != toCoupleId
//                 || this._Statue.ManRoleDataEx == null || this._Statue.WifeRoleDataEx == null)
//                 return StdErrorCode.Error_Operation_Denied;

            int maxAdmireNum = MoBaiConfig.AdrationMaxLimit;
            int hadAdmireCount = GetAdmireCount(client, toDay);
            if (this._Statue != null && this._Statue.IsDivorced != 1 && this._Statue.DbCoupleId > 0 &&
                (client.ClientData.RoleID == this._Statue.Man.RoleId || client.ClientData.RoleID == this._Statue.Wife.RoleId))
            {
                // 玩家未离婚，且是雕像情侣有额外次数
                maxAdmireNum += MoBaiConfig.ExtraNumber;
            }

            // 玩家是VIP 有额外的次数
            int nVIPLev = client.ClientData.VipLevel;
            int[] nArrayVIPAdded = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPMoBaiNum");
            if (nVIPLev > (int)VIPEumValue.VIPENUMVALUE_MAXLEVEL || (nArrayVIPAdded.Length > 13 || nArrayVIPAdded.Length < 1))
                return StdErrorCode.Error_Config_Fault;
            maxAdmireNum += nArrayVIPAdded[nVIPLev];

            // 节日活动多倍
            JieRiMultAwardActivity activity = HuodongCachingMgr.GetJieRiMultAwardActivity();
            JieRiMultConfig config = activity != null ? activity.GetConfig((int)MultActivityType.DiaoXiangCount) : null;
            if (null != config)
                maxAdmireNum = maxAdmireNum * ((int)config.GetMult() + 1)/*做倍数处理的时候减了1*/;

            // 膜拜次数达到上限
            if (hadAdmireCount >= maxAdmireNum)
                return StdErrorCode.Error_No_Residue_Degree;

            // 金币膜拜
            if (admireType == 1 && Global.GetTotalBindTongQianAndTongQianVal(client) < MoBaiConfig.NeedJinBi)
                return StdErrorCode.Error_JinBi_Not_Enough;

            // 钻石膜拜
            if (admireType == 2 && client.ClientData.UserMoney < MoBaiConfig.NeedZuanShi)
                return StdErrorCode.Error_ZuanShi_Not_Enough;

            int ec = TianTiClient.getInstance().CoupleWishAdmire(client.ClientData.RoleID, client.ClientData.ZoneID, admireType, toCoupleId);
            //if (ec < 0) return ec;

            double nRate = client.ClientData.ChangeLifeCount == 0 ? 1 : Data.ChangeLifeEverydayExpRate[client.ClientData.ChangeLifeCount];
            if (admireType == 1)
            {
                Global.SubBindTongQianAndTongQian(client, MoBaiConfig.NeedJinBi, "膜拜情侣祝福");

                // 配置值*转生倍率
                int nExp = (int)(nRate * MoBaiConfig.JinBiExpAward);
                if (nExp > 0)
                    GameManager.ClientMgr.ProcessRoleExperience(client, nExp, true);

                // 战功
                if (MoBaiConfig.JinBiZhanGongAward > 0)
                {
                    GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                        client, ref MoBaiConfig.JinBiZhanGongAward, AddBangGongTypes.CoupleWishMoBai);
                }

                if (MoBaiConfig.LingJingAwardByJinBi > 0)
                {
                    GameManager.ClientMgr.ModifyMUMoHeValue(client, MoBaiConfig.LingJingAwardByJinBi, "膜拜情侣祝福", true, true);
                }
            }

            if (admireType == 2)
            {

                GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                        client, MoBaiConfig.NeedZuanShi, "膜拜情侣祝福");

                // 配置值*转生倍率
                int nExp = (int)(nRate * MoBaiConfig.ZuanShiExpAward);
                if (nExp > 0)
                    GameManager.ClientMgr.ProcessRoleExperience(client, nExp, true);

                // 战功
                if (MoBaiConfig.ZuanShiZhanGongAward > 0)
                {
                    GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                        client, ref MoBaiConfig.ZuanShiZhanGongAward, AddBangGongTypes.CoupleWishMoBai);
                }

                if (MoBaiConfig.LingJingAwardByZuanShi > 0)
                {
                    GameManager.ClientMgr.ModifyMUMoHeValue(client, MoBaiConfig.LingJingAwardByZuanShi, "膜拜情侣祝福", true, true);
                }
            }

            AddAdmireCount(client, toDay, toCoupleId);
            if (this._Statue != null && this._Statue.DbCoupleId > 0 && this._Statue.DbCoupleId == toCoupleId)
            {
                this._Statue.BeAdmireCount++; // 先手工临时加1次，让本服膜拜的人立即看到, 等一会会自动从中心同步过来
            }
            return StdErrorCode.Error_Success;
        }

        /// <summary>
        /// 获取我膜拜了多少次
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toCoupleId"></param>
        /// <returns></returns>
        private int GetAdmireCount(GameClient client, int toDay)
        {
            if (client == null) return 0;

            string szAdmire = Global.GetRoleParamByName(client, RoleParamName.CoupleWishAdmireFlag);
            string[] szAdmireFields = !string.IsNullOrEmpty(szAdmire) ? szAdmire.Split(',') : null;
            if (szAdmireFields != null && szAdmireFields.Length == 3 && Convert.ToInt32(szAdmireFields[0]) == toDay)
            {
                return Convert.ToInt32(szAdmireFields[1]);
            }

            return 0;
        }

        /// <summary>
        /// 增加膜拜次数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toCoupleId"></param>
        /// <param name="addCount"></param>
        /// <returns></returns>
        private void AddAdmireCount(GameClient client, int toDay, int toCoupleId, int addCount = 1)
        {
            if (client == null) return;

            int totalCount = addCount + GetAdmireCount(client, toDay);
            Global.SaveRoleParamsStringToDB(client, RoleParamName.CoupleWishAdmireFlag, string.Format("{0},{1},{2}", toDay, totalCount, toCoupleId), true);
        }

        /// <summary>
        /// 刷新雕像
        /// </summary>
        /// <param name="manStatue"></param>
        /// <param name="wifeStatue"></param>
        private void ReshowCoupleStatue(RoleDataEx manStatue, RoleDataEx wifeStatue)
        {
            NPC manNpc = NPCGeneralManager.FindNPC(GameManager.MainMapCode, FakeRoleNpcId.CoupleWishMan);
            if (null != manNpc)
            {
                if (manStatue == null)
                {
                    manNpc.ShowNpc = true;
                    GameManager.ClientMgr.NotifyMySelfNewNPCBy9Grid(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, manNpc);
                    FakeRoleManager.ProcessDelFakeRoleByType(FakeRoleTypes.CoupleWishMan, true);
                }
                else
                {
                    manNpc.ShowNpc = false;
                    GameManager.ClientMgr.NotifyMySelfDelNPCBy9Grid(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, manNpc);
                    FakeRoleManager.ProcessDelFakeRoleByType(FakeRoleTypes.CoupleWishMan, true);
                    SafeClientData clientData = new SafeClientData();
                    clientData.RoleData = manStatue;
                    FakeRoleManager.ProcessNewFakeRole(clientData, manNpc.MapCode, FakeRoleTypes.CoupleWishMan, (int)manNpc.CurrentDir, (int)manNpc.CurrentPos.X, (int)manNpc.CurrentPos.Y, FakeRoleNpcId.CoupleWishMan);
                }
            }

            NPC wifeNpc = NPCGeneralManager.FindNPC(GameManager.MainMapCode, FakeRoleNpcId.CoupleWishWife);
            if (null != wifeNpc)
            {
                if (wifeStatue == null)
                {
                   // wifeNpc.ShowNpc = true;
                   // GameManager.ClientMgr.NotifyMySelfNewNPCBy9Grid(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, wifeNpc);

                    wifeNpc.ShowNpc = true;
                    GameManager.ClientMgr.NotifyMySelfNewNPCBy9Grid(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, wifeNpc);
                    FakeRoleManager.ProcessDelFakeRoleByType(FakeRoleTypes.CoupleWishWife, true);
                }
                else
                {
                    wifeNpc.ShowNpc = false;
                    GameManager.ClientMgr.NotifyMySelfDelNPCBy9Grid(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, wifeNpc);
                    FakeRoleManager.ProcessDelFakeRoleByType(FakeRoleTypes.CoupleWishWife, true);
                    SafeClientData clientData = new SafeClientData();
                    clientData.RoleData = wifeStatue;
                    FakeRoleManager.ProcessNewFakeRole(clientData, wifeNpc.MapCode, FakeRoleTypes.CoupleWishWife, (int)wifeNpc.CurrentDir, (int)wifeNpc.CurrentPos.X, (int)wifeNpc.CurrentPos.Y, FakeRoleNpcId.CoupleWishWife);
                }

            }
        }
        #endregion
    }
}
