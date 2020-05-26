using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Interface;

namespace GameServer.Logic.NewBufferExt
{
    /// <summary>
    /// Buffer扩展管理
    /// </summary>
    public class BufferExtManager
    {
        /// <summary>
        /// buffer项字典
        /// </summary>
        private Dictionary<int, IBufferItem> BufferItemDict = new Dictionary<int, IBufferItem>();

        /// <summary>
        /// 添加一个Buffer项
        /// </summary>
        /// <param name="id"></param>
        /// <param name="bufferItem"></param>
        public void AddBufferItem(int id, IBufferItem bufferItem)
        {
            lock (BufferItemDict)
            {
                BufferItemDict[id] = bufferItem;
            }
        }

        /// <summary>
        /// 查找一个Buffer项
        /// </summary>
        /// <param name="id"></param>
        public IBufferItem FindBufferItem(int id)
        {
            IBufferItem bufferItem = null;
            lock (BufferItemDict)
            {
                BufferItemDict.TryGetValue(id, out bufferItem);
            }

            return bufferItem;
        }

        /// <summary>
        /// 删除一个Buffer项
        /// </summary>
        /// <param name="id"></param>
        /// <param name="bufferItem"></param>
        public void RemoveBufferItem(int id)
        {
            lock (BufferItemDict)
            {
                BufferItemDict.Remove(id);
            }
        }
    }
}
