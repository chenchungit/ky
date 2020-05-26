#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using Server.Data;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using GameServer.Server;
using GameServer.Logic;
using GameServer.Core.GameEvent;
using GameServer.Logic.BangHui.ZhanMengShiJian;
using GameServer.Core.Executor;

namespace GameServer.Logic
{

    /// <summary>
    /// 战盟副本信息
    /// </summary> 
    public class GuildCopyMap
    {
        //public CopyMap copymap;
        public int GuildID;
        public int FuBenID;
        public int SeqID;
        public int MapCode;
    }

    /// <summary>
    /// 战盟副本信息管理
    /// </summary> 
    public class GuildCopyMapManager
    {
        /// <summary>
        /// 战盟副本信息字典
        /// Key = 帮会id
        /// </summary> 
        private Dictionary<int, GuildCopyMap>   GuildCopyMapDict = new Dictionary<int, GuildCopyMap>();

        /// <summary>
        /// 帮会副本起始ID 40000-40006
        /// </summary> 
        private const int firstGuildCopyMapOrder = 40000;
        
        /// <summary>
        /// 帮会副本起始ID
        /// </summary> 
        public int FirstGuildCopyMapOrder
        { 
            get {   return firstGuildCopyMapOrder;  }
        }

        private List<int> guildCopyMapOrderList = new List<int>();

        public List<int> GuildCopyMapOrderList
        {
            get { return guildCopyMapOrderList; }
        }

        public void LoadGuildCopyMapOrder()
        {
            GuildCopyMapOrderList.Clear();
            GuildCopyMapOrderList.Add(FirstGuildCopyMapOrder);
            int beginOrder = FirstGuildCopyMapOrder;

            while (true)
            {
                SystemXmlItem systemFuBenItem = null;
                if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(beginOrder, out systemFuBenItem))
                    return;

                if (null == systemFuBenItem)
                    return;

                int nDownCopyID = systemFuBenItem.GetIntValue("DownCopyID");
                if (nDownCopyID <= 0)
                {
                    LastGuildCopyMapOrder = beginOrder;
                    break;
                }
                beginOrder = nDownCopyID;
                GuildCopyMapOrderList.Add(nDownCopyID);
            }

        }

        /// <summary>
        /// 帮会副本最后ID
        /// </summary> 
        private int lastGuildCopyMapOrder = 40006;

        /// <summary>
        /// 帮会副本最后ID
        /// </summary> 
        public int LastGuildCopyMapOrder
        {
            get { return lastGuildCopyMapOrder; }
            set { this.lastGuildCopyMapOrder = value; }
        }

        private int maxDamageSendCount = 5;

        public int MaxDamageSendCount
        { 
            get { return maxDamageSendCount; }  
        }

        /// <summary>
        /// 跨周关闭副本的间隔计数
        /// </summary> 
        /// 
        public long lastProcessEndTicks = 0;

        /// <summary>
        /// 跨周关闭副本开始和完成的标记
        /// </summary> 
        public bool ProcessEndFlag = false;

        /// <summary>
        /// 是否在跨周关闭副本的准备时间里
        /// 后台在这个时间内会把ProcessEndFlag设置成true
        /// 过了这个时间立刻开始依次移除GuildCopyMapDict里的内容并关闭对应副本
        /// </summary> 
        public bool IsPrepareResetTime()
        {
            DateTime now = TimeUtil.NowDateTime();

            // 周一的0:00到0:01分
            DayOfWeek dayofweek = now.DayOfWeek;
            if (dayofweek != DayOfWeek.Sunday)
                return false;

            DateTime beginTime = new DateTime(now.Year, now.Month, now.Day, 23, 55, 0);// DateTime.Parse("0:00");
            DateTime EndTime = new DateTime(now.Year, now.Month, now.Day, 23, 56, 0);//DateTime.Parse("0:01");
            return now >= beginTime && now <= EndTime;
        }

        /// <summary>
        /// 该阶段才允许进入跨服副本
        /// </summary> 
        public bool IsRefuseTime()
        {
            DateTime now = TimeUtil.NowDateTime();

            // 周一的0:00到0:01分
            DayOfWeek dayofweek = now.DayOfWeek;
            if (dayofweek != DayOfWeek.Sunday)
                return false;

            DateTime beginTime = new DateTime(now.Year, now.Month, now.Day, 23, 55, 0);// DateTime.Parse("0:00");
            DateTime EndTime = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);//DateTime.Parse("0:01");
            return now >= beginTime && now <= EndTime;
        }

        /// <summary>
        /// 判断一个id是不是一个帮会副本
        /// </summary> 
        public bool IsGuildCopyMap(int fubenID)
        {
            //return mapid >= FirstGuildCopyMapOrder && mapid <= LastGuildCopyMapOrder;
            return GuildCopyMapOrderList.IndexOf(fubenID) >= 0;
        }

        /// <summary>
        /// 根据副本id取得这个副本是第几个的副本
        /// 从0开始 返回-1说明不是帮会副本
        /// </summary> 
        public int GetGuildCopyMapIndex(int fubenID)
        {
            //if (!IsGuildCopyMap(fubenID))
            //    return -1;

            //return fubenID - FirstGuildCopyMapOrder;
            return GuildCopyMapOrderList.IndexOf(fubenID);
        }

        /// <summary>
        /// 取得下一个副本id
        /// </summary
        public int GetNextGuildCopyMapIndex(int fubenID)
        {
            if (fubenID == LastGuildCopyMapOrder)
                return -1;
            int index = GetGuildCopyMapIndex(fubenID);
            if (index < 0)
                return -1;
            return GetGuildCopyMapOrderByIndex(index + 1);
        }

        /// <summary>
        /// 根据索引取得副本id
        /// </summary> 
        public int GetGuildCopyMapOrderByIndex(int index)
        {
            if (index < 0 || index >= GuildCopyMapOrderList.Count)
                return -1;
            return GuildCopyMapOrderList[index];
        }

        /// <summary>
        /// 取得副本领奖的信息   
        /// 领奖记录用一个int的左14位(7天*2中奖励)来标志
        /// index：1 副本内击杀boss奖励 2 界面领取奖励
        /// </summary>
        public bool GetGuildCopyMapAwardDayFlag(int Flag, int day, int index)
        {
            return ((Flag >> (day * 2)) & (index)) == index ? true : false;
        }

        /// <summary>
        /// 更新副本领奖的信息
        /// index：1 副本内击杀boss奖励 2 界面领取奖励
        /// </summary>
        public int SetGuildCopyMapAwardDayFlag(int Flag, int day, int index)
        { 
            return Flag | (index << (day * 2));
        }

        /// <summary>
        /// 更新副本信息
        /// </summary>
        public void UpdateGuildCopyMap(int guildid, int fubenid, int seqid, int mapcode)
        {
            GuildCopyMap CopyMap = new GuildCopyMap()
            {
                GuildID = guildid,
                FuBenID = fubenid,
                SeqID = seqid,
                MapCode = mapcode,
            };
            UpdateGuildCopyMap(guildid, CopyMap);
        }

        /// <summary>
        /// 更新副本信息
        /// </summary>
        public void UpdateGuildCopyMap(int guildid, GuildCopyMap CopyMap)
        {
            lock (GuildCopyMapDict)
            {
                GuildCopyMapDict[guildid] = CopyMap;
            }
        }

        /// <summary>
        /// 根据帮会id查找到一个帮会副本记录
        /// </summary> 
        public GuildCopyMap FindGuildCopyMap(int guildid)
        {
            GuildCopyMap CopyMap = null;
            lock (GuildCopyMapDict)
            {
                if (GuildCopyMapDict.ContainsKey(guildid))
                {
                    CopyMap = GuildCopyMapDict[guildid];
                }            
            }
            return CopyMap;
        }

        /// <summary>
        /// 找到第一个副本记录
        /// 在跨周的时候陆续删除
        /// </summary> 
        public GuildCopyMap FindActiveGuildCopyMap()
        {
            GuildCopyMap CopyMap = null;
            lock (GuildCopyMapDict)
            {
                foreach (var map in GuildCopyMapDict)
                {
                    return map.Value;
                }          
            }
            return CopyMap;
        }

        /// <summary>
        /// 通过副本流水id查找到对应的帮会副本信息
        /// 在副本被超时自动关闭的时候，对应删除掉副本信息
        /// 在击杀boss的时候查找帮会id
        /// </summary> 
        public GuildCopyMap FindGuildCopyMapBySeqID(int seqid)
        {
            lock (GuildCopyMapDict)
            {
                foreach (var map in GuildCopyMapDict)
                {
                    if (seqid == map.Value.SeqID)
                    {
                        return map.Value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 根据帮会id移除副本信息
        /// </summary> 
        public void RemoveGuildCopyMap(int guildid)
        {
            GuildCopyMapDict.Remove(guildid);
        }

        /// <summary>
        /// 检查帮会副本的数据库状态，找到应该进入的副本id，副本流水id，副本code
        /// fubenid = 0 表示本周的副本全通关了
        /// fubenid = -1 表示在调用之前应该先判断玩家是否有帮会
        /// </summary>
        public void CheckCurrGuildCopyMap(GameClient client, out int fubenid, out int seqid, int mapcode)
        {
            fubenid = -1;
            seqid = -1;
            mapcode = -1;
            int guildid = client.ClientData.Faction;

            GuildCopyMapDB data = GameManager.GuildCopyMapDBMgr.FindGuildCopyMapDB(guildid, client.ServerId);
            // 没有帮会 否则不应该为空  
            if (null == data)
                return;

            // 开始时间不是本周
            DateTime openTime = Global.GetRealDate(data.OpenDay);
            if (Global.BeginOfWeek(openTime) != Global.BeginOfWeek(TimeUtil.NowDateTime()))
            {
                // 重置数据库信息 并使用第一个副本id 让玩家重新开始
                GameManager.GuildCopyMapDBMgr.ResetGuildCopyMapDB(guildid, client.ServerId);
                fubenid = FirstGuildCopyMapOrder;
                return;
            }

            // 到达最后一关并且已经通了
            if (data.FuBenID >= LastGuildCopyMapOrder && data.State == (int)GuildCopyMapState.Passed)
            {
                fubenid = 0;
                return;
            }

            // 已经通关
            if (data.State == (int)GuildCopyMapState.Passed)
            {
                // 更新数据库
                data.FuBenID = GetNextGuildCopyMapIndex(data.FuBenID);
                data.State = (int)GuildCopyMapState.NotOpen;
                data.OpenDay = Global.GetOffsetDay(TimeUtil.NowDateTime());

                if (GameManager.GuildCopyMapDBMgr.UpdateGuildCopyMapDB(data, client.ServerId))
                    fubenid = data.FuBenID ;
                UpdateGuildCopyMap(guildid, fubenid, -1, -1);
                return;
            }
            else
            {
                fubenid = data.FuBenID;
            }

            //if (!IsGuildCopyMap(newmapid))
            //    newmapid = FirstGuildCopyMapOrder;

            GuildCopyMap CopyMap = FindGuildCopyMap(guildid);
            if (null == CopyMap)
                return;

            seqid = CopyMap.SeqID;
        }

        /// <summary>
        /// 玩家申请进入帮会副本，给玩家返回自己帮会当前副本id和副本流水号
        /// 没有副本流水号则申请一个新的
        /// 使用后需要检查fubenid是否允许玩家进入
        /// </summary> 
        public void EnterGuildCopyMap(GameClient client, out int fubenid, out int seqid, int mapcode)
        {
            fubenid = -1;
            seqid = -1;
            int guildid = client.ClientData.Faction;

            lock (GuildCopyMapDict)
            {
                CheckCurrGuildCopyMap(client, out fubenid, out seqid, mapcode);
                if (seqid < 0)
                {
                    string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GETFUBENSEQID, string.Format("{0}", client.ClientData.RoleID), client.ServerId);
                    if (null != dbFields && dbFields.Length >= 2)
                    {
                        seqid = Global.SafeConvertToInt32(dbFields[1]);
                        if (seqid > 0)
                        {
                            UpdateGuildCopyMap(guildid, fubenid, seqid, mapcode);
                        }
                    }
                }            
            }
        }

        /// <summary>
        /// 检查死亡的怪物是否为帮会副本boss并进行通关处理
        /// </summary> 
        public void ProcessMonsterDead(GameClient client, Monster monster)
        {
            // 怪物所在场景不是帮会地图
            if (IsGuildCopyMap(monster.CurrentMapCode) == false)
                return;

            SystemXmlItem systemFuBenItem = null;
            if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(monster.CurrentMapCode, out systemFuBenItem))
                return;
            
            if (null == systemFuBenItem)
                return;

            int nBossID = systemFuBenItem.GetIntValue("BossID");
#if ___CC___FUCK___YOU___BB___
            // 不是boss
            if (nBossID != monster.XMonsterInfo.MonsterId)
                return;
#else
             // 不是boss
            if (nBossID != monster.MonsterInfo.ExtensionID)
                return;
#endif
            // 没有对应副本？
            CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(monster.CurrentCopyMapID);
            if (null == copyMap)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapManager::ProcessMonsterDead (null == copyMap), CurrentCopyMapID={0}", monster.CurrentCopyMapID));
                return;
            }

            GuildCopyMap mapData = GameManager.GuildCopyMapMgr.FindGuildCopyMapBySeqID(copyMap.FuBenSeqID);
            if (null == mapData)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapManager::ProcessMonsterDead (null == mapData), copyMap.FuBenSeqID={0}", copyMap.FuBenSeqID));
                return;
            }
            int guildid = mapData.GuildID;

            // 玩家没有帮会？
            GuildCopyMapDB data = GameManager.GuildCopyMapDBMgr.FindGuildCopyMapDB(guildid/*client.ClientData.Faction*//*不再用击杀者的帮会id*/, client.ServerId);
            if (null == data)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapManager::ProcessMonsterDead (null == data), guildid={0}", client.ClientData.Faction));
                return;
            }

            List<GameClient> objsList = copyMap.GetClientsList();
            // 副本里没人？
            if (null == objsList || objsList.Count <= 0)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapManager::ProcessMonsterDead (null == objsList || objsList.Count <= 0), CurrentCopyMapID={0}", monster.CurrentCopyMapID));
                return;
            }

            // 更新副本记录的状态
            if (copyMap.FubenMapID >= data.FuBenID)
            {
                data.FuBenID = copyMap.FubenMapID;
                data.State = (int)GuildCopyMapState.Passed;
                if (copyMap.FubenMapID == GameManager.GuildCopyMapMgr.FirstGuildCopyMapOrder)
                {
                    data.Killers = monster.WhoKillMeName;
                }
                else
                {
                    data.Killers += ",";
                    data.Killers += monster.WhoKillMeName;
                }
            }
#if ___CC___FUCK___YOU___BB___
            //触发战盟事件
            GlobalEventSource.getInstance().fireEvent(ZhanMengShijianEvent.createKillBossEvent(Global.FormatRoleName4(client), client.ClientData.Faction, 
                monster.XMonsterInfo.MonsterId, client.ServerId));
#else
            //触发战盟事件
            GlobalEventSource.getInstance().fireEvent(ZhanMengShijianEvent.createKillBossEvent(Global.FormatRoleName4(client), client.ClientData.Faction,
                monster.MonsterInfo.ExtensionID, client.ServerId));
#endif
            // 检查是否更新成功
            bool result = GameManager.GuildCopyMapDBMgr.UpdateGuildCopyMapDB(data, client.ServerId);
            if (false == result)
            {
                string logStr=@"GuildCopyMapManager::ProcessMonsterDead (false == result), 
                        data.GuildID={0}, data.FuBenID={1}, data.State={2}, data.OpenDay={3}, data.Killers={4}";
                LogManager.WriteLog(LogTypes.Error, string.Format(logStr, data.GuildID, data.FuBenID, data.State, data.OpenDay, data.Killers));
                return;
            }
        }

        public int GetZhanGongAward(GameClient client, int fubenid, int awardZhanGong)
        {
            if (IsGuildCopyMap(fubenid) == false)
                return 0;

            // 准备发奖
            // 先检查上次领奖是不是本周~~
            int nGuildCopyMapAwardDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.GuildCopyMapAwardDay);
            DateTime AwardTime = Global.GetRealDate(nGuildCopyMapAwardDay);
            if (Global.BeginOfWeek(AwardTime) != Global.BeginOfWeek(TimeUtil.NowDateTime()))
            {
                // 不是这周领奖则重置玩家领奖记录
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.GuildCopyMapAwardFlag, 0, true);
            }

            int nGuildCopyMapAwardFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.GuildCopyMapAwardFlag);
            bool flag = GetGuildCopyMapAwardDayFlag(nGuildCopyMapAwardFlag, GetGuildCopyMapIndex(fubenid), 1);
            // 领取过了
            if (flag == true)
            {
                return -1;
            }

            nGuildCopyMapAwardFlag = SetGuildCopyMapAwardDayFlag(nGuildCopyMapAwardFlag, GetGuildCopyMapIndex(fubenid), 1);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.GuildCopyMapAwardFlag, nGuildCopyMapAwardFlag, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.GuildCopyMapAwardDay, Global.GetOffsetDay(TimeUtil.NowDateTime()), true);

            return awardZhanGong;
        }
    }

    /// <summary>
    /// 副本状态
    /// </summary> 
    public enum GuildCopyMapState
    { 
        NotOpen = 0,    // 未开启
        Opened = 1,     // 已开启
        Passed = 2,     // 已通关
    }

    /// <summary>
    /// 副本数据库相关信息
    /// </summary> 
    public class GuildCopyMapDB
    {
        public int GuildID;
        public int FuBenID;
        public int State;
        public int OpenDay;
        // 通关的人的名字：name1,name2……
        public string Killers;
    }

    /// <summary>
    /// 帮会副本的状态缓存
    /// </summary>
    public class GuildCopyMapDBManager
    {
        /// <summary>
        /// 副本数据库相关信息字典
        /// </summary>
        Dictionary<int, GuildCopyMapDB> GuildCopyMapDBDict = new Dictionary<int, GuildCopyMapDB>();

        /// <summary>
        /// 根据帮会id查找帮会副本信息
        /// </summary>
        public GuildCopyMapDB FindGuildCopyMapDB(int guildid, int serverId)
        {
            if (guildid <= 0)
                return null;

            GuildCopyMapDB data = null;
            lock (GuildCopyMapDBDict)
            { 
                // 先在缓存里查找
                if (GuildCopyMapDBDict.ContainsKey(guildid))
                {
                    data = GuildCopyMapDBDict[guildid];
                }
                // 缓存里没有就去数据库申请
                else
                { 
                        // 去db要数据
                    string[] dbFields = null;
                    string strDbCmd = string.Format("{0}", guildid);
                    TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool
                        , (int)TCPGameServerCmds.CMD_SPR_GETBANGHUIFUBEN, strDbCmd, out dbFields, serverId);
                    if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapDBManager::FindGuildCopyMapDB dbRequestResult == TCPProcessCmdResults.RESULT_FAILED strDbCmd={0}", strDbCmd));
                        return null;
                    }

                    if (dbFields.Length < 5 || Convert.ToInt32(dbFields[0]) <= 0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapDBManager::FindGuildCopyMapDB 参数数量错误或失败 strDbCmd={0}, dbFields.Length={1}", strDbCmd, dbFields.Length));
                        return null;
                    }

                    try
                    {
                        data = new GuildCopyMapDB() 
                        {
                            GuildID = Convert.ToInt32(dbFields[0]),
                            FuBenID = Convert.ToInt32(dbFields[1]),
                            State = Convert.ToInt32(dbFields[2]),
                            OpenDay = Convert.ToInt32(dbFields[3]),
                            Killers = dbFields[4],
                        };

                        // 加入缓存
                        AddGuildCopyMapDB(data);

                        if (guildid != Convert.ToInt32(dbFields[0]))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapDBManager::FindGuildCopyMapDB DB返回的id不符，guildid={0}, dbFields[0]={1}", guildid, Convert.ToInt32(dbFields[0])));
                            return null;
                        }
                    }
                
                    catch (Exception)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapDBManager::FindGuildCopyMapDB参数解析失败？"));
                        return null;
                    }
                }
            }
            
            return data;
        }

        /*public void RemoveGuildCopyMapDB(int guildid)
        {
            // 
            GuildCopyMapDBDict.Remove(guildid);
        }*/

        /// <summary>
        /// 从数据库加载后增加到帮会缓存
        /// 更新信息请使用Update，防止出现没有保存的情况
        /// </summary>
        public void AddGuildCopyMapDB(GuildCopyMapDB data)
        {
            if (GuildCopyMapDBDict.ContainsKey(data.GuildID))
                return;
            GuildCopyMapDBDict[data.GuildID] = data;
        }

        /// <summary>
        /// 更新帮会信息，先向数据库提交更新申请，后更新缓存
        /// </summary>
        public bool UpdateGuildCopyMapDB(GuildCopyMapDB data, int serverId)
        {
            // 查找现有信息 如果存在直接返回
            GuildCopyMapDB oldData = FindGuildCopyMapDB(data.GuildID, serverId);
            if (null == oldData)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapDBManager::UpdateGuildCopyMapDB null == oldData data.GuildID={0}", data.GuildID));
                return false;
            }

            lock (GuildCopyMapDBDict)
            { 
                // 向DB提申请
                string[] dbFields = null;
                string strDbCmd = string.Format("{0}:{1}:{2}:{3}:{4}", data.GuildID, data.FuBenID, data.State, data.OpenDay, data.Killers);
                TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool
                    , (int)TCPGameServerCmds.CMD_DB_UPDATEBANGHUIFUBEN, strDbCmd, out dbFields, serverId);
                if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapDBManager::ResetGuildCopyMapDB dbRequestResult == TCPProcessCmdResults.RESULT_FAILED strDbCmd={0}", strDbCmd));
                    return false;
                }
                if (dbFields.Length < 1 || Convert.ToInt32(dbFields[0]) != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("GuildCopyMapDBManager::ResetGuildCopyMapDB 参数数量错误或失败 strDbCmd={0}, dbFields.Length={1}", strDbCmd, dbFields.Length));
                    return false;
                }

                GuildCopyMapDBDict[data.GuildID] = data;
            }
            return true;
        }

        /// <summary>
        /// 重置副本信息
        /// </summary>
        public void ResetGuildCopyMapDB(int guildid, int serverId)
        {
            GuildCopyMapDB data = new GuildCopyMapDB() 
            {
                GuildID = guildid,
                FuBenID = GameManager.GuildCopyMapMgr.FirstGuildCopyMapOrder,
                State = (int)GuildCopyMapState.NotOpen,
                OpenDay = Global.GetOffsetDay(TimeUtil.NowDateTime()),
                Killers = "",
            };
            UpdateGuildCopyMapDB(data, serverId);
        }
    }
}
