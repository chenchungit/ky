using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Interface;
using GameServer.Core.Executor;

namespace GameServer.Logic.NewBufferExt
{
    /// <summary>
    /// 延迟持续伤害buffer数据项
    /// </summary>
    public class DelayInjuredBufferItem : IBufferItem
    {
        /// <summary>
        /// 被攻击者
        /// </summary>
        public int ObjectID = 0;

        /// <summary>
        /// 间隔时间(秒)
        /// </summary>
        public int TimeSlotSecs = 0;

        /// <summary>
        /// 伤害值
        /// </summary>
        public int SubLifeV = 0;

        /// <summary>
        /// 开始计算时间
        /// </summary>
        public long StartSubLifeNoShowTicks = TimeUtil.NOW();
    }
}
