using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 怪物死亡事件(人打怪，怪死了)
    /// </summary>
    public class MonsterDeadEventObject : EventObject
    {
        private Monster monster;
        private GameClient attacker;

        public MonsterDeadEventObject(Monster monster, GameClient attacker)
            : base((int)EventTypes.MonsterDead)
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