using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic;
using Tmsk.Contract;
using GameServer.Interface;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 主动离开当前地图
    /// </summary>
    public class PreGotoLastMapEventObject : EventObjectEx
    {
        public GameClient Player;
        public int SceneType;

        public PreGotoLastMapEventObject(GameClient player, int sceneType)
            : base((int)EventTypes.PreGotoLastMap)
        {
            Player = player;
            SceneType = sceneType;
        }
    }

    /// <summary>
    /// 玩家升级事件
    /// </summary>
    public class PreInstallJunQiEventObject : EventObjectEx
    {
        public GameClient Player;
        public int NPCID;
        public int SceneType;

        public PreInstallJunQiEventObject(GameClient player, int npcID, int sceneType)
            : base((int)EventTypes.PreInstallJunQi)
        {
            Player = player;
            NPCID = npcID;
            SceneType = sceneType;
        }
    }

    /// <summary>
    /// CampNoAttack怪，伤害计算
    /// </summary>
    public class PreMonsterInjureEventObject : EventObjectEx
    {
        public int SceneType;
        public IObject Attacker;
        public Monster Monster;
        public int Injure;

        public PreMonsterInjureEventObject(IObject attacker, Monster monster, int sceneType)
            : base((int)EventTypes.PreMonsterInjure)
        {
            Attacker = attacker;
            Monster = monster;
            SceneType = sceneType;
        }
    }

    /// <summary>
    /// 战盟添加成员事件
    /// </summary>
    public class PreBangHuiAddMemberEventObject : EventObjectEx
    {
        public GameClient Player;
        public int BHID;

        public PreBangHuiAddMemberEventObject(GameClient player, int bhid)
            : base((int)EventTypes.PreBangHuiAddMember)
        {
            Player = player;
            BHID = bhid;
            Result = true;
        }
    }
    /// <summary>
    /// 战盟添加删除事件
    /// </summary>
    public class PreBangHuiRemoveMemberEventObject : EventObjectEx
    {
        public GameClient Player;
        public int BHID;

        public PreBangHuiRemoveMemberEventObject(GameClient player, int bhid)
            : base((int)EventTypes.PreBangHuiRemoveMember)
        {
            Player = player;
            BHID = bhid;
            Result = true;
        }
    }
    /// <summary>
    /// 战盟添加删除事件
    /// </summary>
    public class PostBangHuiChangeEventObject : EventObjectEx
    {
        public GameClient Player;
        public int BHID;

        public PostBangHuiChangeEventObject(GameClient player, int bhid)
            : base((int)EventTypes.PostBangHuiChange)
        {
            Player = player;
            BHID = bhid;
            Result = true;
        }
    }

    /// <summary>
    /// 处理点击NPC事件
    /// </summary>
    public class ProcessClickOnNpcEventObject : EventObjectEx
    {
        public GameClient Client;
        public NPC Npc;
        public int NpcId;
        public int ExtensionID;

        public ProcessClickOnNpcEventObject(GameClient client, NPC npc, int npcId, int extensionID)
            : base((int)EventTypes.ProcessClickOnNpc)
        {
            Client = client;
            Npc = npc;
            NpcId = npcId;
            ExtensionID = extensionID;
        }
    }

    /// <summary>
    /// 处理点击NPC事件
    /// </summary>
    public class OnStartPlayGameEventObject : EventObject
    {
        public GameClient Client;

        public OnStartPlayGameEventObject(GameClient client)
            : base((int)EventTypes.StartPlayGame)
        {
            Client = client;
        }
    }

    /// <summary>
    /// 处理点击NPC事件
    /// </summary>
    public class OnClientChangeMapEventObject : EventObjectEx
    {
        public GameClient Client;
        public int TeleportID;
        public int ToMapCode;
        public int ToPosX;
        public int ToPosY;

        public OnClientChangeMapEventObject(GameClient client, int teleportID, int toMapCode, int toPosX, int toPosY)
            : base((int)EventTypes.OnClientChangeMap)
        {
            Client = client;
            TeleportID = teleportID;
            ToMapCode = toMapCode;
            ToPosX = toPosX;
            ToPosY = toPosY;
        }
    }

    /// <summary>
    /// 处理点击NPC事件
    /// </summary>
    public class OnCreateMonsterEventObject : EventObjectEx
    {
        public Monster Monster;
        public OnCreateMonsterEventObject(Monster monster)
            : base((int)EventTypes.OnCreateMonster)
        {
            Monster = monster;
        }
    }
    /// <summary>
    /// 战盟成员职务变更前的许可确认
    /// </summary>
    public class PreBangHuiChangeZhiWuEventObject : EventObjectEx
    {
        public GameClient Player;
        public int BHID;
        public int TargetRoleId;
        public int TargetZhiWu;

        public PreBangHuiChangeZhiWuEventObject(GameClient player, int bhid, int targetRoleId, int targetZhiWu)
            : base((int)EventTypes.PreBangHuiChangeZhiWu)
        {
            Player = player;
            BHID = bhid;
            TargetRoleId = targetRoleId;
            TargetZhiWu = targetZhiWu;
        }
    }
}
