using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class PlayerInitGameEventObject : EventObject
    {
        private GameClient player;

        public PlayerInitGameEventObject(GameClient player)
            : base((int)EventTypes.PlayerInitGame)
        {
            this.player = player;
        }

        public GameClient getPlayer()
        {
            return this.player;
        }
    }
}
