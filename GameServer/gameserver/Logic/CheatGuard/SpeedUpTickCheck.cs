using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Tools.Pattern;
using GameServer.Core.Executor;
using Server.Tools;

namespace GameServer.Logic.CheatGuard
{
    class SpeedUpTickCheck : SingletonTemplate<SpeedUpTickCheck>
    {
        class CheckRoleItem
        {
            public string UserId;
            public int RoleId;
            public string RoleName;
            public string IpAndPort;

            /// <summary>
            /// 上次心跳时客户端的DateTime.Now.Ticks
            /// </summary>
            public long LastReportClientTick;

            /// <summary>
            /// 上次心跳时服务器的时间 timeGetTime, 防止时间飘逸
            /// </summary>
            public uint LastReceiveServerMs;

            #region Check1
            public int MaybeTroubleTimes;
            public List<double> MaybeTroubleDiffRates = new List<double>();
            #endregion

            #region Check2
            /// <summary>
            /// 心跳N次客户端经历的总Tick
            /// </summary>
            public long CliTotalElapsedTicks;

            /// <summary>
            /// 心跳N次服务器经历的总Tick
            /// </summary>
            public long SrvTotalElapsedTicks;
            public int TotalElapsedTimes;
            #endregion
        }

        private SpeedUpTickCheck() { }
        private object Mutex = new object();
        private Dictionary<int, CheckRoleItem> checkRoleDict = new Dictionary<int, CheckRoleItem>();
        private Dictionary<int, long> roleLastLog1Ticks = new Dictionary<int, long>();
        private Dictionary<int, long> roleLastLog2Ticks = new Dictionary<int, long>();

        #region Check1
        private double totalDiffRate = 0;
        private double totalDiffCnt = 0;
        private double currUseDiffRate = 1.0;
        #endregion

        #region Check2
        private int TotalElapsedTimes = 10;
        private double TotalElapsedDiffRate = 0.2;
        #endregion

        private void ForceRemove(int roleId)
        {
            lock (Mutex)
            {
                this.checkRoleDict.Remove(roleId);
                this.roleLastLog1Ticks.Remove(roleId);
                this.roleLastLog2Ticks.Remove(roleId);
            }
        }

        public void TickTest()
        {
            long t1 = DateTime.Now.Ticks;
            uint u1 = TimeUtil.timeGetTime();

            System.Threading.Thread.Sleep(5);

            long t2 = DateTime.Now.Ticks;
            uint u2 = TimeUtil.timeGetTime();

            Console.WriteLine(t2 - t1);
            Console.WriteLine((u2 - u1) * TimeSpan.TicksPerMillisecond);
        }

        public void LoadConfig()
        {
            try
            {
                string[] arr = GameManager.systemParamsList.GetParamValueByName("SpeedUpTickCheck").Split(',');
                this.TotalElapsedTimes = Convert.ToInt32(arr[0]);
                this.TotalElapsedDiffRate = Convert.ToDouble(arr[1]);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, ex.Message.ToString());
                this.TotalElapsedTimes = 10;
                this.TotalElapsedDiffRate = 0.2;
            }  
        }

        public void OnLogin(GameClient client)
        {
            if (client == null) return;
            this.ForceRemove(client.ClientData.RoleID);
        }

        public void OnLogout(GameClient client)
        {
            if (client == null) return;
            this.ForceRemove(client.ClientData.RoleID);
        }

        public void OnClientHeart(GameClient client, long reportRealClientTick)
        {
            if (client == null) return;
            if (client.ClientSocket.session.IsGM) return;

            lock (Mutex)
            {
                CheckRoleItem roleItem = null;
                if (!checkRoleDict.TryGetValue(client.ClientData.RoleID, out roleItem))
                {
                    roleItem = new CheckRoleItem();
                    roleItem.RoleId = client.ClientData.RoleID;
                    roleItem.RoleName = client.ClientData.RoleName;
                    roleItem.UserId = client.strUserID;
                    roleItem.IpAndPort = RobotTaskValidator.getInstance().GetIp(client);

                    roleItem.LastReportClientTick = reportRealClientTick;
                    roleItem.LastReceiveServerMs = TimeUtil.timeGetTime();

                    #region Check1
                    roleItem.MaybeTroubleTimes = 0;
                    roleItem.MaybeTroubleDiffRates.Clear();
                    #endregion

                    #region Check2
                    roleItem.CliTotalElapsedTicks = 0;
                    roleItem.SrvTotalElapsedTicks = 0;
                    roleItem.TotalElapsedTimes = 0;
                    #endregion

                    checkRoleDict[client.ClientData.RoleID] = roleItem;
                }
                else
                {
                    long _LastReportClientTick = roleItem.LastReportClientTick;
                    uint _LastReceiveServerMs = roleItem.LastReceiveServerMs;
                    roleItem.LastReportClientTick = reportRealClientTick;
                    roleItem.LastReceiveServerMs = TimeUtil.timeGetTime();

                    uint serverDiffMs = roleItem.LastReceiveServerMs - _LastReceiveServerMs;
                    long serverDiffTick = ((long)serverDiffMs) * TimeSpan.TicksPerMillisecond;
                    long clientDiffTick = roleItem.LastReportClientTick - _LastReportClientTick;
                    if (serverDiffTick <= 0) return; //wtf

                    #region Check1
                    double pipeTickDiffRate = Math.Abs(serverDiffTick - clientDiffTick) * 1.0 / serverDiffTick;
                    if (pipeTickDiffRate > currUseDiffRate)
                    {
                        roleItem.MaybeTroubleTimes++;
                        roleItem.MaybeTroubleDiffRates.Add(pipeTickDiffRate);
                        if (roleItem.MaybeTroubleTimes >= 5)
                        {
                            long lastLog1Tick = 0;
                            if (!roleLastLog1Ticks.TryGetValue(roleItem.RoleId, out lastLog1Tick)
                                || TimeUtil.NowDateTime().Ticks - lastLog1Tick >= 12 * TimeSpan.TicksPerMinute)
                            {
                                roleLastLog1Ticks[roleItem.RoleId] = TimeUtil.NowDateTime().Ticks;
                                LogManager.WriteLog(LogTypes.Fatal, string.Format("Check1 uid={0},rid={1},rname={2},ip={3} 疑似使用加速, 心跳时间差比例={4}",
                                    roleItem.UserId, roleItem.RoleId, roleItem.RoleName, roleItem.IpAndPort, string.Join(",", roleItem.MaybeTroubleDiffRates)), null, false);
                            }
                            roleItem.MaybeTroubleTimes = 0;
                            roleItem.MaybeTroubleDiffRates.Clear();
                        }
                    }
                    else
                    {
                        // 在有效的时间差范围内, 有40%的概率统计本次时间差，用于逐步修正服务器的正确时间差范围
                        if (Global.GetRandom() > 0.6)
                        {
                            totalDiffRate += pipeTickDiffRate;
                            totalDiffCnt++;

                            if (totalDiffCnt >= 100)
                            {
                                // 每统计100次，校正一下时间差
                                double oldDiffRate = currUseDiffRate;
                                currUseDiffRate = totalDiffRate / totalDiffCnt;
                                totalDiffCnt = 0;

                                LogManager.WriteLog(LogTypes.Error, string.Format("加速时间允许时间差范围变更 {0} ---> {1}", oldDiffRate, currUseDiffRate));
                            }
                        }
                    }
                    #endregion

                    #region Check2
                    roleItem.CliTotalElapsedTicks += clientDiffTick;
                    roleItem.SrvTotalElapsedTicks += serverDiffTick;
                    roleItem.TotalElapsedTimes++;
                    if (roleItem.TotalElapsedTimes >= this.TotalElapsedTimes)
                    {
                        double _rate = Math.Abs(roleItem.SrvTotalElapsedTicks - roleItem.CliTotalElapsedTicks) * 1.0 / roleItem.SrvTotalElapsedTicks;
                        if (_rate > this.TotalElapsedDiffRate)
                        {
                            long lastLog2Tick = 0;
                            if (!roleLastLog2Ticks.TryGetValue(roleItem.RoleId, out lastLog2Tick)
                                || TimeUtil.NowDateTime().Ticks - lastLog2Tick >= 12 * TimeSpan.TicksPerMinute)
                            {
                                roleLastLog2Ticks[roleItem.RoleId] = TimeUtil.NowDateTime().Ticks;
                                LogManager.WriteLog(LogTypes.Fatal, string.Format("Check2 uid={0},rid={1},rname={2},ip={3} 疑似使用加速, CliTotalElapsedTicks={4}, SrvTotalElapsedTicks={5}, diffRate={6}",
                                    roleItem.UserId, roleItem.RoleId, roleItem.RoleName, roleItem.IpAndPort, roleItem.CliTotalElapsedTicks, roleItem.SrvTotalElapsedTicks, _rate), null, false);
                            }
                        }

                        roleItem.CliTotalElapsedTicks = 0;
                        roleItem.SrvTotalElapsedTicks = 0;
                        roleItem.TotalElapsedTimes = 0;
                    }
                    #endregion
                }
            }
        }
    }
}
