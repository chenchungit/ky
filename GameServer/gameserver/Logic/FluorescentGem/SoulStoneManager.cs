using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Server.Tools.Pattern;
using System.Diagnostics;
using Server.Tools;
using GameServer.Server;
using Server.Data;
using GameServer.Logic.ActivityNew;

namespace GameServer.Logic.FluorescentGem
{
    public class SoulStoneManager : SingletonTemplate<SoulStoneManager>, IManager, ICmdProcessorEx
    {
        private SoulStoneManager()
        {
            // 魂石精华，只能被吃
            JingHuaCategorys.Add((int)ItemCategories.SoulStoneJingHua);

            // 魂石装备，技能被吃，也能穿戴
            EquipCategorys.Add((int)ItemCategories.SoulStoneFire);//火魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneThunder);//雷魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneWind);//风魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneWater);//水魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneIce);//冰魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneSoil);//土魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneLight);//光魂石
            EquipCategorys.Add((int)ItemCategories.SoulStonePower);//电魂石
            EquipCategorys.Add((int)ItemCategories.SoulStonePole);//极魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneCold);//冷魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneFrost);//霜魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneHot);//热魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneExplode);//爆魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneCloud);//云魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneRomantic);//漫魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneSnow);//雪魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneSeal);//封魂石
            EquipCategorys.Add((int)ItemCategories.SoulStoneRed);//赤魂石
        }

        private int defaultRandId;
        private Dictionary<int, SoulStoneRandConfig> randDict = new Dictionary<int, SoulStoneRandConfig>();
        private Dictionary<int, SoulStoneExpConfig> suitExpDict = new Dictionary<int, SoulStoneExpConfig>();
        private Dictionary<int, int> stone2TypeDict = new Dictionary<int, int>();
        private Dictionary<int, SoulStoneGroupConfig> groupDict = new Dictionary<int, SoulStoneGroupConfig>();
        private HashSet<int> EquipCategorys = new HashSet<int>();
        private HashSet<int> JingHuaCategorys = new HashSet<int>();

        // Category = 910的魂石精华提供的经验单独配置 key:goodsId, value:exp
        // 这个在SystemParams中配置，需要考虑reload的情况
        private Dictionary<int, int> jinghuaExpDict = null;
        private Dictionary<int, int> equipLvlLimitDict = null;

        private bool bOpenStoneGetLog = false; // 魂石产生log，方便调试

        #region IManager
        public bool initialize()
        {
            if (!LoadRandType()
                || !LoadRandInfo()
                || !LoadExp()
                || !LoadStoneType()
                || !LoadStoneGroup()
                )
            {
                LogManager.WriteLog(LogTypes.Error, "SoulStoneManager.initialize failed!");
                return false;
            }

            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_GET, 3, 3, SoulStoneManager.Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_LVL_UP, 4, 4, SoulStoneManager.Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_MOD_EQUIP, 3, 3, SoulStoneManager.Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_RESET_BAG, 1, 1, SoulStoneManager.Instance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_QUERY_GET, 1, 1, SoulStoneManager.Instance());

            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            return true;
        }
        #endregion

        #region ICmdProcessorEx
        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            // 如果1.9的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot9))
                return true;

            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_GET:
                    return ProcessSoulStoneGet(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_LVL_UP:
                    return ProcessSoulStoneLevelUp(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_MOD_EQUIP:
                    return ProcessSoulStoneModEquip(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_RESET_BAG:
                    return ProcessSoulStoneResetBag(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_SOUL_STONE_QUERY_GET:
                    return ProcessSoulStoneQueryGet(client, nID, bytes, cmdParams);
            }

            return true;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return true;
        }
        #endregion

        #region load config

        /// <summary>
        /// 加载魂石精华的经验配置配置文件
        /// </summary>
        public void LoadJingHuaExpConfig()
        {
            Dictionary<int, int> tmpJingHuaExp = new Dictionary<int, int>();
            Dictionary<int, int> tmpLvlLimit = new Dictionary<int, int>();

            // 魂石精华的经验
            List<string> expList = GameManager.systemParamsList.GetParamValueStringListByName("HunShiExp", '|');     
            if (expList != null)
            {
                foreach (var s in expList)
                {
                    string[] fields = s.Split(',');
                    if (fields.Length == 2)
                    {
                        tmpJingHuaExp[Convert.ToInt32(fields[0])] = Convert.ToInt32(fields[1]);
                    }
                }
            }

            // 穿戴的等级限制
            List<string> lvlList = GameManager.systemParamsList.GetParamValueStringListByName("HunShiOpen", '|');
            if (lvlList != null)
            {
                for (int i = 0; i < lvlList.Count; i++)
                {
                    string[] fields = lvlList[i].Split(',');
                    if (fields.Length == 2)
                    {
                        int cl = Convert.ToInt32(fields[0]);
                        int lvl = Convert.ToInt32(fields[1]);
                        tmpLvlLimit[i + 1] = Global.GetUnionLevel(cl, lvl);
                    }
                }
            }


            jinghuaExpDict = tmpJingHuaExp;
            equipLvlLimitDict = tmpLvlLimit;
        }

        /// <summary>
        /// 加载随机类型
        /// </summary>
        /// <returns></returns>
        private bool LoadRandType()
        {
            try
            {
                defaultRandId = int.MaxValue;

                XElement randTypeXml = XElement.Load(Global.GameResPath(SoulStoneConsts.RandTypeCfgFile));
                foreach (var typeXml in randTypeXml.Elements())
                {
                    long[] tmpLongArr = null;
                    string[] tmpStringArr = null;

                    SoulStoneRandConfig randCfg = new SoulStoneRandConfig();
                    randCfg.RandId = (int)Global.GetSafeAttributeLong(typeXml, "ID");
                    randCfg.NeedLangHunFenMo = (int)Global.GetSafeAttributeLong(typeXml, "NeedLangHunFenMo");
                    randCfg.SuccessRate = Global.GetSafeAttributeDouble(typeXml, "SuccessRate");

                    tmpLongArr = Global.GetSafeAttributeLongArray(typeXml, "SuccessTo");
                    Debug.Assert(tmpLongArr != null);
                    for (int i = 0; i < tmpLongArr.Length; i++)
                    {
                        randCfg.SuccessTo.Add((int)tmpLongArr[i]);
                    }

                    tmpLongArr = Global.GetSafeAttributeLongArray(typeXml, "FailTo");
                    Debug.Assert(tmpLongArr != null);
                    for (int i = 0; i < tmpLongArr.Length; i++)
                    {
                        randCfg.FailTo.Add((int)tmpLongArr[i]);
                    }

                    tmpStringArr = Global.GetSafeAttributeStr(typeXml, "AddedGoodsNeed").Split('|');
                    Debug.Assert(tmpStringArr != null && tmpStringArr.Length == 5);
                    for (int i = 0; i < 5; ++i)
                    {
                        randCfg.AddedNeedDict[(ESoulStoneExtCostType)(i + 1)] = Convert.ToInt32(tmpStringArr[i]);
                    }
                    randCfg.AddedRate = Global.GetSafeAttributeDouble(typeXml, "AddedGoodsOdds");
                    randCfg.AddedGoods = Global.ParseGoodsFromStr_7(Global.GetSafeAttributeStr(typeXml, "AddedGoods").Split(','), 0);

                    tmpStringArr = Global.GetSafeAttributeStr(typeXml, "ReduceNeed").Split('|');
                    Debug.Assert(tmpStringArr != null && tmpStringArr.Length == 5);
                    for (int i = 0; i < 5; ++i)
                    {
                        randCfg.ReduceNeedDict[(ESoulStoneExtCostType)(i + 1)] = Convert.ToInt32(tmpStringArr[i]);
                    }
                    randCfg.ReduceRate = Global.GetSafeAttributeDouble(typeXml, "ReduceOdds");
                    randCfg.ReduceValue = (int)Global.GetSafeAttributeLong(typeXml, "ReduceNum");

                    tmpStringArr = Global.GetSafeAttributeStr(typeXml, "AdvanceSuccessNeed").Split('|');
                    Debug.Assert(tmpStringArr != null && tmpStringArr.Length == 5);
                    for (int i = 0; i < 5; ++i)
                    {
                        randCfg.UpSucRateNeedDict[(ESoulStoneExtCostType)(i + 1)] = Convert.ToInt32(tmpStringArr[i]);
                    }
                    randCfg.UpSucRateTo = Global.GetSafeAttributeDouble(typeXml, "AdvanceSuccessRate");

                    tmpStringArr = Global.GetSafeAttributeStr(typeXml, "HoldTypeNeed").Split('|');
                    Debug.Assert(tmpStringArr != null && tmpStringArr.Length == 5);
                    for (int i = 0; i < 5; ++i)
                    {
                        randCfg.FailHoldNeedDict[(ESoulStoneExtCostType)(i + 1)] = Convert.ToInt32(tmpStringArr[i]);
                    }

                    tmpLongArr = Global.GetSafeAttributeLongArray(typeXml, "HoldTypeFailTo");
                    Debug.Assert(tmpLongArr != null);
                    for (int i = 0; i < tmpLongArr.Length; i++)
                    {
                        randCfg.FailToIfHold.Add((int)tmpLongArr[i]);
                    }

                    randDict.Add(randCfg.RandId, randCfg);
                    defaultRandId = Math.Min(defaultRandId, randCfg.RandId);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, "load config file " + SoulStoneConsts.RandTypeCfgFile + " failed", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 加载具体的随机信息
        /// </summary>
        /// <returns></returns>
        private bool LoadRandInfo()
        {
            try
            {
                XElement randInfoXml = XElement.Load(Global.GameResPath(SoulStoneConsts.RandInfoCfgFile));
                foreach (var randXml in randInfoXml.Elements())
                {
                    SoulStoneRandConfig randCfg = null;
                    int randId = (int)Global.GetSafeAttributeLong(randXml, "TypeID");
                    if (!randDict.TryGetValue(randId, out randCfg))
                    {
                        throw new Exception("can't find typeid=" + randId + ", please check " + SoulStoneConsts.RandTypeCfgFile);
                    }

                    randCfg.RandMinNumber = int.MaxValue;
                    randCfg.RandMaxNumber = int.MinValue;

                    foreach (var xml in randXml.Elements())
                    {
                        SoulStoneRandInfo info = new SoulStoneRandInfo();
                        info.Id = (int)Global.GetSafeAttributeLong(xml, "ID");
                        info.Goods = Global.ParseGoodsFromStr_7(Global.GetSafeAttributeStr(xml, "Goods").Split(','), 0);
                        info.RandBegin = (int)Global.GetSafeAttributeLong(xml, "BeginNum");
                        info.RandEnd = (int)Global.GetSafeAttributeLong(xml, "EndNum");

                        randCfg.RandMinNumber = Math.Min(randCfg.RandMinNumber, info.RandBegin);
                        randCfg.RandMaxNumber = Math.Max(randCfg.RandMaxNumber, info.RandEnd);

                        randCfg.RandStoneList.Add(info);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, "load config file " + SoulStoneConsts.RandTypeCfgFile + " failed", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 加载魂石经验信息
        /// </summary>
        /// <returns></returns>
        private bool LoadExp()
        {
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(SoulStoneConsts.ExpCfgFile));
                foreach (var suitXml in xml.Elements())
                {
                    SoulStoneExpConfig expCfg = new SoulStoneExpConfig();
                    expCfg.Suit = (int)Global.GetSafeAttributeLong(suitXml, "SuitID");
                    expCfg.MinLevel = int.MaxValue;
                    expCfg.MaxLevel = int.MinValue;

                    Dictionary<int, int> tmpLvlExp = new Dictionary<int, int>();
                    foreach (var lvlXml in suitXml.Elements())
                    {
                        int lvl = (int)Global.GetSafeAttributeLong(lvlXml, "ID");
                        int exp = (int)Global.GetSafeAttributeLong(lvlXml, "Exp");

                        expCfg.MinLevel = Math.Min(expCfg.MinLevel, lvl);
                        expCfg.MaxLevel = Math.Max(expCfg.MaxLevel, lvl);
                        tmpLvlExp.Add(lvl, exp);
                    }

                    int prevTotalExp = 0;
                    int curLvlExp = 0;
                    for (int lvl = expCfg.MinLevel; lvl <= expCfg.MaxLevel; ++lvl)
                    {
                        if (!tmpLvlExp.TryGetValue(lvl, out curLvlExp))
                            curLvlExp = 0;

                        expCfg.Lvl2Exp.Add(lvl, curLvlExp + prevTotalExp);
                        prevTotalExp += curLvlExp;
                    }

                    suitExpDict.Add(expCfg.Suit, expCfg);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, "load config file " + SoulStoneConsts.ExpCfgFile + " failed", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 加载魂石类型
        /// </summary>
        /// <returns></returns>
        private bool LoadStoneType()
        {
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(SoulStoneConsts.GoodsTypeCfgFile));
                foreach (var typeXml in xml.Elements())
                {
                    int type = (int)Global.GetSafeAttributeLong(typeXml, "ID");
                    long[] stones = Global.GetSafeAttributeLongArray(typeXml, "Goods");

                    for (int i = 0; i < stones.Length; ++i)
                    {
                        stone2TypeDict.Add((int)stones[i], type);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, "load config file " + SoulStoneConsts.GoodsTypeCfgFile + " failed", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 加载魂石组加成
        /// </summary>
        /// <returns></returns>
        private bool LoadStoneGroup()
        {
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(SoulStoneConsts.GroupCfgFile));
                foreach (var groupXml in xml.Elements())
                {
                    SoulStoneGroupConfig groupCfg = new SoulStoneGroupConfig();

                    groupCfg.Group = (int)Global.GetSafeAttributeLong(groupXml, "ID");
                    long[] stones = Global.GetSafeAttributeLongArray(groupXml, "HunShiGoodsType");
                    for (int i = 0; i < stones.Length; ++i)
                    {
                        groupCfg.NeedTypeList.Add((int)stones[i]);
                    }

                    string[] strProps = Global.GetSafeAttributeStr(groupXml, "GroupProperty").Split('|');
                    for (int i = 0; i < strProps.Length; ++i)
                    {
                        string[] oneProp = strProps[i].Split(',');
                        ExtPropIndexes propIndex;
                        if (Enum.TryParse<ExtPropIndexes>(oneProp[0], out propIndex))
                        {
                            double val = Convert.ToDouble(oneProp[1]);
                            groupCfg.AttrValue.Add((int)propIndex, val);
                        }
                        else
                        {
                            LogManager.WriteLog(LogTypes.Error, "can't parse " + groupCfg.Group.ToString() + " " + oneProp[0] + " as ExtPropIndexes");
                        }
                    }

                    groupDict.Add(groupCfg.Group, groupCfg);

                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, "load config file " + SoulStoneConsts.GroupCfgFile + " failed", ex);
                return false;
            }

            return true;
        }
        #endregion

        #region util function
        /// <summary>
        /// 获取额外功能列表
        /// </summary>
        /// <returns></returns>
        private List<SoulStoneExtFuncItem> GetExtFuncItems()
        {
            JieRiFuLiActivity act = HuodongCachingMgr.GetJieriFuLiActivity();
            if (act == null) return null;

            object arg = null;
            if (!act.IsOpened(EJieRiFuLiType.SoulStoneExtFunc, out arg))
                return null;

            if (arg == null) return null;

            List<Tuple<int, int>> tList = arg as List<Tuple<int, int>>;
            if (tList == null || tList.Count <= 0)
                return null;

            List<SoulStoneExtFuncItem> resultList = new List<SoulStoneExtFuncItem>();
            for (int i = 0; i < tList.Count; ++i)
            {
                resultList.Add(new SoulStoneExtFuncItem() { FuncType = tList[i].Item1, CostType = tList[i].Item2 });
            }
            return resultList;
        }

        /// <summary>
        /// 功能开启
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private bool IsGongNengOpened(GameClient client)
        {
            if (client == null) return false;

            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.SoulStone))
                return false;

            return true;
        }

        /// <summary>
        /// 魂石装备栏有多个环，每个环有多个位置，组合成一个BagIndex
        /// </summary>
        /// <param name="cycle"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        private int GenerateBagIndex(int cycle, int grid)
        {
            return cycle * 100 + grid;
        }

        /// <summary>
        /// 装备栏的bagindex  ---> cycle & grid
        /// </summary>
        /// <param name="bagIndex"></param>
        /// <param name="cycle"></param>
        /// <param name="grid"></param>
        private void ParseCycleAndGrid(int bagIndex, out int cycle, out int grid)
        {
            cycle = bagIndex / 100;
            grid = bagIndex % 100;
        }

        /// <summary>
        /// 检测初始化随机组
        /// </summary>
        /// <param name="gameClient"></param>
        public void CheckOpen(GameClient client)
        {
            if (client == null)
                return;

            if (client.ClientData.IsSoulStoneOpened)
                return;

            if (!IsGongNengOpened(client))
                return;

            client.ClientData.IsSoulStoneOpened = true;

            // 首次开启，激活默认随机组
            string szRandId = Global.GetRoleParamByName(client, RoleParamName.SoulStoneRandId);
            int iRand = 0;
            if (string.IsNullOrEmpty(szRandId) || !int.TryParse(szRandId, out iRand) || !randDict.ContainsKey(iRand))
            {
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.SoulStoneRandId, this.defaultRandId, true);
            }

            // 背包格子不存数据库，上线时初始化
            ResetSoulStoneBag(client);

            UpdateProps(client);
        }

        /// <summary>
        /// 是否可以向魂石背包添加num个物品
        /// </summary>
        /// <param name="client"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        private bool CanAddGoodsNum(GameClient client, int num)
        {
            if (client == null || num <= 0)
                return false;

            if (num + client.ClientData.SoulStoneInBag.Count > SoulStoneConsts.MaxBagNum)
                return false;

            return true;
        }

        /// <summary>
        /// 找到空闲背包的位置
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetIdleSlotOfBag(GameClient client)
        {
            byte[] flagArray = new byte[SoulStoneConsts.MaxBagNum];

            for (int i = 0; i < client.ClientData.SoulStoneInBag.Count; i++)
            {
                int bagIndex = client.ClientData.SoulStoneInBag[i].BagIndex;
                if (bagIndex >= 0 && bagIndex < SoulStoneConsts.MaxBagNum)
                {
                    flagArray[bagIndex] = 1;
                }
            }

            for (int i = 0; i < SoulStoneConsts.MaxBagNum; i++)
            {
                if (flagArray[i] == 0)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 添加魂石
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gd"></param>
        /// <param name="site"></param>
        private void AddSoulStoneGoods(GameClient client, GoodsData gd, int site)
        {
            if (client == null || gd == null) return;

            gd.Site = site;
            if (site == (int)SaleGoodsConsts.SoulStoneBag)
                client.ClientData.SoulStoneInBag.Add(gd);
            else if (site == (int)SaleGoodsConsts.SoulStoneEquip)
                client.ClientData.SoulStoneInUsing.Add(gd);
        }

        public GoodsData AddSoulStoneGoods(GameClient client, int id, int goodsID, int forgeLevel, int quality, int goodsNum, int binding, int site, string jewelList, string startTime, string endTime,
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

            AddSoulStoneGoods(client, gd, gd.Site);
            return gd;
        }

        // 删除魂石
        public void RemoveSoulStoneGoods(GameClient client, GoodsData goodsData, int site)
        {
            if (goodsData != null && client != null)
            {
                if (goodsData.Site == (int)SaleGoodsConsts.SoulStoneBag)
                {
                    client.ClientData.SoulStoneInBag.Remove(goodsData);
                }
                else if (goodsData.Site == (int)SaleGoodsConsts.SoulStoneEquip)
                {
                    client.ClientData.SoulStoneInUsing.Remove(goodsData);
                }
            }
        }

        private GoodsData GetSoulStoneByDbId(GameClient client, int site, int dbid)
        {
            if (site == (int)SaleGoodsConsts.SoulStoneBag)
                return client.ClientData.SoulStoneInBag.Find(_g => _g.Id == dbid);
            else if (site == (int)SaleGoodsConsts.SoulStoneEquip)
                return client.ClientData.SoulStoneInUsing.Find(_g => _g.Id == dbid);

            return null;
        }

        private void UpdateProps(GameClient client)
        {
            if (client == null)
                return;

            // 汇总属性
            EquipPropItem totalProp = new EquipPropItem();

            // 统计一下每一个装备环的魂石
            List<int>[] eachCycleStones = new List<int>[SoulStoneConsts.EquipCycleNum + 1];
            for (int i = 1; i <= SoulStoneConsts.EquipCycleNum; ++i)
                eachCycleStones[i] = new List<int>();

            foreach (var gd in client.ClientData.SoulStoneInUsing)
            {
                int cycle = 0, grid = 0;
                ParseCycleAndGrid(gd.BagIndex, out cycle, out grid);

                if (cycle >= 1 && cycle <= SoulStoneConsts.EquipCycleNum && stone2TypeDict.ContainsKey(gd.GoodsID))
                {
                    eachCycleStones[cycle].Add(stone2TypeDict[gd.GoodsID]);
                }

                int lvl = gd.ElementhrtsProps != null ? gd.ElementhrtsProps[0] : SoulStoneConsts.DefaultLevel;

                // 每个魂石的基础属性
                EquipPropItem baseProp = GameManager.EquipPropsMgr.FindEquipPropItem(gd.GoodsID);
                for (int i = 0; baseProp != null && i < baseProp.ExtProps.Length; ++i)
                {
                    totalProp.ExtProps[i] += baseProp.ExtProps[i] * lvl;
                }
            }

            // 计算魂石组加成
            foreach (var kvp in groupDict)
            {
                var group = kvp.Value;
                if (group.NeedTypeList == null || group.NeedTypeList.Count <= 0)
                    continue;

                // 依次检测每个魂石组合，有几个装备环满足了
                for (int cycle = 1; cycle <= SoulStoneConsts.EquipCycleNum; cycle++)
                {
                    if (group.NeedTypeList.All(_t => eachCycleStones[cycle].Contains(_t)))
                    {
                        foreach (var attrKvp in group.AttrValue)
                        {
                            if (attrKvp.Key >= 0 && attrKvp.Key < totalProp.ExtProps.Length)
                            {
                                totalProp.ExtProps[attrKvp.Key] += attrKvp.Value;
                            }
                        }
                    }
                }
            }


            client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.SoulStone, totalProp.ExtProps);
        }

        private int ExtCostTypeHadValue(GameClient client, ESoulStoneExtCostType costType)
        {
            int val = 0;

            if (costType == ESoulStoneExtCostType.MoJing)
                val = GameManager.ClientMgr.GetTianDiJingYuanValue(client);
            else if (costType == ESoulStoneExtCostType.XingHun)
                val = client.ClientData.StarSoul;
            else if (costType == ESoulStoneExtCostType.ChengJiu)
                val = GameManager.ClientMgr.GetChengJiuPointsValue(client);
            else if (costType == ESoulStoneExtCostType.ShengWang)
                val = GameManager.ClientMgr.GetShengWangValue(client);
            else if (costType == ESoulStoneExtCostType.ZuanShi)
                val = client.ClientData.UserMoney;

            return val;
        }

        private bool DoExtCostType(GameClient client, ESoulStoneExtCostType costType, int val)
        {
            if (val <= 0) return true;

            if (costType == ESoulStoneExtCostType.MoJing)
                GameManager.ClientMgr.ModifyTianDiJingYuanValue(client, -val, "聚魂额外消耗", true, true);
            else if (costType == ESoulStoneExtCostType.XingHun)
                GameManager.ClientMgr.ModifyStarSoulValue(client, -val, "聚魂额外消耗", true, true);
            else if (costType == ESoulStoneExtCostType.ChengJiu)
                GameManager.ClientMgr.ModifyChengJiuPointsValue(client, -val, "聚魂额外消耗", true, true);
            else if (costType == ESoulStoneExtCostType.ShengWang)
                GameManager.ClientMgr.ModifyShengWangValue(client, -val, "聚魂额外消耗", true, true);
            else if (costType == ESoulStoneExtCostType.ZuanShi)
                GameManager.ClientMgr.SubUserMoney(client, val, "聚魂额外消耗");

            return true;
        }
        #endregion

        #region 客户端请求查询魂石获取
        /// <summary>
        /// 查询节日活动---魂石额外功能
        /// </summary>
        private bool ProcessSoulStoneQueryGet(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                SoulStoneQueryGetData data = new SoulStoneQueryGetData();
                data.CurrRandId = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SoulStoneRandId);
                data.ExtFuncList = GetExtFuncItems();
                client.sendCmd(nID, data);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                return false;
            }

            return true;
        }
        #endregion

        #region 客户端请求整理魂石背包
        /// <summary>
        /// 整理魂石背包
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool ProcessSoulStoneResetBag(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                if (!IsGongNengOpened(client))
                    return true;

                // roleid
                int roleid = Convert.ToInt32(cmdParams[0]);
                ResetSoulStoneBag(client);
                client.sendCmd(nID, client.ClientData.SoulStoneInBag);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 背包排列优先级：品阶高＞等级高＞ID号靠前
        /// </summary>
        /// <param name="client"></param>
        private void ResetSoulStoneBag(GameClient client)
        {
            if (client == null) return;

            client.ClientData.SoulStoneInBag.Sort((_left, _right) =>
            {
                int l_suit = Global.GetEquipGoodsSuitID(_left.GoodsID);
                int r_suit = Global.GetEquipGoodsSuitID(_right.GoodsID);

                if (l_suit > r_suit) return -1;
                else if (l_suit < r_suit) return 1;
                else
                {
                    int lvlDiff = 0;
                    if (_left.ElementhrtsProps != null && _right.ElementhrtsProps != null)
                    {
                       lvlDiff =  _left.ElementhrtsProps[0] - _right.ElementhrtsProps[0];
                    }
                    if (lvlDiff > 0) return -1;
                    else if (lvlDiff < 0) return 1;
                    else
                    {
                        return _left.GoodsID - _right.GoodsID;
                    }
                }
            });

            for (int i = 0; i < client.ClientData.SoulStoneInBag.Count; i++)
            {
                // 背包格子不再存数据库,上线时会初始化
                client.ClientData.SoulStoneInBag[i].BagIndex = i;
            }
        }
        #endregion

        #region 客户端请求穿戴、卸下装备
        /// <summary>
        /// 穿戴、卸载魂石
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool ProcessSoulStoneModEquip(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                // roleid:bagIndex:dbid    dbid == -1 表示卸载该位置
                int roleid = Convert.ToInt32(cmdParams[0]);
                int bagIndex = Convert.ToInt32(cmdParams[1]); // 装备栏bagIndex
                int dbid = Convert.ToInt32(cmdParams[2]);

                ESoulStoneErrorCode ec = handleModEquip(client, bagIndex, dbid);
                string rsp = string.Format("{0}:{1}:{2}", (int)ec, bagIndex, dbid);
                client.sendCmd(nID, rsp);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 穿戴、卸下装备
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bagIndex"></param>
        /// <param name="newDbId"></param>
        /// <returns></returns>
        private ESoulStoneErrorCode handleModEquip(GameClient client, int bagIndex, int newDbId)
        {
            if (!IsGongNengOpened(client))
                return ESoulStoneErrorCode.NotOpen;

            int cycle = 0, grid = 0;
            ParseCycleAndGrid(bagIndex, out cycle, out grid);
            if (cycle < 1 || cycle > SoulStoneConsts.EquipCycleNum || grid < 1 || grid > SoulStoneConsts.EquipCycleGridNum)
                return ESoulStoneErrorCode.VisitParamsError;

            GoodsData newGd = null;
            if (newDbId != -1) // -1 表示仅仅是脱下该位置装备，不穿戴
            {
                // 查找下要穿戴的装备在不在
                newGd = client.ClientData.SoulStoneInBag.Find(_g => _g.Id == newDbId);
                if (newGd == null) return ESoulStoneErrorCode.VisitParamsError;

                //	魂石之环随等级开放
                Dictionary<int, int> tmpLvlLimitDict = equipLvlLimitDict;
                if (tmpLvlLimitDict == null)
                    return ESoulStoneErrorCode.ConfigError;
                if (!tmpLvlLimitDict.ContainsKey(cycle) || tmpLvlLimitDict[cycle] > Global.GetUnionLevel(client))
                    return ESoulStoneErrorCode.CanNotEquip;

                // 物品不存在
                SystemXmlItem systemGoods = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(newGd.GoodsID, out systemGoods))
                {
                    return ESoulStoneErrorCode.ConfigError;
                }

                // 检查category是否是装备，只有装备才可穿戴
                int categoriy = systemGoods.GetIntValue("Categoriy");
                if (!EquipCategorys.Contains(categoriy))
                    return ESoulStoneErrorCode.CanNotEquip;

                // 同一环不能镶嵌相同ID的魂石
                // 找到一个换相同，但是格子不同的已穿戴魂石
                GoodsData checkGd = client.ClientData.SoulStoneInUsing.Find(_g => {
                    int _cycle = 0, _grid = 0;
                    ParseCycleAndGrid(_g.BagIndex, out _cycle, out _grid);
                    if (_cycle == cycle && _grid != grid && _g.GoodsID == newGd.GoodsID)
                    {
                        return true;
                    }

                    return false;
                });

                if (checkGd != null)
                {
                    return ESoulStoneErrorCode.CanNotEquip;
                }
            }

            GoodsData oldGd = client.ClientData.SoulStoneInUsing.Find(_g => _g.BagIndex == bagIndex);
            if (oldGd != null)
            {
                // 先卸下旧装备
                if (!CanAddGoodsNum(client, 1))
                    return ESoulStoneErrorCode.BagNoSpace; // 背包已满, 旧装备无法卸下

                int newBagIndex = GetIdleSlotOfBag(client);
                if (newBagIndex < 0) return ESoulStoneErrorCode.BagNoSpace;

                int newSite = (int)SaleGoodsConsts.SoulStoneBag;

                string[] dbFields = null;
                string strCmd = Global.FormatUpdateDBGoodsStr(client.ClientData.RoleID, oldGd.Id, "*"/*isusing*/, "*", "*", "*", newSite, "*", "*", oldGd.GCount, "*", newBagIndex, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*");
                TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strCmd, out dbFields, client.ServerId);
                if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED || dbFields.Length <= 0 || Convert.ToInt32(dbFields[1]) < 0)
                {
                    // 卸载失败
                    return ESoulStoneErrorCode.DbFailed;
                }

                RemoveSoulStoneGoods(client, oldGd, oldGd.Site);
                oldGd.BagIndex = newBagIndex;
                oldGd.Site = newSite;         
                AddSoulStoneGoods(client, oldGd, oldGd.Site);

                // 通知客户端物品变更
                GameManager.ClientMgr.NotifyModGoods(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, (int)ModGoodsTypes.EquipLoad,
                    oldGd.Id, oldGd.Using, oldGd.Site, oldGd.GCount, oldGd.BagIndex, 1/*???*/);
            }

            // 穿
            if (newGd != null)
            {
                int newBagIndex = bagIndex;
                int newSite = (int)SaleGoodsConsts.SoulStoneEquip;

                string[] dbFields = null;
                string strCmd = Global.FormatUpdateDBGoodsStr(client.ClientData.RoleID, newGd.Id, "*"/*isusing*/, "*", "*", "*", newSite, "*", "*", newGd.GCount, "*", newBagIndex, "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*");
                TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, strCmd, out dbFields, client.ServerId);
                if (dbRequestResult != TCPProcessCmdResults.RESULT_FAILED && dbFields.Length > 0 && Convert.ToInt32(dbFields[1]) >= 0)
                {
                    RemoveSoulStoneGoods(client, newGd, newGd.Site);
                    newGd.BagIndex = newBagIndex;
                    newGd.Site = newSite;
                    AddSoulStoneGoods(client, newGd, newSite);

                    // 通知客户端物品变更
                    GameManager.ClientMgr.NotifyModGoods(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, (int)ModGoodsTypes.Destroy,
                        newGd.Id, newGd.Using, newGd.Site, newGd.GCount, newGd.BagIndex, 1/*???*/);
                }
                else if (oldGd == null)
                {
                    // 不脱旧装备，直接就穿失败了，提示错误
                    return ESoulStoneErrorCode.DbFailed;
                }
                else
                {
                    // 新装备穿失败，但是旧装备脱成功了，不处理
                }
            }

            UpdateProps(client);
            // 通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            return ESoulStoneErrorCode.Success;
        }
        #endregion

        #region 客户端请求升级魂石
        /// <summary>
        /// 魂石升级
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nID"></param>
        /// <param name="bytes"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        private bool ProcessSoulStoneLevelUp(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int roleid = Convert.ToInt32(cmdParams[0]);
                int target = Convert.ToInt32(cmdParams[1]);
                int site = Convert.ToInt32(cmdParams[2]);
                string[] szSourceList = cmdParams[3].Split(',');

                List<int> srcList = new List<int>();
                for (int i = 0; i < szSourceList.Length; ++i)
                {
                    if (!string.IsNullOrEmpty(szSourceList[i]))
                    {
                        srcList.Add(Convert.ToInt32(szSourceList[i]));
                    }
                }
                // 去重，防止客户端发送重复的原材料
                srcList = srcList.Distinct().ToList();

                int currLvl, currExp;
                ESoulStoneErrorCode ec = handleSoulStoneLevelUp(client, target, site, srcList, out currLvl, out currExp);

                string rsp = string.Format("{0}:{1}:{2}:{3}:{4}", (int)ec, target, site, currLvl, currExp);
                client.sendCmd(nID, rsp);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                return false;
            }

            return true;
        }

        private ESoulStoneErrorCode handleSoulStoneLevelUp(GameClient client, int target, int site, List<int> srcList, out int currLvl, out int currExp)
        {
            currLvl = 0;
            currExp = 0;

            if (!IsGongNengOpened(client))
                return ESoulStoneErrorCode.NotOpen;

            if (srcList == null || srcList.Count <= 0)
                return ESoulStoneErrorCode.VisitParamsError;

            if (srcList.IndexOf(target) >= 0)
                return ESoulStoneErrorCode.VisitParamsError;

            GoodsData targetGd = GetSoulStoneByDbId(client, site, target);
            if (targetGd == null)
                return ESoulStoneErrorCode.VisitParamsError;

            SystemXmlItem targetGoodsXml = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(targetGd.GoodsID, out targetGoodsXml))
            {
                return ESoulStoneErrorCode.ConfigError;
            }

            // 魂石装备可升级，魂石精华不可升级
            if (!EquipCategorys.Contains(targetGoodsXml.GetIntValue("Categoriy")))
            {
                return ESoulStoneErrorCode.VisitParamsError;
            }

            int targetSuit = targetGoodsXml.GetIntValue("SuitID");
            SoulStoneExpConfig targetExpCfg = null;
            if (!suitExpDict.TryGetValue(targetSuit, out targetExpCfg))
                return ESoulStoneErrorCode.ConfigError;
            if (targetGd.ElementhrtsProps == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("roleid={0}, dbid={1}的魂石等级和经验为null", client.ClientData.RoleID, target));
                return ESoulStoneErrorCode.UnknownFailed;
            }
            if (targetGd.ElementhrtsProps[0] >= targetExpCfg.MaxLevel)
                return ESoulStoneErrorCode.LevelIsFull;

            //check catagory
            int addExp = 0;
            foreach (var srcId in srcList)
            {
                GoodsData srcGd = GetSoulStoneByDbId(client, (int)SaleGoodsConsts.SoulStoneBag, srcId);
                if (srcGd == null)
                    return ESoulStoneErrorCode.VisitParamsError;

                SystemXmlItem systemMaterial = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(srcGd.GoodsID, out systemMaterial))
                {
                    return ESoulStoneErrorCode.ConfigError;
                }

                // 魂石精华
                if ((int)ItemCategories.SoulStoneJingHua == systemMaterial.GetIntValue("Categoriy"))
                {
                    Dictionary<int, int> tmpJingHuaExp = jinghuaExpDict;
                    if (tmpJingHuaExp == null || !tmpJingHuaExp.ContainsKey(srcGd.GoodsID))
                    {
                        return ESoulStoneErrorCode.ConfigError;
                    }

                    addExp += tmpJingHuaExp[srcGd.GoodsID] * srcGd.GCount;
                }
                else
                {
                    // 魂石
                    int suitid = systemMaterial.GetIntValue("SuitID");
                    SoulStoneExpConfig expCfg = null;
                    if (!suitExpDict.TryGetValue(suitid, out expCfg))
                        return ESoulStoneErrorCode.ConfigError;

                    if (srcGd.ElementhrtsProps == null)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("roleid={0}, dbid={1}的魂石等级和经验为null", client.ClientData.RoleID, srcGd.Id));
                        return ESoulStoneErrorCode.UnknownFailed;
                    }

                    int hadExp;
                    if (!expCfg.Lvl2Exp.TryGetValue(srcGd.ElementhrtsProps[0], out hadExp))
                    {
                        return ESoulStoneErrorCode.ConfigError;
                    }

                    addExp += (hadExp * srcGd.GCount) + (srcGd.ElementhrtsProps[1] * srcGd.GCount);
                }
            }

            // 删除道具
            foreach (var srcId in srcList)
            {
                GoodsData srcGd = GetSoulStoneByDbId(client, (int)SaleGoodsConsts.SoulStoneBag, srcId);
                if (srcGd == null) continue;

                //RemoveElementhrtsData(client, material);
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, srcGd, srcGd.GCount, false))
                {
                }
            }

            targetGd.ElementhrtsProps[1] += addExp;
            while (targetGd.ElementhrtsProps[0] < targetExpCfg.MaxLevel)
            {
                int currLvlExp = 0, nextLvlExp = 0;
                if (!targetExpCfg.Lvl2Exp.TryGetValue(targetGd.ElementhrtsProps[0], out currLvlExp)
                    || !targetExpCfg.Lvl2Exp.TryGetValue(targetGd.ElementhrtsProps[0] + 1, out nextLvlExp))
                {
                    break;
                }
                int needExp = nextLvlExp - currLvlExp;

                if (targetGd.ElementhrtsProps[1] < needExp)
                {
                    break;
                }

                targetGd.ElementhrtsProps[0]++;
                targetGd.ElementhrtsProps[1] -= needExp;
            }

            UpdateGoodsArgs updateGoodsArgs = new UpdateGoodsArgs() { RoleID = client.ClientData.RoleID, DbID = target, WashProps = null };
            updateGoodsArgs.ElementhrtsProps = new List<int>();
            updateGoodsArgs.ElementhrtsProps.Add(targetGd.ElementhrtsProps[0]);
            updateGoodsArgs.ElementhrtsProps.Add(targetGd.ElementhrtsProps[1]);

            //存盘并通知用户结果
            Global.UpdateGoodsProp(client, targetGd, updateGoodsArgs);

            if (targetGd.Site == (int)SaleGoodsConsts.SoulStoneEquip)
            {
                UpdateProps(client);
                //通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }

            currLvl = targetGd.ElementhrtsProps[0];
            currExp = targetGd.ElementhrtsProps[1];

            return ESoulStoneErrorCode.Success;
        }
        #endregion

        #region 客户端请求获取魂石
        /// <summary>
        /// 获取魂石(聚魂)
        /// </summary>
        private bool ProcessSoulStoneGet(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                int roleid = Convert.ToInt32(cmdParams[0]);
                int reqTimes = Convert.ToInt32(cmdParams[1]);
                string[] szExtFuncs = cmdParams[2].Split(',');

                // 玩家选择的额外功能
                List<int> selectExtFuncs = null;
                if (szExtFuncs.Length > 0)
                {
                    selectExtFuncs = new List<int>();
                    for (int i = 0; i < szExtFuncs.Length; ++i)
                    {
                        if (string.IsNullOrEmpty(szExtFuncs[i])) continue;

                        selectExtFuncs.Add(Convert.ToInt32(szExtFuncs[i]));
                    }

                    // 去重，防止发送重复的额外功能
                    selectExtFuncs = selectExtFuncs.Distinct().ToList();
                }

                // 当前开启的额外功能
                List<SoulStoneExtFuncItem> openedExtFuncs = GetExtFuncItems();

                SoulStoneGetData data = new SoulStoneGetData();
                data.RequestTimes = reqTimes;
                data.RealDoTimes = 0;
                if (reqTimes != (int)ESoulStoneGetTimes.One && reqTimes != (int)ESoulStoneGetTimes.Ten)
                {
                    data.Error = (int)ESoulStoneErrorCode.VisitParamsError;
                }
                else
                {
                    data.Stones = new List<int>();
                    data.ExtGoods = new List<int>();

                    for (int times = 1; times <= reqTimes; ++times)
                    {
                        List<int> goodsIdList = null;
                        List<int> extGoodsList = null;
                        // times传进去是为了打log分析
                        ESoulStoneErrorCode ec = handleSoulStoneGetOne(client, selectExtFuncs, openedExtFuncs, out goodsIdList, out extGoodsList, times);
                        if (ec == ESoulStoneErrorCode.Success)
                        {
                            data.Error = (int)ESoulStoneErrorCode.Success;
                            data.RealDoTimes++;
                            if (goodsIdList != null)
                                data.Stones.AddRange(goodsIdList);
                            if (extGoodsList != null)
                                data.ExtGoods.AddRange(extGoodsList);
                        }
                        else
                        {
                            if (data.RealDoTimes == 0)
                            {
                                // 第一次执行就失败了
                                data.Error = (int)ec;
                            }

                            break;
                        }
                    }
                }

                data.NewRandId = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SoulStoneRandId);
                client.sendCmd(nID, data);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 聚魂一次
        /// </summary>
        private ESoulStoneErrorCode handleSoulStoneGetOne(GameClient client, List<int> selectExtFuncs, List<SoulStoneExtFuncItem> openedExtFuncs, out List<int> goodsIdList, out List<int> extGoodsList, int currTimes)
        {
            goodsIdList = new List<int>();
            extGoodsList = new List<int>();

            if (!IsGongNengOpened(client))
                return ESoulStoneErrorCode.NotOpen;

            // 检查玩家选中的额外功能是否都开启了
            if (selectExtFuncs != null && selectExtFuncs.Count > 0
                && !selectExtFuncs.All(_type => openedExtFuncs != null && openedExtFuncs.Exists(_item => _item.FuncType == _type)))
            {
                return ESoulStoneErrorCode.SelectExtFuncNotOpen;
            }

            // 去除玩家当前所属的随机组配置文件
            int oldRandId = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SoulStoneRandId);
            SoulStoneRandConfig randCfg = null;
            if (!randDict.TryGetValue(oldRandId, out randCfg))
            {
                return ESoulStoneErrorCode.ConfigError;
            }

            SoulStoneExtFuncItem addedItem = null;
            SoulStoneExtFuncItem reduceItem = null;
            SoulStoneExtFuncItem upSucessItem = null;
            SoulStoneExtFuncItem holdTypeItem = null;

            if (selectExtFuncs != null && openedExtFuncs != null && selectExtFuncs.Contains((int)ESoulStoneExtFuncType.AddedGoods))
                addedItem = openedExtFuncs.Find(_item => _item.FuncType == (int)ESoulStoneExtFuncType.AddedGoods);
            if (selectExtFuncs != null && openedExtFuncs != null && selectExtFuncs.Contains((int)ESoulStoneExtFuncType.ReduceLangHunFenMo))
                reduceItem = openedExtFuncs.Find(_item => _item.FuncType == (int)ESoulStoneExtFuncType.ReduceLangHunFenMo);
            if (selectExtFuncs != null && openedExtFuncs != null && selectExtFuncs.Contains((int)ESoulStoneExtFuncType.UpSuccessRate))
                upSucessItem = openedExtFuncs.Find(_item => _item.FuncType == (int)ESoulStoneExtFuncType.UpSuccessRate);
            if (selectExtFuncs != null && openedExtFuncs != null && selectExtFuncs.Contains((int)ESoulStoneExtFuncType.HoldTypeIfFail))
                holdTypeItem = openedExtFuncs.Find(_item => _item.FuncType == (int)ESoulStoneExtFuncType.HoldTypeIfFail);

            Dictionary<ESoulStoneExtCostType, int> extCostDict = new Dictionary<ESoulStoneExtCostType, int>(){
                {ESoulStoneExtCostType.MoJing, 0},
                {ESoulStoneExtCostType.XingHun, 0},
                {ESoulStoneExtCostType.ChengJiu, 0},
                {ESoulStoneExtCostType.ShengWang, 0},
                {ESoulStoneExtCostType.ZuanShi, 0},
            };

            bool bAddedItem = false;
            bool bUpSuccessRate = false;
            bool bHoldType = false;
            int costLangHun = randCfg.NeedLangHunFenMo;

            // 额外功能1:消耗XXX获得额外道具
            if (addedItem != null)
            {
                ESoulStoneExtCostType costType = (ESoulStoneExtCostType)addedItem.CostType;
                int costValue;
                if (randCfg.AddedNeedDict.TryGetValue(costType, out costValue) && costValue > 0)
                {
                    int hadVal = ExtCostTypeHadValue(client, costType);
                    if (hadVal < costValue + extCostDict[costType])
                    {
                        return ESoulStoneErrorCode.ExtCostNotEnough;
                    }

                    extCostDict[costType] += costValue;

                    // 有概率获得额外道具
                    if ((Global.GetRandomNumber(1, 100) * 1.0 / 100) <= randCfg.AddedRate)
                    {
                        bAddedItem = true;
                    }
                }
            }

            // 额外功能2:消耗XXX减少狼魂粉末消耗
            if (reduceItem != null)
            {
                ESoulStoneExtCostType costType = (ESoulStoneExtCostType)reduceItem.CostType;
                int costValue;
                if (randCfg.ReduceNeedDict.TryGetValue(costType, out costValue) && costValue > 0)
                {
                    int hadVal = ExtCostTypeHadValue(client, costType);
                    if (hadVal < costValue + extCostDict[costType])
                    {
                        return ESoulStoneErrorCode.ExtCostNotEnough;
                    }

                    extCostDict[costType] += costValue;

                    // 有概率减少狼魂粉末的消耗
                    if ((Global.GetRandomNumber(1, 100) * 1.0 / 100) <= randCfg.ReduceRate)
                    {
                        costLangHun -= randCfg.ReduceValue;
                    }
                }
            }

            // 额外功能3:消耗XXX提高成功概率到
            if (upSucessItem != null)
            {
                ESoulStoneExtCostType costType = (ESoulStoneExtCostType)upSucessItem.CostType;
                int costValue;
                if (randCfg.UpSucRateNeedDict.TryGetValue(costType, out costValue) && costValue > 0)
                {
                    int hadVal = ExtCostTypeHadValue(client, costType);
                    if (hadVal < costValue + extCostDict[costType])
                    {
                        return ESoulStoneErrorCode.ExtCostNotEnough;
                    }

                    extCostDict[costType] += costValue;
                    bUpSuccessRate = true;
                }
            }

            // 额外功能4消耗XXX失败不变系
            if (holdTypeItem != null)
            {
                ESoulStoneExtCostType costType = (ESoulStoneExtCostType)holdTypeItem.CostType;
                int costValue;
                if (randCfg.UpSucRateNeedDict.TryGetValue(costType, out costValue) && costValue > 0)
                {
                    int hadVal = ExtCostTypeHadValue(client, costType);
                    if (hadVal < costValue + extCostDict[costType])
                    {
                        return ESoulStoneErrorCode.ExtCostNotEnough;
                    }

                    extCostDict[costType] += costValue;
                    bHoldType = true;
                }
            }

            // 各种额外消耗都ok了，看下狼魂粉末咋样，够不够？
            costLangHun = Math.Max(0, costLangHun);
            if (costLangHun > 0 && costLangHun > Global.GetRoleParamsInt32FromDB(client, RoleParamName.LangHunFenMo))
            {
                return ESoulStoneErrorCode.LangHunFenMoNotEnough;
            }

            // 看下背包够不够
            if (!CanAddGoodsNum(client, 1 + (bAddedItem ? 1 : 0)))
            {
                return ESoulStoneErrorCode.BagNoSpace;
            }

            // 额外消耗
            foreach (var kvp in extCostDict)
            {
                DoExtCostType(client, kvp.Key, kvp.Value);
            }

            // 消耗狼魂
            GameManager.ClientMgr.ModifyLangHunFenMoValue(client, -costLangHun, "聚魂", true, true);

            // 随机产生一个魂石
            int magic = Global.GetRandomNumber(randCfg.RandMinNumber, randCfg.RandMaxNumber);
            foreach (var ri in randCfg.RandStoneList)
            {
                if (ri.RandBegin <= magic && magic <= ri.RandEnd)
                {
                    GoodsData gd = Global.CopyGoodsData(ri.Goods);

                    List<int> elementhrtsProps = new List<int>();
                    elementhrtsProps.Add(SoulStoneConsts.DefaultLevel);
                    elementhrtsProps.Add(0);

                    gd.Site = (int)SaleGoodsConsts.SoulStoneBag;
                    gd.ElementhrtsProps = elementhrtsProps;

                    Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                                gd.GoodsID, gd.GCount/*GCount*/,
                                0, ""/*props*/, gd.Forge_level/*forgelevel*/,
                                gd.Binding/*binding*/, gd.Site, ""/*jewelList*/, false, 1,
                        /**/"聚魂", Global.ConstGoodsEndTime, 0, 0, 0, 0/*Strong*/, 0, 0, 0, null, elementhrtsProps);

                    goodsIdList.Add(gd.GoodsID);

                    break;
                }
            }

            // 获得额外道具
            if (bAddedItem)
            {
                GoodsData addedGd = Global.CopyGoodsData(randCfg.AddedGoods);

                List<int> elementhrtsProps = new List<int>();
                elementhrtsProps.Add(SoulStoneConsts.DefaultLevel);
                elementhrtsProps.Add(0);

                addedGd.Site = (int)SaleGoodsConsts.SoulStoneBag;
                addedGd.ElementhrtsProps = elementhrtsProps;

                Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                            addedGd.GoodsID, addedGd.GCount/*GCount*/,
                            0, ""/*props*/, addedGd.Forge_level/*forgelevel*/,
                            addedGd.Binding/*binding*/, addedGd.Site, ""/*jewelList*/, false, 1,
                    /**/"聚魂", Global.ConstGoodsEndTime, 0, 0, 0, 0/*Strong*/, 0, 0, 0, null, elementhrtsProps);

                extGoodsList.Add(addedGd.GoodsID);
            }

            // 再随机魂石组
            int newRandId;
            double upSuccessRate = bUpSuccessRate ? randCfg.UpSucRateTo : randCfg.SuccessRate;
            double randSuccessRate = Global.GetRandomNumber(1, 101) * 1.0 / 100;
            if (randSuccessRate <= upSuccessRate)
            {
                // 随机本系成功
                newRandId = randCfg.SuccessTo[Global.GetRandomNumber(0, randCfg.SuccessTo.Count)];
            }
            else if (bHoldType)
            {
                // 随机本系失败，但是锁定不跳转
                newRandId = randCfg.FailToIfHold[Global.GetRandomNumber(0, randCfg.FailToIfHold.Count)];
            }
            else
            {
                // 随机本系失败，跳转
                newRandId = randCfg.FailTo[Global.GetRandomNumber(0, randCfg.FailTo.Count)];
            }

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.SoulStoneRandId, newRandId, true);

            // 写入分析日志，方便QA测试
            if (bOpenStoneGetLog)
            {
                StringBuilder sb = new StringBuilder();
                /**/sb.AppendFormat("rolename={0} 第{1}次聚魂, 再随机成功配置几率={2}, 产生几率={3}, 随机组变化{4}--->{5},", client.ClientData.RoleName, currTimes, upSuccessRate, randSuccessRate, oldRandId, newRandId);
                /**/sb.Append("消耗[");
                /**/sb.Append("狼魂粉末:" + costLangHun + ",");
                /**/sb.Append("魔晶:" + extCostDict[ESoulStoneExtCostType.MoJing] + ",");
                /**/sb.Append("星魂:" + extCostDict[ESoulStoneExtCostType.XingHun] + ",");
                /**/sb.Append("成就:" + extCostDict[ESoulStoneExtCostType.ChengJiu] + ",");
                /**/sb.Append("声望:" + extCostDict[ESoulStoneExtCostType.ShengWang] + ",");
                /**/sb.Append("钻石:" + extCostDict[ESoulStoneExtCostType.ZuanShi] + "]");
                sb.AppendLine();

                LogManager.WriteLog(LogTypes.Error, sb.ToString());
            }

            return ESoulStoneErrorCode.Success;
        }

        #endregion

        /// <summary>
        /// GM测试
        /// </summary>
        /// <param name="client"></param>
        /// <param name="args"></param>
        public void GM_Test(GameClient client, string[] args)
        {
            if (client == null) return;

            if (args.Length >= 2)
            {
                if (args[1] == "addlanghun")
                {
                    if (args.Length >= 3)
                    {
                        int val = Convert.ToInt32(args[2]);
                        GameManager.ClientMgr.ModifyLangHunFenMoValue(client, val, "GM", true, true);
                    }
                }
                else if (args[1] == "juhun")
                {
                    if (args.Length >= 3)
                    {
                        int times = Convert.ToInt32(args[2]);
                        List<int> selectExtFuncs = new List<int>();
                        if (args.Length >= 4)
                        {
                            string[] fields = args[3].Split(',');
                            for (int i = 0; i < fields.Length; ++i)
                            {
                                selectExtFuncs.Add(Convert.ToInt32(fields[i]));
                            }
                        }

                        List<SoulStoneExtFuncItem> openedExtFuncs = GetExtFuncItems();

                        for (int currTime = 1; currTime <= times; ++currTime)
                        {
                           
                        }
                    }
                }
                else if (args[1] == "modequip")
                {
                    if (args.Length >= 4)
                    {
                        int slot = Convert.ToInt32(args[2]);
                        int newDbId = Convert.ToInt32(args[3]);
                        handleModEquip(client, slot, newDbId);
                    }
                }
                else if (args[1] == "resetbag")
                {
                    ResetSoulStoneBag(client);
                }
                else if (args[1] == "lvlup")
                {
                    if (args.Length >= 5)
                    {
                        int target = Convert.ToInt32(args[2]);
                        int site = Convert.ToInt32(args[3]);
                        List<int> srcList = new List<int>();
                        string[] fields = args[4].Split(',');
                        for (int i = 0; i < fields.Length; ++i)
                        {
                            srcList.Add(Convert.ToInt32(fields[i]));
                        }
                        int currLvl, currExp;
                        handleSoulStoneLevelUp(client, target, site, srcList, out currLvl, out currExp);
                    }
                }
            }
        }
    }
}
