using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProtoBuf;
using Server.Data;
using KF.Contract.Data;

namespace GameServer.Logic.Marriage.CoupleWish
{
    /// <summary>
    /// 情侣祝福 --- 情侣
    /// </summary>
    [ProtoContract]
    public class CoupleWishCoupleData
    {
        /// <summary>
        /// 跨服中心生成，唯一组合id，每对夫妻每周不是同一个值
        /// </summary>
        [ProtoMember(1)]
        public int DbCoupleId;

        /// <summary>
        /// 丈夫形象
        /// </summary>
        [ProtoMember(2)]
        public KuaFuRoleMiniData Man;

        /// <summary>
        /// 形象数据
        /// </summary>
        [ProtoMember(3)]
        public RoleData4Selector ManSelector;

        /// <summary>
        /// 妻子形象
        /// </summary>
        [ProtoMember(4)]
        public KuaFuRoleMiniData Wife;

        /// <summary>
        /// 形象数据
        /// </summary>
        [ProtoMember(5)]
        public RoleData4Selector WifeSelector;

        /// <summary>
        /// 被祝福次数
        /// </summary>
        [ProtoMember(6)]
        public int BeWishedNum;

        /// <summary>
        /// 排名
        /// </summary>
        [ProtoMember(7)]
        public int Rank;
    }

    /// <summary>
    /// 情侣祝福 --- 主界面信息
    /// </summary>
    [ProtoContract]
    public class CoupleWishMainData
    {
        /// <summary>
        /// 排行榜数据
        /// </summary>
        [ProtoMember(1)]
        public List<CoupleWishCoupleData> RankList;

        /// <summary>
        /// 我的情侣排行
        /// 0 表示无
        /// </summary>
        [ProtoMember(2)]
        public int MyCoupleRank;

        /// <summary>
        /// 我的情侣被祝福数
        /// </summary>
        [ProtoMember(3)]
        public int MyCoupleBeWishNum;

        /// <summary>
        /// 可领取的奖励id, 参考WishAward.xml
        /// </summary>
        [ProtoMember(4)]
        public int CanGetAwardId;

        /// <summary>
        /// 本情侣的丈夫数据
        /// </summary>
        [ProtoMember(5)]
        public RoleData4Selector MyCoupleManSelector;

        /// <summary>
        /// 本情侣的妻子数据
        /// </summary>
        [ProtoMember(6)]
        public RoleData4Selector MyCoupleWifeSelector;
    }

    /// <summary>
    /// 情侣祝福 --- 请求祝福
    /// </summary>
    [ProtoContract]
    public class CoupleWishWishReqData
    {
        public enum ECostType
        {
            Goods = 1,
            ZuanShi = 2,
        }

        /// <summary>
        /// 是否是赠送排行榜中的数据
        /// 本字段必须正确，如果是本服赠送，那么服务器将要更新目标角色的形象数据
        /// true: 赠送排行榜中的角色
        /// false: 赠送本服中的角色
        /// </summary>
        [ProtoMember(1)]
        public bool IsWishRankRole;

        /// <summary>
        /// 当IsWishRankRole == true时，使用此字段，表示赠送跨服排行榜上的情侣，
        /// 这个字段表示被祝福情侣的CoupleWishCoupleData.DbCoupleId;
        /// </summary>
        [ProtoMember(2)]
        public int ToRankCoupleId;

        /// <summary>
        /// 当IsWishRankRole == false时，使用此字段，表示赠送本服的情侣，
        /// 这个字段表示被赠送情侣中某一方(丈夫或妻子)的角色名
        /// </summary>
        [ProtoMember(3)]
        public string ToLocalRoleName;

        /// <summary>
        /// 祝福类型，参考WishType.xml的ID字段
        /// </summary>
        [ProtoMember(4)]
        public int WishType;

        /// <summary>
        /// 祝福寄语
        /// </summary>
        [ProtoMember(5)]
        public string WishTxt;

        /// <summary>
        /// 消耗类型，1=道具，2=钻石
        /// </summary>
        [ProtoMember(6)]
        public int CostType;
    }

    /// <summary>
    /// 广播给客户端祝福特效信息
    /// </summary>
    [ProtoContract]
    public class CoupleWishNtfWishEffectData
    {
        /// <summary>
        /// 祝福者
        /// </summary>
        [ProtoMember(1)]
        public KuaFuRoleMiniData From;

        /// <summary>
        /// 被祝福者
        /// </summary>
        [ProtoMember(2)]
        public List<KuaFuRoleMiniData> To;

        /// <summary>
        /// 祝福类型，参考WishType.xml的ID字段
        /// </summary>
        [ProtoMember(3)]
        public int WishType;

        /// <summary>
        /// 祝福寄语
        /// </summary>
        [ProtoMember(4)]
        public string WishTxt;

        /// <summary>
        /// 我得到的绑金
        /// </summary>
        [ProtoMember(5)]
        public int GetBinJinBi;

        /// <summary>
        /// 我得到的绑钻
        /// </summary>
        [ProtoMember(6)]
        public int GetBindZuanShi;

        /// <summary>
        /// 我得到的经验
        /// </summary>
        [ProtoMember(7)]
        public int GetExp;
    }

    /// <summary>
    /// 情侣祝福榜雕像膜拜数据
    /// </summary>
    [ProtoContract]
    public class CoupleWishTop1AdmireData
    {
        /// <summary>
        /// 情侣唯一组合id
        /// </summary>
        [ProtoMember(1, IsRequired=true)]
        public int DbCoupleId;

        /// <summary>
        /// 丈夫形象
        /// </summary>
        [ProtoMember(2)]
        public RoleData4Selector ManSelector;

        /// <summary>
        /// 妻子形象
        /// </summary>
        [ProtoMember(3)]
        public RoleData4Selector WifeSelector;

        /// <summary>
        /// 总共被膜拜次数
        /// </summary>
        [ProtoMember(4)]
        public int BeAdmireCount;

        /// <summary>
        /// 我膜拜了几次
        /// </summary>
        [ProtoMember(5)]
        public int MyAdmireCount;
    }

    /// <summary>
    /// 情侣祝福榜 --- 宴会数据
    /// </summary>
    [ProtoContract]
    public class CoupleWishYanHuiData
    {
        /// <summary>
        /// 丈夫
        /// </summary>
        [ProtoMember(1)]
        public KuaFuRoleMiniData Man;

        /// <summary>
        /// 妻子
        /// </summary>
        [ProtoMember(2)]
        public KuaFuRoleMiniData Wife;

        /// <summary>
        /// 总共参加次数
        /// </summary>
        [ProtoMember(3, IsRequired=true)]
        public int TotalJoinNum;

        /// <summary>
        /// 我已经参加次数
        /// </summary>
        [ProtoMember(4, IsRequired = true)]
        public int MyJoinNum;

        /// <summary>
        /// 宴会表示的情侣组合唯一id
        /// </summary>
        [ProtoMember(5, IsRequired = true)]
        public int DbCoupleId;
    }
}
