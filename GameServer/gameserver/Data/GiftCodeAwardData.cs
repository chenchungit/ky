using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Server.Data;

namespace Server.Data
{
    /// <summary>
    /// GiftCode礼包数据
    /// </summary>
    [ProtoContract]
    public class GiftCodeAwardData
    {
        /// <summary>
        /// Dbid
        /// </summary>
        [ProtoMember(1)]
        public int Dbid = 0;

        /// <summary>
        /// userid
        /// </summary>
        [ProtoMember(2)]
        public string UserId = "";

        /// <summary>
        /// RoleID
        /// </summary>
        [ProtoMember(3)]
        public int RoleID = 0;

        /// <summary>
        /// 礼品id
        /// </summary>
        [ProtoMember(4)]
        public string GiftId = "";

        /// <summary>
        /// 礼品码
        /// </summary>
        [ProtoMember(5)]
        public string CodeNo = "";

        /// <summary>
        /// reset
        /// </summary>
        public void reset()
        {
            this.Dbid = 0;
            this.UserId = "";
            this.RoleID = 0;
            this.GiftId = "";
            this.CodeNo = "";
        }
    }

    /// <summary>
    /// GiftCode礼包配置数据
    /// </summary>
    [ProtoContract]
    public class GiftCodeInfo
    {
        /// <summary>
        /// 礼品码
        /// </summary>
        [ProtoMember(1)]
        public string GiftCodeTypeID = "";

        /// <summary>
        /// 礼品码名字描述
        /// </summary>
        [ProtoMember(2)]
        public string GiftCodeName = "";

        /// <summary>
        /// 频道列表
        /// </summary>
        [ProtoMember(3)]
        public List<string> ChannelList = new List<string>();

        /// <summary>
        /// 平台列表
        /// </summary>
        [ProtoMember(4)]
        public List<int> PlatformList = new List<int>();

        /// <summary>
        /// 开始时间
        /// </summary>
        [ProtoMember(5)]
        public DateTime TimeBegin = DateTime.MinValue;

        /// <summary>
        /// 截止时间
        /// </summary>
        [ProtoMember(6)]
        public DateTime TimeEnd = DateTime.MinValue;

        /// <summary>
        /// 大区列表
        /// </summary>
        [ProtoMember(7)]
        public List<int> ZoneList = new List<int>();

        /// <summary>
        /// 用户类型
        /// </summary>
        [ProtoMember(8)]
        public GiftCodeUserType UserType = GiftCodeUserType.All;

        /// <summary>
        /// 使用次数
        /// </summary>
        [ProtoMember(9)]
        public int UseCount = 0;

        /// <summary>
        /// 道具列表
        /// </summary>
        [ProtoMember(10)]
        public List<GoodsData> GoodsList = new List<GoodsData>();

        /// <summary>
        /// 额外的物品列表
        /// </summary>
        [ProtoMember(11)]
        public List<GoodsData> ProGoodsList = new List<GoodsData>();
    }

    /// <summary>
    /// GiftCode礼包领取类型
    /// </summary>
    public enum GiftCodeUserType
    {
        All = 0,
        Role = 1,
        User = 2,//预留（未使用），根据账号领取礼包
    }

    /// <summary>
    /// 状态
    /// </summary>
    public enum GiftCodeResultType
    {
        Default = 0,     //默认
        Success = 1,     //成功
        EnoUserOrRole = -1,//没有此UID 或角色ID不存在
        EAware = -2,    //礼包错误
        Fail = -3,       //领取失败（其他原有）
        Exception = -4,     //领取异常
    };
}
