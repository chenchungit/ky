#define ___CC___FUCK___YOU___BB___


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
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic.ActivityNew.SevenDay;
using GameServer.Core.GameEvent;

namespace GameServer.Logic
{
    /// <summary>
    /// 物品掉落配置缓存项
    /// </summary>
    public class FallGoodsItem
    {
        /// <summary>
        /// 物品ID
        /// </summary>
        public int GoodsID
        {
            get;
            set;
        }

        /// <summary>
        /// 物品掉落概率的开始值(0~100)整数
        /// </summary>
        public int BasePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 物品掉落概率(0~100)整数
        /// </summary>
        public int SelfPercent
        {
            get;
            set;
        }

        /// <summary>
        /// 物品掉落品质ID
        /// </summary>
        //public int FallQualityID
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 是否是绑定物品
        /// </summary>
        public int Binding
        {
            get;
            set;
        }

        /// <summary>
        /// 物品掉落的锻造级别
        /// </summary>
        public int FallLevelID
        {
            get;
            set;
        }

        /// <summary>
        /// 物品掉落的天生ID
        /// </summary>
        //public int FallBornIndexID
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 是否是好物品
        /// </summary>
        public bool IsGood
        {
            get;
            set;
        }

        /// <summary>
        /// 物品掉落幸运触发概率
        /// </summary>
        public int LuckyRate
        {
            get;
            set;
        }

        /// <summary>
        /// 物品掉落追加流水ID
        /// </summary>
        public int ZhuiJiaID
        {
            get;
            set;
        }

        /// <summary>
        /// 卓越属性激活ID
        /// </summary>
        public int ExcellencePropertyID
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 物品掉落品质缓存项
    /// </summary>
    public class FallQualityItem
    {
        /// <summary>
        /// 流水ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// 不同品质掉落概率的开始值
        /// </summary>
        public double[] QualityBasePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 不同品质掉落概率
        /// </summary>
        public double[] QualitySelfPercent
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 物品掉落锻造级别缓存项
    /// </summary>
    public class FallLevelItem
    {
        /// <summary>
        /// 流水ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// 不同级别掉落概率的开始值
        /// </summary>
        public double[] LevelBasePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 不同级别掉落概率
        /// </summary>
        public double[] LevelSelfPercent
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 物品掉落追加级别缓存项
    /// </summary>
    public class ZhuiJiaIDItem
    {
        /// <summary>
        /// 流水ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// 不同级别追加概率的开始值
        /// </summary>
        public double[] LevelBasePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 不同级别追加概率
        /// </summary>
        public double[] LevelSelfPercent
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 物品掉落卓越属性
    /// </summary>
    public class ExcellencePropertyItem
    {
        /// <summary>
        /// 卓越属性条数
        /// </summary>
        public int Num
        {
            get;
            set;
        }

        /// <summary>
        /// 概率的开始值(0~100)整数
        /// </summary>
        public int BasePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 概率(0~100)整数
        /// </summary>
        public int SelfPercent
        {
            get;
            set;
        }

        /*/// <summary>
        /// 概率值
        /// </summary>
        public double Percent
        {
            get;
            set;
        }*/
    }

    /// <summary>
    /// 物品掉落卓越属性组项
    /// </summary>
    public class ExcellencePropertyGroupItem
    {
        /// <summary>
        /// 流水ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// 卓越属性的ID集合
        /// </summary>
        public int[] Max
        {
            get;
            set;
        }

        /// <summary>
        /// 卓越属性项数组
        /// </summary>
        public ExcellencePropertyItem[] ExcellencePropertyItems
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 物品掉落天生级别缓存项
    /// </summary>
    public class FallBornIndexItem
    {
        /// <summary>
        /// 流水ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// 不同级别掉落概率的开始值
        /// </summary>
        public double[] LevelBasePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 不同级别掉落概率
        /// </summary>
        public double[] LevelSelfPercent
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 物品掉落项
    /// </summary>
    public class GoodsPackItem : IObject
    {
        public GoodsPackItem()
        {
        }

        /// <summary>
        /// 物品掉落流水ID
        /// </summary>
        public int AutoID
        {
            get;
            set;
        }

        /// <summary>
        /// 物品掉落包索引ID
        /// </summary>
        public int GoodsPackID
        {
            get;
            set;
        }

        /// <summary>
        /// 物品原始拥有者ID
        /// </summary>
        public int OwnerRoleID
        {
            get;
            set;
        }

        /// <summary>
        /// 物品原始拥有者的名称
        /// </summary>
        public string OwnerRoleName
        {
            get;
            set;
        }

        /// <summary>
        /// 掉落的包裹的类型(0: 怪物掉落, 1: 其他角色的物品掉落)
        /// </summary>
        public int GoodsPackType
        {
            get;
            set;
        }

        /// <summary>
        /// 物品掉落时间
        /// </summary>
        public long ProduceTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 当前锁定的角色ID
        /// </summary>
        public int LockedRoleID
        {
            get;
            set;
        }

        /// <summary>
        /// 组队的成员列表diaol
        /// </summary>
        public List<int> TeamRoleIDs = null;

        /// <summary>
        /// 队伍的ID
        /// </summary>
        public int TeamID = -1;

        /// <summary>
        /// 记录已经获取的物品的索引
        /// </summary>
        private Dictionary<int, bool> _GoodsIDDict = new Dictionary<int, bool>();

        /// <summary>
        /// 记录已经获取的物品的索引属性
        /// </summary>
        public Dictionary<int, bool> GoodsIDDict
        {
            get { return _GoodsIDDict; }
        }

        /// <summary>
        /// 记录已经获取的物品的所有者属性
        /// </summary>
        private Dictionary<int, int> _GoodsIDToRolesDict = new Dictionary<int, int>();

        /// <summary>
        /// 记录已经获取的物品的所有者属性
        /// </summary>
        public Dictionary<int, int> GoodsIDToRolesDict
        {
            get { return _GoodsIDToRolesDict; }
        }

        /// <summary>
        /// 打开包裹的时间
        /// </summary>
        public long OpenPackTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 记录打开关闭包裹的时间
        /// </summary>
        private Dictionary<int, long> _RolesTicksDict = new Dictionary<int, long>();

        /// <summary>
        /// 记录打开关闭包裹的累加时间
        /// </summary>
        public Dictionary<int, long> RolesTicksDict
        {
            get { return _RolesTicksDict; }
        }

        /// <summary>
        /// 物品列表
        /// </summary>
        public List<GoodsData> GoodsDataList = null;

        /// <summary>
        /// 掉落的地图编号
        /// </summary>
        public int MapCode = -1;

        /// <summary>
        /// 掉落位置
        /// </summary>
        public Point FallPoint;

        /// <summary>
        /// 副本地图ID
        /// </summary>
        public int CopyMapID = -1;

        /// <summary>
        /// 杀掉的怪物名称
        /// </summary>
        public string KilledMonsterName = "";

        /// <summary>
        /// 是否属于拥有者
        /// </summary>
        public int BelongTo = -1;

        /// <summary>
        /// 掉落的怪物或者角色级别
        /// </summary>
        public int FallLevel = 0;

        #region 实现IObject接口方法

        /// <summary>
        /// 对象的类型
        /// </summary>
        public ObjectTypes ObjectType
        {
            get { return ObjectTypes.OT_GOODSPACK; }
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
                return new Point((int)(FallPoint.X / gameMap.MapGridWidth), (int)(FallPoint.Y / gameMap.MapGridHeight));
            }

            set
            {
                GameMap gameMap = GameManager.MapMgr.DictMaps[this.MapCode];
                this.FallPoint = new Point((int)(value.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2), (int)(value.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2));
            }
        }

        /// <summary>
        /// 当前所在的像素坐标
        /// </summary>
        public Point CurrentPos
        {
            get
            {
                return this.FallPoint;
            }

            set
            {
                this.FallPoint = value;
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

    /// <summary>
    /// 大乱斗存活角色的概率项
    /// </summary>
    public class BattleRoleItem
    {
        /// <summary>
        /// 角色
        /// </summary>
        public GameClient Client
        {
            get;
            set;
        }

        /// <summary>
        /// 获取概率
        /// </summary>
        public double Percent
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 物品掉落管理
    /// </summary>
    public class GoodsPackManager
    {
        #region 掉落包流水ID

        /// <summary>
        /// 基础的掉落物品ID
        /// </summary>
        private long BaseAutoID = 0;

        /// <summary>
        /// 获取下一个掉落的物品ID
        /// </summary>
        /// <returns></returns>
        public int GetNextAutoID()
        {
            return (int)(Interlocked.Increment(ref BaseAutoID) & 0x7fffffff);
        }

        #endregion 物品流水ID

        #region 掉落包中的物品流水ID

        /// <summary>
        /// 基础的掉落物品ID
        /// </summary>
        private long BaseGoodsID = 0;

        /// <summary>
        /// 获取下一个掉落的物品ID
        /// </summary>
        /// <returns></returns>
        public int GetNextGoodsID()
        {
            return (int)(Interlocked.Increment(ref BaseGoodsID) & 0x7fffffff);
        }

        #endregion 掉落包中的物品流水ID

        #region 角色的掉落包ID

        /// <summary>
        /// 角色的掉落包ID
        /// </summary>
        private long BaseRoleGoodsPackID = 0;

        /// <summary>
        /// 获取下一个角色的掉落包ID
        /// </summary>
        /// <returns></returns>
        public int GetNextRoleGoodsPackID()
        {
            return (int)(Interlocked.Increment(ref BaseRoleGoodsPackID) & 0x7fffffff);
        }

        #endregion 角色的掉落包ID

        #region Xml配置项加速缓存

        /// <summary>
        /// 物品掉落配置缓存项
        /// </summary>
        private Dictionary<int, List<FallGoodsItem>> _FallGoodsItemsDict = new Dictionary<int, List<FallGoodsItem>>();

        /// <summary>
        /// 根据物品掉落包ID来获取配置信息
        /// </summary>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        private List<FallGoodsItem> GetNormalFallGoodsItem(int goodsPackID)
        {
            List<FallGoodsItem> fallGoodsItemList = null;
            lock (_FallGoodsItemsDict)
            {
                _FallGoodsItemsDict.TryGetValue(goodsPackID, out fallGoodsItemList);
            }

            if (null != fallGoodsItemList)
            {
                return fallGoodsItemList;
            }

            SystemXmlItem monsterGoodsItem = null;
            if (!GameManager.SystemMonsterGoodsList.SystemXmlItemDict.TryGetValue(goodsPackID, out monsterGoodsItem))
            {
                return null;
            }

            FallGoodsItem fallGoodsItem = null;
            fallGoodsItemList = new List<FallGoodsItem>();

            string goodsData = monsterGoodsItem.GetStringValue("GoodsID");
            string[] goodsFields = goodsData.Split('|');
            int basePercent = 0;
            for (int i = 0; i < goodsFields.Length; i++)
            {
                string item = goodsFields[i].Trim();
                if (item == "") continue;

                string[] itemFields = item.Split(',');
                if (itemFields.Length != 7)
                {
                    continue;
                }

                fallGoodsItem = null;

                try
                {
                    fallGoodsItem = new FallGoodsItem()
                    {
                        GoodsID = Convert.ToInt32(itemFields[0]),
                        BasePercent = basePercent,
                        SelfPercent = (int)(Convert.ToDouble(itemFields[1]) * 100000),
                        Binding = Convert.ToInt32(itemFields[2]),
                        LuckyRate = (int)(Convert.ToDouble(itemFields[3])),
                        FallLevelID = Convert.ToInt32(itemFields[4]),
                        ZhuiJiaID = Convert.ToInt32(itemFields[5]),
                        ExcellencePropertyID = Convert.ToInt32(itemFields[6]),
                    };

                    basePercent += fallGoodsItem.SelfPercent;
                }
                catch (Exception)
                {
                    fallGoodsItem = null;
                }

                if (null == fallGoodsItem)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("解析掉落项时发生错误, GoodsPackID={0}, GoodsID={1}", goodsPackID, item));
                    continue;
                }

                fallGoodsItemList.Add(fallGoodsItem);
            }

            if (basePercent > 100000)
            {
                //并非所有配置项都要应该是小于100000,所以降低日志类型等级
                LogManager.WriteLog(LogTypes.Info, string.Format("解析掉落项时发生概率溢出100000错误, GoodsPackID={0}", goodsPackID));
            }

            lock (_FallGoodsItemsDict)
            {
                _FallGoodsItemsDict[goodsPackID] = fallGoodsItemList;
            }

            return fallGoodsItemList;
        }

        /// <summary>
        /// 限制时间物品掉落配置缓存项
        /// </summary>
        private Dictionary<int, List<FallGoodsItem>> _LimitTimeFallGoodsItemsDict = new Dictionary<int, List<FallGoodsItem>>();

        /// <summary>
        /// 限制时间掉落的开始时间
        /// </summary>
        private DateTime _LimitTimeStartDayTime = new DateTime(2000, 1, 1);

        /// <summary>
        /// 限制时间掉落的结束时间
        /// </summary>
        private DateTime _LimitTimeEndDayTime = new DateTime(2000, 1, 1);

        /// <summary>
        /// 充值限制掉落的时间项
        /// </summary>
        public void ResetLimitTimeRange()
        {
            _LimitTimeStartDayTime = new DateTime(2000, 1, 1);
            _LimitTimeEndDayTime = new DateTime(2000, 1, 1);
        }

        /// <summary>
        /// 判断当前是否在限制时间掉落
        /// </summary>
        /// <returns></returns>
        private bool JugeInLimitTimeRange()
        {
            if (2000 == _LimitTimeStartDayTime.Year)
            {
                _LimitTimeStartDayTime = Global.GetJieriStartDay();
            }

            if (2000 == _LimitTimeEndDayTime.Year)
            {
                _LimitTimeEndDayTime = Global.GetAddDaysDataTime(Global.GetJieriStartDay(), Math.Max(0, (int)GameManager.systemParamsList.GetParamValueIntByName("LimitTimeFallDays")));
            }

            DateTime today = TimeUtil.NowDateTime();
            if (today.Ticks >= _LimitTimeStartDayTime.Ticks && today.Ticks < _LimitTimeEndDayTime.Ticks)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据物品掉落包ID来获取配置信息
        /// </summary>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        private List<FallGoodsItem> GetLimitTimeFallGoodsItem(int goodsPackID)
        {
            if (!JugeInLimitTimeRange())
            {
                return null;
            }

            List<FallGoodsItem> fallGoodsItemList = null;
            lock (_LimitTimeFallGoodsItemsDict)
            {
                _LimitTimeFallGoodsItemsDict.TryGetValue(goodsPackID, out fallGoodsItemList);
            }

            if (null != fallGoodsItemList)
            {
                return fallGoodsItemList;
            }

            SystemXmlItem monsterGoodsItem = null;
            if (!GameManager.SystemLimitTimeMonsterGoodsList.SystemXmlItemDict.TryGetValue(goodsPackID, out monsterGoodsItem))
            {
                return null;
            }

            FallGoodsItem fallGoodsItem = null;
            fallGoodsItemList = new List<FallGoodsItem>();

            string goodsData = monsterGoodsItem.GetStringValue("GoodsID");
            string[] goodsFields = goodsData.Split('|');
            int basePercent = 0;
            for (int i = 0; i < goodsFields.Length; i++)
            {
                string item = goodsFields[i].Trim();
                if (item == "") continue;

                string[] itemFields = item.Split(',');
                if (itemFields.Length != 7)
                {
                    continue;
                }

                fallGoodsItem = null;

                try
                {
                    fallGoodsItem = new FallGoodsItem()
                    {
                        GoodsID = Convert.ToInt32(itemFields[0]),
                        BasePercent = basePercent,
                        SelfPercent = (int)(Convert.ToDouble(itemFields[1]) * 100000),
                        Binding = Convert.ToInt32(itemFields[2]),
                        LuckyRate = (int)(Convert.ToDouble(itemFields[3])),
                        FallLevelID = Convert.ToInt32(itemFields[4]),
                        ZhuiJiaID = Convert.ToInt32(itemFields[5]),
                        ExcellencePropertyID = Convert.ToInt32(itemFields[6]),
                    };

                    basePercent += fallGoodsItem.SelfPercent;
                }
                catch (Exception)
                {
                    fallGoodsItem = null;
                }

                if (null == fallGoodsItem)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("解析节日掉落项时发生错误, GoodsPackID={0}, GoodsID={1}", goodsPackID, item));
                    continue;
                }

                fallGoodsItemList.Add(fallGoodsItem);
            }

            if (basePercent > 100000)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析节日掉落项时发生概率溢出100000错误, GoodsPackID={0}", goodsPackID));
            }

            lock (_LimitTimeFallGoodsItemsDict)
            {
                _LimitTimeFallGoodsItemsDict[goodsPackID] = fallGoodsItemList;
            }

            return fallGoodsItemList;
        }

        /// <summary>
        /// 物品掉落配置缓存项
        /// </summary>
        private Dictionary<int, List<GoodsData>> _FixedGoodsItemsDict = new Dictionary<int, List<GoodsData>>();

        /// <summary>
        /// 根据物品掉落包ID来获取配置信息
        /// </summary>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        private List<GoodsData> GetFixedGoodsDataList(int goodsPackID)
        {
            List<GoodsData> fixedGoodsDataList = null;
            lock (_FixedGoodsItemsDict)
            {
                _FixedGoodsItemsDict.TryGetValue(goodsPackID, out fixedGoodsDataList);
            }

            if (null != fixedGoodsDataList)
            {
                return fixedGoodsDataList;
            }

            SystemXmlItem monsterGoodsItem = null;
            if (!GameManager.SystemMonsterGoodsList.SystemXmlItemDict.TryGetValue(goodsPackID, out monsterGoodsItem))
            {
                return null;
            }

            fixedGoodsDataList = new List<GoodsData>();

            string fixedaward = monsterGoodsItem.GetStringValue("Fixedaward");
            if (!string.IsNullOrEmpty(fixedaward))
            {
                string[] goodsFields = fixedaward.Split('|');
                for (int i = 0; i < goodsFields.Length; i++)
                {
                    string item = goodsFields[i].Trim();
                    if (item == "") continue;

                    string[] itemFields = item.Split(',');
                    if (itemFields.Length != 6)
                    {
                        continue;
                    }

                    GoodsData goodsData = null;

                    try
                    {
                        goodsData = new GoodsData()
                        {
                            Id = -1,
                            GoodsID = Convert.ToInt32(itemFields[0]),
                            Using = 0,
                            Forge_level = Convert.ToInt32(itemFields[3]),
                            Starttime = "1900-01-01 12:00:00",
                            Endtime = Global.ConstGoodsEndTime,
                            Site = 0,
                            Quality = Convert.ToInt32(itemFields[4]),
                            Props = "",
                            GCount = Convert.ToInt32(itemFields[1]),
                            Binding = Convert.ToInt32(itemFields[2]),
                            Jewellist = "",
                            BagIndex = 0,
                            AddPropIndex = 0,
                            BornIndex = Convert.ToInt32(itemFields[5]),
                            Lucky = 0,
                            Strong = 0,
                            ExcellenceInfo = 0,
                            AppendPropLev = 0,
                            ChangeLifeLevForEquip = 0,
                        };
                    }
                    catch (Exception)
                    {
                        goodsData = null;
                    }

                    if (null == goodsData)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("解析掉落项的固定奖励时时发生错误, GoodsPackID={0}, GoodsID={1}", goodsPackID, item));
                        continue;
                    }

                    fixedGoodsDataList.Add(goodsData);
                }
            }

            lock (_FixedGoodsItemsDict)
            {
                _FixedGoodsItemsDict[goodsPackID] = fixedGoodsDataList;
            }

            return fixedGoodsDataList;
        }

        /// <summary>
        /// 物品掉落单个的个数配置
        /// </summary>
        private Dictionary<int, int> _FallGoodsMaxCountDict = new Dictionary<int, int>();

        /// <summary>
        /// 根据物品掉落包ID来获取掉落的单个个数配置
        /// </summary>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        public int GetFallGoodsMaxCount(int goodsPackID)
        {
            int maxCount = -1;
            lock (_FallGoodsMaxCountDict)
            {
                if (_FallGoodsMaxCountDict.TryGetValue(goodsPackID, out maxCount))
                {
                    return maxCount;
                }
            }

            SystemXmlItem monsterGoodsItem = null;
            if (!GameManager.SystemMonsterGoodsList.SystemXmlItemDict.TryGetValue(goodsPackID, out monsterGoodsItem))
            {
                return -1;
            }

            maxCount = monsterGoodsItem.GetIntValue("MaxList");
            lock (_FallGoodsMaxCountDict)
            {
                _FallGoodsMaxCountDict[goodsPackID] = maxCount;
            }

            return maxCount;
        }

        /// <summary>
        /// 物品掉落单个的个数配置
        /// </summary>
        private Dictionary<int, int> _LimitTimeFallGoodsMaxCountDict = new Dictionary<int, int>();

        /// <summary>
        /// 根据物品掉落包ID来获取掉落的限制时间单个个数配置
        /// </summary>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        private int GetLimitTimeFallGoodsMaxCount(int goodsPackID)
        {
            int maxCount = -1;
            lock (_LimitTimeFallGoodsMaxCountDict)
            {
                if (_LimitTimeFallGoodsMaxCountDict.TryGetValue(goodsPackID, out maxCount))
                {
                    return maxCount;
                }
            }

            SystemXmlItem monsterGoodsItem = null;
            if (!GameManager.SystemLimitTimeMonsterGoodsList.SystemXmlItemDict.TryGetValue(goodsPackID, out monsterGoodsItem))
            {
                return -1;
            }

            maxCount = monsterGoodsItem.GetIntValue("MaxList");
            lock (_LimitTimeFallGoodsMaxCountDict)
            {
                _LimitTimeFallGoodsMaxCountDict[goodsPackID] = maxCount;
            }

            return maxCount;
        }

        /// <summary>
        /// 物品掉落品质缓存项
        /// </summary>
        private Dictionary<int, FallQualityItem> _FallGoodsQualityDict = new Dictionary<int, FallQualityItem>();

        /// <summary>
        /// 根据物品掉落品质ID获取缓存项
        /// </summary>
        /// <param name="fallQualityID"></param>
        /// <returns></returns>
        private FallQualityItem GetFallQualityItem(int fallQualityID)
        {
            FallQualityItem fallQualityItem = null;
            lock (_FallGoodsQualityDict)
            {
                _FallGoodsQualityDict.TryGetValue(fallQualityID, out fallQualityItem);
            }

            if (null != fallQualityItem)
            {
                return fallQualityItem;
            }

            SystemXmlItem goodsQualityItem = null;
            if (!GameManager.SystemGoodsQuality.SystemXmlItemDict.TryGetValue(fallQualityID, out goodsQualityItem))
            {
                return null;
            }

            fallQualityItem = new FallQualityItem()
            {
                ID = fallQualityID,
                QualityBasePercent = new double[(int)GoodsQuality.Max],
                QualitySelfPercent = new double[(int)GoodsQuality.Max],
            };

            string quality = goodsQualityItem.GetStringValue("Quality");
            if (!string.IsNullOrEmpty(quality))
            {
                string[] sa = quality.Split('|');
                if (sa.Length == (int)GoodsQuality.Max)
                {
                    fallQualityItem.QualitySelfPercent = Global.StringArray2DoubleArray(sa);
                    double basePercent = 0.0;
                    for (int i = 0; i < fallQualityItem.QualitySelfPercent.Length; i++)
                    {
                        fallQualityItem.QualityBasePercent[i] = basePercent;
                        basePercent += fallQualityItem.QualitySelfPercent[i];
                    }

                    if (basePercent > 1.0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("解析掉落项的品质掉落概率溢出1.0错误, fallQualityID={0}", fallQualityID));
                    }
                }
            }

            lock (_FallGoodsQualityDict)
            {
                _FallGoodsQualityDict[fallQualityID] = fallQualityItem;
            }

            return fallQualityItem;
        }

        /// <summary>
        /// 物品掉落级别缓存项
        /// </summary>
        private Dictionary<int, FallLevelItem> _FallGoodsLevelDict = new Dictionary<int, FallLevelItem>();

        /// <summary>
        /// 根据物品掉落锻造级别ID获取缓存项
        /// </summary>
        /// <param name="fallQualityID"></param>
        /// <returns></returns>
        private FallLevelItem GetFallLevelItem(int fallLevelID)
        {
            FallLevelItem fallLevelItem = null;
            lock (_FallGoodsLevelDict)
            {
                _FallGoodsLevelDict.TryGetValue(fallLevelID, out fallLevelItem);
            }

            if (null != fallLevelItem)
            {
                return fallLevelItem;
            }

            SystemXmlItem goodsLevelItem = null;
            if (!GameManager.SystemGoodsLevel.SystemXmlItemDict.TryGetValue(fallLevelID, out goodsLevelItem))
            {
                return null;
            }

            fallLevelItem = new FallLevelItem()
            {
                ID = fallLevelID,
                LevelBasePercent = new double[(int)Global.MaxForgeLevel + 1],
                LevelSelfPercent = new double[(int)Global.MaxForgeLevel + 1],
            };

            string level = goodsLevelItem.GetStringValue("Level");
            if (!string.IsNullOrEmpty(level))
            {
                string[] sa = level.Split('|');
                if (sa.Length == (Global.MaxForgeLevel + 1))
                {
                    fallLevelItem.LevelSelfPercent = Global.StringArray2DoubleArray(sa);
                    double basePercent = 0.0;
                    for (int i = 0; i < fallLevelItem.LevelSelfPercent.Length; i++)
                    {
                        fallLevelItem.LevelBasePercent[i] = basePercent;
                        basePercent += fallLevelItem.LevelSelfPercent[i];
                    }

                    if (basePercent > 1.0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("解析掉落项的级别掉落概率溢出1.0错误, fallLevelID={0}", fallLevelID));
                    }
                }
            }

            lock (_FallGoodsLevelDict)
            {
                _FallGoodsLevelDict[fallLevelID] = fallLevelItem;
            }

            return fallLevelItem;
        }

        /// <summary>
        /// 物品掉落天生属性缓存项
        /// </summary>
        private Dictionary<int, FallBornIndexItem> _FallGoodsBornIndexDict = new Dictionary<int, FallBornIndexItem>();

        /// <summary>
        /// 根据物品掉落天生属性ID获取缓存项
        /// </summary>
        /// <param name="fallQualityID"></param>
        /// <returns></returns>
        private FallBornIndexItem GetFallBornIndexItem(int fallBornIndexID)
        {
            FallBornIndexItem fallBornIndexItem = null;
            lock (_FallGoodsBornIndexDict)
            {
                _FallGoodsBornIndexDict.TryGetValue(fallBornIndexID, out fallBornIndexItem);
            }

            if (null != fallBornIndexItem)
            {
                return fallBornIndexItem;
            }

            SystemXmlItem goodsBornIndexItem = null;
            if (!GameManager.SystemGoodsBornIndex.SystemXmlItemDict.TryGetValue(fallBornIndexID, out goodsBornIndexItem))
            {
                return null;
            }

            fallBornIndexItem = new FallBornIndexItem()
            {
                ID = fallBornIndexID,
                LevelBasePercent = new double[(int)Global.MaxSubForgeLevel + 2],
                LevelSelfPercent = new double[(int)Global.MaxSubForgeLevel + 2],
            };

            string born = goodsBornIndexItem.GetStringValue("Born");
            if (!string.IsNullOrEmpty(born))
            {
                string[] sa = born.Split('|');
                if (sa.Length == (Global.MaxSubForgeLevel + 2))
                {
                    fallBornIndexItem.LevelSelfPercent = Global.StringArray2DoubleArray(sa);
                    double basePercent = 0.0;
                    for (int i = 0; i < fallBornIndexItem.LevelSelfPercent.Length; i++)
                    {
                        fallBornIndexItem.LevelBasePercent[i] = basePercent;
                        basePercent += fallBornIndexItem.LevelSelfPercent[i];
                    }

                    if (basePercent > 1.0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("解析掉落项的天生掉落概率溢出1.0错误, fallBornIndexID={0}", fallBornIndexID));
                    }
                }
            }

            lock (_FallGoodsBornIndexDict)
            {
                _FallGoodsBornIndexDict[fallBornIndexID] = fallBornIndexItem;
            }

            return fallBornIndexItem;
        }

        /// <summary>
        /// 物品追加级别缓存项
        /// </summary>
        private Dictionary<int, ZhuiJiaIDItem> _ZhuiJiaIDDict = new Dictionary<int, ZhuiJiaIDItem>();

        /// <summary>
        /// 根据物品掉落追加ID获取缓存项
        /// </summary>
        /// <param name="fallQualityID"></param>
        /// <returns></returns>
        private ZhuiJiaIDItem GetZhuiJiaIDItem(int zhuiJiaID)
        {
            ZhuiJiaIDItem zhuiJiaIDItem = null;
            lock (_ZhuiJiaIDDict)
            {
                _ZhuiJiaIDDict.TryGetValue(zhuiJiaID, out zhuiJiaIDItem);
            }

            if (null != zhuiJiaIDItem)
            {
                return zhuiJiaIDItem;
            }

            SystemXmlItem goodsZhuiJiaItem = null;
            if (!GameManager.SystemGoodsZhuiJia.SystemXmlItemDict.TryGetValue(zhuiJiaID, out goodsZhuiJiaItem))
            {
                return null;
            }

            zhuiJiaIDItem = new ZhuiJiaIDItem()
            {
                ID = zhuiJiaID,
                LevelBasePercent = new double[(int)Global.MaxZhuiJiaLevel + 1],
                LevelSelfPercent = new double[(int)Global.MaxZhuiJiaLevel + 1],
            };

            string level = goodsZhuiJiaItem.GetStringValue("ZhuiJiaLevel");
            if (!string.IsNullOrEmpty(level))
            {
                string[] sa = level.Split('|');
                if (sa.Length == (Global.MaxForgeLevel + 1))
                {
                    zhuiJiaIDItem.LevelSelfPercent = Global.StringArray2DoubleArray(sa);
                    double basePercent = 0.0;
                    for (int i = 0; i < zhuiJiaIDItem.LevelSelfPercent.Length; i++)
                    {
                        zhuiJiaIDItem.LevelBasePercent[i] = basePercent;
                        basePercent += zhuiJiaIDItem.LevelSelfPercent[i];
                    }

                    if (basePercent > 1.0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("解析掉落项的级别追加概率溢出1.0错误, zhuiJiaID={0}", zhuiJiaID));
                    }
                }
            }

            lock (_ZhuiJiaIDDict)
            {
                _ZhuiJiaIDDict[zhuiJiaID] = zhuiJiaIDItem;
            }

            return zhuiJiaIDItem;
        }

        /// <summary>
        /// 物品追加级别缓存项
        /// </summary>
        private Dictionary<int, ExcellencePropertyGroupItem> _ExcellencePropertyGroupItemDict = new Dictionary<int, ExcellencePropertyGroupItem>();

        /// <summary>
        /// 根据物品掉落卓越ID获取缓存项
        /// </summary>
        /// <param name="fallQualityID"></param>
        /// <returns></returns>
        public ExcellencePropertyGroupItem GetExcellencePropertyGroupItem(int excellencePropertyGroupID)
        {
            ExcellencePropertyGroupItem excellencePropertyGroupItem = null;
            lock (_ExcellencePropertyGroupItemDict)
            {
                _ExcellencePropertyGroupItemDict.TryGetValue(excellencePropertyGroupID, out excellencePropertyGroupItem);
            }

            if (null != excellencePropertyGroupItem)
            {
                return excellencePropertyGroupItem;
            }

            SystemXmlItem goodsExcellencePropertyItem = null;
            if (!GameManager.SystemGoodsExcellenceProperty.SystemXmlItemDict.TryGetValue(excellencePropertyGroupID, out goodsExcellencePropertyItem))
            {
                return null;
            }

            excellencePropertyGroupItem = new ExcellencePropertyGroupItem()
            {
                ID = excellencePropertyGroupID,
                Max = goodsExcellencePropertyItem.GetIntArrayValue("MAX"),
                ExcellencePropertyItems = ParseExcellencePropertyItems(goodsExcellencePropertyItem),
            };

            lock (_ExcellencePropertyGroupItemDict)
            {
                _ExcellencePropertyGroupItemDict[excellencePropertyGroupID] = excellencePropertyGroupItem;
            }

            return excellencePropertyGroupItem;
        }

        private ExcellencePropertyItem[] ParseExcellencePropertyItems(SystemXmlItem goodsExcellencePropertyItem)
        {
            ExcellencePropertyItem[] excellencePropertyItems = null;
            int nBase = 0;
            string property = goodsExcellencePropertyItem.GetStringValue("ExcellenceProperty");
            if (!string.IsNullOrEmpty(property))
            {
                string[] sa = property.Split('|');
                if (null != sa && sa.Length > 0)
                {
                    excellencePropertyItems = new ExcellencePropertyItem[sa.Length];
                    for (int i = 0; i < sa.Length; i++)
                    {
                        string[] fields = sa[i].Split(',');
                        if (2 == fields.Length)
                        {
                            excellencePropertyItems[i] = new ExcellencePropertyItem()
                            {
                                Num = Global.SafeConvertToInt32(fields[0]),
                                BasePercent = nBase,
                                SelfPercent = (int)(Global.SafeConvertToDouble(fields[1]) * 100000)
                            };

                            nBase += excellencePropertyItems[i].SelfPercent;
                        }
                    }
                }
            }

            return excellencePropertyItems;
        }

        /// <summary>
        /// 重置缓存项，以便下次访问时，从配置文件中重新读取
        /// </summary>
        public int ResetCachingItems()
        {
            int ret = GameManager.SystemMonsterGoodsList.ReloadLoadFromXMlFile();

            lock (_FallGoodsItemsDict)
            {
                _FallGoodsItemsDict.Clear();
            }

            ret = GameManager.SystemLimitTimeMonsterGoodsList.ReloadLoadFromXMlFile();

            lock (_LimitTimeFallGoodsItemsDict)
            {
                _LimitTimeFallGoodsItemsDict.Clear();
            }

            lock (_FixedGoodsItemsDict)
            {
                _FixedGoodsItemsDict.Clear();
            }

            lock (_FallGoodsMaxCountDict)
            {
                _FallGoodsMaxCountDict.Clear();
            }

            lock (_LimitTimeFallGoodsMaxCountDict)
            {
                _LimitTimeFallGoodsMaxCountDict.Clear();
            }

            if (ret < 0) return ret;

            ret = GameManager.SystemGoodsQuality.ReloadLoadFromXMlFile();

            lock (_FallGoodsQualityDict)
            {
                _FallGoodsQualityDict.Clear();
            }

            if (ret < 0) return ret;

            ret = GameManager.SystemGoodsLevel.ReloadLoadFromXMlFile();

            lock (_FallGoodsLevelDict)
            {
                _FallGoodsLevelDict.Clear();
            }

            if (ret < 0) return ret;

            ret = GameManager.SystemGoodsBornIndex.ReloadLoadFromXMlFile();

            lock (_FallGoodsBornIndexDict)
            {
                _FallGoodsBornIndexDict.Clear();
            }

            if (ret < 0) return ret;

            ret = GameManager.SystemGoodsZhuiJia.ReloadLoadFromXMlFile();

            lock (_ZhuiJiaIDDict)
            {
                _ZhuiJiaIDDict.Clear();
            }

            if (ret < 0) return ret;

            ret = GameManager.SystemGoodsExcellenceProperty.ReloadLoadFromXMlFile();

            lock (_ExcellencePropertyGroupItemDict)
            {
                _ExcellencePropertyGroupItemDict.Clear();
            }

            if (ret < 0) return ret;

            lock (_CacheShiQuGoodsDict)
            {
                _CacheShiQuGoodsDict.Clear();
            }
            
            return ret;
        }

        #endregion Xml配置项加速缓存

        #region 掉落物品包管理

        /// <summary>
        /// 每次掉落的最大个数
        /// </summary>
        public static int MaxFallCount = 10000;

        /// <summary>
        /// 物品掉落项字典
        /// </summary>
        private Dictionary<int, GoodsPackItem> _GoodsPackDict = new Dictionary<int, GoodsPackItem>();

        public Dictionary<int, GoodsPackItem> GoodsPackDict
        {
            get { return _GoodsPackDict; }
            set { _GoodsPackDict = value; }
        }

        /// <summary>
        /// 根据物品掉落品质获取掉落的物品的配置
        /// </summary>
        /// <param name="fallQualityID"></param>
        /// <param name="goodsQuality"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        private int GetFallGoodsQuality(int fallQualityID)
        {
            int goodsQuality = (int)GoodsQuality.White;
            if (-1 == fallQualityID)
            {
                return goodsQuality;
            }

            // 根据物品掉落品质ID获取缓存项
            FallQualityItem fallQualityItem = GetFallQualityItem(fallQualityID);
            if (null == fallQualityItem)
            {
                return goodsQuality;
            }

            int rndPercent = Global.GetRandomNumber(1, 100001);
            for (int i = 0; i < fallQualityItem.QualitySelfPercent.Length; i++)
            {
                int basePercent = (int)(fallQualityItem.QualityBasePercent[i] * 100000);
                int percent = (int)(fallQualityItem.QualitySelfPercent[i] * 100000);
                if (rndPercent > basePercent && rndPercent <= (basePercent + percent))
                {
                    goodsQuality = i;
                    break;
                }
            }

            return goodsQuality;
        }

        /// <summary>
        /// 根据物品掉落级别获取掉落的物品的级别配置
        /// </summary>
        /// <param name="fallLevelID"></param>
        /// <returns></returns>
        public int GetFallGoodsLevel(int fallLevelID)
        {
            int goodsLevel = 0;
            if (-1 == fallLevelID)
            {
                return goodsLevel;
            }

            // 根据物品掉落即被ID获取缓存项
            FallLevelItem fallLevelItem = GetFallLevelItem(fallLevelID);
            if (null == fallLevelItem)
            {
                return goodsLevel;
            }

            int rndPercent = Global.GetRandomNumber(1, 100001);
            for (int i = 0; i < fallLevelItem.LevelSelfPercent.Length; i++)
            {
                int basePercent = (int)(fallLevelItem.LevelBasePercent[i] * 100000);
                int percent = (int)(fallLevelItem.LevelSelfPercent[i] * 100000);
                if (rndPercent > basePercent && rndPercent <= (basePercent + percent))
                {
                    goodsLevel = i;
                    break;
                }
            }

            return goodsLevel;
        }

        /// <summary>
        /// 根据物品掉落级别获取掉落的物品的天生配置
        /// </summary>
        /// <param name="fallBornIndexID"></param>
        /// <returns></returns>
        private int GetFallGoodsBornIndex(IObject attacker, int fallBornIndexID, int goodsID)
        {
            int goodsBornIndex = 0;
            if (!(attacker is GameClient))
            {
                return goodsBornIndex;
            }

            if (!DBRoleBufferManager.ProcessFallTianSheng(attacker as GameClient))
            {
                return goodsBornIndex;
            }

            ///装备掉落的时候，获取天生属性
            goodsBornIndex = Global.GetBornIndexOnFallGoods(goodsID);
            
            return goodsBornIndex;
            /*int goodsBornIndex = 0;
            if (-1 == fallBornIndexID)
            {
                return goodsBornIndex;
            }

            // 根据物品掉落即被ID获取缓存项
            FallBornIndexItem fallBornIndexItem = GetFallBornIndexItem(fallBornIndexID);
            if (null == fallBornIndexItem)
            {
                return goodsBornIndex;
            }

            int rndPercent = Global.GetRandomNumber(1, 100001);
            for (int i = 0; i < fallBornIndexItem.LevelSelfPercent.Length; i++)
            {
                int basePercent = (int)(fallBornIndexItem.LevelBasePercent[i] * 100000);
                int percent = (int)(fallBornIndexItem.LevelSelfPercent[i] * 100000);
                if (rndPercent > basePercent && rndPercent <= (basePercent + percent))
                {
                    goodsBornIndex = i;
                    break;
                }
            }

            if (goodsBornIndex <= 0)
            {   
                return 0;
            }
            else if (goodsBornIndex >= 11)
            {
                return 100;
            }

            goodsBornIndex = Global.GetRandomNumber((goodsBornIndex - 1) * 10 + 1, goodsBornIndex * 10);
            goodsBornIndex = Global.GMin(100, goodsBornIndex);
            goodsBornIndex = Global.GMax(0, goodsBornIndex);

            return goodsBornIndex;*/
        }

        /// <summary>
        /// 根据物品追加ID别获取掉落的物品的幸运属性配置
        /// </summary>
        /// <param name="fallLevelID"></param>
        /// <returns></returns>
        public int GetLuckyGoodsID(int luckyPercent)
        {
            int rndPercent = Global.GetRandomNumber(1, 100001);
            if (rndPercent <= luckyPercent*100000)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// 根据物品追加ID别获取掉落的物品的追加属性配置
        /// </summary>
        /// <param name="fallLevelID"></param>
        /// <returns></returns>
        public int GetZhuiJiaGoodsLevelID(int zhuiJiaID)
        {
            int appendPropLev = 0;
            if (-1 == zhuiJiaID)
            {
                return appendPropLev;
            }

            // 根据物品掉落即被ID获取缓存项
            ZhuiJiaIDItem zhuiJiaIDItem = GetZhuiJiaIDItem(zhuiJiaID);
            if (null == zhuiJiaIDItem)
            {
                return appendPropLev;
            }

            int rndPercent = Global.GetRandomNumber(1, 100001);
            for (int i = 0; i < zhuiJiaIDItem.LevelSelfPercent.Length; i++)
            {
                int basePercent = (int)(zhuiJiaIDItem.LevelBasePercent[i] * 100000);
                int percent = (int)(zhuiJiaIDItem.LevelSelfPercent[i] * 100000);
                if (rndPercent > basePercent && rndPercent <= (basePercent + percent))
                {
                    appendPropLev = i;
                    break;
                }
            }

            return appendPropLev;
        }

        /// <summary>
        /// 根据物品追加ID别获取掉落的物品的卓越属性配置
        /// </summary>
        /// <param name="fallLevelID"></param>
        /// <returns></returns>
        public int GetExcellencePropertysID(int excellencePropertyGroupID)
        {
            int excellencePropertyID = 0;

            if (-1 == excellencePropertyGroupID)
            {
                return excellencePropertyID;
            }

            // 根据物品掉落即被ID获取缓存项
            ExcellencePropertyGroupItem excellencePropertyGroupItem = GetExcellencePropertyGroupItem(excellencePropertyGroupID);
            if (null == excellencePropertyGroupItem || null == excellencePropertyGroupItem.ExcellencePropertyItems || 
                    null == excellencePropertyGroupItem.Max || excellencePropertyGroupItem.Max.Length <= 0)
            {
                return excellencePropertyID;
            }

            List<int> idList = new List<int>();
            
            // 卓越属性掉落改造 [4/15/2014 LiaoWei]
            int nNum = 0;
            int rndPercent = Global.GetRandomNumber(1, 100001);
            for (int i = 0; i < excellencePropertyGroupItem.ExcellencePropertyItems.Length; i++)
            {
                /*int rndPercent = Global.GetRandomNumber(1, 100001);
                int percent = (int)(excellencePropertyGroupItem.ExcellencePropertyItems[i].Percent * 100000);
                if (rndPercent <= percent)
                {
                    idList.Add(excellencePropertyGroupItem.ExcellencePropertyItems[i].ID);
                }*/
                if (rndPercent > excellencePropertyGroupItem.ExcellencePropertyItems[i].BasePercent &&
                        rndPercent <= (excellencePropertyGroupItem.ExcellencePropertyItems[i].BasePercent + excellencePropertyGroupItem.ExcellencePropertyItems[i].SelfPercent))
                {
                    nNum = excellencePropertyGroupItem.ExcellencePropertyItems[i].Num;
                    break;
                }
            }

            if (nNum > 0 && nNum <= excellencePropertyGroupItem.Max.Length)
            {
                /*for (int n = 0; n < nNum; ++n)
                {
                    int nProp = 0;
                    nProp = Global.GetRandomNumber(0, nNum + 1);
                    idList.Add(nProp);
                }*/
                int nCount = 0;
                while(true)
                {
                    int nProp = 0;
                    nProp = Global.GetRandomNumber(0, excellencePropertyGroupItem.Max.Length);
                    if (idList.IndexOf(nProp) < 0)
                    {
                        idList.Add(nProp);
                        ++nCount;
                    }

                    if (nCount == nNum)
                        break;
                }
            }

            /*if (idList.Count > excellencePropertyGroupItem.Max)
            {
                idList = Global.RandomSortList<int>(idList);
            }*/

            for (int i = 0; i < idList.Count && i < excellencePropertyGroupItem.Max.Length; i++)
            {
                excellencePropertyID |= (0x01 << excellencePropertyGroupItem.Max[idList[i]]);
            }

            return excellencePropertyID;
        }

        

        public List<FallGoodsItem> GetFallGoodsItemByPercent(List<FallGoodsItem> gallGoodsItemList, int maxFallCount, int fallAlgorithm, double robotDropRate = 1.0)
        {
            if (null == gallGoodsItemList)
            {
                return gallGoodsItemList;
            }

            if (gallGoodsItemList.Count <= 0)
            {
                return gallGoodsItemList;
            }

            List<FallGoodsItem> goodsItemList = new List<FallGoodsItem>();
            if ((int)FallAlgorithm.MonsterFall == fallAlgorithm)
            {
                bool hasRobotDropRate = (robotDropRate < 1.0) ? true : false;
                for (int i = 0; i < gallGoodsItemList.Count; i++)
                {
                    int itemDropRate = gallGoodsItemList[i].SelfPercent;
                    if (hasRobotDropRate == true)
                    {
                        double rate = (double)itemDropRate * robotDropRate;
                        itemDropRate = (int)rate;
                    }
                    int randPercent = Global.GetRandomNumber(1, 100001);
                    if (randPercent <= itemDropRate)
                    {
                        goodsItemList.Add(gallGoodsItemList[i]);
                    }
                }

                if (goodsItemList.Count > maxFallCount)
                {
                    goodsItemList = Global.RandomSortList<FallGoodsItem>(goodsItemList);
                    goodsItemList = goodsItemList.GetRange(0, maxFallCount);
                }
            }
            else //宝箱掉落
            {
                //maxFallCount = Global.GMin(maxFallCount, gallGoodsItemList.Count);
                for (int i = 0; i < maxFallCount; i++)
                {
                    int randPercent = Global.GetRandomNumber(1, 100001);
                    FallGoodsItem fallGoodsItem = PickUpGoodsItemByPercent(gallGoodsItemList, randPercent);
                    if (null != fallGoodsItem)
                    {
                        goodsItemList.Add(fallGoodsItem);
                    }
                }
            }

            return goodsItemList;
        }

        private FallGoodsItem PickUpGoodsItemByPercent(List<FallGoodsItem> gallGoodsItemList, int randPercent)
        {
            FallGoodsItem fallGoodsItem = null;
            for (int i = 0; i < gallGoodsItemList.Count; i++)
            {
                if (randPercent > gallGoodsItemList[i].BasePercent && randPercent <= (gallGoodsItemList[i].BasePercent + gallGoodsItemList[i].SelfPercent))
                {
                    fallGoodsItem = gallGoodsItemList[i];
                    break;
                }
            }

            return fallGoodsItem;
        }

        /// <summary>
        /// 根据物品掉落ID获取要掉落的物品
        /// </summary>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        public List<GoodsData> GetGoodsDataList(IObject attacker, int goodsPackID, int maxFallCount, int forceBinding, double robotDropRate = 1.0)
        {
            List<GoodsData> goodsDataList = null;
            List<FallGoodsItem> fallGoodsItemList = GetNormalFallGoodsItem(goodsPackID);
            if (null == fallGoodsItemList)
            {
                return null;
            }

            List<GoodsData> fixedGoodsDataList = GetFixedGoodsDataList(goodsPackID);
            List<FallGoodsItem> tempItemList2 = GetFallGoodsItemByPercent(fallGoodsItemList, maxFallCount, (int)FallAlgorithm.MonsterFall, robotDropRate);
            if (tempItemList2.Count <= 0) //如果无掉落
            {
                if (null == fixedGoodsDataList || fixedGoodsDataList.Count <= 0) //如果无固定掉落项
                {
                    return null;
                }
            }
            else //根据掉落的概率进行排序
            {
                tempItemList2.Sort(delegate(FallGoodsItem item1, FallGoodsItem item2)
                {
                    return (item2.SelfPercent - item1.SelfPercent);
                });                
            }

            goodsDataList = new List<GoodsData>();

            //先将固定的奖励放前边，固定的奖励一般的没好东西
            if (null != fixedGoodsDataList && fixedGoodsDataList.Count > 0) //固定掉路的奖励
            {
                int toAddNum = GoodsPackManager.MaxFallCount - tempItemList2.Count;
                if (toAddNum > 0)
                {
                    toAddNum = Global.GMin(toAddNum, fixedGoodsDataList.Count);
                    for (int i = 0; i < toAddNum; i++)
                    {
                        GoodsData goodData = fixedGoodsDataList[i];
                        goodData.Id = GetNextGoodsID();
                        goodData.Binding = Math.Max(goodData.Binding, forceBinding);
                        goodsDataList.Add(goodData);
                    }
                }
            }        

            for (int i = 0; i < tempItemList2.Count; i++)
            {
                // 根据物品掉落品质ID获取掉落的物品的配置
                int goodsQualtiy = 0; // GetFallGoodsQuality(tempItemList2[i].FallQualityID);

                // 根据物品掉落级别获取掉落的物品的级别配置
                int goodsLevel = GetFallGoodsLevel(tempItemList2[i].FallLevelID);

                // 根据物品掉落级别获取掉落的物品的天生配置
                int goodsBornIndex = 0; // GetFallGoodsBornIndex(attacker, tempItemList2[i].FallBornIndexID, tempItemList2[i].GoodsID);

                //根据物品追加ID别获取掉落的物品的幸运属性配置
                int nLuckyProp = 0;
                int luckyRate = GetLuckyGoodsID(tempItemList2[i].LuckyRate);
                if (luckyRate > 0)
                {
                    int nValue = 0;
                    nValue = GameManager.GoodsPackMgr.GetLuckyGoodsID(luckyRate);
                    if (nValue >= 1)
                    {
                        nLuckyProp = 1;
                    }
                }

                //根据物品追加ID别获取掉落的物品的追加属性配置
                int appendPropLev = GetZhuiJiaGoodsLevelID(tempItemList2[i].ZhuiJiaID);

                //根据物品追加ID别获取掉落的物品的卓越属性配置
                int excellenceInfo = GetExcellencePropertysID(tempItemList2[i].ExcellencePropertyID);

                // 根据物品品质获取随机属性字符串(废弃)
                string props = "";

                GoodsData goodsData = new GoodsData()
                {
                    Id = GetNextGoodsID(),
                    GoodsID = tempItemList2[i].GoodsID,
                    Using = 0,
                    Forge_level = goodsLevel,
                    Starttime = "1900-01-01 12:00:00",
                    Endtime = Global.ConstGoodsEndTime,
                    Site = 0,
                    Quality = goodsQualtiy,
                    Props = props,
                    GCount = 1,
                    Binding = Math.Max(tempItemList2[i].Binding, forceBinding),
                    Jewellist = "",
                    BagIndex = 0,
                    AddPropIndex = 0,
                    BornIndex = goodsBornIndex,
                    Lucky = nLuckyProp,
                    Strong = 0,
                    ExcellenceInfo = excellenceInfo,
                    AppendPropLev = appendPropLev,
                    ChangeLifeLevForEquip = 0,
                };

                goodsDataList.Add(goodsData);
            }

            return goodsDataList;
        }

        /// <summary>
        /// 根据物品掉落ID获取要限制时间掉落的物品
        /// </summary>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        private List<GoodsData> GetLimitTimeGoodsDataList(IObject attacker, int goodsPackID, int maxFallCount, int forceBinding, double robotDropRate = 1.0)
        {
            List<GoodsData> goodsDataList = null;
            List<FallGoodsItem> fallGoodsItemList = GetLimitTimeFallGoodsItem(goodsPackID);
            if (null == fallGoodsItemList)
            {
                return null;
            }

            List<FallGoodsItem> tempItemList2 = GetFallGoodsItemByPercent(fallGoodsItemList, maxFallCount, (int)FallAlgorithm.MonsterFall, robotDropRate);
            if (tempItemList2.Count <= 0) //如果无掉落
            {
                return null;
            }
            else //根据掉落的概率进行排序
            {
                tempItemList2.Sort(delegate(FallGoodsItem item1, FallGoodsItem item2)
                {
                    return (item2.SelfPercent - item1.SelfPercent);
                });
            }

            goodsDataList = new List<GoodsData>();

            for (int i = 0; i < tempItemList2.Count; i++)
            {
                // 根据物品掉落品质ID获取掉落的物品的配置
                int goodsQualtiy = 0;// GetFallGoodsQuality(tempItemList2[i].FallQualityID);

                // 根据物品掉落级别获取掉落的物品的级别配置
                int goodsLevel = GetFallGoodsLevel(tempItemList2[i].FallLevelID);

                // 根据物品掉落级别获取掉落的物品的天生配置
                int goodsBornIndex = 0;// GetFallGoodsBornIndex(attacker, tempItemList2[i].FallBornIndexID, tempItemList2[i].GoodsID);

                //根据物品追加ID别获取掉落的物品的幸运属性配置
                int luckyRate = GetLuckyGoodsID(tempItemList2[i].LuckyRate);

                //根据物品追加ID别获取掉落的物品的追加属性配置
                int appendPropLev = GetZhuiJiaGoodsLevelID(tempItemList2[i].ZhuiJiaID);

                //根据物品追加ID别获取掉落的物品的卓越属性配置
                int excellenceInfo = GetExcellencePropertysID(tempItemList2[i].ExcellencePropertyID);

                // 根据物品品质获取随机属性字符串(废弃)
                string props = "";

                GoodsData goodsData = new GoodsData()
                {
                    Id = GetNextGoodsID(),
                    GoodsID = tempItemList2[i].GoodsID,
                    Using = 0,
                    Forge_level = goodsLevel,
                    Starttime = "1900-01-01 12:00:00",
                    Endtime = Global.ConstGoodsEndTime,
                    Site = 0,
                    Quality = goodsQualtiy,
                    Props = props,
                    GCount = 1,
                    Binding = Math.Max(tempItemList2[i].Binding, forceBinding),
                    Jewellist = "",
                    BagIndex = 0,
                    AddPropIndex = 0,
                    BornIndex = goodsBornIndex,
                    Lucky = luckyRate,
                    Strong = 0,
                    ExcellenceInfo = excellenceInfo,
                    AppendPropLev = appendPropLev,
                    ChangeLifeLevForEquip = 0,
                };

                goodsDataList.Add(goodsData);
            }

            return goodsDataList;
        }

        /// <summary>
        /// 如果是副本，则进行额外的判断
        /// </summary>
        /// <param name="copyMapID"></param>
        /// <param name="newGridX"></param>
        /// <param name="newGridY"></param>
        /// <returns></returns>
        private bool JugeFuBenMapFall(MapGrid mapGrid, int copyMapID, int newGridX, int newGridY)
        {
            //如果是在副本中
            if (copyMapID <= 0)
            {
                return false;
            }

            bool canFall = true;

            /// 获取指定格子中的对象列表
            List<Object> objsList = mapGrid.FindObjects(newGridX, newGridY);
            if (null != objsList)
            {
                for (int objIndex = 0; objIndex < objsList.Count; objIndex++)
                {
                    if (objsList[objIndex] is GoodsPackItem)
                    {
                        if ((objsList[objIndex] as GoodsPackItem).CopyMapID == copyMapID)
                        {
                            canFall = false;
                            break;
                        }
                    }
                }
            }

            return canFall;
        }

        /// <summary>
        /// 找一个空闲的格子
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="centerPoint"></param>
        /// <returns></returns>
        private Point FindABlankPoint(ObjectTypes objType, int mapCode, Dictionary<string, bool> dict, Point centerPoint, int copyMapID)
        {
            GameMap gameMap = GameManager.MapMgr.DictMaps[mapCode];
            Point fallPoint = new Point(centerPoint.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, centerPoint.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];

            int gridX = (int)centerPoint.X;
            int gridY = (int)centerPoint.Y;
            for (int circleNum = 1; circleNum <= 5; circleNum++)
            {
                for (int x = gridX - circleNum; x <= gridX + circleNum; x++)
                {
                    int newGridX = x;
                    int newGridY = gridY - circleNum;

                    string key = string.Format("{0}_{1}", newGridX, newGridY);
                    if (dict.ContainsKey(key))
                    {
                        continue;
                    }

                    if (!Global.InOnlyObs(objType, mapCode, newGridX, newGridY))
                    {
                        if (mapGrid.CanMove(objType, newGridX, newGridY, 0))
                        {
                            dict[key] = true;
                            fallPoint = new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                            return fallPoint;
                        }
                        else if (JugeFuBenMapFall(mapGrid, copyMapID, newGridX, newGridY))
                        {
                            dict[key] = true;
                            fallPoint = new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                            return fallPoint;
                        }
                    }
                }

                for (int x = gridX - circleNum; x <= gridX + circleNum; x++)
                {
                    int newGridX = x;
                    int newGridY = gridY + circleNum;

                    string key = string.Format("{0}_{1}", newGridX, newGridY);
                    if (dict.ContainsKey(key))
                    {
                        continue;
                    }

                    if (!Global.InOnlyObs(objType, mapCode, newGridX, newGridY))
                    {
                        if (mapGrid.CanMove(objType, newGridX, newGridY, 0))
                        {
                            dict[key] = true;
                            fallPoint = new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                            return fallPoint;
                        }
                        else if (JugeFuBenMapFall(mapGrid, copyMapID, newGridX, newGridY))
                        {
                            dict[key] = true;
                            fallPoint = new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                            return fallPoint;
                        }
                    }
                }

                for (int y = gridY - circleNum + 1; y <= gridY + circleNum - 1; y++)
                {
                    int newGridY = y;
                    int newGridX = gridX - circleNum;

                    string key = string.Format("{0}_{1}", newGridX, newGridY);
                    if (dict.ContainsKey(key))
                    {
                        continue;
                    }

                    if (!Global.InOnlyObs(objType, mapCode, newGridX, newGridY))
                    {
                        if (mapGrid.CanMove(objType, newGridX, newGridY, 0))
                        {
                            dict[key] = true;
                            fallPoint = new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                            return fallPoint;
                        }
                        else if (JugeFuBenMapFall(mapGrid, copyMapID, newGridX, newGridY))
                        {
                            dict[key] = true;
                            fallPoint = new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                            return fallPoint;
                        }
                    }
                }

                for (int y = gridY - circleNum + 1; y <= gridY + circleNum - 1; y++)
                {
                    int newGridY = y;
                    int newGridX = gridX + circleNum;

                    string key = string.Format("{0}_{1}", newGridX, newGridY);
                    if (dict.ContainsKey(key))
                    {
                        continue;
                    }

                    if (!Global.InOnlyObs(objType, mapCode, newGridX, newGridY))
                    {
                        if (mapGrid.CanMove(objType, newGridX, newGridY, 0))
                        {
                            dict[key] = true;
                            fallPoint = new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                            return fallPoint;
                        }
                        else if (JugeFuBenMapFall(mapGrid, copyMapID, newGridX, newGridY))
                        {
                            dict[key] = true;
                            fallPoint = new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                            return fallPoint;
                        }
                    }
                }
            }

            return fallPoint;
        }

        /// <summary>
        /// 找一个空白格子,如果5个格子内没空白格子,则以攻击者为圆心重新查找,仍然没有空白格子,返回攻击者位置
        /// </summary>
        /// <param name="objType"></param>
        /// <param name="mapCode"></param>
        /// <param name="dict"></param>
        /// <param name="centerPoint"></param>
        /// <param name="copyMapID"></param>
        /// <param name="attacker"></param>
        /// <returns></returns>
        private Point FindABlankPointEx(ObjectTypes objType, int mapCode, Dictionary<string, bool> dict, Point centerPoint, int copyMapID, IObject attacker)
        {
            GameMap gameMap = GameManager.MapMgr.DictMaps[mapCode];
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            Point fallPoint = new Point(centerPoint.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, centerPoint.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);

            int centerCridX = (int)centerPoint.X;
            int centerCridY = (int)centerPoint.Y;
            for (int k = 0; k < Global.GoodsPackFallGridArray_Length; k += 2)
            {
                int newGridX = Global.ClientViewGridArray[k] + centerCridX;
                int newGridY = Global.ClientViewGridArray[k + 1] + centerCridY;

                if (!Global.InOnlyObs(objType, mapCode, newGridX, newGridY))
                {
                    string key = string.Format("{0}_{1}", newGridX, newGridY);
                    if (dict.ContainsKey(key))
                    {
                        continue;
                    }

                    if (mapGrid.CanMove(objType, newGridX, newGridY, 0))
                    {
                        dict[key] = true;
                        return new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                    }
                    else if (JugeFuBenMapFall(mapGrid, copyMapID, newGridX, newGridY))
                    {
                        dict[key] = true;
                        return new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                    }
                }
            }

            if (null != attacker)
            {
                fallPoint = attacker.CurrentPos;
                centerCridX = (int)fallPoint.X;
                centerCridY = (int)fallPoint.Y;
                for (int k = 0; k < Global.GoodsPackFallGridArray_Length; k += 2)
                {
                    int newGridX = Global.ClientViewGridArray[k] + centerCridX;
                    int newGridY = Global.ClientViewGridArray[k + 1] + centerCridY;

                    if (!Global.InOnlyObs(objType, mapCode, newGridX, newGridY))
                    {
                        string key = string.Format("{0}_{1}", newGridX, newGridY);
                        if (dict.ContainsKey(key))
                        {
                            continue;
                        }

                        if (mapGrid.CanMove(objType, newGridX, newGridY, 0))
                        {
                            dict[key] = true;
                            return new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                        }
                        else if (JugeFuBenMapFall(mapGrid, copyMapID, newGridX, newGridY))
                        {
                            dict[key] = true;
                            return new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                        }
                    }
                }
            }

            return fallPoint;
        }

        /// <summary>
        /// 获取掉落物品的位置
        /// </summary>
        /// <param name="index"></param>
        /// <param name="centerPoint"></param>
        /// <returns></returns>
        public Point GetFallGoodsPosition(ObjectTypes objType, int mapCode, Dictionary<string, bool> dict, Point centerPoint, int copyMapID, IObject attacker)
        {    
            /*GameMap gameMap = GameManager.MapMgr.DictMaps[mapCode];
            Point fallPoint = new Point(centerPoint.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, centerPoint.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];

            List<Point> searchTableList = SearchTable.GetSearchTableList();

            int gridX = (int)(centerPoint.X);
            int gridY = (int)(centerPoint.Y);
            for (int i = 0; i < searchTableList.Count; i++)
            {
                int newGridX = gridX + (int)searchTableList[i].X;
                int newGridY = gridY + (int)searchTableList[i].Y;

                if (Global.InOnlyObs(objType, mapCode, newGridX, newGridY)) //这里只判断障害物
                {
                    continue;
                }

                if (!mapGrid.CanMove(objType, newGridX, newGridY, 0))
                {
                    continue;
                }

                if (dict.ContainsKey(i))
                {
                    continue;
                }

                dict[i] = true;
                fallPoint = new Point(newGridX * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, newGridY * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                break;
            }

            return fallPoint;*/

            //return FindABlankPoint(objType, mapCode, dict, centerPoint, copyMapID);
            return FindABlankPointEx(objType, mapCode, dict, centerPoint, copyMapID, attacker);
        }

        /// <summary>
        /// 获取一个怪物的物品掉路项
        /// </summary>
        /// <param name="ownerRoleID"></param>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        private GoodsPackItem GetMonsterGoodsPackItem(GameClient client, int ownerRoleID, string ownerRoleName, int goodsPackID, List<int> teamRoleIDs, int mapCode, int copyMapID, int toX, int toY, int forceBinding, string monsterName, int belongTo, int fallLevel, int teamID)
        {
            //根据物品掉落包ID来获取掉落的单个个数配置
            int maxFallCountByID = GetFallGoodsMaxCount(goodsPackID);
            if (maxFallCountByID <= 0)
            {
                maxFallCountByID = GoodsPackManager.MaxFallCount;
            }

            // 根据物品掉落ID获取要掉落的物品
            List<GoodsData> goodsDataList = GetGoodsDataList(client, goodsPackID, maxFallCountByID, forceBinding);

            //根据物品掉落包ID来获取掉落的单个个数配置
            maxFallCountByID = GetLimitTimeFallGoodsMaxCount(goodsPackID);
            if (maxFallCountByID <= 0)
            {
                maxFallCountByID = GoodsPackManager.MaxFallCount;
            }

            List<GoodsData> goodsDataListLimitTime = GetLimitTimeGoodsDataList(client, goodsPackID, maxFallCountByID, forceBinding);
            if (null == goodsDataList && null == goodsDataListLimitTime)
            {
                return null;
            }

            if (null != goodsDataList && null != goodsDataListLimitTime)
            {
                goodsDataList.AddRange(goodsDataListLimitTime);
            }
            else if (null == goodsDataList && null != goodsDataListLimitTime)
            {
                goodsDataList = goodsDataListLimitTime;
            }
            else if (null != goodsDataList && null == goodsDataListLimitTime)
            {
                //不处理
            }

            GoodsPackItem goodsPackItem = new GoodsPackItem()
            {
                AutoID = GetNextAutoID(),
                GoodsPackID = goodsPackID,
                OwnerRoleID = ownerRoleID,
                OwnerRoleName = ownerRoleName,
                GoodsPackType = 0,
                ProduceTicks = TimeUtil.NOW(),
                LockedRoleID = -1,
                GoodsDataList = goodsDataList,
                TeamRoleIDs = teamRoleIDs,
                MapCode = mapCode,
                FallPoint = new Point(toX, toY),
                CopyMapID = copyMapID,
                KilledMonsterName = monsterName,
                BelongTo = belongTo,
                FallLevel = fallLevel,
                TeamID = teamID,
            };

            lock (_GoodsPackDict)
            {
                _GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
            }

            return goodsPackItem;
        }

        /// <summary>
        /// 获取一个怪物的物品掉路项列表
        /// </summary>
        /// <param name="ownerRoleID"></param>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        public List<GoodsPackItem> GetMonsterGoodsPackItemList(IObject attacker, int ownerRoleID, string ownerRoleName, int goodsPackID, List<int> teamRoleIDs, int mapCode, int copyMapID, int toX, int toY, int forceBinding, string monsterName, int belongTo, int fallLevel, int teamID, int monsterType = -1)
        {
            //根据物品掉落包ID来获取掉落的单个个数配置
            int maxFallCountByID = GetFallGoodsMaxCount(goodsPackID);
            if (maxFallCountByID <= 0)
            {
                maxFallCountByID = GoodsPackManager.MaxFallCount;
            }

            double dropRate = 1.0;


            // 根据物品掉落ID获取要掉落的物品
            List<GoodsData> goodsDataList = GetGoodsDataList(attacker, goodsPackID, maxFallCountByID, forceBinding, dropRate);

            //根据物品掉落包ID来获取掉落的单个个数配置
            maxFallCountByID = GetLimitTimeFallGoodsMaxCount(goodsPackID);
            if (maxFallCountByID <= 0)
            {
                maxFallCountByID = GoodsPackManager.MaxFallCount;
            }

            List<GoodsData> goodsDataListLimitTime = GetLimitTimeGoodsDataList(attacker, goodsPackID, maxFallCountByID, forceBinding, dropRate);

            if (null == goodsDataList && null == goodsDataListLimitTime)
            {
                return null;
            }

            if (null != goodsDataList && null != goodsDataListLimitTime)
            {
                goodsDataList.AddRange(goodsDataListLimitTime);
            }
            else if (null == goodsDataList && null != goodsDataListLimitTime)
            {
                goodsDataList = goodsDataListLimitTime;
            }
            else if (null != goodsDataList && null == goodsDataListLimitTime)
            {
                //不处理
            }

            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();
            List<GoodsPackItem> goodsPackItemList = new List<GoodsPackItem>();
            for (int i = 0; i < goodsDataList.Count; i++)
            {
                List<GoodsData> oneGoodsDataList = new List<GoodsData>();
                oneGoodsDataList.Add(goodsDataList[i]);

                GoodsPackItem goodsPackItem = new GoodsPackItem()
                {
                    AutoID = GetNextAutoID(),
                    GoodsPackID = goodsPackID,
                    OwnerRoleID = ownerRoleID,
                    OwnerRoleName = ownerRoleName,
                    GoodsPackType = 0,
                    ProduceTicks = TimeUtil.NOW(),
                    LockedRoleID = -1,
                    GoodsDataList = oneGoodsDataList,
                    TeamRoleIDs = teamRoleIDs,
                    MapCode = mapCode,                    
                    CopyMapID = copyMapID,
                    KilledMonsterName = monsterName,
                    BelongTo = belongTo,
                    FallLevel = fallLevel,
                    TeamID = teamID,
                };

                goodsPackItem.FallPoint = GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, mapCode, gridDict, new Point(toX, toY), copyMapID, attacker);

                goodsPackItemList.Add(goodsPackItem);

                lock (_GoodsPackDict)
                {
                    _GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
                }
            }

            return goodsPackItemList;
        }

        /// <summary>
        /// 获取一个角色的物品掉路项
        /// </summary>
        /// <param name="ownerRoleID"></param>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        private GoodsPackItem GetRoleGoodsPackItem(int ownerRoleID, string ownerRoleName, int goodsPackID, List<GoodsData> goodsDataList, int mapCode, int copyMapID, int toGridX, int toGridY, string fromRoleName)
        {
            GoodsPackItem goodsPackItem = new GoodsPackItem()
            {
                AutoID = GetNextAutoID(),
                GoodsPackID = goodsPackID,
                OwnerRoleID = ownerRoleID,
                OwnerRoleName = ownerRoleName,
                GoodsPackType = 0,
                ProduceTicks = TimeUtil.NOW(),
                LockedRoleID = -1,
                GoodsDataList = goodsDataList,
                TeamRoleIDs = null,
                MapCode = mapCode,
                CopyMapID = copyMapID,
                KilledMonsterName = fromRoleName,
                BelongTo = -1,
                FallLevel = 0,
                TeamID = -1,
            };

            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();
            goodsPackItem.FallPoint = GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, mapCode, gridDict, new Point(toGridX, toGridY), copyMapID, null);

            lock (_GoodsPackDict)
            {
                _GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
            }

            return goodsPackItem;
        }

        /// <summary>
        /// 获取一个角色的物品掉路项列表
        /// </summary>
        /// <param name="ownerRoleID"></param>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        public List<GoodsPackItem> GetRoleGoodsPackItemList(int ownerRoleID, string ownerRoleName, List<GoodsData> goodsDataList, int mapCode, int copyMapID, int toGridX, int toGridY, string fromRoleName)
        {
            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();
            List<GoodsPackItem> goodsPackItemList = new List<GoodsPackItem>();
            for (int i = 0; i < goodsDataList.Count; i++)
            {
                List<GoodsData> oneGoodsDataList = new List<GoodsData>();
                oneGoodsDataList.Add(goodsDataList[i]);

                GoodsPackItem goodsPackItem = new GoodsPackItem()
                {
                    AutoID = GetNextAutoID(),
                    GoodsPackID = GetNextRoleGoodsPackID(),
                    OwnerRoleID = ownerRoleID,
                    OwnerRoleName = ownerRoleName,
                    GoodsPackType = 0,
                    ProduceTicks = TimeUtil.NOW(),
                    LockedRoleID = -1,
                    GoodsDataList = oneGoodsDataList,
                    TeamRoleIDs = null,
                    MapCode = mapCode,
                    CopyMapID = copyMapID,
                    KilledMonsterName = fromRoleName,
                    BelongTo = -1,
                    FallLevel = 0,
                    TeamID = -1,
                };

                goodsPackItem.FallPoint = GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, mapCode, gridDict, new Point(toGridX, toGridY), copyMapID, null);

                goodsPackItemList.Add(goodsPackItem);

                lock (_GoodsPackDict)
                {
                    _GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
                }
            }

            return goodsPackItemList;
        }

        /// <summary>
        /// 获取一个大乱斗中的角色的物品掉路项
        /// </summary>
        /// <param name="ownerRoleID"></param>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        private GoodsPackItem GetBattleGoodsPackItem(int ownerRoleID, string ownerRoleName, int goodsPackID, List<GoodsData> awardsGoodsDataList, List<GoodsData> giveGoodsDataList, int mapCode, int copyMapID, int toX, int toY)
        {
            List<GoodsData> packGoodsDataList = new List<GoodsData>();
            if (null != awardsGoodsDataList)
            {
                for (int i = 0; i < awardsGoodsDataList.Count; i++)
                {
                    packGoodsDataList.Add(awardsGoodsDataList[i]);
                }
            }

            for (int i = 0; i < giveGoodsDataList.Count; i++)
            {
                packGoodsDataList.Add(new GoodsData()
                {
                    Id = GetNextGoodsID(),
                    GoodsID = giveGoodsDataList[i].GoodsID,
                    Using = giveGoodsDataList[i].Using,
                    Forge_level = giveGoodsDataList[i].Forge_level,
                    Starttime = giveGoodsDataList[i].Starttime,
                    Endtime = giveGoodsDataList[i].Endtime,
                    Site = giveGoodsDataList[i].Site,
                    Quality = giveGoodsDataList[i].Quality,
                    Props = giveGoodsDataList[i].Props,
                    GCount = giveGoodsDataList[i].GCount,
                    Binding = giveGoodsDataList[i].Binding,
                    Jewellist = giveGoodsDataList[i].Jewellist,
                    BagIndex = 0,
                    AddPropIndex = 0,
                    BornIndex = 0,
                    Lucky = 0,
                    Strong = 0,
                    ExcellenceInfo = 0,
                    AppendPropLev = 0,
                    ChangeLifeLevForEquip = 0,
                });
            }

            GoodsPackItem goodsPackItem = new GoodsPackItem()
            {
                AutoID = GetNextAutoID(),
                GoodsPackID = goodsPackID,
                OwnerRoleID = ownerRoleID,
                OwnerRoleName = ownerRoleName,
                GoodsPackType = 0,
                ProduceTicks = TimeUtil.NOW(),
                LockedRoleID = -1,
                GoodsDataList = packGoodsDataList,
                TeamRoleIDs = null,
                MapCode = mapCode,
                FallPoint = new Point(toX, toY),
                CopyMapID = copyMapID,
                KilledMonsterName = "",
                BelongTo = -1,
                FallLevel = 0,
                TeamID = -1,
            };

            lock (_GoodsPackDict)
            {
                _GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
            }

            return goodsPackItem;
        }

        /// <summary>
        /// 获取标识某物品属于某人
        /// </summary>
        /// <param name="goodsPackItem"></param>
        private int FindGoodsID2RoleID(GoodsPackItem goodsPackItem, int goodsDbID)
        {
            int roleID = -1;
            if (null != goodsPackItem)
            {
                lock (_GoodsPackDict)
                {
                    if (null != goodsPackItem.TeamRoleIDs) //属于组队的
                    {
                        if (!goodsPackItem.GoodsIDToRolesDict.TryGetValue(goodsDbID, out roleID))
                        {
                            roleID = -1;
                        }
                    }
                }
            }

            return roleID;
        }

        /// <summary>
        /// 添加标识某物品属于某人
        /// </summary>
        /// <param name="goodsPackItem"></param>
        private void AddGoodsID2RoleID(GoodsPackItem goodsPackItem, int goodsDbID, int roleID)
        {
            if (null != goodsPackItem)
            {
                lock (_GoodsPackDict)
                {
                    goodsPackItem.GoodsIDToRolesDict[goodsDbID] = roleID;
                }
            }
        }

        /// <summary>
        /// 发送摇塞子的点数信息和最终获得者信息
        /// </summary>
        /// <param name="clientsArray"></param>
        /// <param name="msgText"></param>
        private void SendRandMessage(GameClient[] clientsArray, string msgText)
        {
            for (int i = 0; i < clientsArray.Length; i++)
            {
                GameClient gc = clientsArray[i];
                if (null == gc)
                {
                    continue;
                }

                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    gc, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlyChatBox, (int)HintErrCodeTypes.TeamChatTypeIndex);
            }
        }

        /// <summary>
        /// 判断物品属于组队中的那个？
        /// </summary>
        /// <param name="goodsPackItem"></param>
        /// <param name="goodsDbID"></param>
        private void JugeGoodsID2RoleID(GameClient client, GoodsPackItem goodsPackItem, int goodsDbID, int goodsID)
        {
            int MaxRandNum = -1;
            GameClient toClient = null;
            string goodsName = Global.GetGoodsNameByID(goodsID);
            GameClient[] clientsArray = new GameClient[goodsPackItem.TeamRoleIDs.Count];
            int[] RandNumArray = new int[goodsPackItem.TeamRoleIDs.Count];
            for (int i = 0; i < goodsPackItem.TeamRoleIDs.Count; i++)
            {
                GameClient gc = GameManager.ClientMgr.FindClient(goodsPackItem.TeamRoleIDs[i]);
                if (null == gc) //不参与
                {
                    clientsArray[i] = null;
                    RandNumArray[i] = -1;
                    continue;
                }

                int randNum = Global.GetRandomNumber(1, 101);

                clientsArray[i] = gc;
                RandNumArray[i] = randNum;

                if (randNum > MaxRandNum)
                {
                    MaxRandNum = randNum;
                    toClient = gc;
                }
            }

            //
            for (int i = 0; i < clientsArray.Length; i++)
            {
                GameClient gc = clientsArray[i];
                if (null == gc)
                {
                    continue;
                }

                /// 发送摇塞子的点数信息和最终获得者信息
                SendRandMessage(clientsArray, StringUtil.substitute(Global.GetLang("{0}对{1}摇出的骰子点数为{2}"), Global.FormatRoleName(gc, gc.ClientData.RoleName), goodsName, RandNumArray[i]));
            }

            if (null != toClient)
            {
                AddGoodsID2RoleID(goodsPackItem, goodsDbID, toClient.ClientData.RoleID);

                /// 发送摇塞子的点数信息和最终获得者信息
                SendRandMessage(clientsArray, StringUtil.substitute(Global.GetLang("{0}摇出了最大点数，获得{1}"), Global.FormatRoleName(toClient, toClient.ClientData.RoleName), goodsName));
            }
        }

        /// <summary>
        /// 根据掉落ID查找掉落的物品包对象
        /// </summary>
        /// <param name="autoID"></param>
        /// <returns></returns>
        public GoodsPackItem FindGoodsPackItem(int autoID)
        {
            GoodsPackItem goodsPackItem = null;
            lock (_GoodsPackDict)
            {
                if (!_GoodsPackDict.TryGetValue(autoID, out goodsPackItem))
                {
                    return null;
                }
            }

            return goodsPackItem;
        }

        #endregion 掉落物品包管理
           
        #region 处理铜钱的掉落

        /// <summary>
        /// 是否掉落的铜钱
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static bool IsFallTongQianGoods(int goodsID)
        {
            int fallTongQianGoodsID = (int)GameManager.systemParamsList.GetParamValueIntByName("FallTongQianGoodsID");
            if (fallTongQianGoodsID != goodsID)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 处理掉落的铜钱
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        /// <param name="goodsNum"></param>
        /// <returns></returns>
        private static bool ProcessFallTongQian(TCPOutPacketPool pool, GameClient client, int goodsID, int goodsNum, int fallLevel)
        {
            if (!IsFallTongQianGoods(goodsID))
            {
                return false;
            }

            SystemXmlItem systemDropMoneyItem = null;
            if (!GameManager.SystemDropMoney.SystemXmlItemDict.TryGetValue(fallLevel, out systemDropMoneyItem)) //如果没有找到则，不处理.但是返回成功
            {
                return true;
            }

            int minMoney = systemDropMoneyItem.GetIntValue("MinMoney");
            int maxMoney = systemDropMoneyItem.GetIntValue("MaxMoney");

            for (int i = 0; i < goodsNum; i++)
            {
                int money = Global.GetRandomNumber(minMoney, maxMoney);
                money = Global.FilterValue(client, money);
                GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, pool, client, money, "拾取金币");
            }

            return true;
        }

        #endregion 处理铜钱的掉落

        #region 自动拾取物品入背包处理

        /// <summary>
        /// 自动拾取物品进入背包
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="autoID"></param>
        public bool AutoAddThingIntoBag(SocketListener sl, TCPOutPacketPool pool, GameClient client, GoodsPackItem goodsPackItem, GoodsData goodsData)
        {
            //如果物品列表为null
            if (null == goodsData)
            {
                return false;
            }

            GameClient gc = null;
            int toRoleID = FindGoodsID2RoleID(goodsPackItem, goodsData.Id);
            if (-1 == toRoleID) //属于当前的点击者
            {
                gc = client;
            }
            else
            {
                //属于所有者
                gc = GameManager.ClientMgr.FindClient(toRoleID);
            }

            if (null == gc)
            {
                return false;
            }

            // 属性改造 去掉 负重[8/15/2013 LiaoWei]
            //是否能得到物品(判断包裹负重是否足够)
            /*if (!Global.CanAddGoodsWeight(gc, goodsData) &&  !IsFallTongQianGoods(goodsData.GoodsID))
            {
                /// 通知在线的对方(不限制地图)个人紧要消息
                GameManager.ClientMgr.NotifyImportantMsg(sl, pool, gc, Global.GetLang("背包负重不足，请先清理后再拾取物品"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoBagGrid);
                return false;
            }*/

            if (Global.CanAddGoods(gc, goodsData.GoodsID, 1, goodsData.Binding) || IsFallTongQianGoods(goodsData.GoodsID))
            {
                lock (_GoodsPackDict) //先锁定
                {
                    goodsPackItem.GoodsIDDict[goodsData.Id] = true;
                }

                //先判断是否是铜钱
                if (!ProcessFallTongQian(pool, gc, goodsData.GoodsID, goodsData.GCount, goodsPackItem.FallLevel))
                {
                    //添加物品
                    int nRet = Global.AddGoodsDBCommand_Hook(pool, gc, goodsData.GoodsID, goodsData.GCount, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, Global.GetLang("杀怪掉落后自动拾取"), true, goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, true); // 卓越信息 [12/13/2013 LiaoWei]
                    if (0 == nRet)
                    {
                        GameManager.logDBCmdMgr.AddDBLogInfo(/*Convert.ToInt32(goodsData.Id)*/-1, Global.ModifyGoodsLogName(goodsData), "杀怪掉落后自动拾取", Global.GetMapName(client.ClientData.MapCode), "系统", "销毁", goodsData.GCount, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId, goodsData);
                    }
                }

                //物品拾取通知
                GameManager.ClientMgr.NotifySelfGetThing(sl, pool, gc, goodsData.Id);

                // 七日活动
                SevenDayGoalEventObject evObj = SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.PickUpEquipCount);
                evObj.Arg1 = goodsData.GoodsID;
                evObj.Arg2 = goodsData.GCount;
                GlobalEventSource.getInstance().fireEvent(evObj);
            }
            else
            {
                /// 通知在线的对方(不限制地图)个人紧要消息
                // 由于一直给客户端发提示消息 现在注释掉 由客户端自己判断背包满了  [12/31/2013 LiaoWei]
                //GameManager.ClientMgr.NotifyImportantMsg(sl, pool, gc, Global.GetLang("背包已满,建议您将闲置的物品村存放到龙城[仓库管理员]处"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoBagGrid);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否是在自动挂机，如果是是否拾取物品
        /// </summary>
        /// <param name="goodsPackItem"></param>
        private bool CanAutoFightGetThings(GameClient client, GoodsPackItem goodsPackItem, GoodsData goodsData)
        {
            if (!client.ClientData.AutoFighting)
            {
                return false;
            }

            //判断是否开启了自动拾取功能
            if (client.ClientData.AutoFightGetThings <= 0)
            {
                return false;
            }

            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out systemGoods) || null == systemGoods)
            {
                LogManager.WriteLog(LogTypes.Warning, string.Format("处理自动挂机拾取物品到背包时，获取物品xml信息失败: GoodsID={0}", goodsData.GoodsID));
                return false; //获取物品信息失败，配置错误？？？？
            }

            int categoriy = systemGoods.GetIntValue("Categoriy");
            //if (categoriy >= (int)ItemCategories.Weapon && categoriy < (int)ItemCategories.EquipMax) //装备
            //{
            //    //判断是否开启了自动拾取装备功能
            //    if (0x01 != ((byte)client.ClientData.AutoFightGetThings & (byte)0x01))
            //    {
            //        return false;
            //    }
            //}
            if ((int)ItemCategories.MoneyPack == categoriy) //铜钱包
            {
                if (0x04 != ((byte)client.ClientData.AutoFightGetThings & (byte)0x04))
                {
                    return false;
                }
            }
            else if ((int)ItemCategories.ItemDrug == categoriy) //药品
            {
                if (0x08 != ((byte)client.ClientData.AutoFightGetThings & (byte)0x08))
                {
                    return false;
                }
            }
            else //其他物品
            {
                //判断是否开启了自动拾取其他物品功能
                if (0x02 != ((byte)client.ClientData.AutoFightGetThings & (byte)0x02))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 自动拾取物品
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="autoID"></param>
        public bool AutoGetThings(SocketListener sl, TCPOutPacketPool pool, GameClient client, GoodsPackItem goodsPackItem)
        {
            //如果自己处于挂机并且组队的状态，则不自动拾取
            if (client.ClientData.AutoFighting)
            {
                if (goodsPackItem.TeamRoleIDs != null) //组队而且有分享的模式
                {
                    return false;
                }
            }

            if (null == goodsPackItem.GoodsDataList || goodsPackItem.GoodsDataList.Count <= 0)
            {
                return true; //处理完毕返回true
            }

            int getThingCount = 0;
            for (int i = 0; i < goodsPackItem.GoodsDataList.Count; i++)
            {
                //是否是在自动挂机，如果是是否拾取物品
                if (CanAutoFightGetThings(client, goodsPackItem, goodsPackItem.GoodsDataList[i]))
                {
                    /// 自动拾取物品进入背包
                    if (AutoAddThingIntoBag(sl, pool, client, goodsPackItem, goodsPackItem.GoodsDataList[i]))
                    {
                        getThingCount++;
                    }
                }
                else
                {
                    //是否自动拾取物品进入背包
                    if (Data.AutoGetThing > 0)
                    {
                        /// 自动拾取物品进入背包
                        if (AutoAddThingIntoBag(sl, pool, client, goodsPackItem, goodsPackItem.GoodsDataList[i]))
                        {
                            getThingCount++;
                        }
                    }
                }
            }

            if (getThingCount < goodsPackItem.GoodsDataList.Count)
            {
                return false;
            }

            return true; //处理完毕返回true
        }

        #endregion 自动拾取物品入背包处理

        #region 处理队伍字符串

        /// <summary>
        /// 格式化队伍的字符串
        /// </summary>
        /// <param name="goodsPackItem"></param>
        /// <returns></returns>
        public string FormatTeamRoleIDs(GoodsPackItem goodsPackItem)
        {
            string teamRoleIDs = "";
            if (null == goodsPackItem)
            {
                return teamRoleIDs;
            }

            if (null != goodsPackItem.TeamRoleIDs && goodsPackItem.TeamRoleIDs.Count > 0)
            {
                for (int i = 0; i < goodsPackItem.TeamRoleIDs.Count; i++)
                {
                    if (teamRoleIDs.Length > 0)
                    {
                        teamRoleIDs += ",";
                    }

                    teamRoleIDs += goodsPackItem.TeamRoleIDs[i].ToString();
                }
            }

            return teamRoleIDs;
        }

        #endregion 处理队伍字符串

        #region 处理怪物掉落

        /// <summary>
        /// 处理掉落
        /// </summary>
        public void ProcessMonster(SocketListener sl, TCPOutPacketPool pool, IObject attacker, Monster monster)
        {
            if (attacker is GameClient)
            {
                ProcessMonsterByClient(sl, pool, attacker as GameClient, monster);
            }
            else
            {
                ProcessMonsterByMonster(sl, pool, attacker as Monster, monster);
            }
        }

        /// <summary>
        /// 处理掉落
        /// </summary>
        public void ProcessMonsterByClient(SocketListener sl, TCPOutPacketPool pool, GameClient client, Monster monster)
        {
            if (!Global.FilterFallGoods(client))
            {
                return;
            }

            //以怪物为中心
            //if (monster.MonsterInfo.VLevel < 1000)
            //{
            //    int lowNofalfLevel = GameManager.GameConfigMgr.GetGameConfigItemInt("low-nofall-level", 20);
            //    int upNofallLevel = GameManager.GameConfigMgr.GetGameConfigItemInt("up-nofall-level", 20);
            //    if (client.ClientData.Level - monster.MonsterInfo.VLevel >= lowNofalfLevel)
            //    {
            //        return;
            //    }

            //    if (monster.MonsterInfo.VLevel - client.ClientData.Level >= upNofallLevel)
            //    {
            //        return;
            //    }
            //}
#if ___CC___FUCK___YOU___BB___
            if (monster.XMonsterInfo.Dropped.Count < 0)
            {
                return;
            }
#else
            if (monster.MonsterInfo.FallGoodsPackID < 0)
            {
                return;
            }
#endif

            /// 处理采集掉落  暂时不支持采集物掉落物品（add by tanglong 2014/11/19)
            //if (ProcessMonsterOfCaiJiByClient(sl, pool, client, monster))
            //{
            //    return;
            // }

            bool isTeamSharingMap = true;
            if (//client.ClientData.MapCode == GameManager.BattleMgr.BattleMapCode || 
                monster.CurrentMapCode == GameManager.ArenaBattleMgr.BattleMapCode) //角斗场模式下，不进行组队分配--但炎黄战场允许
            {
                isTeamSharingMap = false;
            }

            int teamID = -1;
            List<int> teamRoleIDs = null;
            if (client.ClientData.TeamID > 0 && isTeamSharingMap)
            {
                //查找组队数据
                TeamData td = GameManager.TeamMgr.FindData(client.ClientData.TeamID);
                if (td != null && td.GetThingOpt > 0)
                {
                    lock (td)
                    {
                        teamID = td.TeamID;
                        teamRoleIDs = new List<int>();
                        for (int i = 0; i < td.TeamRoles.Count; i++)
                        {
                            if (td.TeamRoles[i].RoleID == client.ClientData.RoleID)
                            {
                                teamRoleIDs.Add(td.TeamRoles[i].RoleID);
                                continue;
                            }

                            //不在同一个地图上不参与分配
                            GameClient gc = GameManager.ClientMgr.FindClient(td.TeamRoles[i].RoleID);
                            if (null == gc)
                            {
                                continue;
                            }

                            //如果不在同一个地图上，则不处理
                            if (gc.ClientData.MapCode != monster.CurrentMapCode)
                            {
                                continue;
                            }

                            //如果不在同一个副本地图上，则不处理
                            if (gc.ClientData.CopyMapID != monster.CurrentCopyMapID)
                            {
                                continue;
                            }

                            //如果对方具体则不处理
                            if (!Global.InCircle(new Point(gc.ClientData.PosX, gc.ClientData.PosY), monster.SafeCoordinate, 800))
                            {
                                continue;
                            }

                            teamRoleIDs.Add(td.TeamRoles[i].RoleID);
                        }

                        if (teamRoleIDs.Count <= 1) //只有自己, 强迫未非组队模式
                        {
                            teamRoleIDs = null;
                        }
                    }
                }
            }

            // ****怪的掉落是否绑定 与副本没关系**** [8/15/2013 LiaoWei]
            //判断是否是副本，如果是是否设置了强制绑定
            //int forceBinding = FuBenManager.GetFuBenMapAwardsGoodsBinding(client);
            int forceBinding = -1;
            // ****怪的掉落是否绑定 与副本没关系**** [8/15/2013 LiaoWei]

            /*GoodsPackItem goodsPackItem = GetMonsterGoodsPackItem(client.ClientData.RoleID, Global.FormatRoleName(client, client.ClientData.RoleName), monster.FallGoodsPackID, teamRoleIDs,
                client.ClientData.MapCode, client.ClientData.CopyMapID, (int)monster.SafeCoordinate.X, (int)monster.SafeCoordinate.Y, forceBinding, monster.VSName);
            if (null == goodsPackItem) //获取掉路项失败
            {
                //失败是正常的，因为可能爆，也可能不爆，和随机数有关系
                //LogManager.WriteLog(LogTypes.Warning, string.Format("处理怪物掉落时，获取掉落项失败: GoodsPackID={0}, RoleID={1}", monster.FallGoodsPackID, client.ClientData.RoleID));
                return;
            }

            /// 自动拾取物品
            if (AutoGetThings(sl, pool, client, goodsPackItem))
            {
                //从内存中删除包裹
                lock (_GoodsPackDict) //先锁定
                {
                    _GoodsPackDict.Remove(goodsPackItem.AutoID);
                }

                goodsPackItem = null;
                return; //自动拾取了，不必再处理
            }

            GameManager.MapGridMgr.DictGrids[goodsPackItem.MapCode].MoveObject(-1, -1, (int)goodsPackItem.FallPoint.X, (int)goodsPackItem.FallPoint.Y, goodsPackItem);

            List<Object> objList = Global.GetAll9Clients(goodsPackItem.MapCode, (int)goodsPackItem.FallPoint.X, (int)goodsPackItem.FallPoint.Y, goodsPackItem.CopyMapID);
            GameManager.ClientMgr.NotifyOthersNewGoodsPack(sl, pool, objList, goodsPackItem.OwnerRoleID, goodsPackItem.OwnerRoleName,
                goodsPackItem.AutoID, goodsPackItem.GoodsPackID,
                goodsPackItem.MapCode,
                (int)monster.SafeCoordinate.X,
                (int)monster.SafeCoordinate.Y);*/

            Point grid = monster.CurrentGrid;
#if ___CC___FUCK___YOU___BB___
            List<GoodsPackItem> goodsPackItemList = GetMonsterGoodsPackItemList(client, client.ClientData.RoleID, Global.FormatRoleName(client, client.ClientData.RoleName),
                 monster.XMonsterInfo.Dropped[Global.GetRandomNumber(0, monster.XMonsterInfo.Dropped.Count)],
                teamRoleIDs,
                monster.CurrentMapCode, monster.CurrentCopyMapID, (int)grid.X, (int)grid.Y, forceBinding, 
                monster.XMonsterInfo.Name, 1, monster.XMonsterInfo.Level, teamID, monster.MonsterType);
#else
            List<GoodsPackItem> goodsPackItemList = GetMonsterGoodsPackItemList(client, client.ClientData.RoleID, Global.FormatRoleName(client, client.ClientData.RoleName),
                monster.MonsterInfo.FallGoodsPackID, teamRoleIDs,
                monster.CurrentMapCode, monster.CurrentCopyMapID, (int)grid.X, (int)grid.Y, forceBinding, 
                monster.MonsterInfo.VSName, monster.MonsterInfo.FallBelongTo, monster.MonsterInfo.VLevel, teamID, monster.MonsterType);
#endif
            if (null == goodsPackItemList || goodsPackItemList.Count <= 0) //获取掉路项失败
            {
                //失败是正常的，因为可能爆，也可能不爆，和随机数有关系
                //LogManager.WriteLog(LogTypes.Warning, string.Format("处理怪物掉落时，获取掉落项失败: GoodsPackID={0}, RoleID={1}", monster.FallGoodsPackID, client.ClientData.RoleID));
                return;
            }

            //处理掉落
            for (int i = 0; i < goodsPackItemList.Count; i++)
            {
                ProcessGoodsPackItem(client, monster, goodsPackItemList[i], forceBinding);
#if ___CC___FUCK___YOU___BB___
                //得到装备的提示
                bool bNeedSend = true;
                if (monster.XMonsterInfo.MonsterId == 1800 || monster.XMonsterInfo.MonsterId == 1900 || monster.XMonsterInfo.MonsterId == 2900 ||
                        monster.XMonsterInfo.MonsterId == 3900 || monster.XMonsterInfo.MonsterId == 4900 || monster.XMonsterInfo.MonsterId == 5900 ||
                        monster.XMonsterInfo.MonsterId == 6900 || monster.XMonsterInfo.MonsterId == 7900 || monster.XMonsterInfo.MonsterId == 8900)
                {
                    bNeedSend = false;
                }

                Global.BroadcastGetGoodsHint(client, goodsPackItemList[i].GoodsDataList[0], monster.XMonsterInfo.Name, monster.CurrentMapCode, bNeedSend);
#else
                bool bNeedSend = true;
                if (monster.MonsterInfo.ExtensionID == 1800 || monster.MonsterInfo.ExtensionID == 1900 || monster.MonsterInfo.ExtensionID == 2900 ||
                        monster.MonsterInfo.ExtensionID == 3900 || monster.MonsterInfo.ExtensionID == 4900 || monster.MonsterInfo.ExtensionID == 5900 ||
                        monster.MonsterInfo.ExtensionID == 6900 || monster.MonsterInfo.ExtensionID == 7900 || monster.MonsterInfo.ExtensionID == 8900)
                {
                    bNeedSend = false;
                }

                Global.BroadcastGetGoodsHint(client, goodsPackItemList[i].GoodsDataList[0], monster.MonsterInfo.VSName, monster.CurrentMapCode, bNeedSend);
#endif
            }
        }

        /*
        /// <summary>
        /// 处理采集掉落  暂时不支持采集物掉落物品（add by tanglong 2014/11/19)
        /// </summary>
        public bool ProcessMonsterOfCaiJiByClient(SocketListener sl, TCPOutPacketPool pool, GameClient client, Monster monster)
        {
            if (monster.MonsterType != (int)MonsterTypes.CaiJi)
            {
                return false;
            }

            int fallID = monster.MonsterInfo.FallGoodsPackID;
            if (fallID <= 0) return false;

            List<FallGoodsItem> gallGoodsItemList = GameManager.GoodsPackMgr.GetFallGoodsItemList(fallID);
            if (null == gallGoodsItemList) return false;

            List<FallGoodsItem> tempItemList2 = GameManager.GoodsPackMgr.GetFallGoodsItemByPercent(gallGoodsItemList, 1, (int)FallAlgorithm.BaoXiang);
            if (tempItemList2.Count <= 0) return false;

            List<GoodsData> goodsDataList = GameManager.GoodsPackMgr.GetGoodsDataListFromFallGoodsItemList(tempItemList2);

            // 属性改造 去掉 负重[8/15/2013 LiaoWei]
            //是否能得到物品(判断包裹负重是否足够)
            /*if (!Global.CanAddGoodsWeight(client, goodsDataList[0]))
            {
                /// 通知在线的对方(不限制地图)个人紧要消息
                GameManager.ClientMgr.NotifyImportantMsg(sl, pool, client, Global.GetLang("背包负重不足，请先清理后再拾取物品"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoBagGrid);
                return false;
            }*/

        /*     if (!Global.CanAddGoods(client, goodsDataList[0].GoodsID, 1, goodsDataList[0].Binding, Global.ConstGoodsEndTime, true))
             {
                 /// 通知在线的对方(不限制地图)个人紧要消息
                 GameManager.ClientMgr.NotifyImportantMsg(sl, pool, client, Global.GetLang("背包已满,建议您将闲置的物品村存放到勇者大陆[仓库管理员]处"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoBagGrid);
                 return false;
             }

             for (int i = 0; i < goodsDataList.Count; i++)
             {
                 //想DBServer请求加入某个新的物品到背包中
                 //添加物品
                 Global.AddGoodsDBCommand_Hook(pool, client, goodsDataList[i].GoodsID, 
                     goodsDataList[i].GCount, goodsDataList[i].Quality, goodsDataList[i].Props, goodsDataList[i].Forge_level, 
                     goodsDataList[i].Binding, 0, goodsDataList[i].Jewellist, true, 1, //"从怪物采集获取", true, goodsDataList[i].Endtime, 
                     goodsDataList[i].AddPropIndex, goodsDataList[i].BornIndex, goodsDataList[i].Lucky, goodsDataList[i].Strong, goodsDataList[i].ExcellenceInfo, goodsDataList[i].AppendPropLev, goodsDataList[i].ChangeLifeLevForEquip, true); // 卓越信息 [12/13/2013 LiaoWei]
             }

             return true;
         }
         */

        /// <summary>
        /// 处理掉落
        /// </summary>
        public void ProcessMonsterByMonster(SocketListener sl, TCPOutPacketPool pool, Monster attacker, Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            if (monster.XMonsterInfo.Dropped.Count < 0)
            {
                return;
            }
#else
               if (monster.MonsterInfo.FallGoodsPackID < 0)
            {
                return;
            }
#endif

            //判断是否是副本，如果是是否设置了强制绑定
            int forceBinding = 0;

            Point grid = monster.CurrentGrid;
#if ___CC___FUCK___YOU___BB___
             List<GoodsPackItem> goodsPackItemList = GetMonsterGoodsPackItemList(attacker, -1, "",
                 monster.XMonsterInfo.Dropped[Global.GetRandomNumber(0, monster.XMonsterInfo.Dropped.Count)], 
                 null,
                attacker.MonsterZoneNode.MapCode, attacker.CopyMapID, (int)grid.X, (int)grid.Y, forceBinding, monster.XMonsterInfo.Name,
                0, monster.XMonsterInfo.Level, -1);
            if (null == goodsPackItemList || goodsPackItemList.Count <= 0) //获取掉路项失败
            {
                //失败是正常的，因为可能爆，也可能不爆，和随机数有关系
                //LogManager.WriteLog(LogTypes.Warning, string.Format("处理怪物掉落时，获取掉落项失败: GoodsPackID={0}, RoleID={1}", monster.FallGoodsPackID, client.ClientData.RoleID));
                return;
            }
#else
            List<GoodsPackItem> goodsPackItemList = GetMonsterGoodsPackItemList(attacker, -1, "", monster.MonsterInfo.FallGoodsPackID, null,
               attacker.MonsterZoneNode.MapCode, attacker.CopyMapID, (int)grid.X, (int)grid.Y, forceBinding, monster.MonsterInfo.VSName, monster.MonsterInfo.FallBelongTo, monster.MonsterInfo.VLevel, -1);
            if (null == goodsPackItemList || goodsPackItemList.Count <= 0) //获取掉路项失败
            {
                //失败是正常的，因为可能爆，也可能不爆，和随机数有关系
                //LogManager.WriteLog(LogTypes.Warning, string.Format("处理怪物掉落时，获取掉落项失败: GoodsPackID={0}, RoleID={1}", monster.FallGoodsPackID, client.ClientData.RoleID));
                return;
            }
#endif


            //处理掉落
            for (int i = 0; i < goodsPackItemList.Count; i++)
            {
                ProcessGoodsPackItem(attacker, monster, goodsPackItemList[i], forceBinding);
            }
        }

#endregion 处理怪物掉落

#region 掉落包裹到地图

        public void ProcessGoodsPackItem(IObject attacker, IObject obj, GoodsPackItem goodsPackItem, int forceBinding)
        {
            if (null == goodsPackItem) //获取掉路项失败
            {
                //失败是正常的，因为可能爆，也可能不爆，和随机数有关系
                //LogManager.WriteLog(LogTypes.Warning, string.Format("处理怪物掉落时，获取掉落项失败: GoodsPackID={0}, RoleID={1}", monster.FallGoodsPackID, client.ClientData.RoleID));
                return;
            }

            //最早的一个版本时客户端处理，自动去拾取。但是当时的调度有些问题，会导致，挂机不打怪。
            //所以，改为了此处自动飞入包裹
            //传奇版本，必须客户端处理，怎么办？因为现在有了格子，能否和怪物一样处理，把掉落的包裹当做怪物处理。
            //打怪时，专心打怪。捡取东西时，专心捡取东西。

            //if (attacker is GameClient && obj is Monster)
            //{
            //    if (null != obj)
            //    {
            //        //有属主的怪物掉落，并且不是boss的掉落，可以自动拾取(如果客户端配置了自动拾取选项)
            //        if ((goodsPackItem.BelongTo >= 1 && (int)MonsterTypes.BOSS != (obj as Monster).MonsterType) || FuBenManager.CanFuBenMapFallGoodsAutoGet(attacker as GameClient))
            //        {
            //            //自动拾取物品
            //            if (AutoGetThings(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, attacker as GameClient, goodsPackItem))
            //            {
            //                //从内存中删除包裹
            //                lock (_GoodsPackDict) //先锁定
            //                {
            //                    _GoodsPackDict.Remove(goodsPackItem.AutoID);
            //                }

            //                goodsPackItem = null;
            //                return; //自动拾取了，不必再处理
            //            }
            //        }
            //    }
            //}

            GameManager.MapGridMgr.DictGrids[goodsPackItem.MapCode].MoveObject(-1, -1, (int)goodsPackItem.FallPoint.X, (int)goodsPackItem.FallPoint.Y, goodsPackItem);

            /// 格式化队伍的字符串
            //string teamRoleIDs = FormatTeamRoleIDs(goodsPackItem);            

            //List<Object> objList = Global.GetAll9Clients(goodsPackItem);
            //GameManager.ClientMgr.NotifyOthersNewGoodsPack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, objList, goodsPackItem.OwnerRoleID, goodsPackItem.OwnerRoleName,
            //    goodsPackItem.AutoID, goodsPackItem.GoodsPackID,
            //    goodsPackItem.MapCode,
            //    (int)goodsPackItem.FallPoint.X,
            //    (int)goodsPackItem.FallPoint.Y,
            //    goodsPackItem.GoodsDataList[0].GoodsID,
            //    goodsPackItem.GoodsDataList[0].GCount,
            //    goodsPackItem.ProduceTicks,
            //    goodsPackItem.TeamID,
            //    teamRoleIDs);

            /// 记录掉落物品时的日志
            WriteFallGoodsRecords(goodsPackItem);
        }

#endregion 掉落包裹到地图

#region 处理个人掉落

        /// <summary>
        /// 处理角色掉落
        /// </summary>
        public void ProcessRole(SocketListener sl, TCPOutPacketPool pool, GameClient client, GameClient otherClient, string enemyName)
        {
            //判断地图是否允许掉落装备
            if (!Global.CanMapLostEquip(client.ClientData.MapCode))
            {
                return;
            }

            if (!Global.FilterFallGoods(client))
            {
                return;
            } 

            if (null == otherClient.ClientData.GoodsDataList) return;

            lock (otherClient.ClientData.GoodsDataList)
            {
                if (otherClient.ClientData.GoodsDataList.Count <= 0)
                {
                    return;
                }
            }

            //每次掉落的佩戴的装备的概率
            int maxFallRoleUsingRate = 0;

            //每次掉落的包裹物品最大个数
            int maxFallRoleBagRate = 0;

            int maxFallUsingGoodsNum = 1;
            int maxmaxFallBagGoodsNum = 3;

            if (Global.IsRedName(otherClient))
            {
                //以下代码对实际公式做了简化,实际的掉落概率值=配置概率+INT（（当前PK值-200）/100））*配置概率
                //maxFallRoleBagRate = 10000;
                maxFallRoleBagRate = Global.GMax(0, (int)GameManager.systemParamsList.GetParamValueIntByName("MaxFallRedRoleBagRate"));
                maxFallRoleUsingRate = Global.GMax(0, (int)GameManager.systemParamsList.GetParamValueIntByName("MaxFallRedRoleUsingRate"));
                maxFallRoleBagRate *= (otherClient.ClientData.PKPoint / 100 - 1);
                maxFallRoleUsingRate *= (otherClient.ClientData.PKPoint / 100 - 1);
            }
            else
            {
                //maxFallRoleUsingRate = Global.GMax(0, (int)GameManager.systemParamsList.GetParamValueIntByName("MaxFallRoleUsingRate"));
                //maxFallRoleBagRate = Global.GMax(0, (int)GameManager.systemParamsList.GetParamValueIntByName("MaxFallRoleBagRate"));
                return; //改为非红名不掉落
            }

            List<GoodsData> goodsDataList = new List<GoodsData>();

            if (maxFallRoleBagRate > 0)
            {
                /// 获取到没有装备的物品列表
                List<GoodsData> fallGoodsList = Global.GetFallGoodsList(otherClient);

                //先处理背包中的物品掉落
                if (null != fallGoodsList && fallGoodsList.Count > 0)
                {
                    int fallBagGoodsNum = 0;
                    for (int i = 0; i < fallGoodsList.Count; i++)
                    {
                        int randNum = Global.GetRandomNumber(1, 100001);
                        if (randNum > maxFallRoleBagRate)
                        {
                            continue;
                        }

                        GoodsData goodsData = fallGoodsList[i];
                        if (null != goodsData)
                        {
                            int oldGoodsNum = 1;
                            if (Global.GetGoodsDefaultCount(goodsData.GoodsID) > 1)
                            {
                                oldGoodsNum = goodsData.GCount;
                            }

                            /// 从用户物品中扣除消耗的数量
                            if (GameManager.ClientMgr.FallRoleGoods(sl, Global._TCPManager.tcpClientPool, pool, otherClient, goodsData))
                            {
                                fallBagGoodsNum++;

                                /// 复制GoodsData对象
                                goodsData = Global.CopyGoodsData(goodsData); //因为不是整个格子全部掉落，所以要复制一个对象

                                goodsData.Id = GetNextGoodsID();
                                goodsData.GCount = oldGoodsNum;
                                goodsDataList.Add(goodsData);
                                //string goodsName = Global.GetGoodsNameByID(goodsData.GoodsID);

                                //通知对方物品爆了
                                //GameManager.ClientMgr.NotifyImportantMsg(sl, pool, otherClient,
                                //    StringUtil.substitute(Global.GetLang("很不幸，你被{0}击杀后，{1}({2})从背包中掉落了"), Global.FormatRoleName(client, client.ClientData.RoleName), goodsName, goodsData.GCount),
                                //    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                            }
                        }
                        if (fallBagGoodsNum >= maxmaxFallBagGoodsNum) //是否打到最大掉落个数
                        {
                            break;
                        }
                    }
                }
            }

            if (maxFallRoleUsingRate > 0)
            {
                /// 获取到正在装备的物品列表
                List<GoodsData> usingGoodsDataList = Global.GetUsingGoodsList(otherClient, 0);
                if (null != usingGoodsDataList && usingGoodsDataList.Count > 0)
                {
                    //maxFallRoleUsingRate = 10000; ///临时测试
                    int fallUsingGoodsNum = 0;
                    for (int i = 0; i < usingGoodsDataList.Count; i++)
                    {
                        int goodsCatetoriy = Global.GetGoodsCatetoriy(usingGoodsDataList[i].GoodsID);

                        int randNum = Global.GetRandomNumber(1, 100001);
                        int thisTimeMaxFallRoleUsingRate = maxFallRoleUsingRate;

                        //如果是神兵，神甲，那么概率要除以10
                        /*if ((int)ItemCategories.ShenBing == goodsCatetoriy || (int)ItemCategories.ShenJia == goodsCatetoriy)
                        {
                            thisTimeMaxFallRoleUsingRate = Math.Max(1, thisTimeMaxFallRoleUsingRate / 10);
                        }*/

                        if (randNum > thisTimeMaxFallRoleUsingRate)
                        {
                            continue;
                        }

                        GoodsData goodsData = usingGoodsDataList[i];
                        if (null != goodsData)
                        {
                            int oldGoodsNum = goodsData.GCount; //装备不需要特殊处理，而且特殊处理后，会导致，无法刷新旧用户身上佩戴装备的数量

                            /// 从用户物品中扣除消耗的数量
                            if (GameManager.ClientMgr.FallRoleGoods(sl, Global._TCPManager.tcpClientPool, pool, otherClient, goodsData))
                            {
                                fallUsingGoodsNum++;

                                /// 复制GoodsData对象
                                //goodsData = Global.CopyGoodsData(goodsData); //因为不是整个格子全部掉落，所以要复制一个对象
                                //上行代码会导致掉落装备的角色，无法再佩戴新装备，会说装备数量超过限制。

                                goodsData.Id = GetNextGoodsID();
                                goodsData.GCount = oldGoodsNum;
                                goodsDataList.Add(goodsData);
                                //string goodsName = Global.GetGoodsNameByID(goodsData.GoodsID);

                                //通知对方物品爆了
                                //GameManager.ClientMgr.NotifyImportantMsg(sl, pool, otherClient,
                                //    StringUtil.substitute(Global.GetLang("很不幸，你被{0}杀死后，{1}({2})从背包中掉落了"), Global.FormatRoleName(client, client.ClientData.RoleName), goodsName, goodsData.GCount),
                                //    GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

                                /// 通知更换衣服和武器代码(当物品变化时)
                                Global.NotifyChangeEquip(Global._TCPManager, pool, otherClient, goodsData, 1);

                                //刷新装备
                                goodsData.Using = 0;
                                otherClient.UsingEquipMgr.RefreshEquip(goodsData);
                            }
                        }
                        if (fallUsingGoodsNum >= maxFallUsingGoodsNum) //是否打到最大掉落个数
                        {
                            break;
                        }
                    }

                    if (fallUsingGoodsNum > 0)
                    {
                        //刷新装备(这个导致了所有的装备跑到了包裹中，最后，被掉出)
                        //otherClient.UsingEquipMgr.RefreshEquips(otherClient);

                        //重新计算装备的合成属性
                        Global.RefreshEquipProp(otherClient);
                        {
                            //通知客户端属性变化
                            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, pool, otherClient);

                            // 总生命值和魔法值变化通知(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, pool, otherClient);
                        }
                    }
                }
            }

            if (goodsDataList.Count <= 0)
            {
                return;
            }

            Point grid = otherClient.CurrentGrid;

            ///获取一个角色的物品掉路项列表
            List<GoodsPackItem> goodsPackItemList = GetRoleGoodsPackItemList(client.ClientData.RoleID, Global.FormatRoleName(client, client.ClientData.RoleName),
                goodsDataList, otherClient.ClientData.MapCode, otherClient.ClientData.CopyMapID, (int)grid.X, (int)grid.Y, enemyName);

            if (null == goodsPackItemList || goodsPackItemList.Count <= 0) //获取掉路项失败
            {
                return;
            }

            StringBuilder sb = new StringBuilder();

            //处理掉落
            for (int i = 0; i < goodsPackItemList.Count; i++)
            {
                ProcessGoodsPackItem(client, otherClient, goodsPackItemList[i], 0);

                sb.AppendFormat("{0}", Global.GetGoodsNameByID(goodsPackItemList[i].GoodsDataList[0].GoodsID));
                if (i != goodsPackItemList.Count - 1)
                {
                    sb.Append(" ");
                }
            }

            //通知客户端学习了新技能
            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                otherClient, StringUtil.substitute(Global.GetLang("很不幸，您被[{0}]杀死，掉落了{1}"), enemyName, sb.ToString()), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
        }

#endregion 处理个人掉落

#region 处理个人物品丢弃

        /// <summary>
        /// 处理角色丢弃物品
        /// </summary>
        public void ProcessRoleAbandonGoods(SocketListener sl, TCPOutPacketPool pool, GameClient client, GoodsData goodsData, int toGridX, int toGridY)
        {
            List<GoodsData> goodsDataList = new List<GoodsData>();
            goodsDataList.Add(goodsData);

            Point grid = client.CurrentGrid;

            ///获取一个角色的物品掉路项列表
            List<GoodsPackItem> goodsPackItemList = GetRoleGoodsPackItemList(client.ClientData.RoleID, Global.FormatRoleName(client, client.ClientData.RoleName),
                goodsDataList, client.ClientData.MapCode, client.ClientData.CopyMapID, (int)grid.X, (int)grid.Y, Global.FormatRoleName(client, client.ClientData.RoleName));

            if (null == goodsPackItemList || goodsPackItemList.Count <= 0) //获取掉路项失败
            {
                return;
            }

            //处理掉落
            for (int i = 0; i < goodsPackItemList.Count; i++)
            {
                ProcessGoodsPackItem(client, null, goodsPackItemList[i], 0);
            }
        }

#endregion 处理个人物品丢弃

#region 处理大乱斗物品掉落

        /// <summary>
        /// 获取一个要奖励的角色
        /// </summary>
        /// <param name="battleRoleItemList"></param>
        /// <returns></returns>
        private GameClient GetBattleRandomClient(List<BattleRoleItem> battleRoleItemList)
        {
            int randNum = Global.GetRandomNumber(0, 101);
            int maxNum = 0;
            for (int i = 0; i < battleRoleItemList.Count && i < 10; i++)
            {
                if (randNum > battleRoleItemList[i].Percent)
                {
                    break;
                }

                maxNum++;
            }

            if (maxNum < 0)
            {
                return null;
            }

            int randIndex = Global.GetRandomNumber(0, maxNum);
            return battleRoleItemList[randIndex].Client;
        }

        /// <summary>
        /// 为获胜的角色增加Buffer和标识
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bufferType"></param>
        private void AddBattleBufferAndFlags(GameClient client, int bufferType)
        {
            //Buffer参数
            double[] actionParams = new double[2];
            actionParams[0] = 60.0 * 24.0; //24个小时的buffer时间

            if (0 == bufferType) //第一名
            {
                actionParams[1] = (double)20.0;
                client.ClientData.BattleNameStart = TimeUtil.NOW();
                client.ClientData.BattleNameIndex = 1;
            }
            else if (1 == bufferType) //第二名
            {
                actionParams[1] = (double)15.0;
                client.ClientData.BattleNameStart = TimeUtil.NOW();
                client.ClientData.BattleNameIndex = 2;
            }
            else if (2 == bufferType) //第三名
            {
                actionParams[1] = (double)10.0;
                client.ClientData.BattleNameStart = TimeUtil.NOW();
                client.ClientData.BattleNameIndex = 3;
            }
            else
            {
                return;
            }

            //更新BufferData
            Global.UpdateBufferData(client, BufferItemTypes.AntiRole, actionParams);

            //异步写数据库，写入当前的角斗场称号信息
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEBATTLENAME,
                string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.BattleNameStart, client.ClientData.BattleNameIndex),
                null, client.ServerId);

            //通知角斗场称号的信息
            GameManager.ClientMgr.NotifyRoleBattleNameInfo(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            //角斗场称号次数更新
            GameManager.ClientMgr.UpdateBattleNum(client, 1, false);
        }

        /// <summary>
        /// 处理大乱斗结束时的物品掉落
        /// </summary>
        public void ProcessBattle(SocketListener sl, TCPOutPacketPool pool, List<Object> objsList, List<GoodsData> giveGoodsDataList, int fallGoodsPackID, int fallNum)
        {
            //炎黄战争战斗结束后不需要计算排名
            return;

            /*
            if (null == objsList)
            {
                return;
            }

            int totalKilledNum = 0;
            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;
                if (c.ClientData.CurrentLifeV <= 0) continue;
                totalKilledNum += c.ClientData.BattleKilledNum;
            }

            List<BattleRoleItem> battleRoleItemList = new List<BattleRoleItem>();
            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;
                if (c.ClientData.CurrentLifeV <= 0) continue;
                battleRoleItemList.Add(new BattleRoleItem()
                {
                    Client = c,
                    Percent = totalKilledNum > 0 ? ((double)c.ClientData.BattleKilledNum * 100.0 / (double)totalKilledNum) : 0.0,
                });
            }

            battleRoleItemList.Sort(delegate(BattleRoleItem x, BattleRoleItem y) { return (int)x.Percent - (int)y.Percent; });
            battleRoleItemList.Reverse();

            List<BattleEndRoleItem> battleEndRoleItemList = new List<BattleEndRoleItem>();
            for (int i = 0; i < battleRoleItemList.Count && i < 3; i++)
            {
                //为获胜的角色增加Buffer和标识
                AddBattleBufferAndFlags(battleRoleItemList[i].Client, i);

                //将前三名加入获胜队列
                battleEndRoleItemList.Add(new BattleEndRoleItem()
                {
                    RoleID = battleRoleItemList[i].Client.ClientData.RoleID,
                    RoleName = Global.FormatRoleName(battleRoleItemList[i].Client, battleRoleItemList[i].Client.ClientData.RoleName),
                    KilledNum = battleRoleItemList[i].Client.ClientData.BattleKilledNum,
                });
            }

            //通知角斗场结束时的信息
            GameManager.ClientMgr.NotifyRoleBattleEndInfo(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, 
                GameManager.BattleMgr.BattleMapCode, battleEndRoleItemList);

            return; //返回，不再处理
            */

            //记录已经处理过的客户端
            /*Dictionary<int, List<GoodsData>> alreadyGiveGoodsDict = new Dictionary<int, List<GoodsData>>();

            //先处理高级奖励物品
            // 根据物品掉落ID获取要掉落的物品
            List<GoodsData> goodsDataList = GetGoodsDataList(fallGoodsPackID, fallNum, 0);
            if (null != goodsDataList && goodsDataList.Count > 0)
            {
                for (int i = 0; i < goodsDataList.Count; i++)
                {
                    /// 获取一个要奖励的角色
                    GameClient c = GetBattleRandomClient(battleRoleItemList);
                    if (null != c)
                    {
                        List<GoodsData> clientGoodsDataList = null;
                        if (!alreadyGiveGoodsDict.TryGetValue(c.ClientData.RoleID, out clientGoodsDataList))
                        {
                            clientGoodsDataList = new List<GoodsData>();
                            alreadyGiveGoodsDict[c.ClientData.RoleID] = clientGoodsDataList;
                        }

                        clientGoodsDataList.Add(goodsDataList[i]);
                    }
                }
            }

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;
                if (c.ClientData.CurrentLifeV <= 0) continue;

                if (!Global.FilterFallGoods(c))
                {
                    continue;
                }

                List<GoodsData> clientGoodsDataList = null;
                alreadyGiveGoodsDict.TryGetValue(c.ClientData.RoleID, out clientGoodsDataList);

                /// 获取一个大乱斗中的角色的物品掉路项
                GoodsPackItem goodsPackItem = GetBattleGoodsPackItem(c.ClientData.RoleID, Global.FormatRoleName(c, c.ClientData.RoleName), fallGoodsPackID, clientGoodsDataList,
                    giveGoodsDataList, c.ClientData.MapCode, c.ClientData.CopyMapID, c.ClientData.PosX + 50, c.ClientData.PosY);
                if (null == goodsPackItem) //获取掉路项失败
                {
                    continue;
                }

                /// 自动拾取物品
                if (AutoGetThings(sl, pool, c, goodsPackItem))
                {
                    //从内存中删除包裹
                    lock (_GoodsPackDict) //先锁定
                    {
                        _GoodsPackDict.Remove(goodsPackItem.AutoID);
                    }

                    goodsPackItem = null;
                    continue; //自动拾取了，不必再处理
                }

                GameManager.MapGridMgr.DictGrids[goodsPackItem.MapCode].MoveObject(-1, -1, (int)goodsPackItem.FallPoint.X, (int)goodsPackItem.FallPoint.Y, goodsPackItem);

                //List<Object> objList = Global.GetAll9Clients(goodsPackItem.MapCode, (int)goodsPackItem.FallPoint.X, (int)goodsPackItem.FallPoint.Y, goodsPackItem.CopyMapID);
                GameManager.ClientMgr.NotifyMySelfNewGoodsPack(sl, pool, c, goodsPackItem.OwnerRoleID, goodsPackItem.OwnerRoleName,
                    goodsPackItem.AutoID, goodsPackItem.GoodsPackID,
                    goodsPackItem.MapCode,
                    c.ClientData.PosX + 50,
                    c.ClientData.PosY);
            }*/
        }

#endregion 处理大乱斗物品掉落

#region 处理角色移动时的掉落包裹发送

        /// <summary>
        /// 发送已经掉落的包裹到给自己
        /// </summary>
        /// <param name="client"></param>
        public void SendMySelfGoodsPackItems(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            GoodsPackItem goodsPackItem = null;
            for (int i = 0; i < objsList.Count && i < 30; i++)
            {
                if (!(objsList[i] is GoodsPackItem))
                {
                    continue;
                }

                goodsPackItem = objsList[i] as GoodsPackItem;

                if (goodsPackItem.GoodsDataList.Count <= 0)
                {
                    continue;
                }

                GoodsData goodsData = goodsPackItem.GoodsDataList[0];
                if (null == goodsData)
                {
                    continue;
                }

                /// 格式化队伍的字符串
                string teamRoleIDs = FormatTeamRoleIDs(goodsPackItem);

                GameManager.ClientMgr.NotifyMySelfNewGoodsPack(sl, pool, client, goodsPackItem.BelongTo <= 0 ? -1 : goodsPackItem.OwnerRoleID, goodsPackItem.OwnerRoleName,
                    goodsPackItem.AutoID, goodsPackItem.GoodsPackID,
                    goodsPackItem.MapCode,
                    (int)goodsPackItem.FallPoint.X,
                    (int)goodsPackItem.FallPoint.Y,
                    (int)goodsData.GoodsID,
                    (int)goodsData.GCount,
                    goodsPackItem.ProduceTicks,
                    goodsPackItem.TeamID,
                    teamRoleIDs,
                    goodsData.Lucky,
                    goodsData.ExcellenceInfo,
                    goodsData.AppendPropLev,
                    goodsData.Forge_level
                    );
            }
        }

        /// <summary>
        /// 删除自己哪儿掉落的包裹
        /// </summary>
        /// <param name="client"></param>
        public void DelMySelfGoodsPackItems(SocketListener sl, TCPOutPacketPool pool, GameClient client, List<Object> objsList)
        {
            if (null == objsList) return;
            GoodsPackItem goodsPackItem = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                if (!(objsList[i] is GoodsPackItem))
                {
                    continue;
                }

                goodsPackItem = objsList[i] as GoodsPackItem;

                // 修正掉落物品BUG 注意 -- 修正掉落的物品会飞向玩家的BUG [5/5/2014 LiaoWei]
                if (!CanOpenGoodsPack(goodsPackItem, client.ClientData.RoleID))
                    continue;
                
                List<GoodsData> Goodslist = null;
                Goodslist = goodsPackItem.GoodsDataList;

                if (Goodslist != null)
                {
                    for (int n = 0; n < Goodslist.Count; ++n)
                    {
                        if (!IsFallTongQianGoods(Goodslist[n].GoodsID) && !Global.CanAddGoods(client, Goodslist[n].GoodsID, Goodslist[n].GCount, Goodslist[n].Binding))
                        {
                            return;
                        }
                    }
                }

                GameManager.ClientMgr.NotifyMySelfDelGoodsPack(sl, pool, client, goodsPackItem.AutoID);

            }
        }

#endregion 处理角色移动时的掉落包裹发送

#region 处理物品掉落的超时

        /// <summary>
        /// 处理已经掉落的包裹超时
        /// </summary>
        /// <param name="client"></param>
        public void ProcessAllGoodsPackItems(SocketListener sl, TCPOutPacketPool pool)
        {
            List<GoodsPackItem> goodsPackItemList = new List<GoodsPackItem>();
            lock (_GoodsPackDict)
            {
                foreach (var val in _GoodsPackDict.Values)
                {
                    goodsPackItemList.Add(val);
                }
            }

            long nowTicks = TimeUtil.NOW();

            GoodsPackItem goodsPackItem = null;
            for (int i = 0; i < goodsPackItemList.Count; i++)
            {
                goodsPackItem = goodsPackItemList[i];

                //判断是否超过了最大的抢时间
                if (nowTicks - goodsPackItem.ProduceTicks < (Data.PackDestroyTimeTick * 1000)) //删除
                {
                    continue;
                }

                //从内存中删除包裹
                lock (_GoodsPackDict) //先锁定
                {
                    _GoodsPackDict.Remove(goodsPackItem.AutoID);
                }

                GameManager.MapGridMgr.DictGrids[goodsPackItem.MapCode].RemoveObject(goodsPackItem);

                //List<Object> objList = Global.GetAll9Clients(goodsPackItem);

                //发送包裹消失消息
                // 物品掉落消失通知(同一个地图才需要通知)
                //GameManager.ClientMgr.NotifyOthersDelGoodsPack(sl, pool, objList, goodsPackItem.MapCode, goodsPackItem.AutoID);
            }
        }

#endregion 处理物品掉落的超时

#region 处理掉落物品的点击打开

        /// <summary>
        /// 判断用户是否有打开包裹的权利
        /// </summary>
        /// <param name="goodsPackItem"></param>
        /// <param name="roleID"></param>
        /// <returns></returns>
        private bool CanOpenGoodsPack(GoodsPackItem goodsPackItem, int roleID)
        {
            if (goodsPackItem.BelongTo <= 0) //任何人都可以打开
            {
                return true;
            }

            //判断是否超过了最大的抢时间
            long nowTicks = TimeUtil.NOW();            
            if (nowTicks - goodsPackItem.ProduceTicks >= (Data.GoodsPackOvertimeTick * 1000)) //谁都可以获取
            {
                return true;
            }

            //处理掉落物品的点击
            //首先判断是否是自己的掉落物品
            if (null != goodsPackItem.TeamRoleIDs)
            {
                if (-1 != goodsPackItem.TeamRoleIDs.IndexOf(roleID))
                {
                    //如果剩余时间不为0，则可以打开
                    //long lastOpenPackTicks = 0;
                    //goodsPackItem.RolesTicksDict.TryGetValue(roleID, out lastOpenPackTicks);
                    //long packTicks = (15 * 1000) - lastOpenPackTicks;
                    //if (packTicks > 0)
                    GameClient gc = GameManager.ClientMgr.FindClient(roleID);
                    if (null != gc)
                    {
                        bool isTeamSharingMap = true;
                        if (//client.ClientData.MapCode == GameManager.BattleMgr.BattleMapCode || 
                            gc.ClientData.MapCode == GameManager.ArenaBattleMgr.BattleMapCode) //角斗场模式下，不进行组队分配--但炎黄战场允许
                        {
                            isTeamSharingMap = false;
                        }

                        //查找组队数据
                        TeamData td = GameManager.TeamMgr.FindData(gc.ClientData.TeamID);
                        if (td != null)
                        {
                            if (td.GetThingOpt > 0 && isTeamSharingMap)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            
            if (goodsPackItem.OwnerRoleID < 0 || goodsPackItem.OwnerRoleID == roleID) //是自己的或者是无主的
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 解锁掉落物品的包裹
        /// </summary>
        /// <param name="goodsPackItem"></param>
        /// <param name="client"></param>
        public void UnLockGoodsPackItem(GameClient client)
        {
            if (null == client.ClientData.LockedGoodsPackItem)
            {
                return;
            }

            lock (_GoodsPackDict)
            {
                if (_GoodsPackDict.ContainsKey(client.ClientData.LockedGoodsPackItem.AutoID))
                {
                    client.ClientData.LockedGoodsPackItem.LockedRoleID = -1;
                }
            }

            client.ClientData.LockedGoodsPackItem = null;
        }

        /// <summary>
        /// 获取剩余的物品列表
        /// </summary>
        /// <param name="goodsPackItem"></param>
        public List<GoodsData> GetLeftGoodsDataList(GoodsPackItem goodsPackItem)
        {
            if (goodsPackItem.GoodsDataList == null) return null;

            List<GoodsData> goodsDataList = new List<GoodsData>();
            for (int i = 0; i < goodsPackItem.GoodsDataList.Count; i++)
            {
                if (!goodsPackItem.GoodsIDDict.ContainsKey(goodsPackItem.GoodsDataList[i].Id))
                {
                    goodsDataList.Add(goodsPackItem.GoodsDataList[i]);
                }
            }

            return goodsDataList;
        }

        /// <summary>
        /// 不参与掷骰子分配的物品ID
        /// </summary>
        private HashSet<int> ProhibitRollHashSet;

        /// <summary>
        /// 处理组队的包裹
        /// </summary>
        /// <param name="goodsPackItem"></param>
        private void ProcessTeamGoodsPack(GameClient client, GoodsPackItem goodsPackItem)
        {
            if (null == goodsPackItem) return;

            //判断是否超过了最大的抢时间(或组队掷骰子时间)
            if ((TimeUtil.NOW() - goodsPackItem.ProduceTicks) >= (Data.GoodsPackOvertimeTick * 1000)) //判断是否还在???秒期限内
            {
                return;
            }

            if (null == goodsPackItem.TeamRoleIDs) return;
            if (goodsPackItem.GoodsIDToRolesDict.Count > 0) return; //已经分配过了
            if (null == ProhibitRollHashSet)
            {
                ProhibitRollHashSet = new HashSet<int>();
                try
                {
                    int[] ProhibitRollGoodsIDs = GameManager.systemParamsList.GetParamValueIntArrayByName("ProhibitRoll");
                    if (null != ProhibitRollGoodsIDs && ProhibitRollGoodsIDs.Length > 0)
                    {
                        foreach (var goodsID in ProhibitRollGoodsIDs)
                        {
                            ProhibitRollHashSet.Add(goodsID);
                        }
                    }
                }catch{}
            }
            for (int i = 0; i < goodsPackItem.GoodsDataList.Count; i++)
            {
                int goodsID = goodsPackItem.GoodsDataList[i].GoodsID;
                if (!ProhibitRollHashSet.Contains(goodsID))
                {
                    /// 判断物品属于组队中的那个？
                    JugeGoodsID2RoleID(client, goodsPackItem, goodsPackItem.GoodsDataList[i].Id, goodsID);
                }
            }
        }

        /// <summary>
        /// 处理掉落物品的点击打开
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="autoID"></param>
        public GoodsPackListData ProcessClickOnGoodsPack(SocketListener sl, TCPOutPacketPool pool, GameClient client, int autoID, out TCPOutPacket tcpOutPacket, int nID, int openState, bool tcpPacketData)
        {
            tcpOutPacket = null;
            int retError = 0;
            long leftTicks = 0;
            long packTicks = -1;

            //如果掉落物品不存在，或者已经被其他人锁定则返回失败
            List<GoodsData> leftGoodsDataList = null;
            GoodsPackItem goodsPackItem = null;
            lock (_GoodsPackDict)
            {
                if (_GoodsPackDict.TryGetValue(autoID, out goodsPackItem))
                {
                    if (openState > 0) //打开操作
                    {
                        // 增加判断 不要轻易的锁住物品 一定要能捡的时候  [5/6/2014 LiaoWei]
                        if (goodsPackItem != null)
                        {
                            List<GoodsData> GoodsDataList = null;
                            GoodsDataList = GetLeftGoodsDataList(goodsPackItem);

                            if (GoodsDataList != null)
                            {
                                for (int n = 0; n < GoodsDataList.Count; ++n)
                                {
                                    if (!IsFallTongQianGoods(GoodsDataList[n].GoodsID) && !Global.CanAddGoods(client, GoodsDataList[n].GoodsID, GoodsDataList[n].GCount, GoodsDataList[n].Binding))
                                    {
                                        return null;
                                    }
                                }
                            }
                        }
                        

                        /// 判断用户是否有打开包裹的权利
                        if (CanOpenGoodsPack(goodsPackItem, client.ClientData.RoleID))
                        {
                            if (-1 == goodsPackItem.LockedRoleID || goodsPackItem.LockedRoleID == client.ClientData.RoleID)
                            {
                                goodsPackItem.LockedRoleID = client.ClientData.RoleID;
                                client.ClientData.LockedGoodsPackItem = goodsPackItem;
                                leftGoodsDataList = GetLeftGoodsDataList(goodsPackItem);

                                /// 处理组队的包裹
                                ProcessTeamGoodsPack(client, goodsPackItem);

                                goodsPackItem.OpenPackTicks = TimeUtil.NOW();

                                //只有组队的才限制包裹打开的时间
                                if (null != goodsPackItem.TeamRoleIDs)
                                {
                                    long lastOpenPackTicks = 0;
                                    goodsPackItem.RolesTicksDict.TryGetValue(client.ClientData.RoleID, out lastOpenPackTicks);
                                    packTicks = (15 * 1000) - lastOpenPackTicks;
                                }
                            }
                            else
                            {
                                retError = -3;
                                goodsPackItem = null; //已经被其他人打开了，无法再打开，客户端提示用户                            
                            }
                        }
                        else
                        {
                            long nowTicks = TimeUtil.NOW();            
                            leftTicks = (Data.GoodsPackOvertimeTick * 1000) - (nowTicks - goodsPackItem.ProduceTicks);

                            if (null != goodsPackItem.TeamRoleIDs && -1 != goodsPackItem.TeamRoleIDs.IndexOf(client.ClientData.RoleID))
                            {
                                packTicks = -2;
                            }

                            retError = -2;
                            goodsPackItem = null; //你无权打开这个包裹
                        }
                    }
                    else
                    {
                        long lastOpenPackTicks = 0;
                        goodsPackItem.RolesTicksDict.TryGetValue(client.ClientData.RoleID, out lastOpenPackTicks);
                        goodsPackItem.RolesTicksDict[client.ClientData.RoleID] = lastOpenPackTicks + (TimeUtil.NOW() - goodsPackItem.OpenPackTicks);

                        goodsPackItem.LockedRoleID = -1;
                        client.ClientData.LockedGoodsPackItem = null;
                        goodsPackItem = null;
                    }
                }
                else
                {
                    retError = -1;
                }
            }

            List<GoodsData> goodsDataList = null;
            if (goodsPackItem != null)
            {
                goodsDataList = leftGoodsDataList;
            }

            GoodsPackListData goodsPackListData = new GoodsPackListData()
            {
                AutoID = autoID,
                GoodsDataList = goodsDataList,
                OpenState = openState,
                RetError = retError,
                LeftTicks = leftTicks,
                PackTicks = packTicks,
            };

            if (tcpPacketData)
            {
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<GoodsPackListData>(goodsPackListData, pool, nID);
            }

            return goodsPackListData;
        }

#endregion 处理掉落物品的点击打开

#region 处理掉落物品的获取

        /// <summary>
        /// 是否是包裹的锁定者, 如果是返回包裹对象
        /// </summary>
        /// <param name="client"></param>
        /// <param name="autoID"></param>
        /// <returns></returns>
        private GoodsPackItem GetLockedGoodsPackItem(GameClient client, int autoID)
        {
            GoodsPackItem goodsPackItem = null;
            if (!_GoodsPackDict.TryGetValue(autoID, out goodsPackItem))
            {
                return null;
            }

            if (goodsPackItem.LockedRoleID != client.ClientData.RoleID)
            {
                return null;
            }

            return goodsPackItem;
        }

        /// <summary>
        /// 处理掉落物品的获取
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="autoID"></param>
        public void ProcessGetThing(SocketListener sl, TCPOutPacketPool pool, GameClient client, int autoID, int goodsDbID, out bool bRet)
        {
            bRet = true;
            string killedMonsterName = "";
            List<GoodsData> goodsDataList = null;
            GoodsPackItem goodsPackItem = null;

            lock (_GoodsPackDict)
            {
                goodsPackItem = GetLockedGoodsPackItem(client, autoID);
                if (null == goodsPackItem) //不处理
                {
                    return;
                }

                killedMonsterName = goodsPackItem.KilledMonsterName;
                goodsDataList = new List<GoodsData>();

                if (-1 == goodsDbID) //获取所有物品
                {
                    for (int i = 0; i < goodsPackItem.GoodsDataList.Count; i++)
                    {
                        if (goodsPackItem.GoodsIDDict.ContainsKey(goodsPackItem.GoodsDataList[i].Id)) //已经被获取了
                        {
                            continue;
                        }

                        goodsDataList.Add(goodsPackItem.GoodsDataList[i]);
                    }
                }
                else if (!goodsPackItem.GoodsIDDict.ContainsKey(goodsDbID))
                {
                    for (int i = 0; i < goodsPackItem.GoodsDataList.Count; i++)
                    {
                        if (goodsPackItem.GoodsDataList[i].Id == goodsDbID)
                        {
                            goodsDataList.Add(goodsPackItem.GoodsDataList[i]);
                            break;
                        }
                    }
                }
            }

            if (null == goodsDataList || goodsDataList.Count <= 0)
            {
                return;
            }

            GameClient gc = null;
            for (int i = 0; i < goodsDataList.Count; i++)
            {
                int toRoleID = FindGoodsID2RoleID(goodsPackItem, goodsDataList[i].Id);
                if (-1 == toRoleID) //属于当前的点击者
                {
                    gc = client;
                }
                else
                {
                    //属于所有者
                    gc = GameManager.ClientMgr.FindClient(toRoleID);
                }

                if (null == gc)
                {
                    continue;
                }

                // 属性改造 去掉 负重[8/15/2013 LiaoWei]
                //是否能得到物品(判断包裹负重是否足够)
                /*if (!Global.CanAddGoodsWeight(gc, goodsDataList[i]) && !IsFallTongQianGoods(goodsDataList[i].GoodsID))
                {
                    /// 通知在线的对方(不限制地图)个人紧要消息
                    GameManager.ClientMgr.NotifyImportantMsg(sl, pool, gc, Global.GetLang("背包负重不足，请先清理后再拾取物品"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoBagGrid);
                    break;
                }*/

                if (Global.CanAddGoods(gc, goodsDataList[i].GoodsID, 1, goodsDataList[i].Binding) || IsFallTongQianGoods(goodsDataList[i].GoodsID))
                {
                    lock (_GoodsPackDict) //先锁定
                    {
                        goodsPackItem.GoodsIDDict[goodsDataList[i].Id] = true;
                    }

                    //先判断是否是铜钱
                    if (!ProcessFallTongQian(pool, gc, goodsDataList[i].GoodsID, goodsDataList[i].GCount, goodsPackItem.FallLevel) && Global.CanAddGoodsNum(gc, goodsDataList[i].GCount))
                    {
                        //添加物品
                        int nRet = Global.AddGoodsDBCommand_Hook(pool, gc, goodsDataList[i].GoodsID, goodsDataList[i].GCount, goodsDataList[i].Quality, goodsDataList[i].Props, goodsDataList[i].Forge_level, goodsDataList[i].Binding, 0, goodsDataList[i].Jewellist, true, 1, "杀怪掉落后手动拾取", true, goodsDataList[i].Endtime, goodsDataList[i].AddPropIndex, goodsDataList[i].BornIndex, goodsDataList[i].Lucky, goodsDataList[i].Strong, goodsDataList[i].ExcellenceInfo, goodsDataList[i].AppendPropLev, goodsDataList[i].ChangeLifeLevForEquip, true);// 卓越信息 [12/13/2013 LiaoWei]
                        if (0 == nRet)
                        {
                            GameManager.logDBCmdMgr.AddDBLogInfo(/*Convert.ToInt32(goodsDataList[i].Id)*/-1, Global.ModifyGoodsLogName(goodsDataList[i]), "杀怪掉落后手动拾取", Global.GetMapName(client.ClientData.MapCode), "系统", "销毁", goodsDataList[i].GCount, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId, goodsDataList[i]);
                        }
                    }

                    //物品拾取通知
                    GameManager.ClientMgr.NotifySelfGetThing(sl, pool, gc, goodsDbID);

                    // 七日活动
                    SevenDayGoalEventObject evObj = SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.PickUpEquipCount);
                    evObj.Arg1 = goodsDataList[i].GoodsID;
                    evObj.Arg2 = goodsDataList[i].GCount;
                    GlobalEventSource.getInstance().fireEvent(evObj);

                }
                else
                {
                    /// 通知在线的对方(不限制地图)个人紧要消息
                    // 由于一直给客户端发提示消息 现在注释掉 由客户端自己判断背包满了  [12/31/2013 LiaoWei]
                    //GameManager.ClientMgr.NotifyImportantMsg(sl, pool, gc, Global.GetLang("背包已满,建议您将闲置的物品村存放到勇者大陆[仓库管理员]处"), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoBagGrid);

                    bRet = false;
                    break;
                }
            }

            bool nothing = false;
            lock (_GoodsPackDict) //先锁定
            {
                nothing = (goodsPackItem.GoodsIDDict.Count >= goodsPackItem.GoodsDataList.Count);
            }

            //如果物品已经没有了
            if (nothing)
            {
                //从内存中删除包裹
                lock (_GoodsPackDict) //先锁定
                {
                    _GoodsPackDict.Remove(autoID);
                }

                GameManager.MapGridMgr.DictGrids[goodsPackItem.MapCode].RemoveObject(goodsPackItem);

                //立即发送通知消息，删除已经捡取的包裹（此处如果交给九宫格处理延迟，会导致抢包裹时的误会）
                List<Object> objsList = Global.GetAll9Clients(goodsPackItem);

                //发送包裹消失消息
                // 物品掉落消失通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersDelGoodsPack(sl, pool, objsList, client.ClientData.MapCode, autoID, client.ClientData.RoleID);
            }
        }

        /// <summary>
        /// 删除地图格子上的包裹并通知其他人(管理器里已经删除)
        /// </summary>
        /// <param name="goodsPackItem"></param>
        public void ExternalRemoveGoodsPack(GoodsPackItem goodsPackItem)
        {
            GameManager.MapGridMgr.DictGrids[goodsPackItem.MapCode].RemoveObject(goodsPackItem);

            //立即发送通知消息，删除已经捡取的包裹（此处如果交给九宫格处理延迟，会导致抢包裹时的误会）
            List<Object> objsList = Global.GetAll9Clients(goodsPackItem);

            //发送包裹消失消息
            // 物品掉落消失通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersDelGoodsPack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, objsList, goodsPackItem.MapCode, goodsPackItem.AutoID, -1);
        }

#endregion 处理掉落物品的获取

#region 处理外部获取掉落ID相关队列的操作

        /// <summary>
        /// 根据物品掉落ID获取掉落列表
        /// </summary>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        public List<FallGoodsItem> GetFallGoodsItemList(int goodsPackID)
        {
            List<FallGoodsItem> fallGoodsItemList = GetNormalFallGoodsItem(goodsPackID);
            if (null == fallGoodsItemList)
            {
                return null;
            }

            return fallGoodsItemList;
        }

        /// <summary>
        /// 根据物品掉落ID获取掉落列表
        /// </summary>
        /// <param name="goodsPackID"></param>
        /// <returns></returns>
        public List<FallGoodsItem> GetRandomFallGoodsItemList(int goodsPackID, int maxFallCount, bool isGood)
        {
            List<FallGoodsItem> fallGoodsItemList = GetNormalFallGoodsItem(goodsPackID);
            if (null == fallGoodsItemList)
            {
                return null;
            }

            List<FallGoodsItem> randFallGoodsItemList = Global.RandomSortList<FallGoodsItem>(fallGoodsItemList);
            if (maxFallCount > 0)
            {
                while (randFallGoodsItemList.Count > maxFallCount)
                {
                    randFallGoodsItemList.RemoveAt(randFallGoodsItemList.Count - 1); //删除多余
                }
            }

            for (int i = 0; i < fallGoodsItemList.Count; i++)
            {
                fallGoodsItemList[i].IsGood = isGood;
            }

            return randFallGoodsItemList;
        }

        /// <summary>
        /// 从掉落表获取物品表
        /// </summary>
        /// <param name="fallGoodsItemList"></param>
        /// <returns></returns>
        public List<GoodsData> GetGoodsDataListFromFallGoodsItemList(List<FallGoodsItem> fallGoodsItemList)
        {
            if (null == fallGoodsItemList || fallGoodsItemList.Count <= 0) return null;

            List<GoodsData> goodsDataList = new List<GoodsData>();
            for (int i = 0; i < fallGoodsItemList.Count; i++)
            {
                // 根据物品掉落品质ID获取掉落的物品的配置
                int goodsQualtiy = 0; //GetFallGoodsQuality(fallGoodsItemList[i].FallQualityID);

                // 根据物品掉落级别获取掉落的物品的级别配置
                int goodsLevel = GetFallGoodsLevel(fallGoodsItemList[i].FallLevelID);

                // 根据物品掉落级别获取掉落的物品的天生配置
                int goodsBornIndex = 0;

                //根据物品追加ID别获取掉落的物品的幸运属性配置
                int luckyRate = GetLuckyGoodsID(fallGoodsItemList[i].LuckyRate);

                //根据物品追加ID别获取掉落的物品的追加属性配置
                int appendPropLev = GetZhuiJiaGoodsLevelID(fallGoodsItemList[i].ZhuiJiaID);

                //根据物品追加ID别获取掉落的物品的卓越属性配置
                int excellenceInfo = GetExcellencePropertysID(fallGoodsItemList[i].ExcellencePropertyID);

                // 根据物品品质获取随机属性字符串(废弃)
                string props = "";

                GoodsData goodsData = new GoodsData()
                {
                    Id = i,
                    GoodsID = fallGoodsItemList[i].GoodsID,
                    Using = 0,
                    Forge_level = goodsLevel,
                    Starttime = "1900-01-01 12:00:00",
                    Endtime = Global.ConstGoodsEndTime,
                    Site = 0,
                    Quality = goodsQualtiy,
                    Props = props,
                    GCount = 1,
                    Binding = fallGoodsItemList[i].Binding,
                    Jewellist = "",
                    BagIndex = 0,
                    AddPropIndex = 0,
                    BornIndex = goodsBornIndex,
                    Lucky = luckyRate,
                    Strong = 0,
                    ExcellenceInfo = excellenceInfo,
                    AppendPropLev = appendPropLev,
                    ChangeLifeLevForEquip = 0,
                };

                goodsDataList.Add(goodsData);
            }

            return goodsDataList;
        }

#endregion 处理外部获取掉落ID相关队列的操作

#region 移动停止时获取掉落的包裹

        /// <summary>
        /// 根据位置获取掉落的包裹
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private GoodsPackItem FindGoodsPackItemByPos(Point grid, GameClient gameClient)
        {
            /*lock (_GoodsPackDict)
            {
                foreach (var GoodsPackItem in _GoodsPackDict.Values)
                {
                    //同一个地图才进一步判断
                    if (GoodsPackItem.MapCode == gameClient.ClientData.MapCode)
                    {
                        //如果玩家在副本里面，则物品副本地图ID 必须一致才判断
                        if (gameClient.ClientData.CopyMapID >= 0 && GoodsPackItem.CopyMapID >= 0
                            && gameClient.ClientData.CopyMapID != GoodsPackItem.CopyMapID)
                        {
                            continue;
                        }

                        Point goodsPackGrid = GoodsPackItem.CurrentGrid;
                        if ((int)goodsPackGrid.X == (int)grid.X &&
                            (int)goodsPackGrid.Y == (int)grid.Y)
                        {
                            return GoodsPackItem;
                        }
                    }
                }
            }*/

            MapGrid mapGrid = null;
            if (!GameManager.MapGridMgr.DictGrids.TryGetValue(gameClient.ClientData.MapCode, out mapGrid))
            {
                return null;
            }

            if (null == mapGrid)
            {
                return null;
            }

            /// 获取指定格子中的对象列表
            List<Object> objsList = mapGrid.FindObjects((int)grid.X, (int)grid.Y);
            if (null != objsList)
            {
                for (int objIndex = 0; objIndex < objsList.Count; objIndex++)
                {
                    if (objsList[objIndex] is GoodsPackItem)
                    {
                        if (gameClient.ClientData.CopyMapID > 0)
                        {
                            if ((objsList[objIndex] as GoodsPackItem).CopyMapID == gameClient.ClientData.CopyMapID)
                            {
                                return objsList[objIndex] as GoodsPackItem;
                            }
                        }
                        else
                        {
                            return objsList[objIndex] as GoodsPackItem;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 移动停止时拾取包裹
        /// </summary>
        /// <param name="client"></param>
        public void ProcessClickGoodsPackWhenMovingEnd(GameClient client)
        {
            //根据位置获取掉落的包裹
            GoodsPackItem goodsPackItem = FindGoodsPackItemByPos(client.CurrentGrid, client);
            if (null == goodsPackItem)
            {
                return;
            }

            TCPOutPacket tcpOutPacket = null;

            try
            {
                /// 处理掉落物品的点击打开
                GoodsPackListData goodsPackListData = GameManager.GoodsPackMgr.ProcessClickOnGoodsPack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, goodsPackItem.AutoID, out tcpOutPacket, (int)TCPGameServerCmds.CMD_SPR_CLICKONGOODSPACK, 1, false);
                if (null != goodsPackListData)
                {
                    if (0 == goodsPackListData.RetError) //正常的打开了包裹
                    {
                        /// 处理掉落物品的获取
                        // 完善逻辑 [5/5/2014 LiaoWei]
                        bool bRet = true;

                        GameManager.GoodsPackMgr.ProcessGetThing(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, goodsPackItem.AutoID, -1, out bRet);

                        if (!bRet)
                            return;

                        /// 获取掉落物品时的日志
                        TakeFallGoodsRecords(goodsPackItem, client);

                        goodsPackListData.GoodsDataList = null;

                        TCPOutPacket tcpOutPacket2 = DataHelper.ObjectToTCPOutPacket<GoodsPackListData>(goodsPackListData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_CLICKONGOODSPACK);
                        if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket2))
                        {
                            //
                            /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                tcpOutPacket2.PacketCmdID,
                                tcpOutPacket2.PacketDataSize,
                                client.ClientData.RoleID,
                                client.ClientData.RoleName));*/
                        }

                        UnLockGoodsPackItem(client);
                    }
                    else if (goodsPackListData.RetError == -1)
                    {
                        GameManager.GoodsPackMgr.ExternalRemoveGoodsPack(goodsPackItem);
                    }
                    else //错误处理
                    {
                        goodsPackListData.GoodsDataList = null;

                        TCPOutPacket tcpOutPacket2 = DataHelper.ObjectToTCPOutPacket<GoodsPackListData>(goodsPackListData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_CLICKONGOODSPACK);
                        if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket2))
                        {
                            //
                            /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                tcpOutPacket2.PacketCmdID,
                                tcpOutPacket2.PacketDataSize,
                                client.ClientData.RoleID,
                                client.ClientData.RoleName));*/
                        }
                    }
                }
            }
            finally
            {
            }
        }

#endregion 移动停止时获取掉落的包裹

#region 移动时捡取物品的操作（MU）

        /// <summary>
        /// 拾取字典缓存
        /// </summary>
        private Dictionary<int, Dictionary<int, bool>> _CacheShiQuGoodsDict = new Dictionary<int, Dictionary<int, bool>>();
        
        /// <summary>
        /// 初始化拾取物品缓存字典
        /// </summary>
        private void InitShiQuGoodsList()
        {
            lock (_CacheShiQuGoodsDict)
            {
                if (_CacheShiQuGoodsDict.Count > 0)
                {
                    return;
                }

                string str = GameManager.systemParamsList.GetParamValueByName("ShiQuGoodsList");
                if (string.IsNullOrEmpty(str))
                {
                    return;
                }

                string[] fields1 = str.Split('|');
                for (int i = 0; i < fields1.Length; i++)
                {
                    string[] fields2 = fields1[i].Split(',');
                    Dictionary<int, bool> dict = new Dictionary<int, bool>();
                    for (int j = 0; j < fields2.Length; j++)
                    {
                        dict[Global.SafeConvertToInt32(fields2[j])] = true;
                    }

                    _CacheShiQuGoodsDict[i] = dict;
                }
            }
        }

        /// <summary>
        /// 获取要拾取的物品的捡取类型
        /// </summary>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        private int GetPickUpShiQuGoodsType(int goodsID)
        {
            lock (_CacheShiQuGoodsDict)
            {
                foreach (var key in _CacheShiQuGoodsDict.Keys)
                {
                    Dictionary<int, bool> dict = _CacheShiQuGoodsDict[key];
                    if (dict.ContainsKey(goodsID))
                    {
                        return key;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 是否是在自动挂机，如果是是否拾取物品
        /// </summary>
        /// <param name="goodsPackItem"></param>
        private bool CanPickUpGoodss(GameClient client, GoodsPackItem goodsPackItem)
        {
            //判断是否开启了自动拾取功能
            if (client.ClientData.AutoFightGetThings == 0)
            {
                return false;
            }

            if (goodsPackItem.GoodsDataList.Count <= 0)
            {
                return false;
            }

            //初始化拾取物品缓存字典
            InitShiQuGoodsList();

            GoodsData goodsData = goodsPackItem.GoodsDataList[0];

            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out systemGoods) || null == systemGoods)
            {
                LogManager.WriteLog(LogTypes.Warning, string.Format("处理拾取物品到背包时，获取物品xml信息失败: GoodsID={0}", goodsData.GoodsID));
                return false; //获取物品信息失败，配置错误？？？？
            }

            // 检测包裹 [5/5/2014 LiaoWei]
            if (!IsFallTongQianGoods(goodsData.GoodsID) && !Global.CanAddGoods(client, goodsData.GoodsID, goodsData.GCount, goodsData.Binding))
            {
                return false;
            }

            int bitVal = 0;
            int categoriy = systemGoods.GetIntValue("Categoriy");
            if (categoriy >= (int)ItemCategories.TouKui && categoriy < (int)ItemCategories.EquipMax) //如果是装备
            {
                int color = Global.GetEquipColor(goodsData);

                bitVal = Global.GetIntSomeBit(client.ClientData.AutoFightGetThings, color - 1);
                if (1 == bitVal)
                {
                    return true;
                }

                return false;
            }
            else //物品拾取
            {
                int shiquGoodsType = GetPickUpShiQuGoodsType(goodsData.GoodsID);

                bitVal = Global.GetIntSomeBit(client.ClientData.AutoFightGetThings, (int)GetThingsIndexes.BaoShi);
                if (1 == bitVal)
                {
                    if (0 == shiquGoodsType)
                    {
                        return true;
                    }
                }

                bitVal = Global.GetIntSomeBit(client.ClientData.AutoFightGetThings, (int)GetThingsIndexes.YuMao);
                if (1 == bitVal)
                {
                    if (1 == shiquGoodsType)
                    {
                        return true;
                    }
                }

                bitVal = Global.GetIntSomeBit(client.ClientData.AutoFightGetThings, (int)GetThingsIndexes.YaoPin);
                if (1 == bitVal)
                {
                    if (2 == shiquGoodsType)
                    {
                        return true;
                    }
                }

                bitVal = Global.GetIntSomeBit(client.ClientData.AutoFightGetThings, (int)GetThingsIndexes.JinBi);
                if (1 == bitVal)
                {
                    if (3 == shiquGoodsType)
                    {
                        return true;
                    }
                }

                bitVal = Global.GetIntSomeBit(client.ClientData.AutoFightGetThings, (int)GetThingsIndexes.MenPiaoCaiLiao);
                if (1 == bitVal)
                {
                    if (4 == shiquGoodsType)
                    {
                        return true;
                    }
                }

                bitVal = Global.GetIntSomeBit(client.ClientData.AutoFightGetThings, (int)GetThingsIndexes.QiTaDaoJu); //其他道具
                if (1 == bitVal)
                {
                    if (-1 == shiquGoodsType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 根据位置获取掉落的包裹
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private List<GoodsPackItem> FindGoodsPackItemListByPos(Point grid, int girdNum, GameClient gameClient)
        {
            MapGrid mapGrid = null;
            if (!GameManager.MapGridMgr.DictGrids.TryGetValue(gameClient.ClientData.MapCode, out mapGrid))
            {
                return null;
            }

            if (null == mapGrid)
            {
                return null;
            }

            int startGridX = (int)grid.X - girdNum;
            int endGridX = (int)grid.X + girdNum;
            int startGridY = (int)grid.Y - girdNum;
            int endGridY = (int)grid.Y + girdNum;

            startGridX = Global.GMax(startGridX, 0);
            startGridY = Global.GMax(startGridY, 0);
            endGridX = Global.GMin(endGridX, mapGrid.MapGridXNum - 1);
            endGridY = Global.GMin(endGridY, mapGrid.MapGridYNum - 1);

            List<GoodsPackItem> GoodsPackItemList = new List<GoodsPackItem>();

            for (int gridX = startGridX; gridX <= endGridX; gridX++)
            {
                for (int gridY = startGridY; gridY <= endGridY; gridY++)
                {
                    /// 获取指定格子中的对象列表
                    List<Object> objsList = mapGrid.FindGoodsPackItems((int)gridX, (int)gridY);
                    if (null != objsList)
                    {
                        for (int objIndex = 0; objIndex < objsList.Count; objIndex++)
                        {
                            if (objsList[objIndex] is GoodsPackItem)
                            {
                                /// 判断用户是否有打开包裹的权利
                                if (!CanOpenGoodsPack(objsList[objIndex] as GoodsPackItem, gameClient.ClientData.RoleID))
                                {
                                    continue; //不能打开的就不捡取
                                }

                                /// 是否拾取物品
                                if (!CanPickUpGoodss(gameClient, objsList[objIndex] as GoodsPackItem))
                                {
                                    continue;
                                }

                                if (gameClient.ClientData.CopyMapID > 0)
                                {
                                    if ((objsList[objIndex] as GoodsPackItem).CopyMapID == gameClient.ClientData.CopyMapID)
                                    {
                                        GoodsPackItemList.Add(objsList[objIndex] as GoodsPackItem);
                                    }
                                }
                                else
                                {
                                    GoodsPackItemList.Add(objsList[objIndex] as GoodsPackItem);
                                }
                            }
                        }
                    }
                }
            }

            return GoodsPackItemList;
        }

        /// <summary>
        /// 移动到另外一个格子时拾取包裹
        /// </summary>
        /// <param name="client"></param>
        public void ProcessClickGoodsPackWhenMovingToOtherGrid(GameClient client, int gridNum = 1)
        {
            //根据位置获取掉落的包裹
            List<GoodsPackItem> goodsPackItemList = FindGoodsPackItemListByPos(client.CurrentGrid, gridNum, client);
            if (null == goodsPackItemList || goodsPackItemList.Count <= 0)
            {
                return;
            }

            lock (client.ClientData.PickUpGoodsPackMutex)
            {
                for (int i = 0; i < goodsPackItemList.Count; i++)
                {
                    GoodsPackItem goodsPackItem = goodsPackItemList[i];

                    TCPOutPacket tcpOutPacket = null;

                    try
                    {
                        /// 处理掉落物品的点击打开
                        GoodsPackListData goodsPackListData = GameManager.GoodsPackMgr.ProcessClickOnGoodsPack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, goodsPackItem.AutoID, out tcpOutPacket, (int)TCPGameServerCmds.CMD_SPR_CLICKONGOODSPACK, 1, false);
                        if (null != goodsPackListData)
                        {
                            if (0 == goodsPackListData.RetError) //正常的打开了包裹
                            {
                                /// 处理掉落物品的获取
                                // 完善逻辑 [5/5/2014 LiaoWei]
                                bool bRet = true;

                                GameManager.GoodsPackMgr.ProcessGetThing(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, goodsPackItem.AutoID, -1, out bRet);

                                if (!bRet)
                                    return;

                                /// 获取掉落物品时的日志
                                TakeFallGoodsRecords(goodsPackItem, client);

                                goodsPackListData.GoodsDataList = null;

                                TCPOutPacket tcpOutPacket2 = DataHelper.ObjectToTCPOutPacket<GoodsPackListData>(goodsPackListData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_CLICKONGOODSPACK);
                                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket2))
                                {
                                    //
                                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                        tcpOutPacket2.PacketCmdID,
                                        tcpOutPacket2.PacketDataSize,
                                        client.ClientData.RoleID,
                                        client.ClientData.RoleName));*/
                                }

                                UnLockGoodsPackItem(client);
                            }
                            else if (goodsPackListData.RetError == -1)
                            {
                                GameManager.GoodsPackMgr.ExternalRemoveGoodsPack(goodsPackItem);
                            }
                            else //错误处理
                            {
                                goodsPackListData.GoodsDataList = null;

                                TCPOutPacket tcpOutPacket2 = DataHelper.ObjectToTCPOutPacket<GoodsPackListData>(goodsPackListData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_CLICKONGOODSPACK);
                                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket2))
                                {
                                    //
                                    /*LogManager.WriteLog(LogTypes.Error, string.Format("向用户发送tcp数据失败: ID={0}, Size={1}, RoleID={2}, RoleName={3}",
                                        tcpOutPacket2.PacketCmdID,
                                        tcpOutPacket2.PacketDataSize,
                                        client.ClientData.RoleID,
                                        client.ClientData.RoleName));*/
                                }
                            }
                        }
                    }
                    finally
                    {
                    }
                }
            }
        }

#endregion 移动时捡取物品的操作（MU）

#region 物品掉落或者拾取时的日志记录

        /// <summary>
        /// 记录掉落物品时的日志
        /// </summary>
        /// <param name="goodsPackItem"></param>
        private void WriteFallGoodsRecords(GoodsPackItem goodsPackItem)
        {
            GoodsData goodsData = goodsPackItem.GoodsDataList[0];

            //判断物品是否播报或者记录
            SystemXmlItem systemGoods = Global.CanBroadcastOrEventGoods(goodsData.GoodsID);
            if (null == systemGoods) return;

            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDFALLGOODSITEM,
                string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}",
                goodsPackItem.OwnerRoleID, goodsPackItem.AutoID, -1, goodsData.GoodsID, goodsData.GCount, goodsData.Binding, goodsData.Quality, goodsData.Forge_level, goodsData.Jewellist, Global.GetMapName(goodsPackItem.MapCode), goodsPackItem.CurrentGrid.ToString(), goodsPackItem.KilledMonsterName),
                null, GameManager.LocalServerIdForNotImplement);
        }

        /// <summary>
        /// 获取掉落物品时的日志
        /// </summary>
        /// <param name="goodsPackItem"></param>
        private void TakeFallGoodsRecords(GoodsPackItem goodsPackItem, GameClient client)
        {
            GoodsData goodsData = goodsPackItem.GoodsDataList[0];

            //判断物品是否播报或者记录
            SystemXmlItem systemGoods = Global.CanBroadcastOrEventGoods(goodsData.GoodsID);
            if (null == systemGoods) return;

            GameManager.logDBCmdMgr.AddDBLogInfo(goodsData.Id, Global.ModifyGoodsLogName(goodsData), "拾取物品", Global.GetMapName(client.ClientData.MapCode), client.ClientData.RoleName, "增加", goodsData.GCount, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId, goodsData);

            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDFALLGOODSITEM,
                string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}",
                client.ClientData.RoleID, goodsPackItem.AutoID, 1, goodsData.GoodsID, goodsData.GCount, goodsData.Binding, goodsData.Quality, goodsData.Forge_level, goodsData.Jewellist, Global.GetMapName(goodsPackItem.MapCode), goodsPackItem.CurrentGrid.ToString(), goodsPackItem.KilledMonsterName),
                null, client.ServerId);
        }

#endregion 物品掉落或者拾取时的日志记录
    }
}
