using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 怪物死亡事件(怪打怪，怪死了)
    /// </summary>
    public class MonsterToMonsterDeadEventObject : EventObject
    {
        private Monster monsterAttack;
        private Monster monster;

        public MonsterToMonsterDeadEventObject(Monster monster, Monster monsterAttack)
            : base((int)EventTypes.MonsterToMonsterDead)
        {
            this.monster = monster;
            this.monsterAttack = monsterAttack;
        }

        public Monster getMonster()
        {
            return monster;
        }

        public Monster getMonsterAttack()
        {
            return monsterAttack;
        }
    }
}

