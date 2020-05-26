using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 地图格子管理类(线程可以直接使用, 记住，此处的格子和障碍物的格子不是一个概念)
    /// </summary>
    public class MapGridManager
    {
        public MapGridManager()
        {
        }

        /// <summary>
        /// 根据索引访问地图的对象
        /// </summary>
        private Dictionary<int, MapGrid> _DictGrids = new Dictionary<int, MapGrid>(100);

        /// <summary>
        /// 根据索引访问地图的对象 属性
        /// </summary>
        public Dictionary<int, MapGrid> DictGrids
        {
            get { return _DictGrids; }
        }

        /// <summary>
        /// 初始化添加地图
        /// </summary>
        /// <param name="mapWidth"></param>
        /// <param name="mapHeight"></param>
        /// <param name="gridWidth"></param>
        /// <param name="gridHeight"></param>
        public void InitAddMapGrid(int mapCode, int mapWidth, int mapHeight, int gridWidth, int gridHeight, GameMap gameMap)
        {
            MapGrid mapGrid = new MapGrid(mapCode, mapWidth, mapHeight, gridWidth, gridHeight, gameMap);

            lock (_DictGrids)
            {
                _DictGrids.Add(mapCode, mapGrid);
            }
        }

        public MapGrid GetMapGrid(int mapCode)
        {
            MapGrid mapGrid;
            lock (_DictGrids)
            {
                if (_DictGrids.TryGetValue(mapCode, out mapGrid))
                {
                    return mapGrid;
                }

                return null;
            }
        }

        /// <summary>
        /// 获取所有地图对象引用的人数列表
        /// </summary>
        /// <returns></returns>
        public string GetAllMapClientCountForConsole()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var kv in _DictGrids)
            {
                if (null != kv.Value)
                {
                    int count = kv.Value.GetGridClientCountForConsole();
                    if (count > 0)
                    {
                        sb.AppendFormat("{0}:{1}\n", kv.Key, count);
                    }
                }
            }

            return sb.ToString();
        }
    }
}
