using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using GameServer.Server;

namespace GameServer.Logic
{
    /// <summary>
    /// 宝石镶嵌管理
    /// </summary>
    public class EnchaseJewelMgr
    {
        /// <summary>
        /// 处理宝石镶嵌
        /// </summary>
        /// <param name="goodsDbID"></param>
        /// <param name="ironNum"></param>
        /// <param name="goldRock"></param>
        /// <param name="luckyNum"></param>
        /// <returns></returns>
        public static int ProcessEnchaseJewel(GameClient client, int actionType, int equipGoodsDbID, int jewelGoodsIDorDbID, out string jewellist, out int binding)
        {
            jewellist = "";
            binding = 0;

            //判断背包中是否有此装备
            GoodsData equipGoodsData = Global.GetGoodsByDbID(client, equipGoodsDbID);
            if (null == equipGoodsData)
            {
                return -1;
            }

            if (equipGoodsData.Site != 0)
            {
                return -9998;
            }

            if (equipGoodsData.Using > 0)
            {
                return -9999;
            }

            //首先判断要进阶的物品是否是装备，如果不是，则返回失败代码
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(equipGoodsData.GoodsID, out systemGoods))
            {
                return -2;
            }

            //判断是否是装备
            int categoriy = systemGoods.GetIntValue("Categoriy");
            if (categoriy < (int)ItemCategories.TouKui || categoriy >= (int)ItemCategories.EquipMax)
            {
                return -3;
            }

            GoodsData jewelGoodsData = null;
            int jewelGoodsID = 0;
            if ((int)EnchaseJewelTypes.Enchase == actionType) //镶嵌宝石
            {
                jewelGoodsData = Global.GetGoodsByDbID(client, jewelGoodsIDorDbID);
                if (null == jewelGoodsData || jewelGoodsData.GCount <= 0)
                {
                    return -100;
                }

                jewelGoodsID = jewelGoodsData.GoodsID;
            }
            else
            {
                jewelGoodsID = jewelGoodsIDorDbID;
            }

            //判断是否是可以镶嵌的宝石
            if (!Global.CanEnchaseJewel(jewelGoodsID))
            {
                return -4;
            }

            //判断装备上是否能够镶嵌指定的宝石
            if (!Global.CanAddJewelIntoEquip(equipGoodsData.GoodsID, jewelGoodsID))
            {
                return -5;
            }           

            if ((int)EnchaseJewelTypes.Enchase == actionType) //镶嵌宝石
            {
                //扣除一块宝石
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                    Global._TCPManager.TcpOutPacketPool, client, jewelGoodsData, 1, false))
                {
                    return -101;
                }

                if (!string.IsNullOrEmpty(equipGoodsData.Jewellist))
                {
                    string[] jewelFields = equipGoodsData.Jewellist.Split(',');
                    if (jewelFields.Length >= 6) //控制镶嵌的个数
                    {
                        return -110;
                    }
                }

                jewellist = equipGoodsData.Jewellist;
                if (jewellist.Length > 0)
                {
                    jewellist += ",";
                }

                jewellist += string.Format("{0}", jewelGoodsID);
                binding = equipGoodsData.Binding;
                if (equipGoodsData.Binding != jewelGoodsData.Binding)
                {
                    if (jewelGoodsData.Binding > 0)
                    {
                        binding = 1; //强制装备也绑定
                    }
                }

                //修改物品的宝石列表
                if (Global.ModGoodsJewelDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                    equipGoodsData, jewellist, binding) < 0)
                {
                    return -102;
                }
            }
            else //取出镶嵌的宝石
            {
                //判断背包中是否有空格
                if (!Global.CanAddGoods(client, jewelGoodsID, 1, 0))
                {
                    return -200;
                }

                jewellist = equipGoodsData.Jewellist;
                if (string.IsNullOrEmpty(jewellist))
                {
                    return -201;
                }

                string[] fields = jewellist.Split(',');

                List<string> copyList = new List<string>();
                for (int i = 0; i < fields.Length; i++)
                {
                    copyList.Add(fields[i].Trim());
                }

                bool findJewel = false;
                for (int i = 0; i < copyList.Count; i++)
                {
                    if (copyList[i] == jewelGoodsID.ToString())
                    {
                        findJewel = true;
                        copyList.RemoveAt(i);
                        break;
                    }
                }

                if (!findJewel)
                {
                    return -300;
                }

                jewellist = "";
                for (int i = 0; i < copyList.Count; i++)
                {
                    if (jewellist.Length > 0)
                    {
                        jewellist += ",";
                    }

                    jewellist += copyList[i];
                }

                //修改物品的宝石列表
                if (Global.ModGoodsJewelDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                    equipGoodsData, jewellist, equipGoodsData.Binding) < 0)
                {
                    return -202;
                }

                //给予新的宝石
                int dbRet = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, jewelGoodsID, 1, 0, "", 0, equipGoodsData.Binding, 0, "", true, 1, "宝石解镶嵌");
                if (dbRet < 0)
                {
                    return -203;
                }
            }

            return 0;
        }
    }
}
