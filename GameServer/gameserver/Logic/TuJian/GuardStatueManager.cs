using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Server.Tools.Pattern;
using Server.Tools;
using Server.Data;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using GameServer.Core.Executor;

namespace GameServer.Logic.TuJian
{
    /// <summary>
    /// 守护雕像系统
    /// </summary>
    public class GuardStatueManager : SingletonTemplate<GuardStatueManager>
    {
        private GuardStatueManager() { }

        #region 配置文件数据
        // 守护之灵配置 key:守护之灵Type，对应图鉴Type
        private Dictionary<int, GuardSoul> guardSoulDict = new Dictionary<int, GuardSoul>();
        // 守护点配置 key：精魄id，对应图鉴item
        private Dictionary<int, GuardPoint> guardPointDict = new Dictionary<int, GuardPoint>();
        // 守护雕像升级配置 key：等级
        private Dictionary<int, GuardLevelUp> guardLevelUpDict = new Dictionary<int, GuardLevelUp>();
        // 守护雕像升阶配置 key: 品阶
        private Dictionary<int, GuardSuitUp> guardSuitUpDict = new Dictionary<int, GuardSuitUp>();

        // 每日最大可回收的守护点限制，item1：守护之灵个数，item2：每日最大可回收守护点
        private List<Tuple<int, int>> dayMaxCanRecoverPointList = new List<Tuple<int, int>>();
        // 最多激活守护之灵装备栏限制，item1：守护之灵个数，item2：最多可激活守护之灵装备栏
        private List<Tuple<int, int>> maxActiveSlotCntList = new List<Tuple<int, int>>();

        // 守护雕像最大等级，读取升阶配置文件时初始化
        private int GuardStatueMaxLevel = 0;
        // 守护雕像最大品阶，读取升阶配置文件时初始化
        private int GuardStatueMaxSuit = 0;

        // 守护雕像等级对属性的加成因子
        public double LevelFactor = GuardStatueConst.DefaultLevelFactor;
        // 守护雕像品阶对属性的加成因子
        public double SuitFactor = GuardStatueConst.DefaultSuitFactor;
        #endregion 配置文件数据

        #region 加载配置文件
        public void LoadConfig()
        {
            if (!loadGuardSoul() || !loadGuardPoint() || !loadGuardLevelUp() || !loadGuardSuitUp())
            {
            }
        }

        // 加载守护之灵配置文件
        private bool loadGuardSoul()
        {
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(GuardStatueConst.GuardSoulCfgFile));
                if (xml == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("{0} 不存在!", Global.GameResPath(GuardStatueConst.GuardSoulCfgFile)));
                    return false;
                }

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem != null)
                    {
                        GuardSoul soul = new GuardSoul();
                        soul.TypeID = (int)Global.GetSafeAttributeLong(xmlItem, "Type");
                        soul.Name = Global.GetSafeAttributeStr(xmlItem, "Name");
                        soul.GoodsID = (int)Global.GetSafeAttributeLong(xmlItem, "GoodsID");
                        guardSoulDict.Add(soul.TypeID, soul);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("{0} 读取出错!", Global.GameResPath(GuardStatueConst.GuardSoulCfgFile)), ex);
                return false;
            }

            return true;
        }

        // 加载精魄回收配置文件
        private bool loadGuardPoint()
        {
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(GuardStatueConst.GuardPointCfgFile));
                if (xml == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("{0} 不存在!", Global.GameResPath(GuardStatueConst.GuardPointCfgFile)));
                    return false;
                }

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem != null)
                    {
                        GuardPoint point = new GuardPoint();
                        point.ItemID = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        point.TypeID = (int)Global.GetSafeAttributeLong(xmlItem, "Type");
                        point.Name = Global.GetSafeAttributeStr(xmlItem, "Name");
                        point.GoodsID = (int)Global.GetSafeAttributeLong(xmlItem, "GoodsID");
                        point.Point = (int)Global.GetSafeAttributeLong(xmlItem, "ShouHuAward");
                        guardPointDict.Add(point.ItemID, point);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("{0} 读取出错!", Global.GameResPath(GuardStatueConst.GuardPointCfgFile)), ex);
                return false;
            }

            return true;
        }

        // 加载守护雕像升级配置文件
        private bool loadGuardLevelUp()
        {
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(GuardStatueConst.GuardLevelUpCfgFile));
                if (xml == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("{0} 不存在!", Global.GameResPath(GuardStatueConst.GuardLevelUpCfgFile)));
                    return false;
                }

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem != null)
                    {
                        GuardLevelUp levelUp = new GuardLevelUp();
                        levelUp.Level = (int)Global.GetSafeAttributeLong(xmlItem, "Level");
                        levelUp.NeedGuardPoint = (int)Global.GetSafeAttributeLong(xmlItem, "NeedShouHu");
                        guardLevelUpDict.Add(levelUp.Level, levelUp);

                        GuardStatueMaxLevel = Math.Max(GuardStatueMaxLevel, levelUp.Level);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("{0} 读取出错!", Global.GameResPath(GuardStatueConst.GuardLevelUpCfgFile)), ex);
                return false;
            }

            return true;
        }

        // 加载守护雕像升阶配置文件
        private bool loadGuardSuitUp()
        {
            try
            {
                XElement xml = XElement.Load(Global.GameResPath(GuardStatueConst.GuardSuitUpCfgFile));
                if (xml == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("{0} 不存在!", Global.GameResPath(GuardStatueConst.GuardSuitUpCfgFile)));
                    return false;
                }

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem != null)
                    {
                        GuardSuitUp suitUp = new GuardSuitUp();
                        suitUp.Suit = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                        //suitUp.NeedGuardPoint = (int)Global.GetSafeAttributeLong(xmlItem, "NeedShouHu");
                        string szGoods = Global.GetSafeAttributeStr(xmlItem, "NeedGoods");
                        string[] szNeedGoods = szGoods.Split(',');
                        suitUp.NeedGoodsID = Convert.ToInt32(szNeedGoods[0]);
                        suitUp.NeedGoodsCnt = Convert.ToInt32(szNeedGoods[1]);
                        guardSuitUpDict.Add(suitUp.Suit, suitUp);

                        GuardStatueMaxSuit = Math.Max(GuardStatueMaxSuit, suitUp.Suit);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("{0} 读取出错!", Global.GameResPath(GuardStatueConst.GuardSuitUpCfgFile)), ex);
                return false;
            }

            return true;
        }
        #endregion 加载配置文件

        #region 检测守护之灵开启信息，以及属性计算
        public void OnTaskComplete(GameClient client)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return;
            }

            if (client != null && !client.ClientData.MyGuardStatueDetail.IsActived)
            {
                CheckGuardStatueOpenInfo(client);
            }
        }

        public void OnLogin(GameClient client)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return;
            }

            if (client != null)
            {
                CheckGuardStatueOpenInfo(client);
                UpdateGuardStatueProps(client);
            }
        }

        public void OnActiveTuJian(GameClient client)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return;
            }

            if (client != null)
            {
                CheckGuardStatueOpenInfo(client);
            }
        }

        // 检查守护雕像和守护之灵的激活信息，角色上线、完成指定任务、图鉴激活时调用
        private void CheckGuardStatueOpenInfo(GameClient client)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return ;
            }

            if (client == null) return;

            if (!client.ClientData.MyGuardStatueDetail.IsActived)
            {
                // 守护雕像尚未激活，检查指定任务是否完成
                if (GlobalNew.IsGongNengOpened(client, GongNengIDs.GuardStatue))
                {
                    client.ClientData.MyGuardStatueDetail.IsActived = true;
                    client.ClientData.MyGuardStatueDetail.GuardStatue.Level = GuardStatueConst.GuardStatueDefaultLevel;
                    client.ClientData.MyGuardStatueDetail.GuardStatue.Suit = GuardStatueConst.GuardStatueDefaultSuit;
                    client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint = 0;
                    client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList.Clear();
                    client.ClientData.MyGuardStatueDetail.LastdayRecoverPoint = 0;
                    client.ClientData.MyGuardStatueDetail.LastdayRecoverOffset = Global.GetOffsetDay(TimeUtil.NowDateTime());
                    client.ClientData.MyGuardStatueDetail.ActiveSoulSlot = 0;

                    // 新激活，直接写数据库，防止守护雕像写数据库失败了，还继续写守护之灵到数据库
                    if (!_UpdateGuardStatue2DB(client))
                    {
                        // 写失败了，回滚，设为未激活
                        client.ClientData.MyGuardStatueDetail.IsActived = false;
                    }
                }
            }

            if (!client.ClientData.MyGuardStatueDetail.IsActived)
            {
                return;
            }

            // 对于每一个激活的图鉴Type，都需要得到一个守护之灵
            foreach (int type in client.ClientData.ActivedTuJianType)
            {
                // 已激活，但是没有守护之灵
                if (!client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList.Exists((soul) => { return soul.Type == type; }))
                {
                    GuardSoulData soulData = new GuardSoulData() { Type = type, EquipSlot = -1 };
                    if (_UpdateGuardSoul2DB(client.ClientData.RoleID, soulData.Type, soulData.EquipSlot, client.ServerId))
                    {
                        // 保存到数据库成功，加入激活的守护之灵列表中
                        client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList.Add(soulData);
                    }
                }
            }

            // 新的守护之灵的个数
            int newGuardSoulCnt = client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList.Count;
            int newSoltCnt = GetSlotCntBySoulCnt(newGuardSoulCnt);
            if (client.ClientData.MyGuardStatueDetail.ActiveSoulSlot != newSoltCnt)
            {
                // 激活的守护之灵可装备栏位变化，刷新数据库，如果刷新失败，则回滚
                int oldSlotCnt = client.ClientData.MyGuardStatueDetail.ActiveSoulSlot;
                client.ClientData.MyGuardStatueDetail.ActiveSoulSlot = newSoltCnt;
                if (!_UpdateGuardStatue2DB(client))
                {
                    client.ClientData.MyGuardStatueDetail.ActiveSoulSlot = oldSlotCnt;
                }
            }
        }

        //  刷新玩家守护雕像属性加成
        private void UpdateGuardStatueProps(GameClient client)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return;
            }

            if (client == null || !client.ClientData.MyGuardStatueDetail.IsActived) return;

            EquipPropItem props = new EquipPropItem();

            foreach (var soul in client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList)
            {
                // 跳过未穿戴的
                if (soul.EquipSlot == GuardStatueConst.GuardSoulNotEquipSlot) 
                    continue;

                GuardSoul gs = null;
                if (!guardSoulDict.TryGetValue(soul.Type, out gs)) 
                    continue;

                EquipPropItem tmp = GameManager.EquipPropsMgr.FindEquipPropItem(gs.GoodsID);
                if (tmp == null) continue;

                int level = client.ClientData.MyGuardStatueDetail.GuardStatue.Level;
                int suit = client.ClientData.MyGuardStatueDetail.GuardStatue.Suit;

                for (int i = 0; i < tmp.ExtProps.Length; ++i)
                {
                    // 基础值*（1+等级*升级属性系数+（阶位-1）*升阶属性系数）
                    props.ExtProps[i] += tmp.ExtProps[i] * (1 + level * LevelFactor + (suit -1) * SuitFactor);
                }
            }

            client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.GuardStatue, props.ExtProps);
        }
        #endregion 检测守护之灵开启信息，以及属性计算

        #region 更新db
        /// <summary>
        /// 更新守护雕像信息到db
        /// </summary>
        private bool _UpdateGuardStatue2DB(GameClient client)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return false;
            }

            if (client == null || !client.ClientData.MyGuardStatueDetail.IsActived)
            {
                return false;
            }

            int roleid = client.ClientData.RoleID;
            int slotCnt = client.ClientData.MyGuardStatueDetail.ActiveSoulSlot;
            int level = client.ClientData.MyGuardStatueDetail.GuardStatue.Level;
            int suit = client.ClientData.MyGuardStatueDetail.GuardStatue.Suit;
            int totalGuardPoint = client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint;
            int lastdayRecoverPoint = client.ClientData.MyGuardStatueDetail.LastdayRecoverPoint;
            int lastdayRecoverOffset = client.ClientData.MyGuardStatueDetail.LastdayRecoverOffset;

            string cmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", client.ClientData.RoleID, slotCnt, level, suit, totalGuardPoint, lastdayRecoverPoint, lastdayRecoverOffset);
            string[] dbRsp = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_ROLE_GUARD_STATUE, cmd, client.ServerId);
            if (dbRsp != null && dbRsp.Length != 1 && Convert.ToInt32(dbRsp[0]) < 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("更新角色守护雕像失败，roleid={0}, slot={1}, level={2}, suit={3}, totalGuardPoint={4}, lastdayRecoverPoint={5}, lastdayRecoverOffset={6}", roleid, slotCnt, level, suit, totalGuardPoint, lastdayRecoverPoint, lastdayRecoverOffset));
                return false;
            }
            return true;
        }

        /// <summary>
        /// 更新守护之灵信息到db
        /// </summary>
        private bool _UpdateGuardSoul2DB(int roleid, int soulType, int equipSlot, int serverId)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return false;
            }

            string cmdText = string.Format("{0}:{1}:{2}", roleid, soulType, equipSlot);
            string[] dbRsp = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_ROLE_GUARD_SOUL, cmdText, serverId);
            if (dbRsp != null && dbRsp.Length != 1 && Convert.ToInt32(dbRsp[0]) < 0)
            {
                LogManager.WriteLog(LogTypes.Error, "更新角色守护之灵信息失败，" + cmdText);
                return false;
            }
            return true;
        }

        #endregion 更新db

        #region 客户端查询守护点信息
        // 客户端查询守护点回收信息   c--->s roleid    s--->c todayHasRecover:todayMaxRecover
        public TCPProcessCmdResults ProcRoleQueryGuardPointRecover(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int todayHasRecover = 0;
                int todayMaxRecover = 0;
                GuardStatueErrorCode ec = QueryGuardPointRecoverInfo(client, out todayHasRecover, out todayMaxRecover);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, string.Format("{0}:{1}:{2}", (int)ec, todayHasRecover, todayMaxRecover), nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        // 查询角色的守护点信息, 如果守护雕像未开放，则都为0
        private GuardStatueErrorCode QueryGuardPointRecoverInfo(GameClient client, out int todayHasRecover, out int todayMaxRecover)
        {
            todayHasRecover = 0;
            todayMaxRecover = 0;
            if (client == null || !client.ClientData.MyGuardStatueDetail.IsActived)
            {
                return GuardStatueErrorCode.NotOpen;
            }

            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return GuardStatueErrorCode.NotOpen;
            }

            int guardSoulCnt = client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList.Count;
            int nowOffsetDay = Global.GetOffsetDay(TimeUtil.NowDateTime());
            if (client.ClientData.MyGuardStatueDetail.LastdayRecoverOffset != nowOffsetDay)
            {
                client.ClientData.MyGuardStatueDetail.LastdayRecoverPoint = 0;
                client.ClientData.MyGuardStatueDetail.LastdayRecoverOffset = nowOffsetDay;
                if (!_UpdateGuardStatue2DB(client))
                {
                }
            }

            todayHasRecover = client.ClientData.MyGuardStatueDetail.LastdayRecoverPoint;
            todayMaxRecover = GetDayMaxCanRecoverPointBySoulCnt(guardSoulCnt);
            return GuardStatueErrorCode.Success;
        }
        #endregion 客户端查询守护点信息

        #region 客户端回收守护点
        // 客户端请求回收守护点 c--->s roleid : itemid,cnt : itemid,cnt   s--->c todayHasRecover : todayMaxRecover
        public TCPProcessCmdResults ProcRoleGuardPointRecover(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, 
            TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length <= 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 回收的精魄，回收的数量
                // 保存到字典里面，防止客户端发送重复的精魄
                Dictionary<int, int> itemDict = new Dictionary<int, int>();
                for (int i = 1; i < fields.Length; ++i)
                {
                    string[] szItem = fields[i].Split(',');
                    if (szItem.Length == 2)
                    {
                        int itemID = Convert.ToInt32(szItem[0]);
                        int itemCnt = Convert.ToInt32(szItem[1]);
                        if (itemDict.ContainsKey(itemID))
                        {
                            itemDict[itemID] += itemCnt;
                        }
                        else
                        {
                            itemDict.Add(itemID, itemCnt);
                        }
                    }
                }

                GuardStatueErrorCode ec = RecoverGuardPoint(client, itemDict);
                int todayHasRecover = 0;
                int todayMaxRecover = 0;
                QueryGuardPointRecoverInfo(client, out todayHasRecover, out todayMaxRecover);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, string.Format("{0}:{1}:{2}", (int)ec, todayHasRecover, todayMaxRecover), nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        // 回收守护点 itemDict 精魄id，精魄数量
        private GuardStatueErrorCode RecoverGuardPoint(GameClient client, Dictionary<int, int> itemDict)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return GuardStatueErrorCode.NotOpen;
            }

            if (client == null || itemDict == null || !client.ClientData.MyGuardStatueDetail.IsActived)
                return GuardStatueErrorCode.NotOpen;

            // 保存实际消耗的GoodsID和GoodsCnt
            List<Tuple<int, int>> costGoodsList = new List<Tuple<int, int>>();
            // 保存总共计算得到的守护点，有可能超过今日可回收
            int totalPoint = 0;

            int todayHasRecoverPoint = 0;
            int todayMaxRecoverPoint = 0;
            QueryGuardPointRecoverInfo(client, out todayHasRecoverPoint, out todayMaxRecoverPoint);

            foreach (var kvp in itemDict)
            {
                int itemID = kvp.Key;
                int itemCnt = kvp.Value;

                // 图鉴未激活，精魄不可回收
                if (!client.ClientData.ActivedTuJianItem.Contains(itemID))
                {
                    continue;
                }

                // 精魄的回收配置找不到啊
                GuardPoint gp = null;
                if (!guardPointDict.TryGetValue(itemID, out gp))
                {
                    continue;
                }

                // 该地图对应的地图未全部激活图鉴
                if (!client.ClientData.ActivedTuJianType.Contains(gp.TypeID))
                {
                    continue;
                }

                int canAddPoint = todayMaxRecoverPoint - todayHasRecoverPoint - totalPoint;
                if (canAddPoint <= 0) break;

                int realCanRecoverCnt = canAddPoint / gp.Point + (canAddPoint % gp.Point != 0 ? 1 : 0);
                realCanRecoverCnt = Math.Max(0, Math.Min(realCanRecoverCnt, itemCnt));

                // 精魄不足
                if (Global.GetTotalGoodsCountByID(client, gp.GoodsID) < realCanRecoverCnt)
                {
                    continue;
                }

                costGoodsList.Add(new Tuple<int, int>(gp.GoodsID, realCanRecoverCnt));
                totalPoint += gp.Point * realCanRecoverCnt;

                if (totalPoint >= todayMaxRecoverPoint - todayHasRecoverPoint)
                    break;
            }

            // 没有可以回收的材料
            if (costGoodsList.Count == 0 || totalPoint <= 0)
                return GuardStatueErrorCode.MaterialNotEnough;

            // 超过了今日剩余可回收
            if (totalPoint > todayMaxRecoverPoint - todayHasRecoverPoint)
            {
                //return GuardStatueErrorCode.MoreThanTodayCanRecover;
            }

            foreach (var tuple in costGoodsList)
            {
                int goodsID = tuple.Item1;
                int goodsCnt = tuple.Item2;
                bool usedBinding_just_placeholder = false;
                bool usedTimeLimited_just_placeholder = false;
                GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    client, goodsID, goodsCnt, false, out usedBinding_just_placeholder, out usedTimeLimited_just_placeholder);
            }

            GuardStatueData data = client.ClientData.MyGuardStatueDetail.GuardStatue;
            int validPoint = Math.Min(totalPoint, todayMaxRecoverPoint - todayHasRecoverPoint);

            data.HasGuardPoint += validPoint;
            client.ClientData.MyGuardStatueDetail.LastdayRecoverPoint += validPoint;
            if (!_UpdateGuardStatue2DB(client))
            {
                data.HasGuardPoint -= validPoint;
                client.ClientData.MyGuardStatueDetail.LastdayRecoverPoint -= validPoint;

                return GuardStatueErrorCode.DBFailed;
            }

            // 守护点数量变更通知
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.TotalGuardPoint, data.HasGuardPoint);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.GuardPoint, validPoint, data.HasGuardPoint, "回收守护点");

            return GuardStatueErrorCode.Success;
        }
        #endregion 客户端回收守护点

        #region 客户端查询守护雕像信息
        // 客户端查询守护雕像信息
        public TCPProcessCmdResults ProcRoleQueryGuardStatue(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                tcpOutPacket = DataHelper.ObjectToTCPOutPacket(client.ClientData.MyGuardStatueDetail.GuardStatue, pool, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion 客户端查询守护雕像信息

        #region 客户端升级守护雕像
        // 客户端请求升级守护雕像
        // c--->s roleid
        // s--->c error : level : hasGuardPoint
        public TCPProcessCmdResults ProcRoleGuardStatueLevelUp(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                GuardStatueErrorCode err = HandleLevelUp(client);
                int level = client.ClientData.MyGuardStatueDetail.GuardStatue.Level;
                int hasGuardPoint = client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint;
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, string.Format("{0}:{1}:{2}", (int)err, level, hasGuardPoint), nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        public GuardStatueErrorCode HandleLevelUp(GameClient client)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return GuardStatueErrorCode.NotOpen;
            }

            if (client == null || !client.ClientData.MyGuardStatueDetail.IsActived)
                return GuardStatueErrorCode.NotOpen;

            GuardStatueData data = client.ClientData.MyGuardStatueDetail.GuardStatue;

            if (data.Level >= GuardStatueMaxLevel)
                return GuardStatueErrorCode.LevelIsFull;

            if (data.Level > 0 && data.Level % 10 == 0 && (data.Level + (GuardStatueConst.GuardStatueDefaultSuit * 10))/ 10 != data.Suit)
                return GuardStatueErrorCode.NeedSuitUp;

            GuardLevelUp levelUp = null;
            if (!guardLevelUpDict.TryGetValue(data.Level + 1, out levelUp))
                return GuardStatueErrorCode.ConfigError;

            if (levelUp.NeedGuardPoint > data.HasGuardPoint)
                return GuardStatueErrorCode.GuardPointNotEnough;

            data.HasGuardPoint -= levelUp.NeedGuardPoint;
            data.Level += 1;

            if (!_UpdateGuardStatue2DB(client))
            {
                data.HasGuardPoint += levelUp.NeedGuardPoint;
                data.Level -= 1;
                return GuardStatueErrorCode.DBFailed;
            }

            // 守护点数量变更通知
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.TotalGuardPoint, data.HasGuardPoint);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.GuardPoint, -levelUp.NeedGuardPoint, data.HasGuardPoint, "升级守护雕像");

            UpdateGuardStatueProps(client);
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            if (client._IconStateMgr.CheckSpecialActivity(client))
                client._IconStateMgr.SendIconStateToClient(client);

            return GuardStatueErrorCode.Success;
        }
        #endregion 客户端升级守护雕像

        #region 客户端升阶守护雕像
        // 客户端请求升阶守护雕像
        // c--->s roleid
        // s--->c error : suit : hasGuardPoint
        public TCPProcessCmdResults ProcRoleGuardStatueSuitUp(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                GuardStatueErrorCode err = HandleSuitUp(client);
                int suit = client.ClientData.MyGuardStatueDetail.GuardStatue.Suit;
                int hasGuardPoint = client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint;
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, string.Format("{0}:{1}:{2}", (int)err, suit, hasGuardPoint), nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        private GuardStatueErrorCode HandleSuitUp(GameClient client)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return GuardStatueErrorCode.NotOpen;
            }
            if (client == null || !client.ClientData.MyGuardStatueDetail.IsActived)
                return GuardStatueErrorCode.NotOpen;

            GuardStatueData data = client.ClientData.MyGuardStatueDetail.GuardStatue;

            if (data.Suit >= GuardStatueMaxSuit)
                return GuardStatueErrorCode.SuitIsFull;

            if (data.Level == 0 || (data.Level + GuardStatueConst.GuardStatueDefaultSuit * 10)/10 == data.Suit)
                return GuardStatueErrorCode.NeedLevelUp;

            GuardSuitUp suitUp = null;
            if (!guardSuitUpDict.TryGetValue(data.Suit + 1, out suitUp))
                return GuardStatueErrorCode.ConfigError;

           // if (suitUp.NeedGuardPoint > data.HasGuardPoint)
           //     return GuardStatueErrorCode.GuardPointNotEnough;

            if (Global.GetTotalGoodsCountByID(client, suitUp.NeedGoodsID) < suitUp.NeedGoodsCnt)
                return GuardStatueErrorCode.MaterialNotEnough;

           // data.HasGuardPoint -= suitUp.NeedGuardPoint;
           data.Suit += 1;

            if (!_UpdateGuardStatue2DB(client))
            {
                // data.HasGuardPoint += suitUp.NeedGuardPoint;
                data.Suit -= 1;
                return GuardStatueErrorCode.DBFailed;
            }

            bool bUsedBinding_just_placeholder = false, bUsedTimeLimited_just_placeholder = false;
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, suitUp.NeedGoodsID, suitUp.NeedGoodsCnt,
                    false, out bUsedBinding_just_placeholder, out bUsedTimeLimited_just_placeholder))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("守护雕像进阶时，消耗{0}个GoodsID={0}的物品失败，但是已设置为升阶成功", suitUp.NeedGoodsCnt, suitUp.NeedGoodsID));
            }

            // 守护点数量变更通知
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.TotalGuardPoint, data.HasGuardPoint);

            UpdateGuardStatueProps(client);
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            if (client._IconStateMgr.CheckSpecialActivity(client))
                client._IconStateMgr.SendIconStateToClient(client);

            return GuardStatueErrorCode.Success;
        }
        #endregion 客户端升阶守护雕像

        #region 客户端操作守护之灵装备
        // 客户端穿戴、卸下、替换守护之灵
        public TCPProcessCmdResults ProcRoleModGuardSoulEquip(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int slot = Convert.ToInt32(fields[1]);
                int soulType = Convert.ToInt32(fields[2]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                HandleModGuardSoulEquip(client, slot, soulType);
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket(client.ClientData.MyGuardStatueDetail.GuardStatue, pool, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        private void HandleModGuardSoulEquip(GameClient client, int slot, int guardSoulType)
        {
            if (client == null || !client.ClientData.MyGuardStatueDetail.IsActived) return;

            int slotCnt = client.ClientData.MyGuardStatueDetail.ActiveSoulSlot;
            if (slot < 0 || slot >= slotCnt) return;

            // 找到该位置穿戴的旧装备, 如果木有穿，那就是null
            GuardSoulData oldSoulData = client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList.Find((soul) =>
            {
                return soul.EquipSlot == slot;
            });
            // 找到要穿戴的新装备, 如果是要卸装备，那么传入的guardSoulType==-1，
            GuardSoulData newSoulData = null;
            if (guardSoulType != -1)
            {
                newSoulData = client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList.Find((soul) =>
                {
                    return soul.Type == guardSoulType;
                });
            }

            // 1: 既不脱，又不穿  2: 脱的和穿的是同一件
            if ((oldSoulData == null && newSoulData == null) || (oldSoulData == newSoulData))
            {
                return;
            }

            // 新装备已佩戴，不允许再佩戴到别的位置
            if (newSoulData != null && newSoulData.EquipSlot != GuardStatueConst.GuardSoulNotEquipSlot)
            {
                return;
            }

            // 如果该位置有旧的守护之灵，那么就先脱下来
            if (oldSoulData != null
                && _UpdateGuardSoul2DB(client.ClientData.RoleID, oldSoulData.Type, GuardStatueConst.GuardSoulNotEquipSlot, client.ServerId))
            {
                oldSoulData.EquipSlot = GuardStatueConst.GuardSoulNotEquipSlot;
            }

            // 该位置没有旧的守护之灵，或者旧的守护之灵已经脱下来了
            if (oldSoulData == null || oldSoulData.EquipSlot == GuardStatueConst.GuardSoulNotEquipSlot)
            {
                // 穿上新装备
                if (newSoulData != null && _UpdateGuardSoul2DB(client.ClientData.RoleID, newSoulData.Type, slot, client.ServerId))
                {
                    newSoulData.EquipSlot = slot;
                }
            }
            
            UpdateGuardStatueProps(client);
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
        }
        #endregion 客户端操作守护之灵装备

        #region 守护之灵个数与每日最多可回收的守护点  守护之灵个数与激活的装备栏位
        // 根据守护之灵的个数获取开启的槽数
        private int GetSlotCntBySoulCnt(int cnt)
        {
            int slot = 0;
            foreach (var t in maxActiveSlotCntList)
            {
                if (cnt >= t.Item1)
                {
                    slot = Math.Max(slot, t.Item2);
                }
            }

            return slot;
        }
        // 根据守护之灵的个数获取每日可回收的守护点数
        private int GetDayMaxCanRecoverPointBySoulCnt(int cnt)
        {
            int maxPoint = 0;
            foreach (var t in dayMaxCanRecoverPointList)
            {
                if (cnt >= t.Item1)
                {
                    maxPoint = Math.Max(maxPoint, t.Item2);
                }
            }

            return maxPoint;
        }
        public void InitRecoverPoint_BySysParam(string str)
        {
            if (string.IsNullOrEmpty(str)) return;

            dayMaxCanRecoverPointList.Clear();
            string[] szDetail = str.Split('|');

            foreach (var s in szDetail)
            {
                string[] szSlot = s.Split(',');
                if (szSlot.Length == 2)
                {
                    // 激活soulCnt后，每日最大可回收守护点
                    int soulCnt = Convert.ToInt32(szSlot[0]);
                    int maxPoint = Convert.ToInt32(szSlot[1]);
                    dayMaxCanRecoverPointList.Add(new Tuple<int, int>(soulCnt, maxPoint));
                }
            }
        }
       public void InitSoulSlot_BySysParam(string str)
        {
            if (string.IsNullOrEmpty(str)) return;

            maxActiveSlotCntList.Clear();
            string[] szDetail = str.Split('|');

            foreach (var s in szDetail)
            {
                string[] szSlot = s.Split(',');
                if (szSlot.Length == 2)
                {
                    // 激活slotCnt至少需要激活needSoulCnt个守护之灵
                    int slotCnt = Convert.ToInt32(szSlot[0]);
                    int needSoulCnt = Convert.ToInt32(szSlot[1]);
                    maxActiveSlotCntList.Add(new Tuple<int, int>(needSoulCnt, slotCnt));
                }
            }
        }
        #endregion 守护之灵个数与每日最多可回收的守护点  守护之灵个数与激活的装备栏位

       #region GM接口
       public void GM_HandleModGuardSoulEquip(GameClient client, int slot, int guardSoulType)
       {
           HandleModGuardSoulEquip(client, slot, guardSoulType);
       }

       public string GM_QueryGuardPoint(GameClient client)
       {
           int todayHasRecover = 0;
           int todayCanRecover = 0;
           QueryGuardPointRecoverInfo(client, out todayHasRecover, out todayCanRecover);
           int totalHas = client != null ? client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint : 0;
           return string.Format("今日已回收[{0}], 今日最大可回收[{1}]，总共有守护点[{2}]", todayHasRecover, todayCanRecover, totalHas);
       }

        public void GM_GuardPointRecover(GameClient client, Dictionary<int, int> itemDict)
        {
            RecoverGuardPoint(client, itemDict);
        }

        public void GM_HandleLevelUp(GameClient client)
        {
            HandleLevelUp(client);
        }

        public void GM_HandleSuitlUp(GameClient client)
        {
            HandleSuitUp(client);
        }

        public string GM_QueryGuardStatue(GameClient client)
        {
            if (client == null || !client.ClientData.MyGuardStatueDetail.IsActived)
            {
                /**/return "守护雕像未激活";
            }

            /**/string tip = string.Format("守护雕像已激活, 等级={0}, 品阶={1}, 激活的守护之灵装备栏={2}, 共有守护之灵={3}， ",
                client.ClientData.MyGuardStatueDetail.GuardStatue.Level, client.ClientData.MyGuardStatueDetail.GuardStatue.Suit,
                GetSlotCntBySoulCnt(client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList.Count), client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList.Count);

            tip += GM_QueryGuardPoint(client);

            foreach (var soul in client.ClientData.MyGuardStatueDetail.GuardStatue.GuardSoulList)
            {
                /**/tip += string.Format(", 【type={0}, slot={1}】", soul.Type, soul.EquipSlot);
            }

            return tip;
        }

        public string GM_ModGuardPoint(GameClient client, int newVal)
        {
            if (client == null || !client.ClientData.MyGuardStatueDetail.IsActived)
            {
                return /**/"守护雕像未激活";
            }

            client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint = newVal;
            if (!_UpdateGuardStatue2DB(client))
            {
                return /**/"设置守护点失败";
            }

            // 守护点数量变更通知
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.TotalGuardPoint, client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint);
            EventLogManager.AddMoneyEvent(client, OpTypes.Set, OpTags.None, MoneyTypes.GuardPoint, 0, newVal, "GM设置");

            return /**/"设置守护点成功";
        }
       #endregion GM接口

        public void AddGuardPoint(GameClient client, int point, string strFrom)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return;
            }
            if (client == null || !client.ClientData.MyGuardStatueDetail.IsActived)
            {
                return;
            }

            client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint += point;
            if (!_UpdateGuardStatue2DB(client))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("AddGuardPoint failed, roleid={0}, rolename={1}, addpoint={2}", client.ClientData.RoleID, client.ClientData.RoleName, point));
                return;
            }

            // 守护点数量变更通知
            GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.TotalGuardPoint, client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint);
            EventLogManager.AddMoneyEvent(client, OpTypes.AddOrSub, OpTags.None, MoneyTypes.GuardPoint, point, client.ClientData.MyGuardStatueDetail.GuardStatue.HasGuardPoint, strFrom);
        }
    }
}
