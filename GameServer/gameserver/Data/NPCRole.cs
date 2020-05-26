using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// NPC角色的数据,主要用于九宫格移动时客户端动态创建NPC使用
    /// </summary>
    [ProtoContract]
    public class NPCRole
    {
        /// <summary>
        /// NPC的角色ID
        /// </summary>
        [ProtoMember(1)]
        public int NpcID = 0;

        /// <summary>
        /// 格子X坐标
        /// </summary>
        [ProtoMember(2)]
        public int PosX = 0;

        /// <summary>
        /// 格子Y坐标
        /// </summary>
        [ProtoMember(3)]
        public int PosY = 0;

        /// <summary>
        /// 地图编码
        /// </summary>
        [ProtoMember(4)]
        public int MapCode = -1;

        /// <summary>
        /// NPC角色基础配置数据
        /// </summary>
        [ProtoMember(5)]
        public string RoleString = "";

        /// <summary>
        /// npc的方向
        /// </summary>
        [ProtoMember(6)]
        public int Dir = 0;
    }
}
