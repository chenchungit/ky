using System;
using System.Net;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Shapes;

namespace GameServer.Interface
{
    /// <summary>
    /// 控件定位的接口(坐标, Z层次, 中心位置)
    /// </summary>
    public interface IPosition
    {
        /// <summary>
        /// 获取或设置中心
        /// </summary>
        Point Center
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置X、Y坐标
        /// </summary>
        Point Coordinate
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置Z层次深度
        /// </summary>
        int Z
        {
            get;
            set;
        }
    }
}
