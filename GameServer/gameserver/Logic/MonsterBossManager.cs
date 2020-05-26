#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;

namespace GameServer.Logic
{
    /// <summary>
    /// 怪物野外Boss管理
    /// </summary>
    public class MonsterBossManager
    {
        /// <summary>
        /// 野外Boss列表
        /// </summary>
        private static List<Monster> BossList = new List<Monster>();

        /// <summary>
        /// 添加野外Boss
        /// </summary>
        /// <param name="monster"></param>
        public static void AddBoss(Monster monster)
        {
            BossList.Add(monster);
        }

        /// <summary>
        /// 获取野外Boss的字典
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, BossData> GetBossDictData()
        {
            Dictionary<int, BossData> dict = new Dictionary<int, BossData>();

            BossData bossData = null;
            Monster monster = null;
            for (int i = 0; i < BossList.Count; i++)
            {
                monster = BossList[i];

                string nextBirthTime = "";
                if (!monster.Alive)
                {
                    nextBirthTime = monster.MonsterZoneNode.GetNextBirthTimePoint();
                }

                bossData = new BossData()
                {
                    MonsterID = monster.RoleID,
#if ___CC___FUCK___YOU___BB___
                   ExtensionID = monster.XMonsterInfo.MonsterId,
#else
                    ExtensionID = monster.MonsterInfo.ExtensionID,
#endif

                    KillMonsterName = monster.WhoKillMeName,
                    KillerOnline = (null != GameManager.ClientMgr.FindClient(monster.WhoKillMeID)) ? 1 : 0,
                    NextTime = nextBirthTime,
                };

                dict[bossData.ExtensionID] = bossData;
            }

            return dict;
        }

        // 角色改名
        public static void OnChangeName(int roleId, string oldName, string newName)
        {
            for (int i = 0; i < BossList.Count; ++i)
            {
                Monster m = BossList[i];
                if (m.WhoKillMeID == roleId)
                {
                    // 暂时不需要用Format
                    m.WhoKillMeName = newName;
                }
            }
        }

    }
}
