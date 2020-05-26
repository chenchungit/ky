using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.TarotData
{
    /// <summary>
    /// 玩家卡罗牌相关数据
    /// </summary>
    [ProtoContract]
    public class TarotSystemData
    {
        /// <summary>
        /// 国王特权数据
        /// </summary>
        [ProtoMember(1)]
        public TarotKingData KingData;

        /// <summary>
        /// 玩家卡牌数据
        /// </summary>
        [ProtoMember(2)]
        public List<TarotCardData> TarotCardDatas;

        public TarotSystemData()
        {
            KingData = new TarotKingData();
            TarotCardDatas = new List<TarotCardData>();
        }
    }

    /// <summary>
    /// 塔罗牌数据(存储结构 GoodId_Level_Exp_Postion;)
    /// </summary>
    [ProtoContract]
    public class TarotCardData
    {
        /// <summary>
        /// 道具ID
        /// </summary>
        [ProtoMember(1)]
        public int GoodId = 0;

        /// <summary>
        /// 卡牌等级
        /// </summary>
        [ProtoMember(2)]
        public int Level = 0;

        /// <summary>
        /// 装备位置(0=未装备)
        /// </summary>
        [ProtoMember(3)]
        public byte Postion = 0;

        public string GetDataStrInfo()
        {
            return string.Format("{0}_{1}_{2}", GoodId, Level, Postion);
        }
    }

    [ProtoContract]
    public class TarotKingData
    {
        [ProtoMember(1)]
        public long StartTime = 0;

        /// <summary>
        /// 持续时间(单位 秒)
        /// </summary>
        [ProtoMember(2)]
        public long BufferSecs = 0;

        [ProtoMember(3)]
        public Dictionary<int, int> AddtionDict;

        public TarotKingData()
        {
            AddtionDict = new Dictionary<int, int>();
        }

        public string GetDataStrInfo()
        {
            var addStr = string.Empty;
            if (AddtionDict.Count == 3)
            {
                foreach (var addtion in AddtionDict)
                {
                    addStr += addtion.Key + "@" + addtion.Value + ",";
                }
            }
            return string.Format("{0}_{1}_{2}", StartTime, BufferSecs, addStr);
        }
    }
}
