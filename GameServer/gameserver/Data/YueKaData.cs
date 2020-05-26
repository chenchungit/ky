using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    public class YueKaData
    {
        /// <summary>
        /// 是否有月卡, 只有为true的时候, 以下字段才有效
        /// </summary>
        [ProtoMember(1)]
        public bool HasYueKa;

        /// <summary>
        /// 当前是30天中的第几天
        /// </summary>
        [ProtoMember(2)]
        public int CurrDay;

        /// <summary>
        /// 领奖信息, 每个字符存储0(未领奖)或者1(已领奖)
        /// 服务器保证AwardInfo.Length == CurrDay
        /// 例如 CurrDay为3, AwardInfo = "011", 则表示第一天未领取, 第二天已领取, 第三天已领取
        /// </summary>
        [ProtoMember(3)]
        public string AwardInfo;

        /// <summary>
        /// 月卡剩余天数,如果有多个月的月卡，则该值可能>30
        /// </summary>
        [ProtoMember(4)]
        public int RemainDay;

        /// <summary>
        /// 构造函数, 特别要注意的是只有HasYueKa == true的时候，其余字段才有效
        /// </summary>
        public YueKaData()
        {
            HasYueKa = false;
            CurrDay = 0;
            AwardInfo = "";
            RemainDay = 0;
        }
    }
}