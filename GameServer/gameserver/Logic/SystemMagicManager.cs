using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 职业/级别/技能ID索引
    /// </summary>
    public class SystemMagicManager
    {
        #region 职业/级别/技能ID索引

        private Dictionary<int, SystemXmlItem> _MagicItemsDict = new Dictionary<int, SystemXmlItem>();

        /// <summary>
        /// 快速索引词典对象
        /// </summary>
        public Dictionary<int, SystemXmlItem> MagicItemsDict
        {
            get { return _MagicItemsDict; }
        }

        /// <summary>
        /// 加载魔法的职业/级别/技能ID索引
        /// </summary>
        /// <param name="systemMagicMgr"></param>
        public void LoadMagicItemsDict(SystemXmlItems systemMagicMgr)
        {
            foreach (var key in systemMagicMgr.SystemXmlItemDict.Keys)
            {
                SystemXmlItem systemMagic = systemMagicMgr.SystemXmlItemDict[key];

                int intKey = systemMagic.GetIntValue("ID");
                _MagicItemsDict[intKey] = systemMagic;
            }
        }

        #endregion 职业/级别/技能ID索引
    }
}
