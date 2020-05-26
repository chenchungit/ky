using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    public class RoleSpeedControl
    {
        /// <summary>
        /// 上次的X坐标
        /// </summary>
        public int LastPosX
        {
            get;
            set;
        }

        /// <summary>
        /// 上次的Y坐标
        /// </summary>
        public int LastPosY
        {
            get;
            set;
        }

        private double _LastSlowRate = 0.0;
        private long _LastSlowRateTicks = 0;

        /// <summary>
        /// 上次的减速比例值
        /// </summary>
        public double LastSlowRate
        {
            get 
            {
                long ticks = TimeUtil.NOW();
                int punishSecs = GameManager.GameConfigMgr.GetGameConfigItemInt("punish-speed-secs", 5); //惩罚值（缺省是5)
                if (ticks - _LastSlowRateTicks < (punishSecs * 1000))
                {
                    return _LastSlowRate;
                }

                return 0.0; 
            }
            set
            {
                _LastSlowRateTicks = TimeUtil.NOW();
                _LastSlowRate = value;
            }
        }

        /// <summary>
        /// 上次包延迟的Ticks
        /// </summary>
        public long LastDelayTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 角色速度记录项列表
        /// </summary>
        private List<RoleSpeedItem> RoleSpeedItemList = new List<RoleSpeedItem>();

        /// <summary>
        /// 添加一个速度项
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="overflowSpeed"></param>
        public void AddRoleSpeed(int mapCode, int x, int y, double overflowSpeed)
        {
            RoleSpeedItem roleSpeedItem = new RoleSpeedItem()
            {
                MapCode = mapCode,
                X = x,
                Y = y,
                OverflowSpeed = overflowSpeed,
            };

            RoleSpeedItemList.Add(roleSpeedItem);
        }

        /// <summary>
        /// 获取速度项的个数
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="overflowSpeed"></param>
        public int GetRoleSpeedCount()
        {
            return (RoleSpeedItemList.Count);
        }

        /// <summary>
        /// 获取最前一个的速度项
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="overflowSpeed"></param>
        public RoleSpeedItem GetFirstRoleSpeed()
        {
            if (RoleSpeedItemList.Count <= 0)
            {
                return null;
            }

            return RoleSpeedItemList[0];
        }

        /// <summary>
        /// 清空速度记录列表
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="overflowSpeed"></param>
        public void ClearRoleSpeed()
        {
            LastPosX = 0;
            LastPosY = 0;

            RoleSpeedItemList.Clear();
        }
    }
}
