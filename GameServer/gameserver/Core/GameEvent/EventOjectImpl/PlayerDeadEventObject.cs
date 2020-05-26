using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    public enum PlayerDeadEventTypes
    {
        ByMonster,
        ByRole,
    }

    /// <summary>
    /// 玩家死亡事件
    /// </summary>
    public class PlayerDeadEventObject : EventObject
    {
        private GameClient attackerRole;

        private Monster attacker;

        private GameClient player;

        public PlayerDeadEventTypes Type;

        public PlayerDeadEventObject(GameClient player, Monster attacker)
            : base((int)EventTypes.PlayerDead)
        {
            this.player = player;
            this.attacker = attacker;
            Type = PlayerDeadEventTypes.ByMonster;
        }

        public PlayerDeadEventObject(GameClient player, GameClient attacker)
            : base((int)EventTypes.PlayerDead)
        {
            this.player = player;
            this.attackerRole = attacker;
            Type = PlayerDeadEventTypes.ByRole;
        }

        public Monster getAttacker()
        {
            return attacker;
        }

        public GameClient getPlayer()
        {
            return player;
        }

        public GameClient getAttackerRole()
        {
            return attackerRole;
        }
    }
}
