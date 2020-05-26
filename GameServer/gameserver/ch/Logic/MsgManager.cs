#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using System.IO;
using ProtoBuf;
using Server.Data;
using Server.TCP;
using Server.Tools;
//using System.Windows.Forms;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
using GameServer.Server;
using GameServer.Interface;
using GameServer.Logic.JingJiChang;
using GameServer.Core.Executor;

using GameServer.Logic.RefreshIconState;
using System.Threading;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic.NewBufferExt;
using Tmsk.Contract;
using System.Collections.Concurrent;
using GameServer.Logic.ActivityNew.SevenDay;
using CC;
using GameServer.cc.Attack;
using GameServer.cc.Skill;
using GameServer.Logic;

namespace GameServer.ch.Logic
{
    public class SystemMsgManager
    {
        //服务器弹窗公告
        public void NotifySystemAlertToClient(Object obj, int _errID)
        {
            SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
            szSystemErrorReponse.State = _errID;
            GameManager.ClientMgr.SendToClient((obj as GameClient), DataHelper.ObjectToBytes<SystemErrorReponse>(szSystemErrorReponse), (int)CommandID.CMD_SYS_ERRCODE);
        }
        
    }
}
