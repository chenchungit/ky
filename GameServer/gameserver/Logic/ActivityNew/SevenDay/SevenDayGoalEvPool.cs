using GameServer.Core.GameEvent.EventOjectImpl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.ActivityNew.SevenDay
{
    /// <summary>
    /// 七日活动的事件会非常频繁，所以把事件对象用pool缓存一下，防止GC压力太大
    /// </summary>
    public static class SevenDayGoalEvPool
    {
        private static object Mutex = new object();
        private static Queue<SevenDayGoalEventObject> freeEvList = new Queue<SevenDayGoalEventObject>();

        public static SevenDayGoalEventObject Alloc(GameClient client, ESevenDayGoalFuncType funcType)
        {
            SevenDayGoalEventObject evObj = null;
            lock (Mutex)
            {
                if (freeEvList.Count > 0)
                {
                    evObj = freeEvList.Dequeue();
                }
            }

            if (evObj == null)
            {
                evObj = new SevenDayGoalEventObject();
            }

            evObj.Reset();
            evObj.Client = client;
            evObj.FuncType = funcType;
            return evObj;
        }

        public static void Free(SevenDayGoalEventObject evObj)
        {
            if (evObj == null) return;

            evObj.Reset();

            lock (Mutex)
            {
                freeEvList.Enqueue(evObj);
            }
        }
    }
}
