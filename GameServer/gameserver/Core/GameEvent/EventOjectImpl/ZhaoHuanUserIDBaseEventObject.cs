using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 召唤用户事件对象
    /// </summary>
    public class ZhaoHuanUserIDBaseEventObject : EventObject
    {
        public ZhaoHuanUserIDBaseEventObject()
            : base((int)EventTypes.ZhaoHuanUserID)
        {

        }
    }
}
