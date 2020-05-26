using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Server.Protocol;
using GameServer.Logic;
using GameServer.Server;
using Server.Tools;

namespace GameServer.Logic.MUWings
{
    /// <summary>
    /// 静态MU翅膀管理类
    /// </summary>
    public static class MUWingsManager
    {
        #region 翅膀管理

        /// <summary>
        /// 翅膀的最大ID值(1~9)
        /// </summary>
        public static int MaxWingID { get { return GameManager.SystemWingsUp.MaxKey; } }

        /// <summary>
        /// 翅膀的最高强化级别
        /// </summary>
        public static int MaxWingEnchanceLevel = 10;

        /// <summary>
        /// 为角色初始化第一阶段的翅膀
        /// </summary>
        /// <param name="client"></param>
        public static void InitFirstWing(GameClient client)
        {
            if (null == client.ClientData.MyWingData)
            {
                WingData wingData = AddWingDBCommand(TCPOutPacketPool.getInstance(), client.ClientData.RoleID, 1, client.ServerId); //获取得到第一阶翅膀
                client.ClientData.MyWingData = wingData;
            }
        }

        /// <summary>
        /// 数据库命令添加翅膀事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static WingData AddWingDBCommand(TCPOutPacketPool pool, int roleID, int WingID, int serverId)
        {
            //先DBServer请求扣费
            TCPOutPacket tcpOutPacket = null;
            string strcmd = string.Format("{0}:{1}", roleID, WingID);
            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer2(Global._TCPManager.tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_ADDWING, strcmd, out tcpOutPacket, serverId);
            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                return null;
            }

            //添加数据库失败
            if (null == tcpOutPacket)
            {
                return null;
            }

            WingData wingData = DataHelper.BytesToObject<WingData>(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);

            Global.PushBackTcpOutPacket(tcpOutPacket);

            return wingData;
        }

        /// <summary>
        /// 数据库命令翅膀佩戴/卸下事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dbID"></param>
        /// <param name="isUsing"></param>
        /// <returns></returns>
        public static int WingOnOffDBCommand(GameClient client, int dbID, int isUsing)
        {  
            //roleid, wingDbid, using, wingid, forgeLevel, failedNum, starExp, zhuLing, zhuHun
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, dbID, isUsing, "*", "*", "*", "*", "*", "*");
            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_MODWING, strcmd, client.ServerId);
            if (null == fields || fields.Length != 2)
            {
                return -1;
            }

            return Convert.ToInt32(fields[1]);
        }

        /// <summary>
        /// 数据库命令翅膀升星事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dbID"></param>
        /// <param name="nStarLevel"></param>
        /// <param name="nStarExp"></param>
        /// <returns></returns>
        public static int WingUpStarDBCommand(GameClient client, int dbID, int nStarLevel, int nStarExp)
        {
            //roleid, wingDbid, using, wingid, forgeLevel, failedNum, starExp, zhuLing, zhuHun
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, dbID,  
                                          "*",
                                          "*",
                                          nStarLevel,
                                          "*",
                                          nStarExp,
                                          "*",
                                          "*");

            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_MODWING, strcmd, client.ServerId);
            if (null == fields || fields.Length != 2)
            {
                return -1;
            }

            return Convert.ToInt32(fields[1]);
        }

        /// <summary>
        /// 数据库命令翅膀进阶事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dbID"></param>
        /// <param name="nWingLevel"></param>
        /// <param name="nFailNum"></param>
        /// <param name="nStarLevel"></param>
        /// <param name="nStarExp"></param>
        /// <returns></returns>
        public static int WingUpDBCommand(GameClient client, int dbID, int nWingLevel, int nFailNum, int nStarLevel, int nStarExp, int nZhuLingNum, int nZhuHunNum)
        {
            //roleid, wingDbid, using, wingid, forgeLevel, failedNum, starExp, ZhuLingNum, ZhuHunNum
            string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", client.ClientData.RoleID, dbID,
                                          "*",
                                          nWingLevel,
                                          nStarLevel,
                                          nFailNum,
                                          nStarExp,
                                          nZhuLingNum,
                                          nZhuHunNum);

            string[] fields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_MODWING, strcmd, client.ServerId);
            if (null == fields || fields.Length != 2)
            {
                return -1;
            }

            return Convert.ToInt32(fields[1]);
        }

        /// <summary>
        /// 将翅膀的扩展属性加入Buffer中
        /// </summary>
        /// <param name="client"></param>
        public static bool UpdateWingDataProps(GameClient client, bool toAdd = true)
        {
            if (null == client.ClientData.MyWingData) return false;
            if (client.ClientData.MyWingData.WingID <= 0)
            {
                return false;
            }

            SystemXmlItem baseXmlNode = WingPropsCacheManager.GetWingPropsCacheItem(Global.CalcOriginalOccupationID(client), client.ClientData.MyWingData.WingID);
            if (null == baseXmlNode) 
                return false;

            // 改变翅膀等级带来的属性变化
            ChangeWingDataProps(client, baseXmlNode, toAdd);

            baseXmlNode = WingStarCacheManager.GetWingStarCacheItem(Global.CalcOriginalOccupationID(client), client.ClientData.MyWingData.WingID, client.ClientData.MyWingData.ForgeLevel);
            if (null == baseXmlNode) 
                return false;

            // 改变翅膀升星带来的属性变化
            ChangeWingDataProps(client, baseXmlNode, toAdd);

            return true;
        }

        /// <summary>
        /// 改变翅膀的扩展属性
        /// </summary>
        /// <param name="client"></param>
        /// <param name="toAdd"></param>
        public static bool ChangeWingDataProps(GameClient client, SystemXmlItem baseXmlNode, bool toAdd = true)
        {
            if (null == client.ClientData.MyWingData) return false;
            if (client.ClientData.MyWingData.WingID <= 0)
            {
                return false;
            }

            double minAttackV = baseXmlNode.GetDoubleValue("MinAttackV");
            if (false == toAdd)
            {
                minAttackV = 0 - minAttackV;
            }

            double maxAttackV = baseXmlNode.GetDoubleValue("MaxAttackV");
            if (false == toAdd)
            {
                maxAttackV = 0 - maxAttackV;
            }

            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MinAttack] += minAttackV;
            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxAttack] += maxAttackV;

            double minMAttackV = baseXmlNode.GetDoubleValue("MinMAttackV");
            if (false == toAdd)
            {
                minMAttackV = 0 - minMAttackV;
            }

            double maxMAttackV = baseXmlNode.GetDoubleValue("MaxMAttackV");
            if (false == toAdd)
            {
                maxMAttackV = 0 - maxMAttackV;
            }

            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MinMAttack] += minMAttackV;
            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxMAttack] += maxMAttackV;

            double minDefenseV = baseXmlNode.GetDoubleValue("MinDefenseV");
            if (false == toAdd)
            {
                minDefenseV = 0 - minDefenseV;
            }

            double maxDefenseV = baseXmlNode.GetDoubleValue("MaxDefenseV");
            if (false == toAdd)
            {
                maxDefenseV = 0 - maxDefenseV;
            }

            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MinDefense] += minDefenseV;
            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxDefense] += maxDefenseV;

            double minMDefenseV = baseXmlNode.GetDoubleValue("MinMDefenseV");
            if (false == toAdd)
            {
                minMDefenseV = 0 - minMDefenseV;
            }

            double maxMDefenseV = baseXmlNode.GetDoubleValue("MaxMDefenseV");
            if (false == toAdd)
            {
                maxMDefenseV = 0 - maxMDefenseV;
            }

            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MinMDefense] += minMDefenseV;
            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxMDefense] += maxMDefenseV;

            double maxLifeV = baseXmlNode.GetDoubleValue("MaxLifeV");
            if (false == toAdd)
            {
                maxLifeV = 0 - maxLifeV;
            }

            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.MaxLifeV] += maxLifeV;

            double subAttackInjurePercent = baseXmlNode.GetDoubleValue("SubAttackInjurePercent");
            if (false == toAdd)
            {
                subAttackInjurePercent = 0 - subAttackInjurePercent;
            }

            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.SubAttackInjurePercent] += subAttackInjurePercent;

            double addAttackInjurePercent = baseXmlNode.GetDoubleValue("AddAttackInjurePercent");
            if (false == toAdd)
            {
                addAttackInjurePercent = 0 - addAttackInjurePercent;
            }

            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.AddAttackInjurePercent] += addAttackInjurePercent;
           
            double Dodge = baseXmlNode.GetDoubleValue("Dodge");
            if (false == toAdd)
            {
                Dodge = 0 - Dodge;
            }

            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.Dodge] += Dodge;

            double HitV = baseXmlNode.GetDoubleValue("HitV");
            if (false == toAdd)
            {
                HitV = 0 - HitV;
            }

            client.ClientData.EquipProp.ExtProps[(int)ExtPropIndexes.HitV] += HitV;

            return true;
        }

        /// <summary>
        /// 从缓存中读取进阶配置项
        /// </summary>
        /// <param name="occupation"></param>
        /// <param name="jingMaiID"></param>
        /// <param name="jingMaiLevel"></param>
        /// <returns></returns>
        public static SystemXmlItem GetWingUPCacheItem(int nLevel)
        {
            SystemXmlItem systemWingPropsCacheItem = null;
            if (!GameManager.SystemWingsUp.SystemXmlItemDict.TryGetValue(nLevel, out systemWingPropsCacheItem))
            {
                return null;
            }

            return systemWingPropsCacheItem;
        }

        #endregion 翅膀管理
    }
}
