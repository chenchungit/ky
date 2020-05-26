#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Threading;
//using System.Windows.Threading;
using Server.Data;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using System.IO;
using ProtoBuf;
using Server.TCP;
using Server.Tools;
using GameServer.Logic.JingJiChang;
using Tmsk.Contract;
//using System.Windows.Forms;

namespace GameServer.Logic
{
    /// <summary>
    /// 等待操作的怪物区域项
    /// </summary>
    public class MonsterZoneQueueItem
    {
        /// <summary>
        /// 副本地图ID
        /// </summary>
        public int CopyMapID = 0;

        /// <summary>
        /// 一次召唤几个
        /// </summary>
        public int BirthCount = 0;

        /// <summary>
        /// 怪物区域对象
        /// </summary>
        public MonsterZone MyMonsterZone = null;

        /// <summary>
        /// 种子怪物，用于动态刷该--->动态召唤怪物
        /// </summary>
        public Monster seedMonster = null;

        /// <summary>
        /// 依次是刷怪位置 和 半径
        /// </summary>
        public int ToX = 0;
        public int ToY = 0;
        public int Radius = 0;

        /// <summary>
        /// 动态的追击范围
        /// </summary>
        public int PursuitRadius = 0;


        /// <summary>
        /// 逻辑模块自定义的标记对象,用于动态刷怪时添加自定义的信息,供模块内部使用
        /// </summary>
        public object Tag;

        /// <summary>
        /// 模块管理器的类型,用于动态刷怪时设置类型信息,分辨由那个管理器管理
        /// </summary>
        public SceneUIClasses ManagerType = SceneUIClasses.Normal;
    };

    /// <summary>
    /// 地图爆怪区域管理类
    /// </summary>
    public class MonsterZoneManager
    {
        public MonsterZoneManager()
        {
            for (int i = 0; i < WaitingAddDynamicMonsterQueue.Length; i++)
            {
                if (null == WaitingAddDynamicMonsterQueue[i])
                {
                    WaitingAddDynamicMonsterQueue[i] = new Queue<MonsterZoneQueueItem>();
                }
            }
        }

        #region 基础属性

        /// <summary>
        /// 单次处理的最大数量
        /// </summary>
        public static int MaxRunQueueNum = 100;

        /// <summary>
        /// 最大等待的数量
        /// </summary>
        public static int MaxWaitingRunQueueNum = 200;

        /// <summary>
        /// 动态刷怪的单次处理最大数量
        /// </summary>
        public static int MaxRunAddDynamicMonstersQueueNum = 30;

        /// <summary>
        /// 动态爆怪的区域列表
        /// </summary>
        private Dictionary<int, MonsterZone> MonsterDynamicZoneDict = new Dictionary<int, MonsterZone>(100);

        /// <summary>
        /// 总的爆怪的区域列表
        /// </summary>
        private List<MonsterZone> MonsterZoneList = new List<MonsterZone>(100);

        /// <summary>
        /// 副本地图的的区域列表
        /// </summary>
        private List<MonsterZone> FuBenMonsterZoneList = new List<MonsterZone>(100);

        /// <summary>
        /// 根据地图编号存取爆怪的区域字典
        /// </summary>
        private Dictionary<int, List<MonsterZone>> Map2MonsterZoneDict = new Dictionary<int, List<MonsterZone>>(100);

        /// <summary>
        /// 等待爆副本怪的队列
        /// </summary>
        private Queue<MonsterZoneQueueItem> WaitingAddFuBenMonsterQueue = new Queue<MonsterZoneQueueItem>();

        /// <summary>
        /// 等待删除副本怪的队列
        /// </summary>
        private Queue<MonsterZoneQueueItem> WaitingDestroyFuBenMonsterQueue = new Queue<MonsterZoneQueueItem>();

        /// <summary>
        /// 等待立刻刷新副本怪的队列
        /// </summary>
        private Queue<MonsterZoneQueueItem> WaitingReloadFuBenMonsterQueue = new Queue<MonsterZoneQueueItem>();

        /// <summary>
        /// 等待立刻刷新普通地图怪的队列
        /// </summary>
        private Queue<MonsterZoneQueueItem> WaitingReloadNormalMapMonsterQueue = new Queue<MonsterZoneQueueItem>();

        // 分组
        private const int Max_WaitingAddDynamicMonsterQueneCount = 10;

        /// <summary>
        /// 等待立刻生成地图怪的队列---》用于动态召唤怪物--->不管是副本地图还说普通地图都一样
        /// </summary>
        private Queue<MonsterZoneQueueItem>[] WaitingAddDynamicMonsterQueue = new Queue<MonsterZoneQueueItem>[Max_WaitingAddDynamicMonsterQueneCount];

        /// <summary>
        /// 等待立刻刷新竞技场假人的队列
        /// </summary>
        private Queue<MonsterZoneQueueItem> WaitingReloadRobotQueue = new Queue<MonsterZoneQueueItem>();

        /// <summary>
        /// 动态怪物种子,用于动态刷怪
        /// </summary>
        private Dictionary<int, Monster> _DictDynamicMonsterSeed = new Dictionary<int, Monster>();

        /// <summary>
        /// 动态怪物种子,用于动态刷怪
        /// </summary>
        public Dictionary<int, Monster> DictDynamicMonsterSeed
        {
            get;
            set;
        }

        //这个变量cache了Config/Monsters.xml 不要直接引用，要使用下面的get和set来保证线程安全
        private XElement _allMonstersXml = null;
        private Object _allMonsterXmlMutex = new Object();

        public XElement AllMonstersXml
        {
            get
            {
                lock (_allMonsterXmlMutex)
                {
                    return _allMonstersXml;
                }
            }
            set
            {
                lock (_allMonsterXmlMutex)
                {
                    _allMonstersXml = value;
                }
            }
        }

        // 程序启动和Reload的时候调用
        public void LoadAllMonsterXml()
        {
            XElement tmpXml = null;
            try
            {
                string Url = Global.GameResPath("Config/Monsters.xml");
                SysConOut.WriteLine(Url);
                tmpXml = XElement.Load(Url);
            }
            catch (Exception ex)
            {
            }

            if (tmpXml != null)
            {
                this.AllMonstersXml = tmpXml;
            }
        }

        #endregion 基础属性

        #region 程序加载时初始化怪物

        private object InitMonsterZoneMutex = new object();

        /// <summary>
        /// 加入爆怪区域
        /// </summary>
        /// <param name="monsterZone"></param>
        private void AddMap2MonsterZoneDict(MonsterZone monsterZone)
        {
            List<MonsterZone> monsterZoneList = null;
            if (Map2MonsterZoneDict.TryGetValue(monsterZone.MapCode, out monsterZoneList))
            {
                monsterZoneList.Add(monsterZone);
                return;
            }

            monsterZoneList = new List<MonsterZone>();
            Map2MonsterZoneDict[monsterZone.MapCode] = monsterZoneList;
            monsterZoneList.Add(monsterZone);
        }

        /// <summary>
        /// 获取爆怪的时间点
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private List<BirthTimePoint> ParseBirthTimePoints(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            string[] fields = s.Split('|');
            if (fields.Length <= 0) return null;

            List<BirthTimePoint> list = new List<BirthTimePoint>();
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.IsNullOrEmpty(fields[i]))
                {
                    continue;
                }

                string[] fields2 = fields[i].Split(':');
                if (fields2.Length != 2) continue;

                string str1 = fields2[0].TrimStart('0');
                string str2 = fields2[1].TrimStart('0');
                BirthTimePoint birthTimePoint = new BirthTimePoint()
                {
                    BirthHour = Global.SafeConvertToInt32(str1),
                    BirthMinute = Global.SafeConvertToInt32(str2),
                };

                list.Add(birthTimePoint);
            }

            return list.Count > 0 ? list : null;
        }

        /// <summary>
        /// 加载怪物
        /// </summary>
        public void AddMapMonsters(int mapCode, GameMap gameMap)
        {
            //对于每一个地图，都要加入一个动态刷怪区域，用于动态刷怪管理
            AddDynamicMonsterZone(mapCode);

            string fileName = string.Format("Map/{0}/Monsters.xml", mapCode);
            XElement xml = null;

            try
            {
                xml = XElement.Load(Global.ResPath(fileName));
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载地图怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            IEnumerable<XElement> monsterItems = xml.Elements("Monsters").Elements();
            if (null == monsterItems) return;

            //判断是否是副本地图
            bool isFuBenMap = FuBenManager.IsFuBenMap(mapCode);

            foreach (var monsterItem in monsterItems)
            {
                String timePoints = Global.GetSafeAttributeStr(monsterItem, "TimePoints");
                int configBirthType = (int)Global.GetSafeAttributeLong(monsterItem, "BirthType");
                int realBirthType = configBirthType;

                String realTimePoints = timePoints;
                int spawnMonstersAfterKaiFuDays = 0;
                int spawnMonstersDays = 0;
                List<BirthTimeForDayOfWeek> CreateMonstersDayOfWeek = new List<BirthTimeForDayOfWeek>();
                List<BirthTimePoint> birthTimePointList = null;

                //对于开服多少天之后才开始刷怪，进行特殊配置 格式:开服多少天;连续刷多少天[负数0表示一直];刷怪方式0或1;0或1的配置
                if ((int)MonsterBirthTypes.AfterKaiFuDays == configBirthType || (int)MonsterBirthTypes.AfterHeFuDays == configBirthType || (int)MonsterBirthTypes.AfterJieRiDays == configBirthType)
                {
                    String[] arr = timePoints.Split(';');
                    if (4 != arr.Length)
                    {
                        throw new Exception(String.Format("地图{0}的类型4的刷怪配置参数个数不对!!!!", mapCode));
                    }

                    spawnMonstersAfterKaiFuDays = int.Parse(arr[0]);
                    spawnMonstersDays = int.Parse(arr[1]);
                    realBirthType = int.Parse(arr[2]);
                    realTimePoints = arr[3];

                    if ((int)MonsterBirthTypes.TimePoint != realBirthType && (int)MonsterBirthTypes.TimeSpan != realBirthType)
                    {
                        throw new Exception(String.Format("地图{0}的类型4的刷怪配置子类型不对!!!!", mapCode));
                    }
                }

                // MU新增 一周中的哪天刷 TimePoints 配置形式 周几,时间点|周几,时间点|周几,时间点... [1/10/2014 LiaoWei]
                if ((int)MonsterBirthTypes.CreateDayOfWeek == configBirthType)
                {
                    String[] arrTime = timePoints.Split('|');

                    if (arrTime.Length > 0)
                    {
                        for (int nIndex = 0; nIndex < arrTime.Length; ++nIndex)
                        {
                            string sTimePoint = null;
                            sTimePoint = arrTime[nIndex];

                            if (sTimePoint != null)
                            {
                                String[] sTime = null;
                                sTime = sTimePoint.Split(',');

                                if (sTime != null && sTime.Length == 2)
                                {
                                    string sTimeString  = null;
                                    int nDayOfWeek      = -1;

                                    nDayOfWeek = int.Parse(sTime[0]);
                                    sTimeString = sTime[1];

                                    if (nDayOfWeek != -1 && !string.IsNullOrEmpty(sTimeString))
                                    {
                                        string[] fields2 = sTimeString.Split(':');
                                        if (fields2.Length != 2) continue;

                                        string str1 = fields2[0].TrimStart('0');
                                        string str2 = fields2[1].TrimStart('0');

                                        BirthTimePoint birthTimePoint = new BirthTimePoint()
                                        {
                                            BirthHour = Global.SafeConvertToInt32(str1),
                                            BirthMinute = Global.SafeConvertToInt32(str2),
                                        };

                                        BirthTimeForDayOfWeek BirthTimeTmp = new BirthTimeForDayOfWeek();

                                        BirthTimeTmp.BirthDayOfWeek = nDayOfWeek;
                                        BirthTimeTmp.BirthTime      = birthTimePoint;

                                        CreateMonstersDayOfWeek.Add(BirthTimeTmp);
                                    }
                                }
                            }

                        }
                    }
                }
                else
                {
                    birthTimePointList = ParseBirthTimePoints(realTimePoints);
                }

                MonsterZone monsterZone = new MonsterZone()
                {
                    MapCode = mapCode,
                    ID = (int)Global.GetSafeAttributeLong(monsterItem, "ID"),
                    Code = (int)Global.GetSafeAttributeLong(monsterItem, "Code"),
                    ToX = (int)Global.GetSafeAttributeLong(monsterItem, "X") / gameMap.MapGridWidth,
                    ToY = (int)Global.GetSafeAttributeLong(monsterItem, "Y") / gameMap.MapGridHeight,
                    Radius = (int)Global.GetSafeAttributeLong(monsterItem, "Radius") / gameMap.MapGridWidth,
                    TotalNum = (int)Global.GetSafeAttributeLong(monsterItem, "Num"),
                    Timeslot = (int)Global.GetSafeAttributeLong(monsterItem, "Timeslot"),
                    IsFuBenMap = isFuBenMap,
                    BirthType = realBirthType,
                    ConfigBirthType = configBirthType,
                    SpawnMonstersAfterKaiFuDays = spawnMonstersAfterKaiFuDays,
                    SpawnMonstersDays = spawnMonstersDays,
                    SpawnMonstersDayOfWeek = CreateMonstersDayOfWeek,
                    BirthTimePointList = birthTimePointList,
                    BirthRate = (int)(Global.GetSafeAttributeDouble(monsterItem, "BirthRate") * 10000),
                };

                XAttribute attrib = monsterItem.Attribute("PursuitRadius");
                if (null != attrib)
                {
                    monsterZone.PursuitRadius = (int)Global.GetSafeAttributeLong(monsterItem, "PursuitRadius");
                }
                else
                {
                    monsterZone.PursuitRadius = (int)Global.GetSafeAttributeLong(monsterItem, "Radius");
                }

                lock (InitMonsterZoneMutex)
                {
                    //加入列表
                    MonsterZoneList.Add(monsterZone);

                    //如果是副本地图, 则加入副本爆怪区域列表
                    if (isFuBenMap)
                    {
                        FuBenMonsterZoneList.Add(monsterZone);
                    }

                    //加入爆怪区域
                    AddMap2MonsterZoneDict(monsterZone);
                }

                //加载静态的怪物信息
                monsterZone.LoadStaticMonsterInfo();

                //加载怪物
                monsterZone.LoadMonsters();//暂时屏蔽怪物加载
            }
        }

        #endregion 程序加载时初始化怪物

        #region 定时怪物复活调度

        /// <summary>
        /// 定时怪物复活调度(主线程中调用)
        /// </summary>
        public void RunMapMonsters(SocketListener sl, TCPOutPacketPool pool)
        {
            for ( int i = 0; i < MonsterZoneList.Count; i++)
            {
                //根据当前的怪剩余个数，重新爆怪
                MonsterZoneList[i].ReloadMonsters(sl, pool);
            }

            // 这会让MonsterZone中的MonsterList被清空 它是用于怪复活的 副本怪的销毁 交给ProcessEndCopyMap(副本时间到了就会销毁) [8/20/2014 LiaoWei]
            for (int i = 0; i < FuBenMonsterZoneList.Count; i++)
            {
                //遍历判断副本地图怪是否需要销毁(主线程调用)
                FuBenMonsterZoneList[i].DestroyDeadMonsters();
            }

            //for (int i = 0; i < MonsterDynamicZoneDict.Values.Count; i++)
            List<MonsterZone> monsterZoneList = MonsterDynamicZoneDict.Values.ToList<MonsterZone>();
            for (int i = 0; i < monsterZoneList.Count; i++)
            {
                //执行销毁动态生成的怪物(主线程调用)
                monsterZoneList[i].DestroyDeadDynamicMonsters();
            }
        }

        public void RunMapDynamicMonsters(SocketListener sl, TCPOutPacketPool pool)
        {
            //执行添加动态的副本怪物
            for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
            {
                if (!RunAddCopyMapMonsters())
                {
                    break;
                }
            }

            //执行销毁动态的副本怪物
            for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
            {
                if (!RunDestroyCopyMapMonsters())
                {
                    break;
                }
            }

            //执行立刻刷新副本怪物的操作
            for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
            {
                if (!RunReloadCopyMapMonsters())
                {
                    break;
                }
            }

            //执行立刻刷新普通地图怪物的操作
            for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
            {
                if (!RunReloadNormalMapMonsters())
                {
                    break;
                }
            }

            /*//执行动态召唤怪物操作
            for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
            {
                if (!RunAddDynamicMonsters())
                {
                    break;
                }
            }*/

            //执行立刻刷新竞技场机器人的刷新
            for (int i = 0; i < MonsterZoneManager.MaxRunAddDynamicMonstersQueueNum; i++)
            {
                if (!RunAddRobots())
                {
                    break;
                }
            }

            // 最多循环100次
            int Count = 0;
            int loop_Count = 0;
            while (Count < MaxRunAddDynamicMonstersQueueNum)
            {
                for (int i = 0; i < Max_WaitingAddDynamicMonsterQueneCount; i++ )
                {
                    loop_Count++;
                    if (RunAddDynamicMonsters(i))
                    {
                        Count++;
                    }
                }
                if (loop_Count >= MonsterZoneManager.MaxRunQueueNum)
                {
                    break;
                }
            }
        }

        #endregion 定时怪物复活调度

        #region 副本地图中的动态刷怪

        /// <summary>
        /// 等待执行添加动态的副本怪物的队列
        /// </summary>
        /// <returns></returns>
        public int WaitingAddFuBenMonsterQueueCount()
        {
            //等待爆副本怪的队列
            lock (WaitingAddFuBenMonsterQueue) //线程锁
            {
                return WaitingAddFuBenMonsterQueue.Count;
            }
        }

        /// <summary>
        /// 执行添加动态的副本怪物
        /// </summary>
        private bool RunAddCopyMapMonsters()
        {
            MonsterZoneQueueItem monsterZoneQueueItem = null;

            //等待爆副本怪的队列
            lock (WaitingAddFuBenMonsterQueue) //线程锁
            {
                if (WaitingAddFuBenMonsterQueue.Count > 0)
                {
                    monsterZoneQueueItem = WaitingAddFuBenMonsterQueue.Dequeue();
                }
            }
                
            if (null != monsterZoneQueueItem)
            {
                monsterZoneQueueItem.MyMonsterZone.LoadCopyMapMonsters(monsterZoneQueueItem.CopyMapID);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 执行销毁动态的副本怪物
        /// </summary>
        /// <returns></returns>
        public int WaitingDestroyFuBenMonsterQueueCount()
        {
            //等待爆副本怪的队列
            lock (WaitingDestroyFuBenMonsterQueue) //线程锁
            {
                return WaitingDestroyFuBenMonsterQueue.Count;
            }
        }

        /// <summary>
        /// 执行销毁动态的副本怪物
        /// </summary>
        private bool RunDestroyCopyMapMonsters()
        {
            MonsterZoneQueueItem monsterZoneQueueItem = null;

            //等待删除副本怪的队列
            lock (WaitingDestroyFuBenMonsterQueue) //线程锁
            {
                if (WaitingDestroyFuBenMonsterQueue.Count > 0)
                {
                    monsterZoneQueueItem = WaitingDestroyFuBenMonsterQueue.Dequeue();
                }
            }

            if (null != monsterZoneQueueItem)
            {
                monsterZoneQueueItem.MyMonsterZone.ClearCopyMapMonsters(monsterZoneQueueItem.CopyMapID);
                return true;
            }

            return false;

        }

        /// <summary>
        /// 执行立刻刷新副本怪物的操作
        /// </summary>
        /// <returns></returns>
        public int WaitingReloadFuBenMonsterQueueCount()
        {
            //等待爆副本怪的队列
            lock (WaitingReloadFuBenMonsterQueue) //线程锁
            {
                return WaitingReloadFuBenMonsterQueue.Count;
            }
        }

        /// <summary>
        /// 执行立刻刷新副本怪物的操作----->1.必须是副本， 2.副本内部的怪物死亡，进行复活, 3.不会增加任何新怪物
        /// 相当于 RunTryToReliveCopyMapMonsters() //尝试运行复活副本怪物
        /// </summary>
        private bool RunReloadCopyMapMonsters()
        {
            MonsterZoneQueueItem monsterZoneQueueItem = null;

            //等待刷新副本怪的队列
            lock (WaitingReloadFuBenMonsterQueue) //线程锁
            {
                if (WaitingReloadFuBenMonsterQueue.Count > 0)
                {
                    monsterZoneQueueItem = WaitingReloadFuBenMonsterQueue.Dequeue();
                }
            }

            if (null != monsterZoneQueueItem)
            {
                monsterZoneQueueItem.MyMonsterZone.ReloadCopyMapMonsters(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monsterZoneQueueItem.CopyMapID);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据地图编号和副本地图编号，执行动态刷怪物的操作(会在线程中调用)
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMap"></param>
        public void AddCopyMapMonsters(int mapCode, int copyMapID)
        {
            List<MonsterZone> monsterZoneList = null;
            if (!Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
            {
                return;
            }

            for (int i = 0; i < monsterZoneList.Count; i++)
            {
                //等待爆副本怪的队列
                lock (WaitingAddFuBenMonsterQueue) //线程锁
                {
                    WaitingAddFuBenMonsterQueue.Enqueue(new MonsterZoneQueueItem()
                    {
                        CopyMapID = copyMapID,
                        BirthCount = 0,
                        MyMonsterZone = monsterZoneList[i],
                    });
                }
            }
        }

        /// <summary>
        /// 根据地图编号和副本地图编号，执行销毁怪物的操作(会在线程中调用)
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMap"></param>
        public void DestroyCopyMapMonsters(int mapCode, int copyMapID)
        {
            List<MonsterZone> monsterZoneList = null;
            if (!Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
            {
                return;
            }

            for (int i = 0; i < monsterZoneList.Count; i++)
            {
                //等待删除副本怪的队列
                lock (WaitingDestroyFuBenMonsterQueue) //线程锁
                {
                    WaitingDestroyFuBenMonsterQueue.Enqueue(new MonsterZoneQueueItem()
                    {
                        CopyMapID = copyMapID,
                        BirthCount = 0,
                        MyMonsterZone = monsterZoneList[i],
                    });
                }
            }
        }

        /// <summary>
        /// 根据地图编号和副本地图编号，执行立刻刷新副本怪物的的操作(会在线程中调用)
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMap"></param>
        public void ReloadCopyMapMonsters(int mapCode, int copyMapID)
        {
            List<MonsterZone> monsterZoneList = null;
            if (!Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
            {
                return;
            }

            for (int i = 0; i < monsterZoneList.Count; i++)
            {
                //等待删除副本怪的队列
                lock (WaitingReloadFuBenMonsterQueue) //线程锁
                {
                    WaitingReloadFuBenMonsterQueue.Enqueue(new MonsterZoneQueueItem()
                    {
                        CopyMapID = copyMapID,
                        BirthCount = 0,
                        MyMonsterZone = monsterZoneList[i],
                    });
                }
            }

        }

        #endregion 副本地图中的动态刷怪

        #region 副本中总的怪物个数获取

        /// <summary>
        /// 获取副本地图中的总的怪物的个数 excludePets为true 表示排除 玩家的宠物怪【类型DSPetMonster】
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMapID"></param>
        /// <param name="monsterType"></param>
        /// <returns></returns>
        public int GetMapTotalMonsterNum(int mapCode, MonsterTypes monsterType, Boolean excludePets = true)
        {
            List<MonsterZone> monsterZoneList = null;
            if (!Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
            {
                return 0;
            }

            int totalNum = 0;
            for (int i = 0; i < monsterZoneList.Count; i++)
            {
                if (MonsterTypes.None != monsterType) //None表示获取所有的怪物
                {
                    if (monsterZoneList[i].MonsterType != monsterType)
                    {
                        continue;
                    }
                }

                //动态刷怪区域刷出的怪，都是属于玩家的怪，玩家可以带走并穿越地图
                if (excludePets && monsterZoneList[i].IsDynamicZone())
                {
                    continue;
                }

                totalNum += monsterZoneList[i].TotalNum;
            }

            return totalNum;
        }

        /// <summary>
        /// 获取副本地图中指定的怪物的个数
        /// </summary>
        public int GetMapMonsterNum(int mapCode, int nMonsterID)
        {
            List<MonsterZone> monsterZoneList = null;
            if (!Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
            {
                return 0;
            }

            int nCount = 0;
            for (int i = 0; i < monsterZoneList.Count; i++)
            {
#if ___CC___FUCK___YOU___BB___
                XMonsterStaticInfo monsterInfo = null;
                monsterInfo = monsterZoneList[i].GetMonsterInfo();
                if (monsterInfo == null)
                    continue;

                if (monsterInfo.MonsterId == nMonsterID)
                    nCount += monsterZoneList[i].TotalNum;
#else
                MonsterStaticInfo monsterInfo = null;
                monsterInfo = monsterZoneList[i].GetMonsterInfo();
                 if (monsterInfo == null)
                    continue;

                if (monsterInfo.ExtensionID == nMonsterID)
                    nCount += monsterZoneList[i].TotalNum;
#endif


            }

            return nCount;
        }

        public bool GetMonsterBirthPoint(int mapCode, int nMonsterID, out int posX, out int posY, out int radis)
        {
            posX = 0;
            posY = 0;
            radis = 0;
            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out gameMap))
                return false;

            List<MonsterZone> monsterZoneList = null;
            if (!Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
            {
                return false;
            }

            for (int i = 0; i < monsterZoneList.Count; i++)
            {
                MonsterZone zone = monsterZoneList[i];
                if (zone.Code == nMonsterID)
                {
                    Point p = Global.GridToPixel(mapCode, zone.ToX, zone.ToY);
                    posX = (int)p.X;
                    posY = (int)p.Y;

                    return true;
                 //   Radius = (int)Global.GetSafeAttributeLong(monsterItem, "Radius") / gameMap.MapGridWidth,
                }
            }

            return false;
        }

#endregion 副本中总的怪物个数获取

#region 普通地图召唤怪物

        /// <summary>
        /// 执行立刻刷新普通地图怪物的操作
        /// </summary>
        private bool RunReloadNormalMapMonsters()
        {
            MonsterZoneQueueItem monsterZoneQueueItem = null;

            //等待刷新副本怪的队列
            lock (WaitingReloadNormalMapMonsterQueue) //线程锁
            {
                if (WaitingReloadNormalMapMonsterQueue.Count > 0)
                {
                    monsterZoneQueueItem = WaitingReloadNormalMapMonsterQueue.Dequeue();
                }
            }

            if (null != monsterZoneQueueItem)
            {
                monsterZoneQueueItem.MyMonsterZone.ReloadNormalMapMonsters(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monsterZoneQueueItem.BirthCount);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据地图编号，执行立刻刷新怪物的的操作(会在线程中调用)
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMap"></param>
        public void ReloadNormalMapMonsters(int mapCode, int birthCount)
        {
            List<MonsterZone> monsterZoneList = null;
            if (!Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
            {
                return;
            }

            for (int i = 0; i < monsterZoneList.Count; i++)
            {
                //等待删除普通地图怪的队列
                lock (WaitingReloadNormalMapMonsterQueue) //线程锁
                {
                    WaitingReloadNormalMapMonsterQueue.Enqueue(new MonsterZoneQueueItem()
                    {
                        CopyMapID = -1,
                        BirthCount = birthCount,
                        MyMonsterZone = monsterZoneList[i],
                    });
                }
            }
        }

#endregion 普通地图召唤怪物

#region 动态刷怪

        /// <summary>
        /// 返回动态刷怪区域
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public MonsterZone GetDynamicMonsterZone(int mapCode)
        {
            MonsterZone zone = null;
            if (MonsterDynamicZoneDict.TryGetValue(mapCode, out zone))
            {
                return zone;
            }

            return null;
        }

        /// <summary>
        /// 添加动态爆怪区域，每个地图有一个,所有参数都动态生成
        /// </summary>
        /// <param name="mapCode"></param>
        public void AddDynamicMonsterZone(int mapCode)
        {
            //判断是否是副本地图
            bool isFuBenMap = FuBenManager.IsFuBenMap(mapCode);

            MonsterZone monsterZone = new MonsterZone()
            {
                MapCode = mapCode,
                ID = MonsterDynamicZoneDict.Count + 10000, //动态配置 爆怪的区域ID，这个ID这样生成就可以
                Code = -1,//动态配置---区域怪物ID，需要生成的时候动态配置
                ToX = -1,//动态配置
                ToY = -1,//动态配置
                Radius = 300,//可以动态修改
                TotalNum = 0,//动态
                Timeslot = 1,
                IsFuBenMap = isFuBenMap,
                //意味着该区域的怪不管是在副本还说非副本，都由外部代码动态控制怪物的加载，怪物不会被程序定期循环复活
                //同时该区域的怪物一旦死亡，直接销毁【将来或许需要加入一个针对这种类型的最大复活次数？】
                BirthType = (int)MonsterBirthTypes.CrossMap,//这个字段非常重要，用于区分是动态刷怪区域还是 旧的系统怪区域!!!!!!
                ConfigBirthType = -1,//配置文件可没配置
                BirthTimePointList = null,
                BirthRate = 10000,//必爆
            };

            //追踪范围，动态配置
            monsterZone.PursuitRadius = 0;

            lock (InitMonsterZoneMutex)
            {
                //动态爆怪区域列表--->里面的怪物一旦死亡，移除
                MonsterDynamicZoneDict.Add(mapCode, monsterZone);

                //加入列表
                MonsterZoneList.Add(monsterZone);

                //如果是副本地图, 则加入副本爆怪区域列表
                if (isFuBenMap)
                {
                    FuBenMonsterZoneList.Add(monsterZone);
                }

                //加入爆怪区域
                AddMap2MonsterZoneDict(monsterZone);
            }
        }

        /// <summary>
        /// 初始化动态召唤怪物的缓存
        /// </summary>
        /// <param name="monsterID"></param>
        private void InitDynamicMonsterSeedByMonserID(int monsterID)
        {
            MonsterZone monsterZone = new MonsterZone();

            Monster myMonster = null;

            //如果原来有，就不要再次加载了，重用就好,相当于增量加载
            if (_DictDynamicMonsterSeed.TryGetValue(monsterID, out myMonster) && null != myMonster)
            {
                return;
            }

            int ID = 1;
            lock (_DictDynamicMonsterSeed)
            {
                ID = _DictDynamicMonsterSeed.Count + 1;
            }

            monsterZone.MapCode = 1;//强行设置地图编号 后期进程允许过程中动态修改
            monsterZone.ID = ID;
            monsterZone.Code = monsterID;

            //加载静态的怪物信息
            monsterZone.LoadStaticMonsterInfo();

            //加载怪物
            myMonster = monsterZone.LoadDynamicMonsterSeed();

            lock (_DictDynamicMonsterSeed)
            {
                //不存在就加入
                if (!_DictDynamicMonsterSeed.ContainsKey(monsterID))
                {
                    _DictDynamicMonsterSeed.Add(monsterID, myMonster);
                }
            }
        }

        /// <summary>
        /// 初始化动态刷该用的怪物种子
        /// </summary>
        /*public void InitDynamicMonsterSeed()
        {
            //动态怪物配置文件
            string fileName = string.Format("Config/DynamicMonsters.xml");
            XElement xml = null;

            try
            {
                xml = XElement.Load(Global.GameResPath(fileName));
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载地图怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            IEnumerable<XElement> monsterItems = xml.Elements("Monsters").Elements();
            if (null == monsterItems) return;

            MonsterZone monsterZone = new MonsterZone();

            //临时容器
            Dictionary<int, Monster> lsMonster = new Dictionary<int, Monster>();

            //循环加载怪物种子---有的字段不需要，先不管
            foreach (var monsterItem in monsterItems)
            {
                int monsterID = (int)Global.GetSafeAttributeLong(monsterItem, "Code");

                Monster myMonster = null;

                //如果原来有，就不要再次加载了，重用就好,相当于增量加载
                if (!_DictDynamicMonsterSeed.TryGetValue(monsterID, out myMonster) || null == myMonster)
                {
                    monsterZone.MapCode = 1;//强行设置地图编号 后期进程允许过程中动态修改
                    monsterZone.ID = (int)Global.GetSafeAttributeLong(monsterItem, "ID");
                    monsterZone.Code = monsterID;

                    //加载怪物
                    myMonster = monsterZone.LoadDynamicMonsterSeed();
                }

                //不存在就加入
                if (!lsMonster.ContainsKey(monsterID))
                {
                    lsMonster.Add(monsterID, myMonster);
                }
            }

            //替换赋值
            _DictDynamicMonsterSeed = lsMonster;

        }*/

        /// <summary>
        /// 返回动态刷怪需要的怪物种子
        /// </summary>
        /// <param name="monsterID"></param>
        /// <returns></returns>
        private Monster GetDynamicMonsterSeed(int monsterID)
        {
            Monster monster = null;

            lock (_DictDynamicMonsterSeed)
            {
                if (_DictDynamicMonsterSeed.TryGetValue(monsterID, out monster))
                {
                    return monster;
                }
            }

            try
            {
                //重新初始化一下 由于这儿是动态加载，所以需要try 起来，避免异常
                //InitDynamicMonsterSeed();
                InitDynamicMonsterSeedByMonserID(monsterID);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "InitDynamicMonsterSeed()", false);
            }

            lock (_DictDynamicMonsterSeed)
            {
                //再尝试一次
                _DictDynamicMonsterSeed.TryGetValue(monsterID, out monster);
            }

            return monster;
        }

        /// <summary>
        /// 添加任意地图的动态怪物到生成队列 radius 是格子坐标范围，不是像素点坐标范围！！！！
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMap"></param>
        public void AddDynamicRobot(int mapCode, Robot robot, int copyMapID = -1, int addNum = 1, int gridX = 0, int gridY = 0, int radius = 3, int pursuitRadius = 0, SceneUIClasses managerType = SceneUIClasses.Normal, object tag = null)
        {
            //先打印下怪物日志，便于调试
            TraceAllDynamicMonsters();

            MonsterZone monsterZone = null;
            if (!MonsterDynamicZoneDict.TryGetValue(mapCode, out monsterZone))
            {
                return;
            }

            robot.MonsterZoneNode = monsterZone;

            //等待爆副本怪的队列
            lock (WaitingReloadRobotQueue) //线程锁
            {
                WaitingReloadRobotQueue.Enqueue(new MonsterZoneQueueItem()
                {
                    CopyMapID = copyMapID,
                    BirthCount = addNum,
                    MyMonsterZone = monsterZone,
                    seedMonster = robot,
                    ToX = gridX,
                    ToY = gridY,
                    Radius = radius,
                    PursuitRadius = pursuitRadius,
                    Tag = tag,
                    ManagerType = managerType,
                });
            }
        }

        /// <summary>
        /// 添加任意地图的动态怪物到生成队列 radius 是格子坐标范围，不是像素点坐标范围！！！！
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMap"></param>
        /// <returns>怪物种子对象,并非实际的怪物对象</returns>
        public Monster AddDynamicMonsters(int mapCode, int monsterID, int copyMapID = -1, int addNum = 1, int gridX = 0, int gridY = 0, int radius = 3,
            int pursuitRadius = 0, SceneUIClasses managerType = SceneUIClasses.Normal, object tag = null)
        {
            //先打印下怪物日志，便于调试
            TraceAllDynamicMonsters();

            MonsterZone monsterZone = null;
            if (!MonsterDynamicZoneDict.TryGetValue(mapCode, out monsterZone))
            {
                return null;
            }

            Monster seedMonster = GetDynamicMonsterSeed(monsterID);
            if (null == seedMonster)
            {
                return null;
            }

            int index = 0;
            if (copyMapID >= 0)
            {
                index = Global.Clamp(copyMapID % Max_WaitingAddDynamicMonsterQueneCount, 0, Max_WaitingAddDynamicMonsterQueneCount - 1);
            }
            else
            {
                index = Global.Clamp(mapCode % Max_WaitingAddDynamicMonsterQueneCount, 0, Max_WaitingAddDynamicMonsterQueneCount - 1);
            }

            //等待爆副本怪的队列
            lock (WaitingAddDynamicMonsterQueue) //线程锁
            {
                WaitingAddDynamicMonsterQueue[index].Enqueue(new MonsterZoneQueueItem()
                {
                    CopyMapID = copyMapID,
                    BirthCount = addNum,
                    MyMonsterZone = monsterZone,
                    seedMonster = seedMonster,
                    ToX = gridX,
                    ToY = gridY,
                    Radius = radius,
                    PursuitRadius = pursuitRadius,
                    Tag = tag,
                    ManagerType = managerType,
                });
            }

            return seedMonster;
        }

        /// <summary>
        /// 添加任意地图的动态怪物到生成队列[角色采用召唤的方式动态生成宠物怪]
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMap"></param>
        public Boolean CallDynamicMonstersOwnedByRole(GameClient client, int monsterID, int callAsType = (int)MonsterTypes.Pet, int callNum = 1, int pursuitRadius = 0)
        {
            //先打印下怪物日志，便于调试
            TraceAllDynamicMonsters();

            int mapCode = client.ClientData.MapCode;
            int copyMapID = client.ClientData.CopyMapID;
            Point grid = client.CurrentGrid;
            int gridX = (int)grid.X, gridY = (int)grid.Y;
            int radius = 3;

            MonsterZone monsterZone = null;
            if (!MonsterDynamicZoneDict.TryGetValue(mapCode, out monsterZone))
            {
                return false;
            }

            Monster seedMonster = GetDynamicMonsterSeed(monsterID);
            if (null == seedMonster)
            {
                return false;
            }

            Monster realSeedMonster = seedMonster.Clone();

            //更改怪物类型，通过这个参数将怪物转换为非野外怪物
            realSeedMonster.MonsterType = callAsType;

            //更改怪物所有者，这样怪物接受该client的指令控制,同时，总跟着client跑
            realSeedMonster.OwnerClient = client;


           

            int index = 0;
            if (client.ClientData.CopyMapID >= 0)
            {
                index = Global.Clamp(client.ClientData.CopyMapID % Max_WaitingAddDynamicMonsterQueneCount, 0, Max_WaitingAddDynamicMonsterQueneCount - 1);
            }
            else
            {
                index = Global.Clamp(client.ClientData.MapCode % Max_WaitingAddDynamicMonsterQueneCount, 0, Max_WaitingAddDynamicMonsterQueneCount - 1);
            }

            //等待爆副本怪的队列
            lock (WaitingAddDynamicMonsterQueue) //线程锁
            {
                WaitingAddDynamicMonsterQueue[index].Enqueue(new MonsterZoneQueueItem()
                {
                    CopyMapID = copyMapID,
                    BirthCount = callNum,
                    MyMonsterZone = monsterZone,
                    seedMonster = realSeedMonster,
                    ToX = gridX,
                    ToY = gridY,
                    Radius = radius,
                    PursuitRadius = pursuitRadius,
                });
            }

            return true;
        }

        /// <summary>
        /// 执行添加任意地图的动态怪物
        /// </summary>
        private bool RunAddDynamicMonsters(int index)
        {
            if (index < 0 || index >= Max_WaitingAddDynamicMonsterQueneCount)
                return false;

            MonsterZoneQueueItem monsterZoneQueueItem = null;

            //等待爆副本怪的队列
            lock (WaitingAddDynamicMonsterQueue) //线程锁
            {
                if (WaitingAddDynamicMonsterQueue[index].Count > 0)
                {
                    monsterZoneQueueItem = WaitingAddDynamicMonsterQueue[index].Dequeue();
                }
            }

            if (null != monsterZoneQueueItem)
            {
                //SysConOut.WriteLine(string.Format("monsterZoneQueueItem.CopyMapID={0}, monsterZoneQueueItem.ManagerType={1}", monsterZoneQueueItem.CopyMapID, monsterZoneQueueItem.ManagerType));
                //LogManager.WriteLog(LogTypes.Error, string.Format("monsterZoneQueueItem.CopyMapID={0}, monsterZoneQueueItem.ManagerType={1}", monsterZoneQueueItem.CopyMapID, monsterZoneQueueItem.ManagerType));

                //动态生成怪物
                monsterZoneQueueItem.MyMonsterZone.LoadDynamicMonsters(monsterZoneQueueItem);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 执行添加竞技场机器人
        /// </summary>
        private bool RunAddRobots()
        {
            MonsterZoneQueueItem monsterZoneQueueItem = null;

            //等待爆副本怪的队列
            lock (WaitingReloadRobotQueue) //线程锁
            {
                if (WaitingReloadRobotQueue.Count > 0)
                {
                    monsterZoneQueueItem = WaitingReloadRobotQueue.Dequeue();
                }
            }

            if (null != monsterZoneQueueItem)
            {
                //动态生成怪物
                monsterZoneQueueItem.MyMonsterZone.LoadDynamicRobot(monsterZoneQueueItem);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 动态输出所有的动态怪物信息，用于调试
        /// </summary>
        protected void TraceAllDynamicMonsters()
        {
            // 注释掉 引起了多线程问题 [1/6/2014 LiaoWei]
            /*foreach (var zone in MonsterDynamicZoneDict.Values)
            {
                String sInfo = zone.GetMonstersInfoString();

                if (sInfo.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine(sInfo);
                }
            }*/
        }

#endregion 动态刷怪

#region 外部调用接口

        /// <summary>
        /// 根据地图编号获取爆怪区域列表
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public List<MonsterZone> GetMonsterZoneListByMapCode(int mapCode)
        {
            List<MonsterZone> list = null;
            Map2MonsterZoneDict.TryGetValue(mapCode, out list);
            return list;
        }

        /// <summary>
        /// 根据地图编号获取爆怪区域列表
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public List<MonsterZone> GetMonsterZoneByMapCodeAndMonsterID(int mapCode, int monsterID)
        {
            List<MonsterZone> list2 = new List<MonsterZone>();
            List<MonsterZone> list = GetMonsterZoneListByMapCode(mapCode);
            if (null == list) return list2;

            for (int i = 0; i < list.Count; i++)
            {
                if (monsterID == list[i].Code)
                {
                    list2.Add(list[i]);
                }
            }

            return list2;
        }

        /// <summary>
        /// 根据地图编号获取爆怪区域列表
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public Point GetMonsterPointByMapCodeAndMonsterID(int mapCode, int monsterID)
        {
            Point pt = new Point(-1, -1);
            List<MonsterZone> monsterZoneList = GetMonsterZoneByMapCodeAndMonsterID(mapCode, monsterID);
            if (null == monsterZoneList || monsterZoneList.Count <= 0)
            {
                return pt;
            }

            List<Point> ptList = new List<Point>();
            for (int i = 0; i < monsterZoneList.Count; i++)
            {
                Point targetPoint = Global.GetMapPointByGridXY(ObjectTypes.OT_CLIENT, monsterZoneList[i].MapCode, monsterZoneList[i].ToX, monsterZoneList[i].ToY, monsterZoneList[i].Radius);
                ptList.Add(targetPoint);
            }

            if (ptList.Count <= 0)
            {
                return new Point(-1, -1);
            }
            return ptList[Global.GetRandomNumber(0, ptList.Count)];
        }

#endregion 外部调用接口
    }
}
