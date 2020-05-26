using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Tmsk.Contract;

namespace GameServer.Logic.Marriage.CoupleArena
{
    class CoupleArenaCopyScene
    {
        public int FuBenSeq;
        public int GameId;
        public int MapCode;
        public CopyMap CopyMap;

        public long m_lPrepareTime = 0;
        public long m_lBeginTime = 0;
        public long m_lEndTime = 0;
        public long m_lLeaveTime = 0;
        public GameSceneStatuses m_eStatus = GameSceneStatuses.STATUS_NULL;
        public GameSceneStateTimeData StateTimeData = new GameSceneStateTimeData();

        // 0: 无胜方， 1：第一对夫妻胜利，2：第2对夫妻胜利
        public int WinSide = 0;
        public long m_lPrevUpdateTime = 0;
        public long m_lCurrUpdateTime = 0;

        /// <summary>
        /// key: roleid 
        /// value: side
        /// </summary>
        public Dictionary<int, int> EnterRoleSide = new Dictionary<int, int>();

        public bool IsYongQiMonsterExist = false;
        public int YongQiBuff_Role;

        public bool IsZhenAiMonsterExist = false;
        public int ZhenAiBuff_Role;
        public long ZhenAiBuff_StartMs;
    }
}
