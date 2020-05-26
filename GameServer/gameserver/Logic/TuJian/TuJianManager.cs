using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using GameServer.Server;
using Server.Tools.Pattern;
using Server.Tools;
using GameServer.Logic.ActivityNew.SevenDay;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Core.GameEvent;

namespace GameServer.Logic.TuJian
{
    class _AttrValue
    {
        public int MinDefense = 0;
        public int MaxDefense = 0;
        public int MinMDefense = 0;
        public int MaxMDefense = 0;
        public int MinAttack = 0;
        public int MaxAttack = 0;
        public int MinMAttack = 0;
        public int MaxMAttack = 0;
        public int HitV = 0;
        public int Dodge = 0;
        public int MaxLifeV = 0;

        public _AttrValue Add(_AttrValue other)
        {
            if (other != null)
            {
                this.MinDefense += other.MinDefense;
                this.MaxDefense += other.MaxDefense;
                this.MinMDefense += other.MinMDefense;
                this.MaxMDefense += other.MaxMDefense;
                this.MinAttack += other.MinAttack;
                this.MaxAttack += other.MaxAttack;
                this.MinMAttack += other.MinMAttack;
                this.MaxMAttack += other.MaxMAttack;
                this.HitV += other.HitV;
                this.Dodge += other.Dodge;
                this.MaxLifeV += other.MaxLifeV;
            }

            return this;
        }
    }

    // 每一张地图都是一个图鉴type
    class TuJianType
    {
        public int TypeID;                  //  图鉴类型
        public string Name;                // 图鉴名字，方便调试观察
        public int OpenChangeLife;      // 图鉴开启转生
        public int OpenLevel;           //图鉴开启等级
        public int ItemCnt;             // 本类型一共含有多少个具体图鉴项
        public _AttrValue AttrValue = null; //所有图鉴项都完成后的属性加成
        public List<int> ItemList = new List<int>(); //图鉴item子项
    }

    // 每一个地图中的一种怪是一种图鉴Item
    class TuJianItem
    {
        public int ItemID;          //  图鉴项ID
        public int TypeID;          // 图鉴类型
        public string Name;         // 图鉴项名字
        public int CostGoodsID;     //激活本项图鉴需要消耗的物品ID
        public int CostGoodsCnt; //激活本项图鉴需要消耗的物品数量
        public _AttrValue AttrValue = null;//激活本项图鉴后属性加成
    }

    public class TuJianManager : SingletonTemplate<TuJianManager>
    {
        private TuJianManager() { }

        private const string TuJianType_fileName = "Config/TuJianType.xml";
        private const string TuJianItem_fileName = "Config/TuJianItems.xml";

        // key:图鉴类型 value:图鉴类型信息
        private Dictionary<int, TuJianType> TuJianTypes = new Dictionary<int, TuJianType>();
        // key:图鉴项  value:图鉴item信息
        private Dictionary<int, TuJianItem> TuJianItems = new Dictionary<int, TuJianItem>();

        public void LoadConfig()
        {
            bool bFailed = false;
            if (!loadTuJianType() || !loadTuJianItem())
            {
                bFailed = true;
            }

            // 最好check一下，保证每个图鉴item所属的type存在，并且个数配置一致
            bool _check = true;
            if (_check && !bFailed)
            {
                // 统计出"Config/TuJianItems.xml"每个type下配置的item个数
                Dictionary<int, int> itemCntByType = new Dictionary<int, int>();
                foreach (var kvp in TuJianItems)
                {
                    int itemID = kvp.Key;
                    int typeID = kvp.Value.TypeID;
                    if (!itemCntByType.ContainsKey(typeID))
                    {
                        itemCntByType.Add(typeID, 0);
                    }
                    itemCntByType[typeID]++;
                }

                // 检查统计出来的item与"Config/TuJianType.xml"众每个type包含的子item个数是否相同
                foreach (var kvp in itemCntByType)
                {
                    int typeID = kvp.Key;
                    int itemCnt = kvp.Value;

                    TuJianType type = null;
                    if (!TuJianTypes.TryGetValue(typeID, out type) || type.ItemCnt != itemCnt)
                    {
                        bFailed = true;
                        break;
                    }
                }
            }

            if (bFailed)
            {
                LogManager.WriteLog(LogTypes.Error, TuJianType_fileName + " " + TuJianItem_fileName + " 配置文件出错，请检查文件是否存在或者配置的item个数是否一致");
            }
        }

        // 加载图鉴类型信息
        private bool loadTuJianType()
        {
            try
            {
                XElement xmlFile = GeneralCachingXmlMgr.GetXElement(Global.IsolateResPath(TuJianType_fileName));
                if (xmlFile == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("{0}不存在!", TuJianType_fileName));
                    return false;
                }

                TuJianTypes.Clear();
                IEnumerable<XElement> TuJianXEle = xmlFile.Elements("TuJian").Elements();
                foreach (var xmlItem in TuJianXEle)
                {
                    if (null != xmlItem)
                    {
                        TuJianType tjType = new TuJianType();
                        tjType.TypeID = (int)Global.GetSafeAttributeDouble(xmlItem, "ID");
                        tjType.Name = Global.GetSafeAttributeStr(xmlItem, "Name");
                        tjType.ItemCnt = (int)Global.GetSafeAttributeDouble(xmlItem, "TuJianNum");

                        string sLevelInfo = Global.GetSafeAttributeStr(xmlItem, "KaiQiLevel");
                        string[] sArrayLevelInfo = sLevelInfo.Split(',');
                        tjType.OpenChangeLife = Global.SafeConvertToInt32(sArrayLevelInfo[0]);
                        tjType.OpenLevel = Global.SafeConvertToInt32(sArrayLevelInfo[1]);

                        string strAttrs = Global.GetSafeAttributeStr(xmlItem, "ShuXiangJiaCheng");
                        tjType.AttrValue = analyseToAttrValues(strAttrs);

                        TuJianTypes.Add(tjType.TypeID, tjType);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("{0}读取出错!", TuJianType_fileName), ex);
                return false;
            }

            return true;
        }

        // 加载图鉴项
        private bool loadTuJianItem()
        {
            try
            {
                XElement xmlFile = GeneralCachingXmlMgr.GetXElement(Global.IsolateResPath(TuJianItem_fileName));
                if (null == xmlFile)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("{0}不存在!", TuJianItem_fileName));
                    return false;
                }

                IEnumerable<XElement> TuJianXEle = xmlFile.Elements();
                foreach (var xmlItem in TuJianXEle)
                {
                    if (null != xmlItem)
                    {
                        TuJianItem item = new TuJianItem();
                        item.TypeID = (int)Global.GetSafeAttributeDouble(xmlItem, "Type");
                        item.ItemID = (int)Global.GetSafeAttributeDouble(xmlItem, "ID");
                        item.Name = Global.GetSafeAttributeStr(xmlItem, "Name");

                        string strCostGoods = Global.GetSafeAttributeStr(xmlItem, "NeedGoods");
                        if (!string.IsNullOrEmpty(strCostGoods))
                        {
                            string[] sArry = strCostGoods.Split(',');
                            item.CostGoodsID = Global.SafeConvertToInt32(sArry[0]);
                            item.CostGoodsCnt = Global.SafeConvertToInt32(sArry[1]);
                        }

                        string strAttrs = Global.GetSafeAttributeStr(xmlItem, "ShuXing");
                        item.AttrValue = analyseToAttrValues(strAttrs);

                        TuJianItems.Add(item.ItemID, item);

                        if (!TuJianTypes.ContainsKey(item.TypeID))
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("{0}配置了不存在的图鉴类型Type={1}", TuJianItem_fileName, item.TypeID));
                            return false;
                        }
                        else
                        {
                            TuJianTypes[item.TypeID].ItemList.Add(item.ItemID);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("{0}读取出错!", TuJianItem_fileName), ex);
                return false;
            }
            return true;
        }

        // 把配置的字符串属性值转化为 'attr，value'
        private _AttrValue analyseToAttrValues(string strAttrs)
        {
            if (string.IsNullOrEmpty(strAttrs)) return null;

            string[] sArry = strAttrs.Split('|');
            if (sArry == null || sArry.Length == 0) return null;

            _AttrValue result = new _AttrValue();
            foreach (var str in sArry)
            {
                string[] attr = str.Split(',');
                if (attr == null || attr.Length != 2)
                {
                    continue;
                }

                string attrName = attr[0].ToLower();
                string attrValue = attr[1];
                string[] attrTwoValue = attrValue.Split('-');

                if (attrName == "defense")
                {
                    if (attrTwoValue != null && attrTwoValue.Length == 2)
                    {
                        result.MinDefense = Global.SafeConvertToInt32(attrTwoValue[0]);
                        result.MaxDefense = Global.SafeConvertToInt32(attrTwoValue[1]);
                    }
                }
                else if (attrName == "mdefense")
                {
                    if (attrTwoValue != null && attrTwoValue.Length == 2)
                    {
                        result.MinMDefense = Global.SafeConvertToInt32(attrTwoValue[0]);
                        result.MaxMDefense = Global.SafeConvertToInt32(attrTwoValue[1]);
                    }
                }
                else if (attrName == "attack")
                {
                    if (attrTwoValue != null && attrTwoValue.Length == 2)
                    {
                        result.MinAttack = Global.SafeConvertToInt32(attrTwoValue[0]);
                        result.MaxAttack = Global.SafeConvertToInt32(attrTwoValue[1]);
                    }
                }
                else if (attrName == "mattack")
                {
                    if (attrTwoValue != null && attrTwoValue.Length == 2)
                    {
                        result.MinMAttack = Global.SafeConvertToInt32(attrTwoValue[0]);
                        result.MaxMAttack = Global.SafeConvertToInt32(attrTwoValue[1]);
                    }
                }
                else if (attrName == "hitv")
                {
                    result.HitV = Global.SafeConvertToInt32(attrTwoValue[0]);
                }
                else if (attrName == "dodge")
                {
                    result.Dodge = Global.SafeConvertToInt32(attrTwoValue[0]);
                }
                else if (attrName == "maxlifev")
                {
                    result.MaxLifeV = Global.SafeConvertToInt32(attrTwoValue[0]);
                }
            }

            return result;
        }

        // 计算图鉴系统属性加成
        public void UpdateTuJianProps(GameClient client)
        {
            if (client == null) return;
            if (client.ClientData.PictureJudgeReferInfo == null
                || client.ClientData.PictureJudgeReferInfo.Count == 0)
                return;

            // 统计每个图鉴Type激活了多少个Item
            Dictionary<int, int> activeItemByType = new Dictionary<int, int>();
            // 计算图鉴总属性
            _AttrValue totalAttrValue = new _AttrValue();

            // 计算激活的图鉴Item加成
            foreach (var kvp in client.ClientData.PictureJudgeReferInfo)
            {
                int itemID = kvp.Key;
                int itemReferCnt = kvp.Value;

                TuJianItem item = null;
                if (!TuJianItems.TryGetValue(itemID, out item))
                {
                    continue;
                }

                // 本item已激活
                if (itemReferCnt >= item.CostGoodsCnt)
                {
                    if (!activeItemByType.ContainsKey(item.TypeID))
                    {
                        activeItemByType.Add(item.TypeID, 0);
                    }
                    activeItemByType[item.TypeID]++;

                    totalAttrValue.Add(item.AttrValue);

                    if (client.ClientData.ActivedTuJianItem != null && !client.ClientData.ActivedTuJianItem.Contains(itemID))
                    {
                        client.ClientData.ActivedTuJianItem.Add(itemID);
                    }
                }
            }

            // 计算图鉴Type加成(只有所有子item全部激活的图鉴type)
            foreach (var kvp in activeItemByType)
            {
                TuJianType type = null;
                if (!TuJianTypes.TryGetValue(kvp.Key, out type))
                {
                    continue;
                }

                // 本图鉴type全部激活
                if (kvp.Value >= type.ItemCnt)
                {
                    totalAttrValue.Add(type.AttrValue);

                    if (client.ClientData.ActivedTuJianType != null && !client.ClientData.ActivedTuJianType.Contains(kvp.Key))
                    {
                        client.ClientData.ActivedTuJianType.Add(kvp.Key);
                    }
                }
            }

            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.MinAttack, totalAttrValue.MinAttack);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.MaxAttack, totalAttrValue.MaxAttack);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.MinMAttack, totalAttrValue.MinMAttack);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.MaxMAttack, totalAttrValue.MaxMAttack);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.MinDefense, totalAttrValue.MinDefense);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.MaxDefense, totalAttrValue.MaxDefense);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.MinMDefense, totalAttrValue.MinMDefense);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.MaxMDefense, totalAttrValue.MaxMDefense);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.HitV, totalAttrValue.HitV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.MaxLifeV, totalAttrValue.MaxLifeV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.TuJian, (int)ExtPropIndexes.Dodge, totalAttrValue.Dodge);
        }

        // 玩家激活图鉴
        public void HandleActiveTuJian(GameClient client, string[] itemArr)
        {
            if (itemArr == null || itemArr.Length == 0 || client == null) return;

            bool anySuccess = false;
            foreach (string strItemID in itemArr)
            {
                // 客户端请求激活的图鉴Item
                int itemID = Convert.ToInt32(strItemID);
                TuJianItem item = null;
                TuJianType type = null;
                if (!TuJianItems.TryGetValue(itemID, out item) || !TuJianTypes.TryGetValue(item.TypeID, out type))
                {
                    continue;
                }

                // 等级不满足
                if (client.ClientData.ChangeLifeCount < type.OpenChangeLife ||
                    (client.ClientData.ChangeLifeCount == type.OpenChangeLife && client.ClientData.Level < type.OpenLevel))
                {
                    continue;
                }

                int hadReferCnt = 0;
                if (client.ClientData.PictureJudgeReferInfo.ContainsKey(itemID))
                {
                    hadReferCnt = client.ClientData.PictureJudgeReferInfo[itemID];
                }

                // 已激活
                if (hadReferCnt >= item.CostGoodsCnt)
                {
                    continue;
                }

                int needReferCnt = item.CostGoodsCnt - hadReferCnt;
                int hasGoodsCnt = Global.GetTotalGoodsCountByID(client, item.CostGoodsID);
                // 材料不足
                if (hasGoodsCnt <= 0)
                {
                    continue;
                }

                // 允许提交一部分材料
                int thisTimeReferCnt = Math.Min(needReferCnt, hasGoodsCnt);
                bool usedBinding_just_placeholder = false;
                bool usedTimeLimited_just_placeholder = false;
                // 扣除物品失败
                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                    client, item.CostGoodsID, thisTimeReferCnt, false, out usedBinding_just_placeholder, out usedTimeLimited_just_placeholder))
                {
                    continue;
                }

                string strDbCmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, itemID, hadReferCnt + thisTimeReferCnt);
                string[] dbRsp = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_REFERPICTUREJUDGE, strDbCmd, client.ServerId);
                if (dbRsp == null || dbRsp.Length != 1 || Convert.ToInt32(dbRsp[0]) <= 0)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("角色RoleID={0}，RoleName={1} 激活图鉴Item={2}时，与db通信失败，物品已扣除GoodsID={3},Cnt={4}",
                        client.ClientData.RoleID, client.ClientData.RoleName, itemID, item.CostGoodsID, thisTimeReferCnt));
                    continue;
                }

                anySuccess = true;
                if (!client.ClientData.PictureJudgeReferInfo.ContainsKey(itemID))
                {
                    client.ClientData.PictureJudgeReferInfo.Add(itemID, hadReferCnt + thisTimeReferCnt);
                }
                else
                {
                    client.ClientData.PictureJudgeReferInfo[itemID] = hadReferCnt + thisTimeReferCnt;
                }
            }

            // 只有在任何一项提交成功时，才重新计算属性加成
            if (anySuccess)
            {
                UpdateTuJianProps(client);
                // 激活的图鉴项变化了，检查守护雕像的激活情况
                GuardStatueManager.Instance().OnActiveTuJian(client);

                // 七日活动
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.CompleteTuJian));

                // 通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
        }

        public bool GM_OneKeyActiveTuJianType(GameClient client, int typeId, out string failedMsg)
        {
            failedMsg = string.Empty;
            if (client == null)
            {
                failedMsg = "unknown";
                return false;
            }

            TuJianType type = null;
            if (!TuJianTypes.TryGetValue(typeId, out type))
            {
                failedMsg = "图鉴类型找不到: " + typeId.ToString();
                return false;
            }

            // 等级不满足
            if (client.ClientData.ChangeLifeCount < type.OpenChangeLife ||
                (client.ClientData.ChangeLifeCount == type.OpenChangeLife && client.ClientData.Level < type.OpenLevel))
            {
                failedMsg = "该项图鉴未开启，类型=" + typeId.ToString() + " ,需求转生：" + type.OpenChangeLife + " , 等级：" + type.OpenLevel;
                return false;
            }

            bool bRealRefer = false;
            foreach (var itemId in type.ItemList)
            {
                TuJianItem item = null;
                if (!TuJianItems.TryGetValue(itemId, out item))
                {
                    continue;
                }

                // 该子项已激活
               if ( client.ClientData.PictureJudgeReferInfo.ContainsKey(itemId) &&
                   client.ClientData.PictureJudgeReferInfo[itemId] >= item.CostGoodsCnt)
               {
                   continue;
               }

               string strDbCmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, itemId, item.CostGoodsCnt);
               string[] dbRsp = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_REFERPICTUREJUDGE, strDbCmd, client.ServerId);
               if (dbRsp == null || dbRsp.Length != 1 || Convert.ToInt32(dbRsp[0]) <= 0)
               {
                   failedMsg = "数据库异常";
                   return false;
               }

               bRealRefer = true;
               if (!client.ClientData.PictureJudgeReferInfo.ContainsKey(itemId))
               {
                   client.ClientData.PictureJudgeReferInfo.Add(itemId, item.CostGoodsCnt);
               }
               else
               {
                   client.ClientData.PictureJudgeReferInfo[itemId] = item.CostGoodsCnt;
               }
            }

            // 只有在任何一项提交成功时，才重新计算属性加成
            if (bRealRefer)
            {
                client.sendCmd(DataHelper.ObjectToTCPOutPacket<Dictionary<int, int>>(client.ClientData.PictureJudgeReferInfo, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_SPR_REFERPICTUREJUDGE));

                UpdateTuJianProps(client);
                // 激活的图鉴项变化了，检查守护雕像的激活情况
                GuardStatueManager.Instance().OnActiveTuJian(client);
                // 七日活动
                GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.CompleteTuJian));
                // 通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }

            return true;
        }
    }
}
