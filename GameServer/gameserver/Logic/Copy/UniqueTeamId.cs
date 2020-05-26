using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Tools.Pattern;
using GameServer.Core.Executor;

namespace GameServer.Logic.Copy
{
    // 生成一个不重复的道具id
    // 0-16位为程序自增数
    // 17-48位为时间(秒)
    // 49-63位为区号
    // 最后一位符号位为0 避免出现负数
    // serverid唯一会保证和别的区不会重复，程序自增数保证同一秒不会重复
    // 但是服务器使用虚拟机时间会有漂移的情况，所以启动的时候使用DateTime初始化一个时间，后面程序自增数循环的时候会在这个时间加1
    // 这样时间位会远远落后当前时间，但是可以保证不会重复

    public class UniqueTeamId : SingletonTemplate<UniqueTeamId>
    {
        private UniqueTeamId() { }

        public const long INVALID_TEAM_ID = -1;

        private object Mutex = new object();
        private long ThisServerId;
        private ushort AutoInc = 0;
        private int CurrSecond;

        public void Init()
        {
            CurrSecond = (int)Global.GetOffsetSecond(TimeUtil.NowDateTime());
            ThisServerId = GameCoreInterface.getinstance().GetLocalServerId();
        }

        public long Create()
        {
            long _validCurSecond;
            ushort _validAutoInc;

            lock (Mutex)
            {
                if (AutoInc >= ushort.MaxValue)
                {
                    CurrSecond++;
                    AutoInc = 0;

#if _UNIQUE_TEAM_ID_TIME_CHECK
                    int tmpCurSec = (int)Global.GetOffsetSecond(TimeUtil.NowDateTime());
                    if (CurrSecond > tmpCurSec)
                    {
                    }
#endif
                }

                _validAutoInc = AutoInc++;
                _validCurSecond = CurrSecond;
            }

            return (ThisServerId << 48) | (_validCurSecond << 16) | _validAutoInc;
        }
    }
}
