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
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    /// <summary>
    /// 充值回馈
    /// </summary>
    public  class RechargeRepayActiveMgr
    {
        private static int GetBtnIndexState(int money,int minMoney, bool recode )
        {
            //未达到领取条件
            int ret = 0;
            //已领取
            if(money>=minMoney&&recode)
            {
                ret = 2;
            }
            //可领取
            if (money >= minMoney && recode == false)
            {
                ret = 1;
            }
            return ret;
        }

        /// <summary>
        /// 领取奖励按钮状态，转换为字符串
        /// </summary>
        /// <param name="client"></param>
        /// <param name="money"></param>
        /// <param name="type"></param>
        /// <param name="records"></param>
        /// <returns></returns>
        static string GetBtnIndexStateListStr(GameClient client, int money, ActivityTypes type,string[] records)
        {
            Activity instActivity = Global.GetActivity(type);
            if (null == instActivity)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("GetBtnIndexStateListStr Params Error: type={0}", type));
                return "";
            }

            List<int> condision = instActivity.GetAwardMinConditionlist();
            string ret = "";
            //if (type == ActivityTypes.MeiRiChongZhiHaoLi)
            //{
            //    for (int i = 0; i < condision.Count; i++)
            //    {
            //        ret += GetBtnIndexState(money, condision[i], !Global.CanGetDayChongZhiDaLi(client, i + 1));
            //        if (i < condision.Count - 1)
            //            ret += ",";

            //    }
            //    return ret;
           // }else
            {
                
                for (int i = 0; i < condision.Count; i++)
                {
                    bool rec = false;
                    if (i < records.Length)
                        rec = records[i] == "2" ? true : false;
                    ret += GetBtnIndexState(money, condision[i], rec);
                    if (i < condision.Count - 1)
                        ret += ",";

                }
                return ret;
             }
        }
        /// <summary>
        /// 获取所有活动信息，此方法只获取充值和消费钱数
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
        public static TCPProcessCmdResults QueryAllRechargeRepayActiveInfo(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string[] fields = null;
            if (!GetCmdDataField(socket, nID, data, count, out fields))
            {
                return TCPProcessCmdResults.RESULT_FAILED;
            }

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
            
            int totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);
            totalChongZhiMoney = Global.TransMoneyToYuanBao(totalChongZhiMoney);

            int totalChongZhiMoneyToday = GameManager.ClientMgr.QueryTotaoChongZhiMoneyToday(client);
            totalChongZhiMoneyToday = Global.TransMoneyToYuanBao(totalChongZhiMoneyToday);

            string[] dbFields = null;
            TCPProcessCmdResults retcmd = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", roleID, (int)ActivityTypes.TotalConsume), out dbFields, client.ServerId);
            if (null == dbFields)
                return TCPProcessCmdResults.RESULT_FAILED;
            if (dbFields.Length != 3)
                return TCPProcessCmdResults.RESULT_FAILED;
            int totalusedmoney = Global.SafeConvertToInt32(dbFields[2]);
            //totalusedmoney = Global.TransMoneyToYuanBao(totalusedmoney);
            string strcmd = string.Format("{0}:{1}:{2}:{3}", totalChongZhiMoney, totalChongZhiMoneyToday, totalChongZhiMoney, totalusedmoney);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            return TCPProcessCmdResults.RESULT_DATA;
        }
        /// <summary>
        /// 查询回馈活动信息，首冲，每日充值，累计充值，累计消费
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
        public static TCPProcessCmdResults QueryRechargeRepayActive(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool , TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string[] fields = null;

            if (!GetCmdDataField(socket, nID, data, count, out fields))
            {
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            if (fields.Length != 2)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                    (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            int roleID = Convert.ToInt32(fields[0]);
            int activeid = Global.SafeConvertToInt32(fields[1]);
            GameClient client = GameManager.ClientMgr.FindClient(socket);
            if (null == client || client.ClientData.RoleID != roleID)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                return TCPProcessCmdResults.RESULT_FAILED;
            }
            int totalChongZhiMoney = 0;

            // 历史遗留的组合方式 "返回参数1:返回参数2:活动类型"
            // 为什么不把活动类型放在第一位呢，我现在没有返回参数2，还得补个"0"
            string resoult = "";

            string[] dbFields = null;
            switch((ActivityTypes)activeid)
            {
                case ActivityTypes.InputFirst:
                    {
                        totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);
                        totalChongZhiMoney = Global.TransMoneyToYuanBao(totalChongZhiMoney);
                        resoult =RechargeRepayActiveMgr.GetBtnIndexState(totalChongZhiMoney,1,!(Global.CanGetFirstChongZhiDaLiByUserID(client))) + ":" + totalChongZhiMoney;
                    }
                    break;
                case ActivityTypes.MeiRiChongZhiHaoLi:
                    {
                        int totalChongZhiMoneyToday = GameManager.ClientMgr.QueryTotaoChongZhiMoneyToday(client);
                        totalChongZhiMoney = Global.TransMoneyToYuanBao(totalChongZhiMoneyToday);

                        TCPProcessCmdResults retcmd = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", fields[0], (int)ActivityTypes.MeiRiChongZhiHaoLi), out dbFields, client.ServerId);
                        if (null == dbFields)
                            return TCPProcessCmdResults.RESULT_FAILED;
                        if (dbFields.Length != 3)
                            return TCPProcessCmdResults.RESULT_FAILED;
                        string[] rec = dbFields[1].Split(',');
                        resoult = RechargeRepayActiveMgr.GetBtnIndexStateListStr(client, totalChongZhiMoney, ActivityTypes.MeiRiChongZhiHaoLi, rec);
                        resoult += ":" + totalChongZhiMoney;

                    }
                    break;
                case ActivityTypes.TotalCharge:
                    {
                        totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);
                        totalChongZhiMoney = Global.TransMoneyToYuanBao(totalChongZhiMoney);
                        TCPProcessCmdResults retcmd = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", fields[0], (int)ActivityTypes.TotalCharge), out dbFields, client.ServerId);
                        if (null == dbFields)
                            return TCPProcessCmdResults.RESULT_FAILED;
                        if (dbFields.Length != 3)
                            return TCPProcessCmdResults.RESULT_FAILED;
                        string[] rec = dbFields[1].Split(',');
                       resoult= RechargeRepayActiveMgr.GetBtnIndexStateListStr(client, totalChongZhiMoney,ActivityTypes.TotalCharge,rec);
                       resoult = string.Format("{0}:{1}", resoult, totalChongZhiMoney);
                    }
                    break;
                case ActivityTypes.TotalConsume:
                    {
                        TCPProcessCmdResults retcmd = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", fields[0], fields[1]), out dbFields, client.ServerId);
                        if (null == dbFields)
                            return TCPProcessCmdResults.RESULT_FAILED;
                        if (dbFields.Length != 3)
                            return TCPProcessCmdResults.RESULT_FAILED;
                        int totalusedmoney = Global.SafeConvertToInt32(dbFields[2]);
                       // totalusedmoney = Global.TransMoneyToYuanBao(totalusedmoney);
                        string[] rec = dbFields[1].Split(',');
                        resoult = RechargeRepayActiveMgr.GetBtnIndexStateListStr(client, totalusedmoney, ActivityTypes.TotalConsume, rec);
                        resoult = string.Format("{0}:{1}", resoult, totalusedmoney);
                    }
                    break;
                // 合服登陆豪礼活动
                case ActivityTypes.HeFuLogin:
                    {
                        /*HeFuLoginActivity instance = HuodongCachingMgr.GetHeFuLoginActivity();
                        if (null == instance)
                        {
                            return TCPProcessCmdResults.RESULT_FAILED;
                        }
                        // 不在活动时间内
                        if (!instance.InActivityTime() && !instance.InAwardTime())
                        {
                            return TCPProcessCmdResults.RESULT_FAILED;
                        }*/
                        
                        // 把玩家的登陆标记发送给玩家
                        resoult = string.Format("{0}:0", Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuLoginFlag).ToString() );
                    }
                    break;
                // 合服累计登陆活动
                case ActivityTypes.HeFuTotalLogin:
                    {
                        /*HeFuTotalLoginActivity instance = HuodongCachingMgr.GetHeFuTotalLoginActivity();
                        if (null == instance)
                        {
                            return TCPProcessCmdResults.RESULT_FAILED;
                        }
                        // 不在活动时间内
                        if (!instance.InActivityTime() && !instance.InAwardTime())
                        {
                            return TCPProcessCmdResults.RESULT_FAILED;
                        }*/
                        // 把玩家的登陆标记发送给玩家
                        resoult = string.Format("{0}:{1}",
                            Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuTotalLoginNum).ToString(),
                            Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuTotalLoginFlag).ToString());
                    }
                    break;
                // 合服战场之神活动
                case ActivityTypes.HeFuPKKing:
                    {
                        /*HeFuPKKingActivity instance = HuodongCachingMgr.GetHeFuPKKingActivity();
                        if (null == instance)
                        {
                            return TCPProcessCmdResults.RESULT_FAILED;
                        }
                        // 不在活动时间内
                        if (!instance.InActivityTime() && !instance.InAwardTime())
                        {
                            return TCPProcessCmdResults.RESULT_FAILED;
                        }*/
                        // 把玩家的登陆标记发送给玩家
                        resoult = string.Format("{0}:{1}", HuodongCachingMgr.GetHeFuPKKingRoleID(), Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuPKKingFlag).ToString());
                    }
                    break;
                // 玩家向GameDBServer请求昨天的充值排行榜和自己的充值积分
                case ActivityTypes.HeFuRecharge:
                    {
                        // 不要太频繁
                        // do something
                        // [JIRA] (MUBUG-333) 【充值返利】充值返利活动结束后第二天还可以累计返利金额
                        // 策划要求，合服充值返利界面显示今天排行帮
                        HeFuRechargeActivity instance = HuodongCachingMgr.GetHeFuRechargeActivity();
                        if (null == instance)
                        {
                            resoult = string.Format("{0}:{1}", "0", "0");
                        }
                        else if (!instance.InActivityTime() && !instance.InAwardTime())
                        {
                            resoult = string.Format("{0}:{1}", "0", "0");
                        }
                        else
                        {
                            int hefuday = Global.GetOffsetDay(Global.GetHefuStartDay());

                            // 向DB申请数据
                            TCPProcessCmdResults retcmd = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO,
                                string.Format("{0}:{1}:{2}:{3}:{4}", fields[0], fields[1], hefuday, Global.GetOffsetDay(DateTime.Parse(instance.ToDate)), instance.strcoe), out dbFields, client.ServerId);
                            if (null == dbFields)
                                return TCPProcessCmdResults.RESULT_FAILED;

                            if (null == dbFields || 4 != dbFields.Length)
                                return TCPProcessCmdResults.RESULT_FAILED;

                            resoult = string.Format("{0}:{1}", dbFields[2], dbFields[3]);
                        }
                        
                    }
                    break;
                case ActivityTypes.HeFuLuoLan:
                    {
                        // 向客户端发送罗兰城战记录和合服罗兰城主活动领奖记录
                        string strHefuLuolanGuildid = GameManager.GameConfigMgr.GetGameConfigItemStr(GameConfigNames.hefu_luolan_guildid, "");
                        resoult = string.Format("{0}:{1}", strHefuLuolanGuildid, Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuLuoLanAwardFlag).ToString());
                    }
                    break;
                default:
                    break;
            }
            
            string strcmd = string.Format("{0}:{1}", resoult,activeid);
            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            return TCPProcessCmdResults.RESULT_DATA;
            
        }

        /// <summary>
        /// 充值回馈 是否达到奖励条件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="type"></param>
        /// <param name="hasGet">是否已全部领取,客户端根据此状态确定主界面充值图标的显示等</param>
        /// <returns></returns>
        public static bool CheckRechargeReplay(GameClient client, ActivityTypes type, out bool hasGet)
        {
            hasGet = false;
            try
            {
                //switch (type)
                //{
                //    case ActivityTypes.MeiRiChongZhiHaoLi:
                //        {
                //            int totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoneyToday(client);
                //            totalChongZhiMoney = Global.TransMoneyToYuanBao(totalChongZhiMoney);
                //            string[] dbFields = null;
                //            TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", client.ClientData.RoleID, type), out dbFields);

                //            string[] rec = dbFields[1].Split(',');
                //            string resoult = RechargeRepayActiveMgr.GetBtnIndexStateListStr(client, totalChongZhiMoney, ActivityTypes.TotalCharge, rec);
                //            string[] retlist = resoult.Split(',');
                //            foreach (string st in retlist)
                //            {
                //                if (st.Equals("1"))
                //                    return true;
                //            }
                //        }
                //        break;
                //    case ActivityTypes.TotalCharge:
                //        {
                //            int totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);
                //            totalChongZhiMoney = Global.TransMoneyToYuanBao(totalChongZhiMoney);
                //            string[] dbFields = null;
                //            TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", client.ClientData.RoleID, type), out dbFields);

                //            string[] rec = dbFields[1].Split(',');
                //            string resoult = RechargeRepayActiveMgr.GetBtnIndexStateListStr(client, totalChongZhiMoney, ActivityTypes.TotalCharge, rec);
                //            string[] retlist = resoult.Split(',');
                //            foreach (string st in retlist)
                //            {
                //                if (st.Equals("1"))
                //                    return true;
                //            }
                //        }
                //        break;
                //    case ActivityTypes.TotalConsume:
                //        {
                            string[] dbFields = null;
                            TCPProcessCmdResults retcmd = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", client.ClientData.RoleID, (int)type), out dbFields, client.ServerId);
                            if (retcmd == TCPProcessCmdResults.RESULT_DATA && dbFields != null)
                            {
                                int extdata = Global.SafeConvertToInt32(dbFields[2]);
                                string[] rec = dbFields[1].Split(',');
                                string resoult = RechargeRepayActiveMgr.GetBtnIndexStateListStr(client, extdata, type, rec);
                                string[] retlist = resoult.Split(',');
                                hasGet = retlist.Length > 0 && retlist.All((x) => { return x.Equals("2"); });
                                foreach (string st in retlist)
                                {
                                    if (st.Equals("1"))
                                        return true;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        //}
                        //break;
               // }
            }
            catch (Exception e)
            {
                DataHelper.WriteFormatExceptionLog(e, Global.GetDebugHelperInfo(client.ClientSocket), false);
                return false;
            }
           
            return false;
        }
        /// <summary>
        /// 获取首次充值信息
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        static string GetFirstChargeInfo(GameClient client)
        {
            int totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);
            string resoult =  ""+(Global.CanGetFirstChongZhiDaLiByUserID(client) == true ? 1 : 0) + totalChongZhiMoney + ":" +(int)ActivityTypes.InputFirst;
            string strcmd = string.Format("{0}", resoult);
            return strcmd;
        }
        static string GetDailyChargeActiveInfo(GameClient client)
        {
            string strcmd="";
            int totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);
            return strcmd;
        }

         /// <summary>
         /// 解析
         /// </summary>
         /// <param name="socket"></param>
         /// <param name="nID"></param>
         /// <param name="data"></param>
         /// <param name="count"></param>
         /// <param name="fields"></param>
         /// <returns></returns>
        static bool GetCmdDataField(TMSKSocket socket, int nID, byte[] data, int count, out string[] fields)
        {
            string cmdData = null;
            fields = null;
             try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception) //解析错误
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return false;
            }

             //解析用户名称和用户密码
             fields = cmdData.Split(':');
             return true;
        }
        
        /// 精灵从服务器端获取首次充值的大礼包
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static TCPProcessCmdResults GetFirstChargeAward(TMSKSocket socket, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
           
            string[] fields = null;
            if (!GetCmdDataField(socket, nID, data, count, out fields))
            {
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
               
                if (fields.Length != 3)
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

                string strcmd = "";

                if (client.ClientData.CZTaskID > 0)
                {
                    strcmd = string.Format("{0}:{1}:{2}:", -10, (int)ActivityTypes.InputFirst, 1);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                Activity instActivity = Global.GetActivity(ActivityTypes.InputFirst);
                if (null == instActivity)
                {
                    strcmd = string.Format("{0}:{1}:{2}:", -1, (int)ActivityTypes.InputFirst, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (!Global.CanGetFirstChongZhiDaLiByUserID(client))
                {
                    strcmd = string.Format("{0}:{1}:{2}:", -10, (int)ActivityTypes.InputFirst, 0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                // 判断背包是否够用
                if (!instActivity.HasEnoughBagSpaceForAwardGoods(client))
                {
                    strcmd = string.Format("{0}:{1}:{2}:", -20, (int)ActivityTypes.InputFirst, 0);                                // 背包不够
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                int totalChongZhiMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);

                if (totalChongZhiMoney <= 0)
                {
                    strcmd = string.Format("{0}:{1}:{2}:", -30, (int)ActivityTypes.InputFirst,0);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                int nOcc = Global.CalcOriginalOccupationID(client);

                instActivity.GiveAward(client);

                //判断完成充值任务ID
                Global.JugeCompleteChongZhiSecondTask(client, 1);

                //首冲大礼领取提示  ***********需要修改提示内容
                Global.BroadcastShouChongDaLiHint(client);

                client._IconStateMgr.CheckShouCiChongZhi(client);
                strcmd = string.Format("{0}:{1}:{2}:", 0, (int)ActivityTypes.InputFirst,2);
                
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
               
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static string BuildWriteActiveRecordStr(string record, int nBtnIndex)
        {
            string activeRecord = "";
            string[] recordlist = record.Split(',');

            List<string> writeRecord = new List<string>();
            int cout=nBtnIndex;
            if(nBtnIndex<recordlist.Length)
                cout=recordlist.Length;
            for(int i=0;i<cout;i++)
            {
                if(i<recordlist.Length)
                    writeRecord.Add(recordlist[i]);
                else
                {
                    writeRecord.Add("1");//默认是未领取  
                }
                
             }
            writeRecord[nBtnIndex-1]="2";//3代表领取
            for (int i=0;i<writeRecord.Count;i++) 
            {
                activeRecord += writeRecord[i];
                if (i < writeRecord.Count - 1)
                    activeRecord += ",";
            }
            return activeRecord;
        }
        /// <summary>
        /// 通用获得奖励方法
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
        public static TCPProcessCmdResults ProcessGetRepayAwardCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
           
            string[] fields = null;
            if (!GetCmdDataField(socket, nID, data, count, out fields))
            {
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                int nRoleID = Convert.ToInt32(fields[0]);
                int nActivityType = Global.SafeConvertToInt32(fields[1]);
                int nBtnIndex = Convert.ToInt32(fields[2]);
                //int nRoleID = count;
                //int nActivityType = (int)ActivityTypes.HeFuRecharge;
                //int nBtnIndex = 0;

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != nRoleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                string strcmd = "";

                Activity instActivity = Global.GetActivity((ActivityTypes)nActivityType);
                if (null == instActivity)
                {
                    strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ACTIVITY_NOTEXIST, nActivityType);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                // 默认返回成功结果
                int result = (int)ActivityErrorType.RECEIVE_SUCCEED;
                string nRetValue = ""; 

                ActivityTypes tmpActType = (ActivityTypes)nActivityType;
                switch (tmpActType)
                {

                    case ActivityTypes.InputFirst:
                        {
                            return RechargeRepayActiveMgr.GetFirstChargeAward(socket, pool, nID, data, count, out tcpOutPacket);
                        }
                    case ActivityTypes.MeiRiChongZhiHaoLi:
                        {
                           //return RechargeRepayActiveMgr.GetDailyChargeAward(client, nBtnIndex, instActivity, pool, nID, out tcpOutPacket);
                            // 判断背包是否够用
                            if (!instActivity.HasEnoughBagSpaceForAwardGoods(client, nBtnIndex))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.BAG_NOTENOUGH, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 判断是否已经领取
                            string[] dbFields = null;
                            TCPProcessCmdResults retcmd = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", nRoleID, (int)tmpActType), out dbFields, client.ServerId);
                            if (dbFields == null)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            if (dbFields != null && dbFields.Length != 3)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            int retcode = Global.SafeConvertToInt32(dbFields[0]);
                            if (retcode != 1)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            string[] retIndexarry = dbFields[1].Split(',');

                            if (nBtnIndex > 0 && nBtnIndex <= retIndexarry.Length && retIndexarry[nBtnIndex - 1] == "2")
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ALREADY_GETED, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            AwardItem tmp = instActivity.GetAward(client, nBtnIndex);
                            if (tmp == null)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ACTIVITY_NOTEXIST, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            int totalChongZhiMoneyToday = GameManager.ClientMgr.QueryTotaoChongZhiMoneyToday(client);
                            totalChongZhiMoneyToday = Global.TransMoneyToYuanBao(totalChongZhiMoneyToday);
                            if (totalChongZhiMoneyToday < tmp.MinAwardCondionValue)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.MINAWARDCONDIONVALUE, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                            //保存数据库
                            string[] dbFields2 = null;
                            string writerec = RechargeRepayActiveMgr.BuildWriteActiveRecordStr(dbFields[1], nBtnIndex);
                            Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_GET_REPAYACTIVEAWARD, string.Format("{0}:{1}:{2}", nRoleID, (int)tmpActType, writerec.Replace(",", "")), out dbFields2, client.ServerId);
                            if (dbFields2 == null || dbFields2.Length != 3)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            //添加物品
                            instActivity.GiveAward(client, nBtnIndex);

                            //每日充值大礼领取提示
                            Global.BroadcastDayChongDaLiHint(client);
                            client._IconStateMgr.CheckMeiRiChongZhi(client);
                            nRetValue = writerec;
                            break;
                            
                        }
                    case ActivityTypes.TotalCharge:
                        {
                            // 判断背包是否够用
                            if (!instActivity.HasEnoughBagSpaceForAwardGoods(client,nBtnIndex))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.BAG_NOTENOUGH, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            string[] dbFields = null;
                            TCPProcessCmdResults retcmd = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", nRoleID, (int)tmpActType), out dbFields, client.ServerId);
                            if (dbFields == null)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            if (dbFields != null && dbFields.Length != 3)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            int retcode = Global.SafeConvertToInt32(dbFields[0]);
                            if (retcode != 1)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            string[] retIndexarry = dbFields[1].Split(',');
                            
                                
                            //判断是否已领取
                            if (nBtnIndex > 0 && nBtnIndex <= retIndexarry.Length && retIndexarry[nBtnIndex - 1] == "2")
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ALREADY_GETED, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            //判断是否达到领取条件，包括背包空间，充值最低要求
                            int totalMoney = GameManager.ClientMgr.QueryTotaoChongZhiMoney(client);
                            // 之前忘记了乘以元宝比例
                            totalMoney = Global.TransMoneyToYuanBao(totalMoney);

                            if (!instActivity.CanGiveAward(client, nBtnIndex, totalMoney))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                            string[] dbFields2 = null;
                            
                            string writerec = RechargeRepayActiveMgr.BuildWriteActiveRecordStr(dbFields[1], nBtnIndex);
                            Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_GET_REPAYACTIVEAWARD, string.Format("{0}:{1}:{2}", nRoleID, (int)ActivityTypes.TotalCharge, writerec.Replace(",", "")), out dbFields2, client.ServerId);
                            if (dbFields2 == null || dbFields2.Length != 3)
                                return TCPProcessCmdResults.RESULT_FAILED;

                            //添加物品
                            instActivity.GiveAward(client, nBtnIndex);
                            RechargeRepayActiveMgr.BroadcastActiveHint(client, ActivityTypes.TotalCharge);
                            client._IconStateMgr.CheckLeiJiChongZhi(client);
                            nRetValue = writerec;
                            break;
                        }
                    case ActivityTypes.TotalConsume:
                        {
                            // 判断背包是否够用
                            if (!instActivity.HasEnoughBagSpaceForAwardGoods(client,nBtnIndex))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.BAG_NOTENOUGH, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            
                            string[] dbFields=null;
                            TCPProcessCmdResults retcmd = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", nRoleID, (int)ActivityTypes.TotalConsume), out dbFields, client.ServerId);
                            if(dbFields==null)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            if (dbFields != null && dbFields.Length != 3)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            int retcode = Global.SafeConvertToInt32(dbFields[0]);
                            if (retcode != 1)
                                return TCPProcessCmdResults.RESULT_FAILED;
                            string[] retIndexarry = dbFields[1].Split(',');
                            //判断是否已领取
                            if (nBtnIndex <= retIndexarry.Length&&retIndexarry[nBtnIndex-1]=="2")
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ALREADY_GETED, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                               
                            //判断是否达到领取条件
                            int totalMoney = Global.SafeConvertToInt32(dbFields[2]);
                            if (!instActivity.CanGiveAward(client, nBtnIndex, totalMoney))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                            string[] dbFields2 = null; 
                            string writerec = RechargeRepayActiveMgr.BuildWriteActiveRecordStr(dbFields[1],nBtnIndex);
                            Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_GET_REPAYACTIVEAWARD, string.Format("{0}:{1}:{2}", nRoleID, (int)tmpActType, writerec.Replace(",", "")), out dbFields2, client.ServerId);
                            if (dbFields2 == null || dbFields2.Length != 3)
                                return TCPProcessCmdResults.RESULT_FAILED;
                           
                            //添加物品
                            instActivity.GiveAward(client, nBtnIndex);
                            //广播
                            RechargeRepayActiveMgr.BroadcastActiveHint(client, ActivityTypes.TotalConsume);
                            client._IconStateMgr.CheckLeiJiXiaoFei(client);
                            nRetValue = writerec;
                            break;
                        }
                    // 领取合服登陆的奖励
                    case ActivityTypes.HeFuLogin:
                        {
                            // 检查是否在允许领取的时间内
                            if (!instActivity.InAwardTime())
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDTIME_OUT, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            if (null == instActivity.GetAward(nBtnIndex))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDCFG_ERROR, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 领取奖励的类型
                            HeFuLoginAwardType tmpType = (HeFuLoginAwardType)nBtnIndex;
                            HeFuLoginFlagTypes HefuFlag = HeFuLoginFlagTypes.HeFuLogin_Null;
                            // 请求领取普通奖励
                            if (HeFuLoginAwardType.NormalAward == tmpType)
                            {
                                HefuFlag = HeFuLoginFlagTypes.HeFuLogin_NormalAward;
                                
                            }
                            // 如果是VIP奖励
                            else if (HeFuLoginAwardType.VIPAward == tmpType)
                            {
                                // 判断玩家是不是VIP
                                if (!Global.IsVip(client))
                                {
                                    strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.HEFULOGIN_NOTVIP, nActivityType);
                                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                    return TCPProcessCmdResults.RESULT_DATA;
                                }

                                HefuFlag = HeFuLoginFlagTypes.HeFuLogin_VIPAward;
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error, string.Format("TCPProcessCmdResults ProcessGetRepayAwardCmd 领取合服登陆奖励收到无效的领取类型 CMD={0}, Client={1}, RoleID={2}, nBtnIndex={3}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), nRoleID, nBtnIndex));
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                     
                            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuLoginFlag);
                            int nValue =  Global.GetIntSomeBit(nFlag, (int)HeFuLoginFlagTypes.HeFuLogin_Login);
                            // 是否在活动期间登陆过
                            if (0 == nValue)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 检查是否已经领取
                            nValue = Global.GetIntSomeBit(nFlag, (int)HefuFlag);
                            if (0 != nValue)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ALREADY_GETED, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 判断背包是否够用
                            if (!instActivity.HasEnoughBagSpaceForAwardGoods(client, nBtnIndex))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.BAG_NOTENOUGH, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 给玩家奖励
                            instActivity.GiveAward(client, nBtnIndex);

                            // 设置领奖标记
                            nFlag = Global.SetIntSomeBit((int)HefuFlag, nFlag, true);

                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HeFuLoginFlag, nFlag, true);
                            nRetValue = string.Format("{0}", nFlag);

                            if (client._IconStateMgr.CheckHeFuActivity(client))
                                client._IconStateMgr.SendIconStateToClient(client);
                        }
                        break;
                    // 领取合服累计登陆奖励
                    case ActivityTypes.HeFuTotalLogin:
                        {
                            // 检查是否在允许领取的时间内
                            if (!instActivity.InAwardTime())
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDTIME_OUT, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            if (null == instActivity.GetAward(nBtnIndex))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDCFG_ERROR, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // nBtnIndex 代表玩家要领取第几天奖励
                            int totalloginnum = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuTotalLoginNum);
                            // 当玩家在活动时间内总登陆数小于领奖条件时
                            if (totalloginnum < nBtnIndex)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 判断是否已经领取
                            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuTotalLoginFlag);
                            int nValue = Global.GetIntSomeBit(nFlag, nBtnIndex);
                            if (0 != nValue)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ALREADY_GETED, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 判断背包是否够用
                            if (!instActivity.HasEnoughBagSpaceForAwardGoods(client, nBtnIndex))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.BAG_NOTENOUGH, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 给玩家奖励
                            instActivity.GiveAward(client, nBtnIndex);

                            // 设置领奖标记
                            //nFlag = nFlag | Global.GetBitValue(nBtnIndex);
                            nFlag = Global.SetIntSomeBit(nBtnIndex, nFlag, true);
                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HeFuTotalLoginFlag, nFlag, true);
                            nRetValue = string.Format("{0}", nFlag);

                            if (client._IconStateMgr.CheckHeFuActivity(client))
                                client._IconStateMgr.SendIconStateToClient(client);
                        }
                        break;
                    // 领取合服战场之王奖励
                    case ActivityTypes.HeFuPKKing:
                        {
                            // 检查是否在允许领取的时间内
                            if (!instActivity.InAwardTime())
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDTIME_OUT, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            /*if (null == instActivity.GetAward())
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDCFG_ERROR, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }*/

                            // 判断玩家是不是战场之神
                            if (nRoleID != HuodongCachingMgr.GetHeFuPKKingRoleID())
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 判断是否已经领取
                            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuPKKingFlag);
                            if (nFlag != 0)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ALREADY_GETED, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 判断背包是否够用
                            if (!instActivity.HasEnoughBagSpaceForAwardGoods(client, nBtnIndex))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.BAG_NOTENOUGH, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 给玩家奖励
                            instActivity.GiveAward(client);

                            // 设置领奖标记
                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HeFuPKKingFlag, 1, true);
                            nRetValue = string.Format("{0}", Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuPKKingFlag).ToString());
                            //nRetValue = string.Format("{0}|{1}", Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuPKKingFlag).ToString(), nBtnIndex.ToString());

                            if (client._IconStateMgr.CheckHeFuActivity(client))
                                client._IconStateMgr.SendIconStateToClient(client);
                        }
                        break;
                    // 领取合服充值返利奖励
                    case ActivityTypes.HeFuRecharge:
                        {
                            // 不让玩家有过多的领取操作
                            int currday = Global.GetOffsetDay(TimeUtil.NowDateTime());
                            int hefuday = Global.GetOffsetDay(Global.GetHefuStartDay());
                            // 活动开始的第一天没有数据
                            if (currday == hefuday)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDTIME_OUT, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                            HeFuRechargeActivity instance = HuodongCachingMgr.GetHeFuRechargeActivity();
                            if (null == instance)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ACTIVITY_NOTEXIST, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                            if (!instance.InAwardTime())
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDTIME_OUT, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                            // 不要太频繁
                            // do something

                            string[] dbFields = null;
                            Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_GET_REPAYACTIVEAWARD, string.Format("{0}:{1}:{2}:{3}:{4}", nRoleID, (int)tmpActType, hefuday, Global.GetOffsetDay(DateTime.Parse(instance.ToDate)), instance.strcoe), out dbFields, client.ServerId);
                            if (null == dbFields || 3 != dbFields.Length)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.FATALERROR, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            if (instance.ActivityType != Convert.ToInt32(dbFields[1]))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.FATALERROR, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            int userrebate = Convert.ToInt32(dbFields[2]);
                            if (userrebate <= 0)
                            {
                                // 没有可领取的充值返利
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            string huoDongKeyStr = hefuday + "_" + Global.GetOffsetDay(DateTime.Parse(instance.ToDate));

                            // 真正的发奖 db收到给钻石的消息，会给用户增加领取记录
                            if (!GameManager.ClientMgr.AddUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                 client, userrebate, string.Format(Global.GetLang("领取{0}活动奖励"), instance.ActivityType), ActivityTypes.HeFuRecharge, huoDongKeyStr))
                            {
                                // 没有可领取的充值返利
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                                           StringUtil.substitute(Global.GetLang("恭喜获得钻石 +{0}"), userrebate),
                                                                                           GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyErr, (int)HintErrCodeTypes.None);
                            //添加获取元宝记录
                            GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ADDGIVEUSERMONEYITEM, string.Format("{0}:{1}:{2}",
                                client.ClientData.RoleID, userrebate, string.Format(Global.GetLang("领取{0}活动奖励"), instance.ActivityType)),
                                null, client.ServerId);

                            if (client._IconStateMgr.CheckHeFuActivity(client))
                                client._IconStateMgr.SendIconStateToClient(client);

                        }
                        break;
                    case ActivityTypes.HeFuLuoLan:
                        {
                            if (!instActivity.InAwardTime())
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDTIME_OUT, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            HeFuLuoLanAward hefuLuoLanAward = (instActivity as HeFuLuoLanActivity).GetHeFuLuoLanAward(nBtnIndex);
                            // 没有对应的选项
                            if (null == hefuLuoLanAward)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.AWARDCFG_ERROR, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            int guildwinnum = 0;
                            int chengzhuwinnum = 0;
                            int guizuwinnum = 0;

                            // 向客户端发送罗兰城战记录和合服罗兰城主活动领奖记录
                            string strHefuLuolanGuildid = GameManager.GameConfigMgr.GetGameConfigItemStr(GameConfigNames.hefu_luolan_guildid, "");
                            string[] strFields = strHefuLuolanGuildid.Split('|');
                            for (int i = 0; i < strFields.Length; ++i)
                            {
                                string[] strInfos = strFields[i].Split(',');
                                if (2 != strInfos.Length)
                                {
                                    continue;
                                }
                                // 计算帮会胜利过几次
                                if (Convert.ToInt32(strInfos[0]) == client.ClientData.Faction)
                                {
                                    guildwinnum++;
                                    // 计算不是帮主才是贵族
                                    if (Convert.ToInt32(strInfos[1]) == client.ClientData.RoleID)
                                    {
                                    }
                                    else
                                    {
                                        guizuwinnum++;
                                    }
                                }
                                // 计算个人当过几次城主
                                if (Convert.ToInt32(strInfos[1]) == client.ClientData.RoleID)
                                {
                                    chengzhuwinnum++;
                                }
                            }

                            // 帮会的胜利次数不足
                            if (guildwinnum < hefuLuoLanAward.winNum)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 如果是城主领奖励
                            if (1 == hefuLuoLanAward.status)
                            {
                                // 个人没当过那么多次城主 只能点选贵族奖励
                                if (chengzhuwinnum < hefuLuoLanAward.winNum)
                                {
                                    strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                    return TCPProcessCmdResults.RESULT_DATA;
                                }
                            }
                            else if (2 == hefuLuoLanAward.status)
                            {
                                // 城主不能领取贵族的奖励
                                if (guizuwinnum < hefuLuoLanAward.winNum)
                                {
                                    strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.NOTCONDITION, nActivityType);
                                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                    return TCPProcessCmdResults.RESULT_DATA;
                                }
                            }

                            // 判断是否已经领取
                            int nFlag = Global.GetRoleParamsInt32FromDB(client, RoleParamName.HeFuLuoLanAwardFlag);
                            int nValue = Global.GetIntSomeBit(nFlag, nBtnIndex);
                            if (0 != nValue)
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.ALREADY_GETED, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 判断背包是否够用
                            if (!instActivity.HasEnoughBagSpaceForAwardGoods(client, nBtnIndex))
                            {
                                strcmd = string.Format("{0}:{1}::", (int)ActivityErrorType.BAG_NOTENOUGH, nActivityType);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            // 给玩家奖励
                            instActivity.GiveAward(client, nBtnIndex);

                            nFlag = Global.SetIntSomeBit(nBtnIndex, nFlag, true);
                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.HeFuLuoLanAwardFlag, nFlag, true);
                            nRetValue = string.Format("{0}", nBtnIndex);

                            if (client._IconStateMgr.CheckHeFuActivity(client))
                                client._IconStateMgr.SendIconStateToClient(client);
                        }
                        break;
                    default:
                        break;
                }

                strcmd = string.Format("{0}:{1}:{2}", result,nActivityType, nRetValue);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
               
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
              
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        //public static TCPProcessCmdResults GetDailyChargeAward(GameClient client, int nBtnIndex, Activity instActivity, TCPOutPacketPool pool, int nID, out TCPOutPacket tcpOutPacket)
        //{
        //    int nActivityType = instActivity.ActivityType;
        //    string strcmd = "";
        //    // 判断背包是否够用
        //    if (!instActivity.HasEnoughBagSpaceForAwardGoods(client, nBtnIndex))
        //    {
        //        strcmd = string.Format("{0}:{1}::", -20, nActivityType);
        //        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
        //        return TCPProcessCmdResults.RESULT_DATA;
        //    }

        //    // 判断是否已经领取
        //    string[] dbFields = null;
        //    TCPProcessCmdResults retcmd = Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_QUERY_REPAYACTIVEINFO, string.Format("{0}:{1}", nRoleID, (int)tmpActType), out dbFields);
        //    if (dbFields == null)
        //        return TCPProcessCmdResults.RESULT_FAILED;
        //    if (dbFields != null && dbFields.Length != 3)
        //        return TCPProcessCmdResults.RESULT_FAILED;
        //    int retcode = Global.SafeConvertToInt32(dbFields[0]);
        //    if (retcode != 1)
        //        return TCPProcessCmdResults.RESULT_FAILED;
        //    string[] retIndexarry = dbFields[1].Split(',');

        //    if (nBtnIndex <= retIndexarry.Length && retIndexarry[nBtnIndex - 1] == "2")
        //    {
        //        strcmd = string.Format("{0}:{1}::", -10, nActivityType);
        //        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
        //        return TCPProcessCmdResults.RESULT_DATA;
        //    }

        //    AwardItem tmp = instActivity.GetAward(client, nBtnIndex);
        //    if (tmp == null)
        //    {
        //        strcmd = string.Format("{0}:{1}::", -1, nActivityType);
        //        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
        //        return TCPProcessCmdResults.RESULT_DATA;
        //    }

        //    int totalChongZhiMoneyToday = GameManager.ClientMgr.QueryTotaoChongZhiMoneyToday(client);
        //    totalChongZhiMoneyToday = Global.TransMoneyToYuanBao(totalChongZhiMoneyToday);
        //    if (totalChongZhiMoneyToday < tmp.MinAwardCondionValue)
        //    {
        //        strcmd = string.Format("{0}:{1}::", -5, nActivityType);
        //        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
        //        return TCPProcessCmdResults.RESULT_DATA;
        //    }
        //    //保存数据库
        //    string[] dbFields2 = null;
        //    string writerec = RechargeRepayActiveMgr.BuildWriteActiveRecordStr(dbFields[1], nBtnIndex);
        //    Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_DB_GET_REPAYACTIVEAWARD, string.Format("{0}:{1}:{2}", nRoleID, (int)tmpActType, writerec.Replace(",", "")), out dbFields2);
        //    if (dbFields2 == null || dbFields2.Length != 3)
        //        return TCPProcessCmdResults.RESULT_FAILED;
        //    //添加物品
        //    instActivity.GiveAward(client, nBtnIndex);

        //    //完成每日充值大礼的领取
        //   // Global.CompleteDayChongZhiDaLi(client, nBtnIndex);

        //    //每日充值大礼领取提示
        //    Global.BroadcastDayChongDaLiHint(client);
        //    client._IconStateMgr.CheckMeiRiChongZhi(client);
        //    string resout = "";
            
        //    resout=RechargeRepayActiveMgr.GetBtnIndexStateListStr(client, totalChongZhiMoneyToday, ActivityTypes.MeiRiChongZhiHaoLi, null);
        //    strcmd = string.Format("{0}:{1}:{2}:", 0, nActivityType, resout);
        //    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
        //    return TCPProcessCmdResults.RESULT_DATA;
        //}

        /// <summary>
        /// 神装激情回馈领取提示
        /// </summary>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        public static void BroadcastActiveHint(GameClient client,ActivityTypes activeType)
        {
            string broadCastMsg = "";
            string activeStr = "";
            switch (activeType)
            {
                case ActivityTypes.TotalCharge:
                    activeStr = Global.GetLang("累计充值");
                    break;
                case ActivityTypes.TotalConsume:
                    activeStr = Global.GetLang("累计消费");
                    break;
            }
            broadCastMsg = StringUtil.substitute(Global.GetLang("恭喜【{0}】成功领取了{1}回馈的丰厚奖品，让人羡慕不已！"),
                Global.FormatRoleName(client, client.ClientData.RoleName), activeStr);

            //播放用户行为消息
            Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.Bulletin, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
        }
    }
}
