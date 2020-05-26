using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Tools.Pattern;

namespace GameServer.Logic.Goods
{
    /// <summary>
    /// 限制类物品管理器
    /// </summary>
    public class GoodsCanUseManager : SingletonTemplate<GoodsCanUseManager>
    {
        private GoodsCanUseManager() {  }

        private Dictionary<string, ICondJudger> canUseDict = new Dictionary<string, ICondJudger>();

        public void Init()
        {
            // 翅膀阶数是否满足
            canUseDict[CondIndex.Cond_WingSuit.ToLower()] = new CondJudger_WingSuit();
            // 成就等级是否满足
            canUseDict[CondIndex.Cond_ChengJiuLvl.ToLower()] = new CondJudger_ChengJiuLvl();
            // 军衔等级是否满足
            canUseDict[CondIndex.Cond_JunXianLvl.ToLower()] = new CondJudger_JunXianLvl();
            // 转生等级
            canUseDict[CondIndex.Cond_ChangeLife.ToLower()] = new CondJudger_ChangeLife();
            // 角色等级
            canUseDict[CondIndex.Cond_RoleLevel.ToLower()] = new CondJudger_RoleLevel();
            // VIP等级
            canUseDict[CondIndex.Cond_VipLvl.ToLower()] = new CondJudger_VIPLvl();
            // 护身符阶数
            canUseDict[CondIndex.Cond_HuFuSuit.ToLower()] = new CondJudger_HuFuSuit();
            //大天使阶数
            canUseDict[CondIndex.Cond_DaTianShiSuit.ToLower()] = new CondJudger_DaTianShiSuit();
            // 需要结婚
            canUseDict[CondIndex.Cond_NeedMarry.ToLower()] = new CondJudger_NeedMarry();
            // 需要完成任务
            canUseDict[CondIndex.Cond_NeedTask.ToLower()] = new CondJudger_NeedTask();

            // 旧的3种条件，移植过来

            // 不可超过等级
            canUseDict[CondIndex.Cond_CanNotBeyondLevel.ToLower()] = new CondJudger_CannotBeyongLevel();
            // 非安全区
            canUseDict[CondIndex.Cond_NotSafeRegion.ToLower()] = new CondJudger_NotSafeRegion();
            // 至少需要元宝
            canUseDict[CondIndex.cond_YuanBaoMoreThan.ToLower()] = new CondJudger_YuanBaoMoreThan();
            // 需要开启功能
            canUseDict[CondIndex.Cond_NeedOpen.ToLower()] = new CondJudger_NeedOpen();
        }

        /// <summary>
        /// 限制类物品检测是否可以使用
        /// </summary>
        /// <param name="client">GameClient</param>
        /// <param name="goodsID">物品ID</param>
        /// <returns>可以使用返回True，否则返回False</returns>
        public bool CheckCanUse_ByToType(GameClient client, int goodsID)
        {
            string failedMsg = string.Empty;
            return CheckCanUse_ByToType(client, goodsID, out failedMsg);
        }

        /// <summary>
        /// 限制类物品检测是否可以使用
        /// </summary>
        /// <param name="client">GameClient</param>
        /// <param name="goodsID">物品ID</param>
        /// <param name="failedMsg">[out]不可使用的信息提示</param>
        /// <returns>可以使用返回True，同时failedMsg为NULL。否则返回False，failedMsg指示不可使用原因</returns>
        public bool CheckCanUse_ByToType(GameClient client, int goodsID, out string failedMsg)
        {
            failedMsg = "";

            SystemXmlItem systemGoodsItem = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsID, out systemGoodsItem))
            {
                return false;
            }

            string condIdx = systemGoodsItem.GetStringValue("ToType");
            string condArg = systemGoodsItem.GetStringValue("ToTypeProperty");

            // 未配置限制条件，认为可以使用
            if (string.IsNullOrEmpty(condIdx) || condIdx == "-1")
            {
                return true;
            }

            condIdx = condIdx.ToLower();

            ICondJudger judger = null;
            if (!canUseDict.TryGetValue(condIdx, out judger))
            {
                // 没有找到限制处理程序，默认可以使用
                return true;
            }

            return judger.Judge(client, condArg, out failedMsg);
        }
    }
}
