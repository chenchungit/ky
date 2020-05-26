using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 恶魔广场事件对象
    /// </summary>
    public class EMoGuangChangBaseEventObject : EventObject
    {
        public EMoGuangChangBaseEventObject()
            : base((int)EventTypes.EMoGuangChang)
        {

        }
    }
}
