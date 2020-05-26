using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 被挑战者数据
    /// </summary>
    [ProtoContract]
    public class JingJiBeChallengeData
    {
        /// <summary>
        /// 返回状态码
        /// 1:请求成功，0：非法参数,-1:冷却时间未到，-2：被挑战机器人不存在,-3:被挑战机器人排名已更改,-4:正在被其他玩家挑战
        /// </summary>
        [ProtoMember(1)]
        public int state;

        /// <summary>
        /// 被挑战者数据
        /// </summary>
        [ProtoMember(2)]
        public PlayerJingJiData beChallengerData = null;
    }
}
