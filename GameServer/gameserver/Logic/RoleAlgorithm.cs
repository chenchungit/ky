#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Interface;
using Server.Data;
using GameServer.Logic.JingJiChang;
using Server.Tools;
using GameServer.Core.Executor;
using GameServer.Logic.Marriage.CoupleArena;

/*
 属性改造 [8/16/2013 LiaoWei]
 1.去掉重量相关
 
 */
namespace GameServer.Logic
{
    public class ExtPropItem
    {
        public UnitPropIndexes UnitProp;    // 一级属性
        public double[] Coefficient;        // 系数数组，对应职业系数百分比
        public ExtPropIndexes ExtProp;      // 依赖的二级属性
        public ExcellencePorp[] ExcellentProp;  // 依赖的卓越属性
        public ExtPropIndexes ExtPropPercent;   // 依赖的百分比加成
        public double PropCoef;             // 属性倍数加成
        public BufferItemTypes[] BufferProp;    // 药水buff加成
    }

    /// <summary>
    /// 角色的相关属性和攻击伤害计算算法
    /// </summary>
    public class RoleAlgorithm
    {
        public static Dictionary<ExtPropIndexes, ExtPropItem> roleExtPropDic = new Dictionary<ExtPropIndexes, ExtPropItem>()
        {
            { ExtPropIndexes.Strong, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1, } },
            { ExtPropIndexes.AttackSpeed, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.MoveSpeed, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.MinDefense, new ExtPropItem(){ExtProp = ExtPropIndexes.AddDefense, ExtPropPercent = ExtPropIndexes.IncreasePhyDefense, PropCoef = 1,
                UnitProp = UnitPropIndexes.Dexterity, Coefficient = new double[(int)UnitPropIndexes.Max]{0.88 * 0.6, 0.64 * 0.6, 0.76 * 0.6, 0.8 * 0.6}, 
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP9}, }
            },
            { ExtPropIndexes.MaxDefense, new ExtPropItem(){ ExtProp = ExtPropIndexes.AddDefense, ExtPropPercent = ExtPropIndexes.IncreasePhyDefense, PropCoef = 1,
                UnitProp = UnitPropIndexes.Dexterity, Coefficient = new double[(int)UnitPropIndexes.Max]{0.88, 0.64, 0.76, 0.8},
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP9},
                BufferProp = new BufferItemTypes[]{BufferItemTypes.TimeAddDefense}, }
            },
            { ExtPropIndexes.MinMDefense, new ExtPropItem(){ ExtProp = ExtPropIndexes.AddDefense, ExtPropPercent = ExtPropIndexes.IncreaseMagDefense, PropCoef = 1,
                UnitProp = UnitPropIndexes.Dexterity, Coefficient = new double[(int)UnitPropIndexes.Max]{0.6 * 0.6, 0.84 * 0.6, 0.72 * 0.6, 0.76 * 0.6},
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP9},
                BufferProp = new BufferItemTypes[]{BufferItemTypes.TimeAddMDefense}, }
            },
            { ExtPropIndexes.MaxMDefense, new ExtPropItem(){ ExtProp = ExtPropIndexes.AddDefense, ExtPropPercent = ExtPropIndexes.IncreaseMagDefense, PropCoef = 1,
                UnitProp = UnitPropIndexes.Dexterity, Coefficient = new double[(int)UnitPropIndexes.Max]{0.6, 0.84, 0.72, 0.76},
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP9}, }
            },
            { ExtPropIndexes.MinAttack, new ExtPropItem(){ ExtProp = ExtPropIndexes.AddAttack, ExtPropPercent = ExtPropIndexes.IncreasePhyAttack, PropCoef = 1,
                UnitProp = UnitPropIndexes.Strength, Coefficient = new double[(int)UnitPropIndexes.Max]{0.76 * 0.6, 0, 0.8 * 0.6, 0.84 * 0.6},
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP1, ExcellencePorp.EXCELLENCEPORP2}, }
            },
            { ExtPropIndexes.MaxAttack, new ExtPropItem(){ ExtProp = ExtPropIndexes.AddAttack, ExtPropPercent = ExtPropIndexes.IncreasePhyAttack, PropCoef = 1,
                UnitProp = UnitPropIndexes.Strength, Coefficient = new double[(int)UnitPropIndexes.Max]{0.76, 0, 0.8, 0.84},
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP1, ExcellencePorp.EXCELLENCEPORP2},
                BufferProp = new BufferItemTypes[]{BufferItemTypes.TimeAddAttack}, }
            },
            { ExtPropIndexes.MinMAttack, new ExtPropItem(){ ExtProp = ExtPropIndexes.AddAttack, ExtPropPercent = ExtPropIndexes.IncreaseMagAttack, PropCoef = 1,
                UnitProp = UnitPropIndexes.Intelligence, Coefficient = new double[(int)UnitPropIndexes.Max]{0, 0.88 * 0.6, 0, 0.92 * 0.6},
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP1, ExcellencePorp.EXCELLENCEPORP2}, }
            },
            { ExtPropIndexes.MaxMAttack, new ExtPropItem(){ ExtProp = ExtPropIndexes.AddAttack, ExtPropPercent = ExtPropIndexes.IncreaseMagAttack, PropCoef = 1,
                UnitProp = UnitPropIndexes.Intelligence, Coefficient = new double[(int)UnitPropIndexes.Max]{0, 0.88, 0, 0.92},
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP1, ExcellencePorp.EXCELLENCEPORP2},
                BufferProp = new BufferItemTypes[]{BufferItemTypes.TimeAddMAttack}, }
            },
            { ExtPropIndexes.IncreasePhyAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.AddAttackPercent, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP3, ExcellencePorp.EXCELLENCEPORP4, ExcellencePorp.EXCELLENCEPORP24}, }
            },
            { ExtPropIndexes.IncreaseMagAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.AddAttackPercent, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP3, ExcellencePorp.EXCELLENCEPORP4, ExcellencePorp.EXCELLENCEPORP24}, }
            },
            { ExtPropIndexes.MaxLifeV, new ExtPropItem(){ ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.MaxLifePercent, PropCoef = 1,
                UnitProp = UnitPropIndexes.Constitution, Coefficient = new double[(int)UnitPropIndexes.Max]{5, 3.6, 4.2, 4.4},
                BufferProp = new BufferItemTypes[]{BufferItemTypes.MU_ADDMAXHPVALUE}, }
            },            
            { ExtPropIndexes.MaxLifePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP8, ExcellencePorp.EXCELLENCEPORP20}, } 
            },
            { ExtPropIndexes.MaxMagicV, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.MaxMagicPercent, PropCoef = 1,
                BufferProp = new BufferItemTypes[]{BufferItemTypes.MU_ADDMAXMPVALUE}, }
            },
            { ExtPropIndexes.MaxMagicPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.Lucky, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,} },
            { ExtPropIndexes.HitV, new ExtPropItem(){ ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.HitPercent, PropCoef = 1,
                UnitProp = UnitPropIndexes.Dexterity, Coefficient = new double[(int)UnitPropIndexes.Max]{0.5, 0.5, 0.5, 0.5}, } 
            },
            { ExtPropIndexes.Dodge, new ExtPropItem(){ ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.DodgePercent, PropCoef = 1,
                UnitProp = UnitPropIndexes.Dexterity, Coefficient = new double[(int)UnitPropIndexes.Max]{0.5, 0.5, 0.5, 0.5}, } 
            },
            { ExtPropIndexes.LifeRecoverPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                BufferProp = new BufferItemTypes[]{BufferItemTypes.MU_ADDLIFERECOVERPERCENT}, }
            },
            //{ ExtPropIndexes.MagicRecoverPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.LifeRecover, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.MagicRecover, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.SubAttackInjurePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.SubAttackInjure, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.AddAttackInjurePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP5, ExcellencePorp.EXCELLENCEPORP21},}
            },
            { ExtPropIndexes.AddAttackInjure, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.IgnoreDefensePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP14, ExcellencePorp.EXCELLENCEPORP26}, }
            },
            { ExtPropIndexes.DamageThornPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP11, ExcellencePorp.EXCELLENCEPORP28}, }
            },
            { ExtPropIndexes.DamageThorn, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.PhySkillIncreasePercent, new ExtPropItem(){ ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                UnitProp = UnitPropIndexes.Intelligence, Coefficient = new double[(int)UnitPropIndexes.Max]{0.00001, 0, 0.00001, 0.00001}, }
            },            
            // { ExtPropIndexes.PhySkillIncrease, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.MagicSkillIncreasePercent, new ExtPropItem(){ ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                UnitProp = UnitPropIndexes.Strength, Coefficient = new double[(int)UnitPropIndexes.Max]{0, 0.00001, 0, 0.00001}, }
            },  
            // { ExtPropIndexes.MagicSkillIncrease, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max,} },
            { ExtPropIndexes.FatalAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP0, ExcellencePorp.EXCELLENCEPORP18},
                BufferProp = new BufferItemTypes[]{BufferItemTypes.ADDTEMPFATALATTACK}, }
            },
            { ExtPropIndexes.DoubleAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP23},
                BufferProp = new BufferItemTypes[]{BufferItemTypes.ADDTEMPDOUBLEATTACK}, }
            },
            { ExtPropIndexes.DecreaseInjurePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP10}, }
            },
            { ExtPropIndexes.DecreaseInjureValue, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.CounteractInjurePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.CounteractInjureValue, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} }, 
            { ExtPropIndexes.IgnoreDefenseRate, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP7}, }
            },
            { ExtPropIndexes.IncreasePhyDefense, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.AddDefensePercent, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP13, ExcellencePorp.EXCELLENCEPORP27}, }
            },
            { ExtPropIndexes.IncreaseMagDefense, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.AddDefensePercent, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP13, ExcellencePorp.EXCELLENCEPORP27}, }
            },
            { ExtPropIndexes.LifeSteal, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.LifeStealPercent, PropCoef = 1,} },
            { ExtPropIndexes.AddAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.AddDefense, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            /* 带参数 */
            { ExtPropIndexes.StateDingShen, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.StateMoveSpeed, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.StateJiTui, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.StateHunMi, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            /* 带参数 end */
            { ExtPropIndexes.DeLucky, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP29}, }
            },
            { ExtPropIndexes.DeFatalAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP30}, }
            },
            { ExtPropIndexes.DeDoubleAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP31}, }
            },
            { ExtPropIndexes.HitPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP6, ExcellencePorp.EXCELLENCEPORP19}, }
            },
            { ExtPropIndexes.DodgePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,
                ExcellentProp = new ExcellencePorp[]{ExcellencePorp.EXCELLENCEPORP12, ExcellencePorp.EXCELLENCEPORP25}, }
            },
            { ExtPropIndexes.FrozenPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.PalsyPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.SpeedDownPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.BlowPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.AutoRevivePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.SavagePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,} },
            { ExtPropIndexes.ColdPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,} },
            { ExtPropIndexes.RuthlessPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,} },
            { ExtPropIndexes.DeSavagePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,} },
            { ExtPropIndexes.DeColdPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,} },
            { ExtPropIndexes.DeRuthlessPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 100,} },
            { ExtPropIndexes.LifeStealPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.Potion, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.FireAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.WaterAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.LightningAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.SoilAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.IceAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.WindAttack, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.FirePenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.WaterPenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.LightningPenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.SoilPenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.IcePenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.WindPenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.DeFirePenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.DeWaterPenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.DeLightningPenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.DeSoilPenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.DeIcePenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.DeWindPenetration, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.Holywater, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.RecoverLifeV, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.RecoverMagicV, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.Fatalhurt, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.AddAttackPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.AddDefensePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.InjurePenetrationPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.ElementInjurePercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.IgnorePhyAttackPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
            { ExtPropIndexes.IgnoreMagyAttackPercent, new ExtPropItem(){ UnitProp = UnitPropIndexes.Max, ExtProp = ExtPropIndexes.Max, ExtPropPercent = ExtPropIndexes.Max, PropCoef = 1,} },
        };

        public static double GetPureExtProp(GameClient client, int extProp)
        {
            double dValue = 0.0;
            ExtPropItem extPropItem = null;
            roleExtPropDic.TryGetValue((ExtPropIndexes)extProp, out extPropItem);
            if (extPropItem == null)
                return 0;

            /* 根据职业和二级属性计算基值 */
            // 取配置文件中角色职业等级对应的基本属性
            int nOcc = Global.CalcOriginalOccupationID(client);
            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
            dValue += roleBasePropItem.arrRoleExtProp[extProp];
            // 加上 一级属性和职业系数相乘
            double addVal = 0.0;
            switch (extPropItem.UnitProp)
            {
                case UnitPropIndexes.Strength: addVal = GetStrength(client); break;
                case UnitPropIndexes.Intelligence: addVal = GetIntelligence(client); break;
                case UnitPropIndexes.Dexterity: addVal = GetDexterity(client); break;
                case UnitPropIndexes.Constitution: addVal = GetConstitution(client); break;
                case UnitPropIndexes.Max: addVal = 0; break;
            }
            if (extPropItem.UnitProp != UnitPropIndexes.Max)
                addVal *= extPropItem.Coefficient[nOcc];
            dValue += addVal;

            // 计算加成
            dValue += client.ClientData.EquipProp.ExtProps[extProp] + client.RoleBuffer.GetExtProp(extProp) +
                                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[extProp] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[extProp];
            dValue += client.AllThingsMultipliedBuffer.GetExtProp(extProp);
            dValue += client.ClientData.PropsCacheManager.GetExtProp(extProp);
            //val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.Strong);
            dValue += client.RoleMultipliedBuffer.GetExtProp(extProp);
            dValue += client.RoleOnceBuffer.GetExtProp(extProp);
            //dValue += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, (ExtPropIndexes)extProp);
            // 依赖其他二级属性值
            if (extPropItem.ExtProp != ExtPropIndexes.Max)
                dValue += GetExtProp(client, (int)extPropItem.ExtProp);
            // 卓越属性
            if (extPropItem.ExcellentProp != null)
            {
                for (int i = 0; i < extPropItem.ExcellentProp.Length; ++i)
                    dValue += client.ClientData.ExcellenceProp[(int)extPropItem.ExcellentProp[i]];
            }
            
            // 药水 buff
            if (extPropItem.BufferProp != null)
            {
                for (int i = 0; i < extPropItem.BufferProp.Length; ++i)
                    dValue += DBRoleBufferManager.ProcessTimeAddProp(client, extPropItem.BufferProp[i]);
            }

            // Luck 特殊处理
            if (extProp == (int)ExtPropIndexes.Lucky)
                dValue += client.ClientData.LuckProp * 0.01;

            return dValue;
        }

        public static double GetExtProp(GameClient client, int extProp)
        {
            double dValue = 0.0;
            ExtPropItem extPropItem = null;
            roleExtPropDic.TryGetValue((ExtPropIndexes)extProp, out extPropItem);
            if (extPropItem == null)
                return 0;

            /* 根据职业和二级属性计算基值 */
            // 取配置文件中角色职业等级对应的基本属性
            int nOcc = Global.CalcOriginalOccupationID(client);
            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
            dValue += roleBasePropItem.arrRoleExtProp[extProp];
            // 加上 一级属性和职业系数相乘
            double addVal = 0.0;
            switch(extPropItem.UnitProp)
            {
                case UnitPropIndexes.Strength : addVal = GetStrength(client); break;
                case UnitPropIndexes.Intelligence : addVal = GetIntelligence(client); break;
                case UnitPropIndexes.Dexterity : addVal = GetDexterity(client); break;
                case UnitPropIndexes.Constitution : addVal = GetConstitution(client); break;
                case UnitPropIndexes.Max : addVal = 0; break;
            }
            if (extPropItem.UnitProp != UnitPropIndexes.Max)
                addVal *= extPropItem.Coefficient[nOcc];
            dValue += addVal;
            
            // 计算加成
            dValue += client.ClientData.EquipProp.ExtProps[extProp] + client.RoleBuffer.GetExtProp(extProp) +
                                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[extProp] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[extProp];
            dValue += client.AllThingsMultipliedBuffer.GetExtProp(extProp);
            dValue += client.ClientData.PropsCacheManager.GetExtProp(extProp);
            //val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.Strong);
            dValue += client.RoleMultipliedBuffer.GetExtProp(extProp);
            dValue += client.RoleOnceBuffer.GetExtProp(extProp);
            //dValue += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, (ExtPropIndexes)extProp);
            // 依赖其他二级属性值
            if (extPropItem.ExtProp != ExtPropIndexes.Max)
                dValue += GetExtProp(client, (int)extPropItem.ExtProp);
            // 卓越属性
            if (extPropItem.ExcellentProp != null)
            {
                for (int i = 0; i < extPropItem.ExcellentProp.Length; ++i)
                    dValue += client.ClientData.ExcellenceProp[(int)extPropItem.ExcellentProp[i]];
            }
            dValue *= extPropItem.PropCoef;
            // 药水 buff
            if (extPropItem.BufferProp != null)
            {
                for (int i = 0; i < extPropItem.BufferProp.Length; ++i)
                    dValue += DBRoleBufferManager.ProcessTimeAddProp(client, extPropItem.BufferProp[i]);
            }

            // Luck 特殊处理
            if(extProp == (int)ExtPropIndexes.Lucky)
                dValue += client.ClientData.LuckProp;

            // 百分比加成
            if (extPropItem.ExtPropPercent != ExtPropIndexes.Max)
            {
                double addPercent = GetExtProp(client, (int)extPropItem.ExtPropPercent);
                dValue *= (1 + addPercent);
            }
            return dValue;
        }

        public static double GetBaseExtProp(GameClient client, ExtPropItem extPropItem)
        {
            double val = 0.0;
            int nOcc = Global.CalcOriginalOccupationID(client);
            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
            //double dValue = roleBasePropItem.arrRoleExtProp[(int)extPropItem];
            return val;
        }
        #region 基础属性值公式

        // 属性改造 注释掉重量 [8/15/2013 LiaoWei]
        //重量
        /*public static double GetWeight(GameClient client)
        {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.Weight] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.Weight);
            double origVal = val;

            val += (client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Weight, origVal) - origVal);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.Weight);

            return client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Weight, val);
        }

        //重量
        public static double GetWeight(Monster monster)
        {
            return 0.0;
        }*/

        // 属性改造 新增属性 begin [8/15/2013 LiaoWei]
        
        /// <summary>
        /// 力量
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetStrength(GameClient client, bool bAddBuff = true)
        {
            return client.propsCacheModule.GetBasePropsValue((int)UnitPropIndexes.Strength, () =>
            {
                double dValue = 0.0;

                dValue = (double)client.ClientData.PropStrength + client.RoleBuffer.GetBaseProp((int)UnitPropIndexes.Strength)
                                + client.ClientData.RoleStarConstellationProp.StarConstellationFirstProps[(int)UnitPropIndexes.Strength]
                                + client.ClientData.PropsCacheManager.GetBaseProp((int)UnitPropIndexes.Strength);

                if (bAddBuff)
                {
                    dValue += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.ADDTEMPStrength);
                }

                return dValue;
            }, bAddBuff);
        }

        /// <summary>
        /// 智力
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetIntelligence(GameClient client, bool bAddBuff = true)
        {
            return client.propsCacheModule.GetBasePropsValue((int)UnitPropIndexes.Intelligence, () =>
            {
            double dValue = 0.0;

            dValue = (double)client.ClientData.PropIntelligence + client.RoleBuffer.GetBaseProp((int)UnitPropIndexes.Intelligence) +
                        + client.ClientData.RoleStarConstellationProp.StarConstellationFirstProps[(int)UnitPropIndexes.Intelligence]
                        + client.ClientData.PropsCacheManager.GetBaseProp((int)UnitPropIndexes.Intelligence);

            if (bAddBuff)
            {
                dValue += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.ADDTEMPIntelligsence);
            }

            return dValue;
            }, bAddBuff);
        }

        /// <summary>
        /// 敏捷
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetDexterity(GameClient client, bool bAddBuff = true)
        {
            return client.propsCacheModule.GetBasePropsValue((int)UnitPropIndexes.Dexterity, () =>
            {
            double dValue = 0.0;

            dValue = (double)client.ClientData.PropDexterity + client.RoleBuffer.GetBaseProp((int)UnitPropIndexes.Dexterity) 
                        + client.ClientData.RoleStarConstellationProp.StarConstellationFirstProps[(int)UnitPropIndexes.Dexterity]
                        + client.ClientData.PropsCacheManager.GetBaseProp((int)UnitPropIndexes.Dexterity);

            if (bAddBuff)
            {
                dValue += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.ADDTEMPDexterity);
            }

            return dValue;
            }, bAddBuff);
        }

        /// <summary>
        /// 体力
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetConstitution(GameClient client, bool bAddBuff = true)
        {
            return client.propsCacheModule.GetBasePropsValue((int)UnitPropIndexes.Constitution, () =>
            {
            double dValue = 0.0;

            dValue = (double)client.ClientData.PropConstitution + client.RoleBuffer.GetBaseProp((int)UnitPropIndexes.Constitution)
                        + client.ClientData.RoleStarConstellationProp.StarConstellationFirstProps[(int)UnitPropIndexes.Constitution]
                        + client.ClientData.PropsCacheManager.GetBaseProp((int)UnitPropIndexes.Constitution);

            if (bAddBuff)
            {
                dValue += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.ADDTEMPConstitution);
            }

            return dValue;
            }, bAddBuff);
        }

        /// <summary>
        /// 魔法技能增幅
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMagicSkillIncrease(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MagicSkillIncreasePercent, () =>
            {
                double dValue = 0.0;
            
                // 区分职业 -- 只有法师和魔剑士(OccupationID = 1、4)有魔法技能增幅
                int nOcc = Global.CalcOriginalOccupationID(client);
                EOccupationType eOcc = (EOccupationType)nOcc;

                //  魔剑士职业全职享有力量给予的魔法技能增幅[4/15/2015 chdeng]
                //if (EOccupationType.EOT_Magician == eOcc || EOccupationType.EOT_MagicSword == eOcc)
                //{
                //    RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];

                //    dValue = GetStrength(client) / 100000.0; //根据力量计算

                //    dValue += roleBasePropItem.MagicSkillIncreasePercent;
                //}
            
                return dValue;
            });
        }

        /// <summary>
        /// 物理技能增幅
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetPhySkillIncrease(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.PhySkillIncreasePercent, () =>
            {
            double dValue = 0.0;
            
            // 区分职业 -- 战士和弓箭手和魔剑士有物理技能增幅
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;

            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];

            //  魔剑士职业全职享有智力给予的物理技能增幅[4/15/2015 chdeng]
            //if (EOccupationType.EOT_Warrior == eOcc || EOccupationType.EOT_Bow == eOcc || EOccupationType.EOT_MagicSword == eOcc)
            //    dValue = GetIntelligence(client) / 100000.0; // 根据智力计算

            dValue += roleBasePropItem.PhySkillIncreasePercent;

            return dValue;
            });
        }

        /// <summary>
        /// 攻击速度-客户端显示用
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetAttackSpeed(GameClient client)
        {
            // 1攻击速度基础值读配置文件 2攻击速度将被一级属性(敏捷)、BUFF、武器影响
            int nOcc = Global.CalcOriginalOccupationID(client);

            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];

            double dValue = roleBasePropItem.AttackSpeed;
            //dValue = GetDexterity(client) / 10.0;

//             dValue += (int)DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.ADDTEMPATTACKSPEED);
//             dValue += (int)client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.AttackSpeed];
//             dValue += (int)client.RoleBuffer.GetExtProp((int)ExtPropIndexes.AttackSpeed);
//             dValue += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP5];    // 卓越属性影响攻击速度
// 
//             dValue = Math.Max(Data.MaxAttackSlotTick, dValue); //增加值上限限制为“一倍基础值”
//             dValue = Data.MaxAttackSlotTick + dValue;
            
            return dValue;
        }

        /// <summary>
        /// 攻击速度-给服务器运算用
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetAttackSpeedServer(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.AttackSpeed, () =>
            {
            // 1攻击速度基础值读配置文件 2攻击速度将被一级属性(敏捷)、BUFF、武器影响

            int nOcc = Global.CalcOriginalOccupationID(client);

            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];

            double dValue = roleBasePropItem.AttackSpeed;
            //dValue = GetDexterity(client) / 10.0;

//             dValue += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.ADDTEMPATTACKSPEED);
//             dValue += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.AttackSpeed];
//             dValue += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.AttackSpeed);
//             dValue += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP5];    // 卓越属性影响攻击速度
// 
//             dValue = Math.Max(Data.MaxAttackSlotTick, dValue); //增加值上限限制为“一倍基础值”
//             dValue = Data.MaxAttackSlotTick * Data.MaxAttackSlotTick / (Data.MaxAttackSlotTick + dValue);

            return dValue;
            });
        }

        /// <summary>
        /// 卓越一击
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetFatalAttack(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.FatalAttack, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.FatalAttack] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.FatalAttack) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.FatalAttack] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.FatalAttack];
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.FatalAttack);
            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP0]; // 卓越属性影响卓越一击
            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP18]; // 卓越属性影响卓越一击
            val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.ADDTEMPFATALATTACK);
            val *= 100;

            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.FatalAttack);//未知单位，待定
            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.FatalAttack);//未知单位，待定，未使用

            //double addrate = 0.0d;

            ////效果：卓越一击伤害加成*（1+X.X）
            //addrate += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.Fatalhurt);

            //val *= (1.0d + addrate);

            return val; 
            });
        }

        /// <summary>
        /// 抵抗卓越一击
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetDeFatalAttack(GameClient client)
        {
            double val = 0.0;

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeFatalAttack);
            val += (client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP30]);
            val *= 100;

            return val; 
        }

        /// <summary>
        /// 卓越一击伤害加成
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetFatalHurt(GameClient client)
        {
            double val = 0.0;

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.Fatalhurt);
            //val *= 100;

            return val;
        }

        public static double GetDeLuckyAttack(GameClient client)
        {
            double val = 0.0;

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeLucky);
            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP29];
            val *= 100;

            return val;
        }

        /// <summary>
        /// 怪的卓越一击概率
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetFatalAttack(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = monster.XMonsterInfo.CritChance * 100;
#else
             double val = monster.MonsterInfo.MonsterFatalAttack*100;
#endif


            return val;
        }

        /// <summary>
        /// 致命一击
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetDoubleAttack(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.DoubleAttack, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DoubleAttack] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.DoubleAttack) +
                                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.DoubleAttack] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.DoubleAttack];
            val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.ADDTEMPDOUBLEATTACK);
            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP23]; // 卓越属性影响双倍一击
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DoubleAttack);
            val *= 100;

            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.DoubleAttack);
            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.DoubleAttack);

            return val;
            });
        }

        public static double GetDeDoubleAttack(GameClient client)
        {
            double val = 0.0;

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeDoubleAttack);
            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP31];
            val *= 100;

            return val;
        }

        /// <summary>
        /// 野蛮一击
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetSavagePercent(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.SavagePercent);
            val *= 100;

            return Math.Max(val, 0);
        }

        public static double GetDeSavagePercent(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeSavagePercent);
            val *= 100;

            return Math.Max(val, 0);
        }

        /// <summary>
        /// 冷血一击
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetColdPercent(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.ColdPercent);
            val *= 100;

            return Math.Max(val, 0);
        }

        public static double GetDeColdPercent(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeColdPercent);
            val *= 100;

            return Math.Max(val, 0);
        }

        /// <summary>
        /// 无情一击
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetRuthlessPercent(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.RuthlessPercent);
            val *= 100;

            return Math.Max(val,0);
        }

        public static double GetDeRuthlessPercent(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeRuthlessPercent);
            val *= 100;

            return Math.Max(val, 0);
        }

        /// <summary>
        /// 怪的双倍一击概率
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetDoubleAttack(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = 0.0f;
#else
              double val = monster.MonsterInfo.MonsterDoubleAttack * 100;
#endif


            return val;
        }

        /// <summary>
        /// 移动速度
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMoveSpeed(GameClient client)
        {
            if (client.RoleBuffer.GetExtProp((int)ExtPropIndexes.StateDingShen) > 0.1)
            {
                return 0.0;
            }

            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MoveSpeed, () =>
            {
            double val = 1.0; // 移动速度的base值
            // 填的就是百分比，不应该除以100
            val = val * (1.0 + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MoveSpeed]/* / 100.0*/) * (1.0 + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MoveSpeed)/* / 100.0*/);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MoveSpeed);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MoveSpeed);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MoveSpeed);

            if (val < 0.0)
            {
                val = 0.0;
            }

            return val;
            });
        }

        /// <summary>
        /// 伤害反弹(百分比)
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetDamageThornPercent(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.DamageThornPercent, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DamageThornPercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.DamageThornPercent) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.DamageThornPercent] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.DamageThornPercent];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.DamageThornPercent);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DamageThornPercent);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.DamageThornPercent);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.DamageThornPercent);

            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP11];  // 卓越属性影响伤害反弹百分比

            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP28];  // 卓越属性影响伤害反弹百分比

            return val;
            });
        }

        /// <summary>
        /// 怪的伤害反弹(百分比)
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetDamageThornPercent(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = 0.0f;
#else
              double val = monster.MonsterInfo.MonsterDamageThornPercent;
#endif


            return val;
        }

        /// <summary>
        /// 伤害反弹(固定值)
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetDamageThorn(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.DamageThorn, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DamageThorn] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.DamageThorn) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.DamageThorn] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.DamageThorn];

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DamageThorn);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.DamageThorn);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.DamageThorn);

            //double addPercent = GetDamageThornPercent(client);
            //val *= Math.Max(0, 1 + addPercent);
            return Global.GMax(0, val);
            });
        }

        /// <summary>
        /// 怪的伤害反弹(固定值)
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetDamageThorn(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = 0.0f;
#else
              double val = monster.MonsterInfo.MonsterDamageThorn;
#endif


            //double addPercent = GetDamageThornPercent(monster);
            //val *= Math.Max(0, 1 + addPercent);
            return Global.GMax(0, val);
        }

        // 属性改造 新增属性 end [8/15/2013 LiaoWei]

        //耐久
        public static double GetStrong(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.Strong, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.Strong] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.Strong) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.Strong] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.Strong];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Strong);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.Strong);
            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Strong);
            return val;
            });
        }

        /// <summary>
        /// 耐久
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetStrong(Monster monster)
        {
            return 0.0;
        }

        /// <summary>
        /// 最小物理防御力
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMinADefenseV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MinDefense, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;

            double val = 0;
            double addPercent = 0;

            // 基础
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                 val = roleBasePropItem.MinDefenseV;
            }

            // a2b 属性成长
            //{

            //    if (EOccupationType.EOT_Warrior == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.88 * 0.6;
            //    }
            //    else if (EOccupationType.EOT_Magician == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.64 * 0.6;
            //    }
            //    else if (EOccupationType.EOT_Bow == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.76 * 0.6;
            //    }
            //    else if (EOccupationType.EOT_MagicSword == eOcc)  //  增加魔剑士最小物理防御力[4/15/2015 chdeng]
            //    {
            //        val += GetDexterity(client) * 0.8 * 0.6;
            //    }
            //}

            // 定值
            {
                val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MinDefense] +
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MinDefense] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MinDefense];

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinDefense);

                //防御符咒

                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MinDefense);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MinDefense);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinDefense);

                val += GetAddDefenseV(client);

                val += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MinDefense);

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP9]; // 卓越属性影响防御力

                val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.MinDefense);
            }

            // 百分比加成
            addPercent = GetIncreasePhyDefense(client);
            val *= Math.Max(0, 1 + addPercent);
            return Global.GMax(0, val);
            });
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetMinADefenseV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = monster.XMonsterInfo.Pd;
#else
              double val = monster.MonsterInfo.Defense;
#endif
            // 防御提升
           

            val *= (1.0 + monster.TempPropsBuffer.GetExtProp((int)ExtPropIndexes.IncreasePhyDefense));

            return val;
        }

        /// <summary>
        /// 最大物理防御力
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMaxADefenseV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MaxDefense, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;


            double val = 0;
            double addPercent = 0;

            // 基础
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                val = roleBasePropItem.MaxDefenseV;
            }

            // a2b属性成长
            //{
            //    if (EOccupationType.EOT_Warrior == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.88;
            //    }
            //    else if (EOccupationType.EOT_Magician == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.64;
            //    }
            //    else if (EOccupationType.EOT_Bow == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.76;
            //    }
            //    else if (EOccupationType.EOT_MagicSword == eOcc) // 增加魔剑士最大物理防御力[4/15/2015 chdeng]
            //    {
            //        val += GetDexterity(client) * 0.8;
            //    }

            //}

            // 定值加成
            {
                val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxDefense] +
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MaxDefense] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MaxDefense];

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxDefense);



                //持续时间加属性
                val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.TimeAddDefense);
                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MaxDefense);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MaxDefense);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxDefense);


                val += GetAddDefenseV(client);


                val += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MaxDefense);

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP9]; // 卓越属性影响防御力

                val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.MaxDefense);
            }

            // 百分比加成
            addPercent = GetIncreasePhyDefense(client);
            val *= Math.Max(0, 1 + addPercent);

            return Global.GMax(0, val);
            });
        }

        /// <summary>
        /// 物理防御加成百分比
        /// </summary>
        public static double GetIncreasePhyDefense(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.IncreasePhyDefense, () =>
            {
            // 物理攻击提升
            double addPercent = 0.0;
            //防御符咒
            addPercent = DBRoleBufferManager.ProcessAddTempDefense(client);
            //防御提升
            addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IncreasePhyDefense);
            addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddDefensePercent);
            addPercent += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.IncreasePhyDefense);
            //addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IncreaseDefensePercent);

            addPercent += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP13] + client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP27];

            return addPercent;
            });
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetMaxADefenseV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = monster.XMonsterInfo.Pd;
#else
              double val = monster.MonsterInfo.Defense;
#endif


            // 防御提升
            val *= (1.0 + monster.TempPropsBuffer.GetExtProp((int)ExtPropIndexes.IncreasePhyDefense));
            return val;
        }

        /// <summary>
        /// 最小魔法防御值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMinMDefenseV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MinMDefense, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;  

            double val = 0;
            double addPercent = 0;
            // 基础
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                val = roleBasePropItem.MinMDefenseV;
            }

            // a2b属性成长
            //{
            //    if (EOccupationType.EOT_Warrior == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.6 * 0.6;
            //    }
            //    else if (EOccupationType.EOT_Magician == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.84 * 0.6;
            //    }
            //    else if (EOccupationType.EOT_Bow == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.72 * 0.6;
            //    }
            //    else if (EOccupationType.EOT_MagicSword == eOcc) //  增加魔剑士最小魔法防御值[4/15/2015 chdeng]
            //    {
            //        val += GetDexterity(client) * 0.76 * 0.6;
            //    }

            //}

            // 定值加成
            {
                val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MinMDefense] +
                 client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MinMDefense] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MinMDefense];

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinMDefense);

        
                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MinMDefense);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MinMDefense);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinMDefense);

                val += GetAddDefenseV(client);

                val += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MinMDefense);

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP9];// 卓越属性影响防御力

                val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.MinMDefense);
            }

            // 百分比加成
            addPercent = GetIncreaseMagDefense(client);
            val *= Math.Max(0, 1 + addPercent);

            return Global.GMax(0, val);
            });
        }

        /// <summary>
        /// 魔法防御加成百分比
        /// </summary>
        public static double GetIncreaseMagDefense(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.IncreaseMagDefense, () =>
            {
            // 物理攻击提升
            double addPercent = 0.0;
            //防御符咒
            addPercent = DBRoleBufferManager.ProcessAddTempDefense(client);
            //防御提升
            addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IncreaseMagDefense);
            addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddDefensePercent);
            addPercent += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.IncreaseMagDefense);
            //addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IncreaseDefensePercent);

            addPercent += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP13] + client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP27];

            return addPercent;
            });
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetMinMDefenseV(Monster monster)
        {

#if ___CC___FUCK___YOU___BB___
            double val = monster.XMonsterInfo.Pd;
#else
           double val = monster.MonsterInfo.MDefense;
#endif


            // 防御提升
            val *= (1.0 + monster.TempPropsBuffer.GetExtProp((int)ExtPropIndexes.IncreaseMagDefense));

            return val;
        }

        /// <summary>
        /// 最大魔法防御值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMaxMDefenseV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MaxMDefense, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;


            double val = 0;
            double addPercent = 0;
            // 基础属性
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                val = roleBasePropItem.MaxMDefenseV;
            }

            // a2b属性成长
            //{
            //    if (EOccupationType.EOT_Warrior == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.6;
            //    }
            //    else if (EOccupationType.EOT_Magician == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.84;
            //    }
            //    else if (EOccupationType.EOT_Bow == eOcc)
            //    {
            //        val += GetDexterity(client) * 0.72;
            //    }
            //    else if (EOccupationType.EOT_MagicSword == eOcc) //  增加魔剑士最大魔法防御值[4/15/2015 chdeng]
            //    {
            //        val += GetDexterity(client) * 0.76;
            //    }
            //}

            // 定值加成
            {
                val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxMDefense] +
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MaxMDefense] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MaxMDefense];

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxMDefense);

                //持续时间加属性
                val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.TimeAddMDefense);
                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MaxMDefense);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MaxMDefense);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxMDefense);

                val += GetAddDefenseV(client);

                val += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MaxMDefense);

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP9]; // 卓越属性影响防御力

                val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.MaxMDefense);
            }

            // 百分比加成
            addPercent = GetIncreaseMagDefense(client);
            val *= Math.Max(0, 1 + addPercent);

            return Global.GMax(0, val);
            });
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetMaxMDefenseV(Monster monster)
        {

#if ___CC___FUCK___YOU___BB___
            double val = monster.XMonsterInfo.Pd;
#else
            double val = monster.MonsterInfo.MDefense;
#endif
            // 防御提升
            val *= (1.0 + monster.TempPropsBuffer.GetExtProp((int)ExtPropIndexes.IncreaseMagDefense));

            return val;
        }

        /// <summary>
        /// 物理攻击力最小值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMinAttackV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MinAttack, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;


            double val = 0;
            double addPercent = 0;
            // 基础属性
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                val = roleBasePropItem.MinAttackV;
            }

            // a2b属性成长
            //{
            //    if (EOccupationType.EOT_Warrior == eOcc)
            //    {
            //        val += GetStrength(client) * 0.76 * 0.6;
            //    }
            //    else if (EOccupationType.EOT_Bow == eOcc)
            //    {
            //        val += GetStrength(client) * 0.8 * 0.6;
            //    }
            //    else if (EOccupationType.EOT_MagicSword == eOcc) //  增加魔剑士物理攻击力最小值[4/15/2015 chdeng]
            //    {
            //        val += GetStrength(client) * 0.84 * 0.6;
            //    }
            //}

            // 定值加成
            {
                val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MinAttack] +
                                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MinAttack] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MinAttack];

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinAttack);

                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MinAttack);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MinAttack);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinAttack);

                val += GetAddAttackV(client);

                val += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MinAttack);

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP1];   // 卓越属性影响最小物理攻击力

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP2];

                val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.MinAttack);
            }

            // 百分比加成
            {
                //buffer中的放大基础属性
                addPercent = GetIncreasePhyAttack(client);

            }

            val *= Math.Max(0, 1 + addPercent);

            return Global.GMax(0, val);
            });
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetMinAttackV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double attackVal = monster.XMonsterInfo.Ad;
#else
            double attackVal = monster.MonsterInfo.MinAttack;
#endif


            // 物理攻击提升
            attackVal *= (1.0 + monster.TempPropsBuffer.GetExtProp((int)ExtPropIndexes.IncreasePhyAttack));

            return attackVal;
        }

        /// <summary>
        /// 物理攻击力最大值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMaxAttackV(GameClient client)
        {
            //LogManager.WriteLog(LogTypes.Error, string.Format("---------------------------GetMaxAttackV={0}", client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxAttack]));

            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MaxAttack, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;    

            double val = 0;
            double addPercent = 0;
            // 基础属性
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                val = roleBasePropItem.MaxAttackV;
            }

            // a2b属性成长
            //{
            //    if (EOccupationType.EOT_Warrior == eOcc)
            //    {
            //        val += GetStrength(client) * 0.76;
            //    }
            //    else if (EOccupationType.EOT_Bow == eOcc)
            //    {
            //        val += GetStrength(client) * 0.8;
            //    }
            //    else if (EOccupationType.EOT_MagicSword == eOcc) //  增加魔剑士物理攻击力最大值[4/15/2015 chdeng]
            //    {
            //        val += GetStrength(client) * 0.84;
            //    }
            //}
            //LogManager.WriteLog(LogTypes.Error, string.Format("--------------aaaaaaa={0}", client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxAttack]));
            // 定值加成
            {
                val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxAttack] +
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MaxAttack] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MaxAttack];

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxAttack);

                //持续时间加属性
                val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.TimeAddAttack);
                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MaxAttack);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MaxAttack);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxAttack);
                val += client.RoleOnceBuffer.GetExtProp((int)ExtPropIndexes.MaxAttack);
                val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.MaxAttack);
                val += DBRoleBufferManager.ProcessTimeAddJunQiProp(client, ExtPropIndexes.MaxAttack);

                val += GetAddAttackV(client);

                val += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MaxAttack);

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP1];   // 卓越属性影响最大物理攻击力

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP2];
            }

            // 百分比加成
            {

                //buffer中的放大基础属性
                addPercent = GetIncreasePhyAttack(client);
            }

            val *= Math.Max(0, 1 + addPercent);

            return Global.GMax(0, val);
            });
        }

        /// <summary>
        /// 物理攻击加成百分比
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetIncreasePhyAttack(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.IncreasePhyAttack, () =>
            {
            //buffer中的放大基础属性
            double addPercent = DBRoleBufferManager.ProcessAddTempAttack(client);

            // 物理攻击提升
            addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IncreasePhyAttack);
            //addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IncreaseAttackPercent);
            addPercent += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.IncreasePhyAttack);
            addPercent += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.IncreasePhyAttack);
            addPercent += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP4];
            addPercent += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP3];
            addPercent += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP24];

            addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddAttackPercent);

            return addPercent;
            });
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetMaxAttackV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            int attackVal = monster.XMonsterInfo.Ad;
#else
             int attackVal = monster.MonsterInfo.MaxAttack;
#endif

            return attackVal;
        }

        /// <summary>
        /// 魔法攻击力最小值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMinMagicAttackV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MinMAttack, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;


            double val = 0;
            double addPercent = 0;
            // 基础属性
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                val = roleBasePropItem.MinMAttackV;
            }

            // a2b属性成长
            //{
            //    if (EOccupationType.EOT_Magician == eOcc)
            //    {
            //        val += GetIntelligence(client) * 0.88 * 0.6;
            //    }
            //    else if (EOccupationType.EOT_MagicSword == eOcc) //  增加魔剑士魔法攻击力最小值[4/15/2015 chdeng]
            //    {
            //        val += GetIntelligence(client) * 0.92 * 0.6;
            //    }
            //}

            // 定值加成
            {
                val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MinMAttack] +
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MinMAttack] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MinMAttack];

                val += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MinMAttack);

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinMAttack);

                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MinMAttack);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MinMAttack);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinMAttack);

                val += GetAddAttackV(client);

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP2];   // 卓越属性影响最小魔法攻击力

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP1];

                val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.MinMAttack);

            }

            // 百分比加成
            {
                addPercent = GetIncreaseMagAttack(client);
            }


            val *= Math.Max(0, 1 + addPercent);
                        
            return Global.GMax(0, val);
            });
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetMinMagicAttackV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double attackVal = monster.XMonsterInfo.Ad;
#else
            double attackVal = monster.MonsterInfo.MinAttack;
#endif

            // 魔法攻击提升
            attackVal *= (1.0 + monster.TempPropsBuffer.GetExtProp((int)ExtPropIndexes.IncreaseMagAttack));

            return attackVal;
        }

        /// <summary>
        /// 魔法攻击力最大值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMaxMagicAttackV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MaxMAttack, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;


            double val = 0;
            double  addPercent = 0;
            // 基础属性
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                val = roleBasePropItem.MaxMAttackV;
            }

            // a2b属性成长
            //{
            //    if (EOccupationType.EOT_Magician == eOcc)
            //    {
            //        val += GetIntelligence(client) * 0.88;
            //    }
            //    else if (EOccupationType.EOT_MagicSword == eOcc) //  增加魔剑士魔法攻击力最大值[4/15/2015 chdeng]
            //    {
            //        val += GetIntelligence(client) * 0.92;
            //    }

            //}

            // 定值加成
            {
                val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxMAttack] +
                                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MaxMAttack] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MaxMAttack];

                val += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MaxMAttack);

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxMAttack);

                val += GetAddAttackV(client);

                //持续时间加属性
                val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.TimeAddMAttack);
                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MaxMAttack);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MaxMAttack);
                val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.MaxMAttack);
                val += DBRoleBufferManager.ProcessTimeAddJunQiProp(client, ExtPropIndexes.MaxMAttack);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxMAttack);
                val += client.RoleOnceBuffer.GetExtProp((int)ExtPropIndexes.MaxMAttack);

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP2];   // 卓越属性影响最大魔法攻击力

                val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP1];
            }

            // 百分比加成
            {
                addPercent = GetIncreaseMagAttack(client);
            }

            val *= Math.Max(0, 1 + addPercent);

            return Global.GMax(0, val);
            });
        }

        /// <summary>
        /// 魔法攻击加成百分比
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetIncreaseMagAttack(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.IncreaseMagAttack, () =>
            {
            //狂攻符咒
            double addPercent = DBRoleBufferManager.ProcessAddTempAttack(client);

            // 魔法攻击提升
            addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IncreaseMagAttack);
            //addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IncreaseAttackPercent);
            addPercent += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.IncreaseMagAttack);
            addPercent += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.IncreaseMagAttack);
            addPercent += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP4];
            addPercent += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP3];
            addPercent += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP24];

            addPercent += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddAttackPercent);

            return addPercent;
            });
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetMaxMagicAttackV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            int attackVal = monster.XMonsterInfo.Ad;
#else
            int attackVal = monster.MonsterInfo.MaxAttack;
#endif

            return attackVal;
        }

        // 属性改造 [8/15/2013 LiaoWei]
        //道术攻击力最小值
        /*public static double GetMinDSAttackV(GameClient client)
        {
            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[client.ClientData.Occupation][client.ClientData.Level];
            double val = roleBasePropItem.MinDSAttackV;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MinDSAttack] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MinDSAttack);
            double origVal = val;

            val += (client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinDSAttack, origVal) - origVal);

            //狂攻符咒
            val *= (1.0 + DBRoleBufferManager.ProcessAddTempAttack(client));
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MinDSAttack);

            val = client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MinDSAttack, val);
            
            return Global.GMax(0, val);
        }

        //怪
        public static double GetMinDSAttackV(Monster monster)
        {
            int attackVal = monster.MinAttack;
            return attackVal;
        }

        //道术攻击力最大值
        public static double GetMaxDSAttackV(GameClient client)
        {
            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[client.ClientData.Occupation][client.ClientData.Level];
            double val = roleBasePropItem.MaxDSAttackV;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxDSAttack] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MaxDSAttack);
            double origVal = val;

            val += (client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxDSAttack, origVal) - origVal);

            //狂攻符咒
            val *= (1.0 + DBRoleBufferManager.ProcessAddTempAttack(client));

            //持续时间加属性
            val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.TimeAddDSAttack);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MaxDSAttack);
            val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.MaxDSAttack);
            val += DBRoleBufferManager.ProcessTimeAddJunQiProp(client, ExtPropIndexes.MaxDSAttack);

            val = client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxDSAttack, val);
            val += client.RoleOnceBuffer.GetExtProp((int)ExtPropIndexes.MaxDSAttack);

            return Global.GMax(0, val);
        }

        //怪
        public static double GetMaxDSAttackV(Monster monster)
        {
            int attackVal = monster.MaxAttack;
            return attackVal;
        }*/

        /// <summary>
        /// 最大生命值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMaxLifeV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MaxLifeV, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);
            EOccupationType eOcc = (EOccupationType)nOcc;

            double val = 0;
            double addPercent = 0;
            //  基础属性
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                val = roleBasePropItem.LifeV;
            }

                // a2b属性成长
                {
                    if (EOccupationType.EOT_Warrior == eOcc)
                    {
                        val += GetConstitution(client) * 5;
                    }
                    else if (EOccupationType.EOT_SD == eOcc)
                    {
                        val += GetConstitution(client) * 3.6;
                    }
                    else if (EOccupationType.EOT_QZ == eOcc)
                    {
                        val += GetConstitution(client) * 4.2;
                    }
                    else if (EOccupationType.EOT_DZ == eOcc) //  增加魔剑士最大生命值[4/15/2015 chdeng]
                    {
                        val += GetConstitution(client) * 4.4;
                    }
                    else if (EOccupationType.EOT_SS == eOcc) //  增加魔剑士最大生命值[4/15/2015 chdeng]
                    {
                        val += GetConstitution(client) * 4.4;
                    }
                    else if (EOccupationType.EOT_GS == eOcc) //  增加魔剑士最大生命值[4/15/2015 chdeng]
                    {
                        val += GetConstitution(client) * 4.4;
                    }
                }

            // 定值加成
            {
                
                val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxLifeV] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MaxLifeV) +
                                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MaxLifeV] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MaxLifeV];

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxLifeV); //2014-12-27 百分比

                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MaxLifeV);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MaxLifeV);
                val += DBRoleBufferManager.ProcessTimeAddJunQiProp(client, ExtPropIndexes.MaxLifeV);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxLifeV);

                val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.MU_ADDMAXHPVALUE);
           }

            // 百分比加成
            {
                addPercent = GetMaxLifePercentV(client);

            }


            val *= Math.Max(0, 1 + addPercent);

            return val + 100000;
            });
        }

        /// <summary>
        /// 最大生命值(加成比例)
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMaxLifePercentV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MaxLifePercent, () =>
            {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxLifePercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MaxLifePercent) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MaxLifePercent] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MaxLifePercent];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxLifePercent);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MaxLifePercent);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MaxLifePercent);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxLifePercent);

            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP8]; //2014-12-27
            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP20];

            //处理生命符咒
            val += DBRoleBufferManager.ProcessUpLifeLimit(client);

            return val;
            });
        }

        /// <summary>
        /// 击中生命恢复
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetLifeStealV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.LifeSteal, () =>
            {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.LifeSteal] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.LifeSteal) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.LifeSteal] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.LifeSteal];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.LifeSteal);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.LifeSteal);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.LifeSteal);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.LifeSteal);
            //string logInfo = string.Format("\n----击中生命恢复，固定值={0}", val);

            double per = GetLifeStealPercentV(client);
            val = val * (1 + per);
            
            //logInfo += string.Format("\n----击中生命恢复，百分比={0}", per);
            //logInfo += string.Format("\n----击中生命恢复，最终={0}", val);
            //LogManager.WriteLog(LogTypes.Error, logInfo);


            return val;
            });
        }

        /// <summary>
        /// 击中生命恢复(加成比例)
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetLifeStealPercentV(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.LifeStealPercent);
           
            return val;
        }

        /// <summary>
        /// 药水效果（百分比）
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetPotionPercentV(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.Potion);

            return val;
        }

        /// <summary>
        /// 添加攻击力
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetAddAttackV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.AddAttack, () =>
            {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.AddAttack] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.AddAttack) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.AddAttack] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.AddAttack];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.AddAttack);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddAttack);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.AddAttack);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.AddAttack);

            return val;
            });
        }

        /// <summary>
        /// 添加防御力
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetAddDefenseV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.AddDefense, () =>
            {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.AddDefense] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.AddDefense) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.AddDefense] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.AddDefense];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.AddDefense);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddDefense);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.AddDefense);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.AddDefense);

            return val;
            });
        }

        /// <summary>
        /// 添加攻击力百分比
        /// </summary>
        public static double GetAddAttackPercent(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddAttackPercent);

            return val;
        }

        /// <summary>
        /// 添加防御力百分比
        /// </summary>
        public static double GetAddDefensePercent(GameClient client)
        {
            double val = 0.0;
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddDefensePercent);

            return val;
        }

        /// <summary>
        /// 最大魔法值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMaxMagicV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MaxMagicV, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
            double val = roleBasePropItem.MagicV;

            /*if (nOcc == 0)
            {
                val += GetConstitution(client) * 6.0;
            }
            else if (nOcc == 1)
            {
                val += GetConstitution(client) * 12.0;
            }
            else if (nOcc == 2)
            {
                val += GetConstitution(client) * 9.0;
            }*/

            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxMagicV] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MaxMagicV) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MaxMagicV] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MaxMagicV];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxMagicV);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MaxMagicV);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MaxMagicV);

            val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.MU_ADDMAXMPVALUE);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxMagicV);

            double addPercent = GetMaxMagicPercent(client);

            return val * Math.Max(0, 1 + addPercent);
           });
        }

        /// <summary>
        /// 幸运值：
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetLuckV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.Lucky, () =>
            {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.Lucky] 
                + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.Lucky)
                + client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.Lucky] 
                + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.Lucky];

            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP17];
            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.Lucky);
            val *= 100;
           
            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Lucky);      
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.Lucky);
            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Lucky);
            val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.ADDTEMPLUCKYATTACK);

            val += client.ClientData.LuckProp;

            return val; // 属性改造 直接返回lucky值 [8/15/2013 LiaoWei]     //val - GetCurseV(client); //外部真正使用到的是幸运减去诅咒的值
            });
        }

        /// <summary>
        /// 怪的幸运值
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetLuckV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = monster.XMonsterInfo.CritChance;
#else
            double val = monster.MonsterInfo.MonsterLucky;
#endif

            return val;
        }

        // 属性改造 [8/15/2013 LiaoWei]
        //诅咒：
        /*public static double GetCurseV(GameClient client)
        {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.Curse] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.Curse);
            double origVal = val;

            val += (client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Curse, origVal) - origVal);

            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.Curse);

            val = client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Curse, val);
            return val;
        }*/

        /// <summary>
        /// 命中值：
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetHitV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.HitV, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            double val = 0;
            double addPercent = 0;
            // 基础属性
            {
                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
                val = roleBasePropItem.HitV;
            }

            // a2b属性成长
            {
                val += GetDexterity(client) * 0.5;
            }

            // 定数值加成
            {
                val += client.RoleBuffer.GetExtProp((int)ExtPropIndexes.HitV); // buff增加的命中值

                val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.HitV] +
                                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.HitV] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.HitV];

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.HitV);

                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.HitV);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.HitV);


                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.HitV);
            }

            // 百分比加成
            {
                addPercent = client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP6] + 
                    client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP19] + 
                    client.RoleBuffer.GetExtProp((int)ExtPropIndexes.HitPercent) +
                    client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.HitPercent); // 新增buff命中百分比 [XSea 2015/5/12]
            }

            // 百分比都放在最后做加法
            val *= (1 + addPercent);

            return val;
            });
        }

        public static double GetHitPercent(GameClient client)
        {
            double addPercent = 0;
            addPercent = client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP6] +
                    client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP19] +
                    client.RoleBuffer.GetExtProp((int)ExtPropIndexes.HitPercent) +
                    client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.HitPercent); // 新增buff命中百分比 [XSea 2015/5/12]
            return addPercent;
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetHitV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = monster.XMonsterInfo.DodgeResis;
#else
             double val = monster.MonsterInfo.HitV;
#endif

            val *= (1.0 + monster.TempPropsBuffer.GetExtProp((int)ExtPropIndexes.HitV));
            return val;
        }

        /// <summary>
        /// 闪避值：
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetDodgeV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.Dodge, () =>
            {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            double val = 0;
            double addPercent = 0;
            // 基础属性 
            {

                RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
               val = roleBasePropItem.Dodge;
            }

            // a2b属性成长
            {
                val += GetDexterity(client) * 0.5;
            }

            // 定值加成
            {
                val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.Dodge] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.Dodge) +
                    client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.Dodge] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.Dodge];

                val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Dodge);

                val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.Dodge);
                val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.Dodge);

                val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.Dodge);
            }

            // 百分比加成
            {
                addPercent = client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP12] +
                    client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP25] +
                    client.RoleBuffer.GetExtProp((int)ExtPropIndexes.DodgePercent) +
                    client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DodgePercent); // 新增buff命中百分比 [XSea 2015/5/12];
            }
    

			// 百分比都放在最后做加法
            val *= (1 + addPercent);

            return val;
            });
        }

        public static double GetDodgePercent(GameClient client)
        {
            double addPercent = 0;
            addPercent = client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP12] +
                    client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP25] +
                    client.RoleBuffer.GetExtProp((int)ExtPropIndexes.DodgePercent) +
                    client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DodgePercent); // 新增buff命中百分比 [XSea 2015/5/12];
            return addPercent;
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetDodgeV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            return monster.XMonsterInfo.DodgeChance;
#else
              return monster.MonsterInfo.Dodge;
#endif

        }

        // 属性改造 [8/15/2013 LiaoWei]
        //魔法闪避值(百分比)：
        /*public static double GetMagicDodgePercentV(GameClient client)
        {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MagicDodgePercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MagicDodgePercent);
            double origVal = val;

            val += (client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MagicDodgePercent, origVal) - origVal);

            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MagicDodgePercent);

            return client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MagicDodgePercent, val);
        }

        //怪
        public static double GetMagicDodgePercentV(Monster monster)
        {
            return 0.0;
        }

        //中毒恢复(百分比)：
        public static double GetPoisoningReoverPercentV(GameClient client)
        {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.PoisoningReoverPercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.PoisoningReoverPercent);
            double origVal = val;

            val += (client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.PoisoningReoverPercent, origVal) - origVal);

            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.PoisoningReoverPercent);

            return client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.PoisoningReoverPercent, val);
        }

        //中毒闪避(百分比):
        public static double GetPoisoningDodgeV(GameClient client)
        {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.PoisoningDodge] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.PoisoningDodge);
            double origVal = val;

            val += (client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.PoisoningDodge, origVal) - origVal);

            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.PoisoningDodge);

            return client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.PoisoningDodge, val);
        }*/

        /// <summary>
        /// 定时生命回复比例：
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetLifeRecoverValPercentV(GameClient client)
        {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
            double val = roleBasePropItem.RecoverLifeV;

            return val;
        }

        /// <summary>
        /// 生命回复增加比例
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetLifeRecoverAddPercentV(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.LifeRecoverPercent, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.LifeRecoverPercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.LifeRecoverPercent) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.LifeRecoverPercent] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.LifeRecoverPercent];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.LifeRecoverPercent);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.LifeRecoverPercent);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.LifeRecoverPercent);

            val += DBRoleBufferManager.ProcessTimeAddProp(client, BufferItemTypes.MU_ADDLIFERECOVERPERCENT);

            return val + client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.LifeRecoverPercent);
            });
            
        }

        /// <summary>
        /// 生命回复增加比例 仅仅自身回复效果 非buff 或技能
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetLifeRecoverAddPercentOnlySandR(GameClient client)
        {
            double addrate = 0.0;
            
            //自动恢复生命效果：RecoverLifeV，百分比 效果：基础恢复生命效果*（1+X.X）
            addrate += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.RecoverLifeV);

            return addrate;
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetLifeRecoverValPercentV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            return 0.0;
#else
             return monster.MonsterInfo.RecoverLifeV;
#endif

        }

        /// <summary>
        /// 魔法回复速度：
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMagicRecoverValPercentV(GameClient client)
        {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
            double val = roleBasePropItem.RecoverMagicV;

            return val;
        }

        /// <summary>
        /// 魔法回复增加比例
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMagicRecoverAddPercentV(GameClient client)
        {
            /*double val = 0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MagicRecoverPercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MagicRecoverPercent);
            double origVal = val;

            val += (client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MagicRecoverPercent, origVal) - origVal);

            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MagicRecoverPercent);

            return client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MagicRecoverPercent, val);*/

            return 0.0d;
        }

        /// <summary>
        /// 魔法回复增加比例 仅仅自身回复效果 非buff 或技能
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMagicRecoverAddPercentOnlySandR(GameClient client)
        {
            double addrate = 0.0;

            //自动回复魔法效果：RecoverMagicV，百分比 效果：基础恢复生命效果*（1+X.X）
            addrate += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.RecoverMagicV);

            return addrate;
        }

        /// <summary>
        /// 怪
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetMagicRecoverValPercentV(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            return 0.0;
#else
              return monster.MonsterInfo.RecoverMagicV;
#endif

        }

        // 属性改造 [8/15/2013 LiaoWei]
        // 伤害吸收魔法/物理(百分比)
        public static double GetSubAttackInjurePercent(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.SubAttackInjurePercent, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.SubAttackInjurePercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.SubAttackInjurePercent) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.SubAttackInjurePercent] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.SubAttackInjurePercent];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.SubAttackInjurePercent);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.SubAttackInjurePercent);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.SubAttackInjurePercent);
            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.SubAttackInjurePercent);

            val = Math.Max(0, val);

            return val;
            });
        }

        /// <summary>
        /// 伤害穿透
        /// </summary>
        public static double GetInjurePenetrationPercent(GameClient client)
        {
            double val = client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.InjurePenetrationPercent);//伤害穿透
            return Math.Max(0, val); ;
        }

        /// <summary>
        /// 怪的伤害吸收魔法/物理(百分比)
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetSubAttackInjurePercent(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = 0.0;
#else
             double val = monster.MonsterInfo.MonsterSubAttackInjurePercent;
#endif

            return val;
        }

        /// <summary>
        /// 伤害吸收魔法/物理(固定值)
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetSubAttackInjureValue(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.SubAttackInjure, () =>
            {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.SubAttackInjure] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.SubAttackInjure) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.SubAttackInjure] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.SubAttackInjure];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.SubAttackInjure);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.SubAttackInjure);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.SubAttackInjure);

            return val + client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.SubAttackInjure);
            });
        }

        /// <summary>
        /// 怪的伤害吸收魔法/物理(固定值)
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetSubAttackInjureValue(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = 0.0;
#else
            double val = monster.MonsterInfo.MonsterSubAttackInjure;
#endif

            return val;
        }

        // 属性改造 [8/15/2013 LiaoWei]
        /*//吸收魔法伤害(百分比)
        public static double GetSubMAttackInjurePercent(GameClient client)
        {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.SubMAttackInjurePercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.SubMAttackInjurePercent);
            double origVal = val;

            val += (client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.SubMAttackInjurePercent, origVal) - origVal);

            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.SubMAttackInjurePercent);

            return client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.SubMAttackInjurePercent, val);
        }

        //吸收魔法伤害(百分比)
        public static double GetSubMAttackInjurePercent(Monster monster)
        {
            double val = 0.0;
            return val;
        }*/

        /*/// <summary>
        /// 腕力
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetHandFuZhong(GameClient client)
        {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
            double val = roleBasePropItem.HandFuZhong;

            return val;
        }*/

        /*/// <summary>
        /// 背包负重
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetBagFuZhong(GameClient client)
        {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
            double val = roleBasePropItem.BagFuZhong;

            return val;
        }*/

        /*/// <summary>
        /// 穿戴负重
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetDressFuZhong(GameClient client)
        {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            RoleBasePropItem roleBasePropItem = Data.RoleBasePropList[nOcc][client.ClientData.Level];
            double val = roleBasePropItem.DressFuZhong;

            return val;
        }*/

        /// <summary>
        /// 魔法上限增加(百分比)
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetMaxMagicPercent(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.MaxMagicPercent, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxMagicPercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.MaxMagicPercent) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.MaxMagicPercent] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.MaxMagicPercent];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxMagicPercent);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.MaxMagicPercent);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.MaxMagicPercent);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.MaxMagicPercent);

            return val;
            });
        }

        /// <summary>
        /// 无视攻击对象的物理防御(概率)
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetIgnoreDefensePercent(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.IgnoreDefensePercent, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.IgnoreDefensePercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.IgnoreDefensePercent) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.IgnoreDefensePercent] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.IgnoreDefensePercent];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.IgnoreDefensePercent);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IgnoreDefensePercent);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.IgnoreDefensePercent);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.IgnoreDefensePercent);

            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP14];

            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP26];

            return val;
            });
        }

        /// <summary>
        /// 怪物无视攻击对象的物理防御(概率)
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetIgnoreDefensePercent(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = 0.0;
#else
            double val = monster.MonsterInfo.MonsterIgnoreDefensePercent;
#endif
            return val;
        }

        // 属性改造 [8/15/2013 LiaoWei]
        /*//无视攻击对象的魔法防御(概率)
        public static double GetIgnoreMDefensePercent(GameClient client)
        {
            double val = 0.0;
            val = val + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.IgnoreMDefensePercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.IgnoreMDefensePercent);
            double origVal = val;

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.IgnoreMDefensePercent);

            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.IgnoreMDefensePercent);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.IgnoreMDefensePercent);
            return val;
        }*/

        /// <summary>
        /// 伤害减少百分比(物理、魔法) [1/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetDecreaseInjurePercent(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.DecreaseInjurePercent, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DecreaseInjurePercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.DecreaseInjurePercent) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.DecreaseInjurePercent] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.DecreaseInjurePercent];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.DecreaseInjurePercent);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DecreaseInjurePercent);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.DecreaseInjurePercent);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.DecreaseInjurePercent);

            // 卓越属性 减小伤害
            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP10];

            return val;
            });
        }

        /// <summary>
        /// 怪物伤害减少百分比(物理、魔法) [1/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetDecreaseInjurePercent(Monster monster)
        {
            double val = 0.0;
            return val;
        }

        /// <summary>
        /// 伤害减少数值(物理、魔法) [1/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetDecreaseInjureValue(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.DecreaseInjureValue, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DecreaseInjureValue] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.DecreaseInjureValue) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.DecreaseInjureValue] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.DecreaseInjureValue];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.DecreaseInjureValue);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DecreaseInjureValue);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.DecreaseInjureValue);

            return val + client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.DecreaseInjureValue);
            });
        }

        /// <summary>
        /// 怪物伤害减少数值(物理、魔法) [1/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetDecreaseInjureValue(Monster monster)
        {
            double val = 0.0;
            return val;
        }

        /// <summary>
        /// 伤害抵挡百分比(物理、魔法) [1/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetCounteractInjurePercent(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.CounteractInjurePercent, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.CounteractInjurePercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.CounteractInjurePercent) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.CounteractInjurePercent] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.CounteractInjurePercent];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.CounteractInjurePercent);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.CounteractInjurePercent);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.CounteractInjurePercent);

            return val + client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.CounteractInjurePercent);
            });
        }

        /// <summary>
        /// 怪物伤害抵挡百分比(物理、魔法) [1/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetCounteractInjurePercent(Monster monster)
        {
            double val = 0.0;
            return val;
        }

        /// <summary>
        /// 伤害抵挡数值(物理、魔法) [1/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetCounteractInjureValue(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.CounteractInjureValue, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.CounteractInjureValue] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.CounteractInjureValue) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.CounteractInjureValue] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.CounteractInjureValue];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.CounteractInjureValue);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.CounteractInjureValue);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.CounteractInjureValue);

            return val + client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.CounteractInjureValue);
            });
        }

        /// <summary>
        /// 怪物伤害抵挡数值(物理、魔法) [1/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetCounteractInjureValue(Monster monster)
        {
            double val = 0.0;
            return val;
        }

        /// <summary>
        // 伤害加成魔法/物理(百分比) [3/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetAddAttackInjurePercent(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.AddAttackInjurePercent, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.AddAttackInjurePercent] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.AddAttackInjurePercent) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.AddAttackInjurePercent] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.AddAttackInjurePercent];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.AddAttackInjurePercent);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddAttackInjurePercent);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.AddAttackInjurePercent);

            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP5];

            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP21];

            val += DBRoleBufferManager.ProcessTimeAddPkKingAttackProp(client, ExtPropIndexes.AddAttackInjurePercent);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.AddAttackInjurePercent);

            //LogManager.WriteLog(LogTypes.Error, string.Format("\n----伤害加成，百分比={0}", val));

            return val;
            });
        }

        /// <summary>
        /// 伤害加成魔法/物理(百分比) [3/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetAddAttackInjurePercent(Monster monster)
        {
            double val = 0.0;
            return val;
        }

        /// <summary>
        // 伤害加成魔法/物理(固定值) [3/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetAddAttackInjureValue(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.AddAttackInjure, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.AddAttackInjure] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.AddAttackInjure) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.AddAttackInjure] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.AddAttackInjure];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.AddAttackInjure);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AddAttackInjure);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.AddAttackInjure);

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.AddAttackInjure);

            //LogManager.WriteLog(LogTypes.Error, string.Format("\n----伤害加成，固定值={0}", val));

            return val;
            });
        }

        /// <summary>
        /// 伤害加成魔法/物理(固定值) [3/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetAddAttackInjureValue(Monster monster)
        {
            double val = 0.0;
            return val;
        }

        /// <summary>
        // 无视防御的比例 [3/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetIgnoreDefenseRate(GameClient client)
        {
            return client.propsCacheModule.GetExtPropsValue((int)ExtPropIndexes.IgnoreDefenseRate, () =>
            {
            double val = 0.0;
            val += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.IgnoreDefenseRate] + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.IgnoreDefenseRate) +
                            client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[(int)ExtPropIndexes.IgnoreDefenseRate] + client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[(int)ExtPropIndexes.IgnoreDefenseRate];

            val += client.AllThingsMultipliedBuffer.GetExtProp((int)ExtPropIndexes.IgnoreDefenseRate);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IgnoreDefenseRate);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.IgnoreDefenseRate);

            val += client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP7];

            val += client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.IgnoreDefenseRate);

            //LogManager.WriteLog(LogTypes.Error, string.Format("\n----无视防御的比例={0}", val));
            return val;
            });
        }

        /// <summary>
        /// 怪无视伤害的比例 [3/6/2014 LiaoWei]
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static double GetIgnoreDefenseRate(Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            double val = 0.0;
#else
             double val = monster.MonsterInfo.MonsterIgnoreDefenseRate;
#endif

            return val;
        }

        #region 获取冰冻几率
        /// <summary>
        /// 获取冰冻几率 [XSea 2015/6/25]
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        public static double GetFrozenPercent(IObject obj)
        {
            double dVal = 0;

            // 是人
            if (obj is GameClient)
            {
                GameClient client = obj as GameClient;
                if (null != client)
                {
                    dVal += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.FrozenPercent];
                    dVal += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.FrozenPercent);
                }
            }
            else if (obj is Robot) // 是机器人
            {
                Robot robot = obj as Robot;
                if (null != robot)
                {
                    dVal = robot.FrozenPercent;
                    //上个版本个版本这个值的是百分比,所以现在要除以100,新项目可以去掉此判断
                    if (dVal > 1)
                    {
                        dVal /= 100;
                    }
                }
            }
            else if (obj is Monster) // 是怪
            {
                Monster monster = obj as Monster;
                if (null != monster)
                {

                }
            }
            return Math.Max(dVal, 0);
        }
        #endregion

        #region 获取麻痹几率
        /// <summary>
        /// 获取麻痹几率 [XSea 2015/6/25]
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        public static double GetPalsyPercent(IObject obj)
        {
            double dVal = 0;

            // 是人
            if (obj is GameClient)
            {
                GameClient client = obj as GameClient;
                if (null != client)
                {
                    dVal += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.PalsyPercent];
                    dVal += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.PalsyPercent);
                }
            }
            else if (obj is Robot) // 是机器人
            {
                Robot robot = obj as Robot;
                if (null != robot)
                {
                    dVal = robot.PalsyPercent;
                    //上个版本个版本这个值的是百分比,所以现在要除以100,新项目可以去掉此判断
                    if (dVal > 1)
                    {
                        dVal /= 100;
                    }
                }
            }
            else if (obj is Monster) // 是怪
            {
                Monster monster = obj as Monster;
                if (null != monster)
                {

                }
            }
            return Math.Max(dVal, 0);
        }
        #endregion

        #region 获取减速几率
        /// <summary>
        /// 获取减速几率 [XSea 2015/6/25]
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        public static double GetSpeedDownPercent(IObject obj)
        {
            double dVal = 0;

            // 是人
            if (obj is GameClient)
            {
                GameClient client = obj as GameClient;
                if (null != client)
                {
                    dVal += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.SpeedDownPercent];
                    dVal += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.SpeedDownPercent);
                }
            }
            else if (obj is Robot) // 是机器人
            {
                Robot robot = obj as Robot;
                if (null != robot)
                {
                    dVal = robot.SpeedDownPercent;
                    //上个版本个版本这个值的是百分比,所以现在要除以100,新项目可以去掉此判断
                    if (dVal > 1)
                    {
                        dVal /= 100;
                    }
                }
            }
            else if (obj is Monster) // 是怪
            {
                Monster monster = obj as Monster;
                if (null != monster)
                {

                }
            }
            return Math.Max(dVal, 0);
        }
        #endregion

        #region 获取重击几率
        /// <summary>
        /// 获取重击几率 [XSea 2015/6/25]
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        public static double GetBlowPercent(IObject obj)
        {
            double dVal = 0;

            // 是人
            if (obj is GameClient)
            {
                GameClient client = obj as GameClient;
                if (null != client)
                {
                    dVal += client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.BlowPercent];
                    dVal += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.BlowPercent);
                }
            }
            else if (obj is Robot) // 是机器人
            {
                Robot robot = obj as Robot;
                if (null != robot)
                {
                    dVal = robot.BlowPercent;
                    //上个版本个版本这个值的是百分比,所以现在要除以100,新项目可以去掉此判断
                    if (dVal > 1)
                    {
                        dVal /= 100;
                    }
                }
            }
            else if (obj is Monster) // 是怪
            {
                Monster monster = obj as Monster;
                if (null != monster)
                {

                }
            }
            return Math.Max(dVal, 0);
        }
        #endregion

        #region 获取自动重生几率
        /// <summary>
        /// 获取自动重生几率 [XSea 2015/6/26]
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        public static double GetAutoRevivePercent(object obj)
        {
            double dVal = 0;

            // 是人
            if (obj is GameClient)
            {
                GameClient client = obj as GameClient;
                if (null != client)
                {
                    dVal += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.AutoRevivePercent);
                }
            }
            else if (obj is Robot) // 是机器人
            {
                Robot robot = obj as Robot;
                if (null != robot)
                {

                }
            }
            else if (obj is Monster) // 是怪
            {
                Monster monster = obj as Monster;
                if (null != monster)
                {

                }
            }
            return dVal;
        }
        #endregion

        #region 通用属性获取接口

        public static double GetExtPropValue(GameClient client, ExtPropIndexes extPropIndex)
        {
            double val = 0;
            int index = (int)extPropIndex;
            val += client.ClientData.EquipProp.ExtProps[index];
            val += client.ClientData.PropsCacheManager.GetExtProp(index);
            return val;
        }

        #endregion 通用属性获取接口

        #endregion 基础属性值公式

        #region 攻击伤害计算公式

        /// <summary>
        /// 获取攻击力的一个公式
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="val"></param>
        /// <param name="luck"></param>
        /// <param name="nFatalValue"></param>
        /// <param name="nDoubleValue"></param>
        /// <returns></returns>
        private static double GetAttackPower(IObject obj, int damage, int val, int luck, int nFatalValue, out int nDamageType, int nMaxAttackValue)
        {
            int result = 0;
            GameClient client = null;
            nDamageType = (int)DamageType.DAMAGETYPE_DEFAULT;   // 默认伤害类型

            if (val < 0) val = 0;

            int r = Global.GetRandomNumber(1, 101);
            //string logInfo = string.Format(" \n----------攻击力计算1，随机数={0}, 卓越={1}, 幸运={2} \n", r, nFatalValue, luck);

            // 1.卓越一击
            if (r <= nFatalValue)
            {           
                result = nMaxAttackValue;
                double dValue = 1.2;

                client = obj as GameClient;
                if (client != null)
                {
                    dValue +=  0.2 * GetFatalHurt(client);
                    dValue += DBRoleBufferManager.ProcessSpecialAttackValueBuff(client, (int)BufferItemTypes.MU_ADDFATALATTACKPERCENTTIMER);
                    client.CheckCheatData.LastDamageType = Global.SetIntSomeBit((int)DamageType.DAMAGETYPE_EXCELLENCEATTACK, client.CheckCheatData.LastDamageType, true);
                }

                result = (int)(result * dValue);
                nDamageType = (int)DamageType.DAMAGETYPE_EXCELLENCEATTACK;   // 卓越一击

                //logInfo += string.Format("----------攻击力计算2，卓越一击 buff={0}, 攻击={1} \n", dValue, result);
            }
            else if (r <= luck)
            {
                result = damage + val;

                double dValue = 0.0;
                client = obj as GameClient;
                if (client != null)
				{
                    dValue = DBRoleBufferManager.ProcessSpecialAttackValueBuff(client, (int)BufferItemTypes.MU_ADDLUCKYATTACKPERCENTTIMER);
					client.CheckCheatData.LastDamageType = Global.SetIntSomeBit((int)DamageType.DAMAGETYPE_LUCKYATTACK, client.CheckCheatData.LastDamageType, true);
				}
                result = result + (int)(result * dValue);
                nDamageType = (int)DamageType.DAMAGETYPE_LUCKYATTACK;   // 幸运一击
                //logInfo += string.Format("----------攻击力计算3，幸运一击 buff={0}, 攻击={1} \n", dValue, result);
            }
            else
			{
                result = damage + Global.GetRandomNumber(0, val + 1);
                if (obj is GameClient)
                {
                    client = obj as GameClient;
                    client.CheckCheatData.LastDamageType = Global.SetIntSomeBit((int)DamageType.DAMAGETYPE_DEFAULT, client.CheckCheatData.LastDamageType, true);
                }
            }

            //logInfo += string.Format("----------攻击力计算4，最小攻击={0}, 最大攻击={1}, 攻击差值={2}, 攻击={3} ", damage, nMaxAttackValue, val,result);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            return result;
        }

        /// <summary>
        /// 计算伤害（物理，魔法）
        /// （无情,冷血,野蛮，致命）
        /// </summary>
        /// <param name="obj">攻击方</param>
        /// <param name="objTarget">被攻击方</param>
        /// <param name="attack">攻击力</param>
        /// <returns></returns>
        public static double CalcInjureValue(IObject obj, IObject objTarget, double damage,ref int damageType, double elementInjurePercnet)
        {
            double result = damage;

            //无情,冷血,野蛮
            double ruthlessValue = 0;
            double coldValue = 0;
            double savageValue = 0;
            double doubleValue = 0;

            //string logInfo = string.Format(" \n----------伤害计算1，伤害={0} \n", damage);        

            if (obj is Robot)
            {
                Robot robot = obj as Robot;

                ruthlessValue = robot.RuthlessValue;
                ruthlessValue -= (int)RoleAlgorithm.GetDeRuthlessPercent(objTarget as GameClient);

                coldValue = robot.ColdValue;
                coldValue -= (int)RoleAlgorithm.GetDeColdPercent(objTarget as GameClient);

                savageValue = robot.SavageValue;
                savageValue -= (int)RoleAlgorithm.GetDeSavagePercent(objTarget as GameClient);

                doubleValue = robot.DoubleValue;
                doubleValue -= (int)RoleAlgorithm.GetDeDoubleAttack(objTarget as GameClient);

                //logInfo += string.Format("----------伤害计算2，【机器人】 无情={0}, 冷血={1}, 野蛮={2}, 致命={3} \n", ruthlessValue, coldValue, savageValue, doubleValue);      
            }
            else if (obj is GameClient)
            {
                ruthlessValue = (int)RoleAlgorithm.GetRuthlessPercent(obj as GameClient);
                coldValue = (int)RoleAlgorithm.GetColdPercent(obj as GameClient);
                savageValue = (int)RoleAlgorithm.GetSavagePercent(obj as GameClient);
                doubleValue = (int)RoleAlgorithm.GetDoubleAttack(obj as GameClient);

                //logInfo += string.Format("----------伤害计算3，【人】 无情={0}, 冷血={1}, 野蛮={2}, 致命={3} \n", ruthlessValue, coldValue, savageValue, doubleValue);      

                if (objTarget is GameClient)
                {
                    ruthlessValue -= (int)RoleAlgorithm.GetDeRuthlessPercent(objTarget as GameClient);
                    coldValue -= (int)RoleAlgorithm.GetDeColdPercent(objTarget as GameClient);
                    savageValue -= (int)RoleAlgorithm.GetDeSavagePercent(objTarget as GameClient);
                    doubleValue -= (int)RoleAlgorithm.GetDeDoubleAttack(objTarget as GameClient);

                    //logInfo += string.Format("----------伤害计算4，【打人】 无情={0}, 冷血={1}, 野蛮={2}, 致命={3} \n", ruthlessValue, coldValue, savageValue, doubleValue);      
                }
                else if (objTarget is Robot)
                {
                    Robot robot = objTarget as Robot;
                    ruthlessValue -= robot.DeRuthlessValue;
                    coldValue -= robot.DeColdValue;
                    savageValue -= robot.DeSavageValue;
                    doubleValue -= robot.DeDoubleValue;

                    //logInfo += string.Format("----------伤害计算5，【打机器】 无情={0}, 冷血={1}, 野蛮={2}, 致命={3} \n", ruthlessValue, coldValue, savageValue, doubleValue);    
                }

            }

            //LogManager.WriteLog(LogTypes.Error, logInfo);

            //int r = Global.GetRandomNumber(1, 101);
            //logInfo += string.Format("----------伤害计算6，几率={0} \n", r);

            double[] rateArr = { ruthlessValue, coldValue, savageValue, doubleValue };
            int index = GetRateIndex(rateArr, 100);
            //logInfo = "";
            switch (index)
            {
                case 0:
                    {//无情 本次伤害= 伤害 *1.5，并恢复（本次伤害*10%）的生命
                        result = damage * 1.5;
                        //
                        double[] param = { (int)(result * 0.1), 0 }; // 参数列表
                        MagicAction.ProcessAction(obj, objTarget, MagicActionIDs.MU_ADD_HP, param);
                        //logInfo += string.Format("----------无情一击，恢复血量={0} \n", param[0]);   
                        damageType = (int)DamageType.DAMAGETYPE_RUTHLESS;

                        if (obj is GameClient)
                        {
                            (obj as GameClient).CheckCheatData.LastDamageType = Global.SetIntSomeBit(damageType, (obj as GameClient).CheckCheatData.LastDamageType, true);
                        }
                    }
                    break;
                case 1:
                    {//冷血 本次伤害=伤害*2，目标受到减速50%的效果，持续4秒
                        result = damage * 2;
                        //
                        double[] param = { 0.5, 4.0 }; // 参数列表
                        MagicAction.ProcessAction(obj, objTarget, MagicActionIDs.MU_ADD_MOVE_SPEED_DOWN, param);
                        //logInfo += string.Format("----------冷血一击，减速={0}_{0} \n", param[0],param[1]);   
                        damageType = (int)DamageType.DAMAGETYPE_COLD;

                        if (obj is GameClient)
                        {
                            (obj as GameClient).CheckCheatData.LastDamageType = Global.SetIntSomeBit(damageType, (obj as GameClient).CheckCheatData.LastDamageType, true);
                        }
                    }
                    break;
                case 2:
                    {//野蛮 本次伤害=伤害*3
                        result = damage * 3;
                        //logInfo += string.Format("----------野蛮一击，伤害={0} \n", result);   
                        damageType = (int)DamageType.DAMAGETYPE_SAVAGE;

                        if (obj is GameClient)
                        {
                            (obj as GameClient).CheckCheatData.LastDamageType = Global.SetIntSomeBit(damageType, (obj as GameClient).CheckCheatData.LastDamageType, true);
                        }
                    }
                    break;
                case 3:
                    {//双倍一击 伤害*2
                        result = damage * 2;

                        double dValue = 0.0;
                        if (obj as GameClient != null)
                            dValue = DBRoleBufferManager.ProcessSpecialAttackValueBuff(obj as GameClient, (int)BufferItemTypes.MU_ADDDOUBLEATTACKPERCENTTIMER);

                        result = result + (int)(result * dValue);
                        //logInfo += string.Format("----------双倍一击，伤害={0} \n", result);   

                        damageType = (int)DamageType.DAMAGETYPE_DOUBLEATTACK;
                        if (obj is GameClient)
                        {
                            (obj as GameClient).CheckCheatData.LastDamageType = Global.SetIntSomeBit(damageType, (obj as GameClient).CheckCheatData.LastDamageType, true);
                        }
                    }
                    break;
            }

            // 元素伤害统计上多段伤害的百分比
            // 累加上元素伤害 [XSea 2015/8/14]
            result += GameManager.ElementsAttackMgr.CalcAllElementDamage(obj, objTarget) * elementInjurePercnet;

            // 情侣竞技，buff伤害加成
            result = result * (1 + CoupleArenaManager.Instance().CalcBuffHurt(obj, objTarget));

            //logInfo += string.Format("----------伤害计算7，伤害={0} \n", result);   
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            return result;
        }

        /// <summary>
        /// 几率计算
        /// </summary>
        /// <param name="rateArr"></param>
        /// <returns></returns>
        public static int GetRateIndexPercent(double[] rateArr)
        {
            int index = -1;
            if (rateArr == null || rateArr.Length <= 0)
                return index;

            double result = 0;
            double r = Global.GetRandom();

            //string logInfo = string.Format("----------伤害几率计算，几率={0} \n", r);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            //string logInfo = string.Format("----------伤害几率计算，几率={0} 无情={1} 冷血={2} 野蛮={3} 致命={4}\n", r, rateArr[0], rateArr[1], rateArr[2], rateArr[3]);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            for (int i = 0; i < rateArr.Length; i++)
            {
                result += rateArr[i];
                if (result > r)
                    return i;
            }

            return index;
        }

        /// <summary>
        /// 几率计算
        /// </summary>
        /// <param name="rateArr"></param>
        /// <returns></returns>
        public static int GetRateIndex(double[] rateArr,int max)
        {
            int index = -1;
            if (rateArr == null || rateArr.Length <= 0)
                return index;

            double result = 0;
            double r = Global.GetRandomNumber(0, max);

            //string logInfo = string.Format("----------伤害几率计算，几率={0} \n", r);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            //string logInfo = string.Format("----------伤害几率计算，几率={0} 无情={1} 冷血={2} 野蛮={3} 致命={4}\n", r, rateArr[0], rateArr[1], rateArr[2], rateArr[3]);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            for (int i = 0; i < rateArr.Length; i++)
            {
                result += rateArr[i];
                if (result > r)
                    return i;
            }

            return index;
        }

        /// <summary>
        /// 获取防御力一个公式
        /// </summary>
        /// <param name="baseDefense"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        private static double GetDefensePower(int baseDefense, int val)
        {
            if (val < 0) val = 0;
            return baseDefense + Global.GetRandomNumber(0, val + 1);
        }

        /// <summary>
        /// 获取防御力的一个公式
        /// </summary>
        /// <param name="minDefense"></param>
        /// <param name="maxDefense"></param>
        /// <returns></returns>
        private static double GetDefenseValue(int minDefense, int maxDefense)
        {
            return GetDefensePower(minDefense, maxDefense - minDefense);
        }

        /// <summary>
        /// 计算物理攻击力
        /// </summary>
        /// <param name="minAttackV"></param>
        /// <param name="maxAttackV"></param>
        /// <param name="lucky"></param>
        /// <param name="nFatalValue"></param>
        /// <param name="nDoubleValue"></param>
        /// <returns></returns>
        public static double CalcAttackValue(IObject obj, int minAttackV, int maxAttackV, int lucky, int nFatalValue, out int nDamageType)
        {
            nDamageType = 0;

            return GetAttackPower(obj, minAttackV, maxAttackV - minAttackV, lucky, nFatalValue, out nDamageType, maxAttackV);
        }

        /// <summary>
        /// 真正的伤害
        /// </summary>
        /// <param name="attackV"></param>
        /// <param name="defenseV"></param>
        /// <returns></returns>
        public static double GetRealInjuredValue(long attackV, long defenseV)
        {
            double newJnjureV = 0.0;
            
            // MU修正伤害计算 [1/22/2014 LiaoWei]
            //defenseV = defenseV / 2;

            //newJnjureV = Math.Max(attackV - defenseV, 0.0);

            /*double percent = Math.Max((1.0 -((double)defenseV / ((double)defenseV + 1000.0))), 0.05);
            newJnjureV = (int)Math.Max(attackV * percent, 0.0);
            newJnjureV = Math.Max(newJnjureV, 1);*/

            // MU 再次修改伤害公式 伤害=自己攻击^2/（自身攻击+对方防御） [2/11/2014 LiaoWei]
            //newJnjureV = (int)Math.Max((attackV * attackV) / (attackV + defenseV), 1.0);

            // MU 再次修改伤害公式 伤害=MAX((攻击-防御/4),攻击*0.1) [5/8/2014 LiaoWei]
            //newJnjureV = (int)Math.Max((attackV - defenseV / 4), attackV * 0.1);

            // MU 再次修改伤害公式 伤害=MAX((攻击-对方防御/4),MAX(攻击*0.1,5)) [5/8/2014 LiaoWei]
            // MU 再次修改伤害公式 伤害=MAX((攻击-对方防御),MAX(攻击*0.1,5)) [6/19/2014 LiaoWei]
            newJnjureV = (int)Math.Max((attackV - defenseV), (int)Math.Max(attackV * 0.1, 5));

            return newJnjureV;
        }

        /// <summary>
        /// 命中率=攻击方命中值/（攻击方命中值+被攻击方闪避值/4.5）
        /// 命中率最小为75%，当命中率小于75%时按75%计算
        /// Rand<=命中率*100 为命中，否则为闪避
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="hitV"></param>
        /// <param name="dodgeV"></param>
        /// <returns></returns>
        public static double GetRealHitRate(double hitV, double dodgeV)
        {
            // 属性改造  命中率=max(攻击方命中值/(攻击方命中值+被攻击方闪避值), 3%)   [8/15/2013 LiaoWei]
            // 再次修改命中 命中率=max(攻击方命中值/(攻击方命中值+被攻击方闪避值/2), 3%) [1/22/2014 LiaoWei]
            // 命中公式调整为：命中率=max(自身命中/(自身命中+对方闪避/4), 3%) [1/23/2014 LiaoWei]
            // 再次再次修改命中公式：命中率=max(自己命中/（自己命中+对方闪避/10）,3%) [3/6/2014 LiaoWei]

            if (dodgeV <= 0.0)
            {
                return 1;
            }

            int rndNum = Global.GetRandomNumber(0, 101);

            //double newHitV = hitV / dodgeV;
            //int value = (int)(newHitV * 100.0);

            int nHit = (int)(hitV / (hitV + dodgeV / 10) * 100.0);

            int value = Global.GMax(nHit, 3);

            return (rndNum <= value) ? 1 : 0;
        }

        /// <summary>
        /// 获取闪避率
        /// </summary>
        /// <param name="DodgePercent"></param>
        /// <returns></returns>
        public static double GetRealHitRate(double DodgePercent)
        {
            if (DodgePercent <= 0.0)
            {
                return 1;
            }

            int rndNum = Global.GetRandomNumber(0, 101);
            int value = (int)(DodgePercent * 100.0);
            return (rndNum <= value) ? 0 : 1; //意思反过来，闪避掉，不闪避
        }

        /// <summary>
        /// 计算伤害吸收
        /// </summary>
        /// <param name="injured"></param>
        /// <returns></returns>
        public static int CalcAttackInjure(int attackType, IObject obj, int injured, GameClient attackClient = null)
        {
            // 属性改造 加上新增加的"伤害吸收魔法/物理(固定值)" [8/15/2013 LiaoWei]
            // 增加 "伤害减少百分比(物理、魔法) 伤害减少数值(物理、魔法) 伤害抵挡百分比(物理、魔法) 伤害抵挡数值(物理、魔法)" [1/6/2014 LiaoWei]
            
            var subPercent      = 0.0;
            var subValue        = 0.0; 
            var DecreasePercent = 0.0;
            var DecreaseValue   = 0.0;
            var CounteractPercent = 0.0;
            var CounteractValue = 0.0;
            var ctPercent = 0.0;
            
            // 1.不区分物理、魔法攻击类型 2.增加伤害吸收魔法/物理(固定值)
            if (obj is GameClient)
            {
                /*if (0 == attackType)
                {
                    subPercent = RoleAlgorithm.GetSubAttackInjurePercent(obj as GameClient);
                }
                else
                {
                    subPercent = RoleAlgorithm.GetSubMAttackInjurePercent(obj as GameClient);
                }*/
                subPercent      = RoleAlgorithm.GetSubAttackInjurePercent(obj as GameClient);
                
                subValue        = RoleAlgorithm.GetSubAttackInjureValue(obj as GameClient);

                DecreasePercent = RoleAlgorithm.GetDecreaseInjurePercent(obj as GameClient);

                DecreaseValue   = RoleAlgorithm.GetDecreaseInjureValue(obj as GameClient);

                CounteractPercent = RoleAlgorithm.GetCounteractInjurePercent(obj as GameClient);

                CounteractValue = RoleAlgorithm.GetCounteractInjureValue(obj as GameClient);
            }
            else if (obj is Monster)
            {
                /*if (0 == attackType)
                {
                     subPercent = RoleAlgorithm.GetSubAttackInjurePercent(obj as Monster);
                }
                else
                {
                    subPercent = RoleAlgorithm.GetSubMAttackInjurePercent(obj as Monster);
                }*/
                subPercent  = RoleAlgorithm.GetSubAttackInjurePercent(obj as Monster);

                subValue    = RoleAlgorithm.GetSubAttackInjureValue(obj as Monster);

                DecreasePercent = RoleAlgorithm.GetDecreaseInjurePercent(obj as Monster);

                DecreaseValue = RoleAlgorithm.GetDecreaseInjureValue(obj as Monster);

                CounteractPercent = RoleAlgorithm.GetCounteractInjurePercent(obj as Monster);

                CounteractValue = RoleAlgorithm.GetCounteractInjureValue(obj as Monster);
            }

            //string logInfo = string.Format(
            //   "\n--------------伤害吸收1，伤害吸收【百分比】={0}，【固定】={1}，伤害减少【百分比】={2}，【固定】={3}，伤害抵挡【百分比】={4}，【固定】={5}，【原始伤害】={6}",
            //   subPercent, subValue, DecreasePercent, DecreaseValue, CounteractPercent, CounteractValue, injured);

            if (attackClient != null)
            {
                ctPercent = RoleAlgorithm.GetInjurePenetrationPercent(attackClient);
                subPercent = Math.Max(0, subPercent - ctPercent);
            }

            //logInfo += string.Format(
            //  "\n--------------伤害吸收2，伤害吸收【百分比】={0}，【穿透】={1}", subPercent, ctPercent);

            //injured = (int)(((((injured * (1.0 - subPercent) - subValue) * (1 - DecreasePercent)) - DecreaseValue) * (1 - CounteractPercent)) - CounteractValue);
            injured -= (int)(subValue + DecreaseValue + CounteractValue); //先算固定值减伤,减伤和附加伤害权重相同
            injured = (int)(injured * (1.0 - subPercent) * (1 - DecreasePercent) * (1 - CounteractPercent));

            //logInfo += string.Format("\n--------------【最终伤害】={0}", injured);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            return injured;
        }

        /// <summary>
        /// 获取忽视对方防御后的防御值
        /// </summary>
        /// <param name="DodgePercent"></param>
        /// <returns></returns>
        public static int GetDefenseByCalcingIgnoreDefense(int attackType, IObject self, int defense, ref int burst)
        {
            // 属性改造 [8/15/2013 LiaoWei]
            var ignorePercent = 0.0;
            /*if (0 == attackType) //物理防御
            {
                ignorePercent = RoleAlgorithm.GetIgnoreDefensePercent(self as GameClient);
            }
            else
            {
                ignorePercent = RoleAlgorithm.GetIgnoreMDefensePercent(self as GameClient);
            }*/

            if (self is GameClient)
                ignorePercent = RoleAlgorithm.GetIgnoreDefensePercent(self as GameClient);
            else if (self is Monster)
                ignorePercent = RoleAlgorithm.GetIgnoreDefensePercent(self as Monster);

            //string  logInfo = string.Format("\n----无视防御概率={0}", ignorePercent);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            if (ignorePercent <= 0.0)
            {
                return defense;
            }

            int rndNum = Global.GetRandomNumber(0, 101);
            int value = (int)(ignorePercent * 100.0);

            //logInfo = string.Format("\n----无视防御概率 几率={0}", rndNum);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            if (rndNum <= value)
            {
                burst = (int)DamageType.DAMAGETYPE_IGNOREDEFENCE;   // 无视防御了
                return 0;
            }
            else
            {
                return defense;
            }
        }

        #endregion //攻击伤害计算公式

        #region 物理免疫和魔法免疫

        private static bool ClientIgnorePhyAttack(GameClient client, ref int burst)
        {
            double ignorePhyAttackPercent = RoleAlgorithm.GetExtPropValue(client, ExtPropIndexes.IgnorePhyAttackPercent);
            if (ignorePhyAttackPercent > 0.0 && ignorePhyAttackPercent >= Global.GetRandom())
            {
                burst = (int)DamageType.IgnorePhyAttack;
                return true;
            }

            return false;
        }

        private static bool ClientIgnoreMagicAttack(GameClient client, ref int burst)
        {
            double ignorePhyAttackPercent = RoleAlgorithm.GetExtPropValue(client, ExtPropIndexes.IgnoreMagyAttackPercent);
            if (ignorePhyAttackPercent > 0.0 && ignorePhyAttackPercent >= Global.GetRandom())
            {
                burst = (int)DamageType.IgnoreMagicAttack;
                return true;
            }

            return false;
        }

        #endregion 物理免疫和魔法免疫

        #region 物理攻击伤害计算

        /// <summary>
        /// 角色攻击怪的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void AttackEnemy(GameClient client, Monster monster, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, out int burst, out int injure, bool ignoreDefenseAndDodge, double baseRate, int addVlue)
        {
            burst = 0;
            injure = 0;

            #region 命中判定

            if (!ignoreDefenseAndDodge)
            {
                //判断是否命中
                double hitV = RoleAlgorithm.GetHitV(client);
                double dodgeV = RoleAlgorithm.GetDodgeV(monster);

                int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
                if (hit <= 0) //表示对方闪避掉了自己的攻击
                {
                    injure = 0;
                    return;
                }
            }

            #endregion 命中判定

            #region 伤害计算

            int minAttackV = (int)RoleAlgorithm.GetMinAttackV(client);
            int maxAttackV = (int)RoleAlgorithm.GetMaxAttackV(client);
            int lucky = (int)RoleAlgorithm.GetLuckV(client);
            int nFatalValue = (int)RoleAlgorithm.GetFatalAttack(client);    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

            //string logInfo = string.Format("\n----人打怪【物理】1，最小物攻={0},最小物攻增加={1}, 最大物攻={2}, 最大物攻增加={3} , 幸运={4}, 卓越={5}", minAttackV, addAttackMin, maxAttackV, addAttackMax, lucky, nFatalValue);

            if (monster is Robot)
            {
                Robot robot = monster as Robot;
                // 卓越属性的影响
                lucky -= (int)robot.DeLucky;
                nFatalValue -= (int)robot.DeFatalValue;

                //logInfo = string.Format("\n----人打怪【物理】2，幸运抵抗={0},卓越抵抗={1}, 幸运={2}, 卓越={3}", (int)robot.DeLucky, (int)robot.DeFatalValue, lucky, nFatalValue);
            }

            //LogManager.WriteLog(LogTypes.Error, logInfo);

            int attackV = (int)RoleAlgorithm.CalcAttackValue(client, minAttackV+addAttackMin, maxAttackV+addAttackMax, lucky, nFatalValue, out burst);
            attackV = (int)(attackV * attackPercent);

            //logInfo = string.Format("\n----人打怪【物理】3，攻击力={0},加成={1} \n", attackV, attackPercent);

            int minDefenseV = (int)RoleAlgorithm.GetMinADefenseV(monster);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxADefenseV(monster);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            //logInfo += string.Format("\n----人打怪【物理】4，minDefenseV={0},maxDefenseV={1}, defenseV={2}", minDefenseV, maxDefenseV, defenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
                burst = (int)DamageType.DAMAGETYPE_IGNOREDEFENCE;   // 无视防御了
            }
            else
            {
                //获取忽视对方防御后的防御值
                defenseV = GetDefenseByCalcingIgnoreDefense(0, client, defenseV, ref burst);
            }

            // 无视对方防御比例 [3/6/2014 LiaoWei]
            if (defenseV > 0)
                defenseV = (int)(defenseV * (1 - GetIgnoreDefenseRate(client)));

            //logInfo += string.Format("\n----人打怪【物理】5，无视防御, defenseV={0}", defenseV);

            injure += (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            //logInfo += string.Format("\n----人打怪【物理】6，伤害={0}", injure);

            #endregion 伤害计算

            #region 伤害加成和减免

            //伤害比例
            injure = (int)(injure * (1 + GetAddAttackInjurePercent(client) + GetPhySkillIncrease(client)));
            //logInfo += string.Format("\n----人打怪【物理】7，伤害={0}", injure);

            //技能伤害比例和附加伤害
            injure = (int)(injure * injurePercnet + GetAddAttackInjureValue(client) + addInjure);
            //logInfo += string.Format("\n----人打怪【物理】8，伤害比例={0}，附加伤害={1}，伤害={2}", injurePercnet, addInjure, injure);

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(0, monster, injure, client);
            //logInfo += string.Format("\n----人打怪【物理】9，伤害={0}", injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            if (injure <= 0)
            {
                injure = -1;
                return;
            }

            #endregion 伤害加成和减免

            injure = (int)RoleAlgorithm.CalcInjureValue(client, monster, injure, ref burst, injurePercnet);

            //logInfo = string.Format("\n----人打怪【物理】10，伤害={0}", injure);

            #region 技能的伤害系数和附加伤害

            // 技能改造[3/13/2014 LiaoWei]
            injure = (int)(injure * baseRate + addVlue);
            //logInfo += string.Format("\n----人打怪【物理】11，伤害系数={0}，附加伤害={1}，伤害={2}", baseRate, addVlue, injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);
      
            #endregion 技能的伤害系数和附加伤害

            #region 梅林魔法书各种几率日志记录 给检测用 [XSea 2015/7/23]
            /*logInfo = string.Format("\n----人打怪 【梅林魔法书】冰冻几率={0}%,麻痹几率={1}%,减速几率={2}%,重击几率={3}%,重生几率={4}%,魔法全恢复几率={5}%",
                RoleAlgorithm.GetFrozenPercent(client), RoleAlgorithm.GetPalsyPercent(client), RoleAlgorithm.GetSpeedDownPercent(client), RoleAlgorithm.GetBlowPercent(client),
                RoleAlgorithm.GetAutoRevivePercent(client) * 100, client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP16] * 100);
            LogManager.WriteLog(LogTypes.Error, logInfo);*/
            #endregion

            if (ElementsAttack.ElementsAttackManager.LogElementInjure)
            {
                string _log = string.Format(/**/"人打怪的元素伤害: ") + GameManager.ElementsAttackMgr.CalcElementInjureLog(client, monster, injurePercnet);
                LogManager.WriteLog(LogTypes.Error, _log);
            }
            client.CheckCheatData.LastDamage = injure;
            client.CheckCheatData.LastEnemyID = monster.GetObjectID();
#if ___CC___FUCK___YOU___BB___
            client.CheckCheatData.LastEnemyName = monster.XMonsterInfo.Name;
#else
            client.CheckCheatData.LastEnemyName = monster.MonsterInfo.VSName;
#endif

            client.CheckCheatData.LastEnemyPos = monster.CurrentPos;
        }

        /// <summary>
        /// 怪攻击角色的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void AttackEnemy(Monster monster, GameClient client, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, out int burst, out int injure, bool ignoreDefenseAndDodge, double baseRate, int addVlue)
        {
            burst = 0;
            injure = 0;

            #region 命中判定

            if (!ignoreDefenseAndDodge)
            {
                if (ClientIgnorePhyAttack(client, ref burst))
                {
                    return;
                }

                //判断是否命中
                double hitV = RoleAlgorithm.GetHitV(monster);
                double dodgeV = RoleAlgorithm.GetDodgeV(client);

                int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
                if (hit <= 0) //表示对方闪避掉了自己的攻击
                {
                    injure = 0;
                    return;
                }
            }

            #endregion 命中判定

            #region 伤害计算

            int minAttackV = (int)RoleAlgorithm.GetMinAttackV(monster);
            int maxAttackV = (int)RoleAlgorithm.GetMaxAttackV(monster);
            int lucky = (int)RoleAlgorithm.GetLuckV(monster);
            int nFatalValue = (int)RoleAlgorithm.GetFatalAttack(monster);    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

            //string logInfo = string.Format("\n----怪——》人【物理】1，最小物攻={0},最小物攻增加={1}, 最大物攻={2}, 最大物攻增加={3} , 幸运={4}, 卓越={5}", minAttackV, addAttackMin, maxAttackV, addAttackMax, lucky, nFatalValue);

            if (monster is Robot)
            {
                Robot robot = monster as Robot;

                lucky = robot.Lucky;
                nFatalValue = robot.FatalValue;    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

                // 卓越属性的影响
                lucky -= (int)RoleAlgorithm.GetDeLuckyAttack(client);
                nFatalValue -= (int)RoleAlgorithm.GetDeFatalAttack(client);

                //logInfo = string.Format("\n----怪——》人【物理】2，幸运抵抗={0},卓越抵抗={1}, 幸运={2}, 卓越={3}", (int)robot.DeLucky, (int)robot.DeFatalValue, lucky, nFatalValue);
            }

            //LogManager.WriteLog(LogTypes.Error, logInfo);

            int attackV = (int)RoleAlgorithm.CalcAttackValue(monster, minAttackV + addAttackMin, maxAttackV + addAttackMax, lucky, nFatalValue, out burst);
            attackV = (int)(attackV * attackPercent);

            //logInfo = string.Format("\n----怪——》人【物理】3，攻击力={0},加成={1} \n", attackV, attackPercent);

            int minDefenseV = (int)RoleAlgorithm.GetMinADefenseV(client);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxADefenseV(client);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            //logInfo += string.Format("\n----怪——》人【物理】4，minDefenseV={0},maxDefenseV={1}, defenseV={2}", minDefenseV, maxDefenseV, defenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
            }
            else
            {
                //获取忽视对方防御后的防御值
                defenseV = GetDefenseByCalcingIgnoreDefense(0, monster, defenseV, ref burst);
            }

            // 无视对方防御比例 [3/6/2014 LiaoWei]
            if (defenseV > 0)
                defenseV = (int)(defenseV * (1 - GetIgnoreDefenseRate(monster)));

            //logInfo += string.Format("\n----怪——》人【物理】5，无视防御, defenseV={0}", defenseV);

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            //logInfo += string.Format("\n----怪——》人【物理】6，伤害={0}", injure);

            #endregion 伤害计算

            #region 伤害加成和减免

            injure = (int)(injure * (1 + GetAddAttackInjurePercent(monster)));
            //logInfo += string.Format("\n----怪——》人【物理】7，伤害={0}", injure);

            //伤害比例
            injure = (int)(injure * injurePercnet + GetAddAttackInjureValue(monster) + addInjure);
            //logInfo += string.Format("\n----怪——》人【物理】8，伤害比例={0}，附加伤害={1}，伤害={2}", injurePercnet, addInjure, injure);

            // 竞技场的伤害为50% ChenXiaojun
            if (monster is Robot)
            {
                injure /= 2;
            }

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(0, client, injure);

            //logInfo += string.Format("\n----怪——》人【物理】10，伤害={0}", injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            if (injure <= 0)
            {
                injure = -1;
                return;
            }
            #endregion 伤害加成和减免

            injure = (int)RoleAlgorithm.CalcInjureValue(monster, client, injure, ref burst, injurePercnet);
            //logInfo = string.Format("\n----怪——》人【物理】11，伤害={0}", injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);
        }

        /// <summary>
        /// 怪攻击怪物的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void AttackEnemy(Monster monster, Monster enemy, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, out int burst, out int injure, bool ignoreDefenseAndDodge)
        {
            burst = 0;
            injure = 0;

            #region 命中判定

            if (!ignoreDefenseAndDodge)
            {
                //判断是否命中
                double hitV = RoleAlgorithm.GetHitV(monster);
                double dodgeV = RoleAlgorithm.GetDodgeV(enemy);
                int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
                if (hit <= 0) //表示对方闪避掉了自己的攻击
                {
                    injure = 0;
                    return;
                }
            }

            #endregion 命中判定

            #region 伤害计算

            int minAttackV = (int)RoleAlgorithm.GetMinAttackV(monster);
            int maxAttackV = (int)RoleAlgorithm.GetMaxAttackV(monster);
            int lucky = (int)RoleAlgorithm.GetLuckV(monster);
            int nFatalValue = (int)RoleAlgorithm.GetFatalAttack(monster);    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

            if (monster is Robot)
            {
                Robot robot = monster as Robot;

                lucky = robot.Lucky;
                nFatalValue = robot.FatalValue;    // 属性改造 卓越一击 [8/15/2013 LiaoWei]
            }

            int attackV = (int)RoleAlgorithm.CalcAttackValue(monster, minAttackV + addAttackMin, maxAttackV + addAttackMax, lucky, nFatalValue, out burst);
            attackV = (int)(attackV * attackPercent);
            //attackV += addAttack;

            int minDefenseV = (int)RoleAlgorithm.GetMinADefenseV(enemy);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxADefenseV(enemy);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
            }
            else
            {
                //获取忽视对方防御后的防御值
                defenseV = GetDefenseByCalcingIgnoreDefense(0, monster, defenseV, ref burst);
            }

            // 无视对方防御比例 [3/6/2014 LiaoWei]
            if (defenseV > 0)
                defenseV = (int)(defenseV * (1 - GetIgnoreDefenseRate(monster)));

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            #endregion 伤害计算

            #region 伤害加成和减免

            //伤害比例
            injure = (int)(injure * (1 + GetAddAttackInjurePercent(monster)));

            injure = (int)(injure * injurePercnet + GetAddAttackInjureValue(monster) + addInjure);

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(0, enemy, injure);
            if (injure <= 0)
            {
                injure = -1;
                return;
            }

            #endregion 伤害加成和减免
        }

        /// <summary>
        /// 角色攻击角色的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void AttackEnemy(GameClient client, GameClient enemy, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, out int burst, out int injure, bool ignoreDefenseAndDodge, double baseRate, int addVlue)
        {
            burst = 0;
            injure = 0;

            #region 命中判定

            if (!ignoreDefenseAndDodge)
            {
                if (ClientIgnorePhyAttack(enemy, ref burst))
                {
                    return;
                }

                //判断是否命中
                double hitV = RoleAlgorithm.GetHitV(client);
                double dodgeV = RoleAlgorithm.GetDodgeV(enemy);
                int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
                if (hit <= 0) //表示对方闪避掉了自己的攻击
                {
                    injure = 0;
                    return;
                }
            }

            #endregion 命中判定

            #region 伤害计算

            int minAttackV = (int)RoleAlgorithm.GetMinAttackV(client);
            int maxAttackV = (int)RoleAlgorithm.GetMaxAttackV(client);
            int lucky = (int)RoleAlgorithm.GetLuckV(client);
            int nFatalValue = (int)RoleAlgorithm.GetFatalAttack(client);    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

            //string logInfo = string.Format("\n----人——》人【物理】1，最小物攻={0},最小物攻增加={1}, 最大物攻={2}, 最大物攻增加={3} , 幸运={4}, 卓越={5}", minAttackV, addAttackMin, maxAttackV, addAttackMax, lucky, nFatalValue);

            // 卓越属性的影响
            lucky -= (int)RoleAlgorithm.GetDeLuckyAttack(enemy);
            nFatalValue -= (int)RoleAlgorithm.GetDeFatalAttack(enemy);
            //logInfo += string.Format("\n----人——》人【物理】2，幸运抵抗={0},卓越抵抗={1}, 幸运={2}, 卓越={3}", (int)RoleAlgorithm.GetDeLuckyAttack(enemy), (int)RoleAlgorithm.GetDeFatalAttack(enemy), lucky, nFatalValue);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            int attackV = (int)RoleAlgorithm.CalcAttackValue(client, minAttackV + addAttackMin, maxAttackV + addAttackMax, lucky, nFatalValue, out burst);
            attackV = (int)(attackV * attackPercent);

            //logInfo = string.Format("\n----人——》人【物理】3，攻击力={0},加成={1}", attackV, attackPercent);

            int minDefenseV = (int)RoleAlgorithm.GetMinADefenseV(enemy);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxADefenseV(enemy);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            //logInfo += string.Format("\n----人——》人【物理】4，minDefenseV={0},maxDefenseV={1}, defenseV={2}", minDefenseV, maxDefenseV, defenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
                burst = (int)DamageType.DAMAGETYPE_IGNOREDEFENCE;   // 无视防御了
            }
            else
            {
                //获取忽视对方防御后的防御值
                defenseV = GetDefenseByCalcingIgnoreDefense(0, client, defenseV, ref burst);
            }

            // 无视对方防御比例 [3/6/2014 LiaoWei]
            if (defenseV > 0)
                defenseV = (int)(defenseV * (1 - GetIgnoreDefenseRate(client)));

            //logInfo += string.Format("\n----人——》人【物理】5，无视防御, defenseV={0}", defenseV);

            if (defenseV < 0)
                defenseV = 0;

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            //logInfo = string.Format("\n----人——》人【物理】6，伤害={0}", injure);
            #endregion 伤害计算

            #region 伤害加成和减免

            //伤害比例
            injure = (int)(injure * (1 + GetAddAttackInjurePercent(client) + GetPhySkillIncrease(client)));
            //logInfo += string.Format("\n----人——》人【物理】7，伤害={0}", injure);

            //技能伤害比例和附加伤害
            injure = (int)(injure * injurePercnet + GetAddAttackInjureValue(client) + addInjure);
            //logInfo += string.Format("\n----人——》人【物理】8，伤害比例={0}，附加伤害={1}，伤害={2}", injurePercnet, addInjure, injure);

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(0, enemy, injure, client);
            //logInfo += string.Format("\n----人——》人【物理】9，伤害={0}", injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            if (injure <= 0)
            {
                injure = -1;
                return;
            }

            #endregion 伤害加成和减免

            injure = (int)RoleAlgorithm.CalcInjureValue(client, enemy, injure, ref burst, injurePercnet);
            //logInfo = string.Format("\n----人——》人【物理】10，伤害={0}", injure);

            #region 技能的伤害系数和附加伤害

            // 技能改造[3/13/2014 LiaoWei]
            injure = (int)(injure * baseRate + addVlue);
            //logInfo += string.Format("\n----人——》人【物理】11，伤害系数={0}，附加伤害={1}，伤害={2}", baseRate, addVlue, injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            client.CheckCheatData.LastDamage = injure;
            client.CheckCheatData.LastEnemyID = enemy.ClientData.RoleID;
            client.CheckCheatData.LastEnemyName = enemy.ClientData.RoleName;
            client.CheckCheatData.LastEnemyPos = enemy.CurrentPos;

            #endregion 技能的伤害系数和附加伤害

            #region 梅林魔法书各种几率日志记录 给检测用 [XSea 2015/7/23]
            /**//*logInfo = string.Format("\n----人打人 【梅林魔法书】冰冻几率={0}%,麻痹几率={1}%,减速几率={2}%,重击几率={3}%,重生几率={4}%,魔法全恢复几率={5}%",
                RoleAlgorithm.GetFrozenPercent(client), RoleAlgorithm.GetPalsyPercent(client), RoleAlgorithm.GetSpeedDownPercent(client), RoleAlgorithm.GetBlowPercent(client),
                RoleAlgorithm.GetAutoRevivePercent(client) * 100, client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP16] * 100);
            LogManager.WriteLog(LogTypes.Error, logInfo);*/
            #endregion

            if (ElementsAttack.ElementsAttackManager.LogElementInjure)
            {
                /**/string _log = string.Format("人打人的元素伤害: ") + GameManager.ElementsAttackMgr.CalcElementInjureLog(client, enemy, injurePercnet);
                LogManager.WriteLog(LogTypes.Error, _log);
            }
        }

        #endregion 物理攻击伤害计算

        #region 魔法攻击伤害计算

        /// <summary>
        /// 角色攻击怪的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void MAttackEnemy(GameClient client, Monster monster, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, out int burst, out int injure, bool ignoreDefenseAndDodge, double baseRate, int addVlue)
        {
            burst = 0;
            injure = 0;

            #region 命中判定

            if (!ignoreDefenseAndDodge)
            {
                //判断是否命中
                double hitV = RoleAlgorithm.GetHitV(client);
                double dodgeV = RoleAlgorithm.GetDodgeV(monster);

                int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
                //魔法闪避值(百分比)
                //int hit = (int)GetRealHitRate(GetMagicDodgePercentV(monster));
                if (hit <= 0) //表示对方闪避掉了自己的攻击
                {
                    injure = 0;
                    return;
                }
            }

            #endregion 命中判定

            #region 伤害计算

            int minAttackV = (int)RoleAlgorithm.GetMinMagicAttackV(client);
            int maxAttackV = (int)RoleAlgorithm.GetMaxMagicAttackV(client);
            int lucky = (int)RoleAlgorithm.GetLuckV(client);
            int nFatalValue = (int)RoleAlgorithm.GetFatalAttack(client);    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

            //string logInfo = string.Format("\n----人打怪【魔法】1，最小物攻={0},最小物攻增加={1}, 最大物攻={2}, 最大物攻增加={3} , 幸运={4}, 卓越={5}", minAttackV, addAttackMin, maxAttackV, addAttackMax, lucky, nFatalValue);

            if (monster is Robot)
            {
                Robot robot = monster as Robot;

                lucky -= (int)robot.DeLucky;
                nFatalValue -= (int)robot.DeFatalValue;

                //logInfo += string.Format("\n----人打怪【魔法】2，幸运抵抗={0},卓越抵抗={1}, 幸运={2}, 卓越={3}", (int)robot.DeLucky, (int)robot.DeFatalValue, lucky, nFatalValue);
            }

            //LogManager.WriteLog(LogTypes.Error, logInfo);

            int attackV = (int)RoleAlgorithm.CalcAttackValue(client, minAttackV + addAttackMin, maxAttackV + addAttackMax, lucky, nFatalValue, out burst);
            attackV = (int)(attackV * attackPercent);

            //logInfo = string.Format("\n----人打怪【魔法】3，攻击力={0},加成={1} \n", attackV, attackPercent);

            int minDefenseV = (int)RoleAlgorithm.GetMinMDefenseV(monster);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxMDefenseV(monster);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            //logInfo += string.Format("\n----人打怪【魔法】4，minDefenseV={0},maxDefenseV={1}, defenseV={2}", minDefenseV, maxDefenseV, defenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
                burst = (int)DamageType.DAMAGETYPE_IGNOREDEFENCE;   // 无视防御了
            }
            else
            {
                //获取忽视对方防御后的防御值
                defenseV = GetDefenseByCalcingIgnoreDefense(1, client, defenseV, ref burst);
            }

            // 无视对方防御比例 [3/6/2014 LiaoWei]
            if (defenseV > 0)
                defenseV = (int)(defenseV * (1 - GetIgnoreDefenseRate(client)));

            //logInfo += string.Format("\n----人打怪【魔法】5，无视防御, defenseV={0}", defenseV);

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            //logInfo += string.Format("\n----人打怪【魔法】6，伤害={0}", injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);
            #endregion 伤害计算

            #region 伤害加成和减免

            //伤害比例
            injure = (int)(injure * (1 + GetAddAttackInjurePercent(client) + GetMagicSkillIncrease(client)));
            //logInfo += string.Format("\n----人打怪【魔法】7，伤害={0}", injure);

            //技能伤害比例和附加伤害
            injure = (int)(injure * injurePercnet + GetAddAttackInjureValue(client) + addInjure);
            //logInfo += string.Format("\n----人打怪【魔法】8，伤害比例={0}，附加伤害={1}，伤害={2}", injurePercnet, addInjure, injure);

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(1, monster, injure, client);
            //logInfo += string.Format("\n----人打怪【魔法】9，伤害={0}", injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            if (injure <= 0)
            {
                injure = -1;
                return;
            }

            #endregion 伤害加成和减免

            injure = (int)RoleAlgorithm.CalcInjureValue(client, monster, injure, ref burst, injurePercnet);
            //logInfo = string.Format("\n----人打怪【魔法】10，伤害={0}", injure);

            #region 技能的伤害系数和附加伤害

            // 技能改造[3/13/2014 LiaoWei]
            injure = (int)(injure * baseRate + addVlue);
            //logInfo += string.Format("\n----人打怪【魔法】11，伤害系数={0}，附加伤害={1}，伤害={2}", baseRate, addVlue, injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);
            #endregion 技能的伤害系数和附加伤害

            #region 梅林魔法书各种几率日志记录 给检测用 [XSea 2015/7/23]
            /*logInfo = string.Format("\n----人打怪 【梅林魔法书】冰冻几率={0}%,麻痹几率={1}%,减速几率={2}%,重击几率={3}%,重生几率={4}%,魔法全恢复几率={5}%",
                RoleAlgorithm.GetFrozenPercent(client), RoleAlgorithm.GetPalsyPercent(client), RoleAlgorithm.GetSpeedDownPercent(client), RoleAlgorithm.GetBlowPercent(client),
                RoleAlgorithm.GetAutoRevivePercent(client) * 100, client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP16] * 100);
            LogManager.WriteLog(LogTypes.Error, logInfo);*/
            #endregion

            if (ElementsAttack.ElementsAttackManager.LogElementInjure)
            {
                string _log = string.Format(/**/"人打怪的元素伤害: ") + GameManager.ElementsAttackMgr.CalcElementInjureLog(client, monster, injurePercnet);
                LogManager.WriteLog(LogTypes.Error, _log);
            }
            client.CheckCheatData.LastDamage = injure;
            client.CheckCheatData.LastEnemyID = monster.GetObjectID();
#if ___CC___FUCK___YOU___BB___
            client.CheckCheatData.LastEnemyName = monster.XMonsterInfo.Name;
#else
            client.CheckCheatData.LastEnemyName = monster.MonsterInfo.VSName;
#endif

            client.CheckCheatData.LastEnemyPos = monster.CurrentPos;
        }

        /// <summary>
        /// 怪攻击角色的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void MAttackEnemy(Monster monster, GameClient client, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, out int burst, out int injure, bool ignoreDefenseAndDodge, double baseRate, int addVlue)
        {
            burst = 0;
            injure = 0;

#region 命中判定

            if (!ignoreDefenseAndDodge)
            {
                if (ClientIgnoreMagicAttack(client, ref burst))
                {
                    return;
                }

                //判断是否命中
                double hitV = RoleAlgorithm.GetHitV(monster);
                double dodgeV = RoleAlgorithm.GetDodgeV(client);

                int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
                //魔法闪避值(百分比)
                //int hit = (int)GetRealHitRate(GetMagicDodgePercentV(client));
                if (hit <= 0) //表示对方闪避掉了自己的攻击
                {
                    injure = 0;
                    return;
                }
            }

#endregion 命中判定

#region 伤害计算

            int minAttackV = (int)RoleAlgorithm.GetMinMagicAttackV(monster);
            int maxAttackV = (int)RoleAlgorithm.GetMaxMagicAttackV(monster);
            int lucky = 0;
            int nFatalValue = 0;    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

            //string logInfo = string.Format("\n----怪——》人【魔法】1，最小物攻={0},最小物攻增加={1}, 最大物攻={2}, 最大物攻增加={3} , 幸运={4}, 卓越={5}", minAttackV, addAttackMin, maxAttackV, addAttackMax, lucky, nFatalValue);

            if (monster is Robot)
            {
                Robot robot = monster as Robot;

                lucky = robot.Lucky;
                nFatalValue = robot.FatalValue;    

                // 卓越属性的影响
                lucky -= (int)RoleAlgorithm.GetDeLuckyAttack(client);
                nFatalValue -= (int)RoleAlgorithm.GetDeFatalAttack(client);

                //logInfo += string.Format("\n----怪——》人【魔法】2，幸运抵抗={0},卓越抵抗={1}, 幸运={2}, 卓越={3}", (int)robot.DeLucky, (int)robot.DeFatalValue, lucky, nFatalValue);
            }

            //LogManager.WriteLog(LogTypes.Error, logInfo);

            int attackV = (int)RoleAlgorithm.CalcAttackValue(monster, minAttackV + addAttackMin, maxAttackV + addAttackMax, lucky, nFatalValue, out burst);
            attackV = (int)(attackV * attackPercent);

            //logInfo = string.Format("\n----怪——》人【魔法】3，攻击力={0},加成={1} \n", attackV, attackPercent);

            int minDefenseV = (int)RoleAlgorithm.GetMinMDefenseV(client);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxMDefenseV(client);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            //logInfo += string.Format("\n----怪——》人【魔法】4，minDefenseV={0},maxDefenseV={1}, defenseV={2}", minDefenseV, maxDefenseV, defenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
            }
            else
            {
                //获取忽视对方防御后的防御值
                defenseV = GetDefenseByCalcingIgnoreDefense(0, monster, defenseV, ref burst);
            }

            // 无视对方防御比例 [3/6/2014 LiaoWei]
            if (defenseV > 0)
                defenseV = (int)(defenseV * (1 - GetIgnoreDefenseRate(monster)));

            //logInfo += string.Format("\n----怪——》人【魔法】5，无视防御, defenseV={0}", defenseV);

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            //logInfo += string.Format("\n----怪——》人【魔法】6，伤害={0}", injure);

#endregion 伤害计算

#region 伤害加成和减免

            injure = (int)(injure * (1 + GetAddAttackInjurePercent(monster)));
            //logInfo += string.Format("\n----怪——》人【魔法】7，伤害={0}", injure);

            //伤害比例
            injure = (int)(injure * injurePercnet + GetAddAttackInjureValue(monster) + addInjure);
            //logInfo += string.Format("\n----怪——》人【魔法】8，伤害比例={0}，附加伤害={1}，伤害={2}", injurePercnet, addInjure, injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);
            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(1, client, injure);
            //logInfo = string.Format("\n----怪——》人【魔法】10，伤害={0}", injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            if (injure <= 0)
            {
                injure = -1;
                return;
            }

#endregion 伤害加成和减免

            injure = (int)RoleAlgorithm.CalcInjureValue(monster, client, injure, ref burst, injurePercnet);
            //logInfo = string.Format("\n----怪——》人【魔法】10，伤害={0}", injure);

            // 技能改造[3/13/2014 LiaoWei]
            injure = (int)(injure * baseRate + addVlue);

            //logInfo += string.Format("\n----怪——》人【魔法】11，伤害系数={0}，附加伤害={1}，伤害={2}", baseRate, addVlue, injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);
        }

        /// <summary>
        /// 怪攻击怪的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void MAttackEnemy(Monster monster, Monster enemy, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, out int burst, out int injure, bool ignoreDefenseAndDodge)
        {
            burst = 0;
            injure = 0;

#region 命中判定

            if (!ignoreDefenseAndDodge)
            {
                //判断是否命中
                double hitV = RoleAlgorithm.GetHitV(monster);
                double dodgeV = RoleAlgorithm.GetDodgeV(enemy);
                int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);

                //魔法闪避值(百分比)
                //int hit = (int)GetRealHitRate(GetMagicDodgePercentV(enemy));
                if (hit <= 0) //表示对方闪避掉了自己的攻击
                {
                    return;
                }
            }

#endregion 命中判定

#region 伤害计算

            int minAttackV = (int)RoleAlgorithm.GetMinMagicAttackV(monster);
            int maxAttackV = (int)RoleAlgorithm.GetMaxMagicAttackV(monster);
            int lucky = 0;
            int nFatalValue = 0;    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

            if (monster is Robot)
            {
                Robot robot = monster as Robot;

                lucky = robot.Lucky;
                nFatalValue = robot.FatalValue;    
            }

            int attackV = (int)RoleAlgorithm.CalcAttackValue(monster, minAttackV + addAttackMin, maxAttackV + addAttackMax, lucky, nFatalValue, out burst);
            attackV = (int)(attackV * attackPercent);
            //attackV += addAttack;

            int minDefenseV = (int)RoleAlgorithm.GetMinMDefenseV(enemy);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxMDefenseV(enemy);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
            }
            else
            {
                //获取忽视对方防御后的防御值
                defenseV = GetDefenseByCalcingIgnoreDefense(0, monster, defenseV, ref burst);
            }

            // 无视对方防御比例 [3/6/2014 LiaoWei]
            if (defenseV > 0)
                defenseV = (int)(defenseV * (1 - GetIgnoreDefenseRate(monster)));

           injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

#endregion 伤害计算

#region 伤害加成和减免

            //伤害比例
            injure = (int)(injure * (1 + GetAddAttackInjurePercent(monster)));

            injure = (int)(injure * injurePercnet + GetAddAttackInjureValue(monster) + addInjure);

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(1, enemy, injure);
            if (injure <= 0)
            {
                injure = -1;
                return;
            }

#endregion 伤害加成和减免
        }

        /// <summary>
        /// 角色攻击角色的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void MAttackEnemy(GameClient client, GameClient enemy, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, out int burst, out int injure, bool ignoreDefenseAndDodge, double baseRate, int addVlue)
        {
            burst = 0;
            injure = 0;

#region 命中判定

            if (!ignoreDefenseAndDodge)
            {
                if (ClientIgnoreMagicAttack(enemy, ref burst))
                {
                    return;
                }

                //判断是否命中
                double hitV = RoleAlgorithm.GetHitV(client);
                double dodgeV = RoleAlgorithm.GetDodgeV(enemy);
                int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);

                //魔法闪避值(百分比)
                //int hit = (int)GetRealHitRate(GetMagicDodgePercentV(enemy));
                if (hit <= 0) //表示对方闪避掉了自己的攻击
                {
                    return;
                }
            }

#endregion 命中判定

#region 伤害计算
            int minAttackV = (int)RoleAlgorithm.GetMinMagicAttackV(client);
            int maxAttackV = (int)RoleAlgorithm.GetMaxMagicAttackV(client);
            int lucky = (int)RoleAlgorithm.GetLuckV(client);                // 幸运一击
            int nFatalValue = (int)RoleAlgorithm.GetFatalAttack(client);    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

            //string logInfo = string.Format("\n----人——》人【魔法】1，最小物攻={0},最小物攻增加={1}, 最大物攻={2}, 最大物攻增加={3} , 幸运={4}, 卓越={5}", minAttackV, addAttackMin, maxAttackV, addAttackMax, lucky, nFatalValue);

            // 卓越属性的影响
            lucky -= (int)RoleAlgorithm.GetDeLuckyAttack(enemy);
            nFatalValue -= (int)RoleAlgorithm.GetDeFatalAttack(enemy);
            //logInfo += string.Format("\n----人——》人【魔法】2，幸运抵抗={0},卓越抵抗={1}, 幸运={2}, 卓越={3}", (int)RoleAlgorithm.GetDeLuckyAttack(enemy), (int)RoleAlgorithm.GetDeFatalAttack(enemy), lucky, nFatalValue);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            int attackV = (int)RoleAlgorithm.CalcAttackValue(client, minAttackV + addAttackMin, maxAttackV + addAttackMax, lucky, nFatalValue, out burst);
            attackV = (int)(attackV * attackPercent);

            //logInfo = string.Format("\n----人——》人【魔法】3，攻击力={0},加成={1} \n", attackV, attackPercent);

            int minDefenseV = (int)RoleAlgorithm.GetMinMDefenseV(enemy);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxMDefenseV(enemy);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            //logInfo += string.Format("\n----人——》人【魔法】4，minDefenseV={0},maxDefenseV={1}, defenseV={2}", minDefenseV, maxDefenseV, defenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
                burst = (int)DamageType.DAMAGETYPE_IGNOREDEFENCE;   // 无视防御了
            }
            else
            {
                //获取忽视对方防御后的防御值
                defenseV = GetDefenseByCalcingIgnoreDefense(1, client, defenseV, ref burst);
            }

            // 无视对方防御比例 [3/6/2014 LiaoWei]
            if (defenseV > 0)
                defenseV = (int)(defenseV * (1 - GetIgnoreDefenseRate(client)));

            //logInfo += string.Format("\n----人——》人【魔法】5，无视防御, defenseV={0}", defenseV);

            if (defenseV < 0)  defenseV = 0;

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            //logInfo += string.Format("\n----人——》人【魔法】6，伤害={0}", injure);

#endregion 伤害计算

#region 伤害加成和减免

            //伤害比例
            injure = (int)(injure * (1 + GetAddAttackInjurePercent(client) + GetMagicSkillIncrease(client)));
            //logInfo += string.Format("\n----人——》人【魔法】7，伤害={0}", injure);

            //技能伤害比例和附加伤害
            injure = (int)(injure * injurePercnet + GetAddAttackInjureValue(client) + addInjure);
            //logInfo += string.Format("\n----人——》人【魔法】8，伤害比例={0}，附加伤害={1}，伤害={2}", injurePercnet, addInjure,injure);

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(1, enemy, injure, client);
            //logInfo += string.Format("\n----人——》人【魔法】9，伤害={0}", injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);
            if (injure <= 0)
            {
                injure = -1;
                return;
            }

#endregion 伤害加成和减免

            injure = (int)RoleAlgorithm.CalcInjureValue(client, enemy, injure, ref burst, injurePercnet);
            //logInfo = string.Format("\n----人——》人【魔法】10，伤害={0}", injure);

#region 技能的伤害系数和附加伤害

            // 技能改造[3/13/2014 LiaoWei]
            injure = (int)(injure * baseRate + addVlue);
            //logInfo += string.Format("\n----人——》人【魔法】11，伤害系数={0}，附加伤害={1}，伤害={2}", baseRate, addVlue, injure);
            //LogManager.WriteLog(LogTypes.Error, logInfo);

            client.CheckCheatData.LastDamage = injure;
            client.CheckCheatData.LastEnemyID = enemy.ClientData.RoleID;
            client.CheckCheatData.LastEnemyName = enemy.ClientData.RoleName;
            client.CheckCheatData.LastEnemyPos = enemy.CurrentPos;

#endregion 技能的伤害系数和附加伤害

#region 梅林魔法书各种几率日志记录 给检测用 [XSea 2015/7/23]
            /**//*logInfo = string.Format("\n----人打人 【梅林魔法书】冰冻几率={0}%,麻痹几率={1}%,减速几率={2}%,重击几率={3}%,重生几率={4}%,魔法全恢复几率={5}%",
                RoleAlgorithm.GetFrozenPercent(client), RoleAlgorithm.GetPalsyPercent(client), RoleAlgorithm.GetSpeedDownPercent(client), RoleAlgorithm.GetBlowPercent(client),
                RoleAlgorithm.GetAutoRevivePercent(client) * 100, client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP16] * 100);
            LogManager.WriteLog(LogTypes.Error, logInfo);*/
#endregion

            if (ElementsAttack.ElementsAttackManager.LogElementInjure)
            {
                /**/string _log = string.Format("人打人的元素伤害: ") + GameManager.ElementsAttackMgr.CalcElementInjureLog(client, enemy, injurePercnet);
                LogManager.WriteLog(LogTypes.Error, _log);
            }
        }

#endregion //魔法攻击伤害计算

#region 状态命中属性计算

        public static double GetRoleNegativeRate(GameClient client, double baseVal, ExtPropIndexes extPropIndex)
        {
            double val = 0.0;
            // 填的就是百分比，不应该除以100
            val = (client.ClientData.EquipProp.ExtProps[(int)extPropIndex]) + (client.RoleBuffer.GetExtProp((int)extPropIndex));

            val += client.ClientData.PropsCacheManager.GetExtProp((int)extPropIndex);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, extPropIndex);

            val += client.RoleMultipliedBuffer.GetExtProp((int)extPropIndex);
            val += baseVal;

            return val;
        }

        /// <summary>
        /// 定身状态命中
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetRoleStateDingSheng(GameClient client, double baseVal)
        {
            //double val = 1.0;
            //// 填的就是百分比，不应该除以100
            //val = val * (1.0 + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.StateDingSheng]/* / 100.0*/) * (1.0 + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.StateDingSheng)/* / 100.0*/);

            double val = 0.0;
            // 填的就是百分比，不应该除以100
            val = (client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.StateDingShen]/* / 100.0*/) + (client.RoleBuffer.GetExtProp((int)ExtPropIndexes.StateDingShen)/* / 100.0*/);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.StateDingShen);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.StateDingShen);

            val = client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.StateDingShen, val);
            val += baseVal;

            val += 0.1 * client.ClientData.ChangeLifeCount;
            return val;
        }

        /// <summary>
        /// 减速状态命中
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetRoleStateMoveSpeed(GameClient client, double baseVal)
        {
            //double val = 1.0;
            //// 填的就是百分比，不应该除以100
            //val = val * (1.0 + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.StateMoveSpeed]/* / 100.0*/) * (1.0 + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.StateMoveSpeed)/* / 100.0*/);

            double val = 0.0;
            // 填的就是百分比，不应该除以100
            val = (client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.StateMoveSpeed]/* / 100.0*/) + (client.RoleBuffer.GetExtProp((int)ExtPropIndexes.StateMoveSpeed)/* / 100.0*/);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.StateMoveSpeed);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.StateMoveSpeed);

            val = client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.StateMoveSpeed, val);
            val += baseVal;

            val += 0.1 * client.ClientData.ChangeLifeCount;
            return val;
        }

        /// <summary>
        /// 击退状态命中
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetRoleStateJiTui(GameClient client, double baseVal)
        {
            //double val = 1.0;
            //// 填的就是百分比，不应该除以100
            //val = (1.0 + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.StateJiTui]/* / 100.0*/) * (1.0 + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.StateJiTui)/* / 100.0*/);

            double val = 0.0;
            // 填的就是百分比，不应该除以100
            val = (client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.StateJiTui]/* / 100.0*/) + (client.RoleBuffer.GetExtProp((int)ExtPropIndexes.StateJiTui)/* / 100.0*/);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.StateJiTui);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.StateJiTui);

            val = client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.StateJiTui, val);
            val += baseVal;

            return val;
        }

        /// <summary>
        /// 昏迷状态命中
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double GetRoleStateHunMi(GameClient client, double baseVal)
        {
            //double val = 1.0;
            //// 填的就是百分比，不应该除以100
            //val = val * (1.0 + client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.StateHunMi]/* / 100.0*/) * (1.0 + client.RoleBuffer.GetExtProp((int)ExtPropIndexes.StateHunMi)/* / 100.0*/);

            double val = 0.0;
            // 填的就是百分比，不应该除以100
            val = (client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.StateHunMi]/* / 100.0*/) + (client.RoleBuffer.GetExtProp((int)ExtPropIndexes.StateHunMi)/* / 100.0*/);

            val += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.StateHunMi);
            val += DBRoleBufferManager.ProcessTempBufferProp(client, ExtPropIndexes.StateHunMi);

            val = client.RoleMultipliedBuffer.GetExtProp((int)ExtPropIndexes.StateHunMi, val);
            val += baseVal;

            val += 0.1 * client.ClientData.ChangeLifeCount;
            return val;
        }

#endregion 状态命中属性计算

#region 道术攻击伤害计算

        // 属性改造 去掉道术攻击[8/15/2013 LiaoWei]

        /// <summary>
        /// 角色攻击怪的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        /*public static void DSAttackEnemy(GameClient client, Monster monster, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttack, out int burst, out int injure, bool ignoreDefenseAndDodge, bool ignoreDoge = false)
        {
            
            
            burst = 0;
            injure = 0;

            int minAttackV = (int)RoleAlgorithm.GetMinDSAttackV(client);
            int maxAttackV = (int)RoleAlgorithm.GetMaxDSAttackV(client);
            int lucky = (int)RoleAlgorithm.GetLuckV(client);
            int attackV = (int)RoleAlgorithm.CalcAttackValue(minAttackV, maxAttackV, lucky);
            attackV = (int)(attackV * attackPercent);
            attackV += addAttack;

            int minDefenseV = (int)RoleAlgorithm.GetMinMDefenseV(monster);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxMDefenseV(monster);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
            }

            //获取忽视对方防御后的防御值
            defenseV = GetDefenseByCalcingIgnoreDefense(1, client, defenseV);

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            //获取怪物的减伤比例
            injure = (int)(injure * injurePercnet);
            injure += addInjure;

            if (injure <= 0)
            {
                injure = -1;
                return;
            }

            if (ignoreDefenseAndDodge)
            {
                return;
            }

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(2, monster, injure);

            if (ignoreDoge)
            {
                return;
            }

            // 属性改造 去掉了”魔法闪避“[8/15/2013 LiaoWei]
            //如果不是暴击判断是否命中
            double hitV = RoleAlgorithm.GetHitV(client);
            double dodgeV = RoleAlgorithm.GetDodgeV(monster);
            int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
            //魔法闪避值(百分比)
            //int hit = (int)GetRealHitRate(GetMagicDodgePercentV(monster));
            if (hit <= 0) //表示对方闪避掉了自己的攻击
            {
                injure = 0;
            }
        }*/

        /// <summary>
        /// 怪攻击角色的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        /*public static void DSAttackEnemy(Monster monster, GameClient client, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttack, out int burst, out int injure, bool ignoreDefenseAndDodge, bool ignoreDoge = false)
        {
            burst = 0;
            injure = 0;

            int minAttackV = (int)RoleAlgorithm.GetMinDSAttackV(monster);
            int maxAttackV = (int)RoleAlgorithm.GetMaxDSAttackV(monster);
            int lucky = 0;
            int attackV = (int)RoleAlgorithm.CalcAttackValue(minAttackV, maxAttackV, lucky);
            attackV = (int)(attackV * attackPercent);
            attackV += addAttack;

            int minDefenseV = (int)RoleAlgorithm.GetMinMDefenseV(client);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxMDefenseV(client);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
            }

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            //获取怪物的减伤比例
            injure = (int)(injure * injurePercnet);
            injure += addInjure;

            if (injure <= 0)
            {
                injure = -1;
                return;
            }

            if (ignoreDefenseAndDodge)
            {
                return;
            }

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(2, client, injure);

            if (ignoreDoge)
            {
                return;
            }

            // 属性改造 去掉了”魔法闪避“[8/15/2013 LiaoWei]
            //如果不是暴击判断是否命中
            double hitV = RoleAlgorithm.GetHitV(monster);
            double dodgeV = RoleAlgorithm.GetDodgeV(client);
            int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
            //魔法闪避值(百分比)
            //int hit = (int)GetRealHitRate(GetMagicDodgePercentV(client));
            if (hit <= 0) //表示对方闪避掉了自己的攻击
            {
                injure = 0;
            }
        }

        /// <summary>
        /// 怪攻击怪的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void DSAttackEnemy(Monster monster, Monster enemy, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttack, out int burst, out int injure, bool ignoreDefenseAndDodge, bool ignoreDoge = false)
        {
            burst = 0;
            injure = 0;

            int minAttackV = (int)RoleAlgorithm.GetMinDSAttackV(monster);
            int maxAttackV = (int)RoleAlgorithm.GetMaxDSAttackV(monster);
            int lucky = 0;
            int attackV = (int)RoleAlgorithm.CalcAttackValue(minAttackV, maxAttackV, lucky);
            attackV = (int)(attackV * attackPercent);
            attackV += addAttack;

            int minDefenseV = (int)RoleAlgorithm.GetMinMDefenseV(enemy);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxMDefenseV(enemy);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
            }

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            //获取怪物的减伤比例
            injure = (int)(injure * injurePercnet);
            injure += addInjure;

            if (injure <= 0)
            {
                injure = -1;
                return;
            }

            if (ignoreDefenseAndDodge)
            {
                return;
            }

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(2, enemy, injure);

            if (ignoreDoge)
            {
                return;
            }

            // 属性改造 去掉了”魔法闪避“[8/15/2013 LiaoWei]
            //如果不是暴击判断是否命中
            double hitV = RoleAlgorithm.GetHitV(monster);
            double dodgeV = RoleAlgorithm.GetDodgeV(enemy);
            int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
            //魔法闪避值(百分比)
            //int hit = (int)GetRealHitRate(GetMagicDodgePercentV(enemy));
            if (hit <= 0) //表示对方闪避掉了自己的攻击
            {
                injure = 0;
            }
        }

        /// <summary>
        /// 角色攻击角色的计算公式
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        /// <param name="burst"></param>
        /// <param name="injure"></param>
        public static void DSAttackEnemy(GameClient client, GameClient enemy, bool forceBurst, double injurePercnet, int addInjure, double attackPercent, int addAttack, out int burst, out int injure, bool ignoreDefenseAndDodge, bool ignoreDoge = false)
        {
            burst = 0;
            injure = 0;

            int minAttackV = (int)RoleAlgorithm.GetMinDSAttackV(client);
            int maxAttackV = (int)RoleAlgorithm.GetMaxDSAttackV(client);
            int lucky = (int)RoleAlgorithm.GetLuckV(client);
            int attackV = (int)RoleAlgorithm.CalcAttackValue(minAttackV, maxAttackV, lucky);
            attackV = (int)(attackV * attackPercent);
            attackV += addAttack;

            int minDefenseV = (int)RoleAlgorithm.GetMinMDefenseV(enemy);
            int maxDefenseV = (int)RoleAlgorithm.GetMaxMDefenseV(enemy);
            int defenseV = (int)RoleAlgorithm.GetDefenseValue(minDefenseV, maxDefenseV);

            if (ignoreDefenseAndDodge)
            {
                defenseV = 0;
            }

            //获取忽视对方防御后的防御值
            defenseV = GetDefenseByCalcingIgnoreDefense(1, client, defenseV);

            injure = (int)RoleAlgorithm.GetRealInjuredValue(attackV, defenseV);

            injure = (int)(injure * injurePercnet);
            injure += addInjure;

            if (injure <= 0)
            {
                injure = -1;
                return;
            }

            if (ignoreDefenseAndDodge)
            {
                return;
            }

            //计算伤害吸收
            injure = RoleAlgorithm.CalcAttackInjure(2, enemy, injure);

            if (ignoreDoge)
            {
                return;
            }

            // 属性改造 去掉了”魔法闪避“[8/15/2013 LiaoWei]
            //如果不是暴击判断是否命中
            double hitV = RoleAlgorithm.GetHitV(client);
            double dodgeV = RoleAlgorithm.GetDodgeV(enemy);
            int hit = (int)RoleAlgorithm.GetRealHitRate(hitV, dodgeV);
            //魔法闪避值(百分比)
            //int hit = (int)GetRealHitRate(GetMagicDodgePercentV(enemy));
            if (hit <= 0) //表示对方闪避掉了自己的攻击
            {
                injure = 0;
            }
        }*/

#endregion 道术攻击伤害计算
    }
}
