using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Server.Data;

namespace Server.Data
{
    /// <summary>
    /// 藏宝秘境数据 CMD_SPR_ONEPIECE_GET_INFO
    /// </summary>
    [ProtoContract]
    public class OnePieceTreasureData
    {
        /// <summary>
        /// 位置信息，流水号为层数+3位格子序号
        /// </summary>
        [ProtoMember(1)] 
        public int PosID = 0;

        /// <summary>
        /// 缓存事件ID
        /// </summary>
        [ProtoMember(2)]
        public int EventID = 0;

        /// <summary>
        /// 普通骰子个数
        /// </summary>
        [ProtoMember(3)]
        public int RollNumNormal = 0;

        /// <summary>
        /// 奇迹骰子个数
        /// </summary>
        [ProtoMember(4)]
        public int RollNumMiracle = 0;

        /// <summary>
        /// 距离重置的时间
        /// </summary>
        [ProtoMember(5)]
        public long ResetPosTicks = 0;
    }

    /// <summary>
    /// 藏宝秘境同步事件信息 CMD_SPR_ONEPIECE_SYNC_EVENT
    /// </summary>
    [ProtoContract]
    public class OnePieceTreasureEvent
    {
        /// <summary>
        /// 事件ID
        /// </summary>
        [ProtoMember(1)]
        public int EventID = 0;

        /// <summary>
        /// 事件Value 对于ETET_Move 代表随机步数
        /// </summary>
        [ProtoMember(2)]
        public int EventValue = 0;

        /// <summary>
        /// 宝箱奖励
        /// </summary>
        [ProtoMember(3)]
        public List<int> BoxIDList = null;

        /// <summary>
        /// 错误码
        /// </summary>
        [ProtoMember(4)]
        public int ErrCode = 0;
    }
}

namespace GameServer.Logic.OnePiece
{
    /// <summary>
    /// 消息错误码
    /// </summary>
    public enum OnePieceTreasureErrorCode
    {
        OnePiece_Success = 0,          		// 成功
        OnePiece_ErrorZuanShiNotEnough,     // 钻
        OnePiece_ErrorBagNotEnough,         // 背包空间不够
        OnePiece_ErrorParams,        		// 传来的参数错误
        OnePiece_DBFailed,                  // 数据库出错
        OnePiece_ErrorMoving,               // 正在移动中，请稍后
        OnePiece_ErrorNotHaveEvent,         // 没有待执行的事件
        OnePiece_ErrorNeedGoodsID,          // 兑换物品配置错误
        OnePiece_ErrorNeedGoodsCount,       // 兑换物品配置错误
        OnePiece_ErrorGoodsNotEnough,       // 兑换背包物品不足
        OnePiece_ErrorNeedMoneyNotEnough,   // 兑换所需货币不足
        OnePiece_ErrorMoveRange,            // 随机移动事件配置错误
        OnePiece_ErrorMoveNumNotEnough,     // 移动步数不足
        OnePiece_ResetPos,                  // 重置位置
        OnePiece_ErrorRollNumMax,           // 骰子数达到上限
        OnePiece_ErrorRollNumNotEnough,     // 骰子数不足
        OnePiece_ErrorCheckMail,            // 请检查邮件
    }

    /// <summary>
    /// 藏宝秘境日志数据更新枚举
    /// </summary>
    public enum OnePieceTreasureLogType
    {
        TreasureLog_Role = 0,       // 每日参与人次，单人多次计1次
        TreasureLog_BuyDice,        // 每日购买普通骰子次数
        TreasureLog_BuySuperDice,   // 每日购买奇迹骰子次数
        TreasureLog_MoveNum,        // 每日总移动格数
    }

    /// <summary>
    /// 骰子类型
    /// </summary>
    public enum DiceType
    {
        EDT_Null = -1,

        EDT_Normal = 0, // 普通骰子

        EDT_Miracle, // 奇迹骰子
    }

    /// <summary>
    /// 触发方式 0为停留触发，1为经过触发
    /// </summary>
    public enum TriggerType
    {
        ETT_Null = -1,

        ETT_Stay = 0, // 0为停留触发

        ETT_Pass, // 1为经过触发
    }

    /// <summary>
    /// 类型：1奖励、2兑换、3移动、4战斗、5宝箱
    /// </summary>
    public enum TreasureEventType
    {
        ETET_Null = 0,

        ETET_Award = 1, // 奖励(停留)(自动)

        ETET_Excharge, // 兑换(停留)

        ETET_Move, // 移动(停留)(自动)

        ETET_Combat, // 战斗(停留)

        ETET_TreasureBox, // 宝箱(经过)(自动)
    }

    /// <summary>
    /// 类型：1~道具；2~秘宝点数；3~血钻
    /// </summary>
    public enum TeasureBoxType
    {
        ETBT_Null = 0,

        ETBT_Goods = 1, // 道具

        ETBT_BaoZangJiFen, // 秘宝点数

        ETBT_BaoZangXueZuan, // 宝藏血钻
    }

    /// <summary>
    /// 货币Key & Value
    /// </summary>
    public class OnePieceMoneyPair
    {
        /// <summary>
        /// 类型
        /// </summary>
        public MoneyTypes Type = MoneyTypes.None;

        /// <summary>
        /// 数量
        /// </summary>
        public int Num = 0;
    }

    /// <summary>
    /// 物品Key & Value
    /// </summary>
    public class OnePieceGoodsPair
    {
        /// <summary>
        /// ID
        /// </summary>
        public int _NeedGoodsID;

        /// <summary>
        /// 数量
        /// </summary>
        public int _NeedGoodsCount;
    }

    /// <summary>
    /// 宝箱Key & Value
    /// </summary>
    public class OnePieceTreasureBoxPair
    {
        /// <summary>
        /// ID
        /// </summary>
        public int BoxID = 0;

        /// <summary>
        /// 打开次数
        /// </summary>
        public int OpenNum = 0;
    }

    /// <summary>
    /// 藏宝地图事件随机
    /// </summary>
    public class OnePieceRandomEvent
    {
        /// <summary>
        /// 事件ID
        /// </summary>
        public int EventID = 0;

        /// <summary>
        /// 概率
        /// </summary>
        public double Rate = 0.0d;
    }

    /// <summary>
    /// 藏宝地图 TreasureMap.xml
    /// </summary>
    public class OnePieceTreasureMapConfig
    {
        /// <summary>
        /// 流水号，流水号为层数+3位格子序号
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// 格子序号顺序排列，即0后为1依次类推
        /// </summary>
        public int Num = 0;

        /// <summary>
        /// 层数
        /// </summary>
        public int Floor = 0;

        /// <summary>
        /// 0为停留触发，1为经过触发
        /// </summary>
        public TriggerType Trigger = TriggerType.ETT_Null;

        /// <summary>
        /// 停留在该格子获得秘宝点数
        /// </summary>
        public int Score = 0;

        /// <summary>
        /// 填写多个事件，格式：事件ID,概率|事件ID,概率注：事件总概率和为1
        /// </summary>
        public List<OnePieceRandomEvent> LisRandomEvent = new List<OnePieceRandomEvent>();
    }

    /// <summary>
    /// 藏宝事件 TreasureEvent.xml
    /// </summary>
    public class OnePieceTreasureEventConfig
    {
        /// <summary>
        /// 流水号
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// 类型：1奖励、2兑换、3移动、4战斗、5宝箱
        /// </summary>
        public TreasureEventType Type = TreasureEventType.ETET_Null;

        /// <summary>
        /// 奖励物品列表
        /// </summary>
        public AwardsItemList GoodsList = new AwardsItemList();

        /// <summary>
        /// 奖励物品列表
        /// </summary>
        public OnePieceMoneyPair NewValue = new OnePieceMoneyPair();

        /// <summary>
        /// 普通骰子奖励
        /// </summary>
        public int NewDiec = 0;

        /// <summary>
        /// 奇迹骰子奖励
        /// </summary>
        public int NewSuperDiec = 0;

        /// <summary>
        /// 消耗道具
        /// </summary>
        public List<OnePieceGoodsPair> NeedGoods = new List<OnePieceGoodsPair>();

        /// <summary>
        /// 消耗货币
        /// </summary>
        public OnePieceMoneyPair NeedValue = new OnePieceMoneyPair();

        /// <summary>
        /// 移动范围
        /// </summary>
        public List<int> MoveRange = new List<int>();

        /// <summary>
        /// 副本ID
        /// </summary>
        public int FuBenID = 0;

        /// <summary>
        /// 宝箱
        /// </summary>
        public List<OnePieceTreasureBoxPair> BoxList = new List<OnePieceTreasureBoxPair>();
    }

    /// <summary>
    /// 宝箱 TreasureBox.xml
    /// </summary>
    public class OnePieceTreasureBoxConfig
    {
        /// <summary>
        /// 流水号
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// 1~道具；2~秘宝点数；3~血钻
        /// </summary>
        public TeasureBoxType Type = TeasureBoxType.ETBT_Null;

        /// <summary>
        /// 1~道具7位数组；2,3~数值
        /// </summary>
        public AwardsItemList Goods = new AwardsItemList();

        /// <summary>
        /// 2,3~数值
        /// </summary>
        public int Num = 0;

        /// <summary>
        /// 随机数 开始
        /// </summary>
        public int BeginNum = 0;

        /// <summary>
        /// 随机数 结束
        /// </summary>
         public int EndNum = 0;
    }

}