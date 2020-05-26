using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 开服在线奖励信息数据
    /// </summary>
    [ProtoContract]
    public class KaiFuOnlineInfoData
    {
        /// <summary>
        /// 自己在线的天位状态
        /// </summary>
        [ProtoMember(1)]
        public int SelfDayBit = 0;

        /// <summary>
        /// 自己在线的天在线时长
        /// </summary>
        [ProtoMember(2)]
        public List<int> SelfDayOnlineSecsList;

        /// <summary>
        /// 开区在线奖励列表
        /// </summary>
        [ProtoMember(3)]
        public List<KaiFuOnlineAwardData> KaiFuOnlineAwardDataList;
    }
}
