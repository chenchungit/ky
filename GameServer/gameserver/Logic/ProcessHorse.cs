using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
//using System.Windows.Forms;
using System.Windows;
using Server.Data;
using ProtoBuf;
using GameServer.Logic;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 坐骑强化和进阶管理
    /// </summary>
    public class ProcessHorse
    {
        /// <summary>
        /// 坐骑强化
        /// </summary>
        /// <param name="client"></param>
        /// <param name="horseDbID"></param>
        /// <param name="extPropIndex"></param>
        public static int ProcessHorseEnchance(GameClient client, int horseDbID, int extPropIndex, bool allowAutoBuy)
        {          
            //获取坐骑数据
            HorseData horseData = Global.GetHorseDataByDbID(client, horseDbID);
            if (null == horseData)
            {
                return - 1;
            }

            //判断要强化的属性是否超出了索引
            if (extPropIndex < 0 || extPropIndex >= (int)HorseExtIndexes.MaxVal)
            {
                return -10;
            }

            //获取强化需要的道具的物品ID
            int EnchanceNeedGoodsID = (int)GameManager.systemParamsList.GetParamValueIntByName("HorseEnchancseGoodsID");
            if (EnchanceNeedGoodsID <= 0)
            {
                return -20;
            }

            int[] horseExtNumIntArray = Global.HorseExtStr2IntArray(horseData.PropsNum);
            int[] horseExtPropIntArray = Global.HorseExtStr2IntArray(horseData.PropsVal);
            int horseExtNum = Global.GetHorseExtFieldIntVal(horseExtNumIntArray, (HorseExtIndexes)extPropIndex);
            int horseExtVal = Global.GetHorseExtFieldIntVal(horseExtPropIntArray, (HorseExtIndexes)extPropIndex);
            //if (horseExtNum >= Global.MaxEnchanceLevel)
            //{
            //    return -30;
            //}

            //获取坐骑强到指定级别时，指定属性的配置xml节点
            SystemXmlItem systemHorseEnchance = Global.GetHorseEnchanceXmlNode(horseExtNum + 1, (HorseExtIndexes)extPropIndex);
            if (null == systemHorseEnchance)
            {
                return -35;
            }

            //获取坐骑基础属性值
            int baseVal = Global.GetHorseBasePropVal(horseData.HorseID, (HorseExtIndexes)extPropIndex, null);

            //获取坐骑属性的上限值
            int propLimit = Global.GetHorsePropLimitVal(horseData.HorseID, (HorseExtIndexes)extPropIndex);
            //propLimit += baseVal;

            if ((baseVal + horseExtVal) >= propLimit)
            {
                return -40;
            }

            //本次强化需要的银两
            int needYinLiang = Global.GMax(systemHorseEnchance.GetIntValue("UseMoney"), 0);
            if (client.ClientData.YinLiang < needYinLiang)
            {
                return -60;
            }

            int needBuyGoodsNum = 0;//需要购买的
            int needSubGoodsNum = 0;//需要扣除的

            //本次强化需要的辅助物品的个数
            int needGoodsNum = Global.GMax(systemHorseEnchance.GetIntValue("HanTie"), 0);

            needSubGoodsNum = needGoodsNum;

            if (Global.GetTotalGoodsCountByID(client, EnchanceNeedGoodsID) < needGoodsNum)
            {
                if (allowAutoBuy)
                {
                    needSubGoodsNum = Global.GetTotalGoodsCountByID(client, EnchanceNeedGoodsID);
                    needBuyGoodsNum = needGoodsNum - needSubGoodsNum;
                }
                else
                {
                    return -70;
                }
            }

            bool usedBinding = false;
            bool usedTimeLimited = false;

            //自动扣除物品
            if (needSubGoodsNum > 0)
            {
                //扣除物品
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, EnchanceNeedGoodsID, needSubGoodsNum, false, out usedBinding, out usedTimeLimited))
                {
                    return -70;
                }
            }

            //自动扣除money
            if (needBuyGoodsNum > 0)
            {
                //自动扣除元宝购买
                int retAuto = Global.SubUserMoneyForGoods(client, EnchanceNeedGoodsID, needBuyGoodsNum, "坐骑强化");

                if (retAuto <= 0)
                {
                    return retAuto;
                }
            }

            //扣除银两
            if (!GameManager.ClientMgr.SubUserYinLiang(Global._TCPManager.MySocketListener,
                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, needYinLiang, "坐骑强化"))
            {
                return -60;
            }

            int successRate = Global.GMax(systemHorseEnchance.GetIntValue("SucceedRate"), 0);
            int randNum = Global.GetRandomNumber(0, 101);

            if (client.ClientData.TempHorseEnchanceRate != 1) //临时测试加成功率
            {
                successRate *= client.ClientData.TempHorseEnchanceRate;
                successRate = Global.GMin(100, successRate);
            }

            if (randNum > successRate)
            {
                return -1000;
            }

            int addPropValue = Global.GMax(systemHorseEnchance.GetIntValue("PropVal"), 0);

            //处理先前的坐骑
            if (client.ClientData.HorseDbID > 0 && horseDbID == client.ClientData.HorseDbID)
            {
                //减去buffer属性值
                //加载骑乘的属性
                /// 将坐骑的扩展属性加入Buffer中
                Global.UpdateHorseDataProps(client, false);
            }

            //进行坐骑属性的强化，并存入数据库中
            int ret = 0;
            if (Global.UpdateHorsePropsDBCommand(Global._TCPManager.TcpOutPacketPool, client, horseData.DbID, (HorseExtIndexes)extPropIndex, addPropValue, 1) < 0)
            {
                ret = -2000;
            }

            //如果成功，并且坐骑处于骑乘状态
            if (client.ClientData.HorseDbID > 0 && horseDbID == client.ClientData.HorseDbID)
            {
                //减去buffer属性值
                //加载骑乘的属性
                /// 将坐骑的扩展属性加入Buffer中
                Global.UpdateHorseDataProps(client, true);

                if (0 == ret) //如果成功了, 否则没变化，不通知
                {
                    //计算坐骑的积分值
                    client.ClientData.RoleHorseJiFen = Global.CalcHorsePropsJiFen(horseData);

                    //通知客户端属性变化
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                    // 总生命值和魔法值变化通知(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }
            }

            if (0 == ret) //如果成功了, 否则没变化，不通知
            {
                //判断坐骑的属性是否已经满了?
                if (Global.IsHorsePropsFull(horseData))
                {
                    //坐骑强化完毕
                    Global.BroadcastHorseEnchanceOk(client, horseData.HorseID);
                }
            }

            return ret;
        }

        /// <summary>
        /// 坐骑快速全部属性强化
        /// </summary>
        /// <param name="client"></param>
        /// <param name="horseDbID"></param>
        /// <param name="extPropIndex"></param>
        public static int ProcessHorseQuickAllEnchance(GameClient client, int horseDbID)
        {
            //获取坐骑数据
            HorseData horseData = Global.GetHorseDataByDbID(client, horseDbID);
            if (null == horseData)
            {
                return -1;
            }

            //判断坐骑的属性是否已经满了?
            if (Global.IsHorsePropsFull(horseData))
            {
                return -10;
            }

            //获取强化需要的道具的物品ID
            int chaoJiLianGuGoodsID = (int)GameManager.systemParamsList.GetParamValueIntByName("ChaoJiLianGuGoodsID");
            if (chaoJiLianGuGoodsID <= 0)
            {
                return -20;
            }

            //本次强化需要的银两
            int needYinLiang = Global.QuickHorseExtPropNeedYinLiang;
            if (client.ClientData.YinLiang < needYinLiang)
            {
                return -30;
            }

            //本次强化需要的辅助物品的个数
            if (Global.GetTotalGoodsCountByID(client, chaoJiLianGuGoodsID) < 1)
            {
                return -40;
            }

            bool usedBinding = false;
            bool usedTimeLimited = false;

            //扣除物品
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, chaoJiLianGuGoodsID, 1, false, out usedBinding, out usedTimeLimited))
            {
                return -60;
            }

            //扣除银两
            if (!GameManager.ClientMgr.SubUserYinLiang(Global._TCPManager.MySocketListener,
                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, needYinLiang, "坐骑快速全部属性强化"))
            {
                return -70;
            }

            //处理先前的坐骑
            if (client.ClientData.HorseDbID > 0 && horseDbID == client.ClientData.HorseDbID)
            {
                //减去buffer属性值
                //加载骑乘的属性
                /// 将坐骑的扩展属性加入Buffer中
                Global.UpdateHorseDataProps(client, false);
            }

            for (int i = (int)HorseExtIndexes.Attack; i < (int)HorseExtIndexes.MaxVal; i++)
            {
                int[] horseExtNumIntArray = Global.HorseExtStr2IntArray(horseData.PropsNum);
                int[] horseExtPropIntArray = Global.HorseExtStr2IntArray(horseData.PropsVal);
                int horseExtNum = Global.GetHorseExtFieldIntVal(horseExtNumIntArray, (HorseExtIndexes)i);
                int horseExtVal = Global.GetHorseExtFieldIntVal(horseExtPropIntArray, (HorseExtIndexes)i);


                //获取坐骑基础属性值
                int baseVal = Global.GetHorseBasePropVal(horseData.HorseID, (HorseExtIndexes)i, null);

                //获取坐骑属性的上限值
                int propLimit = Global.GetHorsePropLimitVal(horseData.HorseID, (HorseExtIndexes)i);
                //propLimit += baseVal;

                if ((baseVal + horseExtVal) >= propLimit)
                {
                    continue;
                }

                //坐骑的最大强化次数
                int maxEnchanceLevel = Global.GetHorseEnchanceNum(horseData.HorseID);

                int addNum = maxEnchanceLevel - horseExtNum;
                int addPropValue = propLimit - baseVal - horseExtVal;

                //进行坐骑属性的强化，并存入数据库中
                Global.UpdateHorsePropsDBCommand(Global._TCPManager.TcpOutPacketPool, client, horseData.DbID, (HorseExtIndexes)i, addPropValue, addNum);
            }

            //如果成功，并且坐骑处于骑乘状态
            if (client.ClientData.HorseDbID > 0 && horseDbID == client.ClientData.HorseDbID)
            {
                //减去buffer属性值
                //加载骑乘的属性
                /// 将坐骑的扩展属性加入Buffer中
                Global.UpdateHorseDataProps(client, true);

                {
                    //计算坐骑的积分值
                    client.ClientData.RoleHorseJiFen = Global.CalcHorsePropsJiFen(horseData);

                    //通知客户端属性变化
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                    // 总生命值和魔法值变化通知(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }
            }

            {
                //判断坐骑的属性是否已经满了?
                if (Global.IsHorsePropsFull(horseData))
                {
                    //坐骑强化完毕
                    Global.BroadcastHorseEnchanceOk(client, horseData.HorseID);
                }
            }

            return 0;
        }

        /// <summary>
        /// 坐骑进阶
        /// </summary>
        /// <param name="client"></param>
        /// <param name="horseDbID"></param>
        public static int ProcessHorseUpgrade(GameClient client, int horseDbID, bool allowAutoBuy)
        {
            //获取坐骑数据
            HorseData horseData = Global.GetHorseDataByDbID(client, horseDbID);
            if (null == horseData)
            {
                return -1;
            }

            SystemXmlItem horseUpXmlNode = Global.GetHorseUpXmlNode(horseData.HorseID + 1);
            if (null == horseUpXmlNode)
            {
                return -35;
            }

            //int needRoleLevel = horseUpXmlNode.GetIntValue("LevelLimit");
            //if (client.ClientData.Level < needRoleLevel)
            //{
            //    return -38;
            //}

            //获取强化需要的道具的物品ID
            int horseUpgradeGoodsID = 0;
            int horseUpgradeGoodsNum = 0;

            //解析进阶需要的物品ID和数量
            Global.ParseHorseJinJieFu(horseData.HorseID, out horseUpgradeGoodsID, out horseUpgradeGoodsNum, horseUpXmlNode);
            if (horseUpgradeGoodsID <= 0)
            {
                return -20;
            }

            if (horseData.HorseID >= Global.MaxHorseID)
            {
                return -30;
            }

            //本次强化需要的银两
            int needYinLiang = Global.GMax(horseUpXmlNode.GetIntValue("UseYinLiang"), 0);
            needYinLiang = Global.RecalcNeedYinLiang(needYinLiang); //判断银两是否折半
            if (client.ClientData.YinLiang < needYinLiang)
            {
                return -60;
            }

            int needBuyGoodsNum = 0;//需要购买的
            int needSubGoodsNum = 0;//需要扣除的

            //本次强化需要的辅助物品的个数
            int needGoodsNum = Global.GMax(horseUpgradeGoodsNum, 0);

            needSubGoodsNum = needGoodsNum;

            if (Global.GetTotalGoodsCountByID(client, horseUpgradeGoodsID) < needGoodsNum)
            {
                if (allowAutoBuy)
                {
                    needSubGoodsNum = Global.GetTotalGoodsCountByID(client, horseUpgradeGoodsID);
                    needBuyGoodsNum = needGoodsNum - needSubGoodsNum;
                }
                else
                {
                    return -70;
                }
            }

            bool usedBinding = false;
            bool usedTimeLimited = false;

            //自动扣除物品
            if (needSubGoodsNum > 0)
            {
                //扣除物品
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, horseUpgradeGoodsID, needSubGoodsNum, false, out usedBinding, out usedTimeLimited))
                {
                    return -70;
                }
            }

            //自动扣除money
            if (needBuyGoodsNum > 0)
            {
                //自动扣除元宝购买
                int ret = Global.SubUserMoneyForGoods(client, horseUpgradeGoodsID, needBuyGoodsNum, "坐骑进阶");

                if (ret <= 0)
                {
                    return ret;
                }
            }

            //扣除银两
            if (!GameManager.ClientMgr.SubUserYinLiang(Global._TCPManager.MySocketListener,
                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, needYinLiang, "坐骑进阶"))
            {
                return -60;
            }

            int horseOne = 110000 - horseUpXmlNode.GetIntValue("HorseOne");
            int horseTwo = 110000 - horseUpXmlNode.GetIntValue("HorseTwo");
            double horseThree = horseUpXmlNode.GetDoubleValue("HorseThree");
            //int horseBlessPoint = horseUpXmlNode.GetIntValue("BlessPoint");

            //获取坐骑的失败积分
            int jinJieFailedNum = Global.GetHorseFailedNum(horseData);

            //判断如果还没到真概率，则直接返回失败
            if (jinJieFailedNum < horseTwo)
            {
                //if (!usedTimeLimited) //不是限时的道具
                {
                    //记录失败次数
                    //horseData.JinJieFailedNum += 1;
                    Global.AddHorseFailedNum(horseData, 1);
                }
                //else
                //{
                //    Global.AddHorseTempJiFen(horseData, 1);
                //}

                //记录失败次数
                Global.UpdateHorseIDDBCommand(Global._TCPManager.TcpOutPacketPool, client, horseData.DbID, horseData.HorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID);

                //写入角色进阶坐骑的行为日志
                Global.AddRoleHorseUpgradeEvent(client, horseData.DbID, horseData.HorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID, "失败");

                //改变坐骑的临时ID
                //Global.ChangeTempHorseID(client, horseData.HorseID + 1);

                return -1000;
            }

            //判断如果还没到必定成功的幸运值，则判断随机数
            if (jinJieFailedNum < (horseOne - 1))
            {
                int successRate = (int)(horseThree * 10000);
                int randNum = Global.GetRandomNumber(1, 10001);

                if (client.ClientData.TempHorseUpLevelRate != 1) //临时测试加成功率
                {
                    successRate *= client.ClientData.TempHorseUpLevelRate;
                    successRate = Global.GMin(10000, successRate);
                }

                if (randNum > successRate)
                {
                    //if (!usedTimeLimited) //不是限时的道具
                    {
                        //记录失败次数
                        //horseData.JinJieFailedNum += 1;
                        Global.AddHorseFailedNum(horseData, 1);
                    }
                    //else
                    //{
                    //    Global.AddHorseTempJiFen(horseData, 1);
                    //}

                    //记录失败次数
                    Global.UpdateHorseIDDBCommand(Global._TCPManager.TcpOutPacketPool, client, horseData.DbID, horseData.HorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID);

                    //写入角色进阶坐骑的行为日志
                    Global.AddRoleHorseUpgradeEvent(client, horseData.DbID, horseData.HorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID, "失败");

                    //改变坐骑的临时ID
                    //Global.ChangeTempHorseID(client, horseData.HorseID + 1);

                    return -1000;
                }
            }

            //处理坐骑进阶
            return ProcessHorseUpgradeNow(client, horseDbID, horseData);
        }

        /// <summary>
        /// 处理坐骑进阶
        /// </summary>
        private static int ProcessHorseUpgradeNow(GameClient client, int horseDbID, HorseData horseData)
        {
            //处理先前的坐骑
            if (client.ClientData.HorseDbID > 0 && horseDbID == client.ClientData.HorseDbID)
            {
                //减去buffer属性值
                //加载骑乘的属性
                /// 将坐骑的扩展属性加入Buffer中
                Global.UpdateHorseDataProps(client, false);
            }

            int oldHorseID = horseData.HorseID;
            int newHorseID = horseData.HorseID + 1;

            //强制清空临时的积分
            Global.AddHorseTempJiFen(horseData, 0);

            horseData.JinJieFailedDayID = TimeUtil.NowDateTime().DayOfYear;
            horseData.JinJieFailedNum = 0; //清空失败的积分

            //进行坐骑属性的强化，并存入数据库中
            int ret = 0;
            if (Global.UpdateHorseIDDBCommand(Global._TCPManager.TcpOutPacketPool, client, horseData.DbID, newHorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID) < 0)
            {
                ret = -2000;
            }

            //写入角色进阶坐骑的行为日志
            Global.AddRoleHorseUpgradeEvent(client, horseData.DbID, horseData.HorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID, "成功");

            //如果成功，并且坐骑处于骑乘状态
            if (client.ClientData.HorseDbID > 0 && horseDbID == client.ClientData.HorseDbID)
            {
                //减去buffer属性值
                //加载骑乘的属性
                /// 将坐骑的扩展属性加入Buffer中
                Global.UpdateHorseDataProps(client, true);

                if (0 == ret) //如果成功了, 否则没变化，不通知
                {
                    //通知客户端属性变化
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                    // 总生命值和魔法值变化通知(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }

                //通知用户更新坐骑的形象
                if (0 == ret) //如果成功了, 否则没变化，不通知
                {
                    //计算坐骑的积分值
                    client.ClientData.RoleHorseJiFen = Global.CalcHorsePropsJiFen(horseData);

                    List<Object> objsList = Global.GetAll9Clients(client);

                    //通知骑乘的的指令信息
                    GameManager.ClientMgr.NotifyHorseCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                        0, (int)HorseCmds.On, horseDbID, horseData.HorseID, horseData.BodyID, objsList);

                    //坐骑进阶成功
                    Global.BroadcastHorseUpgradeOk(client, oldHorseID, newHorseID);
                }
            }

            return 0;
        }

        /// <summary>
        /// 获取当前正在骑乘的坐骑的进阶养成点
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetCurrentHorseBlessPoint(GameClient client)
        {
            int horseDbID = client.ClientData.HorseDbID;
            if (horseDbID <= 0)
            {
                return 0;
            }

            //获取坐骑数据
            HorseData horseData = Global.GetHorseDataByDbID(client, horseDbID);
            if (null == horseData)
            {
                return 0;
            }

            SystemXmlItem horseUpXmlNode = Global.GetHorseUpXmlNode(horseData.HorseID + 1);
            if (null == horseUpXmlNode)
            {
                return 0;
            }

            int horseBlessPoint = horseUpXmlNode.GetIntValue("BlessPoint");
            return horseBlessPoint;
        }

        /// <summary>
        /// 为指定的坐骑增加养成点(临时或者永久)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="horseDbID"></param>
        /// <param name="luckyGoodsID"></param>
        /// <returns></returns>
        public static int ProcessAddHorseAwardLucky(GameClient client, int luckyValue, bool usedTimeLimited, string getType)
        {
            if (0 == luckyValue)
            {
                return 0;
            }

            int horseDbID = client.ClientData.HorseDbID;
            if (horseDbID <= 0)
            {
                return -300;
            }

            //获取坐骑数据
            HorseData horseData = Global.GetHorseDataByDbID(client, horseDbID);
            if (null == horseData)
            {
                return -1;
            }

            SystemXmlItem horseUpXmlNode = Global.GetHorseUpXmlNode(horseData.HorseID + 1);
            if (null == horseUpXmlNode)
            {
                return -35;
            }

            int horseBlessPoint = horseUpXmlNode.GetIntValue("BlessPoint");

            //获取坐骑的失败积分
            int jinJieFailedNum = Global.GetHorseFailedNum(horseData);

            //如果已经是最高阶，则不需要再增加幸运点
            if (horseData.HorseID >= Global.MaxHorseID)
            {
                return -10;
            }

            int addLuckValue = luckyValue;
            addLuckValue = Global.GMin(addLuckValue, horseBlessPoint - jinJieFailedNum);
            addLuckValue = Global.GMax(0, addLuckValue);

            if (!usedTimeLimited)
            {
                //记录失败次数
                Global.AddHorseFailedNum(horseData, addLuckValue);
            }
            else
            {
                Global.AddHorseTempJiFen(horseData, addLuckValue);
            }

            //记录失败次数
            Global.UpdateHorseIDDBCommand(Global._TCPManager.TcpOutPacketPool, client, horseData.DbID, horseData.HorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID);

            //写入角色进阶坐骑的行为日志
            Global.AddRoleHorseUpgradeEvent(client, horseData.DbID, horseData.HorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID, getType);

            return addLuckValue;
        }

        /// <summary>
        /// 为指定的坐骑增加养成点(临时)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="horseDbID"></param>
        /// <param name="luckyGoodsID"></param>
        /// <returns></returns>
        public static int ProcessAddHorseLucky(GameClient client, int horseDbID, int luckyGoodsID)
        {
            //获取坐骑数据
            HorseData horseData = Global.GetHorseDataByDbID(client, horseDbID);
            if (null == horseData)
            {
                return -1;
            }

            SystemXmlItem horseUpXmlNode = Global.GetHorseUpXmlNode(horseData.HorseID + 1);
            if (null == horseUpXmlNode)
            {
                return -35;
            }

            int horseOne = 110000 - horseUpXmlNode.GetIntValue("HorseOne");
            int horseTwo = 110000 - horseUpXmlNode.GetIntValue("HorseTwo");
            //int horseBlessPoint = horseUpXmlNode.GetIntValue("BlessPoint");

            //获取坐骑的失败积分
            int jinJieFailedNum = Global.GetHorseFailedNum(horseData);

            //判断如果已经到了幸运点 - 1，则提示用户不需要使用祝福丹
            if (jinJieFailedNum >= (horseOne - 1))
            {
                return -100;
            }

            //获取增加幸运点需要的物品ID
            int[] allHorseLuckyGoodsIDs = GameManager.systemParamsList.GetParamValueIntArrayByName("AllHorseLuckyGoodsIDs");
            int[] allHorseLuckyGoodsIDsToLucky = GameManager.systemParamsList.GetParamValueIntArrayByName("AllHorseLuckyGoodsIDsToLucky");
            if (null == allHorseLuckyGoodsIDs || null == allHorseLuckyGoodsIDsToLucky || allHorseLuckyGoodsIDs.Length != allHorseLuckyGoodsIDsToLucky.Length)
            {
                return -2;
            }

            //如果已经是最高阶，则不需要再增加幸运点
            if (horseData.HorseID >= Global.MaxHorseID)
            {
                return -10;
            }

            //判断物品数量
            if (Global.GetTotalGoodsCountByID(client, luckyGoodsID) <= 0)
            {
                return -20;
            }

            bool usedBinding = false;
            bool usedTimeLimited = false;

            //扣除物品
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, luckyGoodsID, 1, false, out usedBinding, out usedTimeLimited))
            {
                return -30;
            }

            int addLuckValue = 0;
            for (int i = 0; i < allHorseLuckyGoodsIDs.Length; i++)
            {
                if (allHorseLuckyGoodsIDs[i] == luckyGoodsID)
                {
                    addLuckValue = allHorseLuckyGoodsIDsToLucky[i];
                    break;
                }
            }

            //addLuckValue = Global.GMin(addLuckValue, horseBlessPoint - jinJieFailedNum);
            addLuckValue = Global.GMax(0, addLuckValue);

            //if (!usedTimeLimited) //不是限时的道具
            {
                //记录失败次数
                //horseData.JinJieFailedNum += addLuckValue;
                Global.AddHorseFailedNum(horseData, addLuckValue);
            }
            //else
            //{
            //    Global.AddHorseTempJiFen(horseData, addLuckValue);
            //}

            //记录失败次数
            Global.UpdateHorseIDDBCommand(Global._TCPManager.TcpOutPacketPool, client, horseData.DbID, horseData.HorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID);

            //写入角色进阶坐骑的行为日志
            Global.AddRoleHorseUpgradeEvent(client, horseData.DbID, horseData.HorseID, horseData.JinJieFailedNum, Global.GetHorseStrTempTime(horseData), horseData.JinJieTempNum, horseData.JinJieFailedDayID, "祝福丹");

            return addLuckValue;
        }
    }
}
