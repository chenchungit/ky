using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    public class GoodsCoolDownMgr
    {
        #region 物品CoolDown 冷却管理

        /// 技能CoolDown项
        private Dictionary<int, CoolDownItem> GoodsCoolDownDict = new Dictionary<int, CoolDownItem>();

        /// <summary>
        /// 判断物品是否处于冷却状态
        /// </summary>
        /// <param name="magicCode"></param>
        /// <returns></returns>
        public bool GoodsCoolDown(int goodsID)
        {
            CoolDownItem coolDownItem = null;
            if (!GoodsCoolDownDict.TryGetValue(goodsID, out coolDownItem))
            {
                //物品不在冷却辞典中，当已冷却处理, 主要针对第一次使用获取非cooldownitem
                return true;
            }

            long ticks = TimeUtil.NOW();
            if (ticks > (coolDownItem.StartTicks + coolDownItem.CDTicks))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 添加物品冷却
        /// </summary>
        /// <param name="magicCode"></param>
        public void AddGoodsCoolDown(GameClient client, int goodsID)
        {
            // 物品名字索引管理
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsID, out systemGoods))
            {
                return;
            }

            int cdTime = systemGoods.GetIntValue("CDTime");
            if (cdTime <= 0) //不需要CD时间
            {
                return;
            }

            int pubCDTime = systemGoods.GetIntValue("PubCDTime");
            int cdGroup = systemGoods.GetIntValue("ShareGroupID");

            long nowTicks = TimeUtil.NOW();
            Global.AddCoolDownItem(GoodsCoolDownDict, goodsID, nowTicks, cdTime * 1000);
            if (cdGroup > 0)
            {
                if (null != client.ClientData.GoodsDataList)
                {
                    for (int i = 0; i < client.ClientData.GoodsDataList.Count; i++)
                    {
                        GoodsData goodsData = client.ClientData.GoodsDataList[i];
                        if (null == goodsData) continue;
                        if (goodsData.Using > 0) continue;

                        SystemXmlItem systemGoods2 = null;
                        if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out systemGoods2))
                        {
                            continue;
                        }

                        if (null == systemGoods2)
                        {
                            continue;
                        }

                        if (cdGroup == systemGoods2.GetIntValue("ShareGroupID")) //同组
                        {
                            Global.AddCoolDownItem(GoodsCoolDownDict, goodsData.GoodsID, nowTicks, pubCDTime * 1000);
                        }
                    }
                }
            }
        }

        #endregion 物品CoolDown 冷却管理

    }
}
