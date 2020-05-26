using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Tools.Pattern;
using Server.Tools;

namespace GameServer.Logic.WanMota
{
    public class WanMoTaTopLayerManager : SingletonTemplate<WanMoTaTopLayerManager>
    {
        private WanMoTaTopLayerManager() { }

        private int iTopLayer = 0;
        private object TopLayerMutex = new object();

        public void CheckNeedUpdate(int layer)
        {
            lock (TopLayerMutex)
            {
                if (iTopLayer < layer)
                {
                    iTopLayer = layer;
                }
            }
        }

        public void OnClientPass(GameClient client, int layer)
        {
            // 当用户首次通关30层开始，每通关10层万魔塔副本，显示游戏公告
            if (layer >= 30 && layer % 10 == 0)
            {
                // 玩家【用户名字】勇往直前，勇不可挡，通过了万魔塔第XX层！
                string broadCastMsg = StringUtil.substitute(Global.GetLang("玩家【{0}】勇往直前，勇不可挡，通过了万魔塔第{1}层！"),
                                                            Global.FormatRoleName(client, client.ClientData.RoleName), layer);

                //播放用户行为消息
                Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.HintMsg, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
            }

            // 当用户通关超过30层，成功成为万魔塔第一名的角色时，显示游戏公告
            if (layer >= 30)
            {
                bool bTop1 = false;
                lock (TopLayerMutex)
                {
                    if (iTopLayer < layer)
                    {
                        bTop1 = true;
                        iTopLayer = layer;
                    }
                }

                if (bTop1)
                {
                    string broadCastMsg = StringUtil.substitute(Global.GetLang("玩家【{0}】已势如破竹，雄霸万魔榜首！"),
                                                                Global.FormatRoleName(client, client.ClientData.RoleName));

                    //播放用户行为消息
                    Global.BroadcastRoleActionMsg(client, RoleActionsMsgTypes.HintMsg, broadCastMsg, true, GameInfoTypeIndexes.Hot, ShowGameInfoTypes.OnlySysHint);
                }
            }
        }
    }
}
