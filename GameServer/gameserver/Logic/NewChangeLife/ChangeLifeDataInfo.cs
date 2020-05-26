using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Windows;
using System.Collections;
using Server.Data;

namespace GameServer.Logic//.NewChangeLife
{
    // 转生数据信息类 [8/25/2014 LiaoWei]

    // 星座类型信息类 [7/31/2014 LiaoWei]
    public class ChangeLifeProp
    {
        /// <summary>
        /// 一级属性数值
        /// </summary>
        private double[] m_ChangeLifeFirstProps = new double[(int)UnitPropIndexes.Max];

        /// <summary>
        /// 一级属性数值
        /// </summary>
        public double[] ChangeLifeFirstProps
        {
            get { return m_ChangeLifeFirstProps; }
        }

        /// <summary>
        /// 二级属性数值
        /// </summary>
        private double[] m_ChangeLifeSecondProps = new double[(int)ExtPropIndexes.Max];

        /// <summary>
        /// 二级属性数值
        /// </summary>
        public double[] ChangeLifeSecondProps
        {
            get { return m_ChangeLifeSecondProps; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ChangeLifeProp()
        {
            ResetChangeLifeProps();
        }

        /// <summary>
        /// 清空属性值
        /// </summary>
        public void ResetChangeLifeProps()
        {
            for (int i = 0; i < (int)UnitPropIndexes.Max; i++)
            {
                m_ChangeLifeFirstProps[i] = 0;
            }

            for (int i = 0; i < (int)ExtPropIndexes.Max; i++)
            {
                m_ChangeLifeSecondProps[i] = 0;
            }
        }
    }

    public class ChangeLifeDataInfo
    {
        /// <summary>
        /// 职业ID
        /// </summary>
        public int ChangeLifeID { get; set; }

        /// <summary>
        /// 需要的等级
        /// </summary>
        public int NeedLevel { get; set; }

        /// <summary>
        /// 需要的金币
        /// </summary>
        public int NeedMoney { get; set; }

        /// <summary>
        /// 需要的魔晶
        /// </summary>
        public int NeedMoJing { get; set; }

        /// <summary>
        /// 需要的物品
        /// </summary>
        public List<GoodsData> NeedGoodsDataList { get; set; }

        /// <summary>
        /// 加成属性信息
        /// </summary>
        public ChangeLifePropertyInfo Propertyinfo = null;

        /// <summary>
        /// 需要的物品
        /// </summary>
        public List<GoodsData> AwardGoodsDataList { get; set; }

        /// <summary>
        /// 升级经验系数
        /// </summary>
        public long ExpProportion { get; set; }
    }

    public class ChangeLifePropertyInfo
    {
        /// <summary>
        /// 物理攻击力最小值
        /// </summary>
        public int PhyAttackMin = 0;

        /// <summary>
        /// 物理攻击力最大值
        /// </summary>
        public int PhyAttackMax = 0;

        /// <summary>
        /// 魔法攻击力最小值
        /// </summary>
        public int MagAttackMin = 0;

        /// <summary>
        /// 魔法攻击力最大值
        /// </summary>
        public int MagAttackMax = 0;

        /// <summary>
        /// 物理防御力最小值
        /// </summary>
        public int PhyDefenseMin = 0;

        /// <summary>
        /// 物理防御力最大值
        /// </summary>
        public int PhyDefenseMax = 0;

        /// <summary>
        /// 魔法防御力最小值
        /// </summary>
        public int MagDefenseMin = 0;

        /// <summary>
        /// 魔法防御力最大值
        /// </summary>
        public int MagDefenseMax = 0;

        /// <summary>
        /// 命中值
        /// </summary>
        public int HitProp = 0;

        /// <summary>
        /// 闪避值
        /// </summary>
        public int DodgeProp = 0;

        /// <summary>
        /// 最大生命上限
        /// </summary>
        public int MaxLifeProp = 0;


        /// <summary>
        /// 物理攻击最小值增量
        /// </summary>
        public int AddPhyAttackMinValue = 0;

        /// <summary>
        /// 物理攻击最大值增量
        /// </summary>
        public int AddPhyAttackMaxValue = 0;

        /// <summary>
        /// 魔法攻击最小值增量
        /// </summary>
        public int AddMagAttackMinValue = 0;

        /// <summary>
        /// 魔法攻击最大值增量
        /// </summary>
        public int AddMagAttackMaxValue = 0;

        /// <summary>
        /// 物理防御力最小值增量
        /// </summary>
        public int AddPhyDefenseMinValue = 0;

        /// <summary>
        /// 物理防御力最大值增量
        /// </summary>
        public int AddPhyDefenseMaxValue = 0;

        /// <summary>
        /// 魔法防御力最小值增量
        /// </summary>
        public int AddMagDefenseMinValue = 0;

        /// <summary>
        /// 魔法防御力最大值增量
        /// </summary>
        public int AddMagDefenseMaxValue = 0;

        /// <summary>
        /// 命中值增量
        /// </summary>
        public int AddHitPropValue = 0;

        /// <summary>
        /// 闪避值增量
        /// </summary>
        public int AddDodgePropValue = 0;

        /// <summary>
        /// 最大生命上限增量
        /// </summary>
        public int AddMaxLifePropValue = 0;

        public void AddFrom(ChangeLifePropertyInfo info)
        {
            AddPhyAttackMinValue += info.AddPhyAttackMinValue;
            AddPhyAttackMaxValue += info.AddPhyAttackMaxValue;
            AddMagAttackMinValue += info.AddMagAttackMinValue;
            AddMagAttackMaxValue += info.AddMagAttackMaxValue;
            AddPhyDefenseMinValue += info.AddPhyDefenseMinValue;
            AddPhyDefenseMaxValue += info.AddPhyDefenseMaxValue;
            AddMagDefenseMinValue += info.AddMagDefenseMinValue;
            AddMagDefenseMaxValue += info.AddMagDefenseMaxValue;
            AddHitPropValue += info.AddHitPropValue;
            AddDodgePropValue += info.AddDodgePropValue;
            AddMaxLifePropValue += info.AddMaxLifePropValue;
        }
    }
}
