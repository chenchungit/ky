using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Windows;
using System.Collections;

namespace GameServer.Logic//.StarConstellation
{
    // 星座类型信息类 [7/31/2014 LiaoWei]
    public class StarConstellationProp
    {
        /// <summary>
        /// 一级属性数值
        /// </summary>
        private double[] m_StarConstellationFirstProps = new double[(int)UnitPropIndexes.Max];

        /// <summary>
        /// 一级属性数值
        /// </summary>
        public double[] StarConstellationFirstProps
        {
            get { return m_StarConstellationFirstProps; }
        }

        /// <summary>
        /// 二级属性数值
        /// </summary>
        private double[] m_StarConstellationSecondProps = new double[(int)ExtPropIndexes.Max];

        /// <summary>
        /// 二级属性数值
        /// </summary>
        public double[] StarConstellationSecondProps
        {
            get { return m_StarConstellationSecondProps; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public StarConstellationProp()
        {
            ResetStarConstellationProps();
        }

        /// <summary>
        /// 清空属性值
        /// </summary>
        public void ResetStarConstellationProps()
        {
            for (int i = 0; i < (int)UnitPropIndexes.Max; i++)
            {
                m_StarConstellationFirstProps[i] = 0;
            }

            for (int i = 0; i < (int)ExtPropIndexes.Max; i++)
            {
                m_StarConstellationSecondProps[i] = 0;
            }
        }

    }

    // 星座类型信息类 [7/31/2014 LiaoWei]
    public class StarConstellationTypeInfo
    {
        /// <summary>
        /// 类型ID
        /// </summary>
        public int TypeID = 0;

        /// <summary>
        /// 转生等级要求
        /// </summary>
        public int ChangeLifeLimit = 0;

        /// <summary>
        /// 等级要求
        /// </summary>
        public int LevelLimit = 0;

        /// <summary>
        /// 加成属性信息
        /// </summary>
        public PropertyInfo Propertyinfo = null;

        /// <summary>
        /// 附加系数星位限制
        /// </summary>
        public int[] AddPropStarSiteLimit;

        /// <summary>
        /// 附加系数数值 
        /// </summary>
        public int[] AddPropModulus;

    }

    // 星座详细信息类 [7/31/2014 LiaoWei]
    public class StarConstellationDetailInfo
    {
        /// <summary>
        /// 星位ID
        /// </summary>
        public int StarConstellationID = 0;

        /// <summary>
        /// 转生等级要求
        /// </summary>
        public int ChangeLifeLimit = 0;

        /// <summary>
        /// 等级要求
        /// </summary>
        public int LevelLimit = 0;

        /// <summary>
        /// 加成属性信息
        /// </summary>
        public PropertyInfo Propertyinfo = null;

        /// <summary>
        /// 需求星魂值
        /// </summary>
        public int NeedStarSoul = 0;

        /// <summary>
        /// 需求物品ID
        /// </summary>
        public int NeedGoodsID = 0;

        /// <summary>
        /// 需求物品数量
        /// </summary>
        public int NeedGoodsNum = 0;

        /// <summary>
        /// 需求金币数量
        /// </summary>
        public int NeedJinBi = 0;

        /// <summary>
        /// 成功率
        /// </summary>
        public int SuccessRate = 0;
    }

    public class PropertyInfo
    {
        /// <summary>
        /// 属性ID1
        /// </summary>
        public int PropertyID1 = 0;

        /// <summary>
        /// 属性ID2
        /// </summary>
        public int PropertyID2 = 0;

        /// <summary>
        /// 属性ID3
        /// </summary>
        public int PropertyID3 = 0;

        /// <summary>
        /// 属性ID4
        /// </summary>
        public int PropertyID4 = 0;

        /// <summary>
        /// 属性ID5
        /// </summary>
        public int PropertyID5 = 0;

        /// <summary>
        /// 属性ID6
        /// </summary>
        public int PropertyID6 = 0;

        /// <summary>
        /// 属性ID7
        /// </summary>
        public int PropertyID7 = 0;

        /// <summary>
        /// 属性ID8
        /// </summary>
        public int PropertyID8 = 0;

        /// <summary>
        /// 属性ID9
        /// </summary>
        public int PropertyID9 = 0;

        /// <summary>
        /// 属性ID10
        /// </summary>
        public int PropertyID10 = 0;

        /// <summary>
        /// 属性ID11
        /// </summary>
        public int PropertyID11 = 0;


        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue1 = 0;

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue1 = 0;

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue2 = 0;

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue2 = 0;

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue3 = 0;

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue3 = 0;

        /// <summary>
        /// 属性增加最大值
        /// </summary>
        public int AddPropertyMaxValue4 = 0;

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue4 = 0;

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue5 = 0;

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue6 = 0;

        /// <summary>
        /// 属性增加最小值
        /// </summary>
        public int AddPropertyMinValue7 = 0;
    }


}
