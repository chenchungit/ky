using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Windows;
using Tmsk.Contract;
using Server.Data;
using KF.Contract.Data;

namespace GameServer.Logic
{
    /// <summary>
    /// 勇者战场场景对象
    /// </summary>
    public class LangHunLingYuScene
    {
        /// <summary>
        /// 活动起始时间点
        /// </summary>
        public long StartTimeTicks = 0;

        /// <summary>
        /// 血色城堡场景开始时间
        /// </summary>
        public long m_lPrepareTime = 0;

        /// <summary>
        /// 血色城堡场景战斗开始时间
        /// </summary>
        public long m_lBeginTime = 0;

        /// <summary>
        /// 血色城堡场景战斗结束时间
        /// </summary>
        public long m_lEndTime = 0;

        /// <summary>
        /// 立场时间
        /// </summary>
        public long m_lLeaveTime = 0;

        /// <summary>
        /// 场景状态
        /// </summary>
        public GameSceneStatuses m_eStatus = GameSceneStatuses.STATUS_NULL;

        /// <summary>
        /// 获胜方
        /// </summary>
        public int SuccessSide = 0;

        /// <summary>
        /// 跨服
        /// </summary>
        public int GameId;

        /// <summary>
        /// 关联的副本对象字典
        /// </summary>
        public Dictionary<int, CopyMap> CopyMapDict = new Dictionary<int, CopyMap>();

        /// <summary>
        /// 场景配置信息
        /// </summary>
        public LangHunLingYuSceneInfo SceneInfo;

        public CityLevelInfo LevelInfo;

        /// <summary>
        /// 龙塔内帮会的人数信息列表
        /// </summary>
        public List<BangHuiRoleCountData> LongTaBHRoleCountList = new List<BangHuiRoleCountData>();

        /// <summary>
        /// 龙塔临时占有者信息
        /// </summary>
        public LangHunLingYuLongTaOwnerData LongTaOwnerData = new LangHunLingYuLongTaOwnerData();

        /// <summary>
        /// 罗兰城战旗帜Buff拥有者信息列表
        /// </summary>
        public List<LangHunLingYuQiZhiBuffOwnerData> QiZhiBuffOwnerDataList = new List<LangHunLingYuQiZhiBuffOwnerData>();

        /// <summary>
        /// NPCID到旗帜配置字典
        /// </summary>
        public Dictionary<int, QiZhiConfig> NPCID2QiZhiConfigDict = new Dictionary<int, QiZhiConfig>();

        /// <summary>
        /// 上一个唯一的帮会
        /// </summary>
        public int LastTheOnlyOneBangHui = 0;

        /// <summary>
        /// 特殊旗帜的拥有者帮会
        /// </summary>
        public int SuperQiZhiOwnerBhid;

        /// <summary>
        /// 帮会独占龙塔的保持时间
        /// </summary>
        public long BangHuiTakeHuangGongTicks;

        /// <summary>
        /// 定时给予收益
        /// </summary>
        public long LastAddBangZhanAwardsTicks = 0;

        /// <summary>
        /// 角色得分信息集合
        /// </summary>
        public Dictionary<int, LangHunLingYuClientContextData> ClientContextDataDict = new Dictionary<int, LangHunLingYuClientContextData>();

        public LangHunLingYuCityData CityData = new LangHunLingYuCityData();

        public Dictionary<int, BangHuiMiniData> BHID2BangHuiMiniDataDict = new Dictionary<int, BangHuiMiniData>();

        public int SuccessBangHuiId;

        /// <summary>
        /// 时间状态信息
        /// </summary>
        public GameSceneStateTimeData StateTimeData = new GameSceneStateTimeData();

        /// <summary>
        /// 怪物创建队列
        /// </summary>
        public SortedList<long, List<object>> CreateMonsterQueue = new SortedList<long, List<object>>();

        public void CleanAllInfo()
        {
            m_lPrepareTime = 0;
            m_lBeginTime = 0;
            m_lEndTime = 0;
            m_eStatus = GameSceneStatuses.STATUS_NULL;
        }

    }
}
