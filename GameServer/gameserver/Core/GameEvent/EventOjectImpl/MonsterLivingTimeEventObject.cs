using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 怪物存活事件对象(每隔1分钟触发一次)
    /// </summary>
    public class MonsterLivingTimeEventObject : EventObject
    {
        private Monster monster;

        public MonsterLivingTimeEventObject(Monster monster)
            : base((int)EventTypes.MonsterLivingTime)
        {
            this.monster = monster;
        }

        public Monster getMonster()
        {
            return monster;
        }
    }
}
