using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    // 每个角色的改名信息
    [ProtoContract]
    public class EachRoleChangeName
    {
        // 角色Id
        [ProtoMember(1)]
        public int RoleId = 0;

        // 剩余免费改名次数
        [ProtoMember(2)]
        public int LeftFreeTimes = 0;

        // 已经使用钻石改名的次数
        [ProtoMember(3)]
        public int AlreadyZuanShiTimes = 0;
    }

    // 账号下各角色的改名情况，CMD_ROLE_LIST的时候，服务器主动推送给客户端(CMD_NTF_EACH_ROLE_ALLOW_CHANGE_NAME)
    // 服务器保证该消息先与CMD_ROLE_LIST发送，所以客户端收到CMD_ROLE_LIST的时候，一定收到了该消息
    [ProtoContract]
    public class ChangeNameInfo
    {
        // 钻石，所有角色通用
        [ProtoMember(1)]
        public int ZuanShi = 0;

        // 每个角色的改名信息
        [ProtoMember(2)]
        public List<EachRoleChangeName> RoleList = new List<EachRoleChangeName>();
    }

    // 改名结果
    [ProtoContract]
    public class ChangeNameResult
    {
        // 错误代码，对应ChangeNameError
        [ProtoMember(1)]
        public int ErrCode;

        // 服务器id，原样返回客户端参数
        [ProtoMember(2)]
        public int ZoneId;

        // 新名字，原样返回客户端参数
        [ProtoMember(3)]
        public string NewName;

        // 每个角色的允许改名情况，服务器保证该数据总是正确的
        [ProtoMember(4)]
        public ChangeNameInfo NameInfo = new ChangeNameInfo();
    }
}