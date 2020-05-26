using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System.Windows;
//using System.Windows.Documents;
using GameServer.Server;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 时间信息项
    /// </summary>
    public class BroadcastTimeItem
    {
        /// <summary>
        /// 小时
        /// </summary>
        public int Hour = 0;

        /// <summary>
        /// 分钟
        /// </summary>
        public int Minute = 0;
    }

    /// <summary>
    /// 广播信息项
    /// </summary>
    public class BroadcastInfoItem
    {
        /// <summary>
        /// 广播的信息ID
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// 播放的信息类型, 0: 公告模式， 1: 提示信息模式
        /// </summary>
        public int InfoClass = 0;

        /// <summary>
        /// 错误信息码
        /// </summary>
        public int HintErrID = -1;

        /// <summary>
        /// 时间的类型
        /// </summary>
        public int TimeType = 0;

        /// <summary>
        /// 开服后几天
        /// </summary>
        public int KaiFuStartDay = -1;

        /// <summary>
        /// 开服后几天的播报类型
        /// </summary>
        public int KaiFuShowType = -1;

        /// <summary>
        /// 是否是周ID
        /// </summary>
        public string WeekDays = "";

        /// <summary>
        /// 播放的时间项列表
        /// </summary>
        public BroadcastTimeItem[] Times = null;

        /// <summary>
        /// 上线提示时间项列表
        /// </summary>
        public DateTimeRange[] OnlineNoticeTimeRanges = null;

        /// <summary>
        /// 要播放的文字信息
        /// </summary>
        public string Text = "";

        /// <summary>
        /// 最小转生等级要求
        /// </summary>
        public int MinZhuanSheng = 0;

        /// <summary>
        /// 最小等级要求
        /// </summary>
        public int MinLevel = 0;
    }

    /// <summary>
    /// 广播信息管理
    /// </summary>
    public class BroadcastInfoMgr
    {
        #region 加载广播列表

        /// <summary>
        /// 播放的文字信息列表
        /// </summary>
        private static List<BroadcastInfoItem> BroadcastInfoItemList = null;

        /// <summary>
        /// 加载文字播放列表
        /// </summary>
        public static void LoadBroadcastInfoItemList()
        {
            XElement xml = null;
            string fileName = "Config/BroadcastInfos.xml";

            try
            {
                xml = XElement.Load(Global.IsolateResPath(fileName));
                if (null == xml)
                {
                    throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            List<BroadcastInfoItem> broadcastInfoItemList = new List<BroadcastInfoItem>();
            SystemXmlItem systemXmlItem = null;
            IEnumerable<XElement> nodes = xml.Elements("Infos").Elements();
            foreach (var node in nodes)
            {
                systemXmlItem = new SystemXmlItem()
                {
                    XMLNode = node,
                };

                //解析Xml项
                ParseXmlItem(systemXmlItem, broadcastInfoItemList);
            }

            BroadcastInfoItemList = broadcastInfoItemList;
        }

        /// <summary>
        /// 解析Xml项
        /// </summary>
        /// <param name="systemXmlItem"></param>
        private static void ParseXmlItem(SystemXmlItem systemXmlItem, List<BroadcastInfoItem> broadcastInfoItemList)
        {
            int id = systemXmlItem.GetIntValue("ID");
            int infoClass = systemXmlItem.GetIntValue("InfoClass");
            int hintErrID = systemXmlItem.GetIntValue("HintErrID");
            int timeType = systemXmlItem.GetIntValue("TimeType");
            int kaiFuStartDay = systemXmlItem.GetIntValue("StartDay");
            int kaiFuShowType = systemXmlItem.GetIntValue("ShowType");
            string weekDays = systemXmlItem.GetStringValue("WeekDays");
            string times = systemXmlItem.GetStringValue("Times");
            string text = systemXmlItem.GetStringValue("Text");
            string onlineNotice = systemXmlItem.GetStringValue("OnlineNotice");
            int minZhuanSheng = systemXmlItem.GetIntValue("MinZhuanSheng");
            int minLevel = systemXmlItem.GetIntValue("MinLevel");

            if (string.IsNullOrEmpty(times))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析广播配置表中的时间项失败, ID={0}", id));
                return;
            }

            BroadcastTimeItem[] broadcastTimeItemArray = ParseBroadcastTimeItems(times);
            if (null == broadcastTimeItemArray)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析广播配置表中的时间项为数组时失败, ID={0}", id));
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析广播配置表中的时间项失败, ID={0}", id));
                return;
            }

            DateTimeRange[] onlineNoticeTimeRanges = Global.ParseDateTimeRangeStr(onlineNotice);

            BroadcastInfoItem broadcastInfoItem = new BroadcastInfoItem()
            {
                ID = id,
                InfoClass = infoClass,
                HintErrID = hintErrID,
                TimeType = timeType,
                KaiFuStartDay = kaiFuStartDay,
                KaiFuShowType = kaiFuShowType,
                WeekDays = weekDays,
                Times = broadcastTimeItemArray,
                OnlineNoticeTimeRanges = onlineNoticeTimeRanges,
                Text = text.Replace(":", ""), //防止出现半角的冒号
                MinZhuanSheng = minZhuanSheng,
                MinLevel = minLevel,
            };
            
            broadcastInfoItemList.Add(broadcastInfoItem);
        }

        /// <summary>
        /// 解析时间段
        /// </summary>
        /// <param name="times"></param>
        /// <returns></returns>
        private static BroadcastTimeItem[] ParseBroadcastTimeItems(string times)
        {
            if (string.IsNullOrEmpty(times))
            {
                return null;
            }

            string[] fields = times.Split('|');
            if (fields.Length <= 0)
            {
                return null;
            }

            BroadcastTimeItem[] broadcastTimeItemArray = new BroadcastTimeItem[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                string str = fields[i].Trim();
                if (string.IsNullOrEmpty(str))
                {
                    return null;
                }

                string[] fields2 = str.Split(':');
                if (null == fields2 || fields2.Length != 2)
                {
                    return null;
                }

                broadcastTimeItemArray[i] = new BroadcastTimeItem()
                {
                    Hour = Global.SafeConvertToInt32(fields2[0]),
                    Minute = Global.SafeConvertToInt32(fields2[1]),
                };
            }

            return broadcastTimeItemArray;
        }

        #endregion 加载广播列表

        #region 驱动广播列表

        /// <summary>
        /// 上次播放的天
        /// </summary>
        private static int LastBroadcastDay = TimeUtil.NowDateTime().DayOfYear;

        /// <summary>
        /// 上一次广播的时间记录
        /// </summary>
        private static BroadcastTimeItem LastBroadcastTimeItem = new BroadcastTimeItem() { Hour = TimeUtil.NowDateTime().Hour, Minute = TimeUtil.NowDateTime().Minute, };

        /// <summary>
        /// 是否能否播放
        /// </summary>
        /// <param name="broadcastInfoItem"></param>
        /// <returns></returns>
        private static bool CanBroadcast(BroadcastInfoItem broadcastInfoItem, BroadcastTimeItem lastBroadcastTimeItem, int weekDayID, int hour, int minute)
        {
            if (null == broadcastInfoItem.Times) return false;

            //如果要限制周日期
            if (!string.IsNullOrEmpty(broadcastInfoItem.WeekDays))
            {
                if (-1 == broadcastInfoItem.WeekDays.IndexOf(weekDayID.ToString()))
                {
                    return false;
                }
            }

            if (broadcastInfoItem.KaiFuStartDay > 0) //根据开服后几天来播报
            {
                DateTime jugeDateTime = Global.GetKaiFuTime();
                if (2 == broadcastInfoItem.TimeType) //合服
                {
                    jugeDateTime = Global.GetHefuStartDay();
                }
                else if (3 == broadcastInfoItem.TimeType) //节日
                {
                    jugeDateTime = Global.GetJieriStartDay();
                }

                DateTime todayTime = TimeUtil.NowDateTime();

                int currday = Global.GetOffsetDay(todayTime);
                int jugeday = Global.GetOffsetDay(jugeDateTime);
                if (currday - jugeday >= broadcastInfoItem.KaiFuStartDay)
                {
                    if (broadcastInfoItem.KaiFuShowType > 0)
                    {
                        if (currday - jugeday < (broadcastInfoItem.KaiFuStartDay + broadcastInfoItem.KaiFuShowType))
                        {
                            ///可以播报
                        }
                        else
                        {
                            return false; //不播报
                        }
                    }
                }
                else
                {
                    return false; //不播报
                }
            }

            int lastTime = lastBroadcastTimeItem.Hour * 60 + lastBroadcastTimeItem.Minute;
            int nowTime = hour * 60 + minute;
            for (int i = 0; i < broadcastInfoItem.Times.Length; i++)
            {
                int itemTime = broadcastInfoItem.Times[i].Hour * 60 + broadcastInfoItem.Times[i].Minute;
                if (itemTime <= lastTime) //已经广播过了
                {
                    continue;
                }

                if (nowTime >= itemTime)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 驱动广播列表
        /// </summary>
        public static void ProcessBroadcastInfos()
        {
            DateTime now = TimeUtil.NowDateTime();
            int weekDayID = (int)now.DayOfWeek;
            int day = now.DayOfYear;
            int hour = now.Hour;
            int minute = now.Minute;

            if (day != LastBroadcastDay)
            {
                LastBroadcastDay = day;
                LastBroadcastTimeItem.Hour = hour;
                LastBroadcastTimeItem.Minute = minute;
                return;
            }

            //时间相同，不处理
            if (hour == LastBroadcastTimeItem.Hour && minute == LastBroadcastTimeItem.Minute)
            {
                return;
            }

            List<BroadcastInfoItem> broadcastInfoItemList = BroadcastInfoItemList;
            if (null == broadcastInfoItemList || broadcastInfoItemList.Count <= 0)
            {
                LastBroadcastDay = day;
                LastBroadcastTimeItem.Hour = hour;
                LastBroadcastTimeItem.Minute = minute;
                return;
            }

            for (int i = 0; i < broadcastInfoItemList.Count; i++)
            {
                if (CanBroadcast(broadcastInfoItemList[i], LastBroadcastTimeItem, weekDayID, hour, minute))
                {
                    if (broadcastInfoItemList[i].InfoClass <= 1)
                    {
                        //播放用户行为消息
                        Global.BroadcastRoleActionMsg(null, broadcastInfoItemList[i].InfoClass == 0 ? RoleActionsMsgTypes.Bulletin : RoleActionsMsgTypes.HintMsg,
                            broadcastInfoItemList[i].Text, false, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint,
                            broadcastInfoItemList[i].MinZhuanSheng, broadcastInfoItemList[i].MinLevel);
                    }
                    else if (3 == broadcastInfoItemList[i].InfoClass)
                    {
                        GameManager.ClientMgr.NotifyAllImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null,
                            broadcastInfoItemList[i].Text,
                            GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, Math.Max(broadcastInfoItemList[i].HintErrID, 0),
                            broadcastInfoItemList[i].MinZhuanSheng, broadcastInfoItemList[i].MinLevel);
                    }
                }
            }

            LastBroadcastDay = day;
            LastBroadcastTimeItem.Hour = hour;
            LastBroadcastTimeItem.Minute = minute;
        }

        /// <summary>
        /// 登录时广播列表
        /// </summary>
        public static void LoginBroadcastInfos(GameClient client)
        {
            DateTime now = TimeUtil.NowDateTime();
            List<BroadcastInfoItem> broadcastInfoItemList = BroadcastInfoItemList;
            if (null == broadcastInfoItemList || broadcastInfoItemList.Count <= 0)
            {
                return;
            }

            DateTime todayTime = now;

            int weekDayID = (int)now.DayOfWeek;
            for (int i = 0; i < broadcastInfoItemList.Count; i++)
            {
                if (broadcastInfoItemList[i].InfoClass != 3)
                {
                    continue;
                }

                if (null == broadcastInfoItemList[i].OnlineNoticeTimeRanges)
                {
                    continue;
                }

                if (Global.GetUnionLevel(client) < Global.GetUnionLevel(broadcastInfoItemList[i].MinZhuanSheng, broadcastInfoItemList[i].MinLevel))
                {
                    continue;
                }

                int endMinute = 0;
                if (!Global.JugeDateTimeInTimeRange(now, broadcastInfoItemList[i].OnlineNoticeTimeRanges, out endMinute))
                {
                    continue;
                }

                //如果要限制周日期
                if (!string.IsNullOrEmpty(broadcastInfoItemList[i].WeekDays))
                {
                    if (-1 == broadcastInfoItemList[i].WeekDays.IndexOf(weekDayID.ToString()))
                    {
                        continue;
                    }
                }

                if (broadcastInfoItemList[i].KaiFuStartDay > 0) //根据开服后几天来播报
                {
                    DateTime jugeDateTime = Global.GetKaiFuTime();
                    if (2 == broadcastInfoItemList[i].TimeType) //合服
                    {
                        jugeDateTime = Global.GetHefuStartDay();
                    }
                    else if (3 == broadcastInfoItemList[i].TimeType) //节日
                    {
                        jugeDateTime = Global.GetJieriStartDay();
                    }

                    int currday = Global.GetOffsetDay(todayTime);
                    int jugeday = Global.GetOffsetDay(jugeDateTime);
                    if (currday - jugeday >= broadcastInfoItemList[i].KaiFuStartDay)
                    {
                        if (broadcastInfoItemList[i].KaiFuShowType > 0)
                        {
                            if (currday - jugeday < (broadcastInfoItemList[i].KaiFuStartDay + broadcastInfoItemList[i].KaiFuShowType))
                            {
                                ///可以播报
                            }
                            else
                            {
                                continue; //不播报
                            }
                        }
                    }
                    else
                    {
                        continue; //不播报
                    }
                }

                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    broadcastInfoItemList[i].Text,
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, Math.Max(broadcastInfoItemList[i].HintErrID, 0));
            }
        }

        #endregion 驱动广播列表
    }
}
