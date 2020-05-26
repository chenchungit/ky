﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
//using GameServer.Logic;

namespace Server.Data
{
    /// <summary>
    /// 公告消息数据
    /// </summary>
    [ProtoContract]
    public class DailyTaskData
    {
        /// <summary>
        /// 环的ID
        /// </summary>
        [ProtoMember(1)]
        public int HuanID = 0;

        /// <summary>
        /// 跑环的日子
        /// </summary>
        [ProtoMember(2)]
        public string RecTime = "";

        /// <summary>
        /// 跑环的次数
        /// </summary>
        [ProtoMember(3)]
        public int RecNum = 0;

        /// <summary>
        /// 跑环的任务类型
        /// </summary>
        [ProtoMember(4)]
        public int TaskClass = 0;//0(int)TaskClasses.MainTask;

        /// <summary>
        /// 额外的次数天ID
        /// </summary>
        [ProtoMember(5)]
        public int ExtDayID = 0;

        /// <summary>
        /// 额外的次数
        /// </summary>
        [ProtoMember(6)]
        public int ExtNum = 0;
    }
}