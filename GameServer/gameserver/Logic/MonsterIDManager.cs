using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 怪物的ID生成类
    /// </summary>
    public class MonsterIDManager
    {
        /// <summary>
        /// 根据地图分布的怪物的ID
        /// </summary>
        //private Dictionary<int, long> IDsDict = new Dictionary<int, long>(100);

        /// <summary>
        /// 空闲id列表
        /// </summary>
        private List<long> IdleIDList = new List<long>();

        /// <summary>
        /// 最大ID，从某个值开始，表示怪物 最多800万个，由于有还回机制，根本用不完
        /// </summary>
        private long _MaxID = SpriteBaseIds.MonsterBaseId;

        /// <summary>
        /// 获取一个新的基于地图的ID===>地图编号不再使用
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public long GetNewID(int mapCode)
        {
            long id = SpriteBaseIds.MonsterBaseId;
            lock (IdleIDList)
            {
                if (IdleIDList.Count > 0)
                {
                    id = IdleIDList.ElementAt(0);
                    IdleIDList.RemoveAt(0);
                }
                else
                {
                    id = ++_MaxID;
                }
            }

            return id;
        }

        /// <summary>
        /// 还回ID
        /// </summary>
        /// <param name="id"></param>
        public void PushBack(long id)
        {
            lock (IdleIDList)
            {
                if (IdleIDList.IndexOf(id) < 0 && IdleIDList.Count < 10000)
                {
                    IdleIDList.Add(id);
                }
            }

            if (IdleIDList.Count > 10000)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("MonsterIDManager中的空闲ID数达到了{0}", IdleIDList.Count));
            }
        }
        /*
        /// <summary>
        /// 获取一个新的基于地图的ID
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public long GetNewID(int mapCode)
        {
            long id = SpriteBaseIds.MonsterBaseId;
            lock (IDsDict)
            {
                if (IDsDict.TryGetValue(mapCode, out id))
                {
                    id++;
                    IDsDict[mapCode] = id;
                }
                else
                {
                    id = SpriteBaseIds.MonsterBaseId;
                    IDsDict[mapCode] = id;
                }
            }

            return id;
        }
        */
    }
}
