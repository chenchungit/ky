using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Threading;

using Server.Data;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 武器类型信息
    /// </summary>
    public class WeaponTypeAndACTInfo
    {
        /// <summary>
        /// 握武器手势
        /// </summary>
        public int nHandType = -1;

        /// <summary>
        /// 武器动作类型
        /// </summary>
        public int nActionType = -1;
    }

    /// <summary>
    /// 武器佩戴限制信息
    /// </summary>
    public class WeaponAdornInfo
    {
        /// <summary>
        /// 职业限制
        /// </summary>
        public int nOccupationLimit;

        /// <summary>
        /// 武器类型
        /// </summary>
        public WeaponTypeAndACTInfo tagWeaponTypeInfo = new WeaponTypeAndACTInfo();

        /// <summary>
        /// 与本武器可同时佩戴的武器类型列表
        /// </summary>
        public List<WeaponTypeAndACTInfo> listCoexistType = new List<WeaponTypeAndACTInfo>();
    }

    public class WeaponAdornManager
    {
        /// <summary>
        /// 武器佩戴限制信息字典
        /// </summary>
        public static Dictionary<int, WeaponAdornInfo> dictWeaponAdornInfo = new Dictionary<int, WeaponAdornInfo>();

        /// <summary>
        /// 装入武器配带限制信息序号
        /// </summary>
        public static int GetWeaponAdornOrder(int nOccupation, int nHandType, int nActionType)
        {
            if (nOccupation < 0 || nHandType < 0 || nActionType < 0)
            {
                return -1;
            }

            return 1000 * nOccupation + 100 * nHandType + nActionType;
        }

        /// <summary>
        // 装入武器配带限制信息
        /// </summary>
        public static WeaponAdornInfo GetWeaponAdornInfo(int nOccupation, int nHandType, int nActionType)
        {
            int nOrder = GetWeaponAdornOrder(nOccupation, nHandType, nActionType);
            if (nOrder < 0)
            {
                return null;
            }

            WeaponAdornInfo weaponInfo = null;
            if (!dictWeaponAdornInfo.TryGetValue(nOrder, out weaponInfo))
            {
                return null;
            }

            return weaponInfo;
        }

        /// <summary>
        // 校验武器是否能装备
        /// </summary>
        public static int VerifyWeaponCanEquip(int nOccupation, int nHandType, int nActionType, 
                                               Dictionary<int, List<GoodsData>> EquipDict)
        {
            WeaponAdornInfo weaponInfo = GetWeaponAdornInfo(nOccupation, nHandType, nActionType);
            if (null == weaponInfo)
            {
                return -1;
            }

            // 已穿戴的武器数量
            int nWeaponCount = 0;

            List<GoodsData> listGood = null;
            for (int i = (int)ItemCategories.WuQi_Jian; i < (int)ItemCategories.HuFu; i++)
            {
                listGood = null;
                lock (EquipDict)
                {
                    if (!EquipDict.TryGetValue(i, out listGood))
                    {
                        continue;
                    }

                    if (null != listGood && listGood.Count > 0)
                    {
                        // 不能和任何武器同时穿戴
                        if (weaponInfo.listCoexistType.Count < 1)
                        {
                            return -5;
                        }

                        nWeaponCount += listGood.Count;

                        // 检查某类型已穿戴装备，是否和待穿戴的装备兼容
                        bool bCanEquip = false;
                        for (int nCount = 0; nCount < listGood.Count; nCount++)
                        {
                            bCanEquip = false;
                            SystemXmlItem systemGoods = null;
                            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(listGood[nCount].GoodsID, out systemGoods))
                            {
                                return -1;
                            }
                            
                            int nExistHandType = systemGoods.GetIntValue("HandType");
                            int nExistActionType = systemGoods.GetIntValue("ActionType");
                            for (int nCoexistCount = 0; nCoexistCount < weaponInfo.listCoexistType.Count; nCoexistCount++)
                            {
                                // 是可以穿戴的兼容装备
                                if (weaponInfo.listCoexistType[nCoexistCount].nHandType == nExistHandType
                                    && weaponInfo.listCoexistType[nCoexistCount].nActionType == nExistActionType)
                                {
                                    bCanEquip = true;
                                    break;
                                }
                            }
                        }

                        // 已经找到不能兼容的装备，不能穿戴
                        if (!bCanEquip)
                        {
                            return -5;
                        }
                    }
                }
            }

            // 武器最多同时装备两件
            if (nWeaponCount > 1)
            {
                return -3;
            }
          
            return 0;
        }

        /// <summary>
        // 装入武器配带限制信息
        /// </summary>
        public static void LoadWeaponAdornInfo()
        {
            try
            {
                XElement xmlFile = null;
                xmlFile = Global.GetGameResXml(string.Format("Config/WeaponAdorn.xml"));
                if (null == xmlFile)
                    return;

                string [] strFields = null;
                IEnumerable<XElement> ChgOccpXEle = xmlFile.Elements("Weapons").Elements();
                foreach (var xmlItem in ChgOccpXEle)
                {
                    if (null != xmlItem)
                    {
                        WeaponAdornInfo tmpInfo = new WeaponAdornInfo();
                        int nOccupation = (int)Global.GetSafeAttributeLong(xmlItem, "Occupation");
                        tmpInfo.nOccupationLimit = nOccupation;
                        
                        // 解析武器类型信息
                        string strWeaponType = Global.GetSafeAttributeStr(xmlItem, "Type");
                        if (!string.IsNullOrEmpty(strWeaponType.Trim()))
                        {
                            strFields = strWeaponType.Split(',');
                            if (null != strFields && strFields.Length == 2)
                            {
                                tmpInfo.tagWeaponTypeInfo.nHandType = Convert.ToInt32(strFields[0]);
                                tmpInfo.tagWeaponTypeInfo.nActionType = Convert.ToInt32(strFields[1]);
                            }
                        }

                        // 与本武器可同时佩戴的武器类型列表信息
                        string strCoexistType = Global.GetSafeAttributeStr(xmlItem, "CoexistType");
                        if (!string.IsNullOrEmpty(strCoexistType.Trim()))
                        {
                            strFields = strCoexistType.Split('|');
                            if (null != strFields && strFields.Length > 0)
                            {
                                for (int i = 0; i < strFields.Length; i++)
                                {
                                    string[] strWeaponTypes = strFields[i].Split(',');
                                    if (null != strWeaponTypes && strWeaponTypes.Length == 2)
                                    {
                                        WeaponTypeAndACTInfo tmpCoexistType = new WeaponTypeAndACTInfo();
                                        tmpCoexistType.nHandType = Convert.ToInt32(strWeaponTypes[0]);
                                        tmpCoexistType.nActionType = Convert.ToInt32(strWeaponTypes[1]);
                                        tmpInfo.listCoexistType.Add(tmpCoexistType);
                                    }
                                }
                            }
                        }

                        int nOrder = GetWeaponAdornOrder(nOccupation, tmpInfo.tagWeaponTypeInfo.nHandType, tmpInfo.tagWeaponTypeInfo.nActionType);
                        if (nOrder > 0)
                        {
                            dictWeaponAdornInfo.Add(nOrder, tmpInfo);
                        }
                    }
                }

            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", string.Format("Config/WeaponAdorn.xml")));
            }
        }
    }
}
