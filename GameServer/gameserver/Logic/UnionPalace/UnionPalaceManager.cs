using GameServer.Core.Executor;
using GameServer.Server;
using GameServer.Tools;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic.UnionPalace
{
    public class UnionPalaceManager : ICmdProcessorEx, IManager
    {
        #region ----------接口

        private const int DEFAULT_MIN_ID = 1;
        private const int UP_LEVEL_MAX = 5;
        private const int STATUE_COUNT = 8;

        private const int UNION_PALACE_MAX_LEVEL = 9;
        private const int STATUE_MAX_LEVEL = 5;

        public static int _gmRate = 1;

        private static UnionPalaceManager instance = new UnionPalaceManager();
        public static UnionPalaceManager getInstance()
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
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_UNION_PALACE_DATA, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_UNION_PALACE_UP, 1, 1, getInstance());

            return true;
        }

        public bool showdown() { return true; }
        public bool destroy() { return true; }
        public bool processCmd(GameClient client, string[] cmdParams) { return true; }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_UNION_PALACE_DATA:
                    return ProcessCmdUnionPalaceData(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_UNION_PALACE_UP:
                    return ProcessCmdUnionPalaceUp(client, nID, bytes, cmdParams);
            }

            return true;
        }

        private bool ProcessCmdUnionPalaceData(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                UnionPalaceData data = UnionPalaceGetData(client);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_UNION_PALACE_DATA, data);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        private bool ProcessCmdUnionPalaceUp(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 1);
                if (!isCheck) return false;

                UnionPalaceData data = UnionPalaceUp(client);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_UNION_PALACE_UP, data);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        #endregion

        #region ----------相关
        //BangHuiDetailData unionData = Global.GetBangHuiDetailData(client.ClientData.RoleID, Global.GetBangHuiLevel(client));
        public static UnionPalaceData UnionPalaceGetData(GameClient client, bool isUpdataProps = false)
        {
            lock (client.ClientData.LockUnionPalace)
            {
                UnionPalaceData myUPData = client.ClientData.MyUnionPalaceData;
                UnionPalaceBasicInfo basicInfo = null;

                if (!IsGongNengOpened())
                {
                    myUPData = new UnionPalaceData();
                    myUPData.ResultType = (int)EUnionPalaceState.EnoOpen;
                    return myUPData;
                }

                if (Global.GetBangHuiLevel(client) <= 0)
                {
                    if (myUPData == null) myUPData = new UnionPalaceData();
                    myUPData.ResultType = (int)EUnionPalaceState.ENoHave;

                    if (isUpdataProps) SetUnionPalaceProps(client, myUPData);

                    return myUPData;
                }

                if (myUPData == null)
                {
                    myUPData = new UnionPalaceData();

                    List<int> data = Global.GetRoleParamsIntListFromDB(client, RoleParamName.UnionPalace);
                    if (data == null || data.Count <= 0)
                    {
                        myUPData.RoleID = client.ClientData.RoleID;
                        myUPData.StatueID = DEFAULT_MIN_ID;

                        ModifyUnionPalaceData(client, myUPData);
                    }
                    else
                    {
                        myUPData.RoleID = client.ClientData.RoleID;
                        myUPData.StatueID = data[0];
                        myUPData.LifeAdd = data[1];
                        myUPData.AttackAdd = data[2];
                        myUPData.DefenseAdd = data[3];
                        myUPData.AttackInjureAdd = data[4];
                    }

                    basicInfo = GetUPBasicInfoByID(myUPData.StatueID);
                    myUPData.StatueType = basicInfo.StatueType;
                    myUPData.StatueLevel = basicInfo.StatueLevel;

                    client.ClientData.MyUnionPalaceData = myUPData;
                    SetUnionPalaceProps(client, myUPData);
                }

                myUPData.ZhanGongNeed = GetUnionPalaceZG(client);
                myUPData.UnionLevel = Global.GetBangHuiLevel(client);
                myUPData.BurstType = 0;
                if (basicInfo == null) basicInfo = GetUPBasicInfoByID(myUPData.StatueID);

                if (basicInfo.UnionLevel < 0)
                {
                    myUPData.ResultType = (int)EUnionPalaceState.End;
                    if (myUPData.UnionLevel < UNION_PALACE_MAX_LEVEL)
                        myUPData.ResultType = (int)EUnionPalaceState.PalaceMore;
                }
                else
                {
                    myUPData.ResultType = (int)EUnionPalaceState.Default;
                    if (myUPData.UnionLevel < basicInfo.UnionLevel)
                    {
                        myUPData.ResultType = (int)EUnionPalaceState.UnionNeedUp;
                        //
                        if (myUPData.LifeAdd != 0 || myUPData.AttackInjureAdd != 0 || myUPData.DefenseAdd != 0 || myUPData.AttackAdd != 0 || myUPData.StatueType > 1)
                            myUPData.ResultType = (int)EUnionPalaceState.PalaceMore;
                        else
                        {
                            int maxLevel = 0;
                            var temp = from info in _unionPalaceBasicList.Values
                                       where info.StatueID <= myUPData.StatueID && info.StatueLevel <= myUPData.StatueLevel && info.UnionLevel <= myUPData.UnionLevel && info.UnionLevel > 0
                                       orderby info.StatueID descending
                                       select info;

                            if (temp.Any()) maxLevel = temp.First<UnionPalaceBasicInfo>().StatueLevel;

                            if (basicInfo.StatueLevel > maxLevel + 1)
                                myUPData.ResultType = (int)EUnionPalaceState.PalaceMore;
                        }
                    }
                }

                if (isUpdataProps) SetUnionPalaceProps(client, myUPData);

                myUPData.ZhanGongLeft = client.ClientData.BangGong;
                // LogManager.WriteLog(LogTypes.Error, string.Format("-------------------------------------------union palace end {0}", myUPData.StatueID));

                return myUPData;
            }
        }

        public static void ModifyUnionPalaceData(GameClient client, UnionPalaceData data)
        {
            List<int> dataList = new List<int>();
            dataList.AddRange(new int[] { data.StatueID, data.LifeAdd, data.AttackAdd, data.DefenseAdd, data.AttackInjureAdd });

            Global.SaveRoleParamsIntListToDB(client, dataList, RoleParamName.UnionPalace, true);
        }

        public static UnionPalaceData UnionPalaceUp(GameClient client)
        {
            lock (client.ClientData.LockUnionPalace)
            {
                UnionPalaceData myUPData = UnionPalaceGetData(client);
                if (myUPData.ResultType < (int)EUnionPalaceState.Default)
                    return myUPData;

                if (myUPData.ResultType == (int)EUnionPalaceState.End || myUPData.ResultType == (int)EUnionPalaceState.EOver)
                {
                    myUPData.ResultType = (int)EUnionPalaceState.EOver;
                    return myUPData;
                }
           
                UnionPalaceBasicInfo basicInfo = GetUPBasicInfoByID(myUPData.StatueID);
                if (basicInfo.UnionLevel < 0)
                {
                    myUPData.ResultType = (int)EUnionPalaceState.EOver;
                    return myUPData;
                }

                int bhLevel = Global.GetBangHuiLevel(client);
                if (basicInfo.UnionLevel > bhLevel)
                {
                    myUPData.ResultType = (int)EUnionPalaceState.EUnionNeedUp;
                    return myUPData;
                }

                if (bhLevel < myUPData.StatueLevel)
                {
                    myUPData.ResultType = (int)EUnionPalaceState.EPalaceMore;
                    return myUPData;
                }

                int zhanGongNeed = GetUnionPalaceZG(client);
                if (!GameManager.ClientMgr.SubUserBangGong(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, zhanGongNeed))
                {
                    myUPData.ResultType = (int)EUnionPalaceState.EnoZhanGong;
                    return myUPData;
                }

                //几率
                UnionPalaceRateInfo rateInfo = GetUPRateInfoByID(basicInfo.StatueLevel);
                int[] addNums = null;
                int rate = 0;
                int r = Global.GetRandomNumber(0, 100);
                for (int i = 0; i < rateInfo.RateList.Count; i++)
                {
                    rate += rateInfo.RateList[i];
                    if (r <= rate)
                    {
                        addNums = rateInfo.AddNumList[rateInfo.RateList[i]].ToArray();
                        myUPData.BurstType = i;//暴击
                        break;
                    }
                }

                List<int> logList = new List<int>();
                logList.Add(myUPData.StatueID);
                logList.Add(myUPData.LifeAdd);
                logList.Add(myUPData.AttackAdd);
                logList.Add(myUPData.DefenseAdd);
                logList.Add(myUPData.AttackInjureAdd);

                //加成
                myUPData.LifeAdd += addNums[0] * _gmRate;
                myUPData.LifeAdd = myUPData.LifeAdd > basicInfo.LifeMax ? basicInfo.LifeMax : myUPData.LifeAdd;

                myUPData.AttackAdd += addNums[1] * _gmRate;
                myUPData.AttackAdd = myUPData.AttackAdd > basicInfo.AttackMax ? basicInfo.AttackMax : myUPData.AttackAdd;

                myUPData.DefenseAdd += addNums[2] * _gmRate;
                myUPData.DefenseAdd = myUPData.DefenseAdd > basicInfo.DefenseMax ? basicInfo.DefenseMax : myUPData.DefenseAdd;

                myUPData.AttackInjureAdd += addNums[3] * _gmRate;
                myUPData.AttackInjureAdd = myUPData.AttackInjureAdd > basicInfo.AttackInjureMax ? basicInfo.AttackInjureMax : myUPData.AttackInjureAdd;

                if (myUPData.LifeAdd < basicInfo.LifeMax || myUPData.DefenseAdd < basicInfo.DefenseMax ||
                    myUPData.AttackAdd < basicInfo.AttackMax || myUPData.AttackInjureAdd < basicInfo.AttackInjureMax)
                {
                    myUPData.ResultType = (int)EUnionPalaceState.Success;
                }
                else
                {
                    myUPData.StatueID += 1;
                    basicInfo = GetUPBasicInfoByID(myUPData.StatueID);
                    myUPData.StatueType = basicInfo.StatueType;
                    myUPData.StatueLevel = basicInfo.StatueLevel;

                    myUPData.LifeAdd = 0;
                    myUPData.AttackAdd = 0;
                    myUPData.DefenseAdd = 0;
                    myUPData.AttackInjureAdd = 0;
                 
                    myUPData.ResultType = (int)EUnionPalaceState.Next;
                    if (myUPData.StatueID > _unionPalaceBasicList.Count || basicInfo.UnionLevel<0)
                        myUPData.ResultType = (int)EUnionPalaceState.End;
                    else if (bhLevel < basicInfo.UnionLevel)
                        myUPData.ResultType = (int)EUnionPalaceState.UnionNeedUp;
                    else if (bhLevel < myUPData.StatueLevel)
                        myUPData.ResultType = (int)EUnionPalaceState.PalaceMore;
                }

                int today = Global.GetOffsetDayNow();
                int upCount = GetUpCount(client, today);
                myUPData.ZhanGongNeed = GetUnionPalaceZG(client, upCount + 1);

                ModifyUpCount(client, upCount + 1);
                ModifyUnionPalaceData(client, myUPData);

                client.ClientData.MyUnionPalaceData = myUPData;

                SetUnionPalaceProps(client, myUPData);

                //通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                myUPData.ZhanGongLeft = client.ClientData.BangGong;

                logList.Add(myUPData.StatueID);
                logList.Add(myUPData.LifeAdd);
                logList.Add(myUPData.AttackAdd);
                logList.Add(myUPData.DefenseAdd);
                logList.Add(myUPData.AttackInjureAdd);
                logList.Add(upCount);

                EventLogManager.AddUnionPalaceEvent(client, LogRecordType.UnionPalace, logList.ToArray());

                //LogManager.WriteLog(LogTypes.Error, string.Format("-------------------------------------------union palace end {0}", myUPData.StatueID));
                return myUPData;
            }
        }

        public static void initSetUnionPalaceProps(GameClient client, bool isUpdataProps = false)
        {
            lock (client.ClientData.LockUnionPalace)
            {
                client.ClientData.MyUnionPalaceData = null;
                UnionPalaceData UnionPalaceData = UnionPalaceGetData(client, isUpdataProps);

                //通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            } 
        }

        public static void SetUnionPalaceProps(GameClient client, UnionPalaceData myData)
        {
            lock (client.ClientData.LockUnionPalace)
            {
                //加成
                int life = 0;
                int attack = 0;
                int defense = 0;
                int injure = 0;

                UnionPalaceBasicInfo basicInfo = GetUPBasicInfoByID(myData.StatueID);
                int bangHuiLevel = Global.GetBangHuiLevel(client);
                if (basicInfo != null && basicInfo.UnionLevel <= bangHuiLevel && myData.StatueLevel <= bangHuiLevel)
                {
                    life = myData.LifeAdd;
                    attack = myData.AttackAdd;
                    defense = myData.DefenseAdd;
                    injure = myData.AttackInjureAdd;
                }

                foreach (UnionPalaceBasicInfo d in _unionPalaceBasicList.Values)
                {
                    if (d.StatueID < myData.StatueID && d.UnionLevel <= bangHuiLevel && d.StatueLevel <= bangHuiLevel)
                    {
                        life += d.LifeMax;
                        attack += d.AttackMax;
                        defense += d.DefenseMax;
                        injure += d.AttackInjureMax;
                    }
                }

                //额外加成
                double lifePercent = 0;
                int step = (myData.StatueID - 1) / STATUE_COUNT;
                if (step > 0)
                {
                    if (myData.ResultType == (int)EUnionPalaceState.PalaceMore || myData.ResultType == (int)EUnionPalaceState.EPalaceMore)
                    {
                        int maxID = 0;
                        var temp = from info in _unionPalaceBasicList.Values
                                   where info.StatueID <= myData.StatueID && info.StatueLevel <= myData.StatueLevel && info.UnionLevel <= myData.UnionLevel && info.UnionLevel > 0
                                   orderby info.StatueID descending
                                   select info;

                        if (temp.Any()) maxID = temp.First<UnionPalaceBasicInfo>().StatueID;
                        step = maxID / STATUE_COUNT;
                    }

                    UnionPalaceSpecialInfo s = GetUPSpecialInfoByID(step);
                    if (s != null && s.UnionLevel <= bangHuiLevel)
                        lifePercent = s.MaxLifePercent;
                }

                EquipPropItem propItem = new EquipPropItem();
                propItem.ExtProps[(int)ExtPropIndexes.MaxLifeV] = life;
                propItem.ExtProps[(int)ExtPropIndexes.AddAttack] = attack;
                propItem.ExtProps[(int)ExtPropIndexes.AddDefense] = defense;
                propItem.ExtProps[(int)ExtPropIndexes.AddAttackInjure] = injure;
                propItem.ExtProps[(int)ExtPropIndexes.MaxLifePercent] = lifePercent;
                client.ClientData.PropsCacheManager.SetExtProps((int)PropsSystemTypes.UnionPalace, propItem.ExtProps);
            }
        }

        public static int GetUpCount(GameClient client,int day)
        {
            int count = 0;
            int dayOld = 0;
            List<int> data = Global.GetRoleParamsIntListFromDB(client, RoleParamName.UnionPalaceUpCount);
            if (data != null && data.Count > 0)
                dayOld = data[0];

            if (dayOld == day) count = data[1];
            else ModifyUpCount(client, count);

            return count;
        }

        public static void ModifyUpCount(GameClient client, int count)
        {
            List<int> dataList = new List<int>();
            dataList.AddRange(new int[] {  Global.GetOffsetDayNow(), count });

            Global.SaveRoleParamsIntListToDB(client, dataList, RoleParamName.UnionPalaceUpCount, true);
        }

        public static bool IsGongNengOpened()
        {
            // 如果2.0的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System2Dot0))
                return false;
	
            return GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.UnionPalace);
        }

        #endregion

        #region ----------配置

        private static Dictionary<int, UnionPalaceBasicInfo> _unionPalaceBasicList = new Dictionary<int, UnionPalaceBasicInfo>();
        private static Dictionary<int, UnionPalaceSpecialInfo> _unionPalaceSpecialList = new Dictionary<int, UnionPalaceSpecialInfo>();
        private static Dictionary<int, UnionPalaceRateInfo> _unionPalaceRateList = new Dictionary<int, UnionPalaceRateInfo>();

        public static void InitConfig()
        {
            LoadUnionPalaceBasicInfo();
            LoadUnionPalaceSpecialInfo();
            LoadUnionPalaceRateInfo();
        }

        private static void LoadUnionPalaceBasicInfo()
        {
            string fileName = Global.GameResPath("Config/ShenDianLevelUp.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _unionPalaceBasicList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    UnionPalaceBasicInfo config = new UnionPalaceBasicInfo();
                    config.StatueID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    config.StatueType = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Type", "0"));
                    config.StatueLevel = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Level", "0"));

                    config.UnionLevel = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedZhanMengLevel", "0"));
                    string preStr = Global.GetDefAttributeStr(xmlItem, "NeedStatueLevel", "");
                    if (!string.IsNullOrEmpty(preStr))
                    {
                        string[] arr = preStr.Split(',');
                        if (arr.Length == 2)
                        {
                            config.PreStatueType = Convert.ToInt32(arr[0]);
                            config.PreStatueLevel = Convert.ToInt32(arr[1]);
                        }
                    }

                    config.LifeMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "MaxLifeV", "0"));
                    config.AttackMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "AddAttack", "0"));
                    config.DefenseMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "AddDefense", "0"));
                    config.AttackInjureMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "AddAttackInjure", "0"));

                    _unionPalaceBasicList.Add(config.StatueID, config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }
        }

        public static void LoadUnionPalaceSpecialInfo()
        {
            string fileName = Global.GameResPath("Config/ShenDianExtra.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _unionPalaceSpecialList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    UnionPalaceSpecialInfo config = new UnionPalaceSpecialInfo();
                    config.StatueLevel = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "StatueLevel", "0"));
                    config.UnionLevel = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ZhanMengLevel", "0"));
                    config.MaxLifePercent = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "MaxLifePercent", "0"));
                    _unionPalaceSpecialList.Add(config.StatueLevel, config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }
        }

        public static void LoadUnionPalaceRateInfo()
        {
            string fileName = Global.GameResPath("Config/ShenDianScale.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return;

            try
            {
                _unionPalaceRateList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    UnionPalaceRateInfo config = new UnionPalaceRateInfo();
                    config.StatueLevel = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Level", "0"));

                    string addString = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "Scale", ""));
                    if (addString.Length > 0)
                    {
                        string[] addArr = addString.Split('|');
                        foreach (string str in addArr)
                        {
                            string[] oneArr = str.Split(',');

                            int rate = (int)(float.Parse(oneArr[0])*100);
                            config.RateList.Add(rate);

                            List<int> valueList = new List<int>();
                            for (int i = 1; i < oneArr.Length; i++)
                            {
                                valueList.Add(int.Parse(oneArr[i]));
                            }
                            config.AddNumList.Add(rate,valueList);
                        }

                        _unionPalaceRateList.Add(config.StatueLevel, config);
                    }//if
                }//foreach
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }
        }

        public static UnionPalaceBasicInfo GetUPBasicInfoByID(int id)
        {
            if (_unionPalaceBasicList.ContainsKey(id))
                return _unionPalaceBasicList[id];

            return null;
        }

        public static UnionPalaceSpecialInfo GetUPSpecialInfoByID(int id)
        {
            if (_unionPalaceSpecialList.ContainsKey(id))
                return _unionPalaceSpecialList[id];

            return null;
        }

        public static UnionPalaceRateInfo GetUPRateInfoByID(int id)
        {
            if (_unionPalaceRateList.ContainsKey(id))
                return _unionPalaceRateList[id];

            return null;
        }

        public static int GetUnionPalaceZG(GameClient client,int upCount =0)
        {
            if (upCount <= 0)
            {
                int today = Global.GetOffsetDayNow();
                upCount = GetUpCount(client, today);
            }

            int[] zhanGongList = GameManager.systemParamsList.GetParamValueIntArrayByName("ZhanMengShenDian");

            if (upCount >= zhanGongList.Length)
                upCount = zhanGongList.Length - 1;

            return zhanGongList[upCount];
        }

        #endregion

        #region ----------GM

        public static void SetUnionPalaceLevelByID(GameClient client, int id)
        {
            List<int> dataList = new List<int>();
            dataList.AddRange(new int[] { id, 0, 0, 0, 0 });
            Global.SaveRoleParamsIntListToDB(client, dataList, RoleParamName.UnionPalace, true);

            client.ClientData.MyUnionPalaceData = null;
            initSetUnionPalaceProps(client, true);
        }

        public static void SetUnionPalaceCount(GameClient client, int count)
        {
            count = count < 0 ? 0 : count;
            ModifyUpCount(client, count);

            UnionPalaceData myUPData = client.ClientData.MyUnionPalaceData;
            myUPData.ZhanGongNeed = GetUnionPalaceZG(client);
        }

        public static void SetUnionPalaceRate(GameClient client, int rate)
        {
            _gmRate = rate;
        }
        #endregion

    }
}
