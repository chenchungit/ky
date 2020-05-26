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
using System.Diagnostics.Contracts;

namespace GameServer.Logic
{
    public enum PropsSystemTypes
    {
        Total = 0,              //所有子系统的属性总计
        BufferByGoodsProps = 1, //罗兰城战旗帜Buff属性, MU_LUOLANCHENGZHAN_QIZHI1, 扩展属性数组
        FashionByGoodsProps = 2,//时装属性, 时装TabID, 扩展属性数组
        GM_Temp_Props = 3,      //GM设置的临时属性
        AchievementRune = 4,    //成就符文
        LingYuProps = 5,        //翎羽
        ZhuLingZhuHunProps = 6, //注灵注魂
        Artifact = 7,           //神器再造
        HysyShengBei = 8,       //幻影寺院-圣杯
        PrestigeMedal = 9,      //声望勋章
        MarriageRing = 10,      //[bing] 婚戒
        JingLingQiYuan = 11,    //精灵奇缘
        TuJian = 12,            //图鉴
        GuardStatue = 13,       //守护雕像
        Talent = 14,            //天赋
        MerlinMagicBook = 15,   //梅林魔法书
        HolyItem = 16,          //[bing] 圣物系统
        FluorescentGem = 17,    //荧光宝石
        SoulStone = 18,         //魂石
        UnionPalace = 19,
        BufferPropsManager = 20, //BufferPropsManager系统加的属性,第二关键字为BufferId
        CoupleArena = 21, //情侣竞技场
 		TarotCard = 22, //塔罗牌系统属性，牌的位置索引，扩展属性数组
 		RedNameDebuff = 23, //红名惩罚buff
    }

    public delegate void DelOnPropsChanged(int key);

    /// <summary>
    /// 属性项
    /// </summary>
    public class PropsCacheItem
    {
        /// <summary>
        /// 年龄
        /// </summary>
        public int Age = 0;

        /// <summary>
        /// 上次检查差异时的年龄
        /// </summary>
        public int LastAge = 0;

        /// <summary>
        /// 属性修改通知
        /// </summary>
        public DelOnPropsChanged OnPropsChanged;

        /// <summary>
        /// 4个基础属性值
        /// </summary>
        public double[] BaseProps = new double[(int)UnitPropIndexes.Max];

        /// <summary>
        /// 41个扩展属性值
        /// </summary>
        public double[] ExtProps = new double[(int)ExtPropIndexes.Max];

        /// <summary>
        /// 父对象
        /// </summary>
        public PropsCacheItem ParentPropsCacheItem;

        /// <summary>
        /// 子系统属性字典
        /// </summary>
        public Dictionary<int, PropsCacheItem> SubPropsItemDict = new Dictionary<int, PropsCacheItem>();

        /// <summary>
        /// 属性路径
        /// </summary>
        public List<int> Path = new List<int>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parent"></param>
        public PropsCacheItem(PropsCacheItem parent, int type)
        {
            ParentPropsCacheItem = parent;
            if (parent != null)
            {
                Path.AddRange(parent.Path);
                Path.Add(type);
            }
        }

        /// <summary>
        /// 获取全路径名
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            string name = "";
            for (int i = 0; i < Path.Count; i++)
            {
                if (i == 0)
                {
                    name += "\\" + (PropsSystemTypes)Path[i];
                }
                else
                {
                    name += "\\" + Path[i];
                }
            }

            return name;
        }

        /// <summary>
        /// 添加基础属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void SetBaseProp(int index, double value)
        {
            double change = 0;
            PropsCacheItem parent = ParentPropsCacheItem;
            lock (this)
            {
                change = value - BaseProps[index];
                if (change != 0)
                {
                    BaseProps[index] = value;

                    do 
                    {
                        parent.BaseProps[index] += change;
                        if (null == parent.ParentPropsCacheItem)
                        {
                            parent.Age++;
                            break;
                        }
                        parent = parent.ParentPropsCacheItem;
                    } while (true);
                }
            }
        }

        /// <summary>
        /// 添加基础属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void SetExtProp(int index, double value)
        {
            double change = 0;
            PropsCacheItem parent = ParentPropsCacheItem;
            lock (this)
            {
                change = value - ExtProps[index];
                if (change != 0)
                {
                    ExtProps[index] = value;

                    do
                    {

                        parent.ExtProps[index] += change;
                        if (null == parent.ParentPropsCacheItem)
                        {
                            parent.Age++;
                            break;
                        }
                        parent = parent.ParentPropsCacheItem;
                    } while (true);
                }
            }
        }

        /// <summary>
        /// 清空属性值
        /// </summary>
        public void ResetProps()
        {
            for (int i = 0; i < (int)UnitPropIndexes.Max; i++)
            {
                BaseProps[i] = 0;
            }

            for (int i = 0; i < (int)ExtPropIndexes.Max; i++)
            {
                ExtProps[i] = 0;
            }
        }
    }

    /// <summary>
    /// 精灵Buffer
    /// </summary>
    public class PropsCacheManager
    {
        #region 属性

        /// <summary>
        /// 属性汇总值
        /// </summary>
        private PropsCacheItem PropsCacheRoot = new PropsCacheItem(null, (int)PropsSystemTypes.Total);
        
        /// <summary>
        /// 空的基础属性数组
        /// </summary>
        public static readonly double[] ConstBaseProps = new double[(int)UnitPropIndexes.Max];

        /// <summary>
        /// 空的扩展属性数组
        /// </summary>
        public static readonly double[] ConstExtProps = new double[(int)ExtPropIndexes.Max];

        /// <summary>
        /// 获取当前属性Age
        /// </summary>
        /// <returns></returns>
        public int GetAge()
        {
            lock (PropsCacheRoot)
            {
                return PropsCacheRoot.Age;
            }
        }

        /// <summary>
        /// 自上次调用此方法以后，属性是否有变化
        /// </summary>
        /// <returns></returns>
        public bool IsChanged()
        {
            lock (PropsCacheRoot)
            {
                if (PropsCacheRoot.LastAge != PropsCacheRoot.Age)
                {
                    PropsCacheRoot.LastAge = PropsCacheRoot.Age;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取基础属性的拷贝
        /// </summary>
        /// <returns></returns>
        public double[] getCopyBaseProp()
        {
            double[] baseProps = PropsCacheRoot.BaseProps;
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
            double[] extProps = PropsCacheRoot.ExtProps;
            double[] copyExtProps = new double[extProps.Length];

            for (int i = 0; i < extProps.Length; i++)
            {
                copyExtProps[i] = extProps[i];
            }

            return copyExtProps;
        }

        /// <summary>
        /// 设置基础属性的值(数组)
        /// </summary>
        /// <param name="args"></param>
        public void SetBaseProps(params object[] args)
        {
            PropsCacheItem parent = PropsCacheRoot;
            PropsCacheItem child = null;
            double[] props = null;
            object propsObject = null;

            if (args.Length > 1)
            {
                propsObject = args[args.Length - 1];
                EquipPropItem equipPropItem = args[args.Length - 1] as EquipPropItem;
                if (null != equipPropItem)
                {
                    props = equipPropItem.BaseProps;
                }
                else
                {
                    props = args[args.Length - 1] as double[];
                }
            }

            if (null == props)
            {
                return;
            }

            lock (PropsCacheRoot)
            {
                foreach (var obj in args)
                {
                    if (obj == propsObject)
                    {
                        if (child != null)
                        {
                            Contract.Assert(child.SubPropsItemDict.Count == 0, "only leaf node can set props!");
                            for (int i = 0; i < (int)UnitPropIndexes.Max && i < props.Length; i++)
                            {
                                child.SetBaseProp(i, props[i]);
                            }
                        }
                        break;
                    }
                    else
                    {
                        if (!parent.SubPropsItemDict.TryGetValue((int)obj, out child))
                        {
                            child = new PropsCacheItem(parent, Convert.ToInt32(obj));
                            parent.SubPropsItemDict.Add((int)obj, child);
                        }

                        parent = child;
                    }
                }
            }
        }

        /// <summary>
        /// 设置一级属性的值(单个值)
        /// </summary>
        /// <param name="args">系统类型(int),子系统类型(int),...,属性类型索引(int),属性值(double)</param>
        public void SetBasePropsSingle(params object[] args)
        {
            PropsCacheItem parent = PropsCacheRoot;
            PropsCacheItem child = null;
            double propValue = 0;
            int propIndex = -1;

            try
            {
                if (args.Length <= 2)
                {
                    return;
                }

                propIndex = (int)args[args.Length - 2];
                propValue = Convert.ToDouble(args[args.Length - 1]);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException(ex.ToString());
                return;
            }

            if (propIndex < 0 || propIndex >= (int)UnitPropIndexes.Max)
            {
                return;
            }

            lock (PropsCacheRoot)
            {
                for (int i = 0; i < args.Length - 2; i++)
                {
                    if (!parent.SubPropsItemDict.TryGetValue((int)args[i], out child))
                    {
                        child = new PropsCacheItem(parent, Convert.ToInt32(args[i]));
                        parent.SubPropsItemDict.Add((int)args[i], child);
                    }

                    parent = child;
                }

                if (child != null)
                {
                    Contract.Assert(child.SubPropsItemDict.Count == 0, "only leaf node can set props!");
                    child.SetBaseProp(propIndex, propValue);
                }
            }
        }

        /// <summary>
        /// 设置扩展属性的值(数组)
        /// </summary>
        /// <param name="args">系统类型(int),子系统类型(int),...,属性值数组(double[])</param>
        public void SetExtProps(params object[] args)
        {
            PropsCacheItem parent = PropsCacheRoot;

            PropsCacheItem child = null;
            double[] props = null;
            object propsObject = null;

            try
            {
                if (args.Length > 1)
                {
                    propsObject = args[args.Length - 1];
                    EquipPropItem equipPropItem = propsObject as EquipPropItem;
                    if (null != equipPropItem)
                    {
                        props = equipPropItem.ExtProps;
                    }
                    else
                    {
                        props = args[args.Length - 1] as double[];
                    }
                }

                lock (PropsCacheRoot)
                {
                    foreach (var obj in args)
                    {
                        if (obj == propsObject)
                        {
                            if (child != null)
                            {
                                Contract.Assert(child.SubPropsItemDict.Count == 0, "only leaf node can set props!");
                                if (null != props)
                                {
                                    for (int i = 0; i < (int)ExtPropIndexes.Max && i < props.Length; i++)
                                    {
                                        child.SetExtProp(i, props[i]);
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < (int)ExtPropIndexes.Max; i++)
                                    {
                                        child.SetExtProp(i, 0);
                                    }
                                }
                            }

                            break;
                        }
                        else
                        {
                            if (!parent.SubPropsItemDict.TryGetValue((int)obj, out child))
                            {
                                child = new PropsCacheItem(parent, Convert.ToInt32(obj));
                                parent.SubPropsItemDict.Add((int)obj, child);
                            }

                            parent = child;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// 设置扩展属性的值(数组)
        /// </summary>
        /// <param name="args">系统类型(int),子系统类型(int),...,属性类型索引(int),属性值(double)</param>
        public void SetExtPropsSingle(params object[] args)
        {
            PropsCacheItem parent = PropsCacheRoot;
            PropsCacheItem child = null;
            double propValue = 0;
            int propIndex = -1;

            try
            {
                if (args.Length <= 2)
                {
                    return;
                }

                propIndex = (int)args[args.Length - 2];
                propValue = Convert.ToDouble(args[args.Length - 1]);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException(ex.ToString());
                return;
            }

            if (propIndex < 0 || propIndex >= (int)ExtPropIndexes.Max)
            {
                return;
            }

            lock (PropsCacheRoot)
            {
                for (int i = 0; i < args.Length - 2; i++ )
                {
                    if (!parent.SubPropsItemDict.TryGetValue((int)args[i], out child))
                    {
                        child = new PropsCacheItem(parent, Convert.ToInt32(args[i]));
                        parent.SubPropsItemDict.Add((int)args[i], child);
                    }

                    parent = child;
                }

                if (child != null)
                {
                    Contract.Assert(child.SubPropsItemDict.Count == 0, "only leaf node can set props!");
                    child.SetExtProp(propIndex, propValue);
                }
            }
        }

        /// <summary>
        /// 获取所有的叶节点属性缓存项
        /// </summary>
        /// <returns></returns>
        public List<PropsCacheItem> GetAllPropsCacheItems(PropsCacheItem parent = null)
        {
            List<PropsCacheItem> list = new List<PropsCacheItem>();

            if (null == parent)
            {
                parent = PropsCacheRoot;
            }

            lock (PropsCacheRoot)
            {
                if (parent.SubPropsItemDict.Count > 0)
                {
                    foreach (var item in parent.SubPropsItemDict.Values)
                    {
                        list.AddRange(GetAllPropsCacheItems(item));
                    }
                }
                else
                {
                    list.Add(parent);
                }
            }

            return list;
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
            lock (PropsCacheRoot)
            {
                tempProp = PropsCacheRoot.BaseProps[index];
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

            lock (PropsCacheRoot)
            {
                tempProp = PropsCacheRoot.ExtProps[index];
            }

            return tempProp;
        }

        #endregion 获取Buffer属性
    }
}
