using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 物品交易管理
    /// </summary>
    public class GoodsExchangeManager
    {
        #region 物品交易流水ID

        /// <summary>
        /// 基础的物品交易流水ID
        /// </summary>
        private long BaseAutoID = 0;

        /// <summary>
        /// 获取下一个掉落的物品ID
        /// </summary>
        /// <returns></returns>
        public int GetNextAutoID()
        {
            return (int)(Interlocked.Increment(ref BaseAutoID) & 0x7fffffff);
        }

        #endregion 物品交易流水ID

        #region 物品交易项管理

        /// <summary>
        ///  物品交易项字典
        /// </summary>
        private Dictionary<int, ExchangeData> _GoodsExchangeDict = new Dictionary<int, ExchangeData>();

        /// <summary>
        /// 添加项
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public void AddData(int exchangeID, ExchangeData ed)
        {
            lock (_GoodsExchangeDict)
            {
                _GoodsExchangeDict[exchangeID] = ed;
            }
        }

        /// <summary>
        /// 删除项
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public void RemoveData(int exchangeID)
        {
            lock (_GoodsExchangeDict)
            {
                if (_GoodsExchangeDict.ContainsKey(exchangeID))
                {
                    _GoodsExchangeDict.Remove(exchangeID);
                }
            }
        }

        /// <summary>
        /// 查找项
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ed"></param>
        public ExchangeData FindData(int exchangeID)
        {
            ExchangeData ed = null;
            lock (_GoodsExchangeDict)
            {
                _GoodsExchangeDict.TryGetValue(exchangeID, out ed);
            }

            return ed;
        }

        #endregion 物品交易项管理
    }
}
