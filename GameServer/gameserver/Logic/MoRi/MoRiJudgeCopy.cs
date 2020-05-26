using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;

namespace GameServer.Logic.MoRi
{
    class MoRiJudgeCopy
    {
        public MoRiJudgeCopy()
        {

        }

        public CopyMap MyCopyMap;
        public long GameId;

        public GameSceneStatuses m_eStatus = GameSceneStatuses.STATUS_NULL;

        public long DeadlineMs = 0L;

        public long CurrStateBeginMs = 0L;

        public int CurrMonsterIdx = -1;
        public long CurrMonsterBegin = 0L;

        public List<MoRiMonsterData> MonsterList = new List<MoRiMonsterData>();

        // 时间状态信息
        public GameSceneStateTimeData StateTimeData = new GameSceneStateTimeData();

        // analysize
        public DateTime StartTime; // 副本开始时间
        public DateTime EndTime;  // 通关结束结束时间
        public int LimitKillCount = 0; // 限时击杀的数量
        public int RoleCount; //副本结束时角色个数
        public bool Passed; // 是否通关
    }
}
