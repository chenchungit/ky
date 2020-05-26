using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 副本数据
    /// </summary>
    [ProtoContract]
    public class FuBenHistData
    {
        /// <summary>
        /// 副本的ID
        /// </summary>
        [ProtoMember(1)]
        public int FuBenID = 0;

        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(2)]
        public int RoleID = 0;

        /// <summary>
        /// 角色名称
        /// </summary>
        [ProtoMember(3)]
        public string RoleName = "";

        /// <summary>
        /// 通关的记录(秒)
        /// </summary>
        [ProtoMember(4)]
        public int UsedSecs = 0;
    }
}
