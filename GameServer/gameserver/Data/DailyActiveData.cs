using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    // 每日活跃数据 [2/25/2014 LiaoWei]
    [ProtoContract]
    class DailyActiveData
    {
        /// <summary>
        /// RoleID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID = 0;

        /// <summary>
        /// 活跃值
        /// </summary>
        [ProtoMember(2)]
        public long DailyActiveValues = 0;

        /// <summary>
        /// 总的杀怪数量
        /// </summary>
        [ProtoMember(3)]
        public long TotalKilledMonsterCount = 0;

        /// <summary>
        /// 每日总登陆计数
        /// </summary>
        [ProtoMember(4)]
        public long DailyActiveTotalLoginCount = 0;

        /// <summary>
        /// 连续在线时间
        /// </summary>
        [ProtoMember(5)]
        public int DailyActiveOnLineTimer = 0;

        /// <summary>
        /// 完成的成就标志 和 是否领取标志 
        /// 16个bit 一组，前14个bit表示id， 后面一次是完成bit 和 奖励bit
        /// </summary>
        [ProtoMember(6)]
        public List<ushort> DailyActiveInforFlags = null;

        /// <summary>
        /// 刚刚完成的活跃ID
        /// </summary>
        [ProtoMember(7)]
        public int NowCompletedDailyActiveID = 0;

        /// <summary>
        /// 总的杀boss数量
        /// </summary>
        [ProtoMember(8)]
        public int TotalKilledBossCount = 0;

        /// <summary>
        /// 普通副本通关次数
        /// </summary>
        [ProtoMember(9)]
        public int PassNormalCopySceneNum = 0;

        /// <summary>
        /// 精英副本通关次数
        /// </summary>
        [ProtoMember(10)]
        public int PassHardCopySceneNum = 0;

        /// <summary>
        /// 炼狱副本通关次数
        /// </summary>
        [ProtoMember(11)]
        public int PassDifficultCopySceneNum = 0;

        /// <summary>
        /// 商城消费
        /// </summary>
        [ProtoMember(12)]
        public int BuyItemInMall = 0;

        /// <summary>
        /// 完成日常任务计数
        /// </summary>
        [ProtoMember(13)]
        public int CompleteDailyTaskCount = 0;

        /// <summary>
        /// 完成血色堡垒计数
        /// </summary>
        [ProtoMember(14)]
        public int CompleteBloodCastleCount = 0;

        /// <summary>
        /// 完成恶魔广场计数
        /// </summary>
        [ProtoMember(15)]
        public int CompleteDaimonSquareCount = 0;

        /// <summary>
        /// 完成阵营战计数
        /// </summary>
        [ProtoMember(16)]
        public int CompleteBattleCount = 0;

        /// <summary>
        /// 装备强化
        /// </summary>
        [ProtoMember(17)]
        public int EquipForge = 0;

        /// <summary>
        /// 装备追加
        /// </summary>
        [ProtoMember(18)]
        public int EquipAppend = 0;

        /// <summary>
        /// 转生
        /// </summary>
        [ProtoMember(19)]
        public int ChangeLife = 0;

        /// <summary>
        /// 合成水果
        /// </summary>
        [ProtoMember(20)]
        public int MergeFruit = 0;

        /// <summary>
        /// 领取标记
        /// </summary>
        [ProtoMember(21)]
        public int GetAwardFlag = 0;
    }
}
