using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic.FluorescentGem
{
    /// <summary>
    /// 荧光宝石管理器 [XSea 2015/8/7]
    /// </summary>
    public class FluorescentGemManager
    {
        #region 成员变量
        /// <summary>
        /// 荧光宝石矿坑类型表 key=矿坑类型
        /// </summary>
        private Dictionary<int, FluorescentGemLevelTypeConfigData> FluorescentGemLevelTypeConfigDict = new Dictionary<int, FluorescentGemLevelTypeConfigData>();

        /// <summary>
        /// 荧光宝石挖掘表 key=矿坑类型
        /// </summary>
        private Dictionary<int, List<FluorescentGemDigConfigData>> FluorescentGemDigConfigDict = new Dictionary<int,List<FluorescentGemDigConfigData>>();

        /// <summary>
        /// 荧光宝石升级表 key=宝石id
        /// </summary>
        private Dictionary<int, FluorescentGemUpConfigData> FluorescentGemUpConfigDict = new Dictionary<int, FluorescentGemUpConfigData>();
        #endregion

        #region private函数

        #region 读取配置文件
        #region 读取矿坑类型配置文件
        /// <summary>
        /// 读取矿坑类型配置文件
        /// </summary>
        private void LoadFluorescentGemLevelTypeConfigData()
        {
            try
            {
                lock (FluorescentGemLevelTypeConfigDict)
                {
                    string fileName = FluorescentGemDefine.LEVEL_TYPE_PATH; // 配置文件地址
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName)); // 移除缓存
                    XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName)); // 获取XML
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件异常", fileName));
                        return;
                    }

                    IEnumerable<XElement> xmlItems = xml.Elements();

                    // 清空容器
                    FluorescentGemLevelTypeConfigDict.Clear();

                    // 放入容器管理 key = 矿坑类型，value = FluorescentGemLevelTypeConfigData
                    foreach (var xmlItem in xmlItems)
                    {
                        FluorescentGemLevelTypeConfigData tmpData = new FluorescentGemLevelTypeConfigData();
                        int nTypeID = (int)Global.GetSafeAttributeLong(xmlItem, "Type"); // 矿坑类型
                        tmpData._NeedFluorescentPoint = (int)Global.GetSafeAttributeLong(xmlItem, "CostYingGuangFenMo"); // 消耗荧光粉末
                        tmpData._NeedDiamond = (int)Global.GetSafeAttributeLong(xmlItem, "CostZuanShi"); // 消耗钻石

                        // 加入字典
                        FluorescentGemLevelTypeConfigDict.Add(nTypeID, tmpData);
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("load xml file : {0} fail", string.Format("Config/SystemParams.xml-LoadFluorescentGemLevelTypeConfigData")));
            }
        }
        #endregion

        #region 读取荧光宝石挖掘配置文件
        /// <summary>
        /// 读取荧光宝石挖掘配置文件
        /// </summary>
        private void LoadFluorescentGemDigConfigData()
        {
            try
            {
                lock (FluorescentGemDigConfigDict)
                {
                    string fileName = FluorescentGemDefine.DIG_PATH; // 配置文件地址
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName)); // 移除缓存
                    XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName)); // 获取XML
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件异常", fileName));
                        return;
                    }

                    IEnumerable<XElement> xmlItems = xml.Elements();

                    // 清空容器
                    FluorescentGemDigConfigDict.Clear();

                    // 放入容器管理 key = 矿坑类型，value = FluorescentGemDigConfigData
                    foreach (var xmlItem in xmlItems)
                    {
                        int nTypeID = (int)Global.GetSafeAttributeLong(xmlItem, "TypeID"); // 矿坑类型
                        
                        List<FluorescentGemDigConfigData> list = new List<FluorescentGemDigConfigData>();
                        IEnumerable<XElement> xmlDatas = xmlItem.Elements(); // 再找内层数据
                        foreach (var xmlData in xmlDatas)
                        {
                            FluorescentGemDigConfigData tmpData = new FluorescentGemDigConfigData();
                            tmpData._GoodsID = (int)Global.GetSafeAttributeLong(xmlData, "GoodsID"); // 宝石id
                            tmpData._StartValue = (int)Global.GetSafeAttributeLong(xmlData, "StartValues"); // 几率起始值
                            tmpData._EndValue = (int)Global.GetSafeAttributeLong(xmlData, "EndValues"); // 几率结束值

                            list.Add(tmpData);
                        }

                        // 加入字典
                        FluorescentGemDigConfigDict.Add(nTypeID, list);
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("load xml file : {0} fail", string.Format("Config/SystemParams.xml-FluorescentGemDigConfigDict")));
            }
        }
        #endregion

        #region 读取荧光宝石升级配置文件
        /// <summary>
        /// 读取矿坑类型配置文件
        /// </summary>
        private void LoadFluorescentGemUpConfigData()
        {
            try
            {
                lock (FluorescentGemUpConfigDict)
                {
                    string fileName = FluorescentGemDefine.UP_PATH; // 配置文件地址
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName)); // 移除缓存
                    XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName)); // 获取XML
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件异常", fileName));
                        return;
                    }

                    IEnumerable<XElement> xmlItems = xml.Elements();

                    // 清空容器
                    FluorescentGemUpConfigDict.Clear();

                    // 放入容器管理 key = 宝石id，value = FluorescentGemUpConfigDict
                    foreach (var xmlItem in xmlItems)
                    {
                        FluorescentGemUpConfigData tmpData = new FluorescentGemUpConfigData();
                        int nGoodsID = (int)Global.GetSafeAttributeLong(xmlItem, "GoodsID"); // 宝石id
                        tmpData._ElementsType = (int)Global.GetSafeAttributeLong(xmlItem, "ElementsTypeID"); // 元素类型
                        tmpData._GemType = (int)Global.GetSafeAttributeLong(xmlItem, "GemTypeID"); // 宝石类型
                        tmpData._Level = (int)Global.GetSafeAttributeLong(xmlItem, "Level"); // 宝石等级
                        tmpData._OldGoodsID = (int)Global.GetSafeAttributeLong(xmlItem, "OldGoodsID"); // 上一级宝石id
                        tmpData._NewGoodsID = (int)Global.GetSafeAttributeLong(xmlItem, "NewGoodsID"); // 下一级宝石id
                        tmpData._NeedOldGoodsCount = (int)Global.GetSafeAttributeLong(xmlItem, "NeedOldGoodsNum"); // 所需上一级宝石数量
                        tmpData._NeedLevelOneGoodsCount = (int)Global.GetSafeAttributeLong(xmlItem, "NeedOneLevelNum"); // 所需1级宝石数量
                        tmpData._NeedGold = (int)Global.GetSafeAttributeLong(xmlItem, "CostBandJinBi"); // 消耗金币

                        // 加入字典
                        FluorescentGemUpConfigDict.Add(nGoodsID, tmpData);
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("load xml file : {0} fail", string.Format("Config/SystemParams.xml-LoadFluorescentGemLevelTypeConfigData")));
            }
        }
        #endregion
        #endregion

        #region 检查是否为相同类型宝石
        /// <summary>
        /// 检查是否为相同类型宝石
        /// </summary>
        /// <param name="data1"></param>
        /// <param name="data2"></param>
        /// <returns></returns>
        private bool IsSameGem(FluorescentGemUpConfigData data1, FluorescentGemUpConfigData data2)
        {
            if (null == data1 || null == data2)
                return false;

            // ElementsType、GemType、均相同的宝石
            if (data1._ElementsType == data2._ElementsType &&
                data1._GemType == data2._GemType)
                return true;

            return false;
        }
        #endregion

        #region 检查荧光宝石装备栏位索引
        /// <summary>
        /// 检查装备栏位索引
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        private bool CheckEquipPositionIndex(int nIndex)
        {
            if (nIndex > (int)FluorescentGemEquipPosition.Start && nIndex < (int)FluorescentGemEquipPosition.End)
                return true;
            return false;
        }
        #endregion

        #region 检查荧光宝石类型
        /// <summary>
        /// 检查装备栏位索引
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        private bool CheckGemTypeIndex(int nIndex)
        {
            if (nIndex > (int)FluorescentGemType.Start && nIndex < (int)FluorescentGemType.End)
                return true;
            return false;
        }
        #endregion

        #region 获取分解宝石获得荧光粉末的个数
        /// <summary>
        /// 获取分解宝石获得荧光粉末的个数
        /// </summary>
        /// <param name="nGoodsID"></param>
        /// <returns></returns>
        private int GetFluorescentPointByGoodsID(int nGoodsID)
        {
            //获取Xml项
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(nGoodsID, out systemGoods))
            {
                return 0;
            }

            return systemGoods.GetIntValue("ChangeYingGuang");
        }
        #endregion

        #region 获取荧光宝石背包剩余空间
        /// <summary>
        /// 获取荧光宝石背包剩余空间
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private int GetFluorescentGemBagSpace(GameClient client)
        {
            if (null == client)
                return 0;

            return FluorescentGemDefine.MAX_FLUORESCENT_GEM_BAG_COUNT - client.ClientData.FluorescentGemData.GemBagList.Count();
        }
        #endregion

        #region 执行荧光宝石背包整理
        /// <summary>
        /// 执行荧光宝石背包整理
        /// </summary>
        /// <param name="client"></param>
        private void ResetBagAllGoods(GameClient client)
        {
            lock (client.ClientData.FluorescentGemData)
            {
                List<GoodsData> list = client.ClientData.FluorescentGemData.GemBagList;

                Dictionary<string, GoodsData> oldGoodsDict = new Dictionary<string, GoodsData>();
                List<GoodsData> toRemovedGoodsDataList = new List<GoodsData>();
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].BagIndex = 1;
                    int gridNum = Global.GetGoodsGridNumByID(list[i].GoodsID);
                    if (gridNum <= 1)
                    {
                        continue;
                    }

                    GoodsData oldGoodsData = null;
                    string key = string.Format("{0}_{1}_{2}_{3}", list[i].GoodsID,
                        list[i].Binding,
                        Global.DateTimeTicks(list[i].Starttime),
                        Global.DateTimeTicks(list[i].Endtime));
                    if (oldGoodsDict.TryGetValue(key, out oldGoodsData))
                    {
                        int toAddNum = Global.GMin((gridNum - oldGoodsData.GCount), list[i].GCount);

                        oldGoodsData.GCount += toAddNum;

                        list[i].BagIndex = 1;
                        oldGoodsData.BagIndex = 1;
                        list[i].GCount -= toAddNum;
                        if (!Global.ResetBagGoodsData(client, list[i]))
                        {
                            //出错, 停止整理
                            break;
                        }

                        if (oldGoodsData.GCount >= gridNum) //旧的物品已经加满
                        {
                            if (list[i].GCount > 0)
                            {
                                oldGoodsDict[key] = list[i];
                            }
                            else
                            {
                                oldGoodsDict.Remove(key);
                                toRemovedGoodsDataList.Add(list[i]);
                            }
                        }
                        else
                        {
                            if (list[i].GCount <= 0)
                            {
                                toRemovedGoodsDataList.Add(list[i]);
                            }
                        }
                    }
                    else
                    {
                        oldGoodsDict[key] = list[i];
                    }
                }

                for (int i = 0; i < toRemovedGoodsDataList.Count; i++)
                {
                    list.Remove(toRemovedGoodsDataList[i]);
                }

                // PriceTwo铜钱列 从小到大 数量从多到少
                list.Sort(delegate(GoodsData x, GoodsData y)
                {
                    int xPrice = Global.GetGoodsYinLiangNumByID(x.GoodsID); // x铜钱价格
                    int yPrice = Global.GetGoodsYinLiangNumByID(y.GoodsID); // y铜钱价格
                    if (yPrice == xPrice)
                        return (y.GCount - x.GCount);
                    else
                        return (xPrice - yPrice);
                });

                int index = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (false && GameManager.Flag_OptimizationBagReset)
                    {
                        bool godosCountChanged = list[i].BagIndex > 0;
                        list[i].BagIndex = index++;
                        if (godosCountChanged)
                        {
                            if (!Global.ResetBagGoodsData(client, list[i]))
                            {
                                //出错, 停止整理
                                break;
                            }
                        }
                    }
                    else
                    {
                        list[i].BagIndex = index++;
                        if (!Global.ResetBagGoodsData(client, list[i]))
                        {
                            //出错, 停止整理
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        public Dictionary<int, GoodsData> GetBagDict(GameClient client)
        {
            Dictionary<int, GoodsData> dict = new Dictionary<int, GoodsData>();
            foreach (var goods in client.ClientData.FluorescentGemData.GemBagList)
            {
                if (goods.BagIndex < FluorescentGemDefine.MAX_FLUORESCENT_GEM_BAG_COUNT)
                {
                    dict[goods.BagIndex] = goods;
                }
            }
            return dict;
        }

        public Dictionary<int, Dictionary<int, GoodsData>> GetEquipDict(GameClient client)
        {
            Dictionary<int, Dictionary<int, GoodsData>> result = new Dictionary<int, Dictionary<int, GoodsData>>();

            foreach (var gd in client.ClientData.FluorescentGemData.GemEquipList)
            {
                int _pos, _type;
                ParsePosAndType(gd.BagIndex, out _pos, out _type);
                Dictionary<int, GoodsData> tmpGoodsDict = null;
                if (!result.TryGetValue(_pos, out tmpGoodsDict))
                {
                    tmpGoodsDict = new Dictionary<int, GoodsData>();
                    result[_pos] = tmpGoodsDict;
                }

                tmpGoodsDict[_type] = gd;
            }

            return result;
        }

        #region 执行荧光宝石挖掘
        /// <summary>
        /// 执行荧光宝石挖掘
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nLevelType">矿坑类型：100=初级，200=中级，300=高级</param>
        /// <param name="nDigType">挖掘类型：0=1次，1=10次</param>
        /// <param name="gemList">挖掘的宝石列表</param>
        /// <returns></returns>
        private EFluorescentGemDigErrorCode FluorescentGemDig(GameClient client, int nLevelType, int nDigType, out List<int> gemList)
        {
            // 创建返回的宝石挖掘列表
            gemList = new List<int>();

            try
            {
                // 判空
                if(null == client)
                    return EFluorescentGemDigErrorCode.Error;

                // 如果1.7的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                    return EFluorescentGemDigErrorCode.Error;

                // 矿坑类型错误
                if (!FluorescentGemLevelTypeConfigDict.ContainsKey(nLevelType))
                    return EFluorescentGemDigErrorCode.LevelTypeError;

                // 挖掘类型错误
                if (nDigType < 0 || nDigType > 1)
                    return EFluorescentGemDigErrorCode.DigType;

                // 根据挖掘类型不同实做
                switch (nDigType)
                {
                    case 0: // 挖一次
                    {
                        lock (client.ClientData.FluorescentGemData)
                        {
                            // 先检查背包空间
                            if (GetFluorescentGemBagSpace(client) < 1)
                                return EFluorescentGemDigErrorCode.BagNotEnoughOne;

                            FluorescentGemLevelTypeConfigData levelTypeData = null; // 矿坑类型表

                            // 找矿坑消耗信息
                            lock (FluorescentGemLevelTypeConfigDict)
                            {
                                if (!FluorescentGemLevelTypeConfigDict.TryGetValue(nLevelType, out levelTypeData) || null == levelTypeData)
                                    return EFluorescentGemDigErrorCode.LevelTypeDataError;
                            }

                            // 检查荧光粉末
                            if (levelTypeData._NeedFluorescentPoint > 0)
                            {
                                // 检查消耗
                                if (client.ClientData.FluorescentPoint < levelTypeData._NeedFluorescentPoint)
                                    return EFluorescentGemDigErrorCode.PointNotEnough;
                            }

                            // 检查钻石
                            if (levelTypeData._NeedDiamond > 0)
                            {
                                // 检查消耗
                                if (client.ClientData.UserMoney < levelTypeData._NeedDiamond)
                                    return EFluorescentGemDigErrorCode.DiamondNotEnough;
                            }

                            // 扣除荧光粉末
                            if (levelTypeData._NeedFluorescentPoint > 0)
                            {
                                if (!DecFluorescentPoint(client, levelTypeData._NeedFluorescentPoint, "宝石挖掘扣除"))
                                    return EFluorescentGemDigErrorCode.UpdatePointError;
                            }

                            // 扣除钻石
                            if (levelTypeData._NeedDiamond > 0)
                            {
                                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, levelTypeData._NeedDiamond, "荧光宝石挖掘"))
                                    return EFluorescentGemDigErrorCode.UpdateDiamondError;
                            }

                            // 挖掘数据
                            List<FluorescentGemDigConfigData> digList = null;
                            if (!FluorescentGemDigConfigDict.TryGetValue(nLevelType, out digList) || null == digList)
                                return EFluorescentGemDigErrorCode.DigDataError;

                            // 根据矿坑类型进行一次挖宝
                            // 几率 1 - 100000
                            int nRandom = Global.GetRandomNumber(1, 100001);
                            int nGoodsID = 0; // 挖掘到的宝石id
                            for (int i = 0; i < digList.Count; ++i)
                            {
                                if (nRandom >= digList[i]._StartValue && nRandom <= digList[i]._EndValue)
                                {
                                    nGoodsID = digList[i]._GoodsID;
                                    break;
                                }
                            }

                            // 检查是否为荧光宝石
                            if (!CheckIsFluorescentGemByGoodsID(nGoodsID))
                                return EFluorescentGemDigErrorCode.NotGem;

                            // 给与宝石
                            int nDBID = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, nGoodsID, 1, 0, "", 0, 0, (int)SaleGoodsConsts.FluorescentGemBag, "", true, 1, "荧光宝石挖掘");
                            if(nDBID < 0)
                                return EFluorescentGemDigErrorCode.AddGoodsError;

                            // 加入挖掘列表
                            gemList.Add(nGoodsID);

                            // 加日志
                            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荧光宝石", "挖掘", "系统", client.ClientData.RoleName, "修改", nGoodsID, client.ClientData.ZoneID, client.strUserID, nLevelType, client.ServerId);

                            // 加日志
                            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荧光宝石", "消耗", "系统", client.ClientData.RoleName, "修改", levelTypeData._NeedFluorescentPoint, client.ClientData.ZoneID, client.strUserID, levelTypeData._NeedDiamond, client.ServerId);
                        }
                    }
                    break;
                    case 1: // 挖10次
                    {
                        lock (client.ClientData.FluorescentGemData)
                        {
                            // 先检查背包空间
                            if (GetFluorescentGemBagSpace(client) < 10)
                                return EFluorescentGemDigErrorCode.BagNotEnoughTen;

                            FluorescentGemLevelTypeConfigData levelTypeData = null; // 矿坑类型表

                            // 找矿坑消耗信息
                            lock (FluorescentGemLevelTypeConfigDict)
                            {
                                if (!FluorescentGemLevelTypeConfigDict.TryGetValue(nLevelType, out levelTypeData) || null == levelTypeData)
                                    return EFluorescentGemDigErrorCode.LevelTypeDataError;
                            }

                            // 检查荧光粉末
                            if (levelTypeData._NeedFluorescentPoint > 0)
                            {
                                // 检查消耗
                                if (client.ClientData.FluorescentPoint < levelTypeData._NeedFluorescentPoint * 10)
                                    return EFluorescentGemDigErrorCode.PointNotEnough;
                            }

                            // 检查钻石
                            if (levelTypeData._NeedDiamond > 0)
                            {
                                // 检查消耗
                                if (client.ClientData.UserMoney < levelTypeData._NeedDiamond * 10)
                                    return EFluorescentGemDigErrorCode.DiamondNotEnough;
                            }

                            // 扣除荧光粉末
                            if (levelTypeData._NeedFluorescentPoint > 0)
                            {
                                if (!DecFluorescentPoint(client, levelTypeData._NeedFluorescentPoint * 10, "宝石挖掘扣除"))
                                    return EFluorescentGemDigErrorCode.UpdatePointError;
                            }

                            // 扣除钻石
                            if (levelTypeData._NeedDiamond > 0)
                            {
                                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, levelTypeData._NeedDiamond * 10, "荧光宝石挖掘"))
                                    return EFluorescentGemDigErrorCode.UpdateDiamondError;
                            }

                            // 挖掘数据
                            List<FluorescentGemDigConfigData> digList = null;
                            if (!FluorescentGemDigConfigDict.TryGetValue(nLevelType, out digList) || null == digList)
                                return EFluorescentGemDigErrorCode.DigDataError;

                            for (int i = 0; i < 10; ++i)
                            {
                                // 根据矿坑类型进行一次挖宝
                                // 几率 1 - 100000
                                int nRandom = Global.GetRandomNumber(1, 100001);
                                int nGoodsID = 0; // 挖掘到的宝石id
                                for (int k = 0; k < digList.Count; ++k)
                                {
                                    if (nRandom >= digList[k]._StartValue && nRandom <= digList[k]._EndValue)
                                    {
                                        nGoodsID = digList[k]._GoodsID;
                                        break;
                                    }
                                }

                                // 检查是否为荧光宝石
                                if (!CheckIsFluorescentGemByGoodsID(nGoodsID))
                                    return EFluorescentGemDigErrorCode.NotGem;

                                // 给与宝石奖励
                                int nDBID = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, nGoodsID, 1, 0, "", 0, 0, (int)SaleGoodsConsts.FluorescentGemBag, "", true, 1, "荧光宝石挖掘");
                                if (nDBID < 0)
                                    return EFluorescentGemDigErrorCode.AddGoodsError;

                                // 加入挖掘列表
                                gemList.Add(nGoodsID);

                                // 加日志
                                GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荧光宝石", "挖掘", "系统", client.ClientData.RoleName, "修改", nGoodsID, client.ClientData.ZoneID, client.strUserID, nLevelType, client.ServerId);
                                
                            }

                            // 加日志
                            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荧光宝石", "消耗", "系统", client.ClientData.RoleName, "修改", levelTypeData._NeedFluorescentPoint * 10, client.ClientData.ZoneID, client.strUserID, levelTypeData._NeedDiamond * 10, client.ServerId);
                        }
                    }
                    break;
                }

                return EFluorescentGemDigErrorCode.Success;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return EFluorescentGemDigErrorCode.Error;
        }
        #endregion

        #region 执行荧光宝石分解
        /// <summary>
        /// 执行荧光宝石分解
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nBagIndex">格子索引</param>
        /// <param name="nResolveCount">分解数量</param>
        /// <returns></returns>
        private EFluorescentGemResolveErrorCode FluorescentGemResolve(GameClient client, int nBagIndex, int nResolveCount)
        {
            try
            {
                // 判空
                if(null == client)
                    return EFluorescentGemResolveErrorCode.Error;
                lock (client.ClientData.FluorescentGemData)
                {
                    GoodsData goodsData = null;
                    if ((goodsData = client.ClientData.FluorescentGemData.GemBagList.Find(_g => _g.BagIndex == nBagIndex)) == null)
                    // 物品不存在
                        return EFluorescentGemResolveErrorCode.GoodsNotExist;

                    // 检查是否为荧光宝石
                    if (!CheckIsFluorescentGemByGoodsID(goodsData.GoodsID))
                        return EFluorescentGemResolveErrorCode.NotGem;

                    // 分解个数错误
                    if(nResolveCount <=0 || nResolveCount > goodsData.GCount)
                        return EFluorescentGemResolveErrorCode.ResolveCountError;

                    // 获取分解将获得的荧光粉末个数
                    int nTotalCount = GetFluorescentPointByGoodsID(goodsData.GoodsID) * nResolveCount;

                    // 进行分解 通知DB 物品数量改变
                    if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, goodsData, nResolveCount, false))
                        return EFluorescentGemResolveErrorCode.ResolveError;
                
                    // 分解成功后 给荧光粉末
                    AddFluorescentPoint(client, nTotalCount, "宝石分解获得");
                }

                return EFluorescentGemResolveErrorCode.Success;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return EFluorescentGemResolveErrorCode.Error;
        }
        #endregion

        public int GenerateBagIndex(int pos, int type)
        {
            return pos * 100 + type;
        }

        public void ParsePosAndType(int bagIndex, out int pos, out int type)
        {
            pos = bagIndex / 100;
            type = bagIndex % 100;
        }

        #region 执行荧光宝石升级
        /// <summary>
        /// 执行荧光宝石升级
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="upData">宝石升级传输结构</param>
        /// <param name="nNewGoodsBagIndex">升级后的宝石dbid</param>
        /// <returns></returns>
        private EFluorescentGemUpErrorCode FluorescentGemUp(GameClient client, FluorescentGemUpTransferData upData, out int nNewGoodsDBID)
        {
            nNewGoodsDBID = -1; // 默认-1
            try
            {
                // 判空
                if (null == client)
                    return EFluorescentGemUpErrorCode.Error;

                // 如果1.7的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                    return EFluorescentGemUpErrorCode.Error;

                GoodsData goodsData = null; // 指定要升级的宝石
                lock (client.ClientData.FluorescentGemData)
                {
                    if (upData._UpType == 0) // 背包中升级
                    {
                        if (GetFluorescentGemBagSpace(client) < 1)
                            return EFluorescentGemUpErrorCode.BagNotEnoughOne;


                        // 找指定要升级的宝石
                        if ((goodsData = client.ClientData.FluorescentGemData.GemBagList.Find(_g => _g.BagIndex == upData._BagIndex)) == null)
                            return EFluorescentGemUpErrorCode.GoodsNotExist;
                    }
                    else // 装备栏中升级
                    {
                        // 检查 装备栏位索引
                        if (!CheckEquipPositionIndex(upData._Position))
                            return EFluorescentGemUpErrorCode.PositionIndexError;

                        // 检查宝石类型
                        if (!CheckGemTypeIndex(upData._GemType))
                            return EFluorescentGemUpErrorCode.GemTypeError;

                        if ((goodsData = client.ClientData.FluorescentGemData.GemEquipList.Find(
                                _g => _g.BagIndex == GenerateBagIndex(upData._Position, upData._GemType))) == null)
                            return EFluorescentGemUpErrorCode.GoodsNotExist;
                    }

                    // 物品不存在
                    if (null == goodsData)
                        return EFluorescentGemUpErrorCode.GoodsNotExist;

                    // 检查是否为荧光宝石
                    if (!CheckIsFluorescentGemByGoodsID(goodsData.GoodsID))
                        return EFluorescentGemUpErrorCode.NotGem;

                    // 找到静态数据
                    FluorescentGemUpConfigData goodsConfig = null; // 宝石升级静态数据
                    if (!FluorescentGemUpConfigDict.TryGetValue(goodsData.GoodsID, out goodsConfig) || null == goodsConfig)
                        return EFluorescentGemUpErrorCode.UpDataError;

                    // 检查宝石等级
                    if (goodsConfig._NewGoodsID <= 0)
                        return EFluorescentGemUpErrorCode.MaxLevel;

                    // 下一级宝石静态数据
                    FluorescentGemUpConfigData nextGoodsConfig = null;
                    if (!FluorescentGemUpConfigDict.TryGetValue(goodsConfig._NewGoodsID, out nextGoodsConfig) || null == nextGoodsConfig)
                        return EFluorescentGemUpErrorCode.NextLevelDataError;

                    // 检查金币
                    if (client.ClientData.Money1 + client.ClientData.YinLiang < nextGoodsConfig._NeedGold)
                        return EFluorescentGemUpErrorCode.GoldNotEnough;

                    int nNeedLevelOneGemCount = 0; // 需要几个1级石头
                    // 需要多少1级石头
                    nNeedLevelOneGemCount = goodsConfig._NeedLevelOneGoodsCount * 3; // 需要3颗
                    
                    int nTotalLevelOneCount = 0; // 要扣除的宝石换算为1级石头的总个数
                    
                    // 如果是装备栏先将装备栏上宝石的个数加上
                    if (upData._UpType == 1)
                    {
                        nTotalLevelOneCount += goodsConfig._NeedLevelOneGoodsCount;
                    }

                    // 遍历要扣除的宝石 key=格子索引，value=要扣除的数量
                    foreach (var item in upData._DecGoodsDict)
                    {
                        // 背包宝石
                        GoodsData tmpGoods = null;

                        // 根据格子索引找要扣除的物品
                        if ((tmpGoods = client.ClientData.FluorescentGemData.GemBagList.Find(_g => _g.BagIndex == item.Key)) == null)
                            return EFluorescentGemUpErrorCode.DecGoodsNotExist;

                        // 检查要扣除的物品数量是否足够
                        if (tmpGoods.GCount < item.Value)
                            return EFluorescentGemUpErrorCode.DecGoodsNotEnough;

                        // 要扣除的宝石静态数据
                        FluorescentGemUpConfigData tmpConfig = null;
                        if (!FluorescentGemUpConfigDict.TryGetValue(tmpGoods.GoodsID, out tmpConfig) || null == tmpConfig)
                            continue;

                        // 检查是否为相同宝石
                        if (!IsSameGem(goodsConfig, tmpConfig))
                            continue;

                        nTotalLevelOneCount += tmpConfig._NeedLevelOneGoodsCount * item.Value; // 累加 转换为1级石头个数
                    }
                    
                    // 换算为1级宝石个数 检查是否对等
                    if (nNeedLevelOneGemCount != nTotalLevelOneCount)
                        return EFluorescentGemUpErrorCode.GemNotEnough;
                
                    // 扣除参与升级的宝石
                    foreach (var item in upData._DecGoodsDict)
                    {
                        // 找到要扣除的
                        GoodsData tmpGoods = null;
                        if ((tmpGoods = client.ClientData.FluorescentGemData.GemBagList.Find(_g => _g.BagIndex == item.Key)) == null)
                            return EFluorescentGemUpErrorCode.DecGoodsError;

                        // 进行扣除 通知DB 物品数量改变
                        if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                        Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, tmpGoods, item.Value, false))
                            return EFluorescentGemUpErrorCode.DecGoodsError;
                    }

                    if (upData._UpType == 0) // 背包中升级
                    {
                        // 扣金币
                        if(!Global.SubBindTongQianAndTongQian(client, nextGoodsConfig._NeedGold, "荧光宝石升级"))
                            return EFluorescentGemUpErrorCode.GoldNotEnough;

                        // 给与新宝石 升级会导致物品绑定 不是用旧格子
                        int nDBID = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, goodsConfig._NewGoodsID, 1, 0, "", 0, 0, (int)SaleGoodsConsts.FluorescentGemBag, "", false, 1, "荧光宝石升级");
                        if (nDBID < 0)
                            return EFluorescentGemUpErrorCode.AddGoodsError;

                        nNewGoodsDBID = nDBID;

                        // 加日志
                        GameManager.logDBCmdMgr.AddDBLogInfo(nDBID, "荧光宝石", "背包宝石升级", "系统", client.ClientData.RoleName, "修改", goodsData.GoodsID, client.ClientData.ZoneID, client.strUserID, goodsConfig._NewGoodsID, client.ServerId);
                    }
                    else // 装备栏升级
                    {
                        // 扣金币
                        if (!Global.SubBindTongQianAndTongQian(client, nextGoodsConfig._NeedGold, "荧光宝石升级"))
                            return EFluorescentGemUpErrorCode.GoldNotEnough;

                        // 移除原有的宝石
                        FluorescentGemSaveDBData unEquipData = new FluorescentGemSaveDBData();
                        unEquipData._RoleID = client.ClientData.RoleID;
                        unEquipData._Position = upData._Position;
                        unEquipData._GemType = upData._GemType;

                        // 通知移除荧光宝石
                        if(!NotifyUnEquipGem(client, unEquipData, 1))
                            return EFluorescentGemUpErrorCode.DecGoodsError;

                        // 装备新的宝石
                        FluorescentGemSaveDBData equipData = new FluorescentGemSaveDBData();
                        equipData._RoleID = client.ClientData.RoleID;
                        equipData._GoodsID = goodsConfig._NewGoodsID;
                        equipData._Position = upData._Position;
                        equipData._GemType = upData._GemType;
                        //equipData._Bind = 1; // 宝石升级会导致 物品绑定
                            
                        // 通知装备荧光宝石
                        if (!NotifyEquipGem(client, equipData))
                            return EFluorescentGemUpErrorCode.EquipError;

                        UpdateProps(client);
                        //通知客户端属性变化
                        GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                        // 总生命值和魔法值变化通知(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                        // 加日志
                        GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荧光宝石", "装备栏宝石升级", "系统", client.ClientData.RoleName, "修改", goodsData.GoodsID, client.ClientData.ZoneID, client.strUserID, goodsConfig._NewGoodsID, client.ServerId);
                    }
                }

                return EFluorescentGemUpErrorCode.Success;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return EFluorescentGemUpErrorCode.Error;
        }
        #endregion

        #region 执行荧光宝石装备
        /// <summary>
        /// 执行荧光宝石装备
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nBagIndex">格子索引</param>
        /// <param name="nPositionIndex">部位索引</param>
        /// <param name="nGemType">宝石类型</param>
        /// <returns></returns>
        private EFluorescentGemEquipErrorCode FluorescentGemEquip(GameClient client, int nBagIndex, int nPositionIndex, int nGemType)
        {
            try
            {
                // 判空
                if (null == client)
                    return EFluorescentGemEquipErrorCode.Error;

                // 如果1.7的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                    return EFluorescentGemEquipErrorCode.Error;

                GoodsData goodsData = null; // 指定要装备的宝石
                // 找指定要装备的宝石
                if ((goodsData = client.ClientData.FluorescentGemData.GemBagList.Find(_g => _g.BagIndex == nBagIndex)) == null)
                    return EFluorescentGemEquipErrorCode.GoodsNotExist;

                // 检查是否为荧光宝石
                if (!CheckIsFluorescentGemByGoodsID(goodsData.GoodsID))
                    return EFluorescentGemEquipErrorCode.NotGem;

                // 检查 装备栏位索引
                if (!CheckEquipPositionIndex(nPositionIndex))
                    return EFluorescentGemEquipErrorCode.PositionIndexError;

                // 检查宝石类型
                if (!CheckGemTypeIndex(nGemType))
                    return EFluorescentGemEquipErrorCode.GemTypeError;
                
                // 找到静态数据
                FluorescentGemUpConfigData goodsConfig = null; // 宝石升级静态数据
                if (!FluorescentGemUpConfigDict.TryGetValue(goodsData.GoodsID, out goodsConfig) || null == goodsConfig)
                    return EFluorescentGemEquipErrorCode.GemDataError;

                // 检查要装备的宝石的类型与要装备部位的宝石类型是否一致
                if (nGemType != goodsConfig._GemType)
                    return EFluorescentGemEquipErrorCode.GemTypeError;

                GoodsData destGoods = null;
                destGoods = client.ClientData.FluorescentGemData.GemEquipList.Find(_g => _g.BagIndex == GenerateBagIndex(nPositionIndex, nGemType));
                if (destGoods != null)
                {
                    // 有则先卸下 参数0 代表卸下一个宝石
                    EFluorescentGemUnEquipErrorCode unEquipRes = FluorescentGemUnEquip(client, 0, nPositionIndex, nGemType);
                    if (unEquipRes != EFluorescentGemUnEquipErrorCode.Success)
                        return EFluorescentGemEquipErrorCode.UnEquipError;
                }

                // 创建一个要装备的宝石
                GoodsData equipGoods = Global.CopyGoodsData(goodsData);

                // 将要装备的宝石从背包移除
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, goodsData, 1, false))
                    return EFluorescentGemEquipErrorCode.DecGoodsError;

                FluorescentGemSaveDBData sendData = new FluorescentGemSaveDBData();
                sendData._RoleID = client.ClientData.RoleID;
                sendData._GoodsID = equipGoods.GoodsID;
                sendData._Position = nPositionIndex;
                sendData._GemType = nGemType;
                sendData._Bind = equipGoods.Binding;

                // 通知装备荧光宝石
                if (!NotifyEquipGem(client, sendData))
                    return EFluorescentGemEquipErrorCode.EquipError;

                UpdateProps(client);

                //通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                return EFluorescentGemEquipErrorCode.Success;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return EFluorescentGemEquipErrorCode.Error;
        }
        #endregion

        #region 执行荧光宝石卸下
        /// <summary>
        /// 执行荧光宝石卸下
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nUnEquipType">卸下类型 0=单个，1=全部</param>
        /// <param name="nPositionIndex">部位索引</param>
        /// <param name="nGemType">宝石类型</param>
        /// <returns></returns>
        private EFluorescentGemUnEquipErrorCode FluorescentGemUnEquip(GameClient client, int nUnEquipType, int nPositionIndex, int nGemType)
        {
            try
            {
                // 判空
                if (null == client)
                    return EFluorescentGemUnEquipErrorCode.Error;

                // 如果1.7的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                    return EFluorescentGemUnEquipErrorCode.Error;

                GoodsData goodsData = null; // 指定要卸下的宝石

                // 根据卸下类型分别实做
                switch (nUnEquipType)
                {
                    case 0: // 单个卸下
                        {
                            lock (client.ClientData.FluorescentGemData)
                            {
                                // 先检查背包空间
                                if (GetFluorescentGemBagSpace(client) < 1)
                                    return EFluorescentGemUnEquipErrorCode.BagNotEnoughOne;

                                // 检查 装备栏位索引
                                if (!CheckEquipPositionIndex(nPositionIndex))
                                    return EFluorescentGemUnEquipErrorCode.PositionIndexError;

                                // 检查宝石类型
                                if (!CheckGemTypeIndex(nGemType))
                                    return EFluorescentGemUnEquipErrorCode.GemTypeError;

                                if ((goodsData = client.ClientData.FluorescentGemData.GemEquipList.Find(
                                        _g => _g.BagIndex == GenerateBagIndex(nPositionIndex, nGemType))) == null)
                                    return EFluorescentGemUnEquipErrorCode.GoodsNotExist;

                                FluorescentGemSaveDBData sendData = new FluorescentGemSaveDBData();
                                sendData._RoleID = client.ClientData.RoleID;
                                sendData._GoodsID = goodsData.GoodsID;
                                sendData._Position = nPositionIndex;
                                sendData._GemType = nGemType;
                                sendData._Bind = goodsData.Binding;

                                // 通知卸下荧光宝石
                                if (!NotifyUnEquipGem(client, sendData, 0))
                                    return EFluorescentGemUnEquipErrorCode.UnEquipError;
                            }
                        }
                        break;
                    case 1: // 全部卸下
                        {
                            lock (client.ClientData.FluorescentGemData)
                            {
                                // 先检查背包空间
                                if (GetFluorescentGemBagSpace(client) < 3)
                                    return EFluorescentGemUnEquipErrorCode.BagNotEnoughThree;

                                // 检查 装备栏位索引
                                if (!CheckEquipPositionIndex(nPositionIndex))
                                    return EFluorescentGemUnEquipErrorCode.PositionIndexError;

                                List<GoodsData> decList = new List<GoodsData>(); // 要删除的宝石List
                                List<int> decGemTypeList = new List<int>(); // 要删除的宝石类型List

                                foreach (var item in client.ClientData.FluorescentGemData.GemEquipList)
                                {
                                    int _pos, _type;
                                    ParsePosAndType(item.BagIndex, out _pos, out _type);
                                    if (_pos != nPositionIndex)
                                        continue;

                                    decList.Add(item);
                                    decGemTypeList.Add(_type);
                                }

                                // 循环卸下
                                for (int i = 0; i < decList.Count; ++i)
                                {
                                    goodsData = decList[i];
                                    if (null == goodsData)
                                        continue;

                                    FluorescentGemSaveDBData sendData = new FluorescentGemSaveDBData();
                                    sendData._RoleID = client.ClientData.RoleID;
                                    sendData._GoodsID = goodsData.GoodsID;
                                    sendData._Position = nPositionIndex;
                                    sendData._GemType = decGemTypeList[i];
                                    sendData._Bind = goodsData.Binding;

                                    // 通知卸下荧光宝石
                                    if (!NotifyUnEquipGem(client, sendData, 0))
                                        return EFluorescentGemUnEquipErrorCode.UnEquipError;
                                }
                            }
                            break;
                        }
                }

                UpdateProps(client);

                //通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                return EFluorescentGemUnEquipErrorCode.Success;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return EFluorescentGemUnEquipErrorCode.Error;
        }
        #endregion

        #region 通知装备荧光宝石
        /// <summary>
        /// 通知装备荧光宝石
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool NotifyEquipGem(GameClient client, FluorescentGemSaveDBData data)
        {
            // 判空
            if (null == client || null == data)
                return false;

            // 如果1.7的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                return false;

            // 通知DB
            byte[] sendBytes = DataHelper.ObjectToBytes<FluorescentGemSaveDBData>(data);
            if (!Global.sendToDB<bool, byte[]>((int)TCPGameServerCmds.CMD_DB_FLUORESCENT_EQUIP, sendBytes, client.ServerId))
                return false;

            // 创建一个宝石
            GoodsData equipGoods = new GoodsData();
            equipGoods.GoodsID = data._GoodsID;
            equipGoods.GCount = 1;
            equipGoods.Binding = data._Bind;
            equipGoods.Site = (int)SaleGoodsConsts.FluorescentGemEquip; // 荧光宝石装备栏
            equipGoods.BagIndex = GenerateBagIndex(data._Position, data._GemType);

            // 更新缓存
            lock (client.ClientData.FluorescentGemData)
            {
                client.ClientData.FluorescentGemData.GemEquipList.RemoveAll(_g => _g.BagIndex == equipGoods.BagIndex);
                client.ClientData.FluorescentGemData.GemEquipList.Add(equipGoods);
            }

            // 通知客户端荧光宝石装备栏变动
            FluorescentGemEquipChangesTransferData equipChangesData = new FluorescentGemEquipChangesTransferData();
            equipChangesData._Position = data._Position;
            equipChangesData._GemType = data._GemType;
            equipChangesData._GoodsData = equipGoods;
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_FLUORESCENT_GEM_EQUIP_CHANGES, equipChangesData);

            // 加日志
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荧光宝石", "镶嵌", "系统", client.ClientData.RoleName, "修改", data._GoodsID, client.ClientData.ZoneID, client.strUserID, 0, client.ServerId);

            return true;
        }
        #endregion

        #region 通知卸下/移除荧光宝石
        /// <summary>
        /// 通知卸下/移除荧光宝石
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <param name="nOP">操作 0=卸下，1=移除</param>
        /// <returns></returns>
        private bool NotifyUnEquipGem(GameClient client, FluorescentGemSaveDBData data, int nOP)
        {
            // 判空
            if (null == client || null == data)
                return false;

            // 如果1.7的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                return false;

            // 检查操作索引 0=卸下，1=移除
            if (nOP < 0 || nOP > 1)
                return false;

            // 通知DB
            byte[] sendBytes = DataHelper.ObjectToBytes<FluorescentGemSaveDBData>(data);
            if (!Global.sendToDB<bool, byte[]>((int)TCPGameServerCmds.CMD_DB_FLUORESCENT_UN_EQUIP, sendBytes, client.ServerId))
                return false;

            // 更新缓存
            lock (client.ClientData.FluorescentGemData)
            {
                client.ClientData.FluorescentGemData.GemEquipList.RemoveAll(_g => _g.BagIndex == GenerateBagIndex(data._Position, data._GemType));
            }

            // 通知客户端荧光宝石装备栏变动
            FluorescentGemEquipChangesTransferData equipChangesData = new FluorescentGemEquipChangesTransferData();
            equipChangesData._Position = data._Position;
            equipChangesData._GemType = data._GemType;
            equipChangesData._GoodsData = null;
            client.sendCmd((int)TCPGameServerCmds.CMD_SPR_FLUORESCENT_GEM_EQUIP_CHANGES, equipChangesData);

            // 卸下才会加宝石
            if (nOP == 0)
            {
                // 给与宝石
                int nDBID = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, data._GoodsID, 1, 0, "", 0, data._Bind, (int)SaleGoodsConsts.FluorescentGemBag, "", true, 0, "荧光宝石卸下");
                if (nDBID < 0)
                    return false;
            }

            // 加日志
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荧光宝石", "卸下", "系统", client.ClientData.RoleName, "修改", data._GoodsID, client.ClientData.ZoneID, client.strUserID, 0, client.ServerId);

            return true;
        }
        #endregion

        #endregion

        #region public函数

        #region 统一读取荧光宝石配置文件
        /// <summary>
        /// 统一读取荧光宝石配置文件
        /// </summary>
        public void LoadFluorescentGemConfigData()
        {
            LoadFluorescentGemDigConfigData();
            LoadFluorescentGemLevelTypeConfigData();
            LoadFluorescentGemUpConfigData();
        }
        #endregion

        #region 检查荧光宝石是否开启
        /// <summary>
        /// 检查荧光宝石是否开启
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool IsOpenFluorescentGem(GameClient client)
        {
            if (null == client)
                return false;

            // 检查版本是否开启
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.FluorescentGem))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("版本控制未开启荧光宝石功能, RoleID={0}", client.ClientData.RoleID));
                return false;
            }

            // 检查功能是否开启
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.FluorescentGem))
            {
                return false;
            }

            return true;
        }
        #endregion

        #region 检查物品是否为荧光宝石
        /// <summary>
        /// 检查物品是否为荧光宝石
        /// </summary>
        /// <param name="nGoodsID">物品id</param>
        /// <returns></returns>
        public bool CheckIsFluorescentGemByGoodsID(int nGoodsID)
        {
            //获取Xml项
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(nGoodsID, out systemGoods))
            {
                return false;
            }

            return systemGoods.GetIntValue("Categoriy") == (int)ItemCategories.FluorescentGem;
        }
        #endregion

        #region 角色上线增加荧光宝石属性
        /// <summary>
        /// refactor by chenjg. 2015-11-30
        /// 登录时修正重复bagindex的宝石，计算属性加成
        /// </summary>
        /// <param name="client"></param>
        public void OnLogin(GameClient client)
        {
            if (client == null) return;

            // 上线做判空处理
            if (client.ClientData.FluorescentGemData == null)
                client.ClientData.FluorescentGemData = new FluorescentGemData();
            if (client.ClientData.FluorescentGemData.GemBagList == null)
                client.ClientData.FluorescentGemData.GemBagList = new List<GoodsData>();
            if (client.ClientData.FluorescentGemData.GemEquipList == null)
                client.ClientData.FluorescentGemData.GemEquipList = new List<GoodsData>();

         
            // 装备栏位置重复的由db进行删除
            // 背包栏上线整理，防止位置重复

            // 整理背包代价太大，如果上线的时候，发现没有重复格子的宝石，就不整理背包了
            HashSet<int> usedBagIndex = new HashSet<int>();
            foreach (var gd in client.ClientData.FluorescentGemData.GemBagList)
            {
                if (usedBagIndex.Contains(gd.BagIndex))
                {
                    // 整理下背包
                    ResetBagAllGoods(client);
                    break;
                }

                usedBagIndex.Add(gd.BagIndex);
            }

            // 计算属性加成
            UpdateProps(client);
        }

        private void UpdateProps(GameClient client)
        {
            if (client == null) return;

            EquipPropItem totalPros = new EquipPropItem();
            foreach (var gd in client.ClientData.FluorescentGemData.GemEquipList)
            {
                SystemXmlItem systemGoods = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(gd.GoodsID, out systemGoods))
                    continue;

                if (!CheckIsFluorescentGemByGoodsID(gd.GoodsID))
                    continue;

                EquipPropItem oneProp = GameManager.EquipPropsMgr.FindEquipPropItem(gd.GoodsID);

                for (int i = 0; oneProp != null && i < (int)ExtPropIndexes.Max; i++)
                {
                    totalPros.ExtProps[i] += oneProp.ExtProps[i];
                }
            }

            client.ClientData.PropsCacheManager.SetExtProps((int)PropsSystemTypes.FluorescentGem, totalPros);
        }
        #endregion

        #region 荧光宝石粉末相关
        
        #region 增加荧光粉末
        /// <summary>
        /// 增加荧光粉末
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nAddPoint">要增加的数量</param>
        /// <param name="reasonStr">原因</param>
        /// <returns></returns>
        public bool AddFluorescentPoint(GameClient client, int nAddPoint, string reasonStr)
        {
            // 判空
            if (null == client)
                return false;

            // 非负数
            if (nAddPoint <= 0)
                return false;

            // 总数
            int nTotalPoint = client.ClientData.FluorescentPoint + nAddPoint;

            // 通知DB
            if (!UpdateFluorescentPoint2DB(client, nTotalPoint))
                return false;

            // 更新缓存
            client.ClientData.FluorescentPoint = nTotalPoint;

            // 加日志
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荧光粉末", reasonStr, "系统", client.ClientData.RoleName, "修改", nAddPoint, client.ClientData.ZoneID, client.strUserID, nTotalPoint, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.Fluorescent, nAddPoint, nTotalPoint, reasonStr);

            // 通知客户端
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.FluorescentGem, nTotalPoint);

            return true;
        }
        #endregion

        #region 扣除荧光粉末
        /// <summary>
        /// 扣除荧光粉末
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nDecPoint">要扣除的数量</param>
        /// <param name="reasonStr">原因</param>
        /// <returns></returns>
        public bool DecFluorescentPoint(GameClient client, int nDecPoint, string reasonStr)
        {
            // 判空
            if (null == client)
                return false;

            // 非负数
            if (nDecPoint <= 0)
                return false;

            // 总数
            int nTotalPoint = client.ClientData.FluorescentPoint - nDecPoint;

            // 校正负数
            Math.Max(0, nTotalPoint);

            // 通知DB
            if (!UpdateFluorescentPoint2DB(client, nTotalPoint))
                return false;

            // 更新缓存
            client.ClientData.FluorescentPoint = nTotalPoint;

            // 加日志
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "荧光粉末", reasonStr, "系统", client.ClientData.RoleName, "修改", nDecPoint, client.ClientData.ZoneID, client.strUserID, nTotalPoint, client.ServerId);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.Fluorescent, -nDecPoint, nTotalPoint, reasonStr);

            // 通知客户端
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.FluorescentGem, nTotalPoint);
            return true;
        }
        #endregion

        #region 更新荧光粉末
        /// <summary>
        /// 更新荧光粉末
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nTotalPoint">粉末总数</param>
        /// <returns></returns>
        public bool UpdateFluorescentPoint2DB(GameClient client, int nTotalPoint)
        {
            if (null == client)
                return false;

            // 通知DB RoleID(int) + 荧光粉末总数(int)
            string dbStrCmd = string.Format("{0}:{1}", client.ClientData.RoleID, nTotalPoint); // 组字符串
            byte[] dbBytesCmd = new UTF8Encoding().GetBytes(dbStrCmd); // 转字节数组
            return Global.sendToDB<bool, byte[]>((int)TCPGameServerCmds.CMD_DB_FLUORESCENT_POINT_UPDATE, dbBytesCmd, client.ServerId); // 发送
        }

        /// <summary>
        /// 更新荧光粉末 变化量
        /// </summary>
        /// <param name="rid">角色ID</param>
        /// <param name="nPointChg">粉末变化量</param>
        /// <returns></returns>
        public bool ModifyFluorescentPoint2DB(int rid, int nPointChg)
        {
            // 通知DB RoleID(int) + 荧光粉末变化量(int)
            string dbStrCmd = string.Format("{0}:{1}", rid, nPointChg); // 组字符串
            byte[] dbBytesCmd = new UTF8Encoding().GetBytes(dbStrCmd); // 转字节数组
            return Global.sendToDB<bool, byte[]>((int)TCPGameServerCmds.CMD_DB_FLUORESCENT_POINT_MODIFY, dbBytesCmd, GameManager.LocalServerId); // 发送
            EventLogManager.AddMoneyEvent(GameManager.ServerId, 0, "none", rid, OpTypes.AddOrSub, OpTags.None, MoneyTypes.Fluorescent, nPointChg, -1, "none");
        }
        #endregion

        #endregion

        #region 荧光宝石背包相关

        #region 添加到荧光宝石到背包
        /// <summary>
        /// 添加到荧光宝石到背包
        /// </summary>
        /// <param name="client"></param>
        public GoodsData AddFluorescentGemData(GameClient client, int id, int goodsID, int forgeLevel, int quality, int goodsNum, int binding, int site, string jewelList, string startTime, string endTime,
            int addPropIndex, int bornIndex, int lucky, int strong, int ExcellenceProperty, int nAppendPropLev, int nEquipChangeLife, int bagIndex = 0, List<int> washProps = null)
        {
            GoodsData gd = new GoodsData()
            {
                Id = id,
                GoodsID = goodsID,
                Using = 0,
                Forge_level = forgeLevel,
                Starttime = startTime,
                Endtime = endTime,
                Site = site,
                Quality = quality,
                Props = "",
                GCount = goodsNum,
                Binding = binding,
                Jewellist = jewelList,
                BagIndex = bagIndex,
                AddPropIndex = addPropIndex,
                BornIndex = bornIndex,
                Lucky = lucky,
                Strong = strong,
                ExcellenceInfo = ExcellenceProperty,
                AppendPropLev = nAppendPropLev,
                ChangeLifeLevForEquip = nEquipChangeLife,
                WashProps = washProps,
            };

            AddFluorescentGemData(client, gd);
            return gd;
        }

        /// <summary>
        /// 添加到荧光宝石到背包
        /// </summary>
        /// <param name="client"></param>
        public void AddFluorescentGemData(GameClient client, GoodsData gd)
        {
            // 判空
            if (null == gd)
                return;

            // 判空
            if (null == client.ClientData.FluorescentGemData)
                client.ClientData.FluorescentGemData = new FluorescentGemData();

            // 加入荧光宝石背包
            lock (client.ClientData.FluorescentGemData)
            {
                client.ClientData.FluorescentGemData.GemBagList.Add(gd);
            }
        }
        #endregion

        #region 从荧光宝石背包移除
        /// <summary>
        /// 从荧光宝石背包移除
        /// </summary>
        /// <param name="client"></param>
        /// <param name="goodsData"></param>
        public void RemoveFluorescentGemData(GameClient client, GoodsData goodsData)
        {
            if ((int)SaleGoodsConsts.FluorescentGemBag == goodsData.Site)
            {
                lock (client.ClientData.FluorescentGemData)
                {
                    client.ClientData.FluorescentGemData.GemBagList.RemoveAll(_g => _g.BagIndex == goodsData.BagIndex);
                }
            }
        }
        #endregion

        #region 返回荧光宝石背包中的空闲位置 找不到返回-1
        /// <summary>
        /// 返回荧光宝石背包中的空闲位置 找不到返回-1
        /// </summary>
        public int GetIdleSlotOfFluorescentGemBag(GameClient client)
        {
            int idelPos = -1;

            if (null == client.ClientData.FluorescentGemData || null == client.ClientData.FluorescentGemData)
                return idelPos;

            if (null == client.ClientData.GoodsDataList)
                return idelPos;

            List<int> usedBagIndex = new List<int>();

            client.ClientData.FluorescentGemData.GemBagList.ForEach(_g => usedBagIndex.Add(_g.BagIndex));

            // 找出空闲索引
            for (int n = 0; n < FluorescentGemDefine.MAX_FLUORESCENT_GEM_BAG_COUNT; n++)
            {
                if (usedBagIndex.IndexOf(n) < 0)
                {
                    idelPos = n;
                    break;
                }
            }

            return idelPos;
        }
        #endregion

        #region 从荧光宝石背包中查找指定的物品
        /// <summary>
        /// 从荧光宝石背包中查找指定的物品(记忆索引)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        public GoodsData GetGoodsByID(GameClient client, int goodsID, int bingding, string startTime, string endTime, ref int startIndex)
        {
            if (null == client)
                return null;

            List<GoodsData> list = new List<GoodsData>();

            lock (client.ClientData.FluorescentGemData)
            {
                foreach (var goods in client.ClientData.FluorescentGemData.GemEquipList)
                {
                    if (goods.GoodsID == goodsID
                        && goods.Binding == bingding
                        && Global.DateTimeEqual(goods.Endtime, endTime)
                        && Global.DateTimeEqual(goods.Starttime, startTime))
                    {
                        list.Add(goods);
                    }
                }

                if (null == list || list.Count <= 0)
                    return null;

                // 根据格子索引 从小到大排序
                list.Sort(delegate(GoodsData x, GoodsData y)
                {
                    return (x.BagIndex - y.BagIndex);
                });

                if (startIndex >= list.Count)
                    return null;

                for (int i = startIndex; i < list.Count; i++)
                {
                    startIndex = i + 1;
                    return list[i];
                }
            }

            return null;
        }
        #endregion

        #endregion

        #region 荧光宝石操作相关

        #region 荧光宝石背包整理
        /// <summary>
        /// 荧光宝石背包整理
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public TCPProcessCmdResults ProcessResetBagCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                // RoleID(int)
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //解析用户名称和用户密码
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
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

                // 检查是否开启
                if (!IsOpenFluorescentGem(client))
                    return TCPProcessCmdResults.RESULT_OK;

                //整理用户的荧光宝石背包
                ResetBagAllGoods(client);
                client.sendCmd(nID, GetBagDict(client));

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion

        #region 荧光宝石挖掘
        /// <summary>
        /// 荧光宝石挖掘
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>CMD_SPR_FLUORESCENT_GEM_DIG
        public TCPProcessCmdResults ProcessFluorescentGemDig(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // RoleID(int) + 矿坑类型(int)[0=初级，1=中级，2=高级] + 挖掘类型(int)[0=1次，1=10次]
                string[] fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                
                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                int nLevelType = Convert.ToInt32(fields[1]); // 矿坑类型
                int nDigType = Convert.ToInt32(fields[2]); // 挖掘类型

                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                FluorescentGemDigTransferData sendData = null;

                // 检查是否开启
                if (!IsOpenFluorescentGem(client))
                {
                    sendData = new FluorescentGemDigTransferData();
                    sendData._Result = (int)EFluorescentGemDigErrorCode.NotOpen;
                    client.sendCmd(nID, sendData);
                    return TCPProcessCmdResults.RESULT_OK;
                }

                string strcmd = ""; // 返回字符串

                // 挖掘的宝石列表
                List<int> gemList = null;

                // 执行挖掘
                EFluorescentGemDigErrorCode err = FluorescentGemDig(client, nLevelType, nDigType, out gemList);

                // 创建挖掘结果传输结构
                sendData = new FluorescentGemDigTransferData();
                sendData._Result = (int)err;
                sendData._GemList = gemList;

                // 发送给客户端
                // FluorescentGemDigTransferData
                client.sendCmd(nID, sendData);
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion

        #region 荧光宝石分解
        /// <summary>
        /// 荧光宝石分解
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public TCPProcessCmdResults ProcessFluorescentGemResolve(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // RoleID(int) + 宝石在背包的格子索引(int) + 分解个数(int)
                string[] fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                int nBagIndex = Convert.ToInt32(fields[1]); // 格子索引
                int nResolveCount = Convert.ToInt32(fields[2]); // 分解个数

                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 检查是否开启
                if (!IsOpenFluorescentGem(client))
                {
                    client.sendCmd(nID, string.Format("{0}", (int)EFluorescentGemResolveErrorCode.NotOpen));
                    return TCPProcessCmdResults.RESULT_OK;
                }

                string strcmd = ""; // 返回字符串

                // 执行分解
                EFluorescentGemResolveErrorCode err = FluorescentGemResolve(client, nBagIndex, nResolveCount);

                // 结果(int)[0=成功，非0=错误代码]
                strcmd = string.Format("{0}", (int)err);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion
        
        #region 荧光宝石升级
        /// <summary>
        /// 荧光宝石升级
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public TCPProcessCmdResults ProcessFluorescentGemUp(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            FluorescentGemUpTransferData upData = null;
            try
            {
                upData = DataHelper.BytesToObject<FluorescentGemUpTransferData>(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // FluorescentGemUpTransferData
                if (null == upData)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令结构解析错误:FluorescentGemUpTransferData, CMD={0}", (TCPGameServerCmds)nID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != upData._RoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), upData._RoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 检查是否开启
                if (!IsOpenFluorescentGem(client))
                {
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, string.Format("{0}:{1}", (int)EFluorescentGemUpErrorCode.NotOpen, -1), nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                string strcmd = ""; // 返回字符串
                int nNewGoodsDBID = -1; // 升级后的宝石dbid
                // 执行升级
                EFluorescentGemUpErrorCode err = FluorescentGemUp(client, upData, out nNewGoodsDBID);

                // 结果(int)[0=成功 + 升级后的宝石dbid(int)，非0=错误代码]
                strcmd = string.Format("{0}:{1}", (int)err, nNewGoodsDBID);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion

        #region 荧光宝石装备
        /// <summary>
        /// 荧光宝石装备
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public TCPProcessCmdResults ProcessFluorescentGemEquip(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // RoleID(int) + 宝石在背包的格子索引(int) + 佩戴部位索引(int) + 宝石类型(int)
                string[] fields = cmdData.Split(':');
                if (fields.Length != 4)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                int nBagIndex = Convert.ToInt32(fields[1]); // 格子索引
                int nPositionIndex = Convert.ToInt32(fields[2]); // 部位索引
                int nGemType = Convert.ToInt32(fields[3]); // 宝石类型

                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 检查是否开启
                if (!IsOpenFluorescentGem(client))
                {
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, (((int)EFluorescentGemEquipErrorCode.NotOpen).ToString()), nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                string strcmd = ""; // 返回字符串

                // 执行装备
                EFluorescentGemEquipErrorCode err = FluorescentGemEquip(client, nBagIndex, nPositionIndex, nGemType);

                // 结果(int)[0=成功，非0=错误代码]
                strcmd = string.Format("{0}", (int)err);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion

        #region 荧光宝石卸下
        /// <summary>
        /// 荧光宝石卸下
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public TCPProcessCmdResults ProcessFluorescentGemUnEquip(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // RoleID(int) + 佩戴部位索引(int) + 卸下类型[0=单个 + 宝石类型(int)，1=全部]
                string[] fields = cmdData.Split(':');
                if (fields.Length != 3 && fields.Length != 4)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                int nPositionIndex = Convert.ToInt32(fields[1]); // 部位索引
                int nUnEquipType = Convert.ToInt32(fields[2]); // 卸下类型
                int nGemType = 0; // 宝石类型
                if(nUnEquipType == 0)
                    nGemType = Convert.ToInt32(fields[3]);

                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 检查是否开启
                if (!IsOpenFluorescentGem(client))
                {
                    client.sendCmd(nID, string.Format("{0}", (int)EFluorescentGemUnEquipErrorCode.NotOpen));
                    return TCPProcessCmdResults.RESULT_OK;
                }

                string strcmd = ""; // 返回字符串

                // 执行装备
                EFluorescentGemUnEquipErrorCode err = FluorescentGemUnEquip(client, nUnEquipType, nPositionIndex, nGemType);

                // 结果(int)[0=成功，非0=错误代码]
                strcmd = string.Format("{0}", (int)err);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion

        #endregion
        
        #endregion

        #region GM工具测试
        /// <summary>
        /// GM命令清除宝石背包
        /// </summary>
        /// <param name="client"></param>
        public void GMClearGemBag(GameClient client)
        {
            if (null == client)
                return;

            List<GoodsData> list = new List<GoodsData>(client.ClientData.FluorescentGemData.GemBagList);
            /*foreach (var item in client.ClientData.FluorescentGemData.GemBagDict)
            {
                GoodsData goods = item.Value;
                if (null == goods)
                    continue;

                list.Add(goods);
            }*/

            for (int i = 0; i < list.Count; ++i)
            {
                // 进行分解 通知DB 物品数量改变
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, list[i], list[i].GCount, false))
                    continue;
            }
            list.Clear();
           // client.ClientData.FluorescentGemData.GemBagDict.Clear();
            client.ClientData.FluorescentGemData.GemEquipList.Clear();
        }

        /// <summary>
        /// GM命令增加荧光粉末
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nPoint"></param>
        public void GMAddFluorescentPoint(GameClient client, int nPoint)
        {
            if (null == client)
                return;

            AddFluorescentPoint(client, nPoint, "GM命令增加");
        }

        /// <summary>
        /// GM命令减少荧光粉末
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nPoint"></param>
        public void GMDecFluorescentPoint(GameClient client, int nPoint)
        {
            if (null == client)
                return;

            DecFluorescentPoint(client, nPoint, "GM命令减少");
        }

        #endregion
    }
}
