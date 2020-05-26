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
using GameServer.Core.Executor;
using System.IO;

namespace GameServer.Logic
{
    public class TenRetutnManager : IManager
    {
        #region 接口相关

        private static TenRetutnManager instance = new TenRetutnManager();
        public static TenRetutnManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            if (!InitConfig())
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

        public TenRetutnData RuntimeData = new TenRetutnData();

        /// <summary>
        /// 加载奖励配置
        /// </summary>
        public bool InitConfig()
        {
            lock (RuntimeData.Mutex)
            {
                RuntimeData.SystemOpen = false;
                RuntimeData._tenUserIdAwardsDict.Clear();

                string fileName = Global.GameResPath("Config/TenRetutnAward.xml");
                XElement xml = CheckHelper.LoadXml(fileName);
                if (null == xml) return true;

                try
                {
                    RuntimeData._tenAwardDic.Clear();

                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (xmlItem == null) continue;

                        TenRetutnAwardsData config = new TenRetutnAwardsData();
                        config.ID = Convert.ToInt32(Global.GetSafeAttributeLong(xmlItem, "ID"));
                        //config.ChongZhiZhuanShi = Convert.ToInt32(Global.GetSafeAttributeLong(xmlItem, "ChongZhiZhuanShi"));
                        config.MailUser = Global.GetLang("系统");
                        config.MailTitle = Global.GetSafeAttributeStr(xmlItem, "MailTitle");
                        config.MailContent = Global.GetSafeAttributeStr(xmlItem, "MailContent");
                        ConfigParser.ParseAwardsItemList(Global.GetDefAttributeStr(xmlItem, "GoodsID1", ""), ref config.GoodsID1);
                        ConfigParser.ParseAwardsItemList(Global.GetDefAttributeStr(xmlItem, "GoodsID2", ""), ref config.GoodsID2);
                        config.UserList = Global.GetSafeAttributeStr(xmlItem, "UserList");

                        RuntimeData._tenAwardDic.Add(config.ID, config);

                        fileName = Global.GameResPath("Config/" + config.UserList);
                        if (File.Exists(fileName))
                        {
                            string[] allUserIds = File.ReadAllLines(fileName);
                            foreach (var userid in allUserIds)
                            {
                                if (!string.IsNullOrEmpty(userid))
                                {
                                    RuntimeData._tenUserIdAwardsDict[userid.ToLower()] = config.ID;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Warning, "加载Config/TenRetutnAward.xml时文件出现异常!!!", ex);
                }

                fileName = Global.GameResPath("Config/TenRetutnTime.xml");
                xml = CheckHelper.LoadXml(fileName);
                if (null == xml) return true;

                try
                {
                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        RuntimeData.SystemOpen = true;
                        string beginTime = Global.GetDefAttributeStr(xmlItem, "BeginTime", "2019-12-31");
                        string finishTime = Global.GetDefAttributeStr(xmlItem, "FinishTime", "2011-11-11");
                        string notLoggedInBegin = Global.GetDefAttributeStr(xmlItem, "NotLoggedInBegin", "2011-11-11");
                        string notLoggedInFinish = Global.GetDefAttributeStr(xmlItem, "NotLoggedInFinish", "2019-12-31");
                        RuntimeData.BeginTimeStr = beginTime.Replace(':', '$');
                        RuntimeData.FinishTimeStr = finishTime.Replace(':', '$');
                        RuntimeData.SystemOpen &= DateTime.TryParse(beginTime, out RuntimeData.BeginTime);
                        RuntimeData.SystemOpen &= DateTime.TryParse(finishTime, out RuntimeData.FinishTime);
                        RuntimeData.SystemOpen &= DateTime.TryParse(notLoggedInBegin, out RuntimeData.NotLoggedInBegin);
                        RuntimeData.SystemOpen &= DateTime.TryParse(notLoggedInFinish, out RuntimeData.NotLoggedInFinish);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Warning, "加载Config/TenRetutnTime.xml时文件出现异常!!!", ex);
                }
            }

            return true;
        }

        public void GiveAwards(GameClient client)
        {
            if (!RuntimeData.SystemOpen)
            {
                return;
            }

            if (RuntimeData.BeginTime <= Global.GetKaiFuTime())
            {
                RuntimeData.SystemOpen = false;
                return;
            }

            DateTime now = TimeUtil.NowDateTime();
            if (now < RuntimeData.BeginTime || now > RuntimeData.FinishTime)
            {
                return;
            }

            TenRetutnAwardsData data = null;
            lock (RuntimeData.Mutex)
            {
                int id;
                if (!RuntimeData._tenUserIdAwardsDict.TryGetValue(client.strUserID.ToLower(), out id))
                {
                    return;
                }

                if (!RuntimeData._tenAwardDic.TryGetValue(id, out data))
                {
                    return;
                }
            }

            if (null == data)
            {
                //充值达不到要求的条件
                return;
            }

            string keyStr = string.Format("{0}_{1}_{2}", RuntimeData.BeginTimeStr, RuntimeData.FinishTimeStr, client.ClientData.ZoneID);
            string[] result = Global.QeuryUserActivityInfo(client, keyStr, (int)ActivityTypes.TenReturn);
            if (null == result || result.Length == 0)
            {
                //数据库访问失败,本次不做其他操作
                return;
            }

            int ret = Global.SafeConvertToInt32(result[0]);
            int hasGetTimes = Global.SafeConvertToInt32(result[3]);
            if (hasGetTimes > 0)
            {
                //已经发放过奖励
                return;
            }

            List<AwardsItemData> itemList = new List<AwardsItemData>(data.GoodsID1.Items);
            foreach (var item in data.GoodsID2.Items)
            {
                if (Global.IsCanGiveRewardByOccupation(client, item.GoodsID))
                {
                    itemList.Add(item);
                }
            }

            //发放奖励
            ret = Global.UseMailGivePlayerAward2(client, itemList, data.MailTitle, data.MailContent);
            if (ret >= 0)
            {
                //更新活动状态
                Global.UpdateUserActivityInfo(client, keyStr, (int)ActivityTypes.TenReturn, 1, now.ToString("yyyy-MM-dd HH$mm$ss"));
            }
        }

        #endregion
    }
}
