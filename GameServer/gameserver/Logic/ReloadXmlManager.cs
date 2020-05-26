using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.TCP;
using GameServer.Logic.BossAI;
using GameServer.Logic.ExtensionProps;
using GameServer.Logic.UserReturn;
using GameServer.Logic.FluorescentGem;
using GameServer.Logic.CheatGuard;
using GameServer.Logic.Name;
using GameServer.Logic.Video;
using GameServer.Logic.Marriage.CoupleArena;

namespace GameServer.Logic
{
    /// <summary>
    /// 动态重新加载参数管理
    /// </summary>
    public class ReloadXmlManager
    {
        /// <summary>
        /// 重新加载程序配置参数文件
        /// </summary>
        /// <param name="xmlFileName"></param>
        /// <returns></returns>
        public static int ReloadXmlFile(string xmlFileName)
        {
            string lowerXmlFileName = xmlFileName.ToLower();

            if (Global.GetGiftExchangeFileName().ToLower() == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_activities();
            }
            else if ("config/gifts/biggift.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_biggift();
            }
            else if ("config/gifts/loginnumgift.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_loginnumgift();
            }
            else if ("config/gifts/huodongloginnumgift.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_huodongloginnumgift();
            }
            else if ("config/gifts/newrolegift.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_newrolegift();
            }
            else if ("config/gifts/uplevelgift.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_uplevelgift();
            } 
            else if ("config/gifts/onlietimegift.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_onlietimegift();
            }
            else if ("config/platconfig.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_platconfig();
            }
            else if ("config/mall.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_mall();
            }
            else if ("config/monstergoodslist.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_monstergoodslist();
            }
            else if ("config/broadcastinfos.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_broadcastinfos();
            }
            else if ("config/specialtimes.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_specialtimes();
            }
            else if ("config/battle.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_battle();
            }
            else if ("config/arenabattle.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_ArenaBattle();
            }
            else if ("config/popupwin.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_popupwin();
            }
            else if ("config/npcscripts.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_npcscripts();
            }
            else if ("config/systemoperations.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_systemoperations();
            }
            else if ("config/systemparams.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_systemparams();
            }
            else if ("config/goodsmergeitems.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_goodsmergeitems();
            }
            else if ("config/qizhengegoods.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_qizhengegoods();
            }
            else if ("config/npcsalelist.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_npcsalelist();
            }
            else if ("config/goods.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_goods();
            }
            else if ("config/goodspack.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_goodspack();
            }
            else if ("config/systemtasks.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_systemtasks();
            }
            else if ("config/taskzhangjie.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_taskzhangjie();
            }
            else if ("config/equipupgrade.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_equipupgrade();
            }
            else if ("config/dig.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_dig();
            }
            else if ("config/battleexp.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_battleexp();
            }
            else if ("config/rebirth.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_rebirth();
            }
            else if ("config/battleaward.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_Award();
            }
            else if ("config/equipborn.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_EquipBorn();
            }
            else if ("config/bornname.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_BornName();
            }
            //***************新活动部分*************************
            else if ("config/gifts/fanli.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_FanLi();
            }
            else if ("config/gifts/chongzhisong.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_ChongZhiSong();
            }
            else if ("config/gifts/chongzhiking.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_ChongZhiKing();
            }
            else if ("config/gifts/levelking.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_LevelKing();
            }
            else if ("config/gifts/bossking.xml" == lowerXmlFileName)//原来的装备王 ==》修改成boss王
            {
                return ReloadXmlFile_config_gifts_EquipKing();
            }
            else if ("config/gifts/wuxueking.xml" == lowerXmlFileName)//原来的经脉王 ==》修改成武学王
            {
                return ReloadXmlFile_config_gifts_HorseKing();
            }
            else if ("config/gifts/jingmaiking.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_JingMaiKing();
            }
            else if ("config/gifts/vipdailyawards.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_gifts_VipDailyAwards();
            }
            else if ("config/activity/activitytip.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_ActivityTip();
            }
            else if ("config/luckyaward.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_LuckyAward();
            }
            else if ("config/lucky.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_Lucky();
            }
            else if ("config/chengjiu.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_ChengJiu();
            }
            else if ("config/chengjiubuff.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_ChengJiuBuff();
            }
            else if ("config/jingmai.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_JingMai();
            }
            else if ("config/wuxue.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_WuXue();
            }
            else if ("config/zuanhuang.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_ZuanHuang();
            }
            else if ("config/vip.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_Vip();
            }
            else if ("config/qianggou.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_QiangGou();
            }
            else if ("config/hefugifts/hefuqianggou.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_HeFuQiangGou();
            }
            else if ("config/jierigifts/jirriqianggou.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_JieRiQiangGou();
            }
            else if ("config/systemopen.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_SystemOpen();
            }
            else if ("config/DailyActiveInfor.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_DailyActive();
            }
            else if ("DailyActiveAward.xml" == lowerXmlFileName)
            {
                return ReloadXmlFile_config_DailyActiveAward();
            }
            else if ("config/ipwhitelist.xml" == lowerXmlFileName)
            {
                CreateRoleLimitManager.Instance().LoadConfig();
                return 1;
            }
            else if ("kuafu" == lowerXmlFileName)
            {
                if (KuaFuManager.getInstance().InitConfig())
                {
                    return 1;
                }
            }
            else if ("langhunlingyu" == lowerXmlFileName)
            {
                if (LangHunLingYuManager.getInstance().InitConfig())
                {
                    return 1;
                }
            }
            else if ("config/chongzhi_app.xml" == lowerXmlFileName
                || "config/chongzhi_andrid.xml" == lowerXmlFileName || "config/chongzhi_android.xml" == lowerXmlFileName
                || "config/chongzhi_yueyu.xml" == lowerXmlFileName)
            {
                // 任何一项条件满足时，都会重新加载，但是重新加载时，会根据本平台，决定加载哪一项
                Global.InitFirstChargeConfigData();
            }
            else if ("config/AssInfo.xml" == lowerXmlFileName || "config/AssList.xml" == lowerXmlFileName || "config/AssConfig.xml" == lowerXmlFileName)
            {
                return RobotTaskValidator.getInstance().LoadRobotTaskData()? 1 : 0;
            }
            return -1000;
        }

        /// <summary>
        /// 重新加载所有程序配置参数文件
        /// </summary>
        /// <param name="xmlFileName"></param>
        /// <returns></returns>
        public static void ReloadAllXmlFile()
        {
            ReloadXmlFile_config_platconfig();
            ReloadXmlFile_config_gifts_activities();
            ReloadXmlFile_config_gifts_biggift();
            ReloadXmlFile_config_gifts_loginnumgift();
            ReloadXmlFile_config_gifts_huodongloginnumgift();
            ReloadXmlFile_config_gifts_newrolegift();
            ReloadXmlFile_config_gifts_uplevelgift();
            ReloadXmlFile_config_gifts_onlietimegift();
            ReloadXmlFile_config_mall();
            ReloadXmlFile_config_monstergoodslist();
            ReloadXmlFile_config_broadcastinfos();
            ReloadXmlFile_config_specialtimes();
            ReloadXmlFile_config_battle();
            ReloadXmlFile_config_ArenaBattle();
            ReloadXmlFile_config_popupwin();
            ReloadXmlFile_config_npcscripts();
            ReloadXmlFile_config_systemoperations();
            ReloadXmlFile_config_systemparams();
            ReloadXmlFile_config_goodsmergeitems();
            ReloadXmlFile_config_qizhengegoods();
            ReloadXmlFile_config_npcsalelist();
            ReloadXmlFile_config_goods();
            ReloadXmlFile_config_goodspack();
            ReloadXmlFile_config_systemtasks();
            ReloadXmlFile_config_equipupgrade();
            ReloadXmlFile_config_dig();
            ReloadXmlFile_config_battleexp();
            ReloadXmlFile_config_bangzhanawards();
            ReloadXmlFile_config_rebirth();
            ReloadXmlFile_config_Award();
            ReloadXmlFile_config_EquipBorn();
            ReloadXmlFile_config_BornName();
            ReloadXmlFile_config_gifts_FanLi();
            ReloadXmlFile_config_gifts_ChongZhiSong();
            ReloadXmlFile_config_gifts_ChongZhiKing();
            ReloadXmlFile_config_gifts_LevelKing();
            ReloadXmlFile_config_gifts_EquipKing();
            ReloadXmlFile_config_gifts_HorseKing();
            ReloadXmlFile_config_gifts_JingMaiKing();
            ReloadXmlFile_config_gifts_VipDailyAwards();
            ReloadXmlFile_config_ActivityTip();
            ReloadXmlFile_config_LuckyAward();
            ReloadXmlFile_config_Lucky();
            ReloadXmlFile_config_ChengJiu();
            ReloadXmlFile_config_ChengJiuBuff();
            ReloadXmlFile_config_JingMai();
            ReloadXmlFile_config_WuXue();
            ReloadXmlFile_config_ZuanHuang();
            ReloadXmlFile_config_Vip();
            ReloadXmlFile_config_QiangGou();
            ReloadXmlFile_config_HeFuQiangGou();
            ReloadXmlFile_config_JieRiQiangGou();
            ReloadXmlFile_config_SystemOpen();
            ReloadXmlFile_config_DailyActive();
            ReloadXmlFile_config_DailyActiveAward();

            ReloadXmlFile_config_gifts_JieRiType();
            ReloadXmlFile_config_gifts_JieRiLiBao();
            ReloadXmlFile_config_gifts_JieRiDengLu();
            ReloadXmlFile_config_gifts_JieRiVip();
            ReloadXmlFile_config_gifts_JieRiChongZhiSong();
            ReloadXmlFile_config_gifts_JieRiLeiJi();
            ReloadXmlFile_config_gifts_JieRiBaoXiang();
            ReloadXmlFile_config_gifts_JieRiXiaoFeiKing();
            ReloadXmlFile_config_gifts_JieRiChongZhiKing();
            ReloadXmlFile_config_gifts_JieRiTotalConsume();
            ReloadXmlFile_config_gifts_JieRiMultAward();

            ReloadXmlFile_config_bossAI();
            ReloadXmlFile_config_TuoZhan();
            ReloadXmlFile_config_MoJingAndQiFu();
            ReloadXmlFile_config_TotalLoginDataInfo();

            // 新服活动
            HuodongCachingMgr.ResetXinXiaoFeiKingActivity();

            // 重载合服活动配置
            HuodongCachingMgr.ResetHeFuActivityConfig();
            HuodongCachingMgr.ResetHeFuLoginActivity();
            HuodongCachingMgr.ResetHeFuTotalLoginActivity();
            HuodongCachingMgr.ResetHeFuRechargeActivity();
            HuodongCachingMgr.ResetHeFuPKKingActivity();
            HuodongCachingMgr.ResetHeFuAwardTimeActivity();
            HuodongCachingMgr.ResetHeFuLuoLanActivity();

            // 节日活动配置
            HuodongCachingMgr.ResetJieriActivityConfig();
            HuodongCachingMgr.ResetJieriDaLiBaoActivity();
            HuodongCachingMgr.ResetJieRiDengLuActivity();
            HuodongCachingMgr.ResetJieriCZSongActivity();
            HuodongCachingMgr.ResetJieRiLeiJiCZActivity();
            HuodongCachingMgr.ResetJieRiTotalConsumeActivity();
            HuodongCachingMgr.ResetJieRiMultAwardActivity();
            HuodongCachingMgr.ResetJieRiZiKaLiaBaoActivity();
            HuodongCachingMgr.ResetJieRiXiaoFeiKingActivity();
            HuodongCachingMgr.ResetJieRiCZKingActivity();
            HuodongCachingMgr.ResetJieriGiveActivity();
            HuodongCachingMgr.ResetJieRiGiveKingActivity();
            HuodongCachingMgr.ResetJieriRecvKingActivity();
            HuodongCachingMgr.ResetJieRiFanLiAwardActivity();
            HuodongCachingMgr.ResetJieriLianXuChargeActivity();
            HuodongCachingMgr.ResetJieriRecvActivity();
            HuodongCachingMgr.ResetJieriPlatChargeKingActivity();
            HuodongCachingMgr.ResetFirstChongZhiGift();
            HuodongCachingMgr.ResetTotalChargeActivity();
            HuodongCachingMgr.ResetTotalConsumeActivity();
            HuodongCachingMgr.ResetSeriesLoginItem();
            HuodongCachingMgr.ResetEveryDayOnLineAwardItem();
            HuodongCachingMgr.ResetJieriIPointsExchangeActivity();
            HuodongCachingMgr.ResetJieriFuLiActivity();

            if(!UserReturnManager.getInstance().IsUserReturnOpen())
                UserReturnManager.getInstance().initConfigInfo();

            //HuodongCachingMgr.ResetHeFuVIPActivity();
            //HuodongCachingMgr.ResetHeFuWCKingActivity();

            HuodongCachingMgr.ResetXinFanLiActivity();
            HuodongCachingMgr.ResetWeedEndInputActivity();
            HuodongCachingMgr.ResetSpecialActivity();

            Global.CachingJieriXmlData = null;
            Global.CachingSpecActXmlData = null;

            /// 重置补偿的字典
            BuChangManager.ResetBuChangItemDict();            

            // begin [7/24/2013 LiaoWei]
            //重置获取每日充值
            HuodongCachingMgr.ResetMeiRiChongZhiActivity();
            
            // 重置获取冲级豪礼活动的配置项
            HuodongCachingMgr.ResetChongJiHaoLiActivity();

            // 重置神装激情回馈 
            HuodongCachingMgr.ResetShenZhuangJiQiHuiKuiHaoLiActivity();

            // 重置月度抽奖活动
            HuodongCachingMgr.ResetYueDuZhuanPanActivity();

            //进入游戏时公告信息
            GongGaoDataManager.LoadGongGaoData();
            
            // end [7/24/2013 LiaoWei]

            //GameManager.systemQianKunMgr.ReloadLoadFromXMlFile();
            // 以前的祈福不能用了 [8/28/2014 LiaoWei]
            GameManager.systemImpetrateByLevelMgr.ReloadLoadFromXMlFile();
            QianKunManager.LoadImpetrateItemsInfo();
            QianKunManager.LoadImpetrateItemsInfoFree();
            QianKunManager.LoadImpetrateItemsInfoHuodong();
            
            GameManager.systemXingYunChouJiangMgr.ReloadLoadFromXMlFile();
            GameManager.systemYueDuZhuanPanChouJiangMgr.ReloadLoadFromXMlFile();
			
            Global.LoadSpecialMachineConfig();

            ElementhrtsManager.LoadRefineType();
            ElementhrtsManager.LoadElementHrtsBase();
            ElementhrtsManager.LoadElementHrtsLevelInfo();
            ElementhrtsManager.LoadSpecialElementHrtsExp();
            // 加载庆功宴配置
            GameManager.QingGongYanMgr.LoadQingGongYanConfig();

            // 加载精灵召唤配置
            CallPetManager.LoadCallPetType();
            CallPetManager.LoadCallPetConfig();
            CallPetManager.LoadCallPetSystem();

            Global.LoadGuWuMaps();

            // 加载自动重生地图 [XSea 2015/6/19]
            Global.LoadAutoReviveMaps();

            GameManager.MonsterZoneMgr.LoadAllMonsterXml();

            // 加载版本系统开放数据 [XSea 2015/5/4]
            GameManager.VersionSystemOpenMgr.LoadVersionSystemOpenData();

            // 充值配置文件
            Global.InitFirstChargeConfigData();

            RobotTaskValidator.getInstance().LoadRobotTaskData();

            // 梅林魔法书
            GameManager.MerlinMagicBookMgr.LoadMerlinConfigData();

            // 荧光宝石 [XSea 2015/8/13]
            GameManager.FluorescentGemMgr.LoadFluorescentGemConfigData();

            GetInterestingDataMgr.Instance().LoadConfig();

            // 玩家创建角色限制管理
            CreateRoleLimitManager.Instance().LoadConfig();

            //加载勇者战场配置文件
            YongZheZhanChangManager.getInstance().InitConfig();

            //加载王者战场配置文件
            KingOfBattleManager.getInstance().InitConfig();

            //跨服boss配置文件
            KuaFuBossManager.getInstance().InitConfig();

            //跨服主线地图配置文件
            KuaFuMapManager.getInstance().InitConfig();

            //初始化配置
            FashionManager.getInstance().InitConfig();

            // 精灵奇缘
            JingLingQiYuanManager.getInstance().InitConfig();

            //所有装备强化附加属性
            AllThingsCalcItem.InitAllForgeLevelInfo();

            TradeBlackManager.Instance().LoadConfig();

            Global.LoadLangDict();

            LogFilterConfig.InitConfig();
            TenRetutnManager.getInstance().InitConfig();

            //加载视频聊天室房间数据
            VideoLogic.LoadVideoXml(); 

            Data.LoadConfig();

            GiftCodeNewManager.getInstance().initGiftCode();//礼包码
        }

        /// <summary>
        /// 加载送礼窗口中的活动送礼配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_gifts_activities()
        {
            //礼品码数据，用于缓存
            Global._activitiesData = null;

            //重置送礼活动的项，强迫下次使用时重新加载
            return HuodongCachingMgr.ResetSongLiItem();
        }

        /// <summary>
        /// 加载送礼窗口中的大奖活动(充值有礼)配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_gifts_biggift()
        {
            //重置获取大奖活动的项,以便下次使用次，强迫重新读配置文件
            return HuodongCachingMgr.ResetBigAwardItem();
        }

        /// <summary>
        /// 加载送礼窗口中的登录有礼配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_gifts_loginnumgift()
        {
            //重置获取周连续登录的物品列表, 以便下次访问强迫读取配置文件
            return HuodongCachingMgr.ResetWLoginItem();
        }
        
        /// <summary>
        /// 加载限时累计登录窗口中的登录有礼配置文件
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_huodongloginnumgift()
        {
            //重置获取限时累计登录的物品列表, 以便下次访问强迫读取配置文件
            return HuodongCachingMgr.ResetLimitTimeLoginItem();
        }
        
        /// <summary>
        /// 加载送礼窗口中的见面有礼配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_gifts_newrolegift()
        {
            //重置获取新手见面的项
            return HuodongCachingMgr.ResetNewStepItem();
        }

        /// <summary>
        /// 加载送礼窗口中的升级有礼配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_gifts_uplevelgift()
        {
            //重置获取新手见面的项
            return HuodongCachingMgr.ResetUpLevelItem();
        }
        
        /// <summary>
        /// 加载送礼窗口中的在线有礼配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_gifts_onlietimegift()
        {
            //重置获取月在线时长的项, 以便下次访问时强迫从配置文件中获取
            return HuodongCachingMgr.ResetMOnlineTimeItem();
        }
        /// <summary>
        /// 重新加载PlatConfig.xml
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_platconfig()
        {
            return GameManager.PlatConfigMgr.ReloadPlatConfig();
        }

        /// <summary>
        /// 加载商城配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_mall()
        {
            MallPriceMgr.ClearCache();
            Global._MallSaleData = null; //清空缓存
            return GameManager.systemMallMgr.ReloadLoadFromXMlFile();
        }
        
        /// <summary>
        /// 加载怪物掉落配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_monstergoodslist()
        {
            //重置缓存项，以便下次访问时，从配置文件中重新读取
            return GameManager.GoodsPackMgr.ResetCachingItems();
        }
        
        /// <summary>
        /// 加载信息广播配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_broadcastinfos()
        {
            //加载文字播放列表
            try
            {
                BroadcastInfoMgr.LoadBroadcastInfoItemList();
            }
            catch(Exception)
            {
                return -1;
            }

            return 0;
        }
        
        /// <summary>
        /// 加载特殊时间段配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_specialtimes()
        {
            //重置特殊时间段限制缓存
            return SpecailTimeManager.ResetSpecialTimeLimits();
        }
        
        /// <summary>
        /// 加载炎黄战场配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_battle()
        {
            int ret = GameManager.SystemBattle.ReloadLoadFromXMlFile();
            GameManager.BattleMgr.LoadParams();
            return ret;
        }

        /// <summary>
        /// 加载角斗场配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_ArenaBattle()
        {
            int ret = GameManager.SystemArenaBattle.ReloadLoadFromXMlFile();
            GameManager.ArenaBattleMgr.LoadParams();
            return ret;
        }
        
        /// <summary>
        /// 加载弹窗配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_popupwin()
        {
            //加载弹窗列表
            try
            {
                PopupWinMgr.LoadPopupWinItemList();
            }
            catch (Exception)
            {
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// 加载NPC功能配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_npcscripts()
        {
            int ret = 0;

            try
            {
                //NPC功能脚本列表管理
                ret = GameManager.systemNPCScripts.ReloadLoadFromXMlFile();

                //初始化NPC功能脚本的公式列表
                GameManager.SystemMagicActionMgr.ParseNPCScriptActions(GameManager.systemNPCScripts);

                //清空NPC脚本时间限制缓存
                Global.ClearNPCScriptTimeLimits();
            }
            catch (Exception)
            {
                return -1;
            }

            return ret;
        }

        /// <summary>
        /// 加载NPC脚本配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_systemoperations()
        {
            // 初始化系统操作列表管理
            int ret = GameManager.SystemOperasMgr.ReloadLoadFromXMlFile();

            //清除NPC功能时间缓存
            Global.ClearNPCOperationTimeLimits();

            return ret;
        }

        /// <summary>
        /// 加载参数配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_systemparams()
        {
            int ret = GameManager.systemParamsList.ReloadLoadParamsList();

            //解析插旗战的日期和时间
            JunQiManager.ParseWeekDaysTimes();

            if (GameManager.OPT_ChengZhanType == 0)
            {
                //解析皇城战的日期和时间
                HuangChengManager.ParseWeekDaysTimes();

                //解析王城战的日期和时间
                WangChengManager.ParseWeekDaysTimes();
            }

            //重新读取罗兰城战配置文件
            LuoLanChengZhanManager.getInstance().InitConfig();

            //重置皇城地图编号
            Global.ResetHuangChengMapCode();

            //重置皇宫的地图编号
            Global.ResetHuangGongMapCode();

            //坐骑的名称
            Global.HorseNamesList = null;

            //坐骑的速度
            Global.HorseSpeedList = null;

            //生肖竞猜配置
            GameManager.ShengXiaoGuessMgr.ReloadConfig();

            //古墓配置
            Global.InitGuMuMapCodes();
            Global.InitVipGumuExpMultiple();

            //充值限制掉落的时间项
            GameManager.GoodsPackMgr.ResetLimitTimeRange();

            //缓存的二锅头物品列表
            Global.ErGuoTouGoodsIDList = null;

            //绑定铜钱符每日使用次数列表缓存
            Global._VipUseBindTongQianGoodsIDNum = null;

            //自动给予的物品的
            GameManager.AutoGiveGoodsIDList = null;

            //加载采集配置
            CaiJiLogic.LoadConfig();

            // 加载魔剑士静态数据 [XSea 2015/4/14]
            GameManager.MagicSwordMgr.LoadMagicSwordData();

            // 加载梅林魔法书静态数据 [XSea 2015/6/19]
            GameManager.MerlinMagicBookMgr.LoadMerlinSystemParamsConfigData();

            // LogGoods
            Global.LoadItemLogMark();

            // logTradeGoods
            Global.LoadLogTradeGoods();

            //强化最大等级相关配置
            Global.LoadForgeSystemParams();

            // 副本惩罚时间
            KuaFuManager.getInstance().InitCopyTime();

            // 魂石精华的经验配置
            SoulStoneManager.Instance().LoadJingHuaExpConfig();

            // 加载需要记录日志的怪物
            MonsterAttackerLogManager.Instance().LoadRecordMonsters();

            // 玩家创建角色限制管理
            CreateRoleLimitManager.Instance().LoadConfig();

            SpeedUpTickCheck.Instance().LoadConfig();

            NameManager.Instance().LoadConfig();

            CoupleArenaManager.Instance().InitSystenParams();

            return ret;
        }
        
        /// <summary>
        /// 加载合成配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_goodsmergeitems()
        {
            return MergeNewGoods.ReloadCacheMergeItems();
        }

        /// <summary>
        /// 加载奇珍阁配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_qizhengegoods()
        {
            int ret = GameManager.systemQiZhenGeGoodsMgr.ReloadLoadFromXMlFile();

            //清空初始化奇珍阁缓存项
            QiZhenGeManager.ClearQiZhenGeCachingItems();

            return ret;
        }

        /// <summary>
        /// 加载npc购买文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_npcsalelist()
        {
            return GameManager.NPCSaleListMgr.ReloadSaleList() ? 0 : -1;            
        }
        
        /// <summary>
        /// 加载物品文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_goods()
        {
            int ret = GameManager.SystemGoods.ReloadLoadFromXMlFile();
            if (ret < 0)
            {
                return ret;
            }

            // 物品名字索引管理
            GameManager.SystemGoodsNamgMgr.LoadGoodsItemsDict(GameManager.SystemGoods);

            //初始化物品的公式列表
            GameManager.SystemMagicActionMgr.ParseGoodsActions(GameManager.SystemGoods);

            //重新加载炎黄战场奖励的物品
            GameManager.BattleMgr.ReloadGiveAwardsGoodsDataList();

            //重新加载角斗场奖励物品 角斗场现在没有奖励物品，这个操作暂时无效
            GameManager.ArenaBattleMgr.ReloadGiveAwardsGoodsDataList();

            //清空装备耐久度字段缓存
            Global.ClearEquipGoodsMaxStrongDict();

            // 清空装备属性缓存
            GameManager.EquipPropsMgr.ClearCachedEquipPropItem();

            // 清除首饰goods的suit缓存
            Global.ClearCachedGoodsShouShiSuitID();

            Global.ResetCachedGoodsQuality();

            return ret;
        }

        private static int ReloadXmlFile_config_goodspack()
        {
            int ret = GameManager.systemGoodsBaoGuoMgr.ReloadLoadFromXMlFile();
            if (ret < 0)
            {
                return ret;
            }

            //重新加载物品包的配置缓存
            return GoodsBaoGuoCachingMgr.LoadGoodsBaoGuoDict();
        }

        private static int ReloadXmlFile_config_systemtasks()
        {
            //重新加载任务配置
            int ret = GameManager.SystemTasksMgr.ReloadLoadFromXMlFile();

            //重新加载NPC和任务映射配置
            GameManager.NPCTasksMgr.LoadNPCTasks(GameManager.SystemTasksMgr);

            //清空所有的缓存
            GameManager.TaskAwardsMgr.ClearAllDictionary();

            return ret;
        }

        public static int ReloadXmlFile_config_taskzhangjie()
        {
            int ret = GameManager.TaskZhangJie.ReloadLoadFromXMlFile();
            if (ret >= 0)
            {
                InitTaskZhangJieInfo();
            }
            return ret;
        }

        /// <summary>
        /// 加载任务章节配置信息,RangeKey记录适用该章节属性的任务ID范围
        /// </summary>
        public static void InitTaskZhangJieInfo()
        {
            try
            {
                GameManager.TaskZhangJieDict.Clear();

                int startTaskID = 0;
                int endTaskID = 0;
                SystemXmlItem preXmlItem = null;
                foreach (var kv in GameManager.TaskZhangJie.SystemXmlItemDict)
                {
                    endTaskID = kv.Value.GetIntValue("EndTaskID");
                    if (startTaskID != 0)
                    {
                        GameManager.TaskZhangJieDict.Add(new RangeKey(startTaskID, endTaskID - 1, preXmlItem));
                    }
                    startTaskID = endTaskID;
                    preXmlItem = kv.Value;
                }
                GameManager.TaskZhangJieDict.Add(new RangeKey(endTaskID, int.MaxValue, preXmlItem));
            }
            catch (Exception)
            {
                throw new Exception(string.Format("Init xml file : {0} fail", string.Format("Config/TaskZhangJie.xml")));
            }
        }

        private static int ReloadXmlFile_config_equipupgrade()
        {
            EquipUpgradeCacheMgr.LoadEquipUpgradeItems();
            return 0;
        }

        private static int ReloadXmlFile_config_dig()
        {
            //重新加载挖宝配置
            int ret = GameManager.systemWaBaoMgr.ReloadLoadFromXMlFile();
            return ret;
        }

        private static int ReloadXmlFile_config_battleexp()
        {
            int ret = GameManager.systemBattleExpMgr.ReloadLoadFromXMlFile();

            //清空缓存
            GameManager.BattleMgr.ClearBattleExpByLevels();

            return ret;
        }

        private static int ReloadXmlFile_config_bangzhanawards()
        {
            int ret = GameManager.systemBangZhanAwardsMgr.ReloadLoadFromXMlFile();

            //清空缓存
            BangZhanAwardsMgr.ClearAwardsByLevels();

            return ret;
        }

        private static int ReloadXmlFile_config_rebirth()
        {
            int ret = GameManager.systemBattleRebirthMgr.ReloadLoadFromXMlFile();
            return ret;
        }

        /// <summary>
        /// 重新加载隋唐战场奖励表
        /// </summary>
        private static int ReloadXmlFile_config_Award()
        {
            int ret = GameManager.systemBattleAwardMgr.ReloadLoadFromXMlFile();
            //清空缓存
            GameManager.BattleMgr.ClearBattleAwardByScore();

            return ret;
        }

        /// <summary>
        /// 重新加载装备天生洗练表
        /// </summary>
        private static int ReloadXmlFile_config_EquipBorn()
        {
            int ret = GameManager.systemEquipBornMgr.ReloadLoadFromXMlFile();

            return ret;
        }

        /// <summary>
        /// 重新加载装备天生属性级别名称表
        /// </summary>
        private static int ReloadXmlFile_config_BornName()
        {
            int ret = GameManager.systemBornNameMgr.ReloadLoadFromXMlFile();

            return ret;
        }

        /// <summary>
        /// 加载BossAI文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_bossAI()
        {
            int ret = GameManager.SystemBossAI.ReloadLoadFromXMlFile();
            if (ret < 0)
            {
                return ret;
            }

            //初始化BossAI的公式列表
            GameManager.SystemMagicActionMgr.ParseBossAIActions(GameManager.SystemBossAI);

            //使用双键值缓存boss AI项
            BossAICachingMgr.LoadBossAICachingItems(GameManager.SystemBossAI);

            return ret;
        }

        /// <summary>
        /// 加载拓展属性文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_TuoZhan()
        {
            int ret = GameManager.SystemExtensionProps.ReloadLoadFromXMlFile();
            if (ret < 0)
            {
                return ret;
            }

            //解析公式
            GameManager.SystemMagicActionMgr.ParseExtensionPropsActions(GameManager.SystemExtensionProps);

            //使用键值缓存拓展属性项
            ExtensionPropsMgr.LoadCachingItems(GameManager.SystemExtensionProps);

            return ret;
        }

        #region 新活动部分
        /// <summary>
        /// 充值返利配置文件
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_FanLi()
        {
            //重置充值返利配置文件，强迫下次使用时重新加载
            return HuodongCachingMgr.ResetInputFanLiActivity();
        }

        /// <summary>
        /// 充值加送配置文件
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_ChongZhiSong()
        {
            //重置充值加送配置文件，强迫下次使用时重新加载
            return HuodongCachingMgr.ResetInputSongActivity();
        }

        /// <summary>
        /// 充值王配置文件
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_ChongZhiKing()
        {
            //重置充值王配置文件，强迫下次使用时重新加载
            return HuodongCachingMgr.ResetInputKingActivity();
        }

        /// <summary>
        /// 冲级王配置文件
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_LevelKing()
        {
            //重置冲级王配置文件，强迫下次使用时重新加载
            return HuodongCachingMgr.ResetLevelKingActivity();
        }

        /// <summary>
        /// 装备王配置文件===>已经修改成boss王
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_EquipKing()
        {
            //重置装备王配置文件，强迫下次使用时重新加载
            return HuodongCachingMgr.ResetEquipKingActivity();
        }

        /// <summary>
        /// 坐骑王配置文件==>已经修改成武学王
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_HorseKing()
        {
            //重置坐骑王配置文件，强迫下次使用时重新加载
            return HuodongCachingMgr.ResetHorseKingActivity();
        }

        /// <summary>
        /// 经脉王配置文件
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JingMaiKing()
        {
            //重置经脉王配置文件，强迫下次使用时重新加载
            return HuodongCachingMgr.ResetJingMaiKingActivity();
        }

        /// <summary>
        /// Vip每日奖励缓存表
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_gifts_VipDailyAwards()
        {
            //Vip每日奖励缓存表
            GameManager.systemVipDailyAwardsMgr.LoadFromXMlFile("Config/Gifts/VipDailyAwards.xml", "", "AwardID", 1);

            return 1;
        }

        /// <summary>
        /// 活动引导提示缓存表
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_ActivityTip()
        {
            //活动引导提示缓存表
            GameManager.systemActivityTipMgr.LoadFromXMlFile("Config/Activity/ActivityTip.xml", "", "ID", 0);

            return 1;
        }

        /// <summary>
        /// 杨公宝库幸运值奖励缓存表
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_LuckyAward()
        {
            //杨公宝库幸运值奖励缓存表
            GameManager.systemLuckyAwardMgr.LoadFromXMlFile("Config/LuckyAward.xml", "", "ID");

            //砸金蛋幸运值奖励缓存表
            GameManager.systemLuckyAward2Mgr.LoadFromXMlFile("Config/LuckyAward2.xml", "", "ID");

            return 1;
        }

        /// <summary>
        /// 杨公宝库幸运值规则表
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_Lucky()
        {
            //杨公宝库幸运值规则表
            GameManager.systemLuckyMgr.LoadFromXMlFile("Config/Lucky.xml", "", "Number");

            return 1;
        }

        /// <summary>
        /// 成就配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_ChengJiu()
        {
            //成就配置文件
            GameManager.systemChengJiu.LoadFromXMlFile("Config/ChengJiu.xml", "ChengJiu", "ChengJiuID");
            ChengJiuManager.InitChengJiuConfig();

            return 1;
        }

        /// <summary>
        /// 重新加载魔晶和祈福兑换管理
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_MoJingAndQiFu()
        {
            //成就配置文件
            GameManager.SystemExchangeMoJingAndQiFu.LoadFromXMlFile("Config/DuiHuanItems.xml", "Items", "ID", 1);

            return 1;
        }

        /// <summary>
        /// 重新加载累计登陆奖励信息
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_TotalLoginDataInfo()
        {
            //成就配置文件
           // GameManager.SystemExchangeMoJingAndQiFu.LoadFromXMlFile("Config/DuiHuanItems.xml", "Items", "ID", 1);
            Program.LoadTotalLoginDataInfo();

            return 1;
        }


        /// <summary>
        /// 成就Buffer管理
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_ChengJiuBuff()
        {
            //杨公宝库幸运值奖励缓存表
            GameManager.systemChengJiuBuffer.LoadFromXMlFile("Config/ChengJiuBuff.xml", "", "ID");

            return 1;
        }

        /// <summary>
        /// 加载经脉配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_JingMai()
        {
            int ret = GameManager.SystemJingMaiLevel.ReloadLoadFromXMlFile();

            return ret;
        }

        /// <summary>
        /// 加载武学配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_WuXue()
        {
            int ret = GameManager.SystemWuXueLevel.ReloadLoadFromXMlFile();

            return ret;
        }

        /// <summary>
        /// 加载钻皇配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_ZuanHuang()
        {
            int ret = GameManager.SystemZuanHuangLevel.ReloadLoadFromXMlFile();

            return ret;
        }

        /// <summary>
        /// 加载系统激活项配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_SystemOpen()
        {
            int ret = GameManager.SystemSystemOpen.ReloadLoadFromXMlFile();

            return ret;
        }

        /// <summary>
        /// 加载vip配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_Vip()
        {
            Global.LoadVipLevelAwardList();

            return 1;
        }

        /// <summary>
        /// 加载抢购配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_QiangGou()
        {
            int ret = GameManager.SystemQiangGou.ReloadLoadFromXMlFile();

            return ret;
        }

        /// <summary>
        /// 加载合服配置文件
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_HeFuQiangGou()
        {
            int ret = GameManager.SystemHeFuQiangGou.ReloadLoadFromXMlFile();

            return ret;
        }

        /// <summary>
        /// 加载节日抢购配置文件
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_JieRiQiangGou()
        {
            int ret = GameManager.SystemJieRiQiangGou.ReloadLoadFromXMlFile();

            return ret;
        }

        /// <summary>
        /// 成就信息配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_DailyActive()
        {
            //成就配置文件
            GameManager.systemDailyActiveInfo.LoadFromXMlFile("Config/DailyActiveInfor.xml", "DailyActive", "DailyActiveID");

            return 1;
        }

        /// <summary>
        /// 成就奖励配置文件
        /// </summary>
        /// <returns></returns>
        private static int ReloadXmlFile_config_DailyActiveAward()
        {
            //成就配置文件
            GameManager.systemDailyActiveAward.LoadFromXMlFile("Config/DailyActiveAward.xml", "DailyActiveAward", "ID");

            return 1;
        }
        #endregion 新活动部分

        #region 大型节日活动

        /// <summary>
        /// 大型节日活动=>节日是否开启的配置
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiType()
        {
            return HuodongCachingMgr.ResetJieriActivityConfig();
        }

        /// <summary>
        /// 大型节日活动=>节日大礼包
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiLiBao()
        {
            return HuodongCachingMgr.ResetJieriDaLiBaoActivity();
        }

        /// <summary>
        /// 大型节日活动=>节日登录豪礼
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiDengLu()
        {
            return HuodongCachingMgr.ResetJieRiDengLuActivity();
        }

        /// <summary>
        /// 大型节日活动=>VIP大回馈
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiVip()
        {
            return HuodongCachingMgr.ResetJieriVIPActivity();
        }

        /// <summary>
        /// 大型节日活动=>充值大回馈
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiChongZhiSong()
        {
            return HuodongCachingMgr.ResetJieriCZSongActivity();
        }

        /// <summary>
        /// 大型节日活动=>累计充值豪礼
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiLeiJi()
        {
            return HuodongCachingMgr.ResetJieRiLeiJiCZActivity();
        }

        /// <summary>
        /// 大型节日活动=>字卡换礼盒
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiBaoXiang()
        {
            return HuodongCachingMgr.ResetJieRiZiKaLiaBaoActivity();
        }

        /// <summary>
        /// 大型节日活动=>五一消费王
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiXiaoFeiKing()
        {
            return HuodongCachingMgr.ResetJieRiXiaoFeiKingActivity();
        }

        /// <summary>
        /// 大型节日活动=>五一充值王
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiChongZhiKing()
        {
            return HuodongCachingMgr.ResetJieRiCZKingActivity();
        }

        /// <summary>
        /// 节日累计充值
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiTotalConsume()
        {
            return HuodongCachingMgr.ResetJieRiTotalConsumeActivity();
        }

        /// <summary>
        /// 节日多倍
        /// </summary>
        /// <returns></returns>
        public static int ReloadXmlFile_config_gifts_JieRiMultAward()
        {
            return HuodongCachingMgr.ResetJieRiMultAwardActivity();
        }

        #endregion 大型节日活动
    }
}
