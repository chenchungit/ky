using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Server.Data;
using GameServer.Interface;
using Tmsk.Contract;
//using System.Windows.Threading;

namespace GameServer.Logic
{
    /// <summary>
    /// 替代lua区域脚本
    /// </summary>
    public class ProcessAreaScripts
    {
        /// <summary>
        /// 为了提高效率替代lua脚本
        /// </summary>
        /// <param name="gameMap"></param>
        /// <param name="areaLuaID"></param>
        /// <param name="functionName"></param>
        public static void ProcessScripts(GameClient client, string LuaScriptFileName, string functionName, int areaLuaID)
        {
            LuaScriptFileName = LuaScriptFileName.ToLower();
            functionName = functionName.ToLower();

            if ("anquanqu.lua" == LuaScriptFileName) //安全区
            {
                if ("enterarea" == functionName)
                {
                    GameManager.LuaMgr.BroadcastMapRegionEvent(client, areaLuaID, (int)RegionEventTypes.SafeRegion, 1);
                }
                else if ("leavearea" == functionName)
                {
                    GameManager.LuaMgr.BroadcastMapRegionEvent(client, areaLuaID, (int)RegionEventTypes.SafeRegion, 0);
                }
            }
            else if ("jinqu.lua" == LuaScriptFileName) //禁区
            {
                if ("enterarea" == functionName)
                {
                    GameManager.LuaMgr.BroadcastMapRegionEvent(client, areaLuaID, (int)RegionEventTypes.JinQu, 1);
                }
                else if ("leavearea" == functionName)
                {
                    GameManager.LuaMgr.BroadcastMapRegionEvent(client, areaLuaID, (int)RegionEventTypes.JinQu, 0);
                }
            }
            else if ("jiaofu.lua" == LuaScriptFileName) //交付物品区
            {
                if ("enterarea" == functionName)
                {
                    GameManager.LuaMgr.BroadcastMapRegionEvent(client, areaLuaID, (int)RegionEventTypes.JiaoFu, 1);
                }
                else if ("leavearea" == functionName)
                {
                    GameManager.LuaMgr.BroadcastMapRegionEvent(client, areaLuaID, (int)RegionEventTypes.JiaoFu, 0);
                }
            }
            else if ("rmtempcunmin.lua" == LuaScriptFileName)
            {
                if ("enterarea" == functionName)
                {

                }
                else if ("leavearea" == functionName)
                {
                    GameManager.LuaMgr.RemoveNPCForClient(client, 17);
                }
            }
            else if ("caijiyaocao.lua" == LuaScriptFileName)
            {
                if ("enterarea" == functionName)
                {
	                GameManager.LuaMgr.Error(client, "风云突变，天降大雨");
                    GameManager.LuaMgr.SendGameEffect(client, "xiayu1.swf", 0, 1, "xiayu2.mp3");
                }
                else if ("leavearea" == functionName)
                {
                    GameManager.LuaMgr.SendGameEffect(client, "", 0);
                }
            }
            else if ("gouhuo.lua" == LuaScriptFileName)
            {
                if ("enterarea" == functionName)
                {
                    GameManager.LuaMgr.NotifySelfDeco(client, 60000, 1, -1, 6112, 2660 - 140, 0, -1, -1, 0, 0);
                }
                else if ("leavearea" == functionName)
                {
                    GameManager.LuaMgr.NotifySelfDeco(client, 60000, -1, -1, 0, 0, 0, -1, -1, 0, 0);
                }
            }
        }
    }
}
