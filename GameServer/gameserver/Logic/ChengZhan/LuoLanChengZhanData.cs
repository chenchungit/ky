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

namespace GameServer.Logic
{
    public enum LuoLanChengZhanBirthPointTypes
    {
        ShouChengFang = 0,
        GongChengFang1 = 1,
        GongChengFang2 = 2,
        GongChengFang3 = 3,
    }

    /// <summary>
    /// 每日奖励配置项
    /// </summary>
    public class SiegeWarfareEveryDayAwardsItem
    {
        public int ID;
        public int ZhiWu;
        public int DayZhanGong;
        public long DayExp;
        public AwardsItemList DayGoods = new AwardsItemList();
    }

    public class MapBirthPoint
    {
        public int ID;
        public int Type;
        public int MapCode;
        public int BirthPosX;
        public int BirthPosY;
        public int BirthRangeX;
        public int BirthRangeY;
    }

    public class QiZhiConfig : ICloneable
    {
        //静态数据
        public int NPCID;
        public int BufferID;
        public int PosX;
        public int PosY;
        public HashSet<int> UseAuthority = new HashSet<int>();
        public int MonsterId;

        //运行时数据
        public int BattleWhichSide;
        public bool Alive;
        public long DeadTicks;
        public long KillerBhid;
        public long InstallBhid;
        public string InstallBhName;

        public object Clone()
        {
            QiZhiConfig obj = new QiZhiConfig()
            {
                NPCID = NPCID,
                BufferID = BufferID,
                PosX = PosX,
                PosY = PosY,
                UseAuthority = new HashSet<int>(UseAuthority),
                MonsterId = MonsterId,
            };

            return obj;
        }
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class LuoLanChengZhanData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();

        #region 活动配置

        /// <summary>
        /// 罗兰峡谷地图编号
        /// </summary>
        public int MapCode;

        /// <summary>
        /// 罗兰龙塔地图编号
        /// </summary>
        public int MapCode_LongTa;

        /// <summary>
        /// 复活和地图传送位置
        /// </summary>
        public Dictionary<int, List<MapBirthPoint>> MapBirthPointListDict = new Dictionary<int, List<MapBirthPoint>>();

        /// <summary>
        /// NPCID到旗帜配置字典
        /// </summary>
        public Dictionary<int, QiZhiConfig> NPCID2QiZhiConfigDict = new Dictionary<int, QiZhiConfig>();

        /// <summary>
        /// 申请城战所需战盟资金
        /// </summary>
        public long ApplyZhangMengZiJin = 0;

        /// <summary>
        /// 最大参与战盟数(包括守城方)
        /// </summary>
        public int MaxZhanMengNum = 4;

        /// <summary>
        /// 竞标城战所需增加的战盟资金
        /// </summary>
        public long BidZhangMengZiJin = 0;

        /// <summary>
        /// 最小的转生等级
        /// </summary>
        public int MinZhuanSheng = 0;

        /// <summary>
        /// 最低等级
        /// </summary>
        public int MinLevel = 0;

        /// <summary>
        /// 最小参战战盟数
        /// </summary>
        public int MinRequestNum = 1;

        /// <summary>
        /// 最大进入人数
        /// </summary>
        public int MaxEnterNum = 1000;

        /// <summary>
        /// 安插军旗的花费
        /// </summary>
        public int InstallJunQiNeedMoney = 0;

        /// <summary>
        /// 活动前的预留截止报名时间（秒）
        /// </summary>
        public long EnrollTime = 1800;

        #endregion 活动配置

        #region 活动时间

        /// <summary>
        /// 开服几天后这个功能开启
        /// </summary>
        public int GongNengOpenDaysFromKaiFu = 5;

        /// <summary>
        /// 申请时间开始
        /// </summary>
        public TimeSpan NoRequestTimeStart;

        /// <summary>
        /// 申请时间结束
        /// </summary>
        public TimeSpan NoRequestTimeEnd;

        /// <summary>
        /// 活动的DayOfWeek
        /// </summary>
        public int[] WeekPoints = new int[0];

        /// <summary>
        /// 活动开始时间
        /// </summary>
        public DateTime TimePoints;

        /// <summary>
        /// 下次王城战的举行的时间
        /// </summary>
        public DateTime WangChengZhanFightingDateTime;

        /// <summary>
        /// 准备进入时间
        /// </summary>
        public int WaitingEnterSecs;

        /// <summary>
        /// 准备时间
        /// </summary>
        public int PrepareSecs;

        /// <summary>
        /// 战斗时间
        /// </summary>
        public int FightingSecs;

        /// <summary>
        /// 清场时间
        /// </summary>
        public int ClearRolesSecs;

        /// <summary>
        /// 播放竞价结果公告的状态(是否已播报)
        /// </summary>
        public bool CanRequestState = false;

        #endregion 活动时间

        #region 奖励配置

        /// <summary>
        /// 每日奖励配置字典
        /// </summary>
        public Dictionary<int, SiegeWarfareEveryDayAwardsItem> SiegeWarfareEveryDayAwardsDict = new Dictionary<int, SiegeWarfareEveryDayAwardsItem>();

        /// <summary>
        /// 活动奖励经验
        /// </summary>
        public long ExpAward;

        /// <summary>
        /// 活动奖励战功
        /// </summary>
        public int ZhanGongAward;

        /// <summary>
        /// 活动奖励战盟资金
        /// </summary>
        public int ZiJin;

        #endregion 奖励配置

        #region 运行时数据

        public string WarRequestStr = null;

        /// <summary>
        /// 城战竞标信息字典
        /// </summary>
        public Dictionary<int, LuoLanChengZhanRequestInfo> WarRequstDict = new Dictionary<int, LuoLanChengZhanRequestInfo>();

        /// <summary>
        /// 帮会ID到罗兰城战参展序号的映射字典,和复活点、一些界面显示相关
        /// </summary>
        public Dictionary<int, int> BHID2SiteDict = new Dictionary<int, int>();

        /// <summary>
        /// 龙塔内帮会的人数信息列表
        /// </summary>
        public List<LuoLanChengZhanRoleCountData> LongTaBHRoleCountList = new List<LuoLanChengZhanRoleCountData>();

        /// <summary>
        /// 龙塔临时占有者信息
        /// </summary>
        public LuoLanChengZhanLongTaOwnerData LongTaOwnerData = new LuoLanChengZhanLongTaOwnerData();

        /// <summary>
        /// 罗兰城战旗帜Buff拥有者信息列表
        /// </summary>
        public List<LuoLanChengZhanQiZhiBuffOwnerData> QiZhiBuffOwnerDataList = new List<LuoLanChengZhanQiZhiBuffOwnerData>();

        /// <summary>
        /// 拥有后,享有特殊复活点的旗帜NPC的ID
        /// </summary>
        public int SuperQiZhiNpcId = 80000;

        public int SuperQiZhiOwnerBirthPosX;
        public int SuperQiZhiOwnerBirthPosY;

        /// <summary>
        /// 特殊旗帜的拥有者帮会
        /// </summary>
        public int SuperQiZhiOwnerBhid = 0;

        /// <summary>
        /// 上次清场时间
        /// </summary>
        public long LastClearMapTicks = 0;

        /// <summary>
        /// 本次活动结束时间
        /// </summary>
        public DateTime FightEndTime;

        /// <summary>
        /// 旗帜Buff禁用的参数
        /// </summary>
        public Dictionary<int, double[]> QiZhiBuffDisableParamsDict = new Dictionary<int, double[]>();

        /// <summary>
        /// 旗帜Buff启用的参数
        /// </summary>
        public Dictionary<int, double[]> QiZhiBuffEnableParamsDict = new Dictionary<int, double[]>();

        /// <summary>
        /// 罗兰城主战盟ID
        /// </summary>
        public int LuoLanChengZhuBHID;

        /// <summary>
        /// 罗兰城主战盟名称
        /// </summary>
        public string LuoLanChengZhuBHName;

        /// <summary>
        /// 罗兰城主上次登录时间
        /// </summary>
        public long LuoLanChengZhuLastLoginTicks;

        /// <summary>
        /// 罗兰城主的GameClient对象
        /// </summary>
        public GameClient LuoLanChengZhuClient;

        #endregion 运行时数据
    }
}
