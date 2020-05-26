using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Tools.Pattern;
using GameServer.Core.Executor;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;

namespace GameServer.Logic
{
    public enum EInterestingIndex
    {
        Speed = 0,
        Task = 1,
        Max,
    }

    public class InterestingData
    {
        public class Item
        {
            public int RequestCount = 0;
            public int ResponseCount = 0;
            public long LastRequestMs = 0;
            public long LastResponseMs = 0;
            public int InvalidCount = 0;
        }

        public Item[] itemArray = null;

        public InterestingData()
        {
            itemArray = new Item[(int)EInterestingIndex.Max];
            for (int i = 0; i < (int)EInterestingIndex.Max; ++i)
            {
                itemArray[i] = new Item();
            }
        }

        #region 获取得到的数据
        public double Speed = 0;
        #endregion
    }

    class GetInterestingDataMgr : SingletonTemplate<GetInterestingDataMgr>
    {
        private GetInterestingDataMgr() 
        {
        }

        public void LoadConfig()
        {
        }

        public void Update(GameClient client)
        {
            
        }


    }
}
