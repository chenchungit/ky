using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace GameServer.Logic
{
    /// <summary>
    /// 基金
    /// </summary>
    public class FundInfo
    {
        /// <summary>
        /// 基金id
        /// </summary>
        public int FundID = 0;

        /// <summary>
        /// 基金类型
        /// </summary>
        public int FundType = 0;

        /// <summary>
        /// 最小vip等级
        /// </summary>
        public int MinVip = 0;

        /// <summary>
        /// 下一阶段（0=结束）
        /// </summary>
        public int NextID = 0;

        /// <summary>
        /// 购买价格（钻石）
        /// </summary>
        public int Price = 0;
    }

    /// <summary>
    /// 基金奖励
    /// </summary>
    public class FundAwardInfo
    {
        /// <summary>
        /// 奖励id
        /// </summary>
        public int AwardID = 0;

        /// <summary>
        /// 基金id
        /// </summary>
        public int FundID = 0;

        /// <summary>
        /// 基金类型
        /// </summary>
        public int FundType = 0;

        /// <summary>
        /// 是否绑定
        /// </summary>
        public bool AwardIsBind = true;

        /// <summary>
        /// 奖励金额（钻石）
        /// </summary>
        public int AwardCount = 0;

        /// <summary>
        /// 转生,登陆天数,累计充值（钻石）
        /// </summary>
        public int Value1 = 0;

        /// <summary>
        /// 等级,累计消费（钻石）
        /// </summary>
        public int Value2 = 0;

        ///// <summary>
        ///// 转生
        ///// </summary>
        //public int ChangeLife = 0;

        ///// <summary>
        ///// 等级
        ///// </summary>
        //public int Level = 0;

        ///// <summary>
        ///// 登陆天数
        ///// </summary>
        //public int LoginCount = 0;

        ///// <summary>
        ///// 累计充值（钻石）
        ///// </summary>
        //public int MoneyAdd = 0;

        ///// <summary>
        ///// 累计消费（钻石）
        ///// </summary>
        //public int MoneyCost = 0;
    }
}
