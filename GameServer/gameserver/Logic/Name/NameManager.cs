using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Tools.Pattern;
using GameServer.Server;
using Server.TCP;
using Server.Protocol;
using Server.Tools;
using GameServer.Logic.Copy;
using GameServer.Logic.SecondPassword;
using Server.Data;
using GameServer.Logic.UnionAlly;

namespace GameServer.Logic.Name
{
    // 更改角色名
    public enum ChangeNameError
    {
        Success = 0,        //改名成功
        InvalidName = 1,    //新名字非法
        DBFailed = 2,       //数据库错误
        NoChangeNameTimes = 3,  //没有改名权限
        SelfIsBusy = 4, //当前忙(目前跨服中不可改名)
        NameAlreayUsed = 5, //名字已被占用
        NameLengthError = 6, //名字长度错误
        NotContainRole = 7, //账号下没有该角色
        NeedVerifySecPwd = 8, //需要验证二级密码
        ZuanShiNotEnough = 9, //钻石不足
        ServerDenied = 10, //服务器拒绝(改名操作未开放)
        BackToSelectRole = 11, //只能在选角色界面改名
    }

    // 更改帮会名
    public enum EChangeGuildNameError
    {
        Success = 0, //成功
        InvalidName = 1, // 新名字非法
        DBFailed = 2, // 数据库错误
        NameAlreadyUsed = 3, // 名字已被占用
        OperatorDenied = 4, // 服务器拒绝
        LengthError = 5, // 名字长度错误
    }

    public class NameManager : SingletonTemplate<NameManager>
    {
        private NameManager() { }

        private int NameMinLen = 2;
        private int NameMaxLen = 32;//HX_Server 7->32

        public int CostZuanShiBase = 300;
        public int CostZuanShiMax = 1500;

        public void LoadConfig()
        {
            try
            {
                int[] arr = GameManager.systemParamsList.GetParamValueIntArrayByName("NameLengthRange");

                if (arr != null && arr.Length >= 2)
                {
                    this.NameMinLen = arr[0];
                    this.NameMaxLen = arr[1];
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, "NameManager.LoadConfig", ex);
                this.NameMinLen = 2;
                this.NameMaxLen = 7;
            }
        }

        // 当前在选角界面改名，所以socket登录的有账号，但不能有客户端
        public TCPProcessCmdResults ProcessChangeName(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                int roleId = Convert.ToInt32(fields[0]);
                int zoneId = Convert.ToInt32(fields[1]);
                string newName = fields[2]; //新名字

                string uid = GameManager.OnlineUserSession.FindUserID(socket);
                if (string.IsNullOrEmpty(uid))
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("角色改名时，找不到socket对应的uid，其中roleid={0}，zoneid={1}，newname={2}", roleId, zoneId, newName));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                ChangeNameResult result = new ChangeNameResult();

                // 跨服以及登录游戏后禁止改名
                if (socket.IsKuaFuLogin || GameManager.ClientMgr.FindClient(socket) != null)
                {
                    result.ErrCode = (int)ChangeNameError.BackToSelectRole;
                }
                else
                {
                    result.ErrCode = (int)HandleChangeName(uid, zoneId, roleId, newName);
                }

                result.ZoneId = zoneId;
                result.NewName = newName;
                result.NameInfo = GetChangeNameInfo(uid, zoneId, socket.ServerId);

                tcpOutPacket = DataHelper.ObjectToTCPOutPacket(result, pool, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
            return TCPProcessCmdResults.RESULT_DATA;
        }

        private ChangeNameError HandleChangeName(string uid, int zoneId, int roleId, string newName)
        {
            // 如果1.6的功能没开放
            if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot6))
            {
                return ChangeNameError.ServerDenied;
            }

            SecPwdState pwdState = SecondPasswordManager.GetSecPwdState(uid);
            if (pwdState != null && pwdState.NeedVerify)
            {
                // 二级密码尚未验证
                return ChangeNameError.NeedVerifySecPwd;
            }

            // 非法字符
            if (string.IsNullOrEmpty(newName) || NameServerNamager.CheckInvalidCharacters(newName) <= 0)
            {
                return ChangeNameError.InvalidName;
            }

            // 检测长度
            if (!IsNameLengthOK(newName))
            {
                return ChangeNameError.NameLengthError;
            }

            /*
            // 跨服禁止改名
            // 组队情况下禁止改名, 副本开房间等待中禁止改名(队伍或者开房间改名需要广播，禁止掉)
            // 非常规地图禁止改名，(副本中可能需要更新各个角色的战斗积分信息，禁止掉)
            if (client.ClientSocket.IsKuaFuLogin
                || client.ClientData.TeamID > 0 || CopyTeamManager.getInstance().FindRoleID2TeamID(client.ClientData.RoleID) > 0
                || MapTypes.Normal != Global.GetMapType(client.ClientData.MapCode)
                )
            {
                return ChangeNameError.SelfIsBusy;
            }
             */

            if (NameServerNamager.RegisterNameToNameServer(zoneId, uid, new string[]{newName}, 0, roleId) <= 0)
            {
                return ChangeNameError.NameAlreayUsed;
            }

            int canFreeMod = GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.FreeModName) ? 1 : 0;
            int canZuanShiMod = GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(VersionSystemOpenKey.ZuanShiModName) ? 1 : 0;

            // db上重点检查名字是否重复
            string[] dbRet = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_SPR_CHANGE_NAME,
                string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", uid, zoneId, roleId, newName, CostZuanShiBase, CostZuanShiMax, canFreeMod, canZuanShiMod), 
                GameManager.LocalServerId);
            if (dbRet == null || dbRet.Length != 4)
            {
                return ChangeNameError.DBFailed;
            }

            int ec = Convert.ToInt32(dbRet[0]);
            string oldName = dbRet[1];
            int costDiamond = Convert.ToInt32(dbRet[2]);
            int leftDiamond = Convert.ToInt32(dbRet[3]);

            if (ec == (int)ChangeNameError.Success)
            {
                if (costDiamond > 0)
                {
                    /**/string msg = "改名 " + oldName + " -> " + newName;
                    Global.AddRoleUserMoneyEvent(roleId, "-", costDiamond, msg);
                    GameManager.logDBCmdMgr.AddDBLogInfo(-1, "钻石", "改名", oldName, newName, "减少", costDiamond, zoneId, uid, leftDiamond, GameManager.LocalServerId);
                    EventLogManager.AddMoneyEvent(GameManager.ServerId, zoneId, uid, roleId, OpTypes.AddOrSub, OpTags.Use, MoneyTypes.YuanBao, -costDiamond, -1, "改名");
                }
                _OnChangeNameSuccess(roleId, oldName, newName);
            }

            return (ChangeNameError)ec;
        }

        private void _OnChangeNameSuccess(int roleId, string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                return;
           }

            // 改名成功后，GameServer需要处理的事情
            RoleName2IDs.OnChangeName(roleId, oldName, newName);

            // 通知配偶, 更新婚宴缓存角色名
            MarryLogic.OnChangeName(roleId, oldName, newName);

            // 通知pk之王，有人改名，用于判断是否更新pk之王雕像，pk之王最高分名字
            GameManager.ArenaBattleMgr.OnChangeName(roleId, oldName, newName);

            // 罗兰城主
            if (LuoLanChengZhanManager.getInstance().GetLuoLanChengZhuRoleID() == roleId)
            {
                // 重新显示罗兰城主的时候，重新加载罗兰城主角色id
                LuoLanChengZhanManager.getInstance().OnChangeName(roleId, oldName, newName);
            }

            // 血色城堡更新最高积分者名字
            GameManager.BloodCastleCopySceneMgr.OnChangeName(roleId, oldName, newName);

            // 恶魔广场更新最高积分者名字
            GameManager.DaimonSquareCopySceneMgr.OnChangeName(roleId, oldName, newName);

            // 阵营战
            GameManager.BattleMgr.OnChangeName(roleId, oldName, newName);

            // 天使神殿
            GameManager.AngelTempleMgr.OnChangeName(roleId, oldName, newName);

            // boss击杀记录
            MonsterBossManager.OnChangeName(roleId, oldName, newName);

            // 节日赠送排行榜
            Logic.ActivityNew.JieRiGiveKingActivity gkAct = HuodongCachingMgr.GetJieriGiveKingActivity();
            if (gkAct != null)
            {
                gkAct.OnChangeName(roleId, oldName, newName);
            }

            // 节日收取排行榜
            Logic.ActivityNew.JieRiRecvKingActivity rkAct = HuodongCachingMgr.GetJieriRecvKingActivity();
            if (rkAct != null)
            {
                rkAct.OnChangeName(roleId, oldName, newName);
            }

            AllyManager.getInstance().UnionLeaderChangName(roleId, oldName, newName);
        }

        public bool IsNameLengthOK(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (name.Length < this.NameMinLen || name.Length > this.NameMaxLen)
            {
                return false;
            }

            return true;
        }

        public ChangeNameInfo GetChangeNameInfo(string uid, int zoneId, int serverId)
        {
            return Global.sendToDB<ChangeNameInfo, string>((int)TCPGameServerCmds.CMD_NTF_EACH_ROLE_ALLOW_CHANGE_NAME, string.Format("{0}:{1}", uid, zoneId), serverId);
        }

        public void GM_ChangeNameTest(GameClient client, string newName)
        {
          //  HandleChangeName(client.ClientSocket, client.ClientData.ZoneID, client.ClientData.RoleID, newName, TCPManager.getInstance(), TCPOutPacketPool.getInstance(), TCPClientPool.getInstance(), null);
        }

        public void GM_SetFreeModName(int roleid, int count)
        {
            GameClient client = GameManager.ClientMgr.FindClient(roleid);
            if (client != null)
            {
                int leftCount =  Global.GetRoleParamsInt32FromDB(client, RoleParamName.LeftFreeChangeNameTimes);
                int newCount = count + leftCount;
                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.LeftFreeChangeNameTimes, newCount, true);
            }
            else
            {
                Global.UpdateRoleParamByNameOffline(roleid, RoleParamName.LeftFreeChangeNameTimes, count.ToString(), GameManager.LocalServerId);
            }
        }

        #region 帮会改名
        public void GM_ChangeBangHuiName(GameClient client, string newName)
        {
            if (client == null) return;

            HandleChangeBangHuiName(client, newName);
        }

        public TCPProcessCmdResults ProcessChangeBangHuiName(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                int roleId = Convert.ToInt32(fields[0]);
                string newName = fields[1]; //新名字

                GameClient client = GameManager.ClientMgr.FindClient(socket); // 找客户端
                if (null == client || client.ClientData.RoleID != roleId)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("根据RoleID定位GameClient对象失败, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleId));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.ClientSocket.IsKuaFuLogin)
                    return TCPProcessCmdResults.RESULT_OK;

                if (client.ClientData.Faction <= 0)
                    return TCPProcessCmdResults.RESULT_OK;

                EChangeGuildNameError ne = HandleChangeBangHuiName(client, newName);
                string rsp = string.Format("{0}:{1}:{2}", (int)ne, client.ClientData.Faction, newName);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(TCPOutPacketPool.getInstance(), rsp, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "", false);
            }

            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", (int)TCPGameServerCmds.CMD_DB_ERR_RETURN);
            return TCPProcessCmdResults.RESULT_DATA;
        }


        private EChangeGuildNameError HandleChangeBangHuiName(GameClient client, string newName)
        {
            EChangeGuildNameError ne = EChangeGuildNameError.OperatorDenied;
            // 非法字符
            if (string.IsNullOrEmpty(newName) || NameServerNamager.CheckInvalidCharacters(newName) <= 0)
            {
                ne = EChangeGuildNameError.InvalidName;
            }
            else if (!IsNameLengthOK(newName)) // 检测长度
            {
                ne = EChangeGuildNameError.LengthError;
            }
            else
            {
                string[] result = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_SPR_CHANGE_BANGHUI_NAME, string.Format("{0}:{1}:{2}", client.ClientData.RoleID, client.ClientData.Faction, newName), client.ServerId);
                if (result == null || result.Length < 1) ne = EChangeGuildNameError.DBFailed;
                else ne = (EChangeGuildNameError)Convert.ToInt32(result[0]);
            }

            if (ne == EChangeGuildNameError.Success)
            {
                client.ClientData.BHName = newName;

                //通知所有指定帮会的在线用户帮会已经改名
                GameManager.ClientMgr.NotifyBangHuiChangeName(client.ClientData.Faction, newName);

                //通知GameServer同步领地帮会分布
                JunQiManager.NotifySyncBangHuiLingDiItemsDict();

                //更新缓存项
                Global.UpdateBangHuiMiniDataName(client.ClientData.Faction, newName);

                //罗兰城主
                LuoLanChengZhanManager.getInstance().ReShowLuolanKing();

                // pk之王
                if (GameManager.ArenaBattleMgr.GetPKKingRoleID() == client.ClientData.RoleID)
                {
                    GameManager.ArenaBattleMgr.ReShowPKKing();
                }

                AllyManager.getInstance().UnionDataChange(client.ClientData.Faction, client.ServerId);
            }

            return ne;
        }
        #endregion
    }
}

