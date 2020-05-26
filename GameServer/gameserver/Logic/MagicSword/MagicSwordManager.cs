using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Tools;
using Server.Data;
using Server.Protocol;
using GameServer.Server;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic.Spread;


namespace GameServer.Logic.MagicSword
{
    /// <summary>
    /// 魔剑士管理器 [XSea 2015/6/5]
    /// </summary>
    public class MagicSwordManager
    {
        #region 加载魔剑士静态数据
        /// <summary>
        /// 加载魔剑士静态数据 [XSea 2015/4/14]
        /// </summary>
        public void LoadMagicSwordData()
        {
            // 加载魔剑士静态数据
            string MagicSwordInitStr = ""; //读表
            string[] MagicSwordInitArr;
            try
            {
                // 魔剑士初始参数 
                MagicSwordInitStr = GameManager.systemParamsList.GetParamValueByName("MJSChuShi");
                // 解析
                MagicSwordInitArr = MagicSwordInitStr.Split('|');
                // 判断参数个数
                if (MagicSwordInitArr.Length != 5)
                {
                    LogManager.WriteLog(LogTypes.Error, "魔剑士静态数据有误，无法读取");
                    return;
                }
                // 解析转生与级数
                string[] tmpArr = MagicSwordInitArr[4].Split(',');
                if (tmpArr.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, "魔剑士静态数据转生与级数有误，无法读取");
                    return;
                }

                // 魔剑士初始任务id
                MagicSwordData.InitTaskID = int.Parse(MagicSwordInitArr[0]);
                // 魔剑士初始任务npcid
                MagicSwordData.InitTaskNpcID = int.Parse(MagicSwordInitArr[1]);
                // 魔剑士初始任务id之前的一个任务id
                MagicSwordData.InitPrevTaskID = int.Parse(MagicSwordInitArr[2]);
                // 魔剑士初始场景id
                MagicSwordData.InitMapID = int.Parse(MagicSwordInitArr[3]);
                // 魔剑士初始转生数
                MagicSwordData.InitChangeLifeCount = int.Parse(tmpArr[0]); ;
                // 魔剑士初始级数
                MagicSwordData.InitLevel = int.Parse(tmpArr[1]);

                // 存放力量魔剑士大天使武器
                if (null == MagicSwordData.StrengthWeaponList)
                    MagicSwordData.StrengthWeaponList = new List<int>();

                MagicSwordData.StrengthWeaponList.Clear();
                // 力量魔剑士大天使武器
                MagicSwordInitStr = GameManager.systemParamsList.GetParamValueByName("LiMJSDaTianShi");
                // 解析
                MagicSwordInitArr = MagicSwordInitStr.Split(',');
                // 将武器存起来
                if (MagicSwordInitArr.Length > 0)
                {
                    for (int i = 0; i < MagicSwordInitArr.Length; ++i)
                        MagicSwordData.StrengthWeaponList.Add(int.Parse(MagicSwordInitArr[i]));
                }

                // 存放智力魔剑士大天使武器
                if (null == MagicSwordData.IntelligenceWeaponList)
                    MagicSwordData.IntelligenceWeaponList = new List<int>();

                MagicSwordData.IntelligenceWeaponList.Clear();
                // 智力魔剑士大天使武器
                MagicSwordInitStr = GameManager.systemParamsList.GetParamValueByName("ZhiMJSDaTianShi");
                // 解析
                MagicSwordInitArr = MagicSwordInitStr.Split(',');
                // 将武器存起来
                if (MagicSwordInitArr.Length > 0)
                {
                    for (int i = 0; i < MagicSwordInitArr.Length; ++i)
                        MagicSwordData.IntelligenceWeaponList.Add(int.Parse(MagicSwordInitArr[i]));
                }

                // 力魔剑士普攻技能id
                MagicSwordData.StrAttackID = (int)GameManager.systemParamsList.GetParamValueIntByName("LiMJSAttackSkill");

                // 智魔剑士普攻技能id
                MagicSwordData.IntAttackID = (int)GameManager.systemParamsList.GetParamValueIntByName("ZhiMJSAttackSkill");
            }
            catch (Exception)
            {
                throw new Exception(string.Format("load xml file : {0} fail", string.Format("Config/SystemParams.xml-LoadMagicSwordData")));
            }
        }
        #endregion

        #region 初始化魔剑士信息
        /// <summary>
        /// 初始化魔剑士信息 [XSea 2015/4/14]
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="eType">0=力魔，1=智魔</param>
        public bool InitMagicSwordInfo(GameClient client, EMagicSwordTowardType eType)
        {
            // 检查版本是否开放魔剑士
            if (!IsVersionSystemOpenOfMagicSword())
                return false;

            // 判空
            if (null == client)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("client不存在，初始化魔剑士信息"));
                return false;
            }

            // 如果转生成功
            if (AutoUpChangeLifeAndLevel(client, MagicSwordData.InitChangeLifeCount, MagicSwordData.InitLevel))
            {
                AutoMaigcSwordFirstAddPoint(client, eType); // 加点 魔剑士职业分支
                AutoGiveMagicSwordGoods(client); // 发放新手装备
                AutoGiveMagicSwordDefaultSkillHotKey(client, eType); // 给技能栏默认技能
                GlobalEventSource.getInstance().fireEvent(new PlayerLevelupEventObject(client)); //触发玩家升级事件 创建竞技场数据
                return true;
            }
            return false;
        }
        #endregion

        #region 是否为魔剑士
        /// <summary>
        /// 是否为魔剑士
        /// </summary>
        /// <param name="client">角色</param>
        public bool IsMagicSword(GameClient client)
        {
            if (client == null)
                return false;

            return IsMagicSword(client.ClientData.Occupation);
        }

        /// <summary>
        /// 是否为魔剑士
        /// </summary>
        /// <param name="nOccu">职业</param>
        public bool IsMagicSword(int nOccu)
        {
            // 用原始职业对比
            //if ((EOccupationType)Global.CalcOriginalOccupationID(nOccu) == EOccupationType.EOT_MagicSword)
            //    return true;

            return false;
        }
        #endregion

        #region 是否为初次登录的魔剑士
        /// <summary>
        /// 是否为初次登录的魔剑士
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nDestChangeLifeCount">出生时要达到的转生数</param>
        public bool IsFirstLoginMagicSword(GameClient client, int nDestChangeLifeCount)
        {
            // 检查版本是否开放魔剑士
            if (!IsVersionSystemOpenOfMagicSword())
                return false;

            // 判空
            if (client == null)
                return false;

            // 不是魔剑士
            if (!IsMagicSword(client))
                return false;

            // 没有登录过的 或者 登录过但是意外的没有达到转生等级
            if (client.ClientData.LoginNum <= 0 || client.ClientData.ChangeLifeCount < nDestChangeLifeCount)
                return true;

            return false;
        }
        #endregion

        #region 是否为魔剑士可用的大天使武器
        /// <summary>
        /// 是否为魔剑士可用的大天使武器
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nGoodsID">武器ID</param>
        /// <returns></returns>
        public bool IsMagicSwordAngelWeapon(GameClient client, int nGoodsID)
        {
            // 判空
            if (null == client)
                return false;

            // 检查职业
            //if ((EOccupationType)client.ClientData.Occupation != EOccupationType.EOT_MagicSword)
            //    return false;

            // 力量魔剑士 or 智力魔剑士
            EMagicSwordTowardType eType = GetMagicSwordTowardType(client);

            List<int> tmpList; // 临时容器

            // 根据魔剑士类别 进行操作
            switch (eType)
            {
                case EMagicSwordTowardType.EMST_Strength: // 力量魔剑士
                    {
                        tmpList = MagicSwordData.StrengthWeaponList;
                        break;
                    }
                case EMagicSwordTowardType.EMST_Intelligence: // 智力魔剑士
                    {
                        tmpList = MagicSwordData.IntelligenceWeaponList;
                        break;
                    }
                default:
                    return false;
            }

            for (int i = 0; i < tmpList.Count; ++i)
            {
                // 如果找到符合的 返回
                if (nGoodsID == tmpList[i])
                    return true;
            }

            return false;
        }
        #endregion

        #region 是否为魔剑士武器
        /// <summary>
        /// 是否为魔剑士武器
        /// </summary>
        /// <param name="nGoodsID">物品ID</param>
        /// <returns></returns>
        public bool IsMagicSwordWeapon(int nGoodsID)
        {
            SystemXmlItem systemGoods = null;

            // 找静态表
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(nGoodsID, out systemGoods))
                return false;

            ItemCategories eCategoriy = (ItemCategories)systemGoods.GetIntValue("Categoriy"); // 种类

            bool bRes = false;
            // 如果在这范围内 是魔剑士武器
            switch (eCategoriy)
            {
                case ItemCategories.WuQi_Jian:
                case ItemCategories.WuQi_Fu:
                case ItemCategories.WuQi_Chui:
                case ItemCategories.WuQi_Mao:
                case ItemCategories.WuQi_Zhang:
                case ItemCategories.WuQi_Dun:
                case ItemCategories.WuQi_Dao:
                    bRes = true;
                    break;
            }

            return bRes;
        }
        #endregion

        #region 是否开放魔剑士
        /// <summary>
        /// 是否开放魔剑士 [XSea 2015/5/4]
        /// </summary>
        public bool IsVersionSystemOpenOfMagicSword()
        {
            if (!GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.MagicSword))
                return false;

            // 如果1.5的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot5))
            {
                return false;
            }

            return true;
        }
        #endregion

        #region 魔剑士释放技能检查
        /// <summary>
        /// 魔剑士释放技能检查 [XSea 2015/4/30]
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nMagicID">技能id</param>
        /// <returns></returns>
        public bool CanUseMagicOfMagicSword(GameClient client, int nMagicID)
        {
            // 判空
            if (null == client)
                return false;

            // 先检查职业是否为魔剑士
            if (!IsMagicSword(client))
                return true;

            // 检查版本是否开放魔剑士
            if (!IsVersionSystemOpenOfMagicSword())
                return false;

            // 找技能静态表
            SystemXmlItem systemMagics = null;
            if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(nMagicID, out systemMagics))
                return false;

            // 技能伤害类型 -1=无限制，1=物理，2=智力
            int nMagicDamageType = systemMagics.GetIntValue("DamageType");

            // 如果无限制则可以直接释放
            if (nMagicDamageType < 0)
                return true;

            // 普攻检查
            switch (nMagicDamageType)
            {
                case 1: // 物理伤害
                    // 看是否为力魔普攻，普攻不走这个检测
                    if (nMagicID == MagicSwordData.StrAttackID)
                        return true;
                    break;
                case 2: // 魔法伤害
                    // 看是否为智魔普攻，普攻不走这个检测
                    if (nMagicID == MagicSwordData.IntAttackID)
                        return true;
                    break;
            }

            // 最后看武器主属性是否对应技能伤害类型
            /**
             * 1.没有武器不能释放
             * 2.只装备一把武器则直接检查
             * 3.如果装备两把武器则 先检查右手武器，如果没有再检查左手武器
             * 4.规则为武器的所需主属性对应技能表的伤害类型DamageType 1=物理，2=魔法
             * 5.BagIndex解释：0=右手（主手），1=左右（副手）
             */

            // 武器列表
            List<GoodsData> WeaponList = client.UsingEquipMgr.GetWeaponEquipList();
            lock (WeaponList)
            {
                // 没有装备武器 不能释放魔剑士技能
                if (null == WeaponList || WeaponList.Count <= 0)
                    return false;

                GoodsData goods = null; // 武器

                // 只装备了一把武器
                if (WeaponList.Count == 1)
                {
                    goods = WeaponList[0]; // 直接取出来
                }
                else if (WeaponList.Count > 1) // 装备了两把武器
                {
                    // 优先检查右手武器
                    for (int i = 0; i < WeaponList.Count; ++i)
                    {
                        // 右手（主手）
                        if (WeaponList[i].BagIndex == 0)
                        {
                            goods = WeaponList[i]; // 取出武器
                            break;
                        }
                    }

                    // 没有的话再找左手
                    if (null == goods)
                    {
                        for (int i = 0; i < WeaponList.Count; ++i)
                        {
                            // 左手（副手）
                            if (WeaponList[i].BagIndex == 1)
                            {
                                goods = WeaponList[i]; // 取出武器
                                break;
                            }
                        }
                    }
                }

                // 如果没找到，说明有问题。。
                if (null == goods)
                    return false;

                // 规则为武器的所需主属性对应技能表的伤害类型DamageType

                // 找物品静态表
                SystemXmlItem systemGoods = null;
                if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goods.GoodsID, out systemGoods))
                    return false;

                // 武器所需属性
                int nStrength = systemGoods.GetIntValue("Strength"); // 力量
                int nIntelligence = systemGoods.GetIntValue("Intelligence"); // 智力

                // 武器 力量大于等于智力 为物理，否则为魔法 1=物理，2=魔法
                int nWeaponDamageType = nStrength >= nIntelligence ? 1 : 2;

                // 如果武器类型与技能类型相同
                if (nWeaponDamageType == nMagicDamageType)
                    return true;

                LogManager.WriteLog(LogTypes.Warning,
                    string.Format("武器与技能类型不符，无法释放技能: RoleID={0}, 武器id{1}, 武器类型{2}, 技能id{3}, 技能类型{4}",
                    client.ClientData.RoleID, goods.GoodsID, nWeaponDamageType, nMagicID, nMagicDamageType));

                return false;
            }
        }
        #endregion

        #region 魔剑士奖励的过滤接口
        /// <summary>
        /// 魔剑士奖励的过滤接口
        /// </summary>
        /// <returns></returns>
        public bool IsCanAward2MagicSword(GameClient client, int nGoodsID)
        {
            int nOcc = Global.CalcOriginalOccupationID(client); // 角色职业

            // 不是魔剑士
            if (!IsMagicSword(nOcc))
                return false;

            // 物品职业走向
            SystemXmlItem systemGoods = null;
            if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(nGoodsID, out systemGoods))
                return false;

            // -1等于物品无限制 通用
            if (Global.GetMainOccupationByGoodsID(nGoodsID) == -1)
                return true;

            // 魔剑士职业走向
            EMagicSwordTowardType eEMSType = GetMagicSwordTowardType(client);

            int nStrength = systemGoods.GetIntValue("Strength"); // 力量
            int nIntelligence = systemGoods.GetIntValue("Intelligence"); // 智力

            EMagicSwordTowardType eEMSGoodType = EMagicSwordTowardType.EMST_Intelligence;

            if (nStrength >= nIntelligence)
                eEMSGoodType = EMagicSwordTowardType.EMST_Strength;

            if (eEMSType == eEMSGoodType)
                return true;

            return false;
        }
        #endregion

        #region 魔剑士职业走向类型
        /// <summary>
        /// 魔剑士职业走向类型
        /// </summary>
        public EMagicSwordTowardType GetMagicSwordTowardType(GameClient client)
        {
            //       判断标准： 1.力量 > 智力 => 力魔剑
            //                  2.力量 < 智力 => 智魔剑
            //                  3.力量 = 智力 => 力魔剑

            double meStrength = RoleAlgorithm.GetStrength(client);
            double meIntelligence = RoleAlgorithm.GetIntelligence(client);

            if (meStrength >= meIntelligence) return EMagicSwordTowardType.EMST_Strength;
            return EMagicSwordTowardType.EMST_Intelligence;
        }
        #endregion

        #region 自动给予魔剑士物品
        /// <summary>
        /// 自动给予魔剑士物品
        /// </summary>
        public void AutoGiveMagicSwordGoods(GameClient client)
        {
            if (null == client)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("client不存在，服务器无法给与魔剑士新手装备"));
                return;
            }

            // 不是魔剑士
            if (!IsMagicSword(client))
                return;

            int nRoleID = client.ClientData.RoleID; // 角色id

            try
            {
                List<List<int>> giveEquip = null;
                // 力魔剑
                if (EMagicSwordTowardType.EMST_Strength == (EMagicSwordTowardType)GetMagicSwordTowardType(client))
                {
                    giveEquip = ConfigParser.ParserIntArrayList(GameManager.systemParamsList.GetParamValueByName("LiMJSZhuangBei"));
                    if (null == giveEquip)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备默认数据报错.RoleID{0}", nRoleID));
                        return;
                    }

                    if (giveEquip.Count <= 0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备数量为空.RoleID{0}", nRoleID));
                        return;
                    }
                }
                else // 智魔剑
                {
                    giveEquip = ConfigParser.ParserIntArrayList(GameManager.systemParamsList.GetParamValueByName("ZhiMJSZhuangBei"));
                    if (null == giveEquip)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备默认数据报错.RoleID{0}", nRoleID));
                        return;
                    }

                    if (giveEquip.Count <= 0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备数量为空.RoleID{0}", nRoleID));
                        return;
                    }
                }
                // 戒指标识0=一个都没装，1=装了一个
                bool bRingFalg = false;
                // Give Equip
                for (int i = 0; i < giveEquip.Count; i++)
                {
                    int nGoodID = giveEquip[i][0];  // 物品id
                    int nNum = giveEquip[i][1]; // 数量
                    int nBind = giveEquip[i][2]; // 是否绑定
                    int nIntensify = giveEquip[i][3]; // 强化
                    int nAppendPropLev = giveEquip[i][4]; // 追加
                    int nLuck = giveEquip[i][5]; // 幸运
                    int nExcellence = giveEquip[i][6]; // 卓越

                    SystemXmlItem sytemGoodsItem = null;
                    if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(nGoodID, out sytemGoodsItem))
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备数量ID不存在:RoleID{0},GoodsID={1}", nRoleID, nGoodID));
                        continue;
                    }

                    // [4/15/2015 chdeng]
                    if (!Global.IsRoleOccupationMatchGoods(client, nGoodID))
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备与职业不符RoleID{0}, 物品id{1}.", nRoleID, nGoodID));
                        continue;
                    }

                    //                     int toOccupation = sytemGoodsItem.GetIntValue("ToOccupation");
                    //                     if (toOccupation >= 0)
                    //                     {
                    //                         int nOcc = Global.CalcOriginalOccupationID(client);
                    // 
                    //                         if (nOcc != toOccupation)
                    //                         {
                    //                             continue;
                    //                         }
                    //                     }

                    if (1 != nNum)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备数量必须为1件RoleID{0}, 数量{1}.", nRoleID, nNum));
                        continue;
                    }

                    //  给装备(装备nNum 必须为1)
                    int nSeriralID = Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, nGoodID/*ID*/, nNum/*数量*/, 0/*品级*/, ""/*品质的随机属性*/, nIntensify/*锻造级别*/,
                        nBind/*绑定*/, 0/*容器类型*/, ""/*镶嵌的宝石物品ID列表*/, false/*是否允许重用旧的格子*/, 1/*????*/, "自动给于魔剑士装备"/*from where*/,
                        Global.ConstGoodsEndTime/*end time */, 0/*精锻级别*/, 0/*天生属性的百分比*/, nLuck/*幸运*/, 0/*装备耐久度*/, nExcellence/*卓越*/, nAppendPropLev/*追加属性*/);

                    // 失败
                    if (nSeriralID <= 0)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备数量[AddGoodsDBCommand]失败.RoleID{0}", nRoleID));
                        continue;
                    }

                    GoodsData newEquip = Global.GetGoodsByDbID(client, nSeriralID);

                    if (null == newEquip)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备数量[GetGoodsByID]失败.RoleID{0}", nRoleID));
                        continue;
                    }
                    int nBagIndex = 0; // 要装备位置的索引
                    int nCatetoriy = Global.GetGoodsCatetoriy(newEquip.GoodsID); // 获取装备类别

                    // 如果是戒指 并且以装备一个了，就装在另一个格子里
                    if (nCatetoriy == (int)ItemCategories.JieZhi && bRingFalg)
                        nBagIndex++;

                    // 穿上[注意参数5：和当前期装备使用状态反过来，否则会被外挂]
                    String cmdData = "";
                    cmdData = String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, (int)ModGoodsTypes.EquipLoad,
                                newEquip.Id, newEquip.GoodsID, 1, newEquip.Site, newEquip.GCount, nBagIndex, "");

                    TCPProcessCmdResults eErrorCode = Global.ModifyGoodsByCmdParams(client, cmdData);
                    if (TCPProcessCmdResults.RESULT_FAILED == eErrorCode)
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始化装备数量[ModifyGoodsByCmdParams]失败.RoleID{0}", nRoleID));
                    }
                    else
                    {
                        Global.RefreshEquipProp(client, newEquip);
                        // 戒指比较特别，需要分别装备到左右两个格子
                        if (nCatetoriy == (int)ItemCategories.JieZhi)
                            bRingFalg = true; // 以装备一个
                    }
                }
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }
        }
        #endregion

        #region 自动给与魔剑士默认技能热键
        /// <summary>
        /// 自动给与魔剑士默认技能热键 [XSea 2015/6/5]
        /// </summary>
        /// <param name="client"></param>
        /// <param name="eType">职业类型，0=力魔，1=智魔</param>
        public void AutoGiveMagicSwordDefaultSkillHotKey(GameClient client, EMagicSwordTowardType eType)
        {
            if (null == client)
                return;

            /*技能选择：
                中间大按钮是两个职业的普通攻击技能（一段ID：10000、10100）
                力魔其他技能：10104、10106、10101
                法魔其他技能：10004、10006、10001
             */
            string skillStr = "";
            switch (eType)
            {
                case EMagicSwordTowardType.EMST_Strength: // 力魔
                    skillStr = string.Format("0@{0}|0@{1}|0@{2}|0@{3}", 10004, 10000, 10006, 10001);
                    break;
                case EMagicSwordTowardType.EMST_Intelligence: // 智魔
                    skillStr = string.Format("0@{0}|0@{1}|0@{2}|0@{3}", 10104, 10100, 10106, 10101);
                    break;
                default:
                    return;
            }

            string cmdStr = string.Format("{0}:{1}:{2}", client.ClientData.RoleID, 0, skillStr);

            //修改内存记录
            client.ClientData.MainQuickBarKeys = skillStr;

            //通知数据库修改
            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATEKEYS, cmdStr, null, client.ServerId);
        }
        #endregion

        #region 根据转生次数与等级一次性驱动角色升级与转生
        /// <summary>
        /// 根据转生次数与等级一次性驱动角色升级与转生 [XSea 2015/4/7]
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="nDsetChangeLifeCount">目标转生数(要升到几转)</param>
        /// <param name="nDestLevel">目标级数(要升到几级)</param>
        public bool AutoUpChangeLifeAndLevel(GameClient client, int nDestChangeLifeCount, int nDestLevel)
        {
            // 判空
            if (null == client)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("client不存在，服务器无法驱动转生"));
                return false;
            }

            int nRoleID = client.ClientData.RoleID; // 角色id

            try
            {
                // 当前等级
                int nCurLevel = client.ClientData.Level;

                // 当前转生次数
                int nCurChangeLifeCount = client.ClientData.ChangeLifeCount;

                // 这里防止一下创角转生后立即掉线未成功进入游戏,则不再执行驱动升级
                if (client.ClientData.LoginNum <= 0 && (nCurChangeLifeCount >= nDestChangeLifeCount && nCurLevel >= nDestLevel))
                    return false;

                // 等级不可小于0且不可超上限
                if (nDestLevel <= 0 && nDestLevel > Data.LevelUpExperienceList.Length - 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("等级越界，服务器无法驱动转生，RoleID{0}", nRoleID));
                    return false;
                }

                // 转生次数上限
                int nValue = (int)GameManager.systemParamsList.GetParamValueIntByName("ChangeLifeMaxValue");

                // 转生不可超上限
                if (nDestChangeLifeCount > nValue) //GameManager.ChangeLifeMgr.m_MaxChangeLifeCount 10转
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("转生次数已达上限，服务器无法驱动转生，RoleID{0}", nRoleID));
                    return false;
                }

                // 要转生的次数不可低于当前转生次数
                if (nDestChangeLifeCount < nCurChangeLifeCount)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("要转生次数低于当前转生次数，服务器无法驱动转生，RoleID{0}", nRoleID));
                    return false;
                }

                // 如果要转生的次数与当前转生次数相等，则要升到的级数必须大于当前级数
                if (nDestChangeLifeCount == nCurChangeLifeCount && nDestLevel <= nCurLevel)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("转生次数相同，但目标等级低于当前等级，服务器无法驱动转生，RoleID{0}", nRoleID));
                    return false;
                }

                // 执行升级 循环直至转生次数与等级达到目标
                while (client.ClientData.ChangeLifeCount < nDestChangeLifeCount || client.ClientData.Level < nDestLevel)
                {
                    // 如果当前转生等级未达到
                    if (client.ClientData.ChangeLifeCount < nDestChangeLifeCount)
                    {
                        int nTmpLv = client.ClientData.Level; // 当前等级
                        // 升至顶级
                        for (int i = nTmpLv; i < Data.LevelUpExperienceList.Length - 1; ++i)
                        {
                            long lNeedExp = GameManager.ClientMgr.GetCurRoleLvUpNeedExp(client); // 获取升级所需经验
                            if (lNeedExp > 0)
                                GameManager.ClientMgr.ProcessRoleExperience(client, lNeedExp); // 加经验
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error, string.Format("经验表数据错误，升级所需经验为0，服务器无法驱动转生，RoleID{0}", nRoleID));
                                return false;
                            }
                        }

                        // 执行转生
                        bool bFlag = ProcessChangeLifeByServer(client);

                        if (!bFlag)
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("执行转身过程中出现错误，服务器无法驱动转生，RoleID{0}", nRoleID));
                            return false;
                        }
                    }
                    else // 转生次数已达目标，则直接检查等级是否已达目标
                    {
                        // 如果等级未达目标
                        if (client.ClientData.Level < nDestLevel)
                        {
                            int nTmpLv = client.ClientData.Level; // 当前等级
                            // 升至目标
                            for (int i = nTmpLv; i < nDestLevel; ++i)
                            {
                                long lNeedExp = GameManager.ClientMgr.GetCurRoleLvUpNeedExp(client); // 获取升级所需经验
                                if (lNeedExp > 0)
                                    GameManager.ClientMgr.ProcessRoleExperience(client, lNeedExp); // 加经验
                                else
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("经验表数据错误，升级所需经验为0，服务器无法驱动转生，RoleID{0}", nRoleID));
                                    return false;
                                }
                            }
                        }
                    }
                }
                // 通知DB对等级经验进行保存
                GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATE_EXPLEVEL,
                        string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.Level, client.ClientData.Experience), null, client.ServerId);

                return true;
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }
            return false;
        }
        #endregion

        #region 根据武器获魔剑士类型
        /// <summary>
        /// 根据武器获魔剑士类型 [XSea 2015/5/18]
        /// </summary>
        /// <param name="nOccu">职业</param>
        /// <param name="list">物品列表</param>
        /// <returns>EMagicSwordTowardType = 0力魔，=1智魔</returns>
        public EMagicSwordTowardType GetMagicSwordTypeByWeapon(int nOccu, List<GoodsData> list)
        {
            lock (list)
            {
                EMagicSwordTowardType eType = EMagicSwordTowardType.EMST_Strength; // 默认0= 力量类型

                // 不是魔剑士
                if (!IsMagicSword(nOccu))
                    return EMagicSwordTowardType.EMST_Not;

                // 判空
                if (null == list || list.Count <= 0)
                    return eType;

                GoodsData goodsData = null; // 物品、武器
                SystemXmlItem systemGoods = null; // 静态表物品
                List<GoodsData> WeaponList = new List<GoodsData>(); // 武器列表
                //先筛选 武器
                for (int i = 0; i < list.Count; ++i)
                {
                    goodsData = list[i];
                    // 不为空
                    if (null != goodsData)
                    {
                        // 找物品静态表
                        if (!GameManager.SystemGoods.SystemXmlItemDict.TryGetValue(goodsData.GoodsID, out systemGoods))
                            return eType;

                        ItemCategories eCategoriy = (ItemCategories)systemGoods.GetIntValue("Categoriy"); // 种类

                        // 如果在这范围内 是武器
                        switch (eCategoriy)
                        {
                            case ItemCategories.WuQi_Jian:
                            case ItemCategories.WuQi_Fu:
                            case ItemCategories.WuQi_Chui:
                            case ItemCategories.WuQi_Gong:
                            case ItemCategories.WuQi_Nu:
                            case ItemCategories.WuQi_Mao:
                            case ItemCategories.WuQi_Zhang:
                            case ItemCategories.WuQi_Dun:
                            case ItemCategories.WuQi_Dao:
                                // 如果装备中
                                if (goodsData.Using > 0)
                                    WeaponList.Add(goodsData); //加入武器列表
                                break;
                        }
                    }
                }

                // 没有装备武器 默认物理类型
                if (null == WeaponList || WeaponList.Count <= 0)
                    return eType;

                goodsData = null; // 重置为空

                // 只装备了一把武器
                if (WeaponList.Count == 1)
                {
                    goodsData = WeaponList[0]; // 直接取出来
                }
                else if (WeaponList.Count > 1) // 装备了两把武器
                {
                    // 优先检查右手武器
                    for (int i = 0; i < WeaponList.Count; ++i)
                    {
                        // 右手（主手）
                        if (WeaponList[i].BagIndex == 0)
                        {
                            goodsData = WeaponList[i]; // 取出武器
                            break;
                        }
                    }

                    // 没有的话再找左手
                    if (null == goodsData)
                    {
                        for (int i = 0; i < WeaponList.Count; ++i)
                        {
                            // 左手（副手）
                            if (WeaponList[i].BagIndex == 1)
                            {
                                goodsData = WeaponList[i]; // 取出武器
                                break;
                            }
                        }
                    }
                }

                // 如果没找到，默认物理类型
                if (null == goodsData)
                    return eType;

                // 武器所需属性
                int nStrength = systemGoods.GetIntValue("Strength"); // 力量
                int nIntelligence = systemGoods.GetIntValue("Intelligence"); // 智力

                // 武器 力量大于等于智力 为物理，否则为魔法 1=物理，2=魔法
                eType = nStrength >= nIntelligence ? EMagicSwordTowardType.EMST_Strength : EMagicSwordTowardType.EMST_Intelligence;

                return eType;
            }
        }
        #endregion

        #region 根据参数表配置第一次给魔剑士加点
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client">角色</param>
        /// <param name="eType">职业类型，0=力魔，1=智魔</param>
        /// <returns>[结果=0成功,结果!=0失败]</returns>
        public void AutoMaigcSwordFirstAddPoint(GameClient client, EMagicSwordTowardType eType)
        {
            // 角色不存在
            if (null == client)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("client不存在，服务器无法根据参数表配置第一次给魔剑士加点"));
                return;
            }

            // 职业不是魔剑士
            if (!IsMagicSword(client))
                return;

            // 职业不是力魔或智魔
            if (eType != EMagicSwordTowardType.EMST_Strength && eType != EMagicSwordTowardType.EMST_Intelligence)
                return;

            // 角色id
            int nRoleID = client.ClientData.RoleID;

            try
            {
                // 读表
                string MagicSwordInitAttrStr = "";
                string[] MagicSwordInitAttrArr;

                // 魔剑士初始参数 
                if (eType == EMagicSwordTowardType.EMST_Strength)
                    MagicSwordInitAttrStr = GameManager.systemParamsList.GetParamValueByName("LiMJS");
                else
                    MagicSwordInitAttrStr = GameManager.systemParamsList.GetParamValueByName("ZhiMJS");

                // 解析
                MagicSwordInitAttrArr = MagicSwordInitAttrStr.Split(',');

                // 判断参数个数
                if (MagicSwordInitAttrArr.Length != 4)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士读取初始加点失败，无法创建魔剑士, RoleID={0}", nRoleID));
                    return;
                }

                int nPoint = 0; // 要分配的总点数
                for (int i = 0; i < MagicSwordInitAttrArr.Length; ++i)
                    nPoint += int.Parse(MagicSwordInitAttrArr[i]);

                int nRemainPoint = 0; // 剩余点数
                int nTotal = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint); // 总点数
                int nStrength = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropStrength); // 力量点数
                int nIntelligence = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropIntelligence); // 智力点数
                int nDexterity = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropDexterity); // 敏捷点数
                int nConstitution = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropConstitution); // 体力点数

                // 总点数减去已分配的 就是剩余点数
                nRemainPoint = nTotal - nStrength - nIntelligence - nDexterity - nConstitution;

                // 剩余点数不足
                if (nRemainPoint < nPoint)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("魔剑士初始加点不足，无法创建魔剑士, RoleID={0}", nRoleID));
                    return;
                }

                // 力量
                client.ClientData.PropStrength += int.Parse(MagicSwordInitAttrArr[0]);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropStrength, client.ClientData.PropStrength, true);

                // 智力
                client.ClientData.PropIntelligence += int.Parse(MagicSwordInitAttrArr[1]);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropIntelligence, client.ClientData.PropIntelligence, true);

                // 敏捷
                client.ClientData.PropDexterity += int.Parse(MagicSwordInitAttrArr[2]);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropDexterity, client.ClientData.PropDexterity, true);

                // 体力
                client.ClientData.PropConstitution += int.Parse(MagicSwordInitAttrArr[3]);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropConstitution, client.ClientData.PropConstitution, true);

                // 血上限
                client.ClientData.LifeV = (int)RoleAlgorithm.GetMaxLifeV(client);

                // 蓝上限
                client.ClientData.MagicV = (int)RoleAlgorithm.GetMaxMagicV(client);

                // 当前血量
                if (client.ClientData.CurrentLifeV > client.ClientData.LifeV)
                    client.ClientData.CurrentLifeV = client.ClientData.LifeV;

                // 当前蓝量
                if (client.ClientData.CurrentMagicV > client.ClientData.MagicV)
                    client.ClientData.CurrentMagicV = client.ClientData.MagicV;

                // 刷新装备属性
                //Global.RefreshEquipProp(client); 这里加的角色基础属性，应该不用刷新装备属性吧

                // 通知属性改变
                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 通知自己血量改变
                GameManager.ClientMgr.NotifySelfLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
                //throw ex;
                //});
            }
        }
        #endregion
        
        #region 服务器驱动转生
        /// <summary>
        /// 服务器驱动转生(目前用于“根据转生次数与等级一次性驱动角色升级与转生”)
        /// </summary>
        /// <param name="client">角色</param>
        public bool ProcessChangeLifeByServer(GameClient client)
        {
            try
            {
                // 判空
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("client不存在，服务器无法驱动转生"));
                    return false;
                }

                // 角色id
                int roleID = client.ClientData.RoleID;

                // 检查转生次数
                int nValue = (int)GameManager.systemParamsList.GetParamValueIntByName("ChangeLifeMaxValue");
                if (client.ClientData.ChangeLifeCount >= nValue)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("已达到最高转生等级，服务器无法驱动转生{0}", roleID));
                    return false;
                }

                // 转生计数
                int nChangeCount = client.ClientData.ChangeLifeCount + 1;

                Dictionary<int, ChangeLifeDataInfo> tmpDic = new Dictionary<int, ChangeLifeDataInfo>();

                if (!GameManager.ChangeLifeMgr.m_ChangeLifeInfoList.TryGetValue(client.ClientData.Occupation, out tmpDic) || tmpDic == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("配置错误1，服务器无法驱动转生{0}", roleID));
                    return false;
                }

                ChangeLifeDataInfo temChagLifeInfo = new ChangeLifeDataInfo();

                if (!tmpDic.TryGetValue(nChangeCount, out temChagLifeInfo) || temChagLifeInfo == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("配置错误2，服务器无法驱动转生{0}", roleID));
                    return false;
                }

                // 检测级别需求
                int nLev = client.ClientData.Level;
                if (nLev < temChagLifeInfo.NeedLevel)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("等级不足，服务器无法驱动转生{0}", roleID));
                    return false;
                }

                /*GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_EXECUTECHANGELIFE,
                        string.Format("{0}:{1}", client.ClientData.RoleID, nChangeCount),
                        null);*/

                // 通知DB
                TCPOutPacket tcpOutPacket = null;
                string strDbCmd = string.Format("{0}:{1}", roleID, nChangeCount);
                TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer2(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                                                                (int)TCPGameServerCmds.CMD_SPR_EXECUTECHANGELIFE, strDbCmd, out tcpOutPacket, client.ServerId);

                if (TCPProcessCmdResults.RESULT_FAILED == dbRequestResult)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("与DBServer通讯失败, 服务器无法驱动转生{0}:CMD={1}", roleID, (int)TCPGameServerCmds.CMD_SPR_EXECUTECHANGELIFE));
                    return false;
                }

                // 接收DB返回信息
                string strData = new UTF8Encoding().GetString(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);

                // 还回
                Global.PushBackTcpOutPacket(tcpOutPacket);

                //解析指令
                string[] fieldsData = strData.Split(':');

                if (fieldsData[1] == "-1")
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("转生时 DBServer发生错误, 服务器无法驱动转生{0}:CMD={1}", roleID, (int)TCPGameServerCmds.CMD_SPR_EXECUTECHANGELIFE));
                    return false;
                }

                ++client.ClientData.ChangeLifeCount;

                // 保存经验
                long nExperienceNow = client.ClientData.Experience;
#if false
                // 根据当前转生计数 以及当前的等级 计算出经验值
                long nExperience = 0;
                int nLevelNow = client.ClientData.Level;
                int nLevelNeed = temChagLifeInfo.NeedLevel;

                if (nLevelNow > nLevelNeed)
                {
                    for (int i = nLevelNeed; i < nLevelNow; ++i)
                        nExperience += Data.LevelUpExperienceList[i];
                }

                // 加上现有的经验值
                nExperience += nExperienceNow;

                // 设置等级
                //client.ClientData.Level = 1;
                client.ClientData.Level = 80;

                // 设置 "升级获得的属性点" 因为它在转生时要扣除
                int nPropPointForLevelUp = Global.GetRoleParamsInt32FromDB(client, RoleParamName.AddProPointForLevelUp);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.AddProPointForLevelUp, 0);

                // 转生成功 -- DB保存住转生计数
                //Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sChangeLifeCount, nChangeCount, true);

                // 奖励属性点
                int nOldPoint = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint);
                int nNewPoint = nOldPoint + temChagLifeInfo.AwardPropPoint - nPropPointForLevelUp;  // 注意 - 扣除升级获得的属性点

                // 清空分配出去的属性点
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropStrength, 0, true);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropIntelligence, 0, true);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropDexterity, 0, true);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropConstitution, 0, true);
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TotalPropPoint, nNewPoint);

                client.ClientData.TotalPropPoint    = nNewPoint;
                client.ClientData.PropStrength      = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropStrengthChangeless);
                client.ClientData.PropIntelligence  = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropIntelligenceChangeless);
                client.ClientData.PropDexterity     = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropDexterityChangeless);
                client.ClientData.PropConstitution  = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropConstitutionChangeless);

                // 转生后 等级直接变成80级 增加属性点
                {
                    ChangeLifeAddPointInfo tmpChangeAddPointInfo = new ChangeLifeAddPointInfo();
                    tmpChangeAddPointInfo = Data.ChangeLifeAddPointInfoList[client.ClientData.ChangeLifeCount];

                    // 奖励属性点
                    int nOldPoint1 = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint);
                    int nNewPoint1 = 0;
                    nNewPoint1 = 79 * tmpChangeAddPointInfo.AddPoint + nOldPoint1;
                    client.ClientData.TotalPropPoint = nNewPoint1;

                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TotalPropPoint, nNewPoint1, true);

                    int nPoint = Global.GetRoleParamsInt32FromDB(client, RoleParamName.AddProPointForLevelUp);
                    nNewPoint1 = 0;
                    nNewPoint1 = 79 * tmpChangeAddPointInfo.AddPoint + nPoint;
                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.AddProPointForLevelUp, nNewPoint1, true);
                }

                // 根据经验值 重新计数等级
                client.ClientData.Experience = 0; // 注意--清零操作 用刚才取得的经验值nExperience去做重新升级的计算
                if (nExperience <= 0)
                {
                    GameManager.ClientMgr.NotifySelfExperience(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, nExperience);
                }
                else
                {
                    GameManager.ClientMgr.ProcessRoleExperience(client, nExperience, false);
                }

                // 奖励属性点
                int nOldPoint = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint);
                int nNewPoint = nOldPoint + temChagLifeInfo.AwardPropPoint;
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TotalPropPoint, nNewPoint, true);
                client.ClientData.TotalPropPoint = nNewPoint;

                nOldPoint = Global.GetRoleParamsInt32FromDB(client, RoleParamName.AddProPointForLevelUp);
                nNewPoint = nOldPoint * temChagLifeInfo.AwardPropPoint;
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.AddProPointForLevelUp, nNewPoint, true);
#else
                client.ClientData.Level = 1;
                client.ClientData.Experience = 0; // 注意--清零操作 用刚才取得的经验值nExperience去做重新升级的计算
                HuodongCachingMgr.ProcessGetUpLevelGift(client); // 获取礼包

                // 没有剩余经验 则 直接通知客户端
                if (nExperienceNow <= 0)
                {
                    GameManager.ClientMgr.NotifySelfExperience(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, nExperienceNow);
                }
                else // 有剩余经验则继续执行升级
                {
                    GameManager.ClientMgr.ProcessRoleExperience(client, nExperienceNow, false);
                }
#endif
                GameManager.ChangeLifeMgr.InitPlayerChangeLifePorperty(client);

                GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                //通知组队中的其他队员自己的级别发生了变化
                // GameManager.ClientMgr.NotifyTeamUpLevel(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                // 总生命值和魔法值变化通知(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, true, true);

                //自动学习技能
                Global.AutoLearnSkills(client);

                // 广播
                /*if (nChangeCount >= 2)
                    Global.BroadcastChangeLifeSuccess(client, nChangeCount);*/

                // 奖励物品
                if (temChagLifeInfo.AwardGoodsDataList != null)
                {
                    for (int i = 0; i < temChagLifeInfo.AwardGoodsDataList.Count; ++i)
                    {
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, temChagLifeInfo.AwardGoodsDataList[i].GoodsID, temChagLifeInfo.AwardGoodsDataList[i].GCount,
                                                        0, "", 0, 1, 0, "", true, 1, "转生奖励物品");
                    }
                }

                // 成就
                ChengJiuManager.OnRoleChangeLife(client);

                SpreadManager.getInstance().SpreadIsLevel(client);

                FundManager.FundChangeLife(client);

                // 每日活跃 [2/26/2014 LiaoWei]
                //DailyActiveManager.ProcessDailyActiveChangeLife(client);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        

        #endregion 服务器驱动转生
    }
}
