using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 天赋经验注入信息
    /// </summary>
    public class TalentExpInfo
    {
        /// <summary>
        /// 成就点数
        /// </summary>
        public int ID = 0; 

        /// <summary>
        /// 需要经验
        /// </summary>
        public long Exp = 0;

        /// <summary>
        /// 角色等级
        /// </summary>
        public int RoleLevel = 0;
    }
}
