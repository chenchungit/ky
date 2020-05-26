using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    public class AchievementRuneSpecialData
    {
        /// <summary>
        /// 额外属性id
        /// </summary>
        [ProtoMember(2)]
        public int SpecialID = 0;

        /// <summary>
        /// 符文id
        /// </summary>
        [ProtoMember(1)]
        public int RuneID = 0;

        /// <summary>
        /// 卓越一击
        /// </summary>
        [ProtoMember(3)]
        public double ZhuoYue = 0;

        /// <summary>
        /// 抵抗卓越一击
        /// </summary>
        [ProtoMember(4)]
        public double DiKang = 0;

        
    }
}
