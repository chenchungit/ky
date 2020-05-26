using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Today
{
    public enum ETodayType
    {
        Defalut = 0,
        Exp = 1,
        Gold = 2,
        KaLiMa = 3,
        EM = 4,
        Lo = 5,
        Tao = 6,
    }


   public enum ETodayState
   {
       NotOpen = -11,       //功能未开启
       TaoCancel = -8,      //讨伐任务放弃失败
       EFubenConfig = -7,   //副本配置错误
       IsFuben = -6,        //已经全部领取
       IsAllAward = -5,     //已经全部领取
       IsAward = -4,        //已经领取
       NoType = -3,         //没有操作类型
       NoBag = -2,          //背包已满
       Fail = -1,           //失败
       Default = 0,         //默认
       Success = 1,         //成功
   }

}
