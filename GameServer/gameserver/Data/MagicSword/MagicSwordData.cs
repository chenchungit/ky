using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 魔剑士静态数据 [XSea 2015/6/9]
    /// </summary>
    class MagicSwordData
    {
        /// <summary>
        /// 魔剑士初始任务id
        /// </summary>
        public static int InitTaskID;

        /// <summary>
        /// 魔剑士初始任务npcid
        /// </summary>
        public static int InitTaskNpcID;

        /// <summary>
        /// 魔剑士初始任务id之前的一个任务id
        /// </summary>
        public static int InitPrevTaskID;

        /// <summary>
        /// 魔剑士初始场景id
        /// </summary>
        public static int InitMapID;

        /// <summary>
        /// 魔剑士初始转生数
        /// </summary>
        public static int InitChangeLifeCount;

        /// <summary>
        /// 魔剑士初始级数
        /// </summary>
        public static int InitLevel;

        /// <summary>
        /// 力量魔剑士大天使武器
        /// </summary>
        public static List<int> StrengthWeaponList;

        /// <summary>
        /// 力量魔剑士大天使武器
        /// </summary>
        public static List<int> IntelligenceWeaponList;

        /// <summary>
        /// 力魔剑士普攻技能id
        /// </summary>
        public static int StrAttackID;

        /// <summary>
        /// 智魔剑士普攻技能id
        /// </summary>
        public static int IntAttackID;
    }
}
