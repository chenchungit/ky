using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 王城战管理
    /// </summary>
    public class WangChengManager
    {
        #region 内存中的王族帮会ID

        /// <summary>
        /// 是否正在等待王城的归属
        /// </summary>
        private static bool WaitingHuangChengResult = false;

        /// <summary>
        /// 王城战期间帮会独占皇宫的保持时间
        /// </summary>
        private static long BangHuiTakeHuangGongTicks = TimeUtil.NOW();

        /// <summary>
        /// 王族的所在帮会名称
        /// </summary>
        private static string WangZuBHName = "";

        /// <summary>
        /// 王族所在的帮会ID
        /// </summary>
        private static int WangZuBHid = -1;

        /// <summary>
        /// 程序启动时从DBServer更新王族的ID
        /// </summary>
        public static void UpdateWangZuBHNameFromDBServer(int bhid)
        {
            //每一次更新，都记录
            WangZuBHid = bhid;

            if (bhid <= 0) return; //不获取

            BangHuiMiniData bangHuiMiniData = Global.GetBangHuiMiniData(bhid);
            if (null == bangHuiMiniData)
            {
                return;
            }

            WangZuBHName = bangHuiMiniData.BHName;
        }

        /// <summary>
        /// 获取王族的帮会ID
        /// </summary>
        public static int GetWangZuBHid()
        {
            return WangZuBHid;
        }

        /// <summary>
        /// 获取王族的帮会名称
        /// </summary>
        public static string GetWangZuBHName()
        {
            return WangZuBHName;
        }

        #endregion 内存中的王族帮会ID

        #region 处理王城战的胜负结果

        /// <summary>
        /// 申请王城战的锁
        /// </summary>
        public static object ApplyWangChengWarMutex = new object();

        /// <summary>
        /// 占领皇宫决定胜负的最长时间
        /// </summary>
        private static int MaxTakingHuangGongSecs = (20 * 60);

        /// <summary>
        /// 从配置文件中loading是否是有效数据
        /// </summary>
        private static bool WangChengZhanWeekDaysByConfig = false;

        /// <summary>
        /// 王城战的举行的周日期
        /// </summary>
        private static int[] WangChengZhanWeekDays = null;

        /// <summary>
        /// 王城战的举行的时间段
        /// </summary>
        private static DateTimeRange[] WangChengZhanFightingDayTimes = null;

        /// <summary>
        /// 解析王城战的日期和时间
        /// </summary>
        public static void ParseWeekDaysTimes()
        {
            string WangChengZhanWeekDays_str = GameManager.systemParamsList.GetParamValueByName("WangChengZhanWeekDays");
            if (!string.IsNullOrEmpty(WangChengZhanWeekDays_str))
            {
                string[] WangChengZhanWeekDays_fields = WangChengZhanWeekDays_str.Split(',');
                int[] weekDays = new int[WangChengZhanWeekDays_fields.Length];
                for (int i = 0; i < WangChengZhanWeekDays_fields.Length; i++)
                {
                    weekDays[i] = Global.SafeConvertToInt32(WangChengZhanWeekDays_fields[i]);
                }

                if (weekDays.Length > 0 && weekDays[0] >= 0)
                {
                    WangChengZhanWeekDaysByConfig = true;
                    WangChengZhanWeekDays = weekDays;
                }
            }

            string wangChengZhanFightingDayTimes_str = GameManager.systemParamsList.GetParamValueByName("WangChengZhanFightingDayTimes");
            WangChengZhanFightingDayTimes = Global.ParseDateTimeRangeStr(wangChengZhanFightingDayTimes_str);

            MaxTakingHuangGongSecs = (int)GameManager.systemParamsList.GetParamValueIntByName("MaxTakingHuangGongSecs");
            MaxTakingHuangGongSecs *= 1000;

            //每一次重新加载都更新一次，保证最新的修改都得到应用
            Global.UpdateWangChengZhanWeekDays(true);

            //通知地图数据变更信息
            NotifyAllWangChengMapInfoData();
        }

        /// <summary>
        /// 判断周日期是否相符
        /// </summary>
        /// <param name="weekDayID"></param>
        /// <returns></returns>
        private static bool IsDayOfWeek(int weekDayID)
        {
            if (null == WangChengZhanWeekDays) return false;
            for (int i = 0; i < WangChengZhanWeekDays.Length; i++)
            {
                if (WangChengZhanWeekDays[i] == weekDayID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 是否在王城战时间段内
        /// </summary>
        /// <returns></returns>
        public static bool IsInWangChengFightingTime()
        {
            DateTime now = TimeUtil.NowDateTime();
            int weekDayID = (int)now.DayOfWeek;
            if (!IsDayOfWeek(weekDayID))
            {
                return false;
            }

            int endMinute = 0;
            return Global.JugeDateTimeInTimeRange(now, WangChengZhanFightingDayTimes, out endMinute, false);
        }

        /// <summary>
        /// 王城战是否已经结束
        /// </summary>
        /// <returns></returns>
        public static bool IsWangChengZhanOver()
        {
            return !WaitingHuangChengResult;
        }

        /// <summary>
        /// 王城战的状态类型
        /// </summary>
        public static WangChengZhanStates WangChengZhanState = WangChengZhanStates.None;

        /// <summary>
        /// 判断是否正在进行王城争霸赛 在地图和在战斗时间
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool IsInCityWarBattling(GameClient client)
        {
            if (client.ClientData.MapCode == Global.GetHuangGongMapCode())
            {
                if (WangChengZhanStates.None != WangChengZhanState)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断是否在战斗时间 战斗时间主要是不让领取每日奖励
        /// </summary>
        /// <returns></returns>
        public static bool IsInBattling()
        {
            if (WangChengZhanStates.None != WangChengZhanState)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 处理王城战的战斗结果
        /// </summary>
        public static void ProcessWangChengZhanResult()
        {
            //if (Global.GetBangHuiFightingLineID() != GameManager.ServerLineID)
            //{
            //    return;
            //}

            //进行weekday更新，这样新旧代码一致
            Global.UpdateWangChengZhanWeekDays();

            if (WangChengZhanStates.None == WangChengZhanState) //非战斗状态
            {
                if (IsInWangChengFightingTime())
                {
                    WangChengZhanState = WangChengZhanStates.Fighting;
                    BangHuiTakeHuangGongTicks = TimeUtil.NOW();
                    WaitingHuangChengResult = true;

                    //通知地图数据变更信息
                    NotifyAllWangChengMapInfoData();

                    //王城战开始通知
                    Global.BroadcastHuangChengBattleStart();
                }
            }
            else //战斗状态
            {
                if (IsInWangChengFightingTime()) //还在战斗期间
                {
                    //这儿其实是在模拟拥有舍利之源的操作，如此就走就代码的逻辑，不用修改太多代码
                    bool ret = TryGenerateNewHuangChengBangHui();

                    //生成了新的占有王城的帮会
                    if (ret)
                    {
                        //处理王城的归属
                        HandleHuangChengResultEx(false);

                        //通知地图数据变更信息
                        NotifyAllWangChengMapInfoData();
                    }
                    else
                    {
                        /// 定时给在场的玩家增加经验
                        ProcessTimeAddRoleExp();
                    }
                }
                else
                {
                    //战斗结束
                    WangChengZhanState = WangChengZhanStates.None;

                    //一旦战斗结束，就将今天移除
                    //RemoveTodayInWarRequest();

                    //王族产生了
                    WaitingHuangChengResult = false;

                    //这儿其实是在模拟拥有舍利之源的操作，如此就走就代码的逻辑，不用修改太多代码
                    TryGenerateNewHuangChengBangHui();

                    //处理王城的归属
                    HandleHuangChengResultEx(true);

                    //通知地图数据变更信息
                    NotifyAllWangChengMapInfoData();
                }
            }
        }

        /// <summary>
        /// 上一个唯一的帮会
        /// </summary>
        private static int LastTheOnlyOneBangHui = -1;

        /// <summary>
        /// <summary>
        /// 新的王城帮会
        /// 2.1若在结束前，王城已有归属，且当前皇宫内成为拥有多个行会的成员或者无人在皇宫中，则王城胜利方属于王城原有归属
        /// 2.2若在结束前，王城无归属，且皇宫内所有成员均为一个行会的成员，则该行会将成为本次王城战的胜利方
        /// 2.3王城战结束时间到后，若之前王城为无归属状态，且皇宫内成员非同一个行会或者无人在皇宫中，则本次王城战流产
        /// </summary>
        /// 尝试产生新帮会[拥有王城所有权的帮会]
        /// </summary>
        /// <returns></returns>
        public static bool TryGenerateNewHuangChengBangHui()
        {
            int newBHid = GetTheOnlyOneBangHui();

            //剩下的帮会是王城帮会，没有产生新帮会
            if (newBHid <= 0 || WangZuBHid == newBHid)
            {
                LastTheOnlyOneBangHui = -1;
                return false;
            }

            //这次的新帮会和上次不一样，替换,并记录时间
            if (LastTheOnlyOneBangHui != newBHid)
            {
                LastTheOnlyOneBangHui = newBHid;
                BangHuiTakeHuangGongTicks = TimeUtil.NOW();

                //还是没产生
                return false;
            }

            if (LastTheOnlyOneBangHui > 0)
            {
                //超过最小时间之后，产生了新帮会，接下来外面的代码需要进行数据库修改
                long ticks = TimeUtil.NOW();
                if (ticks - BangHuiTakeHuangGongTicks > MaxTakingHuangGongSecs)
                {
                    WangZuBHid = LastTheOnlyOneBangHui;
                    UpdateWangZuBHNameFromDBServer(newBHid); //加载帮会名称等细节信息
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 返回剩下的唯一帮会,-1表示有没有唯一帮会
        /// </summary>
        /// <returns></returns>
        public static int GetTheOnlyOneBangHui()
        {
            //皇宫地图中活着的玩家列表
            List<GameClient> lsClients = GameManager.ClientMgr.GetMapAliveClientsEx(Global.GetHuangGongMapCode());

            int newBHid = -1;
            int existBHid = -1;

            //根据活着的玩家列表，判断王族是否应该产生 保留 还说流产
            for (int n = 0; n < lsClients.Count; n++)
            {
                GameClient client = lsClients[n];
                if (existBHid != -1)
                {
                    if (client.ClientData.Faction > 0 && client.ClientData.Faction != existBHid)
                    {
                        //执行到这，表示活着的玩家中，至少有两个帮会，设置新帮会id 为 -1，这样要么保留就王族，要么流产
                        newBHid = -1;
                        break;
                    }
                }
                else
                {
                    if (client.ClientData.Faction > 0)
                    {
                        existBHid = client.ClientData.Faction;
                        newBHid = existBHid;
                    }
                }
            }

            return newBHid;
        }

        /// <summary>
        /// 通知地图数据变更信息
        /// </summary>
        public static void NotifyAllWangChengMapInfoData()
        {
            WangChengMapInfoData wangChengMapInfoData = FormatWangChengMapInfoData();

            //通知在线的所有人(不限制地图)领地信息数据通知
            GameManager.ClientMgr.NotifyAllWangChengMapInfoData(wangChengMapInfoData);
        }

        /// <summary>
        /// 处理王城战流产
        /// </summary>
        private static void HandleWangChengFailed()
        {
            //处理领地战的结果
            JunQiManager.HandleLingDiZhanResultByMapCode((int)LingDiIDs.HuangGong, Global.GetHuangGongMapCode(), 0, true, false);

            //王城流产的提示
            Global.BroadcastWangChengFailedHint();

            //通知GameServer同步帮会的所属和范围
            JunQiManager.NotifySyncBangHuiJunQiItemsDict(null);
        }

        /// <summary>
        /// 处理王城的归属--->只考虑帮会ID，不考虑具体角色
        /// </summary>
        private static void HandleHuangChengResultEx(bool isBattleOver = false)
        {
            if (WangZuBHid <= 0)
            {
                if (isBattleOver)
                {
                    //处理王城战流产
                    HandleWangChengFailed();
                }
                return; //谁也当不成王族
            }


            //处理领地战的结果
            JunQiManager.HandleLingDiZhanResultByMapCode((int)LingDiIDs.HuangGong, Global.GetHuangGongMapCode(), WangZuBHid, true, false);

            //夺取王城的提示
            Global.BroadcastHuangChengOkHintEx(WangZuBHName, isBattleOver);

            //通知GameServer同步帮会的所属和范围
            JunQiManager.NotifySyncBangHuiJunQiItemsDict(null);

            if (isBattleOver)
            {
                //设置合服后的王城霸主
                HuodongCachingMgr.UpdateHeFuWCKingBHID(WangZuBHid);
            }
        }

        #endregion 处理王城战的胜负结果

        #region 地图战斗状态数据

        /// <summary>
        /// 通知角色王城地图信息数据
        /// </summary>
        /// <param name="client"></param>
        public static void NotifyClientWangChengMapInfoData(GameClient client)
        {
            WangChengMapInfoData wangChengMapInfoData = GetWangChengMapInfoData(client);
            GameManager.ClientMgr.NotifyWangChengMapInfoData(client, wangChengMapInfoData);
        }

        /// <summary>
        /// 获取地图战斗状态数据
        /// </summary>
        /// <returns></returns>
        public static WangChengMapInfoData GetWangChengMapInfoData(GameClient client)
        {
            return FormatWangChengMapInfoData();
        }

        /// <summary>
        /// 获取地图战斗状态数据
        /// </summary>
        /// <returns></returns>
        public static WangChengMapInfoData FormatWangChengMapInfoData()
        {
            String nextBattleTime = Global.GetLang("没有帮派申请");
            long endTime = 0;

            if (WangChengZhanStates.None == WangChengZhanState) //非战斗状态
            {
                nextBattleTime = GetNextCityBattleTime();
            }
            else
            {
                endTime = GetBattleEndMs();
            }

            WangChengMapInfoData WangChengMapInfoData = new WangChengMapInfoData()
            {
                FightingEndTime = endTime,
                FightingState = WaitingHuangChengResult ? 1 : 0,
                NextBattleTime = nextBattleTime,
                WangZuBHName = WangZuBHName,
                WangZuBHid = WangZuBHid,
            };

            return WangChengMapInfoData;
        }

        #endregion 地图战斗状态数据

        #region 王城战申请管理

        /// <summary>
        /// 从王城战申请字符串解析出申请映射辞典
        /// banghuiid_day,banghuiid_day,banghuiid_day
        /// </summary>
        /// <param name="warReqString"></param>
        /// <returns></returns>
        public static Dictionary<int, int> GetWarRequstMap(String warReqString)
        {
            Dictionary<int, int> warRequstMap = new Dictionary<int, int>();

            //解析帮会 申请时间 映射表
            String[] reqItems = warReqString.Split(',');
            String[] item = null;
            for (int n = 0; n < reqItems.Length; n++)
            {
                item = reqItems[n].Split('_');

                if (item.Length != 2) continue;

                int bhid = int.Parse(item[0]);
                int day = int.Parse(item[1]);

                if (warRequstMap.ContainsKey(bhid)) continue;

                //小于今天，但 + 120天大于今天的，不生成，这样，跨年问题就不存在了，120天的空当时间，
                //足够将1月1号前后的意外消除, 比如  12月30号， 1月1号，1月2号，三天同时出现是合法的，
                //但是假如1月1号服务器没有启动，则1月一号的数据不会被清除，在这儿就过滤掉了，当然，也可以
                //在清除王城战当天清除前几天的数据，如果连续120天不执行这个代码。。。。
                if (day < TimeUtil.NowDateTime().DayOfYear && day + 120 > TimeUtil.NowDateTime().DayOfYear) continue;

                warRequstMap.Add(bhid, day);
            }

            return warRequstMap;
        }

        /// <summary>
        /// 返回新的王城争夺战请求日期列表字符串
        /// </summary>
        /// <returns></returns>
        public static String GeWarRequstString(Dictionary<int, int> warRequstMap)
        {
            String nowWarRequest = "";

            //生成新的字符串，并提交给gamedbserver，成功之后进行广播通知
            for (int n = 0; n < warRequstMap.Count; n++)
            {
                if (nowWarRequest.Length > 0)
                {
                    nowWarRequest += ",";
                }
                nowWarRequest += String.Format("{0}_{1}", warRequstMap.ElementAt(n).Key, warRequstMap.ElementAt(n).Value);
            }

            return nowWarRequest;
        }

        /// <summary>
        /// 通知dbserver更新王城争夺战请求
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="bhid"></param>
        /// <param name="lingDiID"></param>
        /// <param name="nowWarRequest"></param>
        /// <returns></returns>
        public static int SetCityWarRequestToDBServer(int lingDiID, String nowWarRequest)
        {
            int retCode = -200;

            //提交给gamedbserver 修改
            String strcmd = string.Format("{0}:{1}", lingDiID, nowWarRequest);
            String[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_SETLINGDIWARREQUEST, strcmd, GameManager.LocalServerId);
            if (null == fields || fields.Length != 5)
            {
                return retCode;
            }

            retCode = Global.SafeConvertToInt32(fields[0]);

            //通知GameServer同步领地相关信息辞典
            JunQiManager.NotifySyncBangHuiLingDiItemsDict();

            return retCode;
        }

        /// <summary>
        /// 判断当天是否存在王城争夺战
        /// </summary>
        /// <returns></returns>
        public static bool IsExistCityWarToday()
        {
            int day = TimeUtil.NowDateTime().DayOfYear;

            BangHuiLingDiItemData lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.HuangGong);
            if (null == lingDiItem)
            {
                return false;
            }

            Dictionary<int, int> warRequestMap = GetWarRequstMap(lingDiItem.WarRequest);

            if (!warRequestMap.ContainsValue(day))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 更新王城争夺战周日期
        /// </summary>
        /// <param name="weekDays"></param>
        public static void UpdateWangChengZhanWeekDays(int[] weekDays)
        {
            if (!WangChengZhanWeekDaysByConfig)
            {
                WangChengZhanWeekDays = weekDays;
            }
        }

        /// <summary>
        /// 在王城争夺战中去掉今天
        /// </summary>
        protected static void RemoveTodayInWarRequest()
        {
            int day = TimeUtil.NowDateTime().DayOfYear;

            BangHuiLingDiItemData lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.HuangGong);
            if (null == lingDiItem)
            {
                return;
            }

            Dictionary<int, int> warRequestMap = GetWarRequstMap(lingDiItem.WarRequest);

            if (!warRequestMap.ContainsValue(day))
            {
                return;
            }

            for (int n = 0; n < warRequestMap.Count; n++)
            {
                if (warRequestMap.Values.ElementAt(n) == day)
                {
                    //移除今天
                    warRequestMap.Remove(warRequestMap.Keys.ElementAt(n));
                    break;
                }
            }

            //重新生成争夺战请求字符串
            String nowWarRequest = GeWarRequstString(warRequestMap);

            //提交给gameserver
            SetCityWarRequestToDBServer((int)LingDiIDs.HuangGong, nowWarRequest);
        }

        /// <summary>
        /// 返回战斗结束时的毫秒数
        /// </summary>
        /// <returns></returns>
        public static long GetBattleEndMs()
        {
            DateTime now = TimeUtil.NowDateTime();
            int hour = now.Hour;
            int minute = now.Minute;

            int nowMinite = hour * 60 + minute;

            int endMinute = 0;
            Global.JugeDateTimeInTimeRange(TimeUtil.NowDateTime(), WangChengZhanFightingDayTimes, out endMinute, true);

            DateTime endTime = now.AddMinutes(Math.Max(0, endMinute - nowMinite));

            return endTime.Ticks / 10000;
        }

        /// <summary>
        /// 返回下次王城争霸赛的时间
        /// </summary>
        public static String GetNextCityBattleTime()
        {
            //实现过程不采用 GetNextCityBattleTimeAndBangHui()了，这个函数是先写的
            String unKown = Global.GetLang("没有帮派申请");

            int day = TimeUtil.NowDateTime().DayOfYear;

            BangHuiLingDiItemData lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.HuangGong);
            if (null == lingDiItem)
            {
                return unKown;
            }

            Dictionary<int, int> warRequestMap = GetWarRequstMap(lingDiItem.WarRequest);

            List<DateTime> lsDays = new List<DateTime>();

            for (int n = 0; n < warRequestMap.Count; n++)
            {
                DateTime dt = TimeUtil.NowDateTime();
                int span = warRequestMap.Values.ElementAt(n) - day;
                if (span >= 0)
                {
                    //未跨年
                    dt = dt.AddDays(span);
                }
                else
                {
                    //跨年了--->先到下一年
                    int yearNext = dt.Year + 1;
                    dt = DateTime.Parse(String.Format("{0}-01-01", yearNext));
                    dt = dt.AddDays(warRequestMap.Values.ElementAt(n) - 1);
                }

                lsDays.Add(dt);
            }

            lsDays.Sort(delegate(DateTime l, DateTime r)
            {
                if (l.Ticks < r.Ticks)
                {
                    return -1;
                }
                else if (l.Ticks > r.Ticks)
                {
                    return 1;
                }

                return 0;
            });

            if (lsDays.Count > 0)
            {
                DateTime nextDate = lsDays[0];

                //返回时 分
                if (null != WangChengZhanFightingDayTimes && WangChengZhanFightingDayTimes.Length > 0)
                {
                    return lsDays[0].ToString("yyyy-MM-dd " + String.Format("{0:00}:{1:00}", WangChengZhanFightingDayTimes[0].FromHour, WangChengZhanFightingDayTimes[0].FromMinute));
                }
            }

            return unKown;
        }

        /// <summary>
        /// 返回下次王城争霸赛的时间和帮会
        /// </summary>
        public static bool GetNextCityBattleTimeAndBangHui(out int dayID, out int bangHuiID)
        {
            dayID = -1;
            bangHuiID = -1;

            int day = TimeUtil.NowDateTime().DayOfYear;

            BangHuiLingDiItemData lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.HuangGong);
            if (null == lingDiItem)
            {
                return false;
            }

            Dictionary<int, int> warRequestMap = GetWarRequstMap(lingDiItem.WarRequest);

            List<DateTime> lsDays = new List<DateTime>();

            //这儿考虑跨年和不跨年是为了兼容旧的dayOfYear的存储方式，实际上跨年的比较少，毕竟也就那么几天
            for (int n = 0; n < warRequestMap.Count; n++)
            {
                DateTime dt = TimeUtil.NowDateTime();
                int span = warRequestMap.Values.ElementAt(n) - day;
                if (span >= 0)
                {
                    //未跨年
                    dt = dt.AddDays(span);
                }
                else//小于0表示跨年，实际上申请数量最多几十天
                {
                    //跨年了--->先到下一年
                    int yearNext = dt.Year + 1;
                    dt = DateTime.Parse(String.Format("{0}-01-01", yearNext));
                    dt = dt.AddDays(warRequestMap.Values.ElementAt(n) - 1);
                }

                lsDays.Add(dt);
            }

            //依次从小到大进行排序
            lsDays.Sort(delegate(DateTime l, DateTime r)
            {
                if (l.Ticks < r.Ticks)
                {
                    return -1;
                }
                else if (l.Ticks > r.Ticks)
                {
                    return 1;
                }

                return 0;
            });

            if (lsDays.Count > 0)
            {
                DateTime nextDate = lsDays[0];

                //返回时 分
                if (null != WangChengZhanFightingDayTimes && WangChengZhanFightingDayTimes.Length > 0)
                {
                    dayID = nextDate.DayOfYear;
                    for (int n = 0; n < warRequestMap.Count; n++)
                    {
                        //查询帮会ID
                        if (dayID == warRequestMap.Values.ElementAt(n))
                        {
                            bangHuiID = warRequestMap.Keys.ElementAt(n);
                            return true;
                        }
                    }

                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// 返回下一次王城争霸的时间和发起帮会
        /// </summary>
        /// <param name="dayTime"></param>
        /// <param name="bangHuiName"></param>
        /// <returns></returns>
        public static bool GetNextCityBattleTimeAndBangHui(out String dayTime, out String bangHuiName)
        {
            dayTime = Global.GetLang("没有帮派申请");
            bangHuiName = Global.GetLang("空缺中");

            int warDay, bangHuiID;
            if (!GetNextCityBattleTimeAndBangHui(out warDay, out bangHuiID))
            {
                return false;
            }

            return GetWarTimeStringAndBHName(warDay, bangHuiID, out dayTime, out bangHuiName);
        }

        /// <summary>
        /// 返回下次王城争霸赛的时间和帮会
        /// </summary>
        public static String GetCityBattleTimeAndBangHuiListString()
        {
            if (null == WangChengZhanFightingDayTimes || WangChengZhanFightingDayTimes.Length <= 0)
            {
                return "";
            }

            int day = TimeUtil.NowDateTime().DayOfYear;

            BangHuiLingDiItemData lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.HuangGong);
            if (null == lingDiItem)
            {
                return "";
            }

            Dictionary<int, int> warRequestMap = GetWarRequstMap(lingDiItem.WarRequest);

            List<DateTime> lsDays = new List<DateTime>();

            //这儿考虑跨年和不跨年是为了兼容旧的dayOfYear的存储方式，实际上跨年的比较少，毕竟也就那么几天
            for (int n = 0; n < warRequestMap.Count; n++)
            {
                DateTime dt = TimeUtil.NowDateTime();
                int span = warRequestMap.Values.ElementAt(n) - day;
                if (span >= 0)
                {
                    //未跨年
                    dt = dt.AddDays(span);
                }
                else//小于0表示跨年，实际上申请数量最多几十天
                {
                    //跨年了--->先到下一年
                    int yearNext = dt.Year + 1;
                    dt = DateTime.Parse(String.Format("{0}-01-01", yearNext));
                    dt = dt.AddDays(warRequestMap.Values.ElementAt(n) - 1);
                }

                lsDays.Add(dt);
            }

            //依次从小到大进行排序
            lsDays.Sort(delegate(DateTime l, DateTime r)
            {
                if (l.Ticks < r.Ticks)
                {
                    return -1;
                }
                else if (l.Ticks > r.Ticks)
                {
                    return 1;
                }

                return 0;
            });

            String timeBangHuiString = "";

            //组装字符串,最多10条
            for (int index = 0; index < lsDays.Count && index < 10; index++)
            {
                DateTime nextDate = lsDays[index];

                int dayID = nextDate.DayOfYear;
                for (int n = 0; n < warRequestMap.Count; n++)
                {
                    //查询帮会ID,查询到就生成字符串
                    if (dayID == warRequestMap.Values.ElementAt(n))
                    {
                        int bangHuiID = warRequestMap.Keys.ElementAt(n);
                        String strTime, strBH;
                        GetWarTimeStringAndBHName(dayID, bangHuiID, out strTime, out strBH);

                        if (timeBangHuiString.Length > 0)
                        {
                            timeBangHuiString += ",";
                        }

                        timeBangHuiString += String.Format("{0},{1}", strTime, strBH);
                        break;
                    }
                }//这个循环跳出后会继续外面的小循环
            }

            return timeBangHuiString;
        }

        /// <summary>
        /// 根据天 和 帮会id 返回字符串
        /// </summary>
        /// <param name="warDay"></param>
        /// <param name="bangHuiID"></param>
        /// <param name="dayTime"></param>
        /// <param name="bangHuiName"></param>
        /// <returns></returns>
        private static bool GetWarTimeStringAndBHName(int warDay, int bangHuiID, out String dayTime, out String bangHuiName)
        {
            dayTime = Global.GetLang("没有帮派申请");
            bangHuiName = Global.GetLang("空缺中");

            BangHuiMiniData bhData = Global.GetBangHuiMiniData(bangHuiID);
            if (null != bhData)
            {
                bangHuiName = bhData.BHName;//Global.FormatBangHuiName(bhData.ZoneID, bhData.BHName);
            }

            //dayid转换字符串
            int day = TimeUtil.NowDateTime().DayOfYear;
            DateTime dt = TimeUtil.NowDateTime();
            int span = warDay - day;
            if (span >= 0)
            {
                //未跨年
                dt = dt.AddDays(span);
            }
            else
            {
                //跨年了--->先到下一年
                int yearNext = dt.Year + 1;
                dt = DateTime.Parse(String.Format("{0}-01-01", yearNext));
                dt = dt.AddDays(warDay - 1);
            }

            if (null != WangChengZhanFightingDayTimes && WangChengZhanFightingDayTimes.Length > 0)
            {
                String dayTime1 = dt.ToString("yyyy-MM-dd " + String.Format("{0:00}:{1:00}", WangChengZhanFightingDayTimes[0].FromHour, WangChengZhanFightingDayTimes[0].FromMinute));
                //String dayTime2 = dt.ToString("yyyy-MM-dd " + String.Format("{0:00}:{1:00}", WangChengZhanFightingDayTimes[0].EndHour, WangChengZhanFightingDayTimes[0].EndMinute));
                String dayTime2 = String.Format("{0:00}:{1:00}", WangChengZhanFightingDayTimes[0].EndHour, WangChengZhanFightingDayTimes[0].EndMinute);
                dayTime = String.Format(Global.GetLang("{0} 至 {1}"), dayTime1, dayTime2);
            }
            else
            {
                dayTime = dt.ToString("yyyy-MM-dd");
            }

            return true;
        }

        #endregion 王城战申请管理

        #region 定时给在场的玩家家经验

        /// <summary>
        /// 定时给予收益
        /// </summary>
        private static long LastAddBangZhanAwardsTicks = 0;

        /// <summary>
        /// 定时给在场的玩家增加经验
        /// </summary>
        private static void ProcessTimeAddRoleExp()
        {
            long ticks = TimeUtil.NOW();
            if (ticks - LastAddBangZhanAwardsTicks < (10 * 1000))
            {
                return;
            }

            LastAddBangZhanAwardsTicks = ticks;

            //先找出当前大乱斗中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(Global.GetHuangGongMapCode());
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                //if (c.ClientData.CurrentLifeV <= 0) continue;

                /// 处理用户的经验奖励
                BangZhanAwardsMgr.ProcessBangZhanAwards(c);
            }
        }

        #endregion 定时给在场的玩家家经验
    }
}
