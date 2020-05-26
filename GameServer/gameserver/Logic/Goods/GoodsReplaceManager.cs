using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Server.Tools.Pattern;
using Server.Tools;

namespace GameServer.Logic.Goods
{
    public class ReplaceExtArg
    {
        public int CurrEquipZhuiJiaLevel = -1;
        public int CurrEquipQiangHuaLevel = -1;
        public int CurrEquipSuit = -1;

        public void Reset()
        {
            this.CurrEquipZhuiJiaLevel = -1;
            this.CurrEquipQiangHuaLevel = -1;
            this.CurrEquipSuit = -1;
        }
    }

    /// <summary>
    /// 物品替换结果
    /// </summary>
    public class GoodsReplaceResult
    {
        // 替换得到的一个具体物品id与数量
        public class ReplaceItem
        {
            public int GoodsID = 0;
            public int GoodsCnt = 0;
            public bool IsBind = false;
        }

        public int TotalGoodsCnt()
        {
            return OriginBindGoods.GoodsCnt + OriginUnBindGoods.GoodsCnt + TotalBindCnt + TotalUnBindCnt;
        }

        // 原物品
        public ReplaceItem OriginBindGoods = new ReplaceItem();
        public ReplaceItem OriginUnBindGoods = new ReplaceItem();

        // 替换得到的绑定物品列表
        public int TotalBindCnt = 0;
        public List<ReplaceItem> BindList = new List<ReplaceItem>();

        // 替换得到的非绑定物品列表
        public int TotalUnBindCnt = 0;
        public List<ReplaceItem> UnBindList = new List<ReplaceItem>();
    }

    /// <summary>
    /// 替换类物品管理器
    /// </summary>
    public class GoodsReplaceManager : SingletonTemplate<GoodsReplaceManager>
    {
        private GoodsReplaceManager() {  }

        // 对应ReplaceGoods.xml中的一条替换项，一个物品可以有多条替换项
        class ReplaceRecord
        {
            // 对应ReplaceGoods.xml中的ID属性, 用于替换的优先级
            public int seq;
            // 对应ReplaceGoods.xml中的ToType属性, 用于替换的条件检查
            public string condIdx;
            // 对应ReplaceGoods.xml中的ToTypeProperty属性, 用于替换的条件检查的参数
            public string condArg;
            // 对应ReplaceGoods.xml中的OldGoods属性, 替换前物品
            public int oldGoods;
            // 对应ReplaceGoods.xml中的NewGoods属性, 替换前物品
            public int newGoods;
        }

        private const string ReplaceCfgFile = "Config/ReplaceGoods.xml";

        public bool NeedCheckSuit(int categoriy)
        {
            if (categoriy >= 0 && categoriy <= 6) return true;
            if (categoriy >= 11 && categoriy <= 21) return true;

            return false;
        }
        /// <summary>
        /// key: oldGoods
        /// value:  多条替换项 
        /// </summary>
        private Dictionary<int, List<ReplaceRecord>> replaceDict = new Dictionary<int, List<ReplaceRecord>>();
        private Dictionary<string, ICondJudger> replaceJudgerDict = new Dictionary<string, ICondJudger>();

        public void Init()
        {
            replaceJudgerDict.Clear();
            replaceJudgerDict[CondIndex.Cond_WingSuit.ToLower()] = new CondJudger_WingSuit();
            replaceJudgerDict[CondIndex.Cond_EquipForgeLvl.ToLower()] = new CondJudger_EquipForgeLvl();
            replaceJudgerDict[CondIndex.Cond_EquipAppendLvl.ToLower()] = new CondJudger_EquipAppendLvl();
            replaceJudgerDict[CondIndex.Cond_EquipSuit.ToLower()] = new CondJudger_EquipSuit();

            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(ReplaceCfgFile));
            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(ReplaceCfgFile));
            if (xml == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", ReplaceCfgFile));
                return;
            }

            try
            {
                replaceDict.Clear();
                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null == xmlItem)
                        continue;

                    ReplaceRecord record = new ReplaceRecord();
                    record.seq = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
                    record.condIdx = Global.GetSafeAttributeStr(xmlItem, "ToType").ToLower();
                    record.condArg = Global.GetSafeAttributeStr(xmlItem, "ToTypeProperty");
                    record.oldGoods = (int)Global.GetSafeAttributeLong(xmlItem, "OldGoods");
                    record.newGoods = (int)Global.GetSafeAttributeLong(xmlItem, "NewGoods");

                    List<ReplaceRecord> recordList = null;
                    if (!replaceDict.TryGetValue(record.oldGoods, out recordList))
                    {
                        recordList = new List<ReplaceRecord>();
                        replaceDict[record.oldGoods] = recordList;
                    }

                    recordList.Add(record);
                }

                // 优先级排序
                foreach (var kvp in replaceDict)
                {
                    kvp.Value.Sort((left, right) =>
                    {
                        if (left.seq > right.seq) return 1;
                        else if (left.seq == right.seq) return 0;
                        else return -1;
                    });
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!! {1}", ReplaceCfgFile, ex.Message));
            }
        }

        public GoodsReplaceResult GetReplaceResult(GameClient client, int OriginGoods)
        {
            if (client == null) return null;

            GoodsReplaceResult result = new GoodsReplaceResult();

            result.OriginBindGoods.IsBind = true;
            result.OriginBindGoods.GoodsID = OriginGoods;
            result.OriginBindGoods.GoodsCnt = Global.GetTotalBindGoodsCountByID(client, OriginGoods);

            result.OriginUnBindGoods.IsBind = false;
            result.OriginUnBindGoods.GoodsID = OriginGoods;
            result.OriginUnBindGoods.GoodsCnt = Global.GetTotalNotBindGoodsCountByID(client, OriginGoods);

            List<ReplaceRecord> records = null;
            if (replaceDict.TryGetValue(OriginGoods, out records))
            {
                foreach (var record in records)
                {
                    ICondJudger judger = null;
                    if (!replaceJudgerDict.TryGetValue(record.condIdx, out judger))
                    {
                        // 抄不到替换规则不可替换
                        continue;
                    }

                    string strPlaceHolder = string.Empty;
                    if (!judger.Judge(client, record.condArg, out strPlaceHolder))
                    {
                        // 不满足替换条件
                        continue;
                    }

                    int replaceGoodsID = record.newGoods;
                    int bindCnt = Global.GetTotalBindGoodsCountByID(client, replaceGoodsID);
                    int unBindCnt = Global.GetTotalNotBindGoodsCountByID(client, replaceGoodsID);

                    if (bindCnt > 0)
                    {
                        GoodsReplaceResult.ReplaceItem item = new GoodsReplaceResult.ReplaceItem();
                        item.IsBind = true;
                        item.GoodsID = replaceGoodsID;
                        item.GoodsCnt = bindCnt;
                        result.TotalBindCnt += bindCnt;
                        result.BindList.Add(item);
                    }

                    if (unBindCnt > 0)
                    {
                        GoodsReplaceResult.ReplaceItem item = new GoodsReplaceResult.ReplaceItem();
                        item.IsBind = false;
                        item.GoodsID = replaceGoodsID;
                        item.GoodsCnt = unBindCnt;
                        result.TotalUnBindCnt += unBindCnt;
                        result.UnBindList.Add(item);
                    }
                }
            }

            return result;
        }
    }
}
