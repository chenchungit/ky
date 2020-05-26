using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 守护之灵数据
    /// </summary>
    [ProtoContract]
    public class GuardSoulData
    {
        //  对应TuJianShouHuType.xml中的Type，表示该守护之灵对应的图鉴Type
        [ProtoMember(1, IsRequired = true)]
        public int Type = 0;

        // 装备的栏位，-1表示该守护之灵未装备, >= 0 表示装备的栏位id
        [ProtoMember(2, IsRequired = true)]
        public int EquipSlot = -1;
    }

    /// <summary>
    /// 守护雕像数据, 用户服务器和客户端通信
    /// </summary>
    [ProtoContract]
    public class GuardStatueData
    {
        // 等级
        [ProtoMember(1, IsRequired = true)]
        public int Level = 0;

        // 品阶
        [ProtoMember(2, IsRequired = true)]
        public int Suit = 1;

        // 拥有的守护点
        [ProtoMember(3, IsRequired = true)]
        public int HasGuardPoint = 0;

        // 激活的所有守护之灵
        [ProtoMember(4, IsRequired = true)]
        public List<GuardSoulData> GuardSoulList = new List<GuardSoulData>();
    }

    /// <summary>
    /// 守护雕像详细信息，用于GameServer和GameDBServer通信
    /// </summary>
    [ProtoContract]
    public class GuardStatueDetail
    {
        // 是否激活
        [ProtoMember(1, IsRequired = true)]
        public bool IsActived = false;

        // 服务器内部使用, 表示上次回收守护点的当天一共回收了多少守护点
        [ProtoMember(2, IsRequired = true)]
        public int LastdayRecoverPoint = 0;

        // 服务器内部使用，表示上次回收守护点相对于2011年11月11日的偏移天数
        [ProtoMember(3, IsRequired = true)]
        public int LastdayRecoverOffset = 0;

        // 服务器内部使用，表示激活的守护之灵装备栏位数
        [ProtoMember(4, IsRequired = true)]
        public int ActiveSoulSlot = 0;

        // 用于通知客户端的守护雕像数据
        [ProtoMember(5, IsRequired = true)]
        public GuardStatueData GuardStatue = new GuardStatueData();
    }
}