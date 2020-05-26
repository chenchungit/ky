using System;
using GameServer.Logic;
using GameServer.Logic.MUWings;
using GameServer.Logic.Goods;
using System.Collections.Generic;
using Server.Data;
using GameServer.Logic.ActivityNew.SevenDay;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using Tmsk.Contract;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 翅膀升星
    /// </summary>
    public class WingUpStarCmdProcessor : ICmdProcessor
    {
        private static WingUpStarCmdProcessor instance = new WingUpStarCmdProcessor();

        private WingUpStarCmdProcessor() 
        {
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, 2, this);
        }

        public static WingUpStarCmdProcessor getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 翅膀升星处理
        /// </summary>
        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            //int nID = (int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR;
            int nRoleID = Global.SafeConvertToInt32(cmdParams[0]);
            int nUpStarMode = Global.SafeConvertToInt32(cmdParams[1]); //0: 道具升星, 1: 钻石升星

            //string strCmd = "";
            SCWingStarUp scData = null;

            if (null == client.ClientData.MyWingData)
            {
                //strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                //client.sendCmd(nID, strCmd);

                scData = new SCWingStarUp(-3, nRoleID, 0, 0);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                return true;
            }

            // 获取升星信息表
            SystemXmlItem upStarXmlItem = WingStarCacheManager.GetWingStarCacheItem(Global.CalcOriginalOccupationID(client), client.ClientData.MyWingData.WingID, client.ClientData.MyWingData.ForgeLevel + 1);
            if (null == upStarXmlItem)
            {
                //strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                //client.sendCmd(nID, strCmd);

                scData = new SCWingStarUp(StdErrorCode.Error_Level_Reach_Max_Level, nRoleID, 0, 0);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                return true;
            }

            string strWingShengXing = GameManager.systemParamsList.GetParamValueByName("WingShengXing");
            if ("" == strWingShengXing)
            {
                //strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                //client.sendCmd(nID, strCmd);

                scData = new SCWingStarUp(-3, nRoleID, 0, 0);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                return true;
            }

            // 解析升级暴率
            string[] wingShengXing = strWingShengXing.Split(',');
            if (3 != wingShengXing.Length)
            {
                //strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                //client.sendCmd(nID, strCmd);
                scData = new SCWingStarUp(-3, nRoleID, 0, 0);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                return true;
            }

            // 暴率
            int nPowRate = 0;
            // 增加的星经验
            int nAddExp = 0;
            if (0 == nUpStarMode)
            {
                nPowRate = (int)(Convert.ToDouble(wingShengXing[0]) * 100.0);
                nAddExp = Convert.ToInt32(upStarXmlItem.GetIntValue("GoodsExp"));
            }
            else
            {
                nPowRate = (int)(Convert.ToDouble(wingShengXing[1]) * 100.0);
                nAddExp = Convert.ToInt32(upStarXmlItem.GetIntValue("ZuanShiExp"));
            }

            // 暴升，乘以经验倍数
            int nRandNum = Global.GetRandomNumber(0, 100);
            if (nRandNum < nPowRate)
            {
                nAddExp *= Convert.ToInt32(wingShengXing[2]);
            }

            int nUpStarReqExp = upStarXmlItem.GetIntValue("StarExp");
            int nNextStarLevel = client.ClientData.MyWingData.ForgeLevel;
            int nNextStarExp = 0;
            if (client.ClientData.MyWingData.StarExp + nAddExp >= nUpStarReqExp)
            {
                if (nNextStarLevel < MUWingsManager.MaxWingEnchanceLevel)
                {
                    // 改变星级，剩余的经验改到下级
                    nNextStarLevel += 1;
                    nNextStarExp = client.ClientData.MyWingData.StarExp + nAddExp - nUpStarReqExp;
                    
                    // 连续升星
                    while (true)
                    {
                        if (nNextStarLevel >= MUWingsManager.MaxWingEnchanceLevel)
                        {
                            break;
                        }

                        SystemXmlItem nextStarXmlItem = WingStarCacheManager.GetWingStarCacheItem(Global.CalcOriginalOccupationID(client), client.ClientData.MyWingData.WingID, nNextStarLevel + 1);
                        if (null == upStarXmlItem)
                        {
                            break;
                        }

                        int nNextUpStarReqExp = nextStarXmlItem.GetIntValue("StarExp");
                        if (nNextStarExp >= nNextUpStarReqExp)
                        {
                            nNextStarLevel += 1;
                            nNextStarExp = nNextStarExp - nNextUpStarReqExp;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    // 已经达到最高星
                    nNextStarExp = nUpStarReqExp;
                }
            }
            else
            {
                nNextStarExp = client.ClientData.MyWingData.StarExp + nAddExp;
            }

            if (0 == nUpStarMode)
            {
                //获取强化需要的道具的物品ID
                string strReqItemID = upStarXmlItem.GetStringValue("NeedGoods");

                // 解析物品ID与数量
                string[] itemParams = strReqItemID.Split(',');
                if (null == itemParams || itemParams.Length != 2)
                {
                    //strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                    //client.sendCmd(nID, strCmd);

                    scData = new SCWingStarUp(-3, nRoleID, 0, 0);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                    return true;
                }

                int originGoodsID = Convert.ToInt32(itemParams[0]);
                int originGoodsNum = Convert.ToInt32(itemParams[1]);
                if (originGoodsID <= 0 || originGoodsNum <= 0)
                {
                    //strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                    //client.sendCmd(nID, strCmd);

                    scData = new SCWingStarUp(-3, nRoleID, 0, 0);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                    return true;
                }

                GoodsReplaceResult replaceRet = GoodsReplaceManager.Instance().GetReplaceResult(client, originGoodsID);
                if (replaceRet == null || replaceRet.TotalGoodsCnt() < originGoodsNum)
                {
                    // 物品数量不够
                    //strCmd = string.Format("{0}:{1}:{2}:{3}", -4, nRoleID, 0, 0);
                    //client.sendCmd(nID, strCmd);

                    scData = new SCWingStarUp(-4, nRoleID, 0, 0);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                    return true;
                }

                List<GoodsReplaceResult.ReplaceItem> realCostList = new List<GoodsReplaceResult.ReplaceItem>();
                // 1：替换后的绑定材料
                // 2：替换后的非绑定材料
                // 3：原始绑定材料
                // 4：原始非绑定材料
                realCostList.AddRange(replaceRet.BindList);
                realCostList.AddRange(replaceRet.UnBindList);
                realCostList.Add(replaceRet.OriginBindGoods);
                realCostList.Add(replaceRet.OriginUnBindGoods);

                int hasUseCnt = 0;
                int stillNeedCnt = originGoodsNum;
                foreach (var item in realCostList)
                {
                    if (item.GoodsCnt <= 0) continue;

                    int realCostCnt = Math.Min(stillNeedCnt, item.GoodsCnt);
                    if (realCostCnt <= 0) break;

                    bool bUsedBinding = false;
                    bool bUsedTimeLimited = false;
                    bool bFailed = false;
                    if (item.IsBind)
                    {
                        if (!GameManager.ClientMgr.NotifyUseBindGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                            client, item.GoodsID, realCostCnt, false, out bUsedBinding, out bUsedTimeLimited))
                        {
                            bFailed = true;
                        }
                    }
                    else
                    {
                        if (!GameManager.ClientMgr.NotifyUseNotBindGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                client, item.GoodsID, realCostCnt, false, out bUsedBinding, out bUsedTimeLimited))
                        {
                            bFailed = true;
                        }
                    }

                    stillNeedCnt -= realCostCnt;

                    if (bFailed)
                    {
                        //strCmd = string.Format("{0}:{1}:{2}:{3}", -5, nRoleID, 0, 0);
                        //client.sendCmd(nID, strCmd);

                        scData = new SCWingStarUp(-5, nRoleID, 0, 0);
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                        return true;
                    }
                }
            }
            else
            {
                // 获取强化需要的钻石数量
                int nReqZuanShi = upStarXmlItem.GetIntValue("NeedZuanShi");
                if (nReqZuanShi <= 0)
                {
                    //strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                    //client.sendCmd(nID, strCmd);

                    scData = new SCWingStarUp(-3, nRoleID, 0, 0);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                    return true;
                }
                //判断用户点卷额是否不足【钻石】
                if (client.ClientData.UserMoney < nReqZuanShi)
                {
                    //用户点卷额不足【钻石】
                    //strCmd = string.Format("{0}:{1}:{2}:{3}", -6, nRoleID, 0, 0);
                    //client.sendCmd(nID, strCmd);

                    scData = new SCWingStarUp(-6, nRoleID, 0, 0);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                    return true;
                }

                //先DBServer请求扣费
                //扣除用户点卷
                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nReqZuanShi, "翅膀升星"))
                {
                    //扣除用户点卷失败
                    //strCmd = string.Format("{0}:{1}:{2}:{3}", -7, nRoleID, 0, 0);
                    //client.sendCmd(nID, strCmd);

                    scData = new SCWingStarUp(-7, nRoleID, 0, 0);
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                    return true;
                }
            }

            // 七日活动
            // 翅膀升级和升阶次数，成功失败都算
            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.WingSuitStarTimes));

            // 将改变保存到数据库
            int iRet = MUWingsManager.WingUpStarDBCommand(client, client.ClientData.MyWingData.DbID, nNextStarLevel, nNextStarExp);
            if (iRet < 0)
            {
                //strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                //client.sendCmd(nID, strCmd);

                scData = new SCWingStarUp(-3, nRoleID, 0, 0);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                return true;
            }
            else
            {
                //strCmd = string.Format("{0}:{1}:{2}:{3}", 0, nRoleID, nNextStarLevel, nNextStarExp);
                //client.sendCmd(nID, strCmd);

                scData = new SCWingStarUp(0, nRoleID, nNextStarLevel, nNextStarExp);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_WINGUPSTAR, scData);

                client.ClientData.MyWingData.StarExp = nNextStarExp;
                if (client.ClientData.MyWingData.ForgeLevel != nNextStarLevel)
                {
                    // 先移除原来的属性
                    if (1 == client.ClientData.MyWingData.Using)
                    {
                        MUWingsManager.UpdateWingDataProps(client, false);
                    }

                    bool oldWingLingYuOpened = GlobalNew.IsGongNengOpened(client, GongNengIDs.WingLingYu);

                    client.ClientData.MyWingData.ForgeLevel = nNextStarLevel;

                    // 七日活动
                    GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.WingLevel));

                    bool newWingLingYuOpened = GlobalNew.IsGongNengOpened(client, GongNengIDs.WingLingYu);
                    if (!oldWingLingYuOpened && newWingLingYuOpened)
                    {
                        // 翎羽功能开启了，执行初始化
                        LingYuManager.InitAsOpened(client);
                    }

                    // 按新等级增加属性
                    if (1 == client.ClientData.MyWingData.Using)
                    {
                        MUWingsManager.UpdateWingDataProps(client, true);
                        //升星时候，翅膀的基础属性的改变会影响注魂加成
                        ZhuLingZhuHunManager.UpdateZhuLingZhuHunProps(client);

                        // 通知客户端属性变化
                        GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                        // 总生命值和魔法值变化通知(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                    }

                    //[bing] 刷新客户端活动叹号
                    if (client._IconStateMgr.CheckJieRiFanLi(client, ActivityTypes.JieriWing)
                        || client._IconStateMgr.CheckSpecialActivity(client))
                    {
                        client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                        client._IconStateMgr.SendIconStateToClient(client);
                    }
                }
            }
            
            return true;
        }
    }
}
