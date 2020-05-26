using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 怪物攻击事件对象
    /// </summary>
    public class MonsterAttackedEventObject : EventObject
    {
        private Monster monster;
        private int enemy;

        public MonsterAttackedEventObject(Monster monster, int enemy)
            : base((int)EventTypes.MonsterAttacked)
        {
            this.monster = monster;
            this.enemy = enemy;
        }

        public Monster getMonster()
        {
            return monster;
        }

        public int getEnemy()
        {
            return enemy;
        }
    }
}
