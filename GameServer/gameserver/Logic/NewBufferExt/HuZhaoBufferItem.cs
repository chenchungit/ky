using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Interface;

namespace GameServer.Logic.NewBufferExt
{
    /// <summary>
    /// 护罩buffer项
    /// </summary>
    public class HuZhaoBufferItem : IBufferItem
    {
        /// <summary>
        /// 单次蛋受伤的伤害值
        /// </summary>
        public int InjuredV = 0;

        /// <summary>
        /// 蛋的总的血量值
        /// </summary>
        public int MaxLifeV = 0;

        /// <summary>
        /// 血量回复加速的倍数
        /// </summary>
        public double RecoverLifePercent = 1.0;
    }
}
