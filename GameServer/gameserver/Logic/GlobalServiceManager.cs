using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.BangHui.ZhanMengShiJian;
using GameServer.Logic.JingJiChang;
using GameServer.Logic.LiXianBaiTan;
using GameServer.Logic.LiXianGuaJi;
using GameServer.Server.CmdProcesser;
using GameServer.Server;
using GameServer.Logic.Copy;
using GameServer.Logic.BossAI;
using Tmsk.Contract;
using System.Threading;
using GameServer.Logic.UserReturn;
using GameServer.Logic.Talent;
using Tmsk.Tools;
using GameServer.Logic.MoRi;
using Server.Tools;
using GameServer.Logic.Today;
using GameServer.Logic.Building;
using GameServer.Logic.Ten;
using GameServer.Logic.ActivityNew.SevenDay;
using GameServer.Logic.Spread;
using GameServer.Logic.OnePiece;
using GameServer.Logic.FluorescentGem;
using GameServer.Logic.CheatGuard;
using GameServer.Logic.UnionPalace;
using GameServer.Logic.Goods;
using GameServer.Logic.UserActivate;
using GameServer.Logic.Tarot;
using GameServer.Logic.Marriage.CoupleArena;
using GameServer.Logic.UnionAlly;
using GameServer.Logic.Marriage.CoupleWish;

namespace GameServer.Logic
{
    /// <summary>
    /// 功能模块管理器接口
    /// </summary>
    public interface IManager
    {
        bool initialize();
        bool startup();
        bool showdown();
        bool destroy();
    }

    public interface IEventSource
    {

    }

    public interface IEventProcessor
    {

    }

    /// <summary>
    /// 全局功能模块服务管理器
    /// 负责统一初始化，开启，关闭，销毁所有的功能模块管理器
    /// </summary>
    public class GlobalServiceManager
    {
        private static Dictionary<int, List<IManager>> Scene2ManagerDict = new Dictionary<int, List<IManager>>();

        /// <summary>
        /// 注册管理器模块
        /// </summary>
        /// <param name="ManagerType"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        public static bool RegisterManager4Scene(int ManagerType, IManager manager)
        {
            lock (Scene2ManagerDict)
            {
                List<IManager> list;
                if (!Scene2ManagerDict.TryGetValue(ManagerType, out list))
                {
                    list = new List<IManager>();
                    Scene2ManagerDict[ManagerType] = list;
                }

                if (!list.Contains(manager))
                {
                    list.Add(manager);
                }
            }

            return true;
        }

        public static void initialize()
        {
#if BetaConfig
            int round = 9;
            Console.WriteLine("连接调试器或按任意键继续");
            do 
            {
                Console.Write("\b\b" + round);
                if (Console.KeyAvailable)
                {
                    break;
                }
                Thread.Sleep(1000);
            } while (--round > 0);
            Console.Write("\b\b");
#endif
            //战盟事件管理器
            ZhanMengShiJianManager.getInstance().initialize();

            //竞技场管理器
            JingJiChangManager.getInstance().initialize();

            //离线摆摊
            LiXianBaiTanManager.getInstance().initialize();

            //离线挂机
            LiXianGuaJiManager.getInstance().initialize();

            //副本活动组队管理器
            CopyTeamManager.Instance().initialize();

            //指令注册管理器
            CmdRegisterTriggerManager.getInstance().initialize();

            //发送指令管理
            SendCmdManager.getInstance().initialize();

            //Boss AI管理器
            BossAIManager.getInstance().initialize();

            //洗炼管理器
            WashPropsManager.initialize();

            //MU交易所
            SaleManager.getInstance().initialize();

            //炼制系统
            LianZhiManager.GetInstance().initialize();

            // 成就升级
            ChengJiuManager.GetInstance().initialize();

            //声望勋章
            PrestigeMedalManager.getInstance().initialize();

            UnionPalaceManager.getInstance().initialize();
            UserActivateManager.getInstance().initialize();

            PetSkillManager.getInstance().initialize();

            //玩家召回
            UserReturnManager.getInstance().initialize();

            //天赋
            TalentManager.getInstance().initialize();

            //每日专享
            TodayManager.getInstance().initialize();

            FundManager.getInstance().initialize();

            //警告
            WarnManager.getInstance().initialize();

            //恶魔来袭
            EMoLaiXiCopySceneManager.LoadEMoLaiXiCopySceneInfo();

            //罗兰法阵副本
            LuoLanFaZhenCopySceneManager.initialize();

            //情侣副本管理器
            MarryFuBenMgr.getInstance().initialize();
            MarryLogic.LoadMarryBaseConfig();
            MarryPartyLogic.getInstance().LoadMarryPartyConfig();

            //领地
            BuildingManager.getInstance().initialize();

            // 藏宝秘境
            OnePieceManager.getInstance().initialize();

            //初始化跨服相关管理器
            RegisterManager4Scene((int)SceneUIClasses.Normal, KuaFuManager.getInstance());
           // RegisterManager4Scene((int)SceneUIClasses.LangHunLingYu, LangHunLingYuManager.getInstance());
			
            //注册罗兰城战管理器
            RegisterManager4Scene((int)SceneUIClasses.LuoLanChengZhan, LuoLanChengZhanManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.Normal, FashionManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.HuanYingSiYuan, HuanYingSiYuanManager.getInstance());
            RegisterManager4Scene((int)ManagerTypes.ClientGoodsList, JingLingQiYuanManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.TianTi, TianTiManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.YongZheZhanChang, YongZheZhanChangManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.KingOfBattle, KingOfBattleManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.MoRiJudge, MoRiJudgeManager.Instance());
            RegisterManager4Scene((int)SceneUIClasses.ElementWar, ElementWarManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.CopyWolf, CopyWolfManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.KuaFuBoss, KuaFuBossManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.KuaFuMap, KuaFuMapManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.Spread, SpreadManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.KFZhengBa, ZhengBaManager.Instance());
            RegisterManager4Scene((int)SceneUIClasses.CoupleArena, CoupleArenaManager.Instance());
            RegisterManager4Scene((int)SceneUIClasses.Ally, AllyManager.getInstance());
            RegisterManager4Scene((int)SceneUIClasses.CoupleWish, CoupleWishManager.Instance());

            // 读取外挂列表和相关配置
            RobotTaskValidator.getInstance().Initialize(false, 0, 0, "");

            //初始化圣物系统相关配置
            HolyItemManager.getInstance().Initialize();

            //初始化塔罗牌相关配置
            TarotManager.getInstance().Initialize();

            // 七日活动
            SevenDayActivityMgr.Instance().initialize();

            // 魂石
            SoulStoneManager.Instance().initialize();

            TradeBlackManager.Instance().LoadConfig();
            //调用所有注册的管理模块的初始化函数
            lock (Scene2ManagerDict)
            {
                foreach (var list in Scene2ManagerDict.Values)
                {
                    foreach (var m in list)
                    {
                        bool success = m.initialize();
                        IManager2 m2 = m as IManager2;
                        if (null != m2)
                        {
                            success = success && m2.initialize(GameCoreInterface.getinstance());
                        }

                        if (GameManager.ServerStarting && !success)
                        {
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("执行{0}.initialize()失败,按任意键继续启动!", m.GetType()));
                            //Console.ReadKey(); HX_SERVER close the copy scenes;
                        }
                    }
                }
            }

            //应用宝
            TenManager.getInstance().initialize();
            TenRetutnManager.getInstance().initialize();

            //礼包码
            GiftCodeNewManager.getInstance().initialize();
        }
        
        public static void startup()
        {
            //战盟事件管理器
            ZhanMengShiJianManager.getInstance().startup();

            //竞技场管理器
            JingJiChangManager.getInstance().startup();

            //离线摆摊
            LiXianBaiTanManager.getInstance().startup();

            //离线挂机
            LiXianGuaJiManager.getInstance().startup();

            //副本活动组队管理器
            CopyTeamManager.Instance().startup();

            //指令注册管理器
            CmdRegisterTriggerManager.getInstance().startup();

            //发送指令管理
            SendCmdManager.getInstance().startup();

            //Boss AI管理器
            BossAIManager.getInstance().startup();

            //MU交易所
            SaleManager.getInstance().startup();

            //炼制系统
            LianZhiManager.GetInstance().startup();

            // 成就升级
            ChengJiuManager.GetInstance().startup();

            //玩家召回
            UserReturnManager.getInstance().startup();

            //天赋
            TalentManager.getInstance().startup();

            //每日专享
            TodayManager.getInstance().startup();

            FundManager.getInstance().startup();

            WarnManager.getInstance().startup();

            //声望勋章
            PrestigeMedalManager.getInstance().startup();

            UnionPalaceManager.getInstance().startup();
            UserActivateManager.getInstance().startup();

            PetSkillManager.getInstance().startup();

            //领地
            BuildingManager.getInstance().startup();

            //藏宝秘境
            OnePieceManager.getInstance().startup();

            TenManager.getInstance().startup();

            // 七日活动
            SevenDayActivityMgr.Instance().startup();

            SoulStoneManager.Instance().startup();

            //调用所有注册的管理模块的启动函数
            lock (Scene2ManagerDict)
            {
                foreach (var list in Scene2ManagerDict.Values)
                {
                    foreach (var m in list)
                    {
                        bool success = m.startup();
                        if (GameManager.ServerStarting && !success)
                        {
                            LogManager.WriteLog(LogTypes.Fatal, string.Format("初始化{0}.startup()失败,按任意键忽略此错误并继续启动服务器!", m.GetType()));
                            Console.ReadKey();
                        }
                    }
                }
            }
        }
        
        public static void showdown()
        {
            //战盟事件管理器
            ZhanMengShiJianManager.getInstance().showdown();

            //竞技场管理器
            JingJiChangManager.getInstance().showdown();

            //离线摆摊
            LiXianBaiTanManager.getInstance().showdown();

            //离线挂机
            LiXianGuaJiManager.getInstance().showdown();

            //副本活动组队管理器
            CopyTeamManager.Instance().showdown();

            //指令注册管理器
            CmdRegisterTriggerManager.getInstance().showdown();

            //发送指令管理
            SendCmdManager.getInstance().showdown();

            //Boss AI管理器
            BossAIManager.getInstance().showdown();

            //MU交易所
            SaleManager.getInstance().showdown();

            //炼制系统
            LianZhiManager.GetInstance().showdown();

            // 成就升级
            ChengJiuManager.GetInstance().showdown();

            //声望勋章
            PrestigeMedalManager.getInstance().showdown();

            UnionPalaceManager.getInstance().showdown();
            UserActivateManager.getInstance().showdown();

            PetSkillManager.getInstance().showdown();

            //玩家召回
            UserReturnManager.getInstance().showdown();

            //天赋
            TalentManager.getInstance().showdown();
             
            //每日专享
            TodayManager.getInstance().showdown();

            FundManager.getInstance().showdown();

            WarnManager.getInstance().showdown();

            //领地
            BuildingManager.getInstance().showdown();

            //藏宝秘境
            OnePieceManager.getInstance().showdown();

            //求婚和离婚未返回金钱处理
            MarryLogic.ApplyShutdownClear();

            TenManager.getInstance().showdown();

            // 七日活动
            SevenDayActivityMgr.Instance().showdown();

            SoulStoneManager.Instance().showdown();

            //调用所有注册的管理模块的停止函数
            lock (Scene2ManagerDict)
            {
                foreach (var list in Scene2ManagerDict.Values)
                {
                    foreach (var m in list)
                    {
                        m.showdown();
                    }
                }
            }
        }
        
        public static void destroy()
        {
            //战盟事件管理器
            ZhanMengShiJianManager.getInstance().destroy();

            //竞技场管理器
            JingJiChangManager.getInstance().destroy();

            //离线摆摊
            LiXianBaiTanManager.getInstance().destroy();

            //离线挂机
            LiXianGuaJiManager.getInstance().destroy();

            //副本活动组队管理器
            CopyTeamManager.Instance().destroy();

            //指令注册管理器
            CmdRegisterTriggerManager.getInstance().destroy();

            //发送指令管理
            SendCmdManager.getInstance().destroy();

            //Boss AI管理器
            BossAIManager.getInstance().destroy();

            //MU交易所
            SaleManager.getInstance().destroy();

            //炼制系统
            LianZhiManager.GetInstance().destroy();

            // 成就升级
            ChengJiuManager.GetInstance().destroy();

            //声望勋章
            PrestigeMedalManager.getInstance().destroy();

            UnionPalaceManager.getInstance().destroy();
            UserActivateManager.getInstance().destroy();

            PetSkillManager.getInstance().destroy();

            //玩家召回
            UserReturnManager.getInstance().destroy();

            //天赋
            TalentManager.getInstance().destroy();

             //每日专享
            TodayManager.getInstance().destroy();

            FundManager.getInstance().destroy();

            WarnManager.getInstance().destroy();

            //情侣副本管理器
            MarryFuBenMgr.getInstance().destroy();

            //领地
            BuildingManager.getInstance().destroy();

            //藏宝秘境
            OnePieceManager.getInstance().destroy();

            TenManager.getInstance().destroy();

            // 七日活动
            SevenDayActivityMgr.Instance().destroy();

            SoulStoneManager.Instance().destroy();

            //调用所有注册的管理模块的销毁函数
            lock (Scene2ManagerDict)
            {
                foreach (var list in Scene2ManagerDict.Values)
                {
                    foreach (var m in list)
                    {
                        m.destroy();
                    }
                }
            }
        }
    }
}
