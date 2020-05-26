#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System.Windows;
//using System.Windows.Documents;
using GameServer.Server;
using System.Threading;

using GameServer.Logic.WanMota;
using GameServer.Core.Executor;
using GameServer.Logic.Copy;
using Tmsk.Contract;
using GameServer.Logic.MoRi;
using GameServer.Logic.Marriage.CoupleArena;

namespace GameServer.Logic
{
    /// <summary>
    /// 副本地图管理类
    /// </summary>
    public class CopyMapManager
    {
        #region 副本地图号管理

        /// <summary>
        /// 基础的副本地图号
        /// </summary>
        int BaseCopyMapID = 1;

        /// <summary>
        /// 获取一个新的副本地图号ID
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public int GetNewCopyMapID()
        {
            int id = 1;
            lock (this)
            {
                id = BaseCopyMapID++;
            }

            return id;
        }

        #endregion 副本地图号管理

        #region 角色的副本顺序ID到副本地图的映射

        /// <summary>
        /// 角色的副本顺序ID + 地图号 => 副本地图的映射
        /// </summary>
        private Dictionary<string, int> FuBenSeqID2CopyIDDict = new Dictionary<string, int>();

        /// <summary>
        /// 添加角色的副本地图编号
        /// </summary>
        /// <param name="fuBenSeqID"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        private void AddCopyID(int fuBenSeqID, int mapCode, int copyMapID)
        {
            string key = string.Format("{0}_{1}", fuBenSeqID, mapCode);
            lock (FuBenSeqID2CopyIDDict)
            {
                FuBenSeqID2CopyIDDict[key] = copyMapID;
            }
        }

        /// <summary>
        /// 删除角色的副本地图编号
        /// </summary>
        /// <param name="fuBenSeqID"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public void RemoveCopyID(int fuBenSeqID, int mapCode)
        {
            string key = string.Format("{0}_{1}", fuBenSeqID, mapCode);
            lock (FuBenSeqID2CopyIDDict)
            {
                FuBenSeqID2CopyIDDict.Remove(key);
            }
        }

        /// <summary>
        /// 查找角色的副本地图编号
        /// </summary>
        /// <param name="fuBenSeqID"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public int FindCopyID(int fuBenSeqID, int mapCode)
        {
            int copyMapID = -1;
            string key = string.Format("{0}_{1}", fuBenSeqID, mapCode);
            lock (FuBenSeqID2CopyIDDict)
            {
                if (!FuBenSeqID2CopyIDDict.TryGetValue(key, out copyMapID))
                {
                    copyMapID = -1;
                }
            }

            return copyMapID;
        }

        #endregion 角色到副本地图的映射

        #region 角色的副本顺序ID对应地图编号的怪物杀光状态

        /// <summary>
        /// 角色的副本顺序ID + 地图号 => 怪物杀光状态
        /// </summary>
        private Dictionary<string, int> FuBenSeqID2MonsterStateDict = new Dictionary<string, int>();

        /// <summary>
        /// 添加角色的副本地图编号的怪物杀光状态
        /// </summary>
        /// <param name="fuBenSeqID"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        private void AddMonsterState(int fuBenSeqID, int mapCode, int monsterState)
        {
            string key = string.Format("{0}_{1}", fuBenSeqID, mapCode);
            lock (FuBenSeqID2MonsterStateDict)
            {
                FuBenSeqID2MonsterStateDict[key] = monsterState;
            }
        }

        /// <summary>
        /// 查找角色的副本地图编号的怪物杀光状态
        /// </summary>
        /// <param name="fuBenSeqID"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        private int FindMonsterState(int fuBenSeqID, int mapCode)
        {
            int monsterState = 0;
            string key = string.Format("{0}_{1}", fuBenSeqID, mapCode);
            lock (FuBenSeqID2MonsterStateDict)
            {
                if (!FuBenSeqID2MonsterStateDict.TryGetValue(key, out monsterState))
                {
                    monsterState = 0;
                }
            }

            return monsterState;
        }

        #endregion 角色的副本顺序ID对应地图编号的怪物杀光状态

        #region 角色的ID+副本顺序ID对应地图编号的奖励领取

        /// <summary>
        /// 角色的副本顺序ID + 地图号 => 怪物杀光状态
        /// </summary>
        private Dictionary<string, int> RoleIDFuBenSeqID2AwardStateDict = new Dictionary<string, int>();

        /// <summary>
        /// 添加角色的ID+副本顺序ID对应地图编号的奖励领取状态
        /// </summary>
        /// <param name="fuBenSeqID"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public void AddAwardState(int roleID, int fuBenSeqID, int mapCode, int awardState)
        {
            string key = string.Format("{0}_{1}_{2}", roleID, fuBenSeqID, mapCode);
            lock (RoleIDFuBenSeqID2AwardStateDict)
            {
                RoleIDFuBenSeqID2AwardStateDict[key] = awardState;
            }
        }

        /// <summary>
        /// 查找角色的ID+副本顺序ID对应地图编号的奖励领取状态
        /// </summary>
        /// <param name="fuBenSeqID"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public int FindAwardState(int roleID, int fuBenSeqID, int mapCode)
        {
            int awardState = 0;
            string key = string.Format("{0}_{1}_{2}", roleID, fuBenSeqID, mapCode);
            lock (RoleIDFuBenSeqID2AwardStateDict)
            {
                if (!RoleIDFuBenSeqID2AwardStateDict.TryGetValue(key, out awardState))
                {
                    awardState = 0;
                }
            }

            return awardState;
        }

        #endregion 角色的ID+副本顺序ID对应地图编号的奖励领取

        #region 获取副本地图对象

        /// <summary>
        /// 为制定的角色查找副本地图对象
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public CopyMap FindCopyMap(int mapCode, int fuBenSeqID)
        {
            int copyMapID = -1;
            CopyMap copyMap = null;

            //先查找有没有以前的副本地图存在
            copyMapID = FindCopyID(fuBenSeqID, mapCode);
            if (copyMapID > 0)
            {
                copyMap = FindCopyMap(copyMapID);
            }

            return copyMap;
        }

        /// <summary>
        /// 获取副本地图对象
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public CopyMap GetCopyMap(GameClient client, MapTypes mapType)
        {
            CopyMap copyMap = null;
            int totalMonsterNum = GameManager.MonsterZoneMgr.GetMapTotalMonsterNum(client.ClientData.MapCode, MonsterTypes.None);
            int totalNormalNum = GameManager.MonsterZoneMgr.GetMapTotalMonsterNum(client.ClientData.MapCode, MonsterTypes.Noraml);
            int totalBossNum = totalMonsterNum - totalNormalNum;
            SceneUIClasses sceneType = Global.GetMapSceneType(client.ClientData.MapCode);

            //锁定，防止多线程重复加入
            lock (this)
            {
                int copyMapID = -1;

                //先查找有没有以前的副本地图存在
                copyMapID = FindCopyID(client.ClientData.FuBenSeqID, client.ClientData.MapCode);
                if (copyMapID > 0)
                {
                    copyMap = FindCopyMap(copyMapID);
                }

                //查找角色的副本地图编号的怪物杀光状态
                int monsterState = FindMonsterState(client.ClientData.FuBenSeqID, client.ClientData.MapCode);

                if (null == copyMap)
                {
                    copyMap = new CopyMap()
                    {
                        CopyMapID = GetNewCopyMapID(),              //GameServer 自增ID
                        FuBenSeqID = client.ClientData.FuBenSeqID,  //DBServer 自增ID
                        MapCode = client.ClientData.MapCode,        //map 静态表ID
                        FubenMapID = client.ClientData.FuBenID,     //Fuben.xml 静态表ID
                        CopyMapType = mapType,
                        IsInitMonster = monsterState > 0 ? true : false,
                        InitTicks = TimeUtil.NOW(),
                        TotalNormalNum = totalNormalNum,
                        TotalBossNum = totalBossNum,
                        bStoryCopyMapFinishStatus = false
                    };

                    // 保证FubenMapID字段 [8/20/2014 Administrator]
                    if (copyMap.FubenMapID < 0)
                    {
                        copyMap.FubenMapID = FuBenManager.FindFuBenIDByMapCode(client.ClientData.MapCode);
                        client.ClientData.FuBenID = copyMap.FubenMapID;
                    }

                    //根据副本地图类型，生成副本地图对象
                    AddCopyID(client.ClientData.FuBenSeqID, client.ClientData.MapCode, copyMap.CopyMapID);

                    //将副本对象加入管理
                    AddCopyMap(copyMap);

                    //添加到组队副本列表
                    AddTeamCopyMap(copyMap);

                    //如果副本中还没有刷怪物
                    if (!copyMap.IsInitMonster)
                    {
                        copyMap.IsInitMonster = true;

                        //通知怪物区域管理对象，开始动态的刷出副本中的怪物
                        GameManager.MonsterZoneMgr.AddCopyMapMonsters(client.ClientData.MapCode, copyMap.CopyMapID); 
                    }
                }

                //添加角色进入副本
                copyMap.AddGameClient(client);

                // 新手场景 [12/1/2013 LiaoWei]
                if (client.ClientData.MapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)
                {
                    copyMap.FreshPlayerCreateGateFlag = 0;
                    FreshPlayerCopySceneManager.AddFreshPlayerListCopyMap(client.ClientData.FuBenSeqID, copyMap);
                }
                else if (Global.IsInExperienceCopyScene(client.ClientData.MapCode))
                {
                    // 经验副本 [3/18/2014 LiaoWei]
                    //copyMap.ExperienceCopyMapCreateMonsterFlag = 0;
                    //copyMap.ExperienceCopyMapCreateMonsterWave = 0;
                    ExperienceCopySceneManager.AddExperienceListCopyMap(client.ClientData.FuBenSeqID, copyMap);
                }
                else if (client.ClientData.MapCode == (int)GoldCopySceneEnum.GOLDCOPYSCENEMAPCODEID)
                {
                    //金币副本
                    GlodCopySceneManager.AddGlodCopySceneList(client.ClientData.FuBenSeqID, copyMap);
                }
                else if (client.ClientData.MapCode == EMoLaiXiCopySceneManager.EMoLaiXiCopySceneMapCode)
                {
                    //恶魔来袭
                    EMoLaiXiCopySceneManager.AddEMoLaiXiCopySceneList(client.ClientData.FuBenSeqID, copyMap);
                }
                else if (GameManager.BloodCastleCopySceneMgr.IsBloodCastleCopyScene2(client.ClientData.MapCode))
                {
                    // 血色城堡副本 [7/7/2014 LiaoWei]
                    GameManager.BloodCastleCopySceneMgr.AddBloodCastleCopyScenes(copyMap.FuBenSeqID, copyMap.FubenMapID, client.ClientData.MapCode, copyMap);
                }
                else if (GameManager.DaimonSquareCopySceneMgr.IsDaimonSquareCopyScene2(client.ClientData.MapCode))
                {
                    // 恶魔广场副本 [7/11/2014 LiaoWei]
                    GameManager.DaimonSquareCopySceneMgr.AddDaimonSquareCopyScenes(copyMap.FuBenSeqID, copyMap.FubenMapID, client.ClientData.MapCode, copyMap);
                }
                else if (Global.IsStoryCopyMapScene(client.ClientData.MapCode))
                {
                    // 剧情副本 [7/25/2014 LiaoWei]
                    SystemXmlItem systemFuBenItem = null;
                    if (GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(copyMap.FubenMapID, out systemFuBenItem) && systemFuBenItem != null)
                    {
                        int nBossID = -1;
                        nBossID = systemFuBenItem.GetIntValue("BossID");

                        int nNum = 0;
                        nNum = GameManager.MonsterZoneMgr.GetMapMonsterNum(client.ClientData.MapCode, nBossID);

                        if (nNum == 0)
                            Global.NotifyClientStoryCopyMapInfo(copyMap.CopyMapID, 1);
                        else
                            Global.NotifyClientStoryCopyMapInfo(copyMap.CopyMapID, 2);                            
                    }
                }

                if (client.ClientSocket.IsKuaFuLogin)
                {
                    FuBenManager.AddFuBenSeqID(client.ClientData.RoleID, copyMap.FuBenSeqID, 0, copyMap.FubenMapID);
                }

                switch (sceneType)
                {
                    case SceneUIClasses.HuanYingSiYuan:
                        {
                            HuanYingSiYuanManager.getInstance().AddHuanYingSiYuanCopyScenes(client, copyMap);
                        }
                        break;
                    case SceneUIClasses.TianTi:
                        {
                            TianTiManager.getInstance().AddTianTiCopyScenes(client, copyMap, sceneType);
                        }
                        break;
                    case SceneUIClasses.YongZheZhanChang:
                        {
                            YongZheZhanChangManager.getInstance().AddCopyScenes(client, copyMap, sceneType);
                        }
                        break;
                    case SceneUIClasses.KingOfBattle:
                        {
                            KingOfBattleManager.getInstance().AddCopyScenes(client, copyMap, sceneType);
                        }
                        break;
                    case SceneUIClasses.MoRiJudge:
                        {
                            MoRiJudgeManager.Instance().AddCopyScene(client, copyMap, sceneType);
                        }
                        break;
                    case SceneUIClasses.ElementWar:
                        {
                            ElementWarManager.getInstance().AddCopyScene(client, copyMap);
                        }
                        break;
                    case SceneUIClasses.CopyWolf:
                        {
                            CopyWolfManager.getInstance().AddCopyScene(client, copyMap);
                        }
                        break;
                    case SceneUIClasses.KuaFuBoss:
                        {
                            KuaFuBossManager.getInstance().AddCopyScenes(client, copyMap, sceneType);
                        }
                        break;
                    case SceneUIClasses.LangHunLingYu:
                        {
                            LangHunLingYuManager.getInstance().AddCopyScenes(client, copyMap, sceneType);
                        }
                        break;
                    case SceneUIClasses.KFZhengBa:
                        {
                            ZhengBaManager.Instance().AddCopyScenes(client, copyMap, sceneType);
                        }
                        break;
                    case SceneUIClasses.CoupleArena:
                        {
                            CoupleArenaManager.Instance().AddCopyScenes(client, copyMap, sceneType);
                        }
                        break;
               }
            }

            return copyMap;
        }

        #endregion 获取副本地图对象

        #region 基本属性和方法

        /// <summary>
        /// 副本地图队列
        /// </summary>
        private List<CopyMap> _ListCopyMaps = new List<CopyMap>(300);

        /// <summary>
        /// 近于ID的映射对象
        /// </summary>
        private Dictionary<int, CopyMap> _DictCopyMaps = new Dictionary<int, CopyMap>(300);

        /// <summary>
        /// 添加一个新的副本地图
        /// </summary>
        /// <param name="client"></param>
        private void AddCopyMap(CopyMap copyMap)
        {
            lock (_ListCopyMaps)
            {
                _ListCopyMaps.Add(copyMap);
            }

            lock (_DictCopyMaps)
            {
                _DictCopyMaps.Add(copyMap.CopyMapID, copyMap);
            }
        }

        /// <summary>
        /// 删除一个新的副本地图
        /// </summary>
        /// <param name="client"></param>
        public void RemoveCopyMap(CopyMap copyMap)
        {
            lock (_ListCopyMaps)
            {
                _ListCopyMaps.Remove(copyMap);
            }

            lock (_DictCopyMaps)
            {
                _DictCopyMaps.Remove(copyMap.CopyMapID);
            }
        }

        /// <summary>
        /// 通过ID查找一个副本地图对象
        /// </summary>
        /// <param name="client"></param>
        public CopyMap FindCopyMap(int copyMapID)
        {
            CopyMap copyMap = null;
            lock (_DictCopyMaps)
            {
                _DictCopyMaps.TryGetValue(copyMapID, out copyMap);
            }

            return copyMap;
        }

        /// <summary>
        /// 获取下一个副本对象
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public CopyMap GetNextCopyMap(int index)
        {
            CopyMap copyMap = null;
            lock (_ListCopyMaps)
            {
                if (index < _ListCopyMaps.Count)
                {
                    copyMap = _ListCopyMaps[index];
                }
            }

            return copyMap;
        }

        /// <summary>
        /// 获取在线副本地图的个数
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetCopyMapCount()
        {
            int count = 0;
            lock (_ListCopyMaps)
            {
                count = _ListCopyMaps.Count;
            }

            return count;
        }

        #endregion 基本属性和方法

        #region 副本删除处理

        /// <summary>
        /// 副本是否超过了最大的生存期间
        /// </summary>
        /// <param name="copyMap"></param>
        /// <param name="nowTicks"></param>
        /// <returns></returns>
        private bool CopyMapOverTime(CopyMap copyMap, long nowTicks, List<GameClient> clientsList)
        {
            //根据地图编号获取副本ID
            int fuBenID = FuBenManager.FindFuBenIDByMapCode(copyMap.MapCode);
            FuBenMapItem fuBenMapItem = FuBenManager.FindMapCodeByFuBenID(fuBenID, copyMap.MapCode);
            if (null == fuBenMapItem)
            {
                return false;
            }

            FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(copyMap.FuBenSeqID);
            if (null == fuBenInfoItem)
            {

            }

            // 如果是帮会副本
            if (GameManager.GuildCopyMapMgr.IsGuildCopyMap(fuBenID))
            {
                // 已经没人了
                if (null == clientsList || 0 == clientsList.Count)
                {
                    // 如果超过了十分钟就把副本释放了
                    if ((nowTicks - copyMap.GetLastLeaveClientTicks()) >= (60 * 10 * 1000))
                    {
                        return true;
                    }
                }
            }

            if (fuBenMapItem.MaxTime <= 0) return false;

            //是否超时
            if (nowTicks - copyMap.InitTicks < (fuBenMapItem.MaxTime * 60L * 1000L))
            {
                return false;
            }

            if (null != clientsList)
            {
                int toMapCode = GameManager.MainMapCode;
                GameMap gameMap = null;
                if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                {
                    for (int i = 0; i < clientsList.Count; i++)
                    {
                        if (copyMap.MapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)  // 新手场景超时
                        {
                            int nID = (int)TCPGameServerCmds.CMD_SPR_FRESHPLAYERSCENEOVERTIME;

                            string strcmd = string.Format("{0}", clientsList[i].ClientData.RoleID);

                            TCPOutPacket tcpOutPacket = null;
                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, strcmd, nID);
                            
                            Global._TCPManager.MySocketListener.SendData(clientsList[i].ClientSocket, tcpOutPacket);
                        }
                        else
                        {
                            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,clientsList[i], toMapCode, -1, -1, -1);
                            
                            GameManager.LuaMgr.Error(clientsList[i], Global.GetLang("副本时间已到，离开副本地图"));
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 判断副本是否能否删除
        /// </summary>
        /// <param name="copyMap"></param>
        /// <returns></returns>
        private bool CanRemoveCopyMap(CopyMap copyMap, long nowTicks)
        {
            if (copyMap.bNeedRemove)
            {
                return true;
            }

            List<GameClient> clientsList = copyMap.GetClientsList();

            long maxKeepAliveTicks = 60 * 3 * 1000;
            if (copyMap.IsKuaFuCopy)
            {
                if (copyMap.CanRemoveTicks > 0)
                {
                    if (nowTicks > copyMap.CanRemoveTicks)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                maxKeepAliveTicks = 30 * 1000;
            }

            //副本是否超过了最大的生存期间
            if (CopyMapOverTime(copyMap, nowTicks, clientsList))
            {
                return true;
            }

            if (null != clientsList && clientsList.Count > 0)
            {
                //还有角色存在不允许删除
                return false;
            }

            long lastLeaveClientTicks = copyMap.GetLastLeaveClientTicks();
            if ((nowTicks - lastLeaveClientTicks) < maxKeepAliveTicks)
            {
                //还未到删除的时间
                return false;
            }

            return true;
        }

        /// <summary>
        /// 执行删除副本的操作
        /// </summary>
        /// <param name="copyMap"></param>
        /// <returns></returns>
        public void ProcessRemoveCopyMap(CopyMap copyMap)
        {
            //处理副本地图中剩余的怪物(如果已经被杀光了，则记录状态)
            //获取指定副本ID上的怪物的个数
            int monsterCount = Global.GetLeftMonsterByCopyMapID(copyMap.CopyMapID);
            int monsterState = 0;
            if (copyMap.IsInitMonster)
            {
                monsterState = (monsterCount <= 0) ? 1 : 0;
            }

            //添加角色的副本地图编号的怪物杀光状态
            AddMonsterState(copyMap.FuBenSeqID, copyMap.MapCode, monsterState);

            //执行删除副本中的怪物的操作
            GameManager.MonsterZoneMgr.DestroyCopyMapMonsters(copyMap.MapCode, copyMap.CopyMapID);

            //删除角色的副本地图编号
            RemoveCopyID(copyMap.FuBenSeqID, copyMap.MapCode);

            //将副本对象加入管理
            RemoveCopyMap(copyMap);

            //从组队副本列表移除
            RemoveTeamCopyMap(copyMap);

            SceneUIClasses sceneType = Global.GetMapSceneType(copyMap.MapCode);

            // 处理新手场景 -- 移除 [12/1/2013 LiaoWei]
            if (copyMap.MapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)
                FreshPlayerCopySceneManager.RemoveFreshPlayerListCopyMap(copyMap.FuBenSeqID, copyMap);

            // 经验副本 [3/19/2014 LiaoWei]
            if (Global.IsInExperienceCopyScene(copyMap.MapCode))
            {
                ExperienceCopySceneManager.RemoveExperienceListCopyMap(copyMap.FuBenSeqID);
            }

            // 金币副本 [6/11/2014 LiaoWei]
            if (copyMap.MapCode == (int)GoldCopySceneEnum.GOLDCOPYSCENEMAPCODEID)
            {
                GlodCopySceneManager.RemoveGlodCopySceneList(copyMap.FuBenSeqID);
            }
            else if (copyMap.MapCode == EMoLaiXiCopySceneManager.EMoLaiXiCopySceneMapCode)
            {
                EMoLaiXiCopySceneManager.RemoveEMoLaiXiCopySceneList(copyMap.FuBenSeqID, copyMap.CopyMapID);
            }
            else
            // 血色城堡副本 [7/7/2014 LiaoWei]
            if (GameManager.BloodCastleCopySceneMgr.IsBloodCastleCopyScene(copyMap.FubenMapID))
            {
                GameManager.BloodCastleCopySceneMgr.RemoveBloodCastleListCopyScenes(copyMap, copyMap.FuBenSeqID, copyMap.FubenMapID);
            }
            else
            // 恶魔广场副本 [7/11/2014 LiaoWei]
            if (GameManager.DaimonSquareCopySceneMgr.IsDaimonSquareCopyScene(copyMap.FubenMapID))
            {
                GameManager.DaimonSquareCopySceneMgr.RemoveDaimonSquareListCopyScenes(copyMap, copyMap.FuBenSeqID, copyMap.FubenMapID);
            }

            switch (sceneType)
            {
                case SceneUIClasses.HuanYingSiYuan:
                    {
                        HuanYingSiYuanManager.getInstance().RemoveHuanYingSiYuanListCopyScenes(copyMap);
                    }
                    break;
                case SceneUIClasses.TianTi:
                    {
                        TianTiManager.getInstance().RemoveTianTiCopyScene(copyMap, sceneType);
                    }
                    break;
                case SceneUIClasses.YongZheZhanChang:
                    {
                        YongZheZhanChangManager.getInstance().RemoveCopyScene(copyMap, sceneType);
                    }
                    break;
                case SceneUIClasses.KingOfBattle:
                    {
                        KingOfBattleManager.getInstance().RemoveCopyScene(copyMap, sceneType);
                    }
                    break;
                case SceneUIClasses.MoRiJudge:
                    {
                        MoRiJudgeManager.Instance().DelCopyScene(copyMap);
                    }
                    break;
                case SceneUIClasses.ElementWar:
                    {
                        ElementWarManager.getInstance().RemoveCopyScene(copyMap);
                    }
                    break;
                case SceneUIClasses.CopyWolf:
                    {
                        CopyWolfManager.getInstance().RemoveCopyScene(copyMap);
                    }
                    break;
                case SceneUIClasses.KuaFuBoss:
                    {
                        KuaFuBossManager.getInstance().RemoveCopyScene(copyMap, sceneType);
                    }
                    break;
                case SceneUIClasses.LangHunLingYu:
                    {
                        LangHunLingYuManager.getInstance().RemoveCopyScene(copyMap, sceneType);
                    }
                    break;
                case SceneUIClasses.KFZhengBa:
                    {
                        ZhengBaManager.Instance().RemoveCopyScene(copyMap, sceneType);
                    }
                    break;
                case SceneUIClasses.CoupleArena:
                    {
                        CoupleArenaManager.Instance().RemoveCopyScene(copyMap, sceneType);
                    }
                    break;
            }

            // 检测是否通知跨服中心删除副本
            CopyTeamManager.Instance().OnCopyRemove(copyMap.FuBenSeqID);
            FuBenManager.RemoveFuBenInfoBySeqID(copyMap.FuBenSeqID);

            copyMap.bNeedRemove = false;
        }

        /// <summary>
        /// 执行删除副本的操作
        /// </summary>
        /// <param name="copyMap"></param>
        /// <returns></returns>
        public void ProcessRemoveCopyMaps(List<CopyMap> listCopyMap, int FuBenSeqID, int FubenMapID)
        {
            foreach (CopyMap copyMap in listCopyMap)
            {
                //处理副本地图中剩余的怪物(如果已经被杀光了，则记录状态)
                //获取指定副本ID上的怪物的个数
                int monsterCount = Global.GetLeftMonsterByCopyMapID(copyMap.CopyMapID);
                int monsterState = 0;
                if (copyMap.IsInitMonster)
                {
                    monsterState = (monsterCount <= 0) ? 1 : 0;
                }

                //添加角色的副本地图编号的怪物杀光状态
                AddMonsterState(copyMap.FuBenSeqID, copyMap.MapCode, monsterState);

                //执行删除副本中的怪物的操作
                GameManager.MonsterZoneMgr.DestroyCopyMapMonsters(copyMap.MapCode, copyMap.CopyMapID);

                //删除角色的副本地图编号
                RemoveCopyID(copyMap.FuBenSeqID, copyMap.MapCode);

                //将副本对象加入管理
                RemoveCopyMap(copyMap);

                //从组队副本列表移除
                RemoveTeamCopyMap(copyMap);

                // 检测是否通知跨服中心删除副本
                CopyTeamManager.Instance().OnCopyRemove(copyMap.FuBenSeqID);
            }

            if (LuoLanFaZhenCopySceneManager.IsLuoLanFaZhen(FubenMapID))
            {
                LuoLanFaZhenCopySceneManager.OnFubenOver(FuBenSeqID);
            }

            FuBenManager.RemoveFuBenInfoBySeqID(FuBenSeqID);
        }

        /// <summary>
        /// 处理超时的副本，从副本管理中删除
        /// </summary>
        public void ProcessEndCopyMap()
        {
            long nowTicks = TimeUtil.NOW();
            int index = 0;
            CopyMap copyMap = GetNextCopyMap(index);
            //Dictionary<int, List<CopyMap>> dicMultiCopyMap = new Dictionary<int, List<CopyMap>>();
            while (null != copyMap)
            {
                //罗兰法阵是由多个地图组成的副本，需要判断每个地图是否都应该销毁
                //以后的多地图副本也加在这
                if (LuoLanFaZhenCopySceneManager.IsLuoLanFaZhen(copyMap.FubenMapID))
                {
                    List<CopyMap> listMultiCopyMap = null;
                    bool bCanRemoveFuBen = true;
                    //根据副本编号获取副本地图编号列表
                    List<int> mapCodeList = FuBenManager.FindMapCodeListByFuBenID(copyMap.FubenMapID);
                    if (null != mapCodeList)
                    {
                        foreach (int mapcode in mapCodeList)
                        {
                            int copyMapID = FindCopyID(copyMap.FuBenSeqID, mapcode);
                            if (copyMapID >= 0)
                            {
                                CopyMap child_map = FindCopyMap(copyMapID);
                                if (null != child_map)
                                {
                                    if (!CanRemoveCopyMap(child_map, nowTicks))
                                    {
                                        //只要有一个地图不能删除，这个副本就不能删除
                                        bCanRemoveFuBen = false;
                                        break;
                                    }
                                    if (null == listMultiCopyMap)
                                        listMultiCopyMap = new List<CopyMap>();
                                    listMultiCopyMap.Add(child_map);
                                }
                            }
                        }
                    }

                    // 有一个地图不能删除，就找下一个
                    if (!bCanRemoveFuBen)
                    {
                        index++;
                        copyMap = GetNextCopyMap(index);
                        continue;
                    }

                    if (bCanRemoveFuBen && null != listMultiCopyMap && listMultiCopyMap.Count > 0)
                    {
                        //删除这个副本和里面的所有地图
                        ProcessRemoveCopyMaps(listMultiCopyMap, copyMap.FuBenSeqID, copyMap.FubenMapID);
                        //一次只删除一个副本
                        break;
                    }
                }
                
                //判断副本是否能否删除
                if (CanRemoveCopyMap(copyMap, nowTicks))
                {
                    //执行删除副本的操作
                    ProcessRemoveCopyMap(copyMap);

                    GuildCopyMap mapData = GameManager.GuildCopyMapMgr.FindGuildCopyMapBySeqID(copyMap.FuBenSeqID);
                    if (null != mapData)
                        GameManager.GuildCopyMapMgr.RemoveGuildCopyMap(mapData.GuildID);

                    break; //一次只删除一个
                }

                index++;
                copyMap = GetNextCopyMap(index);
            }
        }

        /// <summary>
        /// 处理跨周的帮会副本
        /// </summary>
        public void ProcessEndGuildCopyMapFlag()
        {
            if (GameManager.GuildCopyMapMgr.IsPrepareResetTime())
            {
                GameManager.GuildCopyMapMgr.ProcessEndFlag = true;
            }
        }

        /// <summary>
        /// 处理跨周的帮会副本
        /// </summary>
        public void ProcessEndGuildCopyMap(long ticks)
        {
            if (ticks - GameManager.GuildCopyMapMgr.lastProcessEndTicks < 1000)
            {
                return;
            }
            GameManager.GuildCopyMapMgr.lastProcessEndTicks = ticks;

            // 这个时间点在设置标记
            if (GameManager.GuildCopyMapMgr.IsPrepareResetTime())
            {
                return;
            }

            // 更新没有完成
            if (GameManager.GuildCopyMapMgr.ProcessEndFlag == true)
            {
                GuildCopyMap mapData = GameManager.GuildCopyMapMgr.FindActiveGuildCopyMap();
                // 检测完成
                if (null == mapData)
                {
                    GameManager.GuildCopyMapMgr.ProcessEndFlag = false;
                    return;
                }

                GameManager.GuildCopyMapMgr.RemoveGuildCopyMap(mapData.GuildID);
                // 关闭副本
                CloseGuildCopyMap(mapData.SeqID, mapData.MapCode);
            }
        }

        /// <summary>
        /// 强制关闭一个副本
        /// </summary> 
        public void CloseGuildCopyMap(int fuBenSeqID, int mapCode)
        {
            int copyMapID = FindCopyID(fuBenSeqID, mapCode);
            if (copyMapID <= 0)
                return;

            CopyMap copyMap = FindCopyMap(copyMapID);
            if (null == copyMap)
                return;
            if (!GameManager.GuildCopyMapMgr.IsGuildCopyMap(copyMap.FubenMapID))
                return;
            // 把人都踢出去
            RemoveCopyMapAllPlayer(copyMap);
            // 然后释放副本
            ProcessRemoveCopyMap(copyMap);
        }

        /// <summary>
        /// 清空副本里的人
        /// </summary>
        public void RemoveCopyMapAllPlayer(CopyMap copyMap)
        {
            List<GameClient> objsList = copyMap.GetClientsList();
            if (objsList != null)
            {
                for (int n = 0; n < objsList.Count; ++n)
                {
                    GameClient c = objsList[n] as GameClient;
                    if (c == null)
                        continue;

                    if (c.ClientData.MapCode != copyMap.MapCode)
                        continue;

                    // 退出场景
                    int toMapCode = GameManager.MainMapCode;    //主城ID 防止意外
                    int toPosX = -1;
                    int toPosY = -1;
                    if (MapTypes.Normal == Global.GetMapType(c.ClientData.LastMapCode))
                    {
                        if (GameManager.BattleMgr.BattleMapCode != c.ClientData.LastMapCode || GameManager.ArenaBattleMgr.BattleMapCode != c.ClientData.LastMapCode)
                        {
                            toMapCode = c.ClientData.LastMapCode;
                            toPosX = c.ClientData.LastPosX;
                            toPosY = c.ClientData.LastPosY;
                        }
                    }

                    GameMap gameMap = null;
                    if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap))
                    {
                        c.ClientData.bIsInAngelTempleMap = false;
                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, c, toMapCode, toPosX, toPosY, -1);
                    }
                }
            }
        }

        #endregion 副本删除处理

        #region 角色伤害列表

        /// <summary>
        /// 伤害字典和副本列表的联合锁对象
        /// </summary>
        private object _RoleDamageDict_TeamCopyMapDict_Mutex = new object();

        /// <summary>
        /// 副本地图角色伤害字典
        /// </summary>
        private Dictionary<int, Dictionary<int, RoleDamage>> RoleDamageDict = new Dictionary<int, Dictionary<int, RoleDamage>>();

        /// <summary>
        /// 角色地图需要记录伤害列表的副本地图列表
        /// </summary>
        private List<CopyMap> TeamCopyMapDict = new List<CopyMap>();

        /// <summary>
        /// 添加副本地图时的相关处理
        /// </summary>
        /// <param name="copyMap"></param>
        public void AddTeamCopyMap(CopyMap copyMap)
        {
            if (CopyTeamManager.Instance().NeedRecordDamageInfoFuBenID(copyMap.FubenMapID))
            {
                lock (_RoleDamageDict_TeamCopyMapDict_Mutex)
                {
                    if (!TeamCopyMapDict.Contains(copyMap))
                    {
                        RoleDamageDict.Add(copyMap.CopyMapID, new Dictionary<int, RoleDamage>());
                        TeamCopyMapDict.Add(copyMap);
                    }
                }
            }
        }

        /// <summary>
        /// 移除副本地图时的相关处理
        /// </summary>
        /// <param name="copyMap"></param>
        public void RemoveTeamCopyMap(CopyMap copyMap)
        {
            if (CopyTeamManager.Instance().NeedRecordDamageInfoFuBenID(copyMap.FubenMapID))
            {
                lock (_RoleDamageDict_TeamCopyMapDict_Mutex)
                {
                    RoleDamageDict.Remove(copyMap.CopyMapID);
                    TeamCopyMapDict.Remove(copyMap);
                }
            }
        }

        /// <summary>
        /// 获取一个副本地图的所有角色累计伤害列表
        /// </summary>
        /// <param name="copyMapID"></param>
        /// <returns></returns>
        public List<RoleDamage> GetCopyMapAllRoleDamages(int copyMapID)
        {
            List<RoleDamage> roleDamages = null;
            lock (_RoleDamageDict_TeamCopyMapDict_Mutex)
            {
                Dictionary<int, RoleDamage> dict;
                if (RoleDamageDict.TryGetValue(copyMapID, out dict))
                {
                    roleDamages = new List<RoleDamage>();
                    foreach (var rd in dict.Values)
                    {
                        roleDamages.Add(rd);
                    }
                }
            }

            return roleDamages;
        }

        /// <summary>
        /// 想副本内角色广播伤害信息
        /// </summary>
        /// <param name="copyMap"></param>
        /// <param name="sendtoRoleId">仅仅发送给一个玩家</param>
        public void BroadcastCopyMapDamageInfo(CopyMap copyMap, int sendtoRoleId = -1)
        {
            lock (_RoleDamageDict_TeamCopyMapDict_Mutex)
            {
                Dictionary<int, RoleDamage> dict;
                if (RoleDamageDict.TryGetValue(copyMap.CopyMapID, out dict))
                {
                    List<GameClient> clientList = copyMap.GetClientsList();
                    foreach (var client in clientList)
                    {
                        long damage = Interlocked.Exchange(ref client.SumDamageForCopyTeam, 0);
                        //if (damage > 0)
                        {
                            RoleDamage rd;
                            int roleID = client.ClientData.RoleID;
                            if (dict.TryGetValue(roleID, out rd))
                            {
                                rd.Damage += damage;
                            }
                            else
                            {
                                dict.Add(roleID, new RoleDamage(roleID, damage, Global.FormatRoleName(client, client.ClientData.RoleName)));
                            }
                        }
                    }

                    List<RoleDamage> roleDamages = GetCopyMapAllRoleDamages(copyMap.CopyMapID);
                    foreach (var client in clientList)
                    {
                        if (sendtoRoleId < 0 || sendtoRoleId == client.ClientData.RoleID)
                        {
                            client.sendCmd<List<RoleDamage>>((int)TCPGameServerCmds.CMD_SPR_COPYTEAMDAMAGEINFO, roleDamages);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 想副本内角色发送伤害信息
        /// </summary>
        /// <param name="copyMap"></param>
        /// <param name="sendtoRoleId">仅仅发送给一个玩家</param>
        public void SendCopyMapMaxDamageInfo(GameClient client, CopyMap copyMap, int MaxCount)
        {
            if (MaxCount <= 0)
                return;

            lock (_RoleDamageDict_TeamCopyMapDict_Mutex)
            {
                Dictionary<int, RoleDamage> dict;
                if (RoleDamageDict.TryGetValue(copyMap.CopyMapID, out dict))
                {
                    List<GameClient> clientList = copyMap.GetClientsList();
                    foreach (var gc in clientList)
                    {
                        long damage = Interlocked.Exchange(ref gc.SumDamageForCopyTeam, 0);
                        //if (damage > 0)
                        {
                            RoleDamage rd;
                            int roleID = gc.ClientData.RoleID;
                            if (dict.TryGetValue(roleID, out rd))
                            {
                                rd.Damage += damage;
                            }
                            else
                            {
                                dict.Add(roleID, new RoleDamage(roleID, damage, Global.FormatRoleName(gc, gc.ClientData.RoleName)));
                            }
                        }
                    }

                    List<RoleDamage> roleDamages = GetCopyMapAllRoleDamages(copyMap.CopyMapID);
                    // 排序
                    IEnumerable<RoleDamage> query = null;
                    query = from items in roleDamages orderby items.Damage descending select items;
                    List<RoleDamage> tempList = new List<RoleDamage>();
                    int count = 0;
                    foreach (var item in query)
                    {
                        tempList.Add(item);
                        count++;
                        if (count >= GameManager.GuildCopyMapMgr.MaxDamageSendCount)
                            break;
                    }
                    roleDamages = tempList;

                    client.sendCmd<List<RoleDamage>>((int)TCPGameServerCmds.CMD_SPR_COPYTEAMDAMAGEINFO, roleDamages);
                }
            }
        }

        /// <summary>
        /// 上次刷新伤害列表的时间
        /// </summary>
        private long LastNotifyTeamDamageTicks = 0;

        /// <summary>
        /// 更新副本地图中角色的伤害累计值,将变化通知客户端
        /// </summary>
        /// <param name="ticks"></param>
        /// <param name="force"></param>
        public void CheckCopyTeamDamage(long ticks, bool force = false)
        {
            if (ticks - LastNotifyTeamDamageTicks < 2000)
            {
                return;
            }
            LastNotifyTeamDamageTicks = ticks;

            lock (_RoleDamageDict_TeamCopyMapDict_Mutex)
            {
                foreach (var copyMap in TeamCopyMapDict)
                {
                    Dictionary<int, RoleDamage> dict;
                    if (RoleDamageDict.TryGetValue(copyMap.CopyMapID, out dict))
                    {
                        List<GameClient> clientList = copyMap.GetClientsList();
                        long sumdamage = 0;
                        foreach (var client in clientList)
                        {
                            long damage = Interlocked.Exchange(ref client.SumDamageForCopyTeam, 0);
                            if (damage > 0)
                            {
                                RoleDamage rd;
                                int roleID = client.ClientData.RoleID;
                                if (dict.TryGetValue(roleID, out rd))
                                {
                                    rd.Damage += damage;
                                }
                                else
                                {
                                    dict.Add(roleID, new RoleDamage(roleID, damage, Global.FormatRoleName(client, client.ClientData.RoleName)));
                                }
                                sumdamage += damage;
                            }
                        }

                        if (sumdamage > 0 || force)
                        {
                            List<RoleDamage> roleDamages = GetCopyMapAllRoleDamages(copyMap.CopyMapID);

                            // 如果是帮会副本，只发送前五名
                            if (GameManager.GuildCopyMapMgr.IsGuildCopyMap(copyMap.FubenMapID))
                            {
                                // 排序
                                IEnumerable<RoleDamage> query = null;
                                query = from items in roleDamages orderby items.Damage descending select items;
                                List<RoleDamage> tempList = new List<RoleDamage>();
                                int count = 0;
                                foreach (var item in query)
                                {
                                    tempList.Add(item);
                                    count++;
                                    if (count >= GameManager.GuildCopyMapMgr.MaxDamageSendCount)
                                        break;
                                }
                                roleDamages = tempList;
                            }
                            foreach (var client in clientList)
                            {
                                client.sendCmd<List<RoleDamage>>((int)TCPGameServerCmds.CMD_SPR_COPYTEAMDAMAGEINFO, roleDamages);
                            }
                        }
                    }
                }
            }
        }

        #endregion 角色伤害列表

        #region 副本怪物杀死个数

        /// <summary>
        /// 英雄逐擂的地图字段
        /// </summary>
        private static string[] HeroMapCodeFileds = null;

        /// <summary>
        /// 是否是英雄逐擂的地图编号
        /// </summary>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        private bool IsHeroMapCode(int mapCode)
        {
            if (null == HeroMapCodeFileds)
            {
                string heroMapCodes = GameManager.systemParamsList.GetParamValueByName("HeroMapCodes");
                if (!string.IsNullOrEmpty(heroMapCodes)) //如果是英雄逐擂地图编号
                {
                    HeroMapCodeFileds = heroMapCodes.Split(',');
                }
            }

            if (null == HeroMapCodeFileds || HeroMapCodeFileds.Length <= 0) return false;

            string strMapCode = mapCode.ToString();
            for (int i = 0; i < HeroMapCodeFileds.Length; i++)
            {
                if (HeroMapCodeFileds[i] == strMapCode)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 副本通关奖励
        /// </summary>
        public void CopyMapPassAward(GameClient client, CopyMap copyMap, bool anyAlive)
        {
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

                        // 副本改造 begin [11/15/2013 LiaoWei]
                        // 增加每日活跃对副本通关的活跃度 [2/26/2014 LiaoWei]
                        int nLev = -1;
                        string strName = null;
                        SystemXmlItem systemFuBenItem = null;
                        if (GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(fuBenID, out systemFuBenItem))
                        {
                            nLev = systemFuBenItem.GetIntValue("FuBenLevel");
                            strName = systemFuBenItem.GetStringValue("CopyName");
                        }

                        // 更新玩家通关时间信息
                        Global.UpdateFuBenDataForQuickPassTimer(client, fuBenID, usedSecs, addFuBenNum);

                        // 给玩家物品
                        FuBenMapItem fuBenMapItem = FuBenManager.FindMapCodeByFuBenID(fuBenID, client.ClientData.MapCode);
                        if (null != fuBenMapItem && fuBenMapItem.Experience > 0 && fuBenMapItem.Money1 > 0)
                        {
                            int nMaxTime = fuBenMapItem.MaxTime * 60; //分->秒
                            long startTicks = fuBenInfoItem.StartTicks;
                            long endTicks = fuBenInfoItem.EndTicks;
                            int nFinishTimer = (int)(endTicks - startTicks) / 1000;//毫秒->秒
                            int killedNum = 0;//copyMap.KilledNormalNum + copyMap.KilledBossNum;
                            int nDieCount = fuBenInfoItem.nDieCount;

                            //向客户的发放通关奖励
                            fubenTongGuanData = Global.GiveCopyMapGiftForScore(client, fuBenID, copyMap.MapCode, nMaxTime, nFinishTimer, killedNum, nDieCount, (int)(fuBenMapItem.Experience * fuBenInfoItem.AwardRate), (int)(fuBenMapItem.Money1 * fuBenInfoItem.AwardRate), fuBenMapItem, strName);

                        }

                        // 副本改造 end [11/15/2013 LiaoWei]

                        //记录通关记录
                        //异步写数据库，写入当前的重新开始闭关的的时间
                        GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDFUBENHISTDATA,
                            string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, Global.FormatRoleName(client, client.ClientData.RoleName), fuBenID, usedSecs),
                            null, client.ServerId);

                        //更新每日的通关副本的数量
                        bool bActiveChengJiu = true;
                        if (GameManager.BloodCastleCopySceneMgr.IsBloodCastleCopyScene(copyMap.FubenMapID) && GameManager.DaimonSquareCopySceneMgr.IsDaimonSquareCopyScene(copyMap.FubenMapID))
                            bActiveChengJiu = false;

                        GameManager.ClientMgr.UpdateRoleDailyData_FuBenNum(client, 1, nLev, bActiveChengJiu);

                        //副本通关
                        //Global.BroadcastFuBenOk(client, usedSecs, fuBenID);

                        // 是否自动领取奖励 这个变理没用 ChenXiaojun
                        // autoGetFuBenAwards = true;
                    }
                }
            }

            //通知客户端消息
            //通知副本地图上的所有人副本信息(同一个副本地图才需要通知)
            GameManager.ClientMgr.NotifyAllFuBenBeginInfo(client, !anyAlive);

            if (fubenTongGuanData != null)
            {
                //发送奖励到客户端
                /*                        TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<FuBenTongGuanData>(fubenTongGuanData, Global._TCPManager.TcpOutPacketPool, 
                                            (int)TCPGameServerCmds.CMD_SPR_FUBENPASSNOTIFY);

                                        if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                                        {
                                            //如果发送失败
                                        }*/

                byte[] bytesData = DataHelper.ObjectToBytes<FuBenTongGuanData>(fubenTongGuanData);
                GameManager.ClientMgr.NotifyAllFuBenTongGuanJiangLi(client, bytesData);
            }
        }

        /// <summary>
        /// 副本通关奖励
        /// </summary>
        public void CopyMapPassAwardForAll(GameClient client, CopyMap copyMap, bool anyAlive)
        {
            if (copyMap.CopyMapPassAwardFlag)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("CopyMapPassAwardForAll: 组队副本{0}序列ID({1})完成并给过奖励,不应再次给予", copyMap.FubenMapID, copyMap.FuBenSeqID));
                return;
            }
            copyMap.CopyMapPassAwardFlag = true; 

            int fuBenSeqID = FuBenManager.FindFuBenSeqIDByRoleID(client.ClientData.RoleID);

            List<GameClient> objsList = new List<GameClient>();

            //罗兰法阵特殊处理
            if (LuoLanFaZhenCopySceneManager.IsLuoLanFaZhen(copyMap.FubenMapID))
            {
                //根据副本编号获取副本地图编号列表
                List<int> mapCodeList = FuBenManager.FindMapCodeListByFuBenID(copyMap.FubenMapID);
                if (null != mapCodeList)
                {
                    //多地图副本需要处理各个地图内所有玩家
                    foreach (int mapcode in mapCodeList)
                    {
                        int copyMapID = FindCopyID(fuBenSeqID, mapcode);
                        if (copyMapID >= 0)
                        {
                            CopyMap child_map = FindCopyMap(copyMapID);
                            if (null != child_map)
                            {
                                objsList.AddRange(child_map.GetClientsList());
                            }
                        }
                    }
                }
            }
            else
                objsList.AddRange(copyMap.GetClientsList());

            objsList = Global.DistinctGameClientList(objsList); //过滤重复对象列表
            if (null == objsList)
            {
                return;
            }

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

                        int nLev = -1;
                        string strName = null;
                        SystemXmlItem systemFuBenItem = null;
                        if (GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(fuBenID, out systemFuBenItem))
                        {
                            nLev = systemFuBenItem.GetIntValue("FuBenLevel");
                            strName = systemFuBenItem.GetStringValue("CopyName");
                        }

                        // 给玩家物品
                        FuBenMapItem fuBenMapItem = FuBenManager.FindMapCodeByFuBenID(fuBenID, client.ClientData.MapCode);
                        if (fuBenMapItem.Experience > 0 && fuBenMapItem.Money1 > 0)
                        {
                            int nMaxTime = fuBenMapItem.MaxTime * 60; //分->秒
                            long startTicks = fuBenInfoItem.StartTicks;
                            long endTicks = fuBenInfoItem.EndTicks;
                            int nFinishTimer = (int)(endTicks - startTicks) / 1000;//毫秒->秒
                            int killedNum = 0;// copyMap.KilledNormalNum + copyMap.KilledBossNum;
                            int nDieCount = fuBenInfoItem.nDieCount;

                            for (int i = 0; i < objsList.Count; i++)
                            {
                                GameClient c = objsList[i] as GameClient;
                                if (null == c)
                                {
                                    continue;
                                }

                                //向客户的发放通关奖励
                                fubenTongGuanData = Global.GiveCopyMapGiftForScore(c, fuBenID, copyMap.MapCode, nMaxTime, nFinishTimer, killedNum, nDieCount, (int)(fuBenMapItem.Experience * fuBenInfoItem.AwardRate), (int)(fuBenMapItem.Money1 * fuBenInfoItem.AwardRate), fuBenMapItem, strName);

                                if (fubenTongGuanData != null)
                                {
                                    byte[] bytesData = DataHelper.ObjectToBytes<FuBenTongGuanData>(fubenTongGuanData);
                                    GameManager.ClientMgr.SendToClient(c, bytesData, (int)TCPGameServerCmds.CMD_SPR_FUBENPASSNOTIFY);
                                }
                            }
                        }

                        for (int i = 0; i < objsList.Count; i++)
                        {
                            GameClient c = objsList[i] as GameClient;
                            if (null == c)
                            {
                                continue;
                            }

                            // 更新玩家通关时间信息
                            Global.UpdateFuBenDataForQuickPassTimer(c, fuBenID, usedSecs, addFuBenNum);

                            //记录通关记录
                            //异步写数据库，写入当前的重新开始闭关的的时间
                            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDFUBENHISTDATA,
                                string.Format("{0}:{1}:{2}:{3}", c.ClientData.RoleID, Global.FormatRoleName(c, c.ClientData.RoleName), fuBenID, usedSecs),
                                null, c.ServerId);

                            //更新每日的通关副本的数量
                            bool bActiveChengJiu = true;
                            if (GameManager.BloodCastleCopySceneMgr.IsBloodCastleCopyScene(copyMap.FubenMapID) && GameManager.DaimonSquareCopySceneMgr.IsDaimonSquareCopyScene(copyMap.FubenMapID))
                                bActiveChengJiu = false;

                            GameManager.ClientMgr.UpdateRoleDailyData_FuBenNum(c, 1, nLev, bActiveChengJiu);

                            //副本通关广播
                            //Global.BroadcastFuBenOk(c, usedSecs, fuBenID);
                        }
                    }
                }
            }

            //通知客户端副本结束消息
            if (LuoLanFaZhenCopySceneManager.IsLuoLanFaZhen(copyMap.FubenMapID))
            {
                //通知副本里所有地图上的所有人
                GameManager.ClientMgr.NotifyAllMapFuBenBeginInfo(client, !anyAlive);
            }
            else
            {
                //通知副本地图上的所有人副本信息(同一个副本地图才需要通知)
                GameManager.ClientMgr.NotifyAllFuBenBeginInfo(client, !anyAlive);
            }

            //if (fubenTongGuanData != null)
            {
                //发送奖励到客户端
                /*                        TCPOutPacket tcpOutPacket = DataHelper.ObjectToTCPOutPacket<FuBenTongGuanData>(fubenTongGuanData, Global._TCPManager.TcpOutPacketPool, 
                                            (int)TCPGameServerCmds.CMD_SPR_FUBENPASSNOTIFY);

                                        if (!Global._TCPManager.MySocketListener.SendData(client.ClientSocket, tcpOutPacket))
                                        {
                                            //如果发送失败
                                        }*/

                //byte[] bytesData = DataHelper.ObjectToBytes<FuBenTongGuanData>(fubenTongGuanData);
                //GameManager.ClientMgr.NotifyAllFuBenTongGuanJiangLi(client, bytesData);
            }
        }

        /// <summary>
        /// 副本通关奖励
        /// </summary>
        public void CopyMapFaildForAll(List<GameClient> objsList, CopyMap copyMap)
        {
            if (copyMap.CopyMapPassAwardFlag)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("CopyMapPassAwardForAll: 组队副本{0}序列ID({1})完成并给过奖励,不应再次给予", copyMap.FubenMapID, copyMap.FuBenSeqID));
                return;
            }
            copyMap.CopyMapPassAwardFlag = true; 

            int fuBenSeqID = copyMap.FuBenSeqID;
            int mapCode = copyMap.MapCode;
            objsList = Global.DistinctGameClientList(objsList); //过滤重复对象列表
            if (null == objsList)
            {
                return;
            }

            FuBenTongGuanData fubenTongGuanData = null;
            if (fuBenSeqID > 0) //如果副本不存在
            {
                FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(fuBenSeqID);
                if (null != fuBenInfoItem)
                {
                    fuBenInfoItem.EndTicks = TimeUtil.NOW();
                    int fuBenID = FuBenManager.FindFuBenIDByMapCode(mapCode);
                    if (fuBenID > 0)
                    {
                        int usedSecs = (int)((fuBenInfoItem.EndTicks - fuBenInfoItem.StartTicks) / 1000);
                        fubenTongGuanData = new FuBenTongGuanData();
                        fubenTongGuanData.FuBenID = copyMap.FubenMapID;
                        fubenTongGuanData.MapCode = mapCode;
                        fubenTongGuanData.ResultMark = 1;
                    }
                }
            }

            foreach (var client in objsList)
            {
                //通知副本地图上的所有人副本信息(同一个副本地图才需要通知)
                GameManager.ClientMgr.NotifyAllFuBenBeginInfo(client, false);
            }

            if (fubenTongGuanData != null && objsList.Count > 0)
            {
                byte[] bytesData = DataHelper.ObjectToBytes<FuBenTongGuanData>(fubenTongGuanData);
                GameManager.ClientMgr.NotifyAllFuBenTongGuanJiangLi(objsList[0], bytesData);
            }
        }

        /// <summary>
        /// 处理杀死的怪物个数
        /// </summary>
        /// <param name="monster"></param>
        public void ProcessKilledMonster(GameClient client, Monster monster)
        {
            //非副本怪物
            if (monster.CopyMapID <= 0)
            {
                return;
            }

            CopyMap copyMap = this.FindCopyMap(monster.CopyMapID);
            if (null == copyMap) 
                return;

            if (monster.ManagerType == SceneUIClasses.EMoLaiXiCopy)
            {
                copyMap.SetKilledDynamicMonsterDict(monster.UniqueID);
                return;
            }
            else if (monster.CurrentMapCode == MoRiJudgeManager.Instance().MapCode)
            {
                copyMap.SetKilledDynamicMonsterDict(monster.UniqueID);
                return;
            }

            // 剧情副本处理 [7/14/2014 LiaoWei]
            bool bIsStoryCopyMap = false;
            SystemXmlItem systemFuBenItem = null;
            if (GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(copyMap.FubenMapID, out systemFuBenItem))
            {
                int nKillAll = -1;
                nKillAll = systemFuBenItem.GetIntValue("KillAll");

                if (nKillAll == 2)
                    bIsStoryCopyMap = true;
            }

            //玩家召唤怪物区域的怪 是宠物，不能算死亡个数
            if (monster.MonsterZoneNode.IsDynamicZone() && !bIsStoryCopyMap)
            {
                return;
            }

            if ((int)MonsterTypes.Noraml == monster.MonsterType) //普通怪
            {
                copyMap.SetKilledNormalDict(monster.RoleID);
            }
            else
            {
                copyMap.SetKilledBossDict(monster.RoleID);
            }

            bool autoGetFuBenAwards = false;

            //如果怪物已经杀完毕了
            bool anyAlive = GameManager.MonsterMgr.IsAnyMonsterAliveByCopyMapID(monster.CopyMapID);
            
            if (bIsStoryCopyMap == true && !copyMap.bStoryCopyMapFinishStatus)
            {
                int nNeedKillBoos = -1;
                nNeedKillBoos = systemFuBenItem.GetIntValue("BossID");
#if ___CC___FUCK___YOU___BB___
                if (monster.XMonsterInfo.MonsterId == nNeedKillBoos)
                {
                    if (Global.IsInTeamCopyScene(client.ClientData.MapCode) || GameManager.GuildCopyMapMgr.IsGuildCopyMap(monster.CurrentMapCode))
                        CopyMapPassAwardForAll(client, copyMap, true);
                    else
                        CopyMapPassAward(client, copyMap, true);

                    Global.NotifyClientStoryCopyMapInfo(copyMap.CopyMapID, 3);

                    copyMap.bStoryCopyMapFinishStatus = true;

                    KillAllMonster(copyMap);
                }
#else
                if (monster.MonsterInfo.ExtensionID == nNeedKillBoos)
                {
                    if (Global.IsInTeamCopyScene(client.ClientData.MapCode) || GameManager.GuildCopyMapMgr.IsGuildCopyMap(monster.CurrentMapCode))
                        CopyMapPassAwardForAll(client, copyMap, true);
                    else
                        CopyMapPassAward(client, copyMap, true);

                    Global.NotifyClientStoryCopyMapInfo(copyMap.CopyMapID, 3);

                    copyMap.bStoryCopyMapFinishStatus = true;

                    KillAllMonster(copyMap);
                }
#endif
            }


            if (!bIsStoryCopyMap && ((copyMap.KilledNormalNum >= copyMap.TotalNormalNum && copyMap.KilledBossNum >= copyMap.TotalBossNum) || !anyAlive))
            {
                //是否是英雄逐擂的地图编号
                if (IsHeroMapCode(monster.MonsterZoneNode.MapCode))
                {
                    //当前所在副本的层数
                    int currentMapCodeIndex = FuBenManager.FindMapCodeIndexByFuBenID(monster.MonsterZoneNode.MapCode);

                    //通知英雄逐擂到达层数更新(限制当前地图)
                    GameManager.ClientMgr.ChangeRoleHeroIndex(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, currentMapCodeIndex, false);
                }

                //判断如果是最后一层，则不显示
                int toNextMapCode = FuBenManager.FindNextMapCodeByFuBenID(monster.MonsterZoneNode.MapCode);
                if (-1 == toNextMapCode) //最后一层？
                {
                    //通知副本地图上的所有人怪物数量(同一个副本地图才需要通知)
                    GameManager.ClientMgr.NotifyAllFuBenMonstersNum(client, !anyAlive);

                    // 万魔塔的结束处理，放在万魔塔管理类中
                    if (WanMotaCopySceneManager.IsWanMoTaMapCode(monster.MonsterZoneNode.MapCode))
                    {
                        WanMotaCopySceneManager.SendMsgToClientForWanMoTaCopyMapAward(client, copyMap, anyAlive);
                    }
                    else
                    {
                        if (Global.IsInTeamCopyScene(client.ClientData.MapCode) || GameManager.GuildCopyMapMgr.IsGuildCopyMap(monster.CurrentMapCode))
                        {
                            CopyMapPassAwardForAll(client, copyMap, anyAlive);
                        }
                        else
                        {
                            CopyMapPassAward(client, copyMap, anyAlive);
                        }
                    }
                }
                else //还有下一层
                {
                    //通知副本地图上的所有人怪物数量(同一个副本地图才需要通知)
                    GameManager.ClientMgr.NotifyAllFuBenMonstersNum(client, !anyAlive);
                }
            }
            else
            {
                //通知副本地图上的所有人怪物数量(同一个副本地图才需要通知)
                GameManager.ClientMgr.NotifyAllFuBenMonstersNum(client, !anyAlive);
            }

            //是否自动领取奖励
            if (autoGetFuBenAwards)
            {
                //处理自动获取副本经验和铜钱的奖励的操作
                //Global.ProcessAutoGetFuBenExpAndMoneyAwards(client);
            }
        }

        /// <summary>
        /// 杀死所有存活的怪物
        /// </summary>
        /// <param name="copyMap"></param>
        public void KillAllMonster(CopyMap copyMap)
        {
            List<object> objList = GameManager.MonsterMgr.GetCopyMapIDMonsterList(copyMap.CopyMapID);
            objList = Global.ConvertObjsList(copyMap.MapCode, copyMap.CopyMapID, objList);
            if (null != objList)
            {
                for (int i = 0; i < objList.Count; i++ )
                {
                    Monster monster = objList[i] as Monster;
                    if (null != monster)
                    {
                        Global.SystemKillMonster(monster);
                    }
                }
            }
        }

#endregion 副本怪物杀死个数

#region 获取副本个数信息

        /// <summary>
        /// 获取副本的个数运行信息
        /// </summary>
        /// <returns></returns>
        public string GetCopyMapStrInfo()
        {
            Dictionary<int, int> copyMapInfoDict = new Dictionary<int, int>();

            int totalCopyMapMonsterCount = 0;
            int totalCount = 0;
            int index = 0;
            CopyMap copyMap = GetNextCopyMap(index);
            while (null != copyMap)
            {
                totalCount++;
                index++;

                int totalMapCount = 0;
                if (copyMapInfoDict.TryGetValue(copyMap.MapCode, out totalMapCount))
                {
                    copyMapInfoDict[copyMap.MapCode] = totalMapCount + 1;
                }
                else
                {
                    copyMapInfoDict[copyMap.MapCode] = 1;
                }

                totalCopyMapMonsterCount += copyMap.TotalNormalNum;
                totalCopyMapMonsterCount += copyMap.TotalBossNum;

                copyMap = GetNextCopyMap(index);
            }

            StringBuilder infoTxt = new StringBuilder();
            infoTxt.AppendFormat(String.Format("当前总的副本数量 {0} 个 \r\n", totalCount));
            infoTxt.AppendFormat(String.Format("当前总的副本怪物数量 {0} 个, 总的动态怪物 {1} 个 \r\n", totalCopyMapMonsterCount, Monster.GetMonsterCount()));

            infoTxt.AppendFormat(String.Format("WaitingAddFuBenMonsterQueueCount {0} \r\n", GameManager.MonsterZoneMgr.WaitingAddFuBenMonsterQueueCount()));
            infoTxt.AppendFormat(String.Format("WaitingDestroyFuBenMonsterQueueCount {0} \r\n", GameManager.MonsterZoneMgr.WaitingDestroyFuBenMonsterQueueCount()));
            infoTxt.AppendFormat(String.Format("WaitingReloadFuBenMonsterQueueCount {0} \r\n", GameManager.MonsterZoneMgr.WaitingReloadFuBenMonsterQueueCount()));

            foreach (var item in copyMapInfoDict)
            {
                infoTxt.AppendFormat(String.Format("MapCode {0} 副本数量 {1} 个 \r\n", item.Key, item.Value));
            }

            return infoTxt.ToString();
        }

#endregion 获取副本个数信息

#region 光幕管理

        public void AddGuangMuEvent(CopyMap copyMap, int guangMuId, int show)
        {
            copyMap.AddGuangMuEvent(guangMuId, show);
            GameManager.ClientMgr.BroadSpecialMapAIEvent(copyMap.MapCode, copyMap.CopyMapID, guangMuId, show);
        }

#endregion 光幕管理
    }
}
