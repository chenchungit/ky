using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.Tools;
using Server.Data;
using GameServer.Core.Executor;
using System.IO;

namespace GameServer.Logic
{
    /// <summary>
    /// 原来t_config表中的一部分变量，变为动态配置
    /// </summary>
    public class PlatConfig
    {
        #region 基础数据
        /// <summary>
        /// 单个配置的字典
        /// </summary>
        private Dictionary<string, string> _PlatConfigNormalDict = null;
        /// <summary>
        /// waiting配置的字典
        /// </summary>
        private Dictionary<int, WaitingConfig> _PlatConfigWaitingDict = null;
        /// <summary>
        /// 交易限制的字典
        /// </summary>
        private List<TradeLevelLimitConfig> _PlatConfigTradeLevelLimitList = null;

        private string fileName = string.Format("Config/PlatConfig.xml");
        #endregion 基础数据

        /// <summary>
        /// 从platConfig.xml获取配置参数
        /// </summary>
        public void LoadPlatConfig()
        {
            //查询游戏配置参数
            //从platConfig,xml加载配置参数

            string filePath = Global.GameResPath(fileName);
            _PlatConfigNormalDict = new Dictionary<string, string>();
            _PlatConfigWaitingDict = new Dictionary<int, WaitingConfig>();
            _PlatConfigTradeLevelLimitList = new List<TradeLevelLimitConfig>();

            try
            {
                XElement xml = XElement.Load(filePath);
                LoadNormalConfig(xml, _PlatConfigNormalDict);
                LoadWaitingConfig(xml, _PlatConfigWaitingDict);
                LoadTradeLevelLimitConfig(xml, _PlatConfigTradeLevelLimitList);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "区域平台特殊配置文件加载失败" + filePath + "\r\n" + ex.ToString(), ex, false);
            }
        }
        /// <summary>
        /// 重新加载文件
        /// </summary>
        /// <returns></returns>
        public int ReloadPlatConfig()
        {
            Dictionary<string, string> normalDict = new Dictionary<string, string>();
            Dictionary<int, WaitingConfig> waitingDict = new Dictionary<int, WaitingConfig>();
            List<TradeLevelLimitConfig> tradeLevelLimitList = new List<TradeLevelLimitConfig>();
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(fileName));
                LoadNormalConfig(xml, normalDict);
                LoadWaitingConfig(xml, waitingDict);
                LoadTradeLevelLimitConfig(xml, tradeLevelLimitList);
            }
            catch (Exception e)
            {
                LogManager.WriteException("重新加载配置文件 PlatConfig.xml  失败！！！" + e.ToString());
                return -1;
            }

            //没有异常，说明reload的配置文件没有错
            //可以把相关字典替换掉替换了
            lock (_PlatConfigNormalDict)
            {
                _PlatConfigNormalDict = normalDict;
            }
            lock (_PlatConfigWaitingDict)
            {
                _PlatConfigWaitingDict = waitingDict;
            }
            lock (_PlatConfigTradeLevelLimitList)
            {
                _PlatConfigTradeLevelLimitList = tradeLevelLimitList;
            }
            //重新Load配置文件后要重新设定MaxPosCmdNumPer5Seconds的值
            TCPSession.SetMaxPosCmdNumPer5Seconds(8);
            //更新登录配置
            GameManager.loginWaitLogic.LoadConfig();
            return 0;
        }
        /// <summary>
        /// 加载没有子节点的配置，把每个节点的属性一起存储在同一个字典中
        /// </summary>
        /// <param name="xml"></param>
        private void LoadNormalConfig(XElement xml, Dictionary<string, string> normalDict)
        {
            lock (normalDict)
            {
                XElement xmlEle = null;
                try
                {
                    //加载DCLog
                    xmlEle = Global.GetSafeXElement(xml, "DCLogs").Element("DCLog");
                    //开始添加配置项
                    normalDict.Add(PlatConfigNames.TwLogPid, (string)Global.GetSafeAttributeStr(xmlEle, "pid"));
                    normalDict.Add(PlatConfigNames.TwLogPath, (string)Global.GetSafeAttributeStr(xmlEle, "path"));
                    normalDict.Add(PlatConfigNames.TwLogHead, (string)Global.GetSafeAttributeStr(xmlEle, "logHead"));
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("加载系统配置参数配置文件:{0}, 失败。{1} 节点配置错误！", fileName, "DCLog") + e.ToString());
                }

                try
                {
                    //加载FileBans,角色禁止登陆相关
                    xmlEle = Global.GetSafeXElement(xml, "FileBans").Element("FileBanPros");
                    //开始添加配置项
                    normalDict.Add(PlatConfigNames.FileBanHour, (string)Global.GetSafeAttributeStr(xmlEle, "FileBanHour"));
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("加载系统配置参数配置文件:{0}, 失败。{1} 节点配置错误！", fileName, "FileBanPros") + e.ToString());
                }

                try
                {
                    //加载Chat配置
                    xmlEle = Global.GetSafeXElement(xml, "Chats").Element("Chat");
                    //开始添加配置项
                    normalDict.Add(PlatConfigNames.ChatWorldLevel, (string)Global.GetSafeAttributeStr(xmlEle, "world"));
                    normalDict.Add(PlatConfigNames.ChatFamilyLevel, (string)Global.GetSafeAttributeStr(xmlEle, "family"));
                    normalDict.Add(PlatConfigNames.ChatTeamTevel, (string)Global.GetSafeAttributeStr(xmlEle, "team"));
                    normalDict.Add(PlatConfigNames.ChatPrivateLevel, (string)Global.GetSafeAttributeStr(xmlEle, "private"));
                    normalDict.Add(PlatConfigNames.ChatNearLevel, (string)Global.GetSafeAttributeStr(xmlEle, "near"));
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("加载系统配置参数配置文件:{0}, 失败。{1} 节点配置错误！", fileName, "Chat") + e.ToString());
                }

                try
                {
                    //加载Speed配置
                    xmlEle = Global.GetSafeXElement(xml, "Speeds").Element("Speed");
                    //开始添加配置项
                    normalDict.Add(PlatConfigNames.BanSpeedUpMinutes2, (string)Global.GetSafeAttributeStr(xmlEle, "BanMins"));
                    normalDict.Add(PlatConfigNames.MaxPosCmdNum, (string)Global.GetSafeAttributeStr(xmlEle, "MaxPosCmdNum"));
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("加载系统配置参数配置文件:{0}, 失败。{1} 节点配置错误！", fileName, "Speed") + e.ToString());
                }
            }
        }

        /// <summary>
        /// 获取游戏配置项,返回字符串
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public string GetGameConfigItemStr(string paramName, string defVal)
        {
            //先从数据库读取，如果没有的话，再从配置表读取，如果都没有，则返回默认值
            string retStr = GameManager.GameConfigMgr.GetGameConifgItem(paramName);
            if (retStr == null)
            {
                if (paramName.Equals(PlatConfigNames.TradeLevelLlimit))
                {
                    retStr = GetPlatTradeLevelLimitConfig(paramName);
                }
                else if (paramName.Equals(PlatConfigNames.UserWaitConfig) || paramName.Equals(PlatConfigNames.VipWaitConfig) || paramName.Equals(PlatConfigNames.LoginAllowVipExp))
                {
                    retStr = GetWaitingConfig(paramName);
                }
                else
                {
                    retStr = GetNormalConfig(paramName);
                }
            }
            return !string.IsNullOrEmpty(retStr) ? retStr : defVal;
        }
        /// <summary>
        /// 获取游戏配置项，返回32位整数
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public int GetGameConfigItemInt(string paramName, int defVal)
        {
            //先从数据库读取，如果没有的话，再从配置表读取，如果都没有，则返回默认值
            string retStr = GameManager.GameConfigMgr.GetGameConifgItem(paramName);
            int retInt = 0;
            if (null == retStr)
            {
                if (paramName.Equals(PlatConfigNames.TradeLevelLlimit))
                {
                    retStr = GetPlatTradeLevelLimitConfig(paramName);
                }
                else if (paramName.Equals(PlatConfigNames.UserWaitConfig) || paramName.Equals(PlatConfigNames.VipWaitConfig) || paramName.Equals(PlatConfigNames.LoginAllowVipExp))
                {
                    retStr = GetWaitingConfig(paramName);
                }
                else
                {
                    retStr = GetNormalConfig(paramName);
                }
            }
            try
            {
                //如果配置文件也没有，直接返回默认值
                if (string.IsNullOrEmpty(retStr))
                {
                    return defVal;
                }
                else
                {
                    retInt = Convert.ToInt32(retStr);
                }
            }
            catch (Exception e)
            {
                return defVal;
            }
            return retInt;
        }
        /// <summary>
        /// 获取平台配置文件
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="defVal"></param>
        /// <returns></returns>
        private string GetNormalConfig(string paramName)
        {
            string paramValue = null;
            lock (_PlatConfigNormalDict)
            {
                if (!_PlatConfigNormalDict.TryGetValue(paramName, out paramValue))
                {
                    paramValue = null;
                }
            }
            return paramValue;
        }

        /// <summary>
        /// 获取等待排队的配置
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="defVal"></param>
        /// <returns></returns>
        private string GetWaitingConfig(string paramName)
        {
            WaitingConfig waitingConfig = null;
            lock (_PlatConfigWaitingDict)
            {
                if (!_PlatConfigWaitingDict.TryGetValue(GameManager.ServerId, out waitingConfig))
                {
                    //如果指定的服务器不存在，则选择默认的，默认的为0
                    _PlatConfigWaitingDict.TryGetValue(0, out waitingConfig);
                }
            }
            //如果不为空
            if (waitingConfig != null)
            {
                if (paramName.Equals(PlatConfigNames.UserWaitConfig))
                {
                    return waitingConfig.UserWaitConfig;
                }
                if (paramName.Equals(PlatConfigNames.VipWaitConfig))
                {
                    return waitingConfig.VIPWaitConfig;
                }
                if (paramName.Equals(PlatConfigNames.LoginAllowVipExp))
                {
                    return waitingConfig.LoginAllow_VIPExp.ToString();
                }
            }
            //上面三个都不满足，返回NULL
            return null;
        }
        /// <summary>
        /// 获取交易限制配置项
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        private string GetPlatTradeLevelLimitConfig(string paramName)
        {
            string str = null;
            IEnumerable<TradeLevelLimitConfig> query = null;
            lock (_PlatConfigTradeLevelLimitList)
            {
                query = from items in _PlatConfigTradeLevelLimitList orderby items.Day select items;
            }
            //开服了多少天
            DateTime t1 = TimeUtil.NowDateTime();
            DateTime t2 = Global.GetKaiFuTime();
            int elapsedDays = Global.GetDaysSpanNum(TimeUtil.NowDateTime(), t2);
            foreach (var v in query)
            {
                str = v.Limit;
                if (elapsedDays <= v.Day)
                {
                    break;
                }
            }
            return str;
        }
        /// <summary>
        /// 加载排队配置，每个区的排队配置组成一个类 WatingConfig，字典的key为serverID
        /// </summary>
        /// <param name="xml"></param>
        private void LoadWaitingConfig(XElement xml, Dictionary<int, WaitingConfig> waitingDict)
        {
            lock (waitingDict)
            {
                try
                {
                    XElement xmlEle = null;
                    //加载Waiting 
                    xmlEle = Global.GetSafeXElement(xml, "Waiting");
                    IEnumerable<XElement> waitingNodes = xmlEle.Elements();
                    foreach (var xmlNode in waitingNodes)
                    {
                        WaitingConfig waitingConfig = new WaitingConfig();
                        string severID = (string)Global.GetSafeAttributeStr(xmlNode, "ID");
                        waitingConfig.SeverID = Convert.ToInt32(severID);
                        int[] NeedWaitNumberArr = Global.GetSafeAttributeIntArray(xmlNode, "NeedWaitNumber", 2);
                        waitingConfig.NormalNeedWaitNumber = NeedWaitNumberArr[0];
                        waitingConfig.VIPNeedWaitNumber = NeedWaitNumberArr[1];
                        int[] MaxNumber = Global.GetSafeAttributeIntArray(xmlNode, "MaxNumber", 2);
                        waitingConfig.NormalMaxNumber = MaxNumber[0];
                        waitingConfig.VIPMaxNumber = MaxNumber[1];
                        int[] WaitingMaxNumber = Global.GetSafeAttributeIntArray(xmlNode, "WaitingMaxNumber", 2);
                        waitingConfig.NormalWaitingMaxNumber = WaitingMaxNumber[0];
                        waitingConfig.VIPWaitingMaxNumber = WaitingMaxNumber[1];
                        int[] EnterMinInt = Global.GetSafeAttributeIntArray(xmlNode, "EnterMinInt", 2);
                        waitingConfig.NormalEnterMinInt = EnterMinInt[0];
                        waitingConfig.VIPEnterMinInt = EnterMinInt[1];
                        int[] AllowMSecs = Global.GetSafeAttributeIntArray(xmlNode, "AllowMSecs", 2);
                        waitingConfig.NormalAllowMSecs = AllowMSecs[0];
                        waitingConfig.VIPAllowMSecs = AllowMSecs[1];
                        int[] LogoutAllowMSecs = Global.GetSafeAttributeIntArray(xmlNode, "LogoutAllowMSecs", 2);
                        waitingConfig.NormalLogoutAllowMSecs = LogoutAllowMSecs[0];
                        waitingConfig.VIPLogoutAllowMSecs = LogoutAllowMSecs[1];
                        waitingConfig.VipExp = Convert.ToInt32((string)Global.GetSafeAttributeStr(xmlNode, "vipexp"));
                        //添加到等待配置字典
                        waitingDict.Add(waitingConfig.SeverID, waitingConfig);
                    }

                    //如果waiting配置项没有配置默认项(0),或者没有waiting项没有内容，也抛出异常
                    if (!waitingDict.ContainsKey(0))
                    {
                        throw new Exception(string.Format("配置文件 {0} 可能没有配置 {1} 项或者没有默认配置项，请正确配置后重新加载文件。", fileName, "waiting"));
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("加载系统配置参数配置文件:{0}, 失败。{1} 节点配置错误！! !  {2}", fileName, "Waiting", e.ToString()));
                }
            }
        }
        /// <summary>
        /// 交易限制，交易限制配置组成一个类 TradeLevelLimitConfig，放在一个列表里面，方便排序对比
        /// </summary>
        /// <param name="xml"></param>
        private void LoadTradeLevelLimitConfig(XElement xml, List<TradeLevelLimitConfig> tradeLevelLimitList)
        {
            lock (tradeLevelLimitList)
            {
                try
                {
                    XElement xmlEle = null;
                    //加载TradeLevelLimit
                    xmlEle = Global.GetSafeXElement(xml, "TradeLevelLimit");
                    IEnumerable<XElement> waitingNodes = xmlEle.Elements();
                    foreach (var xmlNode in waitingNodes)
                    {
                        TradeLevelLimitConfig tradeLevelLimitConfig = new TradeLevelLimitConfig();
                        string ID = (string)Global.GetSafeAttributeStr(xmlNode, "ID");
                        string Day = (string)Global.GetSafeAttributeStr(xmlNode, "Day");
                        string Limit = (string)Global.GetSafeAttributeStr(xmlNode, "Limit");
                        tradeLevelLimitConfig.ID = Convert.ToInt32(ID);
                        tradeLevelLimitConfig.Day = Convert.ToInt32(Day);
                        tradeLevelLimitConfig.Limit = Limit;
                        tradeLevelLimitList.Add(tradeLevelLimitConfig);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("加载系统配置参数配置文件:{0}, 失败。{1} 节点配置错误！ {2}", fileName, "TradeLevelLimit", e.ToString()));
                }
            }
        }
    }
    class WaitingConfig
    {
        int _SeverID = 0;
        public int SeverID
        {
            get { return _SeverID; }
            set { _SeverID = value; }
        }
        /// <summary>
        /// 需要排队的人数
        /// </summary>
        int _NormalNeedWaitNumber = 0;
        public int NormalNeedWaitNumber
        {
            get { return _NormalNeedWaitNumber; }
            set { _NormalNeedWaitNumber = value; }
        }

        int _VIPNeedWaitNumber = 0;
        public int VIPNeedWaitNumber
        {
            get { return _VIPNeedWaitNumber; }
            set { _VIPNeedWaitNumber = value; }
        }
        /// <summary>
        /// 服务器最大人数
        /// </summary>
        int _NormalMaxNumber = 0;
        public int NormalMaxNumber
        {
            get { return _NormalMaxNumber; }
            set { _NormalMaxNumber = value; }
        }

        int _VIPMaxNumber = 0;
        public int VIPMaxNumber
        {
            get { return _VIPMaxNumber; }
            set { _VIPMaxNumber = value; }
        }
        /// <summary>
        /// 排队的队伍最大数量
        /// </summary>
        int _NormalWaitingMaxNumber = 0;
        public int NormalWaitingMaxNumber
        {
            get { return _NormalWaitingMaxNumber; }
            set { _NormalWaitingMaxNumber = value; }
        }

        int _VIPWaitingMaxNumber = 0;
        public int VIPWaitingMaxNumber
        {
            get { return _VIPWaitingMaxNumber; }
            set { _VIPWaitingMaxNumber = value; }
        }
        /// <summary>
        /// 玩家进入的最小时间间隔 毫秒
        /// </summary>
        int _NormalEnterMinInt = 0;
        public int NormalEnterMinInt
        {
            get { return _NormalEnterMinInt; }
            set { _NormalEnterMinInt = value; }
        }

        int _VIPEnterMinInt = 0;
        public int VIPEnterMinInt
        {
            get { return _VIPEnterMinInt; }
            set { _VIPEnterMinInt = value; }
        }
        /// <summary>
        ///  排队成功允许多久进入 毫秒
        /// </summary>
        int _NormalAllowMSecs = 0;
        public int NormalAllowMSecs
        {
            get { return _NormalAllowMSecs; }
            set { _NormalAllowMSecs = value; }
        }

        int _VIPAllowMSecs = 0;
        public int VIPAllowMSecs
        {
            get { return _VIPAllowMSecs; }
            set { _VIPAllowMSecs = value; }
        }

        /// <summary>
        ///   登出后多久允许任意登陆 毫秒
        /// </summary>
        int _NormalLogoutAllowMSecs = 0;
        public int NormalLogoutAllowMSecs
        {
            get { return _NormalLogoutAllowMSecs; }
            set { _NormalLogoutAllowMSecs = value; }
        }

        int _VIPLogoutAllowMSecs = 0;
        public int VIPLogoutAllowMSecs
        {
            get { return _VIPLogoutAllowMSecs; }
            set { _VIPLogoutAllowMSecs = value; }
        }
        /// <summary>
        /// 充值多少钱算vip
        /// </summary>
        int _VipExp = 0;
        public int VipExp
        {
            get { return _VipExp; }
            set { _VipExp = value; }
        }
        /// <summary>
        /// 返回普通用户的配置
        /// </summary>
        public string UserWaitConfig
        {
            get { return string.Format("{0},{1},{2},{3},{4},{5}", NormalNeedWaitNumber, NormalMaxNumber, NormalWaitingMaxNumber, NormalEnterMinInt, NormalAllowMSecs, NormalLogoutAllowMSecs); }
            // private set;
        }
        /// <summary>
        /// 返回VIP用户的配置
        /// </summary>
        public string VIPWaitConfig
        {
            get { return string.Format("{0},{1},{2},{3},{4},{5}", VIPNeedWaitNumber, VIPMaxNumber, VIPWaitingMaxNumber, VIPEnterMinInt, VIPAllowMSecs, VIPLogoutAllowMSecs); }
            // private set;
        }
        /// <summary>
        /// 返回充值多少钱算VIP
        /// </summary>
        public int LoginAllow_VIPExp
        {
            get { return VipExp; }
        }
    }

    /// <summary>
    /// 交易等级限制
    /// </summary>
    class TradeLevelLimitConfig
    {
        int _ID = 0;
        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }
        /// <summary>
        /// 开服过了多少天
        /// </summary>
        int _Day = 0;
        public int Day
        {
            get { return _Day; }
            set { _Day = value; }
        }
        /// <summary>
        /// 等级限制
        /// </summary>
        string _Limit = "";
        public string Limit
        {
            get { return _Limit; }
            set { _Limit = value; }
        }
    }
}
