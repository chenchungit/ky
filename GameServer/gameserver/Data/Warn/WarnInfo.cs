using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 警告信息
    /// </summary>
    [ProtoContract]
    public class WarnInfo
    {
        /// <summary>
        /// id
        /// </summary>
        [ProtoMember(1, IsRequired = true)]
        public int ID = 0;

        /// <summary>
        /// 描述
        /// </summary>
        [ProtoMember(2, IsRequired = true)]
        public string Desc = "";

        /// <summary>
        /// 时间，秒
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public int TimeSec = 0;

        /// <summary>
        /// 操作方式
        /// </summary>
        [ProtoMember(4, IsRequired = true)]
        public int Operate = 0;
    }

    /// <summary>
    /// 警告类型
    /// </summary>
    public enum WarnOperateType
    {
        /// <summary>
        /// 手动关闭
        /// </summary>
        Hand = 1,

        /// <summary>
        /// 封号72小时
        /// </summary>
        Hour72 = 2,

        /// <summary>
        /// 封号7天
        /// </summary>
        Day7 = 3,

        /// <summary>
        /// 封号永久
        /// </summary>
        Forever = 4,
    }

}
