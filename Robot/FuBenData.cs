using System.Collections.Generic;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 副本数据
    /// </summary>
    [ProtoContract]
    public class FuBenData
    {
        /// <summary>
        /// 副本的ID
        /// </summary>
        [ProtoMember(1)]
        public int FuBenID;

        /// <summary>
        /// 日期ID
        /// </summary>
        [ProtoMember(2)]
        public int DayID;

        /// <summary>
        /// 当日进入的次数
        /// </summary>
        [ProtoMember(3)]
        public int EnterNum;

        /// <summary>
        /// 最快通关时间 副本改造 增加存盘字段 begin[11/15/2013 LiaoWei]
        /// </summary>
        [ProtoMember(4)]
        public int QuickPassTimer;

        /// <summary>
        /// 今日完成次数
        /// </summary>
        [ProtoMember(5)]
        public int FinishNum;
    }
}
