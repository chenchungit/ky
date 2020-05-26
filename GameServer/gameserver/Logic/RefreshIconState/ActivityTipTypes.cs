	/// summary
	/// 活动提示类型(节点)
	/// summary
	public enum ActivityTipTypes
	{
        Root,                           // 根节点

        MainHuoDongIcon = 1000,         // 主活动图标 (客户端判断)
        RiChangHuoDong,                 // 日常活动 (客户端判断)
		ShiJieBoss,	                    // 世界Boss 
        VIPHuoDong,	                    // 付费Boss (客户端判断)
        ShouFeiBoss,                    // 付费Boss (客户端判断)
        HuangJinBoss,                   // 黄金部队
        RiChangHuoDongOther,            // 其他日常活动(除黄金Boss) (客户端判断)
        AngelTemple,                    // 天使神殿
        TianTiMonthRankAwards = 1008,   // 天梯上月段位排行奖励

        MainFuBenIcon = 2000,           // 主副本图标 (客户端判断)
        JuQingFuBen,                    // 剧情副本 (客户端判断)
        ZuDuiFuBen,                     // 组队副本 (客户端判断)
        RiChangFuBen,                   // 日常副本 (客户端判断)

        MainFuLiIcon = 3000,            // 主福利图标
        FuLiChongZhiHuiKui,             // 充值回馈
        ShouCiChongZhi,                 // 充值回馈-首次充值 (OK)
        MeiRiChongZhi,                  // 充值回馈-每日充值 (OK)
        LeiJiChongZhi,                  // 充值回馈-累积充值
        LeiJiXiaoFei,                   // 充值回馈-累计消费
        FuLiMeiRiHuoYue,                // 每日活跃 (OK)
        FuLiLianXuDengLu,               // 连续登录 (OK)
        FuLiLeiJiDengLu,                // 累计登陆 (OK)
        FuLiMeiRiZaiXian,               // 每日在线 (OK)
        FuLiUpLevelGift,                // 等级奖励 (OK)
        ShouCiChongZhi_YiLingQu,        // 首次充值-已领取
        MeiRiChongZhi_YiLingQu,         // 每日充值-已领取
        FuLiYueKaFanLi,                 // 月卡返利

        MainJingJiChangIcon = 4000,     // 主竞技场图标
        JingJiChangJiangLi,             // 奖励预览
        JingJiChangJunXian,             // 军衔提升
        JingJiChangLeftTimes,           // 剩余挑战次数

        MainGongNeng = 5000,            // 主功能图标 (客户端判断)
        MainMingXiangIcon = 5001,       // 功能里的冥想图标 (客户端判断)
        MainEmailIcon = 5002,           // 功能里的邮件图标

        MainXinFuIcon = 6000,           // 主新服图标
        XinFuLevel = 6001,              // 练级狂人 (客户端判断)
        XinFuKillBoss = 6002,           // 屠魔勇士
        XinFuChongZhiMoney = 6003,      // 充值达人
        XinFuUseMoney = 6004,           // 消费达人
        XinFuFreeGetMoney = 6005,       // 劲爆返利

        MainMeiRiBiZuoIcon = 7000,      // 每日必做图标
        ZiYuanZhaoHui = 7001,           // 资源找回

        QiFuIcon = 8000,                //祈福功能

        MainChengJiuIcon = 9000,        // 主成就图标

        VIPGongNeng = 10000,            //vip功能
        VIPGifts = 10001,               //vip礼包

        BuChangIcon = 11000,            //补偿

        HeFuActivity    = 12000,        // 合服活动总叹号
        HeFuLogin       = 12001,        // 合服登陆
        HeFuTotalLogin  = 12002,        // 合服累计登陆
        HeFuRecharge    = 12003,        // 合服充值返利 
        HeFuPKKing      = 12004,        // 合服战场之神
        HeFuLuoLan      = 12005,        // 合服罗兰城主

        ShuiJingHuangJin = 13000,       //水晶幻境

        JieRiActivity   = 14000,        // 节日活动总叹号
        JieRiLogin      = 14001,        // 节日登陆
        JieRiTotalLogin = 14002,        // 节日累计登陆
        JieRiDayCZ      = 14003,        // 节日每日充值 
        JieRiLeiJiXF    = 14004,        // 节日累计消费 
        JieRiLeiJiCZ    = 14005,        // 节日累计充值 
        JieRiCZKING     = 14006,        // 节日充值王 
        JieRiXFKING     = 14007,        // 节日消费王 

        JieRiGive = 14008,          //节日赠送
        JieRiGiveKing = 14009,   //节日赠送王
        JieRiRecvKing = 14010,    // 节日收取王
        JieriWing       = 14011,             // 节日翅膀返利
        JieriAddon      = 14012,            // 节日追加返利
        JieriStrengthen = 14013,       // 节日强化返利
        JieriAchievement = 14014,      // 节日成就返利
        JieriMilitaryRank = 14015,     // 节日军衔返利
        JieriVIPFanli   = 14016,         // 节日VIP返利
        JieriAmulet     = 14017,           // 节日护身符返利
        JieriArchangel  = 14018,        // 节日大天使返利
        JieriMarriage   = 14019,        // 节日婚姻返利
        JieRiLianXuCharge = 14020,      // 节日连续充值
        JieRiRecv = 14021,              // 节日收礼
        JieRiPlatChargeKing = 14022,      // 节日平台充值王(这个应该不需要使用)

        JieRiIPointsExchg   = 14023,    // 节日积分兑换

        UserReturnAll       = 14100,    //角色召回
        UserReturnRecall    = 14101,    //召回奖励
        UserReturnAward     = 14102,    //回归奖励
        UserReturnCheck     = 14103,    //回归签到
        UserReturnResult     = 14104,   //召回结果

        TipSpread = 14105,    //推广

        
        FundChangeLife  = 14106,    //基金——转生
        FundLogin       = 14107,    //基金——登陆
        FundMoney       = 14108,    //基金——豪气
        Fund            = 14109,    //基金
        ZhuanXiang      = 14110,    //专享活动

        Ally = 14111,
        AllyAccept = 14112,
        AllyMsg = 14113,

        MerlinSecretAttr    = 14201,       // 梅林秘语属性   
        GuildIcon           = 15000,       // 战盟界面
        GuildCopyMap = 15001,       // 有没领取的战盟副本的奖励
        LangHunLingYuIcon = 15002,       // 有没领取的圣域争霸奖励
        LangHunLingYuFightIcon = 15003,       // 有没圣域争霸城池参战资格

        ZhengBaCanJoinIcon = 15010, //获得参赛资格的角色，跨服活动标签按钮有!提示（点击进入众神页签后 消失并记录，本月不再提示）
        CoupleArenaCanAward = 15011, // 夫妻竞技可领奖提示
        CoupleWishCanAward = 15012,     // 情侣排行榜可领奖提示

        BuildingIcon        = 15050,       // 领地系统icon

        PetBagIcon          = 16000,       // 主icon 当精灵仓库内有剩余精灵时，猎取界面会有“红数字”显示 精灵界面会有感叹号显示
        CallPetIcon         = 16001,       // 精灵猎取有免费次数时 猎取界面会有感叹号显示 精灵界面会有感叹号显示

        SevenDayActivity = 17000, // 七日活动总图标
        SevenDayLogin = 17001, // 七日登录
        SevenDayCharge = 17002, // 七日充值
        SevenDayGoal = 17003,   // 七日目标
        SevenDayBuy = 17004,    // 七日抢购
        SevenDayGoal_1 = 17005, // 七日目标第1天
        SevenDayGoal_2 = 17006, // 七日目标第2天
        SevenDayGoal_3 = 17007, // 七日目标第3天
        SevenDayGoal_4 = 17008, // 七日目标第3天
        SevenDayGoal_5 = 17009, // 七日目标第5天
        SevenDayGoal_6 = 17010, // 七日目标第6天
        SevenDayGoal_7 = 17011, // 七日目标第7天
	}
