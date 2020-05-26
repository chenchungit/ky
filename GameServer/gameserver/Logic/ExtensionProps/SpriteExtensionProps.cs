using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.ExtensionProps
{
    /// <summary>
    /// 精灵的扩展属性
    /// </summary>
    public class SpriteExtensionProps
    {
        /// <summary>
        /// 线程锁
        /// </summary>
        private Object Mutex = new Object();

        /// <summary>
        /// 属性ID列表（允许重复）
        /// </summary>
        private List<int> ExtensionPropIDsList = new List<int>();

        /// <summary>
        /// 添加一个ID
        /// </summary>
        /// <param name="id"></param>
        public void AddID(int id)
        {
            lock (Mutex)
            {
                ExtensionPropIDsList.Add(id);
            }
        }

        /// <summary>
        /// 删除一个ID
        /// </summary>
        /// <param name="id"></param>
        public void RemoveID(int id)
        {
            lock (Mutex)
            {
                for (int i = 0; i < ExtensionPropIDsList.Count; i++)
                {
                    if (ExtensionPropIDsList[i] == id)
                    {
                        ExtensionPropIDsList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 获取ID列表的浅层拷贝
        /// </summary>
        /// <param name="id"></param>
        public List<int> GetIDs()
        {
            List<int> list = null;
            lock (Mutex)
            {
                list = ExtensionPropIDsList.GetRange(0, ExtensionPropIDsList.Count);
            }

            return list;
        }
    }
}
