using System;
using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Shapes;
using System.Collections.Generic;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// NPC角色的数据定义
    /// </summary>
    [ProtoContract]  
    public class NPCData
    {
        /// <summary>
        /// 当前的地图编号
        /// </summary>
        [ProtoMember(1)]
        public int MapCode = 0;

        /// <summary>
        /// 当前的角色ID
        /// </summary>
        [ProtoMember(2)]
        public int RoleID = 0;

        /// <summary>
        /// 当前的NPCID
        /// </summary>
        [ProtoMember(3)]
        public int NPCID = 0;

        /// <summary>
        /// 尚未接受的任务列表
        /// </summary>
        [ProtoMember(4)]
        public List<int> NewTaskIDs = null;

        /// <summary>
        /// 系统功能列表
        /// </summary>
        [ProtoMember(5)]
        public List<int> OperationIDs = null;

        /// <summary>
        /// NPC功能脚本列表
        /// </summary>
        [ProtoMember(6)]
        public List<int> ScriptIDs = null;

        /// <summary>
        /// 扩展ID
        /// </summary>
        [ProtoMember(7)]
        public int ExtensionID = 0;

        /// <summary>
        /// 尚未接受的任务列表已经完成的次数
        /// </summary>
        [ProtoMember(8)]
        public List<int> NewTaskIDsDoneCount = null;
    }
}
