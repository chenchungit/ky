using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic
{
    /*
     * 
     * 1.3

    精灵系统
    精灵猎取系统
    元素之心系统
    罗兰城战系统
    战盟BOSS系统
    战功商店系统
    罗兰法阵多人副本

    1.4
    翎羽系统
    翅膀注灵系统
    翅膀注魂系统
    成就符文系统
    神器再造系统
    罗兰城主雕像膜拜

    1.4.1
    月卡
    新增的合服活动	无需特别屏蔽
    二级密码
    跨服战
    声望勋章
     
     */




    enum GameFuncType
    {
        System1Dot3 = 1,
        System1Dot4 = 2,
        System1Dot4Dot1 = 3,
        System1Dot5 = 4,
        System1Dot6 = 5,
        System1Dot7 = 6,
        System1Dot8 = 7,
        System1Dot9 = 8,
        System2Dot0 = 9,
    }

    class GameFuncControlManager
    {
        #region 成员

        private static List<int> GameFuncTypeList = new List<int>();

        #endregion

        #region 配置文件

        public static void LoadConfig()
        {
            string fileName = "Config/GameFuncControl.xml";
            string fullPathFileName = Global.GameResPath(fileName);

            XElement xml = null;

            try
            {
                xml = XElement.Load(fullPathFileName);
            }
            catch (Exception)
            {
                return;
            }

            XElement args = xml.Element("GameFunc");
            if (null != args)
            {
                IEnumerable<XElement> xmlItems = args.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    int Effective = (int)Global.GetSafeAttributeLong(xmlItem, "Effective");

                    if (0 == Effective)
                    {
                        int TypeID = (int)Global.GetSafeAttributeLong(xmlItem, "TypeID");
                        GameFuncTypeList.Add(TypeID);
                        string Description = Global.GetSafeAttributeStr(xmlItem, "Description");
                        SysConOut.WriteLine(string.Format("{0}所有功能已屏蔽", Description));
                    }
                }
            }
        }

        public static bool IsGameFuncDisabled(GameFuncType type)
        {
            return GameFuncTypeList.IndexOf((int)type) >= 0;
        }

        #endregion

    }
}
