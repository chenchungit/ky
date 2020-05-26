using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.JingJiChang;
using GameServer.Interface;
using Server.Data;
using GameServer.Logic.FluorescentGem;

namespace GameServer.Logic.ElementsAttack
{
    /// <summary>
    /// 元素攻击管理器 [XSea 2015/5/28]
    /// </summary>
    public class ElementsAttackManager
    {
        // 是否记录元素伤害，方便测试统计
        public const bool LogElementInjure = false;
        /**/public static readonly string[] ElementAttrName = { "unknown", "火伤", "水伤", "雷伤", "土伤", "冰伤", "风伤" };

        public string CalcElementInjureLog(IObject attacker, IObject defender, double injurePercent)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = (int)EElementDamageType.EEDT_Fire; i <= (int)EElementDamageType.EEDT_Wind; ++i)
            {
                double nElementInjure = CalcElementDamage(attacker, defender, (EElementDamageType)i) * injurePercent; // 造成的元素伤害
                sb.AppendFormat("{0}:{1}  ", ElementAttrName[i], nElementInjure);
            }

            return sb.ToString();
        }


        #region 计算全部元素伤害
        /// <summary>
        /// 计算元素伤害/触发元素伤害效果
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="defender">防守者</param>
        /// <returns>元素伤害</returns>
        public int CalcAllElementDamage(IObject attacker, IObject defender)
        {
            // 系统版本开放检查
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.FluorescentGem))
                return 0;

            //  不是玩家或者机器人  否掉
            if (!(attacker is GameClient) && !(attacker is Robot))
                return 0;

            //  被攻击者不是玩家或机器人 或者怪物 否掉
            if (!(defender is GameClient) && !(defender is Robot) && !(defender is Monster))
                return 0;

            // 计算6种元素伤害
            int nElementInjure = 0;
            for (int i = (int)EElementDamageType.EEDT_Fire; i <= (int)EElementDamageType.EEDT_Wind; ++i)
                nElementInjure += CalcElementDamage(attacker, defender, (EElementDamageType)i); // 造成的元素伤害

            return nElementInjure;
        }
        #endregion

        #region 计算元素伤害
        /// <summary>
        /// 计算元素伤害
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="defender">防守者</param>
        /// <param name="eEDT">元素伤害类型</param>
        /// <returns>元素伤害</returns>
        private int CalcElementDamage(IObject attacker, IObject defender, EElementDamageType eEDT)
        {
            int nElementInjure = 0; // 造成的元素伤害
            
            // 攻击者的穿透百分比
            double AtkPenetration = GetElementDamagePenetration(attacker, eEDT);

            // 防守者的抗性百分比
            double DefPenetration = GetDeElementDamagePenetration(defender, eEDT);

            // 1 + 元素穿透 - 元素抗性
            double rate = 1 + (AtkPenetration - DefPenetration); 
            
            // 穿透抗性比
            rate = Global.GMax(0.01, rate); // 最小1%
            rate = Global.GMin(1, rate); // 最大100%

            // 获取元素固定伤害 [XSea 2015/5/29]
            nElementInjure = GetElementAttack(attacker, eEDT);

            //	最终元素伤害 = 元素伤害 *（1+攻方元素穿透-防方元素抗性）
            nElementInjure = (int)(nElementInjure * rate);

            if (attacker.ObjectType == ObjectTypes.OT_CLIENT)
            {
                nElementInjure = (int)(nElementInjure * (1 + RoleAlgorithm.GetExtPropValue(attacker as GameClient, ExtPropIndexes.ElementInjurePercent)));
            }

            return nElementInjure;
        }
        #endregion
        
        #region 获取元素伤害穿透
        /// <summary>
        /// 获取元素伤害穿透
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="eEDT">元素伤害类型</param>
        /// <returns></returns>
        public double GetElementDamagePenetration(IObject attacker, EElementDamageType eEDT)
        {
            double val = 0.0;
            // 目前只有角色与竞技场机器人才有穿透百分比
            if (attacker is GameClient)
            {
                GameClient attackClient = attacker as GameClient;
                // 判空
                if (null == attackClient)
                    return val;

                // 根据元素类型获取角色伤害穿透
                switch (eEDT)
                {
                    case EElementDamageType.EEDT_Water: // 水
                        val += attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.WaterPenetration];
                        val += attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.WaterPenetration);
                        break;
                    case EElementDamageType.EEDT_Fire: // 火
                        val += attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.FirePenetration];
                        val += attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.FirePenetration);
                        break;
                    case EElementDamageType.EEDT_Wind: // 风
                        val += attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.WindPenetration];
                        val += attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.WindPenetration);
                        break;
                    case EElementDamageType.EEDT_Soil: // 土
                        val += attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.SoilPenetration];
                        val += attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.SoilPenetration);
                        break;
                    case EElementDamageType.EEDT_Ice: // 冰
                        val += attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.IcePenetration];
                        val += attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IcePenetration);
                        break;
                    case EElementDamageType.EEDT_Lightning: // 雷
                        val += attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.LightningPenetration];
                        val += attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.LightningPenetration);
                        break;
                 }
            }
            else if (attacker is Robot) // 机器人穿透百分比
            {
                Robot attackRobot = attacker as Robot;
                // 判空
                if (null == attackRobot)
                    return val;

                // 根据元素类型获取机器人伤害穿透
                switch (eEDT)
                {
                    case EElementDamageType.EEDT_Water: // 水
                        val = attackRobot.WaterPenetration;
                        break;
                    case EElementDamageType.EEDT_Fire: // 火
                        val = attackRobot.FirePenetration;
                        break;
                    case EElementDamageType.EEDT_Wind: // 风
                        val = attackRobot.WindPenetration;
                        break;
                    case EElementDamageType.EEDT_Soil: // 土
                        val = attackRobot.SoilPenetration;
                        break;
                    case EElementDamageType.EEDT_Ice: // 冰
                        val = attackRobot.IcePenetration;
                        break;
                    case EElementDamageType.EEDT_Lightning: // 雷
                        val = attackRobot.LightningPenetration;
                        break;
                 }
            }
            return Math.Max(val, 0);
        }
        #endregion
        
        #region 获取元素伤害抗性
        /// <summary>
        /// 获取元素伤害抗性
        /// </summary>
        /// <param name="defender">防守者</param>
        /// <param name="eEDT">元素伤害类型</param>
        /// <returns></returns>
        public double GetDeElementDamagePenetration(IObject defender, EElementDamageType eEDT)
        {
            double val = 0.0;

            // 目前只有角色与竞技场机器人才有抵抗百分比
            if (defender is GameClient)
            {
                GameClient defenderClient = defender as GameClient;
                // 判空
                if (null == defenderClient)
                    return val;

                // 根据元素类型获取角色元素伤害抗性
                switch (eEDT)
                {
                    case EElementDamageType.EEDT_Water: // 水
                        val += defenderClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DeWaterPenetration];
                        val += defenderClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeWaterPenetration);
                        break;
                    case EElementDamageType.EEDT_Fire: // 火
                        val += defenderClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DeFirePenetration];
                        val += defenderClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeFirePenetration);
                        break;
                    case EElementDamageType.EEDT_Wind: // 风
                        val += defenderClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DeWindPenetration];
                        val += defenderClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeWindPenetration);
                        break;
                    case EElementDamageType.EEDT_Soil: // 土
                        val += defenderClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DeSoilPenetration];
                        val += defenderClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeSoilPenetration);
                        break;
                    case EElementDamageType.EEDT_Ice: // 冰
                        val += defenderClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DeIcePenetration];
                        val += defenderClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeIcePenetration);
                        break;
                    case EElementDamageType.EEDT_Lightning: // 雷
                        val += defenderClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.DeLightningPenetration];
                        val += defenderClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.DeLightningPenetration);
                        break;
                }
            }
            else if (defender is Robot) // 对方机器人抵抗百分比
            {
                Robot defenderRobot = defender as Robot;
                // 判空
                if (null == defenderRobot)
                    return val;

                // 根据元素类型获取机器人元素伤害抗性
                switch (eEDT)
                {
                    case EElementDamageType.EEDT_Water: // 水
                        val = defenderRobot.DeWaterPenetration;
                        break;
                    case EElementDamageType.EEDT_Fire: // 火
                        val = defenderRobot.DeFirePenetration;
                        break;
                    case EElementDamageType.EEDT_Wind: // 风
                        val = defenderRobot.DeWindPenetration;
                        break;
                    case EElementDamageType.EEDT_Soil: // 土
                        val = defenderRobot.DeSoilPenetration;
                        break;
                    case EElementDamageType.EEDT_Ice: // 冰
                        val = defenderRobot.DeIcePenetration;
                        break;
                    case EElementDamageType.EEDT_Lightning: // 雷
                        val = defenderRobot.DeLightningPenetration;
                        break;
                }
            }
            return Math.Max(val, 0);
        }
        #endregion

        #region 获取元素固定伤害
        /// <summary>
        /// 获取元素固定伤害
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="eEDT">元素伤害类型</param>
        /// <returns></returns>
        public int GetElementAttack(IObject attacker, EElementDamageType eEDT)
        {
            int val = 0;
            // 目前只有角色与竞技场机器人才有元素固定伤害
            if (attacker is GameClient)
            {
                GameClient attackClient = attacker as GameClient;
                // 判空
                if (null == attackClient)
                    return val;

                // 根据元素类型获取角色固定伤害
                switch (eEDT)
                {
                    case EElementDamageType.EEDT_Water: // 水
                        val += (int)attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.WaterAttack];
                        val += (int)attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.WaterAttack);
                        break;
                    case EElementDamageType.EEDT_Fire: // 火
                        val += (int)attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.FireAttack];
                        val += (int)attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.FireAttack);
                        break;
                    case EElementDamageType.EEDT_Wind: // 风
                        val += (int)attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.WindAttack];
                        val += (int)attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.WindAttack);
                        break;
                    case EElementDamageType.EEDT_Soil: // 土
                        val += (int)attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.SoilAttack];
                        val += (int)attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.SoilAttack);
                        break;
                    case EElementDamageType.EEDT_Ice: // 冰
                        val += (int)attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.IceAttack];
                        val += (int)attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.IceAttack);
                        break;
                    case EElementDamageType.EEDT_Lightning: // 雷
                        val += (int)attackClient.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.LightningAttack];
                        val += (int)attackClient.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.LightningAttack);
                        break;
                }
            }
            else if (attacker is Robot) // 机器人元素固定伤害
            {
                Robot attackRobot = attacker as Robot;
                // 判空
                if (null == attackRobot)
                    return val;

                // 根据元素类型获取机器人元素固定伤害
                switch (eEDT)
                {
                    case EElementDamageType.EEDT_Water: // 水
                        val = attackRobot.WaterAttack;
                        break;
                    case EElementDamageType.EEDT_Fire: // 火
                        val = attackRobot.FireAttack;
                        break;
                    case EElementDamageType.EEDT_Wind: // 风
                        val = attackRobot.WindAttack;
                        break;
                    case EElementDamageType.EEDT_Soil: // 土
                        val = attackRobot.SoilAttack;
                        break;
                    case EElementDamageType.EEDT_Ice: // 冰
                        val = attackRobot.IceAttack;
                        break;
                    case EElementDamageType.EEDT_Lightning: // 雷
                        val = attackRobot.LightningAttack;
                        break;
                }
            }
            return Math.Max(val, 0);
        }
        #endregion
            
        #region 创建竞技场机器人时获取机器人元素属性
        /// <summary>
        /// 创建竞技场机器人时获取机器人元素属性 [XSea 2015/6/1]
        /// </summary>
        public double GetJJCRobotExtProps(int nIndex, double[] extProps)
        {
            // 超出数组上限 给默认值
            if (nIndex > extProps.Length - 1)
                return 0.0;
            else // 有值 直接给
                return extProps[nIndex];
        }
        #endregion
    }
}
