using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Windows;

namespace GameServer.Logic
{
    // 血色城堡场景类 [11/5/2013 LiaoWei]
    public class BloodCastleScene
    {
        /// <summary>
        /// 血色城堡场景mapcode
        /// </summary>
        public int m_nMapCode = 0;

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
        /// 场景状态
        /// </summary>
        public BloodCastleStatus m_eStatus = BloodCastleStatus.FIGHT_STATUS_NULL;

        /// 玩家人数
        /// </summary>
        public int m_nPlarerCount = 0;

        /// <summary>
        /// 杀死A类型怪的个数
        /// </summary>
        public int m_nKillMonsterACount = 0;

        /// <summary>
        /// 杀死A类型怪状态是否完成
        /// </summary>
        public int m_bKillMonsterAStatus = 0;

        /// <summary>
        /// 杀死B类型怪的个数
        /// </summary>
        public int m_nKillMonsterBCount = 0;

        /// <summary>
        /// 杀死B类型怪状态是否完成
        /// </summary>
        public int m_bKillMonsterBStatus = 0;

        /// <summary>
        /// 动态怪物LIST
        /// </summary>
        public List<Monster> m_nDynamicMonsterList = new List<Monster>();

        /// <summary>
        /// 完成任务
        /// </summary>
        public bool m_bIsFinishTask = false;

        /// <summary>
        /// 完成任务人的ID
        /// </summary>
        public int m_nRoleID = -1;

        /// <summary>
        /// end标记
        /// </summary>
        public bool m_bEndFlag = false;

        public object Mutex = new object();

        public int m_Step = 0;

        /// <summary>
        /// 关联的副本地图对象
        /// </summary>
        public CopyMap m_CopyMap;

        /// <summary>
        /// 角色ID和对应的保存的积分的字典
        /// </summary>
        public Dictionary<long, int> RoleIdSavedScoreDict = new Dictionary<long, int>();

        /// <summary>
        /// 角色ID和对应的保存的积分的字典
        /// </summary>
        public Dictionary<long, int> RoleIdTotalScoreDict = new Dictionary<long, int>();

        public void CleanAllInfo()
        {
            m_nMapCode = 0;
            m_lPrepareTime = 0;
            m_lBeginTime = 0;
            m_lEndTime = 0;
            m_eStatus = BloodCastleStatus.FIGHT_STATUS_NULL;
            m_nPlarerCount = 0;
            m_nKillMonsterACount = 0;
            m_nKillMonsterBCount = 0;
            m_nDynamicMonsterList.Clear();
            m_bIsFinishTask = false;
            m_nRoleID = -1;
            m_bKillMonsterAStatus = 0;
            m_bKillMonsterBStatus = 0;
            m_bEndFlag = false;
            RoleIdSavedScoreDict.Clear();
            RoleIdTotalScoreDict.Clear();
        }

        public bool AddRole(GameClient client)
        {
            bool result = false;
            int roleId = client.ClientData.RoleID;
            lock (Mutex)
            {
                if (!RoleIdSavedScoreDict.ContainsKey(roleId))
                {
                    RoleIdSavedScoreDict[roleId] = 0;
                    result = true;
                }

                if (!RoleIdTotalScoreDict.ContainsKey(roleId))
                {
                    RoleIdTotalScoreDict[roleId] = 0;
                }
            }

            return result;
        }

        public void RemoveRole(GameClient client)
        {
            int roleId = client.ClientData.RoleID;
            lock (Mutex)
            {
                RoleIdSavedScoreDict.Remove(roleId);
            }
        }

        public bool CantiansRole(GameClient client)
        {
            int roleId = client.ClientData.RoleID;
            lock (Mutex)
            {
                return RoleIdTotalScoreDict.ContainsKey(roleId);
            }
        }

        public void AddRoleScoreAll(int addScore)
        {
            lock (Mutex)
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
            lock (Mutex)
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
            lock (Mutex)
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
