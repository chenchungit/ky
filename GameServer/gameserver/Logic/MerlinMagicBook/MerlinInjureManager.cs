using GameServer.Interface;
using GameServer.Logic.JingJiChang;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.MerlinMagicBook
{
    /// <summary>
    /// 攻击中附带的其他效果与伤害管理器(如冰冻、麻痹、减速、重击等) [XSea 2015/6/26]
    /// </summary>
    public class MerlinInjureManager
    {
        #region 计算特殊伤害/触发特殊伤害效果
        /// <summary>
        /// 计算特殊伤害/触发特殊伤害效果
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="defender">防守者</param>
        /// <param name="nBaseInjure">已造成伤害基数</param>
        /// <param name="eref">伤害类型</param>
        /// <returns>伤害值</returns>
        public int CalcMerlinInjure(IObject attacker, IObject defender, int nBaseInjure, ref EMerlinSecretAttrType eref)
        {
            eref = EMerlinSecretAttrType.EMSAT_None; // 默认为无伤害

            try
            {
                //  不是玩家或者机器人  否掉
                if (!(attacker is GameClient) && !(attacker is Robot))
                    return 0;

                //  被攻击者不是玩家或机器人 或者怪物 否掉
                if (!(defender is GameClient) && !(defender is Robot) && !(defender is Monster))
                    return 0;

                // 没有造成伤害
                if (nBaseInjure <= 0)
                    return 0;

                // 获取伤害类型
                eref = GetMerlinInjureType(attacker);
                if (eref == EMerlinSecretAttrType.EMSAT_None)
                    return 0;

                // 触发伤害效果
                int nInjure = TriggerEffect(attacker, defender, nBaseInjure, eref); // 造成的伤害

                return nInjure;
            }
            catch (Exception ex)
            {
                if (attacker is GameClient)
                {
                    GameClient client = attacker as GameClient;
                    if (null != client)
                        DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                }
            }

            return 0;
        }
        #endregion

        #region 获取伤害类型
        /// <summary>
        /// 获取伤害类型
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <returns></returns>
        private EMerlinSecretAttrType GetMerlinInjureType(IObject attacker)
        {
            try
            {
                // 攻击者的概率 百分比
                double dSpeedDownRate = GetMerlinInjurePercent(attacker, EMerlinSecretAttrType.EMSAT_SpeedDownP);
                double dFrozenRate = GetMerlinInjurePercent(attacker, EMerlinSecretAttrType.EMSAT_FrozenP);
                double dBlowRate = GetMerlinInjurePercent(attacker, EMerlinSecretAttrType.EMSAT_BlowP);
                double dPalsyRate = GetMerlinInjurePercent(attacker, EMerlinSecretAttrType.EMSAT_PalsyP);

                // 按优先顺序获取，减速、冰冻、重击、麻痹
                double[] rateArr = { dSpeedDownRate, dFrozenRate, dBlowRate, dPalsyRate };
                int index = RoleAlgorithm.GetRateIndexPercent(rateArr); // 总几率100
                switch (index)
                {
                    case 0: // 减速
                        return EMerlinSecretAttrType.EMSAT_SpeedDownP;
                        break;
                    case 1: // 冰冻
                        return EMerlinSecretAttrType.EMSAT_FrozenP;
                        break;
                    case 2: // 重击
                        return EMerlinSecretAttrType.EMSAT_BlowP;
                        break;
                    case 3: // 麻痹
                        return EMerlinSecretAttrType.EMSAT_PalsyP;
                        break;
                    default: // 无
                        return EMerlinSecretAttrType.EMSAT_None;
                        break;
                }
            }
            catch (Exception ex)
            {
                if (attacker is GameClient)
                {
                    GameClient client = attacker as GameClient;
                    if(null != client)
                        DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                }
            }

            return EMerlinSecretAttrType.EMSAT_None;
        }
        #endregion

        #region 获取伤害概率
        /// <summary>
        /// 获取伤害概率
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="eType">伤害类型</param>
        /// <returns></returns>
        public double GetMerlinInjurePercent(IObject attacker, EMerlinSecretAttrType eType)
        {
            double val = 0.0;

            try
            {
                // 根据类型获取角色伤害概率
                switch (eType)
                {
                    case EMerlinSecretAttrType.EMSAT_FrozenP: // 冰冻
                        val += RoleAlgorithm.GetFrozenPercent(attacker);
                        break;
                    case EMerlinSecretAttrType.EMSAT_SpeedDownP: // 减速
                        val += RoleAlgorithm.GetSpeedDownPercent(attacker);
                        break;
                    case EMerlinSecretAttrType.EMSAT_PalsyP: // 麻痹
                        val += RoleAlgorithm.GetPalsyPercent(attacker);
                        break;
                    case EMerlinSecretAttrType.EMSAT_BlowP: // 重击
                        val += RoleAlgorithm.GetBlowPercent(attacker);
                        break;
                }
                return val;
            }
            catch (Exception ex)
            {
                if (attacker is GameClient)
                {
                    GameClient client = attacker as GameClient;
                    if (null != client)
                        DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                }
            }
            return val;
        }
        #endregion

        #region 触发伤害效果
        /// <summary>
        /// 触发伤害效果
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="defender">防守者</param>
        /// <param name="nBaseInjure">已造成伤害基数</param>
        /// <param name="eType">伤害类型</param>
        /// <returns>伤害值</returns>
        private int TriggerEffect(IObject attacker, IObject defender, int nBaseInjure, EMerlinSecretAttrType eType)
        {
            int nInjure = 0; // 造成的伤害

            try
            {
                // 根据类型计算伤害
                switch (eType)
                {
                    case EMerlinSecretAttrType.EMSAT_SpeedDownP: // 减速
                        // 效果1：触发后追加本次伤害的50%
                        nInjure = (int)(nBaseInjure * 0.5);

                        // 效果2：触发后100%的几率降低目标30%的移动速度4秒
                        if (Global.GetRandomNumber(0, 10001) <= 10000)
                        {
                            double[] param = { 0.3, 4.0 }; // 参数列表
                            MagicAction.ProcessAction(attacker, defender, MagicActionIDs.MU_ADD_MOVE_SPEED_DOWN, param);
                        }
                        break;
                    case EMerlinSecretAttrType.EMSAT_FrozenP: // 冰冻
                        // 效果1：触发后追加本次伤害的50%
                        nInjure = (int)(nBaseInjure * 0.5);

                        // 效果2：触发后100%的几率冻结目标2秒
                        if (Global.GetRandomNumber(0, 10001) <= 10000)
                        {
                            double[] param = { 0.99, 2.0 }; // 参数列表
                            MagicAction.ProcessAction(attacker, defender, MagicActionIDs.MU_ADD_FROZEN, param);
                        }
                        break;
                    case EMerlinSecretAttrType.EMSAT_BlowP: // 重击
                        // 效果1：触发后追加本次伤害的100%
                        nInjure = nBaseInjure;
                        break;
                    case EMerlinSecretAttrType.EMSAT_PalsyP: // 麻痹
                        // 效果1：触发后追加本次伤害的50%
                        nInjure = (int)(nBaseInjure * 0.5);

                        // 效果2：触发后100%的几率另目标晕眩1秒
                        if (Global.GetRandomNumber(0, 10001) <= 10000)
                        {
                            double[] param = { 0.99, 1.0 }; // 参数列表
                            MagicAction.ProcessAction(attacker, defender, MagicActionIDs.MU_ADD_PALSY, param);
                        }
                        break;
                    default: // 默认无伤害
                        return 0;
                        break;
                }

                return nInjure;
            }
            catch (Exception ex)
            {
                if (attacker is GameClient)
                {
                    GameClient client = attacker as GameClient;
                    if (null != client)
                        DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                }
            }
            return nInjure;
        }
        #endregion
    }
}
