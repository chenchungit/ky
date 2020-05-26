using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 帮会管理成员数据
    /// </summary>
    [ProtoContract]
    public class BangHuiMgrItemData
    {
        /// <summary>
        /// 区ID
        /// </summary>
        [ProtoMember(1)]
        public int ZoneID = 0;

        /// <summary>
        /// 角色的ID
        /// </summary>
        [ProtoMember(2)]
        public int RoleID = 0;

        /// <summary>
        /// 角色的名称
        /// </summary>
        [ProtoMember(3)]
        public string RoleName = "";

        /// <summary>
        /// 角色的职业
        /// </summary>
        [ProtoMember(4)]
        public int Occupation = 0;

        /// <summary>
        /// 帮中职务
        /// </summary>
        [ProtoMember(5)]
        public int BHZhiwu = 0;

        /// <summary>
        /// 帮中称号
        /// </summary>
        [ProtoMember(6)]
        public string ChengHao = "";

        /// <summary>
        /// 帮会公告
        /// </summary>
        [ProtoMember(7)]
        public int BangGong = 0;

        /// <summary>
        /// 角色的级别
        /// </summary>
        [ProtoMember(8)]
        public int Level = 0;
    }

    /// <summary>
    /// 帮会详细数据
    /// </summary>
    [ProtoContract]
    public class BangHuiDetailData
    {
        /// <summary>
        /// 帮派的ID
        /// </summary>
        [ProtoMember(1)]
        public int BHID = 0;

        /// <summary>
        /// 帮派的名称
        /// </summary>
        [ProtoMember(2)]
        public string BHName = "";

        /// <summary>
        /// 区ID
        /// </summary>
        [ProtoMember(3)]
        public int ZoneID = 0;

        /// <summary>
        /// 帮主的ID
        /// </summary>
        [ProtoMember(4)]
        public int BZRoleID = 0;

        /// <summary>
        /// 帮主的名称
        /// </summary>
        [ProtoMember(5)]
        public string BZRoleName = "";

        /// <summary>
        /// 帮主的职业
        /// </summary>
        [ProtoMember(6)]
        public int BZOccupation = 0;

        /// <summary>
        /// 帮成员总的个数
        /// </summary>
        [ProtoMember(7)]
        public int TotalNum = 0;

        /// <summary>
        /// 帮成员总的级别
        /// </summary>
        [ProtoMember(8)]
        public int TotalLevel = 0;

        /// <summary>
        /// 帮会公告
        /// </summary>
        [ProtoMember(9)]
        public string BHBulletin = "";

        /// <summary>
        /// 建立时间
        /// </summary>
        [ProtoMember(10)]
        public string BuildTime = "";

        /// <summary>
        /// 帮旗名称
        /// </summary>
        [ProtoMember(11)]
        public string QiName = "";

        /// <summary>
        /// 帮成员总的级别
        /// </summary>
        [ProtoMember(12)]
        public int QiLevel = 0;

        /// <summary>
        /// 管理成员列表
        /// </summary>
        [ProtoMember(13)]
        public List<BangHuiMgrItemData> MgrItemList = null;

        /// <summary>
        /// 是否验证
        /// </summary>
        [ProtoMember(14)]
        public int IsVerify = 0;

        // MU 新增 [3/7/2014 LiaoWei]
        /// <summary>
        /// 帮会资金
        /// </summary>
        [ProtoMember(15)]
        public int TotalMoney = 0;

        // MU 新增 [3/7/2014 LiaoWei]
        /// <summary>
        /// 玩家今日获得战功值
        /// </summary>
        [ProtoMember(16)]
        public int TodayZhanGongForGold = 0;

        // MU 新增 [3/7/2014 LiaoWei]
        /// <summary>
        /// 玩家今日获得战功值
        /// </summary>
        [ProtoMember(17)]
        public int TodayZhanGongForDiamond = 0;

        /// <summary>
        /// 祭坛
        /// </summary>
        [ProtoMember(18)]
        public int JiTan = 0;

        /// <summary>
        /// 军械
        /// </summary>
        [ProtoMember(19)]
        public int JunXie = 0;

        /// <summary>
        /// 光环
        /// </summary>
        [ProtoMember(20)]
        public int GuangHuan = 0;

        /// <summary>
        /// 剩余允许改名次数
        /// </summary>
        [ProtoMember(21)]
        public int CanModNameTimes = 0;
    }
}
