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
using GameServer.Core.Executor;

namespace GameServer.Logic.MerlinMagicBook
{
    /// <summary>
    /// 梅林魔法书管理器 [XSea 2015/6/18]
    /// </summary>
    public class MerlinMagicBookManager
    {
        #region 成员变量
        /// <summary>
        /// 梅林魔法书升阶配置字典 key = 阶数
        /// </summary>
        private Dictionary<int, MerlinLevelUpConfigData> MerlinLevelUpConfigDict = new Dictionary<int, MerlinLevelUpConfigData>();

        /// <summary>
        /// 梅林魔法书升星配置字典 key = 阶数 * 1000 + 星数
        /// </summary>
        private Dictionary<int, MerlinStarUpConfigData> MerlinStarUpConfigDict = new Dictionary<int, MerlinStarUpConfigData>();

        /// <summary>
        /// 梅林魔法书秘语配置字典 key = 阶数
        /// </summary>
        private Dictionary<int, MerlinSecretConfigData> MerlinSecretConfigDict = new Dictionary<int, MerlinSecretConfigData>();

        /// <summary>
        /// 梅林秘语检查间隔
        /// </summary>
        private long nextCheckTime = 0;
        #endregion

        #region private函数

        #region 读取梅林魔法书升阶配置
        /// <summary>
        /// 读取梅林魔法书升阶配置
        /// </summary>
        private void LoadMerlinLevelUpConfigData()
        {
            try
            {
                lock (MerlinLevelUpConfigDict)
                {
                    string fileName = MerlinMagicBookDefine.MAGIC_BOOK_PATH; // 配置文件地址
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName)); // 移除缓存
                    XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName)); // 获取XML
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件异常", fileName));
                        return;
                    }

                    IEnumerable<XElement> xmlItems = xml.Elements();

                    // 清空容器
                    MerlinLevelUpConfigDict.Clear();

                    // 放入容器管理 key = 阶数，value = MerlinLevelUpConfigData
                    foreach (var xmlItem in xmlItems)
                    {
                        // 默认就是1阶，所以不需要读升到1阶的数据
                        if ((int)Global.GetSafeAttributeLong(xmlItem, "Level") > 1)
                        {
                            MerlinLevelUpConfigData tmpData = new MerlinLevelUpConfigData();
                            tmpData._Level = (int)Global.GetSafeAttributeLong(xmlItem, "Level"); // 阶数
                            tmpData._LuckyOne = (int)Global.GetSafeAttributeLong(xmlItem, "LuckyOne"); // 幸运点
                            tmpData._LuckyTwo = (int)Global.GetSafeAttributeLong(xmlItem, "LuckyTwo"); // 触发几率幸运点
                            tmpData._Rate = Global.GetSafeAttributeDouble(xmlItem, "LuckyTwoRate"); // 升阶几率
                            long[] NeedGoods = Global.GetSafeAttributeLongArray(xmlItem, "NeedGoods"); // 升阶所需物品信息
                            if (NeedGoods.Length != 2)
                            {
                                LogManager.WriteLog(LogTypes.Error, "梅林魔法书升阶数据有误，无法读取");
                                return;
                            }
                            tmpData._NeedGoodsID = (int)NeedGoods[0]; // 物品id
                            tmpData._NeedGoodsCount = (int)NeedGoods[1]; // 物品数量
                            tmpData._NeedDiamond = (int)Global.GetSafeAttributeLong(xmlItem, "NeedZuanShi"); // 顶替物品所需钻石

                            // 加入字典
                            MerlinLevelUpConfigDict[tmpData._Level] = tmpData;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("load xml file : {0} fail", string.Format("Config/SystemParams.xml-LoadMerlinLevelUpConfigData")));
            }
        }
        #endregion

        #region 读取梅林魔法书升阶配置
        /// <summary>
        /// 读取梅林魔法书升阶配置
        /// </summary>
        private void LoadMerlinStarUpConfigData()
        {
            try
            {
                lock (MerlinStarUpConfigDict)
                {
                    string fileName = MerlinMagicBookDefine.MAGIC_BOOK_STAR_PATH; // 配置文件地址
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName)); // 移除缓存
                    XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName)); // 获取XML
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件异常", fileName));
                        return;
                    }

                    IEnumerable<XElement> xmlItems = xml.Elements();

                    // 清空容器
                    MerlinStarUpConfigDict.Clear();

                    // 放入容器管理 key = SystemCode，value = IsOpen(0=不开，1=开放)
                    foreach (var xmlItem in xmlItems)
                    {
                        MerlinStarUpConfigData tmpData = new MerlinStarUpConfigData();
                        tmpData._Level = (int)Global.GetSafeAttributeLong(xmlItem, "Level"); // 阶数
                        tmpData._StarNum = (int)Global.GetSafeAttributeLong(xmlItem, "Star"); // 星数
                        tmpData._MinAttackV = (int)Global.GetSafeAttributeLong(xmlItem, "MinAttackV"); // 最小物攻
                        tmpData._MaxAttackV = (int)Global.GetSafeAttributeLong(xmlItem, "MaxAttackV"); // 最大物攻
                        tmpData._MinMAttackV = (int)Global.GetSafeAttributeLong(xmlItem, "MinMAttackV"); // 最小魔攻
                        tmpData._MaxMAttackV = (int)Global.GetSafeAttributeLong(xmlItem, "MaxMAttackV"); // 最大魔攻
                        tmpData._MinDefenseV = (int)Global.GetSafeAttributeLong(xmlItem, "MinDefenseV"); // 最小物防
                        tmpData._MaxDefenseV = (int)Global.GetSafeAttributeLong(xmlItem, "MaxDefenseV"); // 最大物防
                        tmpData._MinMDefenseV = (int)Global.GetSafeAttributeLong(xmlItem, "MinMDefenseV"); // 最小魔防
                        tmpData._MaxMDefenseV = (int)Global.GetSafeAttributeLong(xmlItem, "MaxMDefenseV"); // 最大魔防
                        tmpData._HitV = (int)Global.GetSafeAttributeLong(xmlItem, "HitV"); // 命中
                        tmpData._DodgeV = (int)Global.GetSafeAttributeLong(xmlItem, "Dodge"); // 闪避
                        tmpData._MaxHpV = (int)Global.GetSafeAttributeLong(xmlItem, "MaxLifeV"); // 生命上限

                        tmpData._ReviveP = Global.GetSafeAttributeDouble(xmlItem, "Revive"); // 重生几率
                        tmpData._MpRecoverP = Global.GetSafeAttributeDouble(xmlItem, "MagicRecover"); // 魔法完全恢复

                        long[] NeedGoods = Global.GetSafeAttributeLongArray(xmlItem, "NeedGoods"); // 升星所需物品
                        if (NeedGoods.Length == 2)
                        {
                            tmpData._NeedGoodsID = (int)NeedGoods[0]; // 物品id
                            tmpData._NeedGoodsCount = (int)NeedGoods[1]; // 物品数量    
                        }

                        tmpData._NeedDiamond = (int)Global.GetSafeAttributeLong(xmlItem, "NeedZuanShi"); // 顶替物品所需钻石
                        tmpData._NeedExp = (int)Global.GetSafeAttributeLong(xmlItem, "StarExp"); // 升星所需经验

                        // 成长经验与暴击几率
                        string tmpStr = Global.GetSafeAttributeStr(xmlItem, "GrowUp");

                        if (String.IsNullOrEmpty(tmpStr))
                        {
                            LogManager.WriteLog(LogTypes.Error, "梅林魔法书升星成长经验与暴击率有误，无法读取");
                            return;
                        }

                        // 根据“|”解析 xx,xx|xx,xx
                        string[] tmpStrArr1 = tmpStr.Split('|');

                        tmpData._AddExp = new int[2]; // 创建经验数组
                        tmpData._CritPercent = new double[2]; // 创建暴击几率数组

                        // 判断参数个数
                        if (tmpStrArr1.Length == 2)
                        {
                            for (int i = 0; i < tmpStrArr1.Length; ++i)
                            {
                                //根据“，”解析 xx,xx
                                string[] tmpStrArr2 = tmpStrArr1[i].Split(',');

                                // 判断参数个数
                                if (tmpStrArr2.Length == 2)
                                {
                                    tmpData._AddExp[i] = Convert.ToInt32(tmpStrArr2[0]);
                                    tmpData._CritPercent[i] = Convert.ToDouble(tmpStrArr2[1]);
                                }
                            }
                        }
                        
                        // 组合一个key = 阶数 * 1000 +星数
                        int nKey = GetMerlinStarUpKey(tmpData._Level, tmpData._StarNum);

                        // 加入字典
                        MerlinStarUpConfigDict[nKey] = tmpData;
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("load xml file : {0} fail", string.Format("Config/SystemParams.xml-LoadMerlinStarUpConfigData")));
            }
        }
        #endregion

        #region 读取梅林魔法书秘语配置
        /// <summary>
        /// 读取梅林魔法书秘语配置
        /// </summary>
        private void LoadMerlinSecretConfigData()
        {
            try
            {
                lock (MerlinSecretConfigDict)
                {
                    string fileName = MerlinMagicBookDefine.MAGIC_WORD_PATH; // 配置文件地址
                    GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName)); // 移除缓存
                    XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName)); // 获取XML
                    if (null == xml)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件异常", fileName));
                        return;
                    }

                    IEnumerable<XElement> xmlItems = xml.Elements();

                    // 清空容器
                    MerlinSecretConfigDict.Clear();

                    // 放入容器管理 key = SystemCode，value = IsOpen(0=不开，1=开放)
                    foreach (var xmlItem in xmlItems)
                    {
                        MerlinSecretConfigData tmpData = new MerlinSecretConfigData();
                        tmpData._Level = (int)Global.GetSafeAttributeLong(xmlItem, "Level"); // 阶数

                        long[] NeedGoods = Global.GetSafeAttributeLongArray(xmlItem, "NeedGoods"); // 升阶所需物品信息
                        if (NeedGoods.Length != 2)
                        {
                            LogManager.WriteLog(LogTypes.Error, "梅林魔法书秘语数据有误，无法读取");
                            return;
                        }

                        tmpData._NeedGoodsID = (int)NeedGoods[0]; // 物品id
                        tmpData._NeedGoodsCount = (int)NeedGoods[1]; // 物品数量

                        long[] lNums = Global.GetSafeAttributeLongArray(xmlItem, "Num"); // 可随机到的总值库

                        tmpData._Num = new int[lNums.Length]; // 创建总值库数组

                        for (int i = 0; i < lNums.Length; ++i)
                            tmpData._Num[i] = Convert.ToInt32(lNums[i]);

                        // 加入字典
                        MerlinSecretConfigDict[tmpData._Level] = tmpData;
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("load xml file : {0} fail", string.Format("Config/SystemParams.xml-LoadMerlinSecretConfigData")));
            }
        }
        #endregion

        #region 获取梅林魔法书升星静态数据key
        /// <summary>
        /// 获取梅林魔法书升星静态数据key
        /// </summary>
        /// <param name="nLevel">阶数</param>
        /// <param name="nStarNum">星数</param>
        /// <returns></returns>
        private int GetMerlinStarUpKey(int nLevel, int nStarNum)
        {
            // 组合一个key = 阶数 * 1000 +星数
            return nLevel * 1000 + nStarNum;
        }
        #endregion

        #region 是否在梅林秘语持续时间内
        /// <summary>
        /// 是否在梅林秘语持续时间内
        /// </summary>
        /// <param name="client">角色</param>
        /// <returns></returns>
        private bool IsMerlinSecretTime(GameClient client)
        {
            long lNowTicks = TimeUtil.NOW(); // 毫秒

            // 检查秘语属性持续时间 当前时间-目标时间 < 0 在持续时间内
            if (lNowTicks - client.ClientData.MerlinData._ToTicks < 0)
                return true;

            return false;
        }
        #endregion

        #region 刷新梅林魔法书二级属性
        private void RefreshMerlinSecondAttr(GameClient client, int nLevel, int nStarNum)
        {
            // 获取梅林魔法书升星静态属性key
            int nKey = GetMerlinStarUpKey(nLevel, nStarNum);

            // 梅林魔法书升星静态数据
            MerlinStarUpConfigData starData = null;
            lock (MerlinStarUpConfigDict)
            {
                if (!MerlinStarUpConfigDict.TryGetValue(nKey, out starData) || null == starData)
                    return;
            }

            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.MinAttack, starData._MinAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.MaxAttack, starData._MaxAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.MinMAttack, starData._MinMAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.MaxMAttack, starData._MaxMAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.MinDefense, starData._MinDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.MaxDefense, starData._MaxDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.MinMDefense, starData._MinMDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.MaxMDefense, starData._MaxMDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.HitV, starData._HitV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.Dodge, starData._DodgeV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.MaxLifeV, starData._MaxHpV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.AutoRevivePercent, starData._ReviveP);
        }
        #endregion

        #region 刷新梅林魔法书秘语二级属性
        /// <summary>
        /// 刷新梅林魔法书秘语二级属性
        /// </summary>
        /// <param name="client">角色</param>
        private void RefreshMerlinSecretSecondAttr(GameClient client)
        {
            if (client.ClientData.MerlinData._ActiveAttr.ContainsKey((int)EMerlinSecretAttrType.EMSAT_FrozenP))
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.FrozenPercent, client.ClientData.MerlinData._ActiveAttr[(int)EMerlinSecretAttrType.EMSAT_FrozenP] / 100);
            if (client.ClientData.MerlinData._ActiveAttr.ContainsKey((int)EMerlinSecretAttrType.EMSAT_PalsyP))
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.PalsyPercent, client.ClientData.MerlinData._ActiveAttr[(int)EMerlinSecretAttrType.EMSAT_PalsyP] / 100);
            if (client.ClientData.MerlinData._ActiveAttr.ContainsKey((int)EMerlinSecretAttrType.EMSAT_SpeedDownP))
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.SpeedDownPercent, client.ClientData.MerlinData._ActiveAttr[(int)EMerlinSecretAttrType.EMSAT_SpeedDownP] / 100);
            if (client.ClientData.MerlinData._ActiveAttr.ContainsKey((int)EMerlinSecretAttrType.EMSAT_BlowP))
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.MerlinMagicBook, (int)ExtPropIndexes.BlowPercent, client.ClientData.MerlinData._ActiveAttr[(int)EMerlinSecretAttrType.EMSAT_BlowP] / 100);
        }
        #endregion

        #region 重置梅林魔法书生效的秘语属性
        /// <summary>
        /// 重置梅林魔法书生效的秘语属性
        /// </summary>
        /// <param name="client">角色</param>
        private void ResetActiveSecretAttr(GameClient client)
        {
            for (int i = 0; i < client.ClientData.MerlinData._ActiveAttr.Count; ++i)
                client.ClientData.MerlinData._ActiveAttr[i] = 0;
        }
        #endregion

        #region 重置梅林魔法书未生效的秘语属性
        /// <summary>
        /// 重置梅林魔法书未生效的秘语属性
        /// </summary>
        /// <param name="client">角色</param>
        private void ResetUnActiveSecretAttr(GameClient client)
        {
            for (int i = 0; i < client.ClientData.MerlinData._UnActiveAttr.Count; ++i)
                client.ClientData.MerlinData._UnActiveAttr[i] = 0;
        }
        #endregion

        #region 执行梅林魔法书升星
        /// <summary>
        /// 执行梅林魔法书升星
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="bIsDiamond">是否使用钻石</param>
        /// <param name="nIsCrit">返回是否暴击0=没暴击，1=暴击</param>
        /// <param name="nOutAddExp">返回加了多少经验</param>
        /// <returns></returns>
        private EMerlinStarUpErrorCode MerlinStarUp(GameClient client, bool bIsDiamond, out int nIsCrit, out int nOutAddExp)
        {
            nIsCrit = 0; // 默认0=没暴击，1=暴击
            nOutAddExp = 0; // 默认0经验
            int nRoleID = client.ClientData.RoleID;
            int nCurStarNum = client.ClientData.MerlinData._StarNum; // 当前星数
            int nCurLevel = client.ClientData.MerlinData._Level; // 当前阶数
            string strcmd = ""; // 返回字符串
            int nUseType = 0; // 升星类型 0=使用物品，1=使用钻石

            try
            {
                // 阶数异常
                if (nCurLevel <= 0 || nCurLevel > MerlinSystemParamsConfigData._MaxLevelNum)
                    return EMerlinStarUpErrorCode.LevelError;

                // 星数
                if (nCurStarNum < 0)
                    return EMerlinStarUpErrorCode.StarError;

                // 已达最高星，无法升星
                if (nCurStarNum >= MerlinSystemParamsConfigData._MaxStarNum)
                    return EMerlinStarUpErrorCode.MaxStarNum;

                // 升星静态数据
                MerlinStarUpConfigData starData = null;

                // 组合一个key = 阶数 * 1000 +星数
                int nKey = GetMerlinStarUpKey(nCurLevel, (nCurStarNum + 1));

                // 获取升星静态数据
                lock (MerlinStarUpConfigDict)
                {
                    if (!MerlinStarUpConfigDict.TryGetValue(nKey, out starData) || null == starData)
                        return EMerlinStarUpErrorCode.StarDataError;
                }

                // 检查升星所需物品
                SystemXmlItem needGoods = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(starData._NeedGoodsID, out needGoods))
                    return EMerlinStarUpErrorCode.NeedGoodsIDError;

                // 检查升星所需物品数量
                if (starData._NeedGoodsCount <= 0)
                    return EMerlinStarUpErrorCode.NeedGoodsCountError;

                // 获取背包里所需升星物品的数量
                int nTotalGoodsCount = Global.GetTotalGoodsCountByID(client, starData._NeedGoodsID);

                // 物品数量不足
                if (nTotalGoodsCount < starData._NeedGoodsCount)
                {
                    // 如果使用钻石
                    if (bIsDiamond)
                        nUseType = 1; // 物品不足，标识为使用钻石，默认为0=使用物品
                    else // 不使用钻石
                        return EMerlinStarUpErrorCode.GoodsNotEnough;
                }

                // 根据使用类型
                switch (nUseType)
                {
                    case 0: // 使用物品
                        {
                            // 扣除指定数量的物品
                            bool bUsedBinding = false;
                            bool bUsedTimeLimited = false;

                            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, starData._NeedGoodsID, starData._NeedGoodsCount, false, out bUsedBinding, out bUsedTimeLimited))
                                return EMerlinStarUpErrorCode.GoodsNotEnough;
                        }
                        break;
                    case 1: // 使用钻石
                        {
                            // 扣除钻石
                            if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, starData._NeedDiamond, "梅林魔法书升星"))
                                return EMerlinStarUpErrorCode.DiamondNotEnough;
                        }
                        break;
                }

                // 检查是否暴击
                int nRandom = Global.GetRandomNumber(0, 10001); // 获取随机数
                int nRate = (int)(starData._CritPercent[1] * 10000); // 成功率
                int nAddExp = 0; // 剩余可加的经验
                // 暴击了
                if (nRandom < nRate)
                {
                    nAddExp = starData._AddExp[1];
                    nIsCrit = 1; // 暴击了
                }
                else // 没暴击
                    nAddExp = starData._AddExp[0];

                nOutAddExp = nAddExp; // 返回加了多少经验

                int nMaxNeedExp = 0; // 升星所需经验上限
                int nNeedExp = 0; // 升星所需经验
                // 循环升星
                while (nAddExp > 0)
                {
                    // 所需经验上限
                    nMaxNeedExp = starData._NeedExp;

                    // 升星所需经验 = 所需经验上限 - 当前经验
                    nNeedExp = nMaxNeedExp - client.ClientData.MerlinData._StarExp;

                    // 剩余可加的经验经验大于等于所需经验
                    if (nAddExp >= nNeedExp)
                    {
                        // 星级 + 1
                        client.ClientData.MerlinData._StarNum += 1;

                        // 经验归0
                        client.ClientData.MerlinData._StarExp = 0;

                        // 将已加上的经验扣除
                        nAddExp -= nNeedExp;

                        // 已经满星 跳出
                        if (client.ClientData.MerlinData._StarNum >= MerlinSystemParamsConfigData._MaxStarNum)
                        {
                            // 如果当前不是最高阶 则将剩余经验累加上
                            if (nCurLevel < MerlinSystemParamsConfigData._MaxLevelNum)
                                client.ClientData.MerlinData._StarExp += nAddExp;
                            break;
                        }
                        else // 没有满星则继续循环检查剩余经验是否还能继续升星
                        {
                            // 没有剩余经验了 跳出
                            if (nAddExp <= 0)
                                break;
                            else // 还有剩余经验
                            {
                                // 找下一星的静态数据
                                // 组合一个key = 阶数 * 1000 +星数
                                nKey = GetMerlinStarUpKey(nCurLevel, (client.ClientData.MerlinData._StarNum));

                                // 获取下一星静态数据 没有则跳出，有则继续循环
                                lock (MerlinStarUpConfigDict)
                                {
                                    if (!MerlinStarUpConfigDict.TryGetValue(nKey, out starData))
                                        break;
                                }
                            }
                        }
                    }
                    else // 获得经验小于所需经验
                    {
                        // 累加经验 跳出
                        client.ClientData.MerlinData._StarExp += nAddExp;
                        break;
                    }
                }
                // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
                string strCmd = FormatUpdateDBMerlinStr(nRoleID, "*", "*", client.ClientData.MerlinData._StarNum, client.ClientData.MerlinData._StarExp, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*");
                // 通知DB更新数据
                UpdateMerlinMagicBookData2DB(client, strCmd);

                // 星数有改变才做属性相关的 更新
                if (nCurStarNum != client.ClientData.MerlinData._StarNum)
                {
                    // 刷新梅林魔法书二级属性
                    RefreshMerlinSecondAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum);

                    // 刷新梅林魔法书卓越属性
                    RefreshMerlinExcellenceAttr(client, nCurLevel, nCurStarNum, false); // 先减之前的属性
                    RefreshMerlinExcellenceAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum, true); // 再加新的属性

                    // 通知客户端属性变化
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                    // 总生命值和魔法值变化通知(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }

                if (client._IconStateMgr.CheckSpecialActivity(client))
                    client._IconStateMgr.SendIconStateToClient(client);

                return EMerlinStarUpErrorCode.Success;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return EMerlinStarUpErrorCode.Error;
        }
        #endregion

        #region 执行梅林魔法书升阶
        /// <summary>
        /// 执行梅林魔法书升阶
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="bIsDiamond">是否使用钻石</param>
        /// <returns></returns>
        private EMerlinLevelUpErrorCode MerlinLevelUp(GameClient client, bool bIsDiamond)
        {
            int nRoleID = client.ClientData.RoleID;
            int nCurLevel = client.ClientData.MerlinData._Level; // 当前阶数
            int nCurStarNum = client.ClientData.MerlinData._StarNum; // 当前星数
            string strcmd = ""; // 返回字符串
            int nUseType = 0; // 升阶类型 0=使用物品，1=使用钻石

            try
            {

                // 阶数异常
                if (nCurLevel <= 0)
                    return EMerlinLevelUpErrorCode.LevelError;

                // 已达最高阶，无法升阶
                if (nCurLevel >= MerlinSystemParamsConfigData._MaxLevelNum)
                    return EMerlinLevelUpErrorCode.MaxLevelNum;

                // 未达最高星，无法升阶
                if (nCurStarNum < MerlinSystemParamsConfigData._MaxStarNum)
                    return EMerlinLevelUpErrorCode.NotMaxStarNum;

                // 升阶静态数据
                MerlinLevelUpConfigData levelData = null;
            
                // 获取升阶静态数据
                lock (MerlinLevelUpConfigDict)
                {
                    if (!MerlinLevelUpConfigDict.TryGetValue(nCurLevel + 1, out levelData) || null == levelData)
                        return EMerlinLevelUpErrorCode.LevelDataError;
                }

                // 检查升阶所需物品
                SystemXmlItem needGoods = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(levelData._NeedGoodsID, out needGoods))
                    return EMerlinLevelUpErrorCode.NeedGoodsIDError;

                // 检查升阶所需物品数量
                if (levelData._NeedGoodsCount <= 0)
                    return EMerlinLevelUpErrorCode.NeedGoodsCountError;

                // 获取背包里所需升阶物品的数量
                int nTotalGoodsCount = Global.GetTotalGoodsCountByID(client, levelData._NeedGoodsID);

                // 物品数量不足
                if (nTotalGoodsCount < levelData._NeedGoodsCount)
                {
                    // 如果使用钻石
                    if (bIsDiamond)
                        nUseType = 1; // 物品不足，标识为使用钻石，默认为0=使用物品
                    else // 不使用钻石
                        return EMerlinLevelUpErrorCode.GoodsNotEnough;
                }

                // 根据使用类型
                switch (nUseType)
                {
                    case 0: // 使用物品
                        {
                            // 扣除指定数量的物品
                            bool bUsedBinding = false;
                            bool bUsedTimeLimited = false;

                            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, levelData._NeedGoodsID, levelData._NeedGoodsCount, false, out bUsedBinding, out bUsedTimeLimited))
                                return EMerlinLevelUpErrorCode.GoodsNotEnough;
                        }
                        break;
                    case 1: // 使用钻石
                        {
                            // 扣除钻石
                            if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, levelData._NeedDiamond, "梅林魔法书升阶"))
                                return EMerlinLevelUpErrorCode.DiamondNotEnough;
                        }
                        break;
                }

                // 如果没有幸运点则校正为幸运点起点
                if (client.ClientData.MerlinData._LuckyPoint <= 0)
                    client.ClientData.MerlinData._LuckyPoint = levelData._LuckyOne;

                // 幸运点递增
                client.ClientData.MerlinData._LuckyPoint++;

                // 幸运点不够，直接判定为失败
                if (client.ClientData.MerlinData._LuckyPoint < levelData._LuckyTwo)
                {
                    // 升阶失败次数 + 1
                    client.ClientData.MerlinData._LevelUpFailNum++;

                    // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
                    string strCmd = FormatUpdateDBMerlinStr(nRoleID, "*", client.ClientData.MerlinData._LevelUpFailNum, "*", "*", client.ClientData.MerlinData._LuckyPoint, "*", "*", "*", "*", "*", "*", "*", "*", "*");

                    // 通知DB更新数据
                    UpdateMerlinMagicBookData2DB(client, strCmd);

                    return EMerlinLevelUpErrorCode.Fail;
                }

                // 幸运点够了

                // 检查升阶真实成功率
                int nRandom = Global.GetRandomNumber(0, 10001); // 获取随机数
                int nRate = (int)(levelData._Rate * 10000); // 成功率
                // 成功
                if (nRandom < nRate)
                {
                    // 阶段+1
                    client.ClientData.MerlinData._Level += 1;

                    // 升阶失败次数归0
                    client.ClientData.MerlinData._LevelUpFailNum = 0;

                    // 星数归0
                    client.ClientData.MerlinData._StarNum = 0;

                    // 幸运点归0
                    client.ClientData.MerlinData._LuckyPoint = 0;

                    // 如果已达最高阶 则 升星经验也归0
                    if (client.ClientData.MerlinData._Level >= MerlinSystemParamsConfigData._MaxLevelNum)
                        client.ClientData.MerlinData._StarExp = 0;

                    // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
                    string strCmd = FormatUpdateDBMerlinStr(nRoleID,
                        client.ClientData.MerlinData._Level,
                        client.ClientData.MerlinData._LevelUpFailNum,
                        client.ClientData.MerlinData._StarNum,
                        client.ClientData.MerlinData._StarExp,
                        client.ClientData.MerlinData._LuckyPoint,
                        "*", "*", "*", "*", "*", "*", "*", "*", "*");

                    // 通知DB更新数据
                    UpdateMerlinMagicBookData2DB(client, strCmd);
                }
                else // 失败
                {
                    // 升阶失败次数 + 1
                    client.ClientData.MerlinData._LevelUpFailNum++;

                    // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
                    string strCmd = FormatUpdateDBMerlinStr(nRoleID, "*", client.ClientData.MerlinData._LevelUpFailNum, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*");

                    // 通知DB更新数据
                    UpdateMerlinMagicBookData2DB(client, strCmd);

                    return EMerlinLevelUpErrorCode.Fail;
                }

                // 刷新梅林魔法书二级属性
                RefreshMerlinSecondAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum);

                // 刷新梅林魔法书卓越属性属性
                RefreshMerlinExcellenceAttr(client, nCurLevel, nCurStarNum, false); // 先减之前的属性
                RefreshMerlinExcellenceAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum, true); // 再加新的属性

                // 通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                if (client._IconStateMgr.CheckSpecialActivity(client))
                    client._IconStateMgr.SendIconStateToClient(client);

                return EMerlinLevelUpErrorCode.Success;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return EMerlinLevelUpErrorCode.Error;
        }
        #endregion

        #region 执行梅林魔法书擦拭秘语
        /// <summary>
        /// 执行梅林魔法书擦拭秘语
        /// </summary>
        /// <param name="client">角色</param>
        /// <returns></returns>
        private EMerlinSecretAttrUpdateErrorCode MerlinSecretAttrUpdate(GameClient client)
        {
            int nRoleID = client.ClientData.RoleID;
            int nCurLevel = client.ClientData.MerlinData._Level; // 当前阶数
            string strcmd = ""; // 返回字符串

            try
            {
                // 阶数异常
                if (nCurLevel <= 0 || nCurLevel > MerlinSystemParamsConfigData._MaxLevelNum)
                    return EMerlinSecretAttrUpdateErrorCode.LevelError;

                // 秘语静态数据
                MerlinSecretConfigData secretData = null;

                // 获取秘语静态数据
                lock (MerlinSecretConfigDict)
                {
                    if (!MerlinSecretConfigDict.TryGetValue(nCurLevel, out secretData) || null == secretData)
                        return EMerlinSecretAttrUpdateErrorCode.SecretDataError;
                }

                // 检查擦拭秘语所需物品
                SystemXmlItem needGoods = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(secretData._NeedGoodsID, out needGoods))
                    return EMerlinSecretAttrUpdateErrorCode.NeedGoodsIDError;

                // 检查升阶所需物品数量
                if (secretData._NeedGoodsCount <= 0)
                    return EMerlinSecretAttrUpdateErrorCode.NeedGoodsCountError;

                // 获取背包里所需擦拭秘语物品的数量
                int nTotalGoodsCount = Global.GetTotalGoodsCountByID(client, secretData._NeedGoodsID);

                // 物品数量不足
                if (nTotalGoodsCount < secretData._NeedGoodsCount)
                    return EMerlinSecretAttrUpdateErrorCode.GoodsNotEnough;

                // 扣除指定数量的物品
                bool bUsedBinding = false;
                bool bUsedTimeLimited = false;

                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, secretData._NeedGoodsID, secretData._NeedGoodsCount, false, out bUsedBinding, out bUsedTimeLimited))
                    return EMerlinSecretAttrUpdateErrorCode.GoodsNotEnough;

                // 先从总值库随机出本次给了多少点
                int nAddTotalPointIndex = Global.GetRandomNumber(0, secretData._Num.Length);
                int nAddTotalPoint = secretData._Num[nAddTotalPointIndex];

                // 先重置未生效的属性
                ResetUnActiveSecretAttr(client);

                // 将点数随机加到未生效属性上
                while (nAddTotalPoint > 0)
                {
                    // 当前属性点总和
                    int nTotalPoint = (int)(client.ClientData.MerlinData._UnActiveAttr[(int)EMerlinSecretAttrType.EMSAT_FrozenP] +
                        client.ClientData.MerlinData._UnActiveAttr[(int)EMerlinSecretAttrType.EMSAT_PalsyP] +
                        client.ClientData.MerlinData._UnActiveAttr[(int)EMerlinSecretAttrType.EMSAT_SpeedDownP] +
                        client.ClientData.MerlinData._UnActiveAttr[(int)EMerlinSecretAttrType.EMSAT_BlowP]);

                    // 属性点上限=单项属性点上限 * 属性条数
                    int nMaxPoint = MerlinSystemParamsConfigData._MaxSecretAttrNum * (int)EMerlinSecretAttrType.EMSAT_MAX;

                    // 属性均已达上限就不加了
                    if (nTotalPoint >= nMaxPoint)
                        break;

                    // 随机找一个属性
                    int nAttrIndex = Global.GetRandomNumber(0, (int)EMerlinSecretAttrType.EMSAT_MAX);

                    // 某项属性已达上限
                    if (client.ClientData.MerlinData._UnActiveAttr[nAttrIndex] >= MerlinSystemParamsConfigData._MaxSecretAttrNum)
                        continue;

                    // 加在未生效属性上
                    client.ClientData.MerlinData._UnActiveAttr[nAttrIndex] += 1;

                    // 总点数 - 1
                    nAddTotalPoint--;
                }

                // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
                string strCmd = FormatUpdateDBMerlinStr(nRoleID, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*",
                    client.ClientData.MerlinData._UnActiveAttr[0],
                    client.ClientData.MerlinData._UnActiveAttr[1],
                    client.ClientData.MerlinData._UnActiveAttr[2],
                    client.ClientData.MerlinData._UnActiveAttr[3]);

                // 通知DB更新数据
                UpdateMerlinMagicBookData2DB(client, strCmd);

                return EMerlinSecretAttrUpdateErrorCode.Success;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return EMerlinSecretAttrUpdateErrorCode.Error;
        }
        #endregion

        #region 执行替换秘语属性
        /// <summary>
        /// 执行替换秘语属性
        /// </summary>
        /// <param name="client">角色</param>
        private EMerlinSecretAttrReplaceErrorCode MerlinSecretAttrReplace(GameClient client)
        {
            bool bIsReplace = false; // 是否替换

            try
            {
                // 加一层检查，如果为激活的秘语属性都是0 则不做替换
                for (int i = 0; i < client.ClientData.MerlinData._UnActiveAttr.Count; ++i)
                {
                    if (client.ClientData.MerlinData._UnActiveAttr[i] > 0)
                    {
                        bIsReplace = true;
                        break;
                    }
                }

                if (!bIsReplace)
                    return EMerlinSecretAttrReplaceErrorCode.NotUpdate;

                // 先将属性替换
                for (int i = 0; i < client.ClientData.MerlinData._ActiveAttr.Count; ++i)
                    client.ClientData.MerlinData._ActiveAttr[i] = client.ClientData.MerlinData._UnActiveAttr[i];

                // 然后清除未生效属性
                ResetUnActiveSecretAttr(client);

                // 最后将新的秘语二级属性加上
                RefreshMerlinSecretSecondAttr(client);

                // 秘语持续时间
                client.ClientData.MerlinData._ToTicks = TimeUtil.NOW() + MerlinSystemParamsConfigData._MaxSecretTime * 60 * 1000; // 分钟 -> 秒 - > 毫秒 [XSea 2015/6/25]

                // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
                string strCmd = FormatUpdateDBMerlinStr(client.ClientData.RoleID, "*", "*", "*", "*", "*",
                    client.ClientData.MerlinData._ToTicks,
                    client.ClientData.MerlinData._ActiveAttr[0],
                    client.ClientData.MerlinData._ActiveAttr[1],
                    client.ClientData.MerlinData._ActiveAttr[2],
                    client.ClientData.MerlinData._ActiveAttr[3],
                    client.ClientData.MerlinData._UnActiveAttr[0],
                    client.ClientData.MerlinData._UnActiveAttr[1],
                    client.ClientData.MerlinData._UnActiveAttr[2],
                    client.ClientData.MerlinData._UnActiveAttr[3]);

                // 通知DB更新梅林魔法书信息
                UpdateMerlinMagicBookData2DB(client, strCmd);

                // 通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                return EMerlinSecretAttrReplaceErrorCode.Success;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return EMerlinSecretAttrReplaceErrorCode.Error;
        }
        #endregion

        #region 执行放弃替换秘语属性
        /// <summary>
        /// 执行放弃替换秘语属性
        /// </summary>
        /// <param name="client">角色</param>
        private void MerlinSecretAttrNotReplace(GameClient client)
        {
            try
            {
                // 执行清除未生效的秘语属性
                ResetUnActiveSecretAttr(client);

                // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
                string strCmd = FormatUpdateDBMerlinStr(client.ClientData.RoleID, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*",
                    client.ClientData.MerlinData._UnActiveAttr[0],
                    client.ClientData.MerlinData._UnActiveAttr[1],
                    client.ClientData.MerlinData._UnActiveAttr[2],
                    client.ClientData.MerlinData._UnActiveAttr[3]);

                // 通知DB更新梅林魔法书信息
                UpdateMerlinMagicBookData2DB(client, strCmd);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
        }
        #endregion

        #region 数据库操作
        /// <summary>
        /// 通知db创建梅林魔法书
        /// </summary>
        /// <param name="client">角色</param>
        /// <returns></returns>
        private bool CreateMerlinMagicBookData2DB(GameClient client)
        {
            try
            {
                byte[] dataBytes = DataHelper.ObjectToBytes<MerlinGrowthSaveDBData>(client.ClientData.MerlinData);
                byte[] byRoleID = BitConverter.GetBytes(client.ClientData.RoleID);
                byte[] sendBytes = new byte[dataBytes.Length + 4];
                Array.Copy(byRoleID, sendBytes, 4);
                Array.Copy(dataBytes, 0, sendBytes, 4, dataBytes.Length);

                return Global.sendToDB<bool, byte[]>((int)TCPGameServerCmds.CMD_DB_MERLIN_CREATE, sendBytes, client.ServerId);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
            return false;
        }

        /// <summary>
        /// 通知db更新梅林魔法书
        /// </summary>
        /// <param name="client">角色</param>
        /// <returns></returns>
        private bool UpdateMerlinMagicBookData2DB(GameClient client, string strCmd)
        {
            byte[] bytesCmd = new UTF8Encoding().GetBytes(strCmd);
            return Global.sendToDB<bool, byte[]>((int)TCPGameServerCmds.CMD_DB_MERLIN_UPDATE, bytesCmd, client.ServerId);
        }

        /// <summary>
        /// 格式化更新数据库梅林魔法书字符串
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static string FormatUpdateDBMerlinStr(params object[] args)
        {
            if (args.Length != 15)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("FormatUpdateDBMerlinStr, 参数个数不对{0}", args.Length));
                return null;
            }

            return string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}:{13}:{14}", args);
        }
        #endregion

        #endregion

        #region public函数

        #region 刷新梅林魔法书卓越属性
        /// <summary>
        /// 刷新梅林魔法书卓越属性
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nLevel">阶数</param>
        /// <param name="nStarNum">星数</param>
        public void RefreshMerlinExcellenceAttr(GameClient client, int nLevel, int nStarNum, bool bToAdd)
        {
            // 获取梅林魔法书升星静态属性key
            int nKey = GetMerlinStarUpKey(nLevel, nStarNum);

            // 梅林魔法书升星静态数据
            MerlinStarUpConfigData starData = null;
            lock (MerlinStarUpConfigDict)
            {
                if (!MerlinStarUpConfigDict.TryGetValue(nKey, out starData) || null == starData)
                    return;
            }

            // 属性没变动 直接退出
            if (starData._MpRecoverP <= 0)
                return;

            // 加属性
            if (bToAdd)
            {
                // 魔法完全回复几率 特殊 属于卓越属性
                client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP16] += starData._MpRecoverP;
            }
            else // 减属性
            {
                // 魔法完全回复几率 特殊 属于卓越属性
                client.ClientData.ExcellenceProp[(int)ExcellencePorp.EXCELLENCEPORP16] -= starData._MpRecoverP;
            }
        }
        #endregion

        #region 角色上线初始化创建梅林魔法书
        /// <summary>
        /// 角色上线初始化创建梅林魔法书
        /// </summary>
        /// <param name="client">角色</param>
        public void OnLoginInitMerlinMagicBook(GameClient client)
        {
            try
            {
                // 检查梅林魔法书是否开启
                if (!IsOpenMerlin(client))
                    return;

                if (null == client.ClientData.MerlinData)
                    client.ClientData.MerlinData = new MerlinGrowthSaveDBData();

                // 已初始化过了
                if (client.ClientData.MerlinData._Level >= 1)
                    return;

                client.ClientData.MerlinData._RoleID = client.ClientData.RoleID; // 角色id
                client.ClientData.MerlinData._Level = 1; // 初始化默认为1阶
                client.ClientData.MerlinData._Occupation = Global.CalcOriginalOccupationID(client); // 角色职业

                // 重置秘语属性
                ResetActiveSecretAttr(client);
                ResetUnActiveSecretAttr(client);

                // 通知DB创建数据
                CreateMerlinMagicBookData2DB(client);

                // 叹号通知客户端
                CheckMerlinSecretAttr(client);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
        }
        #endregion

        #region 角色上线增加梅林魔法书属性
        /// <summary>
        /// 角色上线增加梅林魔法书属性
        /// </summary>
        /// <param name="client">角色</param>
        public void OnLoginAddAttr(GameClient client)
        {
            try
            {
                if (!IsOpenMerlin(client))
                    return;

                // 刷新梅林魔法书卓越属性 ps: 这里上线起始不用刷新卓越属性，在startgame的时候卓越属性会重置，所以卓越属性需要加在RefreshEquipProp的client.ClientData.ResetExcellenceProp();之后 [XSea 2015/7/27]
                //RefreshMerlinExcellenceAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum, true);

                // 刷新梅林魔法书二级属性
                RefreshMerlinSecondAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum);

                // 刷新梅林魔法书秘语二级属性
                RefreshMerlinSecretSecondAttr(client);

                // 通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
        }
        #endregion

        #region 统一读取梅林魔法书配置文件
        /// <summary>
        /// 统一读取梅林魔法书配置文件
        /// </summary>
        public void LoadMerlinConfigData()
        {
            LoadMerlinLevelUpConfigData();
            LoadMerlinStarUpConfigData();
            LoadMerlinSecretConfigData();
        }
        #endregion

        #region 读取SystemParams中梅林魔法书的静态属性
        /// <summary>
        /// 读取SystemParams中梅林魔法书的静态属性
        /// </summary>
        public void LoadMerlinSystemParamsConfigData()
        {
            try
            {
                // 重生内置cd时间 单位：秒
                MerlinSystemParamsConfigData._ReviveCDTime = Convert.ToInt32(GameManager.systemParamsList.GetParamValueByName("ChongShengCD"));

                // 秘语属性每项最大值
                MerlinSystemParamsConfigData._MaxSecretAttrNum = Convert.ToInt32(GameManager.systemParamsList.GetParamValueByName("MagicWordMax"));

                // 秘语持续时间 单位：分钟
                MerlinSystemParamsConfigData._MaxSecretTime = Convert.ToInt32(GameManager.systemParamsList.GetParamValueByName("MagicWordTime"));

                // 最大阶数和阶数
                int[] ArrayMaxLevelAndStar = GameManager.systemParamsList.GetParamValueIntArrayByName("MagicBookLevel");
                if (ArrayMaxLevelAndStar.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, "梅林魔法书最大阶数星数有误，无法读取");
                    return;
                }

                // 最大阶数
                MerlinSystemParamsConfigData._MaxLevelNum = ArrayMaxLevelAndStar[0];

                // 最大星数
                MerlinSystemParamsConfigData._MaxStarNum = ArrayMaxLevelAndStar[1];
            }
            catch (Exception)
            {
                throw new Exception(string.Format("load xml file : {0} fail", string.Format("Config/SystemParams.xml-LoadMerlinSystemParamsConfigData")));
            }
        }
        #endregion

        #region 检查梅林魔法书是否开启
        public bool IsOpenMerlin(GameClient client)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return false;
            }

            if (null == client)
                return false;

            // 检查版本是否开启
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.MerlinMagicBook))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("版本控制未开启梅林魔法书功能, RoleID={0}", client.ClientData.RoleID));
                return false;
            }

            // 检查功能是否开启
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.MerlinMagicBook))
            {
                return false;
            }

            return true;
        }
        #endregion

        #region 检查梅林秘语叹号
        public void CheckMerlinSecretAttr(GameClient client)
        {
            if (!IsOpenMerlin(client))
                return;

            // 如果在持续时间内
            if (IsMerlinSecretTime(client))
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.MerlinSecretAttr, false);
            else // 如果不再持续时间内
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.MerlinSecretAttr, true);
        }
        #endregion

        #region 处理梅林秘语持续时间
        /// <summary>
        /// 处理梅林秘语持续时间
        /// </summary>
        /// <param name="client"></param>
        public void DoMerlinSecretTime(GameClient client)
        {
            try
            {
                // 检查梅林魔法书是否开启
                if (!IsOpenMerlin(client))
                    return;

                long lNowTicks = TimeUtil.NOW(); // 当前毫秒

                // 每5秒检查一次
                if (lNowTicks - nextCheckTime < 5 * 1000)
                    return;

                nextCheckTime = lNowTicks;

                // 在秘语属性持续时间中
                if (IsMerlinSecretTime(client))
                    return;

                if (client.ClientData.MerlinData._ToTicks > 0)
                {
                    // 已超过秘语持续时间 时间归0
                    client.ClientData.MerlinData._ToTicks = 0;

                    // 重置秘语生效属性
                    ResetActiveSecretAttr(client);

                    // 刷新秘语二级属性
                    RefreshMerlinSecretSecondAttr(client);

                    // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
                    string strCmd = FormatUpdateDBMerlinStr(client.ClientData.RoleID, "*", "*", "*", "*", "*",
                        client.ClientData.MerlinData._ToTicks,
                        client.ClientData.MerlinData._ActiveAttr[0],
                        client.ClientData.MerlinData._ActiveAttr[1],
                        client.ClientData.MerlinData._ActiveAttr[2],
                        client.ClientData.MerlinData._ActiveAttr[3],
                        "*", "*", "*", "*");

                    // 通知DB更新梅林魔法书信息
                    UpdateMerlinMagicBookData2DB(client, strCmd);

                    // 通知客户端属性变化
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                    // 总生命值和魔法值变化通知(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                    // 叹号通知客户端
                    CheckMerlinSecretAttr(client);
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
        }
        #endregion

        #region 初始化创建梅林魔法书
        /// <summary>
        /// 初始化创建梅林魔法书
        /// </summary>
        /// <param name="client">角色</param>
        public void InitMerlinMagicBook(GameClient client)
        {
            try
            {
                // 检查梅林魔法书是否开启
                if (!IsOpenMerlin(client))
                    return;

                if (null == client.ClientData.MerlinData)
                    client.ClientData.MerlinData = new MerlinGrowthSaveDBData();

                // 已初始化过了
                if (client.ClientData.MerlinData._Level >= 1)
                    return;

                client.ClientData.MerlinData._RoleID = client.ClientData.RoleID; // 角色id
                client.ClientData.MerlinData._Level = 1; // 初始化默认为1阶
                client.ClientData.MerlinData._Occupation = Global.CalcOriginalOccupationID(client); // 角色职业

                // 重置秘语属性
                ResetActiveSecretAttr(client);
                ResetUnActiveSecretAttr(client);

                // 通知DB创建数据
                CreateMerlinMagicBookData2DB(client);

                // 刷新梅林魔法书二级属性
                RefreshMerlinSecondAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum);

                // 刷新梅林魔法书卓越属性
                RefreshMerlinExcellenceAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum, true);

                // 通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 叹号通知客户端
                CheckMerlinSecretAttr(client);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }
        }
        #endregion

        

        #region 客户请求查询梅林魔法书信息
        /// <summary>
        /// 客户端请求查询梅林魔法书信息
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
        public TCPProcessCmdResults ProcQueryMerlinMagicBookData(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                // RoleID(int)
                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 检查是否开启
                if (!IsOpenMerlin(client))
                    return TCPProcessCmdResults.RESULT_OK;

                // 空数据的话 去数据库找一下
                if (null == client.ClientData.MerlinData || client.ClientData.MerlinData._RoleID <= 0)
                {
                    client.ClientData.MerlinData = Global.sendToDB<MerlinGrowthSaveDBData, string>(
                        (int)TCPGameServerCmds.CMD_DB_MERLIN_QUERY, string.Format("{0}", client.ClientData.RoleID), client.ServerId);
                }

                tcpOutPacket = DataHelper.ObjectToTCPOutPacket(client.ClientData.MerlinData, pool, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion

        #region 客户端请求梅林魔法书升星
        /// <summary>
        /// 客户端请求梅林魔法书升星
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
        public TCPProcessCmdResults ProcMerlinMagicBookStarUp(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                // RoleID(int) + 是否使用钻石(int)[0=不用，1=用]
                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                int nIsDiamond = Convert.ToInt32(fields[1]); // 是否使用钻石 0=不用，1=用
                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 检查是否开启
                if (!IsOpenMerlin(client))
                    return TCPProcessCmdResults.RESULT_OK;

                string strcmd = ""; // 返回字符串
                bool bIsDiamond = nIsDiamond == 0 ? false : true; // 是否使用钻石

                int nIsCrit = 0; // 是否暴击，0=没有，1=暴击
                int nAddExp = 0; // 加了多少经验

                // 执行升星
                EMerlinStarUpErrorCode err = MerlinStarUp(client, bIsDiamond, out nIsCrit, out nAddExp);

                // 结果(int)[0=成功，非0=错误代码] + 当前星数(int) + 当前星数经验值(int) + 是否暴击(int)[0=没暴击，1=暴击] + 加了多少经验(int)
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)err, client.ClientData.MerlinData._StarNum, client.ClientData.MerlinData._StarExp, nIsCrit, nAddExp);
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

        #region 客户端请求梅林魔法书升阶
        /// <summary>
        /// 客户端请求梅林魔法书升阶
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
        public TCPProcessCmdResults ProcMerlinMagicBookLevelUp(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                // RoleID(int) + 是否使用钻石(int)[0=不用，1=用]
                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                int nIsDiamond = Convert.ToInt32(fields[1]); // 是否使用钻石 0=不用，1=用
                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 检查是否开启
                if (!IsOpenMerlin(client))
                    return TCPProcessCmdResults.RESULT_OK;

                string strcmd = ""; // 返回字符串
                bool bIsDiamond = nIsDiamond == 0 ? false : true; // 是否使用钻石

                // 执行升阶
                EMerlinLevelUpErrorCode err = MerlinLevelUp(client, bIsDiamond);

                // 结果(int)[0=成功，非0=错误代码] + 当前星数(int) + 当前星数经验值(int) + 当前阶数(int) + 升阶失败次数(int)
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)err, client.ClientData.MerlinData._StarNum, client.ClientData.MerlinData._StarExp, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._LevelUpFailNum);
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

        #region 客户端请求擦拭梅林魔法书秘语
        /// <summary>
        /// 客户端请求擦拭梅林魔法书秘语
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
        public TCPProcessCmdResults ProcMerlinSecretAttrUpdate(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                // RoleID(int)
                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 检查是否开启
                if (!IsOpenMerlin(client))
                    return TCPProcessCmdResults.RESULT_OK;

                string strcmd = ""; // 返回字符串

                // 执行擦拭秘语
                EMerlinSecretAttrUpdateErrorCode err = MerlinSecretAttrUpdate(client);

                // 结果(int)[0=成功，非0=错误代码] + 冰冻几率(double) + 麻痹几率(double) + 减速几率(double) + 重击几率(double)
                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", (int)err,
                   client.ClientData.MerlinData._UnActiveAttr[(int)EMerlinSecretAttrType.EMSAT_FrozenP],
                   client.ClientData.MerlinData._UnActiveAttr[(int)EMerlinSecretAttrType.EMSAT_PalsyP],
                   client.ClientData.MerlinData._UnActiveAttr[(int)EMerlinSecretAttrType.EMSAT_SpeedDownP],
                   client.ClientData.MerlinData._UnActiveAttr[(int)EMerlinSecretAttrType.EMSAT_BlowP]);

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

        #region 客户端请求替换梅林魔法书秘语
        /// <summary>
        /// 客户端请求擦拭梅林魔法书秘语
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
        public TCPProcessCmdResults ProcMerlinSecretAttrReplace(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                // RoleID(int)
                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!IsOpenMerlin(client))
                    return TCPProcessCmdResults.RESULT_OK;

                string strcmd = ""; // 返回字符串

                // 执行秘语属性替换
                EMerlinSecretAttrReplaceErrorCode err = MerlinSecretAttrReplace(client);

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

        #region 客户端请求放弃替换梅林魔法书秘语
        /// <summary>
        /// 客户端请求擦拭梅林魔法书秘语
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
        public TCPProcessCmdResults ProcMerlinSecretAttrNotReplace(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // RoleID(int)
                int nRoleID = Convert.ToInt32(fields[0]); // 角色id
                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!IsOpenMerlin(client))
                    return TCPProcessCmdResults.RESULT_OK;

                string strcmd = ""; // 返回字符串

                // 执行放弃替换秘语属性
                MerlinSecretAttrNotReplace(client);

                // 结果(int)[0=成功]
                strcmd = string.Format("{0}", (int)0);

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

        #region GM工具测试
        /// <summary>
        /// GM物品升星
        /// </summary>
        /// <param name="client"></param>
        public void GMMerlinStarUp1(GameClient client)
        {
            int nCrit = 0;
            int nAddExp = 0;
            MerlinStarUp(client, false, out nCrit, out nAddExp);
        }

        /// <summary>
        /// GM钻石升星
        /// </summary>
        /// <param name="client"></param>
        public void GMMerlinStarUp2(GameClient client)
        {
            int nCrit = 0;
            int nAddExp = 0;
            MerlinStarUp(client, true, out nCrit, out nAddExp);
        }

        /// <summary>
        /// GM物品升阶
        /// </summary>
        /// <param name="client"></param>
        public void GMMerlinLevelUp1(GameClient client)
        {
            MerlinLevelUp(client, false);
        }

        /// <summary>
        /// GM钻石升阶
        /// </summary>
        /// <param name="client"></param>
        public void GMMerlinLevelUp2(GameClient client)
        {
            MerlinLevelUp(client, true);
        }

        /// <summary>
        /// GM擦拭秘语
        /// </summary>
        /// <param name="client"></param>
        public void GMMerlinSecretUpdate(GameClient client)
        {
            MerlinSecretAttrUpdate(client);
        }

        /// <summary>
        /// GM替换秘语属性
        /// </summary>
        /// <param name="client"></param>
        public void GMMerlinSecretReplace(GameClient client)
        {
            MerlinSecretAttrReplace(client);
        }

        /// <summary>
        /// GM放弃替换秘语属性
        /// </summary>
        /// <param name="client"></param>
        public void GMMerlinSecretNotReplace(GameClient client)
        {
            MerlinSecretAttrNotReplace(client);
        }

        /// <summary>
        /// GM初始化梅林魔法书
        /// </summary>
        /// <param name="client"></param>
        public void GMMerlinInit(GameClient client)
        {
            InitMerlinMagicBook(client);
        }

        /// <summary>
        /// GM直接升阶至N
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nLevel"></param>
        /// <returns></returns>
        public string GMMerlinLevelUpToN(GameClient client, int nLevel)
        {
            if (client == null || !IsOpenMerlin(client))
            {
                return Global.GetLang("梅林魔法书未开启");
            }
            if (nLevel < 1)
                return Global.GetLang("阶数不可小于1");

            nLevel = Math.Min(nLevel, MerlinSystemParamsConfigData._MaxLevelNum); // 阶数

            int nCurLevel = client.ClientData.MerlinData._Level; // 当前阶数
            int nCurStarNum = client.ClientData.MerlinData._StarNum; // 当前星数

            client.ClientData.MerlinData._Level = nLevel;

            // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
            string strCmd = FormatUpdateDBMerlinStr(client.ClientData.RoleID, nLevel, 0, "*", "*", 0, "*", "*", "*", "*", "*", "*", "*", "*", "*");

            // 通知DB 更新数据
            if (!UpdateMerlinMagicBookData2DB(client, strCmd))
            {
                return Global.GetLang("设置梅林魔法书阶数失败");
            }

            // 刷新梅林魔法书二级属性
            RefreshMerlinSecondAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum);

            // 刷新梅林魔法书卓越属性
            RefreshMerlinExcellenceAttr(client, nCurLevel, nCurStarNum, false); // 先减之前的属性
            RefreshMerlinExcellenceAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum, true); // 再加新的属性

            // 通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            return Global.GetLang("设置梅林魔法书阶数成功");
        }

        /// <summary>
        /// GM直接升星至N
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nLevel"></param>
        /// <returns></returns>
        public string GMMerlinStarUpToN(GameClient client, int nStarNum)
        {
            if (client == null || !IsOpenMerlin(client))
            {
                return Global.GetLang("梅林魔法书未开启");
            }

            if (nStarNum < 0)
                return Global.GetLang("星数不可小于0");

            nStarNum = Math.Min(nStarNum, MerlinSystemParamsConfigData._MaxStarNum); // 星数

            int nCurLevel = client.ClientData.MerlinData._Level; // 当前阶数
            int nCurStarNum = client.ClientData.MerlinData._StarNum; // 当前星数

            client.ClientData.MerlinData._StarNum = nStarNum;

            // 格式化 角色id，0阶数，1升阶失败次数，2星数，3星级经验，4幸运点，5秘语结束时间，6-9激活秘语属性，10-13未激活秘语属性
            string strCmd = FormatUpdateDBMerlinStr(client.ClientData.RoleID, "*", "*", nStarNum, 0, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*");

            if (!UpdateMerlinMagicBookData2DB(client, strCmd))
            {
                return Global.GetLang("设置梅林魔法书星数失败");
            }

            // 刷新梅林魔法书二级属性
            RefreshMerlinSecondAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum);

            // 刷新梅林魔法书卓越属性
            RefreshMerlinExcellenceAttr(client, nCurLevel, nCurStarNum, false); // 先减之前的属性
            RefreshMerlinExcellenceAttr(client, client.ClientData.MerlinData._Level, client.ClientData.MerlinData._StarNum, true); // 再加新的属性

            // 通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            return Global.GetLang("设置梅林魔法书星数成功");
        }
        #endregion
    }
}
