using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 地图管理(线程间可以直接使用)
    /// </summary>
    public class MapManager
    {
        public MapManager()
        {
        }

        /// <summary>
        /// 根据索引访问地图的对象
        /// </summary>
        private Dictionary<int, GameMap> _DictMaps = new Dictionary<int, GameMap>(10);

        /// <summary>
        /// 根据索引访问地图的对象 属性
        /// </summary>
        public Dictionary<int, GameMap> DictMaps
        {
            get { return _DictMaps; }
        }

        /// <summary>
        /// 初始化添加地图
        /// </summary>
        /// <param name="mapWidth"></param>
        /// <param name="mapHeight"></param>
        /// <param name="gridWidth"></param>
        /// <param name="gridHeight"></param>
        public GameMap InitAddMap(int mapCode, int mapPicCode, int mapWidth, int mapHeight, int birthPosX, int birthPosY, int birthRadius)
        {
            GameMap gameMap = new GameMap()
            {
                MapCode = mapCode,
                MapPicCode = mapPicCode,
                MapWidth = mapWidth,
                MapHeight = mapHeight,
                DefaultBirthPosX = birthPosX,
                DefaultBirthPosY = birthPosY,
                BirthRadius = birthRadius,
            };

            gameMap.InitMap();

            lock (_DictMaps)
            {
                _DictMaps.Add(mapCode, gameMap);
            }

            return gameMap;
        }

        /// <summary>
        /// 获取指定地图的GameMap
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public GameMap GetGameMap(int mapCode)
        {
            GameMap gameMap;
            if (_DictMaps.TryGetValue(mapCode, out gameMap) && gameMap != null)
            {
                return gameMap;
            }
            return null;
        }
    }
}
