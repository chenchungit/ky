using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Interface;
using GameServer.Server;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 执行项定义
    /// </summary>
    public class MagicHelperItem
    {
        /// <summary>
        /// 公式ID
        /// </summary>
        public MagicActionIDs MagicActionID
        {
            get;
            set;
        }

        /// <summary>
        /// 公式参数
        /// </summary>
        public double[] MagicActionParams
        {
            get;
            set;
        }

        /// <summary>
        /// 起始时间
        /// </summary>
        public long StartedTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 上次执行时间
        /// </summary>
        public long LastTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 执行次数
        /// </summary>
        public int ExecutedNum
        {
            get;
            set;
        }

        /// <summary>
        /// 目标ID
        /// </summary>
        public int ObjectID
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 技能辅助选项
    /// </summary>
    public class SpriteMagicHelper
    {
        #region 基础数据

        /// <summary>
        /// 技能辅助项词典
        /// </summary>
        private Dictionary<MagicActionIDs, MagicHelperItem> _MagicHelperDict = new Dictionary<MagicActionIDs, MagicHelperItem>();

        /// <summary>
        /// 添加技能辅助项
        /// </summary>
        /// <param name="magicActionID"></param>
        /// <param name="magicActionParams"></param>
        public void AddMagicHelper(MagicActionIDs magicActionID, double[] magicActionParams, int objID)
        {
            MagicHelperItem magicHelperItem = new MagicHelperItem()
            {
                MagicActionID = magicActionID,
                MagicActionParams = magicActionParams,
                StartedTicks = TimeUtil.NOW() * 10000,
                LastTicks = 0,
                ExecutedNum = 0,
                ObjectID = objID,
            };

            lock (_MagicHelperDict)
            {
                _MagicHelperDict[magicActionID] = magicHelperItem;
            }
        }

        /// <summary>
        /// 删除技能辅助项
        /// </summary>
        /// <param name="magicActionID"></param>
        /// <param name="magicActionParams"></param>
        public void RemoveMagicHelper(MagicActionIDs magicActionID)
        {
            lock (_MagicHelperDict)
            {
                if (_MagicHelperDict.ContainsKey(magicActionID))
                {
                    _MagicHelperDict.Remove(magicActionID);
                }
            }
        }

        #endregion 基础数据

        #region 执行相关辅助项

        /// <summary>
        /// 是否可以执行选项
        /// </summary>
        /// <param name="magicHelperItem"></param>
        /// <returns></returns>
        private bool CanExecuteItem(MagicHelperItem magicHelperItem, int effectSecs, int maxNum)
        {
            long nowTicks = TimeUtil.NOW();
            long ticks = magicHelperItem.StartedTicks + ((long)effectSecs * 1000);

            if (maxNum <= 0)
            {
                //判断是否超过了时间
                if (nowTicks >= ticks)
                {
                    lock (_MagicHelperDict)
                    {
                        _MagicHelperDict.Remove(magicHelperItem.MagicActionID);
                    }

                    return false;
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("CanExecuteItem, {0}", nowTicks - ticks);
                    return true;
                }
            }

            //判断是否超过了次数
            if (magicHelperItem.ExecutedNum >= maxNum)
            {
                lock (_MagicHelperDict)
                {
                    _MagicHelperDict.Remove(magicHelperItem.MagicActionID);
                }

                return false;
            }

            long ticksSlot = ((effectSecs / maxNum) * 1000 * 10000);

            //判断是否超过了时间
            if (nowTicks - magicHelperItem.LastTicks < ticksSlot)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 提高药品使用效果	提高的百分比
        /// </summary>
        /// <returns></returns>
        public double GetAddDrugEffect()
        {
            double percent = 1.0;
            lock (_MagicHelperDict)
            {
                MagicHelperItem magicHelperItem = null;
                if (_MagicHelperDict.TryGetValue(MagicActionIDs.FOREVER_ADDDRUGEFFECT, out magicHelperItem))
                {
                    percent = (magicHelperItem.MagicActionParams[0] / 100.0);
                }
            }

            return percent;
        }

        /// <summary>
        /// 获取当前是否减速的百分比
        /// </summary>
        /// <returns></returns>
        public double GetMoveSlow()
        {
            double percent = 0.0;

            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_SLOW, out magicHelperItem);
            }

            if (null == magicHelperItem) return percent;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return percent;
            percent = (magicHelperItem.MagicActionParams[0] / 100.0);
            return percent;
        }

        /// <summary>
        /// 获取当前是否被冰冻
        /// </summary>
        /// <returns></returns>
        public bool GetFreeze()
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_FREEZE, out magicHelperItem);
            }

            if (null == magicHelperItem) return false;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[0], 0)) return false;
            return true;
        }

        /// <summary>
        /// 将对敌人的伤害转换为自己的血的百分比
        /// </summary>
        /// <returns></returns>
        public double GetInjure2Life()
        {
            double percent = 0.0;

            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_INJUE2LIFE, out magicHelperItem);
            }

            if (null == magicHelperItem) return percent;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return percent;
            percent = (magicHelperItem.MagicActionParams[0] / 100.0);
            return percent;
        }

        /// <summary>
        /// 持续减少伤害的值
        /// </summary>
        /// <returns></returns>
        public double GetSubInjure()
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_SUBINJUE, out magicHelperItem);
            }

            if (null == magicHelperItem) return 0.0;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return 0.0;
            return magicHelperItem.MagicActionParams[0];
        }

        /// <summary>
        /// 持续增加伤害的值
        /// </summary>
        /// <returns></returns>
        public double GetAddInjure()
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_ADDINJUE, out magicHelperItem);
            }

            if (null == magicHelperItem) return 0.0;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return 0.0;
            return magicHelperItem.MagicActionParams[0];
        }

        /// <summary>
        /// 持续减少伤害的的百分比
        /// </summary>
        /// <returns></returns>
        public double GetSubInjure1()
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_SUBINJUE1, out magicHelperItem);
            }

            if (null == magicHelperItem) return 0.0;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return 0.0;
            return magicHelperItem.MagicActionParams[0];
        }

        /// <summary>
        /// 持续增加伤害的百分比
        /// </summary>
        /// <returns></returns>
        public double GetAddInjure1()
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_ADDINJUE1, out magicHelperItem);
            }

            if (null == magicHelperItem) return 0.0;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return 0.0;
            return magicHelperItem.MagicActionParams[0];
        }

        /// <summary>
        /// 将伤害转化为魔法消耗
        /// </summary>
        /// <returns></returns>
        public double GetInjure2Magic()
        {
            double percent = 0.0;

            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_INJUE2MAGIC, out magicHelperItem);
            }

            if (null == magicHelperItem) return percent;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return percent;
            percent = (magicHelperItem.MagicActionParams[0] / 100.0);
            return percent;
        }

        /// <summary>
        /// 将伤害转化为魔法消耗
        /// </summary>
        /// <returns></returns>
        public double GetNewInjure2Magic()
        {
            double injure2Magic = 0.0;

            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.NEW_TIME_INJUE2MAGIC, out magicHelperItem);
            }

            if (null == magicHelperItem) return injure2Magic;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return injure2Magic;
            injure2Magic = magicHelperItem.MagicActionParams[0];
            return injure2Magic;
        }

        /// <summary>
        /// 将伤害转化为魔法消耗3
        /// </summary>
        /// <returns></returns>
        public double GetNewInjure2Magic3()
        {
            double injure2Magic = 0.0;

            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.NEW_TIME_INJUE2MAGIC3, out magicHelperItem);
            }

            if (null == magicHelperItem) return injure2Magic;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return injure2Magic;
            injure2Magic = magicHelperItem.MagicActionParams[0];
            return injure2Magic;
        }

        /// <summary>
        /// 将伤害转化为魔法消耗
        /// </summary>
        /// <returns></returns>
        public double GetNewMagicSubInjure()
        {
            double injure2Magic = 0.0;

            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.NEW_MAGIC_SUBINJURE, out magicHelperItem);
            }

            if (null == magicHelperItem) return injure2Magic;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], 0)) return injure2Magic;
            injure2Magic = magicHelperItem.MagicActionParams[0];
            return injure2Magic;
        }

        /// <summary>
        /// 执行持续的物理伤害
        /// </summary>
        public void ExecuteAttack(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_ATTACK, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, 0, (magicHelperItem.MagicActionParams[0] / 100.0), 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, 0, (magicHelperItem.MagicActionParams[0] / 100.0), 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 执行持续的物理伤害
        /// </summary>
        public void ExecuteNewAttack(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.NEW_TIME_ATTACK, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        int addInjure = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, 0, (1.0 / magicHelperItem.MagicActionParams[2]), 0, false, addInjure, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        int addInjure = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, 0, (1.0 / magicHelperItem.MagicActionParams[2]), 0, false, addInjure, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 执行持续的物理伤害3
        /// </summary>
        public void ExecuteNewAttack3(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.NEW_TIME_ATTACK3, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        int injureValue = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, injureValue, 0.0, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        int injureValue = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, injureValue, 0.0, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 执行持续的魔法伤害
        /// </summary>
        public void ExecuteMAttack(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_MAGIC, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, 0, (magicHelperItem.MagicActionParams[0] / 100.0), 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, 0, (magicHelperItem.MagicActionParams[0] / 100.0), 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 执行持续的魔法伤害
        /// </summary>
        public void ExecuteNewMAttack(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.NEW_TIME_MAGIC, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        int addInjure = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, 0, (1.0 / magicHelperItem.MagicActionParams[2]), 1, false, addInjure, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        int addInjure = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, 0, (1.0 / magicHelperItem.MagicActionParams[2]), 1, false, addInjure, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 执行持续的魔法伤害3
        /// </summary>
        public void ExecuteNewMAttack3(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.NEW_TIME_MAGIC3, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        int injureValue = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, injureValue, 0.0, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        int injureValue = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, injureValue, 0.0, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 执行持续的魔法伤害4
        /// </summary>
        public void ExecuteNewMAttack4(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.NEW_TIME_MAGIC4, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        int injureValue = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, injureValue, 0.0, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        int injureValue = (int)(magicHelperItem.MagicActionParams[0] / magicHelperItem.MagicActionParams[2]);
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, injureValue, 0.0, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 持续伤血
        /// </summary>
        /// <param name="self"></param>
        public void ExecuteSubLife(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_SUBLIFE, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        ; GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, (int)magicHelperItem.MagicActionParams[0], 0.0, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, (int)magicHelperItem.MagicActionParams[0], 0.0, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 持续伤血2
        /// </summary>
        /// <param name="self"></param>
        public void ExecuteSubLife2(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_DS_INJURE, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[0], (int)magicHelperItem.MagicActionParams[1])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                int attackType = nOcc;
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        //最低伤害1，使用一个外部传入的1的技巧
                        ; GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, (int)magicHelperItem.MagicActionParams[2], 1.0, attackType, false, 0, 0.0, 0, 0, 0, 0.0, 0.0);

                        if (enemyMonster.VLife <= 0) //如果死亡
                        {
                            magicHelperItem.ExecutedNum = (int)magicHelperItem.MagicActionParams[1]; //终止buffer
                        }
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        //最低伤害1，使用一个外部传入的1的技巧
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, (int)magicHelperItem.MagicActionParams[2], 1.0, attackType, false, 0, 0.0, 0, 0, 0, 0.0, 0.0);

                        if (enemyClient.ClientData.CurrentLifeV <= 0) //如果死亡
                        {
                            magicHelperItem.ExecutedNum = (int)magicHelperItem.MagicActionParams[1]; //终止buffer
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 持续加血
        /// </summary>
        /// <param name="self"></param>
        public void ExecuteAddLife(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_ADDLIFE, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            if (self is GameClient) //如果是角色
            {
                GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient, magicHelperItem.MagicActionParams[0], "道具脚本" + MagicActionIDs.TIME_ADDLIFE.ToString());
            }
        }

        /// <summary>
        /// 持续加魔1
        /// </summary>
        /// <param name="self"></param>
        public void ExecuteAddMagic1(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_ADDMAGIC1, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[1], (int)magicHelperItem.MagicActionParams[2])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW() * 10000;

            if (self is GameClient) //如果是角色
            {
                GameManager.ClientMgr.AddSpriteMagicV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient, magicHelperItem.MagicActionParams[0], "道具脚本" + MagicActionIDs.TIME_ADDMAGIC1.ToString());
            }
        }

        /// <summary>
        /// 执行延迟的物理伤害
        /// </summary>
        public void ExecuteDelayAttack(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_DELAYATTACK, out magicHelperItem);
            }

            if (null == magicHelperItem) return;

            long nowTicks = TimeUtil.NOW() * 10000;
            long ticks = magicHelperItem.StartedTicks + ((int)magicHelperItem.MagicActionParams[1] * 1000 * 10000);

            //判断是否超过了时间
            if (nowTicks < ticks)
            {
                return;
            }

            //只执行一次
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.Remove(magicHelperItem.MagicActionID);
            }

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        ; GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, 0, (magicHelperItem.MagicActionParams[0] / 100.0), 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, 0, (magicHelperItem.MagicActionParams[0] / 100.0), 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 执行延迟的魔法伤害
        /// </summary>
        public void ExecuteDelayMAttack(IObject self)
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.TIME_DELAYMAGIC, out magicHelperItem);
            }

            if (null == magicHelperItem) return;

            long nowTicks = TimeUtil.NOW() * 10000;
            long ticks = magicHelperItem.StartedTicks + ((int)magicHelperItem.MagicActionParams[1] * 1000 * 10000);

            //判断是否超过了时间
            if (nowTicks < ticks)
            {
                return;
            }

            //只执行一次
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.Remove(magicHelperItem.MagicActionID);
            }

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int enemy = magicHelperItem.ObjectID;
            if (-1 != enemy)
            {
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    Monster enemyMonster = GameManager.MonsterMgr.FindMonster((self as GameClient).ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                        ; GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                             self as GameClient, enemyMonster, 0, 0, (magicHelperItem.MagicActionParams[0] / 100.0), 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
                else
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self as GameClient, enemyClient, 0, 0, (magicHelperItem.MagicActionParams[0] / 100.0), 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// 执行所有项
        /// </summary>
        public static void ExecuteAllItems(IObject self)
        {
            //执行持续的物理伤害
            (self as GameClient).RoleMagicHelper.ExecuteAttack(self);

            //执行持续的物理伤害
            (self as GameClient).RoleMagicHelper.ExecuteNewAttack(self);

            //执行持续的物理伤害3
            (self as GameClient).RoleMagicHelper.ExecuteNewAttack3(self);

            //执行持续的魔法伤害
            (self as GameClient).RoleMagicHelper.ExecuteMAttack(self);

            //执行持续的魔法伤害
            (self as GameClient).RoleMagicHelper.ExecuteNewMAttack(self);

            //执行持续的魔法伤害3
            (self as GameClient).RoleMagicHelper.ExecuteNewMAttack3(self);

            //执行持续的魔法伤害4
            (self as GameClient).RoleMagicHelper.ExecuteNewMAttack4(self);

            //持续伤血
            (self as GameClient).RoleMagicHelper.ExecuteSubLife(self);

            //持续伤血2
            (self as GameClient).RoleMagicHelper.ExecuteSubLife2(self);

            //持续加血
            (self as GameClient).RoleMagicHelper.ExecuteAddLife(self);

            // 持续加魔1
            (self as GameClient).RoleMagicHelper.ExecuteAddMagic1(self);

            //执行延迟的物理伤害
            (self as GameClient).RoleMagicHelper.ExecuteDelayAttack(self);

            //执行延迟的魔法伤害
            (self as GameClient).RoleMagicHelper.ExecuteDelayMAttack(self);
        }

        #endregion 执行相关辅助项

        #region     MU项目  辅租项

        /// <summary>
        /// MU项目持续减少伤害的百分比
        /// </summary>
        /// <returns></returns>
        public double MU_GetSubInjure1()
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.MU_SUB_DAMAGE_PERCENT_TIMER, out magicHelperItem);
            }

            if (null == magicHelperItem) return 0.0;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[0], 0)) return 0.0;
            return magicHelperItem.MagicActionParams[1];
        }

        /// <summary>
        /// MU项目持续减少伤害的值
        /// </summary>
        /// <returns></returns>
        public double MU_GetSubInjure2()
        {
            MagicHelperItem magicHelperItem = null;
            lock (_MagicHelperDict)
            {
                _MagicHelperDict.TryGetValue(MagicActionIDs.MU_SUB_DAMAGE_VALUE, out magicHelperItem);
            }

            if (null == magicHelperItem) return 0.0;
            if (!CanExecuteItem(magicHelperItem, (int)magicHelperItem.MagicActionParams[0], 0)) return 0.0;
            return magicHelperItem.MagicActionParams[1];
        }
        #endregion  MU项目  辅助项
    }
}
