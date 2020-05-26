using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Data;

namespace GameServer.Logic.YueKa
{
    /// <summary>
    /// 在服务器端使用的月卡详细信息
    /// 该结构对应t_roleparams中pname=YueKaInfo的pvalue
    /// pvalue格式为"1,2,3,4,5"
    /// pvalue[1] 0表示没有月卡，1表示有月卡
    /// pvalue[2] 月卡相对于2011年11月11日的起始天数偏移
    /// pvalue[3] 月卡相对于2011年11月11日的结束天数偏移
    /// pvalue[4] 当前月卡领奖天数，相对于相对于2011年11月11日的天数偏移
    /// pvalue[5] 当前领奖信息 0000111 0表示没有领奖，1表示已领奖
    /// </summary>
    public class YueKaDetail
    {
        /// <summary>
        /// 是否有月卡，0表示有月卡，1表示没有月卡
        /// 如果没有月卡，则该结构其余字段没有意义
        /// </summary>
        public int HasYueKa = 0;

        /// <summary>
        /// 月卡的起始天数，相对于2011-11-11日的天数偏移
        /// </summary>
        public int BegOffsetDay = 0;

        /// <summary>
        /// 月卡的终止天数，相对于2011-11-11日的天数偏移
        /// </summary>
        public int EndOffsetDay = 0;

        /// <summary>
        /// 月卡的当前领奖天数，相对于2011-11-11日的天数偏移
        /// </summary>
        public int CurOffsetDay = 0;

        /// <summary>
        /// 月卡的当前领奖信息
        /// "0001010" 0表示没有领取，1表示已领取
        /// 如果有多张月卡，那么只存储当前月卡周期的领奖信息
        /// </summary>
        public string AwardInfo = "";

        public YueKaDetail()
        {
            HasYueKa = 0;
            BegOffsetDay = 0;
            EndOffsetDay = 0;
            CurOffsetDay = 0;
            AwardInfo = "";
        }

        /// <summary>
        /// 把该结构转化成YueKaData用于通知客户端
        /// </summary>
        /// <returns></returns>
        public YueKaData ToYueKaData()
        {
            YueKaData ykData = new YueKaData();
            ykData.HasYueKa = HasYueKa == 1 ? true : false;
            ykData.CurrDay = CurDayOfPerYueKa();
            ykData.AwardInfo = AwardInfo;
            ykData.RemainDay = RemainDayOfYueKa();
            return ykData;
        }

        /// <summary>
        /// 从"1:2:3:4:5" 这个格式初始化
        /// 用于从数据库t_roleparams读取后初始化
        /// </summary>
        /// <param name="str"></param>
        public void ParseFrom(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            string[] fields = str.Split(',');
            if (fields.Length == 5)
            {
                HasYueKa = Convert.ToInt32(fields[0]);
                BegOffsetDay = Convert.ToInt32(fields[1]);
                EndOffsetDay = Convert.ToInt32(fields[2]);
                CurOffsetDay = Convert.ToInt32(fields[3]);
                AwardInfo = fields[4];
            }
        }

        /// <summary>
        /// 转化为存储到t_roleparams中的格式化字符串
        /// </summary>
        /// <returns></returns>
        public string SerializeToString()
        {
            if (HasYueKa == 0)
            {
                return "0,0,0,0,0";
            }
            else
            {
                return string.Format("{0},{1},{2},{3},{4}", 1,
                    BegOffsetDay, EndOffsetDay, CurOffsetDay, AwardInfo);
            }
        }

        /// <summary>
        /// 计算当前是月卡中的第几天，范围1-30
        /// 调用前必须保证CurOffsetDay已经被正确处理
        /// </summary>
        public int CurDayOfPerYueKa()
        {
            if (HasYueKa == 0)
            {
                return 0;
            }
            else
            {
                return ((CurOffsetDay - BegOffsetDay) % YueKaManager.DAYS_PER_YUE_KA) + 1;
            }
        }

        /// <summary>
        /// 计算月卡剩余天数, 如果买了多张月卡，那么可能大于30
        /// 调用前必须保证CurOffsetDay已经被正确处理
        /// </summary>
        /// <returns></returns>
        public int RemainDayOfYueKa()
        {
            if (HasYueKa == 0)
            {
                return 0;
            }
            else
            {
                return EndOffsetDay - CurOffsetDay;
            }
        }
    }
}
