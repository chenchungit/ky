using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 每日的已经冲穴次数数据
    /// </summary>
    [ProtoContract]    
    public class DailyJingMaiData
    {
        /// <summary>
        /// 冲穴的日子
        /// </summary>
        [ProtoMember(1)]
        public string JmTime = "";

        /// <summary>
        /// 冲穴的次数
        /// </summary>
        [ProtoMember(2)]
        public int JmNum = 0;
    }
}
