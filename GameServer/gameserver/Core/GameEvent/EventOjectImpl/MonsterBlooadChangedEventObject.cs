using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 怪物血量变化事件对象
    /// </summary>
    public class MonsterBlooadChangedEventObject : EventObject
    {
        private Monster monster;
        private GameClient client;

        public MonsterBlooadChangedEventObject(Monster monster, GameClient client = null)
            : base((int)EventTypes.MonsterBlooadChanged)
        {
            this.monster = monster;
            this.client = client;
        }

        public Monster getMonster()
        {
            return monster;
        }

        //[bing] 改造一下 可以返回攻击者 结婚副本需要用
        public GameClient getGameClient()
        {
            return client;
        }
    }
}
