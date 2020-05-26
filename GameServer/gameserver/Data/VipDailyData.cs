using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// vip每日数据
    /// </summary>
    [ProtoContract]
    public class VipDailyData
    {
        /// <summary>
        /// vip特权类型
        /// </summary>
        [ProtoMember(1)]
        public int PriorityType = 0;

        /// <summary>
        /// 日ID
        /// </summary>
        [ProtoMember(2)]
        public int DayID = 0;

        /// <summary>
        /// 已经使用次数
        /// </summary>
        [ProtoMember(3)]
        public int UsedTimes = 0;
    }
}
