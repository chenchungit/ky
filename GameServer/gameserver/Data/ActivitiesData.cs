using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 礼品码活动数据
    /// </summary>
    [ProtoContract]
    public class ActivitiesData
    {
        /// <summary>
        /// Activities.xml 字符串
        /// </summary>
        [ProtoMember(1)]
        public String ActivitiesXmlString = "";
    }
}
