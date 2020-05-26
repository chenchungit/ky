using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using System.Xml.Linq;
using Server.Tools;
using GameServer.Server;
using GameServer.Core.Executor;
using Tmsk.Contract;
using GameServer.Logic.TuJian;

namespace GameServer.Logic.ActivityNew
{
#region 原型数据定义
    /// <summary>
    /// 专属活动类型
    /// </summary>
    public enum SpecialActivityType
    {
        SAT_QiangGou = 1, // 限时抢购
        SAT_InputExchange = 2, // 充值兑换
        SAT_Consume = 3, // 累计消费
        SAT_DirectAward = 4, // 直接领取
        SAT_Level = 5, // 达到转生等级以上
        SAT_Wing = 6, // 达到翅膀等级以上
        SAT_Vip = 7, // 达到VIP等级以上
        SAT_ChengJiu = 8, // 达到成就等级以上
        SAT_JunXian = 9, // 达到军衔等级以上
        SAT_Merlin = 10, // 达到梅林之书等级以上
        SAT_ShengWu = 11, // 达到圣物总等级以上
        SAT_Ring = 12, // 达到婚戒等级以上
        SAT_ShouHuShen = 13, // 达到守护神等级以上
    }

    /// <summary>
    /// 专属活动开启配置文件SpecialActivityTime.xml
    /// </summary>
    public class SpecialActivityTimeConfig
    {
        // 组ID
        public int GroupID = 0;

        // 开服时间范围起点
        public DateTime ServerOpenFromDate;

        // 开服时间范围终点
        public DateTime ServerOpenToDate;

        // 活动开启时间
        public DateTime FromDate;

        // 活动关闭时间
        public DateTime ToDate;
    }

    /// <summary>
    /// 专属活动限制数据
    /// </summary>
    public class SpecActLimitData
    {
        // 一级限制
        public int MinFirst = -1;
        public int MaxFirst = -1;

        // 二级限制
        public int MinSecond = -1;
        public int MaxSecond = -1;

        public bool IfValid()
        {
            if (MinFirst <= 0 && MaxFirst <= 0 && MinSecond <= 0 && MaxSecond <= 0)
                return false;
            return true;
        }
    }

    // 专属活动价格数据
    public class SpecActPriceData
    {
        public int NumOne = -1;
        public int NumTwo = -1;
    }

    // 专属活动目标数
    public class SpecActGoalData
    {
        public int NumOne = 0;
        public int NumTwo = 0;

        public bool IsValid()
        {
            if (NumOne <= 0 && NumTwo <= 0)
                return false;
            return true;
        }
    }

    /// <summary>
    /// 专属活动SpecialActivity.xml，控制活动内容
    /// </summary>
    public class SpecialActivityConfig
    {
        // 活动ID
        public int ID = 0;

        // 组ID
        public int GroupID = 0;

        // 活动开启时间 解析Day字段获取
        public DateTime FromDay;

        // 活动关闭时间 解析Day字段获取
        public DateTime ToDay;

        // NeedLevel
        public SpecActLimitData LevLimit = new SpecActLimitData();

        // NeedVIP
        public SpecActLimitData VipLimit = new SpecActLimitData();

        // NeedChongZhi
        public SpecActLimitData ChongZhiLimit = new SpecActLimitData();

        // NeedWing
        public SpecActLimitData WingLimit = new SpecActLimitData();

        // NeedChengJiu
        public SpecActLimitData ChengJiuLimit = new SpecActLimitData();

        // NeedJunXian
        public SpecActLimitData JunXianLimit = new SpecActLimitData();

        // NeedMerlin
        public SpecActLimitData MerlinLimit = new SpecActLimitData();

        // NeedShengWu
        public SpecActLimitData ShengWuLimit = new SpecActLimitData();

        // NeedRing
        public SpecActLimitData RingLimit = new SpecActLimitData();

        // NeedShouHuShen
        public SpecActLimitData ShouHuShenLimit = new SpecActLimitData();

        // 活动类型
        public int Type = 0;

        // 达成条件
        public SpecActGoalData GoalData = new SpecActGoalData();

        /// 奖励物品列表
        public List<GoodsData> GoodsDataListOne = new List<GoodsData>();
        public List<GoodsData> GoodsDataListTwo = new List<GoodsData>();

        // 限时奖励参数
        public AwardEffectTimeItem GoodsDataListThr = new AwardEffectTimeItem();

        // 抢购价格
        public SpecActPriceData Price = new SpecActPriceData();

        // 抢购数
        public int PurchaseNum = -1;
    }
#endregion

    /// <summary>
    /// 专属活动
    /// </summary>
    public class SpecialActivity : Activity
    {
        // 配置文件路径相关
        protected const string SpecialChongZhiDuiHuan = "SpecialChongZhiDuiHuan";

        public const string SpecialActivityData_fileName = "Config/SpecialActivity/SpecialActivity.xml";

        public const string SpecialActivityTimeData_fileName = "Config/SpecialActivity/SpecialActivityTime.xml";

        // 专属活动开启配置文件SpecialActivityTime.xml
        protected Dictionary<int, SpecialActivityTimeConfig> SpecialActTimeDict = new Dictionary<int, SpecialActivityTimeConfig>();

        // 专属活动SpecialActivity.xml，控制活动内容
        protected Dictionary<int, SpecialActivityConfig> SpecialActDict = new Dictionary<int, SpecialActivityConfig>();

        // 响应充值 在线
        public void OnMoneyChargeEventOnLine(GameClient client, int addMoney)
        {
            string FromActDate = "";
            string ToActDate = "";

            int GroupID = GenerateSpecActGroupID();
            if (-1 == GroupID)
                return;

            SpecialActivityTimeConfig timeConfig = null;
            if (!SpecialActTimeDict.TryGetValue(GroupID, out timeConfig))
                return;

            FromActDate = timeConfig.FromDate.ToString();
            ToActDate = timeConfig.ToDate.ToString();


#if false
            // 检查角色身上是否有激活状态的充值兑换活动
            foreach (var kvp in client.ClientData.SpecActInfoDict)
            {
                SpecActInfoDB myData = kvp.Value;
                if(myData.Active == false)
                    continue;

                SpecialActivityConfig actProto = null;
                if(!SpecialActDict.TryGetValue(myData.ActID, out actProto))
                    continue;

                if(actProto.Type == (int)SpecialActivityType.SAT_InputExchange)
                {
                    FromActDate = actProto.FromDay.ToString();
                    ToActDate = actProto.ToDay.ToString();
                    break;
                }
            }
#endif
            if(!string.IsNullOrEmpty(FromActDate) && !string.IsNullOrEmpty(ToActDate))
                OnMoneyChargeEvent(client.strUserID, client.ClientData.RoleID, addMoney, FromActDate, ToActDate);
        }

        // 响应充值 离线
        public void OnMoneyChargeEventOffLine(string userid, int roleid, int addMoney)
        {
            string FromActDate = "";
            string ToActDate = "";

            int GroupID = GenerateSpecActGroupID();
            SpecialActivityTimeConfig timeConfig = null;
            if (!SpecialActTimeDict.TryGetValue(GroupID, out timeConfig))
                return;

            FromActDate = timeConfig.FromDate.ToString();
            ToActDate = timeConfig.ToDate.ToString();
#if false
            // CMD_DB_GET_SPECACTINFO
            Dictionary<int, SpecActInfoDB> SpecActInfoDict = Global.sendToDB<Dictionary<int, SpecActInfoDB>, string>((int)TCPGameServerCmds.CMD_DB_GET_SPECACTINFO, 
                string.Format("{0}", roleid), GameManager.LocalServerId);
            if (null == SpecActInfoDict)
                return;

            // 检查角色身上是否有激活状态的充值兑换活动
            foreach (var kvp in SpecActInfoDict)
            {
                SpecActInfoDB myData = kvp.Value;
                if (myData.Active == false)
                    continue;

                SpecialActivityConfig actProto = null;
                if (!SpecialActDict.TryGetValue(myData.ActID, out actProto))
                    continue;

                if (actProto.Type == (int)SpecialActivityType.SAT_InputExchange)
                {
                    FromActDate = actProto.FromDay.ToString();
                    ToActDate = actProto.ToDay.ToString();
                    break;
                }
            }
#endif
            OnMoneyChargeEvent(userid, roleid, addMoney, FromActDate, ToActDate);
        }

        // 响应充值
        protected void OnMoneyChargeEvent(string userid, int roleid, int addMoney, string FromActDate, string ToActDate)
        {
            // 根据转换比增加积分
            string strYuanbaoToJiFen = GameManager.systemParamsList.GetParamValueByName("SpecialChongZhiDuiHuan");
            if (string.IsNullOrEmpty(strYuanbaoToJiFen))
                return;

            string[] strFieldsMtoJiFen = strYuanbaoToJiFen.Split(':');    // (钻石数：积分)
            if (strFieldsMtoJiFen.Length != 2)
                return;

            int DivJiFen = Convert.ToInt32(strFieldsMtoJiFen[0]);
            if (DivJiFen == 0)
                return;

            // 转换率
            double YuanbaoToJiFenDiv = Convert.ToDouble(strFieldsMtoJiFen[1]) / DivJiFen;
            int JiFenAdd = (int)(YuanbaoToJiFenDiv * Global.TransMoneyToYuanBao(addMoney));

            // 增加专属活动充值积分
            string strcmd = string.Format("{0}:{1}:{2}:{3}", roleid, JiFenAdd, FromActDate.Replace(':', '$'), ToActDate.Replace(':', '$'));
            Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_SPECJIFEN, strcmd, GameManager.LocalServerId);
        }

        // 消费
        public void MoneyConst(GameClient client, int moneyCost)
        {
            if (client.ClientData.SpecActInfoDict == null || client.ClientData.SpecActInfoDict.Count == 0)
                return;

            // 检查角色身上是否有激活状态的累计消费
            foreach (var kvp in client.ClientData.SpecActInfoDict)
            {
                SpecActInfoDB myData = kvp.Value;
                if (myData.Active == 0)
                    continue;

                SpecialActivityConfig actProto = null;
                if (!SpecialActDict.TryGetValue(myData.ActID, out actProto))
                    continue;

                if (actProto.Type == (int)SpecialActivityType.SAT_Consume)
                {
                    myData.CountNum += moneyCost;
                    UpdateClientSpecActData(client, myData);
                }
            }

            if (client._IconStateMgr.CheckSpecialActivity(client))
                client._IconStateMgr.SendIconStateToClient(client);
        }

        // 感叹号检查
        public bool CheckIconState(GameClient client)
        {
            bool bFlush = false;
            if (client.ClientData.SpecActInfoDict == null || client.ClientData.SpecActInfoDict.Count == 0)
                return bFlush;

            foreach (var kvp in client.ClientData.SpecActInfoDict)
            {
                SpecActInfoDB myActInfoDB = kvp.Value;
                int ErrCode = SpecActCheckCondition(client, kvp.Key, false);
                if(ErrCode == StdErrorCode.Error_Success_No_Info)
                {
                    bFlush = true;
                    break;
                }
            }
            return bFlush;
        }
      
        // 处理角色上线、跨天
        public void OnRoleLogin(GameClient client, bool isLogin)
        {          
            // 尝试生成专享活动
            GenerateSpecActGroup(client);

            // 开启 or 关闭
            NotifyActivityState(client);
        }

        // 构造一个对象给客户端查询用
        public SpecialActivityData GetSpecialActivityDataForClient(GameClient client)
        {
            SpecialActivityData mySpecActData = new SpecialActivityData();
            mySpecActData.GroupID = GetClientSpecActGroupID(client);
            mySpecActData.SpecActInfoList = new List<SpecActInfo>();

            foreach(var kvp in client.ClientData.SpecActInfoDict)
            {
                SpecActInfoDB mySaveData = kvp.Value;
                if (mySaveData.Active == 0)
                    continue;

                SpecialActivityConfig myActConfig = null;
                if(!SpecialActDict.TryGetValue(mySaveData.ActID, out myActConfig))
                    continue;

                SpecActInfo ActInfo = new SpecActInfo();
                ActInfo.ActID = mySaveData.ActID;

                // 目标数据
                SpecActGoalData CurGoalNum = GetCurrentGoalNum(client, mySaveData, myActConfig);
                ActInfo.ShowNum = CurGoalNum.NumOne; // 
                ActInfo.ShowNum2 = CurGoalNum.NumTwo;

                // 按钮状态
                if (myActConfig.PurchaseNum == -1)
                {
                    ActInfo.State = (mySaveData.PurNum == 1) ? 1 : 0;
                }
                else
                {
                    ActInfo.LeftPurNum = myActConfig.PurchaseNum - mySaveData.PurNum;
                    ActInfo.State = (ActInfo.LeftPurNum <= 0) ? 1 : 0;
                    if (ActInfo.LeftPurNum < 0) // limit for client
                        ActInfo.LeftPurNum = 0;
                }

                // 条件是否达成
                if (myActConfig.GoalData.IsValid())
                {
                    if (CurGoalNum.NumOne < myActConfig.GoalData.NumOne ||
                        (CurGoalNum.NumOne == myActConfig.GoalData.NumOne && CurGoalNum.NumTwo < myActConfig.GoalData.NumTwo))
                        ActInfo.State = -1;  // 未达成
                }
                mySpecActData.SpecActInfoList.Add(ActInfo);
            }
            return mySpecActData;
        }

        // 检查各种条件
        public int SpecActCheckCondition(GameClient client, int ActID, bool CheckCost = true)
        {
            SpecActInfoDB mySaveData = null;
            if(!client.ClientData.SpecActInfoDict.TryGetValue(ActID, out mySaveData))
                return StdErrorCode.Error_Invalid_Index;

            if (mySaveData.Active == 0)
                return StdErrorCode.Error_Invalid_Index;

            SpecialActivityConfig myActConfig = null;
            if (!SpecialActDict.TryGetValue(mySaveData.ActID, out myActConfig))
                return StdErrorCode.Error_Invalid_Index;

            DateTime nowDateTm = TimeUtil.NowDateTime();
            if (nowDateTm < myActConfig.FromDay || nowDateTm > myActConfig.ToDay)
                return StdErrorCode.Error_Not_In_valid_Time;

            SpecActGoalData CurGoalNum = GetCurrentGoalNum(client, mySaveData, myActConfig);
            if (myActConfig.GoalData.IsValid())
            {
                if (CurGoalNum.NumOne < myActConfig.GoalData.NumOne || 
                    (CurGoalNum.NumOne == myActConfig.GoalData.NumOne && CurGoalNum.NumTwo < myActConfig.GoalData.NumTwo))
                    return StdErrorCode.Error_Operation_Denied;
            }

            if (myActConfig.PurchaseNum == -1)
            {
                if (mySaveData.PurNum == 1) // 已领取
                    return StdErrorCode.Error_Has_Get;
            }
            else
            {
                // 已领完
                if (myActConfig.PurchaseNum - mySaveData.PurNum <= 0)
                    return StdErrorCode.Error_Has_Get;
            }

            // 如果是充值兑换活动检查充值积分是否够
            if (myActConfig.Type == (int)SpecialActivityType.SAT_InputExchange)
            {
                if (GetCurrentSpecActJiFen(client, myActConfig) < myActConfig.Price.NumOne)
                    return StdErrorCode.Error_SpecJiFen_Not_Enough;
            }

            // 检查钻石是否够
            if (CheckCost == true && myActConfig.Type == (int)SpecialActivityType.SAT_QiangGou)
            {
                if (client.ClientData.UserMoney < myActConfig.Price.NumOne)
                    return StdErrorCode.Error_ZuanShi_Not_Enough;
            }

            return StdErrorCode.Error_Success_No_Info;
        }

        // 背包中是否有足够的位置
        public override bool HasEnoughBagSpaceForAwardGoods(GameClient client, Int32 ActID)
        {
            SpecActInfoDB mySaveData = null;
            if (!client.ClientData.SpecActInfoDict.TryGetValue(ActID, out mySaveData))
                return false;

            if (mySaveData.Active == 0)
                return false;

            SpecialActivityConfig myActConfig = null;
            if (!SpecialActDict.TryGetValue(mySaveData.ActID, out myActConfig))
                return false;

            int nOccu = Global.CalcOriginalOccupationID(client);
            List<GoodsData> lData = new List<GoodsData>();

            //One
            foreach (GoodsData item in myActConfig.GoodsDataListOne)
            {
                lData.Add(item);
            }

            //Two 职业奖励
            int count = myActConfig.GoodsDataListTwo.Count;
            for (int i = 0; i < count; i++)
            {
                GoodsData data = myActConfig.GoodsDataListTwo[i];
                if (Global.IsRoleOccupationMatchGoods(nOccu, data.GoodsID))
                    lData.Add(data);
            }

            //Tre
            AwardItem tmpAwardItem = myActConfig.GoodsDataListThr.ToAwardItem();
            foreach (GoodsData item in tmpAwardItem.GoodsDataList)
            {
                lData.Add(item);
            }

            //判断背包空格是否能提交接受奖励的物品
            return Global.CanAddGoodsDataList(client, lData);
        }  
        
        // 给奖励
        public int SpecActGiveAward(GameClient client, Int32 ActID)
        {
            SpecActInfoDB mySaveData = null;
            if (!client.ClientData.SpecActInfoDict.TryGetValue(ActID, out mySaveData))
                return StdErrorCode.Error_Invalid_Index;

            if (mySaveData.Active == 0)
                return StdErrorCode.Error_Invalid_Index;

            SpecialActivityConfig myActConfig = null;
            if (!SpecialActDict.TryGetValue(mySaveData.ActID, out myActConfig))
                return StdErrorCode.Error_Invalid_Index;

            // 扣充值积分
            if (myActConfig.Type == (int)SpecialActivityType.SAT_InputExchange)
            {
                if (!SubSpecActJiFen(client, myActConfig))
                    return StdErrorCode.Error_SpecJiFen_Not_Enough;
            }

            // 扣钻石
            if (myActConfig.Type == (int)SpecialActivityType.SAT_QiangGou && myActConfig.Price.NumOne > 0)
            {
                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, myActConfig.Price.NumOne, "专属活动抢购"))
                {
                    return StdErrorCode.Error_ZuanShi_Not_Enough;
                }
            }

            AwardItem myAwardItem = new AwardItem();

            // 给通用奖励
            myAwardItem.GoodsDataList = myActConfig.GoodsDataListOne;
            GiveAward(client, myAwardItem);

            //给职业奖励
            myAwardItem.GoodsDataList = myActConfig.GoodsDataListTwo;
            GiveAward(client, myAwardItem);

            //给时效性奖励
            myAwardItem = myActConfig.GoodsDataListThr.ToAwardItem();
            GiveEffectiveTimeAward(client, myAwardItem);

            // 无限购
            if (myActConfig.PurchaseNum == -1)
            {
                mySaveData.PurNum = 1;
            }
            else
            {
                ++mySaveData.PurNum;
            }
            // save to db
            UpdateClientSpecActData(client, mySaveData);

            if (client._IconStateMgr.CheckSpecialActivity(client))
                client._IconStateMgr.SendIconStateToClient(client);

            return StdErrorCode.Error_Success_No_Info;
        }

        // 构造一个领取奖励的返回消息
        public string BuildFetchSpecActAwardCmd(GameClient client, int ErrCode, int actID)
        {
            int roleID = client.ClientData.RoleID;

            SpecActInfoDB mySaveData = null;
            if (!client.ClientData.SpecActInfoDict.TryGetValue(actID, out mySaveData))
                return string.Format("{0}:{1}:{2}:{3}:{4}", StdErrorCode.Error_Invalid_Index, roleID, actID, 0, 0);

            SpecialActivityConfig myActConfig = null;
            if (!SpecialActDict.TryGetValue(mySaveData.ActID, out myActConfig))
                return string.Format("{0}:{1}:{2}:{3}:{4}", StdErrorCode.Error_Invalid_Index, roleID, actID, 0, 0);

            int LeftPurNum = myActConfig.PurchaseNum - mySaveData.PurNum;

            SpecActGoalData CurGoalNum = GetCurrentGoalNum(client, mySaveData, myActConfig);
            int ShowNum = CurGoalNum.NumOne;

            //ret:roleID:ActID:LeftPurNum:ShowNum
            return string.Format("{0}:{1}:{2}:{3}:{4}", ErrCode, roleID, actID, LeftPurNum, ShowNum);
        }

        #region 私有函数

        // 获取符合条件的专享GroupID
        private int GenerateSpecActGroupID()
        {
            DateTime kaifuTm = Global.GetKaiFuTime();
            DateTime nowDateTm = TimeUtil.NowDateTime();
            foreach (var kvp in SpecialActTimeDict)
            {
                SpecialActivityTimeConfig data = kvp.Value;
                if (kaifuTm < data.ServerOpenFromDate || kaifuTm > data.ServerOpenToDate)
                    continue;

                if (nowDateTm < data.FromDate || nowDateTm > data.ToDate)
                    continue;

                return data.GroupID;
            }
            return -1;
        }

        // 查询当前充值积分
        private int GetCurrentSpecActJiFen(GameClient client, SpecialActivityConfig myActConfig)
        {
            string FromActDate = myActConfig.FromDay.ToString();
            string ToActDate = myActConfig.ToDay.ToString();

            string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, FromActDate.Replace(':', '$'), ToActDate.Replace(':', '$'));
            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GET_SPECJIFENINFO, strcmd, client.ServerId);
            if (null == fields || fields.Length < 2)
            {
                return 0;
            }
            return Global.SafeConvertToInt32(fields[1]);
        }

        // 扣除专属活动充值积分
        private bool SubSpecActJiFen(GameClient client, SpecialActivityConfig myActConfig)
        {
            string FromActDate = myActConfig.FromDay.ToString();
            string ToActDate = myActConfig.ToDay.ToString();

            string strcmd = string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, -myActConfig.Price.NumOne, FromActDate.Replace(':', '$'), ToActDate.Replace(':', '$'));
            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_SPECJIFEN, strcmd, client.ServerId);
            if (null == fields || fields.Length < 2)
            {
                return false;
            }

            int retJiFen = Convert.ToInt32(fields[1]);
            if (retJiFen < 0)
            {
                return false;
            }
            return true;
        }

        // 获取角色拥有的专享活动组ID集合
        private HashSet<int> GetClientSpecActGroupIDSet(GameClient client)
        {
            HashSet<int> groupIDSet = new HashSet<int>();
            if (client.ClientData.SpecActInfoDict == null || client.ClientData.SpecActInfoDict.Count == 0)
                return groupIDSet;

            foreach (var kvp in client.ClientData.SpecActInfoDict)
            {
                groupIDSet.Add(kvp.Value.GroupID);
            }
            return groupIDSet;
        }

        // 获取角色拥有的专享活动组ID
        private int GetClientSpecActGroupID(GameClient client)
        {
            int GroupID = -1;
            if (client.ClientData.SpecActInfoDict == null || client.ClientData.SpecActInfoDict.Count == 0)
                return GroupID;

            foreach (var kvp in client.ClientData.SpecActInfoDict)
            {
                GroupID = kvp.Value.GroupID;
                break;
            }
            return GroupID;
        }

        // 生成一个新的专享活动项
        private SpecActInfoDB CreatNewSpecAct(GameClient client, SpecialActivityConfig myActConfig)
        {
            SpecActInfoDB SpecActData = new SpecActInfoDB()
            {
                GroupID = myActConfig.GroupID,
                ActID = myActConfig.ID
            };

            // 检查各种Need条件
            if (CheckNeedCondition(client, myActConfig))
            {
                SpecActData.Active = 1;
            }
            else
            {
                SpecActData.Active = 0;
            }
            return SpecActData;
        }

        // 生成符合条件的专享活动
        private void GenerateSpecActGroup(GameClient client)
        {
            // 组ID
            int GroupID = GenerateSpecActGroupID();
            DateTime nowDateTm = TimeUtil.NowDateTime();

            // 线程安全
            lock (client.ClientData)
            {
                // 无数据
                if (null == client.ClientData.SpecActInfoDict)
                {
                    client.ClientData.SpecActInfoDict = new Dictionary<int, SpecActInfoDB>();
                }

                HashSet<int> CurGroupIDSet = GetClientSpecActGroupIDSet(client);
                foreach (var id in CurGroupIDSet)
                {
                    if (id == GroupID)
                        continue;

                    // 清空活动DB数据
                    DeleteClientSpecActData(client, id);
                }

                // 切换GroupID
                Dictionary<int, SpecActInfoDB> SpecActInfoForUpdate = new Dictionary<int, SpecActInfoDB>(client.ClientData.SpecActInfoDict);
                foreach (var kvp in client.ClientData.SpecActInfoDict)
                {
                    if (kvp.Value.GroupID == GroupID)
                        continue;

                    // 清空活动内存数据
                    SpecActInfoForUpdate.Remove(kvp.Key);
                }

                // 遍历所有活动数据 寻找与GroupID对应的活动
                foreach (var kvp in SpecialActDict)
                {
                    SpecialActivityConfig myActConfig = kvp.Value;
                    if (myActConfig.GroupID != GroupID)
                        continue;

                    SpecActInfoDB SpecActData = null;
                    if (SpecActInfoForUpdate.TryGetValue(myActConfig.ID, out SpecActData))
                    {
                        // 不在活动时间
                        if (nowDateTm < myActConfig.FromDay || nowDateTm > myActConfig.ToDay)
                        {
                            SpecActData.Active = 0;
                        }
                    }
                    else
                    {
                        // 不在活动时间
                        if (nowDateTm < myActConfig.FromDay || nowDateTm > myActConfig.ToDay)
                            continue;

                        SpecActData = CreatNewSpecAct(client, myActConfig);

                        // 更新数据
                        SpecActInfoForUpdate[SpecActData.ActID] = SpecActData;
                    }
                    UpdateClientSpecActData(client, SpecActData);
                }

                // update
                client.ClientData.SpecActInfoDict = SpecActInfoForUpdate;
            }
        }

        // 二级检查
        private bool CheckFirstSecondCondition(int FirstValue, int SecondValue, SpecActLimitData Limit)
        {
            if (FirstValue < Limit.MinFirst || (FirstValue == Limit.MinFirst && SecondValue < Limit.MinSecond))
                return false;

            if (FirstValue > Limit.MaxFirst || (FirstValue == Limit.MaxFirst && SecondValue > Limit.MaxSecond))
                return false;

            return true;
        }

        // 检查各种Need条件
        private bool CheckNeedCondition(GameClient client, SpecialActivityConfig myActConfig)
        {
            // 获得活动所需等级
            if (myActConfig.LevLimit.IfValid())
            {
                if (!CheckFirstSecondCondition(client.ClientData.ChangeLifeCount, client.ClientData.Level, myActConfig.LevLimit))
                    return false;
            }

            // 获得活动所需VIP等级
            if (myActConfig.VipLimit.IfValid())
            {
                if (client.ClientData.VipLevel < myActConfig.VipLimit.MinFirst || client.ClientData.VipLevel > myActConfig.VipLimit.MaxFirst)
                    return false;
            }

            // 获得活动所需充值总数
            if (myActConfig.ChongZhiLimit.IfValid())
            {
                int totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);
                int totalChongZhiYuanBao = Global.TransMoneyToYuanBao(totalChongZhiMoney);
                if (totalChongZhiYuanBao < myActConfig.ChongZhiLimit.MinFirst || totalChongZhiYuanBao > myActConfig.ChongZhiLimit.MaxFirst)
                    return false;
            }

            // 获得活动所需翅膀等级
            if (myActConfig.WingLimit.IfValid())
            {
                if (client.ClientData.MyWingData == null)
                    return false;

                if (!CheckFirstSecondCondition(client.ClientData.MyWingData.WingID, client.ClientData.MyWingData.ForgeLevel, myActConfig.WingLimit))
                    return false;
            }

            // 获得活动所需成就等级
            if (myActConfig.ChengJiuLimit.IfValid())
            {
                int ChengJiuLev = ChengJiuManager.GetChengJiuLevel(client);
                if (ChengJiuLev < myActConfig.ChengJiuLimit.MinFirst || ChengJiuLev > myActConfig.ChengJiuLimit.MaxFirst)
                    return false;
            }

            // 获得活动所需军衔等级
            if (myActConfig.JunXianLimit.IfValid())
            {
                int junxian = GameManager.ClientMgr.GetShengWangLevelValue(client);
                if (junxian < myActConfig.JunXianLimit.MinFirst || junxian > myActConfig.JunXianLimit.MaxFirst)
                    return false;
            }

            // 获得活动所需梅林之书等级
            if (myActConfig.MerlinLimit.IfValid())
            {
                if (client.ClientData.MerlinData == null)
                    return false;

                if (!CheckFirstSecondCondition(client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum, myActConfig.MerlinLimit))
                    return false;
            }

            // 获得活动所需圣物部件总等级
            if (myActConfig.ShengWuLimit.IfValid())
            {
                int TotalLev = 0;
                foreach (var kvp in client.ClientData.MyHolyItemDataDic)
                {
                    HolyItemData myHolyData = kvp.Value;
                    foreach (var item in myHolyData.m_PartArray)
                    {
                        HolyItemPartData myHolyPartData = item.Value;
                        TotalLev += myHolyPartData.m_sSuit;
                    }
                }

                if (TotalLev < myActConfig.ShengWuLimit.MinFirst || TotalLev > myActConfig.ShengWuLimit.MaxFirst)
                    return false;
            }

            // 获得活动所需婚戒等级
            if (myActConfig.RingLimit.IfValid())
            {
                if (client.ClientData.MyMarriageData == null)
                    return false;

                if (!CheckFirstSecondCondition(client.ClientData.MyMarriageData.byGoodwilllevel, client.ClientData.MyMarriageData.byGoodwillstar, myActConfig.RingLimit))
                    return false;
            }

            // 获得活动所需守护神等级
            if (myActConfig.ShouHuShenLimit.IfValid())
            {
                if (client.ClientData.MyGuardStatueDetail == null)
                    return false;

                if (!CheckFirstSecondCondition(client.ClientData.MyGuardStatueDetail.GuardStatue.Suit, CalMyGuardStatueLevel(client), myActConfig.ShouHuShenLimit))
                    return false;
            }

            return true;
        }

        // 获取指定类型的GoalNum当前值
        private SpecActGoalData GetCurrentGoalNum(GameClient client, SpecActInfoDB mySaveData, SpecialActivityConfig myActConfig)
        {
            SpecActGoalData GoalNum = new SpecActGoalData();
            switch (myActConfig.Type)
            {
                case (int)SpecialActivityType.SAT_QiangGou:
                    {
                        GoalNum.NumOne = client.ClientData.UserMoney;
                        break;
                    }
                case (int)SpecialActivityType.SAT_InputExchange:
                    {
                        GoalNum.NumOne = GetCurrentSpecActJiFen(client, myActConfig);
                        break;
                    }
                case (int)SpecialActivityType.SAT_Consume:
                    {
                        GoalNum.NumOne = mySaveData.CountNum;
                        break;
                    }
                case (int)SpecialActivityType.SAT_Level:
                    {
                        GoalNum.NumOne = client.ClientData.ChangeLifeCount;
                        GoalNum.NumTwo = client.ClientData.Level;
                        break;
                    }
                case (int)SpecialActivityType.SAT_Wing:
                    {
                        if (client.ClientData.MyWingData != null)
                        {
                            GoalNum.NumOne = client.ClientData.MyWingData.WingID;
                            GoalNum.NumTwo = client.ClientData.MyWingData.ForgeLevel;
                        }
                        break;
                    }
                case (int)SpecialActivityType.SAT_Vip:
                    {
                        GoalNum.NumOne = client.ClientData.VipLevel;
                        break;
                    }
                case (int)SpecialActivityType.SAT_ChengJiu:
                    {
                        GoalNum.NumOne = ChengJiuManager.GetChengJiuLevel(client);
                        break;
                    }
                case (int)SpecialActivityType.SAT_Merlin:
                    {
                        if (client.ClientData.MerlinData != null)
                        {
                            GoalNum.NumOne = client.ClientData.MerlinData._Level;
                            GoalNum.NumTwo = client.ClientData.MerlinData._StarNum;
                        }
                        break;
                    }
                case (int)SpecialActivityType.SAT_ShengWu:
                    {
                        int TotalLev = 0;
                        foreach (var kvp in client.ClientData.MyHolyItemDataDic)
                        {
                            HolyItemData myHolyData = kvp.Value;
                            foreach (var item in myHolyData.m_PartArray)
                            {
                                HolyItemPartData myHolyPartData = item.Value;
                                TotalLev += myHolyPartData.m_sSuit;
                            }
                        }
                        GoalNum.NumOne = TotalLev;
                        break;
                    }
                case (int)SpecialActivityType.SAT_Ring:
                    {
                        if (client.ClientData.MyMarriageData != null)
                        {
                            GoalNum.NumOne = client.ClientData.MyMarriageData.byGoodwilllevel;
                            GoalNum.NumTwo = client.ClientData.MyMarriageData.byGoodwillstar;
                        }
                        break;
                    }
                case (int)SpecialActivityType.SAT_JunXian:
                    {
                        GoalNum.NumOne = GameManager.ClientMgr.GetShengWangLevelValue(client);
                        break;
                    }
                case (int)SpecialActivityType.SAT_ShouHuShen:
                    {
                        if (client.ClientData.MyGuardStatueDetail != null)
                        {
                            GoalNum.NumOne = client.ClientData.MyGuardStatueDetail.GuardStatue.Suit;
                            GoalNum.NumTwo = CalMyGuardStatueLevel(client);
                        }
                        break;
                    }
            }
            return GoalNum;
        }

        // 计算守护神等级
        private int CalMyGuardStatueLevel(GameClient client)
        {
            GuardStatueData data = client.ClientData.MyGuardStatueDetail.GuardStatue;
            if (data.Level > 0 && data.Level % 10 == 0 && (data.Level + (GuardStatueConst.GuardStatueDefaultSuit * 10)) / 10 != data.Suit)
                return 10;

            return (data.Level % 10);
        }

        // 删除专属活动数据
        private void DeleteClientSpecActData(GameClient client, int GroupID = 0)
        {
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, GroupID);
            Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_DELETE_SPECACT, strcmd, client.ServerId);
        }

        // SpecActInfoDB 序列化
        private void UpdateClientSpecActData(GameClient client, SpecActInfoDB SpecActData)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", 
                client.ClientData.RoleID, SpecActData.GroupID, SpecActData.ActID, SpecActData.PurNum, SpecActData.CountNum, SpecActData.Active);
            Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_SPECACT, strcmd, client.ServerId);
        }

        // 提示客户端开启、关闭专属活动入口
        public void NotifyActivityState(GameClient client)
        {
            // 是否开启活动
            bool bNotifyOpen = false;
            if (null != client.ClientData.SpecActInfoDict && client.ClientData.SpecActInfoDict.Count != 0)
            {
                foreach(var kvp in client.ClientData.SpecActInfoDict)
                {
                    if(kvp.Value.Active == 1)
                    {
                        bNotifyOpen = true;
                        break;
                    }
                }
            }

            // 通知客户端
            if (bNotifyOpen)
            {
                // 开启专属活动
                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 4, 1, "", 0, 0);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_JIERIACT_STATE, strcmd);
            }
            else
            {
                // 关闭专属活动
                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 4, 0, "", 0, 0);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_JIERIACT_STATE, strcmd);
            }
        }

#endregion

#region 配置文件
        public bool Init()
        {
            try
            {
                if (!LoadSpecialActivityTimeData())
                    return false;

                if (!LoadSpecialActivityData())
                    return false;

                // 活动时间初始化为常开
                FromDate = "-1";
                ToDate = "-1";

                AwardStartDate = "-1";
                AwardEndDate = "-1";

                ActivityType = (int)ActivityTypes.SpecActivity;

                PredealDateTime();
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常", ex.Message));
                return false;
            }

            return true;
        }

        // 初始化专属活动SpecialActivity.xml，控制活动内容
        private bool ParseSpecActLimitData(SpecActLimitData LevLimit, string Value)
        {
            if (string.Compare(Value, "-1") == 0 || string.IsNullOrEmpty(Value))
                return true;

            string[] Filed = Value.Split('|');
            if (Filed.Length != 2)
                return false;

            string[] LimitFirst = Filed[0].Split(',');
            string[] LimitSecond = Filed[1].Split(',');
            if (LimitFirst.Length == 2 && LimitSecond.Length == 2) // 2,4|3,5
            {
                LevLimit.MinFirst = Global.SafeConvertToInt32(LimitFirst[0]);
                LevLimit.MinSecond = Global.SafeConvertToInt32(LimitFirst[1]);

                LevLimit.MaxFirst = Global.SafeConvertToInt32(LimitSecond[0]);
                LevLimit.MaxSecond = Global.SafeConvertToInt32(LimitSecond[1]);
            }
            else if (LimitFirst.Length == 1 && LimitSecond.Length == 1) // 4|5
            {
                LevLimit.MinFirst = Global.SafeConvertToInt32(LimitFirst[0]);
                LevLimit.MaxFirst = Global.SafeConvertToInt32(LimitSecond[0]);
            }
            else
            {
                return false;
            }
            return true;
        }

        // 初始化活动数据Day字段
        private bool ParseSpecActDay(int groupID, string Day, SpecialActivityConfig myData)
        {
            SpecialActivityTimeConfig timeConfig = null;
            if (!SpecialActTimeDict.TryGetValue(groupID, out timeConfig))
                return false;

            if (string.Compare(Day, "-1") == 0 || string.IsNullOrEmpty(Day))
            {
                myData.FromDay = timeConfig.FromDate;
                myData.ToDay = timeConfig.ToDate;
                return true;
            }

            string[] DayFiled = Day.Split(',');
            if (DayFiled.Length == 2) // 2,4 代表活动档期内第2天开始、第4天结束
            {
                int SpanFromDay = Global.SafeConvertToInt32(DayFiled[0]) - 1;
                int SpanToDay = Global.SafeConvertToInt32(DayFiled[1]);

                myData.FromDay = Global.GetAddDaysDataTime(timeConfig.FromDate, SpanFromDay);
                myData.ToDay = Global.GetAddDaysDataTime(timeConfig.FromDate, SpanToDay);
            }
            else // 2 代表第2天开始、第2天结束
            {
                int SpanFromDay = Global.SafeConvertToInt32(DayFiled[0]) - 1;
                myData.FromDay = Global.GetAddDaysDataTime(timeConfig.FromDate, SpanFromDay);
                myData.ToDay = new DateTime(myData.FromDay.Year, myData.FromDay.Month, myData.FromDay.Day, 23, 59, 59);
            }
            return true;
        }

        // 初始化专属活动SpecialActivity.xml，控制活动内容
        public bool LoadSpecialActivityData()
        {
            try
            {
                GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(SpecialActivityData_fileName));
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(SpecialActivityData_fileName));
                if (null == xml) return false;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null != xmlItem)
                    {
                        SpecialActivityConfig myData = new SpecialActivityConfig();
                        myData.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        myData.GroupID = (int)Global.GetSafeAttributeLong(xmlItem, "GroupID");

                        // 活动开放日
                        string DayString = Global.GetSafeAttributeStr(xmlItem, "Day");
                        if (!ParseSpecActDay(myData.GroupID, DayString, myData))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件Day失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        if (!ParseSpecActLimitData(myData.LevLimit, Global.GetSafeAttributeStr(xmlItem, "NeedLevel")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedLevel失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        if (!ParseSpecActLimitData(myData.VipLimit, Global.GetSafeAttributeStr(xmlItem, "NeedVIP")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedVIP失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }
                        if (!ParseSpecActLimitData(myData.ChongZhiLimit, Global.GetSafeAttributeStr(xmlItem, "NeedChongZhi")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedChongZhi失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        if (!ParseSpecActLimitData(myData.WingLimit, Global.GetSafeAttributeStr(xmlItem, "NeedWing")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedWing失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        if (!ParseSpecActLimitData(myData.ChengJiuLimit, Global.GetSafeAttributeStr(xmlItem, "NeedChengJiu")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedChengJiu失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        if (!ParseSpecActLimitData(myData.JunXianLimit, Global.GetSafeAttributeStr(xmlItem, "NeedJunXian")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedJunXian失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        if (!ParseSpecActLimitData(myData.MerlinLimit, Global.GetSafeAttributeStr(xmlItem, "NeedMerlin")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedMerlin失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        if (!ParseSpecActLimitData(myData.ShengWuLimit, Global.GetSafeAttributeStr(xmlItem, "NeedShengWu")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedShengWu失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        if (!ParseSpecActLimitData(myData.RingLimit, Global.GetSafeAttributeStr(xmlItem, "NeedRing")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedRing失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        if (!ParseSpecActLimitData(myData.ShouHuShenLimit, Global.GetSafeAttributeStr(xmlItem, "NeedShouHuShen")))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("解析专享活动文件NeedShouHuShen失败 ID:{0},GroupID:{1}", myData.ID, myData.GroupID));
                            continue;
                        }

                        // 活动类型
                        myData.Type = (int)Global.GetSafeAttributeLong(xmlItem, "Type");

                        // 达成条件
                        string GoalString = Global.GetSafeAttributeStr(xmlItem, "Goal");
                        string[] GoalFiled = GoalString.Split(',');
                        if (GoalFiled.Length == 2)
                        {
                            myData.GoalData.NumOne = (int)Global.SafeConvertToInt32(GoalFiled[0]);
                            myData.GoalData.NumTwo = (int)Global.SafeConvertToInt32(GoalFiled[1]);
                        }
                        else
                        {
                            myData.GoalData.NumOne = (int)Global.SafeConvertToInt32(GoalFiled[0]);
                        }

                        // 全职业固定奖励道具
                        string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsOne");
                        string[] fields = goodsIDs.Split('|');
                        if (fields.Length <= 0)
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("解析大型专享活动配置文件中的物品配置项1失败"));
                        }
                        else
                        {
                            //将物品字符串列表解析成物品数据列表
                            myData.GoodsDataListOne = HuodongCachingMgr.ParseGoodsDataList(fields, "专享活动配置1");
                        }

                        // 分职业奖励
                        goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsTwo");
                        if (!string.IsNullOrEmpty(goodsIDs))
                        {
                            fields = goodsIDs.Split('|');
                            myData.GoodsDataListTwo = HuodongCachingMgr.ParseGoodsDataList(fields, "专享活动配置2");                 
                        }


                        // 限时奖励
                        goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsThr");
                        myData.GoodsDataListThr.Init(goodsIDs, Global.GetSafeAttributeStr(xmlItem, "EffectiveTime"), /**/"专享活动配置3");

                        // 价格
                        string Price = Global.GetSafeAttributeStr(xmlItem, "Price");
                        string[] PriceFiled = Price.Split('|');
                        if(PriceFiled.Length == 2)
                        {
                            myData.Price.NumOne = (int)Global.SafeConvertToInt32(PriceFiled[0]);
                            myData.Price.NumTwo = (int)Global.SafeConvertToInt32(PriceFiled[1]);
                        }
                        else
                        {
                            myData.Price.NumOne = (int)Global.SafeConvertToInt32(PriceFiled[0]); 
                        }

                        // 抢购数
                        myData.PurchaseNum = (int)Global.GetSafeAttributeLong(xmlItem, "PurchaseNum");

                        SpecialActDict[myData.ID] = myData;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", SpecialActivityData_fileName, ex.Message));
                return false;
            }

            return true;
        }

        // 初始化专属活动开启配置文件SpecialActivityTime.xml
        public bool LoadSpecialActivityTimeData()
        {
            try
            {
                GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(SpecialActivityTimeData_fileName));
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(SpecialActivityTimeData_fileName));
                if (null == xml) return false;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null != xmlItem)
                    {
                        SpecialActivityTimeConfig myData = new SpecialActivityTimeConfig();
                        myData.GroupID = (int)Global.GetSafeAttributeLong(xmlItem, "GroupID");

                        // 开服时间范围起点
                        string ServerOpenFromDate = Global.GetSafeAttributeStr(xmlItem, "ServerOpenFromDate");
                        if (string.Compare(ServerOpenFromDate, "-1") != 0)
                            myData.ServerOpenFromDate = DateTime.Parse(ServerOpenFromDate);
                        else
                            myData.ServerOpenFromDate = Global.GetKaiFuTime();

                        // 开服时间范围终点
                        string ServerOpenToDate = Global.GetSafeAttributeStr(xmlItem, "ServerOpenToDate");
                        if (string.Compare(ServerOpenToDate, "-1") != 0)
                            myData.ServerOpenToDate = DateTime.Parse(ServerOpenToDate);
                        else
                            myData.ServerOpenToDate = DateTime.Parse("2028-08-08 08:08:08");

                        // 活动开启时间 活动关闭时间
                        string FromDate = Global.GetSafeAttributeStr(xmlItem, "FromDate");
                        if (!string.IsNullOrEmpty(FromDate))
                            myData.FromDate = DateTime.Parse(FromDate);
                        else
                            myData.FromDate = DateTime.Parse("2008-08-08 08:08:08");

                        string ToDate = Global.GetSafeAttributeStr(xmlItem, "ToDate");
                        if (!string.IsNullOrEmpty(ToDate))
                            myData.ToDate = DateTime.Parse(ToDate);
                        else
                            myData.ToDate = DateTime.Parse("2028-08-08 08:08:08");

                        SpecialActTimeDict[myData.GroupID] = myData;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", SpecialActivityTimeData_fileName, ex.Message));
                return false;
            }

            return true;
        }

#endregion

    }
}