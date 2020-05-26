using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    public class TalentPropData
    {
        /// <summary>
        /// 单个技能(技能id，技能等级)
        /// </summary>
        public Dictionary<int, int> SkillOneValue = new Dictionary<int, int>();

        /// <summary>
        /// 全部技能
        /// </summary>
        public int SkillAllValue = 2;

        /// <summary>
        /// 增加属性
        /// 1.效果（一级，二级）
        /// 2.附加属性
        /// </summary>
        public EquipPropItem PropItem = new EquipPropItem();

        public TalentPropData()
        {
            ResetProps();
        }

        /// <summary>
        /// 清空属性值
        /// </summary>
        public void ResetProps()
        {
            this.PropItem.ResetProps();
            this.SkillOneValue = new Dictionary<int, int>();
            this.SkillAllValue = 0;
        }
    }
}
