using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;

namespace GameServer.Logic
{
    /// <summary>
    /// 单件装备的属性加成
    /// </summary>
    public class SingleEquipAddPropMgr
    {
        #region 获取缓存的属性列表

        /// <summary>
        /// 获取缓存的属性列表
        /// </summary>
        /// <param name="occupation"></param>
        /// <param name="goodsData"></param>
        /// <param name="systemGoods"></param>
        /// <returns></returns>
        private static List<double[]> GetCachingPropsList(SingleEquipProps singleEquipPropsMgr, int occupation, SystemXmlItem systemGoods)
        {
            int categoriy = systemGoods.GetIntValue("Categoriy");
            int suitID = systemGoods.GetIntValue("SuitID");
            List<double[]> propsList = singleEquipPropsMgr.GetSingleEquipPropsList(occupation, categoriy, suitID);
            return propsList;
        }

        /// <summary>
        /// 将附加的属性应用到装备上
        /// </summary>
        /// <param name="equipProps"></param>
        /// <param name="newProps"></param>
        private static void ApplyNewPropsToEquipProps(double[] equipProps, double[] newProps, bool toAdd)
        {
        }

        #endregion 获取缓存的属性列表

        #region 缓存所有属性加成

        /// <summary>
        /// 缓存所有属性加成
        /// </summary>
        public static void LoadAllSingleEquipProps()
        {
            //加载单件装备强化属性加成缓存
            LoadSingleEquipPropsForge();

            //加载单件装备宝石属性加成缓存
            LoadSingleEquipPropsJewels();

            //加载单件装备附加属性缓存
            LoadSingleEquipPropsFuJia();
        }

        #endregion 缓存所有属性加成

        #region 单件装备强化属性加成

        /// <summary>
        /// 单件装备强化属性加成缓存
        /// </summary>
        private static SingleEquipProps _SingleEquipPropsForgeMgr = new SingleEquipProps();

        /// <summary>
        /// 加载单件装备强化属性加成缓存
        /// </summary>
        private static void LoadSingleEquipPropsForge()
        {
            _SingleEquipPropsForgeMgr.LoadEquipPropItems("Config/SingleEquipAddProp/QiangHua/");
        }

        /// <summary>
        /// 处理强化的属性加成
        /// </summary>
        /// <param name="equipProps"></param>
        /// <param name="goodsData"></param>
        /// <param name="systemGoodsItem"></param>
        public static void ProcessSingleEquipPropsForge(double[] equipProps, int occupation, GoodsData goodsData, SystemXmlItem systemGoods, bool toAdd)
        {
            List<double[]> propsList = GetCachingPropsList(_SingleEquipPropsForgeMgr, occupation, systemGoods);
            if (null == propsList || propsList.Count <= 0) return;

            int propsIndex = 0;
            if (goodsData.Forge_level >= 10)
            {
                propsIndex = 3;
            }
            else if (goodsData.Forge_level >= 9)
            {
                propsIndex = 2;
            }
            else if (goodsData.Forge_level >= 7)
            {
                propsIndex = 1;
            }
            else if (goodsData.Forge_level >= 5)
            {
                propsIndex = 0;
            }
            else
            {
                return;
            }

            if (propsIndex >= propsList.Count) return;

            //累加计算
            for (int i = 0; i <= propsIndex; i++)
            {
                double[] newProps = propsList[i];
                if (null == newProps || newProps.Length != 10)
                {
                    return;
                }

                //将附加的属性应用到装备上
                ApplyNewPropsToEquipProps(equipProps, newProps, toAdd);
            }
        }

        #endregion 单件装备强化属性加成

        #region 单件装备宝石属性加成

        /// <summary>
        /// 单件装备宝石属性加成缓存
        /// </summary>
        private static SingleEquipProps _SingleEquipPropsJewelsMgr = new SingleEquipProps();

        /// <summary>
        /// 加载单件装备宝石属性加成缓存
        /// </summary>
        private static void LoadSingleEquipPropsJewels()
        {
            _SingleEquipPropsJewelsMgr.LoadEquipPropItems("Config/SingleEquipAddProp/Jewels/");
        }

        /// <summary>
        /// 处理宝石的属性加成
        /// </summary>
        /// <param name="equipProps"></param>
        /// <param name="goodsData"></param>
        /// <param name="systemGoodsItem"></param>
        public static void ProcessSingleEquipPropsJewels(double[] equipProps, int occupation, AllThingsCalcItem singleEquipJewels, SystemXmlItem systemGoods, bool toAdd)
        {
            List<double[]> propsList = GetCachingPropsList(_SingleEquipPropsJewelsMgr, occupation, systemGoods);
            if (null == propsList || propsList.Count <= 0) return;

            int propsIndex = 0;
            if (singleEquipJewels.TotalJewel8LevelNum >= (6))
            {
                propsIndex = 2;
            }
            else if ((singleEquipJewels.TotalJewel6LevelNum + singleEquipJewels.TotalJewel7LevelNum + singleEquipJewels.TotalJewel8LevelNum) >= (6))
            {
                propsIndex = 1;
            }
            else if ((singleEquipJewels.TotalJewel4LevelNum + singleEquipJewels.TotalJewel5LevelNum + singleEquipJewels.TotalJewel6LevelNum + singleEquipJewels.TotalJewel7LevelNum + singleEquipJewels.TotalJewel8LevelNum) >= (6))
            {
                propsIndex = 0;
            }
            else
            {
                return;
            }

            if (propsIndex >= propsList.Count) return;

            for (int i = 0; i <= propsIndex; i++)
            {
                double[] newProps = propsList[i];
                if (null == newProps || newProps.Length != 10)
                {
                    return;
                }

                //将附加的属性应用到装备上
                ApplyNewPropsToEquipProps(equipProps, newProps, toAdd);
            }
        }

        #endregion 单件装备宝石属性加成

        #region 单件装备附加属性

        /// <summary>
        /// 单件装备附加属性缓存
        /// </summary>
        private static SingleEquipProps _SingleEquipPropsFuJiaMgr = new SingleEquipProps();

        /// <summary>
        /// 加载单件装备附加属性缓存
        /// </summary>
        private static void LoadSingleEquipPropsFuJia()
        {
            _SingleEquipPropsFuJiaMgr.LoadEquipPropItems("Config/SingleEquipAddProp/FuJia/");
        }

        /// <summary>
        /// 处理附加的属性加成
        /// </summary>
        /// <param name="equipProps"></param>
        /// <param name="goodsData"></param>
        /// <param name="systemGoodsItem"></param>
        public static void ProcessSingleEquipPropsFuJia(double[] equipProps, int occupation, GoodsData goodsData, SystemXmlItem systemGoods, bool toAdd)
        {
            List<double[]> propsList = GetCachingPropsList(_SingleEquipPropsFuJiaMgr, occupation, systemGoods);
            if (null == propsList || propsList.Count <= 0) return;

            int propsIndex = 0;
            if (goodsData.Quality >= 4)
            {
                propsIndex = 3;
            }
            else if (goodsData.Quality >= 3)
            {
                propsIndex = 2;
            }
            else if (goodsData.Quality >= 2)
            {
                propsIndex = 1;
            }
            else if (goodsData.Quality >= 1)
            {
                propsIndex = 0;
            }
            else
            {
                return;
            }

            if (propsIndex >= propsList.Count) return;
            double[] newProps = propsList[propsIndex];
            if (null == newProps || newProps.Length != 10)
            {
                return;
            }

            int addPropIndex = goodsData.AddPropIndex;
            addPropIndex = Global.GMax(addPropIndex, 0);
            addPropIndex = Global.GMin(addPropIndex, 10);

            double[] calcProps = new double[newProps.Length];
            for (int i = 0; i < newProps.Length; i++)
            {
                double origExtProp = newProps[i];
                double newProp = origExtProp * (1 + addPropIndex);
                calcProps[i] = newProp;
            }

            //将附加的属性应用到装备上
            ApplyNewPropsToEquipProps(equipProps, calcProps, toAdd);
        }

        #endregion 单件装备附加属性
    }
}
