using GameServer.Logic;
using Server.Protocol;
using Server.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.cc.monster
{
   public class CMonsterZoneManager
    {
        #region 基础属性
        /// <summary>
        /// 总的爆怪的区域列表
        /// </summary>
        private List<CMonsterZone> MonsterZoneList = new List<CMonsterZone>(100);
        /// <summary>
        /// 根据地图编号存取爆怪的区域字典
        /// </summary>
        private Dictionary<int, List<CMonsterZone>> Map2MonsterZoneDict = new Dictionary<int, List<CMonsterZone>>(100);

        /// <summary>
        /// 动态爆怪的区域列表
        /// </summary>
        private Dictionary<int, CMonsterZone> MonsterDynamicZoneDict = new Dictionary<int, CMonsterZone>(100);
        #endregion
        #region 初始化怪
        private object InitMonsterZoneMutex = new object();


        /// <summary>
        /// 加入爆怪区域
        /// </summary>
        /// <param name="monsterZone"></param>
        private void AddMap2MonsterZoneDict(CMonsterZone monsterZone)
        {
            List<CMonsterZone> monsterZoneList = null;
            if (Map2MonsterZoneDict.TryGetValue(monsterZone.MapCode, out monsterZoneList))
            {
                monsterZoneList.Add(monsterZone);
                return;
            }

            monsterZoneList = new List<CMonsterZone>();
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
                if ((int)MonsterBirthTypes.AfterKaiFuDays == configBirthType
                    || (int)MonsterBirthTypes.AfterHeFuDays == configBirthType
                    || (int)MonsterBirthTypes.AfterJieRiDays == configBirthType)
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
                                    string sTimeString = null;
                                    int nDayOfWeek = -1;

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
                                        BirthTimeTmp.BirthTime = birthTimePoint;

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

                CMonsterZone cmonsterZone = new CMonsterZone()
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
                    cmonsterZone.PursuitRadius = (int)Global.GetSafeAttributeLong(monsterItem, "PursuitRadius");
                }
                else
                {
                    cmonsterZone.PursuitRadius = (int)Global.GetSafeAttributeLong(monsterItem, "Radius");
                }

                lock (InitMonsterZoneMutex)
                {
                    //加入列表
                    MonsterZoneList.Add(cmonsterZone);
                    //加入爆怪区域
                    AddMap2MonsterZoneDict(cmonsterZone);
                }

                //加载静态的怪物信息
                cmonsterZone.LoadStaticMonsterInfo();

                //加载怪物
                cmonsterZone.LoadMonsters();//暂时屏蔽怪物加载
            }
        }



        #region 定时怪物复活调度

        /// <summary>
        /// 定时怪物复活调度(主线程中调用)
        /// </summary>
        public void RunMapMonsters(SocketListener sl, TCPOutPacketPool pool)
        {
            for (int i = 0; i < MonsterZoneList.Count; i++)
            {
                //根据当前的怪剩余个数，重新爆怪
                MonsterZoneList[i].ReloadMonsters(sl, pool);
            }

            

            //for (int i = 0; i < MonsterDynamicZoneDict.Values.Count; i++)
            List<CMonsterZone> monsterZoneList = MonsterDynamicZoneDict.Values.ToList<CMonsterZone>();
            for (int i = 0; i < monsterZoneList.Count; i++)
            {
                //执行销毁动态生成的怪物(主线程调用)
                monsterZoneList[i].DestroyDeadDynamicMonsters();
            }
        }

        //public void RunMapDynamicMonsters(SocketListener sl, TCPOutPacketPool pool)
        //{
        //    //执行添加动态的副本怪物
        //    for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
        //    {
        //        if (!RunAddCopyMapMonsters())
        //        {
        //            break;
        //        }
        //    }

        //    //执行销毁动态的副本怪物
        //    for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
        //    {
        //        if (!RunDestroyCopyMapMonsters())
        //        {
        //            break;
        //        }
        //    }

        //    //执行立刻刷新副本怪物的操作
        //    for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
        //    {
        //        if (!RunReloadCopyMapMonsters())
        //        {
        //            break;
        //        }
        //    }

        //    //执行立刻刷新普通地图怪物的操作
        //    for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
        //    {
        //        if (!RunReloadNormalMapMonsters())
        //        {
        //            break;
        //        }
        //    }

        //    /*//执行动态召唤怪物操作
        //    for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
        //    {
        //        if (!RunAddDynamicMonsters())
        //        {
        //            break;
        //        }
        //    }*/

        //    //执行立刻刷新竞技场机器人的刷新
        //    for (int i = 0; i < MonsterZoneManager.MaxRunAddDynamicMonstersQueueNum; i++)
        //    {
        //        if (!RunAddRobots())
        //        {
        //            break;
        //        }
        //    }

        //    // 最多循环100次
        //    int Count = 0;
        //    int loop_Count = 0;
        //    while (Count < MaxRunAddDynamicMonstersQueueNum)
        //    {
        //        for (int i = 0; i < Max_WaitingAddDynamicMonsterQueneCount; i++)
        //        {
        //            loop_Count++;
        //            if (RunAddDynamicMonsters(i))
        //            {
        //                Count++;
        //            }
        //        }
        //        if (loop_Count >= MonsterZoneManager.MaxRunQueueNum)
        //        {
        //            break;
        //        }
        //    }
        //}

        #endregion 

        #region
        /// <summary>
        /// 添加动态爆怪区域，每个地图有一个,所有参数都动态生成
        /// </summary>
        /// <param name="mapCode"></param>
        public void AddDynamicMonsterZone(int mapCode)
        {
            //判断是否是副本地图
            bool isFuBenMap = FuBenManager.IsFuBenMap(mapCode);

            CMonsterZone monsterZone = new CMonsterZone()
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

                

                //加入爆怪区域
                AddMap2MonsterZoneDict(monsterZone);
            }
        }
        #endregion

        #endregion

    }
}
