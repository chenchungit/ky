using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using GameServer.Interface;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 执行项定义
    /// </summary>
    public class GridMagicHelperItem
    {
        /// <summary>
        /// 公式ID
        /// </summary>
        public MagicActionIDs MagicActionID
        {
            get;
            set;
        }

        /// <summary>
        /// 公式参数
        /// </summary>
        public double[] MagicActionParams
        {
            get;
            set;
        }

        /// <summary>
        /// 起始时间
        /// </summary>
        public long StartedTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 上次执行时间
        /// </summary>
        public long LastTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 执行次数
        /// </summary>
        public int ExecutedNum
        {
            get;
            set;
        }

        /// <summary>
        /// 地图编号
        /// </summary>
        public int MapCode
        {
            get;
            set;
        }

        /// <summary>
        /// deco autoid
        /// </summary>
        public int DecoID
        {
            get;
            set;
        }
    }

    public struct GridMagicHelperItemKey : IComparer<GridMagicHelperItemKey>
    {
        //默认比较器
        public static GridMagicHelperItemKey Comparer = new GridMagicHelperItemKey();

        public int MapCode;
        public int PosX;
        public int PosY;
        public int CopyMapID;
        public MagicActionIDs MagicActionID;
        public MagicActionIDs MagicActionID2;

        public int Compare(GridMagicHelperItemKey x, GridMagicHelperItemKey y)
        {
            int ret = x.MapCode - y.MapCode;
            if (ret != 0) return ret;
            ret = x.CopyMapID - y.CopyMapID;
            if (ret != 0) return ret;
            ret = x.PosX - y.PosX;
            if (ret != 0) return ret;
            ret = x.PosY - y.PosY;
            if (ret != 0) return ret;
            ret = x.MagicActionID - y.MagicActionID;
            if (ret != 0) return ret;
            ret = x.MagicActionID2 - y.MagicActionID2;
            return ret;
        }
    }

    /// <summary>
    /// 执行项定义
    /// </summary>
    public class GridMagicHelperItemEx
    {
        /// <summary>
        /// 公式ID
        /// </summary>
        public MagicActionIDs MagicActionID;

        /// <summary>
        /// 公式参数
        /// </summary>
        public double[] MagicActionParams;

        /// <summary>
        /// 起始时间
        /// </summary>
        public long StartedTicks;

        /// <summary>
        /// 上次执行时间
        /// </summary>
        public long LastTicks;

        /// <summary>
        /// 执行次数
        /// </summary>
        public int ExecutedNum;

        /// <summary>
        /// 地图编号
        /// </summary>
        public int MapCode;

        /// <summary>
        /// 技能公示
        /// </summary>
        public MagicActionIDs MagicActionID2;

        /// <summary>
        /// 附加效果公式参数
        /// </summary>
        public double[] MagicActionParams2;

        /// <summary>
        /// 受影响的坐标列表
        /// </summary>
        public List<Point> PointList = new List<Point>();

        /// <summary>
        /// 攻击者ID
        /// </summary>
        public int AttackerRoleId;
    }

    public class MapGridMagicHelper
    {
        #region 基础数据

        /// <summary>
        /// 技能辅助项词典
        /// </summary>
        private Dictionary<string, Dictionary<MagicActionIDs, GridMagicHelperItem>> _GridMagicHelperDict = new Dictionary<string, Dictionary<MagicActionIDs, GridMagicHelperItem>>();

        /// <summary>
        /// 技能辅助项词典(扩展)
        /// </summary>
        private SortedDictionary<GridMagicHelperItemKey, GridMagicHelperItemEx> _GridMagicHelperDictEx = new SortedDictionary<GridMagicHelperItemKey, GridMagicHelperItemEx>(GridMagicHelperItemKey.Comparer);

        /// <summary>
        /// 添加技能辅助项
        /// </summary>
        /// <param name="magicActionID"></param>
        /// <param name="magicActionParams"></param>
        public void AddMagicHelper(MagicActionIDs magicActionID, double[] magicActionParams, int mapCode, Point centerGridXY, int gridWidthNum, int gridHeightNum, int copyMapID = -1)
        {
            if (copyMapID < 0)
            {
                copyMapID = -1;
            }

            GameMap gameMap = GameManager.MapMgr.DictMaps[mapCode];

            List<Point> pts = new List<Point>();
            pts.Add(centerGridXY);

            for (int i = (int)centerGridXY.X - gridWidthNum; i <= centerGridXY.X + gridWidthNum; i++ )
            {
                for (int j = (int)centerGridXY.Y - gridHeightNum; j <= centerGridXY.Y + gridHeightNum; j++ )
                {
                    pts.Add(new Point(i, j));
                }
            }

            for (int i = 0; i < pts.Count; i++)
            {
                ///障碍上边，不能放火墙
                if (Global.InOnlyObs(ObjectTypes.OT_CLIENT, mapCode, (int)pts[i].X, (int)pts[i].Y))
                {
                    continue;
                }

                Dictionary<MagicActionIDs, GridMagicHelperItem> dict = null;
                string key = string.Format("{0}_{1}_{2}", pts[i].X, pts[i].Y, copyMapID);
                lock (_GridMagicHelperDict)
                {
                    if (!_GridMagicHelperDict.TryGetValue(key, out dict))
                    {
                        dict = new Dictionary<MagicActionIDs, GridMagicHelperItem>();
                        _GridMagicHelperDict[key] = dict;
                    }
                }

                lock (dict)
                {
                    if (dict.ContainsKey(magicActionID)) //一个格子上边，同时只能有一个buffer
                    {
                        continue;
                    }
                }

                GridMagicHelperItem magicHelperItem = new GridMagicHelperItem()
                {
                    MagicActionID = magicActionID,
                    MagicActionParams = magicActionParams,
                    StartedTicks = TimeUtil.NOW(),
                    LastTicks = TimeUtil.NOW(),
                    ExecutedNum = 0,
                    MapCode = mapCode,
                };

                lock (dict)
                {
                    dict[magicHelperItem.MagicActionID] = magicHelperItem;
                }
            }
        }

        /// <summary>
        /// 添加技能辅助项（扩展）
        /// </summary>
        /// <param name="magicActionID"></param>
        /// <param name="magicActionParams"></param>
        /// <param name="mapCode"></param>
        /// <param name="centerGridXY"></param>
        /// <param name="gridWidthNum"></param>
        /// <param name="gridHeightNum"></param>
        /// <param name="copyMapID"></param>
        public void AddMagicHelperEx(MagicActionIDs magicActionID, double[] magicActionParams, int mapCode, int posX, int posY, int copyMapID = -1)
        {
            if (copyMapID < 0)
            {
                copyMapID = -1;
            }

            GameMap gameMap = GameManager.MapMgr.DictMaps[mapCode];

            ///障碍上边，不能放火墙
            if (Global.InOnlyObs(ObjectTypes.OT_CLIENT, mapCode, posX, posY))
            {
                return;
            }

            GridMagicHelperItemKey itemKey = new GridMagicHelperItemKey()
                {
                    MapCode = mapCode,
                    PosX = posX,
                    PosY = posY,
                    CopyMapID = copyMapID,
                    MagicActionID = magicActionID,
                };

            GridMagicHelperItemEx magicHelperItem = new GridMagicHelperItemEx()
            {
                MagicActionID = magicActionID,
                MagicActionParams = magicActionParams,
                StartedTicks = TimeUtil.NOW(),
                LastTicks = TimeUtil.NOW(),
                ExecutedNum = 0,
                MapCode = mapCode,
            };

            lock (_GridMagicHelperDictEx)
            {
                if (_GridMagicHelperDictEx.ContainsKey(itemKey))
                {
                    return;//一个格子上边，同时只能有一个buffer
                }
                _GridMagicHelperDictEx.Add(itemKey, magicHelperItem);
            }
        }

        /// <summary>
        /// 添加技能辅助项（扩展）
        /// </summary>
        /// <param name="magicActionID"></param>
        /// <param name="magicActionParams"></param>
        /// <param name="mapCode"></param>
        /// <param name="centerGridXY"></param>
        /// <param name="gridWidthNum"></param>
        /// <param name="gridHeightNum"></param>
        /// <param name="copyMapID"></param>
        public void AddMagicHelperExAction(MagicActionIDs magicActionID, double[] magicActionParams, int mapCode, Point centerGridXY, int gridWidthNum, int gridHeightNum, int copyMapID = -1)
        {
            if (copyMapID < 0)
            {
                copyMapID = -1;
            }

            int gridX = (int)centerGridXY.X;
            int gridY = (int)centerGridXY.Y;
            GameMap gameMap = GameManager.MapMgr.DictMaps[mapCode];

            ///障碍上边，不能放火墙
            if (Global.InOnlyObs(ObjectTypes.OT_CLIENT, mapCode, gridX, gridY))
            {
                return;
            }

            GridMagicHelperItemKey itemKey = new GridMagicHelperItemKey()
                {
                    MapCode = mapCode,
                    PosX = gridX,
                    PosY = gridY,
                    CopyMapID = copyMapID,
                    MagicActionID = magicActionID,
                };

            GridMagicHelperItemEx magicHelperItem = new GridMagicHelperItemEx()
            {
                MagicActionID = magicActionID,
                MagicActionParams = magicActionParams,
                StartedTicks = TimeUtil.NOW(),
                LastTicks = TimeUtil.NOW(),
                ExecutedNum = 0,
                MapCode = mapCode,
                AttackerRoleId = (int)magicActionParams[2],
            };

            int magicActionID2Index = 3;
            int length = magicActionParams.Length - magicActionID2Index - 1;
            itemKey.MagicActionID2 = (MagicActionIDs)(int)magicActionParams[magicActionID2Index];
            magicHelperItem.MagicActionParams2 = new double[length];
            Array.Copy(magicActionParams, magicActionID2Index + 1, magicHelperItem.MagicActionParams2, 0, length);

            HashSet<Point> pts = new HashSet<Point>();
            pts.Add(centerGridXY);
            for (int i = (int)centerGridXY.X - gridWidthNum; i <= centerGridXY.X + gridWidthNum; i++ )
            {
                for (int j = (int)centerGridXY.Y - gridHeightNum; j <= centerGridXY.Y + gridHeightNum; j++ )
                {
                    pts.Add(new Point(i, j));
                }
            }

            foreach (var pt in pts)
            {
                ///障碍上边，不能放火墙
                if (Global.InOnlyObs(ObjectTypes.OT_CLIENT, mapCode, (int)pt.X, (int)pt.Y))
                {
                    continue;
                }

                magicHelperItem.PointList.Add(pt);
            }

            lock (_GridMagicHelperDictEx)
            {
                if (_GridMagicHelperDictEx.ContainsKey(itemKey))
                {
                    return;//一个格子上边，同时只能有一个buffer
                }
                _GridMagicHelperDictEx.Add(itemKey, magicHelperItem);
            }
        }

        /// <summary>
        /// 添加技能辅助项
        /// </summary>
        /// <param name="magicActionID"></param>
        /// <param name="magicActionParams"></param>
        public bool ExistsMagicHelper(MagicActionIDs magicActionID, int gridX, int gridY, int copyMapID = -1)
        {
            if (copyMapID < 0)
            {
                copyMapID = -1;
            }

            string key = string.Format("{0}_{1}_{2}", gridX, gridY, copyMapID);
            Dictionary<MagicActionIDs, GridMagicHelperItem> dict = null;
            lock (_GridMagicHelperDict)
            {
                if (!_GridMagicHelperDict.TryGetValue(key, out dict) || null == dict)
                {
                    return false;
                }
            }

            lock (dict)
            {
                return dict.ContainsKey(magicActionID);
            }
        }

        #endregion 基础数据

        #region 执行相关辅助项

        /// <summary>
        /// 是否可以执行选项
        /// </summary>
        /// <param name="magicHelperItem"></param>
        /// <returns></returns>
        private bool CanExecuteItem(Dictionary<MagicActionIDs, GridMagicHelperItem> dict, GridMagicHelperItem magicHelperItem, double effectSecs, int maxNum)
        {
            long nowTicks = TimeUtil.NOW();
            long ticks = magicHelperItem.StartedTicks + (long)(effectSecs * 1000);

            if (maxNum <= 0)
            {
                //判断是否超过了时间
                if (nowTicks >= ticks)
                {
                    lock (dict)
                    {
                        dict.Remove(magicHelperItem.MagicActionID);
                    }

                    return false;
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("CanExecuteItem, {0}", nowTicks - ticks);
                    return true;
                }
            }

            //判断是否超过了次数
            if (magicHelperItem.ExecutedNum >= maxNum)
            {
                lock (dict)
                {
                    dict.Remove(magicHelperItem.MagicActionID);
                }

                return false;
            }

            long ticksSlot = (long)((effectSecs / maxNum) * 1000);

            //判断是否超过了时间
            if (nowTicks - magicHelperItem.LastTicks < ticksSlot)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否可以执行选项
        /// </summary>
        /// <param name="magicHelperItem"></param>
        /// <returns></returns>
        private bool CanExecuteItemEx(GridMagicHelperItemKey key, GridMagicHelperItemEx magicHelperItem, double effectSecs, int maxNum, long nowTicks)
        {
            long ticks = magicHelperItem.StartedTicks + (long)(effectSecs * 1000);

            if (maxNum <= 0)
            {
                //判断是否超过了时间
                if (nowTicks >= ticks)
                {
                    lock (_GridMagicHelperDictEx)
                    {
                        _GridMagicHelperDictEx.Remove(key);
                    }

                    return false;
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("CanExecuteItem, {0}", nowTicks - ticks);
                    return true;
                }
            }

            //判断是否超过了次数
            if (magicHelperItem.ExecutedNum >= maxNum)
            {
                lock (_GridMagicHelperDictEx)
                {
                    _GridMagicHelperDictEx.Remove(key);
                }

                return false;
            }

            long ticksSlot = (long)((effectSecs / maxNum) * 1000);

            //判断是否超过了时间
            if (nowTicks - magicHelperItem.LastTicks < ticksSlot)
            {
                return false;
            }

            return true;
        }

        /// </summary>
        public void ExecuteMAttack(string gridXY, Dictionary<MagicActionIDs, GridMagicHelperItem> dict)
        {
            string[] fields = gridXY.Split('_'); 
            int gridX = Global.SafeConvertToInt32(fields[0]);
            int gridY = Global.SafeConvertToInt32(fields[1]);

            GridMagicHelperItem magicHelperItem = null;
            lock (dict)
            {
                dict.TryGetValue(MagicActionIDs.FIRE_WALL, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(dict, magicHelperItem, (int)magicHelperItem.MagicActionParams[0], (int)magicHelperItem.MagicActionParams[1])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW();

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            double attackPercent = magicHelperItem.MagicActionParams[2];
            int attacker = (int)magicHelperItem.MagicActionParams[3];
            if (-1 != attacker)
            {
                GameClient client = GameManager.ClientMgr.FindClient(attacker);
                if (null != client)
                {
                    List<Object> enemiesList = new List<object>();
                    GameManager.ClientMgr.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                    GameManager.MonsterMgr.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                    BiaoCheManager.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                    JunQiManager.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                    for (int x = 0; x < enemiesList.Count; x++)
                    {
                        IObject obj = enemiesList[x] as IObject;
                        if (obj.CurrentCopyMapID != client.CurrentCopyMapID)
                        {
                            continue;
                        }

                        if (obj is GameClient) //被攻击者是角色
                        {
                            if ((obj as GameClient).ClientData.RoleID != attacker && Global.IsOpposition(client, (obj as GameClient)))
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as GameClient, 0, 0, 1.0, 1, false, 0, attackPercent, 0, 0, 0, 0.0, 0.0, false);
                            }
                        }
                        else if (obj is Monster) //被攻击者是怪物
                        {
                            if (Global.IsOpposition(client, (obj as Monster)))
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as Monster, 0, 0, 1.0, 1, false, 0, attackPercent, 0, 0, 0, 0.0, 0.0, false);
                            }
                        }
                        else if (obj is BiaoCheItem) //被攻击者是镖车
                        {
                            if (Global.IsOpposition(client, (obj as BiaoCheItem)))
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as BiaoCheItem, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else if (obj is JunQiItem) //被攻击者是帮旗
                        {
                            if (Global.IsOpposition(client, (obj as JunQiItem)))
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as JunQiItem, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else if (obj is FakeRoleItem) //被攻击者是假人
                        {
                            if (Global.IsOpposition(client, (obj as FakeRoleItem)))
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as FakeRoleItem, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                    }
                }
            }
        }

        /// </summary>
        public void ExecuteMUFireWall(int id, string gridXY, Dictionary<MagicActionIDs, GridMagicHelperItem> dict)
        {
            string[] fields = gridXY.Split('_');
            int gridX = Global.SafeConvertToInt32(fields[0]);
            int gridY = Global.SafeConvertToInt32(fields[1]);

            GridMagicHelperItem magicHelperItem = null;
            lock (dict)
            {
                dict.TryGetValue((MagicActionIDs)id, out magicHelperItem);
            }

            if (null == magicHelperItem) return;
            if (!CanExecuteItem(dict, magicHelperItem, (int)magicHelperItem.MagicActionParams[0], (int)magicHelperItem.MagicActionParams[1])) return;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = TimeUtil.NOW();

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int addValue = (int)magicHelperItem.MagicActionParams[2];
            int attacker = (int)magicHelperItem.MagicActionParams[3];
            double baseRate = magicHelperItem.MagicActionParams[4];
            if (-1 != attacker)
            {
                GameClient client = GameManager.ClientMgr.FindClient(attacker);
                if (null != client)
                {
                    List<Object> enemiesList = new List<object>();
                    GameManager.ClientMgr.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                    GameManager.MonsterMgr.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                    BiaoCheManager.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                    JunQiManager.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                    for (int x = 0; x < enemiesList.Count; x++)
                    {
                        IObject obj = enemiesList[x] as IObject;
                        if (obj.CurrentCopyMapID != client.CurrentCopyMapID)
                        {
                            continue;
                        }

                        if (obj is GameClient) //被攻击者是角色
                        {
                            if ((obj as GameClient).ClientData.RoleID != attacker && Global.IsOpposition(client, (obj as GameClient)))
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as GameClient, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, false, baseRate, addValue);
                            }
                        }
                        else if (obj is Monster) //被攻击者是怪物
                        {
                            if (Global.IsOpposition(client, (obj as Monster)))
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as Monster, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                            }
                        }
                        else if (obj is BiaoCheItem) //被攻击者是镖车
                        {
                            if (Global.IsOpposition(client, (obj as BiaoCheItem)))
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as BiaoCheItem, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, baseRate, addValue);
                            }
                        }
                        else if (obj is JunQiItem) //被攻击者是帮旗
                        {
                            if (Global.IsOpposition(client, (obj as JunQiItem)))
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as JunQiItem, 0, 0, 1.0, 1, false, 0, 0, 0, 0, baseRate, addValue);
                            }
                        }
                        else if (obj is FakeRoleItem) //被攻击者是假人
                        {
                            if (Global.IsOpposition(client, (obj as FakeRoleItem)))
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as FakeRoleItem, 0, 0, 1.0, 1, false, 0, baseRate, addValue, 0);
                            }
                        }
                    }
                }
                else
                {
                    Monster monster = GameManager.MonsterMgr.FindMonster(magicHelperItem.MapCode, attacker);
                    if (null != monster)
                    {
                        List<Object> enemiesList = new List<object>();
                        GameManager.ClientMgr.LookupEnemiesAtGridXY(monster, gridX, gridY, enemiesList);
                        GameManager.MonsterMgr.LookupEnemiesAtGridXY(monster, gridX, gridY, enemiesList);
                        BiaoCheManager.LookupEnemiesAtGridXY(monster, gridX, gridY, enemiesList);
                        JunQiManager.LookupEnemiesAtGridXY(monster, gridX, gridY, enemiesList);
                        for (int x = 0; x < enemiesList.Count; x++)
                        {
                            IObject obj = enemiesList[x] as IObject;
                            if (obj.CurrentCopyMapID != monster.CurrentCopyMapID)
                            {
                                continue;
                            }

                            if (obj is GameClient) //被攻击者是角色
                            {
                                if ((obj as GameClient).ClientData.RoleID != attacker && Global.IsOpposition(monster, (obj as GameClient)))
                                {
                                    GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        monster, obj as GameClient, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                                }
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                if (Global.IsOpposition(monster, (obj as Monster)))
                                {
                                    GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        monster, obj as Monster, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 执行所有项
        /// </summary>
        public void ExecuteAllItems()
        {
            List<string> list = new List<string>();
            lock (_GridMagicHelperDict)
            {
                list = _GridMagicHelperDict.Keys.ToList<string>();
                //foreach (var key in _GridMagicHelperDict.Keys)
                //{
                //    list.Add(key);
                //}
            }

            Dictionary<MagicActionIDs, GridMagicHelperItem> dict = null;
            for (int i = 0; i < list.Count; i++)
            {
                dict = null;
                lock (_GridMagicHelperDict)
                {
                    _GridMagicHelperDict.TryGetValue(list[i], out dict);
                }

                if (null != dict)
                {
                    //执行持续的魔法伤害
                    ExecuteMAttack(list[i], dict);

                    ExecuteMUFireWall((int)MagicActionIDs.MU_FIRE_WALL1, list[i], dict);
                    ExecuteMUFireWall((int)MagicActionIDs.MU_FIRE_WALL9, list[i], dict);
                    ExecuteMUFireWall((int)MagicActionIDs.MU_FIRE_WALL25, list[i], dict);
                }
            }
        }

        /// <summary>
        /// 获取某对象加的持续地图BUFF数量
        /// </summary>
        public int GetObjectAddMapBuffer(int nAttackID)
        {
            int nCount = 0;
            lock (_GridMagicHelperDictEx)
            {
                foreach (var kv in _GridMagicHelperDictEx)
                {
                    if (((int)kv.Value.MagicActionParams[3]) == nAttackID)
                    {
                        if (kv.Value.ExecutedNum < 1)
                        {
                            nCount++;
                        }
                    }
                }
            }

            return nCount;
        }

        /// <summary>
        /// 执行所有扩展项
        /// </summary>
        public void ExecuteAllItemsEx()
        {
            long nowTicks = TimeUtil.NOW();
            List<KeyValuePair<GridMagicHelperItemKey, GridMagicHelperItemEx>> list = null;
            lock (_GridMagicHelperDictEx)
            {
                foreach (var kv in _GridMagicHelperDictEx)
                {
                    if (CanExecuteItemEx(kv.Key, kv.Value, (int)kv.Value.MagicActionParams[0], (int)kv.Value.MagicActionParams[1], nowTicks))
                    {
                        if (null == list)
                        {
                            list = new List<KeyValuePair<GridMagicHelperItemKey, GridMagicHelperItemEx>>();
                        }
                        list.Add(kv);
                    }
                }
            }

            if (null != list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    GridMagicHelperItemKey key = list[i].Key;
                    switch (key.MagicActionID)
                    {
                        case MagicActionIDs.MU_FIRE_WALL_X:
                            ExecuteMUFireWall_X(list[i].Key, list[i].Value, nowTicks);
                            break;
                        case MagicActionIDs.MU_FIRE_WALL_ACTION:
                            ExecuteMUFireWallAction(list[i].Key, list[i].Value, nowTicks);
                            break;
                        case MagicActionIDs.MU_FIRE_SECTOR:
                            ExecuteMUFireSector(list[i].Key, list[i].Value, nowTicks);
                            break;
                        case MagicActionIDs.MU_FIRE_STRAIGHT:
                            ExecuteMUFireStraight(list[i].Key, list[i].Value, nowTicks);
                            break;
                    }
                }
            }
        }

        #endregion 执行相关辅助项

        #region 自定义范围的圆形区域伤害

        /// <summary>
        /// 圆形范围的圆形区域伤害
        /// </summary>
        /// <param name="key"></param>
        /// <param name="magicHelperItem"></param>
        /// <param name="nowTicks"></param>
        public void ExecuteMUFireWall_X(GridMagicHelperItemKey key, GridMagicHelperItemEx magicHelperItem, long nowTicks)
        {
            int id = (int)key.MagicActionID;
            int gridX = key.PosX;
            int gridY = key.PosY;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = nowTicks;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int addValue = (int)magicHelperItem.MagicActionParams[2];
            int attacker = (int)magicHelperItem.MagicActionParams[3];
            double baseRate = magicHelperItem.MagicActionParams[4];
            int radio = (int)(magicHelperItem.MagicActionParams[5]); //服务器单位
            int mapGridWidth = (int)(magicHelperItem.MagicActionParams[9]); //格子宽度
            gridX *= mapGridWidth;
            gridY *= mapGridWidth;
            if (-1 != attacker)
            {
                GameClient client = GameManager.ClientMgr.FindClient(attacker);
                if (null != client)
                {
                    List<Object> enemiesList = new List<object>();
                    GameManager.ClientMgr.LookupEnemiesInCircle(magicHelperItem.MapCode, client.ClientData.CopyMapID, gridX, gridY, radio, enemiesList);
                    GameManager.MonsterMgr.LookupEnemiesInCircle(magicHelperItem.MapCode, client.ClientData.CopyMapID, gridX, gridY, radio, enemiesList);

                    for (int x = 0; x < enemiesList.Count; x++)
                    {
                        IObject obj = enemiesList[x] as IObject;
                        if (obj.CurrentCopyMapID != client.CurrentCopyMapID)
                        {
                            continue;
                        }

                        if (obj is GameClient) //被攻击者是角色
                        {
                            if ((obj as GameClient).ClientData.RoleID != attacker && Global.IsOpposition(client, (obj as GameClient)))
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as GameClient, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, false, baseRate, addValue);
                            }
                        }
                        else if (obj is Monster) //被攻击者是怪物
                        {
                            if (Global.IsOpposition(client, (obj as Monster)))
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as Monster, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                            }
                        }
                        else if (obj is BiaoCheItem) //被攻击者是镖车
                        {
                            if (Global.IsOpposition(client, (obj as BiaoCheItem)))
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as BiaoCheItem, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, baseRate, addValue);
                            }
                        }
                        else if (obj is JunQiItem) //被攻击者是帮旗
                        {
                            if (Global.IsOpposition(client, (obj as JunQiItem)))
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as JunQiItem, 0, 0, 1.0, 1, false, 0, 0, 0, 0, baseRate, addValue);
                            }
                        }
                        else if (obj is FakeRoleItem) //被攻击者是假人
                        {
                            if (Global.IsOpposition(client, (obj as FakeRoleItem)))
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as FakeRoleItem, 0, 0, 1.0, 1, false, 0, baseRate, addValue, 0);
                            }
                        }
                    }
                }
                else
                {
                    Monster monster = GameManager.MonsterMgr.FindMonster(magicHelperItem.MapCode, attacker);
                    if (null != monster)
                    {
                        List<Object> enemiesList = new List<object>();
                        GameManager.ClientMgr.LookupEnemiesInCircle(magicHelperItem.MapCode, monster.CopyMapID, gridX, gridY, radio, enemiesList);
                        GameManager.MonsterMgr.LookupEnemiesInCircle(magicHelperItem.MapCode, monster.CopyMapID, gridX, gridY, radio, enemiesList);

                        // SysConOut.WriteLine(string.Format("ExecuteMUFireWall_X执行{0}:{1}:敌人数:{2}", monster.CurrentMagic, TimeUtil.NOW() * 10000, enemiesList.Count));
                        for (int x = 0; x < enemiesList.Count; x++)
                        {
                            IObject obj = enemiesList[x] as IObject;
                            if (obj.CurrentCopyMapID != monster.CurrentCopyMapID)
                            {
                                continue;
                            }

                            if (obj is GameClient) //被攻击者是角色
                            {
                                if ((obj as GameClient).ClientData.RoleID != attacker && Global.IsOpposition(monster, (obj as GameClient)))
                                {
                                    GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        monster, obj as GameClient, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                                }
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                if (Global.IsOpposition(monster, (obj as Monster)))
                                {
                                    GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        monster, obj as Monster, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 圆形范围的圆形区域附加公式效果
        /// </summary>
        /// <param name="key"></param>
        /// <param name="magicHelperItem"></param>
        /// <param name="nowTicks"></param>
        public void ExecuteMUFireWallAction(GridMagicHelperItemKey key, GridMagicHelperItemEx magicHelperItem, long nowTicks)
        {
            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = nowTicks;

            int attacker = (int)magicHelperItem.AttackerRoleId;
            if (-1 != attacker)
            {
                GameClient client = GameManager.ClientMgr.FindClient(attacker);
                if (null != client)
                {
                    List<Object> enemiesList = new List<object>();
                    foreach (var pt in magicHelperItem.PointList)
                    {
                        int gridX = (int)pt.X;
                        int gridY = (int)pt.Y;
                        GameManager.ClientMgr.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                        GameManager.MonsterMgr.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                        //BiaoCheManager.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                        //JunQiManager.LookupEnemiesAtGridXY(client, gridX, gridY, enemiesList);
                    }

                    for (int x = 0; x < enemiesList.Count; x++)
                    {
                        IObject obj = enemiesList[x] as IObject;
                        if (obj.CurrentCopyMapID != client.CurrentCopyMapID)
                        {
                            continue;
                        }

                        MagicAction.ProcessAction(client, obj, key.MagicActionID2, magicHelperItem.MagicActionParams2);
                    }
                }
                else
                {
                    Monster monster = GameManager.MonsterMgr.FindMonster(magicHelperItem.MapCode, attacker);
                    if (null != monster)
                    {
                        List<Object> enemiesList = new List<object>();
                        foreach (var pt in magicHelperItem.PointList)
                        {
                            int gridX = (int)pt.X;
                            int gridY = (int)pt.Y;
                            GameManager.ClientMgr.LookupEnemiesAtGridXY(monster, gridX, gridY, enemiesList);
                            GameManager.MonsterMgr.LookupEnemiesAtGridXY(monster, gridX, gridY, enemiesList);
                            //BiaoCheManager.LookupEnemiesAtGridXY(monster, gridX, gridY, enemiesList);
                            //JunQiManager.LookupEnemiesAtGridXY(monster, gridX, gridY, enemiesList);
                        }

                        // SysConOut.WriteLine(string.Format("ExecuteMUFireWall_X执行{0}:{1}:敌人数:{2}", monster.CurrentMagic, TimeUtil.NOW() * 10000, enemiesList.Count));
                        for (int x = 0; x < enemiesList.Count; x++)
                        {
                            IObject obj = enemiesList[x] as IObject;
                            if (obj.CurrentCopyMapID != monster.CurrentCopyMapID)
                            {
                                continue;
                            }

                            MagicAction.ProcessAction(monster, obj, key.MagicActionID2, magicHelperItem.MagicActionParams2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 扇形范围的圆形区域伤害
        /// </summary>
        /// <param name="key"></param>
        /// <param name="magicHelperItem"></param>
        /// <param name="nowTicks"></param>
        public void ExecuteMUFireSector(GridMagicHelperItemKey key, GridMagicHelperItemEx magicHelperItem, long nowTicks)
        {
            int id = (int)key.MagicActionID;
            int gridX = key.PosX;
            int gridY = key.PosY;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = nowTicks;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int addValue = (int)magicHelperItem.MagicActionParams[2];
            int attacker = (int)magicHelperItem.MagicActionParams[3];
            double baseRate = magicHelperItem.MagicActionParams[4];
            int radio = (int)(magicHelperItem.MagicActionParams[5]); //服务器坐标单位
            int angel = (int)(magicHelperItem.MagicActionParams[6]); //角度
            int direction = (int)(magicHelperItem.MagicActionParams[7]); //释放时的方向
            int mapGridWidth = (int)(magicHelperItem.MagicActionParams[9]); //格子宽度
            gridX *= mapGridWidth;
            gridY *= mapGridWidth;
            if (-1 != attacker)
            {
                GameClient client = GameManager.ClientMgr.FindClient(attacker);
                if (null != client)
                {
                    List<Object> enemiesList = new List<object>();
                    GameManager.ClientMgr.LookupEnemiesInCircleByAngle(direction, magicHelperItem.MapCode, client.ClientData.CopyMapID, gridX, gridY, radio, enemiesList, angel, false);
                    GameManager.MonsterMgr.LookupEnemiesInCircleByAngle(direction, magicHelperItem.MapCode, client.ClientData.CopyMapID, gridX, gridY, radio, enemiesList, angel, false);
                    for (int x = 0; x < enemiesList.Count; x++)
                    {
                        IObject obj = enemiesList[x] as IObject;
                        if (obj.CurrentCopyMapID != client.CurrentCopyMapID)
                        {
                            continue;
                        }

                        if (obj is GameClient) //被攻击者是角色
                        {
                            if ((obj as GameClient).ClientData.RoleID != attacker && Global.IsOpposition(client, (obj as GameClient)))
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as GameClient, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, false, baseRate, addValue);
                            }
                        }
                        else if (obj is Monster) //被攻击者是怪物
                        {
                            if (Global.IsOpposition(client, (obj as Monster)))
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as Monster, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                            }
                        }
                        else if (obj is BiaoCheItem) //被攻击者是镖车
                        {
                            if (Global.IsOpposition(client, (obj as BiaoCheItem)))
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as BiaoCheItem, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, baseRate, addValue);
                            }
                        }
                        else if (obj is JunQiItem) //被攻击者是帮旗
                        {
                            if (Global.IsOpposition(client, (obj as JunQiItem)))
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as JunQiItem, 0, 0, 1.0, 1, false, 0, 0, 0, 0, baseRate, addValue);
                            }
                        }
                        else if (obj is FakeRoleItem) //被攻击者是假人
                        {
                            if (Global.IsOpposition(client, (obj as FakeRoleItem)))
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as FakeRoleItem, 0, 0, 1.0, 1, false, 0, baseRate, addValue, 0);
                            }
                        }
                    }
                }
                else
                {
                    Monster monster = GameManager.MonsterMgr.FindMonster(magicHelperItem.MapCode, attacker);
                    if (null != monster)
                    {
                        List<Object> enemiesList = new List<object>();
                        GameManager.ClientMgr.LookupEnemiesInCircleByAngle(direction, magicHelperItem.MapCode, monster.CopyMapID, gridX, gridY, radio, enemiesList, angel, false);
                        GameManager.MonsterMgr.LookupEnemiesInCircleByAngle(direction, magicHelperItem.MapCode, monster.CopyMapID, gridX, gridY, radio, enemiesList, angel, false);
                        for (int x = 0; x < enemiesList.Count; x++)
                        {
                            IObject obj = enemiesList[x] as IObject;
                            if (obj.CurrentCopyMapID != monster.CurrentCopyMapID)
                            {
                                continue;
                            }

                            if (obj is GameClient) //被攻击者是角色
                            {
                                if ((obj as GameClient).ClientData.RoleID != attacker && Global.IsOpposition(monster, (obj as GameClient)))
                                {
                                    GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        monster, obj as GameClient, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                                }
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                if (Global.IsOpposition(monster, (obj as Monster)))
                                {
                                    GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        monster, obj as Monster, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 矩形范围的圆形区域伤害
        /// </summary>
        /// <param name="key"></param>
        /// <param name="magicHelperItem"></param>
        /// <param name="nowTicks"></param>
        public void ExecuteMUFireStraight(GridMagicHelperItemKey key, GridMagicHelperItemEx magicHelperItem, long nowTicks)
        {
            int id = (int)key.MagicActionID;
            int gridX = key.PosX;
            int gridY = key.PosY;

            magicHelperItem.ExecutedNum++;
            magicHelperItem.LastTicks = nowTicks;

            //执行伤害
            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            int addValue = (int)magicHelperItem.MagicActionParams[2];
            int attacker = (int)magicHelperItem.MagicActionParams[3];
            double baseRate = magicHelperItem.MagicActionParams[4];
            int radio = (int)(magicHelperItem.MagicActionParams[5]); //服务器坐标单位
            int width = (int)(magicHelperItem.MagicActionParams[6]); //宽度
            int deltaX = (int)(magicHelperItem.MagicActionParams[7]); //释放时的方向,X
            int deltaY = (int)(magicHelperItem.MagicActionParams[8]); //释放时的方向,Y
            int mapGridWidth = (int)(magicHelperItem.MagicActionParams[9]); //格子宽度
            gridX *= mapGridWidth;
            gridY *= mapGridWidth;
            if (-1 != attacker)
            {
                GameClient client = GameManager.ClientMgr.FindClient(attacker);
                if (null != client)
                {
                    List<Object> enemiesList = new List<object>();
                    GameManager.ClientMgr.LookupRolesInSquare(magicHelperItem.MapCode, client.ClientData.CopyMapID, gridX, gridY, 0, 0, radio, width, enemiesList);
                    GameManager.MonsterMgr.LookupRolesInSquare(magicHelperItem.MapCode, client.ClientData.CopyMapID, gridX, gridY, 0, 0, radio, width, enemiesList, (int)ObjectTypes.OT_MONSTER);
                    for (int x = 0; x < enemiesList.Count; x++)
                    {
                        IObject obj = enemiesList[x] as IObject;
                        if (obj.CurrentCopyMapID != client.CurrentCopyMapID)
                        {
                            continue;
                        }

                        if (obj is GameClient) //被攻击者是角色
                        {
                            if ((obj as GameClient).ClientData.RoleID != attacker && Global.IsOpposition(client, (obj as GameClient)))
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as GameClient, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, false, baseRate, addValue);
                            }
                        }
                        else if (obj is Monster) //被攻击者是怪物
                        {
                            if (Global.IsOpposition(client, (obj as Monster)))
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as Monster, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                            }
                        }
                        else if (obj is BiaoCheItem) //被攻击者是镖车
                        {
                            if (Global.IsOpposition(client, (obj as BiaoCheItem)))
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as BiaoCheItem, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, baseRate, addValue);
                            }
                        }
                        else if (obj is JunQiItem) //被攻击者是帮旗
                        {
                            if (Global.IsOpposition(client, (obj as JunQiItem)))
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as JunQiItem, 0, 0, 1.0, 1, false, 0, 0, 0, 0, baseRate, addValue);
                            }
                        }
                        else if (obj is FakeRoleItem) //被攻击者是假人
                        {
                            if (Global.IsOpposition(client, (obj as FakeRoleItem)))
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, obj as FakeRoleItem, 0, 0, 1.0, 1, false, 0, baseRate, addValue, 0);
                            }
                        }
                    }
                }
                else
                {
                    Monster monster = GameManager.MonsterMgr.FindMonster(magicHelperItem.MapCode, attacker);
                    if (null != monster)
                    {
                        List<Object> enemiesList = new List<object>();
                        GameManager.ClientMgr.LookupRolesInSquare(magicHelperItem.MapCode, monster.CopyMapID, gridX, gridY, 0, 0, radio, width, enemiesList);
                        GameManager.MonsterMgr.LookupRolesInSquare(magicHelperItem.MapCode, monster.CopyMapID, gridX, gridY, 0, 0, radio, width, enemiesList, (int)ObjectTypes.OT_MONSTER);
                        for (int x = 0; x < enemiesList.Count; x++)
                        {
                            IObject obj = enemiesList[x] as IObject;
                            if (obj.CurrentCopyMapID != monster.CurrentCopyMapID)
                            {
                                continue;
                            }

                            if (obj is GameClient) //被攻击者是角色
                            {
                                if ((obj as GameClient).ClientData.RoleID != attacker && Global.IsOpposition(monster, (obj as GameClient)))
                                {
                                    GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        monster, obj as GameClient, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                                }
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                if (Global.IsOpposition(monster, (obj as Monster)))
                                {
                                    GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        monster, obj as Monster, 0, 0, 1.0, 1, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false, baseRate, addValue);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
