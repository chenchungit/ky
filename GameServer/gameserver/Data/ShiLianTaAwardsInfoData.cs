using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 试练塔副本经验奖励的数据
    /// </summary>
    [ProtoContract]
    public class ShiLianTaAwardsInfoData
    {
        /// <summary>
        /// 本层总怪物数量
        /// </summary>
        [ProtoMember(1)]
        public int CurrentFloorTotalMonsterNum = 0;

        /// <summary>
        /// 本层经验奖励
        /// </summary>
        [ProtoMember(2)]
        public int CurrentFloorExperienceAward = 0;

        /// <summary>
        /// 下层需要道具
        /// </summary>
        [ProtoMember(3)]
        public int NextFloorNeedGoodsID = 0;

        /// <summary>
        /// 下层需要道具数量
        /// </summary>
        [ProtoMember(4)]
        public int NextFloorNeedGoodsNum = 0;

        /// <summary>
        /// 下层经验奖励
        /// </summary>
        [ProtoMember(5)]
        public int NextFloorExperienceAward = 0;
    }
}
