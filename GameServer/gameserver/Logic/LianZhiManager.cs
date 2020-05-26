using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Server.Protocol;
using GameServer.Logic;
using GameServer.Server;
using Server.Tools;
using System.Xml.Linq;
using GameServer.Server.CmdProcesser;
using GameServer.Core.Executor;
using Tmsk.Contract;
using GameServer.Core.GameEvent;
using GameServer.Logic.ActivityNew.SevenDay;

namespace GameServer.Logic
{
    /// <summary>
    /// 炼制系统管理类
    /// </summary>
    public class LianZhiManager : IManager
    {
        #region 标准接口

        private static LianZhiManager Instance = new LianZhiManager();

        public static LianZhiManager GetInstance()
        {
            return Instance;
        }

        public bool initialize()
        {
            InitConfig();
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_EXEC_LIANZHI, 3, LianZhiCmdProcessor.getInstance(TCPGameServerCmds.CMD_SPR_EXEC_LIANZHI));
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_QUERY_LIANZHICOUNT, 1, LianZhiCmdProcessor.getInstance(TCPGameServerCmds.CMD_SPR_QUERY_LIANZHICOUNT));
            return true;
        }
        public bool startup()
        {
            return true;
        }
        public bool showdown()
        {
            return true;
        }
        public bool destroy()
        {
            return true;
        }

        public void InitConfig()
        {
            try
            {
                JinBiLianZhi = GameManager.systemParamsList.GetParamValueIntArrayByName("JinBiLianZhi");
                BangZuanLianZhi = GameManager.systemParamsList.GetParamValueIntArrayByName("BangZuanLianZhi");
                ZuanShiLianZhi = GameManager.systemParamsList.GetParamValueIntArrayByName("ZuanShiLianZhi");
                VIPJinBiLianZhi = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPJinBiLianZhi");
                VIPBangZuanLianZhi = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPBangZuanLianZhi");
                VIPZuanShiLianZhi = GameManager.systemParamsList.GetParamValueIntArrayByName("VIPZuanShiLianZhi");
                ConfigLoadSuccess = true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("加载炼制系统配置信息是出错: {0}", ex.ToString()));
            }
        }

        #endregion 标准接口

        #region 配置变量

        /// <summary>
        /// 金币炼制配置参数
        /// </summary>
        private int[] JinBiLianZhi = null;

        /// <summary>
        /// 金币炼制次数的VIP提升配置
        /// </summary>
        private int[] VIPJinBiLianZhi = null;

        /// <summary>
        /// 绑钻炼制配置参数
        /// </summary>
        private int[] BangZuanLianZhi = null;

        /// <summary>
        /// 绑钻炼制次数的VIP提升配置
        /// </summary>
        private int[] VIPBangZuanLianZhi = null;

        /// <summary>
        /// 钻石炼制配置参数
        /// </summary>
        private int[] ZuanShiLianZhi = null;

        /// <summary>
        /// 钻石炼制次数的VIP提升配置
        /// </summary>
        private int[] VIPZuanShiLianZhi = null;

        /// <summary>
        /// 配置是否成功加载
        /// </summary>
        private bool ConfigLoadSuccess = false;

        #endregion 配置变量

        #region 运行时变量



        #endregion 运行时变量

        #region 处理方法

        /// <summary>
        /// 查询已用炼制次数
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool QueryLianZhiCount(GameClient client)
        {
            List<int> result = new List<int>();
            int roleID = client.ClientData.RoleID;
            int vipLevel = client.ClientData.VipLevel;
            int nID = (int)TCPGameServerCmds.CMD_SPR_QUERY_LIANZHICOUNT;
            result.Add(StdErrorCode.Error_Success);

            int lianZhiCount = 0; //当日已用此数
            int lianZhiDayID = -1; //使用的日期
            int lianZhiMaxCount = 0; //最大次数限制
            int dayID = TimeUtil.NowDateTime().DayOfYear;

            //验证配置和参数
            if (!ConfigLoadSuccess)
            {
                result[0] = StdErrorCode.Error_Config_Fault;
                result.Add(0);
                result.Add(0);
                result.Add(0);
                client.sendCmd(nID, result);
                return true;
            }

            //整理次数、消耗和奖励信息
            lianZhiCount = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiJinBiCount);
            lianZhiDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiJinBiDayID);
            lianZhiMaxCount = JinBiLianZhi[2] + VIPJinBiLianZhi[Math.Min(VIPJinBiLianZhi.Length - 1, vipLevel)];
            if (lianZhiDayID != dayID)
            {
                lianZhiCount = 0;
            }
            result.Add(lianZhiCount);

            lianZhiCount = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiBangZuanCount);
            lianZhiDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiBangZuanDayID);
            lianZhiMaxCount = BangZuanLianZhi[2] + VIPBangZuanLianZhi[Math.Min(VIPBangZuanLianZhi.Length - 1, vipLevel)];
            if (lianZhiDayID != dayID)
            {
                lianZhiCount = 0;
            }
            result.Add(lianZhiCount);

            lianZhiCount = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiZuanShiCount);
            lianZhiDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiZuanShiDayID);
            lianZhiMaxCount = ZuanShiLianZhi[4] + VIPZuanShiLianZhi[Math.Min(VIPZuanShiLianZhi.Length - 1, vipLevel)];
            if (lianZhiDayID != dayID)
            {
                lianZhiCount = 0;
            }
            result.Add(lianZhiCount);

            client.sendCmd(nID, result);
            return true;
        }

        /// <summary>
        /// 执行炼制
        /// </summary>
        /// <param name="type">炼制类型: 0 金币,1 绑钻,2钻石</param>
        /// <param name="count">炼制次数: 非正数表示全部剩余次数</param>
        /// <returns></returns>
        public bool ExecLianZhi(GameClient client, int type, int count)
        {
            int roleID = client.ClientData.RoleID;
            int vipLevel = client.ClientData.VipLevel;
            int nID = (int)TCPGameServerCmds.CMD_SPR_EXEC_LIANZHI;
            string useMsg = "炼制系统";
            List<int> result = new List<int>();

            result.Add(StdErrorCode.Error_Success);
            result.Add(type);
            result.Add(count);

            //验证配置和参数
            if (!ConfigLoadSuccess)
            {
                result[0] = StdErrorCode.Error_Config_Fault;
                client.sendCmd(nID, result);
            }
            else if (type < 0 || type > 2)
            {
                result[0] = StdErrorCode.Error_Invalid_Operation;
                client.sendCmd(nID, result);
            }
            else
            {
                int needJinBi = 0;
                int needBangZuan = 0;
                int needZuanShi = 0;
                long addExp = 0;
                int addXingHun = 0;
                int addJinBi = 0;

                int lianZhiCount = 0; //当日已用此数
                int lianZhiDayID = -1; //使用的日期
                int lianZhiMaxCount = 0; //最大次数限制
                int dayID = TimeUtil.NowDateTime().DayOfYear;

                //整理次数、消耗和奖励信息
                if (type == 0)
                {
                    useMsg = "金币炼制";
                    lianZhiCount = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiJinBiCount);
                    lianZhiDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiJinBiDayID);
                    lianZhiMaxCount = JinBiLianZhi[2] + VIPJinBiLianZhi[Math.Min(VIPJinBiLianZhi.Length - 1, vipLevel)];
                    needJinBi = JinBiLianZhi[0];
                    addExp = JinBiLianZhi[1];

                    JieRiMultAwardActivity activity = HuodongCachingMgr.GetJieRiMultAwardActivity();
                    if (null != activity)
                    {
                        JieRiMultConfig config = activity.GetConfig((int)MultActivityType.ZhuanHuanCount);
                        if (null != config)
                        {
                            lianZhiMaxCount = lianZhiMaxCount * ((int)config.GetMult() + 1);
                        }
                        config = activity.GetConfig((int)MultActivityType.ZhuanHuanAward);
                        if (null != config)
                        {
                            addExp += (int)(addExp * config.GetMult());
                        }
                    }
                }
                else if (type == 1)
                {
                    useMsg = "绑钻炼制";
                    lianZhiCount = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiBangZuanCount);
                    lianZhiDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiBangZuanDayID);
                    lianZhiMaxCount = BangZuanLianZhi[2] + VIPBangZuanLianZhi[Math.Min(VIPBangZuanLianZhi.Length - 1, vipLevel)];
                    needBangZuan = BangZuanLianZhi[0];
                    addXingHun = BangZuanLianZhi[1];

                    JieRiMultAwardActivity activity = HuodongCachingMgr.GetJieRiMultAwardActivity();
                    if (null != activity)
                    {
                        JieRiMultConfig config = activity.GetConfig((int)MultActivityType.ZhuanHuanCount);
                        if (null != config)
                        {
                            lianZhiMaxCount = lianZhiMaxCount * ((int)config.GetMult() + 1);
                        }
                        config = activity.GetConfig((int)MultActivityType.ZhuanHuanAward);
                        if (null != config)
                        {
                            addXingHun += (int)(addXingHun * config.GetMult());
                        }
                    }
                }
                else if (type == 2)
                {
                    useMsg = "钻石炼制";
                    lianZhiCount = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiZuanShiCount);
                    lianZhiDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.LianZhiZuanShiDayID);
                    lianZhiMaxCount = ZuanShiLianZhi[4] + VIPZuanShiLianZhi[Math.Min(VIPZuanShiLianZhi.Length - 1, vipLevel)];
                    needZuanShi = ZuanShiLianZhi[0];
                    addExp = ZuanShiLianZhi[1];
                    addXingHun = ZuanShiLianZhi[2];
                    addJinBi = ZuanShiLianZhi[3];

                    JieRiMultAwardActivity activity = HuodongCachingMgr.GetJieRiMultAwardActivity();
                    if (null != activity)
                    {
                        JieRiMultConfig config = activity.GetConfig((int)MultActivityType.ZhuanHuanCount);
                        if (null != config)
                        {
                            lianZhiMaxCount = lianZhiMaxCount * ((int)config.GetMult() + 1);
                        }
                        config = activity.GetConfig((int)MultActivityType.ZhuanHuanAward);
                        if (null != config)
                        {
                            addExp += (int)(addExp * config.GetMult());
                            addXingHun += (int)(addXingHun * config.GetMult());
                            addJinBi += (int)(addJinBi * config.GetMult());
                        }
                    }
                }

                //炼制日期已经不是今天,则次数归零
                if (lianZhiDayID != dayID)
                {
                    lianZhiCount = 0;
                }

                //如果传入0及以下的次数,则默认为最大次数
                if (count <= 0)
                {
                    count = lianZhiMaxCount - lianZhiCount;
                }

                //验证剩余次数
                if (count <= 0 || lianZhiCount + count > lianZhiMaxCount)
                {
                    result[0] = StdErrorCode.Error_No_Residue_Degree;
                    client.sendCmd(nID, result);
                }
                else
                {
                    needJinBi *= count;
                    needBangZuan *= count;
                    needZuanShi *= count;
                    addExp *= count;
                    addXingHun *= count;
                    addJinBi *= count;

                    addExp = Global.GetExpMultiByZhuanShengExpXiShu(client, addExp);

                    //扣除消耗所需
                    if (needJinBi > 0 && !Global.SubBindTongQianAndTongQian(client, needJinBi, useMsg))
                    {
                        result[0] = StdErrorCode.Error_JinBi_Not_Enough;
                        client.sendCmd(nID, result);
                    }
                    else if (needBangZuan > 0 && !GameManager.ClientMgr.SubUserGold(client, needBangZuan, useMsg))
                    {
                        result[0] = StdErrorCode.Error_BangZuan_Not_Enough;
                        client.sendCmd(nID, result);
                    }
                    else if (needZuanShi > 0 && !GameManager.ClientMgr.SubUserMoney(client, needZuanShi, useMsg))
                    {
                        result[0] = StdErrorCode.Error_ZuanShi_Not_Enough;
                        client.sendCmd(nID, result);
                    }
                    else
                    {
                        //既然该扣的都扣除了,下面可以给奖励了
                        if (addExp > 0)
                        {
                            GameManager.ClientMgr.ProcessRoleExperience(client, addExp);
                        }
                        if (addJinBi > 0)
                        {
                            GameManager.ClientMgr.AddMoney1(client, addJinBi, useMsg);
                        }
                        if (addXingHun > 0)
                        {
                            GameManager.ClientMgr.ModifyStarSoulValue(client, addXingHun, useMsg, true);
                        }

                        //计次存盘
                        lianZhiCount += count;
                        lianZhiDayID = dayID;
                        if (type == 0)
                        {
                            // 七日活动
                            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.JinBiZhuanHuanTimes));

                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LianZhiJinBiCount, lianZhiCount, true);
                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LianZhiJinBiDayID, lianZhiDayID, true);
                        }
                        else if (type == 1)
                        {
                            // 七日活动
                            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.BangZuanZhuanHuanTimes));

                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LianZhiBangZuanCount, lianZhiCount, true);
                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LianZhiBangZuanDayID, lianZhiDayID, true);
                        }
                        else if (type == 2)
                        {
                            // 七日活动
                            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.ZuanShiZhuanHuanTimes));

                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LianZhiZuanShiCount, lianZhiCount, true);
                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LianZhiZuanShiDayID, lianZhiDayID, true);
                        }

                        //返回结果
                        client.sendCmd(nID, result);
                    }
                }
            }

            return true;
        }

        #endregion 处理方法

    }
}
