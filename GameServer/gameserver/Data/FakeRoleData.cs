using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 假人数据
    /// </summary>
    [ProtoContract]
    public class FakeRoleData
    {
        /// <summary>
        /// 假人的ID
        /// </summary>
        [ProtoMember(1)]
        public int FakeRoleID = 0;

        /// <summary>
        /// 假人的类型
        /// </summary>
        [ProtoMember(2)]
        public int FakeRoleType = 0;

        /// <summary>
        /// 映射的其他精灵的ID
        /// </summary>
        [ProtoMember(3)]
        public int ToExtensionID = 0;

        /// <summary>
        /// 假人对应的mini角色数据
        /// </summary>
        [ProtoMember(4)]
        public RoleDataMini MyRoleDataMini = null;
    }
}
