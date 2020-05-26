using GameServer.Logic.WanMota;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Copy
{
   public static class FuBenChecker
    {
        /// <summary>
        /// 是否完成了副本的前置任务
        /// </summary>
        public static bool HasFinishedPreTask(GameClient client, SystemXmlItem fubenItem)
        {
            if (client == null || fubenItem == null) return false;

            int copyTab = fubenItem.GetIntValue("TabID");
            int needTask = GlobalNew.GetFuBenTabNeedTask(copyTab);
            if (needTask > client.ClientData.MainTaskID)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否完成了前置副本
        /// </summary>
        public static bool HasPassedPreCopy(GameClient client, SystemXmlItem fubenItem)
        {
            if (client == null || fubenItem == null) return false;

            int nUpCopyID = fubenItem.GetIntValue("UpCopyID");
            int nFinishNumber = fubenItem.GetIntValue("FinishNumber");

            if (nUpCopyID > 0 && nFinishNumber > 0)
            {
                if (!Global.FuBenPassed(client, nUpCopyID))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///  是否在副本的等级限制中
        /// </summary>
        public static bool IsInCopyLevelLimit(GameClient client, SystemXmlItem fubenItem)
        {
            if (client == null || fubenItem == null) return false;

            int minLevel = fubenItem.GetIntValue("MinLevel");
            int maxLevel = fubenItem.GetIntValue("MaxLevel");
            maxLevel = maxLevel <= 0 ? 1000 : maxLevel;  //表示无限制

            int nMinZhuanSheng = fubenItem.GetIntValue("MinZhuanSheng");
            int nMaxZhuanSheng = fubenItem.GetIntValue("MaxZhuanSheng");
            nMaxZhuanSheng = nMaxZhuanSheng <= 0 ? 1000 : nMaxZhuanSheng;  // 无限制

            minLevel = Global.GetUnionLevel(nMinZhuanSheng, minLevel);
            maxLevel = Global.GetUnionLevel(nMaxZhuanSheng, maxLevel, true);

            // 首先判断级别是否满足
            int unionLevel = Global.GetUnionLevel(client.ClientData.ChangeLifeCount, client.ClientData.Level);
            if (unionLevel < minLevel || unionLevel > maxLevel)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否满足副本的进入次数和完成次数限制
        /// </summary>
        public static bool IsInCopyTimesLimit(GameClient client, SystemXmlItem fubenItem)
        {
            if (client == null || fubenItem == null) return false;

            int copyId = fubenItem.GetIntValue("ID");
            if (WanMotaCopySceneManager.IsWanMoTaMapCode(copyId))
            {
                // 万魔塔的挑战次数没有限制
                return true;
            }

            int maxEnterNum = fubenItem.GetIntValue("EnterNumber");
            int maxFinishNum = fubenItem.GetIntValue("FinishNumber");

            int hadFinishNum;
            int hadEnterNum = Global.GetFuBenEnterNum(Global.GetFuBenData(client, copyId), out hadFinishNum);

            if (maxEnterNum <= 0 && maxFinishNum <= 0)
            {
                // 没有次数限制
                return true;
            }

            // VIP 对金币副本和经验副本有次数加成
            // 血色城堡和万兽场不在这里判断
            int[] nAddNum = null;
            if (Global.IsInExperienceCopyScene(copyId))
                nAddNum = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJinYanFuBenNum");
            else if (copyId == (int)GoldCopySceneEnum.GOLDCOPYSCENEMAPCODEID)
                nAddNum = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJinBiFuBenNum");
            else
                nAddNum = null;

            int extAddNum = 0;
            int nVipLev = client.ClientData.VipLevel;
            if (nVipLev > 0 && nVipLev <= (int)VIPEumValue.VIPENUMVALUE_MAXLEVEL
                    && nAddNum != null && nAddNum.Length > nVipLev)
            {
                extAddNum = nAddNum[nVipLev];
            }

            // 注：最大进入次数和最大完成次数实际只会配置一个，但是这里不做特殊处理，两个都检查一下
            if (maxEnterNum > 0 && hadEnterNum >= maxEnterNum + extAddNum)
            {
                return false;
            }

            if (maxFinishNum > 0 && hadFinishNum >= maxFinishNum + extAddNum)
            {
                return false;
            }

            return true;
        }

       /*
        /// <summary>
        /// 检查是否能够进入副本
        /// </summary>
        /// <param name="client"></param>
        /// <param name="copyId"></param>
        /// <returns></returns>
        public bool CanEnter(GameClient client, int copyId)
        {
            if (client == null) return false;

            SystemXmlItem copyItem = null;
            if (!GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(copyId, out copyItem))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("CopyUtil.CanEnter copyid={0}不存在", copyId));
                return false;
            }

            return CanEnter(client, copyId, copyItem);
        }

        public bool CanEnter(GameClient client, int copyId, SystemXmlItem copyItem)
        {
            int minLevel = copyItem.GetIntValue("MinLevel");
            int maxLevel = copyItem.GetIntValue("MaxLevel");
            maxLevel = maxLevel <= 0 ? 1000 : maxLevel;  //表示无限制

            int nMinZhuanSheng = copyItem.GetIntValue("MinZhuanSheng");
            int nMaxZhuanSheng = copyItem.GetIntValue("MaxZhuanSheng");
            nMaxZhuanSheng = nMaxZhuanSheng <= 0 ? 1000 : nMaxZhuanSheng;  // 无限制

            minLevel = Global.GetUnionLevel(nMinZhuanSheng, minLevel);
            maxLevel = Global.GetUnionLevel(nMaxZhuanSheng, maxLevel, true);

            // 首先判断级别是否满足
            int unionLevel = Global.GetUnionLevel(client.ClientData.ChangeLifeCount, client.ClientData.Level);
            if (unionLevel < minLevel || unionLevel > maxLevel)
            {
                return false;
            }

            int copyType = copyItem.GetIntValue("CopyType");
            int enterNumber = copyItem.GetIntValue("EnterNumber");
            int finishNumber = copyItem.GetIntValue("FinishNumber");
            int toMapCode = copyItem.GetIntValue("MapCode");

            FuBenData fuBenData = Global.GetFuBenData(client, copyId);
            int nFinishNum;
            int haveEnterNum = Global.GetFuBenEnterNum(fuBenData, out nFinishNum);

            //判断进入次数是否满足
            if ((enterNumber >= 0 && haveEnterNum >= enterNumber) || (finishNumber >= 0 && nFinishNum >= finishNumber))
            {
                return false;
            }

            return true;
        }*/
    }
}
