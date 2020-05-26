using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Server;
using Server.Tools;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using Tmsk.Contract;

namespace GameServer.Logic
{
    public enum MarryFubenResult
    {
        Error = -1,     //非常规出错
        Success = 0,       // 成功
        ResultRoomInfo = 1, //返回房间信息 + 4参数 (int)nHusband_ID, (sbyte)nHusband_state, (int)nWife_ID, (sbyte)nWife_state
        NotMarriaged = 2,   //未结婚
        InFuben = 3,       //已经在副本内
        SelfOrOtherLimit = 4, //自己或配偶完成次数已满
        IsReaday = 5,     //已经在准备状态
        NotOpen = 6,        //结婚系统没有开启
    }

    //[bing] 情侣副本实例 相当于CopyMap的外延属性
    class MarriageInstance
    {
        /// <summary>
        /// 创建者RoleID
        /// 创建者如果退出了不交换roleid 因为dic内的key值不变 这里只是当key用
        /// </summary>
        public int nCreateRole_ID = -1;

        /// <summary>
        /// 丈夫RoleID
        /// </summary>
        public int nHusband_ID = -1;

        /// <summary>
        /// 丈夫准备状态-1 未进入房间 0 = 未准备 1 = 准备中 2 = 进行中3秒倒计时
        /// </summary>
        public int nHusband_state = -1;

        /// <summary>
        /// 妻子RoleID
        /// </summary>
        public int nWife_ID = -1;

        /// <summary>
        /// 妻子准备状态-1 未进入房间  0 = 未准备 1 = 准备中 2 = 进行中3秒倒计时
        /// </summary>
        public int nWife_state = -1;
        /// <summary>
        /// 副本ID
        /// </summary>
        public int nHusband_FuBenID = 0;
        /// <summary>
        /// 副本ID
        /// </summary>
        public int nWife_FuBenID = 0;
    }


    //[bing] 情侣副本管理器
    class MarryFuBenMgr : IManager, ICmdProcessorEx, IEventListener
    {
        /// <summary>
        /// 静态实例
        /// </summary>
        private static MarryFuBenMgr instance = new MarryFuBenMgr();
        public static MarryFuBenMgr getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 情侣副本缓存
        /// </summary>
        private Dictionary<int, MarriageInstance> MarriageInstanceDic = new Dictionary<int, MarriageInstance>();

        #region 目前只有一个副本 先这样处理 以后多个改用dic

        /// <summary>
        /// 情侣副本数据
        /// </summary>
        private SystemXmlItem MarriageFubenXmlItem = null;

        /// <summary>
        /// 情侣副本怪物额外数据列表
        /// </summary>
        private SystemXmlItems ManAndWifeBossXmlItems = new SystemXmlItems();

        //         /// <summary>
        //         /// 情侣副本ID
        //         /// </summary>
        //         public static readonly int MarriageFuBenId = 50000;
        // 
        //         /// <summary>
        //         /// 情侣副本地图ID
        //         /// </summary>
        //         public static readonly int nMarriageFubenMapCode = 50000;

        #endregion

        /// <summary>
        /// 初始化管理器
        /// </summary>
        public bool initialize()
        {
            //初始化MarragieLogic管理器
            MarriageOtherLogic.getInstance().init();

            //             //初始化情侣副本静态资料
            //             if (false == GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(MarriageFuBenId, out MarriageFubenXmlItem)
            //                 || null == MarriageFubenXmlItem)
            //             {
            //                 return false;
            //             }

            ManAndWifeBossXmlItems.LoadFromXMlFile("Config/ManAndWifeBoss.xml", "", "MonsterID");

            //初始化协议列表
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_MARRY_FUBEN, 1, 2, getInstance());

            //初始化消息监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterBlooadChanged, getInstance());   //怪物血量改变监听器
            //GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, getInstance());   //怪物死亡监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerLogout, getInstance());  //玩家登出事件

            return true;
        }

        public bool startup()
        {
            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            //移除监听器
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterBlooadChanged, getInstance());
            //GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, getInstance());
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerLogout, getInstance());

            //清空缓存列表
            if (null != MarriageInstanceDic)
            {
                lock (MarriageInstanceDic)
                {
                    MarriageInstanceDic.Clear();
                }
            }

            MarriageOtherLogic.getInstance().destroy();

            return true;
        }

        //         /// <summary>
        //         /// 根据副本ID判断是否为情侣副本
        //         /// </summary>
        //         public static bool IsMarriageFuben(int nFubenID)
        //         {
        //             if (nFubenID == MarriageFuBenId)
        //                 return true;
        // 
        //             return false;
        //         }

        public void processEvent(EventObject eventObject)
        {
          /*  if (eventObject.getEventType() == (int)EventTypes.MonsterBlooadChanged)
            {
                //被攻击事件
                Monster monster = (eventObject as MonsterBlooadChangedEventObject).getMonster();
                GameClient client = (eventObject as MonsterBlooadChangedEventObject).getGameClient();

                //可能是怪物自己在加血
                if (null == monster
                    || null == client)
                    return;

                //看看该怪物是不是属于情侣副本
                if (client.ClientData.CopyMapID > 0 && client.ClientData.FuBenSeqID > 0
                    //                     && client.ClientData.MapCode == nMarriageFubenMapCode
                    //                     && monster.CurrentMapCode == nMarriageFubenMapCode
                    && MapTypes.MarriageCopy == Global.GetMapType(client.ClientData.MapCode)
                    && MapTypes.MarriageCopy == Global.GetMapType(monster.CurrentMapCode)
                    )
                {
                    //根据ManAndWifeBoss.xml配置 看看该怪物由谁攻击 如果被错误的人攻击会给怪物添加一个永久的减伤buff
                    SystemXmlItem XMLItem = null;
                    if (false == ManAndWifeBossXmlItems.SystemXmlItemDict.TryGetValue(monster.MonsterInfo.ExtensionID, out XMLItem)
                        || null == XMLItem)
                        return;

                    //攻击条件不相符 给怪物加buff
                    if (XMLItem.GetIntValue("Need") != client.ClientData.MyMarriageData.byMarrytype)
                    {
                        BufferData bufferData = Global.GetMonsterBufferDataByID(monster, XMLItem.GetIntValue("GoodsID"));
                        if (null == bufferData || true == Global.IsBufferDataOver(bufferData))
                        {
                            double[] newActionParams = new double[2];
                            newActionParams[0] = 15.0d; //buffer 增加15分钟相当于到副本结束
                            newActionParams[1] = 1.0d;
                            EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem((int)BufferItemTypes.MU_MARRIAGE_SUBDAMAGEPERCENTTIMER);
                            if (null != item)
                            {
                                newActionParams[1] = item.ExtProps[24];
                            }

                            //更新BufferDat
                            Global.UpdateMonsterBufferData(monster, BufferItemTypes.MU_MARRIAGE_SUBDAMAGEPERCENTTIMER, newActionParams);
                            string text = string.Format(Global.GetLang("【{0}】攻击了【{1}】，导致【{1}】防御力大幅度提升"), client.ClientData.RoleName, monster.MonsterInfo.VSName);
                            GameManager.ClientMgr.BroadSpecialHintText(monster.CurrentMapCode, monster.CurrentCopyMapID, text);
                        }
                    }
                }
            }
            else if (eventObject.getEventType() == (int)EventTypes.PlayerLogout)
            {
                //[bing] 玩家登出或掉线后要清掉该房间 回收房间占用的内存
                GameClient client = (eventObject as PlayerLogoutEventObject).getPlayer();
 
                //玩家登出要清理掉房间
                ClientExitRoom(client);
            }*/
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_MARRY_FUBEN:
                    {
                        int nSelect = 0;
                        try
                        {
                            nSelect = Global.SafeConvertToInt32(cmdParams[0]);
                        }
                        catch (Exception ex) //解析错误
                        {
                            DataHelper.WriteFormatExceptionLog(ex, "ProcessMarryFuben", false);
                        }

                        int[] iRet = null;

                        if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                        {
                            iRet = new int[] { (int)MarryFubenResult.NotOpen };
                            client.sendCmd(nID, iRet);
                            break;
                        }

                        //1 = 获取副本状态, 2 = 进入房间, 3 = 离开房间, 4 = 准备， 5 = 离开准备
                        if (1 == nSelect)
                            iRet = new int[] { (int)GetMarriageInstanceState(client) };
                        else if (2 == nSelect)
                            iRet = new int[] { (int)ClientEnterRoom(client) };
                        else if (3 == nSelect)
                            iRet = new int[] { (int)ClientExitRoom(client) };
                        else if (4 == nSelect)
                            iRet = new int[] { (int)ClientReady(client, Global.SafeConvertToInt32(cmdParams[1])) };
                        else if (5 == nSelect)
                            iRet = new int[] { (int)ClientExitReady(client) };

                        client.sendCmd(nID, iRet);
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// 得到情侣副本
        /// </summary>
        private MarriageInstance GetMarriageInstance(GameClient client)
        {
            if (null == client)
                return null;

            //找一下自己是否创建过Instance
            lock (MarriageInstanceDic)
            {
                MarriageInstance FubenInstance = null;
                MarriageInstanceDic.TryGetValue(client.ClientData.RoleID, out FubenInstance);
                if (null == FubenInstance)
                {
                    //没找到找一下伴侣是否创建过Instance
                    MarriageInstanceDic.TryGetValue(client.ClientData.MyMarriageData.nSpouseID, out FubenInstance);
                }

                return FubenInstance;
            }
        }

        /// <summary>
        /// 得到当前副本状态
        /// </summary>
        private MarryFubenResult GetMarriageInstanceState(GameClient client, MarriageInstance FubenInstance = null)
        {
            if (null == client)
                return MarryFubenResult.Error;

            int[] RetArry = new int[6];
            string tcpstring = "";

            if (null == FubenInstance)
            {
                FubenInstance = GetMarriageInstance(client);
            }

            if (null != FubenInstance)
            {
                RetArry = new int[]{ FubenInstance.nHusband_ID, FubenInstance.nHusband_state, FubenInstance.nWife_ID, FubenInstance.nWife_state,
                    FubenInstance.nHusband_FuBenID,FubenInstance.nWife_FuBenID};
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_MARRY_FUBEN, RetArry);
                return MarryFubenResult.Success;
            }
            RetArry = new int[] { -1, 0, -1, 0, 0, 0 };
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_MARRY_FUBEN, RetArry);

            return MarryFubenResult.Success;
        }

        /// <summary>
        /// 玩家进入房间
        /// </summary>
        public MarryFubenResult ClientEnterRoom(GameClient client)
        {
            //判断下是否为夫妻
            if (-1 == client.ClientData.MyMarriageData.byMarrytype)
                return MarryFubenResult.NotMarriaged;

            //如果在副本里应该不能进入房间
            if (client.ClientData.CopyMapID > 0 && client.ClientData.FuBenSeqID > 0)
                return MarryFubenResult.InFuben;

            //古战场和水晶幻境不是副本但也应该算副本类
            SceneUIClasses sceneType = Global.GetMapSceneType(client.ClientData.MapCode);
            if (sceneType == SceneUIClasses.ShuiJingHuanJing
                || sceneType == SceneUIClasses.GuZhanChang)
                return MarryFubenResult.InFuben;

            //取出情侣
            GameClient Spouseclient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);

            //取一下情侣副本
            MarriageInstance FubenInstance = GetMarriageInstance(client);

            //没创建过副本用自己的roleid创建一个
            lock (MarriageInstanceDic)
            {
                if (null == FubenInstance)
                {
                    FubenInstance = new MarriageInstance();
                    FubenInstance.nCreateRole_ID = client.ClientData.RoleID;

                    //如果是丈夫
                    if (1 == client.ClientData.MyMarriageData.byMarrytype)
                    {
                        FubenInstance.nHusband_ID = client.ClientData.RoleID;
                        FubenInstance.nHusband_state = 0;
                    }

                    //如果是妻子
                    else if (2 == client.ClientData.MyMarriageData.byMarrytype)
                    {
                        FubenInstance.nWife_ID = client.ClientData.RoleID;
                        FubenInstance.nWife_state = 0;
                    }

                    //加入缓存
                    MarriageInstanceDic.Add(FubenInstance.nCreateRole_ID, FubenInstance);

                    //给主客户端发送副本信息
                    GetMarriageInstanceState(client, FubenInstance);

                    //给配偶客户端发送副本信息
                    GetMarriageInstanceState(Spouseclient, FubenInstance);
                    return MarryFubenResult.Success;
                }
                else
                {
                    //如果是丈夫
                    if (1 == client.ClientData.MyMarriageData.byMarrytype)
                    {
                        FubenInstance.nHusband_ID = client.ClientData.RoleID;
                        FubenInstance.nHusband_FuBenID = 0;
                        FubenInstance.nHusband_state = 0;
                    }

                    //如果是妻子
                    else if (2 == client.ClientData.MyMarriageData.byMarrytype)
                    {
                        FubenInstance.nWife_ID = client.ClientData.RoleID;
                        FubenInstance.nWife_FuBenID = 0;
                        FubenInstance.nWife_state = 0;
                    }

                    //给主客户端发送副本信息
                    GetMarriageInstanceState(client, FubenInstance);

                    //给配偶客户端发送副本信息
                    GetMarriageInstanceState(Spouseclient, FubenInstance);
                    return MarryFubenResult.Success;
                }
            }
        }

        /// <summary>
        /// 玩家退出房间
        /// </summary>
        public MarryFubenResult ClientExitRoom(GameClient client)
        {
            //判断下是否为夫妻
            if (-1 == client.ClientData.MyMarriageData.byMarrytype)
                return MarryFubenResult.NotMarriaged;

            //取出情侣
            GameClient Spouseclient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);

            //取一下情侣副本
            MarriageInstance FubenInstance = GetMarriageInstance(client);

            if (null != FubenInstance)
            {
                //如果是丈夫
                if (1 == client.ClientData.MyMarriageData.byMarrytype) 
                {
                    FubenInstance.nHusband_ID = -1;
                    FubenInstance.nHusband_state = -1;
                    FubenInstance.nHusband_FuBenID = 0;
                }
                else
                {
                    FubenInstance.nWife_ID = -1;    //如果是妻子
                    FubenInstance.nWife_state = -1;
                    FubenInstance.nWife_FuBenID = 0;

                }

                //如果双方都已经退出了清掉缓存
                if (-1 == FubenInstance.nHusband_ID
                    && -1 == FubenInstance.nWife_ID)
                {
                    RemoveMarriageInstance(FubenInstance, false);
                    FubenInstance = null;
                }

                //给主客户端发送副本信息
                GetMarriageInstanceState(client, FubenInstance);

                //给配偶客户端发送副本信息
                GetMarriageInstanceState(Spouseclient, FubenInstance);
                return MarryFubenResult.Success;
            }

            return MarryFubenResult.Error;
        }

        /// <summary>
        /// 玩家准备开始副本
        /// </summary>
        public MarryFubenResult ClientReady(GameClient client, int FuBenID)
        {
            //判断下是否为夫妻
            if (-1 == client.ClientData.MyMarriageData.byMarrytype)
                return MarryFubenResult.NotMarriaged;

            //如果在副本里应该不能再准备
            if (client.ClientData.CopyMapID > 0 && client.ClientData.FuBenSeqID > 0)
                return MarryFubenResult.InFuben;

            //古战场和水晶幻境不是副本但也应该算副本类
            SceneUIClasses sceneType = Global.GetMapSceneType(client.ClientData.MapCode);
            if (sceneType == SceneUIClasses.ShuiJingHuanJing
                || sceneType == SceneUIClasses.GuZhanChang)
                return MarryFubenResult.InFuben;

            //取出情侣
            GameClient Spouseclient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);

            //             //如果情侣不在线应该是不让准备进副本的
            //             bool bCanEnter = Spouseclient == null ? false : true;
            //             
            //             //判断一下自己是不是今天次数已满
            //             if(true == bCanEnter)
            //             {
            //                 FuBenData tmpfubdata = Global.GetFuBenData(client, FuBenID);
            //                 int nFinishNum;
            //                 int haveEnterNum = Global.GetFuBenEnterNum(tmpfubdata, out nFinishNum);
            //                 if (nFinishNum >= 5)    //todo...
            //                 {
            //                     bCanEnter = false;
            //                 }
            //             }

            //             //再检查一下配偶的进入次数是否已满 mark: 如果配偶不在线应该让其不能准备
            //             if (true == bCanEnter)
            //             {
            //                 FuBenData tmpfubdata = Global.GetFuBenData(client, FuBenID);
            //                 int nFinishNum;
            //                 int haveEnterNum = Global.GetFuBenEnterNum(tmpfubdata, out nFinishNum);
            //                 if (nFinishNum >= 1)    //todo...
            //                 {
            //                     bCanEnter = false;
            //                 }
            //             }
            // 
            //             //次数满了不能进
            //             if (false == bCanEnter)
            //                 return MarryFubenResult.SelfOrOtherLimit;

            //取一下情侣副本
            MarriageInstance FubenInstance = GetMarriageInstance(client);

            //进入房间就创建instance 不应该会null
            if (null == FubenInstance)
                return MarryFubenResult.Error;

            //都已经进入倒计时阶段了不应该再准备了
            if (1 == FubenInstance.nHusband_state && 1 == FubenInstance.nWife_state
                && FubenInstance.nHusband_FuBenID == FubenInstance.nWife_FuBenID)
                return MarryFubenResult.IsReaday;

            //如果是丈夫
            if (1 == client.ClientData.MyMarriageData.byMarrytype)
            {
                FubenInstance.nHusband_state = 1;
                FubenInstance.nHusband_FuBenID = FuBenID;

            }

            //如果是妻子
            else if (2 == client.ClientData.MyMarriageData.byMarrytype)
            {
                FubenInstance.nWife_state = 1;
                FubenInstance.nWife_FuBenID = FuBenID;
            }

            //现在改用客户端去判断当2人状态都OK的时候由客户端发送组队副本消息进入了

            //当双方都准备好时就进入3秒倒计时准备 由客户端倒计时 和多人本类似
            //if (1 == FubenInstance.nHusband_state
            //    && 1 == FubenInstance.nWife_state)
            //{
                //从DBServer获取副本顺序ID
            //    string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GETFUBENSEQID, string.Format("{0}", client.ClientData.RoleID), client.ServerId);
            //    if (null == dbFields || dbFields.Length < 2)
            //    {
                    //取失败了 回滚状态
            //        FubenInstance.nHusband_state = 0;
            //        FubenInstance.nWife_state = 0;
            //        FubenInstance.nHusband_FuBenID = 0;
            //        FubenInstance.nWife_FuBenID = 0;

                    //给主客户端发送副本信息
            //        GetMarriageInstanceState(client, FubenInstance);

                    //给配偶客户端发送副本信息
            //        GetMarriageInstanceState(Spouseclient, FubenInstance);

            //        return MarryFubenResult.Error;
            //    }

//                 FubenInstance.nHusband_state = 2;//进入时判断2个为1
//                 FubenInstance.nWife_state = 2;
// 
//                 int fuBenSeqID = Global.SafeConvertToInt32(dbFields[1]);
// 
//                 //通知组队副本进入的消息
//                 GameManager.ClientMgr.NotifyTeamMemberFuBenEnterMsg(client, FubenInstance.nCreateRole_ID, FuBenID, fuBenSeqID);//应该由客户端发
// 
//                 //通知组队副本进入的消息
//                 GameManager.ClientMgr.NotifyTeamMemberFuBenEnterMsg(Spouseclient, FubenInstance.nCreateRole_ID, FuBenID, fuBenSeqID);
            //}

            //给主客户端发送副本信息
            GetMarriageInstanceState(client, FubenInstance);

            //给配偶客户端发送副本信息
            GetMarriageInstanceState(Spouseclient, FubenInstance);

            return MarryFubenResult.Success;
        }

        /// <summary>
        /// 玩家退出副本
        /// </summary>
        public MarryFubenResult ClientExitReady(GameClient client)
        {
            //判断下是否为夫妻
            if (-1 == client.ClientData.MyMarriageData.byMarrytype)
                return MarryFubenResult.NotMarriaged;

            //取出情侣
            GameClient Spouseclient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);

            //取一下情侣副本
            MarriageInstance FubenInstance = GetMarriageInstance(client);

            if (null != FubenInstance)
            {
                //都已经进入倒计时阶段了不应该再退出准备了
                if (1 == FubenInstance.nHusband_state
                    && 1 == FubenInstance.nWife_state
                    && FubenInstance.nHusband_FuBenID == FubenInstance.nWife_FuBenID)
                    return MarryFubenResult.IsReaday;

                //如果是丈夫
                if (1 == client.ClientData.MyMarriageData.byMarrytype)
                {
                    FubenInstance.nHusband_state = 0;
                    FubenInstance.nHusband_FuBenID = 0;
                }
                else
                {
                    FubenInstance.nWife_state = 0;    //如果是妻子
                    FubenInstance.nWife_FuBenID = 0;
                }

                //给主客户端发送副本信息
                GetMarriageInstanceState(client, FubenInstance);

                //给配偶客户端发送副本信息
                GetMarriageInstanceState(Spouseclient, FubenInstance);
                return MarryFubenResult.Success;
            }

            return MarryFubenResult.Error;
        }

        /// <summary>
        /// 5秒倒计时后启动副本 这里为玩家进入副本 应该从房间内把他拿出来 以后可能保留房间信息
        /// </summary>
        public void StartInstance(GameClient client)
        {
            ClientExitRoom(client);
        }

        /// <summary>
        /// 把情侣副本从缓存列表移除
        /// 有可能会给客户端发送通知
        /// </summary>
        private void RemoveMarriageInstance(MarriageInstance FubenInstance, bool bNeedsendtoclient = false)
        {
            lock (MarriageInstanceDic)
            {
                if (true == bNeedsendtoclient)
                {
                    //给双方发送房间删除消息
                    GameClient Husbandclient = GameManager.ClientMgr.FindClient(FubenInstance.nHusband_ID);
                    GetMarriageInstanceState(Husbandclient, FubenInstance);
                    GameClient Wifeclient = GameManager.ClientMgr.FindClient(FubenInstance.nWife_ID);
                    GetMarriageInstanceState(Wifeclient, FubenInstance);
                }

                MarriageInstanceDic.Remove(FubenInstance.nCreateRole_ID);
            }
        }

        #region 数据库操作

        public static bool UpdateMarriageData2DB(GameClient client)
        {
            return UpdateMarriageData2DB(client.ClientData.RoleID, client.ClientData.MyMarriageData, client);
        }

        public static bool UpdateMarriageData2DB(int nRoleID, MarriageData updateMarriageData, GameClient self)
        {
            byte[] dataBytes = DataHelper.ObjectToBytes<MarriageData>(updateMarriageData);
            byte[] byRoleID = BitConverter.GetBytes(nRoleID);
            byte[] sendBytes = new byte[dataBytes.Length + 4];
            Array.Copy(byRoleID, sendBytes, 4);
            Array.Copy(dataBytes, 0, sendBytes, 4, dataBytes.Length);

            return Global.sendToDB<bool, byte[]>((int)TCPGameServerCmds.CMD_DB_UPDATE_MARRY_DATA, sendBytes, self.ServerId);
        }

        #endregion
        public  bool CanEnterSceneEX(GameClient client)
        {
            //取一下情侣副本
            MarriageInstance FubenInstance = GetMarriageInstance(client);
            GameClient clienth = GameManager.ClientMgr.FindClient(FubenInstance.nHusband_ID);
            if (clienth == null)
            {
                return false;
            }
            GameClient clientw = GameManager.ClientMgr.FindClient(FubenInstance.nWife_ID);
            if (clientw == null)
            {
                return false;
            }
           if (1 != FubenInstance.nHusband_state
                  || 1 != FubenInstance.nWife_state)
            {
                RemoveMarriageInstance(FubenInstance, true);
                FubenInstance = null;
                return false;
            }
            return true;
        }
        public MarriageInstance GetMarriageInstanceEX(GameClient client)
        {
            return GetMarriageInstance(client);
        }
      
    }
}
