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
using Server.TCP;
using Server.Protocol;

namespace GameServer.Logic
{
    public class GiftCodeNewManager : IManager
    {
        #region 接口相关

        private static GiftCodeNewManager instance = new GiftCodeNewManager();
        public static GiftCodeNewManager getInstance()
        {
            return instance;
        }

        private object _lockConfig = new object();

        public bool initialize()
        {
            if (!initGiftCode())
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

        #region 基本信息
        /// <summary>
        ///基本信息
        /// </summary>
        private static Dictionary<string, GiftCodeInfo> _GiftCodeCfgAwards = new Dictionary<string, GiftCodeInfo>();

        /// <summary>
        /// 功能是否开放
        /// </summary>
        public static bool IsFuncOpen()
        {
            return true;
            //return GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.GiftCodeNew);
        }

        /// <summary>
        /// 加载奖励配置
        /// </summary>
        public bool initGiftCode()
        {
            bool success = true;
            string fileName = "";
            string[] fields;
            string goods = "";

            lock (_lockConfig)
            {
                try
                {
                    _GiftCodeCfgAwards.Clear();
                    Dictionary<string, GiftCodeInfo> newDic = new Dictionary<string, GiftCodeInfo>();
                    fileName = Global.GameResPath("Config/GiftCodeNew.xml");

                    XElement xml = CheckHelper.LoadXml(fileName);
                    if (null == xml)
                    {
                        return false;
                    }

                    IEnumerable<XElement> xmlItems = xml.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (xmlItem == null) continue;

                        GiftCodeInfo info = new GiftCodeInfo();
                        info.GiftCodeTypeID = Global.GetDefAttributeStr(xmlItem, "TypeID", "");
                        info.GiftCodeName = Global.GetDefAttributeStr(xmlItem, "TypeName", "");
                        info.ChannelList.Clear();
                        string[] channel = Global.GetDefAttributeStr(xmlItem, "Channel", "").Split('|');
                        foreach (string p in channel)
                        {
                            if (!string.IsNullOrEmpty(p)) info.ChannelList.Add(p);
                        }

                        info.PlatformList.Clear();
                        string[] platform = Global.GetDefAttributeStr(xmlItem, "Platform", "").Split('|');
                        foreach (string p in platform)
                        {
                            if (!string.IsNullOrEmpty(p)) info.PlatformList.Add(Global.SafeConvertToInt32(p));
                        }

                        string timeBegin = Global.GetDefAttributeStr(xmlItem, "TimeBegin", "");
                        string timeEnd = Global.GetDefAttributeStr(xmlItem, "TimeEnd", "");
                        if (!string.IsNullOrEmpty(timeBegin)) info.TimeBegin = DateTime.Parse(timeBegin);
                        if (!string.IsNullOrEmpty(timeEnd)) info.TimeEnd = DateTime.Parse(timeEnd);

                        info.ZoneList.Clear();
                        string[] zone = Global.GetDefAttributeStr(xmlItem, "Zone", "").Split('|');
                        foreach (string p in zone)
                        {
                            if (!string.IsNullOrEmpty(p)) info.ZoneList.Add(Global.SafeConvertToInt32(p));
                        }

                        info.UserType = (GiftCodeUserType)Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "UserType", "0"));
                        info.UseCount = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "UseCount", "0"));

                        goods = Global.GetSafeAttributeStr(xmlItem, "GoodsOne");
                        if (!string.IsNullOrEmpty(goods))
                        {
                            fields = goods.Split('|');
                            if (fields.Length > 0)
                                info.GoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                        }

                        goods = Global.GetSafeAttributeStr(xmlItem, "GoodsTwo");
                        if (!string.IsNullOrEmpty(goods))
                        {
                            fields = goods.Split('|');
                            if (fields.Length > 0)
                                info.ProGoodsList = GoodsHelper.ParseGoodsDataList(fields, fileName);
                        }

                        newDic.Add(info.GiftCodeTypeID, info);
                    }
                    
                    _GiftCodeCfgAwards = newDic;
                }
                catch (System.Exception ex)
                {
                    success = false;
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("[GiftCodeNew]加载xml配置文件:{0}, 失败。", fileName), ex);
                }
            }

            return success;
        }

        /// <summary>
        /// 通过giftid获取礼包数据
        /// </summary>
        private GiftCodeInfo GetGiftCodeInfo(string giftid)
        {
            lock (_lockConfig)
            {
                GiftCodeInfo info = null;
                _GiftCodeCfgAwards.TryGetValue(giftid, out info);
                return info;
            }
        }

        #endregion


        #region 逻辑处理

        /// <summary>
        /// 网络处理
        /// </summary>
        public void ProcessGiftCodeList(string strcmd)
        {
            if (null == strcmd)
            {
                return;
            }
            if (!IsFuncOpen())
            {
                LogManager.WriteLog(LogTypes.Info, string.Format("[GiftCodeNew]礼包码功能未开放，礼包码信息={0}", strcmd));
                return;
            }
            try
            {
                string[] fields = strcmd.Split('#');
                if (fields.Length <= 0)
                {
                    return;
                }
                GiftCodeAwardData data = new GiftCodeAwardData();
                for (int i = 0; i < fields.Length; ++i)
                {
                    string[] GiftData = fields[i].Split(',');
                    //userid:rid:giftid:codeno
                    if (GiftData.Length != 4)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("[GiftCodeNew]ProcessGiftCodeList[{0}]参数错误。", fields[i]));
                        continue;
                    }
                    data.reset();
                    data.UserId = GiftData[0];
                    data.RoleID = Convert.ToInt32(GiftData[1]);
                    data.GiftId = GiftData[2];
                    data.CodeNo = GiftData[3];

                    if (data.RoleID <= 0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("[GiftCodeNew]ProcessGiftCodeList[{0}]角色id错误。", data.RoleID));
                        continue;
                    }
                    SendAward(data);
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "[GiftCodeNew]ProcessGiftCodeList error", false);
            }
        }

        /// <summary>
        /// 发送奖励
        /// </summary>
        void SendAward(GiftCodeAwardData ItemData)
        {
            if (null == ItemData)
            {
                return;
            }
            try
            {
                GiftCodeInfo gift = GetGiftCodeInfo(ItemData.GiftId);
                if (null == gift)
                {
                    AddLogEvent(ItemData, (int)GiftCodeResultType.EAware);
                    return;
                }

                if (null != gift.GoodsList)
                {
                    
                    int index = 0;
                    List<GoodsData> lTmp = new List<GoodsData>();
                    foreach (var item in gift.GoodsList)
                    {
                        index++;
                        lTmp.Add(item);
                        if (index % 5 == 0)
                        {
                            SendMailForGiftCode(lTmp, ItemData);
                            lTmp.Clear();
                        }
                    }
                    if (lTmp.Count > 0)
                    {
                        SendMailForGiftCode(lTmp, ItemData);
                        lTmp.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLogEvent(ItemData, (int)GiftCodeResultType.Exception);
                DataHelper.WriteFormatExceptionLog(ex, "[GiftCodeNew]SendAward error", false);
            }
        }

        /// <summary>
        /// 发邮件
        /// </summary>
        void SendMailForGiftCode(List<GoodsData> GoodList, GiftCodeAwardData ItemData)
        {
            if (null == GoodList || null == ItemData)
            {
                return;
            }
            string Content = Global.GetLang("礼包码邮件");
            string sSubject = string.Format(Global.GetLang("您的礼包码{0}使用成功。"), ItemData.CodeNo);
            bool bSuccess = Global.UseMailGivePlayerAward3(ItemData.RoleID, GoodList, Content, sSubject, 0);
            if (bSuccess)
            {
                GameClient client = GameManager.ClientMgr.FindClient(ItemData.RoleID);
                if (null != client)
                {
                    GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener,
                        Global._TCPManager.TcpOutPacketPool, client, Global.GetLang("您有新的邮件，请查收！"));
                }
                AddLogEvent(ItemData, (int)GiftCodeResultType.Success);
            }
            else
            {
                AddLogEvent(ItemData, (int)GiftCodeResultType.Fail);
            }
        }

        /// <summary>
        /// 记录log
        /// </summary>
        void AddLogEvent(GiftCodeAwardData ItemData, int result)
        {
            if (null == ItemData)
            {
                return;
            }

            EventLogManager.SystemRoleEvents[(int)RoleEvent.NewGiftCode].AddImporEvent(
                GameManager.ServerId,
                ItemData.UserId,
                CacheManager.GetZoneIdByRoleId(ItemData.RoleID, GameManager.ServerId),
                ItemData.RoleID,
                ItemData.GiftId,
                ItemData.CodeNo,
                result
                );
        }

        #endregion
       
    }
}
