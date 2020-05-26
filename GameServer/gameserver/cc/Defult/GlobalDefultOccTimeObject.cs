using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.cc.Defult
{
   public class GlobalDefultOccTimeObject
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int ID
        {
            set;get;
        }
        /// <summary>
        /// 职业
        /// </summary>
        public int Occ
        {
            set;get;
        }
        /// <summary>
        /// 移动速度
        /// </summary>
        public int MoveSpeed
        {
            set;get;
        }
        /// <summary>
        /// 每帧移动时间
        /// </summary>
        public int MoveFrameTime
        {
            set;get;
        }
        /// <summary>
        /// 移动类型
        /// </summary>
        public int MoveType
        {
            set;get;
        }
    }
}
