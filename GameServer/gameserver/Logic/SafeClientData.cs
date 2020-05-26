using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Logic;
using System.Windows;
using GameServer.Logic.WanMota;
using GameServer.Logic.ExtensionProps;
using GameServer.Logic.YueKa;
using GameServer.Logic.Goods;
using GameServer.Core.Executor;
using GameServer.Logic.Spread;
using GameServer.Logic.UnionPalace;
using Tmsk.Contract.KuaFuData;
using GameServer.TarotData;
using GameServer.Logic.UserReturn;
//using System.Windows.Threading;

namespace GameServer.Logic
{
    public delegate void ChangePosEventHandler();

    /// <summary>
    /// 线程安全的客户端数据
    /// </summary>
    public class SafeClientData
    {
        #region 缓存的数据

        public int LastRoleCommonUseIntParamValueListTickCount = 0;

        /// <summary>
        /// 整理背包Tick 单线程
        /// </summary>
        public long _ResetBagTicks = 0;

        /// <summary>
        /// 刷新交易市场Tick 单线程
        /// </summary>
        public long _RefreshMarketTicks = 0;

        /// <summary>
        /// 宠物出战Tick 单线程
        /// </summary>
        public long _SpriteFightTicks = 0;

        /// <summary>
        /// 添加帮派成员Tick 单线程
        /// </summary>
        public long _AddBHMemberTicks = 0;

        /// <summary>
        /// 添加好友/黑名单/仇人Tick 单线程
        /// </summary>
        public long[] _AddFriendTicks = new long[] { 0, 0, 0 };

        #endregion 缓存的数据

        #region RoleDataEx 映射值

        /// <summary>
        /// 和DBserver同步的数据结构
        /// </summary>
        private RoleDataEx _RoleDataEx = null;

        //获取角色数据(禁止使用此数据修改)
        public RoleDataEx GetRoleData()
        {
            return _RoleDataEx;
        }

        /// <summary>
        /// 设置数据结构
        /// </summary>
        public RoleDataEx RoleData
        {
            set { lock (this) _RoleDataEx = value; }
        }

        /// <summary>
        /// 当前的角色ID
        /// </summary>
        public int RoleID
        {
            get { return _RoleDataEx.RoleID; }
        }

        /// <summary>
        /// 当前的角色ID
        /// </summary>
        public string RoleName
        {
            get { return _RoleDataEx.RoleName; }
        }

        /// <summary>
        /// 当前角色的性别
        /// </summary>
        public int RoleSex
        {
            get { return _RoleDataEx.RoleSex; }
        }

        /// <summary>
        /// 角色职业
        /// </summary>
        public int Occupation
        {
            get { return _RoleDataEx.Occupation; }
            set { _RoleDataEx.Occupation = value; }
        }

        /// <summary>
        /// 角色级别
        /// </summary>
        public int Level
        {
            get
            {
                int tmpVar = _RoleDataEx.Level;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.Level = value;
            }

            //get { lock (this) return _RoleDataEx.Level; }
            //set { lock (this) _RoleDataEx.Level = value; }
        }

        /// <summary>
        /// 角色所属的帮派
        /// </summary>
        public int Faction
        {
            get
            {
                int tmpVar = _RoleDataEx.Faction;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.Faction = value;
            }

            //get { lock (this) return _RoleDataEx.Faction; }
            //set { lock (this) _RoleDataEx.Faction = value; }
        }

        public List<AllyData> AllyList = null;

        public object LockAlly = new object();

        /// <summary>
        /// 绑定金币
        /// </summary>
        public int Money1
        {
            get
            {
                int tmpVar = _RoleDataEx.Money1;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.Money1 = value;
            }

            //get { lock (this) return _RoleDataEx.Money1; }
            //set { lock (this) _RoleDataEx.Money1 = value; }
        }

        /// <summary>
        /// 非绑定金币
        /// </summary>
        public int Money2
        {
            get
            {
                int tmpVar = _RoleDataEx.Money2;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.Money2 = value;
            }

            //get { lock (this) return _RoleDataEx.Money2; }
            //set { lock (this) _RoleDataEx.Money2 = value; }
        }

        /// <summary>
        /// 当前的经验
        /// </summary>
        public long Experience
        {
            get
            {
                return System.Threading.Thread.VolatileRead(ref _RoleDataEx.Experience);
            }

            set
            {
                System.Threading.Thread.VolatileWrite(ref _RoleDataEx.Experience, value);
            }

            //get { lock (this) return _RoleDataEx.Experience; }
            //set { lock (this) _RoleDataEx.Experience = value; }
        }

        /// <summary>
        /// 当前的PK模式
        /// </summary>
        public int PKMode
        {
            get
            {
                int tmpVar = _RoleDataEx.PKMode;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.PKMode = value;
            }

            //get { lock (this) return _RoleDataEx.PKMode; }
            //set { lock (this) _RoleDataEx.PKMode = value; }
        }

        /// <summary>
        /// 当前的PK值
        /// </summary>
        public int PKValue
        {
            get
            {
                int tmpVar = _RoleDataEx.PKValue;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.PKValue = value;
            }

            //get { lock (this) return _RoleDataEx.PKValue; }
            //set { lock (this) _RoleDataEx.PKValue = value; }
        }

        /// <summary>
        /// 所在的地图的编号
        /// </summary>
        public int MapCode
        {
            get
            {
                int tmpVar = _RoleDataEx.MapCode;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.MapCode = value;
            }

            //get { lock (this) return _RoleDataEx.MapCode; }
            //set { lock (this) _RoleDataEx.MapCode = value; }
        }

        /// <summary>
        /// 当前所在的位置X坐标
        /// </summary>
        public int PosX
        {
            get
            {
                int tmpVar = _RoleDataEx.PosX;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.PosX = value;
            }

            //get { lock (this) return _RoleDataEx.PosX; }
            //set { lock (this) _RoleDataEx.PosX = value; }
        }

        /// <summary>
        /// 当前所在的位置Y坐标
        /// </summary>
        public int PosY
        {
            get
            {
                int tmpVar = _RoleDataEx.PosY;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.PosY = value;

                //触发回到函数
                if (null != ChangePosHandler)
                {
                    ChangePosHandler();
                }
            }

            //get { lock (this) return _RoleDataEx.PosY; }
            //set
            //{
            //    lock (this)
            //    {
            //        _RoleDataEx.PosY = value;
            //    }

            //    //触发回到函数
            //    if (null != ChangePosHandler)
            //    {
            //        ChangePosHandler();
            //    }
            //}
        }

        /// <summary>
        /// 当前的方向
        /// </summary>
        public int RoleDirection
        {
            get
            {
                int tmpVar = _RoleDataEx.RoleDirection;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.RoleDirection = value;
            }

            //get { lock (this) return _RoleDataEx.RoleDirection; }
            //set { lock (this) _RoleDataEx.RoleDirection = value; }
        }

        /// <summary>
        /// 当前的生命值
        /// </summary>
        public int LifeV
        {
            get
            {
                int tmpLifeV = _RoleDataEx.LifeV;
                return tmpLifeV;
            }

            // get { lock (this) return _RoleDataEx.LifeV; }
            set
            {
                lock (this)
                {
                    if (_RoleDataEx.LifeV > 0)
                    {
                        CurrentLifeV = (int)((long)_CurrentLifeV * value / _RoleDataEx.LifeV);
                    }
                    else
                    {
                        CurrentLifeV = value;
                    }
                    _RoleDataEx.LifeV = value;
                }
            }
        }

        /// <summary>
        /// 当前的魔法值
        /// </summary>
        public int MagicV
        {
            get
            {
                int tmpMagicV = _RoleDataEx.MagicV;
                return tmpMagicV;
            }

            // get { lock (this) return _RoleDataEx.MagicV; }
            set
            {
                lock (this)
                {
                    if (_RoleDataEx.MagicV > 0)
                    {
                        CurrentMagicV = (int)((long)_CurrentMagicV * value / _RoleDataEx.MagicV);
                    }
                    else
                    {
                        CurrentMagicV = value;
                    }
                    _RoleDataEx.MagicV = value;
                }
            }
        }

        /// <summary>
        /// 已经完成的任务列表
        /// </summary>
        public List<OldTaskData> OldTasks
        {
            get { lock (this) return _RoleDataEx.OldTasks; }
            set { lock (this) _RoleDataEx.OldTasks = value; }
        }

        /// <summary>
        /// 当前的头像
        /// </summary>
        public int RolePic
        {
            get
            {
                int tmpVar = _RoleDataEx.RolePic;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.RolePic = value;
            }

            //get { lock (this) return _RoleDataEx.RolePic; }
            //set { lock (this) _RoleDataEx.RolePic = value; }
        }

        /// <summary>
        /// 当前背包的页数(总个数 - 1)
        /// </summary>
        public int BagNum
        {
            get
            {
                int tmpVar = _RoleDataEx.BagNum;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.BagNum = value;
            }

            //get { lock (this) return _RoleDataEx.BagNum; }
            //set { lock (this) _RoleDataEx.BagNum = value; }
        }

        /// <summary>
        /// 任务数据
        /// </summary>
        public List<TaskData> TaskDataList
        {
            get { lock (this) return _RoleDataEx.TaskDataList; }
            set { lock (this) _RoleDataEx.TaskDataList = value; }
        }

        /// <summary>
        /// 物品数据
        /// </summary>
        public List<GoodsData> GoodsDataList
        {
            get { lock (this) return _RoleDataEx.GoodsDataList; }
            set { lock (this) _RoleDataEx.GoodsDataList = value; }
        }

        /// <summary>
        /// 技能数据
        /// </summary>
        public List<SkillData> SkillDataList
        {
            get { lock (this) return _RoleDataEx.SkillDataList; }
            set { lock (this) _RoleDataEx.SkillDataList = value; }
        }

        /// <summary>
        /// 技能ID的集合
        /// </summary>
        public HashSet<int> SkillIdHashSet = new HashSet<int>();

        /// <summary>
        /// 称号
        /// </summary>
        public string OtherName
        {
            get { lock (this) return _RoleDataEx.OtherName; }
            set { lock (this) _RoleDataEx.OtherName = value; }
        }

        /// <summary>
        /// 主快捷面板的映射
        /// </summary>
        public string MainQuickBarKeys
        {
            get { lock (this) return _RoleDataEx.MainQuickBarKeys; }
            set { lock (this) _RoleDataEx.MainQuickBarKeys = value; }
        }

        /// <summary>
        /// 辅助快捷面板的映射
        /// </summary>
        public string OtherQuickBarKeys
        {
            get { lock (this) return _RoleDataEx.OtherQuickBarKeys; }
            set { lock (this) _RoleDataEx.OtherQuickBarKeys = value; }
        }

        /// <summary>
        /// 登陆的次数
        /// </summary>
        public int LoginNum
        {
            get { lock (this) return _RoleDataEx.LoginNum; }
            set { lock (this) _RoleDataEx.LoginNum = value; }
        }

        /// <summary>
        /// 充值的钱数
        /// </summary>
        public int UserMoney
        {
            get { lock (this) return _RoleDataEx.UserMoney; }
            set { lock (this) _RoleDataEx.UserMoney = value; }
        }

        /// <summary>
        /// 剩余的自动挂机时间
        /// </summary>
        public int LeftFightSeconds
        {
            get { lock (this) return _RoleDataEx.LeftFightSeconds; }
            set { lock (this) _RoleDataEx.LeftFightSeconds = value; }
        }

        /// <summary>
        /// 好友列表
        /// </summary>
        public List<FriendData> FriendDataList
        {
            get { lock (this) return _RoleDataEx.FriendDataList; }
            set { lock (this) _RoleDataEx.FriendDataList = value; }
        }

        /// <summary>
        /// 坐骑数据列表
        /// </summary>
        public List<HorseData> HorsesDataList
        {
            get { lock (this) return _RoleDataEx.HorsesDataList; }
            set { lock (this) _RoleDataEx.HorsesDataList = value; }
        }

        /// <summary>
        /// 当前骑乘的坐骑数据库ID
        /// </summary>
        public int HorseDbID
        {
            get { lock (this) return _RoleDataEx.HorseDbID; }
            set { lock (this) _RoleDataEx.HorseDbID = value; }
        }

        /// <summary>
        /// 宠物数据列表
        /// </summary>
        public List<PetData> PetsDataList
        {
            get { lock (this) return _RoleDataEx.PetsDataList; }
            set { lock (this) _RoleDataEx.PetsDataList = value; }
        }

        /// <summary>
        /// 当前放出的宠物的数据库ID
        /// </summary>
        public int PetDbID
        {
            get { lock (this) return _RoleDataEx.PetDbID; }
            set { lock (this) _RoleDataEx.PetDbID = value; }
        }

        /// <summary>
        /// 角色的内力值
        /// </summary>
        public int InterPower
        {
            get { lock (this) return _RoleDataEx.InterPower; }
            set { lock (this) _RoleDataEx.InterPower = value; }
        }

        /// <summary>
        /// 角色的经脉数据
        /// </summary>
        public List<JingMaiData> JingMaiDataList
        {
            get { lock (this) return _RoleDataEx.JingMaiDataList; }
            set { lock (this) _RoleDataEx.JingMaiDataList = value; }
        }

        /// <summary>
        /// 点将积分
        /// </summary>
        public int DJPoint
        {
            get { lock (this) return _RoleDataEx.DJPoint; }
            set { lock (this) _RoleDataEx.DJPoint = value; }
        }

        /// <summary>
        /// 点将总的比赛场次
        /// </summary>
        public int DJTotal
        {
            get { lock (this) return _RoleDataEx.DJTotal; }
            set { lock (this) _RoleDataEx.DJTotal = value; }
        }

        /// <summary>
        /// 点将获胜的场次
        /// </summary>
        public int DJWincnt
        {
            get { lock (this) return _RoleDataEx.DJWincnt; }
            set { lock (this) _RoleDataEx.DJWincnt = value; }
        }

        /// <summary>
        /// 总的在线秒数
        /// </summary>
        public int TotalOnlineSecs
        {
            get { lock (this) return _RoleDataEx.TotalOnlineSecs; }
            set { lock (this) _RoleDataEx.TotalOnlineSecs = value; }
        }

        /// <summary>
        /// 防止沉迷在线秒数
        /// </summary>
        public int AntiAddictionSecs
        {
            get { lock (this) return _RoleDataEx.AntiAddictionSecs; }
            set { lock (this) _RoleDataEx.AntiAddictionSecs = value; }
        }

        /// <summary>
        /// 上次离线时间
        /// </summary>
        public long LastOfflineTime
        {
            get { lock (this) return _RoleDataEx.LastOfflineTime; }
            set { lock (this) _RoleDataEx.LastOfflineTime = value; }
        }

        /// <summary>
        ///  本次闭关的开始时间
        /// </summary>
        public long BiGuanTime
        {
            get { lock (this) return _RoleDataEx.BiGuanTime; }
            set { lock (this) _RoleDataEx.BiGuanTime = value; }
        }

        /// <summary>
        ///  系统绑定的银两
        /// </summary>
        public int YinLiang
        {
            get { lock (this) return _RoleDataEx.YinLiang; }
            set { lock (this) _RoleDataEx.YinLiang = value; }
        }

        /// <summary>
        ///  从别人冲脉获取的经验值(累加)
        /// </summary>
        public int TotalJingMaiExp
        {
            get { lock (this) return _RoleDataEx.TotalJingMaiExp; }
            set { lock (this) _RoleDataEx.TotalJingMaiExp = value; }
        }

        /// <summary>
        ///  从别人冲脉获取的经验的次数
        /// </summary>
        public int JingMaiExpNum
        {
            get { lock (this) return _RoleDataEx.JingMaiExpNum; }
            set { lock (this) _RoleDataEx.JingMaiExpNum = value; }
        }

        /// <summary>
        /// 注册时间
        /// </summary>
        public long RegTime
        {
            get { lock (this) return _RoleDataEx.RegTime; }
            set { lock (this) _RoleDataEx.RegTime = value; }
        }

        /// <summary>
        /// 上一次的坐骑ID
        /// </summary>
        public int LastHorseID
        {
            get { lock (this) return _RoleDataEx.LastHorseID; }
            set { lock (this) _RoleDataEx.LastHorseID = value; }
        }

        /// <summary>
        /// 出售中的物品数据
        /// </summary>
        public List<GoodsData> SaleGoodsDataList
        {
            get { lock (this) return _RoleDataEx.SaleGoodsDataList; }
            set { lock (this) _RoleDataEx.SaleGoodsDataList = value; }
        }

        /// <summary>
        /// 缺省的技能ID
        /// </summary>
        public int DefaultSkillID
        {
            get { lock (this) return _RoleDataEx.DefaultSkillID; }
            set { lock (this) _RoleDataEx.DefaultSkillID = value; }
        }

        /// <summary>
        /// 自动补血喝药的百分比
        /// </summary>
        public int AutoLifeV
        {
            get { lock (this) return _RoleDataEx.AutoLifeV; }
            set { lock (this) _RoleDataEx.AutoLifeV = value; }
        }

        /// <summary>
        /// 自动补蓝喝药的百分比
        /// </summary>
        public int AutoMagicV
        {
            get { lock (this) return _RoleDataEx.AutoMagicV; }
            set { lock (this) _RoleDataEx.AutoMagicV = value; }
        }

        /// <summary>
        /// Buffer的数据列表
        /// </summary>
        public List<BufferData> BufferDataList
        {
            get { lock (this) return _RoleDataEx.BufferDataList; }
            set { lock (this) _RoleDataEx.BufferDataList = value; }
        }

        public HashSet<int> BufferDataListHashSet = new HashSet<int>();

        /// <summary>
        /// 跑环的数据列表
        /// </summary>
        public List<DailyTaskData> MyDailyTaskDataList
        {
            get { lock (this) return _RoleDataEx.MyDailyTaskDataList; }
            set { lock (this) _RoleDataEx.MyDailyTaskDataList = value; }
        }

        /// <summary>
        /// 每日冲穴的次数数据
        /// </summary>
        public DailyJingMaiData MyDailyJingMaiData
        {
            get { lock (this) return _RoleDataEx.MyDailyJingMaiData; }
            set { lock (this) _RoleDataEx.MyDailyJingMaiData = value; }
        }

        /// <summary>
        /// 自动增加熟练度的被动技能ID
        /// </summary>
        public int NumSkillID
        {
            get { lock (this) return _RoleDataEx.NumSkillID; }
            set { lock (this) _RoleDataEx.NumSkillID = value; }
        }

        /// <summary>
        /// 随身仓库数据
        /// </summary>
        public PortableBagData MyPortableBagData
        {
            get { lock (this) return _RoleDataEx.MyPortableBagData; }
            set { lock (this) _RoleDataEx.MyPortableBagData = value; }
        }

        /// <summary>
        /// 活动送礼相关数据
        /// </summary>
        public HuodongData MyHuodongData
        {
            get { lock (this) return _RoleDataEx.MyHuodongData; }
            set { lock (this) _RoleDataEx.MyHuodongData = value; }
        }

        /// <summary>
        /// 专享活动相关数据
        /// </summary>


        /// <summary>
        /// 副本数据
        /// </summary>
        public List<FuBenData> FuBenDataList
        {
            get { lock (this) return _RoleDataEx.FuBenDataList; }
            set { lock (this) _RoleDataEx.FuBenDataList = value; }
        }

        /// <summary>
        // 副本通关奖励道具 - 抽奖中的道具 临时保存下 防止外挂 [3/5/2014 LiaoWei]
        /// </summary>
        public GoodsData _CopyMapAwardTmpGoods = null;

        /// <summary>
        // 副本通关奖励道具 - 抽奖中的道具 临时保存下 防止外挂 [3/5/2014 LiaoWei]
        /// </summary>
        public GoodsData CopyMapAwardTmpGoods
        {
            get { lock (this) return _CopyMapAwardTmpGoods; }
            set { lock (this) _CopyMapAwardTmpGoods = value; }
        }

        /// <summary>
        /// 副本通过奖励抽奖 - 随机获得的魔晶值
        /// </summary>
        public int FuBenPingFenAwardMoJing;

        /// <summary>
        /// 已经完成的主线任务的ID
        /// </summary>
        public int MainTaskID
        {
            get { lock (this) return _RoleDataEx.MainTaskID; }
            set { lock (this) _RoleDataEx.MainTaskID = value; }
        }

        /// <summary>
        /// 当前的PK点
        /// </summary>
        public int PKPoint
        {
            get { lock (this) return _RoleDataEx.PKPoint; }
            set { lock (this) _RoleDataEx.PKPoint = value; }
        }

        /// <summary>
        /// 上次处理红名Buff时记录的pk值
        /// </summary>
        public int LastPKPoint = 0;

        private int _TmpPKPoint = 0;

        /// <summary>
        /// 用来通知DB的临时变量 PK点
        /// </summary>
        public int TmpPKPoint
        {
            get { lock (this) return _TmpPKPoint; }
            set { lock (this) _TmpPKPoint = value; }
        }

        /// <summary>
        /// 最高连斩数
        /// </summary>
        public int LianZhan
        {
            get { lock (this) return _RoleDataEx.LianZhan; }
            set { lock (this) _RoleDataEx.LianZhan = value; }
        }

        /// <summary>
        /// 角色每日数据
        /// </summary>
        public RoleDailyData MyRoleDailyData
        {
            get { lock (this) return _RoleDataEx.MyRoleDailyData; }
            set { lock (this) _RoleDataEx.MyRoleDailyData = value; }
        }

        /// <summary>
        /// 杀BOSS的总个数
        /// </summary>
        public int KillBoss
        {
            get { lock (this) return _RoleDataEx.KillBoss; }
            set { lock (this) _RoleDataEx.KillBoss = value; }
        }

        /// <summary>
        /// 押镖的数据
        /// </summary>
        public YaBiaoData MyYaBiaoData
        {
            get { lock (this) return _RoleDataEx.MyYaBiaoData; }
            set { lock (this) _RoleDataEx.MyYaBiaoData = value; }
        }

        /// <summary>
        /// 角斗场荣誉称号开始时间
        /// </summary>
        public long BattleNameStart
        {
            get { lock (this) return _RoleDataEx.BattleNameStart; }
            set { lock (this) _RoleDataEx.BattleNameStart = value; }
        }

        /// <summary>
        /// 角斗场荣誉称号
        /// </summary>
        public int BattleNameIndex
        {
            get { lock (this) return _RoleDataEx.BattleNameIndex; }
            set { lock (this) _RoleDataEx.BattleNameIndex = value; }
        }

        /// <summary>
        /// 充值TaskID
        /// </summary>
        public int CZTaskID
        {
            get { lock (this) return _RoleDataEx.CZTaskID; }
            set { lock (this) _RoleDataEx.CZTaskID = value; }
        }

        /// <summary>
        /// 角斗场称号次数
        /// </summary>
        public int BattleNum
        {
            get { lock (this) return _RoleDataEx.BattleNum; }
            set { lock (this) _RoleDataEx.BattleNum = value; }
        }

        /// <summary>
        /// 英雄逐擂的层数
        /// </summary>
        public int HeroIndex
        {
            get { lock (this) return _RoleDataEx.HeroIndex; }
            set { lock (this) _RoleDataEx.HeroIndex = value; }
        }

        /// <summary>
        /// 区ID
        /// </summary>
        public int ZoneID
        {
            get { lock (this) return _RoleDataEx.ZoneID; }
            set { lock (this) _RoleDataEx.ZoneID = value; }
        }

        /// <summary>
        /// 帮会名称
        /// </summary>
        public string BHName
        {
            get { lock (this) return _RoleDataEx.BHName; }
            set { lock (this) _RoleDataEx.BHName = value; }
        }

        /// <summary>
        /// 被邀请加入帮会时是否验证
        /// </summary>
        public int BHVerify
        {
            get { lock (this) return _RoleDataEx.BHVerify; }
            set { lock (this) _RoleDataEx.BHVerify = value; }
        }

        /// <summary>
        /// 帮会职务
        /// </summary>
        public int BHZhiWu
        {
            get { lock (this) return _RoleDataEx.BHZhiWu; }
            set { lock (this) _RoleDataEx.BHZhiWu = value; }
        }

        /// <summary>
        /// 帮会每日贡日ID1
        /// </summary>
        public int BGDayID1
        {
            get { lock (this) return _RoleDataEx.BGDayID1; }
            set { lock (this) _RoleDataEx.BGDayID1 = value; }
        }

        /// <summary>
        /// 帮会每日铜钱帮贡
        /// </summary>
        public int BGMoney
        {
            get { lock (this) return _RoleDataEx.BGMoney; }
            set { lock (this) _RoleDataEx.BGMoney = value; }
        }

        /// <summary>
        /// 帮会每日贡日ID2
        /// </summary>
        public int BGDayID2
        {
            get { lock (this) return _RoleDataEx.BGDayID2; }
            set { lock (this) _RoleDataEx.BGDayID2 = value; }
        }

        /// <summary>
        /// 帮会每日道具帮贡
        /// </summary>
        public int BGGoods
        {
            get { lock (this) return _RoleDataEx.BGGoods; }
            set { lock (this) _RoleDataEx.BGGoods = value; }
        }

        /// <summary>
        /// 帮会帮贡
        /// </summary>
        public int BangGong
        {
            get { lock (this) return _RoleDataEx.BangGong; }
            set { lock (this) _RoleDataEx.BangGong = value; }
        }

        /// <summary>
        /// 是否皇后
        /// </summary>
        public int HuangHou
        {
            get { lock (this) return _RoleDataEx.HuangHou; }
            set { lock (this) _RoleDataEx.HuangHou = value; }
        }

        /// <summary>
        /// 自己在排行中的位置字典
        /// </summary>
        public Dictionary<int, int> PaiHangPosDict
        {
            get { lock (this) return _RoleDataEx.PaiHangPosDict; }
            set { lock (this) _RoleDataEx.PaiHangPosDict = value; }
        }

        /// <summary>
        /// 劫镖的日ID
        /// </summary>
        public int JieBiaoDayID
        {
            get { lock (this) return _RoleDataEx.JieBiaoDayID; }
            set { lock (this) _RoleDataEx.JieBiaoDayID = value; }
        }

        /// <summary>
        /// 劫镖的日次数
        /// </summary>
        public int JieBiaoDayNum
        {
            get { lock (this) return _RoleDataEx.JieBiaoDayNum; }
            set { lock (this) _RoleDataEx.JieBiaoDayNum = value; }
        }

        /// <summary>
        /// 新邮件ID
        /// </summary>
        public int LastMailID
        {
            get { lock (this) return _RoleDataEx.LastMailID; }
            set { lock (this) _RoleDataEx.LastMailID = value; }
        }

        /// <summary>
        /// VIP日数据
        /// </summary>
        public List<VipDailyData> VipDailyDataList
        {
            get { lock (this) return _RoleDataEx.VipDailyDataList; }
            set { lock (this) _RoleDataEx.VipDailyDataList = value; }
        }

        /// <summary>
        /// VIP日数据
        /// </summary>
        public YangGongBKDailyJiFenData YangGongBKDailyJiFen
        {
            get { lock (this) return _RoleDataEx.YangGongBKDailyJiFen; }
            set { lock (this) _RoleDataEx.YangGongBKDailyJiFen = value; }
        }

        /// <summary>
        /// 单次奖励标志位
        /// </summary>
        public long OnceAwardFlag
        {
            get { lock (this) return _RoleDataEx.OnceAwardFlag; }
            set { lock (this) _RoleDataEx.OnceAwardFlag = value; }
        }

        /// <summary>
        ///  系统绑定的金币
        /// </summary>
        public int Gold
        {
            get { lock (this) return _RoleDataEx.Gold; }
            set { lock (this) _RoleDataEx.Gold = value; }
        }

        /// <summary>
        /// 已经使用的物品限制列表
        /// </summary>
        public List<GoodsLimitData> GoodsLimitDataList
        {
            get { lock (this) return _RoleDataEx.GoodsLimitDataList; }
            set { lock (this) _RoleDataEx.GoodsLimitDataList = value; }
        }

        /// <summary>
        /// 角色参数字典
        /// </summary>
        public Dictionary<string, RoleParamsData> RoleParamsDict
        {
            get { lock (this) return _RoleDataEx.RoleParamsDict; }
            set { lock (this) _RoleDataEx.RoleParamsDict = value; }
        }

        /// <summary>
        ///  永久禁言
        /// </summary>
        public int BanChat
        {
            get { lock (this) return _RoleDataEx.BanChat; }
            set { lock (this) _RoleDataEx.BanChat = value; }
        }

        /// <summary>
        ///  永久禁止登陆
        /// </summary>
        public int BanLogin
        {
            get { lock (this) return _RoleDataEx.BanLogin; }
            set { lock (this) _RoleDataEx.BanLogin = value; }
        }

        // MU项目增加字段 [11/30/2013 LiaoWei]
        /// <summary>
        ///  新人标记
        /// </summary>
        public int IsFlashPlayer
        {
            get { lock (this) return _RoleDataEx.IsFlashPlayer; }
            set { lock (this) _RoleDataEx.IsFlashPlayer = value; }
        }

        // MU项目增加字段 [11/30/2013 LiaoWei]
        /// <summary>
        ///  被崇拜次数
        /// </summary>
        public int AdmiredCount
        {
            get { lock (this) return _RoleDataEx.AdmiredCount; }
            set { lock (this) _RoleDataEx.AdmiredCount = value; }
        }

        // MU项目增加字段 [11/30/2013 LiaoWei]
        /// <summary>
        ///  崇拜次数
        /// </summary>
        private int _AdorationCount = 0;

        public int AdorationCount
        {
            get { lock (this) return _AdorationCount; }
            set { lock (this) _AdorationCount = value; }
        }

        /// <summary>
        /// PK之王崇拜次数
        /// </summary>
        private int _PKKingAdorationCount = 0;

        public int PKKingAdorationCount
        {
            get { lock (this) return _PKKingAdorationCount; }
            set { lock (this) _PKKingAdorationCount = value; }
        }

        /// <summary>
        /// 竞技场下次领奖时间
        /// </summary>
        private long _JingJiNextRewardTime = -1;
        public long JingJiNextRewardTime
        {
            get { lock (this) return _JingJiNextRewardTime; }
            set { lock (this) _JingJiNextRewardTime = value; }
        }

        /// <summary>
        /// PK之王崇拜日期
        /// </summary>

        private int _PKKingAdorationDayID = 0;

        public int PKKingAdorationDayID
        {
            get { lock (this) return _PKKingAdorationDayID; }
            set { lock (this) _PKKingAdorationDayID = value; }
        }

        // MU项目增加字段 [3/3/2014 LiaoWei]
        /// <summary>
        /// 自动分配属性点
        /// </summary>
        /// 
        public int AutoAssignPropertyPoint
        {
            get { lock (this) return _RoleDataEx.AutoAssignPropertyPoint; }
            set { lock (this) _RoleDataEx.AutoAssignPropertyPoint = value; }
        }

        /// <summary>
        /// 翅膀数据
        /// </summary>
        /// 
        public WingData MyWingData
        {
            get { lock (this) return _RoleDataEx.MyWingData; }
            set { lock (this) _RoleDataEx.MyWingData = value; }
        }

        /// <summary>
        ///  仓库金币
        /// </summary>
        public long StoreYinLiang
        {
            get { lock (this) return _RoleDataEx.Store_Yinliang; }
            set { lock (this) _RoleDataEx.Store_Yinliang = value; }
        }

        /// <summary>
        ///  仓库绑定金币
        /// </summary>
        public long StoreMoney
        {
            get { lock (this) return _RoleDataEx.Store_Money; }
            set { lock (this) _RoleDataEx.Store_Money = value; }
        }

        #endregion RoleDataEx 映射值

        #region 属性其他

        // 属性改造 begin[8/15/2013 LiaoWei]

        /// <summary>
        /// 所有属性点的读写锁,可能写入的地方,整个读写过程相关代码都要锁住
        /// </summary>
        public object PropPointMutex = new object();

        /// <summary>
        /// 总属性点
        /// </summary>
        private int _TotalPropPoint = 0;

        /// <summary>
        /// 总属性点
        /// </summary>
        public int TotalPropPoint
        {
            get { lock (this) return _TotalPropPoint; }
            set { lock (this) _TotalPropPoint = value; }
        }

        // 战斗力 [12/17/2013 LiaoWei]
        /// <summary>
        /// 战斗力数值
        /// </summary>
        public int CombatForce
        {
            get { lock (this) return _RoleDataEx.CombatForce; }
            set { lock (this) _RoleDataEx.CombatForce = value; }
        }

        /// <summary>
        /// 上次通知出去的战力值
        /// </summary>
        public int LastNotifyCombatForce = -1;

        /// <summary>
        /// 角色最大战斗力
        /// </summary>
        public long MaxCombatForce;

        #endregion 属性

        #region 增加一级属性

        // 注意: DB方面 尽量不对t_roles表做修改 一级属性还是放在t_roleparams 如果以后发现设计不合理 在做调整 LiaoWei

        /// <summary>
        /// 力量
        /// </summary>
        private int _PropStrength = 0;

        /// <summary>
        /// 力量
        /// </summary>
        public int PropStrength
        {
            get { lock (this) return _PropStrength; }
            set { lock (this) _PropStrength = value; }
        }

        /// <summary>
        /// 智力
        /// </summary>
        private int _PropIntelligence = 0;

        /// <summary>
        /// 智力
        /// </summary>
        public int PropIntelligence
        {
            get { lock (this) return _PropIntelligence; }
            set { lock (this) _PropIntelligence = value; }
        }

        /// <summary>
        /// 敏捷
        /// </summary>
        private int _PropDexterity = 0;

        /// <summary>
        /// 敏捷
        /// </summary>
        public int PropDexterity
        {
            get { lock (this) return _PropDexterity; }
            set { lock (this) _PropDexterity = value; }
        }

        /// <summary>
        /// 体力
        /// </summary>
        private int _PropConstitution = 0;

        /// <summary>
        /// 体力
        /// </summary>
        public int PropConstitution
        {
            get { lock (this) return _PropConstitution; }
            set { lock (this) _PropConstitution = value; }
        }

        #endregion 增加一级属性

        #region 转生相关

        // 转生相关 begin [10/10/2013 LiaoWei]

        /// <summary>
        /// 转生计数
        /// </summary>
        public int ChangeLifeCount
        {
            get
            {
                int tmpVar = _RoleDataEx.ChangeLifeCount;
                return tmpVar;
            }

            set
            {
                _RoleDataEx.ChangeLifeCount = value;
            }

            //get { lock (this) return _RoleDataEx.ChangeLifeCount; }
            //set { lock (this) _RoleDataEx.ChangeLifeCount = value; }
        }

        /// <summary>
        /// 转生属性值
        /// </summary>
        private ChangeLifeProp _RoleChangeLifeProp = new ChangeLifeProp();

        /// <summary>
        /// 转生属性值
        /// </summary>
        public ChangeLifeProp RoleChangeLifeProp
        {
            get { return _RoleChangeLifeProp; }
        }

        // 转生相关 end [10/10/2013 LiaoWei]

        #endregion 转生相关

        #region 推送相关

        /// <summary>
        /// 推送ID
        /// </summary>
        public string PushMessageID
        {
            get { lock (this) return _RoleDataEx.PushMessageID; }
            set { lock (this) _RoleDataEx.PushMessageID = value; }
        }

        #endregion 推送相关

        #region 额外添加值

        /// <summary>
        /// 汇报角色的坐标的客户端时间
        /// </summary>
        private long _ReportPosTicks = 0;

        /// <summary>
        /// 汇报角色的坐标的客户端时间
        /// </summary>
        public long ReportPosTicks
        {
            get { lock (this) return _ReportPosTicks; }
            set { lock (this) _ReportPosTicks = value; }
        }

        /// <summary>
        /// 记录角色的坐标的服务器端时间
        /// </summary>
        private long _ServerPosTicks = 0;

        /// <summary>
        /// 记录角色的坐标的服务器端时间
        /// </summary>
        public long ServerPosTicks
        {
            get { lock (this) return _ServerPosTicks; }
            set { lock (this) _ServerPosTicks = value; }
        }

        /// <summary>
        /// 当前正在做的动作
        /// </summary>
        private int _CurrentAction = 0;

        /// <summary>
        /// 当前正在做的动作
        /// </summary>
        public int CurrentAction
        {
            get { lock (this) return _CurrentAction; }
            set
            {
                lock (this)
                {
                    if (_CurrentAction != value)
                    {
                        _CurrentAction = value;

                        //如果是进入了打坐状态
                        //if ((int)GActions.Sit == _CurrentAction)
                        //{
                        //    _LastSiteExpTicks = TimeUtil.NOW(); //开始计时
                        //    _LastCheckGridTicks = _LastSiteExpTicks; //开始计时
                        //}
                    }
                }
            }
        }

        /// <summary>
        /// 移动的速度
        /// </summary>
        private double _MoveSpeed = 1.0;

        /// <summary>
        /// 移动的速度
        /// </summary>
        public double MoveSpeed
        {
            get { lock (this) return _MoveSpeed; }
            set { lock (this) _MoveSpeed = value; }
        }

        /// <summary>
        /// 移动的目的地坐标点
        /// </summary>
        private Point _DestPoint = new Point(-1, -1);

        /// <summary>
        /// 移动的目的地坐标点
        /// </summary>
        public Point DestPoint
        {
            get { lock (this) return _DestPoint; }
            set { lock (this) _DestPoint = value; }
        }

        private int _CurrentLifeV;

        /// <summary>
        /// 当前的生命值
        /// </summary>
        public int CurrentLifeV
        {
            get
            {
                int tmpCurrentLifeV = _CurrentLifeV;
                return tmpCurrentLifeV;

                // lock (this) return _CurrentLifeV; 
            }

            set
            {
                _CurrentLifeV = value;
                // lock (this) _CurrentLifeV = value;
            }
        }

        private int _CurrentMagicV;

        /// <summary>
        /// 当前的魔法值
        /// </summary>
        public int CurrentMagicV
        {
            get
            {
                int tmpCurrentMagicV = _CurrentMagicV;
                return tmpCurrentMagicV;
            }

            set
            {
                _CurrentMagicV = value;
            }

            //get { lock (this) return _CurrentMagicV; }
            //set { lock (this) _CurrentMagicV = value; }
        }

        private int _TeamID;

        /// <summary>
        /// 组队编号
        /// </summary>
        public int TeamID
        {
            get { lock (this) return _TeamID; }
            set { lock (this) _TeamID = value; }
        }

        private int _ExchangeID;

        /// <summary>
        /// 用户间交易ID
        /// </summary>
        public int ExchangeID
        {
            get { lock (this) return _ExchangeID; }
            set { lock (this) _ExchangeID = value; }
        }

        private long _ExchangeTicks;

        /// <summary>
        /// 用户间交易请求的时间
        /// </summary>
        public long ExchangeTicks
        {
            get { lock (this) return _ExchangeTicks; }
            set { lock (this) _ExchangeTicks = value; }
        }

        private int _LastMapCode = -1;

        /// <summary>
        /// 跳转地图前的地图的编号
        /// </summary>
        public int LastMapCode
        {
            get { lock (this) return _LastMapCode; }
            set { lock (this) _LastMapCode = value; }
        }

        private int _LastPosX = -1;

        /// <summary>
        /// 跳转地图前的所在的位置X坐标
        /// </summary>
        public int LastPosX
        {
            get { lock (this) return _LastPosX; }
            set { lock (this) _LastPosX = value; }
        }

        private int _LastPosY = -1;

        /// <summary>
        /// 跳转地图前的所在的位置Y坐标
        /// </summary>
        public int LastPosY
        {
            get { lock (this) return _LastPosY; }
            set { lock (this) _LastPosY = value; }
        }

        private StallData _StallDataItem = null;

        /// <summary>
        /// 摆摊的数据
        /// </summary>
        public StallData StallDataItem
        {
            get { lock (this) return _StallDataItem; }
            set { lock (this) _StallDataItem = value; }
        }

        private long _LastDBHeartTicks = TimeUtil.NOW(); //如果不赋予初始值，会导致在线时长计算为负数?

        /// <summary>
        /// 上次标记和DBserver心跳的时间(单位秒)
        /// </summary>
        public long LastDBHeartTicks
        {
            get { lock (this) return _LastDBHeartTicks; }
            set { lock (this) _LastDBHeartTicks = value; }
        }

        private long _LastSiteExpTicks = TimeUtil.NOW();

        /// <summary>
        /// 上次标记处理打坐收益时间(单位秒)
        /// </summary>
        public long LastSiteExpTicks
        {
            get { lock (this) return _LastSiteExpTicks; }
            set { lock (this) _LastSiteExpTicks = value; }
        }

        /// <summary>
        /// 上次标记处理消减PK值的时间(单位秒)
        /// </summary>
        private long _LastSiteSubPKPointTicks = 0;

        /// <summary>
        /// 上次标记处理消减PK值的时间(单位秒)
        /// </summary>
        public long LastSiteSubPKPointTicks
        {
            get { lock (this) return _LastSiteSubPKPointTicks; }
            set { lock (this) _LastSiteSubPKPointTicks = value; }
        }

        private bool _AutoFighting = false;

        /// <summary>
        /// 是否开始了自动战斗
        /// </summary>
        public bool AutoFighting
        {
            get { lock (this) return _AutoFighting; }
            set { lock (this) _AutoFighting = value; }
        }

        private long _LastAutoFightTicks = 0;

        /// <summary>
        /// 上次标记自动战斗的时间(单位秒)
        /// </summary>
        public long LastAutoFightTicks
        {
            get { lock (this) return _LastAutoFightTicks; }
            set { lock (this) _LastAutoFightTicks = value; }
        }

        /// <summary>
        /// 是否已经通知了自动战斗进入保护状态
        /// </summary>
        private int _AutoFightingProctect = 0;

        /// <summary>
        /// 是否已经通知了自动战斗进入保护状态
        /// </summary>
        public int AutoFightingProctect
        {
            get { lock (this) return _AutoFightingProctect; }
            set { lock (this) _AutoFightingProctect = value; }
        }

        private int _DJRoomID = -1;

        /// <summary>
        /// 创建的点将台房间ID
        /// </summary>
        public int DJRoomID
        {
            get { lock (this) return _DJRoomID; }
            set { lock (this) _DJRoomID = value; }
        }

        private int _DJRoomTeamID = -1;

        /// <summary>
        /// 创建的点将台房间中的队伍ID
        /// </summary>
        public int DJRoomTeamID
        {
            get { lock (this) return _DJRoomTeamID; }
            set { lock (this) _DJRoomTeamID = value; }
        }

        private bool _ViewDJRoomDlg = false;

        /// <summary>
        /// 是否打开了点将台房间界面
        /// </summary>
        public bool ViewDJRoomDlg
        {
            get { lock (this) return _ViewDJRoomDlg; }
            set { lock (this) _ViewDJRoomDlg = value; }
        }

        private int _CopyMapID = -1;

        /// <summary>
        /// 副本地图编号
        /// </summary>
        public int CopyMapID
        {
            get { lock (this) { return _CopyMapID; } }
            set { lock (this) _CopyMapID = value; }
        }

        /// <summary>
        /// 锁定的包裹
        /// </summary>
        private GoodsPackItem _GoodsPackItem = null;

        /// <summary>
        /// 锁定的包裹
        /// </summary>
        public GoodsPackItem LockedGoodsPackItem
        {
            get { lock (this) return _GoodsPackItem; }
            set { lock (this) _GoodsPackItem = value; }
        }

        /// <summary>
        /// 客户端选中的坐骑数据库ID
        /// </summary>
        private int _SelectHorseDbID = -1;

        /// <summary>
        /// 客户端选中的坐骑数据库ID
        /// </summary>
        public int SelectHorseDbID
        {
            get { lock (this) return _SelectHorseDbID; }
            set { lock (this) _SelectHorseDbID = value; }
        }

        /// <summary>
        /// 客户端放出的宠物的ID(场景ID)
        /// </summary>
        private int _PetRoleID = -1;

        /// <summary>
        /// 客户端放出的宠物的ID(场景ID)
        /// </summary>
        public int PetRoleID
        {
            get { lock (this) return _PetRoleID; }
            set { lock (this) _PetRoleID = value; }
        }

        /// <summary>
        /// 当前操作的移动仓库的列表
        /// </summary>
        public List<GoodsData> _PortableGoodsDataList = null;

        /// <summary>
        /// 当前操作的移动仓库的列表
        /// </summary>
        public List<GoodsData> PortableGoodsDataList
        {
            get { lock (this) return _PortableGoodsDataList; }
            set { lock (this) _PortableGoodsDataList = value; }
        }

        /// <summary>
        /// 当前操作的金蛋仓库的物品列表
        /// </summary>
        public List<GoodsData> _JinDanGoodsDataList = null;

        /// <summary>
        /// 当前操作的金蛋仓库的物品列表
        /// </summary>
        public List<GoodsData> JinDanGoodsDataList
        {
            get { lock (this) return _JinDanGoodsDataList; }
            set { lock (this) _JinDanGoodsDataList = value; }
        }

        /// <summary>
        /// 当前操作的时装仓库的物品列表  包括翅膀和称号 panghui add 
        /// </summary>
        public List<GoodsData> FashionGoodsDataList
        {
            get { lock (this) return _RoleDataEx.FashionGoodsDataList; }
            set { lock (this) _RoleDataEx.FashionGoodsDataList = value; }
        }

        /// <summary>
        /// 领地数据
        /// </summary>
        public List<BuildingData> BuildingDataList
        {
            get { lock (this) return _RoleDataEx.BuildingDataList; }
            set { lock (this) _RoleDataEx.BuildingDataList = value; }
        }

        /// <summary>
        /// 当前操作的精灵列表
        /// </summary>
        public List<GoodsData> DamonGoodsDataList
        {
            get { lock (this) return _RoleDataEx.DamonGoodsDataList; }
            set { lock (this) _RoleDataEx.DamonGoodsDataList = value; }
        }

        /// <summary>
        /// 当前操作的元素背包的物品列表
        /// </summary>
        //public List<GoodsData> _ElementhrtsList = null;

        /// <summary>
        /// 当前操作的元素装备栏的物品列表
        /// </summary>
        public List<GoodsData> ElementhrtsList
        {
            get { lock (this) return _RoleDataEx.ElementhrtsList; }
            set { lock (this) _RoleDataEx.ElementhrtsList = value; }
        }

        /// <summary>
        /// 当前操作的元素装备栏的物品列表
        /// </summary>
        //public List<GoodsData> _UsingElementhrtsList = null;

        /// <summary>
        /// 当前操作的元素装备栏的物品列表
        /// </summary>
        public List<GoodsData> UsingElementhrtsList
        {
            get { lock (this) return _RoleDataEx.UsingElementhrtsList; }
            set { lock (this) _RoleDataEx.UsingElementhrtsList = value; }
        }

        /// <summary>
        /// 当前操作的精灵背包的物品列表
        /// </summary>
        public List<GoodsData> PetList
        {
            get { lock (this) return _RoleDataEx.PetList; }
            set { lock (this) _RoleDataEx.PetList = value; }
        }

        /// <summary>
        /// 汇报宠物的坐标的客户端时间
        /// </summary>
        private long _ReportPetPosTicks = 0;

        /// <summary>
        /// 汇报宠物的坐标的客户端时间
        /// </summary>
        public long ReportPetPosTicks
        {
            get { lock (this) return _ReportPetPosTicks; }
            set { lock (this) _ReportPetPosTicks = value; }
        }

        /// <summary>
        /// 宠物的当前的X坐标
        /// </summary>
        private int _PetPosX = 0;

        /// <summary>
        /// 宠物的当前的X坐标
        /// </summary>
        public int PetPosX
        {
            get { lock (this) return _PetPosX; }
            set { lock (this) _PetPosX = value; }
        }

        /// <summary>
        /// 宠物的当前的Y坐标
        /// </summary>
        private int _PetPosY = 0;

        /// <summary>
        /// 宠物的当前的X坐标
        /// </summary>
        public int PetPosY
        {
            get { lock (this) return _PetPosY; }
            set { lock (this) _PetPosY = value; }
        }

        /// <summary>
        /// 装备属性
        /// </summary>
        private EquipPropItem _EquipProp = new EquipPropItem();

        /// <summary>
        /// 装备属性
        /// </summary>
        public EquipPropItem EquipProp
        {
            get { return _EquipProp; }
        }

        /// <summary>
        /// 新的属性缓存管理器
        /// </summary>
        public PropsCacheManager PropsCacheManager = new PropsCacheManager();

        /// <summary>
        /// 重量属性
        /// </summary>
        private WeighItems _WeighItems = new WeighItems();

        /// <summary>
        /// 重量属性
        /// </summary>
        public WeighItems WeighItems
        {
            get { return _WeighItems; }
        }

        /// <summary>
        /// 获取RoleDataEx 对象
        /// </summary>
        /// <returns></returns>
        public RoleDataEx GetRoleDataEx()
        {
            return _RoleDataEx;
        }

        /// <summary>
        /// 上次检测失效对象的格子X坐标
        /// </summary>
        public int _LastCheckGridX = -1;

        /// <summary>
        /// 上次检测失效对象的格子X坐标
        /// </summary>
        public int LastCheckGridX
        {
            get { lock (this) return _LastCheckGridX; }
            set { lock (this) _LastCheckGridX = value; }
        }

        /// <summary>
        /// 上次检测失效对象的格子Y坐标
        /// </summary>
        public int _LastCheckGridY = -1;

        /// <summary>
        /// 上次检测失效对象的格子Y坐标
        /// </summary>
        public int LastCheckGridY
        {
            get { lock (this) return _LastCheckGridY; }
            set { lock (this) _LastCheckGridY = value; }
        }

        /// <summary>
        /// 上次检测失效对象的时间
        /// </summary>
        //public long _LastCheckGridTicks = 0;

        /// <summary>
        /// 上次检测失效对象的时间
        /// </summary>
        //public long LastCheckGridTicks
        //{
        //    get { lock (this) return _LastCheckGridTicks; }
        //    set { lock (this) _LastCheckGridTicks = value; }
        //}

        /*
        /// <summary>
        /// 当前所在的地图九宫中的其他玩家和怪物的ID
        /// </summary>
        private Dictionary<string, bool> _CurrentObjsDict = null;

        /// <summary>
        /// 当前所在的地图九宫中的其他玩家和怪物的ID
        /// </summary>
        public Dictionary<string, bool> CurrentObjsDict
        {
            get { lock (this) return _CurrentObjsDict; }
            set { lock (this) _CurrentObjsDict = value; }
        }

        /// <summary>
        /// 当前所在的地图九宫中的格子字典
        /// </summary>
        private Dictionary<string, bool> _CurrentGridsDict = null;

        /// <summary>
        /// 当前所在的地图九宫中的格子字典
        /// </summary>
        public Dictionary<string, bool> CurrentGridsDict
        {
            get { lock (this) return _CurrentGridsDict; }
            set { lock (this) _CurrentGridsDict = value; }
        }
        */

        /// <summary>
        /// 大乱斗中杀死的敌人的数量
        /// </summary>
        private int _BattleKilledNum = 0;

        /// <summary>
        /// 炎黄战场中杀死的敌人的数量
        /// </summary>
        public int BattleKilledNum
        {
            get { lock (this) return _BattleKilledNum; }
            set { lock (this) _BattleKilledNum = value; }
        }

        /// <summary>
        /// 竞技场角斗赛中杀死的敌人的数量
        /// </summary>
        private int _ArenaBattleKilledNum = 0;

        /// <summary>
        /// 竞技场角斗赛中杀死的敌人的数量
        /// </summary>
        public int ArenaBattleKilledNum
        {
            get { lock (this) return _ArenaBattleKilledNum; }
            set { lock (this) _ArenaBattleKilledNum = value; }
        }

        /// <summary>
        /// 是否是隐身模式
        /// </summary>
        private int _HideSelf = 0;

        /// <summary>
        /// 是否是隐身模式
        /// </summary>
        public int HideSelf
        {
            get { lock (this) return _HideSelf; }
            set { lock (this) _HideSelf = value; }
        }

        /// <summary>
        /// 是否是GM隐身模式
        /// </summary>
        private int _HideGM = 0;

        public int HideGM
        {
            get { lock (this) return _HideGM; }
            set { lock (this) _HideGM = value; }
        }

        /// <summary>
        /// 上次判断是否安全区的时间
        /// </summary>
        public long _LastJugeSafeRegionTicks = 0;

        /// <summary>
        /// 上次判断是否安全区的时间
        /// </summary>
        public long LastJugeSafeRegionTicks
        {
            get { lock (this) return _LastJugeSafeRegionTicks; }
            set { lock (this) _LastJugeSafeRegionTicks = value; }
        }

        /// <summary>
        /// 是否在安全区的内
        /// </summary>
        //public bool _InSafeRegion = false;

        /// <summary>
        /// 是否在安全区内
        /// </summary>
        //public bool InSafeRegion
        //{
        //    get { lock (this) return _InSafeRegion; }
        //    set { lock (this) _InSafeRegion = value; }
        //}

        /// <summary>
        /// 防止沉迷的时间类型
        /// </summary>
        private int _AntiAddictionTimeType = 0;

        /// <summary>
        /// 防止沉迷的时间类型
        /// </summary>
        public int AntiAddictionTimeType
        {
            get { lock (this) return _AntiAddictionTimeType; }
            set { lock (this) _AntiAddictionTimeType = value; }
        }

        /// <summary>
        /// 计算出的经脉的添加属性
        /// </summary>
        private Dictionary<string, int> _JingMaiPropsDict = null;

        /// <summary>
        /// 计算出的经脉的添加属性
        /// </summary>
        public Dictionary<string, int> JingMaiPropsDict
        {
            get { lock (this) return _JingMaiPropsDict; }
            set { lock (this) _JingMaiPropsDict = value; }
        }

        /// <summary>
        ///  当前冲脉的重数
        /// </summary>
        private int _JingMaiBodyLevel = 1;

        /// <summary>
        ///  当前冲脉的重数
        /// </summary>
        public int JingMaiBodyLevel
        {
            get { lock (this) return _JingMaiBodyLevel; }
            set { lock (this) _JingMaiBodyLevel = value; }
        }

        /// <summary>
        ///  是否是第一次登录后地图
        /// </summary>
        private bool _FirstPlayStart = true;

        /// <summary>
        ///  是否是第一次登录后地图
        /// </summary>
        public bool FirstPlayStart
        {
            get { lock (this) return _FirstPlayStart; }
            set { lock (this) _FirstPlayStart = value; }
        }

        /// <summary>
        /// 上次标记处理数据库中的Buffer项的时间(单位秒)
        /// </summary>
        private long _LastProcessBufferTicks = 0;

        /// <summary>
        /// 上次标记处理数据库中的Buffer项的时间(单位秒)
        /// </summary>
        public long LastProcessBufferTicks
        {
            get { lock (this) return _LastProcessBufferTicks; }
            set { lock (this) _LastProcessBufferTicks = value; }
        }

        /// <summary>
        /// 提升生命上线的数据库buffer项
        /// </summary>
        private BufferData _UpLifeLimitBufferData = null;

        /// <summary>
        /// 提升生命上线的数据库buffer项
        /// </summary>
        public BufferData UpLifeLimitBufferData
        {
            get { lock (this) return _UpLifeLimitBufferData; }
            set { lock (this) _UpLifeLimitBufferData = value; }
        }

        /// <summary>
        /// 提升攻击力的数据库buffer项
        /// </summary>
        private BufferData _AddTempAttackBufferData = null;

        /// <summary>
        /// 提升攻击力的数据库buffer项
        /// </summary>
        public BufferData AddTempAttackBufferData
        {
            get { lock (this) return _AddTempAttackBufferData; }
            set { lock (this) _AddTempAttackBufferData = value; }
        }

        /// <summary>
        /// 提升防御力的数据库buffer项
        /// </summary>
        private BufferData _AddTempDefenseBufferData = null;

        /// <summary>
        /// 提升防御力的数据库buffer项
        /// </summary>
        public BufferData AddTempDefenseBufferData
        {
            get { lock (this) return _AddTempDefenseBufferData; }
            set { lock (this) _AddTempDefenseBufferData = value; }
        }

        /// <summary>
        /// BOSS克星的数据库buffer项
        /// </summary>
        private BufferData _AntiBossBufferData = null;

        /// <summary>
        /// BOSS克星的数据库buffer项
        /// </summary>
        public BufferData AntiBossBufferData
        {
            get { lock (this) return _AntiBossBufferData; }
            set { lock (this) _AntiBossBufferData = value; }
        }

        /// <summary>
        /// 皇帝的舍利之源临时buffer项
        /// </summary>
        private BufferData _SheLiZhiYuanBufferData = null;

        /// <summary>
        /// 皇帝的舍利之源临时buffer项
        /// </summary>
        public BufferData SheLiZhiYuanBufferData
        {
            get { lock (this) return _SheLiZhiYuanBufferData; }
            set { lock (this) _SheLiZhiYuanBufferData = value; }
        }

        /// <summary>
        /// 皇后的帝王之佑临时buffer项
        /// </summary>
        private BufferData _DiWangZhiYouBufferData = null;

        /// <summary>
        /// 皇后的帝王之佑临时buffer项
        /// </summary>
        public BufferData DiWangZhiYouBufferData
        {
            get { lock (this) return _DiWangZhiYouBufferData; }
            set { lock (this) _DiWangZhiYouBufferData = value; }
        }

        /// <summary>
        /// 帮旗加成Buffer项
        /// </summary>
        private BufferData _JunQiBufferData = null;

        /// <summary>
        /// 帮旗加成Buffer项
        /// </summary>
        public BufferData JunQiBufferData
        {
            get { lock (this) return _JunQiBufferData; }
            set { lock (this) _JunQiBufferData = value; }
        }

        /// <summary>
        /// 临时冲穴成功率倍数
        /// </summary>
        private int _TempJMChongXueRate = 1;

        /// <summary>
        /// 临时冲穴成功率倍数
        /// </summary>
        public int TempJMChongXueRate
        {
            get { lock (this) return _TempJMChongXueRate; }
            set { lock (this) _TempJMChongXueRate = value; }
        }

        /// <summary>
        /// 临时坐骑强化成功率倍数
        /// </summary>
        private int _TempHorseEnchanceRate = 1;

        /// <summary>
        /// 临时坐骑强化成功率倍数
        /// </summary>
        public int TempHorseEnchanceRate
        {
            get { lock (this) return _TempHorseEnchanceRate; }
            set { lock (this) _TempHorseEnchanceRate = value; }
        }

        /// <summary>
        /// 临时坐骑进阶成功率倍数
        /// </summary>
        private int _TempHorseUpLevelRate = 1;

        /// <summary>
        /// 临时坐骑进阶成功率倍数
        /// </summary>
        public int TempHorseUpLevelRate
        {
            get { lock (this) return _TempHorseUpLevelRate; }
            set { lock (this) _TempHorseUpLevelRate = value; }
        }

        /// <summary>
        /// 自动挂机时拾取的设置
        /// </summary>
        private int _AutoFightGetThings = 0;

        /// <summary>
        /// 自动挂机时拾取的设置
        /// </summary>
        public int AutoFightGetThings
        {
            get { lock (this) return _AutoFightGetThings; }
            set { lock (this) _AutoFightGetThings = value; }
        }

        /// <summary>
        /// 更新某个指令的时间
        /// </summary>
        private Dictionary<int, long> _LastDBCmdTicksDict = new Dictionary<int, long>();

        /// <summary>
        /// 更新某个指令的时间
        /// </summary>
        public Dictionary<int, long> LastDBCmdTicksDict
        {
            get { lock (this) return _LastDBCmdTicksDict; }
            set { lock (this) _LastDBCmdTicksDict = value; }
        }

        /// <summary>
        /// 标记客户端心跳的次数
        /// </summary>
        private int _ClientHeartCount = 0;

        /// <summary>
        /// 标记客户端心跳的次数
        /// </summary>
        public int ClientHeartCount
        {
            get { lock (this) return _ClientHeartCount; }
            set { lock (this) _ClientHeartCount = value; }
        }

        /// <summary>
        /// 标记客户端心跳的时间
        /// </summary>
        private long _LastClientHeartTicks = TimeUtil.NOW();

        /// <summary>
        /// 标记客户端心跳的时间
        /// </summary>
        public long LastClientHeartTicks
        {
            get { lock (this) return _LastClientHeartTicks; }
            set { lock (this) _LastClientHeartTicks = value; }
        }

        //用于防止宇宙终极加速的变量
        private long _LastClientServerSubTicks = 0L;

        ///用于防止宇宙终极加速的变量
        public long LastClientServerSubTicks
        {
            get { lock (this) return _LastClientServerSubTicks; }
            set { lock (this) _LastClientServerSubTicks = value; }
        }

        /// <summary>
        /// 用于防止宇宙加速的个数
        /// </summary>
        private int _LastClientServerSubNum = 0;

        public int LastClientServerSubNum
        {
            get { lock (this) return _LastClientServerSubNum; }
            set { lock (this) _LastClientServerSubNum = value; }
        }

        /// <summary>
        /// 关闭无心跳的客户端的步骤
        /// </summary>
        private int _ClosingClientStep = 0;

        /// <summary>
        /// 关闭无心跳的客户端的步骤
        /// </summary>
        public int ClosingClientStep
        {
            get { lock (this) return _ClosingClientStep; }
            set { lock (this) _ClosingClientStep = value; }
        }

        /// <summary>
        /// 增加熟练度的被动技能
        /// </summary>
        private SkillData _NumSkillData = null;

        /// <summary>
        /// 增加熟练度的被动技能
        /// </summary>
        public SkillData NumSkillData
        {
            get { lock (this) return _NumSkillData; }
            set { lock (this) _NumSkillData = value; }
        }


        /// <summary>
        /// 默认技能的等级
        /// </summary>
        private int _DefaultSkillLev = 1;

        /// <summary>
        /// 默认技能的等级
        /// </summary>
        public int DefaultSkillLev
        {
            get { lock (this) return _DefaultSkillLev; }
            set { lock (this) _DefaultSkillLev = value; }
        }

        /// <summary>
        /// 默认技能的熟练度
        /// </summary>
        private int _DefaultSkillUseNum = 0;

        /// <summary>
        /// 默认技能的熟练度
        /// </summary>
        public int DefaultSkillUseNum
        {
            get { lock (this) return _DefaultSkillUseNum; }
            set { lock (this) _DefaultSkillUseNum = value; }
        }

        /// <summary>
        /// 更新某个技能命令的时间
        /// </summary>
        private Dictionary<int, long> _LastDBSkillCmdTicksDict = new Dictionary<int, long>();

        /// <summary>
        /// 更新某个技能命令的时间
        /// </summary>
        public Dictionary<int, long> LastDBSkillCmdTicksDict
        {
            get { lock (this) return _LastDBSkillCmdTicksDict; }
            set { lock (this) _LastDBSkillCmdTicksDict = value; }
        }

        /// <summary>
        /// 挖宝挖到的物品数据
        /// </summary>
        private GoodsData _WaBaoGoodsData = null;

        /// <summary>
        /// 挖宝挖到的物品数据
        /// </summary>
        public GoodsData WaBaoGoodsData
        {
            get { lock (this) return _WaBaoGoodsData; }
            set { lock (this) _WaBaoGoodsData = value; }
        }

        /// <summary>
        /// 充值的元宝的修改锁
        /// </summary>
        private object _UserMoneyMutex = new object();

        /// <summary>
        /// 充值的元宝的修改锁
        /// </summary>
        public object UserMoneyMutex
        {
            get { lock (this) return _UserMoneyMutex; }
        }

        /// <summary>
        /// 银两的修改锁
        /// </summary>
        private object _YinLiangMutex = new object();

        /// <summary>
        /// 银两的修改锁
        /// </summary>
        public object YinLiangMutex
        {
            get { lock (this) return _YinLiangMutex; }
        }

        /// <summary>
        /// 金币的修改锁
        /// </summary>
        private object _GoldMutex = new object();

        /// <summary>
        /// 金币的修改锁
        /// </summary>
        public object GoldMutex
        {
            get { lock (this) return _GoldMutex; }
        }

        /// <summary>
        /// 副本顺序ID
        /// </summary>
        private int _FuBenSeqID = -1;

        /// <summary>
        /// 副本顺序ID
        /// </summary>
        public int FuBenSeqID
        {
            get { lock (this) return _FuBenSeqID; }
            set { lock (this) _FuBenSeqID = value; }
        }

        /// <summary>
        /// 副本ID [7/7/2014 LiaoWei]
        /// </summary>
        private int _FuBenID = -1;

        /// <summary>
        /// 副本ID [7/7/2014 LiaoWei]
        /// </summary>
        public int FuBenID
        {
            get { lock (this) return _FuBenID; }
            set { lock (this) _FuBenID = value; }
        }

        /// <summary>
        /// 藏宝秘境移动剩余步数 大于0正向 小于0反向
        /// </summary>
        public int OnePieceMoveLeft = 0;

        /// <summary>
        /// 藏宝秘境移动方向 大于0正向 小于0反向
        /// 处理OnePieceMoveLeft == 0跨层移动时的特殊情况
        /// </summary>
        public int OnePieceMoveDir = 0;

        /// <summary>
        /// 藏宝秘境临时缓存事件ID 为了战斗事件ETET_Combat
        /// </summary>
        public int OnePieceTempEventID = 0;

        /// <summary>
        /// 藏宝秘境缓存宝箱数据
        /// </summary>
        public List<int> _OnePieceBoxIDList = null;

        /// <summary>
        /// 藏宝秘境缓存宝箱数据
        /// </summary>
        public List<int> OnePieceBoxIDList
        {
            get { lock (this) return _OnePieceBoxIDList; }
            set { lock (this) _OnePieceBoxIDList = value; }
        }

        /// <summary>
        /// 是否禁止紫名/灰名(2015-2-11开始总是禁止,如果需要,可以按地图放开限制)
        /// </summary>
        public bool DisableChangeRolePurpleName = true;

        /// <summary>
        /// 紫名开始的时间
        /// </summary>
        private long _StartPurpleNameTicks = 0;

        /// <summary>
        /// 紫名开始的时间
        /// </summary>
        public long StartPurpleNameTicks
        {
            get { lock (this) return _StartPurpleNameTicks; }
            set { lock (this) _StartPurpleNameTicks = value; }
        }

        /// <summary>
        /// 临时的连斩开始的时间
        /// </summary>
        private long _StartLianZhanTicks = 0;

        /// <summary>
        /// 临时的连斩开始的时间
        /// </summary>
        public long StartLianZhanTicks
        {
            get { lock (this) return _StartLianZhanTicks; }
            set { lock (this) _StartLianZhanTicks = value; }
        }

        /// <summary>
        /// 临时的连斩等待的时间
        /// </summary>
        private long _WaitingLianZhanTicks = 0;

        /// <summary>
        /// 临时的连斩开始的时间
        /// </summary>
        public long WaitingLianZhanTicks
        {
            get { lock (this) return _WaitingLianZhanTicks; }
            set { lock (this) _WaitingLianZhanTicks = value; }
        }

        /// <summary>
        /// 临时的连斩值
        /// </summary>
        private int _TempLianZhan = 0;

        /// <summary>
        /// 临时的连斩值
        /// </summary>
        public int TempLianZhan
        {
            get { lock (this) return _TempLianZhan; }
            set { lock (this) _TempLianZhan = value; }
        }

        /// <summary>
        /// 当前技能总的升级的个数
        /// </summary>
        private int _TotalLearnedSkillLevelCount = 0;

        /// <summary>
        /// 当前技能总的升级的个数
        /// </summary>
        public int TotalLearnedSkillLevelCount
        {
            get { lock (this) return _TotalLearnedSkillLevelCount; }
            set { lock (this) _TotalLearnedSkillLevelCount = value; }
        }

        /// <summary>
        /// 上次标记处理地图时间限制的时间(单位秒)
        /// </summary>
        private long _LastProcessMapLimitTimesTicks = TimeUtil.NOW();

        /// <summary>
        /// 上次标记处理地图时间限制的时间(单位秒)
        /// </summary>
        public long LastProcessMapLimitTimesTicks
        {
            get { lock (this) return _LastProcessMapLimitTimesTicks; }
            set { lock (this) _LastProcessMapLimitTimesTicks = value; }
        }

        /// <summary>
        /// 装备评分
        /// </summary>
        private int _RoleEquipJiFen = 0;

        /// <summary>
        /// 装备评分
        /// </summary>
        public int RoleEquipJiFen
        {
            get { lock (this) return _RoleEquipJiFen; }
            set { lock (this) _RoleEquipJiFen = value; }
        }

        /// <summary>
        /// 经脉穴位已冲个数
        /// </summary>
        private int _RoleXueWeiNum = 0;

        /// <summary>
        /// 经脉穴位已冲个数
        /// </summary>
        public int RoleXueWeiNum
        {
            get { lock (this) return _RoleXueWeiNum; }
            set { lock (this) _RoleXueWeiNum = value; }
        }

        /// <summary>
        /// 坐骑的积分值
        /// </summary>
        private int _RoleHorseJiFen = 0;

        /// <summary>
        /// 坐骑的积分值
        /// </summary>
        public int RoleHorseJiFen
        {
            get { lock (this) return _RoleHorseJiFen; }
            set { lock (this) _RoleHorseJiFen = value; }
        }

        //排队的命令队列项
        private List<QueueCmdItem> _QueueCmdItemList = new List<QueueCmdItem>();

        //排队的命令队列项
        public List<QueueCmdItem> QueueCmdItemList
        {
            get { lock (this) return _QueueCmdItemList; }
        }

        /// <summary>
        /// 冲脉的失败次数
        /// </summary>
        private int _ChongXueFailedNum = 0;

        /// <summary>
        /// 冲脉的失败次数
        /// </summary>
        public int ChongXueFailedNum
        {
            get { lock (this) return _ChongXueFailedNum; }
            set { lock (this) _ChongXueFailedNum = value; }
        }

        /// <summary>
        /// 临时坐骑形象开始的时间
        /// </summary>
        private long _StartTempHorseIDTicks = 0;

        /// <summary>
        /// 临时坐骑形象开始的时间
        /// </summary>
        public long StartTempHorseIDTicks
        {
            get { lock (this) return _StartTempHorseIDTicks; }
            set { lock (this) _StartTempHorseIDTicks = value; }
        }

        /// <summary>
        /// 临时坐骑形象ID
        /// </summary>
        private int _TempHorseID = 0;

        /// <summary>
        /// 临时坐骑形象开始的时间
        /// </summary>
        public int TempHorseID
        {
            get { lock (this) return _TempHorseID; }
            set { lock (this) _TempHorseID = value; }
        }

        /// <summary>
        /// 计算登录次数的日ID
        /// </summary>
        private int _LoginDayID = TimeUtil.NowDateTime().DayOfYear;

        /// <summary>
        /// 计算登录次数的日ID
        /// </summary>
        public int LoginDayID
        {
            get { lock (this) return _LoginDayID; }
            set { lock (this) _LoginDayID = value; }
        }

        /// <summary>
        /// 全套品质的级别
        /// </summary>
        private int _AllQualityIndex = 0;

        /// <summary>
        /// 全套品质的级别
        /// </summary>
        public int AllQualityIndex
        {
            get { lock (this) return _AllQualityIndex; }
            set { lock (this) _AllQualityIndex = value; }
        }

        /// <summary>
        /// 全套锻造级别
        /// </summary>
        private int _AllForgeLevelIndex = 0;

        /// <summary>
        /// 全套锻造级别
        /// </summary>
        public int AllForgeLevelIndex
        {
            get { lock (this) return _AllForgeLevelIndex; }
            set { lock (this) _AllForgeLevelIndex = value; }
        }

        /// <summary>
        /// 全套宝石级别
        /// </summary>
        private int _AllJewelLevelIndex = 0;

        /// <summary>
        /// 全套宝石级别
        /// </summary>
        public int AllJewelLevelIndex
        {
            get { lock (this) return _AllJewelLevelIndex; }
            set { lock (this) _AllJewelLevelIndex = value; }
        }

        /// <summary>
        /// 全套卓越属性个数
        /// </summary>
        private int _AllZhuoYueNum = 0;

        /// <summary>
        /// 全套卓越属性个数
        /// </summary>
        public int AllZhuoYueNum
        {
            get { lock (this) return _AllZhuoYueNum; }
            set { lock (this) _AllZhuoYueNum = value; }
        }

        /// <summary>
        /// 全套属性加成管理项
        /// </summary>
        private AllThingsCalcItem _AllThingsCalcItem = new AllThingsCalcItem();

        /// <summary>
        /// 全套属性加成管理项
        /// </summary>
        public AllThingsCalcItem MyAllThingsCalcItem
        {
            get { lock (this) return _AllThingsCalcItem; }
            set { lock (this) _AllThingsCalcItem = value; }
        }

        /// <summary>
        /// 杨公宝库项
        /// </summary>
        public YangGongBKItem _MyYangGongBKItem = null;

        /// <summary>
        /// 杨公宝库项
        /// </summary>
        public YangGongBKItem MyYangGongBKItem
        {
            get { lock (this) return _MyYangGongBKItem; }
            set { lock (this) _MyYangGongBKItem = value; }
        }

        /// <summary>
        /// 奇珍阁物品字典
        /// </summary>
        public Dictionary<int, QiZhenGeItemData> _QiZhenGeGoodsDict = null;

        /// <summary>
        /// 奇珍阁物品字典
        /// </summary>
        public Dictionary<int, QiZhenGeItemData> QiZhenGeGoodsDict
        {
            get { lock (this) return _QiZhenGeGoodsDict; }
            set { lock (this) _QiZhenGeGoodsDict = value; }
        }

        /// <summary>
        /// 奇珍阁物品购买次数
        /// </summary>
        private int _QiZhenGeBuyNum = 0;

        /// <summary>
        /// 奇珍阁物品字典
        /// </summary>
        public int QiZhenGeBuyNum
        {
            get { lock (this) return _QiZhenGeBuyNum; }
            set { lock (this) _QiZhenGeBuyNum = value; }
        }

        /// <summary>
        /// 进入地图的时间
        /// </summary>
        private long _EnterMapTicks = 0;

        /// <summary>
        /// 进入地图的时间
        /// </summary>
        public long EnterMapTicks
        {
            get { lock (this) return _EnterMapTicks; }
            set { lock (this) _EnterMapTicks = value; }
        }

        /// <summary>
        /// 登录期间消费的元宝
        /// </summary>
        private int _TotalUsedMoney = 0;

        /// <summary>
        /// 登录期间消费的元宝
        /// </summary>
        public int TotalUsedMoney
        {
            get { lock (this) return _TotalUsedMoney; }
            set { lock (this) _TotalUsedMoney = value; }
        }

        /// <summary>
        /// 登录期间获取的商城道具的元宝价值
        /// </summary>
        private int _TotalGoodsMoney = 0;

        /// <summary>
        /// 登录期间获取的商城道具的元宝价值
        /// </summary>
        public int TotalGoodsMoney
        {
            get { lock (this) return _TotalGoodsMoney; }
            set { lock (this) _TotalGoodsMoney = value; }
        }

        /// <summary>
        /// 是否对物品获取进行了报警价值
        /// </summary>
        private int _ReportWarningGoodsMoney = 0;

        /// <summary>
        /// 是否对物品获取进行了报警价值
        /// </summary>
        public int ReportWarningGoodsMoney
        {
            get { lock (this) return _ReportWarningGoodsMoney; }
            set { lock (this) _ReportWarningGoodsMoney = value; }
        }

        /// <summary>
        /// 上次攻击的时间
        /// </summary>
        private long _LastAttackTicks = 0;

        /// <summary>
        /// 上次攻击的时间
        /// </summary>
        public long LastAttackTicks
        {
            get { lock (this) return _LastAttackTicks; }
            set { lock (this) _LastAttackTicks = value; }
        }

        /// <summary>
        /// 是否强制断开网络
        /// </summary>
        private bool _ForceShenFenZheng = false;

        /// <summary>
        /// 是否强制断开网络
        /// </summary>
        public bool ForceShenFenZheng
        {
            get { lock (this) return _ForceShenFenZheng; }
            set { lock (this) _ForceShenFenZheng = value; }
        }

        /// <summary>
        /// 法师的护盾开始的时间
        /// </summary>
        private long _FSHuDunStart = 0;

        /// <summary>
        /// 法师的护盾开始的时间
        /// </summary>
        public long FSHuDunStart
        {
            get { lock (this) return _FSHuDunStart; }
            set { lock (this) _FSHuDunStart = value; }
        }

        /// <summary>
        /// 法师的护盾持续时间
        /// </summary>
        private int _FSHuDunSeconds = 0;

        /// <summary>
        /// 法师的护盾持续时间
        /// </summary>
        public int FSHuDunSeconds
        {
            get { lock (this) return _FSHuDunSeconds; }
            set { lock (this) _FSHuDunSeconds = value; }
        }

        /// <summary>
        /// 道术隐身的时间
        /// </summary>
        private long _DSHideStart = 0;

        /// <summary>
        /// 道术隐身的时间
        /// </summary>
        public long DSHideStart
        {
            get { lock (this) return _DSHideStart; }
            set { lock (this) _DSHideStart = value; }
        }

        /// <summary>
        /// 服务器端通知切换地图时的状态
        /// </summary>
        private bool _WaitingNotifyChangeMap = false;

        /// <summary>
        /// 服务器端通知切换地图时的状态
        /// </summary>
        public bool WaitingNotifyChangeMap
        {
            get { lock (this) return _WaitingNotifyChangeMap; }
            set { lock (this) _WaitingNotifyChangeMap = value; }
        }

        public int WaitingChangeMapToMapCode;
        public int WaitingChangeMapToPosX;
        public int WaitingChangeMapToPosY;

        public int KuaFuChangeMapCode;

        /// <summary>
        /// 大乱斗中的阵营ID
        /// </summary>
        private int _BattleWhichSide = 0;

        /// <summary>
        /// 大乱斗中的阵营ID 0仙 1魔
        /// </summary>
        public int BattleWhichSide
        {
            get { lock (this) return _BattleWhichSide; }
            set { lock (this) _BattleWhichSide = value; }
        }

        /// <summary>
        /// 本次在线时长，单位秒
        /// </summary>
        private int _ThisTimeOnlineSecs = 0;

        /// <summary>
        /// 本次在线时长，单位秒
        /// </summary>
        public int ThisTimeOnlineSecs
        {
            get { lock (this) return _ThisTimeOnlineSecs; }
            set { lock (this) _ThisTimeOnlineSecs = value; }
        }

        //技能CD控制
        private MagicCoolDownMgr _MagicCoolDownMgr = new MagicCoolDownMgr();

        /// <summary>
        /// 技能CD控制
        /// </summary>
        public MagicCoolDownMgr MyMagicCoolDownMgr
        {
            get { lock (this) return _MagicCoolDownMgr; }
        }

        //物品CD控制
        private GoodsCoolDownMgr _GoodsCoolDownMgr = new GoodsCoolDownMgr();

        /// <summary>
        /// 技能CD控制
        /// </summary>
        public GoodsCoolDownMgr MyGoodsCoolDownMgr
        {
            get { lock (this) return _GoodsCoolDownMgr; }
        }

        /// <summary>
        /// 邮件发送验证码
        /// </summary>
        private string _MailSendSecurityCode = "";

        /// <summary>
        /// 邮件发送验证码
        /// </summary>
        public string MailSendSecurityCode
        {
            get { lock (this) return _MailSendSecurityCode; }
            set { lock (this) _MailSendSecurityCode = value; }
        }

        //开始移动的时间
        private long _RoleStartMoveTicks = 0;

        /// <summary>
        /// 开始移动的时间
        /// </summary>
        public long RoleStartMoveTicks
        {
            get { lock (this) return _RoleStartMoveTicks; }
            set { lock (this) _RoleStartMoveTicks = value; }
        }

        /// <summary>
        /// 角色当前路径信息字符串
        /// </summary>
        private string _RolePathString = "";

        /// <summary>
        /// 角色当前路径信息字符串
        /// </summary>
        public string RolePathString
        {
            get { lock (this) return _RolePathString; }
            set { lock (this) _RolePathString = value; }
        }

        /// <summary>
        /// 腾讯版本的防止沉迷的数值
        /// </summary>
        private double _TengXunFCMRate = 1.0;

        /// <summary>
        /// 腾讯版本的防止沉迷的数值
        /// </summary>
        public double TengXunFCMRate
        {
            get { lock (this) return _TengXunFCMRate; }
            set { lock (this) _TengXunFCMRate = value; }
        }

        /// <summary>
        /// 更新某个角色参数命令的时间
        /// </summary>
        private Dictionary<string, long> _LastDBRoleParamCmdTicksDict = new Dictionary<string, long>();

        /// <summary>
        /// 更新某个角色参数命令的时间
        /// </summary>
        public Dictionary<string, long> LastDBRoleParamCmdTicksDict
        {
            get { lock (this) return _LastDBRoleParamCmdTicksDict; }
            set { lock (this) _LastDBRoleParamCmdTicksDict = value; }
        }

        /// <summary>
        /// 更新某个装备耐久度命令的时间
        /// </summary>
        private Dictionary<int, long> _LastDBEquipStrongCmdTicksDict = new Dictionary<int, long>();

        /// <summary>
        /// 更新某个装备耐久度命令的时间
        /// </summary>
        public Dictionary<int, long> LastDBEquipStrongCmdTicksDict
        {
            get { lock (this) return _LastDBEquipStrongCmdTicksDict; }
            set { lock (this) _LastDBEquipStrongCmdTicksDict = value; }
        }

        /// <summary>
        /// 玩家总杀怪数量
        /// </summary>
        private uint _TotalKilledMonsterNum = 0;

        /// <summary>
        /// 玩家总杀怪数量
        /// </summary>
        public uint TotalKilledMonsterNum
        {
            get { lock (this) return _TotalKilledMonsterNum; }
            set { lock (this) _TotalKilledMonsterNum = value; }
        }

        /// <summary>
        /// 玩家杀怪数据每次更新后增加杀怪数量 用于不定时更新判断
        /// </summary>
        private ushort _TimerKilledMonsterNum = 0;

        /// <summary>
        /// 玩家本次登录增加杀怪数量
        /// </summary>
        public ushort TimerKilledMonsterNum
        {
            get { lock (this) return _TimerKilledMonsterNum; }
            set { lock (this) _TimerKilledMonsterNum = value; }
        }

        /// <summary>
        /// 下一个杀死怪物成就的数量，这样能在每个怪物被杀死的时进行更快速的判断
        /// </summary>
        private uint _NextKillMonsterChengJiuNum = 0;

        /// <summary>
        /// 下一个杀死怪物成就的数量，这样能在每个怪物被杀死的时进行更快速的判断
        /// </summary>
        public uint NextKilledMonsterChengJiuNum
        {
            get { lock (this) return _NextKillMonsterChengJiuNum; }
            set { lock (this) _NextKillMonsterChengJiuNum = value; }
        }

        /// <summary>
        /// 玩家铜钱的最大值
        /// </summary>
        private int _MaxTongQianNum = -1;

        /// <summary>
        /// 玩家铜钱的最大值
        /// </summary>
        public int MaxTongQianNum
        {
            get { lock (this) return _MaxTongQianNum; }
            set { lock (this) _MaxTongQianNum = value; }
        }

        /// <summary>
        /// 下一个铜钱成就的数量，有效减少成就判断次数
        /// </summary>
        private uint _NextTongQianChengJiuNum = 0;

        /// <summary>
        /// 下一个铜钱成就的数量，有效减少成就判断次数
        /// </summary>
        public uint NextTongQianChengJiuNum
        {
            get { lock (this) return _NextTongQianChengJiuNum; }
            set { lock (this) _NextTongQianChengJiuNum = value; }
        }

        /// <summary>
        /// 总的日登陆数量
        /// </summary>
        private uint _TotalDayLoginNum = 0;

        /// <summary>
        /// 总的日登陆数量
        /// </summary>
        public uint TotalDayLoginNum
        {
            get { lock (this) return _TotalDayLoginNum; }
            set { lock (this) _TotalDayLoginNum = value; }
        }

        /// <summary>
        /// 连续日登陆数量
        /// </summary>
        private uint _ContinuousDayLoginNum = 0;

        /// <summary>
        /// 连续日登陆数量
        /// </summary>
        public uint ContinuousDayLoginNum
        {
            get { lock (this) return _ContinuousDayLoginNum; }
            set { lock (this) _ContinuousDayLoginNum = value; }
        }

        /// <summary>
        /// 玩家成就点数
        /// </summary>
        private int _ChengJiuPoints = 0;

        /// <summary>
        /// 玩家成就点数
        /// </summary>
        public int ChengJiuPoints
        {
            get { lock (this) return _ChengJiuPoints; }
            set { lock (this) _ChengJiuPoints = value; }
        }

        /// <summary>
        /// 玩家成就等级
        /// </summary>
        private int _ChengJiuLevel = 0;

        /// <summary>
        /// 玩家成就等级
        /// </summary>
        public int ChengJiuLevel
        {
            get { lock (this) return _ChengJiuLevel; }
            set { lock (this) _ChengJiuLevel = value; }
        }

        /// <summary>
        /// 可挑战的万魔塔层编号
        /// </summary>
        private int _WanMoTaNextLayerOrder = 0;

        /// <summary>
        /// 可挑战的万魔塔层编号
        /// </summary>
        public int WanMoTaNextLayerOrder
        {
            get { lock (this) return _WanMoTaNextLayerOrder; }
            set { lock (this) _WanMoTaNextLayerOrder = value; }
        }

        /// <summary>
        /// 玩家野蛮冲撞时的状态
        /// </summary>
        //private int _YeManChongZhuang = 0;

        /// <summary>
        /// 玩家野蛮冲撞时的状态
        /// </summary>
        //public int YeManChongZhuang
        //{
        //    get { lock (this) return _YeManChongZhuang; }
        //    set { lock (this) _YeManChongZhuang = value; }
        //}

        /// <summary>
        /// 自己召唤的且为自己战斗或者帮助自己的怪物列表
        /// </summary>
        public List<Monster> _SummonMonstersList = new List<Monster>();

        /// <summary>
        /// 自己召唤的且为自己战斗或者帮助自己的怪物列表
        /// </summary>
        public List<Monster> SummonMonstersList
        {
            get { lock (this) return _SummonMonstersList; }
            set { lock (this) _SummonMonstersList = value; }
        }

        /// <summary>
        /// 根据经验TimeExp buffer上次增加经验的时间
        /// </summary>
        private long _StartAddExpTicks = 0;

        /// <summary>
        /// 根据经验TimeExp buffer上次增加经验的时间
        /// </summary>
        public long StartAddExpTicks
        {
            get { lock (this) return _StartAddExpTicks; }
            set { lock (this) _StartAddExpTicks = value; }
        }

        /// <summary>
        /// buffer项标记(在修改传奇版本时，发现以前定义变量的方式太麻烦了)
        /// </summary>
        private Dictionary<int, BufferData> _BufferDataDict = new Dictionary<int, BufferData>();

        /// <summary>
        /// buffer项标记(在修改传奇版本时，发现以前定义变量的方式太麻烦了)
        /// </summary>
        public Dictionary<int, BufferData> BufferDataDict
        {
            get { lock (this) return _BufferDataDict; }
            set { lock (this) _BufferDataDict = value; }
        }

        /// <summary>
        /// 根据TimeAddLifeMagic buffer上次增加生命和蓝的时间
        /// </summary>
        private long _StartAddLifeMagicTicks = 0;

        /// <summary>
        /// 根据TimeAddLifeMagic buffer上次增加生命和蓝的时间
        /// </summary>
        public long StartAddLifeMagicTicks
        {
            get { lock (this) return _StartAddLifeMagicTicks; }
            set { lock (this) _StartAddLifeMagicTicks = value; }
        }

        /// <summary>
        /// 根据TimeAddLifeNoShow buffer上次增加生命时间
        /// </summary>
        private long _StartAddLifeNoShowTicks = 0;

        /// <summary>
        /// 根据TimeAddLifeNoShow buffer上次增加生命和蓝的时间
        /// </summary>
        public long StartAddLifeNoShowTicks
        {
            get { lock (this) return _StartAddLifeNoShowTicks; }
            set { lock (this) _StartAddLifeNoShowTicks = value; }
        }

        /// <summary>
        /// 根据TimeAddMagicNoShow buffer上次增加生命时间
        /// </summary>
        private long _StartAddMaigcNoShowTicks = 0;

        /// <summary>
        /// 根据TimeAddMagicNoShow buffer上次增加生命和蓝的时间
        /// </summary>
        public long StartAddMaigcNoShowTicks
        {
            get { lock (this) return _StartAddMaigcNoShowTicks; }
            set { lock (this) _StartAddMaigcNoShowTicks = value; }
        }

        /// <summary>
        /// 根据DSTimeAddLifeNoShow buffer上次增加生命时间
        /// </summary>
        private long _DSStartDSAddLifeNoShowTicks = 0;

        /// <summary>
        /// 根据DSTimeAddLifeNoShow buffer上次增加生命和蓝的时间
        /// </summary>
        public long DSStartDSAddLifeNoShowTicks
        {
            get { lock (this) return _DSStartDSAddLifeNoShowTicks; }
            set { lock (this) _DSStartDSAddLifeNoShowTicks = value; }
        }

        /// <summary>
        /// 位置变化通知函数
        /// </summary>
        public event ChangePosEventHandler ChangePosHandler;

        /// <summary>
        /// 上次记录自己攻击角色的时间
        /// </summary>
        private long _LastLogRoleIDAttackebByMyselfTicks = 0;

        /// <summary>
        /// 自己攻击的目标的角色ID,包括怪物和玩家,多角色时只考虑一个
        /// </summary>
        private int _RoleIDAttackebByMyself = 0;

        /// <summary>
        /// 自己攻击的目标的角色ID,包括怪物和玩家,多角色时只考虑一个
        /// </summary>
        public int RoleIDAttackebByMyself
        {
            get
            {
                //15秒后无效
                if (TimeUtil.NOW() - _LastLogRoleIDAttackebByMyselfTicks > 15000)
                {
                    return -1;
                }

                lock (this) return _RoleIDAttackebByMyself;
            }

            set
            {
                _LastLogRoleIDAttackebByMyselfTicks = TimeUtil.NOW();

                lock (this) _RoleIDAttackebByMyself = value;
            }
        }

        /// <summary>
        /// 上次记录别人攻击自己的时间
        /// </summary>
        private long _LastLogRoleIDAttackMeTicks = 0;

        /// <summary>
        /// 攻击的自己目标的角色ID,包括怪物和玩家,多角色时只考虑一个
        /// </summary>
        private int _RoleIDAttackMe = 0;

        /// <summary>
        /// 攻击的自己目标的角色ID,包括怪物和玩家,多角色时只考虑一个
        /// </summary>
        public int RoleIDAttackMe
        {
            get
            {
                //15秒后无效
                if (TimeUtil.NOW() - _LastLogRoleIDAttackMeTicks > 15000)
                {
                    return -1;
                }

                lock (this) return _RoleIDAttackMe;
            }

            set
            {
                _LastLogRoleIDAttackMeTicks = TimeUtil.NOW();

                lock (this) _RoleIDAttackMe = value;
            }
        }

        /// <summary>
        /// 角色常用整形参数值列表
        /// </summary>
        public List<int> _RoleCommonUseIntPamams = new List<int>();

        /// <summary>
        /// 角色常用整形参数值列表
        /// </summary>
        public List<int> RoleCommonUseIntPamams
        {
            get { lock (this) return _RoleCommonUseIntPamams; }

            set { lock (this) _RoleCommonUseIntPamams = value; }
        }

        /// <summary>
        /// 上次地图更新判断时间时间 主要用于使用道具进入的限时地图 古墓地图 冥界地图
        /// </summary>
        private long _LastMapLimitUpdateTicks = TimeUtil.NOW();

        /// <summary>
        /// 上次地图更新判断时间时间 主要用于使用道具进入的限时地图 古墓地图 冥界地图
        /// </summary>
        public long LastMapLimitUpdateTicks
        {
            get { lock (this) return _LastMapLimitUpdateTicks; }
            set { lock (this) _LastMapLimitUpdateTicks = value; }
        }

        /// <summary>
        /// 上次提示用户更新客户端的时间
        /// </summary>
        private long _LastHintToUpdateClientTicks = TimeUtil.NOW();

        /// <summary>
        /// 上次提示用户更新客户端的时间
        /// </summary>
        public long LastHintToUpdateClientTicks
        {
            get { lock (this) return _LastHintToUpdateClientTicks; }
            set { lock (this) _LastHintToUpdateClientTicks = value; }
        }

        /// <summary>
        /// 上次角色基础战斗属性，主要用于装备切换过程中的穿戴条件判断，索引从0开始，依次是最大物理攻击，最大魔法攻击，最大道术攻击
        /// 【最后一次的战斗属性】 卸载前记录，穿戴时判断，穿戴后记录
        /// </summary>
        private int[] _BaseBattleAttributesOfLastTime = new int[3];

        /// <summary>
        /// 上次角色基础战斗属性，主要用于装备切换过程中的穿戴条件判断，索引从0开始，依次是最大物理攻击，最大魔法攻击，最大道术攻击
        /// 【最后一次的战斗属性】 卸载前记录，穿戴时判断，穿戴后记录
        /// </summary>
        public int[] BaseBattleAttributesOfLastTime
        {
            get { lock (this) return _BaseBattleAttributesOfLastTime; }

            set { lock (this) _BaseBattleAttributesOfLastTime = value; }
        }

        /// <summary>
        /// 物品使用限时更新时间
        /// </summary>
        private long _LastGoodsLimitUpdateTicks = TimeUtil.NOW();

        /// <summary>
        /// 物品使用限时更新时间
        /// </summary>
        public long LastGoodsLimitUpdateTicks
        {
            get { lock (this) return _LastGoodsLimitUpdateTicks; }
            set { lock (this) _LastGoodsLimitUpdateTicks = value; }
        }


        /// <summary>
        /// 时装限时更新时间
        /// </summary>
        private long _LastFashionLimitUpdateTicks = TimeUtil.NOW();

        /// <summary>
        /// 时装限时更新时间
        /// </summary>
        public long LastFashionLimitUpdateTicks
        {
            get { lock (this) return _LastFashionLimitUpdateTicks; }
            set { lock (this) _LastFashionLimitUpdateTicks = value; }
        }

        /// <summary>
        /// 角色上次死亡时间
        /// </summary>
        private long _LastRoleDeadTicks = TimeUtil.NOW();

        /// <summary>
        /// 角色上次死亡时间
        /// </summary>
        public long LastRoleDeadTicks
        {
            get { lock (this) return _LastRoleDeadTicks; }
            set { lock (this) _LastRoleDeadTicks = value; }
        }

        /// <summary>
        /// 记录移动或者动作的次数
        /// </summary>
        private int _MoveAndActionNum = 0;

        /// <summary>
        /// 记录移动或者动作的次数
        /// </summary>
        public int MoveAndActionNum
        {
            get { lock (this) return _MoveAndActionNum; }
            set { lock (this) _MoveAndActionNum = value; }
        }

        /// <summary>
        /// 九宫格内的可见对象
        /// </summary>
        public Dictionary<Object, byte> _VisibleGrid9Objects = new Dictionary<object, byte>();

        /// <summary>
        /// 九宫格内的可见对象
        /// </summary>
        public Dictionary<Object, byte> VisibleGrid9Objects
        {
            get { lock (this) return _VisibleGrid9Objects; }
            set { lock (this) _VisibleGrid9Objects = value; }
        }

        /// <summary>
        /// 可以看到我的其他玩家对象列表
        /// </summary>
        public Dictionary<Object, byte> _VisibleMeGrid9GameClients = new Dictionary<object, byte>();

        /// <summary>
        /// 可以看到我的其他玩家对象列表
        /// </summary>
        public Dictionary<Object, byte> VisibleMeGrid9GameClients
        {
            get { lock (this) return _VisibleMeGrid9GameClients; }
            set { lock (this) _VisibleMeGrid9GameClients = value; }
        }

        /// <summary>
        /// 当前缓存的抢购项
        /// </summary>
        private List<QiangGouItemData> _QiangGouItemList = null;

        /// <summary>
        /// 当前缓存的抢购项
        /// </summary>
        public List<QiangGouItemData> QiangGouItemList
        {
            get { lock (this) return _QiangGouItemList; }
            set { lock (this) _QiangGouItemList = value; }
        }

        /// <summary>
        /// 中毒开始的时间
        /// </summary>
        private long _ZhongDuStart = 0;

        /// <summary>
        /// 中毒开始的时间
        /// </summary>
        public long ZhongDuStart
        {
            get { lock (this) return _ZhongDuStart; }
            set { lock (this) _ZhongDuStart = value; }
        }

        /// <summary>
        /// 中毒的持续时间
        /// </summary>
        private int _ZhongDuSeconds = 0;

        /// <summary>
        /// 中毒的持续时间
        /// </summary>
        public int ZhongDuSeconds
        {
            get { lock (this) return _ZhongDuSeconds; }
            set { lock (this) _ZhongDuSeconds = value; }
        }

        /// <summary>
        /// 放毒的角色的ID
        /// </summary>
        private int _FangDuRoleID = 0;

        /// <summary>
        /// 放毒的角色的ID
        /// </summary>
        public int FangDuRoleID
        {
            get { lock (this) return _FangDuRoleID; }
            set { lock (this) _FangDuRoleID = value; }
        }

        /// <summary>
        /// 根据DSTimeShiDuNoShow buffer上次伤害时间
        /// </summary>
        private long _DSStartDSSubLifeNoShowTicks = 0;

        /// <summary>
        /// 根据DSTimeShiDuNoShow buffer上次伤害时间
        /// </summary>
        public long DSStartDSSubLifeNoShowTicks
        {
            get { lock (this) return _DSStartDSSubLifeNoShowTicks; }
            set { lock (this) _DSStartDSSubLifeNoShowTicks = value; }
        }

        /// <summary>
        /// 节日称号的值
        /// </summary>
        private int _JieriChengHao = 0;

        /// <summary>
        /// 节日称号的值
        /// </summary>
        public int JieriChengHao
        {
            get { lock (this) return _JieriChengHao; }
            set { lock (this) _JieriChengHao = value; }
        }

        /// <summary>
        /// 上次特殊戒指触发时间
        /// </summary>
        private long _SpecialEquipLastUseTicks = 0;

        /// <summary>
        /// 上次特殊戒指触发时间
        /// </summary>
        public long SpecialEquipLastUseTicks
        {
            get { lock (this) return _SpecialEquipLastUseTicks; }
            set { lock (this) _SpecialEquipLastUseTicks = value; }
        }

        /// <summary>
        /// 冻结开始的时间
        /// </summary>
        private long _DongJieStart = 0;

        /// <summary>
        /// 冻结开始的时间
        /// </summary>
        public long DongJieStart
        {
            get { lock (this) return _DongJieStart; }
            set { lock (this) _DongJieStart = value; }
        }

        /// <summary>
        /// 冻结的持续时间
        /// </summary>
        private int _DongJieSeconds = 0;

        /// <summary>
        /// 冻结的持续时间
        /// </summary>
        public int DongJieSeconds
        {
            get { lock (this) return _DongJieSeconds; }
            set { lock (this) _DongJieSeconds = value; }
        }

        /// <summary>
        /// 是否被冻结了
        /// </summary>
        /// <returns></returns>
        public bool IsDongJie()
        {
            if (DongJieStart <= 0)
            {
                return false;
            }

            long ticks = TimeUtil.NOW();
            if (ticks >= (DongJieStart + (DongJieSeconds * 1000)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 拾取物品的锁定
        /// </summary>
        private object _PickUpGoodsPackMutex = new object();

        /// <summary>
        /// 拾取物品的锁定
        /// </summary>
        public object PickUpGoodsPackMutex
        {
            get { lock (this) return _PickUpGoodsPackMutex; }
        }


        /// <summary>
        /// 冥想时间计时
        /// </summary>
        private long _MeditateTicks = TimeUtil.NOW();

        /// <summary>
        /// 冥想时间计时
        /// </summary>
        public long MeditateTicks
        {
            get { lock (this) return _MeditateTicks; }
            set { lock (this) _MeditateTicks = value; }
        }

        /// <summary>
        /// 上次处理死亡状态的时间
        /// </summary>
        public long LastProcessDeadTicks = 0;

        /// <summary>
        /// 冥想时间
        /// </summary>
        private int _MeditateTime = 0;

        /// <summary>
        /// 冥想时间
        /// </summary>
        public int MeditateTime
        {
            get { lock (this) return _MeditateTime; }
            set { lock (this) _MeditateTime = value; }
        }

        /// <summary>
        /// 野外冥想时间
        /// </summary>
        private int _NotSafeMeditateTime = 0;

        /// <summary>
        /// 野外冥想时间
        /// </summary>
        public int NotSafeMeditateTime
        {
            get { lock (this) return _NotSafeMeditateTime; }
            set { lock (this) _NotSafeMeditateTime = value; }
        }

        /// <summary>
        /// 开始冥想时间
        /// </summary>
        private int _StartMeditate = 0;

        /// <summary>
        /// 开始冥想时间
        /// </summary>
        public int StartMeditate
        {
            get { lock (this) return _StartMeditate; }
            set { lock (this) _StartMeditate = value; }
        }

        /// <summary>
        /// 上次检测到位置变化的时刻
        /// </summary>
        public long LastMovePosTicks = 0;

        /// <summary>
        /// 上次记录的X坐标
        /// </summary>
        public int Last10sPosX { get; set; }

        /// <summary>
        /// 上次记录的Y坐标
        /// </summary>
        public int Last10sPosY { get; set; }

        /// <summary>
        /// 仓库金币的修改锁
        /// </summary>
        private object _StoreYinLiangMutex = new object();

        /// <summary>
        /// 仓库金币的修改锁
        /// </summary>
        public object StoreYinLiangMutex
        {
            get { lock (this) return _StoreYinLiangMutex; }
        }

        /// <summary>
        /// 仓库绑定币的修改锁
        /// </summary>
        private object _StoreMoneyMutex = new object();

        /// <summary>
        /// 仓库绑定金币的修改锁
        /// </summary>
        public object StoreMoneyMutex
        {
            get { lock (this) return _StoreMoneyMutex; }
        }

        #endregion 额外添加值

        // 血色堡垒  [11/8/2013 LiaoWei]
        #region 血色堡垒

        /// <summary>
        /// 是否在血色堡垒中 不存盘
        /// </summary>
        private bool _bIsInBloodCastleMap = false;

        public bool bIsInBloodCastleMap
        {
            get { return _bIsInBloodCastleMap; }
            set { _bIsInBloodCastleMap = value; }
        }

        /// <summary>
        /// 血色积分  不存盘 在下线时检测 有值就用公式算出奖励并给与
        /// </summary>
        private int _BloodCastleAwardPoint = 0;

        public int BloodCastleAwardPoint
        {
            get { return _BloodCastleAwardPoint; }
            set { _BloodCastleAwardPoint = value; }
        }

        private int _BloodCastleAwardPointTmp = 0;

        public int BloodCastleAwardPointTmp
        {
            get { return _BloodCastleAwardPointTmp; }
            set { _BloodCastleAwardPointTmp = value; }
        }

        /// <summary>
        /// 血色最高积分
        /// </summary>
        private int _BloodCastleAwardTotalPoint = 0;

        public int BloodCastleAwardTotalPoint
        {
            get { return _BloodCastleAwardTotalPoint; }
            set { _BloodCastleAwardTotalPoint = value; }
        }

        #endregion 血色堡垒

        // 阵营战场 [12/24/2013 LiaoWei]
        #region 阵营战场

        /// <summary>
        /// 阵营战场最高积分
        /// </summary>
        private int _CampBattleTotalPoint = 0;

        public int CampBattleTotalPoint
        {
            get { return _CampBattleTotalPoint; }
            set { _CampBattleTotalPoint = value; }
        }

        #endregion 阵营战场

        // 恶魔广场 [12/24/2013 LiaoWei]
        #region 恶魔广场

        /// <summary>
        /// 是否在恶魔广场 中 不存盘
        /// </summary>
        private bool _bIsInDaimonSquareMap = false;

        public bool bIsInDaimonSquareMap
        {
            get { return _bIsInDaimonSquareMap; }
            set { _bIsInDaimonSquareMap = value; }
        }

        /// <summary>
        /// 恶魔积分  不存盘 在下线时检测 有值就用公式算出奖励并给与
        /// </summary>
        private int _DaimonSquarePoint = 0;

        public int DaimonSquarePoint
        {
            get { return _DaimonSquarePoint; }
            set { _DaimonSquarePoint = value; }
        }

        /// <summary>
        /// 恶魔最高积分
        /// </summary>
        private int _DaimonSquarePointTotalPoint = 0;

        public int DaimonSquarePointTotalPoint
        {
            get { return _DaimonSquarePointTotalPoint; }
            set { _DaimonSquarePointTotalPoint = value; }
        }

        #endregion 恶魔广场

        #region PK之王

        /// <summary>
        /// PK当前积分
        /// </summary>
        private int _KingOfPkCurrentPoint = 0;

        /// <summary>
        /// PK之王当前积分
        /// </summary>
        public int KingOfPkCurrentPoint
        {
            get { return _KingOfPkCurrentPoint; }
            set { _KingOfPkCurrentPoint = value; }
        }

        /// <summary>
        /// PK之王最高积分
        /// </summary>
        private int _KingOfPkTopPoint = 0;

        /// <summary>
        /// PK之王最高积分
        /// </summary>
        public int KingOfPkTopPoint
        {
            get { return _KingOfPkTopPoint; }
            set { _KingOfPkTopPoint = value; }
        }

        #endregion PK之王

        #region 天使神殿

        /// <summary>
        /// 天使神殿当前积分
        /// </summary>
        private long _AngelTempleCurrentPoint = 0;

        /// <summary>
        /// 天使神殿当前积分
        /// </summary>
        public long AngelTempleCurrentPoint
        {
            get { return _AngelTempleCurrentPoint; }
            set { _AngelTempleCurrentPoint = value; }
        }

        /// <summary>
        /// 天使神殿最高积分
        /// </summary>
        private long _AngelTempleTopPoint = 0;

        /// <summary>
        /// 天使神殿最高积分
        /// </summary>
        public long AngelTempleTopPoint
        {
            get { return _AngelTempleTopPoint; }
            set { _AngelTempleTopPoint = value; }
        }

        /// <summary>
        /// 是否在天使神殿中 不存盘
        /// </summary>
        private bool _bIsInAngelTempleMap = false;

        public bool bIsInAngelTempleMap
        {
            get { return _bIsInAngelTempleMap; }
            set { _bIsInAngelTempleMap = value; }
        }
        #endregion 天使神殿

        #region 卓越属性

        private double[] _ExcellenceProp = new double[(int)ExcellencePorp.EXCELLENCEPORPMAXINDEX];

        public double[] ExcellenceProp
        {
            get { return _ExcellenceProp; }
            set { _ExcellenceProp = value; }
        }

        public void ResetExcellenceProp()
        {
            for (int i = 0; i < (int)ExcellencePorp.EXCELLENCEPORPMAXINDEX; i++)
            {
                _ExcellenceProp[i] = 0;
            }
        }

        #endregion 卓越属性

        #region 幸运属性

        private double _LuckProp = 0.0;

        public double LuckProp
        {
            get { return _LuckProp; }
            set { _LuckProp = value; }
        }

        public void ResetLuckyProp()
        {
            _LuckProp = 0.0;
        }


        #endregion 幸运属性

        #region 每日登陆时长

        /// <summary>
        /// 每日登陆时间长 单位-秒
        /// </summary>
        private int _DayOnlineSecond = 10;

        /// <summary>
        /// 每日登陆时间长 单位-秒
        /// </summary>
        public int DayOnlineSecond
        {
            get { return _DayOnlineSecond; }
            set { _DayOnlineSecond = value; }
        }

        /// <summary>
        /// 临时每日登陆时间长 单位-秒
        /// </summary>
        private int _BakDayOnlineSecond = 10;

        /// <summary>
        /// 临时每日登陆时间长 单位-秒
        /// </summary>
        public int BakDayOnlineSecond
        {
            get { return _BakDayOnlineSecond; }
            set { _BakDayOnlineSecond = value; }
        }

        /// <summary>
        /// 登陆时间长开始记录时间 单位-秒
        /// </summary>
        private long _DayOnlineRecSecond = 10;

        /// <summary>
        /// 登陆时间长开始记录时间 单位-秒
        /// </summary>
        public long DayOnlineRecSecond
        {
            get { return _DayOnlineRecSecond; }
            set { _DayOnlineRecSecond = value; }
        }

        /// <summary>
        /// 连续登陆次数 1-7
        /// </summary>
        public int _SeriesLoginNum = 0;

        /// <summary>
        /// 连续登陆次数 1-7
        /// </summary>
        public int SeriesLoginNum
        {
            get { return _SeriesLoginNum; }
            set { _SeriesLoginNum = value; }
        }

        public List<FallGoodsItem> EverydayOnlineAwardGiftList
        {
            get;
            set;
        }

        public List<FallGoodsItem> SeriesLoginAwardGiftList
        {
            get;
            set;
        }

        #endregion 每日登陆时长

        #region 活跃值

        /// <summary>
        /// 玩家活跃值
        /// </summary>
        private int _DailyActiveValues = 0;

        /// <summary>
        /// 玩家活跃值
        /// </summary>
        public int DailyActiveValues
        {
            get { lock (this) return _DailyActiveValues; }
            set { lock (this) _DailyActiveValues = value; }
        }

        /// <summary>
        /// 玩家活跃dayID
        /// </summary>
        private int _DailyActiveDayID = 0;

        /// <summary>
        /// 玩家活跃dayID
        /// </summary>
        public int DailyActiveDayID
        {
            get { lock (this) return _DailyActiveDayID; }
            set { lock (this) _DailyActiveDayID = value; }
        }

        /// <summary>
        /// 玩家活跃每日登陆次数
        /// </summary>
        private uint _DailyActiveDayLginCount = 0;

        /// <summary>
        /// 玩家活跃每日登陆次数
        /// </summary>
        public uint DailyActiveDayLginCount
        {
            get { lock (this) return _DailyActiveDayLginCount; }
            set { lock (this) _DailyActiveDayLginCount = value; }
        }

        /// <summary>
        /// 玩家活跃每日登陆次数设置标记  --  为了不发多次消息给客户端 节省流量
        /// </summary>
        private bool _DailyActiveDayLginSetFlag = false;

        /// <summary>
        /// 玩家活跃每日登陆次数设置标记
        /// </summary>
        public bool DailyActiveDayLginSetFlag
        {
            get { lock (this) return _DailyActiveDayLginSetFlag; }
            set { lock (this) _DailyActiveDayLginSetFlag = value; }
        }

        /// <summary>
        /// 玩家活跃每日商城购买计费
        /// </summary>
        private int _DailyActiveDayBuyItemInMall = 0;

        /// <summary>
        /// 玩家活跃每日商城购买计费
        /// </summary>
        public int DailyActiveDayBuyItemInMall
        {
            get { lock (this) return _DailyActiveDayBuyItemInMall; }
            set { lock (this) _DailyActiveDayBuyItemInMall = value; }
        }

        /// <summary>
        /// 玩家每日杀总怪数
        /// </summary>
        private uint _DailyTotalKillMonsterNum = 0;

        /// <summary>
        /// 玩家每日杀总怪数
        /// </summary>
        public uint DailyTotalKillMonsterNum
        {
            get { lock (this) return _DailyTotalKillMonsterNum; }
            set { lock (this) _DailyTotalKillMonsterNum = value; }
        }

        /// <summary>
        /// 玩家每日杀Boss数
        /// </summary>
        private uint _DailyTotalKillKillBossNum = 0;

        /// <summary>
        /// 玩家每日杀Boss数
        /// </summary>
        public uint DailyTotalKillKillBossNum
        {
            get { lock (this) return _DailyTotalKillKillBossNum; }
            set { lock (this) _DailyTotalKillKillBossNum = value; }
        }

        /// <summary>
        /// 玩家每日完成日常跑环任务的数量
        /// </summary>
        private uint _DailyCompleteDailyTaskCount = 0;

        /// <summary>
        /// 玩家每日完成日常跑环任务的数量
        /// </summary>
        public uint DailyCompleteDailyTaskCount
        {
            get { lock (this) return _DailyCompleteDailyTaskCount; }
            set { lock (this) _DailyCompleteDailyTaskCount = value; }
        }

        /// <summary>
        /// 下一个杀死怪物每日活跃的数量，这样能在每个怪物被杀死的时进行更快速的判断
        /// </summary>
        private uint _DailyNextKillMonsterNum = 0;

        /// <summary>
        /// 下一个杀死怪物每日活跃的数量，这样能在每个怪物被杀死的时进行更快速的判断
        /// </summary>
        public uint DailyNextKillMonsterNum
        {
            get { lock (this) return _DailyNextKillMonsterNum; }
            set { lock (this) _DailyNextKillMonsterNum = value; }
        }

        /// <summary>
        /// 临时时间 秒
        /// </summary>
        private int _DailyOnlineTimeTmp = 60;

        /// <summary>
        /// 临时时间 秒
        /// </summary>
        public int DailyOnlineTimeTmp
        {
            get { lock (this) return _DailyOnlineTimeTmp; }
            set { lock (this) _DailyOnlineTimeTmp = value; }
        }

        #endregion 活跃值

        #region 交易市场补充/合并摆摊/离线摆摊功能

        /// <summary>
        /// 是否开放了交易市场
        /// </summary>
        private bool _AllowMarketBuy = false;

        /// <summary>
        /// 是否开放了交易市场
        /// </summary>
        public bool AllowMarketBuy
        {
            get { return _AllowMarketBuy; }
            set { _AllowMarketBuy = value; }
        }

        /// <summary>
        /// 是否离线时进入离线摆摊状态持续的状态
        /// </summary>
        private int _OfflineMarketState = 0;

        /// <summary>
        /// 是否离线时进入离线摆摊状态
        /// </summary>
        public int OfflineMarketState
        {
            get { return _OfflineMarketState; }
            set { _OfflineMarketState = value; }
        }

        /// <summary>
        /// 交易市场的摊位名称
        /// </summary>
        private string _MarketName = "";

        /// <summary>
        /// 交易市场的摊位名称
        /// </summary>
        public string MarketName
        {
            get { return _MarketName; }
            set { _MarketName = value; }
        }

        #endregion 交易市场补充/合并摆摊/离线摆摊功能

        #region VIP

        /// <summary>
        /// VIP等级
        /// </summary>
        private int _VipLevel = 0;

        /// <summary>
        /// VIP等级
        /// </summary>
        public int VipLevel
        {
            get { lock (this) return _VipLevel; }
            set { lock (this) _VipLevel = _RoleDataEx.VIPLevel = value; }
        }

        /// <summary>
        /// VIP奖励标记
        /// </summary>
        private int _VipAwardFlag = 0;

        /// <summary>
        /// VIP奖励标记
        /// </summary>
        public int VipAwardFlag
        {
            get { lock (this) return _VipAwardFlag; }
            set { lock (this) _VipAwardFlag = value; }
        }

        #endregion VIP

        #region 每日在线奖励相关
        // 说明：每日在线奖励 客户端的表现是 转盘转动 为了避免穿帮 玩家点击奖励之后 不立即给物品 等待客户端回包后 再把物品给玩家 [3/27/2014 LiaoWei]

        /// <summary>
        /// 每日在线奖励
        /// </summary>
        private List<GoodsData> _DailyOnLineAwardGift = null;

        /// <summary>
        /// 每日在线奖励
        /// </summary>
        public List<GoodsData> DailyOnLineAwardGift
        {
            get { lock (this) return _DailyOnLineAwardGift; }
            set { lock (this) _DailyOnLineAwardGift = value; }
        }

        /// <summary>
        /// 连续登陆奖励
        /// </summary>
        private List<GoodsData> _SeriesLoginAwardGift = null;

        /// <summary>
        /// 连续奖励
        /// </summary>
        public List<GoodsData> SeriesLoginAwardGift
        {
            get { lock (this) return _SeriesLoginAwardGift; }
            set { lock (this) _SeriesLoginAwardGift = value; }
        }
        #endregion 每日在线奖励相关

        #region 背包格子开启

        /// </summary>
        /// 背包格子开启时间
        /// </summary>
        private int _OpenGridTime = 0;

        /// <summary>
        /// 背包格子开启时间
        /// </summary>
        public int OpenGridTime
        {
            get { lock (this) return _OpenGridTime; }
            set { lock (this) _OpenGridTime = value; }
        }

        /// </summary>
        /// 背包随身仓库格子开启时间
        /// </summary>
        private int _OpenPortableGridTime = 0;

        /// <summary>
        /// 背包随身仓库格子开启时间
        /// </summary>
        public int OpenPortableGridTime
        {
            get { lock (this) return _OpenPortableGridTime; }
            set { lock (this) _OpenPortableGridTime = value; }
        }

        /// <summary>
        /// 打开仓库时的坐标(X,Y)
        /// </summary>
        public Point OpenPortableBagPoint;

        #endregion 背包格子开启

        #region 图鉴

        /// <summary>
        /// 图鉴提交信息
        /// </summary>
        public Dictionary<int, int> PictureJudgeReferInfo
        {
            get { return _RoleDataEx.RolePictureJudgeReferInfo; }
            set { _RoleDataEx.RolePictureJudgeReferInfo = value; }
        }

        // 保存所有已激活的图鉴Item
        public HashSet<int> ActivedTuJianItem = new HashSet<int>();

        // 保存所有已激活的图鉴Type
        public HashSet<int> ActivedTuJianType = new HashSet<int>();

        #endregion 图鉴

        #region 万魔塔

        /// <summary>
        /// 万魔塔属性值
        /// </summary>
        private WanMotaInfo _WanMoTaProp;

        /// <summary>
        /// 万魔塔属性值
        /// </summary>
        public WanMotaInfo WanMoTaProp
        {
            get { return _WanMoTaProp; }
            set { _WanMoTaProp = value; }
        }

        /// <summary>
        /// 万魔塔扫荡奖励数据
        /// </summary>
        private LayerRewardData _LayerRewardData = null;

        /// <summary>
        /// 万魔塔扫荡奖励数据
        /// </summary>
        public LayerRewardData LayerRewardData
        {
            get { return _LayerRewardData; }
            set { _LayerRewardData = value; }
        }

        /// <summary>
        /// 万魔塔扫荡测定时器
        /// </summary>
        private SweepWanmota _WanMoTaSweeping = null;

        /// <summary>
        /// 万魔塔扫荡测定时器
        /// </summary>
        public SweepWanmota WanMoTaSweeping
        {
            get { return _WanMoTaSweeping; }
            set { _WanMoTaSweeping = value; }
        }

        #endregion 万魔塔

        #region 是否正在切换地图中

        /// <summary>
        /// 是否正在切换地图
        /// </summary>
        private bool _WaitingForChangeMap = false;

        /// <summary>
        /// 是否正在切换地图
        /// </summary>
        public bool WaitingForChangeMap
        {
            get { lock (this) return _WaitingForChangeMap; }
            set { lock (this) _WaitingForChangeMap = value; }
        }

        #endregion 是否正在切换地图中

        #region 绑定钻石兑换信息

        /// <summary>
        /// 魔晶兑换DayID
        /// </summary>
        public int MoJingExchangeDayID = 0;

        /// <summary>
        /// 魔晶兑换信息 -- key 魔晶兑换ID  value 今日兑换的数量
        /// </summary>
        private Dictionary<int, int> _MoJingExchangeInfo = null;

        /// <summary>
        /// 魔晶兑换信息
        /// </summary>
        public Dictionary<int, int> MoJingExchangeInfo
        {
            get { lock (this)  return _MoJingExchangeInfo; }
            set { lock (this)  _MoJingExchangeInfo = value; }
        }

        #endregion 绑定钻石兑换信息

        #region 本地进程加速防止误判的容错次数

        /// <summary>
        /// 容错次数
        /// </summary>
        private int _MaxAntiProcessJiaSuSubNum = 0;

        /// <summary>
        /// 容错次数
        /// </summary>
        public int MaxAntiProcessJiaSuSubNum
        {
            get { lock (this) return _MaxAntiProcessJiaSuSubNum; }
            set { lock (this) _MaxAntiProcessJiaSuSubNum = value; }
        }

        #endregion 本地进程加速防止误判的容错次数

        #region 任务章节

        public int CompleteTaskZhangJie;

        #endregion 任务章节

        #region BUG检测追踪记录

        /// <summary>
        /// 上次通知传送地图的时间点
        /// </summary>
        public long LastNotifyChangeMapTicks;

        /// <summary>
        /// 上次传送地图的时间点
        /// </summary>
        public long LastChangeMapTicks;

        #endregion BUG检测追踪记录

        #region 洗练数值临时存储

        /// <summary>
        /// 临时的洗炼操作索引
        /// </summary>
        public int TempWashPropOperationIndex = 0;

        /// <summary>
        /// 临时存储的洗炼结果
        /// </summary>
        public Dictionary<int, UpdateGoodsArgs> TempWashPropsDict = new Dictionary<int, UpdateGoodsArgs>();

        #endregion 洗练数值临时存储

        #region 世界等级数值临时存储

        /// <summary>
        /// 临时的世界等级经验加成百分比
        /// </summary>
        public double nTempWorldLevelPer = 0.0;

        #endregion 世界等级数值临时存储

        #region 扩展属性ID

        /// <summary>
        /// 扩展属性ID管理
        /// </summary>
        public SpriteExtensionProps ExtensionProps = new SpriteExtensionProps();

        #endregion 扩展属性ID

        /// <summary>
        /// 昨天的日常任务数据
        /// </summary>
        public DailyTaskData YesterdayDailyTaskData = null;

        /// <summary>
        /// 昨天的讨伐任务数据
        /// </summary>
        public DailyTaskData YesterdayTaofaTaskData = null;

        /// <summary>
        /// 资源找回数据
        /// </summary>
        public Dictionary<int, OldResourceInfo> OldResourceInfoDict = null;

        List<FuBenData> _OldFuBenDataList = new List<FuBenData>();
        /// <summary>
        /// 旧副本数据
        /// </summary>
        public List<FuBenData> OldFuBenDataList
        {
            get { lock (this) return _OldFuBenDataList; }
            set { lock (this) OldFuBenDataList = value; }
        }

        /// <summary>
        /// 成就符文数据
        /// </summary>
        public AchievementRuneData achievementRuneData = null;

        /// <summary>
        /// 声望勋章数据
        /// </summary>
        public PrestigeMedalData prestigeMedalData = null;

        public UnionPalaceData MyUnionPalaceData = null;

        private object _LockUnionPalace = new object();
        public object LockUnionPalace
        {
            get { return _LockUnionPalace; }
        }

        public int EveryDayUpDate = 0;

        /// <summary>
        /// 玩家召回
        /// </summary>
        public UserReturnData ReturnData = null;

        /// <summary>
        /// 玩家召回修改锁
        /// </summary>
        public object LockReturnData = new object();

        /// <summary>
        /// 天赋
        /// </summary>
        public TalentData MyTalentData
        {
            get { lock (this) return _RoleDataEx.MyTalentData; }
            set { lock (this) _RoleDataEx.MyTalentData = value; }
        }

        private TalentPropData _myTalentPropData = new TalentPropData();

        /// <summary>
        /// 天赋效果增加属性
        /// </summary>
        public TalentPropData MyTalentPropData
        {
            get { lock (this) return _myTalentPropData; }
            set { lock (this) _myTalentPropData = value; }
        }

        #region ---------推广

        private SpreadData _mySpreadData = new SpreadData();

        /// <summary>
        /// 推广数据
        /// </summary>
        public SpreadData MySpreadData
        {
            get { lock (this) return _mySpreadData; }
            set { lock (this) _mySpreadData = value; }
        }

        private SpreadVerifyData _mySpreadVerifyData = new SpreadVerifyData();

        /// <summary>
        /// 推广验证数据
        /// </summary>
        public SpreadVerifyData MySpreadVerifyData
        {
            get { lock (this) return _mySpreadVerifyData; }
            set { lock (this) _mySpreadVerifyData = value; }
        }

        /// <summary>
        /// 推广修改锁
        /// </summary>
        private object _lockSpread = new object();

        /// <summary>
        /// 推广修改锁
        /// </summary>
        public object LockSpread
        {
            get { return _lockSpread; }
        }

        #endregion



        #region 星座信息

        /// <summary>
        /// 玩家星座信息
        /// </summary>
        public Dictionary<int, int> RoleStarConstellationInfo
        {
            get { return _RoleDataEx.RoleStarConstellationInfo; }
            set { _RoleDataEx.RoleStarConstellationInfo = value; }
        }

        /// <summary>
        /// 星座属性值
        /// </summary>
        private StarConstellationProp _RoleStarConstellationProp = new StarConstellationProp();

        /// <summary>
        /// 星座属性值
        /// </summary>
        public StarConstellationProp RoleStarConstellationProp
        {
            get { return _RoleStarConstellationProp; }
        }

        /// <summary>
        /// 已经激活的星座计数 key-star type value-count
        /// </summary>
        //public Dictionary<int, int> _StarConstellationCount = new Dictionary<int, int>();

        /// <summary>
        /// 已经激活的星座计数
        /// </summary>
        //public Dictionary<int, int> StarConstellationCount
        //{
        //    get { return _StarConstellationCount; }
        //    set { _StarConstellationCount = value; }
        //}

        /// <summary>
        /// 星魂值
        /// </summary>
        public int _StarSoul = 0;

        /// <summary>
        /// 星魂值
        /// </summary>
        public int StarSoul
        {
            get { return _StarSoul; }
            set { _StarSoul = value; }
        }

        #endregion 星座信息

        #region 角色的360度方向

        /// <summary>
        /// 当前的360度方向
        /// </summary>
        private int _RoleYAngle = 0;

        /// <summary>
        /// 当前的360度方向
        /// </summary>
        public int RoleYAngle
        {
            get { lock (this) return _RoleYAngle; }
            set { lock (this) _RoleYAngle = value; }
        }

        #endregion 角色的360度方向

        #region 采集相关
        private uint _CaiJiStartTick = 0;
        public uint CaiJiStartTick
        {
            get { lock (this) return _CaiJiStartTick; }
            set { lock (this) _CaiJiStartTick = value; }
        }

        private int _CaijTargetId = 0;
        public int CaijTargetId
        {
            get { lock (this) return _CaijTargetId; }
            set { lock (this) _CaijTargetId = value; }
        }

        /// <summary>
        /// 完成水晶采集计数
        /// </summary>
        private int _DailyCrystalCollectNum = 0;
        public int DailyCrystalCollectNum
        {
            get { lock (this) return _DailyCrystalCollectNum; }
            set { lock (this) _DailyCrystalCollectNum = value; }
        }

        /// <summary>
        /// 完成水晶采集的日期
        /// </summary>
        private int _CrystalCollectDayID = 0;
        public int CrystalCollectDayID
        {
            get { lock (this) return _CrystalCollectDayID; }
            set { lock (this) _CrystalCollectDayID = value; }
        }
        /// <summary>
        /// 昨天之前（包括昨天）的采集数据
        /// </summary>
        public OldCaiJiData OldCrystalCollectData = null;
        #endregion 采集相关

        #region 城市信息采集活跃

        /// <summary>
        /// 登陆期间的活跃值
        /// </summary>
        private int _OnlineActiveVal = 0;

        /// <summary>
        /// 登陆期间的活跃值
        /// </summary>
        public int OnlineActiveVal
        {
            get { lock (this) return _OnlineActiveVal; }
            set { lock (this) _OnlineActiveVal = value; }
        }

        #endregion 城市信息采集活跃

        #region 翎羽数据

        public Dictionary<int, LingYuData> LingYuDict
        {
            get { lock (this) return _RoleDataEx.LingYuDict; }
            set { lock (this) _RoleDataEx.LingYuDict = value; }
        }

        #endregion

        #region 守护雕像
        public GuardStatueDetail MyGuardStatueDetail
        {
            get { lock (this) return _RoleDataEx.MyGuardStatueDetail; }
            set { lock (this) _RoleDataEx.MyGuardStatueDetail = value; }
        }
        #endregion
        /// <summary>
        /// 玩家月卡具体信息
        /// </summary>
        public YueKaDetail YKDetail = new YueKaDetail();

        #region 进入水晶幻境的计时

        private long _ShuiJingHuanJingTicks = TimeUtil.NOW() * 10000;

        public long ShuiJingHuanJingTicks
        {
            get { lock (this) return _ShuiJingHuanJingTicks; }
            set { lock (this) _ShuiJingHuanJingTicks = value; }
        }

        #endregion

        /// <summary>
        /// 玩家获取礼品码的时间
        /// </summary>
        private long _GetLiPinMaTicks = 0;

        /// <summary>
        /// 玩家获取礼品码的时间
        /// </summary>
        public long GetLiPinMaTicks
        {
            get { lock (this) return _GetLiPinMaTicks; }
            set { lock (this) _GetLiPinMaTicks = value; }
        }

        // 王者战场商店数据
        private KingOfBattleStoreData _KingOfBattleStroeData;

        /// <summary>
        /// 玩家获取王者战场商店数据
        /// </summary>
        public KingOfBattleStoreData KOBattleStoreData
        {
            get { lock (this) return _KingOfBattleStroeData; }
            set { lock (this) _KingOfBattleStroeData = value; }
        }

        #region 魔剑士参数0=力魔，1=智魔

        /// <summary>
        ///  魔剑士参数0=力魔，1=智魔 [XSea 2015/4/14]
        /// </summary>
        public int MagicSwordParam
        {
            get { lock (this) return _RoleDataEx.MagicSwordParam; }
            set { lock (this) _RoleDataEx.MagicSwordParam = value; }
        }

        #endregion


        #region [bing] 结婚数据

        public MarriageData MyMarriageData
        {
            get { lock (this) return _RoleDataEx.MyMarriageData; }
            set { lock (this) _RoleDataEx.MyMarriageData = value; }
        }

        /// <summary>
        /// 个人婚宴參予次数列表
        /// </summary>
        public Dictionary<int, int> MyMarryPartyJoinList
        {
            get { lock (this) return _RoleDataEx.MyMarryPartyJoinList; }
            set { lock (this) _RoleDataEx.MyMarryPartyJoinList = value; }
        }

        #endregion

        #region 物品使用限制，材料替换时，对特定物品传参，add by chenjingui
        public ReplaceExtArg _ReplaceExtArg = new ReplaceExtArg();
        #endregion

        /// <summary>
        /// 群邮件发放的记录
        /// </summary>
        public List<int> GroupMailRecordList
        {
            get { lock (this) return _RoleDataEx.GroupMailRecordList; }
            set { lock (this) _RoleDataEx.GroupMailRecordList = value; }
        }

        #region 跨服战

        /// <summary>
        /// 准备状态的跨服活动类型
        /// </summary>
        public int SignUpGameType;

        /// <summary>
        /// 跨服天梯数据
        /// </summary>
        public RoleTianTiData TianTiData
        {
            get { return _RoleDataEx.TianTiData; }
        }

        public int LangHunLingYuCityAwardsCheckDayId;

        /// <summary>
        /// 圣域争霸奖励标记
        /// </summary>
        public int LangHunLingYuCityAwardsLevelFlags;

        /// <summary>
        /// 圣域争霸奖励标记,自己的
        /// </summary>
        public int LangHunLingYuCityAwardsLevelFlagsSelf;

        /// <summary>
        /// 圣域争霸日奖励领取时间
        /// </summary>
        public int LangHunLingYuCityAwardsDay;

        public bool LangHunLingYuCityAwardsCanGet;

        #endregion 跨服战

        #region 梅林魔法书
        /// <summary>
        /// 梅林魔法书 [XSea 2015/6/23]
        /// </summary>
        public MerlinGrowthSaveDBData MerlinData
        {
            get { lock (this) return _RoleDataEx.MerlinData; }
            set { lock (this) _RoleDataEx.MerlinData = value; }
        }
        #endregion

        /// <summary>
        /// [bing] 圣物系统数据
        /// </summary>
        public Dictionary<sbyte, HolyItemData> MyHolyItemDataDic
        {
            get { lock (this) return _RoleDataEx.MyHolyItemDataDic; }
        }

        /// <summary>
        /// 塔罗牌数据
        /// </summary>
        public TarotSystemData TarotData
        {
            get { lock (this) return _RoleDataEx.TarotData; }
        }

        /// <summary>
        /// 荧光宝石数据 [XSea 2015/8/10]
        /// </summary>
        public FluorescentGemData FluorescentGemData
        {
            get { lock (this) return _RoleDataEx.FluorescentGemData; }
            set { lock (this) _RoleDataEx.FluorescentGemData = value; }
        }

        /// <summary>
        /// 荧光粉末 [XSea 2015/8/10]
        /// </summary>
        public int FluorescentPoint
        {
            get { lock (this) return _RoleDataEx.FluorescentPoint; }
            set { lock (this) _RoleDataEx.FluorescentPoint = value; }
        }

        /// <summary>
        /// 七日登录
        /// </summary>
        public Dictionary<int, Dictionary<int, SevenDayItemData>> SevenDayActDict
        {
            get { lock (this) { return _RoleDataEx.SevenDayActDict; } }
            set { lock (this) { _RoleDataEx.SevenDayActDict = value; } }
        }

        //public int Camp = -1;

        /// <summary>
        /// 魂石系统是否激活
        /// </summary>
        public bool IsSoulStoneOpened = false;

        /// <summary>
        /// 魂石背包
        /// </summary>
        public List<GoodsData> SoulStoneInBag
        {
            get { lock (this) { return _RoleDataEx.SoulStonesInBag; } }
            set { lock (this) { _RoleDataEx.SoulStonesInBag = value; } }
        }

        /// <summary>
        /// 魂石装备
        /// </summary>
        public List<GoodsData> SoulStoneInUsing
        {
            get { lock (this) { return _RoleDataEx.SoulStonesInUsing; } }
            set { lock (this) { _RoleDataEx.SoulStonesInUsing = value; } }
        }

        public long BanTradeToTicks
        {
            get { lock (this) { return _RoleDataEx.BantTradeToTicks; } }
            set { lock (this) { _RoleDataEx.BantTradeToTicks = value; } }
        }

        /// <summary>
        /// 专享活动数据
        /// </summary>
        public Dictionary<int, SpecActInfoDB> SpecActInfoDict
        {
            get { lock (this) { return _RoleDataEx.SpecActInfoDict; } }
            set { lock (this) { _RoleDataEx.SpecActInfoDict = value; } }
        }

        /// <summary>
        /// 基金
        /// </summary>
        public FundData MyFundData = new FundData();

        /// <summary>
        /// 基金修改锁
        /// </summary>
        private object _LockFund = new object();

        /// <summary>
        /// 基金修改锁
        /// </summary>
        public object LockFund
        {
            get { return _LockFund; }
        }

        /// <summary>
        /// 跨服排位赛(众神争霸)个人赞、贬、押注信息
        /// 经分析，虽然多线程使用，但无须加锁，每月10-17号玩家可修改，每月1号跨天清空，不存在同时读写的情况
        /// </summary>
        public List<ZhengBaSupportFlagData> ZhengBaSupportFlags = new List<ZhengBaSupportFlagData>();
    }
}