#define UseTimer
#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;
using GameServer.Server;
using Server.Protocol;
using Server.Data;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using Server.Tools;
using GameServer.Server.CmdProcesser;
using GameServer.Logic.Talent;
using GameServer.Logic.ActivityNew.SevenDay;

namespace GameServer.Logic.JingJiChang
{
    /// <summary>
    /// 竞技场静态常量配置
    /// </summary>
    public class JingJiChangConstants
    {
        /// <summary>
        /// 机器人出生的XY坐标
        /// </summary>
        public static readonly int RobotBothX = 4684;
        public static readonly int RobotBothY = 4684;

        /// <summary>
        /// 每次可选择的挑战玩家的数量
        /// </summary>
        public static readonly int CanChallengeNum = 3;

        /// <summary>
        /// 竞技场排行榜最大排名
        /// </summary>
        public static readonly int RankingListMaxNum = 500;

        /// <summary>
        /// 免费进入
        /// </summary>
        public static readonly int Enter_Type_Free = 0;

        /// <summary>
        /// vip进入
        /// </summary>
        public static readonly int Enter_Type_Vip = 1;

        /// <summary>
        /// 挑战冷却时间
        /// </summary>
        public static readonly long Challenge_CD_Time = 3 * TimeUtil.MINITE;

        /// <summary>
        /// 排行榜领取奖励冷却时间
        /// </summary>
        public static readonly long RankingReward_CD_Time = 1 * TimeUtil.DAY;

        /// <summary>
        /// AI战士技能列表
        /// </summary>
        public static readonly int[] ZhanShiSkillList = new int[] {120, 105, 106, 100, 187, 188, 189, 190};
        public static readonly int[] ZhanShiFiveCombotSkillList = new int[] { 100, 187, 188, 189, 190 };

        /// <summary>
        /// AI法师技能列表
        /// </summary>
        public static readonly int[] FaShiSkillList = new int[] {204, 206, 220, 200, 287, 288, 289, 290};
        public static readonly int[] FaShiFiveCombotSkillList = new int[] { 200, 287, 288, 289, 290 };

        /// <summary>
        /// AI弓箭手技能列表
        /// </summary>
        public static readonly int[] GongJianShouSkillList = new int[] {305, 301, 306, 300, 388, 389, 390, 391};
        public static readonly int[] GongJianShouFiveCombotSkillList = new int[] { 300, 388, 389, 390, 391 };

        /// <summary>
        /// AI力魔剑士技能列表
        /// </summary>
        public static readonly int[] StrMagicSwordSkillList = new int[] { 10007, 10001, 10004, 10000, 10088, 10089, 10090, 10091 }; // 3个技能+5个普攻Combo
        public static readonly int[] StrMagicSwordFiveCombotSkillList = new int[] { 10000, 10088, 10089, 10090, 10091 }; // 普攻Combo

        /// <summary>
        /// AI智魔剑士技能列表
        /// </summary>
        public static readonly int[] IntMagicSwordSkillList = new int[] { 10107, 10101, 10104, 10100, 10188, 10189, 10190, 10191 }; // 3个技能+5个普攻Combo
        public static readonly int[] IntMagicSwordFiveCombotSkillList = new int[] { 10100, 10188, 10189, 10190, 10191 }; // 普攻Combo

        /// <summary>
        /// [技能， 吟唱tick， 使用tick]
        /// </summary>
        public static readonly int[][] SkillFrameCounts = new int[][] {
            new int[]{120,11, 5},new int[]{105,15, 9},new int[]{106,11, 5},new int[]{100,2, 2},new int[]{187,2, 2},new int[]{188,2, 2},new int[]{189,2, 2},new int[]{190,7, 4},//战士
            new int[]{204,11, 6},new int[]{206,15, 6},new int[]{220,15, 6},new int[]{200,2, 2},new int[]{287,2, 2},new int[]{288,2, 2},new int[]{289,2, 2},new int[]{290,7, 4},//法师
            new int[]{305,11, 6},new int[]{301,11, 6},new int[]{306,5, 6},new int[]{300,2, 2},new int[]{388,2, 2},new int[]{389,2, 2},new int[]{390,2, 2},new int[]{391,7, 4},//弓箭手
            new int[]{10007,11, 4},new int[]{10001,13, 4},new int[]{10004,11, 4},new int[]{10000,3, 3},new int[]{10088,3, 3},new int[]{10089,3, 3},new int[]{10090,7, 6},new int[]{10091,7, 4}, //力魔剑士 [XSea 2015/6/11]
            new int[]{10107,15, 5},new int[]{10101,15, 4},new int[]{10104,11, 5},new int[]{10100,3, 3},new int[]{10188,4, 4},new int[]{10189,3, 3},new int[]{10190,3, 3},new int[]{10191,7, 4} //智魔剑士 [XSea 2015/6/11]
        };

        /// <summary>
        /// 得到AI职业技能
        /// </summary>
        /// <param name="eOccupation">职业</param>
        /// <param name="eMagicSwordType">魔剑士分支类型</param>
        public static int[] GetJingJiChangeSkillList(int eOccupation, EMagicSwordTowardType eMagicSwordType)
        {
            //switch ((EOccupationType)eOccupation)
            //{
            //    case EOccupationType.EOT_Warrior:       return JingJiChangConstants.ZhanShiSkillList; break;
            //    case EOccupationType.EOT_Magician:      return JingJiChangConstants.FaShiSkillList; break;
            //    case EOccupationType.EOT_Bow:           return JingJiChangConstants.GongJianShouSkillList; break;
            //    case EOccupationType.EOT_MagicSword:
            //        // 魔剑士分力魔与智魔
            //        switch (eMagicSwordType)
            //        {
            //            case EMagicSwordTowardType.EMST_Strength: // 力魔
            //                return JingJiChangConstants.StrMagicSwordSkillList; 
            //                break;
            //            case EMagicSwordTowardType.EMST_Intelligence: // 智魔
            //                return JingJiChangConstants.IntMagicSwordSkillList; 
            //                break;
            //        }
            //        break;
            //}
            return null;
        }

        /// <summary>
        /// 得到AI普攻
        /// </summary>
        /// <param name="eOccupation">职业</param>
        /// <param name="eMagicSwordType">魔剑士分支类型</param>
        public static int[] getJingJiChangeFiveCombatSkillList(int eOccupation, EMagicSwordTowardType eMagicSwordType)
        {
            //switch ((EOccupationType)eOccupation)
            //{
            //    case EOccupationType.EOT_Warrior:       return JingJiChangConstants.ZhanShiFiveCombotSkillList; break;
            //    case EOccupationType.EOT_Magician:      return JingJiChangConstants.FaShiFiveCombotSkillList; break;
            //    case EOccupationType.EOT_Bow:           return JingJiChangConstants.GongJianShouFiveCombotSkillList; break;
            //    case EOccupationType.EOT_MagicSword:
            //        // 魔剑士分力魔与智魔
            //        switch (eMagicSwordType)
            //        {
            //            case EMagicSwordTowardType.EMST_Strength: // 力魔
            //                return JingJiChangConstants.StrMagicSwordFiveCombotSkillList;
            //                break;
            //            case EMagicSwordTowardType.EMST_Intelligence: // 智魔
            //                return JingJiChangConstants.IntMagicSwordFiveCombotSkillList;
            //                break;
            //        }
            //        break;
            //}
            return null;
        }

        /// <summary>
        /// [废]
        /// </summary>
        public static readonly int[] SkillList = new int[] { 120, 105, 106, 100, 187, 188, 189, 190, 204, 206, 220, 200, 287, 288, 289, 290, 305, 301, 306, 300, 388, 389, 390, 391 };

        /// <summary>
        /// AI战士技能动画帧数[废]
        /// </summary>
        public static readonly int[] ZhanShiSkillFrameCounts = new int[] { 11, 16, 11, 5, 5, 6, 6, 8 };

        /// <summary>
        /// AI法师技能列表[废]
        /// </summary>
        public static readonly int[] FaShiSkillFrameCounts = new int[] { 11, 13, 11, 5, 5, 6, 6, 8 };

        /// <summary>
        /// AI弓箭手技能列表[废]
        /// </summary>
        public static readonly int[] GongJianShouSkilFrameCounts = new int[] { 11, 11, 11, 5, 5, 6, 6, 8 };

    }

    public class ResultCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        public static readonly int Success = 1;

        /// <summary>
        /// 非法参数
        /// </summary>
        public static readonly int Illegal = 0;

        /// <summary>
        /// 战斗时不允许请求
        /// </summary>
        public static readonly int Combat_Error = -1;

        /// <summary>
        /// 死亡时不允许请求
        /// </summary>
        public static readonly int Dead_Error = -2;

        /// <summary>
        /// 免费次数不够
        /// </summary>
        public static readonly int FreeNum_Error = -3;

        /// <summary>
        /// vip次数不够
        /// </summary>
        public static readonly int VipNum_Error = -4;

        /// <summary>
        /// 付费失败
        /// </summary>
        public static readonly int Pay_Error = -5;

        /// <summary>
        /// 钻石不够
        /// </summary>
        public static readonly int Money_Not_Enough_Error = -6;

        /// <summary>
        /// 无效地图
        /// </summary>
        public static readonly int Map_Error = -7;

        /// <summary>
        /// 无效副本顺序
        /// </summary>
        public static readonly int FubenSeqId_Error = -8;

        /// <summary>
        /// 冷却时间未到
        /// </summary>
        public static readonly int Challenge_CD_Error = -9;

        /// <summary>
        /// 被挑战者不存在
        /// </summary>
        public static readonly int BeChallenger_Null_Error = -10;

        /// <summary>
        /// 被挑战者排名已改变
        /// </summary>
        public static readonly int BeChallenger_Ranking_Change_Error = -11;

        /// <summary>
        /// 被挑战者正在被其他玩家挑战
        /// </summary>
        public static readonly int BeChallenger_Lock_Error = -12;

        /// <summary>
        /// 排行榜领取奖励未冷却
        /// </summary>
        public static readonly int RankingReward_CD_Error = -13;

        /// <summary>
        /// 没有军衔
        /// </summary>
        public static readonly int Junxian_Null_Error = -14;

        /// <summary>
        /// 当前有军衔Buff
        /// </summary>
        public static readonly int HasJunxianBuff_Error = -15;

        /// <summary>
        /// 声望值不够
        /// </summary>
        public static readonly int ShengWang_Not_Enough_Error = -16;

    }

    /// <summary>
    /// 竞技场管理器
    /// </summary>
    public class JingJiChangManager : JingJiChangConstants, IManager
    {
        private static JingJiChangManager instance = new JingJiChangManager();

        /// <summary>
        /// 竞技场奖励数据
        /// </summary>
        private SystemXmlItems rewardConfig = new SystemXmlItems();
        
        /// <summary>
        /// 竞技场参数数据
        /// </summary>
        private SystemXmlItems jingjiMainConfig = new SystemXmlItems();
        
        /// <summary>
        /// 竞技场军衔数据
        /// </summary>
        private SystemXmlItems junxianConfig = new SystemXmlItems();

        /// <summary>
        /// 竞技场副本数据
        /// </summary>
        private SystemXmlItem jingjiFuBenItem = null;

        /// <summary>
        /// 竞技场地图编号
        /// </summary>
        private int nJingJiChangMapCode = 0;

#if !UseTimer
        /// <summary>
        /// 任务调度器
        /// </summary>
        private ScheduleExecutor executor = null;
#endif        
        /// <summary>
        /// 竞技场缓存
        /// </summary>
        private Dictionary<int, JingJiChangInstance> jingjichangInstances = new Dictionary<int, JingJiChangInstance>();

        /// <summary>
        /// 竞技场副本ID
        /// </summary>
        private int jingjiFuBenId = -1;

        /// <summary>
        /// 竞技场AI Buff
        /// </summary>
        private int jingjiBuffId = -1;

        /// <summary>
        /// 竞技场数据激活最小转生等级
        /// </summary>
        private int jingjiFuBenMinZhuanSheng = -1;

        /// <summary>
        /// 军衔Buff持续时间配置
        /// </summary>
        private string[] junxianBuffTimeConfig;

        private JingJiChangManager() { }

        public static JingJiChangManager getInstance()
        {
            return instance;
        }


        public bool initialize()
        {

            loadStaticData();

            initCmdProcessor();

            initListener();
#if !UseTimer
            //分配10个线程
            executor = new ScheduleExecutor(10);
#endif
            return true;
        }

        /// <summary>
        /// 加载静态数据
        /// </summary>
        private void loadStaticData()
        {
            //加载竞技场配置
            jingjiMainConfig.LoadFromXMlFile("Config/JingJi.xml", "", "ID");

            //加载军衔配置
            junxianConfig.LoadFromXMlFile("Config/JunXian.xml", "", "Level");

            //竞技场副本ID
            jingjiFuBenId = (int)GameManager.systemParamsList.GetParamValueIntByName("JingJiFuBenID");
            
            //竞技场AI Buff
            jingjiBuffId = (int)GameManager.systemParamsList.GetParamValueIntByName("JingJiBuff");

            GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(jingjiFuBenId, out jingjiFuBenItem);

            nJingJiChangMapCode = jingjiFuBenItem.GetIntValue("MapCode");

            jingjiFuBenMinZhuanSheng = jingjiFuBenItem.GetIntValue("MinZhuanSheng");
        }

        /// <summary>
        /// 初始化指令处理器
        /// </summary>
        private void initCmdProcessor()
        {
            //竞技场详情
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJI_DETAIL, 2, JingJiDetailCmdProcessor.getInstance());

            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_GET_ROLE_LOOKS, 2, JingJiGetRoleLooksCmdProcessor.getInstance());
            //竞技场请求挑战
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJI_REQUEST_CHALLENGE, 4, JingJiRequestChallengeCmdProcessor.getInstance());
            //竞技场战报
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJI_CHALLENGEINFO, 2, JingJiChallengeInfoCmdProcessor.getInstance());
            //领取竞技场排行榜奖励
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJI_RANKING_REWARD, 1, JingJiRankingRewardCmdProcessor.getInstance());
            //竞技场消除挑战CD
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_REMOVE_CD, 1, JingJiRemoveCDCmdProcessor.getInstance());
            //领取军衔Buff
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_GET_BUFF, 2, JingJiGetBuffCmdProcessor.getInstance());
            //军衔升级
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_JUNXIAN_LEVELUP, 1, JingJiJunxianLevelupCmdProcessor.getInstance());
            //玩家离开副本
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_LEAVE, 1, JingJiLeaveFuBenCmdProcessor.getInstance());
            //竞技场战斗开始
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_JINGJI_START_FIGHT, 1, JingJiStartFightCmdProcessor.getInstance());

        }

        /// <summary>
        /// 初始化监听器
        /// </summary>
        private void initListener()
        {
            //玩家升级事件监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerLevelup, JingJiPlayerLevelupEventListener.getInstance());

            //玩家死亡事件监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerDead, JingJiFuBenEndEventListener.getInstance());

            //玩家副本结束事件监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, JingJiFuBenEndEventListener.getInstance());

            //玩家登出事件监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerLogout, JingJiPlayerLogoutEventListener.getInstance());

            //玩家离开副本事件监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerLeaveFuBen, JingJiPlayerLeaveFuBenEventListener.getInstance());
        }

        public bool startup()
        {
#if !UseTimer
            executor.start();
#endif
            return true;
        }

        public bool showdown()
        {
#if !UseTimer
            executor.stop();
#endif
            return true;
        }

        public bool destroy()
        {
#if !UseTimer
            executor = null;
#endif
            removeListener();

            if (null != jingjichangInstances)
            {
                lock (jingjichangInstances)
                {
                    jingjichangInstances.Clear();
                }

                // jingjichangInstances = null;
            }

            return true;
        }

        /// <summary>
        /// 删除监听器
        /// </summary>
        private void removeListener()
        {
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerLevelup, JingJiPlayerLevelupEventListener.getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerDead, JingJiFuBenEndEventListener.getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, JingJiFuBenEndEventListener.getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerLogout, JingJiPlayerLogoutEventListener.getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerLeaveFuBen, JingJiPlayerLeaveFuBenEventListener.getInstance());
        }

        /// <summary>
        /// 获取玩家竞技场详情
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public JingJiDetailData getDetailData(GameClient player, int requestType = 0)
        {
            JingJiDetailData detailData = new JingJiDetailData();

            if (player.ClientData.Level < jingjiFuBenItem.GetIntValue("MinLevel")
              && player.ClientData.ChangeLifeCount == jingjiFuBenItem.GetIntValue("MinZhuanSheng"))
            {
                //非法数据
                detailData.state = ResultCode.Illegal;

                return detailData;
            }

            if (requestType != 0 && requestType != 1) 
            {
                //非法数据
                detailData.state = ResultCode.Illegal;

                return detailData;
            }

            //战斗时不允许请求
            //if (!checkAction(player))
            //{
            //    detailData.state = ResultCode.Combat_Error;
            //    return detailData;
            //}

            //死亡时不允许请求
            if (player.ClientData.CurrentLifeV <= 0 || player.ClientData.CurrentAction == (int)GActions.Death)
            {
                detailData.state = ResultCode.Dead_Error;
                return detailData;
            }

            //获取玩家竞技场数据
            PlayerJingJiData jingjiData = Global.sendToDB<PlayerJingJiData, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_GET_DATA, DataHelper.ObjectToBytes<int>(player.ClientData.RoleID), player.ServerId);

            if (null == jingjiData.baseProps)
            {
                //非法数据
//                 detailData.state = ResultCode.Illegal;
// 
//                 return detailData;
                //如果没有数据，说明是和服后清除了竞技场数据，需要重新创建
                PlayerJingJiData data = createJingJiData(player);
                Global.sendToDB<byte, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_CREATE_DATA, DataHelper.ObjectToBytes<PlayerJingJiData>(data), player.ServerId);
                jingjiData = Global.sendToDB<PlayerJingJiData, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_GET_DATA, DataHelper.ObjectToBytes<int>(player.ClientData.RoleID), player.ServerId);
            }

            detailData.ranking = jingjiData.ranking;
            detailData.winCount = jingjiData.winCount;
            detailData.nextRewardTime = jingjiData.nextRewardTime;
            detailData.nextChallengeTime = jingjiData.nextChallengeTime;
            detailData.maxwincount = jingjiData.MaxWinCnt;

            //被挑战的玩家ID集合
            int[] beChallengerRankings = new int[CanChallengeNum];

            if (requestType == 0)
            {
                //排名500之后的玩家，列出的5个被挑战者为：400-410名随机1个、411-425名随机1个、426-450名随机1个、451-475名随机1个、476-500名随机1个
                if (detailData.ranking == -1)
                {
                    beChallengerRankings[0] = Global.GetRandomNumber(400, 431);
                    beChallengerRankings[1] = Global.GetRandomNumber(431, 461);
                    beChallengerRankings[2] = Global.GetRandomNumber(461, 501);
                    //beChallengerRankings[3] = Global.GetRandomNumber(451, 476);
                    //beChallengerRankings[4] = Global.GetRandomNumber(476, 501);

                }
                //	若玩家排名为前3名，则除去自身名次外提取前6名玩家供其挑战
                else if (detailData.ranking >= 1 && detailData.ranking <= 3)
                {
                    int index = 0;
                    for (int i = 1; i <= 4; i++)
                    {
                        if (i == detailData.ranking)
                            continue;

                        beChallengerRankings[index++] = i;
                    }
                }
                //	若玩家排名为5名之外，则按照 玩家竞技场名次-N*配置系数(Coefficient)  注：N为随机五个玩家的位置排序，从左往右1、2、3、4、5
                else
                {
                    //被挑战系数
                    int Coefficient = -1;

                    foreach (SystemXmlItem xmlItem in jingjiMainConfig.SystemXmlItemDict.Values)
                    {
                        if (detailData.ranking >= xmlItem.GetIntValue("MinRank") && detailData.ranking <= xmlItem.GetIntValue("MaxRank"))
                        {
                            Coefficient = xmlItem.GetIntValue("Coefficient");
                            break;
                        }
                    }

                    for (int i = 0; i < CanChallengeNum; i++)
                    {
                        beChallengerRankings[i] = detailData.ranking - (i + 1) * Coefficient;
                    }
                }
            }
            else {
                if (detailData.ranking >= 1 && detailData.ranking <= 3)
                {
                    int index = 0;
                    for (int i = 1; i <= 4; i++)
                    {
                        if (i == detailData.ranking)
                            continue;

                        beChallengerRankings[index++] = i;
                    }
                }
                else {
                    int index = 0;
                    for (int i = 1; i <= 3; i++)
                    {
                        if (i == detailData.ranking)
                            continue;

                        beChallengerRankings[index++] = i;
                    }
                }
            }

            //向DB请求获取被挑战者数据
            List<PlayerJingJiMiniData> beChallengerMiniDatas = Global.sendToDB<List<PlayerJingJiMiniData>, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_GET_CHALLENGE_DATA, DataHelper.ObjectToBytes<int[]>(beChallengerRankings), player.ServerId);

            //被挑战者mini数据
            detailData.beChallengerData = beChallengerMiniDatas;

            //每日免费进入次数
            detailData.freeChallengeNum = jingjiFuBenItem.GetIntValue("EnterNumber");

            //获取玩家竞技场副本数据
            FuBenData jingjifuBenData = Global.GetFuBenData(player, jingjiFuBenId);

            //获取玩家进入竞技场总次数
            int nFinishNum;
            int useTotalNum = Global.GetFuBenEnterNum(jingjifuBenData, out nFinishNum);

            //获取已用免费进入次数
            int useFreeNum = useTotalNum <= jingjiFuBenItem.GetIntValue("EnterNumber") ? useTotalNum : jingjiFuBenItem.GetIntValue("EnterNumber");

            detailData.useFreeChallengeNum = useFreeNum;

            //获取Vip进入次数等级数据
            int[] vipJingjiCounts = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJingJi");

            //获取vip等级
            int playerVipLev = player.ClientData.VipLevel;

            //获取vip可进入次数
            detailData.vipChallengeNum = vipJingjiCounts[playerVipLev];

            //vip已用次数
            int useVipNum = useTotalNum <= jingjiFuBenItem.GetIntValue("EnterNumber") ? 0 : useTotalNum - jingjiFuBenItem.GetIntValue("EnterNumber");

            detailData.useVipChallengeNum = useVipNum;

            //请求成功
            detailData.state = ResultCode.Success;

            return detailData;
        }

        public int getJingJiMapCode()
        {
            return null != jingjiFuBenItem ? jingjiFuBenItem.GetIntValue("MapCode") : -1; 
        }

        /// <summary>
        /// 是否能升级军衔
        /// </summary>
        /// <returns></returns>
        public bool CanGradeJunXian(GameClient player)
        {
            int junxian = this.getJunxian(player);

            //已经最高级了，按理说应该不能升了，当非法参数处理吧
            if ((junxian + 1) > junxianConfig.SystemXmlItemDict.Count)
            {
                return false;
            }

            // 升级需要消耗的声望
            int needShengWang = junxianConfig.SystemXmlItemDict[junxian + 1].GetIntValue("NeedShengWang");

            // 声望不够
            if (this.getShengWangValue(player) < needShengWang)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 军衔升级
        /// </summary>
        /// <returns></returns>
        public int upGradeJunXian(GameClient player)
        {
            int result = check(player);

            if (result != ResultCode.Success)
                return result;

            int junxian = this.getJunxian(player);

            //已经最高级了，按理说应该不能升了，当非法参数处理吧
            if ((junxian + 1) > junxianConfig.SystemXmlItemDict.Count)
            {
                return ResultCode.Illegal;
            }

            //升级需要消耗的声望
            int needShengWang = junxianConfig.SystemXmlItemDict[junxian + 1].GetIntValue("NeedShengWang");

            //声望不够
            if (!this.consumeShengWang(player, needShengWang))
            {
                return ResultCode.ShengWang_Not_Enough_Error;
            }

            //军衔升一级
            this.modifyJunxian(player);

            // 自动激活军衔
            if (ResultCode.Success != activeJunXianBuff(player, true))
            {
                //设置军衔等级加1,军衔等级不是经常变化，立即更新到数据库
                //GameManager.ClientMgr.ModifyShengWangLevelValue(player, -1, "改变军衔", true, true);                
            }
            else
            {
                // MU军衔播报
                Global.BroadcastClientMUShengWang(player, getJunxian(player));
            }


            
            // 军衔升级成功时，刷新相应的图标状态
            player._IconStateMgr.CheckJingJiChangJunXian(player);
            player._IconStateMgr.SendIconStateToClient(player);

            return ResultCode.Success;
        }

        /// <summary>
        /// 激活军衔Buff
        /// </summary>
        /// <returns></returns>
        public int activeJunXianBuff(GameClient player, bool replace)
        {
            int result = check(player);

            if (result != ResultCode.Success)
                return result;

            int junxian = this.getJunxian(player);

            //还没有军衔呢
            if (junxian <= 0)
            {
                return ResultCode.Junxian_Null_Error;
            }
            else
            {
                //直接替换
                if (replace)
                {
                    //声望不够
                    if (!consumeShengWang(player, (int)junxianConfig.SystemXmlItemDict[junxian].GetIntValue("XiaoHaoShengWang")))
                    {
                        return ResultCode.ShengWang_Not_Enough_Error;
                    }
                    else
                    {
                        //安装Buff
                        this.installJunXianBuff(player);
                        return ResultCode.Success;
                    }
                }
                else
                {
                    if (isHasJunXianBuff(player))
                    {
                        return ResultCode.HasJunxianBuff_Error;
                    }
                    else
                    {
                        //声望不够
                        if (!consumeShengWang(player, (int)junxianConfig.SystemXmlItemDict[junxian].GetIntValue("XiaoHaoShengWang")))
                        {
                            return ResultCode.ShengWang_Not_Enough_Error;
                        }
                        else
                        {
                            //安装Buff
                            this.installJunXianBuff(player);
                            return ResultCode.Success;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 消耗声望
        /// </summary>
        /// <returns></returns>
        private bool consumeShengWang(GameClient player, int consumeValue)
        {
            int shengwangValue = this.getShengWangValue(player);

            if (this.getShengWangValue(player) < consumeValue)
            {
                return false;
            }

            this.changeShengWangValue(player, -consumeValue);

            return true;
        }

        /// <summary>
        /// 获取机器人最小攻击力 [XSea 2015/6/12]
        /// <param name="nOccu">职业</param>
        /// <param name="eType">魔剑士类型</param>
        /// <param name="data">竞技场机器人数据</param>
        /// </summary>
        /// <returns></returns>
        private int GetRobotMinAttack(int nOccu, EMagicSwordTowardType eType, PlayerJingJiData data)
        {
            switch(eType)
            {
                case EMagicSwordTowardType.EMST_Not: // 不是魔剑士
                    return nOccu == 1 ? (int)data.extProps[(int)ExtPropIndexes.MinMAttack] : (int)data.extProps[(int)ExtPropIndexes.MinAttack];
                    break;
                case EMagicSwordTowardType.EMST_Strength: // 力魔
                    return (int)data.extProps[(int)ExtPropIndexes.MinAttack];
                    break;
                case EMagicSwordTowardType.EMST_Intelligence: // 智魔
                    return (int)data.extProps[(int)ExtPropIndexes.MinMAttack];
                    break;
            }
            return 0;
        }

        /// <summary>
        /// 获取机器人最大攻击力 [XSea 2015/6/12]
        /// <param name="nOccu">职业</param>
        /// <param name="eType">魔剑士类型</param>
        /// <param name="data">竞技场机器人数据</param>
        /// </summary>
        /// <returns></returns>
        private int GetRobotMaxAttack(int nOccu, EMagicSwordTowardType eType, PlayerJingJiData data)
        {
            switch (eType)
            {
                case EMagicSwordTowardType.EMST_Not: // 不是魔剑士
                    return nOccu == 1 ? (int)data.extProps[(int)ExtPropIndexes.MaxMAttack] : (int)data.extProps[(int)ExtPropIndexes.MaxMAttack];
                    break;
                case EMagicSwordTowardType.EMST_Strength: // 力魔
                    return (int)data.extProps[(int)ExtPropIndexes.MaxMAttack];
                    break;
                case EMagicSwordTowardType.EMST_Intelligence: // 智魔
                    return (int)data.extProps[(int)ExtPropIndexes.MaxMAttack];
                    break;
            }
            return 0;
        }

        /// <summary>
        /// 获取机器人攻击类型 [XSea 2015/6/12]
        /// <param name="nOccu">职业</param>
        /// <param name="eType">魔剑士类型</param>
        /// </summary>
        /// <returns></returns>
        private int GetRobotAttackType(int nOccu, EMagicSwordTowardType eType)
        {
            switch (eType)
            {
                case EMagicSwordTowardType.EMST_Not: // 不是魔剑士
                    return nOccu == 1 ? (int)AttackType.MAGIC_ATTACK : (int)AttackType.PHYSICAL_ATTACK;
                    break;
                case EMagicSwordTowardType.EMST_Strength: // 力魔
                    return (int)AttackType.PHYSICAL_ATTACK;
                    break;
                case EMagicSwordTowardType.EMST_Intelligence: // 智魔
                    return (int)AttackType.MAGIC_ATTACK;
                    break;
            }
            return 0;
        }

        /// <summary>
        /// 身上是否存在Buff
        /// </summary>
        /// <returns></returns>
        private bool isHasJunXianBuff(GameClient player)
        {
            BufferData bufferData = Global.GetBufferDataByID(player, (int)BufferItemTypes.MU_JINGJICHANG_JUNXIAN);

            //身上有Buff
            if (null != bufferData && !Global.IsBufferDataOver(bufferData))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取BuffId
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private int getJunxianBuffId(GameClient player)
        {
            int[] buffIds = GameManager.systemParamsList.GetParamValueIntArrayByName("JunXianBufferGoodsIDs");
            
            int junxian = getJunxian(player);

            return buffIds[junxian];
        }

        /// <summary>
        /// 安装Buff
        /// </summary>
        /// <param name="player"></param>
        private void installJunXianBuff(GameClient player)
        {
            int nNewBufferGoodsIndexID = getJunxian(player) - 1;

             int nOldBufferGoodsIndexID = -1;
             BufferData bufferData = Global.GetBufferDataByID(player, (int)BufferItemTypes.MU_JINGJICHANG_JUNXIAN);
             if (null != bufferData && !Global.IsBufferDataOver(bufferData))
             {
                 nOldBufferGoodsIndexID = (int)bufferData.BufferVal;
             }

             if (nOldBufferGoodsIndexID == nNewBufferGoodsIndexID)
             {
                 return;
             }             

            //更新BufferData
            double[] actionParams = new double[1];
            //actionParams[0] = (double)(60);//持续时间改为60分钟
            //actionParams[1] = (double)nNewBufferGoodsIndexID;

            actionParams[0] = (double)nNewBufferGoodsIndexID;
            if (actionParams[0] < 1 && player.CodeRevision < 1) //兼容性BUG修正
            {
                actionParams[0] = 1;
            }

            Global.UpdateBufferData(player, BufferItemTypes.MU_JINGJICHANG_JUNXIAN, actionParams, 0, true);

            //通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player);

            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player);
        }

        /// <summary>
        ///  得到竞技场排行下次领奖时间
        /// </summary>
        public void GetNextRewardTime(GameClient player)
        {
            lock (player)
            {
                //从DB获取排名和下次领取时间
                long[] resultParams = Global.sendToDB<long[], byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_GET_RANKING_AND_NEXTREWARDTIME, DataHelper.ObjectToBytes<int>(player.ClientData.RoleID), player.ServerId);
                if (null == resultParams)
                {
                    player.ClientData.JingJiNextRewardTime = -1;
                    return;
                }

                long _nextRewardTime = resultParams[1];
                if (_nextRewardTime < 1)
                {
                    player.ClientData.JingJiNextRewardTime = -1;
                    return;
                }

                player.ClientData.JingJiNextRewardTime = _nextRewardTime;
            }
        }

        /// <summary>
        ///  是否能领取排行榜奖励
        /// </summary>
        public bool CanGetrankingReward(GameClient player)
        {
            // 第一次从数据库取下次领奖时间
            if (-1 == player.ClientData.JingJiNextRewardTime)
            {
                GetNextRewardTime(player);
            }

            // 时间未冷却
            if (TimeUtil.NOW() < player.ClientData.JingJiNextRewardTime)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///  领取排行榜奖励
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="result"></param>
        /// <param name="addShengWangValue"></param>
        public void rankingReward(GameClient player, out int result, out long nextRewardTime)
        {
            result = check(player);

            nextRewardTime = 0;

            if (result != ResultCode.Success)
            {
                return ;
            }

            int addExpValue;
            int addShengWangValue;
            string goodsInfos;

            lock (player)
            {
                //从DB获取排名和下次领取时间
                long[] resultParams = Global.sendToDB<long[], byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_GET_RANKING_AND_NEXTREWARDTIME, DataHelper.ObjectToBytes<int>(player.ClientData.RoleID), player.ServerId);

                int ranking = (int)resultParams[0];
                long _nextRewardTime = resultParams[1];

                if (ranking == -2)
                {
                    result = ResultCode.Illegal;
                    return;
                }

                //时间未冷却
                if (TimeUtil.NOW() < _nextRewardTime)
                {
                    result = ResultCode.RankingReward_CD_Error;
                    return;
                }

                getRankingRewardValue(player, ranking, out addShengWangValue, out addExpValue, out goodsInfos);

                _nextRewardTime = TimeUtil.NOW() + RankingReward_CD_Time;

                nextRewardTime = _nextRewardTime;

                //更新DB下次领取时间
                Global.sendToDB<int, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_UPDATE_NEXTREWARDTIME, DataHelper.ObjectToBytes<long[]>(new long[] { player.ClientData.RoleID, _nextRewardTime }), player.ServerId);

                // 刷新下次领取奖励时间
                GetNextRewardTime(player);
            }

            //添加经验
            GameManager.ClientMgr.ProcessRoleExperience(player, addExpValue, true, true);

            //发放奖励物品
            addGoods(player, goodsInfos);

            //添加声望
            changeShengWangValue(player, addShengWangValue);

            // 刷新“竞技场奖励”图标感叹号状态
            player._IconStateMgr.CheckJingJiChangJiangLi(player);
            player._IconStateMgr.SendIconStateToClient(player);
        }

        /// <summary>
        /// 发放奖励物品
        /// </summary>
        /// <param name="player"></param>
        /// <param name="goodsInfos"></param>
        private void addGoods(GameClient player, string goodsInfos)
        {
            string[] _goodsInfos = goodsInfos.Split('|');

            foreach(string goodsInfo in _goodsInfos)
            {
                string[] _goodsInfo = goodsInfo.Split(',');

                int goodsId = Convert.ToInt32(_goodsInfo[0]);// 物品ID
                int goodsNum = Convert.ToInt32(_goodsInfo[1]);//物品数量
                int binding = Convert.ToInt32(_goodsInfo[2]);//是否绑定(1绑定、0非绑定、-1无限制)
                int forgeLevel = Convert.ToInt32(_goodsInfo[3]);//强化等级
                int nAppendPropLev = Convert.ToInt32(_goodsInfo[4]);//追加等级
                int lucky = Convert.ToInt32(_goodsInfo[5]);//是否有幸运
                int ExcellenceProperty = Convert.ToInt32(_goodsInfo[6]);//卓越属性

                //[bing] fix newhint改为1 不然客户端没有给物品提示
                Global.AddGoodsDBCommand(TCPOutPacketPool.getInstance(), player, goodsId, goodsNum, 0, "", forgeLevel, binding, 0, "", false, 1, "竞技场排行榜奖励", Global.ConstGoodsEndTime,0,0,lucky,0,ExcellenceProperty,nAppendPropLev,0);
            }

            
        }

        /// <summary>
        /// 改变声望值
        /// </summary>
        /// <param name="player"></param>
        /// <param name="value"></param>
        private void changeShengWangValue(GameClient player, int value)
        {
            GameManager.ClientMgr.ModifyShengWangValue(player, value, "竞技场", true, true);
        }

        /// <summary>
        /// 获取声望
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private int getShengWangValue(GameClient player)
        {
            return GameManager.ClientMgr.GetShengWangValue(player);
            
        }

        /// <summary>
        /// 改变军衔
        /// </summary>
        /// <param name="player"></param>
        /// <param name="value"></param>
        private void modifyJunxian(GameClient player)
        {
            //设置军衔等级加1,军衔等级不是经常变化，立即更新到数据库
            GameManager.ClientMgr.ModifyShengWangLevelValue(player, 1, "改变军衔", true, true);            
        }

        /// <summary>
        /// 获取军衔
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private int getJunxian(GameClient player)
        {
            int junxian = GameManager.ClientMgr.GetShengWangLevelValue(player);
            return junxian<0?0:junxian;
        }

        /// <summary>
        /// 获取奖励信息
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ranking"></param>
        /// <param name="addShengWangValue"></param>
        /// <param name="addExpValue"></param>
        /// <param name="goodsInfos"></param>
        private void getRankingRewardValue(GameClient player, int ranking, out int addShengWangValue, out int addExpValue, out string goodsInfos)
        {
            addShengWangValue = -1;
            addExpValue = -1;
            goodsInfos = null;

            foreach (SystemXmlItem xmlItem in jingjiMainConfig.SystemXmlItemDict.Values)
            {
                //500名以外的奖励
                if (ranking == -1 && xmlItem.GetStringValue("MaxRank").Equals(""))
                {
                    addShengWangValue = xmlItem.GetIntValue("ShengWang2");
                    //Min(400,转生级别*400+人物等级)*名次对应经验系数
                   // addExpValue = Global.GMin(400, player.ClientData.ChangeLifeCount * 400 + player.ClientData.Level) * xmlItem.GetIntValue("ExpCoefficient2");
                    addExpValue = xmlItem.GetIntValue("ExpCoefficient2");
                    goodsInfos = xmlItem.GetStringValue("GoodsID");

                    break;
                }

                if (ranking >= xmlItem.GetIntValue("MinRank") && ranking <= xmlItem.GetIntValue("MaxRank"))
                {
                    addShengWangValue = xmlItem.GetIntValue("ShengWang2");
                    //玩家竞技场名次 * 经验系数
                    //addExpValue = Global.GMin(400, player.ClientData.ChangeLifeCount * 400 + player.ClientData.Level) * xmlItem.GetIntValue("ExpCoefficient2");
                    addExpValue = xmlItem.GetIntValue("ExpCoefficient2");
                    goodsInfos = xmlItem.GetStringValue("GoodsID");

                    break;
                }

            }

        }

        public bool isInJingJiFuBen(GameClient player)
        {
            if (player.ClientData.MapCode == jingjiFuBenItem.GetIntValue("MapCode"))
            {
                return true;
            }
            return false;
        }

        private int check(GameClient player)
        {
            int result = ResultCode.Success;

            if ((player.ClientData.Level < jingjiFuBenItem.GetIntValue("MinLevel")
              && player.ClientData.ChangeLifeCount == jingjiFuBenItem.GetIntValue("MinZhuanSheng"))
              || (player.ClientData.IsFlashPlayer == 1 && player.ClientData.MapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID))
            {
                //非法数据
                result = ResultCode.Illegal;

                return result;
            }

            //战斗时不允许请求
            //if (!checkAction(player))
            //{
            //    result = ResultCode.Combat_Error;
            //    return result;
            //}

            //死亡时不允许请求
            if (player.ClientData.CurrentLifeV <= 0 || player.ClientData.CurrentAction == (int)GActions.Death)
            {
                result = ResultCode.Dead_Error;
                return result;
            }

            return result;
        }

        /// <summary>
        /// 消除CD
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public int removeCD(GameClient player)
        {
            int result = check(player);

            if (result != ResultCode.Success)
            {
                return result;
            }

            //获取玩家竞技场数据
            PlayerJingJiData jingjiData = Global.sendToDB<PlayerJingJiData, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_GET_DATA, DataHelper.ObjectToBytes<int>(player.ClientData.RoleID), player.ServerId);

            if (null == jingjiData.baseProps)
            {
                return ResultCode.Illegal;
            }

            long cdForSeconds = ((jingjiData.nextChallengeTime - TimeUtil.NOW()) / 1000);
            if (cdForSeconds <= 0) {
                return ResultCode.Success;
            }

            int price = (int)(Math.Ceiling(cdForSeconds * GameManager.systemParamsList.GetParamValueDoubleByName("CDXiaoHaoZhuanShi")));
            if (price > 0)
            {
                //钱不够。。。囧。。。
                if (player.ClientData.UserMoney < price)
                {
                    result = ResultCode.Money_Not_Enough_Error;
                    return result;
                }

                //艹。。。扣费失败了。。。
                if (!GameManager.ClientMgr.SubUserMoney(TCPManager.getInstance().MySocketListener, TCPManager.getInstance().tcpClientPool, TCPOutPacketPool.getInstance(), player, price, "竞技场消除CD"))
                {
                    result = ResultCode.Pay_Error;
                    return result;
                }
            }

            //将挑战时间置0
            Global.sendToDB<bool, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_REMOVE_CD, DataHelper.ObjectToBytes<int>(player.ClientData.RoleID), player.ServerId);
            result = ResultCode.Success;

            return result; 
        }

        /// <summary>
        /// 创建玩家竞技场数据
        /// </summary>
        /// <param name="player"></param>
        public PlayerJingJiData createJingJiData(GameClient player)
        {
            PlayerJingJiData data = new PlayerJingJiData();

            data.roleId = player.ClientData.RoleID;
            data.roleName = Global.FormatRoleName4(player);
            data.name = player.ClientData.RoleName;
            data.zoneId = player.ClientData.ZoneID;
            data.level = player.ClientData.Level;
            data.changeLiveCount = player.ClientData.ChangeLifeCount;
            data.occupationId = player.ClientData.Occupation;
            data.nextChallengeTime = 0;
            data.nextRewardTime = TimeUtil.NOW() + RankingReward_CD_Time;
            data.combatForce = player.ClientData.CombatForce;
            data.equipDatas = getSaveEquipData(player);
            data.skillDatas = getSaveSkillData(player);
            data.baseProps = getBaseProps(player);
            data.extProps = getExtProps(player);
            data.sex = player.ClientData.RoleSex;
            data.wingData = null;
            if (player.ClientData.MyWingData != null 
                && player.ClientData.MyWingData.WingID > 0)
            {
                data.wingData = player.ClientData.MyWingData;
            }
            data.settingFlags = Global.GetRoleParamsInt64FromDB(player, RoleParamName.SettingBitFlags);

            return data;
        }

        /// <summary>
        /// 创建玩家竞技场数据
        /// </summary>
        /// <param name="player"></param>
        public void onPlayerLevelup(GameClient player)
        {
            int nMinLevel = 0; // 所需最小等级
            int nChangeLifeCount = 0; // 所需最小转生次数

            // 魔剑士 [XSea 2015/6/11]
            if (GameManager.MagicSwordMgr.IsMagicSword(player))
            {
                nMinLevel = MagicSwordData.InitLevel;
                nChangeLifeCount = MagicSwordData.InitChangeLifeCount;
            }
            else // 其他职业
            {
                nMinLevel = jingjiFuBenItem.GetIntValue("MinLevel");
                nChangeLifeCount =jingjiFuBenItem.GetIntValue("MinZhuanSheng");
            }

            if (player.ClientData.Level == nMinLevel
             && player.ClientData.ChangeLifeCount == nChangeLifeCount
             && !(player.ClientData.IsFlashPlayer == 1 && player.ClientData.MapCode == (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID))
            {
                PlayerJingJiData data = createJingJiData(player);
                Global.sendToDB<byte, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_CREATE_DATA, DataHelper.ObjectToBytes<PlayerJingJiData>(data), player.ServerId);
               
                if (GameManager.ClientMgr.GetShengWangValue(player) <= 0)
                {
                    //初始化声望
                    GameManager.ClientMgr.SaveShengWangValue(player, 0, true);

                    //通知自己
                    GameManager.ClientMgr.NotifySelfParamsValueChange(player, RoleCommonUseIntParamsIndexs.ShengWang, 0);
                }
               
                if (GameManager.ClientMgr.GetShengWangLevelValue(player) != -1)
                {
                    //初始化军衔
                    GameManager.ClientMgr.ModifyShengWangLevelValue(player, 0, "初始化军衔二", true, true);
                    //MU军衔播报
                    Global.BroadcastClientMUShengWang(player, getJunxian(player));
                }
                
            }
            
        }

        /// <summary>
        /// 获取角色当前技能数据
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private List<PlayerJingJiSkillData> getSaveSkillData(GameClient client)
        {
            List<PlayerJingJiSkillData> skillDataList = new List<PlayerJingJiSkillData>();

            List<SkillData> _skillList = client.ClientData.SkillDataList;

            if (null == _skillList || _skillList.Count == 0)
                return skillDataList;

            //int[] skillIdList = baseOccupation == 0 ? ZhanShiSkillList : baseOccupation == 1 ? FaShiSkillList : GongJianShouSkillList;

            // 职业
            int nOccupation = Global.CalcOriginalOccupationID(client);

            // 魔剑士分支类型
            EMagicSwordTowardType eMagicSwordType = GameManager.MagicSwordMgr.GetMagicSwordTypeByWeapon(client.ClientData.Occupation, client.UsingEquipMgr.GetWeaponEquipList());

            // 魔剑士需要根据武器判断魔剑士分支 [XSea 2015/5/19]
            int[] skillIdList = JingJiChangConstants.GetJingJiChangeSkillList(Global.CalcOriginalOccupationID(nOccupation), eMagicSwordType);// 技能列表
            
            foreach (int skillId in skillIdList)
            {
                foreach (SkillData _skillData in _skillList)
                {
                    if (skillId == _skillData.SkillID)
                    {
                        PlayerJingJiSkillData skillData = new PlayerJingJiSkillData();

                        skillData.skillID = _skillData.SkillID;
                        skillData.skillLevel = _skillData.SkillLevel;
                        //天赋
                        skillData.skillLevel += TalentManager.GetSkillLevel(client, skillData.skillID);
                        skillData.skillLevel = Math.Min(skillData.skillLevel, Global.MaxSkillLevel);
                        skillData.skillLevel = Global.GMax(0, skillData.skillLevel);

                        skillDataList.Add(skillData);
                        break;
                    }
                }
            }

            return skillDataList;
        }

        /// <summary>
        /// 获取基础属性
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private double[] getBaseProps(GameClient player)
        {
            double[] baseProps = new double[(int)UnitPropIndexes.Max];
            // 力量
            baseProps[(int)UnitPropIndexes.Strength] = RoleAlgorithm.GetStrength(player);
            // 智力
            baseProps[(int)UnitPropIndexes.Intelligence] = RoleAlgorithm.GetIntelligence(player);
            // 敏捷
            baseProps[(int)UnitPropIndexes.Dexterity] = RoleAlgorithm.GetDexterity(player);
            // 体力
            baseProps[(int)UnitPropIndexes.Constitution] = RoleAlgorithm.GetConstitution(player);

            return baseProps;
        }

        /// <summary>
        /// 获取扩展属性
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private double[] getExtProps(GameClient player)
        {
            double[] extProps = new double[(int)ExtPropIndexes.Max];

            extProps[(int)ExtPropIndexes.Strong] = RoleAlgorithm.GetStrong(player);           // 耐久
            extProps[(int)ExtPropIndexes.AttackSpeed] = RoleAlgorithm.GetAttackSpeed(player);       // 攻击速度GetAttackSpeedServer
            extProps[(int)ExtPropIndexes.MoveSpeed] = RoleAlgorithm.GetMoveSpeed(player);      // 移动速度
            extProps[(int)ExtPropIndexes.MinDefense] = RoleAlgorithm.GetMinADefenseV(player);      // 最小物防	
            extProps[(int)ExtPropIndexes.MaxDefense] = RoleAlgorithm.GetMaxADefenseV(player);      // 最大物防	
            extProps[(int)ExtPropIndexes.MinMDefense] = RoleAlgorithm.GetMinMDefenseV(player);       // 最小魔防	
            extProps[(int)ExtPropIndexes.MaxMDefense] = RoleAlgorithm.GetMaxMDefenseV(player);       // 最大魔防	
            extProps[(int)ExtPropIndexes.MinAttack]  = RoleAlgorithm.GetMinAttackV(player);       // 最小物攻	
            extProps[(int)ExtPropIndexes.MaxAttack] = RoleAlgorithm.GetMaxAttackV(player);       // 最大物攻	
            extProps[(int)ExtPropIndexes.MinMAttack] = RoleAlgorithm.GetMinMagicAttackV(player);       // 最小魔攻	
            extProps[(int)ExtPropIndexes.MaxMAttack] = RoleAlgorithm.GetMaxMagicAttackV(player);      // 最大魔攻
            extProps[(int)ExtPropIndexes.IncreasePhyAttack] = player.RoleBuffer.GetExtProp((int)ExtPropIndexes.IncreasePhyAttack);      // 物理攻击提升
            extProps[(int)ExtPropIndexes.IncreaseMagAttack] = player.RoleBuffer.GetExtProp((int)ExtPropIndexes.IncreaseMagAttack);      // 魔法攻击提升
            extProps[(int)ExtPropIndexes.MaxLifeV] = RoleAlgorithm.GetMaxLifeV(player);      // 生命上限	
            extProps[(int)ExtPropIndexes.MaxLifePercent] = RoleAlgorithm.GetMaxLifePercentV(player);      // 生命上限加成比例(百分比)	
            extProps[(int)ExtPropIndexes.MaxMagicV] = RoleAlgorithm.GetMaxMagicV(player);      // 魔法上限
            extProps[(int)ExtPropIndexes.MaxMagicPercent] = RoleAlgorithm.GetMaxMagicPercent(player);      // 魔法上限加成比例(百分比)
            extProps[(int)ExtPropIndexes.Lucky] = RoleAlgorithm.GetLuckV(player);     // 幸运
            extProps[(int)ExtPropIndexes.HitV]  = RoleAlgorithm.GetHitV(player);      // 准确	
            extProps[(int)ExtPropIndexes.Dodge] = RoleAlgorithm.GetDodgeV(player);      // 闪避
            extProps[(int)ExtPropIndexes.LifeRecoverPercent] = RoleAlgorithm.GetLifeRecoverAddPercentV(player);      // 生命恢复(百分比)
            extProps[(int)ExtPropIndexes.MagicRecoverPercent] = RoleAlgorithm.GetMagicRecoverAddPercentV(player);      // 魔法恢复(百分比)
            extProps[(int)ExtPropIndexes.LifeRecover] = RoleAlgorithm.GetLifeRecoverValPercentV(player);      // 单位时间恢复的生命恢复(固定值)
            extProps[(int)ExtPropIndexes.MagicRecover] = RoleAlgorithm.GetMagicRecoverValPercentV(player);      // 单位时间恢复的魔法恢复(固定值)
            extProps[(int)ExtPropIndexes.SubAttackInjurePercent] = RoleAlgorithm.GetSubAttackInjurePercent(player);     // 伤害吸收魔法/物理(百分比)
            extProps[(int)ExtPropIndexes.SubAttackInjure] = RoleAlgorithm.GetSubAttackInjureValue(player);      // 伤害吸收魔法/物理(固定值)
            extProps[(int)ExtPropIndexes.AddAttackInjurePercent] = RoleAlgorithm.GetAddAttackInjurePercent(player);     // 伤害加成魔法/物理(百分比)
            extProps[(int)ExtPropIndexes.AddAttackInjure] = RoleAlgorithm.GetAddAttackInjureValue(player);      // 伤害加成魔法/物理(固定值)
            extProps[(int)ExtPropIndexes.IgnoreDefensePercent] = RoleAlgorithm.GetIgnoreDefensePercent(player);      // 无视攻击对象的物理/魔法防御(概率)
            extProps[(int)ExtPropIndexes.DamageThornPercent] = RoleAlgorithm.GetDamageThornPercent(player);      // 伤害反弹(百分比)
            extProps[(int)ExtPropIndexes.DamageThorn] = RoleAlgorithm.GetDamageThorn(player);      // 伤害反弹(固定值)
            extProps[(int)ExtPropIndexes.PhySkillIncreasePercent] = RoleAlgorithm.GetPhySkillIncrease(player);    // 物理技能增幅(百分比)
            extProps[(int)ExtPropIndexes.PhySkillIncrease] = 0;      // 物理技能增幅(固定值)    
            extProps[(int)ExtPropIndexes.MagicSkillIncreasePercent] = RoleAlgorithm.GetMagicSkillIncrease(player);  // 魔法技能增幅(百分比)
            extProps[(int)ExtPropIndexes.MagicSkillIncrease] = 0;      // 魔法技能增幅(固定值)
            extProps[(int)ExtPropIndexes.FatalAttack] = RoleAlgorithm.GetFatalAttack(player);       // 卓越一击
            extProps[(int)ExtPropIndexes.DoubleAttack] = RoleAlgorithm.GetDoubleAttack(player);      // 双倍一击
            extProps[(int)ExtPropIndexes.DecreaseInjurePercent] = RoleAlgorithm.GetDecreaseInjurePercent(player);      // 伤害减少百分比(物理、魔法)
            extProps[(int)ExtPropIndexes.DecreaseInjureValue] = RoleAlgorithm.GetDecreaseInjureValue(player);      // 伤害减少数值(物理、魔法)
            extProps[(int)ExtPropIndexes.CounteractInjurePercent] = RoleAlgorithm.GetCounteractInjurePercent(player);    // 伤害抵挡百分比(物理、魔法)
            extProps[(int)ExtPropIndexes.CounteractInjureValue] = RoleAlgorithm.GetCounteractInjureValue(player);      // 伤害抵挡数值(物理、魔法)
            extProps[(int)ExtPropIndexes.IgnoreDefenseRate] = RoleAlgorithm.GetIgnoreDefenseRate(player);      // 无视防御的比例
            extProps[(int)ExtPropIndexes.IncreasePhyDefense] = player.RoleBuffer.GetExtProp((int)ExtPropIndexes.IncreasePhyDefense);      // 物理防御提升
            extProps[(int)ExtPropIndexes.IncreaseMagDefense] = player.RoleBuffer.GetExtProp((int)ExtPropIndexes.IncreaseMagDefense);      // 魔法防御提升
            extProps[(int)ExtPropIndexes.LifeSteal] = RoleAlgorithm.GetLifeStealV(player);       //击中恢复    

            extProps[(int)ExtPropIndexes.DeLucky] = RoleAlgorithm.GetDeLuckyAttack(player);       //抵抗幸运一击      
            extProps[(int)ExtPropIndexes.DeFatalAttack] = RoleAlgorithm.GetDeFatalAttack(player);  //抵抗卓越一击      
            extProps[(int)ExtPropIndexes.DeDoubleAttack] = RoleAlgorithm.GetDeDoubleAttack(player);//抵抗双倍一击 

            // 新增属性 [XSea 2015/6/26]
            extProps[(int)ExtPropIndexes.FrozenPercent] = RoleAlgorithm.GetFrozenPercent(player); // 冰冻几率
            extProps[(int)ExtPropIndexes.PalsyPercent] = RoleAlgorithm.GetPalsyPercent(player); // 麻痹几率
            extProps[(int)ExtPropIndexes.SpeedDownPercent] = RoleAlgorithm.GetSpeedDownPercent(player); // 减速几率
            extProps[(int)ExtPropIndexes.BlowPercent] = RoleAlgorithm.GetBlowPercent(player); // 重击几率

            #region 天赋 [j 2015-06-10]
            extProps[(int)ExtPropIndexes.SavagePercent] = RoleAlgorithm.GetSavagePercent(player);           //野蛮一击   
            extProps[(int)ExtPropIndexes.ColdPercent] = RoleAlgorithm.GetColdPercent(player);               //冷血一击   
            extProps[(int)ExtPropIndexes.RuthlessPercent] = RoleAlgorithm.GetRuthlessPercent(player);       //无情一击   

            extProps[(int)ExtPropIndexes.DeSavagePercent] = RoleAlgorithm.GetDeSavagePercent(player);       //抵抗野蛮一击   
            extProps[(int)ExtPropIndexes.DeColdPercent] = RoleAlgorithm.GetDeColdPercent(player);           //抵抗冷血一击   
            extProps[(int)ExtPropIndexes.DeRuthlessPercent] = RoleAlgorithm.GetDeRuthlessPercent(player);   //抵抗无情一击          
            #endregion

            return extProps;
        }

        /// <summary>
        /// 获取角色当前身上可存储的装备
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private List<PlayerJingJiEquipData> getSaveEquipData(GameClient client)
        {
            List<PlayerJingJiEquipData> equipDataList = new List<PlayerJingJiEquipData>();

            List<GoodsData> goodsDataList = client.ClientData.GoodsDataList;
            
            if (null != goodsDataList)
            {
                foreach (GoodsData goods in goodsDataList)
                {
                    if (!canSaveEquip(goods))
                    {
                        continue;
                    }

                    PlayerJingJiEquipData data = new PlayerJingJiEquipData();

                    data.EquipId = goods.GoodsID;
                    data.ExcellenceInfo = goods.ExcellenceInfo;
                    data.Forge_level = goods.Forge_level;
                    equipDataList.Add(data);
                }
            }

            //[bing] 把精灵也加入robot的装备列表
            if (null != client.ClientData.DamonGoodsDataList)
            {
                lock (client.ClientData.DamonGoodsDataList)
                {
                    for (int i = 0; i < client.ClientData.DamonGoodsDataList.Count; i++)
                    {
                        GoodsData DamonGoodsData = client.ClientData.DamonGoodsDataList[i];

                        if (DamonGoodsData.GCount <= 0 || 0 == DamonGoodsData.Using)
                        {
                            continue;
                        }

                        PlayerJingJiEquipData data = new PlayerJingJiEquipData();

                        data.EquipId = DamonGoodsData.GoodsID;
                        data.ExcellenceInfo = DamonGoodsData.ExcellenceInfo;
                        data.Forge_level = DamonGoodsData.Forge_level;
                        equipDataList.Add(data);
                    }
                }
            }

            // 时装衣橱
            if (null != client.ClientData.FashionGoodsDataList)
            {
                lock (client.ClientData.FashionGoodsDataList)
                {
                    for (int i = 0; i < client.ClientData.FashionGoodsDataList.Count; i++)
                    {
                        if (client.ClientData.FashionGoodsDataList[i].GCount <= 0 || 0 == client.ClientData.FashionGoodsDataList[i].Using
                            || client.ClientData.FashionGoodsDataList[i].Site != (int)SaleGoodsConsts.FashionGoods)
                        {
                            continue;
                        }

                        GoodsData FashionGoodsData = client.ClientData.FashionGoodsDataList[i];
                        PlayerJingJiEquipData data = new PlayerJingJiEquipData();

                        data.EquipId = FashionGoodsData.GoodsID;
                        data.ExcellenceInfo = FashionGoodsData.ExcellenceInfo;
                        data.Forge_level = FashionGoodsData.Forge_level;
                        equipDataList.Add(data);
                    }
                }
            }

            return equipDataList;
        }

        /// <summary>
        /// 判断装备是否可以保存
        /// 只存储可显示的装备用于客户端显示
        /// </summary>
        /// <param name="equip"></param>
        /// <returns></returns>
        private bool canSaveEquip(GoodsData equip)
        {
            if (equip.Site != 0 && equip.Site != (int)SaleGoodsConsts.UsingDemonGoodsID)
            {
                return false;
            }

            if (equip.Using > 0)
            {
                int category = Global.GetGoodsCatetoriy(equip.GoodsID);

                if (category >= (int)ItemCategories.TouKui
                && category < (int)ItemCategories.EquipMax
                && category != (int)ItemCategories.XiangLian
                && category != (int)ItemCategories.JieZhi
                && category != (int)ItemCategories.HuFu
                && category != (int)ItemCategories.HuFu_2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断装备是否可以保存
        /// 只存储可显示的装备用于客户端显示
        /// </summary>
        /// <param name="equip"></param>
        /// <returns></returns>
        public bool IsJingJiChangMap(int nMapCode)
        {
            if (nMapCode == nJingJiChangMapCode)
            {
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// 请求挑战
        /// </summary>
        /// <param name="player">挑战者</param>
        /// <param name="beChallengerId">被挑战者Id</param>
        /// <param name="beChallengerRanking">被挑战者排名</param>
        /// <param name="enterType">进入竞技场方式</param>
        /// <returns>1:成功进入，0：非法参数，-1:免费次数已用完,-2:vip次数已用完,-3:钻石不够，-4:扣费失败, -5：无效地图，-6：无效副本顺序ID，-7：死亡不让进,-8:冷却时间未到,-9:被挑战机器人不存在,-10:被挑战机器人排名已更改,-11:正在被其他玩家挑战,-12:向DB请求数据失败,-13:战斗时不允许挑战</returns>
        public int requestChallenge(GameClient player, int beChallengerId, int beChallengerRanking, int enterType)
        {
            int result = check(player);

            if (result != ResultCode.Success)
            {
                return result;
            }

            // 避免连续挑战
            if (IsJingJiChangMap(player.ClientData.MapCode))
            {
                result = ResultCode.Challenge_CD_Error;
                return result;
            }

            //请求挑战数据
            JingJiBeChallengeData requestChallengeData = Global.sendToDB<JingJiBeChallengeData, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_REQUEST_CHALLENGE, DataHelper.ObjectToBytes<int[]>(new int[] { player.ClientData.RoleID, beChallengerId, beChallengerRanking }), player.ServerId);

            result = requestChallengeData.state;

            // VIP取消CD [8/15/2014 LiaoWei]
            if (result == -1)
            {
                int nNeedVip = 0;
                nNeedVip = (int)GameManager.systemParamsList.GetParamValueDoubleByName("VIPJingJiCD");

                if (nNeedVip > 0 && player.ClientData.VipLevel >= nNeedVip)
                {
                    result = ResultCode.Success;
                }
            }

            if (result != ResultCode.Success)
            {
                //0：非法参数,-1:冷却时间未到，-2：被挑战机器人不存在,-3:被挑战机器人排名已更改,-4:正在被其他玩家挑战
                switch (result)
                {
                    case -1:
                        result = ResultCode.Challenge_CD_Error;
                        break;
                    case -2:
                        result = ResultCode.BeChallenger_Null_Error;
                        break;
                    case -3:
                        result = ResultCode.BeChallenger_Ranking_Change_Error;
                        break;
                    case -4:
                        result = ResultCode.BeChallenger_Lock_Error;
                        break;
                    default:
                        result = ResultCode.Illegal;
                        break;
                }

                return result;
            }
            
            //检验进入次数
            result = checkEnterNum(player, enterType);

            if (result != ResultCode.Success)
                return result;            

            //进入副本
            result = enterJingJiChang(player, requestChallengeData.beChallengerData);

            return result;
        }

        /// <summary>
        /// 检验当前状态
        /// 战斗状态时不允许挑战
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool checkAction(GameClient player)
        {
            //摆摊状态下不允许挑战
            if (player.ClientData.StallDataItem != null)
                return false;

            switch (player.ClientData.CurrentAction)
            {
                case (int)GActions.Attack:
                case (int)GActions.Injured:
                case (int)GActions.Magic:
                case (int)GActions.Bow:
                case (int)GActions.PreAttack:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 检验进入次数
        /// </summary>
        /// <returns></returns>
        public int checkEnterNum(GameClient player, int enterType)
        {
            int result = ResultCode.Success;

            //免费进入次数
            int freeNum = jingjiFuBenItem.GetIntValue("EnterNumber");

            //获取玩家竞技场副本数据
            FuBenData jingjifuBenData = Global.GetFuBenData(player, jingjiFuBenId);

            //获取玩家进入竞技场总次数
            int nFinishNum;
            int useTotalNum = Global.GetFuBenEnterNum(jingjifuBenData, out nFinishNum);

            if (enterType == Enter_Type_Free)
            {
                //免费用完了
                if (useTotalNum >= freeNum)
                {
                    result = ResultCode.FreeNum_Error;
                    return result;
                }

            }
            else if (enterType == Enter_Type_Vip)
            {

                //免费的用完了，用收费的吧
                if (useTotalNum >= freeNum)
                {
                    //vip进入次数
                    int vipNum = useTotalNum - freeNum;

                    //获取Vip进入次数等级数据
                    int[] vipJingjiCounts = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJingJi");

                    //获取vip等级
                    int playerVipLev = player.ClientData.VipLevel;

                    //当前等级vip可进入的次数
                    int vipCanUseNum = vipJingjiCounts[playerVipLev];

                    //vip次数没用完
                    if (vipCanUseNum > vipNum)
                    {
                        //获取价格
                        int price = (int)GameManager.systemParamsList.GetParamValueIntByName("VIPGouMaiJingJi");

                        //钱不够。。。囧。。。
                        if (player.ClientData.UserMoney < price)
                        {
                            result = ResultCode.Money_Not_Enough_Error;
                            return result;
                        }
                        //哥有钱
                        else
                        {
                            //艹。。。扣费失败了。。。
                            if (!GameManager.ClientMgr.SubUserMoney(TCPManager.getInstance().MySocketListener, TCPManager.getInstance().tcpClientPool, TCPOutPacketPool.getInstance(), player, price, "竞技场额外进入"))
                            {
                                result = ResultCode.Pay_Error;
                            }
                            //扣费成功
                            else
                            {
                                result = ResultCode.Success;
                            }
                        }
                    }
                    //vip次数用完了。。。
                    else
                    {
                        result = ResultCode.VipNum_Error;
                        return result;
                    }

                }

            }

            return result;
        }

        /// <summary>
        /// 竞技场战斗开始
        /// </summary>
        public int JingJiChangStartFight(GameClient client)
        {
            if (IsHaveFuBen(client.ClientData.FuBenSeqID))
            {
                JingJiChangInstance instance = null;
                lock (jingjichangInstances)
                {
                    jingjichangInstances.TryGetValue(client.ClientData.FuBenSeqID, out instance);
                }

                if (null == instance)
                    return -1;

                if (instance.getState() == JingJiFuBenState.INITIALIZED)
                {
                    instance.ResetJingJiTime();
                    instance.switchState(JingJiFuBenState.WAITING_CHANGEMAP_FINISH);
                }

                return 0;
            }

            return -1;
        }

        /// <summary>
        /// 进入竞技场
        /// </summary>
        /// <param name="client"></param>
        /// <param name="beChallengerData"></param>
        /// <returns></returns>
        public int enterJingJiChang(GameClient client, PlayerJingJiData beChallengerData)
        {

            //通知用户切换地图到副本的地图上
            GameMap gameMap = null;

            int mapId = jingjiFuBenItem.GetIntValue("MapCode");

            if (!GameManager.MapMgr.DictMaps.TryGetValue(mapId, out gameMap)) 
            {
                //无效地图
                return ResultCode.Map_Error;
            }

            //从DBServer获取副本顺序ID
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GETFUBENSEQID, string.Format("{0}", client.ClientData.RoleID), client.ServerId);
            if (null == dbFields || dbFields.Length < 2)
            {
                //无效副本顺序ID
                return ResultCode.FubenSeqId_Error;
            }

            int fuBenSeqID = Global.SafeConvertToInt32(dbFields[1]);

            //设置角色的副本顺序ID
            client.ClientData.FuBenSeqID = fuBenSeqID;

            Global.UpdateFuBenData(client, jingjiFuBenId); //增加副本今日的进入次数

            //添加一个角色到副本顺序ID的映射
            FuBenManager.AddFuBenSeqID(client.ClientData.RoleID, client.ClientData.FuBenSeqID, 0, jingjiFuBenId);

            //创建竞技场机器人
            Robot robot = createRobot(client, beChallengerData);

            //创建竞技场实例
            JingJiChangInstance jingjichangInstance = new JingJiChangInstance(client, robot, fuBenSeqID);
            lock (jingjichangInstances)
            {
                jingjichangInstances.Add(jingjichangInstance.getFuBenSeqId(), jingjichangInstance);
            }

#if !UseTimer
            //调度竞技场
            PeriodicTaskHandle handle = executor.scheduleExecute(jingjichangInstance, 0, 100);
            jingjichangInstance.Handle = handle;
#else
            ScheduleExecutor2.Instance.scheduleExecute(jingjichangInstance, 0, 100);
#endif
            // 满血满蓝
            GameManager.ClientMgr.UserFullLife(client, "进入竞技场", false);

            //玩家进入竞技场
            GameManager.ClientMgr.ChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), client, -1, mapId, gameMap.DefaultBirthPosX, gameMap.DefaultBirthPosY, client.ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_MAPCHANGE);

            // 挑战成功进入副本，刷新相应的图标状态
            client._IconStateMgr.CheckJingJiChangLeftTimes(client);
            client._IconStateMgr.SendIconStateToClient(client);

            // 七日活动
            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.JoinJingJiChangTimes));

            //成功
            return ResultCode.Success;
        }

        public void createSkillIDs(List<PlayerJingJiSkillData> skillDatas, Robot robot)
        {
            if(null == skillDatas || skillDatas.Count == 0){
                return ;
            }

            int[] skillIDs = new int[skillDatas.Count];
#if ___CC___FUCK___YOU___BB___
            for (int i = 0; i < skillDatas.Count; i++)
            {
               skillIDs[i] = skillDatas[i].skillID;
                robot.XMonsterInfo.Skills.Add(skillIDs[i]);
               robot.skillInfos.Add(skillDatas[i].skillID, skillDatas[i].skillLevel);
            }
#else
            for (int i = 0; i < skillDatas.Count; i++)
            {
               skillIDs[i] = skillDatas[i].skillID;
               robot.skillInfos.Add(skillDatas[i].skillID, skillDatas[i].skillLevel);
            }
             robot.MonsterInfo.SkillIDs = skillIDs;
#endif
        }

        #region 创建竞技场机器人时获取机器人属性
        /// <summary>
        /// 创建竞技场机器人时获取机器人属性 [XSea 2015/6/1]
        /// </summary>
        private double GetRobotExtProps(int nIndex, double[] extProps)
        {
            // 超出数组上限 给默认值
            if (nIndex > extProps.Length - 1)
                return 0.0;
            else // 有值 直接给
                return extProps[nIndex];
        }
#endregion

        /// <summary>
        /// 创建机器人
        /// </summary>
        /// <param name="beChallengerData"></param>
        /// <returns></returns>
        public Robot createRobot(GameClient player, PlayerJingJiData beChallengerData)
        {
            int roleId = (int)GameManager.MonsterIDMgr.GetNewID(jingjiFuBenItem.GetIntValue("MapCode"));
            
            RoleDataMini roleDataMini = this.createRoleDataMini(roleId, beChallengerData);

            Robot robot = new Robot(player, roleDataMini);
            robot.Lucky = (int)beChallengerData.extProps[(int)ExtPropIndexes.Lucky];
            robot.DoubleValue = (int)beChallengerData.extProps[(int)ExtPropIndexes.DoubleAttack];
            robot.FatalValue = (int)beChallengerData.extProps[(int)ExtPropIndexes.FatalAttack];

            robot.DeLucky = GetRobotExtProps((int)ExtPropIndexes.DeLucky, beChallengerData.extProps);
            robot.DeDoubleValue = GetRobotExtProps((int)ExtPropIndexes.DeDoubleAttack, beChallengerData.extProps);
            robot.DeFatalValue = GetRobotExtProps((int)ExtPropIndexes.DeFatalAttack, beChallengerData.extProps);

#region 天赋 [j 2015-06-10]
            robot.SavageValue = GetRobotExtProps((int)ExtPropIndexes.SavagePercent, beChallengerData.extProps);
            robot.ColdValue = GetRobotExtProps((int)ExtPropIndexes.ColdPercent, beChallengerData.extProps);
            robot.RuthlessValue = GetRobotExtProps((int)ExtPropIndexes.RuthlessPercent, beChallengerData.extProps);

            robot.DeSavageValue = GetRobotExtProps((int)ExtPropIndexes.DeSavagePercent, beChallengerData.extProps);
            robot.DeColdValue = GetRobotExtProps((int)ExtPropIndexes.DeColdPercent, beChallengerData.extProps);
            robot.DeRuthlessValue = GetRobotExtProps((int)ExtPropIndexes.DeRuthlessPercent, beChallengerData.extProps); 
#endregion

#region 元素属性 [XSea 2015/8/14]
            // 固定伤害
            robot.FireAttack = (int)GetRobotExtProps((int)ExtPropIndexes.FireAttack, beChallengerData.extProps);
            robot.WaterAttack = (int)GetRobotExtProps((int)ExtPropIndexes.WaterAttack, beChallengerData.extProps);
            robot.LightningAttack = (int)GetRobotExtProps((int)ExtPropIndexes.LightningAttack, beChallengerData.extProps);
            robot.SoilAttack = (int)GetRobotExtProps((int)ExtPropIndexes.SoilAttack, beChallengerData.extProps);
            robot.IceAttack = (int)GetRobotExtProps((int)ExtPropIndexes.IceAttack, beChallengerData.extProps);
            robot.WindAttack = (int)GetRobotExtProps((int)ExtPropIndexes.WindAttack, beChallengerData.extProps);

            // 穿透
            robot.FirePenetration = (int)GetRobotExtProps((int)ExtPropIndexes.FirePenetration, beChallengerData.extProps);
            robot.WaterPenetration = (int)GetRobotExtProps((int)ExtPropIndexes.WaterPenetration, beChallengerData.extProps);
            robot.LightningPenetration = (int)GetRobotExtProps((int)ExtPropIndexes.LightningPenetration, beChallengerData.extProps);
            robot.SoilPenetration = (int)GetRobotExtProps((int)ExtPropIndexes.SoilPenetration, beChallengerData.extProps);
            robot.IcePenetration = (int)GetRobotExtProps((int)ExtPropIndexes.IcePenetration, beChallengerData.extProps);
            robot.WindPenetration = (int)GetRobotExtProps((int)ExtPropIndexes.WindPenetration, beChallengerData.extProps);

            // 抗性
            robot.DeFirePenetration = (int)GetRobotExtProps((int)ExtPropIndexes.DeFirePenetration, beChallengerData.extProps);
            robot.DeWaterPenetration = (int)GetRobotExtProps((int)ExtPropIndexes.DeWaterPenetration, beChallengerData.extProps);
            robot.DeLightningPenetration = (int)GetRobotExtProps((int)ExtPropIndexes.DeLightningPenetration, beChallengerData.extProps);
            robot.DeSoilPenetration = (int)GetRobotExtProps((int)ExtPropIndexes.DeSoilPenetration, beChallengerData.extProps);
            robot.DeIcePenetration = (int)GetRobotExtProps((int)ExtPropIndexes.DeIcePenetration, beChallengerData.extProps);
            robot.DeWindPenetration = (int)GetRobotExtProps((int)ExtPropIndexes.DeWindPenetration, beChallengerData.extProps);
#endregion

            createSkillIDs(beChallengerData.skillDatas, robot);
            robot.RoleID = roleId;
            robot.UniqueID = Global.GetUniqueID();
            robot.PlayerId = beChallengerData.roleId;
            robot.Name = string.Format("Role_{0}", robot.RoleID);
#if ___CC___FUCK___YOU___BB___
            robot.XMonsterInfo.Name = beChallengerData.roleName;

           // robot.MonsterInfo.SpriteSpeedTickList = new int[] { 148, 222, 0, 222, 222, 0, 185, 0, 0, 0, 0, 100, 148 };
           // robot.MonsterInfo.EachActionFrameRange = new int[] { 3, 3, 0, 3, 3, 0, 3, 0, 0, 0, 0, 1, 3 };
           // robot.MonsterInfo.EffectiveFrame = new int[] { -1, -1, -1, 1, 1, 0, 1, -1, -1, -1, -1, -1, -1 };

            robot.Sex = beChallengerData.sex;
            //cur血
            robot.VLife = beChallengerData.extProps[(int)ExtPropIndexes.MaxLifeV];
            //cur蓝
            robot.VMana = beChallengerData.extProps[(int)ExtPropIndexes.MaxMagicV];
            //max血
            robot.XMonsterInfo.MaxHP = (int)beChallengerData.extProps[(int)ExtPropIndexes.MaxLifeV];
            //max蓝
           // robot.MonsterInfo.VManaMax = beChallengerData.extProps[(int)ExtPropIndexes.MaxMagicV];
            //移动速度
            robot.MoveSpeed = beChallengerData.extProps[(int)ExtPropIndexes.MoveSpeed];

            int baseOccupation = (beChallengerData.occupationId - beChallengerData.changeLiveCount) > 10 ? ((beChallengerData.occupationId - beChallengerData.changeLiveCount) / 10 - 1) : beChallengerData.occupationId;

            // 魔剑士类型 [XSea 2015/6/12]
            EMagicSwordTowardType eMagicSwordType = GameManager.MagicSwordMgr.GetMagicSwordTypeByWeapon(baseOccupation, robot.getRoleDataMini().GoodsDataList);

            //最大最小攻击力
            robot.XMonsterInfo.Ad = GetRobotMinAttack(baseOccupation, eMagicSwordType, beChallengerData); // [XSea 2015/6/12]
            //robot.XMonsterInfo.MaxAttack = GetRobotMaxAttack(baseOccupation, eMagicSwordType, beChallengerData); // [XSea 2015/6/12]
            //物防法防
            robot.XMonsterInfo.Pd = (int)beChallengerData.extProps[(int)ExtPropIndexes.MaxDefense];
            //robot.XMonsterInfo.MDefense = (int)beChallengerData.extProps[(int)ExtPropIndexes.MaxMDefense];
            //命中闪避
            robot.XMonsterInfo.DodgeChance = (int)beChallengerData.extProps[(int)ExtPropIndexes.HitV];
            robot.XMonsterInfo.DodgeResis = (int)beChallengerData.extProps[(int)ExtPropIndexes.Dodge];
            //回血
            //robot.XMonsterInfo.RecoverLifeV = beChallengerData.extProps[(int)ExtPropIndexes.LifeRecoverPercent];
            //回蓝
           // robot.XMonsterInfo.RecoverMagicV = beChallengerData.extProps[(int)ExtPropIndexes.MagicRecoverPercent];

            robot.XMonsterInfo.Level = beChallengerData.level;
         // robot.XMonsterInfo.ChangeLifeCount = beChallengerData.changeLiveCount; // 加一个转生次数，之前只给了等级。。 [XSea 2015/6/18]
            robot.XMonsterInfo.Exp = 0;
            

            //锁敌距离
            robot.XMonsterInfo.PursuitRange = 100;
            //robot.XMonsterInfo.EquipmentBody = -1;
           // robot.XMonsterInfo.EquipmentWeapon = -1;

           // robot.XMonsterInfo.ToOccupation = baseOccupation;

            //无掉落
            //robot.XMonsterInfo.FallGoodsPackID = -1;
          //  robot.MonsterType = (int)MonsterTypes.JingJiChangRobot;
          //  robot.XMonsterInfo.BattlePersonalJiFen = 0;
          //  robot.XMonsterInfo.BattleZhenYingJiFen = 0;
          //  robot.XMonsterInfo.FallBelongTo = 0;
           // robot.XMonsterInfo.DaimonSquareJiFen = 0;
            //robot.XMonsterInfo.BloodCastJiFen = 0;
           // robot.XMonsterInfo.WolfScore = 0;
           // robot.XMonsterInfo.AttackType = GetRobotAttackType(baseOccupation, eMagicSwordType); // 攻击类型 [XSea 2015/6/12]
#else
               robot.MonsterInfo.VSName = beChallengerData.roleName;

            robot.MonsterInfo.SpriteSpeedTickList = new int[] { 148, 222, 0, 222, 222, 0, 185, 0, 0, 0, 0, 100, 148 };
            robot.MonsterInfo.EachActionFrameRange = new int[] { 3, 3, 0, 3, 3, 0, 3, 0, 0, 0, 0, 1, 3 };
            robot.MonsterInfo.EffectiveFrame = new int[] { -1, -1, -1, 1, 1, 0, 1, -1, -1, -1, -1, -1, -1 };
            
            robot.Sex = beChallengerData.sex;
            //cur血
            robot.VLife = beChallengerData.extProps[(int)ExtPropIndexes.MaxLifeV];
            //cur蓝
            robot.VMana = beChallengerData.extProps[(int)ExtPropIndexes.MaxMagicV];
            //max血
            robot.MonsterInfo.VLifeMax = beChallengerData.extProps[(int)ExtPropIndexes.MaxLifeV];
            //max蓝
            robot.MonsterInfo.VManaMax = beChallengerData.extProps[(int)ExtPropIndexes.MaxMagicV];
            //移动速度
            robot.MoveSpeed = beChallengerData.extProps[(int)ExtPropIndexes.MoveSpeed];

            int baseOccupation = (beChallengerData.occupationId - beChallengerData.changeLiveCount) > 10 ? ((beChallengerData.occupationId - beChallengerData.changeLiveCount) / 10 - 1) : beChallengerData.occupationId;

            // 魔剑士类型 [XSea 2015/6/12]
            EMagicSwordTowardType eMagicSwordType = GameManager.MagicSwordMgr.GetMagicSwordTypeByWeapon(baseOccupation, robot.getRoleDataMini().GoodsDataList);

            //最大最小攻击力
            robot.MonsterInfo.MinAttack = GetRobotMinAttack(baseOccupation, eMagicSwordType, beChallengerData); // [XSea 2015/6/12]
            robot.MonsterInfo.MaxAttack = GetRobotMaxAttack(baseOccupation, eMagicSwordType, beChallengerData); // [XSea 2015/6/12]
            //物防法防
            robot.MonsterInfo.Defense = (int)beChallengerData.extProps[(int)ExtPropIndexes.MaxDefense];
            robot.MonsterInfo.MDefense = (int)beChallengerData.extProps[(int)ExtPropIndexes.MaxMDefense];
            //命中闪避
            robot.MonsterInfo.HitV = beChallengerData.extProps[(int)ExtPropIndexes.HitV];
            robot.MonsterInfo.Dodge = beChallengerData.extProps[(int)ExtPropIndexes.Dodge];
            //回血
            robot.MonsterInfo.RecoverLifeV = beChallengerData.extProps[(int)ExtPropIndexes.LifeRecoverPercent];
            //回蓝
            robot.MonsterInfo.RecoverMagicV = beChallengerData.extProps[(int)ExtPropIndexes.MagicRecoverPercent];
            
            robot.MonsterInfo.VLevel = beChallengerData.level;
            robot.MonsterInfo.ChangeLifeCount = beChallengerData.changeLiveCount; // 加一个转生次数，之前只给了等级。。 [XSea 2015/6/18]
            robot.MonsterInfo.VExperience = 0;
            robot.MonsterInfo.VMoney = 0;
            
            //锁敌距离
            robot.MonsterInfo.SeekRange = 100;
            robot.MonsterInfo.EquipmentBody = -1;
            robot.MonsterInfo.EquipmentWeapon = -1;

            robot.MonsterInfo.ToOccupation = baseOccupation;
           
            //无掉落
            robot.MonsterInfo.FallGoodsPackID = -1;
            robot.MonsterType = (int)MonsterTypes.JingJiChangRobot;
            robot.MonsterInfo.BattlePersonalJiFen = 0;
            robot.MonsterInfo.BattleZhenYingJiFen = 0;
            robot.MonsterInfo.FallBelongTo = 0;
            robot.MonsterInfo.DaimonSquareJiFen = 0;
            robot.MonsterInfo.BloodCastJiFen = 0;
            robot.MonsterInfo.WolfScore = 0;
            robot.MonsterInfo.AttackType = GetRobotAttackType(baseOccupation, eMagicSwordType); // 攻击类型 [XSea 2015/6/12]
#endif

            //无阵营
            robot.Camp = -1;
            robot.PetAiControlType = -1;
            //5帧搜寻一次敌人
            robot.NextSeekEnemyTicks = 500;
            robot.OwnerClient = null;

            // 新增属性 [XSea 2015/6/26]
            robot.FrozenPercent = GetRobotExtProps((int)ExtPropIndexes.FrozenPercent, beChallengerData.extProps);
            robot.PalsyPercent = GetRobotExtProps((int)ExtPropIndexes.PalsyPercent, beChallengerData.extProps);
            robot.SpeedDownPercent = GetRobotExtProps((int)ExtPropIndexes.SpeedDownPercent, beChallengerData.extProps);
            robot.BlowPercent = GetRobotExtProps((int)ExtPropIndexes.BlowPercent, beChallengerData.extProps);

            //安装Buff
            return robot;
        }

        private RoleDataMini createRoleDataMini(int roleId, PlayerJingJiData data)
        {
            RoleDataMini roleData = new RoleDataMini()
            {
                RoleID = roleId,
                RoleName = data.name,
                ZoneID = data.zoneId,
                RoleSex = data.sex,
                Occupation = data.occupationId,
                Level = data.level,
                MapCode = jingjiFuBenItem.GetIntValue("MapCode"),
                MaxLifeV = (int)data.extProps[(int)ExtPropIndexes.MaxLifeV],
                LifeV = (int)data.extProps[(int)ExtPropIndexes.MaxLifeV],
                MaxMagicV = (int)data.extProps[(int)ExtPropIndexes.MaxMagicV],
                MagicV = (int)data.extProps[(int)ExtPropIndexes.MaxMagicV],
                BodyCode = FindEquipCode(data.equipDatas, 1),
                WeaponCode = FindEquipCode(data.equipDatas, 0),
                GoodsDataList = GetUsingGoodsList(data.equipDatas),
                ChangeLifeLev = data.changeLiveCount,
                ChangeLifeCount = data.changeLiveCount,
                BufferMiniInfo = new List <BufferDataMini>(),
                MyWingData = data.wingData,
                SettingBitFlags = data.settingFlags,
            };
            roleData.BodyCode = Global.GMax(roleData.RoleSex, roleData.BodyCode);
            roleData.WeaponCode = Global.GMax(0, roleData.WeaponCode);

            return roleData;
        }

        /// <summary>
        /// 获取到正在装备的物品列表
        /// </summary>
        /// <param name="client"></param>
        public static List<GoodsData> GetUsingGoodsList(List<PlayerJingJiEquipData> equipDatas)
        {
            int WuQiNum = 0;
            int Hand = -1;
            List<GoodsData> goodsDataList = new List<GoodsData>();
            if (null != equipDatas)
            {
                for (int i = 0; i < equipDatas.Count; i++)
                {
                    int category = Global.GetGoodsCatetoriy(equipDatas[i].EquipId);

                    GoodsData data = new GoodsData();

                    data.GoodsID = equipDatas[i].EquipId;
                    data.ExcellenceInfo = equipDatas[i].ExcellenceInfo;
                    data.Forge_level = equipDatas[i].Forge_level;
                    data.Using = 1;
                    if (category >= (int)ItemCategories.WuQi_Jian && category <= (int)ItemCategories.WuQi_NuJianTong)
                    {
                        SystemXmlItem systemGoods = null;
                        if (GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(equipDatas[i].EquipId, out systemGoods))
                        {
                            WuQiNum++;
                            int handType = systemGoods.GetIntValue("HandType");
                            if ((int)WeaponHandType.WHT_BOTHTYPE != handType)
                            {
                                Hand = handType;
                            }
                            else
                            {
                                WuQiNum++;//两把武器并且至少有一把是双手武器，因此必须大于等于3
                            }
                        }
                    }
                    goodsDataList.Add(data);
                }
                if (WuQiNum >= 3)
                {
                    int tmpcategory;
                    for (int i = 0; i < goodsDataList.Count; i++)
                    {
                        tmpcategory = Global.GetGoodsCatetoriy(goodsDataList[i].GoodsID);
                        if (tmpcategory >= (int)ItemCategories.WuQi_Jian && tmpcategory <= (int)ItemCategories.WuQi_NuJianTong)
                        {
                            SystemXmlItem systemGoods = null;
                            if (GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsDataList[i].GoodsID, out systemGoods))
                            {
                                int handType = systemGoods.GetIntValue("HandType");
                                if ((int)WeaponHandType.WHT_BOTHTYPE == handType) //第一个双手武器改变佩戴默认值
                                {
                                    goodsDataList[i].BagIndex = Hand;//（在客户端）佩戴方式和配戴的BagIndex是左右相反的，所以当前这个的BagIndex正好另一只手装备的Hand值
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return goodsDataList;
        }

        /// <summary>
        /// 查找指定的装备代号
        /// </summary>
        /// <param name="equipDatas"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        private int FindEquipCode(List<PlayerJingJiEquipData> equipDatas, int category)
        {
            if (equipDatas == null) return -1;
            lock (equipDatas)
            {
                for (int i = 0; i < equipDatas.Count; i++)
                {
                    SystemXmlItem systemGoods = null;
                    if (GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(equipDatas[i].EquipId, out systemGoods))
                    {
                        // 逻辑平移--之前只有Weapon(又分Weapon和ShenBing)和Clothes(又分Clothes和ShenJia) 要去取EquipCode [10/28/2013 LiaoWei]
                        if ((category >= (int)ItemCategories.TouKui && category <= (int)ItemCategories.XueZi) ||
                                (category >= (int)ItemCategories.ZuoJi && category <= (int)ItemCategories.WuQi_NuJianTong))
                            return systemGoods.GetIntValue("EquipCode");

                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 竞技场中是否有相应的副本
        /// </summary>
        /// <param name="player"></param>
        /// <param name="monster"></param>
        public bool IsHaveFuBen(int nFuBenSeqID)
        {
            bool bContainFuBenID = false;
            lock (jingjichangInstances)
            {
                if (jingjichangInstances.ContainsKey(nFuBenSeqID))
                {
                    bContainFuBenID = true;
                }
            }

            return bContainFuBenID;
        }

        /// <summary>
        /// 玩家死亡，副本结束
        /// </summary>
        /// <param name="player"></param>
        /// <param name="monster"></param>
        public void onChallengeEndForPlayerDead(GameClient player, Monster monster)
        {
            if (player.ClientData.CopyMapID > 0 && player.ClientData.FuBenSeqID > 0
            && IsHaveFuBen(player.ClientData.FuBenSeqID)
            && player.ClientData.MapCode == jingjiFuBenItem.GetIntValue("MapCode")
            /*&& monster.MonsterType == (int)MonsterTypes.JingJiChangRobot*/
            && monster.CurrentMapCode == jingjiFuBenItem.GetIntValue("MapCode"))
            {
                JingJiChangInstance instance = null;
                lock (jingjichangInstances)
                {
                    jingjichangInstances.TryGetValue(player.ClientData.FuBenSeqID, out instance);
                }

                if (null == instance)
                    return;

                lock (instance)
                {
                    //已经处理过就不再处理了
                    if (instance.getState() == JingJiFuBenState.STOP_CD || instance.getState() == JingJiFuBenState.STOPED || instance.getState() == JingJiFuBenState.DESTROYED)
                        return;

                    Robot robot = instance.getRobot();

                    //停止战斗
                    robot.stopAttack();

                    JingJiChallengeResultData resultData = new JingJiChallengeResultData();

                    resultData.playerId = player.ClientData.RoleID;
                    resultData.robotId = robot.PlayerId;
                    resultData.isWin = false;

                    this.processFailed(player, robot, resultData);

                    instance.switchState(JingJiFuBenState.STOP_CD);
                }
            }
        }

        /// <summary>
        /// 怪物死亡，副本结束
        /// </summary>
        /// <param name="player"></param>
        /// <param name="monster"></param>
        public void onChallengeEndForMonsterDead(GameClient player, Monster monster)
        {
            if (player.ClientData.CopyMapID > 0 && player.ClientData.FuBenSeqID > 0
            && IsHaveFuBen(player.ClientData.FuBenSeqID)
            && player.ClientData.MapCode == jingjiFuBenItem.GetIntValue("MapCode")
            /*&& monster.MonsterType == (int)MonsterTypes.JingJiChangRobot*/
            && monster.CurrentMapCode == jingjiFuBenItem.GetIntValue("MapCode"))
            {
                JingJiChangInstance instance = null;

                lock (jingjichangInstances)
                {
                    if (!jingjichangInstances.TryGetValue(player.ClientData.FuBenSeqID, out instance))
                        return;
                }

                // 怪死了，"我"赢了
                if (monster.VLife <= 0 && player.ClientData.CurrentLifeV > 0)
                {
                    lock (instance)
                    {
                        //已经处理过就不再处理了
                        if (instance.getState() == JingJiFuBenState.STOP_CD || instance.getState() == JingJiFuBenState.STOPED || instance.getState() == JingJiFuBenState.DESTROYED)
                            return;

                        Robot robot = instance.getRobot();

                        //停止战斗
                robot.stopAttack();

                        JingJiChallengeResultData resultData = new JingJiChallengeResultData();

                        resultData.playerId = player.ClientData.RoleID;
                        resultData.robotId = robot.PlayerId;
                        resultData.isWin = true;

                        this.processWin(player, robot, resultData);

                        instance.switchState(JingJiFuBenState.STOP_CD);

                    }
                        
                }
                //"我"如果也死了，就算输了
                else
                {
                    onChallengeEndForPlayerDead(player, monster);
                }
                
            }
        }

        /// <summary>
        /// 玩家离开副本，副本结束
        /// </summary>
        /// <param name="player"></param>
        public void onChallengeEndForPlayerLeaveFuBen(GameClient player)
        {
            if (player.ClientData.CopyMapID > 0 && player.ClientData.FuBenSeqID > 0
            && IsHaveFuBen(player.ClientData.FuBenSeqID)
            && player.ClientData.MapCode == jingjiFuBenItem.GetIntValue("MapCode"))
            {
                JingJiChangInstance instance = null;

                lock (jingjichangInstances)
                {
                    if (!jingjichangInstances.TryGetValue(player.ClientData.FuBenSeqID, out instance))
                        return;
                }

                lock (instance)
                {
                    //已经处理过就不再处理了
                    if (instance.getState() == JingJiFuBenState.STOP_CD || instance.getState() == JingJiFuBenState.STOPED || instance.getState() == JingJiFuBenState.DESTROYED)
                        return;

                    Robot robot = instance.getRobot();
                    //停止战斗
                    robot.stopAttack();

                    JingJiChallengeResultData resultData = new JingJiChallengeResultData();

                    resultData.playerId = player.ClientData.RoleID;
                    resultData.robotId = robot.PlayerId;
                    resultData.isWin = false;

                    //处理失败
                    this.processFailed(player, robot, resultData);

                    //此时副本还木有销毁，但是玩家主动退出副本，如果玩家挂了，必须复活后才能切换场景
                    if (player.ClientData.CurrentLifeV <= 0)
                    {
                        this.relive(player);
                    }

                    //直接销毁
                    instance.switchState(JingJiFuBenState.DESTROYED);
                }

            }
        }

        /// <summary>
        /// 挑战结束（玩家退出）
        /// </summary>
        /// <param name="player"></param>
        public void onChallengeEndForPlayerLogout(GameClient player)
        {
            if (player.ClientData.CopyMapID > 0 && player.ClientData.FuBenSeqID > 0
            && IsHaveFuBen(player.ClientData.FuBenSeqID)
            && player.ClientData.MapCode == jingjiFuBenItem.GetIntValue("MapCode"))
            {
                JingJiChangInstance instance = null;

                lock (jingjichangInstances)
                {
                    if (!jingjichangInstances.TryGetValue(player.ClientData.FuBenSeqID, out instance))
                        return;
                }

                lock (instance)
                {
                    //已经处理过就不再处理了
                    if (instance.getState() == JingJiFuBenState.STOP_CD || instance.getState() == JingJiFuBenState.STOPED || instance.getState() == JingJiFuBenState.DESTROYED)
                        return;

                    Robot robot = instance.getRobot();

                    //停止战斗
                    robot.stopAttack();

                    JingJiChallengeResultData resultData = new JingJiChallengeResultData();

                    resultData.playerId = player.ClientData.RoleID;
                    resultData.robotId = robot.PlayerId;
                    resultData.isWin = false;

                    //处理失败
                    this.processFailed(player, robot, resultData);

                    //此时副本还木有销毁，但是玩家主动退出副本，如果玩家挂了，必须复活后才能切换场景
                    if (player.ClientData.CurrentLifeV <= 0)
                    {
                        this.relive(player);
                    }

                    //直接销毁
                    instance.switchState(JingJiFuBenState.DESTROYED);
                }
            }
            
        }

        /// <summary>
        /// 挑战胜利
        /// </summary>
        private void processWin(GameClient player, Robot robot, JingJiChallengeResultData resultData)
        {
            int ranking = getChallengeEndRanking(resultData, player.ServerId);

            int addShengWangValue;
            int addExpValue;
            int challengeCD;

            //获取奖励声望和经验
            getChallengeReward(player, ranking, true, out addShengWangValue, out addExpValue, out challengeCD);

            //奖励经验
            GameManager.ClientMgr.ProcessRoleExperience(player, addExpValue, true, true, false);

            //奖励声望
            this.changeShengWangValue(player, addShengWangValue);

            JingJiSaveData saveData = new JingJiSaveData();

            //更新数据
            saveData.isWin = true;
            saveData.nextChallengeTime = challengeCD > 0 ? TimeUtil.NOW() + challengeCD * TimeUtil.SECOND : 0;//Challenge_CD_Time;
            saveData.roleId = player.ClientData.RoleID;
            saveData.level = player.ClientData.Level;
            saveData.changeLiveCount = player.ClientData.ChangeLifeCount;
            saveData.combatForce = player.ClientData.CombatForce;
            saveData.equipDatas = getSaveEquipData(player);
            saveData.skillDatas = getSaveSkillData(player);
            saveData.baseProps = getBaseProps(player);
            saveData.extProps = getExtProps(player);
            saveData.robotId = robot.PlayerId;
            saveData.wingData = null;
            if (player.ClientData.MyWingData != null
                && player.ClientData.MyWingData.WingID > 0)
            {
                // 竞技场镜像附加翅膀
                saveData.wingData = player.ClientData.MyWingData;
            }
            saveData.settingFlags = Global.GetRoleParamsInt64FromDB(player, RoleParamName.SettingBitFlags);

            int winCount = Global.sendToDB<int, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_SAVE_DATA, DataHelper.ObjectToBytes<JingJiSaveData>(saveData), player.ServerId);

            if (winCount > 0)
            {
#if ___CC___FUCK___YOU___BB___
                LianShengGongGao(player, robot.XMonsterInfo.Name, false, winCount);
#else
                 LianShengGongGao(player, robot.MonsterInfo.VSName, false, winCount);
#endif
               
            }

            //通知客户端弹出窗口
            player.sendCmd((int)TCPGameServerCmds.CMD_SPR_JINGJI_CHALLENGE_END, string.Format("{0}:{1}:{2}:{3}", 1, addShengWangValue, addExpValue, ranking));

            // 七日活动
            SevenDayGoalEventObject evRank = SevenDayGoalEvPool.Alloc(player, ESevenDayGoalFuncType.JingJiChangRank);
            evRank.Arg1 = ranking;
            GlobalEventSource.getInstance().fireEvent(evRank);

            SevenDayGoalEventObject evWin = SevenDayGoalEvPool.Alloc(player, ESevenDayGoalFuncType.WinJingJiChangTimes);
            GlobalEventSource.getInstance().fireEvent(evWin);
        }

        /// <summary>
        /// 挑战失败
        /// </summary>
        private void processFailed(GameClient player, Robot robot, JingJiChallengeResultData resultData)
        {
            int ranking = getChallengeEndRanking(resultData, player.ServerId);

            int addShengWangValue;
            int addExpValue;
            int challengeCD;

            //获取奖励声望和经验
            getChallengeReward(player, ranking, false, out addShengWangValue, out addExpValue, out challengeCD);

            //添加经验
            GameManager.ClientMgr.ProcessRoleExperience(player, addExpValue, true, true, false);

            //添加声望
            this.changeShengWangValue(player, addShengWangValue);

            JingJiSaveData saveData = new JingJiSaveData();

            saveData.isWin = false;
            saveData.nextChallengeTime = challengeCD > 0 ? TimeUtil.NOW() + challengeCD * TimeUtil.SECOND : 0;//Challenge_CD_Time;
            saveData.robotId = robot.PlayerId;
            saveData.roleId = player.ClientData.RoleID;

            int winCount = Global.sendToDB<int, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_SAVE_DATA, DataHelper.ObjectToBytes<JingJiSaveData>(saveData), player.ServerId);

            if (winCount > 0)
            {
#if ___CC___FUCK___YOU___BB___
                LianShengGongGao(player, robot.XMonsterInfo.Name, false, winCount);
#else
                 LianShengGongGao(player, robot.MonsterInfo.VSName, false, winCount);
#endif
            }

            //通知客户端弹出窗口
            player.sendCmd((int)TCPGameServerCmds.CMD_SPR_JINGJI_CHALLENGE_END, string.Format("{0}:{1}:{2}:{3}", 0, addShengWangValue, addExpValue, ranking));
            
        }

        /// <summary>
        /// 连胜公告
        /// </summary>
        private void LianShengGongGao(GameClient player, string robotName, bool isWin, int winCount)
        {

        }

        /// <summary>
        /// 获取挑战奖励
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ranking"></param>
        /// <param name="isWin"></param>
        /// <param name="addShengWangValue"></param>
        /// <param name="addExpValue"></param>
        private void getChallengeReward(GameClient player, int ranking, bool isWin, out int addShengWangValue, out int addExpValue, out int challengeCD)
        {
            addShengWangValue = -1;
            addExpValue = -1;
            challengeCD = -1;

            foreach (SystemXmlItem xmlItem in jingjiMainConfig.SystemXmlItemDict.Values)
            {
                // 经验跟转生数有关
                int nChangeLev = player.ClientData.ChangeLifeCount;
                double nRate = 0;
                if (nChangeLev == 0)
                    nRate = 1;
                else
                    nRate = Data.ChangeLifeEverydayExpRate[nChangeLev];

                addExpValue = (int)(xmlItem.GetIntValue("ExpCoefficient1") * nRate);

                //500名以外的奖励
                if (ranking == -1 && xmlItem.GetStringValue("MaxRank").Equals(""))
                {
                    if (isWin)
                    {
                        addShengWangValue = xmlItem.GetIntValue("ShengWang1");
                        // Min(400,转生级别*400+人物等级)*名次对应经验系数
                        // addExpValue = Global.GMin(400, player.ClientData.ChangeLifeCount*400 + player.ClientData.Level) * xmlItem.GetIntValue("ExpCoefficient1");
                        // addExpValue = xmlItem.GetIntValue("ExpCoefficient1");                      

                        challengeCD = xmlItem.GetIntValue("CD");
                    }
                    else
                    {
                        addShengWangValue = xmlItem.GetIntValue("ShengWang1") / 2;
                        //Min(400,转生级别*400+人物等级)*名次对应经验系数 / 2
                        //addExpValue = Global.GMin(400, player.ClientData.ChangeLifeCount * 400 + player.ClientData.Level) * xmlItem.GetIntValue("ExpCoefficient1") / 2;

                        addExpValue = addExpValue / 2;
                        challengeCD = xmlItem.GetIntValue("CD");
                    }

                    break;
                }

                if (ranking >= xmlItem.GetIntValue("MinRank") && ranking <= xmlItem.GetIntValue("MaxRank"))
                {
                    if (isWin)
                    {
                        addShengWangValue = xmlItem.GetIntValue("ShengWang1");
                        //玩家竞技场名次 * 经验系数
                        //addExpValue = Global.GMin(400, player.ClientData.ChangeLifeCount * 400 + player.ClientData.Level) * xmlItem.GetIntValue("ExpCoefficient1");
                        //addExpValue = xmlItem.GetIntValue("ExpCoefficient1");

                        challengeCD = xmlItem.GetIntValue("CD");
                    }
                    else
                    {
                        addShengWangValue = xmlItem.GetIntValue("ShengWang1") / 2;
                        //玩家竞技场名次 * 经验系数
                        //addExpValue = Global.GMin(400, player.ClientData.ChangeLifeCount * 400 + player.ClientData.Level) * xmlItem.GetIntValue("ExpCoefficient1") / 2;
                        //addExpValue = xmlItem.GetIntValue("ExpCoefficient1") / 2;
                        addExpValue = addExpValue / 2;

                        challengeCD = xmlItem.GetIntValue("CD");
                    }

                    break;
                }

            }

            int nNeedVip = 0;
            nNeedVip = (int)GameManager.systemParamsList.GetParamValueDoubleByName("VIPJingJiCD");

            if (nNeedVip > 0 && player.ClientData.VipLevel >= nNeedVip)
            {
                challengeCD = 0;
            }

        }

        /// <summary>
        /// 获取挑战后的排名
        /// </summary>
        /// <returns></returns>
        private int getChallengeEndRanking(JingJiChallengeResultData resultData, int serverId)
        {
            return Global.sendToDB<int, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_CHALLENGE_END, DataHelper.ObjectToBytes<JingJiChallengeResultData>(resultData), serverId);
        }


        /// <summary>
        /// 竞技场复活
        /// </summary>
        /// <param name="player"></param>
        private void relive(GameClient player)
        {
            player.ClientData.CurrentLifeV = player.ClientData.LifeV;
            player.ClientData.CurrentMagicV = player.ClientData.MagicV;
            //角色复活
            Global.ClientRealive(player, (int)player.CurrentPos.X, (int)player.CurrentPos.Y, player.ClientData.RoleDirection);
        }

        /// <summary>
        /// 通知客户端开始倒计时
        /// </summary>
        /// <param name="instance"></param>
        public void onJingJiFuBenStartCD(JingJiChangInstance instance)
        {
            GameClient player = instance.getPlayer();
            player.sendCmd((int)TCPGameServerCmds.CMD_SPR_JINGJI_NOTIFY_START, "");
        }

        /// <summary>
        /// 冷却时间到，战斗开始
        /// </summary>
        /// <param name="instance"></param>
        public void onJingJiFuBenStarted(JingJiChangInstance instance)
        {
            GameClient player = instance.getPlayer();
            Robot robot = instance.getRobot();

            GameMap gameMap = GameManager.MapMgr.DictMaps[jingjiFuBenItem.GetIntValue("MapCode")];

            int gridX = gameMap.CorrectWidthPointToGridPoint(RobotBothX) / gameMap.MapGridWidth;
            int gridY = gameMap.CorrectHeightPointToGridPoint(RobotBothY) / gameMap.MapGridHeight;

            //AI进入竞技场
            GameManager.MonsterZoneMgr.AddDynamicRobot(player.CurrentMapCode, robot, player.ClientData.CopyMapID, 1, gridX, gridY, 1, 0, Tmsk.Contract.SceneUIClasses.NormalCopy, player.ClientData.RoleID);
        }

        public void onRobotBron(Robot robot)
        {
            //查找gameclient
            GameClient client = GameManager.ClientMgr.FindClient((int)robot.Tag);
            if (null == client)
            {
                return;
            }

            //通知客户端
            SendMySelfJingJiFakeRoleItem(client, robot);

            //开始战斗
            robot.startAttack();

            GameManager.ClientMgr.BroadSpecialMapAIEvent(client.ClientData.MapCode, client.ClientData.CopyMapID, 1, 0);
        }

        /// <summary>
        /// 发送竞技场假人数据
        /// </summary>
        /// <param name="player"></param>
        public void SendMySelfJingJiFakeRoleItem(GameClient player, Robot robot)
        {
            RoleDataMini roleDataMini = robot.getRoleDataMini();
            roleDataMini.PosX = (int)player.CurrentPos.X;
            roleDataMini.PosY = (int)player.CurrentPos.Y;

            player.sendCmd<RoleDataMini>((int)TCPGameServerCmds.CMD_OTHER_ROLE, roleDataMini);

        }

        /// <summary>
        /// 战斗结束，开始倒计时
        /// </summary>
        /// <param name="fubenId"></param>
        public void onJingJiFuBenStopForTimeOutCD(JingJiChangInstance instance)
        {
            if (null == instance)
                return;
            lock (instance)
            {
                GameClient player = instance.getPlayer();

                Robot robot = instance.getRobot();

                if (null != player && null != robot)
                {
                    //执行删除副本中的怪物的操作
                    GameManager.MonsterZoneMgr.DestroyCopyMapMonsters(player.ClientData.MapCode, player.ClientData.CopyMapID);

                    //停止战斗
                    robot.stopAttack();

                    JingJiChallengeResultData resultData = new JingJiChallengeResultData();

                    resultData.playerId = player.ClientData.RoleID;
                    resultData.robotId = robot.PlayerId;
                    resultData.isWin = false;

                    this.processFailed(player, robot, resultData);
                }

            }

        }

        /// <summary>
        /// 副本结束，传回原场景
        /// </summary>
        /// <param name="instance"></param>
        public void onJingJiFuBenStoped(JingJiChangInstance instance)
        {
            GameClient player = instance.getPlayer();

            if (null == player || player.LogoutState)
                return;

            //已经离开副本
            if (player.CurrentMapCode != jingjiFuBenItem.GetIntValue("MapCode"))
            {
                return;
            }

            //如果角色死亡，原地复活
            if (player.ClientData.CurrentLifeV <= 0)
            {
                relive(player);
            }

            //判断上次的地图有效性
            if(!Global.CanChangeMap(player, player.ClientData.LastMapCode, player.ClientData.LastPosX, player.ClientData.LastPosY))
            {
                player.ClientData.LastMapCode = GameManager.MainMapCode;
                player.ClientData.LastPosX = 0;
                player.ClientData.LastPosY = 0;
            }

            //回原场景
            GameManager.ClientMgr.ChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), player, -1, player.ClientData.LastMapCode, player.ClientData.LastPosX, player.ClientData.LastPosY, player.ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_MAPCHANGE);

            //通知客户端离开竞技场副本
            player.sendCmd((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_LEAVE, "");
        }

        /// <summary>
        /// 竞技场副本销毁
        /// </summary>
        /// <param name="instance"></param>
        public void onJingJiFuBenDestroy(JingJiChangInstance instance)
        {
#if !UseTimer
            instance.Handle.cannel();
#else
            ScheduleExecutor2.Instance.scheduleCancle(instance);
#endif
            lock (jingjichangInstances)
            {
                this.jingjichangInstances.Remove(instance.getFuBenSeqId());
            }

            instance.release();
        }

        /// <summary>
        /// 在倒计时结束的过程中请求离开副本
        /// </summary>
        public void onLeaveFuBenForStopCD(GameClient player)
        {
            JingJiChangInstance instance = null;
            lock (jingjichangInstances)
            {
                if (!jingjichangInstances.TryGetValue(player.ClientData.FuBenSeqID, out instance))
                    return;
            }

            lock (instance)
            {
                //已经处理过就不再处理了
                if (instance.getState() != JingJiFuBenState.STOP_CD)
                    return;

                Robot robot = instance.getRobot();
                //停止战斗
                robot.stopAttack();

                instance.switchState(JingJiFuBenState.STOPED);
            }
        }

        public int GetLeftEnterCount(GameClient client)
        {
            //每日免费进入次数
            int freeChallengeNum = jingjiFuBenItem.GetIntValue("EnterNumber");
            //获取玩家竞技场副本数据
            FuBenData jingjifuBenData = Global.GetFuBenData(client, jingjiFuBenId);
            //获取玩家进入竞技场总次数
            int nFinishNum;
            int useTotalNum = Global.GetFuBenEnterNum(jingjifuBenData, out nFinishNum);
            //获取已用免费进入次数
            int useFreeNum = useTotalNum <= jingjiFuBenItem.GetIntValue("EnterNumber") ? useTotalNum : jingjiFuBenItem.GetIntValue("EnterNumber");

            //获取Vip进入次数等级数据
            int[] vipJingjiCounts = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJingJi");

            //获取vip等级
            int playerVipLev = client.ClientData.VipLevel;

            //获取vip可进入次数
            int vipChallengeNum = vipJingjiCounts[playerVipLev];

            //vip已用次数
            int useVipNum = useTotalNum <= jingjiFuBenItem.GetIntValue("EnterNumber") ? 0 : useTotalNum - jingjiFuBenItem.GetIntValue("EnterNumber");

            return freeChallengeNum + vipChallengeNum - useFreeNum - useVipNum;
        }
    }



    /// <summary>
    /// 竞技场玩家升级事件监听器
    /// </summary>
    public class JingJiPlayerLevelupEventListener : IEventListener
    {
        private static JingJiPlayerLevelupEventListener instance = new JingJiPlayerLevelupEventListener();

        private JingJiPlayerLevelupEventListener() { }

        public static JingJiPlayerLevelupEventListener getInstance()
        {
            return instance;
        }

        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() != (int)EventTypes.PlayerLevelup)
            {
                return;
            }

            PlayerLevelupEventObject levelupEvent = (PlayerLevelupEventObject)eventObject;

            JingJiChangManager.getInstance().onPlayerLevelup(levelupEvent.Player);
            
        }
    }

    public class JingJiFuBenEndEventListener : IEventListener
    {
        private static JingJiFuBenEndEventListener instance = new JingJiFuBenEndEventListener();

        private JingJiFuBenEndEventListener() { }

        public static JingJiFuBenEndEventListener getInstance()
        {
            return instance;
        }

        public void processEvent(EventObject eventObject)
        {
            PlayerDeadEventObject playerDeadEvent = null;
            MonsterDeadEventObject monsterDeadEvent = null;
            JingJiFuBenEndForTimeEventObject endForTimeEvent = null;

            if (eventObject.getEventType() == (int)EventTypes.PlayerDead)
            {
                playerDeadEvent = (PlayerDeadEventObject)eventObject;
                JingJiChangManager.getInstance().onChallengeEndForPlayerDead(playerDeadEvent.getPlayer(), playerDeadEvent.getAttacker());
            }
            if (eventObject.getEventType() == (int)EventTypes.MonsterDead)
            {
                monsterDeadEvent = (MonsterDeadEventObject)eventObject;
                JingJiChangManager.getInstance().onChallengeEndForMonsterDead(monsterDeadEvent.getAttacker(), monsterDeadEvent.getMonster());
            }

        }
    }

    /// <summary>
    /// 玩家登出事件监听器
    /// </summary>
    public class JingJiPlayerLogoutEventListener : IEventListener
    {
        private static JingJiPlayerLogoutEventListener instance = new JingJiPlayerLogoutEventListener();

        private JingJiPlayerLogoutEventListener() { }

        public static JingJiPlayerLogoutEventListener getInstance()
        {
            return instance;
        }

        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() != (int)EventTypes.PlayerLogout)
                return;

            PlayerLogoutEventObject _eventObject = (PlayerLogoutEventObject)eventObject;

            JingJiChangManager.getInstance().onChallengeEndForPlayerLogout(_eventObject.getPlayer());
        }
    }

    /// <summary>
    /// 玩家离开副本监听器
    /// </summary>
    public class JingJiPlayerLeaveFuBenEventListener : IEventListener
    {
        private static JingJiPlayerLeaveFuBenEventListener instance = new JingJiPlayerLeaveFuBenEventListener();

        private JingJiPlayerLeaveFuBenEventListener() { }

        public static JingJiPlayerLeaveFuBenEventListener getInstance()
        {
            return instance;
        }

        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() != (int)EventTypes.PlayerLeaveFuBen)
                return;

            PlayerLeaveFuBenEventObject _eventObject = eventObject as PlayerLeaveFuBenEventObject;

            JingJiChangManager.getInstance().onChallengeEndForPlayerLeaveFuBen(_eventObject.getPlayer());
        }
    }
}
