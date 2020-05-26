using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Tools
{
    public class GoodsHelper
    {
        /// <summary>
        /// 将字符串转换成物品列表
        /// </summary>
        public static List<GoodsData> ParseGoodsDataList(string[] fields, string fileName)
        {
            int attrCount = 7;
            List<GoodsData> goodsDataList = new List<GoodsData>();
            for (int i = 0; i < fields.Length; i++)
            {
                string[] sa = fields[i].Split(',');
                if (sa.Length != attrCount)
                {
                    LogManager.WriteLog(LogTypes.Warning, string.Format("解析{0}文件中的奖励项时失败, 物品配置项个数错误", fileName));
                    continue;
                }

                int[] goodsFields = Global.StringArray2IntArray(sa);
                //获取物品数据  物品ID,物品数量,是否绑定,强化等级,追加等级,是否有幸运,卓越属性
                GoodsData goodsData = Global.GetNewGoodsData(goodsFields[0], goodsFields[1], 0, goodsFields[3], goodsFields[2], 0, goodsFields[5], 0, goodsFields[6], goodsFields[4], 0);
                goodsDataList.Add(goodsData);
            }

            return goodsDataList;
        }

        // 根据职业，获取召回奖励
        public static List<GoodsData> GetAwardPro(GameClient client, List<GoodsData> proGoodsList)
        {
            if (proGoodsList == null || proGoodsList.Count <= 0)
                return null;

            List<GoodsData> list = new List<GoodsData>();
            foreach (GoodsData data in proGoodsList)
            {
                if (Global.IsCanGiveRewardByOccupation(client, data.GoodsID))
                    list.Add(data);
            }

            return list;
        }

        //将字符串转换成物品列表
        public static GoodsData ParseGoodsData(string fields, string fileName)
        {
            int attrCount = 7;
            string[] sa = fields.Split(',');
            if (sa.Length != attrCount)
            {
                LogManager.WriteLog(LogTypes.Warning, string.Format("解析{0}文件中的奖励项时失败, 物品配置项个数错误", fileName));
                return null;
            }

            int[] goodsFields = Global.StringArray2IntArray(sa);
            //获取物品数据  物品ID,物品数量,是否绑定,强化等级,追加等级,是否有幸运,卓越属性
            return Global.GetNewGoodsData(goodsFields[0], goodsFields[1], 0, goodsFields[3], goodsFields[2], 0, goodsFields[5], 0, goodsFields[6], goodsFields[4], 0);
        }

    }
}
