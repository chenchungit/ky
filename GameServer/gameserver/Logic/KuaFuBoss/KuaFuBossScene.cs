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
    /// 跨服Boss场景对象
    /// </summary>
    public class KuaFuBossScene
    {
        /// <summary>
        /// 血色城堡场景mapcode
        /// </summary>
        public int m_nMapCode = 0;

        public int FuBenSeqId = 0;

        public int CopyMapId = 0;

        /// <summary>
        /// 活动起始时间点
        /// </summary>
        public long StartTimeTicks = 0;

        /// <summary>
        /// 血色城堡场景开始时间
        /// </summary>
        public long m_lPrepareTime = 0;

        /// <summary>
        /// 血色城堡场景战斗开始时间
        /// </summary>
        public long m_lBeginTime = 0;

        /// <summary>
        /// 血色城堡场景战斗结束时间
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
        /// 从活动开始到现在经过的秒数
        /// </summary>
        public int ElapsedSeconds;

        /// <summary>
        /// 结束标记
        /// </summary>
        public bool m_bEndFlag = false;

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
        public KuaFuBossSceneInfo SceneInfo;

        /// <summary>
        /// 战斗统计信息,评估产品设计水平
        /// </summary>
        public KuaFuBossStatisticalData GameStatisticalData = new KuaFuBossStatisticalData();

        /// <summary>
        /// 时间状态信息
        /// </summary>
        public GameSceneStateTimeData StateTimeData = new GameSceneStateTimeData();

        /// <summary>
        /// 下次广播场景信息的时间
        /// </summary>
        public long NextNotifySceneStateDataTicks = 0;

        /// <summary>
        /// 场景信息
        /// </summary>
        public KuaFuBossSceneStateData SceneStateData = new KuaFuBossSceneStateData();

        /// <summary>
        /// 动态刷怪列表
        /// </summary>
        public List<BattleDynamicMonsterItem> DynMonsterList = null;

        /// <summary>
        /// 已经动态刷出的怪, key：怪物流水号, BattleMonster.xml
        /// </summary>
        public HashSet<int> DynMonsterSet = new HashSet<int>();

        /// <summary>
        /// 已击杀的怪物字典
        /// </summary>
        public HashSet<int> KilledMonsterSet = new HashSet<int>();

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
