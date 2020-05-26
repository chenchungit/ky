using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.cc.Skill
{
    public class SkillObject
    {
        /// <summary>
        /// 技能ID
        /// </summary>
        public int SkillID
        {
            set;get;
        }
        /// <summary>
        /// 技能名称
        /// </summary>
        public string SkillName
        {
            set; get;
        }
        /// <summary>
        /// 技能说明
        /// </summary>
        public string Remark
        {
            set; get;
        }
        /// <summary>
        /// 血量消耗
        /// </summary>
        public int NeedHP
        {
            set; get;
        }
        /// <summary>
        /// 蓝量消耗
        /// </summary>
        public int NeedMP
        {
            set; get;
        }
        /// <summary>
        /// 需要武器类型
        /// </summary>
        public List<int> NeedWeapons = null;
        /// <summary>
        /// 技能类型
        /// </summary>
        public int SkillType
        {
            set;get;
        }
        /// <summary>
        /// 释放距离
        /// </summary>
        public int Distance
        {
            set; get;
        }
        /// <summary>
        /// 技能等级
        /// </summary>
        public int SkillLevel
        {
            set; get;
        }
        /// <summary>
        /// 冷却时间
        /// </summary>
        public int CDTime
        {
            set; get;
        }
        /// <summary>
        /// 影响范围
        /// </summary>
        public List<int> RangeType = null;
        /// <summary>
        /// 效果表
        /// </summary>
        public List<int> EffectList = null;
        /// <summary>
        /// BUFF表
        /// </summary>
        public List<int> BuffList = null;

        /// <summary>
        /// 伤害类型
        /// </summary>
        public int HarmType
        {
            set; get;
        }
        /// <summary>
        /// 技能伤害
        /// </summary>
        public List<int> SkillHarmList = null;
        
        /// <summary>
        /// 僵直伤害
        /// </summary>
        public int HardHarm
        {
            set; get;
        }
        /// <summary>
        /// 是否弹道
        /// </summary>
        public int Ballistic
        {
            set; get;
        }
        /// <summary>
        /// 学习等级
        /// </summary>
        public int NeedLV
        {
            set; get;
        }
        /// <summary>
        /// 前置技能
        /// </summary>
        public List<int> NeedSkill = null;
        /// <summary>
        /// 开化后前置技能
        /// </summary>
        public List<int> BecomeNeedSkill = null;
        /// <summary>
        /// 下级技能
        /// </summary>
        public List<int> NextSkill = null;
        /// <summary>
        /// 开化后下级技能
        /// </summary>
        public List<int> BecomeNextSkill = null;
        
        /// <summary>
        /// 所属职业
        /// </summary>
        public int Occupation
        {
            set; get;
        }
        /// <summary>
        /// 技能目标
        /// </summary>
        public int  TargetType = 0;
        /// <summary>
        /// 释放类型
        /// </summary>
        public List<int> ReleaseTypeList = null;
        
    }
}
