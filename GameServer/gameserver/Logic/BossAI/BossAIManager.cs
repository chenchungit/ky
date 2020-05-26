using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.GameEvent;

namespace GameServer.Logic.BossAI
{
    /// <summary>
    /// Boss AI 管理器
    /// </summary>
    public class BossAIManager : IManager
    {
        #region 标准接口

        private static BossAIManager instance = new BossAIManager();

        private BossAIManager() { }

        public static BossAIManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            //向事件源注册监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterBirthOn, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterInjured, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterAttacked, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterBlooadChanged, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterLivingTime, BossAIEventListener.getInstance());

            return true;
        }

        public bool startup()
        {
            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            //向事件源删除监听器
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterBirthOn, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterInjured, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterAttacked, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterBlooadChanged, BossAIEventListener.getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterLivingTime, BossAIEventListener.getInstance());

            return true;
        }

        #endregion 标准接口
    }
}
