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
    // 血色城堡副本管理类 [7/3/2014 LiaoWei]    说明 - 给客户端显示的刷怪状态消息里 1击杀吊桥怪物 2击杀吊桥 3击杀巫师 4击碎灵棺 5天使武器
    public class BloodCastleCopySceneManager
    {
        /// <summary>
        /// 血色城堡副本场景list
        /// </summary>
        public Dictionary<int, CopyMap> m_BloodCastleCopyScenesList = new Dictionary<int, CopyMap>();    // KEY-副本顺序ID VALUE-副本信息

        /// <summary>
        /// 血色城堡副本场景数据
        /// </summary>
        public Dictionary<int, Dictionary<int, BloodCastleScene>> m_BloodCastleCopyScenesInfo = new Dictionary<int, Dictionary<int, BloodCastleScene>>(); // KEY-副本ID VALUE- KEY-副本顺序ID VALUE-BloodCastleScene信息

        /// <summary>
        /// 线程锁对象 -- 血色城堡场景
        /// </summary>
        public static object m_Mutex = new object();

        /// <summary>
        /// 最高积分 -- 分数
        /// </summary>
        public int m_nTotalPointValue = -1;

        /// <summary>
        /// 最高积分 -- 人名
        /// /// </summary>
        public string m_sTotalPointName = "";

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long LastHeartBeatTicks = 0L;

        /// <summary>
        /// 加载血色城堡场景到管理器
        /// </summary>
        public void InitBloodCastleCopyScene()
        {
            // 向DB请求最高积分信息
            Global.QueryDayActivityTotalPointInfoToDB(SpecialActivityTypes.BloodCastle);

            //LoadBloodCastleListScenes();
        }

        /// <summary>
        /// 加载血色城堡场景到管理器
        /// </summary>
        public void LoadBloodCastleListScenes()
        {
            /*
            // 向DB请求最高积分信息
            Global.QueryDayActivityTotalPointInfoToDB(SpecialActivityTypes.BloodCastle);

            m_nPushMsgDayID = Global.SafeConvertToInt32(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.BloodCastlePushMsgDayID));*/

        }

        /// <summary>
        /// 设置最高积分信息
        /// </summary>
        public void SetBloodCastleCopySceneTotalPoint(string sName, int nPoint)
        {
            m_sTotalPointName = sName;
            m_nTotalPointValue = nPoint;
        }

        public bool CanEnterExistCopyScene(GameClient client)
        {
            CopyMap copyMap = null;
            int fuBenSeqId = client.ClientData.FuBenSeqID;
            lock (m_BloodCastleCopyScenesList)
            {
                if (!m_BloodCastleCopyScenesList.TryGetValue(fuBenSeqId, out copyMap))
                {
                    return false;
                }
            }

            lock (m_BloodCastleCopyScenesInfo)
            {
                Dictionary<int, BloodCastleScene> dicTmp = null;
                BloodCastleScene bcData = null;

                int nFubenID = copyMap.FubenMapID;
                if (!m_BloodCastleCopyScenesInfo.TryGetValue(nFubenID, out dicTmp))
                {
                    return false;
                }

                if (!dicTmp.TryGetValue(fuBenSeqId, out bcData))
                {
                    return false;
                }

                if (bcData.m_eStatus != BloodCastleStatus.FIGHT_STATUS_BEGIN)
                {
                    return false;
                }

                return bcData.CantiansRole(client);
            }
        }
 
        /// <summary>
        /// 添加一个场景
        /// </summary>
        public void AddBloodCastleCopyScenes(int nSequenceID, int nFubenID, int nMapCodeID, CopyMap mapInfo)
        {
            lock (m_BloodCastleCopyScenesList)
            {
                CopyMap cmInfo = null;
                if (!m_BloodCastleCopyScenesList.TryGetValue(nSequenceID, out cmInfo) || cmInfo == null)
                {
                    m_BloodCastleCopyScenesList.Add(nSequenceID, mapInfo);
                }                
            }

            lock (m_BloodCastleCopyScenesInfo)
            {
                Dictionary<int, BloodCastleScene> dicTmp = null;
                BloodCastleScene bcData = null;
                
                if (!m_BloodCastleCopyScenesInfo.TryGetValue(nFubenID, out dicTmp))
                {
                    dicTmp = new Dictionary<int, BloodCastleScene>();
                    m_BloodCastleCopyScenesInfo.Add(nFubenID, dicTmp);
                }

                if (!dicTmp.TryGetValue(nSequenceID, out bcData))
                {
                    bcData = new BloodCastleScene();
                    dicTmp.Add(nSequenceID, bcData);
                    bcData.CleanAllInfo();
                }

                bcData.m_nMapCode = nMapCodeID;
                bcData.m_CopyMap = mapInfo;
            }
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public void RemoveBloodCastleListCopyScenes(CopyMap cmInfo, int nSqeID, int nCopyID)
        {
            lock (m_BloodCastleCopyScenesList)
            {
                CopyMap cmTmp = null;
                if (m_BloodCastleCopyScenesList.TryGetValue(nSqeID, out cmTmp) && cmTmp != null)
                {
                    m_BloodCastleCopyScenesList.Remove(nSqeID);
                }
            }

            lock (m_BloodCastleCopyScenesInfo)
            {
                Dictionary<int, BloodCastleScene> dicTmp = null;
                if (m_BloodCastleCopyScenesInfo.TryGetValue(nCopyID, out dicTmp) && dicTmp != null)
                {
                    BloodCastleScene bcTmp = null;
                    if (dicTmp.TryGetValue(nSqeID, out bcTmp) && bcTmp != null)
                        dicTmp.Remove(nSqeID);

                    if (dicTmp.Count <= 0)
                        m_BloodCastleCopyScenesInfo.Remove(nCopyID);
                }
            }
        }

        /// <summary>
        /// 检测场景管理器
        /// </summary>
        public int CheckBloodCastleListScenes(int nFuBenMapID)
        {
            lock (m_BloodCastleCopyScenesInfo)
            {
                Dictionary<int, BloodCastleScene> tmpData = null;

                if (!m_BloodCastleCopyScenesInfo.TryGetValue(nFuBenMapID, out tmpData))
                    return -1;

                if (tmpData == null)
                    return -1;

                BloodCastleDataInfo bcDataTmp = null;

                if (!Data.BloodCastleDataInfoList.TryGetValue(nFuBenMapID, out bcDataTmp))
                    return -1;

                if (bcDataTmp == null)
                    return -1;

                foreach (var bcData in tmpData)
                {
                    int nID = -1;
                    nID = bcData.Key;

                    BloodCastleScene tmpbcinfo = null;
                    tmpbcinfo = bcData.Value;

                    if (nID < 0 || tmpbcinfo == null)
                        continue;

                    if (nID == nFuBenMapID && tmpbcinfo.m_nPlarerCount < bcDataTmp.MaxEnterNum && tmpbcinfo.m_eStatus < BloodCastleStatus.FIGHT_STATUS_BEGIN)
                        return nID;
                }

            }

            return -1;
        }

        /// <summary>
        /// 检测是否是血色城堡副本
        /// </summary>
        public bool IsBloodCastleCopyScene(int nFuBenMapID)
        {
            if (Data.BloodCastleDataInfoList.ContainsKey(nFuBenMapID))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检测是否是血色城堡副本
        /// </summary>
        public bool IsBloodCastleCopyScene2(int nMpaCodeID)
        {
            SceneUIClasses sceneType = Global.GetMapSceneType(nMpaCodeID);
            if (sceneType == SceneUIClasses.BloodCastle)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 取得血色城堡副本信息
        /// </summary>
        public CopyMap GetBloodCastleCopySceneInfo(int nSequenceID)
        {
            if (nSequenceID < 0)
                return null;

            CopyMap copymapInfo = null;
            if (!m_BloodCastleCopyScenesList.TryGetValue(nSequenceID, out copymapInfo))
                return null;

            return copymapInfo;
        }

        /// <summary>
        /// 取得血色城堡信息
        /// </summary>
        public BloodCastleScene GetBloodCastleCopySceneDataInfo(CopyMap cmInfo, int nSequenceID, int nFuBenID)
        {
            if (cmInfo == null || nSequenceID < 0)
                return null;

            Dictionary<int, BloodCastleScene> dicTmp = null;
            if (!m_BloodCastleCopyScenesInfo.TryGetValue(nFuBenID, out dicTmp) || dicTmp == null)
                return null;

            BloodCastleScene bcInfo = null;
            if (!dicTmp.TryGetValue(nSequenceID, out bcInfo) || bcInfo == null)
                return null;

            return bcInfo;
        }
 
//         /// <summary>
//         /// 取得基本信息
//         /// </summary>
//         static public BloodCastleScene GetBloodCastleListScenes(int nMap)
//         {
//             return m_BloodCastleListScenes[nMap];
//         }
// 
//         /// <summary>
//         /// 管理动态刷怪列表--增加
//         /// </summary>
//         public void AddBloodCastleDynamicMonster(int nMap, Monster monster)
//         {
//             m_BloodCastleListScenes[nMap].m_nDynamicMonsterList.Add(monster);
//             return ;
//         }
// 
//         /// <summary>
//         /// 管理动态刷怪列表--移除
//         /// </summary>
//         public void RemoveBloodCastleDynamicMonster(int nMap, Monster monster)
//         {
//             m_BloodCastleListScenes[nMap].m_nDynamicMonsterList.Remove(monster);
//             return;
//         }

        /// <summary>
        /// 玩家进入血色城堡副本计数
        /// </summary>
        public int EnterBloodCastSceneCopySceneCount(GameClient client, int nFubenID, out int nBloodNum)
        {
            nBloodNum = -1;

            BloodCastleDataInfo bcDataTmp = null;
            if (!Data.BloodCastleDataInfoList.TryGetValue(nFubenID, out bcDataTmp))
                return -1;
            
            int nDate = TimeUtil.NowDateTime().DayOfYear;               // 当前时间
            int nType = (int)SpecialActivityTypes.BloodCastle;// 血色堡垒

            int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, nType);
            nBloodNum = nCount;

            if (nCount >= bcDataTmp.MaxEnterNum)
            {
                bool nRet = true;

                // VIP检测
                int dayID = TimeUtil.NowDateTime().DayOfYear;
                int nVipLev = client.ClientData.VipLevel;

                int nNum = 0;
                int[] nArry = null;
                nArry = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPEnterBloodCastleCountAddValue");

                if (nVipLev > 0 && nArry != null && nArry[nVipLev] > 0)
                {
                    nNum = nArry[nVipLev];

                    if (nCount < bcDataTmp.MaxEnterNum + nNum)
                    {
                        Global.UpdateVipDailyData(client, dayID, (int)VIPTYPEEum.VIPTYPEEUM_ENTERBLOODCASTLE);
                        nRet = false;
                    }
                }

                if (nRet == true)
                {
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                       StringUtil.substitute(Global.GetLang("您今天进入血色堡垒已达上限 请明天再试")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                    
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
            lock(m_BloodCastleCopyScenesList)
            {   
                if (!m_BloodCastleCopyScenesList.TryGetValue(nSquID, out cmInfo) || cmInfo == null)
                    return;
            }

            long ticks = TimeUtil.NOW();    // 当前tick
            lock (m_BloodCastleCopyScenesInfo)
            {
                Dictionary<int, BloodCastleScene> dicTmp = null;
                if (m_BloodCastleCopyScenesInfo.TryGetValue(nFubenID, out dicTmp) && dicTmp != null)
                {
                    BloodCastleScene bcTmp = null;
                    if (dicTmp.TryGetValue(nSquID, out bcTmp) && bcTmp != null)
                    {
                        BloodCastleDataInfo bcDataTmp = null;
                        if (!Data.BloodCastleDataInfoList.TryGetValue(nFubenID, out bcDataTmp) || bcDataTmp == null)
                            return;

                        if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPLAYERNUMNOTIFY)
                        {
                            if (bcTmp.m_eStatus <= BloodCastleStatus.FIGHT_STATUS_PREPARE)
                            {
                                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                                            cmInfo, nCmdID, 0, 0, 0, bcTmp.m_nPlarerCount);
                            }
                        }
                        else if (nCmdID == (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPREPAREFIGHT)
                        {
                            if (bcTmp.m_lPrepareTime <= 0)
                            {
                                //SysConOut.WriteLine("血色堡垒{0}里 时间没得到返回了...！！！", nSquID);
                                return;
                            }

                            int nTimer = (int)((bcDataTmp.PrepareTime * 1000 - (ticks - bcTmp.m_lPrepareTime)) / 1000);

                            //string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", nFubenID, nTimer, bcDataTmp.NeedKillMonster1Num, 1, bcDataTmp.NeedKillMonster2Num, 1, 1, 1);
                            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", nFubenID, nTimer, bcDataTmp.NeedKillMonster1Num, 1, bcDataTmp.NeedKillMonster2Num, 1, 1, 1);

                            //SysConOut.WriteLine("血色堡垒{0}里 时间{1}！！！", nSquID, nTimer);

                            GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPREPAREFIGHT);

                            GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                                        cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPLAYERNUMNOTIFY, 0, 0, 0, bcTmp.m_nPlarerCount, client);
                            if (bcTmp.m_eStatus == BloodCastleStatus.FIGHT_STATUS_BEGIN)
                            {
                                nTimer = (int)((bcDataTmp.DurationTime * 1000 - (ticks - bcTmp.m_lBeginTime)) / 1000);
                                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                                                cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEBEGINFIGHT, nTimer, 0, 0, bcTmp.m_nPlarerCount, client); // 战斗结束倒计时
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 玩家进入血色城堡副本
        /// </summary>
        public int EnterBloodCastSceneCopyScene(GameClient client, int nFubenID, int nBloodNum, out int nSeqID, int mapCode)
        {
            string strcmd = "";

            nSeqID = -1;

            // 没有领取积分奖励
            if (client.ClientData.BloodCastleAwardPoint > 0)
            {
                int FuBenSeqID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BloodCastleFuBenSeqID);
                int nSceneID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BloodCastleSceneid);
                int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BloodCastleSceneFinishFlag);

                //如果已经获取过一次奖励，则不再提示奖励
                //查找角色的ID+副本顺序ID对应地图编号的奖励领取状态
                int awardState = GameManager.CopyMapMgr.FindAwardState(client.ClientData.RoleID, FuBenSeqID, nSceneID);
                if (awardState == 0)
                {
                    BloodCastleDataInfo bcDataTmp1 = null;
                    if (Data.BloodCastleDataInfoList.TryGetValue(nSceneID, out bcDataTmp1))
                    {
                        if (bcDataTmp1 == null)
                        {
                            client.ClientData.BloodCastleAwardPoint = 0;
                            return 1;
                        }

                        string AwardItem1 = null;
                        string AwardItem2 = null;

                        for (int n = 0; n < bcDataTmp1.AwardItem2.Length; ++n)
                        {
                            AwardItem2 += bcDataTmp1.AwardItem2[n];
                            if (n != bcDataTmp1.AwardItem2.Length - 1)
                                AwardItem2 += "|";
                        }

                        // 1.离场倒计时开始 2.是否成功完成 3.玩家的积分 4.玩家经验奖励 5.玩家的金钱奖励 6.玩家物品奖励1(只有提交大天使武器的玩家才有 其他人为null) 7.玩家物品奖励2(通用奖励 大家都有的)
                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", -1, nFlag, client.ClientData.BloodCastleAwardPoint,
                                                Global.CalcExpForRoleScore(client.ClientData.BloodCastleAwardPoint, bcDataTmp1.ExpModulus),
                                                client.ClientData.BloodCastleAwardPoint * bcDataTmp1.MoneyModulus, AwardItem1, AwardItem2);

                        GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEENDFIGHT);

                        return -1;
                    }
                }
                else
                {
                    // 清空
                    client.ClientData.BloodCastleAwardPoint = 0;
                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastlePlayerPoint, client.ClientData.BloodCastleAwardPoint, true);
                }
            }

            int nFubenMapID = Global.GetBloodCastleCopySceneIDForRole(client);

            if (nFubenMapID <= 0)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                            StringUtil.substitute(Global.GetLang("对不起 您没有达到进入血色城堡的要求等级！！")), 
                                                            GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);
                
                LogManager.WriteLog(LogTypes.Error, string.Format("enter bloodcastle scene fail!! get scene info fail!!!!"));
                
                return -1;
            }

            BloodCastleDataInfo bcInfo = null;
            if (!Data.BloodCastleDataInfoList.TryGetValue(nFubenMapID, out bcInfo) || bcInfo == null)
                return -1;

            // 时限段判断
            if (!Global.CanEnterBloodCastleOnTime(bcInfo.BeginTime, bcInfo.PrepareTime))
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                            StringUtil.substitute(Global.GetLang("当前时间段血色堡垒并未开启，请稍后再试")), GameInfoTypeIndexes.Error, 
                                                                ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                return -1;
            }

            // 需要物品判断
            GoodsData goodData = Global.GetGoodsByID(client, bcInfo.NeedGoodsID);
            if (goodData == null)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                    StringUtil.substitute(Global.GetLang("对不起 您没有进入血色城堡的道具！！")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                
                return -1;
            }

            if (goodData.GCount < bcInfo.NeedGoodsNum)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                       StringUtil.substitute(Global.GetLang("对不起 您拥有的进入血色城堡的道具数量不够！！")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);

                return -1;
            }

            bool usedBinding = false;
            bool usedTimeLimited = false;

            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, bcInfo.NeedGoodsID, 1, false, out usedBinding, out usedTimeLimited))
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                       StringUtil.substitute(Global.GetLang("对不起 扣除进入血色城堡的道具失败 进入失败！！")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                
                return -1;
            }

            Dictionary<int, BloodCastleScene> dicTmp = null;

            lock (m_BloodCastleCopyScenesInfo)
            {
                if (m_BloodCastleCopyScenesInfo.TryGetValue(nFubenMapID, out dicTmp) && dicTmp != null)
                {
                    foreach (var bcsceneInfo in dicTmp)
                    {
                        if (bcsceneInfo.Value.m_eStatus >= BloodCastleStatus.FIGHT_STATUS_BEGIN)
                        {
                            continue;
                        }
                        if (bcsceneInfo.Value.m_nPlarerCount >= bcInfo.MaxPlayerNum)
                            continue;

                        ++bcsceneInfo.Value.m_nPlarerCount;
                        nSeqID = bcsceneInfo.Key;
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
                            BloodCastleScene bcData = null;
                            if (!m_BloodCastleCopyScenesInfo.TryGetValue(nFubenID, out dicTmp) || dicTmp == null)
                            {
                                dicTmp = new Dictionary<int, BloodCastleScene>();
                                m_BloodCastleCopyScenesInfo.Add(nFubenID, dicTmp);
                            }

                            if (!dicTmp.TryGetValue(nSeqID, out bcData) || bcData == null)
                            {
                                bcData = new BloodCastleScene();
                                bcData.CleanAllInfo();
                                bcData.m_nMapCode = mapCode;
                                bcData.m_nPlarerCount = 1;

                                dicTmp[nSeqID] = bcData;
                            }
                        }
                    }
                }
            }

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastleFuBenSeqID, nSeqID, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastleSceneid, nFubenMapID, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastleSceneFinishFlag, 0, true);

            return 0;
        }

        /// <summary>
        // 心跳处理
        /// </summary>
        public void HeartBeatBloodCastScene()
        {

            long nowTicks = TimeUtil.NOW();
            if (Math.Abs(nowTicks - LastHeartBeatTicks) < (1000))
                return;

            LastHeartBeatTicks = nowTicks;

            HashSet<int> mapCodeHashSet = new HashSet<int>();
            lock (m_BloodCastleCopyScenesList)
            {
                CopyMap copyMap = null;
                foreach (var BloodCastleCopySceneList in m_BloodCastleCopyScenesList)
                {
                    int nID = -1;
                    nID = BloodCastleCopySceneList.Value.FuBenSeqID;

                    int nCopyID = -1;
                    nCopyID = BloodCastleCopySceneList.Value.FubenMapID;

                    int nMapCodeID = -1;
                    nMapCodeID = BloodCastleCopySceneList.Value.MapCode;

                    if (nID < 0 || nCopyID < 0 || nMapCodeID < 0)
                        continue;

                    copyMap = BloodCastleCopySceneList.Value;

                    lock (m_BloodCastleCopyScenesInfo)
                    {
                        BloodCastleDataInfo bcDataTmp = null;
                        if (!Data.BloodCastleDataInfoList.TryGetValue(nCopyID, out bcDataTmp) || bcDataTmp == null)
                            continue;

                        Dictionary<int, BloodCastleScene> dicTmp = null;
                        if (!m_BloodCastleCopyScenesInfo.TryGetValue(nCopyID, out dicTmp) || dicTmp == null)
                            continue;

                        BloodCastleScene bcTmp = null;
                        if (!dicTmp.TryGetValue(nID, out bcTmp) || bcTmp == null)
                            continue;
                        
                        // 区分时段 注意 每个时段都要计时

                        // 当前tick
                        long ticks = TimeUtil.NOW();

                        if (bcTmp.m_eStatus == BloodCastleStatus.FIGHT_STATUS_NULL)             // 如果处于空状态 -- 是否要切换到准备状态
                        {
                            int nSecond = 0;
                            string strTimer = null;

                            if (Global.CanEnterBloodCastleCopySceneOnTime(bcDataTmp.BeginTime, bcDataTmp.PrepareTime + bcDataTmp.DurationTime, out nSecond, out strTimer))
                            {
                                bcTmp.m_eStatus = BloodCastleStatus.FIGHT_STATUS_PREPARE;

                                //SysConOut.WriteLine("剩余时间为{0}...！！！", nSecond);

                                DateTime staticTime = DateTime.Parse(strTimer);

                                bcTmp.m_lPrepareTime = staticTime.Ticks / 10000;//TimeUtil.NOW();

                                List<GameClient> objsList = BloodCastleCopySceneList.Value.GetClientsList(); //发送给所有地图的用户
                                if (null == objsList)
                                    return;

                                for (int i = 0; i < objsList.Count; i++)
                                    SendMegToClient(objsList[i], nCopyID, nID, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPREPAREFIGHT);

                                //触发血色城堡事件
                                //GlobalEventSource.getInstance().fireEvent(XueSeChengBaoBaseEventObject.CreateStatusEvent((int)bcTmp.m_eStatus));
                            }
                        }
                        else if (bcTmp.m_eStatus == BloodCastleStatus.FIGHT_STATUS_PREPARE)     // 场景战斗状态切换
                        {
                            if (ticks >= (bcTmp.m_lPrepareTime + (bcDataTmp.PrepareTime * 1000)))
                            {
                                bcTmp.m_Step++;
                                GameManager.CopyMapMgr.AddGuangMuEvent(bcTmp.m_CopyMap, 1, 0);

                                // 准备战斗 通知客户端 面前的阻挡消失 玩家可以上桥上去杀怪了
                                bcTmp.m_eStatus = BloodCastleStatus.FIGHT_STATUS_BEGIN;

                                bcTmp.m_lBeginTime = TimeUtil.NOW();
                                int nTimer = (int)((bcDataTmp.DurationTime * 1000 - (ticks - bcTmp.m_lBeginTime)) / 1000);

                                List<GameClient> clientList = bcTmp.m_CopyMap.GetClientsList();
                                if (null != clientList)
                                {
                                    foreach (var c in clientList)
                                    {
                                        if (bcTmp.AddRole(c))
                                        {
                                            Global.UpdateRoleEnterActivityCount(c, SpecialActivityTypes.BloodCastle);
                                        }
                                    }
                                }

                                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                                                BloodCastleCopySceneList.Value, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEBEGINFIGHT, nTimer); // 战斗结束倒计时

                                // 把城门刷出来
                                int monsterID = bcDataTmp.GateID;
                                string[] sfields = bcDataTmp.GatePos.Split(',');

                                int nPosX = Global.SafeConvertToInt32(sfields[0]);
                                int nPosY = Global.SafeConvertToInt32(sfields[1]);

                                GameMap gameMap = null;
                                if (!GameManager.MapMgr.DictMaps.TryGetValue(bcTmp.m_nMapCode, out gameMap))
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("血色城堡报错 地图配置 ID = {0}", bcDataTmp.MapCode));
                                    return;
                                }
                                int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                                int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                                //SysConOut.WriteLine("liaowei是帅哥  血色堡垒{0}里 刷城门怪了--1！！！", BloodCastleScenes.Key);
                                GameManager.MonsterZoneMgr.AddDynamicMonsters(nMapCodeID, monsterID, BloodCastleCopySceneList.Value.CopyMapID, 1, gridX, gridY, 0);

                                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                                            BloodCastleCopySceneList.Value, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 1);   // 杀死桥上的怪 0/50 -- 显示杀怪状态

                                //触发血色城堡事件
                                //GlobalEventSource.getInstance().fireEvent(XueSeChengBaoBaseEventObject.CreateStatusEvent((int)bcTmp.m_eStatus));
                            }
                        }
                        else if (bcTmp.m_eStatus == BloodCastleStatus.FIGHT_STATUS_BEGIN)       // 战斗开始
                        {
                            if (ticks >= (bcTmp.m_lBeginTime + (bcDataTmp.DurationTime * 1000)))
                            {
                                bcTmp.m_eStatus = BloodCastleStatus.FIGHT_STATUS_END;
                                bcTmp.m_lEndTime = TimeUtil.NOW();

                                try
                                {
                                    /**/string log = string.Format("血色城堡已结束,是否完成{0},结束时间{1},m_bIsFinishTask:{2},m_nKillMonsterACount:{3},m_bKillMonsterAStatus:{4},m_nRoleID:{5}",
                                        bcTmp.m_bIsFinishTask, new DateTime(bcTmp.m_lEndTime * 10000), bcTmp.m_bIsFinishTask,
                                        bcTmp.m_nKillMonsterACount, bcTmp.m_bKillMonsterAStatus, bcTmp.m_nRoleID);

                                    LogManager.WriteLog(LogTypes.Error, log);
                                }catch{ }

                                //触发血色城堡事件
                                //GlobalEventSource.getInstance().fireEvent(XueSeChengBaoBaseEventObject.CreateStatusEvent((int)bcTmp.m_eStatus));
                            }

                            mapCodeHashSet.Add(copyMap.MapCode);
                        }
                        else if (bcTmp.m_eStatus == BloodCastleStatus.FIGHT_STATUS_END)         // 战斗结束
                        {
                            // 血色堡垒结束战斗  客户端显示倒计时界面
                            int nTimer = (int)((bcDataTmp.LeaveTime * 1000 - (ticks - bcTmp.m_lEndTime)) / 1000);

                            if (bcTmp.m_bEndFlag == false)
                            {
                                // 剩余时间奖励
                                long nTimerInfo = 0;
                                nTimerInfo = bcTmp.m_lEndTime - bcTmp.m_lBeginTime;
                                long nRemain = 0;
                                nRemain = ((bcDataTmp.DurationTime * 1000) - nTimerInfo) / 1000;

                                if (nRemain >= bcDataTmp.DurationTime)
                                    nRemain = bcDataTmp.DurationTime / 2;

                                int nTimeAward = 0;
                                nTimeAward = (int)(bcDataTmp.TimeModulus * nRemain);

                                if (nTimeAward < 0)
                                    nTimeAward = 0;

                                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsgEndFight(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                                                    BloodCastleCopySceneList.Value, bcTmp, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEENDFIGHT, nTimer, nTimeAward);
                            }
                        }
                    }
                }

                foreach (var mapCode in mapCodeHashSet)
                {
                    // 加载自动复活的怪物
                    GameManager.MonsterZoneMgr.ReloadCopyMapMonsters(mapCode, -1);
                }
            }

            return;
        }

        /// <summary>
        /// 刷怪
        /// </summary>
        public void CreateMonsterBBloodCastScene(int mapid, BloodCastleDataInfo bcDataTmp, BloodCastleScene bcTmp, int nCopyMapID)
        {
            int monsterID = bcDataTmp.NeedKillMonster2ID;
            string[] sfields = bcDataTmp.NeedCreateMonster2Pos.Split(',');

            int nPosX = Global.SafeConvertToInt32(sfields[0]);
            int nPosY = Global.SafeConvertToInt32(sfields[1]);

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(bcTmp.m_nMapCode, out gameMap))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("血色城堡报错 地图配置 ID = {0}", bcDataTmp.MapCode));
                return;
            }

            int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
            int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

            int gridNum = gameMap.CorrectPointToGrid(bcDataTmp.NeedCreateMonster2Radius);

            for (int i = 0; i < bcDataTmp.NeedCreateMonster2Num; ++i)
                GameManager.MonsterZoneMgr.AddDynamicMonsters(mapid, monsterID, nCopyMapID, 1, gridX, gridY, gridNum, bcDataTmp.NeedCreateMonster2PursuitRadius);
            
            return;
        }

        public void OnStartPlayGame(GameClient client)
        {
            if (client.ClientData.FuBenSeqID < 0 || client.ClientData.CopyMapID < 0 || !IsBloodCastleCopyScene(client.ClientData.FuBenID))
                return;

            BloodCastleDataInfo bcDataTmp = null;
            if (!Data.BloodCastleDataInfoList.TryGetValue(client.ClientData.FuBenID, out bcDataTmp) || bcDataTmp == null)
                return;

            Dictionary<int, BloodCastleScene> dicTmp = null;
            if (!m_BloodCastleCopyScenesInfo.TryGetValue(client.ClientData.FuBenID, out dicTmp) || dicTmp == null)
                return;

            BloodCastleScene bcTmp = null;
            if (!dicTmp.TryGetValue(client.ClientData.FuBenSeqID, out bcTmp) || bcTmp == null)
                return;

            CopyMap cmInfo = null;
            if (!m_BloodCastleCopyScenesList.TryGetValue(client.ClientData.FuBenSeqID, out cmInfo) || cmInfo == null)
                return;

            if (bcTmp.m_bEndFlag == true)
                return;

            SendMegToClient(client, client.ClientData.FuBenID, client.ClientData.FuBenSeqID, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPREPAREFIGHT);
            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.BloodCastleAwardPoint);
            GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLECOMBATPOINT);
            if (bcTmp.m_Step == 0)
            {
            }
            else if (bcTmp.m_Step == 1)
            {
                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, bcTmp.m_nKillMonsterACount, 1);    // 杀死桥上的怪 数量/50 显示杀怪状态
            }
            else if (bcTmp.m_Step == 2)
            {
                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 2);   // 杀死城门 1/1 -- 显示杀怪状态
            }
            else if (bcTmp.m_Step == 3)
            {
                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, bcTmp.m_nKillMonsterBCount, 3);   // 杀死巫师怪 数量/8 -- 显示杀怪状态
            }
            else if (bcTmp.m_Step == 4)
            {
                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 4);   // 杀死水晶棺 1/1 -- 显示杀怪状态
            }
            else if (!bcTmp.m_bIsFinishTask)
            {
                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 5);   // 击杀Boss 1/1 -- 显示杀怪状态
            }
            else
            {
                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 1, 5);   // 击杀Boss 1/1 -- 显示杀怪状态
            }
        }

        /// <summary>
        // 杀死了怪
        /// </summary>
        public void KillMonsterABloodCastCopyScene(GameClient client, Monster monster)
        {
            if (client.ClientData.FuBenSeqID < 0 || client.ClientData.CopyMapID < 0 || !IsBloodCastleCopyScene(client.ClientData.FuBenID))
                return;

            BloodCastleDataInfo bcDataTmp = null;
            if (!Data.BloodCastleDataInfoList.TryGetValue(client.ClientData.FuBenID, out bcDataTmp) || bcDataTmp == null)
                return;

            Dictionary<int, BloodCastleScene> dicTmp = null;
            if (!m_BloodCastleCopyScenesInfo.TryGetValue(client.ClientData.FuBenID, out dicTmp) || dicTmp == null)
                return;

            BloodCastleScene bcTmp = null;
            if (!dicTmp.TryGetValue(client.ClientData.FuBenSeqID, out bcTmp) || bcTmp == null)
                return;

            CopyMap cmInfo = null;
            if (!m_BloodCastleCopyScenesList.TryGetValue(client.ClientData.FuBenSeqID, out cmInfo) || cmInfo == null)
                return;

            if (bcTmp.m_bEndFlag == true || bcTmp.m_eStatus != BloodCastleStatus.FIGHT_STATUS_BEGIN)
                return;
#if ___CC___FUCK___YOU___BB___

#else
             if (bcTmp.m_eStatus == BloodCastleStatus.FIGHT_STATUS_BEGIN)
            {
                client.ClientData.BloodCastleAwardPoint += monster.MonsterInfo.BloodCastJiFen;
            }
#endif

            client.ClientData.BloodCastleAwardPointTmp = client.ClientData.BloodCastleAwardPoint;
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastlePlayerPoint, client.ClientData.BloodCastleAwardPoint, false);

            string strcmd = string.Format("{0}:{1}", client.ClientData.RoleID, client.ClientData.BloodCastleAwardPoint);

            GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLECOMBATPOINT);


#if ___CC___FUCK___YOU___BB___
            if (monster.XMonsterInfo.Level >= bcDataTmp.NeedKillMonster1Level && bcTmp.m_bKillMonsterAStatus == 0)
            {
                int killedMonster = Interlocked.Increment(ref bcTmp.m_nKillMonsterACount);

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, bcTmp.m_nKillMonsterACount, 1);    // 杀死桥上的怪 数量/50 显示杀怪状态

                if (killedMonster == bcDataTmp.NeedKillMonster1Num)
                {
                    bcTmp.m_Step++;
                    GameManager.CopyMapMgr.AddGuangMuEvent(cmInfo, 2, 0);
                    GameManager.CopyMapMgr.AddGuangMuEvent(cmInfo, 22, 2);//吊桥放下

                    // 杀死A怪的数量已经达到限额 通知客户端 面前的阻挡消失 玩家可以离开桥 攻击城门了
                    GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERAHASDONE);

                    GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 2);   // 杀死吊桥怪 0/1 -- 显示杀怪状态

                    bcTmp.m_bKillMonsterAStatus = 1;
                }
            }
            if (monster.XMonsterInfo.MonsterId == bcDataTmp.GateID)
            {
                bcTmp.m_Step++;
                GameManager.CopyMapMgr.AddGuangMuEvent(cmInfo, 3, 0);

                CreateMonsterBBloodCastScene(bcTmp.m_nMapCode, bcDataTmp, bcTmp, client.ClientData.CopyMapID);       // 把B怪刷出来吧

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 1, 2);   // 杀死吊桥怪 1/1 -- 显示杀怪状态


                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 3);   // 杀死巫师怪 0/8 -- 显示杀怪状态

            }

            if (monster.XMonsterInfo.MonsterId == bcDataTmp.NeedKillMonster2ID && bcTmp.m_bKillMonsterBStatus == 0)
            {
                int killedMonster = Interlocked.Increment(ref bcTmp.m_nKillMonsterBCount);

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, bcTmp.m_nKillMonsterBCount, 3);   // 杀死巫师怪 数量/8 -- 显示杀怪状态

                if (killedMonster == bcDataTmp.NeedKillMonster2Num)
                {
                    bcTmp.m_Step++;

                    // 把水晶棺刷出来
                    int monsterID = bcDataTmp.CrystalID;
                    string[] sfields = bcDataTmp.CrystalPos.Split(',');

                    int nPosX = Global.SafeConvertToInt32(sfields[0]);
                    int nPosY = Global.SafeConvertToInt32(sfields[1]);


                    GameMap gameMap = null;
                    if (!GameManager.MapMgr.DictMaps.TryGetValue(bcTmp.m_nMapCode, out gameMap))
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("血色城堡报错 地图配置 ID = {0}", bcDataTmp.MapCode));
                        return;
                    }
                    int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                    int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                    GameManager.MonsterZoneMgr.AddDynamicMonsters(bcTmp.m_nMapCode, monsterID, cmInfo.CopyMapID, 1, gridX, gridY, 0);

                    GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 4);   // 杀死水晶棺 0/1 -- 显示杀怪状态

                    bcTmp.m_bKillMonsterBStatus = 1;
                }
            }

            if (monster.XMonsterInfo.MonsterId == bcDataTmp.CrystalID)
            {
                bcTmp.m_Step++;

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 1, 4);   // 杀死水晶棺 1/1 -- 显示杀怪状态


                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 5);   // 采集雕像 0/1 -- 显示杀怪状态

            }
#else
            if (monster.MonsterInfo.VLevel >= bcDataTmp.NeedKillMonster1Level && bcTmp.m_bKillMonsterAStatus == 0)
            {
                int killedMonster = Interlocked.Increment(ref bcTmp.m_nKillMonsterACount);

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, bcTmp.m_nKillMonsterACount, 1);    // 杀死桥上的怪 数量/50 显示杀怪状态

                if (killedMonster == bcDataTmp.NeedKillMonster1Num)
                {
                    bcTmp.m_Step++;
                    GameManager.CopyMapMgr.AddGuangMuEvent(cmInfo, 2, 0);
                    GameManager.CopyMapMgr.AddGuangMuEvent(cmInfo, 22, 2);//吊桥放下

                    // 杀死A怪的数量已经达到限额 通知客户端 面前的阻挡消失 玩家可以离开桥 攻击城门了
                    GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERAHASDONE);

                    GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 2);   // 杀死吊桥怪 0/1 -- 显示杀怪状态

                    bcTmp.m_bKillMonsterAStatus = 1;
                }
            }
             if (monster.MonsterInfo.ExtensionID == bcDataTmp.GateID)
            {
                bcTmp.m_Step++;
                GameManager.CopyMapMgr.AddGuangMuEvent(cmInfo, 3, 0);

                CreateMonsterBBloodCastScene(bcTmp.m_nMapCode, bcDataTmp, bcTmp, client.ClientData.CopyMapID);       // 把B怪刷出来吧

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 1, 2);   // 杀死吊桥怪 1/1 -- 显示杀怪状态


                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 3);   // 杀死巫师怪 0/8 -- 显示杀怪状态

            }
            if (monster.MonsterInfo.ExtensionID == bcDataTmp.NeedKillMonster2ID && bcTmp.m_bKillMonsterBStatus == 0)
            {
                int killedMonster = Interlocked.Increment(ref bcTmp.m_nKillMonsterBCount);

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, bcTmp.m_nKillMonsterBCount, 3);   // 杀死巫师怪 数量/8 -- 显示杀怪状态

                if (killedMonster == bcDataTmp.NeedKillMonster2Num)
                {
                    bcTmp.m_Step++;

                    // 把水晶棺刷出来
                    int monsterID = bcDataTmp.CrystalID;
                    string[] sfields = bcDataTmp.CrystalPos.Split(',');

                    int nPosX = Global.SafeConvertToInt32(sfields[0]);
                    int nPosY = Global.SafeConvertToInt32(sfields[1]);


                    GameMap gameMap = null;
                    if (!GameManager.MapMgr.DictMaps.TryGetValue(bcTmp.m_nMapCode, out gameMap))
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("血色城堡报错 地图配置 ID = {0}", bcDataTmp.MapCode));
                        return;
                    }
                    int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                    int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                    GameManager.MonsterZoneMgr.AddDynamicMonsters(bcTmp.m_nMapCode, monsterID, cmInfo.CopyMapID, 1, gridX, gridY, 0);

                    GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 4);   // 杀死水晶棺 0/1 -- 显示杀怪状态

                    bcTmp.m_bKillMonsterBStatus = 1;
                }
            }

            if (monster.MonsterInfo.ExtensionID == bcDataTmp.CrystalID)
            {
                bcTmp.m_Step++;

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 1, 4);   // 杀死水晶棺 1/1 -- 显示杀怪状态


                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 0, 5);   // 采集雕像 0/1 -- 显示杀怪状态

            }
#endif


#if ___CC___FUCK___YOU___BB___
            if (monster.XMonsterInfo.MonsterId == bcDataTmp.DiaoXiangID)
            {
                bcTmp.m_Step++;

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 1, 5);   // 采集雕像 1/1 -- 显示杀怪状态

                CompleteBloodcastleAndGiveAwards(client, bcTmp, bcDataTmp);
                //GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                //                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 1, 6);   // 提交大天使武器 1/1 -- 显示杀怪状态
            }
            // 刷雕像
            if (monster.XMonsterInfo.MonsterId == bcDataTmp.CrystalID)
            {
                int monsterID = bcDataTmp.DiaoXiangID;
                string[] sfields = bcDataTmp.DiaoXiangPos.Split(',');

                int nPosX = Global.SafeConvertToInt32(sfields[0]);
                int nPosY = Global.SafeConvertToInt32(sfields[1]);

                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(bcTmp.m_nMapCode, out gameMap))
                {
                    return;
                }

                int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                GameManager.MonsterZoneMgr.AddDynamicMonsters(bcTmp.m_nMapCode, monsterID, cmInfo.CopyMapID, 1, gridX, gridY, 0);
            }
#else
            if (monster.MonsterInfo.ExtensionID == bcDataTmp.DiaoXiangID)
            {
                bcTmp.m_Step++;

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 1, 5);   // 采集雕像 1/1 -- 显示杀怪状态

                CompleteBloodcastleAndGiveAwards(client, bcTmp, bcDataTmp);
                //GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                //                                               cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEKILLMONSTERSTATUS, 0, 1, 6);   // 提交大天使武器 1/1 -- 显示杀怪状态
            }
             // 刷雕像
            if (monster.MonsterInfo.ExtensionID == bcDataTmp.CrystalID)
            {
                int monsterID = bcDataTmp.DiaoXiangID;
                string[] sfields = bcDataTmp.DiaoXiangPos.Split(',');

                int nPosX = Global.SafeConvertToInt32(sfields[0]);
                int nPosY = Global.SafeConvertToInt32(sfields[1]);

                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(bcTmp.m_nMapCode, out gameMap))
                {
                    return;
                }

                int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                GameManager.MonsterZoneMgr.AddDynamicMonsters(bcTmp.m_nMapCode, monsterID, cmInfo.CopyMapID, 1, gridX, gridY, 0);
            }
#endif
        }

        /// <summary>
        /// 给奖励
        /// </summary>
        public void GiveAwardBloodCastCopyScene(GameClient client, int nMultiple)
        {
            int FuBenSeqID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BloodCastleFuBenSeqID);

            int nSceneID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BloodCastleSceneid);
            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BloodCastleSceneFinishFlag);

            //如果已经获取过一次奖励，则不再提示奖励
            //查找角色的ID+副本顺序ID对应地图编号的奖励领取状态
            int awardState = GameManager.CopyMapMgr.FindAwardState(client.ClientData.RoleID, FuBenSeqID, nSceneID);
            if (awardState > 0)
            {
                GameManager.ClientMgr.NotifyImportantMsg(
                    Global._TCPManager.MySocketListener,
                    Global._TCPManager.TcpOutPacketPool, client, StringUtil.substitute(Global.GetLang("当前副本地图的奖励只能领取一次")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                return;
            }

            BloodCastleDataInfo bcDataTmp = null;
            if (!Data.BloodCastleDataInfoList.TryGetValue(nSceneID, out bcDataTmp))
                return;

            // 如果提交了任务(就是提交了水晶棺物品) 就给大家奖励
            if (nFlag == 1)
            {
                string[] sItem = bcDataTmp.AwardItem2;

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
                        int nBinding = Convert.ToInt32(sFields[2].Trim());

                        GoodsData goodsData = new GoodsData() {Id = -1,GoodsID = nGoodsID,Using = 0,Forge_level = 0,Starttime = "1900-01-01 12:00:00",Endtime = Global.ConstGoodsEndTime,Site = 0,
                            Quality = (int)GoodsQuality.White,Props = "",GCount = nGoodsNum,Binding = nBinding,Jewellist = "",BagIndex = 0,AddPropIndex = 0,BornIndex = 0,Lucky = 0,Strong = 0,
                            ExcellenceInfo = 0,AppendPropLev = 0,ChangeLifeLevForEquip = 0};

                        string sMsg = /**/"血色堡垒奖励--统一奖励";

                        if (!Global.CanAddGoodsNum(client, nGoodsNum))
                        {
                            //for (int j = 0; j < nGoodsNum; ++j)
                                Global.UseMailGivePlayerAward(client, goodsData, Global.GetLang("血色堡垒奖励"), sMsg);
                        }
                        else
                            Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, nGoodsID, nGoodsNum, goodsData.Quality, "", goodsData.Forge_level,
                                                        goodsData.Binding, 0, "", true, 1, sMsg, goodsData.Endtime);
                    }
                }
            }

            // 根据积分以及公式给奖励(经验)
            if (client.ClientData.BloodCastleAwardPoint > 0)
            {
                // 公式
                long nExp = nMultiple * Global.CalcExpForRoleScore(client.ClientData.BloodCastleAwardPoint, bcDataTmp.ExpModulus);
                int nMoney = client.ClientData.BloodCastleAwardPoint * bcDataTmp.MoneyModulus;

                if (nExp > 0)
                {
                    GameManager.ClientMgr.ProcessRoleExperience(client, nExp, false);
                    GameManager.ClientMgr.NotifyAddExpMsg(client, nExp);
                }

                if (nMoney > 0)
                {
                    GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nMoney, "血色城堡副本", false);
                    GameManager.ClientMgr.NotifyAddJinBiMsg(client, nMoney);
                }
                //GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nMoney);

                // 存盘
                if (client.ClientData.BloodCastleAwardPoint > client.ClientData.BloodCastleAwardTotalPoint)
                    client.ClientData.BloodCastleAwardTotalPoint = client.ClientData.BloodCastleAwardPoint;

                if (client.ClientData.BloodCastleAwardPoint > m_nTotalPointValue)
                    SetBloodCastleCopySceneTotalPoint(client.ClientData.RoleName, client.ClientData.BloodCastleAwardPoint);

                // 清空
                client.ClientData.BloodCastleAwardPoint = 0;
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastlePlayerPoint, client.ClientData.BloodCastleAwardPoint, true);
            }

            GameManager.CopyMapMgr.AddAwardState(client.ClientData.RoleID, FuBenSeqID, nSceneID, 1);
        }

        /// <summary>
        /// 玩家离开血色堡垒
        /// </summary>
        public void LeaveBloodCastCopyScene(GameClient client, bool clearScore = false)
        {
            int nFuBenId = -1;
            nFuBenId = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BloodCastleSceneid);

            if (client.ClientData.CopyMapID < 0 || client.ClientData.FuBenSeqID < 0 || !IsBloodCastleCopyScene(nFuBenId))
                return;

            CopyMap cmInfo = null;
            lock (m_BloodCastleCopyScenesList)
            {
                if (!m_BloodCastleCopyScenesList.TryGetValue(client.ClientData.FuBenSeqID, out cmInfo) || cmInfo == null)
                    return;
            }

            Dictionary<int, BloodCastleScene> dicTmp = null;
            lock (m_BloodCastleCopyScenesInfo)
            {
                if (!m_BloodCastleCopyScenesInfo.TryGetValue(client.ClientData.FuBenID, out dicTmp) || dicTmp == null)
                    return;

                BloodCastleScene bcTmp = null;
                if (!dicTmp.TryGetValue(client.ClientData.FuBenSeqID, out bcTmp) || bcTmp == null)
                    return;

                Interlocked.Decrement(ref bcTmp.m_nPlarerCount);

                GameManager.ClientMgr.NotifyBloodCastleCopySceneMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                                        cmInfo, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEPLAYERNUMNOTIFY, 0, 0, 0, bcTmp.m_nPlarerCount);
                if (clearScore && bcTmp.m_eStatus == BloodCastleStatus.FIGHT_STATUS_BEGIN)
                {
                    client.ClientData.BloodCastleAwardPoint = 0;
                }
            }

            // 离开时 保存积分

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastlePlayerPoint, client.ClientData.BloodCastleAwardPoint, true);

            // 如果持有水晶棺宝物 则在离开场景时 掉落出来
#if false
            // 获取到没有装备的物品列表
            //List<GoodsData> fallGoodsList = Global.GetFallGoodsList(client);
            List<GoodsData> goodsDataList = new List<GoodsData>();
            
            GoodsData tmpFallGoods = null;
            tmpFallGoods = Global.GetGoodsByID(client, (int)BloodCastleCrystalItemID.BloodCastleCrystalItemID1);

            if (tmpFallGoods == null)
                tmpFallGoods = Global.GetGoodsByID(client, (int)BloodCastleCrystalItemID.BloodCastleCrystalItemID2);

            if (tmpFallGoods == null)
                tmpFallGoods = Global.GetGoodsByID(client, (int)BloodCastleCrystalItemID.BloodCastleCrystalItemID3);

            if (tmpFallGoods != null)
            {
                int oldGoodsNum = 1;
                if (Global.GetGoodsDefaultCount(tmpFallGoods.GoodsID) > 1)
                    oldGoodsNum = tmpFallGoods.GCount;

                if (GameManager.ClientMgr.FallRoleGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, tmpFallGoods))
                {
                    tmpFallGoods = Global.CopyGoodsData(tmpFallGoods);
                    tmpFallGoods.Id = GameManager.GoodsPackMgr.GetNextGoodsID();
                    tmpFallGoods.GCount = oldGoodsNum;
                    goodsDataList.Add(tmpFallGoods);
                }

                Point grid = client.CurrentGrid;

                List<GoodsPackItem> tempgoodsPackItem = GameManager.GoodsPackMgr.GetRoleGoodsPackItemList(client.ClientData.RoleID, Global.FormatRoleName(client, client.ClientData.RoleName),
                                                                                                            goodsDataList, client.ClientData.MapCode, client.ClientData.CopyMapID,
                                                                                                                (int)grid.X, (int)grid.Y, client.ClientData.RoleName);

                StringBuilder sInfor = new StringBuilder();

                for (int i = 0; i < tempgoodsPackItem.Count; i++)
                {
                    GameManager.GoodsPackMgr.ProcessGoodsPackItem(client, client, tempgoodsPackItem[i], 0);

                    sInfor.AppendFormat("{0}", Global.GetGoodsNameByID(tempgoodsPackItem[i].GoodsDataList[0].GoodsID));
                    if (i != tempgoodsPackItem.Count - 1)
                        sInfor.Append(" ");
                }

                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,client,
                                                            StringUtil.substitute(Global.GetLang("很不幸，[{0}]离开血色堡垒 掉落物品{1}"), client.ClientData.RoleName, 
                                                            sInfor.ToString()),GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
            }

            //client.ClientData.bIsInBloodCastleMap = false;

            return;
#endif
        }

        /// <summary>
        /// 玩家离开血色堡垒
        /// </summary>
        public void LogOutWhenInBloodCastleCopyScene(GameClient client)
        {
            LeaveBloodCastCopyScene(client);
        }

        /// <summary>
        /// 完成血色堡垒
        /// </summary>
        public void CompleteBloodCastScene(GameClient client, BloodCastleScene bsInfo, BloodCastleDataInfo bsData)
        {
            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BloodCastleSceneFinishFlag);

            if (nFlag != 1)
            {
                // 保存完成状态
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BloodCastleSceneFinishFlag, 1, true);
            }
        }

        /// <summary>
        /// 击杀水晶雕像后,完成活动并给予奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bcTmp"></param>
        /// <param name="bcDataTmp"></param>
        public void CompleteBloodcastleAndGiveAwards(GameClient client, BloodCastleScene bcTmp, BloodCastleDataInfo bcDataTmp)
        {
            CopyMap cmInfo = null;
            cmInfo = GameManager.BloodCastleCopySceneMgr.GetBloodCastleCopySceneInfo(client.ClientData.FuBenSeqID);

            if (cmInfo == null)
                return;

            if (bcTmp.m_eStatus == BloodCastleStatus.FIGHT_STATUS_END)
            {
                return;
            }

            bcTmp.m_nRoleID = client.ClientData.RoleID;

            // 2 给奖励物品

            string[] sItem = bcDataTmp.AwardItem1;

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
                    int nBinding = Convert.ToInt32(sFields[2].Trim());

                    GoodsData goods = new GoodsData()
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

                    string sMsg = Global.GetLang("血色堡垒奖励--提交任务者奖励");

                    if (!Global.CanAddGoodsNum(client, nGoodsNum))
                    {
                        //for (int j = 0; j < nGoodsNum; ++j)
                            Global.UseMailGivePlayerAward(client, goods, Global.GetLang("血色堡垒奖励"), sMsg);
                    }
                    else
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, nGoodsID, nGoodsNum, 0, "", 0, goods.Binding, 0, "", true, 1, sMsg);
                }
            }

            // 3 置场景状态 战斗结束
            bcTmp.m_eStatus = BloodCastleStatus.FIGHT_STATUS_END;
            bcTmp.m_lEndTime = TimeUtil.NOW();

            bcTmp.m_bIsFinishTask = true;

            // 完成该血色堡垒
            CompleteBloodCastScene(client, bcTmp, bcDataTmp);
        }

        /// <summary>
        /// 清除
        /// </summary>
        public void CleanBloodCastScene(int mapid)
        {
            // 首先是动态刷出的怪
            /*for (int i = 0; i < m_BloodCastleListScenes[mapid].m_nDynamicMonsterList.Count; i++)
            {
                Monster monsterInfo = m_BloodCastleListScenes[mapid].m_nDynamicMonsterList[i];
                if (monsterInfo != null)
                {
                    GameManager.MonsterMgr.AddDelayDeadMonster(monsterInfo);
                    //RemoveBloodCastleDynamicMonster(mapid, monsterInfo);
                }
            }*/

            return;
        }

        // add by chenjingui. 20150704 角色改名后，检测是否更新最高积分者
        // note: 这地方肯定会有线程安全问题啊，根据以前代码，玩家提交副本时，检测积分，如果大于当前副本的积分，就更新最高积分和名字
        // 但是每个玩家的socket消息处理是多线程的啊，也没有加锁。
        public void OnChangeName(int roleId, string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                return;
            }

            if (!string.IsNullOrEmpty(m_sTotalPointName) && m_sTotalPointName == oldName)
            {
                m_sTotalPointName = newName;
            }
        }
     }

}
