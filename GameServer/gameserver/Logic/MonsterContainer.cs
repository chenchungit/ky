#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 怪物容器类
    /// </summary>
    class MonsterContainer
    {
        public MonsterContainer()
        {
        }

        public void initialize(IEnumerable<XElement> mapItems)
        {
            foreach (var mapItem in mapItems)
            {
                int mapCode = (int)Global.GetSafeAttributeLong(mapItem, "Code");

                List<object> objList = new List<object>(100);

                _MapObjectDict.Add(mapCode, objList);

                Dictionary<int, object> objDict = new Dictionary<int, object>(100);

                _ObjectDict.Add(mapCode, objDict);

                //如果是新手场景，生成平行管理容器
                if (mapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)
                {
                    for(int i = 0; i < 25; i++)
                    {
                        Dictionary<int, object> freshPlayerObjDict = new Dictionary<int, object>(2000);
                        _FreshPlayerObjectDict.Add(i, freshPlayerObjDict);

                        List<object> freshPlayerObjList = new List<object>(100);
                        _FreshPlayerMapObjectDict.Add(i, freshPlayerObjList);
                    }
                    
                }
            }
        }

        

        /// <summary>
        /// 怪物列表
        /// </summary>
        public List<Object> _ObjectList = new List<Object>(20000);
        //public List<Monster> _ObjectList = new List<Monster>(20000);

        /// <summary>
        /// 怪物列表
        /// </summary>
        public List<Object> ObjectList
        //public List<Monster> ObjectList
        {
            get { return _ObjectList; }
        }

        /// <summary>
        /// 根据地图编号索引的怪物字典对象
        /// </summary>
        private Dictionary<int, Dictionary<int, object>> _ObjectDict = new Dictionary<int, Dictionary<int, object>>(10000);

        /// <summary>
        /// 新手场景平行管理容器
        /// </summary>
        private Dictionary<int, Dictionary<int, object>> _FreshPlayerObjectDict = new Dictionary<int, Dictionary<int, object>>(50);

        /// <summary>
        /// 根据地图编号索引的怪物字典对象
        /// </summary>
        public Dictionary<int, Dictionary<int, object>> ObjectDict
        {
            get { return _ObjectDict; }
        }

        /// <summary>
        /// 根据地图编号索引的怪物字典对象
        /// </summary>
        private Dictionary<int, List<object>> _MapObjectDict = new Dictionary<int, List<object>>(10000);

        /// <summary>
        /// 新手场景平行管理容器
        /// </summary>
        private Dictionary<int, List<object>> _FreshPlayerMapObjectDict = new Dictionary<int, List<object>>(50);

        /// <summary>
        /// 根据地图编号索引的怪物字典对象
        /// </summary>
        public Dictionary<int, List<object>> MapObjectDict
        {
            get { return _MapObjectDict; }
        }

        /// <summary>
        /// 根据副本ID索引的怪物字典对象
        /// </summary>
        private Dictionary<int, List<object>> _CopyMapIDObjectDict = new Dictionary<int, List<object>>(10000);

        /// <summary>
        /// 根据副本ID索引的怪物字典对象
        /// </summary>
        public Dictionary<int, List<object>> CopyMapIDObjectDict
        {
            get { return _CopyMapIDObjectDict; }
        }

        /// <summary>
        /// 添加对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mapCode"></param>
        /// <param name="obj"></param>
        public void AddObject(int id, int mapCode, int copyMapID, Monster obj)
        {
            lock (_ObjectList)
            {
                _ObjectList.Add(obj);
            }

//             //先锁定对象
//             lock (_ObjectDict)
//             {
                Dictionary<int, object> objDict = null;
                if (_ObjectDict.TryGetValue(mapCode, out objDict))
                {
                    lock (objDict)
                    {
                        objDict.Add(id, obj);
                    }

                }

                
//                 else
//                 {
//                     objDict = new Dictionary<int, object>(100);
//                     objDict.Add(id, obj);
//                     _ObjectDict.Add(mapCode, objDict);
//                 }
//            }

//             //先锁定对象
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
//             }

                if (mapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)
                {
                    int subMapCode = Global.GetRandomNumber(0, 24);

                    obj.SubMapCode = subMapCode;

                    List<Object> list = null;

                    if (_FreshPlayerMapObjectDict.TryGetValue(subMapCode, out list))
                    {
                        lock (list)
                        {
                            list.Add(obj);
                        }
                    }

                    Dictionary<int, object> dict = null;

                    if (_FreshPlayerObjectDict.TryGetValue(subMapCode, out dict))
                    {
                        lock (dict)
                        {
                            dict.Add(id, obj);
                        }
                    }
                }

            //先锁定对象
            lock (_CopyMapIDObjectDict)
            {
                List<object> _objList = null;
                if (_CopyMapIDObjectDict.TryGetValue(copyMapID, out _objList))
                {
                    _objList.Add(obj);
                }
                else
                {
                    _objList = new List<object>(100);
                    _objList.Add(obj);
                    _CopyMapIDObjectDict.Add(copyMapID, _objList);
                }
            }

            
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="?"></param>
        public void RemoveObject(int id, int mapCode, int copyMapID, Monster obj)
        {
            lock (_ObjectList)
            {
                _ObjectList.Remove(obj);
            }

//             //先锁定对象
//             lock (_ObjectDict)
//             {
                Dictionary<int, object> objDict = null;
                
                if (_ObjectDict.TryGetValue(mapCode, out objDict))
                {
                    lock (objDict)
                    {
                        objDict.Remove(id);
                    }
                }
//            }

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
                            objList.Remove(obj);
                        }
                        
                    }
                    catch (Exception)
                    {
                    }
                }
//            }

                if (mapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)
                {
                    int subMapCode = obj.SubMapCode ;

                    List<Object> list = null;

                    if (_FreshPlayerMapObjectDict.TryGetValue(subMapCode, out list))
                    {
                        try
                        {
                            lock (list)
                            {
                                list.Remove(obj);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.WriteException(ex.ToString());
                        }
                        
                    }

                    Dictionary<int, object> dict = null;

                    if (_FreshPlayerObjectDict.TryGetValue(subMapCode, out dict))
                    {
                        try
                        {
                            lock (dict)
                            {
                                dict.Remove(id);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.WriteException(ex.ToString());
                        }
                        
                    }
                }

            //先锁定对象
//             lock (_CopyMapIDObjectDict)
//             {
                List<object> _objList = null;
                if (_CopyMapIDObjectDict.TryGetValue(copyMapID, out _objList))
                {
                    try
                    {
                        lock (_objList)
                        {
                            _objList.Remove(obj);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
//            }
        }

        /// <summary>
        /// 获取指定地图上的其他用户(可以选择是否排除自己)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public List<Object> GetObjectsByMap(int mapCode, int subMapCode = -1)
        {
            List<Object> newObjList = null;
//             //先锁定对象
//             lock (_MapObjectDict)
//             {
            List<object> objList = null;

            if (mapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID && subMapCode != -1)
            {
                if (this._FreshPlayerMapObjectDict.TryGetValue(subMapCode, out objList))
                {
                    lock (objList)
                    {
                        newObjList = objList.GetRange(0, objList.Count);
                    }
                }
            }
            else
            {
                if (_MapObjectDict.TryGetValue(mapCode, out objList))
                {
                    lock (objList)
                    {
                        newObjList = objList.GetRange(0, objList.Count);
                    }
                }
            }
                
//            }

            return newObjList;
        }

        /// <summary>
        /// 获取指定地图上的其他用户个数(可以选择是否排除自己)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public int GetObjectsCountByMap(int mapCode)
        {
            int count = 0;

//             //先锁定对象
//             lock (_MapObjectDict)
//             {
                List<object> objList = null;
                if (_MapObjectDict.TryGetValue(mapCode, out objList))
                {
                    lock (objList)
                    {
                        count = objList.Count;
                    }
                }
//            }

            return count;
        }

        /// <summary>
        /// 根据副本ID获取指定地图上的怪物列表
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public List<Object> GetObjectsByCopyMapID(int copyMapID)
        {
            List<Object> newObjList = null;

//             //先锁定对象
//             lock (_CopyMapIDObjectDict)
//             {
                List<object> objList = null;
                if (_CopyMapIDObjectDict.TryGetValue(copyMapID, out objList))
                {
                    lock (objList)
                    {
                        newObjList = objList.GetRange(0, objList.Count);
                    }
                   
                }
//            }

            return newObjList;
        }

        /// <summary>
        /// 根据副本ID获取指定地图上的怪物数量
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public int GetObjectsCountByCopyMapID(int copyMapID, int aliveType = -1)
        {
            int count = 0;

//             //先锁定对象
//             lock (_CopyMapIDObjectDict)
//             {
                List<object> objList = null;
                if (_CopyMapIDObjectDict.TryGetValue(copyMapID, out objList))
                {
                    if (null != objList)
                    {
                        if (-1 == aliveType) //所有
                        {
                            lock (objList)
                            {
                                count = objList.Count;
                            }
                            
                        }
                        else if (0 == aliveType) //活着的
                        {
                            lock (objList)
                            {
                                for (int i = 0; i < objList.Count; i++)
                                {
                                    if ((objList[i] as Monster).VLife > 0 &&
                                        (objList[i] as Monster).Alive 
                                       )
                                    {
                                        count++;
                                    }
                                }
                            }
                            
                        }
                        else //死亡的
                        {
                            lock (objList)
                            {
                                for (int i = 0; i < objList.Count; i++)
                                {
                                    if (!(objList[i] as Monster).Alive)
                                    {
                                        count++;
                                    }
                                }
                            }
                        }
                    }
                }
//            }

            return count;
        }

        /// <summary>
        /// 副本中是否有怪的Alive标志为true
        /// </summary>
        /// <param name="copyMapID"></param>
        /// <returns></returns>
        public bool IsAnyMonsterAliveByCopyMapID(int copyMapID)
        {
//             //先锁定对象
//             lock (_CopyMapIDObjectDict)
//             {
                List<object> objList = null;
                if (_CopyMapIDObjectDict.TryGetValue(copyMapID, out objList))
                {
                    if (null != objList)
                    {
                        lock (objList)
                        {
                            for (int i = 0; i < objList.Count; i++)
                            {
                                if ((objList[i] as Monster).Alive)
                                {
                                    return true;//有怪还活着
                                }
                            }
                        }
                    }
                }
//            }

            return false;//所有怪均死亡
        }

        /// <summary>
        /// 根据ID和地图编号查找对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public object FindObject(int id, int mapCode)
        {
            object obj = null;

//             //先锁定对象
//             lock (_ObjectDict)
//             {
                Dictionary<int, object> objDict = null;
                if (_ObjectDict.TryGetValue(mapCode, out objDict))
                {
                    lock (objDict)
                    {
                        objDict.TryGetValue(id, out obj);
                    }
                }
//            }

            return obj;
        }

        /// <summary>
        /// 根据ID和地图编号查找对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public List<object> FindObjectsByExtensionID(int extensionID, int copyMapID)
        {
            List<object> findObjsList = new List<object>();

            //             //先锁定对象
            //             lock (_CopyMapIDObjectDict)
            //             {
            List<object> objList = null;
            if (_CopyMapIDObjectDict.TryGetValue(copyMapID, out objList))
            {
                if (null != objList)
                {
                    lock (objList)
                    {
                        for (int i = 0; i < objList.Count; i++)
                        {
#if ___CC___FUCK___YOU___BB___
                            if ((objList[i] as Monster).VLife > 0 &&
                                (objList[i] as Monster).Alive &&
                                (objList[i] as Monster).XMonsterInfo.MonsterId == (int)extensionID)
                            {
                                findObjsList.Add(objList[i]);
                            }
#else
                            if ((objList[i] as Monster).VLife > 0 &&
                                (objList[i] as Monster).Alive &&
                                (objList[i] as Monster).MonsterInfo.ExtensionID == (int)extensionID)
                            {
                                findObjsList.Add(objList[i]);
                            }
#endif
                        }
                    }
                }
            }
            //            }

            return findObjsList;//所有怪均死亡
        }
    }
}
