#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace GameServer.Logic.BossAI
{
    /// <summary>
    /// Boss AI 事件监听器
    /// </summary>
    public class BossAIEventListener : IEventListener
    {
        private static BossAIEventListener instance = new BossAIEventListener();

        private BossAIEventListener() { }

        public static BossAIEventListener getInstance()
        {
            return instance;
        }

        public void processEvent(EventObject eventObject)
        {
            //System.Diagnostics.Debug.WriteLine(string.Format("Boss AI Event Listener, EventType={0}", (EventTypes)eventObject.getEventType()));

            List<BossAIItem> bossAIItemList = null;
            Monster monster = null;
            GameClient gameClient = null;
            List<BossAIItem> allDeadBossAIItemList = null;

            List<BossAIItem> execBossAIItemList = new List<BossAIItem>();

            if (eventObject.getEventType() == (int)EventTypes.MonsterBirthOn)
            {
                monster = (eventObject as MonsterBirthOnEventObject).getMonster();
#if ___CC___FUCK___YOU___BB___
                int AIID = 0;
#else
                int AIID = monster.MonsterInfo.AIID;
#endif

                if (AIID > 0)
                {
                    bossAIItemList = BossAICachingMgr.FindCachingItem(AIID, (int)BossAITriggerTypes.BirthOn);
                    if (null != bossAIItemList)
                    {
                        lock (monster.TriggerMutex)
                        {
                            for (int i = 0; i < bossAIItemList.Count; i++)
                            {
                                if (!monster.CanExecBossAI(bossAIItemList[i]))
                                {
                                    continue; //如果不能执行，则跳过
                                }

                                
                                monster.RecBossAI(bossAIItemList[i]);
                                execBossAIItemList.Add(bossAIItemList[i]);
                            }
                        }
                    }
                }
            }
            else if (eventObject.getEventType() == (int)EventTypes.MonsterDead)
            {
                monster = (eventObject as MonsterDeadEventObject).getMonster();
                gameClient = (eventObject as MonsterDeadEventObject).getAttacker();

#if ___CC___FUCK___YOU___BB___
                int AIID = 0;
#else
                int AIID = monster.MonsterInfo.AIID;
#endif              

                if (AIID > 0)
                {
                    bossAIItemList = BossAICachingMgr.FindCachingItem(AIID, (int)BossAITriggerTypes.Dead);

                    if (null != bossAIItemList)
                    {
                        lock (monster.TriggerMutex)
                        {
                            for (int i = 0; i < bossAIItemList.Count; i++)
                            {
                                if (!monster.CanExecBossAI(bossAIItemList[i]))
                                {
                                    continue; //如果不能执行
                                }

                                monster.RecBossAI(bossAIItemList[i]);
                                execBossAIItemList.Add(bossAIItemList[i]);
                            }
                        }
                    }

                    allDeadBossAIItemList = BossAICachingMgr.FindCachingItem(AIID, (int)BossAITriggerTypes.DeadAll);
                    if (null != allDeadBossAIItemList)
                    {
                        for (int i = 0; i < allDeadBossAIItemList.Count; i++)
                        {
                            if (!monster.CanExecBossAI(allDeadBossAIItemList[i]))
                            {
                                continue; //如果不能执行
                            }

                            bool toContinue = false;
                            List<int> monsterIDList = (allDeadBossAIItemList[i].Condition as AllDeadCondition).MonsterIDList;
                            for (int j = 0; j < monsterIDList.Count; j++)
                            {
                                List<object> findMonsters = GameManager.MonsterMgr.FindMonsterByExtensionID(monster.CurrentCopyMapID, monsterIDList[j]);
                                if (findMonsters.Count > 0)
                                {
                                    toContinue = true;
                                    break;
                                }
                            }

                            if (toContinue)
                            {
                                continue;
                            }

                            monster.RecBossAI(allDeadBossAIItemList[i]);
                            execBossAIItemList.Add(allDeadBossAIItemList[i]);
                        }
                    }
                }
            }
            else if (eventObject.getEventType() == (int)EventTypes.MonsterInjured)
            {
                monster = (eventObject as MonsterInjuredEventObject).getMonster();
                gameClient = (eventObject as MonsterInjuredEventObject).getAttacker();

#if ___CC___FUCK___YOU___BB___
                int AIID = 0;
#else
                int AIID = monster.MonsterInfo.AIID;
#endif

                if (AIID > 0)
                {
                    bossAIItemList = BossAICachingMgr.FindCachingItem(AIID, (int)BossAITriggerTypes.Injured);

                    if (null != bossAIItemList)
                    {
                        lock (monster.TriggerMutex)
                        {
                            for (int i = 0; i < bossAIItemList.Count; i++)
                            {
                                if (!monster.CanExecBossAI(bossAIItemList[i]))
                                {
                                    continue; //如果不能执行
                                }

                                monster.RecBossAI(bossAIItemList[i]);
                                execBossAIItemList.Add(bossAIItemList[i]);
                            }
                        }
                    }
                }
            }
            else if (eventObject.getEventType() == (int)EventTypes.MonsterAttacked)
            {
                monster = (eventObject as MonsterAttackedEventObject).getMonster();

#if ___CC___FUCK___YOU___BB___
                int AIID = 0;
#else
                int AIID = monster.MonsterInfo.AIID;
#endif

                if (AIID > 0)
                {
                    bossAIItemList = BossAICachingMgr.FindCachingItem(AIID, (int)BossAITriggerTypes.Attacked);

                    if (null != bossAIItemList)
                    {
                        lock (monster.TriggerMutex)
                        {
                            for (int i = 0; i < bossAIItemList.Count; i++)
                            {
                                if (!monster.CanExecBossAI(bossAIItemList[i]))
                                {
                                    continue; //如果不能执行
                                }

                                monster.RecBossAI(bossAIItemList[i]);
                                execBossAIItemList.Add(bossAIItemList[i]);
                            }
                        }
                    }
                }
            }
            else if (eventObject.getEventType() == (int)EventTypes.MonsterBlooadChanged)
            {
                monster = (eventObject as MonsterBlooadChangedEventObject).getMonster();
#if ___CC___FUCK___YOU___BB___
                int AIID = 0;
#else
                int AIID = monster.MonsterInfo.AIID;
#endif

                if (AIID > 0)
                {
                    bossAIItemList = BossAICachingMgr.FindCachingItem(AIID, (int)BossAITriggerTypes.BloodChanged);

                    if (null != bossAIItemList)
                    {
                        lock (monster.TriggerMutex)
                        {
                            for (int i = 0; i < bossAIItemList.Count; i++)
                            {
                                if (!monster.CanExecBossAI(bossAIItemList[i]))
                                {
                                    continue; //如果不能执行
                                }
#if ___CC___FUCK___YOU___BB___
                                double currentLifeVPercent = monster.VLife / monster.XMonsterInfo.MaxHP;
#else
                double currentLifeVPercent = monster.VLife / monster.MonsterInfo.VLifeMax;
#endif

                                bool canExecActions = (currentLifeVPercent >= (bossAIItemList[i].Condition as BloodChangedCondition).MinLifePercent && currentLifeVPercent <= (bossAIItemList[i].Condition as BloodChangedCondition).MaxLifePercent);

                                if (canExecActions)
                                {
                                    monster.RecBossAI(bossAIItemList[i]);
                                    execBossAIItemList.Add(bossAIItemList[i]);
                                }
                            }
                        }
                    }
                }
            }
            else if (eventObject.getEventType() == (int)EventTypes.MonsterLivingTime)
            {
                monster = (eventObject as MonsterLivingTimeEventObject).getMonster();
#if ___CC___FUCK___YOU___BB___
                int AIID = 0;
#else
                int AIID = monster.MonsterInfo.AIID;
#endif
                if (AIID > 0)
                {
                    bossAIItemList = BossAICachingMgr.FindCachingItem(AIID, (int)BossAITriggerTypes.LivingTime);

                    if (null != bossAIItemList)
                    {
                        lock (monster.TriggerMutex)
                        {
                            for (int i = 0; i < bossAIItemList.Count; i++)
                            {
                                if (!monster.CanExecBossAI(bossAIItemList[i]))
                                {
                                    continue; //如果不能执行
                                }

                                bool canExecActions = monster.GetMonsterLivingTicks() >= ((bossAIItemList[i].Condition as LivingTimeCondition).LivingMinutes * 60 * 1000);
                                if (canExecActions)
                                {
                                    monster.RecBossAI(bossAIItemList[i]);
                                    execBossAIItemList.Add(bossAIItemList[i]);
                                }
                            }
                        }
                    }
                }
            }

            if (null != execBossAIItemList)
            {
                for (int i = 0; i < execBossAIItemList.Count; i++)
                {
                    BossAIItem bossAIItem = execBossAIItemList[i];
                    List<MagicActionItem> magicActionItemList = null;
                    if (GameManager.SystemMagicActionMgr.BossAIActionsDict.TryGetValue(bossAIItem.ID, out magicActionItemList) && null != magicActionItemList)
                    {
                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(monster, gameClient, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams);

                            //向地图中的用户广播特殊提示信息
                            if (!string.IsNullOrEmpty(bossAIItem.Desc))
                            {
                                GameManager.ClientMgr.BroadSpecialHintText(monster.CurrentMapCode, monster.CurrentCopyMapID, bossAIItem.Desc);
                            }
                        }
                    }
                }
            }
        }
    }
}
