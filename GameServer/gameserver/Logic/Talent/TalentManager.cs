using GameServer.Server;
using GameServer.Tools;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Tmsk.Contract;

namespace GameServer.Logic.Talent
{
    public class TalentManager : ICmdProcessorEx
    {
        #region ----------接口
        private static TalentManager instance = new TalentManager();
        public static TalentManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            LoadTalentExpInfo();
            LoadTalentSpecialData();
            LoadTalentInfoData();

            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TALENT_OTHER, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TALENT_GET_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TALENT_ADD_EXP, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TALENT_WASH, 2, 2, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_TALENT_ADD_EFFECT, 3, 3, getInstance());

            return true;
        }

        public bool showdown() { return true; }
        public bool destroy() { return true; }
        public bool processCmd(GameClient client, string[] cmdParams) { return false; }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_TALENT_OTHER:
                    return ProcessCmdTalentOther(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TALENT_GET_DATA:
                    return ProcessCmdTalentGetData(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TALENT_ADD_EXP:
                    return ProcessCmdTalentAddExp(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TALENT_WASH:
                    return ProcessCmdTalentWash(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_TALENT_ADD_EFFECT:
                    return ProcessCmdTalentAddEffect(client, nID, bytes, cmdParams);
            }

            return true;
        }

        public bool ProcessCmdTalentOther(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                int roleID = Convert.ToInt32(cmdParams[0]);

                TalentData talentData = null;
                GameClient otherClient = GameManager.ClientMgr.FindClient(roleID);
                if (otherClient != null)
                    talentData = GetTalentData(otherClient);

                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TALENT_OTHER, talentData);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessCmdTalentGetData(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                TalentData talentData = GetTalentData(client);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TALENT_GET_DATA, talentData);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessCmdTalentAddExp(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 1);
                if (!isCheck) return false;
       
                int state = TalentAddExp(client);

                TalentData talentData = GetTalentData(client);
                talentData.State = state;
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TALENT_ADD_EXP, talentData);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessCmdTalentWash(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 2);
                if (!isCheck) return false;

                int washType = int.Parse(cmdParams[1]);
                int state = TalentWash(client, washType);

                TalentData talentData = GetTalentData(client);
                talentData.State = state;
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TALENT_WASH, talentData);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessCmdTalentAddEffect(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 3);
                if (!isCheck) return false;

                int effectId = int.Parse(cmdParams[1]);
                int count = int.Parse(cmdParams[2]);
                int state = TalentAddEffect(client, effectId, count);

                TalentData talentData = GetTalentData(client);
                talentData.State = state;
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_TALENT_ADD_EFFECT, talentData);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        #endregion

        #region -----------功能

        /// <summary>
        /// 获取天赋数据
        /// </summary>
        private static TalentData GetTalentData(GameClient client)
        {
            //开放
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.Talent))
                return null;

            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return null;
            }

            client.ClientData.MyTalentData.IsOpen = true;
            client.ClientData.MyTalentData.SkillOneValue = client.ClientData.MyTalentPropData.SkillOneValue;
            client.ClientData.MyTalentData.SkillAllValue = client.ClientData.MyTalentPropData.SkillAllValue;
            client.ClientData.MyTalentData.Occupation = client.ClientData.Occupation;

            return client.ClientData.MyTalentData;
        }

        /// <summary>
        /// 经验注入
        /// </summary>
        private static int TalentAddExp(GameClient client)
        {
            //开放
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.Talent))
                return TalentResultType.EnoOpen;

            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return TalentResultType.EnoOpen;
            }

            //没有经验
            if (client.ClientData.Experience <= 0)
                return TalentResultType.EnoExp;

            TalentData talentData = client.ClientData.MyTalentData;

            //经验数据
            int talentCount = talentData.TotalCount <= 0 ? 1 : talentData.TotalCount + 1;
            if (!_TalentExpList.ContainsKey(talentCount))
                return TalentResultType.EnoOpenPoint;

            //等级限制
            TalentExpInfo expInfo = _TalentExpList[talentCount];
            int level = client.ClientData.ChangeLifeCount * 100 + client.ClientData.Level;
            if (level < expInfo.RoleLevel)
                return TalentResultType.EnoOpenPoint;

            //需要经验      
            long needExp = expInfo.Exp - talentData.Exp;

            long exp = 0;//注入经验
            long expAdd = 0;//增加的经验
            long expRole = client.ClientData.Experience;//角色现有经验
            bool isUp = false;
            if (needExp <= expRole)
            {
                isUp = true;
                expAdd = needExp;
            }
            else
            {
                exp = talentData.Exp + expRole;
                talentCount -= 1;
                expAdd = expRole;
            }

            if (!DBTalentModify(client.ClientData.RoleID, talentCount, exp, expAdd, isUp, client.ClientData.ZoneID,client.ServerId))
                return TalentResultType.EFail;

            if (isUp)
            {
                talentData.Exp = exp;
                talentData.TotalCount += 1;
                client.ClientData.Experience -= needExp;
            }
            else
            {
                talentData.Exp = exp;
                client.ClientData.Experience -= expRole;

            }

            //经验通知
            GameManager.ClientMgr.NotifySelfExperience(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, -exp);

            if (isUp)
                return TalentResultType.Success;
            else
                return TalentResultType.SuccessHalf;
        }

        /// <summary>
        /// 洗点
        /// </summary>
        /// <param name="client"></param>
        /// <param name="washType">洗点类型 TalentWashType</param>
        /// <returns></returns>
        private static int TalentWash(GameClient client, int washType)
        {
            //开放
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.Talent))
                return TalentResultType.EnoOpen;

            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return TalentResultType.EnoOpen;
            }

            TalentData talentData = client.ClientData.MyTalentData;

            //点数
            int washCount = GetTalentUseCount(talentData);
            if (washCount <= 0)
                return TalentResultType.EnoTalentCount;

            bool result = false;
            if (washType == (int)TalentWashType.Diamond)
            {
                int needDiamond = GetWashDiamond(washCount);
                result = GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, needDiamond, "天赋洗点");
                if (!result) return TalentResultType.EnoDiamond;
            }
            else
            { //从背包中找装备
                int goodsId = 0;
                int goodsCount = 0;
                GetWashGoods(out goodsId,out goodsCount);
                GoodsData goodsData = Global.GetGoodsByID(client, goodsId);
                if (goodsData == null) return TalentResultType.EnoWash;

                //扣除物品
                result = GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, goodsData, goodsCount, false);
                if (!result) return TalentResultType.EnoWash;
            }

            //清除数据库加点数据
            result = DBTalentEffectClear(client.ClientData.RoleID,client.ClientData.ZoneID,client.ServerId);
            if (!result) return TalentResultType.EFail;

            //分配点数
            talentData.CountList[(int)TalentType.Savage] = 0;
            talentData.CountList[(int)TalentType.Tough] = 0;
            talentData.CountList[(int)TalentType.Quick] = 0;

            //效果
            talentData.EffectList = new List<TalentEffectItem>();

            //属性清空
            TalentPropData propData = client.ClientData.MyTalentPropData;
            propData.ResetProps();

            SetTalentProp(client, TalentEffectType.PropBasic, propData.PropItem);
            SetTalentProp(client, TalentEffectType.PropExt, propData.PropItem);

            RefreshProp(client);
            return TalentResultType.Success;
        }

        /// <summary>
        /// 效果升级
        /// </summary>
        /// <param name="client"></param>
        /// <param name="effectID">效果id</param>
        /// <returns></returns>
        private static int TalentAddEffect(GameClient client, int effectID, int addCount)
        {
            //开放
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.Talent))
                return TalentResultType.EnoOpen;

            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return TalentResultType.EnoOpen;
            }

            //配置信息
            TalentInfo talentInfo = GetTalentInfoByID(client.ClientData.Occupation, effectID);
            if (talentInfo == null) return TalentResultType.EnoEffect;

            TalentData talentData = client.ClientData.MyTalentData;

            //天赋点数不足
            int talentCountLeft = talentData.TotalCount - GetTalentUseCount(talentData);
            if (talentCountLeft < addCount) return TalentResultType.EnoTalentCount;

            //前置效果开启
            if (!IsEffectOpen(talentData, talentInfo.NeedTalentID, talentInfo.NeedTalentLevel))
                return TalentResultType.EnoOpenPreEffect;

            //前置开启点数
            if (talentInfo.NeedTalentCount>0 && talentInfo.NeedTalentCount > talentData.CountList[talentInfo.Type])
                return TalentResultType.EnoOpenPreCount;

            int newLevel = 0;
            List<TalentEffectInfo> newItemEffectList = null;

            //当前等级
            TalentEffectItem effectItemOld = GetOpenEffectItem(talentData, effectID);
            if (effectItemOld != null)
            {
                //最高等级
                if (effectItemOld.Level >= talentInfo.LevelMax)
                    return TalentResultType.EisMaxLevel;

                newLevel = effectItemOld.Level;
            }

            newLevel += addCount;
            newItemEffectList = talentInfo.EffectList[newLevel];

            //是否溢出
            if (newLevel > talentInfo.LevelMax)
                return TalentResultType.EisMaxLevel;

            //更新数据库
            bool result = DBTalentEffectModify(client.ClientData.RoleID, talentInfo.Type, effectID, newLevel, client.ClientData.ZoneID, client.ServerId);
            if (!result) return TalentResultType.EFail;

            //更新内存
            talentData.CountList[talentInfo.Type] += addCount;

            if (effectItemOld == null)
            {
                effectItemOld = new TalentEffectItem();
                effectItemOld.ID = effectID;
                effectItemOld.TalentType = talentInfo.Type;

                talentData.EffectList.Add(effectItemOld);
            }

            effectItemOld.Level = newLevel;
            effectItemOld.ItemEffectList = newItemEffectList;

            //属性加成
            initTalentEffectProp(client);

            //属性刷新
            RefreshProp(client);

            return TalentResultType.Success;
        }

        /// <summary>
        /// 效果是否开启
        /// </summary>
        private static bool IsEffectOpen(TalentData talentData, int effectID, int level)
        {
            if (effectID <= 0)
                return true;

            TalentEffectItem item = GetOpenEffectItem(talentData, effectID);
            if (item != null)
            {
                if (item.Level >= level)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获得已开启效果
        /// </summary>
        private static TalentEffectItem GetOpenEffectItem(TalentData talentData, int effectID)
        {
            if (effectID <= 0) return null;

            foreach (TalentEffectItem item in talentData.EffectList)
            {
                if (item.ID == effectID)
                    return item;
            }

            return null;
        }

        /// <summary>
        /// 获得已经使用的天赋点数
        /// </summary>
        private static int GetTalentUseCount(TalentData talentData)
        {
            if (talentData.CountList == null || talentData.CountList.Count <= 0)
                return 0;

            return talentData.CountList[(int)TalentType.Tough] + talentData.CountList[(int)TalentType.Savage] + talentData.CountList[(int)TalentType.Quick];
        }

        /// <summary>
        /// 初始天赋效果属性
        /// </summary>
        public static void initTalentEffectProp(GameClient client)
        {
            TalentData myTalentData = GetTalentData(client);
            if (myTalentData == null || !myTalentData.IsOpen)
                return;
            
            TalentPropData myPropData = client.ClientData.MyTalentPropData;
            myPropData.ResetProps();

            foreach (TalentEffectItem item in myTalentData.EffectList)
            {
                TalentInfo talentInfo = GetTalentInfoByID(client.ClientData.Occupation, item.ID);
                if (talentInfo.LevelMax < item.Level) continue;

                item.ItemEffectList = talentInfo.EffectList[item.Level];

                //计算效果值
                foreach (TalentEffectInfo info in item.ItemEffectList)
                {
                    switch (info.EffectType)
                    {
                        case (int)TalentEffectType.PropBasic:
                            myPropData.PropItem.BaseProps[info.EffectID] += (int)info.EffectValue;
                            break;
                        case (int)TalentEffectType.PropExt:
                            myPropData.PropItem.ExtProps[info.EffectID] += info.EffectValue;
                            break;
                        case (int)TalentEffectType.SkillOne:
                            {
                                if (myPropData.SkillOneValue.ContainsKey(info.EffectID))
                                    myPropData.SkillOneValue[info.EffectID] += (int)info.EffectValue;
                                else
                                    myPropData.SkillOneValue.Add(info.EffectID, (int)info.EffectValue);
                            }
                            break;
                        case (int)TalentEffectType.SkillAll:
                            myPropData.SkillAllValue += (int)info.EffectValue;
                            break;
                    }//switch
                }//foreach 计算效果值
            }

            InitSpecialProp(client);

            client.ClientData.MyTalentData.SkillOneValue = client.ClientData.MyTalentPropData.SkillOneValue;
            client.ClientData.MyTalentData.SkillAllValue = client.ClientData.MyTalentPropData.SkillAllValue;

            SetTalentProp(client, TalentEffectType.PropBasic, myPropData.PropItem);
            SetTalentProp(client, TalentEffectType.PropExt, myPropData.PropItem);
        }

        /// <summary>
        /// 初始天赋附加属性
        /// </summary>
        private static void InitSpecialProp(GameClient client)
        {
            TalentData talentData = client.ClientData.MyTalentData;
            if (talentData.CountList == null || talentData.CountList.Count <= 0)
                return;

            foreach (var c in talentData.CountList)
            {
                int type = c.Key;
                int value = c.Value;

                TalentSpecialInfo specialInfo = _TalentSpecialList[type];
                int count = value / specialInfo.SingleCount;
                foreach (var item in specialInfo.EffectList)
                {
                    int effectType = item.Key;
                    double effectValue = item.Value * count;

                    client.ClientData.MyTalentPropData.PropItem.ExtProps[effectType] += effectValue;
                }
            }
        }
       
        /// <summary>
        /// 设置属性
        /// </summary>
        private static void SetTalentProp(GameClient client, TalentEffectType type ,EquipPropItem item)
        {
            switch (type)
            {
                case TalentEffectType.PropBasic:
                    client.ClientData.PropsCacheManager.SetBaseProps((int)PropsSystemTypes.Talent, (int)type, item.BaseProps);
                    break;
                case TalentEffectType.PropExt:
                    client.ClientData.PropsCacheManager.SetExtProps((int)PropsSystemTypes.Talent, (int)type, item.ExtProps);
                    break;
            }  
        }

        /// <summary>
        /// 刷新属性
        /// </summary>
        public static void RefreshProp(GameClient client)
        {
            client.delayExecModule.SetDelayExecProc(DelayExecProcIds.RecalcProps, DelayExecProcIds.NotifyRefreshProps);
        }

        /// <summary>
        /// 获得天赋增加的技能等级
        /// </summary>
        public static int GetSkillLevel(GameClient client, int skillID)
        {
            int level = 0;
            //开放
            //if (GlobalNew.IsGongNengOpened(client, GongNengIDs.Talent))
            if (client.ClientData.MyTalentData.IsOpen)
            {
                TalentPropData talentData = client.ClientData.MyTalentPropData;

                if (talentData.SkillOneValue.ContainsKey(skillID))
                {
                    level += talentData.SkillOneValue[skillID];
                }
                else
                {
                    SystemXmlItem systemMagic = null;
                    if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(skillID, out systemMagic))
                        return level;

                    int nParentMagicID = systemMagic.GetIntValue("ParentMagicID");
                    if (nParentMagicID > 0)
                    {
                        SkillData ParentSkillData = Global.GetSkillDataByID(client, nParentMagicID);
                        if (null != ParentSkillData)
                        {
                            if (talentData.SkillOneValue.ContainsKey(ParentSkillData.SkillID))
                                level += talentData.SkillOneValue[ParentSkillData.SkillID];
                        }
                    }
                }

                level += talentData.SkillAllValue;
            }

            return level;
        }

        #endregion

        #region ----------配置

        #region 注入经验

        /// <summary>
        /// 注入经验配置信息
        /// </summary>
        private static Dictionary<int, TalentExpInfo> _TalentExpList = new Dictionary<int, TalentExpInfo>();

        private static void LoadTalentExpInfo()
        {
            string fileName = Global.GameResPath("Config/TianFuDian.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _TalentExpList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    TalentExpInfo info = new TalentExpInfo();
                    info.ID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "TianFuDian", "0"));
                    info.Exp = Convert.ToInt64(Global.GetDefAttributeStr(xmlItem, "NeedExp", "0"));

                    string[] level = Global.GetDefAttributeStr(xmlItem, "NeedLevel", "0,0").Split(',');
                    info.RoleLevel = int.Parse(level[0]) * 100 + int.Parse(level[1]);

                    _TalentExpList.Add(info.ID, info);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }
        }

        #endregion

        #region 洗点

        /// <summary>
        /// 洗点——消耗钻石
        /// </summary>
        /// <param name="count">洗点数量</param>
        private static int GetWashDiamond(int count)
        {
            int[] diamondList = GameManager.systemParamsList.GetParamValueIntArrayByName("ResettingTianFuCostZuanShi");
            return Math.Min(count * diamondList[0], diamondList[1]);
        }

        /// <summary>
        /// 洗点——消耗洗点卡
        /// </summary>
        private static void GetWashGoods(out int goodsID, out int goodsCount)
        {
            int[] arr = GameManager.systemParamsList.GetParamValueIntArrayByName("ResettingTianFuCostGoods");
            goodsID = arr[0];
            goodsCount = arr[1];
        }

        #endregion

        #region 额外属性

        /// <summary>
        /// 额外配置信息
        /// </summary>
        private static Dictionary<int, TalentSpecialInfo> _TalentSpecialList = new Dictionary<int, TalentSpecialInfo>();

        private static void LoadTalentSpecialData()
        {
            string fileName = Global.GameResPath("Config/TianFuGroupProperty.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _TalentSpecialList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    TalentSpecialInfo config = new TalentSpecialInfo();
                    config.SpecialType = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "TianFuType", "0"));
                    config.SingleCount = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedExp", "0"));

                    config.EffectList = new Dictionary<int, double>();
                    double value = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "TripleAttack", "0"));
                    config.EffectList.Add((int)ExtPropIndexes.SavagePercent, value);

                    value = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "SlowAttack", "0"));
                    config.EffectList.Add((int)ExtPropIndexes.ColdPercent, value);

                    value = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "VampiricAttack", "0"));
                    config.EffectList.Add((int)ExtPropIndexes.RuthlessPercent, value);

                    value = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "TripleDefense", "0"));
                    config.EffectList.Add((int)ExtPropIndexes.DeSavagePercent, value);

                    value = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "SlowDefensee", "0"));
                    config.EffectList.Add((int)ExtPropIndexes.DeColdPercent, value);

                    value = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "VampiricDefense", "0"));
                    config.EffectList.Add((int)ExtPropIndexes.DeRuthlessPercent, value);

                    _TalentSpecialList.Add(config.SpecialType, config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }
        }

        #endregion

        #region 天赋效果

        /// <summary>
        /// 天赋配置信息(职业，《id，info》)
        /// </summary>
        private static Dictionary<int, Dictionary<int, TalentInfo>> _TalentInfoList = new Dictionary<int, Dictionary<int, TalentInfo>>();

        private static void LoadTalentInfoData()
        {
            _TalentInfoList.Clear();
   
            for (int i = 0; i < (int)EOccupationType.EOT_MAX; i++)
            {
                string fileName = Global.GameResPath(string.Format("Config/TianFuProperty_{0}.xml", i));
                XElement xml = CheckHelper.LoadXml(fileName);
                if (null == xml) return;

                Dictionary<int, TalentInfo> list = new Dictionary<int, TalentInfo>();
                try
                {
                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (xmlItem == null) continue;

                        TalentInfo config = new TalentInfo();
                        config.ID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                        config.Type = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "TianFuType", "0"));
                        config.Name = Global.GetDefAttributeStr(xmlItem, "Name", "");
                        config.NeedTalentCount = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedInputPoint", "0"));
                        config.NeedTalentID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedTianFu", "0"));
                        config.NeedTalentLevel = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedTianFuLevel", "0"));                      
                        config.LevelMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "LevelMax", "0"));
                        config.EffectType = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "EffectType", "0"));

                        config.EffectList = new Dictionary<int, List<TalentEffectInfo>>();
                        string effect = Global.GetDefAttributeStr(xmlItem, "Effect1", "");
                        XmlGetTalentEffect(config, 1, effect);

                        effect = Global.GetDefAttributeStr(xmlItem, "Effect2", "");
                        XmlGetTalentEffect(config, 2, effect);

                        effect = Global.GetDefAttributeStr(xmlItem, "Effect3", "");
                        XmlGetTalentEffect(config, 3, effect);

                        effect = Global.GetDefAttributeStr(xmlItem, "Effect4", "");
                        XmlGetTalentEffect(config, 4, effect);

                        effect = Global.GetDefAttributeStr(xmlItem, "Effect5", "");
                        XmlGetTalentEffect(config, 5, effect);

                        list.Add(config.ID, config);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!{1}", fileName,ex.Message));
                }

                _TalentInfoList.Add(i, list);
            }
        }

        /// <summary>
        /// 解析效果数据
        /// </summary>
        private static void XmlGetTalentEffect(TalentInfo talentInfo, int level, string strEffect)
        {
            if (string.IsNullOrEmpty(strEffect))
                return;

            string[] arrEffect = strEffect.Split('|');
            List<TalentEffectInfo> list = new List<TalentEffectInfo>();

            foreach (string effect in arrEffect)
            {
                string[] arr = effect.Split(',');
                TalentEffectInfo info = new TalentEffectInfo();
                info.EffectType = talentInfo.EffectType;
                switch (info.EffectType)
                {
                    case (int)TalentEffectType.PropBasic:
                        info.EffectID = (int)Enum.Parse(typeof(UnitPropIndexes), arr[0]);
                        info.EffectValue = double.Parse(arr[1]);
                        break;
                    case (int)TalentEffectType.PropExt:
                        info.EffectID = (int)Enum.Parse(typeof(ExtPropIndexes), arr[0]);
                        info.EffectValue = double.Parse(arr[1]);
                        break;
                    case (int)TalentEffectType.SkillOne:
                    case (int)TalentEffectType.SkillAll:
                        info.EffectID = int.Parse(arr[1]);//技能id，职业类型
                        info.EffectValue = double.Parse(arr[2]);
                        break;
                }

                list.Add(info);
            }

            talentInfo.EffectList.Add(level, list);
        }

        /// <summary>
        /// 根据天赋id，获取天赋基本信息
        /// </summary>
        /// <param name="id">天赋id</param>
        private static TalentInfo GetTalentInfoByID(int type, int id)
        {
            if (type >= (int)EOccupationType.EOT_MAX || type < 0)
                return null;

            Dictionary<int, TalentInfo> list = _TalentInfoList[type];
            if (list.ContainsKey(id))
                return list[id];

            return null;
        }

        #endregion

        #endregion

        #region ----------数据库

        /// <summary>
        /// 天赋——基本数据更新
        /// </summary>
        /// <param name="roleID">角色id</param>
        /// <param name="totalCount">开启的天赋总点数</param>
        /// <param name="exp">当前点数注入经验</param>
        /// <returns></returns>
        private static bool DBTalentModify(int roleID, int totalCount, long exp, long expAdd, bool isUp, int zoneID,int serverID)
        {
            bool result = false;
            int up = isUp?1:0;

            string cmd2db = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", roleID, totalCount, exp, expAdd, up, zoneID);
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_TALENT_MODIFY, cmd2db, serverID);
            if (null != dbFields && dbFields.Length == 1)
                result = (dbFields[0] == ((int)TCPProcessCmdResults.RESULT_OK).ToString());

            return result;
        }

        /// <summary>
        /// 天赋——效果数据更新
        /// </summary>
        /// <param name="roleID">角色id</param>
        /// <param name="talentType">天赋类型</param>
        /// <param name="effectID">效果id</param>
        /// <param name="effectLevel">效果等级</param>
        /// <returns></returns>
        private static bool DBTalentEffectModify(int roleID, int talentType, int effectID, int effectLevel, int zoneID, int serverID)
        {
            bool result = false;

            string cmd2db = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, talentType, effectID, effectLevel, zoneID);
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_TALENT_EFFECT_MODIFY, cmd2db, serverID);
            if (null != dbFields && dbFields.Length == 1)
                result = (dbFields[0] == ((int)TCPProcessCmdResults.RESULT_OK).ToString());

            return result;
        }

        /// <summary>
        /// 天赋——效果清除
        /// </summary>
        /// <param name="roleID">角色id</param>
        /// <returns></returns>
        private static bool DBTalentEffectClear(int roleID, int zoneID, int serverID)
        {
            bool result = false;

            string cmd2db = string.Format("{0}:{1}", roleID, zoneID);
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_TALENT_EFFECT_CLEAR, cmd2db, serverID);
            if (null != dbFields && dbFields.Length == 1)
                result = (dbFields[0] == ((int)TCPProcessCmdResults.RESULT_OK).ToString());

            return result;
        }

        #endregion

        #region GM

        public static bool TalentAddCount(GameClient client, int count)
        {
            TalentData talentData = client.ClientData.MyTalentData;
            if (!talentData.IsOpen)
                return false;

            if (!DBTalentModify(client.ClientData.RoleID, count, 0,0,false,client.ClientData.ZoneID,client.ServerId))
                return false;

            talentData.TotalCount = count;

            return true;
        }

        #endregion

    }
}
