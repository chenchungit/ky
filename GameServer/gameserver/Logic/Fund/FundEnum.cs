using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 基金类型
    /// </summary>
    public enum EFund
    {
        ChangeLife = 1, //转生
        Login = 2, //登陆
        Money = 3, //消费
    }

    /// <summary>
    /// 基金购买类型
    /// </summary>
    public enum EFundBuy
    {
        Have = 1, //已购
        Can = 2, //可购
        Limit = 3, //不可购买
    }

    /// <summary>
    /// 基金奖励领取类型
    /// </summary>
    public enum EFundAward
    {
        Have = 1, //已领
        Can = 2, //可领
        Limit = 3, //不可领取
    }

    /// <summary>
    /// 基金状态
    /// </summary>
    public enum EFundState
    {
        Old = 0, //部分领取完
        Now = 1, //正在领取
        End = 2, //全部领取完
    }

    /// <summary>
    /// 操作状态
    /// </summary>
    public enum EFundError
    {
        ENoBuy = -8,//未购买
        EAward = -7,//已经领取
        EAwardLimit = -6,//未达到领奖条件
        EVipLimit = -5,//vip限制购买
        EIsBuy = -4,//已购买
        ENoMoney = -3,//购买金额不足
        ENoOpen = -2,//未开放
        Error = -1,//操作失败
        Default = 0,//默认
        Succ = 1,//成功
    }
}
