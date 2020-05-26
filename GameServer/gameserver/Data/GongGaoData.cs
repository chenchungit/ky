using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 公告数据
    /// </summary>
    [ProtoContract]
    public class GongGaoData
    {
        /// <summary>
        /// 有公告信息
        /// </summary>
        [ProtoMember(1)]
        public int nHaveGongGao = 0;

        /// <summary>
        /// 连续登录奖励未领取
        /// </summary>
        [ProtoMember(2)]
        public int nLianXuLoginReward = 0;

        /// <summary>
        /// 累计登录奖励未领取
        /// </summary>
        [ProtoMember(3)]
        public int nLeiJiLoginReward = 0;

        /// <summary>
        /// 公告信息
        /// </summary>
        [ProtoMember(4)]
        public String strGongGaoInfo = "";
    }
}
