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
using Tmsk.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 跨服天梯管理
    /// </summary>
    public partial class KuaFuMapManager : IManager, ICmdProcessorEx, IManager2
    {
        enum EKuaFuMapEnterFlag
        {
            FromMapCode = 0,
            FromTeleport = 1,
            TargetBossId = 2,

            Max = 3
        }

        #region 标准接口

        public const SceneUIClasses ManagerType = SceneUIClasses.KuaFuMap;

        private static KuaFuMapManager instance = new KuaFuMapManager();

        public static KuaFuMapManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public KuaFuMapData RuntimeData = new KuaFuMapData();

        public bool initialize()
        {
            if (!InitConfig())
            {
                return false;
            }

            return true;
        }

        public bool initialize(ICoreInterface coreInterface)
        {
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("KuaFuBossManager.TimerProc", TimerProc), 15000, 5000);
            return true;
        }

        public bool startup()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KUAFU_MAP_ENTER, 2, 4, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_KUAFU_MAP_INFO, 1, 1, getInstance());
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
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_KUAFU_MAP_ENTER:
                    return ProcessKuaFuMapEnterCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_KUAFU_MAP_INFO:
                    return ProcessGetKuaFuLineDataListCmd(client, nID, bytes, cmdParams);
            }

            return true;
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
                    //线路地图配置
                    RuntimeData.LineMap2KuaFuLineDataDict.Clear();
                    RuntimeData.ServerMap2KuaFuLineDataDict.Clear();
                    RuntimeData.KuaFuMapServerIdDict.Clear();
                    RuntimeData.MapCode2KuaFuLineDataDict.Clear();

                    fileName = "Config/MapLine.xml";
                    fullPathFileName = Global.GameResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        int mapMaxOnlineCount = (int)Global.GetSafeAttributeLong(node, "MaxNum");
                        string str = Global.GetSafeAttributeStr(node, "Line");
                        if (!string.IsNullOrEmpty(str))
                        {
                            string[] mapLineStrs = str.Split('|');
                            foreach (var mapLineStr in mapLineStrs)
                            {
                                KuaFuLineData kuaFuLineData = new KuaFuLineData();
                                string[] mapLineParams = mapLineStr.Split(',');
                                kuaFuLineData.Line = int.Parse(mapLineParams[0]);
                                kuaFuLineData.MapCode = int.Parse(mapLineParams[1]);
                                if (mapLineParams.Length >= 3)
                                {
                                    kuaFuLineData.ServerId = int.Parse(mapLineParams[2]);
                                }

                                kuaFuLineData.MaxOnlineCount = mapMaxOnlineCount;
                                RuntimeData.LineMap2KuaFuLineDataDict.TryAdd(new IntPairKey(kuaFuLineData.Line, kuaFuLineData.MapCode), kuaFuLineData);
                                List<KuaFuLineData> list = null;
                                if (kuaFuLineData.ServerId > 0)
                                {
                                    if (RuntimeData.ServerMap2KuaFuLineDataDict.TryAdd(new IntPairKey(kuaFuLineData.ServerId, kuaFuLineData.MapCode), kuaFuLineData))
                                    {
                                        if (!RuntimeData.KuaFuMapServerIdDict.TryGetValue(kuaFuLineData.ServerId, out list))
                                        {
                                            list = new List<KuaFuLineData>();
                                            RuntimeData.KuaFuMapServerIdDict.TryAdd(kuaFuLineData.ServerId, list);
                                        }

                                        list.Add(kuaFuLineData);
                                    }

                                }

                                if (!RuntimeData.MapCode2KuaFuLineDataDict.TryGetValue(kuaFuLineData.MapCode, out list))
                                {
                                    list = new List<KuaFuLineData>();
                                    RuntimeData.MapCode2KuaFuLineDataDict.TryAdd(kuaFuLineData.MapCode, list);
                                }

                                list.Add(kuaFuLineData);
                            }
                        }
                    }
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

        #region 指令处理

        public bool IsKuaFuMap(int mapCode)
        {
            if (RuntimeData.MapCode2KuaFuLineDataDict.ContainsKey(mapCode))
            {
                return true;
            }

            return false;
        }

        // 检查该地图是否允许操作
        private bool CheckMap(GameClient client)
        {
            SceneUIClasses sceneType = Global.GetMapSceneType(client.ClientData.MapCode);
            if (sceneType != SceneUIClasses.Normal)
            {
                return false;
            }

            return true;
        }

        public bool ProcessGetKuaFuLineDataListCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int mapCode = Global.SafeConvertToInt32(cmdParams[0]);
                List<KuaFuLineData> list = YongZheZhanChangClient.getInstance().GetKuaFuLineDataList(mapCode) as List<KuaFuLineData>;
                client.sendCmd(nID, list);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessKuaFuMapEnterCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success_No_Info;
                int toMapCode = Global.SafeConvertToInt32(cmdParams[0]);
                int line = Global.SafeConvertToInt32(cmdParams[1]);
                int toBoss = 0;
                int teleportId = 0;
                if (cmdParams.Length >= 3) toBoss = Global.SafeConvertToInt32(cmdParams[2]);
                if (cmdParams.Length >= 4) teleportId = Global.SafeConvertToInt32(cmdParams[3]);

                do 
                {
                    if (!KuaFuMapManager.getInstance().IsKuaFuMap(toMapCode))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }

                    if (!Global.CanEnterMap(client, toMapCode) 
                        || toMapCode == client.ClientData.MapCode)
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }

                    // 新增需求，跨服主线地图能够直接进入另一个跨服主线地图
                    if (!KuaFuMapManager.getInstance().IsKuaFuMap(client.ClientData.MapCode)
                        && !CheckMap(client))
                    {
                        result = StdErrorCode.Error_Denied_In_Current_Map;
                        break;
                    }
                    
                    if(!IsGongNengOpened(client))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }

                    KuaFuLineData kuaFuLineData;
                    if (!RuntimeData.LineMap2KuaFuLineDataDict.TryGetValue(new IntPairKey(line, toMapCode), out kuaFuLineData))
                    {
                        result = StdErrorCode.Error_Operation_Denied;
                        break;
                    }

                    if (kuaFuLineData.OnlineCount >= kuaFuLineData.MaxOnlineCount)
                    {
                        result = StdErrorCode.Error_Server_Connections_Limit;
                        break;
                    }

                    int fromMapCode = client.ClientData.MapCode;
                    if (teleportId > 0)
                    {
                        // 要通过传送点进入跨服主线，必须检测是否能真正使用这个传送点
                        GameMap fromGameMap = null;
                        if (!GameManager.MapMgr.DictMaps.TryGetValue(fromMapCode, out fromGameMap))
                        {
                            result = StdErrorCode.Error_Config_Fault;
                            break;
                        }

                        MapTeleport mapTeleport = null;
                        if (!fromGameMap.MapTeleportDict.TryGetValue(teleportId, out mapTeleport) || mapTeleport.ToMapID != toMapCode)
                        {
                            result = StdErrorCode.Error_Operation_Denied;
                            break;
                        }

                        // 这里要增加一个位置判断，玩家是否在传送点附近， CMD_SPR_MAPCHANGE 里面没有判断，这里先放宽松一点
                        if (Global.GetTwoPointDistance(client.CurrentPos, new Point(mapTeleport.X, mapTeleport.Y)) > 800)
                        {
                            result = StdErrorCode.Error_Too_Far;
                            break;
                        }
                    }

                    int kuaFuServerId = YongZheZhanChangClient.getInstance().EnterKuaFuMap(client.ClientData.RoleID, kuaFuLineData.MapCode, kuaFuLineData.Line, client.ServerId, Global.GetClientKuaFuServerLoginData(client));
                    if (kuaFuServerId > 0)
                    {
                        // 废弃这个判断，两个跨服主线地图配在同一台服务器上，仍然统一短线重连<客户端并不需要知道没有跨到另一个服务器>
                        if (false && kuaFuServerId == GameManager.ServerId)
                        {
                            Global.GotoMap(client, toMapCode);
                        }
                        else
                        {
                            // 使用传送点，不扣金币
                            int needMoney = teleportId > 0 ? 0 : Global.GetMapTransNeedMoney(toMapCode);
                            if (Global.GetTotalBindTongQianAndTongQianVal(client) < needMoney)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(client, StringUtil.substitute(Global.GetLang("金币不足【{0}】,无法传送到【{1}】!"), needMoney, Global.GetMapName(toMapCode)), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoTongQian);
                                result = StdErrorCode.Error_JinBi_Not_Enough;
                            }
                            else
                            {
                                int[] enterFlags = new int[(int)EKuaFuMapEnterFlag.Max];
                                enterFlags[(int)EKuaFuMapEnterFlag.FromMapCode] = fromMapCode;
                                enterFlags[(int)EKuaFuMapEnterFlag.FromTeleport] = teleportId;
                                enterFlags[(int)EKuaFuMapEnterFlag.TargetBossId] = toBoss;
                                Global.SaveRoleParamsIntListToDB(client, new List<int>(enterFlags), RoleParamName.EnterKuaFuMapFlag, true);

                                GlobalNew.RecordSwitchKuaFuServerLog(client);
                                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_KF_SWITCH_SERVER, Global.GetClientKuaFuServerLoginData(client));
                            }
                        }
                    }
                    else
                    {
                        Global.GetClientKuaFuServerLoginData(client).RoleId = 0;
                        result = kuaFuServerId;
                    }
                } while (false);

                client.sendCmd(nID, result);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        #endregion 指令处理

        #region 其他

        public bool OnInitGame(GameClient client)
        {
            KuaFuServerLoginData kuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);

            //在跨服主线地图中,这个值表示存储编号
            client.ClientData.MapCode = (int)kuaFuServerLoginData.GameId;
            client.ClientData.PosX = 0;
            client.ClientData.PosY = 0;

            List<int> enterFlags = Global.GetRoleParamsIntListFromDB(client, RoleParamName.EnterKuaFuMapFlag);
            if (enterFlags != null && enterFlags.Count >= (int)EKuaFuMapEnterFlag.Max)
            {
                int fromMapCode = enterFlags[(int)EKuaFuMapEnterFlag.FromMapCode];
                int fromTeleport = enterFlags[(int)EKuaFuMapEnterFlag.FromTeleport];
                int targetBossId = enterFlags[(int)EKuaFuMapEnterFlag.TargetBossId];

                if (fromMapCode > 0 && fromTeleport > 0)
                {
                    // 要通过传送点进入跨服主线，必须检测是否能真正使用这个传送点
                    GameMap fromGameMap = null;
                    MapTeleport mapTeleport = null;
                    if (GameManager.MapMgr.DictMaps.TryGetValue(fromMapCode, out fromGameMap)
                        && fromGameMap.MapTeleportDict.TryGetValue(fromTeleport, out mapTeleport))
                    {
                        GameMap toGameMap = null;
                        if (GameManager.MapMgr.DictMaps.TryGetValue(mapTeleport.ToMapID, out toGameMap)
                            && toGameMap.CanMove(mapTeleport.ToX / toGameMap.MapGridWidth, mapTeleport.ToY / toGameMap.MapGridHeight))
                        {
                            client.ClientData.MapCode = mapTeleport.ToMapID;
                            client.ClientData.PosX = mapTeleport.ToX;
                            client.ClientData.PosY = mapTeleport.ToY;
                        }
                    }
                }

                if (targetBossId > 0)
                {
                    Global.ProcessVipLevelUp(client);
                    if (Global.IsVip(client)
                        && client.ClientData.VipLevel >= GameManager.systemParamsList.GetParamValueIntByName("VIPBossChuanSong", 4))
                    {
                        int bossX, bossY, radis;
                        if (GameManager.MonsterZoneMgr.GetMonsterBirthPoint(client.ClientData.MapCode, targetBossId, out bossX, out bossY, out radis))
                        {
                            radis = 1;
                            Point newPos = Global.GetMapPoint(ObjectTypes.OT_CLIENT, client.ClientData.MapCode, bossX, bossY, radis);
                            client.ClientData.PosX = (int)newPos.X;
                            client.ClientData.PosY = (int)newPos.Y;
                        }
                    }
                }
            }

            return true;
        }

        public void OnStartPlayGame(GameClient client)
        {
            bool bUserTeleport = false;
            List<int> enterFlags = Global.GetRoleParamsIntListFromDB(client, RoleParamName.EnterKuaFuMapFlag);
            if (enterFlags != null && enterFlags.Count >= (int)EKuaFuMapEnterFlag.Max && enterFlags[(int)EKuaFuMapEnterFlag.FromTeleport] > 0)
            {
                bUserTeleport = true;
            }

            if (!bUserTeleport)
            {
                // 传送点进入的不扣钱哈
                KuaFuServerLoginData kuaFuServerLoginData = Global.GetClientKuaFuServerLoginData(client);
                int mapCode = (int)kuaFuServerLoginData.GameId;
                int needMoney = Global.GetMapTransNeedMoney(mapCode);
                if (Global.SubBindTongQianAndTongQian(client, needMoney, "地图传送"))
                {
                    GameManager.ClientMgr.NotifyImportantMsg(client, StringUtil.substitute(Global.GetLang("传送到【{1}】消耗了【{0}】金币!"), needMoney, Global.GetMapName(mapCode)), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                }
            }

            // 清空进入标识
            int[] clearFlags = new int[(int)EKuaFuMapEnterFlag.Max];
            clearFlags[(int)EKuaFuMapEnterFlag.FromMapCode] = enterFlags[(int)EKuaFuMapEnterFlag.FromMapCode];
            clearFlags[(int)EKuaFuMapEnterFlag.FromTeleport] = 0;
            clearFlags[(int)EKuaFuMapEnterFlag.TargetBossId] = 0;
            Global.SaveRoleParamsIntListToDB(client, new List<int>(clearFlags), RoleParamName.EnterKuaFuMapFlag, true);
        }

        public void TimerProc(object sender, EventArgs e)
        {
            //更新每个跨服地图的人数
            Dictionary<int, int> dict = new Dictionary<int, int>();
            lock (RuntimeData.Mutex)
            {
                if (YongZheZhanChangClient.getInstance().CanKuaFuLogin())
                {
                    foreach (var mapCode in RuntimeData.MapCode2KuaFuLineDataDict.Keys)
                    {
                        dict[mapCode] = 0;
                    }
                }
            }

            List<int> list = dict.Keys.ToList();
            foreach (var mapCode in list)
            {
                dict[mapCode] = GameManager.ClientMgr.GetMapClientsCount(mapCode);
            }

            lock (RuntimeData.Mutex)
            {
                YongZheZhanChangClient.getInstance().UpdateKuaFuMapClientCount(dict);
            }
        }

        /// <summary>
        /// 判断功能是否开启
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool IsGongNengOpened(GameClient client, bool hint = false)
        {
            // 如果1.8的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot8))
                return false;

            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.KuaFuMap))
            {
                return false;
            }

            return true; //GlobalNew.IsGongNengOpened(client, GongNengIDs.KuaFuMap, hint);
        }

        #endregion 其他
    }
}
