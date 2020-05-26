using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.JingJiChang
{
    /// <summary>
    /// 玩家竞技场数据
    /// </summary>
    public class RoleJingJiChangData
    {
        /// <summary>
        /// 当前排名
        /// </summary>
        public int rankingNum;

        /// <summary>
        /// 已经用掉的免费次数
        /// </summary>
        public int freeCount;

        /// <summary>
        /// 已经用掉的消费次数
        /// </summary>
        public int vipCount;

        /// <summary>
        /// 上一次挑战的时间戳
        /// </summary>
        public long lastChallengeTime;

        /// <summary>
        /// 当前声望值
        /// </summary>
        public int shengwangValue;

        /// <summary>
        /// 当前军衔
        /// </summary>
        public int junxianValue;

        /// <summary>
        /// 连胜次数
        /// </summary>
        public int lianshengNum;
    }
}
