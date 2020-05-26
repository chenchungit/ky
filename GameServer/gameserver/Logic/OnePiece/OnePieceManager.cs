using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Tools;
using Server.Data;
using System.Xml.Linq;
using GameServer.Core.GameEvent;
using GameServer.Server;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Server.CmdProcesser;
using GameServer.Core.Executor;

namespace GameServer.Logic.OnePiece
{
    // 藏宝秘境
    public class OnePieceManager : IManager, ICmdProcessorEx
    {
        // 控制掷骰子数字概率
        public List<double> SystemParamsTreasureDice = new List<double>();

        // 控制每日普通骰子次数
        public int SystemParamsTreasureFreeNum = 0;

        // 控制每日奇迹骰子次数
        public int SystemParamsTreasureMiracleNum = 0;

        // 控制骰子价格消耗钻石
        public int SystemParamsTreasurePrice = 0;

        // 控制奇迹骰子价格消耗钻石
        public int SystemParamsTreasureSuperPrice = 0;

        // 配置文件目录
        private const string OnePiece_TreasureMapfileName = "Config/Treasure/TreasureMap.xml";
        private const string OnePiece_TreasureEventfileName = "Config/Treasure/TreasureEvent.xml";
        private const string OnePiece_TreasureBoxfileName = "Config/Treasure/TreasureBox.xml";

        // 计算用常量
        private const int OnePiece_FloorHashNum = 1000;
        private const int OnePiece_FloorCellNum = 30;
        private const int OnePiece_DiceMaxNum = 99;

        // GM命令用
        public int OnePiece_FakeRollNum_GM = 0;

        /// <summary>
        /// 静态实例
        /// </summary>
        private static OnePieceManager instance = new OnePieceManager();
        public static OnePieceManager getInstance()
        {
            return instance;
        }

        // 藏宝地图 TreasureMap.xml
        // ID vs OnePieceTreasureMapConfig
        public Dictionary<int, OnePieceTreasureMapConfig> TreasureMapConfig = new Dictionary<int, OnePieceTreasureMapConfig>();

        // 藏宝事件 TreasureEvent.xml
        // ID vs OnePieceTreasureEventConfig
        public Dictionary<int, OnePieceTreasureEventConfig> TreasureEventConfig = new Dictionary<int, OnePieceTreasureEventConfig>();

        // 宝箱 TreasureBox.xml
        // ID vs OnePieceTreasureBoxConfig
        public Dictionary<int, List<OnePieceTreasureBoxConfig>> TreasureBoxConfig = new Dictionary<int, List<OnePieceTreasureBoxConfig>>();

        #region 接口实现

        public bool initialize()
        {
            if (!InitConfig())
            {
                return false;
            }
            return true;
        }

        public bool startup()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_GET_INFO, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_ROLL, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_ROLL_MIRACLE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_TRIGGER_EVENT, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_MOVE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_DICE_BUY, 2, 2, getInstance());
            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            bool isOpen = GlobalNew.IsGongNengOpened(client, GongNengIDs.OnePieceTreasure);
            if (!isOpen)
            {
                // 返回错误信息
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    StringUtil.substitute(Global.GetLang("藏宝秘境功能未开放")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

                return true;
            }

            // 如果1.9的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot9))
            {
                // 返回错误信息
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    StringUtil.substitute(Global.GetLang("藏宝秘境功能未开启")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

                return true;
            }

            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_ONEPIECE_GET_INFO:
                    return ProcessOnePieceGetInfoCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ONEPIECE_ROLL:
                    return ProcessOnePieceRollCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ONEPIECE_ROLL_MIRACLE:
                    return ProcessOnePieceRollMiracleCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ONEPIECE_TRIGGER_EVENT:
                    return ProcessOnePieceTriggerEventCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ONEPIECE_MOVE:
                    return ProcessOnePieceMoveCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ONEPIECE_DICE_BUY:
                    return ProcessOnePieceDiceBuyCmd(client, nID, bytes, cmdParams);
            }
            return true;
        }

        #endregion

        #region 藏宝秘境数据序列化接口
        /// <summary>
        /// 获取角色藏宝秘境数据
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public void GetOnePieceTreasureData(GameClient client, OnePieceTreasureData myTreasureData)
        {
            int currday = Global.GetOffsetDay(TimeUtil.NowDateTime());
            DateTime now = TimeUtil.NowDateTime();

            int lastday = 0;
            int rollnum = 0;
            int rollnumMiracle = 0;
            int posid = GenPosID(1, 0);
            int eventid = 0;

            string strFlag = RoleParamName.TreasureData;
            String OnePieceTreasureDataFlag = Global.GetRoleParamByName(client, strFlag);

            // day,rollnum,rollnumMiracle,posid,eventid
            if (null != OnePieceTreasureDataFlag)
            {
                string[] fields = OnePieceTreasureDataFlag.Split(',');
                if (5 == fields.Length)
                {
                    lastday = Convert.ToInt32(fields[0]);
                    rollnum = Convert.ToInt32(fields[1]);
                    rollnumMiracle = Convert.ToInt32(fields[2]);
                    posid = Convert.ToInt32(fields[3]);
                    eventid = Convert.ToInt32(fields[4]);
                }
            }

            // 赋值
            myTreasureData.PosID = posid;
            myTreasureData.EventID = eventid;
            myTreasureData.RollNumNormal = rollnum;
            myTreasureData.RollNumMiracle = rollnumMiracle;

            // 计算距离下一个周一00:00:00的时间
            string resetTime = now.ToString("yyyy-MM-dd");
            DateTime resetDateTm;
            if(DateTime.TryParse(resetTime, out resetDateTm))
            {
                int spanday = DayOfWeek.Monday - now.DayOfWeek;
                spanday = (spanday <= 0) ? (7 + spanday) : spanday;
                resetDateTm = resetDateTm.AddDays(spanday);
                myTreasureData.ResetPosTicks = TimeUtil.TimeDiff(resetDateTm.Ticks, now.Ticks);
            }
        }

        /// <summary>
        /// 服务器检查藏宝秘境重置
        /// 在使用时务必确保串行操作(对于单个GameClient)，不可多线程并行调用接口(对于单个GameClient)
        /// </summary>
        public void JudgeResetOnePieceTreasureData(GameClient client)
        {
            // 移动中不允许重置
            if (client.ClientData.OnePieceMoveLeft != 0)
                return;

            int currday = Global.GetOffsetDay(TimeUtil.NowDateTime());
            DateTime now = TimeUtil.NowDateTime();

            string strFlag = RoleParamName.TreasureData;
            String OnePieceTreasureDataFlag = Global.GetRoleParamByName(client, strFlag);

            int lastday = 0;
            if (null != OnePieceTreasureDataFlag)
            {
                // day,rollnum,rollnumMiracle,posid,eventid
                string[] fields = OnePieceTreasureDataFlag.Split(',');
                if (5 == fields.Length)
                {
                    lastday = Convert.ToInt32(fields[0]);
                }
            }

            OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
            GetOnePieceTreasureData(client, myOnePieceData);

            int daydis = now.DayOfWeek - DayOfWeek.Monday; // -1~5
            daydis = (daydis >= 0) ? (daydis + 1) : 7;
            
            // 每周一刷新一次
            if (lastday != 0 && currday - lastday >= daydis)
            {
                myOnePieceData.PosID = ResetRolePos(client); // 重置角色位置
                myOnePieceData.EventID = 0;
            }

            // 每日刷新一次 限制只针对未来时间起效
            if (currday != lastday && currday > lastday)
            {
                HandleDicePassDay(client, currday, lastday, myOnePieceData);
                lastday = currday;
            }

            // save to db
            string result = string.Format("{0},{1},{2},{3},{4}",
                lastday, myOnePieceData.RollNumNormal, myOnePieceData.RollNumMiracle, myOnePieceData.PosID, myOnePieceData.EventID);
            Global.SaveRoleParamsStringToDB(client, strFlag, result, true);
        }

        /// <summary>
        /// 处理骰子赠送
        /// </summary>
        public void HandleDicePassDay(GameClient client, int currday, int lastday, OnePieceTreasureData myOnePieceData)
        {
            int passday = 1;
            if (lastday != 0) // lastday有效 计算跨了几天
                passday = currday - lastday;

            if (passday > 0)
            {
                // 每天增加SystemParamsTreasureFreeNum次 不可超上限
                ModifyOnePieceDice(client, myOnePieceData, (int)DiceType.EDT_Normal, SystemParamsTreasureFreeNum * passday);

                // 每天增加SystemParamsTreasureMiracleNum次 不可超上限
                ModifyOnePieceDice(client, myOnePieceData, (int)DiceType.EDT_Miracle, SystemParamsTreasureMiracleNum * passday);
            }
        }

        /// <summary>
        /// 重置位置
        /// </summary>
        public int ResetRolePos(GameClient client)
        {
            int posid = GenPosID(1, 0);

            // 宝箱奖励
            TryGiveOnePieceBoxListAward(client);

            // 告诉客户端被刷新了
            string strcmd = string.Format("{0}:{1}", (int)OnePieceTreasureErrorCode.OnePiece_ResetPos, posid);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_MOVE, strcmd);

            return posid;
        }

        /// <summary>
        /// 存储角色藏宝秘境缓存数据
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public void ModifyOnePieceTreasureData(GameClient client, OnePieceTreasureData myOnePieceData)
        {
            int currday = Global.GetOffsetDay(TimeUtil.NowDateTime());
            DateTime now = TimeUtil.NowDateTime();

            string strFlag = RoleParamName.TreasureData;
            String OnePieceTreasureDataFlag = Global.GetRoleParamByName(client, strFlag);

            int lastday = currday; // 首次获取默认值
            if (null != OnePieceTreasureDataFlag)
            {
                // day,rollnum,rollnumMiracle,posid,eventid
                string[] fields = OnePieceTreasureDataFlag.Split(',');
                if (5 == fields.Length)
                {
                    lastday = Convert.ToInt32(fields[0]);
                }
            }
            string result = string.Format("{0},{1},{2},{3},{4}",
                lastday, myOnePieceData.RollNumNormal, myOnePieceData.RollNumMiracle, myOnePieceData.PosID, myOnePieceData.EventID);
            Global.SaveRoleParamsStringToDB(client, strFlag, result, true);
        }
#endregion

        // 根据层号 + 格子号得到位置ID
        public int GenPosID(int floor, int cell)
        {
            return OnePiece_FloorHashNum * floor + cell;
        }

        // 根据PosID计算层号
        public int GetFloorByPosID(int posid)
        {
            return posid / OnePiece_FloorHashNum;
        }

        /// <summary>
        /// 处理GetNextPosIDForEvent完毕后的结果，用于客户端显示
        /// </summary>
        public int FilterPosIDChangeFloor(GameClient client, int posid)
        {
            int FilterPosID = posid;

            if (client.ClientData.OnePieceMoveDir == 0) // 方向非法
                return FilterPosID;

            if (IfHaveOnePieceBoxListAward(client)) // 暂停事件
                return FilterPosID;

            bool foward = (client.ClientData.OnePieceMoveDir > 0) ? true : false;

            // 非起点 && 非终点
            if ((posid % OnePiece_FloorHashNum) != 0 && (posid % OnePiece_FloorHashNum) != OnePiece_FloorCellNum)
                return FilterPosID;

            if(true == foward)
            {
                FilterPosID = (GetFloorByPosID(posid) + 1) * OnePiece_FloorHashNum;
            }
            else
            {
                FilterPosID = posid - 1;
            }

            OnePieceTreasureMapConfig myTreasureMapConfig = null;
            if (!TreasureMapConfig.TryGetValue(FilterPosID, out myTreasureMapConfig))
            {
                FilterPosID = GenPosID(1, 0);
            }
            return FilterPosID;
        }

        /// <summary>
        /// 该方法为了可踩到所有的Event特殊处理，计算下一步的PosID
        /// </summary>
        public int GetNextPosIDForEvent(int posid, bool foward = true)
        {
            int nextPosID = 0;
            if (true == foward)
                nextPosID = posid + 1;
            else
                nextPosID = posid - 1;

            OnePieceTreasureMapConfig myTreasureMapConfig = null;
            if (TreasureMapConfig.TryGetValue(nextPosID, out myTreasureMapConfig)
                && (posid % OnePiece_FloorHashNum) != OnePiece_FloorCellNum // foward == true 当前位置到达层终点位置   
                && (nextPosID % OnePiece_FloorHashNum) != 0 ) // foward == false 下一步到达层起始位置
            {
                return nextPosID;
            }

            // 与策划约定PosID 1030、2000实际对应一个格子有效奖励内容填写在1030中，以此类推。
            if (true == foward)
            {
                nextPosID = (GetFloorByPosID(posid) + 1) * OnePiece_FloorHashNum + 1;
                if (!TreasureMapConfig.TryGetValue(nextPosID, out myTreasureMapConfig))
                {
                    nextPosID = GenPosID(1, 1); // 最高层终点的情况
                }
            }
            else
            {
                nextPosID = (GetFloorByPosID(posid) - 1) * OnePiece_FloorHashNum + OnePiece_FloorCellNum;
                if (!TreasureMapConfig.TryGetValue(nextPosID, out myTreasureMapConfig))
                {
                    nextPosID = GenPosID(1, 0); // 最低层起点
                }
            }
            return nextPosID;
        }

        /// <summary>
        /// roll骰子
        /// </summary>
        public int RollMoveNum()
        {
            int move = 0; // 移动点数

            // gm命令用
            if (OnePiece_FakeRollNum_GM != 0)
            {
                move = OnePiece_FakeRollNum_GM;
                return move;
            }

            double rate = (double)Global.GetRandomNumber(1, 101) / 100;

            double rateend = 0.0d; // 概率右区间
            for (int n = 0; n < SystemParamsTreasureDice.Count; ++n)
            {
                rateend += SystemParamsTreasureDice[n];
                if (rate <= rateend)
                {
                    move = n + 1;
                    break;
                }
            }
            return move;
        }

        /// <summary>
        /// 随机事件
        /// </summary>
        public int RandomTreasureEvent(List<OnePieceRandomEvent> LisRandomEvent)
        {
            int EventID = 0;
            if (null == LisRandomEvent || LisRandomEvent.Count == 0)
                return EventID;

            double rate = (double)Global.GetRandomNumber(1, 101) / 100;
            double rateend = 0.0d; // 概率右区间
            for (int n = 0; n < LisRandomEvent.Count; ++n)
            {
                rateend += LisRandomEvent[n].Rate;
                if (rate <= rateend)
                {
                    EventID = LisRandomEvent[n].EventID;
                    break;
                }
            }
            return EventID;
        }

        /// <summary>
        /// 同步事件给客户端
        /// </summary>
        public void SyncOnePieceEvent(GameClient client, int EventID, int EventValue = 0, int ErrCode = 0, List<int> BoxIDList = null)
        {
            OnePieceTreasureEvent myTreasureEvent = new OnePieceTreasureEvent()
            {
                EventID = EventID,
                EventValue = EventValue,
                BoxIDList = BoxIDList,
                ErrCode = ErrCode
            };
            byte[] bytesData = DataHelper.ObjectToBytes<OnePieceTreasureEvent>(myTreasureEvent);
            GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_SPR_ONEPIECE_SYNC_EVENT);
        }

        /// <summary>
        /// 改变骰子数并同步给客户端
        /// </summary>
        public void ModifyOnePieceDice(GameClient client, OnePieceTreasureData myOnePieceData, int diceType, int num)
        {
            bool RollNumReachMax = false;
            string strcmd = "";
            int oldNum = 0;
            int newNum = 0;
            if (diceType == (int)DiceType.EDT_Normal) // 普通骰子
            {
                oldNum = myOnePieceData.RollNumNormal;

                myOnePieceData.RollNumNormal += num;
                if (myOnePieceData.RollNumNormal > OnePiece_DiceMaxNum) // 客户端提示 “骰子数达到上限”
                {
                    myOnePieceData.RollNumNormal = OnePiece_DiceMaxNum;
                    RollNumReachMax = true;
                }
                newNum = myOnePieceData.RollNumNormal;
            }
            else if (diceType == (int)DiceType.EDT_Miracle) // 奇迹骰子
            {
                oldNum = myOnePieceData.RollNumMiracle;

                myOnePieceData.RollNumMiracle += num;
                if (myOnePieceData.RollNumMiracle > OnePiece_DiceMaxNum) 
                {
                    myOnePieceData.RollNumMiracle = OnePiece_DiceMaxNum;
                    RollNumReachMax = true;
                }
                newNum = myOnePieceData.RollNumMiracle;
            }
            else
            {
                return;
            }
            strcmd = string.Format("{0}:{1}:{2}:{3}",
                (int)OnePieceTreasureErrorCode.OnePiece_Success, diceType, newNum, oldNum);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_SYNC_DICE, strcmd);

            // 客户端提示 “骰子数达到上限”
            if (RollNumReachMax)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}",
                    (int)OnePieceTreasureErrorCode.OnePiece_ErrorRollNumMax, diceType, newNum, oldNum);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_SYNC_DICE, strcmd);
            }
        }

        /// <summary>
        /// 给予事件奖励（副本）
        /// </summary>
        public int GiveCopyMapGift(GameClient client, int fuBenID)
        {
            // 获取藏宝秘境数据
            OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
            GetOnePieceTreasureData(client, myOnePieceData);

            if (client.ClientData.OnePieceTempEventID == 0)
                return 0;
      
            OnePieceTreasureEventConfig myTreasureEventConfig = null;
            if (!TreasureEventConfig.TryGetValue(client.ClientData.OnePieceTempEventID, out myTreasureEventConfig))
                return 0;

            if (myTreasureEventConfig.FuBenID != fuBenID)
                return 0;

            int EventID = client.ClientData.OnePieceTempEventID;

            GiveOnePieceEventAward(client, myOnePieceData, myTreasureEventConfig);

            client.ClientData.OnePieceTempEventID = 0; // clear temp cache event

            return EventID;
        }

        /// <summary>
        /// 给予事件奖励
        /// </summary>
        public OnePieceTreasureErrorCode GiveOnePieceEventAward(GameClient client, OnePieceTreasureData myOnePieceData, OnePieceTreasureEventConfig myTreasureEventConfig)
        {
            OnePieceTreasureErrorCode ret = OnePieceTreasureErrorCode.OnePiece_Success;

            // 尝试给奖励
            List<GoodsData> goodsDataList = Global.ConvertToGoodsDataList(myTreasureEventConfig.GoodsList.Items);
            if (!Global.CanAddGoodsDataList(client, goodsDataList))
            {
                // 兑换奖励如果背包空间不够返回错误码
                if (myTreasureEventConfig.Type != TreasureEventType.ETET_Excharge)
                {
                    foreach (var item in goodsDataList)
                        Global.UseMailGivePlayerAward(client, item, Global.GetLang("获得藏宝秘境奖励"), Global.GetLang("获得藏宝秘境奖励"));

                    // 返回值
                    ret = OnePieceTreasureErrorCode.OnePiece_ErrorCheckMail;
                }
                else
                {
                    return OnePieceTreasureErrorCode.OnePiece_ErrorBagNotEnough;
                }
            }
            else
            {
                //领取物品
                for (int n = 0; n < goodsDataList.Count; n++)
                {
                    GoodsData goodsData = goodsDataList[n];

                    if (null == goodsData)
                    {
                        continue;
                    }

                    //向DBServer请求加入某个新的物品到背包中
                    goodsData.Id = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, "获得藏宝秘境奖励", goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong);
                }
            }

            // 给钱
            if (myTreasureEventConfig.NewValue.Type != MoneyTypes.None)
            {
                switch ((int)myTreasureEventConfig.NewValue.Type)
                {
                    case (int)MoneyTypes.TongQian:
                        GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, myTreasureEventConfig.NewValue.Num, "获得藏宝秘境奖励", false);
                        break;
                    case (int)MoneyTypes.YinLiang:
                        GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, myTreasureEventConfig.NewValue.Num, "获得藏宝秘境奖励");
                        break;
                    case (int)MoneyTypes.YuanBao:
                        GameManager.ClientMgr.AddUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, myTreasureEventConfig.NewValue.Num, "获得藏宝秘境奖励");
                        break;
                    case (int)MoneyTypes.BindYuanBao:
                        GameManager.ClientMgr.AddUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, myTreasureEventConfig.NewValue.Num, "获得藏宝秘境奖励");
                        break;
                    case (int)MoneyTypes.BaoZangJiFen:
                        GameManager.ClientMgr.ModifyTreasureJiFenValue(client, myTreasureEventConfig.NewValue.Num, true);
                        break;
                    case (int)MoneyTypes.BaoZangXueZuan:
                        GameManager.ClientMgr.ModifyTreasureXueZuanValue(client, myTreasureEventConfig.NewValue.Num, true);
                        break;
                }
            }

            // 骰子奖励
            if (myTreasureEventConfig.NewDiec > 0)
                ModifyOnePieceDice(client, myOnePieceData, (int)DiceType.EDT_Normal, myTreasureEventConfig.NewDiec);
            
            // 奇迹骰子奖励
            if (myTreasureEventConfig.NewSuperDiec > 0)
                ModifyOnePieceDice(client, myOnePieceData, (int)DiceType.EDT_Miracle, myTreasureEventConfig.NewSuperDiec);
                
            return ret;
        }

        /// <summary>
        /// 触发事件 ETET_Award
        /// </summary>
        public OnePieceTreasureErrorCode TriggerEventAward(GameClient client, OnePieceTreasureData myOnePieceData, OnePieceTreasureEventConfig myTreasureEventConfig)
        {
            // 尝试给事件奖励
            OnePieceTreasureErrorCode ret = GiveOnePieceEventAward(client, myOnePieceData, myTreasureEventConfig);
            if (ret != OnePieceTreasureErrorCode.OnePiece_Success && ret != OnePieceTreasureErrorCode.OnePiece_ErrorCheckMail)
                return ret; // 给奖励没成功

            // 同步给客户端
            SyncOnePieceEvent(client, myTreasureEventConfig.ID, 0, (int)ret);

            return OnePieceTreasureErrorCode.OnePiece_Success;
        }

        /// <summary>
        /// 触发事件 ETET_Excharge
        /// </summary>
        public OnePieceTreasureErrorCode TriggerEventExcharge(GameClient client, OnePieceTreasureData myOnePieceData, OnePieceTreasureEventConfig myTreasureEventConfig)
        {
            // 检查Need物品
            for (int n = 0; n < myTreasureEventConfig.NeedGoods.Count; ++n )
            {
                SystemXmlItem needGoods = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(myTreasureEventConfig.NeedGoods[n]._NeedGoodsID, out needGoods))
                    return OnePieceTreasureErrorCode.OnePiece_ErrorNeedGoodsID;

                if (myTreasureEventConfig.NeedGoods[n]._NeedGoodsCount <= 0)
                    return OnePieceTreasureErrorCode.OnePiece_ErrorNeedGoodsCount;

                // 获取背包里
                int nTotalGoodsCount = Global.GetTotalGoodsCountByID(client, myTreasureEventConfig.NeedGoods[n]._NeedGoodsID);
                if (nTotalGoodsCount < myTreasureEventConfig.NeedGoods[n]._NeedGoodsCount)
                    return OnePieceTreasureErrorCode.OnePiece_ErrorGoodsNotEnough;
            }

            // 检查Need货币
            if (0 > Global.IsRoleHasEnoughMoney(client, myTreasureEventConfig.NeedValue.Num, (int)myTreasureEventConfig.NeedValue.Type))
            {
                return OnePieceTreasureErrorCode.OnePiece_ErrorNeedMoneyNotEnough;
            }

            // 尝试给事件奖励
            OnePieceTreasureErrorCode ret = GiveOnePieceEventAward(client, myOnePieceData, myTreasureEventConfig);
            if (OnePieceTreasureErrorCode.OnePiece_Success != ret)
                return ret;

            // 扣物品
            for (int n = 0; n < myTreasureEventConfig.NeedGoods.Count; ++n)
            {
                bool usedBinding = false;
                bool usedTimeLimited = false;

                GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    client, myTreasureEventConfig.NeedGoods[n]._NeedGoodsID, myTreasureEventConfig.NeedGoods[n]._NeedGoodsCount, false, out usedBinding, out usedTimeLimited);
            }

            // 扣钱
            Global.SubRoleMoneyForGoods(client, myTreasureEventConfig.NeedValue.Num, (int)myTreasureEventConfig.NeedValue.Type, "藏宝秘境");

            return OnePieceTreasureErrorCode.OnePiece_Success;
        }

        /// <summary>
        /// 触发事件 ETET_Move
        /// </summary>
        public OnePieceTreasureErrorCode TriggerEventMove(GameClient client, OnePieceTreasureData myOnePieceData, OnePieceTreasureEventConfig myTreasureEventConfig)
        {
            if(null == myTreasureEventConfig.MoveRange || myTreasureEventConfig.MoveRange.Count == 0)
                return OnePieceTreasureErrorCode.OnePiece_ErrorMoveRange;

            // 已当前位置为起点 随机移动
            int randIndex = Global.GetRandomNumber(0, myTreasureEventConfig.MoveRange.Count);
            int move = myTreasureEventConfig.MoveRange[randIndex];

            // set movenum
            client.ClientData.OnePieceMoveLeft = move;
            client.ClientData.OnePieceMoveDir = move;

            // 同步给客户端
            SyncOnePieceEvent(client, myTreasureEventConfig.ID, move);

            return OnePieceTreasureErrorCode.OnePiece_Success;
        }

        /// <summary>
        /// 触发事件 ETET_Combat
        /// </summary>
        public OnePieceTreasureErrorCode TriggerEventCombat(GameClient client, OnePieceTreasureEventConfig myTreasureEventConfig)
        {
            //获取副本的数据
            SystemXmlItem systemFuBenItem = null;
            if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(myTreasureEventConfig.FuBenID, out systemFuBenItem))
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client,
                    StringUtil.substitute(Global.GetLang("进入藏宝秘境时错误, 没有找到副本配置")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return OnePieceTreasureErrorCode.OnePiece_DBFailed;
            }

            int toMapCode = systemFuBenItem.GetIntValue("MapCode");

            //从DBServer获取副本顺序ID
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GETFUBENSEQID, string.Format("{0}", client.ClientData.RoleID), client.ServerId);
            if (null == dbFields || dbFields.Length < 2)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client,
                    StringUtil.substitute(Global.GetLang("进入藏宝秘境副本时错误, 从DBServer获取副本序号失败")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return OnePieceTreasureErrorCode.OnePiece_DBFailed;
            }

            int fuBenSeqID = Global.SafeConvertToInt32(dbFields[1]);

            //增加副本今日的进入次数
            Global.UpdateFuBenData(client, myTreasureEventConfig.FuBenID);

            //通知用户切换地图到副本的地图上
            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client,
                    StringUtil.substitute(Global.GetLang("进入藏宝秘境时错误, 地图编号无效")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return OnePieceTreasureErrorCode.OnePiece_DBFailed;
            }

            //设置角色的副本顺序ID
            client.ClientData.FuBenSeqID = fuBenSeqID;

            client.ClientData.FuBenID = myTreasureEventConfig.FuBenID;

            //添加一个角色到副本顺序ID的映射
            FuBenManager.AddFuBenSeqID(client.ClientData.RoleID, client.ClientData.FuBenSeqID, 0, myTreasureEventConfig.FuBenID);

            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, toMapCode, -1, -1, -1);

            // cache event id
            client.ClientData.OnePieceTempEventID = myTreasureEventConfig.ID;

            return OnePieceTreasureErrorCode.OnePiece_Success;
        }

        /// <summary>
        /// 发宝箱奖励
        /// </summary>
        public int GiveOnePieceBoxAward(GameClient client, OnePieceTreasureBoxConfig myBoxConfig)
        {
            int ret = (int)OnePieceTreasureErrorCode.OnePiece_Success;

            // 如果是物品奖励
            if (myBoxConfig.Type == TeasureBoxType.ETBT_Goods)
            {
                List<GoodsData> goodsDataList = Global.ConvertToGoodsDataList(myBoxConfig.Goods.Items);
                // 检查背包空间 发邮件
                if (!Global.CanAddGoodsDataList(client, goodsDataList))
                {
                    foreach (var item in goodsDataList)
                        Global.UseMailGivePlayerAward(client, item, Global.GetLang("获得藏宝秘境奖励"), Global.GetLang("获得藏宝秘境奖励"));

                    // 返回值
                    ret = (int)OnePieceTreasureErrorCode.OnePiece_ErrorCheckMail;
                }
                else
                {
                    //领取物品
                    for (int n = 0; n < goodsDataList.Count; n++)
                    {
                        GoodsData goodsData = goodsDataList[n];

                        if (null == goodsData)
                        {
                            continue;
                        }

                        //向DBServer请求加入某个新的物品到背包中
                        goodsData.Id = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, "获得藏宝秘境奖励", goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong);
                    }
                }
            }
            else if (myBoxConfig.Type == TeasureBoxType.ETBT_BaoZangJiFen)
            {
                GameManager.ClientMgr.ModifyTreasureJiFenValue(client, myBoxConfig.Num, true);
            }
            else if (myBoxConfig.Type == TeasureBoxType.ETBT_BaoZangXueZuan)
            {
                GameManager.ClientMgr.ModifyTreasureXueZuanValue(client, myBoxConfig.Num, true);
            }
            return ret;
        }

        /// <summary>
        /// 给宝箱奖励
        /// </summary>
        public int TryGiveOnePieceBoxListAward(GameClient client)
        {
            List<int> OnePieceBoxIDList = client.ClientData.OnePieceBoxIDList;
            if (OnePieceBoxIDList == null)
                return (int)OnePieceTreasureErrorCode.OnePiece_DBFailed;

            int ret = (int)OnePieceTreasureErrorCode.OnePiece_Success;
            for(int n=0; n<OnePieceBoxIDList.Count; ++n)
            {
                int BoxID = OnePieceBoxIDList[n] / 1000;
                int BoxConfigID = OnePieceBoxIDList[n] % 1000;

                List<OnePieceTreasureBoxConfig> myBoxConfigList = null;
                if (!TreasureBoxConfig.TryGetValue(BoxID, out myBoxConfigList))
                    continue;

                if (BoxConfigID <= 0 || BoxConfigID > myBoxConfigList.Count)
                    continue;

                ret = GiveOnePieceBoxAward(client, myBoxConfigList[BoxConfigID-1]);
            }

            client.ClientData.OnePieceBoxIDList = null;

            return ret;
        }

        /// <summary>
        /// 触发事件 ETET_TreasureBox
        /// </summary>
        public OnePieceTreasureErrorCode TriggerEventTreasureBox(GameClient client, OnePieceTreasureEventConfig myTreasureEventConfig)
        {
            // 随机宝箱数据
            List<int> BoxIDList = new List<int>();
            for (int n = 0; n < myTreasureEventConfig.BoxList.Count; ++n)
            {
                int OpenNum = myTreasureEventConfig.BoxList[n].OpenNum;
                
                List<OnePieceTreasureBoxConfig> myBoxConfig = null;
                if (!TreasureBoxConfig.TryGetValue(myTreasureEventConfig.BoxList[n].BoxID, out myBoxConfig))
                    continue;

                for(int loop=0; loop<OpenNum; ++loop)
                {
                    // 宝箱列表内随机一个宝箱
                    int RandRangeMin = myBoxConfig[0].BeginNum;
                    int RandRangeMax = myBoxConfig[myBoxConfig.Count - 1].EndNum + 1;

                    int randnum = Global.GetRandomNumber(RandRangeMin, RandRangeMax);
                    for (int index = 0; index < myBoxConfig.Count; ++index)
                    {
                        if (randnum <= myBoxConfig[index].EndNum)
                        {
                            int BoxID = myTreasureEventConfig.BoxList[n].BoxID * 1000 + myBoxConfig[index].ID;
                            BoxIDList.Add(BoxID); // for client
                            break;
                        }
                    }
                }
            }

            // 缓存随机出来的宝箱数据
            client.ClientData.OnePieceBoxIDList = BoxIDList;

            // 同步给客户端
            SyncOnePieceEvent(client, myTreasureEventConfig.ID, 0, 0, BoxIDList);

            return OnePieceTreasureErrorCode.OnePiece_Success;
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public OnePieceTreasureErrorCode TriggerEvent(GameClient client, OnePieceTreasureData myOnePieceData, OnePieceTreasureEventConfig myTreasureEventConfig)
        {
            switch (myTreasureEventConfig.Type)
            {
                case TreasureEventType.ETET_Award:
                    {
                        return TriggerEventAward(client, myOnePieceData, myTreasureEventConfig);
                    }
                case TreasureEventType.ETET_Excharge:
                    {
                        return TriggerEventExcharge(client, myOnePieceData, myTreasureEventConfig);
                    }
                case TreasureEventType.ETET_Move:
                    {
                        return TriggerEventMove(client, myOnePieceData, myTreasureEventConfig);
                    }
                case TreasureEventType.ETET_Combat:
                    {
                        return TriggerEventCombat(client, myTreasureEventConfig);
                    }
                case TreasureEventType.ETET_TreasureBox:
                    {
                        return TriggerEventTreasureBox(client, myTreasureEventConfig);
                    }
                default:
                    {
                        break;
                    }
            }
            return OnePieceTreasureErrorCode.OnePiece_ErrorNotHaveEvent;
        }

        /// <summary>
        /// 尝试触发
        /// </summary>
        public bool OnePieceMoveTrigger(GameClient client, ref OnePieceTreasureData myOnePieceData, OnePieceTreasureMapConfig myTreasureMapConfig, TriggerType Trigger)
        {
            // 事件类型
            if (Trigger != myTreasureMapConfig.Trigger)
                return false;

            // 停留在该格子获得秘宝点数
            if (myTreasureMapConfig.Score > 0 && Trigger == TriggerType.ETT_Stay)
                GameManager.ClientMgr.ModifyTreasureJiFenValue(client, myTreasureMapConfig.Score, true);

            int EventID = RandomTreasureEvent(myTreasureMapConfig.LisRandomEvent);
            OnePieceTreasureEventConfig myTreasureEventConfig = null;
            if (!TreasureEventConfig.TryGetValue(EventID, out myTreasureEventConfig))
                return false;

            // ETET_Combat、ETET_Excharge 缓存事件 非自动触发
            if (myTreasureEventConfig.Type == TreasureEventType.ETET_Combat || myTreasureEventConfig.Type == TreasureEventType.ETET_Excharge)
            {  
                myOnePieceData.EventID = EventID;

                // 同步给客户端
                SyncOnePieceEvent(client, myTreasureEventConfig.ID);
            }
            else
            {
                // 触发
                TriggerEvent(client, myOnePieceData, myTreasureEventConfig);
            }
            return true;
        }

        /// <summary>
        /// 处理客户端掉线
        /// </summary>
        public void HandleRoleLogout(GameClient client)
        {
            if (!IfCanContinueMove(client))
                return;

            // 获取藏宝秘境数据
            OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
            GetOnePieceTreasureData(client, myOnePieceData);

            // 把剩余步数走完 该给的全给 6步之内最多碰1个宝箱 所以+1
            for (int n = 0; n < SystemParamsTreasureDice.Count + 1; ++n )
            {
                // 检查宝箱奖励 是否有打开但未给奖励的宝箱
                TryGiveOnePieceBoxListAward(client);
      
                // 移动
                HandleOnePieceTreasureMove(client, client.ClientData.OnePieceMoveLeft, myOnePieceData);
                
                // 是否还能动
                if(!IfCanContinueMove(client))
                    break;
            }

            // save db
            ModifyOnePieceTreasureData(client, myOnePieceData);
        }

        /// <summary>
        /// 根据MoveCellNum计算距离下一个Event的距离(并不真实移动)
        /// </summary>
        public int CalculateMoveCellToNextEvent(GameClient client, int MoveCellNum, OnePieceTreasureData myOnePieceData)
        {
            if (MoveCellNum == 0)
                return myOnePieceData.PosID;

            OnePieceTreasureMapConfig myTreasureMapConfig = null;
            int posid = myOnePieceData.PosID;

            // 尝试移动movenum步 movenum可正可负
            for (int n = 0; n < Math.Abs(MoveCellNum); ++n)
            {
                // 获得下一步位置
                if (MoveCellNum > 0)
                    posid = GetNextPosIDForEvent(posid, true);
                else
                    posid = GetNextPosIDForEvent(posid, false);

                // 得到此步的格子信息
                if (!TreasureMapConfig.TryGetValue(posid, out myTreasureMapConfig))
                    break;

                // int RealMoveNum = (MoveCellNum > 0) ? (n + 1) : -(n + 1);
                if (myTreasureMapConfig.Trigger == TriggerType.ETT_Pass)
                    return posid;
            }
            return posid;
        }

        /// <summary>
        /// 处理客户端移动 返回实际移动步数
        /// </summary>
        public void HandleOnePieceTreasureMove(GameClient client, int MoveCellNum, OnePieceTreasureData myOnePieceData)
        {
            OnePieceTreasureMapConfig myTreasureMapConfig = null;
            int posid = myOnePieceData.PosID;

            // 尝试移动movenum步 movenum可正可负
            for (int n = 0; n < Math.Abs(MoveCellNum); ++n)
            {
                // 获得下一步位置
                if (MoveCellNum > 0)
                    posid = GetNextPosIDForEvent(myOnePieceData.PosID, true);
                else
                    posid = GetNextPosIDForEvent(myOnePieceData.PosID, false);

                // 得到此步的格子信息
                if (!TreasureMapConfig.TryGetValue(posid, out myTreasureMapConfig))
                    break;

                // 调整位置
                myOnePieceData.PosID = posid;
                
                // 移动步数
                if (MoveCellNum > 0)
                    client.ClientData.OnePieceMoveLeft--;
                else
                    client.ClientData.OnePieceMoveLeft++;

                // 经过触发打断移动
                if (myTreasureMapConfig.Trigger == TriggerType.ETT_Pass)
                    break; // ETT_Pass 类型等待客户端 CMD_SPR_ONEPIECE_MOVE
            }

            // 经过触发
            if (null != myTreasureMapConfig && MoveCellNum != 0)
            {
                OnePieceMoveTrigger(client, ref myOnePieceData, myTreasureMapConfig, TriggerType.ETT_Pass);
            }

            // 单次移动的最后一步
            if (null != myTreasureMapConfig && client.ClientData.OnePieceMoveLeft == 0 && MoveCellNum != 0)
            {
                OnePieceMoveTrigger(client, ref myOnePieceData, myTreasureMapConfig, TriggerType.ETT_Stay);
            }

            // 处理跨层 会发生不耗步数的移动
            myOnePieceData.PosID = FilterPosIDChangeFloor(client, myOnePieceData.PosID);
            // 本次step实际移动步数 MoveCellNum - client.ClientData.OnePieceMoveLeft;
        }

        /// <summary>
        /// GM命令 骰子点数
        /// </summary>
        public void GM_SetDice(GameClient client, int diceType, int newNum)
        {
            OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
            GetOnePieceTreasureData(client, myOnePieceData);

            // 普通骰子
            if (diceType == (int)DiceType.EDT_Normal)
            {
                ModifyOnePieceDice(client, myOnePieceData, diceType, newNum - myOnePieceData.RollNumNormal);
            }
            // 奇迹骰子
            else if (diceType == (int)DiceType.EDT_Miracle)
            {
                ModifyOnePieceDice(client, myOnePieceData, diceType, newNum - myOnePieceData.RollNumMiracle);
            }
            else
            {
                return;
            }
            // save to db
            ModifyOnePieceTreasureData(client, myOnePieceData);
        }

        /// <summary>
        /// GM命令 瞬移
        /// </summary>
        public void GM_SetPosID(GameClient client, int posid)
        {
            // 得到此步的格子信息
            OnePieceTreasureMapConfig myTreasureMapConfig = null;
            if (!TreasureMapConfig.TryGetValue(posid, out myTreasureMapConfig))
                return;

            OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
            GetOnePieceTreasureData(client, myOnePieceData);
            myOnePieceData.PosID = posid;

            // save db
            ModifyOnePieceTreasureData(client, myOnePieceData);

            byte[] bytesData = DataHelper.ObjectToBytes<OnePieceTreasureData>(myOnePieceData);
            GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_SPR_ONEPIECE_GET_INFO);
        }

        /// <summary>
        /// GM命令 打印详细信息
        /// </summary>
        public void GM_PrintTreasureData(GameClient client)
        {
            // 获取藏宝秘境数据
            OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
            OnePieceManager.getInstance().GetOnePieceTreasureData(client, myOnePieceData);

            /**/string strinfo = string.Format("藏宝秘境 位置PosID[{0}] MoveLeft[{1}] RollNumNormal[{2}] RollNumMiracle[{3}] JiFen[{4}] XueZuan[{5}]",
                myOnePieceData.PosID, client.ClientData.OnePieceMoveLeft, myOnePieceData.RollNumNormal, myOnePieceData.RollNumMiracle,
                GameManager.ClientMgr.GetTreasureJiFen(client), GameManager.ClientMgr.GetTreasureXueZuan(client));
            GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, strinfo);
        }

        /// <summary>
        /// 刷新藏宝秘境Log数据
        /// </summary>
        public void UpdateOnePieceTreasureLogDB(GameClient client, OnePieceTreasureLogType LogType, int addValue = 1)
        {
            EventLogManager.AddRoleEvent(client, OpTypes.Trace, OpTags.Building, LogRecordType.IntValue, LogType, addValue);
        }

        #region 指令处理

        /// <summary>
        /// 获得藏宝秘境数据
        /// </summary>
        public bool ProcessOnePieceGetInfoCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {

                HandleRoleLogout(client); // 重新打开界面时尝试向终点移动

                JudgeResetOnePieceTreasureData(client);

                OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
                GetOnePieceTreasureData(client, myOnePieceData);

                byte[] bytesData = DataHelper.ObjectToBytes<OnePieceTreasureData>(myOnePieceData);
                GameManager.ClientMgr.SendToClient(client, bytesData, nID);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 扔奇迹骰子
        /// </summary>
        public bool ProcessOnePieceRollMiracleCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                String strcmd = "";
                int result = (int)OnePieceTreasureErrorCode.OnePiece_Success;

                // 是否移动完毕
                if(IfCanContinueMove(client))
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorMoving;
                    strcmd = string.Format("{0}:{1}", result, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 移动点数
                int MoveNumRoll = Global.SafeConvertToInt32(cmdParams[0]);
                if (MoveNumRoll <= 0 || MoveNumRoll > SystemParamsTreasureDice.Count)
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorParams;
                    strcmd = string.Format("{0}:{1}", result, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查是否重置相关数据
                JudgeResetOnePieceTreasureData(client);

                // 获取藏宝秘境数据
                OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
                GetOnePieceTreasureData(client, myOnePieceData);

                // 检查骰子个数
                if (myOnePieceData.RollNumMiracle < 1)
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorRollNumNotEnough;
                    strcmd = string.Format("{0}:{1}", result, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // clear event cache
                myOnePieceData.EventID = 0;

                client.ClientData.OnePieceMoveLeft = MoveNumRoll;
                client.ClientData.OnePieceMoveDir = MoveNumRoll;

                // 处理移动
                int DestPosID = CalculateMoveCellToNextEvent(client, MoveNumRoll, myOnePieceData);

                // 减少移动次数
                myOnePieceData.RollNumMiracle--;
                ModifyOnePieceTreasureData(client, myOnePieceData);

                // 返回成功
                strcmd = string.Format("{0}:{1}", result, MoveNumRoll);
                client.sendCmd(nID, strcmd);

                // 驱动客户端移动
                strcmd = string.Format("{0}:{1}", result, DestPosID);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_MOVE, strcmd);

                // 处理Log
                UpdateOnePieceTreasureLogDB(client, OnePieceTreasureLogType.TreasureLog_Role);
                UpdateOnePieceTreasureLogDB(client, OnePieceTreasureLogType.TreasureLog_MoveNum, MoveNumRoll);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }
            return false;
        }

        /// <summary>
        /// 扔普通骰子
        /// </summary>
        public bool ProcessOnePieceRollCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                String strcmd = "";
                int result = (int)OnePieceTreasureErrorCode.OnePiece_Success;

                // 是否移动完毕
                if(IfCanContinueMove(client))
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorMoving;
                    strcmd = string.Format("{0}:{1}", result, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查是否重置相关数据
                JudgeResetOnePieceTreasureData(client);

                // 获取藏宝秘境数据
                OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
                GetOnePieceTreasureData(client, myOnePieceData);

                // 检查骰子个数
                if (myOnePieceData.RollNumNormal < 1)
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorRollNumNotEnough;
                    strcmd = string.Format("{0}:{1}", result, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // clear event cache
                myOnePieceData.EventID = 0;

                // 随机移动点数
                int MoveNumRoll = RollMoveNum();
                client.ClientData.OnePieceMoveLeft = MoveNumRoll;
                client.ClientData.OnePieceMoveDir = MoveNumRoll;
      
                // 处理移动
                int DestPosID = CalculateMoveCellToNextEvent(client, MoveNumRoll, myOnePieceData);

                // 减少移动次数
                myOnePieceData.RollNumNormal--;
                ModifyOnePieceTreasureData(client, myOnePieceData);

                // 返回成功
                strcmd = string.Format("{0}:{1}", result, MoveNumRoll);
                client.sendCmd(nID, strcmd);

                // 驱动客户端移动
                strcmd = string.Format("{0}:{1}", result, DestPosID);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ONEPIECE_MOVE, strcmd);

                // 处理Log
                UpdateOnePieceTreasureLogDB(client, OnePieceTreasureLogType.TreasureLog_Role);
                UpdateOnePieceTreasureLogDB(client, OnePieceTreasureLogType.TreasureLog_MoveNum, MoveNumRoll);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }
        /// <summary>
        /// 扔普通骰子
        /// </summary>
        public bool ProcessOnePieceDiceBuyCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                String strcmd = "";
                int result = (int)OnePieceTreasureErrorCode.OnePiece_Success;
                int diceType = Global.SafeConvertToInt32(cmdParams[0]);
                int diceBuyNum = Global.SafeConvertToInt32(cmdParams[1]);

                // 检查参数
                if (diceBuyNum <= 0 || diceBuyNum > OnePiece_DiceMaxNum)
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorRollNumMax;
                    strcmd = string.Format("{0}:{1}:{2}", result, diceType, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 获取藏宝秘境数据
                OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
                GetOnePieceTreasureData(client, myOnePieceData);

                // 计算开销
                int UserMoneyCost = 0;
                if (diceType == (int)DiceType.EDT_Normal)
                {
                    if(myOnePieceData.RollNumNormal + diceBuyNum > OnePiece_DiceMaxNum)
                    {
                        result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorRollNumMax;
                        strcmd = string.Format("{0}:{1}:{2}", result, diceType, 0);
                        client.sendCmd(nID, strcmd);
                        return true;
                    }
                    UserMoneyCost = diceBuyNum * SystemParamsTreasurePrice;
                }
                else if (diceType == (int)DiceType.EDT_Miracle)
                {
                    if(myOnePieceData.RollNumMiracle + diceBuyNum > OnePiece_DiceMaxNum)
                    {
                        result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorRollNumMax;
                        strcmd = string.Format("{0}:{1}:{2}", result, diceType, 0);
                        client.sendCmd(nID, strcmd);
                        return true;
                    }
                    UserMoneyCost = diceBuyNum * SystemParamsTreasureSuperPrice;
                }
                else // 非法的类型
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorParams;
                    strcmd = string.Format("{0}:{1}:{2}", result, diceType, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                if (client.ClientData.UserMoney < UserMoneyCost)
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorZuanShiNotEnough;
                    strcmd = string.Format("{0}:{1}", result, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 扣钻石
                if (UserMoneyCost > 0)
                {
                    if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, UserMoneyCost, "藏宝秘境买骰子"))
                    {
                        result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorZuanShiNotEnough;
                        strcmd = string.Format("{0}:{1}", result, 0);
                        client.sendCmd(nID, strcmd);
                        return true;
                    }
                }

                // 普通骰子
                if (diceType == (int)DiceType.EDT_Normal)
                {
                    UpdateOnePieceTreasureLogDB(client, OnePieceTreasureLogType.TreasureLog_BuyDice, diceBuyNum);
                    myOnePieceData.RollNumNormal += diceBuyNum;
                    strcmd = string.Format("{0}:{1}:{2}", result, diceType, myOnePieceData.RollNumNormal);
                }
                // 奇迹骰子
                else if (diceType == (int)DiceType.EDT_Miracle)
                {
                    UpdateOnePieceTreasureLogDB(client, OnePieceTreasureLogType.TreasureLog_BuySuperDice, diceBuyNum);
                    myOnePieceData.RollNumMiracle += diceBuyNum;
                    strcmd = string.Format("{0}:{1}:{2}", result, diceType, myOnePieceData.RollNumMiracle);
                }

                // save to db
                ModifyOnePieceTreasureData(client, myOnePieceData);

                // 返回成功
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }
        /// <summary>
        /// 宝箱随机奖励
        /// </summary>
        public bool IfHaveOnePieceBoxListAward(GameClient client)
        {
            if (client.ClientData.OnePieceBoxIDList != null)
                return true;

            return false;
        }

        /// <summary>
        /// 是否可以继续移动
        /// </summary>
        public bool IfCanContinueMove(GameClient client)
        {
            // 剩余步数
            if (client.ClientData.OnePieceMoveLeft != 0)
                return true;

            // 宝箱缓存事件
            if (IfHaveOnePieceBoxListAward(client))
                return true;

            return false;
        }

        /// <summary>
        /// 客户端尝试继续移动
        /// </summary>
        public bool ProcessOnePieceMoveCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                String strcmd = "";
                int result = (int)OnePieceTreasureErrorCode.OnePiece_Success;

                // 是否可以继续移动
                if (!IfCanContinueMove(client))
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorMoveNumNotEnough;
                    strcmd = string.Format("{0}:{1}", result, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 获取藏宝秘境数据
                OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
                GetOnePieceTreasureData(client, myOnePieceData);

                // 检查宝箱奖励 是否有打开但未给奖励的宝箱
                if (IfHaveOnePieceBoxListAward(client))
                {
                    result = TryGiveOnePieceBoxListAward(client);

                    // 处理跨层 会发生不耗步数的移动
                    myOnePieceData.PosID = FilterPosIDChangeFloor(client, myOnePieceData.PosID);
                }
                else
                {
                    // 服务器真正开始移动
                    HandleOnePieceTreasureMove(client, client.ClientData.OnePieceMoveLeft, myOnePieceData);
                }

                // 计算下一目标位置
                int DestPosID = myOnePieceData.PosID;
                if (!IfHaveOnePieceBoxListAward(client))
                {
                    DestPosID = CalculateMoveCellToNextEvent(client, client.ClientData.OnePieceMoveLeft, myOnePieceData);
                }

                // save db
                ModifyOnePieceTreasureData(client, myOnePieceData);

                // 返回
                strcmd = string.Format("{0}:{1}", result, DestPosID);
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public bool ProcessOnePieceTriggerEventCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                String strcmd = "";
                int result = (int)OnePieceTreasureErrorCode.OnePiece_Success;

                // 是否移动完毕
                if (client.ClientData.OnePieceMoveLeft != 0)
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorMoving;
                    strcmd = string.Format("{0}:{1}", result, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 获取藏宝秘境数据
                OnePieceTreasureData myOnePieceData = new OnePieceTreasureData();
                GetOnePieceTreasureData(client, myOnePieceData);

                // 检查是否有缓存的事件
                if(myOnePieceData.EventID == 0)
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorNotHaveEvent;
                    strcmd = string.Format("{0}:{1}", result, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                OnePieceTreasureEventConfig myTreasureEventConfig = null;
                if (!TreasureEventConfig.TryGetValue(myOnePieceData.EventID, out myTreasureEventConfig))
                {
                    result = (int)OnePieceTreasureErrorCode.OnePiece_ErrorNotHaveEvent;
                    strcmd = string.Format("{0}:{1}", result, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 触发事件
                result = (int)TriggerEvent(client, myOnePieceData, myTreasureEventConfig);

                if (result == (int)OnePieceTreasureErrorCode.OnePiece_Success ||
                    result == (int)OnePieceTreasureErrorCode.OnePiece_ErrorCheckMail)
                {
                    // clear cache event
                    myOnePieceData.EventID = 0;
                }

                // save to db
                ModifyOnePieceTreasureData(client, myOnePieceData);
                
                // 返回错误码
                strcmd = string.Format("{0}:{1}", result, (int)myTreasureEventConfig.Type);
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        #endregion

        /// <summary>
        /// 初始化配置文件
        /// </summary>
        public bool InitConfig()
        {
            // 控制掷骰子数字概率
            string TreasureDice = GameManager.systemParamsList.GetParamValueByName("TreasureDice");
            if (!string.IsNullOrEmpty(TreasureDice))
            {
                string[] Filed = TreasureDice.Split('|');
                for(int n=0; n<Filed.Length; ++n)
                {
                    string[] StringPair = Filed[n].Split(',');
                    if(StringPair.Length == 2)
                    {
                        SystemParamsTreasureDice.Add(Global.SafeConvertToDouble(StringPair[1]));
                    }
                }
            }

            // 控制每日骰子次数
            string TreasureFreeNum = GameManager.systemParamsList.GetParamValueByName("TreasureFreeNum");
            if (!string.IsNullOrEmpty(TreasureFreeNum))
            {
                string[] Filed = TreasureFreeNum.Split(',');
                if (Filed.Length == 2)
                {
                    SystemParamsTreasureFreeNum = Global.SafeConvertToInt32(Filed[0]);
                    SystemParamsTreasureMiracleNum = Global.SafeConvertToInt32(Filed[1]);
                }
            }

            // 普通骰子价格
            string TreasurePrice = GameManager.systemParamsList.GetParamValueByName("TreasurePrice");
            if (!string.IsNullOrEmpty(TreasurePrice))
            {
                SystemParamsTreasurePrice = Global.SafeConvertToInt32(TreasurePrice);
            }

            // 奇迹骰子价格
            string TreasureSuperPrice = GameManager.systemParamsList.GetParamValueByName("TreasureSuperPrice");
            if (!string.IsNullOrEmpty(TreasureSuperPrice))
            {
                SystemParamsTreasureSuperPrice = Global.SafeConvertToInt32(TreasureSuperPrice);
            }

            if (!LoadOnePieceTreasureMapFile())
                return false;

            if (!LoadOnePieceTreasureEventFile())
                return false;

            if (!LoadOnePieceTreasureBoxFile())
                return false;

            return true;
        }

        #region 初始化配置表

        /// <summary>
        /// 初始化TreasureMap.xml
        /// </summary>
        public bool LoadOnePieceTreasureMapFile()
        {
            try
            {
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(OnePiece_TreasureMapfileName));
                if (null == xml) return false;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null != xmlItem)
                    {
                        OnePieceTreasureMapConfig myTreasureMap = new OnePieceTreasureMapConfig();
                        myTreasureMap.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        myTreasureMap.Num = (int)Global.GetSafeAttributeLong(xmlItem, "Num");
                        myTreasureMap.Floor = (int)Global.GetSafeAttributeLong(xmlItem, "Floor");
                        myTreasureMap.Trigger = (TriggerType)Global.GetSafeAttributeLong(xmlItem, "Trigger");
                        myTreasureMap.Score = (int)Global.GetSafeAttributeLong(xmlItem, "Score");

                        // 填写多个事件，格式：事件ID,概率|事件ID,概率注：事件总概率和为1
                        string RandomEvent = Global.GetSafeAttributeStr(xmlItem, "Event");
                        if (string.IsNullOrEmpty(RandomEvent))
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("读取TreasureMap.xml中的Event失败"));
                        }
                        else
                        {
                            string[] Filed = RandomEvent.Split('|');
                            for(int n=0; n<Filed.Length; ++n)
                            {
                                string[] RatePair = Filed[n].Split(',');
                                if(RatePair.Length == 2)
                                {
                                    OnePieceRandomEvent myRandomEvent = new OnePieceRandomEvent();
                                    myRandomEvent.EventID = Global.SafeConvertToInt32(RatePair[0]);
                                    myRandomEvent.Rate = Global.SafeConvertToDouble(RatePair[1]);
                                    myTreasureMap.LisRandomEvent.Add(myRandomEvent);
                                }
                            }
                        }

                        // set
                        TreasureMapConfig[myTreasureMap.ID] = myTreasureMap;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", "TreasureMap.xml", ex.Message));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化TreasureEvent.xml
        /// </summary>
        public bool LoadOnePieceTreasureEventFile()
        {
            try
            {
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(OnePiece_TreasureEventfileName));
                if (null == xml) return false;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null != xmlItem)
                    {
                        OnePieceTreasureEventConfig myTreasureEvent = new OnePieceTreasureEventConfig();
                        myTreasureEvent.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        myTreasureEvent.Type = (TreasureEventType)Global.GetSafeAttributeLong(xmlItem, "Type");

                        // 物品奖励
                        string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "NewGoods");
                        if (!string.IsNullOrEmpty(goodsIDs))
                        {
                            ConfigParser.ParseAwardsItemList(goodsIDs, ref myTreasureEvent.GoodsList);
                        }

                        // 金钱奖励
                        string MoneyAward = Global.GetSafeAttributeStr(xmlItem, "NewValue");
                        if(!string.IsNullOrEmpty(MoneyAward))
                        {
                            string[] Filed = MoneyAward.Split(',');
                            if(Filed.Length == 2)
                            {
                                myTreasureEvent.NewValue.Type = (MoneyTypes)Global.SafeConvertToInt32(Filed[0]);
                                myTreasureEvent.NewValue.Num = Global.SafeConvertToInt32(Filed[1]);
                            }
                        }

                        // 检查配置
                        if(string.IsNullOrEmpty(goodsIDs) && string.IsNullOrEmpty(MoneyAward))
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("读取TreasureEvent.xml奖励配置项1失败"));
                        }

                        // NeedGoods
                        string NeedGoods = Global.GetSafeAttributeStr(xmlItem, "NeedGoods");
                        if (!string.IsNullOrEmpty(MoneyAward))
                        {
                            string[] Filed = MoneyAward.Split('|');
                            for(int n=0; n<Filed.Length; ++n)
                            {
                                string[] GoodsPairFiled = Filed[n].Split(',');
                                if(GoodsPairFiled.Length == 2)
                                {
                                    OnePieceGoodsPair myGoodsPair = new OnePieceGoodsPair();
                                    myGoodsPair._NeedGoodsID = (int)Global.SafeConvertToInt32(GoodsPairFiled[0]);
                                    myGoodsPair._NeedGoodsCount = (int)Global.SafeConvertToInt32(GoodsPairFiled[1]);
                                    myTreasureEvent.NeedGoods.Add(myGoodsPair);
                                }
                            }
                        }

                        // NeedValue
                        string NeedValue = Global.GetSafeAttributeStr(xmlItem, "NeedValue");
                        if (!string.IsNullOrEmpty(NeedValue))
                        {
                            string[] Filed = NeedValue.Split(',');
                            if(Filed.Length == 2)
                            {
                                myTreasureEvent.NeedValue.Type = (MoneyTypes)Global.SafeConvertToInt32(Filed[0]);
                                myTreasureEvent.NeedValue.Num = Global.SafeConvertToInt32(Filed[1]);
                            }
                        }

                        // Move
                        string Move = Global.GetSafeAttributeStr(xmlItem, "Move");
                        if(!string.IsNullOrEmpty(Move))
                        {
                            string[] Filed = Move.Split(',');
                            for(int n=0; n<Filed.Length; ++n)
                            {
                                myTreasureEvent.MoveRange.Add(Global.SafeConvertToInt32(Filed[n]));
                            }
                        }

                        myTreasureEvent.NewDiec = (int)Global.GetSafeAttributeLong(xmlItem, "NewDiec");
                        myTreasureEvent.NewSuperDiec = (int)Global.GetSafeAttributeLong(xmlItem, "NewSuperDiec");

                        // FuBenID
                        myTreasureEvent.FuBenID = (int)Global.GetSafeAttributeLong(xmlItem, "FuBenID");

                        // 宝箱
                        string TreasureBox = Global.GetSafeAttributeStr(xmlItem, "Box");
                        if(!string.IsNullOrEmpty(TreasureBox))
                        {
                            string[] Filed = TreasureBox.Split('|');
                            for(int n=0; n<Filed.Length; ++n)
                            {
                                string[] BoxPair = Filed[n].Split(',');
                                if(BoxPair.Length == 2)
                                {
                                    OnePieceTreasureBoxPair myBox = new OnePieceTreasureBoxPair();
                                    myBox.BoxID = Global.SafeConvertToInt32(BoxPair[0]);
                                    myBox.OpenNum = Global.SafeConvertToInt32(BoxPair[1]);
                                    myTreasureEvent.BoxList.Add(myBox);
                                }
                            }
                        }

                        // set
                        TreasureEventConfig[myTreasureEvent.ID] = myTreasureEvent;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", "TreasureEvent.xml", ex.Message));
                //return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化TreasureBox.xml
        /// </summary>
        public bool LoadOnePieceTreasureBoxFile()
        {
            try
            {
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(OnePiece_TreasureBoxfileName));
                if (null == xml) return false;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems) // Box
                {
                    if (null == xmlItem)
                        continue;

                    List<OnePieceTreasureBoxConfig> RandomTreasureBox = new List<OnePieceTreasureBoxConfig>();
                    int BoxID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");

                    IEnumerable<XElement> xmlBoxItems = xmlItem.Elements();
                    foreach (var xmlBoxItem in xmlBoxItems) // Treasure
                    {
                        OnePieceTreasureBoxConfig myTreasureBox = new OnePieceTreasureBoxConfig();
                        myTreasureBox.ID = (int)Global.GetSafeAttributeLong(xmlBoxItem, "ID");
                        myTreasureBox.Type = (TeasureBoxType)Global.GetSafeAttributeLong(xmlBoxItem, "Type");

                        // 奖励数值
                        string goodsIDs = Global.GetSafeAttributeStr(xmlBoxItem, "Goods");
                        if (string.IsNullOrEmpty(goodsIDs))
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("读取TreasureBox.xml奖励配置项1失败"));
                        }
                        else
                        {
                            string[] Filed = goodsIDs.Split(','); // 7位道具 0,0,0,0,0,0,0
                            if(Filed.Length != 1)
                            {
                                ConfigParser.ParseAwardsItemList(goodsIDs, ref myTreasureBox.Goods);
                            }
                            else // 单一数值
                            {
                                myTreasureBox.Num = Global.SafeConvertToInt32(goodsIDs);
                            }
                        }

                        //
                        myTreasureBox.BeginNum = (int)Global.GetSafeAttributeLong(xmlBoxItem, "BeginNum");
                        myTreasureBox.EndNum = (int)Global.GetSafeAttributeLong(xmlBoxItem, "EndNum");
                        RandomTreasureBox.Add(myTreasureBox);
                    }

                    // Set
                    TreasureBoxConfig[BoxID] = RandomTreasureBox;
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", "TreasureBox.xml", ex.Message));
                return false;
            }

            return true;
        }

        #endregion

    }

}