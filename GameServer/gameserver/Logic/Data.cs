using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
//using System.Windows.Media.Animation;
using System.Threading;
using Server.Data;
using System.Xml.Linq;
using GameServer.Tools;
using Tmsk.Tools.Tools;
using Server.Tools;
using Tmsk.Contract;
//using System.Windows.Threading;

namespace GameServer.Logic
{
    /// <summary>
    /// 游戏静态数据
    /// </summary>
    public static class Data
    {
        /// <summary>
        /// 单元格走路移动需要的Tick
        /// </summary>
        public static int WalkUnitCost;

        /// <summary>
        /// 单元格跑步移动需要的Tick
        /// </summary>
        public static int RunUnitCost;

        /// <summary>
        /// 速度相关的列表
        /// </summary>
        public static int[] SpeedTickList;

        /// <summary>
        /// 走一步的距离
        /// </summary>
        public static int WalkStepWidth;

        /// <summary>
        /// 跑一步的距离
        /// </summary>
        public static int RunStepWidth;

        /// <summary>
        /// 发起物理攻击的距离
        /// </summary>
        public static int MaxAttackDistance;

        /// <summary>
        /// 物理攻击时的最短距离
        /// </summary>
        public static int MinAttackDistance;

        /// <summary>
        /// 发起魔法攻击的距离
        /// </summary>
        public static int MaxMagicDistance;

        /// <summary>
        /// 最小攻击间隔
        /// </summary>
        public static int MaxAttackSlotTick;

        /// <summary>
        /// 生命条的宽度
        /// </summary>
        public static int LifeTotalWidth;

        /// <summary>
        /// 精灵占据的格子宽度(个数)
        /// </summary>
        public static int HoldWidth;

        /// <summary>
        /// 精灵占据的格子高度(个数)
        /// </summary>
        public static int HoldHeight;

        /// <summary>
        /// 可以抢夺别人的物品包的最大时间间隔
        /// </summary>
        public static int GoodsPackOvertimeTick = 90;

        /// <summary>
        /// 包裹消失的时间隔
        /// </summary>
        public static int PackDestroyTimeTick = 90;

        /// <summary>
        /// 最大的任务追踪个数
        /// </summary>
        public static int TaskMaxFocusCount = 4;

        //原地复活的道具ID
        public static int AliveGoodsID = -1;

        //原地复活需要的最大级别
        public static int AliveMaxLevel = 10;

        /// <summary>
        /// 是否自动拾取物品进入背包
        /// </summary>
        public static int AutoGetThing = 0;

        /// <summary>
        /// <summary>
        /// 经验值升级表
        /// </summary>
        public static long[] LevelUpExperienceList = null;

        /// <summary>
        /// 角色的打坐收益列表
        /// </summary>
        public static RoleSitExpItem[] RoleSitExpList = null;

        /// <summary>
        /// 角色基础属性值列表
        /// </summary>
        public static List<RoleBasePropItem[]> RoleBasePropList = new List<RoleBasePropItem[]>();

        /// <summary>
        /// 允许摆摊的位置列表
        /// </summary>
        public static List<MapStallItem> MapStallList = new List<MapStallItem>(10);

        /// <summary>
        /// 地图的名称字典
        /// </summary>
        public static Dictionary<int, string> MapNamesDict = new Dictionary<int, string>(100);

        /// <summary>
        /// 精灵移动用Storyboard管理器
        /// </summary>
        //public static Dictionary<string, Storyboard> storyboard = new Dictionary<string, Storyboard>();

        /// <summary>
        /// 转职信息 [9/28/2013 LiaoWei] 
        /// </summary>
        /// key-OccupationID , value-ChangeOccupInfo
        public static Dictionary<int, ChangeOccupInfo> ChangeOccupInfoList = new Dictionary<int, ChangeOccupInfo>();

        /// <summary>
        /// 转生信息 [9/28/2013 LiaoWei] 
        /// </summary>
        /// key-OccupationID , value-ChangeLifeInfo
        //public static Dictionary<int, ChangeLifeInfo> ChangeLifeInfoList = new Dictionary<int, ChangeLifeInfo>();

        /// <summary>
        /// 最大的转生等级限制
        /// </summary>
       // public static int MaxChangeLifeCount = 0;

        /// <summary>
        /// 转生每天奖励经验系数 [6/25/2014 ChenXiaojun] 
        /// </summary>
        public static Dictionary<int, double> ChangeLifeEverydayExpRate = new Dictionary<int, double>();

        /// <summary>
        /// 职业加点信息 [9/28/2013 LiaoWei] 
        /// </summary>
        /// key-OccupationID , value-OccupationAddPointInfo
        public static Dictionary<int, OccupationAddPointInfo> OccupationAddPointInfoList = new Dictionary<int, OccupationAddPointInfo>();

        /// <summary>
        /// 转生加点信息 [3/6/2014 LiaoWei]
        /// </summary>
        /// key-ChangeLevel , value-AddPointInfo
        public static Dictionary<int, ChangeLifeAddPointInfo> ChangeLifeAddPointInfoList = new Dictionary<int, ChangeLifeAddPointInfo>();

        /// <summary>
        /// 血色城堡信息 [11/04/2013 LiaoWei] 
        /// </summary>
        /// key-mapcodeid , value-BloodCastleDataInfo
        public static Dictionary<int, BloodCastleDataInfo> BloodCastleDataInfoList = new Dictionary<int, BloodCastleDataInfo>();

        /// <summary>
        /// 膜拜配置
        /// </summary>
        public static Dictionary<int, MoBaiData> MoBaiDataInfoList = new Dictionary<int, MoBaiData>();

        /// <summary>
        /// 副本评分信息 [11/15/2013 LiaoWei]
        /// </summary>
        /// key-copyid , value-CopyScoreDataInfo
        public static Dictionary<int, List<CopyScoreDataInfo>> CopyScoreDataInfoList = new Dictionary<int, List<CopyScoreDataInfo>>();

        /// <summary>
        // 新手场景信息 [12/1/2013 LiaoWei]
        /// </summary>
        public static FreshPlayerCopySceneInfo FreshPlayerSceneInfo = new FreshPlayerCopySceneInfo();

        /// <summary>
        // 任务星级信息 [12/3/2013 LiaoWei]
        /// </summary>
        public static List<TaskStarDataInfo> TaskStarInfo = new List<TaskStarDataInfo>();

        /// <summary>
        // 日常跑环任务额外奖励信息 [12/3/2013 LiaoWei]
        /// </summary>
        public static List<DailyCircleTaskAwardInfo> DailyCircleTaskAward = new List<DailyCircleTaskAwardInfo>();

        /// <summary>
        // 讨伐任务额外奖励信息 
        /// </summary>
        public static TaofaTaskAwardInfo TaofaTaskExAward = new TaofaTaskAwardInfo();

        /// <summary>
        /// 战斗力信息表 [12/17/2013 LiaoWei]
        /// </summary>
        public static Dictionary<int, CombatForceInfo> CombatForceDataInfo = new Dictionary<int, CombatForceInfo>();

        /// <summary>
        /// 恶魔广场场景信息 [12/24/2013 LiaoWei]
        /// </summary>
        /// key-mapcodeid , value-DaimonSquareDataInfo
        public static Dictionary<int, DaimonSquareDataInfo> DaimonSquareDataInfoList = new Dictionary<int, DaimonSquareDataInfo>();

        /// <summary>
        /// 翅膀类型的武器由于强化而增加伤害加成静态数据 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] WingForgeLevelAddShangHaiJiaCheng = null;

        /// <summary>
        /// 翅膀类型的武器由于强化而增加防御比率静态数据 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] WingForgeLevelAddDefenseRates = null;

        /// <summary>
        /// 翅膀类型的武器由于强化而增加伤害吸收静态数据 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] WingForgeLevelAddShangHaiXiShou = null;
        
        /// <summary>
        /// 翅膀类型的武器由于追加而增加防御比率静态数据 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] WingZhuiJiaLevelAddDefenseRates = null;

        /// <summary>
        /// 非翅膀类型的武器由于强化而增加伤害加成静态数据 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] ForgeLevelAddAttackRates = null;

        /// <summary>
        /// 非翅膀类型的武器由于强化而增加防御比率静态数据 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] ForgeLevelAddDefenseRates = null;

        /// <summary>
        /// 非翅膀类型的武器由于追加而增加攻击静态数据 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] ZhuiJiaLevelAddAttackRates = null;

        /// <summary>
        /// 非翅膀类型的武器由于追加而增加防御比率静态数据 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] ZhuiJiaLevelAddDefenseRates = null;

        /// <summary>
        /// 非翅膀类型的武器由于强化而增加生命上限比率静态数据 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] ForgeLevelAddMaxLifeVRates = null;

        /// <summary>
        /// 卓越属性增加攻击力 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] ZhuoYueAddAttackRates = null;

        /// <summary>
        /// 卓越属性增加防御力 [1/24/2014 LiaoWei]
        /// </summary>
        public static double[] ZhuoYueAddDefenseRates = null;

        /// <summary>
        /// 强化需要的保护石ID [3/14/2014 LiaoWei]
        /// </summary>
        public static int[] ForgeProtectStoneGoodsID = null;

        /// <summary>
        /// 强化需要的保护石数量 [3/14/2014 LiaoWei]
        /// </summary>
        public static int[] ForgeProtectStoneGoodsNum = null;

        /// <summary>
        /// 钻石转换VIP经验比 [2/20/2014 LiaoWei]
        /// </summary>
        public static int DiamondToVipExpValue = 0;

        /// <summary>
        /// 红名Debuff信息 [4/21/2014 LiaoWei]
        /// </summary>
        public static double[] RedNameDebuffInfo = null;

        /// <summary>
        /// PK值衰减速度,每分钟10点
        /// </summary>
        public const int ConstSubPKPointPerMin = 10;

        /// <summary>
        /// 强化需要物品ID数组 [4/29/2014 LiaoWei]
        /// </summary>
        public static string[] ForgeNeedGoodsID = new string[Global.MaxForgeLevel + 1];

        /// <summary>
        /// 强化需要物品数量数组 [4/29/2014 LiaoWei]
        /// </summary>
        public static string[] ForgeNeedGoodsNum = new string[Global.MaxForgeLevel + 1];

        /// <summary>
        /// 世界地图传送所需金币数信息 [2/14/2014 LiaoWei]
        /// </summary>
        /// key-dayid , value-TotalLoginDataInfo
        public static Dictionary<int, int> MapTransNeedMoneyDict = new Dictionary<int, int>();

        /// <summary>
        /// 装备转生提升攻击力 [2/17/2014 LiaoWei]
        /// </summary>
        public static double[] EquipChangeLifeAddAttackRates = null;

        /// <summary>
        /// 装备转生提升防御力 [2/17/2014 LiaoWei]
        /// </summary>
        public static double[] EquipChangeLifeAddDefenseRates = null;

        /// <summary>
        /// 击杀BOSS成就计数 [3/12/2014 LiaoWei]
        /// </summary>
        public static int[] KillBossCountForChengJiu = null;

        /// <summary>
        /// 新增往随身仓库奖励物品的任务   任务ID和道具信息 [4/9/2014 LiaoWei]
        /// </summary>
        public static int InsertAwardtPortableBagTaskID = 0;

        /// <summary>
        /// 新增往随身仓库奖励物品的任务   任务ID和道具信息 [4/9/2014 LiaoWei]
        /// </summary>
        public static string InsertAwardtPortableBagGoodsInfo = null;

        /// <summary>
        /// 排行榜崇拜基础值 [6/22/2014 LiaoWei]
        /// </summary>
        public static int PaihangbangAdration = 0;

        /// <summary>
        /// 剧情副本ID [7/25/2014 LiaoWei]
        /// </summary>
        public static int[] StoryCopyMapID = null;

        /// <summary>
        /// 免费祈福间隔时间 [7/30/2014 LiaoWei]
        /// </summary>
        public static int FreeImpetrateIntervalTime = 0;

        /// <summary>
        /// 累计登陆奖励信息 [2/11/2014 LiaoWei]
        /// </summary>
        /// key-dayid , value-TotalLoginDataInfo
        public static Dictionary<int, TotalLoginDataInfo> TotalLoginDataInfoList = new Dictionary<int, TotalLoginDataInfo>();
        public static Object TotalLoginDataInfoListLock = new Object();
        public static int GetTotalLoginInfoNum() { lock (TotalLoginDataInfoListLock) { return TotalLoginDataInfoList.Count; } }
        public static TotalLoginDataInfo GetTotalLoginDataInfo(int nIdnex) 
        {
            lock (TotalLoginDataInfoListLock) 
            {
                if (TotalLoginDataInfoList.ContainsKey(nIdnex))
                    return TotalLoginDataInfoList[nIdnex];
             }
            return null;
         }

        /// <summary>
        /// VIP奖励信息 [2/19/2014 LiaoWei]
        /// </summary>
        /// key-VIPindex , value-VIPDataInfo
        public static Dictionary<int, VIPDataInfo> VIPDataInfoList = new Dictionary<int, VIPDataInfo>();

        /// <summary>
        /// VIP等级奖励和经验信息 [2/19/2014 LiaoWei]
        /// </summary>
        /// key-VIPindex , value-VIPDataInfo
        public static Dictionary<int, VIPLevAwardAndExpInfo> VIPLevAwardAndExpInfoList = new Dictionary<int, VIPLevAwardAndExpInfo>();

        /// <summary>
        /// 冥想收益 [3/5/2014 LiaoWei]
        /// </summary>
        /// key-VIPindex , value-VIPDataInfo
        public static Dictionary<int, MeditateData> MeditateInfoList = new Dictionary<int, MeditateData>();

        /// <summary>
        /// 经验副本场景信息 [3/18/2014 LiaoWei]
        /// </summary>
        /// key-mapcodeid , value-ExperienceCopyMapDataInfo
        public static Dictionary<int, ExperienceCopyMapDataInfo> ExperienceCopyMapDataInfoList = new Dictionary<int, ExperienceCopyMapDataInfo>();

        /// <summary>
        /// PK之王崇拜数据[3/25/2014 LiaoWei]
        /// </summary>
        public static PKKingAdrationData PKkingadrationData = new PKKingAdrationData();

        /// <summary>
        /// 罗兰城主崇拜数据 借用PK之王的结构
        /// </summary>
        public static PKKingAdrationData LLCZadrationData = new PKKingAdrationData();

        /// <summary>
        /// Boss之家数据 [4/7/2014 LiaoWei]
        /// </summary>
        public static BossHomeData BosshomeData = new BossHomeData();

        /// <summary>
        /// 黄金神庙数据 [4/7/2014 LiaoWei]
        /// </summary>
        public static GoldTempleData GoldtempleData = new GoldTempleData();
        
        /// <summary>
        /// 图鉴数据 [4/7/2014 LiaoWei]
        /// </summary>
        /// key-图鉴id , value-PictureJudgeData
        public static Dictionary<int, PictureJudgeData> PicturejudgeData = new Dictionary<int, PictureJudgeData>();

        /// <summary>
        /// 图鉴类型数据 [7/12/2014 LiaoWei]
        /// </summary>
        /// key-图鉴id , value-PictureJudgeTypeData
        public static Dictionary<int, PictureJudgeTypeData> PicturejudgeTypeData = new Dictionary<int, PictureJudgeTypeData>();

        /// <summary>
        /// 装备进阶数据 [4/30/2014 LiaoWei]
        /// </summary>
        /// key-武器分类id , value- key 武器的类ID, MuEquipUpgradeData
        public static Dictionary<int, Dictionary<int, MuEquipUpgradeData>> EquipUpgradeData = new Dictionary<int, Dictionary<int, MuEquipUpgradeData>>();

        /// <summary>
        /// 金币副本数据 [6/10/2014 LiaoWei]
        /// </summary>
        public static GoldCopySceneData Goldcopyscenedata = new GoldCopySceneData();

        /// <summary>
        /// 平台的充值信息，一个gameserver实例只对应一个平台
        /// </summary>
        private static SingleChargeData _ChargeData = null;
        /// <summary>
        /// 平台充值信息的锁
        /// </summary>
        private static Object SingleChargeDataMutex = new Object();

        /// <summary>
        /// 平台的充值信息公开属性
        /// </summary>
        public static SingleChargeData ChargeData
        {
            get { lock (SingleChargeDataMutex) { return _ChargeData; } }
            set { lock (SingleChargeDataMutex) { _ChargeData = value; } }
        }

        /// <summary>
        /// 翎羽升级升阶消耗材料对应的钻石
        /// </summary>
        public static Dictionary<int, int> LingYuMaterialZuanshiDict = new Dictionary<int, int>();

        /// <summary>
        /// 进入副本需要达到的任务编号
        /// </summary>
        public static Dictionary<int, int> FuBenNeedDict = new Dictionary<int, int>();

        /// <summary>
        /// 记录战力异常追踪日志的最小战力阈值
        /// </summary>
        public static long CombatForceLogMinValue = 1800000;

        /// <summary>
        /// 记录战力异常追踪日志的最小增幅比例
        /// </summary>
        public static double CombatForceLogPercent = 0.25;

        public const string GAME_CONFIG_USERWHITELIST_FILE = "Config/LoginUserWhiteList.xml";

        private static ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim();

        public static HashSet<string> UseridWhiteList = new HashSet<string>();

        /// <summary>
        /// 大天使ID列表
        /// </summary>
        public static HashSet<int> DaTianShiGoodsIdList = new HashSet<int>();

        public static HashSet<int> CanTeleportMapHashSet = new HashSet<int>()
            {
                (int)SceneUIClasses.BloodCastle,
                (int)SceneUIClasses.Demon,
                (int)SceneUIClasses.Battle,
                (int)SceneUIClasses.PKKing,
                (int)SceneUIClasses.AngelTemple,
                (int)SceneUIClasses.LuoLanChengZhan,
                (int)SceneUIClasses.ShuiJingHuanJing,
                (int)SceneUIClasses.GuZhanChang,
                (int)SceneUIClasses.BossFamily,
                (int)SceneUIClasses.HuangJinShengDian,
                (int)SceneUIClasses.HuanYingSiYuan,
                (int)SceneUIClasses.TianTi,
                (int)SceneUIClasses.YongZheZhanChang,
                (int)SceneUIClasses.KuaFuBoss,
                (int)SceneUIClasses.LangHunLingYu,
                (int)SceneUIClasses.KFZhengBa,
                (int)SceneUIClasses.CoupleArena,
                (int)SceneUIClasses.KingOfBattle,
            };

        #region 加载配置

        /// <summary>
        /// 加载配置,本配置须在其他配置加载完毕后再加载
        /// </summary>
        public static void LoadConfig()
        {
            try
            {
                //战力异常追踪日志参数
                double[] args0 = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZhanDouLiAlertLog");
                if (args0 != null && args0.Length == 2 && args0[0] >= 100000 && args0[1] >= 0.1)
                {
                    CombatForceLogMinValue = (long)args0[0];
                    CombatForceLogPercent = args0[1];
                }

                LoadUseridWhiteList();

                LoadSystemParamsValues();

                int iWithRname = (int)GameManager.systemParamsList.GetParamValueIntByName("LogWithRname");
                Global.WithRname = iWithRname == 1;
            }
            catch (System.Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// 加载帐号白名单
        /// </summary>
        public static void LoadUseridWhiteList()
        {
            string xmlFileName = Global.IsolateResPath(GAME_CONFIG_USERWHITELIST_FILE);

            try
            {
                readWriteLock.EnterWriteLock();
                UseridWhiteList.Clear();

                XElement xmlFile = ConfigHelper.Load(xmlFileName);
                if (null != xmlFile)
                {
                    IEnumerable<XElement> xmls = ConfigHelper.GetXElements(xmlFile, "WhiteList");
                    if (null != xmls)
                    {
                        foreach (var xml in xmls)
                        {
                            string platform = ConfigHelper.GetElementAttributeValue(xml, "PinTai", "");
                            if (0 == string.Compare(platform, GameCoreInterface.getinstance().GetPlatformType().ToString(), true))
                            {
                                string userId = ConfigHelper.GetElementAttributeValue(xml, "UserID", "");
                                UseridWhiteList.Add(userId.ToLower());
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 是否在帐号白名单
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool? InUserWriteList(string userId)
        {
            try
            {
                readWriteLock.EnterReadLock();
                if (UseridWhiteList.Count > 0)
                {
                    return UseridWhiteList.Contains(userId.ToLower());
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                return false;
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }

            return null;
        }

        private static void LoadSystemParamsValues()
        {
            try
            {
                readWriteLock.EnterWriteLock();

                #region 大天使ID列表

                DaTianShiGoodsIdList.Clear();
                int[] argsInt = GameManager.systemParamsList.GetParamValueIntArrayByName("DaTianShi");
                if (argsInt != null && argsInt.Length > 0)
                {
                    foreach (var id in argsInt)
                    {
                        DaTianShiGoodsIdList.Add(id);
                    }
                }

                #endregion 大天使ID列表
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        public static bool IsDaTianShiGoods(int goodsId)
        {
            try
            {
                readWriteLock.EnterReadLock();
                return DaTianShiGoodsIdList.Contains(goodsId);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                return false;
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }

            return false;
        }

        #endregion 加载配置
    }
}
