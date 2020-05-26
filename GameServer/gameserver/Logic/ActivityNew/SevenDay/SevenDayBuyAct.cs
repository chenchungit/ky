using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Data;
using Server.Tools;
using System.Xml.Linq;

namespace GameServer.Logic.ActivityNew.SevenDay
{
    /// <summary>
    /// 七日抢购活动
    /// </summary>
    public class SevenDayBuyAct
    {
        /// <summary>
        /// 抢购物品信息
        /// </summary>
        class _BuyGoodsData
        {
            public int Id;                          // SevenDayQiangGou.xml 配置的ID
            public int Day;                       // 第几天的抢购物品
            public int OriginPrice;             // 原始价格
            public int CurrPrice;               // 当前价格
            public int MaxBuyCount;         // 最多购买多少个
            public GoodsData Goods;
        }

        private Dictionary<int, _BuyGoodsData> _BuyGoodsDict = null;
        private object ConfigMutex = new object();
        
        public SevenDayBuyAct()
        {
        }

        /// <summary>
        /// 加载配置文件，多线程安全
        /// </summary>
        public void LoadConfig()
        {
            Dictionary<int, _BuyGoodsData> tmpDict = new Dictionary<int, _BuyGoodsData>();

            try
            {
                XElement xml = XElement.Load(Global.GameResPath(SevenDayConsts.BuyConfig));
                foreach (var xmlItem in xml.Elements())
                {
                    _BuyGoodsData data = new _BuyGoodsData();
                    data.Id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    data.Day = (int)Global.GetSafeAttributeLong(xmlItem, "Day");
                    data.OriginPrice = (int)Global.GetSafeAttributeLong(xmlItem, "OrigPrice");
                    data.CurrPrice = (int)Global.GetSafeAttributeLong(xmlItem, "Price");
                    data.MaxBuyCount = (int)Global.GetSafeAttributeLong(xmlItem, "Purchase");
                    data.Goods = Global.ParseGoodsFromStr_7(Global.GetSafeAttributeStr(xmlItem, "GoodsID").Split(','), 0);

                    tmpDict[data.Id] = data;
                }

                lock (ConfigMutex)
                {
                    _BuyGoodsDict = tmpDict;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("七日登录活动加载配置失败{0}", SevenDayConsts.BuyConfig), ex);
            }
        }

        /// <summary>
        /// 玩家购买物品
        /// </summary>
        public ESevenDayActErrorCode HandleClientBuy(GameClient client, int id, int cnt)
        {
            int currDay;
            if (!SevenDayActivityMgr.Instance().IsInActivityTime(client, out currDay))
            {
                return ESevenDayActErrorCode.NotInActivityTime;
            }

            _BuyGoodsData goodsConfig = null;
            
            lock (ConfigMutex)
            {
                if (_BuyGoodsDict == null || !_BuyGoodsDict.TryGetValue(id, out goodsConfig))
                    return ESevenDayActErrorCode.ServerConfigError;
            }
            if (goodsConfig == null || goodsConfig.Goods == null)
                return ESevenDayActErrorCode.ServerConfigError;

            if (goodsConfig.Day > currDay)
                return ESevenDayActErrorCode.NotReachCondition;

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Buy);
            lock (itemDict)
            {
                SevenDayItemData itemData = null;
                if (!itemDict.TryGetValue(id, out itemData))
                {
                    itemData = new SevenDayItemData();
                    itemDict[id] = itemData;
                }

                if (cnt <= 0 || itemData.Params1 + cnt > goodsConfig.MaxBuyCount)
                    return ESevenDayActErrorCode.NoEnoughGoodsCanBuy;

                if (client.ClientData.UserMoney < cnt * goodsConfig.CurrPrice)
                    return ESevenDayActErrorCode.ZuanShiNotEnough;


                if (!Global.CanAddGoods(client, goodsConfig.Goods.GoodsID, goodsConfig.Goods.GCount * cnt, goodsConfig.Goods.Binding))
                {
                    return ESevenDayActErrorCode.NoBagSpace;
                }

                // 检查背包
                itemData.Params1 += cnt;
                if (!SevenDayActivityMgr.Instance().UpdateDb(client.ClientData.RoleID, ESevenDayActType.Buy, id, itemData, client.ServerId))
                {
                    itemData.Params1 -= cnt;
                    return ESevenDayActErrorCode.DBFailed;
                }

                if (!GameManager.ClientMgr.SubUserMoney(client, cnt * goodsConfig.CurrPrice, "七日抢购"))
                {
                    // 之前已经检查过了
                    LogManager.WriteLog(LogTypes.Error, string.Format("玩家七日抢购物品，检查钻石足够，但是扣除失败,roleid={0}, id={1}", client.ClientData.RoleID, id));
                }

                GoodsData goodsData = goodsConfig.Goods;
                Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount * cnt, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, string.Format("七日抢购"), false,
                                                                        goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, true);
                // 发物品

                return ESevenDayActErrorCode.Success;
            }
        }

        /// <summary>
        /// 是否有任意物品可以购买
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool HasAnyCanBuy(GameClient client)
        {
            int currDay;
            if (!SevenDayActivityMgr.Instance().IsInActivityTime(client, out currDay))
            {
                return false;
            }

            Dictionary<int, _BuyGoodsData> tmpConfigDict = null;
            lock (ConfigMutex)
            {
                if ((tmpConfigDict = _BuyGoodsDict) == null
                    || tmpConfigDict.Count <= 0)
                    return false;
            }

            Dictionary<int, SevenDayItemData> itemDict = SevenDayActivityMgr.Instance().GetActivityData(client, ESevenDayActType.Buy);

            lock (itemDict)
            {
                foreach (var kvp in tmpConfigDict)
                {
                    _BuyGoodsData goodsConfig = kvp.Value;
                    if (goodsConfig == null || goodsConfig.Day > currDay)
                        continue;

                    int hasBuy = 0;
                    SevenDayItemData itemData = null;
                    if (itemDict.TryGetValue(kvp.Key, out itemData))
                    {
                        hasBuy = itemData.Params1;
                    }

                    if (goodsConfig.MaxBuyCount > hasBuy)
                        return true;
                }
            }

            return false;
        }
    }
}
