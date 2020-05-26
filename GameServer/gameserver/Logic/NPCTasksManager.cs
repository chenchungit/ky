using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// NPC和任务映射管理
    /// </summary>
    public class NPCTasksManager
    {
        /// <summary>
        /// 源NPC和任务的映射
        /// </summary>
        private Dictionary<int, List<int>> _SourceNPCTasksDict = null;

        /// <summary>
        /// 源NPC和任务的映射
        /// </summary>
        public Dictionary<int, List<int>> SourceNPCTasksDict
        {
            get { return _SourceNPCTasksDict; }
        }

        /// <summary>
        /// 添加源NPC任务
        /// </summary>
        /// <param name="npcID"></param>
        /// <param name="taskID"></param>
        private void AddSourceNPCTask(int npcID, int taskID, Dictionary<int, List<int>> sourceNPCTasksDict)
        {
            List<int> taskList = null;
            if (!sourceNPCTasksDict.TryGetValue(npcID, out taskList))
            {
                taskList = new List<int>();
                sourceNPCTasksDict[npcID] = taskList;
            }

            if (-1 == taskList.IndexOf(taskID))
            {
                taskList.Add(taskID);
            }
        }

        /// <summary>
        /// 目标NPC和任务的映射
        /// </summary>
        private Dictionary<int, List<int>> _DestNPCTasksDict = null;

        /// <summary>
        /// 目标NPC和任务的映射
        /// </summary>
        public Dictionary<int, List<int>> DestNPCTasksDict
        {
            get { return _DestNPCTasksDict; }
        }

        /// <summary>
        /// 添加目标NPC任务
        /// </summary>
        /// <param name="npcID"></param>
        /// <param name="taskID"></param>
        private void AddDestNPCTask(int npcID, int taskID, Dictionary<int, List<int>> destNPCTasksDict)
        {
            List<int> taskList = null;
            if (!destNPCTasksDict.TryGetValue(npcID, out taskList))
            {
                taskList = new List<int>();
                destNPCTasksDict[npcID] = taskList;
            }

            if (-1 == taskList.IndexOf(taskID))
            {
                taskList.Add(taskID);
            }
        }

        /// <summary>
        /// 加载映射
        /// </summary>
        /// <param name="systemTasks"></param>
        public void LoadNPCTasks(SystemXmlItems systemTasks)
        {
            Dictionary<int, List<int>> sourceNPCTasksDict = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> destNPCTasksDict = new Dictionary<int, List<int>>();
            foreach (var key in systemTasks.SystemXmlItemDict.Keys)
            {
                SystemXmlItem systemTask = systemTasks.SystemXmlItemDict[key];
                AddSourceNPCTask(systemTask.GetIntValue("SourceNPC"), systemTask.GetIntValue("ID"), sourceNPCTasksDict);
                AddDestNPCTask(systemTask.GetIntValue("DestNPC"), systemTask.GetIntValue("ID"), destNPCTasksDict);
            }

            _SourceNPCTasksDict = sourceNPCTasksDict;
            _DestNPCTasksDict = destNPCTasksDict;
        }
    }
}
