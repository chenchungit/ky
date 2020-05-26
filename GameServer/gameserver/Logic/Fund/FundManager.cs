using GameServer.Core.Executor;
using GameServer.Server;
using GameServer.Tools;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic
{
    public class FundManager : IManager, ICmdProcessorEx
    {
        #region ----------接口
        private static FundManager instance = new FundManager();
        public static FundManager getInstance()
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
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_FUND_INFO, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_FUND_BUY, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_FUND_AWARD, 1, 1, getInstance());

            return true;
        }

        public bool showdown() { return true; }
        public bool destroy() { return true; }
        public bool processCmd(GameClient client, string[] cmdParams) { return false; }
    
        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_FUND_INFO:
                    return ProcessFundInfoCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_FUND_BUY:
                    return ProcessFundBuyCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_FUND_AWARD:
                    return ProcessFundAwardCmd(client, nID, bytes, cmdParams);
            }
            return true;
        }

        public bool ProcessFundInfoCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                FundData data = FundGetData(client);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_FUND_INFO, data);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessFundBuyCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                int fundType = int.Parse(cmdParams[0]);
                FundData data = FundBuy(client, fundType);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_FUND_BUY, data);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessFundAwardCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                int fundType = int.Parse(cmdParams[0]);
                FundData data = FundAward(client, fundType);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_FUND_AWARD, data);

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
        /// <summary>
        /// 基金——数据
        /// </summary>
        private static FundData FundGetData(GameClient client)
        {
            FundData fundData = GetFundData(client);
            bool isOpen = IsGongNengOpened(client);
            if (fundData.IsOpen!=isOpen)
                initFundData(client);

            return GetFundData(client);
        }

        /// <summary>
        /// 基金——购买
        /// </summary>
        private static FundData FundBuy(GameClient client, int fundType)
        {
            FundData myData = GetFundData(client);
            if (!myData.IsOpen) return myData;
            myData.FundType = fundType;

            // 如果1.9的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot9))
            {
                myData.State = (int)EFundError.ENoOpen;
                return myData;
            }            

            //类型错误
            if (!myData.FundDic.ContainsKey(fundType))
            {
                myData.State = (int)EFundError.Error;
                return myData;
            }

            //已购买
            FundItem myItem = myData.FundDic[fundType];
            if(myItem.BuyType == (int)EFundBuy.Have)
            {
                myData.State = (int)EFundError.EIsBuy;
                return myData;
            }

            //vip限制
            if(myItem.BuyType == (int)EFundBuy.Limit)
            {
                myData.State = (int)EFundError.EVipLimit;
                return myData;
            }

            //购买钻石不足
            FundInfo fundInfo = _fundDic[myItem.FundID];
            if (fundInfo.Price > client.ClientData.UserMoney)
            {
                myData.State = (int)EFundError.ENoMoney;
                return myData;
            }

            //扣钱
            bool result = GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, fundInfo.Price, "基金购买", true, 1, false);
            if (!result)
            {
                myData.State = (int)EFundError.Error;
                return myData;
            }
            
            //数据库保存
            DateTime buyTime = DateTime.Now;
            FundDBItem dbItem = new FundDBItem();
            dbItem.zoneID = client.ClientData.ZoneID;
            dbItem.UserID = client.strUserID;
            dbItem.RoleID = client.ClientData.RoleID;
            dbItem.FundType = myData.FundType;
            dbItem.FundID = myItem.FundID;
            dbItem.BuyTime = buyTime;
            dbItem.State = (int)EFundState.Now;         
            if (!DBFundBuy(client, dbItem))
            {
                myData.State = (int)EFundError.Error;
                return myData;
            }

            myItem.BuyType = (int)EFundBuy.Have;
            myItem.BuyTime = buyTime;

            if(myItem.FundType == (int)EFund.Login)
                myItem.Value1 = Global.GetOffsetDay(DateTime.Now) - Global.GetOffsetDay(myItem.BuyTime) + 1;
      
            //检查奖励
            FundAwardInfo awardInfo = _fundAwardDic[myItem.AwardID];
            if (myItem.Value1 >= awardInfo.Value1 && myItem.Value2 >= awardInfo.Value2)
                myItem.AwardType = (int)EFundAward.Can;
            else
                myItem.AwardType = (int)EFundAward.Limit;

            myData.State = (int)EFundError.Succ;
            myData.FundType = fundType;

            CheckActivityTip(client);
            return myData;
        }

        /// <summary>
        /// 基金——领奖
        /// </summary>
        private static FundData FundAward(GameClient client, int fundType)
        {
            FundData myData = GetFundData(client);
            if (!myData.IsOpen) return myData;
            myData.FundType = fundType;

            //类型错误
            if (!myData.FundDic.ContainsKey(fundType))
            {
                myData.State = (int)EFundError.Error;
                return myData;
            }

            //未购买
            FundItem myItem = myData.FundDic[fundType];
            if (myItem.BuyType != (int)EFundBuy.Have)
            {
                myData.State = (int)EFundError.ENoBuy;
                return myData;
            }

            //未达到领奖条件
            if (myItem.AwardType == (int)EFundAward.Limit)
            {
                myData.State = (int)EFundError.EAwardLimit;
                return myData;
            }

            //已经领取
            if (myItem.AwardType == (int)EFundAward.Have)
            {
                myData.State = (int)EFundError.EAward;
                return myData;
            }

            //数据库保存
            DateTime buyTime = DateTime.Now;
            FundDBItem dbItem = new FundDBItem();
            dbItem.zoneID = client.ClientData.ZoneID;
            dbItem.UserID = client.strUserID;
            dbItem.RoleID = client.ClientData.RoleID;
            dbItem.FundType = myData.FundType;
            dbItem.FundID = myItem.FundID;
            dbItem.BuyTime = buyTime;
            dbItem.AwardID = myItem.AwardID;

            int fundState = (int)EFundState.Now;
            //最大奖励
            bool isAwardMax = (from info in _fundAwardDic.Values
                               where info.FundType == myItem.FundType && info.FundID == myItem.FundID && info.AwardID > myItem.AwardID
                               select info).Any();
            //最大基金
            bool isFundMax = (from info in _fundDic.Values
                              where info.FundType == myItem.FundType && info.FundID > myItem.FundID
                              select info).Any();


            if (!isAwardMax && !isFundMax)
                fundState = (int)EFundState.End;

            dbItem.State = fundState;
            if (!DBFundAward(client, dbItem))
            {
                myData.State = (int)EFundError.Error;
                return myData;
            }

            //领取奖励
            FundAwardInfo awardInfo = _fundAwardDic[myItem.AwardID];
            bool isAddDiamond = AddDiamone(client, awardInfo.AwardIsBind, awardInfo.AwardCount);
            if (!isAddDiamond)
            {
                myData.State = (int)EFundError.Error;
                return myData;
            }

            //
            myData.State = (int)EFundError.Succ;
            if (dbItem.State == (int)EFundState.End)
            {
                myItem.AwardType = (int)EFundAward.Have;
                CheckActivityTip(client);
                return myData;
            }

            initFundAwardNext(client, myItem);
            CheckActivityTip(client);

            myData.FundType = fundType;
            return myData;
        }

        //加钻石
        private static bool AddDiamone(GameClient client, bool isBind, int diamond)
        {
            bool result = false;
            if (isBind)
            {
                result = GameManager.ClientMgr.AddUserGold(client, diamond, "基金绑钻");
            }
            else
            {
                result = GameManager.ClientMgr.AddUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, diamond, "基金钻石");
            }

            return result;
        }
        #endregion

        #region ----------初始化
        public static FundData GetFundData(GameClient client)
        {
            lock (client.ClientData.LockFund)
            {
                return client.ClientData.MyFundData;
            }
        }

        public static void initFundData(GameClient client)
        {
            lock (client.ClientData.LockFund)
            {
                FundData fundData = new FundData();
                bool isOpen = IsGongNengOpened(client);
                if (!isOpen)
                {
                    client.ClientData.MyFundData = fundData;
                    return;
                }

                fundData.IsOpen = true;
                fundData.FundDic.Add((int)EFund.ChangeLife, initFundItem(client, EFund.ChangeLife));
                fundData.FundDic.Add((int)EFund.Login, initFundItem(client, EFund.Login));
                fundData.FundDic.Add((int)EFund.Money, initFundItem(client, EFund.Money));

                List<FundDBItem> dbItemList = DBFundInfo(client);
                if (dbItemList == null)
                {
                    client.ClientData.MyFundData = fundData;
                    return;
                }

                foreach (FundDBItem dbItem in dbItemList)
                {
                    if (!fundData.FundDic.ContainsKey(dbItem.FundType) || dbItem.State <= (int)EFundState.Old) continue;

                    FundItem fundItem = fundData.FundDic[dbItem.FundType];
                    fundItem.BuyType = (int)EFundBuy.Have;
                    fundItem.BuyTime = dbItem.BuyTime;
                    fundItem.FundID = dbItem.FundID;
                    fundItem.AwardID = dbItem.AwardID;
                    fundItem.AwardType = (int)EFundAward.Have;

                    if (fundItem.FundType == (int)EFund.Money)
                    {
                        fundItem.Value1 = dbItem.Value1;
                        fundItem.Value2 = dbItem.Value2;
                    }

                    if (fundItem.FundType == (int)EFund.Login && fundItem.BuyTime > DateTime.MinValue)
                        fundItem.Value1 = Global.GetOffsetDay(DateTime.Now) - Global.GetOffsetDay(fundItem.BuyTime) + 1;

                    if (dbItem.State == (int)EFundState.Now)
                        initFundAwardNext(client, fundItem);
                }

                client.ClientData.MyFundData = fundData;
                CheckActivityTip(client);
            }
        }

        private static FundItem initFundItem(GameClient client, EFund fundType)
        {
            lock (client.ClientData.LockFund)
            {
                FundInfo fundInfo = (from info in _fundDic.Values
                                     where info.FundType == (int)fundType
                                     orderby info.FundID
                                     select info).First();

                FundItem item = new FundItem();
                item.FundID = fundInfo.FundID;
                item.FundType = (int)fundType;
                item.BuyType = (int)EFundBuy.Limit;
                if (client.ClientData.VipLevel >= fundInfo.MinVip) item.BuyType = (int)EFundBuy.Can;

                FundAwardInfo awardInfo = (from info in _fundAwardDic.Values
                                           where info.FundType == (int)fundType && info.FundID == fundInfo.FundID
                                           orderby info.AwardID
                                           select info).First();

                item.AwardID = awardInfo.AwardID;
                item.AwardType = (int)EFundAward.Limit;
                checkFundItemValue(client, item);

                return item;
            }
        }

        private static void checkFundItemValue(GameClient client, FundItem fundItem)
        {
            lock (client.ClientData.LockFund)
            {
                switch (fundItem.FundType)
                {
                    case (int)EFund.ChangeLife:
                        fundItem.Value1 = client.ClientData.ChangeLifeCount;
                        fundItem.Value2 = client.ClientData.Level;
                        break;
                    case (int)EFund.Login:
                        if (fundItem.BuyTime > DateTime.MinValue)
                            fundItem.Value1 = Global.GetOffsetDay(DateTime.Now) - Global.GetOffsetDay(fundItem.BuyTime) + 1;
                        break;
                    case (int)EFund.Money:
                        break;
                }
            }
        }

        private static void initFundAwardNext(GameClient client, FundItem fundItem)
        {
            lock (client.ClientData.LockFund)
            {
                var tempAwardList = from info in _fundAwardDic.Values
                                    where info.FundType == fundItem.FundType && info.FundID == fundItem.FundID && info.AwardID > fundItem.AwardID
                                    orderby info.AwardID
                                    select info;
                //下一个奖励
                if (tempAwardList.Any())
                {
                    FundAwardInfo awardInfo = tempAwardList.First();
                    fundItem.AwardID = awardInfo.AwardID;

                    if (fundItem.Value1 >= awardInfo.Value1 && fundItem.Value2 >= awardInfo.Value2)
                        fundItem.AwardType = (int)EFundAward.Can;
                    else
                        fundItem.AwardType = (int)EFundAward.Limit;

                    return;
                }
            }

            //下一个基金
            var tempFundList = from info in _fundDic.Values
                               where info.FundType == fundItem.FundType && info.FundID > fundItem.FundID
                               orderby info.FundID
                               select info;

            if (tempFundList.Any())
            {
                FundInfo fundInfo = tempFundList.First();
                fundItem.FundID = fundInfo.FundID;
                fundItem.BuyTime = DateTime.MinValue;
                fundItem.BuyType = (int)EFundBuy.Limit;
                if (client.ClientData.VipLevel >= fundInfo.MinVip) fundItem.BuyType = (int)EFundBuy.Can;

                FundAwardInfo awardInfo = (from award in _fundAwardDic.Values
                                           where award.FundType == fundItem.FundType && award.FundID == fundItem.FundID && award.AwardID > fundItem.AwardID
                                           orderby award.AwardID
                                           select award).First();

                fundItem.AwardID = awardInfo.AwardID;
                fundItem.AwardType = (int)EFundAward.Limit;
                fundItem.Value1 = 0;
                fundItem.Value2 = 0;
            }

        }

        #endregion

        #region ----------数据库

        private static List<FundDBItem> DBFundInfo(GameClient client)
        {   
            List<FundDBItem> list = new List<FundDBItem>();
            list = Global.sendToDB<List<FundDBItem>, int>((int)TCPGameServerCmds.CMD_DB_FUND_INFO, client.ClientData.RoleID, client.ServerId);
            return list;
        }

        private static bool DBFundBuy(GameClient client,FundDBItem item)
        {
            bool result = Global.sendToDB<bool, FundDBItem>((int)TCPGameServerCmds.CMD_DB_FUND_BUY, item, client.ServerId);
            return result;
        }

        private static bool DBFundAward(GameClient client, FundDBItem item)
        {
            bool result = Global.sendToDB<bool, FundDBItem>((int)TCPGameServerCmds.CMD_DB_FUND_AWARD, item, client.ServerId);
            return result;
        }

        private static bool DBFundMoney(GameClient client, FundDBItem item)
        {
            bool result = Global.sendToDB<bool, FundDBItem>((int)TCPGameServerCmds.CMD_DB_FUND_MONEY, item, client.ServerId);
            return result;
        }

        #endregion

        #region ----------其他

        /// <summary>
        /// 判断功能是否开启
        /// </summary>
        public static bool IsGongNengOpened(GameClient client, bool hint = false)
        {
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.Fund))
            {
                return false;
            }

            return GlobalNew.IsGongNengOpened(client, GongNengIDs.Fund, hint);
        }

        public static void FundChangeLife(GameClient client)
        {
            FundData fundData = GetFundData(client);
            if (fundData == null || !fundData.IsOpen) return;
            
            if(!fundData.FundDic.ContainsKey((int)EFund.ChangeLife))return;

            FundItem fundItem = fundData.FundDic[(int)EFund.ChangeLife];
            fundItem.Value1 = client.ClientData.ChangeLifeCount;
            fundItem.Value2 = client.ClientData.Level;

            if (fundItem.BuyType != (int)EFundBuy.Have || fundItem.AwardType != (int)EFundAward.Limit) return;

            FundAwardInfo awardInfo = _fundAwardDic[fundItem.AwardID];
            if (fundItem.Value1 >= awardInfo.Value1 && fundItem.Value2 >= awardInfo.Value2)
            {
                fundItem.AwardType = (int)EFundAward.Can;
                CheckActivityTip(client);
            }
        }

        public static void FundVip(GameClient client)
        {
            FundData fundData = GetFundData(client);
            if (fundData == null || !fundData.IsOpen) return;

            bool isTip = false;
            foreach (var item in fundData.FundDic.Values)
            {
                if (item.BuyType == (int)EFundBuy.Limit)
                {
                    FundInfo info = _fundDic[item.FundID];
                    if (client.ClientData.VipLevel >= info.MinVip)
                    {
                        item.BuyType = (int)EFundAward.Can;
                        isTip = true;
                    }
                }
            }

            if (isTip) CheckActivityTip(client);
        }

        public static void FundMoneyCost(GameClient client,int moneyCost)
        {
            FundData fundData = GetFundData(client);
            if (fundData == null || !fundData.IsOpen) return;

            if (!fundData.FundDic.ContainsKey((int)EFund.Money)) return;

            FundItem fundItem = fundData.FundDic[(int)EFund.Money];
            if (fundItem.BuyType != (int)EFundBuy.Have) return;

            FundDBItem dbItem = new FundDBItem();
            dbItem.UserID = client.strUserID;
            dbItem.RoleID = client.ClientData.RoleID;
            dbItem.Value1 = 0;
            dbItem.Value2 = moneyCost;
            bool result = DBFundMoney(client, dbItem);
            if (!result) return;

            fundItem.Value2 += moneyCost;
            FundAwardInfo awardInfo = _fundAwardDic[fundItem.AwardID];
            if (fundItem.AwardType == (int)EFundAward.Limit && fundItem.Value1 >= awardInfo.Value1 && fundItem.Value2 >= awardInfo.Value2)
            {
                fundItem.AwardType = (int)EFundAward.Can;
                CheckActivityTip(client);
            }
        }

        #endregion

        #region ----------配置

        private static Dictionary<int, FundInfo> _fundDic = new Dictionary<int, FundInfo>();
        private static Dictionary<int, FundAwardInfo> _fundAwardDic = new Dictionary<int, FundAwardInfo>();

        private static bool InitConfig()
        {
            string fileName = "";
            XElement xml = null;
            IEnumerable<XElement> nodes;

            try
            {
                #region ----------FundSet
                _fundDic.Clear();
                fileName = Global.GameResPath("Config/Fund/FundSet.xml");

                xml = CheckHelper.LoadXml(fileName);
                if (null == xml) return false;
                nodes = xml.Elements();
                foreach (var xmlItem in nodes)
                {
                    if (xmlItem == null) continue;

                    FundInfo config = new FundInfo();
                    config.FundID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MainID", "0"));
                    config.FundType = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "PageID", "0"));
                    config.MinVip = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MinVip", "0"));
                    config.NextID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NextID", "0"));
                    config.Price = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Price", "0"));

                    _fundDic.Add(config.FundID, config);
                }
                #endregion

                #region ----------Fund
                _fundAwardDic.Clear();
                fileName = Global.GameResPath("Config/Fund/Fund.xml");

                xml = CheckHelper.LoadXml(fileName);
                if (null == xml) return false;
                nodes = xml.Elements();
                foreach (var xmlItem in nodes)
                {
                    if (xmlItem == null) continue;

                    FundAwardInfo awardConfig = new FundAwardInfo();
                    awardConfig.AwardID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    awardConfig.FundID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MainID", "0"));
                    awardConfig.FundType = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "GoalType", "0"));
                    awardConfig.AwardIsBind = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "RewardType", "0")) > 0;
                    awardConfig.AwardCount = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "RewardCount", "0"));

                    string[] numArr = Global.GetDefAttributeStr(xmlItem, "GoalNum", "0,0").Split(',');

                    switch (awardConfig.FundType)
                    {
                        case (int)EFund.ChangeLife:
                            awardConfig.Value1 = int.Parse(numArr[0]);
                            awardConfig.Value2 = int.Parse(numArr[1]);
                            break;
                        case (int)EFund.Login:
                            awardConfig.Value1 = int.Parse(numArr[0]);
                            break;
                        case (int)EFund.Money:
                            awardConfig.Value1 = int.Parse(numArr[0]);
                            awardConfig.Value2 = int.Parse(numArr[1]);
                            break;
                    }

                    _fundAwardDic.Add(awardConfig.AwardID, awardConfig);
                }

                #endregion
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载xml配置文件:{0}, 失败。", fileName), ex);
                return false;
            }

            return true;
        }

        #endregion

        #region ----------感叹号提示

        private static void CheckActivityTip(GameClient client)
        {
            lock (client.ClientData.LockFund)
            {
                FundData fundData = GetFundData(client);
                if (!fundData.IsOpen) return;

                bool isChange = false;
                bool isAll = false;
                List<int> tipTypeList = new List<int>();
                foreach (var item in fundData.FundDic.Values)
                {
                    bool tip = (item.BuyType == (int)EFundBuy.Can || item.AwardType == (int)EFundAward.Can);
                    isAll |= tip;

                    switch (item.FundType)
                    {
                        case (int)EFund.ChangeLife:
                           isChange |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.FundChangeLife, tip);
                            break;
                        case (int)EFund.Login:
                            isChange |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.FundLogin, tip);
                            break;
                        case (int)EFund.Money:
                            isChange |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.FundMoney, tip);
                            break;
                    }
                }

                isChange |= client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.Fund, isAll);
                if (isChange)
                {
                    client._IconStateMgr.SendIconStateToClient(client);
                }
            }
        }

        #endregion
    }
}
