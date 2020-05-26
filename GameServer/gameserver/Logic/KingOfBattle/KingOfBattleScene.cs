using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Windows;
using Tmsk.Contract;
using Server.Data;
using KF.Contract.Data;

namespace GameServer.Logic
{
    /// <summary>
    /// 王者战场场景BUFF
    /// </summary>
    public class KingOfBattleSceneBuff
    {
        // 角色ID
        public int RoleID;

        // BuffID
        public int BuffID;

        // 结束时间Ticks
        public long EndTicks;

        // 附加信息
        public object tagInfo;
    }

    /// <summary>
    /// 王者战场场景对象
    /// </summary>
    public class KingOfBattleScene
    {
        public int m_nMapCode = 0;

        public int FuBenSeqId = 0;

        public int CopyMapId = 0;

        /// <summary>
        /// 活动起始时间点
        /// </summary>
        public long StartTimeTicks = 0;

        /// <summary>
        /// 王者战场场景开始时间
        /// </summary>
        public long m_lPrepareTime = 0;

        /// <summary>
        /// 王者战场场景战斗开始时间
        /// </summary>
        public long m_lBeginTime = 0;

        /// <summary>
        /// 王者战场场景战斗结束时间
        /// </summary>
        public long m_lEndTime = 0;

        /// <summary>
        /// 立场时间
        /// </summary>
        public long m_lLeaveTime = 0;

        /// <summary>
        /// 场景状态
        /// </summary>
        public GameSceneStatuses m_eStatus = GameSceneStatuses.STATUS_NULL;

        /// 玩家人数
        /// </summary>
        public int m_nPlarerCount = 0;

        /// <summary>
        /// 获胜方
        /// </summary>
        public int SuccessSide = 0;

        /// <summary>
        /// 结束标记
        /// </summary>
        public bool m_bEndFlag = false;

        /// <summary>
        /// 密道光幕Open信息
        /// </summary>
        public bool GuangMuNotify1 = false;
        public bool GuangMuNotify2 = false;

        /// <summary>
        /// 跨服
        /// </summary>
        public int GameId;

        /// <summary>
        /// 关联的副本对象
        /// </summary>
        public CopyMap CopyMap;

        /// <summary>
        /// 场景配置信息
        /// </summary>
        public KingOfBattleSceneInfo SceneInfo;

        /// <summary>
        /// 战斗统计信息,评估产品设计水平
        /// </summary>
        public KingOfBattleStatisticalData GameStatisticalData = new KingOfBattleStatisticalData();

        /// <summary>
        /// 阵营得分信息
        /// </summary>
        public KingOfBattleScoreData ScoreData = new KingOfBattleScoreData();

        /// <summary>
        /// 角色得分信息集合
        /// </summary>
        public Dictionary<int, KingOfBattleClientContextData> ClientContextDataDict = new Dictionary<int, KingOfBattleClientContextData>();

        /// <summary>
        /// 时间状态信息
        /// </summary>
        public GameSceneStateTimeData StateTimeData = new GameSceneStateTimeData();

        /// <summary>
        /// 怪物创建队列
        /// </summary>
        public SortedList<long, List<object>> CreateMonsterQueue = new SortedList<long, List<object>>();

        /// <summary>
        /// NPCID到旗帜配置字典 Clone for Each Scene
        /// </summary>
        public Dictionary<int, KingOfBattleQiZhiConfig> NPCID2QiZhiConfigDict = new Dictionary<int, KingOfBattleQiZhiConfig>();

        /// <summary>
        /// 场景Buff数据
        /// </summary>
        public Dictionary<string, KingOfBattleSceneBuff> SceneBuffDict = new Dictionary<string, KingOfBattleSceneBuff>();

        /// <summary>
        /// 场景传送门数据
        /// </summary>
        public List<int> SceneOpenTeleportList = new List<int>();

        public int MapGridWidth = 80;

        public int MapGridHeight = 80;

        public void CleanAllInfo()
        {
            m_nMapCode = 0;
            m_lPrepareTime = 0;
            m_lBeginTime = 0;
            m_lEndTime = 0;
            m_eStatus = GameSceneStatuses.STATUS_NULL;
            m_nPlarerCount = 0;
            m_bEndFlag = false;
        }

    }
}
