
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

//暂时引用cc
using CC;
using GameServer.cc;

namespace GameServer.ch.Role
{
    public class RoleManager
    {
        public void InitRoleManager()
        {
            //CMDProcess.GetInstance.RegisterFun((int)CommandID.CMD_CREATE_ROLE, ProcessSystemErrorCmd);
        }

        
    }
}
