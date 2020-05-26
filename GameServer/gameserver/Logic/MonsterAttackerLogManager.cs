using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Tools.Pattern;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 攻击者
    /// </summary>
    public class MonsterAttackerLog
    {
        public int RoleId;                      // 攻击者id
        public int Occupation;                  // 攻击者职业
        public string RoleName;                 // 攻击者角色名
        public long FirstAttackMs;              // 首次攻击的时间(ms)
        public long LastAttackMs;               // 最后一次攻击的时间(ms)
        public long TotalInjured;               // 一共造成的伤害
        public int InjureTimes;                 // 伤害次数
        public double FirstAttack_MaxAttckV;    // 首次造成伤害时的最大物理攻击
        public double FirstAttack_MaxMAttackV;  // 首次造成伤害时的最大魔法攻击
        public double MaxAttackV;               // 最大物理攻击
        public double MaxMAttackV;              // 最大魔法攻击
    }

    public class RoleRelifeLog
    {
        public int RoleId;
        public string Rolename;
        public int MapCode;
        public string Reason;

        public bool hpModify;
        public int oldHp;
        public int newHp;

        public bool mpModify;
        public int oldMp;
        public int newMp;

        public RoleRelifeLog(int roleId, string roleName, int mapcode, string reason)
        {
            this.RoleId = roleId;
            this.Rolename = roleName;
            this.MapCode = mapcode;
            this.Reason = reason;
        }
    }

    public class MonsterAttackerLogManager : SingletonTemplate<MonsterAttackerLogManager>, IEventListener
    {
        private MonsterAttackerLogManager()
        {
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, this);
        }

        private object Mutex = new object();
        private HashSet<int> NeedRecordLogMonsters = new HashSet<int>();
        private HashSet<int> NeedRecordRelifeRoles = new HashSet<int>();

        /// <summary>
        /// 加载需要记录日志的怪物
        /// </summary>
        public void LoadRecordMonsters()
        {
            lock (Mutex)
            {
                NeedRecordLogMonsters.Clear();

                int[] monsterIds = GameManager.systemParamsList.GetParamValueIntArrayByName("LogAttackBoss");
                for (int i = 0; monsterIds != null && i < monsterIds.Length; ++i)
                {
                    if (!NeedRecordLogMonsters.Contains(monsterIds[i]))
                    {
                        NeedRecordLogMonsters.Add(monsterIds[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 是否需要记录怪物的伤害日志
        /// </summary>
        /// <param name="monsterId"></param>
        /// <returns></returns>
        public bool IsNeedRecordAttackLog(int monsterId)
        {
            lock (Mutex)
            {
                return NeedRecordLogMonsters.Contains(monsterId);
            }
        }

        #region Implement interface `IEventListener`
        public void processEvent(EventObject eventObject)
        {
            int eventType = eventObject.getEventType();
            if (eventType == (int)EventTypes.MonsterDead)
            {
                MonsterDeadEventObject monsterDeadEvent = eventObject as MonsterDeadEventObject;
                if (null != monsterDeadEvent)
                {
                    Monster m = monsterDeadEvent.getMonster();
                    if (m == null)
                        return;

                    if (!IsNeedRecordAttackLog(m.XMonsterInfo.MonsterId))
                        return;

                    string log = m.BuildAttackerLog();
                    LogManager.WriteLog(LogTypes.Attack, log);
                }
            }
        }
        #endregion


        public void AddRoleRelifeLog(RoleRelifeLog log)
        {
            if (log == null) return;
            if (!log.hpModify && !log.mpModify) return;

            bool bNeedLog = false;
            lock (Mutex)
            {
                bNeedLog = this.NeedRecordRelifeRoles.Contains(log.RoleId);
            }

            if (!bNeedLog) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("回血日志， roleid={0}, rolename={1}, mapcode={2}, reason={3}", log.RoleId, log.Rolename, log.MapCode, log.Reason);
            if (log.hpModify) sb.AppendFormat(" ,oldHp={0}, newHp={1}, addHp={2}", log.oldHp, log.newHp, log.newHp - log.oldHp);
            if (log.mpModify) sb.AppendFormat(" ,oldMp={0}, newMp={1}, addMp={2}", log.oldMp, log.newMp, log.newMp - log.oldMp);
            LogManager.WriteLog(LogTypes.Attack, sb.ToString());
        }

        public void SetLogRoleRelife(int roleId, bool bLog = true)
        {
            lock (Mutex)
            {
                if (bLog && !this.NeedRecordRelifeRoles.Contains(roleId))
                {
                    this.NeedRecordRelifeRoles.Add(roleId);
                }

                if (!bLog)
                {
                    this.NeedRecordRelifeRoles.Remove(roleId);
                }
            }
        }
    }
}
