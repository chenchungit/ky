using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.cc.Defult
{
    public class GlobalDefultObject
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID
        { set; get; }
        /// <summary>
        /// 职业
        /// </summary>
        public int Occupation
        { set; get; }
        /// <summary>
        ///技能ID
        /// </summary>
        public int SkillID
        { set; get; }
        /// <summary>
        ///血
        /// </summary>
        public int HP
        { set; get; }
        /// <summary>
        ///蓝
        /// </summary>
        public int MP
        { set; get; }
    }
}
