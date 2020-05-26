using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Tmsk.Contract;

namespace Server.Data
{
    /// <summary>
    /// 精灵移动数据封装类
    /// </summary>
    [ProtoContract]
    public class SpritePositionData : IProtoBuffData
    {
        [ProtoMember(1)]
        public int roleID = 0;

        [ProtoMember(2)]
        public int mapCode = 0;

        [ProtoMember(3)]
        public int toX = 0;

        [ProtoMember(4)]
        public int toY = 0;

        [ProtoMember(5)]
        public long currentPosTicks = 0;

        [ProtoMember(6)]
        public int toDirection = 0;

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
                    case 1: roleID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 2: mapCode = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: toX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: toY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 5: currentPosTicks = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 6: toDirection = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(roleID, true, 1);
            total += ProtoUtil.GetIntSize(mapCode, true, 2);
            total += ProtoUtil.GetIntSize(toX, true, 3);
            total += ProtoUtil.GetIntSize(toY, true, 4);
            total += ProtoUtil.GetLongSize(currentPosTicks, true, 5);
            total += ProtoUtil.GetIntSize(toDirection, true, 6);
            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, this.roleID);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, this.mapCode);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, this.toX);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, this.toY);
            ProtoUtil.LongMemberToBytes(data, 5, ref offset, this.currentPosTicks);
            ProtoUtil.LongMemberToBytes(data, 6, ref offset, this.toDirection);
            return data;
        }
    }
}
