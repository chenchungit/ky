using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// PK之王事件对象
    /// </summary>
    public class PKKingHuoDongBaseEventObject : EventObject
    {
        public PKKingHuoDongBaseEventObject()
            : base((int)EventTypes.PKKingHuoDong)
        {

        }
    }
}
