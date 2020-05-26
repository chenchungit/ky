using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.MUWings;
using GameServer.Logic;
using GameServer.Logic.Goods;
using GameServer.Logic.ActivityNew.SevenDay;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// 翅膀进阶指令
    /// </summary>
    public class WingUpgradeCmdProcessor : ICmdProcessor
    {
        private static WingUpgradeCmdProcessor instance = new WingUpgradeCmdProcessor();

        private WingUpgradeCmdProcessor() 
        {
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_WINGUPGRADE, 2, this);
        }

        public static WingUpgradeCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(Logic.GameClient client, string[] cmdParams)
        {
            int nID = (int)TCPGameServerCmds.CMD_SPR_WINGUPGRADE;
            int nRoleID = Global.SafeConvertToInt32(cmdParams[0]);
            int nUpWingMode = Global.SafeConvertToInt32(cmdParams[1]); //0: 道具进阶, 1: 钻石进阶

            string strCmd = "";
            if (null == client.ClientData.MyWingData)
            {
                strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                client.sendCmd(nID, strCmd);
                return true;
            }

            // 已到最高级
            if (client.ClientData.MyWingData.WingID >= MUWingsManager.MaxWingID)
            {
                strCmd = string.Format("{0}:{1}:{2}:{3}", -8, nRoleID, 0, 0);
                client.sendCmd(nID, strCmd);
                return true;
            }

            // 获取进阶信息表
            SystemXmlItem upStarXmlItem = MUWingsManager.GetWingUPCacheItem(client.ClientData.MyWingData.WingID + 1);
            if (null == upStarXmlItem)
            {
                strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                client.sendCmd(nID, strCmd);
                return true;
            }

            if (0 == nUpWingMode)
            {
                // 获取进阶需要的道具的物品信息
                string strReqItemID = upStarXmlItem.GetStringValue("NeedGoods");

                // 解析物品ID与数量
                string[] itemParams = strReqItemID.Split(',');
                if (null == itemParams || itemParams.Length != 2)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                    client.sendCmd(nID, strCmd);
                    return true;
                }

                int originGoodsID = Convert.ToInt32(itemParams[0]);
                int originGoodsNum = Convert.ToInt32(itemParams[1]);
                if (originGoodsID <= 0 || originGoodsNum <= 0)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                    client.sendCmd(nID, strCmd);
                    return true;
                }

                GoodsReplaceResult replaceRet = GoodsReplaceManager.Instance().GetReplaceResult(client, originGoodsID);
                if (replaceRet == null || replaceRet.TotalGoodsCnt() < originGoodsNum)
                {
                    // 物品数量不够
                    strCmd = string.Format("{0}:{1}:{2}:{3}", -4, nRoleID, 0, 0);
                    client.sendCmd(nID, strCmd);
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
                        strCmd = string.Format("{0}:{1}:{2}:{3}", -5, nRoleID, 0, 0);
                        client.sendCmd(nID, strCmd);
                        return true;
                    }
                }
            }
            else
            {
                // 获取进阶需要的钻石数量
                int nReqZuanShi = upStarXmlItem.GetIntValue("NeedZuanShi");
                if (nReqZuanShi <= 0)
                {
                    strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                    client.sendCmd(nID, strCmd);
                    return true;
                }
                // 判断用户点卷额是否不足【钻石】
                if (client.ClientData.UserMoney < nReqZuanShi)
                {
                    //用户点卷额不足【钻石】
                    strCmd = string.Format("{0}:{1}:{2}:{3}", -6, nRoleID, 0, 0);
                    client.sendCmd(nID, strCmd);
                    return true;
                }

                //先DBServer请求扣费
                //扣除用户点卷
                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nReqZuanShi, "翅膀进阶"))
                {
                    //扣除用户点卷失败
                    strCmd = string.Format("{0}:{1}:{2}:{3}", -7, nRoleID, 0, 0);
                    client.sendCmd(nID, strCmd);
                    return true;
                }
            }

            int nLuckOne = upStarXmlItem.GetIntValue("LuckyOne");
            int nLuckyTwo = upStarXmlItem.GetIntValue("LuckyTwo");
            int nLuckTwoRate = (int)(upStarXmlItem.GetDoubleValue("LuckyTwoRate") * 100.0);

            int nNextWingID = client.ClientData.MyWingData.WingID;
            int nNextJinJieFailedNum = client.ClientData.MyWingData.JinJieFailedNum;
            int nNextStarLevel = client.ClientData.MyWingData.ForgeLevel;
            int nNextStarExp = client.ClientData.MyWingData.StarExp;


            // LuckyOne+提升获得幸运点 < LuckyTwo;必定不会提升成功
            if (nLuckOne + client.ClientData.MyWingData.JinJieFailedNum < nLuckyTwo)
            {
                // 幸运点加1
                nNextJinJieFailedNum++;
            }
            // LuckyOne+提升获得幸运点>= LuckyTwo,则根据配置得到LuckyTwoRate概率判定是否能够完成进阶操作
            else if (nLuckOne + client.ClientData.MyWingData.JinJieFailedNum < 110000)
            {
                // 
                int nRandNum = Global.GetRandomNumber(0, 100);
                if (nRandNum < nLuckTwoRate)
                {
                    // 进阶成功
                    nNextWingID++;

                    // 幸运点清0
                    nNextJinJieFailedNum = 0;

                    // 星级清0
                    nNextStarLevel = 0;

                    // 星级经验清0
                    nNextStarExp = 0;
                }
                else
                {
                    // 幸运点加1
                    nNextJinJieFailedNum++;
                }
            }
            // LuckyOne+提升获得幸运点>=110000时，进阶必定成功
            else
            {
                // 进阶成功
                nNextWingID++;

                // 幸运点清0
                nNextJinJieFailedNum = 0;
              
                // 星级清0
                nNextStarLevel = 0;

                // 星级经验清0
                nNextStarExp = 0;
            }

            // 七日活动
            // 翅膀升级和升阶次数，成功失败都算
            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.WingSuitStarTimes));

            // 将改变保存到数据库
            int iRet = MUWingsManager.WingUpDBCommand(client, client.ClientData.MyWingData.DbID, nNextWingID, nNextJinJieFailedNum, nNextStarLevel, nNextStarExp, client.ClientData.MyWingData.ZhuLingNum, client.ClientData.MyWingData.ZhuHunNum);
            if (iRet < 0)
            {
                strCmd = string.Format("{0}:{1}:{2}:{3}", -3, nRoleID, 0, 0);
                client.sendCmd(nID, strCmd);
                return true;
            }
            else
            {
                strCmd = string.Format("{0}:{1}:{2}:{3}", 0, nRoleID, nNextWingID, nNextJinJieFailedNum);
                client.sendCmd(nID, strCmd);

                client.ClientData.MyWingData.JinJieFailedNum = nNextJinJieFailedNum;
                if (client.ClientData.MyWingData.WingID != nNextWingID)
                {
                    // 先移除原来的属性
                    if (1 == client.ClientData.MyWingData.Using)
                    {
                        MUWingsManager.UpdateWingDataProps(client, false);
                    }

                    bool oldWingLingYuOpened = GlobalNew.IsGongNengOpened(client, GongNengIDs.WingLingYu);

                    // 更新翅膀的新等级，星数
                    client.ClientData.MyWingData.WingID = nNextWingID;
                    client.ClientData.MyWingData.ForgeLevel = 0;
                    client.ClientData.MyWingData.StarExp = 0;

                    // 七日活动
                    GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.WingLevel));

                    bool newWingLingYuOpened = GlobalNew.IsGongNengOpened(client, GongNengIDs.WingLingYu);
                    if (!oldWingLingYuOpened && newWingLingYuOpened)
                    {
                        //翎羽功能开启了，初始化
                        LingYuManager.InitAsOpened(client);
                    }

                    // 按新等级增加属性
                    if (1 == client.ClientData.MyWingData.Using)
                    {
                        MUWingsManager.UpdateWingDataProps(client, true);
                        //升阶时候，翅膀的基础属性的改变会影响注魂加成
                        ZhuLingZhuHunManager.UpdateZhuLingZhuHunProps(client);

                        // 通知客户端属性变化
                        GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                        // 总生命值和魔法值变化通知(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                    }

                    //[bing] 刷新客户端活动叹号
                    if (client._IconStateMgr.CheckJieRiFanLi(client, ActivityTypes.JieriWing))
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
