using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProtoBuf;

namespace GameServer.Logic.ActivityNew.SevenDay
{
    /// <summary>
    /// 七日活动常量定义
    /// </summary>
    public static class SevenDayConsts
    {
        // 七日活动天数
        public const int DayCount = 7;
        // 七日登录配置
        public const string LoginConfig = "Config/SevenDay/SevenDayLogin.xml";
        // 七日目标配置
        public const string GoalConfig = "Config/SevenDay/SevenDayGoal.xml";
        // 七日抢购配置
        public const string BuyConfig = "Config/SevenDay/SevenDayQiangGou.xml";
        // 七日充值配置
        public const string ChargeConfig = "Config/SevenDay/SevenDayChongZhi.xml";


        public const int HadGetAward = 1;
        public const int NotGetAward = 0;

        public const int HadLoginFlag = 1;

        public const int HadFinishFlag = 1;
        public const int NotFinishFlag = 0;
    }

    /// <summary>
    /// 七日活动 类型
    /// </summary>
    public enum ESevenDayActType
    {
        Login = 1,
        Charge = 2,
        Goal = 3,
        Buy = 4,
    }

    public enum ESevenDayActErrorCode
    {
        Success = 0,  // 成功，其余全为失败
        NotInActivityTime = 1, // 不在活动时间
        ServerConfigError = 2, // 服务器配置出错
        NoBagSpace = 3, // 背包不足
        DBFailed = 4, // 数据库异常
        NotReachCondition = 5, // 不满足领奖条件
        VisitParamsWrong = 6, // 客户端访问参数错误
        ZuanShiNotEnough = 7, // 钻石不足
        NoEnoughGoodsCanBuy = 8, // 可抢购数量不足
    }

    /// <summary>
    /// 七日目标 功能类型
    /// </summary>
    public enum ESevenDayGoalFuncType
    {
        Unknown = 0,
        RoleLevelUp = 1, // 等级达到[0]转[1]级
        SkillLevelUp = 2, // 将[0]个技能等级升至[1]级
        MoJingCntInBag = 3, //背包内魔晶达到[0] 曾经最大
        RecoverMoJing = 4,  //通过回收获得魔晶[0] 累加
        ExchangeJinHuaJingShiByMoJing = 5, // 使用魔晶兑换进化晶石[1]个   累加
        JoinJingJiChangTimes = 6, // 参与竞技场[0]场   累加
        WinJingJiChangTimes = 7, // 在竞技场中获胜[0]场    累加
        JingJiChangRank = 8, // 在竞技场中达到[0]名  曾经最大
        PeiDaiBlueUp = 9, // 佩戴[0]蓝色或更高品质的装备   实时
        PeiDaiPurpleUp = 10, // 佩戴[0]紫色或更高品质的装备   实时
        // 11 没有
        RecoverEquipBlueUp = 12, //回收[0]件蓝色或更高品质装备
        MallInSaleCount = 13, // 在交易所中上架[0]件装备    当前 上架数量
        GetEquipCountByQiFu = 14, // 通过祈福获得[0]件装备 累加
        PickUpEquipCount = 15, //拾取[0]件装备 累加
        EquipChuanChengTimes = 16, // 进行[0]次装备传承
        EnterFuBenTimes = 17, //进入xxx副本yyy次   累加
        KillMonsterInMap = 18, // 在xxx沙漠杀死yyy只怪 累加
        JoinActivityTimes = 19, // 参与xx广场yy次 累加
        HeChengTimes = 20, // 合成某物品xxx个
        UseGoodsCount = 21, // 使用xxxx果实yyy个   累加
        JinBiZhuanHuanTimes = 22, // 进行金币转换xxx次 累加
        BangZuanZhuanHuanTimes = 23, // 绑钻转换 xxx次  累加
        ZuanShiZhuanHuanTimes = 24, // 钻石转换 xxx次 累加
        ExchangeJinHuaJingShiByQiFuScore = 25, // 使用祈福积分兑换进化晶石道具[1]次 累加
        CombatChange = 26, //战斗力达到[0]  实时
        PeiDaiForgeEquip = 27, // 佩戴xxx个强化+yyy的装备 实时
        ForgeEquipLevel = 28, // 强化等级最高的装备达到+xxx 历史曾经
        ForgeEquipTimes = 29, // 进行xxx次强化  累加  成功失败都算
        CompleteChengJiu = 30, // 完成xxx成就 实时
        ChengJiuLevel = 31, // 成就护体达到xxx级  实时
        JunXianLevel = 32, // 军衔护体达到xxx级 实时
        PeiDaiAppendEquip = 33, // 佩戴xxx个追yyy装备 实时
        AppendEquipLevel = 34, // 最大追加等级达到XXX  曾经
        AppendEquipTimes = 35, // 进行[0]次追加 曾经  成功失败都算
        ActiveXingZuo = 36, // 激活xxx星图yyy颗星
        GetSpriteCountBuleUp = 37, // 入库XXX个蓝色或更高品质精灵 实时
        GetSpriteCountPurpleUp = 38, // 入库XXX个紫色或更高品质精灵 实时
        WingLevel = 39, // 翅膀等级达到xxx阶yyy星
        WingSuitStarTimes = 40, // 进行xxx次翅膀升星或升阶 累加 升星和升阶都算，成功失败都算
        CompleteTuJian = 41, // 完成xxx图鉴
        PeiDaiSuitEquipCount = 42, // 佩戴xxx个yyy阶装备 实时
        PeiDaiSuitEquipLevel = 43, // 佩戴装备最高达到xxx阶  实时
        EquipSuitUpTimes = 44, // 进行xxx次装备进阶  累加
    }
}
