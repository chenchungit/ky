using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Xml.Linq;
using Server.Data;
using System.Windows;
using Server.Tools;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using Server.Protocol;
using System.Threading;

namespace GameServer.Logic//.NewChangeLife
{
    // 转生管理器 [8/25/2014 LiaoWei]
    public class ChangeLifeManager
    {
        /// <summary>
        /// 转生信息 
        /// </summary>
        /// key-OccupationID , value-key changelife value-ChangeLifeInfo
        public Dictionary<int, Dictionary<int, ChangeLifeDataInfo>> m_ChangeLifeInfoList = new Dictionary<int, Dictionary<int, ChangeLifeDataInfo>>();

        /// <summary>
        /// 最大转生数
        /// </summary>
        public int m_MaxChangeLifeCount = 0;        

        /// <summary>
        /// 转生信息表
        /// </summary>
        public void LoadRoleZhuanShengInfo()
        {
            for (int i = (int)EOccupationType.EOT_Warrior; i < (int)EOccupationType.EOT_MAX; i++) // 新增魔剑士转生配置 [XSea 2015/4/16]
            {
                XElement xmlFile = null;
                try
                {
                    xmlFile = Global.GetGameResXml(string.Format("Config/Roles/ZhuanSheng_{0}.xml", i));
                }
                catch (Exception)
                {
                    throw new Exception(string.Format("启动时加载xml文件: {0} 失败", string.Format("Config/Roles/ZhuanSheng_{0}.xml", i)));
                }

                IEnumerable<XElement> ChgOccpXEle = xmlFile.Elements("ZhuanShengs").Elements();
                Dictionary<int, ChangeLifeDataInfo> tmpDic = new Dictionary<int, ChangeLifeDataInfo>();

                foreach (var xmlItem in ChgOccpXEle)
                {
                    ChangeLifeDataInfo tmpChgLifeInfo = new ChangeLifeDataInfo();
                    
                    int nLifeID = 0;
                    
                    if (null != xmlItem)
                    {
                        nLifeID = (int)Global.GetSafeAttributeLong(xmlItem, "ChangeLifeID");

                        tmpChgLifeInfo.ChangeLifeID = (int)Global.GetSafeAttributeLong(xmlItem, "ChangeLifeID");
                        tmpChgLifeInfo.NeedLevel = (int)Global.GetSafeAttributeLong(xmlItem, "Level");
                        tmpChgLifeInfo.NeedMoney = (int)Global.GetSafeAttributeLong(xmlItem, "NeedJinBi");
                        tmpChgLifeInfo.NeedMoJing = (int)Global.GetSafeAttributeLong(xmlItem, "NeedMoJing");
                        tmpChgLifeInfo.ExpProportion = Global.GetSafeAttributeLong(xmlItem, "ExpProportion");

                        string sGoodsID = Global.GetSafeAttributeStr(xmlItem, "NeedGoods");
                        if (string.IsNullOrEmpty(sGoodsID))
                            LogManager.WriteLog(LogTypes.Warning, string.Format("转生文件NeedGoods为空"));
                        else
                        {
                            string[] fields = sGoodsID.Split('|');
                            if (fields.Length <= 0)
                                LogManager.WriteLog(LogTypes.Warning, string.Format("转生文件NeedGoods为空"));
                            else
                                tmpChgLifeInfo.NeedGoodsDataList = Global.LoadChangeOccupationNeedGoodsInfo(sGoodsID, "转生文件"); //将物品字符串列表解析成物品数据列表
                        }

                        string strShuXingInfos = null;
                        strShuXingInfos = Global.GetSafeAttributeStr(xmlItem, "AwardShuXing");

                        string[] sArrayPropInfo = null;
                        sArrayPropInfo = strShuXingInfos.Split('|');

                        string[] sArryInfo = null;

                        if (sArrayPropInfo != null)
                        {
                            tmpChgLifeInfo.Propertyinfo = new ChangeLifePropertyInfo();
                            for (int n = 0; n < sArrayPropInfo.Length; ++n)
                            {
                                sArryInfo = null;
                                sArryInfo = sArrayPropInfo[n].Split(',');

                                string strPorpName = null;
                                strPorpName = sArryInfo[0];

                                string strPorpValue = null;
                                strPorpValue = sArryInfo[1];

                                string[] strArrayPorpValue = null;
                                strArrayPorpValue = strPorpValue.Split('-');

                                if (strPorpName == "Defense")
                                {
                                    tmpChgLifeInfo.Propertyinfo.PhyDefenseMin = (int)ExtPropIndexes.MinDefense;
                                    tmpChgLifeInfo.Propertyinfo.AddPhyDefenseMinValue = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpChgLifeInfo.Propertyinfo.PhyDefenseMax = (int)ExtPropIndexes.MaxDefense;
                                    tmpChgLifeInfo.Propertyinfo.AddPhyDefenseMaxValue = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "Mdefense")
                                {
                                    tmpChgLifeInfo.Propertyinfo.MagDefenseMin = (int)ExtPropIndexes.MinMDefense;
                                    tmpChgLifeInfo.Propertyinfo.AddMagDefenseMinValue = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpChgLifeInfo.Propertyinfo.MagDefenseMax = (int)ExtPropIndexes.MaxMDefense;
                                    tmpChgLifeInfo.Propertyinfo.AddMagDefenseMaxValue = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "Attack")
                                {
                                    tmpChgLifeInfo.Propertyinfo.PhyAttackMin = (int)ExtPropIndexes.MinAttack;
                                    tmpChgLifeInfo.Propertyinfo.AddPhyAttackMinValue = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpChgLifeInfo.Propertyinfo.PhyAttackMax = (int)ExtPropIndexes.MaxAttack;
                                    tmpChgLifeInfo.Propertyinfo.AddPhyAttackMaxValue = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "Mattack")
                                {
                                    tmpChgLifeInfo.Propertyinfo.MagAttackMin = (int)ExtPropIndexes.MinMAttack;
                                    tmpChgLifeInfo.Propertyinfo.AddMagAttackMinValue = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpChgLifeInfo.Propertyinfo.MagAttackMax = (int)ExtPropIndexes.MaxMAttack;
                                    tmpChgLifeInfo.Propertyinfo.AddMagAttackMaxValue = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "HitV")
                                {
                                    tmpChgLifeInfo.Propertyinfo.HitProp = (int)ExtPropIndexes.HitV;
                                    tmpChgLifeInfo.Propertyinfo.AddHitPropValue = Global.SafeConvertToInt32(strArrayPorpValue[0]);
                                }
                                else if (strPorpName == "Dodge")
                                {
                                    tmpChgLifeInfo.Propertyinfo.DodgeProp = (int)ExtPropIndexes.Dodge;
                                    tmpChgLifeInfo.Propertyinfo.AddDodgePropValue = Global.SafeConvertToInt32(strArrayPorpValue[0]);
                                }
                                else if (strPorpName == "MaxLifeV")
                                {
                                    tmpChgLifeInfo.Propertyinfo.MaxLifeProp = (int)ExtPropIndexes.MaxLifeV;
                                    tmpChgLifeInfo.Propertyinfo.AddMaxLifePropValue = Global.SafeConvertToInt32(strArrayPorpValue[0]);
                                }
                                else if (strPorpName == "AddAttack") // 新增 物攻魔攻都加 [XSea 2015/6/5]
                                {
                                    // 物攻最小值
                                    tmpChgLifeInfo.Propertyinfo.PhyAttackMin = (int)ExtPropIndexes.MinAttack;
                                    tmpChgLifeInfo.Propertyinfo.AddPhyAttackMinValue = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    // 物攻最大值
                                    tmpChgLifeInfo.Propertyinfo.PhyAttackMax = (int)ExtPropIndexes.MaxAttack;
                                    tmpChgLifeInfo.Propertyinfo.AddPhyAttackMaxValue = Global.SafeConvertToInt32(strArrayPorpValue[1]);

                                    // 魔攻最小值
                                    tmpChgLifeInfo.Propertyinfo.MagAttackMin = (int)ExtPropIndexes.MinMAttack;
                                    tmpChgLifeInfo.Propertyinfo.AddMagAttackMinValue = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    // 魔攻最大值
                                    tmpChgLifeInfo.Propertyinfo.MagAttackMax = (int)ExtPropIndexes.MaxMAttack;
                                    tmpChgLifeInfo.Propertyinfo.AddMagAttackMaxValue = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                            }
                        }

                        string sGoodsID1 = Global.GetSafeAttributeStr(xmlItem, "AwardGoods");
                        if (string.IsNullOrEmpty(sGoodsID1))
                            LogManager.WriteLog(LogTypes.Warning, string.Format("转生文件NeedGoods为空"));
                        else
                        {
                            string[] fields1 = sGoodsID1.Split('|');
                            if (fields1.Length <= 0)
                                LogManager.WriteLog(LogTypes.Warning, string.Format("转生文件NeedGoods为空"));
                            else
                                tmpChgLifeInfo.AwardGoodsDataList = Global.LoadChangeOccupationNeedGoodsInfo(sGoodsID1, "转生文件"); //将物品字符串列表解析成物品数据列表
                        }
                    }

                    if (nLifeID > m_MaxChangeLifeCount)
                        m_MaxChangeLifeCount = nLifeID;
                    if (nLifeID > 1)
                    {
                        tmpChgLifeInfo.Propertyinfo.AddFrom(tmpDic[nLifeID - 1].Propertyinfo);
                    }
                    tmpDic.Add(nLifeID, tmpChgLifeInfo);
                }

                m_ChangeLifeInfoList.Add(i, tmpDic);
            }
        }

        /// <summary>
        /// 取得转生信息接口
        /// </summary>
        public ChangeLifeDataInfo GetChangeLifeDataInfo(GameClient Client, int nChangeLife = 0)
        {
            if (nChangeLife == 0)
            {
                nChangeLife = Client.ClientData.ChangeLifeCount;
            }

            Dictionary<int, ChangeLifeDataInfo> dicTmp = new Dictionary<int, ChangeLifeDataInfo>();

            if (!GameManager.ChangeLifeMgr.m_ChangeLifeInfoList.TryGetValue(Client.ClientData.Occupation, out dicTmp))
                return null;

            ChangeLifeDataInfo infoTmp = new ChangeLifeDataInfo();

            if (!dicTmp.TryGetValue(nChangeLife, out infoTmp))
                return null;

            return infoTmp;

        }

        /// <summary>
        /// 初始化玩家的转生加成属性
        /// </summary>
        public void InitPlayerChangeLifePorperty(GameClient client)
        {
            if (client.ClientData.ChangeLifeCount > 0)
            {
                int nOccupation = client.ClientData.Occupation;

                Dictionary<int, ChangeLifeDataInfo> dicTmp = null;

                if (!m_ChangeLifeInfoList.TryGetValue(nOccupation, out dicTmp) || dicTmp == null)
                    return;

                ChangeLifeDataInfo dataTmp = new ChangeLifeDataInfo();

                if (!dicTmp.TryGetValue(client.ClientData.ChangeLifeCount, out dataTmp) || dataTmp == null)
                    return;

                ChangeLifePropertyInfo tmpProp = null;
                tmpProp = dataTmp.Propertyinfo;

                if (tmpProp == null)
                    return;

                ActivationChangeLifeProp(client, tmpProp);
                
            }
        }

        /// <summary>
        /// 处理玩家转生属性
        /// </summary>
        public void ProcessRoleChangeLifeProp(GameClient client)
        {
            InitPlayerChangeLifePorperty(client);

            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

        }

        /// <summary>
        /// 处理转生属性
        /// </summary>
        public void ActivationChangeLifeProp(GameClient client, ChangeLifePropertyInfo tmpProp)
        {
            client.ClientData.RoleChangeLifeProp.ResetChangeLifeProps();

            if (tmpProp.PhyAttackMin >= 0 && tmpProp.AddPhyAttackMinValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.PhyAttackMin] += tmpProp.AddPhyAttackMinValue;
            }

            if (tmpProp.PhyAttackMax >= 0 && tmpProp.AddPhyAttackMaxValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.PhyAttackMax] += tmpProp.AddPhyAttackMaxValue;
            }

            if (tmpProp.MagAttackMin >= 0 && tmpProp.AddMagAttackMinValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.MagAttackMin] += tmpProp.AddMagAttackMinValue;
            }

            if (tmpProp.MagAttackMax >= 0 && tmpProp.AddMagAttackMaxValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.MagAttackMax] += tmpProp.AddMagAttackMaxValue;
            }

            if (tmpProp.PhyDefenseMin >= 0 && tmpProp.AddPhyDefenseMinValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.PhyDefenseMin] += tmpProp.AddPhyDefenseMinValue;
            }

            if (tmpProp.PhyDefenseMax >= 0 && tmpProp.AddPhyDefenseMaxValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.PhyDefenseMax] += tmpProp.AddPhyDefenseMaxValue;
            }

            if (tmpProp.MagDefenseMin >= 0 && tmpProp.AddMagDefenseMinValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.MagDefenseMin] += tmpProp.AddMagDefenseMinValue;
            }

            if (tmpProp.MagDefenseMax >= 0 && tmpProp.AddMagDefenseMaxValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.MagDefenseMax] += tmpProp.AddMagDefenseMaxValue;
            }

            if (tmpProp.HitProp >= 0 && tmpProp.AddHitPropValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.HitProp] += tmpProp.AddHitPropValue;
            }

            if (tmpProp.DodgeProp >= 0 && tmpProp.AddDodgePropValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.DodgeProp] += tmpProp.AddDodgePropValue;
            }

            if (tmpProp.MaxLifeProp >= 0 && tmpProp.AddMaxLifePropValue > 0)
            {
                client.ClientData.RoleChangeLifeProp.ChangeLifeSecondProps[tmpProp.MaxLifeProp] += tmpProp.AddMaxLifePropValue;
            }
            
        }



    }

}
