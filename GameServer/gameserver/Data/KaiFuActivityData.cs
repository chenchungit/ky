using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    // 开服活动数据 -- 通知客户端 [3/20/2014 LiaoWei]
    [ProtoContract]
    public class KaiFuActivityData
    {
        /// <summary>
        /// 冲击狂人活动剩余名额
        /// </summary>
        [ProtoMember(1)]
        public int[] LevelUpAwardRemainQuota;

        /// <summary>
        /// 玩家冲击狂人活动的领取状态
        /// </summary>
        [ProtoMember(2)]
        public int LevelUpGetAwardState;

        /// <summary>
        /// 玩家击杀BOSS数量
        /// </summary>
        [ProtoMember(2)]
        public int KillBossNum;

    }
}
