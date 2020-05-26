using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// NPC的任务状态
    /// </summary>
    [ProtoContract]
    public class NPCTaskState
    {
        /// <summary>
        /// NPC的ID
        /// </summary>
        [ProtoMember(1)]
        public int NPCID
        {
            get;
            set;
        }

        /// <summary>
        /// 任务状态
        /// </summary>
        [ProtoMember(2)]
        public int TaskState
        {
            get;
            set;
        }
    }
}
