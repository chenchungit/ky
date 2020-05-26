using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家升级事件
    /// </summary>
    public class PlayerLevelupEventObject : EventObject
    {
        private GameClient player;

        public PlayerLevelupEventObject(GameClient player)
            : base((int)EventTypes.PlayerLevelup)
        {
            this.player = player;
        }

        public GameClient Player
        {
            get { return this.player; }
        }
    }
}
