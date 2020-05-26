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

namespace GameServer.Logic
{
    /// <summary>
    /// NPC 对象---稍后再扩展成从 IObject 继承
    /// </summary>
    public class NPC : IObject
    {
        public NPC()
        {
        }

        /// <summary>
        /// NPC的角色ID
        /// </summary>
        public int NpcID;

        /// <summary>
        /// 所在地图编号
        /// </summary>
        public int MapCode = -1;

        /// <summary>
        /// 所在位置，格子坐标点
        /// </summary>
        public Point GridPoint;

        /// <summary>
        /// 副本地图ID
        /// </summary>
        public int CopyMapID = -1;

        /// <summary>
        /// 角色缓冲数据，需要动态修改角色信息时，可动态修改这个缓冲区数据
        /// </summary>
        public byte[] RoleBufferData = null;

        #region 实现IObject接口方法

        /// <summary>
        /// 对象的类型
        /// </summary>
        public ObjectTypes ObjectType
        {
            get { return ObjectTypes.OT_NPC; }
        }

        /// <summary>
        /// 获取ID
        /// </summary>
        /// <returns></returns>
        public int GetObjectID()
        {
            return NpcID;
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
                return this.GridPoint;
            }

            set
            {
                this.GridPoint = value;
            }
        }

        /// <summary>
        /// 当前所在的像素坐标
        /// </summary>
        private Point _CurrentPos = new Point(0, 0);

        /// <summary>
        /// 当前所在的像素坐标
        /// </summary>
        public Point CurrentPos
        {
            //get
            //{
            //    GameMap gameMap = GameManager.MapMgr.DictMaps[this.MapCode];
            //    return new Point((int)(this.GridPoint.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2), (int)(this.GridPoint.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2));
            //}

            //set
            //{
            //    GameMap gameMap = GameManager.MapMgr.DictMaps[this.MapCode];
            //    this.GridPoint = new Point((int)(value.X / gameMap.MapGridWidth), (int)(value.Y / gameMap.MapGridHeight));
            //}

            get
            {
                return _CurrentPos;
            }

            set
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.MapCode];
                this.GridPoint = new Point((int)(value.X / gameMap.MapGridWidth), (int)(value.Y / gameMap.MapGridHeight));
                _CurrentPos = value;
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

        #endregion 实现IObject接口方法

        /// <summary>
        /// 是否显示npc
        /// </summary>
        public bool ShowNpc = true;

        #region 扩展接口

        public T GetExtComponent<T>(ExtComponentTypes type) where T : class
        {
            return default(T);
        }

        #endregion 扩展接口
    }

    public class NPCGeneralManager
    {
        /// <summary>
        /// npc 映射列表，key值是 MapCode_gridX_gridY ,value 是 npc对象
        /// </summary>
        public static Dictionary<String, NPC> ListNpc = new Dictionary<string, NPC>();

        /// <summary>
        /// 重新加载某个地图的npc，这儿必须更新一下缓存的配置文件
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public static Boolean ReloadMapNPCRoles(int mapCode)
        {
            string fileName = string.Format("Map/{0}/npcs.xml", mapCode);

            GeneralCachingXmlMgr.Reload(Global.ResPath(fileName));

            GameManager.SystemNPCsMgr.ReloadLoadFromXMlFile();

            GameMap gameMap = GameManager.MapMgr.DictMaps[mapCode];
            return LoadMapNPCRoles(mapCode, gameMap);
        }

        /// <summary>
        /// 加载某地图的所有npc角色，将该地图的所有NPC配置到相应的格子坐标中,
        /// 玩家移动的时候，动态推送到客户端
        /// </summary>
        /// <param name="mapCode"></param>
        public static Boolean LoadMapNPCRoles(int mapCode, GameMap gameMap)
        {
            string fileName = string.Format("Map/{0}/npcs.xml", mapCode);
            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.ResPath(fileName));
            if (null == xml)
            {
                return false;
            }

            IEnumerable<XElement> items = xml.Elements("NPCs").Elements();
            foreach (var item in items)
            {
                NPC myNpc = new NPC();

                myNpc.NpcID = Convert.ToInt32((string)item.Attribute("Code"));
                //myNpc.GridPoint.X = Convert.ToInt32((string)item.Attribute("X")) / gameMap.MapGridWidth;
                //myNpc.GridPoint.Y = Convert.ToInt32((string)item.Attribute("Y")) / gameMap.MapGridHeight;
                myNpc.MapCode = mapCode;
                myNpc.CurrentPos = new Point(Convert.ToInt32((string)item.Attribute("X")), Convert.ToInt32((string)item.Attribute("Y")));                
                //myNpc.CurrentDir = (Dircetions)Global.GetSafeAttributeLong(item, "Dir");

                if (item.Attribute("Dir") != null)
                {
                    myNpc.CurrentDir = (Dircetions)Global.GetSafeAttributeLong(item, "Dir");
                }
                else
                {
                    myNpc.CurrentDir = (Dircetions)4;
                }

                //将推送给客户端的数据缓存起来，以后直接取出发送就行
                myNpc.RoleBufferData = GenerateNpcRoleBufferData(myNpc);

                if (null == myNpc.RoleBufferData)
                {
                    continue;
                    //LogManager.WriteLog(LogTypes.Error, string.Format("加载地图{0}的({1}, {2})处旧的NPC数据失败", myNpc.MapCode, myNpc.GridPoint.X, myNpc.GridPoint.Y));
                    //throw new Exception("NPC配置数据出错");
                }

                AddNpcToMap(myNpc);

                //为地图中的某点设置安全区
                int safeGridNum = 2;
                SystemXmlItem npcXmlItem;
                if (GameManager.SystemNPCsMgr.SystemXmlItemDict.TryGetValue(myNpc.NpcID, out npcXmlItem))
                {
                    safeGridNum = npcXmlItem.GetIntValue("IsSafe");
                }

                if (safeGridNum > 0)
                {
                    gameMap.SetPartialSafeRegion(myNpc.GridPoint, safeGridNum);
                }
            }

            return true;
        }

        /// <summary>
        /// 获取一个NPC对象
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="npcID"></param>
        /// <returns></returns>
        public static NPC GetNPCFromConfig(int mapCode, int npcID, int toX, int toY, int dir)
        {
            SystemXmlItem systemNPCItem = null;
            if (!GameManager.SystemNPCsMgr.SystemXmlItemDict.TryGetValue(npcID, out systemNPCItem))
            {
                return null;
            }

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out gameMap))
            {
                return null;
            }

            NPC myNpc = new NPC();
            myNpc.NpcID = npcID;
            //myNpc.GridPoint.X = toX / gameMap.MapGridWidth;
            //myNpc.GridPoint.Y = toY / gameMap.MapGridHeight;
            myNpc.MapCode = mapCode;
            myNpc.CurrentPos = new Point(toX, toY);
            myNpc.CurrentDir = (Dircetions)dir;

            //将推送给客户端的数据缓存起来，以后直接取出发送就行
            myNpc.RoleBufferData = GenerateNpcRoleBufferData(myNpc);

            if (null == myNpc.RoleBufferData)
            {
                return null;
            }

            return myNpc; 
        }

        /// <summary>
        /// NPC的角色ID
        /// </summary>
        public static Object mutexAddNPC = new Object();

        /// <summary>
        /// 将新NPC加入地图格子,如果该地图的该格子存在旧的NPC，则将旧的NPC移除
        /// 这样既可以初始化npc，也可以替换旧的npc
        /// </summary>
        /// <param name="myNpc"></param>
        /// <returns></returns>
        public static bool AddNpcToMap(NPC myNpc)
        {
            MapGrid mapGrid;
            lock (GameManager.MapGridMgr.DictGrids)
            {
                mapGrid = GameManager.MapGridMgr.GetMapGrid(myNpc.MapCode);
            }

            if (null == mapGrid)
            {
                return false;
            }

            lock (mutexAddNPC)
            {
                String sNpcKey = String.Format("{0}_{1}_{2}", myNpc.MapCode, myNpc.GridPoint.X, myNpc.GridPoint.Y);

                //如果该格子存在别的NPC，则将旧的NPC移除
                NPC oldNpc = null;

                if (ListNpc.TryGetValue(sNpcKey, out oldNpc))
                {
                    ListNpc.Remove(sNpcKey);
                    mapGrid.RemoveObject(oldNpc);

                    //先移除，再通知
                    //NotifyNearClientsToRemoveSelf(oldNpc);

                    LogManager.WriteLog(LogTypes.Error, string.Format("地图{0}的({1}, {2})处旧的NPC被替换", myNpc.MapCode, myNpc.GridPoint.X, myNpc.GridPoint.Y));
                }

                GameMap gameMap = GameManager.MapMgr.DictMaps[myNpc.MapCode];

                //再添加新NPC
                if (mapGrid.MoveObject(-1, -1, (int)(gameMap.MapGridWidth * myNpc.GridPoint.X), (int)(gameMap.MapGridHeight * myNpc.GridPoint.Y), myNpc))
                {
                    ListNpc.Add(sNpcKey, myNpc);

                    //通知一下周围的人
                    //NotifyNearClientsToAddSelf(myNpc);

                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 移除某个地图的所有npc
        /// </summary>
        /// <param name="mapCode"></param>
        public static void RemoveMapNpcs(int mapCode)
        {
            if (mapCode <= 0)
            {
                return;
            }

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];

            if (null == mapGrid)
            {
                return;
            }

            List<String> keysToDel = new List<string>();

            //先循环进行地图移除通知
            foreach (var item in ListNpc)
            {
                if (item.Value.MapCode == mapCode)
                {
                    //九宫格地图中移除
                    mapGrid.RemoveObject(item.Value);

                    //先移除，再通知
                    //NotifyNearClientsToRemoveSelf(item.Value);

                    keysToDel.Add(item.Key);
                }
            }

            //再移除缓存列表
            foreach (var key in keysToDel)
            {
                ListNpc.Remove(key);
            }
        }

        /// <summary>
        /// 移除某个地图的某个npc
        /// </summary>
        /// <param name="mapCode"></param>
        public static void RemoveMapNpc(int mapCode, int npcID)
        {
            if (mapCode <= 0)
            {
                return;
            }

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];

            if (null == mapGrid)
            {
                return;
            }

            //先循环进行地图移除通知
            foreach (var item in ListNpc)
            {
                if (item.Value.MapCode == mapCode && item.Value.NpcID == npcID)
                {
                    //九宫格地图中移除
                    mapGrid.RemoveObject(item.Value);

                    //先移除，再通知
                    //NotifyNearClientsToRemoveSelf(item.Value);

                    ListNpc.Remove(item.Key);

                    return;
                }
            }
        }

        /// <summary>
        /// 通知自己周围的玩家移除自己,用于动态更新npc
        /// </summary>
        /// <param name="npc"></param>
        //protected static void NotifyNearClientsToRemoveSelf(NPC npc)
        //{
        //    List<Object> objsList = Global.GetAll9ClientsEx(npc.MapCode, (int)npc.GridPoint.X, (int)npc.GridPoint.Y, npc.CopyMapID);

        //    if (null == objsList) return;

        //    GameClient client = null;
        //    for (int i = 0; i < objsList.Count; i++)
        //    {
        //        client = objsList[i] as GameClient;
        //        if (null == client)
        //        {
        //            continue;
        //        }

        //        GameManager.ClientMgr.NotifyMySelfDelNPC(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, npc);
        //    }
        //}

        /// <summary>
        /// 通知自己周围的玩家移除自己,用于动态更新npc
        /// </summary>
        /// <param name="npc"></param>
        //protected static void NotifyNearClientsToAddSelf(NPC npc)
        //{
        //    List<Object> objsList = Global.GetAll9ClientsEx(npc.MapCode, (int)npc.GridPoint.X, (int)npc.GridPoint.Y, npc.CopyMapID);

        //    if (null == objsList) return;

        //    GameClient client = null;
        //    for (int i = 0; i < objsList.Count; i++)
        //    {
        //        client = objsList[i] as GameClient;
        //        if (null == client)
        //        {
        //            continue;
        //        }

        //        GameManager.ClientMgr.NotifyMySelfNewNPC(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, npc);
        //    }
        //}

        /// <summary>
        /// 根据地图编码 和 npc角色ID 获取npc对象
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static NPC FindNPC(int mapCode, int npcID)
        {
            foreach (var item in ListNpc)
            {
                if (item.Value.MapCode == mapCode && item.Value.NpcID == npcID)
                {
                    return item.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// 生成NPC角色数据
        /// </summary>
        /// <param name="myNpc"></param>
        /// <returns></returns>
        public static Byte[] GenerateNpcRoleBufferData(NPC myNpc)
        {
            SystemXmlItem systemNPC = null;
            if (!GameManager.SystemNPCsMgr.SystemXmlItemDict.TryGetValue(myNpc.NpcID, out systemNPC))
            {
                return null;
            }

            NPCRole npcRole = new NPCRole();

            npcRole.NpcID = myNpc.NpcID;
            npcRole.PosX = (int)myNpc.CurrentPos.X;
            npcRole.PosY = (int)myNpc.CurrentPos.Y;
            npcRole.MapCode = myNpc.MapCode;
            npcRole.Dir = (int)myNpc.CurrentDir;

            npcRole.RoleString = systemNPC.XMLNode.ToString();

            return DataHelper.ObjectToBytes<NPCRole>(npcRole);
        }

        /// <summary>
        /// 将自己周围的NPC发送给自己
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="objsList"></param>
        public static void SendMySelfNPCs(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            NPC npc = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                npc = objsList[i] as NPC;
                if (null == npc)
                {
                    continue;
                }

                if (!npc.ShowNpc)
                {
                    continue;
                }

                GameManager.ClientMgr.NotifyMySelfNewNPC(sl, pool, client, npc);
            }
        }

        /// <summary>
        /// 通知自己删除周围的NPC
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="objsList"></param>
        public static void DelMySelfNpcs(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            NPC npc = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                npc = objsList[i] as NPC;
                if (null == npc)
                {
                    continue;
                }

                GameManager.ClientMgr.NotifyMySelfDelNPC(sl, pool, client, npc);
            }
        }
    }
}
