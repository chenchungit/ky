using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data.OldCopyTeam
{
    /// <summary>
    /// 搜索队伍的数据
    /// </summary>
    [ProtoContract]
    public class CopySearchTeamData
    {
        public CopySearchTeamData SimpleClone()
        {
            CopySearchTeamData simple = new CopySearchTeamData();
            simple.PageTeamsCount = PageTeamsCount;
            simple.StartIndex = StartIndex;
            simple.TotalTeamsCount = TotalTeamsCount;

            if (null != TeamDataList)
            {
                simple.TeamDataList = new List<CopyTeamData>();
                foreach (var item in TeamDataList)
                {
                    simple.TeamDataList.Add(item.SimpleClone());
                }
            }

            return simple;
        }

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
        public List<CopyTeamData> TeamDataList = null;
    }
}
