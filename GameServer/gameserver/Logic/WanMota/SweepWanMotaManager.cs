using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Xml.Linq;
using Server.Data;
using System.Windows;
using Server.Tools;
using Server.Protocol;
using System.Timers;
using GameServer.Core.Executor;

namespace GameServer.Logic.WanMota
{
    // 万魔塔副本扫荡管理器 [6/9/2014 ChenXiaojun]
    class SweepWanMotaManager
    {
        /// <summary>
        /// 扫荡需要的最小挑战通关层数
        /// </summary>
        public static readonly int nSweepReqMinLayerOrder = 1;

        /// <summary>
        /// 万魔塔扫荡副本编号
        /// </summary>
        public static readonly int nWanMoTaSweepFuBenOrder = 19999;

        /// <summary>
        /// 万魔塔最大扫荡次数
        /// </summary>
        public static readonly int nWanMoTaMaxSweepNum = 1;

        /// <summary>
        /// 获取万魔塔扫荡次数
        /// </summary>
        public static int GetSweepCount(GameClient client)
        {
            FuBenData fuBenData = Global.GetFuBenData(client, nWanMoTaSweepFuBenOrder);
            if (null == fuBenData)
            {
                return 0;
            }

            return fuBenData.EnterNum;
        }

        /// <summary>
        /// 开始扫荡
        /// </summary>
        public static void SweepBegin(GameClient client)
        {
            if (client.ClientData.WanMoTaProp.nPassLayerCount < nSweepReqMinLayerOrder)
            {
                return;
            }

            // 每2秒扫荡一层
            if (null == client.ClientData.WanMoTaSweeping)
            {
                client.ClientData.WanMoTaSweeping = new SweepWanmota(client);
            }

            client.ClientData.WanMoTaSweeping.nSweepingOrder = 1;
            client.ClientData.WanMoTaSweeping.nSweepingMaxOrder = client.ClientData.WanMoTaProp.nPassLayerCount;// ((int)((client.ClientData.WanMoTaProp.nPassLayerCount) / 10)) * 10;
            client.ClientData.WanMoTaProp.lFlushTime = TimeUtil.NOW();
            client.ClientData.WanMoTaSweeping.BeginSweeping();

            // 利用副本19999进行扫荡次数更新
            if (-1 != WanMoTaDBCommandManager.SweepBeginDBCommand(client, 1))
            {
                Global.UpdateFuBenData(client, nWanMoTaSweepFuBenOrder, 1, 1);
            }
        }

        /// <summary>
        /// 继续扫荡
        /// </summary>
        public static void SweepContinue(GameClient client)
        {
            if (client.ClientData.WanMoTaProp.nPassLayerCount < nSweepReqMinLayerOrder)
            {
                return;
            }

            // 每2秒扫荡一层
            if (null == client.ClientData.WanMoTaSweeping)
            {
                client.ClientData.WanMoTaSweeping = new SweepWanmota(client);
            }

            client.ClientData.WanMoTaSweeping.nSweepingOrder = client.ClientData.WanMoTaProp.nSweepLayer;
            client.ClientData.WanMoTaSweeping.nSweepingMaxOrder = client.ClientData.WanMoTaProp.nPassLayerCount; //((int)((client.ClientData.WanMoTaProp.nPassLayerCount) / 10)) * 10;
            client.ClientData.WanMoTaSweeping.BeginSweeping();
        }

        /// <summary>
        /// 更新扫荡信息到客户端
        /// </summary>
        public static void UpdataSweepInfo(GameClient client, List<SingleLayerRewardData> listRewardData)
        {
            client.sendCmd<List<SingleLayerRewardData>>((int)TCPGameServerCmds.CMD_SPR_UPDATE_SWEEP_STATE, listRewardData);
        }

        /// <summary>
        /// 汇总扫荡奖励
        /// </summary>
        public static List<SingleLayerRewardData> SummarySweepRewardInfo(GameClient client)
        {
            List<SingleLayerRewardData> listRewardData = null;
            if (null == client.ClientData.LayerRewardData || client.ClientData.LayerRewardData.WanMoTaLayerRewardList.Count < 1)
            {
                return listRewardData;
            }

            int nExp = 0;
            int nMoney = 0;
            int nXinHun = 0;
            List<GoodsData> rewardList = new List<GoodsData>();
            lock (client.ClientData.LayerRewardData)
            {
                // 将各层的奖励汇总
                for (int i = 0; i < client.ClientData.LayerRewardData.WanMoTaLayerRewardList.Count; i++)
                {
                    nExp += client.ClientData.LayerRewardData.WanMoTaLayerRewardList[i].nExp;
                    nMoney += client.ClientData.LayerRewardData.WanMoTaLayerRewardList[i].nMoney;
                    nXinHun += client.ClientData.LayerRewardData.WanMoTaLayerRewardList[i].nXinHun;

                    if (null != client.ClientData.LayerRewardData.WanMoTaLayerRewardList[i].sweepAwardGoodsList)
                    {
                        for (int j = 0; j < client.ClientData.LayerRewardData.WanMoTaLayerRewardList[i].sweepAwardGoodsList.Count; j++)
                        {
                            CombineGoodList(rewardList, client.ClientData.LayerRewardData.WanMoTaLayerRewardList[i].sweepAwardGoodsList[j]);
                        }
                    }
                }
                
                SingleLayerRewardData layerReward = WanMotaCopySceneManager.AddSingleSweepReward(client, rewardList, 0,
                                                                         nExp, nMoney, nXinHun, out listRewardData);                
            }

            return listRewardData;
        }

        /// <summary>
        /// 合并物品
        /// </summary>
        public static void CombineGoodList(List<GoodsData> goodList, GoodsData goodData)
        {
            int gridNum = Global.GetGoodsGridNumByID(goodData.GoodsID);

            if (gridNum > 1)
            {
                for (int i = 0; i < goodList.Count; i++)
                {
                    if (goodList[i].GoodsID == goodData.GoodsID)
                    {
                        // 如果能合并，进行合并
                        if (goodList[i].GCount + goodData.GCount <= gridNum)
                        {
                            goodList[i].GCount += goodData.GCount;
                            return;
                        }
                    }
                }
            }

            // 不能合并，作为新物品加入
            goodList.Add(goodData);
        }
    }

}
