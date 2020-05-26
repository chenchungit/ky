using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 冥想事件对象
    /// </summary>
    public class MingXiangBaseEventObject : EventObject
    {
        public MingXiangBaseEventObject()
            : base((int)EventTypes.MingXiang)
        {

        }
    }
}
