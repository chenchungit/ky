using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 摊位事件对象
    /// </summary>
    public class TanWeiGouMaiBaseEventObject : EventObject
    {
        public TanWeiGouMaiBaseEventObject()
            : base((int)EventTypes.TanWeiGouMai)
        {

        }
    }
}
