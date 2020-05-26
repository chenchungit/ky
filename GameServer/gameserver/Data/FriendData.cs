using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 朋友数据
    /// </summary>
    [ProtoContract]    
    public class FriendData
    {
        /// <summary>
        /// 数据库流水ID
        /// </summary>
        [ProtoMember(1)]        
        public int DbID;

        /// <summary>
        /// 对方的ID
        /// </summary>
        [ProtoMember(2)]           
        public int OtherRoleID;

        /// <summary>
        /// 对方的名称
        /// </summary>
        [ProtoMember(3)]
        public string OtherRoleName;

        /// <summary>
        /// 对方的等级
        /// </summary>
        [ProtoMember(4)]
        public int OtherLevel;

        /// <summary>
        /// 对方的职业
        /// </summary>
        [ProtoMember(5)]
        public int Occupation;

        /// <summary>
        /// 对方的在线状态
        /// </summary>
        [ProtoMember(6)]
        public int OnlineState;

        /// <summary>
        /// 所在的地图编号
        /// </summary>
        [ProtoMember(7)]
        public string Position;

        /// <summary>
        /// 朋友数据类型, 0: 好友 1:黑名单 2: 敌人
        /// </summary>
        [ProtoMember(8)]           
        public int FriendType;

        // MU 新增字段 朋友的转生级别 [1/10/2014 LiaoWei]
        /// <summary>
        /// 朋友转生级别
        /// </summary>
        [ProtoMember(9)]
        public int FriendChangeLifeLev;

        /// <summary>
        /// 朋友战斗力 MU 新增字段 [3/15/2014 LiaoWei] 
        /// </summary>
        [ProtoMember(10)]
        public int FriendCombatForce;

        /// <summary>
        /// 配偶id, >0表示有配偶
        /// </summary>
        [ProtoMember(11)]
        public int SpouseId;
    }
}
