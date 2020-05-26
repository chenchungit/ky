using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 刷新图标状态数据
    /// </summary>
    [ProtoContract]
    public class ActivityIconStateData
    {
        /// <summary>
        /// 前15位表示功能状态编号，后一位表示图标状态（0为不显示感叹号、1为显示）
        /// </summary>
        [ProtoMember(1)]
        public ushort [] arrIconState;
    }
}
