using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProtoBuf;

namespace GameServer.Logic.TuJian
{
    /// <summary>
    /// 守护雕像常量定义
    /// </summary>
    public class GuardStatueConst
    {
        // 守护之灵配置文件
        public const string GuardSoulCfgFile = "Config/TuJianShouHuType.xml";

        // 守护点回收配置文件
        public const string GuardPointCfgFile = "Config/JingPoShouHu.xml";

        // 守护雕像升阶配置文件
        public const string GuardSuitUpCfgFile = "Config/ShouHuSuitUp.xml";

        // 守护雕像升级配置文件
        public const string GuardLevelUpCfgFile = "Config/ShouHuLevelUp.xml";

        // 守护雕像默认等级
        public const int GuardStatueDefaultLevel = 0;

        // 守护雕像默认品阶
        public const int GuardStatueDefaultSuit = 1;

        // 守护之灵未装备槽位标识
        public const int GuardSoulNotEquipSlot = -1;

        // 守护之灵属性加成，默认等级系数
        public const double DefaultLevelFactor = 1.0;

        // 守护之灵属性加成，默认品阶系数
        public const double DefaultSuitFactor = 1.0;
    }

    public enum GuardStatueErrorCode
    {
        Success,                // 成功
        NotOpen,                // 守护雕像未开放
        ContainNotActiveTuJian, // 请求回收的精魄中含有未激活的图鉴项
        MoreThanTodayCanRecover, //超过今日最大可回收守护点
        GuardPointNotEnough,    //守护点不足
        MaterialNotEnough,  //材料不足
        NeedSuitUp, //需要先提升品阶
        NeedLevelUp, //需要先提升等级
        SuitIsFull, //品阶已满
        LevelIsFull, //等级已满
        ConfigError, //服务器配置出错
        DBFailed //数据库出错
    }

    /// <summary>
    /// 守护之灵配置信息
    /// </summary>
    class GuardSoul
    {
        public int TypeID;  // 图鉴Type, 每张地图一个图鉴type
        public string Name;     // 守护之灵名字，方便调试观察
        public int GoodsID;     // 守护之灵道具ID
    }

    /// <summary>
    /// 回收精魄得到守护点
    /// </summary>
    class GuardPoint
    {
        public int ItemID;      // 图鉴ID，每种怪对应一个item
        public int TypeID;      // 怪对应的图鉴Type, 先读取出来，暂时未用到
        public string Name;    // 图鉴名字，方便观察
        public int GoodsID;     //图鉴道具ID
        public int Point;       // 图鉴回收可以得到的守护点
    }

    /// <summary>
    /// 守护雕像升级消耗
    /// </summary>
    class GuardLevelUp
    {
        public int Level;                   // 等级
        public int NeedGuardPoint;  //升级到该等级需要消耗的守护点
    }

    /// <summary>
    /// 守护之灵升阶消耗
    /// </summary>
    class GuardSuitUp
    {
        public int Suit;                        // 品阶
        //public int NeedGuardPoint;      // 升阶需要消耗的守护点
        public int NeedGoodsID;         // 需要消耗的材料
        public int NeedGoodsCnt;        // 需要消耗的材料数量
    }
}
