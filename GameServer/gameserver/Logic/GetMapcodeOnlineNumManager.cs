using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic
{
    public class GetMapcodeOnlineNumManager
    {
        /// <summary>
        /// 保存要统计人数的地图ID数组
        /// </summary>
        private static int[] arrCountMapcode = null;

        /// <summary>
        /// 从系统参数中装入要统计在线人数的地图ID
        /// </summary>
        public static void LoadCountMapID()
        {
            arrCountMapcode = GameManager.systemParamsList.GetParamValueIntArrayByName("CountOnlineMapID");
        }

        /// <summary>
        /// 某地图是否要统计人数
        /// </summary>
        public static int IsCountMapID(int nMapID)
        {
            for (int i = 0; i < arrCountMapcode.Length; ++i)
            {
                if (arrCountMapcode[i] == nMapID)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 统计地图人数，形成在线人数字符串
        /// </summary>
        public static String CountMapIDOnlineNum()
        {
            if (null == arrCountMapcode)
            {
                LoadCountMapID();
            }
            if (null == arrCountMapcode)
            {
                return "";
            }

            String strOnlineInfo = "";
            for (int i = 0; i < arrCountMapcode.Length; i++)
            {
                if (0 != i)
                {
                    strOnlineInfo += "|";
                }

                strOnlineInfo += string.Format("{0},{1}", arrCountMapcode[i], GameManager.ClientMgr.GetMapClientsCount(arrCountMapcode[i]));
            }

            return strOnlineInfo;
        }
    }

   
}
