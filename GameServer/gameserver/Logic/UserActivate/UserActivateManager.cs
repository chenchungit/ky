using GameServer.Server;
using GameServer.Tools;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tmsk.Contract;

namespace GameServer.Logic.UserActivate
{
    public class UserActivateManager : ICmdProcessorEx, IManager
    {
        #region ----------接口

        private enum EUserActivateState
        {
            NotOpen     = -8, //功能未开放
            EnoBind     = -7,//未绑定
            EBag        = -6,//背包位置不足
            ENoAward    = -5,//奖励错误
            EFail       = -4,//领取失败
            EIsAward    = -3,//已经领取
            EPlatform   = -2,//平台错误（仅ios）
            ECheck      = -1,//校验错误
            Default     = 0,//未领取
            Success     = 1,//成功，已领取
        }

        private static UserActivateManager instance = new UserActivateManager();
        public static UserActivateManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            return true;
        }

        public bool startup()
        {
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ACTIVATE_INFO, 5, 5, getInstance());
            TCPCmdDispatcher.getInstance().registerProcessorEx((int)TCPGameServerCmds.CMD_SPR_ACTIVATE_AWARD, 5, 5, getInstance());

            return true;
        }

        public bool showdown() { return true; }
        public bool destroy() { return true; }
        public bool processCmd(GameClient client, string[] cmdParams) { return true; }

        public bool processCmdEx(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            switch (nID)
            {
                case (int)TCPGameServerCmds.CMD_SPR_ACTIVATE_INFO:
                    return ProcessCmdActivateInfo(client, nID, bytes, cmdParams);
                case (int)TCPGameServerCmds.CMD_SPR_ACTIVATE_AWARD:
                    return ProcessCmdActivateAward(client, nID, bytes, cmdParams);
            }

            return true;
        }

        private bool ProcessCmdActivateInfo(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                // 如果1.9的功能没开放
                if (GameFuncControlManager.IsGameFuncDisabled(GameFuncType.System1Dot9))
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ACTIVATE_INFO, (int)EUserActivateState.NotOpen);
                    return true;
                }
	
                bool isCheck = CheckHelper.CheckCmdLengthAndRole(client, nID, cmdParams, 5);
                if (!isCheck) return false;

                int roleID = int.Parse(cmdParams[0]);
                string userID = cmdParams[1];
                int activateType = Convert.ToInt32(cmdParams[2]);
                string activateInfo = cmdParams[3].ToLower();
                string error = cmdParams[4];

                string checkInfo = GetCheckInfo(userID, error, activateType);
                if (checkInfo != activateInfo)
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ACTIVATE_INFO, (int)EUserActivateState.ECheck);
                    return true;
                }

                PlatformTypes platformType = GameCoreInterface.getinstance().GetPlatformType();
                if (platformType != PlatformTypes.APP)
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ACTIVATE_INFO, (int)EUserActivateState.EPlatform);
                    return true;
                }

                if (activateType != 0)
                {
                    client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ACTIVATE_INFO, (int)EUserActivateState.EnoBind);
                    return true;
                }

                int awardState = DBActivateStateGet(client);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ACTIVATE_INFO, awardState);

                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        private bool ProcessCmdActivateAward(GameClient client, int nID, byte[] bytes, string[] cmdParams)
        {
            try
            {
                bool isCheck = CheckHelper.CheckCmdLength(client, nID, cmdParams, 5);
                if (!isCheck) return false;

                int roleID = int.Parse(cmdParams[0]);
                string userID = cmdParams[1];
                int activateType = Convert.ToInt32(cmdParams[2]);
                string activateInfo = cmdParams[3].ToLower();
                string error = cmdParams[4];

                EUserActivateState state = ActivateAward(client, roleID, userID, activateType, activateInfo,error);
                client.sendCmd((int)TCPGameServerCmds.CMD_SPR_ACTIVATE_AWARD, (int)state);
                return true;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(client.ClientSocket), false);
            }

            return false;
        }

        private EUserActivateState ActivateAward(GameClient client,int roleId,string userID,int activateType, string activateInfo,string error)
        {
            string checkInfo = GetCheckInfo(userID, error, activateType);
            if (checkInfo != activateInfo) return EUserActivateState.ECheck;

            if (activateType != 0) return EUserActivateState.EnoBind;

            PlatformTypes platformType = GameCoreInterface.getinstance().GetPlatformType();
            if (platformType != PlatformTypes.APP) return EUserActivateState.EPlatform;

            int awardState = DBActivateStateGet(client);
            if (awardState == 1)return EUserActivateState.EIsAward;

            List<GoodsData> awardList = GetAwardList();
            if (awardList == null || awardList.Count <= 0) return EUserActivateState.ENoAward;
            if (!Global.CanAddGoodsDataList(client, awardList)) return EUserActivateState.EBag;

            bool result = DBActivateStateSet(client);
            if (!result) return EUserActivateState.EFail;

            for (int i = 0; i < awardList.Count; i++)
            {
                Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client,
                    awardList[i].GoodsID, awardList[i].GCount, awardList[i].Quality, "", awardList[i].Forge_level,
                    awardList[i].Binding, 0, "", true, 1,
                    /**/"账号绑定奖励", Global.ConstGoodsEndTime, awardList[i].AddPropIndex, awardList[i].BornIndex,
                    awardList[i].Lucky, awardList[i].Strong, awardList[i].ExcellenceInfo, awardList[i].AppendPropLev);
            }

            return EUserActivateState.Success;
        }

        private string GetCheckInfo(string userID,string error,int activateType)
        {
            userID = userID.ToLower().Replace("apps", "");
            string key = "WwSiia943ui3Wej5NrqUI3rfqrf83quj";
            string result = string.Format("{0}error={1}&ret={2}&uid={3}",key,error,activateType,userID);
            
            result = MD5Helper.get_md5_string(result);
            return result.ToLower();
        }

        private List<GoodsData> GetAwardList()
        {
            List<GoodsData> list = new List<GoodsData>();

            string[] fields;
            string str = GameManager.systemParamsList.GetParamValueByName("App_BindPhoneAward");
            if (string.IsNullOrEmpty(str)) return null;

            fields = str.Split('|');
            if (fields.Length > 0)
                list = GoodsHelper.ParseGoodsDataList(fields, "SystemParams.xml");

            return list;
        }

        private int DBActivateStateGet(GameClient client)
        {
            int result = 0;
            string cmd2db = string.Format("{0}", client.strUserID);
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_ACTIVATE_GET, cmd2db, client.ServerId);
            if (null != dbFields && dbFields.Length == 1)
                result = int.Parse(dbFields[0]);

            return result;
        }

        private bool DBActivateStateSet(GameClient client)
        {
            bool result = false;
            string cmd2db = string.Format("{0}:{1}:{2}", client.ClientData.ZoneID,client.strUserID,client.ClientData.RoleID);
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_ACTIVATE_SET, cmd2db, client.ServerId);
            if (null != dbFields && dbFields.Length == 1)
                result = (dbFields[0] == "1");

            return result;
        }

        #endregion
    }
}
