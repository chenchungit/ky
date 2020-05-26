using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 商城销售数据
    /// </summary>
    [ProtoContract]
    public class MallSaleData
    {
        /// <summary>
        /// Mall.xml 字符串
        /// </summary>
        [ProtoMember(1)]
        public String MallXmlString = "";

        /// <summary>
        /// MallTab.xml 字符串
        /// </summary>
        [ProtoMember(2)]
        public String MallTabXmlString = "";

        /// <summary>
        /// QiangGou.xml 字符串 ===>这个xml内部最多有需要的三条数据
        /// </summary>
        [ProtoMember(3)]
        public String QiangGouXmlString = "";
    }
}
