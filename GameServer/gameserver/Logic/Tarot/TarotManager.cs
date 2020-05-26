using GameServer.Core.Executor;
using GameServer.Server;
using GameServer.TarotData;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic.Tarot
{
    class TarotManager : ICmdProcessorEx
    {
        private static List<TarotTemplate> TarotTemplates = new List<TarotTemplate>();

        //塔罗牌最大等级字典
        private static Dictionary<int, int> TarotMaxLevelDict = new Dictionary<int, int>();

        //所有塔罗牌类型集合
        private static List<int> TarotCardIds = new List<int>();

        //国王特权消耗物品ID
        private static int KingItemId = 0;

        //国王特权持续时间（单位 秒）
        private static long KingBuffTime = 0;

        //国王特权消耗物品数量
        private static int UseKingItemCount = 0;

        //国王特权增加等级随机列表
        private static List<int> KingBuffValueList = new List<int>();

        private static TarotManager instance = new TarotManager();
        public static TarotManager getInstance()
        {
            return instance;
        }

        public void Initialize()
        {
            string fileName = Global.GameResPath("Config/Tarot.xml");
            XElement xml = XElement.Load(fileName);

            if (null == xml)
            {
                throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            IEnumerable<XElement> xmlItems = xml.Elements();
            foreach (XElement xmlItem in xmlItems)
            {
                var data = new TarotTemplate();
                data.Level = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "Level"));
                //策划改了配置结构 服务器不需要读0配置
                if (data.Level == 0)
                    continue;
                data.ID = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "ID"));
                data.Name = Global.GetSafeAttributeStr(xmlItem, "Name");

                data.GoodsID = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "GoodsID"));

                var needGoodsInfo = Global.GetSafeAttributeStr(xmlItem, "NeedGoods").Split(',');

                data.NeedGoodID = Convert.ToInt32(needGoodsInfo[0]);
                data.NeedPartCount = Convert.ToInt32(needGoodsInfo[1]);

                if (TarotMaxLevelDict.ContainsKey(data.GoodsID) && TarotMaxLevelDict[data.GoodsID] < data.Level)
                {
                    TarotMaxLevelDict[data.GoodsID] = data.Level;
                }
                else
                {
                    TarotMaxLevelDict.Add(data.GoodsID, data.Level);
                }
                TarotTemplates.Add(data);
            }

            TarotCardIds = TarotMaxLevelDict.Keys.ToList();

            var kingCost = GameManager.systemParamsList.GetParamValueByName("TarotKingCost").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            KingItemId = Convert.ToInt32(kingCost[0]);
            UseKingItemCount = Convert.ToInt32(kingCost[1]);
            KingBuffTime = Convert.ToInt32(kingCost[2]) * 60;

            var kingValueInfo = GameManager.systemParamsList.GetParamValueByName("TarotKingNum").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var info in kingValueInfo)
            {
                KingBuffValueList.Add(Convert.ToInt32(info));
            }

            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TAROT_UPORINIT, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SET_TAROTPOS, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_USE_TAROTKINGPRIVILEGE, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TAROT_DATA, 1, 1, getInstance());
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                //塔罗牌升级或激活
                case (int)TCPGameServerCmds.CMD_SPR_TAROT_UPORINIT:
                    {
                        if (cmdParams == null || cmdParams.Length != 1)
                            return false;

                        try
                        {
                            int goodID = Convert.ToInt32(cmdParams[0]);
                            var strret = string.Format("{0}:{1}", Convert.ToInt32(ProcessTarotUpCmd(client, goodID)), goodID);

                            client.sendCmd(nID, strret);
                        }
                        catch (Exception ex) //解析错误
                        {
                            var strret = string.Format("{0}:{1}", (int)ETarotResult.Error, cmdParams[0]);
                            client.sendCmd(nID, strret);
                            DataHelper.WriteFormatExceptionLog(ex, "CMD_SPR_TAROT_UPORINIT", false);
                        }
                    }
                    break;
                case (int)TCPGameServerCmds.CMD_SPR_SET_TAROTPOS:
                    {
                        if (cmdParams == null || cmdParams.Length != 2)
                            return false;
                        try
                        {
                            int goodID = Convert.ToInt32(cmdParams[0]);
                            byte pos = Convert.ToByte(cmdParams[1]);

                            var strret = string.Format("{0}:{1}", Convert.ToInt32(ProcessSetTarotPosCmd(client, goodID, pos)), goodID);
                            client.sendCmd(nID, strret);
                        }
                        catch (Exception ex) //解析错误
                        {
                            var strret = string.Format("{0}:{1}", (int)ETarotResult.Error, cmdParams[0]);
                            client.sendCmd(nID, strret);
                            DataHelper.WriteFormatExceptionLog(ex, "CMD_SPR_SET_TAROTPOS", false);
                        }
                    }
                    break;
                case (int)TCPGameServerCmds.CMD_SPR_USE_TAROTKINGPRIVILEGE:
                    {
                        try
                        {
                            var strret = string.Empty;
                            var restult = Convert.ToInt32(ProcessUseKingPrivilegeCmd(client, out strret));
                            client.sendCmd(nID, string.Format("{0}:{1}", restult, strret));
                        }
                        catch (Exception ex) //解析错误
                        {
                            client.sendCmd(nID, ((int)ETarotResult.Error).ToString());
                            DataHelper.WriteFormatExceptionLog(ex, "CMD_SPR_USE_TAROTKINGPRIVILEGE", false);
                        }
                    }
                    break;
                case (int)TCPGameServerCmds.CMD_SPR_TAROT_DATA:
                    {
                        try
                        {
                            TarotSystemData tarotData = client.ClientData.TarotData;
                            client.sendCmd<TarotSystemData>(nID, tarotData);
                        }
                        catch (Exception ex) //解析错误
                        {
                            client.sendCmd(nID, ((int)ETarotResult.Error).ToString());
                            DataHelper.WriteFormatExceptionLog(ex, "CMD_SPR_USE_TAROTKINGPRIVILEGE", false);
                        }
                    }
                    break;
            }

            return true;
        }

        public enum ETarotResult
        {
            Error = -1,   //非常规出错
            Success = 0,    //成功
            Fail = 1,    //失败
            MaxLevel = 2,    //已达到最高等级
            NeedPart = 3,   //碎片数量不足
            PartSuitIsMax = 4,    //要升级的部件已经满级
            NotOpen = 5,    //该系统没开启
            PartNumError = 6, //碎片使用过多，一次只能升一级
            PosError = 7, //上阵位置有卡牌
            ItemNotEnough = 8,//国王特权道具不足
        }

        /// <summary>
        /// 处理塔罗牌升级或激活
        /// </summary>
        /// <param name="client"></param>
        /// <param name="goodID"></param>
        /// <param name="partCount"></param>
        /// <returns></returns>
        public ETarotResult ProcessTarotUpCmd(GameClient client, int goodID)
        {
            //判断功能是否开启
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.TarotCard)) return ETarotResult.NotOpen;

            TarotSystemData tarotData = client.ClientData.TarotData;
            //获取要升级的塔罗牌数据
            TarotCardData currentTarot = tarotData.TarotCardDatas.Find(x => x.GoodId == goodID);

            if (currentTarot == null)
            {
                //激活
                currentTarot = new TarotCardData();
                currentTarot.GoodId = goodID;
                tarotData.TarotCardDatas.Add(currentTarot);
            }
            //判断是否可以升级
            if (currentTarot.Level >= TarotMaxLevelDict[goodID])
            {
                return ETarotResult.MaxLevel;
            }
            //获取下级塔罗牌对应配置模板
            TarotTemplate nextTemp = TarotTemplates.Find(x => x.GoodsID == goodID && x.Level == currentTarot.Level + 1);

            if (nextTemp == null)
            {
                return ETarotResult.Error;
            }
            //判断背包碎片是否足够
            var hasPartCount = Global.GetTotalGoodsCountByID(client, nextTemp.NeedGoodID);
            if (hasPartCount < nextTemp.NeedPartCount)
            {
                return ETarotResult.NeedPart;
            }

            //使用物品  优先使用绑定物品
            bool usedBinding = false;
            bool usedTimeLimited = false;
            if (Global.UseGoodsBindOrNot(client, nextTemp.NeedGoodID, nextTemp.NeedPartCount, true, out usedBinding, out usedTimeLimited) < 1)
            {
                return ETarotResult.NeedPart;
            }

            //处理升级
            currentTarot.Level += 1;
            //更新玩家数据
            UpdataPalyerTarotAttr(client);
            //向DB服更新数据
            UpdateTarotData2DB(client, currentTarot, null);
            return ETarotResult.Success;
        }

        /// <summary>
        /// 设置塔罗牌上阵位置 （0=为上阵）
        /// </summary>
        /// <param name="client"></param>
        /// <param name="goodID"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public ETarotResult ProcessSetTarotPosCmd(GameClient client, int goodID, byte pos)
        {
            //判断功能是否开启
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.TarotCard)) return ETarotResult.NotOpen;

            //判断位置是否合法
            if (pos < 0 || pos > 6)
            {
                return ETarotResult.Error;
            }
            TarotSystemData tarotData = client.ClientData.TarotData;
            //获取塔罗牌数据
            TarotCardData currentTarot = tarotData.TarotCardDatas.Find(x => x.GoodId == goodID);
            if (currentTarot == null)
            {
                return ETarotResult.Error;
            }
            if (currentTarot.Postion == pos)
            {
                return ETarotResult.Error;
            }
            //上阵
            if (pos > 0)
            {
                //判断当前卡牌是否已在阵上
                if (currentTarot.Postion > 0)
                {
                    return ETarotResult.Error;
                }
                //判断装备的位置是否为空
                TarotCardData targetTarot = tarotData.TarotCardDatas.Find(x => x.Postion == pos);
                if (targetTarot != null)
                {
                    targetTarot.Postion = 0;
                }
            }
            currentTarot.Postion = pos;
            //更新玩家塔罗牌加成属性
            UpdataPalyerTarotAttr(client);
            //向DB服更新数据
            UpdateTarotData2DB(client, currentTarot, null);

            return ETarotResult.Success;
        }

        /// <summary>
        /// 使用国王特权
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public ETarotResult ProcessUseKingPrivilegeCmd(GameClient client, out string strret)
        {
            strret = string.Empty;
            //判断功能是否开启
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.TarotCard)) return ETarotResult.NotOpen;

            TarotSystemData tarotData = client.ClientData.TarotData;

            if (tarotData.KingData.StartTime > 0)
            {
                //重置
                tarotData.KingData = new TarotKingData();
                //更新玩家塔罗牌加成属性
                UpdataPalyerTarotAttr(client);
                //向DB服更新数据
                UpdateTarotData2DB(client, null, tarotData.KingData);
                return ETarotResult.Success;
            }
            //判断背包碎片是否足够
            var kingItemCount = Global.GetTotalGoodsCountByID(client, KingItemId);
            if (kingItemCount < UseKingItemCount)
            {
                return ETarotResult.ItemNotEnough;
            }

            //使用物品  优先使用绑定物品
            bool usedBinding = false;
            bool usedTimeLimited = false;
            if (Global.UseGoodsBindOrNot(client, KingItemId, UseKingItemCount, true, out usedBinding, out usedTimeLimited) < 1)
            {
                return ETarotResult.NeedPart;
            }

            tarotData.KingData.StartTime = TimeUtil.NOW();

            tarotData.KingData.BufferSecs = KingBuffTime;

            TarotCardIds = Global.RandomSortList(TarotCardIds);

            KingBuffValueList = Global.RandomSortList(KingBuffValueList);

            tarotData.KingData.AddtionDict = new Dictionary<int, int>();

            var totalNum = KingBuffValueList[0];

            if (totalNum < 3)
                return ETarotResult.Error;

            for (var i = 0; i < 3; i++)
            {
                var ranNum = 1;
                if (i < 2)
                {
                    ranNum = Global.GetRandomNumber(0, totalNum - 3);
                    totalNum -= ranNum;
                }
                else
                    ranNum = totalNum - 3;

                var ranGoodId = TarotCardIds[i];
                strret += ranGoodId + ":" + (int)(ranNum + 1) + ":";
                tarotData.KingData.AddtionDict.Add(ranGoodId, ranNum + 1);
            }

            //更新玩家塔罗牌加成属性
            UpdataPalyerTarotAttr(client);

            //向DB服更新数据
            UpdateTarotData2DB(client, null, tarotData.KingData);

            strret = strret.TrimEnd(':');

            return ETarotResult.Success;
        }

        /// <summary>
        /// 移除国王特权
        /// </summary>
        /// <param name="client"></param>
        public void RemoveTarotKingData(GameClient client)
        {
            TarotSystemData tarotData = client.ClientData.TarotData;

            if (tarotData.KingData.StartTime == 0) return;

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - tarotData.KingData.StartTime) >= (tarotData.KingData.BufferSecs * 1000))
            {
                tarotData.KingData = new TarotKingData();

                //更新玩家塔罗牌加成属性
                UpdataPalyerTarotAttr(client);

                //向DB服更新数据
                UpdateTarotData2DB(client, null, tarotData.KingData);
            }
        }

        ////登陆时发送给客户端圣物数据
        ////发送格式为 string = 圣物数量:<圣物类型:圣物部件数:<部件位置:部件阶数:部件碎片数量:>>
        //public void PlayGameAfterSend(GameClient client)
        //{
        //    TarotSystemData tarotData = client.ClientData.TarotData;
        //    if (tarotData == null)
        //        return;

        //    client.sendCmd<TarotSystemData>((int)TCPGameServerCmds.CMD_SPR_TAROT_DATA, tarotData);
        //}


        /// <summary>
        /// 更新玩家塔罗牌加成属性
        /// </summary>
        public void UpdataPalyerTarotAttr(GameClient client)
        {
            var itemNew = new EquipPropItem();

            var extProps = itemNew.ExtProps;

            foreach (var card in client.ClientData.TarotData.TarotCardDatas)
            {
                EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(card.GoodId);

                if (card.Postion > 0)
                {
                    for (int i = 0; i < extProps.Length; i++)
                    {
                        var addLevel = 0;
                        if (client.ClientData.TarotData.KingData.AddtionDict.ContainsKey(card.GoodId))
                        {
                            addLevel = client.ClientData.TarotData.KingData.AddtionDict[card.GoodId];
                        }
                        extProps[i] += item.ExtProps[i] * (card.Level + addLevel);
                    }
                }
            }
            client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.TarotCard, 0, extProps);

            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
        }


        //更新数据库资料
        private static void UpdateTarotData2DB(GameClient client, TarotCardData tarotData, TarotKingData tarotKingBuffData)
        {
            string[] dbFields = null;

            var tarotStrInfo = tarotData == null ? "-1" : tarotData.GetDataStrInfo();

            var kingBuffStrInfo = tarotKingBuffData == null ? "-1" : tarotKingBuffData.GetDataStrInfo();

            var sCmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, tarotStrInfo, kingBuffStrInfo);

            TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_DB_UPDATA_TAROT, sCmd, out dbFields, client.ServerId);
        }


        public class TarotTemplate
        {
            public int ID { get; set; }

            public string Name { get; set; }

            public int GoodsID { get; set; }

            public int Level { get; set; }

            public int NeedGoodID { get; set; }

            public int NeedPartCount { get; set; }
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }
    }
}
