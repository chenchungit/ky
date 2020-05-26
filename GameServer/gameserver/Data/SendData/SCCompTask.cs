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
    /// CMD_SPR_COMPTASK
    /// </summary>
    [ProtoContract]
    public class SCCompTask : IProtoBuffData
    {
        [ProtoMember(1)]
        public int roleID = 0;

        [ProtoMember(2)]
        public int npcID = 0;

        [ProtoMember(3)]
        public int taskID = 0;

        [ProtoMember(4)]
        public int state = 0;

        public SCCompTask()
        {
        }

        public SCCompTask(int roleID, int npcID, int taskID, int state)
        {
            this.roleID = roleID;
            this.npcID = npcID;
            this.taskID = taskID;
            this.state = state;
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
                    case 1: this.roleID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 2: this.npcID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 3: this.taskID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
                    case 4: this.state = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount); break;
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
            total += ProtoUtil.GetIntSize(npcID, true, 2);
            total += ProtoUtil.GetIntSize(taskID, true, 3);
            total += ProtoUtil.GetIntSize(state, true, 4);

            byte[] data = new byte[total];
            int offset = 0;

            ProtoUtil.IntMemberToBytes(data, 1, ref offset, roleID);
            ProtoUtil.IntMemberToBytes(data, 2, ref offset, npcID);
            ProtoUtil.IntMemberToBytes(data, 3, ref offset, taskID);
            ProtoUtil.IntMemberToBytes(data, 4, ref offset, state);

            return data;
        }
    }
}

