using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
//using System.Windows.Forms;
using System.Windows;
using Server.Data;
using ProtoBuf;
using GameServer.Logic;
using GameServer.Server;

namespace GameServer.Logic
{
    /// <summary>
    /// 物品使用处理
    /// </summary>
    public class UsingGoods
    {
        /// <summary>
        /// 处理精灵的使用物品的校验
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <param name="client"></param>
        public static int ProcessUsingGoodsVerify(GameClient client, int goodsID, int binding, out List<MagicActionItem> magicActionItemList, out int categoriy)
        {
            magicActionItemList = null;
            categoriy = 0;
            if (!GameManager.SystemMagicActionMgr.GoodsActionsDict.TryGetValue(goodsID, out magicActionItemList) || null == magicActionItemList)
            {
                SystemXmlItem goodsXmlItem;
                if (GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsID, out goodsXmlItem) && null != goodsXmlItem && goodsXmlItem.GetIntValue("BaoguoID") > 0)
                {
                    return 1; //可以不配置脚本的物品
                }
                //物品没有配置脚本
                return -3;
            }

            //强制装备不能使用脚本
            SystemXmlItem systemGoodsItem = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsID, out systemGoodsItem))
            {
                return -4;
            }

            categoriy = (int)systemGoodsItem.GetIntValue("Categoriy");
            if (categoriy >= (int)ItemCategories.TouKui && categoriy < (int)ItemCategories.EquipMax)
            {
                return -5;
            }

            // 如果是使用果实，需要进行上限判断
            if ((int)ItemCategories.ItemAddVal == categoriy)
            {
                // 一种果实只加一种属性
                for (int j = 0; j < magicActionItemList.Count; j++)
                {
                    if (!MagicAction.ProcessAction(client, client, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, -1, -1, 0, 1, -1, 0, binding, -1, goodsID, true, true))
                    {
                        // 达到上限
                        return -1;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// 处理精灵的攻击动作
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <param name="client"></param>
        public static int ProcessUsingGoods(GameClient client, int goodsID, int binding, List<MagicActionItem> magicActionItemList, int categoriy)
        {
            bool bItemAddVal = false;
            // 如果是使用果实，需要进行上限判断
            if ((int)ItemCategories.ItemAddVal == categoriy)
            {
                bItemAddVal = true;
            }

            for (int j = 0; j < magicActionItemList.Count; j++)
            {
                MagicAction.ProcessAction(client, client, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, -1, -1, 0, 1, -1, 0, binding, -1, goodsID, bItemAddVal, false);
            }

            return 0;
        }

        /// <summary>
        /// 获取指定物品的公式列表
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <param name="client"></param>
        public static List<MagicActionItem> GetMagicActionListByGoodsID(int goodsID)
        {
            List<MagicActionItem> magicActionItemList = null;
            if (!GameManager.SystemMagicActionMgr.GoodsActionsDict.TryGetValue(goodsID, out magicActionItemList))
            {
                return null;
            }

            return magicActionItemList;
        }
    }
}
