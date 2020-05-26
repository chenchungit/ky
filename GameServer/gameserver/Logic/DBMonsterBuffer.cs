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
//using System.Windows.Documents;
using GameServer.Server;
using GameServer.Logic.NewBufferExt;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 怪物的临时buffer项管理
    /// </summary>
    public class DBMonsterBuffer
    {
        #region 生命和魔法

        /// <summary>
        /// 处理道士加血的buffer，定时不计生命
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessDSTimeAddLifeNoShow(Monster monster)
        {
            //如果已经死亡，则不再调度
            if (monster.VLife > 0)
            {
                double lifeV = 0;

                BufferData bufferData = Global.GetMonsterBufferDataByID(monster, (int)BufferItemTypes.DSTimeAddLifeNoShow);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        int timeSlotSecs = (int)((bufferData.BufferVal >> 32) & 0x00000000FFFFFFFF);
                        int addLiefV = (int)((bufferData.BufferVal) & 0x00000000FFFFFFFF);

                        if (nowTicks - monster.DSStartDSAddLifeNoShowTicks >= (int)(timeSlotSecs * 1000))
                        {
                            monster.DSStartDSAddLifeNoShowTicks = nowTicks;
                            lifeV = addLiefV;
                        }
                    }
                    else
                    {
                        ///删除，防止遗漏，占用内存资源
                        Global.RemoveMonsterBufferData(monster, (int)BufferItemTypes.DSTimeAddLifeNoShow);
                    }
                }

                //如果需要加生命

#if ___CC___FUCK___YOU___BB___
                if (monster.VLife < monster.XMonsterInfo.MaxHP && lifeV > 0.0)
                {
                    lifeV += monster.VLife;
                    monster.VLife = Global.GMin(monster.XMonsterInfo.MaxHP, lifeV);
                    //通知客户端怪已经加血加魔  
                    List<Object> listObjs = Global.GetAll9Clients(monster);
                    GameManager.ClientMgr.NotifyOthersRelife(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster, monster.MonsterZoneNode.MapCode, monster.CopyMapID, monster.RoleID, (int)monster.SafeCoordinate.X, (int)monster.SafeCoordinate.Y, (int)monster.SafeDirection, monster.VLife, monster.VMana, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
                }
#else
                if (monster.VLife < monster.MonsterInfo.VLifeMax && lifeV > 0.0)
                {
                    lifeV += monster.VLife;
                    monster.VLife = Global.GMin(monster.MonsterInfo.VLifeMax, lifeV);
                    //通知客户端怪已经加血加魔  
                    List<Object> listObjs = Global.GetAll9Clients(monster);
                    GameManager.ClientMgr.NotifyOthersRelife(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster, monster.MonsterZoneNode.MapCode, monster.CopyMapID, monster.RoleID, (int)monster.SafeCoordinate.X, (int)monster.SafeCoordinate.Y, (int)monster.SafeDirection, monster.VLife, monster.VMana, (int)TCPGameServerCmds.CMD_SPR_RELIFE, listObjs);
                }
#endif



            }
        }

        /// <summary>
        /// 处理重生(减少血)
        /// </summary>
        /// <param name="client"></param>
        public static int ProcessHuZhaoSubLifeV(Monster monster, int subLifeV)
        {
            //如果已经死亡，则不再调度
            if (monster.VLife > 0)
            {
                BufferData bufferData = null;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(monster.CurrentMapCode, (int)BufferItemTypes.TimeHUZHAONoShow))
                {
                    bufferData = Global.GetMonsterBufferDataByID(monster, (int)BufferItemTypes.TimeHUZHAONoShow);
                    if (null != bufferData)
                    {
                        if (bufferData.BufferVal > 0) //如果还有储备
                        {
#if ___CC___FUCK___YOU___BB___
                            int needLifeV = (int)(monster.XMonsterInfo.MaxHP - monster.VLife);
#else
                            int needLifeV = (int)(monster.MonsterInfo.VLifeMax - monster.VLife);
#endif
                            HuZhaoBufferItem huZhaoBufferItem = monster.MyBufferExtManager.FindBufferItem((int)BufferItemTypes.TimeHUZHAONoShow) as HuZhaoBufferItem;
                            if (huZhaoBufferItem != null)
                            {
                                needLifeV = Global.GMin(needLifeV, huZhaoBufferItem.InjuredV);
                                needLifeV = (int)Global.GMin(needLifeV, (int)bufferData.BufferVal);

                                bufferData.BufferVal -= (int)Global.GMin((int)bufferData.BufferVal, huZhaoBufferItem.InjuredV); //储备减少

                                subLifeV = Global.GMin(needLifeV, subLifeV);
                            }
                        }
                        else
                        {
                            ///删除，防止遗漏，占用内存资源
                            Global.RemoveMonsterBufferData(monster, (int)BufferItemTypes.TimeHUZHAONoShow);
                            monster.MyBufferExtManager.RemoveBufferItem((int)BufferItemTypes.TimeHUZHAONoShow);

                            bufferData.BufferSecs = 0;
                            bufferData.StartTime = 0;
                            GameManager.ClientMgr.NotifyOtherBufferData(monster, bufferData);
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
        public static double ProcessHuZhaoRecoverPercent(Monster monster)
        {
            double percent = 0.0;

            //如果已经死亡，则不再调度
            if (monster.VLife > 0)
            {
                BufferData bufferData = null;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(monster.CurrentMapCode, (int)BufferItemTypes.TimeHUZHAONoShow))
                {
                    bufferData = Global.GetMonsterBufferDataByID(monster, (int)BufferItemTypes.TimeHUZHAONoShow);
                    if (null != bufferData)
                    {
                        if (bufferData.BufferVal > 0) //如果还有储备
                        {
                            HuZhaoBufferItem huZhaoBufferItem = monster.MyBufferExtManager.FindBufferItem((int)BufferItemTypes.TimeHUZHAONoShow) as HuZhaoBufferItem;
                            if (huZhaoBufferItem != null)
                            {
                                percent = huZhaoBufferItem.RecoverLifePercent;
                            }
                        }
                        else
                        {
                            ///删除，防止遗漏，占用内存资源
                            Global.RemoveMonsterBufferData(monster, (int)BufferItemTypes.TimeHUZHAONoShow);
                            monster.MyBufferExtManager.RemoveBufferItem((int)BufferItemTypes.TimeHUZHAONoShow);
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
        public static int ProcessWuDiHuZhaoNoInjured(Monster monster, int subLifeV)
        {
            //如果已经死亡，则不再调度
            if (monster.VLife > 0)
            {
                BufferData bufferData = null;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(monster.CurrentMapCode, (int)BufferItemTypes.TimeWUDIHUZHAONoShow))
                {
                    bufferData = Global.GetMonsterBufferDataByID(monster, (int)BufferItemTypes.TimeWUDIHUZHAONoShow);
                    if (null != bufferData)
                    {
                        long nowTicks = TimeUtil.NOW();
                        if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                        {
                            subLifeV = 0;
                        }
                        else
                        {
                            Global.RemoveMonsterBufferData(monster, (int)BufferItemTypes.TimeWUDIHUZHAONoShow);
                        }
                    }
                }
            }

            return subLifeV;
        }

        /// <summary>
        /// [bing] 处理结婚副本伤害减少buff
        /// </summary>
        /// <param name="monster"></param>
        public static int ProcessMarriageFubenInjured(Monster monster, int subLifeV)
        {
            //如果已经死亡，则不再调度
            if (monster.VLife > 0 && subLifeV > 0)
            {
                BufferData bufferData = null;

                //判断此地图是否允许使用Buffer
                if (Global.CanMapUseBuffer(monster.CurrentMapCode, (int)BufferItemTypes.MU_MARRIAGE_SUBDAMAGEPERCENTTIMER))
                {
                    bufferData = Global.GetMonsterBufferDataByID(monster, (int)BufferItemTypes.MU_MARRIAGE_SUBDAMAGEPERCENTTIMER);
                    if (null != bufferData)
                    {
                        //永久buff不参与时间计算
                        subLifeV = (int)((double)subLifeV * ((double)bufferData.BufferVal / 100.0d));
                    }

                    //[bing] 因为是永久buff 会不会没有RemoveBufferData造成泄漏? 先mark
                }
            }

            return subLifeV;
        }

#endregion 生命和魔法

#region 中毒

        /// <summary>
        /// 处理道士释放毒的buffer, 定时伤害
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessDSTimeSubLifeNoShow(Monster monster)
        {
            //如果已经死亡，则不再调度
            if (monster.VLife > 0)
            {
                double lifeV = 0;

                BufferData bufferData = Global.GetMonsterBufferDataByID(monster, (int)BufferItemTypes.DSTimeShiDuNoShow);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        int timeSlotSecs = (int)((bufferData.BufferVal >> 32) & 0x00000000FFFFFFFF);
                        int SubLiefV = (int)((bufferData.BufferVal) & 0x00000000FFFFFFFF);

                        if (nowTicks - monster.DSStartDSSubLifeNoShowTicks >= (int)(timeSlotSecs * 1000))
                        {
                            monster.DSStartDSSubLifeNoShowTicks = nowTicks;
                            lifeV = SubLiefV;
                        }
                    }
                    else
                    {
                        ///删除，防止遗漏，占用内存资源
                        Global.RemoveMonsterBufferData(monster, (int)BufferItemTypes.DSTimeShiDuNoShow);
                    }
                }

                //如果需要加生命
                if (lifeV > 0.0)
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(monster.FangDuRoleID);
                    if (null != enemyClient)
                    {
                        // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                        int nOcc = Global.CalcOriginalOccupationID(enemyClient);

                        //最低伤害1，使用一个外部传入的1的技巧
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            enemyClient, monster, 0, (int)lifeV, 1.0, nOcc, false, 0, 0.0, 0, 0, 0, 0.0, 0.0);

                        if (monster.VLife <= 0) //如果死亡
                        {
                            ///删除，防止遗漏，占用内存资源
                            Global.RemoveMonsterBufferData(monster, (int)BufferItemTypes.DSTimeShiDuNoShow);
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
        private static void ProcessTimeSubLifeNoShow(Monster monster, int id)
        {
            //如果已经死亡，则不再调度
            if (monster.VLife > 0)
            {
                double lifeV = 0;
                DelayInjuredBufferItem delayInjuredBufferItem = null;

                BufferData bufferData = Global.GetMonsterBufferDataByID(monster, id);
                if (null != bufferData)
                {
                    long nowTicks = TimeUtil.NOW();
                    if ((nowTicks - bufferData.StartTime) < (bufferData.BufferSecs * 1000))
                    {
                        delayInjuredBufferItem = monster.MyBufferExtManager.FindBufferItem(id) as DelayInjuredBufferItem;
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
                        ///删除，防止遗漏，占用内存资源
                        Global.RemoveMonsterBufferData(monster, id);

                        monster.MyBufferExtManager.RemoveBufferItem(id);
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
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            enemyClient, monster, 0, (int)lifeV, 1.0, nOcc, false, 0, 0.0, 0, 0, 0, 0.0, 0.0);

                        if (monster.VLife <= 0) //如果死亡
                        {
                            ///删除，防止遗漏，占用内存资源
                            Global.RemoveMonsterBufferData(monster, id);

                            monster.MyBufferExtManager.RemoveBufferItem(id);
                        }
                    }
                    else
                    {
                        ///删除，防止遗漏，占用内存资源
                        Global.RemoveMonsterBufferData(monster, id);

                        monster.MyBufferExtManager.RemoveBufferItem(id);
                    }
                }
            }
        }

        /// <summary>
        /// 处理持续伤害的新的扩展buffer, 定时伤害
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void ProcessAllTimeSubLifeNoShow(Monster monster)
        {
            for (int id = (int)BufferItemTypes.TimeFEIXUENoShow; id <= (int)BufferItemTypes.TimeRANSHAONoShow; id++)
            {
                ProcessTimeSubLifeNoShow(monster, id);
            }
        }

#endregion 中毒
    }
}
