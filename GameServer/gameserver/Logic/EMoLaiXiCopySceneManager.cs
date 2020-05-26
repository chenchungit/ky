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
using Tmsk.Contract;

namespace GameServer.Logic
{
    // 恶魔来袭副本管理器
    class EMoLaiXiCopySceneManager
    {
        #region 配置信息

        /// <summary>
        /// 恶魔来袭副本数据
        /// </summary>
        public static EMoLaiXiCopySenceData EMoLaiXiCopySencedata = new EMoLaiXiCopySenceData();

        #endregion 配置信息

        #region 运行时信息

        /// <summary>
        /// 准备时间
        /// </summary>
        public static int m_PrepareTime = 1000 * 9;

        /// <summary>
        /// 延迟时间
        /// </summary>
        public static int m_DelayTime = 1000 * 5;

        /// <summary>
        /// 副本场景LIST
        /// key: 副本流水ID 即DB生成
        /// </summary>
        public static Dictionary<int, CopyMap> m_EMoLaiXiCopySceneLists = new Dictionary<int, CopyMap>();

        /// <summary>
        /// 经验副本数据
        /// key: 副本流水ID 即DB生成
        /// </summary>
        public static Dictionary<int, EMoLaiXiCopySence> m_EMoLaiXiCopySceneInfo = new Dictionary<int, EMoLaiXiCopySence>();

        /// <summary>
        /// 恶魔来袭怪物逃走计数字典,<CopyMapID,EscapeCount>
        /// </summary>
        public static Dictionary<int, int> m_EMoLaiXiEscapeMonsterNumDict = new Dictionary<int, int>();

        /// <summary>
        /// 地图编号
        /// </summary>
        public static int EMoLaiXiCopySceneMapCode = 4100;

        #endregion 运行时信息

        #region 初始化配置

        /// <summary>
        /// 金币副本数据 [6/11/2014 LiaoWei]
        /// </summary>
        public static void LoadEMoLaiXiCopySceneInfo()
        {
            try
            {
                int totalWave = 0;//最大波数
                EMoLaiXiCopySencedata.FaildEscapeMonsterNum = (int)GameManager.systemParamsList.GetParamValueIntByName("BaoWeiZhan");

                XElement xmlFile = null;
                xmlFile = Global.GetGameResXml(string.Format("Config/EMoLaiXiLuXian.xml"));
                foreach (var args in xmlFile.Elements("Path"))
                {
                    int id = (int)Global.GetSafeAttributeLong(args, "ID");
                    string sPatorlPathID = Global.GetSafeAttributeStr(args, "Path");
                    List<int[]> pointList = new List<int[]>();
                    if (string.IsNullOrEmpty(sPatorlPathID))
                        LogManager.WriteLog(LogTypes.Warning, string.Format("金币副本怪路径为空"));
                    else
                    {
                        string[] fields = sPatorlPathID.Split('|');
                        if (fields.Length <= 0)
                            LogManager.WriteLog(LogTypes.Warning, string.Format("金币副本怪路径为空"));
                        else
                        {
                            for (int i = 0; i < fields.Length; i++)
                            {
                                string[] sa = fields[i].Split(',');
                                if (sa.Length != 2)
                                {
                                    LogManager.WriteLog(LogTypes.Warning, string.Format("解析{0}文件中的奖励项时失败,坐标有误", "Config/EMoLaiXiLuXian.xml"));
                                    continue;
                                }
                                int[] pos = Global.StringArray2IntArray(sa);
                                pointList.Add(pos);
                            }
                        }
                    }
                    EMoLaiXiCopySencedata.m_MonsterPatorlPathLists.Add(id, pointList);
                }

                xmlFile = Global.GetGameResXml(string.Format("Config/EMoLaiXi.xml"));
                IEnumerable<XElement> MonstersXEle = xmlFile.Elements("FuBen");
                foreach (var xmlItem in MonstersXEle)
                {
                    if (null != xmlItem)
                    {
                        EMoLaiXiCopySenceMonster tmpEMoLaiXiCopySceneMon = new EMoLaiXiCopySenceMonster();

                        tmpEMoLaiXiCopySceneMon.m_MonsterID = new List<int>();
                        int id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");

                        tmpEMoLaiXiCopySceneMon.m_ID = id;
                        tmpEMoLaiXiCopySceneMon.m_Wave = (int)Global.GetSafeAttributeLong(xmlItem, "WaveID");
                        //tmpEMoLaiXiCopySceneMon.m_Num = (int)Global.GetSafeAttributeLong(xmlItem, "Num");
                        tmpEMoLaiXiCopySceneMon.m_Delay1 = (int)Global.GetSafeAttributeLong(xmlItem, "Delay1");
                        tmpEMoLaiXiCopySceneMon.m_Delay2 = (int)Global.GetSafeAttributeLong(xmlItem, "Delay2");
                        string pathIDArray = Global.GetSafeAttributeStr(xmlItem, "PathID");;
                        tmpEMoLaiXiCopySceneMon.PathIDArray = Global.String2IntArray(pathIDArray);

                        string sMonstersID = Global.GetSafeAttributeStr(xmlItem, "MonsterList");
                        if (string.IsNullOrEmpty(sMonstersID))
                            LogManager.WriteLog(LogTypes.Warning, string.Format("金币副本怪ID为空"));
                        else
                        {
                            string[] fields = sMonstersID.Split('|');
                            if (fields.Length <= 0)
                                LogManager.WriteLog(LogTypes.Warning, string.Format("金币副本怪ID为空"));
                            else
                            {
                                for (int i = 0; i < fields.Length; i++)
                                {
                                    int[] Monsters = Global.String2IntArray(fields[i]);
                                    if (null != Monsters && Monsters.Length == 2 && Monsters[0] > 0 && Monsters[1] > 0)
                                    {
                                        for (int j = 0; j < Monsters[1]; j++ )
                                        {
                                            tmpEMoLaiXiCopySceneMon.m_MonsterID.Add(Monsters[0]);
                                            tmpEMoLaiXiCopySceneMon.m_Num++;
                                        }
                                    }
                                }
                            }
                        }
                        EMoLaiXiCopySencedata.EMoLaiXiCopySenceMonsterData.Add(tmpEMoLaiXiCopySceneMon);
                        totalWave = Global.GMax(totalWave, tmpEMoLaiXiCopySceneMon.m_Wave);
                    }
                }
                EMoLaiXiCopySencedata.TotalWave = totalWave;
            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", string.Format("Config/JinBiFuBen.xml")));
            }

        }

        #endregion 初始化配置

        #region 副本和场景管理

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public static void AddEMoLaiXiCopySceneList(int nID, CopyMap mapInfo)
        {
            bool bInsert = false;

            lock (m_EMoLaiXiCopySceneLists)
            {
                CopyMap tmp = null;

                if (!m_EMoLaiXiCopySceneLists.TryGetValue(nID, out tmp))
                {
                    m_EMoLaiXiCopySceneLists.Add(nID, mapInfo);
                    bInsert = true;
                }
                else
                {
                    if (tmp == null)
                    {
                        m_EMoLaiXiCopySceneLists[nID] = mapInfo;
                        bInsert = true;
                    }
                }

                lock (m_EMoLaiXiCopySceneInfo)
                {
                    if (bInsert == true)
                    {
                        EMoLaiXiCopySence eMoLaiXiCopySenceInfo = null;

                        if (!m_EMoLaiXiCopySceneInfo.TryGetValue(nID, out eMoLaiXiCopySenceInfo))
                        {
                            eMoLaiXiCopySenceInfo = new EMoLaiXiCopySence();

                            eMoLaiXiCopySenceInfo.InitInfo(mapInfo.MapCode, mapInfo.CopyMapID, nID);

                            eMoLaiXiCopySenceInfo.m_StartTimer = TimeUtil.NOW();

                            m_EMoLaiXiCopySceneInfo.Add(nID, eMoLaiXiCopySenceInfo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public static void RemoveEMoLaiXiCopySceneList(int nID, int copyMapID)
        {
            lock (m_EMoLaiXiCopySceneLists)
            {
                m_EMoLaiXiCopySceneLists.Remove(nID);
            }

            lock (m_EMoLaiXiCopySceneInfo)
            {
                m_EMoLaiXiCopySceneInfo.Remove(nID);
            }

            lock (m_EMoLaiXiEscapeMonsterNumDict)
            {
                m_EMoLaiXiEscapeMonsterNumDict.Remove(copyMapID);
            }
        }

        #endregion 副本和场景管理

        #region 逻辑处理

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long LastHeartBeatTicks = 0L;

        /// <summary>
        /// 心跳处理
        /// </summary>
        public static void HeartBeatEMoLaiXiCopyScene()
        {
            long nowTicks = TimeUtil.NOW();
            if (nowTicks - LastHeartBeatTicks < TimeUtil.SECOND)
            {
                return;
            }

            LastHeartBeatTicks = LastHeartBeatTicks < TimeUtil.DAY ? nowTicks : LastHeartBeatTicks + 1000;

            // lock住！！！
            lock (m_EMoLaiXiCopySceneLists)
            {
                foreach (var item in m_EMoLaiXiCopySceneLists.Values)
                {
                    EMoLaiXiCopySence scene = null;

                    lock (m_EMoLaiXiCopySceneInfo)
                    {
                        if (!m_EMoLaiXiCopySceneInfo.TryGetValue(item.FuBenSeqID, out scene))
                        {
                            continue;
                        }

                        //tmpEMoLaiXiCopySenceData = m_EMoLaiXiCopySceneInfo[item.FuBenSeqID];
                    }

                    if (scene == null)
                        continue;

                    List<GameClient> clientsList = item.GetClientsList();
                    lock (scene)
                    {
                        if (scene.m_TimeNotifyFlag == 0)
                        {
                            //准备刷怪倒计时
                            if (nowTicks <= (scene.m_StartTimer + m_PrepareTime - TimeUtil.SECOND * (int)CountDownWindowTypes.ConstMaxNumber))
                            {
                                continue;
                            }
                            scene.m_TimeNotifyFlag = 1;

                            //格式: (roleID):窗口类型:参数1秒数:参数2类型:参数3文本
                            string msgCmd = string.Format("{0}:{1}${2}${3}", (int)ServerNotifyOpenWindowTypes.CountDownWindow, (int)CountDownWindowTypes.ConstMaxNumber, (int)CountDownWindowTypes.NumberOnly, "");
                            GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_NOTIFYOPENWINDOW, msgCmd, clientsList, true);
                        }

                        if (nowTicks >= (scene.m_StartTimer + m_PrepareTime))
                        {
                            if (scene.m_Delay2 > 0)
                            {
                                //战斗时间
                                OnSceneTimer(scene, clientsList, item, nowTicks);
                            }
                            else
                            {
                                InitNextWaveMonsterList(scene);
                            }
                        }
                    }

                }
            }

        }

        /// <summary>
        /// 处理刷怪,失败等活动逻辑
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="clientList"></param>
        /// <param name="copyMap"></param>
        /// <param name="nowTicks"></param>
        public static void OnSceneTimer(EMoLaiXiCopySence scene, List<GameClient> clientList, CopyMap copyMap, long nowTicks)
        {
            int nWave = scene.m_CreateMonsterWave;
            int nCount = EMoLaiXiCopySencedata.TotalWave;

            bool notifyWaveAndNum = false;
            bool notifyEnd = false;
            int escapeCount = GetEscapeCount(scene.m_CopyMapID);
            if (escapeCount > 0)
            {
                scene.m_EscapedMonsterNum += escapeCount;
                notifyWaveAndNum = true;
            }

            if (scene.m_LoginEnterFlag == 1)
            {
                if (nowTicks - scene.m_LoginEnterTimer > m_DelayTime)
                {
                    scene.m_LoginEnterFlag = 0;
                    notifyWaveAndNum = true;
                }
            }

            if (scene.m_EscapedMonsterNum >= EMoLaiXiCopySencedata.FaildEscapeMonsterNum)
            {
                if (!scene.m_bFinished)
                {
                    GameManager.CopyMapMgr.CopyMapFaildForAll(clientList, copyMap);
                    scene.m_bFinished = true;
                }
                GameManager.CopyMapMgr.KillAllMonster(copyMap);
                notifyWaveAndNum = true;
            }
            else if (scene.m_bAllMonsterCreated)
            {
                if (!scene.m_bFinished)
                {
                    if (copyMap.KilledDynamicMonsterNum + scene.m_EscapedMonsterNum >= scene.m_TotalMonsterCountAllWave)
                    {
                        if (null != clientList && clientList.Count > 0)
                        {
                            notifyWaveAndNum = true;
                            GameManager.CopyMapMgr.CopyMapPassAwardForAll(clientList[0], copyMap, true);
                            scene.m_bFinished = true;

                            if (copyMap.KilledDynamicMonsterNum > copyMap.TotalDynamicMonsterNum)
                            {
                                try
                                {
                                    /**/string log = string.Format("恶魔来袭已成功,但杀怪计数异常,结束时间{0},KilledDynamicMonsterNum:{1},m_EscapedMonsterNum:{2},m_TotalMonsterCountAllWave:{3}",
                                                                new DateTime(nowTicks * 10000), copyMap.KilledDynamicMonsterNum, scene.m_EscapedMonsterNum, scene.m_TotalMonsterCountAllWave);
                                    LogManager.WriteLog(LogTypes.Error, log);
                                } catch { }
                            }
                        }
                    }
                }
            }
            else
            {
                // 延迟间隔判断
                if (nowTicks - scene.m_CreateMonsterTick2 > scene.m_Delay2 * 1000)       // 大波间隔时间判断
                {
                    if (scene.m_CreateMonsterWaveNotify == 0)
                    {
                        scene.m_CreateMonsterWaveNotify = 1;
                        notifyWaveAndNum = true;
                    }

                    //刷怪
                    for (int i = 0; i < scene.m_CreateWaveMonsterList.Count; i++)
                    {
                        EMoLaiXiCopySenceMonster tmpInfo = scene.m_CreateWaveMonsterList[i];

                        if (tmpInfo.m_CreateMonsterCount < tmpInfo.m_Num)
                        {
                            if (nowTicks - tmpInfo.m_CreateMonsterTick1 > tmpInfo.m_Delay1 * 1000)   // 小波间隔时间判断
                            {
                                // 怪在列表中的索引
                                int nIndex = tmpInfo.m_CreateMonsterCount;

                                // 在起点刷怪
                                int[] pos = tmpInfo.PatrolPath[0];
                                GameManager.MonsterZoneMgr.AddDynamicMonsters(scene.m_MapCodeID, tmpInfo.m_MonsterID[nIndex], scene.m_CopyMapID, 1, pos[0], pos[1], 0, 0, SceneUIClasses.EMoLaiXiCopy, tmpInfo.PatrolPath);

                                tmpInfo.m_CreateMonsterCount++;
                                scene.m_CreateMonsterCount++;

                                tmpInfo.m_CreateMonsterTick1 = nowTicks; // 小波刷怪时间设定
                            }
                        }
                    }

                    // 第N大波刷完了
                    if (scene.m_CreateMonsterCount >= scene.m_TotalMonsterCount)
                    {
                        scene.m_CreateMonsterTick2 = nowTicks; // 设定大波刷完时间
                        scene.m_CreateMonsterWave++;
                        scene.m_CreateMonsterCount = 0;
                        scene.m_CreateMonsterWaveNotify = 0;
                        scene.m_Delay2 = 0;
                        notifyWaveAndNum = true;
                        copyMap.TotalDynamicMonsterNum = scene.m_TotalMonsterCountAllWave;
                        if (scene.m_CreateMonsterWave >= EMoLaiXiCopySencedata.TotalWave)
                        {
                            scene.m_Delay2 = int.MaxValue;
                            scene.m_bAllMonsterCreated = true;
                            notifyEnd = true;
                        }
                    }
                }
            }

            if (notifyWaveAndNum)
            {
                SendMsgToClientForEMoLaiXiCopySceneMonsterWave(clientList, scene.m_EscapedMonsterNum, scene.m_CreateMonsterWave, EMoLaiXiCopySencedata.TotalWave, EMoLaiXiCopySencedata.FaildEscapeMonsterNum);
            }
            if (notifyEnd && null != clientList)
            {
                foreach (var client in clientList)
                {
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                        StringUtil.substitute(Global.GetLang("恶魔来袭副本 刷怪结束了")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                }
            }
        }

        /// <summary>
        /// 大波开始刷怪前,挑选路线,初始化刷怪列表,计算本波的最大大波间隔
        /// </summary>
        /// <param name="scene"></param>
        public static void InitNextWaveMonsterList(EMoLaiXiCopySence scene)
        {
            if (scene.m_CreateMonsterWave >= 0 && scene.m_CreateMonsterWave < EMoLaiXiCopySencedata.TotalWave)
            {
                int delay2 = 1;
                int totalNum = 0;

                scene.m_CreateWaveMonsterList.Clear();
                foreach (var m in EMoLaiXiCopySencedata.EMoLaiXiCopySenceMonsterData)
                {
                    if (m.m_Wave == scene.m_CreateMonsterWave + 1)
                    {
                        EMoLaiXiCopySenceMonster em = m.CloneMini();
                        scene.m_CreateWaveMonsterList.Add(em);

                        int random = Global.GetRandomNumber(0, em.PathIDArray.Length);
                        int pathID = em.PathIDArray[random];
                        em.PatrolPath = EMoLaiXiCopySencedata.m_MonsterPatorlPathLists[pathID];
                        delay2 = Global.GMax(delay2, em.m_Delay2);
                        totalNum += em.m_Num;
                    }
                }

                scene.m_Delay2 = delay2;
                scene.m_TotalMonsterCount = totalNum;
                scene.m_TotalMonsterCountAllWave += totalNum;
            }
        }

        /// <summary>
        /// 逃走的怪物计数
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMapID"></param>
        public static void IncreaceEscapeCount(int copyMapID)
        {
            int count;
            lock (m_EMoLaiXiEscapeMonsterNumDict)
            {
                if (m_EMoLaiXiEscapeMonsterNumDict.TryGetValue(copyMapID, out count))
                {
                    m_EMoLaiXiEscapeMonsterNumDict[copyMapID] = count + 1;
                }
                else
                {
                    m_EMoLaiXiEscapeMonsterNumDict[copyMapID] = 1;
                }
            }
        }

        /// <summary>
        /// 获取自上次调用后,一个副本新增逃走的怪物数
        /// </summary>
        /// <param name="fubenSeqID"></param>
        /// <returns></returns>
        public static int GetEscapeCount(int copyMapID)
        {
            int count;
            lock (m_EMoLaiXiEscapeMonsterNumDict)
            {
                if (m_EMoLaiXiEscapeMonsterNumDict.TryGetValue(copyMapID, out count))
                {
                    m_EMoLaiXiEscapeMonsterNumDict[copyMapID] = 0;
                }
                else
                {
                    count = 0;
                }
            }
            return count;
        }

        /// <summary>
        /// 处理恶魔来袭行走
        /// </summary>
        public static void MonsterMoveStepEMoLaiXiCopySenceCopyMap(Monster monster)
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
                IncreaceEscapeCount(monster.CopyMapID); // 统计逃走的怪物数
                GameManager.MonsterMgr.DeadMonsterImmediately(monster); // 让怪物消失
                return;
            }

            // 取得目标坐标的格子信息
            int nMapCode = (int)monster.CurrentMapCode;
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[nMapCode];

            int nNextX = monster.PatrolPath[nNextStep][0]; // 目标路径点的x
            int nNextY = monster.PatrolPath[nNextStep][1]; // 目标路径点的y

            // 目标格子点
            int gridX = nNextX / mapGrid.MapGridWidth;
            int gridY = nNextY / mapGrid.MapGridHeight;
            Point ToGrid = new Point(gridX, gridY);

            // 怪物当前点
            Point grid = monster.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            // 取得和目标坐标的方向值
            double Direction = Global.GetDirectionByAspect(gridX, gridY, nCurrX, nCurrY);

            // 行走
            if (ChuanQiUtils.WalkTo(monster, (Dircetions)Direction) ||
                ChuanQiUtils.WalkTo(monster, (Dircetions)((Direction + 7) % 8)) ||
                ChuanQiUtils.WalkTo(monster, (Dircetions)((Direction + 9) % 8)) ||
                ChuanQiUtils.WalkTo(monster, (Dircetions)((Direction + 6) % 8)) ||
                ChuanQiUtils.WalkTo(monster, (Dircetions)((Direction + 10) % 8)) ||
                ChuanQiUtils.WalkTo(monster, (Dircetions)((Direction + 5) % 8)) ||
                ChuanQiUtils.WalkTo(monster, (Dircetions)((Direction + 11) % 8)))
            {
                monster.MoveTime = ticks;
            }

            // 允许误差
            if (Global.GetTwoPointDistance(ToGrid, grid) < 2)
                monster.Step = nStep + 1;
        }

        /// <summary>
        /// 通知客户端怪波数信息
        /// </summary>
        public static void SendMsgToClientForEMoLaiXiCopySceneMonsterWave(List<GameClient> clientList, int escapeNum, int nWave, int totalWave, int faildEscapeNum)
        {
            if (null != clientList && clientList.Count > 0)
            {
                string strcmd = string.Format("{0}:{1}:{2}:{3}", escapeNum, nWave, EMoLaiXiCopySencedata.TotalWave, faildEscapeNum);//逃走怪物累积数:当前波数:总波数: 失败所需怪物数量
                GameManager.ClientMgr.BroadSpecialCopyMapMessage((int)TCPGameServerCmds.CMD_SPR_EMOLAIXIMONSTERINFO, strcmd, clientList);
            }
        }

        /// <summary>
        // 玩家登陆后进入金币副本
        /// </summary>
        public static bool EnterEMoLaiXiCopySenceWhenLogin(GameClient client, bool bContinue = true)
        {
            if (client != null)
            {
                CopyMap tmp = null;
                EMoLaiXiCopySence EMoLaiXiCopySenceInfo = null;

                lock (m_EMoLaiXiCopySceneLists)
                {
                    if (!m_EMoLaiXiCopySceneLists.TryGetValue(client.ClientData.FuBenSeqID, out tmp) || tmp == null)
                    {
                        return false;
                    }
                }

                lock (m_EMoLaiXiCopySceneInfo)
                {
                    if (!m_EMoLaiXiCopySceneInfo.TryGetValue(client.ClientData.FuBenSeqID, out EMoLaiXiCopySenceInfo) || EMoLaiXiCopySenceInfo == null)
                    {
                        return false;
                    }
                }

                if (EMoLaiXiCopySenceInfo.m_bFinished)
                {
                    return false;
                }

                long ticks = TimeUtil.NOW();    // 当前tick
                EMoLaiXiCopySenceInfo.m_LoginEnterTimer = ticks;
                EMoLaiXiCopySenceInfo.m_LoginEnterFlag = 1;

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
                        string strcmd = string.Format("{0}:{1}", EMoLaiXiCopySenceInfo.m_CreateMonsterWave, Data.EMoLaiXiCopySencedata.EMoLaiXiCopySenceMonsterData.Count());//1.当前的波数 2.总波数
                        //TCPOutPacket tcpOutPacket = null;
                        //tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, (int)TCPGameServerCmds.CMD_SPR_EMoLaiXiCopySenceMONSTERWAVE);
                        //Global._TCPManager.MySocketListener.SendData(clientsList[0].ClientSocket, tcpOutPacket);
                        GameManager.ClientMgr.SendToClient(clientsList[0], strcmd, (int)TCPGameServerCmds.CMD_SPR_EMoLaiXiCopySenceMONSTERWAVE);
                    }
                }*/

                return true;
            }

            return false;

        }

        #endregion 逻辑处理
    }
}
