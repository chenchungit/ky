using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Tools;

namespace GameServer.Logic
{
    public class DamageMonitor
    {
        #region 成员

        /// <summary>
        /// 需要监控的地图和玩家列表
        /// key=mapCode value=玩家列表
        /// </summary>
        private Dictionary<int, List<int>> DictMonitorList = new Dictionary<int, List<int>>();

        #endregion

        #region 接口

        /// <summary>
        /// 设置监控的地图和玩家
        /// </summary>
        public void Set(int mapCode, int RoleID)
        {
            // 已经存在
            if (Get(mapCode, RoleID))
            {
                return;
            }

            if (DictMonitorList.ContainsKey(mapCode))
            {
                DictMonitorList[mapCode].Add(RoleID);
            }
            else
            {
                List<int> MonitorList = new List<int>();
                MonitorList.Add(RoleID);
                DictMonitorList[mapCode] = MonitorList;
            }
        }

        /// <summary>
        /// 删掉一个监控的地图的玩家
        /// </summary>
        public void Remove(int mapCode, int RoleID)
        {
            // 如果不存在
            if (!Get(mapCode, RoleID))
            {
                return;
            }

            if (DictMonitorList.ContainsKey(mapCode))
            {
                DictMonitorList[mapCode].Remove(RoleID);
            }
        }

        /// <summary>
        /// mapcode和玩家是否被监控
        /// </summary>
        public bool Get(int mapCode, int RoleID)
        {
            if (DictMonitorList.ContainsKey(mapCode))
            {
                if (DictMonitorList[mapCode].IndexOf(RoleID) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 清空列表
        /// </summary>
        public void Clear(int mapCode, int RoleID)
        {
            DictMonitorList.Clear();
        }

        public void Out(GameClient client)
        {
            if (null == client)
            {
                return;
            }

            // 玩家伤害统计
            if (!Get(1, client.ClientData.RoleID))
            {
                return;
            }
            /**/string DamageInfo = string.Format("个人伤害统计，mapcode={0}, roleid={1}, magiccode={2}, damage={3}, posx={4}, posy={5}", client.ClientData.MapCode, client.ClientData.RoleID, client.CheckCheatData.LastMagicCode, client.CheckCheatData.LastDamage, client.ClientData.PosX, client.ClientData.PosY);
            if (client.CheckCheatData.LastEnemyID > 0)
            {
                DamageInfo += string.Format("LastEnemyID={0} LastEnemyName={1} LastEnemyPosX={2} LastEnemyPosY={3} dist={4:00} ", client.CheckCheatData.LastEnemyID, client.CheckCheatData.LastEnemyName, client.CheckCheatData.LastEnemyPos.X, client.CheckCheatData.LastEnemyPos.Y, Global.GetTwoPointDistance(client.CurrentPos, client.CheckCheatData.LastEnemyPos));
            }

            for (int i = (int)DamageType.DAMAGETYPE_DEFAULT; i < (int)DamageType.DAMAGETYPE_MAX; i++)
            {
                if (Global.GetIntSomeBit(client.CheckCheatData.LastDamageType, i) == 1)
                {
                    DamageInfo += string.Format("damagetype={0}", (DamageType)i);
                }
            }
            client.CheckCheatData.LastMagicCode = 0;
            client.CheckCheatData.LastDamage = 0;
            client.CheckCheatData.LastDamageType = 0;
            client.CheckCheatData.LastEnemyID = 0;
            client.CheckCheatData.LastEnemyName = "";
            client.CheckCheatData.LastEnemyPos.X = 0;
            client.CheckCheatData.LastEnemyPos.Y = 0;
            LogManager.WriteLog(LogTypes.Error, DamageInfo);
        }

        #endregion
    }
}
