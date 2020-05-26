using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家地图区域事件
    /// </summary>
    public class ClientRegionEventObject : EventObject
    {
        public GameClient Client { get; private set; }
        public int EventType { get; private set; }
        public int Flag { get; private set; }
        public int AreaLuaID { get; private set; }
        public ClientRegionEventObject(GameClient client, int eventType, int flag, int areaLuaID)
            : base((int)EventTypes.ClientRegionEvent)
        {
            Client = client;
            EventType = eventType;
            Flag = flag;
            AreaLuaID = areaLuaID;
        }
    }
}
