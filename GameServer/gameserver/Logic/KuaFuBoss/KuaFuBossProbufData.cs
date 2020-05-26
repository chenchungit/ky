using System.Collections.Generic;
using ProtoBuf;
using System;

namespace Server.Data
{
    public enum KuaFuBossGameStates
    {
        None, //无
        SignUp, //报名时间
        Wait, //等待开始
        Start, //开始
        Awards, //有未领取奖励
        NotJoin, // 未参加本次活动
    }

    [ProtoContract]
    public class KuaFuBossSceneStateData
    {
        /// <summary>
        /// Boss当前数量
        /// </summary>
        [ProtoMember(1)]
        public int BossNum;

        /// <summary>
        /// 总Boss数量
        /// </summary>
        [ProtoMember(2)]
        public int TotalBossNum;

        /// <summary>
        /// Monster当前数量
        /// </summary>
        [ProtoMember(3)]
        public int MonsterNum;

        /// <summary>
        /// 总Monster数量
        /// </summary>
        [ProtoMember(4)]
        public int TotalNormalNum;
    }
}
