using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 领地地图数据
    /// </summary>
    [ProtoContract]
    public class LingDiMapInfoData
    {
        /// <summary>
        /// 战斗结束的时间
        /// </summary>
        [ProtoMember(1)]
        public long FightingEndTime = 0;

        /// <summary>
        /// 战斗开始的时间
        /// </summary>
        [ProtoMember(2)]
        public long FightingStartTime = 0;

        /// <summary>
        /// 旗座对应的战盟的名称
        /// </summary>
        [ProtoMember(3)]
        public Dictionary<int, string> BHNameDict = null;
    }
}
