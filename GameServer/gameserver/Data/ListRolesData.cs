using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 列举角色的数据
    /// </summary>
    [ProtoContract]
    public class ListRolesData
    {
        /// <summary>
        /// 搜索的开始索引
        /// </summary>
        [ProtoMember(1)]
        public int StartIndex = 0;

        /// <summary>
        /// 当前总的条数
        /// </summary>
        [ProtoMember(2)]
        public int TotalRolesCount = 0;

        /// <summary>
        /// 每页总的条数
        /// </summary>
        [ProtoMember(3)]
        public int PageRolesCount = 0;

        /// <summary>
        /// 返回的用户角色列表
        /// </summary>
        [ProtoMember(4)]
        public List<SearchRoleData> SearchRoleDataList = null;
    }
}
