using GameServer.Core.GameEvent;
using GameServer.Server;
using GameServer.Tools;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic.Ten
{
    public class TenManager : IManager
    {
        #region 接口相关

        private static TenManager instance = new TenManager();
        public static TenManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            if (!initConfig())
                return false;

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

        #endregion


        #region 配置信息

        /// <summary>
        ///基本信息
        /// </summary>
        private static Dictionary<int, TenAwardData> _tenAwardDic = new Dictionary<int, TenAwardData>();

        /// <summary>
        /// 加载奖励配置
        /// </summary>
        public static bool initConfig()
        {
            string fileName = Global.GameResPath("Config/TenAward.xml");
            XElement xml = CheckHelper.LoadXml(fileName);
            if (null == xml) return false;

            try
            {
                _tenAwardDic.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    TenAwardData config = new TenAwardData();
                    config.AwardID = Convert.ToInt32(Global.GetSafeAttributeLong(xmlItem, "ID"));
                    config.AwardName = Global.GetSafeAttributeStr(xmlItem, "Name");
                    config.DbKey = Global.GetSafeAttributeStr(xmlItem, "DbKey");
                    config.DayMaxNum = Convert.ToInt32(Global.GetSafeAttributeLong(xmlItem, "DayMaxNum"));
                    config.OnlyNum = Convert.ToInt32(Global.GetSafeAttributeLong(xmlItem, "OnlyNum"));

                    config.MailUser = Global.GetLang("系统");
                    config.MailTitle = Global.GetSafeAttributeStr(xmlItem, "MailTitle");
                    config.MailContent = Global.GetSafeAttributeStr(xmlItem, "MailContent");

                    string beginTime = Global.GetDefAttributeStr(xmlItem, "BeginDate", "");
                    string endTime = Global.GetDefAttributeStr(xmlItem, "EndDate", "");
                    string roleLevel = Global.GetDefAttributeStr(xmlItem, "Level", "0,1");

                    if (string.IsNullOrEmpty(beginTime)) config.BeginTime = DateTime.MinValue;
                    else config.BeginTime = DateTime.Parse(beginTime);

                    if (string.IsNullOrEmpty(endTime))  config.EndTime = DateTime.MaxValue;
                    else config.EndTime = DateTime.Parse(endTime);

                    string[] arrLevel = roleLevel.Split(',');
                    config.RoleLevel = int.Parse(arrLevel[0]) * 100 + int.Parse(arrLevel[1]);

                    string awards = Global.GetSafeAttributeStr(xmlItem, "AwardGoods");
                    if (!string.IsNullOrEmpty(awards))
                    {  
                        string[] awardsArr = awards.Split('|');
                        config.AwardGoods = GoodsHelper.ParseGoodsDataList(awardsArr,fileName);
                    }

                    _tenAwardDic.Add(config.AwardID, config);
                }

                initDb();
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, "加载Config/TenAward.xml时文件出现异常!!!");
                Process.GetCurrentProcess().Kill();
                return false;
            }

            return true;
        }

        public static void initDb()
        {
            // 如果1.7的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot7))
                return ;

            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.Ten))
                return;
            
            string dbCmds = "";
            foreach (var item in _tenAwardDic.Values)
            {
                if (dbCmds.Length > 0)
                    dbCmds += "#";

                string goodsStr = "";
                if (item.AwardGoods != null && item.AwardGoods.Count > 0)
                {
                    foreach (var goods in item.AwardGoods)
                    {
                        if (goodsStr.Length > 0)
                            goodsStr += "|";

                        goodsStr += string.Format("{0},{1},{2}", goods.GoodsID, goods.GCount, goods.Binding);
                    }
                }

                dbCmds += string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}",
                    item.AwardID, item.DbKey, item.OnlyNum, item.DayMaxNum, goodsStr,
                    item.MailTitle, item.MailContent, item.MailUser, item.BeginTime.ToString("yyyyMMddHHmmss"), item.EndTime.ToString("yyyyMMddHHmmss"), item.RoleLevel);
            }

            string[] dbFields = null;
            Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                (int)TCPGameServerCmds.CMD_DB_TEN_INIT, dbCmds, out dbFields, GameManager.LocalServerId);
        }



        #endregion
    }
}
