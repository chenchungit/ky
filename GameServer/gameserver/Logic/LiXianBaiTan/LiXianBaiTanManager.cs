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

namespace GameServer.Logic.LiXianBaiTan
{
    /// <summary>
    /// 离线摆摊出售物品数据项
    /// </summary>
    public class LiXianSaleGoodsItem
    {
        /// <summary>
        /// 物品的数据库ID
        /// </summary>
        public int GoodsDbID = 0;

        /// <summary>
        /// 出售的物品的数据
        /// </summary>
        public GoodsData SalingGoodsData = null;

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
    }

    /// <summary>
    /// 离线摆摊出售角色数据项
    /// </summary>
    public class LiXianSaleRoleItem
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
        /// 离线摆摊的最长时间
        /// </summary>
        public int LiXianBaiTanMaxTicks = 0;

        /// <summary>
        /// 离线摆摊开始的时间
        /// </summary>
        public long StartTicks = 0L;

        /// <summary>
        /// 假角色ID
        /// </summary>
        public int FakeRoleID = 0;
    }

    /// <summary>
    /// 在原来的“交易市场”的基础上配合实现摆摊
    /// </summary>
    public class LiXianBaiTanManager : ScheduleTask, IManager
    {
        private TaskInternalLock _InternalLock = new TaskInternalLock();
        public TaskInternalLock InternalLock { get { return _InternalLock; } }

        #region 唯一的实例

        /// <summary>
        /// 唯一的静态化实例
        /// </summary>
        private static LiXianBaiTanManager _Instance = new LiXianBaiTanManager();

        /// <summary>
        /// 得到唯一的静态化实例
        /// </summary>
        /// <returns></returns>
        public static LiXianBaiTanManager getInstance()
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
        /// 线程驱动
        /// </summary>
        public void ProcessQueue()
        {
            long nowTicks = TimeUtil.NOW();
            List<LiXianSaleRoleItem> liXianSaleRoleItems;
            lock(_LiXianRoleInfoDict)
            {
                liXianSaleRoleItems = _LiXianRoleInfoDict.Values.ToList<LiXianSaleRoleItem>();
            }

            for (int i = 0; i < liXianSaleRoleItems.Count; i++)
            {
                //挂机结束
                if (nowTicks - liXianSaleRoleItems[i].StartTicks >= liXianSaleRoleItems[i].LiXianBaiTanMaxTicks)
                {
                    RemoveLiXianSaleGoodsItems(liXianSaleRoleItems[i].RoleID);

                    if (liXianSaleRoleItems[i].LiXianBaiTanMaxTicks >= (int)(Global.ConstLiXianBaiTanTicks)) //提示用户12小时摆摊时间到被终止
                    {

                    }
                    else //提示用户摆摊时间结束，终止
                    {

                    }
                }
            }
        }

        #endregion 线程驱动

        #region 待出售的摆摊物品项

        /// <summary>
        /// 保存正在出售的物品的词典
        /// </summary>
        private static Dictionary<int, LiXianSaleGoodsItem> _LiXianSaleGoodsDict = new Dictionary<int, LiXianSaleGoodsItem>();

        /// <summary>
        /// 保存正在出售的物品的角色的信息词典
        /// </summary>
        private static Dictionary<int, LiXianSaleRoleItem> _LiXianRoleInfoDict = new Dictionary<int, LiXianSaleRoleItem>();

        /// <summary>
        /// 添加出售的物品项
        /// </summary>
        /// <param name="LiXianSaleGoodsItem"></param>
        public static void AddLiXianSaleGoodsItem(LiXianSaleGoodsItem liXianSaleGoodsItem)
        {
            if (Global.Flag_MUSale) SaleManager.AddLiXianSaleGoodsItem(liXianSaleGoodsItem);
            lock (_LiXianSaleGoodsDict)
            {
                _LiXianSaleGoodsDict[liXianSaleGoodsItem.GoodsDbID] = liXianSaleGoodsItem;
            }
        }

        /// <summary>
        /// 将角色的所有出售的物品加入管理中
        /// </summary>
        /// <param name="dbRoleInfo"></param>
        public static void AddLiXianSaleGoodsItems(GameClient client, int fakeRoleID)
        {
            string userID = GameManager.OnlineUserSession.FindUserID(client.ClientSocket);
            List<GoodsData> goodsDataList = client.ClientData.SaleGoodsDataList;
            if (null != goodsDataList)
            {
                lock (goodsDataList)
                {
                    for (int i = 0; i < goodsDataList.Count; i++)
                    {
                        LiXianSaleGoodsItem liXianSaleGoodsItem = new LiXianSaleGoodsItem()
                        {
                            GoodsDbID = goodsDataList[i].Id,
                            SalingGoodsData = goodsDataList[i],
                            ZoneID = client.ClientData.ZoneID,
                            UserID = userID,
                            RoleID = client.ClientData.RoleID,
                            RoleName = client.ClientData.RoleName,
                            RoleLevel = client.ClientData.Level,
                        };

                        AddLiXianSaleGoodsItem(liXianSaleGoodsItem);
                    }
                }
            }

            int maxTicks;
            if (!Global.Flag_MUSale)
            {
                maxTicks = GameManager.ClientMgr.GetLiXianBaiTanTicksValue(client);
            }
            else
            {
                maxTicks = (int)SaleManager.MaxSaleGoodsTime;
            }
            maxTicks = (int)Math.Min(Global.ConstLiXianBaiTanTicks, maxTicks);
            GameManager.ClientMgr.ModifyLiXianBaiTanTicksValue(client, -maxTicks, true);

            lock (_LiXianRoleInfoDict)
            {
                _LiXianRoleInfoDict[client.ClientData.RoleID] = new LiXianSaleRoleItem()
                {
                    ZoneID = client.ClientData.ZoneID,
                    UserID = userID,
                    RoleID = client.ClientData.RoleID,
                    RoleName = client.ClientData.RoleName,
                    RoleLevel = client.ClientData.Level,
                    CurrentGrid = client.CurrentGrid,
                    LiXianBaiTanMaxTicks = maxTicks,
                    StartTicks = TimeUtil.NOW(),
                    FakeRoleID = fakeRoleID,
                };
            }
        }

        /// <summary>
        /// 删除出售的物品项
        /// </summary>
        /// <param name="LiXianSaleGoodsItem"></param>
        public static LiXianSaleGoodsItem RemoveLiXianSaleGoodsItem(int goodsDbID)
        {
            if (Global.Flag_MUSale) SaleManager.RemoveSaleGoodsItem(goodsDbID);
            lock (_LiXianSaleGoodsDict)
            {
                LiXianSaleGoodsItem liXianSaleGoodsItem = null;
                if (_LiXianSaleGoodsDict.TryGetValue(goodsDbID, out liXianSaleGoodsItem))
                {
                    _LiXianSaleGoodsDict.Remove(goodsDbID);
                }

                return liXianSaleGoodsItem;
            }
        }

        /// <summary>
        /// 删除角色的所有出售的物品项
        /// </summary>
        /// <param name="LiXianSaleGoodsItem"></param>
        public static void RemoveLiXianSaleGoodsItems(GameClient client)
        {
            RemoveLiXianSaleGoodsItems(client.ClientData.RoleID);
        }

        /// <summary>
        /// 删除角色的所有出售的物品项
        /// </summary>
        /// <param name="LiXianSaleGoodsItem"></param>
        public static void RemoveLiXianSaleGoodsItems(int roleID)
        {
            lock (_LiXianSaleGoodsDict)
            {
                List<LiXianSaleGoodsItem> liXianSaleGoodsItemList = new List<LiXianSaleGoodsItem>();
                foreach (var liXianSaleGoodsItem in _LiXianSaleGoodsDict.Values)
                {
                    if (liXianSaleGoodsItem.RoleID == roleID)
                    {
                        liXianSaleGoodsItemList.Add(liXianSaleGoodsItem);
                    }
                }

                for (int i = 0; i < liXianSaleGoodsItemList.Count; i++)
                {
                    _LiXianSaleGoodsDict.Remove(liXianSaleGoodsItemList[i].GoodsDbID);
                    if (Global.Flag_MUSale) SaleManager.RemoveSaleGoodsItem(liXianSaleGoodsItemList[i].GoodsDbID);
                }
            }

            lock (_LiXianRoleInfoDict)
            {
                _LiXianRoleInfoDict.Remove(roleID);
            }
        }

        /// <summary>
        /// 重获得回剩余的离线摆摊的时间
        /// </summary>
        /// <param name="LiXianSaleGoodsItem"></param>
        public static void GetBackLiXianSaleLeftTicks(GameClient client)
        {
            LiXianSaleRoleItem liXianSaleRoleItem = null;
            lock (_LiXianRoleInfoDict)
            {
                if (!_LiXianRoleInfoDict.TryGetValue(client.ClientData.RoleID, out liXianSaleRoleItem))
                {
                    return; 
                }

                long nowTicks = TimeUtil.NOW();
                long leftTicks = nowTicks - liXianSaleRoleItem.StartTicks;
                if (leftTicks < liXianSaleRoleItem.LiXianBaiTanMaxTicks)
                {
                    leftTicks = Math.Max(0, liXianSaleRoleItem.LiXianBaiTanMaxTicks - leftTicks);
                    GameManager.ClientMgr.ModifyLiXianBaiTanTicksValue(client, (int)leftTicks, true);
                }
            }
        }

        /// <summary>
        /// 获取角色的所有出售的物品项
        /// </summary>
        /// <param name="LiXianSaleGoodsItem"></param>
        public static List<GoodsData> GetLiXianSaleGoodsList(int roleID)
        {
            List<GoodsData> saleGoodsDataList = new List<GoodsData>();
            lock (_LiXianSaleGoodsDict)
            {
                List<LiXianSaleGoodsItem> liXianSaleGoodsItemList = new List<LiXianSaleGoodsItem>();
                foreach (var liXianSaleGoodsItem in _LiXianSaleGoodsDict.Values)
                {
                    if (liXianSaleGoodsItem.RoleID == roleID)
                    {
                        saleGoodsDataList.Add(liXianSaleGoodsItem.SalingGoodsData);
                    }
                }
            }

            return saleGoodsDataList;
        }

        /// <summary>
        /// 根据角色信息，删除假人数据
        /// </summary>
        /// <param name="client"></param>
        public static void DelFakeRoleByClient(GameClient client)
        {
            int fakeRoleID = -1;
            LiXianSaleRoleItem liXianSaleRoleItem = null;
            lock (_LiXianRoleInfoDict)
            {
                if (!_LiXianRoleInfoDict.TryGetValue(client.ClientData.RoleID, out liXianSaleRoleItem))
                {
                    return;
                }

                fakeRoleID = liXianSaleRoleItem.FakeRoleID;
            }

            if (fakeRoleID > 0)
            {
                FakeRoleManager.ProcessDelFakeRole(fakeRoleID);
            }
        }

        /// <summary>
        /// 离线摆摊位置处的角色
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static int GetLiXianRoleCountByPoint(Point grid)
        {
            int roleCount = 0;
            lock (_LiXianRoleInfoDict)
            {
                foreach (var liXianSaleRoleItem in _LiXianRoleInfoDict.Values)
                {
                    if (liXianSaleRoleItem.CurrentGrid.X == grid.X && liXianSaleRoleItem.CurrentGrid.Y == grid.Y)
                    {
                        roleCount++;
                    }
                }
            }

            return roleCount;
        }

        #endregion 待出售的摆摊物品项
    }
}
