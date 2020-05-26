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
using GameServer.Core.GameEvent;
using System.Xml.Linq;
using GameServer.Core.GameEvent.EventOjectImpl;
using Tmsk.Contract;

namespace GameServer.Logic
{
    /// <summary>
    /// 王城战管理
    /// </summary>
    public class LuoLanChengZhanManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx
    {
        #region 标准接口

        public const SceneUIClasses ManagerType = SceneUIClasses.LuoLanChengZhan;

        private static LuoLanChengZhanManager instance = new LuoLanChengZhanManager();

        public static LuoLanChengZhanManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public LuoLanChengZhanData RuntimeData = new LuoLanChengZhanData();

        /// <summary>
        /// 定时的等级经验奖励管理器
        /// </summary>
        public LevelAwardsMgr _LevelAwardsMgr = new LevelAwardsMgr();

        /// <summary>
        /// 地图事件管理器(罗兰峡谷地图)
        /// </summary>
        private MapEventMgr _MapEventMgr = new MapEventMgr();

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
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_CHENGZHAN_JINGJIA, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_GET_CHENGZHAN_DAILY_AWARD, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LUOLANCHENGZHAN, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_GET_LUOLANCHENGZHU_INFO, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_GET_LUOLANCHENGZHAN_REQUEST_INFO_LIST, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SERVERUPDATE_ZHANMENGZIJIN, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_LUOLANKING_GET_ROLE_LOOKS, 2, 2, getInstance());

            //向事件源注册监听器
            //GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerInitGame, getInstance());
            //GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerLogout, getInstance());
            //GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreInstallJunQi, SceneUIClasses.LuoLanChengZhan, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreBangHuiAddMember, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreBangHuiRemoveMember, (int)SceneUIClasses.All, getInstance());

            return true;
        }

        public bool showdown()
        {
            //向事件源删除监听器
            //GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerInitGame, getInstance());
            //GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerLogout, getInstance());
            //GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreInstallJunQi, SceneUIClasses.LuoLanChengZhan, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreBangHuiAddMember, (int)SceneUIClasses.All, getInstance());
            GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreBangHuiRemoveMember, (int)SceneUIClasses.All, getInstance());

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
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_CHENGZHAN_JINGJIA:
                    return ProcessChengZhanJingJiaCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_GET_CHENGZHAN_DAILY_AWARD:
                    return ProcessGetChengZhanDailyAwardsCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LUOLANCHENGZHAN:
                    return ProcessLuoLanChengZhanCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_GET_LUOLANCHENGZHU_INFO:
                    return ProcessGetLuoLanChengZhuInfoCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_GET_LUOLANCHENGZHAN_REQUEST_INFO_LIST:
                    return ProcessLuoLanChengZhanRequestInfoListCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SERVERUPDATE_ZHANMENGZIJIN:
                    return ProcessQueryZhanMengZiJinCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_LUOLANKING_GET_ROLE_LOOKS:
                    return ProcessGetLuoLanKingLooks(client, nID, bytes, cmdParams);
            }
            return true;
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventObject"></param>
        public void processEvent(EventObject eventObject)
        {
            int nID = eventObject.getEventType();
            switch (nID)
            {
                case (int)EventTypes.PlayerInitGame:
                    break;
                case (int)EventTypes.PlayerLogout:
                    break;
            }
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventObject"></param>
        public void processEvent(EventObjectEx eventObject)
        {
            int eventType = eventObject.EventType;
            switch (eventType)
            {
                case (int)EventTypes.PreInstallJunQi:
                    {
                        PreInstallJunQiEventObject e = eventObject as PreInstallJunQiEventObject;
                        if (null != e)
                        {
                            OnPreInstallJunQi(e.Player, e.NPCID);
                            eventObject.Handled = true;
                        }
                    }
                    break;
                case (int)EventTypes.PreBangHuiAddMember:
                    {
                        PreBangHuiAddMemberEventObject e = eventObject as PreBangHuiAddMemberEventObject;
                        if (null != e)
                        {
                            eventObject.Handled = OnPreBangHuiAddMember(e);
                        }
                    }
                    break;
                case (int)EventTypes.PreBangHuiRemoveMember:
                    {
                        PreBangHuiRemoveMemberEventObject e = eventObject as PreBangHuiRemoveMemberEventObject;
                        if (null != e)
                        {
                            eventObject.Handled = OnPreBangHuiRemoveMember(e);
                        }
                    }
                    break;
            }
        }

        #endregion 标准接口

        #region 初始化配置

        /// <summary>
        /// 初始化配置
        /// </summary>
        public bool InitConfig()
        {
            XElement xml = null;
            string fileName = "";

            lock (RuntimeData.Mutex)
            {
                try
                {
                    RuntimeData.SiegeWarfareEveryDayAwardsDict.Clear();

                    fileName = "Config/SiegeWarfareEveryDayAward.xml";
                    string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        SiegeWarfareEveryDayAwardsItem item = new SiegeWarfareEveryDayAwardsItem();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.ZhiWu = (int)Global.GetSafeAttributeLong(node, "Status");
                        item.DayZhanGong = (int)Global.GetSafeAttributeLong(node, "DayZhanGong");
                        item.DayExp = Global.GetSafeAttributeLong(node, "DayExp");
                        item.DayGoods.AddNoRepeat(Global.GetSafeAttributeStr(node, "DayGoods"));

                        if (!RuntimeData.SiegeWarfareEveryDayAwardsDict.ContainsKey(item.ZhiWu))
                        {
                            RuntimeData.SiegeWarfareEveryDayAwardsDict.Add(item.ZhiWu, item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    RuntimeData.MapBirthPointListDict.Clear();

                    fileName = "Config/SiegeWarfareBirthPoint.xml";
                    string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        MapBirthPoint item = new MapBirthPoint();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.Type = (int)Global.GetSafeAttributeLong(node, "Type");
                        item.MapCode = (int)Global.GetSafeAttributeLong(node, "MapCode");
                        item.BirthPosX = (int)Global.GetSafeAttributeLong(node, "BirthPosX");
                        item.BirthPosY = (int)Global.GetSafeAttributeLong(node, "BirthPosY");
                        item.BirthRangeX = (int)Global.GetSafeAttributeLong(node, "BirthRangeX");
                        item.BirthRangeY = (int)Global.GetSafeAttributeLong(node, "BirthRangeY");

                        List<MapBirthPoint> list;
                        if (!RuntimeData.MapBirthPointListDict.TryGetValue(item.Type, out list))
                        {
                            list = new List<MapBirthPoint>();
                            RuntimeData.MapBirthPointListDict.Add(item.Type, list);
                        }
                        list.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    RuntimeData.NPCID2QiZhiConfigDict.Clear();
                    RuntimeData.QiZhiBuffOwnerDataList.Clear();
                    RuntimeData.QiZhiBuffDisableParamsDict.Clear();
                    RuntimeData.QiZhiBuffEnableParamsDict.Clear();

                    fileName = "Config/QiZuoConfig.xml";
                    string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        QiZhiConfig item = new QiZhiConfig();
                        item.NPCID = (int)Global.GetSafeAttributeLong(node, "NPCID");
                        item.BufferID = (int)Global.GetSafeAttributeLong(node, "BufferID");
                        item.PosX = (int)Global.GetSafeAttributeLong(node, "PosX");
                        item.PosY = (int)Global.GetSafeAttributeLong(node, "PosY");
                        List<int> useAuthority = Global.StringToIntList(Global.GetSafeAttributeStr(node, "UseAuthority"), ',');
                        foreach (var zhiwu in useAuthority)
                        {
                            item.UseAuthority.Add(zhiwu);
                        }

                        RuntimeData.NPCID2QiZhiConfigDict[item.NPCID] = item;
                        RuntimeData.QiZhiBuffOwnerDataList.Add(new LuoLanChengZhanQiZhiBuffOwnerData() { NPCID = item.NPCID, OwnerBHName = "" });
                        RuntimeData.QiZhiBuffDisableParamsDict[item.BufferID] = new double[] { 0, item.BufferID };
                        RuntimeData.QiZhiBuffEnableParamsDict[item.BufferID] = new double[] { 0, item.BufferID };
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    RuntimeData.MapCode = 0;
                    RuntimeData.MapCode_LongTa = 0;

                    QiZhiConfig qiZhiConfig;
                    if (RuntimeData.NPCID2QiZhiConfigDict.TryGetValue(RuntimeData.SuperQiZhiNpcId, out qiZhiConfig))
                    {
                        RuntimeData.SuperQiZhiOwnerBirthPosX = qiZhiConfig.PosX;
                        RuntimeData.SuperQiZhiOwnerBirthPosY = qiZhiConfig.PosY;
                    }

                    fileName = "Config/SiegeWarfare.xml";
                    string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        RuntimeData.MapCode = (int)Global.GetSafeAttributeLong(node, "MapCode1");
                        RuntimeData.MapCode_LongTa = (int)Global.GetSafeAttributeLong(node, "MapCode2");
                        RuntimeData.GongNengOpenDaysFromKaiFu = (int)Global.GetSafeAttributeLong(node, "KaiFuDay");
                        RuntimeData.ApplyZhangMengZiJin = (int)Global.GetSafeAttributeLong(node, "ApplyZhangMengZiJin");
                        RuntimeData.BidZhangMengZiJin = (int)Global.GetSafeAttributeLong(node, "BidZhangMengZiJin");
                        RuntimeData.MaxZhanMengNum = (int)Global.GetSafeAttributeLong(node, "MaxZhanMengNum");

                        RuntimeData.WeekPoints = Global.String2IntArray(Global.GetSafeAttributeStr(node, "WeekPoints"), '|');
                        RuntimeData.TimePoints = DateTime.Parse(Global.GetSafeAttributeStr(node, "TimePoints"));
                        RuntimeData.EnrollTime = Global.GetSafeAttributeLong(node, "EnrollTime");

                        RuntimeData.MinZhuanSheng = (int)Global.GetSafeAttributeLong(node, "MinZhuanSheng");
                        RuntimeData.MinLevel = (int)Global.GetSafeAttributeLong(node, "MinLevel");
                        RuntimeData.MinRequestNum = (int)Global.GetSafeAttributeLong(node, "MinRequestNum");
                        RuntimeData.MaxEnterNum = (int)Global.GetSafeAttributeLong(node, "MaxEnterNum");

                        RuntimeData.WaitingEnterSecs = (int)Global.GetSafeAttributeLong(node, "WaitingEnterSecs");
                        RuntimeData.PrepareSecs = (int)Global.GetSafeAttributeLong(node, "PrepareSecs");
                        RuntimeData.FightingSecs = (int)Global.GetSafeAttributeLong(node, "FightingSecs");
                        RuntimeData.ClearRolesSecs = (int)Global.GetSafeAttributeLong(node, "ClearRolesSecs");

                        RuntimeData.ExpAward = Global.GetSafeAttributeLong(node, "ExpAward");
                        RuntimeData.ZhanGongAward = (int)Global.GetSafeAttributeLong(node, "ZhanGongAward");
                        RuntimeData.ZiJin = (int)Global.GetSafeAttributeLong(node, "ZiJin");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    fileName = "Config/SiegeWarfareExp.xml";
                    _LevelAwardsMgr.LoadFromXMlFile(fileName, "", "ID");

                    ParseWeekDaysTimes();

                    InitLuoLanChengZhuInfo();
                }
                catch (System.Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 从DB加载领主和竞价请求信息
        /// </summary>
        public void LoadDataFromDB()
        {
            int luoLanChengZhuRoleID = 0;
            lock (RuntimeData.Mutex)
            {
                BangHuiLingDiItemData lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.LuoLanChengZhan);
                if (null != lingDiItem)
                {
                    WangZuBHid = lingDiItem.BHID;
                    WangZuBHName = lingDiItem.BHName = UpdateWangZuBHNameFromDBServer(lingDiItem.BHID);
                    if (lingDiItem.WarRequest != RuntimeData.WarRequestStr)
                    {
                        RuntimeData.WarRequstDict = GetWarRequstMap(lingDiItem.WarRequest);
                        RuntimeData.WarRequestStr = lingDiItem.WarRequest;
                    }

                    BangHuiDetailData bangHuiDetailData = GetBangHuiDetailDataAuto(lingDiItem.BHID);
                    if (null != bangHuiDetailData)
                    {
                        //更新帮主的信息
                        luoLanChengZhuRoleID = bangHuiDetailData.BZRoleID;
                    }
                }
                else
                {
                    WangZuBHid = 0;
                    WangZuBHName = "";
                    RuntimeData.WarRequstDict = new Dictionary<int, LuoLanChengZhanRequestInfo>();
                    RuntimeData.WarRequestStr = null;
                }

                RuntimeData.LongTaOwnerData.OwnerBHid = WangZuBHid;
                RuntimeData.LongTaOwnerData.OwnerBHName = WangZuBHName;
                RuntimeData.LuoLanChengZhuBHID = WangZuBHid;
                RuntimeData.LuoLanChengZhuBHName = WangZuBHName;
                ResetBHID2SiteDict();
            }

            // 设置罗兰城主的雕像
            ReShowLuolanKing(luoLanChengZhuRoleID);
        }

        private LuoLanChengZhuInfo GetLuoLanChengZhuInfo(GameClient client)
        {
            int roleID = 0;
            if (null != client)
            {
                roleID = client.ClientData.RoleID;
            }
            LuoLanChengZhuInfo luoLanChengZhuInfo = new LuoLanChengZhuInfo();

            BangHuiLingDiItemData lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.LuoLanChengZhan);
            if (null == lingDiItem || lingDiItem.BHID <= 0)
            {
                return luoLanChengZhuInfo;
            }

            BangHuiDetailData bangHuiDetailData = GetBangHuiDetailDataAuto(lingDiItem.BHID, roleID);
            if (null != bangHuiDetailData)
            {
                luoLanChengZhuInfo.BHID = bangHuiDetailData.BHID;
                luoLanChengZhuInfo.BHName = bangHuiDetailData.BHName;
                luoLanChengZhuInfo.ZoneID = bangHuiDetailData.ZoneID;
                if (null != bangHuiDetailData.MgrItemList)
                {
                    foreach (var item in bangHuiDetailData.MgrItemList)
                    {
                        //现在只发首领的
                        if (item.BHZhiwu == (int)ZhanMengZhiWus.ShouLing)
                        {
                            RoleDataEx rd = KingRoleData;
                            if (rd != null && rd.RoleID == item.RoleID)
                            {
                                RoleData4Selector sel = Global.RoleDataEx2RoleData4Selector(rd);
                                luoLanChengZhuInfo.RoleInfoList.Add(sel);
                                luoLanChengZhuInfo.ZhiWuList.Add(item.BHZhiwu);
                            }
//                             RoleData4Selector roleInfo = Global.sendToDB<RoleData4Selector, string>((int)TCPGameServerCmds.CMD_SPR_GETROLEUSINGGOODSDATALIST, string.Format("{0}", item.RoleID), client.ServerId);
//                             if (null != roleInfo)
//                             {
//                                 luoLanChengZhuInfo.RoleInfoList.Add(roleInfo);
//                                 luoLanChengZhuInfo.ZhiWuList.Add(item.BHZhiwu);
//                             }
                        }
                    }
                }
            }

            return luoLanChengZhuInfo;
        }

        public BangHuiDetailData GetBangHuiDetailDataAuto(int bhid, int roleID = -1)
        {
            BangHuiDetailData bangHuiDetailData = Global.GetBangHuiDetailData(roleID, bhid);
            if (null != bangHuiDetailData)
            {
                if (roleID <= 0 && bangHuiDetailData.BZRoleID > 0)
                {
                    bangHuiDetailData = Global.GetBangHuiDetailData(bangHuiDetailData.BZRoleID, bhid);
                }
            }

            return bangHuiDetailData;
        }

        /// <summary>
        /// 解析王城战的日期和时间
        /// </summary>
        public void ParseWeekDaysTimes()
        {
            lock (RuntimeData.Mutex)
            {
                if (null != RuntimeData.WeekPoints && RuntimeData.WeekPoints.Length > 0)
                {
                    WangChengZhanWeekDaysByConfig = true;
                }

                string wangChengZhanFightingDayTimes_str = string.Format("{0}-{1}", RuntimeData.TimePoints.ToString("HH:mm"), RuntimeData.TimePoints.AddSeconds(RuntimeData.FightingSecs).ToString("HH:mm"));
                WangChengZhanFightingDayTimes = Global.ParseDateTimeRangeStr(wangChengZhanFightingDayTimes_str);
                RuntimeData.NoRequestTimeEnd = RuntimeData.TimePoints.AddSeconds(RuntimeData.FightingSecs).TimeOfDay;
                RuntimeData.NoRequestTimeStart = RuntimeData.TimePoints.AddSeconds(-RuntimeData.EnrollTime).TimeOfDay;

                MaxTakingHuangGongSecs = (int)GameManager.systemParamsList.GetParamValueIntByName("LuoLanHoldTime");
                MaxTakingHuangGongSecs *= 1000;
            }
        }

        /// <summary>
        /// 初始化罗兰城主信息
        /// </summary>
        private void InitLuoLanChengZhuInfo()
        {
            //加载领地信息
            LoadDataFromDB();

            //每一次重新加载都更新一次，保证最新的修改都得到应用
            HuodongCachingMgr.UpdateHeFuWCKingBHID(GetWangZuBHid());

            //通知地图数据变更信息
            NotifyAllWangChengMapInfoData();

            //罗兰城主帮会变更
            FashionManager.getInstance().UpdateLuoLanChengZhuFasion(WangZuBHid);

            BangHuiLingDiItemData lingdiItemData = JunQiManager.GetItemByLingDiID((int)LingDiIDs.LuoLanChengZhan);
            if (null != lingdiItemData)
            {

            }
        }

        /// <summary>
        /// 罗兰城主帮会改变
        /// </summary>
        public void BangHuiLingDiItemsDictFromDBServer()
        {
            if (!IsInWangChengFightingTime(TimeUtil.NowDateTime()))
            {
                InitLuoLanChengZhuInfo();
            }
        }

        #endregion 初始化配置

        #region 内存中的王族帮会ID

        /// <summary>
        /// 是否正在等待王城的归属
        /// </summary>
        private bool WaitingHuangChengResult = false;

        /// <summary>
        /// 王城战期间帮会独占皇宫的保持时间
        /// </summary>
        private long BangHuiTakeHuangGongTicks = TimeUtil.NOW();

        /// <summary>
        /// 王族的所在帮会名称
        /// </summary>
        private string WangZuBHName = "";

        /// <summary>
        /// 王族所在的帮会ID
        /// </summary>
        private int WangZuBHid = -1;

        /// <summary>
        /// 程序启动时从DBServer更新王族的ID
        /// </summary>
        public string UpdateWangZuBHNameFromDBServer(int bhid)
        {
            BangHuiMiniData bangHuiMiniData = Global.GetBangHuiMiniData(bhid);
            if (null == bangHuiMiniData)
            {
                return Global.GetLang("无");
            }

            return bangHuiMiniData.BHName;
        }

        /// <summary>
        /// 获取王族的帮会ID
        /// </summary>
        public int GetWangZuBHid()
        {
            return WangZuBHid;
        }

        /// <summary>
        /// 获取王族的帮会名称
        /// </summary>
        public string GetWangZuBHName()
        {
            return WangZuBHName;
        }

        #endregion 内存中的王族帮会ID

        #region 活动状态

        /// <summary>
        /// 申请王城战的锁
        /// </summary>
        public object ApplyWangChengWarMutex = new object();

        /// <summary>
        /// 占领皇宫决定胜负的最长时间
        /// </summary>
        public int MaxTakingHuangGongSecs = (5 * 1000);

        /// <summary>
        /// 从配置文件中loading是否是有效数据
        /// </summary>
        public bool WangChengZhanWeekDaysByConfig = false;

        /// <summary>
        /// 王城战的举行的时间段
        /// </summary>
        public DateTimeRange[] WangChengZhanFightingDayTimes = null;

        #endregion 活动状态

        #region 处理王城战的胜负结果

        /// <summary>
        /// 判断周日期是否相符
        /// </summary>
        /// <param name="weekDayID"></param>
        /// <returns></returns>
        private bool IsDayOfWeek(int weekDayID)
        {
            lock (RuntimeData.Mutex)
            {
                if (null == RuntimeData.WeekPoints) return false;
                for (int i = 0; i < RuntimeData.WeekPoints.Length; i++)
                {
                    if (RuntimeData.WeekPoints[i] == weekDayID)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 是否在王城战时间段内
        /// </summary>
        /// <returns></returns>
        public bool IsInWangChengFightingTime(DateTime now)
        {
            lock (RuntimeData.Mutex)
            {
                int weekDayID = (int)now.DayOfWeek;
                if (!IsDayOfWeek(weekDayID))
                {
                    return false;
                }

                int endMinute = 0;
                return Global.JugeDateTimeInTimeRange(now, WangChengZhanFightingDayTimes, out endMinute, false);
            }
        }

        public void GMStartHuoDongNow()
        {
            try
            {
                lock (RuntimeData.Mutex)
                {
                    RuntimeData.WeekPoints[0] = (int)TimeUtil.NowDateTime().DayOfWeek;
                    RuntimeData.TimePoints = TimeUtil.NowDateTime();
                    ParseWeekDaysTimes();
                }
            }
            catch (System.Exception ex)
            {

            }
        }

        public void GMSetLuoLanChengZhu(int newBHid)
        {
            try
            {
                lock (RuntimeData.Mutex)
                {
                    LastTheOnlyOneBangHui = newBHid;

                    RuntimeData.LongTaOwnerData.OwnerBHid = newBHid;
                    RuntimeData.LongTaOwnerData.OwnerBHName = UpdateWangZuBHNameFromDBServer(newBHid); //加载帮会名称等细节信息
                    WangZuBHid = RuntimeData.LongTaOwnerData.OwnerBHid;
                    WangZuBHName = RuntimeData.LongTaOwnerData.OwnerBHName;

                    //处理王城的归属
                    HandleHuangChengResultEx(true);

                    //通知地图数据变更信息
                    NotifyAllWangChengMapInfoData();
                }
            }
            catch (System.Exception ex)
            {

            }
        }

        /// <summary>
        /// 王城战是否已经结束
        /// </summary>
        /// <returns></returns>
        public bool IsWangChengZhanOver()
        {
            return !WaitingHuangChengResult;
        }

        /// <summary>
        /// 王城战的状态类型
        /// </summary>
        public WangChengZhanStates WangChengZhanState = WangChengZhanStates.None;

        /// <summary>
        /// 判断是否在战斗时间 战斗时间主要是不让领取每日奖励
        /// </summary>
        /// <returns></returns>
        public bool IsInBattling()
        {
            if (WangChengZhanStates.None != WangChengZhanState)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// 全服播报罗兰城战竞价结果
        /// </summary>
        private void NotifyAllLuoLanChengZhanJingJiaResult()
        {
            lock (RuntimeData.Mutex)
            {
                bool canRequest = CanRequest();
                if (RuntimeData.CanRequestState != canRequest)
                {
                    RuntimeData.CanRequestState = canRequest;

                    //如果是从可竞价变化到不可竞价,则需要广播进攻方列表
                    if (!canRequest)
                    {
                        string broadCastMsg = Global.GetLang("本次罗兰城战竞标已结束，活动将在35分钟后开启，获得进攻资格的战盟为");
                        List<LuoLanChengZhanRequestInfoEx> list = GetWarRequestInfoList();
                        list = list.FindAll((x) => { return x.BHID > 0; });
                        for (int i = 0; i < list.Count; i++)
                        {
                            broadCastMsg += string.Format(Global.GetLang("【{0}】"), GetBHName(list[i].BHID));
                            if (i < list.Count - 1)
                            {
                                broadCastMsg += Global.GetLang("、");
                            }
                        }

                        if (list.Count > 0)
                        {
                            Global.BroadcastRoleActionMsg(null, RoleActionsMsgTypes.Bulletin, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理王城战的战斗结果
        /// </summary>
        public void ProcessWangChengZhanResult()
        {
            try
            {
                if (Global.GetBangHuiFightingLineID() != GameManager.ServerLineID)
                {
                    return;
                }

                //进行weekday更新，这样新旧代码一致
                Global.UpdateLuoLanChengZhanWeekDays();

                DateTime now = TimeUtil.NowDateTime();
                if (WangChengZhanStates.None == WangChengZhanState) //非战斗状态
                {
                    if (IsInWangChengFightingTime(now))
                    {
                        _MapEventMgr.ClearAllMapEvents(); //重置地图事件队列为空

                        WangChengZhanState = WangChengZhanStates.Fighting;
                        BangHuiTakeHuangGongTicks = now.Ticks;
                        RuntimeData.FightEndTime = now.AddSeconds(RuntimeData.FightingSecs);
                        WaitingHuangChengResult = true;

                        RuntimeData.SuperQiZhiOwnerBhid = 0;

                        //通知地图数据变更信息
                        NotifyAllWangChengMapInfoData();

                        //王城战开始通知
                        Global.BroadcastHuangChengBattleStart();
                    }
                    else
                    {
                        ClearMapClients();
                        NotifyAllLuoLanChengZhanJingJiaResult();
                    }
                }
                else //战斗状态
                {
                    UpdateQiZhiBuffParams(now);

                    if (IsInWangChengFightingTime(now)) //还在战斗期间
                    {
                        //这儿其实是在模拟拥有舍利之源的操作，如此就走就代码的逻辑，不用修改太多代码
                        bool ret = TryGenerateNewHuangChengBangHui();

                        //生成了新的占有王城的帮会
                        if (ret)
                        {
                            //处理王城的归属
                            HandleHuangChengResultEx(false);
                        }
                        else
                        {
                            /// 定时给在场的玩家增加经验
                            ProcessTimeAddRoleExp();
                        }
                    }
                    else
                    {
                        ClearMapClients(true);

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

                        //删除所有军旗
                        JunQiManager.ProcessDelAllJunQiByMapCode(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, RuntimeData.MapCode);

                        //通知地图数据变更信息
                        NotifyAllWangChengMapInfoData();

                        //发放奖励
                        GiveLuoLanChengZhanAwards();

                        //清空罗兰城战申请信息
                        ResetRequestInfo();
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// 更新旗帜Buff的时间参数信息
        /// </summary>
        /// <param name="now"></param>
        private void UpdateQiZhiBuffParams(DateTime now)
        {
            lock (RuntimeData.Mutex)
            {
                foreach (var key in RuntimeData.QiZhiBuffEnableParamsDict.Keys)
                {
                    RuntimeData.QiZhiBuffEnableParamsDict[key][0] = (int)(RuntimeData.FightEndTime - now).TotalSeconds;
                }
            }
        }

        /// <summary>
        /// 发放活动奖励
        /// </summary>
        private void GiveLuoLanChengZhanAwards()
        {
            LuoLanChengZhanResultInfo resultInfoSuccess = new LuoLanChengZhanResultInfo();
            LuoLanChengZhanResultInfo resultInfoFaild = new LuoLanChengZhanResultInfo();
            resultInfoSuccess.BHID = resultInfoFaild.BHID = WangZuBHid;
            resultInfoSuccess.BHName = resultInfoFaild.BHName = WangZuBHName;
            resultInfoSuccess.ExpAward = RuntimeData.ExpAward;
            resultInfoSuccess.ZhanGongAward = RuntimeData.ZhanGongAward;
            resultInfoSuccess.ZhanMengZiJin = RuntimeData.ZiJin;
            resultInfoFaild.ExpAward = RuntimeData.ExpAward / 2;
            resultInfoFaild.ZhanGongAward = RuntimeData.ZhanGongAward / 2;
            resultInfoFaild.ZhanMengZiJin = RuntimeData.ZiJin / 2;

            GameClient client = GameManager.ClientMgr.GetFirstClient();
            lock (RuntimeData.Mutex)
            {
                foreach (var item in RuntimeData.WarRequstDict.Values)
                {
                    int bhid = item.BHID;
                    int zhanMengZiJin = 0;
                    if (item.BHID == WangZuBHid)
                    {
                        zhanMengZiJin = resultInfoSuccess.ZhanMengZiJin;
                    }
                    else
                    {
                        zhanMengZiJin = resultInfoFaild.ZhanMengZiJin;
                    }

                    BangHuiMiniData bangHuiMiniData = Global.GetBangHuiMiniData(item.BHID);
                    if (!GameManager.ClientMgr.AddBangHuiTongQian(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, bhid, zhanMengZiJin))
                    {
                        LogManager.WriteLog(LogTypes.SQL, string.Format("罗兰城战奖励战盟资金失败,bhid={0}, bidMoney={1}", bhid, zhanMengZiJin));
                    }
                }
            }

            List<Object> objsList = GameManager.ClientMgr.GetMapClients(RuntimeData.MapCode);
            if (null == objsList)
            {
                objsList = new List<Object>();
            }
            List<Object> objsList1 = GameManager.ClientMgr.GetMapClients(RuntimeData.MapCode_LongTa);

            objsList.AddRange(objsList1);
            if (null == objsList || objsList.Count <= 0) return;

            byte[] bytes0 = DataHelper.ObjectToBytes(resultInfoSuccess);
            TCPOutPacket tcpOutPacket0 = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, bytes0, 0, bytes0.Length, (int)TCPGameServerCmds.CMD_SPR_LUOLANCHENGZHAN_RESULT_INFO);
            byte[] bytes1 = DataHelper.ObjectToBytes(resultInfoFaild);
            TCPOutPacket tcpOutPacket1 = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, bytes1, 0, bytes1.Length, (int)TCPGameServerCmds.CMD_SPR_LUOLANCHENGZHAN_RESULT_INFO);
            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                if (c.ClientData.Faction == WangZuBHid)
                {
                    GameManager.ClientMgr.ProcessRoleExperience(c, resultInfoSuccess.ExpAward);
                    int bangGong = resultInfoSuccess.ZhanGongAward;
                    GameManager.ClientMgr.AddBangGong(c, ref bangGong, AddBangGongTypes.BG_ChengZhan);
                    c.sendCmd(tcpOutPacket0, false);
                }
                else
                {
                    GameManager.ClientMgr.ProcessRoleExperience(c, resultInfoFaild.ExpAward);
                    int bangGong = resultInfoFaild.ZhanGongAward;
                    GameManager.ClientMgr.AddBangGong(c, ref bangGong, AddBangGongTypes.BG_ChengZhan);
                    c.sendCmd(tcpOutPacket1, false);
                }
            }

            Global.PushBackTcpOutPacket(tcpOutPacket0);
            Global.PushBackTcpOutPacket(tcpOutPacket1);
        }

        /// <summary>
        /// 情况罗兰城战申请信息
        /// </summary>
        public void ResetRequestInfo()
        {
            lock (RuntimeData.Mutex)
            {
                RuntimeData.WarRequstDict = new Dictionary<int, LuoLanChengZhanRequestInfo>();
                RuntimeData.WarRequestStr = GeWarRequstString(RuntimeData.WarRequstDict);
                BangHuiLingDiItemData lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.LuoLanChengZhan);
                if (null != lingDiItem)
                {
                    lingDiItem.WarRequest = RuntimeData.WarRequestStr;
                    SetCityWarRequestToDBServer(lingDiItem.LingDiID, lingDiItem.WarRequest);
                }

                ResetBHID2SiteDict();

                RuntimeData.LongTaBHRoleCountList.Clear();
                for (int i = 0; i < RuntimeData.QiZhiBuffOwnerDataList.Count; i++)
                {
                    RuntimeData.QiZhiBuffOwnerDataList[i].OwnerBHID = 0;
                    RuntimeData.QiZhiBuffOwnerDataList[i].OwnerBHName = "";
                }
            }
        }

        /// <summary>
        /// 上一个唯一的帮会
        /// </summary>
        private int LastTheOnlyOneBangHui = -1;

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
        public bool TryGenerateNewHuangChengBangHui()
        {
            int newBHid = GetTheOnlyOneBangHui();

            NotifyLongTaRoleDataList();
            NotifyLongTaOwnerData();

            lock (RuntimeData.Mutex)
            {
                //剩下的帮会是王城帮会，没有产生新帮会
                if (newBHid <= 0 || newBHid == RuntimeData.LongTaOwnerData.OwnerBHid)
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
                        RuntimeData.LongTaOwnerData.OwnerBHid = LastTheOnlyOneBangHui;
                        RuntimeData.LongTaOwnerData.OwnerBHName = UpdateWangZuBHNameFromDBServer(newBHid); //加载帮会名称等细节信息
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 返回剩下的唯一帮会,-1表示有没有唯一帮会
        /// </summary>
        /// <returns></returns>
        public int GetTheOnlyOneBangHui()
        {
            //皇宫地图中活着的玩家列表
            List<GameClient> lsClients = GameManager.ClientMgr.GetMapAliveClientsEx(RuntimeData.MapCode_LongTa);

            int newBHid = -1;

            lock (RuntimeData.Mutex)
            {
                List<LuoLanChengZhanRoleCountData> list = new List<LuoLanChengZhanRoleCountData>(RuntimeData.MaxZhanMengNum);

                //根据活着的玩家列表，判断王族是否应该产生 保留 还说流产
                for (int n = 0; n < lsClients.Count; n++)
                {
                    GameClient client = lsClients[n];
                    int bhid = client.ClientData.Faction;
                    if (bhid > 0)
                    {
                        LuoLanChengZhanRoleCountData data = list.Find((x) => x.BHID == bhid);
                        if (null == data)
                        {
                            list.Add(new LuoLanChengZhanRoleCountData() { BHID = bhid, RoleCount = 1 });
                        }
                        else
                        {
                            data.RoleCount++;
                        }
                    }
                }

                RuntimeData.LongTaBHRoleCountList = list;

                if (list.Count == 1)
                {
                    newBHid = list[0].BHID;
                }
            }

            return newBHid;
        }

        /// <summary>
        /// 获取城战竞价战盟列表信息
        /// </summary>
        /// <returns></returns>
        private List<LuoLanChengZhanRequestInfoEx> GetWarRequestInfoList()
        {
            List<LuoLanChengZhanRequestInfoEx> list = new List<LuoLanChengZhanRequestInfoEx>();
            lock (RuntimeData.Mutex)
            {
                foreach (var item in RuntimeData.WarRequstDict.Values)
                {
                    list.Add(new LuoLanChengZhanRequestInfoEx()
                    {
                        Site = item.Site,
                        BHID = item.BHID,
                        BHName = GetBHName(item.BHID),
                        BidMoney = item.BidMoney,
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// 通知地图数据变更信息
        /// </summary>
        public void NotifyAllWangChengMapInfoData()
        {
            WangChengMapInfoData wangChengMapInfoData = FormatWangChengMapInfoData();

            //通知在线的所有人(不限制地图)领地信息数据通知
            GameManager.ClientMgr.NotifyAllWangChengMapInfoData(wangChengMapInfoData);
        }

        /// <summary>
        /// 通知地图数据变更信息
        /// </summary>
        public void NotifyLongTaRoleDataList()
        {
            byte[] bytes;
            lock (RuntimeData.Mutex)
            {
                bytes = DataHelper.ObjectToBytes(RuntimeData.LongTaBHRoleCountList);
            }

            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_LUOLANCHENGZHAN_LONGTA_ROLEINFO);

            //通知在线的所有人(不限制地图)领地信息数据通知
            GameManager.ClientMgr.BroadSpecialMapMessage(tcpOutPacket, RuntimeData.MapCode, -1, false);
            GameManager.ClientMgr.BroadSpecialMapMessage(tcpOutPacket, RuntimeData.MapCode_LongTa, -1);
        }

        /// <summary>
        /// 通知地图数据变更信息
        /// </summary>
        public void NotifyLongTaOwnerData()
        {
            byte[] bytes;
            lock (RuntimeData.Mutex)
            {
                bytes = DataHelper.ObjectToBytes(RuntimeData.LongTaOwnerData);
            }

            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_LUOLANCHENGZHAN_LONGTA_OWNERINFO);

            //通知在线的所有人(不限制地图)领地信息数据通知
            GameManager.ClientMgr.BroadSpecialMapMessage(tcpOutPacket, RuntimeData.MapCode, -1, false);
            GameManager.ClientMgr.BroadSpecialMapMessage(tcpOutPacket, RuntimeData.MapCode_LongTa, -1);
        }

        public void UpdateQiZhiBangHui(int npcExtentionID, int bhid, string bhName)
        {
            int oldBHID = 0;
            int bufferID = 0;
            lock (RuntimeData.Mutex)
            {
                for (int i = 0; i < RuntimeData.QiZhiBuffOwnerDataList.Count; i++)
                {
                    if (RuntimeData.QiZhiBuffOwnerDataList[i].NPCID == npcExtentionID)
                    {
                        oldBHID = RuntimeData.QiZhiBuffOwnerDataList[i].OwnerBHID;
                        RuntimeData.QiZhiBuffOwnerDataList[i].OwnerBHID = bhid;
                        RuntimeData.QiZhiBuffOwnerDataList[i].OwnerBHName = bhName;
                        break;
                    }
                }

                QiZhiConfig qiZhiConfig;
                if (RuntimeData.NPCID2QiZhiConfigDict.TryGetValue(npcExtentionID, out qiZhiConfig))
                {
                    bufferID = qiZhiConfig.BufferID;
                }
            }

            if (bhid == oldBHID)
            {
                return;
            }

            if (npcExtentionID == RuntimeData.SuperQiZhiNpcId)
            {
                RuntimeData.SuperQiZhiOwnerBhid = bhid;
            }

            try
            {
                List<Object> objsList = GameManager.ClientMgr.GetMapClients(RuntimeData.MapCode);
                List<Object> objsList1 = GameManager.ClientMgr.GetMapClients(RuntimeData.MapCode_LongTa);
                objsList.AddRange(objsList1);

                EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(bufferID);
                if (null != item)
                {
                    for (int i = 0; i < objsList.Count; i++)
                    {
                        GameClient c = objsList[i] as GameClient;
                        if (c == null) continue;

                        bool add = false;
                        if (c.ClientData.Faction == oldBHID)
                        {
                            add = false;
                        }
                        else if (c.ClientData.Faction == bhid)
                        {
                            add = true;
                        }

                        UpdateQiZhiBuff4GameClient(c, item, bufferID, add);
                    }
                }

                NotifyQiZhiBuffOwnerDataList();
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException("旗帜状态变化,设置旗帜Buff时发生异常:" + ex.ToString());
            }
        }

        /// <summary>
        /// 更新玩家的军旗Buff
        /// </summary>
        /// <param name="c"></param>
        /// <param name="item"></param>
        /// <param name="bufferID"></param>
        private void UpdateQiZhiBuff4GameClient(GameClient client, EquipPropItem item, int bufferID, bool add)
        {
            try
            {
                if (add)
                {
                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.BufferByGoodsProps, bufferID, item.ExtProps);
                    Global.UpdateBufferData(client, (BufferItemTypes)bufferID, RuntimeData.QiZhiBuffEnableParamsDict[bufferID], 1, true);
                }
                else
                {
                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.BufferByGoodsProps, bufferID, PropsCacheManager.ConstExtProps);//BufferItemTypes.MU_LUOLANCHENGZHAN_QIZHI1
                    Global.UpdateBufferData(client, (BufferItemTypes)bufferID, RuntimeData.QiZhiBuffDisableParamsDict[bufferID], 1, true);
                }

                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// 通知地图数据变更信息
        /// </summary>
        public void NotifyQiZhiBuffOwnerDataList()
        {
            byte[] bytes;
            lock (RuntimeData.Mutex)
            {
                bytes = DataHelper.ObjectToBytes(RuntimeData.QiZhiBuffOwnerDataList);
            }

            TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_SPR_LUOLANCHENGZHAN_QIZHI_OWNERINFO);

            //通知在线的所有人(不限制地图)领地信息数据通知
            GameManager.ClientMgr.BroadSpecialMapMessage(tcpOutPacket, RuntimeData.MapCode, -1, false);
            GameManager.ClientMgr.BroadSpecialMapMessage(tcpOutPacket, RuntimeData.MapCode_LongTa, -1);
        }

        /// <summary>
        /// 处理王城的归属--->只考虑帮会ID，不考虑具体角色
        /// </summary>
        private void HandleHuangChengResultEx(bool isBattleOver = false)
        {
            if (isBattleOver)
            {
                WangZuBHid = RuntimeData.LongTaOwnerData.OwnerBHid;
                WangZuBHName = RuntimeData.LongTaOwnerData.OwnerBHName;
                if (WangZuBHid <= 0)
                {
                    //处理王城战流产
                    JunQiManager.HandleLuoLanChengZhanResult((int)LingDiIDs.LuoLanChengZhan, RuntimeData.MapCode, 0, "", true, false);

                    //通知GameServer同步帮会的所属和范围
                    JunQiManager.NotifySyncBangHuiJunQiItemsDict(null);

                    //王城流产的提示
                    Global.BroadcastWangChengFailedHint();

                    //重新加载
                    ClearDbKingNpc();
                    InitLuoLanChengZhuInfo();

                    return;
                }
                else
                {
                    //处理领地战的结果
                    JunQiManager.HandleLuoLanChengZhanResult((int)LingDiIDs.LuoLanChengZhan, RuntimeData.MapCode, WangZuBHid, WangZuBHName, true, false);

                    //通知GameServer同步帮会的所属和范围
                    JunQiManager.NotifySyncBangHuiJunQiItemsDict(null);

                    //重新加载
                    ClearDbKingNpc();
                    InitLuoLanChengZhuInfo();

                    //[bing] 之前只要罗兰城被打就会记录 现在改为结束后记录 by 高剑南
                    // 合服活动期间记录获得罗兰城战的帮会
                    HeFuLuoLanActivity hefuActivity = HuodongCachingMgr.GetHeFuLuoLanActivity();
                    if (null != hefuActivity && hefuActivity.InActivityTime())
                    {
                        string strHefuLuolanGuildid = GameManager.GameConfigMgr.GetGameConfigItemStr(GameConfigNames.hefu_luolan_guildid, "");
                        // 目前一个合服活动中最多能完成两次罗兰城战

                        // 合服的时候会清掉这个数据
                        if (strHefuLuolanGuildid.Split('|').Length < 2)
                        {
                            // 认为前面有过记录加一个竖线做分隔
                            if (strHefuLuolanGuildid.Length > 0)
                            {
                                strHefuLuolanGuildid += "|";
                            }

                            int luoLanChengZhuRoleID = 0;
                            BangHuiDetailData bangHuiDetailData = GetBangHuiDetailDataAuto(WangZuBHid);
                            if (null != bangHuiDetailData)
                            {
                                //更新帮主的信息
                                luoLanChengZhuRoleID = bangHuiDetailData.BZRoleID;
                            }

                            strHefuLuolanGuildid += WangZuBHid.ToString() + "," + luoLanChengZhuRoleID.ToString();
                            Global.UpdateDBGameConfigg(GameConfigNames.hefu_luolan_guildid, strHefuLuolanGuildid);
                        }
                        else
                        {

                        }
                    }
                }
            }

            if (LastTheOnlyOneBangHui > 0)
            {
                //夺取王城的提示
                Global.BroadcastHuangChengOkHintEx(RuntimeData.LongTaOwnerData.OwnerBHName, isBattleOver);

                /*
                // 合服活动期间记录获得罗兰城战的帮会
                HeFuLuoLanActivity hefuActivity = HuodongCachingMgr.GetHeFuLuoLanActivity();
                if (null != hefuActivity && hefuActivity.InActivityTime())
                {
                    string strHefuLuolanGuildid = GameManager.GameConfigMgr.GetGameConfigItemStr(GameConfigNames.hefu_luolan_guildid, "");
                    // 目前一个合服活动中最多能完成两次罗兰城战

                    // 合服的时候会清掉这个数据
                    if (strHefuLuolanGuildid.Split('|').Length < 2)
                    {
                        // 认为前面有过记录加一个竖线做分隔
                        if (strHefuLuolanGuildid.Length > 0)
                        {
                            strHefuLuolanGuildid += "|";
                        }


                        int luoLanChengZhuRoleID = 0;
                        BangHuiDetailData bangHuiDetailData = GetBangHuiDetailDataAuto(LastTheOnlyOneBangHui);
                        if (null != bangHuiDetailData)
                        {
                            //更新帮主的信息
                            luoLanChengZhuRoleID = bangHuiDetailData.BZRoleID;
                        }

                        strHefuLuolanGuildid += LastTheOnlyOneBangHui + "," + luoLanChengZhuRoleID;
                        Global.UpdateDBGameConfigg(GameConfigNames.hefu_luolan_guildid, strHefuLuolanGuildid);
                    }
                    else
                    {

                    }
                }
                */
            }
        }

        #endregion 处理王城战的胜负结果

        #region 地图战斗状态数据

        /// <summary>
        /// 通知角色王城地图信息数据
        /// </summary>
        /// <param name="client"></param>
        public void NotifyClientWangChengMapInfoData(GameClient client)
        {
            WangChengMapInfoData wangChengMapInfoData = GetWangChengMapInfoData(client);
            GameManager.ClientMgr.NotifyWangChengMapInfoData(client, wangChengMapInfoData);
        }

        /// <summary>
        /// 获取地图战斗状态数据
        /// </summary>
        /// <returns></returns>
        public WangChengMapInfoData GetWangChengMapInfoData(GameClient client)
        {
            return FormatWangChengMapInfoData();
        }

        /// <summary>
        /// 获取地图战斗状态数据
        /// </summary>
        /// <returns></returns>
        public WangChengMapInfoData FormatWangChengMapInfoData()
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

        #region 指令处理

        /// <summary>
        /// 罗兰城战攻防竞价申请指令处理
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessChengZhanJingJiaCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = 0;
                int bidSite = Global.SafeConvertToInt32(cmdParams[0]);
                int bidMoney = Global.SafeConvertToInt32(cmdParams[1]);
                int bhid = client.ClientData.Faction;

                do
                {
                    if (bidSite < 1 || bidSite > RuntimeData.MaxZhanMengNum)
                    {
                        result = StdErrorCode.Error_Invalid_Params;
                        break;
                    }

                    if (!CanRequest())
                    {
                        result = StdErrorCode.Error_Not_In_valid_Time;
                        break;
                    }

                    if (bhid <= 0 || client.ClientData.BHZhiWu != 1)
                    {
                        result = StdErrorCode.Error_ZhanMeng_ShouLing_Only;
                        break;
                    }

                    int oldBHID = -1;
                    int oldBidMoney = 0;
                    int subBidMoney = 0;
                    LuoLanChengZhanRequestInfo requestInfo;
                    BangHuiLingDiItemData lingDiItem;
                    lock (RuntimeData.Mutex)
                    {
                        if (WangZuBHid == bhid)
                        {
                            result = StdErrorCode.Error_Invalid_Operation;
                            break;
                        }

                        lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.LuoLanChengZhan);
                        if (null != lingDiItem)
                        {
                            if (lingDiItem.WarRequest != RuntimeData.WarRequestStr)
                            {
                                RuntimeData.WarRequstDict = GetWarRequstMap(lingDiItem.WarRequest);
                                RuntimeData.WarRequestStr = lingDiItem.WarRequest;
                            }
                        }
                        else
                        {
                            RuntimeData.WarRequstDict = new Dictionary<int, LuoLanChengZhanRequestInfo>();
                            RuntimeData.WarRequestStr = null;
                        }

                        int oldSite;
                        if (RuntimeData.BHID2SiteDict.TryGetValue(bhid, out oldSite) && oldSite != bidSite)
                        {
                            result = StdErrorCode.Error_ZhanMeng_Has_Bid_OtherSite;
                            break;
                        }

                        if (!RuntimeData.WarRequstDict.TryGetValue(bidSite, out requestInfo))
                        {
                            requestInfo = new LuoLanChengZhanRequestInfo();
                            requestInfo.Site = bidSite;
                            RuntimeData.WarRequstDict.Add(bidSite, requestInfo);
                        }
                        else
                        {
                            oldBHID = requestInfo.BHID;
                            oldBidMoney = requestInfo.BidMoney;
                        }

                        //验证客户端传来的竞标价,如果不正确,则说明客户端数据过期,竞价金额已变化
                        if (bidMoney < oldBidMoney + RuntimeData.BidZhangMengZiJin)
                        {
                            result = StdErrorCode.Error_Data_Overdue;
                            break;
                        }

                        int bhZoneID = 0;
                        if (oldBHID == bhid)
                        {
                            subBidMoney = bidMoney - oldBidMoney;
                        }
                        else
                        {
                            subBidMoney = bidMoney;
                        }

                        //扣除所需的竞标资金
                        if (!GameManager.ClientMgr.SubBangHuiTongQian(Global._TCPManager.MySocketListener,
                            Global._TCPManager.tcpClientPool,
                            Global._TCPManager.TcpOutPacketPool,
                            client,
                            subBidMoney,
                            out bhZoneID))
                        {
                            result = StdErrorCode.Error_JinBi_Not_Enough;
                            //GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            //    client, StringUtil.substitute(Global.GetLang("战盟资金不足！"), Global.TakeSheLiZhiYuanNeedMoney / 10000),
                            //    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoTongQian);

                            break;
                        }

                        requestInfo.BHID = bhid;
                        requestInfo.BidMoney = bidMoney;

                        //保存结果
                        RuntimeData.WarRequestStr = GeWarRequstString(RuntimeData.WarRequstDict);
                        lingDiItem.WarRequest = RuntimeData.WarRequestStr;
                        SetCityWarRequestToDBServer(lingDiItem.LingDiID, lingDiItem.WarRequest);

                        ResetBHID2SiteDict();
                    }

                    //返还之前战盟的竞标资金
                    if (oldBHID != bhid && oldBHID > 0 && oldBidMoney > 0)
                    {
                        if (!GameManager.ClientMgr.AddBangHuiTongQian(Global._TCPManager.MySocketListener,
                            Global._TCPManager.tcpClientPool,
                            Global._TCPManager.TcpOutPacketPool,
                            client,
                            oldBHID,
                            oldBidMoney))
                        {
                            LogManager.WriteLog(LogTypes.SQL, string.Format("返还罗兰城战竞价资金失败,bhid={0}, bidMoney={1}", oldBHID, oldBidMoney));
                        }

                    }

                    //string broadCastMsg;
                    //if (oldBHID > 0)
                    //{
                    //    //全服广播消息
                    //    broadCastMsg = StringUtil.substitute(Global.GetLang("【{0}】通过竞标成功取代了【{1}】获得了罗兰峡谷活动的进攻名额！"), GetBHName(bhid), GetBHName(oldBHID));
                    //}
                    //else
                    //{
                    //    //全服广播消息
                    //    broadCastMsg = StringUtil.substitute(Global.GetLang("【{0}】通过竞标获得了罗兰峡谷活动的进攻名额！"), GetBHName(bhid));
                    //}

                    //Global.BroadcastRoleActionMsg(null, RoleActionsMsgTypes.Bulletin, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                    GameManager.ClientMgr.NotifyAllLuoLanChengZhanRequestInfoList(GetWarRequestInfoList());
                } while (false);

                //发送结果给客户端
                client.sendCmd(nID, string.Format("{0}", result));
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

        public bool ProcessLuoLanChengZhanCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = 0;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);
                int operation = Global.SafeConvertToInt32(cmdParams[1]);
                int bhid = client.ClientData.Faction;

                do
                {
                    int uniolLevel = Global.GetUnionLevel(client);
                    if (uniolLevel < Global.GetUnionLevel(RuntimeData.MinZhuanSheng, RuntimeData.MinLevel))
                    {
                        result = StdErrorCode.Error_Level_Limit;
                        break;
                    }

                    if (WangChengZhanState != WangChengZhanStates.Fighting)
                    {
                        result = StdErrorCode.Error_Not_In_valid_Time;
                        break;
                    }

                    if (bhid <= 0)
                    {
                        result = StdErrorCode.Error_ZhanMeng_Not_In_ZhanMeng;
                        break;
                    }

                    bool canEnter = false;
                    lock (RuntimeData.Mutex)
                    {
                        if (bhid == WangZuBHid)
                        {
                            canEnter = true;
                        }
                        else
                        {
                            foreach (var item in RuntimeData.WarRequstDict.Values)
                            {
                                if (item.BHID == bhid)
                                {
                                    canEnter = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!canEnter)
                    {
                        result = StdErrorCode.Error_ZhanMeng_Is_Unqualified;
                        break;
                    }

                    int toMapCode, toPosX, toPosY;
                    if (!GetZhanMengBirthPoint(client, RuntimeData.MapCode, out toMapCode, out toPosX, out toPosY))
                    {
                        result = StdErrorCode.Error_Config_Fault;
                        break;
                    }

                    GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, toMapCode, toPosX, toPosY, -1);
                } while (false);

                //发送结果给客户端
                client.sendCmd(nID, string.Format("{0}", result));
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
        /// 领取每日奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessGetChengZhanDailyAwardsCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success;

                do
                {
                    int roleID = Convert.ToInt32(cmdParams[0]);
                    int bhid = client.ClientData.Faction;
                    int lingDiID = (int)LingDiIDs.LuoLanChengZhan;

                    if (bhid <= 0 || client.ClientData.Faction != bhid)
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }

                    if ((int)LingDiIDs.LuoLanChengZhan != lingDiID)
                    {
                        result = StdErrorCode.Error_Type_Not_Match;
                        break;
                    }

                    BangHuiLingDiItemData lingdiItemData = JunQiManager.GetItemByLingDiID((int)lingDiID);
                    if (lingdiItemData.BHID != bhid)
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }

                    //战斗期间不让领
                    if (IsInBattling())
                    {
                        result = StdErrorCode.Error_Denied_In_Activity_Time;
                        break;
                    }

                    SiegeWarfareEveryDayAwardsItem awardsItem;
                    if (!RuntimeData.SiegeWarfareEveryDayAwardsDict.TryGetValue(client.ClientData.BHZhiWu, out awardsItem))
                    {
                        result = StdErrorCode.Error_ZhanMeng_ZhiWu_Not_Config;
                        break;
                    }

                    int lastDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SiegeWarfareEveryDayAwardDayID);
                    if (lastDayID == Global.GetOffsetDayNow())
                    {
                        result = StdErrorCode.Error_Has_Get;
                        break;
                    }

                    List<GoodsData> goodsDataList = Global.ConvertToGoodsDataList(awardsItem.DayGoods.Items);
                    if (Global.CanAddGoodsDataList(client, goodsDataList))
                    {
                        for (int i = 0; i < goodsDataList.Count; i++)
                        {
                            //向DBServer请求加入某个新的物品到背包中
                            Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsDataList[i].GoodsID, goodsDataList[i].GCount, goodsDataList[i].Quality, "",
                                                        goodsDataList[i].Forge_level, goodsDataList[i].Binding, 0, "", true, 1, /**/"罗兰城战胜利战盟每日奖励", Global.ConstGoodsEndTime,
                                                        0, goodsDataList[i].BornIndex, goodsDataList[i].Lucky, 0, goodsDataList[i].ExcellenceInfo, goodsDataList[i].AppendPropLev);

                            GoodsData goodsData = goodsDataList[i];
                            GameManager.logDBCmdMgr.AddDBLogInfo(goodsData.Id, Global.ModifyGoodsLogName(goodsData), "罗兰城战胜利战盟每日奖励", Global.GetMapName(client.ClientData.MapCode), client.ClientData.RoleName, "增加", goodsData.GCount, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId);
                        }
                    }
                    else
                    {
                        result = StdErrorCode.Error_BagNum_Not_Enough;
                    }

                    long exp = awardsItem.DayExp;
                    int zhanGong = awardsItem.DayZhanGong;
                    if (result >= 0)
                    {
                        //设置领取标识
                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.SiegeWarfareEveryDayAwardDayID, Global.GetOffsetDayNow(), true);

                        //领取经验
                        if (exp > 0)
                        {
                            GameManager.ClientMgr.ProcessRoleExperience(client, exp);
                            long newExp = client.ClientData.Experience;
                            GameManager.SystemServerEvents.AddEvent(string.Format("角色根据领地特权领取经验, roleID={0}({1}), exp={2}, newExp={3}, bhid={4}", client.ClientData.RoleID, client.ClientData.RoleName, exp, exp, bhid), EventLevels.Record);
                        }

                        //领取战功
                        if (zhanGong > 0)
                        {
                            if (GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ref zhanGong, AddBangGongTypes.BG_ChengZhan))
                            {
                                //[bing] 记录战功增加流向log
                                if (0 != zhanGong)
                                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "战功", "罗兰城战每日奖励", "系统", client.ClientData.RoleName, "增加", zhanGong, client.ClientData.ZoneID, client.strUserID, client.ClientData.BangGong, client.ServerId);
                            }
                        }
                    }
                } while (false);

                client.sendCmd(nID, string.Format("{0}", result));
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
        /// 查询罗兰城主帮会信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessGetLuoLanChengZhuInfoCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int roleID = Convert.ToInt32(cmdParams[0]);

                LuoLanChengZhuInfo luoLanChengZhuInfo = GetLuoLanChengZhuInfo(client);
                if (client.ClientData.Faction == luoLanChengZhuInfo.BHID && luoLanChengZhuInfo.BHID > 0)
                {
                    int lastDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SiegeWarfareEveryDayAwardDayID);
                    if (lastDayID != Global.GetOffsetDayNow())
                    {
                        luoLanChengZhuInfo.isGetReward = false;
                    }
                }

                //发送结果给客户端
                client.sendCmd(nID, luoLanChengZhuInfo);
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
        /// 获取单个帮会领地信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessLuoLanChengZhanRequestInfoListCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                GameManager.ClientMgr.NotifyLuoLanChengZhanRequestInfoList(client, GetWarRequestInfoList());
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
        /// 获取单个帮会领地信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessQueryZhanMengZiJinCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                GameManager.ClientMgr.NotifyBangHuiZiJinChanged(client, client.ClientData.Faction);
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

        public bool ProcessGetLuoLanKingLooks(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int lookWho = Convert.ToInt32(cmdParams[1]);
                RoleDataEx rd = KingRoleData;
                if (rd != null && rd.RoleID == lookWho)
                {
                    RoleData4Selector sel = Global.RoleDataEx2RoleData4Selector(rd);
                    client.sendCmd(nID, sel);
                }

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

        #endregion 指令处理

        #region 领地管理

        /// <summary>
        /// 从王城战申请字符串解析出申请映射辞典
        /// banghuiid_day,banghuiid_day,banghuiid_day
        /// </summary>
        /// <param name="warReqString"></param>
        /// <returns></returns>
        public Dictionary<int, LuoLanChengZhanRequestInfo> GetWarRequstMap(string warReqString)
        {
            Dictionary<int, LuoLanChengZhanRequestInfo> warRequstMap = null;

            try
            {
                byte[] bytes = Convert.FromBase64String(warReqString);
                warRequstMap = DataHelper.BytesToObject<Dictionary<int, LuoLanChengZhanRequestInfo>>(bytes, 0, bytes.Length);
            }
            catch (System.Exception ex)
            {

            }

            if (null == warRequstMap)
            {
                warRequstMap = new Dictionary<int, LuoLanChengZhanRequestInfo>();
            }

            return warRequstMap;
        }

        /// <summary>
        /// 返回新的王城争夺战请求日期列表字符串
        /// </summary>
        /// <returns></returns>
        public string GeWarRequstString(Dictionary<int, LuoLanChengZhanRequestInfo> warRequstMap)
        {
            string nowWarRequest = "";

            //生成新的字符串，并提交给gamedbserver，成功之后进行广播通知
            try
            {
                byte[] bytes = DataHelper.ObjectToBytes(warRequstMap);
                return Convert.ToBase64String(bytes);
            }
            catch (System.Exception ex)
            {

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
        public int SetCityWarRequestToDBServer(int lingDiID, String nowWarRequest)
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

        #endregion 领地管理

        #region 辅助函数

        /// <summary>
        /// 这个功能是否开启了,且在允许的竞标时间段内
        /// </summary>
        /// <returns></returns>
        public bool CanRequest()
        {
            DateTime now = TimeUtil.NowDateTime();
            if ((now - Global.GetKaiFuTime()).TotalDays < RuntimeData.GongNengOpenDaysFromKaiFu)
            {
                return false;
            }

            if (IsDayOfWeek((int)now.DayOfWeek))
            {
                TimeSpan time = now.TimeOfDay;
                if (time >= RuntimeData.NoRequestTimeStart && time <= RuntimeData.NoRequestTimeEnd)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 重新生成帮会ID到位置的映射字典
        /// </summary>
        private void ResetBHID2SiteDict()
        {
            lock (RuntimeData.Mutex)
            {
                RuntimeData.BHID2SiteDict.Clear();
                if (WangZuBHid > 0)
                {
                    RuntimeData.BHID2SiteDict[WangZuBHid] = 0;
                }
                foreach (var item in RuntimeData.WarRequstDict.Values)
                {
                    RuntimeData.BHID2SiteDict[item.BHID] = item.Site;
                }
            }
        }

        /// <summary>
        /// 判断当天是否存在王城争夺战
        /// </summary>
        /// <returns></returns>
        public bool IsExistCityWarToday()
        {
            if (!IsDayOfWeek((int)TimeUtil.NowDateTime().DayOfWeek))
            {
                return false;
            }
            lock (RuntimeData.Mutex)
            {
                if (RuntimeData.WarRequstDict.Count == 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 返回战斗结束时的毫秒数
        /// </summary>
        /// <returns></returns>
        public long GetBattleEndMs()
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
        public string GetNextCityBattleTime()
        {
            //实现过程不采用 GetNextCityBattleTimeAndBangHui()了，这个函数是先写的
            string unKown = Global.GetLang("无");

            //返回时 分
            if (null != WangChengZhanFightingDayTimes && WangChengZhanFightingDayTimes.Length > 0)
            {
                return RuntimeData.WangChengZhanFightingDateTime.ToString("yyyy-MM-dd ") + String.Format("{0:00}:{1:00}", WangChengZhanFightingDayTimes[0].FromHour, WangChengZhanFightingDayTimes[0].FromMinute);
            }

            return unKown;
        }

        /// <summary>
        /// 返回下次王城争霸赛的时间和帮会
        /// </summary>
        public string GetCityBattleTimeAndBangHuiListString()
        {
            if (null == WangChengZhanFightingDayTimes || WangChengZhanFightingDayTimes.Length <= 0)
            {
                return "";
            }

            string timeBangHuiString = "";
            lock (RuntimeData.Mutex)
            {
                timeBangHuiString += RuntimeData.WangChengZhanFightingDateTime.ToString("yyyy-MM-dd ") + string.Format("{0:00}:{1:00}", WangChengZhanFightingDayTimes[0].FromHour, WangChengZhanFightingDayTimes[0].FromMinute);
                timeBangHuiString += "|";
                foreach (var req in RuntimeData.WarRequstDict.Values)
                {
                    timeBangHuiString += string.Format(" {0}", GetBHName(req.BHID));
                }
            }

            return timeBangHuiString;
        }

        /// <summary>
        /// 获取帮会名称
        /// </summary>
        /// <param name="warDay"></param>
        /// <param name="bangHuiID"></param>
        /// <param name="dayTime"></param>
        /// <param name="bangHuiName"></param>
        /// <returns></returns>
        private string GetBHName(int bangHuiID)
        {
            BangHuiMiniData bhData = Global.GetBangHuiMiniData(bangHuiID);
            if (null != bhData)
            {
                return bhData.BHName;//Global.FormatBangHuiName(bhData.ZoneID, bhData.BHName);
            }

            return Global.GetLang("无");
        }

        #endregion 辅助函数

        #region 定时给在场的玩家家经验

        /// <summary>
        /// 定时给予收益
        /// </summary>
        private long LastAddBangZhanAwardsTicks = 0;

        /// <summary>
        /// 定时给在场的玩家增加经验
        /// </summary>
        private void ProcessTimeAddRoleExp()
        {
            long ticks = TimeUtil.NOW();
            if (ticks - LastAddBangZhanAwardsTicks < (10 * 1000))
            {
                return;
            }

            LastAddBangZhanAwardsTicks = ticks;

            //刷新旗帜Buff拥有者信息
            NotifyQiZhiBuffOwnerDataList();

            //先找出当前大乱斗中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(RuntimeData.MapCode);
            if (null != objsList)
            {
                for (int i = 0; i < objsList.Count; i++)
                {
                    GameClient c = objsList[i] as GameClient;
                    if (c == null) continue;

                    //if (c.ClientData.CurrentLifeV <= 0) continue;

                    /// 处理用户的经验奖励
                    _LevelAwardsMgr.ProcessBangZhanAwards(c);
                }
            }

            objsList = GameManager.ClientMgr.GetMapClients(RuntimeData.MapCode_LongTa);
            if (null != objsList)
            {
                for (int i = 0; i < objsList.Count; i++)
                {
                    GameClient c = objsList[i] as GameClient;
                    if (c == null) continue;

                    //if (c.ClientData.CurrentLifeV <= 0) continue;

                    /// 处理用户的经验奖励
                    _LevelAwardsMgr.ProcessBangZhanAwards(c);
                }
            }
        }

        #endregion 定时给在场的玩家家经验

        #region 复活与地图传送

        /// <summary>
        /// 获取出生点或复活点位置
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toLongTa"></param>
        /// <param name="mapCode"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <returns></returns>
        public bool GetZhanMengBirthPoint(GameClient client, int toMapCode, out int mapCode, out int posX, out int posY)
        {
            mapCode = GameManager.MainMapCode;
            posX = -1;
            posY = -1;
            int bhid = client.ClientData.Faction;
            lock (RuntimeData.Mutex)
            {
                int site;
                if (!RuntimeData.BHID2SiteDict.TryGetValue(bhid, out site))
                {
                    return true;
                }

                int round = 0;
                if (toMapCode == RuntimeData.MapCode_LongTa)
                {
                    do
                    {
                        Point pt = Global.GetRandomPoint(ObjectTypes.OT_CLIENT, RuntimeData.MapCode_LongTa);
                        if (!Global.InObs(ObjectTypes.OT_CLIENT, RuntimeData.MapCode_LongTa, (int)pt.X, (int)pt.Y))
                        {
                            mapCode = RuntimeData.MapCode_LongTa;
                            posX = (int)pt.X;
                            posY = (int)pt.Y;

                            return true;
                        }
                    } while (round++ < 1000);
                }

                //特殊复活点,拥有指定旗帜后有效
                round = 0;
                if (client.ClientData.Faction == RuntimeData.SuperQiZhiOwnerBhid && toMapCode == RuntimeData.MapCode)
                {
                    do
                    {
                        mapCode = toMapCode;
                        posX = Global.GetRandomNumber(RuntimeData.SuperQiZhiOwnerBirthPosX - 400, RuntimeData.SuperQiZhiOwnerBirthPosX + 400);
                        posY = Global.GetRandomNumber(RuntimeData.SuperQiZhiOwnerBirthPosY - 400, RuntimeData.SuperQiZhiOwnerBirthPosY + 400);
                        if (!Global.InObs(ObjectTypes.OT_CLIENT, toMapCode, (int)posX, (int)posY))
                        {
                            return true;
                        }
                    } while (round++ < 100);
                }

                List<MapBirthPoint> list;
                if (!RuntimeData.MapBirthPointListDict.TryGetValue(site, out list) || list.Count == 0)
                {
                    return true;
                }

                round = 0;
                do
                {
                    int rnd = Global.GetRandomNumber(0, list.Count);
                    MapBirthPoint mapBirthPoint = list[rnd];
                    mapCode = mapBirthPoint.MapCode;
                    posX = mapBirthPoint.BirthPosX + Global.GetRandomNumber(-mapBirthPoint.BirthRangeX, mapBirthPoint.BirthRangeX);
                    posY = mapBirthPoint.BirthPosY + Global.GetRandomNumber(-mapBirthPoint.BirthRangeY, mapBirthPoint.BirthRangeY);
                    if (!Global.InObs(ObjectTypes.OT_CLIENT, mapCode, posX, posY))
                    {
                        return true;
                    }
                } while (round++ < 1000);
            }

            return true;
        }

        /// <summary>
        /// 角色复活
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="direction"></param>
        public bool ClientRelive(GameClient client)
        {
            int mapCode = client.ClientData.MapCode;
            int toMapCode, toPosX, toPosY;
            if (mapCode == RuntimeData.MapCode || mapCode == RuntimeData.MapCode_LongTa)
            {
                if (GetZhanMengBirthPoint(client, RuntimeData.MapCode, out toMapCode, out toPosX, out toPosY))
                {
                    client.ClientData.CurrentLifeV = client.ClientData.LifeV;
                    client.ClientData.CurrentMagicV = client.ClientData.MagicV;

                    client.ClientData.MoveAndActionNum = 0;

                    //通知队友自己要复活
                    GameManager.ClientMgr.NotifyTeamRealive(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.ClientData.RoleID, toPosX, toPosY, -1);

                    //马上通知切换地图---->这个函数每次调用前，如果地图未发生发变化，则直接通知其他人自己位置变动
                    //比如在扬州城死 回 扬州城复活，就是位置变化
                    if (toMapCode != client.ClientData.MapCode)
                    {
                        //通知自己要复活
                        GameManager.ClientMgr.NotifyMySelfRealive(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, client.ClientData.RoleID, client.ClientData.PosX, client.ClientData.PosY, -1);

                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, toMapCode, toPosX, toPosY, -1, 1);
                    }
                    else
                    {
                        Global.ClientRealive(client, toPosX, toPosY, -1);
                    }

                    return true;
                }
            }

            return false;
        }

        public bool ClientInitGame(GameClient client)
        {
            int mapCode = client.ClientData.MapCode;
            int toMapCode, toPosX, toPosY;
            if (mapCode == RuntimeData.MapCode || mapCode == RuntimeData.MapCode_LongTa)
            {
                if (WangChengZhanState != WangChengZhanStates.Fighting)
                {
                    client.ClientData.MapCode = GameManager.MainMapCode;
                    client.ClientData.PosX = -1;
                    client.ClientData.PosY = -1;
                }
                else if (GetZhanMengBirthPoint(client, RuntimeData.MapCode, out toMapCode, out toPosX, out toPosY))
                {
                    client.ClientData.MapCode = toMapCode;
                    client.ClientData.PosX = toPosX;
                    client.ClientData.PosY = toPosY;
                }
            }

            return true;
        }

        public bool ClientChangeMap(GameClient client, ref int toNewMapCode, ref int toNewPosX, ref int toNewPosY)
        {
            if (toNewMapCode == RuntimeData.MapCode || toNewMapCode == RuntimeData.MapCode_LongTa)
            {
                int toMapCode, toPosX, toPosY;
                if (WangChengZhanState != WangChengZhanStates.Fighting)
                {
                    toNewMapCode = GameManager.MainMapCode;
                    toNewPosX = -1;
                    toNewPosY = -1;
                }
                else if (client.ClientData.MapCode == RuntimeData.MapCode_LongTa)
                {
                    //从龙塔通过传送点传到罗兰峡谷地图,不作特殊处理,按传送点配置传送
                }
                else if (GetZhanMengBirthPoint(client, toNewMapCode, out toMapCode, out toPosX, out toPosY))
                {
                    toNewMapCode = toMapCode;
                    toNewPosX = toPosX;
                    toNewPosY = toPosY;
                }
            }

            return true;
        }

        #endregion 复活与地图传送

        #region 军旗和BUFF

        /// <summary>
        /// 安插军旗前的检查
        /// </summary>
        /// <param name="client"></param>
        /// <param name="npcID"></param>
        /// <returns></returns>
        public bool OnPreInstallJunQi(GameClient client, int npcID)
        {
            if (!IsInBattling())
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, StringUtil.substitute(Global.GetLang("只有罗兰城战期间才能安插帮旗!")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

                return false;
            }

            //判断是否在帮会中，否则不允许安插帮旗
            if (client.ClientData.Faction <= 0)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, StringUtil.substitute(Global.GetLang("只有战盟成员才能安插帮旗!")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

                return false;
            }

            int oldBHID = 0;
            lock (RuntimeData.Mutex)
            {
                for (int i = 0; i < RuntimeData.QiZhiBuffOwnerDataList.Count; i++)
                {
                    if (RuntimeData.QiZhiBuffOwnerDataList[i].NPCID == npcID)
                    {
                        oldBHID = RuntimeData.QiZhiBuffOwnerDataList[i].OwnerBHID;
                        break;
                    }
                }
            }

            if (oldBHID > 0)
            {
                return false;
            }

            //判断是否是本帮派能安插帮旗
            //是否能够安插帮旗
            if (!JunQiManager.CanInstallJunQiNow(client.ClientData.MapCode, npcID - SpriteBaseIds.NpcBaseId, client.ClientData.Faction))
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, StringUtil.substitute(Global.GetLang("非砍倒盟旗的战盟，在原有盟旗被砍倒10秒后，才能安插盟旗！")),
                    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

                return false;
            }

            return true;
        }

        /// <summary>
        /// 非活动时间，将地图中的人清出场景
        /// </summary>
        /// <param name="resetTimeOnly"> 仅重置上次清除时间为当前时间</param>
        public void ClearMapClients(bool resetTimeOnly = false)
        {
            if (resetTimeOnly)
            {
                RuntimeData.LastClearMapTicks = TimeUtil.NOW();
            }
            else
            {
                long nowTicks = TimeUtil.NOW();
                if (nowTicks - RuntimeData.LastClearMapTicks > TimeUtil.MINITE)
                {
                    RuntimeData.LastClearMapTicks = nowTicks;

                    List<Object> objsList = GameManager.ClientMgr.GetMapClients(RuntimeData.MapCode);
                    if (null != objsList && objsList.Count > 0)
                    {
                        for (int i = 0; i < objsList.Count; i++)
                        {
                            GameClient c = objsList[i] as GameClient;
                            if (c == null) continue;

                            GameManager.ClientMgr.NotifyChangMap2NormalMap(c);
                        }
                    }

                    objsList = GameManager.ClientMgr.GetMapClients(RuntimeData.MapCode_LongTa);
                    if (null != objsList && objsList.Count > 0)
                    {
                        for (int i = 0; i < objsList.Count; i++)
                        {
                            GameClient c = objsList[i] as GameClient;
                            if (c == null) continue;

                            GameManager.ClientMgr.NotifyChangMap2NormalMap(c);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 安插军旗时,添加通知并添加buff
        /// </summary>
        /// <param name="client"></param>
        /// <param name="npcID"></param>
        public void OnInstallJunQi(GameClient client, int npcID)
        {
            //处理扣除铜钱的操作
            //扣除帮会库存铜钱
            int bhZoneID = 0;
            if (RuntimeData.InstallJunQiNeedMoney > 0)
            {
                if (!GameManager.ClientMgr.SubBangHuiTongQian(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool,
                    Global._TCPManager.TcpOutPacketPool,
                    client,
                    RuntimeData.InstallJunQiNeedMoney,
                    out bhZoneID))
                {
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, StringUtil.substitute(Global.GetLang("战盟库存金币不足，无法安插盟旗！")),
                        GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoTongQian);

                    return;
                }
            }

            //处理获取帮旗名称的操作
            string junQiName = JunQiManager.GetJunQiNameByBHID(client.ClientData.Faction);

            //处理获取帮旗级别的操作
            int junQiLevel = JunQiManager.GetJunQiLevelByBHID(client.ClientData.Faction);

            //通知显示帮旗
            bool installed = JunQiManager.ProcessNewJunQi(
                Global._TCPManager.MySocketListener,
                Global._TCPManager.TcpOutPacketPool,
                client.ClientData.MapCode,
                client.ClientData.Faction,
                bhZoneID,
                client.ClientData.BHName,
                npcID - SpriteBaseIds.NpcBaseId,
                junQiName,
                junQiLevel,
                ManagerType);

            if (installed)
            {
                //通知地图变动信息
                UpdateQiZhiBangHui(npcID - SpriteBaseIds.NpcBaseId, client.ClientData.Faction, client.ClientData.BHName);

                Global.BroadcastBangHuiMsg(-1, client.ClientData.Faction,
                    StringUtil.substitute(Global.GetLang("本战盟成员【{0}】成功在{1}『{2}』安插了本战盟旗帜，可喜可贺"),
                    Global.FormatRoleName(client, client.ClientData.RoleName),
                    Global.GetServerLineName2(),
                    Global.GetMapName(client.ClientData.MapCode)),
                    true, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlySysHint);
            }
        }

        /// <summary>
        /// 军旗死亡时,通知并更新buff
        /// </summary>
        /// <param name="npcID"></param>
        /// <param name="bhid"></param>
        public void OnProcessJunQiDead(int npcID, int bhid)
        {
            UpdateQiZhiBangHui(npcID, 0, "");
        }

        /// <summary>
        /// 设置玩家的旗帜buff
        /// </summary>
        /// <param name="client"></param>
        private void ResetQiZhiBuff(GameClient client)
        {
            int toMapCode = client.ClientData.MapCode;
            List<int> bufferIDList = new List<int>();
            lock (RuntimeData.Mutex)
            {
                for (int i = 0; i < RuntimeData.QiZhiBuffOwnerDataList.Count; i++)
                {
                    QiZhiConfig qiZhiConfig;
                    if (RuntimeData.NPCID2QiZhiConfigDict.TryGetValue(RuntimeData.QiZhiBuffOwnerDataList[i].NPCID, out qiZhiConfig))
                    {
                        int bufferID = qiZhiConfig.BufferID;
                        EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(bufferID);
                        if (null != item)
                        {
                            bool add = false;
                            if (toMapCode == RuntimeData.MapCode || toMapCode == RuntimeData.MapCode_LongTa)
                            {
                                if (RuntimeData.QiZhiBuffOwnerDataList[i].OwnerBHID == client.ClientData.Faction)
                                {
                                    add = true;
                                }
                            }

                            UpdateQiZhiBuff4GameClient(client, item, bufferID, add);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 地图加载完成并准备开始游戏时
        /// </summary>
        /// <param name="client"></param>
        public void OnStartPlayGame(GameClient client)
        {
            ResetQiZhiBuff(client);

            if (client.ClientData.MapCode == RuntimeData.MapCode)
            {
                _MapEventMgr.PlayMapEvents(client);
            }

            BroadcastLuoLanChengZhuLoginHint(client);
        }

        /// <summary>
        /// 皇帝上线的提示
        /// </summary>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        public void BroadcastLuoLanChengZhuLoginHint(GameClient client)
        {
            if (RuntimeData.LuoLanChengZhuClient != client && client.ClientData.Faction == RuntimeData.LuoLanChengZhuBHID && client.ClientData.BHZhiWu == (int)ZhanMengZhiWus.ShouLing)
            {
                long nowTicks = TimeUtil.NOW();
                if (nowTicks > RuntimeData.LuoLanChengZhuLastLoginTicks + TimeUtil.MINITE)
                {
                    RuntimeData.LuoLanChengZhuLastLoginTicks = nowTicks;
                    RuntimeData.LuoLanChengZhuClient = client;

                    //播放用户行为消息
                    string broadCastMsg = StringUtil.substitute(Global.GetLang("伟大的罗兰城主【{0}】上线了！"), Global.FormatRoleName(client, client.ClientData.RoleName));
                    Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.Bulletin, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                }
            }

        }

        /// <summary>
        /// 处理玩家点击NPC事件
        /// 如果是旗座,尝试安装帮旗
        /// </summary>
        /// <param name="client"></param>
        /// <param name="npcID"></param>
        /// <returns>是否是旗座NPC</returns>
        public bool OnSpriteClickOnNpc(GameClient client, int npcID, int npcExtentionID)
        {
            bool isQiZuo = false;
            bool installJunQi = false;
            lock (RuntimeData.Mutex)
            {
                foreach (var item in RuntimeData.NPCID2QiZhiConfigDict.Values)
                {
                    if (item.NPCID == npcExtentionID)
                    {
                        //在1000*1000的距离内才可以
                        if (Math.Abs(client.ClientData.PosX - item.PosX) <= 1000 && Math.Abs(client.ClientData.PosY - item.PosY) <= 1000)
                        {
                            installJunQi = true;
                        }

                        isQiZuo = true;
                        break;
                    }
                }
            }

            if (installJunQi)
            {
                Global.InstallJunQi(client, npcID, ManagerType);
            }

            return isQiZuo;
        }

        #endregion 军旗和BUFF

        #region 光幕

        /// <summary>
        /// 地图光幕事件
        /// </summary>
        /// <param name="guangMuID"></param>
        /// <param name="show"></param>
        public void AddGuangMuEvent(int guangMuID, int show)
        {
            _MapEventMgr.AddGuangMuEvent(guangMuID, show);
        }

        #endregion 光幕

        #region 战盟事件钩子

        /// <summary>
        /// 战盟添加成员事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public bool OnPreBangHuiAddMember(PreBangHuiAddMemberEventObject e)
        {
            if (!IsInBattling())
            {
                return false;
            }

            lock (RuntimeData.Mutex)
            {
                if (RuntimeData.BHID2SiteDict.ContainsKey(e.BHID))
                {
                    e.Result = false;
                }
            }

            if (!e.Result)
            {
                GameManager.ClientMgr.NotifyImportantMsg(e.Player, Global.GetLang("罗兰城战活动中的战盟不能接收新成员!"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 战盟添加删除事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bhid"></param>
        /// <returns></returns>
        public bool OnPreBangHuiRemoveMember(PreBangHuiRemoveMemberEventObject e)
        {
            if (!IsInBattling())
            {
                return false;
            }

            lock (RuntimeData.Mutex)
            {
                if (RuntimeData.BHID2SiteDict.ContainsKey(e.BHID))
                {
                    e.Result = false;
                }
            }

            if (!e.Result)
            {
                GameManager.ClientMgr.NotifyImportantMsg(e.Player, Global.GetLang("罗兰城战活动中的战盟不能有成员退出战盟!"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return true;
            }

            return false;
        }

        #endregion 战盟事件钩子

        #region 罗兰城主雕像

        private object kingRoleDataMutex = new object();
        private RoleDataEx _kingRoleData = null;
        private RoleDataEx KingRoleData
        {
            get { lock (kingRoleDataMutex) { return _kingRoleData; } }
            set { lock (kingRoleDataMutex) { _kingRoleData = value; } }
        }

        /// <summary>
        /// 重新恢复显示PK之王
        /// </summary>
        public void ReShowLuolanKing(int roleID = 0)
        {
            if (roleID <= 0)
            {
                roleID = LuoLanChengZhanManager.getInstance().GetLuoLanChengZhuRoleID();
            }

            if (roleID <= 0)
            {
                RestoreLuolanKingNpc();
                return;
            }

            ReplaceLuolanKingNpc(roleID);
        }

        public void ClearDbKingNpc()
        {
            this.KingRoleData = null;
            Global.sendToDB<bool, string>((int)TCPGameServerCmds.CMD_DB_CLR_KING_ROLE_DATA, string.Format("{0}", (int)KingRoleType.LuoLanKing), GameManager.LocalServerId);
        }

        /// <summary>
        /// 替换罗兰城主的npc显示
        /// </summary>
        /// <param name="clientData"></param>
        public void ReplaceLuolanKingNpc(int roleId)
        {
            RoleDataEx rd = KingRoleData;
            KingRoleData = null;
            if (rd == null || rd.RoleID != roleId)
            {
                rd = Global.sendToDB<RoleDataEx, KingRoleGetData>((int)TCPGameServerCmds.CMD_DB_GET_KING_ROLE_DATA,
                    new KingRoleGetData() { KingType = (int)KingRoleType.LuoLanKing }, GameManager.LocalServerId);

                if (rd == null || rd.RoleID != roleId)
                {
                    RoleDataEx dbRd = Global.sendToDB<RoleDataEx, string>(
                        (int)TCPGameServerCmds.CMD_SPR_GETOTHERATTRIB2,
                        string.Format("{0}:{1}", -1, roleId),
                        GameManager.LocalServerId);
                    if (dbRd == null || dbRd.RoleID <= 0) return;

                    rd = dbRd;
                    bool bSave = Global.sendToDB<bool, KingRolePutData>((int)TCPGameServerCmds.CMD_DB_PUT_KING_ROLE_DATA,
                                    new KingRolePutData() { KingType = (int)KingRoleType.LuoLanKing, RoleDataEx = rd }, GameManager.LocalServerId);
                    if (!bSave)
                    {

                    }
                }
            }

            if (rd == null || rd.RoleID <= 0)
                return;

            KingRoleData = rd;

            NPC npc = NPCGeneralManager.FindNPC(GameManager.MainMapCode, 131);
            if (null != npc)
            {
                npc.ShowNpc = false;
                GameManager.ClientMgr.NotifyMySelfDelNPCBy9Grid(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, npc);
                FakeRoleManager.ProcessDelFakeRoleByType(FakeRoleTypes.DiaoXiang2);
                SafeClientData clientData = new SafeClientData();
                clientData.RoleData = rd;
                FakeRoleManager.ProcessNewFakeRole(clientData, npc.MapCode, FakeRoleTypes.DiaoXiang2, 4, (int)npc.CurrentPos.X, (int)npc.CurrentPos.Y, 131);
            }
        }

        /// <summary>
        /// 回复罗兰城主的雕像
        /// </summary>
        public void RestoreLuolanKingNpc()
        {
            NPC npc = NPCGeneralManager.FindNPC(GameManager.MainMapCode, 131);
            if (null != npc)
            {
                npc.ShowNpc = true;
                GameManager.ClientMgr.NotifyMySelfNewNPCBy9Grid(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, npc);
                FakeRoleManager.ProcessDelFakeRoleByType(FakeRoleTypes.DiaoXiang2);
            }
        }

        #endregion

        public int GetLuoLanChengZhuRoleID()
        {
            int luoLanChengZhuRoleID = 0;
            lock (RuntimeData.Mutex)
            {
                BangHuiLingDiItemData lingDiItem = JunQiManager.GetItemByLingDiID((int)LingDiIDs.LuoLanChengZhan);
                if (null != lingDiItem)
                {
                    BangHuiDetailData bangHuiDetailData = GetBangHuiDetailDataAuto(lingDiItem.BHID);
                    if (null != bangHuiDetailData)
                    {
                        //更新帮主的信息
                        luoLanChengZhuRoleID = bangHuiDetailData.BZRoleID;
                    }
                }
            }
            return luoLanChengZhuRoleID;
        }

        #region
        public void OnChangeName(int roleId, string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                return;
            }

            RoleDataEx rd = KingRoleData;
            if (rd != null && rd.RoleID == roleId)
            {
                rd.RoleName = newName;

                bool bSave = Global.sendToDB<bool, KingRolePutData>((int)TCPGameServerCmds.CMD_DB_PUT_KING_ROLE_DATA,
                                   new KingRolePutData() { KingType = (int)KingRoleType.LuoLanKing, RoleDataEx = rd }, GameManager.LocalServerId);
                if (!bSave)
                {

                }

                KingRoleData = null;
                ReShowLuolanKing();
            }
        }
        #endregion
    }
}
