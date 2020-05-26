using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Tmsk.Contract.Data;

namespace Server.Data
{
    /// <summary>
    /// 充值王排行数据
    /// </summary>
    [ProtoContract]
    public class JieriCZKingData
    {
        /// <summary>
        /// 元宝
        /// </summary>
        [ProtoMember(1)]
        public int YuanBao;

        /// <summary>
        /// 排行榜数据
        /// </summary>
        [ProtoMember(2)]
        public List<InputKingPaiHangData> ListPaiHang;

        /// <summary>
        /// 是否已经领取
        /// </summary>
        [ProtoMember(3)]
        public int State;

        /// <summary>
        /// 排行榜数据
        /// </summary>
        [ProtoMember(4)]
        public List<InputKingPaiHangData> ListPaiHangYestoday;
    }
}
