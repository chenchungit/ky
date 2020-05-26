using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Goods
{
    public class CondIndex
    {
        public const string Cond_WingSuit = "WingSuit";                                     //判断翅膀阶数和星数是否满足
        public const string Cond_ChengJiuLvl = "ChengJiuLevel";                              //成就等级是否满足
        public const string Cond_JunXianLvl = "JunXianLevel";                                 //军衔等级是否满足(军衔就是声望)
        public const string Cond_ChangeLife = "ZhuanShengLevel";                            // 角色转生等级>=配置时可用
        public const string Cond_RoleLevel = "Level";                                      // 角色等级>=配置时可用
        public const string Cond_VipLvl = "VIP";                                                      //vip等级是否满足
        public const string Cond_HuFuSuit = "HuFuSuit";                                          //当前穿戴的护身符阶数是否满足，最多穿戴一个
        public const string Cond_DaTianShiSuit = "DaTianShiSuit";                        //当天穿戴的大天使武器中，最大阶数是否满足，可以穿戴多个大天使武器

        public const string Cond_EquipSuit = "EquipSuit";                                    //当前操作的装备阶数
        public const string Cond_EquipForgeLvl = "QiangHuaLevel";                     //当前强化装备的强化等级之和是否满足 
        public const string Cond_EquipAppendLvl = "ZhuiJiaLevel";                      //当前追加装备的追加等级之和是否满足 
        public const string Cond_NeedMarry = "NeedMarry";                               //[bing] 需要结婚 参数0 == 需要未结婚才可使用 1 == 需要结婚后才可使用
        public const string Cond_NeedTask = "NeedTask";                                   // 需要完成指定任务才能使用

        // 以前的限制类型, 现在只有3种生效
        public const string cond_YuanBaoMoreThan = "UseYuanBao";                    //至少多少元宝
        public const string Cond_CanNotBeyondLevel = "CanNotBeyondLevel";       //等级限制
        public const string Cond_NotSafeRegion = "FEIANQUANQU";                     //非安全区
        public const string Cond_NeedOpen = "NeedOpen";                             //需要开启某项功能
    }

    
    public class CondMultiLang
    {
        public const string Wrapper =  "需要{0}才能使用";

        // 以下内容翻译好后，会填充到上面的wrapper中
        public const string WingSuit = "翅膀阶数达到{0}";
        public const string ChengJiu = "成就称号达到{0}";
        public const string JunXian = "军衔达到{0}";
        public const string ZhuanSheng = "转生等级达到{0}";
        public const string RoleLevel = "角色等级达到{0}";
        public const string VipLevel = "VIP等级达到{0}";
        public const string HuShenFu = "佩戴{0}阶护身符";
        public const string DaTianShi = "佩戴{0}阶大天使武器";
        public const string strNeedMarry = "需要结婚后才可以使用";
        public const string strNotMarry = "未结婚才可以使用";
        public const string strNeedTask = "完成任务 {0} ";

        public const string YuanBao = "使用{0}元宝";
        public const string CannotBeyongLvl = "您已超过物品最大使用等级";
        public const string NotSafeRegion = "非安全区{0}";
    }

    public interface ICondJudger
    {
        // 判断是否满足条件
        bool Judge(GameClient client, string arg, out string failedMsg);
    }
}
