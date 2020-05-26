using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Tmsk.Contract;

namespace GameServer.Logic
{
    /// <summary>
    /// 成就、武学、经脉等高级buffer管理
    /// </summary>
    public class AdvanceBufferPropsMgr
    {
        #region 相关类型定义

        private readonly static Dictionary<BufferItemTypes, string> BufferId2ConfigParamsNameDict = new Dictionary<BufferItemTypes, string>()
        {
            {BufferItemTypes.ChengJiu, "ChengJiuBufferGoodsIDs"},
            {BufferItemTypes.JingMai, "JingMaiBufferGoodsIDs"},
            {BufferItemTypes.WuXue, "WuXueBufferGoodsIDs"},
            {BufferItemTypes.ZuanHuang, "ZhuanhuangBufferGoodsIDs"},
            {BufferItemTypes.ZhanHun, "ZhanhunBufferGoodsIDs"},
            {BufferItemTypes.RongYu, "RongyaoBufferGoodsIDs"},
            {BufferItemTypes.JunQi, "JunQiBufferGoodsIDs"},
            {BufferItemTypes.MU_FRESHPLAYERBUFF, "FreshPlayerBufferGoodsIDs"},
            {BufferItemTypes.MU_ANGELTEMPLEBUFF1,"AngelTempleGoldBuffGoodsID"},
            {BufferItemTypes.MU_ANGELTEMPLEBUFF2,"AngelTempleGoldBuffGoodsID"},
            {BufferItemTypes.MU_JINGJICHANG_JUNXIAN, "JunXianBufferGoodsIDs"},
            {BufferItemTypes.MU_WORLDLEVEL, "WorldLevelGoodsIDs"},
            {BufferItemTypes.MU_ZHANMENGBUILD_ZHANQI, "ZhanMengZhanQiBUFF"},
            {BufferItemTypes.MU_ZHANMENGBUILD_JITAN, "ZhanMengJiTanBUFF"},
            {BufferItemTypes.MU_ZHANMENGBUILD_JUNXIE, "ZhanMengJunXieBUFF"},
            {BufferItemTypes.MU_ZHANMENGBUILD_GUANGHUAN, "ZhanMengGuangHuanBUFF"},
        };

        /// <summary>
        /// 需要定时处理的加属性Buff类型定义
        /// 小于0 暂不需要定时处理加属性
        /// 类型0 bubferVal代表物品列表的索引
        /// 类型1 BufferVal代表物品ID
        /// </summary>
        private readonly static Dictionary<BufferItemTypes, int> BufferId2ConfigTypeDict = new Dictionary<BufferItemTypes, int>()
        {
            {BufferItemTypes.ChengJiu, 0},
            //{BufferItemTypes.JingMai, 0},
            //{BufferItemTypes.WuXue, 0},
            {BufferItemTypes.ZuanHuang, 0},
            {BufferItemTypes.ZhanHun, 0},
            {BufferItemTypes.RongYu, 0},
            {BufferItemTypes.JunQi, 0},
            //{BufferItemTypes.MU_FRESHPLAYERBUFF, 0},
            {BufferItemTypes.MU_ANGELTEMPLEBUFF1,0},
            {BufferItemTypes.MU_ANGELTEMPLEBUFF2,0},
            {BufferItemTypes.MU_JINGJICHANG_JUNXIAN, 0},
            //{BufferItemTypes.MU_WORLDLEVEL, -1},
            {BufferItemTypes.MU_ZHANMENGBUILD_ZHANQI, 0},
            {BufferItemTypes.MU_ZHANMENGBUILD_JITAN, 0},
            {BufferItemTypes.MU_ZHANMENGBUILD_JUNXIE, 0},
            {BufferItemTypes.MU_ZHANMENGBUILD_GUANGHUAN, 0},
            {BufferItemTypes.JieRiChengHao, 1},
        };

        #endregion 相关类型定义

        #region 缓存字典

        /// <summary>
        /// 缓存字典
        /// </summary>
        private static Dictionary<int, int[]> CachingIDsDict = new Dictionary<int, int[]>();

        /// <summary>
        /// 根据BufferID获取缓存的物品ID列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static int[] GetCachingIDsByID(int id)
        {
            int[] ids = null;
            lock (CachingIDsDict)
            {
                if (!CachingIDsDict.TryGetValue(id, out ids))
                {
                    string paramName = "";
                    if (BufferId2ConfigParamsNameDict.TryGetValue((BufferItemTypes)id, out paramName))
                    {
                        ids = GameManager.systemParamsList.GetParamValueIntArrayByName(paramName);
                    }

                    CachingIDsDict[id] = ids;
                }
            }

            return ids;
        }

        /// <summary>
        /// 重置缓存
        /// </summary>
        public static void ResetCache()
        {
            lock (CachingIDsDict)
            {
                CachingIDsDict.Clear();
                foreach (var kv in BufferId2ConfigParamsNameDict)
                {
                    int bufferId = (int)kv.Key;
                    string paramName = kv.Value;
                    int[] ids = GameManager.systemParamsList.GetParamValueIntArrayByName(paramName);
                    CachingIDsDict[bufferId] = ids;
                }
            }
        }

        #endregion 缓存字典

        #region 属性接口

        /// <summary>
        /// 获取物品ID
        /// </summary>
        /// <param name="bufferItemType"></param>
        /// <param name="goodsIndex"></param>
        /// <returns></returns>
        public static int GetGoodsID(BufferItemTypes bufferItemType, int goodsIndex)
        {
            /// 根据BufferID获取缓存的物品ID列表
            int[] goodsIds = GetCachingIDsByID((int)bufferItemType);
            if (null == goodsIds)
            {
                return -1;
            }

            if (goodsIndex < 0 || goodsIndex >= goodsIds.Length)
            {
                return -1;
            }

            int goodsID = goodsIds[goodsIndex];
            return goodsID;
        }

        /// <summary>
        /// 获取扩展属性接口
        /// </summary>
        /// <param name="bufferItemType"></param>
        /// <param name="extPropIndexe"></param>
        /// <returns></returns>
        public static double GetExtProp(BufferItemTypes bufferItemType, ExtPropIndexes extPropIndexe, int goodsIndex)
        {
            /// 根据BufferID获取缓存的物品ID列表
            int[] goodsIds = GetCachingIDsByID((int)bufferItemType);
            if (null == goodsIds)
            {
                return 0.0;
            }

            if (goodsIndex < 0 || goodsIndex >= goodsIds.Length)
            {
                return 0.0;
            }

            int goodsID = goodsIds[goodsIndex];
            EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(goodsID);
            if (null == item)
            {
                return 0.0;
            }

            return item.ExtProps[(int)extPropIndexe];
        }

        /// <summary>
        /// 获取扩展属性接口
        /// </summary>
        /// <param name="bufferItemType"></param>
        /// <param name="extPropIndexe"></param>
        /// <returns></returns>
        public static double GetExtPropByGoodsID(BufferItemTypes bufferItemType, ExtPropIndexes extPropIndexe, int goodsID)
        {
            EquipPropItem item = GameManager.EquipPropsMgr.FindEquipPropItem(goodsID);
            if (null == item)
            {
                return 0.0;
            }

            return item.ExtProps[(int)extPropIndexe];
        }

        #endregion 属性接口

        #region 角色Buff

        public static void AddTempBufferProp(GameClient client, BufferItemTypes bufferID, int type)
        {
            EquipPropItem item = null;

            do 
            {
                //判断此地图是否允许使用Buffer
                if (!Global.CanMapUseBuffer(client.ClientData.MapCode, (int)bufferID))
                {
                    break;
                }

                BufferData bufferData = Global.GetBufferDataByID(client, (int)bufferID);
                if (null == bufferData)
                {
                    break;
                }

                if (Global.IsBufferDataOver(bufferData))
                {
                    break;
                }

                int bufferGoodsId = 0;
                if (type == 0)
                {
                    // VIP处理 [4/10/2014 LiaoWei]
                    int goodsIndex = 0;
                    if (bufferID == BufferItemTypes.ZuanHuang)
                        goodsIndex = client.ClientData.VipLevel;
                    else
                        goodsIndex = (int)bufferData.BufferVal;

                    int[] goodsIds = GetCachingIDsByID((int)bufferID);
                    if (null == goodsIds)
                    {
                        break;
                    }

                    if (goodsIndex < 0 || goodsIndex >= goodsIds.Length)
                    {
                        break;
                    }

                    bufferGoodsId = goodsIds[goodsIndex];
                }
                else if (type == 1)
                {
                    bufferGoodsId = (int)bufferData.BufferVal;
                }

                if (bufferGoodsId > 0)
                {
                    item = GameManager.EquipPropsMgr.FindEquipPropItem(bufferGoodsId);
                }
            } while (false);

            if (null != item)
            {
                client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.BufferByGoodsProps, bufferID, item.ExtProps);
            }
            else
            {
                client.ClientData.PropsCacheManager.SetExtProps(PropsSystemTypes.BufferByGoodsProps, bufferID, PropsCacheManager.ConstExtProps);
            }
        }

        /// <summary>
        /// 处理Buff
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void DoSpriteBuffers(GameClient client)
        {
            int age = client.ClientData.PropsCacheManager.GetAge();
            foreach (var kv in BufferId2ConfigTypeDict)
            {
                if (kv.Value >= 0)
                {
                    AddTempBufferProp(client, kv.Key, kv.Value);
                }
            }

            if (age != client.ClientData.PropsCacheManager.GetAge())
            {
                client.delayExecModule.SetDelayExecProc(DelayExecProcIds.NotifyRefreshProps);
            }
        }

        #endregion 角色Buff
    }
}
