using System.Collections.Generic;
using ProtoBuf;
using System;

namespace Server.Data
{
    /// <summary>
    /// 消息错误码
    /// </summary>
    public enum KingOfBattleErrorCode
    {
        KingOfBattle_Success = 0,          		// 成功
        KingOfBattle_ErrorJiFenNotEnough,       // 王者积分不够
        KingOfBattle_ErrorBagNotEnough,         // 背包空间不够
        KingOfBattle_ErrorNotSaleGoods,         // 非售卖商品
        KingOfBattle_ErrorParams,        		// 传来的参数错误
        KingOfBattle_DBFailed,                  // 数据库出错
        KingOfBattle_ErrorPurchaseLimit,        // 限购数量达到上限
        KingOfBattle_ErrorZuanShiNotEnough,     // 钻石不够
    }

    public enum KingOfBattleGameStates
    {
        None, //无
        SignUp, //报名时间
        Wait, //等待开始
        Start, //开始
        Awards, //有未领取奖励
        NotJoin, // 未参加本次活动
    }

    [ProtoContract]
    public class KingOfBattleStoreSaleData
    {
        /// <summary>
        /// ID
        /// </summary>
        [ProtoMember(1)]
        public int ID;

        /// <summary>
        /// SinglePurchase
        /// </summary>
        [ProtoMember(2)]
        public int Purchase;
    }

    [ProtoContract]
    public class KingOfBattleStoreData
    {
        /// <summary>
        /// 上次刷新时间
        /// </summary>
        [ProtoMember(1)]
        public DateTime LastRefTime;

        /// <summary>
        /// 商品数据
        /// </summary>
        [ProtoMember(2)]
        public List<KingOfBattleStoreSaleData> SaleList;
    }

    /// <summary>
    /// 王者战场积分数据
    /// </summary>
    [ProtoContract]
    public class KingOfBattleScoreData
    {
        /// <summary>
        /// 阵营1得分
        /// </summary>
        [ProtoMember(1)]
        public int Score1;

        /// <summary>
        /// 阵营2得分
        /// </summary>
        [ProtoMember(2)]
        public int Score2;
    }

    /// <summary>
    /// 战斗结束的结果和奖励
    /// </summary>
    [ProtoContract]
    public class KingOfBattleAwardsData
    {
        /// <summary>
        /// 战斗结果(0失败,1胜利)
        /// </summary>
        [ProtoMember(1)]
        public int Success;

        /// <summary>
        /// 奖励绑定金币
        /// </summary>
        [ProtoMember(2)]
        public int BindJinBi;

        /// <summary>
        /// 奖励经验值
        /// </summary>
        [ProtoMember(3)]
        public long Exp;

        /// <summary>
        /// 奖励物品列表
        /// </summary>
        [ProtoMember(4)]
        public List<AwardsItemData> AwardsItemDataList;

        /// <summary>
        /// 阵营1得分
        /// </summary>
        [ProtoMember(5)]
        public int SideScore1;

        /// <summary>
        /// 阵营2得分
        /// </summary>
        [ProtoMember(6)]
        public int SideScore2;

        /// <summary>
        /// 自己得分
        /// </summary>
        [ProtoMember(7)]
        public int SelfScore;
    }

}