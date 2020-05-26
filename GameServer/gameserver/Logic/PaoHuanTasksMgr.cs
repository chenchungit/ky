using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;
using Server.Tools;

namespace GameServer.Logic
{
    public class PaoHuanTaskItem
    {
        /// <summary>
        /// 跑环的任务ID
        /// </summary>
        public int TaskID;

        /// <summary>
        /// 添加的时间
        /// </summary>
        public string AddDateTime;
    };

    /// <summary>
    /// 日跑环任务管理
    /// </summary>
    public class PaoHuanTasksMgr
    {
        /// <summary>
        /// 跑环历史记录字典
        /// </summary>
        private static Dictionary<string, PaoHuanTaskItem> PaoHuanHistDict = new Dictionary<string, PaoHuanTaskItem>();

        /// <summary>
        /// 设置跑环的历史记录ID
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static void SetPaoHuanHistTaskID(int roleID, int taskClass, int taskID)
        {
            lock (PaoHuanHistDict)
            {
                PaoHuanTaskItem paoHuanTaskItem = new PaoHuanTaskItem()
                {
                    TaskID = taskID,
                    AddDateTime = TimeUtil.NowDateTime().ToString("yyyy-MM-dd"),
                };

                string key = string.Format("{0}_{1}", roleID, taskClass);
                PaoHuanHistDict[key] = paoHuanTaskItem;
            }
        }

        /// <summary>
        /// 查找跑环的历史记录ID
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static int FindPaoHuanHistTaskID(int roleID, int taskClass)
        {
            string today = TimeUtil.NowDateTime().ToString("yyyy-MM-dd");
            PaoHuanTaskItem paoHuanTaskItem = null;
            lock (PaoHuanHistDict)
            {
                string key = string.Format("{0}_{1}", roleID, taskClass);
                if (!PaoHuanHistDict.TryGetValue(key, out paoHuanTaskItem))
                {
                    return -1;
                }

                if (today != paoHuanTaskItem.AddDateTime)
                {
                    return -1;
                }

                return paoHuanTaskItem.TaskID;
            }
        }
    }
}
