using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 公告消息数据
    /// </summary>
    [ProtoContract]
    public class BulletinMsgData
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        [ProtoMember(1)]
        public string MsgID = "";

        /// <summary>
        /// 播放多少分钟
        /// </summary>
        [ProtoMember(2)]
        public int PlayMinutes = -1;

        /// <summary>
        /// 需要播放次数
        /// </summary>
        [ProtoMember(3)]
        public int ToPlayNum = 0;

        /// <summary>
        /// 公告消息
        /// </summary>
        [ProtoMember(4)]
        public string BulletinText = "";

        /// <summary>
        /// 公告发布的时间
        /// </summary>
        [ProtoMember(5)]
        public long BulletinTicks = 0;

        /// <summary>
        /// 已经播放次数
        /// </summary>
        [ProtoMember(6)]
        public int playingNum = 0;

        /// <summary>
        /// 类型, 0: 公告, 1: 系统通知
        /// </summary>
        [ProtoMember(7)]
        public int MsgType = 0;
    }
}
