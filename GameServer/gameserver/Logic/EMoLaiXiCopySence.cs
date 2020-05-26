using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 恶魔来袭副本场景类
    /// </summary>
    class EMoLaiXiCopySence
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
        /// 大波时间间隔(所有路线的最大值)
        /// </summary>
        public long m_Delay2 = 0;

        /// <summary>
        /// 当前波要刷的怪物信息列表
        /// </summary>
        public List<EMoLaiXiCopySenceMonster> m_CreateWaveMonsterList = new List<EMoLaiXiCopySenceMonster>();

        /// <summary>
        /// 刷怪TICK2 -- 大波时间
        /// </summary>
        public long m_CreateMonsterTick2 = 0;

        /// <summary>
        /// 刷怪计数
        /// </summary>
        public int m_CreateMonsterCount = 0;

        /// <summary>
        /// 本大波总怪物计数
        /// </summary>
        public int m_TotalMonsterCount = 0;

        /// <summary>
        /// 总怪物计数
        /// </summary>
        public int m_TotalMonsterCountAllWave = 0;

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
        /// 逃跑的怪物数
        /// </summary>
        public int m_EscapedMonsterNum = 0;

        /// <summary>
        /// 结束标识
        /// </summary>
        public bool m_bAllMonsterCreated = false;

        /// <summary>
        /// 结束标识
        /// </summary>
        public bool m_bFinished = false;

        /// <summary>
        /// 初始化接口
        /// </summary>
        public void InitInfo(int nMapCode, int CopyMapID, int nQueueID)
        {
            m_MapCodeID                     = nMapCode;
            m_CopyMapID                     = CopyMapID;
            m_CopyMapQueueID                = nQueueID;
            CleanAllInfo();
        }

        /// <summary>
        /// 清空接口
        /// </summary>
        public void CleanAllInfo()
        {
            m_CreateMonsterWave         = 0;
            m_TimeNotifyFlag            = 0;
            m_CreateMonsterTick2        = 0;
            m_CreateMonsterCount        = 0;
            m_CreateMonsterFirstWaveFlag = 0;
            m_CreateMonsterWaveNotify   = 0;
            m_StartTimer                = 0;
            m_LoginEnterFlag            = 0;
            m_LoginEnterTimer           = 0;
            m_EscapedMonsterNum = 0;
            m_bFinished = false;
            m_bAllMonsterCreated = false;
            m_TotalMonsterCount = 0;
            m_Delay2 = 0;
        }
    }

    /// <summary>
    /// 恶魔来袭副本怪物信息
    /// </summary>
    public class EMoLaiXiCopySenceMonster
    {
        /// <summary>
        /// ID
        /// </summary>
        public int m_ID;

        /// <summary>
        /// 波数
        /// </summary>
        public int m_Wave;

        /// <summary>
        /// 可选路径ID
        /// </summary>
        public int[] PathIDArray;

        /// <summary>
        /// 数量
        /// </summary>
        public int m_Num;

        // 怪ID列表 
        public List<int> m_MonsterID;

        /// <summary>
        /// 小波间隔
        /// </summary>
        public int m_Delay1;

        /// <summary>
        /// 刷怪TICK2 -- 小波时间
        /// </summary>
        public long m_CreateMonsterTick1 = 0;

        /// <summary>
        /// 刷怪计数
        /// </summary>
        public int m_CreateMonsterCount = 0;

        /// <summary>
        /// 大波间隔
        /// </summary>
        public int m_Delay2;

        /// <summary>
        /// 路径信息
        /// </summary>
        public List<int[]> PatrolPath;

        public EMoLaiXiCopySenceMonster CloneMini()
        {
            EMoLaiXiCopySenceMonster em = new EMoLaiXiCopySenceMonster()
            {
                m_ID = m_ID,
                m_Wave = m_Wave,
                PathIDArray = PathIDArray,
                m_Num = m_Num,
                m_MonsterID = m_MonsterID,
                m_Delay1 = m_Delay1,
                m_CreateMonsterTick1 = m_CreateMonsterTick1,
                m_CreateMonsterCount = m_CreateMonsterCount,
                m_Delay2 = m_Delay2,
            };

            return em;
        }
    }
}
