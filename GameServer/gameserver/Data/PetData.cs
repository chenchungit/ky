using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 宠物数据
    /// </summary>
    [ProtoContract]
    public class PetData
    {
        /// <summary>
        /// 宠物的数据库ID
        /// </summary>
        [ProtoMember(1)]
        public int DbID = 0;

        /// <summary>
        /// 宠物ID
        /// </summary>
        [ProtoMember(2)]
        public int PetID = 0;

        /// <summary>
        /// 宠物的名称
        /// </summary>
        [ProtoMember(3)]
        public string PetName = "";

        /// <summary>
        /// 宠物的类型(0: 普通宠物, 高级宠物)
        /// </summary>
        [ProtoMember(4)]
        public int PetType = 0;

        /// <summary>
        /// 宠物的喂食次数
        /// </summary>
        [ProtoMember(5)]
        public int FeedNum = 0;

        /// <summary>
        /// 宠物复活的次数
        /// </summary>
        [ProtoMember(6)]
        public int ReAliveNum = 0;

        /// <summary>
        /// 宠物的领养时间
        /// </summary>
        [ProtoMember(7)]
        public long AddDateTime = 0;

        /// <summary>
        /// 宠物的扩展属性
        /// </summary>
        [ProtoMember(8)]
        public string PetProps = "";

        /// <summary>
        /// 宠物的级别
        /// </summary>
        [ProtoMember(9)]
        public int Level = 1;
    }
}
