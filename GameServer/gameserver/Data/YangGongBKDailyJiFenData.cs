using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 杨公宝库日积分数据
    /// </summary>
    [ProtoContract]
    public class YangGongBKDailyJiFenData
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        //[ProtoMember(1)]   
        //public int RoleID = 0;

        /// <summary>
        /// day ID
        /// </summary>
        [ProtoMember(1)]           
        public int DayID = 0;

        /// <summary>
        /// 积分
        /// </summary>
        [ProtoMember(2)]           
        public int JiFen = 0;

        /// <summary>
        /// 奖励历史
        /// </summary>
        [ProtoMember(3)]
        public long AwardHistory = 0;
    }
}
