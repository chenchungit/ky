using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.FluorescentGem
{
    /// <summary>
    /// 魂石常量定义
    /// </summary>
    public class SoulStoneConsts
    {
        /// <summary>
        /// 魂石升级和经验配置
        /// </summary>
        public const string ExpCfgFile = "Config/Gem/HunShiExp.xml";

        /// <summary>
        /// 魂石随机组配置
        /// </summary>
        public const string RandTypeCfgFile = "Config/Gem/HunShiType.xml";

        /// <summary>
        /// 魂石随机信息
        /// </summary>
        public const string RandInfoCfgFile = "Config/Gem/HunShi.xml";

        /// <summary>
        /// 魂石额外加成组
        /// </summary>
        public const string GroupCfgFile = "Config/Gem/HunShiGroup.xml";

        /// <summary>
        /// 魂石物品所属组
        /// </summary>
        public const string GoodsTypeCfgFile = "Config/Gem/HunShiGoodsType.xml";

        /// <summary>
        /// 背包最大容量
        /// </summary>
        public const int MaxBagNum = 100;

        /// <summary>
        /// 魂石默认等级
        /// </summary>
        public const int DefaultLevel = 1;

        /// <summary>
        /// 魂石装备环的个数，每个环的索引[1-EquipCycleNum]
        /// </summary>
        public const int EquipCycleNum = 3;

        /// <summary>
        /// 每个魂石装备环内的格子数，每个格子的索引[1-EquipCycleGridNum]
        /// </summary>
        public const int EquipCycleGridNum = 6;
    }

    /// <summary>
    /// 魂石错误码
    /// </summary>
    public enum ESoulStoneErrorCode
    {
        Success = 0, //成功，其余全失败
        UnknownFailed, // 未知错误
        VisitParamsError, // 客户端访问参数错误
        SelectExtFuncNotOpen, // 选择的额外功能未开启
        ConfigError, // 配置文件错误
        LangHunFenMoNotEnough, // 狼魂粉末不足
        ExtCostNotEnough, // 额外消耗不足
        BagNoSpace, // 背包不足
        LevelIsFull, // 等级已满
        CanNotEquip, // 不可装备
        DbFailed, // 数据库错误
        NotOpen, // 功能未开启
    }

    /// <summary>
    /// 魂石获取类型
    /// </summary>
    public enum ESoulStoneGetTimes
    {
        One = 1, // 一次
        Ten = 10, // 10次
    }

    /// <summary>
    /// 聚魂额外功能类型
    /// </summary
    public enum ESoulStoneExtFuncType
    {
        AddedGoods = 1, // 获得额外道具
        ReduceLangHunFenMo = 2, //减少狼魂粉末消耗
        UpSuccessRate = 3, //提高成功几率
        HoldTypeIfFail = 4, //聚魂失败锁定跳转
    }

    /// <summary>
    /// 聚魂额外消耗类型
    /// </summary>
    public enum ESoulStoneExtCostType
    {
        MoJing = 1,
        XingHun = 2,
        ChengJiu = 3,
        ShengWang = 4,
        ZuanShi = 5,
    }

    /// <summary>
    /// 魂石随机组随机信息
    /// </summary>
    class SoulStoneRandInfo
    {
        public int Id;
        public GoodsData Goods;
        public int RandBegin;
        public int RandEnd;
    }

    /// <summary>
    /// 魂石随机组配置文件
    /// </summary>
    class SoulStoneRandConfig
    {
        // 随机组ID
        public int RandId;
        // 基础消耗狼魂粉末
        public int NeedLangHunFenMo;
        // 成功几率
        public double SuccessRate;
        // 几率判断成功后，随机到新的随机组
        public List<int> SuccessTo = new List<int>();
        // 几率判断失败后，随机到新的随机组
        public List<int> FailTo = new List<int>();

        // 本随机组的随机物品列表
        public List<SoulStoneRandInfo> RandStoneList = new List<SoulStoneRandInfo>();
        public int RandMinNumber;
        public int RandMaxNumber;

        // 额外功能1 消耗消耗SoulStoneExtCostType额外获得物品
        public Dictionary<ESoulStoneExtCostType, int> AddedNeedDict = new Dictionary<ESoulStoneExtCostType, int>();
        public double AddedRate;
        public GoodsData AddedGoods;

        //额外功能2 消耗消耗SoulStoneExtCostType减少狼魂粉末消耗
        public Dictionary<ESoulStoneExtCostType, int> ReduceNeedDict = new Dictionary<ESoulStoneExtCostType, int>();
        public double ReduceRate;
        public int ReduceValue;

        // 额外功能3 消耗SoulStoneExtCostType提高成功几率
        public Dictionary<ESoulStoneExtCostType, int> UpSucRateNeedDict = new Dictionary<ESoulStoneExtCostType, int>();
        public double UpSucRateTo;

        // 额外功能4 消耗SoulStoneExtCostType失败跳转至
        public Dictionary<ESoulStoneExtCostType, int> FailHoldNeedDict = new Dictionary<ESoulStoneExtCostType, int>();
        public List<int> FailToIfHold = new List<int>();
    }

    /// <summary>
    /// 魂石经验配置
    /// </summary>
    class SoulStoneExpConfig
    {
        public int Suit;
        public int MinLevel;
        public int MaxLevel;
        //每一级的总经验
        public Dictionary<int, int> Lvl2Exp = new Dictionary<int, int>();
    }

    /// <summary>
    /// 魂石组加成配置
    /// </summary>
    class SoulStoneGroupConfig
    {
        public int Group;
        public List<int> NeedTypeList = new List<int>();
        public Dictionary<int, double> AttrValue = new Dictionary<int, double>();
    }

    /// <summary>
    /// 魂石类型配置文件
    /// </summary>
    class SoulStoneTypeConfig
    {
        public int Type;
        public List<int> GoodsIdList = new List<int>();
    }
}
