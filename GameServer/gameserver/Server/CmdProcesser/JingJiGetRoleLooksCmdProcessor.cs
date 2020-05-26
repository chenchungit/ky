using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using GameServer.Logic.JingJiChang;

namespace GameServer.Server.CmdProcesser
{
    /// <summary>
    /// add by chenjg. 20160220 
    /// 竞技场打开后主界面，看到的几个人的形象，使用这个消息
    /// 不再使用 CMD_SPR_GETROLEUSINGGOODSDATALIST
    /// </summary>
    class JingJiGetRoleLooksCmdProcessor : ICmdProcessor
    {
        private static JingJiGetRoleLooksCmdProcessor instance = new JingJiGetRoleLooksCmdProcessor();

        private JingJiGetRoleLooksCmdProcessor() { }

        public static JingJiGetRoleLooksCmdProcessor getInstance()
        {
            return instance;
        }

        public bool processCmd(GameClient client, string[] cmdParams)
        {
            int lookWho = Convert.ToInt32(cmdParams[1]);
            PlayerJingJiData jingjiData = Global.sendToDB<PlayerJingJiData, byte[]>((int)TCPGameServerCmds.CMD_DB_JINGJICHANG_GET_DATA, DataHelper.ObjectToBytes<int>(lookWho), client.ServerId);

            if (jingjiData != null)
            {
                // 有一些字段，PlayerJingJiData未保存，理论上客户端单纯显示是不需要这些字段的，如果有需要，那么创建竞技场数据的时候再保存下来
                RoleData4Selector rd = new RoleData4Selector();
                rd.RoleID = jingjiData.roleId;
                rd.RoleName = jingjiData.roleName;
                rd.RoleSex = jingjiData.sex;
                rd.Occupation = jingjiData.occupationId;
                rd.Level = jingjiData.level;
                // rd.Faction = jingjiData暂未保存
                rd.MyWingData = jingjiData.wingData;
                rd.GoodsDataList = JingJiChangManager.GetUsingGoodsList(jingjiData.equipDatas);
                // rd.OtherName = jingjiData暂未保存
                rd.CombatForce = jingjiData.combatForce;
                rd.AdmiredCount = jingjiData.AdmiredCount;
                // rd.FashionWingsID = jingjiData暂未保存;
                rd.SettingBitFlags = jingjiData.settingFlags;

                client.sendCmd<RoleData4Selector>((int)TCPGameServerCmds.CMD_SPR_JINGJICHANG_GET_ROLE_LOOKS, rd);
            }
           
            return true;
        }
    }
}
