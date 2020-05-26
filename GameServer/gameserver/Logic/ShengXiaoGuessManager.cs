using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;
using GameServer.Server;

namespace GameServer.Logic
{
    /// <summary>
    /// 生肖运程管理
    /// </summary>
    public class ShengXiaoGuessManager
    {
        #region 配置变量定义

        /// <summary>
        /// 生肖运程的场景编号
        /// </summary>
        private int MapCode = -1;

        public int GuessMapCode
        {
            get { return MapCode; }
        }

        /// <summary>
        /// 等待下注的时间【单位秒】
        /// </summary>
        private int WaitingEnterSecs = 120;

        /// <summary>
        /// 等待boss被杀死的最大时间，超过时间杀死boss,0表示不需要倒计时,没人杀怪物就不死
        /// </summary>
        private int WaitingKillBossSecs = 0;

        /// <summary>
        /// 倒计时时间,主要用于记录各个状态的倒计时时间 WaitingEnterSecs， WaitingKillBossSecs 进行统一处理
        /// </summary>
        private long ThisTimeCountDownSecs = 0;

        /// <summary>
        /// 单项竞猜最多注码
        /// </summary>
        private int MaxMortgageForOnce = 100000;

        /// <summary>
        /// 需要的物品id，即礼金卷的物品ID
        /// </summary>
        private int NeedGoodsID = -1;

        /// <summary>
        /// 单个物品转换成的元宝数量
        /// </summary>
        private int SingleGoodsToYuanBaoNum = 100;

        /// <summary>
        /// 系统广播的最小金币条件， 门槛金币数量【单笔注码】
        /// </summary>
        private int GateGoldForBroadcast = 10000;

        /// <summary>
        /// 合法的竞猜线路 默认是1线
        /// </summary>
        private List<int> LegalServerLines = new List<int>();

        #endregion 配置变量定义

        #region 运行状态变量

        /// <summary>
        /// 竞猜状态，默认处于无抵押状态
        /// </summary>
        private ShengXiaoGuessStates GuessStates = ShengXiaoGuessStates.NoMortgage;

        /// <summary>
        /// 奖励倍数缓存信息，缓存竞猜关键字的奖励倍数信息
        /// </summary>
        private Dictionary<int, int> AwardMultipleDict = new Dictionary<int, int>();

        /// <summary>
        /// 合法的竞猜关键字，不合法的不需要
        /// </summary>
        private List<int> LegalGuessKeyList = new List<int>();

        /// <summary>
        /// 不同状态的开始时间
        /// </summary>
        private long StateStartTicks = 0;

        /// <summary>
        /// 判断boss是否已经被杀死
        /// </summary>
        private bool IsBossKilled = false;

        /// <summary>
        /// 竞猜历史信息,记录最近15条竞猜结果
        /// </summary>
        private List<int> ShengXiaoGuessResultHistory = new List<int>();

        //竞猜列表词典，key是roleID， value是 竞猜关键字和注码的字典
        private Dictionary<int, Dictionary<int, int>> GuessItemListDict = new Dictionary<int, Dictionary<int, int>>(); 

        #endregion 运行状态变量

        #region 初始化

        /// <summary>
        /// 初始化生肖竞猜
        /// </summary>
        public void Init()
        {
            //这儿需要加载配置文件，进行各项配置处理
            InitLegalGuessKeys();

            ReloadConfig(true);

            Reset();
        }

        /// <summary>
        /// 重新加载配置文件----进程启动时第一次加载允许抛出异常，gm动态加载的即使失败，也不抛异常
        /// </summary>
        public void ReloadConfig(bool throwAble = false)
        {
            //初始化各个参数  地图编号,等待所有玩家下注时间,注码物品ID,单个注码物品对应的元宝数量,单个竞猜关键字的最大注码数量
            int[] paramsArr = GameManager.systemParamsList.GetParamValueIntArrayByName("ShengXiaoGuessParams");

            if (paramsArr.Length != 6)
            {
                if (throwAble)
                {
                    throw new Exception("SystemParmas.xml中生肖竞猜参数ShengXiaoGuessParams配置个数不对");
                }
                else
                {
                    return;
                }
            }

            MapCode = paramsArr[0];
            WaitingEnterSecs = paramsArr[1];
            NeedGoodsID = paramsArr[2];
            SingleGoodsToYuanBaoNum = paramsArr[3];
            MaxMortgageForOnce = paramsArr[4];
            GateGoldForBroadcast = paramsArr[5];

            LegalServerLines.Clear();
            //默认一线
            int[] lineArr = GameManager.systemParamsList.GetParamValueIntArrayByName("ShengXiaoGuessLines");
            foreach (var line in lineArr)
            {
                LegalServerLines.Add(line);
            }
        }

        /// <summary>
        /// 初始化合法的竞猜关键字
        /// </summary>
        protected void InitLegalGuessKeys()
        {
            LegalGuessKeyList.Clear();

            //12倍关键字
            for (int n = 0; n < 12; n++)
            {
                LegalGuessKeyList.Add(0x0001 << n);
            }

            //6倍奖励关键字 [注 这儿是6个]
            for (int n = 0; n < 6; n++)
            {
                LegalGuessKeyList.Add(0x0003 << (2 * n));
            }

            //3倍奖励关键字 [注 这儿也是6个]
            for (int n = 0; n < 6; n++)
            {
                if (n < 5)
                {
                    LegalGuessKeyList.Add(0x000F << (2 * n));
                }
                else
                {
                    LegalGuessKeyList.Add(0x0c03);//这儿比较特殊，因为又循环转回到鼠兔了
                }
            }

            //2倍奖励关键字 [注 这儿是2个]
            for (int n = 0; n < 2; n++)
            {
                LegalGuessKeyList.Add(0x003f << (6 * n));
            }
        }

        /// <summary>
        /// 重置各种运行状态变量
        /// </summary>
        protected void Reset()
        {
            GuessStates = ShengXiaoGuessStates.NoMortgage;

            //清空竞猜记录
            lock (GuessItemListDict)
            {
                GuessItemListDict.Clear();
            }

            StateStartTicks = TimeUtil.NOW();

            IsBossKilled = false;

            ThisTimeCountDownSecs = 0;
        }

        #endregion 初始化

        #region 处理方法

        /// <summary>
        /// 调度和管理大乱斗场景
        /// </summary>
        public void Process()
        {
            //是否战斗中
            if (GuessStates > ShengXiaoGuessStates.NoMortgage)
            {
                //处理竞猜过程
                ProcessGuessing();
            }
            else
            {
                //处理无人下注的情形
                ProcessNoGuess();
            }
        }

        /// <summary>
        /// 处理竞猜过程
        /// </summary>
        protected void ProcessGuessing()
        {
            if (GuessStates == ShengXiaoGuessStates.MortgageCountDown) //等待别的玩家下注倒计时
            {
                //判断如果超过了最大等待时间， 在准备刷新boss
                long ticks = TimeUtil.NOW();
                if (ticks >= (StateStartTicks + (WaitingEnterSecs * 1000)))
                {
                    GuessStates = ShengXiaoGuessStates.BossCountDown;

                    //刷boss出来打
                    //重新刷新副本中的怪物
                    GameManager.MonsterZoneMgr.ReloadNormalMapMonsters(MapCode, 1);

                    StateStartTicks = TimeUtil.NOW();

                    ThisTimeCountDownSecs = WaitingKillBossSecs;

                    //发送广播消息, 开始击杀boss倒计时
                    GameManager.ClientMgr.NotifyAllShengXiaoGuessStateMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (int)GuessStates, (int)ThisTimeCountDownSecs, 0, GetPreGuessResult());
                }
            }
            else if (GuessStates == ShengXiaoGuessStates.BossCountDown)//击杀boss倒计时
            {
                //判断如果超过了最大等待时间， 且boss还没死，强行杀死boss
                if (WaitingKillBossSecs > 0)
                {
                    //long ticks = TimeUtil.NOW();
                    //if (ticks >= (StateStartTicks + (WaitingKillBossSecs * 1000)))
                    //{
                    //    //强行杀死boss

                    //    IsBossKilled = true;
                    //}
                }

                if (IsBossKilled)
                {
                    GuessStates = ShengXiaoGuessStates.EndKillBoss;
                }
            }
            else if (GuessStates == ShengXiaoGuessStates.EndKillBoss)
            {
                //生成随机生肖
                int resultShengXiaoMask = GenerateRandomShengXiao();

                AddGuessResultHistory(resultShengXiaoMask);

                //发送广播消息, 通知所有玩家竞猜结果
                GameManager.ClientMgr.NotifyAllShengXiaoGuessStateMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (int)GuessStates, resultShengXiaoMask, 0, GetPreGuessResult());

                //处理奖励
                ProcessAwards(resultShengXiaoMask);

                //更新为没有下注状态，等待第一个玩家下注
                GuessStates = ShengXiaoGuessStates.NoMortgage;

                //发送广播消息
                GameManager.ClientMgr.NotifyAllShengXiaoGuessStateMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (int)GuessStates, 0, 0, GetPreGuessResult());

                //重置各种信息
                Reset();
            }
        }

        /// <summary>
        /// 处理无人下注的情形
        /// </summary>
        protected void ProcessNoGuess()
        {
            //有人下注了，则修改竞猜状态
            if (GuessItemListDict.Count > 0)
            {
                //开始竞猜下注倒计时
                GuessStates = ShengXiaoGuessStates.MortgageCountDown;

                StateStartTicks = TimeUtil.NOW();

                ThisTimeCountDownSecs = WaitingEnterSecs;
                //通知客户端
                //发送广播消息, 开始击杀boss倒计时
                GameManager.ClientMgr.NotifyAllShengXiaoGuessStateMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (int)GuessStates, WaitingEnterSecs, 0, GetPreGuessResult());
            }
        }

        #endregion 处理方法

        #region 供外部访问的线程安全变量和方法

        /// <summary>
        /// 扣除需要的物品，统一扣除，避免多次操作
        /// </summary>
        /// <param name="client"></param>
        /// <param name="totalMortgageNum"></param>
        /// <param name="allowAutoBuy"></param>
        /// <returns></returns>
        public int SubNeedGoods(GameClient client, int totalMortgageNum, bool allowAutoBuy = false)
        {
            //扣除相应金币注码-----》注码是一种物品，不是金币，物品和金币有一定的比例关系
            //GameManager.ClientMgr.SubUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, mortgageNum);
            int needGoodsNum = totalMortgageNum;

            int needSubGoodsNum = needGoodsNum;
            int needBuyGoodsNum = 0;

            //判断背包内部物品是否足够 和 是否需要购买
            if (Global.GetTotalGoodsCountByID(client, NeedGoodsID) < needGoodsNum)
            {
                //物品不足，且允许自动购买，则需要购买物品
                if (allowAutoBuy)
                {
                    needBuyGoodsNum = Global.GMax(0, needGoodsNum - Global.GetTotalGoodsCountByID(client, NeedGoodsID));

                    needSubGoodsNum = needGoodsNum - needBuyGoodsNum;
                }
                else
                {
                    return -3998;
                }
            }

            //先扣除背包中剩余的
            if (needSubGoodsNum > 0)
            {
                bool usedBinding = false;
                bool usedTimeLimited = false;

                //从用户物品中扣除消耗的数量
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, NeedGoodsID, needSubGoodsNum, false, out usedBinding, out usedTimeLimited))
                {
                    return -4010;
                }
            }

            //看看是否有需要自动购买的物品
            if (needBuyGoodsNum > 0)
            {
                //自动购买
                int subMoney = 0;

                //自动扣除元宝购买昆仑镜
                subMoney = Global.SubUserMoneyForGoods(client, NeedGoodsID, needBuyGoodsNum, "生肖运程竞猜物品");

                if (subMoney <= 0)
                {
                    return subMoney;
                }
            }

            return 1;
        }

        /// <summary>
        /// 判断注码是否合法
        /// </summary>
        /// <param name="guessKey"></param>
        /// <param name="mortgageNum"></param>
        /// <returns></returns>
        public int IsMortgageLegal(int guessKey, int mortgageNum)
        {
            //如果在不下注时间内，则不能下注
            if (GuessStates > ShengXiaoGuessStates.MortgageCountDown)
            {
                return -3700;
            }

            //参数非法 12生肖最多12个bit, 0x0fff正好12个1
            if (guessKey <= 0 || guessKey > 0x0fff || mortgageNum <= 0)
            {
                return -3800;
            }

            //再次进行严格校对
            if (!LegalGuessKeyList.Contains(guessKey))
            {
                return -3990;
            }

            //单次注码不要太大
            if (mortgageNum > MaxMortgageForOnce)
            {
                return -3996;
            }

            return 1;
        }

        /// <summary>
        /// 添加竞猜选项, mortgageNum对应gold数量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="guessKey"></param>
        /// <param name="mortgageNum"></param>
        /// <returns></returns>
        public int AddGuess(GameClient client, int guessKey, int mortgageNum, bool allowAutoBuy = false)
        {
            //扣除相应注码，由于单个角色可能同时对几个竞猜关键字下注，每次扣除时由外部统一扣除就行，不要分批次扣除

            int ret = 1;

            int oldMortgage = 0;

            Dictionary<int, int> dict = null;

            lock (GuessItemListDict)
            {
                if (GuessItemListDict.TryGetValue(client.ClientData.RoleID, out dict) && null != dict)
                {
                    //是否已经下过该关键字注码
                    if (dict.TryGetValue(guessKey, out oldMortgage))
                    {
                        //已经下过注就直接累加
                        dict[guessKey] = oldMortgage + mortgageNum;
                    }
                    else
                    {
                        //记录下注信息,客户端下注信息由于数据包的连续性，这儿不会多个线程同时对单个客户端下注
                        dict.Add(guessKey, mortgageNum);
                    }
                }
                else
                {
                    dict = new Dictionary<int, int>();

                    //记录下注信息,客户端下注信息由于数据包的连续性，这儿不会多个线程同时对单个客户端下注
                    dict.Add(guessKey, mortgageNum);

                    GuessItemListDict.Add(client.ClientData.RoleID, dict);
                }
            }

            GameManager.SystemServerEvents.AddEvent(string.Format("扣除角色竞猜注码金币, roleID={0}({1}), Money={2}, newMoney={3}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Gold, mortgageNum), EventLevels.Record);

            return ret;
        }

        /// <summary>
        /// boss被杀死了
        /// </summary>
        public void OnBossKilled()
        {
            //判断一下本地图的boss是否被杀死
            if (GuessStates == ShengXiaoGuessStates.BossCountDown)
            {
                //boss已经死亡
                IsBossKilled = true;

                //这儿不要修改GuessStates的状态，多个线程同时修改，可能带来意想不到的结果，统一由循环线程控制
            }
        }

        /// <summary>
        /// 客户端进入地图
        /// </summary>
        /// <returns></returns>
        public bool ClientEnter(GameClient gameClient)
        {
            //线路不对，直接返回
            if (LegalServerLines.IndexOf(GameManager.ServerLineID) < 0)
            {
                return false;
            }

            bool ret = false;

            //通知竞猜状态,处于等待第一个玩家下注，下注倒计时，boss击杀倒计时才通知， boss被杀死不用通知，那个状态很短暂

            if (GuessStates < ShengXiaoGuessStates.EndKillBoss)
            {
                //判断如果超过了最大等待时间， 在准备刷新boss
                long ticks = TimeUtil.NOW();

                //当前状态有倒计时，就传递剩余时间，没有倒计时，就传递已过时间
                long theTicks = ticks - StateStartTicks;
                if (ThisTimeCountDownSecs > 0)
                {
                    theTicks = ThisTimeCountDownSecs * 1000 - theTicks;
                }

                //有倒计时且剩下1200毫秒,或者当前没有倒计时状态 时 通知,否则通知了也没意义
                if (theTicks >= 1200 || ThisTimeCountDownSecs <= 0)
                {
                    GameManager.ClientMgr.NotifyClientShengXiaoGuessStateMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, gameClient, (int)GuessStates, (int)(theTicks/1000), 0, GetPreGuessResult());
                }
            }

            return ret;
        }

        /// <summary>
        /// 客户端离开地图
        /// </summary>
        /// <returns></returns>
        public void ClientLeave()
        {
            //什么也不做
        }

        /// <summary>
        /// 返回合法的竞猜线路
        /// </summary>
        /// <returns></returns>
        public  List<int> GetLegalGuessServerLines()
        {
            return LegalServerLines;
        }

        /// <summary>
        /// 返回角色的竞猜词典
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public Dictionary<int, int> GetRoleGuessDictionay(int roleID)
        {
            Dictionary<int, int> dict = null;

            lock (GuessItemListDict)
            {
                if (GuessItemListDict.TryGetValue(roleID, out dict) && null != dict)
                {
                    return dict;
                }
            }

            return new Dictionary<int,int>();
        }

        #endregion 供外部访问的线程安全变量和方法

        #region 奖励处理

        /// <summary>
        /// 处理竞猜奖励
        /// </summary>
        public void ProcessAwards(int resultShengXiaoMask)
        {
            //奖励倍数
            int nAwardMulitple = 0;

            //奖励结果
            int nAwardNumber = 0;

            //单个角色竞猜结果字符串， 用于通知客户端自己
            string roleGuessResult = "";

            //多个角色的竞猜结果字符串 用于通知gamedbserver
            string batchGuessResult = "";

            List<int> lsRoleID = GuessItemListDict.Keys.ToList<int>();

            try
            {
                //遍历下注玩家，并给予奖励
                foreach (var roleID in lsRoleID)
                {
                    roleGuessResult = "";

                    //角色只要下注了，不在线也给奖励
                    GameClient otherClient = GameManager.ClientMgr.FindClient(roleID);

                    Dictionary<int, int> dict = null;

                    if (!GuessItemListDict.TryGetValue(roleID, out dict) || null == dict)
                    {
                        continue;
                    }

                    //遍历注码列表
                    foreach (var item in dict)
                    {
                        if (roleGuessResult.Length > 0)
                        {
                            roleGuessResult += "|";
                        }

                        //判断是否猜中
                        if ((item.Key & resultShengXiaoMask) <= 0)
                        {
                            //没有猜中
                            roleGuessResult += string.Format("{0}_{1}_{2}_{3}_{4}", item.Key, item.Value, resultShengXiaoMask, 0, null != otherClient ? otherClient.ClientData.Gold : -1);

                            //记录到统计数据库
                            Global.AddShengXiaoGuessHistoryToStaticsDB(otherClient, roleID, item.Key, item.Value, resultShengXiaoMask, 0, null != otherClient ? otherClient.ClientData.Gold : -1);
                            continue;
                        }

                        //猜中了
                        nAwardMulitple = GetMultipleByGuessKey(item.Key);

                        //这种情况应该不会发生
                        if (nAwardMulitple <= 0)
                        {
                            continue;
                        }

                        //计算赢得的物品数量
                        nAwardNumber = nAwardMulitple * item.Value;

                        //将奖励的物品数量转换为对应的金币
                        int nAwardGold = nAwardNumber * SingleGoodsToYuanBaoNum;

                        //这儿也不应该发生
                        if (nAwardGold <= 0)
                        {
                            continue;
                        }

                        //给予相应奖励
                        GameManager.ClientMgr.AddUserGoldOffLine(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, roleID, nAwardGold, "角色竞猜", "" + roleID);

                        if (null != otherClient)
                        {
                            GameManager.SystemServerEvents.AddEvent(string.Format("角色竞猜获取金币, roleID={0}({1}), Money={2}, newMoney={3}", otherClient.ClientData.RoleID, otherClient.ClientData.RoleName, otherClient.ClientData.Gold, nAwardGold), EventLevels.Record);

                            //系统广播
                            if (nAwardGold >= GateGoldForBroadcast)
                            {
                                Global.BroadcastShengXiaoGuessWinHint(otherClient, nAwardMulitple, Global.GetShengXiaoNameByCode(resultShengXiaoMask), nAwardGold);
                            }
                        }
                        else
                        {
                            GameManager.SystemServerEvents.AddEvent(string.Format("角色竞猜获取金币, roleID={0}({1}), Money={2}, newMoney={3}", roleID, "离线角色", "未知", nAwardGold), EventLevels.Record);
                        }

                        //记录通知命令
                        roleGuessResult += string.Format("{0}_{1}_{2}_{3}_{4}", item.Key, item.Value, resultShengXiaoMask, nAwardGold,
                            otherClient != null ? otherClient.ClientData.Gold : -1);

                        //记录到统计数据库
                        Global.AddShengXiaoGuessHistoryToStaticsDB(otherClient, roleID, item.Key, item.Value, resultShengXiaoMask, nAwardGold, otherClient != null ? otherClient.ClientData.Gold : -1);
                    }

                    if (null != otherClient)
                    {
                        //通知这个客户端竞猜结果
                        GameManager.ClientMgr.NotifyShengXiaoGuessResultMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, otherClient, roleGuessResult);
                    }

                    if (roleGuessResult.Length > 0)
                    {
                        if (batchGuessResult.Length > 0)
                        {
                            batchGuessResult += ";";//多个角色的数据用分号隔开
                        }

                        batchGuessResult += string.Format("{0},{1}", roleID, roleGuessResult);//单个角色的信息用逗号隔开角色id 和 其他竞猜项
                    }

                    //通知gamedbserver
                    if (batchGuessResult.Length > 1200)
                    {
                        //通知gamedbserver记录竞猜历史 每个角色的通知一次,这儿应该综合考虑一下数据包大小问题，每次不要太大，也不要太小，保证次数不要太多
                        GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDSHENGXIAOGUESSHIST,
                        string.Format("{0}", batchGuessResult),
                        null, GameManager.LocalServerId);

                        batchGuessResult = "";
                    }
                }

                //通知gamedbserver
                if (batchGuessResult.Length > 0)
                {
                    //通知gamedbserver记录竞猜历史 每个角色的通知一次,这儿应该综合考虑一下数据包大小问题，每次不要太大，也不要太小，保证次数不要太多
                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDSHENGXIAOGUESSHIST,
                    string.Format("{0}", batchGuessResult),
                    null, GameManager.LocalServerIdForNotImplement);
                }
            }
            catch
            {
            }

            //清空竞猜记录
            lock (GuessItemListDict)
            {
                GuessItemListDict.Clear();
            }
        }

        /// <summary>
        /// 生成随机生肖
        /// </summary>
        /// <returns></returns>
        public int GenerateRandomShengXiao()
        {
            int nRandomShengXiaoIndex = Global.GetRandomNumber(0, 12);

            return 0x0001 << nRandomShengXiaoIndex;
        }

        /// <summary>
        /// 通过竞猜关键字查询竞猜倍数，用12除以竞猜关键字二进制bit中1的个数，再取下限整数，就是倍数
        /// </summary>
        /// <param name="guessKey"></param>
        /// <returns></returns>
        protected int GetMultipleByGuessKey(int guessKey)
        {
            int value = 0;

            //不用锁，肯定只在一个线程调用
            if (AwardMultipleDict.TryGetValue(guessKey, out value))
            {
                return value;
            }

            if (guessKey <= 0)
            {
                return -3000;
            }

            int nOneCount = 0;

            //12生肖，最多12个1
            for (int n = 0; n < 12; n++)
            {
                nOneCount += (guessKey >> n) & 0x01;
            }

            //大于12是不可能的
            if (nOneCount <= 0 || nOneCount > 12)
            {
                return -3001;
            }

            int nMultiple = 12 / nOneCount;

            //简单的清空缓存表，避免恶意guessKey数据导致缓存增加
            if (AwardMultipleDict.Count > 50)
            {
                AwardMultipleDict.Clear();
            }

            AwardMultipleDict.Add(guessKey, nMultiple);

            return nMultiple;

        }
        #endregion 奖励处理

        #region 竞猜历史

        /// <summary>
        /// 添加一个竞猜结果
        /// </summary>
        /// <param name="result"></param>
        protected void AddGuessResultHistory(int result)
        {
            lock (ShengXiaoGuessResultHistory)
            {
                if (ShengXiaoGuessResultHistory.Count > 10)
                {
                    ShengXiaoGuessResultHistory.RemoveAt(0);
                }

                ShengXiaoGuessResultHistory.Add(result);
            }
        }

        /// <summary>
        /// 返回竞猜结果字符串，最近的10条
        /// </summary>
        /// <returns></returns>
        public string GetGuessResultHistory()
        {
            string results = "";

            lock (ShengXiaoGuessResultHistory)
            {
                foreach (var item in ShengXiaoGuessResultHistory)
                {
                    if (results.Length > 0)
                    {
                        results += "|";
                    }

                    results += string.Format("{0}", item);
                }
            }

            return results;
        }

        /// <summary>
        /// 返回生一次的竞猜结果
        /// </summary>
        /// <returns></returns>
        private int GetPreGuessResult()
        {
            lock (ShengXiaoGuessResultHistory)
            {
                if (ShengXiaoGuessResultHistory.Count > 0)
                {
                    return ShengXiaoGuessResultHistory[ShengXiaoGuessResultHistory.Count -1];
                }
            }

            return 0;
        }

        #endregion 竞猜历史
    }
}
