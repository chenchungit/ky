using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;

namespace GameServer.Logic.Marriage.CoupleArena
{
    /// <summary>
    /// 夫妻竞技场---常量定义
    /// </summary>
    public static class CoupleAreanConsts
    {
        public readonly static string WarCfgFile = @"Config\CoupleWar.xml";
        public readonly static string DuanWeiCfgFile = @"Config\CoupleDuanWei.xml";
        public readonly static string WeekRankAwardCfgFile = @"Config\CoupleWarAward.xml";
        public readonly static string BuffCfgFile = @"Config\CoupleBuff.xml";
        public readonly static string BirthPointCfgFile = @"Config\CoupleBirthPoint.xml";

        public readonly static int ZhenAiBuffCfgType = 1;
        public readonly static int YongQiBuffCfgType = 2;
    }

    /// <summary>
    /// 夫妻竞技场---角色匹配状态
    /// </summary>
    public enum ECoupleArenaMatchState
    {
        /// <summary>
        /// 离线
        /// </summary>
        Offline = 0,

        /// <summary>
        /// 在线
        /// </summary>
        OnLine = 1,

        /// <summary>
        /// 已准备
        /// </summary>
        Ready = 2,

        /// <summary>
        /// 未开启此系统
        /// </summary>
        NotOpen = 3,
    }

    /// <summary>
    /// 夫妻竞技场pk配置
    /// </summary>
    public class CoupleAreanWarCfg
    {
        public class TimePoint
        {
            public int Weekday;
            public long DayStartTicks;
            public long DayEndTicks;
        }

        public int Id;
        public int MapCode;
        public List<TimePoint> TimePoints;
        public int WaitSec;
        public int FightSec;
        public int ClearSec;
    }

    /// <summary>
    /// 夫妻竞技场段位配置
    /// </summary>
    public class CoupleAreanDuanWeiCfg
    {
        public int Id;
        public int Type;
        public int Level;
        public int NeedJiFen;
        public int WinJiFen;
        public int LoseJiFen;
        public int WeekGetRongYaoTimes;
        public int WinRongYao;
        public int LoseRongYao;
    }

    /// <summary>
    /// 夫妻竞技场周排行奖励
    /// </summary>
    public class CoupleAreanWeekRankAwardCfg
    {
        public int Id;
        public string Name;
        public int StartRank;
        public int EndRank;
        public List<GoodsData> AwardGoods;
    }

    /// <summary>
    /// 夫妻竞技场战斗buff配置
    /// </summary>
    public class CoupleArenaBuffCfg
    {
        public class RandPos
        {
            public int X;
            public int Y;
            public int R;
        }
        public int Type;
        public string Name;
        public List<RandPos> RandPosList;
        public List<int> FlushSecList;
        public Dictionary<ExtPropIndexes, double> ExtProps;
        public int MonsterId;
    }
}
