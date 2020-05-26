using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using GameServer.Logic.ActivityNew.SevenDay;
using Tmsk.Contract;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 七日目标活动的事件
    /// </summary>
    public class SevenDayGoalEventObject : EventObject
    {
        public GameClient Client;
        public ESevenDayGoalFuncType FuncType;
        public int Arg1;
        public int Arg2;
        public int Arg3;
        public int Arg4;

        public SevenDayGoalEventObject()
            : base((int)EventTypes.SevenDayGoal)
        {
            this.Reset();
        }

        public void Reset()
        {
            this.Client = null;
            this.FuncType = ESevenDayGoalFuncType.Unknown;
            this.Arg1 = 0;
            this.Arg2 = 0;
            this.Arg3 = 0;
            this.Arg4 = 0;
        }
    }
}
