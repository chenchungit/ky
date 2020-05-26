using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 成就符文提升数据
    /// </summary>
    [ProtoContract]
    public class AchievementRuneData
    {
        /// <summary>
        /// RoleID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID = 0;

        /// <summary>
        /// 符文id
        /// </summary>
        [ProtoMember(2)]
        public int RuneID = 0;

        /// <summary>
        /// 生命加成
        /// </summary>
        [ProtoMember(3)]
        public int LifeAdd = 0;

        /// <summary>
        /// 攻击力加成
        /// </summary>
        [ProtoMember(4)]
        public int AttackAdd = 0;

        /// <summary>
        /// 防御力加成
        /// </summary>
        [ProtoMember(5)]
        public int DefenseAdd = 0;

        /// <summary>
        /// 闪避加成
        /// </summary>
        [ProtoMember(6)]
        public int DodgeAdd = 0;

        /// <summary>
        /// 成就消耗
        /// </summary>
        [ProtoMember(7)]
        public int Achievement = 0;

        /// <summary>
        /// 钻石消耗
        /// </summary>
        [ProtoMember(8)]
        public int Diamond = 0;

        /// <summary>
        /// 暴击类型 0=无，1=暴击，2=完美暴击
        /// </summary>
        [ProtoMember(9)]
        public int BurstType = 0;
  
        /// <summary>
        /// 提升结果类型
        /// </summary>
        [ProtoMember(10)]
        public int UpResultType = 0;

        /// <summary>
        /// 成就剩余
        /// </summary>
        [ProtoMember(11)]
        public int AchievementLeft = 0;
        

      
    }
}
