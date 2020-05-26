using CC;
using GameServer.cc.Attack;
using GameServer.cc.Defult;
using GameServer.cc.Skill;
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic;
using GameServer.Logic.JingJiChang;
using GameServer.Logic.Name;
using GameServer.Server;
using ProtoBuf;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using Tmsk.Contract;
using static GameServer.cc.Attack.AttackManager;

namespace GameServer.cc.Role
{
    public class RoleManager
    {
        public void InitRoleManager()
        {
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_CREATE_ROLE, ProcessCreateRoleCmd);
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_GET_ROLE_LIST, ProcessGetRoleListCmd);
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_INIT_GAME, ProcessInitGameCmd);
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_PLAY_GAME, ProcessStartPlayGameCmd);
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_GAME_MOVE, ProcessSpriteMoveCmd);
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_STOP_MOVE, ProcessSpriteMoveEndCmd);
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_MAP_CHANGE, ProcessSpriteMapChangeCmd);
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_POSITION_MOVE, ProcessSpritePosCmd);
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_PLAY_ATTACK, ProcessSpriteAttackCmd);
            CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_READY_ATTACK, ProcessSpriteAttackReadyCmd);
        }

        public TCPProcessCmdResults ProcessCreateRoleCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
           TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            TCPProcessCmdResults result = TCPProcessCmdResults.RESULT_FAILED;
            CreateRolesReponse szCreateRolesReponse = new CreateRolesReponse();
            //反序列化
            MemoryStream msRecvData = new MemoryStream(data);
            CreateRoles szCreateRoles = Serializer.Deserialize<CreateRoles>(msRecvData);
            msRecvData.Dispose();
            msRecvData = null;

            //判断是否跨服一级性别超限
            if (socket.IsKuaFuLogin || szCreateRoles.Sex < 0 || szCreateRoles.Sex > 1)
            {
                szCreateRolesReponse.State = (int)ErrorCode.ERROR_DATA_LIMIT;

                MemoryStream msResult = new MemoryStream();
                Serializer.Serialize<CreateRolesReponse>(msResult, szCreateRolesReponse);
                byte[] msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            //检测是否含有非法字符
            int ret = NameServerNamager.CheckInvalidCharacters(szCreateRoles.UserName);
            if (ret <= 0)
            {
                szCreateRolesReponse.State = (int)ErrorCode.ERROR_NAME_INVALCHARACTER;

                MemoryStream msResult = new MemoryStream();
                Serializer.Serialize<CreateRolesReponse>(msResult, szCreateRolesReponse);
                byte[] msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            //注册名字到NameServer
            ret = NameServerNamager.RegisterNameToNameServer((int)szCreateRoles.ZoneID, szCreateRoles.Acc,
                szCreateRoles.PlatformDataList.ToArray(), 0, 0);
            if (ret <= 0)
            {
                szCreateRolesReponse.State = (int)ErrorCode.ERROR_REG_FAIL;

                MemoryStream msResult = new MemoryStream();
                Serializer.Serialize<CreateRolesReponse>(msResult, szCreateRolesReponse);
                byte[] msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            //判断名字长度是否过长
            if (!NameManager.Instance().IsNameLengthOK(szCreateRoles.UserName))
            {
                // 长度不满足的话，返回现有的错误码，服务器禁止创建吧
                szCreateRolesReponse.State = (int)ErrorCode.ERROR_NAME_LENGTH_LIMIT;

                MemoryStream msResult = new MemoryStream();
                Serializer.Serialize<CreateRolesReponse>(msResult, szCreateRolesReponse);
                byte[] msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            // 玩家创建角色限制检查
            int NotifyLeftTime = 0;
            if (!CreateRoleLimitManager.Instance().IfCanCreateRole(szCreateRoles.Acc, szCreateRoles.UserName,
                socket.deviceID, ((IPEndPoint)socket.RemoteEndPoint).Address.ToString(), out NotifyLeftTime))
            {
                szCreateRolesReponse.State = (int)ErrorCode.ERROR_CREATE_ROLE_LIMIT;

                MemoryStream msResult = new MemoryStream();
                Serializer.Serialize<CreateRolesReponse>(msResult, szCreateRolesReponse);
                byte[] msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, socket.ServerId);

            if (null != tcpOutPacket)
            {
                msRecvData = new MemoryStream(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);
                szCreateRolesReponse = Serializer.Deserialize<CreateRolesReponse>(msRecvData);
                msRecvData.Dispose();
                msRecvData = null;

                if ((int)ErrorCode.ERROR_OK != szCreateRolesReponse.State)
                {
                    CreateRoleLimitManager.Instance().ModifyCreateRoleNum(szCreateRoles.Acc, szCreateRoles.UserName,
                        socket.deviceID, ((IPEndPoint)socket.RemoteEndPoint).Address.ToString());


                }
                SysConOut.WriteLine(string.Format("创建角色 状态 ={0}", result));
            }
            return result;
        }

        public TCPProcessCmdResults ProcessGetRoleListCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
          TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;



            GetRolesList szGetRolesList = null;

            byte[] msSendData = null;
            try
            {
                //反序列化
                MemoryStream msRecvData = new MemoryStream(data);
                szGetRolesList = Serializer.Deserialize<GetRolesList>(msRecvData);
                msRecvData.Dispose();
                msRecvData = null;
            }
            catch (Exception ex) //解析错误
            {
                SysConOut.WriteLine(string.Format("解析指令字符串错误, CMD={0} --{1}", (TCPGameServerCmds)nID, ex.Message));
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0} --{1}", (TCPGameServerCmds)nID, ex.Message));


                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            try
            {
                if (!socket.IsKuaFuLogin)
                {
                    ChangeNameInfo info = NameManager.Instance().GetChangeNameInfo(szGetRolesList.Acc, (int)szGetRolesList.ZoneID, socket.ServerId);
                    if (info != null)
                    {

                        tcpMgr.MySocketListener.SendData(socket, DataHelper.ObjectToTCPOutPacket(info, pool, (int)TCPGameServerCmds.CMD_NTF_EACH_ROLE_ALLOW_CHANGE_NAME));
                    }
                }

                socket.session.SetSocketTime(0);
                socket.session.SetSocketTime(1);
                socket.session.SetSocketTime(2);
                TCPProcessCmdResults szState = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, socket.ServerId);
                GetRolesListReponse szGetRolesListReponse = new GetRolesListReponse();
                MemoryStream msRecvData = new MemoryStream(data);
                msRecvData = new MemoryStream(tcpOutPacket.GetPacketBytes(), 6, tcpOutPacket.PacketDataSize - 6);
                szGetRolesListReponse = Serializer.Deserialize<GetRolesListReponse>(msRecvData);
                msRecvData.Dispose();
                msRecvData = null;


                SysConOut.WriteLine("发送角色列表 state = " + szGetRolesListReponse.State.ToString() + "   count = " + szGetRolesListReponse.SubRoleinfo.Count.ToString());
                return szState;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }


        public TCPProcessCmdResults ProcessInitGameCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            try
            {
                //反序列化
                MemoryStream msRecvData = new MemoryStream(data);
                InitialGame szInitialGame = Serializer.Deserialize<InitialGame>(msRecvData);
                msRecvData.Dispose();
                msRecvData = null;

                //判断是否已经登录验证在线
                if (szInitialGame.RoleID < 0 || !GameManager.TestGamePerformanceMode && szInitialGame.Acc != GameManager.OnlineUserSession.FindUserID(socket))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("外挂登录，没有SocketSession的情况下，进行了登录, CMD={0}, Client={1}, RoleID={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), szInitialGame.RoleID));
                    return TCPProcessCmdResults.RESULT_OK;
                }
                byte[] bytesData = null;
                if (TCPProcessCmdResults.RESULT_FAILED == Global.TransferRequestToDBServer2(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out bytesData, socket.ServerId))
                {
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                Int32 length = BitConverter.ToInt32(bytesData, 0);
                UInt16 cmd = BitConverter.ToUInt16(bytesData, 4);

                SysConOut.WriteLine("初始化游戏写数据库返回" + cmd.ToString() + "  Len = " + length.ToString() + "  bytesData len=" + bytesData.Length.ToString());
                msRecvData = new MemoryStream(bytesData, 6, length - 2);
                InitialGameReponse szInitialGameReponse = Serializer.Deserialize<InitialGameReponse>(msRecvData);
                SysConOut.WriteLine("初始化游戏写数据库返回 state = " + szInitialGameReponse.State);

                RoleDataEx roleDataEx = new RoleDataEx();
                roleDataEx.RoleID = (int)szInitialGameReponse.SubRoleinfo.RoleID;
                roleDataEx.RoleSex = (int)szInitialGameReponse.SubRoleinfo.Sex;
                roleDataEx.Level = (int)szInitialGameReponse.SubRoleinfo.CurLv;
                roleDataEx.Experience = (int)szInitialGameReponse.SubRoleinfo.CurExp;
                roleDataEx.Occupation = (int)szInitialGameReponse.SubRoleinfo.JobID;
                roleDataEx.MapCode = (int)szInitialGameReponse.SubRoleinfo.MapCode;
                roleDataEx.RoleDirection = (int)szInitialGameReponse.SubRoleinfo.RoleDirection;
                roleDataEx.PosX = (int)szInitialGameReponse.SubRoleinfo.PosX;
                roleDataEx.PosY = (int)szInitialGameReponse.SubRoleinfo.PosY;
                roleDataEx.LifeV = (int)szInitialGameReponse.SubRoleinfo.LifeV;
                roleDataEx.MagicV = (int)szInitialGameReponse.SubRoleinfo.MagicV;
                roleDataEx.RoleName = szInitialGameReponse.SubRoleinfo.NickName;
                roleDataEx.IsFlashPlayer = (int)szInitialGameReponse.SubRoleinfo.isFlashplayer;

                if (!socket.IsKuaFuLogin)
                {
                    //如果地图不存在
                    if (!Global.MapExists(roleDataEx.MapCode))
                    {
                        SysConOut.WriteLine(string.Format("登陆时强制将不存在的地图编号转为缺省的登录地图: MapCode={0}", roleDataEx.MapCode));
                        LogManager.WriteLog(LogTypes.Warning, string.Format("登陆时强制将不存在的地图编号转为缺省的登录地图: MapCode={0}", roleDataEx.MapCode));
                        //if (roleDataEx.Level <= 1)
                        {
                            // 校正非法地图 [XSea 2015/4/15]
                            if (GameManager.MagicSwordMgr.IsMagicSword(roleDataEx.Occupation)) // 如果是魔剑士 放到魔剑士新手地图
                                roleDataEx.MapCode = MagicSwordData.InitMapID;
                            else // 如果是战士、法师、弓箭手 则放到默认新手地图
                                roleDataEx.MapCode = GameManager.MainMapCode;
                        }
                        //else
                        //{
                        //    roleDataEx.MapCode = GameManager.MainMapCode;
                        //}

                        roleDataEx.PosX = 0;
                        roleDataEx.PosY = 0;
                    }
                    else
                    {
                        //矫正角色上线后的位置
                        GameMap gameMap = GameManager.MapMgr.DictMaps[roleDataEx.MapCode];
                        roleDataEx.PosX = Global.Clamp(roleDataEx.PosX, 1, gameMap.MapWidth);
                        roleDataEx.PosY = Global.Clamp(roleDataEx.PosY, 1, gameMap.MapHeight);
                        roleDataEx.PosX = gameMap.CorrectWidthPointToGridPoint(roleDataEx.PosX);
                        roleDataEx.PosY = gameMap.CorrectHeightPointToGridPoint(roleDataEx.PosY);
                    }
                }

                //保存客户端数据的线程安全对象
                SafeClientData clientData = new SafeClientData()
                {
                    RoleData = roleDataEx,
                    FuBenSeqID = 0,
                    ReportPosTicks = 0,
                    WaitingForChangeMap = true,
                    MapCode = roleDataEx.MapCode,
                    Occupation = roleDataEx.Occupation,
                    Level = roleDataEx.Level
                    //RoleName = roleDataEx.RoleName
                };

                //将此用户加入管理队列中
                GameClient gameClient = new GameClient()
                {
                    ClientSocket = socket,
                    ClientData = clientData,
                };
                if (szInitialGameReponse.SubRoleinfo.SkillInfoList.Count <= 0)
                {
                    //添加缺省的技能情况
                    //Global.AddDefaultSkills(gameClient);
                    // GlobalDefultObject szGlobalDefultObject = null;
                    //GameManager.SystemGlobalDefultMgr.SystemGlobalDefultList.TryGetValue(gameClient.ClientData.Occupation, out szGlobalDefultObject);
                    List<GlobalDefultObject> szOccDefultSkillList = null;
                    GameManager.SystemGlobalDefultMgr.GetOccDefultSkillList(gameClient.ClientData.Occupation, out szOccDefultSkillList);
                    if (null != szOccDefultSkillList)
                    {
                        foreach (var s in szOccDefultSkillList)
                        {
                            //根据配置文件添加默认技能
                            if (!Global.AddDefaultSkills(pool, gameClient, s.SkillID, 1))
                            {
                                return TCPProcessCmdResults.RESULT_FAILED;
                            }
                            // 这两个值是给客户血条显示最大值得
                            gameClient.ClientData.LifeV = (int)RoleAlgorithm.GetMaxLifeV(gameClient);
                            gameClient.ClientData.MagicV = (int)RoleAlgorithm.GetMaxMagicV(gameClient);
                        }
                    }


                    gameClient.ClientData.CurrentLifeV = gameClient.ClientData.LifeV;
                    gameClient.ClientData.CurrentMagicV = gameClient.ClientData.MagicV;
                }
                else
                {
                    foreach (var skill in szInitialGameReponse.SubRoleinfo.SkillInfoList)
                    {
                        SkillData skillData = new SkillData();
                        skillData.DbID = (int)skill.DBID;
                        skillData.SkillID = (int)skill.SkillID;
                        skillData.SkillLevel = (int)skill.SkillLevel;
                        skillData.UsedNum = (int)skill.UsedNum;
                        if (null == gameClient.ClientData.SkillDataList)
                            gameClient.ClientData.SkillDataList = new List<SkillData>();
                        gameClient.ClientData.SkillDataList.Add(skillData);
                    }
                    gameClient.ClientData.LifeV = (int)RoleAlgorithm.GetMaxLifeV(gameClient);
                    gameClient.ClientData.MagicV = (int)RoleAlgorithm.GetMaxMagicV(gameClient);
                    gameClient.ClientData.CurrentLifeV = gameClient.ClientData.LifeV;
                    gameClient.ClientData.CurrentMagicV = gameClient.ClientData.MagicV;
                    // gameClient.ClientData.CurrentLifeV = (int)szInitialGameReponse.SubRoleinfo.LifeV;
                    // gameClient.ClientData.CurrentMagicV = (int)szInitialGameReponse.SubRoleinfo.MagicV;
                }

                // 初始化帐号，避免频繁查询
                gameClient.strUserID = GameManager.OnlineUserSession.FindUserID(gameClient.ClientSocket);
                gameClient.strUserName = GameManager.OnlineUserSession.FindUserName(socket);
                gameClient.deviceID = "0";
                gameClient.IsYueYu = false;



                //gameClient.ClientData.RoleName = roleDataEx.RoleName;
                //添加到队列中
                if (!GameManager.ClientMgr.AddClient(gameClient))
                {
                    SysConOut.WriteLine(string.Format("角色已在客户角色列表中,强制断开连接.角色名:{0}", Global.FormatRoleName(gameClient, gameClient.ClientData.RoleName)));
                    //LogManager.WriteLog(LogTypes.Error, string.Format("角色已在客户角色列表中,强制断开连接.角色名:{0}", Global.FormatRoleName(gameClient, gameClient.ClientData.RoleName)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }


                // RoleData roleData = Global.ClientDataToRoleData1(gameClient);

                Roleinfo szRoleinfo = new Roleinfo();
                szRoleinfo.RoleID = roleDataEx.RoleID;
                szRoleinfo.Sex = roleDataEx.RoleSex;
                szRoleinfo.JobID = roleDataEx.Occupation;
                szRoleinfo.CurLv = roleDataEx.Level;
                szRoleinfo.CurExp = (int)roleDataEx.Experience;
                szRoleinfo.MapCode = roleDataEx.MapCode;
                szRoleinfo.RoleDirection = roleDataEx.RoleDirection;
                szRoleinfo.PosX = roleDataEx.PosX;
                szRoleinfo.PosY = roleDataEx.PosY;
                szRoleinfo.LifeV = gameClient.ClientData.LifeV;
                szRoleinfo.MaxLifeV = gameClient.ClientData.CurrentLifeV;
                szRoleinfo.MagicV = gameClient.ClientData.MagicV;
                szRoleinfo.MaxMagicV = gameClient.ClientData.CurrentMagicV;
                szRoleinfo.ZoneID = roleDataEx.ZoneID;
                szRoleinfo.NickName = roleDataEx.RoleName;
                szRoleinfo.isFlashplayer = roleDataEx.IsFlashPlayer;

                string str = "";
                if (null != clientData.SkillDataList)
                {
                    foreach (var Skill in clientData.SkillDataList)
                    {
                        SkillInfo szSkillInfo = new SkillInfo();
                        szSkillInfo.DBID = Skill.DbID;
                        szSkillInfo.SkillID = Skill.SkillID;
                        szSkillInfo.SkillLevel = Skill.SkillLevel;
                        szSkillInfo.UsedNum = Skill.UsedNum;


                        str += "DBID "+ szSkillInfo.DBID+ " SkillID "+ szSkillInfo.SkillID + " SkillLevel "+ szSkillInfo.SkillLevel + " UsedNum "+ szSkillInfo.UsedNum+"  ||\n";

                        szRoleinfo.SkillInfoList.Add(szSkillInfo);
                    }
                }

                //GameManager.ErrMgr.NotifySystemErrorToClient(gameClient, (int)ErrorCode.ERROR_OTHERERR, str);

                InitialGameReponse SendInitialGameReponse = new InitialGameReponse();
                SendInitialGameReponse.SubRoleinfo = szRoleinfo;

                tcpOutPacket = DataHelper.ProtocolToTCPOutPacket<InitialGameReponse>(SendInitialGameReponse, pool, cmd);


                SysConOut.WriteLine(string.Format("角色所在地图:{0}---{1}--血量{2}",
                    gameClient.ClientData.GetRoleData().MapCode, gameClient.ClientData.MapCode, gameClient.ClientData.CurrentLifeV));
                // tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, bytesData, 6, length - 2, cmd);

                socket.session.SetSocketTime(0);
                socket.session.SetSocketTime(3);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                SysConOut.WriteLine("初始化游戏写数据库异常 " + ex.Message);
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        private TCPProcessCmdResults ProcessStartPlayGameCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            try
            {
                //反序列化
                MemoryStream msRecvData = new MemoryStream(data, 0, count);
                PlayGame szPlayGame = Serializer.Deserialize<PlayGame>(msRecvData);
                msRecvData.Dispose();
                msRecvData = null;

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (client != null)
                {
                    // 成功登录
                    client.ClientData.LoginNum++;
                    //更新上线状态
                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_DB_ROLE_ONLINE,
                        string.Format("{0}:{1}:{2}:{3}", client.ClientData.RoleID, GameManager.ServerLineID, client.ClientData.LoginNum, Global.GetSocketRemoteIP(client)),
                        null, client.ServerId);
                    client.ClientData.WaitingForChangeMap = false; //这里才是地图切换完成
                    GlobalEventSource.getInstance().fireEvent(new OnStartPlayGameEventObject(client));

                }
                socket.session.SetSocketTime(4);

                PlayGameReponse szPlayGameReponse = new PlayGameReponse();
                szPlayGameReponse.RoleID = szPlayGame.RoleID;


                MemoryStream msResult = new MemoryStream();
                Serializer.Serialize<PlayGameReponse>(msResult, szPlayGameReponse);
                byte[] msSendData = msResult.ToArray();
                SysConOut.WriteLine("发送进入游戏消息 cmd = " + nID.ToString());
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);

                msResult.Dispose();
                msResult = null;

                socket.session.SetSocketTime(0);
                socket.session.SetSocketTime(4);
                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        private TCPProcessCmdResults ProcessSpriteMoveCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            //  SysConOut.WriteLine("======ProcessSpriteMoveCmd=======");
            tcpOutPacket = null;
            // MemoryStream msRecvData = null;
            SpriteMove szSpriteMove = null;

            byte[] msSendData = null;

            try
            {
                //反序列化
                MemoryStream msRecvData = new MemoryStream(data);
                szSpriteMove = Serializer.Deserialize<SpriteMove>(msRecvData);
                msRecvData.Dispose();
                msRecvData = null;
            }
            catch (Exception ex) //解析错误
            {
                MemoryStream msResult = new MemoryStream();
                LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0} --{1}", (TCPGameServerCmds)nID, ex.Message));
                SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
                szSystemErrorReponse.State = (int)ErrorCode.ERROR_PROTOCOL;

                Serializer.Serialize<SystemErrorReponse>(msResult, szSystemErrorReponse);
                msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            try
            {
                SpriteMoveData cmdData = new SpriteMoveData();
                cmdData.roleID = (int)szSpriteMove.RoleID;
                cmdData.mapCode = (int)szSpriteMove.MapCode;
                cmdData.action = (int)szSpriteMove.action;
                cmdData.toX = (int)szSpriteMove.toX;
                cmdData.toY = (int)szSpriteMove.toY;
                cmdData.extAction = (int)szSpriteMove.extAction;
                cmdData.fromX = (int)szSpriteMove.fromX;
                cmdData.fromY = (int)szSpriteMove.fromY;
                cmdData.startMoveTicks = (long)szSpriteMove.startMoveTicks;
                cmdData.pathString = szSpriteMove.pathString;


                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != cmdData.roleID)
                {
                    SysConOut.WriteLine(string.Format("根据RoleID定位GameClient对象失败", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), cmdData.roleID));
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), cmdData.roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }


                //  SysConOut.WriteLine(string.Format("{0}/{5}请求从({1},{2})移动到({3},{4})", cmdData.roleID, cmdData.fromX, cmdData.fromY, cmdData.toX, cmdData.toY, client.ClientData.RoleName));



                //GameManager.ErrMgr.NotifySystemErrorToClient(client, (int)ErrorCode.ERROR_OTHERERR, "客户端请求的地图编号："+ cmdData.mapCode+"实际编号"+ client.ClientData.MapCode);

                ///如果地图编号不等，则肯定是外挂发送的
                if (cmdData.mapCode != client.ClientData.MapCode)
                {
                    client.CheckCheatData.MismatchingMapCode = true;
                    return TCPProcessCmdResults.RESULT_OK;
                }

                //if (client.ClientData.WaitingForChangeMap)
                //{
                //    return TCPProcessCmdResults.RESULT_OK;
                //}

                if (GameManager.FlagCheckCmdPosition > 0)
                {//
                    GameMap gameMap = GameManager.MapMgr.GetGameMap(cmdData.mapCode);
                    if (null == gameMap)
                    {
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    if (-1 != cmdData.toX && -1 != cmdData.toY &&
                        !Global.IsGridReachable(cmdData.mapCode, cmdData.toX / gameMap.MapGridWidth, cmdData.toY / gameMap.MapGridHeight))
                    {
                        //LogManager.WriteLog(LogTypes.Error, string.Format("ProcessSpriteMoveCmd Faild RoleID = {0}, MapCode = {1}, toX = {2}, toY = {3}", roleID, mapCode, toX, toY));
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }
                //移动就取消隐身
                //if (client.ClientData.DSHideStart > 0)
                //{
                //    Global.RemoveBufferData(client, (int)BufferItemTypes.DSTimeHideNoShow);
                //    client.ClientData.DSHideStart = 0;
                //    GameManager.ClientMgr.NotifyDSHideCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                //}

                //取消交易市场的开放状态
                //if (!Global.Flag_MUSale)
                //{
                //    Global.CloseMarket(client);
                //}

                //取消冥想状态
                //  Global.EndMeditate(client);

                //取消采集状态
                // GameServer.Logic.CaiJiLogic.CancelCaiJiState(client);

                //记录移动或者动作的次数
                client.ClientData.MoveAndActionNum++;

                //角色停止故事版的移动
                // GameManager.ClientMgr.StopClientStoryboard(client);
                string pathString = cmdData.pathString;
                //记录历史路径
                client.ClientData.RolePathString = pathString;
                client.ClientData.RoleStartMoveTicks = cmdData.startMoveTicks;

                // System.Console.WriteLine(String.Format("{0} Path{1}", client.ClientData.RoleName, pathString));
                // 属性改造 新增移动速度 [8/15/2013 LiaoWei]
                double moveCost = RoleAlgorithm.GetMoveSpeed(client);

                if (client.ClientData.HorseDbID > 0)
                {
                    //获取坐骑增加的速度
                    double horseSpeed = Global.GetHorseSpeed(client);
                    moveCost += horseSpeed;
                }

                //moveCost -= client.RoleMagicHelper.GetMoveSlow();
                //moveCost = Global.GMax(0.5, moveCost);

                if (!Global.ValidateClientPosition(client, cmdData.fromX, cmdData.fromY, cmdData.startMoveTicks, moveCost))
                {
                    int fromX = client.ClientData.PosX;
                    int fromY = client.ClientData.PosY;
                    GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                client, (int)fromX, (int)fromY, client.ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
                }

                //可能会导致九宫格出问题，在客户端上，没出9宫格范围就隐身了？？？
                client.ClientData.PosX = cmdData.fromX;
                client.ClientData.PosY = cmdData.fromY;
                //client.ClientData.ReportPosTicks = startMoveTicks;
                //SysConOut.WriteLine("移动坐标点 x=" + cmdData.fromX.ToString()+"  y = " + cmdData.fromY.ToString());

                //在这里就要处理地图格子位置，否则，在快速的来回反复移动时，自己和自己都会冲突
                MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode];
                mapGrid.MoveObject(-1, -1, cmdData.fromX, cmdData.fromY, client);

                /// 玩家进行了移动
               // Global.GameClientMoveGrid(client);

                /// 当前正在做的动作
                client.ClientData.CurrentAction = cmdData.action;

                /// 移动的速度
                client.ClientData.MoveSpeed = moveCost;

                /// 移动的目的地坐标点
                client.ClientData.DestPoint = new Point(cmdData.toX, cmdData.toY);

                //2015-9-16消息流量优化
                SpriteNotifyOtherMoveData moveData = new SpriteNotifyOtherMoveData();
                moveData.roleID = client.ClientData.RoleID;
                moveData.moveCost = moveCost;
                moveData.mapCode = cmdData.mapCode;
                moveData.fromX = cmdData.fromX;
                moveData.fromY = cmdData.fromY;
                moveData.toX = cmdData.toX;
                moveData.toY = cmdData.toY;
                moveData.action = cmdData.action;
                moveData.pathString = client.ClientData.RolePathString;

                client.sendCmd<SpriteNotifyOtherMoveData>(nID, moveData);

                moveData.action = cmdData.action;
                moveData.toX = cmdData.toX;
                moveData.toY = cmdData.toY;
                moveData.extAction = cmdData.extAction;
                moveData.fromX = cmdData.fromX;
                moveData.fromY = cmdData.fromY;
                moveData.startMoveTicks = cmdData.startMoveTicks;
                moveData.pathString = client.ClientData.RolePathString;

                // Console.WriteLine("通知其他人自己开始移动");
                //通知其他人自己开始移动
                GameManager.ClientMgr.NotifyOthersMyMoving(tcpMgr.MySocketListener, pool, moveData, client, nID);


                //GameManager.ErrMgr.NotifySystemErrorToClient(client, (int)ErrorCode.ERROR_OTHERERR);
                //GameManager.ErrMgr.NotifySystemErrorToClient(tcpMgr.MySocketListener, pool, client, (int)ErrorCode.ERROR_OTHERERR, "测试异常");



                //开始角色故事版
                //  GameManager.ClientMgr.StartClientStoryboard(client, startMoveTicks);

                // SysConOut.WriteLine("--->坐标点 x=" + client.ClientData.PosX.ToString() + "  y = " + client.ClientData.PosY.ToString());
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_OK;
        }

        private TCPProcessCmdResults ProcessSpriteMoveEndCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            tcpOutPacket = null;
            // return TCPProcessCmdResults.RESULT_OK;
            SysConOut.WriteLine("======ProcessSpriteMoveEndCmd=======");


            byte[] msSendData = null;
            SpriteMovePosition szSpriteMovePosition;
            try
            {
                // msRecvDatas = new MemoryStream(data, 0, count);
                szSpriteMovePosition = DataHelper.BytesToObject<SpriteMovePosition>(data, 0, count);// Serializer.Deserialize<SpriteMovePosition>(msRecvDatas);
            }
            catch (Exception ex)
            {
                MemoryStream msResult = new MemoryStream();
                SysConOut.WriteLine("解析停止移动消息错误");
                SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
                szSystemErrorReponse.State = (int)ErrorCode.ERROR_PROTOCOL;

                Serializer.Serialize<SystemErrorReponse>(msResult, szSystemErrorReponse);
                msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            try
            {
                int roleID = (int)szSpriteMovePosition.RoleID;
                int mapCode = (int)szSpriteMovePosition.MapCode;
                int toX = (int)szSpriteMovePosition.ToMapX;
                int toY = (int)szSpriteMovePosition.ToMapY;
                int direction = (int)szSpriteMovePosition.ToDiection;

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }


                int check_cmd_position = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.check_cmd_position, 1);


                //重置野蛮冲撞状态
                //client.ClientData.YeManChongZhuang = 0;

                int oldToX = toX;
                int oldToY = toY;



                int newToX = toX;// gameMap.CorrectWidthPointToGridPoint((int)toX);
                int newToY = toY;// gameMap.CorrectHeightPointToGridPoint((int)toY);

                if (newToX != toX || newToY != toY)
                {
                    toX = newToX;
                    toY = newToY;
                }

                bool sendToSelf = false;
                sendToSelf = (oldToX != toX) || (oldToY != toY);

                int oldX = client.ClientData.PosX;
                int oldY = client.ClientData.PosY;

                client.ClientData.PosX = toX;
                client.ClientData.PosY = toY;
                client.ClientData.RoleDirection = direction;
                client.ClientData.ReportPosTicks = 0;


                /// 当前正在做的动作
                client.ClientData.CurrentAction = (int)GActions.Stand;

                /// 移动的速度
                client.ClientData.MoveSpeed = 1.0;

                /// 移动的目的地坐标点
                client.ClientData.DestPoint = new Point(toX, toY);

                //通知其他人自己停止移动
                GameManager.ClientMgr.NotifyOthersMyMovingEnd<SpriteMovePosition>(tcpMgr.MySocketListener, pool, client, szSpriteMovePosition, sendToSelf);

                /// 玩家进行了移动
                if (GameManager.Update9GridUsingNewMode <= 0)
                {
                    ClientManager.DoSpriteMapGridMove(client);
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        private TCPProcessCmdResults ProcessSpriteMapChangeCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            tcpOutPacket = null;

            SysConOut.WriteLine("======ProcessSpriteMapChangeCmd=======");

            byte[] msSendData = null;
            SpriteExchangeMap szSpriteExchangeMap;
            try
            {
                MemoryStream msRecvData = new MemoryStream(data);
                szSpriteExchangeMap = Serializer.Deserialize<SpriteExchangeMap>(msRecvData);
                msRecvData.Dispose();
                msRecvData = null;
                // szSpriteExchangeMap = DataHelper.BytesToObject<SpriteExchangeMap>(data, 0, count);
            }
            catch (Exception ex)
            {
                MemoryStream msResult = new MemoryStream();
                SysConOut.WriteLine("解析切换地图消息错误");
                SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
                szSystemErrorReponse.State = (int)ErrorCode.ERROR_PROTOCOL;

                Serializer.Serialize<SystemErrorReponse>(msResult, szSystemErrorReponse);
                msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            try
            {
                int roleID = (int)szSpriteExchangeMap.RoleID;
                int teleportID = (int)szSpriteExchangeMap.TeleportID;
                int newMapCode = (int)szSpriteExchangeMap.NewMapCode;
                int toNewMapX = (int)szSpriteExchangeMap.ToNewMapX;
                int toNewMapY = (int)szSpriteExchangeMap.ToNewMapY;
                int toNewDiection = (int)szSpriteExchangeMap.ToNewDiection;

                //调试用
                //SysConOut.WriteLine(string.Format("req mapcode:{0}, x:{1}, y:{2}, dir:{3}", newMapCode, toNewMapX, toNewMapY, toNewDiection));

                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                client.ClientData.LastClientHeartTicks = TimeUtil.NOW();
                if (client.CheckCheatData.GmGotoShadowMapCode == newMapCode)
                {
                    if (!GameManager.ClientMgr.ChangeMap(tcpMgr.MySocketListener, pool, client, teleportID, newMapCode, toNewMapX, toNewMapY, toNewDiection, nID))
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("切换地图时失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    }

                    return TCPProcessCmdResults.RESULT_OK;
                }


                SCMapChange scData = null;

                // 进入水晶幻境增加一个从登陆就开始算的cd
                SceneUIClasses sceneType = Global.GetMapSceneType(newMapCode);


                if (teleportID < 0) //直接的地图传送, 非传送点
                {
                    GameMap toGameMap = null;
                    if (!GameManager.MapMgr.DictMaps.TryGetValue(newMapCode, out toGameMap))
                    {
                        //szSpriteExchangeMap.State = -1000;
                        //目标地图不存在
                        szSpriteExchangeMap.State = (int)ErrorCode.ERROR_MAP_CHANGE_NOTOMAP;
                        client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    int toLevel = toGameMap.MinLevel;
                    int toChangeLifeLev = toGameMap.MinZhuanSheng;
                    if (client.ClientData.ChangeLifeCount < toChangeLifeLev)
                    {
                        //szSpriteExchangeMap.State = -1001;
                        //转身次数较少
                        szSpriteExchangeMap.State = (int)ErrorCode.ERROR_MAP_CHANGE_LOWCHANGELIFE;
                        client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    if ((client.ClientData.ChangeLifeCount * 400 + client.ClientData.Level) < (toChangeLifeLev * 400 + toLevel))
                    {
                        if (client.ClientData.Level < toLevel)
                        {
                            szSpriteExchangeMap.State = -1002;
                            client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                            return TCPProcessCmdResults.RESULT_DATA;
                        }
                    }

                    if (!client.ClientData.WaitingNotifyChangeMap || client.ClientData.WaitingChangeMapToMapCode != newMapCode ||
                        client.ClientData.WaitingChangeMapToPosX != toNewMapX || client.ClientData.WaitingChangeMapToPosY != toNewMapY)
                    {
                        //szSpriteExchangeMap.State = -10;

                        //没在传送点坐标范围内
                        szSpriteExchangeMap.State = (int)ErrorCode.ERROR_MAP_CHANGE_NOTINELEPORTDICT;
                        client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    client.ClientData.WaitingNotifyChangeMap = false;
                }
                else //如果是传送点传送，则判断是否合法
                {
                    GameMap gameMap = null;
                    if (!GameManager.MapMgr.DictMaps.TryGetValue(client.ClientData.MapCode, out gameMap))
                    {
                        //szSpriteExchangeMap.State = -11;//地图不存在
                        
                        szSpriteExchangeMap.State = (int)ErrorCode.ERROR_MAP_CHANGE_NOMAP;
                        client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    MapTeleport mapTeleport = null;
                    if (!gameMap.MapTeleportDict.TryGetValue(teleportID, out mapTeleport))
                    {
                        //szSpriteExchangeMap.State = -12;//地图传送点不存在
                        szSpriteExchangeMap.State = (int)ErrorCode.ERROR_MAP_CHANGE_NOTINELEPORTDICT;
                        client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    //if (mapTeleport.ToMapID != newMapCode || mapTeleport.ToX != toNewMapX || mapTeleport.ToY != toNewMapY)
                    //{
                    //    szSpriteExchangeMap.State = -14;//地图传送点坐标错误
                    //    client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                    //    return TCPProcessCmdResults.RESULT_DATA;
                    //}

                    GameMap toGameMap = null;
                    if (!GameManager.MapMgr.DictMaps.TryGetValue(mapTeleport.ToMapID, out toGameMap))
                    {
                        //szSpriteExchangeMap.State = -1000;//目标地图不存在

                        szSpriteExchangeMap.State = (int)ErrorCode.ERROR_MAP_CHANGE_NOTOMAP;
                        client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);


                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    int toLevel = toGameMap.MinLevel;
                    int toChangeLifeLev = toGameMap.MinZhuanSheng;
                    if (client.ClientData.ChangeLifeCount < toChangeLifeLev)
                    {
                        //szSpriteExchangeMap.State = -1001;
                        //转身次数不够
                        szSpriteExchangeMap.State = (int)ErrorCode.ERROR_MAP_CHANGE_LOWCHANGELIFE;
                        client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    if ((client.ClientData.ChangeLifeCount * 400 + client.ClientData.Level) < (toChangeLifeLev * 400 + toLevel))
                    {
                        if (client.ClientData.Level < toLevel)
                        {
                            //szSpriteExchangeMap.State = -1002;

                            //等级不够
                            szSpriteExchangeMap.State = (int)ErrorCode.ERROR_MAP_CHANGE_LOWLEVEL;
                            client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                            return TCPProcessCmdResults.RESULT_DATA;
                        }
                    }
                }

                OnClientChangeMapEventObject eventObject = new OnClientChangeMapEventObject(client, teleportID, newMapCode, toNewMapX, toNewMapY);
                if (!GlobalEventSource4Scene.getInstance().fireEvent(eventObject, (int)sceneType))
                {
                    if (eventObject.Handled)
                    {
                        szSpriteExchangeMap.State = -1;
                        client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                // 传送点同地图传送
                //if (teleportID >= 0 && client.ClientData.MapCode == newMapCode)
                //{
                //    //GameManager.LuaMgr.GotoMap(client, newMapCode, toNewMapX, toNewMapY, toNewDiection);
                //    GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, toNewMapX, toNewMapY, -1);
                //    szSpriteExchangeMap.State = -10010;
                //    client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                //    return TCPProcessCmdResults.RESULT_DATA;
                //}

                //判断是否允许切换到新地图
                if (eventObject.Handled)
                {
                    newMapCode = eventObject.ToMapCode;
                    toNewMapX = eventObject.ToPosX;
                    toNewMapY = eventObject.ToPosY;
                    if (!eventObject.Result)
                    {
                        szSpriteExchangeMap.State = -1;
                        client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                }
                else if (!Global.CanChangeMapCode(client, newMapCode))
                {
                    szSpriteExchangeMap.State = -1;
                    client.sendProtocolCmd<SpriteExchangeMap>((int)CommandID.CMD_MAP_CHANGE, szSpriteExchangeMap);

                    return TCPProcessCmdResults.RESULT_DATA;
                }



                // 从跨服主线地图回非跨服主线地图，必须强制回源服务器
                if (KuaFuMapManager.getInstance().IsKuaFuMap(client.ClientData.MapCode)
                    && !KuaFuMapManager.getInstance().IsKuaFuMap(newMapCode))
                {
                    //目前,跨服主线状态切换地图,强制切换回原服务器
                    Point pixel = Global.GetMapPoint(ObjectTypes.OT_CLIENT, newMapCode, toNewMapX, toNewMapY, 3);
                    Point grid = Global.PixelToGrid(newMapCode, pixel);
                    Global.ModifyMapRecordData(client, (ushort)newMapCode, (ushort)grid.X, (ushort)grid.Y, (int)MapRecordIndexes.InitGameMapPostion);
                    KuaFuManager.getInstance().GotoLastMap(client);
                    return TCPProcessCmdResults.RESULT_OK;
                }

                //切换到新的地图
                if (!GameManager.ClientMgr.ChangeMap(tcpMgr.MySocketListener, pool, client, teleportID, newMapCode, toNewMapX, toNewMapY, toNewDiection, nID))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("切换地图时失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);

            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        private TCPProcessCmdResults ProcessSpritePosCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            // SysConOut.WriteLine("======ProcessSpritePosCmd=======");
            tcpOutPacket = null;

            byte[] msSendData = null;
            SpriteMovePosition szSpriteMovePosition;
            try
            {
                szSpriteMovePosition = DataHelper.BytesToObject<SpriteMovePosition>(data, 0, count);
            }
            catch (Exception ex)
            {
                MemoryStream msResult = new MemoryStream();
                SysConOut.WriteLine("解析切换地图消息错误");
                SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
                szSystemErrorReponse.State = (int)ErrorCode.ERROR_PROTOCOL;

                Serializer.Serialize<SystemErrorReponse>(msResult, szSystemErrorReponse);
                msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            try
            {

                int roleID = (int)szSpriteMovePosition.RoleID;
                int mapCode = (int)szSpriteMovePosition.MapCode;
                int toX = (int)szSpriteMovePosition.ToMapX;
                int toY = (int)szSpriteMovePosition.ToMapY;
                Int64 currentPosTicks = (Int64)szSpriteMovePosition.clientTicks;
                int toDirection = (int)szSpriteMovePosition.ToDiection;
                if (roleID == 15)
                    SysConOut.WriteLine(string.Format("\r\n\r\n更新坐标点X={0} Y={1}", toX, toY));

                //更新当前的坐标位置
                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // 只有收到更新指令，就更新心跳时间 ChenXiaojun
                client.ClientData.LastClientHeartTicks = TimeUtil.NOW();

                //如果地图已经切换
                if (mapCode != client.ClientData.MapCode)
                {
                    client.CheckCheatData.MismatchingMapCode = true;
                    return TCPProcessCmdResults.RESULT_OK;
                }

                client.CheckCheatData.MismatchingMapCode = false;

                //System.Diagnostics.Debug.WriteLine(string.Format("toX={0}, toY={1}, posX={2}, posY={3}, currentPosTicks={4}", toX, toY, client.ClientData.PosX, client.ClientData.PosY, currentPosTicks));

                /// 玩家进行了移动
                if (GameManager.Update9GridUsingNewMode <= 0)
                {
                    if (GameManager.Update9GridUsingPosition > 0)
                    {
                        ClientManager.DoSpriteMapGridMove(client, GameManager.MaxSlotOnPositionUpdate9GridsTicks);
                    }
                }
                long nowTicks = TimeUtil.NOW();
                if (currentPosTicks > 0)
                {
                    int check_cmd_position = GameManager.GameConfigMgr.GetGameConfigItemInt(GameConfigNames.check_cmd_position, 1);
                    if (check_cmd_position > 0)
                    {
                        GameMap gameMap = GameManager.MapMgr.GetGameMap(mapCode);
                        if (null == gameMap)
                        {
                            return TCPProcessCmdResults.RESULT_OK;
                        }

                        if (-1 != toX && -1 != toY && !Global.IsGridReachable(mapCode, toX / gameMap.MapGridWidth, toY / gameMap.MapGridHeight))
                        {
                            //LogManager.WriteLog(LogTypes.Error, string.Format("ProcessSpriteMoveCmd Faild RoleID = {0}, MapCode = {1}, toX = {2}, toY = {3}", roleID, mapCode, toX, toY));
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                    }

                    int oldX = 0, oldY = 0;
                    oldX = client.ClientData.PosX;
                    oldY = client.ClientData.PosY;

                    //将精灵放入格子
                    if (oldX != toX || oldY != toY)
                    {
                        if (!GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode].MoveObject(oldX, oldY, toX, toY, client))
                        {
                            //LogManager.WriteLog(LogTypes.Warning, string.Format("精灵移动超出了地图边界: Cmd={0}, RoleID={1}, 关闭连接", (TCPGameServerCmds)nID, client.ClientData.RoleID));
                            return TCPProcessCmdResults.RESULT_OK; //的确在切换地图时，有误报的情况, 忽略
                        }
                    }
                    client.ClientData.PosX = toX;
                    client.ClientData.PosY = toY;
                    client.ClientData.ReportPosTicks = currentPosTicks;
                    client.ClientData.ServerPosTicks = nowTicks;

                }
                else
                    if (currentPosTicks == 0)
                {
                    client.ClientData.PosX = toX;
                    client.ClientData.PosY = toY;
                    client.ClientData.ReportPosTicks = currentPosTicks;
                    client.ClientData.ServerPosTicks = nowTicks;

                    SpriteMovePositionReponse szSpriteMovePositionReponse = new SpriteMovePositionReponse();
                    szSpriteMovePositionReponse.RoleID = roleID;
                    szSpriteMovePositionReponse.xxx = 1.0;

                    //strcmd = string.Format("{0}:{1}", roleID, 1.0);
                    tcpOutPacket = DataHelper.ProtocolToTCPOutPacket<SpriteMovePositionReponse>(szSpriteMovePositionReponse, pool, nID);//TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                return TCPProcessCmdResults.RESULT_OK;

            }
            catch (Exception ex)
            {
                //System.Windows.Application.Current.Dispatcher.Invoke((MethodInvoker)delegate
                //{
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
                //throw ex;
                //});
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        private TCPProcessCmdResults ProcessSpriteAttackCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            socket.session.SetSocketTime(0);
            socket.session.SetSocketTime(5);
            SysConOut.WriteLine("======ProcessSpriteAttackCmd=======");
            tcpOutPacket = null;
            MemoryStream msResult = new MemoryStream();
            byte[] msSendData = null;
            ProAttackData szProAttackData;
            try
            {
                // msRecvDatas = new MemoryStream(data, 0, count);
                szProAttackData = DataHelper.BytesToObject<ProAttackData>(data, 0, count);// Serializer.Deserialize<SpriteMovePosition>(msRecvDatas);
            }
            catch (Exception ex)
            {
                SysConOut.WriteLine("解析攻击消息错误");
                SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
                szSystemErrorReponse.State = (int)ErrorCode.ERROR_PROTOCOL;

                Serializer.Serialize<SystemErrorReponse>(msResult, szSystemErrorReponse);
                msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            try
            {


                int roleID = (int)szProAttackData.roleID;
                int roleX = (int)szProAttackData.roleX;
                int roleY = (int)szProAttackData.roleY;

                SysConOut.WriteLine("====> roleX " + roleX + " roleY " + roleY);

                int magicCode = (int)szProAttackData.magicCode;
                int WeaponsType = (int)szProAttackData.WeaponsType;
                List<AttackObjInfo> attackedList = szProAttackData.AttackObjList;


                GameClient client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.ClientData.RoleID != roleID)
                {
                    SysConOut.WriteLine(String.Format("{0} 使用技能失败——角色不存在", roleID));
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                client.ClientData.LastClientHeartTicks = TimeUtil.NOW();
                SkillObject szSkillObject = null;
                if (!GameManager.SystemSkillMgr.SystemSkillList.TryGetValue(magicCode, out szSkillObject))
                {
                    SysConOut.WriteLine(String.Format("{0} 使用技能失败——无改技能", roleID));
                    //LogManager.WriteLog(LogTypes.Error, string.Format("GameClient死亡还放技能, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_OK;
                }
                //client.ClientData.CurrentLifeV = 10000;
                // 如果血量为0 不能放技能 [7/23/2014 LiaoWei]
                if (client.ClientData.CurrentLifeV <= 0 || szSkillObject.NeedHP > client.ClientData.CurrentLifeV)
                {
                    SysConOut.WriteLine(String.Format("{0} 使用技能失败——血量不足", roleID));
                    //LogManager.WriteLog(LogTypes.Error, string.Format("GameClient死亡还放技能, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_OK;
                }
                // 如果血量为0 不能放技能 [7/23/2014 LiaoWei]
                if (client.ClientData.CurrentMagicV <= 0 || szSkillObject.NeedMP > client.ClientData.CurrentMagicV)
                {
                    SysConOut.WriteLine(String.Format("{0} 使用技能失败——蓝量不足", roleID));
                    //LogManager.WriteLog(LogTypes.Error, string.Format("GameClient死亡还放技能, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_OK;
                }
                //检测使用得武器
                if (!szSkillObject.NeedWeapons.Contains(WeaponsType))
                {
                    SysConOut.WriteLine(String.Format("{0} 技能与务器不匹配", roleID));
                    //LogManager.WriteLog(LogTypes.Error, string.Format("GameClient死亡还放技能, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_OK;
                }

                System.Console.WriteLine(String.Format("{0} 使用技能 {1}", roleID, magicCode));

                client.ClientData.MoveAndActionNum++;

                GameMap gameMap = GameManager.MapMgr.DictMaps[client.ClientData.MapCode];
                //找到格子中心点
                roleX = gameMap.CorrectWidthPointToGridPoint(roleX);
                roleY = gameMap.CorrectHeightPointToGridPoint(roleY);


                Point szPointAttack;
                if (null != szProAttackData.PointAttack)
                {
                    szPointAttack = new Point((int)szProAttackData.PointAttack.PosX,
                        (int)szProAttackData.PointAttack.PosY);
                    //找到受击格子中心点

                }
                else
                {
                    szPointAttack = new Point(0, 0);
                }


                // 先锁定
                int oldX = 0, oldY = 0;
                oldX = client.ClientData.PosX;
                oldY = client.ClientData.PosY;
                if (Math.Abs(roleX - oldX) > 500 || Math.Abs(roleY - oldY) > 500)
                {
                    System.Console.WriteLine(String.Format("{0} 使用技能失败——距离过远", roleID));
                    return TCPProcessCmdResults.RESULT_OK;
                }


                client.ClientData.PosX = roleX;
                client.ClientData.PosY = roleY;
                client.ClientData.ReportPosTicks = 0;

                //只有新旧位置不一样的时候，才需要格子变换
                if (oldX != roleX || oldY != roleY)
                {
                    //将精灵放入格子
                    if (!GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode].MoveObject(oldX, oldY, roleX, roleY, client))
                    {
                        System.Console.WriteLine(String.Format("{0} 使用技能失败——位置不一致", roleID));
                        //LogManager.WriteLog(LogTypes.Warning, string.Format("精灵移动超出了地图边界: Cmd={0}, RoleID={1}, 关闭连接", (TCPGameServerCmds)nID, client.ClientData.RoleID));
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    /// 玩家进行了移动
                    //Global.GameClientMoveGrid(client);
                }

                //处理精灵攻击动作
                if (GameManager.FlagManyAttack) // 如果是多段攻击 目前看上去默认为多段attackedList
                {
                    AttackManager.ProcessAttack(client, magicCode, szPointAttack, -1, 1.0, attackedList);
                }
                else
                {
                    AttackManager.ProcessAttack(client, magicCode, szPointAttack, 0, 1.0, attackedList);
                }
                msResult.Dispose();
                msResult = null;

                //Random szRand = new Random();
                //ProAttackDataReponse szProAttackDataReponse = new ProAttackDataReponse();
                //szProAttackDataReponse.enemy = enemy;
                //szProAttackDataReponse.burst = 0;
                //szProAttackDataReponse.injure = 100 + szRand.Next(1000);
                //szProAttackDataReponse.enemyLife = 0;
                //szProAttackDataReponse.newExperience = 0;
                //szProAttackDataReponse.currentExperience = client.ClientData.Experience;
                //szProAttackDataReponse.newLevel = client.ClientData.Level;
                //foreach(var s in attackedList)
                //{
                //    szProAttackDataReponse.AttackObjList.Add(s);
                //}

                // tcpOutPacket = DataHelper.ProtocolToTCPOutPacket<ProAttackDataReponse>(szProAttackDataReponse, pool, (int)CommandID.CMD_PLAY_ATTACK);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                // 格式化异常错误信息
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);

            }
            return TCPProcessCmdResults.RESULT_OK;
        }

        private TCPProcessCmdResults ProcessSpriteAttackReadyCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey,
            TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            SysConOut.WriteLine("======ProcessSpriteAttackReadyCmd=======");
            tcpOutPacket = null;
            MemoryStream msResult = new MemoryStream();
            byte[] msSendData = null;
            AttackReadyData szAttackReadyData;
            try
            {
                // msRecvDatas = new MemoryStream(data, 0, count);
                szAttackReadyData = DataHelper.BytesToObject<AttackReadyData>(data, 0, count);// Serializer.Deserialize<SpriteMovePosition>(msRecvDatas);
            }
            catch (Exception ex)
            {
                SysConOut.WriteLine("解析攻击消息错误");
                SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
                szSystemErrorReponse.State = (int)ErrorCode.ERROR_PROTOCOL;

                Serializer.Serialize<SystemErrorReponse>(msResult, szSystemErrorReponse);
                msSendData = msResult.ToArray();
                msResult.Dispose();
                msResult = null;

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, msSendData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }

            GameClient client = GameManager.ClientMgr.FindClient(socket);
            if (null == client)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }
            client.ClientData.LastClientHeartTicks = TimeUtil.NOW();
            List<object> szHitClient = new List<object>();
            {
                SkillObject szSkillObject = null;
                GameManager.SystemSkillMgr.SystemSkillList.TryGetValue((int)szAttackReadyData.SkillID, out szSkillObject);
                if (null == szSkillObject)
                {
                    return 0;
                }
                List<int> szRangeTypeList = szSkillObject.RangeType;
                int[] szRangleType = szRangeTypeList.ToArray();
                Point targetPos = new Point((int)szAttackReadyData.PointAtk.PosX, (int)szAttackReadyData.PointAtk.PosY);

                //扇形
                if (szRangleType[0] == (int)RangleTypeID.ACCTACK_RANGLE_SECTOR)//扇形
                {
                    GameManager.ClientMgr.LookupEnemiesInCircleByAngle(client.ClientData.RoleDirection, client.ClientData.MapCode, client.ClientData.CopyMapID,
                        (int)targetPos.X, (int)targetPos.Y, 2000/*szRangleType[1] * 100*/, szHitClient, szRangleType[2], true);

                }
                else if (szRangleType[0] == (int)RangleTypeID.ACCTACK_RANGLE_CIRCLE)//圆形
                {
                    GameManager.ClientMgr.LookupEnemiesInCircle(client.ClientData.MapCode, client.ClientData.CopyMapID,
                        (int)targetPos.X, (int)targetPos.Y, 2000/*szRangleType[1] * 100*/, szHitClient);
                }
            }
            GameClient Selfclient = GameManager.ClientMgr.FindClient(socket);
            if (null != Selfclient)
            {
                bool szIsFine = false;
                foreach (var s in szHitClient)
                {
                    if ((s as GameClient).ClientData.RoleID == Selfclient.ClientData.RoleID)
                        szIsFine = true;
                }
                if (!szIsFine)
                    szHitClient.Add(Selfclient);
            }

            GameManager.ClientMgr.SendToClients(tcpMgr.MySocketListener, pool, null, szHitClient, data, (int)CommandID.CMD_READY_ATTACK);

            msResult.Dispose();
            msResult = null;
            return TCPProcessCmdResults.RESULT_OK;
        }

    }
}
