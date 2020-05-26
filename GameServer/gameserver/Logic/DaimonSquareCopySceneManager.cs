#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Xml.Linq;
using Server.Data;
using System.Windows;
using Server.Tools;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using Server.Protocol;
using System.Threading;
using GameServer.Core.Executor;
using Tmsk.Contract;

namespace GameServer.Logic
{
    // 恶魔广场副本管理类 [7/10/2014 LiaoWei]
    public class DaimonSquareCopySceneManager
    {
        /// <summary>
        /// 恶魔广场副本场景list
        /// </summary>
        public Dictionary<int, CopyMap> m_DaimonSquareCopyScenesList = new Dictionary<int, CopyMap>();    // KEY-副本顺序ID VALUE-副本信息

        /// <summary>
        /// 恶魔广场副本场景数据
        /// </summary>
        public Dictionary<int, Dictionary<int, DaimonSquareScene>> m_DaimonSquareCopyScenesInfo = new Dictionary<int, Dictionary<int, DaimonSquareScene>>(); // KEY-副本ID VALUE- KEY-副本顺序ID VALUE-DaimonSquareScene信息

        /// <summary>
        /// 线程锁对象 -- 恶魔广场场景
        /// </summary>
        public static object m_Mutex = new object();

        /// <summary>
        /// 最高积分 -- 分数
        /// </summary>
        public int m_nDaimonSquareMaxPoint = -1;

        /// <summary>
        /// 最高积分 -- 人名
        /// /// </summary>
        public string m_nDaimonSquareMaxName = "";

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long LastHeartBeatTicks = 0L;

        /// <summary>
        /// 加载恶魔广场场景到管理器
        /// </summary>
        public void InitDaimonSquareCopyScene()
        {
            // 向DB请求最高积分信息
            Global.QueryDayActivityTotalPointInfoToDB(SpecialActivityTypes.DemoSque);

        }

        /// <summary>
        /// 设置最高积分信息
        /// </summary>
        public void SetDaimonSquareCopySceneTotalPoint(string sName, int nPoint)
        {
            m_nDaimonSquareMaxName = sName;
            m_nDaimonSquareMaxPoint = nPoint;
        }

        public bool CanEnterExistCopyScene(GameClient client)
        {
            CopyMap copyMap = null;
            int fuBenSeqId = client.ClientData.FuBenSeqID;
            lock (m_DaimonSquareCopyScenesList)
            {
                if (!m_DaimonSquareCopyScenesList.TryGetValue(fuBenSeqId, out copyMap))
                {
                    return false;
                }
            }

            lock (m_DaimonSquareCopyScenesInfo)
            {
                Dictionary<int, DaimonSquareScene> dicTmp = null;
                DaimonSquareScene bcData = null;

                int nFubenID = copyMap.FubenMapID;
                if (!m_DaimonSquareCopyScenesInfo.TryGetValue(nFubenID, out dicTmp))
                {
                    return false;
                }

                if (!dicTmp.TryGetValue(fuBenSeqId, out bcData))
                {
                    return false;
                }

                if (bcData.m_eStatus >= DaimonSquareStatus.FIGHT_STATUS_END)
                {
                    return false;
                }

                return bcData.CantiansRole(client);
            }
        }

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public void AddDaimonSquareCopyScenes(int nSequenceID, int nFubenID, int nMapCodeID, CopyMap mapInfo)
        {
            lock (m_DaimonSquareCopyScenesList)
            {
                CopyMap cmInfo = null;
                if (!m_DaimonSquareCopyScenesList.TryGetValue(nSequenceID, out cmInfo) || cmInfo == null)
                {
                    m_DaimonSquareCopyScenesList.Add(nSequenceID, mapInfo);
                }
            }

            lock (m_DaimonSquareCopyScenesInfo)
            {
                Dictionary<int, DaimonSquareScene> dicTmp = null;
                DaimonSquareScene bcData = null;

                if (!m_DaimonSquareCopyScenesInfo.TryGetValue(nFubenID, out dicTmp))
                {
                    dicTmp = new Dictionary<int, DaimonSquareScene>();
                    m_DaimonSquareCopyScenesInfo.Add(nFubenID, dicTmp);
                }

                if (!dicTmp.TryGetValue(nSequenceID, out bcData))
                {
                    bcData = new DaimonSquareScene();
                    bcData.CleanAllInfo();

                    dicTmp[nSequenceID] = bcData;
                }

                bcData.m_nMapCode = nMapCodeID;
                bcData.m_CopyMap = mapInfo;
            }
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public void RemoveDaimonSquareListCopyScenes(CopyMap cmInfo, int nSqeID, int nCopyID)
        {
            lock (m_DaimonSquareCopyScenesList)
            {
                CopyMap cmTmp = null;
                if (m_DaimonSquareCopyScenesList.TryGetValue(nSqeID, out cmTmp) && cmTmp != null)
                {
                    m_DaimonSquareCopyScenesList.Remove(nSqeID);
                }
            }

            lock (m_DaimonSquareCopyScenesInfo)
            {
                Dictionary<int, DaimonSquareScene> dicTmp = null;
                if (m_DaimonSquareCopyScenesInfo.TryGetValue(nCopyID, out dicTmp) && dicTmp != null)
                {
                    DaimonSquareScene bcTmp = null;
                    if (dicTmp.TryGetValue(nSqeID, out bcTmp) && bcTmp != null)
                        dicTmp.Remove(nSqeID);

                    if (dicTmp.Count <= 0)
                        m_DaimonSquareCopyScenesInfo.Remove(nCopyID);
                }
            }
        }

        /// <summary>
        /// 检测场景管理器
        /// </summary>
        public int CheckDaimonSquareListScenes(int nFuBenMapID)
        {
            lock (m_DaimonSquareCopyScenesInfo)
            {
                Dictionary<int, DaimonSquareScene> tmpData = null;

                if (!m_DaimonSquareCopyScenesInfo.TryGetValue(nFuBenMapID, out tmpData))
                    return -1;

                if (tmpData == null)
                    return -1;

                DaimonSquareDataInfo dsDataTmp = null;

                if (!Data.DaimonSquareDataInfoList.TryGetValue(nFuBenMapID, out dsDataTmp))
                    return -1;

                if (dsDataTmp == null)
                    return -1;

                foreach (var dsData in tmpData)
                {
                    int nID = -1;
                    nID = dsData.Key;

                    DaimonSquareScene tmpdsinfo = null;
                    tmpdsinfo = dsData.Value;

                    if (nID < 0 || tmpdsinfo == null)
                        continue;

                    if (nID == nFuBenMapID && tmpdsinfo.m_nPlarerCount < dsDataTmp.MaxEnterNum && tmpdsinfo.m_eStatus < DaimonSquareStatus.FIGHT_STATUS_BEGIN)
                        return nID;
                }

            }

            return -1;
        }

        /// <summary>
        /// 根据副本地图ID检测是否是恶魔广场副本
        /// </summary>
        public bool IsDaimonSquareCopyScene(int nFuBenMapID)
        {
            if (Data.DaimonSquareDataInfoList.ContainsKey(nFuBenMapID))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据场景ID检测是否是恶魔广场副本
        /// </summary>
        public bool IsDaimonSquareCopyScene2(int nMpaCodeID)
        {
            SceneUIClasses sceneType = Global.GetMapSceneType(nMpaCodeID);
            if (sceneType == SceneUIClasses.Demon)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 取得血色城堡副本信息
        /// </summary>
        public CopyMap GetDaimonSquareCopySceneInfo(int nSequenceID)
        {
            if (nSequenceID < 0)
                return null;

            CopyMap copymapInfo = null;
            if (!m_DaimonSquareCopyScenesList.TryGetValue(nSequenceID, out copymapInfo))
                return null;

            return copymapInfo;
        }

        /// <summary>
        /// 取得血色城堡信息
        /// </summary>
        public DaimonSquareScene GetDaimonSquareCopySceneDataInfo(CopyMap cmInfo, int nSequenceID, int nFuBenID)
        {
            if (cmInfo == null || nSequenceID < 0)
                return null;

            Dictionary<int, DaimonSquareScene> dicTmp = null;
            if (!m_DaimonSquareCopyScenesInfo.TryGetValue(nFuBenID, out dicTmp) || dicTmp == null)
                return null;

            DaimonSquareScene dsInfo = null;
            if (!dicTmp.TryGetValue(nSequenceID, out dsInfo) || dsInfo == null)
                return null;

            return dsInfo;
        }

        /// <summary>
        /// 玩家进入血色城堡副本计数
        /// </summary>
        public int EnterDaimonSquareSceneCopySceneCount(GameClient client, int nFubenID, out int nDemonNum)
        {
            nDemonNum = -1;

            DaimonSquareDataInfo dsDataTmp = null;
            if (!Data.DaimonSquareDataInfoList.TryGetValue(nFubenID, out dsDataTmp))
                return -1;

            int nDate = TimeUtil.NowDateTime().DayOfYear;                 // 当前时间
            int nType = (int)SpecialActivityTypes.DemoSque;     // 恶魔广场

            int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, nType);
            nDemonNum = nCount;

            if (nCount >= dsDataTmp.MaxEnterNum)
            {
                bool nRet = true;

                // VIP检测
                int dayID = TimeUtil.NowDateTime().DayOfYear;
                int nVipLev = client.ClientData.VipLevel;

                int nNum = 0;
                int[] nArry = null;
                nArry = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPEnterDaimonSquareCountAddValue");

                if (nVipLev > 0 && nArry != null && nArry[nVipLev] > 0)
                {
                    nNum = nArry[nVipLev];

                    if (nCount < dsDataTmp.MaxEnterNum + nNum)
                    {
                        Global.UpdateVipDailyData(client, dayID, (int)VIPTYPEEum.VIPTYPEEUM_ENTERDAIMONSQUARE);
                        nRet = false;
                    }
                }

                if (nRet == true)
                {
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                       StringUtil.substitute(Global.GetLang("您今天进入恶魔广场已达上限 请明天再试")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);

                    return -1;
                }
            }

            return 1;
        }

        /// <summary>
        /// 发消息给客户端
        /// </summary>
        public void SendMegToClient(GameClient client, int nFubenID, int nSquID, int nCmdID)
        {
            CopyMap cmInfo = null;
            lock (m_DaimonSquareCopyScenesList)
            {
                if (!m_DaimonSquareCopyScenesList.TryGetValue(nSquID, out cmInfo) || cmInfo == null)
                    return;
            }

            lock (m_DaimonSquareCopyScenesInfo)
            {
                Dictionary<int, DaimonSquareScene> dicTmp = null;
                if (m_DaimonSquareCopyScenesInfo.TryGetValue(nFubenID, out dicTmp) && dicTmp != null)
                {
                    DaimonSquareScene dsTmp = null;
                    if (dicTmp.TryGetValue(nSquID, out dsTmp) && dsTmp != null)
                    {
                        string strcmd = "";
                        
                        DaimonSquareDataInfo dsDataTmp = null;

                        if (!Data.DaimonSquareDataInfoList.TryGetValue(nFubenID, out dsDataTmp) || dsDataTmp == null)
                            return;

                        if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUARETIMERINFO)
                        {
                            // 通知客户端 显示准备战斗倒时间
                            long ticks = TimeUtil.NOW();    // 当前tick

                            if (dsTmp.m_lPrepareTime <= 0)
                            {
                                return;
                            }

                            // 恶魔广场时间信息
                            int nTimer = 15;
                            if (dsTmp.m_eStatus == DaimonSquareStatus.FIGHT_STATUS_PREPARE)
                            {
                                nTimer = (int)((dsDataTmp.PrepareTime * 1000 - (ticks - dsTmp.m_lPrepareTime)) / 1000);
                            }
                            else if (dsTmp.m_eStatus == DaimonSquareStatus.FIGHT_STATUS_BEGIN)
                            {
                                nTimer = (int)((dsDataTmp.DurationTime * 1000 - (ticks - dsTmp.m_lBeginTime)) / 1000);
                            }

                            strcmd = string.Format("{0}:{1}", (int)dsTmp.m_eStatus, nTimer);
                            GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUARETIMERINFO);
                        }
                        else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_DAIMONSQUAREPLAYERNUMNOTIFY)
                        {
                            if (dsTmp.m_eStatus <= DaimonSquareStatus.FIGHT_STATUS_PREPARE)
                            {
                                strcmd = string.Format("{0}", dsTmp.m_nPlarerCount);

                                GameManager.ClientMgr.NotifyDaimonSquareCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, cmInfo,
                                                                        (int)TCPGameServerCmds.CMD_SPR_DAIMONSQUAREPLAYERNUMNOTIFY, 0, 0, 0, dsTmp.m_nPlarerCount);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 玩家进入恶魔广场副本
        /// </summary>
        public int EnterDaimonSquareSceneCopyScene(GameClient client, int nFubenID, int nDemonNum, out int nSeqID, int mapCode)
        {
            string strcmd = "";

            nSeqID = -1;

            // 没有领取积分奖励
            if (client.ClientData.DaimonSquarePoint > 0)
            {
                int FuBenSeqID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DaimonSquareFuBenSeqID);
                int nSceneID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DaimonSquareSceneid);
                int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DaimonSquareSceneFinishFlag);

                //如果已经获取过一次奖励，则不再提示奖励
                //查找角色的ID+副本顺序ID对应地图编号的奖励领取状态
                int awardState = GameManager.CopyMapMgr.FindAwardState(client.ClientData.RoleID, FuBenSeqID, nSceneID);
                if (awardState == 0)
                {
                    DaimonSquareDataInfo dsDataTmp = null;
                    if (Data.DaimonSquareDataInfoList.TryGetValue(nSceneID, out dsDataTmp))
                    {
                        if (dsDataTmp == null)
                        {
                            client.ClientData.DaimonSquarePoint = 0;
                            return 1;
                        }

                        string sAwardItem = null;

                        for (int n = 0; n < dsDataTmp.AwardItem.Length; ++n)
                        {
                            sAwardItem += dsDataTmp.AwardItem[n];
                            if (n != dsDataTmp.AwardItem.Length - 1)
                                sAwardItem += "|";
                        }

                        // 1.是否成功完成 2.玩家的积分 3.玩家经验奖励 4.玩家的金钱奖励 5.玩家物品奖励
                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", nFlag, client.ClientData.DaimonSquarePoint,
                                                Global.CalcExpForRoleScore(client.ClientData.DaimonSquarePoint, dsDataTmp.ExpModulus),
                                                client.ClientData.DaimonSquarePoint * dsDataTmp.MoneyModulus, sAwardItem);

                        GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_DAIMONSQUAREENDFIGHT);

                        return -1;
                    }
                }
                else
                {
                    // 清空
                    client.ClientData.DaimonSquarePoint = 0;
                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquarePlayerPoint, client.ClientData.DaimonSquarePoint, true);
                }
            }

            int nFubenMapID = Global.GetDaimonSquareCopySceneIDForRole(client);

            if (nFubenMapID <= 0)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                            StringUtil.substitute(Global.GetLang("对不起 您没有达到进入恶魔广场的要求等级！！")),
                                                            GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);

                LogManager.WriteLog(LogTypes.Error, string.Format("enter bloodcastle scene fail!! get scene info fail!!!!"));

                return -1;
            }

            DaimonSquareDataInfo bcInfo = null;
            if (!Data.DaimonSquareDataInfoList.TryGetValue(nFubenMapID, out bcInfo) || bcInfo == null)
                return -1;

            // 时限段判断
            if (!Global.CanEnterDaimonSquareOnTime(bcInfo.BeginTime, bcInfo.PrepareTime))
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                            StringUtil.substitute(Global.GetLang("当前时间段恶魔广场并未开启，请稍后再试")), GameInfoTypeIndexes.Error,
                                                                ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                return -1;
            }

            // 需要物品判断
            GoodsData goodData = Global.GetGoodsByID(client, bcInfo.NeedGoodsID);
            if (goodData == null)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    StringUtil.substitute(Global.GetLang("对不起 您没有进入恶魔广场的道具！！")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);

                return -1;
            }

            if (goodData.GCount < bcInfo.NeedGoodsNum)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                       StringUtil.substitute(Global.GetLang("对不起 您拥有的进入恶魔广场的道具数量不够！！")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);

                return -1;
            }

            bool usedBinding = false;
            bool usedTimeLimited = false;

            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, bcInfo.NeedGoodsID, 1, false, out usedBinding, out usedTimeLimited))
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                       StringUtil.substitute(Global.GetLang("对不起 扣除进入恶魔广场的道具失败 进入失败！！")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);

                return -1;
            }

            Dictionary<int, DaimonSquareScene> dicTmp = null;

            lock (m_DaimonSquareCopyScenesInfo)
            {
                if (m_DaimonSquareCopyScenesInfo.TryGetValue(nFubenMapID, out dicTmp) && dicTmp != null)
                {
                    foreach (var dssceneInfo in dicTmp)
                    {
                        if (dssceneInfo.Value.m_eStatus >= DaimonSquareStatus.FIGHT_STATUS_BEGIN)
                        {
                            continue;
                        }
                        if (dssceneInfo.Value.m_nPlarerCount >= bcInfo.MaxPlayerNum)
                            continue;

                        ++dssceneInfo.Value.m_nPlarerCount;
                        nSeqID = dssceneInfo.Key;
                    }
                }

                if (nSeqID < 0)
                {
                    //从DBServer获取副本顺序ID
                    string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GETFUBENSEQID, string.Format("{0}", client.ClientData.RoleID), client.ServerId);
                    if (null != dbFields && dbFields.Length >= 2)
                    {
                        nSeqID = Global.SafeConvertToInt32(dbFields[1]);
                        if (nSeqID > 0)
                        {
                            DaimonSquareScene bcData = null;
                            if (!m_DaimonSquareCopyScenesInfo.TryGetValue(nFubenID, out dicTmp) || dicTmp == null)
                            {
                                dicTmp = new Dictionary<int, DaimonSquareScene>();
                                m_DaimonSquareCopyScenesInfo.Add(nFubenID, dicTmp);
                            }

                            if (!dicTmp.TryGetValue(nSeqID, out bcData) || bcData == null)
                            {
                                bcData = new DaimonSquareScene();
                                bcData.CleanAllInfo();
                                bcData.m_nMapCode = mapCode;
                                bcData.m_nPlarerCount = 1;

                                dicTmp[nSeqID] = bcData;
                            }
                        }
                    }
                }
            }

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquareFuBenSeqID, nSeqID, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquareSceneid, nFubenMapID, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquareSceneFinishFlag, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquareSceneTimer, 0, true);

            return 0;
        }
        
        /// <summary>
        // 心跳处理
        /// </summary>
        public void HeartBeatDaimonSquareScene()
        {
            long nowTicks = TimeUtil.NOW();
            if (Math.Abs(nowTicks - LastHeartBeatTicks) < (1000))
                return;

            LastHeartBeatTicks = nowTicks;

            lock (m_DaimonSquareCopyScenesList)
            {
                foreach (var DaimonSquareScenes in m_DaimonSquareCopyScenesList)
                {
                    int nID = -1;
                    nID = DaimonSquareScenes.Value.FuBenSeqID;

                    int nCopyID = -1;
                    nCopyID = DaimonSquareScenes.Value.FubenMapID;

                    int nMapCodeID = -1;
                    nMapCodeID = DaimonSquareScenes.Value.MapCode;

                    if (nID < 0 || nCopyID < 0 || nMapCodeID < 0)
                        continue;

                    lock(m_DaimonSquareCopyScenesInfo)
                    {
                        DaimonSquareDataInfo dsDataTmp = null;
                        if (!Data.DaimonSquareDataInfoList.TryGetValue(nCopyID, out dsDataTmp) || dsDataTmp == null)
                            continue;

                        Dictionary<int, DaimonSquareScene> dicTmp = null;
                        if (!m_DaimonSquareCopyScenesInfo.TryGetValue(nCopyID, out dicTmp) || dicTmp == null)
                            continue;

                        DaimonSquareScene dsTmp = null;
                        if (!dicTmp.TryGetValue(nID, out dsTmp) || dsTmp == null)
                            continue;

                        // 当前tick
                        long ticks = TimeUtil.NOW();

                        if (dsTmp.m_eStatus == DaimonSquareStatus.FIGHT_STATUS_NULL)
                        {
                            int nSecond = 0;
                            string strTimer = null;

                            if (Global.CanEnterDaimonSquareCopySceneOnTime(dsDataTmp.BeginTime, dsDataTmp.PrepareTime + dsDataTmp.DurationTime, out nSecond, out strTimer))
                            {
                                // 场景开启
                                dsTmp.m_eStatus = DaimonSquareStatus.FIGHT_STATUS_PREPARE;

                                DateTime staticTime = DateTime.Parse(strTimer);

                                dsTmp.m_lPrepareTime = staticTime.Ticks / 10000;//TimeUtil.NOW();

                                dsTmp.m_nMonsterTotalWave = dsDataTmp.MonsterID.Length;

                                List<GameClient> objsList = DaimonSquareScenes.Value.GetClientsList(); //发送给所有地图的用户
                                if (null == objsList)
                                    return;

                                for (int i = 0; i < objsList.Count; i++)
                                {
                                    SendMegToClient(objsList[i], nCopyID, nID, (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUARETIMERINFO);
                                    SendMegToClient(objsList[i], nCopyID, nID, (int)TCPGameServerCmds.CMD_SPR_DAIMONSQUAREPLAYERNUMNOTIFY);                                    
                                }   
                            }
                        }
                        else if (dsTmp.m_eStatus == DaimonSquareStatus.FIGHT_STATUS_PREPARE)
                        {
                            if (ticks >= (dsTmp.m_lPrepareTime + (dsDataTmp.PrepareTime * 1000)))
                            {
                                // 准备战斗
                                dsTmp.m_eStatus = DaimonSquareStatus.FIGHT_STATUS_BEGIN;

                                dsTmp.m_lBeginTime = TimeUtil.NOW();
                                int nTimer = (int)((dsDataTmp.DurationTime * 1000 - (ticks - dsTmp.m_lBeginTime)) / 1000);

                                GameManager.ClientMgr.NotifyDaimonSquareCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, DaimonSquareScenes.Value,
                                                                            (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUARETIMERINFO, (int)DaimonSquareStatus.FIGHT_STATUS_BEGIN, nTimer, 0, 0, 0); // 战斗结束倒计时

                                List<GameClient> clientList = dsTmp.m_CopyMap.GetClientsList();
                                if (null != clientList)
                                {
                                    foreach (var c in clientList)
                                    {
                                        if (dsTmp.AddRole(c))
                                        {
                                            Global.UpdateRoleEnterActivityCount(c, SpecialActivityTypes.DemoSque);
                                        }
                                    }
                                }

                            }
                        }
                        else if (dsTmp.m_eStatus == DaimonSquareStatus.FIGHT_STATUS_BEGIN)
                        {
                            // 开始战斗 -- 刷怪
                            bool bNeedCreateMonster = false;
                            lock (dsTmp.m_CreateMonsterMutex)
                            {
                                if (dsTmp.m_nCreateMonsterFlag == 0 && dsTmp.m_nMonsterWave < dsTmp.m_nMonsterTotalWave)
                                {
                                    bNeedCreateMonster = true;
                                }

                                if (ticks >= (dsTmp.m_lBeginTime + (dsDataTmp.DurationTime * 1000)) || dsTmp.m_nKillMonsterTotalNum == dsDataTmp.MonsterSum)
                                {
                                    dsTmp.m_eStatus = DaimonSquareStatus.FIGHT_STATUS_END;
                                    dsTmp.m_lEndTime = TimeUtil.NOW();

                                    try
                                    {
                                        /**/string log = string.Format("恶魔广场已结束,是否完成{0},结束时间{1},m_nCreateMonsterFlag:{2},m_nMonsterWave:{3},m_nMonsterTotalWave:{4},m_nKillMonsterNum:{5},m_nNeedKillMonsterNum:{6}",
                                                                    dsTmp.m_bIsFinishTask, new DateTime(dsTmp.m_lEndTime * 10000), dsTmp.m_nCreateMonsterFlag,
                                                                    dsTmp.m_nMonsterWave, dsTmp.m_nMonsterTotalWave, dsTmp.m_nKillMonsterNum, dsTmp.m_nNeedKillMonsterNum);
                                        LogManager.WriteLog(LogTypes.Error, log);
                                    } catch{}
                                }
                            }

                            if (bNeedCreateMonster)
                            {
                                DaimonSquareSceneCreateMonster(dsTmp, dsDataTmp, DaimonSquareScenes.Value.CopyMapID, DaimonSquareScenes.Value);
                            }
                        }
                        else if (dsTmp.m_eStatus == DaimonSquareStatus.FIGHT_STATUS_END)
                        {
                            // 战斗结束

                            int nTimer = (int)((dsDataTmp.LeaveTime * 1000 - (ticks - dsTmp.m_lEndTime)) / 1000);

                            if (dsTmp.m_bEndFlag == false)
                            {
                                GameManager.ClientMgr.NotifyDaimonSquareCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, DaimonSquareScenes.Value,
                                                                            (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUARETIMERINFO, (int)DaimonSquareStatus.FIGHT_STATUS_END, nTimer, 0, 0, 0);

                                // 剩余时间奖励
                                long nTimeInfo = 0;
                                nTimeInfo = dsTmp.m_lEndTime - dsTmp.m_lBeginTime;

                                long nRemain = 0;
                                nRemain = ((dsDataTmp.DurationTime * 1000) - nTimeInfo) / 1000;

                                if (nRemain >= dsDataTmp.DurationTime)
                                    nRemain = dsDataTmp.DurationTime / 2;

                                int nTimeAward = 0;
                                nTimeAward = (int)(dsDataTmp.TimeModulus * nRemain);

                                if (nTimeAward < 0)
                                    nTimeAward = 0;

                                GameManager.ClientMgr.NotifyDaimonSquareCopySceneMsgEndFight(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, DaimonSquareScenes.Value,
                                                                                                dsTmp, (int)TCPGameServerCmds.CMD_SPR_DAIMONSQUAREENDFIGHT, nTimeAward);

                                dsTmp.m_bEndFlag = true;
                            }

                            if (ticks >= (dsTmp.m_lEndTime + (dsDataTmp.LeaveTime * 1000) + 3000))
                            {
                                try
                                {
                                    if (DaimonSquareScenes.Value == GameManager.CopyMapMgr.FindCopyMap(DaimonSquareScenes.Value.MapCode, DaimonSquareScenes.Value.FuBenSeqID))
                                    {
                                        if (!dsTmp.ClearRole)
                                        {
                                            dsTmp.ClearRole = true;

                                            // 清场
                                            List<GameClient> objsList = DaimonSquareScenes.Value.GetClientsList();
                                            if (objsList != null && objsList.Count > 0)
                                            {
                                                /**/string log = string.Format("恶魔广场已结束,是否完成{0},结束时间{1},m_nCreateMonsterFlag:{2},m_nMonsterWave:{3},m_nMonsterTotalWave:{4},m_nKillMonsterNum:{5},m_nNeedKillMonsterNum:{6},踢出玩家列表:",
                                                    dsTmp.m_bIsFinishTask, new DateTime(dsTmp.m_lEndTime * 10000), dsTmp.m_nCreateMonsterFlag,
                                                    dsTmp.m_nMonsterWave, dsTmp.m_nMonsterTotalWave, dsTmp.m_nKillMonsterNum, dsTmp.m_nNeedKillMonsterNum);
                                                LogManager.WriteLog(LogTypes.Error, log);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        int ii = 1;
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    DataHelper.WriteExceptionLogEx(ex, "恶魔广场调度异常");
                                }
                            }
                        }
                    }
                    
                }
            }            

        }

        /// <summary>
        // 刷怪接口
        /// </summary>
        public void DaimonSquareSceneCreateMonster(DaimonSquareScene bcTmp, DaimonSquareDataInfo bcDataTmp, int nCopyMapID, CopyMap cmInfo)
        {
            string[] sNum = null;
            string[] sID = null;
            string[] sRate = null;

            lock (bcTmp.m_CreateMonsterMutex)
            {
                if (bcTmp.m_nMonsterWave >= bcTmp.m_nMonsterTotalWave)
                    return;

                // 置刷怪标记
                bcTmp.m_nCreateMonsterFlag = 1;

                string sMonsterNum = null;
                string sMonsterID = null;
                string sNeedSkillMonster = null;

                sMonsterNum = bcDataTmp.MonsterNum[bcTmp.m_nMonsterWave];
                sMonsterID = bcDataTmp.MonsterID[bcTmp.m_nMonsterWave];
                sNeedSkillMonster = bcDataTmp.CreateNextWaveMonsterCondition[bcTmp.m_nMonsterWave];

                if (sMonsterID == null || sMonsterNum == null || sNeedSkillMonster == null)
                    return;


                sNum = sMonsterNum.Split(',');
                sID = sMonsterID.Split(',');
                sRate = sNeedSkillMonster.Split(',');

                if (sNum.Length != sID.Length)
                    return;

                for (int i = 0; i < sNum.Length; ++i)
                {
                    int nNum = Global.SafeConvertToInt32(sNum[i]);
                    int nID = Global.SafeConvertToInt32(sID[i]);
                    for (int j = 0; j < nNum; ++j)
                    {
                        ++bcTmp.m_nCreateMonsterCount;
                    }
                }

                // 计数要杀死怪的数量
                bcTmp.m_nNeedKillMonsterNum = (int)Math.Ceiling(bcTmp.m_nCreateMonsterCount * Global.SafeConvertToDouble(sRate[0]) / 100);

                // 递增刷怪波数
                ++bcTmp.m_nMonsterWave;
            }

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(bcTmp.m_nMapCode, out gameMap))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("恶魔广场报错 地图配置 ID = {0}", bcDataTmp.MapCode));
                return;
            }

            int gridX = gameMap.CorrectWidthPointToGridPoint(bcDataTmp.posX + Global.GetRandomNumber(-bcDataTmp.Radius, bcDataTmp.Radius)) / gameMap.MapGridWidth;
            int gridY = gameMap.CorrectHeightPointToGridPoint(bcDataTmp.posZ + Global.GetRandomNumber(-bcDataTmp.Radius, bcDataTmp.Radius)) / gameMap.MapGridHeight;

            int gridNum = gameMap.CorrectWidthPointToGridPoint(bcDataTmp.Radius);

            for (int i = 0; i < sNum.Length; ++i)
            {
                int nNum = Global.SafeConvertToInt32(sNum[i]);
                int nID = Global.SafeConvertToInt32(sID[i]);
                for (int j = 0; j < nNum; ++j)
                {
                    GameManager.MonsterZoneMgr.AddDynamicMonsters(bcTmp.m_nMapCode, nID, nCopyMapID, 1, gridX, gridY, gridNum);
                }
            }

            // 恶魔广场怪物波和人物得分信息
            GameManager.ClientMgr.NotifyDaimonSquareCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, cmInfo, (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUAREMONSTERWAVEANDPOINTRINFO,
                                                        0, 0, bcDataTmp.MonsterID.Length - bcTmp.m_nMonsterWave, -100, 0);  // 只更新波数

            //SysConOut.WriteLine("liaowei是帅哥  恶魔广场{0}里 刷第{1}波怪了 一共{3}只！！！", bcTmp.m_nMapCode, bcTmp.m_nMonsterWave, bcTmp.m_nCreateMonsterCount);

            return;
        }

        public void OnStartPlayGame(GameClient client)
        {
            if (client.ClientData.FuBenSeqID < 0 || client.ClientData.CopyMapID < 0 || !IsDaimonSquareCopyScene(client.ClientData.FuBenID))
                return;

            DaimonSquareDataInfo bcDataTmp = null;
            if (!Data.DaimonSquareDataInfoList.TryGetValue(client.ClientData.FuBenID, out bcDataTmp) || bcDataTmp == null)  //bcDataTmp = Data.DaimonSquareDataInfoList[client.ClientData.MapCode];
                return;

            Dictionary<int, DaimonSquareScene> dicTmp = null;
            if (!m_DaimonSquareCopyScenesInfo.TryGetValue(client.ClientData.FuBenID, out dicTmp) || dicTmp == null)
                return;

            DaimonSquareScene bcTmp = null;
            if (!dicTmp.TryGetValue(client.ClientData.FuBenSeqID, out bcTmp) || bcTmp == null)
                return;

            string strcmd = string.Format("{0}:{1}", bcDataTmp.MonsterID.Length - bcTmp.m_nMonsterWave, client.ClientData.DaimonSquarePoint);
            GameManager.ClientMgr.SendToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, strcmd, (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUAREMONSTERWAVEANDPOINTRINFO);
        }

        /// <summary>
        // 杀怪接口
        /// </summary>
        public void DaimonSquareSceneKillMonster(GameClient client, Monster monster)
        {
            if (client.ClientData.FuBenSeqID < 0 || client.ClientData.CopyMapID < 0 || !IsDaimonSquareCopyScene(client.ClientData.FuBenID))
                return;

            DaimonSquareDataInfo bcDataTmp = null;
            if (!Data.DaimonSquareDataInfoList.TryGetValue(client.ClientData.FuBenID, out bcDataTmp) || bcDataTmp == null)  //bcDataTmp = Data.DaimonSquareDataInfoList[client.ClientData.MapCode];
                return;

            Dictionary<int, DaimonSquareScene> dicTmp = null;
            if (!m_DaimonSquareCopyScenesInfo.TryGetValue(client.ClientData.FuBenID, out dicTmp) || dicTmp == null)
                return;

            DaimonSquareScene bcTmp = null;
            if (!dicTmp.TryGetValue(client.ClientData.FuBenSeqID, out bcTmp) || bcTmp == null)
                return;

            //如果是重复记录击杀同一只怪,则直接返回
            if (!bcTmp.AddKilledMonster(monster))
            {
                return;
            }
            if (bcTmp.m_bEndFlag == true)
                return;

            bool bCompleteDaimonSquareScene = false;
            lock (bcTmp.m_CreateMonsterMutex)
            {
                bcTmp.m_nKillMonsterNum++;

                //SysConOut.WriteLine("liaowei是帅哥  恶魔广场{0}里 杀了{1}只怪了！！！", client.ClientData.MapCode, bcTmp.m_nKillMonsterNum);
#if ___CC___FUCK___YOU___BB___
                client.ClientData.DaimonSquarePoint += 0;
#else
                client.ClientData.DaimonSquarePoint += monster.MonsterInfo.DaimonSquareJiFen;
#endif

                if (bcTmp.m_nCreateMonsterFlag == 1 && bcTmp.m_nKillMonsterNum == bcTmp.m_nNeedKillMonsterNum)
                {
                    bcTmp.m_nCreateMonsterFlag = 0;

                    // 重新计数
                    bcTmp.m_nNeedKillMonsterNum = 0;
                    bcTmp.m_nKillMonsterNum = 0;

                    bcTmp.m_nCreateMonsterCount = 0;
                }

                // 如果杀光了所有的怪 就胜利了
                if (bcTmp.m_nKillMonsterTotalNum == bcDataTmp.MonsterSum)
                {
                    bcTmp.m_eStatus = DaimonSquareStatus.FIGHT_STATUS_END;

                    bcTmp.m_lEndTime = TimeUtil.NOW();


                    bcTmp.m_bIsFinishTask = true;
                }
            }

            if (bCompleteDaimonSquareScene)
            {
                CompleteDaimonSquareScene(client, bcTmp, bcDataTmp);
            }

            // 恶魔广场怪物波和人物得分信息
            //GameManager.ClientMgr.NotifyDaimonSquareMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, bcDataTmp.MapCode, (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUAREMONSTERWAVEANDPOINTRINFO,
            //                                            0, 0, bcDataTmp.MonsterID.Length - bcTmp.m_nMonsterWave, client.ClientData.DaimonSquarePoint, 0);

            string strcmd = string.Format("{0}:{1}", bcDataTmp.MonsterID.Length - bcTmp.m_nMonsterWave, client.ClientData.DaimonSquarePoint);
            GameManager.ClientMgr.SendToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, strcmd, (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUAREMONSTERWAVEANDPOINTRINFO);

            //string strcmd = string.Format("{0}:{1}", bcDataTmp.MonsterID.Length - bcTmp.m_nMonsterWave, client.ClientData.DaimonSquarePoint);

            //GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_QUERYDAIMONSQUAREMONSTERWAVEANDPOINTRINFO);

        }

        /// <summary>
        // 完成恶魔广场
        /// </summary>
        static public void CompleteDaimonSquareScene(GameClient client, DaimonSquareScene bsInfo, DaimonSquareDataInfo dsData)
        {
            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DaimonSquareSceneFinishFlag);

            if (nFlag != 1)
            {
                // 保存完成状态
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquareSceneFinishFlag, 1, true);

                // 剩余时间奖励
                long nTimer = bsInfo.m_lEndTime - bsInfo.m_lBeginTime;
                long nRemain = ((dsData.DurationTime * 1000) - nTimer) / 1000;

                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquareSceneTimer, (int)nRemain, true);
            }
        }

        /// <summary>
        // 给奖励
        /// </summary>
        public void GiveAwardDaimonSquareCopyScene(GameClient client, int nMultiple)
        {
            int FuBenSeqID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DaimonSquareFuBenSeqID);

            int nSceneID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DaimonSquareSceneid);
            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DaimonSquareSceneFinishFlag);
            int nTimer = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DaimonSquareSceneTimer);

            //如果已经获取过一次奖励，则不再提示奖励
            //查找角色的ID+副本顺序ID对应地图编号的奖励领取状态
            int awardState = GameManager.CopyMapMgr.FindAwardState(client.ClientData.RoleID, FuBenSeqID, nSceneID);
            if (awardState > 0)
            {
                GameManager.ClientMgr.NotifyImportantMsg(
                    Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("当前副本地图的奖励只能领取一次")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return ;
            }

            DaimonSquareDataInfo bcDataTmp = null;
            if (!Data.DaimonSquareDataInfoList.TryGetValue(nSceneID, out bcDataTmp))
                return;

            if (bcDataTmp == null)
                return;

            if (nFlag == 1)
            {
                string[] sItem = bcDataTmp.AwardItem;

                if (null != sItem && sItem.Length > 0)
                {
                    for (int i = 0; i < sItem.Length; i++)
                    {
                        if (string.IsNullOrEmpty(sItem[i].Trim()))
                            continue;

                        string[] sFields = sItem[i].Split(',');
                        if (string.IsNullOrEmpty(sFields[i].Trim()))
                            continue;

                        int nGoodsID = Convert.ToInt32(sFields[0].Trim());
                        int nGoodsNum = Convert.ToInt32(sFields[1].Trim());
                        int nBinding = 1;
                        GoodsData goodsData = new GoodsData()
                        {
                            Id = -1,
                            GoodsID = nGoodsID,
                            Using = 0,
                            Forge_level = 0,
                            Starttime = "1900-01-01 12:00:00",
                            Endtime = Global.ConstGoodsEndTime,
                            Site = 0,
                            Quality = (int)GoodsQuality.White,
                            Props = "",
                            GCount = nGoodsNum,
                            Binding = nBinding,
                            Jewellist = "",
                            BagIndex = 0,
                            AddPropIndex = 0,
                            BornIndex = 0,
                            Lucky = 0,
                            Strong = 0,
                            ExcellenceInfo = 0,
                            AppendPropLev = 0,
                            ChangeLifeLevForEquip = 0,
                        };

                        string sMsg = /**/"恶魔广场--统一奖励";

                        if (!Global.CanAddGoodsNum(client, nGoodsNum))
                        {
                            //for (int j = 0; j < nGoodsNum; ++j)
                                Global.UseMailGivePlayerAward(client, goodsData, Global.GetLang("恶魔广场"), sMsg);
                        }
                        else
                            Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, nGoodsID, nGoodsNum, goodsData.Quality, "", goodsData.Forge_level,
                                                        goodsData.Binding, 0, "", true, 1, sMsg, goodsData.Endtime);
                    }
                }
            }

            // 根据积分以及公式给奖励(经验)
            int nTimeAward = 0;
            nTimeAward = (int)(bcDataTmp.TimeModulus * nTimer);
            if (client.ClientData.DaimonSquarePoint > 0)
            {
                // 公式
                long nExp = nMultiple * Global.CalcExpForRoleScore(client.ClientData.DaimonSquarePoint, bcDataTmp.ExpModulus);
                int nMoney = client.ClientData.DaimonSquarePoint * bcDataTmp.MoneyModulus;

                if (nExp > 0)
                {
                    GameManager.ClientMgr.ProcessRoleExperience(client, nExp, false);
                    GameManager.ClientMgr.NotifyAddExpMsg(client, nExp);
                }

                if (nMoney > 0)
                {
                    GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nMoney, "恶魔广场副本", false);
                    GameManager.ClientMgr.NotifyAddJinBiMsg(client, nMoney);
                }
                //GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nMoney);

                // 存盘
                if (client.ClientData.DaimonSquarePoint > client.ClientData.DaimonSquarePointTotalPoint)
                    client.ClientData.DaimonSquarePointTotalPoint = client.ClientData.DaimonSquarePoint;

                if (client.ClientData.DaimonSquarePoint > m_nDaimonSquareMaxPoint)
                    SetDaimonSquareCopySceneTotalPoint(client.ClientData.RoleName, client.ClientData.DaimonSquarePoint);

                // 清空
                client.ClientData.DaimonSquarePoint = 0;
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquarePlayerPoint, client.ClientData.DaimonSquarePoint, true);
            }

            GameManager.CopyMapMgr.AddAwardState(client.ClientData.RoleID, FuBenSeqID, nSceneID, 1);

            return;
        }

        /// <summary>
        // 玩家离开恶魔堡垒
        /// </summary>
        public void LeaveDaimonSquareCopyScene(GameClient client, bool clearScore = false)
        {
            int nFuBenId = -1;
            nFuBenId = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DaimonSquareSceneid);

            if (client.ClientData.CopyMapID < 0 || client.ClientData.FuBenSeqID < 0 || !IsDaimonSquareCopyScene(nFuBenId))
                return;

            CopyMap cmInfo = null;
            lock (m_DaimonSquareCopyScenesList)
            {
                if (!m_DaimonSquareCopyScenesList.TryGetValue(client.ClientData.FuBenSeqID, out cmInfo) || cmInfo == null)
                    return;
            }

            Dictionary<int, DaimonSquareScene> dicTmp = null;
            lock (m_DaimonSquareCopyScenesInfo)
            {
                if (!m_DaimonSquareCopyScenesInfo.TryGetValue(client.ClientData.FuBenID, out dicTmp) || dicTmp == null)
                    return;

                DaimonSquareScene bcTmp = null;
                if (!dicTmp.TryGetValue(client.ClientData.FuBenSeqID, out bcTmp) || bcTmp == null)
                    return;

                Interlocked.Decrement(ref bcTmp.m_nPlarerCount);

                if (bcTmp.m_eStatus < DaimonSquareStatus.FIGHT_STATUS_BEGIN || (bcTmp.m_eStatus == DaimonSquareStatus.FIGHT_STATUS_BEGIN && TimeUtil.NOW() < bcTmp.m_lBeginTime + 30000))
                {
                    GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                                            cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPLAYERNUMNOTIFY, 0, 0, 0, bcTmp.m_nPlarerCount);
                }

                if (clearScore && bcTmp.m_eStatus == DaimonSquareStatus.FIGHT_STATUS_BEGIN)
                {
                    client.ClientData.DaimonSquarePoint = 0;
                }
            }

            // 离开时 保存积分

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DaimonSquarePlayerPoint, client.ClientData.DaimonSquarePoint, true);

            //client.ClientData.bIsInDaimonSquareMap = false;

            return;
        }

        /// <summary>
        /// 在恶魔广场中掉线 再上线 回到上一场景
        /// </summary>
        /// <returns></returns>
        public void LogOutWhenInDaimonSquareCopyMap(GameClient client)
        {
            LeaveDaimonSquareCopyScene(client);
        }

        /// <summary>
        // 清除
        /// </summary>
        static public void CleanDaimonSquareCopyScene(int mapid)
        {
            // 首先是动态刷出的怪
            /*for (int i = 0; i < m_DaimonSquareListScenes[mapid].m_nDynamicMonsterList.Count; i++)
            {
                Monster monsterInfo = m_DaimonSquareListScenes[mapid].m_nDynamicMonsterList[i];
                if (monsterInfo != null)
                {
                    GameManager.MonsterMgr.AddDelayDeadMonster(monsterInfo);
                }
            }*/

            return;
        }

        // add by chenjingui. 20150704 角色改名后，检测是否更新最高积分者
        // note: 这地方肯定会有线程安全问题啊，根据以前代码，玩家提交副本时，检测积分，如果大于当前副本的积分，就更新最高积分和名字
        // 但是每个玩家的socket消息处理是多线程的啊，也没有加锁。
        public void OnChangeName(int roleId, string oldName, string newName)
        {
            if (!string.IsNullOrEmpty(oldName) && !string.IsNullOrEmpty(newName))
            {
                if (!string.IsNullOrEmpty(m_nDaimonSquareMaxName) && m_nDaimonSquareMaxName == oldName)
                {
                    m_nDaimonSquareMaxName = newName;
                }
            }
        }

    }
}
