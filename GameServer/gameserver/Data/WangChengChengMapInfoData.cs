using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 王城地图数据
    /// </summary>
    [ProtoContract]
    public class WangChengMapInfoData
    {
        /// <summary>
        /// 战斗结束的时间
        /// </summary>
        [ProtoMember(1)]
        public long FightingEndTime = 0;

        /// <summary>
        /// 王城的战斗状态
        /// </summary>
        [ProtoMember(2)]
        public int FightingState = 0;

        /// <summary>
        /// 下场王城争霸赛时间
        /// </summary>
        [ProtoMember(3)]
        public String NextBattleTime = "";

        /// <summary>
        /// 王城战盟名称
        /// </summary>
        [ProtoMember(4)]
        public string WangZuBHName = "";

        /// <summary>
        /// 王城战盟ID
        /// </summary>
        [ProtoMember(5)]
        public int WangZuBHid = -1;
    }
}
