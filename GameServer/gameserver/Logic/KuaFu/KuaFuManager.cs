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
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using System.Xml.Linq;
using GameServer.Core.GameEvent.EventOjectImpl;
using Tmsk.Contract;
using KF.Client;
using KF.Contract.Data;
using Tmsk.Contract.Const;
using System.Diagnostics;
using System.Configuration;
using Tmsk.Tools;
using GameServer.Logic.MoRi;
using GameServer.Logic.Copy;
using GameServer.Logic.Marriage.CoupleArena;

namespace GameServer.Logic
{
    public class KuaFuDbConnection
    {
        public int ServerId;
        public int ErrorCount = 0;
        public GameDbClientPool[] Pool = new GameDbClientPool[2] { new GameDbClientPool(), new GameDbClientPool() };

        public KuaFuDbConnection(int serverId)
        {
            ServerId = serverId;
        }

        ~KuaFuDbConnection()
        {
            Pool[0].Clear();
            Pool[1].Clear();
        }
    }

    /// <summary>
    /// 王城战管理
    /// </summary>
    public class KuaFuManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx, IManager2
    {
        #region 标准接口

        private ICoreInterface CoreInterface = null;

        private static KuaFuManager instance = new KuaFuManager();

        public static KuaFuManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public KuaFuDataData RuntimeData = new KuaFuDataData();

        public bool initialize()
        {
            return true;
        }

        public bool initialize(ICoreInterface coreInterface)
        {
            try
            {
                CoreInterface = coreInterface;
                if (!InitConfig())
                {
                    return false;
                }

                System.Runtime.Remoting.RemotingConfiguration.Configure(Process.GetCurrentProcess().MainModule.FileName + ".config", false);
                if (!HuanYingSiYuanClient.getInstance().initialize(coreInterface))
                {
                    return false;
                }

                if (!TianTiClient.getInstance().initialize(coreInterface))
                {
                    return false;
                }

                if (!YongZheZhanChangClient.getInstance().initialize(coreInterface))
                {
                    return false;
                }

                if (!KFCopyRpcClient.getInstance().initialize(coreInterface))
                {
                    return false;
                }
                /*
                if (!MoRiJudgeClient.getInstance().initialize(coreInterface))
                {
                    return false;
                }

                if (!ElementWarClient.getInstance().initialize(coreInterface))
                {
                    return false;
                }*/

                if (!SpreadClient.getInstance().initialize(coreInterface))
                {
                    return false;
                }

                if (!AllyClient.getInstance().initialize(coreInterface))
                    return false;

                GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerLogout, getInstance());
            }
            catch (System.Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool startup()
        {
            try
            {
                ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("HuanYingSiYuanClient.TimerProc", HuanYingSiYuanClient.getInstance().TimerProc), 2000, 2857);
                ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("TianTiClient.TimerProc", TianTiClient.getInstance().TimerProc), 2000, 1357);
                ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("YongZheZhanChangClient.TimerProc", YongZheZhanChangClient.getInstance().TimerProc), 2000, 3389);
                //ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("MoRiJudgeClient.TimerProc", MoRiJudgeClient.getInstance().TimerProc), 2000, 1732);
                //ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("ElementWarClient.TimerProc", ElementWarClient.getInstance().TimerProc), 2000, 1732);
                ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("KFCopyRpcClient.TimerProc", KFCopyRpcClient.getInstance().TimerProc), 2000, 1732);
                ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("SpreadClient.TimerProc", SpreadClient.getInstance().TimerProc), 2000, 1732);
                ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("AllyClient.TimerProc", AllyClient.getInstance().TimerProc), 2000, 1732);

                lock (RuntimeData.Mutex)
                {
                    if (null == RuntimeData.BackGroundThread)
                    {
                        RuntimeData.BackGroundThread = new Thread(BackGroudThreadProc);
                        RuntimeData.BackGroundThread.IsBackground = true;
                        RuntimeData.BackGroundThread.Start();
                    }
                }
            }
            catch (System.Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool showdown()
        {
            try
            {
                lock (RuntimeData.Mutex)
                {
                    RuntimeData.BackGroundThread.Abort();
                    RuntimeData.BackGroundThread = null;
                }

                GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerLogout, getInstance());

                if (!HuanYingSiYuanClient.getInstance().showdown())
                {
                    return false;
                }
                /*
                if (!ElementWarClient.getInstance().showdown())
                {
                    return false;
                }*/

                if (!SpreadClient.getInstance().showdown())
                {
                    return false;
                }

                if (!AllyClient.getInstance().showdown())
                    return false;
            }
            catch (System.Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool destroy()
        {
            try
            {
                if (!HuanYingSiYuanClient.getInstance().destroy())
                {
                    return false;
                }
                /*
                if (!ElementWarClient.getInstance().destroy())
                {
                    return false;
                }*/

                if (!SpreadClient.getInstance().destroy())
                {
                    return false;
                }

                if (!AllyClient.getInstance().destroy())
                    return false;
            }
            catch (System.Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            return true;
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventObject"></param>
        public void processEvent(EventObject eventObject)
        {
            int nID = eventObject.getEventType();
            switch (nID)
            {
                case (int)EventTypes.PlayerLogout:
                    break;
            }
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventObject"></param>
        public void processEvent(EventObjectEx eventObject)
        {
        }

        #endregion 标准接口

        #region 初始化配置

        /// <summary>
        /// 初始化配置
        /// </summary>
        public bool InitConfig()
        {
            bool success = true;
            XElement xml = null;
            string fileName = "";
            string fullPathFileName = "";
            IEnumerable<XElement> nodes;

            lock (RuntimeData.Mutex)
            {
                try
                {
                    int open = 0;
                    string kuaFuUriKeyNamePrefix = null;
                    int serverId = CoreInterface.GetLocalServerId();
                    PlatformTypes platfromType = CoreInterface.GetPlatformType();
                    fileName = string.Format("Config/ThroughService_{0}.xml", platfromType.ToString());
                    fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);

                    //如果配置文件存在,则读配置文件,否则读默认配置
                    if (File.Exists(fullPathFileName))
                    {
                        xml = XElement.Load(fullPathFileName);
                        nodes = xml.Elements();

                        foreach (var node in nodes)
                        {
                            int startServer = (int)Global.GetSafeAttributeLong(node, "StartServer");
                            int endServer = (int)Global.GetSafeAttributeLong(node, "EndServer");
                            if (startServer <= serverId && serverId < endServer)
                            {
                                open = (int)Global.GetSafeAttributeLong(node, "Open");
                                kuaFuUriKeyNamePrefix = Global.GetSafeAttributeStr(node, "ID");
                                break;
                            }
                        }
                    }
                    else
                    {
                        open = 1;
                        kuaFuUriKeyNamePrefix = null;
                    }

                    CoreInterface.SetRuntimeVariable(RuntimeVariableNames.KuaFuGongNeng, open.ToString());
                    CoreInterface.SetRuntimeVariable(RuntimeVariableNames.KuaFuUriKeyNamePrefix, kuaFuUriKeyNamePrefix);

                    string huanYingSiYuanUri = null;
                    string tianTiUri = null;
                    string yongZheZhanChangUri = null;
                  //  string moRiJudgeUri = null;
                   // string elementWarUri = null;
                    string kfcopyUri = null;
                    string spreadUri = null;
                    string allyUri = null;

                    if (open > 0)
                    {
                        string huanYingSiYuanUriKeyName = RuntimeVariableNames.HuanYingSiYuanUri + kuaFuUriKeyNamePrefix;
                        huanYingSiYuanUri = CoreInterface.GetGameConfigStr(huanYingSiYuanUriKeyName, null);

                        //如果数据库没配置,则读默认配置文件
                        if (string.IsNullOrEmpty(huanYingSiYuanUri)) 
                        {
                            ConfigurationManager.RefreshSection("appSettings");
                            huanYingSiYuanUri = ConfigurationManager.AppSettings.Get(huanYingSiYuanUriKeyName);

                            //如果没有指定后缀的配置,则读默认配置
                            if (string.IsNullOrEmpty(huanYingSiYuanUri))
                            {
                                huanYingSiYuanUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.HuanYingSiYuanUri);
                            }
                        }

                        string tianTiUriKeyName = RuntimeVariableNames.TianTiUri + kuaFuUriKeyNamePrefix;
                        tianTiUri = CoreInterface.GetGameConfigStr(tianTiUriKeyName, null);

                        //如果数据库没配置,则读默认配置文件
                        if (string.IsNullOrEmpty(tianTiUri))
                        {
                            ConfigurationManager.RefreshSection("appSettings");
                            tianTiUri = ConfigurationManager.AppSettings.Get(tianTiUriKeyName);

                            //如果没有指定后缀的配置,则读默认配置
                            if (string.IsNullOrEmpty(tianTiUri))
                            {
                                tianTiUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.TianTiUri);
                            }
                        }

                        string yongZheZhanChangUriKeyName = RuntimeVariableNames.YongZheZhanChangUri + kuaFuUriKeyNamePrefix;
                        yongZheZhanChangUri = CoreInterface.GetGameConfigStr(yongZheZhanChangUriKeyName, null);

                        //如果数据库没配置,则读默认配置文件
                        if (string.IsNullOrEmpty(yongZheZhanChangUri))
                        {
                            ConfigurationManager.RefreshSection("appSettings");
                            yongZheZhanChangUri = ConfigurationManager.AppSettings.Get(yongZheZhanChangUriKeyName);

                            //如果没有指定后缀的配置,则读默认配置
                            if (string.IsNullOrEmpty(yongZheZhanChangUri))
                            {
                                yongZheZhanChangUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.YongZheZhanChangUri);
                            }
                        }

                        /*
                        string moRiJudgeUriKeyName = RuntimeVariableNames.MoRiJudgeUri + kuaFuUriKeyNamePrefix;
                        moRiJudgeUri = CoreInterface.GetGameConfigStr(moRiJudgeUriKeyName, null);

                        //如果数据库没配置,则读默认配置文件
                        if (string.IsNullOrEmpty(moRiJudgeUri))
                        {
                            ConfigurationManager.RefreshSection("appSettings");
                            moRiJudgeUri = ConfigurationManager.AppSettings.Get(moRiJudgeUriKeyName);

                            //如果没有指定后缀的配置,则读默认配置
                            if (string.IsNullOrEmpty(moRiJudgeUri))
                            {
                                moRiJudgeUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.MoRiJudgeUri);
                            }
                        }

                        //元素试炼————————————————————————————————————————
                        string elementWarUriKeyName = RuntimeVariableNames.ElementWarUri + kuaFuUriKeyNamePrefix;
                        elementWarUri = CoreInterface.GetGameConfigStr(elementWarUriKeyName, null);

                        //如果数据库没配置,则读默认配置文件
                        if (string.IsNullOrEmpty(elementWarUri))
                        {
                            ConfigurationManager.RefreshSection("appSettings");
                            elementWarUri = ConfigurationManager.AppSettings.Get(elementWarUriKeyName);

                            //如果没有指定后缀的配置,则读默认配置
                            if (string.IsNullOrEmpty(elementWarUri))
                            {
                                elementWarUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.ElementWarUri);
                            }
                        }
                         * */

                        string kfcopyUriKeyName = RuntimeVariableNames.KuaFuCopyUri + kuaFuUriKeyNamePrefix;
                        kfcopyUri = CoreInterface.GetGameConfigStr(kfcopyUriKeyName, null);

                        //如果数据库没配置,则读默认配置文件
                        if (string.IsNullOrEmpty(kfcopyUri))
                        {
                            ConfigurationManager.RefreshSection("appSettings");
                            kfcopyUri = ConfigurationManager.AppSettings.Get(kfcopyUriKeyName);

                            //如果没有指定后缀的配置,则读默认配置
                            if (string.IsNullOrEmpty(kfcopyUri))
                            {
                                kfcopyUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.KuaFuCopyUri);
                            }
                        }
                        
                        //
                        string SpreadUriKeyName = RuntimeVariableNames.SpreadUri + kuaFuUriKeyNamePrefix;
                        spreadUri = CoreInterface.GetGameConfigStr(SpreadUriKeyName, null);
                        //如果数据库没配置,则读默认配置文件
                        if (string.IsNullOrEmpty(spreadUri))
                        {
                            ConfigurationManager.RefreshSection("appSettings");
                            spreadUri = ConfigurationManager.AppSettings.Get(SpreadUriKeyName);

                            //如果没有指定后缀的配置,则读默认配置
                            if (string.IsNullOrEmpty(spreadUri))
                            {
                                spreadUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.SpreadUri);
                            }
                        }

                        string AllyUriKeyName = RuntimeVariableNames.AllyUri + kuaFuUriKeyNamePrefix;
                        allyUri = CoreInterface.GetGameConfigStr(AllyUriKeyName, null);
                        //如果数据库没配置,则读默认配置文件
                        if (string.IsNullOrEmpty(allyUri))
                        {
                            ConfigurationManager.RefreshSection("appSettings");
                            allyUri = ConfigurationManager.AppSettings.Get(AllyUriKeyName);

                            //如果没有指定后缀的配置,则读默认配置
                            if (string.IsNullOrEmpty(allyUri))
                            {
                                allyUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.AllyUri);
                            }
                        }

                    }

                    CoreInterface.SetRuntimeVariable(RuntimeVariableNames.HuanYingSiYuanUri, huanYingSiYuanUri);
                    CoreInterface.SetRuntimeVariable(RuntimeVariableNames.TianTiUri, tianTiUri);
                    CoreInterface.SetRuntimeVariable(RuntimeVariableNames.YongZheZhanChangUri, yongZheZhanChangUri);                    
					//CoreInterface.SetRuntimeVariable(RuntimeVariableNames.ElementWarUri, elementWarUri);
                    //CoreInterface.SetRuntimeVariable(RuntimeVariableNames.MoRiJudgeUri, moRiJudgeUri);
                    CoreInterface.SetRuntimeVariable(RuntimeVariableNames.KuaFuCopyUri, kfcopyUri);   
                    CoreInterface.SetRuntimeVariable(RuntimeVariableNames.SpreadUri, spreadUri);
                    CoreInterface.SetRuntimeVariable(RuntimeVariableNames.AllyUri, allyUri);
                }
                catch (System.Exception ex)
                {
                    success = false;
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("加载xml配置文件:{0}, 失败。", fileName), ex);
                }
            }

            return success ;
        }

        #endregion 初始化配置

        #region 辅助接口

        public bool OnUserLogin2(TMSKSocket socket, int verSign, string userID, string userName, string lastTime, string isadult, string signCode)
        {
            WebLoginToken webLoginToken = new WebLoginToken()
            {
                VerSign = verSign,
                UserID = userID,
                UserName = userName,
                LastTime = lastTime,
                Isadult = isadult,
                SignCode = signCode,
            };

            socket.ClientKuaFuServerLoginData.WebLoginToken = webLoginToken;
            return true;
        }

        public bool OnUserLogin(TMSKSocket socket, int verSign, string userID, string userName, string lastTime, string userToken, string isadult, string signCode, int serverId, string ip, int port, int roleId, int gameType, long gameId)
        {
            KuaFuServerLoginData kuaFuServerLoginData = socket.ClientKuaFuServerLoginData;
            kuaFuServerLoginData.ServerId = serverId;
            kuaFuServerLoginData.ServerIp = ip;
            kuaFuServerLoginData.ServerPort = port;
            kuaFuServerLoginData.RoleId = roleId;
            kuaFuServerLoginData.GameType = gameType;
            kuaFuServerLoginData.GameId = gameId;

            if (kuaFuServerLoginData.WebLoginToken == null)
            {
                kuaFuServerLoginData.WebLoginToken = new WebLoginToken()
                {
                    VerSign = verSign,
                    UserID = userID,
                    UserName = userName,
                    LastTime = lastTime,
                    Isadult = isadult,
                    SignCode = signCode,
                };
            }

            if (roleId > 0)
            {
                // 跨服服务器的两个ID相等,如果不想等,则配置错误;
                // 如果想要去掉这个限制,允许区游戏服务器兼作跨服活动服务器(混合使用),必须先修改GameManager.ServerLineID相关的逻辑,使其可以兼容
                // 完成修改前,这个限制不可取消.
                if (GameManager.ServerLineID != GameManager.ServerId)
                {
                    LogManager.WriteLog(LogTypes.Error, "GameManager.ServerLineID未配置,禁止跨服登录");
                    return false;
                }

                //跨服登录,限制rid
                if (!string.IsNullOrEmpty(ip) && port > 0 && gameType > 0 && gameId > 0/* && HuanYingSiYuanClient.getInstance().CanKuaFuLogin()*/)
                {
                    string dbIp = "";
                    int dbPort = 0;
                    string logDbIp = "";
                    int logDbPort = 0;
                    socket.ServerId = serverId;
                    switch (gameType)
                    {
                        case (int)GameTypes.HuanYingSiYuan:
                            {
                                if (!HuanYingSiYuanClient.getInstance().CanKuaFuLogin()) return false;

                                socket.IsKuaFuLogin = HuanYingSiYuanClient.getInstance().KuaFuLogin(kuaFuServerLoginData);
                                if (!HuanYingSiYuanClient.getInstance().GetKuaFuDbServerInfo(serverId, out dbIp, out dbPort, out logDbIp, out logDbPort))
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("未能找到角色{0}的原服务器的服务器IP和端口", kuaFuServerLoginData.RoleId));
                                    return false;
                                }
                            }
                            break;
                        case (int)GameTypes.TianTi:
                            {
                                if (!TianTiClient.getInstance().CanKuaFuLogin()) return false;

                                socket.IsKuaFuLogin = TianTiClient.getInstance().KuaFuLogin(kuaFuServerLoginData);
                                if (!TianTiClient.getInstance().GetKuaFuDbServerInfo(serverId, out dbIp, out dbPort, out logDbIp, out logDbPort))
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("未能找到角色{0}的原服务器的服务器IP和端口", kuaFuServerLoginData.RoleId));
                                    return false;
                                }
                            }
                            break;
                        case (int)GameTypes.KingOfBattle:
                        case (int)GameTypes.YongZheZhanChang:
                        case (int)GameTypes.KuaFuBoss:
                            {
                                if (!YongZheZhanChangClient.getInstance().CanKuaFuLogin()) return false;

                                socket.IsKuaFuLogin = YongZheZhanChangClient.getInstance().KuaFuLogin(kuaFuServerLoginData);
                                if (!YongZheZhanChangClient.getInstance().GetKuaFuDbServerInfo(serverId, out dbIp, out dbPort, out logDbIp, out logDbPort))
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("未能找到角色{0}的原服务器的服务器IP和端口", kuaFuServerLoginData.RoleId));
                                    return false;
                                }
                            }
                            break;
                        case (int)GameTypes.KuaFuMap:
                            {
                                if (!YongZheZhanChangClient.getInstance().CanKuaFuLogin()) return false;

                                socket.IsKuaFuLogin = YongZheZhanChangClient.getInstance().CanEnterKuaFuMap(kuaFuServerLoginData);
                                if (!YongZheZhanChangClient.getInstance().GetKuaFuDbServerInfo(serverId, out dbIp, out dbPort, out logDbIp, out logDbPort))
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("未能找到角色{0}的原服务器的服务器IP和端口", kuaFuServerLoginData.RoleId));
                                    return false;
                                }
                            }
                            break;
                        case (int)GameTypes.LangHunLingYu:
                            {
                                if (!YongZheZhanChangClient.getInstance().CanKuaFuLogin()) return false;

                                socket.IsKuaFuLogin = LangHunLingYuManager.getInstance().CanEnterKuaFuMap(kuaFuServerLoginData);
                                if (!YongZheZhanChangClient.getInstance().GetKuaFuDbServerInfo(serverId, out dbIp, out dbPort, out logDbIp, out logDbPort))
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("未能找到角色{0}的原服务器的服务器IP和端口", kuaFuServerLoginData.RoleId));
                                    return false;
                                }
                            }
                            break;
                        case (int)GameTypes.KuaFuCopy:
                            {
                                if (!KFCopyRpcClient.getInstance().CanKuaFuLogin()) return false;

                                socket.IsKuaFuLogin = CopyTeamManager.Instance().HandleKuaFuLogin(kuaFuServerLoginData);
                                if (!KFCopyRpcClient.getInstance().GetKuaFuDbServerInfo(serverId, out dbIp, out dbPort, out logDbIp, out logDbPort))
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("未能找到角色{0}的原服务器的服务器IP和端口", kuaFuServerLoginData.RoleId));
                                    return false;
                                }
                            }
                            break;
                        case (int)GameTypes.ZhengBa:
                            {
                                if (!TianTiClient.getInstance().CanKuaFuLogin()) return false;

                                socket.IsKuaFuLogin = ZhengBaManager.Instance().CanKuaFuLogin(kuaFuServerLoginData);
                                if (!TianTiClient.getInstance().GetKuaFuDbServerInfo(serverId, out dbIp, out dbPort, out logDbIp, out logDbPort))
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("未能找到角色{0}的原服务器的服务器IP和端口", kuaFuServerLoginData.RoleId));
                                    return false;
                                }
                            }
                            break;
                        case (int)GameTypes.CoupleArena:
                            {
                                if (!TianTiClient.getInstance().CanKuaFuLogin()) return false;

                                socket.IsKuaFuLogin = CoupleArenaManager.Instance().CanKuaFuLogin(kuaFuServerLoginData);
                                if (!TianTiClient.getInstance().GetKuaFuDbServerInfo(serverId, out dbIp, out dbPort, out logDbIp, out logDbPort))
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("未能找到角色{0}的原服务器的服务器IP和端口", kuaFuServerLoginData.RoleId));
                                    return false;
                                }
                            }
                            break;
                    }

                    if (socket.IsKuaFuLogin && serverId != 0)
                    {
                        if (serverId != 0)
                        {
                            if (!InitGameDbConnection(serverId, dbIp, dbPort, logDbIp, logDbPort))
                            {
                                LogManager.WriteLog(LogTypes.Error, string.Format("连接角色{0}的原服务器的GameDBServer和LogDBServer失败", kuaFuServerLoginData.RoleId));
                                return false;
                            }
                        }

                        return socket.IsKuaFuLogin;
                    }
                }
                else
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("角色{0}未能在服务器列表中找本服务器，作为跨服服务器", kuaFuServerLoginData.RoleId));
                    return false;
                }
            }
            else
            {
                //非跨服登录
                if (HuanYingSiYuanClient.getInstance().LocalLogin(userID))
                {
                    kuaFuServerLoginData.RoleId = 0;
                    kuaFuServerLoginData.GameId = 0;
                    kuaFuServerLoginData.GameType = 0;
                    kuaFuServerLoginData.ServerId = 0;
                    socket.ServerId = 0;
                    socket.IsKuaFuLogin = false;
                    return true;
                }
            }

            LogManager.WriteLog(LogTypes.Error, string.Format("未能找到角色{0}的跨服活动或副本信息", kuaFuServerLoginData.RoleId));
            return false;
        }

        public bool OnInitGame(GameClient client)
        {
            int gameType = Global.GetClientKuaFuServerLoginData(client).GameType;
            switch(gameType)
            {
                case (int)GameTypes.HuanYingSiYuan:
                    return HuanYingSiYuanManager.getInstance().OnInitGame(client);
                case (int)GameTypes.TianTi:
                    return TianTiManager.getInstance().OnInitGame(client);
                case (int)GameTypes.YongZheZhanChang:
                    return YongZheZhanChangManager.getInstance().OnInitGame(client);
                case (int)GameTypes.KingOfBattle:
                    return KingOfBattleManager.getInstance().OnInitGame(client);
                case (int)GameTypes.MoRiJudge:
                    return MoRiJudgeManager.Instance().OnInitGame(client);
                case (int)GameTypes.ElementWar:
                    return ElementWarManager.getInstance().OnInitGame(client);
                case (int)GameTypes.KuaFuBoss:
                    return KuaFuBossManager.getInstance().OnInitGame(client);
                case (int)GameTypes.KuaFuMap:
                    return KuaFuMapManager.getInstance().OnInitGame(client);
                case (int)GameTypes.KuaFuCopy:
                    return CopyTeamManager.Instance().HandleKuaFuInitGame(client);
                case (int)GameTypes.LangHunLingYu:
                    return LangHunLingYuManager.getInstance().OnInitGameKuaFu(client);
                case (int)GameTypes.ZhengBa:
                    return ZhengBaManager.Instance().KuaFuInitGame(client);
                case (int)GameTypes.CoupleArena:
                    return CoupleArenaManager.Instance().KuaFuInitGame(client);
            }

            return false;
        }

        public void OnStartPlayGame(GameClient client)
        {
            int gameType = Global.GetClientKuaFuServerLoginData(client).GameType;
            switch (gameType)
            {
                case (int)GameTypes.KuaFuMap:
                    KuaFuMapManager.getInstance().OnStartPlayGame(client);
                    break;
            }
        }

        public void OnLeaveScene(GameClient client, SceneUIClasses sceneType, bool logout = false)
        {
            if (client.ClientSocket.IsKuaFuLogin)
            {
                switch (sceneType)
                {
                    case SceneUIClasses.HuanYingSiYuan:
                        HuanYingSiYuanManager.getInstance().OnLogout(client);
                        break;
                    case SceneUIClasses.TianTi:
                        TianTiManager.getInstance().OnLogout(client);
                        break;
                    case SceneUIClasses.YongZheZhanChang:
                        YongZheZhanChangManager.getInstance().OnLogout(client);
                        break;
                    case SceneUIClasses.KingOfBattle:
                        KingOfBattleManager.getInstance().OnLogout(client);
                        break;
                    case SceneUIClasses.MoRiJudge:
                        MoRiJudgeManager.Instance().OnLogOut(client);
                        break;
                    case SceneUIClasses.ElementWar:
                        ElementWarManager.getInstance().OnLogout(client);
                        break;
                    case SceneUIClasses.LangHunLingYu:
                        LangHunLingYuManager.getInstance().OnLogout(client);
                        break;
                    case SceneUIClasses.CopyWolf:
                        CopyWolfManager.getInstance().OnLogout(client);
                        break;
                    case SceneUIClasses.KFZhengBa:
                        ZhengBaManager.Instance().OnLogout(client);
                        break;
                    case SceneUIClasses.CoupleArena:
                        CoupleArenaManager.Instance().OnLeaveFuBen(client);
                        break;
                }

                if (!logout)
                {
                    GotoLastMap(client);
                }
            }
        }

        public void OnLogout(GameClient client)
        {
            int gameType = client.ClientData.SignUpGameType;
            switch (gameType)
            {
                case (int)GameTypes.HuanYingSiYuan:
                    HuanYingSiYuanClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.None, true);
                    break;
                case (int)GameTypes.TianTi:
                    TianTiClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.None, true);
                    break;
                case (int)GameTypes.YongZheZhanChang:
                    //不需要修改状态
                    break;
                    /*
                case (int)GameTypes.MoRiJudge:
                    MoRiJudgeClient.getInstance().ChangeRoleState(client.ClientData.RoleID, KuaFuRoleStates.None, true);
                    break;
                case (int)GameTypes.ElementWar:
                    ElementWarClient.getInstance().RoleChangeState(client.ClientData.RoleID, KuaFuRoleStates.None, true);
                    break;
                     */
            }
        }

        public void GotoLastMap(GameClient client)
        {
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, new KuaFuServerLoginData() { RoleId = 0 });
        }

        #endregion 辅助接口

        #region 跨服数据库连接管理

        private void BackGroudThreadProc()
        {
            do 
            {
                try
                {
                   // HandleTransferChatMsg();
                }
                catch
                {
                }

                Thread.Sleep(1800);
            } while (true);
        }

        private object DbMutex = new object();

        //private Dictionary<int, GameDbClientPool[]> GameDbConnectPoolDict = new Dictionary<int, GameDbClientPool[]>();
        private Dictionary<int, KuaFuDbConnection> GameDbConnectPoolDict = new Dictionary<int, KuaFuDbConnection>();

        public bool InitGameDbConnection(int serverId, string ip, int port, string logIp, int logPort)
        {
            KuaFuDbConnection pool;
            bool init = false;
            lock(DbMutex)
            {
                if (!GameDbConnectPoolDict.TryGetValue(serverId, out pool))
                {
                    pool = new KuaFuDbConnection(serverId);

                    GameDbConnectPoolDict[serverId] = pool;
                    init = true;
                }
                else
                {
                    pool.Pool[0].ChangeIpPort(ip, port);
                    pool.Pool[1].ChangeIpPort(logIp, logPort);
                }
            }

            if (init)
            {
                if (!pool.Pool[0].Init(3, ip, port, string.Format("server_db_{0}", serverId)) || !pool.Pool[1].Init(3, logIp, logPort, string.Format("server_log_{0}", serverId)))
                {
                    return false;
                }
            }
            else
            {
                return pool.Pool[0].Supply() && pool.Pool[1].Supply();
            }

            return true;
        }

        public TCPClient PopGameDbClient(int serverId, int poolId)
        {
            try
            {
                KuaFuDbConnection pool;
                lock (DbMutex)
                {
                    if (!GameDbConnectPoolDict.TryGetValue(serverId, out pool))
                    {
                        return null;
                    }
                }

                return pool.Pool[poolId].Pop();
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return null;
        }

        public void PushGameDbClient(int serverId, TCPClient tcpClient, int poolId)
        {
            try
            {
                KuaFuDbConnection pool;
                lock (DbMutex)
                {
                    if (!GameDbConnectPoolDict.TryGetValue(serverId, out pool))
                    {
                        return;
                    }
                }

                pool.Pool[poolId].Push(tcpClient);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }
        }

        /// <summary>
        /// 上次处理GM指令的时间
        /// </summary>
        public long LastTransferTicks = 0;

        /// <summary>
        /// 发送的心跳的次数(便于DBServer识别是否是重新上线的服务器)
        /// </summary>
        public int SendServerHeartCount = 0;

        /// <summary>
        /// 活跃的服务器列表
        /// </summary>
        private List<KuaFuDbConnection> ActiveServerIdList = new List<KuaFuDbConnection>();

        /// <summary>
        /// 处理聊天消息转发的操作
        /// </summary>
        private void HandleTransferChatMsg()
        {
            long ticks = TimeUtil.NOW();
            if (ticks - LastTransferTicks < (1000))
            {
                return;
            }

            LastTransferTicks = ticks; //记录时间

            string strcmd = "";

            TCPOutPacket tcpOutPacket = null;
            strcmd = string.Format("{0}:{1}:{2}:{3}", GameManager.ServerLineID, 0, SendServerHeartCount, "");
            SendServerHeartCount++; //为了标识是否是第一次

            ActiveServerIdList.Clear();
            lock (DbMutex)
            {
                foreach (var connection in GameDbConnectPoolDict.Values)
                {
                    if ((connection.ServerId % 3) == (SendServerHeartCount % 3))
                    {
                        ActiveServerIdList.Add(connection);
                    }
                }
            }

            foreach (var connection in ActiveServerIdList)
            {
                try
                {
                    List<string> chatMsgList = Global.sendToDB<List<string>, string>((int)TCPGameServerCmds.CMD_DB_GET_CHATMSGLIST, strcmd, connection.ServerId);
                    if (null != chatMsgList && chatMsgList.Count > 0)
                    {
                        //此处转发处理消息
                        for (int i = 0; i < chatMsgList.Count; i++)
                        {
                            GameManager.ClientMgr.TransferChatMsg(chatMsgList[i]);
                        }
                    }

                    connection.ErrorCount = 0;
                }
                catch (System.Exception ex)
                {
                    LogManager.WriteExceptionUseCache(ex.ToString());
                    connection.ErrorCount++;
                }

                if (connection.ErrorCount > 20) 
                {
                    //失联持续60秒后进来
                    lock (DbMutex)
                    {
                        GameDbConnectPoolDict.Remove(connection.ServerId);
                    }
                }
            }
        }

        #endregion 跨服数据库连接管理

        #region 跨服活动免排队管理

        public bool IsKuaFuMap(int mapCode)
        {
            SceneUIClasses sceneType = Global.GetMapSceneType(mapCode);
            switch(sceneType)
            {
                case SceneUIClasses.HuanYingSiYuan:
                case SceneUIClasses.TianTi:
                case SceneUIClasses.YongZheZhanChang:
                case SceneUIClasses.MoRiJudge:
                case SceneUIClasses.ElementWar:
                case SceneUIClasses.KuaFuBoss:
                case SceneUIClasses.KaLunTe:
                case SceneUIClasses.HuanShuYuan:
                case SceneUIClasses.LangHunLingYu:
                case SceneUIClasses.CopyWolf:
                case SceneUIClasses.KFZhengBa:
                case SceneUIClasses.CoupleArena:
                case SceneUIClasses.KingOfBattle:
                    return true;
            }

            return false;
        }

        #endregion 跨服活动免排队管理

        #region 跨服副本时间相关

        // 报名后等待匹配的最大时间
        public int SingUpMaxSeconds { get; private set; }
        // 匹配成功后，客户端点击确定进入的最大等待时间
        public int AutoCancelMaxSeconds { get; private set; }
        // 匹配成功后离开，那么在接下来的一段时间内，不能参加任何跨服副本
        public int CannotJoinCopyMaxSeconds { get; private set; }

        public void InitCopyTime()
        {
            int[] arr = GameManager.systemParamsList.GetParamValueIntArrayByName("KuaFuFuBenTime");

            if (arr != null && arr.Length >= 3)
            {
                SingUpMaxSeconds = arr[0];
                AutoCancelMaxSeconds = arr[1];
                CannotJoinCopyMaxSeconds = arr[2];
            }
        }

        // 设置不能参加跨服副本的结束tick
        public void SetCannotJoinKuaFu_UseAutoEndTicks( GameClient client)
        {
            if (CannotJoinCopyMaxSeconds <= 0) return;

            SetCannotJoinKuaFuCopyEndTicks(client, DateTime.Now.AddSeconds(CannotJoinCopyMaxSeconds).Ticks);
        }

        // 设置不能参加跨服副本的结束tick
        public void SetCannotJoinKuaFuCopyEndTicks(GameClient client, long endTicks)
        {
            if (client == null) return;

            Global.SaveRoleParamsInt64ValueToDB(client, RoleParamName.CannotJoinKFCopyEndTicks, endTicks, true);
            //client.sendCmd((int)TCPGameServerCmds.CMD_NTF_CANNOT_JOIN_KUAFU_FU_BEN_END_TICKS, endTicks);
        }

        // 是否处于不能参加跨服副本的时间
        public bool IsInCannotJoinKuaFuCopyTime(GameClient client)
        {
            if (client == null) return true;

            long endTicks = Global.GetRoleParamsInt64FromDB(client, RoleParamName.CannotJoinKFCopyEndTicks);
            return DateTime.Now.Ticks < endTicks;
        }

        public void NotifyClientCannotJoinKuaFuCopyEndTicks(GameClient client)
        {
            // 客户端说先不通知了
            long endTicks = Global.GetRoleParamsInt64FromDB(client, RoleParamName.CannotJoinKFCopyEndTicks);
            //client.sendCmd((int)TCPGameServerCmds.CMD_NTF_CANNOT_JOIN_KUAFU_FU_BEN_END_TICKS, endTicks);
        }

        #endregion
    }
}
