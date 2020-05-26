using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 野外boss事件对象
    /// </summary>
    public class YeWaiBossBaseEventObject : EventObject
    {
        public YeWaiBossBaseEventObject()
            : base((int)EventTypes.YeWaiBoss)
        {

        }
    }
}
