using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
//using System.Windows.Forms;
using System.Windows;
using Server.Data;
using ProtoBuf;
using GameServer.Logic;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    // 属性改造 [8/15/2013 LiaoWei]
    // 4个一级属性 38个二级属性

    /// <summary>
    /// Buffer属性项
    /// </summary>
    public class BufferPropItem
    {
        public BufferPropItem()
        {
            ResetProps();
        }

        /// <summary>
        /// 4个基础属性值
        /// </summary>
        private double[] _BaseProps = new double[(int)UnitPropIndexes.Max];

        /// <summary>
        /// 4个基础属性值的加入时间
        /// </summary>
        private long[] _BasePropsTick = new long[(int)UnitPropIndexes.Max];

        /// <summary>
        /// 4个基础属性值
        /// </summary>
        public double[] BaseProps
        {
            get { return _BaseProps; }
        }

        /// <summary>
        /// 4个基础属性值的加入时间
        /// </summary>
        public long[] BasePropsTick
        {
            get { return _BasePropsTick; }
        }

        /// <summary>
        /// 41个扩展属性值
        /// </summary>
        private double[] _ExtProps = new double[(int)ExtPropIndexes.Max];

        /// <summary>
        /// 41个扩展属性值加入时间
        /// </summary>
        private long[] _ExtPropsTick = new long[(int)ExtPropIndexes.Max];

        /// <summary>
        /// 41个扩展属性值
        /// </summary>
        public double[] ExtProps
        {
            get { return _ExtProps; }
        }

        /// <summary>
        /// 41个基础属性值加入时间
        /// </summary>
        public long[] ExtPropsTick
        {
            get { return _ExtPropsTick; }
        }

        /// <summary>
        /// 清空属性值
        /// </summary>
        public void ResetProps()
        {
            for (int i = 0; i < (int)UnitPropIndexes.Max; i++)
            {
                _BaseProps[i] = 0;
                _BasePropsTick[i] = 0;
            }

            for (int i = 0; i < (int)ExtPropIndexes.Max; i++)
            {
                _ExtProps[i] = 0;
                _ExtPropsTick[i] = 0;
            }
        }
    }

    /// <summary>
    /// 精灵Buffer
    /// </summary>
    public class SpriteBuffer
    {
        #region 永久属性

        /// <summary>
        /// 永久改变属性
        /// </summary>
        private BufferPropItem _ForeverProp = new BufferPropItem();

        /// <summary>
        /// 重置永久属性
        /// </summary>
        public void ResetForeverProps()
        {
            lock (_ForeverProp)
            {
                _ForeverProp.ResetProps();
            }
        }

        /// <summary>
        /// 添加永久的基础属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddForeverBaseProp(int index, double value)
        {
            lock (_ForeverProp)
            {
                _ForeverProp.BaseProps[index] = value;
            }
        }

        /// <summary>
        /// 添加永久的扩展属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddForeverExtProp(int index, double value)
        {
            lock (_ForeverProp)
            {
                _ForeverProp.ExtProps[index] = value;
            }
        }

        #endregion 永久属性

        #region 临时属性

        /// <summary>
        /// 临时改变属性
        /// </summary>
        private BufferPropItem _TempProp = new BufferPropItem();

        /// <summary>
        /// 获取基础属性的拷贝
        /// </summary>
        /// <returns></returns>
        public double[] getCopyBaseProp()
        {
            double[] baseProps = _TempProp.BaseProps;
            double[] copyBaseProps = new double[baseProps.Length];

            for (int i = 0; i < baseProps.Length; i++)
            {
                copyBaseProps[i] = baseProps[i];
            }

            return copyBaseProps;
        }

        /// <summary>
        /// 获取脱战属性的拷贝
        /// </summary>
        /// <returns></returns>
        public double[] getCopyExtProp()
        {
            double[] extProps = _TempProp.ExtProps;
            double[] copyExtProps = new double[extProps.Length];

            for (int i = 0; i < extProps.Length; i++)
            {
                copyExtProps[i] = extProps[i];
            }

            return copyExtProps;
        }

        /// <summary>
        /// 添加临时的基础属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddTempBaseProp(int index, double value, long toTicks)
        {
            lock (_TempProp)
            {
                _TempProp.BaseProps[index] = value;
                _TempProp.BasePropsTick[index] = toTicks;
            }
        }

        /// <summary>
        /// 添加临时的扩展属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddTempExtProp(int index, double value, long toTicks)
        {
            lock (_TempProp)
            {
                // 只有效果比已有的好时，才进行覆盖 ChenXiaojun
                if (Math.Abs(value) >= Math.Abs(_TempProp.ExtProps[index]))
                {
                    _TempProp.ExtProps[index] = value;
                    _TempProp.ExtPropsTick[index] = toTicks;
                }
            }
        }

        #endregion 临时属性

        #region 获取Buffer属性

        /// <summary>
        /// 获取基础buffer属性 // GetBaseProb逻辑与存值处理有问题，这里并未起到任何效果，先mark 之后看是否要调整 [XSea 2015/6/8]
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double GetBaseProp(int index)
        {
            double tempProp = 0.0;
            lock (_TempProp)
            {
                long nowTicks = TimeUtil.NOW() * 10000;
                if (nowTicks - _TempProp.BasePropsTick[index] < 0)
                {
                    tempProp = _TempProp.BaseProps[index];
                }
            }

            lock (_ForeverProp)
            {
                return _ForeverProp.BaseProps[index] + tempProp;
            }
        }

        /// <summary>
        /// 获取扩展buffer属性
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double GetExtProp(int index)
        {
            double tempProp = 0.0;

            lock (_TempProp)
            {
                long nowTicks = TimeUtil.NOW() * 10000;
                if (nowTicks - _TempProp.ExtPropsTick[index] < 0)
                {
                    tempProp = _TempProp.ExtProps[index];
                }
            }

            lock (_ForeverProp)
            {
                return _ForeverProp.ExtProps[index] + tempProp;
            }
        }

        #endregion 获取Buffer属性


        #region 清空Buffer属性
        /// <summary>
        /// 清空临时属性系数
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void ClearAllTempProps()
        {
            lock (_TempProp)
            {
                _TempProp.ResetProps();
            }
        }

        /// <summary>
        /// 清空永久属性系数
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void ClearAllForeverProps()
        {
            lock (_ForeverProp)
            {
                _ForeverProp.ResetProps();
            }
        }

        #endregion 清空Buffer属性
    }

    /// <summary>
    /// Buffer乘以系数属性项
    /// </summary>
    public class SpriteMultipliedBuffer
    {
        #region 临时属性系数

        /// <summary>
        /// 临时改变属性
        /// </summary>
        private BufferPropItem _TempProp = new BufferPropItem();

        /// <summary>
        /// 添加临时的扩展属性系数
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddTempExtProp(int index, double value, long toTicks)
        {
            lock (_TempProp)
            {
                _TempProp.ExtProps[index] = value;
                _TempProp.ExtPropsTick[index] = toTicks;
            }
        }

        /// <summary>
        /// 清空所有临时的扩展属性系数
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void ClearAllTempExtProps()
        {
            lock (_TempProp)
            {
                _TempProp.ResetProps();
            }
        }

        #endregion 临时属性系数

        #region 获取Buffer属性系数

        /// <summary>
        /// 获取扩展buffer属性系数
        /// </summary>
        /// <param name="index"></param>
        /// <returns>baseValue * (1 + 属性值)</returns>
        public double GetExtProp(int index, double baseValue)
        {
            double tempProp = 0.0;

            lock (_TempProp)
            {
                long nowTicks = TimeUtil.NOW() * 10000;
                if (_TempProp.ExtPropsTick[index] <= 0 || nowTicks - _TempProp.ExtPropsTick[index] < 0)
                {
                    tempProp = _TempProp.ExtProps[index];
                }
            }

            return (1.0 + tempProp) * baseValue;
        }

        /// <summary>
        /// 获取扩展buffer属性系数
        /// </summary>
        /// <param name="index"></param>
        /// <returns>属性值</returns>
        public double GetExtProp(int index)
        {
            double tempProp = 0.0;

            lock (_TempProp)
            {
                long nowTicks = TimeUtil.NOW() * 10000;
                if (_TempProp.ExtPropsTick[index] <= 0 || nowTicks - _TempProp.ExtPropsTick[index] < 0)
                {
                    tempProp = _TempProp.ExtProps[index];
                }
            }

            return tempProp;
        }

        #endregion 获取Buffer属性系数
    }

    /// <summary>
    /// 精灵一次性Buffer
    /// </summary>
    public class SpriteOnceBuffer
    {

        #region 临时属性

        /// <summary>
        /// 临时改变属性
        /// </summary>
        private BufferPropItem _TempProp = new BufferPropItem();

        /// <summary>
        /// 添加临时的基础属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddTempBaseProp(int index, double value, long toTicks)
        {
            lock (_TempProp)
            {
                _TempProp.BaseProps[index] = value;
                _TempProp.BasePropsTick[index] = toTicks;
            }
        }

        /// <summary>
        /// 添加临时的扩展属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddTempExtProp(int index, double value, long toTicks)
        {
            lock (_TempProp)
            {
                _TempProp.ExtProps[index] = value;
                _TempProp.ExtPropsTick[index] = toTicks;
            }
        }

        #endregion 临时属性

        #region 获取Buffer属性

        /// <summary>
        /// 获取基础buffer属性
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double GetBaseProp(int index)
        {
            double tempProp = 0.0;
            lock (_TempProp)
            {
                tempProp = _TempProp.BaseProps[index];
                _TempProp.BaseProps[index] = 0.0;
            }

            return tempProp; 
        }

        /// <summary>
        /// 获取扩展buffer属性
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double GetExtProp(int index)
        {
            double tempProp = 0.0;

            lock (_TempProp)
            {
                 tempProp = _TempProp.ExtProps[index];
                 _TempProp.ExtProps[index] = 0.0;
            }

            return tempProp;
        }

        #endregion 获取Buffer属性
    }

    /// <summary>
    /// 临时属性
    /// </summary>
    public class TempPropItem
    {
        public double PropValue = 0.0;
        public long ToTicks = 0L;
    }

    /// <summary>
    /// 怪物Buffer
    /// </summary>
    public class MonsterBuffer
    {

        #region 临时属性

        /// <summary>
        /// 临时改变属性
        /// </summary>
        //private BufferPropItem _TempProp = new BufferPropItem();
        Dictionary<int, TempPropItem> _TempBasePropsDict = new Dictionary<int, TempPropItem>();
        Dictionary<int, TempPropItem> _TempExtPropsDict = new Dictionary<int, TempPropItem>();

        /// <summary>
        /// 添加临时的基础属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddTempBaseProp(int index, double value, long toTicks)
        {
            lock (_TempBasePropsDict)
            {
                _TempBasePropsDict[index] = new TempPropItem()
                {
                    PropValue = value,
                    ToTicks = toTicks,
                };

                //_TempProp.BaseProps[index] = value;
                //_TempProp.BasePropsTick[index] = toTicks;
            }
        }

        /// <summary>
        /// 添加临时的扩展属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddTempExtProp(int index, double value, long toTicks)
        {
            lock (_TempExtPropsDict)
            {
                _TempExtPropsDict[index] = new TempPropItem()
                {
                    PropValue = value,
                    ToTicks = toTicks,
                };

                //_TempExtPropsDict[index] = value;
                //_TempExtPropsDictPropsTick[index] = toTicks;
            }
        }

        /// <summary>
        /// 清除所有临时的基础属性
        /// </summary>
        public void ClearTempBaseProp()
        {
            lock (_TempBasePropsDict)
            {
                _TempBasePropsDict.Clear();
            }
        }

        /// <summary>
        /// 清除所有临时的扩展属性
        /// </summary>
        public void ClearTempExtProp()
        {
            lock (_TempExtPropsDict)
            {
                _TempExtPropsDict.Clear();
            }
        }

        /// <summary>
        /// 重置/初始化
        /// </summary>
        public void Init()
        {
            ClearTempBaseProp();
            ClearTempExtProp();
        }

        #endregion 临时属性

        #region 获取Buffer属性

        /// <summary>
        /// 获取基础buffer属性
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double GetBaseProp(int index)
        {
            long nowTicks = TimeUtil.NOW() * 10000;
            double tempProp = 0.0;
            TempPropItem tempPropItem = null;
            lock (_TempBasePropsDict)
            {
                //tempProp = _TempProp.BaseProps[index];
                //_TempProp.BaseProps[index] = 0.0;

                if (!_TempBasePropsDict.TryGetValue(index, out tempPropItem))
                {
                    return tempProp;
                }

                if (nowTicks - tempPropItem.ToTicks < 0)
                {
                    tempProp = tempPropItem.PropValue;
                }
                else
                {
                    _TempBasePropsDict.Remove(index);
                }

            }

            return tempProp;
        }

        /// <summary>
        /// 获取扩展buffer属性
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double GetExtProp(int index)
        {
            long nowTicks = TimeUtil.NOW() * 10000;
            double tempProp = 0.0;
            TempPropItem tempPropItem = null;
            lock (_TempExtPropsDict)
            {
                //tempProp = _TempProp.BaseProps[index];
                //_TempProp.BaseProps[index] = 0.0;

                if (!_TempExtPropsDict.TryGetValue(index, out tempPropItem))
                {
                    return tempProp;
                }

                if (nowTicks - tempPropItem.ToTicks < 0)
                {
                    tempProp = tempPropItem.PropValue;
                }
                else
                {
                    _TempExtPropsDict.Remove(index);
                }
            }

            return tempProp;
        }

        #endregion 获取Buffer属性
    }
}
