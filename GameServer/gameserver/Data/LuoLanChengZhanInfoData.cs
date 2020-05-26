using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 罗兰城主信息(战盟信息)
    /// </summary>
    [ProtoContract]
    public class LuoLanChengZhuInfo
    {
        /// <summary>
        /// 当前占领者帮会ID
        /// </summary>
        [ProtoMember(1)]
        public int BHID = 0;

        /// <summary>
        /// 当前占领者帮会名称
        /// </summary>
        [ProtoMember(2)]
        public string BHName = "";

        /// <summary>
        /// 帮会所在区ID(保留未用)
        /// </summary>
        [ProtoMember(3)]
        public int ZoneID = 0;

        /// <summary>
        /// 帮会职务
        /// </summary>
        [ProtoMember(4)]
        public List<int> ZhiWuList = new List<int>();

        /// <summary>
        /// 角色详细信息
        /// </summary>
        [ProtoMember(5)]
        public List<RoleData4Selector> RoleInfoList = new List<RoleData4Selector>();

        /// <summary>
        /// 是否已领取奖励
        /// </summary>
        [ProtoMember(6)]
        public bool isGetReward = true;
    }

    /// <summary>
    /// 罗兰城战请求信息
    /// </summary>
    [ProtoContract]
    public class LuoLanChengZhanRequestInfo
    {
        /// <summary>
        /// 位置
        /// </summary>
        [ProtoMember(1)]
        public int Site = 0;

        /// <summary>
        /// 当前申请的帮会ID
        /// </summary>
        [ProtoMember(2)]
        public int BHID = 0;

        /// <summary>
        /// 出价
        /// </summary>
        [ProtoMember(3)]
        public int BidMoney = 0;
    }

    /// <summary>
    /// 罗兰城战活动龙塔内各帮派的人数列表数据
    /// </summary>
    [ProtoContract]
    public class LuoLanChengZhanRoleCountData
    {
        /// <summary>
        /// 战斗结束的时间
        /// </summary>
        [ProtoMember(1)]
        public int BHID;

        /// <summary>
        /// 王城的战斗状态
        /// </summary>
        [ProtoMember(2)]
        public int RoleCount;
    }
    /// <summary>
    /// 罗兰城战活动各Buff拥有者列表信息
    /// </summary>
    [ProtoContract]
    public class LuoLanChengZhanQiZhiBuffOwnerData
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
    }
    /// <summary>
    /// 罗兰城战活动占领者数据
    /// </summary>
    [ProtoContract]
    public class LuoLanChengZhanLongTaOwnerData
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
        public int OwnerBHid = -1;
    }

    /// <summary>
    /// 罗兰城战活动结果和奖励信息
    /// </summary>
    [ProtoContract]
    public class LuoLanChengZhanResultInfo
    {
        /// <summary>
        /// 当前占领者帮会ID
        /// </summary>
        [ProtoMember(1)]
        public int BHID = 0;

        /// <summary>
        /// 当前占领者帮会名称
        /// </summary>
        [ProtoMember(2)]
        public string BHName = "";

        /// <summary>
        /// 奖励经验
        /// </summary>
        [ProtoMember(3)]
        public long ExpAward;

        /// <summary>
        /// 奖励战功
        /// </summary>
        [ProtoMember(4)]
        public int ZhanGongAward;

        /// <summary>
        /// 战盟资金
        /// </summary>
        [ProtoMember(5)]
        public int ZhanMengZiJin;
    }

    /// <summary>
    /// 罗兰城战请求信息
    /// </summary>
    [ProtoContract]
    public class LuoLanChengZhanRequestInfoEx
    {
        /// <summary>
        /// 位置
        /// </summary>
        [ProtoMember(1)]
        public int Site = 0;

        /// <summary>
        /// 当前申请的帮会ID
        /// </summary>
        [ProtoMember(2)]
        public int BHID = 0;

        /// <summary>
        /// 出价
        /// </summary>
        [ProtoMember(3)]
        public int BidMoney = 0;

        /// <summary>
        /// 帮会名称
        /// </summary>
        [ProtoMember(4)]
        public string BHName = "";
    }
}
