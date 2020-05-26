using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Net.Sockets;
using Server.Protocol;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System.Windows;
//using System.Windows.Documents;
using GameServer.Server;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// lua 脚本管理
    /// </summary>
    public class LuaManager
    {
        #region 测试?

        public String GetUserName(String s)
        {
            return String.Format("对象_{0}", s);
        }

        #endregion 测试?

        #region 字符串转换辅助

        /// <summary>
        /// 转换错误的lua字符串
        /// </summary>
        /// <param name="luaString"></param>
        /// <returns></returns>
        private string ConvertLuaString(string luaString)
        {
            /*byte[] bytes = new byte[luaString.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)luaString[i];
            }

            Encoding ed = Encoding.Default;
            luaString = ed.GetString(bytes, 0, bytes.Length);

            byte[] bytes2 = new byte[luaString.Length];
            for (int i = 0; i < bytes2.Length; i++)
            {
                bytes2[i] = (byte)luaString[i];
            }*/

            return luaString;
        }

        #endregion 字符串转换辅助

        #region 地图传送管理

        /// <summary>
        /// 传送到地图,toMapCode 小于0 或者等于当前地图id，则进行本地图移动
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toMapCode"></param>
        /// <returns></returns>
        public Boolean GotoMap(GameClient client, int toMapCode, int toPosX = -1, int toPosY = -1, int direction = -1)
        {
            Boolean ret = false;

            //传送到某个地图上去
            if (null != client)
            {
                if (client.ClientData.CurrentLifeV <= 0) //如果已经死亡，则不允许传送
                {
                    return ret;
                }

                int oldMapCode = client.ClientData.MapCode;

                if (JunQiManager.GetLingDiIDBy2MapCode(client.ClientData.MapCode) == (int)LingDiIDs.HuangCheng) //如果现在是皇城，则判断是否收回舍利
                {
                    //处理拥有皇帝特效的角色离开皇城地图，而失去皇帝特效的事件
                    HuangChengManager.HandleLeaveMapHuangDiRoleChanging(client);
                }

                if (!Global.CanEnterIfMapIsGuMu(client, toMapCode))//古墓挂机地图过滤判断
                {
                    return false;
                }

                //如果小于0，则回到当前地图
                if (toMapCode < 0) //回当前图
                {
                    GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, toPosX, toPosY, direction);
                }
                else
                {
                    bool execGoToMap = true;
                    if (JunQiManager.GetLingDiIDBy2MapCode(toMapCode) > 0 && toMapCode != GameManager.MainMapCode) //如果是要进入领地的地图
                    {
                        /*
                        if (Global.GetBangHuiFightingLineID() != GameManager.ServerLineID)
                        {
                            execGoToMap = false;

                            string mapName = Global.GetMapName(toMapCode);
                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                client, StringUtil.substitute(Global.GetLang("只有从『{0}』线才能进入『{1}』地图"), Global.GetBangHuiFightingLineID(), mapName),
                                GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                        }
                        */
                    }

                    if (execGoToMap)
                    {
                        GameMap gameMap = null;
                        if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                        {
                            ret = true;

                            //如果不同地图，就进行地图切换传送
                            if (oldMapCode != toMapCode)
                            {
                                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, toMapCode, toPosX, toPosY, direction);
                            }
                            else
                            {
                                //如果在同一个地图，就进行位置移动
                                GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, toPosX, toPosY, direction);
                            }
                            //保存到数据库
                            Global.UpdateDayActivityEnterCountToDB(client, client.ClientData.RoleID, TimeUtil.NowDateTime().DayOfYear, (int)SpecialActivityTypes.OldBattlefield, 1);
                        }
                    }
                }
            }

            return ret;
        }

        #endregion 地图传送管理

        #region 动态召唤怪物

        /// <summary>
        /// 添加一个怪物到玩家所在地图，如果玩家在副本，则添加到所在副本
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monsterID"></param>
        /// <param name="addNum"></param>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public Boolean AddDynamicMonsters(GameClient client, int monsterID, int addNum, int gridX, int gridY, int radius)
        {
            GameManager.MonsterZoneMgr.AddDynamicMonsters(client.ClientData.MapCode, monsterID, client.ClientData.CopyMapID, addNum, gridX, gridY, radius); 
            return true;
        }

        /// <summary>
        /// 为角色召唤宠物怪， callAsType必须为 DSPetMonster否则召唤出来的怪是野外怪
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monsterID"></param>
        /// <param name="callAsType"></param>
        /// <param name="callNum"></param>
        /// <returns></returns>
        public Boolean CallMonstersForGameClient(GameClient client, int monsterID, int callAsType = (int)MonsterTypes.Pet, int callNum = 1)
        {
            return GameManager.MonsterZoneMgr.CallDynamicMonstersOwnedByRole(client, monsterID, callAsType, callNum);
        }

        #endregion 动态召唤怪物

        #region 地图定位管理

        /// <summary>
        /// 返回地图定位的 x,y  
        /// </summary>
        /// <param name="client"></param>
        /// <param name="recordIndex"></param>
        /// <returns></returns>
        public String GetMapRecordXY(GameClient client, int recordIndex)
        {
            int mapCode = 0, x = 0, y = 0;

            if (Global.GetMapRecordDataByField(client, recordIndex, out mapCode, out x, out y))
            {
                return String.Format("{0},{1}", x, y);
            }

            return "无";
        }

        /// <summary>
        /// 返回地图定位的 相应地图名称
        /// </summary>
        /// <param name="client"></param>
        /// <param name="recordIndex"></param>
        /// <returns></returns>
        public String GetMapRecordMapName(GameClient client, int recordIndex)
        {
            int mapCode = 0, x = 0, y = 0;

            if (Global.GetMapRecordDataByField(client, recordIndex, out mapCode, out x, out y))
            {
                return Global.GetMapName(mapCode);
            }

            return "无";
        }

        /// <summary>
        /// 记录当前位置，作为第 recordIndex 个索引
        /// </summary>
        /// <param name="client"></param>
        /// <param name="recordIndex"></param>
        public void RecordCurrentMapPosition(GameClient client, int recordIndex)
        {
            Global.ModifyMapRecordData(client, (ushort)client.CurrentMapCode, (ushort)client.CurrentGrid.X, (ushort)client.CurrentGrid.Y, recordIndex);
        }

        /// <summary>
        /// 传送到具体位置
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mapCode"></param>
        /// <param name="recordIndex"></param>
        public void GotoMapRecordXY(GameClient client, int recordIndex)
        {
            int mapCode = 0, gridX = 0, gridY = 0;

            if (Global.GetMapRecordDataByField(client, recordIndex, out mapCode, out gridX, out gridY))
            {
                //更改位置并传送
                Point pixel = Global.GridToPixel(mapCode, gridX, gridY);
                GotoMap(client, mapCode, (int)pixel.X, (int)pixel.Y);
            }
        }

        #endregion 地图定位管理

        #region 角色级别

        /// <summary>
        /// 获取角色级别
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int get_level(GameClient client)
        {
            return client.ClientData.Level;
        }

        #endregion 角色级别

        #region 绑定元宝接口

        /// <summary>
        /// 添加绑定元宝
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void AddUserGold(GameClient client, int gold)
        {
            GameManager.ClientMgr.AddUserGoldOffLine(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client.ClientData.RoleID, gold, "LUA脚本", client.strUserID);
        }

        /// <summary>
        /// 扣除绑定元宝
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void SubUserGold(GameClient client, int gold)
        {
            GameManager.ClientMgr.SubUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, gold);
        }

        #endregion 绑定元宝接口

        #region 元宝接口

        /// <summary>
        /// 添加元宝
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void AddUserMoney(GameClient client, int userMoney)
        {
            GameManager.ClientMgr.AddUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, userMoney, "lua接口");
        }

        /// <summary>
        /// 扣除元宝
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void SubUserMoney(GameClient client, int userMoney)
        {
            GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, userMoney, "lua接口");
        }

        #endregion 元宝接口

        #region 绑定金币接口

        /// <summary>
        /// 添加绑定金币
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void AddMoney1(GameClient client, int money1)
        {
            GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, money1, "LUA脚本添加绑定金币", false);
        }

        /// <summary>
        /// 扣除绑定金币
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void SubMoney1(GameClient client, int money1)
        {
            GameManager.ClientMgr.SubMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, money1, "LUA脚本扣除绑定金币");
        }

        #endregion 绑定金币接口

        #region 金币接口

        /// <summary>
        /// 添加金币
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void AddYinLiang(GameClient client, int yinLiang)
        {
            GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, yinLiang, "LUA脚本添加金币");
        }

        /// <summary>
        /// 扣除金币
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void SubYinLiang(GameClient client, int yinLiang)
        {
            GameManager.ClientMgr.SubUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, yinLiang, "LUA脚本扣除金币");
        }

        #endregion 金币接口

        #region 经验接口

        /// <summary>
        /// 添加经验
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void AddExp(GameClient client, int exp, bool enableFilter = false, bool writeToDB = false)
        {
            GameManager.ClientMgr.ProcessRoleExperience(client, exp, enableFilter, writeToDB);
        }

        #endregion 经验接口

        #region 使用物品

        /// <summary>
        /// 使用物品
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void ToUseGoods(GameClient client, int goodsID, int goodsNum, bool usingGoods, out bool ret, out bool usingBinding, out bool usedTimeLimited)
        {
            usingBinding = false;
            usedTimeLimited = false;
            ret = GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                client, goodsID, goodsNum, usingGoods, out usingBinding, out usedTimeLimited);
        }

        /// <summary>
        /// 返回物品数量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        public int GetGoodsNumByGoodsID(GameClient client, int goodsID)
        {
            return Global.GetTotalGoodsCountByID(client, goodsID);
        }

        #endregion 使用物品

        #region 给个人增删临时NPC的管理(切换地图回来后，回来NPC就消失了)

        /// <summary>
        /// 增加一个临时的NPC
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void AddNPCForClient(GameClient client, int npcID, int toX, int toY)
        {
            //获取一个NPC对象
            NPC npc = NPCGeneralManager.GetNPCFromConfig(client.ClientData.MapCode, npcID, toX, toY, 0);
            if (null != npc)
            {
                GameManager.ClientMgr.NotifyMySelfNewNPC(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, npc);
            }
        }

        /// <summary>
        /// 删除一个临时的NPC
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void RemoveNPCForClient(GameClient client, int npcID)
        {
            GameManager.ClientMgr.NotifyMySelfDelNPC(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, client.ClientData.MapCode, npcID);
        }

        #endregion 给个人增删临时NPC的管理(切换地图回来后，回来NPC就消失了)

        #region 给地图增删临时NPC的管理(系统重启后，NPC就消失了)

        /// <summary>
        /// 增加一个临时的地图NPC
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void AddNpcToMap(int npcID, int mapCode, int toX, int toY)
        {
            //获取一个NPC对象
            NPC npc = NPCGeneralManager.GetNPCFromConfig(mapCode, npcID, toX, toY, 0);
            if (null != npc)
            {
                NPCGeneralManager.AddNpcToMap(npc);
            }
        }

        /// <summary>
        /// 删除一个临时的地图NPC
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void RemoveMapNpc(int mapCode, int npcID)
        {
            NPCGeneralManager.RemoveMapNpc(mapCode, npcID);
        }

        #endregion 给地图增删临时NPC的管理(系统重启后，NPC就消失了)

        #region 临时特效播放(非地图特效)

        /// <summary>
        /// 地图区域触发事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="type"></param>
        /// <param name="flag"></param>
        public void BroadcastMapRegionEvent(GameClient client, int areaLuaID, int type, int flag)
        {
            GlobalEventSource.getInstance().fireEvent(new ClientRegionEventObject(client, type, flag, areaLuaID));
        }

        /// <summary>
        /// 通知其自己，开始播放特效
        /// </summary>
        /// <param name="client"></param>
        public void NotifySelfDeco(GameClient client, int decoID, int decoType, int toBody, int toX, int toY, int shakeMap, int toX1, int toY1, int moveTicks, int alphaTicks)
        {
            GameManager.ClientMgr.NotifySelfDeco(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, 
                client, decoID, decoType, toBody, toX, toY, shakeMap, toX1, toY1, moveTicks, alphaTicks);
        }

        /// <summary>
        /// 通知其自己和其他人，自己开始播放特效(同一个地图才需要通知)
        /// </summary>
        /// <param name="client"></param>
        public void NotifyOthersMyDeco(GameClient client, int decoID, int decoType, int toBody, int toX, int toY, int shakeMap, int toX1, int toY1, int moveTicks, int alphaTicks, List<Object> objsList = null)
        {
            GameManager.ClientMgr.NotifyOthersMyDeco(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, decoID, decoType, toBody, toX, toY, shakeMap, toX1, toY1, moveTicks, alphaTicks, null);
        }

        #endregion 临时特效播放(非地图特效)

        #region 个人紧要消息通知

        /// <summary>
        /// 通知在线的所有人(不限制地图)个人紧要消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyAllImportantMsg(GameClient client, string msgText, int typeIndex, int showGameInfoType, int errCode = 0)
        {
            msgText = ConvertLuaString(msgText);
            GameManager.ClientMgr.NotifyAllImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, msgText, (GameInfoTypeIndexes)typeIndex, (ShowGameInfoTypes)showGameInfoType, errCode);
        }

        /// <summary>
        /// 通知在线的所有帮会的人(不限制地图)个人紧要消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyBangHuiImportantMsg(int faction, string msgText, GameInfoTypeIndexes typeIndex, ShowGameInfoTypes showGameInfoType, int errCode = 0)
        {
            msgText = ConvertLuaString(msgText);
            GameManager.ClientMgr.NotifyBangHuiImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                faction, msgText, (GameInfoTypeIndexes)typeIndex, (ShowGameInfoTypes)showGameInfoType, errCode);
        }

        /// <summary>
        /// 通知在线的对方(不限制地图)个人紧要消息
        /// </summary>
        /// <param name="client"></param>
        public void NotifyImportantMsg(GameClient client, string msgText, GameInfoTypeIndexes typeIndex, ShowGameInfoTypes showGameInfoType, int errCode = 0)
        {
            msgText = ConvertLuaString(msgText);
            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                client, msgText, (GameInfoTypeIndexes)typeIndex, (ShowGameInfoTypes)showGameInfoType, errCode);
        }

        #endregion 个人紧要消息通知

        #region 模拟傲视遮天的简化的消息通知函数

        /// <summary>
        /// 普通消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="warningText"></param>
        public void Info(GameClient client, string infoText, int errCode = 0)
        {
            if (string.IsNullOrEmpty(infoText))
            {
                return;
            }

            infoText = ConvertLuaString(infoText);

            /// 通知在线的对方(不限制地图)个人紧要消息
            NotifyImportantMsg(client, infoText, GameInfoTypeIndexes.Normal, ShowGameInfoTypes.ErrAndBox, errCode);
        }

        /// <summary>
        /// 成功消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="warningText"></param>
        public void Hot(GameClient client, string infoText, int errCode = 0)
        {
            if (string.IsNullOrEmpty(infoText))
            {
                return;
            }

            infoText = ConvertLuaString(infoText);

            /// 通知在线的对方(不限制地图)个人紧要消息
            NotifyImportantMsg(client, infoText, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.ErrAndBox, errCode);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="warningText"></param>
        public void Error(GameClient client, string warningText, int errCode = 0)
        {
            if (string.IsNullOrEmpty(warningText))
            {
                return;
            }

            warningText = ConvertLuaString(warningText);

            /// 通知在线的对方(不限制地图)个人紧要消息
            NotifyImportantMsg(client, warningText, GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, errCode);
        }

        #endregion 模拟傲视遮天的简化的消息通知函数

        #region 任务管理

        /// <summary>
        /// 处理任务
        /// </summary>
        /// <param name="client"></param>
        /// <param name="npcID"></param>
        /// <param name="extensionID"></param>
        /// <param name="goodsID"></param>
        /// <param name="taskType"></param>
        public void HandleTask(GameClient client, int npcID, int extensionID, int goodsID, int taskType)
        {
            ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                npcID, extensionID, goodsID, (TaskTypes)taskType);
        }

        #endregion 任务管理

        #region 特殊效果接口

        /// <summary>
        /// 播放游戏特殊效果(只给指定的角色)
        /// </summary>
        /// <param name="effectName"></param>
        /// <param name="lifeTicks"></param>
        public void SendGameEffect(GameClient client, string effectName, int lifeTicks, int alignMode = 0, string mp3Name = "")
        {
            GameManager.ClientMgr.SendGameEffect(client, effectName, lifeTicks, (GameEffectAlignModes)alignMode, mp3Name);
        }

        /// <summary>
        /// 播放游戏特殊效果
        /// </summary>
        /// <param name="effectName"></param>
        /// <param name="lifeTicks"></param>
        public void BroadCastGameEffect(int mapCode, int copyMapID, string effectName, int lifeTicks, int alignMode = 0, string mp3Name = "")
        {
            GameManager.ClientMgr.BroadCastGameEffect(mapCode, copyMapID, effectName, lifeTicks, (GameEffectAlignModes)alignMode, mp3Name);
        }

        #endregion 特殊效果接口

        #region 角色常用数据查询
        /// <summary>
        /// 返回角色通用参数数据值，通天令值，成就值，真气值，悟性值，猎杀值，精元值，积分等    
        /// 对应RoleCommonUseIntParamsIndexs中各个枚举变量
        /// </summary>
        /// <param name="client"></param>
        /// <param name="goodsID"></param>
        /// <returns></returns>
        public int GetRoleCommonParamsValue(GameClient client, int type)
        {
            if (type == (int)RoleCommonUseIntParamsIndexs.ChengJiu)
            {
                return GameManager.ClientMgr.GetChengJiuPointsValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.ZhuangBeiJiFen)
            {
                return GameManager.ClientMgr.GetZhuangBeiJiFenValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.WuXingZhi)
            {
                return GameManager.ClientMgr.GetWuXingValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.ZhenQiZhi)
            {
                return GameManager.ClientMgr.GetZhenQiValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.TianDiJingYuan)
            {
                return GameManager.ClientMgr.GetTianDiJingYuanValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.ZaiZaoPoint)
            {
                return GameManager.ClientMgr.GetZaiZaoValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.ShiLianLing)
            {
                return GameManager.ClientMgr.GetShiLianLingValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.JingMaiLevel)
            {
                return GameManager.ClientMgr.GetJingMaiLevelValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.WuXueLevel)
            {
                return GameManager.ClientMgr.GetWuXueLevelValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.ZuanHuangLevel)
            {
                return GameManager.ClientMgr.GetZuanHuangLevelValue(client);
            }
            else if (type == (int)RoleCommonUseIntParamsIndexs.SystemOpenValue)
            {
                return GameManager.ClientMgr.GetSystemOpenValue(client);
            }

            return 0;
        }
        #endregion  角色常用数据查询

        #region npc 旧脚本函数调用

        /// <summary>
        /// 负数表示未配置脚本，其它的表示npc脚本函数调用成功，具体执行结果如何系统会进行提示
        /// </summary>
        /// <param name="client"></param>
        /// <param name="scriptID"></param>
        /// <param name="npcID"></param>
        /// <returns></returns>
        public int ProcessNPCScript(GameClient client, int scriptID, int npcID)
        {
            return RunNPCScripts.ProcessNPCScript(client, scriptID, npcID);
        }

        #endregion npc 旧脚本函数调用

        #region 王城争霸相关接口

        /// <summary>
        /// 返回最近一次的王城争霸时间
        /// </summary>
        /// <returns></returns>
        public void GetNextCityBattleTimeAndBangHui(out Boolean result, out String sTime, out String sBangHui)
        {
            result = WangChengManager.GetNextCityBattleTimeAndBangHui(out sTime, out sBangHui);
        }

        /// <summary>
        /// 返回王城争霸的时间和申请帮会列表信息,逗号隔开
        /// </summary>
        /// <returns></returns>
        public String GetCityBattleTimeAndBangHuiListString()
        {
            if (GameManager.OPT_ChengZhanType == 0)
            {
                return WangChengManager.GetCityBattleTimeAndBangHuiListString();
            }
            else
            {
                return LuoLanChengZhanManager.getInstance().GetCityBattleTimeAndBangHuiListString();
            }
        }

        #endregion 王城争霸相关接口

        #region 角色等级等数据
        /// <summary>
        /// 返回角色等级,在client内部写个小函数也行，对于脚本调用的函数，统一放置在这个位置
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetRoleLevel(GameClient client)
        {
            return client.ClientData.Level;
        }
        #endregion 角色等级等数据

        #region BOSS副本

        /// <summary>
        /// 返回boss副本剩余次数字符串
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public String GetBossFuBenLeftTimeString(GameClient client)
        {
            int bossFuBenID = Global.FindBossFuBenIDByRoleLevel(client.ClientData.Level);
            if (bossFuBenID > 0)
            {
                FuBenData fbData = Global.GetFuBenData(client, bossFuBenID);
                if (null != fbData)
                {
                    int nFinishNum;
                    return String.Format("{0}", Math.Max(0, Global.GetBossFuBenCanFreeEnterNum(client) - Global.GetFuBenEnterNum(fbData, out nFinishNum)) + Global.GetBossFuBenCanExtEnterNum(client));
                }
                else
                {
                    return String.Format("{0}", Global.GetBossFuBenCanFreeEnterNum(client) + Global.GetBossFuBenCanExtEnterNum(client));
                }
            }

            return "0";
        }

        /// <summary>
        /// 进入boss副本
        /// </summary>
        /// <param name="client"></param>
        public void EnterBossFuBen(GameClient client)
        {
            int ret = Global.EnterBossFuBen(client);

            if (-1 == ret)
            {
                Error(client, String.Format(Global.GetLang("至少{0}级才能进入boss副本"), Global.GetBossFuBenMinLevel()));
            }
            else if (-4 == ret)
            {
                Error(client, String.Format(Global.GetLang("今日boss副本进入次数已用完")));
            }
            else if (ret < 0)
            {
                Error(client, String.Format(Global.GetLang("进入boss副本失败{0}"), ret));
            }
        }

        #endregion BOSS副本

        #region VIP接口

        /// <summary>
        /// 是否是VIP
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool IsVip(GameClient client)
        {
            return Global.IsVip(client);
        }

        /// <summary>
        /// 获取Vip的类型
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetVipType(GameClient client)
        {
            return (int)Global.GetVipType(client);
        }

        #endregion VIP接口

        #region 角色参数接口

        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public string get_param(GameClient client, string paramName)
        {
            return Global.GetRoleParamByName(client, paramName);
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public void set_param(GameClient client, string paramName, string paramValue, bool writeToDB = false)
        {
            Global.UpdateRoleParamByName(client, paramName, paramValue, writeToDB);
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public void set_param(GameClient client, string paramName, int paramValue, bool writeToDB = false)
        {
            Global.UpdateRoleParamByName(client, paramName, paramValue.ToString(), writeToDB);
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public void set_param(GameClient client, string paramName, double paramValue, bool writeToDB = false)
        {
            Global.UpdateRoleParamByName(client, paramName, paramValue.ToString(), writeToDB);
        }

        #endregion 角色参数接口

        #region 时间和日期相关

        /// <summary>
        /// 返回日期ID
        /// </summary>
        /// <returns></returns>
        public int Today()
        {
            return (int)TimeUtil.NowDateTime().DayOfYear;
        }

        #endregion 时间和日期相关

        #region 进入古墓地图
        /// <summary>
        /// 进入古墓地图
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GotoGuMuMap(GameClient client)
        {
            return Global.GotoGuMuMap(client);
        }
        #endregion 进入古墓地图

        #region 二锅头相关

        /// <summary>
        /// 获取喝的二锅头的名称
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public string GetErGuoTouBufferName(GameClient client)
        {
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.ErGuoTou);
            if (null == bufferData)
            {
                return Global.GetLang("未喝酒");
            }

            if (Global.IsBufferDataOver(bufferData))
            {
                return Global.GetLang("未喝酒");
            }

            long goodsID = 0x00000000FFFFFFFF & bufferData.BufferVal >> 32;
            return Global.GetGoodsNameByID((int)goodsID);
        }

        /// <summary>
        /// 获取喝的二锅头的剩余时间
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public string GetErGuoTouBufferLeftTime(GameClient client)
        {
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.ErGuoTou);
            if (null == bufferData)
            {
                return Global.GetLang("0秒");
            }

            if (Global.IsBufferDataOver(bufferData))
            {
                return Global.GetLang("0秒");
            }

            return StringUtil.substitute(Global.GetLang("{0}分{1}秒"), bufferData.BufferSecs / 60, bufferData.BufferSecs % 60);
        }

        /// <summary>
        /// 获取喝的二锅头的当前收益
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public string GetErGuoTouBufferExperience(GameClient client)
        {
            BufferData bufferData = Global.GetBufferDataByID(client, (int)BufferItemTypes.ErGuoTou);
            if (null == bufferData)
            {
                return "0";
            }

            if (Global.IsBufferDataOver(bufferData))
            {
                return "0";
            }

            //判断如果是否在打坐，则自动增加经验和内力值
            RoleSitExpItem roleSitExpItem = null;
            if (client.ClientData.Level < Data.RoleSitExpList.Length)
            {
                roleSitExpItem = Data.RoleSitExpList[client.ClientData.Level];
            }

            //经验的收益
            if (null != roleSitExpItem)
            {
                int experience = roleSitExpItem.Experience;
                double dblExperience = 1.0;

                //这儿应该是双倍烤火时间(后期加入)
                if (SpecailTimeManager.JugeIsDoulbeKaoHuo())
                {
                    dblExperience += 1.0;
                }

                //如果是处于组队状态，则有经验加成
                //处理组队状态下的祝福经验加成
                dblExperience += Global.ProcessTeamZhuFuExperience(client);

                double multiExpNum = (bufferData.BufferVal & 0x00000000FFFFFFFF) - 1.0;

                //增加额外的倍数
                dblExperience += multiExpNum;

                //处理双倍经验的buffer
                experience = (int)(experience * dblExperience);

                return experience.ToString();
            }

            return "0";
        }

        /// <summary>
        /// 获取喝的二锅头的剩余次数
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public string GetErGuoTouTodayLeftUseTimes(GameClient client)
        {
            return (6 - Global.GetErGuoTouTodayNum(client)).ToString();
        }

        #endregion 二锅头相关

        #region 补偿用户玩家

        /// <summary>
        /// 获取补偿的开始时间
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public string GetStartBuChangTime(GameClient client)
        {
            return Global.GetTimeByBuChang(0, 0, 0, 0);
        }

        /// <summary>
        /// 获取补偿的结束时间
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public string GetEndBuChangTime(GameClient client)
        {
            return Global.GetTimeByBuChang(1, 23, 59, 59);
        }

        /// <summary>
        /// 获取补偿的经验
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public long GetBuChangExp(GameClient client)
        {
            return BuChangManager.GetBuChangExp(client);
        }

        /// <summary>
        /// 获取补偿的绑定元宝
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public int GetBuChangBindYuanBao(GameClient client)
        {
            return BuChangManager.GetBuChangBindYuanBao(client);
        }

        /// <summary>
        /// 获取补偿的物品名称
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public string GetBuChangGoodsNames(GameClient client)
        {
            List<GoodsData> goodsDataList = BuChangManager.GetBuChangGoodsDataList(client);
            if (null == goodsDataList)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < goodsDataList.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" ");
                }

                sb.AppendFormat("{0}({1})", Global.GetGoodsNameByID(goodsDataList[i].GoodsID), goodsDataList[i].GCount);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 给予补偿
        /// </summary>
        /// <param name="client"></param>
        public void GiveBuChang(GameClient client)
        {
            BuChangManager.GiveBuChang(client);
        }

        #endregion 补偿用户玩家

        #region boss动画相关

        /// <summary>
        /// 通知播放boss动画
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gold"></param>
        public void PlayBossAnimation(GameClient client, int monsterID, int mapCode, int toX, int toY, int effectX, int effectY)
        {
            GameManager.ClientMgr.NotifyPlayBossAnimation(client, monsterID, mapCode, toX, toY, effectX, effectY);
        }

        #endregion boss动画相关

        #region 操作脚本

        public void ExecSwitchServerScript(GameClient client, string script)
        {

        }

        #endregion 操作脚本
    }
}
