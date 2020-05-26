using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// Buffer数据
    /// </summary>
    [ProtoContract]
    public class BufferData
    {
        /// <summary>
        /// Buffer的ID
        /// </summary>
        [ProtoMember(1)]
        public int BufferID = 0;

        /// <summary>
        /// Buffer开始计时的时间
        /// </summary>
        [ProtoMember(2)]
        public long StartTime = 0;

        /// <summary>
        /// Buffer计时秒数长度
        /// </summary>
        [ProtoMember(3)]
        public int BufferSecs = 0;

        /// <summary>
        /// Buffer的动态值
        /// </summary>
        [ProtoMember(4)]
        public long BufferVal = 0;

        /// <summary>
        /// Buffer的类型(0:DBBuffer 1:临时Buffer)
        /// </summary>
        [ProtoMember(5)]
        public int BufferType = 0;
    }

    /// <summary>
    /// Buffer数据
    /// </summary>
    [ProtoContract]
    public class OtherBufferData
    {
        /// <summary>
        /// Buffer的ID
        /// </summary>
        [ProtoMember(1)]
        public int BufferID = 0;

        /// <summary>
        /// Buffer开始计时的时间
        /// </summary>
        [ProtoMember(2)]
        public long StartTime = 0;

        /// <summary>
        /// Buffer计时秒数长度
        /// </summary>
        [ProtoMember(3)]
        public int BufferSecs = 0;

        /// <summary>
        /// Buffer的动态值
        /// </summary>
        [ProtoMember(4)]
        public long BufferVal = 0;

        /// <summary>
        /// Buffer的类型(0:DBBuffer 1:临时Buffer)
        /// </summary>
        [ProtoMember(5)]
        public int BufferType = 0;

        /// <summary>
        /// buff所在对象的RoleID
        /// </summary>
        [ProtoMember(6)]
        public int RoleID = 0;
    }

    /// <summary>
    /// buffer mini 数据 [4/10/2014 LiaoWei]
    /// </summary>
    [ProtoContract]
    public class BufferDataMini
    {
        /// <summary>
        /// Buffer的ID
        /// </summary>
        [ProtoMember(1)]
        public int BufferID = 0;

        /// <summary>
        /// Buffer开始计时的时间
        /// </summary>
        [ProtoMember(2)]
        public long StartTime = 0;

        /// <summary>
        /// Buffer计时秒数长度
        /// </summary>
        [ProtoMember(3)]
        public int BufferSecs = 0;

        /// <summary>
        /// Buffer的动态值
        /// </summary>
        [ProtoMember(4)]
        public long BufferVal = 0;

        /// <summary>
        /// Buffer的类型(0:DBBuffer 1:临时Buffer)
        /// </summary>
        [ProtoMember(5)]
        public int BufferType = 0;
    }
}
