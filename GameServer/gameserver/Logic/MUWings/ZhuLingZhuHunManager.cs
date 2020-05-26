using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Server.Tools;
using Server.TCP;
using Server.Protocol;
using GameServer.Server;

namespace GameServer.Logic.MUWings
{
    public enum ZhuLingZhuHunError
    {
        Success = 0,          			// 成功
        ZhuLingNotOpen,				    // 注灵系统未开放
        ZhuHunNotOpen,				    // 注魂系统未开放
        ZhuLingFull,			   		// 注灵已满
        ZhuHunFull,					    // 注魂已满
        ZhuLingMaterialNotEnough,		// 注灵所需材料不足
        ZhuLingJinBiNotEnough,		    // 注灵所需金币不足
        ZhuHunMaterialNotEnough,		// 注魂所需材料不足
        ZhuHunJinBiNotEnough,	    	// 注魂所需金币不足
        ErrorConfig,        			// 配置错误
        ErrorParams,        			// 传来的参数错误
        ZuanShiNotEnough,    	    	// 钻石不足
        DBSERVERERROR,     		        // 与dbserver通信失败
    } 

    public class ZhuLingZhuHunEffect
    {
        public int Occupation;
        //以下为注灵加成
        public int MaxAttackV;
        public int MaxMAttackV;
        public int MaxDefenseV;
        public int MaxMDefenseV;
        public int LifeV;
        public int HitV;
        public int DodgeV;
        //以下为注魂加成
        public double AllAttribute;
    }

    public class ZhuLingZhuHunLimit
    {
        public int SuitID;
        public int ZhuLingLimit;
        public int ZhuHunLimit;
    }

    public class ZhuLingZhuHunManager
    {
        private static int ZhuLingCostGoodsID = 0;
        private static int ZhuLingCostGoodsNum = 0;
        private static int ZhuLingCostJinBi = 0;
        private static int ZhuHunCostGoodsID = 0;
        private static int ZhuHunCostGoodsNum = 0;
        private static int ZhuHunCostJinBi = 0;
        private static List<ZhuLingZhuHunLimit> Limit = new List<ZhuLingZhuHunLimit>();
        private static List<ZhuLingZhuHunEffect> Effect = new List<ZhuLingZhuHunEffect>();
        
        private ZhuLingZhuHunManager()
        {
        }

        public static void LoadConfig()
        {
            XElement xml = null;
            string fileName = null;

            #region 加载注灵注魂消耗 ZhuLingType.xml

            fileName = "Config/ZhuLingType.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
            xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (xml == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", fileName));
            }
            else
            {
                XElement xml1 = xml.Element("Types");
                if (xml1 == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", fileName));
                }
                else
                {
                    IEnumerable<XElement> xmlItems = xml1.Elements();
                    foreach (XElement xmlItem in xmlItems)
                    {
                        int id = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "ID"));
                        string strGoods = Global.GetSafeAttributeStr(xmlItem, "GoodsID");
                        int bindJinBi = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "CostBandJinBi"));
                        string[] goods = strGoods.Split(',');
                        if (goods.Length != 2)
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!! ID={1} 消耗物品配置错误", fileName, id));
                            continue;
                        }
                        int goodsID = Convert.ToInt32(goods[0]);
                        int goodsNum = Convert.ToInt32(goods[1]);

                        if (id == 1)
                        {
                            ZhuLingZhuHunManager.ZhuLingCostGoodsID = goodsID;
                            ZhuLingZhuHunManager.ZhuLingCostGoodsNum = goodsNum;
                            ZhuLingZhuHunManager.ZhuLingCostJinBi = bindJinBi;
                        }
                        else if (id == 2)
                        {
                            ZhuLingZhuHunManager.ZhuHunCostGoodsID = goodsID;
                            ZhuLingZhuHunManager.ZhuHunCostGoodsNum = goodsNum;
                            ZhuLingZhuHunManager.ZhuHunCostJinBi = bindJinBi;
                        }
                    }
                }
            }

            #endregion

            #region 加载注灵注魂限制 MaxWinZhuLing.xml

            xml = null;
            fileName = "Config/MaxWinZhuLing.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
            xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (xml == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", fileName));
            }
            else
            {
                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach(XElement xmlItem in xmlItems)
                {
                    ZhuLingZhuHunLimit l = new ZhuLingZhuHunLimit();
                    l.SuitID = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "SuitID"));
                    l.ZhuLingLimit = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "PlainZhuLing"));
                    l.ZhuHunLimit = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "SeniorZhuLing"));
                    ZhuLingZhuHunManager.Limit.Add(l);
                }
            }
            #endregion

            #region 加载注灵注魂属性加成 WinZhuLing.xml

            xml = null;
            fileName = "Config/WinZhuLing.xml";
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName));
            xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName));
            if (xml == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", fileName));
            }
            else
            {
                for (int i = (int)EOccupationType.EOT_Warrior; i < (int)EOccupationType.EOT_MAX; ++i)
                {
                    ZhuLingZhuHunManager.Effect.Add(new ZhuLingZhuHunEffect());
                }

                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (XElement xmlItem in xmlItems)
                {
                    int type = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "TypeID")); //1=注灵 2=注魂
                    int occupation = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "Occupation")); //0=战士 1=法师 2=弓箭手 3=魔剑士
                    if (occupation < 0 || occupation >= ZhuLingZhuHunManager.Effect.Count())
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!! 职业配置有问题", fileName));
                        continue;
                    }
                    ZhuLingZhuHunManager.Effect[occupation].Occupation = occupation;
                    if (type == 1)
                    {
                        ZhuLingZhuHunManager.Effect[occupation].MaxAttackV = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "MaxAttackV"));
                        ZhuLingZhuHunManager.Effect[occupation].MaxMAttackV = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "MaxMAttackV"));
                        ZhuLingZhuHunManager.Effect[occupation].MaxDefenseV = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "MaxDefenseV"));
                        ZhuLingZhuHunManager.Effect[occupation].MaxMDefenseV = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "MaxMDefenseV"));
                        ZhuLingZhuHunManager.Effect[occupation].LifeV = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "LifeV"));
                        ZhuLingZhuHunManager.Effect[occupation].HitV = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "HitV"));
                        ZhuLingZhuHunManager.Effect[occupation].DodgeV = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "DodgeV"));
                    }
                    else if (type == 2)
                    {
                        ZhuLingZhuHunManager.Effect[occupation].AllAttribute = Global.GetSafeAttributeDouble(xmlItem, "AllAttribute");
                    }
                }
            }
            #endregion
        }

        #region 注灵

        public static ZhuLingZhuHunError ReqZhuLing(GameClient client)
        {
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.WingZhuLing))
                return ZhuLingZhuHunError.ZhuLingNotOpen;

            ZhuLingZhuHunLimit l = ZhuLingZhuHunManager.GetLimit(client.ClientData.MyWingData.WingID);
            if (l == null) return ZhuLingZhuHunError.ErrorConfig;

            if (client.ClientData.MyWingData.ZhuLingNum >= l.ZhuLingLimit)
                return ZhuLingZhuHunError.ZhuLingFull;

            if (Global.GetTotalGoodsCountByID(client, ZhuLingZhuHunManager.ZhuLingCostGoodsID) < ZhuLingZhuHunManager.ZhuLingCostGoodsNum)
                return ZhuLingZhuHunError.ZhuLingMaterialNotEnough;

            if (Global.GetTotalBindTongQianAndTongQianVal(client) < ZhuLingZhuHunManager.ZhuLingCostJinBi)
                return ZhuLingZhuHunError.ZhuLingJinBiNotEnough;

            if (!Global.SubBindTongQianAndTongQian(client, ZhuLingZhuHunManager.ZhuLingCostJinBi, "注灵消耗金币"))
                return ZhuLingZhuHunError.DBSERVERERROR;

            bool bUsedBinding = true;
            bool bUsedTimeLimited = false;
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client,
                    ZhuLingZhuHunManager.ZhuLingCostGoodsID, ZhuLingZhuHunManager.ZhuLingCostGoodsNum, false, out bUsedBinding, out bUsedTimeLimited))
                return ZhuLingZhuHunError.DBSERVERERROR;

            int iRet = MUWingsManager.WingUpDBCommand(client, client.ClientData.MyWingData.DbID, client.ClientData.MyWingData.WingID, 
                client.ClientData.MyWingData.JinJieFailedNum, client.ClientData.MyWingData.ForgeLevel, 
                client.ClientData.MyWingData.StarExp, client.ClientData.MyWingData.ZhuLingNum + 1, client.ClientData.MyWingData.ZhuHunNum);

            if (iRet < 0) return ZhuLingZhuHunError.DBSERVERERROR;

            client.ClientData.MyWingData.ZhuLingNum++;
            ZhuLingZhuHunManager.UpdateZhuLingZhuHunProps(client);
            // 通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            return ZhuLingZhuHunError.Success;
        }

        public static TCPProcessCmdResults ProcessReqZhuLing(
           TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool,
           TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                if (1 != fields.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                ZhuLingZhuHunError e = ZhuLingZhuHunManager.ReqZhuLing(client);
                string strcmd = string.Format("{0}:{1}:{2}", roleID, (int)e, client.ClientData.MyWingData.ZhuLingNum);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessReqZhuLing", false);
            }

            return TCPProcessCmdResults.RESULT_DATA;
        }

        #endregion

        #region 注魂
        public static ZhuLingZhuHunError ReqZhuHun(GameClient client)
        {
            if (!GlobalNew.IsGongNengOpened(client, GongNengIDs.WingZhuHun))
                return ZhuLingZhuHunError.ZhuHunNotOpen;

            ZhuLingZhuHunLimit l = ZhuLingZhuHunManager.GetLimit(client.ClientData.MyWingData.WingID);
            if (l == null) return ZhuLingZhuHunError.ErrorConfig;

            if (client.ClientData.MyWingData.ZhuHunNum >= l.ZhuHunLimit)
                return ZhuLingZhuHunError.ZhuHunFull;

            if (Global.GetTotalGoodsCountByID(client, ZhuLingZhuHunManager.ZhuHunCostGoodsID) < ZhuLingZhuHunManager.ZhuHunCostGoodsNum)
                return ZhuLingZhuHunError.ZhuHunMaterialNotEnough;

            if (Global.GetTotalBindTongQianAndTongQianVal(client) < ZhuLingZhuHunManager.ZhuHunCostJinBi)
                return ZhuLingZhuHunError.ZhuHunJinBiNotEnough;

            if (!Global.SubBindTongQianAndTongQian(client, ZhuLingZhuHunManager.ZhuHunCostJinBi, "注魂消耗"))
                return ZhuLingZhuHunError.DBSERVERERROR;

            bool bUsedBinding = true;
            bool bUsedTimeLimited = false;
            if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener,
                    Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client,
                    ZhuLingZhuHunManager.ZhuHunCostGoodsID, ZhuLingZhuHunManager.ZhuHunCostGoodsNum, false, out bUsedBinding, out bUsedTimeLimited))
                return ZhuLingZhuHunError.DBSERVERERROR;

            int iRet = MUWingsManager.WingUpDBCommand(client, client.ClientData.MyWingData.DbID, client.ClientData.MyWingData.WingID,
                client.ClientData.MyWingData.JinJieFailedNum, client.ClientData.MyWingData.ForgeLevel,
                client.ClientData.MyWingData.StarExp, client.ClientData.MyWingData.ZhuLingNum, client.ClientData.MyWingData.ZhuHunNum + 1);

            if (iRet < 0) return ZhuLingZhuHunError.DBSERVERERROR;

            client.ClientData.MyWingData.ZhuHunNum++;
            ZhuLingZhuHunManager.UpdateZhuLingZhuHunProps(client);
            // 通知客户端属性变化
            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            // 总生命值和魔法值变化通知(同一个地图才需要通知)
            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

            return ZhuLingZhuHunError.Success;
        }

        public static TCPProcessCmdResults ProcessReqZhuHun(
          TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool,
          TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                if (1 != fields.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                ZhuLingZhuHunError e = ZhuLingZhuHunManager.ReqZhuHun(client);
                string strcmd = string.Format("{0}:{1}:{2}", roleID, (int)e, client.ClientData.MyWingData.ZhuHunNum);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessReqZhuHun", false);
            }

            return TCPProcessCmdResults.RESULT_DATA;
        }

        #endregion

        public static void UpdateZhuLingZhuHunProps(GameClient client)
        {
            if (null == client.ClientData.MyWingData) return;
            if (client.ClientData.MyWingData.WingID <= 0) return;

            ZhuLingZhuHunEffect e = ZhuLingZhuHunManager.GetEffect(Global.CalcOriginalOccupationID(client));
            if (e == null) return;

            double MaxAttackV = 0.0;
            double MinAttackV = 0.0;
            double MaxMAttackV = 0.0;
            double MinMAttackV = 0.0;
            double MaxDefenseV = 0.0;
            double MinDefenseV = 0.0;
            double MaxMDefenseV = 0.0;
            double MinMDefenseV = 0.0;
            double LifeV = 0.0;
            double HitV = 0.0;
            double DodgeV = 0.0;
            double AllAttribute = 0.0;

            if (client.ClientData.MyWingData.Using == 1)
            {
                MaxAttackV = e.MaxAttackV * client.ClientData.MyWingData.ZhuLingNum;
                MaxMAttackV = e.MaxMAttackV * client.ClientData.MyWingData.ZhuLingNum;
                MaxDefenseV = e.MaxDefenseV * client.ClientData.MyWingData.ZhuLingNum;
                MaxMDefenseV = e.MaxMDefenseV * client.ClientData.MyWingData.ZhuLingNum;
                LifeV = e.LifeV * client.ClientData.MyWingData.ZhuLingNum;
                HitV = e.HitV * client.ClientData.MyWingData.ZhuLingNum;
                DodgeV = e.DodgeV * client.ClientData.MyWingData.ZhuLingNum;
                AllAttribute = e.AllAttribute;

                SystemXmlItem baseXmlNodeSuit = WingPropsCacheManager.GetWingPropsCacheItem(Global.CalcOriginalOccupationID(client), client.ClientData.MyWingData.WingID);
                SystemXmlItem baseXmlNodeStar = WingStarCacheManager.GetWingStarCacheItem(Global.CalcOriginalOccupationID(client), client.ClientData.MyWingData.WingID, client.ClientData.MyWingData.ForgeLevel);
                if (baseXmlNodeSuit == null) baseXmlNodeSuit = new SystemXmlItem();
                if (baseXmlNodeStar == null) baseXmlNodeStar = new SystemXmlItem();

                // 注魂影响翅膀基础属性的百分比
                MaxAttackV += (baseXmlNodeSuit.GetDoubleValue("MaxAttackV") + baseXmlNodeStar.GetDoubleValue("MaxAttackV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                MinAttackV += (baseXmlNodeSuit.GetDoubleValue("MinAttackV") + baseXmlNodeStar.GetDoubleValue("MinAttackV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                MaxMAttackV += (baseXmlNodeSuit.GetDoubleValue("MaxMAttackV") + baseXmlNodeStar.GetDoubleValue("MaxMAttackV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                MinMAttackV += (baseXmlNodeSuit.GetDoubleValue("MinMAttackV") + baseXmlNodeStar.GetDoubleValue("MinMAttackV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                MaxDefenseV += (baseXmlNodeSuit.GetDoubleValue("MaxDefenseV") + baseXmlNodeStar.GetDoubleValue("MaxDefenseV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                MinDefenseV += (baseXmlNodeSuit.GetDoubleValue("MinDefenseV") + baseXmlNodeStar.GetDoubleValue("MinDefenseV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                MaxMDefenseV += (baseXmlNodeSuit.GetDoubleValue("MaxMDefenseV") + baseXmlNodeStar.GetDoubleValue("MaxMDefenseV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                MinMDefenseV += (baseXmlNodeSuit.GetDoubleValue("MinMDefenseV") + baseXmlNodeStar.GetDoubleValue("MinMDefenseV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                LifeV += (baseXmlNodeSuit.GetDoubleValue("MaxLifeV") + baseXmlNodeStar.GetDoubleValue("MaxLifeV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                HitV += (baseXmlNodeSuit.GetDoubleValue("HitV") + baseXmlNodeStar.GetDoubleValue("HitV")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
                DodgeV += (baseXmlNodeSuit.GetDoubleValue("Dodge") + baseXmlNodeStar.GetDoubleValue("Dodge")) * (AllAttribute * client.ClientData.MyWingData.ZhuHunNum);
            }

            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.MaxAttack, MaxAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.MinAttack, MinAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.MaxMAttack, MaxMAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.MinMAttack, MinMAttackV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.MaxDefense, MaxDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.MinDefense, MinDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.MaxMDefense, MaxMDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.MinMDefense, MinMDefenseV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.HitV, HitV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.MaxLifeV, LifeV);
            client.ClientData.PropsCacheManager.SetExtPropsSingle((int)PropsSystemTypes.ZhuLingZhuHunProps, (int)ExtPropIndexes.Dodge, DodgeV);
        }

        private static ZhuLingZhuHunEffect GetEffect(int Occupation)
        {
            foreach (ZhuLingZhuHunEffect e in Effect)
            {
                if (e.Occupation == Occupation)
                    return e;
            }
            return null;
        }

        private static ZhuLingZhuHunLimit GetLimit(int suit)
        {
            foreach (ZhuLingZhuHunLimit l in ZhuLingZhuHunManager.Limit)
            {
                if (l.SuitID == suit)
                    return l;
            }
            return null;
        }
    }
}
