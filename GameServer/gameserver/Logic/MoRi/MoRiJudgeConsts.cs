using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.MoRi
{
    /// <summary>
    /// 末日审判的一些常量定义
    /// </summary>
    public static class MoRiJudgeConsts
    {
        // 末日审判的怪物配置
        public const string MonsterConfigFile = "Config/MoRiShenPan.xml";

        // 末日审判的副本id(配置文件)，MapCode在程序启动的时候读取
        public const int CopyId = 70000;

        // 杀死一个boss之后，隔多久刷新下一个boss
        public const long MonsterFlushIntervalMs = 1300;
    }

    /// <summary>
    /// 末日审判boss出生，死亡时间，用以通知客户端
    /// </summary>
    public static class MoRiMonsterEvent
    {
        public const int Unknown = 0;
        public const int Birth = 1;
        public const int Death = 2;
    }
}
