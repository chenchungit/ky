#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Interface;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    public class MagicCoolDownMgr
    {
        #region 技能CoolDown 冷却管理

        /// 技能CoolDown项
        private Dictionary<int, CoolDownItem> SkillCoolDownDict = new Dictionary<int, CoolDownItem>();

        /// <summary>
        /// 判断技能是否处于冷却状态
        /// </summary>
        /// <param name="magicCode"></param>
        /// <returns></returns>
        public bool SkillCoolDown(int skillID)
        {
            CoolDownItem coolDownItem = null;
            if (!SkillCoolDownDict.TryGetValue(skillID, out coolDownItem))
            {
                return true;
            }

            long ticks = TimeUtil.NOW();
            if (ticks > (coolDownItem.StartTicks + coolDownItem.CDTicks))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 添加技能冷却
        /// </summary>
        /// <param name="magicCode"></param>
        public void AddSkillCoolDown(IObject attacker, int skillID)
        {
            if (attacker is GameClient)
            {
                AddSkillCoolDownForClient(attacker as GameClient, skillID);
            }
            else if (attacker is Monster)
            {
                AddSkillCoolDownForMonster(attacker as Monster, skillID);
            }
        }

        /// <summary>
        /// 添加技能冷却
        /// </summary>
        /// <param name="magicCode"></param>
        public void AddSkillCoolDownForClient(GameClient client, int skillID)
        {
            SystemXmlItem systemMagic = null;
            if (!GameManager.SystemMagicQuickMgr.MagicItemsDict.TryGetValue(skillID, out systemMagic))
            {
                return;
            }

            long nowTicks = TimeUtil.NOW();
            int cdTime = systemMagic.GetIntValue("CDTime");
            if (cdTime <= 0) //不需要CD时间
            {
                int skillType = systemMagic.GetIntValue("SkillType");
				// 如果是普攻
                if (skillType == SkillTypes.NormalAttack)
                {
                    if (null != client.ClientData.SkillDataList)
                    {
						// 这里感觉是普攻对其他技能 增加一个375毫秒的公共cd
                        for (int i = 0; i < client.ClientData.SkillDataList.Count; i++)
                        {
                            SkillData skillData = client.ClientData.SkillDataList[i];
                            if (null == skillData || skillData.DbID > 0) continue;

                            Global.AddCoolDownItem(SkillCoolDownDict, skillData.SkillID, nowTicks, 375);//普通攻击统一加个375毫秒的CD
                        }
                    }

                    return;
                }

                //连续技能使用首段技能的CD
                int nParentMagicID = systemMagic.GetIntValue("ParentMagicID");
                if (nParentMagicID <= 0) //不需要CD时间
                {
                    return;
                }
                if (!GameManager.SystemMagicQuickMgr.MagicItemsDict.TryGetValue(nParentMagicID, out systemMagic))
                {
                    return;
                }
                cdTime = systemMagic.GetIntValue("CDTime");
                // 判断父技能的CD是否到时
                if (cdTime <= 0)
                {
                    return;
                }
            }

            int pubCDTime = systemMagic.GetIntValue("PubCDTime");

            Global.AddCoolDownItem(SkillCoolDownDict, skillID, nowTicks, cdTime * 1000 - 500);
            if (pubCDTime > 0)
            {
                if (null != client.ClientData.SkillDataList)
                {
                    for (int i = 0; i < client.ClientData.SkillDataList.Count; i++)
                    {
                        SkillData skillData = client.ClientData.SkillDataList[i];
                        if (null == skillData) continue;

                        Global.AddCoolDownItem(SkillCoolDownDict, skillData.SkillID, nowTicks, pubCDTime/* * 1000*/);
                    }
                }
            }
        }

        /// <summary>
        /// 添加技能冷却
        /// </summary>
        /// <param name="magicCode"></param>
        public void AddSkillCoolDownForMonster(Monster monster, int skillID)
        {
            SystemXmlItem systemMagic = null;
            if (!GameManager.SystemMagicQuickMgr.MagicItemsDict.TryGetValue(skillID, out systemMagic))
            {
                return;
            }

            int cdTime = systemMagic.GetIntValue("CDTime");
            if (cdTime <= 0) //不需要CD时间
            {
                return;
            }

            int pubCDTime = systemMagic.GetIntValue("PubCDTime");

            long nowTicks = TimeUtil.NOW();
            Global.AddCoolDownItem(SkillCoolDownDict, skillID, nowTicks, cdTime * 1000);
#if ___CC___FUCK___YOU___BB___
            if (null != monster.XMonsterInfo.Skills)
            {
                for (int i = 0; i < monster.XMonsterInfo.Skills.Count; i++)
                {
                    if (pubCDTime > 0)
                    {
                        Global.AddCoolDownItem(SkillCoolDownDict, monster.XMonsterInfo.Skills[i], nowTicks, pubCDTime/* * 1000*/);
                    }
                }
            }
#else
             if (null != monster.MonsterInfo.SkillIDs)
            {
                for (int i = 0; i < monster.MonsterInfo.SkillIDs.Length; i++)
                {
                    if (pubCDTime > 0)
                    {
                        Global.AddCoolDownItem(SkillCoolDownDict, monster.MonsterInfo.SkillIDs[i], nowTicks, pubCDTime/* * 1000*/);
                    }
                }
            }
#endif
        }

        #endregion 技能CoolDown 冷却管理
    }
}
