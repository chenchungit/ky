using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Tmsk.Contract;

namespace Server.Data
{
    /// <summary>
    /// 精灵攻击数据
    /// </summary>
    [ProtoContract]
    public class SpriteAttackData : IProtoBuffData
    {
        [ProtoMember(1)]
        public int roleID = 0;

        [ProtoMember(2)]
        public int roleX = 0;

        [ProtoMember(3)]
        public int roleY = 0;

        [ProtoMember(4)]
        public int enemy = 0;

        [ProtoMember(5)]
        public int enemyX = 0;

        [ProtoMember(6)]
        public int enemyY = 0;

        [ProtoMember(7)]
        public int realEnemyX = 0;

        [ProtoMember(8)]
        public int realEnemyY = 0;

        [ProtoMember(9)]
        public int magicCode = 0;

        [ProtoMember(10)]
        public long clientTicks;

        [ProtoMember(11)]
        public String attackedList;//HX_SERVER 被攻击列表

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
                    case 2: this.roleX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.roleY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.enemy = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 5: this.enemyX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 6: this.enemyY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 7: this.realEnemyX = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 8: this.realEnemyY = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 9: this.magicCode = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 10: this.clientTicks = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 11: this.attackedList = ProtoUtil.StringMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(this.roleID, true, 1);
            total += ProtoUtil.GetIntSize(this.roleX, true, 2);
            total += ProtoUtil.GetIntSize(this.roleY, true, 3);
            total += ProtoUtil.GetIntSize(this.enemy, true, 4);
            total += ProtoUtil.GetIntSize(this.enemyX, true, 5);
            total += ProtoUtil.GetIntSize(this.enemyY, true, 6);
            total += ProtoUtil.GetIntSize(this.realEnemyX, true, 7);
            total += ProtoUtil.GetIntSize(this.realEnemyY, true, 8);
            total += ProtoUtil.GetIntSize(this.magicCode, true, 9);
            total += ProtoUtil.GetLongSize(this.clientTicks, true, 10);
            total += ProtoUtil.GetStringSize(this.attackedList, true, 11);

            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, this.roleID);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, this.roleX);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, this.roleY);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, this.enemy);
            ProtoUtil.IntMemberToBytes(data, 5, ref offset, this.enemyX);
            ProtoUtil.IntMemberToBytes(data, 6, ref offset, this.enemyY);
            ProtoUtil.IntMemberToBytes(data, 7, ref offset, this.realEnemyX);
            ProtoUtil.IntMemberToBytes(data, 8, ref offset, this.realEnemyY);
            ProtoUtil.IntMemberToBytes(data, 9, ref offset, this.magicCode);
            ProtoUtil.LongMemberToBytes(data, 10, ref offset, this.clientTicks);
            ProtoUtil.StringMemberToBytes(data, 11, ref offset, this.attackedList);

            return data;
        }
    }
}
