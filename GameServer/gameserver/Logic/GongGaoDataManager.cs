using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using GameServer.Server;
using Server.Tools;
using System.IO;
using Server.Data;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 进行游戏时，公告管理器
    /// </summary>
    public class GongGaoDataManager
    {
        /// <summary>
        /// 公告XML
        /// </summary>
        public static SystemXmlItems systemGongGaoMgr = new SystemXmlItems();

        /// <summary>
        /// 公告文件内容
        /// </summary>
        public static String strGongGaoXML = "";

        /// <summary>
        /// 装入公告数据
        /// </summary>
        public static void LoadGongGaoData()
        {
            String fullPathFileName = Global.IsolateResPath("Config/Gonggao.xml");
            strGongGaoXML = File.ReadAllText(fullPathFileName);

            systemGongGaoMgr.LoadFromXMlFile("Config/Gonggao.xml", "", "ID", 1);
        }

        public static void CheckGongGaoInfo(GameClient client, int nID)
        {
            String strBeginTime = "";
            String strEndTime = "";
           
            foreach (var systemMallItem in systemGongGaoMgr.SystemXmlItemDict.Values)
            {
                strBeginTime = systemMallItem.GetStringValue("FromDate");
                strEndTime = systemMallItem.GetStringValue("ToDate");
                break;
            }

            int nHaveGongGao = 0;
            String strCurrDateTime = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");
            if (String.Compare(strCurrDateTime, strBeginTime) >= 0 && String.Compare(strCurrDateTime, strEndTime) <= 0)
            {
                nHaveGongGao = 1;
            }

            int nLianXuLoginReward = 0;
            int nLeiJiLoginReward = 0;
            if (client._IconStateMgr.CheckFuLiLianXuDengLuReward(client))
            {
                nLianXuLoginReward = 1;
            }

            if (client._IconStateMgr.CheckFuLiLeiJiDengLuReward(client))
            {
                nLeiJiLoginReward = 1;
            }

            GongGaoData gongGaoData = new GongGaoData();

            if (1 == nHaveGongGao)
            {
                gongGaoData.strGongGaoInfo = strGongGaoXML;
            }

            gongGaoData.nHaveGongGao = nHaveGongGao;
            gongGaoData.nLianXuLoginReward = nLianXuLoginReward;
            gongGaoData.nLeiJiLoginReward = nLeiJiLoginReward;

            client.sendCmd<GongGaoData>(nID, gongGaoData);
        }
    }
}
