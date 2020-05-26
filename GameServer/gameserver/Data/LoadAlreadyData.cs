using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Tmsk.Contract;

namespace Server.Data
{
    /// <summary>
    /// 刷新图标状态数据
    /// </summary>
    [ProtoContract]
    public class LoadAlreadyData : IProtoBuffData
    {
        [ProtoMember(1)]
        public int RoleID = 0;

        [ProtoMember(2)]
        public int MapCode = 0;

        [ProtoMember(3)]
        public long StartMoveTicks = 0;

        [ProtoMember(4)]
        public int CurrentX = 0;

        [ProtoMember(5)]
        public int CurrentY = 0;

        [ProtoMember(6)]
        public int CurrentDirection = 0;

        [ProtoMember(7)]
        public int Action = 0;

        [ProtoMember(8)]
        public int ToX = 0;

        [ProtoMember(9)]
        public int ToY = 0;

        [ProtoMember(10)]
        public double MoveCost = 1.0;

        [ProtoMember(11)]
        public int ExtAction = 0;

        [ProtoMember(12)]
        public string PathString = "";

        [ProtoMember(13)]
        public int CurrentPathIndex = 0;

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
                    case 2: this.MapCode = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.StartMoveTicks = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.CurrentX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 5: this.CurrentY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 6: this.CurrentDirection = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 7: this.Action = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 8: this.ToX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 9: this.ToY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 10: this.MoveCost = ProtoUtil.DoubleMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 11: this.ExtAction = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 12: this.PathString = ProtoUtil.StringMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 13: this.CurrentPathIndex = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(MapCode, true, 2);
            total += ProtoUtil.GetLongSize(StartMoveTicks, true, 3);
            total += ProtoUtil.GetIntSize(CurrentX, true, 4);
            total += ProtoUtil.GetIntSize(CurrentY, true, 5);
            total += ProtoUtil.GetIntSize(CurrentDirection, true, 6);
            total += ProtoUtil.GetIntSize(Action, true, 7);
            total += ProtoUtil.GetIntSize(ToX, true, 8);
            total += ProtoUtil.GetIntSize(ToY, true, 9);
            total += ProtoUtil.GetDoubleSize(MoveCost, true, 10);
            total += ProtoUtil.GetIntSize(ExtAction, true, 11);
            total += ProtoUtil.GetStringSize(PathString, true, 12);
            total += ProtoUtil.GetIntSize(CurrentPathIndex, true, 13);
            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, RoleID);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, MapCode);
            ProtoUtil.LongMemberToBytes(data, 3, ref offset, StartMoveTicks);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, CurrentX);
            ProtoUtil.IntMemberToBytes(data, 5, ref offset, CurrentY);
            ProtoUtil.IntMemberToBytes(data, 6, ref offset, CurrentDirection);
            ProtoUtil.IntMemberToBytes(data, 7, ref offset, Action);
            ProtoUtil.IntMemberToBytes(data, 8, ref offset, ToX);
            ProtoUtil.IntMemberToBytes(data, 9, ref offset, ToY);
            ProtoUtil.DoubleMemberToBytes(data, 10, ref offset, MoveCost);
            ProtoUtil.IntMemberToBytes(data, 11, ref offset, ExtAction);
            ProtoUtil.StringMemberToBytes(data, 12, ref offset, PathString);
            ProtoUtil.IntMemberToBytes(data, 13, ref offset, CurrentPathIndex);

            return data;
        }
    }
}
