using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace GameServer.Logic.Spread
{
    /// <summary>
    /// 推广数据
    /// </summary>
    [ProtoContract]
    public class SpreadData
    {
        [ProtoMember(1)]
        public bool IsOpen = false;

        [ProtoMember(2)]
        public string SpreadCode = "";

        [ProtoMember(3)]
        public string VerifyCode = "";

        [ProtoMember(4)]
        public int CountRole = 0;

        [ProtoMember(5)]
        public int CountVip = 0;

        [ProtoMember(6)]
        public int CountLevel = 0;

        [ProtoMember(7)]
        public int State = 0;

        /// <summary>
        /// 已领取推广奖励（vip,level,verify）
        /// (奖励类型，领取次数)
        /// </summary>
        [ProtoMember(8)]
        public Dictionary<int, String> AwardDic = new Dictionary<int, String>();

        /// <summary>
        /// 人数奖励领取状态
        /// (数量，领取状态)
        /// </summary>
        [ProtoMember(9)]
        public Dictionary<int, int> AwardCountDic = new Dictionary<int, int>();
    }

    /// <summary>
    /// 推广验证数据
    /// </summary>
    public class SpreadVerifyData
    {
        public string VerifyCode = "";

        public string Tel = "";

        public int TelCode = 0;

        public DateTime VerifyTime = DateTime.Now;

        public DateTime TelTime = DateTime.Now;
    }


   
}
