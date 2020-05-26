using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 角色伤害信息
    /// </summary>
    [ProtoContract]
    public class RoleDamage : IComparable<RoleDamage>
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID;

        /// <summary>
        /// 累积输出伤害
        /// </summary>
        [ProtoMember(2)]
        public long Damage;

        /// <summary>
        /// 角色名,服务器主动推送时,不填写这个值(保持为null)
        /// 若客户端不知道角色名,需主动请求伤害列表.
        /// </summary>
        [ProtoMember(3)]
        public string RoleName;

        /// <summary>
        /// 附加标记
        /// </summary>
        [ProtoMember(4)]
        public List<int> FlagList;

        /// <summary>
        /// ProtoBuff库要求有默认构造函数
        /// </summary>
        public RoleDamage() { }

        public RoleDamage(int roleID, long damage, string roleName = null, params int[] param)
        {
            RoleID = roleID;
            Damage = damage;
            RoleName = roleName;
            if (null != param && param.Length > 0)
            {
                FlagList = param.ToList();
            }
        }

        /// <summary>
        /// 排序比较函数,按伤害排序
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int CompareTo(RoleDamage right)
        {
            long ret = Damage - right.Damage;
            if (ret > 0)
            {
                return 1;
            }
            else if (ret == 0)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }
}
