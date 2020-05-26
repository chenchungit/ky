using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    // 金币副本场景类 [6/11/2014 LiaoWei]
    class GoldCopyScene
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
        public int m_CreateMonsterWave = 0;
        
        /// <summary>
        /// 时间通知标记
        /// </summary>
        public int m_TimeNotifyFlag = 0;

        /// <summary>
        /// 刷怪TICK1 -- 小波时间
        /// </summary>
        public long m_CreateMonsterTick1  = 0;

        /// <summary>
        /// 刷怪TICK2 -- 大波时间
        /// </summary>
        public long m_CreateMonsterTick2 = 0;

        /// <summary>
        /// 刷怪计数
        /// </summary>
        public int m_CreateMonsterCount = 0;

        /// <summary>
        /// 第一波刷怪通知客户端标记
        /// </summary>
        public int m_CreateMonsterFirstWaveFlag = 0;

        /// <summary>
        /// 刷怪通知
        /// </summary>
        public int m_CreateMonsterWaveNotify = 0;

        /// <summary>
        /// 开始时间
        /// </summary>
        public long m_StartTimer = 0;

        /// <summary>
        /// 登陆进入金币副本标记
        /// </summary>
        public int m_LoginEnterFlag = 0;

        /// <summary>
        /// 登陆进入金币副本时间
        /// </summary>
        public long m_LoginEnterTimer = 0;

        /// <summary>
        /// 初始化接口
        /// </summary>
        public void InitInfo(int nMapCode, int CopyMapID, int nQueueID)
        {
            m_MapCodeID                     = nMapCode;
            m_CopyMapID                     = CopyMapID;
            m_CopyMapQueueID                = nQueueID;
            m_CreateMonsterWave             = 0;
            m_TimeNotifyFlag                = 0;
            m_CreateMonsterTick1            = 0;
            m_CreateMonsterTick2            = 0;
            m_CreateMonsterCount            = 0;
            m_CreateMonsterFirstWaveFlag    = 0;
            m_CreateMonsterWaveNotify       = 0;
            m_StartTimer                    = 0;
            m_LoginEnterFlag                = 0;
            m_LoginEnterTimer               = 0;
        }

        /// <summary>
        /// 清空接口
        /// </summary>
        public void CleanAllInfo()
        {
            m_MapCodeID                 = 0;
            m_CopyMapQueueID            = 0;
            m_CreateMonsterWave         = 0;
            m_TimeNotifyFlag            = 0;
            m_CreateMonsterTick1        = 0;
            m_CreateMonsterTick2        = 0;
            m_CreateMonsterCount        = 0;
            m_CreateMonsterFirstWaveFlag = 0;
            m_CreateMonsterWaveNotify   = 0;
            m_StartTimer                = 0;
            m_StartTimer                = 0;
            m_LoginEnterFlag            = 0;
            m_LoginEnterTimer           = 0;
        }
        
    }

    /// <summary>
    /// 金币副本怪物信息
    /// </summary>
    public class GoldCopySceneMonster
    {
        /// <summary>
        // 波数
        /// </summary>
        public int m_Wave { get; set; }

        /// <summary>
        // 数量
        /// </summary>
        public int m_Num { get; set; }

        /// <summary>
        // 是否是第一波
        /// </summary>
        public bool m_bIsFirstWave { get; set; }

        // 怪ID列表10个元素 
        public List<int> m_MonsterID { get; set; }

        /// <summary>
        // 小波间隔
        /// </summary>
        public int m_Delay1 { get; set; }

        /// <summary>
        // 大波间隔
        /// </summary>
        public int m_Delay2 { get; set; }
    }
}
