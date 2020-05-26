using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 镖车数据
    /// </summary>
    [ProtoContract]
    public class BiaoCheData
    {
        /// <summary>
        /// 拥有者的RoleID
        /// </summary>
        [ProtoMember(1)]
        public int OwnerRoleID = 0;

        /// <summary>
        /// 镖车的ID
        /// </summary>
        [ProtoMember(2)]
        public int BiaoCheID = 0;

        /// <summary>
        /// 镖车的名称
        /// </summary>
        [ProtoMember(3)]
        public string BiaoCheName = "";

        /// <summary>
        /// 押镖的类型
        /// </summary>
        [ProtoMember(4)]
        public int YaBiaoID = 0;

        /// <summary>
        /// 所在的地图的编号
        /// </summary>
        [ProtoMember(5)]
        public int MapCode = 0;

        /// <summary>
        /// 当前所在的位置X坐标
        /// </summary>
        [ProtoMember(6)]
        public int PosX = 0;

        /// <summary>
        /// 当前所在的位置Y坐标
        /// </summary>
        [ProtoMember(7)]
        public int PosY = 0;

        /// <summary>
        /// 当前的方向
        /// </summary>
        [ProtoMember(8)]
        public int Direction = 0;

        /// <summary>
        /// 最大的生命值
        /// </summary>
        [ProtoMember(9)]
        public int LifeV = 0;

        /// <summary>
        /// 每一刀伤害的生命值
        /// </summary>
        [ProtoMember(10)]
        public int CutLifeV = 0;

        /// <summary>
        /// 开始的时间
        /// </summary>
        [ProtoMember(11)]
        public long StartTime = 0;

        /// <summary>
        /// 形象编号
        /// </summary>
        [ProtoMember(12)]
        public int BodyCode = 0;

        /// <summary>
        /// 头像编号
        /// </summary>
        [ProtoMember(13)]
        public int PicCode = 0;

        /// <summary>
        /// 当前的生命值
        /// </summary>
        [ProtoMember(14)]
        public int CurrentLifeV = 0;

        /// <summary>
        /// 镖车所有者的名称
        /// </summary>
        [ProtoMember(15)]
        public string OwnerRoleName = "";
    }
}
