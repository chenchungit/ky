using GameServer.Server;
using Server.Data;
using Server.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Damon
{
    /// <summary>
    /// 精灵升级处理类
    /// </summary>
    public class UpgradeDamon
    {
        /// <summary>
        /// 精灵属性
        /// </summary>
        private static Dictionary<int, double> UpgradeAttrDict = new Dictionary<int, double>();

        /// <summary>
        /// 精灵升级处理
        /// </summary>
        public static void LoadUpgradeAttr()
        {
            String strAttrList = GameManager.systemParamsList.GetParamValueByName("PetQiangHuaProps");
            String[] arrAttr = strAttrList.Split('|');
            if (arrAttr != null)
            {
                for (int i = 0; i < arrAttr.Length; i++)
                {
                    String[] arrSingleAttr = arrAttr[i].Split(',');
                    if (arrSingleAttr != null || arrSingleAttr.Length != 2)
                    {
                        UpgradeAttrDict[int.Parse(arrSingleAttr[0])] = double.Parse(arrSingleAttr[1]);
                    }
                }
            }
        }

        /// <summary>
        /// 获取精灵升级系数
        /// </summary>
        public static double GetPetQiangPer(int nPropIndex)
        {
            double PetQiang = 0.0;
            UpgradeAttrDict.TryGetValue(nPropIndex, out PetQiang);

            return PetQiang;
        }

        /// <summary>
        /// 精灵升级处理
        /// </summary>
        public static TCPProcessCmdResults UpgradeDamonProcess(TCPOutPacketPool pool, GameClient client, GoodsData goodsData, out TCPOutPacket tcpOutPacket, int nID, TCPClientPool tcpClientPool, TCPManager tcpMgr)
        {
            tcpOutPacket = null;
            String strcmd = "";
            
            SystemXmlItem xmlItem = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out xmlItem) || null == xmlItem)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -13, client.ClientData.RoleID, goodsData.Id, 0, 0);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            int MaxUpgradeLevel = xmlItem.GetIntValue("SuitID") * 10 + 9;         

            // 检测强化等级
            if (goodsData.Forge_level >= MaxUpgradeLevel)
            {
                // 强化等级已到最高级
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -4, client.ClientData.RoleID, goodsData.Id, goodsData.Forge_level, goodsData.Binding);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            // 必须在精灵栏才能强化
            if (goodsData.Site != (int)SaleGoodsConsts.UsingDemonGoodsID)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -9, client.ClientData.RoleID, goodsData.Id, goodsData.Forge_level, goodsData.Binding);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            SystemXmlItem xmlItems = null;
            GameManager.SystemDamonUpgrade.SystemXmlItemDict.TryGetValue(goodsData.Forge_level + 2, out xmlItems);
            if (null == xmlItems)
            {
                // 配置错误
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -6, client.ClientData.RoleID, goodsData.Id, 0, 0);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            int nReqMoHe = xmlItems.GetIntValue("NeedEXP");
            long lHaveMoHe = GameManager.ClientMgr.GetMUMoHeValue(client);
            if (lHaveMoHe < nReqMoHe)
            {
                // 魔核不足
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -11, client.ClientData.RoleID, goodsData.Id, goodsData.Forge_level, goodsData.Binding);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            GameManager.ClientMgr.ModifyMUMoHeValue(client, -nReqMoHe, "精灵升级", true, true);

            // 找到精灵基本的扩展属性
            //EquipPropItem orgItem = GameManager.EquipPropsMgr.FindEquipPropItem(goodsData.GoodsID);

            //// 找到精灵现有的属性
            //EquipPropItem newProp = null;            
            //GameManager.EquipPropsMgr.ParseEquipProps(goodsData.Props, out newProp);

            //// 为精灵升级相应的属性
            //bool bHaveAddVal = false;
            //for (int i = 0; i < orgItem.ExtProps.Length; i++)
            //{
            //    if (orgItem.ExtProps[i] > 0)
            //    {
            //        double dAddVal = 0.0;
            //        UpgradeAttrDict.TryGetValue(i + 1, out dAddVal);
            //        if (dAddVal > 0.0)
            //        {
            //            if (newProp == null)
            //            {
            //                newProp = new EquipPropItem();
            //            }

            //            newProp.ExtProps[i] = orgItem.ExtProps[i] * (1 + (goodsData.Forge_level - 1) * dAddVal);
            //            bHaveAddVal = true;
            //        }
            //    }
            //}

            //String strProps = "";
            //if (bHaveAddVal)
            //{
            //    strProps = GameManager.EquipPropsMgr.EquipPropsToString(newProp.ExtProps);
            //}
            //else
            //{
            //    strProps = goodsData.Props;
            //}

            // 将修改后的属性保存到数据库
            int nBingProp = 1;
            string[] dbFields = null;
            string strDbCmd = Global.FormatUpdateDBGoodsStr(client.ClientData.RoleID, goodsData.Id, "*", goodsData.Forge_level + 1, "*", "*", "*", "*", "*"/*strProps*/, "*", "*", "*", "*", "*", "*", nBingProp, "*", "*", "*", "*", "*", "*", "*"); // 卓越一击 [12/13/2013 LiaoWei] 装备转生
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strDbCmd, out dbFields, client.ServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -10, client.ClientData.RoleID, goodsData.Id, 0, 0);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", -10, client.ClientData.RoleID, goodsData.Id, 0, 0);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            int oldUsing = goodsData.Using;
            bool nRet = true;
            //如果是佩戴在身上，则先脱下来
            if (goodsData.Using > 0)
            {
                //先强迫修改为不使用装备，记住后边改回去
                goodsData.Using = 0;

                //重新计算装备的合成属性
                Global.RefreshEquipProp(client, goodsData); //此处的变化不通知客户端
            }

            // 级别加1
            goodsData.Forge_level += 1; 
            goodsData.Binding = nBingProp;

            JingLingQiYuanManager.getInstance().RefreshProps(client);

            if (oldUsing != goodsData.Using)
            {
                goodsData.Using = oldUsing;

                //重新计算装备的合成属性
                if (Global.RefreshEquipProp(client, goodsData))
                {
                    //通知客户端属性变化
                    GameManager.ClientMgr.NotifyUpdateEquipProps(tcpMgr.MySocketListener, pool, client);

                    // 总生命值和魔法值变化通知(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOthersLifeChanged(tcpMgr.MySocketListener, pool, client);
                }
            }

            //写入角色物品的得失行为日志(扩展)
            Global.ModRoleGoodsEvent(client, goodsData, 0, "强化");
            EventLogManager.AddGoodsEvent(client, OpTypes.Forge, OpTags.None, goodsData.GoodsID, goodsData.Id, 0, goodsData.GCount, "强化");

            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 1, client.ClientData.RoleID, goodsData.Id, goodsData.Forge_level, nBingProp);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            return TCPProcessCmdResults.RESULT_DATA;
        }
    }
}
