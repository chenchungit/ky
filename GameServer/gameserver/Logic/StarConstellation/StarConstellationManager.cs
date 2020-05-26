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
using GameServer.Logic.ActivityNew.SevenDay;

namespace GameServer.Logic//.StarConstellation
{
    // 星座管理器 [7/31/2014 LiaoWei]
    public class StarConstellationManager
    {
        /// <summary>
        /// 星座类型信息 key-typeID value-StarConstellationTypeInfo
        /// </summary>
        public Dictionary<int, StarConstellationTypeInfo> m_StarConstellationTypeInfo = new Dictionary<int, StarConstellationTypeInfo>();
    
        /// <summary>
        /// 星座详细信息 key-occupation value- key-type value- key-starid value-StarConstellationDetailInfo
        /// </summary>
        public Dictionary<int, Dictionary<int, Dictionary<int, StarConstellationDetailInfo>>> m_StarConstellationDetailInfo = new Dictionary<int, Dictionary<int, Dictionary<int, StarConstellationDetailInfo>>>();

        /// <summary>
        /// 最大星座ID
        /// </summary>
        public int m_MaxStarSiteID = 0;

        /// <summary>
        /// 最大星位ID
        /// </summary>
        public int m_MaxStarSlotID = 0;
        
        /// <summary>
        /// 星座类型静态数据
        /// </summary>
        public void LoadStarConstellationTypeInfo()
        {
            try
            {
                string fileName = "Config/XingZuo/XingZuoType.xml";

                XElement xmlFile = null;
                xmlFile = Global.GetGameResXml(string.Format(fileName));
                if (null == xmlFile)
                    return;

                IEnumerable<XElement> StarXEle = xmlFile.Elements("XingZuo").Elements();
                foreach (var xmlItem in StarXEle)
                {
                    if (null != xmlItem)
                    {
                        StarConstellationTypeInfo tmpInfo = new StarConstellationTypeInfo();

                        int ID = (int)Global.GetSafeAttributeDouble(xmlItem, "ID");
                        tmpInfo.TypeID = ID;

                        string sLevelInfo = null;
                        sLevelInfo = Global.GetSafeAttributeStr(xmlItem, "KaiQiLevel");

                        string[] sArrayLevelInfo = null;
                        sArrayLevelInfo = sLevelInfo.Split(',');
                        tmpInfo.ChangeLifeLimit = Global.SafeConvertToInt32(sArrayLevelInfo[0]);
                        tmpInfo.LevelLimit = Global.SafeConvertToInt32(sArrayLevelInfo[1]);

                        string strInfos = null;
                        strInfos = Global.GetSafeAttributeStr(xmlItem, "ShuXiangJiaCheng");

                        string[] sArry = null;
                        sArry = strInfos.Split('|');

                        string[] sArryInfo = null;

                        if (sArry != null)
                        {
                            tmpInfo.Propertyinfo = new PropertyInfo();
                            for (int n = 0; n < sArry.Length; ++n)
                            {
                                sArryInfo = null;
                                sArryInfo = sArry[n].Split(',');

                                string strPorpName = null;
                                strPorpName = sArryInfo[0];

                                string strPorpValue = null;
                                strPorpValue = sArryInfo[1];

                                string[] strArrayPorpValue = null;
                                strArrayPorpValue = strPorpValue.Split('-');

                                if (strPorpName == "Defense")
                                {
                                    tmpInfo.Propertyinfo.PropertyID1 = (int)ExtPropIndexes.MinDefense;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue1 = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpInfo.Propertyinfo.PropertyID2 = (int)ExtPropIndexes.MaxDefense;
                                    tmpInfo.Propertyinfo.AddPropertyMaxValue1 = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "Mdefense")
                                {
                                    tmpInfo.Propertyinfo.PropertyID3 = (int)ExtPropIndexes.MinMDefense;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue2 = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpInfo.Propertyinfo.PropertyID4 = (int)ExtPropIndexes.MaxMDefense;
                                    tmpInfo.Propertyinfo.AddPropertyMaxValue2 = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "Attack")
                                {
                                    tmpInfo.Propertyinfo.PropertyID5 = (int)ExtPropIndexes.MinAttack;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue3 = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpInfo.Propertyinfo.PropertyID6 = (int)ExtPropIndexes.MaxAttack;
                                    tmpInfo.Propertyinfo.AddPropertyMaxValue3 = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "Mattack")
                                {
                                    tmpInfo.Propertyinfo.PropertyID7 = (int)ExtPropIndexes.MinMAttack;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue4 = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpInfo.Propertyinfo.PropertyID8 = (int)ExtPropIndexes.MaxMAttack;
                                    tmpInfo.Propertyinfo.AddPropertyMaxValue4 = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "HitV")
                                {
                                    tmpInfo.Propertyinfo.PropertyID9 = (int)ExtPropIndexes.HitV;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue5 = Global.SafeConvertToInt32(strArrayPorpValue[0]);
                                }
                                else if (strPorpName == "Dodge")
                                {
                                    tmpInfo.Propertyinfo.PropertyID10 = (int)ExtPropIndexes.Dodge;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue6 = Global.SafeConvertToInt32(strArrayPorpValue[0]);
                                }
                                else if (strPorpName == "MaxLifeV")
                                {
                                    tmpInfo.Propertyinfo.PropertyID11 = (int)ExtPropIndexes.MaxLifeV;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue7 = Global.SafeConvertToInt32(strArrayPorpValue[0]);
                                }

                            }
                        }

                        strInfos = null;
                        strInfos = Global.GetSafeAttributeStr(xmlItem, "JiaChengBiLie");

                        sArry = null;
                        sArry = strInfos.Split('|');

                        sArryInfo = null;

                        if (sArry != null)
                        {
                            tmpInfo.AddPropStarSiteLimit = new int[sArry.Length];
                            tmpInfo.AddPropModulus= new int[sArry.Length];

                            for (int n = 0; n < sArry.Length; ++n)
                            {
                                sArryInfo = null;
                                sArryInfo = sArry[n].Split(',');

                                tmpInfo.AddPropStarSiteLimit[n] = Global.SafeConvertToInt32(sArryInfo[0]);
                                tmpInfo.AddPropModulus[n] = Global.SafeConvertToInt32(sArryInfo[1]);                                
                            }
                        }

                        m_StarConstellationTypeInfo.Add(ID, tmpInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("load xml file : {0} fail" + ex.ToString(), string.Format("Config/XingZuoType.xml")));
            }
        }

        /// <summary>
        /// 星座详细信息数据
        /// </summary>
        public void LoadStarConstellationDetailInfo()
        {
            for (int i = (int)EOccupationType.EOT_Warrior; i < (int)EOccupationType.EOT_MAX; i++) // 新增魔剑士星座配置 [XSea 2015/4/16]
            {
                XElement xmlFile = null;
                try
                {
                    xmlFile = Global.GetGameResXml(string.Format("Config/XingZuo/XingZuo_{0}.xml", i));
                }
                catch (Exception)
                {
                    throw new Exception(string.Format("启动时加载xml文件: {0} 失败", string.Format("Config/XingZuo/XingZuo_{0}.xml", i)));
                }

                IEnumerable<XElement> StarXmlItems = xmlFile.Elements("XingZuo");

                Dictionary<int, Dictionary<int, StarConstellationDetailInfo>> tmpDicInfo = new Dictionary<int, Dictionary<int, StarConstellationDetailInfo>>();
                foreach (var xmlItem in StarXmlItems)
                {
                    Dictionary<int, StarConstellationDetailInfo> tmpDic = new Dictionary<int, StarConstellationDetailInfo>();

                    int nID = (int)Global.GetSafeAttributeDouble(xmlItem, "ID");
                    
                    IEnumerable<XElement> XingWeiItems = xmlItem.Elements("XingWei");
                    foreach (var XingWeiItem in XingWeiItems)
                    {
                        StarConstellationDetailInfo tmpInfo = new StarConstellationDetailInfo();

                        int ID = (int)Global.GetSafeAttributeDouble(XingWeiItem, "ID");
                        tmpInfo.StarConstellationID = ID;

                        string sLevelInfo = null;
                        sLevelInfo = Global.GetSafeAttributeStr(XingWeiItem, "LevelLimit");

                        string[] sArrayLevelInfo = null;
                        sArrayLevelInfo = sLevelInfo.Split(',');
                        tmpInfo.ChangeLifeLimit = Global.SafeConvertToInt32(sArrayLevelInfo[0]);
                        tmpInfo.LevelLimit = Global.SafeConvertToInt32(sArrayLevelInfo[1]);

                        tmpInfo.SuccessRate = (int)(Global.GetSafeAttributeDouble(XingWeiItem, "Succeed") * 10000);

                        //string sGoodsInfo = null;
                        //sGoodsInfo = Global.GetSafeAttributeStr(XingWeiItem, "NeedGoods");

                        tmpInfo.NeedGoodsID = 0;
                        tmpInfo.NeedGoodsNum = 0;
                        //string[] sArrayGoodInfo = null;
                        //sArrayGoodInfo = sGoodsInfo.Split(',');
                        //if (sArrayGoodInfo != null && sArrayGoodInfo.Length == 2)
                        //{
                        //    tmpInfo.NeedGoodsID = Global.SafeConvertToInt32(sArrayGoodInfo[0]);
                        //    tmpInfo.NeedGoodsNum = Global.SafeConvertToInt32(sArrayGoodInfo[1]);
                        //}

                        tmpInfo.NeedJinBi = (int)Global.GetSafeAttributeDouble(XingWeiItem, "NeedJinBi");

                        tmpInfo.NeedStarSoul = (int)Global.GetSafeAttributeDouble(XingWeiItem, "XingHun");

                        string strShuXingInfos = null;
                        strShuXingInfos = Global.GetSafeAttributeStr(XingWeiItem, "ShuXing");

                        string[] sArrayPropInfo = null;
                        sArrayPropInfo = strShuXingInfos.Split('|');

                        string[] sArryInfo = null;

                        if (sArrayPropInfo != null)
                        {
                            tmpInfo.Propertyinfo = new PropertyInfo();
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
                                    tmpInfo.Propertyinfo.PropertyID1 = (int)ExtPropIndexes.MinDefense;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue1 = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpInfo.Propertyinfo.PropertyID2 = (int)ExtPropIndexes.MaxDefense;
                                    tmpInfo.Propertyinfo.AddPropertyMaxValue1 = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "Mdefense")
                                {
                                    tmpInfo.Propertyinfo.PropertyID3 = (int)ExtPropIndexes.MinMDefense;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue2 = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpInfo.Propertyinfo.PropertyID4 = (int)ExtPropIndexes.MaxMDefense;
                                    tmpInfo.Propertyinfo.AddPropertyMaxValue2 = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "Attack")
                                {
                                    tmpInfo.Propertyinfo.PropertyID5 = (int)ExtPropIndexes.MinAttack;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue3 = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpInfo.Propertyinfo.PropertyID6 = (int)ExtPropIndexes.MaxAttack;
                                    tmpInfo.Propertyinfo.AddPropertyMaxValue3 = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "Mattack")
                                {
                                    tmpInfo.Propertyinfo.PropertyID7 = (int)ExtPropIndexes.MinMAttack;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue4 = Global.SafeConvertToInt32(strArrayPorpValue[0]);

                                    tmpInfo.Propertyinfo.PropertyID8 = (int)ExtPropIndexes.MaxMAttack;
                                    tmpInfo.Propertyinfo.AddPropertyMaxValue4 = Global.SafeConvertToInt32(strArrayPorpValue[1]);
                                }
                                else if (strPorpName == "HitV")
                                {
                                    tmpInfo.Propertyinfo.PropertyID9 = (int)ExtPropIndexes.HitV;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue5 = Global.SafeConvertToInt32(strArrayPorpValue[0]);
                                }
                                else if (strPorpName == "Dodge")
                                {
                                    tmpInfo.Propertyinfo.PropertyID10 = (int)ExtPropIndexes.Dodge;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue6 = Global.SafeConvertToInt32(strArrayPorpValue[0]);
                                }
                                else if (strPorpName == "MaxLifeV")
                                {
                                    tmpInfo.Propertyinfo.PropertyID11 = (int)ExtPropIndexes.MaxLifeV;
                                    tmpInfo.Propertyinfo.AddPropertyMinValue7 = Global.SafeConvertToInt32(strArrayPorpValue[0]);
                                }
                            }
                        }

                        tmpDic.Add(ID, tmpInfo);

                        if (ID > m_MaxStarSlotID)
                            m_MaxStarSlotID = ID;
                    }

                    if (nID > m_MaxStarSiteID)
                        m_MaxStarSiteID = nID;

                    tmpDicInfo.Add(nID, tmpDic);
                }

                m_StarConstellationDetailInfo.Add(i, tmpDicInfo);
            }
        }

        /// <summary>
        /// 找到扩展属性的下标值
        /// </summary>
        public int GetExtendPropIndex(int nValue, StarConstellationTypeInfo starInfo)
        {
            if (nValue > 0)
            {
                for (int i = 0; i < starInfo.AddPropStarSiteLimit.Length; ++i)
                {
                    if (nValue >= starInfo.AddPropStarSiteLimit[i])
                    {
                        if (nValue == starInfo.AddPropStarSiteLimit[i])
                        {
                            return starInfo.AddPropModulus[i];
                        }
                        else
                        {
                            if (nValue < starInfo.AddPropStarSiteLimit[i+1])
                                return starInfo.AddPropModulus[i];
                        }
                    }
                }
            }

            return -1;

        }

        /// <summary>
        /// 初始化玩家的星座加成属性
        /// </summary>
        public void InitPlayerStarConstellationPorperty(GameClient client)
        {
            if (client.ClientData.RoleStarConstellationInfo != null && client.ClientData.RoleStarConstellationInfo.Count > 0)
            {
                int nOccupation = client.ClientData.Occupation;

                Dictionary<int, Dictionary<int, StarConstellationDetailInfo>> dicTmp = null;

                if (!m_StarConstellationDetailInfo.TryGetValue(nOccupation, out dicTmp) || dicTmp == null)
                    return;

                // 清空RoleStarConstellationProp 重新计算之
                client.ClientData.RoleStarConstellationProp.ResetStarConstellationProps();

                foreach (var StarConstellationinfo in client.ClientData.RoleStarConstellationInfo)
                {
                    int nStarSiteID = -1; 
                    nStarSiteID = StarConstellationinfo.Key;
                    
                    int nStarSlotID = -1; 
                    nStarSlotID = StarConstellationinfo.Value;

                    if (nStarSiteID < 0 || nStarSiteID > m_MaxStarSiteID || nStarSlotID < 0 || nStarSlotID > m_MaxStarSlotID)
                        continue;

                    Dictionary<int, StarConstellationDetailInfo> dicTmpInfo = null;

                    if (!dicTmp.TryGetValue(nStarSiteID, out dicTmpInfo) || dicTmpInfo == null)
                        continue;

                    for (int n = 0; n <= nStarSlotID; ++n)
                    {
                        StarConstellationDetailInfo tmpInfo = null;

                        if (!dicTmpInfo.TryGetValue(n, out tmpInfo) || tmpInfo == null)
                            continue;

                        PropertyInfo tmpProp = null;
                        tmpProp = tmpInfo.Propertyinfo;

                        if (tmpProp == null)
                            return;

                        ActivationStarConstellationProp(client, tmpProp);
                    }

                    /*int nCount = 0;

                    if (!client.ClientData.StarConstellationCount.TryGetValue(nStarSiteID, out nCount))
                    {
                        ++nCount;
                        client.ClientData.StarConstellationCount.Add(nStarSiteID, nCount);
                    }
                    else
                    {
                        ++client.ClientData.StarConstellationCount[nStarSiteID];
                    }*/

                    ActivationStarConstellationExtendProp(client, StarConstellationinfo.Key);
                }
            }

            // 通知客户端 星魂值 [8/5/2014 LiaoWei]
            //GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.StarSoulValue, client.ClientData.StarSoul);
            
        }


        /// <summary>
        /// 激活星座信息
        /// </summary>
        public int ActivationStarConstellation(GameClient client, int nStarSiteID)
        {
            if (nStarSiteID < 1 || nStarSiteID > m_MaxStarSiteID)
                return -1;

            if (client.ClientData.RoleStarConstellationInfo == null)
            {
                client.ClientData.RoleStarConstellationInfo = new Dictionary<int, int>();
            }

            int nStarSlot = 0;
            client.ClientData.RoleStarConstellationInfo.TryGetValue(nStarSiteID, out nStarSlot);

            if (nStarSlot >= m_MaxStarSlotID)
                return -1;
    
            ++nStarSlot;

            int nOccupation = client.ClientData.Occupation;

            Dictionary<int, Dictionary<int, StarConstellationDetailInfo>> dicTmp = null;

            if (!m_StarConstellationDetailInfo.TryGetValue(nOccupation, out dicTmp) || dicTmp == null)
                return -2;

            Dictionary<int, StarConstellationDetailInfo> dicTmpInfo = null;

            if (!dicTmp.TryGetValue(nStarSiteID, out dicTmpInfo) || dicTmpInfo == null)
                return -2;

            StarConstellationDetailInfo tmpInfo = null;

            if (!dicTmpInfo.TryGetValue(nStarSlot, out tmpInfo) || tmpInfo == null)
                return -2;

            int nNeeChangeLife = 0; 
            nNeeChangeLife = tmpInfo.ChangeLifeLimit;
            int nNeedLev = tmpInfo.LevelLimit;
            int nReqUnionLevel = Global.GetUnionLevel(nNeeChangeLife, nNeedLev);

            if (Global.GetUnionLevel(client.ClientData.ChangeLifeCount, client.ClientData.Level) < nReqUnionLevel)
                return -3;

            int nGoods = tmpInfo.NeedGoodsID;
            int nNum = tmpInfo.NeedGoodsNum;

            if (nGoods > 0 && nNum > 0)
            {
                GoodsData goods = null;
                goods = Global.GetGoodsByID(client, nGoods);

                if (goods == null || goods.GCount < nNum)
                {
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                            StringUtil.substitute(Global.GetLang("所需物品不足")), GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox);
                
                    return -5;
                }
            }

            int nNeedStarSoul = tmpInfo.NeedStarSoul;
            if (nNeedStarSoul > 0)
            {
                if (nNeedStarSoul > client.ClientData.StarSoul)
                {
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                            StringUtil.substitute(Global.GetLang("星魂不足")), GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox);
                    return -9;
                }                
            }

            int nNeedMoney = tmpInfo.NeedJinBi;
            if (nNeedMoney > 0)
            {
                if (!Global.SubBindTongQianAndTongQian(client, nNeedMoney, "激活星座"))
                {
                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                            StringUtil.substitute(Global.GetLang("金币不足")), GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox);

                    return -10;
                }
            }


            if (nGoods > 0 && nNum > 0)
            {
                bool usedBinding = false;
                bool usedTimeLimited = false;

                if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool,
                                                            Global._TCPManager.TcpOutPacketPool, client, nGoods, nNum, false, out usedBinding, out usedTimeLimited))
                    return -6;
            }

            if (nNeedStarSoul > 0)
            {
                GameManager.ClientMgr.ModifyStarSoulValue(client, -nNeedStarSoul, "激活星座", true, true);
                //client.ClientData.StarSoul -= nNeedStarSoul;

                //if (client.ClientData.StarSoul < 0)
                //    client.ClientData.StarSoul = 0;

                //Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.StarSoul, client.ClientData.StarSoul, true);

                //GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.StarSoulValue, client.ClientData.StarSoul);
            }

            // 概率
            int nRate = 0;
            nRate = Global.GetRandomNumber(1, 10001);

            if (nRate > tmpInfo.SuccessRate)
            {
                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                            StringUtil.substitute(Global.GetLang("激活星位失败")), GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox);
                return -100;
            }

            // 通知DB
            TCPOutPacket tcpOutPacket = null;
            string strDbCmd = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, nStarSiteID, nStarSlot);

            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer2(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                                                                (int)TCPGameServerCmds.CMD_DB_UPDATESTARCONSTELLATION, strDbCmd, out tcpOutPacket, client.ServerId);

            if (TCPProcessCmdResults.RESULT_FAILED == dbRequestResult)
                return -7;
            Global.PushBackTcpOutPacket(tcpOutPacket);
            PropertyInfo tmpProp = null;
            tmpProp = tmpInfo.Propertyinfo;

            if (tmpProp == null)
                return -8;

            client.ClientData.RoleStarConstellationInfo[nStarSiteID] = nStarSlot;

            ActivationStarConstellationProp(client, tmpProp);

            /*int nCount = 0;
            
            if (!client.ClientData.StarConstellationCount.TryGetValue(nStarSiteID, out nCount))
            {
                ++nCount;
                client.ClientData.StarConstellationCount.Add(nStarSiteID, nCount);
            }
            else
            {
                ++client.ClientData.StarConstellationCount[nStarSiteID];
            }*/

            if (0 == nStarSlot % 12)
            {
                ActivationStarConstellationExtendProp(client, nStarSiteID);

                // 星座属性 [8/4/2014 LiaoWei]
                GameManager.StarConstellationMgr.InitPlayerStarConstellationPorperty(client);                
            }

            // 七日活动
            GlobalEventSource.getInstance().fireEvent(SevenDayGoalEvPool.Alloc(client, ESevenDayGoalFuncType.ActiveXingZuo));

            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            client.ClientData.LifeV = (int)RoleAlgorithm.GetMaxLifeV(client);
            client.ClientData.MagicV = (int)RoleAlgorithm.GetMaxMagicV(client);
            GameManager.ClientMgr.NotifySelfLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            return 1;
        }

        /// <summary>
        /// 处理星座额外属性
        /// </summary>
        public void ActivationStarConstellationExtendProp(GameClient client, int nSiteID)
        {
            if (client == null || client.ClientData.RoleStarConstellationInfo == null)
                return;

            int nStarSlot = 0;
            if (!client.ClientData.RoleStarConstellationInfo.TryGetValue(nSiteID, out nStarSlot))
                return;

            //if (nStarSlot < m_MaxStarSlotID)
            //    return;

            StarConstellationTypeInfo scTypeInfo = null;

            if (m_StarConstellationTypeInfo.TryGetValue(nSiteID, out scTypeInfo) && scTypeInfo != null)
            {
                //bool bCanAddProp = false;
                int nModulus = 1;

                if (nStarSlot > 0)
                {
                    nModulus = GetExtendPropIndex(nStarSlot, scTypeInfo);
                }

                //if (nStarSlot >= m_MaxStarSlotID)
                if (nModulus > 0)
                {
                    PropertyInfo tmpProp = null;
                    tmpProp = scTypeInfo.Propertyinfo;

                    if (tmpProp == null)
                        return;

                    ActivationStarConstellationProp(client, tmpProp, nModulus);
                }
            }
        }
        
        /// <summary>
        /// 处理星座属性
        /// </summary>
        public void ActivationStarConstellationProp(GameClient client, PropertyInfo tmpProp, int nModulus = 1)
        {
            if (tmpProp.PropertyID1 >= 0 && tmpProp.AddPropertyMinValue1 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID1] += tmpProp.AddPropertyMinValue1 * nModulus;
            }

            if (tmpProp.PropertyID2 >= 0 && tmpProp.AddPropertyMaxValue1 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID2] += tmpProp.AddPropertyMaxValue1 * nModulus;
            }

            if (tmpProp.PropertyID3 >= 0 && tmpProp.AddPropertyMinValue2 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID3] += tmpProp.AddPropertyMinValue2 * nModulus;
            }

            if (tmpProp.PropertyID4 >= 0 && tmpProp.AddPropertyMaxValue2 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID4] += tmpProp.AddPropertyMaxValue2 * nModulus;
            }

            if (tmpProp.PropertyID5 >= 0 && tmpProp.AddPropertyMinValue3 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID5] += tmpProp.AddPropertyMinValue3 * nModulus;
            }

            if (tmpProp.PropertyID6 >= 0 && tmpProp.AddPropertyMaxValue3 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID6] += tmpProp.AddPropertyMaxValue3 * nModulus;
            }

            if (tmpProp.PropertyID7 >= 0 && tmpProp.AddPropertyMinValue4 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID7] += tmpProp.AddPropertyMinValue4 * nModulus;
            }

            if (tmpProp.PropertyID8 >= 0 && tmpProp.AddPropertyMaxValue4 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID8] += tmpProp.AddPropertyMaxValue4 * nModulus;
            }

            if (tmpProp.PropertyID9 >= 0 && tmpProp.AddPropertyMinValue5 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID9] += tmpProp.AddPropertyMinValue5 * nModulus;
            }

            if (tmpProp.PropertyID10 >= 0 && tmpProp.AddPropertyMinValue6 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID10] += tmpProp.AddPropertyMinValue6 * nModulus;
            }

            if (tmpProp.PropertyID11 >= 0 && tmpProp.AddPropertyMinValue7 > 0)
            {
                client.ClientData.RoleStarConstellationProp.StarConstellationSecondProps[tmpProp.PropertyID11] += tmpProp.AddPropertyMinValue7 * nModulus;
            }
        }

    }
}
