using System.Collections.Generic;
using ProtoBuf;
using System;

namespace Server.Data
{
    public enum LangHunLingYuGameStates
    {
        None, //无
        SignUp, //报名时间
        Wait, //等待开始
        Start, //开始
        Awards, //有未领取奖励
        NotJoin, // 未参加本次活动
    }

    /// <summary>
    /// 圣域城主信息 to client
    /// </summary>
    [ProtoContract]
    public class LangHunLingYuKingShowData
    {
        // 角色当前的膜拜次数
        [ProtoMember(1)]
        public int AdmireCount;

        /// <summary>
        /// 形象数据(类型为RoleData4Selector)
        /// </summary>
        [ProtoMember(2)]
        public RoleData4Selector RoleData4Selector;
    }

    /// <summary>
    /// 历届圣域城主信息 to client
    /// </summary>
    [ProtoContract]
    public class LangHunLingYuKingShowDataHist
    {
        // 被膜拜次数
        [ProtoMember(1)]
        public int AdmireCount;

        // 获得时间
        [ProtoMember(2)]
        public DateTime CompleteTime;

        // 帮派名
        [ProtoMember(3)]
        public string BHName;

        /// <summary>
        /// 形象数据(类型为RoleData4Selector)
        /// </summary>
        [ProtoMember(4)]
        public RoleData4Selector RoleData4Selector;
    }

    /// <summary>
    /// 战斗结束的结果和奖励
    /// </summary>
    [ProtoContract]
    public class LangHunLingYuAwardsData
    {
        /// <summary>
        /// 战斗结果(0失败,1胜利)
        /// </summary>
        [ProtoMember(1)]
        public int Success;

        /// <summary>
        /// 奖励物品列表
        /// </summary>
        [ProtoMember(2)]
        public List<AwardsItemData> AwardsItemDataList;
    }

    /// <summary>
    /// 圣域争霸活动龙塔内各帮派的人数列表数据
    /// </summary>
    [ProtoContract]
    public class BangHuiRoleCountData
    {
        /// <summary>
        /// 战斗结束的时间
        /// </summary>
        [ProtoMember(1)]
        public int BHID;

        /// <summary>
        /// 人数
        /// </summary>
        [ProtoMember(2)]
        public int RoleCount;

        /// <summary>
        /// 服务器ID
        /// </summary>
        [ProtoMember(3)]
        public int ServerID;
    }

    /// <summary>
    /// 圣域争霸活动各Buff拥有者列表信息
    /// </summary>
    [ProtoContract]
    public class LangHunLingYuQiZhiBuffOwnerData
    {
        /// <summary>
        /// 战斗结束的时间
        /// </summary>
        [ProtoMember(1)]
        public int NPCID;

        /// <summary>
        /// 拥有者帮会ID
        /// </summary>
        [ProtoMember(2)]
        public int OwnerBHID;

        /// <summary>
        /// 拥有者帮会名
        /// </summary>
        [ProtoMember(3)]
        public string OwnerBHName;

        /// <summary>
        /// 占有者帮会的区号
        /// </summary>
        [ProtoMember(4)]
        public int OwnerBHZoneId = 0;
    }
    /// <summary>
    /// 圣域争霸活动占领者数据
    /// </summary>
    [ProtoContract]
    public class LangHunLingYuLongTaOwnerData
    {
        /// <summary>
        /// 王城战盟名称
        /// </summary>
        [ProtoMember(1)]
        public string OwnerBHName = "";

        /// <summary>
        /// 王城战盟ID
        /// </summary>
        [ProtoMember(2)]
        public int OwnerBHid = 0;

        /// <summary>
        /// 占有者帮会的区号
        /// </summary>
        [ProtoMember(3)]
        public int OwnerBHZoneId = 0;

        /// <summary>
        /// 占有者帮会的服务器号
        /// </summary>
        [ProtoMember(4)]
        public int OwnerBHServerId = 0;
    }

    [ProtoContract]
    public class LangHunLingYuWorldData
    {
        [ProtoMember(1)]
        public List<LangHunLingYuCityData> CityList = new List<LangHunLingYuCityData>();
    }

    [ProtoContract]
    public class LangHunLingYuCityData
    {
        [ProtoMember(1)]
        public int CityId;

        [ProtoMember(2)]
        public int CityLevel;

        [ProtoMember(3)]
        public BangHuiMiniData Owner;

        [ProtoMember(4)]
        public List<BangHuiMiniData> AttackerList = new List<BangHuiMiniData>();
    }

    [ProtoContract]
    public class LangHunLingYuRoleData
    {
        /// <summary>
        /// 报名状态
        /// </summary>
        [ProtoMember(1)]
        public int SignUpState;

        /// <summary>
        /// 每日奖励领取状态，0表示未领取，非0表示已领取
        /// </summary>
        [ProtoMember(2)]
        public List<int> GetDayAwardsState;

        /// <summary>
        /// 自己相关的（占领或进攻）最多10个城池的列表，列表可为null，数量可能不足10个
        /// </summary>
        [ProtoMember(3)]
        public List<LangHunLingYuCityData> SelfCityList = new List<LangHunLingYuCityData>();

        /// <summary>
        /// 4个其他城池的列表
        /// </summary>
        [ProtoMember(4)]
        public List<LangHunLingYuCityData> OtherCityList = new List<LangHunLingYuCityData>();
    }

    public class LangHunLingYuBangHuiData
    {
        /// <summary>
        /// 报名状态
        /// </summary>
        public int SignUpState;

        /// <summary>
        /// 自己相关的（占领或进攻）最多10个城池的列表，列表可为null，数量可能不足10个
        /// </summary>
        public List<int> SelfCityList = new List<int>();

        /// <summary>
        /// 奖励标记
        /// </summary>
        public int DayAwardFlags;

        /// <summary>
        /// 报名时间
        /// </summary>
        public long SignUpTime;
    }
}
