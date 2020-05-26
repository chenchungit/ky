using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 果实使用时的属性验证
    /// </summary>
    class UseFruitVerify
    {
        /// <summary>
        /// 得到果实添加某属性的上限值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="strPropName"></param>
        public static int GetFruitAddPropLimit(GameClient client, string strPropName)
        {
            ChangeLifeAddPointInfo tmpChangeAddPointInfo = Data.ChangeLifeAddPointInfoList[client.ClientData.ChangeLifeCount];
            if ("Strength" == strPropName)
            {
                return tmpChangeAddPointInfo.nStrLimit;
            }
            else if ("Dexterity" == strPropName)
            {
                return tmpChangeAddPointInfo.nDexLimit;
            }
            else if ("Intelligence" == strPropName)
            {
                return tmpChangeAddPointInfo.nIntLimit;
            }
            else if ("Constitution" == strPropName)
            {
                return tmpChangeAddPointInfo.nConLimit;
            }

            return 0;
        }

        /// <summary>
        /// 检验待添加的属性数值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="strPropName"></param>
        public static int AddValueVerify(GameClient client, int nOld, int nPropLimit, int nAddValue)
        {
            if (nOld < nPropLimit)
            {
                if (nOld + nAddValue > nPropLimit)
                {
                    nAddValue = nPropLimit - nOld;
                }
            }
            else
            {
                nAddValue = 0;
            }

            return nAddValue;
        }
    }
}
