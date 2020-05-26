using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 召唤用户事件对象
    /// </summary>
    public class CaiJiEventObject : EventObjectEx
    {
        public object Source;
        public object Target;
        public CaiJiEventObject(object source, object target)
            : base((int)GlobalEventTypes.PlayerCaiJi)
        {
            Source = source;
            Target = target;
        }
    }
}
