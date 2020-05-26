using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Server.Tools;
using Server.Protocol;
using GameServer.Server;
using Server.TCP;
using System.Net.Sockets;
using System.Xml.Linq;
using GameServer.Logic.WanMota;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    #region 资源数据类
    public class CGetResData
    {
        /// <summary>
        /// 类型
        /// </summary>
        public int type;
        /// <summary>
        /// 副本id
        /// </summary>
        public int copyId;

        /// <summary>
        /// 活动id
        /// </summary>
        public int activeId;

        /// <summary>
        /// 经验
        /// </summary>
        public int exp=0;

        /// <summary>
        /// 绑定金币
        /// </summary>
        public int bandmoney=0;

        /// <summary>
        /// 魔晶
        /// </summary>
        public int mojing=0;

        /// <summary>
        /// 成就
        /// </summary>
        public int chengjiu=0;

        /// <summary>
        /// 声望
        /// </summary> 
        public int shengwang=0;

        /// <summary>
        /// 战功
        /// </summary>
        public int zhangong=0;

        /// <summary>
        /// 绑钻
        /// </summary>
        public int bandDiamond = 0;

        /// <summary>
        /// 星魂
        /// </summary>
        public int xinghun = 0;

        /// <summary>
        /// 元素粉末
        /// </summary>
        public int yuanSuFenMo = 0;

    }
    #endregion  资源数据类

    /// <summary>
    /// 资源找回管理类
    /// </summary>
    public class CGetOldResourceManager
    {

        public static  float GoldRate = 0.75f;

        #region 配置数据
        /// <summary>
        /// 锁
        /// </summary>
        private static object _xmlDataMutex = new object();

        /// <summary>
        /// 配置数据
        /// </summary>
        private static XElement _xmlData = null;
        public static XElement xmlData
        {
            get
            {
                lock (_xmlDataMutex)
                {
                    if (_xmlData != null)
                        return _xmlData;
                }
                XElement xml = null;
                try
                {
                    string fileName = "Config/ZiYuanZhaoHui.xml";
                    xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));

                    //if (xml != null)
                    //{
                    //    XElement args = xml.Elements();
                    //    xml = args;
                    //}

                }
                catch (Exception e)
                {
                    xml = null;
                    LogManager.WriteException(e.ToString());
                }
                lock (_xmlDataMutex)
                {
                    _xmlData = xml;
                }
                return _xmlData;
            }

        }
        #endregion

        #region 系数
        //经验系数
        private static double[] _Exp = null;
        private static object _ExpMutex = new object();
        public static double[] ExpGold
        {
            get
            {
                if (_Exp != null)
                    return _Exp;
                else
                {
                    double[] Exp = null;
                    Exp = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZiYuanZhaoHuiExp");
                    lock (_ExpMutex)
                        _Exp = Exp;
                    return _Exp;
                }
                

            }
        }

        //绑金系数
        private static double[] _BondGold = null;
        private static object _BondGoldMutex = new object();
        public static double[] BondGold
        {
            get
            {
                if (_BondGold != null)
                    return _BondGold;
                else
                {
                    double[] Exp = null;
                    Exp = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZiYuanZhaoHuiBandGold");
                    lock (_BondGoldMutex)
                        _BondGold = Exp;
                    return _BondGold;
                }


            }
        }

        //魔晶系数
        private static double[] _MoJing = null;
        private static object _MoJingMutex = new object();
        public static double[] MoJing
        {
            get
            {
                if (_MoJing != null)
                    return _MoJing;
                else
                {
                    double[] values = null;
                    values = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZiYuanZhaoHuiMoJing");
                    lock (_MoJingMutex)
                        _MoJing = values;
                    return _MoJing;
                }


            }
        }

        //声望系数
        private static double[] _ShengWang = null;
        private static object _ShengWangMutex = new object();
        public static double[] ShengWang
        {
            get
            {
                if (_ShengWang != null)
                    return _ShengWang;
                else
                {
                    double[] values = null;
                    values = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZiYuanZhaoHuiShengWang");
                    lock (_ShengWangMutex)
                        _ShengWang = values;
                    return _ShengWang;
                }


            }
        }

        //成就系数
        private static double[] _ChengJiu = null;
        private static object _ChengJiuMutex = new object();
        public static double[] ChengJiu
        {
            get
            {
                if (_ChengJiu != null)
                    return _ChengJiu;
                else
                {
                    double[] values = null;
                    values = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZiYuanZhaoHuiChengJiu");
                    lock (_ChengJiuMutex)
                        _ChengJiu = values;
                    return _ChengJiu;
                }


            }
        }

        /// <summary>
        /// 战功系数
        /// </summary>
        private static double[] _ZhanGong = null;
        private static object _ZhanGongMutex = new object();
        public static double[] ZhanGong
        {
            get
            {
                if (_ZhanGong != null)
                    return _ZhanGong;
                else
                {
                    double[] values = null;
                    values = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZiYuanZhaoHuiZhanGong");
                    lock (_ZhanGongMutex)
                        _ZhanGong = values;
                    return _ZhanGong;
                }


            }
        }

        /// <summary>
        /// 绑钻系数
        /// </summary>
        private static double[] _BangZuan = null;
        private static object _BangZuanMutex = new object();
        public static double[] BangZuan
        {
            get
            {
                if (_BangZuan != null)
                    return _BangZuan;
                else
                {
                    double[] values = null;
                    values = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZiYuanZhaoHuiBindZuan");
                    lock (_BangZuanMutex)
                        _BangZuan = values;
                    return _BangZuan;
                }
            }
        }

        /// <summary>
        /// 绑钻系数
        /// </summary>
        private static double[] _XingHun = null;
        private static object _XingHunMutex = new object();
        public static double[] XingHun
        {
            get
            {
                if (_XingHun != null)
                    return _XingHun;
                else
                {
                    double[] values = null;
                    values = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZiYuanZhaoHuiXingHun");
                    lock (_XingHunMutex)
                        _XingHun = values;
                    return _XingHun;
                }
            }
        }

        /// <summary>
        /// 元素粉末系数
        /// </summary>
        private static double[] _YuanSuFenMo = null;
        private static object _YuanSuFenMoMutex = new object();
        public static double[] YuanSuFenMo
        {
            get
            {
                if (_YuanSuFenMo != null)
                    return _YuanSuFenMo;
                else
                {
                    double[] values = null;
                    values = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZiYuanZhaoHuiYuanSuFenMo");
                    lock (_YuanSuFenMoMutex)
                        _YuanSuFenMo = values;
                    return _YuanSuFenMo;
                }
            }
        }

        /// <summary>
        /// MU经验各项产出根据转生对应的经验系数
        /// </summary>
        private static double[] _changelifeRate = null;
        public static double RoleChangelifeRate(int count)
        {
            try
            {
                if (_changelifeRate == null)
                    _changelifeRate = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZhuanShengExpXiShu");
                if (_changelifeRate != null && _changelifeRate.Length > count)
                    return _changelifeRate[count];
                return 1;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "获取经验各项产出根据转生对应的经验系数 error count=" + count, false);
            }
            return 1;
            
        }
    #endregion

        /// <summary>
        /// 获得昨天可找回资源的信息
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static List<OldResourceInfo> GetOldResourceInfo(GameClient client)
        {
            List<OldResourceInfo> oldinfo = new List<OldResourceInfo>();
            if (client.ClientData.OldResourceInfoDict != null)
            {
                
                foreach(var item in client.ClientData.OldResourceInfoDict.Values)
                {
                    if(item!=null&&item.leftCount>0)
                        oldinfo.Add(item);
                }
                
            }
            return oldinfo;
        }
        /// <summary>
        /// 是否有可找回的旧资源
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool HasOldResource(GameClient client)
        {
            if (GetOldResourceInfo(client).Count > 0)
                return true;
            return false;
        }

        /// <summary>
        /// 查询角色是否可以做该副本或者任务
        /// </summary>
        /// <param name="client"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private static bool RoleCando(GameClient client, XElement item)
        {
            int type = (int)Global.GetSafeAttributeLong(item, "Type");
            string[] minilevel = Global.GetSafeAttributeStr(item, "MinLevel").Split(',');
            string[] maxlevel = Global.GetSafeAttributeStr(item, "MaxLevel").Split(',');
            int curmaxchangelife = Global.SafeConvertToInt32(minilevel[0]);
            int curmaxlevel = Global.SafeConvertToInt32(minilevel[1]);
            string NeedRenWu = Global.GetSafeAttributeStr(item, "NeedRenWu");
            bool condition1 = false;

            //判断等级
            if (Global.SafeConvertToInt32(minilevel[0]) < client.ClientData.ChangeLifeCount && client.ClientData.ChangeLifeCount < Global.SafeConvertToInt32(maxlevel[0]))
                condition1 = true;
            if (Global.SafeConvertToInt32(minilevel[0]) == client.ClientData.ChangeLifeCount)
            {
                if (Global.SafeConvertToInt32(minilevel[1]) <= client.ClientData.Level)
                    condition1 = true;
            }
            if (Global.SafeConvertToInt32(maxlevel[0]) == client.ClientData.ChangeLifeCount)
            {
                if (Global.SafeConvertToInt32(maxlevel[1]) >= client.ClientData.Level)
                    condition1 = true;
            }

            //判断前置任务
            bool condition2 = false;
            if (string.IsNullOrEmpty(NeedRenWu))
                condition2 = true;
            else
            {
                int taskid = Global.SafeConvertToInt32(NeedRenWu);
                condition2 = TaskHasDone(client, taskid);
            }
            if (condition1 && condition2)
                return true;
            return false;
        }

        /// <summary>
        /// 根据创角时间与当前时间检查是否计算资源找回 [XSea 2015/5/25]
        /// </summary>
        /// <param name="client">角色</param>
        /// <returns>true=计算，false=不计算</returns>
        private static bool IsCanCalcOldResource(GameClient client)
        {
            // 判空
            if (null == client)
                return false;

            // 创建角色时间
            DateTime dtCreateRoleTime = Global.GetRegTime(client.ClientData);

            // 当前时间
            DateTime dtCurTime = TimeUtil.NowDateTime();

            // 只有在同一年中才会出现同一天
            if (dtCreateRoleTime.Year == dtCurTime.Year)
            {
                // 创建角色与当前时间是同一天则不会计算资源找回
                if (dtCreateRoleTime.DayOfYear == dtCurTime.DayOfYear)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 任务是否已经做过
        /// </summary>
        /// <param name="client"></param>
        /// <param name="taskID"></param>
        /// <returns></returns>
        static bool TaskHasDone(GameClient client, int taskID)
        {
            return client.ClientData.MainTaskID >= taskID;
        }

        /// <summary>
        /// 获得旧副本数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="fuBenID"></param>
        /// <returns></returns>
        public static FuBenData GetOldFubenData(GameClient client, int fuBenID)
        {
            if (null == client.ClientData.OldFuBenDataList) return null;

            lock (client.ClientData.OldFuBenDataList)
            {
                for (int i = 0; i < client.ClientData.OldFuBenDataList.Count; i++)
                {
                    if (client.ClientData.OldFuBenDataList[i].FuBenID == fuBenID)
                    {
                        return client.ClientData.OldFuBenDataList[i];
                    }
                }
            }

            return null;
        }

        public static int GetVIPActiveNumByType(GameClient client, int activeId)
        {
            int nVipLev = client.ClientData.VipLevel;

            int nNum = 0;
            int[] nArry = null;
            string keyname = "VIPEnterDaimonSquareCountAddValue";
            switch ((SpecialActivityTypes)activeId)
            {
                case SpecialActivityTypes.DemoSque:
                    keyname = "VIPEnterDaimonSquareCountAddValue";
                    break;
                case SpecialActivityTypes.BloodCastle:
                    keyname = "VIPEnterBloodCastleCountAddValue";
                    break;
                default:
                    return 0;
                
            }

            nArry = GameManager.systemParamsList.GetParamValueIntArrayByName(keyname);

            if (nVipLev > 0 && nArry != null && nArry[nVipLev] > 0)
            {
                nNum = nArry[nVipLev];

            }
            return 0;
        }

        /// <summary>
        /// 计算可找回的资源
        /// </summary>
        private static void CalcOldResourceInfo(int leftnum, CGetResData data, OldResourceInfo inInfo, out OldResourceInfo outInfo)
        {
            outInfo = inInfo;
            if (leftnum > 0)
            {
                if (inInfo == null)
                {
                    outInfo = new OldResourceInfo();
                    outInfo.bandmoney = 0;
                    outInfo.chengjiu = 0;
                    outInfo.exp = 0;
                    outInfo.mojing = 0;
                    outInfo.zhangong = 0;
                    outInfo.shengwang = 0;
                    outInfo.leftCount = 0;
                    outInfo.bandDiamond = 0;
                    outInfo.xinghun = 0;
                    outInfo.yuanSuFenMo = 0;
                }

                outInfo.bandmoney += data.bandmoney * leftnum;
                outInfo.chengjiu += data.chengjiu * leftnum;
                outInfo.exp += data.exp * leftnum;
                outInfo.mojing += data.mojing * leftnum;
                outInfo.zhangong += data.zhangong * leftnum;
                outInfo.shengwang += data.shengwang * leftnum;
                outInfo.bandDiamond += data.bandDiamond * leftnum;
                outInfo.xinghun += data.xinghun * leftnum;
                outInfo.yuanSuFenMo += data.yuanSuFenMo * leftnum;
                outInfo.leftCount += leftnum;
                outInfo.type = data.type;
            }
        }

        /// <summary>
        /// 计算可找回的资源
        /// </summary>
        private static void CalcOldResourceInfo(int oldday, int oldnum, int total, CGetResData data, OldResourceInfo inInfo, out OldResourceInfo outInfo)
        {
            outInfo = inInfo;
            int leftnum = 0;
            int yesterdayid = TimeUtil.NowDateTime().AddDays(-1).DayOfYear;
            if (oldday >= 0 && oldnum >= 0)
            {
                if (yesterdayid == oldday)
                {
                    leftnum = total - oldnum;
                }
                else
                {
                    leftnum = total;
                }

                leftnum = leftnum > 0 ? leftnum : 0;
            }

            CalcOldResourceInfo(leftnum, data, inInfo, out outInfo);
        }

        /// <summary>
        /// 根据副本id 读取数据库存盘数据，计算可以找回的资源
        /// </summary>
        /// <param name="client"></param>
        /// <param name="copyId"></param>
        /// <param name="total"></param>
        /// <param name="data"></param>
        /// <param name="outInfo"></param>
        private static void GetFubenResourceInfo(GameClient client, int copyId, int total, bool needFinish, CGetResData data, OldResourceInfo inInfo, out OldResourceInfo outInfo)
        {
            outInfo = inInfo;
            if (total < 1)
                return;

            int oldday = -1;
            int oldnum = -1;
            FuBenData fubendata = CGetOldResourceManager.GetOldFubenData(client, copyId);
            if (null != fubendata)
            {
                oldday = fubendata.DayID;
                oldnum = needFinish ? fubendata.FinishNum : fubendata.EnterNum;
            }
            else
            {
                oldday = 0;
                oldnum = 0;
            }
            CalcOldResourceInfo(oldday, oldnum, total, data, inInfo, out outInfo);
        }
        /// <summary>
        /// 计算可以获得资源总量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="type"></param>
        /// <param name="getRestDataDict"></param>
        /// <param name="outInfo"></param>
        private static void ComputeResourceByType(GameClient client, int type,Dictionary<int, List<CGetResData>> getRestDataDict, out OldResourceInfo outInfo)
        {
            outInfo = null;
            if (!getRestDataDict.ContainsKey(type))
                return;
            List<CGetResData> datalist = getRestDataDict[type];
           // OldResourceInfo tempInfo = new OldResourceInfo();
            foreach (var data in datalist)
            {
                #region 判断副本
                if (data.copyId != -1)
                {
                    SystemXmlItem systemFuBenItem = null;
                    if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(data.copyId, out systemFuBenItem))
                    {
                       continue;
                    }
                    bool needFinish = false;
                    int total = systemFuBenItem.GetIntValue("FinishNumber");
                    if (total < 0)//经验副本和金币副本
                    {
                        needFinish = true;
                        total = systemFuBenItem.GetIntValue("EnterNumber");
                    }
                    OldResourceInfo tempdata = null;
                    GetFubenResourceInfo(client, data.copyId, total, needFinish, data, tempdata, out tempdata);
                    if (tempdata != null)
                    {
                        int leftnum = tempdata.leftCount;
                        CalcOldResourceInfo(tempdata.leftCount, data, outInfo, out outInfo);
                    }
                }
                #endregion 判断副本

                #region 判断活动
                if(data.activeId!=-1)
                {
                    int leftnum = 0;
                    switch ((CandoType)data.type)
                    {
                        
                        case CandoType.DemonSquare:
                            {

                                int nNum = CGetOldResourceManager.GetVIPActiveNumByType(client, data.activeId);
                                int nMapID = Global.GetDaimonSquareCopySceneIDForRole(client);
                                DaimonSquareDataInfo bcDataTmp = null;

                                Data.DaimonSquareDataInfoList.TryGetValue(nMapID, out bcDataTmp);
                                if (null == bcDataTmp)
                                {
                                    bcDataTmp = Data.DaimonSquareDataInfoList.FirstOrDefault().Value;
                                    if (bcDataTmp == null)
                                    {
                                        break;
                                    }
                                }

                                int nDate = TimeUtil.NowDateTime().AddDays(-1).DayOfYear; // TimeUtil.NowDateTime().DayOfYear;                // 当前时间
                                int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, (int)SpecialActivityTypes.DemoSque);
                                if (nCount < 0)
                                    nCount = 0;
                                leftnum = bcDataTmp.MaxEnterNum + nNum - nCount;
                            }
                            break;
                        case CandoType.BloodCity:
                            {
                                int nNum = CGetOldResourceManager.GetVIPActiveNumByType(client, data.activeId);
                                int nMapID = Global.GetBloodCastleCopySceneIDForRole(client);
                                BloodCastleDataInfo bcDataTmp = null;

                                Data.BloodCastleDataInfoList.TryGetValue(nMapID, out bcDataTmp);
                                if (null == bcDataTmp)
                                {
                                    bcDataTmp = Data.BloodCastleDataInfoList.FirstOrDefault().Value;
                                    if (bcDataTmp == null)
                                    {
                                        break;
                                    }
                                }

                                int nDate = TimeUtil.NowDateTime().AddDays(-1).DayOfYear; //TimeUtil.NowDateTime().DayOfYear;                // 当前时间
                                int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, (int)SpecialActivityTypes.BloodCastle);
                                if (nCount < 0)
                                    nCount = 0;
                                leftnum = bcDataTmp.MaxEnterNum + nNum - nCount;
                            }
                            break;
                        case CandoType.AngelTemple:
                            {
                                List<string> timePointsList = GameManager.AngelTempleMgr.m_AngelTempleData.BeginTime;
                                int nDate = TimeUtil.NowDateTime().AddDays(-1).DayOfYear; //TimeUtil.NowDateTime().DayOfYear;                // 当前时间
                                int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, (int)SpecialActivityTypes.AngelTemple);
                                if (nCount < 0)
                                    nCount = 0;
                                leftnum = timePointsList.Count - nCount;
                            }
                            break;
              
                        
                        case CandoType.PartWar:
                            {
                                SystemXmlItem systemBattle = null;
                                if (!GameManager.SystemBattle.SystemXmlItemDict.TryGetValue(1, out systemBattle))
                                {
                                    return;
                                }

                                string[] fields = null;
                                string timePoints = systemBattle.GetStringValue("TimePoints");
                                if (null != timePoints && timePoints != "")
                                {
                                    fields = timePoints.Split(',');
                                }

                                int nDate = TimeUtil.NowDateTime().AddDays(-1).DayOfYear; //TimeUtil.NowDateTime().DayOfYear;                // 当前时间
                                int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, (int)SpecialActivityTypes.CampBattle);
                                if (nCount < 0)
                                    nCount = 0;
                                if (fields != null)
                                    leftnum = fields.Length - nCount;
                            }
                            break;
                        case CandoType.PKKing:
                            {
                                SystemXmlItem systemBattle = null;
                                if (!GameManager.SystemArenaBattle.SystemXmlItemDict.TryGetValue(1, out systemBattle))
                                {
                                    return;
                                }

                                List<string> timePointsList = new List<string>();
                                string[] fields = null;
                                string timePoints = systemBattle.GetStringValue("TimePoints");
                                if (null != timePoints && timePoints != "")
                                {
                                    fields = timePoints.Split(',');
                                    
                                }
                                int nDate = TimeUtil.NowDateTime().AddDays(-1).DayOfYear; //TimeUtil.NowDateTime().DayOfYear;              
                                int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, (int)SpecialActivityTypes.TheKingOfPK);
                                if (nCount < 0)
                                    nCount = 0;
                                if (fields != null)
                                    leftnum = fields.Length - nCount;
                            }
                            break;

                        case CandoType.OldBattlefield://古战场昨天进入数据
                            {
                                int nDate = TimeUtil.NowDateTime().AddDays(-1).DayOfYear;//TimeUtil.NowDateTime().DayOfYear;   
                                int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, (int)SpecialActivityTypes.OldBattlefield);
                                if (nCount < 0)
                                    nCount = 0;
                                leftnum = 1 - nCount;
                            }
                            break;
                        case CandoType.Arena:

                            break;
                    }
                    leftnum = leftnum > 0 ? leftnum : 0;
                    CalcOldResourceInfo(leftnum, data, outInfo, out outInfo);
                }

                #endregion 判断活动
                if (data.type == (int)CandoType.Arena)
                {
                   int total = 0;
                   int jingjiFuBenId = (int)GameManager.systemParamsList.GetParamValueIntByName("JingJiFuBenID");
                   SystemXmlItem jingjiFuBenItem = null;
                   GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(jingjiFuBenId, out jingjiFuBenItem);
                   total = jingjiFuBenItem.GetIntValue("EnterNumber");
                   GetFubenResourceInfo(client, jingjiFuBenId, total, false, data, outInfo, out outInfo);
                }
                //判断万魔塔
                else if (data.type == (int)CandoType.WanmoTower)
                {
                    //int total =0;
                    //if (client.ClientData.WanMoTaProp.nPassLayerCount >= SweepWanMotaManager.nSweepReqMinLayerOrder)
                    //    total = 1; //client.ClientData.WanMoTaProp.nPassLayerCount;
                    
                    //GetFubenResourceInfo(client, SweepWanMotaManager.nWanMoTaSweepFuBenOrder, total, data, out outInfo);
                }
                //日常任务资源找回
                else if (data.type == (int)CandoType.DailyTask)
                {
                    int oldday = -1;
                    int oldnum = -1;
                    if (client.ClientData.YesterdayDailyTaskData != null)
                    {
                        oldday = DateTime.Parse(client.ClientData.YesterdayDailyTaskData.RecTime).DayOfYear;
                        oldnum = client.ClientData.YesterdayDailyTaskData.RecNum;
                    }
                    CalcOldResourceInfo(oldday, oldnum, Global.MaxDailyTaskNumForMU, data, outInfo, out outInfo);
                }
                //讨伐任务资源找回
                else if (data.type == (int)CandoType.TaofaTaskCanDo)
                {
                    int oldday = -1;
                    int oldnum = -1;

                    if (client.ClientData.YesterdayTaofaTaskData != null)
                    {
                        oldday = DateTime.Parse(client.ClientData.YesterdayTaofaTaskData.RecTime).DayOfYear;
                        oldnum = client.ClientData.YesterdayTaofaTaskData.RecNum;
                    }
                    CalcOldResourceInfo(oldday, oldnum, Global.MaxTaofaTaskNumForMU, data, outInfo, out outInfo);
                }
                //水晶幻境资源找回
                else if (data.type == (int)CandoType.CrystalCollectCanDo)
                {
                    int oldday = -1;
                    int oldnum = -1;
                    if (client.ClientData.OldCrystalCollectData != null)
                    {
                        oldday = client.ClientData.OldCrystalCollectData.OldDay;
                        oldnum = client.ClientData.OldCrystalCollectData.OldNum;
                    }
                    CalcOldResourceInfo(oldday, oldnum, CaiJiLogic.DailyNum, data, outInfo, out outInfo);
                }
                else if (data.type == (int)CandoType.HYSY)
                {
                    int oldday = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysyYTDSuccessDayId);
                    int oldnum = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HysyYTDSuccessCount);
                    int leftnum = 3;
                    int[] nParams = GameManager.systemParamsList.GetParamValueIntArrayByName("TempleMirageWinNum");

                    if (null != nParams && nParams.Length == 2)
                    {
                        leftnum = nParams[0];
                    }

                    CalcOldResourceInfo(leftnum - oldnum, data, outInfo, out outInfo);
                }
            }

        }
        /// <summary>
        /// 每天第一次登陆初始化数据
        /// </summary>
        /// <param name="client"></param>
        public static void InitRoleOldResourceInfo(GameClient client, bool isFirstLogin)
        {
            // 如果创角时间与当前是同一天，则不进行资源找回的计算
            if (!IsCanCalcOldResource(client))
                return;

            // 是否首次登录
            if (!isFirstLogin)
            {
                client.ClientData.OldResourceInfoDict = CGetOldResourceManager.ReadResourceGetfromDB(client);
                return;
            }
            ////保存昨天的日常任务数据
            //for (int nIndex = 0; nIndex < client.ClientData.MyDailyTaskDataList.Count; ++nIndex)
            //{
            //    string today = TimeUtil.NowDateTime().ToString("yyyy-MM-dd");

            //    if (client.ClientData.MyDailyTaskDataList[nIndex].TaskClass == (int)TaskClasses.DailyTask && (client.ClientData.MyDailyTaskDataList[nIndex].RecTime != today /*|| gameClient.ClientData.MyDailyTaskDataList[nIndex].RecNum == 20*/))
            //    {
            //        client.ClientData.YesterdayDailyTaskData = client.ClientData.MyDailyTaskDataList[nIndex];
            //    }
            //}
            //Global.GetFuBenData(client,
            Dictionary<int, List<CGetResData>> getRestDataDict = new Dictionary<int, List<CGetResData>>();
            
            //筛选数据
            if (xmlData == null)
                return;
            IEnumerable<XElement> xmlItems = xmlData.Elements(); 
            foreach (var item in xmlItems)
            {
                if (CGetOldResourceManager.RoleCando(client, item))
                {
                    int type = (int)Global.GetSafeAttributeLong(item, "Type");

                    int copyId = (int)Global.GetSafeAttributeLong(item, "CodeID");
                    int   activeId = (int)Global.GetSafeAttributeLong(item, "HuoDongID");
                    int   exp = (int)Global.GetSafeAttributeLong(item, "ExpAward");
                    int  bandmoney = (int)Global.GetSafeAttributeLong(item, "BandMoneyAward");
                    int   shengwang = (int)Global.GetSafeAttributeLong(item, "ShengWangAward");
                    int   zhangong = (int)Global.GetSafeAttributeLong(item, "ZhanGongAward");
                    int   mojing = (int)Global.GetSafeAttributeLong(item, "MoJingAward");
                    int chengjiu = (int)Global.GetSafeAttributeLong(item, "ChengJiuAward");
                    int bandDiamond = (int)Global.GetSafeAttributeLong(item, "BindZuanAward");
                    int xinghun = (int)Global.GetSafeAttributeLong(item, "XingHunAward");
                    int yuanSuFenMo = (int)Global.GetSafeAttributeLong(item, "YuanSuFenMo");

                    CGetResData data = new CGetResData();
                    data.type = type;
                    data.copyId = copyId;
                    data.activeId = activeId;
                    data.exp = exp > 0 ? exp : 0;
                    data.bandmoney = bandmoney > 0 ? bandmoney : 0;
                    data.shengwang = shengwang > 0 ? shengwang : 0;
                    data.zhangong = zhangong > 0 ? zhangong : 0;
                    data.mojing = mojing > 0 ? mojing : 0;
                    data.chengjiu = chengjiu > 0 ? chengjiu : 0;
                    data.bandDiamond = bandDiamond > 0 ? bandDiamond : 0;
                    data.xinghun = xinghun > 0 ? xinghun : 0;
                    data.yuanSuFenMo = yuanSuFenMo > 0 ? yuanSuFenMo : 0;
                    
                    if (!getRestDataDict.ContainsKey(type))
                    {
                        getRestDataDict[type] = new List<CGetResData>();
                    }
                    getRestDataDict[type].Add(data);

                }
            }
            //计算出可找回的资源信息
            Dictionary<int, OldResourceInfo> ResourceInfoDict = new Dictionary<int,OldResourceInfo>();
            List<int> dictypes = getRestDataDict.Keys.ToList();
            foreach(int type in dictypes)
            {
                OldResourceInfo info = null;
                ComputeResourceByType(client, type, getRestDataDict, out info);
                if (info != null)
                {
                    ResourceInfoDict[type] = info;
                    ResourceInfoDict[type].roleId = client.ClientData.RoleID;
                }
            }
            client.ClientData.OldResourceInfoDict = ResourceInfoDict;
            ReplaceDataToDB(client);
        }
        /// <summary>
        /// 给与角色资源
        /// </summary>
        /// <param name="client"></param>
        /// <param name="actType"></param>
        /// <param name="goldorZuanshi"></param>
        /// <param name="getModel"></param>
        /// <returns></returns>
        public static int GiveRoleOldResource(GameClient client, int actType, int goldorZuanshi, int getModel)
        {
            int ret = 0;
            OldResourceInfo dataInfo = null;
            OldResourceInfo dataToclient = null;
            int cost = 0;
            double changeliferate = CGetOldResourceManager.RoleChangelifeRate(client.ClientData.ChangeLifeCount);
            if (getModel == 0)
            {
                if (client.ClientData.OldResourceInfoDict==null||!client.ClientData.OldResourceInfoDict.TryGetValue(actType, out dataInfo))
                {
                    return  -3;
                }
                if (dataInfo.leftCount == 0)
                    return -3;
                if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.BondGold.Length)
                {
                    if (CGetOldResourceManager.BondGold[goldorZuanshi] != 0)
                        cost = (int)(dataInfo.bandmoney / CGetOldResourceManager.BondGold[goldorZuanshi]);
                }
                if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.ExpGold.Length)
                {
                    if (!changeliferate.Equals(0) && CGetOldResourceManager.ExpGold[goldorZuanshi] != 0)
                        cost += (int)((double)dataInfo.exp / (changeliferate * CGetOldResourceManager.ExpGold[goldorZuanshi]));
                }
                if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.ChengJiu.Length)
                { 
                    if (CGetOldResourceManager.ChengJiu[goldorZuanshi] != 0)
                        cost += (int)(dataInfo.chengjiu / CGetOldResourceManager.ChengJiu[goldorZuanshi]);
                }
                if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.ShengWang.Length)
                { 
                    if (CGetOldResourceManager.ShengWang[goldorZuanshi] != 0)
                        cost += (int)(dataInfo.shengwang / CGetOldResourceManager.ShengWang[goldorZuanshi]);
                }
                if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.MoJing.Length)
                {
                    if (CGetOldResourceManager.MoJing[goldorZuanshi] != 0)
                        cost += (int)(dataInfo.mojing / CGetOldResourceManager.MoJing[goldorZuanshi]);
                }
                if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.ZhanGong.Length)
                {
                    if (CGetOldResourceManager.ZhanGong[goldorZuanshi] != 0)
                        cost += (int)(dataInfo.zhangong / CGetOldResourceManager.ZhanGong[goldorZuanshi]);
                }
                if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.BangZuan.Length)
                {
                    if (CGetOldResourceManager.BangZuan[goldorZuanshi] != 0)
                        cost += (int)(dataInfo.bandDiamond / CGetOldResourceManager.BangZuan[goldorZuanshi]);
                }
                if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.XingHun.Length)
                {
                    if (CGetOldResourceManager.XingHun[goldorZuanshi] != 0)
                        cost += (int)(dataInfo.xinghun / CGetOldResourceManager.XingHun[goldorZuanshi]);
                }
                if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.YuanSuFenMo.Length)
                {
                    if (CGetOldResourceManager.YuanSuFenMo[goldorZuanshi] != 0)
                        cost += (int)(dataInfo.yuanSuFenMo / CGetOldResourceManager.YuanSuFenMo[goldorZuanshi]);
                }             

                dataToclient = dataInfo;
            }
            else
            {
                dataToclient = new OldResourceInfo();
                int count = 0;
                List<int> dicTypes = client.ClientData.OldResourceInfoDict.Keys.ToList();
                foreach(int type in dicTypes)
                {
                    if (!client.ClientData.OldResourceInfoDict.TryGetValue(type, out dataInfo))
                    {
                        continue;
                    }
                    if (dataInfo.leftCount == 0)
                        continue;

                    if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.BondGold.Length)
                    {
                        if (CGetOldResourceManager.BondGold[goldorZuanshi] != 0)
                            cost += (int)(dataInfo.bandmoney / CGetOldResourceManager.BondGold[goldorZuanshi]);
                    }
                    if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.ExpGold.Length)
                    {
                        if (!changeliferate.Equals(0) && CGetOldResourceManager.ExpGold[goldorZuanshi] != 0)
                            cost += (int)(dataInfo.exp / (changeliferate * CGetOldResourceManager.ExpGold[goldorZuanshi]));
                    }
                    if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.ChengJiu.Length)
                    {
                        if (CGetOldResourceManager.ChengJiu[goldorZuanshi] != 0)
                            cost += (int)(dataInfo.chengjiu / CGetOldResourceManager.ChengJiu[goldorZuanshi]);
                    }
                    if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.ShengWang.Length)
                    {
                        if (CGetOldResourceManager.ShengWang[goldorZuanshi] != 0)
                            cost += (int)(dataInfo.shengwang / CGetOldResourceManager.ShengWang[goldorZuanshi]);
                    }
                    if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.MoJing.Length)
                    {
                        if (CGetOldResourceManager.MoJing[goldorZuanshi] != 0)
                            cost += (int)(dataInfo.mojing / CGetOldResourceManager.MoJing[goldorZuanshi]);
                    }
                    if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.ZhanGong.Length)
                    {
                        if (CGetOldResourceManager.ZhanGong[goldorZuanshi] != 0)
                            cost += (int)(dataInfo.zhangong / CGetOldResourceManager.ZhanGong[goldorZuanshi]);
                    }
                    if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.BangZuan.Length)
                    {
                        if (CGetOldResourceManager.BangZuan[goldorZuanshi] != 0)
                            cost += (int)(dataInfo.bandDiamond / CGetOldResourceManager.BangZuan[goldorZuanshi]);
                    }
                    if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.XingHun.Length)
                    {
                        if (CGetOldResourceManager.XingHun[goldorZuanshi] != 0)
                            cost += (int)(dataInfo.xinghun / CGetOldResourceManager.XingHun[goldorZuanshi]);
                    }
                    if (goldorZuanshi >= 0 && goldorZuanshi < CGetOldResourceManager.YuanSuFenMo.Length)
                    {
                        if (CGetOldResourceManager.YuanSuFenMo[goldorZuanshi] != 0)
                            cost += (int)(dataInfo.yuanSuFenMo / CGetOldResourceManager.YuanSuFenMo[goldorZuanshi]);
                    }

                    dataToclient.bandmoney += dataInfo.bandmoney;
                    dataToclient.exp += dataInfo.exp;
                    dataToclient.chengjiu += dataInfo.chengjiu;
                    dataToclient.shengwang += dataInfo.shengwang;
                    dataToclient.mojing += dataInfo.mojing;
                    dataToclient.zhangong += dataInfo.zhangong;
                    dataToclient.bandDiamond += dataInfo.bandDiamond;
                    dataToclient.xinghun += dataInfo.xinghun;
                    dataToclient.yuanSuFenMo += dataInfo.yuanSuFenMo;
                    count++;
                }
                if (count == 0)
                {
                    return -3;
                }
            }

            if (cost <= 0)
            {
                return -3;
            }
            
            switch (goldorZuanshi)
            {
                case 0:
                    {
                        if (cost > client.ClientData.Money1 + client.ClientData.YinLiang)
                            return -1;
                        if (Global.SubBindTongQianAndTongQian(client, cost, "资源找回"))
                        {
                            if (dataToclient.exp > 0)
                            {
                                long giveexp = (long)(dataToclient.exp * GoldRate);
                                GameManager.ClientMgr.ProcessRoleExperience(client, giveexp);

                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                            StringUtil.substitute(Global.GetLang("恭喜获得经验 +{0}"), giveexp),
                                                                            GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlyErr, (int)HintErrCodeTypes.None);
                            }
                                
                            if (dataToclient.mojing > 0)
                                GameManager.ClientMgr.ModifyTianDiJingYuanValue(client, (int)(dataToclient.mojing * GoldRate), "资源找回(金币)");
                            if (dataToclient.shengwang > 0)
                                GameManager.ClientMgr.ModifyShengWangValue(client, (int)(dataToclient.shengwang * GoldRate), "资源找回(金币)");
                            if (dataToclient.bandmoney > 0)
                            {
                                int givemoney = (int)(dataToclient.bandmoney * GoldRate);
                                GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, givemoney, "金币资源找回");

                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                               StringUtil.substitute(Global.GetLang("恭喜获得绑定金币 +{0}"), givemoney),
                                                                               GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyErr, (int)HintErrCodeTypes.None);
                                
                            }
                                
                            if (dataToclient.chengjiu > 0)
                                GameManager.ClientMgr.ModifyChengJiuPointsValue(client, (int)(dataToclient.chengjiu * GoldRate), "资源找回(金币)");
                            if (dataToclient.zhangong > 0)
                            {
                                int zhangong = (int)(dataToclient.zhangong * GoldRate);
                                GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ref zhangong, AddBangGongTypes.None);
                            }

                            if (dataToclient.bandDiamond > 0)
                            {
                                GameManager.ClientMgr.AddUserGold(client, (int)(dataToclient.bandDiamond * GoldRate), "资源找回获得绑钻");
                            }
                            if (dataToclient.xinghun > 0)
                            {
                                GameManager.ClientMgr.ModifyStarSoulValue(client, (int)(dataToclient.xinghun * GoldRate), "资源找回获得星魂", true, true);
                            }
                            if (dataToclient.yuanSuFenMo > 0)
                            {
                                GameManager.ClientMgr.ModifyYuanSuFenMoValue(client, (int)(dataToclient.yuanSuFenMo * GoldRate), "资源找回获得元素粉末", true);
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        if (cost > client.ClientData.UserMoney)
                            return -2;
                        if (GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, cost, "资源找回"))
                        {
                            if (dataToclient.exp > 0)
                            {
                               
                                GameManager.ClientMgr.ProcessRoleExperience(client, dataToclient.exp);

                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                            StringUtil.substitute(Global.GetLang("恭喜获得经验 +{0}"), dataToclient.exp),
                                                                                       GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlyErr, (int)HintErrCodeTypes.None);
                                      
                            }
                               
                            if (dataToclient.mojing > 0)
                                GameManager.ClientMgr.ModifyTianDiJingYuanValue(client, dataToclient.mojing, "资源找回(钻石)");
                            if (dataToclient.shengwang > 0)
                                GameManager.ClientMgr.ModifyShengWangValue(client, dataToclient.shengwang, "资源找回(钻石)");
                            if (dataToclient.bandmoney > 0)
                            {
                                GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, dataToclient.bandmoney, "钻石资源找回");
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                               StringUtil.substitute(Global.GetLang("恭喜获得绑定金币 +{0}"), dataToclient.bandmoney),
                                                                               GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlyErr, (int)HintErrCodeTypes.None);
                            }
                            
                            if (dataToclient.chengjiu > 0)
                                GameManager.ClientMgr.ModifyChengJiuPointsValue(client, dataToclient.chengjiu, "资源找回(钻石)");
                            if (dataToclient.zhangong > 0)
                                GameManager.ClientMgr.AddBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ref dataToclient.zhangong, AddBangGongTypes.None);
                            if (dataToclient.bandDiamond > 0)
                                GameManager.ClientMgr.AddUserGold(client, dataToclient.bandDiamond, "资源找回获得绑钻");
                            if (dataToclient.xinghun > 0)
                                GameManager.ClientMgr.ModifyStarSoulValue(client, dataToclient.xinghun, "资源找回获得星魂", true, true);
                            if (dataToclient.yuanSuFenMo > 0)
                                GameManager.ClientMgr.ModifyYuanSuFenMoValue(client, dataToclient.yuanSuFenMo, "资源找回获得元素粉末", true);
                        }
                    }
                    break;
                
            }

            // 这个锁估计有问题
            lock (client)
            {
                if (getModel == 0)
                {

                    //client.ClientData.OldResourceInfoDict[actType] = new OldResourceInfo() ;
                    //client.ClientData.OldResourceInfoDict[actType].leftCount = 0;

                    //client.ClientData.OldResourceInfoDict[actType].roleId = client.ClientData.RoleID;
                    if (client.ClientData.OldResourceInfoDict != null && client.ClientData.OldResourceInfoDict.ContainsKey(actType))
                        client.ClientData.OldResourceInfoDict.Remove(actType);
                }
                else
                {
                    if (client.ClientData.OldResourceInfoDict != null)
                        client.ClientData.OldResourceInfoDict.Clear();
                }
            }
            
            ReplaceDataToDB(client);
            return ret;
        }

        /// <summary>
        ///保存资源找回数据到数据库
        /// </summary>
        /// <param name="client"></param>
        public static void ReplaceDataToDB(GameClient client)
        {
            Dictionary<int, Dictionary<int, OldResourceInfo>> dict = new Dictionary<int, Dictionary<int, OldResourceInfo>>();
            dict[client.ClientData.RoleID] = client.ClientData.OldResourceInfoDict;
            Global.sendToDB<int, byte[]>((int)TCPGameServerCmds.CMD_DB_UPDATE_OLDRESOURCE, DataHelper.ObjectToBytes<Dictionary<int, Dictionary<int, OldResourceInfo>>>(dict), client.ServerId);
        }

        /// <summary>
        /// 从服务器查询资源找回
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static Dictionary<int, OldResourceInfo> ReadResourceGetfromDB(GameClient client)
        {
            
            Dictionary<int, OldResourceInfo> dict = new Dictionary<int,OldResourceInfo>();

            byte[] bytesData = null;
            if (TCPProcessCmdResults.RESULT_FAILED == Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                (int)TCPGameServerCmds.CMD_DB_QUERY_GETOLDRESINFO, string.Format("{0}", client.ClientData.RoleID), out bytesData, client.ServerId))
            {
                return dict; //查询失败
            }

            if (null == bytesData || bytesData.Length <= 6)
            {
                return dict;
            }

            Int32 length = BitConverter.ToInt32(bytesData, 0);

            //获取资源找回字典
            dict = DataHelper.BytesToObject<Dictionary<int, OldResourceInfo>>(bytesData, 6, length - 2);
            return dict;
        }

        /// <summary>
        /// 处理来自客户端的请求消息
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessOldResourceCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                switch ((TCPGameServerCmds)nID)
                {
                    case TCPGameServerCmds.CMD_SPR_QUERY_GETOLDRESINFO:
                        {
                            if (fields.Length != 1)
                            {
                                LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                               (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                                return TCPProcessCmdResults.RESULT_FAILED;
                            }
                            List<OldResourceInfo> infodata = CGetOldResourceManager.GetOldResourceInfo(client);
                            tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<OldResourceInfo>>(infodata, pool, nID);
                            break;
                        }
                    case TCPGameServerCmds.CMD_SPR_GET_OLDRESOURCE:
                        {
                            if (fields.Length != 4)
                            {
                                LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                               (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                                return TCPProcessCmdResults.RESULT_FAILED;
                            }
                            int actType =Global.SafeConvertToInt32( fields[1]);
                            int mOrZ = Global.SafeConvertToInt32(fields[2]);
                            int getModel = Global.SafeConvertToInt32(fields[3]);
                            int ret = 0;
                            ret = CGetOldResourceManager.GiveRoleOldResource(client, actType, mOrZ, getModel);
                            if (ret == 0)
                            {
                                client._IconStateMgr.CheckZiYuanZhaoHui(client);
                                client._IconStateMgr.SendIconStateToClient(client);
                            }
                                
                            string strcmd = string.Format("{0}:{1}:{2}", ret,actType,getModel);
                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                            break;
                        }
                }
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "QueryOldResource", false);
            }
            
            return TCPProcessCmdResults.RESULT_FAILED;
        }
    }
}
