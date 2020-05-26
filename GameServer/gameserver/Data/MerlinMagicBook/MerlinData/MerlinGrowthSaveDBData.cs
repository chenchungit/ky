using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
    /// <summary>
    /// 梅林魔法书成长-存数据库 [XSea 2015/6/18]
    /// </summary>
    [ProtoContract]
    public class MerlinGrowthSaveDBData
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(1)]
        public int _RoleID = 0;

        /// <summary>
        /// 角色职业
        /// </summary>
        [ProtoMember(2)]
        public int _Occupation = 0;

        /// <summary>
        /// 阶数
        /// </summary>
        [ProtoMember(3)]
        public int _Level = 0;

        /// <summary>
        /// 星数
        /// </summary>
        [ProtoMember(4)]
        public int _StarNum = 0;

        /// <summary>
        /// 星级经验值
        /// </summary>
        [ProtoMember(5)]
        public int _StarExp = 0;

        /// <summary>
        /// 升阶用的幸运点
        /// </summary>
        [ProtoMember(6)]
        public int _LuckyPoint = 0;

        /// <summary>
        /// 秘语持续时间
        /// </summary>
        [ProtoMember(7)]
        public long _ToTicks = 0;

        /// <summary>
        /// 开启时间
        /// </summary>
        [ProtoMember(8)]
        public long _AddTime = 0;

        /// <summary>
        /// 生效的秘语属性 key = EMerlinSecretAttrType value = 值
        /// </summary>
        [ProtoMember(9)]
        public Dictionary<int, double> _ActiveAttr = new Dictionary<int, double>();

        /// <summary>
        /// 未生效的秘语属性 key = EMerlinSecretAttrType value = 值
        /// </summary>
        [ProtoMember(10)]
        public Dictionary<int, double> _UnActiveAttr = new Dictionary<int, double>();

        /// <summary>
        /// 升阶失败次数
        /// </summary>
        [ProtoMember(11)]
        public int _LevelUpFailNum = 0;
    }
}
