using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    // 表示节日赠送王排行中的某一项
    [ProtoContract]
    public class JieriGiveKingItemData
    {
        [ProtoMember(1)]
        public int RoleID;// 角色id  
        [ProtoMember(2)]
        public string Rolename; // 角色名     
        [ProtoMember(3)]
        public int TotalGive; // 一共赠送了多少    
        [ProtoMember(4)]
        public int Rank; // 排名1-N，未进入排行榜则为-1      
        [ProtoMember(5)]
        public int GetAwardTimes; // 领奖次数，大于0表示已领奖
        [ProtoMember(6)]
        public int ZoneID; // 区号，服务器内部用了，客户端可能用不到
    }

    // 节日赠送王活动信息
    [ProtoContract]
    public class JieriGiveKingData
    {       
        [ProtoMember(1)]
        public List<JieriGiveKingItemData> RankingList;// 排行榜数据，可能为空
        [ProtoMember(2)]
        public JieriGiveKingItemData MyData;// [保留]个人赠送数据，客户端可能会用到
    }

    // 表示节日收取王排行中的某一项
    [ProtoContract]
    public class JieriRecvKingItemData
    {
        [ProtoMember(1)]
        public int RoleID;// 角色id  
        [ProtoMember(2)]
        public string Rolename;// 角色名 
        [ProtoMember(3)]
        public int TotalRecv; // 一共赠送了多少   
        [ProtoMember(4)]
        public int Rank;// 排名1-N，未进入排行榜则为-1  
        [ProtoMember(5)]
        public int GetAwardTimes;// 领奖次数，大于0表示已领奖
        [ProtoMember(6)]
        public int ZoneID;// 区号，服务器内部用了，客户端可能用不到
    }

    // 节日收取王活动信息
    [ProtoContract]
    public class JieriRecvKingData
    {
        [ProtoMember(1)]
        public List<JieriRecvKingItemData> RankingList;// 排行榜数据，可能为空
        [ProtoMember(2)]
        public JieriRecvKingItemData MyData;// [保留]个人收取数据，客户端可能会用到
    }
}
