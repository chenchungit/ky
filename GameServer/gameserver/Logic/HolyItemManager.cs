using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ProtoBuf;
using GameServer.Server;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using Server.Data;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace Server.Data
{
    //圣物部件类
    [ProtoContract]
    public class HolyItemPartData
    {
        //圣物部件阶级 目前最高有9阶
        [ProtoMember(1)]
        public sbyte m_sSuit = 0;

        //部件碎片
        [ProtoMember(2)]
        public int m_nSlice = 0;

        //进阶失败次数
        [ProtoMember(3)]
        public int m_nFailCount = 0;
    }

    //圣物数据类
    [ProtoContract]
    public class HolyItemData
    {
        //圣物类型 目前有4个类型 1 == 黄金圣杯 2 == 黄金圣冠 3 == 黄金圣剑 4 == 黄金圣典
        [ProtoMember(1)]
        public sbyte m_sType = 0;

        //圣物部件 目前有6个部件
        [ProtoMember(2)]
        public Dictionary<sbyte, HolyItemPartData> m_PartArray = new Dictionary<sbyte, HolyItemPartData>();
    }
}

namespace GameServer.Logic
{
    //圣物系统部件静态数据
    class HolyPartInfo
    {
        //花费绑定金币
        public int m_nCostBandJinBi = 0;

        //需要物品
        public int m_nNeedGoodsID = 0;

        //需要物品数量
        public int m_nNeedGoodsCount = 0;

        //失败扣除道具ID
        public int m_nFailCostGoodsID = 0;

        //失败扣除道具数量
        public int m_nFailCostGoodsCount = 0;

        //成功几率
        public sbyte m_sSuccessProbability = 0;

        //属性列表
        public List<MagicActionItem> m_PropertyList = new List<MagicActionItem>();

        //最大连续失败次数
        public int m_nMaxFailCount = 0;

        //根据给定的阶级和部件位置和圣物部件类型返回部件的ID
        public static int GetBujianID(sbyte nType, sbyte nSlot, sbyte nSuit)
        {
            //ID规则为 type * 1000 + (部件位置<1 ~ 6> - 1) * 100 + 部件阶级
            return (nType * 1000 + (nSlot - 1) * 100 + nSuit);
        }
    }

    //圣物系统圣物静态数据
    class HolyInfo
    {
        //额外属性列表
        public List<MagicActionItem> m_ExtraPropertyList = new List<MagicActionItem>();

        //根据给定阶级和圣物类型返回圣物ID
        public static int GetShengwuID(sbyte nSuit, sbyte nType)
        {
            //ID规则为 type * 100 + 部件阶级
            return (nType * 100 + nSuit);
        }
    }

    public enum EHolyResult
    {
        Error           = -1,   //非常规出错
        Success         = 0,    //成功
        Fail            = 1,    //失败
        NeedGold        = 2,    //金币加绑金不足
        NeedHolyItemPart = 3,   //碎片数量不足
        PartSuitIsMax   = 4,    //要升级的部件已经满级
        NotOpen         = 5,    //该系统没开启
    }

    //[bing] 圣物系统管理器
    class HolyItemManager : ICmdProcessorEx
    {
        //圣物系统圣物部件最大阶级
        public static readonly sbyte MAX_HOLY_PART_LEVEL = 9;

        //圣物系统圣物部件数量
        public static readonly sbyte MAX_HOLY_PART_NUM = 6;

        //圣物系统圣物个数
        public static readonly sbyte MAX_HOLY_NUM = 4;

        //圣物系统部件静态数据dic
        private Dictionary<int, HolyPartInfo> _partDataDic = new Dictionary<int, HolyPartInfo>();

        //圣物系统圣物静态数据dic
        private Dictionary<int, HolyInfo> _holyDataDic = new Dictionary<int, HolyInfo>();

        //圣物系统碎片物品集合
        public static readonly string[,] SliceNameSet = {
                                       { "null", "null", "null", "null", "null", "null", "null" }
                                       ,{ "null", "圣杯碎片1", "圣杯碎片2", "圣杯碎片3", "圣杯碎片4", "圣杯碎片5", "圣杯碎片6" }
                                       ,{ "null", "圣冠碎片1", "圣冠碎片2", "圣冠碎片3", "圣冠碎片4", "圣冠碎片5", "圣冠碎片6" }
                                       ,{ "null", "圣剑碎片1", "圣剑碎片2", "圣剑碎片3", "圣剑碎片4", "圣剑碎片5", "圣剑碎片6" }
                                       ,{ "null", "圣典碎片1", "圣典碎片2", "圣典碎片3", "圣典碎片4", "圣典碎片5", "圣典碎片6" }
                                   };

        /// <summary>
        /// 静态实例
        /// </summary>
        private static HolyItemManager instance = new HolyItemManager();
        public static HolyItemManager getInstance()
        {
            return instance;
        }

        //初始化静态数据
        public void Initialize()
        {
            //从BuJian.xml读取数据
            SystemXmlItems xml = new SystemXmlItems();
            xml.LoadFromXMlFile("Config/BuJian.xml", "", "ID");

            foreach (KeyValuePair<int, SystemXmlItem> item in xml.SystemXmlItemDict)
            {
                HolyPartInfo data = new HolyPartInfo();
                data.m_nCostBandJinBi = item.Value.GetIntValue("CostBandJinBi");
                //if (data.m_nCostBandJinBi < 0)
                    //data.m_nCostBandJinBi = 0;
                data.m_sSuccessProbability = Convert.ToSByte(item.Value.GetDoubleValue("SuccessProbability") * 100);
                if (data.m_sSuccessProbability < 0)
                    data.m_sSuccessProbability = -1;

                string[] strfiled = item.Value.GetStringValue("NeedGoods").Split(',');
                if(strfiled.Length > 1)
                {
                    //data.m_nNeedGoodsID = Global.SafeConvertToInt32(strfiled[0]);
                    data.m_nNeedGoodsCount = Global.SafeConvertToInt32(strfiled[1]);
                }

                strfiled = item.Value.GetStringValue("FailCost").Split(',');
                if (strfiled.Length > 1)
                {
                    //data.m_nFailCostGoodsID = Global.SafeConvertToInt32(strfiled[0]);
                    data.m_nFailCostGoodsCount = Global.SafeConvertToInt32(strfiled[1]);
                }

                string strParam = item.Value.GetStringValue("Property");
                if (strParam != "-1")
                    data.m_PropertyList = GameManager.SystemMagicActionMgr.ParseActionsOutUse(strParam);

                data.m_nMaxFailCount = item.Value.GetIntValue("FailMaxNum");
                if (data.m_nMaxFailCount < 0)
                    data.m_nMaxFailCount = 0;

                _partDataDic.Add(item.Value.GetIntValue("ID"), data);
            }

            //从ShengWu.xml读取数据
            xml = new SystemXmlItems();
            xml.LoadFromXMlFile("Config/ShengWu.xml", "", "ID");

            foreach (KeyValuePair<int, SystemXmlItem> item in xml.SystemXmlItemDict)
            {
                HolyInfo data = new HolyInfo();

                string strParam = item.Value.GetStringValue("ExtraProperty");
                if (strParam != "-1")
                    data.m_ExtraPropertyList = GameManager.SystemMagicActionMgr.ParseActionsOutUse(strParam);

                _holyDataDic.Add(item.Value.GetIntValue("ID"), data);
            }

            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_DB_UPDATE_HOLYITEM, 2, 2, getInstance());
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch(nID)
            {
                //圣物进阶
                case (int)TCPGameServerCmds.CMD_DB_UPDATE_HOLYITEM:
                    {
                        if (cmdParams == null || cmdParams.Length != 2)
                            return false;

                        try
                        {
                            sbyte sShengWu = Convert.ToSByte(cmdParams[0]);
                            sbyte sBuJian = Convert.ToSByte(cmdParams[1]);

                            string strret = Convert.ToInt32(HolyItem_Suit_Up(client, sShengWu, sBuJian)).ToString();
                            client.sendCmd(nID, strret);
                        }
                        catch (Exception ex) //解析错误
                        {
                            DataHelper.WriteFormatExceptionLog(ex, "CMD_DB_UPDATE_HOLYITEM", false);
                        }
                    }
                    break;
            }

            return true;
        }

        //圣物进阶
        private EHolyResult HolyItem_Suit_Up(GameClient client, sbyte sShengWu_slot, sbyte sBuJian_slot)
        {
            // 如果1.7的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                return EHolyResult.NotOpen;

            //增加系统开启判断
            if (false == GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.HolyItem))
                return EHolyResult.NotOpen;

            //不满足任务条件
            if (GlobalNew.IsGongNengOpened(client, GongNengIDs.HolyItem, true) == false)
                return EHolyResult.NotOpen;

            if (null == client.ClientData.MyHolyItemDataDic)
                return EHolyResult.Error;

            Dictionary<sbyte, HolyItemData> holyitemdata = client.ClientData.MyHolyItemDataDic;
            HolyItemData tmpdata = null;
            HolyItemPartData tmppartdata = null;
            HolyPartInfo xmlData = null;

            //先取得当前圣物部件等级
            if (false == holyitemdata.TryGetValue(sShengWu_slot, out tmpdata))
                return EHolyResult.Error;

            if(false == tmpdata.m_PartArray.TryGetValue(sBuJian_slot, out tmppartdata))
                return EHolyResult.Error;

            //0 看看要升级的部件阶级是不是已经满级了
            if (tmppartdata.m_sSuit >= MAX_HOLY_PART_LEVEL)
                return EHolyResult.PartSuitIsMax;

            int nDataID = HolyPartInfo.GetBujianID(sShengWu_slot, sBuJian_slot, (sbyte)tmppartdata.m_sSuit);
            if(false == _partDataDic.TryGetValue(nDataID, out xmlData))
                return EHolyResult.Error;

            //1 绑金+金币是否足够
            if (-1 != xmlData.m_nCostBandJinBi
                && xmlData.m_nCostBandJinBi > Global.GetTotalBindTongQianAndTongQianVal(client))
            {
                return EHolyResult.NeedGold;
            }

            //2 部件碎片是否足够
            if(-1 != xmlData.m_nNeedGoodsCount
                && xmlData.m_nNeedGoodsCount > tmppartdata.m_nSlice)
            {
                return EHolyResult.NeedHolyItemPart;
            }

            //3 以上均满足，判断成功率
            bool bSuccess = false;
            int nRank = Global.GetRandomNumber(0, 100);
            if (-1 == xmlData.m_sSuccessProbability
                || tmppartdata.m_nFailCount >= xmlData.m_nMaxFailCount      //[bing] 2015,8,12 达到连续失败最大次数必成功
                || nRank < (int)(xmlData.m_sSuccessProbability))
            {
                //4 合成成功：消耗金币、消耗部件碎片，阶数+1
                bSuccess = true;

                //扣除金币
                if(-1 != xmlData.m_nCostBandJinBi)
                {
                    if (!Global.SubBindTongQianAndTongQian(client, xmlData.m_nCostBandJinBi, "圣物部件升级消耗"))
                        return EHolyResult.Error;
                }

                //扣除部件碎片
                if (-1 != xmlData.m_nNeedGoodsCount)
                    tmppartdata.m_nSlice -= xmlData.m_nNeedGoodsCount;
                if (tmppartdata.m_nSlice < 0)
                {
                    tmppartdata.m_nSlice = 0;
                    return EHolyResult.Error;
                }
                
                //部件阶级提升
                tmppartdata.m_sSuit += 1;

                //重置失败次数
                tmppartdata.m_nFailCount = 0;
            }
            else
            {
                //5 合成失败：消耗金币、消耗部分部件碎片

                //扣除金币
                if (-1 != xmlData.m_nCostBandJinBi)
                {
                    if (!Global.SubBindTongQianAndTongQian(client, xmlData.m_nCostBandJinBi, "圣物部件升级消耗"))
                        return EHolyResult.Error;
                }

                //扣除失败时的部件碎片
                if (-1 != xmlData.m_nFailCostGoodsCount)
                    tmppartdata.m_nSlice -= xmlData.m_nFailCostGoodsCount;
                if (tmppartdata.m_nSlice < 0)
                {
                    tmppartdata.m_nSlice = 0;
                    return EHolyResult.Error;
                }

                //失败次数增加1
                tmppartdata.m_nFailCount += 1;
            }

            if(true == bSuccess)
            {
                //计算部件属性
                UpdateHolyItemBuJianAttr(client, sShengWu_slot, sBuJian_slot);

                //计算圣物额外属性
                UpdataHolyItemExAttr(client, sShengWu_slot);

                // 通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }

            //更新db
            UpdateHolyItemData2DB(client, sShengWu_slot, sBuJian_slot, tmppartdata);

            //发送给客户端更新数据
            HolyItemSendToClient(client, sShengWu_slot, sBuJian_slot);

            //写log做进阶统计
            GameManager.logDBCmdMgr.AddDBLogInfo(
                -1
                , SliceNameSet[sShengWu_slot, sBuJian_slot]
                , /**/"圣物进阶"
                , /**/"系统"
                , client.ClientData.RoleName
                , bSuccess == true ? /**/"成功" : /**/"失败"
                , xmlData.m_nCostBandJinBi != -1 ? xmlData.m_nCostBandJinBi : 0     //消耗的金币数
                , client.ClientData.ZoneID
                , client.strUserID
                , tmppartdata.m_nSlice
                , client.ServerId);

            if (client._IconStateMgr.CheckSpecialActivity(client))
                client._IconStateMgr.SendIconStateToClient(client);

            return bSuccess == true ? EHolyResult.Success : EHolyResult.Fail;
        }

        //得到圣物碎片
        public void GetHolyItemPart(GameClient client, sbyte sShengWu_slot, sbyte sBuJian_slot, int nNum)
        {
            // 如果1.7的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                return ;

            Dictionary<sbyte, HolyItemData> holyitemdata = client.ClientData.MyHolyItemDataDic;
            HolyItemData tmpdata = null;
            HolyItemPartData tmppartdata = null;

            //先取得当前圣物部件等级
            if (false == holyitemdata.TryGetValue(sShengWu_slot, out tmpdata))
                return;

            if (false == tmpdata.m_PartArray.TryGetValue(sBuJian_slot, out tmppartdata))
                return;

            tmppartdata.m_nSlice += nNum;

            //更新DB
            UpdateHolyItemData2DB(client, sShengWu_slot, sBuJian_slot, tmppartdata);

            //发送给客户端更新数据
            HolyItemSendToClient(client, sShengWu_slot, sBuJian_slot);

            //推送个hint告诉前端获得碎片
            string strHint = StringUtil.substitute(Global.GetLang("获得【{0}】{1}个"), Global.GetLang(SliceNameSet[sShengWu_slot, sBuJian_slot]), nNum);
            GameManager.ClientMgr.NotifyImportantMsg(client, strHint, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.PiaoFuZi);

            //写log做统计
            GameManager.logDBCmdMgr.AddDBLogInfo(
                -1
                , SliceNameSet[sShengWu_slot, sBuJian_slot]
                , /**/"圣物碎片"
                , Global.GetMapName(client.ClientData.MapCode)
                , client.ClientData.RoleName, /**/"增加"
                , nNum
                , client.ClientData.ZoneID
                , client.strUserID
                , tmppartdata.m_nSlice
                , client.ServerId);
        }

        //登陆时发送给客户端圣物数据
        //发送格式为 string = 圣物数量:<圣物类型:圣物部件数:<部件位置:部件阶数:部件碎片数量:>>
        public void PlayGameAfterSend(GameClient client)
        {
            // 如果1.7的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                return;
            Dictionary<sbyte, HolyItemData> holyitemdata = client.ClientData.MyHolyItemDataDic;
            if(holyitemdata == null)
                return;
            HolyItemData tmpdata = null;
            HolyItemPartData tmppartdata = null;

            for (sbyte i = 1; i <= MAX_HOLY_NUM; ++i )
            {
                if (true == holyitemdata.TryGetValue(i, out tmpdata))
                {
                    for(sbyte j = 1; j <= MAX_HOLY_PART_NUM; ++j)
                    {
                        if(false == tmpdata.m_PartArray.TryGetValue(j, out tmppartdata))
                        {
                            tmppartdata = new HolyItemPartData();
                            tmpdata.m_PartArray.Add(j, tmppartdata); 
                        }
                    }
                }
                else
                {
                    //即使数据库没有也要补全数据
                    tmpdata = new HolyItemData();
                    holyitemdata.Add(i, tmpdata);

                    tmpdata.m_sType = i;

                    for(sbyte j = 1; j <= MAX_HOLY_PART_NUM; ++j)
                    {
                        tmppartdata = new HolyItemPartData();
                        tmpdata.m_PartArray.Add(j, tmppartdata);
                    }
                }
            }

            //byte[] sendbytes = DataHelper.ObjectToBytes<Dictionary<int, HolyItemData>>(holyitemdata);
            //client.sendCmd((int)TCPGameServerCmds.CMD_SPR_HOLYITEM_DATA, sendbytes);

            client.sendCmd<Dictionary<sbyte, HolyItemData>>((int)TCPGameServerCmds.CMD_SPR_HOLYITEM_DATA, holyitemdata);
        }

        //发送给客户端某个圣物，部件更新
        public void HolyItemSendToClient(GameClient client, sbyte sShenWu_slot, sbyte sBuJian_slot)
        {
            Dictionary<sbyte, HolyItemData> holyitemdata = client.ClientData.MyHolyItemDataDic;
            if (holyitemdata == null)
                return;
            HolyItemData tmpdata = null;
            HolyItemPartData tmppartdata = null;

            if (false == holyitemdata.TryGetValue(sShenWu_slot, out tmpdata))
                return;

            if (false == tmpdata.m_PartArray.TryGetValue(sBuJian_slot, out tmppartdata))
                return;

            string strSend = string.Format("{0}:{1}:{2}:{3}", sShenWu_slot, sBuJian_slot, tmppartdata.m_sSuit, tmppartdata.m_nSlice);
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_HOLYITEM_PART_DATA, strSend);
        }

        //更新圣物全部属性
        public void UpdateAllHolyItemAttr(GameClient client)
        {
            for (sbyte i = 1; i <= MAX_HOLY_NUM; ++i)
            {
                //先计算部件属性
                for (sbyte j = 1; j <= MAX_HOLY_PART_NUM; ++j)
                {
                    UpdateHolyItemBuJianAttr(client, i, j);
                }

                //再计算圣物的额外属性
                UpdataHolyItemExAttr(client, i);
            }
        }

        //更新某个圣物某个部件属性
        public void UpdateHolyItemBuJianAttr(GameClient client, sbyte sShenWu_slot, sbyte sBuJian_slot)
        {
            Dictionary<sbyte, HolyItemData> holyitemdata = client.ClientData.MyHolyItemDataDic;
            if (null == holyitemdata)
                return;

            HolyItemData tmpdata = null;
            HolyItemPartData tmppartdata = null;

            if (true == holyitemdata.TryGetValue(sShenWu_slot, out tmpdata))
            {
                if(true == tmpdata.m_PartArray.TryGetValue(sBuJian_slot, out tmppartdata))
                {
                    int nDataID = HolyPartInfo.GetBujianID(sShenWu_slot, sBuJian_slot, (sbyte)tmppartdata.m_sSuit);
                    HolyPartInfo nXmlData = null;
                    if(false == _partDataDic.TryGetValue(nDataID, out nXmlData))
                        return;

                    for (int i = 0; i < nXmlData.m_PropertyList.Count; ++i)
                        ProcessAction(
                            client
                            , nXmlData.m_PropertyList[i].MagicActionID
                            , nXmlData.m_PropertyList[i].MagicActionParams
                            , (int)PropsSystemTypes.HolyItem
                            , sShenWu_slot
                            , sBuJian_slot);
                }
            }
        }

        //更新圣物额外属性
        public void UpdataHolyItemExAttr(GameClient client, sbyte sShenWu_slot)
        {
            Dictionary<sbyte, HolyItemData> holyitemdata = client.ClientData.MyHolyItemDataDic;
            if (null == holyitemdata)
                return;

            HolyItemData tmpdata = null;
            HolyItemPartData tmppartdata = null;

            int sMinSuit = (int)MAX_HOLY_PART_LEVEL;

            if (true == holyitemdata.TryGetValue(sShenWu_slot, out tmpdata))
            {
                for (sbyte i = 1; i <= MAX_HOLY_PART_NUM; ++i)
                {
                    if (true == tmpdata.m_PartArray.TryGetValue(i, out tmppartdata))
                    {
                        if (sMinSuit > tmppartdata.m_sSuit)
                            sMinSuit = tmppartdata.m_sSuit;
                    }
                    else
                    {
                        //存在有0阶情况直接跳
                        sMinSuit = 0;
                        break;
                    }
                }
            }
            else
            {
                //这个圣物不存在按0阶处理
                sMinSuit = 0;
            }

            HolyInfo xmlData = null;
            int nDataID = HolyInfo.GetShengwuID((sbyte)sMinSuit, sShenWu_slot);
            if (true == _holyDataDic.TryGetValue(nDataID, out xmlData))
            {
                for (int j = 0; j < xmlData.m_ExtraPropertyList.Count; ++j)
                    ProcessAction(
                        client
                        , xmlData.m_ExtraPropertyList[j].MagicActionID
                        , xmlData.m_ExtraPropertyList[j].MagicActionParams
                        , (int)PropsSystemTypes.HolyItem
                        , sShenWu_slot
                        , 100);       //0 作为额外属性用
            }
        }

        //更新数据库资料
        private void UpdateHolyItemData2DB(GameClient client, sbyte sShengWu_slot, sbyte sBuJian_slot, HolyItemPartData partdata = null)
        {
            Dictionary<sbyte, HolyItemData> holyitemdata = client.ClientData.MyHolyItemDataDic;
            HolyItemData tmpdata = null;
            HolyItemPartData tmppartdata = partdata;

            if (tmppartdata == null)
            {
                if (false == holyitemdata.TryGetValue(sShengWu_slot, out tmpdata))
                    return;

                if (false == tmpdata.m_PartArray.TryGetValue(sBuJian_slot, out tmppartdata))
                    return;
            }

            //格式:  roleID, sShengwu_type, sPart_slot, sPart_suit, nPart_slice, nFail_count

            string[] dbFields = null;
            string sCmd = "";
            sCmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", client.ClientData.RoleID, sShengWu_slot, sBuJian_slot, tmppartdata.m_sSuit, tmppartdata.m_nSlice, tmppartdata.m_nFailCount);

            TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_DB_UPDATE_HOLYITEM, sCmd, out dbFields, client.ServerId);
        }

        //属性更新
        private void ProcessAction(GameClient client, MagicActionIDs id, double[] actionParams, int nPropsSystemTypes, sbyte sShengWu_slot, sbyte sBuJian_slot)
        {
            switch(id)
            {
                case MagicActionIDs.POTION:             //药水效果：Potion，百分比 药水：GoodsID=1010、1011、1012、1013、1110、1111 效果：基础效果（1+ X.X）
                case MagicActionIDs.HOLYWATER:          //圣水效果：HolyWater，百分比 圣水：GoodsID=1000、1001、1002、1100、1101、1102 效果：基础效果（1+ X.X）
                case MagicActionIDs.RECOVERLIFEV:       //自动恢复生命效果：RecoverLifeV，百分比 效果：基础恢复生命效果*（1+X.X）
                case MagicActionIDs.ADDDEFENSE:
                case MagicActionIDs.ADDATTACK:          //对应SystemMagicAction属性枚举
                case MagicActionIDs.FATALHURT:          //卓越伤害加成：FatalHurt，百分比 效果：卓越一击伤害加成*（1+X.X）
                case MagicActionIDs.HITV:
                case MagicActionIDs.ADDATTACKINJURE:
                case MagicActionIDs.LIFESTEAL:         //击中恢复效果：LifeStealV，固定值 效果：击中恢复生命+XX
                case MagicActionIDs.DAMAGETHORN:
                case MagicActionIDs.COUNTERACTINJUREVALUE:
                case MagicActionIDs.DODGE:
                case MagicActionIDs.MAXLIFEPERCENT:
                case MagicActionIDs.AddAttackPercent:
                case MagicActionIDs.AddDefensePercent:

                //case MagicActionIDs.RECOVERMAGICV:      //自动恢复魔法效果：RecoverMagicV，百分比 效果：基础恢复魔法效果+X.X
                //case MagicActionIDs.LIFESTEALPERCENT:   //击中恢复效果：LifeStealPercent，百分比 效果：击中恢复生命*（1+X.X）             
                    {
                        ExtPropIndexes eExtProp = ExtPropIndexes.Max;

                        switch (id)
                        {
                            case MagicActionIDs.AddAttackPercent:
                                eExtProp = ExtPropIndexes.AddAttackPercent;
                                break;
                            case MagicActionIDs.AddDefensePercent:
                                eExtProp = ExtPropIndexes.AddDefensePercent;
                                break;
                            case MagicActionIDs.MAXLIFEPERCENT:
                                eExtProp = ExtPropIndexes.MaxLifePercent;
                                break;
                            case MagicActionIDs.POTION:
                                eExtProp = ExtPropIndexes.Potion;
                                break;
                            case MagicActionIDs.HOLYWATER:
                                eExtProp = ExtPropIndexes.Holywater;
                                break;
                            case MagicActionIDs.RECOVERLIFEV:
                                eExtProp = ExtPropIndexes.RecoverLifeV;
                                break;
                            //case MagicActionIDs.RECOVERMAGICV:
                            //    eExtProp = ExtPropIndexes.RecoverMagicV;
                            //    break;
                            case MagicActionIDs.LIFESTEAL:
                                eExtProp = ExtPropIndexes.LifeSteal;
                                break;
                            //case MagicActionIDs.LIFESTEALPERCENT:
                            //    eExtProp = ExtPropIndexes.LifeStealPercent;
                            //    break;
                            case MagicActionIDs.FATALHURT:
                                eExtProp = ExtPropIndexes.Fatalhurt;
                                break;
                            case MagicActionIDs.ADDATTACK:
                                eExtProp = ExtPropIndexes.AddAttack;
                                break;
                            case MagicActionIDs.ADDATTACKINJURE:
                                eExtProp = ExtPropIndexes.AddAttackInjure;
                                break;
                            case MagicActionIDs.HITV:
                                eExtProp = ExtPropIndexes.HitV;
                                break;
                            case MagicActionIDs.ADDDEFENSE:
                                eExtProp = ExtPropIndexes.AddDefense;
                                break;
                            case MagicActionIDs.COUNTERACTINJUREVALUE:
                                eExtProp = ExtPropIndexes.CounteractInjureValue;
                                break;
                            case MagicActionIDs.DAMAGETHORN:
                                eExtProp = ExtPropIndexes.DamageThorn;
                                break;
                            case MagicActionIDs.DODGE:
                                eExtProp = ExtPropIndexes.Dodge;
                                break;
                        }

                        if (eExtProp == ExtPropIndexes.Max)
                            break;

                        client.ClientData.PropsCacheManager.SetExtPropsSingle(nPropsSystemTypes, (int)sShengWu_slot, (int)sBuJian_slot, 1000, (int)eExtProp, actionParams[0]);
                    }
                    break;
                case MagicActionIDs.CONSTITUTION:
                    client.ClientData.PropsCacheManager.SetBaseProps(nPropsSystemTypes, (int)sShengWu_slot, (int)sBuJian_slot, (int)UnitPropIndexes.Constitution, new double[] { 0.0d, 0.0d, 0.0d, actionParams[0] });
                    break;
                case MagicActionIDs.DEXTERITY:
                    client.ClientData.PropsCacheManager.SetBaseProps(nPropsSystemTypes, (int)sShengWu_slot, (int)sBuJian_slot, (int)UnitPropIndexes.Dexterity, new double[] { 0.0d, 0.0d, actionParams[0], 0.0d });
                    break;
                case MagicActionIDs.INTELLIGENCE:
                    client.ClientData.PropsCacheManager.SetBaseProps(nPropsSystemTypes, (int)sShengWu_slot, (int)sBuJian_slot, (int)UnitPropIndexes.Intelligence, new double[] { 0.0d, actionParams[0], 0.0d, 0.0d });
                    break;
                case MagicActionIDs.STRENGTH:
                    client.ClientData.PropsCacheManager.SetBaseProps(nPropsSystemTypes, (int)sShengWu_slot, (int)sBuJian_slot, (int)UnitPropIndexes.Strength, new double[] { actionParams[0], 0.0d, 0.0d, 0.0d });
                    break;
            }
        }


        //[bing] GM命令外部使用
        public void GMSetHolyItemLvup(GameClient client, sbyte sShengWu_slot, sbyte sBuJian_slot, sbyte sLv)
        {
            Dictionary<sbyte, HolyItemData> holyitemdata = client.ClientData.MyHolyItemDataDic;
            HolyItemData tmpdata = null;
            HolyItemPartData tmppartdata = null;

            //先取得当前圣物部件等级
            if (null == holyitemdata || false == holyitemdata.TryGetValue(sShengWu_slot, out tmpdata))
                return;

            if (false == tmpdata.m_PartArray.TryGetValue(sBuJian_slot, out tmppartdata))
                return;

            tmppartdata.m_sSuit = sLv;

            if (tmppartdata.m_sSuit > MAX_HOLY_PART_LEVEL)
                tmppartdata.m_sSuit = MAX_HOLY_PART_LEVEL;

            //计算部件属性
            UpdateHolyItemBuJianAttr(client, sShengWu_slot, sBuJian_slot);

            //计算圣物额外属性
            UpdataHolyItemExAttr(client, sShengWu_slot);

            // 通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            //更新db
            UpdateHolyItemData2DB(client, sShengWu_slot, sBuJian_slot, tmppartdata);

            //发送给客户端更新数据
            HolyItemSendToClient(client, sShengWu_slot, sBuJian_slot);
        }
    }
}
