using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    public class OldResourceInfo
    {
        /// <summary>
        /// 索引
        /// </summary>
        [ProtoMember(1)]
        public int type=1;

        /// <summary>
        /// 经验
        /// </summary>
        [ProtoMember(2)]
        public int exp = 0;

        /// <summary>
        /// 绑定金币
        /// </summary>
        [ProtoMember(3)]
        public int bandmoney=0;

        /// <summary>
        /// 魔晶
        /// </summary>
        [ProtoMember(4)]
        public int mojing=0;

        /// <summary>
        /// 成就
        /// </summary>
        [ProtoMember(5)]
        public int chengjiu=0;

        /// <summary>
        /// 声望
        /// </summary>
        [ProtoMember(6)]
        public int shengwang=0;

        /// <summary>
        /// 战功
        /// </summary>
        [ProtoMember(7)]
        public int zhangong=0;

        /// <summary>
        /// 昨日未完成次数
        /// </summary>
        [ProtoMember(8)]
        public int leftCount=0;

        /// <summary>
        /// roleid
        /// </summary>
        [ProtoMember(9)]
        public int roleId;

        /// <summary>
        /// 绑钻
        /// </summary>
        [ProtoMember(10)]
        public int bandDiamond = 0;

        /// <summary>
        /// 星魂
        /// </summary>
        [ProtoMember(11)]
        public int xinghun = 0;

        /// <summary>
        /// 元素粉末
        /// </summary>
        [ProtoMember(12)]
        public int yuanSuFenMo = 0;

        /// <summary>
        /// 领取日期
        /// </summary>
        [ProtoMember(13)]
        public int HasGetOffsetDay;
    }
}
