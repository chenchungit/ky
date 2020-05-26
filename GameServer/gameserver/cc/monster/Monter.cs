
using GameServer.Interface;
using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Tmsk.Contract;

namespace GameServer.cc.monster
{
    public delegate void MoveToEventHandler(Monster monster);

    public delegate void CoordinateEventHandler(Monster monster);

    //定义精灵内部改动作通知事件
    public delegate void SpriteChangeActionEventHandler(object sender, SpriteChangeActionEventArgs e);
    public class Monster
    {
        private MonsterData m_MonsterData = new MonsterData();

        public Monster(MonsterData _MonsterData)
        {
            m_MonsterData = _MonsterData;
        }
    }
}
