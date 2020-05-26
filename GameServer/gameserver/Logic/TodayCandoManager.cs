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
using GameServer.Logic.JingJiChang;
using GameServer.Logic.WanMota;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    
    public class TodayCandoManager
    {
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
            get {
                lock (_xmlDataMutex)
                {
                    if (_xmlData != null)
                        return _xmlData;
                }
                XElement xml = null;
                try
                {
                    string fileName = "Config/JinRiKeZuo.xml";
                    xml = GeneralCachingXmlMgr.GetXElement(Global.IsolateResPath(fileName));

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

        /// <summary>
        /// 获得剩余次数
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static int GetLeftCountByType(GameClient client, int type,int copyId)
        {
            int leftnum = 0;

            switch ((CandoType)type)
            {
                case CandoType.DailyTask:
                    {
                       
                        DailyTaskData dailyTaskData = Global.FindDailyTaskDataByTaskClass(client, 8);
                        if (null == dailyTaskData)
                        {
                            return Global.MaxDailyTaskNumForMU;
                        } 
                        int maxnum = Global.GetMaxDailyTaskNum(client, 8, dailyTaskData);
                        

                        //获取最大日常任务次数
                        
                        leftnum = maxnum - dailyTaskData.RecNum;
                    }
                    break;
                case CandoType.DemonSquare:
                    {
                        int nMapID = Global.GetDaimonSquareCopySceneIDForRole(client);
                        DaimonSquareDataInfo bcDataTmp = null;

                        Data.DaimonSquareDataInfoList.TryGetValue(nMapID, out bcDataTmp);
                           
                        int nDate = TimeUtil.NowDateTime().DayOfYear;                // 当前时间
                        int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, (int)SpecialActivityTypes.DemoSque);
                        if (nCount < 0)
                            nCount = 0;
                        int nVipLev = client.ClientData.VipLevel;

                        int nNum = 0;
                        int[] nArry = null;
                        nArry = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPEnterDaimonSquareCountAddValue");

                        if (nVipLev > 0 && nArry != null && nArry[nVipLev] > 0)
                        {
                            nNum = nArry[nVipLev];

                        }
                        leftnum = bcDataTmp.MaxEnterNum + nNum - nCount;
                    }
                    break;
                case CandoType.AngelTemple:
                    {
                        DateTime now = TimeUtil.NowDateTime();

                        string nowTime = TimeUtil.NowDateTime().ToString("HH:mm");
                        List<string> timePointsList = GameManager.AngelTempleMgr.m_AngelTempleData.BeginTime;

                        leftnum = 0;
                        for (int i = 0; i < timePointsList.Count; i++)
                        {
                            DateTime staticTime = DateTime.Parse(timePointsList[i]);
                            DateTime perpareTime = staticTime.AddMinutes((double)(GameManager.AngelTempleMgr.m_AngelTempleData.PrepareTime / 60));

                            if ( now <= perpareTime)
                                leftnum += 1;
                        }
                    }
                    break;
                case CandoType.BloodCity:
                    {
                        int nMapID = Global.GetBloodCastleCopySceneIDForRole(client);
                        BloodCastleDataInfo bcDataTmp = null;

                        if (!Data.BloodCastleDataInfoList.TryGetValue(nMapID, out bcDataTmp))
                            break;

                         int nDate       = TimeUtil.NowDateTime().DayOfYear;               // 当前时间
                         int nType       = (int)SpecialActivityTypes.BloodCastle;// 血色堡垒

                         int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, nType);
                         if (nCount < 0)
                             nCount = 0;
                        // VIP检测
                        
                        int nVipLev = client.ClientData.VipLevel;

                        int nNum = 0;
                        int[] nArry = null;
                        nArry = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPEnterBloodCastleCountAddValue");

                        if (nVipLev > 0 && nArry != null && nArry[nVipLev] > 0)
                        {
                            nNum = nArry[nVipLev];
                            
                        }
                        leftnum = bcDataTmp.MaxEnterNum + nNum - nCount;
                    }
                    break;
                case CandoType.Arena:
                    {
                        leftnum = JingJiChangManager.getInstance().GetLeftEnterCount(client);
                        
                    }
                    break;
                case CandoType.OldBattlefield:
                    {
                        //古墓 古战场  剩余时间
                        BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.GuMuTimeLimit);
                        leftnum =(int) (bufferData.BufferVal - bufferData.BufferSecs);
                    }
                    break;
                case CandoType.PartWar:
                    {
                        leftnum = GameManager.BattleMgr.LeftEnterCount();
                    }
                    break;
                case CandoType.PKKing:
                    {
                        leftnum = GameManager.ArenaBattleMgr.LeftEnterCount();
                        
                    }
                    break;
                case CandoType.WanmoTower:
                    {
                        leftnum = 1;
                        if (SweepWanMotaManager.GetSweepCount(client) >= SweepWanMotaManager.nWanMoTaMaxSweepNum)
                            leftnum = 0;
                    }
                    break;
                case CandoType.TaofaTaskCanDo:
                    {
                        DailyTaskData dailyTaskData = Global.FindDailyTaskDataByTaskClass(client, (int)TaskClasses.TaofaTask);
                        if (null == dailyTaskData)
                        {
                            return Global.MaxTaofaTaskNumForMU;
                        }
                        int maxnum = Global.GetMaxDailyTaskNum(client, (int)TaskClasses.TaofaTask, dailyTaskData);

                        //获取最大讨伐任务次数
                        leftnum = maxnum - dailyTaskData.RecNum;
                    }
                    break;
                case CandoType.CrystalCollectCanDo:
                    {
                        //获取剩余水晶采集次数
                        int temp = 0;
                        CaiJiLogic.ReqCaiJiLastNum(client, 0, out temp);
                        leftnum = temp;
                    }
                    break;
                case CandoType.HYSY:
                    {
                        leftnum = HuanYingSiYuanManager.getInstance().GetLeftCount(client);
                    }
                    break;
                default:
                    if (copyId > 0)
                    {
                        SystemXmlItem systemFuBenItem = null;
                        if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(copyId, out systemFuBenItem))
                        {
                            return -1;
                        }
                        int enternum = systemFuBenItem.GetIntValue("EnterNumber");
                        int finishnum = systemFuBenItem.GetIntValue("FinishNumber");

                        int total = enternum < finishnum ? finishnum : enternum;

                        if (type == (int)CandoType.GoldCopy || type == (int)CandoType.EXPCopy)
                        {
                            int[] nAddNum = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJinBiFuBenNum");
                            if (type == (int)CandoType.EXPCopy)
                            {
                                nAddNum = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJinYanFuBenNum");
                            }
                            if (client.ClientData.VipLevel > 0 && client.ClientData.VipLevel <= (int)VIPEumValue.VIPENUMVALUE_MAXLEVEL && nAddNum != null && nAddNum.Length > 0 && nAddNum.Length <= 13)
                            {
                                total = total + nAddNum[client.ClientData.VipLevel];
                            }
                        }

                        FuBenData tmpfubdata = Global.GetFuBenData(client, copyId);
                        if (null != tmpfubdata)
                        {

                            leftnum = total - tmpfubdata.EnterNum;

                        }
                        else
                        {
                            return total;
                        }
                    }
                    break;
            }
            return leftnum;
        }

        /// <summary>
        /// 看是否做过此任务
        /// </summary>
        /// <param name="client"></param>
        /// <param name="taskID"></param>
        /// <returns></returns>
        static bool TaskHasDone(GameClient client, int taskID)
        {
            if (null == client.ClientData.OldTasks)
            {
                return false;
            }

            lock (client.ClientData.OldTasks)
            {
                for (int i = 0; i < client.ClientData.OldTasks.Count; i++)
                {
                    if (taskID <= client.ClientData.OldTasks[i].TaskID)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 获得数据
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        private static List<TodayCandoData> GetRoleCandoData(int typeId, GameClient client)
        {
            List<TodayCandoData> candolist = new List<TodayCandoData>();
            if (xmlData == null)
                return null;
            IEnumerable<XElement> xmlItems = xmlData.Elements(); 
            int iMaxlevel = -1;
            int iMaxchangelife = -1;
            int ifirst = 0;
            int lastSectype = -1;
            Dictionary<int, List<TodayCandoData>> temp = new Dictionary<int, List<TodayCandoData>>();
            foreach (var item in xmlItems)
            {
                if (null!= item)
                {
                    int type =(int) Global.GetSafeAttributeLong(item, "Type");
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
                        //OldTaskData taskData = null;
                        //if (type == typeId)
                        //{
                        //    taskData = Global.FindOldTaskByTaskID(client, taskid);
                        //}

                        //if (null != taskData) //如果任务存在
                        //{
                        //    if (taskData.DoCount > 0)
                        //        condition2 = true;

                        //}
                    }
                    
                    
                    if (type == typeId && condition1 && condition2)
                    {
                        TodayCandoData data = new TodayCandoData();
                        data.ID = (int)Global.GetSafeAttributeLong(item, "ID");
                        int secondtype = (int)Global.GetSafeAttributeLong(item, "SecondType");
                        if (ifirst == 0)
                            lastSectype = secondtype;
                        else if (ifirst != 0 && lastSectype != secondtype)
                        {
                            iMaxchangelife = curmaxchangelife;
                            iMaxlevel = curmaxlevel;
                        }

                        ifirst+=1;
                        if (iMaxchangelife < curmaxchangelife && lastSectype == secondtype)
                        {
                            if (temp.ContainsKey(secondtype))
                            {
                                foreach (var tempitem in temp[secondtype])
                                {
                                    candolist.Remove(tempitem);
                                }
                                temp[secondtype] = new List<TodayCandoData>();
                            }
                            
                            iMaxchangelife = curmaxchangelife;
                            iMaxlevel = curmaxlevel;
                            //lastSectype = 
                        }
                        if (iMaxchangelife == curmaxchangelife && iMaxlevel < curmaxlevel && lastSectype == secondtype)
                        {
                            if (temp.ContainsKey(secondtype))
                            {
                                foreach (var tempitem in temp[secondtype])
                                {
                                    candolist.Remove(tempitem);
                                }
                                temp[secondtype] = new List<TodayCandoData>();
                            }
                            iMaxchangelife = curmaxchangelife;
                            iMaxlevel = curmaxlevel;
                        }
                        
                        int copyId = (int)Global.GetSafeAttributeLong(item, "CodeID");
                        int leftcount = TodayCandoManager.GetLeftCountByType(client, secondtype, copyId);
                        if (leftcount <= 0)
                            continue;
                        
                        data.LeftCount = leftcount;
                       
                        candolist.Add(data);
                        if (temp.ContainsKey(secondtype))
                            temp[secondtype].Add(data);
                        else
                        {
                            temp[secondtype] = new List<TodayCandoData>();
                            temp[secondtype].Add(data);
                        }
                            
                        lastSectype = secondtype;
                    }
                }
            }
            
            return candolist;
        }

        /// <summary>
        /// 处理客户端请求
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
        public static TCPProcessCmdResults ProcessQueryTodayCandoInfo(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                   (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                int typeid = Global.SafeConvertToInt32(fields[1]);
                List<TodayCandoData> datalist = TodayCandoManager.GetRoleCandoData(typeid, client);
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<TodayCandoData>>(datalist, pool, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch(Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "QueryTodayCandoInfo", false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }
    }
}
