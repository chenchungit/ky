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
    /// 镖车管理类
    /// </summary>
    public class BiaoCheManager
    {
        #region 静态变量

        /// <summary>
        /// 不能被攻击的镖车ID
        /// </summary>
        public static int NotAttackYaBiaoID = -1;

        #endregion 静态变量

        #region 基础数据

        /// <summary>
        /// 根据角色ID索引的镖车数据字典
        /// </summary>
        private static Dictionary<int, BiaoCheItem> _RoleID2BiaoCheDict = new Dictionary<int, BiaoCheItem>();

        /// <summary>
        /// 根据镖车ID索引的镖车数据字典
        /// </summary>
        private static Dictionary<int, BiaoCheItem> _ID2BiaoCheDict = new Dictionary<int, BiaoCheItem>();

        #endregion 基础数据

        #region 管理函数

        /// <summary>
        /// 添加一个新的镖车数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="yaBiaoID"></param>
        /// <returns></returns>
        private static BiaoCheItem AddBiaoChe(GameClient client, int yaBiaoID)
        {
            SystemXmlItem systemYaBiaoItem = null;
            if (!GameManager.systemYaBiaoMgr.SystemXmlItemDict.TryGetValue(yaBiaoID, out systemYaBiaoItem))
            {
                return null;
            }

            BiaoCheItem biaoCheItem = new BiaoCheItem()
            {
                OwnerRoleID = client.ClientData.RoleID,
                OwnerRoleName = Global.FormatRoleName(client, client.ClientData.RoleName),
                BiaoCheID = (int)GameManager.BiaoCheIDMgr.GetNewID(),
                BiaoCheName = Global.GetYaBiaoName(yaBiaoID),
                YaBiaoID = yaBiaoID,
                MapCode = client.ClientData.MapCode,
                PosX = client.ClientData.PosX,
                PosY = client.ClientData.PosY,
                Direction = client.ClientData.RoleDirection,
                LifeV = systemYaBiaoItem.GetIntValue("Lifev"),
                StartTime = TimeUtil.NOW(),
                CurrentLifeV = systemYaBiaoItem.GetIntValue("Lifev"),
                CutLifeV = systemYaBiaoItem.GetIntValue("CutLifeV"),
                BodyCode = systemYaBiaoItem.GetIntValue("BodyCode"),
                PicCode = systemYaBiaoItem.GetIntValue("PicCode"),
                DestNPC = systemYaBiaoItem.GetIntValue("DestNPC"),
                MinLevel = systemYaBiaoItem.GetIntValue("MinLevel"),
                MaxLevel = systemYaBiaoItem.GetIntValue("MaxLevel"),
            };

            lock (_RoleID2BiaoCheDict)
            {
                _RoleID2BiaoCheDict[biaoCheItem.OwnerRoleID] = biaoCheItem;
            }

            lock (_ID2BiaoCheDict)
            {
                _ID2BiaoCheDict[biaoCheItem.BiaoCheID] = biaoCheItem;
            }

            return biaoCheItem;
        }

        /// <summary>
        /// 通过角色ID查找一个镖车数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="yaBiaoID"></param>
        /// <returns></returns>
        public static BiaoCheItem FindBiaoCheByRoleID(int roleID)
        {
            BiaoCheItem biaoCheItem = null;
            lock (_RoleID2BiaoCheDict)
            {
                _RoleID2BiaoCheDict.TryGetValue(roleID, out biaoCheItem);
            }

            return biaoCheItem;
        }

        /// <summary>
        /// 通过镖车ID查找一个镖车数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="yaBiaoID"></param>
        /// <returns></returns>
        public static BiaoCheItem FindBiaoCheByID(int biaoCheID)
        {
            BiaoCheItem biaoCheItem = null;
            lock (_ID2BiaoCheDict)
            {
                _ID2BiaoCheDict.TryGetValue(biaoCheID, out biaoCheItem);
            }

            return biaoCheItem;
        }

        /// <summary>
        /// 删除一个镖车数据
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="yaBiaoID"></param>
        /// <returns></returns>
        private static void RemoveBiaoChe(int biaoCheID)
        {
            BiaoCheItem biaoCheItem = null;
            lock (_ID2BiaoCheDict)
            {
                _ID2BiaoCheDict.TryGetValue(biaoCheID, out biaoCheItem);
                if (null != biaoCheItem)
                {
                    _ID2BiaoCheDict.Remove(biaoCheItem.BiaoCheID);
                }
            }

            lock (_RoleID2BiaoCheDict)
            {
                if (null != biaoCheItem)
                {
                    _RoleID2BiaoCheDict.Remove(biaoCheItem.OwnerRoleID);
                }
            }
        }

        #endregion 管理函数

        #region 处理新镖车的生成和删除

        /// <summary>
        /// 处理添加镖车
        /// </summary>
        public static void ProcessNewBiaoChe(SocketListener sl, TCPOutPacketPool pool, GameClient client, int yaBiaoID)
        {
            //添加一个新的镖车数据
            BiaoCheItem biaoCheItem = AddBiaoChe(client, yaBiaoID);
            if (null == biaoCheItem)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("为RoleID生成镖车对象时失败, Client={0}, RoleID={1}, YaBiaoID={2}", Global.GetSocketRemoteEndPoint(client.ClientSocket), client.ClientData.RoleID, yaBiaoID));
                return;
            }

            GameManager.MapGridMgr.DictGrids[biaoCheItem.MapCode].MoveObject(-1, -1, (int)biaoCheItem.PosX, (int)biaoCheItem.PosY, biaoCheItem);

            //List<Object> objList = Global.GetAll9Clients(biaoCheItem);
            //GameManager.ClientMgr.NotifyOthersNewBiaoChe(sl, pool, objList, biaoCheItem);
        }

        /// <summary>
        /// 处理删除镖车
        /// </summary>
        public static void ProcessDelBiaoChe(SocketListener sl, TCPOutPacketPool pool, int biaoCheID)
        {
            BiaoCheItem biaoCheItem = FindBiaoCheByID(biaoCheID);
            if (null == biaoCheItem)
            {
                return;
            }

            RemoveBiaoChe(biaoCheID);

            GameManager.MapGridMgr.DictGrids[biaoCheItem.MapCode].RemoveObject(biaoCheItem);
            //List<Object> objList = Global.GetAll9Clients(biaoCheItem);
            //GameManager.ClientMgr.NotifyOthersDelBiaoChe(sl, pool, objList, biaoCheID);;
        }

        #endregion 处理新镖车的生成和删除

        #region 处理镖车的显示和隐藏

        /// <summary>
        /// 通知所有人显示镖车
        /// </summary>
        public static void NotifyOthersShowBiaoChe(SocketListener sl, TCPOutPacketPool pool, BiaoCheItem biaoCheItem)
        {
            if (null == biaoCheItem) return;

            GameManager.MapGridMgr.DictGrids[biaoCheItem.MapCode].MoveObject(-1, -1, (int)biaoCheItem.PosX, (int)biaoCheItem.PosY, biaoCheItem);
            //List<Object> objList = Global.GetAll9Clients(biaoCheItem);
            //GameManager.ClientMgr.NotifyOthersNewBiaoChe(sl, pool, objList, biaoCheItem);
        }

        /// <summary>
        /// 通知所有人隐藏镖车
        /// </summary>
        public static void NotifyOthersHideBiaoChe(SocketListener sl, TCPOutPacketPool pool, BiaoCheItem biaoCheItem)
        {
            if (null == biaoCheItem) return;

            GameManager.MapGridMgr.DictGrids[biaoCheItem.MapCode].RemoveObject(biaoCheItem);
            //List<Object> objList = Global.GetAll9Clients(biaoCheItem);
            //GameManager.ClientMgr.NotifyOthersDelBiaoChe(sl, pool, objList, biaoCheItem.BiaoCheID); ;
        }

        #endregion 处理镖车的显示和隐藏

        #region 处理已经镖车加血/死亡/超时

        /// <summary>
        /// 处理镖车的超时
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="biaoCheItem"></param>
        /// <returns></returns>
        private static bool ProcessBiaoCheOverTime(SocketListener sl, TCPOutPacketPool pool, long nowTicks, BiaoCheItem biaoCheItem)
        {
            //判断是否超过了最大的抢时间
            if (nowTicks - biaoCheItem.StartTime < Global.MaxYaBiaoTicks) //判断是否删除
            {
                return false;
            }

            ProcessDelBiaoChe(sl, pool, biaoCheItem.BiaoCheID);
            return true;
        }

        /// <summary>
        /// 处理镖车的死亡
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="biaoCheItem"></param>
        /// <returns></returns>
        private static bool ProcessBiaoCheDead(SocketListener sl, TCPOutPacketPool pool, long nowTicks, BiaoCheItem biaoCheItem)
        {
            if (biaoCheItem.CurrentLifeV > 0)
            {
                return false;
            }

            long subTicks = nowTicks - biaoCheItem.BiaoCheDeadTicks;

            //如果还没到时间，则跳过
            if (subTicks < (2 * 1000))
            {
                return false;
            }

            ProcessDelBiaoChe(sl, pool, biaoCheItem.BiaoCheID);
            return true;
        }

        /// <summary>
        /// 处理镖车的加血
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="biaoCheItem"></param>
        /// <returns></returns>
        private static void ProcessBiaoCheAddLife(SocketListener sl, TCPOutPacketPool pool, long nowTicks, BiaoCheItem biaoCheItem)
        {
            long subTicks = nowTicks - biaoCheItem.LastLifeMagicTick;

            //如果还没到时间，则跳过
            if (subTicks < (5 * 1000))
            {
                return;
            }

            biaoCheItem.LastLifeMagicTick = nowTicks;
            if (biaoCheItem.CurrentLifeV <= 0)
            {
                return;
            }

            //判断如果血量少于最大血量
            if (biaoCheItem.CurrentLifeV < biaoCheItem.LifeV)
            {
                double lifeMax = biaoCheItem.CutLifeV;
                lifeMax += biaoCheItem.CurrentLifeV;
                if (biaoCheItem.CurrentLifeV > 0)
                {
                    biaoCheItem.CurrentLifeV = (int)Global.GMin(biaoCheItem.LifeV, lifeMax);

                    List<Object> objList = Global.GetAll9Clients(biaoCheItem);

                    //镖车血变化(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOtherBiaoCheLifeV(sl, pool, objList, biaoCheItem.BiaoCheID, biaoCheItem.CurrentLifeV);

                    //GameManager.SystemServerEvents.AddEvent(string.Format("镖车加血, BiaoCheID={0}({1}), Add={2}, Life={3}", biaoCheItem.BiaoCheID, biaoCheItem.BiaoCheName, biaoCheItem.CutLifeV, biaoCheItem.CurrentLifeV), EventLevels.Debug);
                }
            }
        }

        /// <summary>
        /// 处理已经镖车的超时
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessAllBiaoCheItems(SocketListener sl, TCPOutPacketPool pool)
        {
            List<BiaoCheItem> biaoCheItemList = new List<BiaoCheItem>();
            lock (_ID2BiaoCheDict)
            {
                foreach (var val in _ID2BiaoCheDict.Values)
                {
                    biaoCheItemList.Add(val);
                }
            }

            long nowTicks = TimeUtil.NOW();

            BiaoCheItem biaoCheItem = null;
            for (int i = 0; i < biaoCheItemList.Count; i++)
            {
                biaoCheItem = biaoCheItemList[i];

                //处理镖车的超时
                if (ProcessBiaoCheOverTime(sl, pool, nowTicks, biaoCheItem))
                {
                    continue;
                }

                //处理镖车的死亡
                if (ProcessBiaoCheDead(sl, pool, nowTicks, biaoCheItem))
                {
                    continue;
                }

                //处理镖车的加血
                ProcessBiaoCheAddLife(sl, pool, nowTicks, biaoCheItem);
            }
        }

        #endregion 处理已经镖车加血/死亡/超时

        #region 处理角色移动时的镖车发送

        /// <summary>
        /// 发送镖车到给自己
        /// </summary>
        /// <param name="client"></param>
        public static void SendMySelfBiaoCheItems(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            BiaoCheItem biaoCheItem = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is BiaoCheItem))
                {
                    continue;
                }

                if ((objsList[i] as BiaoCheItem).CurrentLifeV <= 0)
                {
                    continue;
                }

                biaoCheItem = objsList[i] as BiaoCheItem;
                GameManager.ClientMgr.NotifyMySelfNewBiaoChe(sl, pool, client, biaoCheItem);
            }
        }

        /// <summary>
        /// 删除自己哪儿的镖车
        /// </summary>
        /// <param name="client"></param>
        public static void DelMySelfBiaoCheItems(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            BiaoCheItem biaoCheItem = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is BiaoCheItem))
                {
                    continue;
                }

                biaoCheItem = objsList[i] as BiaoCheItem;
                GameManager.ClientMgr.NotifyMySelfDelBiaoChe(sl, pool, client, biaoCheItem.BiaoCheID);
            }
        }

        #endregion 处理角色移动时的镖车发送

        #region 查找指定范围内的镖车

        /// <summary>
        /// 查找指定圆周范围内的镖车
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static void LookupEnemiesInCircle(GameClient client, int mapCode, int toX, int toY, int radius, List<int> enemiesList)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objList) return;

            Point center = new Point(toX, toY);
            for (int i = 0; i < objList.Count; i++)
            {
                if (!(objList[i] is BiaoCheItem))
                {
                    continue;
                }

                //非敌对对象
                if (null != client && !Global.IsOpposition(client, (objList[i] as BiaoCheItem)))
                {
                    continue;
                }

                //不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objList[i] as BiaoCheItem).CopyMapID)
                {
                    continue;
                }

                Point pt = new Point((objList[i] as BiaoCheItem).PosX, (objList[i] as BiaoCheItem).PosY);
                if (Global.InCircle(pt, center, (double)radius))
                {
                    enemiesList.Add((objList[i] as BiaoCheItem).BiaoCheID);
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
        public static void LookupEnemiesInCircleByAngle(GameClient client, int direction, int mapCode, int toX, int toY, int radius, List<BiaoCheItem> enemiesList, double angle)
        {
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            List<Object> objList = mapGrid.FindObjects((int)toX, (int)toY, radius);
            if (null == objList) return;

            double loAngle = 0.0, hiAngle = 0.0;
            Global.GetAngleRangeByDirection(direction, angle, out loAngle, out hiAngle);
            Point center = new Point(toX, toY);
            for (int i = 0; i < objList.Count; i++)
            {
                if (!(objList[i] is BiaoCheItem))
                {
                    continue;
                }

                //非敌对对象
                if (null != client && !Global.IsOpposition(client, (objList[i] as BiaoCheItem)))
                {
                    continue;
                }

                //不在同一个副本
                if (null != client && client.ClientData.CopyMapID != (objList[i] as BiaoCheItem).CopyMapID)
                {
                    continue;
                }

                Point pt = new Point((objList[i] as BiaoCheItem).PosX, (objList[i] as BiaoCheItem).PosY);
                if (Global.InCircleByAngle(pt, center, (double)radius, loAngle, hiAngle))
                {
                    enemiesList.Add((objList[i] as BiaoCheItem));
                }
                else
                {
                    ;
                }
            }
        }

        #endregion 查找指定范围内的镖车

        #region 查找指定格子内的镖车

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
                if (!(objList[i] is BiaoCheItem))
                {
                    continue;
                }

                //非敌对对象

                //不在同一个副本
                if (null != attacker && attacker.CurrentCopyMapID != (objList[i] as BiaoCheItem).CopyMapID)
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
                enemiesList.Add((objList[i] as BiaoCheItem).BiaoCheID);
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

        #endregion 查找指定格子内的镖车

        #region 战斗相关

        /// <summary>
        /// 是否能被攻击
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public static bool CanAttack(GameClient client, BiaoCheItem enemy)
        {
            if (null == enemy) return false;
            if (enemy.YaBiaoID == NotAttackYaBiaoID) return false;
            int maxlevel = enemy.MaxLevel < 0 ? 1000 : enemy.MaxLevel;
            if (client.ClientData.Level > maxlevel)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 通知其他人被攻击(单攻)，并且被伤害(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public static int NotifyInjured(SocketListener sl, TCPOutPacketPool pool, GameClient client, BiaoCheItem enemy, int burst, int injure, double injurePercent, int attackType, bool forceBurst, int addInjure, double attackPercent, int addAttackMin, int addAttackMax, double baseRate = 1.0, int addVlue = 0, int nHitFlyDistance = 0)
        {
            int ret = 0;
            object obj = enemy;
            {
                //怪物必须或者才操作
                if ((obj as BiaoCheItem).CurrentLifeV > 0)
                {
                    injure = (obj as BiaoCheItem).CutLifeV;

                    // 技能改造[3/13/2014 LiaoWei]
                    injure = (int)(injure * baseRate + addVlue);

                    // 技能中可配置伤害百分比
                    injure = (int)(injure * injurePercent);

                    ret = injure;

                    (obj as BiaoCheItem).CurrentLifeV -= (int)injure; //是否需要锁定
                    (obj as BiaoCheItem).CurrentLifeV = Global.GMax((obj as BiaoCheItem).CurrentLifeV, 0);
                    double enemyLife = (int)(obj as BiaoCheItem).CurrentLifeV;
                    (obj as BiaoCheItem).AttackedRoleID = client.ClientData.RoleID;

                    //判断是否将给敌人的伤害转化成自己的血量增长
                    GameManager.ClientMgr.SpriteInjure2Blood(sl, pool, client, injure);

                    GameManager.SystemServerEvents.AddEvent(string.Format("镖车减血, Injure={0}, Life={1}", injure, enemyLife), EventLevels.Debug);

                    //判断怪物是否死亡
                    if ((int)(obj as BiaoCheItem).CurrentLifeV <= 0)
                    {
                        GameManager.SystemServerEvents.AddEvent(string.Format("镖车死亡, roleID={0}", (obj as BiaoCheItem).BiaoCheID), EventLevels.Debug);

                        /// 处理怪物死亡
                        ProcessBiaoCheDead(sl, pool, client, (obj as BiaoCheItem));
                    }

                    if ((obj as BiaoCheItem).AttackedRoleID >= 0 && (obj as BiaoCheItem).AttackedRoleID != client.ClientData.RoleID)
                    {
                        GameClient findClient = GameManager.ClientMgr.FindClient((obj as BiaoCheItem).AttackedRoleID);
                        if (null != findClient)
                        {
                            //通知其他在线客户端
                            GameManager.ClientMgr.NotifySpriteInjured(sl, pool, findClient, findClient.ClientData.MapCode, findClient.ClientData.RoleID, (obj as BiaoCheItem).BiaoCheID, 0, 0, enemyLife, findClient.ClientData.Level, new Point(-1, -1));

                            //向自己发送敌人受伤的信息
                            ClientManager.NotifySelfEnemyInjured(sl, pool, findClient, findClient.ClientData.RoleID, enemy.BiaoCheID, 0, 0, enemyLife, 0);
                        }
                    }

                    //通知其他在线客户端
                    GameManager.ClientMgr.NotifySpriteInjured(sl, pool, client, client.ClientData.MapCode, client.ClientData.RoleID, (obj as BiaoCheItem).BiaoCheID, burst, injure, enemyLife, client.ClientData.Level, new Point(-1, -1));

                    //向自己发送敌人受伤的信息
                    ClientManager.NotifySelfEnemyInjured(sl, pool, client, client.ClientData.RoleID, enemy.BiaoCheID, burst, injure, enemyLife, 0);

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
            object obj = FindBiaoCheByID(enemy);
            if (null != obj)
            {
                //怪物必须或者才操作
                if ((obj as BiaoCheItem).CurrentLifeV > 0)
                {
                    //处理BOSS克星
                    injure = (obj as BiaoCheItem).CutLifeV;

                    // 技能改造[3/13/2014 LiaoWei]
                    injure = (int)(injure * baseRate + addVlue); 

                    (obj as BiaoCheItem).CurrentLifeV -= (int)injure; //是否需要锁定
                    (obj as BiaoCheItem).CurrentLifeV = Global.GMax((obj as BiaoCheItem).CurrentLifeV, 0);
                    double enemyLife = (int)(obj as BiaoCheItem).CurrentLifeV;
                    (obj as BiaoCheItem).AttackedRoleID = client.ClientData.RoleID;

                    //判断是否将给敌人的伤害转化成自己的血量增长
                    GameManager.ClientMgr.SpriteInjure2Blood(sl, pool, client, injure);

                    GameManager.SystemServerEvents.AddEvent(string.Format("镖车减血, Injure={0}, Life={1}", injure, enemyLife), EventLevels.Debug);

                    //判断怪物是否死亡
                    if ((int)(obj as BiaoCheItem).CurrentLifeV <= 0)
                    {
                        GameManager.SystemServerEvents.AddEvent(string.Format("镖车死亡, roleID={0}", (obj as BiaoCheItem).BiaoCheID), EventLevels.Debug);

                        /// 处理镖车死亡
                        ProcessBiaoCheDead(sl, pool, client, (obj as BiaoCheItem));
                    }

                    if ((obj as BiaoCheItem).AttackedRoleID >= 0 && (obj as BiaoCheItem).AttackedRoleID != client.ClientData.RoleID)
                    {
                        GameClient findClient = GameManager.ClientMgr.FindClient((obj as BiaoCheItem).AttackedRoleID);
                        if (null != findClient)
                        {
                            //通知其他在线客户端
                            GameManager.ClientMgr.NotifySpriteInjured(sl, pool, findClient, findClient.ClientData.MapCode, findClient.ClientData.RoleID, (obj as BiaoCheItem).BiaoCheID, 0, 0, enemyLife, findClient.ClientData.Level, new Point(-1, -1));

                            //向自己发送敌人受伤的信息
                            ClientManager.NotifySelfEnemyInjured(sl, pool, findClient, findClient.ClientData.RoleID, (obj as BiaoCheItem).BiaoCheID, 0, 0, enemyLife, 0);
                        }
                    }

                    //通知其他在线客户端
                    GameManager.ClientMgr.NotifySpriteInjured(sl, pool, client, client.ClientData.MapCode, client.ClientData.RoleID, (obj as BiaoCheItem).BiaoCheID, burst, injure, enemyLife, client.ClientData.Level, new Point(-1, -1));

                    //向自己发送敌人受伤的信息
                    ClientManager.NotifySelfEnemyInjured(sl, pool, client, client.ClientData.RoleID, (obj as BiaoCheItem).BiaoCheID, burst, injure, enemyLife, 0);

                    //通知紫名信息(限制当前地图)
                    if (!client.ClientData.DisableChangeRolePurpleName)
                    {
                        GameManager.ClientMgr.ForceChangeRolePurpleName2(sl, pool, client);
                    }
                }
            }
        }

        /// <summary>
        /// 处理镖车死亡
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        private static void ProcessBiaoCheDead(SocketListener sl, TCPOutPacketPool pool, GameClient client, BiaoCheItem biaoCheItem)
        {
            if (biaoCheItem.HandledDead)
            {
                return;
            }

            biaoCheItem.HandledDead = true;
            biaoCheItem.BiaoCheDeadTicks = TimeUtil.NOW();

            GameClient findClient = null;
            if (biaoCheItem.AttackedRoleID >= 0 && biaoCheItem.AttackedRoleID != client.ClientData.RoleID)
            {
                findClient = GameManager.ClientMgr.FindClient(biaoCheItem.AttackedRoleID);
                if (null != findClient)
                {
                    client = findClient;
                }
            }

            int yinLiang = 0, experience = 0, yaJin = 0;

            //获取运镖的银两和经验
            Global.GetYaBiaoReward(biaoCheItem.YaBiaoID, out yinLiang, out experience, out yaJin);

            //获取劫镖后，获取的收入的系数
            //int killBiaoCheNum = Global.GetKillBiaoCheNum(client, biaoCheItem);

            yinLiang /= 2;
            experience = 0;

            //增加今日的劫镖次数
            int jieBiaoNum = Global.IncTotayJieBiaoNum(client);
            //int jieBiaoDivNum = (int)Math.Pow(2, jieBiaoNum - 1);
            //if (jieBiaoDivNum > 0)
            //{
            //    yinLiang /= jieBiaoDivNum;
            //    experience /= jieBiaoDivNum;
            //}
            //else
            //{
            //    yinLiang = 0;
            //    experience = 0;
            //}

            //处理角色经验
            if (experience > 0)
            {
                GameManager.ClientMgr.ProcessRoleExperience(client, experience, true, false);
            }

            //奖励用户银两
            //异步写数据库，写入银两
            if (yinLiang > 0)
            {
                //过滤银两奖励
                yinLiang = Global.FilterValue(client, yinLiang);
                GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, yinLiang, "押镖奖励");

                GameManager.SystemServerEvents.AddEvent(string.Format("角色获取银两, roleID={0}({1}), Money={2}, newMoney={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.YinLiang, yinLiang), EventLevels.Record);
            }

            //更新押镖的数据库状态
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEYABIAODATASTATE,
                string.Format("{0}:{1}",
                biaoCheItem.OwnerRoleID,
                1
                ),
                null, client.ServerId);

            findClient = GameManager.ClientMgr.FindClient(biaoCheItem.OwnerRoleID);
            if (null != findClient)
            {
                //修改押镖数据
                if (null != findClient.ClientData.MyYaBiaoData)
                {
                    findClient.ClientData.MyYaBiaoData.State = 1;

                    //将新的押镖数据通知客户端
                    GameManager.ClientMgr.NotifyYaBiaoData(findClient);
                }
            }
            else //对方已经离线或者在其他线
            {
                string gmCmdData = string.Format("-setybstate2 {0} 1", biaoCheItem.OwnerRoleID);

                //转发GM消息到DBServer
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                    string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, "", 0, "", 0, gmCmdData, 0, 0, -1),
                    null, GameManager.LocalServerIdForNotImplement);
            }

            //镖车被劫杀的提示
            Global.BroadcastKillBiaoCheHint(client, biaoCheItem);
        }

        #endregion 战斗相关
    }
}
