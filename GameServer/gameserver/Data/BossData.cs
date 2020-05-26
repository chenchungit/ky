using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 野外Boss数据
    /// </summary>
    [ProtoContract]
    public class BossData
    {
        /// <summary>
        /// 怪物的ID
        /// </summary>
        [ProtoMember(1)]
        public int MonsterID = 0;

        /// <summary>
        /// 怪物的ExtensionID
        /// </summary>
        [ProtoMember(2)]
        public int ExtensionID = 0;

        /// <summary>
        /// 怪物上一次击杀者
        /// </summary>
        [ProtoMember(3)]
        public string KillMonsterName = "";

        /// <summary>
        /// 怪物上一次击杀者的在线状态
        /// </summary>
        [ProtoMember(4)]
        public int KillerOnline = 0;

        /// <summary>
        /// 怪物下次刷新时间(空表示已经刷新)
        /// </summary>
        [ProtoMember(5)]
        public string NextTime = "";
    }
}
