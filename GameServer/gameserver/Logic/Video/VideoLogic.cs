using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic.Video
{
    class VideoLogic
    {
        private static List<VideoData> VideoList = new List<VideoData>();

        public static void LoadVideoXml()
        {
            VideoList.Clear();
            string fileName = Global.GameResPath("Config/Viedo.xml");
            XElement xml = XElement.Load(fileName);

            if (null == xml)
            {
                throw new Exception(string.Format("加载系统xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            IEnumerable<XElement> xmlItems = xml.Elements();
            foreach (XElement xmlItem in xmlItems)
            {
                var data = new VideoData();
                data.TalkID = Convert.ToInt32(Global.GetSafeAttributeStr(xmlItem, "TalkID"));
                data.MinZhuanSheng = Convert.ToByte(Global.GetSafeAttributeStr(xmlItem, "MinZhuanSheng"));
                data.MinLevel = Convert.ToByte(Global.GetSafeAttributeStr(xmlItem, "MinLevel"));
                data.MaxZhuanSheng = Convert.ToByte(Global.GetSafeAttributeStr(xmlItem, "MaxZhuanSheng"));
                data.MaxLevel = Convert.ToByte(Global.GetSafeAttributeStr(xmlItem, "MaxLevel"));
                data.MinVip = Convert.ToByte(Global.GetSafeAttributeStr(xmlItem, "MinVip"));
                data.MaxVip = Convert.ToByte(Global.GetSafeAttributeStr(xmlItem, "MaxVip"));
                data.PassWord = Global.GetSafeAttributeStr(xmlItem, "PassWord");
                data.ZhuanshengSift = Convert.ToByte(Global.GetSafeAttributeStr(xmlItem, "ZhuanshengSift"));
                data.LevelSift = Convert.ToByte(Global.GetSafeAttributeStr(xmlItem, "LevelSift"));
                data.VIPSift = Convert.ToByte(Global.GetSafeAttributeStr(xmlItem, "VIPSift"));
                VideoList.Add(data);
            }

            VideoList = VideoList.OrderByDescending(x => x.MinVip).ToList();
        }

        /// <summary>
        /// 获取视频聊天室相关数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessOpenVideoCmd(TMSKSocket socket, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_SPR_VIDEO_OPEN);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}",
                        (TCPGameServerCmds)nID, fields.Length, cmdData));

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_SPR_VIDEO_OPEN);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                int roleID = Convert.ToInt32(fields[0]);

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                VideoData roomData = GetVideoRoomData(client);
                if (roomData == null)
                {
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "", nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }
                var filterStatus = GetPlayerFilterStatus(client, roomData);

                string strcmd = string.Format("{0}:{1}:{2}", roomData.TalkID, roomData.PassWord, filterStatus);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_DATA;
        }

        /// <summary>
        /// 获取可以进入的聊天室数据
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static VideoData GetVideoRoomData(GameClient client)
        {
            foreach (var videoData in VideoList)
            {
                if (client.ClientData.VipLevel >= videoData.MinVip && client.ClientData.VipLevel <= videoData.MaxVip &&
                    client.ClientData.Level + client.ClientData.ChangeLifeCount * 100 <= videoData.MaxLevel + videoData.MaxZhuanSheng * 100 &&
                    client.ClientData.Level + client.ClientData.ChangeLifeCount * 100 >= videoData.MinLevel + videoData.MinZhuanSheng * 100)
                {
                    return videoData;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取玩家聊天室过滤状态
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static int GetPlayerFilterStatus(GameClient client, VideoData data)
        {
            return (client.ClientData.Level >= data.LevelSift ||
                    client.ClientData.VipLevel >= data.VIPSift ||
                    client.ClientData.ChangeLifeCount >= data.ZhuanshengSift) ? 1 : 0;
        }

        /// <summary>
        /// 获取聊天室按钮开启状态
        /// </summary>
        /// <param name="client"></param>
        /// <param name="oldStatus"></param>
        /// <returns></returns>
        public static int GetOrSendPlayerVideoStatus(GameClient client, List<int> RoleCommonUseIntPamams = null)
        {
            var status = GetVideoRoomData(client) == null ? 0 : 1;

            if (RoleCommonUseIntPamams != null && RoleCommonUseIntPamams.Count >= (int)RoleCommonUseIntParamsIndexs.VideoButton
                && RoleCommonUseIntPamams[(int)RoleCommonUseIntParamsIndexs.VideoButton] == 0 && status == 1)
            {
                client.ClientData.RoleCommonUseIntPamams[(int)RoleCommonUseIntParamsIndexs.VideoButton] = 1;
                GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.VideoButton, status);
            }
            return status;
        }


        public class VideoData
        {
            public int TalkID { get; set; }

            public int MinZhuanSheng { get; set; }

            public int MinLevel { get; set; }

            public int MaxZhuanSheng { get; set; }

            public int MaxLevel { get; set; }

            public int MinVip { get; set; }

            public int MaxVip { get; set; }

            public string PassWord { get; set; }

            public int ZhuanshengSift { get; set; }

            public int LevelSift { get; set; }

            public int VIPSift { get; set; }
        }
    }
}
