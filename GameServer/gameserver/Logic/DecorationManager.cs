using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;
using System.Xml;
using System.Xml.Linq;
using GameServer.Interface;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// Decoration 对象---稍后再扩展成从 IObject 继承
    /// </summary>
    public class Decoration : IObject
    {
        public Decoration()
        {
        }

        /// <summary>
        /// 特效的流水ID
        /// </summary>
        public int AutoID;

        /// <summary>
        /// 特效编号
        /// </summary>
        public int DecoID;

        /// <summary>
        /// 所在地图编号
        /// </summary>
        public int MapCode = -1;

        /// <summary>
        /// 所在位置，格子坐标点
        /// </summary>
        public Point Pos;

        /// <summary>
        /// 副本地图ID
        /// </summary>
        public int CopyMapID = -1;

        /// <summary>
        /// 开始时间
        /// </summary>
        public long StartTicks = 0;

        /// <summary>
        /// 存活时间
        /// </summary>
        public int MaxLiveTicks = 0;

        /// <summary>
        /// 开始变透明的时间(0, 和100都表示无效)
        /// </summary>
        public int AlphaTicks = 0;

        #region 实现IObject接口方法

        /// <summary>
        /// 对象的类型
        /// </summary>
        public ObjectTypes ObjectType
        {
            get { return ObjectTypes.OT_DECO; }
        }

        /// <summary>
        /// 获取ID
        /// </summary>
        /// <returns></returns>
        public int GetObjectID()
        {
            return AutoID;
        }

        /// <summary>
        /// 最后一次补血补魔的时间
        /// </summary>
        public long LastLifeMagicTick { get; set; }

        /// <summary>
        /// 当前所在的格子的X坐标
        /// </summary>
        public Point CurrentGrid
        {
            get
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.MapCode];
                return new Point((int)(this.Pos.X / gameMap.MapGridWidth), (int)(this.Pos.Y / gameMap.MapGridHeight));
            }

            set
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.MapCode];
                this.Pos = new Point((int)(value.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2), (int)(value.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2));
            }
        }

        /// <summary>
        /// 当前所在的像素坐标
        /// </summary>
        public Point CurrentPos
        {
            get
            {
                return this.Pos;
            }

            set
            {
                this.Pos = value;
            }
        }

        /// <summary>
        /// 当前所在的地图的编号
        /// </summary>
        public int CurrentMapCode
        {
            get
            {
                return this.MapCode;
            }
        }

        /// <summary>
        /// 当前所在的副本地图的ID
        /// </summary>
        public int CurrentCopyMapID
        {
            get
            {
                return this.CopyMapID;
            }
        }

        /// <summary>
        /// 当前的方向
        /// </summary>
        public Dircetions CurrentDir
        {
            get;
            set;
        }

        #region 扩展接口

        public T GetExtComponent<T>(ExtComponentTypes type) where T : class
        {
            return default(T);
        }

        #endregion 扩展接口

        #endregion 实现IObject接口方法
    }

    public class DecorationManager
    {
        #region 基本操作

        /// <summary>
        /// 自动增长的DecoID
        /// </summary>
        public static int AutoDecoID = 1;

        /// <summary>
        /// 特效 映射列表
        /// </summary>
        public static Dictionary<int, Decoration> DictDecos = new Dictionary<int, Decoration>();

        /// <summary>
        /// 加入一个新的特效
        /// </summary>
        /// <param name="myNpc"></param>
        /// <returns></returns>
        public static bool AddDecoToMap(int mapCode, int copyMapID, Point pos, int decoID, int maxLiveTicks, int alphaTicks, bool notifyClients)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            if (null == mapGrid)
            {
                return false;
            }

            Decoration deco = new Decoration()
            {
                AutoID = AutoDecoID++,
                DecoID = decoID,
                MapCode = mapCode,
                CopyMapID = copyMapID,
                Pos =  pos,
                StartTicks = TimeUtil.NOW(),
                MaxLiveTicks = maxLiveTicks,
                AlphaTicks = alphaTicks,
            };

            lock (DictDecos)
            {
                DictDecos[deco.AutoID] = deco;
            }

            //加入到地图格子中
            mapGrid.MoveObject(-1, -1, (int)pos.X, (int)pos.Y, deco);

            //通知一下周围的人
            if (notifyClients)
            {
                //对于需要及时通知的，立即刷新
                NotifyNearClientsToAddSelf(deco);
            }

            return false;
        }

        /// <summary>
        /// 获取一个Decoration对象
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="npcID"></param>
        /// <returns></returns>
        public static Decoration GetDecoration(int mapCode, int copyMapID, Point pos, int decoID, int maxLiveTicks, int alphaTicks)
        {
            Decoration deco = new Decoration()
            {
                AutoID = AutoDecoID++,
                DecoID = decoID,
                MapCode = mapCode,
                CopyMapID = copyMapID,
                Pos = pos,
                StartTicks = TimeUtil.NOW(),
                MaxLiveTicks = maxLiveTicks,
                AlphaTicks = alphaTicks,
            };

            return deco;
        }

        /// <summary>
        /// 根据ID查找
        /// </summary>
        /// <param name="autoID"></param>
        /// <returns></returns>
        public static Decoration FindDeco(int autoID)
        {
            Decoration deco = null;
            lock (DictDecos)
            {
                if (DictDecos.TryGetValue(autoID, out deco))
                {
                    DictDecos.Remove(autoID);
                }
            }

            return deco;
        }

        /// <summary>
        /// 移除某个某个Deco
        /// </summary>
        /// <param name="mapCode"></param>
        public static void RemoveDeco(int autoID)
        {
            Decoration deco = null;
            lock (DictDecos)
            {
                if (DictDecos.TryGetValue(autoID, out deco))
                {
                    DictDecos.Remove(autoID);
                }
            }

            if (null != deco)
            {
                MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[deco.MapCode];
                if (null != mapGrid)
                {
                    //九宫格地图中移除
                    mapGrid.RemoveObject(deco);

                    //先移除，再通知
                    //NotifyNearClientsToRemoveSelf(deco);
                }
            }
        }

        #endregion 基本操作

        #region 通知客户端

        /// <summary>
        /// 通知自己周围的玩家移除自己,用于动态更新deco
        /// </summary>
        /// <param name="npc"></param>
        //protected static void NotifyNearClientsToRemoveSelf(Decoration deco)
        //{
        //    List<Object> objsList = Global.GetAll9Clients2(deco.MapCode, (int)deco.Pos.X, (int)deco.Pos.Y, deco.CopyMapID);
        //    if (null == objsList) return;

        //    GameClient client = null;
        //    for (int i = 0; i < objsList.Count; i++)
        //    {
        //        client = objsList[i] as GameClient;
        //        if (null == client)
        //        {
        //            continue;
        //        }

        //        GameManager.ClientMgr.NotifyMySelfDelDeco(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, deco);
        //    }
        //}

        /// <summary>
        /// 通知自己周围的玩家移除自己,用于动态更新deco
        /// </summary>
        /// <param name="npc"></param>
        protected static void NotifyNearClientsToAddSelf(Decoration deco)
        {
            //List<Object> objsList = Global.GetAll9Clients2(deco.MapCode, (int)deco.Pos.X, (int)deco.Pos.Y, deco.CopyMapID);
            List<Object> objsList = Global.GetAll9Clients(deco);

            if (null == objsList) return;

            GameClient client = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                client = objsList[i] as GameClient;
                if (null == client)
                {
                    continue;
                }

                ///强迫加入可见缓存，防止再次通知客户端
                lock (client.ClientData.VisibleGrid9Objects)
                {
                    client.ClientData.VisibleGrid9Objects[client] = 1;
                }

                GameManager.ClientMgr.NotifyMySelfNewDeco(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, deco);
            }
        }

        /// <summary>
        /// 将自己周围的Deco发送给自己
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="objsList"></param>
        public static void SendMySelfDecos(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            Decoration deco = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                deco = objsList[i] as Decoration;
                if (null == deco)
                {
                    continue;
                }

                GameManager.ClientMgr.NotifyMySelfNewDeco(sl, pool, client, deco);
            }
        }

        /// <summary>
        /// 通知自己删除周围的Deco
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="objsList"></param>
        public static void DelMySelfDecos(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            Decoration deco = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                deco = objsList[i] as Decoration;
                if (null == deco)
                {
                    continue;
                }

                GameManager.ClientMgr.NotifyMySelfDelDeco(sl, pool, client, deco);
            }
        }

        #endregion 通知客户端

        #region 处理超时

        /// <summary>
        /// 处理已经掉落的特效超时
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessAllDecos(SocketListener sl, TCPOutPacketPool pool)
        {
            List<Decoration> decorationList = new List<Decoration>();
            lock (DictDecos)
            {
                foreach (var val in DictDecos.Values)
                {
                    decorationList.Add(val);
                }
            }

            long nowTicks = TimeUtil.NOW();

            Decoration decoration = null;
            for (int i = 0; i < decorationList.Count; i++)
            {
                decoration = decorationList[i];
                if (decoration.MaxLiveTicks <= 0) //永久特效，不删除
                {
                    continue;
                }

                //判断是否超过了最大的抢时间
                if (nowTicks - decoration.StartTicks < (decoration.MaxLiveTicks)) //删除
                {
                    continue;
                }

                //从内存中删除
                lock (DictDecos) //先锁定
                {
                    DictDecos.Remove(decoration.AutoID);
                }

                GameManager.MapGridMgr.DictGrids[decoration.MapCode].RemoveObject(decoration);

                List<Object> objList = Global.GetAll9Clients(decoration);

                // 特效消失通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersDelDeco(sl, pool, objList, decoration.MapCode, decoration.AutoID);
            }
        }

        #endregion 处理超时
    }
}
