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
    /// <summary>
    /// 周末充值奖励ZhouMoChongZhiType.xml
    /// </summary>
    public class WeekEndInputTypeData
    {
        /// <summary>
        /// 最小转生数
        /// </summary>
        public int MinZhuanSheng = 0;

        /// <summary>
        /// 最小等级
        /// </summary>
        public int MinLevel = 0;

        /// <summary>
        /// 最大转生数
        /// </summary>
        public int MaxZhuanSheng = 0;

        /// <summary>
        /// 最大等级
        /// </summary>
        public int MaxLevel = 0;

        /// <summary>
        /// 最小钻石数
        /// </summary>
        public int MinZuanShi = 0;

        /// <summary>
        /// 奖励数量
        /// </summary>
        public int Num = 0;
    }

    /// <summary>
    /// 周末充值奖励ZhouMoChongZhi.xml
    /// </summary>
    public class WeekEndInputAwardData : AwardItem
    {
        /// <summary>
        /// 配置表中的ID
        /// </summary>
        public int id = 0;

        /// <summary>
        /// 随机数起点
        /// </summary>
        public int RandBeginNum = 0;

        /// <summary>
        /// 随机数终点
        /// </summary>
        public int RandEndNum = 0;

        /// <summary>
        /// RandEndNum - RandEndNum + 1 计算随机时的辅助变量
        /// </summary>
        public int RandNumMinus = 0;

        /// <summary>
        /// 随机忽略 计算随机时的辅助变量
        /// </summary>
        public bool RandSkip = false;
    }

    /// <summary>
    /// 周末充值活动
    /// </summary>
    public class WeedEndInputActivity : Activity
    {
        // 周末充值类型数据
        // ID vs WeekEndInputTypeData ZhouMoChongZhiType.xml
        protected Dictionary<int, WeekEndInputTypeData> InputTypeDict = new Dictionary<int, WeekEndInputTypeData>();

        // 周末充值奖励数据
        // ID vs WeekEndInputAwardData ZhouMoChongZhi.xml
        protected Dictionary<int, List<WeekEndInputAwardData>> AwardItemDict = new Dictionary<int, List<WeekEndInputAwardData>>();

        /// <summary>
        /// 检查是否在活动持续时间内
        /// </summary>
        public override bool InActivityTime()
        {
            if (string.IsNullOrEmpty(FromDate) || string.IsNullOrEmpty(ToDate))
                return false;

            int NowDayOfWeek = (int)TimeUtil.NowDateTime().DayOfWeek;
            string[] DataBeginSplit = FromDate.Split(',');
            string[] DataEndSplit = ToDate.Split(',');

            // 将DayOfWeek 0~6 转换成 1~7
            if (DayOfWeek.Sunday == (DayOfWeek)NowDayOfWeek)
            {
                NowDayOfWeek = 7;
            }

            int BeginDayOfWeek = Convert.ToInt32(DataBeginSplit[0]);
            int EndDayOfWeek = Convert.ToInt32(DataEndSplit[0]);
            if (NowDayOfWeek < BeginDayOfWeek)
            {
                return false;
            }
            else if (NowDayOfWeek > EndDayOfWeek)
            {
                return false;
            }

            string nowTime = TimeUtil.NowDateTime().ToString("HH:mm:ss");

            // 比较时分秒
            if (BeginDayOfWeek == EndDayOfWeek)
            {
                if (nowTime.CompareTo(DataBeginSplit[1]) > 0 && nowTime.CompareTo(DataEndSplit[1]) < 0)
                    return true;
            }
            else if (NowDayOfWeek == BeginDayOfWeek)
            {
                if (nowTime.CompareTo(DataBeginSplit[1]) > 0)
                    return true;
            }
            else if (NowDayOfWeek == EndDayOfWeek)
            {
                if (nowTime.CompareTo(DataEndSplit[1]) < 0)
                    return true;
            }

            // NowDayOfWeek > BeginDayOfWeek && NowDayOfWeek < EndDayOfWeek && BeginDayOfWeek != EndDayOfWeek
            return true;
        }

        /// <summary>
        /// 检查是否在领取期
        /// </summary>
        public override bool InAwardTime()
        {
            if (string.IsNullOrEmpty(FromDate) || string.IsNullOrEmpty(ToDate))
                return false;

            return InActivityTime();
        }

        /// <summary>
        /// 返回参数有效性验证码, 大于0 表示有效，小于0表示错误代码,派生类可进一步验证其他参数,当参数错误时，会记录日志
        /// </summary>
        /// <returns></returns>
        public override int GetParamsValidateCode()
        {
            return 1;
        }

        /// <summary>
        /// 处理角色上线、跨天
        /// </summary>
        public void OnRoleLogin(GameClient client, bool isLogin)
        {
            // 是否在活动时间
            if(!InActivityTime())        
                return;
            
            // 今天是否随机过有效的周末充值数据
            int currday = Global.GetOffsetDay(TimeUtil.NowDateTime());
            int lastday = 0;
            int count = 0;

            string strFlag = RoleParamName.WeekEndInputFlag;
            String WeekEndInputRandData = Global.GetRoleParamByName(client, strFlag);
            
            // day:count
            if (null != WeekEndInputRandData)
            {
                string[] fields = WeekEndInputRandData.Split('#');
                if (fields.Length == 2)
                {
                    lastday = Convert.ToInt32(fields[0]);
                    if (currday == lastday) // 今天随机过相关奖励信息了
                        return;
                }  
            }

            // 重新随机相关奖励信息存DB
            string result = string.Format("{0}", currday);
            result += '#';
            result += BuildRandAwardData(client);
            Global.SaveRoleParamsStringToDB(client, strFlag, result, true);

            // 跨天主动同步数据
            if (false == isLogin)
            {
              // SyncWeekEndInputData(client);
            }
        }

        /// <summary>
        /// 主动同步随机数据
        /// </summary>
        public void SyncWeekEndInputData(GameClient client)
        {
            string strcmd = "";
            // 主动同步数据
            string strFlag = RoleParamName.WeekEndInputFlag;
            String WeekEndInputRandData = Global.GetRoleParamByName(client, strFlag);
            if (string.IsNullOrEmpty(WeekEndInputRandData))
            {
                strcmd = string.Format("{0}:{1}", -1, 0);
            }
            else
            {
                //"Day|WhitchOne$RewardID$id,id,id,id|WhitchOne$RewardID$id,id,id,id|WhitchOne$RewardID$id,id,id,id"
                string[] InputRandData = WeekEndInputRandData.Split('#');
                if (InputRandData.Length == 2) // 获得周末充值随机奖励数据
                {
                    strcmd = string.Format("{0}:{1}", 0, InputRandData[1]);
                }
            }
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_GETWEEKEND_INPUT_DATA, strcmd);
        }

        /// <summary>
        /// 获取周末充值奖励UI打开日期
        /// </summary>
        public int GetWeekEndInputOpenDay(GameClient client)
        {
            int OpenDay = 0;
            string strFlag = RoleParamName.WeekEndInputOpenDay;
            String WeekEndInputOpenDay = Global.GetRoleParamByName(client, strFlag);
            if (!string.IsNullOrEmpty(WeekEndInputOpenDay))
            {
                OpenDay = Convert.ToInt32(WeekEndInputOpenDay);
            }
            return OpenDay;
        }

        /// <summary>
        /// 更新周末充值奖励UI打开日期
        /// </summary>
        public void UpdateWeekEndInputOpenDay(GameClient client)
        {
            if (!InAwardTime())
                return;

            int currday = Global.GetOffsetDay(TimeUtil.NowDateTime());
            string strFlag = RoleParamName.WeekEndInputOpenDay;
            Global.SaveRoleParamsStringToDB(client, strFlag, Convert.ToString(currday), true);
        }

        /// <summary>
        /// 给奖励
        /// </summary>
        public override bool GiveAward(GameClient client, Int32 NeedYuanBao)
        {
            if (!InAwardTime())
                return false;

            string strFlag = RoleParamName.WeekEndInputFlag;
            String WeekEndInputRandData = Global.GetRoleParamByName(client, strFlag);

            //
            string[] InputRandData = WeekEndInputRandData.Split('#');
            if (InputRandData.Length < 2)
                return false;

            // 奖品数据 "WhitchOne$RewardID$id,id,id,id|WhitchOne$RewardID$id,id,id,id|WhitchOne$RewardID$id,id,id,id"
            string[] AwardArray = InputRandData[1].Split('|');
            if (AwardArray.Length <= 0)
                return false;

            // 根据元宝限制找到对应的AwardID给奖励
            for (int n = 0; n < AwardArray.Length; ++n)
            {
                string awarditem = AwardArray[n];
                string[] award = awarditem.Split('$');

                int AwardID = Convert.ToInt32(award[1]);
                WeekEndInputTypeData InputType = null;
                InputTypeDict.TryGetValue(AwardID, out InputType);
                if (null == InputType || InputType.MinZuanShi != NeedYuanBao)
                {
                    continue;
                }
                     
                List<WeekEndInputAwardData> AwardList = null;
                AwardItemDict.TryGetValue(AwardID, out AwardList);
                if (null != AwardList)
                {
                    string[] arrayid = award[2].Split(',');
                    for (int i = 0; i < arrayid.Length; ++i)
                    {
                        int id = Convert.ToInt32(arrayid[i]);
                        if (id > 0 && id <= AwardList.Count)
                        {
                            GiveAward(client, AwardList[id-1]); // 给奖励
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 获得指定档次的奖励需要的背包空间
        /// </summary>
        public int GetNeedGoodsSpace(GameClient client, Int32 NeedYuanBao)
        {
            if (!InAwardTime())
                return 0;

            string strFlag = RoleParamName.WeekEndInputFlag;
            String WeekEndInputRandData = Global.GetRoleParamByName(client, strFlag);

            //
            string[] InputRandData = WeekEndInputRandData.Split('#');
            if (InputRandData.Length < 2)
                return 0;

            // 奖品数据 "WhitchOne$RewardID$id,id,id,id|WhitchOne$RewardID$id,id,id,id|WhitchOne$RewardID$id,id,id,id"
            string[] AwardArray = InputRandData[1].Split('|');
            if (AwardArray.Length <= 0)
                return 0;

            // 根据元宝限制找到对应的AwardID给奖励
            for (int n = 0; n < AwardArray.Length; ++n)
            {
                string awarditem = AwardArray[n];
                string[] award = awarditem.Split('$');

                int AwardID = Convert.ToInt32(award[1]);
                WeekEndInputTypeData InputType = null;
                InputTypeDict.TryGetValue(AwardID, out InputType);
                if (null != InputType && InputType.MinZuanShi == NeedYuanBao)
                {
                    return InputType.Num;
                }
            }

            return 0;
        }

        /// <summary>
        /// 随机相关奖励项
        /// </summary>
        public string BuildRandAwardData(GameClient client)
        {
            string strResult = ""; // WhitchOne$RewardID$id,id,id,id|WhitchOne$RewardID$id,id,id,id|WhitchOne$RewardID$id,id,id,id
            if (!InActivityTime())
                return strResult;

            //
            MeiRiChongZhiActivity actMeiRi = HuodongCachingMgr.GetMeiRiChongZhiActivity();
            if (null == actMeiRi)
                return strResult;

            //KeyValuePair id vs num
            List<KeyValuePair<int, WeekEndInputTypeData>> RewardTypeList = new List<KeyValuePair<int, WeekEndInputTypeData>>();

            int nChangeLifeCount = client.ClientData.ChangeLifeCount;
            int nLev = client.ClientData.Level;

            // 根据等级、转生次数筛选出符合条件的奖励ID
            foreach (var kvp in InputTypeDict)
            {
                if (kvp.Value.MaxLevel >= nLev && kvp.Value.MinLevel <= nLev
                    && kvp.Value.MaxZhuanSheng >= nChangeLifeCount && kvp.Value.MinZhuanSheng <= nChangeLifeCount)
                {
                    RewardTypeList.Add(new KeyValuePair<int, WeekEndInputTypeData>(kvp.Key, kvp.Value));
                }
            }


            // 根据RewardIDList开始随机相关数据
            foreach (KeyValuePair<int, WeekEndInputTypeData> kvp in RewardTypeList)
            {
                List<WeekEndInputAwardData> AwardList = null;
                AwardItemDict.TryGetValue(kvp.Key, out AwardList);
                if (null != AwardList)
                {
                    // 找到对应的充值档次
                    int WhitchOne = actMeiRi.GetIDByYuanBao(kvp.Value.MinZuanShi);
                    strResult += WhitchOne;
                    strResult += "$";

                    strResult += kvp.Key; // InputTypeDict ID
                    strResult += "$";

                    // 计算100%概率总值
                    int PercentZero = AwardList[0].RandBeginNum;
                    int PercentOne = AwardList[AwardList.Count - 1].RandEndNum;

                    lock (AwardItemDict)
                    {
                        // 随机指定数量的id出来
                        for (int Num = 0; Num < kvp.Value.Num; ++Num)
                        {
                            // 随机一个出来
                            int rate = Global.GetRandomNumber(PercentZero, PercentOne);
                            for (int i = 0; i < AwardList.Count; ++i)
                            {
                                if (true == AwardList[i].RandSkip)
                                {
                                    rate += AwardList[i].RandNumMinus;
                                }

                                if (false == AwardList[i].RandSkip &&
                                    rate >= AwardList[i].RandBeginNum && rate <= AwardList[i].RandEndNum)
                                {
                                    // 命中
                                    AwardList[i].RandSkip = true;
                                    PercentOne -= AwardList[i].RandNumMinus; // 用最新的随机数上限随机
                                 
                                    // 调试log
//                                     LogManager.WriteLog(LogTypes.Info, string.Format("周末充值随机奖励: 角色名:{0} 钻石档位:{1} ID:{2} AwardID:{3} 对应随机区间:{4}-{5} 当前随机上限:{6} 随机数:{7}",
//                                         client.ClientData.RoleName, kvp.Value.MinZuanShi, kvp.Key, AwardList[i].id, AwardList[i].RandBeginNum, AwardList[i].RandEndNum, PercentOne, rate));

                                    // 拼结果
                                    strResult += AwardList[i].id;
                                    if (Num != kvp.Value.Num - 1) // 避免最后一个数后多一个','
                                    {
                                        strResult += ",";
                                    }
                                    break;
                                }
                            }
                        }
                        strResult += "|";

                        // refresh AwardList
                        for (int i = 0; i < AwardList.Count; ++i)
                        {
                            // 调试log
                            if (true == AwardList[i].RandSkip)
                            {
//                                 LogManager.WriteLog(LogTypes.Info, string.Format("周末充值随机奖励: 清理随机命中数据 角色名:{0} ID:{1} AwardID:{2}",
//                                     client.ClientData.RoleName, kvp.Key, AwardList[i].id));
                            }

                            AwardList[i].RandSkip = false;
                        }
                    }
                }
            }
            
            // 处理最后的"|"
            if (!string.IsNullOrEmpty(strResult) && strResult.Substring(strResult.Length - 1) == "|")
            {
                strResult = strResult.Substring(0, strResult.Length - 1);
            }

            return strResult;
        }

        /// <summary>
        /// 解析活动时间
        /// </summary>
        public bool ParseActivityTime(string ZhouMoChongZhiTime)
        {
            // <!--Mu周末充值时间，开启时间，终止时间，1至7代表周一至周日-->
            // <Param Name="ZhouMoChongZhiTime" Value="6,00:00:00|7,23:59:59"/>
            string[] TimeActivity = ZhouMoChongZhiTime.Split('|');
            if (TimeActivity == null || TimeActivity.Length != 2)
                return false;

            string[] DataBeginSplit = TimeActivity[0].Split(',');
            string[] DataEndSplit = TimeActivity[1].Split(',');

            if (DataBeginSplit == null || DataEndSplit == null || DataBeginSplit.Length != 2 || DataEndSplit.Length != 2)
                return false;

            //
            FromDate = DataBeginSplit[0] + ',' + DataBeginSplit[1];
            ToDate = DataEndSplit[0] + ',' + DataEndSplit[1];

            // 删空格 防止填错
            FromDate.Trim();
            ToDate.Trim();

            return true;
        }

        public bool Init()
        {
            try
            {
                // 初始化活动时间相关SystemParams.xml ZhouMoChongZhiTime
                string ZhouMoChongZhiTime = GameManager.systemParamsList.GetParamValueByName("ZhouMoChongZhiTime");
                if (!string.IsNullOrEmpty(ZhouMoChongZhiTime))
                {
                    // 解析活动时间
                    if (!ParseActivityTime(ZhouMoChongZhiTime))
                        return false;
                }

                // 加载ZhouMoChongZhiType.xml
                string fileName = Global.IsolateResPath("Config/Gifts/ZhouMoChongZhiType.xml");
                XElement xmlType = GeneralCachingXmlMgr.GetXElement(fileName);
                if (null == xmlType) return false;

                IEnumerable<XElement> xmlItems = xmlType.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null != xmlItem)
                    {
                        WeekEndInputTypeData myInputType = new WeekEndInputTypeData();
                        int id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        myInputType.MinZhuanSheng = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "MinZhuanSheng"));
                        myInputType.MinLevel = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "MinLevel"));
                        myInputType.MaxZhuanSheng = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "MaxZhuanSheng"));
                        myInputType.MaxLevel = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "MaxLevel"));
                        myInputType.MinZuanShi = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "MinZuanShi"));
                        myInputType.Num = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "Num"));
                        InputTypeDict[id] = myInputType;
                    }
                }

                // 加载ZhouMoChongZhi.xml
                fileName = Global.IsolateResPath("Config/Gifts/ZhouMoChongZhi.xml");
                XElement xmlAward = GeneralCachingXmlMgr.GetXElement(fileName);
                if (null == xmlAward) return false;

                IEnumerable<XElement> xmlItemsAward = xmlAward.Elements();
                foreach (var xmlRandAwardList in xmlItemsAward)
                {
                    if (null != xmlRandAwardList)
                    {
                        List<WeekEndInputAwardData> myRandAwardList = new List<WeekEndInputAwardData>();
                        int id = (int)Global.GetSafeAttributeLong(xmlRandAwardList, "ID");

                        IEnumerable<XElement> xmlRandAwards = xmlRandAwardList.Elements();
                        foreach (var xmlRandAward in xmlRandAwards)
                        {
                            WeekEndInputAwardData myRandAward = new WeekEndInputAwardData();
                            myRandAward.id = (int)Global.GetSafeAttributeLong(xmlRandAward, "ID");
                            myRandAward.RandBeginNum = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlRandAward, "BeginNum"));
                            myRandAward.RandEndNum = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlRandAward, "EndNum"));
                            myRandAward.RandNumMinus = myRandAward.RandEndNum - myRandAward.RandBeginNum + 1;
                            string goodsIDs = Global.GetSafeAttributeStr(xmlRandAward, "Goods");

                            // 实际只有一个物品 为了使用通用的接口ParseGoodsDataList返回GoodsDataList
                            string[] fields = goodsIDs.Split('|');
                            if (fields.Length <= 0)
                            {
                                LogManager.WriteLog(LogTypes.Warning, string.Format("解析大型周末充值活动配置文件中的物品配置项1失败"));
                            }
                            else
                            {
                                //将物品字符串列表解析成物品数据列表
                                myRandAward.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, "大型节日周末充值配置1");
                            }

                            myRandAwardList.Add(myRandAward);
                        }

                        AwardItemDict[id] = myRandAwardList;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", "ZhouMoChongZhiType.xml|ZhouMoChongZhi.xml", ex.Message));
                return false;
            }

            return true;
        }

    }

}