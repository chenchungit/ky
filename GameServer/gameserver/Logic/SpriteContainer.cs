using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Server.Tools;
using Tmsk.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 精灵容器类
    /// </summary>
    public class SpriteContainer
    {
        public SpriteContainer()
        {
        }

        public void initialize(IEnumerable<XElement> mapItems)
        {
            foreach (var mapItem in mapItems)
            {
                int mapCode = (int)Global.GetSafeAttributeLong(mapItem, "Code");

                List<object> objList = new List<object>(100);

                _MapObjectDict.Add(mapCode, objList);
            }
        }

        /// <summary>
        /// 根据对象ID索引的精灵字典对象
        /// </summary>
        private Dictionary<int, object> _ObjectDict = new Dictionary<int, object>(1000);

        /// <summary>
        /// 根据对象ID索引的精灵字典对象
        /// </summary>
        public Dictionary<int, object> ObjectDict
        {
            get { return _ObjectDict; }
        }

        /// <summary>
        /// 根据地图编号索引的精灵字典对象
        /// </summary>
        private Dictionary<int, List<object>> _MapObjectDict = new Dictionary<int, List<object>>(1000);

        /// <summary>
        /// 根据地图编号索引的精灵字典对象
        /// </summary>
        public Dictionary<int, List<object>> MapObjectDict
        {
            get { return _MapObjectDict; }
        }

        /// <summary>
        /// 添加对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mapCode"></param>
        /// <param name="obj"></param>
        public void AddObject(int id, int mapCode, object obj)
        {
            //先锁定对象
            lock (_ObjectDict)
            {
                _ObjectDict.Add(id, obj);
            }

            //先锁定对象
//             lock (_MapObjectDict)
//             {
                List<object> objList = null;
                if (_MapObjectDict.TryGetValue(mapCode, out objList))
                {
                    lock (objList)
                    {
                        objList.Add(obj);
                    }
                    
                }
//                 else
//                 {
//                     objList = new List<object>(100);
//                     objList.Add(obj);
//                     _MapObjectDict.Add(mapCode, objList);
//                 }
            //}
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="?"></param>
        public bool RemoveObject(int id, int mapCode, object obj)
        {
            bool removed = false;

            //先锁定对象
            lock (_ObjectDict)
            {
                try
                {
                    if (_ObjectDict.ContainsKey(id))
                    {
                        _ObjectDict.Remove(id);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(ex.ToString());
                }
            }

//             //先锁定对象
//             lock (_MapObjectDict)
//             {
                List<object> objList = null;
                if (_MapObjectDict.TryGetValue(mapCode, out objList))
                {
                    try
                    {
                        lock (objList)
                        {
                            removed = objList.Remove(obj);
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteException(ex.ToString());
                    }
                }
/*            }*/

            return removed;
        }

        /// <summary>
        /// 获取指定地图上的其他用户(可以选择是否排除自己)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public List<Object> GetObjectsByMap(int mapCode)
        {
            List<Object> newObjList = null;

//             //先锁定对象
//             lock (_MapObjectDict)
//             {
                List<object> objList = null;
                if (_MapObjectDict.TryGetValue(mapCode, out objList))
                {
                    lock (objList)
                    {
                        newObjList = objList.GetRange(0, objList.Count);
                    }
                }
/*            }*/

            return newObjList;
        }

        /// <summary>
        /// 获取指定地图上的其他用户的个数
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public int GetObjectsCountByMap(int mapCode)
        {
            int count = 0;

            //先锁定对象
//             lock (_MapObjectDict)
//             {
                List<object> objList = null;
                if (_MapObjectDict.TryGetValue(mapCode, out objList))
                {
                    lock(objList)
                    {
                        count = objList.Count;
                    }
                }
/*            }*/

            return count;
        }

        /// <summary>
        /// 根据ID查找一个对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Object FindObject(int id)
        {
            Object obj = null;

            //先锁定对象
            lock (_ObjectDict)
            {
                _ObjectDict.TryGetValue(id, out obj);
            }

            return obj;
        }

        /// <summary>
        /// 获取所有地图和人数的列表
        /// </summary>
        /// <returns></returns>
        public string GetAllMapRoleNumStr()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kv in _MapObjectDict)
            {
                lock (kv.Value)
                {
                    if (kv.Value.Count > 0)
                    {
                        sb.AppendFormat("{0}:{1}\n", kv.Key, kv.Value.Count);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
