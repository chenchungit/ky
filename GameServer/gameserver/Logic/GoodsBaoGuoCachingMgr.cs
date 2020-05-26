using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
//using System.Windows.Media.Animation;
using System.Threading;
using Server.Data;
using GameServer.Server;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using System.Net.Sockets;
using HSGameEngine.Tools.AStar;
//using System.Windows.Forms;
using System.Text;

namespace GameServer.Logic
{
    /// <summary>
    /// 物品包的配置缓存
    /// </summary>
    public class GoodsBaoGuoCachingMgr
    {
        /// <summary>
        /// 物品包项缓存
        /// </summary>
        private static Dictionary<int, List<GoodsData>> _GoodsBaoGuoDict = null;

        /// <summary>
        /// 根据物品的包裹ID，获取物品列表
        /// </summary>
        /// <param name="baoguoID"></param>
        /// <returns></returns>
        public static List<GoodsData> FindGoodsBaoGuoByID(int baoguoID)
        {
            List<GoodsData> goodsDataList = null;
            _GoodsBaoGuoDict.TryGetValue(baoguoID, out goodsDataList);
            return goodsDataList;
        }

        /// <summary>
        /// 加载缓存物品包项的词典
        /// </summary>
        public static int LoadGoodsBaoGuoDict()
        {
            try
            {
                Dictionary<int, List<GoodsData>> goodsBaoGuoDict = new Dictionary<int, List<GoodsData>>();
                foreach (var systemGoodsBaoGuoItem in GameManager.systemGoodsBaoGuoMgr.SystemXmlItemDict.Values)
                {
                    int baoguoID = systemGoodsBaoGuoItem.GetIntValue("ID");
                    string goodsIDs = systemGoodsBaoGuoItem.GetStringValue("Item");
                    if (string.IsNullOrEmpty(goodsIDs))
                    {
                        LogManager.WriteLog(LogTypes.Warning, string.Format("加载物品包时, 读取物品列表错误, BaoguoID={0}", baoguoID));
                        continue;
                    }

                    string[] goodsIDFields = goodsIDs.Split('|');
                    if (null == goodsIDFields || goodsIDFields.Length <= 0)
                    {
                        LogManager.WriteLog(LogTypes.Warning, string.Format("加载物品包时, 物品列表格式错误, BaoguoID={0}, List={1}", baoguoID, goodsIDFields));
                        continue;
                    }

                    List<GoodsData> goodsDataList = new List<GoodsData>();
                    for (int i = 0; i < goodsIDFields.Length; i++)
                    {
                        string[] goodsPropFields = goodsIDFields[i].Trim().Split(',');
                        if (null == goodsPropFields || goodsPropFields.Length != 7)
                        {
                            LogManager.WriteLog(LogTypes.Warning, string.Format("加载物品包时, 物品列表格中的物品配置项错误, BaoguoID={0}, GoodsItem={1}", baoguoID, goodsPropFields));
                            continue;
                        }

                        GoodsData goodsData = new GoodsData()
                        {
                            Id = i,
                            GoodsID = Global.SafeConvertToInt32(goodsPropFields[0]),
                            Using = 0,
                            Forge_level = Global.SafeConvertToInt32(goodsPropFields[3]),
                            Starttime = "1900-01-01 12:00:00",
                            Endtime = Global.ConstGoodsEndTime,
                            Site = 0,
                            Quality = 0,
                            Props = "",
                            GCount = Global.SafeConvertToInt32(goodsPropFields[1]),
                            Binding = Global.SafeConvertToInt32(goodsPropFields[2]),
                            Jewellist = "",
                            BagIndex = 0,
                            AddPropIndex = 0,
                            BornIndex = 0,
                            Lucky = Global.SafeConvertToInt32(goodsPropFields[5]),
                            Strong = 0,
                            ExcellenceInfo = Global.SafeConvertToInt32(goodsPropFields[6]),
                            AppendPropLev = Global.SafeConvertToInt32(goodsPropFields[4]),
                        };

                        goodsDataList.Add(goodsData);
                    }

                    goodsBaoGuoDict[baoguoID] = goodsDataList;
                }

                _GoodsBaoGuoDict = goodsBaoGuoDict;
                return 0;
            }
            catch (Exception)
            {
            }

            return -1;
        }
    }
}
