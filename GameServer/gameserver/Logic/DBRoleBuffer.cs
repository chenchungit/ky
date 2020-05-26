#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System.Windows;
using System.Threading;
//using System.Windows.Documents;
using GameServer.Server;
using GameServer.Logic.NewBufferExt;
using GameServer.Core.Executor;
using Tmsk.Contract;

namespace GameServer.Logic
{
    /// <summary>
    /// 数据库中存储的buffer的管理
    /// </summary>
    public class DBRoleBufferManager
    {
        #region 生命和魔法

        /// <summary>
        /// 处理生命和魔法储备
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessLifeVAndMagicVReserve(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                RoleRelifeLog relifeLog = new RoleRelifeLog(client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.MapCode, "血瓶蓝瓶");

                BufferData bufferData = null;
                bool doRelife = false;

                //判断如果血量少于最大血量, 消耗品，不再卡主是否血满
                if (client.ClientData.CurrentLifeV < client.ClientData.LifeV)
                {
                    //判断此地图是否允许使用Buffer
                    if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.LifeVReserve))
                    {
                        bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.LifeVReserve);
                        if (null != bufferData)
                        {
                            if (bufferData.BufferVal > 0) //如果还有储备
                            {
                                doRelife = true;
                                relifeLog.hpModify = true;
                                relifeLog.oldHp = client.ClientData.CurrentLifeV;

                                int needLifeV = client.ClientData.LifeV - client.ClientData.CurrentLifeV;
                                int lifeRecoverNum = Global.GMax(0, (int)GameManager.systemParamsList.GetParamValueIntByName("LifeRecoverNum"));
                                lifeRecoverNum = (int)(lifeRecoverNum * (1.0 + RoleAlgorithm.GetLifeRecoverAddPercentV(client)));

                                needLifeV = Global.GMin(needLifeV, lifeRecoverNum);
                                needLifeV = (int)Global.GMin(needLifeV, (int)bufferData.BufferVal);

                                bufferData.BufferVal -= (int)Global.GMin((int)bufferData.BufferVal, lifeRecoverNum); //储备减少

                                needLifeV += client.ClientData.CurrentLifeV;
                                client.ClientData.CurrentLifeV = (int)Global.GMin(client.ClientData.LifeV, needLifeV);
                                relifeLog.newHp = client.ClientData.CurrentLifeV;
                                //GameManager.SystemServerEvents.AddEvent(string.Format("角色加血, roleID={0}({1}), Add={2}, Life={3}", client.ClientData.RoleID, client.ClientData.RoleName, needLifeV, client.ClientData.CurrentLifeV), EventLevels.Debug);

                                //将新的Buffer数据通知自己
                                GameManager.ClientMgr.NotifyBufferData(client, bufferData);
                            }
                        }
                    }
                }

                //判断如果魔量少于最大魔量, 消耗品，不再卡主是否蓝满
                if (client.ClientData.CurrentMagicV < client.ClientData.MagicV)
                {
                    //判断此地图是否允许使用Buffer
                    if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.MagicVReserve))
                    {
                        bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.MagicVReserve);
                        if (null != bufferData)
                        {
                            if (bufferData.BufferVal > 0) //如果还有储备
                            {
                                doRelife = true;
                                relifeLog.mpModify = true;
                                relifeLog.oldMp = client.ClientData.CurrentMagicV;

                                int needMagicV = client.ClientData.MagicV - client.ClientData.CurrentMagicV;
                                int magicRecoverNum = Global.GMax(0, (int)GameManager.systemParamsList.GetParamValueIntByName("MagicRecoverNum"));
                                magicRecoverNum = (int)(magicRecoverNum * (1.0 + RoleAlgorithm.GetMagicRecoverAddPercentV(client)));

                                needMagicV = Global.GMin(needMagicV, magicRecoverNum);
                                needMagicV = (int)Global.GMin(needMagicV, bufferData.BufferVal);

                                bufferData.BufferVal -= (int)Global.GMin(bufferData.BufferVal, magicRecoverNum); //储备减少

                                needMagicV += client.ClientData.CurrentMagicV;
                                client.ClientData.CurrentMagicV = (int)Global.GMin(client.ClientData.MagicV, needMagicV);
                                relifeLog.newMp = client.ClientData.CurrentMagicV;
                                //GameManager.SystemServerEvents.AddEvent(string.Format("角色加魔, roleID={0}({1}), Add={2}, Magic={3}", client.ClientData.RoleID, client.ClientData.RoleName, needMagicV, client.ClientData.CurrentMagicV), EventLevels.Debug);

                                //将新的Buffer数据通知自己
                                GameManager.ClientMgr.NotifyBufferData(client, bufferData);
                            }
                        }
                    }
                }

                MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);

                if (doRelife)
                {
                    //通知客户端怪已经加血加魔   
                    List<Object> listObjs = Global.GetAll9Clients(client);
                    GameManager.ClientMgr.NotifyOthersRelife(sl, pool, client, client.ClientData.MapCode, client.ClientData.CopyMapID, client.ClientData.RoleID, (int)client.ClientData.PosX, (int)client.ClientData.PosY, (int)client.ClientData.RoleDirection, client.ClientData.CurrentLifeV, client.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
                }
            }
        }

        /// <summary>
        /// 处理重生(减少血)
        /// </summary>
        /// <param name="client"></param>
        public static int ProcessHuZhaoSubLifeV(GameClient client, int subLifeV)
        {
            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                BufferData bufferData = null;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.TimeHUZHAONoShow))
                {
                    bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.TimeHUZHAONoShow);
                    if (null != bufferData)
                    {
                        if (bufferData.BufferVal > 0) //如果还有储备
                        {
                            int needLifeV = client.ClientData.LifeV - client.ClientData.CurrentLifeV;
                            HuZhaoBufferItem huZhaoBufferItem = client.MyBufferExtManager.FindBufferItem((int)BufferItemTypes.TimeHUZHAONoShow) as HuZhaoBufferItem;
                            if (huZhaoBufferItem != null)
                            {
                                needLifeV = Global.GMin(needLifeV, huZhaoBufferItem.InjuredV);
                                needLifeV = (int)Global.GMin(needLifeV, (int)bufferData.BufferVal);

                                bufferData.BufferVal -= (int)Global.GMin((int)bufferData.BufferVal, huZhaoBufferItem.InjuredV); //储备减少

                                subLifeV = Global.GMin(needLifeV, subLifeV);

                                GameManager.ClientMgr.NotifyBufferData(client, bufferData);
                            }
                        }
                        else
                        {
                            Global.RemoveBufferData(client, (int)BufferItemTypes.TimeHUZHAONoShow);
                            client.MyBufferExtManager.RemoveBufferItem((int)BufferItemTypes.TimeHUZHAONoShow);
                        }
                    }
                }
            }

            return subLifeV;
        }

        /// <summary>
        /// 处理重生(加速回血)
        /// </summary>
        /// <param name="client"></param>
        public static double ProcessHuZhaoRecoverPercent(GameClient client)
        {
            double percent = 0.0;

            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                BufferData bufferData = null;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.TimeHUZHAONoShow))
                {
                    bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.TimeHUZHAONoShow);
                    if (null != bufferData)
                    {
                        if (bufferData.BufferVal > 0) //如果还有储备
                        {
                            HuZhaoBufferItem huZhaoBufferItem = client.MyBufferExtManager.FindBufferItem((int)BufferItemTypes.TimeHUZHAONoShow) as HuZhaoBufferItem;
                            if (huZhaoBufferItem != null)
                            {
                                percent = huZhaoBufferItem.RecoverLifePercent;
                            }
                        }
                        else
                        {
                            Global.RemoveBufferData(client, (int)BufferItemTypes.TimeHUZHAONoShow);
                            client.MyBufferExtManager.RemoveBufferItem((int)BufferItemTypes.TimeHUZHAONoShow);
                        }
                    }
                }
            }

            return percent;
        }

        /// <summary>
        /// 处理无敌护照(不受伤)
        /// </summary>
        /// <param name="client"></param>
        public static int ProcessWuDiHuZhaoNoInjured(GameClient client, int subLifeV)
        {
            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                BufferData bufferData = null;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.TimeWUDIHUZHAONoShow))
                {
                    bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.TimeWUDIHUZHAONoShow);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            subLifeV = 0;
                        }
                        else
                        {
                            Global.RemoveBufferData(client, (int)BufferItemTypes.TimeWUDIHUZHAONoShow);
                        }
                    }
                }
            }

            return subLifeV;
        }

        /// <summary>
        /// 处理药品buffer，定时不计生命和蓝
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessTimeAddLifeMagic(GameClient client)
        {
            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                double lifeV = 0;
                double magicV = 0;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.TimeAddLifeMagic))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.TimeAddLifeMagic);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            int actionGoodsID = (int)bufferData.BufferVal;

                            //获取指定物品的公式列表
                            List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                            if (null != magicActionItemList && magicActionItemList[0].MagicActionID == MagicActionIDs.DB_TIME_LIFE_MAGIC)
                            {
                                if (nowTicks - client.ClientData.StartAddLifeMagicTicks >= (int)(magicActionItemList[0].MagicActionParams[3] * 1000))
                                {
                                    client.ClientData.StartAddLifeMagicTicks = nowTicks;

                                    lifeV = magicActionItemList[0].MagicActionParams[0];
                                    lifeV = (int)(lifeV * (1.0 + RoleAlgorithm.GetLifeRecoverAddPercentV(client)));

                                    magicV = magicActionItemList[0].MagicActionParams[1];
                                    magicV = (int)(magicV * (1.0 + RoleAlgorithm.GetMagicRecoverAddPercentV(client)));
                                }
                            }
                        }
                    }
                }

                RoleRelifeLog relifeLog = new RoleRelifeLog(client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.MapCode, "加血加蓝buff" + (int)BufferItemTypes.TimeAddLifeMagic);

                //如果需要加生命
                if (lifeV > 0.0)
                {
                    relifeLog.hpModify = true;
                    relifeLog.oldHp = client.ClientData.CurrentLifeV;
                    int needLifeV = client.ClientData.LifeV - client.ClientData.CurrentLifeV;
                    needLifeV = Global.GMin(needLifeV, (int)lifeV);
                    lifeV = needLifeV;

                    needLifeV += client.ClientData.CurrentLifeV;
                    client.ClientData.CurrentLifeV = (int)Global.GMin(client.ClientData.LifeV, needLifeV);
                    relifeLog.newHp = client.ClientData.CurrentLifeV;
                    //GameManager.SystemServerEvents.AddEvent(string.Format("角色加血, roleID={0}({1}), Add={2}, Life={3}", client.ClientData.RoleID, client.ClientData.RoleName, needLifeV, client.ClientData.CurrentLifeV), EventLevels.Debug);
                }

                //如果需要加魔法
                if (magicV > 0.0)
                {
                    relifeLog.mpModify = true;
                    relifeLog.oldMp = client.ClientData.CurrentMagicV;
                    int needMagicV = client.ClientData.MagicV - client.ClientData.CurrentMagicV;
                    needMagicV = Global.GMin(needMagicV, (int)magicV);
                    magicV = needMagicV;

                    needMagicV += client.ClientData.CurrentMagicV;
                    client.ClientData.CurrentMagicV = (int)Global.GMin(client.ClientData.MagicV, needMagicV);
                    relifeLog.newMp = client.ClientData.CurrentMagicV;
                    //GameManager.SystemServerEvents.AddEvent(string.Format("角色加魔, roleID={0}({1}), Add={2}, Magic={3}", client.ClientData.RoleID, client.ClientData.RoleName, needMagicV, client.ClientData.CurrentMagicV), EventLevels.Debug);
                }

                MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);

                if (lifeV > 0 || magicV > 0)
                {
                    //通知客户端怪已经加血加魔   
                    List<Object> listObjs = Global.GetAll9Clients(client);
                    GameManager.ClientMgr.NotifyOthersRelife(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                        client.ClientData.MapCode, client.ClientData.CopyMapID, client.ClientData.RoleID,
                        (int)client.ClientData.PosX, (int)client.ClientData.PosY, (int)client.ClientData.RoleDirection,
                        client.ClientData.CurrentLifeV, client.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
                }
            }
        }

        /// <summary>
        /// 处理药品buffer，定时不计生命
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessTimeAddLifeNoShow(GameClient client)
        {
            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                double lifeV = 0;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.TimeAddLifeNoShow))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.TimeAddLifeNoShow);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            int actionGoodsID = (int)bufferData.BufferVal;

                            //获取指定物品的公式列表
                            List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                            if (null != magicActionItemList && magicActionItemList[0].MagicActionID == MagicActionIDs.DB_TIME_LIFE_NOSHOW)
                            {
                                if (nowTicks - client.ClientData.StartAddLifeNoShowTicks >= (int)(magicActionItemList[0].MagicActionParams[2] * 1000))
                                {
                                    double addrate = 0.0d;

                                    client.ClientData.StartAddLifeNoShowTicks = nowTicks;
                                    lifeV = magicActionItemList[0].MagicActionParams[0];

                                    addrate += RoleAlgorithm.GetLifeRecoverAddPercentV(client);

                                    //[bing] 检查MagicActionIDs.HOLYWATER属性增加效果 针对以下物品检查 GoodsID=1000、1001、1002、1100、1101、1102
                                    if (1000 == bufferData.BufferVal
                                        || 1001 == bufferData.BufferVal
                                        || 1002 == bufferData.BufferVal
                                        || 1100 == bufferData.BufferVal
                                        || 1101 == bufferData.BufferVal
                                        || 1102 == bufferData.BufferVal)
                                    {
                                        addrate += client.ClientData.PropsCacheManager.GetExtProp((int)ExtPropIndexes.Holywater);
                                    }

                                    lifeV = (int)(lifeV * (1.0d + addrate));
                                }
                            }
                        }
                    }
                }

                //如果需要加生命
                if (lifeV > 0.0)
                {
                    RoleRelifeLog relifeLog = new RoleRelifeLog(client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.MapCode, "加血buff" + (int)BufferItemTypes.TimeAddLifeNoShow);
                    relifeLog.hpModify = true;
                    relifeLog.oldHp = client.ClientData.CurrentLifeV;
                    int needLifeV = client.ClientData.LifeV - client.ClientData.CurrentLifeV;
                    needLifeV = Global.GMin(needLifeV, (int)lifeV);
                    lifeV = needLifeV;

                    needLifeV += client.ClientData.CurrentLifeV;
                    client.ClientData.CurrentLifeV = (int)Global.GMin(client.ClientData.LifeV, needLifeV);
                    relifeLog.newHp = client.ClientData.CurrentLifeV;
                    MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);
                    //GameManager.SystemServerEvents.AddEvent(string.Format("角色加血, roleID={0}({1}), Add={2}, Life={3}", client.ClientData.RoleID, client.ClientData.RoleName, needLifeV, client.ClientData.CurrentLifeV), EventLevels.Debug);
                }

                if (lifeV > 0)
                {
                    //通知客户端怪已经加血加魔   
                    List<Object> listObjs = Global.GetAll9Clients(client);
                    GameManager.ClientMgr.NotifyOthersRelife(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, client.ClientData.MapCode, client.ClientData.CopyMapID, client.ClientData.RoleID,
                        (int)client.ClientData.PosX, (int)client.ClientData.PosY, (int)client.ClientData.RoleDirection,
                        client.ClientData.CurrentLifeV, client.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
                }
            }
        }

        /// <summary>
        /// 处理药品buffer，定时不计蓝
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessTimeAddMagicNoShow(GameClient client)
        {
            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                double magicV = 0;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.TimeAddMagicNoShow))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.TimeAddMagicNoShow);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            int actionGoodsID = (int)bufferData.BufferVal;

                            //获取指定物品的公式列表
                            List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                            if (null != magicActionItemList && magicActionItemList[0].MagicActionID == MagicActionIDs.DB_TIME_MAGIC_NOSHOW)
                            {
                                if (nowTicks - client.ClientData.StartAddMaigcNoShowTicks >= (int)(magicActionItemList[0].MagicActionParams[2] * 1000))
                                {
                                    client.ClientData.StartAddMaigcNoShowTicks = nowTicks;
                                    magicV = magicActionItemList[0].MagicActionParams[0];
                                    magicV = (int)(magicV * (1.0 + RoleAlgorithm.GetMagicRecoverAddPercentV(client)));
                                }
                            }
                        }
                    }
                }

                //如果需要加魔法
                if (magicV > 0.0)
                {
                    RoleRelifeLog relifeLog = new RoleRelifeLog(client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.MapCode, "加蓝buff" + (int)BufferItemTypes.TimeAddMagicNoShow);
                    relifeLog.mpModify = true;
                    relifeLog.oldMp = client.ClientData.CurrentMagicV;
                    int needMagicV = client.ClientData.MagicV - client.ClientData.CurrentMagicV;
                    needMagicV = Global.GMin(needMagicV, (int)magicV);
                    magicV = needMagicV;

                    needMagicV += client.ClientData.CurrentMagicV;
                    client.ClientData.CurrentMagicV = (int)Global.GMin(client.ClientData.MagicV, needMagicV);
                    relifeLog.newMp = client.ClientData.CurrentMagicV;
                    MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);
                    //GameManager.SystemServerEvents.AddEvent(string.Format("角色加魔, roleID={0}({1}), Add={2}, Magic={3}", client.ClientData.RoleID, client.ClientData.RoleName, needMagicV, client.ClientData.CurrentMagicV), EventLevels.Debug);
                }

                if (magicV > 0)
                {
                    //通知客户端怪已经加血加魔   
                    List<Object> listObjs = Global.GetAll9Clients(client);
                    GameManager.ClientMgr.NotifyOthersRelife(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, client.ClientData.MapCode, client.ClientData.CopyMapID, client.ClientData.RoleID,
                        (int)client.ClientData.PosX, (int)client.ClientData.PosY, (int)client.ClientData.RoleDirection,
                        client.ClientData.CurrentLifeV, client.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
                }
            }
        }

        /// <summary>
        /// 处理道士加血的buffer，定时不计生命
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessDSTimeAddLifeNoShow(GameClient client)
        {
            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                double lifeV = 0;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.DSTimeAddLifeNoShow))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.DSTimeAddLifeNoShow);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            int timeSlotSecs = (int)((bufferData.BufferVal >> 32) & 0x00000000FFFFFFFF);
                            int addLiefV = (int)((bufferData.BufferVal) & 0x00000000FFFFFFFF);

                            if (nowTicks - client.ClientData.DSStartDSAddLifeNoShowTicks >= (int)(timeSlotSecs * 1000))
                            {
                                client.ClientData.DSStartDSAddLifeNoShowTicks = nowTicks;
                                lifeV = addLiefV;
                                lifeV = (int)(lifeV * (1.0 + RoleAlgorithm.GetLifeRecoverAddPercentV(client)));
                            }
                        }
                    }
                }

                //如果需要加生命
                if (lifeV > 0.0)
                {
                    RoleRelifeLog relifeLog = new RoleRelifeLog(client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.MapCode, "加血buff" + (int)BufferItemTypes.DSTimeAddLifeNoShow);
                    relifeLog.hpModify = true;
                    relifeLog.oldHp = client.ClientData.CurrentLifeV;

                    int needLifeV = client.ClientData.LifeV - client.ClientData.CurrentLifeV;
                    needLifeV = Global.GMin(needLifeV, (int)lifeV);
                    lifeV = needLifeV;

                    needLifeV += client.ClientData.CurrentLifeV;
                    client.ClientData.CurrentLifeV = (int)Global.GMin(client.ClientData.LifeV, needLifeV);
                    relifeLog.newHp = client.ClientData.CurrentLifeV;
                    MonsterAttackerLogManager.Instance().AddRoleRelifeLog(relifeLog);
                    //GameManager.SystemServerEvents.AddEvent(string.Format("角色加血, roleID={0}({1}), Add={2}, Life={3}", client.ClientData.RoleID, client.ClientData.RoleName, needLifeV, client.ClientData.CurrentLifeV), EventLevels.Debug);
                }

                if (lifeV > 0)
                {
                    //通知客户端怪已经加血加魔   
                    List<Object> listObjs = Global.GetAll9Clients(client);
                    GameManager.ClientMgr.NotifyOthersRelife(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, client.ClientData.MapCode, client.ClientData.CopyMapID, client.ClientData.RoleID,
                        (int)client.ClientData.PosX, (int)client.ClientData.PosY, (int)client.ClientData.RoleDirection,
                        client.ClientData.CurrentLifeV, client.ClientData.CurrentMagicV, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
                }
            }
        }

        #endregion 生命和魔法

        #region 灵力

        /// <summary>
        /// 处理灵力储备
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessLingLiVReserve(SocketListener sl, TCPOutPacketPool pool, GameClient client)
        {
            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.LingLiVReserve))
            {
                return;
            }

            int needLingLiV = 0;
            if (client.ClientData.InterPower >= (int)(int)LingLiConsts.MaxLingLiVal)
            {
                return;
            }

            needLingLiV = (int)(int)LingLiConsts.MaxLingLiVal - client.ClientData.InterPower;
            if (needLingLiV <= 0) return;

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.LingLiVReserve);
            if (null == bufferData)
            {
                return;
            }

            if (bufferData.BufferVal <= 0) //如果没有了储备
            {
                return;
            }

            int lingLiV = (int)Global.GMin(needLingLiV, bufferData.BufferVal);

            bufferData.BufferVal -= lingLiV; //储备减少
            client.ClientData.InterPower += lingLiV;

            //将新的Buffer数据通知自己
            GameManager.ClientMgr.NotifyBufferData(client, bufferData);

            //通知角色自动增长内力的指令信息
            GameManager.ClientMgr.NotifyUpdateInterPowerCmd(sl, pool, client, 0);
        }

        /// <summary>
        /// 处理灵力储备
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessLingLiVReserve2(SocketListener sl, TCPOutPacketPool pool, GameClient client, BufferData bufferData)
        {
            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.LingLiVReserve))
            {
                return;
            }

            int needLingLiV = 0;
            if (client.ClientData.InterPower >= (int)(int)LingLiConsts.MaxLingLiVal)
            {
                return;
            }

            needLingLiV = (int)(int)LingLiConsts.MaxLingLiVal - client.ClientData.InterPower;
            if (needLingLiV <= 0) return;

            if (null == bufferData) return;

            if (bufferData.BufferVal <= 0) //如果没有了储备
            {
                return;
            }

            int lingLiV = (int)Global.GMin(needLingLiV, bufferData.BufferVal);

            bufferData.BufferVal -= lingLiV; //储备减少
            client.ClientData.InterPower += lingLiV;

            //通知角色自动增长内力的指令信息
            GameManager.ClientMgr.NotifyUpdateInterPowerCmd(sl, pool, client, 0);
        }

        /// <summary>
        /// 处理双倍灵力卡
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessDblLingLi(GameClient client)
        {
            double ret = 1.0;

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.DblLingLi))
            {
                return ret;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.DblLingLi);
            if (null == bufferData)
            {
                return ret;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                ret = 2.0;
            }

            return ret;
        }

        #endregion 灵力

        #region 战斗属性刷新

        /// <summary>
        /// 刷新战斗属性
        /// </summary>
        /// <param name="client"></param>
        public static void RefreshTimePropBuffer(GameClient client, BufferItemTypes bufferItemType)
        {
            BufferData bufferData = Global.GetBufferDataFromDict(client, (int)bufferItemType);
            if (null == bufferData)
            {
                bufferData = Global.GetBufferDataByID(client, (int)bufferItemType);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        Global.AddBufferDataIntoDict(client, bufferData.BufferID, bufferData);

                        //通知客户端属性变化
                        client.delayExecModule.SetDelayExecProc(DelayExecProcIds.NotifyRefreshProps);
                    }
                }
            }
            else
            {
                long nowTicks = TimeUtil.NOW();
                if ((nowTicks - bufferData.StartTime) >= (bufferData.BufferSecs * 1000))
                {
                    //将BufferData加入字典中保存
                    Global.AddBufferDataIntoDict(client, bufferData.BufferID, null);

                    //通知客户端属性变化
                    client.delayExecModule.SetDelayExecProc(DelayExecProcIds.NotifyRefreshProps);
                }
            }
        }

        #endregion 战斗属性刷新

        #region 传奇新加的属性buffer

        /// <summary>
        /// 获取持续时间加属性值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetTimeAddProp(GameClient client, BufferItemTypes bufferItemType)
        {
            int nRet = 0;

            BufferData bufferData = Global.GetBufferDataByID(client, (int)bufferItemType);
            if (null == bufferData)
            {
                return nRet;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                // 处理1级属性临时BUFF -- 特别注意 程序员不要乱动这块代码 [2/11/2014 LiaoWei]
                if (bufferData.BufferID == (int)BufferItemTypes.ADDTEMPStrength || bufferData.BufferID == (int)BufferItemTypes.ADDTEMPIntelligsence ||
                    bufferData.BufferID == (int)BufferItemTypes.ADDTEMPDexterity || bufferData.BufferID == (int)BufferItemTypes.ADDTEMPConstitution)
                {
                    int actionGoodsID = -1;
                    actionGoodsID = (int)bufferData.BufferVal;

                    if (actionGoodsID <= 0)
                        return nRet;

                    // 获取指定物品的公式列表
                    List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                    int nValue = -1;
                    nValue = (int)magicActionItemList[0].MagicActionParams[0];
                    if (nValue > -1)
                    {
                        nRet = nValue;
                    }
                }
            }

            return nRet;
        }

        /// <summary>
        /// 获取BUFF加一级属性的值
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetBuffAddProp(GameClient client, BufferItemTypes bufferItemType)
        {
            int nRet = 0;

            BufferData bufferData = Global.GetBufferDataByID(client, (int)bufferItemType);
            if (null == bufferData)
            {
                return nRet;
            }

            if (bufferData.BufferID == (int)BufferItemTypes.ADDTEMPStrength || bufferData.BufferID == (int)BufferItemTypes.ADDTEMPIntelligsence ||
                bufferData.BufferID == (int)BufferItemTypes.ADDTEMPDexterity || bufferData.BufferID == (int)BufferItemTypes.ADDTEMPConstitution)
            {
                int actionGoodsID = -1;
                actionGoodsID = (int)bufferData.BufferVal;

                if (actionGoodsID <= 0)
                    return nRet;

                // 获取指定物品的公式列表
                List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                int nValue = -1;
                nValue = (int)magicActionItemList[0].MagicActionParams[0];
                if (nValue > -1)
                {
                    nRet = nValue;
                }
            }

            return nRet;
        }

        /// <summary>
        /// 持续时间加属性
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessTimeAddProp(GameClient client, BufferItemTypes bufferItemType)
        {
            double ret = 0.0;

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)bufferItemType))
            {
                return ret;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)bufferItemType);
            if (null == bufferData)
            {
                return ret;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                // 处理1级属性临时BUFF -- 特别注意 程序员不要乱动这块代码 [2/11/2014 LiaoWei]
                //if (bufferData.BufferID == (int)BufferItemTypes.ADDTEMPStrength || bufferData.BufferID == (int)BufferItemTypes.ADDTEMPIntelligsence ||
                //    bufferData.BufferID == (int)BufferItemTypes.ADDTEMPDexterity || bufferData.BufferID == (int)BufferItemTypes.ADDTEMPConstitution)
                //{
                //    return ret;
                //}
                //else
                {
                    int actionGoodsID = (int)bufferData.BufferVal;

                    //获取指定物品的公式列表
                    List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                    ret = magicActionItemList[0].MagicActionParams[0];
                }
            }
            else
            {
                // 处理1级属性临时BUFF Begin [2/11/2014 LiaoWei]
                if (bufferData.BufferID == (int)BufferItemTypes.ADDTEMPStrength || bufferData.BufferID == (int)BufferItemTypes.ADDTEMPIntelligsence ||
                    bufferData.BufferID == (int)BufferItemTypes.ADDTEMPDexterity || bufferData.BufferID == (int)BufferItemTypes.ADDTEMPConstitution)
                {
                    long bufferVal = 0;
                    lock (bufferData) //加锁,清空BufferVal 为了在执行添加时判断是否真正加属性点的操作(modified:2014-11-3)
                    {
                        nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < ((long)bufferData.BufferSecs * 1000))
                        {
                            return ret;
                        }

                        bufferVal = bufferData.BufferVal;
                        bufferData.BufferVal = 0;
                        if (bufferVal > 0)
                        {
                            Global.RemoveBufferData(client, (int)bufferItemType);
                        }
                        else
                        {
                            return ret;
                        }
                    }

                    int actionGoodsID = (int)bufferVal;

                    if (actionGoodsID <= 0)
                        return ret;

                    client.delayExecModule.SetDelayExecProc(DelayExecProcIds.RecalcProps, DelayExecProcIds.NotifyRefreshProps);
                }
                // 处理1级属性临时BUFF End [2/11/2014 LiaoWei]
            }

            return ret;
        }

        #endregion 传奇新加的属性buffer

        #region 成就/经脉/武学等buffer

        /// <summary>
        /// 持续时间加属性
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessTempBufferProp(GameClient client, ExtPropIndexes expPropIndex)
        {
            return 0.0;
        }

        #endregion 成就/经脉/武学等buffer

        #region 攻击力

        /// <summary>
        /// 处理狂攻符咒
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessAddTempAttack(GameClient client)
        {
            double ret = 0.0;

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.AddTempAttack))
            {
                return ret;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.AddTempAttack);
            if (null == bufferData)
            {
                return ret;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                ret = 0.10;
            }

            return ret;
        }

        /// <summary>
        /// 添加攻击的buffer
        /// </summary>
        /// <param name="client"></param>
        public static void AddAttackBuffer(GameClient client)
        {
            if (null == client.ClientData.AddTempAttackBufferData) //当前没有狂攻符咒
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.AddTempAttack);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        client.ClientData.AddTempAttackBufferData = bufferData;

                        // 攻击力发生变化的通知(同一个地图才需要通知)
                        //通知客户端属性变化
                        GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                    }
                }
            }
        }

        /// <summary>
        /// 去除攻击的buffer
        /// </summary>
        /// <param name="client"></param>
        public static void RemoveAttackBuffer(GameClient client)
        {
            BufferData bufferData = client.ClientData.AddTempAttackBufferData;
            if (null != bufferData) //有狂攻符咒
            {
                long nowTicks = TimeUtil.NOW();
                if ((nowTicks - bufferData.StartTime) >= (bufferData.BufferSecs * 1000))
                {
                    client.ClientData.AddTempAttackBufferData = null;

                    // 攻击力发生变化的通知(同一个地图才需要通知)
                    //通知客户端属性变化
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }
            }
        }

        #endregion 攻击力

        #region 防御力

        /// <summary>
        /// 处理防御符咒
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessAddTempDefense(GameClient client)
        {
            double ret = 0.0;

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.AddTempDefense))
            {
                return ret;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.AddTempDefense);
            if (null == bufferData)
            {
                return ret;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                ret = 0.10;
            }

            return ret;
        }

        /// <summary>
        /// 添加防御的buffer
        /// </summary>
        /// <param name="client"></param>
        public static void AddDefenseBuffer(GameClient client)
        {
            if (null == client.ClientData.AddTempDefenseBufferData) //当前没有狂攻符咒
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.AddTempDefense);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        client.ClientData.AddTempDefenseBufferData = bufferData;

                        // 攻击力发生变化的通知(同一个地图才需要通知)
                        //通知客户端属性变化
                        GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                    }
                }
            }
        }

        /// <summary>
        /// 去除防御的buffer
        /// </summary>
        /// <param name="client"></param>
        public static void RemoveDefenseBuffer(GameClient client)
        {
            BufferData bufferData = client.ClientData.AddTempDefenseBufferData;
            if (null != bufferData) //有狂攻符咒
            {
                long nowTicks = TimeUtil.NOW();
                if ((nowTicks - bufferData.StartTime) >= (bufferData.BufferSecs * 1000))
                {
                    client.ClientData.AddTempDefenseBufferData = null;

                    // 攻击力发生变化的通知(同一个地图才需要通知)
                    //通知客户端属性变化
                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }
            }
        }

        #endregion 防御力

        #region 生命上限

        /// <summary>
        /// 处理生命符咒
        /// </summary>
        /// <param name="client"></param>
        public static double ProcessUpLifeLimit(GameClient client)
        {
            double ret = 0.0;
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.UpLifeLimit);
            if (null == bufferData)
            {
                return ret;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                ret = 0.10;
            }

            return ret;
        }

        /// <summary>
        /// 添加生命符咒
        /// </summary>
        /// <param name="client"></param>
        public static void AddUpLifeLimitStatus(GameClient client)
        {
            if (null == client.ClientData.UpLifeLimitBufferData) //当前没有生命符咒
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.UpLifeLimit);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        client.ClientData.UpLifeLimitBufferData = bufferData;

                        // 总生命值和魔法值变化通知(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                    }                    
                }
            }
        }

        /// <summary>
        /// 去除生命符咒
        /// </summary>
        /// <param name="client"></param>
        public static void RemoveUpLifeLimitStatus(GameClient client)
        {
            BufferData bufferData = client.ClientData.UpLifeLimitBufferData;
            if (null != bufferData) //有生命符咒
            {
                long nowTicks = TimeUtil.NOW();
                if ((nowTicks - bufferData.StartTime) >= (bufferData.BufferSecs * 1000))
                {
                    client.ClientData.UpLifeLimitBufferData = null;

                    // 总生命值和魔法值变化通知(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                }
            }
        }

        #endregion 生命上限

        #region 经验和金币

        /// <summary>
        /// 处理双倍或者三倍经验卡
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessDblAndThreeExperience(GameClient client)
        {
            double ret = 1.0;

            //判断此地图是否允许使用Buffer
            if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.FiveExperience))
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.FiveExperience);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        ret = 5.0;
                    }
                }
            }

            if (ret < 5.0) //没有五倍的卡
            {
                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.ThreeExperience))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.ThreeExperience);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            ret = 3.0;
                        }
                    }
                }
            }

            if (ret < 3.0) //没有三倍的卡
            {
                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.DblExperience))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.DblExperience);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            ret = 2.0;
                        }
                    }
                }
            }

            if (ret <= 1.0) //没有其他经验卡
            {
                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.MutilExperience))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.MutilExperience);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            ret = (int)(bufferData.BufferVal & 0x00000000FFFFFFFF);
                        }
                    }
                }
            }            

            return ret;
        }

        /// <summary>
        /// 处理双倍或者三倍金币卡
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessDblAndThreeMoney(GameClient client)
        {
            double ret = 1.0;

            //判断此地图是否允许使用Buffer
            if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.ThreeMoney))
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.ThreeMoney);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        ret = 3.0;
                    }
                }
            }

            if (ret < 3.0)
            {
                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.DblMoney))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.DblMoney);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            ret = 2.0;
                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 处理经验buffer，定时给经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessAutoGiveExperience(GameClient client)
        {
            double ret = 0;
            int actionGoodsID = 0;
            int leftSecs = 0;

            //判断此地图是否允许使用Buffer
            if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.TimeExp))
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.TimeExp);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        actionGoodsID = (int)bufferData.BufferVal;

                        //获取指定物品的公式列表
                        List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                        if (null != magicActionItemList && magicActionItemList[0].MagicActionID == MagicActionIDs.DB_ADD_EXP)
                        {
                            if (nowTicks - client.ClientData.StartAddExpTicks >= (int)(magicActionItemList[0].MagicActionParams[2] * 1000))
                            {
                                client.ClientData.StartAddExpTicks = nowTicks;
                                ret = magicActionItemList[0].MagicActionParams[0];
                            }
                        }

                        leftSecs = (int)(((bufferData.BufferSecs * 1000) - (nowTicks - bufferData.StartTime)) / 1000);
                    }
                }
            }

            //如果需要加经验
            if (ret > 0.0)
            {
                //处理角色经验
                GameManager.ClientMgr.ProcessRoleExperience(client, (int)ret, true, false); //不写数据库, 否则太频繁

                if (actionGoodsID > 0)
                {
                    string msgText = string.Format(Global.GetLang("您使用{0}获得了{1}点经验, 还剩余{2}分钟"), Global.GetGoodsNameByID(actionGoodsID), ret, leftSecs / 60);
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, msgText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
                }
            }

            return ret;
        }

        /// <summary>
        /// 处理替身娃娃buffer，杀怪给经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessWaWaGiveExperience(GameClient client, Monster monster)
        {
#if ___CC___FUCK___YOU___BB___
            if (monster.XMonsterInfo.Level < client.ClientData.Level)
            {
                return; //怪物等级小于自身等级，不处理
            }
#else
            if (monster.MonsterInfo.VLevel < client.ClientData.Level)
            {
                return; //怪物等级小于自身等级，不处理
            }
#endif

            double ret = 0;

            //判断此地图是否允许使用Buffer
            if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.WaWaExp))
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.WaWaExp);
                if (null != bufferData)
                {
                    if (bufferData.BufferVal > 0)
                    {
                        bufferData.BufferVal--; //先减去

                        int actionGoodsID = (int)bufferData.BufferSecs;
                        bool notifyBufferData = false;

                        //获取指定物品的公式列表
                        List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                        if (null != magicActionItemList && magicActionItemList[0].MagicActionID == MagicActionIDs.DB_ADD_WAWA_EXP)
                        {
                            int monsterNumToAddExp = (int)magicActionItemList[0].MagicActionParams[0];
                            if (monsterNumToAddExp > 0)
                            {
                                //if (0 == (client.ClientData.TotalKilledMonsterNum % monsterNumToAddExp))
                                if (0 == (bufferData.BufferVal % monsterNumToAddExp))
                                {
                                    //获取经验=等级*系数A+MIN(系数B,随机数)*等级
                                    ret = client.ClientData.Level * (magicActionItemList[0].MagicActionParams[2] + Global.GetRandomNumber(0, (int)magicActionItemList[0].MagicActionParams[3]));

                                    //将新的Buffer数据通知自己
                                    GameManager.ClientMgr.NotifyBufferData(client, bufferData);
                                    notifyBufferData = true;
                                }
                            }
                        }

                        if (!notifyBufferData)
                        {
                            if (bufferData.BufferVal <= 0)
                            {
                                //将新的Buffer数据通知自己
                                GameManager.ClientMgr.NotifyBufferData(client, bufferData);
                            }
                        }
                    }
                }
            }

            //如果需要加经验
            if (ret > 0.0)
            {
                double dblExperience = 1.0;

                //处理双倍经验的buffer
                if (SpecailTimeManager.JugeIsDoulbeExperienceAndLingli())
                {
                    dblExperience += 1.0;
                }

                ret = (int)(ret * dblExperience);

                //处理角色经验
                GameManager.ClientMgr.ProcessRoleExperience(client, (int)ret, true, false); //不写数据库, 否则太频繁

                //通知经验
                Global.NotifySelfWaWaExp(client, (int)ret);
            }
        }

        /// <summary>
        /// 处理祝福经验buffer，定时给经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static long ProcessZhuFuGiveExperience(GameClient client)
        {
            long ret = 0;

            //判断此地图是否允许使用Buffer
            if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.ZhuFu))
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.ZhuFu);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        ret = ((bufferData.BufferSecs * 1000) - (nowTicks - bufferData.StartTime)) / 1000;
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 处理二锅头得经验buffer，定时给经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static long ProcessErGuoTouGiveExperience(GameClient client, long subTicks, out double multiExpNum)
        {
            multiExpNum = 0.0;
            long ret = 0;

            //判断此地图是否允许使用Buffer
            if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.ErGuoTou))
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.ErGuoTou);
                if (null != bufferData)
                {
                    if (bufferData.BufferSecs > 0)
                    {
                        multiExpNum = (bufferData.BufferVal & 0x00000000FFFFFFFF) - 1.0;

                        bufferData.BufferSecs = Math.Max(0, bufferData.BufferSecs - (int)(subTicks / 1000));
                        ret = bufferData.BufferSecs;

                        //将新的Buffer数据通知自己
                        GameManager.ClientMgr.NotifyBufferData(client, bufferData);
                    }
                }
            }

            return ret;
        }

#endregion 经验和金币

#region 技能

        /// <summary>
        /// 处理双倍技能卡
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessDblSkillUp(GameClient client)
        {
            double ret = 1.0;

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.DblSkillUp))
            {
                return ret;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.DblSkillUp);
            if (null == bufferData)
            {
                return ret;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                ret = 2.0;
            }

            return ret;
        }

#endregion 技能

#region Boss和角色

        /// <summary>
        /// 处理BOSS克星
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int ProcessAntiBoss(GameClient client, Monster monster, int injuredVal)
        {
            //只处理BOSS和精英怪
            if (monster.MonsterType != (int)MonsterTypes.BOSS /*&&
                monster.MonsterType != (int)MonsterTypes.Rarity*/)
            {
                return injuredVal;
            }

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.AntiBoss))
            {
                return injuredVal;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.AntiBoss);
            if (null == bufferData)
            {
                return injuredVal;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) >= (bufferData.BufferSecs * 1000))
            {
                return injuredVal;
            }

            return (injuredVal * (int)bufferData.BufferVal);
        }

        /// <summary>
        /// 处理角色克星
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int ProcessAntiRole(GameClient client, GameClient otherClient, int injuredVal)
        {
            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.AntiRole))
            {
                return injuredVal;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.AntiRole);
            if (null == bufferData)
            {
                return injuredVal;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) >= (bufferData.BufferSecs * 1000))
            {
                return injuredVal;
            }

            return injuredVal + (int)(((double)bufferData.BufferVal / 100.0) * injuredVal);
        }

#endregion Boss和角色

#region 月卡

        /// <summary>
        /// 处理VIP月卡
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessMonthVIP(GameClient client)
        {
            double ret = 0.0;

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.MonthVIP))
            {
                return ret;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.MonthVIP);
            if (null == bufferData)
            {
                return ret;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < ((long)bufferData.BufferSecs * 1000))
            {
                ret = 1.0;
            }

            return ret;
        }
#endregion 月卡

#region 挂机保护

        /// <summary>
        /// 处理挂机保护卡
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool ProcessAutoFightingProtect(GameClient client)
        {
            bool ret = false;

            //判断此地图是否允许使用Buffer
            if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.AutoFightingProtect))
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.AutoFightingProtect);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        ret = true;
                    }
                }
            }

            return ret;
        }

#endregion 挂机保护

#region 天生掉落

        /// <summary>
        /// 处理祝福经验buffer，定时给经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool ProcessFallTianSheng(GameClient client)
        {
            bool ret = false;

            //判断此地图是否允许使用Buffer
            if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.FallTianSheng))
            {
                BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.FallTianSheng);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        int randNum = Global.GetRandomNumber(0, 101);
                        if (randNum <= bufferData.BufferVal)
                        {
                            ret = true;
                        }
                    }
                }
            }

            return ret;
        }

#endregion 天生掉落

#region 古墓buffer

        /// <summary>
        /// 处理古墓buffer
        /// </summary>
        /// <param name="client"></param>
        /// <param name="elapseTicks"></param>
        public static void ProcessGuMu(GameClient client, long elapseTicks)
        {
            //古墓buffer
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.GuMuTimeLimit);

            if (bufferData != null)
            {
                //减少buffer剩余时间[秒数]
                if (bufferData.StartTime == (int)TimeUtil.NowDateTime().DayOfYear && bufferData.BufferVal > 0)
                {
                    bufferData.BufferVal = Math.Max(0, bufferData.BufferVal - (elapseTicks / 1000));
                }
                else if (bufferData.BufferSecs > 0)
                {
                    bufferData.BufferSecs = (int)Math.Max(0, bufferData.BufferSecs - (elapseTicks / 1000));
                }

                //将新的Buffer数据通知自己
                GameManager.ClientMgr.NotifyBufferData(client, bufferData);
            }

            //如果超过限制时间，通知玩家离开地图
            if (null == bufferData || Global.IsBufferDataOver(bufferData))
            {
                if (bufferData != null)
                {
                    //移除buffer
                    //Global.RemoveBufferData(client, (int)BufferItemTypes.GuMuTimeLimit);
                }

                //如果buffer给予日发生变化，则给予新的buffer奖励，并且不回城
                int todayID = TimeUtil.NowDateTime().DayOfYear;
                int lastGiveDayID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.GuMuAwardDayID);
                if (todayID == lastGiveDayID)
                {
                    if (client.CheckCheatData.LastNotifyLeaveGuMuTick == 0)
                    {
                        client.CheckCheatData.LastNotifyLeaveGuMuTick = TimeUtil.NOW() * 10000;
                    }
                    else
                    {
                        if (TimeUtil.NOW() * 10000 - client.CheckCheatData.LastNotifyLeaveGuMuTick > 60 * 1000 * 10000)
                        {
                            // 踢掉并且记日志
                            Global.ForceCloseClient(client, "超时未离开古墓地图");
                        }
                    }

                    //同一天，给过时间奖励，而且时间用完 则 回主城
                    GameManager.LuaMgr.GotoMap(client, GameManager.MainMapCode);
                }
                else
                {
                    if (client.CheckCheatData.LastNotifyLeaveGuMuTick > 0)
                    {
                        client.CheckCheatData.LastNotifyLeaveGuMuTick = 0;
                    }

                    Global.GiveGuMuTimeLimitAward(client);
                }
            }
        }

#endregion 古墓buffer

#region 冥界地图

        /// <summary>
        /// 处理角色冥界地图限时buffer
        /// </summary>
        /// <param name="client"></param>
        public static void ProcessMingJieBuffer(GameClient client, long elapseTicks)
        {
            //冥界buffer
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.MingJieMapLimit);

            if (bufferData != null)
            {
                //减少buffer剩余时间[秒数]
                bufferData.BufferVal -= elapseTicks / 1000;

                //将新的Buffer数据通知自己
                GameManager.ClientMgr.NotifyBufferData(client, bufferData);
            }

            //如果超过限制时间，通知玩家离开地图 这儿多给6秒的空余时间，以保证客户端倒计时最先到0，然后回城，
            //毕竟，客户端倒计时没有和服务器严格同步，各种预处理可能会浪费掉几秒的时间
            if (null == bufferData || bufferData.BufferVal <= -6)
            {
                if (bufferData != null)
                {
                    //移除buffer
                    Global.RemoveBufferData(client, (int)BufferItemTypes.MingJieMapLimit);
                }

                //回主城
                GameManager.LuaMgr.GotoMap(client, GameManager.MainMapCode);
            }
        }

#endregion 冥界地图

#region PK王buffer

        /// <summary>
        /// 持续时间增加的pk王攻击属性
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessTimeAddPkKingAttackProp(GameClient client, ExtPropIndexes attackType)
        {
            double ret = 0.0;

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.PKKingBuffer))
            {
                return ret;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.PKKingBuffer);
            if (null == bufferData)
            {
                return ret;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                // PK之王 改造 [4/1/2014 LiaoWei]
                int actionGoodsID = (int)bufferData.BufferVal;
                EquipPropItem item = null;
                item = GameManager.EquipPropsMgr.FindEquipPropItem(actionGoodsID);
                if (null != item)
                {
                    ret += item.ExtProps[(int)attackType];
                }
            }

            return ret;
        }

        /// <summary>
        /// 持续时间增加的pk王经验属性 没有则默认返回0，有则返回倍数，其值一般是1.5，表示1.5倍
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessTimeAddPkKingExpProp(GameClient client)
        {
            double ret = 0.0;

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.PKKingBuffer))
            {
                return ret;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.PKKingBuffer);
            if (null == bufferData)
            {
                return ret;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                int actionGoodsID = (int)bufferData.BufferVal;

                //获取指定物品的公式列表
                List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                ret = magicActionItemList[0].MagicActionParams[3];
            }

            ret -= 1.0;
            ret = Global.GMax(0.0, ret);
            return ret;
        }

#endregion 攻击力

#region 帮旗buffer
        /// <summary>
        /// 持续时间帮旗增加的角色属性
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessTimeAddJunQiProp(GameClient client, ExtPropIndexes attackType)
        {
            double ret = 0.0;
            return ret;

            //判断此地图是否允许使用Buffer
            /*if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.JunQi))
            {
                return ret;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.JunQi);
            if (null == bufferData)
            {
                return ret;
            }

            //从 1 开始的帮旗等级值 目前只考虑 1级帮旗增加的属性 最大攻击都是20，生命上限100 
            int junQiLevel = (int)bufferData.BufferVal;

            //获取指定物品的公式列表
            if (ExtPropIndexes.MaxAttack == attackType)
            {
                switch (junQiLevel)
                {
                    case 1:
                        ret = 20;
                        break;
                    default:
                        break;
                }
            }
            else if (ExtPropIndexes.MaxMAttack == attackType)
            {
                switch (junQiLevel)
                {
                    case 1:
                        ret = 20;
                        break;
                    default:
                        break;
                }
            }
            else if (ExtPropIndexes.MaxDSAttack == attackType)
            {
                switch (junQiLevel)
                {
                    case 1:
                        ret = 20;
                        break;
                    default:
                        break;
                }
            }
            else if (ExtPropIndexes.MaxLifeV == attackType)
            {
                switch (junQiLevel)
                {
                    case 1:
                        ret = 100;
                        break;
                    default:
                        break;
                }
            }

            return ret;*/
        }
#endregion 帮旗buffer

#region 中毒

        /// <summary>
        /// 处理道士释放毒的buffer, 定时伤害
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessDSTimeSubLifeNoShow(GameClient client)
        {
            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                double lifeV = 0;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, (int)BufferItemTypes.DSTimeShiDuNoShow))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.DSTimeShiDuNoShow);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            int timeSlotSecs = (int)((bufferData.BufferVal >> 32) & 0x00000000FFFFFFFF);
                            int SubLiefV = (int)((bufferData.BufferVal) & 0x00000000FFFFFFFF);

                            if (nowTicks - client.ClientData.DSStartDSSubLifeNoShowTicks >= (int)(timeSlotSecs * 1000))
                            {
                                client.ClientData.DSStartDSSubLifeNoShowTicks = nowTicks;
                                lifeV = SubLiefV;
                            }
                        }
                        else
                        {
                            Global.RemoveBufferData(client, (int)BufferItemTypes.DSTimeShiDuNoShow);
                        }
                    }
                }

                //如果需要加生命
                if (lifeV > 0.0)
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(client.ClientData.FangDuRoleID);
                    if (null != enemyClient)
                    {
                        // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                        int nOcc = Global.CalcOriginalOccupationID(enemyClient);

                        //最低伤害1，使用一个外部传入的1的技巧
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            enemyClient, client, 0, (int)lifeV, 1.0, nOcc, false, 0, 0.0, 0, 0, 0, 0.0, 0.0, false, true);

                        if (client.ClientData.CurrentLifeV <= 0) //如果死亡
                        {
                            Global.RemoveBufferData(client, (int)BufferItemTypes.DSTimeShiDuNoShow);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理持续伤害的buffer, 定时伤害
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static void ProcessTimeSubLifeNoShow(GameClient client, int id)
        {
            //如果已经死亡，则不再调度
            if (client.ClientData.CurrentLifeV > 0)
            {
                double lifeV = 0;
                DelayInjuredBufferItem delayInjuredBufferItem = null;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(client.ClientData.MapCode, id))
                {
                    BufferData bufferData = Global.GetBufferDataByID(client, id);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            delayInjuredBufferItem = client.MyBufferExtManager.FindBufferItem(id) as DelayInjuredBufferItem;
                            if (null != delayInjuredBufferItem)
                            {
                                if (nowTicks - delayInjuredBufferItem.StartSubLifeNoShowTicks >= (int)(delayInjuredBufferItem.TimeSlotSecs * 1000))
                                {
                                    delayInjuredBufferItem.StartSubLifeNoShowTicks = nowTicks;
                                    lifeV = delayInjuredBufferItem.SubLifeV;
                                }
                            }
                        }
                        else
                        {
                            Global.RemoveBufferData(client, id);
                            client.MyBufferExtManager.RemoveBufferItem(id);
                        }
                    }
                }

                //如果需要加生命
                if (lifeV > 0.0 && null != delayInjuredBufferItem)
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(delayInjuredBufferItem.ObjectID);
                    if (null != enemyClient)
                    {
                        // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                        int nOcc = Global.CalcOriginalOccupationID(enemyClient);

                        //最低伤害1，使用一个外部传入的1的技巧
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            enemyClient, client, 0, (int)lifeV, 1.0, nOcc, false, 0, 0.0, 0, 0, 0, 0.0, 0.0, false, true);

                        if (client.ClientData.CurrentLifeV <= 0) //如果死亡
                        {
                            Global.RemoveBufferData(client, id);
                            client.MyBufferExtManager.RemoveBufferItem(id);
                        }
                    }
                    else
                    {
                        Global.RemoveBufferData(client, id);
                        client.MyBufferExtManager.RemoveBufferItem(id);
                    }
                }
            }
        }

        /// <summary>
        /// 处理持续伤害的新的扩展buffer, 定时伤害
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessAllTimeSubLifeNoShow(GameClient client)
        {
            for (int id = (int)BufferItemTypes.TimeFEIXUENoShow; id <= (int)BufferItemTypes.TimeRANSHAONoShow; id++)
            {
                ProcessTimeSubLifeNoShow(client, id);
            }
        }

#endregion 中毒


        // MU新增的一些BUFF处理 [1/10/2014 LiaoWei]
#region 提升MU 新增二级属性数值的BUFF

        /// <summary>
        /// 处理特殊攻击加成(幸运一击、卓越一击、双倍一击)的BUFF
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static double ProcessSpecialAttackValueBuff(GameClient client, int BufferTypes)
        {
            // 非玩家 直接返回
            /*if (client == null)
                return 0.0;*/

            double dValue = 0.0;

            //判断此地图是否允许使用Buffer
            if (!Global.CanMapUseBuffer(client.ClientData.MapCode, BufferTypes))
            {
                return dValue;
            }

            BufferData bufferData = Global.GetBufferDataByID(client, BufferTypes);
            if (null == bufferData)
            {
                return dValue;
            }

            int nMagicID = -1;

            switch (BufferTypes)
            {
                case (int)BufferItemTypes.MU_ADDLUCKYATTACKPERCENTTIMER:
                    {
                        nMagicID = (int)MagicActionIDs.DB_ADD_LUCKYATTACKPERCENTTIMER;
                    }
                    break;
                case (int)BufferItemTypes.MU_ADDFATALATTACKPERCENTTIMER:
                    {
                        nMagicID = (int)MagicActionIDs.DB_ADD_FATALATTACKPERCENTTIMER;
                    }
                    break;
                case (int)BufferItemTypes.MU_ADDDOUBLEATTACKPERCENTTIMER:
                    {
                        nMagicID = (int)MagicActionIDs.DB_ADD_DOUBLETACKPERCENTTIMER;
                    }
                    break;
                default:
                    {
                        return dValue;
                    }
                    break;
            }

            long nowTicks = TimeUtil.NOW();
            if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
            {
                int actionGoodsID = (int)bufferData.BufferVal;

                //获取指定物品的公式列表
                List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                if (null != magicActionItemList && magicActionItemList[0].MagicActionID == (MagicActionIDs)nMagicID)
                {
                    dValue = magicActionItemList[0].MagicActionParams[0];
                }
            }

            return dValue;
        }

#endregion 提升MU 新增二级属性数值的BUFF 

    }
}
