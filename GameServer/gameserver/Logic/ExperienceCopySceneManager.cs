using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Xml.Linq;
using Server.Data;
using System.Windows;
using Server.Tools;
using Server.Protocol;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    // 经验副本管理器 [3/18/2014 LiaoWei]
    class ExperienceCopySceneManager
    {
        /// <summary>
        /// 副本场景LIST
        /// </summary>
        public static Dictionary<int, CopyMap> m_ExperienceListCopyMaps = new Dictionary<int, CopyMap>(); // key: 副本流水ID 即DB生成  Value: CopyMap信息

        /// <summary>
        /// 经验副本数据
        /// </summary>
        public static Dictionary<int, ExperienceCopyScene> m_ExperienceListCopyMapsInfo = new Dictionary<int, ExperienceCopyScene>(); // key: 副本流水ID 即DB生成  Value: ExperienceCopyScene信息

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public static void AddExperienceListCopyMap(int nID, CopyMap mapInfo)
        {
            bool bInsert = false;
            lock (m_ExperienceListCopyMaps)
            {
                CopyMap tmp = null;

                if (!m_ExperienceListCopyMaps.TryGetValue(nID, out tmp))
                {
                    m_ExperienceListCopyMaps.Add(nID, mapInfo);
                    bInsert = true;
                }
                else
                {
                    if (tmp == null)
                    {
                        m_ExperienceListCopyMaps[nID] = mapInfo;
                        bInsert = true;
                    }
                }
            }

            lock (m_ExperienceListCopyMapsInfo)
            {
                if (bInsert == true)
                {
                    ExperienceCopyScene ExperienceSceneInfo = null;//new ExperienceCopyScene();

                    if (!m_ExperienceListCopyMapsInfo.TryGetValue(nID, out ExperienceSceneInfo))
                    {
                        ExperienceSceneInfo = new ExperienceCopyScene();
                        
                        ExperienceSceneInfo.InitInfo(mapInfo.MapCode, mapInfo.CopyMapID, nID);

                        ExperienceSceneInfo.m_StartTimer = TimeUtil.NOW();

                        m_ExperienceListCopyMapsInfo.Add(nID, ExperienceSceneInfo);
                    }
                }
            }
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public static void RemoveExperienceListCopyMap(int nID)
        {
            bool bRemve = false;
            lock (m_ExperienceListCopyMaps)
            {
                CopyMap tmp = null;

                if (m_ExperienceListCopyMaps.TryGetValue(nID, out tmp))
                {
                    m_ExperienceListCopyMaps.Remove(nID);
                    bRemve = true;
                }                
            }

            lock (m_ExperienceListCopyMapsInfo)
            {
                if (bRemve)
                {
                    ExperienceCopyScene ExperienceSceneInfo = null;

                    if (m_ExperienceListCopyMapsInfo.TryGetValue(nID, out ExperienceSceneInfo))
                    {
                        m_ExperienceListCopyMapsInfo.Remove(nID);
                    }
                }
            }
        }

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long LastHeartBeatTicks = 0L;

        /// <summary>
        // 心跳处理
        /// </summary>
        public static void HeartBeatExperienceCopyMap()
        {
            long nowTicks = TimeUtil.NOW();
            if (nowTicks - LastHeartBeatTicks < (1000))
            {
                return;
            }

            LastHeartBeatTicks = LastHeartBeatTicks < TimeUtil.DAY ? nowTicks : LastHeartBeatTicks + 1000;

            List<CopyMap> CopyMapList = new List<CopyMap>();

            // lock住！！！
            lock (m_ExperienceListCopyMaps)
            {
                foreach (var item in m_ExperienceListCopyMaps.Values)
                {
                    List<GameClient> clientsList = item.GetClientsList();
                    /*if (null != clientsList && clientsList.Count <= 0)
                    {
                        CopyMapList.Add(item);
                        continue;
                    }*/
                    
                    ExperienceCopyMapDataInfo tmp = null;
                    tmp = Data.ExperienceCopyMapDataInfoList[item.MapCode];

                    if (tmp == null)
                        continue;

                    ExperienceCopyScene tmpExSceneInfo = null;

                    lock (m_ExperienceListCopyMapsInfo)
                    {
                        if (!m_ExperienceListCopyMapsInfo.TryGetValue(item.FuBenSeqID, out tmpExSceneInfo))
                            continue;

                        //tmpExSceneInfo = m_ExperienceListCopyMapsInfo[item.FuBenSeqID];
                    }

                    if (tmpExSceneInfo == null)
                        continue;

                    /*int fuBenID = FuBenManager.FindFuBenIDByMapCode(item.MapCode);
                    FuBenMapItem fuBenMapItem = FuBenManager.FindMapCodeByFuBenID(fuBenID, item.MapCode);
                    if (null == fuBenMapItem)
                        continue;
                    
                    //是否超时
                    if (nowTicks - tmpExSceneInfo.m_StartTimer >= (fuBenMapItem.MaxTime * 60L * 1000L))
                    {
                        CopyMapList.Add(item);
                        continue;
                    }*/

                    int nWave = tmpExSceneInfo.m_ExperienceCopyMapCreateMonsterWave;
                    int nCount = tmp.MonsterIDList.Count;    // 一共有几波

                    if (nWave >= nCount) // 已经刷完了
                        continue;

                    if (tmpExSceneInfo.m_ExperienceCopyMapCreateMonsterFlag == 0)
                    {
                        if (clientsList.Count() != 0 && clientsList[0] != null)
                            ExperienceCopyMapCreateMonster(clientsList[0], tmpExSceneInfo, tmp, nWave);
                        else
                            ExperienceCopyMapCreateMonster(null, tmpExSceneInfo, tmp, nWave);
                    }
                }
            }
            
            for (int i = 0; i < CopyMapList.Count; ++i)
                GameManager.CopyMapMgr.ProcessRemoveCopyMap(CopyMapList[i]);
        }

        /// <summary>
        // 刷怪接口
        /// </summary>
        static public void ExperienceCopyMapCreateMonster(GameClient client, ExperienceCopyScene ExperienceMapInfo, ExperienceCopyMapDataInfo exMap, int nWave)
        {
            ExperienceMapInfo.m_ExperienceCopyMapCreateMonsterFlag = 1;

            ++ExperienceMapInfo.m_ExperienceCopyMapCreateMonsterWave;

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(ExperienceMapInfo.m_MapCodeID, out gameMap))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("经验副本 地图配置 ID = {0}", ExperienceMapInfo.m_MapCodeID));
                return;
            }

            int gridX = gameMap.CorrectWidthPointToGridPoint(exMap.posX) / gameMap.MapGridWidth;
            int gridY = gameMap.CorrectHeightPointToGridPoint(exMap.posZ) / gameMap.MapGridHeight;

            int gridNum = gameMap.CorrectWidthPointToGridPoint(exMap.Radius);

            int nID     = 0;
            int nNum    = 0;
            int nTotal  = 0;

            //for (int i = 0; i < exMap.MonsterIDList.Count; ++i)
            {
                List<int> nListID = exMap.MonsterIDList[nWave];
                List<int> nListNum = exMap.MonsterNumList[nWave];

                for (int j = 0; j < nListID.Count; ++j)
                {
                    nID     = nListID[j];
                    nNum    = nListNum[j];

                    GameManager.MonsterZoneMgr.AddDynamicMonsters(ExperienceMapInfo.m_MapCodeID, nID, ExperienceMapInfo.m_CopyMapID, nNum, gridX, gridY, gridNum);

                    nTotal += nNum;

                    ExperienceMapInfo.m_ExperienceCopyMapCreateMonsterNum += nNum;

                    ExperienceMapInfo.m_ExperienceCopyMapRemainMonsterNum += nNum;
                }
            }

            // 计数要杀死怪的数量
            ExperienceMapInfo.m_ExperienceCopyMapNeedKillMonsterNum = ExperienceMapInfo.m_ExperienceCopyMapCreateMonsterNum * exMap.CreateNextWaveMonsterCondition[nWave] / 100;

            //SysConOut.WriteLine("liaowei是帅哥  经验副本 {0} 里 刷怪了 数量是 {1} ！！！", exMap.MapCodeID, ExperienceMapInfo.m_ExperienceCopyMapCreateMonsterNum);

            if (client != null)
                SendMsgToClientForExperienceCopyMapInfo(client, ExperienceMapInfo.m_ExperienceCopyMapCreateMonsterWave);
        }

        /// <summary>
        // 杀怪接口
        /// </summary>
        static public void ExperienceCopyMapKillMonster(GameClient client, Monster monster)
        {
            ExperienceCopyMapDataInfo TmpExInfo = null;

            if (!Data.ExperienceCopyMapDataInfoList.TryGetValue(client.ClientData.MapCode, out TmpExInfo))
                return;

            ExperienceCopyScene tmpExSceneInfo = null;

            // 此处需要加锁
            lock (m_ExperienceListCopyMapsInfo)
            {
                if (!m_ExperienceListCopyMapsInfo.TryGetValue(client.ClientData.FuBenSeqID, out tmpExSceneInfo))
                {
                    return;
                }
                //tmpExSceneInfo = m_ExperienceListCopyMapsInfo[client.ClientData.FuBenSeqID];
            }

            if (tmpExSceneInfo == null)
                return;

            CopyMap TmpCopyMapInfo = null;
            //TmpCopyMapInfo = m_ExperienceListCopyMaps[client.ClientData.FuBenSeqID];

            if (m_ExperienceListCopyMaps.TryGetValue(client.ClientData.FuBenSeqID, out TmpCopyMapInfo))
            {
                if (TmpCopyMapInfo == null)
                    return;
            }
            else
                return;

            ++tmpExSceneInfo.m_ExperienceCopyMapKillMonsterNum;

            ++tmpExSceneInfo.m_ExperienceCopyMapKillMonsterTotalNum;

            --tmpExSceneInfo.m_ExperienceCopyMapRemainMonsterNum;

            if (tmpExSceneInfo.m_ExperienceCopyMapCreateMonsterFlag == 1 && tmpExSceneInfo.m_ExperienceCopyMapKillMonsterNum == tmpExSceneInfo.m_ExperienceCopyMapNeedKillMonsterNum)
            {
                tmpExSceneInfo.m_ExperienceCopyMapCreateMonsterFlag = 0;
                tmpExSceneInfo.m_ExperienceCopyMapKillMonsterNum = 0;
                tmpExSceneInfo.m_ExperienceCopyMapCreateMonsterNum = 0;
            }

            //if (tmpExSceneInfo.m_ExperienceCopyMapKillMonsterTotalNum == TmpExInfo.MonsterSum)
            {
                if (tmpExSceneInfo.m_ExperienceCopyMapCreateMonsterWave == TmpExInfo.MonsterIDList.Count && tmpExSceneInfo.m_ExperienceCopyMapKillMonsterTotalNum == TmpExInfo.MonsterSum)
                    SendMsgToClientForExperienceCopyMapAward(client);

                int nWave = tmpExSceneInfo.m_ExperienceCopyMapCreateMonsterWave;
                if (tmpExSceneInfo.m_ExperienceCopyMapKillMonsterTotalNum == TmpExInfo.MonsterSum || tmpExSceneInfo.m_ExperienceCopyMapRemainMonsterNum == 0)
                {
                    nWave++;
                }
                SendMsgToClientForExperienceCopyMapInfo(client, nWave);
            }

            //SysConOut.WriteLine("liaowei是帅哥  经验副本 {0} 里 杀怪了 剩余数量是 {1}！！！", TmpCopyMapInfo.FuBenSeqID, tmpExSceneInfo.m_ExperienceCopyMapRemainMonsterNum);
        }

        /// <summary>
        // 通知客户端
        /// </summary>
        public static void SendMsgToClientForExperienceCopyMapInfo(GameClient client, int nWave)
        {
            ExperienceCopyScene tmpExSceneInfo = null;

           
            // 此处需要加锁
            lock (m_ExperienceListCopyMapsInfo)
            {
                m_ExperienceListCopyMapsInfo.TryGetValue(client.ClientData.FuBenSeqID, out tmpExSceneInfo);
            }

            if (tmpExSceneInfo == null)
                return;

            int nRealyWave = nWave;
            int nTotalWave = Data.ExperienceCopyMapDataInfoList[client.ClientData.MapCode].MonsterIDList.Count;

            if (nRealyWave > nTotalWave)
            {
                nRealyWave = nTotalWave;
            }

            string strcmd = string.Format("{0}:{1}:{2}", nRealyWave, nTotalWave, tmpExSceneInfo.m_ExperienceCopyMapRemainMonsterNum);
            TCPOutPacket tcpOutPacket = null;
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_EXPERIENCECOPYMAPINFO);
            Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket);
        }
        
        /// <summary>
        // 经验副本奖励
        /// </summary>
        public static void SendMsgToClientForExperienceCopyMapAward(GameClient client)
        {
            CopyMap tmpCopyMap = null;
            tmpCopyMap = m_ExperienceListCopyMaps[client.ClientData.FuBenSeqID];

            if (tmpCopyMap == null)
                return;

            int fuBenSeqID = FuBenManager.FindFuBenSeqIDByRoleID(client.ClientData.RoleID);

            FuBenTongGuanData fubenTongGuanData = null;

            if (fuBenSeqID > 0) //如果副本不存在
            {
                FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(fuBenSeqID);
                if (null != fuBenInfoItem)
                {
                    fuBenInfoItem.EndTicks = TimeUtil.NOW();
                    int addFuBenNum = 1;
                    if (fuBenInfoItem.nDayOfYear != TimeUtil.NowDateTime().DayOfYear)
                    {
                        addFuBenNum = 0;
                    }

                    int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
                    if (fuBenID > 0)
                    {
                        int usedSecs = (int)((fuBenInfoItem.EndTicks - fuBenInfoItem.StartTicks) / 1000);

                        // 更新玩家通关时间信息
                        Global.UpdateFuBenDataForQuickPassTimer(client, fuBenID, usedSecs, addFuBenNum);

                        // 给玩家物品
                        FuBenMapItem fuBenMapItem = FuBenManager.FindMapCodeByFuBenID(fuBenID, client.ClientData.MapCode);

                        if (fuBenMapItem.Experience > 0 && fuBenMapItem.Money1 > 0)
                        {
                            int nMaxTime = fuBenMapItem.MaxTime * 60; //分->秒
                            long startTicks = fuBenInfoItem.StartTicks;
                            long endTicks = fuBenInfoItem.EndTicks;
                            int nFinishTimer = (int)(endTicks - startTicks) / 1000;//毫秒->秒
                            int killedNum = 0;// tmpCopyMap.KilledNormalNum + tmpCopyMap.KilledBossNum;
                            int nDieCount = fuBenInfoItem.nDieCount;

                            //向客户的发放通关奖励
                            fubenTongGuanData = Global.GiveCopyMapGiftForScore(client, fuBenID, client.ClientData.MapCode, nMaxTime, nFinishTimer, killedNum, nDieCount, (int)(fuBenMapItem.Experience * fuBenInfoItem.AwardRate), (int)(fuBenMapItem.Money1 * fuBenInfoItem.AwardRate), fuBenMapItem);

                        }

                        GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDFUBENHISTDATA, string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID,
                                                        Global.FormatRoleName(client, client.ClientData.RoleName), fuBenID, usedSecs), null, client.ServerId);

                        int nLev = -1;
                        SystemXmlItem systemFuBenItem = null;
                        if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(fuBenID, out systemFuBenItem))
                        {
                            nLev = systemFuBenItem.GetIntValue("FuBenLevel");
                        }

                        //更新每日的通关副本的数量
                        GameManager.ClientMgr.UpdateRoleDailyData_FuBenNum(client, 1, nLev, false);

                        //副本通关
                        //Global.BroadcastFuBenOk(client, usedSecs, fuBenID);

                    }
                }
            }

            if (fubenTongGuanData != null)
            {
                //发送奖励到客户端
                TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<FuBenTongGuanData>(fubenTongGuanData, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_FUBENPASSNOTIFY);

                if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket)) { ; }
            }

        }
    }

}
