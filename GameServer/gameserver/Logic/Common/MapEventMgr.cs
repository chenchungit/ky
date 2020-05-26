using GameServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 地图事件和状态管理器(动态光幕)
    /// </summary>
    public class MapEventMgr
    {
        /// <summary>
        /// 重进地图需要执行的事件队列
        /// </summary>
        private List<object> EventQueue = new List<object>();

        public void AddGuangMuEvent(int guangMuID, int show)
        {
            MapAIEvent guangMuEvent = new MapAIEvent() { GuangMuID = guangMuID, Show = show };
            lock (EventQueue)
            {
                EventQueue.Add(guangMuEvent);
            }
        }

        /// <summary>
        /// 播放地图事件
        /// </summary>
        /// <param name="client"></param>
        public void PlayMapEvents(GameClient client)
        {
            //重发AI相关的指令队列
            lock (EventQueue)
            {
                foreach (var obj in EventQueue)
                {
                    if (obj is MapAIEvent)
                    {
                        MapAIEvent e = (MapAIEvent)obj;
                        int guangMuID = e.GuangMuID;
                        int show = e.Show;
                        client.sendCmd((int)TCPGameServerCmds.CMD_SPR_MAPAIEVENT, string.Format("{0}:{1}", guangMuID, show));
                    }
                }
            }
        }

        /// <summary>
        /// 清除所有事件
        /// </summary>
        public void ClearAllMapEvents()
        {
            lock (EventQueue)
            {
                EventQueue.Clear();
            }
        }
    }
}
