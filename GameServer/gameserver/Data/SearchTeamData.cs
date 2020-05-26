using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 搜索队伍的数据
    /// </summary>
    [ProtoContract]
    public class SearchTeamData
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
        public int TotalTeamsCount = 0;

        /// <summary>
        /// 每页总的条数
        /// </summary>
        [ProtoMember(3)]
        public int PageTeamsCount = 0;

        /// <summary>
        /// 返回的队伍的列表
        /// </summary>
        [ProtoMember(4)]
        public List<TeamData> TeamDataList = null;
    }
}
