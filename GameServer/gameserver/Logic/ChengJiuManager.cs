#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Server.Tools;
using GameServer.Server;
using GameServer.Server.CmdProcesser;
using System.Xml.Linq;
using GameServer.Core.Executor;
using GameServer.Logic.ActivityNew.SevenDay;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace GameServer.Logic
{
    /// <summary>
    /// 成就管理类
    /// </summary>
    public class ChengJiuManager : IManager
    {
         public const String EncodingLatin1 = "latin1"; //拉丁1编码

        /// <summary>
        /// 标志索引，key是成就ID，Value是成就完成与否标志索引，Value+1是成就奖励是否领取标志
        /// </summary>
        private static Dictionary<int, int> _DictFlagIndex = new Dictionary<int, int>();

        /// <summary>
        /// 成就符文基本信息
        /// </summary>
        private static Dictionary<int, AchievementRuneBasicData> _achievementRuneBasicList = new Dictionary<int, AchievementRuneBasicData>();

        /// <summary>
        /// 成就符文额外信息
        /// </summary>
        private static Dictionary<int, AchievementRuneSpecialData> _achievementRuneSpecialList = new Dictionary<int, AchievementRuneSpecialData>();

        /// <summary>
        /// 成就符文增加属性系数
        /// </summary>
        private static int _runeRate = 1;

        private static ChengJiuManager Instance = new ChengJiuManager();
        public static ChengJiuManager GetInstance()
        {
            return Instance;
        }
        public bool initialize()
        {
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_UPGRADE_CHENGJIU, 2, UpGradeChengLevelCmdProcessor.getInstance(TCPGameServerCmds.CMD_SPR_UPGRADE_CHENGJIU));
            return true;
        }
        public bool startup()
        {
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

        #region 初始化配置

        /// <summary>
        /// 为成就项配置类型"Type"字段,不要每次添加新的成就时,要程序改这些常量
        /// </summary>
        public static void InitChengJiuConfig()
        {
            foreach (var kv in GameManager.systemChengJiu.SystemXmlItemDict)
            {
                int type = kv.Value.GetIntValue("ID");
                switch (type)
                {
                    case ChengJiuTypes.Task:
                        {
                            int chengJiuID = kv.Value.GetIntValue("ChengJiuID");
                            if (chengJiuID > ChengJiuTypes.MainLineTaskEnd)
                            {
                                ChengJiuTypes.MainLineTaskEnd = kv.Key;
                            }
                            else if (chengJiuID < ChengJiuTypes.MainLineTaskStart)
                            {
                                ChengJiuTypes.MainLineTaskStart = kv.Key;
                            }
                        }
                        break;
                }
            }
        }

        #endregion 初始化配置

        #region 成就GM相关

        /// <summary>
        /// 成就——设置等级
        /// </summary>
        /// <param name="client"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static void SetAchievementLevel(GameClient client, int level)
        {
            //更新BufferData
            double[] actionParams = new double[1];
            actionParams[0] = (double)level - 1;
            Global.UpdateBufferData(client, BufferItemTypes.ChengJiu, actionParams, 0);

            //更新成就
            client.ClientData.ChengJiuLevel = level;
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "成就等级", "GM", "系统", client.ClientData.RoleName, "修改", level, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId);
            ChengJiuManager.SetChengJiuLevel(client, client.ClientData.ChengJiuLevel, true);

            Global.BroadcastClientChuanQiChengJiu(client, level);
            //通知自己
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ChengJiuLevel, client.ClientData.ChengJiuLevel);
        }

        /// <summary>
        /// 成就符文——设置等级
        /// </summary>
        /// <param name="client"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static void SetAchievementRuneLevel(GameClient client, int level)
        {
            AchievementRuneData achievementRuneData = new AchievementRuneData();

            AchievementRuneBasicData basic = GetAchievementRuneBasicDataByID(level);
            achievementRuneData.RoleID = client.ClientData.RoleID;
            achievementRuneData.RuneID = basic.RuneID;
            if (achievementRuneData.RuneID > _achievementRuneBasicList.Count)
                achievementRuneData.UpResultType = 3;

            ModifyAchievementRuneData(client, achievementRuneData, true);
            client.ClientData.achievementRuneData = achievementRuneData;

            SetAchievementRuneProps(client, achievementRuneData);
        }

        /// <summary>
        /// 成就符文——设置当天升级次数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static void SetAchievementRuneCount(GameClient client, int count)
        {
            ModifyAchievementRuneUpCount(client, count, true);
        }

        /// <summary>
        /// 成就符文——设置属性增加系数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="rate"></param>
        public static void SetAchievementRuneRate(GameClient client, int rate)
        {
            _runeRate = rate;
        } 
        

        #endregion


        #region 成就符文相关

        /// <summary>
        /// 成就符文状态
        /// </summary>
        private enum AchievementRuneResultType
        {
            End = 3,            //提升达到极限
            Next = 2,           //成功，开启下一个
            Success = 1,        //成功，未生效 
            Efail = 0,           //失败
            EnoOpen = -1,       //未开放
            EnoAchievement = -2,//成就不足
            EnoDiamond = -3,    //钻石不足
            EOver = -4,         //全部开启
        };

        /// <summary>
        /// 成就符文基本信息初始化
        /// </summary>
        public static void initAchievementRune()
        {
            LoadAchievementRuneBasicData();
            LoadAchievementRuneSpecialData();
        }

        public static void initSetAchievementRuneProps(GameClient client)
        {
            //开放等级  成就4阶
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.AchievementRune))
                return;

            AchievementRuneData achievementRuneData = GetAchievementRuneData(client);
            SetAchievementRuneProps(client, achievementRuneData);
        }

        /// <summary>
        /// 加载成就符文基本信息
        /// </summary>
        public static void LoadAchievementRuneBasicData()
        {
            string fileName = "Config/ChengJiuFuWen.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));

            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ChengJiuFuWen.xml时出错!!!文件不存在");
                return;
            }

            try
            {
                _achievementRuneBasicList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    AchievementRuneBasicData config = new AchievementRuneBasicData();
                    config.RuneID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    config.RuneName = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "Name", ""));
                    config.LifeMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "LifeV", "0"));
                    config.AttackMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "AddAttack", "0"));
                    config.DefenseMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "AddDefense", "0"));
                    config.DodgeMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Dodge", "0"));
                    config.AchievementCost = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "CostChengJiu", "0"));

                    string addString = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "QiangHua", ""));
                    if (addString.Length > 0)
                    {
                        config.RateList = new List<int>();
                        config.AddNumList = new List<int[]>();

                        string[] addArr = addString.Split('|');
                        foreach (string str in addArr)
                        {
                            string[] oneArr = str.Split(',');

                            float rate = float.Parse(oneArr[0]);
                            config.RateList.Add((int)(rate * 100));

                            List<int> numList = new List<int>();
                            for (int i = 1; i < oneArr.Length; i++)
                            {
                                numList.Add(int.Parse(oneArr[i]));
                            }

                            config.AddNumList.Add(numList.ToArray());
                        }
                    }

                    _achievementRuneBasicList.Add(config.RuneID,config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ChengJiuFuWen.xml时文件出现异常!!!", ex);
            }
        }

        /// <summary>
        /// 加载成就符文额外信息
        /// </summary>
        public static void LoadAchievementRuneSpecialData()
        {
            string fileName = "Config/ChengJiuSpecialAttribute.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));

            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ChengJiuSpecialAttribute.xml时出错!!!文件不存在");
                return;
            }

            try
            {
                _achievementRuneSpecialList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    AchievementRuneSpecialData config = new AchievementRuneSpecialData();
                    config.SpecialID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    config.RuneID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedFuWen", "0"));
                    config.ZhuoYue = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "ZhuoYueYiJi", "0"));
                    config.DiKang = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "DiKangZhuoYueYiJi", "0"));
                    _achievementRuneSpecialList.Add(config.RuneID,config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ChengJiuSpecialAttribute.xml时出现异常!!!", ex);
            }
        }

        public static AchievementRuneBasicData GetAchievementRuneBasicDataByID(int id)
        {
            if (_achievementRuneBasicList.ContainsKey(id))
                return _achievementRuneBasicList[id];

            return null;
        }

        public static AchievementRuneSpecialData GetAchievementRuneSpecialDataByID(int id)
        {
            if(_achievementRuneSpecialList.ContainsKey(id))
                return _achievementRuneSpecialList[id];

            return null;
        }

        /// <summary>
        /// 获得今天成就符文提示次数
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetAchievementRuneUpCount(GameClient client)
        {
            int count = 0;
            int dayOld = 0;
            List<int> data = Global.GetRoleParamsIntListFromDB(client, RoleParamName.AchievementRuneUpCount);
            if (data != null && data.Count > 0)
                dayOld = data[0];

            int day = TimeUtil.NowDateTime().DayOfYear;
            if (dayOld == day)
                count = data[1];
            else
                ModifyAchievementRuneUpCount(client, count, true);

            return count;
        }

        /// <summary>
        /// 修改成就符文次数数据
        /// </summary>
        /// <returns></returns>
        public static void ModifyAchievementRuneUpCount(GameClient client, int count, bool writeToDB = false)
        {
            List<int> dataList = new List<int>();
            dataList.AddRange(new int[] {  TimeUtil.NowDateTime().DayOfYear, count });

            Global.SaveRoleParamsIntListToDB(client, dataList, RoleParamName.AchievementRuneUpCount, writeToDB);
        }

        /// <summary>
        /// 成就符文——消耗钻石
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetAchievementRuneDiamond(GameClient client,int upCount)
        {
            int[] diamondList = GameManager.systemParamsList.GetParamValueIntArrayByName("ChengJiuFuWenZuanShi");

            if (upCount >= diamondList.Length)
                upCount = diamondList.Length-1;

            return diamondList[upCount];
        }

        /// <summary>
        /// 返回成就符文数据
        /// </summary>
        /// <returns></returns>
        public static AchievementRuneData GetAchievementRuneData(GameClient client)
        {
            //开放等级  成就4阶
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.AchievementRune))
                return null;
            //return null;
            AchievementRuneData achievementRuneData = client.ClientData.achievementRuneData;
            if (achievementRuneData == null)
            {
                AchievementRuneBasicData basic = null;
                achievementRuneData = new AchievementRuneData();

                List<int> data = Global.GetRoleParamsIntListFromDB(client, RoleParamName.AchievementRune);
                if (data == null || data.Count <= 0)
                {
                    basic = GetAchievementRuneBasicDataByID(1);
                    achievementRuneData.RoleID = client.ClientData.RoleID;
                    achievementRuneData.RuneID = basic.RuneID;

                    ModifyAchievementRuneData(client, achievementRuneData, true);
                }
                else
                {
                    achievementRuneData.RoleID = client.ClientData.RoleID;
                    achievementRuneData.RuneID = data[0];
                    achievementRuneData.LifeAdd = data[1];
                    achievementRuneData.AttackAdd = data[2];
                    achievementRuneData.DefenseAdd = data[3];
                    achievementRuneData.DodgeAdd = data[4];

                    if (achievementRuneData.RuneID > _achievementRuneBasicList.Count)
                    {
                        //achievementRuneData.RuneID = _achievementRuneBasicList.Count;
                        achievementRuneData.UpResultType = (int)AchievementRuneResultType.End;
                        basic = GetAchievementRuneBasicDataByID(_achievementRuneBasicList.Count);
                    }
                    else
                    {
                        basic = GetAchievementRuneBasicDataByID(achievementRuneData.RuneID);
                    }
                }

                achievementRuneData.Diamond = GetAchievementRuneDiamond(client, GetAchievementRuneUpCount(client));
                achievementRuneData.Achievement = basic.AchievementCost;

                client.ClientData.achievementRuneData = achievementRuneData;
            }

            achievementRuneData.AchievementLeft = client.ClientData.ChengJiuPoints;
            if (achievementRuneData.RuneID > _achievementRuneBasicList.Count)
                achievementRuneData.UpResultType = (int)AchievementRuneResultType.End;         

            return achievementRuneData;
        }

        /// <summary>
        /// 修改成就符文数据
        /// </summary>
        /// <returns></returns>
        public static void ModifyAchievementRuneData(GameClient client, AchievementRuneData data, bool writeToDB = false)
        {
            List<int> dataList = new List<int>();
            dataList.AddRange(new int[] { data.RuneID, data.LifeAdd, data.AttackAdd, data.DefenseAdd, data.DodgeAdd });

            Global.SaveRoleParamsIntListToDB(client, dataList, RoleParamName.AchievementRune, writeToDB);
        }

        /// <summary>
        /// 成就符文——提升
        /// </summary>
        /// <param name="client"></param>
        /// <param name="runeID"></param>
        /// <returns></returns>
        public static AchievementRuneData UpAchievementRune(GameClient client, int runeID)
        {
            AchievementRuneData achievementRuneData = client.ClientData.achievementRuneData;
            if (achievementRuneData != null 
                && achievementRuneData.UpResultType == (int)AchievementRuneResultType.End)
               
            {
                achievementRuneData.UpResultType = (int)AchievementRuneResultType.EOver;
                 return achievementRuneData;
            }

            if (achievementRuneData == null || achievementRuneData.RuneID != runeID)
            {
                achievementRuneData.UpResultType = (int)AchievementRuneResultType.Efail;
                return achievementRuneData;
            }

            //开放等级  成就4阶
             bool isOpen = GlobalNew.IsGongNengOpened(client, GongNengIDs.AchievementRune);
             if (!isOpen)
             {
                 achievementRuneData.UpResultType = (int)AchievementRuneResultType.EnoOpen;
                 return achievementRuneData;
             }
             
            int[] addNums = null;
            AchievementRuneBasicData basicRune = GetAchievementRuneBasicDataByID(runeID);

            //成就
            int achievementNow = client.ClientData.ChengJiuPoints;
            if (basicRune.AchievementCost > achievementNow)
            {
                achievementRuneData.UpResultType = (int)AchievementRuneResultType.EnoAchievement;
                return achievementRuneData;
            }

            //钻石
            int upCount = GetAchievementRuneUpCount(client);
            int diamondNeed = GetAchievementRuneDiamond(client, upCount);
            if (diamondNeed > 0 && !GameManager.ClientMgr.SubUserMoney(client, diamondNeed, "成就符文提升"))
            {
                achievementRuneData.UpResultType = (int)AchievementRuneResultType.EnoDiamond;
                return achievementRuneData;
            }

            try
            {
                GameManager.ClientMgr.ModifyChengJiuPointsValue(client, -basicRune.AchievementCost, "成就符文提升");
            }
            catch (Exception)
            {
                achievementRuneData.UpResultType = (int)AchievementRuneResultType.EnoAchievement;
                return achievementRuneData;
            }

            //几率
            int rate = 0;
            int r = Global.GetRandomNumber(0, 100);
            for (int i = 0; i < basicRune.RateList.Count; i++)
            {
                rate += basicRune.RateList[i];
                if (r <= rate)
                {
                    addNums = basicRune.AddNumList[i];
                    achievementRuneData.BurstType = i;//暴击
                    break;
                }
            }

            //加成
            achievementRuneData.LifeAdd += addNums[0] * _runeRate;
            achievementRuneData.LifeAdd = achievementRuneData.LifeAdd > basicRune.LifeMax ? basicRune.LifeMax : achievementRuneData.LifeAdd;

            achievementRuneData.AttackAdd += addNums[1] * _runeRate;
            achievementRuneData.AttackAdd = achievementRuneData.AttackAdd > basicRune.AttackMax ? basicRune.AttackMax : achievementRuneData.AttackAdd;

            achievementRuneData.DefenseAdd += addNums[2] * _runeRate;
            achievementRuneData.DefenseAdd = achievementRuneData.DefenseAdd > basicRune.DefenseMax ? basicRune.DefenseMax : achievementRuneData.DefenseAdd;

            achievementRuneData.DodgeAdd += addNums[3] * _runeRate;
            achievementRuneData.DodgeAdd = achievementRuneData.DodgeAdd > basicRune.DodgeMax ? basicRune.DodgeMax : achievementRuneData.DodgeAdd;

            if (achievementRuneData.LifeAdd < basicRune.LifeMax || achievementRuneData.DefenseAdd < basicRune.DefenseMax ||
                achievementRuneData.AttackAdd < basicRune.AttackMax || achievementRuneData.DodgeAdd < basicRune.DodgeMax)
            {
                achievementRuneData.UpResultType = (int)AchievementRuneResultType.Success;
                achievementRuneData.Achievement = basicRune.AchievementCost;
                achievementRuneData.Diamond = GetAchievementRuneDiamond(client, upCount + 1);
            }
            else
            {
                achievementRuneData.RuneID += 1;
                achievementRuneData.LifeAdd = 0;
                achievementRuneData.AttackAdd = 0;
                achievementRuneData.DefenseAdd = 0;
                achievementRuneData.DodgeAdd = 0;

                achievementRuneData.UpResultType = (int)AchievementRuneResultType.Next;
                if (achievementRuneData.RuneID > _achievementRuneBasicList.Count)
                {
                    achievementRuneData.UpResultType = (int)AchievementRuneResultType.End;
                    achievementRuneData.Achievement = 0;
                    achievementRuneData.Diamond = 0;
                }
                else
                {
                    basicRune = GetAchievementRuneBasicDataByID(achievementRuneData.RuneID);
                    achievementRuneData.Achievement = basicRune.AchievementCost;
                    achievementRuneData.Diamond = GetAchievementRuneDiamond(client, upCount + 1);
                }
            }

            ModifyAchievementRuneUpCount(client, upCount + 1, true);
            ModifyAchievementRuneData(client, achievementRuneData);

            client.ClientData.achievementRuneData = achievementRuneData;

            SetAchievementRuneProps(client, achievementRuneData);

            //通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            achievementRuneData.AchievementLeft = client.ClientData.ChengJiuPoints;
            return achievementRuneData;
        }

        public static void SetAchievementRuneProps(GameClient client, AchievementRuneData achievementRuneData)
        {
            //加成
            int life = achievementRuneData.LifeAdd;
            int attack = achievementRuneData.AttackAdd;
            int defense = achievementRuneData.DefenseAdd;
            int dodge = achievementRuneData.DodgeAdd;
            foreach (AchievementRuneBasicData d in _achievementRuneBasicList.Values)
            {
                if (d.RuneID < achievementRuneData.RuneID)
                {
                    life += d.LifeMax;
                    attack += d.AttackMax;
                    defense += d.DefenseMax;
                    dodge += d.DodgeMax;
                }
            }

            //额外加成
            double zhuoYue = 0;
            double diKang = 0;
            if (achievementRuneData.RuneID > 1)
            {
                AchievementRuneSpecialData d = GetAchievementRuneSpecialDataByID(achievementRuneData.RuneID - 1);
                zhuoYue += d.ZhuoYue;
                diKang += d.DiKang;
            }

            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.AchievementRune, (int)ExtPropIndexes.MaxLifeV, life);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.AchievementRune, (int)ExtPropIndexes.AddAttack, attack);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.AchievementRune, (int)ExtPropIndexes.AddDefense, defense);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.AchievementRune, (int)ExtPropIndexes.Dodge, dodge);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.AchievementRune, (int)ExtPropIndexes.FatalAttack, zhuoYue);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.AchievementRune, (int)ExtPropIndexes.DeFatalAttack, diKang);
        }

        #endregion


        /*成就是否达成和是否领取的结果存储在数据库中，每个成就分别采用2bit表示，91个成就对应182个bit，
         * 数据库存放的时候，采用多个64bit的 无符号long，182bit对应 3个 long，3个long 的内存字节被base64
         * 编码，然后存储，这样，每3个字节会增加一个字节，数据库存储field是60字节，这样有效字节是3/4，就是45
         * 字节，能存储5个long，意味着最大支持320位，160个成就表示以后还可以增加69个成就，其实存放时应该采用
         * 直接采用byte替换long，这样45个字节都能用。采用long的原因是想直接将long的内存字节转换为字符串直接存储，
         * 实际上该方案由于字符串结束符的存在 和 数据库相应字段是字符串 而没法实现。
         */
        #region 角色登录时初始化成就相关数据

        /// <summary>
        /// 初始化成就相关数据
        /// </summary>
        public static void InitRoleChengJiuData(GameClient client)
        {
            client.ClientData.ContinuousDayLoginNum = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.ContinuousDayLogin);
            client.ClientData.TotalDayLoginNum = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.TotalDayLogin);

            client.ClientData.ChengJiuPoints = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.ChengJiuPoints);
            client.ClientData.TotalKilledMonsterNum = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.TotalKilledMonsterNum);

            // 取得成就等级就行
            client.ClientData.ChengJiuLevel = ChengJiuManager.GetChengJiuLevel(client);

            if (client.ClientData.ChengJiuLevel > 0)
            {
                int nNewBufferGoodsIndexID = client.ClientData.ChengJiuLevel;

                double[] actionParams = new double[1];
                actionParams[0] = (double)nNewBufferGoodsIndexID - 1;
                Global.UpdateBufferData(client, BufferItemTypes.ChengJiu, actionParams, 0);
            }

            // 尝试激活新的成就buffer
            // TryToActiveNewChengJiuBuffer(client, false);

            client._IconStateMgr.CheckChengJiuUpLevelState(client);
        }

        #endregion 初始成就相关数据

        #region 成就数据存盘
        
        /// <summary>
        /// 成就数据存盘
        /// </summary>
        public static void SaveRoleChengJiuData(GameClient client)
        {
            //ModifyChengJiuExtraData(killer, (uint)++nKillBoss, ChengJiuExtraDataField.TotalKilledBossNum, true);
        }
        
        #endregion 成就数据存盘

        #region 标志位索引生成 与 获取

        /// <summary>
        /// 初始化标志位索引
        /// </summary>
        public static void InitFlagIndex()
        {
            _DictFlagIndex.Clear();

            // 索引必须手动生成，每一个id对应的索引位置不能变
            int index = 0;

            // 第一次
            for (int n = ChengJiuTypes.FirstKillMonster; n <= ChengJiuTypes.FirstBaiTan; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            // 等级需求成就
            for (int n = ChengJiuTypes.LevelStart; n <= ChengJiuTypes.LevelEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            // 转生等级需求成就
            for (int n = ChengJiuTypes.LevelChengJiuStart; n <= ChengJiuTypes.LevelChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            // 技能升级成就开始 MU新增 [3/30/2014 LiaoWei]
            for (int n = ChengJiuTypes.SkillLevelUpStart; n <= ChengJiuTypes.SkillLevelUpEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            //连续登录成就
            for (int n = ChengJiuTypes.ContinuousLoginChengJiuStart; n <= ChengJiuTypes.ContinuousLoginChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            //累积登录成就
            for (int n = ChengJiuTypes.TotalLoginChengJiuStart; n <= ChengJiuTypes.TotalLoginChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            //铜钱成就开始
            for (int n = ChengJiuTypes.ToQianChengJiuStart; n <= ChengJiuTypes.ToQianChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            //怪物成就开始
            for (int n = ChengJiuTypes.MonsterChengJiuStart; n <= ChengJiuTypes.MonsterChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            //boss成就开始
            for (int n = ChengJiuTypes.BossChengJiuStart; n <= ChengJiuTypes.BossChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            // 副本通关成就
            for (int n = ChengJiuTypes.CompleteCopyMapCountNormalStart; n <= ChengJiuTypes.CompleteCopyMapCountNormalEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            for (int n = ChengJiuTypes.CompleteCopyMapCountHardStart; n <= ChengJiuTypes.CompleteCopyMapCountHardEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            for (int n = ChengJiuTypes.CompleteCopyMapCountDifficltStart; n <= ChengJiuTypes.CompleteCopyMapCountDifficltEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }
            
            //强化成就
            for (int n = ChengJiuTypes.QiangHuaChengJiuStart; n <= ChengJiuTypes.QianHuaChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            //追加成就
            for (int n = ChengJiuTypes.ZhuiJiaChengJiuStart; n <= ChengJiuTypes.ZhuiJiaChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            //合成成就
            for (int n = ChengJiuTypes.HeChengChengJiuStart; n <= ChengJiuTypes.HeChengChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            // 战盟成就 MU新增 [3/30/2014 LiaoWei]
            for (int n = ChengJiuTypes.GuildChengJiuStart; n <= ChengJiuTypes.GuildChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            // 军衔成就开始 MU新增 [3/30/2014 LiaoWei]
            for (int n = ChengJiuTypes.JunXianChengJiuStart; n <= ChengJiuTypes.JunXianChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            // 主线任务成就 MU新增 [8/6/2014 LiaoWei]
            for (int n = ChengJiuTypes.MainLineTaskStart; n <= ChengJiuTypes.MainLineTaskEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            /*//经脉成就
            for (int n = ChengJiuTypes.JingMaiChengJiuStart; n <= ChengJiuTypes.JingMaiChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }

            //武学成就
            for (int n = ChengJiuTypes.WuXueChengJiuStart; n <= ChengJiuTypes.WuXueChengJiuEnd; n++)
            {
                _DictFlagIndex.Add(n, index);
                index += 2;//完成与否 和 是否领取奖励共需要两个标志位
            }*/

            //如果新加成就，必须加在后面!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }

        /// <summary>
        /// 通过成就索引位置返回成就ID
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected static ushort GetChengJiuIDByIndex(int index)
        {
            for (int n = 0; n < _DictFlagIndex.Count; n++)
            {
                if (_DictFlagIndex.ElementAt(n).Value == index)
                {
                    return (ushort)_DictFlagIndex.ElementAt(n).Key;
                }
            }
            return 0;
        }

        /// <summary>
        /// 根据成就id返回是否完成索引 失败返回-1
        /// </summary>
        /// <param name="chengJiuID"></param>
        /// <returns></returns>
        protected static int GetCompletedFlagIndex(int chengJiuID)
        {
            int index = -1;

            if (_DictFlagIndex.TryGetValue(chengJiuID, out index))
            {
                return index;
            }

            return -1;
        }

        /// <summary>
        /// 根据成就id返回是否领取索引 失败返回-1
        /// </summary>
        /// <param name="chengJiuID"></param>
        /// <returns></returns>
        protected static int GetAwardFlagIndex(int chengJiuID)
        {
            int index = -1;

            if (_DictFlagIndex.TryGetValue(chengJiuID, out index))
            {
                return index + 1;
            }

            return -1;
        }

        #endregion 标志位索引生成

        #region 数据库操作 和 成就点数加减 总击杀怪物数量 总日登录次数 总连续登录天数 等处理

        /// <summary>
        /// 修改成就点数的值，modifyValue 可以是正数或者负数,相应的 增量和 减少量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="modifyValue"></param>
        public static void AddChengJiuPoints(GameClient client, string strFrom, int modifyValue = 1, Boolean forceUpdateBuffer = true, bool writeToDB = false)
        {
            GameManager.ClientMgr.ModifyChengJiuPointsValue(client, modifyValue, strFrom, writeToDB);

            // 不需要再自动更新 ChengXiaojun
            //if (forceUpdateBuffer)
            //{
            //    //处理激活buffer 或者取消buffer 功能
            //    TryToActiveNewChengJiuBuffer(client, true);
            //}
        }

        /// <summary>
        /// 更新杀死的怪物数量
        /// </summary>
        /// <param name="client"></param>
        public static void SaveKilledMonsterNumToDB(GameClient client, bool bWriteDB = false)
        {
            //更新到数据库
            ModifyChengJiuExtraData(client, (uint)client.ClientData.TotalKilledMonsterNum, ChengJiuExtraDataField.TotalKilledMonsterNum, bWriteDB);
        }

        /// <summary>
        /// 返回成就额外数据
        /// </summary>
        /// <returns></returns>
        public static uint GetChengJiuExtraDataByField(GameClient client, ChengJiuExtraDataField field)
        {
            List<uint> lsUint = Global.GetRoleParamsUIntListFromDB(client, RoleParamName.ChengJiuExtraData);

            int index = (int)field;

            if (index >= lsUint.Count)
            {
                return 0;
            }

            return lsUint[index];
        }

        /// <summary>
        /// 修改成就额外数据
        /// </summary>
        /// <returns></returns>
        public static void ModifyChengJiuExtraData(GameClient client, UInt32 value, ChengJiuExtraDataField field, bool writeToDB = false)
        {
            List<uint> lsUint = Global.GetRoleParamsUIntListFromDB(client, RoleParamName.ChengJiuExtraData);

            int index = (int)field;

            while (lsUint.Count < (index + 1))
            {
                lsUint.Add(0);
            }

            lsUint[index] = value;

            Global.SaveRoleParamsUintListToDB(client, lsUint, RoleParamName.ChengJiuExtraData, writeToDB);
        }

        /// <summary>
        /// 返回成就等级
        /// </summary>
        /// <returns></returns>
        public static int GetChengJiuLevel(GameClient client)
        {
            int uChengJiuLevel = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ChengJiuLevel);

            return uChengJiuLevel;
        }

        /// <summary>
        /// 设置成就等级
        /// </summary>
        /// <returns></returns>
        public static void SetChengJiuLevel(GameClient client, int value, bool writeToDB = false)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ChengJiuLevel, value, writeToDB);

            //[bing] 刷新客户端活动叹号
            if (client._IconStateMgr.CheckJieRiFanLi(client, ActivityTypes.JieriAchievement)
                || client._IconStateMgr.CheckSpecialActivity(client))
            {
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                client._IconStateMgr.SendIconStateToClient(client);
            }

            // 七日活动
            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.ChengJiuLevel));
        }

        #endregion 数据库操作 和 成就点数加减 总击杀怪物数量 总日登录次数 总连续登录天数 等处理

        #region 成就buffer 相关

        /// <summary>
        /// 成就升级
        /// </summary>
        /// <returns></returns>
        public int upGradeChengJiuBuffer(GameClient player)
        {
            return 1;
        }

        public static bool CanActiveNextChengHao(GameClient client)
        {
            return GameManager.ClientMgr.GetChengJiuPointsValue(client) >= GetUpLevelNeedChengJiuPoint(client);
        }

        /// <summary>
        /// 尝试激活成就buffer,每次成就点变化 和 角色刚刚登录的时候调用
        /// 每次给buffer，都需要判断一下当前可以激活的buffer 和 旧有的buffer是否一致，如果不一致，就删除旧buffer，给新buffer
        /// 并扣除1%, 当前的成就点只有这儿消耗，所以buffer级别一直上升，以后其他地方有消耗，buffer级别可能下降，反正是每天可能会多扣除
        /// 成就点
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int TryToActiveNewChengJiuBuffer(GameClient client, bool notifyPropsChanged, int nChengJiuLevel = -1)
        {
            double dayXiaoHao = 0.0;
            int nMaxBufferGoodsIndexID = GetNewChengJiuBufferGoodsIndexIDAndDayXiaoHao(client, client.ClientData.ChengJiuPoints, out dayXiaoHao);
            int nNewBufferGoodsIndexID = 1;
            if (-1 != nChengJiuLevel)
            {
                // 需要激活上一个成就
                if (client.ClientData.ChengJiuLevel + 1 < nChengJiuLevel)
                {
                    return -2;
                }
            }

            int needChengJiuPoint = GetUpLevelNeedChengJiuPoint(client);
            if (GameManager.ClientMgr.GetChengJiuPointsValue(client) < needChengJiuPoint)
            {
                return -5;
            }

            nNewBufferGoodsIndexID = client.ClientData.ChengJiuLevel + 1;
            if (nNewBufferGoodsIndexID > nMaxBufferGoodsIndexID)
            {
                // 激活的成就等级大于能激活的等级
                return -1;
            }

            int nOldBufferGoodsIndexID = -1;
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.ChengJiu);

            //旧buffer必须进行有效性判断
            if (null != bufferData && !Global.IsBufferDataOver(bufferData))
            {
                nOldBufferGoodsIndexID = (int)bufferData.BufferVal;
            }

            //如果旧的bufferid 和 新的bufferid 一样，都存在或者都为 -1
            //因为buffer是每天给一次，如果旧的buffer 和 新的bufferid 一致
            //则不用再扣除成就点
            if (nOldBufferGoodsIndexID == nNewBufferGoodsIndexID && 0 != client.ClientData.ChengJiuLevel)
            {
                // 成就已经激活
                return -3;
            }

            if (nOldBufferGoodsIndexID >= 0)
            {
                // 要激活的成就比已有成就旧
                if (nNewBufferGoodsIndexID < nOldBufferGoodsIndexID)
                {
                    return -4;
                }
            }

            if (nNewBufferGoodsIndexID >= 0)
            {
                //更新BufferData
                double[] actionParams = new double[1];
                actionParams[0] = (double)nNewBufferGoodsIndexID - 1;
                Global.UpdateBufferData(client, BufferItemTypes.ChengJiu, actionParams, 0);

                GameManager.ClientMgr.ModifyChengJiuPointsValue(client, -needChengJiuPoint, "提升成就等级");
                GameManager.ClientMgr.SetChengJiuLevelValue(client, 1, "提升成就等级", true, true);

                //扣除成就消耗
                //int subChengJiuPoints = (int)(GameManager.ClientMgr.GetChengJiuPointsValue(client) * dayXiaoHao);
                //int subChengJiuPoints = (int)dayXiaoHao;
                //GameManager.ClientMgr.ModifyChengJiuPointsValue(client, -subChengJiuPoints, "激活成就BUFF：" + nNewBufferGoodsIndexID, true, true);

                // 传奇成就播报
                if (client.ClientData.ChengJiuLevel >= Global.ConstBroadcastMinChengJiuLevel)
                {
                    Global.BroadcastClientChuanQiChengJiu(client, nNewBufferGoodsIndexID);
                }
            }

            if (notifyPropsChanged)
            {
                //通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }

            if (client._IconStateMgr.CheckChengJiuUpLevelState(client))
            {
                client._IconStateMgr.SendIconStateToClient(client);
            }

            return 0;
        }

        /// <summary>
        /// 根据成就点数返回 对应的buffer 物品索引id，从0开始,同时返回每日消耗
        /// </summary>
        /// <param name="client"></param>
        /// <param name="chengJiuPoints"></param>
        /// <returns></returns>
        public static int GetNewChengJiuBufferGoodsIndexIDAndDayXiaoHao(GameClient client, int chengJiuPoints, out double dayXiaoHao)
        {
            int nNewBufferGoodsIndexID = -1;
            dayXiaoHao = 0.0;

            //buffer 配置文件中 从前到后，需要的成就点数依次增加
            for (int i = 0; i < GameManager.systemChengJiuBuffer.SystemXmlItemDict.Count; i++)
            {
                SystemXmlItem item = GameManager.systemChengJiuBuffer.SystemXmlItemDict.ElementAt(i).Value;

                int chengJiu = item.GetIntValue("ChengJiu");

                if (chengJiuPoints >= chengJiu)
                {
                    nNewBufferGoodsIndexID = item.GetIntValue("ID");//不断的替换目标bufferid 得到新buffer 对应物品索引
                    dayXiaoHao = item.GetDoubleValue("DayXiaoHao");
                }
            }

            if (nNewBufferGoodsIndexID < 0)
            {
                nNewBufferGoodsIndexID = -1;
            }

            return nNewBufferGoodsIndexID;
        }

        /// <summary>
        /// 升级成就称号需要的成就点
        /// </summary>
        /// <param name="client"></param>
        /// <param name="chengJiuPoints"></param>
        /// <param name="dayXiaoHao"></param>
        /// <returns></returns>
        public static int GetUpLevelNeedChengJiuPoint(GameClient client)
        {
            SystemXmlItem item;
            if (GameManager.systemChengJiuBuffer.SystemXmlItemDict.TryGetValue(client.ClientData.ChengJiuLevel + 1, out item))
            {
                return item.GetIntValue("ChengJiu");
            }

            return int.MaxValue;
        }

        #endregion 成就buffer 相关

        #region 成就完成与否 与 存储判断

        /// <summary>
        /// 通过成就ID提取成就存储位置，index 表示竖线分开的第几项，subIndex 表示某一项中的某一个标志位
        /// 成就id规则，采用成就类型乘以100加上一个子序号
        /// </summary>
        /// <param name="chengJiuID"></param>
        /// <param name="index"></param>
        /// <param name="subIndex"></param>
        /// <returns></returns>
        public static Boolean IsChengJiuCompleted(GameClient client, int chengJiuID)
        {
            return IsFlagIsTrue(client, chengJiuID);

        }

        /// <summary>
        /// 判断成就奖励是否被领取，index 表示竖线分开的第几项，subIndex 表示某一项中的某一个标志位
        /// 成就id规则，采用成就类型乘以100加上一个子序号
        /// </summary>
        /// <param name="chengJiuID"></param>
        /// <param name="index"></param>
        /// <param name="subIndex"></param>
        /// <returns></returns>
        public static Boolean IsChengJiuAwardFetched(GameClient client, int chengJiuID)
        {
            return IsFlagIsTrue(client, chengJiuID, true);
        }

        /// <summary>
        /// 成就完成提示
        /// </summary>
        /// <param name="client"></param>
        /// <param name="chengJiuID"></param>
        public static void OnChengJiuCompleted(GameClient client, int chengJiuID)
        {
            //设置成就完成标志
            UpdateChengJiuFlag(client, chengJiuID);

            // 给奖励 [3/14/2014 LiaoWei]
            ChengJiuManager.GiveChengJiuAward(client, chengJiuID, "完成成就ID：" + chengJiuID);
            
            //刚刚完成的成就
            NotifyClientChengJiuData(client, chengJiuID);

            // 七日活动
            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.CompleteChengJiu));
        }

        /// <summary>
        /// 通知客户端成就数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="justCompletedChengJiu"></param>
        public static void NotifyClientChengJiuData(GameClient client, int justCompletedChengJiu=-1)
        {
            // ****说明**** Begin [3/30/2014 LiaoWei]
            // 成就的一些数据在client.ClientData上有缓存 后期增加的几项(ChengJiuExtraDataField.CompleteNormalCopyMapNum等) 我没有加进去 因为 感觉不是太频繁 压力不大 所以每次触发时 我立刻存盘了 
            // ****说明**** End [3/30/2014 LiaoWei]

            //通知客户端
            ChengJiuData chengJiuData = new ChengJiuData()
            {
                RoleID = client.ClientData.RoleID,
                ChengJiuPoints = client.ClientData.ChengJiuPoints,//(int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.ChengJiuPoints),
                TotalKilledMonsterNum = client.ClientData.TotalKilledMonsterNum,//(int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.TotalKilledMonsterNum),
                TotalLoginNum = client.ClientData.TotalDayLoginNum,//(int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.TotalDayLogin),
                ContinueLoginNum = (int)client.ClientData.ContinuousDayLoginNum,//(int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.ContinuousDayLogin),
                ChengJiuFlags = GetChengJiuInfoArray(client),//16个bit 一组，前14个bit表示id， 后面一次是完成bit 和 奖励bit
                NowCompletedChengJiu = justCompletedChengJiu,
                TotalKilledBossNum = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.TotalKilledBossNum),    // 意义和原来的不一样了 击杀指定的BOSS [3/30/2014 LiaoWei]
                CompleteNormalCopyMapCount = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.CompleteNormalCopyMapNum),
                CompleteHardCopyMapCount = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.CompleteHardCopyMapNum),
                CompleteDifficltCopyMapCount = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.CompleteDifficltCopyMapNum),
                GuildChengJiu = client.ClientData.BangGong,
                JunXianChengJiu = GameManager.ClientMgr.GetShengWangLevelValue(client),
//                 FrogeNum = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.ForgeNum),
//                 AppendNum = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.AppendNum),
//                 MergeData = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.MergeData),
            };

            byte[] bytesData = DataHelper.ObjectToBytes<ChengJiuData>(chengJiuData);

            //通知客户端
            GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_SPR_CHENGJIUDATA);
        }

        /// <summary>
        /// 返回成就信息无符号整数数组，用于传递给客户端，每个成就 14位的成就id， 一位完成标志，1位领取标准
        /// </summary>
        /// <param name="chengJiuString"></param>
        /// <returns></returns>
        protected static List<ushort> GetChengJiuInfoArray(GameClient client)
        {
            //这儿不采用循环判断IsFlagIsTrue, 使用对成就列表进行循环处理，那样运算少

            List<ulong> lsLong = Global.GetRoleParamsUlongListFromDB(client, RoleParamName.ChengJiuFlags);

            //索引位置
            int curIndex = 0;

            List<ushort> lsUshort = new List<ushort>();

            for (int n = 0; n < lsLong.Count; n++)
            {
                ulong uValue = lsLong[n];

                for (int subIndex = 0; subIndex < 64; subIndex += 2)
                {
                    //采用 11 移动
                    ulong flag = (ulong)(((ulong)0x3) << (subIndex));//完成与否 领取与否

                    //得到2bit标志位
                    ushort realFlag = (ushort)((uValue & flag) >> (subIndex));//提取到两个标志位表示的数 到最右边，得到一个数

                    ushort chengJiuID = GetChengJiuIDByIndex(curIndex);

                    //14bit 的chengJiuID
                    ushort preFix = (ushort)(chengJiuID << 2);

                    //14bit成就ID + 2bit标志位
                    ushort chengJiu = (ushort)(preFix | realFlag);

                    lsUshort.Add(chengJiu);

                    curIndex += 2;//注 索引也是2递增的，因为标志位是两个一组，一个是否完成，一个是否领取

                    //System.Diagnostics.Debug.WriteLine(String.Format("{0}--{1}--{2}--{3}", chengJiuID, chengJiu, preFix, realFlag));
                }
            }

            return lsUshort;
        }

        /// <summary>
        /// 给予完成成就的奖励 会进行完成与否 与是否已经领取的判断
        /// </summary>
        /// <param name="client"></param>
        /// <param name="chengJiuID"></param>
        public static int GiveChengJiuAward(GameClient client, int chengJiuID, string strFrom)
        {
            //未完成成就不能领取
            if (!IsChengJiuCompleted(client, chengJiuID))
            {
                return -1;
            }

            //奖励领取过了不能再领
            if (IsChengJiuAwardFetched(client, chengJiuID))
            {
                return -2;
            }

            //设置领取标志位
            UpdateChengJiuFlag(client, chengJiuID, true);

            int bindZuanShi = 0, awardBindMoney = 0, awardChengJiuPoints = 0;

            //读取奖励参数
            SystemXmlItem itemChengJiu = null;
            if (GameManager.systemChengJiu.SystemXmlItemDict.TryGetValue(chengJiuID, out itemChengJiu))
            {
                bindZuanShi = Math.Max(0, itemChengJiu.GetIntValue("BindZuanShi"));
                awardBindMoney = Math.Max(0, itemChengJiu.GetIntValue("BindMoney"));
                awardChengJiuPoints = Math.Max(0, itemChengJiu.GetIntValue("ChengJiu"));
            }

            //奖励绑钻
            if (bindZuanShi > 0)
            {
                GameManager.ClientMgr.AddUserGold(client, bindZuanShi, strFrom);
            }

            //奖励绑定铜钱
            if (awardBindMoney > 0)
            {
                //过滤绑定铜钱奖励
                awardBindMoney = Global.FilterValue(client, awardBindMoney);

                //更新用户的绑定铜钱
                GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, awardBindMoney, "完成成就：" + chengJiuID, false);

                GameManager.SystemServerEvents.AddEvent(string.Format("角色完成成就获取绑定铜钱, roleID={0}({1}), Money={2}, newMoney={3}, chengJiuID={4}", client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Money1, awardBindMoney, chengJiuID), EventLevels.Record);
            }

            //给予成就奖励
            if (awardChengJiuPoints > 0)
            {
                AddChengJiuPoints(client, strFrom, awardChengJiuPoints, true, true);
            }

            return 0;
        }

        #endregion 成就完成与否 与 存储判断

        #region 成就 是否完成 与成就奖励是否领取公共函数部分

        /// <summary>
        /// 判断chengJiuID对应标志位是否是true ，forAward=false 成就是否完成 和 forAward = true成就奖励是否领取
        /// </summary>
        /// <param name="chengJiuHexString"></param>
        /// <param name="chengJiuID"></param>
        /// <returns></returns>
        public static Boolean IsFlagIsTrue(GameClient client, int chengJiuID, Boolean forAward = false)
        {
            //完成标志索引
            int index = GetCompletedFlagIndex(chengJiuID);
            if (index < 0)
            {
                return false;
            }

            if (forAward)
            {
                index++;
            }

            List<ulong> lsLong = Global.GetRoleParamsUlongListFromDB(client, RoleParamName.ChengJiuFlags);

            if (lsLong.Count <= 0)
            {
                return false;
            }

            //根据 index 定位到相应的整数
            int arrPosIndex = index / 64;

            if (arrPosIndex >= lsLong.Count)
            {
                return false;
            }

            //定位到整数内部的某个具体位置
            int subIndex = index % 64;

            UInt64 destLong = lsLong[arrPosIndex];

            //这个flag值比较特殊，这样写意味着 在 8 字节的 64位中处于从最小值开始，根据subIndex增加而增加
            //从64位存储的角度看，设计到大端序列和小端序列，看起来在不同的机器样子不一样
            ulong flag = ((ulong)(1)) << subIndex;

            //进行标志位判断
            bool bResult = (destLong & flag) > 0;

            return bResult;
        }

        /// <summary>
        /// 更新chengJiuID对应的成就项的标准位，并返回修改后的十六进制字符串，用于成就是否完成和成就奖励是否领取的统一处理
        /// </summary>
        /// <param name="chengJiuHexString"></param>
        /// <param name="chengJiuID"></param>
        /// <returns></returns>
        public static bool UpdateChengJiuFlag(GameClient client, int chengJiuID, bool forAward = false)
        {
            //chengJiuString 长度必须是 8 的倍数，一个长整形是 8 字节

            //完成标志索引
            int index = GetCompletedFlagIndex(chengJiuID);
            if (index < 0)
            {
                return false;
            }

            if (forAward)
            {
                index++;
            }

            List<ulong> lsLong = Global.GetRoleParamsUlongListFromDB(client, RoleParamName.ChengJiuFlags);

            //根据 index 定位到相应的整数
            int arrPosIndex = index / 64;

            //填充64位整数
            while (arrPosIndex > lsLong.Count -1)
            {
                lsLong.Add(0);
            }

            //定位到整数内部的某个具体位置
            int subIndex = index % 64;

            ulong destLong = lsLong[arrPosIndex];

            //这个flag值比较特殊，这样写意味着 在 8 字节的 64位中处于从最小值开始，根据subIndex增加而增加
            //从64位存储的角度看，设计到大端序列和小端序列，看起来在不同的机器样子不一样
            ulong flag = ((ulong)(1)) << subIndex;

            //设置标志位 为 1
            lsLong[arrPosIndex] = destLong | flag;

            //存储到数据库
            Global.SaveRoleParamsUlongListToDB(client, lsLong, RoleParamName.ChengJiuFlags, true);

            return true;
        }

        #endregion 成就 是否完成 与成就奖励是否领取公共函数部分

        #region 第一次相关成就触发处理

        /// <summary>
        /// 第一次杀怪
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstKillMonster(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstKillMonster))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstKillMonster);
            }
        }

        /// <summary>
        /// 第一次添加好友
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstAddFriend(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstAddFriend))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstAddFriend);
            }
        }

        /// <summary>
        /// 第一次入会
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstInFaction(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstInFaction))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstInFaction);
            }
        }

        /// <summary>
        /// 第一次组队
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstInTeam(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstInTeam))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstInTeam);
            }
        }

        /// <summary>
        /// 第一次合成
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstHeCheng(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstHeCheng))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstHeCheng);
            }
        }

        /// <summary>
        /// 第一次强化
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstQiangHua(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstQiangHua))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstQiangHua);
            }
        }

        /// <summary>
        /// 第一次追加
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstAppend(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstZhuiJia))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstZhuiJia);
            }
        }

        /// <summary>
        /// 第一次继承
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstJiCheng(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstJiCheng))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstJiCheng);
            }
        }

        /// <summary>
        /// 第一次摆摊
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstBaiTan(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstBaiTan))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstBaiTan);
            }
        }
        /*/// <summary>
        /// 第一次洗练
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstXiLian(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstXiLian))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstXiLian);
            }
        }

        /// <summary>
        /// 第一次炼化
        /// </summary>
        /// <param name="client"></param>
        public static void OnFirstLianHua(GameClient client)
        {
            if (!IsChengJiuCompleted(client, ChengJiuTypes.FirstLianHua))
            {
                OnChengJiuCompleted(client, ChengJiuTypes.FirstLianHua);
            }
        }*/

        #endregion 第一次相关成就触发管理

        #region 怪物相关成就触发处理---浴血沙场

        /// <summary>
        /// 怪物被杀死事件，进行怪物相关成就判断-->外部怪物被杀死时调用
        /// </summary>
        /// <param name="killer"></param>
        /// <param name="victim"></param>
        public static void OnMonsterKilled(GameClient killer, Monster victim)
        {
            //初始化 和 判断第一次杀怪成就
            if (0 == killer.ClientData.TotalKilledMonsterNum)
            {
                killer.ClientData.TotalKilledMonsterNum = GetChengJiuExtraDataByField(killer, ChengJiuExtraDataField.TotalKilledMonsterNum);

                //触发
                if (0 == killer.ClientData.TotalKilledMonsterNum)
                {
                    OnFirstKillMonster(killer);//触发杀死第一只怪物
                }
            }

            //如果杀怪数量超过一定数量，需要定期更新到数据库,登出时再更新一次,调用很频繁，需要定期调用
            killer.ClientData.TotalKilledMonsterNum++;
            killer.ClientData.TimerKilledMonsterNum++;

            //怪物击杀量增加200个，更新一次数据库
            bool bWriteDB = false;
            
            if (killer.ClientData.ChangeLifeCount == 0)
            {
                if (killer.ClientData.TimerKilledMonsterNum > 200)
                    bWriteDB = true;
            }
            else
            {
                if (killer.ClientData.TimerKilledMonsterNum > 500)
                    bWriteDB = true;
            }

            if (bWriteDB)
            {
                killer.ClientData.TimerKilledMonsterNum = 0;
                SaveKilledMonsterNumToDB(killer, bWriteDB);
            }
            

            //判断怪物击杀成就
            CheckMonsterChengJiu(killer);

            //如果是BOSS，判断boss成就
            if ((int)MonsterTypes.BOSS == victim.MonsterType)
            {
                // 成就改造 Begin [3/12/2014 LiaoWei]
                
                // 如果最后一个击杀BOSS的成就都完成了..
                if (IsChengJiuCompleted(killer, ChengJiuTypes.BossChengJiuEnd))
                    return;

                // 必须击杀指定的BOSS
                for (int i = 0; i < Data.KillBossCountForChengJiu.Length; ++i)
                {
#if ___CC___FUCK___YOU___BB___
                    if (victim.XMonsterInfo.MonsterId == Data.KillBossCountForChengJiu[i])
                    {
                        int nKillBoss = (int)GetChengJiuExtraDataByField(killer, ChengJiuExtraDataField.TotalKilledBossNum);

                        ModifyChengJiuExtraData(killer, (uint)++nKillBoss, ChengJiuExtraDataField.TotalKilledBossNum, true);
                        
                        CheckBossChengJiu(killer, ++nKillBoss);
                    }
#else
                      if (victim.MonsterInfo.ExtensionID == Data.KillBossCountForChengJiu[i])
                    {
                        int nKillBoss = (int)GetChengJiuExtraDataByField(killer, ChengJiuExtraDataField.TotalKilledBossNum);

                        ModifyChengJiuExtraData(killer, (uint)++nKillBoss, ChengJiuExtraDataField.TotalKilledBossNum, true);
                        
                        CheckBossChengJiu(killer, ++nKillBoss);
                    }
#endif
                }

                // 成就改造 End [3/12/2014 LiaoWei]
            }
        }

        /// <summary>
        /// 检查怪物成就,这儿默认怪物成就对怪物数量要求由少到多
        /// </summary>
        public static void CheckMonsterChengJiu(GameClient client)
        {
            //经过这样过滤判断，怪物杀死数量成就的判断很多时候在这儿直接返回
            if (client.ClientData.TotalKilledMonsterNum < client.ClientData.NextKilledMonsterChengJiuNum
                || 0x7fffffff == client.ClientData.NextKilledMonsterChengJiuNum)
            {
                return;
            }

            uint nextNeedNum = CheckSingleConditionChengJiu(client, ChengJiuTypes.MonsterChengJiuStart, ChengJiuTypes.MonsterChengJiuEnd,
                client.ClientData.TotalKilledMonsterNum, "KillMonster");

            //有成就完成 记录下下次需要检查的数量 这样不需要每次都调用 CheckSingleConditionChengJiu
            client.ClientData.NextKilledMonsterChengJiuNum = nextNeedNum;

            //如果最后一个杀怪成就完成了，以后就不需要判断了
            if (IsChengJiuCompleted(client, ChengJiuTypes.MonsterChengJiuEnd))
            {
                 client.ClientData.NextKilledMonsterChengJiuNum = 0x7fffffff;
            }
        }

        /// <summary>
        /// 检查Boss成就,这儿默认Boss成就对怪物数量要求由少到多
        /// </summary>
        public static void CheckBossChengJiu(GameClient client, int nNum)
        {
            CheckSingleConditionChengJiu(client, ChengJiuTypes.BossChengJiuStart, ChengJiuTypes.BossChengJiuEnd,
                nNum, "KillBoss");

        }

#endregion 怪物相关成就触发处理

#region 财富积累相关成就触发处理

        /// <summary>
        /// 当铜钱增加的时候
        /// </summary>
        /// <param name="client"></param>
        public static void OnTongQianIncrease(GameClient client)
        {
            //初始化
            if (0 > client.ClientData.MaxTongQianNum)
            {
                client.ClientData.MaxTongQianNum = Math.Max(0, Global.GetRoleParamsInt32FromDB(client, RoleParamName.MaxTongQianNum));
            }

            if (client.ClientData.YinLiang < client.ClientData.MaxTongQianNum)
            {
                return;
            }

            client.ClientData.MaxTongQianNum = client.ClientData.YinLiang;
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.MaxTongQianNum, client.ClientData.MaxTongQianNum, false);

            if (client.ClientData.MaxTongQianNum < client.ClientData.NextTongQianChengJiuNum)
            {
                return;
            }

            //铜钱会减少，不用再进行最后一个成就完成与否的判断了， 杀死怪物类型由于怪物死亡数量一直递增，所以需要
            client.ClientData.NextTongQianChengJiuNum = CheckSingleConditionChengJiu(client, ChengJiuTypes.ToQianChengJiuStart, ChengJiuTypes.ToQianChengJiuEnd,
                client.ClientData.MaxTongQianNum, "TongQianLimit");
        }

#endregion 财富积累相关成就触发处理

#region 等级相关成就触发处理

        /// <summary>
        /// 当角色升级的时候
        /// </summary>
        /// <param name="client"></param>
        public static void OnRoleLevelUp(GameClient client)
        {
            // 成就改造 等级变成转生等级 [3/12/2014 LiaoWei]
            CheckSingleConditionChengJiu(client, ChengJiuTypes.LevelStart, ChengJiuTypes.LevelEnd,
                client.ClientData.Level, "LevelLimit");
        }

        /// <summary>
        /// 当角色转生的时候
        /// </summary>
        /// <param name="client"></param>
        public static void OnRoleChangeLife(GameClient client)
        {
            // 成就改造 等级变成转生等级 [3/12/2014 LiaoWei]
            CheckSingleConditionChengJiu(client, ChengJiuTypes.LevelChengJiuStart, ChengJiuTypes.LevelChengJiuEnd,
                client.ClientData.ChangeLifeCount, "ZhuanShengLimit");
        }
#endregion 等级相关成就触发处理

        /*#region 经脉相关成就触发处理

        /// <summary>
        /// 当角色经脉升级的时候
        /// </summary>
        /// <param name="client"></param>
        public static void OnJingMaiLevelUp(GameClient client, int curValue)
        {
            CheckSingleConditionChengJiu(client, ChengJiuTypes.JingMaiChengJiuStart, ChengJiuTypes.JingMaiChengJiuEnd,
                curValue, "JingMai");//具体值要配置
        }

#endregion 经脉相关成就触发处理*/

        /*#region 武学相关成就触发处理

        /// <summary>
        /// 当角色武学升级的时候
        /// </summary>
        /// <param name="client"></param>
        public static void OnWuXueLevelUp(GameClient client, int curValue)
        {
            CheckSingleConditionChengJiu(client, ChengJiuTypes.WuXueChengJiuStart, ChengJiuTypes.WuXueChengJiuEnd,
                curValue, "WuXue");//具体值要配置
        }

#endregion 武学相关成就触发处理*/

#region 登录相关成就触发处理

        /// <summary>
        /// 当角色登录游戏的时候--->每天只会被调用一次
        /// </summary>
        /// <param name="client"></param>
        public static void OnRoleLogin(GameClient client, int preLoginDay)
        {
            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (dayID == preLoginDay)
            {
                return;
            }
            
            // fix a bug [3/20/2014 LiaoWei]
            //client.ClientData.ContinuousDayLoginNum = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.TotalDayLogin);
            //client.ClientData.TotalDayLoginNum = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.ContinuousDayLogin);
            client.ClientData.TotalDayLoginNum      = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.TotalDayLogin);
            client.ClientData.ContinuousDayLoginNum = GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.ContinuousDayLogin);

            client.ClientData.TotalDayLoginNum++;

            DateTime tm = TimeUtil.NowDateTime().AddDays(-1);
            int preDay = tm.DayOfYear;

            if (preDay == preLoginDay)
            {
                //连续登录
                client.ClientData.ContinuousDayLoginNum++;

                //if (client.ClientData.SeriesLoginNum < 7)
                client.ClientData.SeriesLoginNum++;
            }
            else
            {
                //重置
                client.ClientData.ContinuousDayLoginNum = 1;

                client.ClientData.SeriesLoginNum = 1;
            }

            if ("" != client.strUserID)
            {
                // 更新连续登录信息到帐号活跃信息表
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_ACCOUNT_ACTIVE,
                                                string.Format("{0}", client.strUserID),
                                                null, client.ServerId);
            }

            // 更新连续登陆信息
            Global.UpdateSeriesLoginInfo(client);

            //更新到数据库
            ModifyChengJiuExtraData(client, (uint)client.ClientData.TotalDayLoginNum, ChengJiuExtraDataField.TotalDayLogin, true);
            ModifyChengJiuExtraData(client, (uint)client.ClientData.ContinuousDayLoginNum, ChengJiuExtraDataField.ContinuousDayLogin, true);
            
            //连续登录 成就检查
            CheckSingleConditionChengJiu(client, ChengJiuTypes.ContinuousLoginChengJiuStart, ChengJiuTypes.ContinuousLoginChengJiuEnd,
                client.ClientData.ContinuousDayLoginNum, "LoginDayOne");//具体值要配置

            //累积登录 成就检查
            CheckSingleConditionChengJiu(client, ChengJiuTypes.TotalLoginChengJiuStart, ChengJiuTypes.TotalLoginChengJiuEnd,
                client.ClientData.TotalDayLoginNum, "LoginDayTwo");//具体值要配置

            // 清空每日活跃信息 [2/25/2014 LiaoWei]
            //DailyActiveManager.m_DailyActiveDayID = dayID;

            DailyActiveManager.CleanDailyActiveInfo(client);

            // 设置每日活跃信息 [2/25/2014 LiaoWei]
            if (client.ClientData.DailyActiveDayLginSetFlag != true)
            {
                bool bIsCompleted = false;
                DailyActiveManager.ProcessLoginForDailyActive(client, out bIsCompleted);
            }

            client.ClientData.DailyActiveDayLginSetFlag = true;
        }

#endregion 登录相关成就触发处理

#region 强化相关成就触发处理

        /// <summary>
        /// 当角色装备强化的时候
        /// </summary>
        /// <param name="client"></param>
        public static void OnRoleEquipmentQiangHua(GameClient client, int equipStarsNum)
        {
            int nCompletedID = -1;

            // 强化装备
            nCompletedID = CheckEquipmentChengJiu(client, ChengJiuTypes.QiangHuaChengJiuStart, ChengJiuTypes.QianHuaChengJiuEnd, equipStarsNum, "QiangHuaLimit");//具体值要配置

            /*if (nCompletedID != -1)
            {
                int nFlag = -1;
                nFlag = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.ForgeNum);

                if (nFlag != -1 && equipStarsNum > nFlag)
                {
                    ModifyChengJiuExtraData(client, (uint)equipStarsNum, ChengJiuExtraDataField.ForgeNum, true);
                }
            }*/
        }

#endregion 强化相关成就触发处理

#region 追加相关成就触发处理

        /// <summary>
        /// 当角色物品追加的时候
        /// </summary>
        /// <param name="client"></param>
        public static void OnRoleGoodsAppend(GameClient client, int AppendLev)
        {
            int nCompletedID = -1;

            // 合成得到物品的时候判断
            nCompletedID = CheckEquipmentChengJiu(client, ChengJiuTypes.ZhuiJiaChengJiuStart, ChengJiuTypes.ZhuiJiaChengJiuEnd,
                AppendLev, "ZhuiJiaLimit");

            /*if (nCompletedID != -1)
            {
                int nFlag = -1;
                nFlag = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.AppendNum);

                if (nFlag != -1 && AppendLev > nFlag)
                {
                    ModifyChengJiuExtraData(client, (uint)AppendLev, ChengJiuExtraDataField.AppendNum, true);
                }
            }*/
        }

#endregion 追加相关成就触发处理

#region 合成相关成就触发处理

        /// <summary>
        /// 当角色物品合成的时候
        /// </summary>
        /// <param name="client"></param>
        public static void OnRoleGoodsHeCheng(GameClient client, int goodsIDCreated)
        {
            int nCompletedID = -1;

            //合成得到物品的时候判断
            nCompletedID = CheckEquipmentChengJiu(client, ChengJiuTypes.HeChengChengJiuStart, ChengJiuTypes.HeChengChengJiuEnd,
                goodsIDCreated, "HeChengLimit");

            /*if (nCompletedID != -1)
            {
                int nFlag = -1;
                nFlag = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.MergeData);

                if (nFlag != -1 && goodsIDCreated != nFlag)
                {
                    ModifyChengJiuExtraData(client, (uint)goodsIDCreated, ChengJiuExtraDataField.MergeData, true);
                }
            }*/
        }

#endregion 合成相关成就触发处理

#region 副本通关相关成就触发处理 -- 它属于浴血沙场
        /// <summary>
        /// 处理完成副本的成就
        /// </summary>
        /// <returns></returns>
        public static void ProcessCompleteCopyMapForChengJiu(GameClient client, int nCopyMapLev,int count = 1)
        {
            if (IsChengJiuCompleted(client, ChengJiuTypes.CompleteCopyMapCountNormalEnd) && IsChengJiuCompleted(client, ChengJiuTypes.CompleteCopyMapCountHardEnd) &&
                                    IsChengJiuCompleted(client, ChengJiuTypes.CompleteCopyMapCountDifficltEnd))
                return;

            if (nCopyMapLev < 0)
                return;

            int nNum = 0;

            switch (nCopyMapLev)
            {
                case (int)COPYMAPLEVEL.COPYMAPLEVEL_NORMAL:
                    {
                        nNum = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.CompleteNormalCopyMapNum);

                        ++nNum;
                        nNum *= count;
                        ModifyChengJiuExtraData(client, (uint)nNum, ChengJiuExtraDataField.CompleteNormalCopyMapNum, true);

                        CheckSingleConditionChengJiu(client, ChengJiuTypes.CompleteCopyMapCountNormalStart, ChengJiuTypes.CompleteCopyMapCountNormalEnd, nNum, "KillRaid");
                    }
                    break;
                case (int)COPYMAPLEVEL.COPYMAPLEVEL_HARD:
                    {
                        nNum = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.CompleteHardCopyMapNum);

                        ++nNum;
                        nNum *= count;
                        ModifyChengJiuExtraData(client, (uint)nNum, ChengJiuExtraDataField.CompleteHardCopyMapNum, true);

                        CheckSingleConditionChengJiu(client, ChengJiuTypes.CompleteCopyMapCountHardStart, ChengJiuTypes.CompleteCopyMapCountHardEnd, nNum, "KillRaid");
                    }
                    break;
                case (int)COPYMAPLEVEL.COPYMAPLEVEL_DIFFICLT:
                    {
                        nNum = (int)GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.CompleteDifficltCopyMapNum);

                        ++nNum;
                        nNum *= count;
                        ModifyChengJiuExtraData(client, (uint)nNum, ChengJiuExtraDataField.CompleteDifficltCopyMapNum, true);

                        CheckSingleConditionChengJiu(client, ChengJiuTypes.CompleteCopyMapCountDifficltStart, ChengJiuTypes.CompleteCopyMapCountDifficltEnd, nNum, "KillRaid");
                    }
                    break;
                default:
                    break;
            }
        }

#endregion 副本通关相关成就触发处理

#region 技能等级成就
        
        /// <summary>
        /// 处理技能等级成就
        /// </summary>
        /// <returns></returns>
        public static void OnRoleSkillLevelUp(GameClient client)
        {
            if (IsChengJiuCompleted(client, ChengJiuTypes.SkillLevelUpEnd))
                return;

            bool bIsAddDefault = false;
            int nCurrentSkillLev = 0;

            for (int i = 0; i < client.ClientData.SkillDataList.Count; ++i)
            {
                if (client.ClientData.SkillDataList[i].DbID == -1)
                {
                    if (!bIsAddDefault)
                    {
                        nCurrentSkillLev += client.ClientData.SkillDataList[i].SkillLevel;
                        bIsAddDefault = true;
                    }
                }
                else
                    nCurrentSkillLev += client.ClientData.SkillDataList[i].SkillLevel;
            }

            CheckSingleConditionChengJiu(client, ChengJiuTypes.SkillLevelUpStart, ChengJiuTypes.SkillLevelUpEnd,
                nCurrentSkillLev, "SkillLevel");
        }

#endregion 技能等级成就

#region 战盟成就

        /// <summary>
        /// 处理战盟成就
        /// </summary>
        /// <returns></returns>
        public static void OnRoleGuildChengJiu(GameClient client)
        {
            if (IsChengJiuCompleted(client, ChengJiuTypes.GuildChengJiuEnd))
                return;

            CheckSingleConditionChengJiu(client, ChengJiuTypes.GuildChengJiuStart, ChengJiuTypes.GuildChengJiuEnd,
                client.ClientData.BangGong, "ZhanGong");

        }

#endregion 战盟成就

#region 军衔成就

        /// <summary>
        /// 处理军衔成就
        /// </summary>
        /// <returns></returns>
        public static void OnRoleJunXianChengJiu(GameClient client)
        {
            if (IsChengJiuCompleted(client, ChengJiuTypes.JunXianChengJiuEnd))
                return;

            CheckSingleConditionChengJiu(client, ChengJiuTypes.JunXianChengJiuStart, ChengJiuTypes.JunXianChengJiuEnd,
                GameManager.ClientMgr.GetShengWangLevelValue(client), "JunXian");

        }

#endregion 军衔成就

#region 主线任务完成相关成就

        /// <summary>
        /// 处理完成副本的成就
        /// </summary>
        /// <returns></returns>
        public static void ProcessCompleteMainTaskForChengJiu(GameClient client, int nTaskID)
        {
            if (IsChengJiuCompleted(client, ChengJiuTypes.MainLineTaskEnd))
                return;

            if (nTaskID < 0)
                return;

            uint uValue = 0;
            SystemXmlItem itemChengJiu = null;

            for (int chengJiuID = ChengJiuTypes.MainLineTaskStart; chengJiuID <= ChengJiuTypes.MainLineTaskEnd; chengJiuID++)
            {
                if (GameManager.systemChengJiu.SystemXmlItemDict.TryGetValue(chengJiuID, out itemChengJiu))
                {
                    if (null == itemChengJiu) 
                        continue;

                    uValue = (uint)itemChengJiu.GetIntValue("RenWu");

                    if (nTaskID >= uValue)
                    {
                        //如果成就没完成
                        if (!IsChengJiuCompleted(client, chengJiuID))
                        {
                            //触发成就完成事件
                            OnChengJiuCompleted(client, chengJiuID);
                        }
                    }

                }
            }

        }

#endregion 主线任务完成相关成就


#region 单一字段成就检查通用处理--比如boss数量，怪物数量，铜钱数量等

        /// <summary>
        /// 单一条件成就检查，成就的完成条件只有一个，而且，此类别成就对数量要求由少到多，比如boss击杀数量，怪物击杀数量
        /// 玩家金币数量等 返回下一个需要完成目标的数值
        /// </summary>
        protected static uint CheckSingleConditionChengJiu(GameClient client, int chengJiuMinID, int chengJiuMaxID, long roleCurrentValue, String strCheckField)
        {
            SystemXmlItem itemChengJiu = null;

            //完成成就需要的最少目标值
            uint needMinValue = 0;

            for (int chengJiuID = chengJiuMinID; chengJiuID <= chengJiuMaxID; chengJiuID++)
            {
                if (GameManager.systemChengJiu.SystemXmlItemDict.TryGetValue(chengJiuID, out itemChengJiu))
                {
                    if (null == itemChengJiu) continue;

                    needMinValue = (uint)itemChengJiu.GetIntValue(strCheckField);

                    if (roleCurrentValue >= needMinValue)
                    {
                        //如果成就没完成
                        if (!IsChengJiuCompleted(client, chengJiuID))
                        {
                            //触发成就完成事件
                            OnChengJiuCompleted(client, chengJiuID);
                        }
                    }
                    else
                    {
                        break;//连最少的都没完成，直接退出
                    }
                }
            }

            return needMinValue;
        }

#endregion 单一字段成就检查通用处理--比如boss数量，怪物数量，铜钱数量等

#region 强化得到一件两星装备 或者 和成得到 一块强化石的判断类似 做装备做物品的通用判断,做限制，这儿只需要得到一件就ok，不需要判断多件

        /// <summary>
        /// 通过【装备强化】获得1件7星装备 通过【合成】功能 roleCurrentValue 是装备星级，成功合成1块三品强化石 roleCurrentValue 是物品ID,
        /// 每次传递，都表明通过某些操作得到这样星级的装备，或者这样的强化石, strCheckField 对应的字段应该是 EquipBornLimit="" 或 GoodsLimit="35002,1"
        /// </summary>
        protected static int CheckEquipmentChengJiu(GameClient client, int chengJiuMinID, int chengJiuMaxID, long roleCurrentValue, String strCheckField)
        {
            SystemXmlItem itemChengJiu = null;

            int maxCompletedID = -1;

            //完成成就需要的最少目标值
            int needMinValue = 0;

            for (int chengJiuID = chengJiuMinID; chengJiuID <= chengJiuMaxID; chengJiuID++)
            {
                if (GameManager.systemChengJiu.SystemXmlItemDict.TryGetValue(chengJiuID, out itemChengJiu))
                {
                    if (null == itemChengJiu) continue;

                    String[] needMinValueArray = itemChengJiu.GetStringValue(strCheckField).Split(',');

                    if (needMinValueArray.Length != 2) continue;

                    needMinValue = Global.SafeConvertToInt32(needMinValueArray[0]);//多少星， 或者物品ID
                    
                    if (roleCurrentValue == needMinValue)
                    {
                        //这儿，其实还涉及到数量的判断，比如 3星的装备三件， 或者 4品强化石 3个,如果没达到要求，就对成就进度进行累加
                        //考虑到数据储存的复杂度和策划的变通程度，要求needMinNum必须为1，不为1，则需要对成就进度数据进行额外存放，需要更大的空间
                        //同时，成就数据比较分散，涉及时由于空间限制，没有涉及通用的成就进度存储方案
                        int needMinNum = Global.SafeConvertToInt32(needMinValueArray[1]);//多少星， 或者物品ID 的个数

                        if (needMinNum > 1) continue;//大于1个的要求暂时不实现

                        //如果成就没完成
                        if (!IsChengJiuCompleted(client, chengJiuID))
                        {
                            //触发成就完成事件
                            OnChengJiuCompleted(client, chengJiuID);

                            maxCompletedID = chengJiuID;
                        }
                    }
                    else
                    {
                        //break;//这儿不用退出，对于宝石物品id的判断，没有最小值的说法
                    }
                }
            }

            return maxCompletedID;
        }

#endregion 强化得到一件两星装备 或者 和成得到 一块强化石的判断类似 做装备做物品的通用判断

    }
}
