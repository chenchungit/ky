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
    public class HuangChengMapInfoData
    {
        /// <summary>
        /// 战斗结束的时间
        /// </summary>
        [ProtoMember(1)]
        public long FightingEndTime = 0;

        /// <summary>
        /// 舍利持有者的角色ID
        /// </summary>
        [ProtoMember(2)]
        public int HuangDiRoleID = 0;

        /// <summary>
        /// 舍利持有者的角色名称
        /// </summary>
        [ProtoMember(3)]
        public string HuangDiRoleName = "";

        /// <summary>
        /// 舍利持有者的战盟名称
        /// </summary>
        [ProtoMember(4)]
        public string HuangDiBHName = "";

        /// <summary>
        /// 皇城的战斗状态
        /// </summary>
        [ProtoMember(5)]
        public int FightingState = 0;

        /// <summary>
        /// 下场王城争霸赛时间
        /// </summary>
        [ProtoMember(6)]
        public String NextBattleTime = "";

        /// <summary>
        /// 皇城战盟ID
        /// </summary>
        [ProtoMember(7)]
        public int WangZuBHid = -1;
    }
}
