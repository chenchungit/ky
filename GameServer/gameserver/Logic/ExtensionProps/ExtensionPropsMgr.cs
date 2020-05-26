using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Interface;
using Server.Tools;
using GameServer.Server;
using Server.TCP;

namespace GameServer.Logic.ExtensionProps
{
    /// <summary>
    /// 拓展属性项
    /// </summary>
    public class ExtensionPropItem
    {
        /// <summary>
        /// 流水ID
        /// </summary>
        public int ID = 0;

        /// <summary>
        /// 前置的拓展属性ID
        /// </summary>
        public Dictionary<int, byte> PrevTuoZhanShuXing = null;

        /// <summary>
        /// 目标类型, 0: 自身, 1: 敌人
        /// </summary>
        public int TargetType = 0;

        /// <summary>
        /// 触发类型, 0: 攻击触发, 1: 受击触发
        /// </summary>
        public int ActionType = 0;

        /// <summary>
        /// 触发概率(0~100)
        /// </summary>
        public int Probability = 0;

        /// <summary>
        /// 允许触发的技能ID
        /// </summary>
        public Dictionary<int, byte> NeedSkill = null;

        /// <summary>
        /// 触发后显示的图标
        /// </summary>
        public int Icon = 0;

        /// <summary>
        /// 触发后显示的目标特效
        /// </summary>
        public int TargetDecoration = 0;

        /// <summary>
        /// 触发后显示的延迟特效
        /// </summary>
        public int DelayDecoration = 0;
    }

    /// <summary>
    /// 拓展属性缓存管理
    /// </summary>
    public class ExtensionPropsMgr
    {
        /// <summary>
        /// 缓存字典
        /// </summary>
        private static Dictionary<int, ExtensionPropItem> _ExtensionPropsCachingDict = new Dictionary<int, ExtensionPropItem>();

        /// <summary>
        /// 根据ID和获取配置项
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ExtensionPropItem FindCachingItem(int id)
        {
            ExtensionPropItem extensionPropItem = null;
            if (!_ExtensionPropsCachingDict.TryGetValue(id, out extensionPropItem))
            {
                return null;
            }

            return extensionPropItem;
        }

        /// <summary>
        /// 解析字典
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static Dictionary<int, byte> ParseDict(string str)
        {
            Dictionary<int, byte> dict = new Dictionary<int, byte>();

            string[] fields = str.Split(',');
            for (int i = 0; i < fields.Length; i++)
            {
                dict[Global.SafeConvertToInt32(fields[i])] = 1;
            }

            return dict;
        }

        /// <summary>
        /// 从xml项中解析缓存项
        /// </summary>
        /// <param name="systemXmlItem"></param>
        private static ExtensionPropItem ParseCachingItem(SystemXmlItem systemXmlItem)
        {
            ExtensionPropItem extensionPropItem = new ExtensionPropItem()
            {
                ID = systemXmlItem.GetIntValue("ID"),
                PrevTuoZhanShuXing = ParseDict(systemXmlItem.GetStringValue("PrevTuoZhanShuXing")),
                TargetType = systemXmlItem.GetIntValue("TargetTyp"),
                ActionType = systemXmlItem.GetIntValue("ActionType"),
                Probability = (int)(systemXmlItem.GetDoubleValue("Probability") * 100),
                NeedSkill = ParseDict(systemXmlItem.GetStringValue("NeedSkill")),
                Icon = systemXmlItem.GetIntValue("Icon"),
                TargetDecoration = systemXmlItem.GetIntValue("TargetDecoration"),
                DelayDecoration = systemXmlItem.GetIntValue("DelayDecoration"),
            };

            return extensionPropItem;
        }

        /// <summary>
        /// 加载缓存项
        /// </summary>
        public static void LoadCachingItems(SystemXmlItems systemExtensionProps)
        {
            Dictionary<int, ExtensionPropItem> cachingDict = new Dictionary<int, ExtensionPropItem>();
            foreach (var key in systemExtensionProps.SystemXmlItemDict.Keys)
            {
                SystemXmlItem systemXmlItem = systemExtensionProps.SystemXmlItemDict[(int)key];
                ExtensionPropItem extensionPropItem = ParseCachingItem(systemXmlItem);
                if (null == extensionPropItem) //解析出错
                {
                    continue;
                }

                cachingDict[extensionPropItem.ID] = extensionPropItem;
            }

            _ExtensionPropsCachingDict = cachingDict;
        }

        /// <summary>
        /// 触发拓展属性
        /// </summary>
        /// <param name="extensionPropsIDList"></param>
        /// <param name="skillID"></param>
        /// <returns></returns>
        public static List<int> ProcessExtensionProps(List<int> extensionPropsIDList, int skillID, int actionType)
        {
            List<int> list = new List<int>();
            if (null == extensionPropsIDList)
            {
                return list;
            }

            Dictionary<int, byte> dict = new Dictionary<int, byte>();

            for (int i = 0; i < extensionPropsIDList.Count; i++)
            {
                int id = extensionPropsIDList[i];
                ExtensionPropItem extensionPropItem = FindCachingItem(id);
                if (null == extensionPropItem)
                {
                    continue;
                }

                if (extensionPropItem.ActionType != actionType) //是否是要求触发的类型
                {
                    continue;
                }

                if (extensionPropItem.NeedSkill.Count > 0) //如果需要指定技能触发
                {
                    if (!extensionPropItem.NeedSkill.ContainsKey(skillID))
                    {
                        continue;
                    }
                }

                int rndNum = Global.GetRandomNumber(0, 101);
                if (rndNum > extensionPropItem.Probability) //判断触发概率
                {
                    continue;
                }

                list.Add(id);
                dict[id] = 1;
            }

            List<int> returnList = new List<int>();
            for (int i = 0; i < list.Count; i++)
            {
                int id = list[i];
                ExtensionPropItem extensionPropItem = FindCachingItem(id);
                if (null == extensionPropItem)
                {
                    continue;
                }

                if (extensionPropItem.PrevTuoZhanShuXing.Count > 0) //如果需要前置触发
                {
                    foreach (var key in extensionPropItem.PrevTuoZhanShuXing.Keys)
                    {
                        if (!dict.ContainsKey(key))
                        {
                            continue;
                        }
                    }
                }

                returnList.Add(id);
            }

            return returnList;
        }

        /// <summary>
        /// 对精灵对象执行拓展属性的公式
        /// </summary>
        /// <param name="list"></param>
        /// <param name="obj"></param>
        public static void ExecuteExtensionPropsActions(List<int> list, IObject self, IObject obj)
        {
            if (null == list || list.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                int id = list[i];
                ExtensionPropItem extensionPropItem = FindCachingItem(id);
                if (null == extensionPropItem)
                {
                    continue;
                }

                IObject targetObj = null;
                if (0 == extensionPropItem.ActionType) //主动触发
                {
                    targetObj = self;
                    if (0 != extensionPropItem.TargetType) //自身
                    {
                        targetObj = obj;
                    }
                }
                else //被动触发
                {
                    targetObj = obj;
                    if (0 != extensionPropItem.TargetType) //自身
                    {
                        targetObj = self;
                    }
                }

                List<MagicActionItem> magicActionItemList = null;
                if (GameManager.SystemMagicActionMgr.BossAIActionsDict.TryGetValue(extensionPropItem.ID, out magicActionItemList) && null != magicActionItemList)
                {
                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(self, targetObj, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams);
                    }
                }

                //通知所有在线用户某个精灵的扩展属性被命中(同一个地图才需要通知)
                GameManager.ClientMgr.NotifySpriteExtensionPropsHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            self, targetObj.GetObjectID(), (int)targetObj.CurrentPos.X, (int)targetObj.CurrentPos.Y, id);
            }
        }
    }
}
