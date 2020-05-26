using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System.Windows;
//using System.Windows.Documents;
using GameServer.Server;

namespace GameServer.Logic
{
    /// <summary>
    /// 挖宝管理
    /// </summary>
    public class WaBaoManager
    {
        /// <summary>
        /// 处理挖宝的操作
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static TCPOutPacket ProcessRandomWaBao(GameClient client, TCPOutPacketPool pool, int cmd)
        {
            GoodsData goodsData = new GoodsData()
            {
                Id = -1,
            };

            //如果有未领取的挖宝物品，则不允许重新开始
            if (null != client.ClientData.WaBaoGoodsData)
            {
                goodsData.Id = -1000;
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }
            
            //获取藏宝图的物品ID
            int waBaoGoodsID = (int)GameManager.systemParamsList.GetParamValueIntByName("WaBaoGoodsID");

            //判断用户背包中是否有藏宝图
            if (Global.GetTotalGoodsCountByID(client, waBaoGoodsID) <= 0)
            {
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }

            bool usedBinding = false;
            bool usedTimeLimited = false;

            //先扣除一个藏宝图
            //从用户物品中扣除消耗的数量
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, pool, client, waBaoGoodsID, 1, false, out usedBinding, out usedTimeLimited))
            {
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }

            //算一个万以内的随机数
            int randomNum = Global.GetRandomNumber(1, 10001);
            Dictionary<int, SystemXmlItem> systemXmlItemDict = GameManager.systemWaBaoMgr.SystemXmlItemDict;

            List<int> idsList = new List<int>();
            foreach (var systemWaBaoItem in systemXmlItemDict.Values)
            {
                if (randomNum >= systemWaBaoItem.GetIntValue("StartValues") && randomNum <= systemWaBaoItem.GetIntValue("EndValues"))
                {
                    idsList.Add(systemWaBaoItem.GetIntValue("ID"));
                }
            }

            //没有挖到物品
            if (idsList.Count <= 0)
            {
                goodsData.Id = -20;
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }

            int index = Global.GetRandomNumber(0, idsList.Count);
            int randomID = idsList[index];

            SystemXmlItem waBaoItem = null;
            if (!GameManager.systemWaBaoMgr.SystemXmlItemDict.TryGetValue(randomID, out waBaoItem))
            {
                goodsData.Id = -30;
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }

            goodsData.Id = randomID;
            goodsData.GoodsID = waBaoItem.GetIntValue("GoodsID");
            goodsData.Using = 0;
            goodsData.Forge_level = waBaoItem.GetIntValue("Level");
            goodsData.Starttime = "1900-01-01 12:00:00";
            goodsData.Endtime = Global.ConstGoodsEndTime;
            goodsData.Site = 0;
            goodsData.Quality = waBaoItem.GetIntValue("Quality");
            goodsData.Props = "";
            goodsData.GCount = 1;
            goodsData.Binding = usedBinding ? 1 : 0;
            goodsData.Jewellist = "";
            goodsData.BagIndex = 0;
            goodsData.AddPropIndex = 0;
            goodsData.BornIndex = 0;
            goodsData.Lucky = 0;
            goodsData.Strong = 0;
            goodsData.ExcellenceInfo = 0;
            goodsData.AppendPropLev = 0;
            goodsData.ChangeLifeLevForEquip = 0;

            //记录挖宝挖到的物品数据
            client.ClientData.WaBaoGoodsData = goodsData;

            //挖宝成功的提示
            Global.BroadcastWaBaoGoodsHint(client, goodsData);

            return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
        }

        /// <summary>
        /// 处理领取挖宝的物品的操作
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static TCPOutPacket ProcessGetWaBaoGoodsData(GameClient client, TCPOutPacketPool pool, int cmd)
        {
            string strcmd = null;

            //首先判断是否有挖宝得到的数据
            if (null == client.ClientData.WaBaoGoodsData)
            {
                strcmd = string.Format("{0}:{1}", -1, client.ClientData.RoleID);
                return TCPOutPacket.MakeTCPOutPacket(pool, strcmd, cmd);
            }

            //判断背包是否已经满了?
            if (!Global.CanAddGoods(client, client.ClientData.WaBaoGoodsData.GoodsID,
                client.ClientData.WaBaoGoodsData.GCount,
                client.ClientData.WaBaoGoodsData.Binding,
                client.ClientData.WaBaoGoodsData.Endtime,
                true))
            {
                strcmd = string.Format("{0}:{1}", -10, client.ClientData.RoleID);
                return TCPOutPacket.MakeTCPOutPacket(pool, strcmd, cmd);
            }

            //想DBServer请求加入某个新的物品到背包中
            int dbRet = Global.AddGoodsDBCommand(pool, client, 
                client.ClientData.WaBaoGoodsData.GoodsID,
                client.ClientData.WaBaoGoodsData.GCount, 
                client.ClientData.WaBaoGoodsData.Quality, 
                client.ClientData.WaBaoGoodsData.Props,
                client.ClientData.WaBaoGoodsData.Forge_level,
                client.ClientData.WaBaoGoodsData.Binding,
                client.ClientData.WaBaoGoodsData.Site,
                client.ClientData.WaBaoGoodsData.Jewellist, true, 1, /**/"挖宝获取道具", Global.ConstGoodsEndTime, client.ClientData.WaBaoGoodsData.AddPropIndex, client.ClientData.WaBaoGoodsData.BornIndex, client.ClientData.WaBaoGoodsData.Lucky, client.ClientData.WaBaoGoodsData.Strong);

            if (dbRet < 0)
            {
                strcmd = string.Format("{0}:{1}", -10, client.ClientData.RoleID);
                return TCPOutPacket.MakeTCPOutPacket(pool, strcmd, cmd);
            }

            //清空挖宝得到的物品数据
            client.ClientData.WaBaoGoodsData = null;

            strcmd = string.Format("{0}:{1}", 0, client.ClientData.RoleID);
            return TCPOutPacket.MakeTCPOutPacket(pool, strcmd, cmd);
        }

        /// <summary>
        /// 处理用钥匙类物品打开箱子类物品挖宝的操作
        /// </summary>
        /// <param name="client"></param>
        /// <param name="pool"></param>
        /// <param name="cmd"></param>
        /// <param name="idXiangZi"></param>
        /// <param name="idYaoShi"></param>
        /// <returns></returns>
        public static TCPOutPacket ProcessWaBaoByYaoShi(GameClient client, TCPOutPacketPool pool, int cmd, int idXiangZi, int idYaoShi, bool autoBuy)
        {
            GoodsData goodsData = new GoodsData()
            {
                Id = -1,
            };

            //如果为开启挖宝，则返回
            if ("1" != GameManager.GameConfigMgr.GetGameConfigItemStr("keydigtreasure", "1"))
            {
                goodsData.Id = -20;
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }

            //返回可以开启箱子的钥匙ID列表
            Dictionary<int, int> dictYaoShi = Global.GetYaoShiDiaoLuoForXiangZhi(idXiangZi);

            //非法的箱子ID
            if (null == dictYaoShi || dictYaoShi.Count <= 0)
            {
                goodsData.Id = -30;
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }

            //判读钥匙是否能打开箱子
            bool bCanOpen = false;
            foreach (var key in dictYaoShi.Keys)
            {
                if (key == idYaoShi)
                {
                    bCanOpen = true;
                    break;
                }
            }

            //钥匙不能打开箱子
            if (!bCanOpen)
            {
                goodsData.Id = -50;
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }

            //箱子和钥匙是否存在的判断
            bool existXiangZi = true;
            bool existYaoShi = true;

            //是否需要扣除箱子, 钥匙
            bool needSubXiangZi = true;
            bool needSubYaoShi = true;

            //判读背包是否有一个位置
            if (!Global.CanAddGoodsNum(client, 1))
            {
                goodsData.Id = -300;
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }

            //需要自动购买的列表
            Dictionary<int, int> needGoods = new Dictionary<int, int>();

            //判断用户背包中是否有箱子类物品
            if (Global.GetTotalGoodsCountByID(client, idXiangZi) <= 0)
            {
                existXiangZi = false;
                needGoods.Add(idXiangZi, 1);
            }

            //钥匙ID是0，表示直接打开，不用钥匙，钥匙ID不等于0的时候才需要进一步判断
            if (0 != idYaoShi)
            {
                //判断用户背包中是否有钥匙类物品
                if (Global.GetTotalGoodsCountByID(client, idYaoShi) <= 0)
                {
                    existYaoShi = false;
                    needGoods.Add(idYaoShi, 1);
                }
            }

            //扣除之前的钱
            int oldMoney = client.ClientData.UserMoney;

            int subMoney = 0;
            //缺少物品时
            if (needGoods.Count > 0)
            {
                //如果能自动购买
                if (autoBuy)
                {
                    //自动扣除元宝
                    subMoney = Global.SubUserMoneyForGoods(client, needGoods, "精雕细琢挖宝");
                    
                    if (subMoney > 0)
                    {
                        //扣除成功后，本来没有的，就不用扣除
                        if (!existXiangZi)
                        {
                            needSubXiangZi = false;
                        }

                        if (!existYaoShi)
                        {
                            needSubYaoShi = false;
                        }
                    }
                    else
                    {
                        //自动扣除元宝出错,返回值就是错误码
                        goodsData.Id = subMoney;
                        return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
                    }
                }
                else//不能自动购买，则判断是哪出错了，提示客户端
                {
                    //没有箱子类物品
                    if (!existXiangZi)
                    {
                        goodsData.Id = -100;
                        return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
                    }

                    //没有钥匙类物品
                    if (!existYaoShi)
                    {
                        goodsData.Id = -200;
                        return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
                    }
                }
            }

            //钥匙ID是0，表示直接打开，不用钥匙，钥匙ID不等于0的时候才需要进一步判断
            if (0 == idYaoShi)
            {
                needSubYaoShi = false;
            }

            //采用开箱子的方式挖取到物品,挖取成功时顺便扣除物品,这样避免挖宝过程中出错先扣物品
            GoodsData retGoodsData = null;
            int ret = GoodsBaoXiang.ProcessFallByYaoShiWaBao(client, dictYaoShi[idYaoShi], needSubYaoShi ? idYaoShi : -1, needSubXiangZi ? idXiangZi : -1, out retGoodsData, (0 == idYaoShi) ? 1 : 0, subMoney);

            //没有挖到物品
            if (ret <= 0 || null == retGoodsData)
            {
                goodsData.Id = ret;
                return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
            }

            goodsData = retGoodsData;

            //挖宝成功的提示
            Global.BroadcastYaoShiWaBaoGoodsHint(client, goodsData, idYaoShi, idXiangZi);

            //记录挖宝成功日志
            Global.AddDigTreasureWithYaoShiEvent(client, idYaoShi, idXiangZi, needSubYaoShi ? 1 : 0, needSubXiangZi ? 1 : 0, subMoney, oldMoney, client.ClientData.UserMoney, goodsData); 
            return DataHelper.ObjectToTCPOutPacket<GoodsData>(goodsData, pool, cmd);
        }
    }
}
