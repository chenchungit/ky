using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 翅膀数据
    /// </summary>
    [ProtoContract]
    public class WingData
    {
        /// <summary>
        /// 翅膀的数据库ID
        /// </summary>
        [ProtoMember(1)]
        public int DbID = 0;

        /// <summary>
        /// 翅膀ID
        /// </summary>
        [ProtoMember(2)]
        public int WingID = 0;

        /// <summary>
        /// 翅膀强化的次数
        /// </summary>
        [ProtoMember(3)]
        public int ForgeLevel = 0;

        /// <summary>
        /// 翅膀的领养时间
        /// </summary>
        [ProtoMember(4)]
        public long AddDateTime = 0;

        /// <summary>
        /// 本次进阶成功前失败的次数
        /// </summary>
        [ProtoMember(5)]
        public int JinJieFailedNum = 0;

        /// <summary>
        /// 是否使用
        /// </summary>
        [ProtoMember(6)]
        public int Using = 0;

        /// <summary>
        /// 升星经验值
        /// </summary>
        [ProtoMember(7)]
        public int StarExp = 0;

        /// <summary>
        /// 注灵次数
        /// </summary>
        [ProtoMember(8)]
        public int ZhuLingNum = 0;

        /// <summary>
        /// 注魂次数
        /// </summary>
        [ProtoMember(9)]
        public int ZhuHunNum = 0;
    }
}
