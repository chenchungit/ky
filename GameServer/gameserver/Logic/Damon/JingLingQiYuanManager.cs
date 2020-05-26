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
using Tmsk.Contract;
using GameServer.Logic.Damon;
using GameServer.Logic.Goods;

namespace GameServer.Logic
{
    /// <summary>
    /// 王城战管理
    /// </summary>
    public class JingLingQiYuanManager : IManager
    {
        #region 标准接口

        private static JingLingQiYuanManager instance = new JingLingQiYuanManager();

        public static JingLingQiYuanManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 配置和运行时数据
        /// </summary>
        public JingLingQiYuanData RuntimeData = new JingLingQiYuanData();

        public bool initialize()
        {
            if (!InitConfig())
            {
                return false;
            }

            return true;
        }

        public bool startup()
        {
            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            return true;
        }

        #endregion 标准接口

        #region 初始化配置

        /// <summary>
        /// 初始化配置
        /// </summary>
        public bool InitConfig()
        {
            XElement xml = null;
            string fileName = "";

            lock (RuntimeData.Mutex)
            {
                try
                {
                    RuntimeData.PetGroupPropertyList.Clear();

                    fileName = "Config/PetGroupProperty.xml";
                    string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        PetGroupPropertyItem item = new PetGroupPropertyItem();
                        item.Id = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.Name = Global.GetSafeAttributeStr(node, "Name");

                        string petGoods = Global.GetSafeAttributeStr(node, "PetGoods");
                        item.PetGoodsList = ConfigParser.ParserIntArrayList(petGoods);
                        
                        string groupProperty = Global.GetSafeAttributeStr(node, "GroupProperty");
                        item.PropItem = ConfigParser.ParseEquipPropItem(groupProperty);
                        RuntimeData.PetGroupPropertyList.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    RuntimeData.PetLevelAwardList.Clear();

                    fileName = "Config/PetLevelAward.xml";
                    string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        PetLevelAwardItem item = new PetLevelAwardItem();
                        item.Id = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.Level = (int)Global.GetSafeAttributeLong(node, "Level");
                        
                        string shuXing = Global.GetSafeAttributeStr(node, "ShuXing");
                        item.PropItem = ConfigParser.ParseEquipPropItem(shuXing);

                        RuntimeData.PetLevelAwardList.Add(item);
                    }

                    RuntimeData.PetLevelAwardList.Sort((x, y) => { return x.Level - y.Level; });
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }

                try
                {
                    RuntimeData.PetTianFuAwardList.Clear();

                    fileName = "Config/PetTianFuAward.xml";
                    string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                    xml = XElement.Load(fullPathFileName);
                    IEnumerable<XElement> nodes = null;
                    nodes = xml.Elements();
                    foreach (var node in nodes)
                    {
                        PetTianFuAwardItem item = new PetTianFuAwardItem();
                        item.Id = (int)Global.GetSafeAttributeLong(node, "ID");
                        item.TianFuNum = (int)Global.GetSafeAttributeLong(node, "TianFuNum");

                        string shuXing = Global.GetSafeAttributeStr(node, "ShuXing");
                        item.PropItem = ConfigParser.ParseEquipPropItem(shuXing);

                        RuntimeData.PetTianFuAwardList.Add(item);
                    }

                    RuntimeData.PetTianFuAwardList.Sort((x, y) => { return x.TianFuNum - y.TianFuNum; });
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(string.Format("加载xml配置文件:{0}, 失败。{1}", fileName, ex.ToString()));
                    return false;
                }
            }

            try
            {
                RuntimeData.PetSkillAwardList.Clear();

                fileName = "Config/PetSkillGroupProperty.xml";
                string fullPathFileName = Global.GameResPath(fileName); //Global.IsolateResPath(fileName);
                xml = XElement.Load(fullPathFileName);
                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    PetSkillGroupInfo config = new PetSkillGroupInfo();
                    config.GroupID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));

                    config.SkillList = new List<int>();
                    string skills = Global.GetDefAttributeStr(xmlItem, "SkillList", "");
                    if (!string.IsNullOrEmpty(skills))
                    {
                        string[] arr = skills.Split('|');
                        foreach (string s in arr)
                        {
                            config.SkillList.Add(int.Parse(s));
                        }
                    }

                    config.SkillNum = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "SkillNum", "0"));
                    string prop = Global.GetDefAttributeStr(xmlItem, "Property", "0");
                    config.GroupProp = GetGroupProp(prop);

                    RuntimeData.PetSkillAwardList.Add(config);
                }

                RuntimeData.PetSkillAwardList.Sort((x, y) => { return x.GroupID - y.GroupID; });
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("加载[{0}]时出错!!!", fileName));
            }

            return true;
        }

        private EquipPropItem GetGroupProp(string strEffect)
        {
            if (string.IsNullOrEmpty(strEffect))
                return null;

            EquipPropItem item = new EquipPropItem();
            string[] arrEffect = strEffect.Split('|');
            foreach (string effect in arrEffect)
            {
                string[] arr = effect.Split(',');
                int id = (int)Enum.Parse(typeof(ExtPropIndexes), arr[0]);
                double value = double.Parse(arr[1]);
                item.ExtProps[id] += value;
            }

            return item;
        }
 
        #endregion 初始化配置

        #region 角色属性加成

        /// <summary>
        /// 精灵奇缘属性二级类型值定义
        /// </summary>
        private static class SubPropsTypes
        {
            public const int Level = 0;
            public const int TianFuNum = 1;
            public const int PetGroup = 2;
            public const int PetSkill = 3;
        }

        /// <summary>
        /// 重新计算和设置角色从精灵奇缘系统活动的属性
        /// </summary>
        /// <param name="client"></param>
        /// <param name="notifyPorpsChangeInfo"></param>
        public void RefreshProps(GameClient client, bool notifyPorpsChangeInfo = true)
        {
            // 如果1.5的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot5))
            {
                return ;
            }

            int sumPetLevel = 0;
            int findPetLevel = 0;
            int sumPetTianFuNum = 0;
            int findPetTianFuNum = 0;
            List<PetSkillInfo> petSkillList = new List<PetSkillInfo>();

            EquipPropItem petLevelAwardItem = null;
            EquipPropItem petTianFuAwardItem = null;
            EquipPropItem petSkillAwardItem = null;

            Dictionary<int, GoodsData> havingPetDict = new Dictionary<int, GoodsData>();
            Dictionary<int, EquipPropItem> groupPropItemDict = new Dictionary<int, EquipPropItem>();

            List<GoodsData> demonGoodsList = DamonMgr.GetDemonGoodsDataList(client);
            foreach (var goodsData in demonGoodsList)
            {
                GoodsData existGoodsData;
                if (!havingPetDict.TryGetValue(goodsData.GoodsID, out existGoodsData))
                {
                    existGoodsData = new GoodsData();
                    existGoodsData.GoodsID = goodsData.GoodsID;
                    existGoodsData.GCount = 0;
                    havingPetDict[existGoodsData.GoodsID] = existGoodsData;
                }

                existGoodsData.GCount++;
                sumPetLevel += goodsData.Forge_level + 1; //潜规则,客户端显示的是这个值+1
                sumPetTianFuNum += Global.GetEquipExcellencePropNum(goodsData);

                petSkillList.AddRange(PetSkillManager.GetPetSkillInfo(goodsData));
            }

            lock (RuntimeData.Mutex)
            {
                //等级奇缘
                foreach (var item in RuntimeData.PetLevelAwardList)
                {
                    if (sumPetLevel >= item.Level && item.Level > findPetLevel)
                    {
                        findPetLevel = item.Level;
                        petLevelAwardItem = item.PropItem;
                    }
                }

                //天赋奇缘
                foreach (var item in RuntimeData.PetTianFuAwardList)
                {
                    if (sumPetTianFuNum >= item.TianFuNum && item.TianFuNum > findPetTianFuNum)
                    {
                        findPetTianFuNum = item.TianFuNum;
                        petTianFuAwardItem = item.PropItem;
                    }
                }

                //精灵组合
                foreach (var item in RuntimeData.PetGroupPropertyList)
                {
                    groupPropItemDict[item.Id] = null;
                    bool avalid = true;
                    foreach (var list in item.PetGoodsList)
                    {
                        GoodsData existGoodsData;
                        if (!havingPetDict.TryGetValue(list[0], out existGoodsData) || existGoodsData.GCount < list[1])
                        {
                            avalid = false;
                            break;
                        }
                    }

                    if (avalid)
                    {
                        groupPropItemDict[item.Id] = item.PropItem;
                    }
                }

                //精灵技能
                foreach (var item in RuntimeData.PetSkillAwardList)
                {
                    int sum = 0;
                    foreach (var p in item.SkillList)
                    {
                        var temp = from info in petSkillList
                                   where info.PitIsOpen && info.SkillID > 0 && info.SkillID == p
                                   select info;

                        if (temp.Any())
                            sum += temp.Count();
                    }

                    if (sum < item.SkillNum) break;

                    petSkillAwardItem = item.GroupProp;
                }

            }

            client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.JingLingQiYuan, SubPropsTypes.Level, petLevelAwardItem);
            client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.JingLingQiYuan, SubPropsTypes.TianFuNum, petTianFuAwardItem);

            foreach (var groupPropItem in groupPropItemDict)
            {
                client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.JingLingQiYuan, SubPropsTypes.PetGroup, groupPropItem.Key, groupPropItem.Value);
            }

            client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.JingLingQiYuan, SubPropsTypes.PetSkill, petSkillAwardItem);

            if (notifyPorpsChangeInfo)
            {
                //通知客户端属性变化
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
        }

        #endregion 角色属性加成
    }
}
