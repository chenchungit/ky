using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Interface;
using Server.Tools;
using GameServer.Server;

namespace GameServer.Logic.BossAI
{
    /// <summary>
    /// 触发条件_出生
    /// </summary>
    public class BirthOnCondition : ITriggerCondition
    {
        /// <summary>
        /// 对象的类型
        /// </summary>
        public BossAITriggerTypes TriggerType
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 触发条件_血量变化
    /// </summary>
    public class BloodChangedCondition : ITriggerCondition
    {
        /// <summary>
        /// 对象的类型
        /// </summary>
        public BossAITriggerTypes TriggerType
        {
            get;
            set;
        }

        /// <summary>
        /// 最小血量比例
        /// </summary>
        public double MinLifePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 最大血量比例
        /// </summary>
        public double MaxLifePercent
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 触发条件_受伤
    /// </summary>
    public class InjuredCondition : ITriggerCondition
    {
        /// <summary>
        /// 对象的类型
        /// </summary>
        public BossAITriggerTypes TriggerType
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 触发条件_死亡
    /// </summary>
    public class DeadCondition : ITriggerCondition
    {
        /// <summary>
        /// 对象的类型
        /// </summary>
        public BossAITriggerTypes TriggerType
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 触发条件_攻击后
    /// </summary>
    public class AttackedCondition : ITriggerCondition
    {
        /// <summary>
        /// 对象的类型
        /// </summary>
        public BossAITriggerTypes TriggerType
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 触发条件_全部死亡
    /// </summary>
    public class AllDeadCondition : ITriggerCondition
    {
        /// <summary>
        /// 对象的类型
        /// </summary>
        public BossAITriggerTypes TriggerType
        {
            get;
            set;
        }

        /// <summary>
        /// 怪物ID列表
        /// </summary>
        public List<int> MonsterIDList = new List<int>();
    }

    /// <summary>
    /// 触发条件_怪物存活多长时间后
    /// </summary>
    public class LivingTimeCondition : ITriggerCondition
    {
        /// <summary>
        /// 对象的类型
        /// </summary>
        public BossAITriggerTypes TriggerType
        {
            get;
            set;
        }

        /// <summary>
        /// 时间(分钟)
        /// </summary>
        public long LivingMinutes = 0;
    }

    /// <summary>
    /// Boss AI 缓存项
    /// </summary>
    public class BossAIItem
    {
        /// <summary>
        /// 流水ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// AI ID
        /// </summary>
        public int AIID { get; set; }

        /// <summary>
        /// 最大触发次数
        /// </summary>
        public int TriggerNum { get; set; }

        /// <summary>
        /// 触发CD
        /// </summary>
        public int TriggerCD { get; set; }

        /// <summary>
        /// 触发类型
        /// </summary>
        public int TriggerType { get; set; }

        /// <summary>
        /// 触发条件
        /// </summary>
        public ITriggerCondition Condition { get; set; }

        /// <summary>
        /// AI广播信息
        /// </summary>
        public String Desc { get; set; }
    }

    /// <summary>
    /// Boss AI 缓存管理
    /// </summary>
    public static class BossAICachingMgr
    {
        /// <summary>
        /// boss AI缓存字典
        /// </summary>
        private static Dictionary<string, List<BossAIItem>> _BossAICachingDict = new Dictionary<string, List<BossAIItem>>();

        /// <summary>
        /// 根据AI ID和触发类型获取配置项
        /// </summary>
        /// <param name="AID"></param>
        /// <param name="triggerType"></param>
        /// <returns></returns>
        public static List<BossAIItem> FindCachingItem(int AIID, int triggerType)
        {
            string key = string.Format("{0}_{1}", AIID, triggerType);

            List<BossAIItem> bossAIItemList = null;
            if (!_BossAICachingDict.TryGetValue(key, out bossAIItemList))
            {
                return null;
            }

            return bossAIItemList;
        }

        /// <summary>
        /// 解析条件
        /// </summary>
        /// <param name="triggerType"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static ITriggerCondition ParseCondition(int ID, int triggerType, string condition)
        {
            ITriggerCondition triggerCondition = null;
            if (triggerType == (int)BossAITriggerTypes.BirthOn)
            {
                triggerCondition = new BirthOnCondition()
                {
                    TriggerType = (BossAITriggerTypes)triggerType,
                };
            }
            else if (triggerType == (int)BossAITriggerTypes.BloodChanged)
            {
                triggerCondition = new BloodChangedCondition()
                {
                    TriggerType = (BossAITriggerTypes)triggerType,
                };

                string[] fields = condition.Split('-');
                if (fields.Length != 2) //报错
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("服务器端配置的Boss AI项，条件配置错误 ID={0}", ID));
                    return null;
                }

                (triggerCondition as BloodChangedCondition).MinLifePercent = Global.SafeConvertToDouble(fields[0]);
                (triggerCondition as BloodChangedCondition).MaxLifePercent = Global.SafeConvertToDouble(fields[1]);
            }
            else if (triggerType == (int)BossAITriggerTypes.Injured)
            {
                triggerCondition = new InjuredCondition()
                {
                    TriggerType = (BossAITriggerTypes)triggerType,
                };
            }
            else if (triggerType == (int)BossAITriggerTypes.Dead)
            {
                triggerCondition = new DeadCondition()
                {
                    TriggerType = (BossAITriggerTypes)triggerType,
                };
            }
            else if (triggerType == (int)BossAITriggerTypes.Attacked)
            {
                triggerCondition = new AttackedCondition()
                {
                    TriggerType = (BossAITriggerTypes)triggerType,
                };
            }
            else if (triggerType == (int)BossAITriggerTypes.DeadAll)
            {
                triggerCondition = new AllDeadCondition()
                {
                    TriggerType = (BossAITriggerTypes)triggerType,
                };

                string[] fields = condition.Split(',');
                for (int i = 0; i < fields.Length; i++)
                {
                    (triggerCondition as AllDeadCondition).MonsterIDList.Add(Global.SafeConvertToInt32(fields[i]));
                }
            }
            else if (triggerType == (int)BossAITriggerTypes.LivingTime)
            {
                triggerCondition = new LivingTimeCondition()
                {
                    TriggerType = (BossAITriggerTypes)triggerType,
                };

                (triggerCondition as LivingTimeCondition).LivingMinutes = Global.SafeConvertToInt32(condition);
            }

            return triggerCondition;
        }

        /// <summary>
        /// 从xml项中解析Boss AI缓存项
        /// </summary>
        /// <param name="systemXmlItem"></param>
        private static BossAIItem ParseBossAICachingItem(SystemXmlItem systemXmlItem)
        {
            BossAIItem bossAIItem = new BossAIItem()
            {
                ID = systemXmlItem.GetIntValue("ID"),
                AIID = systemXmlItem.GetIntValue("AIID"),
                TriggerNum = systemXmlItem.GetIntValue("TriggerNum"),
                TriggerCD = systemXmlItem.GetIntValue("TriggerCD"),
                TriggerType = systemXmlItem.GetIntValue("TriggerType"),
                Desc = systemXmlItem.GetStringValue("Description"),
            };

            bossAIItem.Condition = ParseCondition(bossAIItem.ID, bossAIItem.TriggerType, systemXmlItem.GetStringValue("Condition"));
            if (null == bossAIItem.Condition)
            {
                return null;
            }

            return bossAIItem;
        }

        /// <summary>
        /// 加载缓存项
        /// </summary>
        public static void LoadBossAICachingItems(SystemXmlItems systemBossAI)
        {
            Dictionary<string, List<BossAIItem>> bossAICachingDict = new Dictionary<string, List<BossAIItem>>();
            foreach (var key in systemBossAI.SystemXmlItemDict.Keys)
            {
                SystemXmlItem systemXmlItem = systemBossAI.SystemXmlItemDict[(int)key];
                BossAIItem bossAIItem = ParseBossAICachingItem(systemXmlItem);
                if (null == bossAIItem) //解析出错
                {
                    continue;
                }

                string strKey = string.Format("{0}_{1}", bossAIItem.AIID, bossAIItem.TriggerType);

                List<BossAIItem> bossAIItemList = null;
                if (!bossAICachingDict.TryGetValue(strKey, out bossAIItemList))
                {
                    bossAIItemList = new List<BossAIItem>();
                    bossAICachingDict[strKey] = bossAIItemList;
                }

                bossAIItemList.Add(bossAIItem);                
            }

            _BossAICachingDict = bossAICachingDict;
        }
    }
}
