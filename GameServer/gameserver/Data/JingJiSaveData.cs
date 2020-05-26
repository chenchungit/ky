using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// GS处理完后需要保存的玩家竞技场数据
    /// </summary>
    [ProtoContract]
    public class JingJiSaveData
    {
        /// <summary>
        /// 是否胜利
        /// </summary>
        [ProtoMember(1)]
        public bool isWin;

        /// <summary>
        /// 玩家ID
        /// </summary>
        [ProtoMember(2)]
        public int roleId;

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
        /// 下次挑战时间戳
        /// </summary>
        [ProtoMember(5)]
        public long nextChallengeTime;

        /// <summary>
        /// 玩家基础属性
        /// </summary>
        [ProtoMember(6)]
        public double[] baseProps;

        /// <summary>
        /// 玩家扩展属性
        /// </summary>
        [ProtoMember(7)]
        public double[] extProps;

        /// <summary>
        /// 装备数据
        /// </summary>
        [ProtoMember(8)]
        public List<PlayerJingJiEquipData> equipDatas;

        /// <summary>
        /// 技能数据
        /// </summary>
        [ProtoMember(9)]
        public List<PlayerJingJiSkillData> skillDatas;

        /// <summary>
        /// 战力
        /// </summary>
        [ProtoMember(10)]
        public int combatForce = 0;

        /// <summary>
        /// 被挑战者ID
        /// </summary>
        [ProtoMember(11)]
        public int robotId;

        /// <summary>
        /// 翅膀数据
        /// </summary>
        [ProtoMember(12)]
        public WingData wingData;

        /// <summary>
        /// 功能设置，包括时装隐藏
        /// </summary>
        [ProtoMember(13)]
        public long settingFlags;
    }
}
