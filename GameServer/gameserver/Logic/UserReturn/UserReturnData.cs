using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace GameServer.Logic.UserReturn
{
    //召回数据
    [ProtoContract]
    public class UserReturnData
    {
        [ProtoMember(1)]
        public bool ActivityIsOpen = false;

        [ProtoMember(2)]
        public int ActivityID = 0;//活动id

        [ProtoMember(3)]
        public string ActivityDay = "";//设置的活动开始日期

        [ProtoMember(4)]
        public DateTime TimeBegin = DateTime.MinValue;//活动开始时间

        [ProtoMember(5)]
        public DateTime TimeEnd = DateTime.MinValue;//活动结束时间

        [ProtoMember(6)]
        public DateTime TimeAward = DateTime.MinValue;//领奖截止时间

        [ProtoMember(7)]
        public string RecallCode = "0";//召回码（默认为0）

        [ProtoMember(8)]
        public int RecallZoneID = 0;//召回人zoneID

        [ProtoMember(9)]
        public int RecallRoleID = 0;//召回人roleID

        [ProtoMember(10)]
        public int Level = 0;//召回时等级

        [ProtoMember(11)]
        public int Vip = 0;//召回时vip

        [ProtoMember(12)]
        public DateTime TimeReturn = DateTime.MinValue;

        [ProtoMember(13)]
        public int ReturnState = 0;//召回状态

        [ProtoMember(14)]
        public Dictionary<int, int[]> AwardDic = new Dictionary<int, int[]>();//奖励列表（奖励类型，奖励数据）

        [ProtoMember(15)]
        public DateTime TimeWait = DateTime.MinValue;

        [ProtoMember(16)]
        public int ZhuanSheng = 0;

        //等级
        [ProtoMember(17)]
        public int DengJi = 0;

        [ProtoMember(18)]
        public string MyCode = "";
    }

    //奖励类型
    public enum EReturnAwardType
    {
        Recall = 1, //召回奖励（id1，状态，id2，状态，id3.......【全部id】）
        Return = 2, //回归奖励——vip （id【已经领取，只记录一个】）
        Check = 3,  //回归签到——等级（id【已经领取，只记录一个】）
        Shop = 4,   //商店购买（id1，数量，id2，数量......【已经购买id】）
    }

    //领奖操作状态
    public enum EReturnAwardOperateState
    {
        Old = -1,       //已经领取
        CanNot = 0,     //不能领取
        Can = 1,        //可以领取
    }

    //召回状态
    public enum EReturnState
    {
        EDouble = -100,      //数据重复
        EFailShow = -99,     //已经显示错误提示

        ShowReturn = -52,    //显示已经召回
        ShowNoCheck = -51,   //显示未通过——资格
        ShowNoSign = -50,    //显示未通过——code

        EShopMax = -14,     //超过购买上限
        ELevel = -13,       //等级不足
        EVip = -12,         //vip等级不够
        EPlatform = -11,    //平台不同
        ENoOpen = -10,      //活动未开启
        ENoRecall = -9,     //召回人不存在
        EIsSelf = -8,       //推荐人不能是自己
        ENoReturn = -7,     //不符合召回条件
        EWait = -6,         //验证中
        EIsReturn = -5,     //已经被召回

        ETimeOut = -4,      //超时
        EFail = -3,         //默认失败

        ESign = -2,         //校验失败——召回code
        ECheck = -1,        //校验失败——资格
        Default = 0,        //默认
        WaitCheck = 1,      //等待验证——资格
        Check = 2,          //验证成功——资格
        WaitSign = 3,       //等待验证——召回code
        CheckAndSign = 4,   //验证成功——资格，召回code  
    }

    //领奖状态
    public enum EReturnAwardState
    {
        EShopMax = -9,      //超过购买上限
        EVip = -8,          //vip等级不足
        ENoOpen = -7,       //活动未开启
        ENoMoney = -6,      //钻石不足
        ENoBag = -5,        //背包已满
        EIsHave = -4,       //已经领取
        ENoRecall = -3,     //不是召回人
        ENoReturn = -2,     //不是回归角色
        EFail = -1,         //默认失败
        Default = 0,        //默认
        Succ = 1,           //成功
    }
}
