using GameServer.Core.Executor;
using GameServer.Logic.TuJian;
using GameServer.Server;
using GameServer.Tools;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic.Today
{
    public class TodayManager : ICmdProcessorEx, IManager
    {
        #region ----------接口

        private static TodayManager instance = new TodayManager();
        public static TodayManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            InitConfig();

            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TODAY_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TODAY_AWARD, 2, 2, getInstance());

            return true;
        }

        public bool showdown() { return true; }
        public bool destroy() { return true; }
        public bool processCmd(GameClient client, string[] cmdParams) { return true; }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_TODAY_DATA:
                    return ProcessCmdTodayData(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TODAY_AWARD:
                    return ProcessCmdTodayAward(client, nID, bytes, cmdParams);
            }

            return true;
        }

        private bool ProcessCmdTodayData(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                string result = GetTodayData(client);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TODAY_DATA, result);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        private bool ProcessCmdTodayAward(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 2);
                if (!isCheck) return false;

                bool isAll = int.Parse(cmdParams[0]) > 0;
                int todayID = int.Parse(cmdParams[1]);

                string result = TodayAward(client, isAll, todayID);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TODAY_AWARD, result);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        #endregion

        #region ----------功能

        private string GetTodayData(GameClient client)
        {
            string result = "{0}:{1}";
            if (!IsGongNengOpened())
                return string.Format(result, (int)ETodayState.NotOpen, 0);

            List<TodayInfo> list = InitToday(client);
            if (list.IsNullOrEmpty())
                return string.Format(result, (int)ETodayState.NotOpen, 0);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; )
            {
                sb.Append(string.Format("{0}*{1}", list[i].ID, list[i].NumEnd));
                i++;
                if (i < list.Count) sb.Append("|");
            }

            return string.Format(result, (int)ETodayState.Success, sb.ToString());
        }

        private string TodayAward(GameClient client, bool isAll, int todayID)
        {
            #region 验证
            string result = "{0}:{1}";
            if (!IsGongNengOpened())
                return string.Format(result, (int)ETodayState.NotOpen, 0);

            //单个领取
            TodayInfo oneInfo = null;
            if (!isAll)
            {
                oneInfo = GetTadayInfoByID(client, todayID);
                if (oneInfo == null)
                    return string.Format(result, (int)ETodayState.NoType, 0);

                if ((oneInfo.NumMax - oneInfo.NumEnd) <= 0)
                    return string.Format(result, (int)ETodayState.IsAward, 0);
            }

            //获取可领取数据
            List<TodayInfo> listAll = new List<TodayInfo>(); ;
            if (isAll)
                listAll = InitToday(client);
            else
                listAll.Add(oneInfo);

            if (listAll.IsNullOrEmpty())
                return string.Format(result, (int)ETodayState.NoType, 0);

            //在副本中，不能领取client.ClientData.CopyMapID
            var fubenList = from info in listAll
                            where info.FuBenID > 0 && client.ClientData.FuBenID > 0 && client.ClientData.FuBenID == info.FuBenID
                                && info.NumMax - info.NumEnd > 0
                            select info;

            if (fubenList.Any())
                return string.Format(result, (int)ETodayState.IsFuben, 0);

            //可以领取
            var awardList = from info in listAll
                            where info.NumMax - info.NumEnd > 0
                            select info;

            if (!awardList.Any())
                return string.Format(result, (int)ETodayState.IsAllAward, 0);

            //奖励物品数量
            int goodsCount = 0;
            foreach (var info in awardList)
                goodsCount += info.AwardInfo.GoodsList.Count;

            //背包
            if (!Global.CanAddGoodsNum(client, goodsCount))
                return string.Format(result, (int)ETodayState.NoBag, 0);

            #endregion

            #region 设置次数
            bool b = false;
            foreach (var info in awardList)
            {
                SystemXmlItem fuBenInfo = null;
                if (info.Type == (int)(ETodayType.Tao))
                {
                    TaskData taskData = GetTaoTask(client);
                    if (taskData != null)
                    {
                        b = Global.CancelTask(client, taskData.DbID, taskData.DoingTaskID);
                        if (!b) return string.Format(result, (int)ETodayState.TaoCancel, 0);
                    }
                }
                else if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(info.FuBenID, out fuBenInfo))
                {
                    return string.Format(result, (int)ETodayState.EFubenConfig, 0);
                }

                b = SetFinishNum(client,info,fuBenInfo);
                if(!b) return string.Format(result, (int)ETodayState.Fail, 0);
            }

            #endregion

            #region 发奖
            TodayAwardInfo awardInfo = new TodayAwardInfo();
            //(物品)
            foreach (var info in awardList)
            {
                int num = info.NumMax - info.NumEnd;
                for (int i = 0; i < info.AwardInfo.GoodsList.Count; i++)
                {
                    GoodsData goods = info.AwardInfo.GoodsList[i];
                    Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                        goods.GoodsID, goods.GCount * num, goods.Quality, "", goods.Forge_level,
                        goods.Binding, 0, "", true, 1,
                        /**/"每日专享", Global.ConstGoodsEndTime, goods.AddPropIndex, goods.BornIndex,
                        goods.Lucky, goods.Strong, goods.ExcellenceInfo, goods.AppendPropLev);
                }

                awardInfo.AddAward(info.AwardInfo, num);
            } 

            //(数值)
            //经验值
            if (awardInfo.Exp > 0)
            {
                GameManager.ClientMgr.ProcessRoleExperience(client, (long)awardInfo.Exp);
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                            StringUtil.substitute(Global.GetLang("恭喜获得经验 +{0}"), awardInfo.Exp),
                                                            GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlyErr, (int)HintErrCodeTypes.None);
            }

            //绑金
            if (awardInfo.GoldBind > 0) GameManager.ClientMgr.AddMoney1(client, (int)awardInfo.GoldBind, "每日专享", true);
            //魔晶
            if (awardInfo.MoJing > 0) GameManager.ClientMgr.ModifyTianDiJingYuanValue(client, (int)awardInfo.MoJing, "每日专享", true, true); 
            //成就
            if (awardInfo.ChengJiu > 0) GameManager.ClientMgr.ModifyChengJiuPointsValue(client, (int)awardInfo.ChengJiu, "每日专享", true, true); 
            //声望
            if (awardInfo.ShengWang > 0) GameManager.ClientMgr.ModifyShengWangValue(client, (int)awardInfo.ShengWang, "每日专享", true, true); ;
            //战功
            if (awardInfo.ZhanGong > 0)
            {
                int zhanGong = (int)awardInfo.ZhanGong;
                GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ref zhanGong, AddBangGongTypes.Today);
            }
            //绑钻
            if (awardInfo.DiamondBind > 0 || awardInfo.ExtDiamondBind > 0) GameManager.ClientMgr.AddUserGold(client, (int)(awardInfo.DiamondBind + awardInfo.ExtDiamondBind), "每日专享"); ;
            //星魂
            if (awardInfo.XingHun > 0) GameManager.ClientMgr.ModifyStarSoulValue(client, (int)awardInfo.XingHun, "每日专享", true, true); ;
            //元素粉末
            if (awardInfo.YuanSuFenMo > 0) GameManager.ClientMgr.ModifyYuanSuFenMoValue(client, (int)awardInfo.YuanSuFenMo, "每日专享", true); ;
            //守护点数
            if (awardInfo.ShouHuDianShu > 0) GuardStatueManager.Instance().AddGuardPoint(client, (int)awardInfo.ShouHuDianShu, "每日专享");
            //再造点数
            if (awardInfo.ZaiZao > 0) GameManager.ClientMgr.ModifyZaiZaoValue(client, (int)awardInfo.ZaiZao, "每日专享", true, true); 
            //灵晶
            if (awardInfo.LingJing > 0) GameManager.ClientMgr.ModifyMUMoHeValue(client, (int)awardInfo.LingJing, "每日专享", true); ;
            //荣耀
            if (awardInfo.RongYao > 0) GameManager.ClientMgr.ModifyTianTiRongYaoValue(client, (int)awardInfo.RongYao, "每日专享"); ;

            #endregion

            return GetTodayData(client);
        }

        #endregion

        #region ----------其他

        private List<TodayInfo> InitToday(GameClient client)
        {
            List<TodayInfo> infoList = new List<TodayInfo>();

            int level = client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level;
            int taskID = client.ClientData.MainTaskID;

            var tempList = from info in _todayInfoList
                           where level >= info.LevelMin && level <= info.LevelMax && taskID >= info.TaskMin
                           select info;

            foreach (var t in tempList)
            {
                TodayInfo info = new TodayInfo(t);
                info.NumEnd = GetFinishNum(client, info);
                info.NumMax = GetMaxNum(client, info);

                infoList.Add(info);
            }

            return infoList;
        }

        private TodayInfo GetTadayInfoByID(GameClient client, int id)
        {
            TodayInfo result = null;

            int taskID = client.ClientData.MainTaskID;
            int level = client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level;
            var temp = from info in _todayInfoList
                                  where info.ID == id && level >= info.LevelMin && level <= info.LevelMax && taskID >= info.TaskMin
                                select info;

            if (!temp.Any()) return null;

            TodayInfo tempInfo = temp.First();
            if (tempInfo != null)
            {
                result = new TodayInfo(tempInfo);
                result.NumEnd = GetFinishNum(client, result);
                result.NumMax = GetMaxNum(client, result);
            }

            return result;
        }

        private int GetMaxNum(GameClient client, TodayInfo todayInfo)
        {
            int num = 0;
            int[] arr = null;
            switch ((ETodayType)todayInfo.Type)
            {
                case ETodayType.Exp:
                    //arr = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJinYanFuBenNum");
                    //num = arr[client.ClientData.VipLevel] + todayInfo.NumMax;
                    num = todayInfo.NumMax;
                    break;
                case ETodayType.Gold:
                    //arr = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJinBiFuBenNum");
                    //num = arr[client.ClientData.VipLevel] + todayInfo.NumMax;
                    num = todayInfo.NumMax;
                    break;
                case ETodayType.KaLiMa:
                case ETodayType.Lo:
                case ETodayType.EM:
                    num = todayInfo.NumMax;
                    break;
                case ETodayType.Tao:
                    DailyTaskData dailyTaskData = Global.FindDailyTaskDataByTaskClass(client, (int)TaskClasses.TaofaTask);
                    if (null == dailyTaskData) num = Global.MaxTaofaTaskNumForMU;
                    else num = Global.GetMaxDailyTaskNum(client, (int)TaskClasses.TaofaTask, dailyTaskData);
                    break;
            }

            return Math.Max(0,num);
        }

        private int GetFinishNum(GameClient client, TodayInfo todayInfo)
        {
            int num = 0;
            FuBenData fuBenData = GetFuBenData(client, todayInfo.FuBenID);

            switch ((ETodayType)todayInfo.Type)
            {
                case ETodayType.Exp:  
                case ETodayType.Gold:
                    num = fuBenData.EnterNum;
                    break;
                case ETodayType.KaLiMa:
                case ETodayType.Lo:
                case ETodayType.EM:
                    num = fuBenData.FinishNum;
                    break;            
                case ETodayType.Tao:
                    DailyTaskData dailyTaskData = Global.FindDailyTaskDataByTaskClass(client, (int)TaskClasses.TaofaTask);
                    num = dailyTaskData == null ? 0 : dailyTaskData.RecNum;
                    break;
            }

            return Math.Max(0, num);
        }

        private FuBenData GetFuBenData(GameClient client, int fuBenID)
        {
            bool isNotify = false;
            FuBenData fuBenData = Global.GetFuBenData(client, fuBenID);

            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (null == fuBenData)
                fuBenData = Global.AddFuBenData(client, fuBenID, dayID, 0, 0, 0);
        
            if (fuBenData.DayID != dayID)
            {
                fuBenData.DayID = dayID;
                fuBenData.EnterNum = 0;
                fuBenData.FinishNum = 0;

                isNotify = true;
            }

            //将新的副本的数据通知自己
            if(isNotify) GameManager.ClientMgr.NotifyFuBenData(client, fuBenData);

            return fuBenData;
        }

        private bool SetFinishNum(GameClient client, TodayInfo todayInfo, SystemXmlItem fuBenInfo)
        {
            int num = todayInfo.NumMax - todayInfo.NumEnd;
            switch ((ETodayType)todayInfo.Type)
            {
                case ETodayType.Exp:
                case ETodayType.Gold:
                    Global.UpdateFuBenData(client, todayInfo.FuBenID, num, num);
                    break;
                case ETodayType.KaLiMa:
                case ETodayType.EM:
                case ETodayType.Lo:
                    Global.UpdateFuBenData(client, todayInfo.FuBenID, num, num);
                    break;
                case ETodayType.Tao:
                    {
                        DailyTaskData taoData = null;
                        Global.GetDailyTaskData(client, (int)TaskClasses.TaofaTask, out taoData, true);

                        taoData.RecNum = todayInfo.NumMax;
                        Global.UpdateDBDailyTaskData(client, taoData, true);
                    }
                    break;
            }

            FuBenData fuBenData = Global.GetFuBenData(client, todayInfo.FuBenID);
            if (fuBenData != null && (fuBenData.EnterNum != 0 || fuBenData.FinishNum != 0))
            {
                //记录通关副本数量
                int dayID = TimeUtil.NowDateTime().DayOfYear;
                RoleDailyData roleData = client.ClientData.MyRoleDailyData;
                if (null == roleData || dayID != roleData.FuBenDayID)
                {
                    roleData = new RoleDailyData();
                    roleData.FuBenDayID = dayID;
                    client.ClientData.MyRoleDailyData = roleData;
                }

                int count = todayInfo.NumMax - todayInfo.NumEnd;
                roleData.TodayFuBenNum += count;

                int level = fuBenInfo.GetIntValue("FuBenLevel");
                DailyActiveManager.ProcessCompleteCopyMapForDailyActive(client, level, count);//活跃              
                ChengJiuManager.ProcessCompleteCopyMapForChengJiu(client, level, count); //成就
            }

            return true;
        }

        public static TaskData GetTaoTask(GameClient client)
        {
            if (null == client.ClientData.TaskDataList) return null;

            lock (client.ClientData.TaskDataList)
            {
                for (int i = 0; i < client.ClientData.TaskDataList.Count; i++)
                {
                    TaskData taskData = client.ClientData.TaskDataList[i];

                    SystemXmlItem systemTask = null;
                    if (GameManager.SystemTasksMgr.SystemXmlItemDict.TryGetValue(taskData.DoingTaskID, out systemTask))
                    {
                        int taskClass = systemTask.GetIntValue("TaskClass");
                        if (taskClass == (int)TaskClasses.TaofaTask)
                            return taskData;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 判断功能是否开启
        /// </summary>
        public bool IsGongNengOpened()
        {
            // 如果1.7的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                return false;

            if (GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.Today))
                return true;

            return false;
        }

        #endregion

        #region ----------配置

        private static List<TodayInfo> _todayInfoList = new List<TodayInfo>();

        private static void InitConfig()
        {
            string fileName = Global.GameResPath("Config/JianFu.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _todayInfoList.Clear();
                 string[] fields;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    TodayInfo info = new TodayInfo();
                    info.ID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    info.Type = info.ID / 100;
                    info.Name = Global.GetDefAttributeStr(xmlItem, "Name", "0");
                    info.FuBenID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "FuBenID", "0"));
                    info.HuoDongID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "HuoDongID", "0"));
                    info.LevelMin = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MinZhuanSheng", "0")) * 100 + Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MinLevel", "0")); ;
                    info.LevelMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MaxZhuanSheng", "0")) * 100 + Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MaxLevel", "0")); ;
                    info.TaskMin = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MinTasks", "0"));
                    info.NumMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Num", "0"));

                    TodayAwardInfo awardInfo = new TodayAwardInfo();
                    awardInfo.Exp = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "Exp", "0"));
                    awardInfo.GoldBind = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "BandJinBi", "0"));
                    awardInfo.MoJing = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "MoJing", "0"));
                    awardInfo.ChengJiu = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "ChengJiu", "0"));
                    awardInfo.ShengWang = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "ShengWang", "0"));
                    awardInfo.ZhanGong = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "ZhanGong", "0"));
                    awardInfo.DiamondBind = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "BandZuanShi", "0"));
                    awardInfo.XingHun = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "XingHun", "0"));
                    awardInfo.YuanSuFenMo = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "YuanSuFenMo", "0"));
                    awardInfo.ShouHuDianShu = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "ShouHuDianShu", "0"));
                    awardInfo.ZaiZao = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "ZaiZao", "0"));
                    awardInfo.LingJing = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "LingJing", "0"));
                    awardInfo.RongYao = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "RongYao", "0"));
                    awardInfo.ExtDiamondBind = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "ExtraBandZuanShi", "0"));

                    string goods = Global.GetDefAttributeStr(xmlItem, "Goods", "0");
                    if (!string.IsNullOrEmpty(goods) && !goods.Equals("0"))
                    {
                         fields = goods.Split('|');
                         awardInfo.GoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                    }

                    info.AwardInfo = awardInfo;
                    _todayInfoList.Add(info);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }
        }

       

        #endregion
    }
}
