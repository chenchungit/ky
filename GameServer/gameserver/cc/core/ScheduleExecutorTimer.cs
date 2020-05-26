using GameServer.Core.Executor;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GameServer.cc.core
{
    class ScheduleExecutorTimer
    {
        private Thread g_Thread = null;
        public static ScheduleExecutorTimer Instance = new ScheduleExecutorTimer();
        Dictionary<ScheduleTask, long> TimerDict = new Dictionary<ScheduleTask, long>();

        public void Init()
        {
            g_Thread = new Thread(new ThreadStart(ThreadFunction));
            g_Thread.Start();
        }
        public void scheduleExecute(ScheduleTask task, int periodic)
        {
            long sztimer = 0;
            if (!TimerDict.TryGetValue(task, out sztimer))
            {
                TimerDict.Add(task, periodic);
            }
            else
            {

            }
        }
        public void ThreadFunction()
        {
            while(true)
            {
                foreach(var s in TimerDict)
                {
                    ScheduleTask task = s.Key as ScheduleTask;
                    if (task.InternalLock.TryEnter())
                    {
                        bool logRunTime = false;
                        long nowTicks = TimeUtil.CurrentTicksInexact;
                        try
                        {
                            task.run();
                        }
                        catch (System.Exception ex)
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("{0}执行时异常,{1}", task.ToString(), ex.ToString()));
                        }
                        finally
                        {
                            logRunTime = task.InternalLock.Leave();
                        }

                        if (logRunTime)
                        {
                            long finishTicks = TimeUtil.CurrentTicksInexact;
                            if (finishTicks - nowTicks > TimeUtil.SECOND)
                            {
                                try
                                {
                                    MonsterTask monsterTask = task as MonsterTask;
                                    if (null != monsterTask)
                                    {
                                        LogManager.WriteLog(LogTypes.Error, string.Format("{0} mapCode:{1},subMapCode:{2},执行时间:{3}毫秒"
                                                                , task.ToString(), monsterTask.mapCode, monsterTask.subMapCode, finishTicks - nowTicks));
                                    }
                                    else
                                    {
                                        LogManager.WriteLog(LogTypes.Error, string.Format("{0}执行时间:{1}毫秒", task.ToString(), finishTicks - nowTicks));
                                    }
                                }
                                catch
                                {
                                    //写日志异常就不记了
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(2000);
            }
        }
    }
}
