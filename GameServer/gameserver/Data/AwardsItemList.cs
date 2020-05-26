using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Server.Data;

namespace GameServer.Logic
{
    public class AwardsItemList
    {
        List<AwardsItemData> list = new List<AwardsItemData>();

        public List<AwardsItemData> Items { get { return list; } }

        public AwardsItemData ParseItem(string awardsString)
        {
            if (!string.IsNullOrEmpty(awardsString))
            {
                string[] strFields = awardsString.Split('|');
                for (int i = 0; i < strFields.Length; ++i)
                {
                    string[] strGoods = strFields[i].Split(',');
                    if (strGoods.Length == 7)
                    {
                        AwardsItemData awardsItemData = new AwardsItemData();
                        awardsItemData.GoodsID = Global.SafeConvertToInt32(strGoods[0]);
                        awardsItemData.GoodsNum = Global.SafeConvertToInt32(strGoods[1]);
                        awardsItemData.Binding = Global.SafeConvertToInt32(strGoods[2]);
                        awardsItemData.Level = Global.SafeConvertToInt32(strGoods[3]);
                        awardsItemData.AppendLev = Global.SafeConvertToInt32(strGoods[4]);
                        awardsItemData.IsHaveLuckyProp = Global.SafeConvertToInt32(strGoods[5]);
                        awardsItemData.ExcellencePorpValue = Global.SafeConvertToInt32(strGoods[6]);
                        awardsItemData.EndTime = Global.ConstGoodsEndTime;
                        return awardsItemData;
                    }
                }
            }
            
            return null;
        }

        public bool ItemEqual(AwardsItemData item0, AwardsItemData item1)
        {
            if (item0.GoodsID == item1.GoodsID &&
                item0.Binding == item1.Binding &&
                item0.Level == item1.Level &&
                item0.AppendLev == item1.AppendLev &&
                item0.IsHaveLuckyProp == item1.IsHaveLuckyProp &&
                item0.ExcellencePorpValue == item1.ExcellencePorpValue &&
                item0.Occupation == item1.Occupation && 
                item0.EndTime == item1.EndTime)
            {
                return true;
            }

            return false;
        }

        public void Add(string awardsString)
        {
            if (!string.IsNullOrEmpty(awardsString))
            {
                string[] strFields = awardsString.Split('|');
                for (int i = 0; i < strFields.Length; ++i)
                {
                    AwardsItemData itemData = ParseItem(strFields[i]);
                    if (null != itemData)
                    {
                        list.Add(itemData);
                    }
                }
            }
        }

        public void AddNoRepeat(string awardsString)
        {
            if (!string.IsNullOrEmpty(awardsString))
            {
                string[] strFields = awardsString.Split('|');
                for (int i = 0; i < strFields.Length; ++i)
                {
                    AwardsItemData itemData = ParseItem(strFields[i]);
                    if (null != itemData)
                    {
                        int j = 0;
                        for (; j < list.Count; j++)
                        {
                            AwardsItemData item = list[j];
                            if (ItemEqual(itemData, item))
                            {
                                item.GoodsNum += itemData.GoodsNum;
                                break;
                            }
                        }
                        if (j == list.Count)
                        {
                            list.Add(itemData);
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            for (int j = 0; j < list.Count; j++)
            {
                AwardsItemData item = list[j];
                result.AppendFormat("{0},{1},{2},{3},{4},{5},{6}|", item.GoodsID, item.GoodsNum, item.Binding, item.Level, item.AppendLev, item.IsHaveLuckyProp, item.ExcellencePorpValue);
            }

            return result.ToString().TrimEnd('|');
        }
    }
}
