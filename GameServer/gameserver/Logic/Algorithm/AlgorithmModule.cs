using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GameServer.Core.Executor;
using Tmsk.Contract;
using System.Collections;

namespace GameServer.Logic
{
    public class PropsValueFactory
    {
        public int propIndex;
        public long nextCalcTicks;
        public double propValue;
        public Func<double> factoryFunc;
        public long Age;
    }

    public class PropsCacheModule
    {
        private object mutex = new object();

        private Dictionary<int, PropsValueFactory> extPropValueDict = new Dictionary<int, PropsValueFactory>();

        /// <summary>
        /// 获取缓存的二级属性的值,如超过指定时间间隔,则重新计算
        /// </summary>
        /// <param name="propIndex"></param>
        /// <param name="factoryFunc"></param>
        /// <returns></returns>
        public double GetExtPropsValue(int propIndex, Func<double> factoryFunc)
        {
            long age;
            long nowTicks = TimeUtil.CurrentTicksInexact;
            PropsValueFactory propsValueFactory;
            lock (mutex)
            {
                if (!extPropValueDict.TryGetValue(propIndex, out propsValueFactory))
                {
                    propsValueFactory = new PropsValueFactory()
                        {
                            propIndex = propIndex,
                            nextCalcTicks = nowTicks,
                            factoryFunc = factoryFunc,
                        };

                    extPropValueDict[propIndex] = propsValueFactory;
                }

                if (propsValueFactory.nextCalcTicks > nowTicks)
                {
                    return propsValueFactory.propValue;
                }

                propsValueFactory.Age++;
                age = propsValueFactory.Age;
            }

            double propValue = propsValueFactory.factoryFunc();
            lock (mutex)
            {
                if (propsValueFactory.Age <= age)
                {
                    propsValueFactory.nextCalcTicks = nowTicks + GameManager.FlagRecalcRolePropsTicks;
                    propsValueFactory.propValue = propValue;
                }

                return propValue;
            }
        }

        private Dictionary<int, PropsValueFactory> basePropsValueDict = new Dictionary<int, PropsValueFactory>();

        /// <summary>
        /// 获取缓存的一级属性的值,如超过指定时间间隔,则重新计算
        /// </summary>
        /// <param name="propIndex"></param>
        /// <param name="factoryFunc"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public double GetBasePropsValue(int propIndex, Func<double> factoryFunc, bool cache = true)
        {
            if (cache)
            {
                long age;
                long nowTicks = TimeUtil.CurrentTicksInexact;
                PropsValueFactory propsValueFactory;
                lock (mutex)
                {
                    if (!basePropsValueDict.TryGetValue(propIndex, out propsValueFactory))
                    {
                        propsValueFactory = new PropsValueFactory()
                            {
                                propIndex = propIndex,
                                nextCalcTicks = nowTicks,
                                factoryFunc = factoryFunc,
                            };

                        basePropsValueDict[propIndex] = propsValueFactory;
                    }

                    if (propsValueFactory.nextCalcTicks > nowTicks)
                    {
                        return propsValueFactory.propValue;
                    }

                    propsValueFactory.Age++;
                    age = propsValueFactory.Age;
                }

                double propValue = propsValueFactory.factoryFunc();
                lock (mutex)
                {
                    if (propsValueFactory.Age == age)
                    {
                        propsValueFactory.nextCalcTicks = nowTicks + GameManager.FlagRecalcRolePropsTicks;
                        propsValueFactory.propValue = propValue;
                    }

                    return propValue;
                }
            }

            return factoryFunc();
        }

        /// <summary>
        /// 重置缓存,强制下一次获取时重新计算
        /// </summary>
        public void ResetAllProps()
        {
            lock (mutex)
            {
                foreach (var props in basePropsValueDict.Values)
                {
                    props.nextCalcTicks = 0;
                }

                foreach (var props in extPropValueDict.Values)
                {
                    props.nextCalcTicks = 0;
                }
            }
        }
    }

    /// <summary>
    /// 延时执行模块,基于当前架构,减少战斗逻辑的复杂度,重新分配计算任务并减少多线程代码复杂度
    /// </summary>
    public class DelayExecModule
    {
        private object mutex = new object();

        public BitArray DelayExecPorcsBits = new BitArray((int)DelayExecProcIds.Max);

        /// <summary>
        /// 设置需要延时执行的方法
        /// </summary>
        /// <param name="procIds"></param>
        public void SetDelayExecProc(params DelayExecProcIds[] procIds)
        {
            if (null == procIds || procIds.Length == 0)
            {
                return;
            }

            lock (mutex)
            {
                foreach (var procId in procIds)
                {
                    DelayExecPorcsBits.Set((int)procId, true);
                }
            }
        }

        /// <summary>
        /// 执行需要延时执行的方法
        /// </summary>
        /// <param name="client"></param>
        public void ExecDelayProcs(GameClient client)
        {
            bool recalcProps;
            bool updateOtherProps;
            bool notifyRefreshProps;
            lock (mutex)
            {
                recalcProps = DelayExecPorcsBits.Get((int)DelayExecProcIds.RecalcProps);
                updateOtherProps = DelayExecPorcsBits.Get((int)DelayExecProcIds.UpdateOtherProps);
                notifyRefreshProps = DelayExecPorcsBits.Get((int)DelayExecProcIds.NotifyRefreshProps);
                DelayExecPorcsBits.SetAll(false);
            }

            if (recalcProps) RecalcProps(client);
            if (updateOtherProps) UpdateOtherProps(client);
            if (notifyRefreshProps || recalcProps) NotifyRefreshProps(client);
        }

        /// <summary>
        /// 刷新装备属性
        /// </summary>
        /// <param name="client"></param>
        private void RecalcProps(GameClient client)
        {
            Global.RefreshEquipProp(client);
        }

        /// <summary>
        /// 添加部分其他系统的属性,暂时未用到这些属性
        /// </summary>
        /// <param name="client"></param>
        private void UpdateOtherProps(GameClient client)
        {
            //加载骑乘的属性
            /// 将坐骑的扩展属性加入Buffer中
            Global.UpdateHorseDataProps(client, true);

            //将经脉的列表属性加入Buffer中
            Global.UpdateJingMaiListProps(client, true); 
        }

        /// <summary>
        /// 重新计算自己最新的属性信息并通知自己和附近角色
        /// </summary>
        /// <param name="client"></param>
        private void NotifyRefreshProps(GameClient client)
        {
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
        }
    }
}
