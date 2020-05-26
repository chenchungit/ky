using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;
using Tmsk.Tools;

namespace GameServer.Logic
{
    public class ElementWarScene
    {
        /// <summary>
        /// 关联的副本对象
        /// </summary>
        public CopyMap CopyMapInfo;

        /// <summary>
        /// 地图ID
        /// </summary>
        public int MapID = 0;

        /// <summary>
        /// 副本ID
        /// </summary>
        public int CopyID = 0;

        /// <summary>
        /// 副本的队列ID
        /// </summary>
        public int FuBenSeqId = 0;

        /// <summary>
        /// 跨服
        /// </summary>
        public long GameId;

        /// <summary>
        /// 玩家人数
        /// </summary>
        public int PlayerCount = 0;
   
        /// <summary>
        /// 成绩信息
        /// </summary>
        public ElementWarScoreData ScoreData = new ElementWarScoreData();

        /// <summary>
        /// 场景状态
        /// </summary>
        public GameSceneStatuses SceneStatus = GameSceneStatuses.STATUS_NULL;

        /// <summary>
        /// 场景开始时间
        /// </summary>
        public long PrepareTime = 0;

        /// <summary>
        /// 场景战斗开始时间
        /// </summary>
        public long BeginTime = 0;

        /// <summary>
        /// 场景战斗结束时间
        /// </summary>
        public long EndTime = 0;

        /// <summary>
        /// 立场时间
        /// </summary>
        public long LeaveTime = 0;

        /// <summary>
        /// 时间状态信息
        /// </summary>
        public GameSceneStateTimeData StateTimeData = new GameSceneStateTimeData();

        /// <summary>
        /// 刷怪锁
        /// </summary>
        //public object CreateMonsterMutex = new object();

        /// <summary>
        /// 刷怪标记
        /// </summary>
        public int IsMonsterFlag = 0;

        /// <summary>
        /// 刷怪时间
        /// </summary>
        public long CreateMonsterTime = 0;

        /// <summary>
        /// 刷怪波数(上次)
        /// </summary>
        public int MonsterWaveOld = 0;

        /// <summary>
        /// 刷怪波数
        /// </summary>
        public int MonsterWave = 1;

        /// <summary>
        /// 刷怪总波数
        /// </summary>
        public int MonsterWaveTotal = 30;

        /// <summary>
        /// 刷出的怪的数量
        /// </summary>
        public int MonsterCountCreate = 0;

        /// <summary>
        /// 杀死了几只怪
        /// </summary>
        public int MonsterCountKill = 0;

        /// <summary>
        /// 被击杀的怪物的唯一ID的集合,因为击杀不加锁,用这个集合记录以防止重复计数
        /// </summary>
        public HashSet<long> KilledMonsterHashSet = new HashSet<long>();

        /// <summary>
        /// 记录击杀的怪物ID并递增杀怪计数
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        public bool AddKilledMonster(Monster monster)
        {
            bool firstKill = false;
            lock (KilledMonsterHashSet)
            {
                if (!KilledMonsterHashSet.Contains(monster.UniqueID))
                {
                    KilledMonsterHashSet.Add(monster.UniqueID);
                    MonsterCountKill++;
                    firstKill = true;
                }
            }
            return firstKill;
        }

        /// <summary>
        /// 清空接口
        /// </summary>
        public void CleanAllInfo()
        {
           
        }
    }
}
