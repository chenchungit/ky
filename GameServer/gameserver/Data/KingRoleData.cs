using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    enum KingRoleType
    {
        PKKing = 1,
        LuoLanKing = 2,
    }

    [ProtoContract]
    public class KingRoleGetData
    {
        [ProtoMember(1)]
        public int KingType;
    }

    [ProtoContract]
    public class KingRolePutData
    {
        [ProtoMember(1)]
        public int KingType;

        [ProtoMember(2)]
        public RoleDataEx RoleDataEx;
    }
}
