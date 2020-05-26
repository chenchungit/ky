using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using GameServer.Core.Executor;

namespace GameServer.Logic.ActivityNew
{
    public class RoleGiveRecvInfo
    {
        public int TotalGive;
        public int TotalRecv;
        public int AwardFlag;

        // 保存这三个变量是为了保证查询和领奖时，数据的一致性，防止极端情况出现，导致查询时和领奖时恰好跨了一天
        // 只要角色当天查询过，都会保存下来，经确定.net 对相同的string底层会享元，所以不用担心内存浪费
        public string TodayStart;
        public string TodayEnd;
        public int TodayIdxInActPeriod;
    }

    /// <summary>
    /// 节日赠送活动
    /// </summary>
    public class JieriGiveRecv_Base : Activity
    {
        public virtual string GetConfigFile() { throw new Exception("GetConfigFile未实现"); }
        public virtual string QueryActInfo(GameClient client) { throw new Exception("QueryActInfo未实现"); }
        public virtual void FlushIcon(GameClient client) { throw new Exception("OnGetAwardSuccess未实现"); }
        public virtual bool IsReachConition(RoleGiveRecvInfo info, int condValue) { throw new Exception("IsReachConition未实现"); }

        // 通用奖励
        private Dictionary<int, AwardItem> allAwardDict = new Dictionary<int, AwardItem>();
        // 职业奖励
        private Dictionary<int, AwardItem> occAwardDict = new Dictionary<int, AwardItem>();
        // 通用奖励，有时间限制的物品
        private Dictionary<int, AwardEffectTimeItem> timeAwardDict = new Dictionary<int, AwardEffectTimeItem>();

        // 缓存角色的赠送和收取数据，禁止直接操作该成员，使用线程安全函数GetRoleGiveInfo查询
        // 并且要对查询得到的RoleGiveInfo加锁，因为如果多个人同时向同一个人赠送，那么可能会并发修改同一个RoleGiveInfo
        private Dictionary<int, RoleGiveRecvInfo> roleGiveRecvDict_dont_use_directly = new Dictionary<int, RoleGiveRecvInfo>();

         protected RoleGiveRecvInfo GetRoleGiveRecvInfo(int roleid)
        {
            bool _bLoadFromDb;
            return GetRoleGiveRecvInfo(roleid, out _bLoadFromDb);
        }

        // 使用此函数查询角色的赠送和收取数据，如果本地没有，那么将会从GameDBServer加载
        // 本函数是线程安全的，但是查询到的RoleGiveInfo不是线程安全的。对于查询得到的RoleGiveInfo一定要加锁
        protected RoleGiveRecvInfo GetRoleGiveRecvInfo(int roleid, out bool bLoadFromDb)
        {
            bLoadFromDb = false;

            lock (roleGiveRecvDict_dont_use_directly)
            {
                // 先判断是否需要重新加载
                if (roleGiveRecvDict_dont_use_directly.ContainsKey(roleid))
                {
                    RoleGiveRecvInfo oldInfo = roleGiveRecvDict_dont_use_directly[roleid];
                    if (oldInfo.TodayIdxInActPeriod == 
                        Global.GetOffsetDay(TimeUtil.NowDateTime()) - Global.GetOffsetDay(DateTime.Parse(FromDate)) + 1)
                    {
                        // 存在角色的数据，并且信息的加载日期是今天, 那么不需要重新加载
                        return oldInfo;
                    }
                }

                // 走到这里，肯定是需要重新加载了，1：当前存储的是旧数据  2：没有存储数据
                RoleGiveRecvInfo info = new RoleGiveRecvInfo();
                roleGiveRecvDict_dont_use_directly[roleid] = info;

                DateTime dtNow = TimeUtil.NowDateTime();
                // 判断是否是活动的第一天或者是最后一天
                bool bTodayIsStartDay = Global.GetOffsetDay(dtNow) == Global.GetOffsetDay(DateTime.Parse(FromDate));
                bool bTodayIsEndDay = Global.GetOffsetDay(dtNow) == Global.GetOffsetDay(DateTime.Parse(ToDate));

                // 计算本天活动的起始时间和结束时间
                string todayActStart = bTodayIsStartDay ? FromDate : dtNow.ToString("yyyy-MM-dd") + " " + "00:00:00";
                string todayActEnd = bTodayIsEndDay ? ToDate : dtNow.ToString("yyyy-MM-dd") + " " + "23:59:59";

                // 判断本天是活动的第几天
                int todayIdxInActPeriod = Global.GetOffsetDay(dtNow) - Global.GetOffsetDay(DateTime.Parse(FromDate)) + 1;

                string dbReq = string.Format("{0}:{1}:{2}:{3}:{4}", roleid, ActivityType, todayActStart.Replace(':', '$'), todayActEnd.Replace(':', '$'), todayIdxInActPeriod);
                string[] dbRsp = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_LOAD_ROLE_JIERI_GIVE_RECV_INFO, dbReq, GameManager.LocalServerId);
                if (dbRsp == null || dbRsp.Length != 3)
                {
                    info.TotalGive = 0;
                    info.TotalRecv = 0;
                    info.AwardFlag = 0;
                }
                else
                {
                    bLoadFromDb = true;
                    info.TotalGive = Convert.ToInt32(dbRsp[0]);
                    info.TotalRecv = Convert.ToInt32(dbRsp[1]);
                    info.AwardFlag = Convert.ToInt32(dbRsp[2]);
                }

                // 记录下来查询的天数信息，领奖时使用
                info.TodayStart = todayActStart;
                info.TodayEnd = todayActEnd;
                info.TodayIdxInActPeriod = todayIdxInActPeriod;

                return info;
            }
        }

        // 初始化活动，读取配置文件
        public bool Init()
        {
            string CfgFile = GetConfigFile();
            try
            {
                GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(CfgFile));
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(CfgFile));
                if (null == xml) return false;

                XElement args = xml.Element("Activities");
                if (null != args)
                {
                    FromDate = Global.GetSafeAttributeStr(args, "FromDate");
                    ToDate = Global.GetSafeAttributeStr(args, "ToDate");
                    ActivityType = (int)Global.GetSafeAttributeLong(args, "ActivityType");

                    AwardStartDate = Global.GetSafeAttributeStr(args, "AwardStartDate");
                    AwardEndDate = Global.GetSafeAttributeStr(args, "AwardEndDate");
                }

                args = xml.Element("GiftList");
                if (null != args)
                {
                    IEnumerable<XElement> xmlItems = args.Elements();
                    foreach (var xmlItem in xmlItems)
                    {
                        if (null != xmlItem)
                        {
                            AwardItem myAwardItem = new AwardItem();
                            AwardItem myAwardItem2 = new AwardItem();
                            AwardEffectTimeItem timeAwardItem = new AwardEffectTimeItem();

                            myAwardItem.MinAwardCondionValue = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "Num"));
                            myAwardItem.MinAwardCondionValue2 = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "Goods"));
                            myAwardItem.AwardYuanBao = 0;

                            string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsOne");
                            if (string.IsNullOrEmpty(goodsIDs))
                            {
                                LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项1失败", CfgFile));
                            }
                            else
                            {
                                string[] fields = goodsIDs.Split('|');
                                if (fields.Length <= 0)
                                {
                                    LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}活动配置文件中的物品配置项失败", CfgFile));
                                }
                                else
                                {
                                    myAwardItem.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, CfgFile);
                                }
                            }

                            goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsTwo");
                            if (string.IsNullOrEmpty(goodsIDs))
                            {
                                //LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项2失败", CfgFile));
                            }
                            else
                            {
                                string[] fields = goodsIDs.Split('|');
                                if (fields.Length <= 0)
                                {
                                    LogManager.WriteLog(LogTypes.Warning, CfgFile);
                                }
                                else
                                {
                                    myAwardItem2.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, CfgFile);
                                }
                            }

                            string timeGoods = Global.GetSafeAttributeStr(xmlItem, "GoodsThr");
                            string timeList = Global.GetSafeAttributeStr(xmlItem, "EffectiveTime");
                            timeAwardItem.Init(timeGoods, timeList, CfgFile + " 时效性物品");

                            string strID = Global.GetSafeAttributeStr(xmlItem, "ID");
                            int id = Convert.ToInt32(strID);
                            allAwardDict.Add(id, myAwardItem);
                            occAwardDict.Add(id, myAwardItem2);
                            timeAwardDict.Add(id, timeAwardItem);
                        }
                    }
                }

                PredealDateTime();
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", CfgFile, ex.Message));
                return false;
            }

            return true;
        }

        // 返回 ec:totalgive:totalrecv:awardflag
        public string ProcRoleGetAward(GameClient client, int awardid)
        {
            JieriGiveErrorCode ec = JieriGiveErrorCode.Success;

            do
            {
                if (!InAwardTime())
                {
                    ec = JieriGiveErrorCode.NotAwardTime;
                    break;
                }

                if (!HasEnoughBagSpaceForAwardGoods(client, awardid))
                {
                    ec = JieriGiveErrorCode.NoBagSpace;
                    break;
                }

                AwardItem allItem = null, occItem = null;
                AwardEffectTimeItem timeItem = null;
                if (!allAwardDict.TryGetValue(awardid, out allItem) || !occAwardDict.TryGetValue(awardid, out occItem)
                    || !timeAwardDict.TryGetValue(awardid, out timeItem))
                {
                    ec = JieriGiveErrorCode.ConfigError;
                    break;
                }

                RoleGiveRecvInfo info = GetRoleGiveRecvInfo(client.ClientData.RoleID);
                // 注意，这里可以不用所info，因为TotalGive和AwardFlag只能由玩家自己操作
                if (!IsReachConition(info, allItem.MinAwardCondionValue) || (info.AwardFlag & (1 << awardid)) != 0)
                {
                    ec = JieriGiveErrorCode.NotMeetAwardCond;
                    break;
                }

                int newAwardFlag = info.AwardFlag | (1 << awardid);
                // 天数信息直接取info中保存的，防止查询的和领取时恰好跨天
                string dbReq = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", client.ClientData.RoleID, info.TodayStart.Replace(':', '$'), info.TodayEnd.Replace(':', '$'), ActivityType, info.TodayIdxInActPeriod, newAwardFlag);
                string[] dbRsp = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_SPR_GET_JIERI_GIVE_AWARD, dbReq, client.ServerId);
                if (dbRsp == null || dbRsp.Length < 1 || Convert.ToInt32(dbRsp[0]) <= 0)
                {
                    ec = JieriGiveErrorCode.DBFailed;
                    break;
                }

                info.AwardFlag = newAwardFlag;
                if (!GiveAward(client, allItem) || !GiveAward(client, occItem) || !GiveEffectiveTimeAward(client, timeItem.ToAwardItem()))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("节日赠送活动奖品发送失败，但是已经设置为已发放，roleid={0}, rolename={1}, awardid={3}", client.ClientData.RoleID, client.ClientData.RoleName, awardid));
                }

                ec = JieriGiveErrorCode.Success;
            } while (false);

            // 检查更新图标
            if (ec == JieriGiveErrorCode.Success)
            {
                FlushIcon(client);
            }

            return string.Format("{0}:{1}", (int)ec, awardid);
        }

        public override bool HasEnoughBagSpaceForAwardGoods(GameClient client, Int32 id)
        {
            AwardItem allItem = null, occItem = null;
            AwardEffectTimeItem timeItem = null;
            allAwardDict.TryGetValue(id, out allItem);
            occAwardDict.TryGetValue(id, out occItem);
            timeAwardDict.TryGetValue(id, out timeItem);

            int awardCnt = 0;
            if (allItem != null && allItem.GoodsDataList != null)
            {
                awardCnt += allItem.GoodsDataList.Count;
            }
            if (occItem != null && occItem.GoodsDataList != null)
            {
                awardCnt += occItem.GoodsDataList.Count((goods) =>
                {
                    return Global.IsRoleOccupationMatchGoods(client, goods.GoodsID);
                });
            }
            if (timeItem != null)
            {
                awardCnt += timeItem.GoodsCnt();
            }
            return Global.CanAddGoodsNum(client, awardCnt);
        }   

        protected bool IsGiveGoodsID(int goodsID)
        {
            foreach (var kvp in allAwardDict)
            {
                if (kvp.Value.MinAwardCondionValue2 == goodsID)
                    return true;
            }

            foreach (var kvp in occAwardDict)
            {
                if (kvp.Value.MinAwardCondionValue2 == goodsID)
                    return true;
            }

            return false;
        }

        // 检测是否有未领取的奖励
        public bool CanGetAnyAward(GameClient client)
        {
            if (client == null) return false;
            if (!InAwardTime()) return false;

            RoleGiveRecvInfo info = GetRoleGiveRecvInfo(client.ClientData.RoleID);

            foreach (var kvp in allAwardDict)
            {
                int awardid = kvp.Key;
                AwardItem item = kvp.Value;
                if (IsReachConition(info, item.MinAwardCondionValue) && (info.AwardFlag & (1 << awardid)) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        // 跨天了，新的一天coming
        public void UpdateNewDay(GameClient client)
        {
            if (client == null) return;

            // 跨天了，要做的两件事情
            // 1： 从roleGiveRecvDict_dont_use_directly删掉该client，角色再次查询的时候将会再次加载
            // 2： 判断玩家的感叹号由激活变为非激活状态

            // 判断昨天是否有可能是激活状态
            bool IsYesterdayMayBeActive = false;

            lock (roleGiveRecvDict_dont_use_directly)
            {
                if (roleGiveRecvDict_dont_use_directly.ContainsKey(client.ClientData.RoleID))
                {
                    // 只有存有角色的数据，才有可能是激活状态，否则说明这家伙昨天都没有赠送过
                    roleGiveRecvDict_dont_use_directly.Remove(client.ClientData.RoleID);
                    IsYesterdayMayBeActive = true;
                }
            }

            if (IsYesterdayMayBeActive)
            {
                // 只有昨天有可能是激活状态的时候，才需要判断是否更新熄灭图标
                FlushIcon(client);
            }
        }
    }
}
