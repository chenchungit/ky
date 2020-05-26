#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Server.Tools;
using GameServer.Server;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    // 每日活跃管理器 [2/24/2014 LiaoWei]
    public class DailyActiveManager
    {
        /// <summary>
        /// 每日活跃信息用map存储，key是成就ID，Value是活跃完成与否标志索引，Value+1是活跃奖励是否领取标志
        /// </summary>
        private static Dictionary<int, int> m_DailyActiveInfo = new Dictionary<int, int>();

        /// <summary>
        /// DayID
        /// </summary>
        public static int m_DailyActiveDayID = 0;

        /// <summary>
        /// 初始化标志位索引
        /// </summary>
        public static void InitDailyActiveFlagIndex()
        {
            m_DailyActiveInfo.Clear();

            //索引必须手动生成，每一个id对应的索引位置不能变
            int index = 0;

            //完成与否 和 是否领取奖励共需要两个标志位
            
            m_DailyActiveInfo.Add(DailyActiveTypes.LoginGameCount, index);
            index += 2;
            
            m_DailyActiveInfo.Add(DailyActiveTypes.SeriesLogin, index);
            index += 2;

            m_DailyActiveInfo.Add(DailyActiveTypes.MallBuyCount, index);
            index += 2;

            for (int n = DailyActiveTypes.CompleteDailyTaskCount1; n <= DailyActiveTypes.CompleteDailyTaskCount2; n++)
            {
                m_DailyActiveInfo.Add(n, index);
                index += 2;
            }

            for (int n = DailyActiveTypes.CompleteNormalCopyMapCount1; n <= DailyActiveTypes.CompleteNormalCopyMapCount1; n++)
            {
                m_DailyActiveInfo.Add(n, index);
                index += 2;
            }

            for (int n = DailyActiveTypes.CompleteHardCopyMapCount1; n <= DailyActiveTypes.CompleteHardCopyMapCount1; n++)
            {
                m_DailyActiveInfo.Add(n, index);
                index += 2;
            }

            for (int n = DailyActiveTypes.CompleteDifficltCopyMapCount1; n <= DailyActiveTypes.CompleteDifficltCopyMapCount1; n++)
            {
                m_DailyActiveInfo.Add(n, index);
                index += 2;
            }

            m_DailyActiveInfo.Add(DailyActiveTypes.CompleteBloodCastle, index);
            index += 2;

            m_DailyActiveInfo.Add(DailyActiveTypes.CompleteDaimonSquare, index);
            index += 2;

            m_DailyActiveInfo.Add(DailyActiveTypes.CompleteBattle, index);
            index += 2;

            m_DailyActiveInfo.Add(DailyActiveTypes.EquipForge, index);
            index += 2;

            m_DailyActiveInfo.Add(DailyActiveTypes.EquipAppend, index);
            index += 2;


            for (int n = DailyActiveTypes.KillMonster1; n <= DailyActiveTypes.KillMonster3; n++)
            {
                m_DailyActiveInfo.Add(n, index);
                index += 2;
            }

            m_DailyActiveInfo.Add(DailyActiveTypes.KillBoss, index);
            index += 2;

            m_DailyActiveInfo.Add(DailyActiveTypes.CompleteChangeLife, index);
            index += 2;

            m_DailyActiveInfo.Add(DailyActiveTypes.MergeFruit, index);
            index += 2;

            //m_DailyActiveDayID = (int)TimeUtil.NowDateTime().DayOfYear;
        }

        /// <summary>
        /// 初始化每日活跃相关数据
        /// </summary>
        public static void InitRoleDailyActiveData(GameClient client)
        {
            client.ClientData.DailyActiveValues         = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveValue);               // 活跃值
            client.ClientData.DailyActiveDayLginCount   = GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveDayLoginNum);              // 登陆
            client.ClientData.DailyTotalKillMonsterNum  = GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveTotalKilledMonsterNum);    // 杀怪
            client.ClientData.DailyTotalKillKillBossNum = GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveTotalKilledBossNum);       // 杀BOSS
            client.ClientData.DailyCompleteDailyTaskCount = GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteDailyTask);      // 日常任务完成
            client.ClientData.DailyActiveDayBuyItemInMall = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveBuyItemInMall);     // 商城购买
            //ProcessLoginForDailyActive(client);
        }

        /// <summary>
        /// 每日活跃相关数据存盘
        /// </summary>
        public static void SaveRoleDailyActiveData(GameClient client)
        {
            ModifyDailyActiveInfor(client, (uint)client.ClientData.DailyTotalKillMonsterNum, DailyActiveDataField1.DailyActiveTotalKilledMonsterNum, true);
        }

        /// <summary>
        /// 通过活跃索引位置返回活跃ID
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected static ushort GetDailyActiveIDByIndex(int index)
        {
            for (int n = 0; n < m_DailyActiveInfo.Count; n++)
            {
                if (m_DailyActiveInfo.ElementAt(n).Value == index)
                    return (ushort)m_DailyActiveInfo.ElementAt(n).Key;
            }

            return 0;
        }

        /// <summary>
        /// 根据成就活跃ID返回是否完成索引 失败返回-1
        /// </summary>
        /// <returns></returns>
        protected static int GetCompletedFlagIndex(int DailyActiveID)
        {
            int index = -1;

            if (m_DailyActiveInfo.TryGetValue(DailyActiveID, out index))
                return index;
            
            return -1;
        }

        /// <summary>
        /// 根据成就id返回是否领取索引 失败返回-1
        /// </summary>
        /// <returns></returns>
        protected static int GetAwardFlagIndex(int DailyActiveID)
        {
            int index = -1;

            if (m_DailyActiveInfo.TryGetValue(DailyActiveID, out index))
                return index + 1;

            return -1;
        }

        /// <summary>
        /// 修改活跃点数的值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="modifyValue"></param>
        public static void AddDailyActivePoints(GameClient client, int DailyActiveID, SystemXmlItem itemDailyActive, bool writeToDB = false)
        {
            int awardDailyActiveValue = 0;

            //SystemXmlItem itemDailyActive = null;
            //if (GameManager.systemDailyActiveInfo.SystemXmlItemDict.TryGetValue(DailyActiveID, out itemDailyActive))
                awardDailyActiveValue = Math.Max(0, itemDailyActive.GetIntValue("Award"));

            // VIP处理 [3/29/2014 LiaoWei]
            int nVipLev = client.ClientData.VipLevel;
            if (nVipLev > 0 && nVipLev <= (int)VIPEumValue.VIPENUMVALUE_MAXLEVEL)
            {
                int[] nAddNum = null;
                nAddNum = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPHuoYueAdd");

                if (nAddNum != null && nAddNum.Length > 0 && nAddNum.Length == 13)
                {
                    awardDailyActiveValue += nAddNum[nVipLev];
                }
            }

            if (0 == awardDailyActiveValue)
                return;

            client.ClientData.DailyActiveValues += awardDailyActiveValue;

            //本次在线期间的
            client.ClientData.OnlineActiveVal += awardDailyActiveValue;

            //更新到数据库
            ModifyDailyActiveInfor(client, (uint)client.ClientData.DailyActiveValues, DailyActiveDataField1.DailyActiveValue, writeToDB);

            if (writeToDB)
            {
                //通知自己
                //GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.DailyActive, client.ClientData.DailyActiveValues);
            }
        }

        /// <summary>
        /// 获得每日活跃数据
        /// </summary>
        /// <returns></returns>
        public static uint GetDailyActiveDataByField(GameClient client, DailyActiveDataField1 field)
        {
            List<uint> lsUint = Global.GetRoleParamsUIntListFromDB(client, RoleParamName.DailyActiveInfo1);

            int index = (int)field;

            if (index >= lsUint.Count)
                return 0;
            
            return lsUint[index];
        }

        /// <summary>
        /// 修改每日活跃数据
        /// </summary>
        /// <returns></returns>
        public static void ModifyDailyActiveInfor(GameClient client, UInt32 value, DailyActiveDataField1 field, bool writeToDB = false)
        {
            List<uint> lsUint = Global.GetRoleParamsUIntListFromDB(client, RoleParamName.DailyActiveInfo1);

            int index = (int)field;

            while (lsUint.Count < (index + 1))
                lsUint.Add(0);

            lsUint[index] = value;

            Global.SaveRoleParamsUintListToDB(client, lsUint, RoleParamName.DailyActiveInfo1, writeToDB);
        }

        /// <summary>
        /// 获取每日活跃点
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetDailyActiveValue(GameClient client)
        {
            client.ClientData.DailyActiveValues = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveValue);

            return client.ClientData.DailyActiveValues;
        }

        /// <summary>
        /// 通过成就ID提取成就存储位置，index 表示竖线分开的第几项，subIndex 表示某一项中的某一个标志位
        /// 成就id规则，采用成就类型乘以100加上一个子序号
        /// </summary>
        /// <returns></returns>
        public static bool IsDailyActiveCompleted(GameClient client, int DailyActiveID)
        {
            return IsFlagIsTrue(client, DailyActiveID);
        }

        /// <summary>
        /// 判断成就奖励是否被领取
        /// </summary>
        /// <returns></returns>
        public static int IsDailyActiveAwardFetched(GameClient client, int nID)
        {
            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveAwardFlag);

            int n = nFlag & Global.GetBitValue(nID+1);

            return n;
        }

        /// <summary>
        /// 每日活跃完成提示
        /// </summary>
        /// <param name="client"></param>
        public static void OnDailyActiveCompleted(GameClient client, int DailyActiveID)
        {
            // 设置完成标志
            UpdateDailyActiveFlag(client, DailyActiveID);

            // 增加活动值
            //AddDailyActivePoints(client, DailyActiveID, true);

            // 刚刚完成的成就
            NotifyClientDailyActiveData(client, DailyActiveID);

            // 刷新“每日活跃”图标感叹号状态
            if (client._IconStateMgr.CheckFuLiMeiRiHuoYue(client))
            {
                client._IconStateMgr.SendIconStateToClient(client);
            }
        }

        /// <summary>
        /// 通知客户端活跃数据
        /// </summary>
        /// <param name="client"></param>
        public static void NotifyClientDailyActiveData(GameClient client, int justCompleteddailyactive = -1, bool bRefresh = false)
        {
            // 通知客户端每日活跃数据
            //client.ClientData.DayOnlineSecond = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DayOnlineSecond);

            int nKillBoss = 0;
            if (client.ClientData.MyRoleDailyData != null && bRefresh == false)
            {
                nKillBoss = client.ClientData.MyRoleDailyData.TodayKillBoss;
            }

            DailyActiveData dailyactiveData = new DailyActiveData()
            {
                RoleID = client.ClientData.RoleID,                                      // 角色ID
                DailyActiveValues = client.ClientData.DailyActiveValues,                // 每日活跃值
                TotalKilledMonsterCount = client.ClientData.DailyTotalKillMonsterNum,   // 每日杀怪数 (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveTotalKilledMonsterNum),
                DailyActiveTotalLoginCount = client.ClientData.DailyActiveDayLginCount, // 每日登陆计数
                DailyActiveOnLineTimer = client.ClientData.DayOnlineSecond,             // 每日在线时间
                DailyActiveInforFlags = GetDailyActiveInfoArray(client),                // 16个bit 一组，前14个bit表示id， 后面一次是完成bit 和 奖励bit
                NowCompletedDailyActiveID = justCompleteddailyactive,                   // 刚完成的活跃ID
                TotalKilledBossCount = (int)client.ClientData.DailyTotalKillKillBossNum,// 每日杀BOSS计数 (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveTotalKilledBossNum),
                PassNormalCopySceneNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteCopyMap1),
                PassHardCopySceneNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteCopyMap2),
                PassDifficultCopySceneNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteCopyMap3),
                BuyItemInMall = client.ClientData.DailyActiveDayBuyItemInMall,          // 每日商城消费
                CompleteDailyTaskCount = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteDailyTask),
                CompleteBloodCastleCount = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteBloodCastle),
                CompleteDaimonSquareCount = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteDaimonSquare),
                CompleteBattleCount = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteBattle),
                EquipForge = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveEquipForge),
                EquipAppend = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveEquipAppend),
                ChangeLife = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveChangeLife),
                MergeFruit = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveMergeFruit),
                GetAwardFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveAwardFlag),
            };

            byte[] bytesData = DataHelper.ObjectToBytes<DailyActiveData>(dailyactiveData);

            //通知客户端
            GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_SPR_DAILYACTIVEDATA);
        }

        /// <summary>
        /// 返回每日活跃信息无符号整数数组，用于传递给客户端，每个活跃 14位的活跃id， 一位完成标志，1位领取标准
        /// </summary>
        /// <returns></returns>
        protected static List<ushort> GetDailyActiveInfoArray(GameClient client)
        {
            //这儿不采用循环判断IsFlagIsTrue, 使用对成就列表进行循环处理，那样运算少

            List<ulong> lsLong = Global.GetRoleParamsUlongListFromDB(client, RoleParamName.DailyActiveFlag);

            //索引位置
            int curIndex = 0;

            List<ushort> lsUshort = new List<ushort>();

            for (int n = 0; n < lsLong.Count; n++)
            {
                ulong uValue = lsLong[n];

                for (int subIndex = 0; subIndex < 64; subIndex += 2)
                {
                    //采用 11 移动
                    ulong flag = (ulong)(((ulong)0x3) << (subIndex));//完成与否 领取与否

                    //得到2bit标志位
                    ushort realFlag = (ushort)((uValue & flag) >> (subIndex));//提取到两个标志位表示的数 到最右边，得到一个数

                    ushort dailyactiveID = GetDailyActiveIDByIndex(curIndex);

                    //14bit 的dailyactiveID
                    ushort preFix = (ushort)(dailyactiveID << 2);

                    //14bit成就ID + 2bit标志位
                    ushort dailyactive = (ushort)(preFix | realFlag);

                    lsUshort.Add(dailyactive);

                    curIndex += 2;//注 索引也是2递增的，因为标志位是两个一组，一个是否完成，一个是否领取
                }
            }

            return lsUshort;
        }

        /// <summary>
        /// 给予完成每日活跃的奖励 进行完成与否 与是否已经领取的判断
        /// </summary>
        /// <param name="client"></param>
        public static int GiveDailyActiveAward(GameClient client, int nid)
        {
            // 未完成成就不能领取
            //if (!IsDailyActiveCompleted(client, DailyActiveID))
            //    return -1;
            int awardDailyActiveValue = 0;

            SystemXmlItem itemDailyActive = null;
            if (GameManager.systemDailyActiveAward.SystemXmlItemDict.TryGetValue(nid, out itemDailyActive))
                awardDailyActiveValue = Math.Max(0, itemDailyActive.GetIntValue("NeedhuoYue"));

            if (awardDailyActiveValue > client.ClientData.DailyActiveValues)
                return -3;

            // 奖励领取过了不能再领
            if (IsDailyActiveAwardFetched(client, nid) > 0)
                return -2;

            // 经与策划确定，此处不需要减的。
            // client.ClientData.DailyActiveValues -= awardDailyActiveValue;

            ModifyDailyActiveInfor(client, (uint)client.ClientData.DailyActiveValues, DailyActiveDataField1.DailyActiveValue, true);

            List<GoodsData> goodsDataList = new List<GoodsData>();

            string strGoods = itemDailyActive.GetStringValue("GoodsID");
            if (!string.IsNullOrEmpty(strGoods))
            {
                string[] fields = strGoods.Split('|');
                if (null != fields)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        string strID = fields[i];

                        string[] strinfro = null;
                        strinfro = fields[i].Split(',');

                        if (strinfro != null && strinfro.Length == 7)
                        {
                            GoodsData good = new GoodsData()
                            {
                                Id = -1,
                                GoodsID = Convert.ToInt32(strinfro[0]),
                                Using = 0,
                                Forge_level = Convert.ToInt32(strinfro[3]),
                                Starttime = "1900-01-01 12:00:00",
                                Endtime = Global.ConstGoodsEndTime,
                                Site = 0,
                                Quality = 0,
                                Props = "",
                                GCount = Convert.ToInt32(strinfro[1]),
                                Binding = Convert.ToInt32(strinfro[2]),
                                Jewellist = "",
                                BagIndex = 0,
                                AddPropIndex = 0,
                                BornIndex = 0,
                                Lucky = Convert.ToInt32(strinfro[5]),
                                Strong = 0,
                                ExcellenceInfo = Convert.ToInt32(strinfro[6]),
                                AppendPropLev = Convert.ToInt32(strinfro[4]),
                                ChangeLifeLevForEquip = 0,
                            };
                            goodsDataList.Add(good);
                        }
                    }

                    // 如果背包格子不够 就发邮件-附件带物品给玩家
                    if (!Global.CanAddGoodsNum(client, goodsDataList.Count))
                    {
                        foreach (var item in goodsDataList)
                            Global.UseMailGivePlayerAward(client, item, Global.GetLang("每日活跃领取奖励"), Global.GetLang("每日活跃领取奖励"));

                    }
                    else
                    {
                        foreach (var item in goodsDataList)
                        {
                            GoodsData goodsData = new GoodsData()
                            {
                                Id = -1,
                                GoodsID = item.GoodsID,
                                Using = 0,
                                Forge_level = item.Forge_level,
                                Starttime = "1900-01-01 12:00:00",
                                Endtime = Global.ConstGoodsEndTime,
                                Site = 0,
                                Quality = item.Quality,
                                Props = item.Props,
                                GCount = item.GCount,
                                Binding = item.Binding,
                                Jewellist = item.Jewellist,
                                BagIndex = 0,
                                AddPropIndex = item.AddPropIndex,
                                BornIndex = item.BornIndex,
                                Lucky = item.Lucky,
                                Strong = item.Strong,
                                ExcellenceInfo = item.ExcellenceInfo,
                                AppendPropLev = item.AppendPropLev,
                                ChangeLifeLevForEquip = item.ChangeLifeLevForEquip,
                            };

                            //向DBServer请求加入某个新的物品到背包中
                            //添加物品
                            goodsData.Id = Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, "副本通关获取物品", false,
                                                                            goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, true); // 卓越信息 [12/13/2013 LiaoWei]

                        }

                    }
                }
            }

            // 设置领取标志位
            //UpdateDailyActiveFlag(client, DailyActiveID, true);
            UpdateDailyActiveAwardFlag(client, nid);
            
            return 1;
        }

        /// <summary>
        /// 判断DailyActiveID对应标志位是否是true ，forAward=false 每日活跃是否完成 和 forAward = true 每日活跃奖励是否领取
        /// </summary>
        /// <returns></returns>
        public static Boolean IsFlagIsTrue(GameClient client, int DailyActiveID, Boolean forAward = false)
        {
            //完成标志索引
            int index = GetCompletedFlagIndex(DailyActiveID);
            if (index < 0)
                return false;

            if (forAward)
                index++;

            List<ulong> lsLong = Global.GetRoleParamsUlongListFromDB(client, RoleParamName.DailyActiveFlag);

            if (lsLong.Count <= 0)
                return false;

            //根据 index 定位到相应的整数
            int arrPosIndex = index / 64;

            if (arrPosIndex >= lsLong.Count)
                return false;

            //定位到整数内部的某个具体位置
            int subIndex = index % 64;

            UInt64 destLong = lsLong[arrPosIndex];

            //这个flag值比较特殊，这样写意味着 在 8 字节的 64位中处于从最小值开始，根据subIndex增加而增加
            //从64位存储的角度看，设计到大端序列和小端序列，看起来在不同的机器样子不一样
            ulong flag = ((ulong)(1)) << subIndex;

            //进行标志位判断
            bool bResult = (destLong & flag) > 0;

            return bResult;
        }

        /// <summary>
        /// 更新DailyActiveID对应的活跃项的标准位，并返回修改后的十六进制字符串，用于活跃是否完成和活跃奖励是否领取的统一处理
        /// </summary>
        /// <returns></returns>
        public static bool UpdateDailyActiveFlag(GameClient client, int DailyActiveID /* bool forAward = false*/)
        {
            //DailyActiveString 长度必须是 8 的倍数，一个长整形是 8 字节

            //完成标志索引
            int index = GetCompletedFlagIndex(DailyActiveID);
            if (index < 0)
                return false;

            //if (forAward)
            //    index++;

            List<ulong> lsLong = Global.GetRoleParamsUlongListFromDB(client, RoleParamName.DailyActiveFlag);

            //根据 index 定位到相应的整数
            int arrPosIndex = index / 64;

            //填充64位整数
            while (arrPosIndex > lsLong.Count - 1)
            {
                lsLong.Add(0);
            }

            //定位到整数内部的某个具体位置
            int subIndex = index % 64;

            ulong destLong = lsLong[arrPosIndex];

            //这个flag值比较特殊，这样写意味着 在 8 字节的 64位中处于从最小值开始，根据subIndex增加而增加
            //从64位存储的角度看，设计到大端序列和小端序列，看起来在不同的机器样子不一样
            ulong flag = ((ulong)(1)) << subIndex;

            //设置标志位 为 1
            lsLong[arrPosIndex] = destLong | flag;

            //存储到数据库
            Global.SaveRoleParamsUlongListToDB(client, lsLong, RoleParamName.DailyActiveFlag, true);

            return true;
        }

        /// <summary>
        /// 更新DailyActiveID对应的活跃项的标准位，并返回修改后的十六进制字符串，用于活跃是否完成和活跃奖励是否领取的统一处理
        /// </summary>
        /// <returns></returns>
        public static void UpdateDailyActiveAwardFlag(GameClient client, int nID)
        {
            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveAwardFlag);

            int n = Global.SetIntSomeBit(nID, nFlag, true);

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DailyActiveAwardFlag, n, true);
        }

        /// <summary>
        /// 杀怪时 处理每日活跃中杀怪项
        /// </summary>
        /// <param name="killer"></param>
        /// <param name="victim"></param>
        public static void ProcessDailyActiveKillMonster(GameClient killer, Monster victim)
        {
            if (!CheckLevCondition(killer, DailyActiveTypes.KillMonster1) && !CheckLevCondition(killer, DailyActiveTypes.KillMonster2) && !CheckLevCondition(killer, DailyActiveTypes.KillMonster3))
            {
                return;
            }

            // 保证是每天杀怪增加计数 1.在每天0点清空 2.登陆时判断
            killer.ClientData.DailyTotalKillMonsterNum++;
            killer.ClientData.TimerKilledMonsterNum++;

            // 怪物击杀量增加21个 减轻DB压力
            if (killer.ClientData.TimerKilledMonsterNum > 20)
            {
                killer.ClientData.TimerKilledMonsterNum = 0;
                ModifyDailyActiveInfor(killer, (uint)killer.ClientData.DailyTotalKillMonsterNum, DailyActiveDataField1.DailyActiveTotalKilledMonsterNum);
            }

            // 每日怪物击杀活跃
            CheckDailyActiveKillMonster(killer);

            //如果是BOSS，判断击杀boss活跃
            if ((int)MonsterTypes.BOSS == victim.MonsterType)
            {
                // 必须击杀指定的BOSS
                for (int i = 0; i < Data.KillBossCountForChengJiu.Length; ++i)
                {
#if ___CC___FUCK___YOU___BB___
                    if (victim.XMonsterInfo.MonsterId == Data.KillBossCountForChengJiu[i])
                    {
                        CheckDailyActiveKillBoss(killer);
                    }
#else
                    if (victim.MonsterInfo.ExtensionID == Data.KillBossCountForChengJiu[i])
                    {
                        CheckDailyActiveKillBoss(killer);
                    }
#endif
                }
            }
        }

        /// <summary>
        /// 检查击杀怪物活跃 这儿默认怪物成就对怪物数量要求由少到多
        /// </summary>
        public static void CheckDailyActiveKillMonster(GameClient client)
        {
            // 经过这样过滤判断，怪物杀死数量活跃的判断很多时候在这儿直接返回
            if (client.ClientData.DailyTotalKillMonsterNum < client.ClientData.DailyNextKillMonsterNum || 0x7fffffff == client.ClientData.DailyNextKillMonsterNum)
                return;

            bool bIsCompleted = false;

            uint nextNeedNum = CheckSingleConditionForDailyActive(client, DailyActiveTypes.KillMonster1, DailyActiveTypes.KillMonster3, client.ClientData.DailyTotalKillMonsterNum,
                                                                    "KillMonster", out bIsCompleted);

            // 有活跃完成 记录下下次需要检查的数量 这样不需要每次都调用 CheckSingleConditionForDailyActive
            client.ClientData.DailyNextKillMonsterNum = nextNeedNum;

            // 如果最后一个杀怪成就完成了，以后就不需要判断了
            if (IsDailyActiveCompleted(client, DailyActiveTypes.KillMonster3))
                client.ClientData.DailyNextKillMonsterNum = 0x7fffffff;
        }

        /// <summary>
        /// 检查Boss击杀活跃,这儿默认Boss成就对怪物数量要求由少到多
        /// </summary>
        public static void CheckDailyActiveKillBoss(GameClient client)
        {
            if (IsDailyActiveCompleted(client, DailyActiveTypes.KillBoss))
                return;
            
            if (!CheckLevCondition(client, DailyActiveTypes.KillBoss))
                return;

            bool bIsCompleted = false;

            ++client.ClientData.DailyTotalKillKillBossNum;

            ModifyDailyActiveInfor(client, (uint)client.ClientData.DailyTotalKillKillBossNum, DailyActiveDataField1.DailyActiveTotalKilledBossNum, true);
            

            CheckSingleConditionForDailyActive(client, DailyActiveTypes.KillBoss, DailyActiveTypes.KillBoss, client.ClientData.MyRoleDailyData.TodayKillBoss, "KillBoss", out bIsCompleted);

        }

        /// 单一条件成就检查，每日活跃的完成条件只有一个，而且，此类别成就对数量要求由少到多，比如boss击杀数量，怪物击杀数量
        /// 玩家金币数量等 返回下一个需要完成目标的数值
        /// </summary>
        protected static uint CheckSingleConditionForDailyActive(GameClient client, int DailyActiveMinID, int DailyActiveMaxID, long roleCurrentValue, String strCheckField, out bool bIsCompleted)
        {
            bIsCompleted = false;

            SystemXmlItem itemDailyActive = null;

            //完成成就需要的最少目标值
            uint needMinValue = 0;

            for (int DailyActiveID = DailyActiveMinID; DailyActiveID <= DailyActiveMaxID; DailyActiveID++)
            {
                if (!CheckLevCondition(client, DailyActiveID))
                    continue;
                
                if (GameManager.systemDailyActiveInfo.SystemXmlItemDict.TryGetValue(DailyActiveID, out itemDailyActive))
                {
                    if (null == itemDailyActive) 
                        continue;

                    needMinValue = (uint)itemDailyActive.GetIntValue(strCheckField);

                    if (roleCurrentValue >= needMinValue)
                    {
                        //如果没完成
                        if (!IsDailyActiveCompleted(client, DailyActiveID))
                        {
                            // 增加活跃值
                            AddDailyActivePoints(client, DailyActiveID, itemDailyActive, false);
                            //client.ClientData.DailyActiveValues += itemDailyActive.GetIntValue("Award");
                            //ModifyDailyActiveInfor(client, (uint)client.ClientData.DailyActiveValues, DailyActiveDataField1.DailyActiveValue, true);

                            //触发成就完成事件
                            OnDailyActiveCompleted(client, DailyActiveID);

                            bIsCompleted = true;
                        }
                    }
                    else
                    {
                        break;  //连最少的都没完成，直接退出
                    }
                }
            }

            return needMinValue;
        }

        /// <summary>
        /// 处理每日在线
        /// </summary>
        /// <returns></returns>
        public static void ProcessOnlineForDailyActive(GameClient client)
        {
            bool bIsCompleted = false;

            if (IsDailyActiveCompleted(client, DailyActiveTypes.SeriesLogin))
                return;

            if (client.ClientData.DayOnlineSecond - client.ClientData.DailyOnlineTimeTmp <= 0)
                return;
            
            client.ClientData.DailyOnlineTimeTmp += 60;

            if (!CheckLevCondition(client, DailyActiveTypes.SeriesLogin))
            {
                bIsCompleted = false;
                return;
            }

            CheckSingleConditionForDailyActive(client, DailyActiveTypes.SeriesLogin, DailyActiveTypes.SeriesLogin, client.ClientData.DayOnlineSecond / 60, "Online", out bIsCompleted);
        }

        /// <summary>
        /// 处理每日登陆
        /// </summary>
        /// <returns></returns>
        public static void ProcessLoginForDailyActive(GameClient client, out bool bIsCompleted)
        {
            bIsCompleted = false;

            if (IsDailyActiveCompleted(client, DailyActiveTypes.LoginGameCount))
                return;

            if (!CheckLevCondition(client, DailyActiveTypes.LoginGameCount))
                return;
            
            //int nToday = (int)TimeUtil.NowDateTime().DayOfYear;
            //int nDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveDayID);

            //if (nDay == nToday)
            //{
            //    ++client.ClientData.DailyActiveDayLginCount;
            //}
            //else
            //{
            //    client.ClientData.DailyActiveDayLginCount = 1;
            //    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DailyActiveDayID, nToday, true);
            //}

            ++client.ClientData.DailyActiveDayLginCount;

            uint nvalue = GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveDayLoginNum);

            ModifyDailyActiveInfor(client, (uint)client.ClientData.DailyActiveDayLginCount, DailyActiveDataField1.DailyActiveDayLoginNum, true);

            nvalue = GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveDayLoginNum);

            CheckSingleConditionForDailyActive(client, DailyActiveTypes.LoginGameCount, DailyActiveTypes.LoginGameCount, client.ClientData.DailyActiveDayLginCount, "Login", out bIsCompleted);

            client.ClientData.DailyActiveDayLginSetFlag = true;
        }

        /// <summary>
        /// 处理每日商城消费
        /// </summary>
        /// <returns></returns>
        public static void ProcessBuyItemInMallForDailyActive(GameClient client, int nValue)
        {
            if (IsDailyActiveCompleted(client, DailyActiveTypes.MallBuyCount))
                return;

            if (!CheckLevCondition(client, DailyActiveTypes.MallBuyCount))
            {
                return;
            }

            //int nToday = (int)TimeUtil.NowDateTime().DayOfYear;
            //int nDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveDayID);
            uint nSpend = GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveBuyItemInMall);

            //if (nDay == nToday)
            //{
                client.ClientData.DailyActiveDayBuyItemInMall += (int)nSpend + nValue;
            //}
            //else
            //{
            //    client.ClientData.DailyActiveDayBuyItemInMall = nValue;
            //}

            ModifyDailyActiveInfor(client, (uint)client.ClientData.DailyActiveDayBuyItemInMall, DailyActiveDataField1.DailyActiveBuyItemInMall, true);

            bool bIsCompleted = false;

            CheckSingleConditionForDailyActive(client, DailyActiveTypes.MallBuyCount, DailyActiveTypes.MallBuyCount, client.ClientData.DailyActiveDayBuyItemInMall, "Consumption", out bIsCompleted);
        }

        /// <summary>
        /// 处理每日完成日常任务活跃
        /// </summary>
        /// <returns></returns>
        public static void ProcessCompleteDailyTaskForDailyActive(GameClient client, int nValue)
        {
            if (IsDailyActiveCompleted(client, DailyActiveTypes.CompleteDailyTaskCount1) && IsDailyActiveCompleted(client, DailyActiveTypes.CompleteDailyTaskCount2))
                return;

            if (!CheckLevCondition(client, DailyActiveTypes.CompleteDailyTaskCount1) && !CheckLevCondition(client, DailyActiveTypes.CompleteDailyTaskCount2))
            {
                return;
            }

            //int nToday = (int)TimeUtil.NowDateTime().DayOfYear;
            //int nDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveDayID);
            //uint nNum = GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteDailyTask);

            //if (nDay == nToday)
            //{
            client.ClientData.DailyCompleteDailyTaskCount = (uint)nValue;
            //}
            //else
            //{
            //    client.ClientData.DailyCompleteDailyTaskCount = (uint)nValue;
            //}

            ModifyDailyActiveInfor(client, client.ClientData.DailyCompleteDailyTaskCount, DailyActiveDataField1.DailyActiveCompleteDailyTask, true);

            bool bIsCompleted = false;

            CheckSingleConditionForDailyActive(client, DailyActiveTypes.CompleteDailyTaskCount1, DailyActiveTypes.CompleteDailyTaskCount2, client.ClientData.DailyCompleteDailyTaskCount, "RiChang", out bIsCompleted);
        }

        /// <summary>
        /// 处理每日完成副本的活跃
        /// </summary>
        /// <returns></returns>
        public static void ProcessCompleteCopyMapForDailyActive(GameClient client, int nCopyMapLev ,int count = 1)
        {
            if (IsDailyActiveCompleted(client, DailyActiveTypes.CompleteNormalCopyMapCount1) && IsDailyActiveCompleted(client, DailyActiveTypes.CompleteHardCopyMapCount1)
                && IsDailyActiveCompleted(client, DailyActiveTypes.CompleteDifficltCopyMapCount1))
                return;

            if (nCopyMapLev < 0)
                return;

            //int nToday = (int)TimeUtil.NowDateTime().DayOfYear;
            //int nDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveDayID);
            //bool nIsSameDay = false;

            //if (nDay == nToday)
            //    nIsSameDay = true;

            bool bIsCompleted = false;

            int nNum = 0;

            switch (nCopyMapLev)
            {
                case (int)COPYMAPLEVEL.COPYMAPLEVEL_NORMAL:
                {
                    if (!CheckLevCondition(client, DailyActiveTypes.CompleteNormalCopyMapCount1))
                    {
                        return;
                    }

                    nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteCopyMap1);

                    //if (nIsSameDay)
                        ++nNum;
                    //else
                    //    nNum = 1;
                        nNum *= count;
                    ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveCompleteCopyMap1, true);

                    CheckSingleConditionForDailyActive(client, DailyActiveTypes.CompleteNormalCopyMapCount1, DailyActiveTypes.CompleteNormalCopyMapCount1, nNum, "KillRaid", out bIsCompleted);
                }
                break;
                case (int)COPYMAPLEVEL.COPYMAPLEVEL_HARD:
                {
                    if (!CheckLevCondition(client, DailyActiveTypes.CompleteHardCopyMapCount1))
                    {
                        return;
                    }

                    nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteCopyMap2);

                    //if (nIsSameDay)
                        ++nNum;
                    //else
                    //    nNum = 1;
                        nNum *= count;
                    ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveCompleteCopyMap2, true);

                    CheckSingleConditionForDailyActive(client, DailyActiveTypes.CompleteHardCopyMapCount1, DailyActiveTypes.CompleteHardCopyMapCount1, nNum, "KillRaid", out bIsCompleted);
                }
                break;
                case (int)COPYMAPLEVEL.COPYMAPLEVEL_DIFFICLT:
                {
                    if (!CheckLevCondition(client, DailyActiveTypes.CompleteDifficltCopyMapCount1))
                    {
                        return;
                    }

                    nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteCopyMap3);

                    //if (nIsSameDay)
                        ++nNum;
                    //else
                    //    nNum = 1;
                        nNum *= count;
                    ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveCompleteCopyMap3, true);

                    CheckSingleConditionForDailyActive(client, DailyActiveTypes.CompleteDifficltCopyMapCount1, DailyActiveTypes.CompleteDifficltCopyMapCount1, nNum, "KillRaid", out bIsCompleted);
                }
                break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 处理每日完成活动活跃
        /// </summary>
        /// <returns></returns>
        public static void ProcessCompleteDailyActivityForDailyActive(GameClient client, int nType)
        {
            if (IsDailyActiveCompleted(client, DailyActiveTypes.CompleteBloodCastle) && IsDailyActiveCompleted(client, DailyActiveTypes.CompleteDaimonSquare)
                && IsDailyActiveCompleted(client, DailyActiveTypes.CompleteBattle))
                return;

            if (nType < 0)
                return;

            //int nToday = (int)TimeUtil.NowDateTime().DayOfYear;
            //int nDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveDayID);
            //bool nIsSameDay = false;

            //if (nDay == nToday)
            //    nIsSameDay = true;

            bool bIsCompleted = false;

            int nNum = 0;

            switch (nType)
            {
                case (int)SpecialActivityTypes.BloodCastle:
                    {
                        if (!CheckLevCondition(client, DailyActiveTypes.CompleteBloodCastle))
                        {
                            return;
                        }

                        nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteBloodCastle);

                        //if (nIsSameDay)
                            ++nNum;
                        //else
                        //    nNum = 1;

                        ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveCompleteBloodCastle, true);

                        CheckSingleConditionForDailyActive(client, DailyActiveTypes.CompleteBloodCastle, DailyActiveTypes.CompleteBloodCastle, nNum, "HuoDongLimit", out bIsCompleted);
                    }
                    break;
                case (int)SpecialActivityTypes.DemoSque:
                    {
                        if (!CheckLevCondition(client, DailyActiveTypes.CompleteDaimonSquare))
                        {
                            return;
                        }

                        nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteDaimonSquare);

                        //if (nIsSameDay)
                            ++nNum;
                        //else
                        //    nNum = 1;

                        ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveCompleteDaimonSquare, true);

                        CheckSingleConditionForDailyActive(client, DailyActiveTypes.CompleteDaimonSquare, DailyActiveTypes.CompleteDaimonSquare, nNum, "HuoDongLimit", out bIsCompleted);
                    }
                    break;
                case (int)SpecialActivityTypes.CampBattle:
                    {
                        if (!CheckLevCondition(client, DailyActiveTypes.CompleteBattle))
                        {
                            return;
                        }

                        nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveCompleteBattle);

                        //if (nIsSameDay)
                            ++nNum;
                        //else
                        //    nNum = 1;

                        ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveCompleteBattle, true);

                        CheckSingleConditionForDailyActive(client, DailyActiveTypes.CompleteBattle, DailyActiveTypes.CompleteBattle, nNum, "HuoDongLimit", out bIsCompleted);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 处理每日装备强化
        /// </summary>
        /// <returns></returns>
        public static void ProcessDailyActiveEquipForge(GameClient client)
        {
            if (IsDailyActiveCompleted(client, DailyActiveTypes.EquipForge))
                return;

            if (!CheckLevCondition(client, DailyActiveTypes.EquipForge))
            {
                return;
            }

            //int nToday = (int)TimeUtil.NowDateTime().DayOfYear;
            //int nDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveDayID);

            int nNum = 0;
            nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveEquipForge);

            //if (nDay == nToday)
            //{
                ++nNum;
            //}
            //else
            //{
            //    nNum = 1;
            //}

            ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveEquipForge, true);

            bool bIsCompleted = false;

            CheckSingleConditionForDailyActive(client, DailyActiveTypes.EquipForge, DailyActiveTypes.EquipForge, nNum, "QiangHuaLimit", out bIsCompleted);
        }

        /// <summary>
        /// 处理每日装备追加
        /// </summary>
        /// <returns></returns>
        public static void ProcessDailyActiveEquipAppend(GameClient client)
        {
            if (IsDailyActiveCompleted(client, DailyActiveTypes.EquipAppend))
                return;

            if (!CheckLevCondition(client, DailyActiveTypes.EquipAppend))
            {
                return;
            }

//             int nToday = (int)TimeUtil.NowDateTime().DayOfYear;
//             int nDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveDayID);
// 
               int nNum = 0;
               nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveEquipAppend);
// 
//             if (nDay == nToday)
//             {
                 ++nNum;
//             }
//             else
//             {
//                 nNum = 1;
//             }

            ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveEquipAppend, true);

            bool bIsCompleted = false;

            CheckSingleConditionForDailyActive(client, DailyActiveTypes.EquipAppend, DailyActiveTypes.EquipAppend, nNum, "ZhuiJiaLimit", out bIsCompleted);
        }

        /// <summary>
        /// 处理每日转生活跃
        /// </summary>
        /// <returns></returns>
        public static void ProcessDailyActiveChangeLife(GameClient client)
        {
            if (IsDailyActiveCompleted(client, DailyActiveTypes.CompleteChangeLife))
                return;

            if (!CheckLevCondition(client, DailyActiveTypes.CompleteChangeLife))
            {
                return;
            }

//             int nToday = (int)TimeUtil.NowDateTime().DayOfYear;
//             int nDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveDayID);
// 
               int nNum = 0;
               nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveChangeLife);
// 
//             if (nDay == nToday)
//             {
                 ++nNum;
//             }
//             else
//             {
//                 nNum = 1;
//             }

            ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveChangeLife, true);

            bool bIsCompleted = false;

            CheckSingleConditionForDailyActive(client, DailyActiveTypes.CompleteChangeLife, DailyActiveTypes.CompleteChangeLife, nNum, "ZhuanShengLimit", out bIsCompleted);
        }

        /// <summary>
        /// 处理每日合成果实
        /// </summary>
        /// <returns></returns>
        public static void ProcessDailyActiveMergeFruit(GameClient client)
        {
            if (IsDailyActiveCompleted(client, DailyActiveTypes.MergeFruit))
                return;

            if (!CheckLevCondition(client, DailyActiveTypes.MergeFruit))
            {
                return;
            }

//             int nToday = (int)TimeUtil.NowDateTime().DayOfYear;
//             int nDay = Global.GetRoleParamsInt32FromDB(client, RoleParamName.DailyActiveDayID);
// 
               int nNum = 0;
               nNum = (int)GetDailyActiveDataByField(client, DailyActiveDataField1.DailyActiveMergeFruit);
// 
//             if (nDay == nToday)
//             {
                  ++nNum;
//             }
//             else
//             {
//                 nNum = 1;
//             }

            ModifyDailyActiveInfor(client, (uint)nNum, DailyActiveDataField1.DailyActiveMergeFruit, true);

            bool bIsCompleted = false;

            CheckSingleConditionForDailyActive(client, DailyActiveTypes.MergeFruit, DailyActiveTypes.MergeFruit, nNum, "HeChengLimit", out bIsCompleted);
        }

        /// <summary>
        /// 清空DailyActive数据
        /// </summary>
        /// <returns></returns>
        public static bool CheckLevCondition(GameClient client, int daTpye)
        {
            SystemXmlItem itemDailyActive = null;

            GameManager.systemDailyActiveInfo.SystemXmlItemDict.TryGetValue(daTpye, out itemDailyActive);

            if (null == itemDailyActive)
                return false;

            // 条件
            int MinZhuanshengleve = -1;
            MinZhuanshengleve = itemDailyActive.GetIntValue("MinZhuanshengleve");
            if (client.ClientData.ChangeLifeCount < MinZhuanshengleve)
            {
                return false;
            }
            else if (client.ClientData.ChangeLifeCount == itemDailyActive.GetIntValue("MinZhuanshengleve"))
            {
                int nLev = -1;
                nLev = itemDailyActive.GetIntValue("Minleve");

                if (client.ClientData.Level < nLev)
                    return false;
            }

            return true;
        }


        /// <summary>
        /// 清空DailyActive数据
        /// </summary>
        /// <returns></returns>
        public static void CleanDailyActiveInfo(GameClient client)
        {
            List<ulong> lsLong = new List<ulong>();

            Global.SaveRoleParamsUlongListToDB(client, lsLong, RoleParamName.DailyActiveFlag, true);

            List<uint> lsUint = new List<uint>();

            Global.SaveRoleParamsUintListToDB(client, lsUint, RoleParamName.DailyActiveInfo1, true);

            int nToday = (int)TimeUtil.NowDateTime().DayOfYear;

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DailyActiveDayID, nToday, true);

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.DailyActiveAwardFlag, 0, true);

            client.ClientData.DailyActiveDayID              = nToday;
            client.ClientData.DailyActiveValues             = 0;
            client.ClientData.DailyTotalKillMonsterNum      = 0;
            client.ClientData.DailyCompleteDailyTaskCount   = 0;
            client.ClientData.DailyNextKillMonsterNum       = 0;
            client.ClientData.DailyActiveDayBuyItemInMall   = 0;
            client.ClientData.DailyActiveDayLginCount       = 0;

            // 注释掉 为玩家节省网络流量
            //NotifyClientDailyActiveData(client, -1, true);
        }
    }
}
