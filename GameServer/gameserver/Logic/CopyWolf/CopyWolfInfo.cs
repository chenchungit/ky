using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace GameServer.Logic
{
    public class CreateMonsterTagInfo
    {
        public int FuBenSeqId = 0;

        public bool IsFort = false;

        public int ManagerType = 0;
    }

    /// <summary>
    /// 怪物波次配置
    /// </summary>
    public class CopyWolfWaveInfo
    {
        /// <summary>
        /// 波次id
        /// </summary>
        public int WaveID = 0;

        /// <summary>
        /// 下波时间
        /// </summary>
        public int NextTime = 0;

        /// <summary>
        /// 怪物id,怪物数量
        /// </summary>
        public List<int[]> MonsterList = new List<int[]>();     

        /// <summary>
        /// 出生地
        /// </summary>
        public List<CopyWolfSiteInfo> MonsterSiteDic = new List<CopyWolfSiteInfo>();
    }

    /// <summary>
    /// 出生点配置
    /// </summary>
    public class CopyWolfSiteInfo
    {
        /// <summary>
        /// 出生地X
        /// </summary>
        public int X = 0;

        /// <summary>
        /// 出生地Y
        /// </summary>
        public int Y = 0;

        /// <summary>
        /// 出生地半径
        /// </summary>
        public int Radius = 0;
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class CopyWolfInfo
    {
        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapID = 70200;

        /// <summary>
        /// 副本ID
        /// </summary>
        public int CopyID = 70200;

        public int CampID = 1;

        /// <summary>
        /// 积分系数——时间
        /// </summary>
        public int ScoreRateTime = 0;

        /// <summary>
        /// 积分系数——生命
        /// </summary>
        public int ScoreRateLife = 0;

        /// <summary>
        /// 要塞id
        /// </summary>
        public int FortMonsterID = 0;

        /// <summary>
        /// 要塞出生点
        /// </summary>
        public Point FortSite = new Point();

        /// <summary>
        /// 波次数据
        /// </summary>
        public Dictionary<int, CopyWolfWaveInfo> CopyWolfWaveDic = new Dictionary<int, CopyWolfWaveInfo>();
        public CopyWolfWaveInfo GetWaveConfig(int wave)
        {
            if (CopyWolfWaveDic.ContainsKey(wave))
                return CopyWolfWaveDic[wave];

            return null;
        }

        /// <summary>
        /// 怪物对要塞伤害(怪物id,怪物伤害)
        /// </summary>
        public Dictionary<int, int> MonsterHurtDic = new Dictionary<int, int>();
        public int GetMonsterHurt(int monsterID)
        {
            if (MonsterHurtDic.ContainsKey(monsterID))
                return MonsterHurtDic[monsterID];

            return 0;
        }

        /// <summary>
        /// 副本场景数据
        /// KEY-副本顺序ID VALUE-Scene信息
        /// </summary>
        public ConcurrentDictionary<int, CopyWolfSceneInfo> SceneDict = new ConcurrentDictionary<int, CopyWolfSceneInfo>();

        #region 活动时间

        /// <summary>
        /// 准备时间
        /// </summary>
        public int PrepareSecs = 1;

        /// <summary>
        /// 战斗时间
        /// </summary>
        public int FightingSecs = 900;

        /// <summary>
        /// 清场时间
        /// </summary>
        public int ClearRolesSecs = 15;

        /// <summary>
        /// 总时间
        /// </summary>
        public int TotalSecs { get { return PrepareSecs + FightingSecs + ClearRolesSecs; } }

        #endregion 活动时间

    }
}

