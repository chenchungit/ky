using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Server.Tools;
using Server.Protocol;
using GameServer.Server;
using Server.TCP;
using System.Net.Sockets;

namespace GameServer.Logic
{
    //public enum TCPProcessCmdResults { RESULT_OK = 0, RESULT_FAILED = 1, RESULT_DATA = 2 };
    public class NewZoneActiveManager
    {

        /// <summary>
        /// 新服活动 冲级狂人 gwz
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessQueryLevelUpMadmanCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //解析用户名称和用户密码
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (NewZoneActiveManager.QueryLevelUpMadman(client, pool, nID, out tcpOutPacket))
                {
                    return TCPProcessCmdResults.RESULT_DATA;
                }

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessQueryLevelUpMadmanCmd", false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }
        /// <summary>
        ///  查询冲级达人信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static bool QueryLevelUpMadman(GameClient client, TCPOutPacketPool pool, int nID, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            try
            {
                KingActivity instActivity = (KingActivity)Global.GetActivity(ActivityTypes.NewZoneUpLevelMadman);
               
                NewZoneUpLevelData data = new NewZoneUpLevelData();
                
                int count = instActivity.RoleLimit.Count;
                data.Items = new List<NewZoneUpLevelItemData>();
                for (int i = 1; i < count+1; i++)
                {
                    NewZoneUpLevelItemData item = new NewZoneUpLevelItemData();
                    AwardItem awd = instActivity.GetAward(client, i);
                    item.LeftNum = awd.MinAwardCondionValue2 - Global.GetChongJiLingQuShenZhuangQuota(client, i);
                    item.GetAward = !Global.CanGetChongJiLingQuShenZhuang(client, i);
                    data.Items.Add(item);
                }

                #region 注释掉的代码
                //AwardItem awd = instActivity.GetAward(client, (int)GiftBtnIndex.BTN1);
                //item.LeftNum =awd.MinAwardCondionValue2 - Global.SafeConvertToInt32(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.ChongJiGift1));
                //item.GetAward =! Global.CanGetChongJiLingQuShenZhuang(client, (int)GiftBtnIndex.BTN1);
                //data.Items.Add(item);

                //item = new NewZoneUpLevelItemData();
                //awd = instActivity.GetAward(client, (int)GiftBtnIndex.BTN2);
                //item.LeftNum = awd.MinAwardCondionValue2 - Global.SafeConvertToInt32(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.ChongJiGift2));
                //item.GetAward = !Global.CanGetChongJiLingQuShenZhuang(client, (int)GiftBtnIndex.BTN2);
                //data.Items.Add(item);

                //item = new NewZoneUpLevelItemData();
                //awd = instActivity.GetAward(client, (int)GiftBtnIndex.BTN3);
                //item.LeftNum = awd.MinAwardCondionValue2 - Global.SafeConvertToInt32(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.ChongJiGift3));
                //item.GetAward = !Global.CanGetChongJiLingQuShenZhuang(client, (int)GiftBtnIndex.BTN3);
                //data.Items.Add(item);

                //item = new NewZoneUpLevelItemData();
                //awd = instActivity.GetAward(client, (int)GiftBtnIndex.BTN4);
                //item.LeftNum = awd.MinAwardCondionValue2 - Global.SafeConvertToInt32(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.ChongJiGift4));
                //item.GetAward = !Global.CanGetChongJiLingQuShenZhuang(client, (int)GiftBtnIndex.BTN4);
                //data.Items.Add(item);

                //item = new NewZoneUpLevelItemData();
                //awd = instActivity.GetAward(client, (int)GiftBtnIndex.BTN5);
                //item.LeftNum = awd.MinAwardCondionValue2 - Global.SafeConvertToInt32(GameManager.GameConfigMgr.GetGameConifgItem(GameConfigNames.ChongJiGift5));
                //item.GetAward = !Global.CanGetChongJiLingQuShenZhuang(client, (int)GiftBtnIndex.BTN5);
                //data.Items.Add(item);
                #endregion

                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<NewZoneUpLevelData>(data, pool, nID);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "LevelUpMadman", false);

            }
            return false;

        }
      
       
        #region 活动信息查询 
       

        /// <summary>
        /// 获得活动信息，根据活动type，角色id
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessGetActiveInfo(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID));

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
                    return TCPProcessCmdResults.RESULT_DATA;
                }
                //角色id
                int roleID = Convert.ToInt32(fields[0]);
                //活动类型
                int activetype = Convert.ToInt32(fields[1]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                //定位角色成功之后将命令转发给gamedbserver
                TCPProcessCmdResults ret = Global.RequestToDBServer2(tcpClientPool, pool, nID, Global.GetActivityRequestCmdString((ActivityTypes)activetype, client), out tcpOutPacket, client.ServerId);
                return ret;
            }
            catch (Exception ex)
            {
                
                DataHelper.WriteFormatExceptionLog(ex, "", false);
                
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }


        #endregion

        #region 领取奖励

        /// <summary>
        /// 获得冲级狂人奖励
        /// </summary>
        /// <param name="client"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="nRoleID"></param>
        /// <param name="nActivityType"></param>
        /// <param name="nBtnIndex"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        private static TCPProcessCmdResults GetNewLevelUpMadmanAward(GameClient client, TCPOutPacketPool pool, int nID, int nRoleID, int nActivityType, int nBtnIndex,out TCPOutPacket tcpOutPacket)
        {
            string strcmd = "";
            Activity instActivity = Global.GetActivity((ActivityTypes)nActivityType);
            // 判断背包是否够用
            if (!instActivity.HasEnoughBagSpaceForAwardGoods(client, nBtnIndex))
            {
                strcmd = string.Format("{0}:{1}:0", -20, nActivityType);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }


            //
            int nOcc = Global.CalcOriginalOccupationID(client);

            // MU 修改逻辑
            //int nRoleLev = client.ClientData.Level;
            int nChangeLifeLev = client.ClientData.ChangeLifeCount;
            AwardItem tmpItem = instActivity.GetAward(client, nOcc, 1);
            if (tmpItem == null)
            {
                strcmd = string.Format("{0}:{1}:0", -1, nActivityType);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                return TCPProcessCmdResults.RESULT_DATA;
            }
            //是否在领取奖励期限内
            if (!instActivity.CanGiveAward())
            {
                strcmd = string.Format("{0}:{1}:0", -10, nActivityType);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            tmpItem = instActivity.GetAward(client, nBtnIndex);
            if (nChangeLifeLev < tmpItem.MinAwardCondionValue)
            {
                strcmd = string.Format("{0}:{1}:0", -100, nActivityType);           //您当前的转生次数未达到要求
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            if (nChangeLifeLev == tmpItem.MinAwardCondionValue&&client.ClientData.Level < tmpItem.MinAwardCondionValue3)
            {
                strcmd = string.Format("{0}:{1}:0", -101, nActivityType);           //您当前的等级尚未达到领取等级要求
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            // 检测当前的角色是否已经领取了该项豪礼                            
            if (!Global.CanGetChongJiLingQuShenZhuang(client, nBtnIndex))
            {
                strcmd = string.Format("{0}:{1}:0", -103, nActivityType);          //您已领取过该等级段奖励,无法再次进行领取
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            // 检测当前是否还有领取的名额
            int nQuota = Global.GetChongJiLingQuShenZhuangQuota(client, nBtnIndex);
            if (nQuota >= tmpItem.MinAwardCondionValue2)
            {
                strcmd = string.Format("{0}:{1}:0", -102, nActivityType);   // 该等级段的奖励名额已满，无法进行领取
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            //添加物品
            instActivity.GiveAward(client, nBtnIndex, nOcc);

            //完成冲级神装领取
            Global.CompleteChongJiLingQuShenZhuang(client, nBtnIndex, nQuota + 1);

            //冲级领取神装大礼领取提示
            AwardItem tmpItem1 = instActivity.GetAward(client, nBtnIndex, 2);
            if(tmpItem1!=null && tmpItem1.GoodsDataList.Count>0)
                Global.BroadcastChongJiLingQuShengZhuangHint(client, nBtnIndex, tmpItem1.GoodsDataList[nOcc].GoodsID);

            //告诉客户端已经点击了
           // string resoult = "" + nBtnIndex + "," + 1;
            strcmd = string.Format("{0}:{1}:{2}", 1, nActivityType, nBtnIndex);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            return TCPProcessCmdResults.RESULT_DATA;
        }
        /// <summary>
        /// 获得新区活动奖励
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessGetActiveAwards(Server.TCPManager tcpMgr, TMSKSocket socket, Server.TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
             tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID));

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int activityType = Global.SafeConvertToInt32(fields[1]);
                int extTag = Global.SafeConvertToInt32(fields[2]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                switch (activityType)
                {
                    case (int)ActivityTypes.NewZoneUpLevelMadman:
                        {
                            return NewZoneActiveManager.GetNewLevelUpMadmanAward(client, pool, nID, roleID, activityType, extTag, out tcpOutPacket);

                        }
                    case (int)ActivityTypes.NewZoneConsumeKing:
                    case (int)ActivityTypes.NewZoneFanli:
                    case (int)ActivityTypes.NewZoneRechargeKing:
                    case (int)ActivityTypes.NewZoneBosskillKing:
                        {
                            return NewZoneActiveManager.GetActiveAwards(client, tcpClientPool, pool, nID, roleID, activityType, out tcpOutPacket);

                        }
                }
                
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
            return TCPProcessCmdResults.RESULT_DATA;
            
        }
        /// <summary>
        /// 领取活动奖励，除了冲级达人
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        private static TCPProcessCmdResults GetActiveAwards(GameClient client, Server.TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, int roleID, int activityType,out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                
                string strcmd = "";

                Activity instActivity = Global.GetActivity((ActivityTypes)activityType);

                if (null == instActivity)
                {
                    strcmd = string.Format("{0}:{1}:0", -1, activityType);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                //是否在领取奖励期限内
                if (!instActivity.CanGiveAward())
                {
                    strcmd = string.Format("{0}:{1}:0", -10, activityType);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }
              //  if(!instActivity.
                //判断背包是否够用,
                if (!instActivity.HasEnoughBagSpaceForAwardGoods(client))
                {
                    strcmd = string.Format("{0}:{1}:0", -20, activityType);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                string[] dbFields = null;
                // nDBExecuteID = 0; //Global.GetDBServerExecuteActivityAwardCmdID((ActivityTypes)activityType);
                Int32 nDBExecuteID = (int)TCPGameServerCmds.CMD_SPR_GETNEWZONEACTIVEAWARD;
                string dbCmds = Global.GetActivityRequestCmdString((ActivityTypes)activityType, client, activityType);

                if (nDBExecuteID <= 0 || string.IsNullOrEmpty(dbCmds))
                {
                    strcmd = string.Format("{0}:{1}:0", -4, activityType);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                //通知gamedbserver  领取奖励，gamedbserver 判断如果可以领取，设置数据库领取标志,同时计算奖励额度条件，比如排名，充值额度
                Global.RequestToDBServer(tcpClientPool, pool, nDBExecuteID, dbCmds, out dbFields, client.ServerId);

                if (null == dbFields || dbFields.Length != 3)
                {
                    strcmd = string.Format("{0}:{1}:0", -5, activityType);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                int result = Global.SafeConvertToInt32(dbFields[0]);
                //gamedbserver 设置相关领取标志出错，不能领取
                if (result <= 0)
                {
                    strcmd = string.Format("{0}:{1}:0", result, activityType);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }
                
                //开始发放奖励，奖励分 为元宝奖励 和 物品奖励,根据不同的活动给予不同的奖励
                if (!instActivity.GiveAward(client, Global.SafeConvertToInt32(dbFields[1])))
                {
                    strcmd = string.Format("{0}:{1}:0", -7, activityType);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                //if ((ActivityTypes)activityType == ActivityTypes.InputJiaSong)
                //{
                //    //充值加送公告
                //    Global.BroadcastJiaSongOk(client);
                //}

                strcmd = string.Format("{0}:{1}:{2}", 1, activityType, dbFields[1]);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {

                DataHelper.WriteFormatExceptionLog(ex, "GetActiveAwards", false);
                
            }

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
            return TCPProcessCmdResults.RESULT_DATA;
        }

        #endregion

        //public static void BroadcastNewzoneFanli(GameClient client)
        //{
        //    string broadCastMsg = "";
        //    broadCastMsg = StringUtil.substitute(Global.GetLang("恭喜【{1}】完成了每日充值，领取了丰厚的奖品！"),
        //        Global.GetServerLineName2(),
        //        Global.FormatRoleName(client, client.ClientData.RoleName));

        //    //播放用户行为消息
        //    Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.Bulletin, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
        //}
    }
}
