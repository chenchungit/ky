using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 成就数据
    /// </summary>
    [ProtoContract]
    public class ChengJiuData
    {
        /// <summary>
        /// RoleID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID = 0;

        /// <summary>
        /// 成就值
        /// </summary>
        [ProtoMember(2)]
        public long ChengJiuPoints = 0;

        /// <summary>
        /// 总的杀怪数量
        /// </summary>
        [ProtoMember(3)]
        public long TotalKilledMonsterNum = 0;

        /// <summary>
        /// 总的累积登录次数
        /// </summary>
        [ProtoMember(4)]
        public long TotalLoginNum = 0;

        /// <summary>
        /// 连续登录次数
        /// </summary>
        [ProtoMember(5)]
        public int ContinueLoginNum = 0;

        /// <summary>
        /// 完成的成就标志 和 是否领取标志 
        /// 16个bit 一组，前14个bit表示id， 后面一次是完成bit 和 奖励bit
        /// </summary>
        [ProtoMember(6)]
        public List<ushort> ChengJiuFlags = null;

        /// <summary>
        /// 刚刚完成的成就ID
        /// </summary>
        [ProtoMember(7)]
        public int NowCompletedChengJiu = 0;

        /// <summary>
        /// 总的杀boss数量
        /// </summary>
        [ProtoMember(8)]
        public long TotalKilledBossNum = 0;

        // MU 新增 Begin [3/12/2014 LiaoWei]
        /// <summary>
        /// 普通副本通关计数
        /// </summary>
        [ProtoMember(9)]
        public long CompleteNormalCopyMapCount = 0;

        /// <summary>
        /// 精英副本通关计数
        /// </summary>
        [ProtoMember(10)]
        public long CompleteHardCopyMapCount = 0;

        /// <summary>
        /// 炼狱副本通关计数
        /// </summary>
        [ProtoMember(11)]
        public long CompleteDifficltCopyMapCount = 0;

        /// <summary>
        /// 战盟成就-战功值
        /// </summary>
        [ProtoMember(12)]
        public long GuildChengJiu = 0;

        /// <summary>
        /// 军衔成就-军衔等级
        /// </summary>
        [ProtoMember(13)]
        public long JunXianChengJiu = 0;

        /*/// <summary>
        /// 强化数据
        /// </summary>
        [ProtoMember(14)]
        public int FrogeNum = 0;

        /// <summary>
        /// 追加数据
        /// </summary>
        [ProtoMember(15)]
        public int AppendNum = 0;

        /// <summary>
        /// 合成数据
        /// </summary>
        [ProtoMember(16)]
        public int MergeData = 0;*/

        // MU 新增 End [3/12/2014 LiaoWei]
    }
}
