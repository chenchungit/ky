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
    // 金币副本管理器 [6/11/2014 LiaoWei]
    class GlodCopySceneManager
    {
        /// <summary>
        /// 准备时间
        /// </summary>
        public static int m_PrepareTime = 1000 * 10;

        /// <summary>
        /// 延迟时间
        /// </summary>
        public static int m_DelayTime = 1000 * 2;

        /// <summary>
        /// 副本场景LIST
        /// </summary>
        public static Dictionary<int, CopyMap> m_GlodCopySceneLists = new Dictionary<int, CopyMap>(); // key: 副本流水ID 即DB生成  Value: CopyMap信息

        /// <summary>
        /// 经验副本数据
        /// </summary>
        public static Dictionary<int, GoldCopyScene> m_GlodCopySceneInfo = new Dictionary<int, GoldCopyScene>(); // key: 副本流水ID 即DB生成  Value: GoldCopyScene信息

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public static void AddGlodCopySceneList(int nID, CopyMap mapInfo)
        {
            bool bInsert = false;

            lock (m_GlodCopySceneLists)
            {
                CopyMap tmp = null;

                if (!m_GlodCopySceneLists.TryGetValue(nID, out tmp))
                {
                    m_GlodCopySceneLists.Add(nID, mapInfo);
                    bInsert = true;
                }
                else
                {
                    if (tmp == null)
                    {
                        m_GlodCopySceneLists[nID] = mapInfo;
                        bInsert = true;
                    }
                }

                lock (m_GlodCopySceneInfo)
                {
                    if (bInsert == true)
                    {
                        GoldCopyScene GoldCopySceneInfo = null;

                        if (!m_GlodCopySceneInfo.TryGetValue(nID, out GoldCopySceneInfo))
                        {
                            GoldCopySceneInfo = new GoldCopyScene();

                            GoldCopySceneInfo.InitInfo(mapInfo.MapCode, mapInfo.CopyMapID, nID);

                            GoldCopySceneInfo.m_StartTimer = TimeUtil.NOW();

                            m_GlodCopySceneInfo.Add(nID, GoldCopySceneInfo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public static void RemoveGlodCopySceneList(int nID)
        {
            lock (m_GlodCopySceneLists)
            {
                m_GlodCopySceneLists.Remove(nID);
            }

            lock (m_GlodCopySceneInfo)
            {
                m_GlodCopySceneInfo.Remove(nID);
            }
        }

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long LastHeartBeatTicks = 0L;

        /// <summary>
        // 心跳处理
        /// </summary>
        public static void HeartBeatGlodCopyScene()
        {
            long nowTicks = TimeUtil.NOW();
            if (nowTicks - LastHeartBeatTicks < (1000))
            {
                return;
            }

            LastHeartBeatTicks = LastHeartBeatTicks < TimeUtil.DAY ? nowTicks : LastHeartBeatTicks + 1000;

            // lock住！！！
            lock (m_GlodCopySceneLists)
            {
                foreach (var item in m_GlodCopySceneLists.Values)
                {
                    List<GameClient> clientsList = item.GetClientsList();

                    GoldCopyScene tmpGoldCopySceneData = null;

                    lock (m_GlodCopySceneInfo)
                    {
                        if (!m_GlodCopySceneInfo.TryGetValue(item.FuBenSeqID, out tmpGoldCopySceneData))
                        {
                            continue;
                        }

                        //tmpGoldCopySceneData = m_GlodCopySceneInfo[item.FuBenSeqID];
                    }

                    if (tmpGoldCopySceneData == null)
                        continue;

                    lock (tmpGoldCopySceneData)
                    {
                        if (tmpGoldCopySceneData.m_TimeNotifyFlag == 0)
                        {
                            tmpGoldCopySceneData.m_TimeNotifyFlag = 1;

                            if (clientsList.Count() != 0 && clientsList[0] != null)
                                SendMsgToClientForGlodCopyScenePrepare(clientsList[0], tmpGoldCopySceneData);
                        }

                        // 准备时间
                        if (nowTicks >= (tmpGoldCopySceneData.m_StartTimer + m_PrepareTime))
                        {
                            int nWave = tmpGoldCopySceneData.m_CreateMonsterWave;
                            int nCount = Data.Goldcopyscenedata.GoldCopySceneMonsterData.Count();

                            if (nWave > nCount)
                                continue;

                            if (nWave == 0 && tmpGoldCopySceneData.m_CreateMonsterFirstWaveFlag == 0)
                            {
                                tmpGoldCopySceneData.m_CreateMonsterFirstWaveFlag = 1;
                                tmpGoldCopySceneData.m_CreateMonsterWave = 1;

                                //SendMsgToClientForGlodCopySceneMonsterWave(clientsList[0], 0);
                            }

                            GoldCopySceneMonster tmpMonsterInfo = null;

                            if (!Data.Goldcopyscenedata.GoldCopySceneMonsterData.TryGetValue(nWave, out tmpMonsterInfo))
                                continue;
                            
                            if (tmpMonsterInfo != null)
                            {
                                // 延迟间隔判断
                                if (nowTicks - tmpGoldCopySceneData.m_CreateMonsterTick2 > tmpMonsterInfo.m_Delay2 * 1000)       // 大波间隔时间判断
                                {
                                    if (tmpGoldCopySceneData.m_CreateMonsterWaveNotify == 0)
                                    {
                                        tmpGoldCopySceneData.m_CreateMonsterWaveNotify = 1;
                                        if (clientsList.Count() != 0 && clientsList[0] != null)
                                            SendMsgToClientForGlodCopySceneMonsterWave(clientsList[0], tmpGoldCopySceneData.m_CreateMonsterWave);
                                    }

                                    if (nowTicks - tmpGoldCopySceneData.m_CreateMonsterTick1 > tmpMonsterInfo.m_Delay1 * 1000)   // 小波间隔时间判断
                                    {
                                        if (tmpGoldCopySceneData.m_LoginEnterFlag == 1)
                                        {

                                            if (clientsList.Count() != 0 && clientsList[0] != null && nowTicks - tmpGoldCopySceneData.m_LoginEnterTimer > m_DelayTime)
                                            {
                                                tmpGoldCopySceneData.m_LoginEnterFlag = 0;
                                                SendMsgToClientForGlodCopySceneMonsterWave(clientsList[0], tmpGoldCopySceneData.m_CreateMonsterWave);
                                            }
                                        }

                                        tmpGoldCopySceneData.m_CreateMonsterTick1 = tmpGoldCopySceneData.m_CreateMonsterTick1 < TimeUtil.DAY ? nowTicks : tmpGoldCopySceneData.m_CreateMonsterTick1 + tmpMonsterInfo.m_Delay1 * 1000;

                                        if (clientsList.Count() != 0 && clientsList[0] != null)
                                            CreateMonsterForGoldCopyScene(clientsList[0], tmpGoldCopySceneData, tmpGoldCopySceneData.m_CreateMonsterWave);
                                        else
                                            CreateMonsterForGoldCopyScene(null, tmpGoldCopySceneData, tmpGoldCopySceneData.m_CreateMonsterWave);
                                    }
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
        public static void CreateMonsterForGoldCopyScene(GameClient client, GoldCopyScene goldcopyscene, int nWave)
        {
            GoldCopySceneMonster tmpInfo = Data.Goldcopyscenedata.GoldCopySceneMonsterData[nWave];
            long ticks = TimeUtil.NOW();

            // 随机刷怪
            int nRom = Global.GetRandomNumber(0, 10);

            // 在起点刷怪
            int[] pos = Data.Goldcopyscenedata.m_MonsterPatorlPathList[0];
            Point toPos = new Point(pos[0], pos[1]);

            GameManager.MonsterZoneMgr.AddDynamicMonsters(goldcopyscene.m_MapCodeID, tmpInfo.m_MonsterID[nRom], goldcopyscene.m_CopyMapID, 1, (int)toPos.X, (int)toPos.Y, 1);

            goldcopyscene.m_CreateMonsterCount += 1;

            // 第N大波刷完了
            if (goldcopyscene.m_CreateMonsterCount == tmpInfo.m_Num)
            {
                goldcopyscene.m_CreateMonsterTick2 = ticks; // 设定大波刷完时间
                goldcopyscene.m_CreateMonsterWave = nWave + 1;
                goldcopyscene.m_CreateMonsterCount = 0;
                goldcopyscene.m_CreateMonsterWaveNotify = 0;

                //SendMsgToClientForGlodCopySceneMonsterWave(client, nWave);

                if (goldcopyscene.m_CreateMonsterWave > Data.Goldcopyscenedata.GoldCopySceneMonsterData.Count() && client != null)
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                       StringUtil.substitute(Global.GetLang("金币副本 挂怪结束了")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
            }
        }

        /// <summary>
        /// 处理行走
        /// </summary>
        public static void MonsterMoveStepGoldCopySceneCopyMap(Monster monster)
        {
            long ticks = TimeUtil.NOW();

            // 1秒走一步 如果体验不好 就调整之 todo...
            if (ticks - monster.MoveTime < (1 * 500))
                return;

            int nStep = monster.Step; // 当前点
            int nNumStep = monster.PatrolPath.Count() - 1; // 最后一个点
            int nNextStep = nStep + 1; // 下一个路径点

            // 已经到最后一个点了 删除怪
            if (nNextStep >= nNumStep)
            {
                GameManager.MonsterMgr.AddDelayDeadMonster(monster); // 将怪物加入延迟死亡
                return;
            }

            // 取得目标坐标的格子信息
            int nMapCode = (int)GoldCopySceneEnum.GOLDCOPYSCENEMAPCODEID;
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[nMapCode];

            int nNextX = monster.PatrolPath[nNextStep][0]; // 目标路径x
            int nNextY = monster.PatrolPath[nNextStep][1]; // 目标路径y

            // 目标格子
            int gridX = nNextX / mapGrid.MapGridWidth;
            int gridY = nNextY / mapGrid.MapGridHeight;
            Point ToGrid = new Point(gridX, gridY);

            // 怪物当前格子
            Point grid = monster.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            // 取得和目标坐标的方向值
            double Direction = Global.GetDirectionByAspect(gridX, gridY, nCurrX, nCurrY);

            // 行走
            ChuanQiUtils.WalkTo(monster, (Dircetions)Direction);

            monster.MoveTime = ticks;

            // 允许误差
            if (Global.GetTwoPointDistance(ToGrid, grid) < 2)
                monster.Step = nStep + 1;

        }

        /// <summary>
        // 通知客户端
        /// </summary>
        public static void SendMsgToClientForGlodCopyScenePrepare(GameClient client, GoldCopyScene goldcopyscene)
        {
            if (client != null)
            {
                int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
                if (fuBenID > 0)
                {
                    string strcmd = "";

                    long ticks = TimeUtil.NOW();    // 当前tick

                    int nTimer = (int)((m_PrepareTime - (ticks - goldcopyscene.m_StartTimer)) / 1000);

                    strcmd = string.Format("{0}", nTimer);

                    GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_GOLDCOPYSCENEPREPAREFIGHT);
                }
            }
        }

        /// <summary>
        // 通知客户端怪波数信息
        /// </summary>
        public static void SendMsgToClientForGlodCopySceneMonsterWave(GameClient client, int nWave)
        {
            if (client != null)
            {
                int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
                if (fuBenID > 0)
                {
                    string strcmd = string.Format("{0}:{1}", nWave, Data.Goldcopyscenedata.GoldCopySceneMonsterData.Count());//1.当前的波数 2.总波数
                    TCPOutPacket tcpOutPacket = null;
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GOLDCOPYSCENEMONSTERWAVE);
                    Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket);
                }
            }
            
        }

        /// <summary>
        // 玩家登陆后进入金币副本
        /// </summary>
        public static bool EnterGoldCopySceneWhenLogin(GameClient client, bool bContinue = true)
        {
            if (client != null)
            {
                CopyMap tmp = null;
                GoldCopyScene GoldCopySceneInfo = null;

                lock (m_GlodCopySceneLists)
                {
                    if (!m_GlodCopySceneLists.TryGetValue(client.ClientData.FuBenSeqID, out tmp) || tmp == null)
                    {
                        return false;
                    }
                }

                lock (m_GlodCopySceneInfo)
                {
                    if (!m_GlodCopySceneInfo.TryGetValue(client.ClientData.FuBenSeqID, out GoldCopySceneInfo) || GoldCopySceneInfo == null)
                    {
                        return false;
                    }
                }
                long ticks = TimeUtil.NOW();    // 当前tick

                GoldCopySceneInfo.m_LoginEnterTimer = ticks;
                GoldCopySceneInfo.m_LoginEnterFlag = 1;

                /*if (bContinue == false)
                {
                    return true;
                }

                int fuBenID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
                if (fuBenID > 0)
                {
                    List<GameClient> clientsList = tmp.GetClientsList();

                    if (clientsList.Count() != 0 && clientsList[0] != null)
                    {
                        string strcmd = string.Format("{0}:{1}", GoldCopySceneInfo.m_CreateMonsterWave, Data.Goldcopyscenedata.GoldCopySceneMonsterData.Count());//1.当前的波数 2.总波数
                        //TCPOutPacket tcpOutPacket = null;
                        //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_GOLDCOPYSCENEMONSTERWAVE);
                        //Global._TCPManager.MySocketListener.SendData(clientsList[0].ClientSocket, tcpOutPacket);
                        GameManager.ClientMgr.SendToClient(clientsList[0], strcmd, (int)TCPGameServerCmds.CMD_SPR_GOLDCOPYSCENEMONSTERWAVE);
                    }
                }*/

                return true;
            }

            return false;

        }

    }
}
