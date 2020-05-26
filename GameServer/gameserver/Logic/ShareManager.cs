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
using System.Xml.Linq;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    class ShareManager
    {
        #region 配置数据
        static List<GoodsData> _ShareGoodslist = null;
        private static object _ShareGoodsMutex = new object();
        /// <summary>
        /// 分享奖励数据
        /// </summary>
       public static List<GoodsData> ShareGoodslist
        {
            get
            {
                if (_ShareGoodslist != null && _ShareGoodslist.Count > 0)
                    return _ShareGoodslist;
                else
                {
                    string info = GameManager.systemParamsList.GetParamValueByName("ShareAward");
                    lock (_ShareGoodsMutex)
                    {
                        _ShareGoodslist = ParseGoodsDataList(info.Split('|'));
                    }

                }
                return _ShareGoodslist;

            }
        }
        /// <summary>
        /// 将物品字符串列表解析成物品数据列表
        /// </summary>
        /// <param name="goodsStr"></param>
        /// <returns></returns>
        private static List<GoodsData> ParseGoodsDataList(string[] fields)
        {
            List<GoodsData> goodsDataList = new List<GoodsData>();
            for (int i = 0; i < fields.Length; i++)
            {
                string[] sa = fields[i].Split(',');
                if (sa.Length != 7)
                {
                    LogManager.WriteLog(LogTypes.Warning, string.Format("解析分享奖励道具失败, 物品配置项个数错误"));
                    continue;
                }

                int[] goodsFields = Global.StringArray2IntArray(sa);

                //获取物品数据  liaowei -- MU 改变 物品ID,物品数量,是否绑定,强化等级,追加等级,是否有幸运,卓越属性
                GoodsData goodsData = Global.GetNewGoodsData(goodsFields[0], goodsFields[1], 0, goodsFields[3], goodsFields[2], 0, goodsFields[5], 0, goodsFields[6], goodsFields[4], 0);
                goodsDataList.Add(goodsData);
            }

            return goodsDataList;
        }
        #endregion 配置数据

        /// <summary>
        /// 判断是否能领取分享奖励
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool CanGetShareAward(GameClient client)
        {
            string oldstr = Global.GetRoleParamByName(client, RoleParamName.DailyShare);
            if (oldstr == null)
                return false;
            string[] fields = oldstr.Split(',');
            string olddayid = fields[0];
            if (olddayid == TimeUtil.NowDateTime().DayOfYear.ToString()&&fields[1]=="0")
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 今日是否已经分享
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool HasDoneShare(GameClient client)
        {
            string oldstr = Global.GetRoleParamByName(client, RoleParamName.DailyShare);
            if (oldstr == null)
                return false;
            string[] fields = oldstr.Split(',');
            string olddayid = fields[0];
            if (olddayid == TimeUtil.NowDateTime().DayOfYear.ToString())
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 处理分享相关命令
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
        public static TCPProcessCmdResults ProcessShareCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            string[] fields = null;
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
                fields = cmdData.Split(':');
                int roleID = Convert.ToInt32(fields[0]);
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                int resoult = 0;
                int extdata = 0;
                switch ((TCPGameServerCmds)nID)
                {
                    case TCPGameServerCmds.CMD_SPR_GETSHARESTATE:
                        if (HasDoneShare(client))
                        {
                            if (CanGetShareAward(client))
                            {
                                extdata = 1;//表示可领取，按钮状态激活
                            }
                            else
                                extdata = 2;//表示已经领取
                        }
                        else
                        {
                            extdata = 0;
                        }
                        break;
                    case TCPGameServerCmds.CMD_SPR_UPDATESHARESTATE:
                        UpdateRoleShareState(client);
                        if (HasDoneShare(client))
                        {
                            if (CanGetShareAward(client))
                            {
                                extdata = 1;//表示可领取，按钮状态激活
                            }
                            else
                                extdata = 2;//表示已经领取
                        }
                        else
                        {
                            extdata = 0;
                        }
                        break;
                    case TCPGameServerCmds.CMD_SPR_GETSHAREAWARD:
                        resoult = GiveRoleShareAward(client);
                        if (resoult == 0||resoult==-2)
                        {
                            extdata = 2;//修改状态已经领取
                        }
                        else if (resoult == -1)
                        {
                            extdata = 0;//按钮状态是未激活，灰色
                        }
                        else
                        {
                            extdata = 1;//领取按钮激活，可领取
                        }
                        break;
                }
                string strcmd = string.Format("{0}:{1}", resoult,extdata);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessShareCMD", false);
            }
            return TCPProcessCmdResults.RESULT_DATA;
        }

        /// <summary>
        /// 更新分享状态
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int UpdateRoleShareState(GameClient client)
        {
            int ret = 0;
            string data = Global.GetRoleParamByName(client, RoleParamName.DailyShare);
            if (data == null)
                data = "-1,0";
            string[] fields = data.Split(',');
            if (fields[0] == TimeUtil.NowDateTime().DayOfYear.ToString()) //今天已经分享，不需要更新
                return 1;
            else
                Global.SaveRoleParamsStringToDB(client, RoleParamName.DailyShare,string.Format("{0},{1}", TimeUtil.NowDateTime().DayOfYear,0), true);
            return ret;
        }

        public static int GiveRoleShareAward(GameClient client)
        {
            int ret = 0;
            if (CanGetShareAward(client))
            {
                if (Global.CanAddGoodsDataList(client, ShareGoodslist))
                {
                    List<GoodsData> goodlist = ShareGoodslist;

                    //获取奖励的物品
                    foreach (var item in goodlist)
                    {

                        //想DBServer请求加入某个新的物品到背包中
                        //添加物品
                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, item.GoodsID, item.GCount, item.Quality, "", item.Forge_level, item.Binding, 0, "", true, 1, "分享", Global.ConstGoodsEndTime, item.AddPropIndex, item.BornIndex, item.Lucky, item.Strong, item.ExcellenceInfo, item.AppendPropLev, item.ChangeLifeLevForEquip, item.WashProps);
                    }
                    Global.SaveRoleParamsStringToDB(client, RoleParamName.DailyShare, string.Format("{0},{1}", TimeUtil.NowDateTime().DayOfYear, 1), true);
                }
                else
                {
                    ret = -3;//背包已满
                }
            }
            else if (!HasDoneShare(client))
            {
                ret = -1;//未分享，不能领取
            }
            else
            {
                ret = -2;//已经领取过了
            }
            return ret;
        }
    }
}
