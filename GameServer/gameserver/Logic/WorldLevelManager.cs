using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 排行榜的类型
    /// </summary>
    public enum PaiHangTypes
    {
        None = 0, //无定义
        EquipJiFen = 1, //装备积分
        XueWeiNum = 2, //穴位个数
        SkillLevel = 3, //技能级别
        HorseJiFen = 4, //坐骑积分
        RoleLevel = 5, //角色等级
        RoleYinLiang = 6, //角色银两
        LianZhan = 7, //角色连斩
        KillBoss = 8, //杀BOSS数量
        BattleNum = 9, //角斗场称号次数
        HeroIndex = 10, //英雄逐擂的到达层数
        RoleGold = 11, //角色金币
        CombatForceList = 12, // 战斗力 [12/18/2013 LiaoWei]
        JingJi = 13, //竞技场
        WanMoTa = 14, //万魔塔
        Wing = 15, //翅膀
        Ring = 16, //婚戒
        Merlin = 17, // 梅林魔法书
        MaxVal, //最大值
    }

    /// <summary>
    /// 世界等级管理类
    /// </summary>
    class WorldLevelManager
    {
        /// <summary>
        /// 世界等级
        /// </summary>
        public int m_nWorldLevel = 0;

        /// <summary>
        /// 重置世界等级的天ID
        /// </summary>
        public int m_nResetWorldLevelDayID = 0;


        private static WorldLevelManager instance = new WorldLevelManager();

        private WorldLevelManager() { }

        public static WorldLevelManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 重置世界等级
        /// </summary>
        public void ResetWorldLevel()
        {
            int dayID = TimeUtil.NowDateTime().DayOfYear;
            if (m_nResetWorldLevelDayID == dayID)
            {
                return;
            }

            TCPOutPacket tcpOutPacket = null;
            string strcmd = string.Format("{0}:{1}", 0, (int)PaiHangTypes.RoleLevel);
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer2(Global._TCPManager.tcpClientPool, TCPOutPacketPool.getInstance(), (int)TCPGameServerCmds.CMD_SPR_GETPAIHANGLIST, strcmd, out tcpOutPacket, GameManager.LocalServerId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                LogManager.WriteLog(LogTypes.Error, "世界等级装入异常");
                return;
            }

            int nBakResetWorldLevelDayID = m_nResetWorldLevelDayID;
            m_nResetWorldLevelDayID = dayID;

            // 处理本地精简的好友列表数据
            PaiHangData paiHangData = DataHelper.BytesToObject<PaiHangData>(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);
            if (null != paiHangData)
            {
                int nLevelCount = 0;
                if (null != paiHangData.PaiHangList)
                {
                    for (int i = 0; i < 10 && i < paiHangData.PaiHangList.Count; i++)
                    {
                        nLevelCount += paiHangData.PaiHangList[i].Val2 * 100 + paiHangData.PaiHangList[i].Val1;
                    }
                }
                m_nWorldLevel = nLevelCount / 10;
            }
            else
            {
                LogManager.WriteLog(LogTypes.Error, "世界等级装入时，获取等级排行榜失败");
                return;
            }


            if (0 != nBakResetWorldLevelDayID)
            {
                int count = GameManager.ClientMgr.GetMaxClientCount();
                for( int i = 0; i < count; i++ )
                {
                    GameClient client = GameManager.ClientMgr.FindClientByNid(i);
                    if (null != client)
                    {
                        UpddateWorldLevelBuff(client);
                    }
                }
            }
        }

        /// <summary>
        /// 更新世界等级BUFF
        /// </summary>
        public void UpddateWorldLevelBuff(GameClient client)
        {
            int nMeTotalLevel = client.ClientData.GetRoleData().ChangeLifeCount * 100 + client.ClientData.GetRoleData().Level;
            double nWorldLevelAddPer = Math.Round((m_nWorldLevel - nMeTotalLevel) / 100.0, 2) * GameManager.systemParamsList.GetParamValueDoubleByName("WorldLevel");

            int nNewBufferGoodsIndexID = (int)(nWorldLevelAddPer * 100);
            int nOldBufferGoodsIndexID = -1;
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.MU_WORLDLEVEL);
            if (null != bufferData && !Global.IsBufferDataOver(bufferData))
            {
                nOldBufferGoodsIndexID = (int)bufferData.BufferVal;
            }

            if (nNewBufferGoodsIndexID < 0)
            {
                nNewBufferGoodsIndexID = 0;
            }

            if (nOldBufferGoodsIndexID == nNewBufferGoodsIndexID)
            {
                return;
            }

            //更新BufferData
            double[] actionParams = new double[1];
            //actionParams[0] = (double)(60);//持续时间改为60分钟
            //actionParams[1] = (double)nNewBufferGoodsIndexID;

            actionParams[0] = (double)nNewBufferGoodsIndexID;

            Global.UpdateBufferData(client, BufferItemTypes.MU_WORLDLEVEL, actionParams, 1, true);
            client.ClientData.nTempWorldLevelPer = nWorldLevelAddPer;

            //通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
        }
    }
}
