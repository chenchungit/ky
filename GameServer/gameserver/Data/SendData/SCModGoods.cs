using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;

namespace Server.Data
{
    /// <summary>
    /// 元宝快速接受并完成任务
    /// CMD_SPR_MOD_GOODS
    /// </summary>
    [ProtoContract]
    public class SCModGoods : IProtoBuffData
    {
        [ProtoMember(1)]
        public int State = 0;

        [ProtoMember(2)]
        public int ModType = 0;

        [ProtoMember(3)]
        public int ID = 0;

        [ProtoMember(4)]
        public int IsUsing = 0;

         [ProtoMember(5)]
        public int Site = 0;

         [ProtoMember(6)]
        public int Count = 0;

         [ProtoMember(7)]
        public int BagIndex = 0;

         [ProtoMember(8)]
        public int NewHint = 0;

        public SCModGoods(){}

        public SCModGoods(int state, int modType, int id, int isusing, int site, int gcount, int bagIndex, int newHint)
        {
            this.State = state;
            this.ModType = modType;
            this.ID = id;
            this.IsUsing = isusing;
            this.Site = site;
            this.Count = gcount;
            this.BagIndex = bagIndex;
            this.NewHint = newHint;
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
                    case 1: this.State = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 2: this.ModType = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.ID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.IsUsing = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 5: this.Site = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 6: this.Count = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 7: this.BagIndex = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 8: this.NewHint = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(State, true, 1);
            total += ProtoUtil.GetIntSize(ModType, true, 2);
            total += ProtoUtil.GetIntSize(ID, true, 3);
            total += ProtoUtil.GetIntSize(IsUsing, true, 4);
            total += ProtoUtil.GetIntSize(Site, true, 5);
            total += ProtoUtil.GetIntSize(Count, true, 6);
            total += ProtoUtil.GetIntSize(BagIndex, true, 7);
            total += ProtoUtil.GetIntSize(NewHint, true, 8);

            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, State);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, ModType);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, ID);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, IsUsing);
            ProtoUtil.IntMemberToBytes(data, 5, ref offset, Site);
            ProtoUtil.IntMemberToBytes(data, 6, ref offset, Count);
            ProtoUtil.IntMemberToBytes(data, 7, ref offset, BagIndex);
            ProtoUtil.IntMemberToBytes(data, 8, ref offset, NewHint);

            return data;
        }
    }
}


