using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Tmsk.Contract;

namespace Server.Data
{
    /// <summary>
    /// CMD_SPR_QUERYFUBENINFO
    /// </summary>
    [ProtoContract]
    public class CS_QueryFuBen : IProtoBuffData
    {
        // 角色id
        [ProtoMember(1)]
        public int RoleId = 0;

        // 副本地图Id，(服务器没用到！！！)
        [ProtoMember(2)]
        public int MapId = 0;

        // 副本id
        [ProtoMember(3)]
        public int FuBenId = 0;

        public int fromBytes(byte[] data, int offset, int count)
        {
            int pos = offset;
            int mycount = 0;

            for (; mycount < count; )
            {
                int fieldnumber = -1;
                int wt = -1;
                ProtoUtil.GetTag(data, ref pos, ref fieldnumber, ref wt, ref mycount);

                switch (fieldnumber)
                {
                    case 1: this.RoleId = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 2: this.MapId = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.FuBenId = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    default:
                        {
                            throw new ArgumentException("error!!!" + fieldnumber);
                        }
                }
            }
            return pos;
        }

        public byte[] toBytes()
        {
            int total = 0;
            total += ProtoUtil.GetIntSize(this.RoleId, true, 1);
            total += ProtoUtil.GetIntSize(this.MapId, true, 2);
            total += ProtoUtil.GetIntSize(this.FuBenId, true, 3);

            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, this.RoleId);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, this.MapId);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, this.FuBenId);

            return data;
        }
    }
}
