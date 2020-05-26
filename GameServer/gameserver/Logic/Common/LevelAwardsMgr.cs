using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 皇城，血战地图，领地战地图定时给予奖励
    /// </summary>
    public class LevelAwardsMgr
    {
        /// <summary>
        /// 皇城，血战地府，领地战定时给予的收益表
        /// </summary>
        public SystemXmlItems systemLevelAwardsXml = new SystemXmlItems();

        /// <summary>
        /// 根据级别奖励的经验数组
        /// </summary>
        private long[] ExpByLevels = null;

        /// <summary>
        /// 根据级别奖励的荣誉数组
        /// </summary>
        private int[] RongYuByLevels = null;

        /// <summary>
        /// 清空缓存的奖励
        /// </summary>
        private void ClearAwardsByLevels()
        {
            ExpByLevels = null;
            RongYuByLevels = null;
        }

        public void LoadFromXMlFile(string fullFileName, string rootName, string keyName, int resType = 0)
        {
            ClearAwardsByLevels();
            systemLevelAwardsXml.LoadFromXMlFile(fullFileName, rootName, keyName, resType);
        }

        /// <summary>
        /// 根据角色的级别加载经验
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private long GetExpByLevel(GameClient client, int level)
        {
            long[] expByLevels = ExpByLevels;
            if (null == expByLevels)
            {
                SystemXmlItem systemXmlItem = null;
                expByLevels = new long[Data.LevelUpExperienceList.Length];
                for (int i = 1; i < expByLevels.Length; i++)
                {
                    if (systemLevelAwardsXml.SystemXmlItemDict.TryGetValue(i, out systemXmlItem))
                    {
                        expByLevels[i] = Global.GMax(0, systemXmlItem.GetIntValue("Experience"));
                    }
                }

                ExpByLevels = expByLevels;
            }

            if (level <= 0 || level >= ExpByLevels.Length)
            {
                return 0;
            }

            long addExp = expByLevels[level];
            return Global.GetExpMultiByZhuanShengExpXiShu(client, addExp); 
        }

        /// <summary>
        /// 根据角色的级别加载荣誉
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private int GetRongYuByLevel(int level)
        {
            return 0;
        }

        /// 处理用户的在场的时间经验奖励
        private void ProcessAddRoleExperience(GameClient client)
        {
            long exp = GetExpByLevel(client, client.ClientData.Level);
            if (exp <= 0) return;

            //处理角色经验
            GameManager.ClientMgr.ProcessRoleExperience(client, exp, true, false, true);
        }

        /// 处理用户的在场的时间荣誉奖励
        private void ProcessAddRoleRongYu(GameClient client)
        {
            int rongYu = GetRongYuByLevel(client.ClientData.Level);
            if (rongYu <= 0) return;

            //处理角色经验
            GameManager.ClientMgr.ModifyRongYuValue(client, rongYu, true, true);
        }

        /// <summary>
        ///  处理皇城，血战地府，领地战中的定时收益
        /// </summary>
        /// <param name="client"></param>
        public void ProcessBangZhanAwards(GameClient client)
        {
            // 处理用户的在场的时间经验奖励
            ProcessAddRoleExperience(client);

            // 处理用户的在场的时间荣誉奖励
            //ProcessAddRoleRongYu(client);
        }
    }
}
