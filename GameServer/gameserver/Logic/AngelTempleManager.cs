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
using System.Threading;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    // 天使神殿管理器 [3/23/2014 LiaoWei]
    public class AngelTempleManager
    {
        /// <summary>
        /// 场景信息
        /// </summary>
        public AngelTempleSceneInfo m_AngelTempleScene = new AngelTempleSceneInfo();

        /// <summary>
        /// 静态信息
        /// </summary>
        public AngelTempleData m_AngelTempleData = new AngelTempleData();

        /// <summary>
        /// 玩家击杀天使血量
        /// </summary>
        public Dictionary<int, AngelTemplePointInfo> m_RoleDamageAngelValue = new Dictionary<int, AngelTemplePointInfo>();

        /// <summary>
        /// 保护数据m_RoleDamageAngelValue和m_PointInfoArray
        /// </summary>
        public object m_PointDamageInfoMutex = new object();

        /// <summary>
        /// 玩家击杀天使血量列表
        /// </summary>
        public AngelTemplePointInfo[] m_PointInfoArray = new AngelTemplePointInfo[6];

        /// <summary>
        /// Boss
        /// </summary>
        public Monster m_AngelTempleBoss = null;

        /// <summary>
        /// 是否给予奖励(是否击杀了Boss)
        /// </summary>
        public bool bBossKilled = false;

        /// <summary>
        /// BoosHP
        /// </summary>
        public long m_BossHP = 0;

        /// <summary>
        /// 最高伤害 -- 值
        /// </summary>
        public long m_nTotalDamageValue = -1;

        /// <summary>
        /// 最高伤害 -- 人名
        /// </summary>
        public string m_sTotalDamageName = "";

        /// <summary>
        /// KILL BOSS PLAYER role id
        /// </summary>
        public int m_sKillBossRoleID = 0;

        /// <summary>
        /// KILL BOSS PLAYER role name
        /// </summary>
        public string m_sKillBossRoleName = "";

        /// <summary>
        /// 同步信息TICK
        /// </summary>
        public long m_NotifyInfoTickForAll = 0;

        /// <summary>
        /// 同步信息TICK
        /// </summary>
        public long m_NotifyInfoTickForSingle = 0;

        /// <summary>
        /// 上次同步的Boss血量
        /// </summary>
        public int m_LastNotifyBossHPPercent = -1;

        /// <summary>
        /// 同步信息间隔
        /// </summary>
        public long m_NotifyInfoDelayTick = 3000;   // 3秒同步一次

        /// <summary>
        /// 给予排名奖励和幸运奖励的最低伤害值
        /// </summary>
        public long AngelTempleMinHurt = 0;

        /// <summary>
        /// 需要加强Boss的最小完成击杀时间
        /// </summary>
        private int AngelTempleBossUpgradeTime = 0;

        /// <summary>
        /// 加强Boss的比例参数
        /// </summary>
        private double AngelTempleBossUpgradeParam1 = 0;
        private double AngelTempleBossUpgradeParam2 = 0;
        private double AngelTempleBossUpgradeParam3 = 0;

        /// <summary>
        /// 加强Boss的比例
        /// </summary>
        private double AngelTempleMonsterUpgradePercent = 0;
        private double BossBaseHP = 0;

        /// <summary>
        /// 初始化场景
        /// </summary>
        public void InitAngelTemple()
        {
            Global.QueryDayActivityTotalPointInfoToDB(SpecialActivityTypes.AngelTemple);
            AngelTempleMonsterUpgradePercent = Global.SafeConvertToDouble(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.AngelTempleMonsterUpgradeNumber));
            AngelTempleMinHurt = GameManager.systemParamsList.GetParamValueIntByName("AngelTempleMinHurt");
            double[] AngelTempleBossUpgradeParams = GameManager.systemParamsList.GetParamValueDoubleArrayByName("AngelTempleBossUpgrade");
            if (null != AngelTempleBossUpgradeParams && AngelTempleBossUpgradeParams.Length == 4)
            {
                AngelTempleBossUpgradeTime = (int)AngelTempleBossUpgradeParams[0];
                AngelTempleBossUpgradeParam1 = AngelTempleBossUpgradeParams[1];
                AngelTempleBossUpgradeParam2 = AngelTempleBossUpgradeParams[2];
                AngelTempleBossUpgradeParam3 = AngelTempleBossUpgradeParams[3];
            }

            /*int nRole = -1;
            nRole = Global.SafeConvertToInt32(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.AngelTempleRole));
            
            if (nRole > 0)
            {
                GameClient client = null;
                client = GameManager.ClientMgr.FindClient(nRole);

                if (client != null)
                {
                    m_sKillBossRoleID = nRole;
                    m_sKillBossRoleName = Global.FormatRoleName(client, client.ClientData.RoleName);
                }
            }*/

            m_sKillBossRoleName = GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.AngelTempleRole);

            for (int i = 0; i < 5; ++i)
            {
                AngelTemplePointInfo tmp = new AngelTemplePointInfo();
                tmp.m_RoleID = 0;
                tmp.m_DamagePoint = 0;
                tmp.m_GetAwardFlag = 0;
                tmp.m_RoleName = "";
                m_PointInfoArray[i] = tmp;
            }

            m_BossHP = 10000;

            SystemXmlItem ItemAngelTempleData = null;

            GameManager.systemAngelTempleData.SystemXmlItemDict.TryGetValue(1, out ItemAngelTempleData);

            if (ItemAngelTempleData == null)
            {
                throw new Exception("AngelTemple Scene ERROR");
            }

            m_AngelTempleData.MapCode           = ItemAngelTempleData.GetIntValue("MapCode");
            m_AngelTempleData.MinChangeLifeNum  = ItemAngelTempleData.GetIntValue("MinZhuangSheng");
            m_AngelTempleData.MinLevel          = ItemAngelTempleData.GetIntValue("MinLevel");

            List<string> strTimeList = new List<string>();
            string[] sField = null;
            string timePoints = ItemAngelTempleData.GetStringValue("TimePoints");
            if (null != timePoints && timePoints != "")
            {
                sField = timePoints.Split(',');
                for (int i = 0; i < sField.Length; i++)
                    strTimeList.Add(sField[i].Trim());
            }
            m_AngelTempleData.BeginTime = strTimeList;

            m_AngelTempleData.PrepareTime = Global.GMax(ItemAngelTempleData.GetIntValue("PrepareSecs"), ItemAngelTempleData.GetIntValue("WaitingEnterSecs"));
            m_AngelTempleData.DurationTime  = ItemAngelTempleData.GetIntValue("FightingSecs");
            m_AngelTempleData.LeaveTime     = ItemAngelTempleData.GetIntValue("ClearRolesSecs");
            m_AngelTempleData.MinPlayerNum  = ItemAngelTempleData.GetIntValue("MinRequestNum");
            m_AngelTempleData.MaxPlayerNum  = ItemAngelTempleData.GetIntValue("MaxEnterNum");
            m_AngelTempleData.BossID        = ItemAngelTempleData.GetIntValue("BossID");
            m_AngelTempleData.BossPosX      = ItemAngelTempleData.GetIntValue("BossPosX");
            m_AngelTempleData.BossPosY      = ItemAngelTempleData.GetIntValue("BossPosY");
        }

        public void GMSetHuoDongStartNow()
        {
            InitAngelTemple();
            m_AngelTempleData.BeginTime = new List<string>() { TimeUtil.NowDateTime().ToString("HH:mm")};
            lock (m_AngelTempleScene)
            {
                m_AngelTempleScene.m_eStatus = AngelTempleStatus.FIGHT_STATUS_NULL;
            }
        }

        /// <summary>
        /// 加载怪物时,按对怪物基础属性进行加强,并记下最大HP
        /// </summary>
        /// <param name="monster"></param>
        public void OnLoadDynamicMonsters(Monster monster)
        {
            m_AngelTempleBoss = monster;
            if (0 == BossBaseHP)
            {
#if ___CC___FUCK___YOU___BB___
                BossBaseHP = monster.XMonsterInfo.MaxHP;
#else
                BossBaseHP = monster.MonsterInfo.VLifeMax;
#endif
            }

            if (AngelTempleMonsterUpgradePercent <= 0.0)
            {
                AngelTempleMonsterUpgradePercent = 1;
            }
            AngelTempleMonsterUpgradePercent = Global.Clamp(AngelTempleMonsterUpgradePercent, 0.001, 1000);
#if ___CC___FUCK___YOU___BB___
            monster.XMonsterInfo.MaxHP = (int)(BossBaseHP * AngelTempleMonsterUpgradePercent);
            monster.VLife = monster.XMonsterInfo.MaxHP;
            m_BossHP = (long)monster.XMonsterInfo.MaxHP;
#else
           monster.MonsterInfo.VLifeMax = BossBaseHP * AngelTempleMonsterUpgradePercent;
             monster.VLife = monster.MonsterInfo.VLifeMax;
            m_BossHP = (long)monster.MonsterInfo.VLifeMax;
#endif

            //monster.MonsterInfo.VLifeMax *= (1 + AngelTempleBossUpgradeParam1 * AngelTempleMonsterUpgradePercent);
            //monster.MonsterInfo.MinAttack = (int)(monster.MonsterInfo.MinAttack * (1 + AngelTempleBossUpgradeParam2 * AngelTempleMonsterUpgradePercent));
            //monster.MonsterInfo.MaxAttack = (int)(monster.MonsterInfo.MaxAttack * (1 + AngelTempleBossUpgradeParam2 * AngelTempleMonsterUpgradePercent));
            //monster.MonsterInfo.Defense = (int)(monster.MonsterInfo.Defense * (1 + AngelTempleBossUpgradeParam3 * AngelTempleMonsterUpgradePercent));
            //monster.MonsterInfo.MDefense = (int)(monster.MonsterInfo.MDefense * (1 + AngelTempleBossUpgradeParam3 * AngelTempleMonsterUpgradePercent));

        }

        /// <summary>
        /// 设置最高积分信息
        /// </summary>
        public void SetTotalPointInfo(string sName, long nPoint)
        {
            m_sTotalDamageName = sName;
            m_nTotalDamageValue = nPoint;
        }

        public void SendTimeInfoToAll(long ticks)
        {
            int nRemainSecs;
            int nStatus;
            lock (m_AngelTempleScene)
            {
                nRemainSecs = (int)((m_AngelTempleScene.m_lStatusEndTime - ticks) / 1000);
                nStatus = (int)m_AngelTempleScene.m_eStatus;
            }

            GameManager.ClientMgr.NotifyAngelTempleMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, m_AngelTempleData.MapCode,
                                                            (int)TCPGameServerCmds.CMD_SPR_ANGELTEMPLETIMERINFO, null, nStatus, nRemainSecs); // 倒计时信息
        }

        public void OnEnterScene(GameClient client)
        {
            SetLeaveFlag(client, false);
            SendTimeInfoToClient(client);
            NotifyInfoToClient(client);
            if (null != m_AngelTempleBoss)
            {
                NotifyInfoToAllClient(m_AngelTempleBoss.VLife);
            }
        }

        public void SendTimeInfoToClient(GameClient client)
        {
            long ticks = TimeUtil.NOW();
            int nRemainSecs;
            int nStatus;
            lock (m_AngelTempleScene)
            {
                nRemainSecs = (int)((m_AngelTempleScene.m_lStatusEndTime - ticks) / 1000);
                nStatus = (int)m_AngelTempleScene.m_eStatus;
            }
            string strcmd = string.Format("{0}:{1}", nStatus, nRemainSecs);  // 1.哪个时间段 2.时间(秒)
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ANGELTEMPLETIMERINFO, strcmd);
        }

        public bool ChangeToNextStatus(out AngelTempleStatus newStatus)
        {
            bool changed = false;
            long ticks = TimeUtil.NOW();
            lock (m_AngelTempleScene)
            {
                if (m_AngelTempleScene.m_eStatus == AngelTempleStatus.FIGHT_STATUS_NULL)
                {
                    if (CanEnterAngelTempleOnTime())
                    {
                        //准备时间,开始入场
                        m_AngelTempleScene.m_eStatus = AngelTempleStatus.FIGHT_STATUS_PREPARE;
                        m_AngelTempleScene.m_lPrepareTime = ticks;
                        m_AngelTempleScene.m_lStatusEndTime = ticks + m_AngelTempleData.PrepareTime * 1000;
                        changed = true;
                    }
                }
                else if (m_AngelTempleScene.m_eStatus == AngelTempleStatus.FIGHT_STATUS_PREPARE)
                {
                    if (ticks >= m_AngelTempleScene.m_lStatusEndTime)
                    {
                        // 开始战斗
                        m_AngelTempleScene.m_eStatus = AngelTempleStatus.FIGHT_STATUS_BEGIN;
                        m_AngelTempleScene.m_lBeginTime = ticks;
                        m_AngelTempleScene.m_lStatusEndTime = ticks + m_AngelTempleData.DurationTime * 1000;
                        changed = true;
                    }
                }
                else if (m_AngelTempleScene.m_eStatus == AngelTempleStatus.FIGHT_STATUS_BEGIN)
                {
                    if (ticks >= m_AngelTempleScene.m_lStatusEndTime || m_AngelTempleScene.m_bEndFlag != 0)
                    {
                        // 战斗结束,准备清场
                        m_AngelTempleScene.m_eStatus = AngelTempleStatus.FIGHT_STATUS_END;
                        m_AngelTempleScene.m_lEndTime = ticks;
                        m_AngelTempleScene.m_lStatusEndTime = ticks + m_AngelTempleData.LeaveTime * 1000;
                        changed = true;
                    }
                }
                else if (m_AngelTempleScene.m_eStatus == AngelTempleStatus.FIGHT_STATUS_END)
                {
                    if (ticks >= m_AngelTempleScene.m_lStatusEndTime)
                    {
                        // 清场时间,所有玩家应清出场景
                        m_AngelTempleScene.m_eStatus = AngelTempleStatus.FIGHT_STATUS_NULL;
                        changed = true;
                    }
                }

                newStatus = m_AngelTempleScene.m_eStatus;
            }

            return changed;
        }

        /// <summary>
        // 心跳处理
        /// </summary>
        public void HeartBeatAngelTempleScene()
        {
            long ticks = TimeUtil.NOW();

            AngelTempleStatus newStatus;
            if (ChangeToNextStatus(out newStatus))
            {
                switch (newStatus)
                {
                    case AngelTempleStatus.FIGHT_STATUS_PREPARE:
                        {
                            //不需要做什么
                            Global.AddFlushIconStateForAll((ushort)ActivityTipTypes.AngelTemple, true);
                        }
                        break;
                    case AngelTempleStatus.FIGHT_STATUS_BEGIN:
                        {
                            lock (m_AngelTempleScene)
                            {
                                bBossKilled = false;
                                m_AngelTempleScene.m_bEndFlag = 0;
                            }

                            // 战斗结束倒计时
                            SendTimeInfoToAll(ticks);

                            // 把天使BOSS刷出来
                            int monsterID = m_AngelTempleData.BossID;

                            GameMap gameMap = null;
                            if (!GameManager.MapMgr.DictMaps.TryGetValue(m_AngelTempleData.MapCode, out gameMap))
                            {
                                LogManager.WriteLog(LogTypes.Error, string.Format("天使神殿报错 地图配置 ID = {0}", m_AngelTempleData.MapCode));
                                return;
                            }

                            int gridX = gameMap.CorrectWidthPointToGridPoint(m_AngelTempleData.BossPosX) / gameMap.MapGridWidth;
                            int gridY = gameMap.CorrectHeightPointToGridPoint(m_AngelTempleData.BossPosY) / gameMap.MapGridHeight;
                            AngelTempleMonsterUpgradePercent = Global.SafeConvertToDouble(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.AngelTempleMonsterUpgradeNumber));
                            GameManager.MonsterZoneMgr.AddDynamicMonsters(m_AngelTempleData.MapCode, monsterID, -1, 1, gridX, gridY, 1);
                        }
                        break;
                    case AngelTempleStatus.FIGHT_STATUS_END:
                        {
                            Global.AddFlushIconStateForAll((ushort)ActivityTipTypes.AngelTemple, false);

                            // 清场倒计时
                            SendTimeInfoToAll(ticks);

                            // 如果BOSS没死 KILL掉
                            if (!bBossKilled && m_AngelTempleBoss != null)
                            {
                                //BOSS未被击杀，下次比例=本次血量*（总伤害/总血量）*80%,下限=当前比例/10
                                MonsterData md = m_AngelTempleBoss.GetMonsterData();
                                double damage = 0;
                                if (md.MaxLifeV != md.LifeV)
                                {
                                    damage = Global.Clamp(md.MaxLifeV - md.LifeV, md.MaxLifeV / 10, md.MaxLifeV);
                                    AngelTempleMonsterUpgradePercent *= damage * 0.8 / md.MaxLifeV;
                                    Global.UpdateDBGameConfigg(GameConfigNames.AngelTempleMonsterUpgradeNumber, AngelTempleMonsterUpgradePercent.ToString("0.00"));
                                }

                                GameManager.MonsterMgr.AddDelayDeadMonster(m_AngelTempleBoss);
                                GameManager.ClientMgr.NotifyAngelTempleMsgBossDisappear(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, m_AngelTempleData.MapCode);
                                LogManager.WriteLog(LogTypes.SQL, string.Format("天使神殿Boss未死亡,血量减少百分比{0:P} ,Boss生命值比例成长为{1}", damage / md.MaxLifeV, AngelTempleMonsterUpgradePercent));
                                m_AngelTempleBoss = null;
                            }

                            // 天使神殿结束战斗,如果杀死了Boss,客户端显示奖励界面(倒计时界面)
                            GiveAwardAngelTempleScene(bBossKilled);
                        }
                        break;
                    case AngelTempleStatus.FIGHT_STATUS_NULL:
                        {
                            // 清场
                            List<Object> objsList = GameManager.ClientMgr.GetMapClients(m_AngelTempleData.MapCode);
                            if (objsList != null)
                            {
                                for (int n = 0; n < objsList.Count; ++n)
                                {
                                    GameClient c = objsList[n] as GameClient;
                                    if (c == null)
                                        continue;

                                    if (c.ClientData.MapCode != m_AngelTempleData.MapCode)
                                        continue;

                                    // 根据公式和积分奖励经验
                                    //GiveAwardAngelTempleScene(c);

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

                            CleanUpAngelTempleScene();
                            if (ticks >= (m_AngelTempleScene.m_lEndTime + (m_AngelTempleData.LeaveTime * 20000)))
                            {
                                m_AngelTempleScene.m_eStatus = AngelTempleStatus.FIGHT_STATUS_NULL;
                            }
                        }
                        break;
                }
            }
            if (newStatus == AngelTempleStatus.FIGHT_STATUS_BEGIN)
            {
                //如果需要在这期间进行一些定时操作,写在这里
            }
        }

        /// <summary>
        /// 同步信息给客户端
        /// </summary>
        public void NotifyInfoToAllClient(double nBossHP)
        {
            //long lTicks = TimeUtil.NOW();

            //if (lTicks >= (m_NotifyInfoTickForAll + m_NotifyInfoDelayTick))
            {
                //m_NotifyInfoTickForAll = lTicks;
                lock (m_PointDamageInfoMutex)
                {
                    GameManager.ClientMgr.NotifyAngelTempleMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                                m_AngelTempleData.MapCode, (int)TCPGameServerCmds.CMD_SPR_ANGELTEMPLEFIGHTINFOALL, m_PointInfoArray, 0, 0, 0, 0 ,0, nBossHP);
                }
            }
            
        }

        /// <summary>
        /// 同步信息给客户端
        /// </summary>
        public void NotifyInfoToClient(GameClient client)
        {
            //long lTicks = TimeUtil.NOW();

            //if (lTicks >= (m_NotifyInfoTickForSingle + m_NotifyInfoDelayTick))
            {
                //m_NotifyInfoTickForSingle = lTicks;

                string strName = Global.FormatRoleName(client, client.ClientData.RoleName);

                double dValue = Math.Round(((double)client.ClientData.AngelTempleCurrentPoint / (double)GameManager.AngelTempleMgr.m_BossHP), 2);

                string strcmd = string.Format("{0}:{1}", strName, dValue);

                GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_ANGELTEMPLEFIGHTINFOSINGLE);
            }

        }

        /// <summary>
        /// 攻击BOSS
        /// </summary>
        public void ProcessAttackBossInAngelTempleScene(GameClient client, Monster monster, int nDamage)
        {
            AngelTemplePointInfo tmpInfo;
            lock (m_PointDamageInfoMutex)
            {
                if (!m_RoleDamageAngelValue.TryGetValue(client.ClientData.RoleID, out tmpInfo))
                {
                    tmpInfo = new AngelTemplePointInfo();
                    tmpInfo.m_RoleID = client.ClientData.RoleID;
                    tmpInfo.m_DamagePoint = nDamage;
                    tmpInfo.m_GetAwardFlag = 0;
                    tmpInfo.m_RoleName = Global.FormatRoleName(client, client.ClientData.RoleName);
                    m_RoleDamageAngelValue.Add(client.ClientData.RoleID, tmpInfo);
                }
                else
                {
                    tmpInfo.m_DamagePoint += nDamage;
                }

                if (tmpInfo.CompareTo(m_PointInfoArray[4]) < 0)
                {
                    if (tmpInfo.Ranking < 0)
                    {
                        m_PointInfoArray[5] = tmpInfo;
                        tmpInfo.Ranking = 1; //不看具体排名了,随意设个吧
                    }
                    Array.Sort(m_PointInfoArray, AngelTemplePointInfo.Compare_static);
                    if (null != m_PointInfoArray[5])
                    {
                        m_PointInfoArray[5].Ranking = -1;
                    }
                }
            }

            client.ClientData.AngelTempleCurrentPoint = tmpInfo.m_DamagePoint;
            if (client.ClientData.AngelTempleCurrentPoint > client.ClientData.AngelTempleTopPoint)
            {
                client.ClientData.AngelTempleTopPoint = client.ClientData.AngelTempleCurrentPoint;
            }

            if (tmpInfo.m_DamagePoint > m_nTotalDamageValue)
            {
                string strName = Global.FormatRoleName(client, client.ClientData.RoleName);

                SetTotalPointInfo(strName, tmpInfo.m_DamagePoint);
            }

            long lTicks = TimeUtil.NOW();
            int percent = (int)(100.0 * monster.VLife / m_BossHP);
            if (lTicks >= (m_NotifyInfoTickForSingle + m_NotifyInfoDelayTick) || percent != m_LastNotifyBossHPPercent)
            {
                m_LastNotifyBossHPPercent = percent;
                m_NotifyInfoTickForSingle = lTicks;

                NotifyInfoToClient(client);
                NotifyInfoToAllClient(monster.VLife);
            }
        }

        /// <summary>
        /// 给予奖励
        /// </summary>
        /// <param name="client"></param>
        public void GiveAwardAngelTempleScene(bool bBossKilled)
        {
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(m_AngelTempleData.MapCode); //发送给所有地图的用户
            if (null == objsList)
                return;

            //拷贝一份伤害列表
            int roleCount = 0;
            List<AngelTemplePointInfo> pointList = new List<AngelTemplePointInfo>();
            lock (m_PointDamageInfoMutex)
            {
                for (int i = 0; i < objsList.Count; ++i)
                {
                    if (!(objsList[i] is GameClient))
                        continue;

                    GameClient client = (objsList[i] as GameClient);

                    AngelTemplePointInfo tmpInfo;
                    if (!m_RoleDamageAngelValue.TryGetValue(client.ClientData.RoleID, out tmpInfo))
                    {
                        SendAngelTempleAwardMsg(client, -1, 0, 0, Global.GetLang("无"), "", bBossKilled);
                        continue;
                    }
                    if (tmpInfo.LeaveScene)
                    {
                        continue;
                    }
                    if (Interlocked.CompareExchange(ref tmpInfo.m_GetAwardFlag, 1, 0) != 0)
                        continue;

                    if (tmpInfo.m_DamagePoint < AngelTempleMinHurt)
                    {
                        SendAngelTempleAwardMsg(client, -1, 0, 0, Global.GetLang("无"), "", bBossKilled);
                        continue;
                    }

                    roleCount++;
                    pointList.Add(tmpInfo);
                }
            }

            //按伤害排序
            pointList.Sort(AngelTemplePointInfo.Compare_static); //从大到小排序

            //计算排名奖励
            if (bBossKilled)
            {
                foreach (var kv in GameManager.AngelTempleAward.SystemXmlItemDict)
                {
                    if (null != kv.Value)
                    {
                        int id = kv.Value.GetIntValue("ID");
                        int minPaiMing = kv.Value.GetIntValue("MinPaiMing");
                        int maxPaiMing = kv.Value.GetIntValue("MaxPaiMing");
                        int shengWang = kv.Value.GetIntValue("ShengWang");
                        int gold = kv.Value.GetIntValue("Gold");
                        string goodsStr = kv.Value.GetStringValue("Goods");

                        minPaiMing = Global.GMax(0, minPaiMing - 1); 
                        maxPaiMing = Global.GMin(10000, maxPaiMing - 1);
                        for (int i = minPaiMing; i <= maxPaiMing && i < roleCount; i++)
                        {
                            pointList[i].m_AwardPaiMing = i + 1;
                            pointList[i].m_AwardShengWang += shengWang;
                            pointList[i].m_AwardGold += gold;
                            pointList[i].GoodsList.AddNoRepeat(goodsStr);
                        }
                    }
                }

                //计算幸运奖励
                int[] luckPaiMings = new int[roleCount];
                for (int i = 0; i < roleCount; i++ )
                {
                    luckPaiMings[i] = i;
                }
                int luckAwardsCount = 0; //幸运奖励计数
                foreach (var kv in GameManager.AngelTempleLuckyAward.SystemXmlItemDict)
                {
                    if (null != kv.Value)
                    {
                        int awardID = kv.Value.GetIntValue("ID");
                        int awardNum = kv.Value.GetIntValue("Number");
                        string luckAwardsName = Global.GetLang(kv.Value.GetStringValue("Name"));
                        string luckAwardGoods = kv.Value.GetStringValue("Goods");
                        for (int count = 0; count < awardNum && luckAwardsCount < roleCount; count++, luckAwardsCount++)
                        {
                            int rand = Global.GetRandomNumber(luckAwardsCount, roleCount); //在剩下的角色里面随机抽取一个
                            int t = luckPaiMings[luckAwardsCount];
                            luckPaiMings[luckAwardsCount] = luckPaiMings[rand];
                            luckPaiMings[rand] = t;

                            int index = luckPaiMings[luckAwardsCount]; //此幸运获奖者在伤害排行中的位置
                            //pointList[index].m_LuckPaiMingID = awardID;
                            pointList[index].m_LuckPaiMingName = luckAwardsName;
                            pointList[index].GoodsList.AddNoRepeat(luckAwardGoods);
                        }
                    }
                }
            }
            else
            {
                SystemXmlItem xmlItem = null;
                foreach (var kv in GameManager.AngelTempleAward.SystemXmlItemDict)
                {
                    if (null != kv.Value)
                    {
                        xmlItem = kv.Value;
                    }
                }

                if (null != xmlItem)
                {
                    int id = xmlItem.GetIntValue("ID");
                    int shengWang = xmlItem.GetIntValue("ShengWang");
                    int gold = xmlItem.GetIntValue("Gold");
                    string goodsStr = xmlItem.GetStringValue("Goods");

                    for (int i = 0; i < roleCount; i++)
                    {
                        pointList[i].m_AwardPaiMing = -1;
                        pointList[i].m_LuckPaiMingName = Global.GetLang("无");
                        pointList[i].m_AwardShengWang = shengWang;
                        pointList[i].m_AwardGold = gold;
                        pointList[i].GoodsList.AddNoRepeat(goodsStr);
                    }
                }
            }

            // 节日活动的多倍处理
            double awardmuti = 1.0;
            JieRiMultAwardActivity activity = HuodongCachingMgr.GetJieRiMultAwardActivity();
            if (null != activity)
            {
                JieRiMultConfig config = activity.GetConfig((int)MultActivityType.AngelTemple);
                if (null != config)
                {
                    awardmuti += config.GetMult();
                }
            }

            if (awardmuti > 1.0)
            { 
                foreach (var dp in pointList)
                {
                    dp.m_AwardGold = (int)(dp.m_AwardGold * awardmuti);
                    dp.m_AwardShengWang = (int)(dp.m_AwardShengWang * awardmuti);
                    foreach (var item in dp.GoodsList.Items)
                    {
                        item.GoodsNum = (int)(item.GoodsNum * awardmuti);
                    }
                }            
            }

            //发放奖励
            foreach (var dp in pointList)
            {
                GameClient gc = GameManager.ClientMgr.FindClient(dp.m_RoleID);
                if (null != gc)
                {
                    if (dp.m_AwardGold > 0)
                    {
                        GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, gc, dp.m_AwardGold, "天使神殿奖励");
                    }
                    if (dp.m_AwardShengWang > 0)
                    {
                        GameManager.ClientMgr.ModifyShengWangValue(gc, dp.m_AwardShengWang, "天使神殿", true);
                        //GameManager.ClientMgr.ModifyZhanHunValue(gc, dp.m_AwardShengWang, false, true);
                    }

                    foreach (var item in dp.GoodsList.Items)
                    {
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, gc, item.GoodsID, item.GoodsNum, 0, "", item.Level, item.Binding , 0, "", true, 1, "天使神殿奖励物品", Global.ConstGoodsEndTime, 0, 0, item.IsHaveLuckyProp, 0, item.ExcellencePorpValue, item.AppendLev);
                    }
                    SendAngelTempleAwardMsg(gc, dp.m_AwardPaiMing, dp.m_AwardGold, dp.m_AwardShengWang, dp.m_LuckPaiMingName, dp.GoodsList.ToString(), bBossKilled);
                }
            }
        }

        private void SendAngelTempleAwardMsg(GameClient client, int paiMing, int awardGold, int awardShengWang, string luckPaiMingName, string goodsString, bool success)
        {
            string strcmd;
            // 1.伤害排名 2.伤害奖励金币 3.伤害奖励声望 4.幸运奖名词 5 奖励物品字符串 6 是否胜利
            if (client.CodeRevision >= 2)
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", paiMing, awardGold, awardShengWang, luckPaiMingName, goodsString, success ? 1 : 0);
            }
            else
            {
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", paiMing, awardGold, awardShengWang, luckPaiMingName, goodsString);
            }

            GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_SPR_ANGELTEMPLEFIGHTEND);
        }

        private void SetLeaveFlag(GameClient client, bool leaveFlag)
        {
            AngelTemplePointInfo tmpInfo = null;
            lock (m_PointDamageInfoMutex)
            {
                if (m_RoleDamageAngelValue.TryGetValue(client.ClientData.RoleID, out tmpInfo))
                {
                    tmpInfo.LeaveScene = leaveFlag; //离开状态,即使在线也不发放奖励
                }
            }
        }

        /// <summary>
        // 玩家离开天使神殿
        /// </summary>
        public void LeaveAngelTempleScene(GameClient client, bool logout = false)
        {
            SetLeaveFlag(client, true);
            if (client.ClientData.MapCode != m_AngelTempleData.MapCode && !client.ClientData.bIsInAngelTempleMap)
                return;


            Interlocked.Decrement(ref m_AngelTempleScene.m_nPlarerCount);

            client.ClientData.bIsInAngelTempleMap = false;
            if (logout)
            {
                // 设置MAPCODEID  和 位置信息
                client.ClientData.MapCode = client.ClientData.LastMapCode;
                client.ClientData.PosX = client.ClientData.LastPosX;
                client.ClientData.PosY = client.ClientData.LastPosY;
            }
        }

        /// <summary>
        /// 是否在进入天使神殿的时间段
        /// </summary>
        /// <returns></returns>
        public bool CanEnterAngelTempleOnTime()
        {
            lock (m_AngelTempleScene)
            {
                if (m_AngelTempleScene.m_eStatus >= AngelTempleStatus.FIGHT_STATUS_PREPARE && m_AngelTempleScene.m_eStatus < AngelTempleStatus.FIGHT_STATUS_END)
                {
                    return true;
                }
            }

            DateTime now = TimeUtil.NowDateTime();
            string nowTime = now.ToString("HH:mm");
            List<string> timePointsList = m_AngelTempleData.BeginTime;

            if (null == timePointsList)
                return false;

            for (int i = 0; i < timePointsList.Count; i++)
            {
                DateTime staticTime = DateTime.Parse(timePointsList[i]);
                DateTime perpareTime = staticTime.AddMinutes((double)(m_AngelTempleData.PrepareTime / 60));

                if (timePointsList[i] == nowTime || (now > staticTime && now <= perpareTime))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 激活战鼓BUFF
        /// </summary>
        /// <param name="client"></param>
        /// <param name="notifyPropsChanged"></param>
        /// <returns></returns>
        public bool AddBuffer(GameClient client, BufferItemTypes buffID, double[] newParams, bool notifyPropsChanged)
        {
            BufferData bufferData1 = null;

            if (buffID == BufferItemTypes.MU_ANGELTEMPLEBUFF1)
            {
                Global.RemoveBufferData(client, (int)BufferItemTypes.MU_ANGELTEMPLEBUFF2);
            }
            else if (buffID == BufferItemTypes.MU_ANGELTEMPLEBUFF2)
            {
                Global.RemoveBufferData(client, (int)BufferItemTypes.MU_ANGELTEMPLEBUFF1);
            }

            int nIndex = 0;

            int nOldBufferGoodsIndexID = -1;
            BufferData bufferData = Global.GetBufferDataByID(client, (int)buffID);
            if (null != bufferData && !Global.IsBufferDataOver(bufferData))
            {
                nOldBufferGoodsIndexID = (int)bufferData.BufferVal;
            }

            if (nOldBufferGoodsIndexID == nIndex)
            {
                return false;
            }

            //更新BufferData
            double[] actionParams = new double[2];
            actionParams = newParams;
            Global.UpdateBufferData(client, buffID, actionParams, 1, notifyPropsChanged);
            if (notifyPropsChanged)
            {
                //通知客户端属性变化
                //GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }

            return true;
        }

        /// <summary>
        /// 击杀BOSS
        /// </summary>
        public void KillAngelBoss(GameClient client, Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            //确保是这个场景的这个Boss
            if (m_AngelTempleData.BossID != monster.XMonsterInfo.MonsterId)
            {
                return;
            }
#else
           //确保是这个场景的这个Boss
            if (m_AngelTempleData.BossID != monster.MonsterInfo.ExtensionID)
            {
                return;
            }
#endif


            lock (m_AngelTempleScene)
            {
                bBossKilled = true;
                m_AngelTempleScene.m_bEndFlag = 1;
            }

            string sName = "";
            sName = Global.FormatRoleName(client, client.ClientData.RoleName);
            
            m_sKillBossRoleName = sName;

            Global.UpdateDBGameConfigg(GameConfigNames.AngelTempleRole, m_sKillBossRoleName);

            NotifyInfoToClient(client);
            NotifyInfoToAllClient(monster.VLife);

            m_AngelTempleScene.m_nKillBossRole = client.ClientData.RoleID;
            m_sKillBossRoleID = client.ClientData.RoleID;

            //BOSS被击杀，下次比例=本次血量/（击杀秒数/（总秒数*80%））, 比例上限=当前比例*10
            double usedTime = (TimeUtil.NOW() - m_AngelTempleScene.m_lBeginTime) / 1000;
            double usedTime2 = (double)Global.Clamp(usedTime, m_AngelTempleData.DurationTime / 10, m_AngelTempleData.DurationTime);
            AngelTempleMonsterUpgradePercent *= m_AngelTempleData.DurationTime * 0.8 / usedTime2;
            Global.UpdateDBGameConfigg(GameConfigNames.AngelTempleMonsterUpgradeNumber, AngelTempleMonsterUpgradePercent.ToString("0.00"));
            LogManager.WriteLog(LogTypes.SQL, string.Format("天使神殿Boss被击杀,用时{0}秒 ,Boss生命值比例成长为{1}", usedTime, AngelTempleMonsterUpgradePercent));
            m_AngelTempleBoss = null;
        }

        /// <summary>
        // 清空处理
        /// </summary>
        public void CleanUpAngelTempleScene()
        {
            m_AngelTempleScene.CleanAll();
            lock (m_PointDamageInfoMutex)
            {
                m_RoleDamageAngelValue.Clear();
                for (int i = 0; i < m_PointInfoArray.Length; ++i)
                {
                    if (null != m_PointInfoArray[i])
                    {
                        m_PointInfoArray[i] = new AngelTemplePointInfo();
                    }
                }
            }
        }

        // add by chenjingui. 20150704 角色改名后，检测是否更新最高积分者
        public void OnChangeName(int roleId, string oldName, string newName)
        {
            if (!string.IsNullOrEmpty(oldName) && !string.IsNullOrEmpty(newName))
            {
                if (!string.IsNullOrEmpty(m_sTotalDamageName) && m_sTotalDamageName == oldName)
                {
                    m_sTotalDamageName = newName;
                }
            }
        }
    }
}
