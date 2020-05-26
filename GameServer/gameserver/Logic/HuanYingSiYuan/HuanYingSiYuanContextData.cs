using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Windows;

namespace GameServer.Logic
{
    public class ShengBeiBufferListData
    {
        public DateTime EndTime;
        public long Seconds;
        public List<HuanYingSiYuanShengBeiContextData> ShengBeiContextDataList = new List<HuanYingSiYuanShengBeiContextData>();
    }

    public class HuanYingSiYuanRoleData
    {
        public int RoleId;
        public int GameId;
        public int FuBenSeqId;
        public int Side;
    }

    /// <summary>
    /// 幻影寺院圣杯上下文对象
    /// </summary>
    public class HuanYingSiYuanClientContextData
    {
        public int GameId;

        public int UniqueId = 0;
        public int FuBenSeqId = 0;
        public int ShengBeiId = 0;
        public int BufferGoodsId = 0;
        public int Time = 5;

        public int OwnerRoleId = 0;
        public long EndTicks;
    }

    /// <summary>
    /// 幻影寺院圣杯上下文对象
    /// </summary>
    public class HuanYingSiYuanShengBeiContextData
    {
        public int UniqueId;
        public int FuBenSeqId;
        public int CopyMapID;
        public int ShengBeiId;
        public int BufferGoodsId;
        public int Time; //5
        public int Score; //20

        public int MonsterId;
        public int PosX;
        public int PosY;

        public int OwnerRoleId;
        public long EndTicks;

        /// <summary>
        /// Buffer属性
        /// </summary>
        public double[] BufferProps;
    }

    public class HuanYingSiYuanLianShaContextData
    {
        public int KillNum;

        /// <summary>
        /// 总得分,包括采集和击杀获得
        /// </summary>
        public int TotalScore;
    }
}
