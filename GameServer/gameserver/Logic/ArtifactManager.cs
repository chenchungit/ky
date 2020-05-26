using GameServer.Server;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic
{
    /// <summary>
    /// 神器管理类
    /// </summary>
    public class ArtifactManager : IManager
    {
         //Global.GetEquipExcellencePropNum(new GoodsData());//获得装备卓越条数


        private static ArtifactManager Instance = new ArtifactManager();
        public static ArtifactManager GetInstance()
        {
            return Instance;
        }

        #region 接口相关
        public bool initialize()
        {
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

        #endregion

        #region 常量

        public static int ARTIFACT_SUIT = 10;   //神器再造限制，10阶装备

        public enum ArtifactResultType
        {
            Success = 1,        //成功 
            Fail = 0,           //失败
            EnoOpen = -1,       //神器再造未开放
            EnoEquip = -2,      //装备不存在
            EcantUp = -3,       //该装备不能再造
            EnoZaiZao = -4,     //再造点不足
            EnoGold = -5,       //绑定金币不足
            EnoMaterial = -6,   //材料不足
            EnoBag = -7,        //背包已满
            EdelEquip = -8,     //扣除装备失败
            EaddEquip = -9      //添加装备失败
        };


        #endregion 

        #region 配置信息

        /// <summary>
        /// 神器基本信息
        /// </summary>
        private static List<ArtifactData> _artifactList = new List<ArtifactData>();

        /// <summary>
        /// 神器套装信息
        /// </summary>
        private static List<ArtifactSuitData> _artifactSuitList = new List<ArtifactSuitData>();

        /// <summary>
        /// 成就符文基本信息初始化
        /// </summary>
        public static void initArtifact()
        {
            LoadArtifactData();
            LoadArtifactSuitData();
        }

        /// <summary>
        /// 加载神器基本信息
        /// </summary>
        public static void LoadArtifactData()
        {
            string fileName = "Config/ZaiZao.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));

            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ZaiZao.xml时出错!!!文件不存在");
                return;
            }

            try
            {
                _artifactList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    ArtifactData config = new ArtifactData();
                    config.ArtifactID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    config.ArtifactName = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "Name", ""));
                    config.NewEquitID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NewEquitID", "0"));
                    config.NeedEquitID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedEquitID", "0"));
                    config.NeedGoldBind = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedBandJinBi", "0"));
                    config.NeedZaiZao = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "NeedZaiZao", "0"));
                    config.SuccessRate = (int)(Convert.ToDouble(Global.GetDefAttributeStr(xmlItem, "SuccessRate", "0"))*100);

                    string needMaterial = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "NeedGoods", ""));
                    if (needMaterial.Length > 0)
                    {
                        config.NeedMaterial = new Dictionary<int, int>();

                        string[] materials = needMaterial.Split('|');
                        foreach (string str in materials)
                        {
                            string[] one = str.Split(',');

                            int key = int.Parse(one[0]);
                            int value = int.Parse(one[1]);
                            config.NeedMaterial.Add(key, value);
                        }
                    }

                    string failMaterial = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "XiaoHuiGoods", ""));
                    if (failMaterial.Length > 0)
                    {
                        config.FailMaterial = new Dictionary<int, int>();

                        string[] materials = failMaterial.Split('|');
                        foreach (string str in materials)
                        {
                            string[] one = str.Split(',');

                            int key = int.Parse(one[0]);
                            int value = int.Parse(one[1]);
                            config.FailMaterial.Add(key, value);
                        }
                    }

                    _artifactList.Add(config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/ZaiZao.xml时文件出错", ex);
            }
        }

        /// <summary>
        /// 根据需要装备id获得神器数据
        /// </summary>
        /// <param name="needID"></param>
        /// <returns></returns>
        public static ArtifactData GetArtifactDataByNeedId(int needID)
        {
            foreach (ArtifactData d in _artifactList)
            {
                if (d.NeedEquitID == needID)
                    return d;
            }

            return null;
        }

        /// <summary>
        /// 加载神器套装信息
        /// </summary>
        public static void LoadArtifactSuitData()
        {
            string fileName = "Config/TaoZhuangProps.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));

            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (null == xml)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/TaoZhuangProps.xml时出错!!!文件不存在");
                return;
            }

            try
            {
                _artifactSuitList.Clear();

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (xmlItem == null) continue;

                    ArtifactSuitData config = new ArtifactSuitData();
                    config.SuitID = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "ID", "0"));
                    config.SuitName = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "Name", ""));
                    config.IsMulti = Convert.ToInt32(Global.GetDefAttributeStr(xmlItem, "Multi", "0"))>0;

                    string equipIdStr = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "GoodsID", "0"));
                    if (equipIdStr.Length > 0)
                    {
                        config.EquipIDList = new List<int>();

                        string[] all = equipIdStr.Split(',');
                        foreach (string one in all)
                        {
                            config.EquipIDList.Add(int.Parse(one));
                        }
                    }
                    
                    string addString = Convert.ToString(Global.GetDefAttributeStr(xmlItem, "TaoZhuangProps", ""));
                    if (addString.Length > 0)
                    {
                        config.SuitAttr = new Dictionary<int, Dictionary<string, string>>();
                        string[] addArr = addString.Split('|');
                        foreach (string str in addArr)
                        {
                            string[] oneArr = str.Split(',');

                           int count = int.Parse(oneArr[0]);
                           if (config.SuitAttr.ContainsKey(count))
                           {
                               config.SuitAttr[count].Add(oneArr[1],oneArr[2]);
                           }
                           else
                           {
                               Dictionary<string, string> value = new Dictionary<string, string>();
                               value.Add(oneArr[1], oneArr[2]);
                               config.SuitAttr.Add(int.Parse(oneArr[0]),value);
                           }
                        }
                    }

                    _artifactSuitList.Add(config);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, "加载Config/TaoZhuangProps.xml时文件出错", ex);
            }
        }

        /// <summary>
        /// 根据需要装备id获得神器数据
        /// </summary>
        /// <param name="needID"></param>
        /// <returns></returns>
        public static ArtifactSuitData GetArtifactSuitDataByEquipID(int equipID)
        {
            foreach (ArtifactSuitData d in _artifactSuitList)
            {
                foreach (int id in d.EquipIDList)
                {
                    if (id == equipID)
                        return d;
                }
            }

            return null;
        }

        public static ArtifactSuitData GetArtifactSuitDataBySuitID(int suitID)
        {
            foreach (ArtifactSuitData d in _artifactSuitList)
            {
                if (d.SuitID == suitID)
                    return d;
            }

            return null;
        }

        #endregion

        #region 神器再造

        /// <summary>
        /// 神器再造
        /// </summary>
        /// <param name="client"></param>
        /// <param name="equipID"></param>
        /// <returns></returns>
        public static ArtifactResultData UpArtifact(GameClient client, int equipID, bool isUseBind)
        {
            ArtifactResultData result = new ArtifactResultData();

            #region 检查
            //神器再造功能开放
            bool isOpen = GlobalNew.IsGongNengOpened(client, GongNengIDs.Artifact);
            if (!isOpen)
            {
                result.State = (int)ArtifactResultType.EnoOpen;
                return result;
            }

            // 从背包中找装备
            GoodsData equipData = Global.GetGoodsByDbID(client, equipID);
            if (equipData == null)
            {
                result.State = (int)ArtifactResultType.EnoEquip;
                return result;
            }

            //类型检测Categoriy=0-6、11-21的道具         
            int catetoriy = Global.GetGoodsCatetoriy(equipData.GoodsID);
            bool isCanCatetoriy = (catetoriy >= 0 && catetoriy <= 6) || (catetoriy >= 11 && catetoriy <= 21);
            if (!isCanCatetoriy)
            {
                result.State = (int)ArtifactResultType.EcantUp;
                return result;
            }

            //Suit=10
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(equipData.GoodsID, out systemGoods))
            {
                result.State = (int)ArtifactResultType.EnoEquip;
                return result;
            }

            int nSuitID = systemGoods.GetIntValue("SuitID");
            if (nSuitID < ARTIFACT_SUIT)
            {
                result.State = (int)ArtifactResultType.EcantUp;
                return result;
            }

            //神器基本数据
            ArtifactData artifactDataBasic = GetArtifactDataByNeedId(equipData.GoodsID);
            if (artifactDataBasic == null)
            {
                result.State = (int)ArtifactResultType.EcantUp;
                return result;
            }

            //再造点
            bool enoughZaiZao = Global.IsRoleHasEnoughMoney(client, artifactDataBasic.NeedZaiZao, (int)MoneyTypes.ZaiZao) > 0;
            if (!enoughZaiZao)
            {
                result.State = (int)ArtifactResultType.EnoZaiZao;
                return result;
            }

            //绑定金币
            int goldBind = Global.GetTotalBindTongQianAndTongQianVal(client); ;
            if (artifactDataBasic.NeedGoldBind > goldBind)
            {
                result.State = (int)ArtifactResultType.EnoGold;
                return result;
            }

            //材料——成功扣除
            foreach (var d in artifactDataBasic.NeedMaterial)
            {
                int materialId = d.Key;
                int count = d.Value;

                int totalCount = Global.GetTotalGoodsCountByID(client, materialId);
                if (totalCount < count)
                {
                    result.State = (int)ArtifactResultType.EnoMaterial;
                    return result;
                }
            }

            //材料——失败扣除
            foreach (var d in artifactDataBasic.FailMaterial)
            {
                int materialId = d.Key;
                int count = d.Value;

                int totalCount = Global.GetTotalGoodsCountByID(client, materialId);
                if (totalCount < count)
                {
                    result.State = (int)ArtifactResultType.EnoMaterial;
                    return result;
                }
            }

            //背包已满
            int freeBagIndex = Global.GetIdleSlotOfBagGoods(client);
            if (freeBagIndex < 0)
            {
                result.State = (int)ArtifactResultType.EnoBag;
                return result;
            }
            #endregion

            #region 扣除

            //扣除绑定金币
            if (!Global.SubBindTongQianAndTongQian(client, artifactDataBasic.NeedGoldBind, "神器再造"))
            {
                result.State = (int)ArtifactResultType.EnoGold;
                return result;
            }

            //几率
            //bool isSuccess = true;
            bool isSuccess = false;
            int failCount = Global.GetRoleParamsInt32FromDB(client, RoleParamName.ArtifactFailCount); //失败次数
            int failMax = (int)GameManager.systemParamsList.GetParamValueIntByName("ZaiZaoBaoDi");
            if (failCount >= failMax)
            {
                isSuccess = true;
                failCount = 0;
                SetArtifactFailCount(client, failCount);
            }
            else
            {
                int rate = Global.GetRandomNumber(0, 100);
                if (rate < artifactDataBasic.SuccessRate)
                {
                    isSuccess = true;
                    failCount = 0;
                    SetArtifactFailCount(client, failCount);
                }
            }

            bool useBind = false;
            bool useTimeLimit = false;
            //失败------------------------------------------------------------------------
            if (!isSuccess)
            {
                //扣除材料
                foreach (var d in artifactDataBasic.FailMaterial)
                {
                    int materialId = d.Key;
                    int count = d.Value;

                    bool isOk = Global.UseGoodsBindOrNot(client, materialId, count, isUseBind, out useBind, out useTimeLimit) >= 1;
                    if (!isOk)
                    {
                        result.State = (int)ArtifactResultType.EnoMaterial;
                        return result;
                    }
                }

                failCount++;
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ArtifactFailCount, failCount, true);

                GameManager.logDBCmdMgr.AddDBLogInfo(artifactDataBasic.NewEquitID, artifactDataBasic.ArtifactName, "神器再造失败", client.ClientData.RoleName, client.ClientData.RoleName, "再造", 1, client.ClientData.ZoneID, client.strUserID, -1, client.ServerId, equipData);

                EventLogManager.AddRoleEvent(client, OpTypes.Trace, OpTags.ShenQiZaiZao, LogRecordType.ShenQiZaiZao, artifactDataBasic.NewEquitID, 0, failCount);

                result.State = (int)ArtifactResultType.Fail;
                return result;
            }

            //成功------------------------------------------------------------------------
            //扣除材料
            foreach (var d in artifactDataBasic.NeedMaterial)
            {
                int materialId = d.Key;
                int count = d.Value;

                bool oneUseBind = false;
                bool oneUseTimeLimit = false;
                bool isOk = Global.UseGoodsBindOrNot(client, materialId, count, isUseBind, out oneUseBind, out oneUseTimeLimit) >= 1;
                if (!isOk)
                {
                    result.State = (int)ArtifactResultType.EnoMaterial;
                    return result;
                }

                useBind = useBind || oneUseBind;
                useTimeLimit = useTimeLimit || oneUseTimeLimit;
            }

            //扣除再造点
            GameManager.ClientMgr.ModifyZaiZaoValue(client, -artifactDataBasic.NeedZaiZao, "神器再造", true, true);

            EventLogManager.AddRoleEvent(client, OpTypes.Trace, OpTags.ShenQiZaiZao, LogRecordType.ShenQiZaiZao, artifactDataBasic.NewEquitID, 1, 0);

            #endregion

            #region 再造

            int _Forge_level = equipData.Forge_level;       //强化等级
            int _AppendPropLev = equipData.AppendPropLev;   //追加等级
            int _Lucky = equipData.Lucky;                   //幸运属性
            int _ExcellenceInfo = equipData.ExcellenceInfo; //卓越属性
            List<int> _WashProps = equipData.WashProps;     //培养属性
            int _Binding = equipData.Binding;               //绑定状态
            if (useBind)
                _Binding = 1;

            //扣除原有的装备
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                Global._TCPManager.TcpOutPacketPool, client, equipID, false))
            {
                result.State = (int)ArtifactResultType.EdelEquip;
                return result;
            }

            //给予新的装备
            int nItemDBID = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                artifactDataBasic.NewEquitID, 1,
                equipData.Quality, //
                "",
                _Forge_level,
                _Binding,
                0,
                equipData.Jewellist, //""
                false,
                1,
                /**/"神器再造",
                Global.ConstGoodsEndTime,
                equipData.AddPropIndex,//0, //
                equipData.BornIndex, //
                _Lucky,
                0,
                _ExcellenceInfo,
                _AppendPropLev,
                equipData.ChangeLifeLevForEquip,//0, //
                _WashProps);

            if (nItemDBID < 0)
            {
                result.State = (int)ArtifactResultType.EaddEquip;
                return result;
            }

            #endregion

            // 玩家【用户名字】勇往直前，勇不可挡，通过了万魔塔第XX层！
            string broadcastMsg = StringUtil.substitute(Global.GetLang("玩家【{0}】成功进行了神器再造，获得了{2}阶装备【{1}】！"),
                                                        Global.FormatRoleName(client, client.ClientData.RoleName), artifactDataBasic.ArtifactName, nSuitID + 1);
            //播放用户行为消息
            Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.HintMsg, broadcastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);

            result.State = (int)ArtifactResultType.Success;
            result.EquipDbID = nItemDBID;
            result.Bind = _Binding;
            return result;
        }

        /// <summary>
        /// 神器——套装属性设置
        /// </summary>
        /// <param name="client"></param>
        public static void SetArtifactProp(GameClient client)
        {
            Dictionary<int, List<int>> suitTypeList = new Dictionary<int, List<int>>();

            int addAttack = 0;
            int addDefense = 0;
            int attackMin = 0;
            int attackMax = 0;
            int defenseMin = 0;
            int defenseMax = 0;
            int mAttackMin = 0;
            int mAttackMax = 0;
            int mDefenseMin = 0;
            int mDefenseMax = 0;
            int lifeMax = 0;
            int lifeSteal = 0;

            if (client.ClientData.GoodsDataList == null)
                return;

            lock (client.ClientData.GoodsDataList)
            {
                for (int i = 0; i < client.ClientData.GoodsDataList.Count; i++)
                {
                    GoodsData goodsData = client.ClientData.GoodsDataList[i];
                    
                    #region 验证               
                    if (goodsData.Using <= 0)
                        continue;

                    SystemXmlItem systemGoods = null;
                    if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out systemGoods))
                        continue;

                    //int nSuitID = systemGoods.GetIntValue("SuitID");
                    //if (nSuitID <= ARTIFACT_SUIT)
                      //  continue;

                    //不是装备，是元素之心
                    int categoriy = systemGoods.GetIntValue("Categoriy");
                    bool isElementHrt = ElementhrtsManager.IsElementHrt(categoriy);
                    if (categoriy >= (int)ItemCategories.EquipMax && !isElementHrt)
                        continue;

                    #endregion

                    #region 添加装备
                    ArtifactSuitData suitData = GetArtifactSuitDataByEquipID(goodsData.GoodsID);
                    if (suitData == null)
                        continue;

                    if (suitTypeList.ContainsKey(suitData.SuitID))
                    {
                        bool isAdd = true;
                        List<int> value = suitTypeList[suitData.SuitID];

                        if (!suitData.IsMulti)
                        {           
                            foreach (int id in value)
                            {
                                if (id == goodsData.GoodsID)
                                {
                                    isAdd = false;
                                    break;
                                }
                            }
                        }

                        if (!isAdd)
                            continue;

                        value.Add(goodsData.GoodsID);
                    }
                    else
                    {
                        List<int> value = new List<int>();
                        value.Add(goodsData.GoodsID);

                        suitTypeList.Add(suitData.SuitID, value);
                    }

                    #endregion
                }

                #region 加属性
                foreach (var type in suitTypeList)
                {
                    int count = type.Value.Count;
                    if (count < 2)
                        continue;

                    ArtifactSuitData suitData = GetArtifactSuitDataBySuitID(type.Key);
                    foreach (var attrs in suitData.SuitAttr)
                    {
                        if (count < attrs.Key)
                            continue;

                        foreach (var attr in attrs.Value)
                        {
                            string[] values = attr.Value.Split('-');

                            switch (attr.Key)
                            {
                                case "AddAttack"://攻击力（物理，魔法）、、
                                    addAttack += int.Parse(attr.Value); 
                                    break;
                                case "AddDefense"://防御（物理，魔法）、、
                                    addDefense += int.Parse(attr.Value);
                                    break;
                                case "Attack"://物理攻击
                                    attackMin += int.Parse(values[0]);
                                    attackMax += int.Parse(values[1]);
                                    break;
                                case "Defense"://物理防御
                                    defenseMin += int.Parse(values[0]);
                                    defenseMax += int.Parse(values[1]);
                                    break;
                                case "Mattack"://魔法攻击
                                    mAttackMin += int.Parse(values[0]);
                                    mAttackMax += int.Parse(values[1]);
                                   break;
                                case "Mdefense"://魔法防御
                                    mDefenseMin += int.Parse(values[0]);
                                    mDefenseMax += int.Parse(values[1]);
                                    break;
                                case "MaxLifeV"://生命上限	
                                    lifeMax += int.Parse(attr.Value);
                                    break;
                                case "LifeSteal"://击中恢复
                                    lifeSteal += int.Parse(attr.Value);
                                    break;
                            }
                        }
                    }
                }

                #endregion
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.AddAttack, addAttack);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.AddDefense, addDefense);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.MinAttack, attackMin);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.MaxAttack, attackMax);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.MinDefense, defenseMin);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.MaxDefense, defenseMax);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.MinMAttack, mAttackMin);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.MaxMAttack, mAttackMax);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.MinMDefense, mDefenseMin);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.MaxMDefense, mDefenseMax);                                                                            
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.MaxLifeV, lifeMax);
                client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.Artifact, (int)ExtPropIndexes.LifeSteal, lifeSteal);

            }//end lock

        }
        #endregion

        #region 神器再造GM

        public static void SetArtifactFailCount(GameClient client, int count)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.ArtifactFailCount, count, true);
        }

        #endregion


    }
}
