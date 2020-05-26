using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Data;
using GameServer.Server;
using GameServer.Logic.JingJiChang;
using GameServer.Logic.ActivityNew;
using Server.Tools;
using Server.Protocol;
using Tmsk.Tools;
using GameServer.Core.Executor;
using GameServer.Logic.Building;
using GameServer.Logic.Marriage.CoupleArena;
using GameServer.Logic.Marriage.CoupleWish;

namespace GameServer.Logic.RefreshIconState
{
    /// summary
    /// 玩家各图标状态管理类
    /// summary
    public class IconStateManager
    {
        /// <summary>
        /// ICON状态项缓存，用于生成状态信息网络传输数据
        /// </summary>
        private Dictionary<ushort, ushort> m_StateIconsDict = new Dictionary<ushort, ushort>();

        /// <summary>
        /// ICON状态项缓存，用于减少网络传输次数
        /// </summary>
        private Dictionary<ushort, ushort> m_StateCacheIconsDict = new Dictionary<ushort, ushort>();

        /// <summary>
        /// ICON状态发送结构缓存，用于减少new次数
        /// </summary>
        private ActivityIconStateData m_ActivityIconStateData = new ActivityIconStateData();

        /// <summary>
        /// ICON状态Update时间点
        /// </summary>
        private long m_LastTicks = 0;

        private long m_LastTicksBuilding = 0;

        // 所有的节日活动图标提示
        private static List<ActivityTipTypes> m_jieRiIconList = new List<ActivityTipTypes>()
        {
                ActivityTipTypes.JieRiLogin,        // 节日登陆
                ActivityTipTypes.JieRiTotalLogin,        // 节日累计登陆
                ActivityTipTypes.JieRiDayCZ,        // 节日每日充值 
                ActivityTipTypes.JieRiLeiJiXF,        // 节日累计消费 
                ActivityTipTypes.JieRiLeiJiCZ,        // 节日累计充值 
                ActivityTipTypes.JieRiCZKING,        // 节日充值王 
                ActivityTipTypes.JieRiXFKING,        // 节日消费王 
                ActivityTipTypes.JieRiGive,          //节日赠送
                ActivityTipTypes.JieRiGiveKing,   //节日赠送王
                ActivityTipTypes.JieRiRecvKing,    // 节日收取王
                ActivityTipTypes.JieRiRecv,         // 节日收取
                ActivityTipTypes.JieriWing,             // 节日翅膀返利
                ActivityTipTypes.JieriAddon,            // 节日追加返利
                ActivityTipTypes.JieriStrengthen,       // 节日强化返利
                ActivityTipTypes.JieriAchievement,      // 节日成就返利
                ActivityTipTypes.JieriMilitaryRank,     // 节日军衔返利
                ActivityTipTypes.JieriVIPFanli,         // 节日VIP返利
                ActivityTipTypes.JieriAmulet,           // 节日护身符返利
                ActivityTipTypes.JieriArchangel,        // 节日大天使返利
                ActivityTipTypes.JieriMarriage,         // 节日婚姻返利
                ActivityTipTypes.JieRiLianXuCharge,     // 节日连续充值
                ActivityTipTypes.JieRiIPointsExchg,     // 节日充值积分兑换
                ActivityTipTypes.JieRiPlatChargeKing, // 节日平台充值王
        };

        /// <summary>
        /// 添加刷新项到状态字典
        /// </summary>
        public bool AddFlushIconState(ushort nIconOrder, bool bIconState)
        {
            ushort iState = (ushort)(bIconState ? 1 : 0);
            return AddFlushIconState(nIconOrder, iState);
        }

        /// <summary>
        /// 添加刷新项到状态字典
        /// </summary>
        public bool AddFlushIconState(ushort nIconOrder, ushort iState)
        {
            ushort nIconInfo = (ushort)((nIconOrder << 1) + iState);

            ushort nOldState = 0;
            lock (m_StateIconsDict)
            {
                // 原来缓存中没有值，需要刷新图标状态
                if (!m_StateCacheIconsDict.TryGetValue(nIconOrder, out nOldState))
                {
                    m_StateCacheIconsDict[nIconOrder] = nIconInfo;
                    m_StateIconsDict[nIconOrder] = nIconInfo;
                    // LogManager.WriteLog(LogTypes.Info, "!m_StateCacheIconsDict nIconOrder:" + nIconOrder + ", nIconInfo:" + nIconInfo + ", state:" + iState);
                    return true;
                }
                else
                {
                    // 如果设置的值和原来的相等，则不刷新
                    if ((nOldState & 0x1) == iState)
                    {
                        // LogManager.WriteLog(LogTypes.Info, "(nOldState % 2) == iState nIconOrder:" + nIconOrder + ", nOldState:" + nOldState + ", state:" + iState);
                        return false;
                    }
                    // 不相等，则更新
                    else
                    {
                        // LogManager.WriteLog(LogTypes.Info, "(nOldState % 2) == iState false nIconOrder:" + nIconOrder + ", nIconInfo:" + nIconInfo + ", nOldState:" + nOldState + ", state:" + iState);
                        m_StateCacheIconsDict[nIconOrder] = nIconInfo;
                        m_StateIconsDict[nIconOrder] = nIconInfo;
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// 重置ICON状态项缓存
        /// </summary>
        public void ResetIconStateDict(bool bIsLogin)
        {
            lock (m_StateIconsDict)
            {
                if (true == bIsLogin)
                {
                    // LogManager.WriteLog(LogTypes.Info, "m_StateCacheIconsDict.Clear()");
                    m_StateCacheIconsDict.Clear();
                }

                // LogManager.WriteLog(LogTypes.Info, "m_StateIconsDict.Clear()");
                m_StateIconsDict.Clear();
            }
        }

        /// <summary>
        /// 发送ICON消息到客户端
        /// </summary>
        public void SendIconStateToClient(GameClient client)
        {
            ushort[] arrState = null;
            int nIconStateCount;

            lock (m_StateIconsDict)
            {
                nIconStateCount = m_StateIconsDict.Count();
                if (nIconStateCount > 0)
                {
                    arrState = new ushort[nIconStateCount];
                    nIconStateCount = 0;

                    foreach (KeyValuePair<ushort, ushort> kvp in m_StateIconsDict)
                    {
                        arrState[nIconStateCount++] = kvp.Value;
                    }
                }

                if (null != arrState && arrState.Length > 0)
                {
                    m_ActivityIconStateData.arrIconState = arrState;
                    client.sendCmd<ActivityIconStateData>((int)TCPGameServerCmds.CMD_SPR_REFRESH_ICON_STATE, m_ActivityIconStateData);

                    // 发送自动重置状态缓存列表
                    ResetIconStateDict(false);
                }
            }
        }

        /// <summary>
        /// 用户登录时刷新图标状态
        /// </summary>
        public void LoginGameFlushIconState(GameClient client)
        {
            ResetIconStateDict(true);

            // 日常活动及子项图标状态刷新
            CheckHuangJinBoss(client);
            CheckShiJieBoss(client);
            CheckHuoDongState(client);

            // 福利及子项图标状态刷新
            CheckFuLiMeiRiHuoYue(client);
            CheckFuLiLianXuDengLu(client);
            CheckFuLiLeiJiDengLu(client);
            CheckFuMeiRiZaiXian(client);
            CheckFuUpLevelGift(client);
            CheckFuLiYueKaFanLi(client);

            // 充值相关刷新
            FlushChongZhiIconState(client);
            FlushUsedMoneyconState(client);

            // 竞技场及子项图标状态刷新
            CheckJingJiChangLeftTimes(client);
            CheckJingJiChangJiangLi(client);
            CheckJingJiChangJunXian(client);

            // 每日必做图标刷新
            CheckZiYuanZhaoHui(client);

            //邮件状态
            CheckEmailCount(client, false);

            //免费祈福
            CheckFreeImpetrateState(client);

            //成就称号升级状态
            CheckChengJiuUpLevelState(client);

            //VIP奖励领取状态
            CheckVIPLevelAwardState(client);

            // 合服活动检测
            CheckHeFuActivity(client);

            // 节日活动检测
            CheckJieRiActivity(client, true);

            // 专享活动
            CheckSpecialActivity(client);

            CheckGuildIcon(client, true);
            CheckGuildIcon(client, true);

            // 检查精灵icon
            CheckPetIcon(client);

            // 领地icon
            CheckBuildingIcon(client, true);

            // 发送刷新项到客户端
            SendIconStateToClient(client);

            // 补偿状态检测
            CheckBuChangState(client);

            CheckCaiJiState(client);

            // 梅林秘语
            GameManager.MerlinMagicBookMgr.CheckMerlinSecretAttr(client);
        }

        /// <summary>
        /// 充值后刷新与充值相关的ICON状态
        /// </summary>
        public bool FlushChongZhiIconState(GameClient client)
        {
            CheckShouCiChongZhi(client);
            CheckMeiRiChongZhi(client);
            CheckLeiJiChongZhi(client);

            CheckXinFuChongZhiMoney(client);
            CheckXinFuFreeGetMoney(client);

            CheckSpecialActivity(client);
            return false;
        }

        /// <summary>
        /// 消费后刷新与充值相关的ICON状态
        /// </summary>
        public bool FlushUsedMoneyconState(GameClient client)
        {
            CheckLeiJiXiaoFei(client);
            CheckXinFuUseMoney(client);
            CheckSpecialActivity(client);
            return false;
        }

        /// <summary>
        /// 检查“每日活跃”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckFuLiMeiRiHuoYue(GameClient client)
        {
            // 判断目前的活跃值是否够领奖
            foreach (KeyValuePair<int, SystemXmlItem> kvp in GameManager.systemDailyActiveAward.SystemXmlItemDict)
            {
                int nAwardDailyActiveValue = Math.Max(0, kvp.Value.GetIntValue("NeedhuoYue"));
                int nID = kvp.Value.GetIntValue("ID");

                // 活跃值够
                if (nAwardDailyActiveValue <= client.ClientData.DailyActiveValues)
                {
                    // 还没领过奖
                    if (DailyActiveManager.IsDailyActiveAwardFetched(client, nID) <= 0)
                    {
                        return AddFlushIconState((ushort)ActivityTipTypes.FuLiMeiRiHuoYue, true);
                    }
                }
            }

            // 活跃值不够或已经领过奖了
            return AddFlushIconState((ushort)ActivityTipTypes.FuLiMeiRiHuoYue, false);
        }

        /// <summary>
        /// 检查“连续登录”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckFuLiLianXuDengLuReward(GameClient client)
        {
            int nDay = TimeUtil.NowDateTime().DayOfYear;
            bool bFulsh = true;
            if (client.ClientData.MyHuodongData.SeriesLoginAwardDayID == nDay && client.ClientData.MyHuodongData.SeriesLoginGetAwardStep <= client.ClientData.SeriesLoginNum)
            {
                // 今天没有抽奖次数了
                bFulsh = false;
            }

            return bFulsh;
        }

        /// <summary>
        /// 检查“连续登录”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckFuLiLianXuDengLu(GameClient client)
        {
            bool bFulsh = CheckFuLiLianXuDengLuReward(client);

            return AddFlushIconState((ushort)ActivityTipTypes.FuLiLianXuDengLu, bFulsh);
        }

        /// <summary>
        /// 检查“累计登陆”项是否有奖励未领取
        /// </summary>
        public bool CheckFuLiLeiJiDengLuReward(GameClient client)
        {
            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalLoginAwardFlag);

            int nLoginNum = (int)ChengJiuManager.GetChengJiuExtraDataByField(client, ChengJiuExtraDataField.TotalDayLogin);
            int nMaxLoginNum = /*Data.TotalLoginDataInfoList.Count();*/ Data.GetTotalLoginInfoNum();
            bool bFulsh = false;

            for (int i = 0; i < 7 && i < nLoginNum && i < nMaxLoginNum; i++)
            {
                // 还有未领取的累计登陆奖励
                if ((nFlag & (0x1 << (i + 1))) == 0)
                {
                    bFulsh = true;
                    break;
                }
            }

            if (nLoginNum == 30)
            {
                if ((nFlag & (0x1 << (10))) == 0)
                {
                    bFulsh = true;
                }
            }

            if (nLoginNum == 21)
            {
                if ((nFlag & (0x1 << 9)) == 0)
                {
                    bFulsh = true;
                }
            }

            if (nLoginNum == 14)
            {
                if ((nFlag & (0x1 << (8))) == 0)
                {
                    bFulsh = true;
                }
            }

            return bFulsh;
        }

        /// <summary>
        /// 检查“累计登陆”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckFuLiLeiJiDengLu(GameClient client)
        {
            bool bFulsh = CheckFuLiLeiJiDengLuReward(client);

            return AddFlushIconState((ushort)ActivityTipTypes.FuLiLeiJiDengLu, bFulsh);
        }

        /// <summary>
        /// 检查月卡返利是否有图标更新
        /// </summary>
        /// <param name="client"></param>
        public bool CheckFuLiYueKaFanLi(GameClient client)
        {
            if (client == null)
            {
                return false;
            }

            int dayIdx = client.ClientData.YKDetail.CurDayOfPerYueKa() - 1;
            if (client.ClientData.YKDetail.HasYueKa == 1
                && dayIdx >= 0 
                && dayIdx < client.ClientData.YKDetail.AwardInfo.Length
                && client.ClientData.YKDetail.AwardInfo[dayIdx] == '1')
            {
                return AddFlushIconState((ushort)ActivityTipTypes.FuLiYueKaFanLi, false);
            }
            else
            {
                return AddFlushIconState((ushort)ActivityTipTypes.FuLiYueKaFanLi, true);
            }
        }

        /// <summary>
        /// 检查“每日在线”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckFuMeiRiZaiXian(GameClient client)
        {
            int nDate = TimeUtil.NowDateTime().DayOfYear;

            if (client.ClientData.MyHuodongData.GetEveryDayOnLineAwardDayID != nDate)
            {
                client.ClientData.MyHuodongData.EveryDayOnLineAwardStep = 0;
                client.ClientData.MyHuodongData.GetEveryDayOnLineAwardDayID = nDate;
            }

            int nSetp = client.ClientData.MyHuodongData.EveryDayOnLineAwardStep;

            // 一共能领几次
            int nTotal = HuodongCachingMgr.GetEveryDayOnLineItemCount();
            if (nTotal == client.ClientData.MyHuodongData.EveryDayOnLineAwardStep)
            {
                // 今天已经全部领完了
                return AddFlushIconState((ushort)ActivityTipTypes.FuLiMeiRiZaiXian, false);
            }

            int nIndex1 = nTotal - client.ClientData.MyHuodongData.EveryDayOnLineAwardStep;
            EveryDayOnLineAward EveryDayOnLineAwardItem = null;
            for (int n = client.ClientData.MyHuodongData.EveryDayOnLineAwardStep + 1; n <= nTotal; ++n)
            {
                EveryDayOnLineAwardItem = HuodongCachingMgr.GetEveryDayOnLineItem(n);
                if (null == EveryDayOnLineAwardItem)
                {
                    return false;
                }

                // 如果已到领取的时间
                if (client.ClientData.DayOnlineSecond >= EveryDayOnLineAwardItem.TimeSecs)
                {
                    return AddFlushIconState((ushort)ActivityTipTypes.FuLiMeiRiZaiXian, true);
                }
            }

            return AddFlushIconState((ushort)ActivityTipTypes.FuLiMeiRiZaiXian, false);
        }

        /// <summary>
        /// 检查“等级奖励”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckFuUpLevelGift(GameClient client)
        {
            List<int> flagList = Global.GetRoleParamsIntListFromDB(client, RoleParamName.UpLevelGiftFlags);
            bool exist = false;
            for (int i = 0; i < flagList.Count * 16; i++)
            {
                if (Global.GetBitValue(flagList, i * 2) == 1 && Global.GetBitValue(flagList, i * 2 + 1) == 0)
                {
                    exist = true;
                    break;
                }
            }

            return AddFlushIconState((ushort)ActivityTipTypes.FuLiUpLevelGift, exist);
        }

        /// <summary>
        /// 检查“充值回馈”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckFuLiChongZhiHuiKui(GameClient client)
        {
            bool bShouCiChongZhi = CheckShouCiChongZhi(client);
            bool bMeiRiChongZhi = CheckMeiRiChongZhi(client);
            bool bLeiJiChongZhi = CheckLeiJiChongZhi(client);
            bool bLeiJiXiaoFei = CheckLeiJiXiaoFei(client);

            // 有任意子项要更新，主图标更新
            if (bShouCiChongZhi || bMeiRiChongZhi || bLeiJiChongZhi || bLeiJiXiaoFei)
            {
                return AddFlushIconState((ushort)ActivityTipTypes.FuLiChongZhiHuiKui, true);
            }

            return AddFlushIconState((ushort)ActivityTipTypes.FuLiChongZhiHuiKui, false);
        }

        /// <summary>
        /// 检查“充值回馈-首次充值”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckShouCiChongZhi(GameClient client)
        {
            int totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);
            if (totalChongZhiMoney > 0)
            {
                if (Global.CanGetFirstChongZhiDaLiByUserID(client))
                {
                    AddFlushIconState((ushort)ActivityTipTypes.ShouCiChongZhi_YiLingQu, 0);
                    return AddFlushIconState((ushort)ActivityTipTypes.ShouCiChongZhi, 1);
                }
                else
                {
                    AddFlushIconState((ushort)ActivityTipTypes.ShouCiChongZhi_YiLingQu, 1);
                }
            }
            else
            {
                AddFlushIconState((ushort)ActivityTipTypes.ShouCiChongZhi_YiLingQu, 0);
            }

            return AddFlushIconState((ushort)ActivityTipTypes.ShouCiChongZhi, 0);
        }

        /// <summary>
        /// 检查“充值回馈-每日充值”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckMeiRiChongZhi(GameClient client)
        {
            bool hasGet;
            bool ret = RechargeRepayActiveMgr.CheckRechargeReplay(client, ActivityTypes.MeiRiChongZhiHaoLi, out hasGet);
            AddFlushIconState((ushort)ActivityTipTypes.MeiRiChongZhi_YiLingQu, hasGet);

            // 如果在活动时间内 and 今天没打开过界面
            WeedEndInputActivity act = HuodongCachingMgr.GetWeekEndInputActivity();
            if (null != act && act.InAwardTime())
            {
                int currday = Global.GetOffsetDay(TimeUtil.NowDateTime());
                if(act.GetWeekEndInputOpenDay(client) != currday)
                {
                    ret = ret | true;
                }
            }
            return AddFlushIconState((ushort)ActivityTipTypes.MeiRiChongZhi, ret);
        }

        /// <summary>
        /// 检查“充值回馈-累积充值”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckLeiJiChongZhi(GameClient client)
        {
            bool hasGet;
            bool ret = RechargeRepayActiveMgr.CheckRechargeReplay(client, ActivityTypes.TotalCharge, out hasGet);
            return AddFlushIconState((ushort)ActivityTipTypes.LeiJiChongZhi, ret);
        }

        /// <summary>
        /// 检查“充值回馈-累计消费”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckLeiJiXiaoFei(GameClient client)
        {
            bool hasGet;
            bool ret = RechargeRepayActiveMgr.CheckRechargeReplay(client, ActivityTypes.TotalConsume, out hasGet);
            return AddFlushIconState((ushort)ActivityTipTypes.LeiJiXiaoFei, ret);
        }

        /// <summary>
        /// 检查“主新服图标”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckMainXinFuIcon(GameClient client)
        {
            return false;
            // 此功能没有，暂不添加
            // return AddFlushIconState((ushort)ActivityTipTypes.FuLiLeiJiDengLu, false);
        }

        /// <summary>
        /// 检查“屠魔勇士”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckXinFuKillBoss(GameClient client)
        {
            return false;
            // 此功能没有，暂不添加
            // return AddFlushIconState((ushort)ActivityTipTypes.FuLiLeiJiDengLu, false);
        }

        /// <summary>
        /// 检查“充值达人”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckXinFuChongZhiMoney(GameClient client)
        {
            return false;
            // 此功能没有，暂不添加
            // return AddFlushIconState((ushort)ActivityTipTypes.FuLiLeiJiDengLu, false);
        }

        /// <summary>
        /// 检查“消费达人”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckXinFuUseMoney(GameClient client)
        {
            return false;
            // 此功能没有，暂不添加
            // return AddFlushIconState((ushort)ActivityTipTypes.FuLiLeiJiDengLu, false);
        }

        /// <summary>
        /// 检查“劲爆返利”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckXinFuFreeGetMoney(GameClient client)
        {
            return false;
            // 此功能没有，暂不添加
            // return AddFlushIconState((ushort)ActivityTipTypes.FuLiLeiJiDengLu, false);
        }


        /// <summary>
        /// 检查“剩余挑战次数”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckJingJiChangLeftTimes(GameClient client)
        {
            if (JingJiChangManager.getInstance().checkEnterNum(client, (int)JingJiChangManager.Enter_Type_Free) == ResultCode.Success)
            {
                return AddFlushIconState((ushort)ActivityTipTypes.JingJiChangLeftTimes, true);
            }

            return AddFlushIconState((ushort)ActivityTipTypes.JingJiChangLeftTimes, false);
        }

        /// <summary>
        /// 检查“奖励预览”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckJingJiChangJiangLi(GameClient client)
        {
            if (JingJiChangManager.getInstance().CanGetrankingReward(client))
            {
                return AddFlushIconState((ushort)ActivityTipTypes.JingJiChangJiangLi, true);
            }

            return AddFlushIconState((ushort)ActivityTipTypes.JingJiChangJiangLi, false);
        }

        /// <summary>
        /// 检查“军衔提升”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckJingJiChangJunXian(GameClient client)
        {
            if (JingJiChangManager.getInstance().CanGradeJunXian(client))
            {
                return AddFlushIconState((ushort)ActivityTipTypes.JingJiChangJunXian, true);
            }

            return AddFlushIconState((ushort)ActivityTipTypes.JingJiChangJunXian, false);
        }

        /// <summary>
        /// 检查“世界BOSS”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckShiJieBoss(GameClient client)
        {
            if (TimerBossManager.getInstance().HaveWorldBoss(client))
            {
                return AddFlushIconState((ushort)ActivityTipTypes.ShiJieBoss, true);
            }

            return AddFlushIconState((ushort)ActivityTipTypes.ShiJieBoss, false);
        }

        /// <summary>
        /// 活动项是否要显示要更新图标状态
        /// </summary>
        public bool CheckHuoDongState(GameClient client)
        {
            if (GameManager.AngelTempleMgr.CanEnterAngelTempleOnTime())
            {
                return AddFlushIconState((ushort)ActivityTipTypes.AngelTemple, true);
            }
            AddFlushIconState((ushort)ActivityTipTypes.AngelTemple, false);

            return true;
        }

        /// <summary>
        /// 检查“黄金部队”项是否要显示要更新图标状态
        /// </summary>
        public bool CheckHuangJinBoss(GameClient client)
        {
            if (TimerBossManager.getInstance().HaveHuangJinBoss(client))
            {
                return AddFlushIconState((ushort)ActivityTipTypes.HuangJinBoss, true);
            }

            return AddFlushIconState((ushort)ActivityTipTypes.HuangJinBoss, false);
        }


        /// <summary>
        /// 检查“资源找回”图标状态
        /// </summary>
        public bool CheckZiYuanZhaoHui(GameClient client)
        {
            if (CGetOldResourceManager.HasOldResource(client))
            {
                return AddFlushIconState((ushort)ActivityTipTypes.ZiYuanZhaoHui, true);
            }

            return AddFlushIconState((ushort)ActivityTipTypes.ZiYuanZhaoHui, false);
        }

        /// <summary>
        /// 检查是否有未读邮件
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckEmailCount(GameClient client, bool sendToClient = true)
        {
            bool result;
            string cmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, 1, 1);
            int emailCount = Global.sendToDB<int, string>((int)TCPGameServerCmds.CMD_SPR_GETUSERMAILCOUNT, cmd, client.ServerId);
            if (emailCount > 0)
            {
                result = AddFlushIconState((ushort)ActivityTipTypes.MainEmailIcon, true);
            }
            else
            {
                result = AddFlushIconState((ushort)ActivityTipTypes.MainEmailIcon, false);
            }

            if (result && sendToClient)
            {
                SendIconStateToClient(client);
            }

            return result;
        }

        /// <summary>
        /// 检查是否可提升成就称号
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckChengJiuUpLevelState(GameClient client)
        {
            bool result = AddFlushIconState((ushort)ActivityTipTypes.MainChengJiuIcon, ChengJiuManager.CanActiveNextChengHao(client));
            SendIconStateToClient(client);
            return result;
        }

        /// <summary>
        /// 检查是否可领取VIP奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckVIPLevelAwardState(GameClient client)
        {
            for (int nIndex = 1; nIndex <= client.ClientData.VipLevel; nIndex++)
            {
                int nFlag = client.ClientData.VipAwardFlag & Global.GetBitValue(nIndex + 1);
                if (nFlag < 1)
                {
                    return AddFlushIconState((ushort)ActivityTipTypes.VIPGifts, true);
                }
            }

            return AddFlushIconState((ushort)ActivityTipTypes.VIPGifts, false);
        }

        /// <summary>
        /// 检查免费祈福状态
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckFreeImpetrateState(GameClient client)
        {
            bool bFlush = false;

            DateTime dTime1 = TimeUtil.NowDateTime();
            DateTime dTime2 = Global.GetRoleParamsDateTimeFromDB(client, RoleParamName.ImpetrateTime);

            TimeSpan dTimeSpan = dTime1 - dTime2;

            double dSecond = 0.0;
            dSecond = dTimeSpan.TotalSeconds;

            double dRet = 0.0;
            dRet = Global.GMax(0, Data.FreeImpetrateIntervalTime - dSecond);
            if (dRet <= 0)
            {
                bFlush = true;
            }

            return AddFlushIconState((ushort)ActivityTipTypes.QiFuIcon, bFlush);
        }

        /// <summary>
        /// 检查是否可领取VIP奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckBuChangState(GameClient client)
        {
            bool bFlush = BuChangManager.CheckGiveBuChang(client);

            return AddFlushIconState((ushort)ActivityTipTypes.BuChangIcon, bFlush);
        }

        /// <summary>
        /// 检查合服活动领取标记
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckHeFuActivity(GameClient client)
        {
            AddFlushIconState((ushort)ActivityTipTypes.HeFuLogin, false);
            AddFlushIconState((ushort)ActivityTipTypes.HeFuTotalLogin, false);
            AddFlushIconState((ushort)ActivityTipTypes.HeFuRecharge, false);
            AddFlushIconState((ushort)ActivityTipTypes.HeFuPKKing, false);
            AddFlushIconState((ushort)ActivityTipTypes.HeFuLuoLan, false);

            bool bFlush = false;
            // 登录好礼
            bFlush = CheckHeFuLogin(client) | bFlush;
            // 累计登陆
            bFlush = CheckHeFuTotalLogin(client) | bFlush;
            // 充值返利
            bFlush = CheckHeFuRecharge(client) | bFlush;
            // 战场之神
            bFlush = CheckHeFuPKKing(client) | bFlush;
            // 合服罗兰城主
            bFlush = CheckHeFuLuoLan(client) | bFlush;

            return AddFlushIconState((ushort)ActivityTipTypes.HeFuActivity, bFlush);
        }

        /// <summary>
        /// 检查合服登陆豪礼领取标记
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckHeFuLogin(GameClient client)
        {
            HeFuLoginActivity activity = HuodongCachingMgr.GetHeFuLoginActivity();
            if (null == activity)
            {
                return false;
            }

            // 检查是否在允许领取的时间内
            if (!activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuLoginFlag);
                int nValue = Global.GetIntSomeBit(nFlag, (int)HeFuLoginFlagTypes.HeFuLogin_Login);
                // 是否在活动期间登陆过
                if (nValue == 0)
                {
                    break;
                }

                // 检查是否已经领取普通奖励
                nValue = Global.GetIntSomeBit(nFlag, (int)HeFuLoginFlagTypes.HeFuLogin_NormalAward);
                if (nValue == 0)
                {
                    bFlush = true;
                    break;
                }

                // 如果普通奖励已经领取则检测是否领取vip奖励
                nValue = Global.GetIntSomeBit(nFlag, (int)HeFuLoginFlagTypes.HeFuLogin_VIPAward);
                // 没领取判断是不是vip
                if (nValue == 0)
                {
                    // 判断玩家是不是VIP
                    if (Global.IsVip(client))
                    {
                        bFlush = true;
                        break;
                    }
                }
                break;
            }

            return AddFlushIconState((ushort)ActivityTipTypes.HeFuLogin, bFlush); ;
        }

        /// <summary>
        /// 检查合服累计登陆领取标记
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckHeFuTotalLogin(GameClient client)
        {
            HeFuTotalLoginActivity activity = HuodongCachingMgr.GetHeFuTotalLoginActivity();
            if (null == activity)
            {
                return false;
            }
            // 检查是否在允许领取的时间内
            if (!activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {


                // 玩家登陆的总数
                int totalloginnum = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuTotalLoginNum);
                // 依次检查是否有满足条件的没领取的奖励
                for (int i = 1; i <= totalloginnum; i++)
                {
                    if (activity.GetAward(i) == null)
                        continue;

                    int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuTotalLoginFlag);
                    //int nValue = nFlag & Global.GetBitValue(i);
                    int nValue = Global.GetIntSomeBit(nFlag, i);
                    // 发现有一天没领取
                    if (nValue == 0)
                    {
                        bFlush = true;
                        break;
                    }
                }
                break;
            }

            return AddFlushIconState((ushort)ActivityTipTypes.HeFuTotalLogin, bFlush); ;
        }

        /// <summary>
        /// 检查用户合服充值豪礼状态
        /// 会向GameDBServer申请数据库查询，请避免在同一时间点一起申请
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckHeFuRecharge(GameClient client)
        {
            int currday = Global.GetOffsetDay(TimeUtil.NowDateTime());
            int hefuday = Global.GetOffsetDay(Global.GetHefuStartDay());
            // 活动开始的第一天没有数据
            if (currday == hefuday)
            {
                return false;
            }
            HeFuRechargeActivity activity = HuodongCachingMgr.GetHeFuRechargeActivity();
            if (null == activity)
            {
                return false;
            }
            if (!activity.InActivityTime() && !activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                string[] dbFields = null;
                // 向DB申请数据
                //TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, (int)ActivityTypes.HeFuRecharge, hefuday, activity.strcoe), out dbFields);
                TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO,
                                string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, (int)ActivityTypes.HeFuRecharge, hefuday, Global.GetOffsetDay(DateTime.Parse(activity.ToDate)), activity.strcoe), out dbFields, client.ServerId);

                if (null == dbFields)
                    break;

                if (null == dbFields || 1 != dbFields.Length)
                    break;

                string[] strrebate = dbFields[0].Split('|');
                if (1 > dbFields.Length)
                    break;

                bFlush = Convert.ToInt32(strrebate[0]) > 0;
                break;
            }

            return AddFlushIconState((ushort)ActivityTipTypes.HeFuRecharge, bFlush);
        }

        /// <summary>
        /// 检查用户PK之王领奖状态
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckHeFuPKKing(GameClient client)
        {
            HeFuPKKingActivity activity = HuodongCachingMgr.GetHeFuPKKingActivity();
            if (null == activity)
            {
                return false;
            }
            // 检查是否在允许领取的时间内
            if (!activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                // 判断玩家是不是战场之神
                if (client.ClientData.RoleID != HuodongCachingMgr.GetHeFuPKKingRoleID())
                {
                    break;
                }

                // 判断是否已经领取
                int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuPKKingFlag);
                if (nFlag == 0)
                {
                    bFlush = true;
                    break;
                }
                break;
            }
            return AddFlushIconState((ushort)ActivityTipTypes.HeFuPKKing, bFlush); ;
        }

        /// <summary>
        /// 检查用户PK之王领奖状态
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckHeFuLuoLan(GameClient client)
        {
            HeFuLuoLanActivity activity = HuodongCachingMgr.GetHeFuLuoLanActivity();
            if (null == activity)
            {
                return false;
            }
            // 检查是否在允许领取的时间内
            if (!activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                int guildwinnum = 0;
                int chengzhuwinnum = 0;
                int guizuwinnum = 0;

                // 向客户端发送罗兰城战记录和合服罗兰城主活动领奖记录
                string strHefuLuolanGuildid = GameManager.GameConfigMgr.GetGameConfigItemStr(GameConfigNames.hefu_luolan_guildid, "");
                string[] strFields = strHefuLuolanGuildid.Split('|');
                for (int i = 0; i < strFields.Length; ++i)
                {
                    string[] strInfos = strFields[i].Split(',');
                    if (2 != strInfos.Length)
                    {
                        continue;
                    }
                    // 计算帮会胜利过几次
                    if (Convert.ToInt32(strInfos[0]) == client.ClientData.Faction)
                    {
                        guildwinnum++;
                        // 计算不是帮主才是贵族
                        if (Convert.ToInt32(strInfos[1]) == client.ClientData.RoleID)
                        {
                        }
                        else
                        {
                            guizuwinnum++;
                        }
                    }
                    // 计算个人当过几次城主
                    if (Convert.ToInt32(strInfos[1]) == client.ClientData.RoleID)
                    {
                        chengzhuwinnum++;
                    }
                }

                int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuLuoLanAwardFlag);

                foreach (var item in activity.HeFuLuoLanAwardDict)
                {
                    HeFuLuoLanAward hefuLuoLanAward = item.Value;
                    // 如果是城主领奖励
                    if (1 == hefuLuoLanAward.status)
                    {
                        // 满足领奖条件
                        if (chengzhuwinnum >= hefuLuoLanAward.winNum)
                        {
                            int nValue = Global.GetIntSomeBit(nFlag, item.Key);
                            // 没领取过
                            if (0 == nValue)
                            {
                                bFlush = true;
                                break;
                            }
                        }
                    }
                    else if (2 == hefuLuoLanAward.status)
                    {
                        // 满足领奖条件
                        if (guizuwinnum >= hefuLuoLanAward.winNum)
                        {
                            int nValue = Global.GetIntSomeBit(nFlag, item.Key);
                            // 没领取过
                            if (0 == nValue)
                            {
                                bFlush = true;
                                break;
                            }
                        }
                    }
                }

                break;
            }
            return AddFlushIconState((ushort)ActivityTipTypes.HeFuLuoLan, bFlush); ;
        }

        public bool CheckCaiJiState(GameClient client)
        {
            //水晶幻境采集状态检测
            return AddFlushIconState((ushort)ActivityTipTypes.ShuiJingHuangJin, CaiJiLogic.HasLeftnum(client));
        }

        /// <summary>
        /// 专享活动
        /// <summary>
        public bool CheckSpecialActivity(GameClient client)
        {
            bool bFlush = false;
            SpecialActivity act = HuodongCachingMgr.GetSpecialActivity();
            if (null == act)
                return false;

            bFlush = act.CheckIconState(client);
            return AddFlushIconState((ushort)ActivityTipTypes.ZhuanXiang, bFlush);
        }

        /// <summary>
        /// 检查用户节日活动总叹号
        /// </summary>
        public bool CheckJieRiActivity(GameClient client, bool isLogin)
        {
            if (isLogin)
            {
                AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, false);

                AddFlushIconState((ushort)ActivityTipTypes.JieRiLogin, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiTotalLogin, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiDayCZ, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiLeiJiXF, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiLeiJiCZ, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiCZKING, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiXFKING, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiGive, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiGiveKing, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiRecvKing, false);

                AddFlushIconState((ushort)ActivityTipTypes.JieriWing, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieriAddon, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieriStrengthen, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieriAchievement, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieriMilitaryRank, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieriVIPFanli, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieriAmulet, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieriArchangel, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieriMarriage, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiLianXuCharge, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiRecv, false);
                AddFlushIconState((ushort)ActivityTipTypes.JieRiIPointsExchg, false);
            }

            bool bAnyChildTipChanged = false;
            // 节日登陆
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiLogin(client);
            // 节日累计登陆
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiTotalLogin(client);
            // 节日每日充值
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiDayCZ(client);
            // 节日累计消费
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiLeiJiXF(client);
            // 节日累计充值
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiLeiJiCZ(client);
            // 节日充值王
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiCZKING(client);
            // 节日消费王
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiXFKING(client);
            // [bing] 节日返利活动
            {
                bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiFanLi(client, ActivityTypes.JieriWing);
                bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiFanLi(client, ActivityTypes.JieriAddon);
                bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiFanLi(client, ActivityTypes.JieriStrengthen);
                bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiFanLi(client, ActivityTypes.JieriAchievement);
                bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiFanLi(client, ActivityTypes.JieriMilitaryRank);
                bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiFanLi(client, ActivityTypes.JieriVIPFanli);
                bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiFanLi(client, ActivityTypes.JieriAmulet);
                bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiFanLi(client, ActivityTypes.JieriArchangel);
                bAnyChildTipChanged = bAnyChildTipChanged | CheckJieRiFanLi(client, ActivityTypes.JieriMarriage);
            }
            // 节日赠送
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieriGive(client);
            // 节日赠送王
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieriGiveKing(client);
            // 节日收取王
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieriRecvKing(client);
            // 节日连续充值活动
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieriLianXuCharge(client);
            // 节日收取活动
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieriRecv(client);
            // 节日积分兑换
            bAnyChildTipChanged = bAnyChildTipChanged | CheckJieriIPointsExchg(client);

            // 检查是否有任意一项自节日活动点亮了
            bool isJieRiActivityTipActived = IsAnyJieRiTipActived();
            
            // 如果总节日活动图标变化，那么必须通知客户端
            if (AddFlushIconState((ushort)ActivityTipTypes.JieRiActivity, isJieRiActivityTipActived))
                return true;

            // 虽然节日活动总图标没有变化，但是有子图标变化了，也通知客户端
            if (bAnyChildTipChanged)
                return true;

            return false;
        }

        /// <summary>
        /// 检查是否有任意一项节日活动图标被点亮
        /// </summary>
        public bool IsAnyJieRiTipActived()
        {
            return IsAnyTipActived(m_jieRiIconList);
        }

        /// <summary>
        /// 检查是否有图标中的任意一项被点亮
        /// </summary>
        public bool IsAnyTipActived(List<ActivityTipTypes> iconTipList)
        {
            bool bAnyActived = false;

            if (iconTipList != null)
            {
                lock (m_StateCacheIconsDict)
                {
                    foreach (var e_tip in iconTipList)
                    {
                        ushort state = 0;
                        if (m_StateCacheIconsDict.TryGetValue((ushort)e_tip, out state))
                        {
                            if ((state & 0x01) == 0x01)
                            {
                                bAnyActived = true;
                                break;
                            }
                        }
                    }
                }
            }

            return bAnyActived;
        }

        /// <summary>
        /// 节日登陆
        /// </summary>
        public bool CheckJieRiLogin(GameClient client)
        {
            JieriDaLiBaoActivity activity = HuodongCachingMgr.GetJieriDaLiBaoActivity();
            if (null == activity)
            {
                return false;
            }
            if (!activity.InActivityTime() && !activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                string[] dbFields = null;
                // 向DB申请数据
                TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_SPR_QUERYJIERIDALIBAO, Global.GetActivityRequestCmdString(ActivityTypes.JieriDaLiBao, client), out dbFields, client.ServerId);
                if (null == dbFields)
                    break;

                // strcmd = string.Format("{0}:{1}:{2}", 1, roleID, hasgettimes);
                if (null == dbFields || 3 != dbFields.Length)
                    break;

                int hasgettimes = Convert.ToInt32(dbFields[2]);

                if (hasgettimes == 0)
                    bFlush = true;

                break;
            }

            return AddFlushIconState((ushort)ActivityTipTypes.JieRiLogin, bFlush);
        }

        /// <summary>
        /// 节日累计登陆
        /// </summary>
        public bool CheckJieRiTotalLogin(GameClient client)
        {
            JieRiDengLuActivity activity = HuodongCachingMgr.GetJieRiDengLuActivity();
            if (null == activity)
            {
                return false;
            }
            if (!activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                string[] dbFields = null;
                // 向DB申请数据
                TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_SPR_QUERYJIERIDENGLU, Global.GetActivityRequestCmdString(ActivityTypes.JieriDengLuHaoLi, client), out dbFields, client.ServerId);
                if (null == dbFields)
                    break;

                // hasgettimes |= (1 << (extTag - 1));
                // strcmd = string.Format("{0}:{1}:{2}:{3}", 1, roleID, hasgettimes, dengLuTimes);
                if (null == dbFields || 4 != dbFields.Length)
                    break;

                int hasgettimes = Convert.ToInt32(dbFields[2]);
                int dengLuTimes = Convert.ToInt32(dbFields[3]);

                // 依次检查是否有满足条件的没领取的奖励
                for (int i = 0; i < dengLuTimes; i++)
                {
                    if (activity.GetAward(client, i + 1) == null)
                        continue;

                    int nValue = Global.GetIntSomeBit(hasgettimes, i);
                    // 发现有一天没领取
                    if (nValue == 0)
                    {
                        bFlush = true;
                        break;
                    }
                }
                break;
            }

            return AddFlushIconState((ushort)ActivityTipTypes.JieRiTotalLogin, bFlush);
        }

        /// <summary>
        /// 节日每日充值
        /// </summary>
        public bool CheckJieRiDayCZ(GameClient client)
        {
            JieriCZSongActivity activity = HuodongCachingMgr.GetJieriCZSongActivity();
            if (null == activity)
            {
                return false;
            }
            if (!activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                string[] dbFields = null;
                // 向DB申请数据
                TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_SPR_QUERYJIERICZSONG, Global.GetActivityRequestCmdString(ActivityTypes.JieriCZSong, client), out dbFields, client.ServerId);
                if (null == dbFields)
                    break;

                // hasgettimes |= (1 << (extTag - 1));
                // strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", 1, roleID, minYuanBao, roleYuanBaoInPeriod, hasgettimes);
                if (null == dbFields || 5 != dbFields.Length)
                    break;

                int roleYuanBaoInPeriod = Convert.ToInt32(dbFields[3]);
                int hasgettimes = Convert.ToInt32(dbFields[4]);

                foreach (KeyValuePair<int, AwardItem> item in activity.AwardItemDict)
                {
                    // 满足领取条件
                    if (roleYuanBaoInPeriod >= item.Value.MinAwardCondionValue)
                    {
                        // 判断是否领取
                        int nValue = Global.GetIntSomeBit(hasgettimes, item.Key - 1);
                        // 发现有一天没领取
                        if (nValue == 0)
                        {
                            bFlush = true;
                            break;
                        }
                    }
                }
                break;
            }

            return AddFlushIconState((ushort)ActivityTipTypes.JieRiDayCZ, bFlush);
        }

        /// <summary>
        /// 节日累计消费
        /// <summary>
        public bool CheckJieRiLeiJiXF(GameClient client)
        {
            JieRiTotalConsumeActivity activity = HuodongCachingMgr.GetJieRiTotalConsumeActivity();
            if (null == activity)
            {
                return false;
            }
            if (!activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                string[] dbFields = null;
                // 向DB申请数据
                TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_SPR_QUERYJIERITOTALCONSUME, Global.GetActivityRequestCmdString(ActivityTypes.JieriTotalConsume, client), out dbFields, client.ServerId);
                if (null == dbFields)
                    break;

                // hasgettimes |= (1 << (extTag - 1));
                // strcmd = string.Format("{0}:{1}:{2}:{3}", 1, roleID, roleYuanBaoInPeriod, hasgettimes);
                if (null == dbFields || 4 != dbFields.Length)
                    break;

                int roleYuanBaoInPeriod = Convert.ToInt32(dbFields[2]);
                int hasgettimes = Convert.ToInt32(dbFields[3]);

                foreach (KeyValuePair<int, AwardItem> item in activity.AwardItemDict)
                {
                    // 满足领取条件
                    if (roleYuanBaoInPeriod >= item.Value.MinAwardCondionValue)
                    {
                        // 判断是否领取
                        int nValue = Global.GetIntSomeBit(hasgettimes, item.Key - 1);
                        // 发现有一天没领取
                        if (nValue == 0)
                        {
                            bFlush = true;
                            break;
                        }
                    }
                }

                break;
            }

            return AddFlushIconState((ushort)ActivityTipTypes.JieRiLeiJiXF, bFlush);
        }

        /// <summary>
        /// 节日累计充值 
        /// </summary>
        public bool CheckJieRiLeiJiCZ(GameClient client)
        {
            JieRiLeiJiCZActivity activity = HuodongCachingMgr.GetJieRiLeiJiCZActivity();
            if (null == activity)
            {
                return false;
            }
            if (!activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                string[] dbFields = null;
                // 向DB申请数据
                TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_SPR_QUERYJIERICZLEIJI, Global.GetActivityRequestCmdString(ActivityTypes.JieriLeiJiCZ, client), out dbFields, client.ServerId);
                if (null == dbFields)
                    break;

                // hasgettimes |= (1 << (extTag - 1));
                // strcmd = string.Format("{0}:{1}:{2}:{3}", 1, roleID, roleYuanBaoInPeriod, hasgettimes);
                if (null == dbFields || 4 != dbFields.Length)
                    break;

                int roleYuanBaoInPeriod = Convert.ToInt32(dbFields[2]);
                int hasgettimes = Convert.ToInt32(dbFields[3]);

                foreach (KeyValuePair<int, AwardItem> item in activity.AwardItemDict)
                {
                    // 满足领取条件
                    if (roleYuanBaoInPeriod >= item.Value.MinAwardCondionValue)
                    {
                        // 判断是否领取
                        int nValue = Global.GetIntSomeBit(hasgettimes, item.Key - 1);
                        // 发现有一天没领取
                        if (nValue == 0)
                        {
                            bFlush = true;
                            break;
                        }
                    }
                }
                break;
            }

            return AddFlushIconState((ushort)ActivityTipTypes.JieRiLeiJiCZ, bFlush);
        }

        /// <summary>
        /// 节日充值王
        /// </summary>
        public bool CheckJieRiCZKING(GameClient client)
        {
            KingActivity activity = HuodongCachingMgr.GetJieRiCZKingActivity();
            if (null == activity)
            {
                return false;
            }
            if (!activity.InAwardTime())
            {
                return false;
            }
            bool bFlush = false;
            while (true)
            {
                string[] dbFields = null;
                // 向DB申请数据
                TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_SPR_QUERYJIERICZKING, Global.GetActivityRequestCmdString(ActivityTypes.JieriPTCZKing, client, 1/*查询是否有奖励的标记*/), out dbFields, client.ServerId);

                // string strCmd = string.Format("{0}:{1}:{2}", 1, roleID, (paiHang > 0 && hasgettimes > 0) ? "1" : "0");
                if (null == dbFields || 3 != dbFields.Length)
                    break;

                int result = Convert.ToInt32(dbFields[0]);
                int roleid = Convert.ToInt32(dbFields[1]);
                int hasgettimes = Convert.ToInt32(dbFields[2]);

                if (1 != result)
                    break;

                if (roleid != client.ClientData.RoleID)
                    break;

                bFlush = (hasgettimes == 1);
                break;
            }
            return AddFlushIconState((ushort)ActivityTipTypes.JieRiCZKING, bFlush);
        }

        /// <summary>
        /// 节日消费王
        /// </summary>
        public bool CheckJieRiXFKING(GameClient client)
        {
            KingActivity activity = HuodongCachingMgr.GetJieriXiaoFeiKingActivity();
            if (null == activity)
            {
                return false;
            }
            if (!activity.InAwardTime())
            {
                return false;
            }

            bool bFlush = false;
            while (true)
            {
                //CMD_SPR_QUERYJIERIXIAOFEIKING
                string[] dbFields = null;
                // 向DB申请数据
                TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_SPR_QUERYJIERIXIAOFEIKING, Global.GetActivityRequestCmdString(ActivityTypes.JieriPTXiaoFeiKing, client, 1/*查询是否有奖励的标记*/), out dbFields, client.ServerId);
                // string strCmd = string.Format("{0}:{1}:{2}", 1, roleID, (paiHang > 0 && hasgettimes == 0) ? "1" : "0");
                if (null == dbFields || 3 != dbFields.Length)
                    break;

                int result = Convert.ToInt32(dbFields[0]);
                int roleid = Convert.ToInt32(dbFields[1]);
                int hasgettimes = Convert.ToInt32(dbFields[2]);

                if (1 != result)
                    break;

                if (roleid != client.ClientData.RoleID)
                    break;

                bFlush = (hasgettimes == 1);
                break;
            }
            return AddFlushIconState((ushort)ActivityTipTypes.JieRiXFKING, bFlush);
        }

        /// <summary>
        /// 节日赠送活动, 检测是否刷新图标
        /// </summary>
        public bool CheckJieriGive(GameClient client)
        {
            JieriGiveActivity act = HuodongCachingMgr.GetJieriGiveActivity();
            if (null == act || !act.InAwardTime()) return false;

            bool hasCanGetAward = act.CanGetAnyAward(client);
            return AddFlushIconState((ushort)ActivityTipTypes.JieRiGive, hasCanGetAward);
        }

        /// <summary>
        /// 节日收取活动, 检测是否刷新图标
        /// </summary>
        public bool CheckJieriRecv(GameClient client)
        {
            JieriRecvActivity act = HuodongCachingMgr.GetJieriRecvActivity();
            if (null == act || !act.InAwardTime()) return false;

            bool hasCanGetAward = act.CanGetAnyAward(client);
            return AddFlushIconState((ushort)ActivityTipTypes.JieRiRecv, hasCanGetAward);
        }

        /// <summary>
        /// 节日积分兑换
        /// </summary>
        public bool CheckJieriIPointsExchg(GameClient client)
        {
            JieriIPointsExchgActivity act = HuodongCachingMgr.GetJieriIPointsExchgActivity();
            if(null == act || !act.InAwardTime()) return false;

            bool hasCanGetAward = act.CanGetAnyAward(client);
            return AddFlushIconState((ushort)ActivityTipTypes.JieRiIPointsExchg, hasCanGetAward);
        }

        /// <summary>
        /// 节日赠送排行活动
        /// </summary>
        public bool CheckJieriGiveKing(GameClient client)
        {
            JieRiGiveKingActivity act = HuodongCachingMgr.GetJieriGiveKingActivity();
            if (act == null || !act.InAwardTime()) return false;

            bool hasCanGetAward = act.CanGetAnyAward(client);
            return AddFlushIconState((ushort)ActivityTipTypes.JieRiGiveKing, hasCanGetAward);
        }

        /// <summary>
        /// 节日收取排行活动
        /// </summary>
        public bool CheckJieriRecvKing(GameClient client)
        {
            JieRiRecvKingActivity act = HuodongCachingMgr.GetJieriRecvKingActivity();
            if (act == null || !act.InAwardTime()) return false;

            bool hasCanGetAward = act.CanGetAnyAward(client);
            return AddFlushIconState((ushort)ActivityTipTypes.JieRiRecvKing, hasCanGetAward);
        }

        /// <summary>
        /// 节日连续充值活动
        /// </summary>
        public bool CheckJieriLianXuCharge(GameClient client)
        {
            JieriLianXuChargeActivity act = HuodongCachingMgr.GetJieriLianXuChargeActivity();
            if (act == null || !act.InAwardTime()) return false;

            bool hasCanGetAward = act.CanGetAnyAward(client);
            return AddFlushIconState((ushort)ActivityTipTypes.JieRiLianXuCharge, hasCanGetAward);
        }

        public bool CheckGuildIcon(GameClient client, bool isLogin)
        {
            if (isLogin)
            {
                AddFlushIconState((ushort)ActivityTipTypes.GuildIcon, false);
                AddFlushIconState((ushort)ActivityTipTypes.GuildCopyMap, false);
            }

            bool bFlush = false;
            // 战盟副本
            bFlush = bFlush | CheckGuildCopyMap(client);

            return AddFlushIconState((ushort)ActivityTipTypes.GuildIcon, bFlush);
        }

        /// <summary>
        /// 战盟副本
        /// </summary>
        public bool CheckGuildCopyMap(GameClient client)
        {
            bool bFlush = false;
            int mapid = -1;
            int seqid = -1;
            int mapcode = -1;
            // 查找本周副本通关情况，mapid返回打到第几个副本了
            GameManager.GuildCopyMapMgr.CheckCurrGuildCopyMap(client, out mapid, out seqid, mapcode);
            if (mapid < 0)
            {
                return false;
            }

            int nGuildCopyMapAwardFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.GuildCopyMapAwardFlag);
            for (int i = 0; i < GameManager.GuildCopyMapMgr.GuildCopyMapOrderList.Count; i++)
            {
                int fubenID = GameManager.GuildCopyMapMgr.GuildCopyMapOrderList[i];
                // 如果没通关 就提示叹号
                if (mapid != 0)
                {
                    bFlush = true;
                    break;
                }
                // 不符合领取条件
                if (mapid > 0 && fubenID >= mapid)
                {
                    break;
                }
                bool flag = GameManager.GuildCopyMapMgr.GetGuildCopyMapAwardDayFlag(nGuildCopyMapAwardFlag, i, 2);
                if (flag == false)
                {
                    bFlush = true;
                    break;
                }
            }
            return AddFlushIconState((ushort)ActivityTipTypes.GuildCopyMap, bFlush);
        }

        public bool CheckPetIcon(GameClient client)
        {
            AddFlushIconState((ushort)ActivityTipTypes.PetBagIcon, false);
            AddFlushIconState((ushort)ActivityTipTypes.CallPetIcon, false);

            bool bFlush = false;
            bFlush = bFlush | CheckPetBagIcon(client);
            bFlush = bFlush | CheckCallPetIcon(client);
            return bFlush;
        }

        // 领地 免费队列空闲
        public bool CheckBuildingFreeQueue(GameClient client)
        {
            bool bFlush = false;
            BuildingManager BuildingMgr = BuildingManager.getInstance();
            
            int free = 0;
            int pay = 0;

            // 获取当前任务在个队列的状态
            BuildingMgr.GetTaskNumInEachTeam(client, out free, out pay);

            // 有空闲免费队列
            if (free < BuildingMgr.ManorFreeQueueNumMax)
            {
                bFlush = true;
            }

            return bFlush;
        }

        // 领地 未领取奖励
        public bool CheckBuildingAward(GameClient client)
        {
            bool bFlush = false;
            BuildingManager BuildingMgr = BuildingManager.getInstance();

            // 检查总等级奖励
            bFlush = bFlush | BuildingMgr.CheckCanGetAnyAllLevelAward(client);
            
            // 是否有已经完成的建造任务
            bFlush = bFlush | BuildingMgr.CheckAnyTaskFinish(client);

            return bFlush;
        }

        // 领地 免费队列空闲 or 未领取奖励
        public bool CheckBuildingIcon(GameClient client, bool isLogin)
        {
            if (isLogin)
            {
                AddFlushIconState((ushort)ActivityTipTypes.BuildingIcon, false);
            }
            bool bFlush = false;

            // 免费队列空闲
            bFlush = bFlush | CheckBuildingFreeQueue(client);

            // 未领取奖励
          //  bFlush = bFlush | CheckBuildingAward(client);

            return AddFlushIconState((ushort)ActivityTipTypes.BuildingIcon, bFlush);
        }

        public bool CheckTianTiMonthPaiMingAwards(GameClient client)
        {
            ushort iState = 0;
            DuanWeiRankAward duanWeiRankAward = null; 
            if (TianTiManager.getInstance().CanGetMonthRankAwards(client, out duanWeiRankAward))
            {
                iState = 1;
            }

            if (AddFlushIconState((ushort)ActivityTipTypes.TianTiMonthRankAwards, iState))
            {
                SendIconStateToClient(client);
                return true;
            }

            return false;
        }

        // 检查精灵ICON
        public bool CheckPetBagIcon(GameClient client)
        {
            /*bool bFlush = (null != client.ClientData.PetList) && (client.ClientData.PetList.Count > 0);

            return AddFlushIconState((ushort)ActivityTipTypes.PetBagIcon, bFlush);*/
            return false;
        }

        // 检查是否能够免费召唤精灵
        public bool CheckCallPetIcon(GameClient client)
        {
            bool bFlush = CallPetManager.getFreeSec(client) <= 0;
            return AddFlushIconState((ushort)ActivityTipTypes.CallPetIcon, bFlush);
        }

        public void DoSpriteIconTicks(GameClient client)
        {
            // 定时
            long startTicks = TimeUtil.NOW();

            // 领地
            if (startTicks >= m_LastTicksBuilding)
            {
                m_LastTicksBuilding = startTicks + 1000 * 5; // 5 秒
                if (client._IconStateMgr.CheckBuildingIcon(client, false))
                    client._IconStateMgr.SendIconStateToClient(client);
            }

            // 其它
            if (startTicks >= m_LastTicks)
            {
                //5秒以后再开始检测
                if (m_LastTicks == 0)
                {
                    m_LastTicks = startTicks + 1000 * 5; // 15 秒
                }
                else
                {
                    m_LastTicks = startTicks + 1000 * 20; // 20 秒
                    client._IconStateMgr.CheckPetIcon(client);
                    client._IconStateMgr.CheckTianTiMonthPaiMingAwards(client);
                    LangHunLingYuManager.getInstance().CheckTipsIconState(client);
                    ZhengBaManager.Instance().CheckTipsIconState(client);
                    CoupleArenaManager.Instance().CheckTipsIconState(client);
                    CoupleWishManager.Instance().CheckTipsIconState(client);
                }
            }
        }

        /// <summary>
        /// 节日返利活动
        /// </summary>
        public bool CheckJieRiFanLi(GameClient client, ActivityTypes nActType)
        {
            JieriFanLiActivity activity = HuodongCachingMgr.GetJieriFanLiActivity(nActType);
            if (null == activity)
            {
                return false;
            }
            if (!activity.InAwardTime())
            {
                return false;
            }

            //取出是不是已经领取过
            string[] dbFields = null;
            string sCmd = "";
            sCmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.ClientData.RoleID, activity.FromDate.Replace(':', '$'), activity.ToDate.Replace(':', '$'), (int)nActType, 0);

            TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    (int)TCPGameServerCmds.CMD_DB_EXECUXJIERIFANLI, sCmd, out dbFields, client.ServerId);
            if (null == dbFields || 2 != dbFields.Length)
                return false;

            int hasgettimes = Convert.ToInt32(dbFields[1]);

            //[bing] 可能需要检查条件是否满足
            bool bFlush = false;
            for (int i = 1; i <= 5; ++i)
            {
                int bitVal = Global.GetBitValue(i);
                if ((hasgettimes & bitVal) == bitVal)
                    continue;

                bFlush = activity.CheckCondition(client, i);
                if (true == bFlush)
                    break;
            }

            ushort usIconTypes = 0;
            switch (nActType)
            {
                case ActivityTypes.JieriWing:
                    usIconTypes = (ushort)ActivityTipTypes.JieriWing;
                    break;
                case ActivityTypes.JieriAddon:
                    usIconTypes = (ushort)ActivityTipTypes.JieriAddon;
                    break;
                case ActivityTypes.JieriStrengthen:
                    usIconTypes = (ushort)ActivityTipTypes.JieriStrengthen;
                    break;
                case ActivityTypes.JieriAchievement:
                    usIconTypes = (ushort)ActivityTipTypes.JieriAchievement;
                    break;
                case ActivityTypes.JieriMilitaryRank:
                    usIconTypes = (ushort)ActivityTipTypes.JieriMilitaryRank;
                    break;
                case ActivityTypes.JieriVIPFanli:
                    usIconTypes = (ushort)ActivityTipTypes.JieriVIPFanli;
                    break;
                case ActivityTypes.JieriAmulet:
                    usIconTypes = (ushort)ActivityTipTypes.JieriAmulet;
                    break;
                case ActivityTypes.JieriArchangel:
                    usIconTypes = (ushort)ActivityTipTypes.JieriArchangel;
                    break;
                case ActivityTypes.JieriMarriage:
                    usIconTypes = (ushort)ActivityTipTypes.JieriMarriage;
                    break;
            }

            return AddFlushIconState(usIconTypes, bFlush);
        }
    }
}
