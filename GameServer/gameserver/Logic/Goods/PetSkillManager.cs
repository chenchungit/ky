using GameServer.Core.Executor;
using GameServer.Logic.Damon;
using GameServer.Server;
using GameServer.Tools;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic.Goods
{
    public class PetSkillManager : ICmdProcessorEx, IManager
    {
        #region ----------接口
        private const int PIT_MIN = 1;
        private const int PIT_MAX = 4;
        private const int UP_LEVEL_MAX = 5;
        private const int STATUE_COUNT = 8;
        private const int RANDOM_SEED_AWAKE = 100000;

        public static int _gmRate = 1;

        private static PetSkillManager instance = new PetSkillManager();
        public static PetSkillManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            InitConfig();
            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_PET_SKILL_UP, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_PET_SKILL_AWAKE, 3, 3, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_PET_SKILL_AWAKE_COST, 1, 1, getInstance());

            return true;
        }

        public bool showdown() { return true; }
        public bool destroy() { return true; }
        public bool processCmd(GameClient client, string[] cmdParams) { return true; }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_PET_SKILL_UP:
                    return ProcessCmdPetSkillUp(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_PET_SKILL_AWAKE:
                    return ProcessCmdPetSkillAwake(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_PET_SKILL_AWAKE_COST:
                    return ProcessCmdPetSkillAwakeCost(client, nID, bytes, cmdParams);
            }

            return true;
        }

        private bool ProcessCmdPetSkillUp(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 2);
                if (!isCheck) return false;

                int petID = Convert.ToInt32(cmdParams[0]);
                int pit = Convert.ToInt32(cmdParams[1]);
                int resultType = (int)PetSkillUp(client, petID, pit);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_PET_SKILL_UP, resultType);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        private bool ProcessCmdPetSkillAwake(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 3);
                if (!isCheck) return false;


                int petID = Convert.ToInt32(cmdParams[0]);
                int lockPit1 = Convert.ToInt32(cmdParams[1]);
                int lockPit2 = Convert.ToInt32(cmdParams[2]);

                List<int> lockPitList = new List<int>();
                if (lockPit1 > 0) lockPitList.Add(lockPit1);
                if (lockPit2 > 0) lockPitList.Add(lockPit2);

                string result = "";
                int resultType = (int)PetSkillAwake(client,petID,lockPitList,out result);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_PET_SKILL_AWAKE, string.Format("{0}:{1}", resultType, result));

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        private bool ProcessCmdPetSkillAwakeCost(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                int result = GetSkillAwakeCost(GetUpCount(client));
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_PET_SKILL_AWAKE_COST, result);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        #endregion

        #region 功能
        private static EPetSkillState PetSkillUp(GameClient client, int petID, int pit)
        {
            if (!IsGongNengOpened(client)) return EPetSkillState.EnoOpen;
            //物品
            GoodsData goodsData = DamonMgr.GetDamonGoodsDataByDbID(client, petID);
            if (null == goodsData || goodsData.GCount <= 0) return EPetSkillState.EnoPet;
            if (goodsData.Site != (int)SaleGoodsConsts.UsingDemonGoodsID) return EPetSkillState.EnoUsing;
            if(pit<PIT_MIN || pit>PIT_MAX) return EPetSkillState.EpitWrong;
            //open
            List<PetSkillInfo> petSkillList = GetPetSkillInfo(goodsData);
            PetSkillInfo skillInfo = petSkillList.Find(_g => _g.Pit == pit);
            if (!skillInfo.PitIsOpen) return EPetSkillState.EpitNoOpen;

            if (skillInfo.SkillID <= 0) return EPetSkillState.EpitSkillNull;
            
            int maxLevel = GetPsUpMaxLevel();
            if (skillInfo.Level >= maxLevel) return EPetSkillState.ElevelMax;

            int oldLevel = skillInfo.Level;
            int nextLevel = skillInfo.Level + 1;
            int lingJingNeed = (int)GetPsUpCost(nextLevel);
            long lingjingHave = GameManager.ClientMgr.GetMUMoHeValue(client);
            if (lingjingHave < lingJingNeed) return EPetSkillState.EnoLingJing;
            //扣除
            GameManager.ClientMgr.ModifyMUMoHeValue(client, -lingJingNeed, "精灵技能升级", true, true);

            skillInfo.Level = nextLevel;
            //存盘并通知用户结果
            UpdateGoodsArgs updateGoodsArgs = new UpdateGoodsArgs() { RoleID = client.ClientData.RoleID, DbID = petID, WashProps = null };
            updateGoodsArgs.ElementhrtsProps = new List<int>();
            foreach (var info in petSkillList)
            {
                updateGoodsArgs.ElementhrtsProps.Add(info.PitIsOpen ? 1 : 0);
                updateGoodsArgs.ElementhrtsProps.Add(info.Level);
                updateGoodsArgs.ElementhrtsProps.Add(info.SkillID);
            }

            Global.UpdateGoodsProp(client, goodsData, updateGoodsArgs);

            if (goodsData.Using > 0) UpdateRolePetSkill(client);

            EventLogManager.AddPetSkillEvent(client, LogRecordType.PetSkill, EPetSkillLog.Up,
                petID, goodsData.GoodsID, pit, oldLevel, nextLevel);

            return EPetSkillState.Success;
        }

        public static List<PetSkillInfo> GetPetSkillInfo(GoodsData data)
        {
            List<PetSkillInfo> list = new List<PetSkillInfo>();
            if (data.ElementhrtsProps == null)
                data.ElementhrtsProps = new List<int>() { 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0 };

            int pit = 1;
            for (int i = 0; i < data.ElementhrtsProps.Count; i++)
            {
                PetSkillInfo info = new PetSkillInfo();
                info.PitIsOpen = data.ElementhrtsProps[i++] > 0;
                if (!info.PitIsOpen)
                {
                    int openLevel = GetPitOpenLevel(pit);
                    if (data.Forge_level+1 >= openLevel) info.PitIsOpen = true;
                }

                info.Pit = pit++;
                info.Level = data.ElementhrtsProps[i++];
                info.SkillID = data.ElementhrtsProps[i];
                list.Add(info);
            }

            return list;
        }

        private static EPetSkillState PetSkillAwake(GameClient client, int petID, List<int> lockPitList, out string result)
        {
            result = "";
            if (!IsGongNengOpened(client)) return EPetSkillState.EnoOpen;

            GoodsData goodsData = DamonMgr.GetDamonGoodsDataByDbID(client, petID);
            if (null == goodsData || goodsData.GCount <= 0) return EPetSkillState.EnoPet;
            if (goodsData.Site != (int)SaleGoodsConsts.UsingDemonGoodsID) return EPetSkillState.EnoUsing;

            List<PetSkillInfo> petSkillList = GetPetSkillInfo(goodsData);
            //锁定，钻石
            int diamondNeed = 0;
            if (lockPitList.Count > 0)
            {
                foreach (var lockPit in lockPitList)
                {
                    if (lockPit > PIT_MAX) return EPetSkillState.EpitWrong;
                    if (!petSkillList[lockPit-1].PitIsOpen) return EPetSkillState.EpitNoOpen;
                }
                
                diamondNeed = GetPitLockCost(lockPitList.Count);
                if (diamondNeed > 0 && client.ClientData.UserMoney < diamondNeed)
                    return EPetSkillState.EnoDiamond;
            }
            //次数，灵晶
            int awakeCount = GetUpCount(client);
            int lingJingNeed = GetSkillAwakeCost(awakeCount);
            long lingjingHave = GameManager.ClientMgr.GetMUMoHeValue(client);
            if (lingjingHave < lingJingNeed) return EPetSkillState.EnoLingJing;
            //可领悟pit
            List<PetSkillInfo> canAwakeSkillList = null;

            List<PetSkillInfo> openList = petSkillList.FindAll(_g => _g.PitIsOpen == true);
            if (openList == null || openList.Count <= 0) return EPetSkillState.EpitNoOpen;

            if (lockPitList != null && lockPitList.Count > 0)
            {
                var temp = from info in openList
                           where info.PitIsOpen && lockPitList.IndexOf(info.Pit) <0
                           select info;

                if (!temp.Any()) return EPetSkillState.EnoPitAwake;
                canAwakeSkillList = temp.ToList<PetSkillInfo>();
            }
            else
            {
                canAwakeSkillList = openList;
            }

            var t = from info in canAwakeSkillList
                    where info.PitIsOpen && info.SkillID <= 0
                    select info;
            if (t.Any())
            {
                List<PetSkillInfo> list = t.ToList<PetSkillInfo>();
                canAwakeSkillList = list;
            }
            
            int skRand = Global.GetRandomNumber(0, canAwakeSkillList.Count);
            PetSkillInfo nowAwakeInfo = canAwakeSkillList[skRand];

            //可领取技能
            List<int> canAwakeSkillIDList = new List<int>();
            var tt = _psDic.Where(p => !petSkillList.Select(g => g.SkillID).Contains(p.Value.SkillID));
            if (!tt.Any()) return EPetSkillState.EnoSkillAwake;

            int nowAwakeSkillID = 0;
            int seed = tt.Sum(_s => _s.Value.Rate);
            int skillRand = Global.GetRandomNumber(0, seed);
            int sum = 0;
            int rate = 0;
            foreach (var info in tt)
            {
                nowAwakeSkillID = info.Key;

                rate = info.Value.Rate;
                sum += info.Value.Rate;
                if (sum>=skillRand) break;
            }

            //LogManager.WriteLog(LogTypes.Error, string.Format("---------------------seed={0} random={1} sum={2} rate={3} skillID={4}", seed, skillRand, sum, rate, nowAwakeSkillID));

            //foreach(var s in tt)
            //   canAwakeSkillIDList.Add(s.Key);
            //if (canAwakeSkillIDList.Count<=0) return EPetSkillState.EnoSkillAwake;

            //int skillRand = Global.GetRandomNumber(0, canAwakeSkillIDList.Count);
            //int nowAwakeSkillID = canAwakeSkillIDList[skillRand];

            int oldSkillID = nowAwakeInfo.SkillID;
            nowAwakeInfo.SkillID = nowAwakeSkillID;
            //扣除
            if (diamondNeed > 0 && !GameManager.ClientMgr.SubUserMoney(client, diamondNeed, "精灵技能领悟")) return EPetSkillState.EnoDiamond;
            GameManager.ClientMgr.ModifyMUMoHeValue(client, -lingJingNeed, "精灵技能领悟", true, true);
            ModifyUpCount(client, awakeCount+1);
            //存盘并通知用户结果
            UpdateGoodsArgs updateGoodsArgs = new UpdateGoodsArgs() { RoleID = client.ClientData.RoleID, DbID = petID, WashProps = null };
            updateGoodsArgs.ElementhrtsProps = new List<int>();
            foreach (var info in petSkillList)
            {
                updateGoodsArgs.ElementhrtsProps.Add(info.PitIsOpen ? 1 : 0);
                updateGoodsArgs.ElementhrtsProps.Add(info.Level);
                updateGoodsArgs.ElementhrtsProps.Add(info.SkillID);
            }

            Global.UpdateGoodsProp(client, goodsData, updateGoodsArgs);

            result = string.Join(",", updateGoodsArgs.ElementhrtsProps.ToArray());

            UpdateRolePetSkill(client);

            EventLogManager.AddPetSkillEvent(client, LogRecordType.PetSkill, EPetSkillLog.Awake,
               petID, goodsData.GoodsID, nowAwakeInfo.Pit, oldSkillID, nowAwakeSkillID);

            return EPetSkillState.Success;
        }

        public static int GetUpCount(GameClient client)
        {
            int count = 0;
            int dayOld = 0;
            List<int> data = Global.GetRoleParamsIntListFromDB(client, RoleParamName.PetSkillUpCount);
            if (data != null && data.Count > 0)
                dayOld = data[0];

            int day = Global.GetOffsetDayNow();
            if (dayOld == day)
                count = data[1];
            else
                ModifyUpCount(client, count);

            return count;
        }

        public static void ModifyUpCount(GameClient client, int count)
        {
            List<int> dataList = new List<int>();
            dataList.AddRange(new int[] { Global.GetOffsetDayNow(), count });

            Global.SaveRoleParamsIntListToDB(client, dataList, RoleParamName.PetSkillUpCount, true);
        }

        public static long DelGoodsReturnLingJing(GoodsData goodsData)
        {
            long sum = 0;
            List<PetSkillInfo> petSkillList = GetPetSkillInfo(goodsData);
            var temp = from info in petSkillList
                       where info.PitIsOpen && info.Level > 0
                       select info;
            if (!temp.Any()) return sum;

            foreach (var info in temp)
            {
                sum += (from levelInfo in _psLevelUpDic
                        where levelInfo.Key <= info.Level
                        select levelInfo.Value).Sum();
            }

            return sum;
        }

        public static void UpdateRolePetSkill(GameClient client)
        {
            List<PassiveSkillData> resultList = new List<PassiveSkillData>();

            List<GoodsData> petList = client.ClientData.DamonGoodsDataList;
            GoodsData warPet = client.ClientData.DamonGoodsDataList.Find(_g => _g.Using > 0);
            if (warPet != null)
            { 
                List<PetSkillInfo> allSkillList = new List<PetSkillInfo>();
                List<PetSkillInfo> petSkillList = GetPetSkillInfo(warPet);
                var temp = from info in petSkillList
                           where info.PitIsOpen && info.SkillID > 0
                           select info;

                if (temp.Any())
                {
                    foreach (var t in temp)
                    {
                        SystemXmlItem systemMagic = null;
                        if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(t.SkillID, out systemMagic))
                            continue;

                        PassiveSkillData data = new PassiveSkillData();
                        data.skillId = t.SkillID;
                        data.skillLevel = t.Level;
                        data.triggerRate = (int)(systemMagic.GetDoubleValue("TriggerOdds") * 100);
                        data.triggerType = systemMagic.GetIntValue("TriggerType");
                        data.coolDown = systemMagic.GetIntValue("CDTime");
                        data.triggerCD = systemMagic.GetIntValue("TriggerCD");

                        resultList.Add(data);
                    }
                }
            }

            client.passiveSkillModule.UpdateSkillList(resultList);
            JingLingQiYuanManager.getInstance().RefreshProps(client);
        }

        #endregion

        #region ----------配置

        private static Dictionary<int, PetSkillAwakeInfo> _psDic = new Dictionary<int, PetSkillAwakeInfo>();
        
        private static Dictionary<int, long> _psLevelUpDic = new Dictionary<int, long>();
        private static Dictionary<int, int> _pitOpenDic = new Dictionary<int, int>();
        private static Dictionary<int, int> _pitLockDic = new Dictionary<int, int>();

        public static void InitConfig()
        {
            LoadPsInfo();
            LoadPsUpInfo();
            LoadPitOpenLevel();
            LoadPitLockCost();
        }

        private static void LoadPsInfo()
        {
            string fileName = Global.GameResPath("Config/PetSkill.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _psDic.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    PetSkillAwakeInfo config = new PetSkillAwakeInfo();
                    config.SkillID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "SkillID", "0"));
                    config.RateMin = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "StartValues", "0"));
                    config.RateMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "EndValues", "0"));
                    config.Rate = config.RateMax - config.RateMin + 1;

                    _psDic.Add(config.SkillID, config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }
        }

        public static void LoadPsUpInfo()
        {
            string fileName = Global.GameResPath("Config/PetSkillLevelup.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _psLevelUpDic.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    int level = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Level", "0"));
                    long cost = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "Cost", "0"));

                    if (!_psLevelUpDic.ContainsKey(level))
                        _psLevelUpDic.Add(level, cost);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }
        }

        public static void LoadPitOpenLevel()
        {
            try
            {
                _pitOpenDic.Clear();

                string str = GameManager.systemParamsList.GetParamValueByName("PatSkillCostLevel");
                if (string.IsNullOrEmpty(str)) return;

                string[] arr = str.Split('|');
                foreach (string s in arr)
                {
                    string[] r = s.Split(',');

                    int pit = int.Parse(r[0]);
                    int level = int.Parse(r[1]);

                    if (!_pitOpenDic.ContainsKey(pit))
                        _pitOpenDic.Add(pit, level);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", "PatSkillCostLevel"));
            }
        }

        public static void LoadPitLockCost()
        {
            try
            {
                _pitLockDic.Clear();

                string str = GameManager.systemParamsList.GetParamValueByName("PatSkillCostZuanShi");
                if (string.IsNullOrEmpty(str)) return;

                string[] arr = str.Split('|');
                foreach (string s in arr)
                {
                    string[] r = s.Split(',');

                    int pit = int.Parse(r[0]);
                    int value = int.Parse(r[1]);

                    if (!_pitLockDic.ContainsKey(pit))
                        _pitLockDic.Add(pit, value);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", "PatSkillCostLevel"));
            }
        }

        public static PetSkillAwakeInfo GetPsInfo(int id)
        {
            if (_psDic.ContainsKey(id))
                return _psDic[id];

            return null;
        }

        public static int GetPsUpMaxLevel()
        {
            if (_psLevelUpDic == null && _psLevelUpDic.Count <= 0)
                return 0;

            return _psLevelUpDic.Keys.Max();
        }

        public static long GetPsUpCost(int nextLevel)
        {
            if (_psLevelUpDic.ContainsKey(nextLevel))
                return _psLevelUpDic[nextLevel];

            return 0;
        }

        public static int GetPitOpenLevel(int pit)
        {
            if (_pitOpenDic.ContainsKey(pit))
                return _pitOpenDic[pit];

            return 0;
        }

        public static int GetPitLockCost(int count)
        {
            if (_pitLockDic.ContainsKey(count))
                return (int)_pitLockDic[count];

            return 0;
        }

        public static int GetSkillAwakeCost(int count)
        {
            int[] costList = GameManager.systemParamsList.GetParamValueIntArrayByName("PatSkillCostLingJing");

            if (count >= costList.Length)
                count = costList.Length - 1;

            return costList[count];
        }

        #endregion

        #region other

        public static bool IsGongNengOpened(GameClient client)
        {
            // 如果2.0的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System2Dot0)) return false;
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.PetSkill)) return false;
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.PetSkill)) return false;
            return true;
        }

        #endregion


    }
}
