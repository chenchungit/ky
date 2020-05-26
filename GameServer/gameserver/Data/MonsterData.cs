using System;
using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Shapes;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 系统生成的怪的数据定义
    /// </summary>
    [ProtoContract]
    public class MonsterData
    {
        /// <summary>
        /// 当前的角色ID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID = 0;

        /// <summary>
        /// 当前的角色ID
        /// </summary>
        [ProtoMember(2)]
        public string RoleName = "";

        /// <summary>
        /// 当前角色的性别
        /// </summary>
        [ProtoMember(3)]
        public int RoleSex = 0;

        /// <summary>
        /// 角色级别
        /// </summary>
        [ProtoMember(4)]
        public int Level = 1;

        /// <summary>
        /// 当前的经验
        /// </summary>
        [ProtoMember(5)]
        public int Experience = 0;

        /// 当前所在的位置X坐标
        /// </summary>
        [ProtoMember(6)]
        public int PosX = 0;

        /// <summary>
        /// 当前所在的位置Y坐标
        /// </summary>
        [ProtoMember(7)]
        public int PosY = 0;

        /// <summary>
        /// 当前的方向
        /// </summary>
        [ProtoMember(8)]
        public int RoleDirection = 0;

        /// <summary>
        /// 当前的生命值
        /// </summary>
        [ProtoMember(9)]
        public double LifeV = 0;

        /// <summary>
        /// 当前的生命值
        /// </summary>
        [ProtoMember(10)]
        public double MaxLifeV = 0;

        /// <summary>
        /// 当前的魔法值
        /// </summary>
        [ProtoMember(11)]
        public double MagicV = 0;

        /// <summary>
        /// 当前的魔法值
        /// </summary>
        [ProtoMember(12)]
        public double MaxMagicV = 0;

        /// <summary>
        /// 获取或设置精灵当前衣服代码
        /// </summary>
        [ProtoMember(13)]
        public int EquipmentBody = 0;

        /// <summary>
        /// 扩展ID
        /// </summary>
        [ProtoMember(14)]
        public int ExtensionID = 0;

        /// <summary>
        /// 怪物的阶级
        /// </summary>
        [ProtoMember(15)]
        public int MonsterType = 0;

        /// <summary>
        /// 怪物主人的角色ID 必须是玩家角色
        /// </summary>
        [ProtoMember(16)]
        public int MasterRoleID = 0;

        /// <summary>
        /// 宠物怪的ai类型 默认1 自由攻击 只对道士的宠物怪才有用
        /// </summary>
        [ProtoMember(17)]
        public UInt16 AiControlType = 1;

        /// <summary>
        /// 宠物怪的ai类型 默认1 自由攻击 只对道士的宠物怪才有用
        /// </summary>
        [ProtoMember(18)]
        public string AnimalSound = "";

        /// <summary>
        /// 怪物的级别
        /// </summary>
        [ProtoMember(19)]
        public int MonsterLevel = 0;

        /// <summary>
        /// 中毒开始的时间
        /// </summary>
        [ProtoMember(20)]
        public long ZhongDuStart = 0;

        /// <summary>
        /// 中毒持续的秒数
        /// </summary>
        [ProtoMember(21)]
        public int ZhongDuSeconds = 0;

        /// <summary>
        /// MU昏迷开始时间 [5/7/2014 LiaoWei]
        /// </summary>
        [ProtoMember(22)]
        public long FaintStart = 0;

        /// <summary>
        /// MU昏迷持续的秒数 [5/7/2014 LiaoWei]
        /// </summary>
        [ProtoMember(23)]
        public int FaintSeconds = 0;

        /// <summary>
        /// 所属阵营
        /// </summary>
        [ProtoMember(24)]
        public int BattleWitchSide;
    }
}
