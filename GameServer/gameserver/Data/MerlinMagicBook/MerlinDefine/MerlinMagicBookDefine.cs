using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 梅林魔法书预定义属性常量等
    /// </summary>
    public class MerlinMagicBookDefine
    {
        public const string MAGIC_BOOK_PATH = "Config/Merlin/MagicBook.xml"; // 升阶配置
        public const string MAGIC_BOOK_STAR_PATH = "Config/Merlin/MagicBookStar.xml"; // 升星配置
        public const string MAGIC_WORD_PATH = "Config/Merlin/MagicWord.xml"; // 秘语配置
    }

    /// <summary>
    /// 梅林魔法书秘语属性索引
    /// </summary>
    public enum EMerlinSecretAttrType
    {
        EMSAT_None = -1, // 无
        EMSAT_FrozenP = 0, // 冰冻几率
        EMSAT_PalsyP = 1, // 麻痹几率
        EMSAT_SpeedDownP = 2, // 减速几率
        EMSAT_BlowP = 3, // 重击几率
        EMSAT_MAX, // 最大值
    }

    /// <summary>
    /// 梅林魔法书升星错误码
    /// </summary>
    public enum EMerlinStarUpErrorCode
    {
        Error = -1, // 异常
        Success = 0, // 成功
        MaxStarNum = 1, // 已达最高星
        StarDataError = 2, // 升星数据异常
        NeedGoodsIDError =3, // 升星所需物品ID异常
        NeedGoodsCountError = 4, // 升星所需物品数量异常
        GoodsNotEnough = 5, // 升星所需物品不足
        DiamondNotEnough = 6, // 升星所需钻石不足
        LevelError = 7, // 阶数异常
        StarError = 8, // 星数异常
    }

    /// <summary>
    /// 梅林魔法书升阶错误码
    /// </summary>
    public enum EMerlinLevelUpErrorCode
    {
        Error = -1, // 异常
        Success = 0, // 成功
        LevelError = 1, // 阶数异常
        MaxLevelNum = 2, // 已达最高阶
        NotMaxStarNum = 3, // 未达最高星，无法升阶
        LevelDataError = 4, // 升阶数据异常
        NeedGoodsIDError = 5, // 升阶所需物品ID异常
        NeedGoodsCountError = 6, // 升阶所需物品数量异常
        GoodsNotEnough = 7, // 升阶所需物品不足
        DiamondNotEnough = 8, // 升阶所需钻石不足
        Fail = 9, // 失败
    }

    /// <summary>
    /// 梅林魔法书擦拭秘语错误码
    /// </summary>
    public enum EMerlinSecretAttrUpdateErrorCode
    {
        Error = -1, // 异常
        Success = 0, // 成功
        LevelError = 1, // 阶数异常
        SecretDataError = 2, // 秘语数据异常
        NeedGoodsIDError = 3, // 擦拭秘语所需物品ID异常
        NeedGoodsCountError = 4, // 擦拭秘语所需物品数量异常
        GoodsNotEnough = 5, // 擦拭秘语所需物品不足
    }

    /// <summary>
    /// 梅林魔法书替换秘语错误码
    /// </summary>
    public enum EMerlinSecretAttrReplaceErrorCode
    {
        Error = -1, // 异常
        Success = 0, // 成功
        NotUpdate = 1, // 请先擦拭秘语
    }
}
