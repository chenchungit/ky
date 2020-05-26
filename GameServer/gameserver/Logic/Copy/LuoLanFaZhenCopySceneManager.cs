/// <summary>
/// 罗兰法阵副本
/// 2015-1-8 tanglong
/// </summary>
/// 
#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Server;
using System.Xml.Linq;
using Server.Data;
using System.Windows;
using Server.Tools;
using Server.Protocol;
using GameServer.Core.Executor;
using ProtoBuf;
using GameServer.Logic.Copy;
using Server.TCP;

namespace GameServer.Logic
{
    //罗兰法阵传送门数据
    public class SingleFazhenTelegateData
    {
        public int gateId = 0;              //传送门id
        public int destMapCode = 0;             //正常传送目标地图
        public int destX = 0;
        public int destY = 0;
        public bool usedAlready = false;    //是否已经用过
        public int SpecialDestMapCode = 0;  //特殊地图
        public int SpecialDestX = 0;
        public int SpecialDestY = 0;
    }

    //罗兰法阵地图数据
    public class FazhenMapData
    {
        public int CopyMapID = 0;   //地图流水ID
        public int MapCode = 0;   //地图流编号
        public Dictionary<int, SingleFazhenTelegateData> Telegates = new Dictionary<int, SingleFazhenTelegateData>();
    }

    //发给客户端的传送门数据
    [ProtoContract]
    public class FazhenTelegateProtoData
    {
        [ProtoMember(1)]
        public int gateId = 0;  //传送门编号，1~5
        [ProtoMember(2)]
        public int DestMapCode = 0; //传到哪个地图，0为未知
    }

    //发给客户端的罗兰法阵地图数据
    [ProtoContract]
    public class FazhenMapProtoData
    {
        [ProtoMember(1)]
        public int SrcMapCode = 0;                                     //传送门所在地图编号
        [ProtoMember(2)]
        public List<FazhenTelegateProtoData> listTelegate = null;       //传送门数据
    }

    //罗兰法阵副本数据（跟地图数据不是一回事）
    public class SingleLuoLanFaZhenFubenData
    {
        public int FubenID = 0;         //副本编号
        public int FubenSeqID = 0;
        public int FinalBossLeftNum = 1; //通关boss剩余数量
        public int SpecailBossLeftNum = 1;  //特殊boss剩余数量
        //副本内所有地图的数据
        public Dictionary<int, FazhenMapData> MapDatas = new Dictionary<int, FazhenMapData>();
    }

    //罗兰法阵地图传送门静态数据
    public class SystemFazhenMapData
    {
        public int MapCode = 0;
        public List<int> listGateID = new List<int>();
        public List<int> listDestMapCode = new List<int>();
        public int SpecialDestMapCode = 0;
        public int SpecialDestX = 0;
        public int SpecialDestY = 0;
    }

    // 罗兰法阵副本管理器
    class LuoLanFaZhenCopySceneManager
    {
        public static int GM_OpenState = 0;
        public static SystemXmlItems systemLuoLanFaZhen = new SystemXmlItems();
        /// <summary>
        /// 罗兰法阵副本数据全集
        /// int ：FubenSeqID 副本流水ID
        /// </summary>
        private static Dictionary<int, SingleLuoLanFaZhenFubenData> AllFazhenFubenData = new Dictionary<int, SingleLuoLanFaZhenFubenData>();
        private static Dictionary<int, SystemFazhenMapData> m_AllMapGatesStaticData = new Dictionary<int, SystemFazhenMapData>();
        /// <summary>
        /// 副本编号
        /// </summary>
        public static int LuoLanFaZhenFubenID = 4201;
        public static int SpecialTeleRate = 10;
        protected static int FinalBossID = 0;       //通关boss
        protected static int SpecialBossID = 0;     //特殊boss
        protected static int SpecialMapCode = 0;    //特殊地图
        protected static int SpecialAwardRate = 0;  //奖励倍数

        public static int getAwardRate(int FuBenID, int FuBenSeqID)
        {
            if (FuBenID != LuoLanFaZhenFubenID)
            {
                return 1;
            }

            SingleLuoLanFaZhenFubenData fubenData = GetFubenData(FuBenSeqID);
            if (null == fubenData)
                return 1;

            if (fubenData.SpecailBossLeftNum == 0)
                return SpecialAwardRate;
            else
                return 1;
        }

        public static void initialize()
        {
            try
            {
                int[] nParams = GameManager.systemParamsList.GetParamValueIntArrayByName("LuoLanFaZhen");
                if (nParams.Length != 5)
                {
                    throw new Exception("systemParamsList.LuoLanFaZhen参数数量应该是5");
                }
                //特殊boss
                SpecialBossID = nParams[0];
                //特殊boss所在地图
                SpecialMapCode = nParams[1];
                //杀死特殊boss后的通关奖励倍数
                SpecialAwardRate = nParams[2];
                //罗兰法阵副本编号
                LuoLanFaZhenFubenID = nParams[3];
                //进入特殊地图的概率
                SpecialTeleRate = nParams[4];
                //通关BossID
                SystemXmlItem systemFuBenItem = null;
                if (GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(LuoLanFaZhenFubenID, out systemFuBenItem) && systemFuBenItem != null)
                {
                    FinalBossID = systemFuBenItem.GetIntValue("BossID");
                }

                //读LuoLanFaZhen.xml配置文件
                systemLuoLanFaZhen.LoadFromXMlFile("Config/LuoLanFaZhen.xml", "", "MapID");
                List<int> listMapCode = systemLuoLanFaZhen.SystemXmlItemDict.Keys.ToList();
                foreach (var mapcode in listMapCode)
                {
                    SystemXmlItem systemFazhenMap = null;
                    if (systemLuoLanFaZhen.SystemXmlItemDict.TryGetValue(mapcode, out systemFazhenMap) && systemFazhenMap != null)
                    {
                        SystemFazhenMapData sysMapData = new SystemFazhenMapData();
                        sysMapData.MapCode = mapcode;
                        int[] specailParams = systemFazhenMap.GetIntArrayValue("TeShuMapID", '|');
                        if (null != specailParams && specailParams.Length >= 3)
                        {
                            sysMapData.SpecialDestMapCode = specailParams[0];
                            sysMapData.SpecialDestX = specailParams[1];
                            sysMapData.SpecialDestY = specailParams[2];
                        }
                        else
                        {
                            sysMapData.SpecialDestMapCode = -1;
                            sysMapData.SpecialDestX = -1;
                            sysMapData.SpecialDestY = -1;
                        }

                        int[] gateIds = systemFazhenMap.GetIntArrayValue("ChuanSongMenID", '|');
                        string strDestMapTemp = systemFazhenMap.GetStringValue("MuDidiID");
                        string[] strDestMapTemp2 = strDestMapTemp.Split('|');

                        if (gateIds.Length != strDestMapTemp2.Length)
                        {
                            throw new Exception("LuoLanFaZhen.xml传送门数量和目标地图数量不一致");
                        }

                        for (int i = 0; i < gateIds.Length; i++)
                        {
                            sysMapData.listGateID.Add(gateIds[i]);
                        }

                        for (int i = 0; i < strDestMapTemp2.Length; i++)
                        {
                            string[] strDestMapTemp3 = strDestMapTemp2[i].Split(',');
                            sysMapData.listDestMapCode.Add(Convert.ToInt32(strDestMapTemp3[0]));
                        }

                        m_AllMapGatesStaticData[mapcode] = sysMapData;
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception("罗兰法阵配置项出错");
            }
        }

        //副本被清除时
        public static bool OnFubenOver(int FubenSeqID)
        {
            lock (AllFazhenFubenData)
            {
                AllFazhenFubenData.Remove(FubenSeqID);
            }
            return true;
        }

        //获取某个副本的数据
        public static SingleLuoLanFaZhenFubenData GetFubenData(int FubenSeqID)
        {
            if (FubenSeqID <= 0)
                return null;

            SingleLuoLanFaZhenFubenData fubenData = null;

            lock (AllFazhenFubenData)
            {
                if (!AllFazhenFubenData.TryGetValue(FubenSeqID, out fubenData))
                    return null;
            }

            return fubenData;
        }

        //是否罗兰法阵副本
        public static bool IsLuoLanFaZhen(int FubenID)
        {
            return FubenID == LuoLanFaZhenFubenID;
        }

        //是否罗兰法阵副本地图
        public static bool IsLuoLanFaZhenMap(int mapcode)
        {
            return null != FuBenManager.FindMapCodeByFuBenID(LuoLanFaZhenFubenID, mapcode);
        }

        //断线重连时判断副本是否还能进
        public static bool EnterFubenMapWhenLogin(GameClient client)
        {
            //断线重进时，如果副本已经被清除，玩家就不能进副本了
            return (null != GetFubenData(client.ClientData.FuBenSeqID));
        }

        //玩家进入罗兰法阵副本地图
        public static bool OnEnterFubenMap(GameClient client, int oldmapcode, bool isLogin)
        {
            //创建或增加副本地图数据
            return TryAddFubenData(client.ClientData.FuBenSeqID, client.ClientData.FuBenID, client.ClientData.CopyMapID, client.ClientData.MapCode);
        }

        //玩家离开罗兰法阵副本的某张地图，不一定离开副本
        public static void OnLeaveFubenMap(GameClient client, int toMapCode)
        {

        }

        //随机生成地图内各传送门的目标地图
        public static void CreateRandomGates(int MapCode, FazhenMapData MapData)
        {
            //取得这个地图编号对应的传送门静态数据
            SystemFazhenMapData sysMapData = null;
            if (!m_AllMapGatesStaticData.TryGetValue(MapCode, out sysMapData))
                return ;
            if (null == sysMapData)
                return ;

            List<int> randgates = new List<int>();
            foreach (int gateid in sysMapData.listGateID)
        　　{
              int index = Global.GetRandomNumber(0, randgates.Count + 1);
              randgates.Insert(index, gateid);
            }

            if ( randgates.Count != sysMapData.listDestMapCode.Count )
                return;

            GameMap gameMap = null;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(MapData.MapCode, out gameMap) || null == gameMap)
            {
                return;
            }

            lock (MapData.Telegates)
            {
                for (int i = 0; i < randgates.Count; i++)
                {
                    MapTeleport mapTeleport = null;
                    if (!gameMap.MapTeleportDict.TryGetValue(sysMapData.listGateID[i], out mapTeleport) || null == mapTeleport)
                    {
                        continue;
                    }

                    SingleFazhenTelegateData newGatedata = new SingleFazhenTelegateData();
                    newGatedata.usedAlready = false;
                    newGatedata.gateId = randgates[i];
                    newGatedata.destMapCode = sysMapData.listDestMapCode[i];
                    newGatedata.SpecialDestMapCode = sysMapData.SpecialDestMapCode;
                    newGatedata.SpecialDestX = sysMapData.SpecialDestX;
                    newGatedata.SpecialDestY = sysMapData.SpecialDestY;
                    newGatedata.destX = mapTeleport.ToX;
                    newGatedata.destY = mapTeleport.ToY;
                    MapData.Telegates[newGatedata.gateId] = newGatedata;
                }
            }
        }

        //创建和增加副本地图数据
        protected static bool TryAddFubenData(int _FubenSeqID, int _FubenID, int _MapID, int _MapCode)
        {
            if (_FubenSeqID <= 0 || _FubenID <= 0 || _MapID <= 0 || _MapCode <= 0)
                return false;

            FazhenMapData mapdata = null;
            SingleLuoLanFaZhenFubenData fubenData = null;

            lock (AllFazhenFubenData)
            {
                //是否已经有这个副本了？
                if (!AllFazhenFubenData.TryGetValue(_FubenSeqID, out fubenData) || null == fubenData)
                {
                    //没这个副本的数据，现在创建一份
                    fubenData = new SingleLuoLanFaZhenFubenData()
                    {
                        FubenID = _FubenID,
                        FubenSeqID = _FubenSeqID
                    };
                    //地图数据
                    mapdata = new FazhenMapData()
                    {
                        CopyMapID = _MapID,
                        MapCode = _MapCode
                    };
                    //随机产生各个传送门的目标地图
                    CreateRandomGates(_MapCode, mapdata);

                    fubenData.MapDatas[_MapID] = mapdata;

                    //加入到数据集合中
                    AllFazhenFubenData[_FubenSeqID] = fubenData;
                }
                else
                {
                    lock (fubenData.MapDatas)
                    {
                        //已经有这个副本的数据了
                        if (!fubenData.MapDatas.TryGetValue(_MapID, out mapdata) || null == mapdata)
                        {
                            //没有这个地图的数据
                            mapdata = new FazhenMapData()
                            {
                                CopyMapID = _MapID,
                                MapCode = _MapCode
                            };
                            //随机产生各个传送门的目标地图
                            CreateRandomGates(_MapCode, mapdata);

                            //修改数据
                            fubenData.MapDatas[_MapID] = mapdata;
                        }
                        else
                        {
                            //已经有这个地图的数据，什么都不用做
                        }
                    }
                }
            }
            return true;
        }

        //玩家使用传送门
        public static TCPProcessCmdResults OnTeleport(GameClient client, int teleportID, TCPOutPacketPool pool, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            if (client.ClientData.FuBenID != LuoLanFaZhenFubenID || client.ClientData.FuBenSeqID <= 0)
            {
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            SingleLuoLanFaZhenFubenData fubenData = GetFubenData(client.ClientData.FuBenSeqID);
            if (null == fubenData)
                return TCPProcessCmdResults.RESULT_FAILED;

            FazhenMapData mapdata = null;
            SingleFazhenTelegateData teledata = null;

            lock (fubenData.MapDatas)
            {
                if (!fubenData.MapDatas.TryGetValue(client.ClientData.CopyMapID, out mapdata) || null == mapdata)
                {
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
            }

            if (mapdata.MapCode != client.ClientData.MapCode || mapdata.CopyMapID != client.ClientData.CopyMapID)
            {
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            lock (mapdata.Telegates)
            {
                //如果找不到这个传送门，说明这是个普通的传送点
                if (!mapdata.Telegates.TryGetValue(teleportID, out teledata) || null == teledata)
                {
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
            }

            if (teledata.destMapCode <= 0)
            {
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            //判断是否随机传到特殊地图
            bool TeleToSpecial = false;
            if (teledata.SpecialDestMapCode > 0)
            {
                //[bing] 有些人进入特殊地图时 该地图怪物已经被杀了 导致进入地图后又要返回 所以预先判断有无怪物 没怪物则不传送特殊地
                if(0 != fubenData.SpecailBossLeftNum)
                {
                    int rand = Global.GetRandomNumber(0, 100);
                    //是否随机传送到特殊地图
                    if (rand < SpecialTeleRate)
                    {
                        //传送到特殊地图
                        TeleToSpecial = true;
                    }
                }
            }

            if (TeleToSpecial)
            {
                //传送到特殊地图teledata.SpecialDestMapCode
                GameManager.ClientMgr.ChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), client, teleportID,
                    teledata.SpecialDestMapCode, teledata.SpecialDestX, teledata.SpecialDestY, client.ClientData.RoleDirection,
                    (int)TCPGameServerCmds.CMD_SPR_MAPCHANGE);
            }
            else   //传送到普通地图
            {
                //是否已经用过？
                bool NeedSend = false;
                lock (teledata)
                {
                    if (!teledata.usedAlready)
                    {
                        //没用过
                        teledata.usedAlready = true;
                        NeedSend = true;
                    }
                }
                
                if (NeedSend)
                {
                    //通知副本内所有玩家传送门被用过后的数据
                    FazhenMapProtoData senddata = new FazhenMapProtoData();
                    senddata.listTelegate = new List<FazhenTelegateProtoData>();
                    senddata.SrcMapCode = mapdata.MapCode;

                    FazhenTelegateProtoData gatedata_s = new FazhenTelegateProtoData();
                    gatedata_s.gateId = teledata.gateId;
                    gatedata_s.DestMapCode = teledata.destMapCode;
                    senddata.listTelegate.Add(gatedata_s);

                    //发送此传送门数据给地图内所有玩家
                    BroadMapData<FazhenMapProtoData>((int)TCPGameServerCmds.CMD_MAP_TELEPORT, senddata, mapdata.MapCode, client.ClientData.FuBenSeqID);
                }

                //传送到普通地图
                GameManager.ClientMgr.ChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), client, teleportID,
                    teledata.destMapCode, teledata.destX, teledata.destY, client.ClientData.RoleDirection, 
                    (int)TCPGameServerCmds.CMD_SPR_MAPCHANGE);
            }

            return TCPProcessCmdResults.RESULT_OK;
        }

        //玩家杀死特殊BOSS
        public static bool OnKillMonster(GameClient client, Monster monster)
        {
            if (client.ClientData.FuBenID != LuoLanFaZhenFubenID)
            {
                return false;
            }

            SingleLuoLanFaZhenFubenData fubenData = GetFubenData(client.ClientData.FuBenSeqID);
            if (null == fubenData)
                return false;

            List<int> listMapCodes = null;

            bool bKillBoss = false;
#if ___CC___FUCK___YOU___BB___
            if (monster.XMonsterInfo.MonsterId == FinalBossID)     //杀死的是否罗兰法阵的通关Boss
            {
                fubenData.FinalBossLeftNum = 0;
                bKillBoss = true;
            }
            else if (monster.XMonsterInfo.MonsterId == SpecialBossID)  //杀死的是否罗兰法阵的特殊Boss
            {
                //是
                FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(client.ClientData.FuBenSeqID);
                if (null == fuBenInfoItem)
                    return false;

                fubenData.SpecailBossLeftNum = 0;

                //改变奖励系数
                //fuBenInfoItem.AwardRate = SpecialAwardRate;

                //广播消息给副本内所有玩家
                string msg = StringUtil.substitute(Global.GetLang("『{0}』击杀了时空盗贼，令本次通关奖励翻倍！"), client.ClientData.RoleName);

                listMapCodes = CopyTeamManager.Instance().GetTeamCopyMapCodes(LuoLanFaZhenFubenID);
                if (null == listMapCodes)
                    return false;
                foreach (var mapCode in listMapCodes)
                {
                    BroadMapMessage(msg, mapCode, client.ClientData.FuBenSeqID);
                }

                bKillBoss = true;
            }
#else
            if (monster.MonsterInfo.ExtensionID == FinalBossID)     //杀死的是否罗兰法阵的通关Boss
            {
                fubenData.FinalBossLeftNum = 0;
                bKillBoss = true;
            }
            else if (monster.MonsterInfo.ExtensionID == SpecialBossID)  //杀死的是否罗兰法阵的特殊Boss
            {
                //是
                FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID(client.ClientData.FuBenSeqID);
                if (null == fuBenInfoItem)
                    return false;

                fubenData.SpecailBossLeftNum = 0;

                //改变奖励系数
                //fuBenInfoItem.AwardRate = SpecialAwardRate;

                //广播消息给副本内所有玩家
                string msg = StringUtil.substitute(Global.GetLang("『{0}』击杀了时空盗贼，令本次通关奖励翻倍！"), client.ClientData.RoleName);

                listMapCodes = CopyTeamManager.Instance().GetTeamCopyMapCodes(LuoLanFaZhenFubenID);
                if (null == listMapCodes)
                    return false;
                foreach (var mapCode in listMapCodes)
                {
                    BroadMapMessage(msg, mapCode, client.ClientData.FuBenSeqID);
                }

                bKillBoss = true;
            }
#endif
            //通知副本内所有玩家boss数量改变
            if (bKillBoss)
            {
                //杀死了boss
                if (null == listMapCodes)
                {
                    listMapCodes = CopyTeamManager.Instance().GetTeamCopyMapCodes(LuoLanFaZhenFubenID);
                }
                
                if (null == listMapCodes)
                    return false;

                //通知副本内所有玩家boss数量改变
                string cmdData = string.Format("{0}:{1}:{2}:{3}:{4}", LuoLanFaZhenFubenID, FinalBossID, fubenData.FinalBossLeftNum, SpecialBossID, fubenData.SpecailBossLeftNum);
                foreach (var mapCode in listMapCodes)
                {
                    BroadMapData((int)TCPGameServerCmds.CMD_SPR_FAZHEN_BOSS, cmdData, mapCode, client.ClientData.FuBenSeqID);
                }
            }
            
            return true;
        }

        //客户端请求罗兰法阵地图数据
        public static TCPProcessCmdResults ProcessFazhenTeleportCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                // 206发现这个字段解析异常
                int MapCode = 0;
                if (int.TryParse(fields[1], out MapCode) == false)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("ProcessFazhenTeleportCMD, roleID={0}, MapCode={1}", roleID, fields[1]));
                    return TCPProcessCmdResults.RESULT_OK;
                }

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.ClientData.MapCode != MapCode)
                {
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.ClientData.FuBenID != LuoLanFaZhenFubenID)
                {
                    return TCPProcessCmdResults.RESULT_OK;
                }

                SingleLuoLanFaZhenFubenData fubenData = GetFubenData(client.ClientData.FuBenSeqID);
                if (null == fubenData)
                    return TCPProcessCmdResults.RESULT_OK;

                FazhenMapData mapData = null;
                lock (fubenData.MapDatas)
                {
                    fubenData.MapDatas.TryGetValue(client.ClientData.CopyMapID, out mapData);
                    if (null == mapData)
                        return TCPProcessCmdResults.RESULT_OK;
                }

                //发送此地图内的传送门数据
                FazhenMapProtoData senddata = new FazhenMapProtoData();
                senddata.listTelegate = new List<FazhenTelegateProtoData>();
                senddata.SrcMapCode = mapData.MapCode;

                //遍历此地图内所有传送门
                lock (mapData.Telegates)
                {
                    List<int> listGateID = mapData.Telegates.Keys.ToList();

                    if (null != listGateID)
                    {
                        foreach (var gateid in listGateID)
                        {
                            SingleFazhenTelegateData gatedata = mapData.Telegates[gateid];
                            if (null == gatedata)
                                continue;

                            FazhenTelegateProtoData gatedata_s = new FazhenTelegateProtoData();
                            gatedata_s.gateId = gateid;
                            if (gatedata.usedAlready || GM_OpenState == 1)
                                gatedata_s.DestMapCode = gatedata.destMapCode;
                            else
                                gatedata_s.DestMapCode = 0;

                            senddata.listTelegate.Add(gatedata_s);
                        }
                    }
                }

                //发送此地图内的传送门数据
                byte[] bytes = DataHelper.ObjectToBytes<FazhenMapProtoData>(senddata);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytes, 0, bytes.Length, (int)TCPGameServerCmds.CMD_MAP_TELEPORT);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessFazhenTeleportCMD", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        //客户端请求法阵boss信息
        public static TCPProcessCmdResults ProcessFazhenBossCMD(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                if (client.ClientData.FuBenID != LuoLanFaZhenFubenID)
                {
                    return TCPProcessCmdResults.RESULT_OK;
                }

                SingleLuoLanFaZhenFubenData fubenData = GetFubenData(client.ClientData.FuBenSeqID);
                if (null == fubenData)
                    return TCPProcessCmdResults.RESULT_OK;

                string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", LuoLanFaZhenFubenID, FinalBossID, fubenData.FinalBossLeftNum, SpecialBossID, fubenData.SpecailBossLeftNum);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, (int)TCPGameServerCmds.CMD_SPR_FAZHEN_BOSS);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "ProcessFazhenBossCMD", false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// 向地图中的用户广播数据
        /// </summary>
        /// <param name="text"></param>
        public static void BroadMapData<T>(int cmdID, T cmdData, int mapCode, int FuBenSeqID)
        {
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(mapCode);
            if (null == objsList || objsList.Count <= 0) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                if (c.ClientData.FuBenSeqID != FuBenSeqID) continue;

                c.sendCmd<T>(cmdID, cmdData);
            }
        }

        /// <summary>
        /// 向地图中的用户广播数据
        /// </summary>
        /// <param name="text"></param>
        public static void BroadMapData(int cmdID, string cmdData, int mapCode, int FuBenSeqID)
        {
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(mapCode);
            if (null == objsList || objsList.Count <= 0) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                if (c.ClientData.FuBenSeqID != FuBenSeqID) continue;

                c.sendCmd(cmdID, cmdData);
            }
        }

        /// <summary>
        /// 向地图中的用户广播消息
        /// </summary>
        /// <param name="text"></param>
        public static void BroadMapMessage(string msg, int mapCode, int FuBenSeqID)
        {
            List<Object> objsList = GameManager.ClientMgr.GetMapClients(mapCode);
            if (null == objsList || objsList.Count <= 0) return;

            for (int i = 0; i < objsList.Count; i++)
            {
                GameClient c = objsList[i] as GameClient;
                if (c == null) continue;

                if (c.ClientData.FuBenSeqID != FuBenSeqID) continue;

                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, c,
                                               msg,
                                               GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlyErr, (int)HintErrCodeTypes.None);
            }
        }

        public static void GM_SetOpenState(int openstate)
        {
            GM_OpenState = openstate;
        }
    }
}
