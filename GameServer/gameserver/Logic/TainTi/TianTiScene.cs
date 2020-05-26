using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Windows;
using Tmsk.Contract;
using Server.Data;

namespace GameServer.Logic
{
    /// <summary>
    /// 天梯场景对象
    /// </summary>
    public class TianTiScene
    {
        /// <summary>
        /// 血色城堡场景mapcode
        /// </summary>
        public int m_nMapCode = 0;

        public int FuBenSeqId = 0;

        public int CopyMapId = 0;

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
        /// 获胜方
        /// </summary>
        public int SuccessSide = 0;

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
        /// 记录双方的信息,发放奖励和记录日志时使用
        /// </summary>
        public Dictionary<int, TianTiRoleMiniData> RoleIdDuanWeiIdDict = new Dictionary<int, TianTiRoleMiniData>();

        /// <summary>
        /// 时间状态信息
        /// </summary>
        public GameSceneStateTimeData StateTimeData = new GameSceneStateTimeData();

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
