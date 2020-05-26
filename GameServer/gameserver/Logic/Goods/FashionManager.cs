using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using System.Xml.Linq;
using GameServer.Core.GameEvent.EventOjectImpl;
using Tmsk.Contract;

namespace GameServer.Logic
{
    /// <summary>
    /// Fashion.xml  `ID`
    /// </summary>
    public static class FashionIdConsts
    {
        public static readonly int LuoLanYuYi = 1;                  // 罗兰羽翼
        public static readonly int CoupleArenaFengHuoJiaRen = 102; // 夫妻竞技场 --- 烽火佳人
    }

    /// <summary>
    /// 王城战管理
    /// </summary>
    public class FashionManager : IManager, ICmdProcessorEx, IEventListener
    {
        #region 标准接口

        private int State = 0;

        private static FashionManager instance = new FashionManager();

        public static FashionManager getInstance()
        {
            if (instance.State == 0)
            {
                instance.initialize();
            }
            return instance;
        }

        public FashionNamagerData RuntimeData = new FashionNamagerData();

        public bool initialize()
        {
            if (!InitConfig())
            {
                State = -1;
                return false;
            }

            State = 1;

            return true;
        }

        public bool startup()
        {
            //注册指令处理器
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_MODIFY_FASHION, 4, 4, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_FASHION_FORGE, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_FASHION_ACTIVE, 2, 2, getInstance());

            //向事件源注册监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerInitGame, getInstance());
            //GlobalEventSource4Scene.getInstance().registerListener((int)EventTypes.PreInstallJunQi, SceneUIClasses.LuoLanChengZhan, getInstance());

            return true;
        }

        public bool showdown()
        {
            //向事件源删除监听器
            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerInitGame, getInstance());
            //GlobalEventSource4Scene.getInstance().removeListener((int)EventTypes.PreInstallJunQi, SceneUIClasses.LuoLanChengZhan, getInstance());

            return true;
        }

        public bool destroy()
        {
            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_MODIFY_FASHION:
                    return ProcessModifyFashionCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_FASHION_FORGE:
                    return ProcessFashionForgeLevUpCmd(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_FASHION_ACTIVE:
                    return ProcessFashionActiveCmd(client, nID, bytes, cmdParams);
            }
            return true;
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventObject"></param>
        public void processEvent(EventObject eventObject)
        {
            int nID = eventObject.getEventType();
            switch (nID)
            {
                case (int)EventTypes.PlayerInitGame:
                    GameClient client = (eventObject as PlayerInitGameEventObject).getPlayer();
                    InitFashion(client);
                    break;
            }
        }

        #endregion 标准接口

        #region 初始化配置

        /// <summary>
        /// 初始化配置
        /// </summary>
        public bool InitConfig()
        {
            XElement xml = null;
            string fileName = "";

            lock (RuntimeData.Mutex)
            {
                try
                {
                    RuntimeData.FashionTabDict.Clear();

                    fileName = "Config/FashionTab.xml";
                    string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        FashionTabData item = new FashionTabData();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.Name = Global.GetSafeAttributeStr(node, "Name");
                        item.Categoriy = (int)Global.GetSafeAttributeLong(node, "Categoriy");
                        RuntimeData.FashionTabDict.Add(item.ID, item);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    RuntimeData.FashingDict.Clear();

                    fileName = "Config/Fashion.xml";
                    string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        FashionData item = new FashionData();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.TabID = (int)Global.GetSafeAttributeLong(node, "Tab");
                        item.Name = Global.GetSafeAttributeStr(node, "Name");
                        item.GoodsID = (int)Global.GetSafeAttributeLong(node, "Goods");
                        item.Type = (int)Global.GetSafeAttributeLong(node, "Type");
                       // item.Parameter = (int)Global.GetSafeAttributeLong(node, "Parameter");
                        item.Time = (int)Global.GetSafeAttributeLong(node, "Time");

                        RuntimeData.FashingDict.Add(item.ID, item);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    RuntimeData.FashionBagDict.Clear();

                    fileName = "Config/ShiZhuangLevelup.xml";
                    string fullPathFileName = Global.GameResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        FashionBagData item = new FashionBagData();
                        item.ID = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.GoodsID = (int)Global.GetSafeAttributeLong(node, "GoodsID");
                        item.ForgeLev = (int)Global.GetSafeAttributeLong(node, "level");
                        item.LimitTime = (int)Global.GetSafeAttributeLong(node, "Time");

                        // 提升所需物品
                        string TempValueString = Global.GetSafeAttributeStr(node, "NeedGoods");
                        string[] ValueFileds = TempValueString.Split(',');
                        if(ValueFileds.Length == 2)
                        {
                            item.NeedGoodsID = Global.SafeConvertToInt32(ValueFileds[0]);
                            item.NeedGoodsCount = Global.SafeConvertToInt32(ValueFileds[1]);
                        }
       
                        // 属性加成
                        TempValueString = Global.GetSafeAttributeStr(node, "ProPerty");
                        ValueFileds = TempValueString.Split('|');
                        foreach(var value in ValueFileds)
                        {
                            string[] KvpFileds = value.Split(',');
                            if(KvpFileds.Length == 2)
                            {
                                ExtPropIndexes index = ConfigParser.GetPropIndexByPropName(KvpFileds[0]);
                                if(index != ExtPropIndexes.Max)
                                {
                                    item.ExtProps[(int)index] = Global.SafeConvertToDouble(KvpFileds[1]);
                                }
                            }
                        }

                        // add
                        RuntimeData.FashionBagDict.Add(new KeyValuePair<int, int>(item.GoodsID, item.ForgeLev), item);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }
            }

            return true;
        }

        #endregion 初始化配置

        #region 指令处理

        /// <summary>
        /// 查询罗兰城主帮会信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public bool ProcessModifyFashionCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success_No_Info;

                int roleID = Convert.ToInt32(cmdParams[0]);
                int tabID = Convert.ToInt32(cmdParams[1]);
                int fashionID = Convert.ToInt32(cmdParams[2]);
                FashionModeTypes mode = (FashionModeTypes)Convert.ToInt32(cmdParams[3]);

                result = ModifyFashion(client, tabID, fashionID, mode);

                //发送结果给客户端
                client.sendCmd(nID, string.Format("{0}", result));
                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }

            return false;
        }

        public void InitLuoLanChengZhuFashion(GameClient client)
        {
            if (client.ClientSocket.IsKuaFuLogin)
            {
                // 跨服登录要特殊处理，因为跨服服务器没有罗兰城战数据，所以会导致罗兰城主的罗兰羽翼被卸下来
                // 先判断是否是帮主，然后再从原服数据库加载
                if (client.ClientData.Faction <= 0 || client.ClientData.BHZhiWu != (int)ZhanMengZhiWus.ShouLing)
                {
                    DelLuoLanZhiYi(client);
                    return;
                }

                Dictionary<int, BangHuiLingDiItemData> lingdiItemDataS = JunQiManager.LoadBangHuiLingDiItemsDictFromDBServer(client.ClientSocket.ServerId);
                BangHuiLingDiItemData lingdiItemData = null;
                int lingDiID = (int)LingDiIDs.LuoLanChengZhan;
                if (lingdiItemDataS == null || !lingdiItemDataS.TryGetValue(lingDiID, out lingdiItemData))
                {
                    DelLuoLanZhiYi(client);
                    return;
                }

                if (lingdiItemData == null || client.ClientData.Faction != lingdiItemData.BHID)
                {
                    DelLuoLanZhiYi(client);
                    return;
                }
            }
            else
            {
                int lingDiID = (int)LingDiIDs.LuoLanChengZhan;
                BangHuiLingDiItemData lingdiItemData = JunQiManager.GetItemByLingDiID((int)lingDiID);
                if (lingdiItemData == null || lingdiItemData.BHID <= 0)
                {
                    DelLuoLanZhiYi(client);
                    return;
                }

                if (client.ClientData.Faction != lingdiItemData.BHID || client.ClientData.BHZhiWu != (int)ZhanMengZhiWus.ShouLing)
                {
                    DelLuoLanZhiYi(client);
                    return;
                }
            }

            GetFashionByMagic(client, 1);
        }

        public void DelLuoLanZhiYi(GameClient gameclient)
        {
            DelFashionByMagic(gameclient, 1);
        }

        public void DelFashionByMagic(GameClient client, int nFashionID)
        {
            if (client == null) return;

            FashionData fashionData = null;
            if (!RuntimeData.FashingDict.TryGetValue(nFashionID, out fashionData))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Fashion配置文件中，配置的时装物品不存在, ID={0}", nFashionID));
                return;
            }

            GoodsData goodsData = GetFashionDataByGoodsID(client, fashionData.GoodsID);
            if (goodsData == null)
                return;
            String cmdData = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, (int)ModGoodsTypes.Destroy,
               goodsData.Id, goodsData.GoodsID, 0, goodsData.Site, goodsData.GCount, goodsData.BagIndex, "");
            Global.ModifyGoodsByCmdParams(client, cmdData);
        }

        public void GetFashionByMagic(GameClient client, int nFashionID, bool isAddTime = true)
        {
            FashionData fashionData = null;
            if (!RuntimeData.FashingDict.TryGetValue(nFashionID, out fashionData))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Fashion配置文件中，配置的时装物品不存在, ID={0}", nFashionID));
                return;
            }

            int nGoodsID = fashionData.GoodsID;
            SystemXmlItem systemSZGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(nGoodsID, out systemSZGoods))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Fashion配置文件中，配置的时装物品不存在, GoodsID={0}", nGoodsID));
                return;
            }

            string strStartTime = null;
            string strEndTime = null;

            DateTime oldTime = DateTime.MinValue;
            GoodsData oldGoods = GetFashionDataByGoodsID(client, nGoodsID);
           
            int nGCount = systemSZGoods.GetIntValue("GridNum");
            if (fashionData.Time > 0)
            {
                if (oldGoods != null)
                {
                    if (DateTime.TryParse(oldGoods.Endtime, out oldTime))
                    {
                        oldGoods.Endtime = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    Global.DestroyGoods(client, oldGoods);
                }

                strStartTime = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");

                if (oldTime > DateTime.MinValue && isAddTime)
                    strEndTime = oldTime.AddSeconds((double)fashionData.Time).ToString("yyyy-MM-dd HH:mm:ss");
                else
                    strEndTime = TimeUtil.NowDateTime().AddSeconds((double)fashionData.Time).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                strStartTime = Global.ConstGoodsEndTime;
                strEndTime = Global.ConstGoodsEndTime;
            }

            if (oldGoods == null || fashionData.Time > 0)
            {
                Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, nGoodsID, nGCount, 0, "", 0, 0, (int)SaleGoodsConsts.FashionGoods, "", true, 1, Global.GetLang("使用指定道具后获取"), true, strEndTime, 0, 0, 0, 0, 0, 0, 0, true, null, null, strStartTime);
            }

            NotifyFashionList(client);
        }

        public void GetFashionByMagic(GameClient client, int nFashionID, string endTime)
        {
            FashionData fashionData = null;
            if (!RuntimeData.FashingDict.TryGetValue(nFashionID, out fashionData))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Fashion配置文件中，配置的时装物品不存在, ID={0}", nFashionID));
                return;
            }

            int nGoodsID = fashionData.GoodsID;
            SystemXmlItem systemSZGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(nGoodsID, out systemSZGoods))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Fashion配置文件中，配置的时装物品不存在, GoodsID={0}", nGoodsID));
                return;
            }

            string strStartTime = null;
            string strEndTime = null;

            DateTime oldTime = DateTime.MinValue;
            GoodsData oldGoods = GetFashionDataByGoodsID(client, nGoodsID);
            int nGCount = systemSZGoods.GetIntValue("GridNum");
            if (oldGoods == null || oldGoods.Endtime != endTime)
            {
                if (oldGoods != null)
                {
                    oldGoods.Endtime = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
                    Global.DestroyGoods(client, oldGoods);
                }

                strStartTime = Global.ConstGoodsEndTime;
                strEndTime = endTime;
                Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, nGoodsID, nGCount, 0, "", 0, 0, (int)SaleGoodsConsts.FashionGoods, "", true, 1, Global.GetLang("使用指定道具后获取"), true, strEndTime, 0, 0, 0, 0, 0, 0, 0, true, null, null, strStartTime);
                NotifyFashionList(client);
            }
        }

        public static void NotifyFashionList(GameClient client)
        {
            byte[] bytesData = DataHelper.ObjectToBytes<List<GoodsData>>(client.ClientData.FashionGoodsDataList);
            GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_SPR_GET_FASHION_SLIST);
        }

        public bool FashionCanAdd(GameClient client, int nFashionID)
        {
            FashionData fashionData = null;
            if (!RuntimeData.FashingDict.TryGetValue(nFashionID, out fashionData))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Fashion配置文件中，配置的时装物品不存在, ID={0}", nFashionID));
                return false;
            }

            if (fashionData.Time <= 0)
            {
                int nGoodsID = fashionData.GoodsID;
                GoodsData oldGoods = GetFashionDataByGoodsID(client, nGoodsID);
                if (oldGoods != null) return false;
            }

            return true;
        }

        /// <summary>
        /// 使用或卸载时装
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tabID"></param>
        /// <param name="fashionID"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public int ModifyFashion(GameClient client, int tabID, int fashionID, FashionModeTypes mode)
        {
            int result = StdErrorCode.Error_Success_No_Info;

            do
            {
                if (mode <= FashionModeTypes.None || mode >= FashionModeTypes.Max)
                {
                    result = StdErrorCode.Error_Invalid_Operation;
                    break;
                }

                lock (RuntimeData.Mutex)
                {
                    FashionData fashionData;
                    if (!RuntimeData.FashingDict.TryGetValue(fashionID, out fashionData))
                    {
                        result = StdErrorCode.Error_Not_Exist;
                        break;
                    }
                    if (mode == FashionModeTypes.Load)
                    {
                        result = ValidateFashion(client, fashionData.Type, fashionData.GoodsID);
                        if (result >= StdErrorCode.Error_Success_No_Info)
                        {
                            result = LoadFashion(client, fashionData);
                        }
                    }
                    else if (mode == FashionModeTypes.Unload)
                    {
                        if (RuntimeData.FashionTabDict.ContainsKey(tabID))
                        {
                            result = UnloadFashion(client, fashionData, false);
                        }
                    }
                }
            } while (false);

            return result;
        }

        /// <summary>
        /// 时装是否可用
        /// </summary>
        /// <param name="client"></param>
        /// <param name="fashionData"></param>
        /// <returns></returns>
        public int ValidateFashion(GameClient client, int fashionType, int GoodsID)
        {
            //判断时装使用条件是否满足
            if (fashionType == (int)FashionTypes.LuoLanYuYi)
            {
                if (client.ClientData.Faction <= 0 || client.ClientData.BHZhiWu != (int)ZhanMengZhiWus.ShouLing)
                {
                    return StdErrorCode.Error_Is_Not_LuoLanChengZhu;
                }

                int lingDiID = (int)LingDiIDs.LuoLanChengZhan;
                BangHuiLingDiItemData lingdiItemData = null;
                if (client.ClientSocket.IsKuaFuLogin)
                {
                    // 跨服服务器没有帮会罗兰城战领地数据，从原服加载
                    Dictionary<int, BangHuiLingDiItemData> itemDatas = JunQiManager.LoadBangHuiLingDiItemsDictFromDBServer(client.ServerId);
                    if (itemDatas != null)
                    {
                        itemDatas.TryGetValue(lingDiID, out lingdiItemData);
                    }
                }
                else
                {
                    lingdiItemData = JunQiManager.GetItemByLingDiID((int)lingDiID);
                }

                if (lingdiItemData == null || lingdiItemData.BHID <= 0)
                {
                    return StdErrorCode.Error_Is_Not_LuoLanChengZhu;
                }

                if (client.ClientData.Faction != lingdiItemData.BHID || client.ClientData.BHZhiWu != (int)ZhanMengZhiWus.ShouLing)
                {
                    return StdErrorCode.Error_Is_Not_LuoLanChengZhu;
                }
                return StdErrorCode.Error_Success_No_Info;
            }
            else if (fashionType == (int)FashionTypes.Normal)//普通的 时装翅膀称号
            {
                GoodsData goodsData = null;
                goodsData = GetFashionDataByGoodsID(client, GoodsID);//判断有没有这个道具
                if (goodsData != null)
                    return StdErrorCode.Error_Success_No_Info;
                else
                    return StdErrorCode.Error_Operation_Denied;
            }
            else if (fashionType == (int)FashionTypes.Married) //结婚才可以用
            {
                if (client.ClientData.MyMarriageData.byMarrytype > 0)
                {
                    return StdErrorCode.Error_Success_No_Info;
                }
                else
                {
                    return StdErrorCode.Error_Is_Not_Married;
                }
            }
            else
            {
                return StdErrorCode.Error_Config_Fault;
            }
        }

        /// <summary>
        /// 登录时初始化时装信息
        /// </summary>
        /// <param name="client"></param>
        public void InitFashion(GameClient client)
        {
            InitLuoLanChengZhuFashion(client);
            int usingFashionID = GetFashionWingsID(client);
            FashionData fashionData;
            if (usingFashionID > 0)
            {
                fashionData = null;
                if (RuntimeData.FashingDict.TryGetValue(usingFashionID, out fashionData))
                {
                    if (ValidateFashion(client, fashionData.Type, fashionData.GoodsID) >= StdErrorCode.Error_Success_No_Info)
                    {
                        LoadFashion(client, fashionData);
                    }
                    else
                    {
                        UnloadFashion(client, fashionData, false);
                    }
                }
            }
            //称号
            usingFashionID = GetFashionTitleID(client);
            if (usingFashionID > 0)
            {
                fashionData = null;
                if (RuntimeData.FashingDict.TryGetValue(usingFashionID, out fashionData))
                {
                    if (ValidateFashion(client, fashionData.Type, fashionData.GoodsID) >= StdErrorCode.Error_Success_No_Info)
                    {
                        LoadFashion(client, fashionData);
                    }
                    else
                    {
                        UnloadFashion(client, fashionData, false);
                    }
                }
            }

            //时装衣橱
            InitFashionBag(client);

            //刷新所有称号时装属性
            RefreshTitleFashionProps(client);
        }

        /// <summary>
        /// 添加所有称号时装属性
        /// </summary>
        private void RefreshTitleFashionProps(GameClient client)
        {
            bool propsChanged = false;
            if (null != client.ClientData.FashionGoodsDataList)
            {
                List<GoodsData> fashionGoodsDataList;
                lock (client.ClientData.FashionGoodsDataList)
                {
                    fashionGoodsDataList = new List<GoodsData>(client.ClientData.FashionGoodsDataList);
                }

                lock (RuntimeData.Mutex)
                {
                    foreach (var goodsData in fashionGoodsDataList)
                    {
                        // 称号
                        foreach (var fashionData in RuntimeData.FashingDict.Values)
                        {
                            if (fashionData.GoodsID == goodsData.GoodsID && fashionData.TabID == (int)FashionTabs.Title)
                            {
                                //设置时装属性
                                EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(fashionData.GoodsID);
                                if (null != item)
                                {
                                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.FashionByGoodsProps, fashionData.TabID, fashionData.ID, item.ExtProps);
                                    propsChanged = true;
                                }
                            }
                        }

                        // 时装衣橱
                        foreach (var fashionBagData in RuntimeData.FashionBagDict.Values)
                        {
                            int nCategories = Global.GetGoodsCatetoriy(goodsData.GoodsID);
                            if (fashionBagData.GoodsID == goodsData.GoodsID && goodsData.Forge_level == fashionBagData.ForgeLev && nCategories == (int)ItemCategories.ShiZhuang)
                            {
                                client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.FashionByGoodsProps, (int)FashionTabs.Fashion, fashionBagData.GoodsID, fashionBagData.ExtProps);
                                propsChanged = true;
                            }
                        }
                    }
                }
            }

            if (propsChanged)
            {
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
        }

        /// <summary>
        /// 时装衣橱初始化
        /// </summary>
        public void InitFashionBag(GameClient client)
        {
            GoodsData goodsData = client.UsingEquipMgr.GetGoodsDataByCategoriy(client, (int)ItemCategories.ShiZhuang);
            if (null == goodsData || goodsData.Site == (int)SaleGoodsConsts.FashionGoods) // 没有穿时装 || 存在从衣橱穿上的时装
                return;

            // 卸下
            string cmdData = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, (int)ModGoodsTypes.EquipUnload,
                                    goodsData.Id, goodsData.GoodsID, 0, goodsData.Site, goodsData.GCount, goodsData.BagIndex, "");
            Global.ModifyGoodsByCmdParams(client, cmdData);

            // 移动到衣橱
            cmdData = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, (int)ModGoodsTypes.ModValue,
                                    goodsData.Id, goodsData.GoodsID, goodsData.Using, (int)SaleGoodsConsts.FashionGoods, goodsData.GCount, goodsData.BagIndex, "");
            Global.ModifyGoodsByCmdParams(client, cmdData);

            // 穿戴上
            cmdData = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, (int)ModGoodsTypes.EquipLoad,
                        goodsData.Id, goodsData.GoodsID, 1, (int)SaleGoodsConsts.FashionGoods, goodsData.GCount, goodsData.BagIndex, "");
            Global.ModifyGoodsByCmdParams(client, cmdData);
        }

        /// <summary>
        /// 是否可以激活时装
        /// </summary>
        public bool FashionCanActive(GameClient client, GoodsData goodsData)
        {
            int nCategories = Global.GetGoodsCatetoriy(goodsData.GoodsID);
            if (nCategories != (int)ItemCategories.ShiZhuang)
                return false;

            if (client.ClientData.FashionGoodsDataList == null)
                return true;

            List<GoodsData> fashionGoodsDataList;
            lock (client.ClientData.FashionGoodsDataList)
            {
                fashionGoodsDataList = new List<GoodsData>(client.ClientData.FashionGoodsDataList);
            }

            // 检查是否有重复的时装 限时时装可以反复激活
            foreach (var item in fashionGoodsDataList)
            {
                if (item.GoodsID == goodsData.GoodsID && !Global.IsTimeLimitGoods(item))
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// 时装激活
        /// </summary>
        private bool ProcessFashionActiveCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success_No_Info;

                int roleID = Convert.ToInt32(cmdParams[0]);
                int GoodsDbID = Convert.ToInt32(cmdParams[1]);

                //从背包中查找物品
                GoodsData goodsData = Global.GetGoodsByDbID(client, GoodsDbID);
                if (null == goodsData)
                {
                    result = StdErrorCode.Error_Invalid_DBID;
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}", result, roleID, GoodsDbID));
                    return true;
                }

                // 是否可以激活时装
                if(!FashionCanActive(client, goodsData))
                {
                    result = StdErrorCode.Error_Operation_Denied;
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}", result, roleID, GoodsDbID));
                    return true;
                }

                // 尝试找到对应的配置文件
                FashionBagData FashionDataCurrentLev = null;
                lock(RuntimeData.Mutex)
                {
                    KeyValuePair<int, int> key = new KeyValuePair<int, int>(goodsData.GoodsID, goodsData.Forge_level);
                    if (!RuntimeData.FashionBagDict.TryGetValue(key, out FashionDataCurrentLev))
                    {
                        result = StdErrorCode.Error_Level_Reach_Max_Level;
                        client.sendCmd(nID, string.Format("{0}:{1}:{2}", result, roleID, GoodsDbID));
                        return true;
                    }
                }

                // 在背包中扣1个相应的时装
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                    Global._TCPManager.TcpOutPacketPool, client, goodsData, 1, false, true))
                {
                    result = StdErrorCode.Error_Goods_Not_Enough;
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}", result, roleID, GoodsDbID));
                    return true;
                }

                // 衣橱内的时装
                DateTime oldTime = DateTime.MinValue;
                GoodsData oldGoods = GetFashionDataByGoodsID(client, goodsData.GoodsID);

                // 衣橱内现有时装的相关数据
                int oldGoodsUsing = 0;
                int oldGoodsForgeLev = 0;
                if(null != oldGoods)
                {
                    oldGoodsUsing = oldGoods.Using;
                    oldGoodsForgeLev = oldGoods.Forge_level;
                }

                // 限时时装
                string strStartTime = null;
                string strEndTime = null;
                if (FashionDataCurrentLev.LimitTime > 0)
                {
                    if (oldGoods != null)
                    {
                        if (DateTime.TryParse(oldGoods.Endtime, out oldTime))
                        {
                            oldGoods.Endtime = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
                        }

                        Global.DestroyGoods(client, oldGoods);
                    }

                    strStartTime = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");

                    if (oldTime > DateTime.MinValue)
                        strEndTime = oldTime.AddSeconds((double)FashionDataCurrentLev.LimitTime).ToString("yyyy-MM-dd HH:mm:ss");
                    else
                        strEndTime = TimeUtil.NowDateTime().AddSeconds((double)FashionDataCurrentLev.LimitTime).ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    strStartTime = Global.ConstGoodsEndTime;
                    strEndTime = Global.ConstGoodsEndTime;
                }

                //向DBServer请求加入某个新的物品到时装到衣橱
                if (oldGoods != null)
                {
                    int NewGoodsDBID = Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, 1, 0, "", oldGoodsForgeLev, goodsData.Binding,
                        (int)SaleGoodsConsts.FashionGoods, "", true, 1, Global.GetLang("时装激活"), true, strEndTime, 0, 0, 0, 0, 0, 0, 0, true, null, null, strStartTime);

                    // 原先在佩戴状态 维持穿着状态
                    if (oldGoodsUsing > 0)
                    {
                        string cmdData = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, (int)ModGoodsTypes.EquipLoad,
                                    NewGoodsDBID, goodsData.GoodsID, 1, (int)SaleGoodsConsts.FashionGoods, 1, 0, "");
                        Global.ModifyGoodsByCmdParams(client, cmdData);
                    }
                }
                else
                {
                    Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, 1, 0, "", 0, goodsData.Binding,
                        (int)SaleGoodsConsts.FashionGoods, "", true, 1, Global.GetLang("时装激活"), true, strEndTime, 0, 0, 0, 0, 0, 0, 0, true, null, null, strStartTime);
                }

                //同步新的时装列表
                NotifyFashionList(client);

                // 发送结果给客户端
                client.sendCmd(nID, string.Format("{0}:{1}:{2}", result, roleID, GoodsDbID));
                return true;
            }
            catch (Exception ex)
            {
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
            return false;
        }

        /// <summary>
        /// 时装强化
        /// </summary>
        private bool ProcessFashionForgeLevUpCmd(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int result = StdErrorCode.Error_Success_No_Info;

                int roleID = Convert.ToInt32(cmdParams[0]);
                int GoodsDbID = Convert.ToInt32(cmdParams[1]);

                GoodsData goodsData = GetFashionGoodsDataByDbID(client, GoodsDbID);
                if(null == goodsData)
                {
                    result = StdErrorCode.Error_Invalid_DBID;
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, 0));
                    return true;
                }

                // 时限时装不可以强化
                if(Global.IsTimeLimitGoods(goodsData))
                {
                    result = StdErrorCode.Error_Invalid_Operation;
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, 0));
                    return true;
                }

                // 尝试找到对应的配置文件
                FashionBagData FashionDataCurrentLev = null;
                FashionBagData FashionDataNextLev = null;
                lock(RuntimeData.Mutex)
                {
                    KeyValuePair<int, int> key = new KeyValuePair<int, int>(goodsData.GoodsID, goodsData.Forge_level);
                    if (!RuntimeData.FashionBagDict.TryGetValue(key, out FashionDataCurrentLev))
                    {
                        result = StdErrorCode.Error_Level_Reach_Max_Level;
                        client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, 0));
                        return true;
                    }

                    key = new KeyValuePair<int, int>(goodsData.GoodsID, goodsData.Forge_level + 1);
                    if (!RuntimeData.FashionBagDict.TryGetValue(key, out FashionDataNextLev))
                    {
                        result = StdErrorCode.Error_Level_Reach_Max_Level;
                        client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, 0));
                        return true;
                    }
                }

                int nBindGoodNum = Global.GetTotalBindGoodsCountByID(client, FashionDataNextLev.NeedGoodsID);
                int nNotBindGoodNum = Global.GetTotalNotBindGoodsCountByID(client, FashionDataNextLev.NeedGoodsID);

                // 数量不够
                if (FashionDataNextLev.NeedGoodsCount > nBindGoodNum + nNotBindGoodNum)
                {
                    result = StdErrorCode.Error_Goods_Not_Enough;
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, 0));
                    return true;
                }

                // 优先绑定物品
                int nSubNum = FashionDataNextLev.NeedGoodsCount;
                int nSum = 0;
                if (FashionDataNextLev.NeedGoodsCount > nBindGoodNum)
                {
                    nSum = nBindGoodNum;
                    nSubNum = FashionDataNextLev.NeedGoodsCount - nBindGoodNum;
                }
                else
                {
                    nSum = FashionDataNextLev.NeedGoodsCount;
                    nSubNum = 0;
                }

                // 扣除进阶道具
                bool usedBinding = false;
                bool usedTimeLimited = false;
                if (nSum > 0)
                {
                    if (!GameManager.ClientMgr.NotifyUseBindGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                            Global._TCPManager.TcpOutPacketPool, client, FashionDataNextLev.NeedGoodsID, nSum, false, out usedBinding, out usedTimeLimited))
                    {
                        result = StdErrorCode.Error_Goods_Not_Enough;
                        client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, 0));
                        return true;
                    }
                }

                if (nSubNum > 0)
                {
                    if (!GameManager.ClientMgr.NotifyUseNotBindGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                            Global._TCPManager.TcpOutPacketPool, client, FashionDataNextLev.NeedGoodsID, nSubNum, false, out usedBinding, out usedTimeLimited))
                    {
                        result = StdErrorCode.Error_Goods_Not_Enough;
                        client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, 0));
                        return true;
                    }
                }
                
                // 时装强化升级
                goodsData.Forge_level += 1;

                // 修改装备的数据库
                // 向DBServer请求修改物品
                string[] dbFields = null;
                string strDbCmd = Global.FormatUpdateDBGoodsStr(client.ClientData.RoleID, goodsData.Id, "*", goodsData.Forge_level, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", goodsData.Binding, "*", "*", "*", "*", "*", "*", "*");
                TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strDbCmd, out dbFields, client.ServerId);
                if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                {
                    result = StdErrorCode.Error_DB_Faild;
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, 0));
                    return true;
                }

                if (dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
                {
                    result = StdErrorCode.Error_DB_Faild;
                    client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, 0));
                    return true;
                }

                // 刷新所有称号时装属性
                RefreshTitleFashionProps(client);

                // 发送结果给客户端
                client.sendCmd(nID, string.Format("{0}:{1}:{2}:{3}", result, roleID, GoodsDbID, goodsData.Forge_level));
                return true;
            }
            catch (Exception ex)
            {
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
            return false;
        }

        /// <summary>
        /// 使用时装
        /// </summary>
        /// <param name="client"></param>
        /// <param name="fashionData"></param>
        /// <returns></returns>
        private int LoadFashion(GameClient client, FashionData fashionData)
        {
            //查找时装关联的的属性(物品表)
            EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(fashionData.GoodsID);
            if (null == item)
            {
                return StdErrorCode.Error_Config_Fault;
            }

            int usingFashionID = 0;
            if (fashionData.TabID == (int)FashionTabs.Wings) //翅膀
            {
                usingFashionID = GetFashionWingsID(client);
                if (usingFashionID != fashionData.ID)
                {
                    ModifyFashionWingsID(client, fashionData.ID);
                }

                //设置时装属性
                client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.FashionByGoodsProps, fashionData.TabID, 0, item.ExtProps);
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                return StdErrorCode.Error_Success_No_Info;
            }
            else if( fashionData.TabID == (int)FashionTabs.Title)
            {
                usingFashionID = GetFashionTitleID(client);
                if (usingFashionID != fashionData.ID)
                {
                    ModifyFashionTitleID(client, fashionData.ID);
                }

                //设置时装属性,称号类型默认已附加
                //client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.FashionByGoodsProps, fashionData.TabID, fashionData.ID, item.ExtProps);
                //GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                return StdErrorCode.Error_Success_No_Info;
            }
            else
            {
                return StdErrorCode.Error_Config_Fault;
            }
        }

        /// <summary>
        /// 卸下时装
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private int UnloadFashion(GameClient client, FashionData fashionData, bool bIsRemove)
        {
            ///检查要卸掉的翅膀 是不是 自己 正在用的 
            int usingFashionID = 0;
            if (fashionData.TabID == (int)FashionTabs.Title)
            {
                usingFashionID = GetFashionTitleID(client);
            }
            else if (fashionData.TabID == (int)FashionTabs.Wings)
            {
                usingFashionID = GetFashionWingsID(client);
            }
            if (usingFashionID != fashionData.ID)
            {
                return StdErrorCode.Error_Success_No_Info;
            }

            int nULID = 0;
            if (bIsRemove)
                nULID = -1;

            if (fashionData.TabID == (int)FashionTabs.Wings)
            {
                ModifyFashionWingsID(client, nULID);

                client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.FashionByGoodsProps, fashionData.TabID, 0, PropsCacheManager.ConstExtProps);
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
            else if (fashionData.TabID == (int)FashionTabs.Title)
            {
                ModifyFashionTitleID(client, nULID);

                //设置时装属性,称号类型默认已附加
                //client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.FashionByGoodsProps, fashionData.TabID, fashionData.ID, PropsCacheManager.ConstExtProps);
                //GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }

            return StdErrorCode.Error_Success_No_Info;
        }

        #endregion 指令处理

        /// <summary>
        /// 读取时装翅膀ID
        /// </summary>
        public int GetFashionWingsID(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.FashionWingsID);
        }

        /// <summary>
        /// 读取时装TitleID
        /// </summary>
        public int GetFashionTitleID(GameClient client)
        {
            return Global.GetRoleParamsInt32ValueWithTimeStampFromDB(client, RoleParamName.FashionTitleID);
        }

        /// <summary>
        /// 修改保存时装翅膀ID
        /// </summary>
        public void ModifyFashionWingsID(GameClient client, int nID, bool writeToDB = false, bool notifyClient = true)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.FashionWingsID, nID, true);

            if (notifyClient)
            {
                //通知自己
                GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.FashionWingsID, nID);

                string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, (int)RoleCommonUseIntParamsIndexs.FashionWingsID, nID);
                client.sendOthersCmd((int)TCPGameServerCmds.CMD_SPR_ROLEPARAMSCHANGE, strcmd);
            }
        }

        public void ModifyFashionTitleID(GameClient client, int nID, bool writeToDB = false, bool notifyClient = true)
        {
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.FashionTitleID, nID, true);

            if (notifyClient)
            {
                //通知自己
                GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.FashionTitleID, nID);

                string strcmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, (int)RoleCommonUseIntParamsIndexs.FashionTitleID, nID);
                client.sendOthersCmd((int)TCPGameServerCmds.CMD_SPR_ROLEPARAMSCHANGE, strcmd);
            }
        }

        #region 事件处理

        /// <summary>
        /// 更新罗兰城主时装
        /// </summary>
        /// <param name="bhid"></param>
        public void UpdateLuoLanChengZhuFasion(int bhid)
        {
            int roleID = -1;
            int fashionID = 0;
            BangHuiDetailData bangHuiDetailData = Global.GetBangHuiDetailData(roleID, bhid);
            lock (RuntimeData.Mutex)
            {
                foreach (var item in RuntimeData.FashingDict.Values)
                {
                    if (item.Type == (int)FashionTypes.LuoLanYuYi)
                    {
                        fashionID = item.ID;
                        break;
                    }
                }
            }

            if (bangHuiDetailData != null && fashionID > 0)
            {
                GameClient oldClient = GameManager.ClientMgr.FindClient(RuntimeData.LuoLanChengZhuRoleID);
                if (null != oldClient && bangHuiDetailData.BZRoleID != oldClient.ClientData.RoleID)
                {
                    //去掉上次城主的罗兰羽翼
                    //ModifyFashion(oldClient, (int)FashionTabs.Wings, fashionID, FashionModeTypes.Unload);

                    //删除原来的罗兰之翼道具
                    DelLuoLanZhiYi(oldClient);
                }

                GameClient newClient = GameManager.ClientMgr.FindClient(bangHuiDetailData.BZRoleID);
                if (null != newClient)
                {
                    //添加罗兰之翼道具
                    GetFashionByMagic(newClient, 1, false);
                    //ModifyFashion(newClient, (int)FashionTabs.Wings, fashionID, FashionModeTypes.Load);
                }
                RuntimeData.LuoLanChengZhuRoleID = bangHuiDetailData.BZRoleID;
            }
        }

        #endregion 事件处理

        #region 时装仓库物品管理
        //panghui add

        /// <summary>
        /// 根据物品DbID获取时装仓库物品的信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GoodsData GetFashionGoodsDataByDbID(GameClient client, int id)
        {
            if (null == client.ClientData.FashionGoodsDataList)
            {
                return null;
            }
            lock (client.ClientData.FashionGoodsDataList)
            {
                for (int i = 0; i < client.ClientData.FashionGoodsDataList.Count; i++)
                {
                    if (client.ClientData.FashionGoodsDataList[i].Id == id)
                    {
                        return client.ClientData.FashionGoodsDataList[i];
                    }
                }
            }
            return null;
        }
        
        public static GoodsData GetFashionDataByGoodsID(GameClient client, int id)
        {
            if (null == client.ClientData.FashionGoodsDataList)
            {
                return null;
            }
            lock (client.ClientData.FashionGoodsDataList)
            {
                for (int i = 0; i < client.ClientData.FashionGoodsDataList.Count; i++)
                {
                    if (client.ClientData.FashionGoodsDataList[i].GoodsID == id)
                    {
                        return client.ClientData.FashionGoodsDataList[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 添加时装仓库物品
        /// </summary>
        /// <param name="goodsData"></param>
        public void AddFashionGoodsData(GameClient client, GoodsData goodsData)
        {
            if (goodsData.Site != (int)SaleGoodsConsts.FashionGoods) return;

            if (null == client.ClientData.FashionGoodsDataList)
            {
                client.ClientData.FashionGoodsDataList = new List<GoodsData>();
            }

            lock (client.ClientData.FashionGoodsDataList)
            {
                client.ClientData.FashionGoodsDataList.Add(goodsData);
            }

            //刷新所有称号时装属性
            RefreshTitleFashionProps(client);
        }

        public GoodsData AddFashionGoodsData(GameClient client, int id, int goodsID, int forgeLevel, int quality, int goodsNum, int binding, int site, string jewelList, string endTime,
            int addPropIndex, int bornIndex, int lucky, int strong, int ExcellenceProperty, int nAppendPropLev, int nEquipChangeLife)
        {
            GoodsData gd = new GoodsData()
            {
                Id = id,
                GoodsID = goodsID,
                Using = 0,
                Forge_level = forgeLevel,
                Starttime = "1900-01-01 12:00:00",
                Endtime = endTime,
                Site = site,
                Quality = quality,
                Props = "",
                GCount = goodsNum,
                Binding = binding,
                Jewellist = jewelList,
                BagIndex = 0,
                AddPropIndex = addPropIndex,
                BornIndex = bornIndex,
                Lucky = lucky,
                Strong = strong,
                ExcellenceInfo = ExcellenceProperty,
                AppendPropLev = nAppendPropLev,
                ChangeLifeLevForEquip = nEquipChangeLife,
            };

            AddFashionGoodsData(client, gd);
            return gd;
        }
        /// <summary>
        /// 删除时装仓库物品
        /// </summary>
        /// <param name="goodsData"></param>
        public void RemoveFashionGoodsData(GameClient client, GoodsData goodsData)
        {
            if (null == client.ClientData.FashionGoodsDataList) return;
            if (null == goodsData) return;

            ///检查删除的时装 是不是 正在使用的时装
            FashionData fashionData = null;
            foreach (var item in FashionManager.getInstance().RuntimeData.FashingDict.Values)
            {
                if (item.GoodsID == goodsData.GoodsID)
                {
                    fashionData = item;
                    break;
                }
            }

            if (fashionData != null)
            {
               UnloadFashion(client, fashionData, true);
            }

            lock (client.ClientData.FashionGoodsDataList)
            {
                if (null != fashionData) // 称号、翅膀
                {
                    EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(fashionData.GoodsID);
                    if (null != item)
                        client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.FashionByGoodsProps, fashionData.TabID, fashionData.ID, PropsCacheManager.ConstExtProps);
                }

                // 时装衣橱
                int nCategories = Global.GetGoodsCatetoriy(goodsData.GoodsID);
                if (nCategories == (int)ItemCategories.ShiZhuang)
                {
                    client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.FashionByGoodsProps, (int)FashionTabs.Fashion, goodsData.GoodsID, PropsCacheManager.ConstExtProps);
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }
                client.ClientData.FashionGoodsDataList.Remove(goodsData);
            }

            //刷新所有称号时装属性
            RefreshTitleFashionProps(client);
        }

        public static int GetFashionGoodsDataCount(GameClient client)
        {
            if (null == client.ClientData.FashionGoodsDataList)
            {
                return 0;
            }

            return client.ClientData.FashionGoodsDataList.Count;
        }
        public static TCPProcessCmdResults ProcessGetFashionList(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                if (1 != fields.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                byte[] bytesData = DataHelper.ObjectToBytes<List<GoodsData>>(client.ClientData.FashionGoodsDataList);
                GameManager.ClientMgr.SendToClient(client, bytesData, nID);

                return TCPProcessCmdResults.RESULT_OK;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessGetElementHrtList", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }

        #endregion 时装仓库物品管理

    }
}
