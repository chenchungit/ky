#define ___CC___FUCK___YOU___BB___

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System.Windows;
using Server.Data;
using GameServer.Interface;
using GameServer.Server;

namespace GameServer.Logic
{
    /// <summary>
    /// 处理攻击方与受击方的命中之差
    /// </summary>
    public class StateRate
    {
        /// <summary>
        /// 获取攻击方与受击方的“定身”状态命中之差
        /// </summary>
        public static double GetStateDingShengRate(IObject self, IObject obj, double selfBaseRate, double objBaseRate)
        {
            // 命中中跟转生等级有关
            double dSelfRealRate = 0.0;
            if (self is GameClient)
            {
                dSelfRealRate = selfBaseRate + RoleAlgorithm.GetRoleStateDingSheng(self as GameClient, selfBaseRate);
            }
            else if (self is Monster)
            {
#if ___CC___FUCK___YOU___BB___

#else
             dSelfRealRate = selfBaseRate + 0.1 * (self as Monster).MonsterInfo.ChangeLifeCount;
#endif

            }
            else
            {
                dSelfRealRate = selfBaseRate;
            }

            double dObjRealRate = 0.0;
            if (obj is GameClient)
            {
                dObjRealRate = objBaseRate + RoleAlgorithm.GetRoleStateDingSheng(obj as GameClient, objBaseRate);
            }
            else if (obj is Monster)
            {
#if ___CC___FUCK___YOU___BB___

#else
                  dObjRealRate = objBaseRate + 0.1 * (obj as Monster).MonsterInfo.ChangeLifeCount;
#endif

            }
            else if (obj is FakeRoleItem)
            {
                dObjRealRate = selfBaseRate + 0.1 * (obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
            }
            else
            {
                dObjRealRate = 0.0;
            }

            return dSelfRealRate - dObjRealRate;
        }

        /// <summary>
        /// 获取攻击方与受击方的“减速”状态命中之差
        /// </summary>
        public static double GetStateMoveSpeed(IObject self, IObject obj, double selfBaseRate, double objBaseRate)
        {
            // 命中中跟转生等级有关
            double dSelfRealRate = 0.0;
            if (self is GameClient)
            {
                dSelfRealRate = selfBaseRate + RoleAlgorithm.GetRoleStateMoveSpeed(self as GameClient, selfBaseRate);
            }
            else if (self is Monster)
            {
#if ___CC___FUCK___YOU___BB___

#else
                  dSelfRealRate = selfBaseRate + 0.1 * (self as Monster).MonsterInfo.ChangeLifeCount;
#endif

            }
            else
            {
                dSelfRealRate = selfBaseRate;
            }

            double dObjRealRate = 0.0;
            if (obj is GameClient)
            {
                dObjRealRate = objBaseRate + RoleAlgorithm.GetRoleStateMoveSpeed(obj as GameClient, objBaseRate);
            }
            else if (obj is Monster)
            {
#if ___CC___FUCK___YOU___BB___

#else
                 dObjRealRate = objBaseRate + 0.1 * (obj as Monster).MonsterInfo.ChangeLifeCount;
#endif

            }
            else if (obj is FakeRoleItem)
            {
                dObjRealRate = selfBaseRate + 0.1 * (obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
            }
            else
            {
                dObjRealRate = 0.0;
            }

            return dSelfRealRate - dObjRealRate;
        }

        /// <summary>
        /// 负面效果概率公式(击退,昏迷等)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="obj"></param>
        /// <param name="baseRate"></param>
        /// <returns></returns>
        public static double GetNegativeRate(IObject self, IObject obj, double baseRate, ExtPropIndexes extPropIndex)
        {
            int selfZhuanSheng = 0;
            if (self is GameClient)
            {
                selfZhuanSheng = (self as GameClient).ClientData.ChangeLifeCount;
                baseRate = RoleAlgorithm.GetRoleNegativeRate(self as GameClient, baseRate, extPropIndex);
            }
            else if (self is Monster)
            {
#if ___CC___FUCK___YOU___BB___

#else
                 selfZhuanSheng = (self as Monster).MonsterInfo.ChangeLifeCount;
#endif

            }

            int objZhuanSheng = 0;
            if (obj is GameClient)
            {
                objZhuanSheng = (obj as GameClient).ClientData.ChangeLifeCount;
            }
            else if (obj is Monster)
            {
#if ___CC___FUCK___YOU___BB___

#else
               objZhuanSheng = (obj as Monster).MonsterInfo.ChangeLifeCount;
#endif

                
            }
            else if (obj is FakeRoleItem)
            {
                objZhuanSheng = (obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
            }

            if (selfZhuanSheng > objZhuanSheng)
            {
                return baseRate + 0.1 * Math.Pow(selfZhuanSheng - objZhuanSheng, 2);
            }
            else
            {
                return baseRate - 0.1 * Math.Pow(selfZhuanSheng - objZhuanSheng, 2);
            }
        }

        /// <summary>
        /// 获取攻击方与受击方的“击退”状态命中之差
        /// </summary>
        public static double GetStateJiTui(IObject self, IObject obj, double selfBaseRate, double objBaseRate)
        {
            // 命中中跟转生等级有关
            double dSelfRealRate = 0.0;
            if (self is GameClient)
            {
                dSelfRealRate = selfBaseRate + RoleAlgorithm.GetRoleStateJiTui(self as GameClient, selfBaseRate);
            }
            else if (self is Monster)
            {
#if ___CC___FUCK___YOU___BB___

#else
               dSelfRealRate = selfBaseRate + 0.1 * (self as Monster).MonsterInfo.ChangeLifeCount;
#endif

            }
            else
            {
                dSelfRealRate = selfBaseRate;
            }

            double dObjRealRate = 0.0;
            if (obj is GameClient)
            {
                dObjRealRate = objBaseRate + RoleAlgorithm.GetRoleStateJiTui(obj as GameClient, objBaseRate);
            }
            else if (obj is Monster)
            {
#if ___CC___FUCK___YOU___BB___
                
#else
               dObjRealRate = objBaseRate + 0.1 * (obj as Monster).MonsterInfo.ChangeLifeCount;
#endif
            }
            else if (obj is FakeRoleItem)
            {
                dObjRealRate = selfBaseRate + 0.1 * (obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
            }
            else
            {
                dObjRealRate = 0.0;
            }

            return dSelfRealRate - dObjRealRate;
        }

        /// <summary>
        /// 获取攻击方与受击方的“昏迷”状态命中之差
        /// </summary>
        public static double GetStateHunMi(IObject self, IObject obj, double selfBaseRate, double objBaseRate)
        {
            // 命中中跟转生等级有关
            double dSelfRealRate = 0.0;
            if (self is GameClient)
            {
                dSelfRealRate = selfBaseRate + RoleAlgorithm.GetRoleStateHunMi(self as GameClient, selfBaseRate);
            }
            else if (self is Monster)
            {
#if ___CC___FUCK___YOU___BB___
              //  dSelfRealRate = selfBaseRate + 0.1 * (self as Monster).MonsterInfo.ChangeLifeCount;
#else
                dSelfRealRate = selfBaseRate + 0.1 * (self as Monster).MonsterInfo.ChangeLifeCount;
#endif
            }
            else
            {
                dSelfRealRate = selfBaseRate;
            }

            double dObjRealRate = 0.0;
            if (obj is GameClient)
            {
                dObjRealRate = objBaseRate + RoleAlgorithm.GetRoleStateHunMi(obj as GameClient, objBaseRate);
            }
            else if (obj is Monster)
            {
#if ___CC___FUCK___YOU___BB___
                //dObjRealRate = objBaseRate + 0.1 * (obj as Monster).MonsterInfo.ChangeLifeCount;
#else
                dObjRealRate = objBaseRate + 0.1 * (obj as Monster).MonsterInfo.ChangeLifeCount;
#endif
            }
            else if (obj is FakeRoleItem)
            {
                dObjRealRate = selfBaseRate + 0.1 * (obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
            }
            else
            {
                dObjRealRate = 0.0;
            }

            return dSelfRealRate - dObjRealRate;
        }
    }
}
