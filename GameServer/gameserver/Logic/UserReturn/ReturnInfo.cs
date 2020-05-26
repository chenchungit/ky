using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.UserReturn
{
    //回归活动基本信息
    public class ReturnActivityInfo
    {
        public int ID = 0;
        public int ActivityID = 0;
        public DateTime TimeBegin = DateTime.MinValue;
        public DateTime TimeEnd = DateTime.MinValue;
        public DateTime TimeAward = DateTime.MinValue;
        public DateTime TimeBeginNoLogin = DateTime.MinValue;
        public DateTime TimeEndNoLogin = DateTime.MinValue;
        public int Level = 0;
        public int Vip = 4;

        //----------------------------------------------
        public bool IsOpen = false;
        public DateTime TimeSet = DateTime.MinValue;
        public string ActivityDay = "";
    }

    //回归奖励
    public class ReturnAwardInfo
    {
        public int ID = 0;
        public int Vip = 0;
        public List<GoodsData> DefaultGoodsList = null;
        public List<GoodsData> ProGoodsList = null;
    }

    //签到奖励
    public class ReturnCheckAwardInfo
    {
        public int ID = 0;
        public int LevelMin = 0;
        public int LevelMax = 0;
        public int Day = 0;
        public List<GoodsData> DefaultGoodsList = null;
        public List<GoodsData> ProGoodsList = null;
    }

    //商店
    public class ReturnShopAwardInfo
    {
        public int ID = 0;
        public GoodsData Goods = null;
        public int OldPrice = 0;
        public int NewPrice = 0;
        public int LimitCount = 0;
    }
}
