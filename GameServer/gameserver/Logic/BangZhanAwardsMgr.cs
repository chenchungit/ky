using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 皇城，血战地图，领地战地图定时给予奖励
    /// </summary>
    public class BangZhanAwardsMgr
    {
        /// <summary>
        /// 根据级别奖励的经验数组
        /// </summary>
        private static long[] ExpByLevels = null;

        /// <summary>
        /// 根据级别奖励的荣誉数组
        /// </summary>
        private static int[] RongYuByLevels = null;

        /// <summary>
        /// 清空缓存的奖励
        /// </summary>
        public static void ClearAwardsByLevels()
        {
            ExpByLevels = null;
            RongYuByLevels = null;
        }

        /// <summary>
        /// 根据角色的级别加载经验
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static long GetExpByLevel(int level)
        {
            long[] expByLevels = ExpByLevels;
            if (null == expByLevels)
            {
                SystemXmlItem systemXmlItem = null;
                expByLevels = new long[Data.LevelUpExperienceList.Length - 1];
                for (int i = 1; i < expByLevels.Length; i++)
                {
                    if (GameManager.systemBangZhanAwardsMgr.SystemXmlItemDict.TryGetValue(i, out systemXmlItem))
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

            return expByLevels[level];
        }

        /// <summary>
        /// 根据角色的级别加载荣誉
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static int GetRongYuByLevel(int level)
        {
            int[] rongYuByLevels = RongYuByLevels;
            if (null == rongYuByLevels)
            {
                SystemXmlItem systemXmlItem = null;
                rongYuByLevels = new int[Data.LevelUpExperienceList.Length - 1];
                for (int i = 1; i < rongYuByLevels.Length; i++)
                {
                    if (GameManager.systemBangZhanAwardsMgr.SystemXmlItemDict.TryGetValue(i, out systemXmlItem))
                    {
                        rongYuByLevels[i] = Global.GMax(0, systemXmlItem.GetIntValue("RongYu"));
                    }
                }

                RongYuByLevels = rongYuByLevels;
            }

            if (level <= 0 || level >= RongYuByLevels.Length)
            {
                return 0;
            }

            return rongYuByLevels[level];
        }

        /// 处理用户的在场的时间经验奖励
        private static void ProcessAddRoleExperience(GameClient client)
        {
            long exp = BangZhanAwardsMgr.GetExpByLevel(client.ClientData.Level);
            if (exp <= 0) return;

            //处理角色经验
            GameManager.ClientMgr.ProcessRoleExperience(client, exp, true, false);
        }

        /// 处理用户的在场的时间荣誉奖励
        private static void ProcessAddRoleRongYu(GameClient client)
        {
            int rongYu = BangZhanAwardsMgr.GetRongYuByLevel(client.ClientData.Level);
            if (rongYu <= 0) return;

            //处理角色经验
            GameManager.ClientMgr.ModifyRongYuValue(client, rongYu, true, true);
        }

        /// <summary>
        ///  处理皇城，血战地府，领地战中的定时收益
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessBangZhanAwards(GameClient client)
        {
            // 注释掉 Begin [4/29/2014 LiaoWei]
            /// 处理用户的在场的时间经验奖励
            //ProcessAddRoleExperience(client);

            /// 处理用户的在场的时间荣誉奖励
            //ProcessAddRoleRongYu(client);
            // 注释掉 End [4/29/2014 LiaoWei]
        }
    }
}
