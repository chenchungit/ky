using GameServer.Core.GameEvent;
using GameServer.Server;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Tmsk.Contract;
using GameServer.Core.Executor;

namespace GameServer.Logic.JingJiChang
{
    /// <summary>
    /// 声望勋章管理器
    /// </summary>
    public class PrestigeMedalManager : IManager, ICmdProcessorEx, IEventListener, IEventListenerEx
    {
        /// <summary>
        /// 声望勋章增加属性系数
        /// </summary>
        private static int _medalRate = 1;

        private static int _defaultMedalID = 1;

        /// <summary>
        /// 声望勋章状态
        /// </summary>
        private enum PrestigeMedalResultType
        {
            End = 3,            //提升达到极限
            Next = 2,           //成功，开启下一个
            Success = 1,        //成功，未生效 
            Fail = 0,           //失败
            EnoOpen = -1,       //未开放
            EnoPrestige = -2,   //声望不足
            EnoDiamond = -3,    //钻石不足
            EOver = -4,         //全部开启
        };

        /// <summary>
        /// 声望勋章基本信息
        /// </summary>
        private static Dictionary<int, PrestigeMedalBasicData> _prestigeMedalBasicList = new Dictionary<int, PrestigeMedalBasicData>();

        /// <summary>
        /// 声望勋章额外信息
        /// </summary>
        private static Dictionary<int, PrestigeMedalSpecialData> _prestigeMedalSpecialList = new Dictionary<int, PrestigeMedalSpecialData>();


        #region 接口相关
        /* public static PrestigeMedalManager getInstance()
        {
            if (instance._State == 0)
                instance.initialize();

            return instance;
        }

        public bool initialize()
        {
            if (!initPrestigeMedal())
            {
                _State = -1;
                return false;
            }

            _State = 1;
            return true;
        }*/

        private int _State = 0;
        private static PrestigeMedalManager instance = new PrestigeMedalManager();

        public static PrestigeMedalManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            if (!initPrestigeMedal())
                return false;

            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_PRESTIGE_MEDAL_INFO, 1, 1, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_PRESTIGE_MEDAL_UP, 2, 2, getInstance());

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

        public void processEvent(EventObject eventObject)
        {
        }

        public void processEvent(EventObjectEx eventObject)
        {
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            return false;
        }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_PRESTIGE_MEDAL_INFO:
                    return ProcessCmdPrestigeMedalInfo(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_PRESTIGE_MEDAL_UP:
                    return ProcessCmdPrestigeMedalUp(client, nID, bytes, cmdParams);
            }

            return true;
        }

        public bool ProcessCmdPrestigeMedalInfo(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                //解析用户id,符文id
                if (cmdParams.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(client.ClientSocket), cmdParams.Length));
                    return false;
                }

                int roleID = Convert.ToInt32(cmdParams[0]);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(client.ClientSocket), roleID));
                    return false;
                }

                PrestigeMedalData runeData = GetPrestigeMedalData(client);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_PRESTIGE_MEDAL_INFO, runeData);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        public bool ProcessCmdPrestigeMedalUp(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                //解析用户id,符文id
                if (cmdParams.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(client.ClientSocket), cmdParams.Length));
                    return false;
                }

                int roleID = Convert.ToInt32(cmdParams[0]);
                int runeID = Convert.ToInt32(cmdParams[1]);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(client.ClientSocket), roleID));
                    return false;
                }

                // 如果1.4.1的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("ProcessCmdPrestigeMedalUp功能尚未开放, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(client.ClientSocket), roleID));
                    return false;
                }

                PrestigeMedalData runeData = UpPrestigeMedal(client, runeID);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_PRESTIGE_MEDAL_UP, runeData);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        #endregion

        #region 声望勋章配置信息

        /// <summary>
        /// 声望勋章基本信息初始化
        /// </summary>
        public static bool initPrestigeMedal()
        {
            bool result1 = LoadPrestigeMedalBasicData();
            bool result2 = LoadPrestigeMedalSpecialData();

            return result1 && result2;
        }

        /// <summary>
        /// 加载声望勋章基本信息
        /// </summary>
        public static bool LoadPrestigeMedalBasicData()
        {
            string fileName = "Config/ShengWangXunZhang.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));

            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ShengWangXunZhang.xml时出错!!!文件不存在");
                return false;
            }

            try
            {
                _prestigeMedalBasicList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    PrestigeMedalBasicData config = new PrestigeMedalBasicData();
                    config.MedalID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    config.MedalName = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "Name", ""));
                    config.LifeMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "LifeV", "0"));
                    config.AttackMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "AddAttack", "0"));
                    config.DefenseMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "AddDefense", "0"));
                    config.HitMax = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "HitV", "0"));
                    config.PrestigeCost = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "CostShengWang", "0"));

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

                    _prestigeMedalBasicList.Add(config.MedalID, config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ShengWangXunZhang.xml时文件出现异常!!!", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 加载声望勋章额外信息
        /// </summary>
        public static bool LoadPrestigeMedalSpecialData()
        {
            string fileName = "Config/ShengWangSpecialAttribute.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));

            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ShengWangSpecialAttribute.xml时出错!!!文件不存在");
                return false;
            }

            try
            {
                _prestigeMedalSpecialList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    PrestigeMedalSpecialData config = new PrestigeMedalSpecialData();
                    config.SpecialID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    config.MedalID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedFuWen", "0"));
                    config.DoubleAttack = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "ZhiMingYiJi", "0"));
                    config.DiDouble = Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "DiKangZhiMingYiJi", "0"));
                    _prestigeMedalSpecialList.Add(config.MedalID, config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ShengWangSpecialAttribute.xml时出现异常!!!", ex);
                return false;
            }

            return true;
        }

        public static PrestigeMedalBasicData GetPrestigeMedalBasicDataByID(int id)
        {
            if (_prestigeMedalBasicList.ContainsKey(id))
                return _prestigeMedalBasicList[id];

            return null;
        }

        public static PrestigeMedalSpecialData GetPrestigeMedalSpecialDataByID(int id)
        {
            if (_prestigeMedalSpecialList.ContainsKey(id))
                return _prestigeMedalSpecialList[id];

            return null;
        }

        #endregion

        #region 声望勋章相关

        /// <summary>
        /// 获得今天声望勋章提示次数
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetPrestigeMedalUpCount(GameClient client)
        {
            int count = 0;
            int dayOld = 0;
            List<int> data = Global.GetRoleParamsIntListFromDB(client, RoleParamName.PrestigeMedalUpCount);
            if (data != null && data.Count > 0)
                dayOld = data[0];

            int day = TimeUtil.NowDateTime().DayOfYear;
            if (dayOld == day)
                count = data[1];
            else
                ModifyPrestigeMedalUpCount(client, count, true);

            return count;
        }

        /// <summary>
        /// 修改声望勋章次数数据
        /// </summary>
        /// <returns></returns>
        public static void ModifyPrestigeMedalUpCount(GameClient client, int count, bool writeToDB = false)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
            {
                return;
            }

            List<int> dataList = new List<int>();
            dataList.AddRange(new int[] { TimeUtil.NowDateTime().DayOfYear, count });

            Global.SaveRoleParamsIntListToDB(client, dataList, RoleParamName.PrestigeMedalUpCount, writeToDB);
        }

        /// <summary>
        /// 声望勋章——消耗钻石
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetPrestigeMedalDiamond(GameClient client, int upCount)
        {
            int[] diamondList = GameManager.systemParamsList.GetParamValueIntArrayByName("ShengWangXunZhangZuanShi");

            if (upCount >= diamondList.Length)
                upCount = diamondList.Length - 1;

            return diamondList[upCount];
        }

        /// <summary>
        /// 返回声望勋章数据
        /// </summary>
        /// <returns></returns>
        public static PrestigeMedalData GetPrestigeMedalData(GameClient client)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
            {
                return null;
            }
            //开放等级  声望4阶
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.PrestigeMedal))
                return null;

            PrestigeMedalData prestigeMedalData = client.ClientData.prestigeMedalData;
            if (prestigeMedalData == null)
            {
                PrestigeMedalBasicData basic = null;
                prestigeMedalData = new PrestigeMedalData();

                List<int> data = Global.GetRoleParamsIntListFromDB(client, RoleParamName.PrestigeMedal);
                if (data == null || data.Count <= 0)
                {
                    basic = GetPrestigeMedalBasicDataByID(_defaultMedalID);
                    prestigeMedalData.RoleID = client.ClientData.RoleID;
                    prestigeMedalData.MedalID = basic.MedalID;

                    ModifyPrestigeMedalData(client, prestigeMedalData, true);
                }
                else
                {
                    prestigeMedalData.RoleID = client.ClientData.RoleID;
                    prestigeMedalData.MedalID = data[0];
                    prestigeMedalData.LifeAdd = data[1];
                    prestigeMedalData.AttackAdd = data[2];
                    prestigeMedalData.DefenseAdd = data[3];
                    prestigeMedalData.HitAdd = data[4];

                    if (prestigeMedalData.MedalID > _prestigeMedalBasicList.Count)
                    {
                        prestigeMedalData.UpResultType = 3;
                        basic = GetPrestigeMedalBasicDataByID(_prestigeMedalBasicList.Count);
                    }
                    else
                    {
                        basic = GetPrestigeMedalBasicDataByID(prestigeMedalData.MedalID);
                    }
                }

                prestigeMedalData.Diamond = GetPrestigeMedalDiamond(client, GetPrestigeMedalUpCount(client));
                prestigeMedalData.Prestige = basic.PrestigeCost;

                client.ClientData.prestigeMedalData = prestigeMedalData;
            }

            prestigeMedalData.PrestigeLeft = GameManager.ClientMgr.GetShengWangValue(client);

            return prestigeMedalData;
        }

        /// <summary>
        /// 修改声望勋章数据
        /// </summary>
        /// <returns></returns>
        public static void ModifyPrestigeMedalData(GameClient client, PrestigeMedalData data, bool writeToDB = false)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
            {
                return;
            }
            List<int> dataList = new List<int>();
            dataList.AddRange(new int[] { data.MedalID, data.LifeAdd, data.AttackAdd, data.DefenseAdd, data.HitAdd });

            Global.SaveRoleParamsIntListToDB(client, dataList, RoleParamName.PrestigeMedal, writeToDB);
        }

        /// <summary>
        /// 声望勋章——提升
        /// </summary>
        /// <param name="client"></param>
        /// <param name="MedalID"></param>
        /// <returns></returns>
        public static PrestigeMedalData UpPrestigeMedal(GameClient client, int MedalID)
        {
            PrestigeMedalData prestigeMedalData = client.ClientData.prestigeMedalData;
            if (prestigeMedalData != null && prestigeMedalData.UpResultType == (int)PrestigeMedalResultType.End)
            {
                prestigeMedalData.UpResultType = (int)PrestigeMedalResultType.EOver;
                return prestigeMedalData;
            }

            if (prestigeMedalData == null || prestigeMedalData.MedalID != MedalID)
            {
                prestigeMedalData.UpResultType = (int)PrestigeMedalResultType.Fail;
                return prestigeMedalData;
            }

            //开放等级  声望4阶
            bool isOpen = GlobalNew.IsGongNengOpened(client, GongNengIDs.PrestigeMedal);
            if (!isOpen)
            {
                prestigeMedalData.UpResultType = (int)PrestigeMedalResultType.EnoOpen;
                return prestigeMedalData;
            }

            PrestigeMedalBasicData basicMedal = GetPrestigeMedalBasicDataByID(MedalID);

            //声望
            int prestigeNow = GameManager.ClientMgr.GetShengWangValue(client);
            if (basicMedal.PrestigeCost > prestigeNow)
            {
                prestigeMedalData.UpResultType = (int)PrestigeMedalResultType.EnoPrestige;
                return prestigeMedalData;
            }

            //钻石
            int upCount = GetPrestigeMedalUpCount(client);
            int diamondNeed = GetPrestigeMedalDiamond(client, upCount);
            if (diamondNeed > 0 && !GameManager.ClientMgr.SubUserMoney(client, diamondNeed, "声望勋章提升"))
            {
                prestigeMedalData.UpResultType = (int)PrestigeMedalResultType.EnoDiamond;
                return prestigeMedalData;
            }

            try
            {
                GameManager.ClientMgr.ModifyShengWangValue(client, -basicMedal.PrestigeCost, "声望勋章提升");
            }
            catch (Exception)
            {
                prestigeMedalData.UpResultType = (int)PrestigeMedalResultType.EnoPrestige;
                return prestigeMedalData;
            }

            //几率
            int[] addNums = null;
            int rate = 0;
            int r = Global.GetRandomNumber(0, 100);
            for (int i = 0; i < basicMedal.RateList.Count; i++)
            {
                rate += basicMedal.RateList[i];
                if (r <= rate)
                {
                    addNums = basicMedal.AddNumList[i];
                    prestigeMedalData.BurstType = i;//暴击
                    break;
                }
            }

            //加成
            prestigeMedalData.LifeAdd += addNums[0] * _medalRate;
            prestigeMedalData.LifeAdd = prestigeMedalData.LifeAdd > basicMedal.LifeMax ? basicMedal.LifeMax : prestigeMedalData.LifeAdd;

            prestigeMedalData.AttackAdd += addNums[1] * _medalRate;
            prestigeMedalData.AttackAdd = prestigeMedalData.AttackAdd > basicMedal.AttackMax ? basicMedal.AttackMax : prestigeMedalData.AttackAdd;

            prestigeMedalData.DefenseAdd += addNums[2] * _medalRate;
            prestigeMedalData.DefenseAdd = prestigeMedalData.DefenseAdd > basicMedal.DefenseMax ? basicMedal.DefenseMax : prestigeMedalData.DefenseAdd;

            prestigeMedalData.HitAdd += addNums[3] * _medalRate;
            prestigeMedalData.HitAdd = prestigeMedalData.HitAdd > basicMedal.HitMax ? basicMedal.HitMax : prestigeMedalData.HitAdd;

            if (prestigeMedalData.LifeAdd < basicMedal.LifeMax || prestigeMedalData.DefenseAdd < basicMedal.DefenseMax ||
                prestigeMedalData.AttackAdd < basicMedal.AttackMax || prestigeMedalData.HitAdd < basicMedal.HitMax)
            {
                prestigeMedalData.UpResultType = (int)PrestigeMedalResultType.Success;
                prestigeMedalData.Prestige = basicMedal.PrestigeCost;
                prestigeMedalData.Diamond = GetPrestigeMedalDiamond(client, upCount + 1);
            }
            else
            {
                prestigeMedalData.MedalID += 1;
                prestigeMedalData.LifeAdd = 0;
                prestigeMedalData.AttackAdd = 0;
                prestigeMedalData.DefenseAdd = 0;
                prestigeMedalData.HitAdd = 0;

                prestigeMedalData.UpResultType = (int)PrestigeMedalResultType.Next;
                if (prestigeMedalData.MedalID > _prestigeMedalBasicList.Count)
                {
                    prestigeMedalData.UpResultType = (int)PrestigeMedalResultType.End;
                    prestigeMedalData.Prestige = 0;
                    prestigeMedalData.Diamond = 0;
                }
                else
                {
                    basicMedal = GetPrestigeMedalBasicDataByID(prestigeMedalData.MedalID);
                    prestigeMedalData.Prestige = basicMedal.PrestigeCost;
                    prestigeMedalData.Diamond = GetPrestigeMedalDiamond(client, upCount + 1);
                }
            }

            ModifyPrestigeMedalUpCount(client, upCount + 1, true);
            ModifyPrestigeMedalData(client, prestigeMedalData);

            client.ClientData.prestigeMedalData = prestigeMedalData;

            SetPrestigeMedalProps(client, prestigeMedalData);

            //通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            prestigeMedalData.PrestigeLeft = GameManager.ClientMgr.GetShengWangValue(client);
            return prestigeMedalData;
        }

        /// <summary>
        /// 设置声望勋章属性
        /// </summary>
        /// <param name="client"></param>
        public static void initSetPrestigeMedalProps(GameClient client)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
                return;

            //开放等级  声望4阶
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.PrestigeMedal))
                return;

            PrestigeMedalData PrestigeMedalData = GetPrestigeMedalData(client);
            SetPrestigeMedalProps(client, PrestigeMedalData);
        }

        /// <summary>
        /// 设置声望勋章属性
        /// </summary>
        /// <param name="client"></param>
        public static void SetPrestigeMedalProps(GameClient client, PrestigeMedalData PrestigeMedalData)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
            {
                return;
            }

            //加成
            int life = PrestigeMedalData.LifeAdd;
            int attack = PrestigeMedalData.AttackAdd;
            int defense = PrestigeMedalData.DefenseAdd;
            int hit = PrestigeMedalData.HitAdd;
            foreach (PrestigeMedalBasicData d in _prestigeMedalBasicList.Values)
            {
                if (d.MedalID < PrestigeMedalData.MedalID)
                {
                    life += d.LifeMax;
                    attack += d.AttackMax;
                    defense += d.DefenseMax;
                    hit += d.HitMax;
                }
            }

            //额外加成
            double zhuoYue = 0;
            double diKang = 0;
            if (PrestigeMedalData.MedalID > 1)
            {
                PrestigeMedalSpecialData d = GetPrestigeMedalSpecialDataByID(PrestigeMedalData.MedalID - 1);
                zhuoYue += d.DoubleAttack;
                diKang += d.DiDouble;
            }

            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.PrestigeMedal, (int)ExtPropIndexes.MaxLifeV, life);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.PrestigeMedal, (int)ExtPropIndexes.AddAttack, attack);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.PrestigeMedal, (int)ExtPropIndexes.AddDefense, defense);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.PrestigeMedal, (int)ExtPropIndexes.HitV, hit);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.PrestigeMedal, (int)ExtPropIndexes.DoubleAttack, zhuoYue);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.PrestigeMedal, (int)ExtPropIndexes.DeDoubleAttack, diKang);
        }

        #endregion

        #region 声望GM相关

        /// <summary>
        /// 声望军衔——设置等级
        /// </summary>
        /// <param name="client"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static void SetPrestigeLevel(GameClient client, int level)
        {
            //更新到数据库
            Global.SaveRoleParamsInt32ValueWithTimeStampToDB(client, RoleParamName.ShengWangLevel, level, true);
            GameManager.logDBCmdMgr.AddDBLogInfo(-1, "声望等级", "GM", "系统", client.ClientData.RoleName, "修改", level, client.ClientData.ZoneID, client.strUserID, level, client.ServerId);

            EventLogManager.AddRoleEvent(client, OpTypes.Trace, OpTags.GM, LogRecordType.IntValueWithType, level, RoleAttributeType.ShengWangLevel);
            if (level > 0)
                JingJiChangManager.getInstance().activeJunXianBuff(client, true);

            //更新BufferData
            double[] actionParams = new double[1];
            actionParams[0] = (double)level - 1;
            Global.UpdateBufferData(client, BufferItemTypes.MU_JINGJICHANG_JUNXIAN, actionParams, 0);

            ChengJiuManager.OnRoleJunXianChengJiu(client);
            Global.BroadcastClientMUShengWang(client, level);

            //通知自己
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.ShengWangLevel, level);
            //通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            // 军衔升级成功时，刷新相应的图标状态
            client._IconStateMgr.CheckJingJiChangJunXian(client);
            client._IconStateMgr.CheckSpecialActivity(client);
            client._IconStateMgr.SendIconStateToClient(client);
        }

        /// <summary>
        /// 声望勋章——设置等级
        /// </summary>
        /// <param name="client"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static void SetPrestigeMedalLevel(GameClient client, int level)
        {
            level = level <= 0 ? 1 : level;

            PrestigeMedalData prestigeMedalData = new PrestigeMedalData();
            PrestigeMedalBasicData basic = GetPrestigeMedalBasicDataByID(level);
            prestigeMedalData.RoleID = client.ClientData.RoleID;
            prestigeMedalData.MedalID = basic.MedalID;
            if (prestigeMedalData.MedalID > _prestigeMedalBasicList.Count)
                prestigeMedalData.UpResultType = 3;

            ModifyPrestigeMedalData(client, prestigeMedalData, true);
            client.ClientData.prestigeMedalData = prestigeMedalData;

            SetPrestigeMedalProps(client, prestigeMedalData);
        }

        /// <summary>
        /// 声望勋章——设置当天升级次数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static void SetPrestigeMedalCount(GameClient client, int count)
        {
            count = count < 0 ? 0 : count;
            ModifyPrestigeMedalUpCount(client, count, true);

            PrestigeMedalData prestigeMedalData = client.ClientData.prestigeMedalData;
            prestigeMedalData.Diamond = GetPrestigeMedalDiamond(client, GetPrestigeMedalUpCount(client));
            client.ClientData.prestigeMedalData = prestigeMedalData;
        }

        /// <summary>
        /// 声望勋章——设置属性增加系数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="rate"></param>
        public static void SetPrestigeMedalRate(GameClient client, int rate)
        {
            _medalRate = rate;
        }


        #endregion
    }
}
