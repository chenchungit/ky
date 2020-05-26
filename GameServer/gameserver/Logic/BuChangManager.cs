using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 补偿item，每一个item由一些条件和奖励品组成
    /// </summary>
    public class BuChangItem
    {
        /// <summary>
        /// 补偿的等级
        /// </summary>
        public int MinLevel = 0;

        /// <summary>
        /// 补偿的转生
        /// </summary>
        public int MinZhuanSheng = 0;

        /// <summary>
        /// 补偿的最大等级
        /// </summary>
        public int MaxLevel = 0;

        /// <summary>
        /// 补偿的最大转生
        /// </summary>
        public int MaxZhuanSheng = 0;

        /// <summary>
        /// 补偿的魔晶
        /// </summary>
        public int MoJing = 0;

        /// <summary>
        /// 补偿的经验
        /// </summary>
        public long Experience = 0;

        /// <summary>
        /// 补偿物品列表
        /// </summary>
        public List<GoodsData> GoodsDataList = new List<GoodsData>();
    }

    /// <summary>
    /// 补偿玩家
    /// </summary>
    public class BuChangManager
    {
        #region 缓存项

        /// <summary>
        /// 补偿的字典
        /// </summary>
        private static Dictionary<RangeKey, BuChangItem> _BuChangItemDict = new Dictionary<RangeKey, BuChangItem>(new RangeKey(0, 0));

        /// <summary>
        /// 重置补偿的字典
        /// </summary>
        public static void ResetBuChangItemDict()
        {
            GameManager.SystemBuChang.ReloadLoadFromXMlFile();

            InitBuChangDict();
        }

        private static void InitBuChangDict()
        {
            lock (_BuChangItemDict)
            {
                _BuChangItemDict.Clear();
            }

            foreach (var systemBuChangItem in GameManager.SystemBuChang.SystemXmlItemDict.Values)
            {
                BuChangItem buChangItem = new BuChangItem()
                {
                    MinLevel = systemBuChangItem.GetIntValue("MinLevel"),
                    MinZhuanSheng = systemBuChangItem.GetIntValue("MinZhuanSheng"),
                    MaxLevel = systemBuChangItem.GetIntValue("MaxLevel"),
                    MaxZhuanSheng = systemBuChangItem.GetIntValue("MaxZhuanSheng"),
                    Experience = Math.Max(0, (long)systemBuChangItem.GetDoubleValue("AwardExp")),
                    MoJing = Math.Max(0, systemBuChangItem.GetIntValue("MoJing")),
                    GoodsDataList = ParseGoodsDataList(systemBuChangItem.GetStringValue("Goods")),
                };

                int minUnionLevel = Global.GetUnionLevel(buChangItem.MinZhuanSheng, buChangItem.MinLevel);
                int maxUnionLevel = Global.GetUnionLevel(buChangItem.MaxZhuanSheng, buChangItem.MaxLevel);
                lock (_BuChangItemDict)
                {
                    _BuChangItemDict[new RangeKey(minUnionLevel, maxUnionLevel)] = buChangItem;
                }
            }
        }

        /// <summary>
        /// 获取补偿的项
        /// </summary>
        public static BuChangItem GetBuChangItem(int unionLevel)
        {
            BuChangItem buChangItem = null;
            lock (_BuChangItemDict)
            {
                if (_BuChangItemDict.TryGetValue(unionLevel, out buChangItem))
                {
                    return buChangItem;
                }
            }

            InitBuChangDict();

            lock (_BuChangItemDict)
            {
                if (_BuChangItemDict.TryGetValue(unionLevel, out buChangItem))
                {
                    return buChangItem;
                }
            }

            return buChangItem;
        }

        /// <summary>
        /// 获取补偿经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static long GetBuChangExp(GameClient client)
        {
            BuChangItem buChangItem = GetBuChangItem(Global.GetUnionLevel(client));
            if (null == buChangItem)
            {
                return 0;
            }

            return buChangItem.Experience;
        }

        /// <summary>
        /// 获取补偿绑定元宝
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetBuChangBindYuanBao(GameClient client)
        {
            BuChangItem buChangItem = GetBuChangItem(Global.GetUnionLevel(client));
            if (null == buChangItem)
            {
                return 0;
            }

            return buChangItem.MoJing;
        }

        /// <summary>
        /// 获取物品列表
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static List<GoodsData> GetBuChangGoodsDataList(GameClient client)
        {
            BuChangItem buChangItem = GetBuChangItem(Global.GetUnionLevel(client));
            if (null == buChangItem)
            {
                return null;
            }

            return buChangItem.GoodsDataList;
        }

        /// <summary>
        /// 将物品字符串列表解析成物品数据列表
        /// </summary>
        /// <param name="goodsStr"></param>
        /// <returns></returns>
        private static List<GoodsData> ParseGoodsDataList(string goodsIDs)
        {
            List<GoodsData> goodsDataList = new List<GoodsData>();
            if (string.IsNullOrEmpty(goodsIDs))
            {
                return goodsDataList;
            }

            // 物品参数改造 [5/11/2014 LiaoWei]
            string[] fields = goodsIDs.Split('|');
            for (int i = 0; i < fields.Length; i++)
            {
                string[] sa = fields[i].Split(',');
                if (sa.Length != 7)
                {
                    continue;
                }

                int[] goodsFields = Global.StringArray2IntArray(sa);

                //获取物品数据
                GoodsData goodsData = Global.GetNewGoodsData(goodsFields[0], goodsFields[1], 0, goodsFields[3], goodsFields[2], 0, goodsFields[5], 0, goodsFields[6], goodsFields[4], 0);
                goodsDataList.Add(goodsData);
            }

            return goodsDataList;
        }

        #endregion 缓存项

        #region 给予补偿

        /// <summary>
        /// 根据奖励时间判断当前时间是否满足给予补偿时间
        /// </summary>
        /// <returns></returns>
        public static bool CanGiveBuChang()
        {
            try
            {
                string AwardStartDate = Global.GetTimeByBuChang(0, 0, 0, 0);
                string AwardEndDate = Global.GetTimeByBuChang(1, 23, 59, 59);
                DateTime startAward = DateTime.Parse(AwardStartDate);
                DateTime endAward = DateTime.Parse(AwardEndDate);

                if (TimeUtil.NowDateTime() >= startAward && TimeUtil.NowDateTime() <= endAward)
                {
                    return true;
                }
            }
            catch (Exception)
            {

            }

            return false;
        }

        /// <summary>
        /// 背包中是否有足够的位置
        /// </summary>
        /// <returns></returns>
        public static bool HasEnoughBagSpaceForAwardGoods(GameClient client, BuChangItem buChangItem)
        {
            int needSpace = 0;
            needSpace = buChangItem.GoodsDataList.Count;
            if (needSpace <= 0)
            {
                return true;
            }

            //判断背包空格是否能提交接受奖励的物品
            return Global.CanAddGoodsDataList(client, buChangItem.GoodsDataList);
        }

        /// <summary>
        /// 给予补偿
        /// </summary>
        /// <param name="client"></param>
        public static bool CheckGiveBuChang(GameClient client)
        {
            if (!CanGiveBuChang())
            {
                return false;
            }

            BuChangItem buChangItem = GetBuChangItem(Global.GetUnionLevel(client));
            if (null == buChangItem)
            {
                return false;
            }

            DateTime buChangDateTime = Global.GetBuChangStartDay();
            int buChangFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BuChangFlag);
            if (buChangDateTime.DayOfYear == buChangFlag)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 给予补偿
        /// </summary>
        /// <param name="client"></param>
        public static void GiveBuChang(GameClient client)
        {
            /// 根据奖励时间判断当前时间是否满足给予补偿时间
            
            if (!CanGiveBuChang())
            {
                GameManager.LuaMgr.Error(client, Global.GetLang("非补偿期间，无法获取补偿!"));
                return;
            }

            BuChangItem buChangItem = GetBuChangItem(Global.GetUnionLevel(client));
            if (null == buChangItem)
            {
                GameManager.LuaMgr.Error(client, Global.GetLang("没有找到对应的补偿项!"));
                return;
            }

            /// 背包中是否有足够的位置
            if (!HasEnoughBagSpaceForAwardGoods(client, buChangItem))
            {
                GameManager.LuaMgr.Error(client, Global.GetLang("背包已满，请先清理出空格后再领取补偿!"));
                return;
            }

            DateTime buChangDateTime = Global.GetBuChangStartDay();
            int buChangFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BuChangFlag);
            if (buChangDateTime.DayOfYear == buChangFlag)
            {
                GameManager.LuaMgr.Error(client, Global.GetLang("已经领取过补偿，无法再次领取!"));
                return;
            }

            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.BuChangFlag, buChangDateTime.DayOfYear, true); //先保存后给予奖励

            //获取奖励的物品
            for (int i = 0; i < buChangItem.GoodsDataList.Count; i++)
            {
                //想DBServer请求加入某个新的物品到背包中
                //添加物品
                Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                    buChangItem.GoodsDataList[i].GoodsID, buChangItem.GoodsDataList[i].GCount,
                    buChangItem.GoodsDataList[i].Quality, "", buChangItem.GoodsDataList[i].Forge_level,
                    buChangItem.GoodsDataList[i].Binding, 0, "", true, 1,
                    /**/"系统补偿物品", Global.ConstGoodsEndTime, buChangItem.GoodsDataList[i].AddPropIndex, buChangItem.GoodsDataList[i].BornIndex, buChangItem.GoodsDataList[i].Lucky, buChangItem.GoodsDataList[i].Strong);
            }

            //获取奖励的魔晶
            if (buChangItem.MoJing > 0)
            {
                GameManager.ClientMgr.ModifyTianDiJingYuanValue(client, buChangItem.MoJing, "系统补偿", false, true);
            }

            //获取奖励的经验
            if (buChangItem.Experience > 0)
            {
                //处理角色经验
                GameManager.ClientMgr.ProcessRoleExperience(client, buChangItem.Experience, false, true);
            }

            client._IconStateMgr.CheckBuChangState(client);
            client._IconStateMgr.SendIconStateToClient(client);
        }

        #endregion 给予补偿
    }
}
