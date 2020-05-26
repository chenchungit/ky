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

namespace GameServer.Logic
{
    enum LingYuError
    {
        Success = 0,          		// 成功
        NotOpen,					// 翅膀阶数或星级不满足翎羽开放条件
        LevelFull,			   		// 等级已满，无法提升
        NeedLevelUp,                // 必须先提升等级
        NeedSuitUp,				    // 必须先提升品阶
        SuitFull,					// 品阶已满，无法提升
        LevelUpMaterialNotEnough,	// 提升等级所需材料不足
        LevelUpJinBiNotEnough,		// 提升等级所需金币不足
        SuitUpMaterialNotEnough,	// 提升品阶所需材料不足
        SuitUpJinBiNotEnough,		// 提升品阶所需金币不足
        ErrorConfig,        		// 配置错误
        ErrorParams,        		// 传来的参数错误
        ZuanShiNotEnough,    	    // 钻石不足
        DBSERVERERROR,     		    // 与dbserver通信失败
    }

    class LingYuLevel
    {
        public int Level;                              //等级
        public int MinAttackV;                         //最小物攻
        public int MaxAttackV;                         //最大物攻
        public int MinMAttackV;                        //最小魔攻
        public int MaxMAttackV;                        //最大魔攻
        public int MinDefenseV;                        //最小物防
        public int MaxDefenseV;                        //最大物防
        public int MinMDefenseV;                       //最小魔防
        public int MaxMDefenseV;                       //最大魔防
        public int HitV;                               //命中
        public int LifeV;                              //生命上限
        public int GoodsCost;                          //单次升级消耗
        public int GoodsCostCnt;                       //单次升级消耗个数
        public int JinBiCost;                          //单次升级金币消耗
    }

    class LingYuSuit
    {
        public int Suit;                              //等级
        public int MinAttackV;                         //最小物攻
        public int MaxAttackV;                         //最大物攻
        public int MinMAttackV;                        //最小魔攻
        public int MaxMAttackV;                        //最大魔攻
        public int MinDefenseV;                        //最小物防
        public int MaxDefenseV;                        //最大物防
        public int MinMDefenseV;                       //最小魔防
        public int MaxMDefenseV;                       //最大魔防
        public int HitV;                               //命中
        public int LifeV;                              //生命上限
        public int GoodsCost;                          //单次升级消耗
        public int GoodsCostCnt;                       //单次升级消耗个数
        public int JinBiCost;                          //单次升级金币消耗
    }

    class LingYuType
    {
        public int Type;                      //类型编号
        public string Name;                     //名称
        public double LifeScale;                //生命上限比例
        public double AttackScale;              //物理攻击比例
        public double DefenseScale;             //物理防御比例
        public double MAttackScale;             //魔法攻击比例
        public double MDefenseScale;            //魔法防御比例
        public double HitScale;                 //命中比例
        public Dictionary<int, LingYuLevel> LevelDict = new Dictionary<int, LingYuLevel>();
        public Dictionary<int, LingYuSuit> SuitDict = new Dictionary<int, LingYuSuit>();
    }

    class LingYuCollect
    {
        public int Num;
        public int NeedSuit;
        public double Luck;
        public double DeLuck;
    }

    class LingYuManager
    {
        #region 配置数据

        private static string LingYuTypeFile = "Config/LingyuType.xml";
        private static string LingYuLevelUpFile = "Config/LingYuLevelUp.xml";
        private static string LingYuSuitUpFile = "Config/LingYuSuitUp.xml";
        private static string LingYuCollectFile = "Config/LingYucollect.xml";
        private static int LingYuLevelLimit = 0;
        private static int LingYuSuitLimit = 0;
        private const int DEFAULT_LINGYU_LEVEL = 1;
        private static Dictionary<int, LingYuType> LingYuTypeDict = new Dictionary<int, LingYuType>();
        private static List<LingYuCollect> LingYuCollectList = new List<LingYuCollect>();
        private static int[] SuitOfNotifyList = new int[]{3, 6, 9};

        #endregion

        public static void LoadConfig()
        {
            XElement xml = null;

            #region 加载LingYuTypeFile

            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(LingYuTypeFile));
            xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(LingYuTypeFile));
            if (xml == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", LingYuTypeFile));
            }
            else
            {
                try
                {
                    LingYuTypeDict.Clear();
                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null == xmlItem)
                            continue;

                        LingYuType lyType = new LingYuType();
                        lyType.Type = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "TypeID", "0"));
                        lyType.Name = Global.GetDefAttributeStr(xmlItem, "Name", "no-name");
                        lyType.LifeScale = Global.GetSafeAttributeDouble(xmlItem, "LifeScale");
                        lyType.AttackScale = Global.GetSafeAttributeDouble(xmlItem, "AttackScale");
                        lyType.DefenseScale = Global.GetSafeAttributeDouble(xmlItem, "DefenseScale");
                        lyType.MAttackScale = Global.GetSafeAttributeDouble(xmlItem, "MAttackScale");
                        lyType.MDefenseScale = Global.GetSafeAttributeDouble(xmlItem, "MDefenseScale");
                        lyType.HitScale = Global.GetSafeAttributeDouble(xmlItem, "HitScale");
                        LingYuTypeDict[lyType.Type] = lyType;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时异常{1}", LingYuTypeFile, ex));
                }
            }

            #endregion

            #region 加载LingYuLevelUpFile

            xml = null;
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(LingYuLevelUpFile));
            xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(LingYuLevelUpFile));
            if (xml == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", LingYuLevelUpFile));
            }
            else
            {
                try
                {
                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null == xmlItem)
                            continue;

                        // 先读取翎羽类型
                        int TypeID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "TypeID", "0"));
                        LingYuType lyType = null;
                        if (!LingYuTypeDict.TryGetValue(TypeID, out lyType))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("加载翎羽升级文件{0}时，未找到类型为{1}的翎羽配置", LingYuLevelUpFile, TypeID));
                            continue;
                        }

                        // 读取翎羽的每个级别的信息
                        IEnumerable<XElement> xmlItemLevels = xmlItem.Elements();
                        foreach (var xmlItemLevel in xmlItemLevels)
                        {
                            LingYuLevel lyLevel = new LingYuLevel();
                            lyLevel.Level = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "Level", "0"));
                            lyLevel.MinAttackV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MinAttackV", "0"));
                            lyLevel.MaxAttackV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MaxAttackV", "0"));
                            lyLevel.MinMAttackV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MinMAttackV", "0"));
                            lyLevel.MaxMAttackV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MaxMAttackV", "0"));
                            lyLevel.MinDefenseV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MinDefenseV", "0"));
                            lyLevel.MaxDefenseV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MaxDefenseV", "0"));
                            lyLevel.MinMDefenseV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MinMDefenseV", "0"));
                            lyLevel.MaxMDefenseV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MaxMDefenseV", "0"));
                            lyLevel.HitV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "HitV", "0"));
                            lyLevel.LifeV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "LifeV", "0"));
                            lyLevel.JinBiCost = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "JinBiCost", "0"));
                            string costGoods = Global.GetDefAttributeStr(xmlItemLevel, "GoodsCost", "0");
                            string[] costGoodsField = costGoods.Split(',');
                            if (costGoodsField.Length != 2)
                            {
                                LogManager.WriteLog(LogTypes.Error, string.Format("翎羽Type{0},级别{1}, 消耗物品配置错误", TypeID, lyLevel.Level));
                                continue;
                            }

                            lyLevel.GoodsCost = Convert.ToInt32(costGoodsField[0]);
                            lyLevel.GoodsCostCnt = Convert.ToInt32(costGoodsField[1]);

                            lyType.LevelDict[lyLevel.Level] = lyLevel;

                            LingYuLevelLimit = Global.GMax(LingYuLevelLimit, lyLevel.Level);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时异常{1}", LingYuLevelUpFile, ex));
                }
            }

            #endregion

            #region 加载LingYuSuitUpFile

            xml = null;
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(LingYuSuitUpFile));
            xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(LingYuSuitUpFile));
            if (xml == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", LingYuSuitUpFile));
            }
            else
            {
                try
                {
                    lock (LingYuTypeDict)
                    {
                        IEnumerable<XElement> xmlItems = xml.Elements();
                        foreach (var xmlItem in xmlItems)
                        {
                            if (null == xmlItem)
                                continue;

                            // 先读取翎羽类型
                            int TypeID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "TypeID", "0"));
                            LingYuType lyType = null;
                            if (!LingYuTypeDict.TryGetValue(TypeID, out lyType))
                            {
                                LogManager.WriteLog(LogTypes.Error, string.Format("加载翎羽进阶文件{0}时，未找到类型为{1}的翎羽配置", LingYuSuitUpFile, TypeID));
                                continue;
                            }

                            // 读取翎羽的每个级别的信息
                            IEnumerable<XElement> xmlItemLevels = xmlItem.Elements();
                            foreach (var xmlItemLevel in xmlItemLevels)
                            {
                                LingYuSuit lySuit = new LingYuSuit();
                                lySuit.Suit = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "SuitID", "0"));
                                lySuit.MinAttackV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MinAttackV", "0"));
                                lySuit.MaxAttackV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MaxAttackV", "0"));
                                lySuit.MinMAttackV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MinMAttackV", "0"));
                                lySuit.MaxMAttackV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MaxMAttackV", "0"));
                                lySuit.MinDefenseV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MinDefenseV", "0"));
                                lySuit.MaxDefenseV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MaxDefenseV", "0"));
                                lySuit.MinMDefenseV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MinMDefenseV", "0"));
                                lySuit.MaxMDefenseV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "MaxMDefenseV", "0"));
                                lySuit.HitV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "HitV", "0"));
                                lySuit.LifeV = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "LifeV", "0"));
                                lySuit.JinBiCost = Convert.ToInt32(Global.GetDefAttributeStr(xmlItemLevel, "JinBiCost", "0"));
                                string costGoods = Global.GetDefAttributeStr(xmlItemLevel, "GoodsCost", "0");
                                string[] costGoodsField = costGoods.Split(',');
                                if (costGoodsField.Length != 2)
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("翎羽Type{0},级别{1}, 消耗物品配置错误", TypeID, lySuit.Suit));
                                    continue;
                                }

                                lySuit.GoodsCost = Convert.ToInt32(costGoodsField[0]);
                                lySuit.GoodsCostCnt = Convert.ToInt32(costGoodsField[1]);

                                lyType.SuitDict[lySuit.Suit] = lySuit;

                                LingYuSuitLimit = Global.GMax(LingYuSuitLimit, lySuit.Suit);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时异常{1}", LingYuSuitUpFile, ex));
                }
            }

            #endregion

            #region 加载LingYuCollect

            xml = null;
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(LingYuCollectFile));
            xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(LingYuCollectFile));
            if (xml == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", LingYuCollectFile));
            }
            else
            {
                try
                {
                    LingYuCollectList.Clear();
                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null == xmlItem)
                            continue;

                        LingYuCollect lyCollect = new LingYuCollect();
                        lyCollect.Num = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Num", "0"));
                        lyCollect.NeedSuit = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedSuit", "0"));
                        lyCollect.Luck = Global.GetSafeAttributeDouble(xmlItem, "Luck");
                        lyCollect.DeLuck = Global.GetSafeAttributeDouble(xmlItem, "DeLuck");

                        LingYuCollectList.Add(lyCollect);
                    }

                    //排序 
                    LingYuCollectList.Sort((left, right) =>
                        {
                            if (left.NeedSuit > right.NeedSuit)
                                return 1;
                            else if (left.NeedSuit == right.NeedSuit)
                            {
                                if (left.Num > right.Num) return 1;
                                else if (left.Num == right.Num) return 0;
                                else return -1;
                            }
                            else
                                return -1;
                        });
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时异常{1}", LingYuCollectFile, ex));
                }
            }

            #endregion
        }

        #region 公共函数

        public static string Error2Str(LingYuError lyError)
        {
            if (lyError == LingYuError.Success) return Global.GetLang("成功");
            else if (lyError == LingYuError.NotOpen) return Global.GetLang("翅膀阶数或星级不满足翎羽开放条件");
            else if (lyError == LingYuError.LevelFull) return Global.GetLang("等级已满，无法提升");
            else if (lyError == LingYuError.NeedLevelUp) return Global.GetLang("必须先提升等级");
            else if (lyError == LingYuError.NeedSuitUp) return Global.GetLang("必须先提升品阶");
            else if (lyError == LingYuError.SuitFull) return Global.GetLang("品阶已满，无法提升");
            else if (lyError == LingYuError.LevelUpMaterialNotEnough) return Global.GetLang(" 提升等级所需材料不足");
            else if (lyError == LingYuError.LevelUpJinBiNotEnough) return Global.GetLang("提升等级所需金币不足");
            else if (lyError == LingYuError.SuitUpMaterialNotEnough) return Global.GetLang("提升品阶所需材料不足");
            else if (lyError == LingYuError.SuitUpJinBiNotEnough) return Global.GetLang("提升品阶所需金币不足");
            else if (lyError == LingYuError.ErrorConfig) return Global.GetLang("配置错误");
            else if (lyError == LingYuError.ErrorParams) return Global.GetLang("传来的参数错误");
            else if (lyError == LingYuError.ZuanShiNotEnough) return Global.GetLang("钻石不足");
            else if (lyError == LingYuError.DBSERVERERROR) return Global.GetLang("与dbserver通信失败");
            else return "unknown";
        }

        public static void UpdateLingYuProps(GameClient client)
        {
            if (null == client.ClientData.MyWingData) return;
            if (client.ClientData.MyWingData.WingID <= 0) return;
            //if (1 != client.ClientData.MyWingData.Using) return;

            double MinAttackV = 0;
            double MaxAttackV = 0;
            double MinMAttackV = 0;
            double MaxMAttackV = 0;
            double MinDefenseV = 0;
            double MaxDefenseV = 0;
            double MinMDefenseV = 0;
            double MaxMDefenseV = 0;
            double HitV = 0;
            double LifeV = 0;

            //Key: 阶数  Value: 个数
            //List<int> suitCnt = new List<int>(LingYuManager.LingYuSuitLimit + 1);
            int[] suitCnt = new int[LingYuManager.LingYuSuitLimit + 1];

            if (client.ClientData.MyWingData.Using == 1)
            {
                lock (client.ClientData.LingYuDict)
                {
                    foreach (KeyValuePair<int, LingYuData> kv in client.ClientData.LingYuDict)
                    {
                        int type = kv.Value.Type;
                        int level = kv.Value.Level;
                        int suit = kv.Value.Suit;

                        for (int i = 0; i <= suit; ++i)
                            suitCnt[i]++;

                        LingYuType lyType = null;
                        if (!LingYuTypeDict.TryGetValue(type, out lyType))
                            continue;
                        LingYuLevel lyLevel = null;
                        lyType.LevelDict.TryGetValue(level, out lyLevel);
                        LingYuSuit lySuit = null;
                        lyType.SuitDict.TryGetValue(suit, out lySuit);

                        if (lyLevel != null)
                        {
                            MinAttackV += lyLevel.MinAttackV;
                            MaxAttackV += lyLevel.MaxAttackV;
                            MinMAttackV += lyLevel.MinMAttackV;
                            MaxMAttackV += lyLevel.MaxMAttackV;
                            MinDefenseV += lyLevel.MinDefenseV;
                            MaxDefenseV += lyLevel.MaxDefenseV;
                            MinMDefenseV += lyLevel.MinMDefenseV;
                            MaxMDefenseV += lyLevel.MaxMDefenseV;
                            HitV += lyLevel.HitV;
                            LifeV += lyLevel.LifeV;
                        }

                        if (lySuit != null)
                        {
                            MinAttackV += lySuit.MinAttackV;
                            MaxAttackV += lySuit.MaxAttackV;
                            MinMAttackV += lySuit.MinMAttackV;
                            MaxMAttackV += lySuit.MaxMAttackV;
                            MinDefenseV += lySuit.MinDefenseV;
                            MaxDefenseV += lySuit.MaxDefenseV;
                            MinMDefenseV += lySuit.MinMDefenseV;
                            MaxMDefenseV += lySuit.MaxMDefenseV;
                            HitV += lySuit.HitV;
                            LifeV += lySuit.LifeV;
                        }
                    }
                }
            }

            // 翎羽属性加成
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.MinAttack, MinAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.MaxAttack, MaxAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.MinMAttack, MinMAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.MaxMAttack, MaxMAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.MinDefense, MinDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.MaxDefense, MaxDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.MinMDefense, MinMDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.MaxMDefense, MaxMDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.HitV, HitV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.MaxLifeV, LifeV);

            double lucky = 0.0;
            double deLucky = 0.0;
            // 翎羽阶数满足条件的加成
            if (client.ClientData.MyWingData.Using == 1)
            {
                for (int i = LingYuCollectList.Count() - 1; i >= 0; i--)
                {
                    LingYuCollect lyCollect = LingYuCollectList[i];
                    if (suitCnt[lyCollect.NeedSuit] >= lyCollect.Num)
                    {
                        lucky = lyCollect.Luck;
                        deLucky = lyCollect.DeLuck;
                        break;
                    }
                }
            }
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.Lucky, lucky);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.LingYuProps, (int)ExtPropIndexes.DeLucky, deLucky);
        }
            
        #endregion

        #region 客户端请求翎羽列表

        // 方便GM命令
        public static List<LingYuData> GetLingYuList(GameClient client)
        {
            List<LingYuData> dataList = new List<LingYuData>();
            Dictionary<int, LingYuType>.KeyCollection keys = LingYuManager.LingYuTypeDict.Keys;
            foreach (int type in keys)
            {
                LingYuData lyData = null;
                lock (client.ClientData.LingYuDict)
                {
                    if (!client.ClientData.LingYuDict.TryGetValue(type, out lyData))
                    {
                        lyData = new LingYuData();
                        lyData.Type = type;
                        lyData.Level = DEFAULT_LINGYU_LEVEL;
                        lyData.Suit = 0;
                    }
                }
                dataList.Add(lyData);
            }
            return dataList;
        }

        public static TCPProcessCmdResults ProcessGetLingYuList(
            TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                List<LingYuData> dataList = LingYuManager.GetLingYuList(client);
                byte[] bytesData = DataHelper.ObjectToBytes<List<LingYuData>>(dataList);
                GameManager.ClientMgr.SendToClient(client, bytesData, nID);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessGetLingYuList", false);
            }

            return TCPProcessCmdResults.RESULT_DATA;
        }

        #endregion

        #region 客户端请求提升翎羽等级

        public static LingYuError AdvanceLingYuLevel(GameClient client, int roleID, int type, int useZuanshiIfNoMaterial)
        {
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.WingLingYu))
                return LingYuError.NotOpen;

            LingYuType lyType = null;
            if (!LingYuTypeDict.TryGetValue(type, out lyType))
            {
                //找不到该翎羽
                return LingYuError.ErrorParams;
            }

            LingYuData lyData = null;
            lock (client.ClientData.LingYuDict)
            {
                if (!client.ClientData.LingYuDict.TryGetValue(type, out lyData))
                {
                    lyData = new LingYuData();
                    lyData.Type = type;
                    lyData.Level = DEFAULT_LINGYU_LEVEL;
                    lyData.Suit = 0;
                }
            }

            //已满级
            if (lyData.Level == LingYuLevelLimit)
            {
                return LingYuError.LevelFull;
            }

            //需要提升品阶
            if (lyData.Level > 0 && lyData.Level % 10 == 0 && lyData.Level / 10 != lyData.Suit)
            {
                return LingYuError.NeedSuitUp;
            }

            LingYuLevel nextLevel = null;
            if (!lyType.LevelDict.TryGetValue(lyData.Level + 1, out nextLevel)) //找不到下一级配置
            {
                return LingYuError.ErrorConfig;
            }

            if (Global.GetTotalBindTongQianAndTongQianVal(client) < nextLevel.JinBiCost)
                return LingYuError.LevelUpJinBiNotEnough;

            int haveGoodsCnt = Global.GetTotalGoodsCountByID(client, nextLevel.GoodsCost);
            if (haveGoodsCnt < nextLevel.GoodsCostCnt && useZuanshiIfNoMaterial == 0)
                return LingYuError.LevelUpMaterialNotEnough;

            //  本来的处理方式是，假设要消耗10个材料，现在玩家只有5个材料，并且勾选了使用钻石
            //  那么将会消耗玩家的这5个材料，不足的5个材料使用钻石替补
            /*
            int goodsCostCnt = nextLevel.GoodsCostCnt;
            int zuanshiCost = 0;
            if (haveGoodsCnt < nextLevel.GoodsCostCnt)
            {
                goodsCostCnt = haveGoodsCnt;
                int goodsPrice = 0;
                if (!Data.LingYuMaterialZuanshiDict.TryGetValue(nextLevel.GoodsCost, out goodsPrice))
                    return LingYuError.ErrorConfig;
                zuanshiCost = (nextLevel.GoodsCostCnt - haveGoodsCnt) * goodsPrice;
                if (client.ClientData.UserMoney < zuanshiCost)
                    return LingYuError.ZuanShiNotEnough;
            }
            */

            //  现在的处理方式是，假设要消耗10个材料，现在玩家只有5个材料，并且勾选了使用钻石
            //  那么直接扣除10个材料的钻石价格，玩家的5个材料不消耗
            int goodsCostCnt = nextLevel.GoodsCostCnt;
            int zuanshiCost = 0;
            if (haveGoodsCnt < nextLevel.GoodsCostCnt)
            {
                goodsCostCnt = 0;
                int goodsPrice = 0;
                if (!Data.LingYuMaterialZuanshiDict.TryGetValue(nextLevel.GoodsCost, out goodsPrice))
                    return LingYuError.ErrorConfig;
                zuanshiCost = nextLevel.GoodsCostCnt * goodsPrice;
                if (client.ClientData.UserMoney < zuanshiCost)
                    return LingYuError.ZuanShiNotEnough;
            }

            // 先扣钱
            if (!Global.SubBindTongQianAndTongQian(client, nextLevel.JinBiCost, "翎羽升级消耗"))
                return LingYuError.DBSERVERERROR;

            //有可能出现消耗一部分材料，其余用钻石购买的情况
            if (goodsCostCnt > 0)
            {
                bool bUsedBinding = false;
                bool bUsedTimeLimited = false;

                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nextLevel.GoodsCost, goodsCostCnt, false, out bUsedBinding, out bUsedTimeLimited))
                    return LingYuError.DBSERVERERROR;
            }

            if (zuanshiCost > 0)
            {
                //先DBServer请求扣费
                //扣除用户点卷
                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, zuanshiCost, "翎羽升级"))
                    return LingYuError.DBSERVERERROR;
            }

            //生效，计算属性加成，写数据库
            int iRet = UpdateLingYu2DB(roleID, type, lyData.Level + 1, lyData.Suit, client.ServerId);
            if (iRet < 0)
            {
                return LingYuError.DBSERVERERROR;
            }

            lyData.Level++;
            lock (client.ClientData.LingYuDict)
            {
                client.ClientData.LingYuDict[type] = lyData;
            }

            UpdateLingYuProps(client);
            // 通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            return LingYuError.Success;
        }

        public static TCPProcessCmdResults ProcessAdvanceLingYuLevel(
            TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                //角色id:翎羽Type:材料不足时消耗钻石
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int type = Convert.ToInt32(fields[1]);
                int useZuanshiIfNoMaterial = Convert.ToInt32(fields[2]);

                LingYuError lyError = LingYuManager.AdvanceLingYuLevel(client, roleID, type, useZuanshiIfNoMaterial);
                LingYuData lyData = null;
                lock (client.ClientData.LingYuDict)
                {
                    if (!client.ClientData.LingYuDict.TryGetValue(type, out lyData))
                    {
                        lyData = new LingYuData();
                        lyData.Type = type;
                        lyData.Level = DEFAULT_LINGYU_LEVEL;
                        lyData.Suit = 0;
                    }
                }

                string strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, (int)lyError, lyData.Type, lyData.Level);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessAdvanceLingYuLevel", false);
            }

            return TCPProcessCmdResults.RESULT_DATA;
        }

        #endregion

        #region 客户端请求提升翎羽品阶

        public static LingYuError AdvanceLingYuSuit(GameClient client, int roleID, int type, int useZuanshiIfNoMaterial)
        {
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.WingLingYu))
                return LingYuError.NotOpen;

            LingYuType lyType = null;
            if (!LingYuTypeDict.TryGetValue(type, out lyType))
            {
                //找不到该翎羽
                return LingYuError.ErrorParams;
            }

            LingYuData lyData = null;
            lock (client.ClientData.LingYuDict)
            {
                if (!client.ClientData.LingYuDict.TryGetValue(type, out lyData))
                {
                    lyData = new LingYuData();
                    lyData.Type = type;
                    lyData.Level = DEFAULT_LINGYU_LEVEL;
                    lyData.Suit = 0;
                }
            }

            //已满阶
            if (lyData.Suit == LingYuSuitLimit)
            {
                return LingYuError.SuitFull;
            }

            //需要提升等级
            if (lyData.Level == 0 || lyData.Level / 10 == lyData.Suit)
            {
                return LingYuError.NeedLevelUp;
            }

            LingYuSuit nextSuit = null;
            if (!lyType.SuitDict.TryGetValue(lyData.Suit + 1, out nextSuit)) //找不到下一阶配置
            {
                return LingYuError.ErrorConfig;
            }

            if (Global.GetTotalBindTongQianAndTongQianVal(client) < nextSuit.JinBiCost)
                return LingYuError.SuitUpJinBiNotEnough;

            int haveGoodsCnt = Global.GetTotalGoodsCountByID(client, nextSuit.GoodsCost);
            if (haveGoodsCnt < nextSuit.GoodsCostCnt && useZuanshiIfNoMaterial == 0)
                return LingYuError.SuitUpMaterialNotEnough;

            //  本来的处理方式是，假设要消耗10个材料，现在玩家只有5个材料，并且勾选了使用钻石
            //  那么将会消耗玩家的这5个材料，不足的5个材料使用钻石替补
            /*
            int goodsCostCnt = nextSuit.GoodsCostCnt;
            int zuanshiCost = 0;
            if (haveGoodsCnt < nextSuit.GoodsCostCnt)
            {
                goodsCostCnt = haveGoodsCnt;
                int goodsPrice = 0;
                if (!Data.LingYuMaterialZuanshiDict.TryGetValue(nextSuit.GoodsCost, out goodsPrice))
                    return LingYuError.ErrorConfig;
                zuanshiCost = (nextSuit.GoodsCostCnt - haveGoodsCnt) * goodsPrice;
                if (client.ClientData.UserMoney < zuanshiCost)
                    return LingYuError.ZuanShiNotEnough;
            }
            */

            //  现在的处理方式是，假设要消耗10个材料，现在玩家只有5个材料，并且勾选了使用钻石
            //  那么直接扣除10个材料的钻石价格，玩家的5个材料不消耗
            int goodsCostCnt = nextSuit.GoodsCostCnt;
            int zuanshiCost = 0;
            if (haveGoodsCnt < nextSuit.GoodsCostCnt)
            {
                goodsCostCnt = 0;
                int goodsPrice = 0;
                if (!Data.LingYuMaterialZuanshiDict.TryGetValue(nextSuit.GoodsCost, out goodsPrice))
                    return LingYuError.ErrorConfig;
                zuanshiCost = nextSuit.GoodsCostCnt * goodsPrice;
                if (client.ClientData.UserMoney < zuanshiCost)
                    return LingYuError.ZuanShiNotEnough;
            }

            //先扣钱
            if (!Global.SubBindTongQianAndTongQian(client, nextSuit.JinBiCost, "翎羽升阶消耗"))
            {
                return LingYuError.DBSERVERERROR;
            }

            //有可能出现消耗一部分材料，其余用钻石购买的情况
            if (goodsCostCnt > 0)
            {
                bool bUsedBinding = false;
                bool bUsedTimeLimited = false;

                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, nextSuit.GoodsCost, goodsCostCnt, false, out bUsedBinding, out bUsedTimeLimited))
                    return LingYuError.DBSERVERERROR;
            }

            if (zuanshiCost > 0)
            {
                //先DBServer请求扣费  扣除用户点卷
                if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, zuanshiCost, "翎羽升级"))
                    return LingYuError.DBSERVERERROR;
            }

            //生效，计算属性加成，写数据库
            int iRet = UpdateLingYu2DB(roleID, type, lyData.Level, lyData.Suit + 1, client.ServerId);
            if (iRet < 0)
            {
                return LingYuError.DBSERVERERROR;
            }

            lyData.Suit++;
            lock (client.ClientData.LingYuDict)
            {
                client.ClientData.LingYuDict[type] = lyData;
            }

            if (LingYuManager.SuitOfNotifyList.Contains(lyData.Suit))
            {
                // 【{0}】将【{1}】提升到{2}阶，翅膀的力量得到了提升。
                string broadcastMsg = StringUtil.substitute(Global.GetLang("【{0}】将【{1}】提升到{2}阶，翅膀的力量得到了提升。"),
                                                            Global.FormatRoleName(client, client.ClientData.RoleName), lyType.Name, lyData.Suit);
                //播放用户行为消息
                Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.HintMsg, broadcastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
            }

            UpdateLingYuProps(client);
            // 通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            return LingYuError.Success;
        }

        public static TCPProcessCmdResults ProcessAdvanceLingYuSuit(
            TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                //角色id:翎羽Type:材料不足时消耗钻石
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int type = Convert.ToInt32(fields[1]);
                int useZuanshiIfNoMaterial = Convert.ToInt32(fields[2]);

                LingYuError lyError = LingYuManager.AdvanceLingYuSuit(client, roleID, type, useZuanshiIfNoMaterial);
                LingYuData lyData = null;
                lock (client.ClientData.LingYuDict)
                {
                    if (!client.ClientData.LingYuDict.TryGetValue(type, out lyData))
                    {
                        lyData = new LingYuData();
                        lyData.Type = type;
                        lyData.Level = DEFAULT_LINGYU_LEVEL;
                        lyData.Suit = 0;
                    }
                }

                string strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, (int)lyError, lyData.Type, lyData.Suit);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessAdvanceLingYuSuit", false);
            }

            return TCPProcessCmdResults.RESULT_DATA;
        }
        #endregion

        #region 数据库操作

        private static int UpdateLingYu2DB(int roleID, int type, int level, int suit, int serverId)
        {
            string strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, type, level, suit);
            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_LINGYU, strcmd, serverId);
            if (fields == null || fields.Length != 2)
                return -1;
            return Convert.ToInt32(fields[1]);
        }

        #endregion

        /// <summary>
        /// 翎羽功能开启时，执行初始化，因为默认是1级0阶
        /// 1级时已经有属性加成
        /// </summary>
        /// <param name="client"></param>
        public static void InitAsOpened(GameClient client)
        {
            Dictionary<int, LingYuType>.KeyCollection keys = LingYuManager.LingYuTypeDict.Keys;
            foreach (int type in keys)
            {
                lock (client.ClientData.LingYuDict)
                {
                    if (!client.ClientData.LingYuDict.ContainsKey(type))
                    {
                        LingYuData data = new LingYuData()
                        {
                            Type = type,
                            Level = DEFAULT_LINGYU_LEVEL,
                            Suit = 0
                        };
                        UpdateLingYu2DB(client.ClientData.RoleID, type, 1, 0, client.ServerId);
                        client.ClientData.LingYuDict[type] = data;
                    }
                }
            }

            UpdateLingYuProps(client);
        }
    }
}
