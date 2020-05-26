using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using System.Xml.Linq;
using Server.Tools;
using GameServer.Server;
using GameServer.Core.Executor;

namespace GameServer.Logic.ActivityNew
{
    public class IPointsExchgData : AwardItem
    {
        /// <summary>
        /// 一个角色每天最多兑换的个数
        /// </summary>
        public int DayMaxTimes = 0;
    }

    /// <summary>
    /// 充值点兑换缓存数据
    /// </summary>
    public class JieriIPointsExchgActivity : Activity
    {
        // 充值点兑换奖励
        // ID vs AwardItem
        protected Dictionary<int, IPointsExchgData> AwardItemDict = new Dictionary<int, IPointsExchgData>();

        // 响应充值
        public void OnMoneyChargeEvent(string userid, int roleid, int addMoney)
        {
            // 是否在活动时间内
            if (!InActivityTime())
                return;

            // 根据转换比增加充值点
            string strYuanbaoToIPoints = GameManager.systemParamsList.GetParamValueByName("JieRiChongZhiDuiHuan");
            if (string.IsNullOrEmpty(strYuanbaoToIPoints))
                return;

            string[] strFieldsMtoIPoint = strYuanbaoToIPoints.Split(':');    // (钻石数：充值点)
            if (strFieldsMtoIPoint.Length != 2)
                return;

            int DivIPoints = Convert.ToInt32(strFieldsMtoIPoint[0]);
            if (DivIPoints == 0)
                return;

            // 转换率
            double YuanbaoToIPointsDiv = Convert.ToDouble(strFieldsMtoIPoint[1]) / DivIPoints;
            int IPointsAdd = (int)(YuanbaoToIPointsDiv * Global.TransMoneyToYuanBao(addMoney));

            // 增加充值点数
            string strcmd = string.Format("{0}:{1}:{2}:{3}", roleid, IPointsAdd, FromDate.Replace(':', '$'), ToDate.Replace(':', '$'));
            Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_INPUTPOINTS, strcmd, GameManager.LocalServerId);
        }

        //  检查各各种条件
        public override bool CheckCondition(GameClient client, int extTag)
        {
            IPointsExchgData ipointsExchgData;
            if (!AwardItemDict.TryGetValue(extTag, out ipointsExchgData))
            {
                return false;
            }
            
            // 是否还有领取次数
            if (GetIPointsLeftMergeNum(client, extTag) <= 0)
            {
                return false;
            }

            // 看充值点还够不够
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, FromDate.Replace(':', '$'), ToDate.Replace(':', '$'));
            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_SPR_GETINPUT_POINTS_EXCHGINFO, strcmd, client.ServerId);
            if (null == fields || fields.Length < 2)
            {
                return false;
            }

            if (Convert.ToInt32(fields[1]) < ipointsExchgData.MinAwardCondionValue)
            {
                return false;
            }

            return true;
        }

        // 给客户端同步充值积分相关数据
        public void NotifyInputPointsInfo(GameClient client, bool bPointsOnly = false)
        {
            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, FromDate.Replace(':', '$'), ToDate.Replace(':', '$'));
            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_SPR_GETINPUT_POINTS_EXCHGINFO, strcmd, client.ServerId);
            if (null == fields || fields.Length < 2)
                return;

            string cmdDataDB = fields[0] + ':' + fields[1];
            if(true == bPointsOnly)
            {
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_SYNCINPUT_POINTS_ONLY, cmdDataDB);
            }
            else
            {
                string cmdDataClient = "";
                BuildInputPointsDataCmdForClient(client, cmdDataDB, out cmdDataClient);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_GETINPUT_POINTS_EXCHGINFO, cmdDataClient);
            }
        }

        // 给奖励
        public override bool GiveAward(GameClient client, Int32 _params)
        {
            IPointsExchgData ipointsExchgData;
            if (!AwardItemDict.TryGetValue(_params, out ipointsExchgData))
            {
                return false;
            }

            int retInputPoints = 0;

            //扣除充值的积分
            if (ipointsExchgData.MinAwardCondionValue > 0)
            {
                int InputPointsCost = -ipointsExchgData.MinAwardCondionValue;
                string strcmd = string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, InputPointsCost, FromDate.Replace(':', '$'), ToDate.Replace(':', '$'));
                string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_INPUTPOINTS, strcmd, client.ServerId);
                if (null == fields || fields.Length < 2)
                {
                    return false;
                }

                retInputPoints = Convert.ToInt32(fields[1]);
                if (retInputPoints < 0)
                {
                    return false;
                }
            }

            // 增加领取次数
            ModifyIPointsLeftMergeNum(client, _params);
            
            // 给奖励
            GiveAward(client, ipointsExchgData);
            
            // 同步充值点积分
            NotifyInputPointsInfo(client, true);

            // 充值改变时，刷新与充值相关图标状态
            client._IconStateMgr.CheckJieRiActivity(client, false);
            client._IconStateMgr.SendIconStateToClient(client);

            return true;
        }

        public bool Init()
        {
            try
            {
                string fileName = "Config/JieRiGifts/ChongZhiDuiHuan.xml";
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
                if (null == xml) return false;

                XElement args = xml.Element("Activities");
                if (null != args)
                {
                    FromDate = Global.GetSafeAttributeStr(args, "FromDate");
                    ToDate = Global.GetSafeAttributeStr(args, "ToDate");
                    ActivityType = (int)Global.GetSafeAttributeLong(args, "ActivityType");

                    AwardStartDate = Global.GetSafeAttributeStr(args, "AwardStartDate");
                    AwardEndDate = Global.GetSafeAttributeStr(args, "AwardEndDate");
                }

                args = xml.Element("GiftList");

                if (null != args)
                {
                    IEnumerable<XElement> xmlItems = args.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null != xmlItem)
                        {
                            IPointsExchgData myAwardItem = new IPointsExchgData();
                            int id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                            myAwardItem.MinAwardCondionValue = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "NeedChongZhiDianShu"));
                            myAwardItem.AwardYuanBao = 0;
                            
                            // 最大兑换次数
                            myAwardItem.DayMaxTimes = (int)Global.GetSafeAttributeLong(xmlItem, "MaxNum");

                            string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "NewGoodsID");
                            if (string.IsNullOrEmpty(goodsIDs))
                            {
                                LogManager.WriteLog(LogTypes.Warning, string.Format("读取大型节日充值点兑换活动配置文件中的物品配置项1失败"));
                            }
                            else
                            {
                                string[] fields = goodsIDs.Split('|');
                                if (fields.Length <= 0)
                                {
                                    LogManager.WriteLog(LogTypes.Warning, string.Format("解析大型节日充值点兑换活动配置文件中的物品配置项1失败"));
                                }
                                else
                                {
                                    //将物品字符串列表解析成物品数据列表
                                    myAwardItem.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, "大型节日节日充值点兑换配置1");
                                }
                            }
                            AwardItemDict[id] = myAwardItem;
                        }
                    }
                }

                PredealDateTime();
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", "Config/JieRiGifts/ChongZhiDuiHuan.xml", ex.Message));
                return false;
            }

            return true;
        }

        public override bool HasEnoughBagSpaceForAwardGoods(GameClient client, Int32 id)
        {
            IPointsExchgData allItem = null;
            AwardItemDict.TryGetValue(id, out allItem);
 
            int awardCnt = 0;
            if (allItem != null && allItem.GoodsDataList != null)
            {
                awardCnt += allItem.GoodsDataList.Count;
            }

            return Global.CanAddGoodsNum(client, awardCnt);
        }

        public bool CanGetAnyAward(GameClient client)
        {
            if (client == null) return false;
            if (!InAwardTime()) return false;

            string[] dbFields = null;
            // 向DB申请数据
            string strDbCmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, FromDate.Replace(':', '$'), ToDate.Replace(':', '$'));
            TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                (int)TCPGameServerCmds.CMD_SPR_GETINPUT_POINTS_EXCHGINFO, strDbCmd, out dbFields, client.ServerId);
            
            if (null == dbFields)
                return false;

            if (null == dbFields || 2 != dbFields.Length)
                return false;
            
            int InputPoints = Convert.ToInt32(dbFields[1]);
            if( InputPoints <= 0)
                return false;

            foreach (var kvp in AwardItemDict)
            {
                int awardid = kvp.Key;
                IPointsExchgData item = kvp.Value;

                if (item.MinAwardCondionValue <= InputPoints && GetIPointsLeftMergeNum(client, kvp.Key) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        #region 充值点兑换的数量控制

        public void BuildInputPointsDataCmdForClient(GameClient client, string strCmdDB, out string strCmdClient)
        {
            strCmdClient = strCmdDB;

            // 检查返回消息
            string[] dbFields = strCmdDB.Split(':');
            if (null == dbFields)
                return;

            if (null == dbFields || 2 != dbFields.Length)
                return;

            // 活动结束
            if (!InActivityTime())
            {
                strCmdClient = null;
                strCmdClient += dbFields[0];
                strCmdClient += ':';
                strCmdClient += '0'; // points

                strCmdClient += ':';
                foreach (var kvp in AwardItemDict)
                {
                    strCmdClient += Convert.ToString(kvp.Value.DayMaxTimes);
                    strCmdClient += "|";
                }
            }
            else
            {
                strCmdClient += ":";
                foreach (var kvp in AwardItemDict)
                {
                    strCmdClient += GetIPointsLeftMergeNum(client, kvp.Key);
                    strCmdClient += '|';
                }
            }
        }
        
        /// <summary>
        /// 获取角色活动期间充值点兑换的剩余
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetIPointsLeftMergeNum(GameClient client, int index)
        {
            JieriIPointsExchgActivity instance = HuodongCachingMgr.GetJieriIPointsExchgActivity();
            if (null == instance)
                return 0;

            IPointsExchgData ExchgData = null;
            AwardItemDict.TryGetValue(index, out ExchgData);
            if (ExchgData == null)
                return 0;

            // 每次活动刷新一次
            DateTime startTime = DateTime.Parse(FromDate);
            int currday = Global.GetOffsetDay(startTime);

            int lastday = 0;
            int count = 0;

            string strFlag = RoleParamName.InputPointExchargeFlag + index;
            String JieRiIPointExchgFlag = Global.GetRoleParamByName(client, strFlag);

            // day:count
            if (null != JieRiIPointExchgFlag)
            {
                string[] fields = JieRiIPointExchgFlag.Split(',');
                if (2 == fields.Length)
                {
                    lastday = Convert.ToInt32(fields[0]);
                    count = Convert.ToInt32(fields[1]);
                }
            }

            if (currday == lastday)
            {
                return (ExchgData.DayMaxTimes - count);
            }

            return ExchgData.DayMaxTimes;
        }

        /// <summary>
        /// 修改活动期间充值点兑换的数量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="addNum"></param>
        public int ModifyIPointsLeftMergeNum(GameClient client, int index, int addNum = 1)
        {
            DateTime startTime = DateTime.Parse(FromDate);
            int currday = Global.GetOffsetDay(startTime);

            string strFlag = RoleParamName.InputPointExchargeFlag + index;
            String JieRiIPointExchgFlag = Global.GetRoleParamByName(client, strFlag);

            int lastday = 0;
            int count = 0;
            if (null != JieRiIPointExchgFlag)
            {
                // day:count
                string[] fields = JieRiIPointExchgFlag.Split(',');
                if (2 != fields.Length)
                    return 0;

                lastday = Convert.ToInt32(fields[0]);
                count = Convert.ToInt32(fields[1]);
            }

            if (currday == lastday)
            {
                count += addNum;
            }
            else
            {
                lastday = currday;
                count = addNum;
            }

            string result = string.Format("{0},{1}", lastday, count);
            Global.SaveRoleParamsStringToDB(client, strFlag, result, true);
            return count;
        }
        #endregion 充值点兑换的数量控制

    }
}

