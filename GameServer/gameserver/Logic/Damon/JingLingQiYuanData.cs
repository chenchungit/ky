using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using System.Xml.Linq;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic.Goods;

namespace GameServer.Logic
{
    public class PetGroupPropertyItem
    {
        public int Id;
        public string Name;
        public List<List<int>> PetGoodsList = new List<List<int>>();
        public EquipPropItem PropItem;
    }

    public class PetLevelAwardItem
    {
        public int Id;
        public int Level;
        public EquipPropItem PropItem;
    }

    public class PetTianFuAwardItem
    {
        public int Id;
        public int TianFuNum;
        public EquipPropItem PropItem;
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class JingLingQiYuanData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();

        public List<PetGroupPropertyItem> PetGroupPropertyList = new List<PetGroupPropertyItem>();

        public List<PetLevelAwardItem> PetLevelAwardList = new List<PetLevelAwardItem>();

        public List<PetTianFuAwardItem> PetTianFuAwardList = new List<PetTianFuAwardItem>();

        public List<PetSkillGroupInfo> PetSkillAwardList = new List<PetSkillGroupInfo>();
    }
}
