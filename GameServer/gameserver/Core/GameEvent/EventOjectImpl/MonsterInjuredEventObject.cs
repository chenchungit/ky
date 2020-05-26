using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 怪物第一次被攻击后的触发事件对象
    /// </summary>
    public class MonsterInjuredEventObject : EventObject
    {
        private Monster monster;
        private GameClient attacker;

        public MonsterInjuredEventObject(Monster monster, GameClient attacker)
            : base((int)EventTypes.MonsterInjured)
        {
            this.monster = monster;
            this.attacker = attacker;
        }

        public Monster getMonster()
        {
            return monster;
        }

        public GameClient getAttacker()
        {
            return attacker;
        }
    }
}
