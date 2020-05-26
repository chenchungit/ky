using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace GameServer.Logic.UnionPalace
{
    [ProtoContract]
    public class UnionPalaceData
    {
        //角色id
        [ProtoMember(1)]
        public int RoleID = 0;
        //守护id
        [ProtoMember(2)]
        public int StatueID = 0;
        //守护等级
        [ProtoMember(3)]
        public int StatueLevel = 0;
        //生命上限
        [ProtoMember(4)]
        public int LifeAdd = 0;
        //攻击
        [ProtoMember(5)]
        public int AttackAdd = 0;
        //防御
        [ProtoMember(6)]
        public int DefenseAdd = 0;
        //伤害加成
        [ProtoMember(7)]
        public int AttackInjureAdd = 0;
        //需要战功
        [ProtoMember(8)]
        public int ZhanGongNeed = 0;
        //暴击类型 0=无，1=暴击，2=完美暴击
        [ProtoMember(9)]
        public int BurstType = 0;
        //操作类型
        [ProtoMember(10)]
        public int ResultType = 0;
        //剩余战功
        [ProtoMember(11)]
        public int ZhanGongLeft = 0;
        //战盟等级
        [ProtoMember(12)]
        public int UnionLevel = 0;

        [ProtoMember(13)]
        public int StatueType = 0;
        //数据日期
        //[ProtoMember(13)]
        //public int UpDate = 0;
    }
}

