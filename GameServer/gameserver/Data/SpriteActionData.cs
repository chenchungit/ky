using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Tmsk.Contract;

namespace Server.Data
{
    /// <summary>
    /// 玩家动作数据封装类
    /// </summary>
    [ProtoContract]
    public class SpriteActionData : IProtoBuffData
    {
        [ProtoMember(1)]
        public int roleID = 0;

        [ProtoMember(2)]
        public int mapCode = 0;

        [ProtoMember(3)]
        public int direction = 0;

        [ProtoMember(4)]
        public int action = 0;

        [ProtoMember(5)]
        public int toX = 0;

        [ProtoMember(6)]
        public int toY = 0;

        [ProtoMember(7)]
        public int targetX = 0;

        [ProtoMember(8)]
        public int targetY = 0;

        [ProtoMember(9)]
        public int yAngle = 0;

        [ProtoMember(10)]
        public int moveToX = 0;

        [ProtoMember(11)]
        public int moveToY = 0;

        [ProtoMember(12)]
        public long clientTicks;

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
                    case 1: this.roleID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 2: this.mapCode = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.direction = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.action = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 5: this.toX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 6: this.toY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 7: this.targetX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 8: this.targetY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 9: this.yAngle = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 10: this.moveToX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 11: this.moveToY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 12: this.clientTicks = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    //default:
                    //    {
                    //        throw new ArgumentException("error!!!");
                    //    }
                }
            }
            return pos;
        }

        public byte[] toBytes()
        {
            int total = 0;
            total += ProtoUtil.GetIntSize(roleID, true, 1);
            total += ProtoUtil.GetIntSize(mapCode, true, 2);
            total += ProtoUtil.GetIntSize(direction, true, 3);
            total += ProtoUtil.GetIntSize(action, true, 4);
            total += ProtoUtil.GetIntSize(toX, true, 5);
            total += ProtoUtil.GetIntSize(toY, true, 6);
            total += ProtoUtil.GetIntSize(targetX, true, 7);
            total += ProtoUtil.GetIntSize(targetY, true, 8);
            total += ProtoUtil.GetIntSize(yAngle, true, 9);
            total += ProtoUtil.GetIntSize(moveToX, true, 10);
            total += ProtoUtil.GetIntSize(moveToY, true, 11);
            total += ProtoUtil.GetLongSize(clientTicks, true, 12);
            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, roleID);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, mapCode);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, direction);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, action);
            ProtoUtil.IntMemberToBytes(data, 5, ref offset, toX);
            ProtoUtil.IntMemberToBytes(data, 6, ref offset, toY);
            ProtoUtil.IntMemberToBytes(data, 7, ref offset, targetX);
            ProtoUtil.IntMemberToBytes(data, 8, ref offset, targetY);
            ProtoUtil.IntMemberToBytes(data, 9, ref offset, yAngle);
            ProtoUtil.IntMemberToBytes(data, 10, ref offset, moveToX);
            ProtoUtil.IntMemberToBytes(data, 11, ref offset, moveToY);
            ProtoUtil.LongMemberToBytes(data, 12, ref offset, clientTicks);
            return data;
        }
    }
}
