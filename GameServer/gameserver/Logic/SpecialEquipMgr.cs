using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;
using Server.Data;

namespace GameServer.Logic
{
    /// <summary>
    /// 特殊装备效果管理类(麻痹戒指,复活戒指,护身戒指等)
    /// </summary>
    public class SpecialEquipMgr
    {
        #region 特殊戒指

        /// <summary>
        /// 攻击时附加的效果(麻痹戒指附加一定几率的麻痹效果)
        /// </summary>
        /// <param name="client">客户端对象</param>
        /// <param name="categoriy">装备类型</param>
        /// <param name="enemy">释放目标</param>
        public static void DoEquipExtAttack(GameClient client, int categoriy, int enemy)
        {
            if (-1 == enemy)
            {
                return;
            }

            double time = 0;
            double percent = 0;

            GoodsData goodData = client.UsingEquipMgr.GetGoodsDataByCategoriy(client, categoriy);
            if (null == goodData)
            {
                return ;
            }

            // 获取指定物品的公式列表
            List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(goodData.GoodsID);
            if (null == magicActionItemList || magicActionItemList.Count <= 0)
            {
                return;
            }

            MagicActionItem item = magicActionItemList[0];
            if (MagicActionIDs.EXT_ATTACK_MABI == item.MagicActionID)
            {
                percent = item.MagicActionParams[0];
                time = item.MagicActionParams[1];
            }
            else
            {
                return;
            }

            if (Global.GetRandomNumber(0, 101) > percent)
            {
                return;
            }

            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            if (0 != nOcc)
            {
                percent = percent * 0.5;
            }

            //判断是否找到了敌人
            if (-1 != enemy)
            {
                //根据敌人ID判断对方是系统爆的怪还是其他玩家
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    /*Monster enemyMonster = GameManager.MonsterMgr.FindMonster(client.ClientData.MapCode, enemy);
                    if (null != enemyMonster)
                    {
                    }*/
                }
                else if (st == GSpriteTypes.Other)
                {
                    GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                    if (null != enemyClient)
                    {
                        enemyClient.ClientData.DongJieStart = TimeUtil.NOW();
                        enemyClient.ClientData.DongJieSeconds = (int)time;

                        //发送角色冻结状态命令
                        GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, enemyClient,
                            (int)RoleStatusIDs.DongJie, enemyClient.ClientData.DongJieStart, enemyClient.ClientData.DongJieSeconds);
                    }
                }
            }
        }

        /// <summary>
        /// 生命值为0时,立即回复100%生命值
        /// </summary>
        /// <param name="client">客户端对象</param>
        /// <param name="categoriy">装备类型</param>
        public static void DoEquipRestoreBlood(GameClient client, int categoriy)
        {
            if (client.ClientData.CurrentLifeV > 0)
            {
                return;
            }

            GoodsData goodData = client.UsingEquipMgr.GetGoodsDataByCategoriy(client, categoriy);
            if (null == goodData)
            {
                return;
            }

            // 获取指定物品的公式列表
            List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(goodData.GoodsID);
            if (null == magicActionItemList || magicActionItemList.Count <= 0)
            {
                return;
            }

            double cooldown = 0;
            MagicActionItem item = magicActionItemList[0];
            if (MagicActionIDs.EXT_RESTORE_BLOOD == item.MagicActionID)
            {
                cooldown = item.MagicActionParams[0];
            }
            else
            {
                return;
            }

            //判断是否处于冷却中,如果是则退出
            if ((cooldown * 1000) + client.ClientData.SpecialEquipLastUseTicks >= TimeUtil.NOW())
            {
                return;
            }

            // 恢复全部生命值
            client.ClientData.CurrentLifeV = client.ClientData.LifeV;

            //设置冷却时间
            client.ClientData.SpecialEquipLastUseTicks = TimeUtil.NOW();

            //提示用户复活戒指状态生效
            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, Global.GetLang("复活戒指状态生效"), GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, 0);
        }

        /// <summary>
        /// 被攻击时吸收一部分伤害(护身戒指)
        /// </summary>
        /// <param name="client">客户端对象</param>
        /// <param name="categoriy">装备类型</param>
        /// <param name="injure">传入的伤害值</param>
        /// <returns>抵消的伤害值</returns>
        public static int DoSubInJure(GameClient client, int categoriy, int injure)
        {
            GoodsData goodData = client.UsingEquipMgr.GetGoodsDataByCategoriy(client, categoriy);
            if (null == goodData)
            {
                return 0;
            }

            // 获取指定物品的公式列表
            List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(goodData.GoodsID);
            if (null == magicActionItemList || magicActionItemList.Count <= 0)
            {
                return 0;
            }

            int subInjure = 0;
            int magicValue = 0;
            double percent = 0;
            double magicToBloodRate = 0;

            MagicActionItem item = magicActionItemList[0];
            if (MagicActionIDs.EXT_SUB_INJURE == item.MagicActionID)
            {
                percent = item.MagicActionParams[0];
                magicToBloodRate = item.MagicActionParams[1];
            }
            else
            {
                return 0;
            }

            if (percent <= 0 || magicToBloodRate <= 0)
            {
                return 0;
            }

            percent = percent / 100.0;

            // 计算伤害的抵消量
            magicValue = client.ClientData.CurrentMagicV;
            subInjure = (int)Math.Min(injure * percent, magicValue / magicToBloodRate);

            int oldMagicV = client.ClientData.CurrentMagicV;

            // 减去消耗的魔法值
            client.ClientData.CurrentMagicV -= (int) (magicToBloodRate * subInjure);

            return Math.Min(subInjure, injure);
        }

        #endregion 特殊戒指
    }
}
