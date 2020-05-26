using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Data;

namespace GameServer.Logic.Goods
{
    // 翅膀阶级	WingSuit	翅膀阶数>=配置时可用
    class CondJudger_WingSuit : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && client.ClientData.MyWingData != null && !string.IsNullOrEmpty(arg))
            {
                int iArg = -1;
                if (int.TryParse(arg, out iArg) && client.ClientData.MyWingData.WingID >= iArg)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.WingSuit), arg));
            }

            return bOK;
        }
    }

    // 成就阶数	ChengJiuLevel 成就阶数>=配置时可用
    class CondJudger_ChengJiuLvl : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedLvl = -1;
                if (int.TryParse(arg, out iNeedLvl) && ChengJiuManager.GetChengJiuLevel(client) >= iNeedLvl)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.ChengJiu), arg));
            }

            return bOK;
        }
    }

    // 军衔阶数	JunXianLevel	军衔阶数>=配置时可用
    class CondJudger_JunXianLvl : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedLvl = -1;
                if (int.TryParse(arg, out iNeedLvl) && GameManager.ClientMgr.GetShengWangLevelValue(client) >= iNeedLvl)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.JunXian), arg));
            }

            return bOK;
        }
    }

    // 角色转生等级	ZhuanShengLevel 角色转生等级>=配置时可用
    class CondJudger_ChangeLife : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedChangeLife = -1;
                if (int.TryParse(arg, out iNeedChangeLife) && client.ClientData.ChangeLifeCount >= iNeedChangeLife)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                     string.Format(Global.GetLang(CondMultiLang.ZhuanSheng), arg));
            }

            return bOK;
        }
    }

    // 角色等级	Level	角色等级>=配置时可用
    class CondJudger_RoleLevel : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedLvl = -1;
                if (int.TryParse(arg, out iNeedLvl) && client.ClientData.Level >= iNeedLvl)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.RoleLevel), arg));
            }

            return bOK;
        }
    }

    // VIP等级	VIP VIP等级>=配置时可用
    public class CondJudger_VIPLvl : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedLvl = -1;
                if (int.TryParse(arg, out iNeedLvl) && client.ClientData.VipLevel >= iNeedLvl)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.VipLevel), arg));
            }

            return bOK;
        }
    }

    // 护身符阶数	HuFuSuit	护身符阶数	Categoriy="22"的物品SuitID>=配置时可用
    public class CondJudger_HuFuSuit : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                GoodsData usingHuFu = client.UsingEquipMgr.GetGoodsDataByCategoriy(client, (int)ItemCategories.HuFu);
                if (usingHuFu != null)
                {
                    int iNeedSuit = -1;
                    if (int.TryParse(arg, out iNeedSuit) && Global.GetEquipGoodsSuitID(usingHuFu.GoodsID) >= iNeedSuit)
                    {
                        bOK = true;
                    }
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.HuShenFu), arg));
            }

            return bOK;
        }
    }

    // 判断大天使武器阶数是否满足，如果同时佩戴多把，那么取最大的
    public class CondJudger_DaTianShiSuit : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedSuit = int.MaxValue;
                if (int.TryParse(arg, out iNeedSuit) && client.UsingEquipMgr.GetUsingEquipArchangelWeaponSuit() >= iNeedSuit)
                    bOK = true;
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.DaTianShi), arg));
            }
            return bOK;
        }
    }

    // 判断结婚增加奉献值道具是否满足
    public class CondJudger_NeedMarry : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                //0 == 需未婚使用 1 == 需已婚使用
                if ("1" == arg && client.ClientData.MyMarriageData != null && client.ClientData.MyMarriageData.byMarrytype != -1)
                    bOK = true;
                else if ("0" == arg && (client.ClientData.MyMarriageData == null || client.ClientData.MyMarriageData.byMarrytype == -1))
                    bOK = true;
            }

            if (!bOK)
            {
                if ("1" == arg)
                    failedMsg = Global.GetLang(CondMultiLang.strNeedMarry);
                else
                    failedMsg = Global.GetLang(CondMultiLang.strNotMarry);
            }
            return bOK;
        }
    }

    // 当前装备的追加等级之和是否满足
    class CondJudger_EquipAppendLvl : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedLvl = int.MaxValue;
                if (int.TryParse(arg, out iNeedLvl) && client.ClientData._ReplaceExtArg.CurrEquipZhuiJiaLevel >= iNeedLvl)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                // 这个只在替换类使用，并没有多语言处理
                failedMsg = string.Format("当前操作的装备的追加等级不能低于{0}", arg);
            }

            return bOK;
        }
    }

    // 当前装备的强化等级之和是否满足
    class CondJudger_EquipForgeLvl : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedLvl = int.MaxValue;
                if (int.TryParse(arg, out iNeedLvl) && client.ClientData._ReplaceExtArg.CurrEquipQiangHuaLevel >= iNeedLvl)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                // 这个只在替换类使用，并没有多语言处理
                failedMsg = string.Format("当前装备的强化等级不能低于{0}", arg);
            }

            return bOK;
        }
    }

    // 当前装备的强化等级之和是否满足
    class CondJudger_EquipSuit : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedSuit = int.MaxValue;
                if (int.TryParse(arg, out iNeedSuit) && client.ClientData._ReplaceExtArg.CurrEquipSuit >= iNeedSuit)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                // 这个只在替换类使用，并没有多语言处理
                failedMsg = string.Format("当前装备的品阶不能低于{0}", arg);
            }

            return bOK;
        }
    }

    // 元宝数量必须满足
    class CondJudger_YuanBaoMoreThan : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                int iNeedYB = int.MaxValue;
                if (int.TryParse(arg, out iNeedYB) && client.ClientData.UserMoney >= iNeedYB)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.YuanBao), arg));
            }

            return bOK;
        }
    }

    class CondJudger_NeedOpen : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";
            bool bOK = true;
            var gongnengId = (GongNengIDs)Convert.ToInt32(arg);

            if (!GlobalNew.IsGongNengOpened(client, gongnengId))
            {
                bOK = false;
            }

            if (!bOK)
            {
                failedMsg = string.Format("物品对应的功能没有开启");
            }

            return bOK;
        }
    }

    /// 判断是否超出使用等级上限
    public class CondJudger_CannotBeyongLevel : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            if (client != null && !string.IsNullOrEmpty(arg))
            {
                string[] fields = arg.Split('|');
                if (fields.Length == 2)
                {
                    int maxChangeLife = -1;
                    int maxLvl = -1;
                    if (int.TryParse(fields[0], out maxChangeLife) && int.TryParse(fields[1], out maxLvl))
                    {
                        if (client.ClientData.ChangeLifeCount < maxChangeLife
                            || (client.ClientData.ChangeLifeCount == maxChangeLife && client.ClientData.Level <= maxLvl))
                        {
                            bOK = true;
                        }
                    }
                }
            }

            if (!bOK)
            {
                failedMsg = Global.GetLang(CondMultiLang.CannotBeyongLvl);
            }
            return bOK;
        }
    }


    // 判断是否是非安全区
    public class CondJudger_NotSafeRegion : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;

            if (client != null)
            {
                GameMap gameMap = null;
                if (GameManager.MapMgr.DictMaps.TryGetValue(client.ClientData.MapCode, out gameMap)
                    && gameMap != null)
                {
                    bOK = !gameMap.InSafeRegionList(client.CurrentGrid);
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.NotSafeRegion), ""));
            }

            return bOK;
        }
    }

    // 需要完成指定任务
    public class CondJudger_NeedTask : ICondJudger
    {
        public bool Judge(GameClient client, string arg, out string failedMsg)
        {
            failedMsg = "";

            bool bOK = false;
            int needTaskId = Global.SafeConvertToInt32(arg);
            if (client != null)
            {
                if (client.ClientData.MainTaskID >= needTaskId)
                {
                    bOK = true;
                }
            }

            if (!bOK)
            {
                failedMsg = string.Format(Global.GetLang(CondMultiLang.Wrapper),
                    string.Format(Global.GetLang(CondMultiLang.strNeedTask), GlobalNew.GetTaskName(needTaskId)));
            }

            return bOK;
        }
    }
}