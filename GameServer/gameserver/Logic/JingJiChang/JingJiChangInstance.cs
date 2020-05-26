using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;

namespace GameServer.Logic.JingJiChang
{

    public enum JingJiFuBenState
    {
        INITIALIZED,//创建
        WAITING_CHANGEMAP_FINISH,//等待客户端切换地图
        START_CD,//通开始倒计时
        STARTED,//开始战斗
        STOP_CD,//结束倒计时（战斗结束）
        STOP_TIMEOUT_CD,//结束倒计时（战斗超时）
        STOPED,//副本结束
        DESTROYED,//销毁
    }

    /// <summary>
    /// 竞技场副本实例
    /// </summary>
    public class JingJiChangInstance : ScheduleTask
    {
        private TaskInternalLock _InternalLock = new TaskInternalLock();
        public TaskInternalLock InternalLock { get { return _InternalLock; } }

        private int fubenSeqId;

        private PeriodicTaskHandle handle;

        //副本当前状态
        private JingJiFuBenState state = JingJiFuBenState.INITIALIZED;

        //副本创建时间
        private long createTime = 0;

        //副本开始倒计时
        private long startCDTime = 0;

        //副本开始时间
        private long startedTime = 0;

        //副本结束倒计时时间
        private long stopCDTime = 0;

        //副本结束事件
        private long stopedTime = 0;

         //副本销毁时间
         private long destroyTime = 0;

        //玩家
        private GameClient player = null;

        //机器人
        private Robot robot = null;

        //进入竞技场延迟2秒通知客户端开始倒计时
        private static readonly long DelayStart = 2 * TimeUtil.SECOND;

        //进入竞技场后冷却5秒开始战斗
        private static readonly long StartCDTime = 6 * TimeUtil.SECOND;

        //战斗时间为2分50秒
        private static readonly long CombatTime = 2 * TimeUtil.MINITE + 45 * TimeUtil.SECOND;

        //战斗结束后延迟10秒推出竞技场
        private static readonly long StopCDTime = 10 * TimeUtil.SECOND;

        //副本结束后延迟10秒销毁副本
        private static readonly long DelayDestroyTime = 10 * TimeUtil.SECOND;

        public JingJiChangInstance(GameClient player, Robot robot, int fubenSeqId)
        {
            this.state = JingJiFuBenState.INITIALIZED;
            this.fubenSeqId = fubenSeqId;
            
            this.player = player;
            this.robot = robot;

            this.createTime = TimeUtil.NOW();
            this.startCDTime = this.createTime + DelayStart;

            ResetJingJiTime();
        }

        /// <summary>
        /// 重置竞技场计时
        /// </summary>
        public void ResetJingJiTime()
        {
            this.startedTime = TimeUtil.NOW() + StartCDTime;
            this.stopCDTime = this.startedTime + CombatTime;
            this.stopedTime = this.stopCDTime + StopCDTime;
            this.destroyTime = this.stopedTime + DelayDestroyTime;
        }

        public PeriodicTaskHandle Handle
        {
            get { return this.handle; }
            set { this.handle = value; }
        }

        /// <summary>
        /// 获取竞技场副本状态
        /// </summary>
        /// <returns></returns>
        public JingJiFuBenState getState()
        {
            return state;
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="state"></param>
        public void switchState(JingJiFuBenState state)
        {
            if (this.state == state)
                return;

            this.state = state;

            switch ((int)state)
            {
                case (int)JingJiFuBenState.INITIALIZED:
                    break;
                case (int)JingJiFuBenState.WAITING_CHANGEMAP_FINISH:
                    break;
                case (int)JingJiFuBenState.START_CD:
                    JingJiChangManager.getInstance().onJingJiFuBenStartCD(this);
                    break;
                case (int)JingJiFuBenState.STARTED:
                    JingJiChangManager.getInstance().onJingJiFuBenStarted(this);
                    break;
                case (int)JingJiFuBenState.STOP_CD:
                    this.stopedTime = TimeUtil.NOW() + StopCDTime;
                    this.destroyTime = this.stopedTime + DelayDestroyTime;
                    break;
                case (int)JingJiFuBenState.STOP_TIMEOUT_CD:
                    JingJiChangManager.getInstance().onJingJiFuBenStopForTimeOutCD(this);
                    this.switchState(JingJiFuBenState.STOP_CD);
                    break;
                case (int)JingJiFuBenState.STOPED:
                    JingJiChangManager.getInstance().onJingJiFuBenStoped(this);
                    this.destroyTime = TimeUtil.NOW() + DelayDestroyTime;
                    break;
                case (int)JingJiFuBenState.DESTROYED:
                    JingJiChangManager.getInstance().onJingJiFuBenDestroy(this);
                    break;
            }
        }

        public int getFuBenSeqId()
        {
            return this.fubenSeqId;
        }


        public GameClient getPlayer()
        {
            return this.player;
        }

        public Robot getRobot()
        {
            return this.robot;
        }

        public void setRobot(Robot robot)
        {
            this.robot = robot;
        }

        public void run()
        {
            long now = TimeUtil.NOW();

            if (now > this.startCDTime && now < this.startedTime && this.state == JingJiFuBenState.WAITING_CHANGEMAP_FINISH)
            {
                this.switchState(JingJiFuBenState.START_CD);
            }
            else if (now > this.startedTime && now < this.stopCDTime && this.state == JingJiFuBenState.START_CD)
            {
                this.switchState(JingJiFuBenState.STARTED);
            }
            else if (now > this.stopCDTime && now < this.stopedTime && this.state == JingJiFuBenState.STARTED)
            {
                this.switchState(JingJiFuBenState.STOP_TIMEOUT_CD);
            }
            else if (now > this.stopedTime && now < this.destroyTime && this.state == JingJiFuBenState.STOP_CD)
            {
                this.switchState(JingJiFuBenState.STOPED);
            }
            else if (now > this.destroyTime && this.state == JingJiFuBenState.STOPED)
            {
                this.switchState(JingJiFuBenState.DESTROYED);
            }

            if(this.state == JingJiFuBenState.INITIALIZED || this.state == JingJiFuBenState.START_CD || this.state == JingJiFuBenState.DESTROYED || this.state == JingJiFuBenState.STOPED)
                return;
            if (null == robot)
                return;
            robot.onUpdate();
        }

        public void release()
        {
            this.handle = null;
            this.player = null;
            this.robot = null;
        }
    }
}
