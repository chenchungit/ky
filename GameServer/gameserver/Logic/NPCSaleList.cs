using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// NPC交易项
    /// </summary>
    public class NPCSaleItem
    {
        /// <summary>
        /// 物品的出售类型列表
        /// </summary>
        public Dictionary<int, bool> SaleTypesDict = new Dictionary<int, bool>();

        /// <summary>
        /// 物品的铜钱价格
        /// </summary>
        public int Money1Price = 0;

        /// <summary>
        

        /// </summary>
        public int YinLiangPrice = 0;

        /// <summary>
        /// 物品天地精元的价格
        /// </summary>
        public int TianDiJingYuanPrice = 0;

        /// <summary>
        /// 物品的猎杀值价格
        /// </summary>
        public int LieShaZhiPrice = 0;

        /// <summary>
        /// 物品的积分价格
        /// </summary>
        public int JiFenPrice = 0;

        /// <summary>
        /// 物品的军功价格
        /// </summary>
        public int JunGongPrice = 0;

        /// <summary>
        /// 物品的战魂价格
        /// </summary>
        public int ZhanHunPrice = 0;

        /// <summary>
        /// 锻造级别
        /// </summary>
        public int Forge_level;

        /// <summary>
        /// 装备的幸运值
        /// </summary>
        public int Lucky;

        // 新增物品属性 [12/13/2013 LiaoWei]
        /// <summary>
        /// 卓越信息 -- 一个32位int 每位代表一个卓越属性
        /// </summary>
        public int ExcellenceInfo;

        // 新增物品属性 [12/18/2013 LiaoWei]
        /// <summary>
        /// 追加等级
        /// </summary>
        public int AppendPropLev;
    }

    /// <summary>
    /// NPC交易列表
    /// </summary>
    public class NPCSaleList
    {
        /// <summary>
        /// 物品ID字典
        /// </summary>
        private Dictionary<int, NPCSaleItem> _SaleIDSDict = null;

        /// <summary>
        /// 物品ID字典属性
        /// </summary>
        public Dictionary<int, NPCSaleItem> SaleIDSDict
        {
            get { return _SaleIDSDict; }
        }

        /// <summary>
        /// 加载字典列表
        /// </summary>
        /// <param name="fileName"></param>
        public bool LoadSaleList()
        {
            string fileName = string.Format("Config/NPCSaleList.xml");
            XElement xml = XElement.Load(Global.GameResPath(fileName));
            if (null == xml)
            {
                throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            IEnumerable<XElement> saleNodes = xml.Elements("SaleList").Elements();
            if (null == saleNodes) return false;

            Dictionary<int, NPCSaleItem> saleIDSDict = new Dictionary<int, NPCSaleItem>();

            foreach (var xmlNode in saleNodes)
            {
                int saleType = (int)Global.GetSafeAttributeLong(xmlNode, "SaleType");
                string saleItems = (string)Global.GetSafeAttributeStr(xmlNode, "Items"); 
                string[] itemFields = saleItems.Split('|');
                for (int i = 0; i < itemFields.Length; i++)
                {

                    string[] fields2 = itemFields[i].Split(',');
                    if (fields2.Length != 5)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("加载NPC出售列表时, 物品配置项个数错误，忽略。{0}", itemFields[i]));
                        continue;
                    }


                    XElement element = null;

                    try
                    {
                        element = Global.GetSafeXElement(Global.XmlInfo[Global.GAME_CONFIG_GOODS_NAME], "Item", "ID", fields2[0]);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("加载NPC出售列表时, 物品不存在，忽略。GoodsID={0}", itemFields[0]));
                        continue;
                    }

                    int key = (int)Global.GetSafeAttributeLong(element, "ID");
                    NPCSaleItem nPCSaleItem = null;
                    if (!saleIDSDict.TryGetValue(key, out nPCSaleItem))
                    {
                        nPCSaleItem = new NPCSaleItem()
                        {
                            Money1Price = (int)Global.GetSafeAttributeLong(element, "PriceOne"),
                            YinLiangPrice = (int)Global.GetSafeAttributeLong(element, "PriceTwo"),
                            TianDiJingYuanPrice = (int)Global.GetSafeAttributeLong(element, "JinYuanPrice"),
                            LieShaZhiPrice = (int)Global.GetSafeAttributeLong(element, "LieShaPrice"),
                            JiFenPrice = (int)Global.GetSafeAttributeLong(element, "JiFenPrice"),
                            ZhanHunPrice = (int)Global.GetSafeAttributeLong(element, "ZhanHunPrice"),
                            Forge_level = Math.Max(0, Global.SafeConvertToInt32(fields2[1])),
                            AppendPropLev = Math.Max(0, Global.SafeConvertToInt32(fields2[2])),
                            Lucky = Math.Max(0, Global.SafeConvertToInt32(fields2[3])),
                            ExcellenceInfo = Math.Max(0, Global.SafeConvertToInt32(fields2[4])),
                        };
                    }

                    nPCSaleItem.SaleTypesDict[saleType] = true;
                    saleIDSDict[key] = nPCSaleItem;
                }
            }

            _SaleIDSDict = saleIDSDict;

            return true;
        }

        /// <summary>
        // 重新加载
        /// </summary>
        /// <returns></returns>
        public bool ReloadSaleList()
        {
            try
            {
                return LoadSaleList();
            }
            catch (Exception)
            {
            }

            return false;
        }
    }
}
