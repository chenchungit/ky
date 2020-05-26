using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 竞技场副本结束事件（时间到）
    /// </summary>
    public class JingJiFuBenEndForTimeEventObject : EventObject
    {
        private int fubenId;

        public JingJiFuBenEndForTimeEventObject(int fubenId)
            : base(/*(int)EventTypes.JingJiFuBenEndForTime*/1)
        {
            this.fubenId = fubenId;
        }

        public int getFuBenId()
        {
            return fubenId;
        }
    }
}
