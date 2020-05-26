using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 技能数据
    /// </summary>
    [ProtoContract]
    public class SkillData
    {
        /// <summary>
        /// 数据库ID
        /// </summary>
        [ProtoMember(1)]
        public int DbID;

        /// <summary>
        /// 技能类型ID
        /// </summary>
        [ProtoMember(2)]
        public int SkillID;

        /// <summary>
        /// 技能类型级别
        /// </summary>
        [ProtoMember(3)]
        public int SkillLevel;

        /// <summary>
        /// 熟练度
        /// </summary>
        [ProtoMember(4)]
        public int UsedNum;
    }
}

