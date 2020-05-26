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
    /// 弹窗时间信息项
    /// </summary>
    public class PopupWinTimeItem
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
    /// 弹窗信息项
    /// </summary>
    public class PopupWinItem
    {
        /// <summary>
        /// 弹窗的信息ID
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// 弹窗的文件ID
        /// </summary>
        public int HintFileID = 0;

        /// <summary>
        /// 播放的时间项列表
        /// </summary>
        public PopupWinTimeItem[] Times = null;
    }

    /// <summary>
    /// 弹出窗口管理
    /// </summary>
    public class PopupWinMgr
    {
        #region 加载弹窗列表

        /// <summary>
        /// 弹窗的信息列表
        /// </summary>
        private static List<PopupWinItem> PopupWinItemList = null;

        /// <summary>
        /// 加载弹窗列表
        /// </summary>
        public static void LoadPopupWinItemList()
        {
            XElement xml = null;
            string fileName = "Config/PopupWin.xml";

            try
            {
                xml = XElement.Load(Global.GameResPath(fileName));
                if (null == xml)
                {
                    throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            List<PopupWinItem> popupWinItemList = new List<PopupWinItem>();
            SystemXmlItem systemXmlItem = null;
            IEnumerable<XElement> nodes = xml.Elements();
            foreach (var node in nodes)
            {
                systemXmlItem = new SystemXmlItem()
                {
                    XMLNode = node,
                };

                //解析Xml项
                ParseXmlItem(systemXmlItem, popupWinItemList);
            }

            PopupWinItemList = popupWinItemList;
        }

        /// <summary>
        /// 解析Xml项
        /// </summary>
        /// <param name="systemXmlItem"></param>
        private static void ParseXmlItem(SystemXmlItem systemXmlItem, List<PopupWinItem> popupWinItemList)
        {
            int id = systemXmlItem.GetIntValue("ID");
            int hintFileID = systemXmlItem.GetIntValue("HintFileID");
            string times = systemXmlItem.GetStringValue("Times");

            if (string.IsNullOrEmpty(times))
            {
                //LogManager.WriteLog(LogTypes.Error, string.Format("解析弹窗配置表中的时间项失败, ID={0}", id));
                return;
            }

            PopupWinTimeItem[] popupWinTimeItemArray = ParsePopupWinTimeItems(times);
            if (null == popupWinTimeItemArray)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析弹窗配置表中的时间项为数组时失败, ID={0}", id));
                return;
            }

            PopupWinItem popupWinItem = new PopupWinItem()
            {
                ID = id,
                HintFileID = hintFileID,
                Times = popupWinTimeItemArray,
            };

            popupWinItemList.Add(popupWinItem);
        }

        /// <summary>
        /// 解析时间段
        /// </summary>
        /// <param name="times"></param>
        /// <returns></returns>
        private static PopupWinTimeItem[] ParsePopupWinTimeItems(string times)
        {
            string[] fields = times.Split('|');
            if (fields.Length <= 0)
            {
                return null;
            }

            PopupWinTimeItem[] popupWinTimeItemArray = new PopupWinTimeItem[fields.Length];
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

                popupWinTimeItemArray[i] = new PopupWinTimeItem()
                {
                    Hour = Global.SafeConvertToInt32(fields2[0]),
                    Minute = Global.SafeConvertToInt32(fields2[1]),
                };
            }

            return popupWinTimeItemArray;
        }

        #endregion 加载弹窗列表

        #region 驱动弹窗列表

        /// <summary>
        /// 上次弹窗的天
        /// </summary>
        private static int LastPopupWinDay = TimeUtil.NowDateTime().DayOfYear;

        /// <summary>
        /// 上一次弹窗的时间记录
        /// </summary>
        private static PopupWinTimeItem LastPopupWinTimeItem = new PopupWinTimeItem() { Hour = TimeUtil.NowDateTime().Hour, Minute = TimeUtil.NowDateTime().Minute, };

        /// <summary>
        /// 是否能否弹窗
        /// </summary>
        /// <param name="broadcastInfoItem"></param>
        /// <returns></returns>
        private static bool CanPopupWin(PopupWinItem popupWinItem, PopupWinTimeItem lastPopupWinTimeItem, int hour, int minute)
        {
            if (null == popupWinItem.Times) return false;

            int time2 = lastPopupWinTimeItem.Hour * 60 + lastPopupWinTimeItem.Minute;
            int time3 = hour * 60 + minute;
            for (int i = 0; i < popupWinItem.Times.Length; i++)
            {
                int time1 = popupWinItem.Times[i].Hour * 60 + popupWinItem.Times[i].Minute;
                if (time1 <= time2) //已经广播过了
                {
                    continue;
                }

                if (time3 >= time1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 驱动弹窗列表
        /// </summary>
        public static void ProcessPopupWins()
        {
            DateTime now = TimeUtil.NowDateTime();
            int day = now.DayOfYear;
            int hour = now.Hour;
            int minute = now.Minute;

            if (day != LastPopupWinDay)
            {
                LastPopupWinDay = day;
                LastPopupWinTimeItem.Hour = hour;
                LastPopupWinTimeItem.Minute = minute;
                return;
            }

            //时间相同，不处理
            if (hour == LastPopupWinTimeItem.Hour && minute == LastPopupWinTimeItem.Minute)
            {
                return;
            }

            List<PopupWinItem> popupWinItemList = PopupWinItemList;
            if (null == popupWinItemList || popupWinItemList.Count <= 0)
            {
                LastPopupWinDay = day;
                LastPopupWinTimeItem.Hour = hour;
                LastPopupWinTimeItem.Minute = minute;
                return;
            }

            for (int i = 0; i < popupWinItemList.Count; i++)
            {
                if (CanPopupWin(popupWinItemList[i], LastPopupWinTimeItem, hour, minute))
                {
                    string strcmd = string.Format("{0}", popupWinItemList[i].HintFileID);

                    //通知在线的所有人(不限制地图)弹窗消息
                    GameManager.ClientMgr.NotifyAllPopupWinMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, strcmd);
                }
            }

            LastPopupWinDay = day;
            LastPopupWinTimeItem.Hour = hour;
            LastPopupWinTimeItem.Minute = minute;
        }

        /// <summary>
        /// 驱动针对用户的弹窗列表
        /// </summary>
        public static void ProcessClientPopupWins(GameClient client)
        {
            List<PopupWinItem> popupWinItemList = PopupWinItemList;
            if (null == popupWinItemList || popupWinItemList.Count <= 0)
            {
                return;
            }

            if (popupWinItemList[0].Times.Length <= 0)
            {
                return;
            }

            string strcmd = string.Format("{0}", popupWinItemList[0].HintFileID);

            //通知在线的对方(不限制地图)公告消息
            GameManager.ClientMgr.NotifyPopupWinMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, strcmd);
        }
        

        #endregion 驱动弹窗列表
    }
}
