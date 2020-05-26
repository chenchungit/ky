using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 角斗场结束角色数据
    /// </summary>
    [ProtoContract]
    public class NewGoodsPackData
    {
        [ProtoMember(1)]
        public int ownerRoleID = 0;

        [ProtoMember(2)]
        public string ownerRoleName = "";

        [ProtoMember(3)]
        public int autoID = 0;

        [ProtoMember(4)]
        public int goodsPackID = 0;

        [ProtoMember(5)]
        public int mapCode = 0;

        [ProtoMember(6)]
        public int toX = 0;

        [ProtoMember(7)]
        public int toY = 0;

        [ProtoMember(8)]
        public int goodsID = 0;

        [ProtoMember(9)]
        public int goodsNum = 0;

        [ProtoMember(10)]
        public long productTicks = 0;

        [ProtoMember(11)]
        public int teamID = 0;

        [ProtoMember(12)]
        public string teamRoleIDs = "";

        [ProtoMember(13)]
        public int lucky = 0;

        [ProtoMember(14)]
        public int excellenceInfo = 0;

        [ProtoMember(15)]
        public int appendPropLev = 0;

        [ProtoMember(16)]
        public int forge_Level = 0;
    }
}
