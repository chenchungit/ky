using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;

namespace Server.Data
{
    /// <summary>
    /// 找怪
    /// CMD_SPR_FINDMONSTER
    /// 双向
    /// </summary>
    [ProtoContract]
    public class SCFindMonster : IProtoBuffData
    {
        [ProtoMember(1)]
        public int RoleID = 0;

        [ProtoMember(2)]
        public int X = 0;

        [ProtoMember(3)]
        public int Y = 0;

        [ProtoMember(4)]
        public int Num = 0;

        public SCFindMonster(){}

        public SCFindMonster(int roleID, int x, int y, int num)
        {
            this.RoleID = roleID;
            this.X = x;
            this.Y = y;
            this.Num = num;
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
                    case 2: this.X = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.Y = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.Num = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(X, true, 2);
            total += ProtoUtil.GetIntSize(Y, true, 3);
            total += ProtoUtil.GetIntSize(Num, true, 4);

            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, RoleID);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, X);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, Y);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, Num);

            return data;
        }
    }
}





