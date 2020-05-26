using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using GameServer.Logic;

namespace Server.Data
{
    /// <summary>
    /// 天赋数据
    /// </summary>
    [ProtoContract]
    public class TalentData
    {
        /// <summary>
        /// 是否开放
        /// </summary>
        [ProtoMember(1, IsRequired = true)]
        public bool IsOpen = false;

        /// <summary>
        /// 已获取天赋点数
        /// </summary>
        [ProtoMember(2, IsRequired = true)]
        public int TotalCount = 0;

        /// <summary>
        /// 当前天赋点注入经验
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public long Exp = 0;

        /// <summary>
        /// 效果分类加点数量
        /// </summary>
        [ProtoMember(4, IsRequired = true)]
        public Dictionary<int, int> CountList = new Dictionary<int, int>();

        /// <summary>
        /// 效果列表（天赋类型，效果列表）
        /// </summary>
        [ProtoMember(5, IsRequired = true)]
        public List<TalentEffectItem> EffectList = new List<TalentEffectItem>();

        /// <summary>
        /// 单个技能(技能id，技能等级)
        /// </summary>
        [ProtoMember(6, IsRequired = true)]
        public Dictionary<int, int> SkillOneValue = new Dictionary<int, int>();

        /// <summary>
        /// 全部技能
        /// </summary>
        [ProtoMember(7, IsRequired = true)]
        public int SkillAllValue = 0;

        /// <summary>
        /// 状态
        /// </summary>
        [ProtoMember(8, IsRequired = true)]
        public int State = 0;

        /// <summary>
        /// 职业
        /// </summary>
        [ProtoMember(9, IsRequired = true)]
        public int Occupation = 0;
    }

    /// <summary>
    /// 效果项
    /// </summary>
    [ProtoContract]
    public class TalentEffectItem
    {
        /// <summary>
        /// 效果id
        /// </summary>
        [ProtoMember(1, IsRequired = true)]
        public int ID = 0;

        /// <summary>
        /// 效果等级
        /// </summary>
        [ProtoMember(2, IsRequired = true)]
        public int Level = 0;

        /// <summary>
        /// 天赋类型
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public int TalentType = 1;

        /// <summary>
        /// 效果
        /// </summary>
        [ProtoMember(4, IsRequired = true)]
        public List<TalentEffectInfo> ItemEffectList = null;
    }

    /// <summary>
    /// 效果数据
    /// </summary>
    [ProtoContract]
    public class TalentEffectInfo
    {
        /// <summary>
        /// 效果类型
        /// </summary>
        [ProtoMember(1, IsRequired = true)]
        public int EffectType = 0;

        /// <summary>
        /// 效果id
        /// </summary>
        [ProtoMember(2, IsRequired = true)]
        public int EffectID = 0;

        /// <summary>
        /// 效果值
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public double EffectValue = 0;
    }
}
