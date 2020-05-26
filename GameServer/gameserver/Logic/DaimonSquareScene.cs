using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    // 恶魔广场场景类 [12/24/2013 LiaoWei]
    public class DaimonSquareScene
    {
        /// <summary>
        /// 恶魔广场场景mapcode
        /// </summary>
        public int m_nMapCode = 0;

        /// <summary>
        /// 恶魔广场场景开始时间
        /// </summary>
        public long m_lPrepareTime = 0;

        /// <summary>
        /// 恶魔广场场景战斗开始时间
        /// </summary>
        public long m_lBeginTime = 0;

        /// <summary>
        /// 恶魔广场场景战斗结束时间
        /// </summary>
        public long m_lEndTime = 0;

        /// <summary>
        /// 恶魔广场刷怪波数
        /// </summary>
        public int m_nMonsterWave = 0;

        /// <summary>
        /// 恶魔广场刷怪总波数
        /// </summary>
        public int m_nMonsterTotalWave = 0;

        /// <summary>
        /// 恶魔广场刷怪标记
        /// </summary>
        public int m_nCreateMonsterFlag = 0;

        /// <summary>
        /// 场景状态
        /// </summary>
        public DaimonSquareStatus m_eStatus = DaimonSquareStatus.FIGHT_STATUS_NULL;

        /// 玩家人数
        /// </summary>
        public int m_nPlarerCount = 0;

        /// <summary>
        /// 刷出的怪的数量
        /// </summary>
        public int m_nCreateMonsterCount = 0;

        /// <summary>
        /// 杀死了几只怪
        /// </summary>
        public int m_nKillMonsterNum = 0;

        /// <summary>
        /// 需要杀死几只怪
        /// </summary>
        public int m_nNeedKillMonsterNum = 0;

        /// <summary>
        /// 杀死了几只怪
        /// </summary>
        public int m_nKillMonsterTotalNum = 0;

        /// <summary>
        /// 动态怪物LIST
        /// </summary>
        public List<Monster> m_nDynamicMonsterList = new List<Monster>();

        /// <summary>
        /// 完成任务
        /// </summary>
        public bool m_bIsFinishTask = false;

        /// <summary>
        /// end标记
        /// </summary>
        public bool m_bEndFlag = false;

        /// <summary>
        /// 刷怪锁
        /// </summary>
        public object m_CreateMonsterMutex = new object();

        /// <summary>
        /// 被击杀的怪物的唯一ID的集合,因为击杀不加锁,用这个集合记录以防止重复计数
        /// </summary>
        public HashSet<long> m_KilledMonsterHashSet = new HashSet<long>();

        /// <summary>
        /// 关联的副本地图对象
        /// </summary>
        public CopyMap m_CopyMap;

        public bool ClearRole;

        /// <summary>
        /// 角色ID和对应的保存的积分的字典
        /// </summary>
        public Dictionary<long, int> RoleIdSavedScoreDict = new Dictionary<long, int>();

        public void CleanAllInfo()
        {
            m_nMapCode = 0;
            m_lPrepareTime = 0;
            m_lBeginTime = 0;
            m_lEndTime = 0;
            m_nPlarerCount = 0;
            m_nMonsterWave = 0;
            m_nCreateMonsterFlag = 0;
            m_eStatus = DaimonSquareStatus.FIGHT_STATUS_NULL;
            m_nCreateMonsterCount = 0;
            m_nNeedKillMonsterNum = 0;
            m_nDynamicMonsterList.Clear();
            m_bIsFinishTask = false;
            m_bEndFlag = false;
            m_nKillMonsterNum = 0;
            m_nKillMonsterTotalNum = 0;
            m_nMonsterTotalWave = 0;
            m_KilledMonsterHashSet.Clear();
        }

        /// <summary>
        /// 记录击杀的怪物ID并递增杀怪计数
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public bool AddKilledMonster(Monster monster)
        {
            bool firstKill = false;
            lock (m_KilledMonsterHashSet)
            {
                if (!m_KilledMonsterHashSet.Contains(monster.UniqueID))
                {
                    m_KilledMonsterHashSet.Add(monster.UniqueID);
                    m_nKillMonsterTotalNum++;
                    firstKill = true;
                }
            }
            return firstKill;
        }

        public bool AddRole(GameClient client)
        {
            bool result = false;
            int roleId = client.ClientData.RoleID;
            lock (RoleIdSavedScoreDict)
            {
                if (!RoleIdSavedScoreDict.ContainsKey(roleId))
                {
                    RoleIdSavedScoreDict[roleId] = 0;
                    result = true;
                }
            }

            return result;
        }

        public bool CantiansRole(GameClient client)
        {
            int roleId = client.ClientData.RoleID;
            lock (RoleIdSavedScoreDict)
            {
                return RoleIdSavedScoreDict.ContainsKey(roleId);
            }
        }

        public void AddRoleScoreAll(int addScore)
        {
            lock (RoleIdSavedScoreDict)
            {
                foreach (var key in RoleIdSavedScoreDict.Keys)
                {
                    RoleIdSavedScoreDict[key] += addScore;
                }
            }
        }

        public int AddRoleScore(GameClient client, int addScore)
        {
            int roleId = client.ClientData.RoleID;
            int score;
            lock (RoleIdSavedScoreDict)
            {
                if (RoleIdSavedScoreDict.TryGetValue(roleId, out score))
                {
                    score += addScore;
                    RoleIdSavedScoreDict[roleId] = 0;
                }
                else
                {
                    score = addScore;
                }

                RoleIdSavedScoreDict[roleId] = score;
            }

            return 0;
        }

        public int GetRoleScore(GameClient client)
        {
            int roleId = client.ClientData.RoleID;
            int score;
            lock (RoleIdSavedScoreDict)
            {
                if (RoleIdSavedScoreDict.TryGetValue(roleId, out score))
                {
                    return score;
                }
            }

            return 0;
        }
    }
}
