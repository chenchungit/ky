using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Windows;
using System.Collections;

namespace GameServer.Logic
{
    // 天使神殿场景信息类 [3/23/2014 LiaoWei]
    public class AngelTempleSceneInfo
    {
        /// <summary>
        /// 场景mapcode
        /// </summary>
        public int m_nMapCode = 0;

        /// <summary>
        /// 天使怪的ID
        /// </summary>
        public int m_nAngelMonsterID = 0;

        /// <summary>
        /// 场景开始时间
        /// </summary>
        public long m_lPrepareTime = 0;

        /// <summary>
        /// 场景战斗开始时间
        /// </summary>
        public long m_lBeginTime = 0;

        /// <summary>
        /// 场景战斗结束时间
        /// </summary>
        public long m_lEndTime = 0;

        /// <summary>
        /// end标记
        /// </summary>
        public int m_bEndFlag = 0;

        /// <summary>
        /// 场景状态
        /// </summary>
        public AngelTempleStatus m_eStatus = AngelTempleStatus.FIGHT_STATUS_NULL;

        /// <summary>
        /// 本状态结束时间
        /// </summary>
        public long m_lStatusEndTime = 0;

        /// <summary>
        /// 玩家人数
        /// </summary>
        public int m_nPlarerCount = 0;

        /// <summary>
        /// 击杀BOSS的玩家
        /// </summary>
        public int m_nKillBossRole = 0;

        /// <summary>
        /// 同步信息时的TICK
        /// /// </summary>
        public long m_NotifyInfoTick = 0;

        public void CleanAll()
        {
            m_NotifyInfoTick = 0;
            m_bEndFlag = 0;
            m_nPlarerCount = 0;
            m_nKillBossRole = 0;
            m_nAngelMonsterID = 0;
            m_lPrepareTime = 0;
            m_lBeginTime = 0;
            m_lEndTime = 0;
            m_eStatus = AngelTempleStatus.FIGHT_STATUS_NULL;
        }
    }

    public class AngelTemplePointInfo : IComparer<AngelTemplePointInfo>
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public int m_RoleID = 0;

        /// <summary>
        /// 击杀血量
        /// </summary>
        public long m_DamagePoint = 0;

        /// <summary>
        /// 是否离场状态
        /// </summary>
        public bool LeaveScene = false;

        /// <summary>
        /// 排名
        /// </summary>
        public int Ranking = -1;

        /// <summary>
        /// 角色名
        /// </summary>
        public string m_RoleName;

        /// <summary>
        /// 玩家奖励领取标记
        /// </summary>
        public int m_GetAwardFlag = 0;

        /// <summary>
        /// 幸运奖排名
        /// </summary>
        //public int m_LuckPaiMingID = 0;
        public string m_LuckPaiMingName = "";

        /// <summary>
        /// 物品奖励列表(所有的)
        /// </summary>
        public AwardsItemList GoodsList = new AwardsItemList();

        /// <summary>
        /// 排名奖排名
        /// </summary>
        public int m_AwardPaiMing = 0;

        /// <summary>
        /// 声望奖励
        /// </summary>
        public int m_AwardShengWang = 0;

        /// <summary>
        /// 金币奖励
        /// </summary>
        public int m_AwardGold = 0;

        /// <summary>
        /// 用于从大到小排列的比较函数(倒排)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(AngelTemplePointInfo x, AngelTemplePointInfo y)
        {
            return Compare_static(x, y);
        }

        public static int Compare_static(AngelTemplePointInfo x, AngelTemplePointInfo y)
        {
            if (x == y)
            {
                return 0;
            }
            else if (x != null && y != null)
            {
                long ret = y.m_DamagePoint - x.m_DamagePoint;
                if (ret > 0)
                {
                    return 1;
                }
                else if (ret == 0)
                {
                    return y.Ranking - x.Ranking;
                }
                else
                {
                    return -1;
                }
            }
            else if (x == null)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// 排序比较函数
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public int CompareTo(AngelTemplePointInfo y)
        {
            if (this == y)
            {
                return 0;
            }
            else if (y == null)
            {
                return -1;
            }
            else
            {
                long ret = y.m_DamagePoint - this.m_DamagePoint;
                if (ret > 0)
                {
                    return 1;
                }
                else if (ret == 0)
                {
                    return y.Ranking - Ranking;
                }
                else
                {
                    return -1;
                }

            }
        }
    }
}
