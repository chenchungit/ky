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
using GameServer.Core.Executor;
namespace GameServer.Logic
{
    // 新手场景管理类 [12/1/2013 LiaoWei]
    class FreshPlayerCopySceneManager
    {
        /// <summary>
        /// 副本场景LIST
        /// </summary>
        //public static List<CopyMap> m_FreshPlayerListCopyMaps = new List<CopyMap>();
        public static Dictionary<int, CopyMap> m_FreshPlayerListCopyMaps = new Dictionary<int, CopyMap>(); // key: 副本流水ID 即DB生成  Value: CopyMap信息  

        /// <summary>
        /// 添加一个场景
        /// </summary>
        public static void AddFreshPlayerListCopyMap(int nID, CopyMap mapInfo)
        {
            lock (m_FreshPlayerListCopyMaps)
            {
                m_FreshPlayerListCopyMaps.Add(nID, mapInfo);
            }
        }

        /// <summary>
        /// 删除一个场景
        /// </summary>
        public static void RemoveFreshPlayerListCopyMap(int nID, CopyMap mapInfo)
        {
            lock (m_FreshPlayerListCopyMaps)
            {
                m_FreshPlayerListCopyMaps.Remove(nID);
            }
        }

        /// <summary>
        /// 上次心跳的时间
        /// </summary>
        private static long LastHeartBeatTicks = 0L;

        /// <summary>
        // 心跳处理
        /// </summary>
        public static void HeartBeatFreshPlayerCopyMap()
        {
            long nowTicks = TimeUtil.NOW();
            if (nowTicks - LastHeartBeatTicks < (1000))
            {
                return;
            }

            LastHeartBeatTicks = nowTicks;

            // 没有玩家了 清空场景
            List<CopyMap> CopyMapList = new List<CopyMap>();
            
            // lock住！！
            lock (m_FreshPlayerListCopyMaps)
            {
                CopyMap copyMap = null;
                foreach (var item in m_FreshPlayerListCopyMaps.Values)
                {
                    copyMap = item;

                    // 刷出城门怪
                    if (item.FreshPlayerCreateGateFlag == 0)
                        CreateGateMonster(item);

                    List<GameClient> clientsList = item.GetClientsList();
                    if (null != clientsList && clientsList.Count <= 0)
                        CopyMapList.Add(item);
                }

                if (null != copyMap)
                {
                    // 加载自动复活的怪物
                    GameManager.MonsterZoneMgr.ReloadCopyMapMonsters(copyMap.MapCode, -1);
                }
            }


            for (int i = 0; i < CopyMapList.Count; ++i)
            {
                GameManager.CopyMapMgr.ProcessRemoveCopyMap(CopyMapList[i]);
            }
        }

        /// <summary>
        // 杀死了怪
        /// </summary>
        public static void KillMonsterInFreshPlayerScene(GameClient client, Monster monster)
        {
            CopyMap copyMapInfo;
            lock (m_FreshPlayerListCopyMaps)
            {
                if(!m_FreshPlayerListCopyMaps.TryGetValue(client.ClientData.FuBenSeqID, out copyMapInfo) || copyMapInfo == null)
                    return;
            }


#if ___CC___FUCK___YOU___BB___
            if (monster.XMonsterInfo.Level >= Data.FreshPlayerSceneInfo.NeedKillMonster1Level)
            {
                ++copyMapInfo.FreshPlayerKillMonsterACount;

                if (copyMapInfo.FreshPlayerKillMonsterACount >= Data.FreshPlayerSceneInfo.NeedKillMonster1Num)
                {
                    // 杀死A怪的数量已经达到限额 通知客户端 面前的阻挡消失 玩家可以离开桥 攻击城门了
                    string strcmd = string.Format("{0}", client.ClientData.RoleID);
                    GameManager.ClientMgr.SendToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, strcmd,
                                                            (int)TCPGameServerCmds.CMD_SPR_FRESHPLAYERSCENEKILLMONSTERAHASDONE);

                }
            }
            if (monster.XMonsterInfo.MonsterId == Data.FreshPlayerSceneInfo.NeedKillMonster2ID)
            {
                ++copyMapInfo.FreshPlayerKillMonsterBCount;

                if (copyMapInfo.FreshPlayerKillMonsterBCount >= Data.FreshPlayerSceneInfo.NeedKillMonster2Num)
                {
                    bool canAddMonster = false;
			        TaskData taskData = Global.GetTaskData(client, 105); //先写死吧，临时i解决掉
			        if (null != taskData)
                    {
                        canAddMonster = true;
			        }

                    if (canAddMonster) //是否能刷水晶棺的怪物
                    {
                        copyMapInfo.HaveBirthShuiJingGuan = true;

                        // 把水晶棺刷出来
                        int monsterID = Data.FreshPlayerSceneInfo.CrystalID;
                        string[] sfields = Data.FreshPlayerSceneInfo.CrystalPos.Split(',');

                        int nPosX = Global.SafeConvertToInt32(sfields[0]);
                        int nPosY = Global.SafeConvertToInt32(sfields[1]);

                        GameMap gameMap = null;
                        if (!GameManager.MapMgr.DictMaps.TryGetValue(copyMapInfo.MapCode, out gameMap))
                        {
                            return;
                        }

                        int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                        int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                        GameManager.MonsterZoneMgr.AddDynamicMonsters(copyMapInfo.MapCode, monsterID, copyMapInfo.CopyMapID, 1, gridX, gridY, 0);
                    }
                }
            }



            // 如果杀死的是城门 刷巫师
            if (monster.XMonsterInfo.MonsterId == Data.FreshPlayerSceneInfo.GateID)
            {
                CreateMonsterBFreshPlayerScene(copyMapInfo);
            }
            // 刷雕像
            if (monster.XMonsterInfo.MonsterId == Data.FreshPlayerSceneInfo.CrystalID)
            {
                int monsterID = Data.FreshPlayerSceneInfo.DiaoXiangID;
                string[] sfields = Data.FreshPlayerSceneInfo.DiaoXiangPos.Split(',');

                int nPosX = Global.SafeConvertToInt32(sfields[0]);
                int nPosY = Global.SafeConvertToInt32(sfields[1]);

                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(copyMapInfo.MapCode, out gameMap))
                {
                    return;
                }

                int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                GameManager.MonsterZoneMgr.AddDynamicMonsters(copyMapInfo.MapCode, monsterID, copyMapInfo.CopyMapID, 1, gridX, gridY, 0);
            }
#else
             if (monster.MonsterInfo.VLevel >= Data.FreshPlayerSceneInfo.NeedKillMonster1Level)
            {
                ++copyMapInfo.FreshPlayerKillMonsterACount;

                if (copyMapInfo.FreshPlayerKillMonsterACount >= Data.FreshPlayerSceneInfo.NeedKillMonster1Num)
                {
                    // 杀死A怪的数量已经达到限额 通知客户端 面前的阻挡消失 玩家可以离开桥 攻击城门了
                    string strcmd = string.Format("{0}", client.ClientData.RoleID);
                    GameManager.ClientMgr.SendToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,client, strcmd,
                                                            (int)TCPGameServerCmds.CMD_SPR_FRESHPLAYERSCENEKILLMONSTERAHASDONE);

                }
            }
            if (monster.MonsterInfo.ExtensionID == Data.FreshPlayerSceneInfo.NeedKillMonster2ID)
            {
                ++copyMapInfo.FreshPlayerKillMonsterBCount;

                if (copyMapInfo.FreshPlayerKillMonsterBCount >= Data.FreshPlayerSceneInfo.NeedKillMonster2Num)
                {
                    bool canAddMonster = false;
			        TaskData taskData = Global.GetTaskData(client, 105); //先写死吧，临时i解决掉
			        if (null != taskData)
                    {
                        canAddMonster = true;
			        }

                    if (canAddMonster) //是否能刷水晶棺的怪物
                    {
                        copyMapInfo.HaveBirthShuiJingGuan = true;

                        // 把水晶棺刷出来
                        int monsterID = Data.FreshPlayerSceneInfo.CrystalID;
                        string[] sfields = Data.FreshPlayerSceneInfo.CrystalPos.Split(',');

                        int nPosX = Global.SafeConvertToInt32(sfields[0]);
                        int nPosY = Global.SafeConvertToInt32(sfields[1]);

                        GameMap gameMap = null;
                        if (!GameManager.MapMgr.DictMaps.TryGetValue(copyMapInfo.MapCode, out gameMap))
                        {
                            return;
                        }

                        int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                        int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                        GameManager.MonsterZoneMgr.AddDynamicMonsters(copyMapInfo.MapCode, monsterID, copyMapInfo.CopyMapID, 1, gridX, gridY, 0);
                    }
                }
            }
             // 如果杀死的是城门 刷巫师
            if (monster.MonsterInfo.ExtensionID == Data.FreshPlayerSceneInfo.GateID)
            {
                CreateMonsterBFreshPlayerScene(copyMapInfo);
            }
              // 刷雕像
            if (monster.MonsterInfo.ExtensionID == Data.FreshPlayerSceneInfo.CrystalID)
            {
                int monsterID = Data.FreshPlayerSceneInfo.DiaoXiangID;
                string[] sfields = Data.FreshPlayerSceneInfo.DiaoXiangPos.Split(',');

                int nPosX = Global.SafeConvertToInt32(sfields[0]);
                int nPosY = Global.SafeConvertToInt32(sfields[1]);

                GameMap gameMap = null;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(copyMapInfo.MapCode, out gameMap))
                {
                    return;
                }

                int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                GameManager.MonsterZoneMgr.AddDynamicMonsters(copyMapInfo.MapCode, monsterID, copyMapInfo.CopyMapID, 1, gridX, gridY, 0);
            }
#endif

            return;
        }

        /// <summary>
        /// 补充刷新水晶棺材
        /// </summary>
        /// <param name="client"></param>
        public static void AddShuiJingGuanCaiMonsters(GameClient client)
        {
            CopyMap copyMapInfo = null;
            lock (m_FreshPlayerListCopyMaps)
            {
                if (!m_FreshPlayerListCopyMaps.TryGetValue(client.ClientData.FuBenSeqID, out copyMapInfo) || copyMapInfo == null)
                    return;
            }

            if (copyMapInfo.HaveBirthShuiJingGuan)
            {
                return;
            }

            if (copyMapInfo.FreshPlayerKillMonsterBCount >= Data.FreshPlayerSceneInfo.NeedKillMonster2Num)
            {
                bool canAddMonster = true;
                if (canAddMonster) //是否能刷水晶棺的怪物
                {
                    copyMapInfo.HaveBirthShuiJingGuan = true;

                    // 把水晶棺刷出来
                    int monsterID = Data.FreshPlayerSceneInfo.CrystalID;
                    string[] sfields = Data.FreshPlayerSceneInfo.CrystalPos.Split(',');

                    int nPosX = Global.SafeConvertToInt32(sfields[0]);
                    int nPosY = Global.SafeConvertToInt32(sfields[1]);

                    GameMap gameMap = null;
                    if (!GameManager.MapMgr.DictMaps.TryGetValue(copyMapInfo.MapCode, out gameMap))
                    {
                        return;
                    }

                    int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
                    int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

                    GameManager.MonsterZoneMgr.AddDynamicMonsters(copyMapInfo.MapCode, monsterID, copyMapInfo.CopyMapID, 1, gridX, gridY, 0);
                }
            }
        }

        /// <summary>
        // 刷B怪
        /// </summary>
        public static void CreateMonsterBFreshPlayerScene(CopyMap copyMapInfo)
        {
            int monsterID = Data.FreshPlayerSceneInfo.NeedKillMonster2ID;
            string[] sfields = Data.FreshPlayerSceneInfo.NeedCreateMonster2Pos.Split(',');

            int nPosX = Global.SafeConvertToInt32(sfields[0]);
            int nPosY = Global.SafeConvertToInt32(sfields[1]);

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(copyMapInfo.MapCode, out gameMap))
            {
                return;
            }

            int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
            int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

            int gridNum = gameMap.CorrectPointToGrid(Data.FreshPlayerSceneInfo.NeedCreateMonster2Radius);

            for (int i = 0; i < Data.FreshPlayerSceneInfo.NeedCreateMonster2Num; ++i)
            {
                GameManager.MonsterZoneMgr.AddDynamicMonsters(copyMapInfo.MapCode, monsterID, copyMapInfo.CopyMapID, 1, gridX, gridY, gridNum, Data.FreshPlayerSceneInfo.NeedCreateMonster2PursuitRadius);
            }

            return;
        }

        /// <summary>
        // 刷城门
        /// </summary>
        public static void CreateGateMonster(CopyMap copyMapInfo)
        {
            int monsterID = Data.FreshPlayerSceneInfo.GateID;
            string[] sfields = Data.FreshPlayerSceneInfo.GatePos.Split(',');

            int nPosX = Global.SafeConvertToInt32(sfields[0]);
            int nPosY = Global.SafeConvertToInt32(sfields[1]);

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(copyMapInfo.MapCode, out gameMap))
            {
                return;
            }

            int gridX = gameMap.CorrectWidthPointToGridPoint(nPosX) / gameMap.MapGridWidth;
            int gridY = gameMap.CorrectHeightPointToGridPoint(nPosY) / gameMap.MapGridHeight;

            GameManager.MonsterZoneMgr.AddDynamicMonsters(copyMapInfo.MapCode, monsterID, copyMapInfo.CopyMapID, 1, gridX, gridY, 0);

            copyMapInfo.FreshPlayerCreateGateFlag = 1;

            return;
        }
    }
}
