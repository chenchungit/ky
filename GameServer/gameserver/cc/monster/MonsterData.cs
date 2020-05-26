using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.cc.monster
{
    public class MonsterData
    {
        /// <summary>
        /// 怪物ID
        /// </summary>
        public int MonsterId
        {
            set; get;
        }
        /// <summary>
        /// 怪物名称
        /// </summary>
        public string Name
        {
            set; get;
        }
        /// <summary>
        /// 怪物类型
        /// </summary>
        public int MonsterType
        {
            set; get;
        }
        /// <summary>
        /// 怪物等级
        /// </summary>
        public int Level
        {
            set; get;
        }
        /// <summary>
        /// 基础经验
        /// </summary>
        public int Exp
        {
            set; get;
        }
        /// <summary>
        /// 五行基础经验
        /// </summary>
        public int FiveExp
        {
            set; get;
        }
        /// <summary>
        /// 御灵魂魄基础经验
        /// </summary>
        public int OrenExp
        {
            set; get;
        }
        /// <summary>
        /// 宠物基础经验
        /// </summary>
        public int PetsExp
        {
            set; get;
        }

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHP
        {
            set; get;
        }

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int CurHP
        {
            set; get;
        }
        /// <summary>
        /// 物理攻击值
        /// </summary>
        public int Ad
        {
            set; get;
        }
        /// <summary>
        /// 物理防御
        /// </summary>
        public int Pd
        {
            set; get;
        }
        /// <summary>
        /// 火焰伤害
        /// </summary>
        public int FireDamage
        {
            set; get;
        }

        /// <summary>
        /// 冰霜伤害
        /// </summary>
        public int FrostDamage
        {
            set; get;
        }
        /// <summary>
        /// 闪电伤害
        /// </summary>
        public int LightDamage
        {
            set; get;
        }
        /// <summary>
        /// 毒素伤害
        /// </summary>
        public int ToxicInjury
        {
            set; get;
        }
        /// <summary>
        /// 火焰抗性
        /// </summary>
        public int FireResist
        {
            set; get;
        }
        /// <summary>
        /// 冰霜抗性
        /// </summary>
        public int FrozenResist
        {
            set; get;
        }
        /// <summary>
        /// 闪电抗性
        /// </summary>
        public int LightResist
        {
            set; get;
        }
        /// <summary>
        /// 毒素抗性
        /// </summary>
        public int PoisonResist
        {
            set; get;
        }
        /// <summary>
        /// 闪避几率
        /// </summary>
        public int DodgeChance
        {
            set; get;
        }
        /// <summary>
        /// 命中几率
        /// </summary>
        public int DodgeResis
        {
            set; get;
        }
        /// <summary>
        /// 暴击几率
        /// </summary>
        public int CritChance
        {
            set; get;
        }
        /// <summary>
        /// 韧性几率
        /// </summary>
        public int CritResist
        {
            set; get;
        }
        /// <summary>
        /// 移动速度
        /// </summary>
        public int MoveSpeed
        {
            set; get;
        }
        /// <summary>
        /// 警戒范围
        /// </summary>
        public int AlertRange
        {
            set; get;
        }
        /// <summary>
        /// 追击范围
        /// </summary>
        public int PursuitRange
        {
            set; get;
        }
        /// <summary>
        /// 脱战范围
        /// </summary>
        public int DisengageRange
        {
            set; get;
        }
        /// <summary>
        /// 技能ID1
        /// </summary>
        public List<int> Skills;
        //public int SkillsID1
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 技能ID2
        ///// </summary>
        //public int SkillsID2
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 技能ID3
        ///// </summary>
        //public int SkillsID3
        //{
        //    set; get;
        //}
        /// <summary>
        /// 掉落铜钱
        /// </summary>
        public int DroppedCoin
        {
            set; get;
        }
        /// <summary>
        /// 铜钱掉率
        /// </summary>
        public int CoinRate
        {
            set; get;
        }
        /// <summary>
        /// 掉落ID1
        /// </summary>
        public List<int> Dropped;
        //public int DroppedID1
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 掉落ID2
        ///// </summary>
        //public int DroppedID2
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 掉落ID3
        ///// </summary>
        //public int DroppedID3
        //{
        //    set; get;
        //}
        /// <summary>
        /// 掉率1
        /// </summary>
        public List<int> DroppedRate;
        //public int DroppedRateID1
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 掉率2
        ///// </summary>
        //public int DroppedRateID2
        //{
        //    set; get;
        //}
        ///// <summary>
        ///// 掉率3
        ///// </summary>
        //public int DroppedRateID3
        //{
        //    set; get;
        //}
        /// <summary>
        /// 任务ID1
        /// </summary>
        public List<MonsterTaskObject> MonsterTask;

        /// <summary>
        /// 宠物表
        /// </summary>
        public List<int> PetsTable = null;
        /// <summary>
        /// 魂魄表
        /// </summary>
        public int SpiritsTable
        {
            set; get;
        }
        /// <summary>
        /// 御灵表
        /// </summary>
        public int OrenTable
        {
            set; get;
        }
        /// <summary>
        /// 喊话ID
        /// </summary>
        public List<int> MonsterCall = null;
        /// <summary>
        /// 存活时间
        /// </summary>
        public int SurvivalTime
        {
            set; get;
        }
        /// <summary>
        /// 怪物头像
        /// </summary>
        public string Ico
        {
            set; get;
        }
        /// <summary>
        ///怪物所在的区
        /// </summary>
        public MonsterZone monsterZone
        {
            set; get;
        }

    }
    public class MonsterTaskObject
    {
        public int MissionID1
        {
            set; get;
        }
        /// <summary>
        /// 任务道具1
        /// </summary>
        public int MissionPropsID1
        {
            set; get;
        }
        /// <summary>
        /// 任务掉率1
        /// </summary>
        public int MissionRate1
        {
            set; get;
        }

        /// <summary>
        /// 任务ID2
        /// </summary>
        public int MissionID2
        {
            set; get;
        }
        /// <summary>
        /// 任务道具2
        /// </summary>
        public int MissionPropsID2
        {
            set; get;
        }
        /// <summary>
        /// 任务掉率2
        /// </summary>
        public int MissionRate2
        {
            set; get;
        }

        /// <summary>
        /// 任务ID3
        /// </summary>
        public int MissionID3
        {
            set; get;
        }
        /// <summary>
        /// 任务道具3
        /// </summary>
        public int MissionPropsID3
        {
            set; get;
        }
        /// <summary>
        /// 任务掉率3
        /// </summary>
        public int MissionRate3
        {
            set; get;
        }
    }
}
