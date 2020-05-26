using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 战旗数据
    /// </summary>
    [ProtoContract]
    public class JunQiData
    {
        /// <summary>
        /// 战旗的ID
        /// </summary>
        [ProtoMember(1)]
        public int JunQiID = 0;

        /// <summary>
        /// 战旗的名称
        /// </summary>
        [ProtoMember(2)]
        public string QiName = "";

        /// <summary>
        /// 战旗的级别
        /// </summary>
        [ProtoMember(3)]
        public int JunQiLevel = 0;

        /// <summary>
        /// 区ID
        /// </summary>
        [ProtoMember(4)]
        public int ZoneID = 0;

        /// <summary>
        /// 战盟的ID
        /// </summary>
        [ProtoMember(5)]
        public int BHID = 0;

        /// <summary>
        /// 战盟名称
        /// </summary>
        [ProtoMember(6)]
        public string BHName = "";

        /// <summary>
        /// 旗座NPC的ID
        /// </summary>
        [ProtoMember(7)]
        public int QiZuoNPC = 0;

        /// <summary>
        /// 所在的地图的编号
        /// </summary>
        [ProtoMember(8)]
        public int MapCode = 0;

        /// <summary>
        /// 当前所在的位置X坐标
        /// </summary>
        [ProtoMember(9)]
        public int PosX = 0;

        /// <summary>
        /// 当前所在的位置Y坐标
        /// </summary>
        [ProtoMember(10)]
        public int PosY = 0;

        /// <summary>
        /// 当前的方向
        /// </summary>
        [ProtoMember(11)]
        public int Direction = 0;

        /// <summary>
        /// 最大的生命值
        /// </summary>
        [ProtoMember(12)]
        public int LifeV = 0;

        /// <summary>
        /// 每一刀伤害的生命值
        /// </summary>
        [ProtoMember(13)]
        public int CutLifeV = 0;

        /// <summary>
        /// 开始的时间
        /// </summary>
        [ProtoMember(14)]
        public long StartTime = 0;

        /// <summary>
        /// 形象编号
        /// </summary>
        [ProtoMember(15)]
        public int BodyCode = 0;

        /// <summary>
        /// 头像编号
        /// </summary>
        [ProtoMember(16)]
        public int PicCode = 0;

        /// <summary>
        /// 当前的生命值
        /// </summary>
        [ProtoMember(17)]
        public int CurrentLifeV = 0;
    }
}
