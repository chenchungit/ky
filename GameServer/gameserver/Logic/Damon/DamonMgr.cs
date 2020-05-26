using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Damon
{
    class DamonMgr
    {
        #region 精灵装备栏物品管理

        /// <summary>
        /// 根据物品DbID获取精灵装备栏物品的信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GoodsData GetDamonGoodsDataByDbID(GameClient client, int id)
        {
            if (null == client.ClientData.DamonGoodsDataList)
            {
                return null;
            }

            lock (client.ClientData.DamonGoodsDataList)
            {
                for (int i = 0; i < client.ClientData.DamonGoodsDataList.Count; i++)
                {
                    if (client.ClientData.DamonGoodsDataList[i].Id == id)
                    {
                        return client.ClientData.DamonGoodsDataList[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 添加精灵装备栏物品
        /// </summary>
        /// <param name="goodsData"></param>
        public static void AddDamonGoodsData(GameClient client, GoodsData goodsData, bool refreshProps = true)
        {
            if (goodsData.Site != 0) return;

            if (null == client.ClientData.DamonGoodsDataList)
            {
                client.ClientData.DamonGoodsDataList = new List<GoodsData>();
            }

            lock (client.ClientData.DamonGoodsDataList)
            {
                client.ClientData.DamonGoodsDataList.Add(goodsData);
            }

            JingLingQiYuanManager.getInstance().RefreshProps(client);
        }

        /// <summary>
        /// 兼容老版本的宠物
        /// </summary>
        /// <param name="goodsData"></param>
        public static void AddOldDamonGoodsData(GameClient client)
        {
            if (null == client.ClientData.GoodsDataList)
            {
                return;
            }

            List<GoodsData> listDamon = new List<GoodsData>();
            // 把在装备栏也在背包中的精灵添加到精灵栏
            for (int i = 0; i < client.ClientData.GoodsDataList.Count; i++)
            {
                int nCategories = Global.GetGoodsCatetoriy(client.ClientData.GoodsDataList[i].GoodsID);
                if (nCategories >= (int)ItemCategories.ShouHuChong && nCategories <= (int)ItemCategories.ChongWu)
                {
                    if (client.ClientData.GoodsDataList[i].Using > 0 && client.ClientData.GoodsDataList[i].Site == 0)
                    {   
                        int nBagIndex = Global.GetIdleSlotOfDamonGoods(client);
                        string[] dbFields = null;
                        String strcmd = Global.FormatUpdateDBGoodsStr(client.ClientData.RoleID, client.ClientData.GoodsDataList[i].Id, client.ClientData.GoodsDataList[i].Using, "*", "*", "*", (int)SaleGoodsConsts.UsingDemonGoodsID, "*", "*", client.ClientData.GoodsDataList[i].GCount, "*", nBagIndex, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*"); // 卓越信息 [12/13/2013 LiaoWei] 装备转生
                        TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(TCPClientPool.getInstance(), TCPOutPacketPool.getInstance(), (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strcmd, out dbFields, client.ServerId);
                        if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                        {
                            continue;
                        }

                        if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
                        {
                            continue;
                        }

                        AddDamonGoodsData(client, client.ClientData.GoodsDataList[i], false);
                        client.ClientData.GoodsDataList[i].Site = (int)SaleGoodsConsts.UsingDemonGoodsID;
                        client.ClientData.GoodsDataList[i].BagIndex = nBagIndex;
                        listDamon.Add(client.ClientData.GoodsDataList[i]);
                    }
                }
            }

            // 添加到精灵栏的精灵从背包中移除
            for (int i = 0; i < listDamon.Count; i++)
            {
                Global.RemoveGoodsData(client, listDamon[i]);
            }

            JingLingQiYuanManager.getInstance().RefreshProps(client);
        }

        /// <summary>
        /// 初始化列表
        /// </summary>
        public static void InitDemonGoodsDataList(GameClient client)
        {
            if (null == client.ClientData.DamonGoodsDataList)
            {
                client.ClientData.DamonGoodsDataList = Global.sendToDB<List<GoodsData>, string>((int)TCPGameServerCmds.CMD_GETGOODSLISTBYSITE, string.Format("{0}:{1}", client.ClientData.RoleID, (int)SaleGoodsConsts.UsingDemonGoodsID), client.ServerId);

                //这样做能够保证gamedb的请求每次客户端登录后最多一次
                if (null == client.ClientData.DamonGoodsDataList || client.ClientData.DamonGoodsDataList.Count == 0)
                {
                    client.ClientData.DamonGoodsDataList = new List<GoodsData>();
                    DamonMgr.AddOldDamonGoodsData(client);
                }
            }

            JingLingQiYuanManager.getInstance().RefreshProps(client);
        }

        /// <summary>
        /// 添加物品到精灵装备栏队列中
        /// </summary>
        /// <param name="client"></param>
        public static GoodsData AddDamonGoodsData(GameClient client, int id, int goodsID, int forgeLevel, int quality, int goodsNum, int binding, int site, string jewelList, string endTime, int addPropIndex, int bornIndex, int lucky, int strong, int ExcellenceProperty, int nAppendPropLev, int nEquipChangeLife)
        {
            GoodsData gd = new GoodsData()
            {
                Id = id,
                GoodsID = goodsID,
                Using = 0,
                Forge_level = forgeLevel,
                Starttime = "1900-01-01 12:00:00",
                Endtime = endTime,
                Site = site,
                Quality = quality,
                Props = "",
                GCount = goodsNum,
                Binding = binding,
                Jewellist = jewelList,
                BagIndex = 0,
                AddPropIndex = addPropIndex,
                BornIndex = bornIndex,
                Lucky = lucky,
                Strong = strong,
                ExcellenceInfo = ExcellenceProperty,
                AppendPropLev = nAppendPropLev,
                ChangeLifeLevForEquip = nEquipChangeLife,

            };

            AddDamonGoodsData(client, gd);
            return gd;
        }

        /// <summary>
        /// 删除精灵装备栏物品
        /// </summary>
        /// <param name="goodsData"></param>
        public static void RemoveDamonGoodsData(GameClient client, GoodsData goodsData)
        {
            if (null == client.ClientData.DamonGoodsDataList) return;

            if (goodsData.Site != (int)SaleGoodsConsts.UsingDemonGoodsID) return;

            lock (client.ClientData.DamonGoodsDataList)
            {
                client.ClientData.DamonGoodsDataList.Remove(goodsData);
            }

            JingLingQiYuanManager.getInstance().RefreshProps(client);
        }

        /// <summary>
        /// 整理用户的精灵装备栏
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ResetDamonBagAllGoods(GameClient client)
        {
            if (null != client.ClientData.DamonGoodsDataList)
            {
                lock (client.ClientData.DamonGoodsDataList)
                {
                    Dictionary<string, GoodsData> oldGoodsDict = new Dictionary<string, GoodsData>();
                    List<GoodsData> toRemovedGoodsDataList = new List<GoodsData>();
                    for (int i = 0; i < client.ClientData.DamonGoodsDataList.Count; i++)
                    {
                        if (client.ClientData.DamonGoodsDataList[i].Using > 0)
                        {
                            continue;
                        }

                        client.ClientData.DamonGoodsDataList[i].BagIndex = 1;
                        int gridNum = Global.GetGoodsGridNumByID(client.ClientData.DamonGoodsDataList[i].GoodsID);
                        if (gridNum <= 1)
                        {
                            continue;
                        }

                        GoodsData oldGoodsData = null;
                        string key = string.Format("{0}_{1}_{2}", client.ClientData.DamonGoodsDataList[i].GoodsID,
                            client.ClientData.DamonGoodsDataList[i].Binding, Global.DateTimeTicks(client.ClientData.DamonGoodsDataList[i].Endtime));
                        if (oldGoodsDict.TryGetValue(key, out oldGoodsData))
                        {
                            int toAddNum = Global.GMin((gridNum - oldGoodsData.GCount), client.ClientData.DamonGoodsDataList[i].GCount);

                            oldGoodsData.GCount += toAddNum;

                            client.ClientData.DamonGoodsDataList[i].GCount -= toAddNum;
                            client.ClientData.DamonGoodsDataList[i].BagIndex = 1;
                            oldGoodsData.BagIndex = 1;
                            if (!Global.ResetBagGoodsData(client, client.ClientData.DamonGoodsDataList[i]))
                            {
                                //出错, 停止整理
                                break;
                            }

                            if (oldGoodsData.GCount >= gridNum) //旧的物品已经加满
                            {
                                if (client.ClientData.DamonGoodsDataList[i].GCount > 0)
                                {
                                    oldGoodsDict[key] = client.ClientData.DamonGoodsDataList[i];
                                }
                                else
                                {
                                    oldGoodsDict.Remove(key);
                                    toRemovedGoodsDataList.Add(client.ClientData.DamonGoodsDataList[i]);
                                }
                            }
                            else
                            {
                                if (client.ClientData.DamonGoodsDataList[i].GCount <= 0)
                                {
                                    toRemovedGoodsDataList.Add(client.ClientData.DamonGoodsDataList[i]);
                                }
                            }
                        }
                        else
                        {
                            oldGoodsDict[key] = client.ClientData.DamonGoodsDataList[i];
                        }
                    }

                    for (int i = 0; i < toRemovedGoodsDataList.Count; i++)
                    {
                        client.ClientData.DamonGoodsDataList.Remove(toRemovedGoodsDataList[i]);
                    }

                    //按照物品分类排序
                    client.ClientData.DamonGoodsDataList.Sort(delegate(GoodsData x, GoodsData y)
                    {
                        //return (Global.GetGoodsCatetoriy(y.GoodsID) - Global.GetGoodsCatetoriy(x.GoodsID));
                        return (y.GoodsID - x.GoodsID);
                    });

                    int index = 0;
                    for (int i = 0; i < client.ClientData.DamonGoodsDataList.Count; i++)
                    {
                        if (client.ClientData.DamonGoodsDataList[i].Using > 0)
                        {
                            continue;
                        }

                        if (false && GameManager.Flag_OptimizationBagReset)
                        {
                            bool godosCountChanged = client.ClientData.DamonGoodsDataList[i].BagIndex > 0;
                            client.ClientData.DamonGoodsDataList[i].BagIndex = index++;
                            if (godosCountChanged)
                            {
                                if (!Global.ResetBagGoodsData(client, client.ClientData.DamonGoodsDataList[i]))
                                {
                                    //出错, 停止整理
                                    break;
                                }
                            }
                        }
                        else
                        {
                            client.ClientData.DamonGoodsDataList[i].BagIndex = index++;
                            if (!Global.ResetBagGoodsData(client, client.ClientData.DamonGoodsDataList[i]))
                            {
                                //出错, 停止整理
                                break;
                            }
                        }
                    }
                }
            }

            TCPOutPacket tcpOutPacket = null;

            if (null != client.ClientData.DamonGoodsDataList)
            {
                //先锁定
                lock (client.ClientData.DamonGoodsDataList)
                {
                    tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<GoodsData>>(client.ClientData.DamonGoodsDataList, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_RESETJINDANBAG);
                }
            }
            else
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<GoodsData>>(client.ClientData.DamonGoodsDataList, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_RESETJINDANBAG);
            }

            Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket);
        }

        /// <summary>
        /// 精灵装备栏是否已经满？是否可以添加指定的物品
        /// </summary>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        public static bool CanAddGoodsToDamonCangKu(GameClient client, int goodsID, int newGoodsNum, int binding, string endTime = Global.ConstGoodsEndTime, bool canUseOld = true)
        {
            if (client.ClientData.DamonGoodsDataList == null)
            {
                return true;
            }

            /// 获取物品是否可以叠加的值
            int gridNum = Global.GetGoodsGridNumByID(goodsID);
            gridNum = Global.GMax(gridNum, 1);

            bool findOldGrid = false;
            int totalGridNum = 0;
            lock (client.ClientData.DamonGoodsDataList)
            {
                for (int i = 0; i < client.ClientData.DamonGoodsDataList.Count; i++)
                {
                    totalGridNum++;
                    if (canUseOld && gridNum > 1) //是否可以共占
                    {
                        if (client.ClientData.DamonGoodsDataList[i].GoodsID == goodsID &&
                            client.ClientData.DamonGoodsDataList[i].Binding == binding &&
                            Global.DateTimeEqual(client.ClientData.DamonGoodsDataList[i].Endtime, endTime))
                        {
                            if ((client.ClientData.DamonGoodsDataList[i].GCount + newGoodsNum) <= gridNum)
                            {
                                findOldGrid = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (findOldGrid)
            {
                return true;
            }

            int totalMaxGridCount = GetDamonBagCapacity(client);
            return (totalGridNum < totalMaxGridCount);
        }

        /// <summary>
        ///  获取精灵装备栏的容量===容量默认就是最大容量===>精灵装备栏的容量值默认就是最大值
        /// </summary>
        /// <returns></returns>
        public static int GetDamonBagCapacity(GameClient client)
        {
            return Global.MaxDamonGridNum;
        }

        /// <summary>
        /// 获取精灵装备栏的精灵列表
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static List<GoodsData> GetDemonGoodsDataList(GameClient client)
        {
            List<GoodsData> demonGoodsDataList = new List<GoodsData>();
            if (null != client.ClientData.DamonGoodsDataList)
            {
                lock (client.ClientData.DamonGoodsDataList)
                {
                    demonGoodsDataList.AddRange(client.ClientData.DamonGoodsDataList);
                }
            }

            return demonGoodsDataList;
        }

        #endregion 精灵装备栏物品管理
    }
}
