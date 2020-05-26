using GameServer.Core.Executor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 多段伤害队列项
    /// </summary>
    public class ManyTimeDmageQueueItem
    {
        /// <summary>
        /// 执行的时间点
        /// </summary>
        public long ToExecTicks = 0;

        /// <summary>
        /// 敌人的ID
        /// </summary>
        public int enemy = -1;

        /// <summary>
        /// 敌人的坐标X
        /// </summary>
        public int enemyX = 0;
        
        /// <summary>
        /// 敌人的坐标Y
        /// </summary>
        public int enemyY = 0;

        /// <summary>
        /// 真实的敌人坐标X
        /// </summary>
        public int realEnemyX = 0;

        /// <summary>
        /// 真是的敌人坐标Y
        /// </summary>
        public int realEnemyY = 0;
        
        /// <summary>
        /// 技能ID
        /// </summary>
        public int magicCode = 0;

        /// <summary>
        /// 分段顺序
        /// </summary>
        public int manyRangeIndex = 0;

        /// <summary>
        /// 分段伤害比例
        /// </summary>
        public double manyRangeInjuredPercent = 1.0;
    }

    public class ManyTimeDmageMagicItem
    {
        public static ManyTimeDmageItem SingleDamageItem = new ManyTimeDmageItem() { InjuredPercent = 1 };

        /// <summary>
        /// 执行的时间点
        /// </summary>
        public long execTicks = 0;

        /// <summary>
        /// 敌人的ID
        /// </summary>
        public int enemy = -1;

        /// <summary>
        /// 敌人的坐标X
        /// </summary>
        public int enemyX = 0;

        /// <summary>
        /// 敌人的坐标Y
        /// </summary>
        public int enemyY = 0;

        /// <summary>
        /// 真实的敌人坐标X
        /// </summary>
        public int realEnemyX = 0;

        /// <summary>
        /// 真是的敌人坐标Y
        /// </summary>
        public int realEnemyY = 0;

        /// <summary>
        /// 技能ID
        /// </summary>
        public int magicCode = 0;

        /// <summary>
        /// 开始计时时间
        /// </summary>
        public long startTicks;

        /// <summary>
        /// 当前执行
        /// </summary>
        public int execIndex = -1;

        /// <summary>
        /// 列表
        /// </summary>
        public List<ManyTimeDmageItem> itemList;

        /// <summary>
        /// 
        /// </summary>
        public LinkedListNode<ManyTimeDmageMagicItem> linkedListNode;

        public bool Start(long nowTicks, int magicCode, int enemy, int enemyX, int enemyY, int realEnemyX, int realEnemyY)
        {
            if (execIndex == -1)
            {
                this.execIndex = 0;
                this.startTicks = nowTicks;
                this.magicCode = magicCode;
                this.enemy = enemy;
                this.enemyX = enemyX;
                this.enemyY = enemyY;
                this.realEnemyX = realEnemyX;
                this.realEnemyY = realEnemyY;
                if (null != itemList)
                {
                    execTicks = startTicks + itemList[execIndex].InjuredSeconds;
                }

                return true;
            }

            return false;
        }

        public ManyTimeDmageItem Get()
        {
            lock (this)
            {
                if (null == itemList)
                    return SingleDamageItem;
                else
                    return itemList[execIndex];
            }
        }

        public bool Next()
        {
            lock (this)
            {
                if (null == itemList)
                {
                    execIndex = -1;
                }
                else
                {
                    if (execIndex < itemList.Count - 1)
                    {
                        execIndex++;
                        execTicks = startTicks + itemList[execIndex].InjuredSeconds;
                        return true;
                    }
                    else
                    {
                        execTicks = 0;
                        execIndex = -1;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 多段伤害队列管理
    /// </summary>
    public class MagicsManyTimeDmageQueue
    {
        private object mutex = new object();

        private static Dictionary<int, ManyTimeDmageMagicItem> manyTimeDmageQueueItemStaticDict = new Dictionary<int, ManyTimeDmageMagicItem>();

        private Dictionary<int, ManyTimeDmageMagicItem> manyTimeDmageQueueItemDict = new Dictionary<int, ManyTimeDmageMagicItem>();

        private HashSet<ManyTimeDmageMagicItem> execItemDict = new HashSet<ManyTimeDmageMagicItem>();

        public bool AddManyTimeDmageQueueItemEx(int enemy, int enemyX, int enemyY, int realEnemyX, int realEnemyY, int magicCode)
        {
            Lazy<long> startTicks = new Lazy<long>(() => TimeUtil.NOW());
            Lazy<List<ManyTimeDmageItem>> lazyList = new Lazy<List<ManyTimeDmageItem>>(() => MagicsManyTimeDmageCachingMgr.GetManyTimeDmageItems(magicCode));

            ManyTimeDmageMagicItem itemStatic;
            ManyTimeDmageMagicItem item;

            lock (mutex)
            {
                if (manyTimeDmageQueueItemDict.TryGetValue(magicCode, out item))
                {
                    if (item.itemList == null)
                    {
                        return false;
                    }
                    else
                    {
                        if (item.Start(startTicks.Value, magicCode, enemy, enemyX, enemyY, realEnemyX, realEnemyY))
                        {
                            execItemDict.Add(item);
                        }

                        return true;
                    }
                }

                if (!manyTimeDmageQueueItemStaticDict.TryGetValue(magicCode, out itemStatic))
                {
                    itemStatic = new ManyTimeDmageMagicItem();
                    itemStatic.itemList = lazyList.Value;
                    manyTimeDmageQueueItemStaticDict[magicCode] = itemStatic;
                }

                //如果本对象还未缓存,则先构建一个,并从静态全局字典中查询
                if (!manyTimeDmageQueueItemDict.TryGetValue(magicCode, out item))
                {
                    item = new ManyTimeDmageMagicItem();
                    item.itemList = itemStatic.itemList;
                    manyTimeDmageQueueItemDict[magicCode] = item;
                }

                if (item.itemList == null)
                {
                    return false;
                }

                if (item.Start(startTicks.Value, magicCode, enemy, enemyX, enemyY, realEnemyX, realEnemyY))
                {
                    execItemDict.Add(item);
                }

                return true;
            }
        }

        public bool AddDelayMagicItemEx(int enemy, int enemyX, int enemyY, int realEnemyX, int realEnemyY, int magicCode)
        {
            Lazy<long> startTicks = new Lazy<long>(() => TimeUtil.NOW());
            ManyTimeDmageMagicItem itemStatic;
            ManyTimeDmageMagicItem item;

            lock (mutex)
            {
                if (manyTimeDmageQueueItemDict.TryGetValue(magicCode, out item))
                {
                    if (item.Start(startTicks.Value, magicCode, enemy, enemyX, enemyY, realEnemyX, realEnemyY))
                    {
                        execItemDict.Add(item);
                    }

                    return true;
                }

                if (!manyTimeDmageQueueItemStaticDict.TryGetValue(magicCode, out itemStatic))
                {
                    itemStatic = new ManyTimeDmageMagicItem();
                    manyTimeDmageQueueItemStaticDict[magicCode] = itemStatic;
                }

                //如果本对象还未缓存,则先构建一个,并从静态全局字典中查询
                if (!manyTimeDmageQueueItemDict.TryGetValue(magicCode, out item))
                {
                    item = new ManyTimeDmageMagicItem();
                    item.itemList = itemStatic.itemList;
                    manyTimeDmageQueueItemDict[magicCode] = item;
                }

                if (item.Start(startTicks.Value, magicCode, enemy, enemyX, enemyY, realEnemyX, realEnemyY))
                {
                    execItemDict.Add(item);
                }

                return true;
            }
        }

        /// <summary>
        /// 获取现在的项数
        /// </summary>
        /// <returns></returns>
        public int GetManyTimeDmageQueueItemNumEx()
        {
            lock (mutex)
            {
                return execItemDict.Count;
            }
        }

        /// <summary>
        /// 获取可以执行的项
        /// </summary>
        /// <returns></returns>
        public ManyTimeDmageMagicItem GetCanExecItemsEx(out ManyTimeDmageItem subItem)
        {
            ManyTimeDmageMagicItem magicItem = null;
            subItem = null;
            long ticks = TimeUtil.NowEx();
            lock (mutex)
            {
                List<ManyTimeDmageMagicItem> removeList = null;
                foreach (var item in execItemDict)
                {
                    if (ticks > item.execTicks)
                    {
                        magicItem = item;
                        subItem = magicItem.Get();
                        if (!magicItem.Next())
                        {
                            if (null == removeList)
                            {
                                removeList = new List<ManyTimeDmageMagicItem>();
                            }

                            removeList.Add(item);
                        }

                        break;
                    }
                }

                if (null != removeList)
                {
                    foreach (var item in removeList)
                    {
                        execItemDict.Remove(item);
                    }
                }
            }

            return magicItem;
        }


        /// <summary>
        /// 等待执行的分段技能项
        /// </summary>
        private List<ManyTimeDmageQueueItem> ManyTimeDmageQueueItemList = new List<ManyTimeDmageQueueItem>();

        /// <summary>
        /// 添加一个新的项
        /// </summary>
        /// <param name="manyTimeDmageQueueItem"></param>
        public void AddManyTimeDmageQueueItem(ManyTimeDmageQueueItem manyTimeDmageQueueItem)
        {
            lock (ManyTimeDmageQueueItemList)
            {
                ManyTimeDmageQueueItemList.Add(manyTimeDmageQueueItem);
            }
        }

        /// <summary>
        /// 获取现在的项数
        /// </summary>
        /// <param name="manyTimeDmageQueueItem"></param>
        public int GetManyTimeDmageQueueItemNum()
        {
            lock (ManyTimeDmageQueueItemList)
            {
                return ManyTimeDmageQueueItemList.Count;
            }
        }

        /// <summary>
        /// 获取可以执行的项
        /// </summary>
        /// <returns></returns>
        public List<ManyTimeDmageQueueItem> GetCanExecItems()
        {
            long ticks = TimeUtil.NOW();
            List<ManyTimeDmageQueueItem> canExecItemList = new List<ManyTimeDmageQueueItem>();
            lock (ManyTimeDmageQueueItemList)
            {
                for (int i = 0; i < ManyTimeDmageQueueItemList.Count; i++)
                {
                    if (ticks >= ManyTimeDmageQueueItemList[i].ToExecTicks)
                    {
                        canExecItemList.Add(ManyTimeDmageQueueItemList[i]);
                    }
                }

                for (int i = 0; i < canExecItemList.Count; i++)
                {
                    ManyTimeDmageQueueItemList.Remove(canExecItemList[i]);
                }
            }

            return canExecItemList;
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            lock (ManyTimeDmageQueueItemList)
            {
                ManyTimeDmageQueueItemList.Clear();
            }
        }
    }
}
