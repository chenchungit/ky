using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;
using Server.Tools;

namespace GameServer.Logic
{
    public class MonsterTask : ScheduleTask
    {
        private TaskInternalLock _InternalLock = new TaskInternalLock();
        public TaskInternalLock InternalLock { get { return _InternalLock; } }

        public int mapCode;

        public int subMapCode = -1;

        private int attackFrameCount = 0;

        private int heartbeatNum = 0;

        private int attackNum = 0;

        private long hearBeatTotalTime = 0;

        private long attackTotalTime = 0;

        private int frameCount = 0;
        private long Oldticks = TimeUtil.NOW();

        public MonsterTask(int mapCode, int subMapCode = -1)
        {
            this.mapCode = mapCode;
            this.subMapCode = subMapCode;
        }

        public void run()
        {
            try
            {
                long ticks1 = TimeUtil.NOW();
                //if (mapCode == 1)
                //    SysConOut.WriteLine("----------------------------------追击玩家----------------------------" + (ticks1 - Oldticks).ToString());

                Oldticks = ticks1;
                //角色的buffer后台工作
                GameManager.ClientMgr.DoSpriteExtensionWorkByPerMap(mapCode, subMapCode);

                long ticks2 = TimeUtil.NOW();
                if (ticks2 > ticks1 + 1000)
                {
                    LogManager.WriteLog(LogTypes.Error, String.Format("DoSpriteExtensionWorkByPerMap, mapCode:{0}, subMapCode:{1}, 消耗:{2}毫秒", mapCode, subMapCode, ticks2 - ticks1));
                }

                long startTicks = TimeUtil.NOW();

                ticks1 = TimeUtil.NOW();
                // 怪物生命心跳调度函数
                GameManager.MonsterMgr.DoMonsterHeartTimer(mapCode, subMapCode);
                ticks2 = TimeUtil.NOW();
                if (ticks2 > ticks1 + 800)
                {
                    LogManager.WriteLog(LogTypes.Error, String.Format("DoMonsterHeartTimer, mapCode:{0}, subMapCode:{1}, 消耗:{2}毫秒", mapCode, subMapCode, ticks2 - ticks1));
                }

                //                     if (ticks2 - ticks1 >= 80)
                //                     {
                //                         LogManager.WriteLog(LogTypes.Error, String.Format("DoMonsterHeartTimer 消耗:{0}毫秒, MapID: {1}, SubMapCode{2}", ticks2 - ticks1, mapCode, subMapCode));
                //                     }

                heartbeatNum++;
                hearBeatTotalTime += ticks2 - ticks1;

                ticks1 = TimeUtil.NOW();

                //每5帧执行此战斗调度
                //if (attackFrameCount % 5 == 0) HX_SERVER 攻击/寻路等测试，先关闭
                {
                    GameManager.MonsterMgr.DoMonsterAttack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, 0, mapCode, subMapCode);
                    attackNum++;
                }

                ticks2 = TimeUtil.NOW();

                attackTotalTime += ticks2 - ticks1;

                //                     if (ticks2 - ticks1 >= 80)
                //                     {
                //                         LogManager.WriteLog(LogTypes.Error, String.Format("DoMonsterAttack 消耗:{0}毫秒, MapID: {1}, SubMapCode{2}", ticks2 - ticks1, mapCode, subMapCode));
                //                     }

                if (++attackFrameCount > 1000000)
                    attackFrameCount = 0;

                frameCount++;


                if (frameCount % 240 == 0)
                {
                    long heartBeatCount = hearBeatTotalTime / heartbeatNum;
                    long attackCount = attackTotalTime / attackNum;
                    if (heartBeatCount > 32)
                    {
                        LogManager.WriteLog(LogTypes.Error, String.Format("DoMonsterHeartTimer 平均耗时:{0}毫秒, MapID: {1}, SubMapCode: {2}", heartBeatCount, mapCode, subMapCode));
                    }
                    if (attackCount > 32)
                    {
                        LogManager.WriteLog(LogTypes.Error, String.Format("DoMonsterAttack 平均耗时:{0}毫秒, MapID: {1}, SubMapCode: {2}", attackCount, mapCode, subMapCode));
                    }

                    hearBeatTotalTime = 0;
                    heartbeatNum = 0;
                    attackTotalTime = 0;
                    attackNum = 0;
                }

                if (frameCount >= 2400000)
                {
                    frameCount = 0;
                }
            }
            catch (Exception ex)
            {
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, "monsterHeartTimer_Tick", false);
            }
        }
    }
}
