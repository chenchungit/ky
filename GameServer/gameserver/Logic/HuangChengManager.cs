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
    /// 皇城战管理
    /// </summary>
    public class HuangChengManager
    {
        #region 提取舍利之源时的互斥对象

        /// <summary>
        /// 提取舍利之源时的互斥对象
        /// </summary>
        public static Object SheLiZhiYuanMutex = new object();

        #endregion 提取舍利之源时的互斥对象

        #region 内存中的皇帝角色ID

        /// <summary>
        /// 是否正在等待皇城的归属
        /// </summary>
        private static bool WaitingHuangChengResult = false;

        /// <summary>
        /// 皇城战期间皇帝特效的保持时间
        /// </summary>
        private static long HuangDiRoleTicks = TimeUtil.NOW();

        /// <summary>
        /// 皇帝的角色ID
        /// </summary>
        private static int HuangDiRoleID = 0;

        /// <summary>
        /// 皇帝的角色名称
        /// </summary>
        private static string HuangDiRoleName = "";

        /// <summary>
        /// 皇帝的所在帮会名称
        /// </summary>
        private static string HuangDiBHName = "";

        /// <summary>
        /// 程序启动时从DBServer更新皇帝的ID
        /// </summary>
        public static void LoadHuangDiRoleIDFromDBServer(int bhid)
        {
            if (bhid <= 0) return; //不获取

            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GETLEADERROLEIDBYBHID, string.Format("{0}", bhid), GameManager.LocalServerId);
            if (null == fields || fields.Length != 3)
            {
                return;
            }

            HuangDiRoleID = Global.SafeConvertToInt32(fields[0]);
            HuangDiRoleName = fields[1];
            HuangDiBHName = fields[2];
        }

        /// <summary>
        /// 获取皇帝的角色ID
        /// </summary>
        public static int GetHuangDiRoleID()
        {
            return HuangDiRoleID;
        }

        /// <summary>
        /// 获取皇帝的角色名称
        /// </summary>
        public static string GetHuangDiRoleName()
        {
            return HuangDiRoleName;
        }

        /// <summary>
        /// 获取皇帝的所在帮会名称
        /// </summary>
        public static string GetHuangDiBHName()
        {
            return HuangDiBHName;
        }

        #endregion 内存中的皇帝角色ID

        #region 处理提取舍利之源的操作

        /// <summary>
        /// 处理提取舍利之源的操作
        /// </summary>
        /// <param name="client"></param>
        public static int ProcessTakeSheLiZhiYuan(int roleID, string roleName, string bhName, bool sendToOtherLine = true)
        {
            int oldHuangDiRoleID = HuangDiRoleID;
            HuangDiRoleID = roleID;
            HuangDiRoleName = roleName;
            HuangDiBHName = bhName;
            HuangDiRoleTicks = TimeUtil.NOW();

            //通知GameServer同步设置之源拥有者
            if (sendToOtherLine)
            {
                NotifySyncHuanDiRoleInfo(oldHuangDiRoleID, roleID, roleName, bhName);
            }

            return oldHuangDiRoleID;
        }

        /// <summary>
        /// 通知GameServer同步设置之源拥有者
        /// </summary>
        public static void NotifySyncHuanDiRoleInfo(int oldHuangDiRoleID, int roleID, string roleName, string bhName)
        {
            //通知其他线路
            string gmCmdData = string.Format("-synchuangdi {0} {1} {2} {3}", oldHuangDiRoleID, roleID, roleName, bhName);

            //转发GM消息到DBServer
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", -1, "", 0, "", 0, gmCmdData, 0, 0, GameManager.ServerLineID),
                null, GameManager.LocalServerId);
        }

        #endregion 处理提取舍利之源的操作

        #region 处理皇城战的胜负结果

        /// <summary>
        /// 持有舍利之源决定胜负的最长时间
        /// </summary>
        private static int MaxHavingSheLiZhiYuanSecs = (20 * 60);

        /// <summary>
        /// 皇城战的举行的周日期
        /// </summary>
        private static int[] HuangChengZhanWeekDays = null;

        /// <summary>
        /// 皇城战的举行的时间段
        /// </summary>
        private static DateTimeRange[] HuangChengZhanFightingDayTimes = null;

        /// <summary>
        /// 解析皇城战的日期和时间
        /// </summary>
        public static void ParseWeekDaysTimes()
        {
            string huangChengZhanWeekDays_str = GameManager.systemParamsList.GetParamValueByName("HuangChengZhanWeekDays");
            if (!string.IsNullOrEmpty(huangChengZhanWeekDays_str))
            {
                string[] huangChengZhanWeekDays_fields = huangChengZhanWeekDays_str.Split(',');
                int[] weekDays = new int[huangChengZhanWeekDays_fields.Length];
                for (int i = 0; i < huangChengZhanWeekDays_fields.Length; i++)
                {
                    weekDays[i] = Global.SafeConvertToInt32(huangChengZhanWeekDays_fields[i]);
                }

                HuangChengZhanWeekDays = weekDays;
            }

            string huangChengZhanFightingDayTimes_str = GameManager.systemParamsList.GetParamValueByName("HuangChengZhanFightingDayTimes");
            HuangChengZhanFightingDayTimes = Global.ParseDateTimeRangeStr(huangChengZhanFightingDayTimes_str);

            MaxHavingSheLiZhiYuanSecs = (int)GameManager.systemParamsList.GetParamValueIntByName("MaxHavingSheLiZhiYuanSecs");
            MaxHavingSheLiZhiYuanSecs *= 1000;
        }

        /// <summary>
        /// 判断周日期是否相符
        /// </summary>
        /// <param name="weekDayID"></param>
        /// <returns></returns>
        private static bool IsDayOfWeek(int weekDayID)
        {
            if (null == HuangChengZhanWeekDays) return false;
            for (int i = 0; i < HuangChengZhanWeekDays.Length; i++)
            {
                if (HuangChengZhanWeekDays[i] == weekDayID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 是否在皇城战时间段内
        /// </summary>
        /// <returns></returns>
        public static bool IsInHuangChengFightingTime()
        {
            DateTime now = TimeUtil.NowDateTime();
            int weekDayID = (int)now.DayOfWeek;
            if (!IsDayOfWeek(weekDayID))
            {
                return false;
            }

            int endMinute = 0;
            return Global.JugeDateTimeInTimeRange(now, HuangChengZhanFightingDayTimes, out endMinute, false);
        }

        /// <summary>
        /// 是否能否提取舍利之源
        /// </summary>
        /// <returns></returns>
        public static bool CanTakeSheLiZhiYuan()
        {
            if (HuangDiRoleID > 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 皇城战是否已经结束
        /// </summary>
        /// <returns></returns>
        public static bool IsHuangChengZhanOver()
        {
            return !WaitingHuangChengResult;
        }

        /// <summary>
        /// 皇城战的状态类型
        /// </summary>
        public static HuangChengZhanStates HuangChengZhanState = HuangChengZhanStates.None;

        /// <summary>
        /// 处理皇城战的战斗结果
        /// </summary>
        public static void ProcessHuangChengZhanResult()
        {
            if (Global.GetBangHuiFightingLineID() != GameManager.ServerLineID)
            {
                return;
            }

            if (HuangChengZhanStates.None == HuangChengZhanState) //非战斗状态
            {
                if (IsInHuangChengFightingTime())
                {
                    HuangChengZhanState = HuangChengZhanStates.Fighting;
                    HuangDiRoleTicks = TimeUtil.NOW();
                    WaitingHuangChengResult = true;

                    //通知地图数据变更信息
                    NotifyAllHuangChengMapInfoData();

                    //处理拥有皇帝特效的角色不在皇城地图，而失去皇帝特效的事件
                    HandleOutMapHuangDiRoleChanging();
                }
            }
            else //战斗状态
            {
                if (IsInHuangChengFightingTime()) //还在战斗期间
                {
                    if (WaitingHuangChengResult) //如果皇帝还未产生
                    {
                        //处理拥有皇帝特效的角色不在皇城地图，而失去皇帝特效的事件
                        HandleOutMapHuangDiRoleChanging();

                        //判断是否拥有舍利之源的超过了指定的时间
                        if (HuangDiRoleID > 0) //角色拥有舍利之源
                        {
                            long ticks = TimeUtil.NOW();
                            if (ticks - HuangDiRoleTicks > MaxHavingSheLiZhiYuanSecs)
                            {
                                //皇帝产生了
                                WaitingHuangChengResult = false;

                                //谁拥有舍利之源，谁就是皇帝
                                //处理皇城的归属
                                HandleHuangChengResult();

                                //通知地图数据变更信息
                                NotifyAllHuangChengMapInfoData();
                            }
                        }
                    }
                }
                else
                {
                    //战斗结束
                    HuangChengZhanState = HuangChengZhanStates.None;

                    //如果皇帝还未产生
                    if (WaitingHuangChengResult)
                    {
                        //皇帝产生了
                        WaitingHuangChengResult = false;

                        //谁拥有舍利之源，谁就是皇帝
                        //处理皇城的归属
                        HandleHuangChengResult();

                        //通知地图数据变更信息
                        NotifyAllHuangChengMapInfoData();
                    }
                }
            }
        }

        /// <summary>
        /// 通知地图数据变更信息
        /// </summary>
        public static void NotifyAllHuangChengMapInfoData()
        {
            HuangChengMapInfoData huangChengMapInfoData = FormatHuangChengMapInfoData();

            //通知在线的所有人(不限制地图)领地信息数据通知
            GameManager.ClientMgr.NotifyAllHuangChengMapInfoData(Global.GetHuangChengMapCode(), huangChengMapInfoData);
        }

        /// <summary>
        /// 处理皇城战流产
        /// </summary>
        private static void HandleHuangChengFailed()
        {
            //处理领地战的结果
            JunQiManager.HandleLingDiZhanResultByMapCode((int)LingDiIDs.HuangCheng, Global.GetHuangChengMapCode(), 0, true, false);

            //处理皇城战结束时的奖励
            ProcessHuangChengFightingEndAwards(-1);

            //皇城流产的提示
            Global.BroadcastHuangChengFailedHint();

            //通知GameServer同步势力的所属和范围
            JunQiManager.NotifySyncBangHuiJunQiItemsDict(null);
        }

        /// <summary>
        /// 处理皇城的归属
        /// </summary>
        private static void HandleHuangChengResult()
        {
            if (HuangDiRoleID <= 0)
            {
                //处理皇城战流产
                HandleHuangChengFailed();
                return; //谁也当不成皇帝
            }

            GameClient otherClient = GameManager.ClientMgr.FindClient(HuangDiRoleID);
            if (null == otherClient)
            {
                //处理皇城战流产
                HandleHuangChengFailed();
                return; //谁也当不成皇帝
            }

            if (otherClient.ClientData.Faction <= 0) //如果此时脱离了帮会
            {
                //处理皇城战流产
                HandleHuangChengFailed();
                return; //谁也当不成皇帝
            }

            if (otherClient.ClientData.BHZhiWu != 1) //如果此时不是帮会的帮主
            {
                //处理皇城战流产
                HandleHuangChengFailed();
                return; //谁也当不成皇帝
            }

            //处理领地战的结果
            JunQiManager.HandleLingDiZhanResultByMapCode((int)LingDiIDs.HuangCheng, Global.GetHuangChengMapCode(), otherClient.ClientData.Faction, true, false);

            //处理皇城战结束时的奖励
            ProcessHuangChengFightingEndAwards(otherClient.ClientData.Faction);

            //夺取皇城的提示
            Global.BroadcastHuangChengOkHint(otherClient);

            //通知GameServer同步势力的所属和范围
            JunQiManager.NotifySyncBangHuiJunQiItemsDict(null);
        }

        /// <summary>
        /// 处理拥有皇帝特效的角色不在皇城地图，而失去皇帝特效的事件
        /// </summary>
        public static void HandleOutMapHuangDiRoleChanging()
        {
            if (HuangDiRoleID <= 0)
            {
                return;
            }

            GameClient client = GameManager.ClientMgr.FindClient(HuangDiRoleID);
            if (null == client || client.ClientData.MapCode != Global.GetHuangChengMapCode())
            {
                //处理拥有皇帝特效的角色死亡，而失去皇帝特效的事件
                HandleDeadHuangDiRoleChanging(null);
            }
        }

        /// <summary>
        /// 处理拥有皇帝特效的角色离开皇城地图，而失去皇帝特效的事件
        /// </summary>
        public static void HandleLeaveMapHuangDiRoleChanging(GameClient client)
        {
            if (HuangDiRoleID <= 0)
            {
                return;
            }

            if (client.ClientData.RoleID != HuangDiRoleID)
            {
                return;
            }

            if (!WaitingHuangChengResult) //不在皇城在期间，则不判断
            {
                return;
            }

            //处理拥有皇帝特效的角色死亡，而失去皇帝特效的事件
            HandleDeadHuangDiRoleChanging(null);
        }

        /// <summary>
        /// 处理拥有皇帝特效的角色死亡，而失去皇帝特效的事件
        /// </summary>
        public static void HandleDeadHuangDiRoleChanging(GameClient client)
        {
            if (null != client)
            {
                if (client.ClientData.RoleID != HuangDiRoleID)
                {
                    return;
                }

                if ((int)LingDiIDs.HuangCheng != JunQiManager.GetLingDiIDBy2MapCode(client.ClientData.MapCode))
                {
                    return;
                }

                if (!IsInHuangChengFightingTime())
                {
                    return;
                }

                //皇帝产生了
                if (!WaitingHuangChengResult)
                {
                    return;
                }
            }

            int oldHuangDiRoleID = 0;

            //先锁定互斥，防止重复安插的操作
            lock (HuangChengManager.SheLiZhiYuanMutex)
            {
                //处理提取舍利之源的操作
                oldHuangDiRoleID = HuangChengManager.ProcessTakeSheLiZhiYuan(0, "", "");
            }

            if (oldHuangDiRoleID > 0)
            {
                GameClient oldClient = GameManager.ClientMgr.FindClient(oldHuangDiRoleID);
                if (null != oldClient)
                {
                    //从buffer数据到列表删除指定的临时Buffer
                    Global.RemoveBufferData(oldClient, (int)BufferItemTypes.SheLiZhiYuan);
                }
            }

            //通知在线的所有人(不限制地图)皇帝角色ID变更消息
            GameManager.ClientMgr.NotifyAllChgHuangDiRoleIDMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, oldHuangDiRoleID, HuangChengManager.GetHuangDiRoleID());

            //通知地图数据变更信息
            NotifyAllHuangChengMapInfoData();
        }

        #endregion 处理皇城战的胜负结果

        #region 地图战斗状态数据

        /// <summary>
        /// 获取地图战斗状态数据
        /// </summary>
        /// <returns></returns>
        public static HuangChengMapInfoData GetHuangChengMapInfoData(GameClient client)
        {
            //防止普通地图上的旗帜被攻击
            int lingDiID = JunQiManager.GetLingDiIDBy2MapCode(client.ClientData.MapCode);
            if (lingDiID != (int)LingDiIDs.HuangCheng)
            {
                return null;
            }

            return FormatHuangChengMapInfoData();
        }

        /// <summary>
        /// 获取地图战斗状态数据
        /// </summary>
        /// <returns></returns>
        public static HuangChengMapInfoData FormatHuangChengMapInfoData()
        {
            HuangChengMapInfoData HuangChengMapInfoData = new HuangChengMapInfoData()
            {
                FightingEndTime = HuangDiRoleTicks,
                HuangDiRoleID = HuangDiRoleID,
                HuangDiRoleName = HuangDiRoleName,
                HuangDiBHName = HuangDiBHName,
                FightingState = WaitingHuangChengResult ? 1 : 0,
                NextBattleTime = "",
                WangZuBHid = -1,
            };

            return HuangChengMapInfoData;
        }

        #endregion 地图战斗状态数据

        #region 奖励处理

        /// <summary>
        /// 获取角色的奖励经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static int GetExperienceAwards(GameClient client, bool success)
        {
            if (success)
            {
                return (100 * 10000);
            }

            return (50 * 10000);
        }

        /// <summary>
        /// 获取角色的军贡奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static int GetBangGongAwards(GameClient client, bool success)
        {
            if (success)
            {
                return (100);
            }

            return (50);
        }

        /// <summary>
        /// 处理用户的经验奖励
        /// </summary>
        /// <param name="client"></param>
        private static void ProcessRoleExperienceAwards(GameClient client, bool success)
        {
            //奖励用户经验
            //异步写数据库，写入经验和级别
            int experience = GetExperienceAwards(client, success);

            //处理角色经验
            GameManager.ClientMgr.ProcessRoleExperience(client, experience, true, false);
        }

        /// <summary>
        /// 处理用户的军贡奖励
        /// </summary>
        /// <param name="client"></param>
        private static void ProcessRoleBangGongAwards(GameClient client, bool success)
        {
            //奖励用户军贡
            //异步写数据库，写入军贡
            int bangGong = GetBangGongAwards(client, success);
            if (bangGong > 0)
            {
                //更新用户帮贡
                if (GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ref bangGong, AddBangGongTypes.None))
                {
                    //[bing] 记录战功增加流向log
                    if (0 != bangGong)
                        GameManager.logDBCmdMgr.AddDBLogInfo(-1, "战功", "皇城战结束时的奖励", "系统", client.ClientData.RoleName, "增加", bangGong, client.ClientData.ZoneID, client.strUserID, client.ClientData.BangGong, client.ServerId);
                }

                GameManager.SystemServerEvents.AddEvent(string.Format("角色获取帮贡, roleID={0}({1}), BangGong={2}, newBangGong={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.BangGong, bangGong), EventLevels.Record);
            }
        }

        /// <summary>
        /// 是否能够获取奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static bool CanGetAWards(GameClient client, long nowTicks)
        {
            if (nowTicks - client.ClientData.EnterMapTicks < MaxHavingSheLiZhiYuanSecs)
            {
                return false;
            }

            if (client.ClientData.Faction <= 0) return false;

            BangHuiLingDiItemData bangHuiLingDiItemData = JunQiManager.GetAnyLingDiItemDataByBHID(client.ClientData.Faction);
            if (null == bangHuiLingDiItemData) return false;
            return true;
        }

        /// <summary>
        /// 处理皇城战结束时的奖励
        /// </summary>
        private static void ProcessHuangChengFightingEndAwards(int huangDiBHID)
        {
            //先找出当前大乱斗中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(Global.GetHuangChengMapCode());
            if (null == objsList) return;

            long nowTicks = TimeUtil.NOW();
            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;
                if (c.ClientData.CurrentLifeV <= 0) continue;

                //是否能够获取奖励
                if (!CanGetAWards(c, nowTicks))
                {
                    continue;
                }

                /// 处理用户的经验奖励
                ProcessRoleExperienceAwards(c, (huangDiBHID == c.ClientData.Faction));

                /// 处理用户的金钱奖励
                ProcessRoleBangGongAwards(c, (huangDiBHID == c.ClientData.Faction));
            }
        }

        #endregion 奖励处理

        #region 选妃安全随机数字典

        /// <summary>
        /// 保存选妃随机数的安全字典
        /// </summary>
        private static Dictionary<int, int> XuanFeiSafeDict = new Dictionary<int, int>();

        /// <summary>
        /// 获取一个选妃的安全随机数
        /// </summary>
        /// <returns></returns>
        public static int NewXuanFeiSafeNum(int roleID)
        {
            lock (XuanFeiSafeDict)
            {
                int randNum = Global.GetRandomNumber(0, 1000001);
                XuanFeiSafeDict[roleID] = randNum;
                return randNum;
            }
        }

        /// <summary>
        /// 查找选妃随机数
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static int FindXuanFeiSafeNum(int roleID)
        {
            lock (XuanFeiSafeDict)
            {
                int randNum = 0;
                if (!XuanFeiSafeDict.TryGetValue(roleID, out randNum))
                {
                    return -1;
                }
                
                return randNum;
            }
        }

        /// <summary>
        /// 删除选妃随机数
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static void RemoveXuanFeiSafeNum(int roleID)
        {
            lock (XuanFeiSafeDict)
            {
                XuanFeiSafeDict.Remove(roleID);
            }
        }

        #endregion 选妃安全随机数字典
    }
}
