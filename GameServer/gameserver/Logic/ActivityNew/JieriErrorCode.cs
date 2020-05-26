using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.ActivityNew
{
    public enum JieriGiveErrorCode
    {
        Success,                            // 成功
        ActivityNotOpen,              // 活动未开启
        NotAwardTime,                 // 不是领奖时间
        GoodsIDError,                   // 物品id错误
        GoodsNotEnough,            // 物品不足
        ReceiverNotExist,              // 接收者不存在
        ReceiverCannotSelf,          // 接收者不能是自己
        DBFailed,                           // 数据库服务器出错
        ConfigError,                       // 服务器配置错误
        NoBagSpace,                     // 背包不足
        NotMeetAwardCond,         // 不满足领奖条件(已领奖或未达成)
    }
}