using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using KF.Contract.Data;

namespace Server.Data
{
    /*
     * 进入16强赛的玩家都会分配一个参赛编号，这里成为Group[1-16]
     * 之后，每两个玩家的pk可以由一个UnionGroup唯一标识
     * 设 MinGroup, MaxGroup = 两个玩家group中较小的一个，两个玩家group中较大的一个
     * 那么UnionGroup = MinGroup * 1000 + MaxGroup
     */
//     public static class ZhengBaUtils
//     {
//         public static int MakeUnionGroup(int group1, int group2)
//         {
//             if (group1 > group2)
//             {
//                 int tmp = group1;
//                 group1 = group2;
//                 group2 = tmp;
//             }
// 
//             return group1 * 1000 + group2;
//         }
// 
//         public static void SplitUnionGroup(int union, out int group1, out int group2)
//         {
//             group1 = union % 1000;
//             group2 = union / 1000;
//         }
//     }

    /// <summary>
    /// 众神争霸---主界面信息
    /// </summary>
    [ProtoContract]
    public class ZhengBaMainInfoData
    {
        /// <summary>
        /// 考虑到服务器维护导致活动推移的情况，
        /// 此字段表示实际进行到活动的第几天
        /// </summary>
        [ProtoMember(1)]
        public int RealActDay;

        /// <summary>
        /// 16强列表
        /// </summary>
        [ProtoMember(2)]
        public List<TianTiPaiHangRoleData> Top16List;

        /// <summary>
        /// 当前获得赞最多 [1--16]才有效
        /// </summary>
        [ProtoMember(3)]
        public int MaxSupportGroup;

        /// <summary>
        /// 当前获得贬最多 [1--16]才有效
        /// </summary>
        [ProtoMember(4)]
        public int MaxOpposeGroup;

        /// <summary>
        /// 可以领取的奖励Id [1--8有效]
        /// 参考MatchAward.xml的Id字段
        /// </summary>
        [ProtoMember(5,IsRequired = true)]
        public int CanGetAwardId;

        /// <summary>
        /// 当前是第几天的战斗结果
        /// </summary>
        [ProtoMember(6,IsRequired=true)]
        public int RankResultOfDay;
    }

    /// <summary>
    /// 众神争霸---16强pk组数据
    /// </summary>
    [ProtoContract]
    public class ZhengBaUnionGroupData
    {
        /// <summary>
        /// group组， 由该group组合可以唯一标识两个16强玩家
        /// </summary>
        [ProtoMember(1)]
        public int UnionGroup;

        /// <summary>
        /// pk组内玩家的赞、贬、押注统计数据
        /// </summary>
        [ProtoMember(2)]
        public List<ZhengBaSupportAnalysisData> SupportDatas;

        /// <summary>
        /// pk组的支持日志
        /// </summary>
        [ProtoMember(3)]
        public List<ZhengBaSupportLogData> SupportLogs;

        /// <summary>
        /// pk组内我的赞、贬、押注信息
        /// </summary>
        [ProtoMember(4)]
        public List<ZhengBaSupportFlagData> SupportFlags;

        /// <summary>
        /// 押注胜利能获得的争霸点
        /// </summary>
        [ProtoMember(5)]
        public int WinZhengBaPoint;
    }

    /// <summary>
    /// 众神争霸---我的赞、贬、押注标记
    /// </summary>
    [ProtoContract]
    public class ZhengBaSupportFlagData
    {
        /// <summary>
        /// group组， 由该group组合可以唯一标识两个16强玩家
        /// </summary>
        [ProtoMember(1)]
        public int UnionGroup;

        /// <summary>
        /// group，可以标识出一个特定的16强玩家
        /// </summary>
        [ProtoMember(2)]
        public int Group;

        /// <summary>
        /// 是否已赞
        /// </summary>
        [ProtoMember(3)]
        public bool IsOppose;

        /// <summary>
        /// 是否已贬
        /// </summary>
        [ProtoMember(4)]
        public bool IsSupport;

        /// <summary>
        /// 是否已押注
        /// </summary>
        [ProtoMember(5)]
        public bool IsYaZhu;

  
        // 以下字段为服务器需求
        [ProtoMember(6)]
        public int RankOfDay;
    }

    [ProtoContract]
    public class ZhengBaWaitYaZhuAwardData
    {
        [ProtoMember(1)]
        public int Month;
        [ProtoMember(2)]
        public int RankOfDay;
        [ProtoMember(3)]
        public int FromRoleId;
        [ProtoMember(4)]
        public int UnionGroup;
        [ProtoMember(5)]
        public int Group;
    }
}
