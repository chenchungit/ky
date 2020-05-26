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
using GameServer.Interface;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 假人管理（雕像膜拜，离线摆摊，离线挂机）
    /// </summary>
    public static class FakeRoleManager
    {
        #region 基础数据

        /// <summary>
        /// 根据假人ID索引的假人数据字典
        /// </summary>
        private static Dictionary<int, FakeRoleItem> _ID2FakeRoleDict = new Dictionary<int, FakeRoleItem>();

        /// <summary>
        /// 根据假人角色ID_类型索引的假人数据字典
        /// </summary>
        private static Dictionary<string, FakeRoleItem> _RoleIDType2FakeRoleDict = new Dictionary<string, FakeRoleItem>();

        #endregion 基础数据

        #region 管理函数

        /// <summary>
        /// 添加一个新的假人数据
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="bhid"></param>
        /// <param name="bhName"></param>
        /// <param name="npcID"></param>
        /// <param name="fakeRoleName"></param>
        /// <param name="fakeRoleLevel"></param>
        /// <returns></returns>
        private static FakeRoleItem AddFakeRole(SafeClientData clientData, FakeRoleTypes fakeRoleType)
        {
            FakeRoleItem fakeRoleItem = new FakeRoleItem()
            {
                FakeRoleID = (int)GameManager.FakeRoleIDMgr.GetNewID(),
                FakeRoleType = (int)fakeRoleType,
                MyRoleDataMini = Global.ClientDataToRoleDataMini(clientData),
            };

            lock (_ID2FakeRoleDict)
            {
                _ID2FakeRoleDict[fakeRoleItem.FakeRoleID] = fakeRoleItem;
            }

            string roleID_Type = string.Format("{0}_{1}", fakeRoleItem.MyRoleDataMini.RoleID, (int)fakeRoleType);
            lock (_RoleIDType2FakeRoleDict)
            {
                _RoleIDType2FakeRoleDict[roleID_Type] = fakeRoleItem;
            }

            return fakeRoleItem;
        }

        /// <summary>
        /// 通过假人ID查找一个假人数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="FakeRoleID"></param>
        /// <returns></returns>
        public static FakeRoleItem FindFakeRoleByID(int FakeRoleID)
        {
            FakeRoleItem FakeRoleItem = null;
            lock (_ID2FakeRoleDict)
            {
                _ID2FakeRoleDict.TryGetValue(FakeRoleID, out FakeRoleItem);
            }

            return FakeRoleItem;
        }

        /// <summary>
        /// 通过假人角色ID_Type查找一个假人数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="FakeRoleID"></param>
        /// <returns></returns>
        public static FakeRoleItem FindFakeRoleByRoleIDType(int roleID, FakeRoleTypes fakeRoleType)
        {
            FakeRoleItem fakeRoleItem = null;
            string roleID_Type = string.Format("{0}_{1}", roleID, (int)fakeRoleType);
            lock (_RoleIDType2FakeRoleDict)
            {
                _RoleIDType2FakeRoleDict.TryGetValue(roleID_Type, out fakeRoleItem);
            }

            return fakeRoleItem;
        }

        /// <summary>
        /// 删除一个假人数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="FakeRoleID"></param>
        /// <returns></returns>
        private static void RemoveFakeRole(int FakeRoleID)
        {
            FakeRoleItem fakeRoleItem = null;
            lock (_ID2FakeRoleDict)
            {
                _ID2FakeRoleDict.TryGetValue(FakeRoleID, out fakeRoleItem);
                if (null != fakeRoleItem)
                {
                    _ID2FakeRoleDict.Remove(fakeRoleItem.FakeRoleID);
                }
            }

            if (null != fakeRoleItem)
            {
                string roleID_Type = string.Format("{0}_{1}", fakeRoleItem.MyRoleDataMini.RoleID, (int)fakeRoleItem.FakeRoleType);
                lock (_RoleIDType2FakeRoleDict)
                {
                    _RoleIDType2FakeRoleDict.Remove(roleID_Type);
                }
            }
        }

        /// <summary>
        /// 删除一个假人数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="FakeRoleID"></param>
        /// <returns></returns>
        private static void RemoveFakeRoleByRoleIDType(int roleID, FakeRoleTypes fakeRoleType)
        {
            FakeRoleItem fakeRoleItem = null;
            string roleID_Type = string.Format("{0}_{1}", roleID, (int)fakeRoleType);
            lock (_RoleIDType2FakeRoleDict)
            {
                _RoleIDType2FakeRoleDict.TryGetValue(roleID_Type, out fakeRoleItem);
                if (null != fakeRoleItem)
                {
                    _RoleIDType2FakeRoleDict.Remove(roleID_Type);
                }
            }

            if (null != fakeRoleItem)
            {
                lock (_ID2FakeRoleDict)
                {
                    _ID2FakeRoleDict.Remove(fakeRoleItem.FakeRoleID);
                }
            }
        }

        /// <summary>
        /// 删除指定类型的所有假人数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="FakeRoleID"></param>
        /// <returns></returns>
        private static List<FakeRoleItem> RemoveFakeRoleByType(FakeRoleTypes fakeRoleType)
        {
            List<FakeRoleItem> fakeRoleItemList = new List<FakeRoleItem>();
            lock (_ID2FakeRoleDict)
            {
                foreach (var item in _ID2FakeRoleDict.Values)
                {
                    if (item.FakeRoleType == (int)fakeRoleType)
                    {
                        fakeRoleItemList.Add(item);
                    }
                }

                foreach (var item in fakeRoleItemList)
                {
                    _ID2FakeRoleDict.Remove(item.FakeRoleID);
                }
            }

            lock (_RoleIDType2FakeRoleDict)
            {
                foreach (var item in fakeRoleItemList)
                {
                    string roleID_Type = string.Format("{0}_{1}", item.MyRoleDataMini.RoleID, item.FakeRoleType);
                    _RoleIDType2FakeRoleDict.Remove(roleID_Type);
                }
            }

            return fakeRoleItemList;
        }

        /// <summary>
        /// 根据假人类型获取假人列表
        /// </summary>
        /// <param name="fakeRoleType"></param>
        /// <returns></returns>
        private static List<FakeRoleItem> GetFakeRoleListByType(FakeRoleTypes fakeRoleType)
        {
            List<FakeRoleItem> fakeRoleItemList = new List<FakeRoleItem>();
            lock (_ID2FakeRoleDict)
            {
                foreach (var item in _ID2FakeRoleDict.Values)
                {
                    if (item.FakeRoleType == (int)fakeRoleType)
                    {
                        fakeRoleItemList.Add(item);
                    }
                }
            }

            return fakeRoleItemList;
        }

        #endregion 管理函数

        #region 处理新假人的生成和删除

        /// <summary>
        /// 处理添加假人
        /// </summary>
        public static int ProcessNewFakeRole(SafeClientData clientData, int mapCode, FakeRoleTypes fakeRoleType, int direction = -1, int toPosX = -1, int toPosY = -1, int ToExtensionID = -1)
        {
            if (mapCode <= 0 || !GameManager.MapGridMgr.DictGrids.ContainsKey(mapCode))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("为RoleID离线挂机时失败, MapCode={0}, RoleID={1}", clientData.MapCode, clientData.RoleID));
                return -1;
            }

            //删除一个假人数据
            RemoveFakeRoleByRoleIDType(clientData.RoleID, fakeRoleType);

            //添加一个新的假人数据
            FakeRoleItem fakeRoleItem = AddFakeRole(clientData, fakeRoleType);
            if (null == fakeRoleItem)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("为RoleID生成假人对象时失败, MapCode={0}, RoleID={1}", clientData.MapCode, clientData.RoleID));
                return -1;
            }

            fakeRoleItem.MyRoleDataMini.MapCode = mapCode;

            if (toPosX >= 0 && toPosY >= 0)
            {
                fakeRoleItem.MyRoleDataMini.PosX = toPosX;
                fakeRoleItem.MyRoleDataMini.PosY = toPosY;
            }

            if (direction >= 0)
            {
                fakeRoleItem.MyRoleDataMini.RoleDirection = direction;
            }

            if (ToExtensionID >= 0)
            {
                fakeRoleItem.ToExtensionID = ToExtensionID;
            }

            if (FakeRoleTypes.LiXianGuaJi == fakeRoleType)
            {
                if (clientData.OfflineMarketState <= 0)
                {
                    fakeRoleItem.MyRoleDataMini.StallName = "";
                }
            }

            if (FakeRoleTypes.DiaoXiang2 == fakeRoleType)
            {
                if (null == fakeRoleItem.MyRoleDataMini.RoleCommonUseIntPamams || fakeRoleItem.MyRoleDataMini.RoleCommonUseIntPamams.Count <= 0)
                {
                    int fashionID = 0;
                    foreach (var item in FashionManager.getInstance().RuntimeData.FashingDict.Values)
                    {
                        if (item.Type == (int)FashionTypes.LuoLanYuYi)
                        {
                            fashionID = item.ID;
                            break;
                        }
                    }
                    if (null == fakeRoleItem.MyRoleDataMini.RoleCommonUseIntPamams)
                    {
                        fakeRoleItem.MyRoleDataMini.RoleCommonUseIntPamams = new List<int>();
                    }

                    for (int i = fakeRoleItem.MyRoleDataMini.RoleCommonUseIntPamams.Count; i < (int)RoleCommonUseIntParamsIndexs.MaxCount; ++i)
                    {
                        fakeRoleItem.MyRoleDataMini.RoleCommonUseIntPamams.Add(0);
                    }
                    fakeRoleItem.MyRoleDataMini.RoleCommonUseIntPamams[(int)RoleCommonUseIntParamsIndexs.FashionWingsID] = fashionID;
                }
            }

            fakeRoleItem.MyRoleDataMini.LifeV = Math.Max(1, clientData.LifeV);
            fakeRoleItem.MyRoleDataMini.MagicV = Math.Max(1, clientData.MagicV);

            GameManager.MapGridMgr.DictGrids[fakeRoleItem.MyRoleDataMini.MapCode].MoveObject(-1, -1, (int)fakeRoleItem.MyRoleDataMini.PosX, (int)fakeRoleItem.MyRoleDataMini.PosY, fakeRoleItem);

            //List<Object> objList = Global.GetAll9Clients(FakeRoleItem);
            //GameManager.ClientMgr.NotifyOthersNewFakeRole(sl, pool, objList, FakeRoleItem);

            return fakeRoleItem.FakeRoleID;
        }

        /// <summary>
        /// 删除指定类型的所有假人
        /// </summary>
        public static void ProcessDelFakeRoleByType(FakeRoleTypes fakeRoleType, bool bBroadcastDelMsg = false)
        {
            List<FakeRoleItem> fakeRoleItemList = GetFakeRoleListByType(fakeRoleType);
            foreach (var item in fakeRoleItemList)
            {
                ProcessDelFakeRole(item.FakeRoleID, bBroadcastDelMsg);
            }
        }

        /// <summary>
        /// 处理删除假人
        /// </summary>
        public static void ProcessDelFakeRole(int FakeRoleID, bool bBroadcastDelMsg = false)
        {
            FakeRoleItem FakeRoleItem = FindFakeRoleByID(FakeRoleID);
            if (null == FakeRoleItem)
            {
                return;
            }

            RemoveFakeRole(FakeRoleID);

            GameManager.MapGridMgr.DictGrids[FakeRoleItem.MyRoleDataMini.MapCode].RemoveObject(FakeRoleItem);
            if (bBroadcastDelMsg)
            {
                GameManager.ClientMgr.NotifyAllDelFakeRole(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, FakeRoleItem);
            }
            //List<Object> objList = Global.GetAll9Clients(FakeRoleItem);
            //GameManager.ClientMgr.NotifyOthersDelFakeRole(sl, pool, objList, FakeRoleID);
        }

        /// <summary>
        /// 处理假人回城
        /// </summary>
        public static void ProcessFakeRoleGoBack(int FakeRoleID)
        {
            FakeRoleItem fakeRoleItem = FindFakeRoleByID(FakeRoleID);
            if (null == fakeRoleItem)
            {
                return;
            }

            GameManager.ClientMgr.NotifyAllDelFakeRole(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, fakeRoleItem);

            int toMapCode = fakeRoleItem.CurrentMapCode;
            
            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
            {
                return;
            }

            int defaultBirthPosX = gameMap.DefaultBirthPosX;
            int defaultBirthPosY = gameMap.DefaultBirthPosY;
            int defaultBirthRadius = gameMap.BirthRadius;

            //从配置根据地图取默认位置
            Point newPos = Global.GetMapPoint(ObjectTypes.OT_FAKEROLE, toMapCode, defaultBirthPosX, defaultBirthPosY, defaultBirthRadius);

            //从配置根据地图取默认位置
            int toMapX = (int)newPos.X;
            int toMapY = (int)newPos.Y;

            int oldX = fakeRoleItem.MyRoleDataMini.PosX;
            int oldY = fakeRoleItem.MyRoleDataMini.PosY;

            fakeRoleItem.MyRoleDataMini.PosX = toMapX;
            fakeRoleItem.MyRoleDataMini.PosY = toMapY;

            fakeRoleItem.MyRoleDataMini.LifeV = fakeRoleItem.MyRoleDataMini.MaxLifeV;

            GameManager.MapGridMgr.DictGrids[toMapCode].MoveObject(oldX, oldY, toMapX, toMapY, fakeRoleItem);
            //List<Object> objList = Global.GetAll9Clients(FakeRoleItem);
            //GameManager.ClientMgr.NotifyOthersDelFakeRole(sl, pool, objList, FakeRoleID);
        }

        /// <summary>
        /// 处理删除假人
        /// </summary>
        public static void ProcessDelFakeRole(int roleID, FakeRoleTypes fakeRoleType)
        {
            FakeRoleItem FakeRoleItem = FindFakeRoleByRoleIDType(roleID, fakeRoleType);
            if (null == FakeRoleItem)
            {
                return;
            }

            RemoveFakeRole(FakeRoleItem.FakeRoleID);

            GameManager.MapGridMgr.DictGrids[FakeRoleItem.MyRoleDataMini.MapCode].RemoveObject(FakeRoleItem);
            //List<Object> objList = Global.GetAll9Clients(FakeRoleItem);
            //GameManager.ClientMgr.NotifyOthersDelFakeRole(sl, pool, objList, FakeRoleID);
        }

        #endregion 处理新假人的生成和删除

        #region 处理假人的显示和隐藏

        /// <summary>
        /// 通知所有人显示假人
        /// </summary>
        public static void NotifyOthersShowFakeRole(SocketListener sl, TCPOutPacketPool pool, FakeRoleItem FakeRoleItem)
        {
            if (null == FakeRoleItem) return;

            GameManager.MapGridMgr.DictGrids[FakeRoleItem.MyRoleDataMini.MapCode].MoveObject(-1, -1, (int)FakeRoleItem.MyRoleDataMini.PosX, (int)FakeRoleItem.MyRoleDataMini.PosY, FakeRoleItem);
            //List<Object> objList = Global.GetAll9Clients(FakeRoleItem);
            //GameManager.ClientMgr.NotifyOthersNewFakeRole(sl, pool, objList, FakeRoleItem);
        }

        /// <summary>
        /// 通知所有人隐藏假人
        /// </summary>
        public static void NotifyOthersHideFakeRole(SocketListener sl, TCPOutPacketPool pool, FakeRoleItem FakeRoleItem)
        {
            if (null == FakeRoleItem) return;

            GameManager.MapGridMgr.DictGrids[FakeRoleItem.MyRoleDataMini.MapCode].RemoveObject(FakeRoleItem);
            //List<Object> objList = Global.GetAll9Clients(FakeRoleItem);
            //GameManager.ClientMgr.NotifyOthersDelFakeRole(sl, pool, objList, FakeRoleItem.FakeRoleID);
        }

        #endregion 处理假人的显示和隐藏

        #region 处理已经假人加血/死亡/超时

        /// <summary>
        /// 处理假人的死亡
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="FakeRoleItem"></param>
        /// <returns></returns>
        private static bool ProcessFakeRoleDead(SocketListener sl, TCPOutPacketPool pool, long nowTicks, FakeRoleItem fakeRoleItem)
        {
            if (fakeRoleItem.CurrentLifeV > 0)
            {
                return false;
            }

            long subTicks = nowTicks - fakeRoleItem.FakeRoleDeadTicks;

            //如果还没到时间，则跳过
            if (subTicks < (2 * 1000))
            {
                return false;
            }

            ProcessFakeRoleGoBack(fakeRoleItem.FakeRoleID);

            return true;
        }

        /// <summary>
        /// 处理已经假人的超时
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessAllFakeRoleItems(SocketListener sl, TCPOutPacketPool pool)
        {
            List<FakeRoleItem> FakeRoleItemList = new List<FakeRoleItem>();
            lock (_ID2FakeRoleDict)
            {
                foreach (var val in _ID2FakeRoleDict.Values)
                {
                    FakeRoleItemList.Add(val);
                }
            }

            long nowTicks = TimeUtil.NOW();

            FakeRoleItem FakeRoleItem = null;
            for (int i = 0; i < FakeRoleItemList.Count; i++)
            {
                FakeRoleItem = FakeRoleItemList[i];

                //处理假人的死亡
                if (ProcessFakeRoleDead(sl, pool, nowTicks, FakeRoleItem))
                {
                    continue;
                }
            }
        }

        #endregion 处理已经假人加血/死亡/超时

        #region 处理角色移动时的假人发送

        /// <summary>
        /// 发送假人到给自己
        /// </summary>
        /// <param name="client"></param>
        public static void SendMySelfFakeRoleItems(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList, int totalRoleAndMonsterNum)
        {
            if (null == objsList) return;
            FakeRoleItem fakeRoleItem = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                fakeRoleItem = objsList[i] as FakeRoleItem;
                if (null == fakeRoleItem)
                {
                    continue;
                }

                if (!GameManager.TestGameShowFakeRoleForUser && fakeRoleItem.FakeRoleType != (int)FakeRoleTypes.DiaoXiang
                    && fakeRoleItem.FakeRoleType != (int)FakeRoleTypes.DiaoXiang2 && fakeRoleItem.FakeRoleType != (int)FakeRoleTypes.DiaoXiang3
                    && fakeRoleItem.FakeRoleType != (int)FakeRoleTypes.CoupleWishMan && fakeRoleItem.FakeRoleType != (int)FakeRoleTypes.CoupleWishWife)
                {
                    continue;
                }

                if (fakeRoleItem.CurrentLifeV <= 0)
                {
                    continue;
                }

                if (totalRoleAndMonsterNum >= 30)
                {
                    if (fakeRoleItem.FakeRoleType == (int)FakeRoleTypes.LiXianGuaJi)
                    {
                        continue;
                    }
                }

                GameManager.ClientMgr.NotifyMySelfNewFakeRole(sl, pool, client, fakeRoleItem);
            }
        }

        /// <summary>
        /// 删除自己哪儿的假人
        /// </summary>
        /// <param name="client"></param>
        public static void DelMySelfFakeRoleItems(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            FakeRoleItem fakeRoleItem = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                fakeRoleItem = objsList[i] as FakeRoleItem;
                if (null == fakeRoleItem)
                {
                    continue;
                }

                if (!GameManager.TestGameShowFakeRoleForUser && fakeRoleItem.FakeRoleType != (int)FakeRoleTypes.DiaoXiang
                    && fakeRoleItem.FakeRoleType != (int)FakeRoleTypes.DiaoXiang2 && fakeRoleItem.FakeRoleType != (int)FakeRoleTypes.DiaoXiang3)
                {
                    continue;
                }

                GameManager.ClientMgr.NotifyMySelfDelFakeRole(sl, pool, client, fakeRoleItem.FakeRoleID);
            }
        }

        #endregion 处理角色移动时的假人发送

        #region 查找指定范围内的假人

        /// <summary>
        /// 查找指定圆周范围内的假人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupEnemiesInCircle(GameClient client, int mapCode, int toX, int toY, int radius, List<Object> enemiesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objList) return;

            Point center = new Point(toX, toY);
            for (int i = 0; i < objList.Count; i++)
            {
                if (!(objList[i] is FakeRoleItem))
                {
                    continue;
                }

                //非敌对对象
                if (null != client && !Global.IsOpposition(client, (objList[i] as FakeRoleItem)))
                {
                    continue;
                }

                //不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objList[i] as FakeRoleItem).CopyMapID)
                {
                    continue;
                }

                Point pt = new Point((objList[i] as FakeRoleItem).MyRoleDataMini.PosX, (objList[i] as FakeRoleItem).MyRoleDataMini.PosY);
                if (Global.InCircle(pt, center, (double)radius))
                {
                    enemiesList.Add((objList[i] as FakeRoleItem));
                }
            }
        }

        /// <summary>
        /// 查找指定半圆周范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupEnemiesInCircleByAngle(GameClient client, int direction, int mapCode, int toX, int toY, int radius, List<int> enemiesList, double angle, bool near180)
        {
            List<Object> objList = new List<Object>();
            LookupEnemiesInCircleByAngle(client, direction, mapCode, toX, toY, radius, objList, angle, near180);
            for (int i = 0; i < objList.Count; i++)
            {
                enemiesList.Add((objList[i] as FakeRoleItem).FakeRoleID);
            }
        }

        /// <summary>
        /// 查找指定半圆周范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupEnemiesInCircleByAngle(GameClient client, int direction, int mapCode, int toX, int toY, int radius, List<Object> enemiesList, double angle, bool near180)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objList) return;

            double loAngle = 0.0, hiAngle = 0.0;
            Global.GetAngleRangeByDirection(direction, angle, out loAngle, out hiAngle);
            Point center = new Point(toX, toY);
            for (int i = 0; i < objList.Count; i++)
            {
                if (!(objList[i] is FakeRoleItem))
                {
                    continue;
                }

                //非敌对对象
                if (null != client && !Global.IsOpposition(client, (objList[i] as FakeRoleItem)))
                {
                    continue;
                }

                //不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objList[i] as FakeRoleItem).CopyMapID)
                {
                    continue;
                }

                Point pt = new Point((objList[i] as FakeRoleItem).MyRoleDataMini.PosX, (objList[i] as FakeRoleItem).MyRoleDataMini.PosY);
                if (Global.InCircleByAngle(pt, center, (double)radius, loAngle, hiAngle))
                {
                    enemiesList.Add((objList[i]));
                }
                else
                {
                    ;
                }
            }
        }

        // 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
        /// <summary>
        /// 查找指定矩形范围内的怪
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupRolesInSquare(GameClient client, int mapCode, int radius, int nWidth, List<Object> rolesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objsList = mapGrid.FindObjects((int)client.ClientData.PosX, (int)client.ClientData.PosY, radius);
            if (null == objsList) return;

            // 源点
            Point source = new Point(client.ClientData.PosX, client.ClientData.PosY);

            Point toPos = Global.GetAPointInCircle(source, radius, client.ClientData.RoleYAngle);

            int toX = (int)toPos.X;
            int toY = (int)toPos.Y;

            // 矩形的中心点
            Point center = new Point();
            center.X = (client.ClientData.PosX + toX) / 2;
            center.Y = (client.ClientData.PosY + toY) / 2;

            // 矩形方向向量
            int fDirectionX = toX - client.ClientData.PosX;
            int fDirectionY = toY - client.ClientData.PosY;
            //Point center = new Point(toX, toY);

            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is FakeRoleItem))
                    continue;

                if ((objsList[i] as FakeRoleItem).CurrentLifeV <= 0)
                    continue;

                // 不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objsList[i] as FakeRoleItem).CopyMapID)
                    continue;

                Point target = new Point((objsList[i] as FakeRoleItem).CurrentPos.X, (objsList[i] as FakeRoleItem).CurrentPos.Y);

                if (Global.InSquare(center, target, radius, nWidth, fDirectionX, fDirectionY))
                    rolesList.Add(objsList[i]);
                else if (Global.InCircle(target, source, (double)100))  // 补充扫描
                    rolesList.Add((objsList[i]));
            }
        }

        #endregion 查找指定范围内的假人

        #region 查找指定格子内的假人

        /// <summary>
        /// 查找指定格子内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupEnemiesAtGridXY(IObject attacker, int gridX, int gridY, List<Object> enemiesList)
        {
            int mapCode = attacker.CurrentMapCode;
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];

            List<Object> objList = mapGrid.FindObjects((int)gridX, (int)gridY);
            if (null == objList) return;

            for (int i = 0; i < objList.Count; i++)
            {
                if (!(objList[i] is FakeRoleItem))
                {
                    continue;
                }

                //不在同一个副本
                if (null != attacker && attacker.CurrentCopyMapID != (objList[i] as FakeRoleItem).CopyMapID)
                {
                    continue;
                }

                enemiesList.Add(objList[i]);
            }
        }

        /// <summary>
        /// 查找指定格子内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupAttackEnemies(IObject attacker, int direction, List<Object> enemiesList)
        {
            int mapCode = attacker.CurrentMapCode;
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];

            Point grid = attacker.CurrentGrid;
            int gridX = (int)grid.X;
            int gridY = (int)grid.Y;

            Point p = Global.GetGridPointByDirection(direction, gridX, gridY);

            //查找指定格子内的敌人
            LookupEnemiesAtGridXY(attacker, (int)p.X, (int)p.Y, enemiesList);
        }

        /// <summary>
        /// 查找指定格子内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupAttackEnemyIDs(IObject attacker, int direction, List<int> enemiesList)
        {
            List<Object> objList = new List<Object>();
            LookupAttackEnemies(attacker, direction, objList);
            for (int i = 0; i < objList.Count; i++)
            {
                enemiesList.Add((objList[i] as FakeRoleItem).FakeRoleID);
            }
        }

        /// <summary>
        /// 查找指定给子范围内的敌人
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupRangeAttackEnemies(IObject obj, int toX, int toY, int direction, string rangeMode, List<Object> enemiesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[obj.CurrentMapCode];

            int gridX = toX / mapGrid.MapGridWidth;
            int gridY = toY / mapGrid.MapGridHeight;

            //根据传入的格子坐标和方向返回指定方向的格子列表
            List<Point> gridList = Global.GetGridPointByDirection(direction, gridX, gridY, rangeMode);
            if (gridList.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < gridList.Count; i++)
            {
                //查找指定格子内的敌人
                LookupEnemiesAtGridXY(obj, (int)gridList[i].X, (int)gridList[i].Y, enemiesList);
            }
        }

        #endregion 查找指定格子内的假人

        #region 战斗相关

        /// <summary>
        /// 是否能被攻击
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public static bool CanAttack(FakeRoleItem enemy)
        {
            if (GameManager.TestGameShowFakeRoleForUser)
            {
                return false;
            }
            
            if (null == enemy) return false;

            if (enemy.GetFakeRoleData().FakeRoleType != (int)FakeRoleTypes.LiXianGuaJi) //不是离线挂机的假人不能攻击
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 通知其他人被攻击(单攻)，并且被伤害(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public static int NotifyInjured(SocketListener sl, TCPOutPacketPool pool, GameClient client, FakeRoleItem enemy, int burst, int injure, double injurePercent, int attackType, bool forceBurst, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, double baseRate = 1.0, int addVlue = 0, int nHitFlyDistance = 0)
        {
            int ret = 0;
            object obj = enemy;
            {
                //怪物必须或者才操作
                if ((obj as FakeRoleItem).CurrentLifeV > 0)
                {
                    injure = 1000;

                    // 技能改造[3/13/2014 LiaoWei]
                    //injure = (int)(injure * baseRate + addVlue);

                    // 技能中可配置伤害百分比
                    injure = (int)(injure * injurePercent);
                    ret = injure;

                    (obj as FakeRoleItem).CurrentLifeV -= (int)injure; //是否需要锁定
                    (obj as FakeRoleItem).CurrentLifeV = Global.GMax((obj as FakeRoleItem).CurrentLifeV, 0);
                    int enemyLife = (int)(obj as FakeRoleItem).CurrentLifeV;
                    (obj as FakeRoleItem).AttackedRoleID = client.ClientData.RoleID;

                    //判断是否将给敌人的伤害转化成自己的血量增长
                    GameManager.ClientMgr.SpriteInjure2Blood(sl, pool, client, injure);

                    //将攻击者加入历史列表
                    (obj as FakeRoleItem).AddAttacker(client.ClientData.RoleID, Global.GMax(0, injure));

                    GameManager.SystemServerEvents.AddEvent(string.Format("假人减血, Injure={0}, Life={1}", injure, enemyLife), EventLevels.Debug);

                    //判断怪物是否死亡
                    if ((int)(obj as FakeRoleItem).CurrentLifeV <= 0)
                    {
                        GameManager.SystemServerEvents.AddEvent(string.Format("假人死亡, roleID={0}", (obj as FakeRoleItem).FakeRoleID), EventLevels.Debug);

                        /// 处理怪物死亡
                        ProcessFakeRoleDead(sl, pool, client, (obj as FakeRoleItem));
                    }

                    Point hitToGrid = new Point(-1, -1);

                    // 处理击飞 [3/15/2014 LiaoWei]
                    if (nHitFlyDistance > 0)
                    {
                        MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode];

                        int nGridNum = nHitFlyDistance * 100 / mapGrid.MapGridWidth;

                        if (nGridNum > 0)
                            hitToGrid = ChuanQiUtils.HitFly(client, enemy, nGridNum);
                    }

                    if ((obj as FakeRoleItem).AttackedRoleID >= 0 && (obj as FakeRoleItem).AttackedRoleID != client.ClientData.RoleID)
                    {
                        GameClient findClient = GameManager.ClientMgr.FindClient((obj as FakeRoleItem).AttackedRoleID);
                        if (null != findClient)
                        {
                            //通知其他在线客户端
                            GameManager.ClientMgr.NotifySpriteInjured(sl, pool, findClient, findClient.ClientData.MapCode, findClient.ClientData.RoleID, (obj as FakeRoleItem).FakeRoleID, 0, 0, enemyLife, findClient.ClientData.Level, hitToGrid);

                            //向自己发送敌人受伤的信息
                            ClientManager.NotifySelfEnemyInjured(sl, pool, findClient, findClient.ClientData.RoleID, enemy.FakeRoleID, 0, 0, enemyLife, 0);
                        }
                    }

                    //通知其他在线客户端
                    GameManager.ClientMgr.NotifySpriteInjured(sl, pool, client, client.ClientData.MapCode, client.ClientData.RoleID, (obj as FakeRoleItem).FakeRoleID, burst, injure, enemyLife, client.ClientData.Level, hitToGrid);

                    //向自己发送敌人受伤的信息
                    ClientManager.NotifySelfEnemyInjured(sl, pool, client, client.ClientData.RoleID, enemy.FakeRoleID, burst, injure, enemyLife, 0);

                    //通知紫名信息(限制当前地图)
                    if (!client.ClientData.DisableChangeRolePurpleName)
                    {
                        GameManager.ClientMgr.ForceChangeRolePurpleName2(sl, pool, client);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 通知其他人被攻击(单攻)，并且被伤害(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public static void NotifyInjured(SocketListener sl, TCPOutPacketPool pool, GameClient client, int roleID, int enemy, int enemyX, int enemyY, int burst, int injure, double attackPercent, int addAttack, double baseRate = 1.0, int addVlue = 0, int nHitFlyDistance = 0)
        {
            object obj = FindFakeRoleByID(enemy);
            if (null != obj)
            {
                //怪物必须或者才操作
                if ((obj as FakeRoleItem).CurrentLifeV > 0)
                {
                    //处理BOSS克星
                    injure = 10000;

                    (obj as FakeRoleItem).CurrentLifeV -= (int)injure; //是否需要锁定
                    (obj as FakeRoleItem).CurrentLifeV = Global.GMax((obj as FakeRoleItem).CurrentLifeV, 0);
                    int enemyLife = (int)(obj as FakeRoleItem).CurrentLifeV;
                    (obj as FakeRoleItem).AttackedRoleID = client.ClientData.RoleID;

                    //判断是否将给敌人的伤害转化成自己的血量增长
                    GameManager.ClientMgr.SpriteInjure2Blood(sl, pool, client, injure);

                    GameManager.SystemServerEvents.AddEvent(string.Format("假人减血, Injure={0}, Life={1}", injure, enemyLife), EventLevels.Debug);

                    //判断怪物是否死亡
                    if ((int)(obj as FakeRoleItem).CurrentLifeV <= 0)
                    {
                        GameManager.SystemServerEvents.AddEvent(string.Format("假人死亡, roleID={0}", (obj as FakeRoleItem).FakeRoleID), EventLevels.Debug);

                        /// 处理假人死亡
                        ProcessFakeRoleDead(sl, pool, client, (obj as FakeRoleItem));
                    }

                    int ownerRoleID = (obj as FakeRoleItem).GetAttackerFromList();
                    if (ownerRoleID >= 0 && ownerRoleID != client.ClientData.RoleID)
                    {
                        GameClient findClient = GameManager.ClientMgr.FindClient(ownerRoleID);
                        if (null != findClient)
                        {
                            //通知其他在线客户端
                            GameManager.ClientMgr.NotifySpriteInjured(sl, pool, findClient, findClient.ClientData.MapCode, findClient.ClientData.RoleID, (obj as FakeRoleItem).FakeRoleID, 0, 0, enemyLife, findClient.ClientData.Level, new Point(-1, -1));

                            //向自己发送敌人受伤的信息
                            ClientManager.NotifySelfEnemyInjured(sl, pool, findClient, findClient.ClientData.RoleID, (obj as FakeRoleItem).FakeRoleID, 0, 0, enemyLife, 0);
                        }
                    }

                    //通知其他在线客户端
                    GameManager.ClientMgr.NotifySpriteInjured(sl, pool, client, client.ClientData.MapCode, client.ClientData.RoleID, (obj as FakeRoleItem).FakeRoleID, burst, injure, enemyLife, client.ClientData.Level, new Point(-1, -1));

                    //向自己发送敌人受伤的信息
                    ClientManager.NotifySelfEnemyInjured(sl, pool, client, client.ClientData.RoleID, (obj as FakeRoleItem).FakeRoleID, burst, injure, enemyLife, 0);

                    //通知紫名信息(限制当前地图)
                    if (!client.ClientData.DisableChangeRolePurpleName)
                    {
                        GameManager.ClientMgr.ForceChangeRolePurpleName2(sl, pool, client);
                    }
                }
            }
        }

        /// <summary>
        /// 处理假人死亡
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        private static void ProcessFakeRoleDead(SocketListener sl, TCPOutPacketPool pool, GameClient client, FakeRoleItem fakeRoleItem)
        {
            if (fakeRoleItem.HandledDead)
            {
                return;
            }

            fakeRoleItem.HandledDead = true;
            fakeRoleItem.FakeRoleDeadTicks = TimeUtil.NOW();

            int ownerRoleID = fakeRoleItem.GetAttackerFromList(); //根据血量计算
            if (ownerRoleID >= 0 && ownerRoleID != client.ClientData.RoleID)
            {
                GameClient findClient = GameManager.ClientMgr.FindClient(ownerRoleID);
                if (null != findClient)
                {
                    client = findClient;
                }
            }
        }

        #endregion 战斗相关
    }
}
