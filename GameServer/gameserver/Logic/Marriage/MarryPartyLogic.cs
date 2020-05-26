using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Server.Tools;
using Server.TCP;
using Server.Protocol;
using GameServer.Server;
using GameServer.Logic;
using Server.Data;
using GameServer.Core.Executor;
using Tmsk.Contract;

namespace GameServer.Logic
{
    //<Config>
    //  <WeddingFeasttAward ID="1" Type="1" Name="普通婚宴" Icon="hunyan_01.png" Picture="5000"
    //   ConductBingJinBi="50000000" SumNum="1000" UseNum="10" BindJinBi="10000" EXPAward="10000" XingHunAward="1000" ShengWangAward="20" GoodWillRatio="200000"/>
    public class MarryPartyConfigData
    {
        public int PartyID;                   // 流水号
        public int PartyType;                 // 婚宴类別
        public int PartyCost;                 // 举办费用
        public int PartyMaxJoinCount;         // 可以參予总次数
        public int PlayerMaxJoinCount;        // 每玩家每天可參予次数
        public int JoinCost;                  // 參予费用

        // for player only
        public int RewardExp;                 // 获得经验
        public int RewardXingHun;             // 获得星魂
        public int RewardShengWang;           // 获得声望
        public AwardsItemData RewardItem;     // 友善度物品

        // for holder only
        public int GoodWillRatio;             // 获得奉献值
    }
    public class MarryPartyNPCData
    {
        public int MapCode;
        public int NpcID;
        public int NpcX;
        public int NpcY;
        public int NpcDir;
    }

    public enum MarryPartyResult
    {
        Success = 0,       // 成功
        PartyNotFound,
        InvalidParam,
        NotEnoughMoney,
        NotMarry,            // 未结婚
        AlreadyRequest,      // 已申请举办
        AlreadyStart,        // 已开始
        NotOriginator,       // 不是举办者
        NotStart,            // 未开始
        ZeroPartyJoinCount,  // 零參予次数
        ZeroPlayerJoinCount, // 零个人參予次数
        NotOpen,
    }

    public class MarryPartyLogic
    {
        #region 成员变量

        private MarryPartyDataCache m_MarryPartyDataCache = new MarryPartyDataCache();

        /// <summary>
        /// 配置数据
        /// </summary>
        private Dictionary<int, MarryPartyConfigData> MarryPartyConfigList = new Dictionary<int, MarryPartyConfigData>();
        private MarryPartyNPCData MarryPartyNPCConfig = new MarryPartyNPCData();
        private int MarryPartyPlayerMaxJoinCount;
        private int MarryPartyJoinListResetTime = 0;

        /// <summary>
        /// NPC是否刷新的标记
        /// 因为刷怪是异步操作，所以用这个做标记，防止生成多个Monster
        /// </summary>
        //private Object MarryPartyNPCShowMutex = new Object();
        private bool MarryPartyNPCShow = false;
        private NPC MarryPartyNpc = null;

        #endregion

        private static MarryPartyLogic Instance = new MarryPartyLogic();
        public static MarryPartyLogic getInstance()
        {
            return Instance;
        }

        #region 配置文件

        /// <summary>
        /// 加载配置文件 WeddingFeasttAward.xml
        /// </summary>
        public void LoadMarryPartyConfig()
        {
            lock (MarryPartyConfigList)
            {
                MarryPartyConfigList.Clear();

                string fileName = "Config/WeddingFeasttAward.xml";
                GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
                if (null == xml)
                    return;

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    MarryPartyConfigData data = new MarryPartyConfigData();
                    data.PartyID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    data.PartyType = (int)Global.GetSafeAttributeLong(xmlItem, "Type");
                    data.PartyCost = (int)Global.GetSafeAttributeLong(xmlItem, "ConductBindJinBi");
                    data.PartyMaxJoinCount = (int)Global.GetSafeAttributeLong(xmlItem, "SumNum");
                    data.PlayerMaxJoinCount = (int)Global.GetSafeAttributeLong(xmlItem, "UseNum");
                    data.JoinCost = (int)Global.GetSafeAttributeLong(xmlItem, "BindJinBi");
                    data.RewardExp = (int)Global.GetSafeAttributeLong(xmlItem, "EXPAward");
                    data.RewardXingHun = (int)Global.GetSafeAttributeLong(xmlItem, "XingHunAward");
                    data.RewardShengWang = (int)Global.GetSafeAttributeLong(xmlItem, "ShengWangAward");
                    data.GoodWillRatio = (int)Global.GetSafeAttributeLong(xmlItem, "GoodWillRatio");

                    string strGoodsAward = Global.GetSafeAttributeStr(xmlItem, "GoodsAward");
                    string[] fields = strGoodsAward.Split(',');
                    if (fields.Length == 7)
                    {
                        data.RewardItem = new AwardsItemData()
                        {
                            Occupation = 0,
                            RoleSex = 0,
                            GoodsID = Convert.ToInt32(fields[0]),
                            GoodsNum = Convert.ToInt32(fields[1]),
                            Binding = Convert.ToInt32(fields[2]),
                            Level = Convert.ToInt32(fields[3]),
                            AppendLev = Convert.ToInt32(fields[4]),           // 追加等级
                            IsHaveLuckyProp = Convert.ToInt32(fields[5]),     // 是否有幸运属性
                            ExcellencePorpValue = Convert.ToInt32(fields[6]), // 卓越属性值
                            EndTime = Global.ConstGoodsEndTime,
                        };
                    }

                    MarryPartyConfigList.Add(data.PartyType, data);
                }
            }

            string npcDataString = GameManager.systemParamsList.GetParamValueByName("HunYanNPC");

            string[] npcAttrString = npcDataString.Split(',');
            if (npcAttrString.Length >= 5)
            {
                int.TryParse(npcAttrString[0], out MarryPartyNPCConfig.MapCode);
                int.TryParse(npcAttrString[1], out MarryPartyNPCConfig.NpcID);
                int.TryParse(npcAttrString[2], out MarryPartyNPCConfig.NpcX);
                int.TryParse(npcAttrString[3], out MarryPartyNPCConfig.NpcY);
                int.TryParse(npcAttrString[4], out MarryPartyNPCConfig.NpcDir);
            }

            MarryPartyPlayerMaxJoinCount = (int)GameManager.systemParamsList.GetParamValueIntByName("HunYanUseMaxNum");
            MarryPartyJoinListResetTime = TimeUtil.NowDateTime().DayOfYear;

            MarryPartyQueryList();
        }

        /// <summary>
        /// 根据流水号取得相关配置
        /// </summary>
        private MarryPartyConfigData GetMarryPartyConfigData(int type)
        {
            MarryPartyConfigData data = null;
            lock (MarryPartyConfigList)
            {
                MarryPartyConfigList.TryGetValue(type, out data);
            }
            return data;
        }

        #endregion

        #region 逻辑相关
        public bool MarryPartyQueryList()
        {
            byte[] byteData = null;
            if (TCPProcessCmdResults.RESULT_FAILED ==
                Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_DB_MARRY_PARTY_QUERY, string.Format("{0}", GameManager.ServerLineID), out byteData, GameManager.LocalServerId))
            {
                m_MarryPartyDataCache.MarryPartyList = new Dictionary<int, MarryPartyData>();
                return false;
            }

            if (null == byteData || byteData.Length <= 6)
            {
                m_MarryPartyDataCache.MarryPartyList = new Dictionary<int, MarryPartyData>();
                return false;
            }

            Int32 length = BitConverter.ToInt32(byteData, 0);
            m_MarryPartyDataCache.MarryPartyList = DataHelper.BytesToObject<Dictionary<int, MarryPartyData>>(byteData, 6, length - 2);
            return true;
        }

        public bool MarryPartyJoinListClear(GameClient client, bool clearAll)
        {
            if (null == client.ClientData.MyMarryPartyJoinList)
                return false;

            client.ClientData.MyMarryPartyJoinList.Clear();

            int writeDB = (clearAll == true)? 2 : 1;
            if (clearAll == true)
            {
                int dayID = TimeUtil.NowDateTime().DayOfYear;
                if (MarryPartyJoinListResetTime == dayID)
                {
                    writeDB = 0;
                }
                else
                {
                    MarryPartyJoinListResetTime = dayID;
                }
            }

            byte[] byteData = null;
            if (TCPProcessCmdResults.RESULT_FAILED ==
                Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_DB_MARRY_PARTY_JOIN_CLEAR, string.Format("{0}:{1}", client.ClientData.RoleID, writeDB), out byteData, client.ServerId))
            {
                return false;
            }

            if (null == byteData || byteData.Length <= 6)
            {
                return false;
            }

            SendMarryPartyJoinList(client);
            return true;
        }

        public MarryPartyResult MarryPartyCreate(GameClient client, int partyType, long startTime)
        {
            if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                return MarryPartyResult.NotOpen;

            MarryPartyConfigData configData = GetMarryPartyConfigData(partyType);
            if (null == configData)
            {
                return MarryPartyResult.InvalidParam;
            }

            MarriageData marryData = client.ClientData.MyMarriageData;
            if (marryData.nSpouseID < 0 || marryData.byMarrytype < 0)
            {
                return MarryPartyResult.NotMarry;
            }

            int husbandRoleID = 0;
            int wifeRoleID = 0;
            string husbandName = "";
            string wifeName = "";
            if (1 == marryData.byMarrytype)
            {
                husbandRoleID = client.ClientData.RoleID;
                husbandName = client.ClientData.RoleName;
                wifeRoleID = marryData.nSpouseID;
                wifeName = MarryLogic.GetRoleName(marryData.nSpouseID);
            }
            else
            {
                husbandRoleID = marryData.nSpouseID;
                husbandName = MarryLogic.GetRoleName(marryData.nSpouseID);
                wifeRoleID = client.ClientData.RoleID;
                wifeName = client.ClientData.RoleName;
            }

            // 必须先添加，以防夫妻重复举行婚宴。。。想不通为什么找我
            MarryPartyData partyData = m_MarryPartyDataCache.AddParty(client.ClientData.RoleID, partyType, startTime, husbandRoleID, wifeRoleID, husbandName, wifeName);
            if (partyData == null)
            {
                return MarryPartyResult.AlreadyRequest;
            }

            MarryPartyResult result = MarryPartyResult.Success;

            byte[] byteData = null;
            TCPProcessCmdResults dbResult =
                Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_DB_MARRY_PARTY_ADD,
                    string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", client.ClientData.RoleID, partyType, startTime, husbandRoleID, wifeRoleID, husbandName, wifeName),
                    out byteData,
                    client.ServerId
                    );
            if (TCPProcessCmdResults.RESULT_FAILED == dbResult || null == byteData || byteData.Length <= 6)
            {
                result = MarryPartyResult.AlreadyRequest;
            }

            if (result == MarryPartyResult.Success)
            {
                // 检查举办所需金币是否足够
                if (configData.PartyCost > Global.GetTotalBindTongQianAndTongQianVal(client))
                {
                    result = MarryPartyResult.NotEnoughMoney;
                }
                if (configData.PartyCost > 0)
                {
                    // 扣除举办所需金币
                    if (Global.SubBindTongQianAndTongQian(client, configData.PartyCost, "举办婚宴") == false)
                    {
                        result = MarryPartyResult.NotEnoughMoney;
                    }
                }
            }

            if (result != MarryPartyResult.Success)
            {
                if (dbResult != TCPProcessCmdResults.RESULT_FAILED)
                {
                    try
                    {
                        Global.SendAndRecvData((int)TCPGameServerCmds.CMD_DB_MARRY_PARTY_REMOVE,
                            string.Format("{0}", client.ClientData.RoleID),
                            client.ServerId
                            );
                    }
                    catch (Exception)
                    {
                    }
                }
                m_MarryPartyDataCache.RemoveParty(client.ClientData.RoleID);
                return result;
            }

            Int32 length = BitConverter.ToInt32(byteData, 0);
            MarryPartyData dbPartyData = DataHelper.BytesToObject<MarryPartyData>(byteData, 6, length - 2);

            m_MarryPartyDataCache.SetPartyTime(partyData, dbPartyData.StartTime);

            SendMarryPartyList(client, partyData);

            //lock (MarryPartyNPCShowMutex)
            //{
            //    if (MarryPartyNPCShow == false)
            //    {
            //        // TODO: add npc
            //        MarryPartyNPCShow = true;
            //    }
            //}

            return result;
        }

        public MarryPartyResult MarryPartyCancel(GameClient client)
        {
            if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                return MarryPartyResult.NotOpen;

            MarryPartyData partyData = m_MarryPartyDataCache.GetParty(client.ClientData.MyMarriageData.nSpouseID);
            if (partyData != null)
            {
                return MarryPartyResult.NotOriginator;
            }

            return MarryPartyRemove(client.ClientData.RoleID, false, client);
        }

        public MarryPartyResult MarryPartyRemove(int roleID, bool forceRemove, GameClient client)
        {
            MarryPartyData partyData = m_MarryPartyDataCache.GetParty(roleID);
            if (partyData == null)
            {
                return MarryPartyResult.PartyNotFound;
            }

            if (forceRemove == false && partyData.StartTime <= TimeUtil.NOW())
            {
                return MarryPartyResult.AlreadyStart;
            }

            MarryPartyConfigData configData = GetMarryPartyConfigData(partyData.PartyType);
            if (null == configData)
            {
                return MarryPartyResult.InvalidParam;
            }

            if (MarryPartyRemoveInternal(roleID, forceRemove, client, partyData) == false)
            {
                return MarryPartyResult.PartyNotFound;
            }

            if (forceRemove == false)
            {
                // 手动取消，直接返绑金
                GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client,
                    configData.PartyCost, "婚宴退款", false);

                SendMarryPartyList(client, new MarryPartyData());

                //if (m_MarryPartyDataCache.GetPartyCount() <= 0)
                //{
                //    lock (MarryPartyNPCShowMutex)
                //    {
                //        if (MarryPartyNPCShow == true)
                //        {
                //            // TODO: add npc
                //            MarryPartyNPCShow = false;
                //        }
                //    }
                //}
            }
            else
            {
                // 被动强制取消, 如果婚宴沒开始, 邮件返绑金
                if (partyData.StartTime > TimeUtil.NOW())
                {
                    GoodsData goodData = Global.GetNewGoodsData(50014, 1);
                    goodData.GCount = configData.PartyCost / 10000;
                    List<GoodsData> goodList = new List<GoodsData>();
                    goodList.Add(goodData);

                    Global.UseMailGivePlayerAward3(roleID, goodList, "婚宴取消", "你的婚宴被取消，返回金币请查收！", 0);
                }
            }

            return MarryPartyResult.Success;
        }

        private bool MarryPartyRemoveInternal(int roleID, bool forceRemove, GameClient self, MarryPartyData partyData = null)
        {
            if (m_MarryPartyDataCache.RemoveParty(roleID) == false)
            {
                return false;
            }

            byte[] byteData = null;
            TCPProcessCmdResults dbResult =
                Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_DB_MARRY_PARTY_REMOVE,
                    string.Format("{0}", roleID),
                    out byteData,
                    self.ServerId
                    );
            if (dbResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                if (forceRemove == false)
                {
                    m_MarryPartyDataCache.RemovePartyCancel(partyData);
                    return false;
                }
            }
            return true;
        }

        public MarryPartyResult MarryPartyJoin(GameClient client, int roleID)
        {
            if (!MarryLogic.IsVersionSystemOpenOfMarriage())
                return MarryPartyResult.NotOpen;

            MarryPartyData partyData = m_MarryPartyDataCache.GetParty(roleID);
            if (partyData == null)
            {
                return MarryPartyResult.PartyNotFound;
            }

            if (partyData.StartTime > TimeUtil.NOW())
            {
                return MarryPartyResult.NotStart;
            }

            MarryPartyConfigData configData = GetMarryPartyConfigData(partyData.PartyType);
            if (null == configData)
            {
                return MarryPartyResult.PartyNotFound;
            }

            // 检查參予所需金币是否足够
            if (configData.JoinCost > Global.GetTotalBindTongQianAndTongQianVal(client))
            {
                return MarryPartyResult.NotEnoughMoney;
            }

            // 检查參予次数
            Dictionary<int, int> joinList = client.ClientData.MyMarryPartyJoinList;
            int targetPartyJoinCount = 0;
            int allPartyJoinCount = 0;
            bool remove = false;

            lock (joinList)
            {
                joinList.TryGetValue(roleID, out targetPartyJoinCount);

                foreach (KeyValuePair<int, int> kv in client.ClientData.MyMarryPartyJoinList)
                {
                    allPartyJoinCount += kv.Value;
                }

                if (allPartyJoinCount >= MarryPartyPlayerMaxJoinCount)
                {
                    return MarryPartyResult.ZeroPlayerJoinCount;
                }
                if (targetPartyJoinCount >= configData.PlayerMaxJoinCount)
                {
                    return MarryPartyResult.ZeroPlayerJoinCount;
                }

                // 增加參予次数
                if (m_MarryPartyDataCache.IncPartyJoin(roleID, configData.PartyMaxJoinCount, out remove) == false)
                {
                    return MarryPartyResult.ZeroPartyJoinCount;
                }

                ++targetPartyJoinCount;

                byte[] byteData = null;
                TCPProcessCmdResults dbResult =
                    Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                        (int)TCPGameServerCmds.CMD_DB_MARRY_PARTY_JOIN_INC,
                        string.Format("{0}:{1}:{2}", roleID, client.ClientData.RoleID, targetPartyJoinCount),
                        out byteData,
                        client.ServerId
                        );
                if (TCPProcessCmdResults.RESULT_FAILED == dbResult || null == byteData || byteData.Length <= 6)
                {
                    m_MarryPartyDataCache.IncPartyJoinCancel(roleID);
                    return MarryPartyResult.ZeroPartyJoinCount;
                }

                joinList[roleID] = targetPartyJoinCount;
            }

            if (configData.JoinCost > 0)
            {
                // 扣除參予所需金币
                if (Global.SubBindTongQianAndTongQian(client, configData.JoinCost, "參予婚宴") == false)
                {
                    // TODO: 沒处理參予次数已更新数据库的问题
                    return MarryPartyResult.NotEnoughMoney;
                }
            }

            if (configData.RewardExp > 0)
                GameManager.ClientMgr.ProcessRoleExperience(client, configData.RewardExp, false);

            if (configData.RewardShengWang > 0)
                GameManager.ClientMgr.ModifyShengWangValue(client, configData.RewardShengWang, "婚宴奖励", false);

            if (configData.RewardXingHun > 0)
                GameManager.ClientMgr.ModifyStarSoulValue(client, configData.RewardXingHun, "婚宴奖励", false);

            if (remove == true)
            {
                MarryPartyRemoveInternal(roleID, true, client);

                // 婚宴结算, 换成友善度物品, 个数双方平分
                GoodsData goodData = Global.GetNewGoodsData(configData.RewardItem.GoodsID, configData.RewardItem.Binding);
                goodData.GCount = configData.JoinCost * configData.PartyMaxJoinCount / configData.GoodWillRatio / 2;
                List<GoodsData> goodList = new List<GoodsData>();
                goodList.Add(goodData);

                string sMsg = Global.GetLang("恭喜您成功举办了一场盛大的婚宴，宾客们馈赠的礼物已随这封邮件发送到您手上，请查收附件。");
                Global.UseMailGivePlayerAward3(roleID, goodList, Global.GetLang("婚宴"), sMsg, 0);

                int spouseID = (roleID == partyData.HusbandRoleID)? partyData.WifeRoleID : partyData.HusbandRoleID;
                Global.UseMailGivePlayerAward3(spouseID, goodList, Global.GetLang("婚宴"), sMsg, 0);
            }

            SendMarryPartyJoinList(client);
            SendMarryPartyList(client, partyData);

            //if (m_MarryPartyDataCache.GetPartyCount() <= 0)
            //{
            //    lock (MarryPartyNPCShowMutex)
            //    {
            //        if (MarryPartyNPCShow == true)
            //        {
            //            // TODO: add npc
            //            MarryPartyNPCShow = false;
            //        }
            //    }
            //}

            return MarryPartyResult.Success;
        }

        private long NextUpdateTime = 0;
        public void MarryPartyPeriodicUpdate(long ticks)
        {
            // 10秒检测一次
            if (ticks < NextUpdateTime)
            {
                return;
            }
            NextUpdateTime = ticks + 1000 * 10;

            bool showNPC = m_MarryPartyDataCache.HasPartyStarted(ticks);
            if (showNPC != MarryPartyNPCShow)
            {
                MarryPartyNPCShow = showNPC;

                if (showNPC == true)
                {
                    GameMap gameMap = GameManager.MapMgr.DictMaps[MarryPartyNPCConfig.MapCode];
                    NPC npc = NPCGeneralManager.GetNPCFromConfig(MarryPartyNPCConfig.MapCode, MarryPartyNPCConfig.NpcID, MarryPartyNPCConfig.NpcX, MarryPartyNPCConfig.NpcY, MarryPartyNPCConfig.NpcDir);
                    if (null != npc)
                    {
                        if (NPCGeneralManager.AddNpcToMap(npc))
                        {
                            MarryPartyNpc = npc;
                        }
                        else
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("add marry party npc failure, MapCode={0}, NpcID={1}", MarryPartyNPCConfig.MapCode, MarryPartyNPCConfig.NpcID));
                        }
                    }
                }
                else
                {
                    if (null != MarryPartyNpc)
                    {
                        NPCGeneralManager.RemoveMapNpc(MarryPartyNPCConfig.MapCode, MarryPartyNPCConfig.NpcID);
                        MarryPartyNpc = null;
                    }
                }
            }
        }

        // TODO: client should send spouse id
        // 发送婚宴信息
        public void SendMarryPartyList(GameClient client, MarryPartyData partyData, int roleID = -1)
        {
            Dictionary<int, MarryPartyData> marryPartyList;

            if (partyData != null || roleID > 0)
            {
                if (partyData == null)
                {
                    // 获取指定角色婚宴数据
                    partyData = m_MarryPartyDataCache.GetParty(roleID);
                    if (partyData == null)
                    {
                        int SpouseID = MarryLogic.GetSpouseID(roleID);
                        partyData = m_MarryPartyDataCache.GetParty(SpouseID);
                        if (partyData == null)
                        {
                            // 返回空代表通知客戶端自己沒有婚宴
                            partyData = new MarryPartyData();
                        }
                    }
                }

                marryPartyList = new Dictionary<int, MarryPartyData>();
                marryPartyList.Add(roleID, partyData);

                client.sendCmd<Dictionary<int, MarryPartyData>>((int)TCPGameServerCmds.CMD_SPR_MARRY_PARTY_QUERY, marryPartyList);
            }
            else
            {
                // 获取全部婚宴列表
                client.sendCmd(m_MarryPartyDataCache.GetPartyList(TCPOutPacketPool.getInstance(), (int)TCPGameServerCmds.CMD_SPR_MARRY_PARTY_QUERY));
            }
        }

        // 发送参加婚宴次数信息
        public void SendMarryPartyJoinList(GameClient client)
        {
            if (null == client.ClientData.MyMarryPartyJoinList)
                return;

            client.sendCmd<Dictionary<int, int>>((int)TCPGameServerCmds.CMD_SPR_MARRY_PARTY_JOIN_LIST, client.ClientData.MyMarryPartyJoinList);
        }
        #endregion

        #region 协议处理
        public TCPProcessCmdResults ProcessMarryPartyQuery(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID));

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                int roleID = Convert.ToInt32(fields[0]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.ClientSocket.IsKuaFuLogin)
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_MARRY_PARTY_QUERY, new Dictionary<int, MarryPartyData>());
                }
                else
                {
                    //return Global.RequestToDBServer2(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_MARRY_PARTY_QUERY, string.Format("{0}", GameManager.ServerLineID), out tcpOutPacket);
                    SendMarryPartyList(client, null, roleID);
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
            return TCPProcessCmdResults.RESULT_DATA;
        }

        public TCPProcessCmdResults ProcessMarryPartyCreate(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int partyType = Convert.ToInt32(fields[1]);
                long startTime = Convert.ToInt64(fields[2]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.ClientSocket.IsKuaFuLogin)
                {
                    client.sendCmd(nID, string.Format("{0}:{1}", (int)StdErrorCode.Error_Operation_Denied, roleID));
                    return TCPProcessCmdResults.RESULT_OK;
                }

                MarryPartyResult result = MarryPartyCreate(client, partyType, startTime);

                string strcmd = string.Format("{0}:{1}", (int)result, roleID);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessMarryPartyCreate", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public TCPProcessCmdResults ProcessMarryPartyCancel(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.ClientSocket.IsKuaFuLogin)
                {
                    client.sendCmd(nID, string.Format("{0}:{1}", (int)StdErrorCode.Error_Operation_Denied, roleID));
                    return TCPProcessCmdResults.RESULT_OK;
                }

                MarryPartyResult result = MarryPartyCancel(client);

                string strcmd = string.Format("{0}:{1}", (int)result, roleID);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessMarryPartyCancel", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public TCPProcessCmdResults ProcessMarryPartyJoin(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int holderRoleID = Convert.ToInt32(fields[1]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                MarryPartyResult result = MarryPartyJoin(client, holderRoleID);

                string strcmd = string.Format("{0}:{1}", (int)result, roleID);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessJoinQingGongYanCMD", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        // add by chenjingui. 20150704, 角色改名，检测修改婚宴数据
        public void OnChangeName(int roleId, string oldName, string newName)
        {
            m_MarryPartyDataCache.OnChangeName(roleId, oldName, newName);
        }

        #endregion
    }
}
