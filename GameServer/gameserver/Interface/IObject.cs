using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Shapes;
//using System.Windows.Threading;
using GameServer.Logic;

namespace GameServer.Interface
{
    public enum ExtComponentTypes
    {
        None,
        ManyTimeDamageQueue,
    }

    public interface IObject
    {
        /// <summary>
        /// 对象的类型
        /// </summary>
        ObjectTypes ObjectType
        {
            get;
        }

        /// <summary>
        /// 获取ID
        /// </summary>
        /// <returns></returns>
        int GetObjectID();

        /// <summary>
        /// 最后一次补血补魔的时间
        /// </summary>
        long LastLifeMagicTick { get; set; }

        /// <summary>
        /// 当前所在的格子的坐标
        /// </summary>
        Point CurrentGrid { get; set; }

        /// <summary>
        /// 当前所在的像素坐标
        /// </summary>
        Point CurrentPos { get; set; }

        /// <summary>
        /// 当前所在的地图的编号
        /// </summary>
        int CurrentMapCode { get; }

        /// <summary>
        /// 当前所在的副本地图的ID
        /// </summary>
        int CurrentCopyMapID { get; }

        /// <summary>
        /// 当前的方向
        /// </summary>
        Dircetions CurrentDir { get; set; }

        /// <summary>
        /// 获取扩展组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        T GetExtComponent<T>(ExtComponentTypes type) where T : class;
    }
}
