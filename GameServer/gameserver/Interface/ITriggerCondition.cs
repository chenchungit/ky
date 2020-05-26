using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;

namespace GameServer.Interface
{
    /// <summary>
    /// Boss AI 触发条件接口
    /// </summary>
    public interface ITriggerCondition
    {
        /// <summary>
        /// 对象的类型
        /// </summary>
        BossAITriggerTypes TriggerType
        {
            get;
            set;
        }
    }
}
