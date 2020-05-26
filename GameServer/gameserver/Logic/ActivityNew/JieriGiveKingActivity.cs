using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Data;
using GameServer.Server;
using GameServer.Logic;
using Server.Tools;
using System.Xml.Linq;

namespace GameServer.Logic.ActivityNew
{
    /// <summary>
    /// 节日赠送王活动
    /// </summary>
    public class JieRiGiveKingActivity : Activity
    {
        private readonly string CfgFile = "Config/JieRiGifts/JieRiZengSongKing.xml";
        private object _allMemberMutex = new object();
        // 保存角色的节日赠送王信息，服务器启动时加载前10名的，其余的角色在第一次上线时加载
        private Dictionary<int, JieriGiveKingItemData> giveDict = new Dictionary<int, JieriGiveKingItemData>();
        // 排行榜信息，由于个数比较少(10个)，所以用个list就足以
        private List<JieriGiveKingItemData> orderedGiveList = new List<JieriGiveKingItemData>();

        public Dictionary<int, AwardItem> allAwardDict = new Dictionary<int, AwardItem>();
        public Dictionary<int, AwardItem> occAwardDict = new Dictionary<int, AwardItem>();
        private Dictionary<int, AwardEffectTimeItem> timeAwardDict = new Dictionary<int, AwardEffectTimeItem>();

        private int RANK_LVL_CNT { get { return allAwardDict.Count; } }

        // 赠送了礼物
        public void OnGive(GameClient client, int goods, int cnt)
        {
            if (!InActivityTime()) return;
            if (client == null) return;

            lock (_allMemberMutex)
            {
                bool bLoadFromDb;
                JieriGiveKingItemData detail = GetClientGiveKingInfo(client, out bLoadFromDb);
                if (detail == null) return;

                if (!bLoadFromDb)
                    detail.TotalGive += cnt;

                // 检测是否存在于排行榜中, 数量级很小(10个)，先用List
                bool bExist = orderedGiveList.Any((detail1) => { return detail1.RoleID == client.ClientData.RoleID; });

                bool bAdd = false;
                //  不存在于排行中，同时排行榜未满，或者该玩家赠送数量超过了排行中最后一名，那么插入到排行榜中
                if (!bExist && (orderedGiveList.Count < RANK_LVL_CNT || orderedGiveList[RANK_LVL_CNT - 1].TotalGive < detail.TotalGive))
                {
                    orderedGiveList.Add(detail);
                    bAdd = true;
                }

                if (bExist || bAdd)
                {
                    // 已经存在于排行中，由于更新了赠送数量，需要重建排行信息
                    // 新插入的自然需要重建
                    buildRankingList(orderedGiveList);
                }
            }

            if (client._IconStateMgr.CheckJieriGiveKing(client))
            {
                client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        // 构建排行榜数据，分两步
        // 1: 排序
        // 2: 根据每个档次的赠送数量限制，进行剔除
        private void buildRankingList(List<JieriGiveKingItemData> rankingList)
        {
            rankingList.Sort((left, right) =>
            {
                if (left.TotalGive > right.TotalGive) return -1;
                else if (left.TotalGive == right.TotalGive)
                {
                    // 赠送数量一样，按角色id从小到大排序
                    return left.RoleID - right.RoleID;
                }
                else return 1;
            });

            int procListIdx = 0;
            for (int i = 1; i <= RANK_LVL_CNT && procListIdx < rankingList.Count; ++i)
            {
                AwardItem award = null;
                if (!allAwardDict.TryGetValue(i, out award))
                {
                    continue;
                }

                JieriGiveKingItemData kingItem = rankingList[procListIdx];
                if (kingItem.TotalGive >= award.MinAwardCondionValue)
                {
                    kingItem.Rank = i;
                    ++procListIdx;
                }
            }

            // 虽然进入了前N名，但是最小赠送次数不满足，剔除数据
            for (int i = rankingList.Count - 1; i >= procListIdx; --i)
            {
                rankingList[i].Rank = -1;
                rankingList.RemoveAt(i);
            }
        }

        // 服务器启动，从db加载前N条
        public void LoadRankFromDB()
        {
            if (InActivityTime() || InAwardTime())
            {
                string req = string.Format("{0}:{1}:{2}", FromDate.Replace(':', '$'), ToDate.Replace(':', '$'), RANK_LVL_CNT);
                List<JieriGiveKingItemData> items = Global.sendToDB<List<JieriGiveKingItemData>, string>(
                     (int)TCPGameServerCmds.CMD_DB_LOAD_JIERI_GIVE_KING_RANK, req, GameManager.LocalServerId);
                lock (_allMemberMutex)
                {
                    giveDict.Clear();
                    orderedGiveList.Clear();
                    if (items == null || items.Count == 0) return;

                    foreach (var item in items)
                    {
                        giveDict[item.RoleID] = item;
                        orderedGiveList.Add(item);
                    }

                    buildRankingList(orderedGiveList);
                }
            }
        }

        #region 处理客户端请求

        // 客户端请求查询节日赠送王信息
        // 这里直接返回序列化后的字节数组，不要返回result
        // 因为外部拿到result后，可能会有别的线程修改result引用的原始数据
        public byte[] QueryActivityInfo(GameClient client)
        {
            if (InActivityTime() || InAwardTime())
            {
                lock (_allMemberMutex)
                {
                    JieriGiveKingData result = new JieriGiveKingData();
                    result.MyData = GetClientGiveKingInfo(client);
                    result.RankingList = orderedGiveList;

                    return DataHelper.ObjectToBytes(result);
                }
            }
            else
            {
                return null;
            }
        }

        // 客户端请求领取奖励 return `ec:awardid`
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

                lock (_allMemberMutex)
                {
                    JieriGiveKingItemData myData = GetClientGiveKingInfo(client);
                    if (myData == null || myData.TotalGive < allItem.MinAwardCondionValue || myData.GetAwardTimes > 0 || myData.Rank != awardid)
                    {
                        ec = JieriGiveErrorCode.NotMeetAwardCond;
                        break;
                    }
                    string dbReq = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, FromDate.Replace(':', '$'), ToDate.Replace(':', '$'));
                    string[] dbRsp = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GET_JIERI_GIVE_KING_AWARD, dbReq, client.ServerId);
                    if (dbRsp == null || dbRsp.Length != 1 || Convert.ToInt32(dbRsp[0]) <= 0)
                    {
                        ec = JieriGiveErrorCode.DBFailed;
                        break;
                    }

                    myData.GetAwardTimes = 1;
                }

                if (!GiveAward(client, allItem) || !GiveAward(client, occItem) || !GiveEffectiveTimeAward(client, timeItem.ToAwardItem()))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("发送节日赠送王奖励的时候，发送失败,但是设置领奖成功，roleid={0}, rolename={1}, awardid={3}", client.ClientData.RoleID, client.ClientData.RoleName, awardid));
                }
                ec = JieriGiveErrorCode.Success;
            } while (false);

            if (ec == JieriGiveErrorCode.Success)
            {
                if (client._IconStateMgr.CheckJieriGiveKing(client))
                {
                    client._IconStateMgr.AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, client._IconStateMgr.IsAnyJieRiTipActived());
                    client._IconStateMgr.SendIconStateToClient(client);
                }
            }

            return string.Format("{0}:{1}", (int)ec, awardid);
        }

        #endregion

        private JieriGiveKingItemData GetClientGiveKingInfo(GameClient client)
        {
            bool _bLoadFromDb;
            return GetClientGiveKingInfo(client, out _bLoadFromDb);
        }

        // 外部锁_allMemberMutex
        private JieriGiveKingItemData GetClientGiveKingInfo(GameClient client, out bool bLoadFromDb)
        {
            bLoadFromDb = false;

            JieriGiveKingItemData item = null;
            if (!giveDict.TryGetValue(client.ClientData.RoleID, out item))
            {
                string cmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, FromDate.Replace(':', '$'), ToDate.Replace(':', '$'));
                item = Global.sendToDB<JieriGiveKingItemData, string>((int)TCPGameServerCmds.CMD_DB_LOAD_ROLE_JIERI_GIVE_KING, cmd, client.ServerId);
                if (item == null)
                {
                    item = new JieriGiveKingItemData();
                    item.RoleID = client.ClientData.RoleID;
                    item.Rolename = client.ClientData.RoleName;
                    item.TotalGive = 0;
                    item.Rank = -1;
                    item.GetAwardTimes = 0;
                }
                else
                    bLoadFromDb = true;

                giveDict[client.ClientData.RoleID] = item;
            }
            return item;
        }

        /// <summary>
        /// 背包中是否有足够的位置
        /// </summary>
        /// <returns></returns>
        public override bool HasEnoughBagSpaceForAwardGoods(GameClient client, Int32 id)
        {
            AwardItem allItem = null, occItem = null;
            AwardEffectTimeItem timeItem = null;
            allAwardDict.TryGetValue(id, out allItem);
            occAwardDict.TryGetValue(id, out allItem);
            timeAwardDict.TryGetValue(id, out timeItem);

            int awardCnt = 0;
            if (allItem != null && allItem.GoodsDataList != null)
            {
                awardCnt += allItem.GoodsDataList.Count;
            }
            if (occItem != null && occItem.GoodsDataList != null)
            {
                awardCnt += occItem.GoodsDataList.Count(((goods) => { return Global.IsRoleOccupationMatchGoods(client, goods.GoodsID); }));
            }
            if (timeItem != null)
            {
                awardCnt += timeItem.GoodsCnt();
            }

            return Global.CanAddGoodsNum(client, awardCnt);
        }

        public bool Init()
        {
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

                            myAwardItem.MinAwardCondionValue = Global.GMax(0, (int)Global.GetSafeAttributeLong(xmlItem, "MinYuanBao"));
                            myAwardItem.AwardYuanBao = 0;

                            string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsOne");
                            if (string.IsNullOrEmpty(goodsIDs))
                            {
                                LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项失败", CfgFile));
                            }
                            else
                            {
                                string[] fields = goodsIDs.Split('|');
                                if (fields.Length <= 0)
                                {
                                    LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项失败", CfgFile));
                                }
                                else
                                {
                                    myAwardItem.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, "大型节日赠送王活动配置");
                                }
                            }

                            goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsTwo");
                            if (string.IsNullOrEmpty(goodsIDs))
                            {
                                //LogManager.WriteLog(LogTypes.Warning, string.Format("读取大型节日消费王活动配置文件中的物品配置项1失败"));
                            }
                            else
                            {
                                string[] fields = goodsIDs.Split('|');
                                if (fields.Length <= 0)
                                {
                                    LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项失败", CfgFile));
                                }
                                else
                                {
                                    //将物品字符串列表解析成物品数据列表
                                    myAwardItem2.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, "大型节日赠送王活动配置");
                                }
                            }

                            string timeGoods = Global.GetSafeAttributeStr(xmlItem, "GoodsThr");
                            string timeList = Global.GetSafeAttributeStr(xmlItem, "EffectiveTime");
                            timeAwardItem.Init(timeGoods, timeList, "大型节日赠送王时效性物品活动配置");

                            string rankings = Global.GetSafeAttributeStr(xmlItem, "Ranking");
                            string[] paiHangs = rankings.Split('-');

                            if (paiHangs.Length <= 0)
                            {
                                continue;
                            }

                            int min = Global.SafeConvertToInt32(paiHangs[0]);
                            int max = Global.SafeConvertToInt32(paiHangs[paiHangs.Length - 1]);

                            //设置排行奖励
                            for (int paiHang = min; paiHang <= max; paiHang++)
                            {
                                allAwardDict.Add(paiHang, myAwardItem);
                                occAwardDict.Add(paiHang, myAwardItem2);
                                timeAwardDict.Add(paiHang, timeAwardItem);
                            }
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

        // 检测是否有可以领取的奖励
        public bool CanGetAnyAward(GameClient client)
        {
            if (client == null) return false;
            if (!InAwardTime()) return false;

            lock (_allMemberMutex)
            {
                foreach (var item in orderedGiveList)
                {
                    if (item.RoleID == client.ClientData.RoleID && item.GetAwardTimes <= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 角色改名
        public void OnChangeName(int roleId, string oldName, string newName)
        {
            if (!string.IsNullOrEmpty(oldName) && !string.IsNullOrEmpty(newName))
            {
                lock (_allMemberMutex)
                {
                    JieriGiveKingItemData item = null;
                    giveDict.TryGetValue(roleId, out item);
                    if (item != null)
                    {
                        item.Rolename = newName;
                    }
                }
            }
        }
    }
}
