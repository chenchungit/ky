using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Server.Data;

namespace Server.Data
{
    /// <summary>
    /// 竞技场机器人数据
    /// </summary>
    [ProtoContract]
    public class PlayerJingJiData
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        [ProtoMember(1)]
        public int roleId;

        /// <summary>
        /// 玩家名称
        /// </summary>
        [ProtoMember(2)]
        public string roleName;

        /// <summary>
        /// 玩家等级
        /// </summary>
        [ProtoMember(3)]
        public int level;

        /// <summary>
        /// 玩家转生等级
        /// </summary>
        [ProtoMember(4)]
        public int changeLiveCount;

        /// <summary>
        /// 玩家职业ID
        /// </summary>
        [ProtoMember(5)]
        public int occupationId;

        /// <summary>
        /// 连胜次数
        /// </summary>
        [ProtoMember(6)]
        public int winCount = 0;

        /// <summary>
        /// 排名
        /// </summary>
        [ProtoMember(7)]
        public int ranking = -1;

        /// <summary>
        /// 下次领取奖励时间戳
        /// </summary>
        [ProtoMember(8)]
        public long nextRewardTime;

        /// <summary>
        /// 下次挑战时间戳
        /// </summary>
        [ProtoMember(9)]
        public long nextChallengeTime;

        /// <summary>
        /// 玩家基础属性
        /// </summary>
        [ProtoMember(10)]
        public double[] baseProps;

        /// <summary>
        /// 玩家扩展属性
        /// </summary>
        [ProtoMember(11)]
        public double[] extProps;

        /// <summary>
        /// 装备数据
        /// </summary>
        [ProtoMember(12)]
        public List<PlayerJingJiEquipData> equipDatas;

        /// <summary>
        /// 技能数据
        /// </summary>
        [ProtoMember(13)]
        public List<PlayerJingJiSkillData> skillDatas;

        /// <summary>
        /// 战力
        /// </summary>
        [ProtoMember(14)]
        public int combatForce = 0;

        /// <summary>
        /// 性别
        /// </summary>
        [ProtoMember(15)]
        public int sex;

        /// <summary>
        /// 名称（不带区号）
        /// </summary>
        [ProtoMember(16)]
        public string name;

        /// <summary>
        /// 区ID
        /// </summary>
        [ProtoMember(17)]
        public int zoneId;

        /// <summary>
        /// 最大连胜次数
        /// </summary>
        [ProtoMember(18)]
        public int MaxWinCnt;

        /// <summary>
        /// 翅膀数据
        /// </summary>
        [ProtoMember(19)]
        public WingData wingData;

        /// <summary>
        /// 功能设置，包括时装
        /// </summary>
        [ProtoMember(20)]
        public long settingFlags;

        /// <summary>
        /// 狗日的需求：膜拜次数要实时更新，不随镜像一起
        /// </summary>
        [ProtoMember(21)]
        public int AdmiredCount;
    }
}
