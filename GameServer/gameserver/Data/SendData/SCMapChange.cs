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
    /// CMD_SPR_MAPCHANGE
    /// 双向
    /// </summary>
    [ProtoContract]
    public class SCMapChange : IProtoBuffData
    {
        [ProtoMember(1)]
        public int RoleID = 0;

        [ProtoMember(2)]
        public int TeleportID = 0;

        [ProtoMember(3)]
        public int NewMapCode = 0;

        [ProtoMember(4)]
        public int ToNewMapX = 0;

         [ProtoMember(5)]
        public int ToNewMapY = 0;

         [ProtoMember(6)]
         public int ToNewDiection = 0;

         [ProtoMember(7)]
        public int State = 0;

        public SCMapChange(){}

        public SCMapChange(int roleID, int teleportID, int newMapCode, int toNewMapX, int toNewMapY, int toNewDiection,int state)
        {
            this.RoleID = roleID;
            this.TeleportID = teleportID;
            this.NewMapCode = newMapCode;
            this.ToNewMapX = toNewMapX;
            this.ToNewMapY = toNewMapY;
            this.ToNewDiection = toNewDiection;
            this.State = state;
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
                    case 2: this.TeleportID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.NewMapCode = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.ToNewMapX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 5: this.ToNewMapY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 6: this.ToNewDiection = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 7: this.State = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(TeleportID, true, 2);
            total += ProtoUtil.GetIntSize(NewMapCode, true, 3);
            total += ProtoUtil.GetIntSize(ToNewMapX, true, 4);
            total += ProtoUtil.GetIntSize(ToNewMapY, true, 5);
            total += ProtoUtil.GetIntSize(ToNewDiection, true, 6);
            total += ProtoUtil.GetIntSize(State, true, 7);

            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, RoleID);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, TeleportID);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, NewMapCode);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, ToNewMapX);
            ProtoUtil.IntMemberToBytes(data, 5, ref offset, ToNewMapY);
            ProtoUtil.IntMemberToBytes(data, 6, ref offset, ToNewDiection);
            ProtoUtil.IntMemberToBytes(data, 7, ref offset, State);

            return data;
        }
    }
}



