using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    public class PlayerLeaveFuBenEventObject : EventObject
    {
        private GameClient player;

        public PlayerLeaveFuBenEventObject(GameClient player)
            : base((int)EventTypes.PlayerLeaveFuBen)
        {
            this.player = player;
        }

        public GameClient getPlayer()
        {
            return this.player;
        }
    }
}
