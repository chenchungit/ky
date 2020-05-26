using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Server.Data;
using KF.Contract.Data;
using Tmsk.Contract;

namespace GameServer.Logic
{
    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class KuaFuMapData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();

        /// <summary>
        /// 跨服主线地图的线路状态信息字典
        /// </summary>
        public ConcurrentDictionary<IntPairKey, KuaFuLineData> LineMap2KuaFuLineDataDict = new ConcurrentDictionary<IntPairKey, KuaFuLineData>();

        /// <summary>
        /// 跨服主线地图的线路状态信息字典
        /// </summary>
        public ConcurrentDictionary<IntPairKey, KuaFuLineData> ServerMap2KuaFuLineDataDict = new ConcurrentDictionary<IntPairKey, KuaFuLineData>();

        /// <summary>
        /// 包含跨服主线地图的跨服服务器ID
        /// </summary>
        public ConcurrentDictionary<int, List<KuaFuLineData>> KuaFuMapServerIdDict = new ConcurrentDictionary<int, List<KuaFuLineData>>();

        /// <summary>
        /// 地图编号对应的跨服主线地图数据
        /// </summary>
        public ConcurrentDictionary<int, List<KuaFuLineData>> MapCode2KuaFuLineDataDict = new ConcurrentDictionary<int, List<KuaFuLineData>>();
    }
}
