using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Server.Tools.Pattern;
using Server.Data;
using GameServer.Logic.LiXianBaiTan;
using GameServer.Core.Executor;
using GameServer.Server;

using ProtoBuf;
using Server.Tools;
using Tmsk.Contract;

namespace GameServer.Logic.CheatGuard
{
    /// <summary>
    /// 交易黑名单每日对象
    /// </summary>
    [ProtoContract]
    class TradeBlackHourItem
    {
        [ProtoMember(1)]
        public int RoleId;                                          // 角色id
        [ProtoMember(2)]
        public string Day;                                          // 日期 "yyyy-MM-dd"
        [ProtoMember(3)]
        public int Hour;                                            // 小时[0 - 23]
        [ProtoMember(4)]
        public int MarketTimes;                                     // 拍卖行次数
        [ProtoMember(5)]
        public long MarketInPrice;                                  // 拍卖行买入市值
        [ProtoMember(6)]
        public long MarketOutPrice;                                 // 拍卖行出售市值
        [ProtoMember(7)]
        public int TradeTimes;                                      // 交易次数
        [ProtoMember(8)]
        public long TradeInPrice;                                   // 交易买入市值
        [ProtoMember(9)]
        public long TradeOutPrice;                                  // 交易出售市值
        [ProtoMember(10)]
        public HashSet<int> TradeRoles;                             // 交易对象列表
        [ProtoMember(11)]
        public int TradeDistinctRoleCount;                          // 交易的不同玩家的数量(包括交易行)

        /// <summary>
        /// 拷贝一份供通知存db使用
        /// </summary>
        /// <returns></returns>
        public TradeBlackHourItem SimpleClone()
        {
            TradeBlackHourItem item = new TradeBlackHourItem();
            item.RoleId = this.RoleId;
            item.Day = this.Day;
            item.Hour = this.Hour;
            item.MarketTimes = this.MarketTimes;
            item.MarketInPrice = this.MarketInPrice;
            item.MarketOutPrice = this.MarketOutPrice;
            item.TradeTimes = this.TradeTimes;
            item.TradeInPrice = this.TradeInPrice;
            item.TradeOutPrice = this.TradeOutPrice;
            item.TradeDistinctRoleCount = this.TradeRoles != null ? this.TradeRoles.Count() : 0;

            return item;
        }
    }

    class TradeBlackObject
    {
        public int RoleId;
        public TradeBlackHourItem[] HourItems;
        public DateTime LastFlushTime;
        public long BanTradeToTicks;

        // 角色信息
        public int ChangeLife;
        public int Level;
        public int VipLevel;
        public int ZoneId;
        public string RoleName;
    }

    class TradeConfigItem
    {
        public int Id;
        public int UnionMinLevel;
        public int UnionMaxLevel;
        public int MinVip;
        public int MaxVip;
        public int MaxPrice;
        public int MaxTimes;
    }

    /// <summary>
    /// 交易黑名单管理器
    /// </summary>
    public class TradeBlackManager : SingletonTemplate<TradeBlackManager>
    {
        private TradeBlackManager() { }
        private Dictionary<int, TradeBlackObject> TradeBlackObjs = new Dictionary<int, TradeBlackObject>();
        private DateTime lastCheckUnBanTime = TimeUtil.NowDateTime();
        private DateTime lastFreeUnusedTime = TimeUtil.NowDateTime();

        private const string GoodsPriceCfgFile = @"Config\Blacklist.xml";
        private const string TradeBlackCfgFile = @"Config\TradeConfig.xml";

        private Dictionary<int, int> GoodsPriceDict = null;
        private List<TradeConfigItem> TradeCfgItems = null;

        //	当配置值不等于-1时，对进行处理的账号执行禁止使用点对点交易和交易市场功能，时间为配置值
        //	当配置值等于-1时，不对进行处理的账号执行禁止使用交易
        private int BanTradeSec = -1;
        // 	当配置值为1时，对进行处理的账号信息存储到日志中
        //	当配置值为0时，不记录日志
        private int BanTradeLog = 0;
        //	当配置值不等于-1时，对进行处理的账号执行封停处理，时间为配置值
        //	封号信息中增加交易封号的相关记录标示
        //	当配置值等于-1时，不对进行处理的账号执行封停处理
        private int BanTradeLogin = -1;

        public bool LoadConfig()
        {
            bool bResult = true;
            try
            {
            }
            catch (Exception ex)
            {
                bResult = false;
                LogManager.WriteLog(LogTypes.Error, "load " + GoodsPriceCfgFile + " exception!", ex);
            }

            try
            {
                List<TradeConfigItem> tmpTradeCfgItems = new List<TradeConfigItem>();
                XElement xml = XElement.Load(Global.GameResPath(TradeBlackCfgFile));
                foreach (var xmlItem in xml.Elements())
                {
                    TradeConfigItem item = new TradeConfigItem();
                    item.Id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    item.MinVip = (int)Global.GetSafeAttributeLong(xmlItem, "MinVip");
                    item.MaxVip = (int)Global.GetSafeAttributeLong(xmlItem, "MaxVip");
                    item.MaxPrice = (int)Global.GetSafeAttributeLong(xmlItem, "MaxPrice");
                    item.MaxTimes = (int)Global.GetSafeAttributeLong(xmlItem, "MaxNum");
                    int cl, lvl;
                    cl = (int)Global.GetSafeAttributeLong(xmlItem, "MinZhuanSheng");
                    lvl = (int)Global.GetSafeAttributeLong(xmlItem, "MinLevel");
                    item.UnionMinLevel = Global.GetUnionLevel(cl, lvl);
                    cl = (int)Global.GetSafeAttributeLong(xmlItem, "MaxZhuanSheng");
                    lvl = (int)Global.GetSafeAttributeLong(xmlItem, "MaxLevel");
                    item.UnionMaxLevel = Global.GetUnionLevel(cl, lvl);

                    tmpTradeCfgItems.Add(item);
                }

                this.TradeCfgItems = tmpTradeCfgItems;
            }
            catch (Exception ex)
            {
                bResult = false;
                LogManager.WriteLog(LogTypes.Error, "load " + TradeBlackCfgFile + " exception!", ex);
            }

            string szPlatFlag = string.Empty;
            PlatformTypes pt = GameCoreInterface.getinstance().GetPlatformType();
            if (pt == PlatformTypes.Android) szPlatFlag = "Android";
            else if (pt == PlatformTypes.YueYu) szPlatFlag = "YueYu";
            else if (pt == PlatformTypes.APP) szPlatFlag = "APP";
            else if (pt == PlatformTypes.YYB) szPlatFlag = "YYB";

            this.BanTradeSec = (int)GameManager.systemParamsList.GetParamValueIntByName("NoTrade_" + szPlatFlag);
            this.BanTradeLog = (int)GameManager.systemParamsList.GetParamValueIntByName("TradeLog_" + szPlatFlag);
            this.BanTradeLogin = (int)GameManager.systemParamsList.GetParamValueIntByName("TradeKill_" + szPlatFlag);

            return bResult;
        }

        public bool IsBanTrade(int roleId)
        {
            bool bBan = false;
            TradeBlackObject obj = LoadTradeBlackObject(roleId);
            if (obj != null)
            {
                bBan = obj.BanTradeToTicks > 0 && obj.BanTradeToTicks > TimeUtil.NowDateTime().Ticks;
            }

            return bBan;
        }

        /// <summary>
        /// 更新TradeBlackOject的额外信息
        /// 转生、等级、vip
        /// </summary>
        /// <param name="client"></param>
        public void UpdateObjectExtData(GameClient client)
        {
            if (client == null) return;
            TradeBlackObject obj = LoadTradeBlackObject(client.ClientData.RoleID, false);
            if (obj == null) return;

            lock (obj)
            {
                obj.ChangeLife = client.ClientData.ChangeLifeCount;
                obj.Level = client.ClientData.Level;
                obj.VipLevel = client.ClientData.VipLevel;
                obj.ZoneId = client.ClientData.ZoneID;
                obj.RoleName = client.ClientData.RoleName;
            }
        }

        public void SetBanTradeToTicks(int roleid, long toTicks)
        {
            toTicks = Math.Max(0, toTicks);
            //设置角色的属性 update db
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEROLEPROPS,
                string.Format("{0}:{1}:{2}", roleid, (int)RolePropIndexs.BanTrade, toTicks),
                null, GameManager.LocalServerId);

            TradeBlackObject obj = LoadTradeBlackObject(roleid);
            if (obj != null)
            {
                lock (obj)
                {
                    obj.BanTradeToTicks = toTicks;
                }
            }

            long banTradeSec = 0;
            if (toTicks > TimeUtil.NowDateTime().Ticks)
            {
                banTradeSec = (long)(new DateTime(toTicks) - TimeUtil.NowDateTime()).TotalSeconds;
                banTradeSec = Math.Max(0, banTradeSec);
            }

            if (banTradeSec > 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("roleid={0} 被封禁交易，秒数={1}", roleid, banTradeSec));
            }

            GameClient client = GameManager.ClientMgr.FindClient(roleid);
            if (client != null)
            {
                client.ClientData.BanTradeToTicks = toTicks;

                if (banTradeSec > 0)
                {
                    string tip = string.Format(Global.GetLang("您目前已被禁止交易以及使用交易行，剩余时间【{0}】秒"), banTradeSec);
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, tip, GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                }
            }
        }

        /// <summary>
        /// 检查封禁交易
        /// </summary>
        /// <param name="roleId"></param>
        private void CheckBanTrade(int roleId)
        {
            TradeBlackObject obj = LoadTradeBlackObject(roleId);
            if (obj == null) return;

            int kick_out_minutes = -1;
            lock (obj)
            {
                if (obj.BanTradeToTicks <= 0)
                {
                    List<TradeConfigItem> items = this.TradeCfgItems;
                    TradeConfigItem item = items != null ? items.Find(_i => {
                        return _i.MinVip <= obj.VipLevel && _i.MaxVip >= obj.VipLevel
                            && _i.UnionMinLevel <= Global.GetUnionLevel(obj.ChangeLife, obj.Level)
                            && _i.UnionMaxLevel >= Global.GetUnionLevel(obj.ChangeLife, obj.Level);
                    }) : null;

                    if (item != null)
                    {
                        long totalInPrice = 0, totalOutPrice = 0, totalTimes = 0;
                        foreach (var hourItem in obj.HourItems)
                        {
                            if (hourItem == null) continue;

                            totalInPrice += hourItem.MarketInPrice + hourItem.TradeInPrice;
                            totalOutPrice += hourItem.MarketOutPrice + hourItem.TradeOutPrice;
                            totalTimes += hourItem.MarketTimes + hourItem.TradeTimes;
                        }

                        if (totalInPrice >= item.MaxPrice || totalOutPrice >= item.MaxPrice || totalTimes >= item.MaxTimes)
                        {
                            int _banTradeSec = Math.Max(this.BanTradeSec, 0);
                            if (_banTradeSec > 0)
                            {
                                long toTicks = TimeUtil.NowDateTime().AddSeconds(_banTradeSec).Ticks;
                                SetBanTradeToTicks(roleId, toTicks);
                            }

                            if (this.BanTradeLog == 1)
                            {
                                LogManager.WriteLog(LogTypes.Analysis, string.Format("tradeblack player={0} inprice={1} outprice={2} times={3} bansec={4}", roleId, totalInPrice, totalOutPrice, totalTimes, _banTradeSec));
                            }

                            kick_out_minutes = Math.Max(this.BanTradeLogin, 0) / 60;
                            if (kick_out_minutes > 0)
                            {
                                BanManager.BanRoleName(Global.FormatRoleName3(obj.ZoneId, obj.RoleName), kick_out_minutes, (int)BanManager.BanReason.TradeException);
                            }
                        }
                    }
                }
            }

            if (kick_out_minutes > 0)
            {
                GameClient client = GameManager.ClientMgr.FindClient(roleId);
                if (client != null)
                {
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                        StringUtil.substitute(Global.GetLang("系统检测到您交易异常，您的帐号将被禁止登录{0}分钟！"), kick_out_minutes), GameInfoTypeIndexes.Error, ShowGameInfoTypes.HintAndBox);

                    Global.ForceCloseClient(client, "交易封禁");

                    /*
                    string gmCmdData = string.Format("-kick {0}", obj.RoleName);
                    //转发GM消息到DBServer
                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                        string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", obj.RoleId, "", 0, "", 0, gmCmdData, 0, 0, GameManager.ServerLineID),
                        null, GameManager.LocalServerId);
                     */
                }
            }
        }

        /// <summary>
        /// 检查解封交易
        /// </summary>
        /// <param name="roleId"></param>
        private void CheckUnBanTrade(int roleId)
        {
            TradeBlackObject obj = LoadTradeBlackObject(roleId);
            if (obj == null) return;

            lock (obj)
            {
                if (obj.BanTradeToTicks > 0 && obj.BanTradeToTicks < TimeUtil.NowDateTime().Ticks)
                {
                    // 解封
                    SetBanTradeToTicks(roleId, 0);
                }
            }
        }

        /// <summary>
        /// 定时检测解封交易
        /// </summary>
        public void Update()
        {
            // 60s检测一次解封
            if ((TimeUtil.NowDateTime() - lastCheckUnBanTime).TotalSeconds > 60)
            {
                lastCheckUnBanTime = TimeUtil.NowDateTime();
                List<int> roleIds = null;
                lock (TradeBlackObjs)
                {
                    roleIds = TradeBlackObjs.Keys.ToList();
                }

                if (roleIds != null)
                {
                    roleIds.ForEach(_r => CheckUnBanTrade(_r));
                }
            }

            // 每小时检测1次可以释放的黑名单对象
            if ((TimeUtil.NowDateTime() - lastFreeUnusedTime).TotalHours > 1)
            {
                lastFreeUnusedTime = TimeUtil.NowDateTime();
                List<int> roleIds = null;
                lock (TradeBlackObjs)
                {
                    roleIds = TradeBlackObjs.Keys.ToList();
                }

                // 从内存中卸载未被封禁的，12个小时未被访问的黑名单对象
                List<int> removeIds = roleIds != null ? roleIds.FindAll(_r =>
                {
                    TradeBlackObject obj = LoadTradeBlackObject(_r);
                    return obj != null && obj.BanTradeToTicks <= 0 && (TimeUtil.NowDateTime() - obj.LastFlushTime).TotalHours > 12;
                }) : null;

                if (removeIds != null)
                {
                    lock (TradeBlackObjs)
                    {
                        foreach (var id in removeIds)
                        {
                            TradeBlackObjs.Remove(id);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 当完成交易
        /// </summary>
        /// <param name="ed"></param>
        public void OnExchange(int roleid1, int roleid2, List<GoodsData> gdList1, List<GoodsData> gdList2, int zuanshi1, int zuanshi2)
        {
            long price1 = zuanshi1 > 0 ? zuanshi1 : 0; // roleid1 的支出
            long price2 = zuanshi2 > 0 ? zuanshi2 : 0; // roleid2 的支出

            Func<List<GoodsData>, Dictionary<int, int>, long> _GetGoodsPrice = (gdList, priceDict) =>
            {
                long totalPrice = 0;
                if (gdList != null && priceDict != null)
                {
                    gdList.ForEach(_g => totalPrice += priceDict.ContainsKey(_g.GoodsID) ? priceDict[_g.GoodsID] * _g.GCount : 0);
                }
                return totalPrice;
            };

            price1 += _GetGoodsPrice(gdList1, this.GoodsPriceDict);
            price2 += _GetGoodsPrice(gdList2, this.GoodsPriceDict);

            DateTime now = TimeUtil.NowDateTime();
            TradeBlackObject obj1 = LoadTradeBlackObject(roleid1);
            if (obj1 != null)
            {
                lock (obj1)
                {
                    TradeBlackHourItem item = GetBlackHourItem(obj1, now);
                    item.TradeTimes++;
                    item.TradeOutPrice += price1;
                    item.TradeInPrice += price2;
                    if (!item.TradeRoles.Contains(roleid2))
                    {
                        item.TradeRoles.Add(roleid2);
                        item.TradeDistinctRoleCount++;
                    }

                    TradeBlackHourItem itemCopy = item.SimpleClone();
                    SaveTradeBlackObject(itemCopy);
                }          
            }

            TradeBlackObject obj2 = LoadTradeBlackObject(roleid2);
            if (obj2 != null)
            {
                lock (obj2)
                {
                    TradeBlackHourItem item = GetBlackHourItem(obj2, now);
                    item.TradeTimes++;
                    item.TradeInPrice += price1;
                    item.TradeOutPrice += price2;
                    if (!item.TradeRoles.Contains(roleid1))
                    {
                        item.TradeRoles.Add(roleid1);
                        item.TradeDistinctRoleCount++;
                    }
                    TradeBlackHourItem itemCopy = item.SimpleClone();
                    SaveTradeBlackObject(itemCopy);
                }
            }

            CheckBanTrade(roleid1);
            CheckBanTrade(roleid2);
        }

        /// <summary>
        /// 当商城在线购买
        /// 出售者在线
        /// </summary>
        /// <param name="whoBuy"></param>
        /// <param name="item"></param>
        public void OnMarketBuy(int whoBuy, int whoSale, GoodsData saleGoods)
        {
            if (saleGoods == null) return;

            int pay = Math.Max(saleGoods.SaleYuanBao, 0);
            Dictionary<int, int> tmpPriceDict = GoodsPriceDict;
            int price = (tmpPriceDict != null && tmpPriceDict.ContainsKey(saleGoods.GoodsID))
                ? (tmpPriceDict[saleGoods.GoodsID] * saleGoods.GCount) : 0;

            DateTime now = TimeUtil.NowDateTime();
            TradeBlackObject buyer = LoadTradeBlackObject(whoBuy);
            if (buyer != null)
            {
                lock (buyer)
                {
                    TradeBlackHourItem item = GetBlackHourItem(buyer, now);
                    item.MarketTimes++;
                    item.MarketInPrice += price;
                    item.MarketOutPrice += pay;
                    if (!item.TradeRoles.Contains(whoSale))
                    {
                        item.TradeRoles.Add(whoSale);
                        item.TradeDistinctRoleCount++;
                    }
                    TradeBlackHourItem itemCopy = item.SimpleClone();
                    SaveTradeBlackObject(itemCopy);
                }
            }

            TradeBlackObject saler = LoadTradeBlackObject(whoSale);
            if (saler != null)
            {
                lock (saler)
                {
                    TradeBlackHourItem item = GetBlackHourItem(saler, now);
                    item.MarketTimes++;
                    item.MarketOutPrice += price;
                    item.MarketInPrice += pay;
                    if (!item.TradeRoles.Contains(whoBuy))
                    {
                        item.TradeRoles.Add(whoBuy);
                        item.TradeDistinctRoleCount++;
                    }
                    TradeBlackHourItem itemCopy = item.SimpleClone();
                    SaveTradeBlackObject(itemCopy);
                }
            }

            CheckBanTrade(whoBuy);
            CheckBanTrade(whoSale);
        }

        private TradeBlackObject LoadTradeBlackObject(int roleid, bool loadDbIfNotExist = true)
        {
            DateTime now = TimeUtil.NowDateTime();
            TradeBlackObject obj = null;
            int offsetDay = Global.GetOffsetDay(TimeUtil.NowDateTime());
            lock (TradeBlackObjs)
            {
                if (TradeBlackObjs.TryGetValue(roleid, out obj)) 
                {
                    obj.LastFlushTime = now;
                }
            }

            if (obj == null && loadDbIfNotExist)
            {
                string reqCmd = string.Format("{0}:{1}:{2}", roleid, now.ToString("yyyy-MM-dd"), now.Hour);
                List<TradeBlackHourItem> items = Global.sendToDB<List<TradeBlackHourItem>, string>(
                     (int)TCPGameServerCmds.CMD_DB_LOAD_TRADE_BLACK_HOUR_ITEM, reqCmd, GameManager.LocalServerId);

                obj = new TradeBlackObject();
                obj.RoleId = roleid;
                obj.LastFlushTime = now;
                obj.HourItems = new TradeBlackHourItem[24];

                GameClient client = GameManager.ClientMgr.FindClient(roleid);
                if (client != null)
                {
                    obj.VipLevel = client.ClientData.VipLevel;
                    obj.ChangeLife = client.ClientData.ChangeLifeCount;
                    obj.Level = client.ClientData.Level;
                    obj.BanTradeToTicks = client.ClientData.BanTradeToTicks;
                    obj.ZoneId = client.ClientData.ZoneID;
                    obj.RoleName = client.ClientData.RoleName;
                }
                else
                {
                    SafeClientData clientData = Global.GetSafeClientDataFromLocalOrDB(roleid);
                    if (clientData != null)
                    {
                        obj.VipLevel = Global.CalcVipLevelByZuanShi(Global.GetUserInputAllYuanBao(clientData.RoleID, clientData.RoleName, GameManager.LocalServerId));
                        obj.ChangeLife = clientData.ChangeLifeCount;
                        obj.Level = clientData.Level;
                        obj.BanTradeToTicks = clientData.BanTradeToTicks;
                        obj.ZoneId = clientData.ZoneID;
                        obj.RoleName = clientData.RoleName;
                    }
                }

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        int idx = item.Hour % 24; // 哈哈，hour，必然在0 --- 24 范围内吧
                        obj.HourItems[idx] = item;
                        item.TradeRoles = item.TradeRoles ?? new HashSet<int>();
                    }
                }

                // 防止多个线程同时都从数据库加载同一个人的信息
                lock (TradeBlackObjs)
                {
                    if (!TradeBlackObjs.ContainsKey(roleid))
                        TradeBlackObjs[roleid] = obj;
                    else
                        obj = TradeBlackObjs[roleid];
                }
            }

            return obj;
        }

        private TradeBlackHourItem GetBlackHourItem(TradeBlackObject obj, DateTime date)
        {
            TradeBlackHourItem item = obj.HourItems[date.Hour];
            if (item == null || item.Day != date.ToString("yyyy-MM-dd"))
            {
                item = new TradeBlackHourItem();
                item.RoleId = obj.RoleId;
                item.Day = date.ToString("yyyy-MM-dd");
                item.Hour = date.Hour;
                item.TradeRoles = new HashSet<int>();
                obj.HourItems[date.Hour] = item;
            }

            return item;
        }

        private void SaveTradeBlackObject(TradeBlackHourItem obj)
        {
            if (obj == null) return;

            if (!Global.sendToDB<bool, TradeBlackHourItem>((int)TCPGameServerCmds.CMD_DB_SAVE_TRADE_BLACK_HOUR_ITEM, obj, GameManager.LocalServerId))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("TradeBlackManager.SaveTradeBlackObject failed!, roleid={0}", obj.RoleId));
            }
        }
    }
}
