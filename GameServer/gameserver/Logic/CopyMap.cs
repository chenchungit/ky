using GameServer.Core.Executor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 副本地图定义
    /// </summary>
    public class CopyMap
    {
        #region 基础属性和方法定义

        /// <summary>
        /// 副本地图ID
        /// </summary>
        public int CopyMapID
        {
            get;
            set;
        }

        /// <summary>
        /// 副本顺序ID
        /// </summary>
        public int FuBenSeqID
        {
            get;
            set;
        }

        /// <summary>
        /// 副本地图的编号
        /// </summary>
        public int MapCode
        {
            get;
            set;
        }

        /// <summary>
        /// 副本的编号 [7/7/2014 LiaoWei]
        /// </summary>
        public int FubenMapID
        {
            get;
            set;
        }

        /// <summary>
        /// 副本地图类型
        /// </summary>
        public MapTypes CopyMapType
        {
            get;
            set;
        }

        /// <summary>
        /// 初始化的时间
        /// </summary>
        public long InitTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 剧情副本完成状态 [7/16/2014 LiaoWei]
        /// </summary>
        public bool bStoryCopyMapFinishStatus
        {
            get;
            set;
        }

        public bool bNeedRemove = false;

        /// <summary>
        /// 记录的普通怪物的总的个数
        /// </summary>
        public int TotalNormalNum = 0;

        /// <summary>
        /// 记录的普通怪物的已经杀死的字典
        /// </summary>
        private Dictionary<int, bool> _KilledNormalDict = new Dictionary<int,bool>();

        /// <summary>
        /// 记录已经杀死的怪的ID
        /// </summary>
        /// <param name="monsterID"></param>
        public void SetKilledNormalDict(int monsterID)
        {
            lock (this)
            {
                if (-1 != monsterID)
                {
                    _KilledNormalDict[monsterID] = true;
                }
                else
                {
                    _KilledNormalDict.Clear();
                }
            }
        }

        /// <summary>
        /// 记录已经杀死的怪的ID
        /// </summary>
        /// <param name="monsterID"></param>
        public void ClearKilledNormalDict()
        {
            lock (this)
            {
                _KilledNormalDict.Clear();
            }
        }

        /// <summary>
        /// 记录的普通怪物的已经杀死的个数
        /// </summary>
        public int KilledNormalNum
        {
            get { lock (this) { return _KilledNormalDict.Count; } }
        }

        /// <summary>
        /// 记录的动态创建的怪物总的个数
        /// </summary>
        public int TotalDynamicMonsterNum = 0;

        /// <summary>
        /// 记录的动态创建的怪物的已经杀死的字典
        /// </summary>
        private Dictionary<long, bool> _KilledDynamicMonsterDict = new Dictionary<long, bool>();

        /// <summary>
        /// 记录已经杀死的动态创建的怪物的ID
        /// </summary>
        /// <param name="monsterID"></param>
        public void SetKilledDynamicMonsterDict(long uniqueID)
        {
            lock (this)
            {
                if (-1 != uniqueID)
                {
                    _KilledDynamicMonsterDict[uniqueID] = true;
                }
                else
                {
                    _KilledDynamicMonsterDict.Clear();
                }
            }
        }

        /// <summary>
        /// 记录已经杀死的动态创建的怪物的ID
        /// </summary>
        /// <param name="monsterID"></param>
        public void ClearKilledDynamicMonsterDict()
        {
            lock (this)
            {
                _KilledDynamicMonsterDict.Clear();
            }
        }

        /// <summary>
        /// 记录的动态创建的怪物的已经杀死的个数
        /// </summary>
        public int KilledDynamicMonsterNum
        {
            get { lock (this) { return _KilledDynamicMonsterDict.Count; } }
        }

        /// <summary>
        /// 记录的boss怪物的总的个数
        /// </summary>
        public int TotalBossNum = 0;

        /// <summary>
        /// 记录的Boss的已经杀死的字典
        /// </summary>
        private Dictionary<int, bool> _KilledBossDict = new Dictionary<int, bool>();

        /// <summary>
        /// 记录已经杀死的Boss的ID
        /// </summary>
        /// <param name="monsterID"></param>
        public void SetKilledBossDict(int monsterID)
        {
            lock (this)
            {
                if (-1 != monsterID)
                {
                    _KilledBossDict[monsterID] = true;
                }
                else
                {
                    _KilledBossDict.Clear();
                }
            }
        }

        /// <summary>
        /// 清除已经杀死的Boss的ID
        /// </summary>
        /// <param name="monsterID"></param>
        public void ClearKilledBossDict()
        {
            lock (this)
            {
                _KilledBossDict.Clear();
            }
        }

        /// <summary>
        /// 记录的Boss怪物的已经杀死的个数
        /// </summary>
        public int KilledBossNum
        {
            get { lock (this) { return _KilledBossDict.Count; } }
        }


        // 新手场景相关 begin [12/1/2013 LiaoWei]

        /// <summary>
        /// 创建城门标记
        /// </summary>
        public int FreshPlayerCreateGateFlag
        {
            get;
            set;
        }

        /// <summary>
        /// 杀死A类型怪的个数
        /// </summary>
        public int FreshPlayerKillMonsterACount
        {
            get;
            set;
        }

        /// <summary>
        /// 杀死B类型怪的个数
        /// </summary>
        public int FreshPlayerKillMonsterBCount
        {
            get;
            set;
        }

        /// <summary>
        /// 是否已经刷出了水晶棺材
        /// </summary>
        public bool HaveBirthShuiJingGuan
        {
            get;
            set;
        }

        // 新手场景相关 end [12/1/2013 LiaoWei]

        // 经验副本 Begin [3/18/2014 LiaoWei]

        /*/// <summary>
        /// 经验副本刷怪波数
        /// </summary>
        public int ExperienceCopyMapCreateMonsterWave
        {
            get;
            set;
        }

        /// <summary>
        /// 经验副本刷怪标记
        /// </summary>
        public int ExperienceCopyMapCreateMonsterFlag
        {
            get;
            set;
        }
        
        /// <summary>
        /// 经验副本刷怪数量
        /// </summary>
        public int ExperienceCopyMapCreateMonsterNum
        {
            get;
            set;
        }

        /// <summary>
        /// 经验副本需要杀怪的数量
        /// </summary>
        public int ExperienceCopyMapNeedKillMonsterNum
        {
            get;
            set;
        }

        /// <summary>
        /// 经验副本已经杀怪的数量
        /// </summary>
        public int ExperienceCopyMapKillMonsterNum
        {
            get;
            set;
        }

        /// <summary>
        /// 经验副本剩余怪的数量
        /// </summary>
        public int ExperienceCopyMapRemainMonsterNum
        {
            get;
            set;
        }

        /// <summary>
        /// 经验副本已经杀怪的总数量
        /// </summary>
        public int ExperienceCopyMapKillMonsterTotalNum
        {
            get;
            set;
        }
        // 经验副本 End [3/18/2014 LiaoWei]*/

        /// <summary>
        /// 是否已经执行了进入地图的lua脚本
        /// </summary>
        public bool ExecEnterMapLuaFile
        {
            get;
            set;
        }

        /// <summary>
        /// 是否给予了副本奖励
        /// </summary>
        public bool CopyMapPassAwardFlag = false;

        /// <summary>
        /// 是否跨服副本
        /// </summary>
        public bool IsKuaFuCopy = false;

        /// <summary>
        /// 可以移除副本的时间
        /// </summary>
        public long CanRemoveTicks { get; private set; }

        /// <summary>
        /// 设置移除时间
        /// </summary>
        /// <param name="ticks"></param>
        public void SetRemoveTicks(long ticks)
        {
            CanRemoveTicks = ticks;
        }

        #endregion 基础属性和方法定义

        #region 线程相关属性和方法定义

        #region 玩家相关

        /// <summary>
        /// 副本地图中的玩家角色的列表
        /// </summary>
        private List<GameClient> _ClientsList = new List<GameClient>();

        /// <summary>
        /// 最后一个离开副本地图的用户的ticks
        /// </summary>
        private long LastLeaveClientTicks = 0;

        /// <summary>
        /// 添加玩家角色
        /// </summary>
        /// <param name="client"></param>
        public void AddGameClient(GameClient client)
        {
            lock (_ClientsList)
            {
                _ClientsList.Add(client);
            }
        }

        /// <summary>
        /// 删除玩家角色
        /// </summary>
        /// <param name="client"></param>
        public void RemoveGameClient(GameClient client)
        {
            long ticks = TimeUtil.NOW();
            lock (_ClientsList)
            {
                _ClientsList.Remove(client);

                //最后一个离开副本地图的用户的ticks
                LastLeaveClientTicks = ticks;
            }
        }

        /// <summary>
        /// 获取玩家列表的拷贝
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public List<GameClient> GetClientsList()
        {
            List<GameClient> newClientsList = null;

            //先锁定对象
            lock (_ClientsList)
            {
                newClientsList = _ClientsList.GetRange(0, _ClientsList.Count);
            }

            return newClientsList;
        }

        /// <summary>
        /// 获取玩家列表的拷贝
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mapCode"></param>
        /// <returns></returns>
        public List<object> GetClientsList2()
        {
            List<object> newClientsList = new List<object>(10);

            //先锁定对象
            lock (_ClientsList)
            {
                foreach (var client in _ClientsList)
                {
                    newClientsList.Add(client);
                }
            }

            return newClientsList;
        }

        /// <summary>
        /// 获取最后一个离开副本地图的用户的ticks
        /// </summary>
        /// <returns></returns>
        public long GetLastLeaveClientTicks()
        {
            lock (_ClientsList)
            {
                //最后一个离开副本地图的用户的ticks
                return LastLeaveClientTicks;
            }
        }

        public int GetGameClientCount()
        {
            lock (_ClientsList)
            {
                return _ClientsList.Count;
            }
        }

        #endregion 玩家相关

        #region 怪物相关

        /// <summary>
        /// 是否已经初始化了怪物
        /// </summary>
        private bool _IsInitMonster = false;

        /// <summary>
        /// 是否已经初始化了怪物
        /// </summary>
        public bool IsInitMonster
        {
            get { lock (this) { return _IsInitMonster; } }
            set { lock (this) { _IsInitMonster = value; } }
        }

        #endregion 怪物相关

        #region 地图相关

        /// <summary>
        /// 重进地图需要执行的事件队列
        /// </summary>
        public List<MapAIEvent> EventQueue = new List<MapAIEvent>();

        public void AddGuangMuEvent(int guangMuID, int show)
        {
            MapAIEvent guangMuEvent = new MapAIEvent() { GuangMuID = guangMuID, Show = show };
            lock (EventQueue)
            {
                EventQueue.Add(guangMuEvent);
            }
        }

        #endregion 地图相关

        #endregion 线程相关属性和方法定义
    }
}
