using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 荧光宝石预定义属性、常量等 [XSea 2015/8/6]
    /// </summary>
    public class FluorescentGemDefine
    {
        public const int MAX_FLUORESCENT_GEM_BAG_COUNT = 100; // 荧光宝石背包格子数上限

        public const string LEVEL_TYPE_PATH = "Config/Gem/GemDigType.xml"; // 矿坑类型表
        public const string DIG_PATH = "Config/Gem/GemDig.xml"; // 荧光宝石挖掘表
        public const string UP_PATH = "Config/Gem/GemLevelup.xml"; // 荧光宝石升级表
    }

    #region 荧光宝石升级传输结构
    /// <summary>
    /// 荧光宝石升级传输结构 [XSea 2015/8/13]
    /// </summary>
    [ProtoContract]
    public class FluorescentGemUpTransferData
    {
        /// <summary>
        /// 角色id
        /// </summary>
        [ProtoMember(1)]
        public int _RoleID;

        /// <summary>
        /// 类型 0=背包中升级，1=装备栏升级
        /// </summary>
        [ProtoMember(2)]
        public int _UpType;

        /// <summary>
        /// 背包中的格子索引
        /// </summary>
        [ProtoMember(3)]
        public int _BagIndex;

        /// <summary>
        /// 装备部位索引
        /// </summary>
        [ProtoMember(4)]
        public int _Position;

        /// <summary>
        /// 宝石类型
        /// </summary>
        [ProtoMember(5)]
        public int _GemType;

        /// <summary>
        /// 要删除宝石的字典 key=背包格子索引，value=要扣的个数
        /// </summary>
        [ProtoMember(6)]
        public Dictionary<int, int> _DecGoodsDict;
    }
    #endregion
    
    #region 荧光宝石装备栏变动传输结构
    /// <summary>
    /// 荧光宝石装备栏变动传输结构
    /// </summary>
    [ProtoContract]
    public class FluorescentGemEquipChangesTransferData
    {
        /// <summary>
        /// 部位索引
        /// </summary>
        [ProtoMember(1)]
        public int _Position;

        /// <summary>
        /// 宝石类型
        /// </summary>
        [ProtoMember(2)]
        public int _GemType;

        /// <summary>
        /// 宝石信息
        /// </summary>
        [ProtoMember(3)]
        public GoodsData _GoodsData = null;
    }
    #endregion

    #region 荧光宝石挖掘结果传输结构
    /// <summary>
    /// 荧光宝石挖掘结果传输结构
    /// </summary>
    [ProtoContract]
    public class FluorescentGemDigTransferData
    {
        /// <summary>
        /// 结果0：成功，非0=错误代码
        /// </summary>
        [ProtoMember(1)]
        public int _Result;

        /// <summary>
        /// 获取宝石列表
        /// </summary>
        [ProtoMember(2)]
        public List<int> _GemList;
    }
    #endregion

    /// <summary>
    /// 荧光宝石装备部位索引
    /// </summary>
    public enum FluorescentGemEquipPosition
    {
        Start = 0, // 起始
        Helmet = 1, // 头盔
        MainWeapon = 2, // 主手武器
        Necklace = 3, // 项链
        Glove = 4, // 手套
        LeftRing = 5, // 左手戒指
        Clothes = 6, // 衣服
        DeputyWeapon = 7, // 副手武器
        Gaiter = 8, // 护腿
        Shot = 9, // 鞋子
        RightRing = 10, // 右手戒指
        End = 11, // 结尾
    }

    /// <summary>
    /// 荧光宝石类别
    /// </summary>
    public enum FluorescentGemType
    {
        Start = 0, // 起始
        LightGem = 1, // 光石
        FluorescentGem = 2, // 荧石
        EssenceGem = 3, // 精石
        End = 4, // 结尾
    }

    /// <summary>
    /// 荧光宝石挖掘错误码
    /// </summary>
    public enum EFluorescentGemDigErrorCode
    {
        NotOpen = -2, //功能未开启
        Error = -1, // 异常
        Success = 0, // 成功
        LevelTypeError = 1, // 矿坑类型错误
        DigType = 2, // 挖掘类型错误
        BagNotEnoughOne = 3, // 背包空间不足1格
        LevelTypeDataError = 4, // 矿坑数据异常
        PointNotEnough = 5, // 荧光粉末不足
        DiamondNotEnough = 6, // 升星所需钻石不足
        UpdatePointError = 7, // 更新荧光粉末失败
        UpdateDiamondError = 8, // 更新钻石失败
        DigDataError = 9, // 挖掘数据异常
        BagNotEnoughTen = 10, // 背包空间不足10格
        AddGoodsError = 11, // 新增物品失败
        NotGem = 12, // 不是荧光宝石
    }

    /// <summary>
    /// 荧光宝石分解错误码
    /// </summary>
    public enum EFluorescentGemResolveErrorCode
    {
        NotOpen = -2,
        Error = -1, // 异常
        Success = 0, // 成功
        GoodsNotExist = 1, // 物品不存在
        ResolveCountError = 2, // 分解数量错误
        ResolveError = 3, // 分解异常
        NotGem = 4, // 不是荧光宝石
    }

    /// <summary>
    /// 荧光宝石升级错误码
    /// </summary>
    public enum EFluorescentGemUpErrorCode
    {
        NotOpen = -2,
        Error = -1, // 异常
        Success = 0, // 成功
        GoodsNotExist = 1, // 物品不存在
        UpDataError = 2, // 升级数据错误
        MaxLevel = 3, // 宝石已达最高级
        NextLevelDataError = 4, // 下一级宝石数据异常
        GoldNotEnough = 5, // 金币不足
        PositionIndexError = 6, // 部位索引错误
        GemTypeError = 7, // 宝石类型错误
        GemNotEnough = 8, // 宝石不足
        AddGoodsError = 9, // 新增物品失败
        DecGoodsError = 10, // 扣除物品失败
        DecGoodsNotExist = 11, // 要扣除的物品不存在
        DecGoodsNotEnough = 12, // 要扣除的物品不足
        EquipError = 13, // 装备宝石失败
        NotGem = 14, // 不是荧光宝石
        BagNotEnoughOne = 15, //背包不足
    }

    /// <summary>
    /// 荧光宝石装备错误码
    /// </summary>
    public enum EFluorescentGemEquipErrorCode
    {
        NotOpen = -2, //未开启
        Error = -1, // 异常
        Success = 0, // 成功
        GoodsNotExist = 1, // 物品不存在
        PositionIndexError = 2, // 部位索引错误
        GemTypeError = 3, // 宝石类型错误
        EquipError = 4, // 装备失败
        DecGoodsError = 5, // 扣除物品失败
        UnEquipError = 6, // 卸下失败
        NotGem = 7, // 不是荧光宝石
        GemDataError = 8, // 宝石数据错误
    }

    /// <summary>
    /// 荧光宝石卸下错误码
    /// </summary>
    public enum EFluorescentGemUnEquipErrorCode
    {
        NotOpen = -2,
        Error = -1, // 异常
        Success = 0, // 成功
        GoodsNotExist = 1, // 物品不存在
        PositionIndexError = 2, // 部位索引错误
        GemTypeError = 3, // 宝石类型错误
        UnEquipError = 4, // 卸下失败
        BagNotEnoughOne = 5, // 背包空间不足1格
        BagNotEnoughThree = 6, // 背包空间不足3格
    }
}
