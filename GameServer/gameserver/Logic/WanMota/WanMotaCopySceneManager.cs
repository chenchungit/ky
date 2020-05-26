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
using GameServer.Logic;

using GameServer.Server.CmdProcesser;
using GameServer.Core.Executor;

namespace GameServer.Logic.WanMota
{
    // 万魔塔副本管理器 [6/6/2014 ChenXiaojun]
    public class WanMotaCopySceneManager
    {
        // modify by chenjingui. 20150715
        // 注意：万魔塔暂时写死为150层，如果动态读取可从SystemParam.xml读WanMoTaFenZu

        /// <summary>
        /// 万魔塔第一层副本编号
        /// </summary>
        private static int _firstFuBenOrder_Impl = 20000;
        public static int nWanMoTaFirstFuBenOrder { get { return _firstFuBenOrder_Impl; } }

        /// <summary>
        /// 万魔塔最后副本编号
        /// </summary>
        private static int _lastFuBenOrderImpl = 20149;
        public static int nWanMoTaLastFuBenOrder { get { return _lastFuBenOrderImpl; } }

        /// <summary>
        /// 是否万魔塔的地图编号
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public static bool IsWanMoTaMapCode(int mapCode)
        {
            if (mapCode >= nWanMoTaFirstFuBenOrder && mapCode <= nWanMoTaLastFuBenOrder)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 万魔塔是否在扫荡状态
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public static int WanmotaIsSweeping(GameClient client)
        {   
            if (null != client.ClientData.WanMoTaProp
                && null != client.ClientData.WanMoTaSweeping
                && client.ClientData.WanMoTaProp.nSweepLayer >= 0
                && null != client.ClientData.WanMoTaSweeping.WanMoTaSweepingTimer)
            {
                return 0;
            }

            return 1;
        }

        /// <summary>
        /// 获取万魔塔信息并缓存到本地
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public static WanMotaInfo GetWanMoTaDetail(GameClient client, bool bIsLogin)
        {
            WanMotaInfo dataWanMoTa = null;
            // 在扫荡状态，不向数据库请求万魔塔数据
            if (1 == WanmotaIsSweeping(client))
            {
                //获取玩家竞技场数据
                dataWanMoTa = Global.sendToDB<WanMotaInfo, byte[]>((int)TCPGameServerCmds.CMD_DB_GET_WANMOTA_DETAIL, DataHelper.ObjectToBytes<int>(client.ClientData.RoleID), client.ServerId);

                client.ClientData.WanMoTaProp = dataWanMoTa;                
            }
            else
            {
                dataWanMoTa = client.ClientData.WanMoTaProp;
            }

            if (null != dataWanMoTa)
            {
                if (bIsLogin)
                {
                    if (null != client.ClientData.WanMoTaProp)
                    {
                        byte[] bytes = Convert.FromBase64String(client.ClientData.WanMoTaProp.strSweepReward);
                        client.ClientData.LayerRewardData = DataHelper.BytesToObject<LayerRewardData>(bytes, 0, bytes.Length);
                    }
                }
                else
                {
                    // 选10条奖励信息发送到客户端
                    if (null != client.ClientData.LayerRewardData)
                    {
                        lock (client.ClientData.LayerRewardData)
                        {
                            if (client.ClientData.LayerRewardData.WanMoTaLayerRewardList.Count > 0)
                            {
                                int nBeginIndex = 0;
                                if (client.ClientData.LayerRewardData.WanMoTaLayerRewardList.Count > 10)
                                {
                                    nBeginIndex = client.ClientData.LayerRewardData.WanMoTaLayerRewardList.Count - 10;
                                }

                                List<SingleLayerRewardData> listRewardData = new List<SingleLayerRewardData>();
                                for (int i = nBeginIndex; i < client.ClientData.LayerRewardData.WanMoTaLayerRewardList.Count; i++)
                                {
                                    listRewardData.Add(client.ClientData.LayerRewardData.WanMoTaLayerRewardList[i]);
                                }

                                SweepWanMotaManager.UpdataSweepInfo(client, listRewardData);
                            }
                        }
                    }
                }
            }

            return dataWanMoTa;
        }

        /// <summary>
        /// 获取BOSS奖励
        /// </summary>
        /// <param name="client"></param>
        public static void GetBossReward(GameClient client, int nFubenID, List<GoodsData> goodNormal, List<int> GoodsIDList)
        {
            SystemXmlItem systemFuBenItem = null;
            if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(nFubenID, out systemFuBenItem))
            {
                return;
            }
  
            // 增加BOSS掉落物品奖励
            int nBossGoodsPackID = -1;
            nBossGoodsPackID = systemFuBenItem.GetIntValue("BossGoodsList");

            if (nBossGoodsPackID > 0)
            {
                int maxFallCountByID = GameManager.GoodsPackMgr.GetFallGoodsMaxCount(nBossGoodsPackID);
                if (maxFallCountByID <= 0)
                {
                    maxFallCountByID = GoodsPackManager.MaxFallCount;
                }

                // 根据物品掉落ID获取要掉落的物品
                List<GoodsData> goodsDataList = GameManager.GoodsPackMgr.GetGoodsDataList(client, nBossGoodsPackID, maxFallCountByID, 0);
                if (null != goodsDataList && goodsDataList.Count > 0)
                {
                    for (int j = 0; j < goodsDataList.Count; ++j)
                    {
                        goodNormal.Add(goodsDataList[j]);
                        GoodsIDList.Add(goodsDataList[j].GoodsID);
                    }
                }
            }
        }

        /// <summary>
        /// 获取物品通关的物品奖励
        /// </summary>
        /// <param name="client"></param>
        public static void AddRewardToClient(GameClient client, List<GoodsData> goodNormal, int nExp, int nMoney, int nXinHun, string strTitle)
        {
            if (null != goodNormal)
            {
                // 如果背包格子不够 就发邮件-附件带物品给玩家
                if (!Global.CanAddGoodsNum(client, goodNormal.Count))
                {
                    Global.UseMailGivePlayerAward2(client, goodNormal, strTitle, strTitle);
                }
                else
                {
                    foreach (var item in goodNormal)
                    {
                        GoodsData goodsData = new GoodsData(item);

                        //向DBServer请求加入某个新的物品到背包中
                        //添加物品
                        goodsData.Id = Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount, goodsData.Quality,
                                                                        goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, strTitle,
                                                                        true, goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, true); // 卓越信息 [12/13/2013 LiaoWei]

                    }
                }
            }

            GameManager.ClientMgr.ModifyStarSoulValue(client, nXinHun, strTitle, true, true);
            GameManager.ClientMgr.ProcessRoleExperience(client, nExp, true, true);
            GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nMoney, "万魔塔", false);
        }

        /// <summary>
        /// 获取物品通关的物品奖励
        /// </summary>
        /// <param name="client"></param>
        public static void GetFubenItemReward(GameClient client, FuBenMapItem fuBenMapItem, bool bFirstPass, List<GoodsData> goodNormal, List<int> GoodsIDList)
        {
            // 首次通关物品奖励
            if (bFirstPass)
            {
                if (null != fuBenMapItem.FirstGoodsDataList)
                {
                    for (int j = 0; j < fuBenMapItem.FirstGoodsDataList.Count; ++j)
                    {
                        goodNormal.Add(fuBenMapItem.FirstGoodsDataList[j]);
                        GoodsIDList.Add(fuBenMapItem.FirstGoodsDataList[j].GoodsID);
                    }
                }
            }
            // 扫荡时的Boss奖励
            else
            {
                // 普通通关物品奖励
                if (null != fuBenMapItem.GoodsDataList)
                {
                    for (int j = 0; j < fuBenMapItem.GoodsDataList.Count; ++j)
                    {
                        goodNormal.Add(new GoodsData(fuBenMapItem.GoodsDataList[j]));
                        GoodsIDList.Add(fuBenMapItem.GoodsDataList[j].GoodsID);
                    }
                }

                GetBossReward(client, fuBenMapItem.FuBenID, goodNormal, GoodsIDList);
            }
        }

        /// <summary>
        /// 给玩家副本的奖励 -- 无视评分
        /// </summary>
        /// <param name="client"></param>
        public static FuBenTongGuanData GiveCopyMapGiftNoScore(GameClient client, FuBenMapItem fuBenMapItem, bool bFirstPass)
        {
            if (null == fuBenMapItem)
            {
                return null;
            }

            List<GoodsData> goodNormal = new List<GoodsData>();
            List<int> goodsID = new List<int>();

            // 获取副本的物品奖励
            GetFubenItemReward(client, fuBenMapItem, bFirstPass, goodNormal, goodsID);
            if (bFirstPass)
            {
                GetFubenItemReward(client, fuBenMapItem, false, goodNormal, goodsID);
            }

            FuBenTongGuanData fuBenTongGuanData = new FuBenTongGuanData();
            fuBenTongGuanData.FuBenID = fuBenMapItem.FuBenID;
			fuBenTongGuanData.TotalScore = 0;
			fuBenTongGuanData.KillNum = 0;
			fuBenTongGuanData.KillScore = 0;
			fuBenTongGuanData.MaxKillScore = 0; //击杀数=最大击杀数
			fuBenTongGuanData.UsedSecs = 0;
			fuBenTongGuanData.TimeScore = 0;
			fuBenTongGuanData.MaxTimeScore = 0;
			fuBenTongGuanData.DieCount = 0;
			fuBenTongGuanData.DieScore = 0;
			fuBenTongGuanData.MaxDieScore = 0;
			fuBenTongGuanData.GoodsIDList = goodsID;

            string strTitle = "";
            if (bFirstPass)
            {
                strTitle = string.Format(Global.GetLang("万魔塔首次通关【{0}层】奖励"), client.ClientData.WanMoTaNextLayerOrder);
            }
            else
            {
                strTitle = string.Format(Global.GetLang("万魔塔通关【{0}层】奖励"), client.ClientData.WanMoTaNextLayerOrder);
            }

            // 金币、经验奖励
            if (bFirstPass)
            {
                // 首次通关
                fuBenTongGuanData.AwardExp = fuBenMapItem.nFirstExp + fuBenMapItem.Experience;
                fuBenTongGuanData.AwardJinBi = fuBenMapItem.nFirstGold + fuBenMapItem.Money1;
                fuBenTongGuanData.AwardXingHun = fuBenMapItem.nFirstXingHunAward + fuBenMapItem.nXingHunAward;
            }
            else
            {
                fuBenTongGuanData.AwardExp = fuBenMapItem.Experience;
                fuBenTongGuanData.AwardJinBi = fuBenMapItem.Money1;
                fuBenTongGuanData.AwardXingHun = fuBenMapItem.nXingHunAward;
            }            

            // 给奖励用专门的函数
            AddRewardToClient(client, goodNormal, fuBenTongGuanData.AwardExp, fuBenTongGuanData.AwardJinBi, fuBenTongGuanData.AwardXingHun, strTitle);           

            // 保存万魔塔通关层数(角色参数)
            int nWanMoTaNextLayerOrder = GameManager.ClientMgr.GetWanMoTaPassLayerValue(client) + 1;
            GameManager.ClientMgr.SaveWanMoTaPassLayerValue(client, nWanMoTaNextLayerOrder);
            client.ClientData.WanMoTaNextLayerOrder = nWanMoTaNextLayerOrder;

            WanMoTaTopLayerManager.Instance().OnClientPass(client, nWanMoTaNextLayerOrder);
            /*
            // 当用户首次通关30层开始，每通关10层万魔塔副本，显示游戏公告
            if (nWanMoTaNextLayerOrder >= 30 && nWanMoTaNextLayerOrder % 10 == 0)
            {
                // 玩家【用户名字】勇往直前，勇不可挡，通过了万魔塔第XX层！
                string broadCastMsg = StringUtil.substitute(Global.GetLang("玩家【{0}】勇往直前，勇不可挡，通过了万魔塔第{1}层！"),
                                                            Global.FormatRoleName(client, client.ClientData.RoleName), nWanMoTaNextLayerOrder);

                //播放用户行为消息
                Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.HintMsg, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
            }

            // 当用户通关超过30层，成功成为万魔塔第一名的角色时，显示游戏公告
            if (nWanMoTaNextLayerOrder >= 30)
            {
                if ((nWanMoTaNextLayerOrder - 1) > GetWanMoTaDetailCmdProcessor.getInstance().WanMoTaTopLayer)
                {
                    string broadCastMsg = StringUtil.substitute(Global.GetLang("玩家【{0}】已势如破竹，雄霸万魔榜首！"),
                                                                Global.FormatRoleName(client, client.ClientData.RoleName));

                    //播放用户行为消息
                    Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.HintMsg, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                }
            }*/

            // 通知客户端万魔塔通关层数改变
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.WanMoTaCurrLayerOrder, 0);

            // 保存万魔塔通关层数到万魔塔数据库表
            WanMoTaDBCommandManager.LayerChangeDBCommand(client, nWanMoTaNextLayerOrder);
            return fuBenTongGuanData;
        }

        /// <summary>
        // 万魔塔副本层扫荡单层奖励
        /// </summary>
        public static SingleLayerRewardData AddSingleSweepReward(GameClient client, List<GoodsData> goodNormal, int nParamLayerOrder, int nParamExp, int nParamMoney, int nParamXinHun, out List<SingleLayerRewardData> listRewardData)
        {
            SingleLayerRewardData layerReward = new SingleLayerRewardData()
            {
                nLayerOrder = nParamLayerOrder,
                nExp = nParamExp,
                nMoney = nParamMoney,
                nXinHun = nParamXinHun,
                sweepAwardGoodsList = goodNormal,
            };

            listRewardData = new List<SingleLayerRewardData>();
            listRewardData.Add(layerReward);

            return layerReward;
        }

        /// <summary>
        // 万魔塔副本层扫荡奖励
        /// </summary>
        public static void GetWanmotaSweepReward(GameClient client, int nFubenID)
        {
            FuBenMapItem fuBenMapItem = FuBenManager.FindMapCodeByFuBenID(nFubenID, nFubenID);
            if (null != fuBenMapItem)
            {
                List<GoodsData> goodNormal = new List<GoodsData>();
                List<int> goodsID = new List<int>();

                // 获取副本的物品奖励
                GetFubenItemReward(client, fuBenMapItem, false, goodNormal, goodsID);

                if (null == client.ClientData.LayerRewardData)
                {
                    client.ClientData.LayerRewardData = new LayerRewardData();
                }

                if (IsWanMoTaMapCode(nFubenID))
                {
                    List<SingleLayerRewardData> listRewardData = null;
                    SingleLayerRewardData layerReward = AddSingleSweepReward(client, goodNormal, nFubenID - nWanMoTaFirstFuBenOrder + 1,
                                                                             fuBenMapItem.Experience, fuBenMapItem.Money1, fuBenMapItem.nXingHunAward, out listRewardData);

                    SweepWanMotaManager.UpdataSweepInfo(client, listRewardData);

                    lock (client.ClientData.LayerRewardData)
                    {
                        client.ClientData.LayerRewardData.WanMoTaLayerRewardList.Add(layerReward);
                    }
                }
            }
        }

        /// <summary>
        // 万魔塔副本层奖励
        /// </summary>
        public static void SendMsgToClientForWanMoTaCopyMapAward(GameClient client, CopyMap copyMap, bool anyAlive)
        {
            CopyMap tmpCopyMap = copyMap;

            if (tmpCopyMap == null)
                return;

            int fuBenSeqID = FuBenManager.FindFuBenSeqIDByRoleID(client.ClientData.RoleID);

            FuBenTongGuanData fubenTongGuanData = null;

            bool bFirstPassWanMoTa = false;
            if (fuBenSeqID > 0) //如果副本不存在
            {
                FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(fuBenSeqID);
                if (null != fuBenInfoItem)
                {
                    fuBenInfoItem.EndTicks = TimeUtil.NOW();
                    int addFuBenNum = 1;
                    if (fuBenInfoItem.nDayOfYear != TimeUtil.NowDateTime().DayOfYear)
                    {
                        addFuBenNum = 0;
                    }

                    int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
                    if (fuBenID > 0)
                    {
                        if (WanMotaCopySceneManager.IsWanMoTaMapCode(client.ClientData.MapCode))
                        {
                            // 第一次通过万魔塔某层
                            if (!Global.FuBenPassed(client, fuBenID))
                            {
                                bFirstPassWanMoTa = true;
                            }
                        }

                        int usedSecs = (int)((fuBenInfoItem.EndTicks - fuBenInfoItem.StartTicks) / 1000);

                        // 更新玩家通关时间信息
                        Global.UpdateFuBenDataForQuickPassTimer(client, fuBenID, usedSecs, addFuBenNum);

                        // LogManager.WriteLog(LogTypes.Info, string.Format("万魔塔首次通关标记：{0}", bFirstPassWanMoTa));

                        // 给玩家物品
                        FuBenMapItem fuBenMapItem = FuBenManager.FindMapCodeByFuBenID(fuBenID, client.ClientData.MapCode);
                        fubenTongGuanData = GiveCopyMapGiftNoScore(client, fuBenMapItem, true);                        

                        GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDFUBENHISTDATA, string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID,
                                                        Global.FormatRoleName(client, client.ClientData.RoleName), fuBenID, usedSecs), null, client.ServerId);

                        // 万魔塔通关不计活跃 ChenXiaojun
                        //int nLev = -1;
                        //SystemXmlItem systemFuBenItem = null;
                        //if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(fuBenID, out systemFuBenItem))
                        //{
                        //    nLev = systemFuBenItem.GetIntValue("FuBenLevel");
                        //}                        
                        //GameManager.ClientMgr.UpdateRoleDailyData_FuBenNum(client, 1, nLev);

                        //副本通关
                        //Global.BroadcastFuBenOk(client, usedSecs, fuBenID);

                    }
                }
            }

            GameManager.ClientMgr.NotifyAllFuBenBeginInfo(client, !anyAlive);
            if (fubenTongGuanData != null && bFirstPassWanMoTa)
            {
                //发送奖励到客户端
                TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<FuBenTongGuanData>(fubenTongGuanData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_FUBENPASSNOTIFY);

                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket)) { ; }
            }
        }
    }
}
