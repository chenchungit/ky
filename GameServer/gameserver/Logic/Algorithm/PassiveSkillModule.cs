using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using GameServer.Core.Executor;
using Server.Tools;

namespace GameServer.Logic
{
    public enum SkillTriggerTypes
    {
        None, //无
        Attack, //攻击时触发
        Injured, //受伤
        KillMonster, //杀怪
    }

    public class PassiveSkillData
    {
        public int skillId; //ID
        public int skillLevel;
        public int triggerRate; //TriggerType
        public int triggerType; //TriggerOdds
        public int coolDown; //CDTime
        public int triggerCD; //CDTime

        public SkillData skillData = new SkillData();
         public PassiveSkillData()
        {

        }
        public PassiveSkillData(int _skillId, int _skillLevel, int _triggerType, int _triggerRate, int _coolDown, int _spanTime)
        {
            skillId = _skillId;
            skillLevel = _skillLevel;
            triggerType = _triggerType;
            triggerRate = _triggerRate;
            coolDown = _coolDown;
            triggerCD = _spanTime;
            skillData.SkillID = skillId;
            skillData.SkillLevel = skillLevel;
        }
    }

    public class PassiveSkillModule
    {
        private object mutex = new object();

        /// <summary>
        /// 精灵技能列表
        /// </summary>
        public Dictionary<int, PassiveSkillData> passiveSkillList = new Dictionary<int, PassiveSkillData>();

        private Dictionary<int, long> coolDownDict = new Dictionary<int, long>();

        private Dictionary<int, long> _spanTimeDict = new Dictionary<int, long>();

        /// <summary>
        /// 下次处理
        /// </summary>
        public long NextTriggerSkillForInjuredTicks;

        public SkillData currentSkillData = new SkillData();

        private void TryTriggerSkills(GameClient client, long nowTicks, SkillTriggerTypes type)
        {
            //LogManager.WriteLog(LogTypes.Error, string.Format("-------------------petSkill begin ---------- 触发类型={0}", type.ToString()));
            lock (mutex)
            {
                foreach (var data in passiveSkillList.Values)
                {
                    if (data.triggerType == (int)type)
                    {
                        //触发间隔
                        long spanTicks;
                        bool b = _spanTimeDict.TryGetValue(data.skillId, out spanTicks);
                        //LogManager.WriteLog(LogTypes.Error, string.Format("-------------------petSkill --1-- {0} spanTime={1} nowTime={2} span={3}", data.skillId,spanTicks, nowTicks, spanTicks - nowTicks));
                        if (b && spanTicks > nowTicks) continue;

                        //先判断触发概率
                        int rnd = Global.GetRandomNumber(0, 100);
                        //LogManager.WriteLog(LogTypes.Error, string.Format("-------------------petSkill --2-- {0} 概率={1} 随机数={2} 触发={3}", data.skillId, data.triggerRate, rnd, rnd - data.triggerRate < 0));
                        if (rnd >= data.triggerRate) continue;

                        //技能cd时间                    
                        long coolDownTicks;
                         b = coolDownDict.TryGetValue(data.skillId, out coolDownTicks);
                        //if (coolDownDict.TryGetValue(data.skillId, out coolDownTicks) && coolDownTicks > nowTicks) continue;
                         //LogManager.WriteLog(LogTypes.Error, string.Format("-------------------petSkill --3-- {0} coolTime={1} nowTime={2} cd={3}", data.skillId, coolDownTicks, nowTicks, coolDownTicks - nowTicks));
                        if (b && coolDownTicks > nowTicks) continue;

                       
                        coolDownDict[data.skillId] = nowTicks + data.coolDown * 1000;
                        _spanTimeDict[data.skillId] = nowTicks + data.triggerCD * 1000;

                        //LogManager.WriteLog(LogTypes.Error, string.Format("-------------------petSkill --4-- {0} coolTime={1} spanTime={2}", data.skillId, data.coolDown, data.triggerCD));
                        int posX = client.ClientData.PosX;
                        int posY = client.ClientData.PosY;
                        SpriteAttack.AddDelayMagic(client, client.ClientData.RoleID, posX, posY, posX, posY, data.skillId);
                        EventLogManager.AddRoleSkillEvent(client, SkillLogTypes.PassiveSkillTrigger, LogRecordType.IntValue2, data.skillId, data.skillLevel, data.triggerRate, rnd, data.coolDown);
                    }
                }
            }

            //LogManager.WriteLog(LogTypes.Error, string.Format("-------------------petSkill end ----------"));
        }

        public void OnInjured(GameClient client)
        {
            long nowTicks = TimeUtil.NOW();
            if (nowTicks > NextTriggerSkillForInjuredTicks)
            {
                TryTriggerSkills(client, nowTicks, SkillTriggerTypes.Injured);
                NextTriggerSkillForInjuredTicks = nowTicks + 1000;
            }
        }

        public void OnProcessMagic(GameClient client, int enemy, int enemyX, int enemyY)
        {
            long nowTicks = TimeUtil.NOW();
            TryTriggerSkills(client, nowTicks, SkillTriggerTypes.Attack);
        }

        public void OnKillMonster(GameClient client)
        {
            long nowTicks = TimeUtil.NOW();
            TryTriggerSkills(client, nowTicks, SkillTriggerTypes.KillMonster);
        }

        public void UpdateSkillList(List<PassiveSkillData> skillList)
        {
            if (null != skillList)
            {
                lock (mutex)
                {
                    passiveSkillList.Clear();
                    foreach (var skill in skillList)
                    {
                        passiveSkillList[skill.skillId] = new PassiveSkillData(skill.skillId, skill.skillLevel, skill.triggerType, skill.triggerRate, skill.coolDown, skill.triggerCD);
                    }
                }
            }
        }

        public void UpdateSkillData(int magicCode, int level, int triggerType, int triggerRate, int coolDown, int spanCD)
        {
            lock (mutex)
            {
                PassiveSkillData data;
                if (passiveSkillList.TryGetValue(magicCode, out data))
                {
                    data.skillLevel = level;
                    data.triggerRate = triggerRate;
                    data.coolDown = coolDown;
                    data.triggerType = triggerType;
                    data.triggerCD = spanCD;
                }
            }
        }

        public SkillData GetSkillData(int magicCode)
        {
            lock (mutex)
            {
                PassiveSkillData data;
                if (passiveSkillList.TryGetValue(magicCode, out data))
                {
                    return data.skillData;
                }
            }

            return null;
        }
    }
}
