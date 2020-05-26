using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 战盟mini数据【战盟基础小数据，目前只包括战盟名称，区域ID,主要提供给gameserver缓存，数据量不要太多，太多了同步维护比较复杂】
    /// </summary>
    [ProtoContract]
    public class BangHuiMiniData
    {
        /// <summary>
        /// 帮派的ID
        /// </summary>
        [ProtoMember(1)]
        public int BHID = 0;

        /// <summary>
        /// 帮派的名称
        /// </summary>
        [ProtoMember(2)]
        public string BHName = "";

        /// <summary>
        /// 区ID
        /// </summary>
        [ProtoMember(3)]
        public int ZoneID = 0;
    }
}
