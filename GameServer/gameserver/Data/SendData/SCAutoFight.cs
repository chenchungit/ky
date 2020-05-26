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
    /// CMD_SPR_UPSKILLLEVEL
    /// </summary>
    [ProtoContract]
    public class SCAutoFight : IProtoBuffData
    {
        [ProtoMember(1)]
        public int State = 0;

        [ProtoMember(2)]
        public int RoleID = 0;

        [ProtoMember(3)]
        public int FightType = 0;

        [ProtoMember(4)]
        public int Tag = 0;


        public SCAutoFight(){}

        public SCAutoFight(int state, int roleID, int fightType, int extTag1)
        {
            this.State = state;
            this.RoleID = roleID;
            this.FightType = fightType;
            this.Tag = extTag1;
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
                    case 2: this.RoleID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.FightType = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.Tag = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(RoleID, true, 2);
            total += ProtoUtil.GetIntSize(FightType, true, 3);
            total += ProtoUtil.GetIntSize(Tag, true, 4);

            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, State);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, RoleID);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, FightType);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, Tag);

            return data;
        }
    }
}




