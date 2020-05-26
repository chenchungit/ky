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
    public class ShengBeiData
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID;

        /// <summary>
        /// 采集怪ID
        /// </summary>
        public int MonsterID;

        /// <summary>
        /// 采集时间
        /// </summary>
        public int Time;

        /// <summary>
        /// BuffID
        /// </summary>
        public int GoodsID;

        /// <summary>
        /// 交付得分
        /// </summary>
        public int Score;

        /// <summary>
        /// X坐标
        /// </summary>
        public int PosX;

        /// <summary>
        /// Y坐标
        /// </summary>
        public int PosY;

        /// <summary>
        /// Buffer属性
        /// </summary>
        public double[] BufferProps;
    }

    public class HuanYingSiYuanBirthPoint
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID;

        /// <summary>
        /// X坐标
        /// </summary>
        public int PosX;

        /// <summary>
        /// Y坐标
        /// </summary>
        public int PosY;

        /// <summary>
        /// 随机半径（米）
        /// </summary>
        public int BirthRadius; 
    }

    /// <summary>
    /// 连杀奖励
    /// </summary>
    public class ContinuityKillAward
    {
        public int ID;
        public int Num;
        public int Score;
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class HuanYingSiYuanData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();

        #region 活动配置

        /// <summary>
        /// 等级区间对应的组ID字典
        /// </summary>
        public Dictionary<RangeKey, int> Range2GroupIndexDict = new Dictionary<RangeKey, int>(RangeKey.Comparer);

        /// <summary>
        /// 复活和地图传送位置
        /// </summary>
        public Dictionary<int, HuanYingSiYuanBirthPoint> MapBirthPointDict = new Dictionary<int, HuanYingSiYuanBirthPoint>();

        /// <summary>
        /// 连杀奖励配置
        /// </summary>
        public Dictionary<int, ContinuityKillAward> ContinuityKillAwardDict = new Dictionary<int, ContinuityKillAward>();

        /// <summary>
        /// 幻影寺院地图编号
        /// </summary>
        public int MapCode;

        /// <summary>
        /// 活动时间列表（开始|结束|开始|结束...)
        /// </summary>
        public List<TimeSpan> TimePoints = new List<TimeSpan>();

        /// <summary>
        /// 最小的转生等级
        /// </summary>
        public int MinZhuanSheng = 1;

        /// <summary>
        /// 最低等级
        /// </summary>
        public int MinLevel = 1;

        /// <summary>
        /// 最小参战战盟数
        /// </summary>
        public int MinRequestNum = 1;

        /// <summary>
        /// 最大进入人数
        /// </summary>
        public int MaxEnterNum = 10;

        /// <summary>
        /// 副本ID
        /// </summary>
        public int FuBenId = 13000;

        /// <summary>
        /// 圣杯最大持有不交付的时间(秒)
        /// </summary>
        public int HoldShengBeiSecs = 60;

        /// <summary>
        /// 提交圣杯的最小消耗时间(防外挂)
        /// </summary>
        public int MinSubmitShengBeiSecs = 13;

        /// <summary>
        /// 计次和发奖励的最小分数值
        /// </summary>
        public int TempleMirageMinJiFen = 0;

        #endregion 活动配置

        #region 活动时间

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
        /// 总时间
        /// </summary>
        public int TotalSecs { get { return WaitingEnterSecs + PrepareSecs + FightingSecs + ClearRolesSecs; } }

        #endregion 活动时间

        #region 奖励配置

        /// <summary>
        /// 圣杯信息字典
        /// </summary>
        public Dictionary<int, ShengBeiData> ShengBeiDataDict = new Dictionary<int, ShengBeiData>();

        /// <summary>
        /// 地图格子大小缓存
        /// </summary>
        public int MapGridWidth = 80;
        public int MapGridHeight = 80;

        public Dictionary<int, HuanYingSiYuanShengBeiContextData> ShengBeiContextDict = new Dictionary<int, HuanYingSiYuanShengBeiContextData>();

        /// <summary>
        /// 活动奖励经验
        /// </summary>
        public long TempleMirageEXPAward;

        /// <summary>
        /// 奖励成就
        /// </summary>
        public int TempleMirageAwardChengJiu;

        /// <summary>
        /// 奖励声望
        /// </summary>
        public int TempleMirageAwardShengWang;

        /// <summary>
        /// 获胜所需积分
        /// </summary>
        public int TempleMirageWin = 1000;

        /// <summary>
        /// 杀对付的人得分
        /// </summary>
        public int TempleMiragePK = 8;

        /// <summary>
        /// 每日多倍奖励次数
        /// </summary>
        public int TempleMirageWinExtraNum = 3;

        /// <summary>
        /// 每日多倍奖励倍数
        /// </summary>
        public int TempleMirageWinExtraRate = 10;

        /// <summary>
        /// 副本获得物品是否绑定
        /// </summary>
        public int FuBenGoodsBinding = 1;

        #endregion 奖励配置

        #region 运行时数据

        public Dictionary<int, int> GameId2FuBenSeq = new Dictionary<int, int>();

        #endregion 运行时数据
    }
}
