using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using System.Windows;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 掉落宝箱处理
    /// </summary>
    public class GoodsBaoXiang
    {
        /// <summary>
        /// 处理掉落的宝箱
        /// </summary>
        /// <param name="fallID"></param>
        public static void ProcessFallBaoXiang(GameClient client, int fallID, int maxFallCount, int binding, int actionGoodsID)
        {
            if (fallID <= 0) return;

            List<FallGoodsItem> gallGoodsItemList = GameManager.GoodsPackMgr.GetFallGoodsItemList(fallID);
            if (null == gallGoodsItemList) return;

            List<FallGoodsItem> tempItemList2 = GameManager.GoodsPackMgr.GetFallGoodsItemByPercent(gallGoodsItemList, maxFallCount, (int)FallAlgorithm.BaoXiang);
            if (tempItemList2.Count <= 0) return;

            List<GoodsData> goodsDataList = GameManager.GoodsPackMgr.GetGoodsDataListFromFallGoodsItemList(tempItemList2);
            if (!Global.CanAddGoodsNum(client, goodsDataList.Count))
            {
                return;
            }

            for (int i = 0; i < goodsDataList.Count; i++)
            {
                //想DBServer请求加入某个新的物品到背包中
                //添加物品
                Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsDataList[i].GoodsID, goodsDataList[i].GCount, goodsDataList[i].Quality, goodsDataList[i].Props, goodsDataList[i].Forge_level, binding, 0, "", true, 1, "掉落宝箱获取", goodsDataList[i].Endtime, goodsDataList[i].AddPropIndex, goodsDataList[i].BornIndex, goodsDataList[i].Lucky, goodsDataList[i].Strong,
                    goodsDataList[i].ExcellenceInfo, goodsDataList[i].AppendPropLev, goodsDataList[i].ChangeLifeLevForEquip);

                /// 开宝箱类挖宝成功的提示
                Global.BroadcastFallBaoXiangGoodsHint(client, goodsDataList[i], actionGoodsID);
            }
        }

        // 增加一个新的接口 [11/16/2013 LiaoWei]
        /// <summary>
        /// 根据掉落包ID (随机)取得物品列表
        /// </summary>
        /// <param name="fallID"></param>
        public static List<GoodsData> FetchGoodListBaseFallPacketID(GameClient client, int fallID, int maxFallCount)
        {
            List<GoodsData> goodsDataList = null;

            if (fallID <= 0) 
                return null;

            List<FallGoodsItem> gallGoodsItemList = GameManager.GoodsPackMgr.GetFallGoodsItemList(fallID);
            if (null == gallGoodsItemList) 
                return null;

            List<FallGoodsItem> tempItemList2 = GameManager.GoodsPackMgr.GetFallGoodsItemByPercent(gallGoodsItemList, maxFallCount, (int)FallAlgorithm.BaoXiang);
            if (tempItemList2.Count <= 0) 
                return null;

            goodsDataList = GameManager.GoodsPackMgr.GetGoodsDataListFromFallGoodsItemList(tempItemList2);
            
            return goodsDataList;
        }

        /// <summary>
        /// 打怪掉落物品[宝石之类]随机开启的宝物,用于挖宝功能,返回大于0表示成功，小于0为错误码
        /// </summary>
        /// <param name="fallID"></param>
        public static int ProcessFallByYaoShiWaBao(GameClient client, int fallID, int idYaoShi, int idXiangZi, out GoodsData retGoodsData, int forceBinding, int subMoney)
        {
            retGoodsData = null;

            if (fallID <= 0)
            {
                return -3000;
            }

            //挖宝时每次只能挖出一个
            int maxFallCount = 1;

            List<FallGoodsItem> gallGoodsItemList = GameManager.GoodsPackMgr.GetFallGoodsItemList(fallID);
            if (null == gallGoodsItemList)
            {
                return -3100;
            }

            List<FallGoodsItem> tempItemList2 = GameManager.GoodsPackMgr.GetFallGoodsItemByPercent(gallGoodsItemList, maxFallCount, (int)FallAlgorithm.BaoXiang);
            if (tempItemList2.Count <= 0)
            {
                return -3200;
            }

            //对于挖宝类物品，挖到的物品个数应该是1
            List<GoodsData> goodsDataList = GameManager.GoodsPackMgr.GetGoodsDataListFromFallGoodsItemList(tempItemList2);
            if (!Global.CanAddGoodsNum(client, goodsDataList.Count))
            {
                return -3300;
            }

            if (1 == goodsDataList.Count)
            {
                retGoodsData = goodsDataList[0];

                //给玩家物品之前先扣除箱子内物品
                bool usedBinding = false;
                bool usedTimeLimited = false;
                bool myUseBinding = false;

                //先扣除一个箱子类物品
                //从用户物品中扣除消耗的数量
                if (idXiangZi >= 0 && !GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, idXiangZi, 1, false, out usedBinding, out usedTimeLimited))
                {
                    return -400;
                }

                //myUseBinding = usedBinding; //箱子不决定是否绑定
                //再扣除一个钥匙类物品
                //从用户物品中扣除消耗的数量
                if (idYaoShi >= 0 && !GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, idYaoShi, 1, false, out usedBinding, out usedTimeLimited))
                {
                    return -500;
                }

                //只要有一个物品时绑定的，挖到的也是绑定的
                if (!myUseBinding)
                {
                    myUseBinding = usedBinding;
                }

                if (subMoney > 0)
                {
                    myUseBinding = false;
                }

                retGoodsData.Binding = Math.Max(forceBinding, myUseBinding ? 1 : 0);

                //想DBServer请求加入某个新的物品到背包中
                //添加物品
                int goodsDbID = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, retGoodsData.GoodsID, retGoodsData.GCount, retGoodsData.Quality, retGoodsData.Props, retGoodsData.Forge_level, retGoodsData.Binding, 0, "", true, 1, "精雕细琢挖宝获取", retGoodsData.Endtime, retGoodsData.AddPropIndex, retGoodsData.BornIndex, retGoodsData.Lucky, retGoodsData.Strong);

                retGoodsData.Id = goodsDbID;
            }
            else
            {
                return -3400;
            }

            return 100;
        }

        // 使用宝箱 生成掉落物品 [11/16/2013 LiaoWei]
        /// <summary>
        /// 根据掉落ID 生成物品 并掉落在地上
        /// </summary>
        /// <param name="fallID"></param>
        public static void CreateGoodsBaseFallID(GameClient client, int fallID, int nMaxCount)
        {
            if (fallID <= 0) return;

            List<FallGoodsItem> gallGoodsItemList = GameManager.GoodsPackMgr.GetFallGoodsItemList(fallID);
            if (null == gallGoodsItemList) 
                return;

            List<FallGoodsItem> tempItemList2 = GameManager.GoodsPackMgr.GetFallGoodsItemByPercent(gallGoodsItemList, nMaxCount, (int)FallAlgorithm.BaoXiang);
            if (tempItemList2.Count <= 0) 
                return;

            List<GoodsData> goodsDataList = null;
            goodsDataList = GameManager.GoodsPackMgr.GetGoodsDataListFromFallGoodsItemList(tempItemList2);

            if (goodsDataList == null)
                return;

            List<GoodsPackItem> goodsPackItemList = new List<GoodsPackItem>();
            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();

            for (int i = 0; i < goodsDataList.Count; i++)
            {
                List<GoodsData> oneGoodsDataList = new List<GoodsData>();
                oneGoodsDataList.Add(goodsDataList[i]);

                GoodsPackItem goodsPackItem = new GoodsPackItem()
                {
                    AutoID = GameManager.GoodsPackMgr.GetNextAutoID(),
                    GoodsPackID = fallID,
                    OwnerRoleID = client.ClientData.RoleID,
                    OwnerRoleName = client.ClientData.RoleName,
                    GoodsPackType = 0,
                    ProduceTicks = TimeUtil.NOW(),
                    LockedRoleID = -1,
                    GoodsDataList = oneGoodsDataList,
                    TeamRoleIDs = null,
                    MapCode = client.ClientData.MapCode,
                    CopyMapID = client.ClientData.CopyMapID,
                    KilledMonsterName = null,
                    BelongTo = 1,
                    FallLevel = 0,
                    TeamID = -1,
                };

                goodsPackItem.FallPoint = GameManager.GoodsPackMgr.GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, client.ClientData.MapCode, gridDict,new Point((int)(client.CurrentGrid.X), 
                                                                                            (int)(client.CurrentGrid.Y)), client.ClientData.CopyMapID, client);

                goodsPackItemList.Add(goodsPackItem);

                lock (GameManager.GoodsPackMgr.GoodsPackDict)
                {
                    GameManager.GoodsPackMgr.GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
                }

                for (int j = 0; j < goodsPackItemList.Count; j++)
                {
                    GameManager.GoodsPackMgr.ProcessGoodsPackItem(client, client, goodsPackItemList[i], 1);
                }
            }
            
        }

        // 增加一个接口 [1/12/2014 LiaoWei]
        /// <summary>
        /// 处理活动奖励
        /// </summary>
        /// <param name="fallID"></param>
        public static int ProcessActivityAward(GameClient client, int fallID, int maxFallCount, int binding, string sMsg, List<GoodsData> goodsDataList)
        {
            if (fallID <= 0)
            {
                return -10;
            }

            List<FallGoodsItem> gallGoodsItemList = GameManager.GoodsPackMgr.GetFallGoodsItemList(fallID);
            if (null == gallGoodsItemList)
            {
                return -12;
            }

            List<FallGoodsItem> tempItemList2 = GameManager.GoodsPackMgr.GetFallGoodsItemByPercent(gallGoodsItemList, maxFallCount, (int)FallAlgorithm.BaoXiang);
            if (tempItemList2.Count <= 0)
            {
                return -13;
            }

            List<GoodsData> goodsDataLists = null;
            goodsDataLists = GameManager.GoodsPackMgr.GetGoodsDataListFromFallGoodsItemList(tempItemList2);
            if (!Global.CanAddGoodsNum(client, goodsDataLists.Count))
                return -14;

            for (int i = 0; i < goodsDataLists.Count; i++)
            {
                //添加物品
                /*Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsDataLists[i].GoodsID,
                    goodsDataLists[i].GCount, goodsDataLists[i].Quality, goodsDataLists[i].Props, goodsDataLists[i].Forge_level,
                    binding, 0, "", true, 1, sMsg, goodsDataLists[i].Endtime, goodsDataLists[i].AddPropIndex, goodsDataLists[i].BornIndex, goodsDataLists[i].Lucky, goodsDataLists[i].Strong);*/

                goodsDataList.Add(goodsDataLists[i]);
            }

            return 1;
        }
    }
}
