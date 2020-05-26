using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Tmsk.Contract;

namespace Server.Data
{
    /// <summary>
    /// 精灵攻击结果
    /// </summary>
    [ProtoContract]
    public class SpriteAttackResultData : IProtoBuffData
    {
        [ProtoMember(1)]
        public int enemy = 0;

        [ProtoMember(2)]
        public int burst = 0;

        [ProtoMember(3)]
        public int injure = 0;

        [ProtoMember(4)]
        public double enemyLife = 0;

        [ProtoMember(5)]
        public long newExperience = 0;

        [ProtoMember(6)]
        public long currentExperience = 0;

        [ProtoMember(7)]
        public int newLevel = 0;

        /// <summary>
        /// 梅林伤害值
        /// </summary>
        [ProtoMember(8)]
        public int MerlinInjuer = 0;

        /// <summary>
        /// 梅林伤害类型
        /// </summary>
        [ProtoMember(9)]
        public int MerlinType = 0;

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
                    case 1: this.enemy = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 2: this.burst = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.injure = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.enemyLife = ProtoUtil.DoubleMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 5: this.newExperience = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 6: this.currentExperience = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 7: this.newLevel = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 8: this.MerlinInjuer = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 9: this.MerlinType = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(enemy, true, 1);
            total += ProtoUtil.GetIntSize(burst, true, 2);
            total += ProtoUtil.GetIntSize(injure, true, 3);
            total += ProtoUtil.GetDoubleSize(enemyLife, true, 4);
            total += ProtoUtil.GetLongSize(newExperience, true, 5);
            total += ProtoUtil.GetLongSize(currentExperience, true, 6);
            total += ProtoUtil.GetIntSize(newLevel, true, 7);
            total += ProtoUtil.GetIntSize(MerlinInjuer, true, 8);
            total += ProtoUtil.GetIntSize(MerlinType, true, 9);
            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, enemy);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, burst);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, injure);
            ProtoUtil.DoubleMemberToBytes(data, 4, ref offset, enemyLife);
            ProtoUtil.LongMemberToBytes(data, 5, ref offset, newExperience);
            ProtoUtil.LongMemberToBytes(data, 6, ref offset, currentExperience);
            ProtoUtil.IntMemberToBytes(data, 7, ref offset, newLevel);
            ProtoUtil.IntMemberToBytes(data, 8, ref offset, MerlinInjuer);
            ProtoUtil.IntMemberToBytes(data, 9, ref offset, MerlinType);

            return data;
        }
    }
}
