using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 怪物名称管理
    /// </summary>
    public class MonsterNameManager
    {
        /// <summary>
        /// 怪物的ID到怪物名称的映射
        /// </summary>
        private static Dictionary<int, string> _MonsterID2NameDict = new Dictionary<int, string>(1000);

        /// <summary>
        /// 添加怪物名称
        /// </summary>
        /// <param name="monsterID"></param>
        /// <param name="monsterName"></param>
        public static void AddMonsterName(int monsterID, string monsterName)
        {
            lock (_MonsterID2NameDict)
            {
                _MonsterID2NameDict[monsterID] = monsterName;
            }
        }

        /// <summary>
        /// 获取怪物名称
        /// </summary>
        /// <param name="monsterID"></param>
        /// <returns></returns>
        public static string GetMonsterName(int monsterID)
        {
            string monsterName = null;
            lock (_MonsterID2NameDict)
            {
                if (_MonsterID2NameDict.TryGetValue(monsterID, out monsterName))
                {
                    return monsterName;
                }
            }

            return "";
        }
    }
}
