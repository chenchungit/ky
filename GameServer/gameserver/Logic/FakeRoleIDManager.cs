using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 假人ID的生成
    /// </summary>
    public class FakeRoleIDManager
    {
        /// <summary>
        /// 线程锁对象
        /// </summary>
        private Object Mutex = new Object();

        /// <summary>
        /// 起始的ID值
        /// </summary>
        private long BaseID = SpriteBaseIds.FakeRoleBaseId;

        /// <summary>
        /// 插入的帮旗的ID
        /// </summary>
        private Queue<long> IDsQueue = new Queue<long>(1000);

        /// <summary>
        /// 获取一个新的基于ID
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public long GetNewID()
        {
            lock (Mutex)
            {
                if (IDsQueue.Count > 0)
                {
                    return IDsQueue.Dequeue();
                }

                long id = BaseID;
                BaseID++;
                return id;
            }
        }

        /// <summary>
        /// 回收旧的ID
        /// </summary>
        /// <param name="id"></param>
        public void PushID(long id)
        {
            lock (Mutex)
            {
                IDsQueue.Enqueue(id);
            }
        }
    }
}
