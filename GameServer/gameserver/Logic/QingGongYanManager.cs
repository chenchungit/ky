using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Server.Tools;
using Server.TCP;
using Server.Protocol;
using GameServer.Server;
using GameServer.Logic;
using Server.Data;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    public class QingGongYanInfo
    {
        public int Index;               // 流水号
        public int NpcID;               // NPC编号
        public int MapCode;             // 地图编号
        public int X;                   // NPC坐标
        public int Y;                   // NPC坐标
        public int Direction;           // NPC方向
        // 不可用天
        public List<string> ProhibitedTimeList = new List<string>();
        public string BeginTime;        // 开启时间
        public string OverTime;         // 结束时间
        public int FunctionID;          // 功能编号 SystemOperations.xml中对应的编号
        public int HoldBindJinBi;    // 举办者要花费的金币
        public int TotalNum;            // 能够参加的总数
        public int SingleNum;           // 个人能够参加的总数
        public int JoinBindJinBi;       // 参加所需的金币
        public int ExpAward;            // 参加能够获得的经验
        public int XingHunAward;        // 参加能够获得的星魂
        public int ZhanGongAward;       // 参加能够获得的战功
        public int ZuanShiCoe;          // 结束金币转换成钻石的比例 / ZuanShiCoe

        /// <summary>
        /// 判断某个时间，是否禁止申请举办
        /// </summary>
        public bool IfBanTime(DateTime time)
        {
            int dayofweek = (int)time.DayOfWeek;

            if (dayofweek == 0)
                dayofweek = 7;

            foreach (var item in ProhibitedTimeList)
            {
                string[] strFields = ProhibitedTimeList[0].Split(',');

                // 如果是配置的这天
                if (Convert.ToInt32(strFields[0]) == dayofweek)
                {
                    DateTime beginTime = DateTime.Parse(strFields[1]);
                    DateTime endTime = DateTime.Parse(strFields[2]);

                    if (time >= beginTime && time <= endTime)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public enum QingGongYanResult
    {
        Success = 0,        // 成功
        CheckSuccess,   // 检测成功
        ErrorParam,     // 参数错误
        Holding,        // 庆功宴正在举办
        NotKing,        // 不是王城占领者
        OutTime,        // 允许的时间外
        RepeatHold,     // 重复申请
        CountNotEnough, // 参加次数不足
        TotalNotEnough, // 总参加次数不足
        MoneyNotEnough, // 金钱不足
    }

    public class QingGongYanManager
    {
        #region 成员

        /// <summary>
        /// 庆功宴配置字典线程锁
        /// </summary>
        private object _QingGongYanMutex = new object();

        /// <summary>
        /// 配置字典
        /// </summary>
        private Dictionary<int, QingGongYanInfo> QingGongYanDict = new Dictionary<int, QingGongYanInfo>();

        /// <summary>
        /// 庆功宴的NPC是否刷新的标记
        /// 因为刷怪是异步操作，所以用这个做标记，防止生成多个Monster
        /// </summary>
        private bool QingGongYanOpenFlag = false;

        /// <summary>
        /// 庆功宴的NPC数据
        /// </summary>
        private NPC QingGongYanNpc = null;

        #endregion

        #region 配置文件

        /// <summary>
        /// 加载配置文件 GleeFeastAward.xml
        /// </summary>
        public void LoadQingGongYanConfig()
        {
            lock (_QingGongYanMutex)
            {
                QingGongYanDict.Clear();

                string fileName = "Config/GleeFeastAward.xml";
                GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
                if (null == xml) return;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    QingGongYanInfo InfoData = new QingGongYanInfo();
                    InfoData.Index = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    InfoData.NpcID = (int)Global.GetSafeAttributeLong(xmlItem, "NPCID");
                    InfoData.MapCode = (int)Global.GetSafeAttributeLong(xmlItem, "MapCode");
                    InfoData.X = (int)Global.GetSafeAttributeLong(xmlItem, "X");
                    InfoData.Y = (int)Global.GetSafeAttributeLong(xmlItem, "Y");
                    InfoData.Direction = (int)Global.GetSafeAttributeLong(xmlItem, "Direction");

                    // Week
                    /*string[] strWeek = Global.GetSafeAttributeStr(xmlItem, "Week").Split(',');
                    if (null != strWeek)
                    {
                        for (int i = 0; i < strWeek.Length; i++)
                        {
                            InfoData.DayOfWeek.Add(Convert.ToInt32(strWeek[i]));
                        }
                    }*/
                    string[] strBanTime = Global.GetSafeAttributeStr(xmlItem, "ProhibitedTime").Split('|');
                    for (int i = 0; i < strBanTime.Length; i++)
                    {
                        InfoData.ProhibitedTimeList.Add(strBanTime[i]);
                    }

                    InfoData.BeginTime = Global.GetSafeAttributeStr(xmlItem, "BeginTime");
                    InfoData.OverTime = Global.GetSafeAttributeStr(xmlItem, "OverTime");
                    InfoData.FunctionID = (int)Global.GetSafeAttributeLong(xmlItem, "FunctionID");

                    InfoData.HoldBindJinBi = (int)Global.GetSafeAttributeLong(xmlItem, "ConductBindJinBi");
                    InfoData.TotalNum = (int)Global.GetSafeAttributeLong(xmlItem, "SumNum");
                    InfoData.SingleNum = (int)Global.GetSafeAttributeLong(xmlItem, "UseNum");
                    InfoData.JoinBindJinBi = (int)Global.GetSafeAttributeLong(xmlItem, "BindJinBi");
                    InfoData.ExpAward = (int)Global.GetSafeAttributeLong(xmlItem, "EXPAward");
                    InfoData.XingHunAward = (int)Global.GetSafeAttributeLong(xmlItem, "XingHunAward");
                    InfoData.ZhanGongAward = (int)Global.GetSafeAttributeLong(xmlItem, "ZhanGongAward");
                    InfoData.ZuanShiCoe = (int)Global.GetSafeAttributeLong(xmlItem, "ZuanShiRatio");

                    QingGongYanDict[InfoData.Index] = InfoData;
                }
            }
        }

        /// <summary>
        /// 根据流水号取得庆功宴相关配置
        /// </summary>
        private QingGongYanInfo GetQingGongYanConfig(int index)
        {
            QingGongYanInfo InfoData = null;
            lock (_QingGongYanMutex)
            {
                if (QingGongYanDict.ContainsKey(index))
                    InfoData = QingGongYanDict[index];
            }
            return InfoData;
        }

        #endregion

        #region 逻辑相关

        /// <summary>
        /// 举办庆功宴
        /// </summary>
        public QingGongYanResult HoldQingGongYan(GameClient client, int index, int onlyCheck = 0)
        {
            // 是不是王城占领者
            if (!Global.IsKingCityLeader(client))
            {
                return QingGongYanResult.NotKing;
            }

            QingGongYanInfo InfoData = GetQingGongYanConfig(index);
            if (null == InfoData)
            {
                return QingGongYanResult.ErrorParam;
            }

            /// 此时是否能够开启庆功宴
            if (InfoData.IfBanTime(TimeUtil.NowDateTime()))
            {
                return QingGongYanResult.OutTime;
            }

            int DBStartDay = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.QGYStartDay, 0);
            int currDay = Global.GetOffsetDay(TimeUtil.NowDateTime());

            //  如果今天有庆功宴 并且庆功宴结束时间还没到 提示已经申请
            if (DBStartDay == currDay && TimeUtil.NowDateTime() <= DateTime.Parse(InfoData.OverTime))
            {
                return QingGongYanResult.RepeatHold;
            }

            // 计算申请之后，庆功宴的举办时间
            int startDay = 0;
            // 在庆功宴开始时间之前，就在今天开启
            if (TimeUtil.NowDateTime() < DateTime.Parse(InfoData.BeginTime))
            {
                startDay = currDay;
            }
            // 否则在明天开启
            else
            {
                startDay = currDay + 1;
            }

            // 如果计算出来的举办时间和数据库的举办时间相同，则返回已经申请
            if (startDay == DBStartDay)
            {
                return QingGongYanResult.RepeatHold;
            }

            // 检查举办所需金币是否足够
            if (InfoData.HoldBindJinBi > 0)
            {
                if (InfoData.HoldBindJinBi > Global.GetTotalBindTongQianAndTongQianVal(client))
                {
                    return QingGongYanResult.MoneyNotEnough;
                }
            }

            if (onlyCheck > 0)
            {
                return QingGongYanResult.CheckSuccess;
            }

            // 扣除举办所需金币
            if (InfoData.HoldBindJinBi > 0)
            {
                if (!Global.SubBindTongQianAndTongQian(client, InfoData.HoldBindJinBi, "举办庆功宴"))
                {
                    return QingGongYanResult.MoneyNotEnough;
                }
            }

            Global.UpdateDBGameConfigg(GameConfigNames.QGYRoleID, client.ClientData.RoleID.ToString());
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYRoleID, client.ClientData.RoleID.ToString());

            BangHuiMiniData bangHuiMiniData = Global.GetBangHuiMiniData(client.ClientData.Faction);
            if (null != bangHuiMiniData)
            {
                Global.UpdateDBGameConfigg(GameConfigNames.QGYGuildName, bangHuiMiniData.BHName);
                GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYGuildName, bangHuiMiniData.BHName);
            }
            else
            {
                Global.UpdateDBGameConfigg(GameConfigNames.QGYGuildName, "");
                GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYGuildName, "");
            }

            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYGuildName, client.ClientData.RoleName);
            Global.UpdateDBGameConfigg(GameConfigNames.QGYStartDay, startDay.ToString());
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYStartDay, startDay.ToString());
            Global.UpdateDBGameConfigg(GameConfigNames.QGYGrade, index.ToString());
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYGrade, index.ToString());
            Global.UpdateDBGameConfigg(GameConfigNames.QGYJoinCount, "0");
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYJoinCount, "0");
            Global.UpdateDBGameConfigg(GameConfigNames.QGYJoinMoney, "0");
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYJoinMoney, "0");

            // 为了合服，要记录举办人花了多少金币
            Global.UpdateDBGameConfigg(GameConfigNames.QGYJuBanMoney, InfoData.HoldBindJinBi.ToString());
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYJuBanMoney, InfoData.HoldBindJinBi.ToString());

            // log it...
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "举办庆功宴", startDay.ToString(), "", client.ClientData.RoleName, "", index, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId);
            EventLogManager.AddRoleEvent(client, OpTypes.Hold, OpTags.QingGongYan, LogRecordType.OffsetDayId, startDay);

            return QingGongYanResult.Success;
        }

        /// <summary>
        /// 此时是否需要开启庆功宴
        /// </summary>
        private bool IfNeedOpenQingGongYan()
        {
            // 没有档次
            QingGongYanInfo InfoData = GetInfoData();
            if (null == InfoData)
            {
                return false;
            }

            int DBStartDay = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.QGYStartDay, 0);
            int currDay = Global.GetOffsetDay(TimeUtil.NowDateTime());
            // 今天没有庆功宴
            if (DBStartDay != currDay)
            {
                return false;
            }

            // 在举办时间外
            if (TimeUtil.NowDateTime() < DateTime.Parse(InfoData.BeginTime) || TimeUtil.NowDateTime() > DateTime.Parse(InfoData.OverTime))
            {
                return false;
            }

            // 如果已经开启，刷过怪
            if (true == QingGongYanOpenFlag)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 此时是否需要关闭庆功宴
        /// </summary>
        private bool IfNeedCloseQingGongYan()
        {
            // 没有档次
            QingGongYanInfo InfoData = GetInfoData();
            if (null == InfoData)
            {
                return false;
            }

            int DBStartDay = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.QGYStartDay, 0);
            int currDay = Global.GetOffsetDay(TimeUtil.NowDateTime());
            // 今天没有庆功宴
            if (DBStartDay != currDay)
            {
                return false;
            }

            // 在结束时间之前
            if (TimeUtil.NowDateTime() <= DateTime.Parse(InfoData.OverTime))
            {
                return false;
            }

            // 没有开启的标志
            if (false == QingGongYanOpenFlag)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 定时记录标志
        /// </summary>
        private long lastProcessTicks = 0;

        /// <summary>
        /// 定时检测庆功宴是否要开启
        /// </summary>
        public void CheckQingGongYan(long ticks)
        {
            // 10秒检测一次
            if (ticks - lastProcessTicks < 1000 * 10)
            {
                return;
            }
            lastProcessTicks = ticks;

            if (IfNeedOpenQingGongYan())
            {
                OpenQingGongYan();
            }
            if (IfNeedCloseQingGongYan())
            {
                CloseQingGongYan();
            }
        }

        /// <summary>
        /// 开启庆功宴
        /// </summary>
        private void OpenQingGongYan()
        {
            QingGongYanOpenFlag = true;

            QingGongYanInfo InfoData = GetInfoData();
            if (null == InfoData)
            {
                return;
            }

            GameMap gameMap = GameManager.MapMgr.DictMaps[InfoData.MapCode];
            // 策划要求x, y改成直接传值
            NPC npc = NPCGeneralManager.GetNPCFromConfig(InfoData.MapCode, InfoData.NpcID, InfoData.X, InfoData.Y, InfoData.Direction);
            if (null != npc)
            {
                if (NPCGeneralManager.AddNpcToMap(npc))
                {
                    QingGongYanNpc = npc;
                    //播放用户行为消息
                    string guildName = GameManager.GameConfigMgr.GetGameConfigItemStr(GameConfigNames.QGYGuildName, "");
                    string broadCastMsg = StringUtil.substitute(Global.GetLang("为庆祝{0}战盟在罗兰城战中取得胜利，慷慨的罗兰城主正在勇者大陆摆开宴席，恭候各位光临！"), guildName);
                    Global.BroadcastRoleActionMsg(null, RoleActionsMsgTypes.Bulletin, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.SysHintAndChatBox);
                }
                else
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("OpenQingGongYan, AddNpcToMap Faild !InfoData.MapCode={0}, InfoData.NpcID={1}", InfoData.MapCode, InfoData.NpcID));
                }
            }
        }

        /// <summary>
        /// 取得当前庆功宴的配置
        /// </summary>
        private QingGongYanInfo GetInfoData()
        {
            // 没有档次
            int DBGrade = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.QGYGrade, 0);
            if (DBGrade <= 0)
            {
                return null;
            }

            // 档次没有配置
            return GetQingGongYanConfig(DBGrade);
        }

        /// <summary>
        /// 关闭庆功宴
        /// </summary>
        private void CloseQingGongYan()
        {
            // 销毁怪物
            if (null != QingGongYanNpc)
            {
                NPCGeneralManager.RemoveMapNpc(QingGongYanNpc.MapCode, QingGongYanNpc.NpcID);
                QingGongYanNpc = null;
            }
            QingGongYanOpenFlag = false;

            // log it...

            // 档次没有配置
            QingGongYanInfo InfoData = GetInfoData();
            if (null == InfoData)
            {
                return;
            }

            if (InfoData.ZuanShiCoe <= 0)
            {
                return;
            }

            int JoinMoney = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.QGYJoinMoney, 0);
            int ZuanShiAward = JoinMoney / InfoData.ZuanShiCoe;
            int DBRoleID = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.QGYRoleID, 0);
            if (DBRoleID <= 0)
            {
                return;
            }

            //string sContent = "您在2015年02月02日 20:00举办的宴会已成功结束，共获得收益200钻石。";
            string sContent = string.Format(Global.GetLang("您在{0:0000}年{1:00}月{2:00}日 {3:00}点{4:00}分举办的宴会已成功结束，共获得收益{5}钻石。"), TimeUtil.NowDateTime().Year, TimeUtil.NowDateTime().Month, TimeUtil.NowDateTime().Day, DateTime.Parse(InfoData.BeginTime).Hour, DateTime.Parse(InfoData.BeginTime).Minute, ZuanShiAward);

            Global.UseMailGivePlayerAward3(DBRoleID, null, Global.GetLang("庆功宴"), sContent, ZuanShiAward);

            // 清空记录
            Global.UpdateDBGameConfigg(GameConfigNames.QGYRoleID, "");
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYRoleID, "");
            Global.UpdateDBGameConfigg(GameConfigNames.QGYGuildName, "");
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYGuildName, "");
            Global.UpdateDBGameConfigg(GameConfigNames.QGYStartDay, "");
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYStartDay, "");
            Global.UpdateDBGameConfigg(GameConfigNames.QGYGrade, "");
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYGrade, "");
            Global.UpdateDBGameConfigg(GameConfigNames.QGYJoinCount, "");
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYJoinCount, "");
            Global.UpdateDBGameConfigg(GameConfigNames.QGYJoinMoney, "");
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYJoinMoney, "");
            Global.UpdateDBGameConfigg(GameConfigNames.QGYJuBanMoney, "");
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYJuBanMoney, "");

            //播放用户行为消息
            string broadCastMsg = StringUtil.substitute(Global.GetLang("本次罗兰宴会已圆满结束，愿大家满载而归！"));
            Global.BroadcastRoleActionMsg(null, RoleActionsMsgTypes.Bulletin, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.SysHintAndChatBox);
        }

        /// <summary>
        /// 参加庆功宴
        /// </summary>
        public QingGongYanResult JoinQingGongYan(GameClient client)
        {
            if (null == QingGongYanNpc)
            {
                return QingGongYanResult.OutTime;
            }

            QingGongYanInfo InfoData = GetInfoData();
            if (null == InfoData)
            {
                return QingGongYanResult.OutTime;
            }

            int JoinCount = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.QGYJoinCount, 0);
            if (JoinCount > 0)
            {
                if (JoinCount >= InfoData.TotalNum)
                {
                    return QingGongYanResult.TotalNotEnough;
                }
            }

            // 检查参加所需金币是否足够
            if (InfoData.JoinBindJinBi > 0)
            {
                if (InfoData.JoinBindJinBi > Global.GetTotalBindTongQianAndTongQianVal(client))
                {
                    return QingGongYanResult.MoneyNotEnough;
                }
            }

            String QingGongYanJoinFlag = Global.GetRoleParamByName(client, RoleParamName.QingGongYanJoinFlag);
            int currDay = Global.GetOffsetDay(TimeUtil.NowDateTime());
            int lastJoinDay = 0;
            int joinCount = 0;
            // day:count
            if (null != QingGongYanJoinFlag)
            {
                string[] fields = QingGongYanJoinFlag.Split(',');
                if (2 == fields.Length)
                {
                    lastJoinDay = Convert.ToInt32(fields[0]);
                    joinCount = Convert.ToInt32(fields[1]);
                }
            }

            if (currDay != lastJoinDay)
            {
                joinCount = 0;
            }


            if (InfoData.SingleNum > 0)
            {
                if (joinCount >= InfoData.SingleNum)
                {
                    return QingGongYanResult.CountNotEnough;
                }
            }

            // 扣除参加所需金币
            if (InfoData.JoinBindJinBi > 0)
            {
                if (!Global.SubBindTongQianAndTongQian(client, InfoData.JoinBindJinBi, "参加庆功宴"))
                {
                    return QingGongYanResult.MoneyNotEnough;
                }
            }

            // 玩家计数
            string roleParam = currDay.ToString() + "," + (joinCount + 1).ToString();
            Global.UpdateRoleParamByName(client, RoleParamName.QingGongYanJoinFlag, roleParam, true);

            // 参加全局计数叠加
            // 由于这里是异步操作，数量可能会少
            Global.UpdateDBGameConfigg(GameConfigNames.QGYJoinCount, (JoinCount + 1).ToString());
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYJoinCount, (JoinCount + 1).ToString());
            // 记录缴纳的
            int JoinMoney = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.QGYJoinMoney, 0);
            Global.UpdateDBGameConfigg(GameConfigNames.QGYJoinMoney, (JoinMoney + InfoData.JoinBindJinBi).ToString());
            GameManager.GameConfigMgr.SetGameConfigItem(GameConfigNames.QGYJoinMoney, (JoinMoney + InfoData.JoinBindJinBi).ToString());

            // 发奖
            if (InfoData.ExpAward > 0)
            {
                GameManager.ClientMgr.ProcessRoleExperience(client, InfoData.ExpAward);
            }

            if (InfoData.XingHunAward > 0)
            {
                GameManager.ClientMgr.ModifyStarSoulValue(client, InfoData.XingHunAward, "庆功宴", true, true);
            }

            if (InfoData.ZhanGongAward > 0)
            {
                int nZhanGong = InfoData.ZhanGongAward;
                if (GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ref nZhanGong, AddBangGongTypes.BG_QGY))
                {
                    //[bing] 记录战功增加流向log
                    if (0 != nZhanGong)
                        GameManager.logDBCmdMgr.AddDBLogInfo(-1, "战功", "罗兰宴会领取", "系统", client.ClientData.RoleName, "增加", nZhanGong, client.ClientData.ZoneID, client.strUserID, client.ClientData.BangGong, client.ServerId);
                }
                GameManager.SystemServerEvents.AddEvent(string.Format("角色获取帮贡, roleID={0}({1}), BangGong={2}, newBangGong={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.BangGong, nZhanGong), EventLevels.Record);
            }

            // log it
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "参加庆功宴", "", "", client.ClientData.RoleName, "", 1, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId);
            EventLogManager.AddRoleEvent(client, OpTypes.Join, OpTags.QingGongYan, LogRecordType.OffsetDayId, currDay);

            return QingGongYanResult.Success;
        }

        #endregion

        #region 协议处理

        /// <summary>
        /// 申请举办
        /// </summary>
        public TCPProcessCmdResults ProcessHoldQingGongYanCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int Index = Convert.ToInt32(fields[1]);
                int OnlyCheck = Convert.ToInt32(fields[2]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                QingGongYanResult result = HoldQingGongYan(client, Index, OnlyCheck);
                string strcmd = "";

                if (result != QingGongYanResult.Success)
                {
                    strcmd = string.Format("{0}:{1}:{2}", (int)result, roleID, OnlyCheck);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                strcmd = string.Format("{0}:{1}:{2}", (int)QingGongYanResult.Success, roleID, OnlyCheck);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessHoldQingGongYanCMD", false);
            }


            return TCPProcessCmdResults.RESULT_FAILED;
        }
        /// <summary>
        /// 申请信息
        /// </summary>
        public TCPProcessCmdResults ProcessQueryQingGongYanCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int DBGrade = Convert.ToInt32(GameManager.GameConfigMgr.GetGameConfigItemStr("qinggongyan_grade", "0"));
                int TotalCount = Convert.ToInt32(GameManager.GameConfigMgr.GetGameConfigItemStr("qinggongyan_joincount", "0"));
                int JoinMoney = Convert.ToInt32(GameManager.GameConfigMgr.GetGameConfigItemStr("qinggongyan_joinmoney", "0"));

                String QingGongYanJoinFlag = Global.GetRoleParamByName(client, RoleParamName.QingGongYanJoinFlag);
                int currDay = Global.GetOffsetDay(TimeUtil.NowDateTime());
                int lastJoinDay = 0;
                int joinCount = 0;
                // day:count
                if (null != QingGongYanJoinFlag)
                {
                    string[] strTemp = QingGongYanJoinFlag.Split(',');
                    if (2 == strTemp.Length)
                    {
                        lastJoinDay = Convert.ToInt32(strTemp[0]);
                        joinCount = Convert.ToInt32(strTemp[1]);
                    }
                }

                if (currDay != lastJoinDay)
                {
                    joinCount = 0;
                }

                string strcmd = "";

                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, DBGrade, joinCount, TotalCount, JoinMoney);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessHoldQingGongYanCMD", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// 申请参加
        /// </summary>
        public TCPProcessCmdResults ProcessJoinQingGongYanCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                QingGongYanResult result = JoinQingGongYan(client);
                string strcmd = "";

                if (result != QingGongYanResult.Success)
                {
                    strcmd = string.Format("{0}:{1}", (int)result, roleID);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                strcmd = string.Format("{0}:{1}", (int)QingGongYanResult.Success, roleID);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessJoinQingGongYanCMD", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// 申请当前是否庆功宴已经开启了
        /// </summary>
        public TCPProcessCmdResults ProcessCMD_SPR_QueryQingGongYanOpenCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                int result = (QingGongYanNpc == null) ? 0 : 1;

                string strcmd = string.Format("{0}", result);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessJoinQingGongYanCMD", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        #endregion

    }
}
