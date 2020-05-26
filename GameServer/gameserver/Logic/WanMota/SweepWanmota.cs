using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Server.Data;
using Server.Tools;

namespace GameServer.Logic.WanMota
{
    public class SweepWanmota
    {
        /// <summary>
        /// 万魔塔扫荡时对应的客户端信息
        /// </summary>
        private GameClient sweepClient = null;

        /// <summary>
        /// 开始扫荡的层编号
        /// </summary>
        public int nSweepingOrder;

        /// <summary>
        /// 扫荡最大层数
        /// </summary>
        public int nSweepingMaxOrder;

        /// <summary>
        /// 万魔塔扫荡测定时器
        /// </summary>
        private Timer _WanMoTaSweepingTimer = null;

        /// <summary>
        /// 万魔塔扫荡测定时器
        /// </summary>
        public Timer WanMoTaSweepingTimer
        {
            get { return _WanMoTaSweepingTimer; }
            set { _WanMoTaSweepingTimer = value; }
        }

        public SweepWanmota(GameClient client)
        {
            sweepClient = client;
        }

        /// <summary>
        /// 每2秒扫荡一层万魔塔
        /// </summary>
        public void BeginSweeping()
        {
            if (null == WanMoTaSweepingTimer)
            {
                WanMoTaSweepingTimer = new Timer(2 * 1000);
                WanMoTaSweepingTimer.Elapsed += new ElapsedEventHandler(Sweeping);
                WanMoTaSweepingTimer.Interval = 2 * 1000;
                WanMoTaSweepingTimer.Enabled = true;
            }
        }

        /// <summary>
        /// 每2秒扫荡一层万魔塔
        /// </summary>
        public void StopSweeping()
        {
            if (null != WanMoTaSweepingTimer)
            {
                lock (WanMoTaSweepingTimer)
                {
                    WanMoTaSweepingTimer.Enabled = false;
                    WanMoTaSweepingTimer.Stop();
                    WanMoTaSweepingTimer = null;
                }
            }
        }

        /// <summary>
        /// 每2秒扫荡一层万魔塔
        /// </summary>
        private void Sweeping(object source, ElapsedEventArgs e)
        {
            lock (sweepClient)
            {
                WanMotaCopySceneManager.GetWanmotaSweepReward(sweepClient, WanMotaCopySceneManager.nWanMoTaFirstFuBenOrder + nSweepingOrder - 1);
                nSweepingOrder++;

                if (nSweepingOrder > nSweepingMaxOrder)
                {
                    // 扫荡完成
                    StopSweeping();

                    // 将奖励汇总
                    List<SingleLayerRewardData> listRewardData = SweepWanMotaManager.SummarySweepRewardInfo(sweepClient);
                    List<SingleLayerRewardData> WanMoTaLayerRewardList = sweepClient.ClientData.LayerRewardData.WanMoTaLayerRewardList;

                    // 汇总后用汇总奖励代替各层奖励
                    sweepClient.ClientData.LayerRewardData.WanMoTaLayerRewardList = listRewardData;

                    // 如果更新失败，还原
                    if (-1 == WanMoTaDBCommandManager.UpdateSweepAwardDBCommand(sweepClient, 0))
                    {
                        // 扫荡奖励汇总后，写到数据库失败
                        LogManager.WriteLog(LogTypes.Error, "扫荡奖励汇总后，写到数据库失败");

                        sweepClient.ClientData.LayerRewardData.WanMoTaLayerRewardList = WanMoTaLayerRewardList;
                    }
                    // 成功，发送到客户端
                    else
                    {
                        sweepClient.ClientData.WanMoTaProp.nSweepLayer = 0;
                        SweepWanMotaManager.UpdataSweepInfo(sweepClient, listRewardData);
                        WanMoTaLayerRewardList = null;
                    }
                }
                else
                {
                    sweepClient.ClientData.WanMoTaProp.nSweepLayer = nSweepingOrder;
                    WanMoTaDBCommandManager.UpdateSweepAwardDBCommand(sweepClient, sweepClient.ClientData.WanMoTaProp.nSweepLayer);
                }
            }
        }
    }
}
