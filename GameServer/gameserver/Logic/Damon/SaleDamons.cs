using GameServer.Logic.Goods;
using GameServer.Server;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Damon
{
    /// <summary>
    /// 精灵回收处理类
    /// </summary>
    public class SaleDamons
    {
        /// <summary>
        /// 精灵回收处理
        /// </summary>
        public static TCPProcessCmdResults SaleDamonsProcess(GameClient client, int nRoleID, String strGoodsID)
        {
            int nTotalMoHe = 0;
            //int totalExp = 0;

            string[] idsList = strGoodsID.Split(',');
            for (int i = 0; i < idsList.Length; i++)
            {
                int goodsDbID = Global.SafeConvertToInt32(idsList[i]);
                GoodsData goodsData = Global.GetGoodsByDbID(client, goodsDbID);
                if (null != goodsData && goodsData.Site == 0 && goodsData.Using <= 0) //必须在背包中
                {
                    int category = Global.GetGoodsCatetoriy(goodsData.GoodsID);

                    //判断是否装备，装备才能分解
                    if (category < (int)ItemCategories.ShouHuChong || category > (int)ItemCategories.ChongWu)
                    {
                        continue;
                    }

                    SystemXmlItem xmlItem = null;
                    if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out xmlItem) || null == xmlItem)
                    {
                        continue;
                    }                    

                    string modGoodsCmd = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, (int)ModGoodsTypes.Destroy,
                        goodsData.Id, goodsData.GoodsID, 0, goodsData.Site, goodsData.GCount, goodsData.BagIndex, "");
                    if (TCPProcessCmdResults.RESULT_OK != Global.ModifyGoodsByCmdParams(client, modGoodsCmd))
                    {
                        continue;
                    }

                    // 精灵回收魔核=精灵等级所需魔核和+基础魔核
                    int nMoHePrice = xmlItem.GetIntValue("ZhanHunPrice");
                    if (nMoHePrice > 0)
                    {
                        nTotalMoHe += nMoHePrice;
                    }

                    for (int j = 0; j < goodsData.Forge_level; j++)
                    {
                        SystemXmlItem xmlItems = null;
                        GameManager.SystemDamonUpgrade.SystemXmlItemDict.TryGetValue(j + 2, out xmlItems);
                        if (null == xmlItems)
                        {
                            continue;
                        }

                        int nReqMoHe = xmlItems.GetIntValue("NeedEXP");
                        if (nReqMoHe > 0)
                        {
                            nTotalMoHe += nReqMoHe;
                        }
                    }

                   nTotalMoHe += (int)PetSkillManager.DelGoodsReturnLingJing(goodsData);
                }
            }

            // 增加天地精元 必须设置通知客户端
            if (nTotalMoHe > 0)
            {
                GameManager.ClientMgr.ModifyMUMoHeValue(client, nTotalMoHe, "一键出售或者回收", true, true);
            }

            return TCPProcessCmdResults.RESULT_OK;
        }
        
        /// <summary>
        /// 仓库里的精灵回收处理
        /// </summary>
        public static TCPProcessCmdResults SaleStoreDamonsProcess(GameClient client, int nRoleID, String strGoodsID)
        {
            int nTotalMoHe = 0;
            //int totalExp = 0;

            string[] idsList = strGoodsID.Split(',');
            for (int i = 0; i < idsList.Length; i++)
            {
                int goodsDbID = Global.SafeConvertToInt32(idsList[i]);
                GoodsData goodsData = CallPetManager.GetPetByDbID(client, goodsDbID);
                if (null != goodsData && goodsData.Site == (int)SaleGoodsConsts.PetBagGoodsID && goodsData.Using <= 0) //必须在仓库中
                {
                    int category = Global.GetGoodsCatetoriy(goodsData.GoodsID);

                    //判断是否装备，装备才能分解
                    if (category < (int)ItemCategories.ShouHuChong || category > (int)ItemCategories.ChongWu)
                    {
                        continue;
                    }

                    SystemXmlItem xmlItem = null;
                    if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out xmlItem) || null == xmlItem)
                    {
                        continue;
                    }                    

                    string modGoodsCmd = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, (int)ModGoodsTypes.Destroy,
                        goodsData.Id, goodsData.GoodsID, 0, goodsData.Site, goodsData.GCount, goodsData.BagIndex, "");
                    if (TCPProcessCmdResults.RESULT_OK != Global.ModifyGoodsByCmdParams(client, modGoodsCmd))
                    {
                        continue;
                    }

                    // 精灵回收魔核=精灵等级所需魔核和+基础魔核
                    int nMoHePrice = xmlItem.GetIntValue("ZhanHunPrice");
                    if (nMoHePrice > 0)
                    {
                        nTotalMoHe += nMoHePrice;
                    }

                    for (int j = 0; j < goodsData.Forge_level; j++)
                    {
                        SystemXmlItem xmlItems = null;
                        GameManager.SystemDamonUpgrade.SystemXmlItemDict.TryGetValue(j + 2, out xmlItems);
                        if (null == xmlItems)
                        {
                            continue;
                        }

                        int nReqMoHe = xmlItems.GetIntValue("NeedEXP");
                        if (nReqMoHe > 0)
                        {
                            nTotalMoHe += nReqMoHe;
                        }
                    }
                }
            }

            // 增加天地精元 必须设置通知客户端
            if (nTotalMoHe > 0)
            {
                GameManager.ClientMgr.ModifyMUMoHeValue(client, nTotalMoHe, "一键出售或者回收", true, true);
            }

            return TCPProcessCmdResults.RESULT_OK;
        }
    }
}
