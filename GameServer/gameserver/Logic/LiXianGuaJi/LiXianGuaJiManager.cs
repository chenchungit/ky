#define UseTimer
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;
using GameServer.Server;
using Server.Protocol;
using Server.Data;
using GameServer.Logic;
using GameServer.Core.GameEvent;
using System.Windows;

namespace GameServer.Logic.LiXianGuaJi
{
    /// <summary>
    /// 离线挂机角色数据项
    /// </summary>
    public class LiXianGuaJiRoleItem
    {
        /// <summary>
        /// 角色区号
        /// </summary>
        public int ZoneID = 0;

        /// <summary>
        /// 出售者的用户ID
        /// </summary>
        public string UserID = "";

        /// <summary>
        /// 出售者的角色ID
        /// </summary>
        public int RoleID = 0;

        /// <summary>
        /// 出售者的角色名称
        /// </summary>
        public string RoleName = "";

        /// <summary>
        /// 角色级别
        /// </summary>
        public int RoleLevel = 0;

        /// <summary>
        /// 当前位置（格子）
        /// </summary>
        public Point CurrentGrid;

        /// <summary>
        /// 离线挂机开始的时间
        /// </summary>
        public long StartTicks = 0L;

        /// <summary>
        /// 假角色ID
        /// </summary>
        public int FakeRoleID = 0;

        /// <summary>
        /// 安全区冥想时间
        /// </summary>
        public int MeditateTime = 0;

        /// <summary>
        /// 非安全区冥想时间
        /// </summary>
        public int NotSafeMeditateTime = 0;

        /// <summary>
        /// 冥想时间计时
        /// </summary>
        public long MeditateTicks = TimeUtil.NOW();

        /// <summary>
        ///  本次闭关的开始时间
        /// </summary>
        public long BiGuanTime = TimeUtil.NOW();

        /// <summary>
        /// 地图编号
        /// </summary>
        public int MapCode = 0;
    }

    /// <summary>
    /// 配合冥想实现离线挂机
    /// </summary>
    public class LiXianGuaJiManager : ScheduleTask, IManager
    {
        private TaskInternalLock _InternalLock = new TaskInternalLock();
        public TaskInternalLock InternalLock { get { return _InternalLock; } }

        public const int MaxMingXiangTicks = 12 * 60 * 60 * 1000;

        #region 唯一的实例

        /// <summary>
        /// 唯一的静态化实例
        /// </summary>
        private static LiXianGuaJiManager _Instance = new LiXianGuaJiManager();

        /// <summary>
        /// 得到唯一的静态化实例
        /// </summary>
        /// <returns></returns>
        public static LiXianGuaJiManager getInstance()
        {
            return _Instance;
        }

        #endregion 唯一的实例

        #region IManager接口实现

#if !UseTimer
        //任务调度器
        private ScheduleExecutor _ScheduleExecutor = null;

        /// <summary>
        /// 周期调度的处理
        /// </summary>
        private PeriodicTaskHandle _PeriodicTaskHandle = null;
#endif

        public bool initialize()
        {
#if !UseTimer
            //分配1个线程
            _ScheduleExecutor = new ScheduleExecutor(1);

            //加入循环任务
            _PeriodicTaskHandle = _ScheduleExecutor.scheduleExecute(this, 0L, 100);
#else
            ScheduleExecutor2.Instance.scheduleExecute(this, 0, 30000);
#endif

            return true;
        }

        public bool startup()
        {
#if !UseTimer
            _ScheduleExecutor.start();
#endif
            return true;
        }

        public bool showdown()
        {
#if !UseTimer
            _PeriodicTaskHandle.cannel();
            _ScheduleExecutor.stop();
#else
            ScheduleExecutor2.Instance.scheduleCancle(this);
#endif
            SaveGuaJiTimeForAll();
            return true;
        }

        public bool destroy()
        {
#if !UseTimer
            _ScheduleExecutor = null;
#endif
            return true;
        }

        #endregion IManager接口实现

        #region ScheduleTask接口实现

        /// <summary>
        /// 定时调度
        /// </summary>
        public void run()
        {
            ProcessQueue();
        }

        #endregion ScheduleTask接口实现

        #region 线程驱动

        /// <summary>
        /// 安全获取所有离线挂机角色信息的列表
        /// </summary>
        /// <returns></returns>
        public static List<LiXianGuaJiRoleItem> GetLiXianGuaJiRoleItemList()
        {
            List<LiXianGuaJiRoleItem> LiXianGuaJiRoleItems;
            lock (_LiXianRoleInfoDict)
            {
                LiXianGuaJiRoleItems = _LiXianRoleInfoDict.Values.ToList<LiXianGuaJiRoleItem>();
            }

            return LiXianGuaJiRoleItems;
        }

        /// <summary>
        /// 线程驱动
        /// </summary>
        public void ProcessQueue()
        {
            long nowTicks = TimeUtil.NOW();
            List<LiXianGuaJiRoleItem> LiXianGuaJiRoleItems = GetLiXianGuaJiRoleItemList();
            for (int i = 0; i < LiXianGuaJiRoleItems.Count; i++)
            {
                //先计时,再检测是否打到时限
                DoSpriteMeditateTime(LiXianGuaJiRoleItems[i]);

                //挂机结束
                if (/*nowTicks - LiXianGuaJiRoleItems[i].StartTicks >= MaxMingXiangTicks || */(LiXianGuaJiRoleItems[i].MeditateTime + LiXianGuaJiRoleItems[i].NotSafeMeditateTime) >= MaxMingXiangTicks)
                {
                    SaveDBLiXianGuaJiTimeForRole(LiXianGuaJiRoleItems[i]); //保存挂机时间
                    RemoveLiXianGuaJiRole(LiXianGuaJiRoleItems[i].RoleID); //从列表移除

                    //移除假人
                    if (LiXianGuaJiRoleItems[i].FakeRoleID > 0)
                    {
                        FakeRoleManager.ProcessDelFakeRole(LiXianGuaJiRoleItems[i].FakeRoleID);
                    }
                    //提示用户离线挂机终止

                    continue;
                }
            }
        }

        /// <summary>
        // 处理冥想计时 [3/18/2014 LiaoWei]
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void DoSpriteMeditateTime(LiXianGuaJiRoleItem c)
        {
            long lTicks = 0;
            long lCurrticks = TimeUtil.NOW();

            lTicks = lCurrticks - c.MeditateTicks;

            // 每分钟计时一次
            if (lTicks < (60 * 1000))
            {
                return;
            }

            c.MeditateTicks = lCurrticks;

            // 判断是否在安全区中
            bool bIsInsafeArea = false;

            Point currentGrid = c.CurrentGrid;

            GameMap gameMap = null;
            if (GameManager.MapMgr.DictMaps.TryGetValue(c.MapCode, out gameMap))
                bIsInsafeArea = gameMap.InSafeRegionList(currentGrid);

            if (bIsInsafeArea)
            {
                int nTime = c.MeditateTime;
                int nTime2 = c.NotSafeMeditateTime;
                if ((nTime + nTime2) < MaxMingXiangTicks)
                {
                    long msecs = Math.Max(lCurrticks - c.BiGuanTime, 0);
                    msecs = Math.Min(msecs + nTime, MaxMingXiangTicks - nTime2);   // 12个小时

                    c.MeditateTime = (int)msecs;
                }
            }
            else
            {
                int nTime = c.MeditateTime;
                int nTime2 = c.NotSafeMeditateTime;

                if ((nTime + nTime2) < MaxMingXiangTicks)
                {
                    long msecs = Math.Max(lCurrticks - c.BiGuanTime, 0);
                    msecs = Math.Min(msecs + nTime2, MaxMingXiangTicks - nTime);   // 12个小时

                    c.NotSafeMeditateTime = (int)msecs;
                }
            }

            // 重置时间
            c.BiGuanTime = lCurrticks;

            return;
        }

        #endregion 线程驱动

        #region 离线挂机的角色项

        /// <summary>
        /// 保存正在离线挂机的角色的信息词典
        /// </summary>
        private static Dictionary<int, LiXianGuaJiRoleItem> _LiXianRoleInfoDict = new Dictionary<int, LiXianGuaJiRoleItem>();

        /// <summary>
        /// 将角色的所有离线挂机加入管理中
        /// </summary>
        /// <param name="dbRoleInfo"></param>
        public static void AddLiXianGuaJiRole(GameClient client, int fakeRoleID)
        {
            string userID = GameManager.OnlineUserSession.FindUserID(client.ClientSocket);

            lock (_LiXianRoleInfoDict)
            {
                _LiXianRoleInfoDict[client.ClientData.RoleID] = new LiXianGuaJiRoleItem()
                {
                    ZoneID = client.ClientData.ZoneID,
                    UserID = userID,
                    RoleID = client.ClientData.RoleID,
                    RoleName = client.ClientData.RoleName,
                    RoleLevel = client.ClientData.Level,
                    CurrentGrid = client.CurrentGrid,
                    StartTicks = TimeUtil.NOW(),
                    FakeRoleID = fakeRoleID,
                    MeditateTime = client.ClientData.MeditateTime,
                    NotSafeMeditateTime = client.ClientData.NotSafeMeditateTime,
                    MapCode = client.ClientData.MapCode,
                };
            }
        }

        /// <summary>
        /// 删除角色的所有离线挂机项
        /// </summary>
        /// <param name="LiXianSaleGoodsItem"></param>
        public static void RemoveLiXianGuaJiRole(GameClient client)
        {
            RemoveLiXianGuaJiRole(client.ClientData.RoleID);
        }

        /// <summary>
        /// 删除角色的所有离线挂机项
        /// </summary>
        /// <param name="LiXianSaleGoodsItem"></param>
        public static void RemoveLiXianGuaJiRole(int roleID)
        {
            lock (_LiXianRoleInfoDict)
            {
                _LiXianRoleInfoDict.Remove(roleID);
            }
        }

        /// <summary>
        /// 得回离线的时间
        /// </summary>
        /// <param name="LiXianSaleGoodsItem"></param>
        public static void GetBackLiXianGuaJiTime(GameClient client)
        {
            LiXianGuaJiRoleItem liXianGuaJiRoleItem = null;
            lock (_LiXianRoleInfoDict)
            {
                if (!_LiXianRoleInfoDict.TryGetValue(client.ClientData.RoleID, out liXianGuaJiRoleItem))
                {
                    return;
                }

                client.ClientData.MeditateTime = liXianGuaJiRoleItem.MeditateTime;
                client.ClientData.NotSafeMeditateTime = liXianGuaJiRoleItem.NotSafeMeditateTime;

                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.MeditateTime, client.ClientData.MeditateTime, true);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.NotSafeMeditateTime, client.ClientData.NotSafeMeditateTime, true);
            }
        }

        /// <summary>
        /// 根据角色信息，删除假人数据
        /// </summary>
        /// <param name="client"></param>
        public static bool DelFakeRoleByClient(GameClient client)
        {
            int fakeRoleID = -1;
            LiXianGuaJiRoleItem liXianGuaJiRoleItem = null;
            lock (_LiXianRoleInfoDict)
            {
                if (!_LiXianRoleInfoDict.TryGetValue(client.ClientData.RoleID, out liXianGuaJiRoleItem))
                {
                    return false;
                }

                fakeRoleID = liXianGuaJiRoleItem.FakeRoleID;
            }

            if (fakeRoleID > 0)
            {
                FakeRoleManager.ProcessDelFakeRole(fakeRoleID);
            }

            return true;
        }

        /// <summary>
        /// 为所有离线挂机角色保存挂机时间信息
        /// </summary>
        public static void SaveGuaJiTimeForAll()
        {
            long nowTicks = TimeUtil.NOW();
            List<LiXianGuaJiRoleItem> LiXianGuaJiRoleItems = GetLiXianGuaJiRoleItemList();
            for (int i = 0; i < LiXianGuaJiRoleItems.Count; i++)
            {
                SaveDBLiXianGuaJiTimeForRole(LiXianGuaJiRoleItems[i]);
            }
        }

        /// <summary>
        /// 为单个离线挂机角色保存挂机时间信息
        /// </summary>
        /// <param name="liXianGuaJiRoleItem"></param>
        public static void SaveDBLiXianGuaJiTimeForRole(LiXianGuaJiRoleItem liXianGuaJiRoleItem)
        {
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEROLEPARAM,
                string.Format("{0}:{1}:{2}", liXianGuaJiRoleItem.RoleID, RoleParamName.MeditateTime, liXianGuaJiRoleItem.MeditateTime), null, GameManager.LocalServerIdForNotImplement);
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEROLEPARAM,
                string.Format("{0}:{1}:{2}", liXianGuaJiRoleItem.RoleID, RoleParamName.NotSafeMeditateTime, liXianGuaJiRoleItem.NotSafeMeditateTime), null, GameManager.LocalServerIdForNotImplement);
        }

        #endregion 离线挂机的角色项
    }
}
