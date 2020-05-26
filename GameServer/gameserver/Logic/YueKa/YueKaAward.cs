using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Server.Data;

namespace GameServer.Logic.YueKa
{
    /// <summary>
    /// 每日的月卡奖励
    /// </summary>
    class YueKaAward
    {
        /// <summary>
        /// 第几天的月卡奖励
        /// </summary>
        public int Day = 0;

        /// <summary>
        /// 奖励的钻石
        /// </summary>
        public int BindZuanShi = 0;

        /// <summary>
        /// 所有职业都发的奖励
        /// ID，数量，绑定，强化等级，追加等级，幸运，卓越
        /// </summary>
        public List<Tuple<int, int, int, int, int, int, int>> AllGoodsList = new List<Tuple<int, int, int, int, int, int, int>>();

        /// <summary>
        /// 按职业区分的奖励
        /// ID，数量，绑定，强化等级，追加等级，幸运，卓越
        /// </summary>
        public List<Tuple<int, int, int, int, int, int, int>> OccGoodsList = new List<Tuple<int, int, int, int, int, int, int>>();

        /// <summary>
        /// 初始化奖励信息
        /// </summary>
        /// <param name="xml"></param>
        public void Init(XElement xml)
        {
            Day = (int)Global.GetSafeAttributeLong(xml, "Day");
            BindZuanShi = (int)Global.GetSafeAttributeLong(xml, "BandZuanShiAward");
            _InitGoods(AllGoodsList, Global.GetSafeAttributeStr(xml, "GoodsOne"));
            _InitGoods(OccGoodsList, Global.GetSafeAttributeStr(xml, "GoodsTwo"));
        }

        /// <summary>
        /// 根据职业获取自己的奖励
        /// </summary>
        /// <param name="occ">职业</param>
        /// <returns></returns>
        public List<GoodsData> GetGoodsByOcc(int occ)
        {
            List<GoodsData> goodsDataList = new List<GoodsData>();

            // 所有职业都发的奖励
            foreach (var detail in AllGoodsList)
            {
                GoodsData data = _ParseGoodsFromDetail(detail);
                goodsDataList.Add(data);
            }

            // 根据职业选择发送的奖励
            foreach (var detail in OccGoodsList)
            {
                if (Global.IsRoleOccupationMatchGoods(occ, detail.Item1))
                {
                    GoodsData data = _ParseGoodsFromDetail(detail);
                    goodsDataList.Add(data);
                }
            }

            return goodsDataList;
        }

        /// <summary>
        /// 把Tuple信息转化为GoodsData
        /// </summary>
        /// <param name="detail"></param>
        /// <returns></returns>
        private GoodsData _ParseGoodsFromDetail(Tuple<int, int, int, int, int, int, int> detail)
        {
            GoodsData goods = new GoodsData()
            {
                Id = -1,
                GoodsID = detail.Item1,
                Using = 0,
                Forge_level = detail.Item4,
                Starttime = "1900-01-01 12:00:00",
                Endtime = Global.ConstGoodsEndTime,
                Site = 0,
                Quality = 0,
                Props = "",
                GCount = detail.Item2,
                Binding = detail.Item3,
                Jewellist = "",
                BagIndex = 0,
                AddPropIndex = 0,
                BornIndex = 0,
                Lucky = detail.Item6,
                Strong = 0,
                ExcellenceInfo = detail.Item7,
                AppendPropLev = detail.Item5,
                ChangeLifeLevForEquip = 0,
            };

            return goods;
        }

        private void _InitGoods(List<Tuple<int, int, int, int, int, int, int>> lst, string goods)
        {
            if (string.IsNullOrEmpty(goods))
            {
                return;
            }

            string[] fields = goods.Split('|');
            foreach (var field in fields)
            {
                string[] details = field.Split(',');
                if (details.Length != 7)
                {
                    continue;
                }

                int goodsID = Convert.ToInt32(details[0]);
                int goodsCnt = Convert.ToInt32(details[1]);
                int goodsBind = Convert.ToInt32(details[2]);
                int goodsForge = Convert.ToInt32(details[3]);
                int goodsAppend = Convert.ToInt32(details[4]);
                int goodsLucky = Convert.ToInt32(details[5]);
                int goodsExcellence = Convert.ToInt32(details[6]);

                lst.Add(new Tuple<int, int, int, int, int, int, int>(
                    goodsID, goodsCnt, goodsBind, goodsForge, goodsAppend, goodsLucky, goodsExcellence));
            }
        }
    }
}
