using System;
using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Shapes;
using System.Collections.Generic;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// 精简的角色数据（主要用于通知角色用）
    /// </summary>
    [ProtoContract]
    public class RoleDataMini
    {
        /// <summary>
        /// 当前的角色ID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID = 0;

        /// <summary>
        /// 当前的角色ID
        /// </summary>
        [ProtoMember(2)]
        public string RoleName = "";

        /// <summary>
        /// 当前角色的性别
        /// </summary>
        [ProtoMember(3)]
        public int RoleSex = 0;

        /// <summary>
        /// 角色职业
        /// </summary>
        [ProtoMember(4)]
        public int Occupation = 0;

        /// <summary>
        /// 角色级别
        /// </summary>
        [ProtoMember(5)]
        public int Level = 1;

        /// <summary>
        /// 角色所属的帮派
        /// </summary>
        [ProtoMember(6)]
        public int Faction = 0;

        /// <summary>
        /// 当前的PK模式
        /// </summary>
        [ProtoMember(7)]
        public int PKMode = 0;

        /// <summary>
        /// 当前的PK值
        /// </summary>
        [ProtoMember(8)]
        public int PKValue = 0;

        /// <summary>
        /// 所在的地图的编号
        /// </summary>
        [ProtoMember(9)]
        public int MapCode = 0;

        /// <summary>
        /// 当前所在的位置X坐标
        /// </summary>
        [ProtoMember(10)]
        public int PosX = 0;

        /// <summary>
        /// 当前所在的位置Y坐标
        /// </summary>
        [ProtoMember(11)]
        public int PosY = 0;

        /// <summary>
        /// 当前的方向
        /// </summary>
        [ProtoMember(12)]
        public int RoleDirection = 0;

        /// <summary>
        /// 当前的生命值
        /// </summary>
        [ProtoMember(13)]
        public int LifeV = 0;

        /// <summary>
        /// 最大的生命值
        /// </summary>
        [ProtoMember(14)]
        public int MaxLifeV = 0;

        /// <summary>
        /// 当前的魔法值
        /// </summary>
        [ProtoMember(15)]
        public int MagicV = 0;

        /// <summary>
        /// 最大的魔法值
        /// </summary>
        [ProtoMember(16)]
        public int MaxMagicV = 0;

        /// <summary>
        /// 衣服代号
        /// </summary>
        [ProtoMember(17)]
        public int BodyCode;

        /// <summary>
        /// 武器代号
        /// </summary>
        [ProtoMember(18)]
        public int WeaponCode;

        /// <summary>
        /// 称号
        /// </summary>
        [ProtoMember(19)]
        public string OtherName;

        /// <summary>
        /// 组队的ID
        /// </summary>
        [ProtoMember(20)]
        public int TeamID;

        /// <summary>
        /// 当前的组队中的队长ID
        /// </summary>        
        [ProtoMember(21)]
        public int TeamLeaderRoleID = 0;

        /// <summary>
        /// 当前的PK点
        /// </summary>
        [ProtoMember(22)]
        public int PKPoint = 0;

        /// <summary>
        /// 紫名的开始时间
        /// </summary>
        [ProtoMember(23)]
        public long StartPurpleNameTicks = 0;

        /// <summary>
        /// 角斗场荣誉称号开始时间
        /// </summary>
        [ProtoMember(24)]
        public long BattleNameStart = 0;

        /// <summary>
        /// 角斗场荣誉称号
        /// </summary>
        [ProtoMember(25)]
        public int BattleNameIndex = 0;

        /// <summary>
        /// 区ID
        /// </summary>
        [ProtoMember(26)]
        public int ZoneID = 0;

        /// <summary>
        /// 战盟名称
        /// </summary>
        [ProtoMember(27)]
        public string BHName = "";

        /// <summary>
        /// 被邀请加入战盟时是否验证
        /// </summary>
        [ProtoMember(28)]
        public int BHVerify = 0;

        /// <summary>
        /// 战盟职务
        /// </summary>
        [ProtoMember(29)]
        public int BHZhiWu = 0;

        /// <summary>
        /// 法师的护盾开始的时间
        /// </summary>
        [ProtoMember(30)]
        public long FSHuDunStart = 0;

        /// <summary>
        /// 大乱斗中的阵营ID
        /// </summary>
        [ProtoMember(31)]
        public int BattleWhichSide = -1;

        /// <summary>
        /// 上次的mailID
        /// </summary>
        [ProtoMember(32)]
        public int IsVIP = 0;

        /// <summary>
        /// 道术隐身的时间
        /// </summary>
        [ProtoMember(33)]
        public long DSHideStart = 0;

        /// <summary>
        /// 角色常用整形参数值列表
        /// </summary>
        [ProtoMember(34)]
        public List<int> RoleCommonUseIntPamams = new List<int>();

        /// <summary>
        /// 法师的护盾持续的秒数
        /// </summary>
        [ProtoMember(35)]
        public int FSHuDunSeconds = 0;

        /// <summary>
        /// 中毒开始的时间
        /// </summary>
        [ProtoMember(36)]
        public long ZhongDuStart = 0;

        /// <summary>
        /// 中毒持续的秒数
        /// </summary>
        [ProtoMember(37)]
        public int ZhongDuSeconds = 0;

        /// <summary>
        /// 节日称号
        /// </summary>
        [ProtoMember(38)]
        public int JieriChengHao = 0;

        /// <summary>
        /// 冻结开始的时间
        /// </summary>
        [ProtoMember(39)]
        public long DongJieStart = 0;

        /// <summary>
        /// 冻结持续的秒数
        /// </summary>
        [ProtoMember(40)]
        public int DongJieSeconds = 0;

        /// <summary>
        /// 物品数据
        /// </summary>
        [ProtoMember(41)]
        public List<GoodsData> GoodsDataList;

        // MU 增加 [1/21/2014 LiaoWei]
        /// <summary>
        /// 转生级别
        /// </summary>
        [ProtoMember(42)]
        public int ChangeLifeLev;

        // 转生计数 [10/17/2013 LiaoWei]
        [ProtoMember(43)]
        public int ChangeLifeCount = 0;

        /// <summary>
        /// 摆摊的名称
        /// </summary>
        [ProtoMember(44)]
        public string StallName;

        /// <summary>
        /// Buffer Mini数据 [4/10/2014 LiaoWei]
        /// </summary>
        [ProtoMember(45)]
        public List<BufferDataMini> BufferMiniInfo;

        /// <summary>
        /// 翅膀数据列表
        /// </summary>
        [ProtoMember(46)]
        public WingData MyWingData = null;

        /// <summary>
        /// VIP等级
        /// </summary>
        [ProtoMember(47)]
        public int VIPLevel = 0;

        /// <summary>
        /// 是否gm
        /// </summary>
        [ProtoMember(48)]
        public int GMAuth = 0;

        /// <summary>
        /// 二态功能设置，参考ESettingBitFlag
        /// </summary>
        [ProtoMember(49)]
        public long SettingBitFlags;

        /// <summary>
        /// 配偶id, >0 表示有
        /// </summary>
        [ProtoMember(50)]
        public int SpouseId;
    }
}
