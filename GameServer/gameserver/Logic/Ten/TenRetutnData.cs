using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Server.Data;

namespace GameServer.Logic
{
    public class TenRetutnAwardsData
    {
        /// <summary>
        /// 礼包id
        /// </summary>
        public int ID;

        public int ChongZhiZhuanShi;

        public AwardsItemList GoodsID1 = new AwardsItemList();

        public AwardsItemList GoodsID2 = new AwardsItemList();

        public string MailUser = "";

        /// <summary>
        /// 邮件标题
        /// </summary>
        public string MailTitle = "";

        /// <summary>
        /// 邮件内容
        /// </summary>
        public string MailContent = "";

        public string UserList;
    }

    /// <summary>
    /// 应用宝礼包数据
    /// </summary>
    public class TenRetutnData
    {
        public object Mutex = new object();

        public DateTime BeginTime = DateTime.MaxValue;

        public DateTime FinishTime = DateTime.MinValue;

        public DateTime NotLoggedInBegin = DateTime.MinValue;

        public DateTime NotLoggedInFinish = DateTime.MaxValue;

        public string BeginTimeStr;

        public string FinishTimeStr;

        /// <summary>
        ///基本信息
        /// </summary>
        public Dictionary<int, TenRetutnAwardsData> _tenAwardDic = new Dictionary<int, TenRetutnAwardsData>();

        public Dictionary<string, int> _tenUserIdAwardsDict = new Dictionary<string, int>();

        public bool SystemOpen;
    }
}
