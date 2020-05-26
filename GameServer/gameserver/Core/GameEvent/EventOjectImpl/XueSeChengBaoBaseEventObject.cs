using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 血色城堡事件对象
    /// </summary>
    public class XueSeChengBaoBaseEventObject : EventObject
    {
        /// <summary>
        /// 血色城堡的状态
        /// </summary>
        public int _BloodCastleStatus = 0;

        public XueSeChengBaoBaseEventObject(int bloodCastleStatus)
            : base((int)EventTypes.XueSeChengBao)
        {
            this._BloodCastleStatus = bloodCastleStatus;
        }

        /// <summary>
        /// 创建血色城的状态事件对象
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static XueSeChengBaoBaseEventObject CreateStatusEvent(int status)
        {
            return new XueSeChengBaoBaseEventObject(status);
        }
    }
}
