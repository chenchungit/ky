using System;
using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Shapes;
using System.Collections.Generic;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// Lua 函数处理结果数据定义【不一定要针对npc】
    /// gameserver 执行完npc lua对话后传送给客户端的数据包
    /// </summary>
    [ProtoContract]  
    public class LuaCallResultData
    {
        /// <summary>
        /// 当前的地图编号
        /// </summary>
        [ProtoMember(1)]
        public int MapCode = 0;

        /// <summary>
        /// 当前的角色ID
        /// </summary>
        [ProtoMember(2)]
        public int RoleID = 0;

        /// <summary>
        /// 当前的NPCID
        /// </summary>
        [ProtoMember(3)]
        public int NPCID = 0;

        /// <summary>
        /// 执行结果 大于等于0表示成功，小于0表示失败
        /// </summary>
        [ProtoMember(4)]
        public int IsSuccess;

        /// <summary>
        /// 执行结果字符串，根据不同的脚本函数返回不同的结果
        /// </summary>
        [ProtoMember(5)]
        public String Result;

        /// <summary>
        /// 执行标志，当客户端需要确切的返回结果并进行验证时使用
        /// </summary>
        [ProtoMember(6)]
        public int Tag;

        /// <summary>
        /// 扩展ID
        /// </summary>
        [ProtoMember(7)]
        public int ExtensionID;

        /// <summary>
        /// Lua 调用函数体,包括参数
        /// </summary>
        [ProtoMember(8)]
        public String LuaFunction;

        /// <summary>
        /// 是否强迫刷新
        /// </summary>
        [ProtoMember(9)]
        public int ForceRefresh = 0;
    }
}
