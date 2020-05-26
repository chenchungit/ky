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
    public class SCSkillLevelUp : IProtoBuffData
    {
        [ProtoMember(1)]
        public int State = 0;

        [ProtoMember(2)]
        public int RoleID = 0;

        [ProtoMember(3)]
        public int SkillID = 0;

        [ProtoMember(4)]
        public int SkillLevel = 0;

         [ProtoMember(5)]
        public int SkillUsedNum = 0;


        public SCSkillLevelUp(){}

        public SCSkillLevelUp(int state, int roleID, int skillID, int skillLevel, int SkillUsedNum)
        {
            this.State = state;
            this.RoleID = roleID;
            this.SkillID = skillID;
            this.SkillLevel = skillLevel;
            this.SkillUsedNum = SkillUsedNum;
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
                    case 3: this.SkillID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.SkillLevel = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 5: this.SkillUsedNum = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(SkillID, true, 3);
            total += ProtoUtil.GetIntSize(SkillLevel, true, 4);
            total += ProtoUtil.GetIntSize(SkillUsedNum, true, 5);

            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, State);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, RoleID);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, SkillID);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, SkillLevel);
            ProtoUtil.IntMemberToBytes(data, 5, ref offset, SkillUsedNum);

            return data;
        }
    }
}



