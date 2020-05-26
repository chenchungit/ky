using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 节日活动相关xml数据，推送到客户端
    /// </summary>
    [ProtoContract]
    public class JieriXmlData
    {
        /// <summary>
        /// 登录周ID
        /// </summary>
        [ProtoMember(1)]
        public List<string> XmlList = null;

        /// <summary>
        /// 当前服务器配置版本号 客户端使用此字段检测与本地的配置是否一致
        /// </summary>
        [ProtoMember(2)]
        public int Version;
    }
}
