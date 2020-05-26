using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using GameServer.Server;
using Server.TCP;
using Server.Data;
using Server.Tools;
using Server.Protocol;
using GameServer.Core.Executor;


namespace GameServer.Logic.YueKa
{
    public class YueKaManager
    {
        public static int DAYS_PER_YUE_KA = 30;

        /// <summary>
        /// android和越狱平台中ChongZhiAndrid.xml, 苹果ChongZhiItem.xml
        /// ID配置项为10000的是月卡的价格，需要与普通的充值档次分开
        /// </summary>
        public readonly static int YUE_KA_MONEY_ID_IN_CHARGE_FILE = 10000;

        /// <summary>
        /// 月卡奖励配置文件
        /// </summary>
        private static readonly string YUE_KA_GOODS_FILE = "Config/Activity/Card.xml";

        /// <summary>
        /// 所有职业都给的奖励
        /// Key:天数， Value:奖励信息
        /// </summary>
        private static Dictionary<int, YueKaAward> AllGoodsDict = new Dictionary<int,YueKaAward>();

        public static void LoadConfig()
        {
            GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(YUE_KA_GOODS_FILE));
            XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(YUE_KA_GOODS_FILE));
            if (xml == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件不存在", YUE_KA_GOODS_FILE));
                return;
            }

            try
            {
                IEnumerable<XElement> xmlItems = xml.Elements();
                foreach (var xmlItem in xmlItems)
                {
                    if (null == xmlItem)
                        continue;

                    YueKaAward award = new YueKaAward();
                    award.Init(xmlItem);
                    AllGoodsDict[award.Day] = award;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时异常{1}", YUE_KA_GOODS_FILE, ex));
            }
        }

        /// <summary>
        /// 玩家购买月卡
        /// </summary>
        /// <param name="userID">玩家ID</param>
        /// <param name="roleID">上一次登录的角色ID</param>
        public static void HandleUserBuyYueKa(string userID, int roleID)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
            {
                return;
            }

            /*
            TMSKSocket clientSocket = GameManager.OnlineUserSession.FindSocketByUserID(userID);
            GameClient client = null;
            if (clientSocket != null)
            {
                client = GameManager.ClientMgr.FindClient(clientSocket);
            }*/
            GameClient client = GameManager.ClientMgr.FindClient(roleID);
            LogManager.WriteLog(LogTypes.Error, string.Format("HandleUserBuyYueKa, userid={0}, roleid={1}", userID, roleID));
            if (null != client)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("HandleUserBuyYueKa, 玩家在线, 在线的userid={0},  roleid={1}", userID, client.ClientData.RoleID));
                // level up从t_input log取
                Global.ProcessVipLevelUp(client);

                lock (client.ClientData.YKDetail)
                {
                    //这里是正常流程，只允许一个角色同时只能买一张月卡
                    if (client.ClientData.YKDetail.HasYueKa == 0)
                    {
                        DateTime nowDate = TimeUtil.NowDateTime();
                        client.ClientData.YKDetail.HasYueKa = 1;
                        client.ClientData.YKDetail.BegOffsetDay = Global.GetOffsetDay(nowDate);
                        client.ClientData.YKDetail.EndOffsetDay = client.ClientData.YKDetail.BegOffsetDay + DAYS_PER_YUE_KA;
                        client.ClientData.YKDetail.CurOffsetDay = Global.GetOffsetDay(nowDate);
                        client.ClientData.YKDetail.AwardInfo = "0";
                    }
                    else
                    {
                        //如果买了多张月卡，只有叠加上去了
                        client.ClientData.YKDetail.EndOffsetDay += DAYS_PER_YUE_KA;
                    }

                    // GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.HasYueKa, 1);

                    // modify by chenjingui. 20150624 月卡改为通知剩余天数，买了月卡，通知一次
                    GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.YueKaRemainDay, client.ClientData.YKDetail.RemainDayOfYueKa());

                    _UpdateYKDetail2DB(client, client.ClientData.YKDetail);

                    if (client._IconStateMgr.CheckFuLiYueKaFanLi(client))
                    {
                        client._IconStateMgr.SendIconStateToClient(client);
                    }
                }
            }
            else
            {
                //FIXME!!!
                //会有这种情况么，玩家冲了月卡，db通知game，但是game没有找到这个角色？？？，先打个log吧
                LogManager.WriteLog(LogTypes.Error, string.Format("玩家购买了月卡，但是处理的时候找不到在线角色, UserID={0}, last roldid={1}, 转交给db处理", userID, roleID));
                int beginOffsetDay = Global.GetOffsetDay(TimeUtil.NowDateTime());
                string strcmd = string.Format("{0}:{1}:{2}", roleID, beginOffsetDay, RoleParamName.YueKaInfo); //只发增量
                string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_ROLE_BUY_YUE_KA_BUT_OFFLINE, strcmd, GameManager.LocalServerId);
                if (null == dbFields || dbFields.Length != 1 || dbFields[0] != "0")
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("玩家购买了月卡，但是处理的时候找不到在线角色, UserID={0}, last roldid={1}, 转交给db处理时失败了", userID, roleID));
                }
            }
        }

        public static TCPProcessCmdResults ProcessGetYueKaData(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, global::Server.Protocol.TCPOutPacketPool pool, int nID, byte[] data, int count, out global::Server.Protocol.TCPOutPacket tcpOutPacket)
        {
            //*/
            /*
             //*/

            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                if (1 != fields.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                YueKaData ykData = null;
                lock (client.ClientData.YKDetail)
                {
                    ykData = client.ClientData.YKDetail.ToYueKaData();
                }
                GameManager.ClientMgr.SendToClient(client,  DataHelper.ObjectToBytes<YueKaData>(ykData), nID);
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessGetYueKaData", false);
            }

            return TCPProcessCmdResults.RESULT_DATA;
        }

        public static TCPProcessCmdResults ProcessGetYueKaAward(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, global::Server.Protocol.TCPOutPacketPool pool, int nID, byte[] data, int count, out global::Server.Protocol.TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                fields = cmdData.Split(':');
                if (2 != fields.Length)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int day = Convert.ToInt32(fields[1]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                YueKaError err = _GetYueKaAward(client, day);
                string cmd = string.Format("{0}:{1}:{2}", roleID, (int)err, day);
                GameManager.ClientMgr.SendToClient(client, cmd, nID);
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessGetYueKaData", false);
            }

            return TCPProcessCmdResults.RESULT_DATA;
        }

        private static YueKaError _GetYueKaAward(GameClient client, int day)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
            {
                return YueKaError.YK_CannotAward_HasNotYueKa;
            }

            //传来的天数错误
            if (day <= 0 || day > DAYS_PER_YUE_KA)
            {
                return YueKaError.YK_CannotAward_ParamInvalid;
            }

            lock (client.ClientData.YKDetail)
            {
                //非月卡用户不可领取
                if (client.ClientData.YKDetail.HasYueKa == 0)
                {
                    return YueKaError.YK_CannotAward_HasNotYueKa;
                }

                //过去的天数不可领取
                if (day < client.ClientData.YKDetail.CurDayOfPerYueKa())
                {
                    return YueKaError.YK_CannotAward_DayHasPassed;
                }

                //未来的天数不可领取
                if (day > client.ClientData.YKDetail.CurDayOfPerYueKa())
                {
                    return YueKaError.YK_CannotAward_TimeNotReach;
                }

                string awardInfo = client.ClientData.YKDetail.AwardInfo;
                //今日已领取
                if (awardInfo.Length < day || awardInfo[day - 1] == '1')
                {
                    return YueKaError.YK_CannotAward_AlreadyAward;
                }

                YueKaAward awardData = null;
                AllGoodsDict.TryGetValue(day, out awardData);
                if (awardData == null)
                {
                    return YueKaError.YK_CannotAward_ConfigError;
                }

                List<GoodsData> goodsDataList = awardData.GetGoodsByOcc(Global.CalcOriginalOccupationID(client));
                if (goodsDataList != null && goodsDataList.Count > 0)
                {
                    if (!Global.CanAddGoodsNum(client, goodsDataList.Count))
                    {
                        return YueKaError.YK_CannotAward_BagNotEnough;
                    }

                    foreach (var goodsData in goodsDataList)
                    {
                        //向DBServer请求加入某个新的物品到背包中
                        goodsData.Id = Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, string.Format("第{0}天月卡返利", awardData.Day), false,
                                                                        goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, true);
                    }       
                }

                //发钻石
                GameManager.ClientMgr.AddUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, awardData.BindZuanShi, string.Format("第{0}天月卡返利", awardData.Day));

               // _SendAward2Player(client, awardData);

                client.ClientData.YKDetail.AwardInfo = awardInfo.Substring(0, day - 1) + "1";
                _UpdateYKDetail2DB(client, client.ClientData.YKDetail);
                if (client._IconStateMgr.CheckFuLiYueKaFanLi(client))
                {
                    client._IconStateMgr.SendIconStateToClient(client);
                }
            }

            return YueKaError.YK_Success;
        }

        /// <summary>
        /// 给客户端发奖
        /// </summary>
        /// <param name="client"></param>
        /// <param name="award"></param>
        private static void _SendAward2Player(GameClient client, YueKaAward award)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
            {
                return;
            }

            List<GoodsData> goodsDataList = award.GetGoodsByOcc(Global.CalcOriginalOccupationID(client));

            // 背包不足则邮件发送
            if (!Global.CanAddGoodsNum(client, goodsDataList.Count))
            {
                foreach (var item in goodsDataList)
                {
                    Global.UseMailGivePlayerAward(client, item, Global.GetLang("月卡返利"), string.Format(Global.GetLang("第{0}天月卡返利"), award.Day));
                }
            }
            else
            {
                foreach (var goodsData in goodsDataList)
                {
                    //向DBServer请求加入某个新的物品到背包中
                    goodsData.Id = Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsData.GoodsID, goodsData.GCount, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, 0, goodsData.Jewellist, true, 1, string.Format("第{0}天月卡返利", award.Day), false,
                                                                    goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, true);
                }
            }

            //发钻石
            GameManager.ClientMgr.AddUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, award.BindZuanShi, string.Format("第{0}天月卡返利", award.Day));
        }

        /// <summary>
        /// 玩家登录时，检测月卡的有效性信息
        /// </summary>
        /// <param name="gameClient"></param>
        public static void CheckValid(GameClient client)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1)) return;

            if (client == null) return;

            lock (client.ClientData.YKDetail)
            {
                if (client.ClientData.YKDetail.HasYueKa == 0) return;

                while (true)
                {
                    //判断月卡是否过期
                    int todayOffset = Global.GetOffsetDay(TimeUtil.NowDateTime());
                    if (todayOffset >= client.ClientData.YKDetail.EndOffsetDay)
                    {
                        client.ClientData.YKDetail.HasYueKa = 0;
                        break;
                    }

                    //判断记录的领奖信息是否和现在处于同一个月卡周期中
                    //首先计算当前记录的领奖是从那一天开始的
                    int curBegOffsetDay = client.ClientData.YKDetail.CurOffsetDay - client.ClientData.YKDetail.AwardInfo.Length + 1;

                    //当前记录的领奖信息和现在不是同一个领奖周期了，那么重新开始计算
                    if (todayOffset >= curBegOffsetDay + DAYS_PER_YUE_KA)
                    {
                        client.ClientData.YKDetail.CurOffsetDay = todayOffset;
                        client.ClientData.YKDetail.AwardInfo = "";
                        //现在的领奖周期中未领的天数置0
                        for (int i = curBegOffsetDay + DAYS_PER_YUE_KA; i <= todayOffset; ++i)
                        {
                            client.ClientData.YKDetail.AwardInfo += "0";
                        }
                        break;
                    }

                    //走到这里就说明，月卡未过期并且记录的领奖信息和现在处于同一个周期中，那么只需要对中间间隔的天数补0
                    for (int i = client.ClientData.YKDetail.CurOffsetDay + 1; i <= todayOffset; ++i)
                    {
                        client.ClientData.YKDetail.AwardInfo += "0";
                    }
                    client.ClientData.YKDetail.CurOffsetDay = todayOffset;

                    break;
                }

                _UpdateYKDetail2DB(client, client.ClientData.YKDetail);
            }
        }

        /// <summary>
        /// 保存月卡信息到数据库,外部需保证线程安全
        /// </summary>
        /// <param name="yueKaDetail"></param>
        private static void _UpdateYKDetail2DB(GameClient client, YueKaDetail YKDetail)
        {
            string value = client.ClientData.YKDetail.SerializeToString();

            //更新缓存同时写到数据库去
            Global.SaveRoleParamsStringToDB(client, RoleParamName.YueKaInfo, value, true);
        }

        /// <summary>
        /// 新的一天来了，这家伙还在线
        /// </summary>
        /// <param name="client"></param>
        public static void UpdateNewDay(GameClient client)
        {
            // 如果1.4.1的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot4Dot1))
            {
                return;
            }

            if (client == null)
            {
                return;
            }

            CheckValid(client);
            lock (client.ClientData.YKDetail)
            {
                if (client._IconStateMgr.CheckFuLiYueKaFanLi(client))
                {
                    client._IconStateMgr.SendIconStateToClient(client);
                }

                /*
                if (client.ClientData.YKDetail.HasYueKa == 0)
                {
                    GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.HasYueKa, 0);
                }
                */

                // modify by chenjingui. 20150624 月卡改为通知剩余天数，跨天了，通知一次
                GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.YueKaRemainDay, client.ClientData.YKDetail.RemainDayOfYueKa());
            }
        }
    }
}