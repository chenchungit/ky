using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;

namespace GameServer.Logic
{
    public enum PropsTypes
    {
        BaseProps,
        ExtProps,
    }

    public class TimedPropsData : IComparer<TimedPropsData>
    {
        public long startTicks;
        public int bufferTicks;
        public int propsType;
        public int propsIndex;
        public double propsValue;
        public int tag;
        public int skillId;
        public long endTicks;

        public TimedPropsData(long _startTicks, int _bufferTicks, int _propsType, int _propsIndex, double _propsValue, int _tag, int _color)
        {
            startTicks = _startTicks;
            bufferTicks = _bufferTicks;
            propsType = _propsType;
            propsIndex = _propsIndex;
            propsValue = _propsValue;
            tag = _tag;
            skillId = _color;
            endTicks = _startTicks + _bufferTicks;
        }

        public int Compare(TimedPropsData x, TimedPropsData y)
        {
            if (x.endTicks > y.endTicks)
            {
                return 1;
            }
            else if (x.endTicks < y.endTicks)
            {
                return -1;
            }
            else
            {
                return x.GetHashCode() - y.GetHashCode();
            }
        }
    }

    public class BufferPropsModule
    {
        private object mutex = new object();

        private const long MinCheckIntervalTicks = 10 * 1000;

        private long MinExpireTicks = 0;

        public Dictionary<long, TimedPropsData> bufferDataDict = new Dictionary<long, TimedPropsData>();

        public PropsCacheManager propCacheManager = null;

        public void Init(PropsCacheManager _propCacheManager)
        {
            propCacheManager = _propCacheManager;
        }

        private void UpdateTimedProps(TimedPropsData data, bool enable)
        {
            double propsValue = 0;
            if (enable)
            {
                propsValue = data.propsValue;
            }
            
            if (data.propsType == (int)PropsTypes.ExtProps)
            {
                propCacheManager.SetExtPropsSingle(PropsSystemTypes.BufferPropsManager, data.skillId, data.propsType, data.propsIndex, propsValue);
            }
            else if (data.propsType == (int)PropsTypes.BaseProps)
            {
                propCacheManager.SetBasePropsSingle(PropsSystemTypes.BufferPropsManager, data.skillId, data.propsType, data.propsIndex, propsValue);
            }
        }

        public void UpdateTimedPropsData(long nowTicks, long startTicks, int bufferTicks, int propsType, int propsIndex, double propsValue, int skillId, int tag)
        {
            TimedPropsData data;
            long key = (((long)skillId) << 32) + (propsType << 24) + propsIndex;
            lock (mutex)
            {
                if (!bufferDataDict.TryGetValue(key, out data))
                {
                    data = new TimedPropsData(startTicks, bufferTicks, propsType, propsIndex, propsValue, tag, skillId);
                    bufferDataDict[key] = data;
                }
                else
                {
                    data.startTicks = startTicks;
                    data.bufferTicks = bufferTicks;
                    data.propsType = propsType;
                    data.propsIndex = propsIndex;
                    data.propsValue = propsValue;
                    data.tag = tag;
                    data.skillId = skillId;
                    data.endTicks = startTicks + bufferTicks;
                }

                //设置属性
                UpdateTimedProps(data, true);

                //重置以便重新计算最小超时时间
                TimerUpdateProps(nowTicks, true);
            }
        }

        public void TimerUpdateProps(long nowTicks, bool force = false)
        {
            if (null != propCacheManager)
            {
                lock (mutex)
                {
                    if (!force && nowTicks < MinExpireTicks)
                    {
                        return;
                    }

                    MinExpireTicks = nowTicks + MinCheckIntervalTicks;
                    List<long> list = new List<long>();
                    foreach (var kv in bufferDataDict)
                    {
                        long endTicks = kv.Value.endTicks;
                        if (endTicks < nowTicks)
                        {
                            list.Add(kv.Key);
                            UpdateTimedProps(kv.Value, false);
                        }
                        else if (endTicks < MinExpireTicks)
                        {
                            MinExpireTicks = endTicks;
                        }
                    }

                    foreach (var key in list)
                    {
                        bufferDataDict.Remove(key);
                    }
                }
            }
        }
    }
}
