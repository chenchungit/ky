using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace GameServer.Logic.BangHui.ZhanMengShiJian
{
    /// <summary>
    /// 战盟事件数据
    /// 
    /// 战盟创建：用户名称（蓝色）+创建了战盟
    /// RoleName + "创建了战盟"
    /// 
    /// 脱离战盟：用户名称（蓝色）+离开了战盟
    /// RoleName + "离开了战盟"
    /// 
    /// 加入战盟：用户名称（蓝色）+加入了战盟
    /// RoleName + "加入了战盟"
    /// 
    /// 玩家捐赠：用户名称（蓝色）+捐赠了+捐赠值+捐赠类型（钻石/金币）+ 获得了 + 战功值
    /// RoleName + "捐赠了" + SubValue1(捐赠值) + SubValue2（捐赠类型）+ SubValue3(获得战功值)
    /// 
    /// 职位变更：用户名称（蓝色）+成为了+职位名称
    /// RoleName + "成为了" + SubValue1(职位ID)
    /// 
    /// 建设升级：用户名称（蓝色）+将+建筑名称+等级提升到 + 提升后等级 + 级
    /// RoleName + "将" + SubValue1(建筑ID) + "等级提升到" + SubValue2（等级） + "级"
    /// 
    /// 帮会改名：用户名称（蓝色）+将战盟改名为+战盟名称
    /// RoleName + "将战盟改名为"+SubSzValue1
    /// </summary>
    [ProtoContract]
    public class ZhanMengShiJianData
    {

        /// <summary>
        /// 主键ID
        /// </summary>
        public int PKId = 0;

        /// <summary>
        /// 帮派的ID
        /// </summary>
        [ProtoMember(1)]
        public int BHID = 0;

        /// <summary>
        /// 事件类型
        /// </summary>
        [ProtoMember(2)]
        public int ShiJianType = 0;

        /// <summary>
        /// 用户名称
        /// </summary>
        [ProtoMember(3)]
        public string RoleName = "";

        /// <summary>
        /// 触发事件时间
        /// </summary>
        [ProtoMember(4)]
        public string CreateTime = "";

        /// <summary>
        /// 预留值
        /// </summary>
        [ProtoMember(5)]
        public int SubValue1 = -1;

        /// <summary>
        /// 预留值
        /// </summary>
        [ProtoMember(6)]
        public int SubValue2 = -1;

        /// <summary>
        /// 预留值
        /// </summary>
        [ProtoMember(7)]
        public int SubValue3 = -1;

        /// <summary>
        /// 预留字符串值1
        /// </summary>
        [ProtoMember(8)]
        public string SubSzValue1 = "";

    }
}
