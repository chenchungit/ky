using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.MoRi
{
    // 末日审判配置的怪
    public class MoRiMonster
    {
        public int Id;
        public string Name;
        public int MonsterId;
        public int BirthX;
        public int BirthY;
        public int KillLimitSecond;
        // 限时击杀我之后，给boss加的扩展属性
        public Dictionary<int, float> ExtPropDict = new Dictionary<int, float>();
    }

    public class MoRiMonsterTag
    {
        public int CopySeqId;
        public int MonsterIdx;
        public Dictionary<int, float> ExtPropDict = null;
    }

    public class MoRiMonsterData
    {
        // MoRiShenPan.xml 里面的id
        public int Id;
        // 基于DateTime.Ticks / 10,000
        public long BirthMs;
        // 基于DateTime.Ticks / 10,000
        public long DeathMs;
    }

    // 事件类型：1=出生、2=死亡
    // 出生事件：
    // id:ms

    // 死亡事件
    // id:ms
}
