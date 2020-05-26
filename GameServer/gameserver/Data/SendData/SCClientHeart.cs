using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;

namespace Server.Data
{
    /// <summary>
    /// s ---> c
    /// </summary>
    [ProtoContract]
    public class SCClientHeart : IProtoBuffData
    {
        [ProtoMember(1)]
        public int RoleID = 0;

        [ProtoMember(2)]
        public int RandToken = 0;

        /// <summary>
        /// 这个字段就是个笑话，完全是为了兼容一个错误
        /// </summary>
        [ProtoMember(3)]
        public int Ticks = 0;

        /// <summary>
        /// 客户端上报现实中的tick
        /// </summary>
        [ProtoMember(4)]
        public long ReportCliRealTick;

        public SCClientHeart(){}
        public SCClientHeart(int roleID, int token, int allowTicks)
        {
            this.RoleID = roleID;
            this.RandToken = token;
            this.Ticks = allowTicks;
        }

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
                    case 1: this.RoleID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 2: this.RandToken = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.Ticks = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.ReportCliRealTick = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    default:
                        {
                            throw new ArgumentException("error!!!");
                        }
                }
            }
            return pos;
        }

        public byte[] toBytes()
        {
            int total = 0;
            total += ProtoUtil.GetIntSize(RoleID, true, 1);
            total += ProtoUtil.GetIntSize(RandToken, true, 2);
            total += ProtoUtil.GetIntSize(Ticks, true, 3);
            total += ProtoUtil.GetLongSize(this.ReportCliRealTick, true, 4);

            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, RoleID);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, RandToken);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, Ticks);
            ProtoUtil.LongMemberToBytes(data, 4, ref offset, this.ReportCliRealTick);

            return data;
        }
    }
}





