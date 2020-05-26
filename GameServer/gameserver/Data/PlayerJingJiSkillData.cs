using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 玩家竞技场技能数据
    /// </summary>
    [ProtoContract]
    public class PlayerJingJiSkillData
    {
        /// <summary>
        /// 技能类型ID
        /// </summary>
        [ProtoMember(1)]
        public int skillID;

        /// <summary>
        /// 技能类型级别
        /// </summary>
        [ProtoMember(2)]
        public int skillLevel;
    }
}
