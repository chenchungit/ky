using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 怪物出生事件对象
    /// </summary>
    public class MonsterBirthOnEventObject : EventObject
    {
        private Monster monster;

        public MonsterBirthOnEventObject(Monster monster)
            : base((int)EventTypes.MonsterBirthOn)
        {
            this.monster = monster;
        }

        public Monster getMonster()
        {
            return monster;
        }
    }
}
