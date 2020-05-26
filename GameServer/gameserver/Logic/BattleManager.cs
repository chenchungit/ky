using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
//using System.Windows.Resources;
using System.Windows;
using System.Threading;
using GameServer.Interface;
using Server.Data;
using GameServer.Server;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using System.Net;
using System.Net.Sockets;
using HSGameEngine.Tools.AStar;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    #region 得分与奖励结构定义

    public class BattlePointInfo : System.IComparable<BattlePointInfo>, IComparer<BattlePointInfo>
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public int m_RoleID = 0;

        /// <summary>
        /// 累计得分
        /// </summary>
        public int m_DamagePoint = 0;

        /// <summary>
        /// 是否离场状态
        /// </summary>
        public bool LeaveScene = false;

        /// <summary>
        /// 排名
        /// </summary>
        public int Ranking = -1;

        /// <summary>
        /// 所属阵营
        /// </summary>
        public int Side = 0;

        /// <summary>
        /// 角色名
        /// </summary>
        public string m_RoleName;

        /// <summary>
        /// 玩家奖励领取标记
        /// </summary>
        public int m_GetAwardFlag = 0;

        /// <summary>
        /// 幸运奖排名
        /// </summary>
        //public int m_LuckPaiMingID = 0;
        public string m_LuckPaiMingName = "";

        /// <summary>
        /// 物品奖励列表(所有的)
        /// </summary>
        public AwardsItemList GoodsList = new AwardsItemList();

        /// <summary>
        /// 排名奖排名
        /// </summary>
        public int m_AwardPaiMing = 0;

        /// <summary>
        /// 声望奖励
        /// </summary>
        public int m_AwardShengWang = 0;

        /// <summary>
        /// 金币奖励
        /// </summary>
        public int m_AwardGold = 0;

        #region 比较函数

        /// <summary>
        /// 排序比较函数
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public int CompareTo(BattlePointInfo y)
        {
            if (this == y)
            {
                return 0;
            }
            else if (y == null)
            {
                return -1;
            }
            else
            {
                return y.m_DamagePoint - this.m_DamagePoint;
            }
        }

        /// <summary>
        /// 用于从大到小排列的比较函数(倒排)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(BattlePointInfo x, BattlePointInfo y)
        {
            if (x == y)
            {
                return 0;
            }
            else if (x != null && y != null)
            {
                return y.m_DamagePoint - x.m_DamagePoint;
            }
            else if (x == null)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        #endregion 比较函数
    }

    #endregion 得分与奖励结构定义

    /// <summary>
    /// 大乱斗调度和管理类 隋唐争霸-炎黄战场  -- MU 阵营战 liaowei
    /// </summary>
    public class BattleManager
    {
        #region 配置变量定义

        /// <summary>
        /// 最高分列表中的人数
        /// </summary>
        public const int ConstTopPointNumber = 5;

        /// <summary>
        /// 击杀敌对玩家得积分数
        /// </summary>
        public const int ConstJiFenByKillRole = 5;

        /// <summary>
        /// 被击杀得积分数
        /// </summary>
        public const int ConstJiFenByKilled = 1;

        /// <summary>
        /// 大乱斗开始调度的时间点
        /// </summary>
        private List<string> TimePointsList = new List<string>();

        /// <summary>
        /// 大乱斗的场景编号
        /// </summary>
        private int MapCode = -1;

        /// <summary>
        /// 要求的最低级别
        /// </summary>
        private int MinLevel = 20;

        /// <summary>
        /// 全区要求的最少在线人数
        /// </summary>
        private int MinRequestNum = 100;

        /// <summary>
        /// 允许进入大乱斗场景的最多人数
        /// </summary>
        private int MaxEnterNum = 30;

        /// <summary>
        /// 每次大乱斗活动结束后，分配的奖品的数量
        /// </summary>
        private int FallGiftNum = 5;

        /// <summary>
        /// 奖励的物品的掉落ID
        /// </summary>
        private int FallID = -1;

        /// <summary>
        /// 禁止使用的物品ID字符串
        /// </summary>
        private string DisableGoodsIDs = "";

        /// <summary>
        /// 固定给予的奖励物品的ID列表
        /// </summary>
        private List<GoodsData> GiveAwardsGoodsDataList = null;

        /// <summary>
        /// 多长时间加一次经验给玩家
        /// </summary>
        private int AddExpSecs = 60;

        /// <summary>
        /// 多长时间通知阵营积分信息给玩家
        /// </summary>
        private int NotifyBattleKilledNumSecs = 30;

        /// <summary>
        /// 等待入场的时间
        /// </summary>
        private int WaitingEnterSecs = 30;
        
        /// <summary>
        /// 准备战斗的时间
        /// </summary>
        private int PrepareSecs = 30;

        /// <summary>
        /// 战斗的时间
        /// </summary>
        private int FightingSecs = 300;

        /// <summary>
        /// 清空玩家的时间
        /// </summary>
        private int ClearRolesSecs = 30;

        /// <summary>
        /// 最小转生级别要求 MU新增 [12/23/2013 LiaoWei]
        /// </summary>
        private int m_NeedMinChangeLev = 0;

        /// <summary>
        /// 当前战场最高积分 MU新增 [12/23/2013 LiaoWei]
        /// </summary>
        private static int m_BattleMaxPoint = 0;

        /// <summary>
        /// 当前战场最高积分人名 MU新增 [12/23/2013 LiaoWei]
        /// </summary>
        private static string m_BattleMaxPointName = "";

        /// <summary>
        /// 现在战场最高积分 MU新增 [12/23/2013 LiaoWei]
        /// </summary>
        private static int m_BattleMaxPointNow = 0;

        /// <summary>
        /// 推送日期ID       MU新增 [04/24/2013 LiaoWei]
        /// /// </summary>
        private static int m_nPushMsgDayID = -1;


        /// <summary>
        /// 举行的线路
        /// </summary>
        private int BattleLineID = 1;

        public static SystemXmlItems systemBattleAwardMgr = null;

        #endregion 配置变量定义

        #region 运行状态变量


        /// <summary>
        /// 是否战斗中
        /// </summary>
        private BattleStates BattlingState = BattleStates.NoBattle;

        /// <summary>
        /// 不同状态的开始时间
        /// </summary>
        private long StateStartTicks = 0;

        private object RolePointMutex = new object();
        private BattlePointInfo[] TopPointList = new BattlePointInfo[6];
        private Dictionary<int, BattlePointInfo> RolePointDict = new Dictionary<int, BattlePointInfo>(); //lock优先级高于TopPointList

        /// <summary>
        /// 外部获取战斗状态
        /// </summary>
        /// <returns></returns>
        public int GetBattlingState()
        {
            return (int)BattlingState;
        }

        /// <summary>
        /// 外部获取剩余时间
        /// </summary>
        /// <returns></returns>
        public int GetBattlingLeftSecs()
        {
            long ticks = TimeUtil.NOW();
            int paramSecs = 0;
            if (BattlingState == BattleStates.PublishMsg) //广播大乱斗的消息给在线用户
            {
                paramSecs = WaitingEnterSecs;
            }
            else if (BattlingState == BattleStates.WaitingFight) //等待战斗倒计时(此时禁止新用户进入, 此时伤害无效)
            {
                paramSecs = PrepareSecs;
            }
            else if (BattlingState == BattleStates.StartFight) //开始战斗(倒计时中)
            {
                paramSecs = FightingSecs;
            }
            else if (BattlingState == BattleStates.EndFight) //结束战斗(此时伤害无效)
            {
                paramSecs = ClearRolesSecs;
            }
            else if (BattlingState == BattleStates.ClearBattle) //清空战斗场景
            {
                paramSecs = ClearRolesSecs;
            }

            return (int)(((paramSecs * 1000) - (ticks - StateStartTicks)) / 1000);
        }

        /// <summary>
        /// 上次增加经验的时间
        /// </summary>
        private long LastAddBattleExpTicks = 0;

        /// <summary>
        /// 上次广播阵营战斗积分的时间
        /// </summary>
        private long LastNotifyBattleKilledNumTicks = 0; 

        #endregion 运行状态变量

        #region 初始化

        /// <summary>
        /// 加载参数(4字节赋值，不考虑线程安全)
        /// </summary>
        public void LoadParams()
        {
            SystemXmlItem systemBattle = null;
            if (!GameManager.SystemBattle.SystemXmlItemDict.TryGetValue(1, out systemBattle))
            {
                return;
            }

            List<string> timePointsList = new List<string>();
            string[] fields = null;
            string timePoints = systemBattle.GetStringValue("TimePoints");
            if (null != timePoints && timePoints != "")
            {
                fields = timePoints.Split(',');
                for (int i = 0; i < fields.Length; i++)
                {
                    timePointsList.Add(fields[i].Trim());
                }
            }

            TimePointsList = timePointsList;

            MapCode = systemBattle.GetIntValue("MapCode");
            MinLevel = systemBattle.GetIntValue("MinLevel");
            MinRequestNum = systemBattle.GetIntValue("MinRequestNum");
            MaxEnterNum = systemBattle.GetIntValue("MaxEnterNum");
            FallGiftNum = systemBattle.GetIntValue("FallGiftNum");
            FallID = systemBattle.GetIntValue("FallID");
            DisableGoodsIDs = systemBattle.GetStringValue("DisableGoodsIDs");
            AddExpSecs = systemBattle.GetIntValue("AddExpSecs");

            //20秒到 100秒之间
            NotifyBattleKilledNumSecs = Global.GMax(5, Global.GMin(100, systemBattle.GetIntValue("NotifyBattleKilledNumSecs")));

            WaitingEnterSecs = systemBattle.GetIntValue("WaitingEnterSecs"); ;
            PrepareSecs = systemBattle.GetIntValue("PrepareSecs"); ;
            FightingSecs = systemBattle.GetIntValue("FightingSecs"); ;
            ClearRolesSecs = systemBattle.GetIntValue("ClearRolesSecs");
            m_NeedMinChangeLev = systemBattle.GetIntValue("MinZhuanSheng");
            BattleLineID = Global.GMax(1, systemBattle.GetIntValue("LineID"));

            ReloadGiveAwardsGoodsDataList(systemBattle);

            Global.QueryDayActivityTotalPointInfoToDB(SpecialActivityTypes.CampBattle);

            PushMsgDayID = Global.SafeConvertToInt32(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.BattlePushMsgDayID));
        }

        public void ReloadGiveAwardsGoodsDataList(SystemXmlItem systemBattle = null)
        {
            if (null == systemBattle)
            {
                if (!GameManager.SystemBattle.SystemXmlItemDict.TryGetValue(1, out systemBattle))
                {
                    return;
                }
            }

            List<GoodsData> goodsDataList = new List<GoodsData>();
            string giveGoodsIDs = systemBattle.GetStringValue("GiveGoodsIDs").Trim();
            string[] fields = giveGoodsIDs.Split(',');
            if (null != fields && fields.Length > 0)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (string.IsNullOrEmpty(fields[i].Trim()))
                    {
                        continue;
                    }

                    int goodsID = Convert.ToInt32(fields[i].Trim());
                    SystemXmlItem systemGoods = null;
                    if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsID, out systemGoods))
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("角斗场配置文件中，配置的固定物品奖励中的物品不存在, GoodsID={0}", goodsID));
                        continue;
                    }

                    GoodsData goodsData = new GoodsData()
                    {
                        Id = -1,
                        GoodsID = goodsID,
                        Using = 0,
                        Forge_level = 0,
                        Starttime = "1900-01-01 12:00:00",
                        Endtime = Global.ConstGoodsEndTime,
                        Site = 0,
                        Quality = (int)GoodsQuality.White,
                        Props = "",
                        GCount = 1,
                        Binding = 0,
                        Jewellist = "",
                        BagIndex = 0,
                        AddPropIndex = 0,
                        BornIndex = 0,
                        Lucky = 0,
                        Strong = 0,
                        ExcellenceInfo = 0,
                        AppendPropLev = 0,
                        ChangeLifeLevForEquip = 0,
                    };

                    goodsDataList.Add(goodsData);
                }
            }

            GiveAwardsGoodsDataList = goodsDataList;
        }

        /// <summary>
        /// 初始化大乱斗
        /// </summary>
        public void Init()
        {
            //加载参数
            LoadParams();
            
            ////////////////////////////////////////////////////////////////

            //是否允许攻击
            AllowAttack = false;

            //战斗开始前的人数
            StartRoleNum = 0;

            //当前总的在线用户数
            TotalClientCount = 0;

            //已经杀死的角色的人数
            AllKilledRoleNum = 0;

            //
        }

        #endregion 初始化

        #region 处理方法

        /// <summary>
        /// 调度和管理大乱斗场景
        /// </summary>
        public void Process()
        {
            //是否战斗中
            if (BattlingState > BattleStates.NoBattle)
            {
                //处理正在战斗的过程
                ProcessBattling();
            }
            else
            {
                //处理非战斗期间的逻辑
                ProcessNoBattle();
            }
        }

        /// <summary>
        /// 处理正在战斗的过程
        /// </summary>
        private void ProcessBattling()
        {
            if (BattlingState == BattleStates.PublishMsg) //广播大乱斗的消息给在线用户
            {
                // 消息推送
                int nNow = TimeUtil.NowDateTime().DayOfYear;

                if (PushMsgDayID != nNow)
                {
                    //Global.DayActivityTiggerPushMessage((int)SpecialActivityTypes.CampBattle);

                    Global.UpdateDBGameConfigg(GameConfigNames.BattlePushMsgDayID, nNow.ToString());

                    PushMsgDayID = nNow;
                }

                //判断如果超过了最大等待时间， 先禁止进入大乱斗
                long ticks = TimeUtil.NOW();
                if (ticks >= (StateStartTicks + (WaitingEnterSecs * 1000)))
                {
                    //向大乱斗场景内的角色发送准备战斗倒计时消息
                    //发送广播消息
                    GameManager.ClientMgr.NotifyBattleInviteMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, MapCode, (int)BattleCmds.Time, (int)BattleStates.WaitingFight, PrepareSecs);

                    BattlingState = BattleStates.WaitingFight;
                    StateStartTicks = TimeUtil.NOW();
                }
            }
            else if (BattlingState == BattleStates.WaitingFight) //等待战斗倒计时(此时禁止新用户进入, 此时伤害无效)
            {
                //如果超过了最大倒计时时间，允许角色进入战斗伤害
                long ticks = TimeUtil.NOW();
                if (ticks >= (StateStartTicks + (PrepareSecs * 1000)))
                {
                    //是否允许攻击
                    AllowAttack = true;

                    //向大乱斗场景内的角色发送战斗倒计时消息
                    //发送广播消息
                    GameManager.ClientMgr.NotifyBattleInviteMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, MapCode, (int)BattleCmds.Time, (int)BattleStates.StartFight, FightingSecs);

                    //开始时角色的人数
                    StartRoleNum = GameManager.ClientMgr.GetMapClientsCount(MapCode);

                    BattlingState = BattleStates.StartFight;
                    StateStartTicks = TimeUtil.NOW();

                    //上次增加经验的时间
                    LastAddBattleExpTicks = StateStartTicks;

                    //上次阵营积分通知的时间
                    LastNotifyBattleKilledNumTicks = StateStartTicks;

                    //隋营和唐营最后一次杀敌的时间
                    _SuiLastKillEmemyTime = TimeUtil.NOW() * 10000;
                    _TangLastKillEmemyTime = _SuiLastKillEmemyTime;
                }
            }
            else if (BattlingState == BattleStates.StartFight) //开始战斗(倒计时中)
            {
                //如果超过了最大倒计时时间，允许角色进入战斗伤害
                long ticks = TimeUtil.NOW();
                if (ticks >= (StateStartTicks + (FightingSecs * 1000)))
                {
                    //是否允许攻击
                    AllowAttack = false;

                    BattlingState = BattleStates.EndFight;
                    StateStartTicks = TimeUtil.NOW();
                }
                else
                {
                    //定时给在场的玩家增加经验
                    ProcessTimeAddRoleExp();

                    //定时通知在场的玩家阵营积分信息
                    ProcessTimeNotifyBattleKilledNum();
                }
                //else if (ticks >= (StateStartTicks + (60 * 1 * 1000))) //如果超过了1分钟，角斗场中只剩余一个人了
                //{
                //    //现在角色的人数
                //    int roleNum = GameManager.ClientMgr.GetMapClientsCount(MapCode);
                //    if (roleNum <= 1)
                //    {
                //        //是否允许攻击
                //        AllowAttack = false;

                //        BattlingState = BattleStates.EndFight;
                //        StateStartTicks = TimeUtil.NOW();
                //    }
                //}
            }
            else if (BattlingState == BattleStates.EndFight) //结束战斗(此时伤害无效)
            {
                //先配置，再处理其他的
                BattlingState = BattleStates.ClearBattle;
                StateStartTicks = TimeUtil.NOW();

                //向大乱斗场景内的角色发送战斗清场倒计时消息
                //发送广播消息
                GameManager.ClientMgr.NotifyBattleInviteMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, MapCode, (int)BattleCmds.Time, (int)BattleStates.ClearBattle, ClearRolesSecs);
                
                //开始计算给予的奖励
                /// 处理大乱斗结束时的奖励
                //ProcessBattleResultAwards();
                ProcessBattleResultAwards2();
            }
            else if (BattlingState == BattleStates.ClearBattle) //清空战斗场景
            {
                //如果超过了最大倒计时时间，允许角色进入战斗伤害
                long ticks = TimeUtil.NOW();
                if (ticks >= (StateStartTicks + (ClearRolesSecs * 1000)))
                {
                    //强迫将所有逗留的用户强制传送出去
                    GameManager.ClientMgr.NotifyBattleLeaveMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, MapCode);

                    BattlingState = BattleStates.NoBattle;
                    StateStartTicks = 0;

                    //清除所有记忆信息
                    ClearAllRoleLeaveInfo();
                    ClearAllRolePointInfo();
                }
            }
        }

        /// <summary>
        /// 处理非战斗期间的逻辑
        /// </summary>
        private void ProcessNoBattle()
        {
            //判断是否要开始大乱斗
            if (!JugeStartBattle())
            {
                return;
            }

            //当前总的在线人数是否够？
            //if (GameManager.ClientMgr.GetClientCount() < MinRequestNum)
            //{
            //    return;
            //}

            //战斗开始前的人数
            StartRoleNum = 0;

            //当前总的在线用户数
            TotalClientCount = 0;

            //已经杀死的角色的人数
            AllKilledRoleNum = 0;

            //当前总的隋营用户数
            SuiClientCount = 0;

            //当前总的唐营用户数
            TangClientCount = 0;

            //隋军阵营的积分
            SuiKilledNum = 0;

            //唐军阵营的积分
            TangKilledNum = 0;

            //广播大乱斗的消息给在线用户
            BattlingState = BattleStates.PublishMsg;

            //发送广播消息
            GameManager.ClientMgr.NotifyAllBattleInviteMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, MinLevel, (int)BattleCmds.Invite, (int)BattleStates.PublishMsg, WaitingEnterSecs);

            // 不同状态的开始时间
            StateStartTicks = TimeUtil.NOW();
        }

        /// <summary>
        /// 判断是否需要立刻开始大乱斗
        /// </summary>
        /// <returns></returns>
        private bool JugeStartBattle()
        {
            string nowTime = TimeUtil.NowDateTime().ToString("HH:mm");
            List<string> timePointsList = TimePointsList;
            if (null == timePointsList) return false;

            for (int i = 0; i < timePointsList.Count; i++)
            {
                if (timePointsList[i] == nowTime)
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// 剩余次数
        /// </summary>
        /// <returns></returns>
        public int LeftEnterCount()
        {
            int count = 0;
            string nowTime = TimeUtil.NowDateTime().ToString("HH:mm");
            List<string> timePointsList = TimePointsList;
            if (null == timePointsList) return 0;
            try
            {
                for (int i = 0; i < timePointsList.Count; i++)
                {
                    DateTime tt = DateTime.Parse(timePointsList[i]);
                    tt.AddMinutes(ClearRolesSecs);
                    if (tt >= TimeUtil.NowDateTime())
                    {
                        count += 1;
                    }
                }
            }
            catch(Exception e)
            {
                LogManager.WriteException(e.ToString());
            }
            return count;
        }

        #endregion 处理方法

        #region 供外部访问的线程安全变量和方法

        /// <summary>
        /// 供外部访问的线程锁对象
        /// </summary>
        private object mutex = new object();

        /// <summary>
        /// 外部使用的锁
        /// </summary>
        public object ExternalMutex
        {
            get { return mutex; }
        }

        /// <summary>
        /// 大乱斗的场景编号
        /// </summary>
        public int BattleMapCode
        {
            get { return MapCode; }
        }

        /// <summary>
        /// 大乱斗的连续编号
        /// </summary>
        public int BattleServerLineID
        {
            get { return BattleLineID; }
        }

        /// <summary>
        /// 是否允许了进入场景
        /// </summary>
        public bool AllowEnterMap
        {
            get
            {
                return (BattlingState >= BattleStates.PublishMsg && BattlingState < BattleStates.EndFight);
            }
        }

        private bool _AllowAttack = false;

        /// <summary>
        /// 是否允许攻击
        /// </summary>
        public bool AllowAttack
        {
            get { lock (mutex) { return _AllowAttack; } }
            set { lock (mutex) { _AllowAttack = value; } }
        }

        /// <summary>
        /// 允许的最低级别
        /// </summary>
        public int AllowMinLevel
        {
            get { return MinLevel; }
        }

        private int _TotalClientCount = 0;

        /// <summary>
        /// 当前总的在线用户数
        /// </summary>
        public int TotalClientCount
        {
            get { lock (mutex) { return _TotalClientCount; } }
            set { lock (mutex) { _TotalClientCount = value; } }
        }

        public int NeedMinChangeLev
        {
            get { return m_NeedMinChangeLev; }
        }

        public static int BattleMaxPoint
        {
            get { return m_BattleMaxPoint; }
            set { m_BattleMaxPoint = value; }
        }

        public static string BattleMaxPointName
        {
            get { return m_BattleMaxPointName; }
            set { m_BattleMaxPointName = value; }
        }

        public static int BattleMaxPointNow
        {
            get { return m_BattleMaxPointNow; }
            set { m_BattleMaxPointNow = value; }
        }

        public static int PushMsgDayID
        {
            get { return m_nPushMsgDayID; }
            set { m_nPushMsgDayID = value; }
        }
        
        
        public static void SetTotalPointInfo(string sName, int nValue)
        {
            BattleMaxPointName = sName;
            BattleMaxPoint = nValue;
        }

        private void ClearAllRolePointInfo()
        {
            lock (RolePointMutex)
            {
                RolePointDict.Clear();
                for (int i = 0; i < TopPointList.Length; i++ )
                {
                    TopPointList[i] = null;
                }
            }
        }

        public void UpdateRolePointInfo(GameClient client)
        {
            int roleID = client.ClientData.RoleID;
            int rolePoint = client.ClientData.BattleKilledNum;
            List<RoleDamage> top5PointArray = null;
            BattlePointInfo pointInfo = null;
            bool needSend = false;
            lock (RolePointMutex)
            {
                if (RolePointDict.TryGetValue(roleID, out pointInfo))
                {
                    pointInfo.m_DamagePoint = rolePoint;
                }
                else
                {
                    pointInfo = new BattlePointInfo();
                    pointInfo.m_RoleID = roleID;
                    pointInfo.m_RoleName = Global.FormatRoleName4(client);
                    pointInfo.m_DamagePoint = rolePoint;
                    RolePointDict[roleID] = pointInfo;
                }

                if (pointInfo.CompareTo(TopPointList[4]) < 0)
                {
                    if (pointInfo.Ranking < 0)
                    {
                        TopPointList[5] = pointInfo;
                    }
                    Array.Sort(TopPointList, pointInfo.Compare);
                    if (null != TopPointList[5])
                    {
                        TopPointList[5].Ranking = -1;
                    }
                    needSend = true;
                }
                if (pointInfo.Side != client.ClientData.BattleWhichSide)
                {
                    pointInfo.Side = client.ClientData.BattleWhichSide;
                    needSend = true;
                }
                if (needSend)
                {
                    top5PointArray = new List<RoleDamage>(ConstTopPointNumber);
                    for (int i = 0; null != TopPointList[i] && i < ConstTopPointNumber; i++)
                    {
                        TopPointList[i].Ranking = i;
                        top5PointArray.Add(new RoleDamage(TopPointList[i].m_RoleID, TopPointList[i].m_DamagePoint, TopPointList[i].m_RoleName, TopPointList[i].Side));
                    }
                }
            }

            if (needSend)
            {
                List<GameClient> clientList = GameManager.ClientMgr.GetMapGameClients(MapCode);
                foreach (var c in clientList)
                {
                    c.sendCmd<List<RoleDamage>>((int)TCPGameServerCmds.CMD_SPR_BATTLE_SCORE_LIST, top5PointArray);
                }
            }
        }

        public void SendScoreInfoListToClient(GameClient client)
        {
            int roleID = client.ClientData.RoleID;
            List<RoleDamage> top5PointArray = new List<RoleDamage>(ConstTopPointNumber);

            lock (RolePointMutex)
            {
                for (int i = 0; null != TopPointList[i] && i < ConstTopPointNumber; i++)
                {
                    top5PointArray.Add(new RoleDamage(TopPointList[i].m_RoleID, TopPointList[i].m_DamagePoint, TopPointList[i].m_RoleName, TopPointList[i].Side));
                }
            }

            if (null != top5PointArray)
            {
                client.sendCmd<List<RoleDamage>>((int)TCPGameServerCmds.CMD_SPR_BATTLE_SCORE_LIST, top5PointArray);
            }
        }

        /// <summary>
        /// 客户端进入地图
        /// </summary>
        /// <returns></returns>
        public bool ClientEnter()
        {
            bool ret = false;
            lock (mutex)
            {
                //当前总的在线用户数
                if (_TotalClientCount < MaxEnterNum)
                {
                    _TotalClientCount++;
                    ret = true;
                }
            }

            return ret;
        }

        /// <summary>
        /// 客户端离开地图
        /// </summary>
        /// <returns></returns>
        public void ClientLeave()
        {
            lock (mutex)
            {
                //当前总的在线用户数
                _TotalClientCount--;
            }
        }

        /// <summary>
        /// 禁止使用的物品ID字符串
        /// </summary>
        public string BattleDisableGoodsIDs
        {
            get { return DisableGoodsIDs; }
        }

        /// <summary>
        /// 战斗开始前的人数
        /// </summary>
        private int _StartRoleNum = 0;

        /// <summary>
        /// 战斗开始前的人数
        /// </summary>
        public int StartRoleNum
        {
            get { lock (mutex) { return _StartRoleNum; } }
            set { lock (mutex) { _StartRoleNum = value; } }
        }

        /// <summary>
        /// 已经死亡的角色的人数
        /// </summary>
        private int _AllKilledRoleNum = 0;

        /// <summary>
        /// 已经死亡的角色的人数
        /// </summary>
        public int AllKilledRoleNum
        {
            get { lock (mutex) { return _AllKilledRoleNum; } }
            set { lock (mutex) { _AllKilledRoleNum = value; } }
        }

        /// <summary>
        /// 隋军阵营的上一次杀敌时间
        /// </summary>
        private long _SuiLastKillEmemyTime = 0;

        /// <summary>
        /// 隋军阵营的积分
        /// </summary>
        private int _SuiKilledNum = 0;

        /// <summary>
        /// 隋军阵营的积分
        /// </summary>
        public int SuiKilledNum
        {
            get { lock (mutex) { return _SuiKilledNum; } }
            set 
            { 
                lock (mutex) 
                {
                    //如果原有数据小，表示新增了积分，新杀了敌人
                    _SuiLastKillEmemyTime = TimeUtil.NOW() * 10000;
                    _SuiKilledNum = value; 
                }
            }
        }

        /// <summary>
        /// 唐军阵营的上一次杀敌时间
        /// </summary>
        private long _TangLastKillEmemyTime = 0;

        /// <summary>
        /// 唐军阵营的积分
        /// </summary>
        private int _TangKilledNum = 0;

        /// <summary>
        /// 唐军阵营的积分
        /// </summary>
        public int TangKilledNum
        {
            get { lock (mutex) { return _TangKilledNum; } }
            set 
            { 
                lock (mutex) 
                {
                    //如果原有数据小，表示新增了积分，新杀了敌人
                    _TangLastKillEmemyTime = TimeUtil.NOW() * 10000;
                    _TangKilledNum = value; 
                } 
            }
        }

        private int _SuiClientCount = 0;

        /// <summary>
        /// 当前总的隋营用户数
        /// </summary>
        public int SuiClientCount
        {
            get { lock (mutex) { return _SuiClientCount; } }
            set { lock (mutex) { _SuiClientCount = value; } }
        }

        private int _TangClientCount = 0;

        /// <summary>
        /// 当前总的唐营用户数
        /// </summary>
        public int TangClientCount
        {
            get { lock (mutex) { return _TangClientCount; } }
            set { lock (mutex) { _TangClientCount = value; } }
        }

        #endregion 供外部访问的线程安全变量和方法

        #region 奖励处理

        /// <summary>
        /// 根据级别奖励的经验数组
        /// </summary>
        private long[] BattleExpByLevels = null;

        /// <summary>
        /// 清空缓存的经验
        /// </summary>
        public void ClearBattleExpByLevels()
        {
            BattleExpByLevels = null;
        }

        /// <summary>
        /// 根据角色的级别加载经验
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private long GetBattleExpByLevel(GameClient client, int level)
        {
            long [] expByLevels = BattleExpByLevels;
            if (null == expByLevels)
            {
                SystemXmlItem systemXmlItem = null;
                expByLevels = new long[Data.LevelUpExperienceList.Length - 1];
                for (int i = 0; i < expByLevels.Length; i++)
                {
                    if (GameManager.systemBattleExpMgr.SystemXmlItemDict.TryGetValue(i + 1, out systemXmlItem))
                    {
                        expByLevels[i] = Global.GMax(0, systemXmlItem.GetIntValue("Experience"));
                    }
                }

                BattleExpByLevels = expByLevels;
            }

            int index = level - 1;

            if (index < 0 || index >= BattleExpByLevels.Length)
            {
                return 0;
            }

            double nRate = 0;
            int nChangeLev = client.ClientData.ChangeLifeCount;

            if (nChangeLev == 0)
                nRate = 1;
            else
                nRate = Data.ChangeLifeEverydayExpRate[nChangeLev];

            return (int)(expByLevels[index] * nRate);
        }

        #region 配置和处理战斗结束时的积分和经验奖励

        //积分和经验奖励配置数据类
        protected class Award
        {
            public int MinJiFen = 0;//最小积分
            public int MaxJiFen = 200;//最大积分 
            public int ExpXiShu = 5000;// 经验系数
            public double MoJingXiShu = 4;// 绑定魔晶系数
            public double ChengJiuXiShu = 1;// 绑定元宝系数
            public int MinExp = 3000;// 最小经验
            public int MaxExp = 600000;//最大经验
            public int MinMoJing = 10;//最小魔晶
            public int MaxMoJing = 20;//最大魔晶
            public int MinChengJiu = 100;//最小成就
            public int MaxChengJiu = 200;//最大成就
        }

        /// <summary>
        /// 根据积分奖励的配置信息
        /// </summary>
        private List<Award> _BattleAwardByScore = null;

        /// <summary>
        /// 根据积分奖励的配置信息
        /// </summary>
        private List<Award> BattleAwardByScore
        {
            set
            {
                _BattleAwardByScore = value;
            }

            get
            {
                List<Award> awardByScore = _BattleAwardByScore;
                if (null == awardByScore)
                {
                    //重新初始化积分奖励配置缓存信息
                    awardByScore = new List<Award>();
                    foreach (var val in GameManager.systemBattleAwardMgr.SystemXmlItemDict.Values)
                    {
                        Award award = new Award()
                        {
                            MinJiFen = Math.Max(0, val.GetIntValue("MinJiFen")),
                            MaxJiFen = Math.Max(0, val.GetIntValue("MaxJiFen")),
                            ExpXiShu = Math.Max(0, val.GetIntValue("ExpXiShu")),
                            MoJingXiShu = Math.Max(0, val.GetDoubleValue("MoJingXiShu")),
                            ChengJiuXiShu = Math.Max(0, val.GetDoubleValue("ChengJiuXiShu")),
                            MinExp = Math.Max(0, val.GetIntValue("MinExp")),
                            MaxExp = Math.Max(0, val.GetIntValue("MaxExp")),
                            MinMoJing = Math.Max(0, val.GetIntValue("MinMoJing")),
                            MaxMoJing = Math.Max(0, val.GetIntValue("MaxMoJing")),
                            MinChengJiu = Math.Max(0, val.GetIntValue("MinChengJiu")),
                            MaxChengJiu = Math.Max(0, val.GetIntValue("MaxChengJiu")),
                        };

                        //避免最大积分没配置的情况
                        if (award.MinJiFen > award.MaxJiFen)
                        {
                            award.MaxJiFen = 0xFFFFFFF;//少个F，有符号整数最大范围
                        }

                        awardByScore.Add(award);
                    }

                    _BattleAwardByScore = awardByScore;
                }
                return awardByScore;
            }
        }

        /// <summary>
        /// 清空缓存的积分奖励的配置信息
        /// </summary>
        public void ClearBattleAwardByScore()
        {
            _BattleAwardByScore = null;
        }

        /// <summary>
        /// 胜利方阵营
        /// </summary>
        /// <returns></returns>
        private int GetSuccessSide()
        {
            int successSide = -1;

            if (TangKilledNum > SuiKilledNum)
            {
                //唐=>魔
                successSide = (int)BattleWhichSides.Mo;
            }
            else if (TangKilledNum < SuiKilledNum)
            {
                //隋=>仙
                successSide = (int)BattleWhichSides.Xian;
            }
            else
            {
                //如果双方积分一样，看谁先达到积分谁赢
                if (_SuiLastKillEmemyTime < _TangLastKillEmemyTime)
                {
                    successSide = (int)BattleWhichSides.Xian;//隋军最后一次杀敌时间小，隋军赢
                }
                else if (_SuiLastKillEmemyTime > _TangLastKillEmemyTime)
                {
                    successSide = (int)BattleWhichSides.Mo;//唐军最后一次杀敌时间小，唐军赢
                }
                else
                {
                    //双方积分一样，杀敌时间一样...
                }
            }

            return successSide;
        }

        /// <summary>
        /// 判断是否胜方
        /// </summary>
        /// <param name="client"></param>
        private bool IsSuccessClient(GameClient client)
        {
            int successSide = -1;

            if (TangKilledNum > SuiKilledNum)
            {
                //唐=>魔
                successSide = (int)BattleWhichSides.Mo;
            }
            else if (TangKilledNum < SuiKilledNum)
            {
                //隋=>仙
                successSide = (int)BattleWhichSides.Xian;
            }
            else
            {
                //如果双方积分一样，看谁先达到积分谁赢
                if (_SuiLastKillEmemyTime < _TangLastKillEmemyTime)
                {
                    successSide = (int)BattleWhichSides.Xian;//隋军最后一次杀敌时间小，隋军赢
                }
                else if (_SuiLastKillEmemyTime > _TangLastKillEmemyTime)
                {
                    successSide = (int)BattleWhichSides.Mo;//唐军最后一次杀敌时间小，唐军赢
                }
                else
                {
                    return false;//双方积分一样，杀敌时间一样，都减半
                }
            }

            if (successSide == client.ClientData.BattleWhichSide)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据角色的积分计算并给与经验奖励和阵旗奖励
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private void ProcessRoleBattleExpAndFlagAward(GameClient client, int successSide, int paiMing)
        {
            List<Award> awardByScore = BattleAwardByScore;
            if (null == awardByScore)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("处理大乱斗结束奖励时, 奖励列表项未空"));

                //记录日志
                return;
            }

            double expAward = 0;
            double MoJingAward = 0;
            double chengJiuAward = 0;
            AwardsItemList awardsItemList = new AwardsItemList();
            bool successed = (successSide == client.ClientData.BattleWhichSide);

            double awardmuti = 1.0;

            // 合服活动的多倍处理
            HeFuAwardTimesActivity hefuact = HuodongCachingMgr.GetHeFuAwardTimesActivity();
            if (null != hefuact && hefuact.InActivityTime())
            {
                awardmuti += ((double)hefuact.activityTimes - 1);
            }

            // 节日活动的多倍处理
            JieRiMultAwardActivity jieriact = HuodongCachingMgr.GetJieRiMultAwardActivity();
            if (null != jieriact)
            {
                JieRiMultConfig config = jieriact.GetConfig((int)MultActivityType.CampBattle);
                if (null != config)
                {
                    awardmuti += config.GetMult();
                }
            }

            //计算奖励
            foreach (var award in awardByScore)
            {
                //当积分在某规则的最小积分和最大积分范围内时，根据相应规则计算积分奖励
                if (client.ClientData.BattleKilledNum >= award.MinJiFen && client.ClientData.BattleKilledNum < award.MaxJiFen)
                {
                    //计算经验奖励，积分乘以系数
                    expAward = client.ClientData.BattleKilledNum * award.ExpXiShu;

                    //计算绑定元宝奖励,用积分除以系数，得到
                    if (award.MoJingXiShu > 0)
                    {
                        MoJingAward = (int)(client.ClientData.BattleKilledNum * award.MoJingXiShu);
                    }

                    //计算成就奖励,用积分除以系数，得到
                    if (award.ChengJiuXiShu > 0)
                    {
                        chengJiuAward = (int)(client.ClientData.BattleKilledNum * award.ChengJiuXiShu);
                    }

                    //败方奖励衰减
                    if (!successed)
                    {
                        if (expAward > 0)
                        {
                            expAward = expAward * 0.8;
                        }

                        if (MoJingAward > 0)
                        {
                            MoJingAward = MoJingAward * 0.8;
                        }

                        if (chengJiuAward > 0)
                        {
                            chengJiuAward = chengJiuAward * 0.8;
                        }
                    }

                    //经验奖励必须大于等于MinExp， 且小于等于 MaxExp
                    expAward = (long)(expAward * Data.ChangeLifeEverydayExpRate[client.ClientData.ChangeLifeCount]);
                    expAward = Math.Max(expAward, award.MinExp * Data.ChangeLifeEverydayExpRate[client.ClientData.ChangeLifeCount]);
                    expAward = Math.Min(expAward, award.MaxExp * Data.ChangeLifeEverydayExpRate[client.ClientData.ChangeLifeCount]);

                    //绑定元宝奖励必须大于等于MinBindYuanBao， 且小于等于 MaxBindYuanBao
                    MoJingAward = Math.Max(MoJingAward, award.MinMoJing);
                    MoJingAward = Math.Min(MoJingAward, award.MaxMoJing);

                    //绑定元宝奖励必须大于等于MinBindYuanBao， 且小于等于 MaxBindYuanBao
                    chengJiuAward = Math.Max(chengJiuAward, award.MinChengJiu);
                    chengJiuAward = Math.Min(chengJiuAward, award.MaxChengJiu);

                    if (expAward > 0)
                    {
                        expAward = (int)(expAward * awardmuti);
                    }
                    if (MoJingAward > 0)
                    {
                        MoJingAward = (int)(MoJingAward * awardmuti);
                    }
                    if (chengJiuAward > 0)
                    {
                        chengJiuAward = (int)(chengJiuAward * awardmuti);
                    }

                    //这样计算出来的结果，即使战败方，其得到的奖励也可能是满奖励
                    break;
                }
            }

            foreach (var xml in GameManager.SystemBattlePaiMingAwards.SystemXmlItemDict.Values)
            {
                if (null == xml) continue;
                int min = xml.GetIntValue("MinPaiMing") - 1;
                int max = xml.GetIntValue("MaxPaiMing") - 1;
                if (paiMing >= min && paiMing <= max)
                {
                    awardsItemList.AddNoRepeat(xml.GetStringValue("Goods"));
                }
            }

            if (expAward > 0)
            {
                //处理角色经验
                GameManager.ClientMgr.ProcessRoleExperience(client, (long)expAward, true, false);
            }

            //奖励魔晶
            if (MoJingAward > 0)
            {
                GameManager.ClientMgr.ModifyTianDiJingYuanValue(client, (int)MoJingAward, "阵营战", false, true);
            }

            //成就奖励
            if (chengJiuAward > 0)
            {
                GameManager.ClientMgr.ModifyChengJiuPointsValue(client, (int)chengJiuAward, "阵营战");
            }

            List<GoodsData> goodsDataList = Global.ConvertToGoodsDataList(awardsItemList.Items);
            // 如果没有背包格子 则发邮件  说明 -- 如果不是一键完成每日跑环任务 是不会执行这段代码的 因为 其他任务的提交提交都会检测包裹 [12/6/2013 LiaoWei]
            if (!Global.CanAddGoodsDataList(client, goodsDataList))
            {
                GameManager.ClientMgr.SendMailWhenPacketFull(client, goodsDataList, Global.GetLang("阵营战排名奖励"), string.Format(Global.GetLang("恭喜您在本次阵营战中获得第{0}名，奖励已发放到附件中，请及时领取。"), paiMing + 1));
            }
            else
            {
                for (int i = 0; i < goodsDataList.Count; i++)
                {
                    //向DBServer请求加入某个新的物品到背包中
                    Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsDataList[i].GoodsID, goodsDataList[i].GCount, goodsDataList[i].Quality, "", goodsDataList[i].Forge_level, goodsDataList[i].Binding, 0, "", true, 1, "阵营战排名奖励", Global.ConstGoodsEndTime,
                                                0, goodsDataList[i].BornIndex, goodsDataList[i].Lucky, 0, goodsDataList[i].ExcellenceInfo, goodsDataList[i].AppendPropLev);
                }
            }

            GameManager.ClientMgr.NotifySelfSuiTangBattleAward(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, TangKilledNum,
                                                                    SuiKilledNum, (long)expAward, (int)MoJingAward, (int)chengJiuAward, successed, paiMing, awardsItemList.ToString());
        }

        /// <summary>
        /// 处理大乱斗结束时的奖励
        /// </summary>
        private void ProcessBattleResultAwards()
        {
            //先找出当前大乱斗中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(MapCode);
            if (null == objsList) return;

            int successSide = GetSuccessSide();
            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                //if (c.ClientData.CurrentLifeV <= 0) continue;

                /// 处理用户的经验奖励和旗帜奖励
                ProcessRoleBattleExpAndFlagAward(c, successSide, i);
            }

            /// 处理大乱斗结束时的物品掉落
            GameManager.GoodsPackMgr.ProcessBattle(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                objsList, null, 0, 0);
        }

        /// <summary>
        /// 处理大乱斗结束时的奖励新版 2014-7-22
        /// </summary>
        private void ProcessBattleResultAwards2()
        {
            //先找出当前大乱斗中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(MapCode);
            if (null == objsList) return;

            int successSide = GetSuccessSide();
            List<GameClient> clientList = new List<GameClient>();
            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                //if (c.ClientData.CurrentLifeV <= 0) continue;
                clientList.Add(c);
            }
            //从大到小进行排序
            clientList.Sort((x, y) => { return y.ClientData.BattleKilledNum - x.ClientData.BattleKilledNum; });
            for (int i = 0; i < clientList.Count; i++ )
            {
                /// 处理用户的经验奖励和旗帜奖励
                ProcessRoleBattleExpAndFlagAward(clientList[i], successSide, i);
            }

            /// 处理大乱斗结束时的物品掉落
            GameManager.GoodsPackMgr.ProcessBattle(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                objsList, null, 0, 0);
        }

        #endregion  配置和处理战斗结束时的积分和经验奖励

        /// <summary>
        /// 获取角色的奖励经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        //private int GetExperienceAwards(GameClient client)
        //{
        //    //这是什么公式，faint
        //    double exprience = (Math.Pow(client.ClientData.Level, 1.5) + Math.Pow(client.ClientData.Level, 0.4) + 2) * (5 * client.ClientData.Level + 10 * client.ClientData.Level) * 0.06;
        //    return (int)exprience;
        //}

        /// <summary>
        /// 获取角色的金钱奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        //private int GetMoneyAwards(GameClient client)
        //{
        //    return (client.ClientData.Level * 457);
        //}

        /// <summary>
        /// 处理用户的经验奖励
        /// </summary>
        /// <param name="client"></param>
        //private void ProcessRoleExperienceAwards(GameClient client)
        //{
        //    //奖励用户经验
        //    //异步写数据库，写入经验和级别
        //    int experience = GetExperienceAwards(client);

        //    //处理角色经验
        //    GameManager.ClientMgr.ProcessRoleExperience(client, experience, true, false);
        //}

        /// <summary>
        /// 处理用户的金钱奖励
        /// </summary>
        /// <param name="client"></param>
        //private void ProcessRoleMoneyAwards(GameClient client)
        //{
        //    //奖励用户金钱
        //    //异步写数据库，写入金钱
        //    int money = GetMoneyAwards(client);
        //    if (money > 0)
        //    {
        //        //更新用户的铜钱
        //        GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, money, false);

        //        GameManager.SystemServerEvents.AddEvent(string.Format("角色获取金钱, roleID={0}({1}), Money={2}, newMoney={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Money1, money), EventLevels.Record);
        //    }
        //}

        /// <summary>
        /// 处理大乱斗结束时的奖励
        /// </summary>
        /*private void ProcessBattleAwards()
        {
            //先找出当前大乱斗中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(MapCode);
            if (null == objsList) return;

            //for (int i = 0; i < objsList.Count; i++)
            //{
            //    GameClient c = objsList[i] as GameClient;
            //    if (c == null) continue;
            //    if (c.ClientData.CurrentLifeV <= 0) continue;

            //    /// 处理用户的经验奖励
            //    ProcessRoleExperienceAwards(c);

            //    /// 处理用户的金钱奖励
            //    ProcessRoleMoneyAwards(c);
            //}

            //固定给予的物品列表
            //List<GoodsData> goodsDataList = GiveAwardsGoodsDataList;

            /// 处理大乱斗结束时的物品掉落
            GameManager.GoodsPackMgr.ProcessBattle(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                objsList, null, 0, 0);
        }*/

        /// <summary>
        /// 处理中途被杀死的用户的奖励
        /// </summary>
        /// <param name="client"></param>
        //public void ProcessDeadRoleAwards(GameClient client)
        //{
        //    /// 处理用户的经验奖励
        //    ProcessRoleExperienceAwards(client);
        //}

        #endregion 奖励处理

        #region 记忆角色离线时的积分和时间

        /// <summary>
        /// 角色的积分
        /// </summary>
        private Dictionary<int, int> _RoleLeaveJiFenDict = new Dictionary<int, int>();

        /// <summary>
        /// 角色的阵营ID
        /// </summary>
        private Dictionary<int, int> _RoleLeaveSideDict = new Dictionary<int, int>();

        /// <summary>
        /// 角色的离线的时间
        /// </summary>
        private Dictionary<int, long> _RoleLeaveTicksDict = new Dictionary<int, long>();

        /// <summary>
        /// 清除记忆信息
        /// </summary>
        /// <param name="roleID"></param>
        public void ClearRoleLeaveInfo(int roleID)
        {
            lock (_RoleLeaveJiFenDict)
            {
                _RoleLeaveJiFenDict.Remove(roleID);
            }

            lock (_RoleLeaveTicksDict)
            {
                _RoleLeaveTicksDict.Remove(roleID);
            }

            lock (_RoleLeaveSideDict)
            {
                _RoleLeaveSideDict.Remove(roleID);
            }
        }

        /// <summary>
        /// 清除所有记忆信息
        /// </summary>
        /// <param name="roleID"></param>
        public void ClearAllRoleLeaveInfo()
        {
            lock (_RoleLeaveJiFenDict)
            {
                _RoleLeaveJiFenDict.Clear();
            }

            lock (_RoleLeaveTicksDict)
            {
                _RoleLeaveTicksDict.Clear();
            }

            lock (_RoleLeaveSideDict)
            {
                _RoleLeaveSideDict.Clear();
            }

            m_BattleMaxPointNow = 0;
        }

        /// <summary>
        /// 获取上次离线的时间
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public long GetRoleLeaveTicks(int roleID)
        {
            long ticks = 0;
            lock (_RoleLeaveTicksDict)
            {
                _RoleLeaveTicksDict.TryGetValue(roleID, out ticks);
            }

            return ticks;
        }

        /// <summary>
        /// 获取上次离线的积分
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public int GetRoleLeaveJiFen(int roleID)
        {
            int jiFen = 0;
            lock (_RoleLeaveJiFenDict)
            {
                _RoleLeaveJiFenDict.TryGetValue(roleID, out jiFen);
            }

            return jiFen;
        }

        /// <summary>
        /// 获取上次离线的阵营
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public int GetRoleLeaveSideID(int roleID)
        {
            int sideID = 0;
            lock (_RoleLeaveSideDict)
            {
                _RoleLeaveSideDict.TryGetValue(roleID, out sideID);
            }

            return sideID;
        }

        /// <summary>
        /// 设置上次离线的积分
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public void LeaveBattleMap(GameClient client, bool regLastInfo)
        {
            if (client.ClientData.MapCode != GameManager.BattleMgr.MapCode)
            {
                return;
            }

            //客户端离开地图
            GameManager.BattleMgr.ClientLeave();

            //大乱斗中的阵营ID
            if ((int)BattleWhichSides.Xian == client.ClientData.BattleWhichSide)
            {
                GameManager.BattleMgr.SuiClientCount--;
            }
            else
            {
                GameManager.BattleMgr.TangClientCount--;
            }

            //通知角斗场开始的人数和当前剩余的人数
            //GameManager.ClientMgr.NotifyRoleBattleRoleInfo(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                //GameManager.BattleMgr.BattleMapCode, GameManager.BattleMgr.SuiClientCount, GameManager.BattleMgr.TangClientCount);

            if (!regLastInfo)
            {
                //清除记忆信息
                GameManager.BattleMgr.ClearRoleLeaveInfo(client.ClientData.RoleID);
                return;
            }

            if (GameManager.BattleMgr.GetBattlingState() < (int)BattleStates.PublishMsg ||
                    GameManager.BattleMgr.GetBattlingState() >= (int)BattleStates.EndFight)
            {
                //清除记忆信息
                GameManager.BattleMgr.ClearRoleLeaveInfo(client.ClientData.RoleID);
                return;
            }

            int roleID = client.ClientData.RoleID;
            int jiFen = client.ClientData.BattleKilledNum;
            int sideID = client.ClientData.BattleWhichSide;

            lock (_RoleLeaveJiFenDict)
            {
                _RoleLeaveJiFenDict[roleID] = jiFen;
            }

            long ticks = TimeUtil.NOW();
            lock (_RoleLeaveTicksDict)
            {
                _RoleLeaveTicksDict[roleID] = ticks;
            }

            lock (_RoleLeaveSideDict)
            {
                _RoleLeaveSideDict[roleID] = sideID;
            }

            client.ClientData.BattleWhichSide = 0;
        }

        #endregion 记忆角色离线时的积分和时间

        #region 定时给在场的玩家家经验

        /// 处理用户的在场的时间经验奖励
        private void ProcessAddRoleExperience(GameClient client)
        {
            long exp = GetBattleExpByLevel(client, client.ClientData.Level);
            if (exp <= 0) return;

            JieRiMultAwardActivity jieriact = HuodongCachingMgr.GetJieRiMultAwardActivity();
            if (null != jieriact)
            {
                JieRiMultConfig config = jieriact.GetConfig((int)MultActivityType.CampBattle);
                if (null != config)
                {
                    exp += exp * (long)config.GetMult();
                }
            }

            //处理角色经验
            GameManager.ClientMgr.ProcessRoleExperience(client, exp, true, false);
        }

        /// <summary>
        /// 定时给在场的玩家增加经验
        /// </summary>
        private void ProcessTimeAddRoleExp()
        {
            if (BattlingState != BattleStates.StartFight) //非战斗装备，不加经验
            {
                return;
            }

            long ticks = TimeUtil.NOW();
            if (ticks - LastAddBattleExpTicks < (AddExpSecs * 1000))
            {
                return;
            }

            LastAddBattleExpTicks = ticks;

            //先找出当前大乱斗中的所有的或者的人
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(MapCode);
            if (null == objsList) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                //if (c.ClientData.CurrentLifeV <= 0) continue;

                /// 处理用户的经验奖励
                ProcessAddRoleExperience(c);
            }
        }

        #endregion 定时给在场的玩家家经验

        #region 定时[每隔30秒]通知在场的玩家双方阵营积分信息

        private int LastSuiKilledNum = -1;
        private int LastTangKilledNum = -1;

        /// <summary>
        /// 定时给在场的玩家增加经验
        /// </summary>
        private void ProcessTimeNotifyBattleKilledNum()
        {
            if (BattlingState != BattleStates.StartFight) //非战斗装备，不加经验
            {
                return;
            }

            long ticks = TimeUtil.NOW();
            if (ticks - LastNotifyBattleKilledNumTicks < (NotifyBattleKilledNumSecs * 1000))
            {
                return;
            }

            LastNotifyBattleKilledNumTicks = ticks;

            if (LastSuiKilledNum != SuiKilledNum || LastTangKilledNum != TangKilledNum)
            {
                GameManager.ClientMgr.NotifyBattleKilledNumCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, SuiKilledNum, TangKilledNum);
                LastSuiKilledNum = SuiKilledNum;
                LastTangKilledNum = TangKilledNum;
            }
        }

        #endregion 定时通知在场的玩家双方阵营积分信息

        // add by chenjingui. 20150704 角色改名后，检测是否更新最高积分者
        public void OnChangeName(int roleId, string oldName, string newName)
        {
            if (!string.IsNullOrEmpty(oldName) && !string.IsNullOrEmpty(newName))
            {
                if (!string.IsNullOrEmpty(m_BattleMaxPointName) && m_BattleMaxPointName == oldName)
                {
                    m_BattleMaxPointName = newName;
                }
            }
        }
    }
}
