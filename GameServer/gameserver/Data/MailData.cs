using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 邮件项数据
    /// </summary>
    [ProtoContract]
    public class MailData
    {
        /// <summary>
        /// MailID
        /// </summary>
        [ProtoMember(1)]
        public int MailID = 0;

        /// <summary>
        /// 发送角色ID
        /// </summary>
        [ProtoMember(2)]
        public int SenderRID = 0;

        /// <summary>
        /// 发送角色名称
        /// </summary>
        [ProtoMember(3)]
        public string SenderRName = "";

        /// <summary>
        /// 发送时间
        /// </summary>
        [ProtoMember(4)]
        public string SendTime = "";

        /// <summary>
        /// 接收角色ID
        /// </summary>
        [ProtoMember(5)]
        public int ReceiverRID = 0;

        /// <summary>
        /// 接收角色名称
        /// </summary>
        [ProtoMember(6)]
        public string ReveiverRName = "";

        /// <summary>
        /// 阅读时间
        /// </summary>
        [ProtoMember(7)]
        public string ReadTime = "1900-01-01 12:00:00";

        /// <summary>
        /// 是否已读
        /// </summary>
        [ProtoMember(8)]
        public int IsRead = 0;

        /// <summary>
        /// 邮件类型
        /// </summary>
        [ProtoMember(9)]
        public int MailType = 0;

        /// <summary>
        /// 是否已经提取了附件(钱和物品)
        /// </summary>
        [ProtoMember(10)]
        public int Hasfetchattachment = 0;

        /// <summary>
        /// 邮件主题
        /// </summary>
        [ProtoMember(11)]
        public string Subject = "";

        /// <summary>
        /// 内容,最多字符数由程序内部控制字符
        /// </summary>
        [ProtoMember(12)]
        public string Content = "";

        /// <summary>
        /// 发送的银两
        /// </summary>
        [ProtoMember(13)]
        public int Yinliang = 0;

        /// <summary>
        /// 发送的铜钱
        /// </summary>
        [ProtoMember(14)]
        public int Tongqian = 0;

        /// <summary>
        /// 发送的元宝
        /// </summary>
        [ProtoMember(15)]
        public int YuanBao = 0;

        /// <summary>
        /// 邮件物品列表
        /// </summary>
        [ProtoMember(16)]
        public List<MailGoodsData> GoodsList = null;
    }
}
