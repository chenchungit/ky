using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    // 帮会管理类 [1/17/2014 LiaoWei]
    class GuildManager
    {
        /// <summary>
        /// 帮会申请列表
        /// </summary>
        public static Dictionary<int, List<int>> m_GuildApplyDic = new Dictionary<int, List<int>>();    // key:帮会ID  Value: 申请者列表

        /// <summary>
        /// 添加申请者列表中的元素
        /// </summary>
        public static void AddGuildApply(int nID, int nRole)
        {
            lock (m_GuildApplyDic)
            {
                List<int> lListData = null;
                lListData = m_GuildApplyDic[nID];

                if (lListData == null)
                    lListData = new List<int>();

                lListData.Add(nRole);

                m_GuildApplyDic[nID] = lListData;
            }
        }

        /// <summary>
        /// 从申请者列表中删除一个元素
        /// </summary>
        public static void RemoveGuildApply(int nID, int nRole)
        {
            lock (m_GuildApplyDic)
            {
                List<int> lListData = null;
                lListData = m_GuildApplyDic[nID];

                if (lListData == null)
                    return;

                lListData.Remove(nRole);

                m_GuildApplyDic[nID] = lListData;
            }
        }
    }
}
