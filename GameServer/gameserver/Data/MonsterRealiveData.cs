using System;
using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Shapes;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 怪的复活数据定义
    /// </summary>
    [ProtoContract]
    class MonsterRealiveData
    {
        /// <summary>
        /// 当前的角色ID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID = 0;

        /// <summary>
        /// 当前的角色X坐标
        /// </summary>
        [ProtoMember(2)]
        public int PosX = 0;

        /// <summary>
        /// 当前的角色Y坐标
        /// </summary>
        [ProtoMember(3)]
        public int PosY = 0;

        /// <summary>
        /// 当前的角色方向
        /// </summary>
        [ProtoMember(4)]
        public int Direction = 0;
    }
}
