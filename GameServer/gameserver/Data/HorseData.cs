using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 坐骑数据
    /// </summary>
    [ProtoContract]
    public class HorseData
    {
        /// <summary>
        /// 坐骑的数据库ID
        /// </summary>
        [ProtoMember(1)]
        public int DbID = 0;

        /// <summary>
        /// 坐骑ID
        /// </summary>
        [ProtoMember(2)]
        public int HorseID = 0;

        /// <summary>
        /// 坐骑的形象ID
        /// </summary>
        [ProtoMember(3)]
        public int BodyID = 0;

        /// <summary>
        /// 坐骑强化属性的次数字符串
        /// </summary>
        [ProtoMember(4)]
        public string PropsNum = "";

        /// <summary>
        /// 坐骑强化属性的值的字符串
        /// </summary>
        [ProtoMember(5)]
        public string PropsVal = "";

        /// <summary>
        /// 坐骑的领养时间
        /// </summary>
        [ProtoMember(6)]
        public long AddDateTime = 0;

        /// <summary>
        /// 本次进阶成功前失败的次数
        /// </summary>
        [ProtoMember(7)]
        public int JinJieFailedNum = 0;

        /// <summary>
        /// 坐骑的临时幸运点开始时间
        /// </summary>
        [ProtoMember(8)]
        public long JinJieTempTime = 0;

        /// <summary>
        /// 坐骑的临时幸运点
        /// </summary>
        [ProtoMember(9)]
        public int JinJieTempNum = 0;

        /// <summary>
        /// 本次进阶成功前失败的日ID
        /// </summary>
        [ProtoMember(10)]
        public int JinJieFailedDayID = 0;
    }
}
