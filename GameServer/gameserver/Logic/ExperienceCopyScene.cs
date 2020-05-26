using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    // 经验副本场景类 [3/19/2014 LiaoWei]
    class ExperienceCopyScene
    {
        /// <summary>
        /// mapcode id
        /// </summary>
        public int m_MapCodeID = 0;

        /// <summary>
        /// 副本ID
        /// </summary>
        public int m_CopyMapID = 0;

        /// <summary>
        /// 副本的队列ID
        /// </summary>
        public int m_CopyMapQueueID = 0;

        /// <summary>
        /// 刷怪波数
        /// </summary>
        public int m_ExperienceCopyMapCreateMonsterWave = 0;

        /// <summary>
        /// 刷怪标记
        /// </summary>
        public int m_ExperienceCopyMapCreateMonsterFlag = 0;

        /// <summary>
        /// 刷怪数量
        /// </summary>
        public int m_ExperienceCopyMapCreateMonsterNum = 0;

        /// <summary>
        /// 需要杀怪的数量
        /// </summary>
        public int m_ExperienceCopyMapNeedKillMonsterNum = 0;

        /// <summary>
        /// 已经杀怪的数量
        /// </summary>
        public int m_ExperienceCopyMapKillMonsterNum = 0;

        /// <summary>
        /// 剩余怪的数量
        /// </summary>
        public int m_ExperienceCopyMapRemainMonsterNum = 0;

        /// <summary>
        /// 已经杀怪的总数量
        /// </summary>
        public int m_ExperienceCopyMapKillMonsterTotalNum = 0;

        /// <summary>
        /// 开始时间
        /// </summary>
        public long m_StartTimer = 0;

        /// <summary>
        /// 初始化接口
        /// </summary>
        public void InitInfo(int nMapCode, int CopyMapID, int nQueueID)
        {
            m_MapCodeID                             = nMapCode;
            m_CopyMapID                             = CopyMapID;
            m_CopyMapQueueID                        = nQueueID;
            m_ExperienceCopyMapCreateMonsterWave    = 0;
            m_ExperienceCopyMapCreateMonsterFlag    = 0;
            m_ExperienceCopyMapCreateMonsterNum     = 0;
            m_ExperienceCopyMapNeedKillMonsterNum   = 0;
            m_ExperienceCopyMapKillMonsterNum       = 0;
            m_ExperienceCopyMapRemainMonsterNum     = 0;
            m_ExperienceCopyMapKillMonsterTotalNum  = 0;
            m_StartTimer                            = 0;
        }

        /// <summary>
        /// 清空接口
        /// </summary>
        public void CleanAllInfo()
        {
            m_MapCodeID                             = 0;
            m_CopyMapQueueID                        = 0;
            m_ExperienceCopyMapCreateMonsterWave    = 0;
            m_ExperienceCopyMapCreateMonsterFlag    = 0;
            m_ExperienceCopyMapCreateMonsterNum     = 0;
            m_ExperienceCopyMapNeedKillMonsterNum   = 0;
            m_ExperienceCopyMapKillMonsterNum       = 0;
            m_ExperienceCopyMapRemainMonsterNum     = 0;
            m_ExperienceCopyMapKillMonsterTotalNum  = 0;
            m_StartTimer                            = 0;
        }

    }
}
