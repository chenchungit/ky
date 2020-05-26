using System;
using GameServer.Logic;
using GameServer.Logic.MUWings;
using Server.Data;
using System.Collections.Generic;
using Server.Tools;
using Server.Protocol;
using GameServer.Logic.LiXianBaiTan;
using GameServer.Tools;
using GameServer.Logic.CheatGuard;
using Tmsk.Contract;
using GameServer.Core.Executor;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 装备洗练
    /// </summary>
    public class SaleCmdsProcessor : ICmdProcessor
    {
        private TCPManager tcpMgr { get { return TCPManager.getInstance(); } }
        private TCPOutPacketPool pool { get { return TCPManager.getInstance().TcpOutPacketPool; } }
        private TCPClientPool tcpClientPool { get { return TCPManager.getInstance().tcpClientPool; } }

        private TCPGameServerCmds CmdID = TCPGameServerCmds.CMD_SPR_OPENMARKET2;

        public SaleCmdsProcessor(TCPGameServerCmds cmdID)
        {
            CmdID = cmdID;
        }

        public static SaleCmdsProcessor getInstance(TCPGameServerCmds cmdID)
        {
            return new SaleCmdsProcessor(cmdID);
        }

        private bool CanUseMarket(GameClient client)
        {
            try
            {
                // 默认等级限制100转0级
                string[] szLevelLimit = GameManager.PlatConfigMgr.GetGameConfigItemStr(PlatConfigNames.TradeLevelLlimit, "0,0").Split(',');
                int minChangeLife = Convert.ToInt32(szLevelLimit[0]);
                int minLevel = Convert.ToInt32(szLevelLimit[1]);
                if (Global.GetUnionLevel(client) < Global.GetUnionLevel(minChangeLife, minLevel))
                {
                    //不满足交易的最小等级
                    return false;
                }

                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// 交易所、拍卖行指令处理
        /// </summary>
        public bool processCmd(GameClient client, string[] cmdParams)
        {
            int nID = (int)CmdID;

            switch (CmdID)
            {
                case TCPGameServerCmds.CMD_SPR_OPENMARKET2:
                    return OpenMarket(client, cmdParams);
                case TCPGameServerCmds.CMD_SPR_MARKETSALEMONEY2:
                    return MarketSaleMoney(client, cmdParams);
                case TCPGameServerCmds.CMD_SPR_SALEGOODS2:
                    return SaleGoods(client, cmdParams);
                case TCPGameServerCmds.CMD_SPR_SELFSALEGOODSLIST2:
                    return SelfSaleGoodsList(client, cmdParams);
                case TCPGameServerCmds.CMD_SPR_OTHERSALEGOODSLIST2:
                    return OtherSaleGoodsList(client, cmdParams);
                case TCPGameServerCmds.CMD_SPR_MARKETROLELIST2:
                    return MarketRoleList(client, cmdParams);
                case TCPGameServerCmds.CMD_SPR_MARKETGOODSLIST2:
                    return MarketGoodsList(client, cmdParams);
                case TCPGameServerCmds.CMD_SPR_MARKETBUYGOODS2:
                    return MarketBuyGoods(client, cmdParams);
            }

            return true;
        }

        private bool OpenMarket(GameClient client, string[] fields)
        {

            int roleID = Convert.ToInt32(fields[0]);
            int offlineMarket = Convert.ToInt32(fields[1]);
            string marketName = fields[2];

            string strcmd = "";
            if (string.IsNullOrEmpty(marketName)) //停止摆摊
            {
                client.ClientData.AllowMarketBuy = false;
                client.ClientData.MarketName = "";

                strcmd = string.Format("{0}:{1}:{2}", roleID, marketName, offlineMarket);
                client.sendCmd((int)CmdID, strcmd);
                return true;
            }

            marketName = marketName.Substring(0, Math.Min(10, marketName.Length));

            //判断是否有需要摆摊的物品
            if (client.ClientData.SaleGoodsDataList.Count <= 0)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, StringUtil.substitute(Global.GetLang("请至少上架一件物品才能摆摊!")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }

            //当前位置是否允许打开交易市场
            if (!Global.AllowOpenMarket(client))
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, StringUtil.substitute(Global.GetLang("只有【勇者大陆】安全区中才允许摆摊!")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }

            client.ClientData.AllowMarketBuy = true;
            client.ClientData.OfflineMarketState = 1;
            client.ClientData.MarketName = marketName;
            return true;
        }

        private bool MarketSaleMoney(GameClient client, string[] fields)
        {
            int roleID = Convert.ToInt32(fields[0]);
            int saleOutMoney = Math.Max(0, Convert.ToInt32(fields[1]));
            int userMoneyPrice = Math.Max(0, Convert.ToInt32(fields[2]));

            if (client.ClientSocket.IsKuaFuLogin)
            {
                return true;
            }

            //是否禁用交易市场购买功能
            int disableMarket = GameManager.GameConfigMgr.GetGameConfigItemInt("disable-market", 0);
            if (disableMarket > 0)
            {
                return true;
            }

            if (!CanUseMarket(client))
            {
                return true;
            }

            if (TradeBlackManager.Instance().IsBanTrade(client.ClientData.RoleID))
            {
                string tip = Global.GetLang("您目前被禁止使用交易行");
                GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, client, tip, GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }

            //如果已经在摆摊中，则不能再上线物品
            /*if (client.ClientData.AllowMarketBuy)
            {
                return true;
            }*/

            string strcmd = "";
            if (saleOutMoney > client.ClientData.YinLiang)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -1, roleID, saleOutMoney, userMoneyPrice, 0);
                client.sendCmd((int)CmdID, strcmd);
                return true;
            }

            //扣除银两
            if (!GameManager.ClientMgr.SubUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, client, saleOutMoney, "交易市场一"))
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -2, roleID, saleOutMoney, userMoneyPrice, 0);
                client.sendCmd((int)CmdID, strcmd);
                return true;
            }

            GoodsData goodsData = Global.GetNewGoodsData((int)SaleGoodsConsts.BaiTanJinBiGoodsID, 0);
            goodsData.Site = (int)SaleGoodsConsts.SaleGoodsID;
            goodsData.SaleMoney1 = 0;
            goodsData.SaleYuanBao = userMoneyPrice;
            goodsData.SaleYinPiao = 0;
            goodsData.Quality = saleOutMoney;
            Global.AddSaleGoodsData(client, goodsData);

            int goodsDbID = Global.AddGoodsDBCommand_Hook(pool, client,
                goodsData.GoodsID,
                goodsData.GCount,
                goodsData.Quality,
                goodsData.Props,
                goodsData.Forge_level,
                goodsData.Forge_level,
                goodsData.Site,
                goodsData.Jewellist,
                false,
                0,
                /**/"临时摆摊需要",
                false,
                Global.ConstGoodsEndTime,
                goodsData.AddPropIndex,
                goodsData.BornIndex,
                goodsData.Lucky,
                goodsData.Strong,
                goodsData.ExcellenceInfo,
                goodsData.AppendPropLev,
                goodsData.ChangeLifeLevForEquip);

            if (goodsDbID < 0)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -3, roleID, saleOutMoney, userMoneyPrice, goodsData.Id);
                client.sendCmd((int)CmdID, strcmd);
                return true;
            }

            goodsData.Id = goodsDbID;

            //向DBServer请求修改物品
            string[] dbFields = null;
            strcmd = Global.FormatUpdateDBGoodsStr(roleID, goodsDbID, "*", "*", "*", "*", goodsData.Site, "*", "*", "*", "*", "*", goodsData.SaleMoney1, goodsData.SaleYuanBao, goodsData.SaleYinPiao, "*", "*", "*", "*", "*", "*", "*", "*"); // 卓越一击 [12/13/2013 LiaoWei] 装备转生
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strcmd, out dbFields, client.ServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -4, roleID, saleOutMoney, userMoneyPrice, goodsData.Id);
                client.sendCmd((int)CmdID, strcmd);
                return true;
            }

            if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -5, roleID, saleOutMoney, userMoneyPrice, goodsData.Id);
                client.sendCmd((int)CmdID, strcmd);
                return true;
            }

            //写入角色物品的得失行为日志(扩展)
            Global.ModRoleGoodsEvent(client, goodsData, 0, "铜钱交易上架");
            EventLogManager.AddGoodsEvent(client, OpTypes.Move, OpTags.None, goodsData.GoodsID, goodsData.Id, 0, goodsData.GCount, "铜钱交易上架");

            //将新修改的物品加入出售物品管理列表
            SaleGoodsItem saleGoodsItem = new SaleGoodsItem()
                        {
                            GoodsDbID = goodsData.Id,
                            SalingGoodsData = goodsData,
                            Client = client,
                        };
            SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

            //如果从0到1，则加入摊位管理
            if (1 == client.ClientData.SaleGoodsDataList.Count)
            {
                SaleRoleManager.AddSaleRoleItem(client);
            }

            client.ClientData.AllowMarketBuy = true;
            client.ClientData.OfflineMarketState = 1;

            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 0, roleID, saleOutMoney, userMoneyPrice, goodsData.Id);
            client.sendCmd((int)CmdID, strcmd);
            return true;
        }

        private bool SaleGoods(GameClient client, string[] fields)
        {
            TCPGameServerCmds nID = CmdID;

            int roleID = Convert.ToInt32(fields[0]);
            int goodsDbID = Convert.ToInt32(fields[1]);
            int site = Convert.ToInt32(fields[2]);
            int saleMoney1 = Convert.ToInt32(fields[3]);
            int saleYuanBao = Convert.ToInt32(fields[4]);
            int saleYinPiao = Convert.ToInt32(fields[5]);
            int saleGoodsCount = Convert.ToInt32(fields[6]);

            // 金币也可以作上架用 ChenXiaojun
            // saleMoney1 = 0;
            saleYinPiao = 0;

            //如果出售金币和元宝都大于0,则提交数据有误,拒绝执行
            if ((saleMoney1 > 0) && (saleYuanBao > 0))
            {
                return true;
            }

            if (client.ClientSocket.IsKuaFuLogin)
            {
                return true;
            }

            //是否禁用交易市场购买功能
            int disableMarket = GameManager.GameConfigMgr.GetGameConfigItemInt("disable-market", 0);
            if (disableMarket > 0)
            {
                return true;
            }

            if (!CanUseMarket(client))
            {
                return true;
            }

            if (TradeBlackManager.Instance().IsBanTrade(client.ClientData.RoleID))
            {
                string tip = Global.GetLang("您目前被禁止使用交易行");
                GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, client, tip, GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }

            //如果已经在摆摊中，则不能再上线物品 // 策划要求去掉这个判断 2014.4.11
            /*if (client.ClientData.AllowMarketBuy)
            {
                return true;
            }*/

            string strcmd = "";
            int bagIndex = 0; //找到空闲的包裹格子

            //修改内存中物品记录
            GoodsData goodsData = Global.GetGoodsByDbID(client, goodsDbID);
            if (null == goodsData)
            {
                goodsData = Global.GetSaleGoodsDataByDbID(client, goodsDbID);
                if (null == goodsData)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("从交易市场定位物品对象失败, CMD={0}, Client={1}, RoleID={2}, GoodsDbID={3}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(client.ClientSocket), roleID, goodsDbID));
                    return true;
                }
                else
                {
                    if (!Global.CanAddGoods(client, goodsData.GoodsID, goodsData.GCount, goodsData.Binding, goodsData.Endtime, true))
                    {
                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, StringUtil.substitute(Global.GetLang("背包已满，无法将物品从市场下架到背包中")),
                            GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                        return true;
                    }
                    bagIndex = Global.GetIdleSlotOfBagGoods(client); //找到空闲的包裹格子
                }
            }
            else //如果是从背包到挂售的列表，则判断此物品是否可以挂售
            {
                if (goodsData.Using > 0)
                {
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", StdErrorCode.Error_Goods_Is_Using, roleID, goodsDbID, site, saleMoney1, saleYuanBao);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SALEGOODS2, strcmd);
                    return true;
                }

                if (goodsData.Binding > 0)
                {
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", -100, roleID, goodsDbID, site, saleMoney1, saleYuanBao);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SALEGOODS2, strcmd);
                    return true;
                }

                if (Global.IsTimeLimitGoods(goodsData))
                {
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", -101, roleID, goodsDbID, site, saleMoney1, saleYuanBao);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SALEGOODS2, strcmd);
                    return true;
                }

                //判断已经挂售的物品是否超过了最大限制
                if (Global.GetSaleGoodsDataCount(client) >= (int)SaleManager.MaxSaleNum)
                {
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", -110, roleID, goodsDbID, site, saleMoney1, saleYuanBao);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SALEGOODS2, strcmd);
                    return true;
                }

                //判断如果要挂售的物品可以叠加，并且出售的数量小于物品的数量，则执行拆分操作
                int gridNum = Global.GetGoodsGridNumByID(goodsData.GoodsID);

                //不做任何处理
                if (gridNum > 1 && saleGoodsCount > 0 && saleGoodsCount < goodsData.GCount)
                {
                    //根据参数命令拆分物品
                    if (TCPProcessCmdResults.RESULT_OK != Global.SplitGoodsByCmdParams(client, client.ClientSocket, (int)TCPGameServerCmds.CMD_SPR_SPLIT_GOODS, roleID, goodsData.Id, goodsData.Site, goodsData.GoodsID, goodsData.GCount - saleGoodsCount, false))
                    {
                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", -201, roleID, goodsDbID, site, saleMoney1, saleYuanBao);
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SALEGOODS2, strcmd);
                        return true;
                    }
                }
            }

            //向DBServer请求修改物品
            string[] dbFields = null;

            strcmd = Global.FormatUpdateDBGoodsStr(roleID, goodsDbID, "*", "*", "*", "*", site, "*", "*", "*", "*", bagIndex, saleMoney1, saleYuanBao, saleYinPiao, "*", "*", "*", "*", "*", "*", "*", "*"); // 卓越一击 [12/13/2013 LiaoWei] 装备转生
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strcmd, out dbFields, client.ServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", -1, roleID, goodsDbID, site, saleMoney1, saleYuanBao, saleYinPiao);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SALEGOODS2, strcmd);
                return true;
            }

            if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", -10, roleID, goodsDbID, site, saleMoney1, saleYuanBao, saleYinPiao);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SALEGOODS2, strcmd);
                return true;
            }
            goodsData.BagIndex = bagIndex;
            if (goodsData.Site != site) //位置没有改变
            {
                if (goodsData.Site == 0 && site == (int)SaleGoodsConsts.SaleGoodsID) //原来在背包, 现在到出售列表
                {
                    Global.RemoveGoodsData(client, goodsData);

                    goodsData.Site = site;
                    goodsData.SaleMoney1 = saleMoney1;
                    goodsData.SaleYuanBao = saleYuanBao;
                    goodsData.SaleYinPiao = saleYinPiao;
                    Global.AddSaleGoodsData(client, goodsData);

                    //写入角色物品的得失行为日志(扩展)
                    Global.ModRoleGoodsEvent(client, goodsData, 0, "交易上架");
                    EventLogManager.AddGoodsEvent(client, OpTypes.Move, OpTags.None, goodsData.GoodsID, goodsData.Id, 0, goodsData.GCount, "交易上架");

                    //将新修改的物品加入出售物品管理列表
                    SaleGoodsItem saleGoodsItem = new SaleGoodsItem()
                    {
                        GoodsDbID = goodsData.Id,
                        SalingGoodsData = goodsData,
                        Client = client,
                    };
                    SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                    //如果从0到1，则加入摊位管理
                    if (1 == client.ClientData.SaleGoodsDataList.Count)
                    {
                        SaleRoleManager.AddSaleRoleItem(client);
                    }

                    client.ClientData.AllowMarketBuy = true;
                    client.ClientData.OfflineMarketState = 1;

                    // 属性改造 去掉 负重[8/15/2013 LiaoWei]
                    //更新重量
                    //Global.UpdateGoodsWeight(client, goodsData, goodsData.GCount, false, false);
                }
                else if (goodsData.Site == (int)SaleGoodsConsts.SaleGoodsID && site == 0) //原来在出售列表, 现在到背包
                {
                    //从出售列表中删除
                    SaleGoodsManager.RemoveSaleGoodsItem(goodsData.Id);

                    Global.RemoveSaleGoodsData(client, goodsData);

                    if ((int)SaleGoodsConsts.BaiTanJinBiGoodsID != goodsData.GoodsID)
                    {
                        goodsData.Site = site;
                        goodsData.SaleMoney1 = 0;
                        goodsData.SaleYuanBao = 0;
                        goodsData.SaleYinPiao = 0;
                        Global.AddGoodsData(client, goodsData);

                        //写入角色物品的得失行为日志(扩展)
                        Global.ModRoleGoodsEvent(client, goodsData, 0, "交易下架");
                        EventLogManager.AddGoodsEvent(client, OpTypes.Move, OpTags.None, goodsData.GoodsID, goodsData.Id, 0, goodsData.GCount, "交易下架");
                    }
                    else
                    {
                        //删除临时摆摊的金币物品
                        //从用户物品中扣除消耗的数量[将dbID对应的物品全部扣除,单个dbid对应的数量为多个也一起扣除]
                        GameManager.ClientMgr.NotifyUseGoods(tcpMgr.MySocketListener, tcpClientPool, pool, client, goodsData, goodsData.GCount, false, true);

                        int addMoney = Math.Max(0, goodsData.Quality);
                        if (addMoney > 0)
                        {
                            //将金币还原回去
                            GameManager.ClientMgr.AddUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, client, addMoney, "金币下架");
                        }

                        //写入角色物品的得失行为日志(扩展)
                        Global.ModRoleGoodsEvent(client, goodsData, 0, "铜钱交易下架");
                        EventLogManager.AddGoodsEvent(client, OpTypes.Move, OpTags.None, goodsData.GoodsID, goodsData.Id, 0, goodsData.GCount, "铜钱交易下架");
                    }

                    //如果从1到0，则删除摊位管理
                    if (0 == client.ClientData.SaleGoodsDataList.Count)
                    {
                        SaleRoleManager.RemoveSaleRoleItem(client.ClientData.RoleID);
                    }

                    // 属性改造 去掉 负重[8/15/2013 LiaoWei]
                    //更新重量
                    //Global.UpdateGoodsWeight(client, goodsData, goodsData.GCount, true, false);
                }
            }

            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", 0, roleID, goodsDbID, site, saleMoney1, saleYuanBao, saleYinPiao);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SALEGOODS2, strcmd);
            return true;
        }

        private bool SelfSaleGoodsList(GameClient client, string[] fields)
        {
            int roleID = Convert.ToInt32(fields[0]);

            if (client.ClientSocket.IsKuaFuLogin)
            {
                client.sendCmd<List<GoodsData>>((int)TCPGameServerCmds.CMD_SPR_SELFSALEGOODSLIST2, null);
                return true;
            }

            List<GoodsData> saleGoodsDataList = client.ClientData.SaleGoodsDataList;
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SELFSALEGOODSLIST2, saleGoodsDataList);
            return true;
        }

        private bool OtherSaleGoodsList(GameClient client, string[] fields)
        {
            int roleID = Convert.ToInt32(fields[0]);
            int otherRoleID = Convert.ToInt32(fields[1]);

            if (client.ClientSocket.IsKuaFuLogin)
            {
                client.sendCmd<List<GoodsData>>((int)TCPGameServerCmds.CMD_SPR_OTHERSALEGOODSLIST2, null);
                return true;
            }

            List<GoodsData> saleGoodsDataList = new List<GoodsData>();
            GameClient otherClient = GameManager.ClientMgr.FindClient(otherRoleID);
            if (null != otherClient)
            {
                saleGoodsDataList = otherClient.ClientData.SaleGoodsDataList;
            }
            else
            {
                saleGoodsDataList = LiXianBaiTanManager.GetLiXianSaleGoodsList(otherRoleID);
            }

            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_OTHERSALEGOODSLIST2, saleGoodsDataList);
            return true;
        }

        private bool MarketRoleList(GameClient client, string[] fields)
        {
            List<SaleRoleData> saleRoleDataList = SaleRoleManager.GetSaleRoleDataList();
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_MARKETROLELIST2, saleRoleDataList);
            return true;
        }

        private bool MarketGoodsList(GameClient client, string[] fields)
        {
            int roleID = Convert.ToInt32(fields[0]);
            int marketSearchType = Convert.ToInt32(fields[1]);
            int startIndex = Convert.ToInt32(fields[2]);
            int maxCount = Convert.ToInt32(fields[3]);
            string marketSearchText = fields[4];

            if (client.ClientSocket.IsKuaFuLogin)
            {
                client.sendCmd<SaleGoodsSearchResultData>((int)TCPGameServerCmds.CMD_SPR_MARKETGOODSLIST2, null);
                return true;
            }

            // 恶意操作限制
            if (CreateRoleLimitManager.Instance().RefreshMarketSlotTicks > 0 &&
                TimeUtil.NOW() - client.ClientData._RefreshMarketTicks < CreateRoleLimitManager.Instance().RefreshMarketSlotTicks)
            {
                // 返回错误信息
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    StringUtil.substitute(Global.GetLang("您操作过快，请稍后再试")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }
            client.ClientData._RefreshMarketTicks = TimeUtil.NOW();

            SaleGoodsSearchResultData saleGoodsSearchResultData = new SaleGoodsSearchResultData();
            if ((int)MarketSearchTypes.SearchAll == marketSearchType) //返回全部
            {
                saleGoodsSearchResultData.saleGoodsDataList = SaleGoodsManager.GetSaleGoodsDataList();
            }
            else if ((int)MarketSearchTypes.SearchGoodsIDs == marketSearchType) //根据物品ID匹配返回
            {
                Dictionary<int, bool> goodsIDDict = new Dictionary<int, bool>();
                string[] searchFileds = marketSearchText.Split(',');
                if (null != searchFileds && searchFileds.Length > 0)
                {
                    for (int i = 0; i < searchFileds.Length; i++)
                    {
                        int searchGoodsID = Global.SafeConvertToInt32(searchFileds[i]);
                        goodsIDDict[searchGoodsID] = true;
                    }

                    saleGoodsSearchResultData.saleGoodsDataList = SaleGoodsManager.FindSaleGoodsDataList(goodsIDDict);
                }

            }
            else if ((int)MarketSearchTypes.SearchRoleName == marketSearchType) //根据角色名称模糊匹配返回
            {
                saleGoodsSearchResultData.saleGoodsDataList = SaleGoodsManager.FindSaleGoodsDataListByRoleName(marketSearchText);
            }
            else if ((int)MarketSearchTypes.TypeAndFilterOpts == marketSearchType)
            {
                string[] searchParams = marketSearchText.Split('$');
                if (searchParams.Length >= 6)
                {
                    int type = Global.SafeConvertToInt32(searchParams[0]);
                    int id = Global.SafeConvertToInt32(searchParams[1]);
                    int moneyFlags = Global.SafeConvertToInt32(searchParams[2]);
                    int colorFlags = Global.SafeConvertToInt32(searchParams[3]);
                    int orderBy = Global.SafeConvertToInt32(searchParams[4]);
                    int orderTypeFlags = 1;
                    List<int> goodsIDs;
                    if (searchParams.Length >= 7)
                    {
                        orderTypeFlags = Global.SafeConvertToInt32(searchParams[5]);
                        goodsIDs = Global.StringToIntList(searchParams[6], '#');
                    }
                    else
                    {
                        goodsIDs = Global.StringToIntList(searchParams[5], '#');
                    }

                    saleGoodsSearchResultData.Type = type;
                    saleGoodsSearchResultData.ID = id;
                    saleGoodsSearchResultData.MoneyFlags = moneyFlags;
                    saleGoodsSearchResultData.ColorFlags = colorFlags;
                    saleGoodsSearchResultData.OrderBy = orderBy;
                    if (moneyFlags <= 0)
                    {
                        moneyFlags = SaleManager.ConstAllMoneyFlags;
                    }
                    if (colorFlags <= 0)
                    {
                        colorFlags = SaleManager.ConstAllColorFlags;
                    }
                    SearchArgs args = new SearchArgs(id, type, moneyFlags, colorFlags, orderBy, orderTypeFlags);
                    if (goodsIDs.IsNullOrEmpty())
                    {
                        saleGoodsSearchResultData.saleGoodsDataList = SaleManager.GetSaleGoodsDataList(args, null);
                        if (null != saleGoodsSearchResultData.saleGoodsDataList)
                        {
                            saleGoodsSearchResultData.TotalCount = saleGoodsSearchResultData.saleGoodsDataList.Count;
                        }
                    }
                    else
                    {
                        //maxCount = Global.GMax(maxCount, (int)SaleGoodsConsts.MaxReturnNum);
                        saleGoodsSearchResultData.saleGoodsDataList = SaleManager.GetSaleGoodsDataList(args, goodsIDs);
                        if (null == saleGoodsSearchResultData.saleGoodsDataList || saleGoodsSearchResultData.saleGoodsDataList.Count == 0)
                        {
                            saleGoodsSearchResultData.TotalCount = -1;
                        }
                        else
                        {
                            saleGoodsSearchResultData.TotalCount = saleGoodsSearchResultData.saleGoodsDataList.Count;
                        }
                    }
                    
                    if (null != saleGoodsSearchResultData.saleGoodsDataList && saleGoodsSearchResultData.saleGoodsDataList.Count > 0)
                    {
                        saleGoodsSearchResultData.StartIndex = startIndex;

                        if (startIndex >= saleGoodsSearchResultData.TotalCount)
                        {
                            saleGoodsSearchResultData.saleGoodsDataList = null;
                        }
                        else
                        {
                            startIndex = Global.GMin(startIndex, saleGoodsSearchResultData.saleGoodsDataList.Count - 1);
                            maxCount = Global.GMin(maxCount, saleGoodsSearchResultData.saleGoodsDataList.Count - startIndex);
                            saleGoodsSearchResultData.saleGoodsDataList = saleGoodsSearchResultData.saleGoodsDataList.GetRange(startIndex, maxCount);
                        }
                    }
                }
            }

            client.sendCmd<SaleGoodsSearchResultData>((int)TCPGameServerCmds.CMD_SPR_MARKETGOODSLIST2, saleGoodsSearchResultData);
            return true;
        }

        private int CalcRealMoneyAfterTax(int money, MoneyTypes moneyType, out int tax)
        {
            tax = 0;
            if (moneyType == MoneyTypes.YinLiang)
            {
                tax = (int)Math.Ceiling(money * SaleManager.JiaoYiShuiJinBi);
                tax = Global.GMax(tax, 0);
            }
            else if (moneyType == MoneyTypes.YuanBao)
            {
                tax = (int)Math.Ceiling(money * SaleManager.JiaoYiShuiZuanShi);
                tax = Global.GMax(tax, 0);
            }
            return money - tax;
        }

        private bool MarketBuyGoods(GameClient client, string[] fields)
        {
            int roleID = Convert.ToInt32(fields[0]);
            int goodsDbID = Convert.ToInt32(fields[1]);
            int goodsID = Convert.ToInt32(fields[2]);
            int clientMoneyType = Convert.ToInt32(fields[3]);
            int clientMoneyValue = Convert.ToInt32(fields[4]);
            int tax = 0;

            if (client.ClientSocket.IsKuaFuLogin)
            {
                return true;
            }

            //是否禁用交易市场购买功能
            int disableMarket = GameManager.GameConfigMgr.GetGameConfigItemInt("disable-market", 0);
            if (disableMarket > 0)
            {
                return true;
            }

            if (!CanUseMarket(client))
            {
                return true;
            }

            if (TradeBlackManager.Instance().IsBanTrade(client.ClientData.RoleID))
            {
                string tip = Global.GetLang("您目前被禁止使用交易行");
                GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, client, tip, GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }

            int salePrice = 0;
            int otherRID = 0;

            GameClient otherClient = null;
            SaleGoodsItem saleGoodsItem = SaleGoodsManager.RemoveSaleGoodsItem(goodsDbID);

            if (null != saleGoodsItem)
            {
                //对方的角色
                otherClient = GameManager.ClientMgr.FindClient(saleGoodsItem.Client.ClientData.RoleID); //查找，确保还在线
                if (null != otherClient)
                {
                    if (otherClient.ClientData.RoleID == client.ClientData.RoleID)
                    {
                        //不能购买自己的物品
                        SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);
                        GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -30, 0, goodsDbID, goodsID, (int)CmdID);
                        return true;
                    }
                    if (!otherClient.ClientData.AllowMarketBuy)
                    {
                        SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);
                        GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -3, 0, goodsDbID, goodsID, (int)CmdID);
                        return true;
                    }
                }
                otherRID = saleGoodsItem.Client.ClientData.RoleID;
            }

            if (null != saleGoodsItem && null != otherClient) //在线购买
            {
                if (TradeBlackManager.Instance().IsBanTrade(otherClient.ClientData.RoleID))
                {
                    SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);
                    string tip = Global.GetLang("对方目前被禁止使用交易行");
                    GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, client, tip, GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                    return true;
                }

                if ((clientMoneyType == (int)MoneyTypes.YinLiang && saleGoodsItem.SalingGoodsData.SaleMoney1 != clientMoneyValue) ||
                    (clientMoneyType == (int)MoneyTypes.YuanBao && saleGoodsItem.SalingGoodsData.SaleYuanBao != clientMoneyValue))
                {
                    SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);
                    GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -40, 0, goodsDbID, goodsID, (int)CmdID);
                    return true;
                }

                GoodsData goodsData = Global.GetSaleGoodsDataByDbID(otherClient, goodsDbID);
                if (null == goodsData)
                {
                    //返回管理
                    SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                    //发送错误消息
                    GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -3, 0, goodsDbID, goodsID, (int)CmdID);
                    return true;
                }

                if (goodsData.GoodsID != goodsID)
                {
                    //返回管理
                    SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                    //发送错误消息
                    GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -1003, 0, goodsDbID, goodsID, (int)CmdID);
                    return true;
                }

                //如果不是特殊的摆摊金币物品
                if ((int)SaleGoodsConsts.BaiTanJinBiGoodsID != goodsData.GoodsID)
                {
                    //判断背包是否够用
                    if (!Global.CanAddGoods(client, goodsData.GoodsID, goodsData.GCount, 0, goodsData.Endtime, true))
                    {
                        //返回管理
                        SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -5, 0, goodsDbID, goodsID, (int)CmdID);
                        return true;
                    }
                }

                //判断游银两余额是否不足
                if (saleGoodsItem.SalingGoodsData.SaleMoney1 > 0 && client.ClientData.YinLiang < saleGoodsItem.SalingGoodsData.SaleMoney1)
                {
                    //返回管理
                    SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                    //发送错误消息
                    GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -10, 0, goodsDbID, goodsID, (int)CmdID);
                    return true;
                }

                //判断元宝余额是否不足
                if (saleGoodsItem.SalingGoodsData.SaleYuanBao > 0 && client.ClientData.UserMoney < saleGoodsItem.SalingGoodsData.SaleYuanBao)
                {
                    //返回管理
                    SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                    //发送错误消息
                    GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -20, 0, goodsDbID, goodsID, (int)CmdID);
                    return true;
                }

                //判断银票个数是否不足
                int yinPiaoGoodsID = (int)GameManager.systemParamsList.GetParamValueIntByName("YinPiaoGoodsID");
                if (saleGoodsItem.SalingGoodsData.SaleYinPiao > 0 && Global.GetTotalGoodsCountByID(client, yinPiaoGoodsID) < saleGoodsItem.SalingGoodsData.SaleYinPiao)
                {
                    //返回管理
                    SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                    //发送错误消息
                    GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -21, 0, goodsDbID, goodsID, (int)CmdID);
                    return true;
                }

                //判断对方的背包空格是否足够
                if (saleGoodsItem.SalingGoodsData.SaleYinPiao > 0)
                {
                    if (!Global.CanAddGoods2(otherClient, yinPiaoGoodsID, saleGoodsItem.SalingGoodsData.SaleYinPiao, 0, Global.ConstGoodsEndTime, true))
                    {
                        //返回管理
                        SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -22, 0, goodsDbID, goodsID, (int)CmdID);
                        return true;
                    }
                }

                //先DBServer请求扣费
                //扣除游戏金币1
                if (saleGoodsItem.SalingGoodsData.SaleMoney1 > 0)
                {
                    if (!GameManager.ClientMgr.SubUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, client, saleGoodsItem.SalingGoodsData.SaleMoney1, "交易市场二"))
                    {
                        //返回管理
                        SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -10, 0, goodsDbID, goodsID, (int)CmdID);
                        return true;
                    }
                    else
                    {
                        //添加游戏金币1
                        GameManager.ClientMgr.AddUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, otherClient, CalcRealMoneyAfterTax(saleGoodsItem.SalingGoodsData.SaleMoney1, MoneyTypes.YinLiang, out tax), "交易市场二");
                        EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.YinLiang, -tax, 0, "交易税");
                    }
                }

                //先DBServer请求扣费
                //扣除用户元宝
                if (saleGoodsItem.SalingGoodsData.SaleYuanBao > 0)
                {
                    if (!GameManager.ClientMgr.SubUserMoney(tcpMgr.MySocketListener, tcpClientPool, pool, client, saleGoodsItem.SalingGoodsData.SaleYuanBao,"新交易市场购买", false))
                    {
                        //返回管理
                        SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -20, 0, goodsDbID, goodsID, (int)CmdID);
                        return true;
                    }
                    else
                    {
                        //添加用户点卷
                        GameManager.ClientMgr.AddUserMoney(tcpMgr.MySocketListener, tcpClientPool, pool, otherClient, CalcRealMoneyAfterTax(saleGoodsItem.SalingGoodsData.SaleYuanBao, MoneyTypes.YuanBao, out tax), "新交易市场出售");
                        EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.YuanBao, -tax, 0, "交易税");
                    }
                }

                salePrice = saleGoodsItem.SalingGoodsData.SaleYuanBao;

                //扣除用户银票
                if (saleGoodsItem.SalingGoodsData.SaleYinPiao > 0)
                {
                    bool usedBinding = false;
                    bool usedTimeLimited = false;

                    //从用户物品中扣除消耗的数量
                    if (!GameManager.ClientMgr.NotifyUseGoods(tcpMgr.MySocketListener, tcpClientPool, pool, client, yinPiaoGoodsID, saleGoodsItem.SalingGoodsData.SaleYinPiao, false, out usedBinding, out usedTimeLimited))
                    {
                        //返回管理
                        SaleGoodsManager.AddSaleGoodsItem(saleGoodsItem);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -21, 0, goodsDbID, goodsID, (int)CmdID);
                        return true;
                    }
                    else
                    {
                        //添加银票
                        //想DBServer请求加入某个新的物品到背包中
                        //添加物品
                        Global.BatchAddGoods(otherClient, yinPiaoGoodsID, CalcRealMoneyAfterTax(saleGoodsItem.SalingGoodsData.SaleYinPiao, MoneyTypes.None, out tax), 0, "交易市场购买后批量添加");
                    }
                }

                int saleMoney1 = goodsData.SaleMoney1;
                int saleYuanBao = goodsData.SaleYuanBao;
                int saleYinPiao = goodsData.SaleYinPiao;
                int site = goodsData.Site;

                GoodsData tradeBlackCopy = new GoodsData(goodsData);

                int saleOutMoney = Math.Max(0, goodsData.Quality);

                goodsData.SaleMoney1 = 0;
                goodsData.SaleYuanBao = 0;
                goodsData.SaleYinPiao = 0;
                goodsData.Site = 0;

                Global.RemoveSaleGoodsData(otherClient, goodsData);

                bool bMoveToTarget = true;
                // 如果不是特殊的摆摊金币物品
                if ((int)SaleGoodsConsts.BaiTanJinBiGoodsID != goodsData.GoodsID)
                {
                    bMoveToTarget = true;
                }
                else
                {
                    // 金币物品不移到玩家身上，直接加金币
                    bMoveToTarget = false;
                }

                //转移物品
                bool ret = GameManager.ClientMgr.MoveGoodsDataToOtherRole(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    goodsData, otherClient, client, bMoveToTarget);

                if (!ret)
                {
                    GiveBackSaleGoodsMoney(client, otherClient, goodsData, saleMoney1, saleYuanBao, site);
                    GameManager.SystemServerEvents.AddEvent(string.Format("转移物品时失败, 交易市场购买, FromRole={0}({1}), ToRole={2}({3}), GoodsDbID={4}, GoodsID={5}, GoodsNum={6}",
                        otherClient.ClientData.RoleID, otherClient.ClientData.RoleName, client.ClientData.RoleID, client.ClientData.RoleName,
                        goodsData.Id,
                        goodsData.GoodsID,
                        goodsData.GCount
                        ),
                        EventLevels.Important);

                    //发送错误消息
                    GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -100, 0, goodsDbID, goodsID, (int)CmdID);
                    return true;
                }

                // 特殊的摆摊金币物品
                if (!bMoveToTarget)
                {
                    // 扣除金币物品
                    if (!GameManager.ClientMgr.NotifyUseGoods(tcpMgr.MySocketListener, tcpClientPool, pool, client, goodsData, goodsData.GCount, false, true))
                    {
                        LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场在线购买金币失败, {0}=>{1}", Global.FormatRoleName4(otherClient), Global.FormatRoleName4(client)));
                        GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, -1004, 0, goodsDbID, goodsID, (int)CmdID);
                        return true;
                    }

                    GameManager.ClientMgr.AddUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, client, saleOutMoney, "摆摊出售金币");
                }

                //写入角色出售的行为日志
                Global.AddRoleSaleEvent(client, goodsData.GoodsID, goodsData.GCount, -saleMoney1, -saleYinPiao, -saleYuanBao, yinPiaoGoodsID, -saleOutMoney);

                //写入角色出售的行为日志
                Global.AddRoleSaleEvent(otherClient, goodsData.GoodsID, -goodsData.GCount, Math.Max(0, saleMoney1 - tax), Math.Max(0, saleYinPiao - tax), Math.Max(0, saleYuanBao - tax), yinPiaoGoodsID, saleOutMoney);

                //通知对方
                GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, otherClient, client, 0, 1, goodsDbID, goodsID, (int)CmdID);

                //通知自己
                GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, otherClient, 0, 0, goodsDbID, goodsID, (int)CmdID);

                //摆摊购买日志
                Global.AddMarketBuyLog(otherClient.ClientData.RoleID, client.ClientData.RoleID, client.ClientData.RoleName, goodsData.GoodsID, goodsData.GCount, goodsData.Forge_level, saleYuanBao, client.ClientData.UserMoney, saleMoney1, client.ClientData.YinLiang, tax, goodsData.ExcellenceInfo);

                // 交易黑名单事件
                TradeBlackManager.Instance().OnMarketBuy(client.ClientData.RoleID, otherClient.ClientData.RoleID, tradeBlackCopy);
            }
            else //离线购买
            {
                LiXianSaleGoodsItem liXianSaleGoodsItem = LiXianBaiTanManager.RemoveLiXianSaleGoodsItem(goodsDbID);

                if (null == liXianSaleGoodsItem) //如果离线的也不存在, 则走在线摆摊的提示错误逻辑
                {
                    //发送错误消息
                    GameManager.ClientMgr.NotifySpriteMarketBuy(tcpMgr.MySocketListener, pool, client, null, -1, 0, goodsDbID, goodsID, (int)CmdID);
                    return true;
                }
                else //离线 摆摊逻辑
                {
                    if (TradeBlackManager.Instance().IsBanTrade(liXianSaleGoodsItem.RoleID))
                    {
                        //返回管理
                        LiXianBaiTanManager.AddLiXianSaleGoodsItem(liXianSaleGoodsItem);

                        string tip = Global.GetLang("对方目前被禁止使用交易行");
                        GameManager.ClientMgr.NotifyImportantMsg(tcpMgr.MySocketListener, pool, client, tip, GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                        return true;
                    }

                    GoodsData goodsData = liXianSaleGoodsItem.SalingGoodsData;
                    if (goodsData.GoodsID != goodsID)
                    {
                        //返回管理
                        LiXianBaiTanManager.AddLiXianSaleGoodsItem(liXianSaleGoodsItem);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, -1003, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);
                        return true;
                    }

                    if ((clientMoneyType == (int)MoneyTypes.YinLiang && liXianSaleGoodsItem.SalingGoodsData.SaleMoney1 != clientMoneyValue) ||
                        (clientMoneyType == (int)MoneyTypes.YuanBao && liXianSaleGoodsItem.SalingGoodsData.SaleYuanBao != clientMoneyValue))
                    {
                        //返回管理
                        LiXianBaiTanManager.AddLiXianSaleGoodsItem(liXianSaleGoodsItem);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, -40, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);
                        return true;
                    }

                    //如果不是特殊的摆摊金币物品
                    if ((int)SaleGoodsConsts.BaiTanJinBiGoodsID != goodsData.GoodsID)
                    {
                        //判断背包是否够用
                        if (!Global.CanAddGoods(client, goodsData.GoodsID, goodsData.GCount, 0, goodsData.Endtime, true))
                        {
                            //返回管理
                            LiXianBaiTanManager.AddLiXianSaleGoodsItem(liXianSaleGoodsItem);

                            //发送错误消息
                            GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, -5, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);
                            return true;
                        }
                    }

                    //判断游银两余额是否不足
                    if (liXianSaleGoodsItem.SalingGoodsData.SaleMoney1 > 0 && client.ClientData.YinLiang < liXianSaleGoodsItem.SalingGoodsData.SaleMoney1)
                    {
                        //返回管理
                        LiXianBaiTanManager.AddLiXianSaleGoodsItem(liXianSaleGoodsItem);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, -10, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);
                        return true;
                    }

                    //判断元宝余额是否不足
                    if (liXianSaleGoodsItem.SalingGoodsData.SaleYuanBao > 0 && client.ClientData.UserMoney < liXianSaleGoodsItem.SalingGoodsData.SaleYuanBao)
                    {
                        //返回管理
                        LiXianBaiTanManager.AddLiXianSaleGoodsItem(liXianSaleGoodsItem);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, -20, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);
                        return true;
                    }

                    //先DBServer请求扣费
                    //扣除游戏金币1
                    //如果不是特殊的摆摊金币物品
                    if ((int)SaleGoodsConsts.BaiTanJinBiGoodsID != goodsData.GoodsID)
                    {
                        if (liXianSaleGoodsItem.SalingGoodsData.SaleMoney1 > 0)
                        {
                            if (!GameManager.ClientMgr.SubUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, client, liXianSaleGoodsItem.SalingGoodsData.SaleMoney1, "交易市场三"))
                            {
                                //返回管理
                                LiXianBaiTanManager.AddLiXianSaleGoodsItem(liXianSaleGoodsItem);

                                //发送错误消息
                                GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, -10, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);
                                return true;
                            }
                            else
                            {
                                //添加游戏金币1
                                GameManager.ClientMgr.AddOfflineUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, liXianSaleGoodsItem.UserID, liXianSaleGoodsItem.RoleID, liXianSaleGoodsItem.RoleName, CalcRealMoneyAfterTax(liXianSaleGoodsItem.SalingGoodsData.SaleMoney1, MoneyTypes.YinLiang, out tax), "交易市场三", client.ClientData.ZoneID);
                            }
                        }
                    }

                    //先DBServer请求扣费
                    //扣除用户元宝
                    if (liXianSaleGoodsItem.SalingGoodsData.SaleYuanBao > 0)
                    {
                        if (!GameManager.ClientMgr.SubUserMoney(tcpMgr.MySocketListener, tcpClientPool, pool, client, liXianSaleGoodsItem.SalingGoodsData.SaleYuanBao, "新交易市场购买", false))
                        {
                            //返回管理
                            LiXianBaiTanManager.AddLiXianSaleGoodsItem(liXianSaleGoodsItem);

                            //发送错误消息
                            GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, -20, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);
                            return true;
                        }
                        else
                        {
                            //添加用户点卷
                            GameManager.ClientMgr.AddOfflineUserMoney(tcpMgr.MySocketListener, tcpClientPool, pool, liXianSaleGoodsItem.RoleID, liXianSaleGoodsItem.RoleName, CalcRealMoneyAfterTax(liXianSaleGoodsItem.SalingGoodsData.SaleYuanBao, MoneyTypes.YuanBao, out tax), "新交易市场出售(离线)", liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.UserID);
                        }
                    }

                    salePrice = liXianSaleGoodsItem.SalingGoodsData.SaleYuanBao;
                    otherRID = liXianSaleGoodsItem.RoleID;

                    int saleMoney1 = goodsData.SaleMoney1;
                    int saleYuanBao = goodsData.SaleYuanBao;
                    int saleYinPiao = goodsData.SaleYinPiao;
                    int site = goodsData.Site;

                    GoodsData tradeBlackCopy = new GoodsData(goodsData);

                    int saleOutMoney = Math.Max(0, goodsData.Quality);
                    
                    goodsData.SaleMoney1 = 0;
                    goodsData.SaleYuanBao = 0;
                    goodsData.SaleYinPiao = 0;
                    goodsData.Site = 0;

                    string userID = GameManager.OnlineUserSession.FindUserID(client.ClientSocket);
                    bool bMoveToTarget = true;
                    // 如果不是特殊的摆摊金币物品
                    if ((int)SaleGoodsConsts.BaiTanJinBiGoodsID != goodsData.GoodsID)
                    {
                        bMoveToTarget = true;
                    }
                    else
                    {
                        // 金币物品不移到玩家身上，直接加金币
                        bMoveToTarget = false;
                    }

                    //转移物品
                    bool ret = GameManager.ClientMgr.MoveGoodsDataToOfflineRole(Global._TCPManager.MySocketListener,
                        Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                        goodsData, liXianSaleGoodsItem.UserID, liXianSaleGoodsItem.RoleID, liXianSaleGoodsItem.RoleName, liXianSaleGoodsItem.RoleLevel, userID, client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Level, bMoveToTarget, client.ClientData.ZoneID);

                    if (!ret)
                    {
                        GiveBackSaleGoodsMoneyOffline(client, liXianSaleGoodsItem.UserID, liXianSaleGoodsItem.RoleID, liXianSaleGoodsItem.RoleName, goodsData, saleMoney1, saleYuanBao, site);
                        GameManager.SystemServerEvents.AddEvent(string.Format("转移物品时失败, 交易市场购买, FromRole={0}({1}), ToRole={2}({3}), GoodsDbID={4}, GoodsID={5}, GoodsNum={6}",
                            liXianSaleGoodsItem.RoleID, liXianSaleGoodsItem.RoleName, client.ClientData.RoleID, client.ClientData.RoleName,
                            goodsData.Id,
                            goodsData.GoodsID,
                            goodsData.GCount
                            ),
                            EventLevels.Important);

                        //发送错误消息
                        GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, -100, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);
                        return true;
                    }

                    // 特殊的摆摊金币物品
                    if (!bMoveToTarget)
                    {
                        // 扣除金币物品
                        if (!GameManager.ClientMgr.NotifyUseGoods(tcpMgr.MySocketListener, tcpClientPool, pool, client, goodsData, goodsData.GCount, false, true))
                        {
                            LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场购买金币失败, {0}=>{1}", liXianSaleGoodsItem.RoleName, Global.FormatRoleName4(client)));
                            GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, -1004, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);
                            return true;
                        }

                        GameManager.ClientMgr.AddUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, client, saleOutMoney, "摆摊出售物品获取金币");
                    }

                    //写入角色出售的行为日志
                    Global.AddRoleSaleEvent2(userID, client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Level, goodsData.GoodsID, goodsData.GCount, -saleMoney1, -saleYinPiao, -saleYuanBao, -saleOutMoney);

                    //写入角色出售的行为日志
                    Global.AddRoleSaleEvent2(liXianSaleGoodsItem.UserID, liXianSaleGoodsItem.RoleID, liXianSaleGoodsItem.RoleName, liXianSaleGoodsItem.RoleLevel, goodsData.GoodsID, -goodsData.GCount, Math.Max(0, saleMoney1 - tax), Math.Max(0, saleYinPiao - tax), Math.Max(0, saleYuanBao - tax), saleOutMoney);

                    //通知自己
                    GameManager.ClientMgr.NotifySpriteMarketBuy2(tcpMgr.MySocketListener, pool, client, liXianSaleGoodsItem.RoleID, 0, 0, goodsDbID, goodsID, liXianSaleGoodsItem.ZoneID, liXianSaleGoodsItem.RoleName, (int)CmdID);

                    //摆摊购买日志
                    Global.AddMarketBuyLog(liXianSaleGoodsItem.RoleID, client.ClientData.RoleID, client.ClientData.RoleName, goodsData.GoodsID, goodsData.GCount, goodsData.Forge_level, saleYuanBao, client.ClientData.UserMoney, saleMoney1, client.ClientData.YinLiang, tax, goodsData.ExcellenceInfo);

                    // 交易黑名单事件
                    TradeBlackManager.Instance().OnMarketBuy(client.ClientData.RoleID, liXianSaleGoodsItem.RoleID, tradeBlackCopy);
                }
            }

            // number log
            int tradelog_num_minamount = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.tradelog_num_minamount, 5000);
            if (salePrice >= tradelog_num_minamount)
            {
                GameManager.logDBCmdMgr.AddTradeNumberInfo(2, salePrice, otherRID, client.ClientData.RoleID, client.ServerId);
            }

            // freq log

            // 记录花钱的人的记录
            int freqNumber = Global.IncreaseTradeCount(client, RoleParamName.SaleTradeDayID, RoleParamName.SaleTradeCount);
            int tradelog_freq_sale = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.tradelog_freq_sale, 10);
            if (freqNumber >= tradelog_freq_sale)
            {
                GameManager.logDBCmdMgr.AddTradeFreqInfo(2, freqNumber, client.ClientData.RoleID);
            }

            // 暂不记录
            /*// 记录获得钱的人的记录
            // 不在线就在线改他的计数
            if (null == otherClient)
            {
            }
            else
            {
                freqNumber = Global.IncreaseTradeCount(otherClient, RoleParamName.SaleTradeDayID, RoleParamName.SaleTradeCount);
                tradelog_freq_sale = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.tradelog_freq_sale, 10);
                if (freqNumber >= tradelog_freq_sale)
                {
                    GameManager.logDBCmdMgr.AddTradeFreqInfo(2, freqNumber, otherClient.ClientData.RoleID);
                }
            }*/

            return true;
        }

        /// <summary>
        /// 购买失败时,将玩家从交易市场购买时的花费还回,将卖家获得的钱扣除
        /// </summary>
        /// <param name="client"></param>
        /// <param name="SalingGoodsData"></param>
        private void GiveBackSaleGoodsMoney(GameClient client, GameClient saller, GoodsData SalingGoodsData, int saleMoney, int saleYuanBao, int site)
        {
            SalingGoodsData.SaleMoney1 = saleMoney;
            SalingGoodsData.SaleYuanBao = saleYuanBao;
            SalingGoodsData.Site = site;
            int tax = 0;
            int backSaleMoney1 = CalcRealMoneyAfterTax(saleMoney, MoneyTypes.YinLiang, out tax);
            int backSaleYuanBao = CalcRealMoneyAfterTax(saleYuanBao, MoneyTypes.YuanBao, out tax);
            if (SalingGoodsData.SaleMoney1 > 0)
            {
                if (!GameManager.ClientMgr.AddUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, client, SalingGoodsData.SaleMoney1, "新交易市场购买失败退回"))
                {
                    LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场购买失败退回金币失败:", Global.FormatRoleName4(client), SalingGoodsData.SaleMoney1));
                }
                if (!GameManager.ClientMgr.AddUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, saller, -backSaleMoney1, "新交易市场购买失败退回"))
                {
                    LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场购买失败退回金币失败:", Global.FormatRoleName4(client), -backSaleMoney1));
                }
            }
            if (SalingGoodsData.SaleYuanBao > 0)
            {
                if (!GameManager.ClientMgr.AddUserMoney(tcpMgr.MySocketListener, tcpClientPool, pool, client, SalingGoodsData.SaleYuanBao, "新交易市场购买失败退回"))
                {
                    LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场购买失败退回钻石失败:", Global.FormatRoleName4(client), SalingGoodsData.SaleYuanBao));
                }
                if (!GameManager.ClientMgr.AddUserMoney(tcpMgr.MySocketListener, tcpClientPool, pool, saller, -backSaleYuanBao, "新交易市场购买失败退回"))
                {
                    LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场购买失败退回钻石失败:", Global.FormatRoleName4(client), -backSaleYuanBao));
                }
            }

        }

        /// <summary>
        /// 购买失败时,将玩家从交易市场购买时的花费还回,将离线卖家获得的钱扣除
        /// </summary>
        /// <param name="client"></param>
        /// <param name="SalingGoodsData"></param>
        private void GiveBackSaleGoodsMoneyOffline(GameClient client, string userID, int sallerRoleID, string sallerName, GoodsData SalingGoodsData, int saleMoney, int saleYuanBao, int site)
        {
            SalingGoodsData.SaleMoney1 = saleMoney;
            SalingGoodsData.SaleYuanBao = saleYuanBao;
            SalingGoodsData.Site = site;
            int tax = 0;
            int backSaleMoney1 = CalcRealMoneyAfterTax(saleMoney, MoneyTypes.YinLiang, out tax);
            int backSaleYuanBao = CalcRealMoneyAfterTax(saleYuanBao, MoneyTypes.YuanBao, out tax);
            if (SalingGoodsData.SaleMoney1 > 0)
            {
                if (!GameManager.ClientMgr.AddUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, client, SalingGoodsData.SaleMoney1, "新交易市场购买失败退回"))
                {
                    LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场购买失败退回金币失败:", Global.FormatRoleName4(client), SalingGoodsData.SaleMoney1));
                }
                if (!GameManager.ClientMgr.AddOfflineUserYinLiang(tcpMgr.MySocketListener, tcpClientPool, pool, userID, sallerRoleID, sallerName, -backSaleMoney1, "新交易市场购买失败退回", client.ClientData.ZoneID))
                {
                    LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场购买失败退回金币失败:", Global.FormatRoleName4(client), -backSaleMoney1));
                }
            }
            if (SalingGoodsData.SaleYuanBao > 0)
            {
                if (!GameManager.ClientMgr.AddUserMoney(tcpMgr.MySocketListener, tcpClientPool, pool, client, SalingGoodsData.SaleYuanBao, "新交易市场购买失败退回"))
                {
                    LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场购买失败退回钻石失败:", Global.FormatRoleName4(client), SalingGoodsData.SaleYuanBao));
                }
                if (!GameManager.ClientMgr.AddUserMoneyOffLine(tcpMgr.MySocketListener, tcpClientPool, pool, sallerRoleID, -backSaleYuanBao, "新交易市场购买失败退回", client.ClientData.ZoneID, client.strUserID))
                {
                    LogManager.WriteLog(LogTypes.SQL, string.Format("新交易市场购买失败退回钻石失败:", Global.FormatRoleName4(client), -backSaleYuanBao));
                }
            }

        }
    }
}
