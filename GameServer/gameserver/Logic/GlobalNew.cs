using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Interface;
using GameServer.Server;
using System.Windows;
using Server.Tools;
using Server.Data;
using ProtoBuf;
using GameServer.Logic.TuJian;
using Tmsk.Contract;
using HSGameEngine.Tools.AStar;
using GameServer.Logic.FluorescentGem;
using GameServer.Logic.Marriage.CoupleArena;

namespace GameServer.Logic
{
    class GlobalNew
    {
        #region 功能开启

        /// <summary>
        /// 配置的功能是否开启
        /// </summary>
        /// <param name="client"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsGongNengOpened(GameClient client, GongNengIDs id, bool hint = false)
        {
            SystemXmlItem xmlItem = null;
            if (GameManager.SystemSystemOpen.SystemXmlItemDict.TryGetValue((int)id, out xmlItem))
            {
                int trigger = xmlItem.GetIntValue("TriggerCondition");
                // 等级
                if (trigger == 1)
                {
                    int[] paramArray = xmlItem.GetIntArrayValue("TimeParameters");
                    if (paramArray.Length == 2)
                    {
                        if (Global.GetUnionLevel(paramArray[0], paramArray[1]) > Global.GetUnionLevel(client))
                        {
                            if (hint)
                            {
                                string msg = string.Format(Global.GetLang("开启此功能需要达到【{0}】转【{1}】级"), paramArray[0], paramArray[1]);
                                GameManager.ClientMgr.NotifyHintMsg(client, msg);
                            }

                            return false;
                        }
                    }
                    return true;
                }
                // 完成指定任务
                else if (trigger == 7)
                {
                    int taskId = xmlItem.GetIntValue("TimeParameters");
                    if (client.ClientData.MainTaskID < taskId)
                    {
                        if (hint)
                        {
                            string msg = string.Format(Global.GetLang("开启此功能需要完成主线任务【{0}】"), GlobalNew.GetTaskName(taskId));
                            GameManager.ClientMgr.NotifyHintMsg(client, msg);
                        }

                        return false;
                    }
                    return true;
                }
                // 羽毛阶数
                else if (trigger == 14)
                {
                    string str = xmlItem.GetStringValue("TimeParameters");
                    if (string.IsNullOrEmpty(str)) return true;
                    string[] fields = str.Split(',');
                    if (fields.Length != 2) return true;

                    int suit = Convert.ToInt32(fields[0]);
                    int star = Convert.ToInt32(fields[1]);

                    return ((client.ClientData.MyWingData.WingID > suit)
                        || (client.ClientData.MyWingData.WingID == suit && client.ClientData.MyWingData.ForgeLevel >= star));
                }
                // 成就阶数
                else if (trigger == 15)
                {
                    if (client.ClientData.ChengJiuLevel < xmlItem.GetIntValue("TimeParameters"))
                    {
                        return false;
                    }
                }
                // 军衔阶数
                else if (trigger == 16)
                {
                    int junxian = GameManager.ClientMgr.GetShengWangLevelValue(client);
                    if (junxian < xmlItem.GetIntValue("TimeParameters"))
                    {
                        return false;
                    }
                }
                else if (trigger == 20)
                {
                    int bangHuiLevel = Global.GetBangHuiLevel(client);
                    if (bangHuiLevel < xmlItem.GetIntValue("TimeParameters"))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //刷新当前的功能开启状态，处理有关逻辑
        public static void RefreshGongNeng(GameClient client)
        {
            CaiJiLogic.InitRoleDailyCaiJiData(client, false, false);
            HuanYingSiYuanManager.getInstance().InitRoleDailyHYSYData(client);
            Global.InitRoleDailyTaskData(client, false);
            // 检测触发开启守护之灵
            GuardStatueManager.Instance().OnTaskComplete(client);

            // 检测开启梅林魔法书 [XSea 2015/6/23]
            GameManager.MerlinMagicBookMgr.InitMerlinMagicBook(client);

            // 魂石系统
            SoulStoneManager.Instance().CheckOpen(client);

            ZhengBaManager.Instance().CheckGongNengCanOpen(client);

            FundManager.initFundData(client);

            CoupleArenaManager.Instance().CheckGongNengOpen(client);
        }

        public static int GetFuBenTabNeedTask(int fuBenTabId)
        {
            int needTaskId = 0;

            if (!Data.FuBenNeedDict.TryGetValue(fuBenTabId, out needTaskId))
            {
                needTaskId = 0;
            }

            return needTaskId;
        }

        public static bool IsExtraGongNengOpen(GameClient client, ExtraGongNengIds extGongId)
        {
            int needLevel = 0;
            int needTask = 0;
            int needVip = 0;
            if (extGongId == ExtraGongNengIds.DiaoXiangMoBai)
            {
                needLevel = (int)GameManager.systemParamsList.GetParamValueIntByName("MoBaiLevel");
            }

            if (needLevel > 0 && needLevel > Global.GetUnionLevel(client))
            {
                return false;
            }

            if (needTask > 0 && needTask > client.ClientData.MainTaskID)
            {
                return false;
            }

            if (needVip > 0 && needVip > client.ClientData.VipLevel)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取指定任务编号的任务名称
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public static string GetTaskName(int taskId)
        {
            //修改目标NPC的状态
            SystemXmlItem systemTask = null;
            if (!GameManager.SystemTasksMgr.SystemXmlItemDict.TryGetValue(taskId, out systemTask))
            {
                return taskId.ToString();
            }

            return systemTask.GetStringValue("Title");
        }

        #endregion 功能开启

        #region 外挂与角色数据校验

        /// <summary>
        /// 输出角色现有属性
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
        public static string PrintRoleProps(string otherRoleIdOrName)
        {
            string rolePropsStr = null;
            int roleId;
            bool find = false;
            GameClient otherClient;

            try
            {
                roleId = RoleName2IDs.FindRoleIDByName(otherRoleIdOrName);
                if (-1 == roleId)
                {
                    if (!int.TryParse(otherRoleIdOrName, out roleId))
                    {
                        return rolePropsStr;
                    }
                }

                otherClient = GameManager.ClientMgr.FindClient(roleId);
                if (null == otherClient)
                {
                    return rolePropsStr;
                }

                StringBuilder sb = new StringBuilder();
                Global.PrintSomeProps(otherClient, ref sb);
                rolePropsStr = sb.ToString();
            }
            catch (System.Exception ex)
            {
            	
            }

            return rolePropsStr;
        }

        #endregion

        #region 任务相关

        public class NpcCircleTaskData
        {
            public int taskclass = 0;
            public int oldTaskID = 0;
            public List<int> NpcAttachedTaskID = new List<int>();
            public int DoRandomTaskID(GameClient client)
            {
                if (0 == NpcAttachedTaskID.Count)
                    return -1;

                if (taskclass == (int)TaskClasses.DailyTask)
                {
                    return Global.GetDailyCircleTaskIDBaseChangeLifeLev(client);
                }
                else if (taskclass == (int)TaskClasses.TaofaTask)
                {
                    return Global.GetTaofaTaskIDBaseChangeLifeLev(client);
                }
                else
                {
                    int randIndex = Global.GetRandomNumber(0, NpcAttachedTaskID.Count);
                    return NpcAttachedTaskID[randIndex];
                }
            }
        }

        public static bool GetNpcTaskData(GameClient client, int extensionID, NPCData npcData)
        {
            //再查询属于指定NPC的是否有可以接的任务(要判断前置任务和后置任务等条件，以及级别等条件)

            //查询NPC上挂载的任务
            List<int> tasksList = null;
            if (!GameManager.NPCTasksMgr.SourceNPCTasksDict.TryGetValue(extensionID, out tasksList))
                return false;   //npc上没任务
            if (0 == tasksList.Count)
                return false;   //npc上没任务

            Dictionary<int, NpcCircleTaskData> all_circleTask = null;

            //遍历npc上挂载的所有任务，将跑环任务暂存起来稍后处理，非跑环任务直接处理
            for (int i = 0; i < tasksList.Count; i++)
            {
                int taskID = tasksList[i];
                SystemXmlItem systemTask = null;
                if (!GameManager.SystemTasksMgr.SystemXmlItemDict.TryGetValue(taskID, out systemTask))
                {
                    continue;   //配置错误，没这个任务
                }

                int taskClass = systemTask.GetIntValue("TaskClass");

                if (taskClass >= (int)TaskClasses.CircleTaskStart && taskClass <= (int)TaskClasses.CircleTaskEnd) //如果是跑环任务
                {
                    // 是否能接这种跑环任务
                    if (!Global.CanTaskPaoHuanTask(client, taskClass))
                        continue;
                    //判断是否是能接的新任务
                    if (!Global.CanTakeNewTask(client, taskID, systemTask))
                        continue;
                    
                    //本次请求内是否处理过这种跑环任务
                    NpcCircleTaskData circletask = null;
                    if (null == all_circleTask || !all_circleTask.TryGetValue(taskClass, out circletask))
                    {
                        circletask = new NpcCircleTaskData();
                        circletask.taskclass = taskClass;

                        //之前随机的任务ID
                        circletask.oldTaskID = PaoHuanTasksMgr.FindPaoHuanHistTaskID(client.ClientData.RoleID, taskClass);
                        if (circletask.oldTaskID >= 0)
                        {
                            //验证还是否能继续接
                            //判断是否是能接的新任务
                            if (!Global.CanTakeNewTask(client, circletask.oldTaskID))
                            {
                                circletask.oldTaskID = -1;
                            }
                        }

                        if (null == all_circleTask)
                            all_circleTask = new Dictionary<int, NpcCircleTaskData>();

                        all_circleTask[taskClass] = circletask;
                    }
                    //添加到列表
                    if (null != circletask)
                        circletask.NpcAttachedTaskID.Add(taskID);
                }
                else //非跑环任务，比如主线任务
                {
                    //判断是否是能接的新任务
                    if (!Global.CanTakeNewTask(client, taskID, systemTask))
                        continue;

                    //记录这个任务
                    if (null == npcData.NewTaskIDs)
                    {
                        npcData.NewTaskIDs = new List<int>();
                    }

                    npcData.NewTaskIDs.Add(taskID);

                    if ((int)TaskClasses.SpecialTask == taskClass) //如果是循环任务，要计算已经做过的次数
                    {
                        OldTaskData oldTaskData = Global.FindOldTaskByTaskID(client, tasksList[i]);
                        int doneCount = (null == oldTaskData) ? 0 : oldTaskData.DoCount;

                        if (null == npcData.NewTaskIDsDoneCount)
                        {
                            npcData.NewTaskIDsDoneCount = new List<int>();
                        }
                        npcData.NewTaskIDsDoneCount.Add(doneCount);
                    }
                    else
                    {
                        if (null == npcData.NewTaskIDsDoneCount)
                        {
                            npcData.NewTaskIDsDoneCount = new List<int>();
                        }
                        npcData.NewTaskIDsDoneCount.Add(0);
                    }
                }
            }

            //处理刚才暂存的跑环任务

            if (null == all_circleTask)
                return true;

            foreach (var circletask in all_circleTask)
            {
                bool needRandom = false;
                if (-1 != circletask.Value.oldTaskID)
                {   //之前随机过任务ID

                    if (0 == circletask.Value.NpcAttachedTaskID.Count)
                        continue;   //npc上没这种跑环任务

                    //验证之前随机到的任务是否是存在的
                    if (-1 != circletask.Value.NpcAttachedTaskID.IndexOf(circletask.Value.oldTaskID))
                    {
                        //记录这个任务
                        if (null == npcData.NewTaskIDs)
                        {
                            npcData.NewTaskIDs = new List<int>();
                        }
                        npcData.NewTaskIDs.Add(circletask.Value.oldTaskID);
                        if (null == npcData.NewTaskIDsDoneCount)
                        {
                            npcData.NewTaskIDsDoneCount = new List<int>();
                        }
                        npcData.NewTaskIDsDoneCount.Add(0);
                    }
                    else
                    {
                        needRandom = true;
                    }
                }
                else
                {
                    needRandom = true;
                }

                if (needRandom)
                {
                    int randTaskId = circletask.Value.DoRandomTaskID(client);
                    if (-1 != randTaskId)
                    {
                        //记录这个任务
                        if (null == npcData.NewTaskIDs)
                        {
                            npcData.NewTaskIDs = new List<int>();
                        }
                        npcData.NewTaskIDs.Add(randTaskId);
                        if (null == npcData.NewTaskIDsDoneCount)
                        {
                            npcData.NewTaskIDsDoneCount = new List<int>();
                        }
                        npcData.NewTaskIDsDoneCount.Add(0);
                        PaoHuanTasksMgr.SetPaoHuanHistTaskID(client.ClientData.RoleID, circletask.Value.taskclass, randTaskId);
                    }
                }
            }

            return true;
        }

        public static bool GetNpcFunctionData(GameClient client, int extensionID, NPCData npcData, SystemXmlItem systemNPC)
        {
            if (null == systemNPC)
                return false;

            //查询是否有系统功能
            string operaIDsByString = systemNPC.GetStringValue("Operations");
            operaIDsByString.Trim();
            if (operaIDsByString != "")
            {
                int[] operaIDsByInt = Global.StringArray2IntArray(operaIDsByString.Split(','));
                if (null == npcData.OperationIDs)
                {
                    npcData.OperationIDs = new List<int>();
                }

                for (int i = 0; i < operaIDsByInt.Length; i++)
                {
                    //过滤功能
                    if (Global.FilterNPCOperationByID(client, operaIDsByInt[i], extensionID))
                    {
                        continue;
                    }

                    npcData.OperationIDs.Add(operaIDsByInt[i]);
                }
            }

            //查询是否有NPC功能脚本
            string scriptIDsByString = systemNPC.GetStringValue("Scripts");
            if (null != scriptIDsByString)
            {
                scriptIDsByString = scriptIDsByString.Trim();
            }

            if (!string.IsNullOrEmpty(scriptIDsByString))
            {
                int[] scriptIDsByInt = Global.StringArray2IntArray(scriptIDsByString.Split(','));
                if (null == npcData.ScriptIDs)
                {
                    npcData.ScriptIDs = new List<int>();
                }

                for (int i = 0; i < scriptIDsByInt.Length; i++)
                {
                    int errorCode = 0;

                    //过滤功能脚本
                    if (Global.FilterNPCScriptByID(client, scriptIDsByInt[i], out errorCode))
                    {
                        continue;
                    }

                    npcData.ScriptIDs.Add(scriptIDsByInt[i]);
                }
            }

            return true;
        }

        #endregion 任务相关

        #region 数据库服务器连接

        public static TCPClient PopGameDbClient(int serverId, int poolId)
        {
#if BetaConfig
            if (serverId <= 0)
#else
            if (serverId <= 0 || serverId == GameManager.ServerId)
#endif
            {
                if (poolId == 0)
                {
                    return Global._TCPManager.tcpClientPool.Pop();
                }
                else// if(poolId == 1)
                {
                    return Global._TCPManager.tcpLogClientPool.Pop();
                }
            }
            else
            {
                return KuaFuManager.getInstance().PopGameDbClient(serverId, poolId);
            }
        }

        public static void PushGameDbClient(int serverId, TCPClient tcpClient, int poolId)
        {
#if BetaConfig
            if (serverId <= 0)
#else
            if (serverId <= 0 || serverId == GameManager.ServerId)
#endif
            {
                if (poolId == 0)
                {
                    Global._TCPManager.tcpClientPool.Push(tcpClient);
                }
                else// if(poolId == 1)
                {
                    Global._TCPManager.tcpLogClientPool.Push(tcpClient);
                }
            }
            else
            {
                KuaFuManager.getInstance().PushGameDbClient(serverId, tcpClient, poolId);
            }
        }

        #endregion 数据库服务器连接

        #region 跨服

        public static void UpdateKuaFuRoleDayLogData(int serverId, int roleId, DateTime now, int zoneId, int signUpCount, int startGameCount, int successCount, int faildCount, int gameType)
        {
            Global.SendToDB<RoleKuaFuDayLogData>((int)TCPGameServerCmds.CMD_LOGDB_UPDATE_ROLE_KUAFU_DAY_LOG, new RoleKuaFuDayLogData()
            {
                RoleID = roleId,
                Day = now.Date.ToString("yyyy-MM-dd"),
                ZoneId = zoneId,
                SignupCount = signUpCount,
                StartGameCount = startGameCount,
                SuccessCount = successCount,
                FaildCount = faildCount,
                GameType = gameType,
            }, serverId);
        }

        public static void RecordSwitchKuaFuServerLog(GameClient client)
        {
            ushort LastMapCode = 0, LastPosX = 0, LastPosY = 0;
            if (SceneUIClasses.Normal == Global.GetMapSceneType(client.ClientData.MapCode))
            {
                LastMapCode = (ushort)client.CurrentMapCode;
                LastPosX = (ushort)client.CurrentGrid.X;
                LastPosY = (ushort)client.CurrentGrid.Y;
            }

            Global.ModifyMapRecordData(client, LastMapCode, LastPosX, LastPosY, (int)MapRecordIndexes.InitGameMapPostion);

            KuaFuServerLoginData kuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);
            LogManager.WriteLog(LogTypes.Error, string.Format("RoleId={0},GameId={1},SrcServerId={2},KfIp={3},KfPort={4}", kuaFuServerLoginData.RoleId, kuaFuServerLoginData.GameId, kuaFuServerLoginData.ServerId, kuaFuServerLoginData.ServerIp, kuaFuServerLoginData.ServerPort));
        }

        #endregion 跨服


        #region 寻路

        private static Stack<PathFinderFast> _pathStack = new Stack<PathFinderFast>();
        public static List<int[]> FindPath(Point startPoint,Point endPoint,int mapCode)
        {
            GameMap gameMap = GameManager.MapMgr.DictMaps[mapCode];
            if (null == gameMap)
            {
                return null;
            }

            PathFinderFast pathFinderFast = null;
            if (_pathStack.Count<=0)
            {
                pathFinderFast = new PathFinderFast(gameMap.MyNodeGrid.GetFixedObstruction())
                {
                    Formula = HeuristicFormula.Manhattan,
                    Diagonals = true,
                    HeuristicEstimate = 2,
                    ReopenCloseNodes = true,
                    SearchLimit = 2147483647,
                    Punish = null,
                    MaxNum = Global.GMax(gameMap.MapGridWidth, gameMap.MapGridHeight),
                };
            }
            else
            {
                pathFinderFast = _pathStack.Pop();
            }

            startPoint.X =  gameMap.CorrectWidthPointToGridPoint((int)startPoint.X) / gameMap.MapGridWidth;
            startPoint.Y = gameMap.CorrectHeightPointToGridPoint((int)startPoint.Y) / gameMap.MapGridHeight;
            endPoint.X = gameMap.CorrectWidthPointToGridPoint((int)endPoint.X) / gameMap.MapGridWidth;
            endPoint.Y = gameMap.CorrectHeightPointToGridPoint((int)endPoint.Y) / gameMap.MapGridHeight;

            pathFinderFast.EnablePunish = false;
            List<PathFinderNode> nodeList = pathFinderFast.FindPath(startPoint, endPoint);
            if (null == nodeList || nodeList.Count <= 0)
            {
                return null;
            }

            List<int[]> path = new List<int[]>();
            for (int i = 0; i < nodeList.Count; i++)
            {
                path.Add(new int[] { nodeList[i].X, nodeList[i].Y });
            }

            //push

            return path;
        }


        #endregion

    }   //class
}   //namespace
