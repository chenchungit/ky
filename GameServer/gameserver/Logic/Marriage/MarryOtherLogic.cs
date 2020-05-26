using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using GameServer.Server;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using Server.Data;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Core.Executor;


namespace GameServer.Logic
{
    public enum MarryOtherResult
    {
        Error = -1,     //非常规出错
        Success = 0,       // 成功
        NotMarriaged = 1,   //未结婚
        NotFindItem = 2,    //献花未找到物品
        ItemNotRose = 3,    //物品不是花
        NeedGam = 4,        //绑定钻石不足
        NeedRose = 5,       //花不足
        MessageLimit = 6,   //爱情宣言超过最大64字长度
        NotRing = 7,        //更换物品不是戒指
        CirEffect = 8,      //献花暴击
        NotNexRise = 9,     //不是下一等级戒指
        MaxLimit = 10,      //阶级和星级已达到上限
        NotOpen = 11,       //功能未开启
    }

    //送花数据
    class MarriageRoseData
    {
        public int nBaseAddGoodWill = 0;      //增加奉献值的基值

        public List<int> modulusList = new List<int>();       //增加奉献值系数

        public List<int> rateList = new List<int>();    //增加几率
    }

    class MarriageOtherLogic : ICmdProcessorEx, IEventListener
    {
        /// <summary>
        /// 静态实例
        /// </summary>
        private static MarriageOtherLogic instance = new MarriageOtherLogic();
        public static MarriageOtherLogic getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 送花数据dic
        /// </summary>
        private Dictionary<int, MarriageRoseData> RoseDataDic = new Dictionary<int, MarriageRoseData>();

        /// <summary>
        /// 奉献值升级表 old
        /// </summary>
        private Dictionary<sbyte, Dictionary<sbyte, int>> GoodwillLvDic = new Dictionary<sbyte, Dictionary<sbyte, int>>();

        /// <summary>
        /// 奉献值升级表 new 加载时预计算升级经验
        /// </summary>
        private List<int> GoodwillAllExpList = new List<int>();

        /// <summary>
        /// 婚戒表
        /// </summary>
        public SystemXmlItems WeddingRingDic = new SystemXmlItems();

        /// <summary>
        /// 奉献值升级最大星级
        /// </summary>
        private sbyte byMaxGoodwillStar = 0;

        /// <summary>
        /// 奉献值升级最大星级
        /// </summary>
        private sbyte byMaxGoodwillLv = 0;

        /// <summary>
        /// 缓存献花需要的钻石
        /// </summary>
        private double[] dNeedGam;

        //婚戒最终属性系数参数
        private double dRingmodulus = 0.0d;

        //对方婚戒属性倍率参数
        private double dOtherRingmodulus = 0.0d;

        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void init()
        {
            //缓存献花需要的钻石
            try
            {
                dNeedGam = GameManager.systemParamsList.GetParamValueDoubleArrayByName("XianHuaCost");
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "init marryotherlogic XianHuaCost", false);
            }

            //缓存婚戒最终属性系数
            try
            {
                dRingmodulus = GameManager.systemParamsList.GetParamValueDoubleByName("GoodWillXiShu");
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "init marryotherlogic GoodWillXiShu", false);
            }

            //对方婚戒属性倍率参数
            try
            {
                dOtherRingmodulus = GameManager.systemParamsList.GetParamValueDoubleByName("BanLvXiShu");
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "init marryotherlogic BanLvXiShu", false);
            }

            //加载婚戒数据
            try
            {
                WeddingRingDic.LoadFromXMlFile("Config/WeddingRing.xml", "", "GoodsID");
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "init marryotherlogic WeddingRing.xml", false);
            }

            //加载玫瑰花数据
            try
            {
                SystemXmlItems RoseXmlItems = new SystemXmlItems();
                RoseXmlItems.LoadFromXMlFile("Config/GiveRose.xml", "", "GoodsID");

                foreach (KeyValuePair<int, SystemXmlItem> item in RoseXmlItems.SystemXmlItemDict)
                {
                    MarriageRoseData rosedata = new MarriageRoseData();
                    rosedata.nBaseAddGoodWill = item.Value.GetIntValue("GoodWill");

                    string[] strfiled = item.Value.GetStringValue("MultiplyingPower").Split('|');

                    int nAddRate = 0;

                    for (int i = 0; i < strfiled.Length; ++i)
                    {
                        string[] strfiled2 = strfiled[i].Split(',');

                        //预先计算一下几率
                        nAddRate += (int)(Convert.ToDouble(strfiled2[1]) * 100.0d);

                        rosedata.modulusList.Add(Convert.ToInt32(strfiled2[0]));
                        rosedata.rateList.Add(nAddRate);
                    }

                    RoseDataDic.Add(Convert.ToInt32(item.Value.GetIntValue("GoodsID")), rosedata);
                }
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "init marryotherlogic GiveRose.xml", false);
            }

            //加载奉献数据
            try
            {
                SystemXmlItems XmlItems = new SystemXmlItems();
                XmlItems.LoadFromXMlFile("Config/GoodWill.xml", "", "Type");

                sbyte tmpStar = 0;
                int nAddExp = 0;

                //预先把1阶0星加进去
                GoodwillAllExpList.Add(0);

                foreach (var item in XmlItems.SystemXmlItemDict)
                {
                    tmpStar = 0;

                    foreach (var xmlnode in item.Value.XMLNode.Descendants())
                    {
                        nAddExp += Convert.ToInt32(xmlnode.Attribute("NeedGoodWill").Value);
                        GoodwillAllExpList.Add(nAddExp);

                        tmpStar++;
                    }
                }

                //动态设置一下最大阶级
                byMaxGoodwillLv = (sbyte)((GoodwillAllExpList.Count - 1) / tmpStar);

                //动态设置一下最大星级
                byMaxGoodwillStar = tmpStar;
            }
            catch (System.Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "init marryotherlogic GoodWill.xml", false);
            }

            //初始化协议列表
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_MARRY_ROSE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_MARRY_RING, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_MARRY_MESSAGE, 1, 1, getInstance());

            //初始化消息监听器
            //GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerInitGame, getInstance());   //玩家登陆事件
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void destroy()
        {
            //GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerInitGame, getInstance());
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                //送玫瑰
                case (int)TCPGameServerCmds.CMD_SPR_MARRY_ROSE:
                    {
                        if (cmdParams == null || cmdParams.Length != 1)
                            return false;

                        if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                        {
                            client.sendCmd(nID, (int)MarryOtherResult.NotOpen);
                            break;
                        }

                        try
                        {
                            int nGoodsDBId = Global.SafeConvertToInt32(cmdParams[0]);

                            int iRet = (int)GiveRose(client, nGoodsDBId);
                            client.sendCmd(nID, iRet);
                        }
                        catch (Exception ex) //解析错误
                        {
                            DataHelper.WriteFormatExceptionLog(ex, "CMD_SPR_MARRY_ROSE", false);
                        }
                    }
                    break;
                //更换婚戒
                case (int)TCPGameServerCmds.CMD_SPR_MARRY_RING:
                    {
                        if (cmdParams == null || cmdParams.Length != 1)
                            return false;

                        if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                        {
                            client.sendCmd(nID, (int)MarryOtherResult.NotOpen);
                            break;
                        }

                        try
                        {
                            int nRingId = Global.SafeConvertToInt32(cmdParams[0]);

                            int iRet = (int)ChangeRing(client, nRingId);
                            client.sendCmd(nID, iRet);
                        }
                        catch (Exception ex) //解析错误
                        {
                            DataHelper.WriteFormatExceptionLog(ex, "CMD_SPR_MARRY_RING", false);
                        }
                    }
                    break;
                //爱情宣言
                case (int)TCPGameServerCmds.CMD_SPR_MARRY_MESSAGE:
                    {
                        if (cmdParams == null || cmdParams.Length != 1)
                            return false;

                        if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                        {
                            client.sendCmd(nID, (int)MarryOtherResult.NotOpen);
                            break;
                        }

                        try
                        {
                            int iRet = (int)ChangeMarriageMessage(client, cmdParams[0]);
                            client.sendCmd(nID, iRet);
                        }
                        catch (Exception ex) //解析错误
                        {
                            DataHelper.WriteFormatExceptionLog(ex, "CMD_SPR_MARRY_MESSAGE", false);
                        }
                    }
                    break;
            }

            return true;
        }

        public void processEvent(EventObject eventObject)
        {
        }

        //客户端进入游戏后发送
        public void PlayGameAfterSend(GameClient client)
        {
            //登陆时发送结婚数据给客户端
            SendMarriageDataToClient(client);

            // FIX:MUBUG-1424【勇者战场】夫妻上限提示未屏蔽。
            // 这是因为夫妻二人进入了同一个跨服服务器，只需要判断如果是跨服登录就不通知上线即可解决。
            if (!client.ClientSocket.IsKuaFuLogin)
            {
                //取出情侣
                GameClient Spouseclient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);
                if (null != Spouseclient)
                {
                    //发送给伴侣情侣上线信息
                    string SpouseOLTips = string.Format(Global.GetLang("伴侣【{0}】上线了"), client.ClientData.RoleName);
                    GameManager.ClientMgr.NotifyImportantMsg(Spouseclient, SpouseOLTips, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);

                    //                 //也给自己发送情侣上线信息 //策划要求去掉
                    //                 SpouseOLTips = string.Format("伴侣【{0}】上线了", Spouseclient.ClientData.RoleName);
                    //                 GameManager.ClientMgr.NotifyImportantMsg(client, Global.GetLang(SpouseOLTips), GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                }
            }
        }

        /// <summary>
        /// 更新爱情宣言
        /// </summary>
        public MarryOtherResult ChangeMarriageMessage(GameClient client, string strMessage)
        {
            //看看是不是结婚了
            if (-1 == client.ClientData.MyMarriageData.byMarrytype)
                return MarryOtherResult.NotMarriaged;

            //检查一下宣言长度 todo... 鼠标库表是varchar 128 所以最长就64吧
            if (strMessage.Length >= 64)
                return MarryOtherResult.MessageLimit;

            //检查屏蔽字 todo...

            //把自己的爱情宣言更新
            client.ClientData.MyMarriageData.strLovemessage = strMessage;

            //保存数据我自己的爱情宣言
            MarryFuBenMgr.UpdateMarriageData2DB(client);

            SendMarriageDataToClient(client);

            //取出情侣
            GameClient Spouseclient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);
            if (null != Spouseclient)
            {
                //更新情侣的爱情宣言
                Spouseclient.ClientData.MyMarriageData.strLovemessage = strMessage;

                //保存数据配偶的爱情宣言
                MarryFuBenMgr.UpdateMarriageData2DB(Spouseclient);

                SendMarriageDataToClient(Spouseclient);
            }
            else
            {
                //从db查找配偶数据
                string tcpstring = string.Format("{0}", client.ClientData.MyMarriageData.nSpouseID);
                MarriageData SpouseMarriageData = Global.sendToDB<MarriageData, string>((int)TCPGameServerCmds.CMD_DB_GET_MARRY_DATA, tcpstring, client.ServerId);

                //没有数据? 或者出错?
                if (null == SpouseMarriageData
                    || -1 == SpouseMarriageData.byMarrytype)
                    return MarryOtherResult.Error;

                //更新配偶爱情宣言到数据库
                MarryFuBenMgr.UpdateMarriageData2DB(client.ClientData.MyMarriageData.nSpouseID, SpouseMarriageData, client);
            }
            return MarryOtherResult.Success;
        }

        /// <summary>
        /// 更换婚戒属性
        /// </summary>
        public MarryOtherResult ChangeRing(GameClient client, int nRingID)
        {
            //结婚给戒指不会走该函数 所以不存在扣费用和婚戒id为-1的情况

            //看看是不是结婚了
            if (-1 == client.ClientData.MyMarriageData.byMarrytype)
                return MarryOtherResult.NotMarriaged;

            //不是下一等级戒指
            if (nRingID - client.ClientData.MyMarriageData.nRingID != 1)
            {
                return MarryOtherResult.NotNexRise;
            }

            SystemXmlItem RingXmlItem = null;
            if (false == WeddingRingDic.SystemXmlItemDict.TryGetValue(nRingID, out RingXmlItem)
                || null == RingXmlItem)
            {
                return MarryOtherResult.NotRing;
            }

            SystemXmlItem NowRingXmlItem = null;
            if (false == WeddingRingDic.SystemXmlItemDict.TryGetValue(client.ClientData.MyMarriageData.nRingID, out NowRingXmlItem)
                || null == NowRingXmlItem)
            {
                return MarryOtherResult.NotRing;
            }

            //策划修改为直接扣除婚戒差价
            int nCostGam = RingXmlItem.GetIntValue("NeedZuanShi");
            int nBeforeCostGam = NowRingXmlItem.GetIntValue("NeedZuanShi");
            int chajia = nCostGam - nBeforeCostGam;
            if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, chajia, "更换婚戒扣除"))
                return MarryOtherResult.NeedGam;

            //都好了更换婚戒
            client.ClientData.MyMarriageData.nRingID = nRingID;

            //更新婚戒属性
            UpdateRingAttr(client, true);

            MarryFuBenMgr.UpdateMarriageData2DB(client);

            //发送给客户端更新数据
            SendMarriageDataToClient(client, true);

            return MarryOtherResult.Success;
        }

        /// <summary>
        /// 更新婚戒属性
        /// bNeedUpdateSpouse 更新配偶戒指属性
        /// </summary>
        public void UpdateRingAttr(GameClient client, bool bNeedUpdateSpouse = false, bool bIsLogin = false)
        {
            //功能未开启不增加婚戒属性
            if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                return;

            //看看是否有婚戒
            if (-1 == client.ClientData.MyMarriageData.nRingID)
                return;

            //看看是不是结婚了 没结婚不会增加婚戒属性
            if (-1 == client.ClientData.MyMarriageData.byMarrytype
                || -1 == client.ClientData.MyMarriageData.nSpouseID)
                return;

            //[bing] 如果发现配偶在线直接取数据
            MarriageData SpouseMarriageData = null;
            GameClient Spouseclient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);
            if (null != Spouseclient)
            {
                SpouseMarriageData = Spouseclient.ClientData.MyMarriageData;
            }
            else
            {
                //取一下情侣的数据 需要情侣婚戒数据
                string tcpstring = string.Format("{0}", client.ClientData.MyMarriageData.nSpouseID);
                SpouseMarriageData = Global.sendToDB<MarriageData, String>((int)TCPGameServerCmds.CMD_DB_GET_MARRY_DATA, tcpstring, client.ServerId);
            }

            //没有数据? 或者出错?
            if (null == SpouseMarriageData
                || -1 == SpouseMarriageData.nRingID)
                return;

            //结婚后，夫妻双方享受自己婚戒的BUFF最终属性，和对方婚戒的30%的最终属性

            //先找到自己婚戒的最终属性
            EquipPropItem myringitem = GameManager.EquipPropsMgr.FindEquipPropItem(client.ClientData.MyMarriageData.nRingID);
            EquipPropItem tmpmyringitem = new EquipPropItem();

            //找到自己伴侣婚戒的最终属性
            EquipPropItem spouseringitem = GameManager.EquipPropsMgr.FindEquipPropItem(SpouseMarriageData.nRingID);
            EquipPropItem tmpspouseringitem = new EquipPropItem();

            //计算婚戒最终属性
            for (int i = 0; i < tmpmyringitem.ExtProps.Length; ++i)
            {
                tmpmyringitem.ExtProps[i] = RingAttrJiSuan(client.ClientData.MyMarriageData.byGoodwilllevel, client.ClientData.MyMarriageData.byGoodwillstar, myringitem.ExtProps[i]);
                tmpspouseringitem.ExtProps[i] = RingAttrJiSuan(SpouseMarriageData.byGoodwilllevel, SpouseMarriageData.byGoodwillstar, spouseringitem.ExtProps[i]);
                tmpmyringitem.ExtProps[i] += tmpspouseringitem.ExtProps[i] * dOtherRingmodulus;
            }

            //更新属性 [bing] 因为现在只有一个婚戒 所以不要记ID了 mark
            client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.MarriageRing, /*client.ClientData.MyMarriageData.nRingID,*/ tmpmyringitem.ExtProps);

            if (false == bIsLogin)
            {
                // 通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }

            //如果情侣在线可能需要更新情侣的婚戒属性
            //取出情侣
            if (true == bNeedUpdateSpouse)
            {
                if (null != Spouseclient)
                {
                    UpdateRingAttr(Spouseclient, false);
                }
            }
        }

        //最终属性计算为 戒指最终属性=基础属性*（1+(阶-1）*2+等级*系数）
        private double RingAttrJiSuan(sbyte level, sbyte star, double ExpProp)
        {
            return ExpProp * (1 + (level - 1) * 2 + star * dRingmodulus);
        }

        /// <summary>
        /// 重置婚戒属性 用于离婚后重置属性
        /// </summary>
        public void ResetRingAttr(GameClient client)
        {
            //必须先重置属性再清ringid
            if (-1 == client.ClientData.MyMarriageData.nRingID)
                return;

            EquipPropItem tmpnullprop = new EquipPropItem();

            //更新属性
            client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.MarriageRing, /*client.ClientData.MyMarriageData.nRingID,*/ tmpnullprop.ExtProps);

            // 通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
        }

        /// <summary>
        /// 送花
        /// </summary>
        public MarryOtherResult GiveRose(GameClient client, int nGoodsDBId)
        {
            //看看是不是情侣
            if (client.ClientData.MyMarriageData.byMarrytype == -1)
                return MarryOtherResult.NotMarriaged;

            //到达上限不加
            if (client.ClientData.MyMarriageData.byGoodwilllevel == byMaxGoodwillLv
                && client.ClientData.MyMarriageData.byGoodwillstar == byMaxGoodwillStar)
                return MarryOtherResult.MaxLimit;

            //看看今日是否还有献花次数 现在版本没有次数限制 可以一直捐献
            //if (client.ClientData.MyMarriageData.nGivenrose >= 15)
            //return;

            //在包裹里找到这个物品
            GoodsData goodsData = Global.GetGoodsByID(client, nGoodsDBId);
            if (null == goodsData)
            {
                return MarryOtherResult.NotFindItem;
            }

            lock (RoseDataDic)
            {
                //看看这个物品是不是属于花类
                MarriageRoseData rosedata = null;
                if (!RoseDataDic.TryGetValue(goodsData.GoodsID, out rosedata))
                {
                    return MarryOtherResult.ItemNotRose;
                }

                //检查一下钻石
                int ngamcost = 0;
                //如果次数不够最高按照表来
                if (client.ClientData.MyMarriageData.nGivenrose < dNeedGam.Length)
                    ngamcost = Convert.ToInt32(dNeedGam[client.ClientData.MyMarriageData.nGivenrose]);
                else
                    //如果次数超过表上限则按最高的花费钻石
                    ngamcost = Convert.ToInt32(dNeedGam[dNeedGam.Length - 1]);
                if (ngamcost != 0 && !GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, ngamcost, "结婚献花"))
                    return MarryOtherResult.NeedGam;

                //扣除物品
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, goodsData, 1, false))
                    return MarryOtherResult.NeedRose;

                //更新献花次数
                client.ClientData.MyMarriageData.nGivenrose++;

                //根据几率来选择增加奉献值的倍率
                int nRank = Global.GetRandomNumber(0, 100);
                int nModulus = 1;

                for (int i = 0; i < rosedata.rateList.Count; ++i)
                {
                    if (nRank < rosedata.rateList[i])
                    {
                        nModulus = rosedata.modulusList[i];
                        break;
                    }
                }

                //增加奉献值=基础奉献值*倍率系数
                UpdateMarriageGoodWill(client, (rosedata.nBaseAddGoodWill * nModulus));

                //出现暴击效果返回给客户端
                 if (nModulus != 1)
                     return MarryOtherResult.CirEffect;
            }

            return MarryOtherResult.Success;
        }

        public bool CanAddMarriageGoodWill(GameClient client)
        {
            //功能未开启
            if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                return false;

            //看看是不是情侣
            if (client.ClientData.MyMarriageData.byMarrytype == -1)
                return false;

            sbyte tmpGoodwilllv = client.ClientData.MyMarriageData.byGoodwilllevel;
            sbyte tmpGoodwillstar = client.ClientData.MyMarriageData.byGoodwillstar;

            //加值前先检查当前阶级是否已达上限 到达上限不加
            if (tmpGoodwilllv == byMaxGoodwillLv
                && tmpGoodwillstar == byMaxGoodwillStar)
                return false;

            return true;
        }

        /// <summary>
        /// 提升奉献值
        /// </summary>
        public void UpdateMarriageGoodWill(GameClient client, int addGoodwillValue)
        {
            //功能未开启
            if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                return;

            //看看是不是情侣
            if (client.ClientData.MyMarriageData.byMarrytype == -1)
                return;

            //不加也不减直接无视吧
            if (addGoodwillValue == 0)
                return;

            sbyte tmpGoodwilllv = client.ClientData.MyMarriageData.byGoodwilllevel;
            sbyte tmpGoodwillstar = client.ClientData.MyMarriageData.byGoodwillstar;

            //加值前先检查当前阶级是否已达上限 到达上限不加
            if (tmpGoodwilllv == byMaxGoodwillLv
                && tmpGoodwillstar == byMaxGoodwillStar)
                return;

            //加值
            client.ClientData.MyMarriageData.nGoodwillexp += addGoodwillValue;

            //用当前经验值增加上该阶级和星级应该有的经验
            int nNowLvAddExp = GoodwillAllExpList[(tmpGoodwilllv - 1) * byMaxGoodwillStar + tmpGoodwillstar];
            client.ClientData.MyMarriageData.nGoodwillexp += nNowLvAddExp;

            bool bUpdateLv = false;
            bool bUpdateStar = false;
            //看看当前经验达到哪一级 直接设上去
            for (int i = 1; i < GoodwillAllExpList.Count; ++i)
            {
                //超过最后一项表示满级满星了
                if (i == GoodwillAllExpList.Count - 1 && client.ClientData.MyMarriageData.nGoodwillexp >= GoodwillAllExpList[i])
                {
                    client.ClientData.MyMarriageData.byGoodwilllevel = byMaxGoodwillLv;
                    client.ClientData.MyMarriageData.byGoodwillstar = byMaxGoodwillStar;

                    bUpdateStar = true;

                    //设定到最大经验
                    client.ClientData.MyMarriageData.nGoodwillexp = GoodwillAllExpList[i] - GoodwillAllExpList[i - 1];
                }
                else if (client.ClientData.MyMarriageData.nGoodwillexp < GoodwillAllExpList[i])
                {
                    int nLv = 0;
                    int nStar = 0;

                    //1阶情况
                    if (i <= byMaxGoodwillStar + 1)
                    {
                        nLv = 1;
                        nStar = i - 1;
                    }
                    else
                    {
                        nLv = (i - 2) / byMaxGoodwillStar + 1;
                        nStar = (i - 1) % byMaxGoodwillStar;
                        if (nStar == 0)
                            nStar = 10;
                    }

                    if (nLv != tmpGoodwilllv)
                        bUpdateLv = true;
                    if (nStar != tmpGoodwillstar)
                        bUpdateStar = true;

                    client.ClientData.MyMarriageData.byGoodwilllevel = (sbyte)nLv;
                    client.ClientData.MyMarriageData.byGoodwillstar = (sbyte)nStar;

                    //发送客户端和记录数据库时清掉多余的经验部分 例如66点经验 - 当前阶级总共60点经验后剩 6 记录数据
                    client.ClientData.MyMarriageData.nGoodwillexp -= GoodwillAllExpList[i - 1];

                    break;
                }
            }

            //[bing] 更新时间
            if (true == bUpdateLv || true == bUpdateStar)
                client.ClientData.MyMarriageData.ChangTime = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");

            //发送给DB update数据
            MarryFuBenMgr.UpdateMarriageData2DB(client);

            //更新婚戒属性
            if (true == bUpdateLv || true == bUpdateStar)
                UpdateRingAttr(client, true);

            //发送给客户端更新数据
            SendMarriageDataToClient(client, (true == bUpdateLv || true == bUpdateStar));

            if(true == bUpdateLv)
            {
                //[bing] 刷新客户端活动叹号
                if (client._IconStateMgr.CheckJieRiFanLi(client, ActivityTypes.JieriMarriage) == true
                    || client._IconStateMgr.CheckSpecialActivity(client))
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                    client._IconStateMgr.SendIconStateToClient(client);
                }
            }

            if (addGoodwillValue > 0)
            {
                string strHint = StringUtil.substitute(Global.GetLang("你获得了：{0}点奉献度"), addGoodwillValue);
                GameManager.ClientMgr.NotifyImportantMsg(client, strHint, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.PiaoFuZi);
            }
        }

        /// <summary>
        /// 跨天更新的东西
        /// </summary>
        public void ChangeDayUpdate(GameClient client, bool bIsFirstLogin = true)
        {
            if (true == bIsFirstLogin && client.ClientData.MyMarriageData.nGivenrose != 0)
            {
                client.ClientData.MyMarriageData.nGivenrose = 0;

                //发送给DB update数据
                MarryFuBenMgr.UpdateMarriageData2DB(client);
                SendMarriageDataToClient(client, false);
            }
        }

        /// <summary>
        /// 发送结婚数据给客户端 上线时或者属性变动时发送
        /// </summary>
        public void SendMarriageDataToClient(GameClient client, bool bSendSpouseData = true)
        {
            if (null == client.ClientData.MyMarriageData)
                return;

            client.sendCmd<MarriageData>((int)TCPGameServerCmds.CMD_SPR_MARRY_UPDATE, client.ClientData.MyMarriageData);

            if (false == bSendSpouseData)
                return;

            SendSpouseDataToClient(client);
        }

        /// <summary>
        /// 发送情侣数据给客户端
        /// </summary>
        public void SendSpouseDataToClient(GameClient client)
        {
            try
            {
                if (-1 != client.ClientData.MyMarriageData.nSpouseID)
                {
                    MarriageData_EX myMarriageData_EX = new MarriageData_EX();
                    GameClient Spouseclient = GameManager.ClientMgr.FindClient(client.ClientData.MyMarriageData.nSpouseID);

                    //在线情况
                    if (null != Spouseclient) 
                    {
                        myMarriageData_EX.myMarriageData = Spouseclient.ClientData.MyMarriageData;
                        myMarriageData_EX.roleName = Spouseclient.ClientData.RoleName;
                        myMarriageData_EX.Occupation = Spouseclient.ClientData.Occupation;
                        client.sendCmd<MarriageData_EX>((int)TCPGameServerCmds.CMD_SPR_MARRY_SPOUSE_DATA, myMarriageData_EX);
                    }
                    else //不在线情况
                    {
                        RoleDataEx roleDataEx = MarryLogic.GetOfflineRoleData(client.ClientData.MyMarriageData.nSpouseID);
                        if (roleDataEx != null)
                        {
                            myMarriageData_EX.roleName = roleDataEx.RoleName;
                            myMarriageData_EX.Occupation = roleDataEx.Occupation;
                            myMarriageData_EX.myMarriageData = roleDataEx.MyMarriageData;
                            //将结婚信息和配偶名称职业一起发过去
                            client.sendCmd<MarriageData_EX>((int)TCPGameServerCmds.CMD_SPR_MARRY_SPOUSE_DATA, myMarriageData_EX);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "SendSpouseDataToClient", false);
            }
        }
    }
}