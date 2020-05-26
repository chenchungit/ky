using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.GameEvent;
using GameServer.Server;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Server.CmdProcesser;

namespace GameServer.Logic.BangHui.ZhanMengShiJian
{

    /// <summary>
    /// 战盟事件静态常量参数
    /// </summary>
    public class ZhanMengShiJianConstants
    {
        //事件类型=====begin========

        //战盟创建
        public static readonly int CreateZhanMeng = 0;
        //脱离战盟
        public static readonly int LeaveZhanMeng = 1;
        //加入战盟
        public static readonly int JoinZhanMeng = 2;
        //玩家捐赠
        public static readonly int ZhanMengJuanZeng = 3;
        //职位变更
        public static readonly int ChangeZhiWu = 4;
        //建设升级
        public static readonly int ZhanMengLevelup = 5;
        //道具捐赠
        public static readonly int ZhanMengGooodsJuanZeng = 6;
        // 击杀战盟boss
        public static readonly int KillBoss = 7;
        //更改战盟名字
        public static readonly int ChangeName = 8;

        //事件类型======end=========

        //捐赠类型=====begin========
        
        //金币
        public static readonly int JinBi = 1;

        //钻石
        public static readonly int ZuanShi = 2;

        //捐赠类型======end=========

    }

    /// <summary>
    /// 战盟事件管理器
    /// </summary>
    public class ZhanMengShiJianManager : IManager
    {
        private static ZhanMengShiJianManager instance = new ZhanMengShiJianManager();

        private ZhanMengShiJianManager() { }

        public static ZhanMengShiJianManager getInstance()
        {
            return instance;
        }

        public bool initialize()
        {
            //注册战盟事件指令处理器
            TCPCmdDispatcher.getInstance().registerProcessor((int)TCPGameServerCmds.CMD_SPR_ZHANMENGSHIJIAN_DETAIL, 2, ZhanMengShiJianDetailCmdProcessor.getInstance());
            //向事件源注册监听器
            GlobalEventSource.getInstance().registerListener((int)EventTypes.ZhanMengShiJian, ZhanMengShiJianEventListener.getInstance());
            return true;
        }

        public bool startup()
        {
            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            //向事件源删除监听器
            GlobalEventSource.getInstance().removeListener((int)EventTypes.ZhanMengShiJian, ZhanMengShiJianEventListener.getInstance());
            return true;
        }

        public void addZhanMengShiJian(int BhId, string RoleName, int ShijianType, int Param1, int Param2, int Param3, int serverId)
        {
            string cmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", BhId, RoleName, ShijianType, Param1, Param2, Param3);

            Global.sendToDB<string, string>((int)TCPGameServerCmds.CMD_DB_ADD_ZHANMENGSHIJIAN, cmd, serverId);
        }

    }

    /// <summary>
    /// 战盟事件监听器
    /// </summary>
    public class ZhanMengShiJianEventListener : IEventListener
    {

        private static ZhanMengShiJianEventListener instance = new ZhanMengShiJianEventListener();

        private ZhanMengShiJianEventListener() { }

        public static ZhanMengShiJianEventListener getInstance()
        {
            return instance;
        }

        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() != (int)EventTypes.ZhanMengShiJian)
                return;

            ZhanMengShijianEvent eventObj = (ZhanMengShijianEvent)eventObject;

            //触发事件
            ZhanMengShiJianManager.getInstance().addZhanMengShiJian(eventObj.BhId, eventObj.RoleName, eventObj.ShijianType, eventObj.Param1, eventObj.Param2, eventObj.Param3, eventObj.ServerId);

        }
    }

    /// <summary>
    /// 战盟事件
    /// </summary>
    public class ZhanMengShijianEvent : ZhanMengShijianBaseEventObject
    {
        public ZhanMengShijianEvent(string roleName, int bhId, int shijianType, int param1, int param2, int param3, int serverId) : base(roleName, bhId, shijianType, param1, param2, param3, serverId)
        {
            this.roleName = roleName;
            this.bhId = bhId;
            this.shijianType = shijianType;
            this.param1 = param1;
            this.param2 = param2;
            this.param3 = param3;
        }

        public static ZhanMengShijianEvent createCreateZhanMengEvent(string roleName, int bhId, int serverId)
        {
            return new ZhanMengShijianEvent(roleName, bhId, ZhanMengShiJianConstants.CreateZhanMeng, -1, -1, -1, serverId);
        }
        public static ZhanMengShijianEvent createJoinZhanMengEvent(string roleName, int bhId, int serverId)
        {
            return new ZhanMengShijianEvent(roleName, bhId, ZhanMengShiJianConstants.JoinZhanMeng, -1, -1, -1, serverId);
        }
        public static ZhanMengShijianEvent createLeaveZhanMengEvent(string roleName, int bhId, int serverId)
        {
            return new ZhanMengShijianEvent(roleName, bhId, ZhanMengShiJianConstants.LeaveZhanMeng, -1, -1, -1, serverId);
        }
        public static ZhanMengShijianEvent createZhanMengJuanZengEvent(string roleName, int bhId, int money, int moneyType, int bangGong, int serverId)
        {
            return new ZhanMengShijianEvent(roleName, bhId, ZhanMengShiJianConstants.ZhanMengJuanZeng, money, moneyType, bangGong, serverId);
        }

        public static ZhanMengShijianEvent createZhanMengGoodsJuanZengEvent(string roleName, int bhId, int nGoodID, int nGoodNum, int bangGong, int serverId)
        {
            return new ZhanMengShijianEvent(roleName, bhId, ZhanMengShiJianConstants.ZhanMengGooodsJuanZeng, nGoodID, nGoodNum, bangGong, serverId);
        }

        public static ZhanMengShijianEvent createChangeZhiWuEvent(string roleName, int bhId, int zhiwu, int otherRoleID, int serverId)
        {
            return new ZhanMengShijianEvent(roleName, bhId, ZhanMengShiJianConstants.ChangeZhiWu, zhiwu, -1, otherRoleID, serverId);
        }

        public static ZhanMengShijianEvent createKillBossEvent(string roleName, int bhId, int fubenid, int serverId)
        {
            return new ZhanMengShijianEvent(roleName, bhId, ZhanMengShiJianConstants.KillBoss, fubenid, -1, -1, serverId);
        }
    }
}
