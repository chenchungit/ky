using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Tools;
using Server.Data;
using System.Xml.Linq;
using GameServer.Core.GameEvent;
using GameServer.Server;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Server.CmdProcesser;
using GameServer.Core.Executor;

namespace GameServer.Logic.Building
{
    public class BuildingManager : IManager, ICmdProcessorEx
    {
        /// <summary>
        /// 默认的建筑物开发时间
        /// </summary>
        public static readonly string ConstBuildTime = "0000-00-00 00:00:00";

        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object RandomTaskMutex = new object();

        /// <summary>
        /// 静态实例
        /// </summary>
        private static BuildingManager instance = new BuildingManager();
        public static BuildingManager getInstance()
        {   
            return instance;
        }

        // 建筑物数据
        // ID vs BuildingConfigData
        protected Dictionary<int, BuildingConfigData> BuildDict = new Dictionary<int, BuildingConfigData>();

        // 建筑物开发任务数据
        // ID vs BuildingTaskConfigData
        protected Dictionary<int, BuildingTaskConfigData> BuildTaskDict = new Dictionary<int, BuildingTaskConfigData>();

        // 建筑升级产出表 BuildLevel.xml
        // IDVsLev vs BuildingTaskConfigData
        protected Dictionary<KeyValuePair<int, int>, BuildingLevelConfigData> BuildLevelDict = new Dictionary<KeyValuePair<int, int>, BuildingLevelConfigData>();

        // 建筑总等级奖励表 BuildLevelAward.xml
        // ID vs BuildingLevelAwardConfigData
        protected Dictionary<int, BuildingLevelAwardConfigData> BuildLevelAwardDict = new Dictionary<int, BuildingLevelAwardConfigData>();

        // 免费队列上限
        public int ManorFreeQueueNumMax = 0;

        // 收费队列上限
        public int ManorQueueNumMax = 0;

        // 一键完成收费 石抵换时间系数
        public int ManorQuickFinishNum = 0;

        // 刷新开发花费钻石
        public int ManorRandomTaskPrice = 0;

        // 开启收费队列花费钻石
        public int ManorQueuePrice = 0;

        // 配置文件目录
        private const string Build_fileName = "Config/Manor/Build.xml";
        private const string BuildTask_fileName = "Config/Manor/BuildTask.xml";
        private const string BuildLevel_fileName = "Config/Manor/BuildLevel.xml";
        private const string BuildLevelAward_fileName = "Config/Manor/BuildLevelAward.xml";

#region 接口实现

        public bool initialize()
        {
            if (!InitConfig())
            {
                return false;
            }
            return true;
        }
            
        public bool startup()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_GET_LIST, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_EXCUTE, 3, 3, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_FINISH, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_REFRESH, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_GET_ALLLEVEL_AWARD, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_GET_AWARD, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_OPEN_QUEUE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_GET_QUEUE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_GET_STATE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_BUILD_GET_ALLLEVEL_AWARD_STATE, 1, 1, getInstance());
            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            bool isOpen = GlobalNew.IsGongNengOpened(client, GongNengIDs.Building);
            if(!isOpen)
            {
                // 返回错误信息
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    StringUtil.substitute(Global.GetLang("功能未开放")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);

                return true;
            }

            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_GET_LIST:
                    return ProcessBuildGetListCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_EXCUTE:
                    return ProcessBuildExcuteCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_FINISH:
                    return ProcessBuildFinishCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_REFRESH:
                    return ProcessBuildRefreshCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_GET_ALLLEVEL_AWARD:
                    return ProcessBuildGetAllLevelAwardCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_GET_AWARD:
                    return ProcessBuildGetAwardCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_OPEN_QUEUE:
                    return ProcessBuildOpenQueueCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_GET_QUEUE:
                    return ProcessBuildGetQueueCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_GET_STATE:
                    return ProcessBuildGetStateCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_BUILD_GET_ALLLEVEL_AWARD_STATE:
                    return ProcessBuildGetAllLevelAwardStateCmd(client, nID, bytes, cmdParams);
            }
            return true;
        }

#endregion

        /// <summary>
        /// 寻找对应的建筑物数据
        /// </summary>
        public BuildingData GetBuildingData(GameClient client, int BuildID)
        {
            BuildingData BuildData = null;
            for (int i = 0; i < client.ClientData.BuildingDataList.Count; ++i)
            {
                if (client.ClientData.BuildingDataList[i].BuildId == BuildID)
                {
                    BuildData = client.ClientData.BuildingDataList[i];
                    break;
                }
            }
            return BuildData;
        }

        /// <summary>
        /// 处理角色上线
        /// </summary>
        public void OnRoleLogin(GameClient client)
        {
            try
            {
                // 如果1.8的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                    return;

                // 异常数据
                if (null == client.ClientData.BuildingDataList)
                    return;

                // 如果尚未生成领地数据
                if (client.ClientData.BuildingDataList.Count == 0)
                {
                    GeneralBuildingData(client);
                }
                // 数据缺失
                else if (client.ClientData.BuildingDataList.Count < BuildDict.Count)
                {
                    GeneralBuildingData(client);
                }

                // 数据完整性检查
                if (GlobalNew.IsGongNengOpened(client, GongNengIDs.Building))
                {
                    BuildingDataChecking(client);
                }
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }
        }

        /// <summary>
        /// 随机开放方式
        /// </summary>
        public void RandomBuildTaskData(GameClient client, int BuildID, BuildingData myBuildData, bool ConstRefresh = false)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return;

            BuildingConfigData myBCData = null;
            BuildDict.TryGetValue(BuildID, out myBCData);
            if (null == myBCData)
                return;

            myBuildData.BuildId = BuildID;
            myBuildData.BuildTime = ConstBuildTime;

            List<BuildingRandomData> RandomList = null;
            if (true == ConstRefresh)
            {
                RandomList = myBCData.RandomList;
            }
            else
            {
                RandomList = myBCData.FreeRandomList;
            }

            lock (RandomTaskMutex)
            {
                // 随机任务
                BuildingQuality quality = BuildingQuality.Null;
                quality = RandomQualityByList(RandomList);
                myBuildData.TaskID_1 = RandomBuildTask(myBuildData.BuildId, quality);

                quality = RandomQualityByList(RandomList);
                myBuildData.TaskID_2 = RandomBuildTask(myBuildData.BuildId, quality);

                quality = RandomQualityByList(RandomList);
                myBuildData.TaskID_3 = RandomBuildTask(myBuildData.BuildId, quality);

                // 重置随机数
                ResetRandSkipVavle();
            }
        }

        /// <summary>
        /// 重置随机辅助数
        /// </summary>
        public void ResetRandSkipVavle()
        {
            foreach (var kvp in BuildTaskDict)
            {
                if (true == kvp.Value.RandSkip)
                {
                    kvp.Value.RandSkip = false;
                }
            }
        }

        /// <summary>
        /// 随机一个开发任务出来 不能重复
        /// </summary>
        public int RandomBuildTask(int BuildID, BuildingQuality quality)
        {
            List<BuildingTaskConfigData> listBuildTask = new List<BuildingTaskConfigData>();

            // 寻找满足要求的开发任务队列
            foreach(var kvp in BuildTaskDict)
            {
                if(false == kvp.Value.RandSkip && kvp.Value.BuildID == BuildID && kvp.Value.quality == quality)
                {
                    listBuildTask.Add(kvp.Value);
                }
            }

            // 随机一个出来 要求不能重复
            int rate = Global.GetRandomNumber(0, listBuildTask.Count);
            if (listBuildTask.Count != 0)
            {
                listBuildTask[rate].RandSkip = true;
                return listBuildTask[rate].TaskID;
            }

            return 0;
        }

        /// <summary>
        /// 随机一个品质出来 可以重复
        /// </summary>
        public BuildingQuality RandomQualityByList(List<BuildingRandomData> RandomList)
        {
            // 随机累计数上限
            double rateEnd = 0.0d;

            // 随机一个品质出来
            double rate = (double)Global.GetRandomNumber(1, 101) / 100;
            for (int i = 0; i < RandomList.Count; ++i)
            {
                rateEnd += RandomList[i].rate;
                if (rate <= rateEnd)
                    return RandomList[i].quality;
            }

            return BuildingQuality.Null;
        }

        /// <summary>
        /// 刷新领地Log数据
        /// </summary>
        public void UpdateBuildingLogDB(GameClient client, BuildingLogType BuildLogType)
        {
            EventLogManager.AddRoleEvent(client, OpTypes.Trace, OpTags.Building, LogRecordType.IntValue, (int)BuildLogType);
        }

        /// <summary>
        /// 刷新领地数据
        /// </summary>
        public void UpdateBuildingDataDB(GameClient client, BuildingData myBuildData)
        {
            if (null == myBuildData)
                return;
        
            string buildtime = null;
            if(!string.IsNullOrEmpty(myBuildData.BuildTime))
            {
                buildtime = myBuildData.BuildTime.Replace(':', '$');
            }

            string cmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}",
                                    client.ClientData.RoleID, myBuildData.BuildId, myBuildData.BuildLev,
                                    myBuildData.BuildExp, buildtime, myBuildData.TaskID_1, myBuildData.TaskID_2, myBuildData.TaskID_3);
            Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_BUILDING_DATA, cmd, client.ServerId);
        }

        /// <summary>
        /// 领地数据一致性检查
        /// 由于数据库凌晨备份，导致RoleParamName.BuildQueueData存储失败，
        /// 队列数据 & 建筑物数据不一致造成领地系统异常。
        /// </summary>
        public void BuildingDataChecking(GameClient client)
        {
            try
            {
                // 获得队列数据
                List<BuildTeam> BuildQueue = GetBuildingQueueData(client);
                List<BuildTeam> BuildQueueDel = new List<BuildTeam>();
                List<BuildTeam> BuildQueueAdd = new List<BuildTeam>();

                // 相互纠错
                for (int n = 0; n < client.ClientData.BuildingDataList.Count; ++n)
                {
                    BuildTeam BuildTeamData = null;
                    BuildingData BuildData = client.ClientData.BuildingDataList[n];

                    // MUFUNC-769 1.8.0-领地随机任务极小概率出现0的bug
                    if (BuildData.TaskID_1 == 0 || BuildData.TaskID_2 == 0 || BuildData.TaskID_3 == 0)
                    {
                        RandomBuildTaskData(client, BuildData.BuildId, BuildData);
                        UpdateBuildingDataDB(client, BuildData); // 更新数据库
                    }

                    // BuildData空闲状态
                    if (0 == string.Compare(BuildData.BuildTime, ConstBuildTime))
                    {
                        BuildTeamData = BuildQueue.Find(_da => _da.BuildID == BuildData.BuildId);
                        if (null != BuildTeamData) BuildQueueDel.Add(BuildTeamData);
                        continue;
                    }

                    // BuildData开发状态
                    BuildTeamData = BuildQueue.Find(_da => _da.BuildID == BuildData.BuildId);
                    if (null == BuildTeamData)
                    {
                        BuildQueueAdd.Add(new BuildTeam 
                        {
                            BuildID = BuildData.BuildId,
                            TaskID = BuildData.TaskID_1
                        });
                    }
                }

                // 建筑物数据显示未开发，但队列数据存在有效数据
                foreach(var dat in BuildQueueDel)
                {
                    RemoveBuildingQueueData(client, dat.BuildID, dat.TaskID);
                    LogManager.WriteLog(LogTypes.Data, string.Format("领地数据检查RemoveBuildingQueueData, RoleID={0}, RoleName={1}, BuildID={2}, TaskID={3}",
                        client.ClientData.RoleID, client.ClientData.RoleName, dat.BuildID, dat.TaskID));
                }

                // 建筑物数据显示开发中，但队列数据无对应数据
                foreach (var dat in BuildQueueAdd)
                {
                    if (!AddBuildingQueueData(client, dat.BuildID, dat.TaskID))
                    {
                        // 建筑物数据本身异常 不考虑
                    }
                    else
                    {
                        LogManager.WriteLog(LogTypes.Data, string.Format("领地数据检查AddBuildingQueueData, RoleID={0}, RoleName={1}, BuildID={2}, TaskID={3}",
                            client.ClientData.RoleID, client.ClientData.RoleName, dat.BuildID, dat.TaskID));
                    }
                }
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
        }

        /// <summary>
        /// 生成领地数据
        /// </summary>
        public void GeneralBuildingData(GameClient client)
        {
            foreach (var kvp in BuildDict)
            {
                if (null != GetBuildingData(client, kvp.Value.BuildID))
                    continue;

                BuildingData myBuildData = new BuildingData();
                RandomBuildTaskData(client, kvp.Value.BuildID, myBuildData);

                // 更新数据库
                UpdateBuildingDataDB(client, myBuildData);

                // 生成数据
                if (null != myBuildData)
                    client.ClientData.BuildingDataList.Add(myBuildData);
            }
        }

        /// <summary>
        /// 解析队列数据
        /// </summary>
        public List<BuildTeam> GetBuildingQueueData(GameClient client)
        {
            // 开发队列数据 按顺序排列
            List<BuildTeam> BuildQueue = new List<BuildTeam>();

            // 尝试解析队列数据 openpaynum,queueid|builid|taskid,queueid|buildid|taskid
            string strKey = RoleParamName.BuildQueueData;
            string BuildingQueueData = Global.GetRoleParamByName(client, strKey);
            if (!string.IsNullOrEmpty(BuildingQueueData))
            {
                string[] Filed = BuildingQueueData.Split(',');
                if (1 >= Filed.Length)
                    return BuildQueue;

                for (int i = 1; i < Filed.Length; ++i)
                {
                    string[] TypeVsBuilID = Filed[i].Split('|');
                    if (TypeVsBuilID.Length != 3)
                        continue;

                    BuildTeam teamData = new BuildTeam();
                    teamData._TeamType = (BuildTeamType)Convert.ToInt32(TypeVsBuilID[0]);
                    teamData.BuildID = Convert.ToInt32(TypeVsBuilID[1]);
                    teamData.TaskID = Convert.ToInt32(TypeVsBuilID[2]);
                    BuildQueue.Add(teamData);
                }
            }

            return BuildQueue;
        }

        /// <summary>
        /// 获取开发状态
        /// </summary>
        public BuildingState GetBuildState(GameClient client, int BuildID, int TaskID)
        {
            // 寻找指定开发任务数据
            BuildingTaskConfigData TaskConfigData;
            BuildTaskDict.TryGetValue(TaskID, out TaskConfigData);
            if (null == TaskConfigData)
            {
                return BuildingState.EBS_Null;
            }

            // 遍历建筑物 计算开发状态
            for (int n = 0; n < client.ClientData.BuildingDataList.Count; ++n)
            {
                BuildingData BuildData = client.ClientData.BuildingDataList[n];
                if (BuildData.BuildTime != ConstBuildTime && BuildID == BuildData.BuildId)
                {
                    DateTime BuildTime;
                    DateTime.TryParse(BuildData.BuildTime, out BuildTime);
                    long SpendTicks = (TimeUtil.NowDateTime().Ticks / 10000) - (BuildTime.Ticks / 10000);
                    long SpendSecondes = SpendTicks / 1000;

                    // 获取研发任务数据
                    long SubTimeSecondes = TaskConfigData.Time * 60 - SpendSecondes;
                    if(SubTimeSecondes <= 0)
                    {
                        return BuildingState.EBS_Finish;
                    }
                    else
                    {
                        return BuildingState.EBS_InBuilding;
                    }
                }
            }

            return BuildingState.EBS_Null;
        }

        /// <summary>
        /// 获取各队列开发数
        /// </summary>
        public void GetTaskNumInEachTeam(GameClient client, out int free, out int pay)
        {
            free = 0;
            pay = 0;

            // 获得队列数据
            List<BuildTeam> BuildQueue = GetBuildingQueueData(client);

            // 统计
            for (int i = 0; i < BuildQueue.Count; ++i)
            {
                if (BuildTeamType.FreeTeam == BuildQueue[i]._TeamType)
                {
                    ++free;
                }
                else if (BuildTeamType.PayTeam == BuildQueue[i]._TeamType)
                {
                    ++pay;
                }
            }
        }

        /// <summary>
        /// 获取付费队列开启数
        /// </summary>
        public int GetOpenPayTeamNum(GameClient client)
        {
            // 尝试解析队列数据 openpaynum,queueid|builid|taskid,queueid|buildid|taskid
            string strKey = RoleParamName.BuildQueueData;
            string BuildingQueueData = Global.GetRoleParamByName(client, strKey);
            if (!string.IsNullOrEmpty(BuildingQueueData))
            {
                string[] Filed = BuildingQueueData.Split(',');
                if (0 == Filed.Length)
                    return 0;

                return Convert.ToInt32(Filed[0]);
            }
            return 0;
        }

        /// <summary>
        /// 存储队列数据
        /// </summary>
        public void SaveBuildingQueueData(GameClient client, List<BuildTeam> BuildQueue)
        {
            string BuildingQueueData = ""; // openpaynum,queueid|builid|taskid,queueid|buildid|taskid

            BuildingQueueData += GetOpenPayTeamNum(client);

            for (int i = 0; i < BuildQueue.Count; ++i)
            {
                if (BuildQueue[i].BuildID == 0)
                    continue;

                BuildingQueueData += ',';
                BuildingQueueData += (int)BuildQueue[i]._TeamType;
                BuildingQueueData += '|';
                BuildingQueueData += BuildQueue[i].BuildID;
                BuildingQueueData += '|';
                BuildingQueueData += BuildQueue[i].TaskID;
            }

            string strKey = RoleParamName.BuildQueueData;
            Global.SaveRoleParamsStringToDB(client, strKey, BuildingQueueData, true);
        }

        /// <summary>
        /// openpaynum
        /// </summary>
        public void ModifyOpenPayNum(GameClient client, int chg)
        {
            // 付费队列开启数量
            int OpenPayNum = 0;

            // 尝试解析队列数据 openpaynum,queueid|builid|taskid,queueid|buildid|taskid
            string strKey = RoleParamName.BuildQueueData;
            string BuildingQueueData = Global.GetRoleParamByName(client, strKey);
            if (string.IsNullOrEmpty(BuildingQueueData))
            {
                BuildingQueueData = "0"; // openpaynum = 0
            }

            string[] Filed = BuildingQueueData.Split(',');
            if (0 == Filed.Length)
                return;

            OpenPayNum = Convert.ToInt32(Filed[0]) + chg;

            // fix  
            if (OpenPayNum < 0)
                OpenPayNum = 0;

            // change
            BuildingQueueData = Convert.ToString(OpenPayNum);

            // queueid|builid|taskid,queueid|buildid|taskid
            for (int i = 1; i < Filed.Length; ++i )
            {
                BuildingQueueData += ',';
                BuildingQueueData += Filed[i];
            }
            Global.SaveRoleParamsStringToDB(client, strKey, BuildingQueueData, true);
        }

        /// <summary>
        /// 队列数据删除
        /// </summary>
        public bool RemoveBuildingQueueData(GameClient client, int BuildID, int TaskID)
        {
            // 获得队列数据
            List<BuildTeam> BuildQueue = GetBuildingQueueData(client);

            int RemoveIndex = -1;

            // 删除
            for (int i = 0; i < BuildQueue.Count; ++i)
            {
                if (BuildID == BuildQueue[i].BuildID && TaskID == BuildQueue[i].TaskID)
                {
                    RemoveIndex = i;
                    BuildQueue[i].BuildID = 0;
                    break;
                }
            }

            // 失败
            if (-1 == RemoveIndex)
                return false;

            // 如果是付费队列的任务 --openpaynum
            if (BuildTeamType.PayTeam == BuildQueue[RemoveIndex]._TeamType)
            {
                ModifyOpenPayNum(client, -1);
            }

            // SaveToDB
            SaveBuildingQueueData(client, BuildQueue);
            
            return true;
        }
        /// <summary>
        /// 获得任务所在队列
        /// </summary>
        public BuildTeamType GetBuildTaskQueueType(GameClient client, int BuildID, int TaskID)
        {
            BuildTeamType TeamType = BuildTeamType.NullTeam;
            // 获得队列数据
            List<BuildTeam> BuildQueue = GetBuildingQueueData(client);
            for (int i = 0; i < BuildQueue.Count; ++i )
            {
                if(BuildQueue[i].BuildID == BuildID && BuildQueue[i].TaskID == TaskID)
                {
                    TeamType = BuildQueue[i]._TeamType;
                    break;
                }
            }
           return TeamType;
        }

        /// <summary>
        /// 队列数据添加
        /// </summary>
        public bool AddBuildingQueueData(GameClient client, int BuildID, int TaskID)
        {
            int free = 0;
            int pay = 0;
            
            // 获取当前任务在个队列的状态
            GetTaskNumInEachTeam(client, out free, out pay);
    
            // 优先免费队列
            BuildTeamType TeamType = BuildTeamType.FreeTeam;
            if (free < ManorFreeQueueNumMax)
            {
                TeamType = BuildTeamType.FreeTeam;
            }
            else if(pay < GetOpenPayTeamNum(client))
            {
                TeamType = BuildTeamType.PayTeam;
            }
            else
            {
                return false;
            }

            // 获得队列数据
            List<BuildTeam> BuildQueue = GetBuildingQueueData(client);
            
            BuildTeam TeamData = new BuildTeam();
            TeamData._TeamType = TeamType;
            TeamData.BuildID = BuildID;
            TeamData.TaskID = TaskID;
            
            // add
            BuildQueue.Add(TeamData);

            // SaveToDB
            SaveBuildingQueueData(client, BuildQueue);

            return true;
        }

        /// <summary>
        /// 是否有已经完成的任务
        /// </summary>
        public bool CheckAnyTaskFinish(GameClient client)
        {
            bool Finished = false;

            // 获得开发队列
            List<BuildTeam> BuildQueue = GetBuildingQueueData(client);

            // 寻找建筑物
            for (int i = 0; i < client.ClientData.BuildingDataList.Count; ++i)
            {
                int BuildID = client.ClientData.BuildingDataList[i].BuildId;

                if (client.ClientData.BuildingDataList[i].BuildTime == ConstBuildTime)
                    continue;

                BuildingConfigData BConfigData;
                BuildDict.TryGetValue(BuildID, out BConfigData);
                if (null == BConfigData)
                    continue;

                int taskID = 0; // 寻找任务
                for (int qloop = 0; qloop < BuildQueue.Count; ++qloop)
                {
                    if (BuildQueue[qloop].BuildID == BuildID)
                    {
                        taskID = BuildQueue[qloop].TaskID;
                        break;
                    }
                }

                // 寻找指定开发任务数据
                BuildingTaskConfigData TaskConfigData;
                BuildTaskDict.TryGetValue(taskID, out TaskConfigData);
                if (null == TaskConfigData)
                    continue;


                // 查看任务状态
                BuildingState state = GetBuildState(client, BuildID, taskID);
                if (BuildingState.EBS_Finish == state)
                {
                    Finished = true;
                    break;
                }
            }

            return Finished;
        }

        /// <summary>
        /// 是否有可领取的总等级奖励
        /// </summary>
        public bool CheckCanGetAnyAllLevelAward(GameClient client)
        {
            bool CanGetAny = false;

            HashSet<int> AwardedSet = new HashSet<int>();

            // 检查是否已经领取过 awardID1|awardID2|awardID3
            string strKey = RoleParamName.BuildAllLevAward;
            string BuildAllLevAwardData = Global.GetRoleParamByName(client, strKey);
            if (!string.IsNullOrEmpty(BuildAllLevAwardData))
            {
                string[] Filed = BuildAllLevAwardData.Split('|');
                for (int i = 0; i < Filed.Length; ++i)
                {
                    AwardedSet.Add(Global.SafeConvertToInt32(Filed[i]));
                }
            }

            // 检查是否满足总等级要求
            int BuildAllLevel = 0;
            for (int i = 0; i < client.ClientData.BuildingDataList.Count; ++i)
            {
                BuildAllLevel += client.ClientData.BuildingDataList[i].BuildLev;
            }

            //
            foreach (var kvp in BuildLevelAwardDict)
            {
                if (AwardedSet.Contains(kvp.Key))
                    continue;

                if (BuildAllLevel >= kvp.Value.AllLevel)
                {
                    CanGetAny = true;
                    break;
                }
            }

            return CanGetAny;
        }

        /// <summary>
        /// 完成建造任务
        /// </summary>
        public void BuildTaskFinish(GameClient client, BuildingData BuildData, BuildingConfigData BConfigData, BuildingTaskConfigData TaskConfigData)
        {
            BuildData.BuildTime = ConstBuildTime;

            // 搜索升级数据
            BuildingLevelConfigData myBuildLevel;
            BuildLevelDict.TryGetValue(new KeyValuePair<int, int>(BuildData.BuildId, BuildData.BuildLev), out myBuildLevel);
            if (null == myBuildLevel)
                return;

            //建筑经验 = 建筑经验倍率 * 建筑经验产出基础 * 时间
            int ExpAdd = (int)(TaskConfigData.ExpNum * myBuildLevel.Exp * TaskConfigData.Time);

            // 计算建筑物经验变化
            BuildData.BuildExp += ExpAdd;

            // 处理等级变化
            BuildingLevelConfigData myBuildLevelUP = myBuildLevel;
            while (BuildData.BuildLev != BConfigData.MaxLevel)
            {
                // 升级
                if (null != myBuildLevelUP && BuildData.BuildExp >= myBuildLevelUP.UpNeedExp)
                {
                    BuildData.BuildExp -= myBuildLevelUP.UpNeedExp;
                    BuildData.BuildLev++;

                    // next level
                    BuildLevelDict.TryGetValue(new KeyValuePair<int, int>(BuildData.BuildId, BuildData.BuildLev), out myBuildLevelUP);
                }
                else
                {
                    break;
                }
            }

            // 满级
            if(BuildData.BuildLev == BConfigData.MaxLevel)
            {
                BuildData.BuildExp = 0;
            }

            // 	其他资源 = ( 倍率和 - 建筑经验倍率 ) * 对应产出基础 * 时间
            double rate = (TaskConfigData.SumNum - TaskConfigData.ExpNum) * TaskConfigData.Time;

            // 免费随机任务
            RandomBuildTaskData(client, BuildData.BuildId, BuildData);

            // 给奖励 绑定金币
            if(myBuildLevel.Money > 0.0d)
            {
                GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, (int)(myBuildLevel.Money * rate), "建造任务完成");
            }

            //奖励魔晶
            if (myBuildLevel.MoJing > 0.0d)
            {
                GameManager.ClientMgr.ModifyTianDiJingYuanValue(client, (int)(myBuildLevel.MoJing * rate), "建造任务完成", false, true);
            }

            // 星魂
            if (myBuildLevel.XingHun > 0.0d)
            {
                GameManager.ClientMgr.ModifyStarSoulValue(client, (int)(myBuildLevel.XingHun * rate), "建造任务完成", true, true);
            }

            //成就奖励
            if (myBuildLevel.ChengJiu > 0.0d)
            {
                GameManager.ClientMgr.ModifyChengJiuPointsValue(client, (int)(myBuildLevel.ChengJiu * rate), "建造任务完成");
            }

            // 声望
            if (myBuildLevel.ShengWang > 0.0d)
            {
                GameManager.ClientMgr.ModifyShengWangValue(client, (int)(myBuildLevel.ShengWang * rate), "建造任务完成");
            }

            // 元素粉末
            if (myBuildLevel.YuanSu > 0.0d)
            {
                GameManager.ClientMgr.ModifyYuanSuFenMoValue(client, (int)(myBuildLevel.YuanSu * rate), "建造任务完成", true);
            }

            // 荧光粉末
            if (myBuildLevel.YingGuang > 0.0d)
            {
                GameManager.FluorescentGemMgr.AddFluorescentPoint(client, (int)(myBuildLevel.YingGuang * rate), "建造任务完成");
            }
        }

        /// <summary>
        /// 领地升级GM命令
        /// </summary>
        public void BuildingLevelUp_GM(GameClient client, int buildID)
        {
            // 寻找对应的建筑物数据
            BuildingData BuildData = null;

            // 执行建造
            for (int i = 0; i < client.ClientData.BuildingDataList.Count; ++i)
            {
                if (client.ClientData.BuildingDataList[i].BuildId == buildID)
                {
                    BuildData = client.ClientData.BuildingDataList[i];
                    break;
                }
            }

            if (null == BuildData)
                return;

            // 配置数据
            BuildingConfigData BConfigData;
            BuildDict.TryGetValue(BuildData.BuildId, out BConfigData);
            if (null == BConfigData)
            {
                return;
            }

            // 达到最大等级
            if (BuildData.BuildLev == BConfigData.MaxLevel)
                return;

            // 升级
            BuildData.BuildLev++;

            // 存数据库
            UpdateBuildingDataDB(client, BuildData);

            // 同步单个建筑物数据
            byte[] bytesData = DataHelper.ObjectToBytes<BuildingData>(BuildData);
            GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_SPR_BUILD_SYNC_SINGLE);
        }

#region 指令处理

        /// <summary>
        /// 获得领地数据
        /// </summary>
        public bool ProcessBuildGetListCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);

                byte[] bytesData = DataHelper.ObjectToBytes<List<BuildingData>>(client.ClientData.BuildingDataList);
                GameManager.ClientMgr.SendToClient(client, bytesData, nID);

                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 执行建造
        /// </summary>
        public bool ProcessBuildExcuteCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                // 如果1.8的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                    return false;

                String strcmd = "";

                int result = (int)BuildingErrorCode.Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);
                int buildID = Global.SafeConvertToInt32(cmdParams[1]);
                int taskID = Global.SafeConvertToInt32(cmdParams[2]);

                // 寻找对应的建筑物数据
                BuildingData BuildData = null;

                // 执行建造
                for (int i = 0; i < client.ClientData.BuildingDataList.Count; ++i)
                {
                    if (client.ClientData.BuildingDataList[i].BuildId == buildID)
                    {
                        BuildData = client.ClientData.BuildingDataList[i];
                        break;
                    }
                }

                // 未找到对应的建筑
                if (null == BuildData)
                {
                    result = (int)BuildingErrorCode.ErrorBuildNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, taskID, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 是否有对应的建造任务
                if (BuildData.TaskID_1 != taskID && BuildData.TaskID_2 != taskID && BuildData.TaskID_3 != taskID)
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, taskID, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查开发时间
                if (BuildData.BuildTime != ConstBuildTime)
                {
                    result = (int)BuildingErrorCode.ErrorInBuilding;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, taskID, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 尝试添加到研发队列
                if(!AddBuildingQueueData(client, buildID, taskID))
                {
                    result = (int)BuildingErrorCode.ErrorBQueueNotEnough;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, taskID, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 获取所在队列类型
                BuildTeamType TeamType = GetBuildTaskQueueType(client, buildID, taskID);

                // 开始研发
                BuildData.BuildTime = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");
                UpdateBuildingDataDB(client, BuildData);

                UpdateBuildingLogDB(client, BuildingLogType.BuildLog_Task);
                UpdateBuildingLogDB(client, BuildingLogType.BuildLog_TaskRole);

                // icon
                if (client._IconStateMgr.CheckBuildingIcon(client, false))
                    client._IconStateMgr.SendIconStateToClient(client);

                // 错误代码：角色id：建筑物ID：建筑物开发ID：开发队列ID
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, taskID, TeamType);
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 瞬间完成建造
        /// </summary>
        public bool ProcessBuildFinishCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                // 如果1.8的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                    return false;

                String strcmd = "";

                int result = (int)BuildingErrorCode.Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);
                int buildID = Global.SafeConvertToInt32(cmdParams[1]);
                int taskID = 0;

                // 寻找对应的建筑物数据
                BuildingData BuildData = null;

                // 寻找建筑物
                for (int i = 0; i < client.ClientData.BuildingDataList.Count; ++i)
                {
                    if (client.ClientData.BuildingDataList[i].BuildId == buildID)
                    {
                        BuildData = client.ClientData.BuildingDataList[i];
                        break;
                    }
                }

                // 未找到对应的建筑
                if (null == BuildData)
                {
                    result = (int)BuildingErrorCode.ErrorBuildNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 获得开发队列
                List<BuildTeam> BuildQueue = GetBuildingQueueData(client);
                for (int i = 0; i < BuildQueue.Count; ++i)
                {
                    if (BuildQueue[i].BuildID == buildID)
                    {
                        taskID = BuildQueue[i].TaskID;
                        break;
                    }
                }

                if (0 == taskID)
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                BuildingConfigData BConfigData;
                BuildDict.TryGetValue(BuildData.BuildId, out BConfigData);
                if (null == BConfigData)
                {
                    result = (int)BuildingErrorCode.ErrorBuildNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 是否有对应的建造任务
                if (BuildData.TaskID_1 != taskID && BuildData.TaskID_2 != taskID && BuildData.TaskID_3 != taskID)
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }


                // 检查开发时间
                if(BuildData.BuildTime == ConstBuildTime)
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 寻找指定开发任务数据
                BuildingTaskConfigData TaskConfigData;
                BuildTaskDict.TryGetValue(taskID, out TaskConfigData);
                if(null == TaskConfigData)
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查钻石是否够
                DateTime BuildTime;
                DateTime.TryParse(BuildData.BuildTime, out BuildTime);
                long SpendTicks = (TimeUtil.NowDateTime().Ticks / 10000) - (BuildTime.Ticks / 10000);
                long SpendSecondes = SpendTicks / 1000;

                // 获取研发任务数据
                long SubTimeSecondes = TaskConfigData.Time*60 - SpendSecondes;

                // b)	所需钻石 = 剩余时间 / 钻石抵换时间系数
                int NeedDiamond = (int)SubTimeSecondes / ManorQuickFinishNum;
                if(SubTimeSecondes > 0)
                {
                    if(0 == NeedDiamond) // 不足按1钻石计算
                    {
                        NeedDiamond = 1;
                    }
                    
                    // 检查消耗
                    if (client.ClientData.UserMoney < NeedDiamond)
                    {
                        result = (int)BuildingErrorCode.ZuanShiNotEnough;
                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, -1);
                        client.sendCmd(nID, strcmd);
                        return true;
                    }
                }

                // 扣钻石
                if (NeedDiamond > 0)
                {
                    if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, NeedDiamond, "建筑物完成"))
                    {
                        result = (int)BuildingErrorCode.ZuanShiNotEnough;
                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, -1);
                        client.sendCmd(nID, strcmd);
                        return true;
                    }
                }

                // 获取所在队列类型
                BuildTeamType TeamType = GetBuildTaskQueueType(client, buildID, taskID);
                // 尝试将该任务从队列中清除
                if (!RemoveBuildingQueueData(client, buildID, taskID))
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, -1);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 处理任务完成相关
                BuildTaskFinish(client, BuildData, BConfigData, TaskConfigData);     
                
                // 存数据库
                UpdateBuildingDataDB(client, BuildData);

                // 同步单个建筑物数据
                byte[] bytesData = DataHelper.ObjectToBytes<BuildingData>(BuildData);
                GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_SPR_BUILD_SYNC_SINGLE);

                // icon
                if (client._IconStateMgr.CheckBuildingIcon(client, false))
                    client._IconStateMgr.SendIconStateToClient(client);

                // 错误代码：角色id：建筑物ID：等级：经验：所在队列ID
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, BuildData.BuildLev, BuildData.BuildExp, TeamType);
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 刷新建造任务
        /// </summary>
        public bool ProcessBuildRefreshCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                // 如果1.8的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                    return false;

                String strcmd = "";

                int result = (int)BuildingErrorCode.Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);
                int buildID = Global.SafeConvertToInt32(cmdParams[1]);

                // 寻找对应的建筑物数据
                BuildingData BuildData = null;

                // 寻找建筑物
                for (int i = 0; i < client.ClientData.BuildingDataList.Count; ++i)
                {
                    if (client.ClientData.BuildingDataList[i].BuildId == buildID)
                    {
                        BuildData = client.ClientData.BuildingDataList[i];
                        break;
                    }
                }

                // 未找到对应的建筑
                if (null == BuildData)
                {
                    result = (int)BuildingErrorCode.ErrorBuildNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查开发时间
                if (BuildData.BuildTime != ConstBuildTime)
                {
                    result = (int)BuildingErrorCode.ErrorInBuilding;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查钻石够不够
                if (client.ClientData.UserMoney < ManorRandomTaskPrice)
                {
                    result = (int)BuildingErrorCode.ZuanShiNotEnough;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 扣钻石
                if (ManorRandomTaskPrice > 0)
                {
                    if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ManorRandomTaskPrice, "刷新任务"))
                    {
                        result = (int)BuildingErrorCode.ZuanShiNotEnough;
                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, 0, 0, 0);
                        client.sendCmd(nID, strcmd);
                        return true;
                    }
                }

                // 刷信息任务
                RandomBuildTaskData(client, buildID, BuildData, true);
                UpdateBuildingDataDB(client, BuildData);

                UpdateBuildingLogDB(client, BuildingLogType.BuildLog_Refresh);
                UpdateBuildingLogDB(client, BuildingLogType.BuildLog_RefreshRole);

                // 返回消息
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", result, roleID, buildID, BuildData.TaskID_1, BuildData.TaskID_2, BuildData.TaskID_3);
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 获得总等级奖励
        /// </summary>
        public bool ProcessBuildGetAllLevelAwardCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                // 如果1.8的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                    return false;

                String strcmd = "";
                int result = (int)BuildingErrorCode.Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);
                int awardID = Global.SafeConvertToInt32(cmdParams[1]);

                // 尝试寻找对应的奖励数据
                BuildingLevelAwardConfigData myAwardData = null;
                BuildLevelAwardDict.TryGetValue(awardID, out myAwardData);
                if (null == myAwardData)
                {
                    result = (int)BuildingErrorCode.ErrorAllLevel;
                    strcmd = string.Format("{0}:{1}:{2}", result, roleID, awardID);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查是否已经领取过 awardID1|awardID2|awardID3
                string strKey = RoleParamName.BuildAllLevAward;
                string BuildAllLevAwardData = Global.GetRoleParamByName(client, strKey);
                if (!string.IsNullOrEmpty(BuildAllLevAwardData))
                {
                    string[] Filed = BuildAllLevAwardData.Split('|');
                    for(int i = 0; i < Filed.Length; ++i)
                    {
                        if (awardID == Global.SafeConvertToInt32(Filed[i]))
                        {
                            result = (int)BuildingErrorCode.ErrorAllLevelAwarded;
                            strcmd = string.Format("{0}:{1}:{2}", result, roleID, awardID);
                            client.sendCmd(nID, strcmd);
                            return true;
                        }
                    }
                }

                // 检查是否满足总等级要求
                int BuildAllLevel = 0;
                for (int i = 0; i < client.ClientData.BuildingDataList.Count; ++i)
                {
                    BuildAllLevel += client.ClientData.BuildingDataList[i].BuildLev;
                }

                if (BuildAllLevel < myAwardData.AllLevel)
                {
                    result = (int)BuildingErrorCode.ErrorAllLevel;
                    strcmd = string.Format("{0}:{1}:{2}", result, roleID, awardID);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 尝试给奖励
                List<GoodsData> goodsDataList = Global.ConvertToGoodsDataList(myAwardData.GoodsList.Items);
                if (!Global.CanAddGoodsDataList(client, goodsDataList))
                {
                    result = (int)BuildingErrorCode.ErrorBagNotEnough;
                    strcmd = string.Format("{0}:{1}:{2}", result, roleID, awardID);
                    client.sendCmd(nID, strcmd);
                    return true;
                }
                else
                {
                  //领取物品
                  for (int n = 0; n < goodsDataList.Count; n++)
                  {
                      GoodsData goodsData = goodsDataList[n];

                      if (null == goodsData)
                      {
                          continue;
                      }

                      //向DBServer请求加入某个新的物品到背包中
                      //添加物品
                      goodsData.Id = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID,
                          goodsData.GCount, goodsData.Quality, goodsData.Props, goodsData.Forge_level,
                          goodsData.Binding, 0, goodsData.Jewellist, true, 1, /**/"获得领地总等级奖励", goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong);
                  }
                }

                // 记录为已领取
                if (string.IsNullOrEmpty(BuildAllLevAwardData))
                {
                    BuildAllLevAwardData += awardID;
                }
                else // 追加
                {
                    BuildAllLevAwardData += '|';
                    BuildAllLevAwardData += awardID;
                }
                Global.SaveRoleParamsStringToDB(client, strKey, BuildAllLevAwardData, true);
  
                // 返回成功
                strcmd = string.Format("{0}:{1}:{2}", result, roleID, awardID);
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 获得奖励
        /// </summary>
        public bool ProcessBuildGetAwardCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                // 如果1.8的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                    return false;

                String strcmd = "";
                int result = (int)BuildingErrorCode.Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);
                int buildID = Global.SafeConvertToInt32(cmdParams[1]);
                int taskID = 0;

                // 寻找对应的建筑物数据
                BuildingData BuildData = null;

                // 寻找建筑物
                for (int i = 0; i < client.ClientData.BuildingDataList.Count; ++i)
                {
                    if (client.ClientData.BuildingDataList[i].BuildId == buildID)
                    {
                        BuildData = client.ClientData.BuildingDataList[i];
                        break;
                    }
                }

                // 未找到对应的建筑
                if (null == BuildData)
                {
                    result = (int)BuildingErrorCode.ErrorBuildNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, 0, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                BuildingConfigData BConfigData;
                BuildDict.TryGetValue(BuildData.BuildId, out BConfigData);
                if (null == BConfigData)
                {
                    result = (int)BuildingErrorCode.ErrorBuildNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, 0, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 检查开发时间
                if (BuildData.BuildTime == ConstBuildTime)
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, 0, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 获得开发队列
                List<BuildTeam> BuildQueue = GetBuildingQueueData(client);
                for (int i = 0; i < BuildQueue.Count; ++i)
                {
                    if (BuildQueue[i].BuildID == buildID)
                    {
                        taskID = BuildQueue[i].TaskID;
                        break;
                    }
                }

                if (0 == taskID)
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, 0, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 寻找指定开发任务数据
                BuildingTaskConfigData TaskConfigData;
                BuildTaskDict.TryGetValue(taskID, out TaskConfigData);
                if (null == TaskConfigData)
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, 0, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 查看任务状态
                BuildingState state = GetBuildState(client, buildID, taskID);
                if(BuildingState.EBS_Finish != state)
                {
                    result = (int)BuildingErrorCode.ErrorInBuilding;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, 0, 0);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 尝试将该任务从队列中清除
                if (!RemoveBuildingQueueData(client, buildID, taskID))
                {
                    result = (int)BuildingErrorCode.ErrorTaskNotFind;
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, BuildData.BuildLev, BuildData.BuildExp);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 处理任务完成相关
                BuildTaskFinish(client, BuildData, BConfigData, TaskConfigData);
                
                // 存数据库
                UpdateBuildingDataDB(client, BuildData);

                // 同步单个建筑物数据
                byte[] bytesData = DataHelper.ObjectToBytes<BuildingData>(BuildData);
                GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_SPR_BUILD_SYNC_SINGLE);

                // icon
                if (client._IconStateMgr.CheckBuildingIcon(client, false))
                    client._IconStateMgr.SendIconStateToClient(client);

                // 错误代码：角色id：建筑物ID：等级：经验
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", result, roleID, buildID, BuildData.BuildLev, 0);
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 开启收费队列
        /// </summary>
        public bool ProcessBuildOpenQueueCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                // 如果1.8的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                    return false;

                String strcmd = "";

                int result = (int)BuildingErrorCode.Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);

                // 检查是否达到上限
                int CurPayOpenNum = GetOpenPayTeamNum(client);
                if(CurPayOpenNum >= ManorQueueNumMax)
                {
                    result = (int)BuildingErrorCode.ErrorPayTeamMaxOver;
                    strcmd = string.Format("{0}:{1}", result, roleID);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 看钻石够不够
                if (client.ClientData.UserMoney < ManorQueuePrice)
                {
                    result = (int)BuildingErrorCode.ZuanShiNotEnough;
                    strcmd = string.Format("{0}:{1}", result, roleID);
                    client.sendCmd(nID, strcmd);
                    return true;
                }

                // 扣钻石
                if (ManorQueuePrice > 0)
                {
                    if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ManorQueuePrice, "开启收费队列"))
                    {
                        result = (int)BuildingErrorCode.ZuanShiNotEnough;
                        strcmd = string.Format("{0}:{1}", result, roleID);
                        client.sendCmd(nID, strcmd);
                        return true;
                    }
                }

                // 开启
                ModifyOpenPayNum(client, 1);

                UpdateBuildingLogDB(client, BuildingLogType.BuildLog_Open);
                UpdateBuildingLogDB(client, BuildingLogType.BuildLog_OpenRole);

                // 返回消息
                strcmd = string.Format("{0}:{1}", result, roleID);
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 获得队列数据
        /// </summary>
        public bool ProcessBuildGetQueueCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                String strcmd = "";

                int result = (int)BuildingErrorCode.Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);

                // 返回消息 errcode:roleid:openpaynum:queueid|buildid|taskid:queueid|buildid|taskid
                strcmd = string.Format("{0}:{1}", result, roleID);

                // 尝试解析队列数据
                string strKey = RoleParamName.BuildQueueData;

                //  openpaynum,queueid|builid|taskid,queueid|buildid|taskid
                string BuildingQueueData = Global.GetRoleParamByName(client, strKey);
                if (!string.IsNullOrEmpty(BuildingQueueData))
                {
                    string[] Filed = BuildingQueueData.Split(',');                  
                    
                    // openpaynum
                    strcmd += ':';
                    strcmd += Filed[0];

                    // queueid|buildid|taskid:queueid|buildid|taskid
                    for (int i = 1; i < Filed.Length; ++i)
                    {
                        strcmd += ':';
                        strcmd += Filed[i];
                    }
                }
                else
                {
                    // openpaynum
                    strcmd += ':';
                    strcmd += 0;
                }

                // 返回消息
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 获得总等级奖励状态
        /// </summary>
        public bool ProcessBuildGetAllLevelAwardStateCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                String strcmd = "";

                int result = (int)BuildingErrorCode.Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);

                // 总等级奖励领取状态  awardID1|awardID2|awardID3
                string strKey = RoleParamName.BuildAllLevAward;
                string BuildAllLevAwardData = Global.GetRoleParamByName(client, strKey);

                // 返回消息 errcode:roleid:awardID1|awardID2|awardID3
                strcmd = string.Format("{0}:{1}:{2}", result, roleID, BuildAllLevAwardData);

                // 返回消息
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        /// <summary>
        /// 获得建筑物状态
        /// </summary>
        public bool ProcessBuildGetStateCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                String strcmd = "";

                int result = (int)BuildingErrorCode.Success;
                int roleID = Global.SafeConvertToInt32(cmdParams[0]);

                // 返回消息 errcode:roleid:buildid|buildstate:buildid|buildstate
                strcmd = string.Format("{0}:{1}", result, roleID);

                // 建筑队列
                List<BuildTeam> BuildQueue = GetBuildingQueueData(client);

                // 拼消息
                for (int i = 0; i < BuildQueue.Count; ++i)
                {
                    strcmd += ':';
                    strcmd += BuildQueue[i].BuildID;
                    strcmd += '|';
                    strcmd += (int)GetBuildState(client, BuildQueue[i].BuildID, BuildQueue[i].TaskID);
                }

                // 返回消息
                client.sendCmd(nID, strcmd);
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }
#endregion

        /// <summary>
        /// 初始化配置文件
        /// </summary>
        public bool InitConfig()
        {
            // 初始化队列上限数据
            string QueueNumMax = GameManager.systemParamsList.GetParamValueByName("ManorQueueNum");
            if(!string.IsNullOrEmpty(QueueNumMax))
            {
                ManorQueueNumMax = Global.SafeConvertToInt32(QueueNumMax);
            }

            // 免费队列上限
            QueueNumMax = GameManager.systemParamsList.GetParamValueByName("ManorFreeQueueNum");
            if (!string.IsNullOrEmpty(QueueNumMax))
            {
                ManorFreeQueueNumMax = Global.SafeConvertToInt32(QueueNumMax);
            }

            // 快速完成系数
            string QuickFinishNum = GameManager.systemParamsList.GetParamValueByName("ManorQuickFinishNum");
            if (!string.IsNullOrEmpty(QuickFinishNum))
            {
                ManorQuickFinishNum = Global.SafeConvertToInt32(QuickFinishNum);
            }

            // 刷新任务开销
            string RandomTaskPrice = GameManager.systemParamsList.GetParamValueByName("ManorRandomTaskPrice");
            if (!string.IsNullOrEmpty(RandomTaskPrice))
            {
                ManorRandomTaskPrice = Global.SafeConvertToInt32(RandomTaskPrice);
            }

            // 开启收费队列花销
            string PayQueueOpenPrice = GameManager.systemParamsList.GetParamValueByName("ManorQueuePrice");
            if (!string.IsNullOrEmpty(PayQueueOpenPrice))
            {
                ManorQueuePrice = Global.SafeConvertToInt32(PayQueueOpenPrice);
            }

            if (!LoadBuildFile())
                return false;

            if (!LoadBuildTaskFile())
                return false;

            if (!LoadBuildLevelFile())
                return false;

            if (!LoadBuildLevelAwardFile())
                return false;

            return true;
        }
#region 初始化配置表
        /// <summary>
        /// 初始化Build.xml
        /// </summary>
        public bool LoadBuildFile()
        {
            try
            {
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(Build_fileName));
                if (null == xml) return false;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null != xmlItem)
                    {
                        BuildingConfigData myBuild = new BuildingConfigData();
                        myBuild.BuildID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        myBuild.MaxLevel = (int)Global.GetSafeAttributeLong(xmlItem, "MaxLevel"); // 最高等级

                        // 概率
                        string RateFreeRandom = Global.GetSafeAttributeStr(xmlItem, "FreeRandomTask");
                        if (string.IsNullOrEmpty(RateFreeRandom))
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("读取建筑物配置文件中的免费任务配置项1失败"));
                        }
                        else
                        {
                            string[] fields = RateFreeRandom.Split('|');
                            if (fields.Length <= 0)
                            {
                                LogManager.WriteLog(LogTypes.Warning, string.Format("解析建筑物配置文件中的免费任务配置项1失败"));
                            }
                            else
                            {
                                for (int i = 0; i < fields.Length; ++i)
                                {
                                    string[] IDVsRate = fields[i].Split(',');
                                    if (IDVsRate.Length < 2)
                                        continue;

                                    BuildingRandomData brdata = new BuildingRandomData();
                                    brdata.quality = (BuildingQuality)Global.SafeConvertToInt32(IDVsRate[0]);
                                    brdata.rate = Convert.ToDouble(IDVsRate[1]);
                                    myBuild.FreeRandomList.Add(brdata);
                                }
                            }
                        }

                        string RateRandom = Global.GetSafeAttributeStr(xmlItem, "RandomTask");
                        if (string.IsNullOrEmpty(RateRandom))
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("读取建筑物配置文件中的任务配置项1失败"));
                        }
                        else
                        {
                            string[] fields = RateRandom.Split('|');
                            if (fields.Length <= 0)
                            {
                                LogManager.WriteLog(LogTypes.Warning, string.Format("解析建筑物配置文件中的任务配置项1失败"));
                            }
                            else
                            {
                                for (int i = 0; i < fields.Length; ++i)
                                {
                                    string[] IDVsRate = fields[i].Split(',');
                                    if (IDVsRate.Length < 2)
                                        continue;

                                    BuildingRandomData brdata = new BuildingRandomData();
                                    brdata.quality = (BuildingQuality)Global.SafeConvertToInt32(IDVsRate[0]);
                                    brdata.rate = Convert.ToDouble(IDVsRate[1]);
                                    myBuild.RandomList.Add(brdata);
                                }
                            }
                        }
                        BuildDict[myBuild.BuildID] = myBuild;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", "Build.xml", ex.Message));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化BuildTask.xml
        /// </summary>
        public bool LoadBuildTaskFile()
        {
            try
            {
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(BuildTask_fileName));
                if (null == xml) return false;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null != xmlItem)
                    {
                        BuildingTaskConfigData myBuildTask = new BuildingTaskConfigData();
                        myBuildTask.TaskID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        myBuildTask.BuildID = (int)Global.GetSafeAttributeLong(xmlItem, "BuildID");
                        myBuildTask.quality = (BuildingQuality)Global.GetSafeAttributeLong(xmlItem, "Quality");
                        myBuildTask.SumNum = Global.GetSafeAttributeDouble(xmlItem, "Sum");
                        myBuildTask.ExpNum = Global.GetSafeAttributeDouble(xmlItem, "ExpNum");
                        myBuildTask.Time = (int)Global.GetSafeAttributeLong(xmlItem, "Time");

                        BuildTaskDict[myBuildTask.TaskID] = myBuildTask;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", "BuildTask.xml", ex.Message));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化BuildLevel.xml
        /// </summary>
        public bool LoadBuildLevelFile()
        {
            try
            {
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(BuildLevel_fileName));
                if (null == xml) return false;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null != xmlItem)
                    {
                        BuildingLevelConfigData myBuildLevel = new BuildingLevelConfigData();
                        myBuildLevel.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        myBuildLevel.BuildID = (int)Global.GetSafeAttributeLong(xmlItem, "BuildID");
                        myBuildLevel.Level = (int)Global.GetSafeAttributeLong(xmlItem, "Level");
                        myBuildLevel.UpNeedExp = (int)Global.GetSafeAttributeLong(xmlItem, "UpNeedExp");

                        myBuildLevel.Exp = Global.GetSafeAttributeDouble(xmlItem, "Exp");
                        myBuildLevel.Money = Global.GetSafeAttributeDouble(xmlItem, "Money");
                        myBuildLevel.MoJing = Global.GetSafeAttributeDouble(xmlItem, "MoJing");
                        myBuildLevel.XingHun = Global.GetSafeAttributeDouble(xmlItem, "XingHun");
                        myBuildLevel.ChengJiu = Global.GetSafeAttributeDouble(xmlItem, "ChengJiu");
                        myBuildLevel.ShengWang = Global.GetSafeAttributeDouble(xmlItem, "ShengWang");
                        myBuildLevel.YuanSu = Global.GetSafeAttributeDouble(xmlItem, "YuanSu");
                        myBuildLevel.YingGuang = Global.GetSafeAttributeDouble(xmlItem, "YingGuang");
                        BuildLevelDict.Add(new KeyValuePair<int, int>(myBuildLevel.BuildID, myBuildLevel.Level), myBuildLevel);
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", "BuildTask.xml", ex.Message));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化BuildLevelAward.xml
        /// </summary>
        public bool LoadBuildLevelAwardFile()
        {
            try
            {
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(BuildLevelAward_fileName));
                if (null == xml) return false;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    BuildingLevelAwardConfigData myBuildLevelAward = new BuildingLevelAwardConfigData();
                    myBuildLevelAward.ID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    myBuildLevelAward.AllLevel = (int)Global.GetSafeAttributeLong(xmlItem, "AllLevel");

                    // 物品奖励
                    string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "Award");
                    if (string.IsNullOrEmpty(goodsIDs))
                    {
                        LogManager.WriteLog(LogTypes.Warning, string.Format("读取建筑物总等级奖励配置项1失败"));
                    }
                    else
                    {
                        ConfigParser.ParseAwardsItemList(goodsIDs, ref myBuildLevelAward.GoodsList);
                    }

                    BuildLevelAwardDict[myBuildLevelAward.ID] = myBuildLevelAward;
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", "BuildLevelAward.xml", ex.Message));
                return false;
            }

            return true;
        }

#endregion

    }
}
