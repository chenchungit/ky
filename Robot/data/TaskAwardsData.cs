using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 任务奖励数据
    /// </summary>
    [ProtoContract]
    public class TaskAwardsData
    {
        /// <summary>
        /// 任务奖励
        /// </summary>
        [ProtoMember(1)]
        public List<AwardsItemData> TaskawardList = null;

        /// <summary>
        /// 任务其他奖励
        /// </summary>
        [ProtoMember(2)]
        public List<AwardsItemData> OtherTaskawardList = null;

        /// <summary>
        /// 任务金币奖励
        /// </summary>
        [ProtoMember(3)]
        public int Moneyaward = 0;

        /// <summary>
        /// 任务经验奖励
        /// </summary>
        [ProtoMember(4)]
        public long Experienceaward = 0;

        /// <summary>
        /// 任务银两奖励
        /// </summary>
        [ProtoMember(5)]
        public int YinLiangaward = 0;

        /// <summary>
        /// 任务灵力奖励
        /// </summary>
        [ProtoMember(6)]
        public int LingLiaward = 0;

        /// <summary>
        /// 任务绑定钻石奖励
        /// </summary>
        [ProtoMember(7)]
        public int BindYuanBaoaward = 0;

        /// <summary>
        /// 真气奖励
        /// </summary>
        [ProtoMember(8)]
        public int ZhenQiaward = 0;

        /// <summary>
        /// 猎杀值奖励
        /// </summary>
        [ProtoMember(9)]
        public int LieShaaward = 0;

        /// <summary>
        /// 悟性值奖励
        /// </summary>
        [ProtoMember(10)]
        public int WuXingaward = 0;

        /// <summary>
        /// 钻石完成需要消耗钻石
        /// </summary>
        [ProtoMember(11)]
        public int NeedYuanBao = 0;

        /// <summary>
        /// 军功值奖励
        /// </summary>
        [ProtoMember(12)]
        public int JunGongaward = 0;

        /// <summary>
        /// 荣誉奖励
        /// </summary>
        [ProtoMember(13)]
        public int RongYuaward = 0;

        // 任务改造 新增日常跑环任务 Begin  [12/5/2013 LiaoWei]
        // 说明--以下数据只有任务类型(taskclass)为DailyTask的任务才有意义
        /// <summary>
        /// 完成所有环额外经验奖励
        /// </summary>
        [ProtoMember(14)]
        public int AddExperienceForDailyCircleTask = 0;

        /// <summary>
        /// 完成所有环额外魔晶奖励
        /// </summary>
        [ProtoMember(15)]
        public int AddMoJingForDailyCircleTask = 0;

        /// <summary>
        /// 完成所有环额外物品奖励
        /// </summary>
        [ProtoMember(16)]
        public string AddGoodsForDailyCircleTask = "";

        // 任务改造 End  [12/5/2013 LiaoWei]

        /// <summary>
        /// 魔晶奖励 [4/10/2014 LiaoWei]
        /// </summary>
        [ProtoMember(17)]
        public int MoJingaward = 0;

        /// <summary>
        /// 星魂奖励
        /// </summary>
        [ProtoMember(18)]
        public int XingHunaward = 0;

        /// <summary>
        /// 粉末奖励
        /// </summary>
        [ProtoMember(19)]
        public int FenMoAward = 0;

        /// <summary>
        /// 声望奖励
        /// </summary>
        [ProtoMember(20)]
        public int ShengwangAward = 0;

    }
}
