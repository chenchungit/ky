using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;
using Server.Data;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 公告消息管理
    /// </summary>
    public class BulletinMsgManager
    {
        #region 基础数据

        /// <summary>
        /// 公告字典
        /// </summary>
        private Dictionary<string, BulletinMsgData> _BulletinMsgDict = new Dictionary<string, BulletinMsgData>();

        #endregion 基础数据

        #region 基础方法

        /// <summary>
        /// 从DBServer获取永久的公告数据
        /// </summary>
        public void LoadBulletinMsgFromDBServer()
        {
            /// 从DBserver加载公告
            _BulletinMsgDict = Global.LoadDBBulletinMsgDict();
            if (null == _BulletinMsgDict)
            {
                _BulletinMsgDict = new Dictionary<string, BulletinMsgData>();
            }
        }

        /// <summary>
        /// 发布公告消息
        /// </summary>
        /// <param name="msgID"></param>
        /// <param name="playMinutes"></param>
        /// <param name="playNum"></param>
        /// <param name="bulletinText"></param>
        public BulletinMsgData AddBulletinMsg(string msgID, int playMinutes, int playNum, string bulletinText, int msgType = 0)
        {
            BulletinMsgData bulletinMsgData = new BulletinMsgData()
            {
                MsgID = msgID,
                PlayMinutes = playMinutes,
                ToPlayNum = playNum,
                BulletinText = bulletinText,
                BulletinTicks = TimeUtil.NOW(),
                MsgType = msgType,
            };

            if (playMinutes != 0) //如果公告的留存时间不等于0, -1永久公告, 大于0为限时公告
            {
                lock (_BulletinMsgDict)
                {
                    _BulletinMsgDict[msgID] = bulletinMsgData;
                }
            }

            return bulletinMsgData;
        }

        /// <summary>
        /// 删除公告消息
        /// </summary>
        /// <param name="msgID"></param>
        public BulletinMsgData RemoveBulletinMsg(string msgID)
        {
            BulletinMsgData bulletinMsgData = null;
            lock (_BulletinMsgDict)
            {
                if (_BulletinMsgDict.TryGetValue(msgID, out bulletinMsgData))
                {
                    _BulletinMsgDict.Remove(msgID);
                }
            }

            return bulletinMsgData;
        }

        /// <summary>
        /// 将所有的公告消息发布给指定的客户端
        /// </summary>
        public void SendAllBulletinMsg(GameClient client)
        {
            List<BulletinMsgData> bulletinMsgDataList = new List<BulletinMsgData>();
            lock (_BulletinMsgDict)
            {
                foreach (var key in _BulletinMsgDict.Keys)
                {
                    bulletinMsgDataList.Add(_BulletinMsgDict[key]);
                }
            }

            for (int i = 0; i < bulletinMsgDataList.Count; i++)
            {
                //通知在线的对方(不限制地图)公告消息
                GameManager.ClientMgr.NotifyBulletinMsg(Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, bulletinMsgDataList[i]);
            }
        }

        /// <summary>
        /// 将所有的公告消息枚举给指定的GM客户端
        /// </summary>
        public void SendAllBulletinMsgToGM(GameClient client)
        {            
            BulletinMsgData bulletinMsgData = null;
            List<string> msgList = new List<string>();
            lock (_BulletinMsgDict)
            {
                foreach (var key in _BulletinMsgDict.Keys)
                {
                    bulletinMsgData = _BulletinMsgDict[key];

                    string textMsg = string.Format("{0} {1} {2} {3}",
                        bulletinMsgData.MsgID,
                        bulletinMsgData.PlayMinutes,
                        bulletinMsgData.ToPlayNum,
                        bulletinMsgData.BulletinText);
                    msgList.Add(textMsg);
                }
            }

            for (int i = 0; i < msgList.Count; i++)
            {
                //给某个在线的角色发送系统消息
                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, msgList[i]);
            }
        }

        /// <summary>
        /// 处理过时的系统公告消息
        /// </summary>
        public void ProcessBulletinMsg()
        {
            long ticks = TimeUtil.NOW();
            List<string> bulletinMsgIDList = new List<string>();
            BulletinMsgData bulletinMsgData = null;
            lock (_BulletinMsgDict)
            {
                foreach (var key in _BulletinMsgDict.Keys)
                {
                    bulletinMsgData = _BulletinMsgDict[key];

                    //是永久的公告
                    if (bulletinMsgData.PlayMinutes < 0)
                    {
                        continue;
                    }

                    //还没到删除时间
                    if (ticks - bulletinMsgData.BulletinTicks < (bulletinMsgData.PlayMinutes * 60 * 1000))
                    {
                        continue;
                    }

                    bulletinMsgIDList.Add(key);
                }

                for (int i = 0; i < bulletinMsgIDList.Count; i++)
                {
                    _BulletinMsgDict.Remove(bulletinMsgIDList[i]);
                }

                bulletinMsgIDList.Clear();
                bulletinMsgIDList = null;
            }
        }

        #endregion 基础方法
    }
}
