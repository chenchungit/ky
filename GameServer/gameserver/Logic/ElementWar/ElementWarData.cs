using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 怪物波次配置
    /// </summary>
    public class ElementWarMonsterConfigInfo
    {
        /// <summary>
        /// 波次id
        /// </summary>
        public int OrderID = 0;

        /// <summary>
        /// 怪物数量
        /// </summary>
        public int MonsterCount = 0;

        /// <summary>
        /// 怪物id
        /// </summary>
        public List<int> MonsterIDs = null;

        /// <summary>
        /// 提示1波时间
        /// </summary>
        public int Up1 = 90;

        /// <summary>
        /// 提示2波时间
        /// </summary>
        public int Up2 = 60;

        /// <summary>
        /// 提示3波时间
        /// </summary>
        public int Up3 = 45;

        /// <summary>
        /// 提示4波时间
        /// </summary>
        public int Up4 = 30;

        /// <summary>
        /// 出生地X
        /// </summary>
        public int X = 0;

        /// <summary>
        /// 出生地Y
        /// </summary>
        public int Y = 0;

        /// <summary>
        /// 出生地半径
        /// </summary>
        public int Radius = 0;
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class ElementWarData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();
  
        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapID = 70100;

        /// <summary>
        /// 副本ID
        /// </summary>
        public int CopyID = 70100;

        /// <summary>
        /// 发奖最小波次
        /// </summary>
        public int MinAwardWave = 0;

        /// <summary>
        /// 荧光粉末奖励（0-30）
        /// </summary>
        public int[] AwardLight;

        /// <summary>
        /// 副本获得物品是否绑定
        /// </summary>
        public int GoodsBinding = 1;

        /// <summary>
        /// 波次数据
        /// </summary>
        public Dictionary<int, ElementWarMonsterConfigInfo> MonsterOrderConfigList = new Dictionary<int, ElementWarMonsterConfigInfo>();

        public ElementWarMonsterConfigInfo GetOrderConfig(int order)
        {
            if (MonsterOrderConfigList.ContainsKey(order))
                return MonsterOrderConfigList[order];

            return null;
        }

        #region 活动时间

        /// <summary>
        /// 准备时间
        /// </summary>
        public int PrepareSecs = 1;

        /// <summary>
        /// 战斗时间
        /// </summary>
        public int FightingSecs = 900;

        /// <summary>
        /// 清场时间
        /// </summary>
        public int ClearRolesSecs = 15;

        /// <summary>
        /// 总时间
        /// </summary>
        public int TotalSecs { get { return  PrepareSecs + FightingSecs + ClearRolesSecs; } }

        #endregion 活动时间

    }
}

